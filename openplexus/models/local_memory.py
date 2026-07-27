"""A locality-respecting associative memory — G1's candidate.

Everything that reached 1.000 so far did it with attention, which violates C1
twice over: a softmax normalising over every position, and a backward pass that
carries information from the loss back through the whole sequence. Neither can
run on machines that never synchronise.

This is the local alternative. It keeps the one property note 006 §7 identified
as necessary — **input-dependent mixing**, here as content-addressed retrieval —
and obtains it without either violation.

## What happens, and why each step is local

At each position `t`, with `e` the embedding of the current token:

    k = Wk e                      key      (frozen random projection)
    v = Wv e                      value    (frozen random projection)
    M += v ⊗ k_previous           STORE    bind the previous token to this one
    r = M k                       RETRIEVE query the store with the current token
    y = Wo r                      predict
    Wo += lr · (target − y) ⊗ r   LEARN    delta rule

- **`M += v ⊗ k_prev` is an outer product.** Entry `M[i,j]` changes by
  `v[i] · k_prev[j]` — the product of a signal at its output side and a signal at
  its input side. That is the most local update there is: a synapse changing on
  what its own two ends are doing, consulting nothing else. Purely Hebbian.
- **`r = M k` is a matrix-vector product.** Output `i` sums over its own incoming
  connections. No normalisation across units, no softmax, nothing pooled.
- **The delta rule is local too.** The error is the output unit's own prediction
  error against its own next input; the input is its own retrieved vector.
  Nothing travels backwards through anything.
- **Nothing is stored across sequences except `Wo`.** `M` is per-sequence working
  memory, built and discarded as the sequence runs. It is not a parameter and
  nothing optimises it.

## What it is, in prior-art terms

A fast-weight associative memory (Hebb; Hopfield; Ba et al. 2016), which is also
what a linear-attention layer computes. That lineage matters for expectations
rather than for credit: docs/notes/006 records that linear attention **fails**
MQAR unless its state is large, while softmax attention does not. So a width
penalty relative to the attention model is the *expected* outcome, and the size
of that penalty is the measurement G1 wants (g1-04).

## What is deliberately not claimed

- **`Wk` and `Wv` are frozen random.** Only `Wo` learns. That is the strictest
  version of the question and the honest place to start: if it works, no case has
  been made that those projections need learning; if it fails, learning them
  locally is the next thing to try rather than the reason it failed.
- **This is not distributed.** It is a *locality-respecting* computation running
  in one process. Whether it survives real delay and churn is G2 and G3.
"""

from __future__ import annotations

from dataclasses import dataclass

import numpy as np


def surprise(scores: np.ndarray, token: int) -> float:
    """How unexpected `token` was, given the scores predicted before it arrived.

    The negative log of the token's share of the scores, after they are turned
    into a distribution. **The property that makes this surprise rather than
    some other number is that it depends on the WHOLE prediction** -- on every
    alternative the node considered and rejected, not only on the best one.
    tests/test_surprise_means_surprise.py checks exactly that.

    The measure this replaced -- the margin between the best score and the
    arriving token's -- reads two numbers and ignores the rest. So it cannot see
    the thing learning actually does, which is to suppress the alternatives: a
    node can go from "these five were nearly tied" to "it was clearly this one"
    with the margin unmoved. Measured on eight repeats of one identical cycle
    the margin ROSE 266%, where surprise must fall. John caught it by asking why
    a repeating pattern was not becoming less surprising.

    Available to a node from its own last output and its own next input, so it
    costs no communication and satisfies C1.
    """
    shifted = scores - scores.max()          # overflow only, exp is shift-free
    weights = np.exp(shifted)
    return -float(np.log(weights[token] / weights.sum() + 1e-12))


def admit(strengths, strength: float, capacity: int) -> int | None:
    """Which slot a candidate of this strength takes, or None if it is refused.

    The competition in competitive capture. Synaptic capture is winner-take-all
    over a finite pool of plasticity-related proteins: tagged synapses contend
    and most lose. `capacity` is the pool.

    Returns an index into `strengths` -- the end of it when there is room, or the
    weakest incumbent when there is not and the candidate beats it. **None means
    the candidate loses**, which is the case that has to exist: a pool where
    everything gets in is a pool in name only.

    Ties go to the incumbent, because a strictly-greater test is what makes a
    long run of equal-strength candidates settle instead of churning.
    """
    if len(strengths) < capacity:
        return len(strengths)
    weakest = min(range(len(strengths)), key=lambda i: strengths[i])
    return weakest if strength > strengths[weakest] else None


def fade(rank: float, factor: float) -> float:
    """Move a mark one step closer to losing its slot.

    `admit` keeps the LARGEST rank, so a mark fades by its rank falling -- and
    which arithmetic does that depends on which end of the ranking is winning.
    A tag admitting weak retrievals holds negative ranks, where falling means
    growing in magnitude; one admitting strong retrievals holds positive ranks,
    where falling means shrinking. Multiplying both by `factor` fades one and
    ENTRENCHES the other, which is the first version of this and it changed
    nothing measurable in either direction -- the marks it was supposed to
    release became the ones that could never be displaced.

    A mark that never fades ranks the whole interval since the last reward at
    once, and the writes made when the store was smallest are the weakest
    retrievals it will ever see. So an un-faded tag fills with the first few
    writes after every capture, which is a recency policy pointing backwards.
    """
    return rank * factor if rank > 0 else rank / factor


def tag(tagged: list, strength: float, index: int, capacity: int,
        strongest: bool) -> None:
    """Offer the write at `index` to the tag, keeping it if it beats an incumbent.

    The tag is a fixed number of marks over WRITES, not a span over steps. That
    distinction is the mechanism: at 31 steps per binding a 64-step window holds
    two bindings and sixty-two steps of filler, while four marks span about 124
    steps and hold four bindings. A capacity over admitted items reaches an
    arbitrary delay; a capacity over steps reaches exactly that many steps.

    **The rank is negated, and the negation is the finding.** `admit` keeps the
    largest values, so ranking by `-strength` keeps the WEAKEST retrievals.
    [g9-04](../../experiments/sweeps/g9-04-is-there-a-local-signal.txt) scored
    retrieval strength at AUC 0.293 and 0.215 separating a binding-write from a
    filler-write -- below 0.5, so separating them backwards. Filler is drawn with
    replacement from a small spare alphabet, so a filler key has been bound many
    times and retrieves strongly; a binding's cue is fresh and retrieves weakly.

    Competitive capture ranks on this same quantity and admits the strongest
    (`capture_slots`). It was pointed backwards, which is a mechanistic account
    of one of the six failures rather than another guess about base rates.

    `strongest` reverses it back, and exists to be run as an arm. If admitting
    the strongest scores the same as admitting the weakest, the capacity is
    doing the work and the signal is decoration -- which is the outcome that
    would refute the reason this mechanism was built.
    """
    rank = strength if strongest else -strength
    slot = admit([held for held, _ in tagged], rank, capacity)
    if slot is None:
        return
    if slot < len(tagged):
        tagged[slot] = (rank, index)
    else:
        tagged.append((rank, index))


def _fade(pending: list, factor: float) -> None:
    """Record that the fast store was multiplied by `factor`.

    Everything waiting on a reward was scaled along with the store, so its
    recorded weight has to follow. Without this, taking a contribution back out
    would remove what it was when it went in rather than what it is now — and
    the store would drift away from the sum of what is actually in it.
    """
    for entry in pending:
        entry[0] *= factor


def scale_to(store: np.ndarray, cap: float) -> None:
    """Shrink `store` in place until its norm is at most `cap`. No-op if `cap` is 0.

    **Scales the whole store, never an entry.** That distinction is the whole
    mechanism: synaptic scaling multiplies a neuron's synapses by a common
    factor, preserving what the store has learned about the *relative* strength
    of its contents while bounding the total. Clipping individual weights to a
    budget bounds the total too, and is a different and non-local operation --
    it inspects entries rather than one number, and it destroys the ratios.

    The difference is invisible in the model's predictions, because both are
    gated on `norm > cap` so both make the cap's value decide *when* they fire.
    That is why this is a function: the property that separates them is
    arithmetic, and arithmetic is testable directly.

    Used by both stores. Zenke & Gerstner (2017) is titled *Hebbian plasticity
    requires compensatory processes on multiple timescales*, and for a while this
    project had implemented one.
    """
    if not cap:
        return
    size = float(np.linalg.norm(store))
    if size > cap:
        store *= cap / size


@dataclass(frozen=True)
class LocalMemoryConfig:
    """Shape and learning settings.

    Attributes:
        vocab_size: Token alphabet size.
        d_model: Width of the key/value space. **This is the dial G1 measures.**
            g1-04 put the attention model's threshold between 8 and 16; the ratio
            between that and whatever this needs is the price of locality.
        lr: Delta-rule step size for the output weights.
        decay: Per-step multiplier on the memory. 1.0 keeps everything;
            below 1.0 forgets older bindings, which bounds the interference a
            long sequence accumulates.
        partitions: How many independent groups the width is split into. **This
            is the C1 dial.** With 1 the readout is a single `vocab × d_model`
            matrix whose error term sums over every dimension — which, once
            `d_model` is spread across machines, is a globally synchronised step
            of exactly the kind C1 forbids (note 009 §4). With P > 1 the width is
            cut into P equal groups, each with its own readout over its own
            dimensions only, each learning from its own prediction error. No
            machine's update depends on any other machine's activity.

            **This does not make the reduction vanish, and claiming so would be
            wrong.** What it changes is the reduction's shape: from `d`-sized and
            every step and mandatory, to `vocab`-sized and only at positions where
            an answer is wanted — and *optional*, because each group emits a
            complete answer by itself. Whether one group's answer alone is good
            enough is the measurement, not an assumption.
        decay_when_masked: Whether the fast store fades on steps the `store`
            mask excludes. False keeps the original behaviour, which is how
            every result in this project was measured.

            **The decay lives inside the `store[t]` guard**, so a masked-out
            position is not merely un-written — it is un-faded. On MQAR with 92%
            filler, an oracle-gated arm therefore skips the fade on 92% of steps
            and runs at an effective half-life roughly an order of magnitude
            longer than the ungated arm at the same nominal `decay`.

            So the oracle has been doing two things and only one was named: it
            stores less **and** it forgets more slowly. Six mechanisms have
            failed to match it, all of them aimed at selectivity alone.

            Setting this True gives an oracle its selectivity without its
            retention bonus, which is the only way to find out how much of the
            gap is which. See
            docs/notes/019-the-oracle-also-slows-forgetting.md.
        reward_token: Token id whose arrival means *the recent past mattered*.
            -1 disables the gate, which is how every earlier result was
            measured.

            **This gates the fast store, which is the only thing the oracle
            does.** g8-03 established that: `run(store=mask)` prevents writing,
            holding the number of stored bindings constant at any length, and
            six mechanisms failed because they acted on the lasting store
            instead.

            It is not the oracle. The oracle reads `position_kinds()`, a
            property of the generator no running system can see. This reads a
            token off the same input every node already receives.

        reward_window: How many steps before a reward token are kept. 0 keeps
            only the step immediately before it.

            **This is the whole difficulty.** The reward arrives after the
            binding it refers to, so at write time the node does not yet know
            whether to keep anything. It writes everything into the fast store
            as usual and, when a reward arrives, keeps the last
            `reward_window + 1` steps and discards the rest of what it wrote
            since the previous reward.

            A window that always covers the gap makes the gate trivial -- "keep
            the thing before the obvious marker" -- and learns nothing about
            value. A window shorter than the delay cannot reach the binding at
            all. The interesting region is between, and it is what g9-02 sweeps.
        tag_slots: How many writes the tag may hold at once. 0 keeps the
            window rule above, which is how every earlier result was measured.

            **This is `reward_window` measured in bindings instead of steps**,
            and that is the whole of the mechanism. Both gates keep a small set
            of writes when the reward arrives and discard the rest; they differ
            only in how that set is chosen. The window takes the most recent
            `reward_window + 1` writes. The tag takes the `tag_slots` writes with
            the WEAKEST retrieval, wherever in the interval they fell.

            [g9-03](../../experiments/sweeps/g9-03-is-the-cliff-reach-or-cost.txt)
            is why that matters. Its table is a diagonal cliff: every cell where
            the window covers the delay recovers about 0.2, every cell where it
            does not sits at about -0.22, and a node does not know the delay.
            Widening the window does not fix it, because reach is bought in
            steps -- a window of 64 recovers 0.09 at every delay, since
            sixty-two of those steps are filler.

            The tag has no such cliff to have. Its reach is however far back the
            weakest write it still holds was made, so it is set by the density
            of bindings rather than by a span somebody has to guess.

            **What it does NOT do is predict reward.** `reward_recall` picks
            rewarded cues uniformly out of the same alphabet as the filler, so a
            rewarded binding and an unrewarded one are statistically identical
            until the reward token arrives, and a tag claiming to tell them
            apart would be reading `position_kinds()` in disguise. The tag is
            selective about being a binding at all; the reward token supplies the
            value. That split is the one the biology describes and the only one
            available here.
            docs/notes/022-the-signal-was-there-and-pointing-backwards.md.
        tag_strongest: Rank the tag by the strongest retrieval instead of the
            weakest. **A control, not a setting.**

            g9-04 measured the signal as inverted, so the tag admits weak
            retrievals. This arm admits strong ones instead, holding capacity,
            timing and every other quantity fixed. If it scores the same, the
            capacity is doing the work and the signal is decoration -- the reason
            this mechanism exists would be refuted while its headline number
            still looked fine. Nothing else in the sweep separates those two.
        memory_cap: Largest norm the FAST store may reach. 0 leaves it
            unbounded, which is how every earlier result was measured.

            **The fast store had no brakes and the paper said it should.**
            `lasting_cap` bounds the consolidated store and cites Zenke &
            Gerstner (2017), whose title is *Hebbian plasticity requires
            compensatory processes on multiple timescales*. We took the
            compensatory process, applied it to one store, and left the other
            unbounded.

            The consequence is arithmetic. `memory = decay * memory + outer(...)`
            is a geometric series, so a recurring token drives it toward
            `1 / (1 - decay)` — about **277x** a single binding at the half-life
            these sweeps use. Retrieval is linear in that and the delta-rule
            update is **quadratic**, so it diverges to NaN. Measured without
            training: the memory norm goes 114 to 967 and the largest retrieval
            137 to 3452 as filler skew rises.

            This is not about skewed data. Skew supplies repetition, and so does
            real language, a sensor reporting the same reading twice, or a quiet
            period on a node.

            Scales the whole store, never an entry — see `lasting_cap`, where
            the same distinction is pinned by a mutation.
            docs/notes/018-the-fast-store-has-no-brakes.md.
        capture_slots: How many promotions the lasting store may hold at once.
            0 keeps the original rule — every step that clears the gate is
            promoted, and nothing is ever displaced.

            **This is the scarcity that note 010 left out.** Synaptic capture is
            competitive: tagged synapses contend for a finite pool of
            plasticity-related proteins and most of them lose. We built the tag
            and the later signal and gave the pool no limit.

            It matters because a threshold fires at a *rate*, so promotions grow
            with sequence length, so the number of things superposed grows, so
            retrieval — which goes as `sqrt(d / N)` — decays with length. That is
            g8-01's measured result: recovery fell from 0.05 at seq 192 to −0.00
            at 1536. The oracle wins by holding `N` **constant**, and a fixed
            number of slots is the only tried mechanism that also does.

            It is also why the base rate stops mattering. A bar drowns when 92%
            of the sequence is filler, because 92% of a large number is large. A
            budget of `k` promotes `k` things whatever the base rate.

            A slot costs `w + 1` numbers — this node's own slice of a retrieval,
            plus the **token id**. Not the key vector: with `derived_keys` the key
            is regenerated from `(seed, token)`, and without that a width-1 node
            could not afford a single slot. See
            docs/notes/015-we-implemented-the-tag-and-not-the-competition.md,
            where the first version of the arithmetic was wrong.
        salience: How many standard deviations of surprise a step must carry
            before consolidation fires. 0 keeps the original rule — consolidate
            whenever the previous prediction was right — which
            [g7-04](../../experiments/sweeps/g7-04-when-does-forgetting-pay.txt)
            measured as monotonically harmful.

            **This is John's observation, and it names the failure exactly.** A
            brain floods with neuromodulator on *external* events, and the flood
            is stronger for outcomes that are very good and very bad. Our gate
            fired on every correct prediction instead, which once the model works
            is most of them — so the lasting store, which never decays, filled up
            with the routine and accumulated the saturation the fast store was
            fading to avoid.

            Salience is measured against the node's own running experience: it
            keeps an estimate of its typical surprise and consolidates only when
            the current step departs from it by more than `salience` deviations,
            **in either direction**. Very wrong and very right both count; the
            unremarkable middle does not. That is local, needs no signal from
            anywhere else, and fires on the tail rather than the bulk.

            Lehr et al. put the same structure in the protein threshold,
            `θ_pro(NM) = 1/(NM + 0.001)` — more neuromodulator, lower bar. Here
            the bar is fixed and the surprise has to clear it.
        lasting_cap: Largest norm the consolidated store may reach. 0 leaves it
            unbounded, which diverges.

            **A salience gate cannot run without this, and finding out why was
            the useful part.** Consolidating on *correct* predictions is
            self-limiting: being correct means the retrieval was already good, so
            promoting it adds nothing extreme. Consolidating on *surprise* is
            positive feedback — a large surprise promotes a large retrieval,
            which enlarges the store, which enlarges later retrievals and later
            surprises. Left alone it reaches NaN.

            Zenke and Gerstner, in the sources that prompted this, put the point
            in a title: *Hebbian plasticity requires compensatory processes on
            multiple timescales*. Hebbian storage is unstable by construction and
            biology pairs it with something that pulls the total back down —
            synaptic scaling, which normalises a cell's weights rather than
            editing any one of them.

            This is that, in its crudest form: when the consolidated store
            exceeds the cap, scale the whole thing back. It is a single
            multiplication over one node's own rows, so it stays local.
        derived_keys: Draw each key row from its own seed rather than the whole
            table from one. Off by default, which reproduces every earlier
            result exactly.

            **This is what lets a node be sent a token instead of a key vector.**
            `Wk` is a frozen random projection that never learns, so it does not
            have to be stored: with a per-token seed, any node can regenerate any
            row it needs from the token id alone. Broadcasting a token costs 32
            bytes per step at fan-out 8 whatever the width, against `8·d·4` for
            the key — a factor of four thousand at `d = 4096`.

            [Note 012](../../docs/notes/012-broadcast-the-token.md) works through
            the trade: it roughly triples a node's compute, which
            `tools/step_rate.py` shows is 21× to 380× under-used, in exchange for
            removing the width term from the bandwidth cost, which is binding.

            The two projections are statistically indistinguishable — row norms
            0.9912 against 0.9925, mean absolute overlap 0.04987 against 0.04854
            — but that is not the same as verified, so `tests/test_derived_keys.py`
            checks the model scores the same on the task rather than trusting the
            statistics.
        consolidation: Rate at which a *successful* retrieval is written into a
            second, non-decaying memory. 0 disables it and reproduces every
            earlier result exactly.

            **This is the implementable replacement for the oracle gate.**
            [g7-01](../../experiments/sweeps/g7-01-the-oracle-gate.txt) showed
            that keeping only the useful bindings makes the task trivial —
            devices of one dimension go from 0.404 to 1.000 — but the oracle
            reads task structure no running system has. The blocker was that
            nothing at storage time separates a useful binding from filler.

            [Note 010](../../docs/notes/010-tagging-and-capture.md) took the
            answer from synaptic tagging and capture: **do not decide at storage
            time.** Write everything weakly into a fast, decaying store; when a
            retrieval later turns out to have been right, promote what was
            retrieved into a store that does not decay.

            The signal is the model's own prediction against the next token that
            arrives, so it is self-supervised and entirely local — no labels, no
            coordination, no lookahead. It needs `decay` below 1 to be meaningful,
            since a fast store that never fades is not fast.
        key_active: Non-zero dimensions per key, or 0 for dense signed keys.

            **Biology's standard answer to interference is a sparse, non-negative
            code**, and this is that. With `a` active dimensions out of `d`, two
            random keys share about `a²/d` of them, so distinct addresses
            interfere less as `a` falls.

            What it cannot help with is *same*-address collisions — one token
            bound to different values at different positions — and on this task
            those dominate, because every token recurs many times in a sequence.
            0 keeps the dense signed projection every earlier result was measured
            with.
        key_scale: Multiplier on the frozen projections, on top of the
            `1/sqrt(d_model)` that keeps keys at unit norm.

            **This exists because it was silently 1.0 and that was the wrong
            value.** g3-02 measured a width-32 model at 0.263 with unit-norm keys
            and 0.960 with the same keys multiplied by 0.71 — nothing else
            changed. Retrieval magnitude goes as the cube of this, against a
            fixed learning rate, so it and `lr` are not independent and neither
            can be left untuned while the other is swept. The width curve in
            g1-05 and g1-06 was measured with this pinned at 1.0 and is a curve
            about that choice as much as about width.
        seed: Determines the frozen projections completely.
    """

    vocab_size: int
    d_model: int = 64
    lr: float = 0.05
    decay: float = 1.0
    partitions: int = 1
    key_scale: float = 1.0
    key_active: int = 0
    decay_when_masked: bool = False
    reward_token: int = -1
    reward_window: int = 0
    tag_slots: int = 0
    tag_decay: float = 1.0
    tag_strongest: bool = False
    memory_cap: float = 0.0
    consolidation: float = 0.0
    capture_slots: int = 0
    salience: float = 0.0
    lasting_cap: float = 0.0
    derived_keys: bool = False
    seed: int = 0

    def __post_init__(self) -> None:
        if self.vocab_size < 2:
            raise ValueError("vocab_size must be at least 2")
        if self.d_model < 1:
            raise ValueError("d_model must be at least 1")
        if not 0.0 < self.lr:
            raise ValueError("lr must be positive")
        if not 0.0 < self.decay <= 1.0:
            raise ValueError("decay must be in (0, 1]")
        if self.key_scale <= 0.0:
            raise ValueError("key_scale must be positive")
        if self.derived_keys and self.key_active:
            raise ValueError(
                "derived_keys and key_active both build Wk and would conflict; "
                "sparse keys have no per-token derivation yet")
        if self.reward_window < 0:
            raise ValueError("reward_window must not be negative")
        if self.reward_window and self.reward_token < 0:
            raise ValueError(
                "reward_window is the reach of the reward gate and does nothing "
                "without reward_token")
        if self.tag_slots < 0:
            raise ValueError("tag_slots must not be negative")
        if self.tag_slots and self.reward_token < 0:
            raise ValueError(
                "the tag is captured by the reward token and does nothing "
                "without it")
        if self.tag_slots and self.reward_window:
            raise ValueError(
                "reward_window and tag_slots are two answers to the same "
                "question -- how the gate chooses what to keep when the reward "
                "arrives -- and enabling both runs an arm that is neither")
        if not 0.0 < self.tag_decay <= 1.0:
            raise ValueError("tag_decay must be in (0, 1]")
        if self.tag_decay < 1.0 and not self.tag_slots:
            raise ValueError(
                "tag_decay fades marks and does nothing without tag_slots")
        if self.tag_strongest and not self.tag_slots:
            raise ValueError(
                "tag_strongest picks which end of the tag wins and does nothing "
                "without tag_slots")
        if self.memory_cap < 0.0:
            raise ValueError("memory_cap must not be negative")
        if self.capture_slots < 0:
            raise ValueError("capture_slots must not be negative")
        if self.capture_slots and not self.consolidation:
            raise ValueError(
                "capture_slots bounds consolidation and does nothing without it")
        if self.lasting_cap < 0.0:
            raise ValueError("lasting_cap must not be negative")
        if self.salience and not self.lasting_cap:
            raise ValueError(
                "a salience gate without a cap diverges: promoting on surprise "
                "enlarges the store, which enlarges later surprises")
        if self.salience < 0.0:
            raise ValueError("salience must not be negative")
        if self.salience and not self.consolidation:
            raise ValueError(
                "salience gates consolidation and does nothing without it")
        if self.consolidation < 0.0:
            raise ValueError("consolidation must not be negative")
        if self.consolidation and self.decay >= 1.0:
            raise ValueError(
                "consolidation needs decay < 1: with a memory that never fades "
                "there is no fast store for it to rescue anything from")
        if not 0 <= self.key_active <= self.d_model:
            raise ValueError(
                f"key_active must be in [0, {self.d_model}], got "
                f"{self.key_active}")
        if self.partitions < 1:
            raise ValueError("partitions must be at least 1")
        if self.d_model % self.partitions:
            raise ValueError(
                f"d_model {self.d_model} does not divide into "
                f"{self.partitions} partitions")


class LocalAssociativeMemory:
    """Hebbian store, content-addressed retrieval, delta-rule readout.

    The contract: `run` processes one sequence left to right, returning a
    prediction of the next token at every position. With `learn=True` it also
    updates `Wo` online as it goes. No backward pass exists.
    """

    def __init__(self, config: LocalMemoryConfig) -> None:
        self.config = config
        rng = np.random.default_rng(config.seed)
        d, v = config.d_model, config.vocab_size
        # Rows scaled to roughly unit norm, so retrieval needs no normalisation
        # step. Normalising `k` at run time would be a per-vector operation and
        # defensible, but avoiding it entirely keeps the C1 argument simple.
        spread = config.key_scale / np.sqrt(d)
        if config.key_active:
            # Sparse, non-negative, unit-norm: `key_active` ones per row, scaled
            # so every key has the same length as a dense one. Non-negative is
            # the biologically faithful part -- firing rates do not go below zero
            # -- and it is also what makes sparsity worth anything, since a DENSE
            # non-negative code has every pair of keys strongly overlapping.
            self.wk = np.zeros((v, d))
            for token in range(v):
                active = rng.choice(d, config.key_active, replace=False)
                self.wk[token, active] = config.key_scale / np.sqrt(
                    config.key_active)
        elif config.derived_keys:
            # One draw per token, so a node holding only the seed can rebuild any
            # row on demand. Deliberately NOT taken from `rng`: the point is that
            # row `t` depends on `(seed, t)` alone and on nothing drawn before it,
            # which is what makes it reconstructible out of order.
            self.wk = np.stack([
                np.random.default_rng((config.seed, token)).normal(0.0, spread, d)
                for token in range(v)])
        else:
            self.wk = rng.normal(0.0, spread, (v, d))
        self.wv = rng.normal(0.0, spread, (v, d))
        self.wo = np.zeros((v, d))
        # A view, not a copy: writes through `grouped_wo` land in `wo`, so the
        # readout has exactly one representation and `ablate` keeps working
        # unchanged. Reshaping a freshly allocated C-contiguous array never
        # copies, and the assertion says so rather than trusting it.
        self.grouped_wo = self.wo.reshape(v, config.partitions, -1)
        assert self.grouped_wo.base is self.wo, "grouped_wo must alias wo"

    def ablate(self, dimensions) -> None:
        """Permanently remove these dimensions — a machine has left, for good.

        This is C3's failure, and it is a different thing from C2's. A dropped
        message is transient: the next one arrives. A departed machine takes its
        share of the state with it and never comes back.

        If the `d_model` dimensions were spread across machines, one machine
        leaving is a slice of them gone. Zeroing the corresponding columns of the
        frozen projections is enough to model that: with `wv[:, j]` zero the
        memory's row `j` is empty, with `wk[:, j]` zero its column `j` is, and
        the retrieved vector is therefore zero in those dimensions. The delta
        rule then multiplies by that zero, so the readout's columns stay dead
        without needing to be masked — the machine cannot come back by accident,
        which is the property being modelled.

        **What a departing node takes depends on whether it held keys.** With
        `derived_keys` off, `Wk` is a stored table and each node owns its columns
        of it, so a departure removes key dimensions every *surviving* node
        needed — the shared-broadcast loss that made churn damage global rather
        than local. With `derived_keys` on, no node holds any of `Wk`: each
        computes the full key from the token, so a departure takes only that
        node's own values and readout and the survivors are untouched.

        The difference is large and grows with churn. At seq_len 192, width 64,
        three seeds:

            removed    holds keys    derives keys    gain
                25%         0.961           0.982   +0.021
                50%         0.808           0.919   +0.111
                75%         0.504           0.760   +0.256

        So this reads `derived_keys` rather than taking a flag: whether a
        departing node had keys to take is a property of the configuration, not a
        choice the caller should be able to get wrong.

        **Note what is NOT lost.** The associative memory is per-sequence working
        state, rebuilt from scratch every sequence. Only the readout persists
        across sequences. So a departing machine costs *capacity*, and costs
        whatever the readout had learned in those dimensions — it does not take
        away stored memories, because there are none to take.
        """
        index = np.asarray(list(dimensions), dtype=int)
        if index.size and (index.min() < 0 or index.max() >= self.config.d_model):
            raise ValueError(
                f"dimension outside [0, {self.config.d_model}): {index}")
        if not self.config.derived_keys:
            self.wk[:, index] = 0.0
        self.wv[:, index] = 0.0
        self.wo[:, index] = 0.0

    def surviving_width(self) -> int:
        """How many dimensions still carry signal.

        The honest denominator after churn. Reporting a score against the
        original `d_model` would credit the model with room it no longer has.
        """
        # Counted through the VALUES, not the keys. A departing node always
        # takes its own values; it takes key columns only when keys are a stored
        # table. Counting through `wk` therefore reported a derived-key network
        # as fully intact after half of it had left -- caught by
        # tests/test_departure.py the moment ablate() learned to respect
        # derived_keys, and wrong in the direction that flatters churn survival.
        return int((np.abs(self.wv).sum(axis=0) > 0).sum())

    def run(self, tokens: np.ndarray, targets: np.ndarray | None = None,
            scored: np.ndarray | None = None, learn: bool = False,
            partition=None,
            store: np.ndarray | None = None, leave=None,
            trace: list | None = None) -> np.ndarray:
        """Process one sequence; return the predicted next token per position.

        Args:
            tokens: The sequence.
            targets: Target token per position. Required when `learn` is True.
            scored: Positions the delta rule is applied at. Required when `learn`
                is True. **Note this is a training-time convenience, not part of
                the model** — a genuinely autonomous unit would learn at every
                step. It exists so that this rule can be compared against the
                attention model under the same objective.
            learn: Whether to update `Wo` online.
            leave: `(step, nodes)` — those nodes vanish permanently at that
                step, mid-sequence. `None` means nobody leaves.

                **This is the failure a real network actually has, and the one
                nothing here measured.** `ablate` models a machine gone *between*
                sequences, which is a tidy world where every sequence starts with
                a known set of participants. A machine that drops out halfway
                through takes its rows of the memory with it — including whatever
                was stored in them earlier in this very sequence — and the
                answers after that point are produced without them.

                The survivors keep their own rows and, with `derived_keys`, their
                full key, so they carry on unaffected except that part of the
                memory has gone dark.
            store: Which positions write into the memory. `None` stores every
                consecutive pair, which is what everything before this measured.

                **This is the dial for selective storage.** The store binds every
                consecutive pair, so the number of things in memory *is* the
                sequence length -- and by the measured `sqrt(d/N)` retrieval law
                that is where every scaling exponent in this project comes from.
                On MQAR a 384-step sequence stores 383 bindings and the task asks
                about 4, so over 98% of the interference is bindings no query will
                ever touch.

                Supplying a mask here is an *oracle*: it uses knowledge a running
                system would not have, and exists to measure the ceiling before
                anything is built to reach it. A real gate has to decide from
                locally available signals at the moment of storage, and whether
                such a signal exists on this task is a separate and harder
                question.
            trace: If given, one dict appended per step carrying the signals a
                gate could actually consult — `surprise`, the node's running
                `mean` and `deviation` of it, and the size of the trace being
                written. **Observation only**: nothing here reads it back, and
                `tests/test_trace_observes.py` pins that a traced run and an
                untraced one produce identical predictions.

                It exists because "is there a local signal that separates a real
                binding from filler" is a question about the model's own
                quantities, and the alternative is a probe that recomputes them
                — which is how the 150/300 cap values came from a
                reimplementation whose store never bound.
            partition: Which machines answer. An integer reads one machine
                alone; an iterable of integers reads a **cluster** of them pooled
                together; `None` pools every machine.

                The three cases are one dial, and it is the dial that decides how
                small a machine can be. A lone machine has to be wide enough to
                answer by itself. Pooling everything is the other extreme and
                costs a reduction across the whole network, affordable only if the
                network is small or answers are rare.

                **A cluster is the middle, and it is the interesting case.** A
                handful of machines that pool locally — because they are near each
                other, or cheap to reach — act as one wider machine without any of
                them being wide. Whether a small cluster recovers most of what
                full pooling buys decides whether genuinely tiny devices can
                take part at all.

        Returns:
            `argmax` of the readout at each position.
        """
        if learn and (targets is None or scored is None):
            raise ValueError("learning needs targets and scored positions")
        groups = self.config.partitions
        members = None
        if partition is not None:
            members = ([int(partition)]
                       if isinstance(partition, (int, np.integer))
                       else [int(g) for g in partition])
            if not members:
                raise ValueError("a cluster must contain at least one machine")
            for member in members:
                if not 0 <= member < groups:
                    raise ValueError(
                        f"partition outside [0, {groups}): {member}")
            if len(set(members)) != len(members):
                raise ValueError(
                    f"a machine appears twice in the cluster {members}, which "
                    f"would double-count its answer")

        d = self.config.d_model
        # Which dimensions are still held by somebody. Local to this call: a
        # departure is a fact about one run, not a permanent edit, so one model
        # can be asked what happens under many different failures.
        alive = np.ones(d)
        leave_at, leaving = None, ()
        if leave is not None:
            leave_at, leaving = leave
            for node in leaving:
                if not 0 <= node < groups:
                    raise ValueError(
                        f"departing node outside [0, {groups}): {node}")
            if not 0 <= leave_at < len(tokens):
                raise ValueError(
                    f"departure step {leave_at} outside a sequence of "
                    f"{len(tokens)}")

        memory = np.zeros((d, d))
        # The consolidated store. Written only when a retrieval is confirmed
        # useful by the token that arrives next, and never decayed -- which is
        # the whole difference between it and `memory`.
        # Written into the fast store but not yet vouched for by a reward.
        # Each entry is [weight, value, key]; the weight tracks every scaling
        # the store has had since, so a contribution is removed as it now
        # stands rather than as it went in.
        pending: list = []
        # Which of those writes are marked. Each entry is (rank, index into
        # `pending`), and it is cleared with `pending` at every reward -- so an
        # index can never outlive the list it points into.
        tagged: list = []
        lasting = np.zeros((d, d)) if self.config.consolidation else None
        # Occupied slots, weakest first is not maintained -- the pool is small
        # enough that a linear scan for the weakest is cheaper than keeping order.
        # Each entry is (strength, retrieval, key), and `key` is kept here rather
        # than the token only because the model already holds it; a deployed node
        # would store the token and re-derive. The COST argument is about what a
        # node must keep, and that is the token; this is an implementation of the
        # same thing in a process that already has the key in hand.
        slots: list = []
        # A running estimate of this node's own typical surprise, so "unusual"
        # means unusual for it rather than against some global constant. Welford,
        # because a two-pass mean is not available to something that has to
        # decide as it goes.
        seen, mean_surprise, m2 = 0, 0.0, 0.0
        previous_key = None
        previous_retrieval = None
        predictions = np.zeros(len(tokens), dtype=np.int64)

        captured: tuple = ()
        for t, token in enumerate(tokens):
            captured = ()
            if not 0 <= token < self.config.vocab_size:
                raise ValueError(
                    f"token {token} outside vocab of {self.config.vocab_size}")
            if leave_at is not None and t == leave_at:
                # Permanent, and it takes what those rows were holding. Clearing
                # the memory rows rather than only silencing the vote is the
                # point: a departed machine does not keep answering quietly, and
                # what it had stored is not recoverable from the survivors.
                per_group = d // groups
                for node in leaving:
                    alive[node * per_group:(node + 1) * per_group] = 0.0
                memory *= alive[:, None]

            key = self.wk[token]
            value = self.wv[token] * alive

            # STORE: bind the previous token to this one. Doing this before the
            # retrieval below is what makes the association available later
            # without ever letting position t see position t+1 — the binding
            # written now is (t-1 → t), entirely in the past.
            # The fade, and whether a masked step gets one. Outside the write
            # guard when `decay_when_masked` is set, so selectivity can be
            # measured without the retention that has been riding along with it.
            if (self.config.decay_when_masked and self.config.decay < 1.0
                    and previous_key is not None
                    and not (store is None or store[t])):
                memory *= self.config.decay
                _fade(pending, self.config.decay)

            wrote = previous_key is not None and (store is None or store[t])
            if wrote:
                if self.config.decay < 1.0:
                    memory *= self.config.decay
                    _fade(pending, self.config.decay)
                memory += np.outer(value, previous_key)
                if self.config.reward_token >= 0:
                    # Held so it can be taken back out if no reward vouches for
                    # it. Two vectors per step, not a d x d matrix.
                    pending.append([1.0, value, previous_key])
                    if self.config.tag_slots:
                        # `previous_retrieval` is the retrieval made when the
                        # PREVIOUS token arrived -- the cue of the binding being
                        # written now -- which is exactly the quantity g9-04
                        # scored. It is already in hand, so a mark costs one norm
                        # and no extra state.
                        #
                        # At the sequence's first write the store is still empty,
                        # so that retrieval is zero and the write is tagged
                        # whatever it is. The signal is honestly uninformative
                        # there, and it holds one slot until the first reward
                        # clears the tag.
                        # THE MARK FADES. Note 010 took the shape of synaptic
                        # tagging from Lehr et al., where the tag is a decaying
                        # marker -- and a tag that does not fade ranks the whole
                        # interval at once, which prefers the writes made when
                        # the store was smallest rather than the writes that
                        # look like bindings. Fading a rank toward zero makes it
                        # losable, whichever end of the ranking wins, so this is
                        # one multiplication and no special case.
                        if self.config.tag_decay < 1.0:
                            tagged[:] = [(fade(rank, self.config.tag_decay),
                                          index) for rank, index in tagged]
                        tag(tagged, float(np.linalg.norm(previous_retrieval)),
                            len(pending) - 1, self.config.tag_slots,
                            self.config.tag_strongest)
                before = float(np.linalg.norm(memory))
                scale_to(memory, self.config.memory_cap)
                if self.config.memory_cap and before:
                    # The cap scales the store, so everything pending was scaled
                    # with it. Without this the subtraction below would remove
                    # more than is actually there.
                    _fade(pending, float(np.linalg.norm(memory)) / before)

            # THE REWARD GATE. A token in the input says the recent past
            # mattered. Everything written since the last reward and outside its
            # window is taken back out, so the fast store keeps only what
            # something vouched for -- which is the one thing the oracle does
            # (g8-03) and the one thing no previous mechanism attempted.
            #
            # The signal arrives AFTER the binding, so the decision cannot be
            # made at write time. That is the difficulty, not an inconvenience.
            if self.config.reward_token >= 0 and token == self.config.reward_token:
                if self.config.tag_slots:
                    # CAPTURE. What is still tagged survives; everything else
                    # written since the last reward comes back out. The tag chose
                    # its members from a local signal at write time, so this step
                    # adds no selectivity of its own -- it supplies the value.
                    protected = {index for _, index in tagged}
                else:
                    # The most recent `reward_window + 1` writes, by recency and
                    # nothing else. g9-04 put recency at AUC 0.479, which is no
                    # information: a window's only virtue was ever REACHING the
                    # binding, never selecting it.
                    keep = self.config.reward_window + 1
                    protected = set(
                        range(max(0, len(pending) - keep), len(pending)))
                captured = tuple(sorted(protected & set(range(len(pending)))))
                for index, entry in enumerate(pending):
                    if index not in protected:
                        weight, value_written, key_written = entry
                        memory -= weight * np.outer(value_written, key_written)
                pending.clear()
                tagged.clear()

            # CONSOLIDATE. The prediction made one step ago was a guess at the
            # token that has just arrived, so this is where it gets marked right
            # or wrong -- self-supervised, local, and available to any node.
            #
            # What gets promoted is the retrieved vector itself rather than the
            # binding that produced it. A superposed memory cannot name which of
            # its bindings answered; it can only be asked again and told whether
            # the answer held up. Promoting the answer is the operation that is
            # actually available.
            # Surprise and its running estimate sit OUTSIDE the `lasting` guard
            # because they are properties of this step, not of consolidation --
            # `seen`, `mean_surprise` and `m2` are read only below, so hoisting
            # them changes nothing and lets a probe watch a plain run. Leaving
            # them inside would have made every traced number a number about a
            # model with a lasting store, which is not the model being gated.
            if previous_retrieval is not None:
                # Surprise: how far the arriving token was from what was
                # predicted, as a probability-free magnitude. Available to the
                # node from its own last output and its own next input.
                # Scale-free, and it has to be. The obvious measure -- the
                # margin between the best score and the arriving token's -- grows
                # with the SIZE of the scores, so a memory that is filling up
                # reads as steadily more surprised even while its predictions
                # improve. Measured on a repeating cycle, margin surprise ROSE
                # 266% over eight repeats where it should have fallen.
                #
                # The negative log of the normalised score falls with repetition,
                # which is what surprise is supposed to do. Caught by John asking
                # why a repeated pattern was not becoming less surprising.
                step_surprise = surprise(previous_scores, token)

                seen += 1
                delta = step_surprise - mean_surprise
                mean_surprise += delta / seen
                m2 += delta * (step_surprise - mean_surprise)
                deviation = (m2 / seen) ** 0.5 if seen > 1 else 0.0

                if trace is not None:
                    trace.append({
                        "t": t,
                        "token": int(token),
                        "surprise": float(step_surprise),
                        "mean": float(mean_surprise),
                        "deviation": float(deviation),
                        # What a capture rule would rank on, so the probe ranks
                        # on the same number the mechanism would.
                        "strength": float(np.linalg.norm(previous_retrieval)),
                        # Predict the future and compare, in its literal form:
                        # did the guess made one step ago name the token that
                        # arrived. This is what `consolidate-on-use` fires on and
                        # it is NOT the same quantity as surprise -- one is a
                        # binary hit on the argmax, the other a continuous
                        # measure over the whole prediction. A signal can carry
                        # information in either without the other.
                        "hit": bool(predictions[t - 1] == token),
                        # Which writes a capture kept at this step, as indices
                        # into the pending list, empty on every step that is not
                        # a reward -- and where this step's own write landed in
                        # that list, -1 when it did not write. Together they say
                        # WHICH STEPS SURVIVED, which is the claim the gate
                        # makes. Scoring the gate by its accuracy instead is the
                        # downstream proxy CLAUDE.md rule 2 is about, and it
                        # cannot tell a tag holding bindings from a tag holding
                        # the first four writes after every reward.
                        "captured": captured,
                        "write_index": len(pending) - 1 if wrote else -1,
                    })

            if lasting is not None and previous_retrieval is not None:
                if not self.config.salience:
                    fires = predictions[t - 1] == token
                else:
                    # Both tails. Very wrong and very right are both worth
                    # keeping; the unremarkable middle is not.
                    fires = (deviation > 0.0
                             and abs(step_surprise - mean_surprise)
                             > self.config.salience * deviation)
                if fires and self.config.capture_slots:
                    # COMPETITIVE CAPTURE. Tagging decides who is a candidate;
                    # the pool decides who wins. Strength is the size of the
                    # trace being promoted, which is local, available and
                    # arm-agnostic -- the gate above already applied whatever
                    # selectivity it has, and this ranks among what it passed.
                    strength = float(np.linalg.norm(previous_retrieval))
                    contribution = self.config.consolidation * np.outer(
                        previous_retrieval, previous_key_for_retrieval)
                    index = admit([s for s, _ in slots], strength,
                                  self.config.capture_slots)
                    if index is not None:
                        if index < len(slots):
                            # Displacement, not addition. Subtracting the loser
                            # is what holds N at k -- without it this is a
                            # threshold gate with extra bookkeeping.
                            lasting -= slots[index][1]
                            slots[index] = (strength, contribution)
                        else:
                            slots.append((strength, contribution))
                        lasting += contribution
                elif fires:
                    lasting += self.config.consolidation * np.outer(
                        previous_retrieval, previous_key_for_retrieval)
                    scale_to(lasting, self.config.lasting_cap)

            # RETRIEVE, then read an answer off each group independently.
            # `parts` is (groups, vocab): row g is the complete prediction group
            # g makes from its own dimensions, owing nothing to any other group.
            retrieved = memory @ key if lasting is None else (memory + lasting) @ key
            sliced = retrieved.reshape(groups, -1)
            parts = np.einsum("vgd,gd->gv", self.grouped_wo, sliced)
            answer = parts.sum(0) if members is None else parts[members].sum(0)
            predictions[t] = int(answer.argmax())

            if learn and scored[t]:
                target = np.zeros(self.config.vocab_size)
                target[targets[t]] = 1.0
                # Each group's error is its OWN prediction error. With one group
                # this is the plain delta rule; with more, no group's update
                # reads any other group's activity, which is the whole point.
                self.grouped_wo += self.config.lr * np.einsum(
                    "gv,gd->vgd", target - parts, sliced)

            previous_key = key
            previous_retrieval = retrieved
            previous_key_for_retrieval = key
            previous_scores = answer
        return predictions
