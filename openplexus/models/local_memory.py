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
            store: np.ndarray | None = None, leave=None) -> np.ndarray:
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

        for t, token in enumerate(tokens):
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
            if previous_key is not None and (store is None or store[t]):
                if self.config.decay < 1.0:
                    memory *= self.config.decay
                memory += np.outer(value, previous_key)

            # CONSOLIDATE. The prediction made one step ago was a guess at the
            # token that has just arrived, so this is where it gets marked right
            # or wrong -- self-supervised, local, and available to any node.
            #
            # What gets promoted is the retrieved vector itself rather than the
            # binding that produced it. A superposed memory cannot name which of
            # its bindings answered; it can only be asked again and told whether
            # the answer held up. Promoting the answer is the operation that is
            # actually available.
            if lasting is not None and previous_retrieval is not None:
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
                    if len(slots) < self.config.capture_slots:
                        slots.append((strength, contribution))
                        lasting += contribution
                    else:
                        weakest = min(range(len(slots)),
                                      key=lambda i: slots[i][0])
                        if strength > slots[weakest][0]:
                            # Displacement, not addition. Subtracting the loser
                            # is what holds N at k -- without it this is a
                            # threshold gate with extra bookkeeping.
                            lasting -= slots[weakest][1]
                            slots[weakest] = (strength, contribution)
                            lasting += contribution
                elif fires:
                    lasting += self.config.consolidation * np.outer(
                        previous_retrieval, previous_key_for_retrieval)
                    if self.config.lasting_cap:
                        # Scale the whole store, never one entry. Editing
                        # individual weights to fit a budget would be a
                        # different mechanism and a non-local one.
                        size = float(np.linalg.norm(lasting))
                        if size > self.config.lasting_cap:
                            lasting *= self.config.lasting_cap / size

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
