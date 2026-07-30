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

from openplexus.concepts import OneConceptPerToken, Surfaces
from openplexus.keys import KeySource, PairKeys, TableKeys
from openplexus.partitioned import ConceptStore
from openplexus.retrieval import Retrieval, build as build_retrieval
from openplexus.sketch import AddressSketch, SumSketch
from openplexus.search import (beam as run_beam,
                               candidates as search_candidates,
                               decode_margin as search_margin,
                               search as run_search)

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


def _margin(scores: np.ndarray) -> float:
    """How far the best answer beats the second. Decision 130's signal.

    Not the magnitude of anything: a large retrieval read from the wrong
    address decodes to a confident-looking vector whose top two are close,
    where a small one read from the right address separates cleanly.
    """
    if scores.size < 2:
        return 0.0
    top = np.partition(scores, -2)[-2:]
    return float(top[1] - top[0])


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

    #: Does the consolidated store SURVIVE the sequence? **`False` is what every
    #: number in this project was measured under**, and it is the thing decision
    #: 62 found and nobody acted on.
    #:
    #: `memory` is rebuilt inside `run` and so, until now, was `lasting` -- so
    #: consolidation's two timescales both sat INSIDE one sequence and the only
    #: thing carrying across a corpus was `Wo`, one `vocab x d` linear map.
    #:
    #: GOALS section 1.2 asks for *"a good map of most all concepts, and how a
    #: given concept relates to some other concept"*. **A map needs somewhere to
    #: live**, and this is the smallest change that gives it one: the same
    #: machinery, promoted from per-sequence to per-model.
    #:
    #: **The falsifier is decision 63**, which measured this model converging at
    #: ~16,000 characters and never improving after. If a persistent slow store
    #: does not move that wall, the account in note 042 is wrong and the whole
    #: architectural proposal goes with it. That is a cheap test and it comes
    #: before anything is built on top.
    #:
    #: Requires `consolidation` -- there is nothing to persist without a
    #: mechanism that promotes into it.
    persistent_lasting: bool = False

    #: How much of the slow store survives each SEQUENCE. 1.0 is no forgetting,
    #: which is what every run before g15-01 used.
    #:
    #: **The slow store never decayed, and that is why persistence saturated.**
    #: `memory *= decay` brakes the fast store every step; `lasting` has only
    #: `+=`. So it accumulates monotonically forever, and g15-01 measured its
    #: norm pinned at EXACTLY the cap for every cap tried -- 5, 50, 500 and
    #: 1e9 -- because it grew past each one and was clipped back. At 1e9 the
    #: readout overflowed to NaN.
    #:
    #: A finite cap therefore does not bound a growing store, it *saturates*
    #: one: the magnitude is constant by construction and only the direction
    #: moves, dominated by whatever was written most recently and largest.
    #:
    #: Zenke & Gerstner (2017) -- the paper `lasting_cap` came from -- is titled
    #: *Hebbian plasticity requires compensatory processes on MULTIPLE
    #: timescales*, and this project had implemented one. Note 018 recorded the
    #: same defect in the FAST store; this is its mirror.
    #:
    #: Per SEQUENCE rather than per step, because that is the slow store's
    #: timescale. At 0.99 a contribution is halved after ~69 sequences.
    lasting_decay: float = 1.0
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

    #: Subtract the mean value vector after each `value_lr` update.
    #:
    #: `value_lr` is the correct gradient and it still collapses the
    #: representation rather than organising it (decision 94): every target
    #: moves toward a direction `Wo` chooses, so they all move the same way, and
    #: at `value_lr=0.05` the cosine among ordinary tokens reached 0.382 while
    #: accuracy fell to 0.025.
    #:
    #: Centring removes the component every value shares, which is what drift
    #: accumulates. It does not prevent two tokens converging for a reason —
    #: that is the representation learning this exists for.
    value_centre: bool = False
    #: Keep the store between `run` calls instead of resetting it.
    #:
    #: Correct only when consecutive calls are consecutive TEXT. On the recall
    #: tasks each sequence is independent and this would let the model answer
    #: from the training set -- which is what
    #: `local-memory-persists-across-sequences` exists to catch.
    carry_store: bool = False
    #: Units in a per-group hidden layer on the readout. 0 keeps it linear.
    #:
    #: The readout was the ceiling (note 037, decision 70): a linear map cannot
    #: extract what the retrieval carries, and a two-layer one recovers 0.63
    #: bits prequentially in the deployed regime. Trained by backpropagation
    #: THROUGH THE GROUP'S OWN TWO MATRICES, which is the same locality the
    #: delta rule beside it already has.
    hidden: int = 0
    #: Retrievals chained before the readout reads. 1 is the current behaviour.
    #:
    #: The model performs exactly ONE hop and stops: on the chain task at two
    #: hops it answers the intermediate 100% of the time (decision 83). Each
    #: extra hop decodes the retrieval to a token distribution and re-encodes it
    #: as a key, which needs no new parameters.
    hops: int = 1

    #: How sharply a hop's decode is read before it is re-encoded as a key.
    #:
    #: Applied to logits standardised to unit spread, so it means the same
    #: thing regardless of `key_scale`, `d_model`, `decay` or `memory_cap`. 0
    #: is a uniform decode -- which is what the unsharpened hop was doing by
    #: accident -- and large approaches argmax. Only read when `hops > 1`.
    hop_sharpness: float = 6.0

    #: Which matrix turns a hop's retrieval back into a token distribution.
    #:
    #: ``"encoder"`` uses the transpose of the frozen `Wv`; ``"readout"`` reuses
    #: the learned `Wo`. Only read when `hops > 1`.
    hop_decoder: str = "encoder"

    #: Learn WHICH hop to read from, instead of always reading the last one.
    #:
    #: With this off, `hops` is an exact depth and must match the question --
    #: overshoot scores 0.000 in every direction (decision 85). With it on,
    #: `hops` is a MAXIMUM and each group learns a linear score over its own
    #: slice of each hop, softmaxed across hops. Adds one vector per group.
    #:
    #: Refused together with `hidden`: mixing retrievals and mixing predictions
    #: are the same thing only through a linear readout.
    halt_gate: bool = False

    #: Gain on the gate's scores before the softmax over hops.
    #:
    #: Without it the gate is INERT: the learned vector stays far smaller than
    #: the retrievals it scores, so the softmax is a flat average and the
    #: readout simply learns to live with the blend. Measured at gain 1, the
    #: gate put 0.5020 on hop 1 for depth-1 questions and 0.5000 for depth-2 --
    #: the right direction, and 0.2% of the way there. Only read when
    #: `halt_gate` is on.
    gate_sharpness: float = 1.0

    #: Let the gate see WHERE it is, not only what it retrieved.
    #:
    #: Decision 95 measured the one-rule gate as conflicted rather than
    #: outvoted: 0.0171 on hop 1 at a query, which is right, and 0.4712 in the
    #: body, where serving the body needs ~1.0. A score on the lookahead alone
    #: cannot separate the two cases because the lookahead can look the same.
    #:
    #: With this on, the current key blends a second rule in — it MODULATES the
    #: rule rather than adding to the score, because an added key term is
    #: identical across hops and the softmax would remove it exactly.
    gate_reads_key: bool = False

    #: What the gate learns from. ``"mixture"`` is the readout's error carried
    #: back through the weighted sum; ``"which_hop"`` is a separate objective.
    #:
    #: The mixture objective AVERAGES conflicting demands. In the body the next
    #: token is one hop away and the error says "take hop 1"; at a query it is
    #: several and says "take a later one". One shared vector pulled by both
    #: drifts toward whichever supplies more gradient, which is why composition
    #: is learned and then unlearned (decisions 96, 97) — and neither more
    #: inputs nor more density stops it.
    #:
    #: ``"which_hop"`` asks a question that has the SAME answer in both places:
    #: *which hop would have been right here?* At a scored position that label
    #: is locally available — each hop's own readout either names the target or
    #: does not — so the body stops outvoting the query and merely supplies more
    #: examples of one class.
    gate_objective: str = "mixture"

    #: What a hop does with the retrieval it already had.
    #:
    #: ``"replace"`` keeps only the newest, which is what every earlier result
    #: used. It is right for following a pointer -- a transitive chain only ever
    #: needs where you landed -- and decision 101 measured it failing on typed
    #: relations for a structural reason: composing `R1` with `R2` needs BOTH
    #: held at once, and replacing leaves nowhere for `R1` to be.
    #:
    #: ``"concat"`` gives the readout every hop's retrieval side by side.
    #: ``"bind"`` multiplies them elementwise.
    #:
    #: **`concat` was expected to fail and does not.** The argument against it
    #: was that a linear readout over `[r1, r2]` can only learn `f(r1) + g(r2)`,
    #: and composition is not additive. That is true of the functional FORM and
    #: irrelevant to the task: fitting a linear map from the bound pair to the
    #: answer over the whole rule table gives
    #:
    #:     product   0.812      concat   1.000      convolve   0.812
    #:
    #: because sixteen rules in a space this wide are linearly separable
    #: whatever structure the labels have. The multiplicative bindings LOSE
    #: information -- a product of two random vectors does not keep its
    #: operands recoverable -- and score worse than the option that keeps both.
    #:
    #: `bind` is kept as the measured alternative, not as a fallback. Whether
    #: concat still wins with far more rules than sixteen is a scale question
    #: and is not settled here.
    hop_accumulate: str = "replace"

    #: How many candidate branches to try at an answerable position.
    #:
    #: **0 is OFF** and every number this project recorded before decision 123
    #: was measured with it off. **1 is a GREEDY WALK** -- pair-key traversal
    #: committing to the single best candidate, which is decision 107's
    #: mechanism without the search on top. That is the control that says
    #: whether SEARCHING bought anything, as opposed to traversal buying it, and
    #: it is the reason 0 rather than 1 means off.
    #:
    #: Search is the capability decision 108 named as missing: *"multi-hop
    #: reasoning over a BRANCHING graph requires SEARCH -- try a branch, see
    #: where it lands, backtrack -- and an associative store does RETRIEVAL."*
    #:
    #: It REPLACES the hop loop rather than sitting beside it. A hop decodes a
    #: retrieval and re-encodes it through `Wk`; a walk commits to a token and
    #: keys on `(entity, relation)` pairs directly. Those are different
    #: mechanisms and running both would re-encode pointlessly, so the hop loop
    #: is skipped when this is on. See `openplexus/search.py`.
    search_branches: int = 0

    #: The marker that precedes every fact, making `key(FACT, X)` mean "X in
    #: subject role". Needed because a walk alternates `key(FACT, entity)` with
    #: `key(entity, relation)` and has to build both.
    search_fact_token: int | None = None

    #: The marker after which the next token is the entity the question names as
    #: the far end -- the target a branch is checked against.
    #:
    #: **This is read from the STREAM, not from task structure.** A token in the
    #: input is what g9-02 established as implementable, where
    #: `position_kinds()` is an oracle; the same distinction applies here and it
    #: is the reason search is a mechanism rather than a ceiling.
    search_query_token: int | None = None

    #: Branch only where the first decode's top-two gap is BELOW this. `None`
    #: searches at every answerable position, which is what g13-03 measured.
    #:
    #: **A wide margin means one relation dominates**, so there is nothing to
    #: choose between and branching can only replace a correct greedy pick with
    #: a lucky endpoint — measured at −0.054 where the queried subject holds one
    #: relation, against +0.092 where it holds several.
    #:
    #: **The threshold must not be fitted on the data it is scored against.**
    #: g13-04 measured separability (AUC 0.803 at width ≥ 128) across ALL
    #: thresholds; picking one by trying them on a test set would be fitting a
    #: number rather than measuring one. `experiments/g13_05_*` derives it from
    #: a quantile of the TRAINING margins, which needs no labels at all.
    #:
    #: **Width-dependent** — the signal reaches AUC 0.710 at width 64 and 0.858
    #: at 256, because a wider store holds a cleaner superposition. Registered
    #: in `docs/SCALE.md`.
    search_gate_margin: float | None = None

    #: Beam width for the walk. **0 means `search`, which branches at the ROOT
    #: ONLY** and is what every number before note 103 was taken under; `>= 1`
    #: uses `openplexus.search.beam`, which branches at every step.
    #:
    #: Note 064 is why this exists: the walk's two halves have very different
    #: error rates -- the entity hop is 0.9889 and flat, the relation decode is
    #: 0.9348 and drops to ~0.91 mid-chain -- and 15% of relation decodes land on
    #: an entity with two or more outgoing edges, where `key(FACT, e)` holds a
    #: SUM. `search` hedges at the root, where the decode is already 0.974, and
    #: commits blindly where it is 0.906. Chain recovery: **0.6588 for `search`,
    #: 0.8877 for `beam`** (`tools/clutrr_recovery.py`, `tools/prune_period.py`).
    #:
    #: **DEFAULT 4 as of note 103**, which measured it on `run()`'s own task
    #: rather than inheriting CLUTRR's number: `beam4` beats `search4` by
    #: **+0.041 +/-0.013** on kinship at hops 2, 8 seeds -- above 2 SE, so the
    #: mechanism reaches this regime even though only one mid-chain decode exists
    #: here for it to fix. The split says it is doing its job: **+0.039 at
    #: out-degree >= 2**, where `key(FACT, e)` holds a sum, and **+0.043 at
    #: out-degree 1**, where it repairs damage `search` does by committing at the
    #: root (walk 0.702, search4 0.649, beam4 0.692).
    #:
    #: `0` still selects `search`, and every pre-note-103 number is reproducible
    #: by setting it.
    search_beam_width: int = 4

    #: Hops between the beam's rendezvous, when `search_beam_width >= 1`. `1`
    #: meets every hop and is what note 102 measured as the baseline.
    #:
    #: Exposed because the meeting is a DISTRIBUTION cost, not a search
    #: parameter: ranking all `width` partial walks against each other is the
    #: round trip that keeps a driver-free walk outside `d_max` past depth 7
    #: (`note 101`). Note 102 measured the meeting as worth **0.089** chain
    #: recovery and its period as worth nothing measurable, so `2` fits the
    #: budget for 2.29x the reads.
    #:
    #: **Left at 1, and note 103 is why it is not 2.** On the end task `2` costs
    #: **-0.016 +/-0.006** -- inside the 0.02 tolerance predicted, so note 102's
    #: finding transfers, but it is about 2.7 SE from zero and therefore a real
    #: small loss rather than free. So this is a knob a DEPLOYMENT turns up when
    #: latency binds, not a default: pay 0.016 to meet `d_max`, and only then.
    search_prune_every: int = 1

    cache_slots: int = 0

    #: Read from the cache ALONE, dropping the superposed store's contribution.
    #:
    #: An ablation for [note 030](../docs/notes/030-the-benchmark-does-not-discriminate.md),
    #: which asks for a benchmark that discriminates a superposed store from a
    #: cache and calls it the highest-value open question. It could not be
    #: answered while the cache only ever ADDED to the store: every arm holding
    #: a cache also held the store, so nothing separated them.
    #:
    #: The store is still written and admission still depends on its residual —
    #: that is what the cache selects on. Only the READ changes.
    cache_only: bool = False
    cache_sharpness: float = 8.0
    cache_weight: float = 1.0
    #: HOW TO COMBINE the token's own read with the neighbours' the content
    #: index proposed. Off by default, so every number measured before
    #: 2026-07-29 is untouched -- decision 74's failure was a default that moved.
    #:
    #: **Decision 146 is why this exists.** `index_branches` ADDS the neighbours
    #: to the token's own read, and adding cannot choose: sweeping
    #: `index_weight` moves TRANSFER and EXCEPTION accuracy monotonically
    #: against each other with their sum pinned at ~0.93. A model that must
    #: answer "birds fly, but not this one" cannot afford to average the two.
    #:
    #: The settings are four attempts at the same question -- **which read
    #: should this position answer from** -- and the first three are measured
    #: negatives kept because they are cheaper to read than to rediscover:
    #:
    #: `False`      sum them, as `index_branches` always has.
    #: `"norm"`     answer from whichever RETRIEVAL is larger. REFUTED (147).
    #: `"margin"`   answer from whichever DECODE is more confident, which is
    #:              decision 130's signal. REFUTED (147).
    #: `"occupancy"` answer from whichever ADDRESS has had more written at it,
    #:              summing written keys in the store's own space. REFUTED.
    #: `"inherit"`  answer from this address if ANYTHING was ever written here,
    #:              otherwise from the neighbours.
    #:
    #: **Why the first three failed, which is the whole argument for the
    #: fourth.** `norm` conflates *was this key ever written* with *how large
    #: the value there is*, and only the first is the question. `margin` is
    #: confidence in AN answer, which is not evidence about WHICH read produced
    #: it. `occupancy` asks the right question in the wrong space: a sum of `N`
    #: normalised near-orthogonal keys carries cross-talk of standard deviation
    #: `sqrt(N / d)`, which at `d = 64` and `N ~= 100` is larger than the signal
    #: -- and it asks it as a comparison, so a sibling whose fact was stated
    #: more recently outranks an entity that has its own.
    #:
    #: **`"inherit"` is not a comparison, and that is the point.** Membership is
    #: not "who has more", it is "is there anything here", and `AddressSketch`
    #: answers it exactly: an address never written misses the hash table and
    #: reads 0.0. So the bar is structurally ZERO rather than fitted, which is
    #: the answer note 049's P3 asked for and decision 147 could not give.
    #:
    #: The cost is a second, non-superposed memory -- stated in `sketch.py`
    #: rather than buried. What justifies it is that membership is one bit while
    #: a value is `d` floats, and the sketch must never record more than that.
    index_prefer: bool | str = False
    #: Keep the address sketch even when nothing reads it, and expose it as
    #: `model.occupied` after a run.
    #:
    #: **Decision 152 is why this is separate from `index_prefer`.** The gate
    #: needs two things: a test for *is this address empty* and a source of
    #: neighbours to read instead. The second needs a key that NAMES A CONCEPT,
    #: which is why `index_branches` is refused above one hop. The first needs
    #: only a vector to hash, and a hop key is a vector.
    #:
    #: So this flag is the half of the gate that composition tasks CAN have,
    #: and it is off by default because it is an instrument rather than a
    #: mechanism -- nothing in the read path consults it unless `index_prefer`
    #: does.
    track_occupancy: bool = False
    #: Bind this RELATION token into the hop's key, so a hop follows a NAMED
    #: edge instead of whatever happens to sit at the concept's address.
    #: `-1` is off and is every number measured before 2026-07-29.
    #:
    #: **ARCHITECTURE row D3.** Decision 157 measured the gap it fills: with
    #: typed WRITES a link no longer overwrites a fact, but LINKED queries still
    #: score 0.1275 against chance 0.125 while the gate correctly defers on
    #: 0.9933 of them. The model knows it does not know and cannot act on it,
    #: because the hop reads `key(concept)` and never `key(relation, concept)`.
    #:
    #: **It is a fixed relation, not a chosen one**, and that is the honest
    #: limit of this version. Which relation to follow is a decision the model
    #: does not make -- note 051 §5 flags it as unsolved for open queries, and
    #: decision 147 is why guessing at a learned chooser before the fixed one is
    #: shown to work would be the wrong order.
    #:
    #: Needs a key source that can form a pair (`context_keys`), because a
    #: single key has nowhere to put the relation.
    hop_relation: int = -1
    #: A relation token **per hop depth**, so hop 1 can follow LINK and hop 2
    #: FACT. Empty is off and is every number measured before 2026-07-29.
    #:
    #: **Decision 162 is what this exists for.** `hop_relation` above is one
    #: value per MODEL, so a two-hop walk follows LINK-then-LINK or
    #: FACT-then-FACT and never LINK-then-FACT -- which is exactly the path the
    #: linked-families task needs:
    #:
    #:     key(FACT, entity)   empty -- the gate fires, correctly
    #:     key(LINK, rep)      -> the linked family's representative   hop 1
    #:     key(FACT, rep')     -> that family's value                  hop 2
    #:
    #: 162 named this as the blocker rather than the relation being *fixed*:
    #: "even a correct chooser would not help until a hop can carry its own
    #: relation."
    #:
    #: **It is a SCHEDULE, and a schedule the task does not supply is a fitted
    #: constant wearing a mechanism's clothes** -- 162's own words, and the
    #: reason this is an instrument for reaching the composition measurement
    #: rather than a candidate for the final read path. Note 052 §2's
    #: try-all-and-gate is what replaces it; John's ruling in decision 163 §2 is
    #: layout first, try-all-and-gate next.
    #:
    #: Indexed by hop depth, so entry `i` types the key formed by hop `i`.
    #: Mutually exclusive with `hop_relation`, needs `context_keys` for the same
    #: reason, and must be at least `hops` long -- the halting gate reads one
    #: hop past the last readable one, so `hops - 1 + 1` keys get formed.
    hop_relations: tuple[int, ...] = ()
    #: Let the content index propose neighbours **at the hop's landing concept**
    #: rather than only at the position's -- ARCHITECTURE row E4, and John's
    #: option B.
    #:
    #: Note 044 refuses `index_branches` above one hop because a hop key "names
    #: no concept". Decision 154 measured that false: the hop's softmax lands at
    #: cosine 0.96 on a single row, so `argmax(weights)` names the concept it
    #: arrived at, and the index can look THAT up.
    #:
    #: **The fan-out is gated on emptiness, and that is what stops it
    #: exploding.** Proposing `b` neighbours at every hop is `b ** depth` reads
    #: -- 27 at three hops with three branches, which is the wrong shape for C1.
    #: So neighbours are consulted **only where the hop's own address holds
    #: nothing**, and the first candidate that holds something is taken. A chain
    #: that is finding what it needs never branches at all; branching happens at
    #: dead ends, which is exactly where it is worth paying for.
    #:
    #: **Ungated summing and this are alternatives; two GATED mechanisms are
    #: not** -- decision 160, correcting 159. With this on and `index_prefer`
    #: off, the position-level block is skipped: it would read neighbours at
    #: every position regardless of need, which measured 56 reads against 28.
    #: With `index_prefer` set the position-level mechanism is gated too, both
    #: fire only at dead ends, and they compose.
    #:
    #: Needs `track_occupancy`, because "holds nothing" is the sketch's question
    #: and answering it by norm is what decision 147 refuted.
    index_at_hops: bool = False
    readout_bias: bool = False
    derived_keys: bool = False
    context_keys: bool = False
    #: Split the FAST store by CONCEPT across this many nodes. 0 is the single
    #: `d x d` matrix every number in this project was measured with.
    #:
    #: **This is the falsifier for note 042's item 2, not a tuning knob.**
    #: Decision 134 measured the case -- pooled capacity identical, lone-node
    #: capacity 16x at 16 nodes -- but the model had never read or written
    #: through it, so the arrangement was a data structure with good properties
    #: rather than a component. This is the seam that asks whether it can still
    #: learn.
    #:
    #: **Each node keeps a FULL `d x d` store**, so `n` nodes hold `n` times the
    #: state. A comparison against `concept_nodes=0` at equal `d_model` is
    #: therefore biased TOWARD partitioning, which is deliberate: a LOSS under
    #: that bias is unambiguous evidence that routing hurts, where a win would
    #: need the g10-09 equal-state treatment before it meant anything.
    concept_nodes: int = 0
    #: How many distinct nodes hold each concept. Only meaningful with
    #: `concept_nodes`; 1 is right for a per-sequence store, which is rebuilt
    #: from scratch anyway, and higher values exist so churn can be measured.
    concept_replicas: int = 1
    #: How many similar concepts to ALSO read, from the content index. 0 is off
    #: and reproduces every number measured before note 045.
    #:
    #: **Each is an ordinary exact read at a hard token id.** The candidates come
    #: from meaning-space; the reads do not. That split is the whole design, and
    #: note 035 is why the other route is closed.
    #:
    #: Costs `branches + 1` reads per answered position, in the unit search was
    #: costed in (decision 123: 3.2x at four branches). Set to `vocab - 1` for
    #: the exhaustive arm -- John's *"sweep so it gets everything, then filter"*,
    #: which is the CEILING a cheap variant is measured as a fraction of.
    index_branches: int = 0
    #: How much a neighbour's evidence counts against the concept's own read.
    #: 1.0 gives the whole candidate set the same total weight as the exact read.
    index_weight: float = 1.0
    #: Softmax sharpness over candidate similarities. **Not a free constant.**
    #: Content vectors sit at mean cosine 0.22-0.50 (`ContentIndex.spread`)
    #: against hash keys' 0.0005, so raw similarities would hand every candidate
    #: a large share; the softmax is what makes the ranking rather than the floor
    #: decide. Shares its default with `hop_sharpness`, which solves the same
    #: problem one level up.
    index_sharpness: float = 8.0
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
        # `hop_relation` is the OTHER way to satisfy this guard, and the
        # guard's own text says so: "a hop that constructs a PAIR key is the
        # mechanism this needs". A typed hop constructs exactly that --
        # `key_source.pair(relation, decoded)` -- so the key space it queries is
        # the one the store writes to, which is the whole objection.
        if (self.hops > 1 and self.context_keys and self.search_branches < 1
                and self.hop_relation < 0 and not self.hop_relations):
            raise ValueError(
                "hops re-encode a decoded token through Wk, a SINGLE-TOKEN key "
                "table, and context_keys makes the store's keys derive from "
                "(previous, token) pairs instead -- measured cosine between "
                "the two is -0.069, so every hop after the first would query a "
                "key space the store never writes to and get noise back. It "
                "would still produce numbers. A hop that constructs a PAIR key "
                "is the mechanism this needs, and it now exists: set "
                "search_branches >= 1 to use it (openplexus/search.py, "
                "decision 123). Without it the refusal stands")
        if self.search_branches < 0:
            raise ValueError("search_branches is a count and 0 means off")
        if self.search_beam_width < 0:
            raise ValueError("search_beam_width is a count and 0 means `search`")
        # NO CROSS-CHECK against `search_branches`. There was one -- "a width set
        # while search is off reads as 'the beam is on'" -- and note 103 making 4
        # the default inverted its logic: the width now describes HOW to walk and
        # is simply unread when nothing walks. Left as a comment because the
        # check was correct when written and its removal is a consequence of the
        # default moving, not a decision that the risk went away.
        if self.search_prune_every < 0:
            raise ValueError(
                "search_prune_every counts hops between the beam's rendezvous: "
                "1 meets every hop, 0 never meets. Negative would never prune "
                "while looking like a period, and an unpruned beam is "
                "branches**hops walks")
        if self.search_gate_margin is not None and self.search_branches < 2:
            raise ValueError(
                "a gate on search decides whether to BRANCH, and there is "
                "nothing to decide with fewer than 2 branches. Set "
                "search_branches >= 2 or leave search_gate_margin unset")
        if self.search_branches >= 1:
            # Every one of these is a thing a walk cannot do without, and each
            # would otherwise fail QUIETLY -- decision 105's exact failure mode,
            # where an unwritten key space "still returned answers and
            # accuracies". A wrong number is worse than an error.
            if not self.context_keys:
                raise ValueError(
                    "search walks key on (entity, relation) pairs and a "
                    "single-token table has no such key. Set context_keys")
            if not self.derived_keys:
                raise ValueError(
                    "context_keys requires derived_keys, and a walk rebuilds "
                    "each pair key from two token ids rather than looking one "
                    "up -- that is what makes it local")
            if self.hops < 2:
                raise ValueError(
                    "a search of depth 1 is a single retrieval with extra "
                    "steps; there is no branch to choose between")
            if self.hop_accumulate != "concat":
                raise ValueError(
                    "a walk produces one retrieval per relation and the "
                    "readout has to see all of them, which is what concat "
                    "does. `replace` would discard every step but the last")
            if self.search_fact_token is None:
                raise ValueError(
                    "search needs search_fact_token: a walk alternates "
                    "key(FACT, entity) with key(entity, relation) and cannot "
                    "build the first without the marker")
            if self.search_query_token is None:
                raise ValueError(
                    "search needs search_query_token to find the target it "
                    "checks a branch against. Without a target it would have "
                    "to score branches by confidence, which decision 93 "
                    "measured at 0.628 against 0.500 for guessing")
        if self.hop_accumulate not in ("replace", "bind", "concat"):
            raise ValueError(
                "hop_accumulate must be 'replace', 'bind' or 'concat', not "
                f"{self.hop_accumulate!r}")
        if self.hop_accumulate == "concat" and self.halt_gate:
            raise ValueError(
                "concat gives the readout every hop at once, so there is no "
                "hop left to choose between -- halt_gate would be selecting "
                "among inputs the readout already has")
        if self.hop_accumulate == "concat" and self.hidden:
            raise ValueError(
                "concat resizes the readout's input and `hidden` sits between "
                "them; the two have not been made to compose and silently "
                "mis-shaping the hidden layer would be worse than refusing")
        if self.hop_accumulate != "replace" and self.hops < 2:
            raise ValueError(
                "hop_accumulate says what to do with a PREVIOUS retrieval and "
                "there is none with hops=1")
        if self.gate_objective not in ("mixture", "which_hop"):
            raise ValueError(
                "gate_objective must be 'mixture' or 'which_hop', not "
                f"{self.gate_objective!r}")
        if self.gate_objective != "mixture" and not self.halt_gate:
            raise ValueError(
                "gate_objective describes how the halting gate learns and "
                "does nothing without halt_gate")
        if self.gate_reads_key and not self.halt_gate:
            raise ValueError(
                "gate_reads_key is an input to the halting gate and does "
                "nothing without halt_gate")
        if self.halt_gate and self.hops < 2:
            raise ValueError(
                "halt_gate chooses among hops and there is nothing to choose "
                "with hops=1; set hops to the MAXIMUM depth to consider")
        if self.halt_gate and self.hidden:
            raise ValueError(
                "halt_gate mixes RETRIEVALS, which equals mixing predictions "
                "only through a linear readout; with a hidden layer the two "
                "differ and the gate's gradient would be quietly wrong")
        if self.hop_decoder not in ("encoder", "readout"):
            raise ValueError(
                "hop_decoder must be 'encoder' or 'readout', not "
                f"{self.hop_decoder!r}")
        if self.hop_sharpness < 0.0:
            raise ValueError(
                "hop_sharpness is a softmax gain and cannot be negative; "
                "a negative gain decodes to the LEAST likely token")
        if self.hops < 1:
            raise ValueError(
                "hops is a number of retrievals and must be at least 1; "
                "1 is the single-retrieval behaviour every earlier result used")
        if self.hidden < 0:
            raise ValueError("hidden is a layer width and cannot be negative")
        if self.hidden and self.d_model % self.partitions:
            raise ValueError(
                "a hidden layer is built per group, so d_model must divide "
                "into partitions")
        if self.hidden and self.orthogonal_every:
            raise ValueError(
                "orthogonal_every orthogonalises the readout update, which is "
                "shaped by the LINEAR readout; it has no meaning across two "
                "layers and would silently orthogonalise the wrong matrix")
        if self.value_centre and not self.value_lr:
            raise ValueError(
                "value_centre re-centres what value_lr moves and does nothing "
                "without it; Wv is frozen otherwise")
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
        if self.index_prefer not in (False, True, "norm", "margin",
                                     "occupancy", "sketch", "inherit"):
            raise ValueError(
                "index_prefer must be False, True or 'norm' (compare retrieval "
                "norms), 'margin' (compare decode margins), 'occupancy' "
                "(compare how much has been written at each address, summed in "
                "the store's own space) or 'sketch' (the same question asked of "
                "a hash whose collision rate is free of the store's width) "
                "or 'inherit' (answer from your own address if ANYTHING was "
                "written there, else from the neighbours), not "
                f"{self.index_prefer!r}. An unknown string is truthy and would "
                "have silently selected the norm rule, which decision 147 "
                "refuted -- so a typo would have been measured as the one "
                "setting already known not to work")
        if self.hop_relation >= 0 and not self.context_keys:
            raise ValueError(
                "hop_relation needs context_keys: a typed hop reads "
                "key(relation, concept), and a single key has nowhere to put "
                "the relation. Decision 157 is the measurement this exists for")
        if self.hop_relations:
            if self.hop_relation >= 0:
                raise ValueError(
                    "hop_relation and hop_relations are two answers to the same "
                    "question -- which relation does hop i follow -- and "
                    "silently preferring one would make the other look "
                    "connected while doing nothing. Set exactly one: "
                    "hop_relation for one relation at every depth (decision "
                    "158), hop_relations for a schedule (decision 162)")
            if not self.context_keys:
                raise ValueError(
                    "hop_relations needs context_keys, for the same reason "
                    "hop_relation does: a typed hop reads key(relation, "
                    "concept) and a single key has nowhere to put the relation")
            if any(relation < 0 for relation in self.hop_relations):
                raise ValueError(
                    "every entry in hop_relations must be a relation token. A "
                    "negative entry would mean one UNTYPED hop inside a typed "
                    "walk, which queries the single-token key space the store "
                    "never writes to under context_keys -- measured cosine "
                    "-0.069 -- and would return noise while still producing a "
                    "number. That is the refusal above, mid-walk")
            # A LENGTH CHECK RATHER THAN A FALLBACK, and the reason is that both
            # available fallbacks are wrong in a way nothing downstream can see.
            # Reusing the last entry silently turns a 2-entry schedule into
            # LINK-then-FACT-then-FACT; treating a missing entry as untyped
            # reintroduces the key-space mismatch above. Either produces a full
            # set of numbers from a walk nobody specified.
            if len(self.hop_relations) < self.hops:
                raise ValueError(
                    f"hop_relations has {len(self.hop_relations)} entries and "
                    f"hops is {self.hops}, which needs at least {self.hops}: "
                    "the walk forms hops - 1 keys, plus one more because the "
                    "halting gate scores hop k by what hop k + 1 returns. A "
                    "schedule shorter than the walk has to be extended by a "
                    "rule, and every such rule changes which relations get "
                    "followed without saying so")
        if self.memory_cap < 0.0:
            raise ValueError("memory_cap must not be negative")
        if self.capture_slots < 0:
            raise ValueError("capture_slots must not be negative")
        if self.capture_slots and not self.consolidation:
            raise ValueError(
                "capture_slots bounds consolidation and does nothing without it")
        if self.lasting_cap < 0.0:
            raise ValueError("lasting_cap must not be negative")
        if not 0.0 < self.lasting_decay <= 1.0:
            raise ValueError("lasting_decay must be in (0, 1]")
        if self.lasting_decay != 1.0 and not self.persistent_lasting:
            raise ValueError(
                "lasting_decay is applied once per SEQUENCE, and without "
                "persistent_lasting the slow store does not survive one -- the "
                "setting would be silently inert")
        if self.persistent_lasting and not self.consolidation:
            raise ValueError(
                "persistent_lasting keeps the CONSOLIDATED store across "
                "sequences, and without `consolidation` nothing is ever "
                "promoted into it -- the flag would be silently inert, which "
                "is how decision 79 caught a write gate producing numbers "
                "identical to the baseline to the last decimal")
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
        if self.index_branches < 0:
            raise ValueError("index_branches must not be negative")
        if self.index_sharpness <= 0.0:
            raise ValueError(
                "index_sharpness must be positive: at zero every candidate "
                "gets an equal share regardless of similarity, which is the "
                "index proposing nothing")
        if self.index_at_hops and not self.track_occupancy:
            raise ValueError(
                "index_at_hops needs track_occupancy: the fan-out fires only "
                "where an address holds nothing, and that is the sketch's "
                "question. Deciding it by retrieval norm is what decision 147 "
                "refuted, and an ungated fan-out is b ** depth reads")
        if self.index_branches and self.hops > 1 and not self.index_at_hops:
            raise ValueError(
                "index_branches cannot be combined with hops > 1: the hop key "
                "is a softmax mixture of every token's row, so it names no "
                "concept and the index has nothing to look up. Note 044")
        if self.concept_nodes < 0:
            raise ValueError("concept_nodes must not be negative")
        if self.concept_replicas < 1:
            raise ValueError("a concept held nowhere is a concept lost")
        if self.concept_nodes:
            # SCOPE, stated as refusals rather than discovered as wrong numbers.
            #
            # Each of these is a mechanism that cannot be routed to one node, so
            # combining it with partitioning would either quietly broadcast --
            # the collective amended C1 forbids -- or quietly read the wrong
            # store. Refusing is what keeps the falsifier's result attributable.
            if self.hops > 1:
                raise ValueError(
                    "concept_nodes cannot be combined with hops > 1: the hop "
                    "key is a softmax mixture of every token's key row "
                    "(`hop_key = weights @ wk`), so it names no concept and "
                    "there is no node to send it to. Note 044. The search walk "
                    "commits to a hard token at every step and IS routable, "
                    "which is what a partitioned model should use instead")
            if self.reward_token >= 0:
                raise ValueError(
                    "concept_nodes cannot be combined with reward_token: the "
                    "un-reward path subtracts pending writes, and `pending` "
                    "keeps (weight, value, key) without the concept that names "
                    "which node to subtract from. Storable, and unbuilt")
            if self.memory_cap:
                raise ValueError(
                    "concept_nodes cannot be combined with memory_cap: a cap "
                    "on the pooled norm is a collective, and a per-node cap is "
                    "a different mechanism that has never been measured")
            if self.tag_relative:
                raise ValueError(
                    "concept_nodes cannot be combined with tag_relative: it "
                    "divides by the store's total size, which no single node "
                    "can know")
            if self.carry_store:
                raise ValueError(
                    "concept_nodes cannot be combined with carry_store: "
                    "carrying is sound here but untested, and an untested "
                    "combination inside a falsifier is how a result stops "
                    "being attributable")
            if self.consolidation:
                # THE ONE REFUSAL THAT IS TEMPORARY BY DESIGN. Note 042's items
                # 1 and 2 are the same design seen from two sides, and the
                # eventual target is a persistent store that is ITSELF
                # concept-partitioned. `lasting` is a single `d x d` matrix, so
                # adding it to a node's own store would give every node the same
                # slow store -- a collective wearing a local's clothes.
                #
                # Refused here because this seam exists to ask ONE question. Two
                # changes at once produce a number nobody can attribute, which
                # is the rule `orthogonal_every` is waiting on as well.
                raise ValueError(
                    "concept_nodes cannot yet be combined with consolidation: "
                    "`lasting` is a single matrix shared by every node, so "
                    "partitioning it is the next piece of work rather than a "
                    "flag combination")


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
            #
            # DERIVED SPARSE KEYS. With `derived_keys` the active set for token
            # `t` is drawn from `(seed, t)` alone, so a node holding only the
            # seed can rebuild any row on demand and never has to be sent one.
            # These two used to be refused as conflicting; they do not conflict,
            # nobody had written the per-token draw, and without it sparse keys
            # -- which are worth 0.18 bits on the corpus and are CHEAPER on the
            # wire than dense ones -- could not be used by a distributed node at
            # all.
            self.wk = np.zeros((v, d))
            for token in range(v):
                draw = (np.random.default_rng((config.seed, token))
                        if config.derived_keys else rng)
                active = draw.choice(d, config.key_active, replace=False)
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
        # WHICH CONCEPT A SURFACE BELONGS TO, as a replaceable component for the
        # same reason keys are -- and this one was welded shut until now.
        #
        # `key_source.concept` answers "which surface token addresses this
        # position". This answers "which CONCEPT that surface is of", and the
        # two are different questions that happened to have the same answer
        # because every measurement so far had one surface per concept.
        #
        # John, 2026-07-29: a picture of a dog, a drawing, and the word are one
        # concept. That is impossible while the surface IS the address, however
        # good the content vectors get -- similarity relates DIFFERENT concepts,
        # where this is one concept with different appearances.
        #
        # The default is the identity, so every existing number is untouched.
        # Assign a different `Surfaces` after construction; `run` reads it.
        self.surfaces: Surfaces = OneConceptPerToken(config.vocab_size)
        #: The content index, when there is one. Assigned after construction
        #: because it is LEARNED from observed co-occurrence -- unlike keys and
        #: surfaces, which are pure -- so the model cannot build its own without
        #: deciding what data it should have seen.
        self.content = None
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
        #: The slow store, when `persistent_lasting` keeps it across sequences.
        #: `None` until the first `run` sizes it. **This is the only state on
        #: the model that accumulates across a corpus other than `Wo`**, which
        #: is the point -- see decision 62 and note 042.
        #:
        #: Cleared by `forget_lasting()`, which a test or a new lifetime calls;
        #: nothing clears it implicitly, because a store that quietly resets is
        #: indistinguishable from one that never worked.
        self._lasting: np.ndarray | None = None
        #: Which way `index_prefer="occupancy"` decided, as `(position,
        #: deferred)` pairs. Appended across runs and cleared by whoever reads
        #: it. The position is carried rather than implied, because the gate
        #: only fires where the index proposed something and an index into a
        #: dense list would quietly misalign with the sequence.
        #:
        #: **Recorded because accuracy alone cannot tell the two failures
        #: apart.** If the gate defers on transfer queries and holds on direct
        #: ones and accuracy still does not move, the retrieval is corrupted
        #: rather than mis-selected -- and decision 147 was wrong about where
        #: the problem is. That distinction is not visible in a score.
        self.deferrals: list[tuple[int, bool]] = []
        #: The address sketch from the last `run`, when one was kept. See
        #: `track_occupancy`.
        self.occupied = None
        #: The store this model ended its last sequence with. `None` until `run`
        #: has been called once. Read by `answer_set` and by nothing in `run` --
        #: see the assignment for why that line matters.
        self._final = None
        self._concepts = None
        #: How many times consolidation has fired, over the model's whole life.
        #: **Observation only**, like `trace` -- nothing reads it back.
        #:
        #: It exists so a null result can be attributed. The gate is
        #: `predictions[t-1] == token`, so it opens only where the model was
        #: already correct; a persistent store that does not help because
        #: nothing was ever promoted into it is a different finding from one
        #: that does not help because persistence is the wrong idea.
        self.consolidations: int = 0
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
        # A HIDDEN LAYER INSIDE EACH GROUP, off by default.
        #
        # `Wo` was the only thing this model learned across a corpus, and it is
        # a single LINEAR map onto a retrieval it does not influence. Note 037
        # measured what that costs: a two-layer readout on the SAME frozen
        # features is worth 0.63 bits prequentially, one pass, no split, no
        # temperature -- and it is the first mechanism here to move the data
        # exponent rather than the level.
        #
        # **It does not widen the C1 argument.** A group already holds its own
        # `d / partitions` slice and computes its own `parts[g]`; making that
        # slice two layers means backpropagating through two matrices that the
        # same node already owns, using its own activity and its own error. No
        # other group's state enters, which `test_composed_readout.py` asserts
        # rather than assumes.
        self.hidden_w = None
        if config.hidden:
            per = d // config.partitions
            self.hidden_w = rng.normal(
                0.0, 1.0 / np.sqrt(per), (config.partitions, config.hidden, per))
            # The output weights now read the HIDDEN units, not the retrieval.
            self.wo = np.zeros((v, config.partitions * config.hidden))
        if config.hop_accumulate == "concat":
            # THE READOUT'S INPUT IS `hops` TIMES WIDER, because it sees every
            # hop side by side rather than only the last. Columns
            # `[0 : d // partitions]` of each group are hop 1, so a caller that
            # seeds the readout from `Wv` writes into that slice and leaves the
            # later hops at zero -- which starts the model as the one-hop model
            # and lets the extra hops earn their weight.
            self.wo = np.zeros((v, config.partitions * config.hops
                                * (d // config.partitions)))
        self.grouped_wo = self.wo.reshape(v, config.partitions, -1)
        assert self.grouped_wo.base is self.wo, "grouped_wo must alias wo"
        #: Per-group score for "read from this hop", or None when the gate is
        #: off. ZERO-initialised on purpose: a zero score is a uniform softmax,
        #: so the gate starts as a plain average over hops and every hop gets
        #: gradient. Random init would pick a hop before seeing any data.
        self.halt_w = (np.zeros((config.partitions, d // config.partitions))
                       if config.halt_gate else None)
        #: The second rule, and the selector that blends it in from the current
        #: key. Both None unless `gate_reads_key`. Zero-initialised so the gate
        #: starts as exactly the one-rule gate — `halt_alt` contributes nothing
        #: and `halt_select` sits at 0.5 — which makes this strictly an
        #: extension rather than a different mechanism at step zero.
        self.halt_alt = (np.zeros((config.partitions, d // config.partitions))
                         if config.halt_gate and config.gate_reads_key
                         else None)
        self.halt_select = (
            np.zeros((config.partitions, d // config.partitions))
            if config.halt_gate and config.gate_reads_key else None)
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

    def forget_lasting(self) -> None:
        """Drop the persistent slow store, if there is one.

        **Nothing calls this implicitly.** A store that quietly resets between
        sequences is indistinguishable from one that never accumulated, and
        that failure would look exactly like the null this mechanism is being
        tested against — decision 62 was found by noticing that `learn=False`
        predictions were byte-identical whether or not another sequence had run,
        which is what "nothing carries" looks like from the outside.

        So the reset is explicit and a caller has to mean it.
        """
        self._lasting = None

    def _relation_at(self, depth: int) -> int:
        """Which relation hop `depth` follows, or -1 for an untyped hop.

        The contract: a caller forming a hop key asks this rather than reading
        either config field, so the single-relation and scheduled cases cannot
        drift apart. `depth` is 0-based over the keys the walk forms.

        Decision 162 is why the schedule exists; decision 158 is why the single
        value does. `__post_init__` refuses both at once and refuses a schedule
        shorter than the walk, so this needs no fallback -- which is the point,
        because every available fallback silently changes the walk.
        """
        if self.config.hop_relations:
            return self.config.hop_relations[depth]
        return self.config.hop_relation

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
        # THE OCCUPANCY SKETCH -- one d-vector beside a d x d store, so it is
        # 1/d of the memory and nothing anyone would notice.
        #
        # Written keys accumulate here NORMALISED, so each write contributes 1.0
        # to its own address and ~1/sqrt(d) to every other. Reading it is one
        # dot product. It answers "has anything been written at this key",
        # which is what the refuted norm rule was trying and failing to
        # approximate through the value.
        occupied = None
        if self.config.index_prefer == "occupancy":
            occupied = SumSketch(d)
        elif (self.config.index_prefer in ("sketch", "inherit")
                or self.config.track_occupancy):
            occupied = AddressSketch(d, seed=self.config.seed)
        # EXPOSED, because the sketch answers a question the gate is only one
        # consumer of. `index_branches` is refused above `hops == 1` -- a hop key
        # is a softmax mixture and names no concept, so the content index has
        # nothing to look up (note 044). **The sketch has no such requirement:**
        # it hashes whatever vector it is handed, so it can be asked about a hop
        # key even where the index cannot. Decision 152 is why that separation
        # is worth having in the open.
        self.occupied = occupied
        # THE CONCEPT-PARTITIONED FAST STORE, when one is asked for.
        #
        # `memory` stays as the matrix a read is served FROM -- reassigned each
        # step to whichever node owns the concept being read -- so every
        # retrieval strategy, the readout, and the trace keep working against a
        # `d x d` matrix exactly as they did. What changes is which one.
        #
        # Assigned by `.matrix()`, which returns a VIEW, so `memory += ...`
        # writes into the owning node rather than into a copy. That is the whole
        # of the routing, and it is why this does not need a second inner loop.
        concepts = None
        if self.config.concept_nodes:
            concepts = ConceptStore(nodes=self.config.concept_nodes, width=d,
                                    seed=self.config.seed,
                                    replicas=self.config.concept_replicas)
            if leave is not None:
                raise ValueError(
                    "concept_nodes cannot be combined with `leave`: a departure "
                    "here clears DIMENSION rows, which is the other "
                    "arrangement's failure mode. A concept-partitioned "
                    "departure removes whole concepts and `ConceptStore.lose` "
                    "is what expresses it")
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
        # THE SLOW STORE, and whether it survives this call is the whole of
        # decision 62's unaddressed finding.
        #
        # Built here, it is rebuilt every sequence like the fast one -- so the
        # two-timescale machinery consolidation implements has both timescales
        # INSIDE one sequence, and nothing at all carries across the corpus
        # except `Wo`. `persistent_lasting` moves it onto the model, where a
        # concept map could actually accumulate.
        #
        # Off by default, so every number recorded before this stands.
        if self.config.persistent_lasting:
            if self._lasting is None:
                self._lasting = np.zeros((d, d))
            # THE SLOW STORE'S BRAKE, applied once per sequence because that is
            # its timescale. Without it the store only ever grows and saturates
            # against whatever cap exists -- g15-01 measured the norm pinned at
            # exactly 5, 50, 500 and 1e9 in turn.
            if self.config.lasting_decay != 1.0:
                self._lasting *= self.config.lasting_decay
            lasting = self._lasting
        else:
            lasting = (np.zeros((d, d)) if self.config.consolidation else None)
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
        previous_concept = -1
        previous_retrieval = None
        # None until the query marker is seen, which is also what turns search
        # on: before the question names its far end there is nothing to check a
        # branch against, so the walk would have to score by confidence -- the
        # signal decision 93 measured at 0.628 against 0.500 for guessing.
        search_target = None
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

            # THE TARGET A BRANCH IS CHECKED AGAINST, read from the STREAM.
            #
            # The token after the query marker names the far end of the question,
            # which is the disambiguator decision 108 found the store had never
            # been given: it answers "what relation does S hold" correctly where
            # the question needs "which of S's relations leads to T".
            #
            # Reading it from the input is what makes search a mechanism rather
            # than a ceiling. g9-02 established that a token IN THE STREAM is
            # implementable where `position_kinds()` is an oracle, and this is
            # the same distinction.
            if (self.config.search_query_token is not None and t > 0
                    and int(tokens[t - 1]) == self.config.search_query_token):
                search_target = self.wv[int(tokens[t])]

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
            if concepts is not None and previous_key is not None:
                # POINT THE STORE AT THE NODE THAT OWNS WHAT IS BEING WRITTEN.
                # The binding about to be made is `previous_key -> value`, and
                # `previous_key` is token t-1's, so it belongs on t-1's node --
                # not on the node serving this step's read, which is a different
                # concept and usually a different machine.
                memory = concepts.matrix(previous_concept)
            if (self.config.decay_when_masked and self.config.decay < 1.0
                    and previous_key is not None
                    and not (store is None or store[t])):
                if concepts is None:
                    memory *= self.config.decay
                else:
                    concepts.decay(self.config.decay)
                if occupied is not None:
                    # The sketch fades with the store it describes. An address
                    # whose binding has decayed away must not still read as
                    # occupied, or the gate defends a fact that is gone.
                    occupied.decay(self.config.decay)
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
                    if occupied is not None:
                        occupied.decay(self.config.decay)
                    if concepts is None:
                        memory *= self.config.decay
                    else:
                        # EVERY node fades, not just the one being written.
                        # Decay is per-step, and every node already learns that
                        # a step happened from the token broadcast -- 5 bytes,
                        # the message the model has always sent. No node has to
                        # hear from another for this.
                        concepts.decay(self.config.decay)
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
                if occupied is not None:
                    # ONE write per write. Not the value, not the error, not
                    # the write gate -- the sketch records THAT an address was
                    # written and deliberately nothing about what went in. That
                    # is the whole reason it can separate cases the retrieval
                    # norm could not, and the moment it carries a value it has
                    # become a second store and the comparison is worthless.
                    occupied.add(previous_key)
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
                # COUNTED so a null can be attributed. Consolidation fires on
                # `predictions[t-1] == token` -- it promotes what the model
                # ALREADY GOT RIGHT -- so a persistent store cannot bootstrap a
                # model that predicts badly. Without this counter, "the
                # persistent store did not help" and "the gate never opened"
                # are the same number.
                self.consolidations += int(bool(fires))
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
            if concepts is not None:
                # AND NOW POINT IT AT THE NODE BEING READ, which is this
                # token's concept rather than the previous one's. The write
                # above and the read here are two different machines in the
                # deployed picture; here they are two views of one array.
                memory = concepts.matrix(
                    self.surfaces.of(self.key_source.concept(tokens, t)))
            readable = memory if lasting is None else memory + lasting
            retrieved = self.retrieval.read(readable, key)
            neighbours = None
            strongest = 0.0
            occupancy_near = 0.0
            # SKIP THE POSITION-LEVEL BLOCK ONLY WHEN IT IS THE UNGATED ONE.
            #
            # Decision 160: `index_at_hops` used to skip this unconditionally,
            # which made `inherit` unreachable and blocked the run that would
            # give either mechanism a task number. The doubling that prompted it
            # came from decision 146's UNGATED summing, which reads neighbours at
            # every position whether or not anything is needed.
            #
            # `index_prefer` is not that. It is gated -- it defers only where the
            # token's own address is empty and a neighbour's is not -- so it
            # composes with the hop-level fan-out at a cost measured in dead
            # ends rather than positions. `ItComposesWithTheInheritGate` is where
            # that is measured rather than argued.
            if self.config.index_branches and (
                    self.config.index_prefer
                    or not self.config.index_at_hops):
                # THE CONTENT INDEX, note 045. Similar concepts are asked as
                # well, and each is an ORDINARY EXACT READ at a hard token id --
                # nothing here blurs a key.
                #
                # That is the whole design: similarity decides WHICH exact reads
                # to make, and never what an address looks like. Note 035
                # measured why the other route is closed -- interference is
                # O(N*rho) in mean key cosine, so overlapping keys spend the
                # capacity that is already the wall.
                #
                # The token's own read stays at full weight and the neighbours
                # are added to it. A neighbour is evidence about this concept,
                # not a replacement for it.
                if self.content is None:
                    raise ValueError(
                        "index_branches needs a fitted ContentIndex on "
                        "`model.content`: the index is learned from observed "
                        "co-occurrence and there is nothing to propose "
                        "candidates from until it has seen data")
                if (self.config.index_prefer == "inherit"
                        and occupied is not None
                        and occupied.count(key) > 0.0):
                    # READ-GATED, not just decision-gated -- and decision 148
                    # was never either. `inherit` decides by emptiness but read
                    # every neighbour first and chose afterwards, so it paid the
                    # full fan-out at EVERY position, including the ones where
                    # it could not possibly defer.
                    #
                    # Skipping is behaviour-preserving rather than an
                    # approximation: `defer` requires `here <= 0.0`, so a
                    # position whose own address is occupied never defers no
                    # matter what the neighbours hold. The reads were pure cost.
                    #
                    # Decision 161. Found by a test measuring the composition
                    # with `index_at_hops`, which came back at exactly double
                    # and should not have.
                    proposed = []
                else:
                    here = self.surfaces.of(self.key_source.concept(tokens, t))
                    # NOT `scored`. `scored` is `run`'s parameter saying which
                    # positions the delta rule applies at, and shadowing it made
                    # `if learn and scored[t]` index a candidate list. Same class as
                    # `store` and `key` above, and it failed loudly only because the
                    # lists happened to be different lengths.
                    proposed = self.content.nearest(here,
                                                    self.config.index_branches)
                if proposed:
                    similarity = np.array([s for _, s in proposed])
                    # SOFTMAX, not the raw similarity, and `spread` is why.
                    # Content vectors sit at mean cosine 0.22 to 0.50 against
                    # hash keys' 0.0005, so raw weights would give every
                    # candidate a large share and swamp the exact read with a
                    # floor that carries no information.
                    weights = np.exp(self.config.index_sharpness
                                     * (similarity - similarity.max()))
                    weights /= weights.sum()
                    for (candidate, _), weight in zip(proposed, weights):
                        if weight < 1e-6:
                            continue
                        near = self.key_source.key_as(tokens, t, candidate)
                        if concepts is not None:
                            # A candidate lives on ITS OWN node, which is the
                            # cost note 044 flagged: the ring spreads by hash,
                            # so similar concepts are deliberately apart. Each
                            # candidate is one more machine to ask.
                            readable = concepts.matrix(
                                self.surfaces.of(candidate))
                            if lasting is not None:
                                readable = readable + lasting
                        raw = self.retrieval.read(readable, near)
                        contribution = (self.config.index_weight * weight * raw)
                        if occupied is not None:
                            occupancy_near = max(occupancy_near,
                                                 occupied.count(near))
                        if self.config.index_prefer:
                            # THE COMPARISON MUST BE SCALE-FAIR, and the first
                            # version was not: it weighed the token's own RAW
                            # read against the neighbours' DOWN-WEIGHTED sum,
                            # so `index_weight` decided the winner before the
                            # evidence did. The own read won by default and the
                            # rail caught it -- R2 lost 0.33 of transfer on a
                            # task with no conflicts to resolve at all.
                            #
                            # So the strength of the neighbour evidence is the
                            # strongest RAW neighbour read, while the value
                            # answered from is still the weighted mixture.
                            strongest = max(strongest,
                                            float(np.linalg.norm(raw)))
                            neighbours = (contribution if neighbours is None
                                          else neighbours + contribution)
                        else:
                            retrieved = retrieved + contribution
                    if concepts is not None:
                        # Point `readable` back at THIS concept's node. The loop
                        # above walked it across the candidates' machines, and
                        # everything after this -- the hop, the trace, the
                        # consolidation -- reads it expecting the position's own
                        # store.
                        readable = memory
                        if lasting is not None:
                            readable = readable + lasting
                if self.config.index_prefer and neighbours is not None:
                    # THE CHOICE, and it is a comparison rather than a bar.
                    #
                    # Whichever retrieval carries more signal is the one
                    # answered from -- the other is discarded rather than
                    # blended, because blending is what decision 146 measured
                    # and it averages. A token nothing was written about
                    # retrieves near zero and therefore defers to its
                    # neighbours; a token with its own stated fact does not,
                    # and its own fact wins even when every neighbour disagrees.
                    #
                    # No threshold, so nothing here has to generalise across
                    # configurations -- which is what note 049's P3 was worried
                    # about and the reason this is a comparison at all.
                    if self.config.index_prefer in ("occupancy", "sketch",
                                                    "inherit"):
                        # THE SET-MEMBERSHIP QUESTION, and it is still a
                        # comparison rather than a bar: whichever address has
                        # had more written at it is the one answered from.
                        #
                        # An entity with its own stated fact reads ~1 here and
                        # keeps its answer even when every sibling disagrees --
                        # which is the exception case the grouped arm destroys.
                        # An entity nothing was ever stated about reads at the
                        # cross-talk floor and defers to its siblings -- which
                        # is the transfer case plain addressing cannot do.
                        #
                        # Both directions fall out of one scalar, and the
                        # scalar is blind to the value. That blindness is the
                        # point: decision 147's norm rule failed because it
                        # could not be.
                        here = occupied.count(key)
                        if self.config.index_prefer == "inherit":
                            # MEMBERSHIP IS NOT A COMPARISON, which is what the
                            # `sketch` arm got wrong. Asking whether the
                            # neighbours have MORE written at them than this
                            # address does defers whenever a sibling's fact was
                            # stated more recently -- decay makes a later write
                            # count for more -- so it threw away 0.613 of the
                            # entity's OWN answers while getting every transfer
                            # right.
                            #
                            # The question was never "who has more". It is
                            # "does this address hold anything at all", and with
                            # an exact hash the bar is structurally ZERO rather
                            # than fitted: an address never written misses the
                            # table and reads exactly 0.0, while one written
                            # once reads at worst `decay ** steps`, which is
                            # positive.
                            #
                            # Both sides are required. If nothing was written
                            # here AND nothing was written at any neighbour,
                            # deferring would answer from noise -- decision
                            # 69's lesson about two weak reads summing to a
                            # confident wrong answer.
                            defer = here <= 0.0 < occupancy_near
                        else:
                            defer = occupancy_near > here
                        if defer:
                            retrieved = neighbours
                        self.deferrals.append((t, defer))
                    elif self.config.index_prefer == "margin":
                        # DECISION 130'S ACTUAL SIGNAL, and the reason this
                        # branch exists: the norm version claimed 130's
                        # precedent and did not implement it. 130 fires on the
                        # MARGIN OF THE DECODE -- how far the best answer beats
                        # the second -- not on how large a retrieval is.
                        #
                        # Magnitude says how much was written at an address.
                        # Margin says how sure the readout is about what came
                        # back, and only the second is evidence about being
                        # RIGHT. Measured: the norm rule collapsed to 0.247 on
                        # exceptions where plain addressing holds 0.783.
                        own_scores = self.wo @ retrieved
                        near_scores = self.wo @ neighbours
                        if _margin(near_scores) > _margin(own_scores):
                            retrieved = neighbours
                    elif strongest > float(np.linalg.norm(retrieved)):
                        retrieved = neighbours
            # NOT `key`. `key` is the TOKEN's key and it is carried out of this
            # loop to `previous_key`, which is what the next position WRITES
            # with. Reassigning it here made every binding in the store use a
            # re-encoded hop key instead of the token's -- so turning hops on
            # corrupted the memory the hops were trying to read, and `hops=2`
            # destroyed the 1-hop case that already worked. Same shadowing class
            # as `store` above, and just as quiet.
            hop_key = key
            # `latest` is what the NEXT hop decodes from; `retrieved` is what
            # the readout eventually consumes. Identical under `replace`, and
            # under `bind` they are two different jobs sharing a loop.
            latest = retrieved
            # Only collected when the gate is on, so the ungated path allocates
            # nothing and stays bit-identical to what every earlier result used.
            # Collected for the gate, and for `concat` which needs every hop.
            keep_hops = (self.halt_w is not None
                         or self.config.hop_accumulate == "concat")
            per_hop = [retrieved] if keep_hops else None
            # SEARCH REPLACES THE HOP LOOP, it does not run beside it.
            #
            # A hop decodes a retrieval and re-encodes it through `Wk`; a walk
            # commits to a token and keys on `(entity, relation)` pairs
            # directly. Running both would re-encode pointlessly and the walk's
            # result would be overwritten by the hop's. `searching` is False in
            # every configuration measured before decision 123, so the path
            # below is bit-identical to what every earlier result used.
            searching = (self.config.search_branches >= 1
                         and search_target is not None)
            if searching:
                branches = self.config.search_branches
                if self.config.search_gate_margin is not None:
                    # THE GATE. g13-03 measured search gaining +0.092 where the
                    # queried subject holds several relations and losing 0.054
                    # where it holds one, so running it everywhere is close to a
                    # wash. g13-04 measured the decode margin separating those
                    # cases at AUC 0.803 -- against decision 93's 0.628 for
                    # identity-free confidence signals fitted WITH the labels.
                    #
                    # A WIDE margin means one relation dominates, so there is
                    # nothing to choose between and branching can only replace a
                    # correct greedy pick with a lucky endpoint. Narrow means
                    # several compete, which is what search is for.
                    #
                    # Decided BEFORE walking, which is not merely cheaper: the
                    # endpoint margin, available only after paying for the
                    # walks, measured BELOW CHANCE at every width (g13-04 P3).
                    scored = search_candidates(
                        readable, self.retrieval, self.key_source, self.wv,
                        int(self.config.search_fact_token), int(tokens[t]),
                        self.config.search_branches)
                    if search_margin(scored) >= self.config.search_gate_margin:
                        branches = 1
                # `beam` OR `search`, never both -- they are the same mechanism
                # branching in different places, and note 064 measured `search`'s
                # placement as the wrong one: it hedges at the root where the
                # decode is 0.974 and commits where it is 0.906. `beam` is off by
                # default so every number taken before note 103 is reproducible.
                if self.config.search_beam_width >= 1:
                    walks = run_beam(
                        readable, self.retrieval, self.key_source, self.wv,
                        int(self.config.search_fact_token), int(tokens[t]),
                        search_target, self.config.hops,
                        width=self.config.search_beam_width, branches=branches,
                        prune_every=self.config.search_prune_every)
                else:
                    walks = run_search(
                        readable, self.retrieval, self.key_source, self.wv,
                        int(self.config.search_fact_token), int(tokens[t]),
                        search_target, self.config.hops, branches)
                if walks:
                    # The winning walk's per-relation retrievals ARE the hops the
                    # readout consumes, in order, which is why `concat` is
                    # required: one vector per relation, all of them visible.
                    per_hop = list(walks[0].retrieved)
                    retrieved = per_hop[-1]
                if trace is not None:
                    # THE SIGNALS A GATE ON SEARCH WOULD CONSULT, recorded on
                    # the channel that exists for exactly that question.
                    #
                    # g13-03 measured search gaining +0.092 where the queried
                    # subject holds several relations and losing 0.054 where it
                    # holds one, so running it everywhere is close to free and
                    # close to worthless. A gate needs to know which case it is
                    # in BEFORE walking, and `decode_margin` is the candidate:
                    # wide gap for one relation, narrow for several.
                    #
                    # Observation only, like every other trace field -- nothing
                    # reads it back, and `test_trace_observes.py` pins that a
                    # traced run and an untraced one agree.
                    scored = search_candidates(
                        readable, self.retrieval, self.key_source, self.wv,
                        int(self.config.search_fact_token), int(tokens[t]),
                        self.config.search_branches)
                    trace.append({
                        "position": t,
                        "search_decode_margin": search_margin(scored),
                        "search_top_score": scored[0][1] if scored else 0.0,
                        "search_candidates": len(scored),
                        # The verification margin, which is the OTHER candidate
                        # signal and is only available after paying for the
                        # walks. Recorded so the two can be compared on the same
                        # sequences rather than in separate probes.
                        "search_endpoint_margin": (
                            walks[0].score - walks[1].score
                            if len(walks) > 1 else 0.0),
                    })
            # One EXTRA retrieval when gating: the gate scores hop k by what hop
            # k+1 returns, so the last readable hop still needs a lookahead.
            extra = 1 if self.halt_w is not None else 0
            # INDEXED, because the relation can now vary with depth -- decision
            # 162. `depth` counts the keys this loop forms, which is what
            # `hop_relations` is indexed by and what `__post_init__` sizes
            # against.
            for depth in range(0 if searching
                               else self.config.hops - 1 + extra):
                # DECODE AND RE-ENCODE, which is how a retrieval becomes a key.
                #
                # `retrieved` lives in VALUE space and keys live in KEY space:
                # for token c a retrieval gives about `wv[c]` and the next hop
                # needs `wk[c]`, which are different random vectors. They cannot
                # simply be fed back.
                #
                # So decode to a distribution over tokens and re-encode that
                # distribution as a key. **No new parameters** -- it uses `Wo`
                # and the key source, both of which already exist. Tying
                # `Wk = Wv` would be one line and changes the model everywhere;
                # learning a value-to-key map adds parameters, and decision 65
                # measured a trained projection collapsing the rank and costing
                # 0.45 bits.
                #
                # Each group decodes from its own slice and the votes are then
                # POOLED ACROSS GROUPS, which is one vector of `vocab` numbers
                # per hop -- the same shape and the same crossing that
                # `parts.sum(0)` already makes at the readout, not a new one.
                # Whether that sum is affordable over the internet is the first
                # of the four approved un-constraints in BACKLOG, and a hop
                # makes it cost `hops` times as much.
                # WHICH MATRIX DECODES. `readout` reuses `Wo`, which is also
                # what produces the answer -- and at `hops > 1` the answer
                # gradient flows through it, pulling it toward emitting the
                # FINAL token exactly where the hop needs the INTERMEDIATE one.
                # `encoder` uses the transpose of the frozen `Wv` instead: `Wv`
                # encodes token to value, so it decodes value to token, and
                # being frozen nothing can drag it off that job. Neither adds a
                # parameter. Both are kept because which one wins is a property
                # of the configuration (decision 74) and this is the axis that
                # measures it.
                # DECODED FROM THE LATEST FETCH, never from the accumulator.
                # Under `bind` those differ, and decoding the bound product
                # would ask "what token is R1-and-R2 together", which names
                # nothing -- the traversal would wander off after the first
                # hop while still looking like it was running.
                if self.config.hop_decoder == "encoder":
                    pooled = self.wv @ latest
                else:
                    scores = np.einsum("vgd,gd->gv", self.grouped_wo,
                                       latest.reshape(groups, -1))
                    pooled = scores.sum(0)

                # SHARPEN, SCALE-FREE. Measured before this line existed: the
                # decode is RIGHT -- argmax finds the intermediate token 1.000
                # of the time, trained or untrained -- and the softmax over it
                # was UNIFORM to three decimals (entropy 3.912 against log(50)
                # = 3.912), because top-1 beat top-2 by 0.0388. A flat weight
                # vector makes `weights @ wk` the mean of every key row: noise
                # wearing a key. The hop was throwing away a correct decode.
                #
                # Standardised rather than given a temperature constant. The
                # logit scale moves with `key_scale`, `d_model`, `decay` and
                # `memory_cap`, so a tuned constant would work here and fail
                # silently when any of those changed -- decision 74's pattern,
                # where a mechanism's effect is a property of the configuration.
                # Dividing by the spread makes the sharpness mean the same
                # thing in every cell.
                spread = float(pooled.std())
                if spread > 0.0:
                    pooled = ((pooled - pooled.mean()) / spread
                              * self.config.hop_sharpness)
                pooled = pooled - pooled.max()
                weights = np.exp(pooled)
                weights /= weights.sum()
                # SOFT rather than argmax: a hard decode gives the next hop no
                # gradient of confidence, so a wrong first hop is silently
                # asserted rather than hedged. `hop_sharpness` is the dial
                # between the two, and high enough approaches argmax.
                relation = self._relation_at(depth)
                if relation >= 0:
                    # THE TYPED HOP, ARCHITECTURE row D3.
                    #
                    # The soft mixture is what NAMES the concept -- decision 154
                    # measured it landing at cosine 0.96 on a single row -- so
                    # decoding it costs an argmax and loses little. The pair key
                    # then addresses `(relation, that concept)`, which is the
                    # same address a `RELATION subject object` fact wrote.
                    #
                    # This is where the softness is spent rather than kept: a
                    # pair key is formed from two token IDS, so the relation
                    # cannot be blended in the way `weights @ wk` blends
                    # concepts. Naming that as a cost -- the hop is hard here
                    # where it was soft before, and a wrong decode is asserted
                    # rather than hedged, which is exactly what the comment
                    # below warns about for the untyped path.
                    hop_key = self.key_source.pair(
                        relation, int(np.argmax(weights)))
                else:
                    hop_key = weights @ self.wk
                fetched = self.retrieval.read(readable, hop_key)
                if (self.config.index_at_hops and occupied is not None
                        and self.content is not None
                        and occupied.count(hop_key) <= 0.0):
                    # A DEAD END, and the only place this fans out.
                    #
                    # The hop named a concept and nothing was ever written at
                    # it. Rather than return noise -- which is what an ungated
                    # hop does here, and what note 044's refusal was really
                    # about -- ask the index which concepts are like the one we
                    # landed on, and take the first that actually holds
                    # something.
                    #
                    # FIRST that holds something, not best: `nearest` is already
                    # ranked by similarity, so the first hit is the most similar
                    # non-empty candidate. Reading all of them and choosing
                    # would be decision 146's averaging again, and 147 refuted
                    # every rule for choosing among them by magnitude.
                    landed = int(np.argmax(weights))
                    for candidate, _ in self.content.nearest(
                            landed, self.config.index_branches):
                        # THE SAME DEPTH'S RELATION. A neighbour is consulted
                        # because THIS hop's address was empty, so it stands in
                        # for this hop and must be typed the same way -- reading
                        # a neighbour under a different relation would answer a
                        # question nobody asked.
                        if relation >= 0:
                            near = self.key_source.pair(
                                relation, int(candidate))
                        else:
                            near = self.wk[int(candidate)]
                        if occupied.count(near) > 0.0:
                            fetched = self.retrieval.read(readable, near)
                            break
                if self.config.hop_accumulate == "bind":
                    # HOLD BOTH, by binding them into one vector.
                    #
                    # Rescaled to the norm of what was just fetched. An
                    # elementwise product of two small vectors is much smaller
                    # than either -- magnitudes here run around 0.13, so an
                    # unscaled product lands near 0.017 and shrinks again every
                    # hop, which would starve the readout and the delta rule
                    # both. The DIRECTION carries the binding; the magnitude
                    # only has to stay in the range the rest of the model works
                    # in.
                    bound = retrieved * fetched
                    size = float(np.linalg.norm(bound))
                    if size > 0.0:
                        bound *= float(np.linalg.norm(fetched)) / size
                    retrieved = bound
                else:
                    retrieved = fetched
                latest = fetched
                if per_hop is not None:
                    per_hop.append(retrieved)

            # WHICH HOP TO READ FROM, decided per group from its own slice.
            #
            # A fixed `hops` has to match the question exactly -- decision 85
            # measured overshoot at 0.000 in every direction -- so a model that
            # does not know how deep a question is cannot use one. Decision 86
            # measured where the signal to decide lives: NOT in confidence,
            # which does not separate at all (every d' <= 1.01, and the model is
            # 0.94-confident after walking off the end), but in the CONTENT --
            # the first hop past the end lands on a structural marker 100% of
            # the time and an on-chain hop never does.
            #
            # So the gate is a LINEAR score on the retrieval, which is the shape
            # that measurement says is available, and a SOFTMAX over hops rather
            # than a stopping rule. A mixture is differentiable, so the gate
            # trains from the readout's own error and needs no halting label --
            # and there is none to be had, because the depth of a question is
            # not written anywhere in it.
            #
            # Mixing the RETRIEVALS, not the predictions. For a linear readout
            # those are the same thing, since `Wo @ sum(g_k r_k)` is
            # `sum(g_k Wo @ r_k)`, and mixing the inputs leaves every line below
            # untouched. They are NOT the same through a hidden layer, which is
            # why the two are refused together rather than silently averaged.
            #
            # Per group, and the softmax is per group too: group g scores its
            # own slice of each hop and weighs its own hops. Nothing crosses a
            # group, and groups may disagree because the answer sums over them.
            if self.config.hop_accumulate == "concat":
                # Every hop's slice for this group, side by side. Per group
                # still -- a group sees its OWN dimensions across all hops and
                # no other group's, so the locality argument is unchanged and
                # only the width of each group's view grows.
                sliced = np.concatenate(
                    [r.reshape(groups, -1) for r in per_hop], axis=1)
            elif per_hop is None:
                sliced = retrieved.reshape(groups, -1)
            else:
                # GAIN, because the first version of this gate was INERT.
                # Measured: the learned vector reached norm 0.089 against
                # retrieval slices of ~0.13, so scores were ~0.01 and a 2-way
                # softmax over them put 0.5020 on hop 1 for depth-1 questions
                # and 0.5000 for depth-2. The direction was right and the
                # magnitude was 0.2%. Same shape as the unsharpened hop decode:
                # a correct signal flattened into a uniform average.
                #
                # Applied to the gradient as well, a few lines down, or the
                # chain rule is wrong and the gate learns at the wrong rate.
                # HOP k IS SCORED BY WHAT HOP k+1 RETURNS, which is the only
                # thing the signal actually supports.
                #
                # Scoring hop k by its own content was measured and is only half
                # a mechanism: it took depth-1 questions to 1.000 and left
                # depth-2 at 0.547. Decision 86's signal separates PAST THE END
                # from ON THE CHAIN, and for a depth-2 question hop 1 is `b` and
                # hop 2 is `c` -- both on the chain, both chain symbols, nothing
                # to tell them apart by. The gate split them and averaged.
                #
                # What the signal does support is "the last hop before the first
                # marker". So hop k is weighed by hop k+1: if the NEXT retrieval
                # has walked off the end, this one is the answer. Still one
                # linear score on one retrieval, still inside a group.
                ahead = np.stack([r.reshape(groups, -1) for r in per_hop[1:]])
                stack = np.stack([r.reshape(groups, -1) for r in per_hop[:-1]])
                rule = self.halt_w
                if self.halt_select is not None:
                    # THE POSITION SELECTS WHICH RULE TO APPLY.
                    #
                    # Decision 95: one gate cannot serve both jobs. In the body
                    # the next token is one hop away and the gate should take
                    # hop 1; at a query the answer is several hops out and it
                    # should not. Measured on a gate trained answer-only, it
                    # puts 0.0171 on hop 1 at the query -- correct -- and 0.4712
                    # in the body, a coin flip where serving the body needs ~1.0.
                    # It is CONFLICTED, not outvoted, so no reweighting helps.
                    #
                    # ADDING a key term would do nothing. The key is the same
                    # for every hop at a position, so it shifts all of this
                    # position's scores equally and the softmax removes it
                    # exactly -- the same way a constant perturbation of
                    # `grouped_wo` turned out to be invisible to the decode.
                    # The key has to MODULATE, not contribute.
                    #
                    # So it picks between two rules: `halt_w` and
                    # `halt_w + halt_alt`, blended by a scalar the current key
                    # decides. One scalar per group, from that group's own slice
                    # of the key, so nothing crosses a group.
                    chosen = 1.0 / (1.0 + np.exp(-np.einsum(
                        "gd,gd->g", self.halt_select, key.reshape(groups, -1))))
                    rule = self.halt_w + chosen[:, None] * self.halt_alt
                scores = self.config.gate_sharpness * np.einsum(
                    "gd,kgd->kg", rule, ahead)
                scores = scores - scores.max(axis=0, keepdims=True)
                gate = np.exp(scores)
                gate /= gate.sum(axis=0, keepdims=True)
                sliced = np.einsum("kg,kgd->gd", gate, stack)
            if self.hidden_w is None:
                parts = np.einsum("vgd,gd->gv", self.grouped_wo, sliced)
            else:
                # Group g's own two matrices. `active` is (groups, hidden).
                active = np.maximum(
                    0.0, np.einsum("ghd,gd->gh", self.hidden_w, sliced))
                parts = np.einsum("vgh,gh->gv", self.grouped_wo, active)
            # WHICH HOP TO READ FROM, decided per group from its own slice.
            #
            # A fixed `hops` has to match the question exactly: decision 85
            # measured overshoot at 0.000 in every direction, so a model that
            # does not know how deep a question is cannot use one. Decision 86
            # measured where the signal to decide is: NOT in confidence, which
            # does not separate at all (every d' <= 1.01, and the model is
            # 0.94-confident after walking off the end), but in the CONTENT --
            # the first hop past the end lands on a structural marker 100% of
            # the time and an on-chain hop never does.
            #
            # So the gate is a linear score on the retrieval, which is exactly
            # the shape that measurement says is available, and a SOFTMAX over
            # hops rather than a stopping rule: a mixture is differentiable, so
            # the gate trains from the readout's own error with no halting
            # label, and there is no label to be had -- the depth of a question
            # is not marked anywhere in it.
            #
            # PER GROUP, and the softmax is per group too, so group g scores
            # group g's slice of each hop and weighs its own hops. Nothing
            # crosses a group, and groups are free to disagree because the
            # answer sums over them anyway.
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
                error = target - parts
                if self.halt_w is not None:
                    # THE GATE LEARNS FROM THE READOUT'S OWN ERROR.
                    #
                    # `sliced` is the gate-weighted mixture, so the error
                    # reaches the gate through it: carry the error back to the
                    # mixture, ask each hop how much it agrees with that
                    # direction, and the softmax derivative turns those into a
                    # score update. A hop that points where the error wants to
                    # go gains weight and the rest lose it.
                    #
                    # Every term is group g's own: its error, its readout
                    # columns, its slice of each hop. No group reads another's,
                    # which is the same argument the hidden layer's gradient
                    # makes one layer down.
                    # `agree` asks how much each hop's READ points where the
                    # error wants to go; the update lands on `ahead`, because
                    # that is what the score was computed from. Mixing the two
                    # up would train the gate on the wrong vector and still
                    # descend, which is the kind of wrong that produces a curve.
                    if self.config.gate_objective == "which_hop":
                        # WHICH HOP WOULD HAVE BEEN RIGHT HERE?
                        #
                        # Each hop's own readout either names the target or does
                        # not, and that is decidable at a scored position with
                        # nothing the group does not already hold. The hops that
                        # got it right are the label; the gate is pushed toward
                        # them and away from the rest.
                        #
                        # When NO hop is right there is nothing to teach -- the
                        # answer was not reachable at any depth, so any label
                        # would be inventing one -- and the gate is left alone.
                        # `where` keeps that per group rather than per position,
                        # because groups disagree and one group's silence should
                        # not silence the others.
                        every = np.einsum("vgd,kgd->kgv", self.grouped_wo,
                                          stack)
                        hit = (every.argmax(axis=2) == targets[t]).astype(float)
                        total = hit.sum(0, keepdims=True)
                        step = np.where(total > 0.0, hit / np.maximum(
                            total, 1e-12) - gate, 0.0)
                    else:
                        back = np.einsum("gv,vgd->gd", error, self.grouped_wo)
                        agree = np.einsum("gd,kgd->kg", back, stack)
                        step = gate * (
                            agree - (gate * agree).sum(0, keepdims=True))
                    rate = self.config.lr * self.config.gate_sharpness
                    shared = np.einsum("kg,kgd->gd", step, ahead)
                    self.halt_w += rate * shared
                    if self.halt_select is not None:
                        # `halt_alt` enters the rule scaled by `chosen`, so its
                        # gradient carries the same factor. `halt_select` gets
                        # the error through `chosen`, which is where the KEY
                        # finally reaches the gate -- the sigmoid's own
                        # derivative and then the key slice.
                        self.halt_alt += rate * chosen[:, None] * shared
                        through = np.einsum("kg,kgd,gd->g", step, ahead,
                                            self.halt_alt)
                        self.halt_select += (
                            rate * (through * chosen * (1.0 - chosen))[:, None]
                            * key.reshape(groups, -1))
                if self.hidden_w is None:
                    update = np.einsum("gv,gd->vgd", error, sliced)
                else:
                    # BACKPROPAGATION, CONFINED TO ONE GROUP.
                    #
                    # Group g's hidden gradient uses group g's own output
                    # weights and group g's own error. Nothing crosses a group,
                    # so a node computing this needs only what it already holds.
                    update = np.einsum("gv,gh->vgh", error, active)
                    through = np.einsum("gv,vgh->gh", error, self.grouped_wo)
                    self.hidden_w += self.config.lr * np.einsum(
                        "gh,gd->ghd", through * (active > 0.0), sliced)
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
                    if self.config.value_centre:
                        # REMOVE THE SHARED DRIFT. The update above is the right
                        # gradient and it still collapses (decision 94): `Wv`
                        # and `Wo` co-adapt with nothing holding the values
                        # apart, and every target moves toward a direction `Wo`
                        # picks -- so they all move the SAME way. Measured, the
                        # collapse is directional, not magnitude: cosine among
                        # ordinary tokens rose to 0.382 while accuracy fell to
                        # 0.025.
                        #
                        # Centring subtracts whatever the value vectors have in
                        # common, which is exactly the component that drift
                        # accumulates. It cannot stop two tokens converging for
                        # a REASON -- that is the representation learning this
                        # is for -- it only removes the part every token shares.
                        self.wv -= self.wv.mean(axis=0)
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
            # Carried alongside `previous_key` rather than recomputed from
            # `t - 1`, for the same reason `previous_key` is: the pair a key
            # came from is the key source's business, and an index recomputed
            # here would be a second implementation of it.
            previous_concept = self.surfaces.of(
                self.key_source.concept(tokens, t))
            previous_retrieval = retrieved
            if self.config.tag_relative:
                previous_store_size = float(np.linalg.norm(memory))
            previous_key_for_retrieval = key
            previous_scores = answer
        if self.config.carry_store:
            self._carried = memory
        # THE FINAL STORE, KEPT FOR READ-ONLY PROBES, and deliberately not the
        # same thing as `carry_store`.
        #
        # `carry_store` feeds a store into the NEXT sequence and changes what the
        # model learns from. This only records what THIS sequence ended up
        # holding, and nothing in `run` ever reads it — a set-valued question is
        # asked once every fact is in, so `answer_set` reads it afterwards rather
        # than needing a hook inside the loop.
        #
        # Decision 62 found a persistence bug by noticing that `learn=False`
        # predictions were byte-identical whether or not another sequence had run,
        # so the line between "carried" and "merely visible" is worth keeping
        # sharp: this is assigned on every run and read by nothing here.
        self._final = memory if lasting is None else memory + lasting
        #: THE PARTITIONED STORE, on the same terms as `_final` above, and it is
        #: not the same object.
        #:
        #: `_final` is whichever node's matrix the last write happened to point
        #: at — a VIEW, per the routing comment above — so under
        #: `concept_nodes` it holds one node's bindings and looks exactly like a
        #: whole store that lost most of its facts. A probe reading it would
        #: report a partitioned model as catastrophically worse and be measuring
        #: the wrong object.
        #:
        #: `search.py` accepts either a matrix or something with `.matrix()`, so
        #: a traversal over the real thing needs the store itself. None when
        #: partitioning is off, which is what a caller should branch on.
        self._concepts = concepts
        return predictions

    def _cliff_candidates(self, entity: int, look: int) -> list[int]:
        """The entity and the neighbours before the biggest similarity drop.

        The contract: returns `entity` followed by however many of its nearest
        neighbours sit above the largest gap in the index's ranked cosines.

        **This replaces a fitted count with an argmax over gaps**, which is the
        same move decision 148 made when it replaced a tuned membership threshold
        with a structurally-zero read. Nothing here is compared against a
        constant: the rule asks where the ranking falls off, not whether a
        similarity clears a bar.

        `look` is a CEILING, not a target, and that distinction is the whole
        improvement over `branches`. Being generous costs nothing — extra
        candidates sit below the cliff and are cut — where `branches` had to be
        exactly right and destroyed the answer when it was off by one
        (decision 167). It still has to EXCEED the group, so it is not free.

        Measured on `families.py`: siblings sit at cosine 0.947–0.970 and
        strangers at 0.438–0.585, so the gap at the boundary is ~0.45 against
        within-family steps of ~0.01. **That margin is a property of a task
        calibrated to make families recoverable**, and a noisier grouping narrows
        it. This is not a claim that the rule survives a bad index.
        """
        ranked = self.content.nearest(entity, look)
        if len(ranked) < 2:
            return [int(entity)] + [int(token) for token, _ in ranked]
        sims = [score for _, score in ranked]
        gaps = [sims[i] - sims[i + 1] for i in range(len(sims) - 1)]
        keep = max(range(len(gaps)), key=gaps.__getitem__) + 1
        return [int(entity)] + [int(token) for token, _ in ranked[:keep]]

    def answer_set(self, relation: int, entity: int,
                   branches: int | None = None, look: int = 8) -> frozenset[int]:
        """Every value the store holds about `entity`'s neighbourhood, as a SET.

        The contract: call after `run`. Returns the decoded value at `entity`'s
        own address and at each of its `branches` nearest neighbours in the
        content index, **skipping every address the occupancy sketch says was
        never written.** The result is a set, so order and repetition carry no
        meaning.

        **This COLLECTS where decisions 146 and 147 tried to CHOOSE, and that is
        the whole reason it can work.** 146 found that reading neighbours through
        the index can only average rather than select, and 147 refuted both
        obvious rules for choosing a winner among them. Neither objection applies
        to a set answer: nothing has to be selected, so the mechanism that was
        wrong for a one-token answer is the right shape for this one.

        Precision comes from the gate and costs nothing fitted — an address that
        was never written reads exactly 0.0 (decision 148), so an empty neighbour
        contributes nothing rather than contributing noise.

        **How many neighbours to consider is the other half, and there are two
        answers.** `branches` as an integer fixes the count, which is decision
        167's finding: it has to equal the group size and collapses either side.
        **`branches=None` derives it from the biggest gap in the index's ranked
        similarities** — see `_cliff_candidates` — which turns the constant from a
        target into the ceiling `look`, where being generous costs nothing.

        The fixed form is kept rather than replaced, under rule 14c: it is the
        measured comparison for the gap rule, and a refutation that cannot be
        re-run is a refutation nobody can date.
        """
        if self._final is None:
            raise ValueError(
                "answer_set reads the store this model ended a sequence with, "
                "and no sequence has been run. Calling it first would score a "
                "zero matrix, which decodes to whatever the readout prefers and "
                "looks exactly like a mechanism that found nothing")
        if self.occupied is None:
            raise ValueError(
                "answer_set needs the occupancy sketch: without it an unwritten "
                "address returns noise that decodes to a real token, so every "
                "neighbour would contribute a value and the answer would be as "
                "large as `branches` regardless of what was stored. Set "
                "track_occupancy or index_prefer to 'sketch' or 'inherit'")
        if self.content is None:
            raise ValueError(
                "answer_set needs a fitted ContentIndex to propose neighbours. "
                "Without one it can only read the entity's own address, which is "
                "the single-token measurement")
        if branches is not None and branches < 1:
            raise ValueError(
                "branches must be at least 1, or no neighbour is ever consulted "
                "and the answer cannot exceed one value -- the singleton case "
                "decision 166 refuses at the task level. Pass None for the gap "
                "rule, which chooses the count itself")
        if look < 2:
            raise ValueError(
                "look must be at least 2: the gap rule needs two similarities to "
                "have a gap between them, and with one candidate there is no "
                "ranking to find a cliff in")
        # THE ENTITY ITSELF FIRST, then its neighbours. Its own address is where a
        # DIRECT fact lives, and a set answer needs it alongside the siblings'
        # rather than instead of them.
        if branches is None:
            candidates = self._cliff_candidates(entity, look)
        else:
            candidates = [int(entity)]
            candidates.extend(
                int(token) for token, _ in self.content.nearest(entity, branches))
        found: set[int] = set()
        for candidate in candidates:
            key = self.key_source.pair(int(relation), candidate)
            # THE GATE, and it is the only thing standing between this and an
            # answer of size `branches + 1`.
            if self.occupied.count(key) <= 0.0:
                continue
            retrieved = self.retrieval.read(self._final, key)
            found.add(int(np.argmax(self.wo @ retrieved)))
        return frozenset(found)
