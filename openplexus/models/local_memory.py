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

from openplexus.keys import KeySource, PairKeys, TableKeys
from openplexus.retrieval import Retrieval, build as build_retrieval

#: Newton-Schulz coefficients, from Keller Jordan's Muon. The quintic
#: `3.4445x - 4.7750x^3 + 2.0315x^5` pushes every singular value toward 1
#: without needing an SVD -- five iterations of matrix products, which is why
#: this is affordable per node.
_NS = (3.4445, -4.7750, 2.0315)
_NS_STEPS = 5


def _orthogonalise(matrix: np.ndarray) -> np.ndarray:
    """The nearest orthogonal matrix, approximately, via Newton-Schulz.

    Boeshertz et al. (arXiv:2606.11123) measured local feedback rules failing
    because their updates COLLAPSE IN RANK -- effective rank 12 where backprop
    reaches 100 -- and recovered CIFAR-100 from 1.4% to 46.1% with this plus
    normalisation. Note 035 measured our own store at effective rank ~3, which
    is the same disease at a different site.

    Orthogonalising spreads an update's magnitude evenly across its singular
    directions, so a direction the delta rule barely moved still gets a step.
    """
    scale = float(np.linalg.norm(matrix))
    if scale <= 0.0:
        return matrix
    # Iterating on the shorter side keeps the products small; the quintic acts
    # on the singular values either way.
    flip = matrix.shape[0] > matrix.shape[1]
    x = (matrix.T if flip else matrix) / (scale + 1e-7)
    a, b, c = _NS
    for _ in range(_NS_STEPS):
        gram = x @ x.T
        x = a * x + (b * gram + c * (gram @ gram)) @ x
    out = x.T if flip else x
    # Restore the original magnitude: orthogonalising is meant to change the
    # update's SHAPE, not its size, and letting it change both would confound
    # the measurement with a learning-rate change.
    return out * scale / (float(np.linalg.norm(out)) + 1e-12)


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
            **Setting this AND `reward_window` >= 1 runs the combined gate**,
            which protects the union of what each keeps. `reward_window` 0
            alongside a tag is TAG-ONLY, because 0 is also the default and every
            g9-05 to g9-07 cell was measured with the tag alone -- a default
            must not silently change an arm's identity. The cost is that "a tag
            plus a one-write window" cannot be expressed. They were mutually exclusive
            while each was being measured apart, which was right then and is
            exactly what a gate reading both signals has to change.
            [Note 023](../../docs/notes/023-two-signals-and-only-one-of-them-is-about-value.md)
            is the argument: weak retrieval says *this write is a binding* and
            recency says *this binding is the rewarded one*, because the reward
            token sits a fixed distance after the cue. Each mechanism has one
            answer and a gate needs both.

            The union keeps more, so it pays in interference -- retrieval
            goes as `sqrt(d / N)` -- and that is the whole question.

            **It CAN capture less than either arm alone, and that surprised
            me.** Within one interval the survivors really are the set union, and
            `tests/test_tag.py` pins that on a single-interval stream. Across
            intervals they are not: protecting more writes leaves a larger store,
            a larger store returns stronger retrievals, and the tag ranks on
            retrieval strength -- so the union's own marks diverge from the ones
            a tag-only run would have made. Measured at `slots` 8, `tag_decay`
            0.95, delay 20: the union captured the rewarded binding in 6 of 32
            captures where the tag alone managed 8.

            So this is a feedback loop between what a capture keeps and what the
            next interval marks, not a set operation, and "at least as good as
            both" is not available as an argument.
        tag_newest: Of the writes the tag marked, protect only this many of
            the most RECENT. 0 protects all of them, which is how every earlier
            result was measured.

            **This exists to measure a defect in the task, not as a proposal.**
            [Note 027](../../docs/notes/027-the-task-leaks-the-answer-through-its-layout.md)
            found that `reward_recall` lays bindings on a lattice of spacing 31
            and places each reward at most 20 steps after its cue, so **the
            nearest binding before a reward is always the rewarded one** --
            measured at 160 of 160. "Detect a binding, keep the most recent"
            therefore solves the task exactly, from local signals only.

            Setting this to 1 is that rule: the tag supplies binding-detection,
            and this takes the last of what it found. If the arm approaches the
            oracle, the leak is the whole story and the generator has to be
            fixed before any further result on this task means what it says. If
            binding-detection is too weak to exploit the leak, the fix is merely
            correct rather than urgent.

            Either way the number is wanted before anyone decides about
            re-baselining nine sweeps.
        tag_relative: Rank the tag on retrieval strength divided by the size
            of the store that produced it, instead of on the raw strength.

            **The raw strength confounds two things and one of them is not
            about the write.** A retrieval's magnitude scales with how much is
            in the store, so right after a capture -- when `memory` holds only
            what survived -- every retrieval is small. The weakest retrievals an
            un-faded tag will ever see are therefore the writes made just after
            the previous capture, and it fills with those:
            [g9-05](../../experiments/sweeps/g9-05-a-tag-that-fades.txt)
            measures `tag_decay` 1.0 at -0.18 to -0.22, worse than not gating at
            all, at every delay and both capacities.

            `tag_decay` hides that rather than fixing it. A fade releases old
            marks, so the cold-start writes are eventually displaced -- which is
            why fading rescues the mechanism, and also why the fade ends up
            doing two jobs and can be tuned for only one at a time. That is
            g9-05's whole result: the fade settings that are flat across delay
            are flat at zero, and the ones that score have become a window.

            Dividing by the store's norm is CLAUDE.md rule 7 in the direction it
            is usually not read: a criterion that fails to normalise against a
            quantity moving with it is blind in the same way as one that cancels
            its own input. It asks "is this retrieval weak FOR THIS STORE"
            rather than "is this retrieval small", which is what the signal was
            always supposed to mean.

            The divisor is the store's norm **at the moment the retrieval was
            made**, one step before the write being ranked -- not the norm now,
            which already contains that write. One number per step, local, and
            no time constant. Off by default, which reproduces every earlier
            result exactly.
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
        cache_slots: How many bindings to also keep EXACTLY, alongside the
            superposed store. 0 — the default — reproduces every earlier result.

            **This is the one lever four refutations have all pointed at.**
            Readout bias, competitive retrieval and orthogonal updates each
            failed for the same underlying reason: `r = M @ key` is a SUM, and
            no operation applied after a sum recovers the per-item information
            the sum destroyed. Note 035 measured the consequence — the store's
            effective rank is about 3 whatever its width.

            So the fix is not to read differently. It is **to stop summing some
            of it.** A bounded set of `(key, value)` pairs is held verbatim and
            read by similarity, which is where competition finally becomes
            possible: the entries exist separately, so a softmax over them
            selects rather than averages.

            **Admission is by `‖value − M @ key‖` — what the superposed store
            FAILED to absorb.** That is novelty times commitment, and it is two
            things at once: HOLA (arXiv:2607.02303) ablated it against the
            alternatives and found `β‖e‖` beats `‖e‖`, `β‖v‖` and recency, the
            last by 0.34 absolute at 32k context. And it is **synaptic tagging
            and capture** — the g9 line's mechanism, which this project already
            built from the biology. The policy came from biology and the
            structure from the literature, which is exactly the split John
            proposed.

            **Locality.** The cache is one node's own, holding its own slice of
            the value dimensions. The softmax runs over at most `cache_slots`
            node-resident entries: no barrier, no other node, nothing pooled.
            It is legal under the amended C1 and would have been legal under the
            original.

            **Why this is a hash table, and why that is now fine.** g10-07
            measured a plain hash table answering `reward_recall` perfectly
            where the store could not, and I recorded that as a threat. It was
            a signpost. The 2026 answer is that the compressed store and the
            exact cache should COEXIST, routed by what the compression lost —
            and eviction degrades rather than deletes, because an evicted
            binding is still in the superposed store.
        cache_sharpness: Inverse temperature on the cache read. Cosine
            similarities live in [-1, 1], so a softmax over them is nearly
            uniform without scaling — HOLA measured exactly this failure, where
            unit-norm keys gave ~3.5% weight per entry over 64 entries and
            retrieval degenerated into soft averaging. Rescaling was **the
            single largest design lever in their paper**, worth ~10 perplexity
            points. 8.0 is a starting point, not a measured optimum.
        cache_weight: How much the cache contributes to the retrieval, relative
            to the superposed store. 1.0 adds it at full strength.
        orthogonal_every: Accumulate this many readout updates, orthogonalise
            the sum, then apply it. 0 — the default — applies each update
            immediately and reproduces every earlier result exactly.

            **This is the one intervention in note 036 with a large measured
            effect elsewhere.** Boeshertz et al. (arXiv:2606.11123) found local
            feedback rules fail because their updates collapse in rank —
            effective rank 12 where backprop reaches 100 — and recovered
            CIFAR-100 ResNet-18 from 1.4% to 46.1% with Muon-style
            orthogonalisation plus normalisation. Note 035 measured our own
            store at effective rank ~3 at every width, which is the same disease
            at a different site.

            A single delta-rule step is `error ⊗ retrieval` — **rank one by
            construction**, so there is nothing to orthogonalise until several
            have been summed. That is what the window is for, and it is the
            cost: the update is no longer applied at every token.

            **Orthogonalisation is per GROUP.** A group owns a slice of the
            dimensions, so its node holds only `vocab × d/groups` of the matrix.
            Orthogonalising across groups would need every node's columns at
            once — a barrier, which the amended C1 still forbids. Whether a
            per-slice orthogonalisation buys what a whole-matrix one does is
            exactly what this flag exists to measure, and it is not obvious that
            it does.
        retrieval_steps: How many times to re-read the store per query. 1 — the
            default — is a single linear read and reproduces every earlier
            result exactly.

            **This is the one thing the capacity literature agrees on and we did
            not have.** `r = M @ key` is a weighted SUM: every stored value,
            weighted by how much its key overlaps this one. Nothing selects, so
            retrieval returns an average. That is precisely what separates linear
            associative capacity O(d) from a competitive read's O(e^{d/2}), and
            Xu et al. (arXiv:2602.01744) measure +15.5 points on hardest-case
            retrieval from reinstating competition on linear-attention baselines.

            A step maps the retrieval back through the store, renormalises, and
            reads again:

                back = M.T @ r ;  r = M @ (back / ||back|| · ||key||)

            Since `M = Σ value_i ⊗ key_i`, the returned weights become
            `⟨key_i, key⟩·⟨value_i, r⟩` — a PRODUCT of two similarities. A value
            matching on both key and content survives; one matching on neither
            fades. That is Hopfield settling, and it is why iterating sharpens
            rather than merely rescaling.

            **It is local, and it is not free.** A step re-reads the node's own
            store, so C1 holds — no barrier, no population statistic, no other
            node's contents. But `M.T @ r` sums over the VALUE dimensions where
            `M @ key` sums over the key dimensions, so a partitioned network
            needs a second pooling round per step. **Under C2 that doubles the
            communication per token**, which is the real price and is not visible
            in a single-process run.
        write_gate: How much of the corrective write to apply, in (0, 1]. Does
            nothing unless `corrective_writes` is on. **1.0 reproduces every
            earlier result and is the wrong default**, kept only for that reason.

            g11-01 measured corrective writes fixing rebinding completely
            (0.125 → 1.000 at decay 0.997) and COSTING capacity (0.70×), and we
            recorded that as a trade. The 2026 linear-attention literature says
            the delta rule is a strictly better estimator of the same object than
            the Hebbian outer product — not a trade — and the difference turned
            out to be this scalar. Every published delta-rule variant gates the
            correction; ours applied all of it.

            A full correction forces the store to reproduce `value` at `key`
            exactly, at this step. It gets there by editing every direction
            correlated with `key`, and with random keys **every other key is
            slightly correlated with it**. So a full correction is a small
            overwrite of everything else in the store, applied on every write.

            Measured at width 128, 8 seeds — rebinding accuracy, and the share of
            bindings still retrievable at a load of 256:

                gate     rebinding   capacity
                Hebbian      0.500      0.997
                0.25         0.922      0.986
                0.50         1.000      0.922
                0.75         1.000      0.768
                1.00         1.000      0.618

            **0.25 keeps 92% of the rebinding win for 1% of the capacity**, where
            the full correction pays 38%. The trade recorded in g11-01 was an
            artefact of the implementation, not a property of corrective storage.
        context_keys: Derive the key from the token PAIR `(t-1, t)` rather than
            from token `t` alone. Off by default, which reproduces every earlier
            result exactly. Requires `derived_keys`.

            **This lifts a ceiling that [note 033](../../docs/notes/033-the-architecture-pass.md)
            proved was there.** With a per-token key the write rule binds
            `value(t)` to `key(t-1)`, so a retrieval is the sum of the values of
            every token that has ever followed this one — a bigram count table in
            superposition. Measured: cosine 0.9455 against exactly that table at
            low load, falling to 0.88 as items superpose, which is the
            interference signature. **Nothing in that architecture can represent
            a trigram, because no trigram is ever written down**, so "beat a
            bigram" was the model's ceiling rather than its target.

            With a pair key, `previous_key` is the key of `(t-2, t-1)` and the
            query at `t` is the key of `(t-1, t)`, so the same three lines make
            the retrieval a *trigram* count vector. Nothing else in `run` changes.

            **It is not free, and the price is measured.** The number of distinct
            keys goes from `vocab` to the number of pairs that actually occur —
            469 in 4000 characters of Shakespeare against 4356 possible, so real
            text is far kinder than uniform tokens, but it is still seven times
            more keys. Since capacity goes as `sqrt(d/N)` (note 020), the store
            fills sooner: the cosine against a trigram table plateaus near 0.53
            where the bigram version held 0.88. **Whether the higher ceiling is
            worth the lower signal-to-noise is a bits-per-character question that
            a cosine cannot answer**, which is why this is a flag and not a
            change of default.

            The derivation is per pair, from `(seed, t-1, t)`, cached rather than
            tabulated: a `vocab^2` table would be 16 million rows at `vocab
            4096`, and not storing it is the whole point. Position 0 uses
            `vocab_size` as a start-of-sequence token so every key lives in one
            space.
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
    tag_relative: bool = False
    tag_newest: int = 0
    tag_strongest: bool = False
    memory_cap: float = 0.0
    consolidation: float = 0.0
    capture_slots: int = 0
    salience: float = 0.0
    lasting_cap: float = 0.0
    corrective_writes: bool = False
    write_gate: float = 1.0
    retrieval_steps: int = 1
    orthogonal_every: int = 0
    #: Write the LEARNED readout row as the value, instead of a frozen draw.
    #:
    #: The store is rebuilt every chunk and `Wk`/`Wv` are never updated, so
    #: `Wo` is the only thing that learns across a corpus -- and the model
    #: converges at about 16,000 characters (decision 63). This is the cheapest
    #: test of whether a LEARNED value projection moves that, and it adds no
    #: parameters: `Wo` and the value projection become one matrix.
    value_from_readout: bool = False
    #: Learning rate for the value projection. 0 leaves `Wv` frozen.
    #:
    #: **This is the one that adds persistent capacity.** `Wv` is `vocab x d`
    #: of frozen random numbers; training it doubles what the model carries
    #: across a corpus, where `value_from_readout` merely merged `Wv` into `Wo`
    #: and was refuted (decision 64). Whether the saturation point at 16,000
    #: characters moves is the measurement that separates decision 59's
    #: explanation from decision 62's.
    value_lr: float = 0.0
    #: Keep the store between `run` calls instead of resetting it.
    #:
    #: Correct only when consecutive calls are consecutive TEXT. On the recall
    #: tasks each sequence is independent and this would let the model answer
    #: from the training set -- which is what
    #: `local-memory-persists-across-sequences` exists to catch.
    carry_store: bool = False
    cache_slots: int = 0
    cache_sharpness: float = 8.0
    cache_weight: float = 1.0
    readout_bias: bool = False
    derived_keys: bool = False
    context_keys: bool = False
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
        if self.value_lr < 0.0:
            raise ValueError("value_lr is a learning rate and cannot be "
                             "negative; 0 leaves the projection frozen")
        if self.value_lr and self.value_from_readout:
            raise ValueError(
                "value_from_readout writes Wo as the value, so training Wv "
                "would train a matrix nothing reads -- a mechanism that runs "
                "and does nothing, which is the failure this repo is built "
                "against")
        if self.cache_slots < 0:
            raise ValueError(
                f"cache_slots is a number of exact bindings and cannot be "
                f"negative; got {self.cache_slots}. 0 disables the cache")
        if self.cache_slots and self.cache_sharpness <= 0.0:
            raise ValueError(
                "cache_sharpness is an inverse temperature and must be "
                "positive; at or below zero the read is uniform or inverted, "
                "which is the soft-averaging failure the cache exists to avoid")
        if self.orthogonal_every < 0:
            raise ValueError(
                f"orthogonal_every is a window length and cannot be negative; "
                f"got {self.orthogonal_every}. 0 disables it")
        if self.retrieval_steps < 1:
            raise ValueError(
                f"retrieval_steps is how many times the store is read and must "
                f"be at least 1; got {self.retrieval_steps}. Zero would mean "
                f"answering without reading the memory at all")
        if not 0.0 < self.write_gate <= 1.0:
            raise ValueError(
                f"write_gate is a fraction of the correction and must be in "
                f"(0, 1]; got {self.write_gate}. Above 1 overshoots the target "
                f"and the store oscillates; at or below 0 nothing is written")
        if self.context_keys and not self.derived_keys:
            raise ValueError(
                "context_keys derives a key from a token PAIR and rests on the "
                "same argument as derived_keys, which must be on; a stored "
                "vocab^2 table is what the derivation exists to avoid")
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
        if not 0.0 < self.tag_decay <= 1.0:
            raise ValueError("tag_decay must be in (0, 1]")
        if self.tag_decay < 1.0 and not self.tag_slots:
            raise ValueError(
                "tag_decay fades marks and does nothing without tag_slots")
        if self.tag_newest < 0:
            raise ValueError("tag_newest must not be negative")
        if self.tag_newest and not self.tag_slots:
            raise ValueError(
                "tag_newest narrows what the tag marked and does nothing "
                "without tag_slots")
        if self.tag_newest > self.tag_slots:
            raise ValueError(
                f"tag_newest {self.tag_newest} exceeds tag_slots "
                f"{self.tag_slots}, so it would narrow nothing")
        if self.tag_relative and not self.tag_slots:
            raise ValueError(
                "tag_relative changes how the tag ranks and does nothing "
                "without tag_slots")
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
        # WHERE A KEY COMES FROM IS A REPLACEABLE COMPONENT.
        #
        # `openplexus/keys.py` holds the seam and says why. Assign a different
        # `KeySource` after construction and `run` uses it -- no config flag, no
        # branch here, no edit to any experiment script. That is the point:
        # keys have been varied twice already and each variation cost a flag
        # threaded through the whole tree, which is the tax that stops a third
        # idea being tried.
        self.key_source: KeySource = (
            PairKeys(config.seed, spread, d, config.vocab_size)
            if config.context_keys else TableKeys(self.wk))
        # HOW THE STORE IS READ IS A REPLACEABLE COMPONENT, for the same reason
        # and with more at stake. `openplexus/retrieval.py` holds the seam.
        #
        # `r = M @ key` is a SUM, and it is the common cause of every mechanism
        # this project has refuted. g11-05 then measured the consequence: 16x
        # the training text buys 0.012 bits against a backprop control that
        # moves cleanly. **A suspect you cannot swap out is a suspect you cannot
        # test**, so the sum, the exact cache and the settling loop are three
        # composed objects rather than four config fields and two branches.
        self.retrieval: Retrieval = build_retrieval(config)
        self.wv = rng.normal(0.0, spread, (v, d))
        self.wo = np.zeros((v, d))
        # A constant term on the readout, off by default.
        #
        # Without it `answer` is a pure matrix product, so a near-zero retrieval
        # gives all-zero scores and a uniform distribution. **A unigram is
        # exactly a bias over tokens**, which is why the model scoring below one
        # (g10-12) is not a mystery: it had no way to express a prior.
        #
        # Updated by each output unit's own prediction error, which is the same
        # locality the delta rule beside it already has.
        self.bias = np.zeros(v)
        # A view, not a copy: writes through `grouped_wo` land in `wo`, so the
        # readout has exactly one representation and `ablate` keeps working
        # unchanged. Reshaping a freshly allocated C-contiguous array never
        # copies, and the assertion says so rather than trusting it.
        self.grouped_wo = self.wo.reshape(v, config.partitions, -1)
        assert self.grouped_wo.base is self.wo, "grouped_wo must alias wo"
        #: Held-back readout updates, when `orthogonal_every` is on.
        self.pending_update = np.zeros_like(self.grouped_wo)
        self.since_orthogonal = 0
        #: The store carried between runs when `carry_store` is on, else None.
        self._carried = None

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

    def _apply_orthogonal(self) -> None:
        """Orthogonalise each group's accumulated update, then apply it.

        **Per group, not across groups.** A group owns a slice of the
        dimensions, so `pending_update[:, g, :]` is the only part of the matrix
        its node holds. Orthogonalising the whole `vocab x d` matrix would need
        every group's columns at once, which is the barrier the amended C1 still
        forbids. Whether a per-slice orthogonalisation buys what a whole-matrix
        one does is exactly what this flag exists to measure.
        """
        for group in range(self.config.partitions):
            self.grouped_wo[:, group, :] += (
                self.config.lr * _orthogonalise(self.pending_update[:, group, :]))
        self.pending_update[:] = 0.0
        self.since_orthogonal = 0

    def context_key(self, previous: int, token: int) -> np.ndarray:
        """The key for a token PAIR, derived from `(seed, previous, token)`.

        The same argument as `derived_keys`, one token wider: a node handed two
        token ids can rebuild this vector without holding any table, and the
        table it would otherwise hold has `vocab^2` rows.

        `previous` is `vocab_size` at the start of a sequence, so the first step
        has a key in the same space as every other step rather than a special
        case that would have to be excluded from the store.
        """
        if not self.config.context_keys:
            raise ValueError(
                "context_key is only meaningful with context_keys set; without "
                "it the model binds single tokens and this vector is in no key "
                "space the store uses")
        return self.key_source.pair(previous, token)

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
        # CARRYING THE STORE BETWEEN SEQUENCES.
        #
        # The reset above is deliberate and is guarded by
        # `local-memory-persists-across-sequences`: on MQAR and reward_recall
        # each sequence is independent, and a store that accumulated across
        # them would be answering from the training set rather than from the
        # sequence in front of it.
        #
        # **A corpus is not that.** Chunk 41 of Shakespeare continues chunk 40,
        # and resetting between them gives the model a memory 128 characters
        # long -- a limit inherited from the synthetic tasks and never chosen
        # for text. Off by default, because every earlier number was measured
        # with the reset in place, and it must stay off for the recall tasks.
        if self.config.carry_store and self._carried is not None:
            memory = self._carried
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
        # The exact cache, per sequence like the fast store it sits beside. A
        # score of 0 marks a slot never filled, which is why admission compares
        # against `argmin` rather than tracking a count.
        # NOT named `slots` -- that is the capture-slot list a few lines above,
        # and shadowing it silently disabled the reward gate.
        # The retrieval strategy owns whatever per-run state it needs -- the
        # exact cache's entries live in it, not here. `openplexus/retrieval.py`
        # holds the seam and says why. Assign a different strategy to
        # `self.retrieval` and nothing in this method changes.
        self.retrieval.begin(d)
        previous_key = None
        previous_retrieval = None
        # The size of the store that produced `previous_retrieval`, so a
        # relative tag divides by what the retrieval could have returned rather
        # than by a store that already contains the write being ranked. Computed
        # only when something reads it -- it is a d x d norm every step.
        previous_store_size = 0.0
        predictions = np.zeros(len(tokens), dtype=np.int64)

        captured: tuple = ()
        pending_now: tuple = ()
        for t, token in enumerate(tokens):
            captured = pending_now = ()
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

            # The single point where a key enters the model. `previous_key`
            # below is simply this key one step back, so a key source that binds
            # a pair makes the store a trigram table without touching the write,
            # the retrieval or the readout.
            key = self.key_source.key(tokens, t)
            # THE VALUE PROJECTION, FROZEN OR LEARNED.
            #
            # `Wv` is drawn once and never updated, so with the store rebuilt
            # every chunk the ONLY thing this model learns across a corpus is
            # `Wo` -- one `vocab x d` linear map (decision 62). `value_from_readout`
            # writes the learned readout row instead of the frozen draw, which
            # costs no extra state and makes what is stored track what the
            # readout has learned to want.
            #
            # Off by default: it changes every stored value, so every earlier
            # number is measured without it.
            value = (self.wo[token] if self.config.value_from_readout
                     else self.wv[token]) * alive

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
            # Captured here rather than read off `pending` in the trace block:
            # the reward gate below empties `pending`, so by then a write made
            # at a capture step reports -1 and is invisible to every probe. That
            # is silent when a capture keeps thirty writes and fatal when it
            # keeps one.
            wrote_at = -1
            if wrote:
                # Told BEFORE the write, so a strategy scoring novelty measures
                # what the store knew rather than what it is about to be told.
                self.retrieval.observe(memory, previous_key, value,
                                       self.config.write_gate)
            if wrote:
                if self.config.decay < 1.0:
                    memory *= self.config.decay
                    _fade(pending, self.config.decay)
                if self.config.corrective_writes:
                    # THE DELTA RULE FOR STORAGE, rather than the Hebbian one.
                    #
                    # Hebbian storage adds `outer(value, key)` whatever the
                    # store already holds, so rebinding a key ACCUMULATES: the
                    # old value is still in there and retrieval returns their
                    # sum. g10-11 measured that as 0.0x chance after 512
                    # rebindings of 8 cues.
                    #
                    # Subtracting what the key currently retrieves stores only
                    # the ERROR, so `memory @ key` lands on `value` rather than
                    # on `value` plus whatever was there. Rebinding replaces.
                    #
                    # **It is local.** The correction needs the node's own store
                    # and the key it is writing under, and nothing else -- no
                    # population statistic, no barrier, no other node. C1 holds.
                    #
                    # The division is by the key's own squared norm, which makes
                    # a FULL correction exact for that key: the binding lands on
                    # `value` in one step instead of asymptotically.
                    #
                    # `write_gate` is how much of that correction to apply, and
                    # 1.0 -- landing exactly -- turns out to be the wrong
                    # default. See the field's documentation: a full correction
                    # edits every direction correlated with this key, and with
                    # random keys that is every other binding in the store.
                    scale = float(previous_key @ previous_key)
                    if scale > 0.0:
                        error = value - memory @ previous_key
                        memory += (self.config.write_gate
                                   * np.outer(error, previous_key) / scale)
                else:
                    memory += np.outer(value, previous_key)
                if self.config.reward_token >= 0:
                    # Held so it can be taken back out if no reward vouches for
                    # it. Two vectors per step, not a d x d matrix.
                    pending.append([1.0, value, previous_key])
                    wrote_at = len(pending) - 1
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
                        strength = float(np.linalg.norm(previous_retrieval))
                        if self.config.tag_relative and previous_store_size:
                            strength /= previous_store_size
                        tag(tagged, strength, len(pending) - 1,
                            self.config.tag_slots, self.config.tag_strongest)
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
                # CAPTURE. Two ways to choose what survives, and with both
                # set the survivors are the UNION -- a write is kept if either
                # mechanism claimed it.
                #
                # The tag chose its members from a local signal at write time,
                # so it adds no selectivity here; the reward supplies the value.
                # The window ranks on recency, which g9-04 put at AUC 0.479 for
                # telling a binding from filler -- no information -- and which
                # is nonetheless almost all the information about which binding
                # was REWARDED, since the token sits a fixed distance after the
                # cue. Note 023.
                protected: set = set()
                if self.config.tag_slots:
                    marked = sorted(index for _, index in tagged)
                    # The LAST of what the tag found, not the best of it.
                    # A higher pending index is a later write, so this is
                    # recency among candidates rather than rank among them.
                    #
                    # The write made AT this step binds the previous token to
                    # the reward token, and a reward does not vouch for the
                    # write that carried it. Note 027's rule is the most recent
                    # binding BEFORE the reward, so it is dropped -- which the
                    # node can do without knowing anything it does not already
                    # know, since it has just seen the reward token itself.
                    if self.config.tag_newest:
                        marked = [i for i in marked if i != wrote_at]
                        marked = marked[-self.config.tag_newest:]
                    protected |= set(marked)
                if self.config.reward_window or not self.config.tag_slots:
                    # A window of 0 keeps the write at the reward step itself,
                    # which is why this arm runs whenever there is no tag --
                    # `reward_window` defaulting to 0 is still a gate.
                    keep = self.config.reward_window + 1
                    protected |= set(
                        range(max(0, len(pending) - keep), len(pending)))
                captured = tuple(sorted(protected & set(range(len(pending)))))
                # OBSERVATION ONLY, like `captured` and `write_index`. Nothing
                # below reads it and no gate consults it.
                #
                # At a capture step every quantity the trace already carries is
                # a property of the STEP -- surprise, strength, the running mean
                # -- so it is identical for every candidate and cannot rank
                # them. The only candidate-specific things available are what
                # was recorded when the write happened, and how long ago that
                # was. This is the one exception: a node holds `pending`, so it
                # can ask its own store what each pending key retrieves NOW, and
                # that is a different number per candidate.
                #
                # Whether it says anything the AGE does not already say is the
                # open question g9-13 asks. It is recorded here rather than
                # asserted either way.
                if trace is not None:
                    pending_now = tuple(
                        float(np.linalg.norm(memory @ key_written))
                        for _, _, key_written in pending)
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
                        "write_index": wrote_at,
                        # The prediction itself, over the whole vocabulary, as
                        # it stood before this token arrived. OBSERVATION ONLY.
                        #
                        # `surprise` above is already the negative log of this
                        # distribution at the arriving token, so nothing here
                        # needs it -- but a CALIBRATED cross-entropy does, since
                        # fitting a temperature means rescaling the scores and
                        # a scalar summary cannot be rescaled after the fact.
                        # Copied because the array is reused each step.
                        "scores": previous_scores.copy(),
                        # What each pending write's key retrieves from the store
                        # AS IT STANDS at this step, one number per candidate,
                        # empty on every step that is not a capture. See the
                        # comment at the capture site: it is the only
                        # candidate-specific signal that is not fixed at write
                        # time, and so the only new place a delay-agnostic gate
                        # could find WHICH binding was rewarded.
                        "pending_now": pending_now,
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
            # NOT named `store` -- that is the write mask parameter, and
            # shadowing it silently turned `if wrote:` into an array test.
            readable = memory if lasting is None else memory + lasting
            retrieved = self.retrieval.read(readable, key)
            sliced = retrieved.reshape(groups, -1)
            parts = np.einsum("vgd,gd->gv", self.grouped_wo, sliced)
            answer = parts.sum(0) if members is None else parts[members].sum(0)
            if self.config.readout_bias:
                answer = answer + self.bias
            predictions[t] = int(answer.argmax())

            if learn and scored[t]:
                target = np.zeros(self.config.vocab_size)
                target[targets[t]] = 1.0
                # Each group's error is its OWN prediction error. With one group
                # this is the plain delta rule; with more, no group's update
                # reads any other group's activity, which is the whole point.
                update = np.einsum("gv,gd->vgd", target - parts, sliced)
                if self.config.value_lr:
                    # UNFREEZING THE VALUE PROJECTION -- the real version.
                    #
                    # `Wv` has always been drawn once and never updated, so with
                    # the store rebuilt every chunk, `Wo` was the ONLY thing
                    # learning across a corpus: one linear map, converged by
                    # 16,000 characters (decisions 62, 63). This ADDS persistent
                    # parameters rather than re-using the readout's, which is
                    # what `value_from_readout` did and why that was refuted.
                    #
                    # The rule is the readout's own error carried one step back.
                    # A retrieval should land on the target's value, so
                    #
                    #     dL/d(Wv[target]) = Wo^T (p - y)
                    #
                    # and group g's share of it uses only group g's readout
                    # columns and group g's prediction error -- the same
                    # locality the delta rule beside it already has, and no
                    # wider than the readout update above.
                    self.wv[targets[t]] += self.config.value_lr * np.einsum(
                        "gv,vgd->gd", target - parts,
                        self.grouped_wo).reshape(-1) * alive
                if self.config.orthogonal_every:
                    # Hold the update back and orthogonalise a batch of them.
                    # See `orthogonal_every`: the point is the RANK of what gets
                    # applied, and a single step's update is rank one by
                    # construction, so there is nothing to orthogonalise until
                    # several have been summed.
                    self.pending_update += update
                    self.since_orthogonal += 1
                    if self.since_orthogonal >= self.config.orthogonal_every:
                        self._apply_orthogonal()
                else:
                    self.grouped_wo += self.config.lr * update
                if self.config.readout_bias:
                    # The whole prediction's error, not one group's: the bias is
                    # a single shared constant rather than a per-group weight,
                    # so there is no group whose own error it could use.
                    self.bias += self.config.lr * (target - answer)

            previous_key = key
            previous_retrieval = retrieved
            if self.config.tag_relative:
                previous_store_size = float(np.linalg.norm(memory))
            previous_key_for_retrieval = key
            previous_scores = answer
        if self.config.carry_store:
            self._carried = memory
        return predictions
