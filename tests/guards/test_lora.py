"""The adapter attaches where it should, starts at zero, and merges once.

NO CHECKPOINT AND NO CARD. These are shape and arithmetic checks over a fake
weight dict, which is where the mistakes in a LoRA actually live: adapting the
wrong tensors, transposing a factor, or initialising so the untrained adapter
already perturbs the model.

WHY THE ZERO INITIALISATION MATTERS ENOUGH TO TEST. `B` starts at zero so the
delta is exactly zero before any training. That is what lets the regression
gate's first reading be a measurement of the BASE rather than of a random
perturbation of it -- and Phase 3's whole procedure is "measure, train, measure,
roll back if worse". A gate calibrated against a model that was already nudged
would have its noise floor set by the nudge.
"""

from __future__ import annotations

import torch

from sylvatica.learn.lora import CHANNEL_MIX, DEFAULT_TARGETS, TIME_MIX, LoraAdapter


def weights(n_layer: int = 3, n_embd: int = 64) -> dict[str, torch.Tensor]:
    """A fake `z` shaped like the package's, including the traps."""
    z: dict[str, torch.Tensor] = {}
    for i in range(n_layer):
        for name in ("receptance", "key", "value", "output"):
            z[f"blocks.{i}.att.{name}.weight"] = torch.randn(n_embd, n_embd)
        z[f"blocks.{i}.ffn.key.weight"] = torch.randn(n_embd, n_embd * 4)
        z[f"blocks.{i}.ffn.value.weight"] = torch.randn(n_embd * 4, n_embd)
        # The traps: per-channel gains and biases that are NOT worth adapting,
        # and a low-rank pair the model already has.
        z[f"blocks.{i}.att.x_r"] = torch.randn(n_embd)
        z[f"blocks.{i}.att.w0"] = torch.randn(n_embd)
        z[f"blocks.{i}.ln1.weight"] = torch.randn(n_embd)
    z["emb.weight"] = torch.randn(100, n_embd)
    z["head.weight"] = torch.randn(n_embd, 100)
    return z


def test_it_adapts_the_six_matrices_a_layer_and_nothing_else():
    """The doc: "a LoRA adapter over the core's time-mix and channel-mix weights".

    The small per-channel vectors are deliberately excluded. A rank-8
    factorisation of a 2048-element gain is not low-rank, it is a second copy
    with extra steps -- and adapting the embedding or the head would let the
    adapter rewrite what words MEAN rather than how they are mixed, which is a
    much bigger lever than this design is asking for.
    """
    z = weights(n_layer=3)
    lora = LoraAdapter(z, rank=4)

    assert len(lora.names) == 3 * len(DEFAULT_TARGETS)
    assert all(any(n.endswith(t) for t in DEFAULT_TARGETS) for n in lora.names)

    for forbidden in ("x_r", "w0", "ln1.weight", "emb.weight", "head.weight"):
        assert not any(forbidden in n for n in lora.names), f"{forbidden} was adapted"

    assert len([n for n in lora.names if any(n.endswith(t) for t in TIME_MIX)]) == 12
    assert len([n for n in lora.names if any(n.endswith(t) for t in CHANNEL_MIX)]) == 6


def test_an_untrained_adapter_is_indistinguishable_from_no_adapter():
    """`B` starts at zero, so the delta is EXACTLY zero. See the module docstring
    for why the gate depends on it."""
    z = weights()
    lora = LoraAdapter(z, rank=4)
    for name in lora.names:
        assert torch.equal(lora(name, z[name]), z[name]), f"{name} moved before training"
        assert float(lora.delta(name).detach().abs().max()) == 0.0


def test_a_weight_it_does_not_adapt_comes_back_untouched():
    z = weights()
    lora = LoraAdapter(z, rank=4)
    for name in ("emb.weight", "blocks.0.att.x_r", "blocks.1.ln1.weight"):
        assert lora(name, z[name]) is z[name], f"{name} was copied unnecessarily"
        assert lora.delta(name) is None


def test_the_factors_are_shaped_for_weights_stored_transposed():
    """The package applies `x @ W` with W of shape (in, out), not the usual
    (out, in). `A` is therefore (in, r) and `B` is (r, out); getting it backwards
    raises rather than answering wrongly, which is this file's one mercy."""
    z = weights(n_layer=1, n_embd=32)
    lora = LoraAdapter(z, rank=4)
    for name in lora.names:
        assert lora.delta(name).shape == z[name].shape


def test_the_delta_is_actually_low_rank():
    z = weights(n_layer=1, n_embd=32)
    lora = LoraAdapter(z, rank=2)
    key = "blocks_0_att_key_weight"
    lora.b[key].data.normal_()
    d = lora.delta("blocks.0.att.key.weight")
    assert torch.linalg.matrix_rank(d.detach(), tol=1e-5) <= 2


def test_merging_moves_the_base_and_resets_the_adapter():
    """THE ONLY IRREVERSIBLE STEP IN THE DESIGN. A cycle rolls back by discarding
    an adapter; a merge cannot be undone, so Phase 3 must take the gate's verdict
    BEFORE merging rather than after."""
    z = weights(n_layer=2)
    lora = LoraAdapter(z, rank=4)
    for key in lora.b:
        lora.b[key].data.normal_()

    name = lora.names[0]
    before = z[name].clone()
    expected = before + lora.delta(name).detach()

    merged = lora.merge_into(z)
    assert merged == len(lora.names)
    assert torch.allclose(z[name], expected, atol=1e-5), "the merge did not apply the delta"

    # And the adapter is fresh: a second merge must be a no-op, not a doubling.
    for name in lora.names:
        assert float(lora.delta(name).detach().abs().max()) == 0.0
    again = z[lora.names[0]].clone()
    lora.merge_into(z)
    assert torch.allclose(z[lora.names[0]], again), "merging twice applied the delta twice"


def test_the_trainable_count_is_small_enough_to_be_the_point():
    """Rank 8 over 24 layers is a few million parameters against 1.5 billion.
    If this number ever approaches the base, the adapter has stopped being the
    reason to believe refutation 2 might not fire."""
    z = weights(n_layer=24, n_embd=2048)
    lora = LoraAdapter(z, rank=8)
    base = sum(t.numel() for t in z.values())
    assert lora.trainable < base * 0.02, (
        f"{lora.trainable:,} trainable against {base:,} base -- the low-rank "
        "delta is no longer meaningfully low-rank"
    )
