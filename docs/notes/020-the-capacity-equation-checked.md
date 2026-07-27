# Note 020 — The capacity equation, checked against theory at last

`SNR = sqrt(d / N)` — node width over things stored — is the most load-bearing
equation in this project. It is why tiny nodes need selective storage, why the
oracle works, why note 015 was written and why it was wrong. It was obtained
**empirically here**, measured within 5% across a 16× range, and never checked
against anything.

It has now been checked, against Clarkson, Ubaru & Yang,
[*Capacity Analysis of Vector Symbolic Architectures*](https://arxiv.org/abs/2301.10352)
(arXiv:2301.10352). **The paper was read, not summarised** — the abstract does
not contain the relevant result, and the first two attempts to fetch it returned
only metadata and then unreadable PDF compression.

## The relevant result

Most of the paper is about set membership and intersection sizes. **Theorem 20 is
our case exactly**: a *bundle of key-value pairs*, asking whether a particular
binding is in it.

> **Theorem 20.** Let `v` be such that `x = sign(S⊙²v)` is a bundle of key-value
> pairs. Let `n = ‖v‖₁`. Then there is `m = O(n log(d/δ))` such that with failure
> probability at most `δ`, `j` is in the bundle if and only if
> `xᵀ S⊙²∗ⱼ ≥ 2√(m log(d/δ))`.

Translating to this project's names: `m` is our `d_model`, `n` is our `N`
(bindings stored), `d` is the **universe size** — our vocabulary.

## It agrees, on the axis we swept

Fix the accuracy in `SNR = sqrt(d_model / N)` and the width must grow **linearly
in `N`**. Theorem 20 says the same: `m = Θ(n · log d)` is linear in `n`.

So the equation this project has been building on is not a local observation. It
is an instance of a known bound, and the empirical fit and the analytic result
agree on the dependence that matters for tiny nodes.

That is worth having on its own. It also means the *shape* of every argument
built on it — that holding `N` constant is the whole of the oracle's advantage —
rests on something firmer than a curve fit.

## And it names a factor we have never varied

`log(d/δ)`. The **vocabulary**.

Every sweep in this project has held the vocabulary at 41 tokens. `n_keys` and
`n_values` have moved; the alphabet's total size has not moved enough to see a
logarithm. So the fit could not have detected this term, and did not.

It is not large, but it is not nothing:

    vocab     41    log ≈ 3.7      the only vocabulary ever measured
    vocab  1,000    log ≈ 6.9      1.9x the width for the same accuracy
    vocab 50,000    log ≈ 10.8     2.9x

**So a node that works at 32 dimensions on MQAR needs roughly 90 for the same
retrieval quality at a language-sized vocabulary**, before anything else about
language is taken into account. That is a real cost against the tiny-node claim
and it has never appeared in any estimate here.

This is the "check the frozen axes, not just the swept ones" rule finding its
second instance. The first cost a headline; this one was found by reading rather
than by measuring, which is cheaper.

## Prediction, registered before measuring

Widening the vocabulary at fixed `N` should require width to grow as
**`log(vocab)`**, not as `vocab` and not not at all.

Concretely: at fixed `n_pairs` and fixed `seq_len`, the width at which accuracy
crosses a fixed bar should rise by about **1.9×** going from vocab 41 to vocab
1,000. Guess: between 1.5× and 2.5×. If it is flat, the log term does not apply
to this variant and the paper's model differs from ours in a way that matters. If
it rises linearly in vocabulary, something worse is happening and the tiny-node
claim is in more trouble than this note says.

## Caveats, and one of them is real

**Theorem 20 is for MAP-B**, which bundles with `sign(...)` — a binarised
architecture. This project's store is real-valued and unnormalised, closer to
MAP-I. The linear-in-`n` shape is common to both in the paper's framing, but the
constant and the exact log factor are not transferable, and nothing above should
be read as a numeric prediction for our store.

**It is a sufficiency bound**, `m = O(...)`, not a tight characterisation. It says
that much width suffices, not that less fails.

## Status

**Read and compared. The vocabulary prediction is not measured.** The experiment
is cheap — vary `n_keys` and `n_values` together at fixed `n_pairs`, find the
width that crosses a fixed accuracy bar — and it belongs on Actions rather than
locally.
