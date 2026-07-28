"""Verify that the tests can fail.

A passing test is not evidence. This harness breaks a named mechanism on
purpose, runs the suite, and requires it to go red. A mutation the suite does
*not* catch marks a vacuous region of the test set — the tests there are
asserting something other than what they claim (CLAUDE.md rule 10).

It also fails loudly when it goes stale. If a refactor moves a line a mutation
targets, the mutation is reported as SOURCE MOVED and the run fails, rather than
quietly passing because nothing was mutated.

    python tools/mutate.py

Safety: the original file is written to a sibling `.bak` before any edit, so an
interrupted run is recoverable from disk. `try`/`finally` does not run when the
process is killed, and a timeout will eventually leave an edit in the working
tree — so on startup any leftover `.bak` is restored before anything else.
"""

from __future__ import annotations

import os
import subprocess
import sys
from dataclasses import dataclass
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
MQAR = ROOT / "openplexus" / "tasks" / "mqar.py"
CHAINS = ROOT / "openplexus" / "tasks" / "chains.py"
BASELINES = ROOT / "openplexus" / "baselines.py"
INDUCTION = ROOT / "openplexus" / "models" / "induction.py"
ATTENTION = ROOT / "openplexus" / "models" / "attention.py"
LOCAL = ROOT / "openplexus" / "models" / "local_memory.py"
DISTRIBUTED = ROOT / "openplexus" / "distributed.py"
KEYS = ROOT / "openplexus" / "keys.py"
RETRIEVAL = ROOT / "openplexus" / "retrieval.py"
SPLIT = ROOT / "experiments" / "g6_01_forgetting.py"
# Experiment code is not usually mutated -- experiments are read once and
# discarded. This one is, because its generator returned a wrong SET rather
# than crashing, which is how a sweep becomes a confident wrong answer.
CHURN = ROOT / "experiments" / "g4_02_machine_churn.py"
TRANSPORT = ROOT / "openplexus" / "transport.py"
DEPLOYMENT = ROOT / "openplexus" / "deployment.py"
NODE_MAIN = ROOT / "openplexus" / "node_main.py"
NGRAM = ROOT / "openplexus" / "ngram.py"
REWARD_RECALL = ROOT / "openplexus" / "tasks" / "reward_recall.py"
CORPUS = ROOT / "openplexus" / "tasks" / "corpus.py"
SLOT_COST = ROOT / "tools" / "slot_cost.py"
RECOVERY = ROOT / "tools" / "recovery.py"
TESTBED = ROOT / "testbed" / "run.py"


@dataclass(frozen=True)
class Mutation:
    """One deliberate defect.

    Attributes:
        name: Short identifier shown in the report.
        breaks: What mechanism this disables, in the terms a reader would use.
        path: File to edit.
        old: Exact source text to replace. Must appear exactly once.
        new: What to replace it with.
    """

    name: str
    breaks: str
    path: Path
    old: str
    new: str


MUTATIONS = [
    Mutation(
        name="rewarded-cues-are-not-chosen-uniformly",
        breaks="the property the whole g9 line rests on -- that a rewarded "
               "binding is statistically identical to an unrewarded one until "
               "the reward arrives. Taking the FIRST n cues instead of a random "
               "sample makes reward predictable from position, so a gate could "
               "score by reading the layout, and every recovery number would be "
               "measuring that instead",
        path=REWARD_RECALL,
        old="    rewarded = rng.sample(cues, config.n_rewarded)",
        new="    rewarded = cues[:config.n_rewarded]",
    ),
    Mutation(
        name="the-reward-lands-at-the-wrong-offset",
        breaks="the delay, which is the dial the task exists for. Placing the "
               "reward token one step from its binding regardless of `delay` "
               "makes every delay the trivial case g9-02 described, and the "
               "cliff g9-03 measured would vanish",
        path=REWARD_RECALL,
        old="            reward_due[position + config.delay] = config.reward_token",
        new="            reward_due[position + 1] = config.reward_token",
    ),
    Mutation(
        name="the-corpus-vocabulary-comes-from-the-test-text",
        breaks="the leak `corpus.py` is written to prevent. A symbol appearing "
               "only in the test set would get its own index, so the model is "
               "scored over a vocabulary it never had reason to predict -- "
               "which lowers cross-entropy and reads as a better model",
        path=CORPUS,
        old="    for name in train_names:\n"
            "        for character in texts[name]:",
        new="    for name in sorted(texts):\n"
            "        for character in texts[name]:",
    ),
    Mutation(
        name="the-stream-split-overlaps",
        breaks="the disjointness of a single-stream corpus. An off-by-one "
               "overlap puts the same characters on both sides, and the test "
               "figure improves for a reason that has nothing to do with the "
               "model",
        path=CORPUS,
        old="    head, tail = text[:cut], text[cut:]",
        new="    head, tail = text[:cut + 1], text[cut:]",
    ),
    Mutation(
        name="corrective-writes-forget-to-subtract",
        breaks="the whole mechanism, leaving Hebbian storage scaled by the "
               "key's norm. Rebinding accumulates again, and because a uniform "
               "rescaling of the store does not move an argmax, the model "
               "still RUNS and still predicts -- it simply stops replacing",
        path=LOCAL,
        old="                        error = value - memory @ previous_key",
        new="                        error = value",
    ),
    Mutation(
        name="corrective-writes-skip-the-normalisation",
        breaks="the exactness. Without dividing by the key's squared norm the "
               "write lands on `value * (key @ key)` rather than on `value`, "
               "so how much of a binding is stored depends on which key it was "
               "stored under -- and every key differs",
        path=LOCAL,
        old="                                   * np.outer(error, previous_key) / scale)",
        new="                                   * np.outer(error, previous_key))",
    ),
    Mutation(
        name="the-reward-gate-never-prunes",
        breaks="the gate. Everything written stays written, so the fast store "
               "accumulates one binding per step exactly as it did ungated -- "
               "which is what six previous mechanisms did wrong",
        path=LOCAL,
        old="                    if index not in protected:",
        new="                    if False:",
    ),
    Mutation(
        name="the-reward-window-is-ignored",
        breaks="the reach of the gate, pinning it to the step immediately "
               "before the reward. That is 'keep the thing before the obvious "
               "marker', which learns nothing about value and is exactly the "
               "trivial case the delay dial exists to avoid",
        path=LOCAL,
        old="                    keep = self.config.reward_window + 1",
        new="                    keep = 1",
    ),
    Mutation(
        name="pending-contributions-do-not-fade",
        breaks="the bookkeeping. A contribution is removed as it went in rather "
               "than as it now stands, so the subtraction takes out more than "
               "is there and drives the store negative -- silently, since "
               "nothing checks the store's sign",
        path=LOCAL,
        old="                    _fade(pending, self.config.decay)",
        new="                    pass",
    ),
    Mutation(
        name="the-fast-store-cap-never-binds",
        breaks="the compensatory process on the fast store, restoring the "
               "geometric runaway: repetition drives the store toward "
               "1/(1-decay), retrieval is linear in that and the readout update "
               "is quadratic, so it reaches NaN",
        path=LOCAL,
        old="    if size > cap:",
        new="    if False:",
    ),
    Mutation(
        name="the-fast-store-cap-clips-entries",
        breaks="synaptic scaling on the fast store. Clipping individual weights "
               "to a budget is a different mechanism and a non-local one -- it "
               "inspects entries rather than a single total. Already pinned for "
               "the lasting store; the same distinction applies here",
        path=LOCAL,
        old="        store *= cap / size",
        new="        np.clip(store, -1e-3, 1e-3, out=store)",
    ),
    Mutation(
        name="departed-nodes-are-not-read-from",
        breaks="causality and liveness at once. A departure stops a node being "
               "SENT to; it cannot un-send a vote already transmitted. Dropping "
               "departed nodes from the read set discards answers still sitting "
               "in their sockets, so a step never reaches its expected count "
               "and the run times out BEFORE the departure it was testing. "
               "Invisible at window 1, where nothing is ever in flight across "
               "the departure -- which is why every in-process test passed and "
               "the container testbed found it on the first run combining C1 "
               "asynchrony with C3 churn",
        path=DISTRIBUTED,
        old="            live = [sock for i, sock in enumerate(self._connections)\n"
            "                    if i not in dead]",
        new="            live = [sock for i, sock in enumerate(self._connections)\n"
            "                    if i not in dead and i not in gone]",
    ),
    Mutation(
        name="capture-never-displaces",
        breaks="the only thing the pool is for. Without subtracting the loser "
               "this is a threshold gate with extra bookkeeping: N grows with "
               "sequence length again, which is exactly the failure g8-01 "
               "measured",
        path=LOCAL,
        old="                            lasting -= slots[index][1]",
        new="                            pass",
    ),
    Mutation(
        name="capture-admits-everything",
        breaks="the competition. A newcomer would evict an incumbent whether or "
               "not it was stronger, so the pool holds the most RECENT k rather "
               "than the best k -- a different policy, and one nothing here "
               "argued for",
        path=LOCAL,
        old="    return weakest if strength > strengths[weakest] else None",
        new="    return weakest",
    ),
    Mutation(
        name="capture-evicts-the-strongest",
        breaks="which end of the pool loses, so the store fills with the "
               "weakest traces it has seen",
        path=LOCAL,
        old="    weakest = min(range(len(strengths)), key=lambda i: strengths[i])",
        new="    weakest = max(range(len(strengths)), key=lambda i: strengths[i])",
    ),
    Mutation(
        name="the-tag-admits-the-strongest",
        breaks="the direction, which is the whole finding. g9-04 measured "
               "retrieval strength separating a binding-write from a "
               "filler-write at AUC 0.22 -- BELOW 0.5, so inverted. Admitting "
               "the strongest is what competitive capture already did, and it "
               "is the failure this mechanism is the correction to",
        path=LOCAL,
        old="    rank = strength if strongest else -strength",
        new="    rank = strength",
    ),
    Mutation(
        name="the-mark-never-fades",
        breaks="ageing. Without it the tag ranks the whole interval at once, "
               "and the weakest retrievals it will ever see are the writes made "
               "when the store was nearly empty -- so the pool fills with the "
               "first few writes after every capture. Measured: the same 8 "
               "bindings out of 32 captures at every capacity and every delay",
        path=LOCAL,
        old="                            tagged[:] = [(fade(rank, self.config.tag_decay),",
        new="                            tagged[:] = [(rank,",
    ),
    Mutation(
        name="the-fade-entrenches-instead-of-releasing",
        breaks="which way a mark ages. `admit` keeps the largest rank, so "
               "fading means the rank FALLS -- and the arithmetic that does "
               "that depends on the sign. Multiplying both ends releases one "
               "and makes the other immortal. This was the first version, and "
               "it produced numbers identical to no fade at every setting",
        path=LOCAL,
        old="    return rank * factor if rank > 0 else rank / factor",
        new="    return rank * factor",
    ),
    Mutation(
        name="the-newest-marks-are-the-oldest",
        breaks="which end of the marks survives. Taking the first instead of "
               "the last makes the arm keep whatever was written just after the "
               "previous capture, which is a recency policy pointing backwards "
               "and is exactly what an un-faded tag already does",
        path=LOCAL,
        old="                        marked = marked[-self.config.tag_newest:]",
        new="                        marked = marked[:self.config.tag_newest]",
    ),
    Mutation(
        name="the-reward-step-write-is-not-excluded",
        breaks="the one thing that makes the arm measure anything. The write "
               "made AT a capture binds the previous token to the reward token, "
               "is always the most recent, and is never what a reward vouches "
               "for -- so without excluding it the arm keeps that write every "
               "time and scores zero. That is what the first version did",
        path=LOCAL,
        old="                        marked = [i for i in marked if i != wrote_at]",
        new="                        marked = list(marked)",
    ),
    Mutation(
        name="the-write-index-is-read-after-the-gate",
        breaks="the trace, silently. `pending` is emptied by the reward gate, "
               "so reading the index there reports -1 at every capture step and "
               "the write made at that step becomes invisible to every probe. "
               "Harmless when a capture keeps thirty writes; it made the "
               "newest-mark arm report zero kept",
        path=LOCAL,
        old="                    wrote_at = len(pending) - 1",
        new="                    wrote_at = -1",
    ),
    Mutation(
        name="the-combined-gate-forgets-the-tag",
        breaks="the union, leaving a plain window. A write the tag marked and "
               "the window did not is discarded, so a gate configured to read "
               "both signals reads one -- and scores like the window, which is "
               "a number that looks entirely reasonable",
        path=LOCAL,
        old="                    protected |= set(\n"
            "                        range(max(0, len(pending) - keep), len(pending)))",
        new="                    protected = set(\n"
            "                        range(max(0, len(pending) - keep), len(pending)))",
    ),
    Mutation(
        name="the-combined-gate-intersects",
        breaks="which set operation combines the two mechanisms. An "
               "intersection keeps only what BOTH claimed, which is a policy "
               "nothing here argued for and which is strictly more selective "
               "than either alone -- the opposite of the intended trade",
        path=LOCAL,
        old="                    protected |= set(\n"
            "                        range(max(0, len(pending) - keep), len(pending)))",
        new="                    protected &= set(\n"
            "                        range(max(0, len(pending) - keep), len(pending)))",
    ),
    Mutation(
        name="the-tag-ranks-on-raw-strength",
        breaks="the normalisation, restoring the confound it exists to remove. "
               "A retrieval's size scales with the store's size, so an "
               "un-normalised tag reads every write made just after a capture "
               "as weak and fills with them. Measured: rewarded-binding capture "
               "falls from 10 of 32 to 3 of 32 at slots 8",
        path=LOCAL,
        old="                        if self.config.tag_relative and previous_store_size:",
        new="                        if False:",
    ),
    Mutation(
        name="the-relative-tag-divides-by-the-wrong-store",
        breaks="WHICH store the strength is relative to. Using the norm after "
               "this step's write divides by a store that already contains the "
               "write being ranked, so the quantity stops being 'weak for the "
               "store that produced this retrieval' and silently becomes "
               "something with no name",
        path=LOCAL,
        old="            if self.config.tag_relative:\n"
            "                previous_store_size = float(np.linalg.norm(memory))",
        new="            if self.config.tag_relative:\n"
            "                previous_store_size = 1.0",
    ),
    Mutation(
        name="the-tag-outlives-its-capture",
        breaks="the one invariant the indices rest on. `tagged` holds positions "
               "in `pending`, which empties at every reward, so a mark that "
               "survives its capture points into the NEXT interval and protects "
               "whatever landed at those positions -- a gate keeping the "
               "earliest writes after a reward, which looks fine from outside",
        path=LOCAL,
        old="                tagged.clear()",
        new="                pass",
    ),
    Mutation(
        name="the-tag-never-displaces",
        breaks="the competition, for the tag rather than for the lasting pool. "
               "A winning candidate is computed and then dropped, so the pool "
               "holds the first k marks of each interval whatever arrives after",
        path=LOCAL,
        old="        tagged[slot] = (rank, index)",
        new="        pass",
    ),
    Mutation(
        name="the-node-ignores-the-decoder-switch",
        breaks="the only thing that makes nodes distinguishable. wo is learned "
               "and starts at zeros, so without it every node predicts token 0 "
               "forever, a departure changes nothing, and a latency experiment "
               "produces clean meaningless curves",
        path=NODE_MAIN,
        old='    if os.environ.get(DECODER_VAR) == "1":',
        new="    if False:",
    ),
    Mutation(
        name="every-node-claims-the-first-slice",
        breaks="slice assignment, so the network covers a quarter of its width "
               "four times over instead of covering all of it once",
        path=NODE_MAIN,
        old="    own = slices_for(config.d_model, nodes)[index]",
        new="    own = slices_for(config.d_model, nodes)[0]",
    ),
    Mutation(
        name="the-node-ignores-the-vocabulary-it-was-given",
        breaks="config plumbing from the environment, which is the entrypoint's "
               "entire job. A node with the wrong vocabulary still runs, still "
               "votes and still produces a number",
        path=NODE_MAIN,
        old='        vocab_size=int(os.environ.get(VOCAB_VAR, "41")),',
        new="        vocab_size=41,",
    ),
    Mutation(
        name="a-failed-tc-lets-the-node-join-anyway",
        breaks="the guarantee that an impaired run was impaired. The node would "
               "join with a clean link and its vote would be recorded as a "
               "latency measurement",
        path=TESTBED,
        old='    return " ".join(parts) + " || exit 3; "',
        new='    return " ".join(parts) + " ; "',
    ),
    Mutation(
        name="the-zipf-law-is-inverted",
        breaks="which end of the alphabet is heavy. The filler would still be "
               "skewed, so any test that only checks concentration passes -- but "
               "the RARE tokens become the common ones, which is the opposite of "
               "the language statistic this mode exists to imitate",
        path=MQAR,
        old="        weights = [1.0 / (rank + 1) ** config.zipf_s",
        new="        weights = [1.0 * (rank + 1) ** config.zipf_s",
    ),
    Mutation(
        name="the-zipf-exponent-is-ignored",
        breaks="the dial. Every setting would give the same distribution, so a "
               "sweep over zipf_s would measure nothing and report it as a "
               "flat line rather than as a broken knob",
        path=MQAR,
        old="        weights = [1.0 / (rank + 1) ** config.zipf_s",
        new="        weights = [1.0 / (rank + 1) ** 1.0",
    ),
    Mutation(
        name="the-host-outranks-the-container",
        breaks="the only thing this module exists for: a container allowed one "
               "core of forty would read the HOST's memory and plan for a "
               "machine it is not running on",
        path=DEPLOYMENT,
        old="              else _cgroup_memory() or _meminfo_available() or ASSUMED_MEMORY)",
        new="              else _meminfo_available() or _cgroup_memory() or ASSUMED_MEMORY)",
    ),
    Mutation(
        name="gated-deployment-takes-one-wide-node",
        breaks="the g7-03 allocation policy, inverting it. Gated, allocation is "
               "worth at most 0.031 and the smallest node reaches the most "
               "devices, which is the whole priority; one wide node abandons it "
               "for a measured gain of nothing",
        path=DEPLOYMENT,
        old="        width = 1\n        basis +=",
        new="        width = capacity\n        basis +=",
    ),
    Mutation(
        name="capacity-need-not-divide-into-nodes",
        breaks="the exact partition, handing slices_for a split it refuses "
               "rather than one it can serve",
        path=DEPLOYMENT,
        old="        capacity -= capacity % width",
        new="        pass",
    ),
    Mutation(
        name="connections-kept-in-arrival-order",
        breaks="the slice handshake, restoring the bug exactly as it shipped: "
               "connections indexed by when they arrived rather than by which "
               "slice they announced. Summing votes is order-independent so no "
               "bit-identity test can see it, and it only bites where a node is "
               "named BY INDEX -- every departure and churn result",
        path=DISTRIBUTED,
        old="        self._connections = [sock for _, sock\n"
            "                             in sorted(pending, key=lambda p: order[p[0]])]",
        new="        self._connections = [sock for _, sock in pending]",
    ),
    Mutation(
        name="driver-accepts-any-slice",
        breaks="the check that the nodes which turned up are the ones asked "
               "for, so a network of the wrong shape runs and reports numbers",
        path=DISTRIBUTED,
        old="        if sorted(k for k, _ in pending) != sorted(order):",
        new="        if False:",
    ),
    Mutation(
        name="surprise-is-the-margin-again",
        breaks="the meaning of surprise, restoring the measure that read only "
               "the best score and the arriving one. It cannot see learning "
               "suppress the alternatives, and it grows with the size of the "
               "scores, so a filling memory reports rising surprise while its "
               "predictions improve. Shipped, published a conclusion, and was "
               "caught by John asking why a repeat was not getting less "
               "surprising -- not by any test here, which is why the meaning "
               "tests exist",
        path=LOCAL,
        old="    return -float(np.log(weights[token] / weights.sum() + 1e-12))",
        new="    return float(scores.max() - scores[token])",
    ),
    Mutation(
        name="salience-fires-on-one-tail-only",
        breaks="the two-tailed rule, leaving only surprise and dropping the very-good outcomes John's framing turns on",
        path=LOCAL,
        old="                             and abs(step_surprise - mean_surprise)",
        new="                             and (step_surprise - mean_surprise)",
    ),
    Mutation(
        name="the-cap-never-binds",
        breaks="the compensatory process, restoring the divergence that made a salience gate reach NaN",
        path=LOCAL,
        old="    if not cap:",
        new="    if False:",
    ),
    Mutation(
        name="the-cap-edits-entries-instead-of-scaling",
        breaks="synaptic scaling: clipping individual weights is a different and non-local mechanism",
        path=LOCAL,
        old="    size = float(np.linalg.norm(store))",
        new="    size = cap",
    ),
    Mutation(
        name="votes-lose-their-step",
        breaks="reassembly -- votes would be matched to steps by arrival "
               "order, so running ahead would scramble which answer belongs "
               "to which position, and only at windows above 1",
        path=DISTRIBUTED,
        old='                (step,) = struct.unpack("!i", message[:4])',
        new="                step = settled",
    ),
    Mutation(
        name="nodes-keep-memory-between-sequences",
        breaks="the per-sequence contract -- the REAL bug this harness was "
               "extended to cover, found when a departure test showed answers "
               "changing before the departure step, which a departure cannot do",
        path=DISTRIBUTED,
        old="            if token == _RESET:\n                node.reset()",
        new="            if token == _RESET:\n                pass",
    ),
    Mutation(
        name="driver-ignores-a-node",
        breaks="the sum -- votes overwrite instead of accumulating, so "
               "every node but the last to answer is silently discarded. "
               "The previous form of this mutation scaled every vote by "
               "one half, which survived because a uniform positive scale "
               "cannot change an argmax: it was a false mutation testing "
               "nothing, not a vacuous test",
        path=DISTRIBUTED,
        old='                slot[0] += np.frombuffer(message[4:], dtype=">f8")',
        new='                slot[0] = np.frombuffer(message[4:], dtype=">f8")',
    ),
    Mutation(
        name="derived-key-ignores-the-seed",
        breaks="agreement between nodes and the single-process model -- every "
               "node would still agree with every OTHER node, so only the "
               "comparison against the reference implementation can catch it",
        path=DISTRIBUTED,
        old="        return np.random.default_rng((self.config.seed, int(token))).normal(",
        new="        return np.random.default_rng((0, int(token))).normal(",
    ),
    Mutation(
        name="slices-overlap",
        breaks="the partition -- dimensions would be owned twice and counted "
               "twice, inflating the pooled answer",
        path=DISTRIBUTED,
        old="    return [Slice(i * width, (i + 1) * width) for i in range(nodes)]",
        new="    return [Slice(i * width, (i + 1) * width + 1) for i in range(nodes)]",
    ),
    Mutation(
        name="departure-keeps-what-it-stored",
        breaks="the severity of mid-sequence loss -- a node would stop voting "
               "but leave its bindings behind, a far gentler failure than the "
               "real one and the whole reason this case is measured separately",
        path=LOCAL,
        old="                memory *= alive[:, None]",
        new="                memory *= 1.0",
    ),
    Mutation(
        name="departure-is-only-a-dropped-message",
        breaks="permanence -- the node would fall silent for one step and come "
               "back, which is C2's failure rather than C3's",
        path=LOCAL,
        old="                     else self.wv[token]) * alive",
        new="                     else self.wv[token])",
    ),
    Mutation(
        name="value-from-readout-is-read-and-never-applied",
        breaks="the mechanism entirely -- the frozen draw would be written "
               "whatever the flag said, so the measurement refuting a learned "
               "value projection would have been the frozen model twice",
        path=LOCAL,
        old="            value = (self.wo[token] if self.config.value_from_readout",
        new="            value = (self.wv[token] if self.config.value_from_readout",
    ),
    Mutation(
        name="departure-takes-derived-keys-too",
        breaks="the correction note 012 earned -- a node that derives its keys "
               "holds none, so blanking key columns on departure would report "
               "churn as up to 0.256 more damaging than it is",
        path=LOCAL,
        old="        if not self.config.derived_keys:\n            self.wk[:, index] = 0.0",
        new="        if True:\n            self.wk[:, index] = 0.0",
    ),
    Mutation(
        name="departure-leaves-the-readout-behind",
        breaks="permanence -- a departed node would keep voting through a "
               "readout nobody zeroed",
        path=LOCAL,
        old="        self.wo[:, index] = 0.0",
        new="        self.wo[:, index] *= 1.0",
    ),
    Mutation(
        name="derived-keys-share-one-stream",
        breaks="reconstructibility -- a row would depend on every draw before "
               "it, so a node could only rebuild the table by rebuilding all of "
               "it, which is the storage the scheme exists to avoid",
        path=LOCAL,
        old="                np.random.default_rng((config.seed, token)).normal(0.0, spread, d)",
        new="                rng.normal(0.0, spread, d)",
    ),
    Mutation(
        name="derived-keys-ignore-the-token",
        breaks="distinctness -- every token would get the same key, so no two "
               "things could ever be told apart",
        path=LOCAL,
        old="                np.random.default_rng((config.seed, token)).normal(0.0, spread, d)",
        new="                np.random.default_rng((config.seed, 0)).normal(0.0, spread, d)",
    ),
    Mutation(
        name="the-cache-admits-by-RECENCY-not-residual",
        breaks="the admission policy, which is the whole claim -- the cache "
               "would keep the last N bindings rather than the ones the store "
               "could not absorb, and HOLA measured that as 0.34 absolute worse",
        path=RETRIEVAL,
        old="        if residual > self.score[weakest]:",
        new="        if True:",
    ),
    Mutation(
        name="the-cache-read-is-not-gated-by-the-MATCH",
        breaks="the guard against contributing noise -- an unmatched query "
               "would still pull a full-magnitude vector out of the cache, "
               "which measurably made synthetic recall worse",
        path=RETRIEVAL,
        old="        if match <= 0.0:",
        new="        if False:",
    ),
    Mutation(
        name="a-single-read-secretly-settles-once",
        breaks="every result in the project -- `retrieval_steps` defaults to 1 "
               "and one settling step is measured to drop recall from 0.924 to "
               "0.600, so this would silently degrade every earlier number",
        path=RETRIEVAL,
        old="        for _ in range(self.steps - 1):",
        new="        for _ in range(self.steps):",
    ),
    Mutation(
        name="the-hidden-layer-never-learns",
        breaks="the mechanism that recovered 0.63 bits -- the hidden layer "
               "would stay at its random initialisation, which is a FIXED "
               "projection wearing a learned layer's name, and it would still "
               "change every prediction so a smoke test could not tell",
        path=LOCAL,
        old="                    self.hidden_w += self.config.lr * np.einsum(",
        new="                    self.hidden_w += 0.0 * np.einsum(",
    ),
    Mutation(
        name="the-hidden-gradient-crosses-groups",
        breaks="the C1 argument, not the loss -- group g would take its hidden "
               "gradient from every group's readout, so the model would still "
               "learn and the locality claim would silently be false",
        path=LOCAL,
        old='                    through = np.einsum("gv,vgh->gh", error, self.grouped_wo)',
        new='                    through = np.einsum("gv,vgh->gh", error.sum(0, keepdims=True) + 0*error, self.grouped_wo)',
    ),
    Mutation(
        name="a-chain-can-be-asked-twice",
        breaks="the hop axis, by leaking. A query block writes `a` next to `c`, "
               "so it STATES the link a -> c; asking a chain twice means the "
               "first block answers the second, and the second question is a "
               "ONE-HOP LOOKUP of a link already in the store. The leak grows "
               "with `n_queries`, which is the axis it would be swept along, "
               "and it produces a clean plausible curve -- it did, and the "
               "numbers were reported before the guard caught them",
        path=CHAINS,
        old="    asked_chains = rng.sample(list(chains), config.n_queries)",
        new="    asked_chains = [rng.choice(list(chains))\n"
            "                    for _ in range(config.n_queries)]",
    ),
    Mutation(
        name="only-the-last-question-is-scored",
        breaks="the point of `n_queries`. Scoring one question raises the "
               "DIFFICULTY of a sequence without raising the density of "
               "composition in the training signal, which is the whole reason "
               "the dial exists -- and the task would still generate, still "
               "validate, and still produce a curve along the axis",
        path=CHAINS,
        old="    for position, chain in queries:\n"
            "        targets[position] = chain[-1]",
        new="    targets[answer_position] = asked[-1]",
    ),
    Mutation(
        name="one-separator-still-consumes-a-random-draw",
        breaks="the reproducibility of every chain number ever measured. "
               "`rng.choice` consumes a draw even from a ONE-element sequence, "
               "so taking this branch unconditionally shifts the random stream "
               "and silently regenerates every single-separator sequence. The "
               "task stays perfectly valid -- same shape, same separator, no "
               "false links, every structural test still passes -- and every "
               "figure measured before `n_separators` existed quietly stops "
               "reproducing, with nothing to show it happened",
        path=CHAINS,
        old="        tokens.append(separators[0] if len(separators) == 1\n"
            "                      else rng.choice(separators))",
        new="        tokens.append(rng.choice(separators))",
    ),
    Mutation(
        name="a-chain-reuses-one-separator-throughout",
        breaks="the point of several terminators. Drawing once per SEQUENCE "
               "instead of once per chain means every chain in a sequence ends "
               "the same way, so a model sees one terminator at a time and the "
               "question of whether it learns a CLASS cannot be asked -- while "
               "the pool still looks used across the dataset as a whole",
        path=CHAINS,
        old="        tokens.append(separators[0] if len(separators) == 1\n"
            "                      else rng.choice(separators))\n"
            "        tokens.extend(chain)",
        new="        tokens.append(separators[0])\n"
            "        tokens.extend(chain)",
    ),
    Mutation(
        name="the-gate-scores-its-own-hop-not-the-next",
        breaks="the only signal the gate has. Decision 86 separates PAST THE "
               "END from ON THE CHAIN, which says whether hop k+1 has walked "
               "off -- not which of two on-chain hops is the answer. Scored by "
               "its own hop the gate takes depth-1 questions to 1.000 and "
               "leaves depth-2 at 0.547, so it still beats every fixed hop "
               "count and still looks like a working mechanism",
        path=LOCAL,
        old='                    "gd,kgd->kg", rule, ahead)',
        new='                    "gd,kgd->kg", rule, stack)',
    ),
    Mutation(
        name="which-hop-invents-a-label-when-none-is-right",
        breaks="the one restraint the objective has. When no hop names the "
               "target the answer was not reachable at any depth and there is "
               "nothing to teach; dropping the guard pushes the gate toward "
               "hop 0 every time that happens, which is a bias with no "
               "justification wearing the shape of a gradient -- and it points "
               "at exactly the hop the decay was already pulling toward",
        path=LOCAL,
        old="                        step = np.where(total > 0.0, hit / np.maximum(\n"
            "                            total, 1e-12) - gate, 0.0)",
        new="                        step = hit / np.maximum(total, 1e-12) - gate",
    ),
    Mutation(
        name="the-selector-never-reaches-the-rule",
        breaks="`gate_reads_key` entirely, leaving the one-rule gate wearing "
               "the name of the two-rule one. The extra parameters still exist "
               "and still receive gradient, the config still validates, and "
               "the answer-only number is 1.000 either way -- only the "
               "all-position number moves, 0.400 back down to 0.117",
        path=LOCAL,
        old="                    rule = self.halt_w + chosen[:, None] * self.halt_alt",
        new="                    rule = self.halt_w + 0.0 * self.halt_alt",
    ),
    Mutation(
        name="the-gate-never-learns",
        breaks="the gate, leaving it at its zero initialisation -- which is a "
               "UNIFORM softmax, so the model silently becomes a flat average "
               "over hops. Measured, that still scores 0.707 on mixed depths "
               "against 0.500 for either fixed hop count, because the readout "
               "learns to cope with the blend. A mechanism that does nothing "
               "and still beats the baseline is the hardest kind to notice",
        path=LOCAL,
        old="                    self.halt_w += rate * shared",
        new="                    self.halt_w += 0.0 * shared",
    ),
    Mutation(
        name="a-hop-decodes-from-the-accumulator",
        breaks="the traversal, only under `bind`, which is the quietest place "
               "for it. The accumulator and the newest retrieval are the same "
               "vector under `replace`, so every default result is unchanged "
               "and every structural test still passes -- but under `bind` the "
               "decode would ask what token R1-and-R2 together names, which is "
               "nothing, and the hops wander off after the first while still "
               "looking like they run",
        path=LOCAL,
        old="                    pooled = self.wv @ latest",
        new="                    pooled = self.wv @ retrieved",
    ),
    Mutation(
        name="a-hop-key-escapes-into-the-write-path",
        breaks="the invariant that hops change what is READ and never what is "
               "written, and this is the bug that actually happened. `key` is "
               "carried out to `previous_key`, which is what the next position "
               "writes its binding with, so reassigning it here makes every "
               "binding in the store use a re-encoded hop key instead of the "
               "token's. The hop mechanism then corrupts the memory it is "
               "trying to read, and it looks like a retrieval failure -- four "
               "probes and two refuted hypotheses went past before it was found",
        path=LOCAL,
        old="                hop_key = weights @ self.wk",
        new="                hop_key = key = weights @ self.wk",
    ),
    Mutation(
        name="the-hop-decode-is-never-sharpened",
        breaks="the re-encode, and this is the bug that actually happened. "
               "Without the standardisation the decode's logits are so flat "
               "the softmax is UNIFORM -- measured entropy 3.912 against "
               "log(50) = 3.912 -- so `weights @ wk` is the mean of every key "
               "row: the same constant vector no matter what was decoded. The "
               "decode itself stays correct (argmax finds the intermediate "
               "1.000 of the time), so nothing looks broken and every hop "
               "silently lands in the same wrong place",
        path=LOCAL,
        old="                    pooled = ((pooled - pooled.mean()) / spread\n"
            "                              * self.config.hop_sharpness)",
        new="                    pooled = (pooled - pooled.mean()) / spread",
    ),
    Mutation(
        name="the-hop-re-encodes-into-value-space",
        breaks="the whole point of decode-and-re-encode. A retrieval lives in "
               "VALUE space and the next hop needs a KEY: for token c a "
               "retrieval gives about wv[c] and the next lookup needs wk[c], "
               "which is a different random vector. Feeding values back would "
               "still produce a plausible vector of the right shape, and every "
               "hop would look like it was running",
        path=LOCAL,
        old="                hop_key = weights @ self.wk",
        new="                hop_key = weights @ self.wv",
    ),
    Mutation(
        name="a-workers-refusal-hangs-the-pool",
        breaks="every fail-fast guard in the project, in the configuration "
               "sweeps actually run in -- SystemExit is a BaseException, so a "
               "worker raising one dies silently and pool.map waits forever. "
               "Measured: 23 minutes against an expected 2, heading for the "
               "full 300-minute timeout",
        path=ROOT / "experiments" / "harness.py",
        old="        return pool.map(_Guarded(function), items)",
        new="        return pool.map(function, items)",
    ),
    Mutation(
        name="the-write-gate-is-ignored",
        breaks="the whole finding -- every corrective write would apply the "
               "full correction whatever the gate said, so a sweep over the "
               "gate would report one number six times and call it flat",
        path=LOCAL,
        old="                        memory += (self.config.write_gate\n                                   * np.outer(error, previous_key) / scale)",
        new="                        memory += (1.0\n                                   * np.outer(error, previous_key) / scale)",
    ),
    Mutation(
        name="the-table-source-takes-a-COPY-of-the-key-table",
        breaks="churn, partitions and ablation all at once -- they work by "
               "editing `Wk` in place, and a key source reading a stale copy "
               "would keep answering with dimensions their node took away",
        path=KEYS,
        old="        self.table = table",
        new="        self.table = np.array(table)",
    ),
    Mutation(
        name="the-key-seam-ignores-what-was-plugged-into-it",
        breaks="the whole point of the seam -- a replacement key source would "
               "be accepted and silently unused, so an experiment would report "
               "the stock scheme's numbers under a new scheme's name",
        path=LOCAL,
        old="            key = self.key_source.key(tokens, t)",
        new="            key = self.wk[token]",
    ),
    Mutation(
        name="context-keys-ignore-the-CONTEXT",
        breaks="the whole point -- the pair key would depend only on the current "
               "token, which is a bigram wearing a trigram's name and would "
               "report the ceiling as lifted when nothing changed",
        path=KEYS,
        old="                (self.seed, previous, token)).normal(",
        new="                (self.seed, token)).normal(",
    ),
    Mutation(
        name="context-keys-forget-the-ORDER",
        breaks="the distinction between `(a, b)` and `(b, a)` -- two different "
               "contexts would collide, so half the trigram table would be "
               "written on top of the other half",
        path=KEYS,
        old="                (self.seed, previous, token)).normal(",
        new="                (self.seed, min(previous, token), max(previous, token))).normal(",
    ),
    Mutation(
        name="the-context-key-queries-the-WRONG-pair",
        breaks="the alignment between what is written and what is read -- the "
               "query would be one step behind the store, so every retrieval "
               "would answer a question nobody asked",
        path=KEYS,
        old="        return self.pair(int(tokens[t - 1]) if t else self.start,",
        new="        return self.pair(int(tokens[t - 2]) if t > 1 else self.start,",
    ),
    Mutation(
        name="consolidation-ignores-confirmation",
        breaks="the gate -- it would promote every retrieval rather than the "
               "confirmed ones, making it a second memory wearing a gate's name",
        path=LOCAL,
        old="                    fires = predictions[t - 1] == token",
        new="                    fires = True",
    ),
    Mutation(
        name="consolidation-reads-ahead",
        breaks="causality -- confirming against the CURRENT token rather than "
               "the one that has just arrived lets the answer at t depend on "
               "information from t itself, which no running system would have",
        path=LOCAL,
        old="                    fires = predictions[t - 1] == token",
        new="                    fires = predictions[t - 1] == tokens[t - 1]",
    ),
    Mutation(
        name="consolidated-store-is-never-read",
        breaks="the point of consolidating at all, since the lasting store "
               "would be written and then ignored at retrieval",
        path=LOCAL,
        old="            readable = memory if lasting is None else memory + lasting",
        new="            readable = memory",
    ),
    Mutation(
        name="trace-reports-the-neighbouring-token",
        breaks="which position every traced signal belongs to -- the counts, the "
               "ordering and the number of entries all stay right, so a "
               "separability number computed from it would be about the wrong "
               "positions and nothing downstream could tell",
        path=LOCAL,
        old='                        "token": int(token),',
        new='                        "token": int(tokens[t - 1]),',
    ),
    Mutation(
        name="trace-reports-the-mean-as-the-surprise",
        breaks="the signal the probe ranks on, replacing a per-step quantity "
               "with a running average of it -- still monotone, still falling on "
               "repetition, and useless for separating one step from another",
        path=LOCAL,
        old='                        "surprise": float(step_surprise),',
        new='                        "surprise": float(mean_surprise),',
    ),
    Mutation(
        name="the-floor-refusal-does-nothing",
        breaks="the rail that withdrew g8-01's seq-1536 row -- a cell whose "
               "floor arm has collapsed below chance gets a ratio again, and it "
               "is a large and impressive-looking one",
        path=RECOVERY,
        old='    if means["none"] <= floor:',
        new='    if False:',
    ),
    Mutation(
        name="slot-cost-forgets-the-node-is-sliced",
        breaks="the finding that vector slots never get cheaper on a small "
               "node. Dropping `w` makes them a fixed cost like token ids, so "
               "the two options look alike and the ONE reason to prefer "
               "derived keys disappears -- which is note 015's error exactly, "
               "in a cost model for a different mechanism",
        path=SLOT_COST,
        old="    return vocab * slots * w",
        new="    return vocab * slots",
    ),
    Mutation(
        name="slot-cost-ignores-the-vocabulary",
        breaks="the axis that decides everything past character level. A "
               "word-level vocabulary is a thousand times larger and the table "
               "reverses; a cost model blind to it would recommend the same "
               "mechanism at every scale",
        path=SLOT_COST,
        old="    return vocab * slots\n\n\ndef report(",
        new="    return slots\n\n\ndef report(",
    ),
    Mutation(
        name="the-absurdity-guard-lets-everything-through",
        breaks="the check that a cross-entropy far worse than uniform is a "
               "BROKEN number rather than a bad model. g10-01 reported 39.5 "
               "bits over an 86-symbol vocabulary and NaN, both read off a "
               "table as measurements; they were a readout at 1e72 with "
               "accuracy below chance",
        path=NGRAM,
        old="    if value > ceiling + slack:",
        new="    if False:",
    ),
    Mutation(
        name="the-absurdity-threshold-ignores-the-vocabulary",
        breaks="the scaling of that check. A constant cut-off refuses real "
               "results on a large vocabulary -- 13 bits is absurd for 86 "
               "symbols and unremarkable for 100,000 -- so the guard would "
               "start rejecting the very corpus this project would move to",
        path=NGRAM,
        old="    ceiling = uniform_bits(vocab_size)\n"
            "    if value != value or value in (float(\"inf\"), float(\"-inf\")):",
        new="    ceiling = 7.0\n"
            "    if value != value or value in (float(\"inf\"), float(\"-inf\")):",
    ),
    Mutation(
        name="the-baseline-is-measured-in-nats",
        breaks="the UNIT of every corpus number. Natural log instead of log2 "
               "reports 0.693 of the bits, which is a plausible number, is "
               "smaller in the flattering direction, and makes the memory look "
               "like it beat a bigram it did not beat",
        path=NGRAM,
        old="                probability = self.probability(\n"
            "                    self._context(tokens, position), token)\n"
            "                total -= math.log2(probability)",
        new="                probability = self.probability(\n"
            "                    self._context(tokens, position), token)\n"
            "                total -= math.log(probability)",
    ),
    Mutation(
        name="smoothing-inflates-the-numerator-only",
        breaks="normalisation. Adding k to every count without adding "
               "k * vocab_size to the total leaves a distribution summing to "
               "more than one, so every probability is too large and every "
               "cross-entropy too small -- the baseline gets stronger, which "
               "makes the model being compared to it look worse, so this one "
               "errs against the interesting result rather than for it",
        path=NGRAM,
        old="        return (seen + self.k) / (total + self.k * self.vocab_size)",
        new="        return (seen + self.k) / (total + self.k)",
    ),
    Mutation(
        name="the-normalisation-check-does-nothing",
        breaks="the guard on scoring a model by its own distributions. An "
               "unnormalised one yields a SMALLER cross-entropy, so it reads "
               "as a better model rather than as a broken one",
        path=NGRAM,
        old="        if not 0.999 <= mass <= 1.001:",
        new="        if False:",
    ),
    Mutation(
        name="every-candidate-gets-the-same-number",
        breaks="the only candidate-specific signal available at a capture step "
               "-- the store's own norm is a property of the STEP, so every "
               "pending write gets one value and ranking on it ranks on "
               "nothing. Lengths, counts and magnitudes all stay plausible",
        path=LOCAL,
        old="                        float(np.linalg.norm(memory @ key_written))",
        new="                        float(np.linalg.norm(memory))",
    ),
    Mutation(
        name="the-candidates-are-listed-backwards",
        breaks="the alignment between `pending_now`'s indices and `captured`'s "
               "-- anything reading them together then scores the wrong "
               "candidate, and every length, count and magnitude is unchanged",
        path=LOCAL,
        old="                        for _, _, key_written in pending)",
        new="                        for _, _, key_written in reversed(pending))",
    ),
    Mutation(
        name="foreign-records-are-dropped-in-silence",
        breaks="the announcement, not the filtering. A summariser that quietly "
               "discards half its input still prints a confident table, and "
               "nobody learns that a stray file is being uploaded with every "
               "artifact of every sweep -- which is how this was found",
        path=RECOVERY,
        old="    if len(kept) != len(rows):",
        new="    if False:",
    ),
    Mutation(
        name="the-error-does-not-shrink-with-seeds",
        breaks="the entire reason for running more seeds. Reporting the "
               "standard DEVIATION instead of the standard error of the mean "
               "gives a number that does not fall as evidence accumulates, so "
               "a twelve-seed re-run would conclude its comparisons were no "
               "sharper than at three -- the exact backwards reading BACKLOG "
               "item 0b exists to avoid",
        path=RECOVERY,
        old="    return mean, (variance / len(values)) ** 0.5",
        new="    return mean, variance ** 0.5",
    ),
    Mutation(
        name="the-ratio-is-not-paired-to-its-own-seed",
        breaks="the pairing, which is the whole point of a per-seed ratio. "
               "Dividing by a shared constant instead of THIS seed's own gap "
               "puts each seed's difficulty back into the numerator, which is "
               "the variance the pairing exists to remove -- and it still "
               "returns plausible ratios in the right rough range",
        path=RECOVERY,
        old="        usable.append((value - none) / gap)",
        new="        usable.append((value - none) / (oracle - floor))",
    ),
    Mutation(
        name="the-noise-floor-is-in-the-wrong-units",
        breaks="the comparison between a lead and the error on it -- the spread "
               "is measured in accuracy and the lead in recovery, so dropping "
               "the division compares two different quantities. It still "
               "returns a plausible small number, and at a gap near 1.0 it is "
               "very nearly the right one",
        path=RECOVERY,
        old='    return cell.spread / cell.gap if cell.gap else float("inf")',
        new='    return cell.spread',
    ),
    Mutation(
        name="a-tie-still-names-a-winner",
        breaks="the guard against reading a difference smaller than the "
               "measurement error -- `max` over a swept axis always names "
               "something, so without a noise floor three identical numbers "
               "become a trend. This is how g9-12's summariser first reported "
               "'the best rate MOVES with node width' from fabricated records "
               "where every rate was identical by construction",
        path=RECOVERY,
        old='    return key, cells[key].ratios[arm] - cells[incumbent].ratios[arm], \\\n        margin(cells[incumbent])',
        new='    return key, cells[key].ratios[arm] - cells[incumbent].ratios[arm], 0.0',
    ),
    Mutation(
        name="selection-prefers-the-largest-gap",
        breaks="the rule that a cell is chosen AFTER the refusals -- picking on "
               "the gap prefers exactly the cells whose floor arm collapsed, "
               "because collapse IS a large gap",
        path=RECOVERY,
        old='    return max(usable, key=lambda pair: pair[1].ratios[arm])',
        new='    return max(usable, key=lambda pair: pair[1].gap)',
    ),
    Mutation(
        name="storage-mask-off-by-one",
        breaks="which binding the mask gates -- a binding written at t is "
               "(t-1 -> t), so gating on t-1 keeps the wrong ones while every "
               "count and shape stays identical",
        path=LOCAL,
        # Qualified with the write guard because `decay_when_masked` added a
        # second reading of the same mask, and an ambiguous target is a stale
        # target: without this the harness cannot tell which branch it broke.
        old="previous_key is not None and (store is None or store[t])",
        new="previous_key is not None and (store is None or store[t - 1])",
    ),
    Mutation(
        name="storage-mask-ignored",
        breaks="selective storage entirely, so the oracle gate silently becomes "
               "the ungated model and g7-02's headline evaporates",
        path=LOCAL,
        old="            wrote = previous_key is not None and (store is None or store[t])",
        new="            wrote = previous_key is not None",
    ),
    Mutation(
        name="repeats-are-ignored",
        breaks="recurrence, so every key is queried once however many repeats "
               "are asked for and consolidation has nothing to pay off against",
        path=MQAR,
        old="    query_order = keys * config.queries_per_pair",
        new="    query_order = keys[:]",
    ),
    Mutation(
        name="repeats-do-not-reserve-room",
        breaks="the filler count, so the extra queries eat into filler without "
               "the layout knowing and the sequence silently changes length",
        path=MQAR,
        old="    n_queries = config.n_pairs * config.queries_per_pair",
        new="    n_queries = config.n_pairs",
    ),
    Mutation(
        name="task-split-folds-keys-together",
        breaks="well-posedness -- the ORIGINAL bug, which made keys k and k+16 "
               "the same token so 3% of queries had two correct answers",
        path=SPLIT,
        old="    out[is_key] = tokens[is_key] + half * GEN.n_keys",
        new="    out[is_key] = tokens[is_key] % (GEN.n_keys // 2) + half * GEN.n_keys",
    ),
    Mutation(
        name="task-split-shares-its-values",
        breaks="disjointness of the value alphabets, so the two tasks answer "
               "with the same tokens and there is nothing left to forget",
        path=SPLIT,
        old="out[is_value] = (BASE.n_keys + half * GEN.n_values",
        new="out[is_value] = (BASE.n_keys + 0 * GEN.n_values",
    ),
    Mutation(
        name="sparse-keys-are-not-actually-sparse",
        breaks="sparsity, so a sweep over key_active would measure nothing and "
               "report the dense result at every setting",
        path=LOCAL,
        old="                active = draw.choice(d, config.key_active, replace=False)",
        new="                active = draw.choice(d, d, replace=False)",
    ),
    Mutation(
        name="sparse-keys-drift-in-length",
        breaks="the norm control -- key scale would move with sparsity, and "
               "g3-02 showed scale alone swings accuracy from 0.263 to 0.960",
        path=LOCAL,
        old="active] = config.key_scale / np.sqrt(",
        new="active] = config.key_scale * (",
    ),
    Mutation(
        name="sparse-keys-may-repeat-a-dimension",
        breaks="distinctness of active sets, so a token can end up with fewer "
               "active dimensions than requested",
        path=LOCAL,
        old="                active = draw.choice(d, config.key_active, replace=False)",
        new="                active = draw.choice(d, config.key_active, replace=True)",
    ),
    Mutation(
        name="round-a-fractional-machine",
        breaks="block churn's granularity -- the ORIGINAL bug, which turned a "
               "request to remove half the width at P=1 into removing all of it "
               "and reporting 0.000 as a finding about churn",
        path=CHURN,
        old="    return removed > 0 and abs(removed / per_group - round(removed / per_group)) < 1e-9",
        new="    return removed > 0",
    ),
    Mutation(
        name="block-churn-removes-the-wrong-count",
        breaks="the equal-size control, so the two arms differ in how much they "
               "remove and any gap between them is size rather than shape",
        path=CHURN,
        old="    chosen = rng.choice(groups, size=n // per_group, replace=False)",
        new="    chosen = rng.choice(groups, size=max(1, n // per_group - 1), replace=False)",
    ),
    Mutation(
        name="block-churn-may-repeat-a-group",
        breaks="distinctness, so block removes fewer distinct dimensions than "
               "scattered while appearing to remove the same number",
        path=CHURN,
        old="    chosen = rng.choice(groups, size=n // per_group, replace=False)",
        new="    chosen = rng.choice(groups, size=n // per_group, replace=True)",
    ),
    Mutation(
        name="scattered-churn-is-secretly-block-churn",
        breaks="the contrast the whole experiment measures -- both arms would "
               "remove whole machines and the comparison would be an expensive "
               "way of running one condition twice",
        path=CHURN,
        old='    if shape == "scattered":\n        return rng.choice(width, size=n, replace=False)',
        new='    if shape == "scattered" and False:\n        return rng.choice(width, size=n, replace=False)',
    ),
    Mutation(
        name="pool-the-error-across-groups",
        breaks="partition independence -- every group's update would read every "
               "other group's prediction, restoring the global reduction C1 "
               "forbids while leaving the pooled output looking correct",
        path=LOCAL,
        old="                error = target - parts",
        new="                error = target - parts.sum(0)",
    ),
    Mutation(
        name="ignore-the-requested-cluster",
        breaks="reading an answer off one machine or one cluster, so a "
               "measurement of how small a node can be would silently measure "
               "the whole network pooled instead",
        path=LOCAL,
        old="            answer = parts.sum(0) if members is None else parts[members].sum(0)",
        new="            answer = parts.sum(0)",
    ),
    Mutation(
        name="cluster-may-double-count-a-machine",
        breaks="the distinctness guard, so a cluster can list the same machine "
               "twice and count its vote twice, inflating small clusters",
        path=LOCAL,
        old="            if len(set(members)) != len(members):",
        new="            if False:",
    ),
    Mutation(
        name="split-the-retrieved-vector-the-wrong-way",
        breaks="which dimensions belong to which group -- the groups would "
               "interleave rather than partition, so no group owns a contiguous "
               "slice and the row-split argument does not apply",
        path=LOCAL,
        old="            sliced = retrieved.reshape(groups, -1)",
        new="            sliced = retrieved.reshape(-1, groups).T",
    ),
    Mutation(
        name="copy-the-readout-instead-of-viewing-it",
        breaks="the aliasing between wo and grouped_wo, so learning updates a "
               "detached copy and ablate stops reaching the readout",
        path=LOCAL,
        old="        self.grouped_wo = self.wo.reshape(v, config.partitions, -1)",
        new="        self.grouped_wo = self.wo.reshape(v, config.partitions, -1).copy()",
    ),
    Mutation(
        name="filler-collides-with-keys",
        breaks="filler may reuse a key this sequence queries, making the task ill-posed",
        path=MQAR,
        old="spare_keys = tuple(k for k in range(config.n_keys) if k not in pairs)",
        new="spare_keys = tuple(range(config.n_keys))",
    ),
    Mutation(
        name="query-only-the-first-pair",
        breaks="the multi-query property that makes the benchmark discriminating",
        path=MQAR,
        old="    slots = [True] * n_queries + [False] * n_filler",
        new="    slots = [True] + [False] * (n_filler + n_queries - 1)",
    ),
    Mutation(
        name="ignore-the-seed",
        breaks="seeding, so every sequence in a dataset becomes identical",
        path=MQAR,
        old="rng = random.Random(config.seed)",
        new="rng = random.Random(0)",
    ),
    Mutation(
        name="wrong-recall-target",
        breaks="the task definition itself: query targets stop being the paired value",
        path=MQAR,
        old="            targets.append(pairs[key])",
        new="            targets.append(_value_token(config, 0))",
    ),
    Mutation(
        name="filler-mode-disconnected",
        breaks="the filler dial, so 'random' and 'structured' become the same condition",
        path=MQAR,
        old='    if config.filler == "random":',
        new="    if False:",
    ),
    Mutation(
        name="value-alphabet-collapsed",
        breaks="n_values, so the base rate is wrong in every configuration",
        path=MQAR,
        old="pairs = {k: _value_token(config, rng.randrange(config.n_values)) for k in keys}",
        new="pairs = {k: _value_token(config, rng.randrange(1)) for k in keys}",
    ),
    Mutation(
        name="query-order-not-shuffled",
        breaks="the shuffle, so query order leaks pair order and `positional` solves the task",
        path=MQAR,
        old="    rng.shuffle(query_order)",
        new="    pass",
    ),
    Mutation(
        name="trivial-floor-is-the-base-rate",
        breaks="the floor, reverting it to the flattering 1/n_values a model must NOT be judged against",
        path=MQAR,
        old="        return 1 / self.n_pairs + (1 - 1 / self.n_pairs) / self.n_values",
        new="        return 1 / self.n_values",
    ),
    Mutation(
        name="oracle-off-by-one",
        breaks="the oracle, which is the only check that the task is answerable at all",
        path=BASELINES,
        old="    return sequence.pairs[sequence.tokens[position]]",
        new="    return sequence.pairs[sequence.tokens[position]] + 1",
    ),
    Mutation(
        name="constant-baseline-not-fitted",
        breaks="fitting, so the base rate stops tracking the data it is meant to describe",
        path=BASELINES,
        old="    most_common = counts.most_common(1)[0][0]",
        new="    most_common = min(counts)",
    ),
    Mutation(
        name="accuracy-scores-every-position",
        breaks="scoring, diluting every measurement with positions where no answer is required",
        path=BASELINES,
        old="        for position in sequence.query_positions:",
        new="        for position in range(len(sequence.tokens)):",
    ),
    Mutation(
        name="autoregressive-flag-inert",
        breaks="the autoregressive layout, so docs/notes/001 P2 is silently unsatisfied again",
        path=MQAR,
        old="    query_width = 2 if config.autoregressive else 1",
        new="    query_width = 1",
    ),
    Mutation(
        name="answer-positions-classified-as-filler",
        breaks="the answer's classification, excluding it from the task column of the probe",
        path=MQAR,
        old='            kinds[i] = "answer"',
        new='            kinds[i] = "filler"',
    ),
    Mutation(
        name="mask-ignores-the-furthest-offset",
        breaks="the causal mask, letting a +2 offset read the token the model must predict",
        path=ATTENTION,
        old="        mask = np.tril(np.ones((T, T), dtype=bool), k=-self.reach)",
        new="        mask = np.tril(np.ones((T, T), dtype=bool), k=-1)",
    ),
    Mutation(
        name="offset-mixture-disconnected",
        breaks="the learned mixture, so the model cannot discover which offset to read",
        path=ATTENTION,
        old="        shifted = np.tensordot(p[\"offset_mix\"], sources, axes=(0, 0))",
        new="        shifted = sources[0]",
    ),
    Mutation(
        name="value-shift-removed",
        breaks="the induction shape: attending to s retrieves s rather than what followed it",
        path=ATTENTION,
        old="            out[j, :-offset] = h[offset:]",
        new="            out[j, :-offset] = h[:-offset]",
    ),
    Mutation(
        name="backward-shift-off-by-one",
        breaks="the gradient through the value shift, so training optimises a different objective",
        path=ATTENTION,
        old="                d_h[offset:] += weight * d_shifted[:-offset]",
        new="                d_h[:-offset] += weight * d_shifted[:-offset]",
    ),
    Mutation(
        name="optimiser-does-not-step",
        breaks="the Adam update, so training runs and nothing changes",
        path=ATTENTION,
        old="            self.params[name] -= self.lr * m_hat / (np.sqrt(v_hat) + self.eps)",
        new="            self.params[name] -= 0.0 * m_hat / (np.sqrt(v_hat) + self.eps)",
    ),
    Mutation(
        name="key-scale-ignored",
        breaks="the scale parameter, re-pinning the degree of freedom g3-02 found mattered most",
        path=LOCAL,
        old="        spread = config.key_scale / np.sqrt(d)",
        new="        spread = 1.0 / np.sqrt(d)",
    ),
    Mutation(
        name="ablation-spares-the-key-projection",
        breaks="churn permanence, letting a departed machine keep contributing",
        path=LOCAL,
        old="        self.wk[:, index] = 0.0",
        new="        pass",
    ),
    Mutation(
        name="surviving-width-reports-the-original",
        breaks="the honest denominator after churn -- a departed network would "
               "be scored against room it no longer has",
        path=LOCAL,
        old="        return int((np.abs(self.wv).sum(axis=0) > 0).sum())",
        new="        return self.config.d_model",
    ),
    Mutation(
        name="surviving-width-counts-through-keys",
        breaks="liveness detection under derived keys -- keys are never zeroed "
               "there, so a half-departed network would report itself intact",
        path=LOCAL,
        old="        return int((np.abs(self.wv).sum(axis=0) > 0).sum())",
        new="        return int((np.abs(self.wk).sum(axis=0) > 0).sum())",
    ),
    Mutation(
        name="buffer-releases-a-slot-too-early",
        breaks="the buffer depth, so events still in flight are treated as lost",
        path=TRANSPORT,
        old="        release = step - config.max_delay",
        new="        release = step - config.max_delay + 1",
    ),
    Mutation(
        name="late-events-appended-out-of-order",
        breaks="emission ordering, corrupting the sequence rather than showing a gap",
        path=TRANSPORT,
        old="    landed.sort(key=lambda pair: pair[0])",
        new="    landed.sort(key=lambda pair: pair[1])",
    ),
    Mutation(
        name="arrivals-recorded-after-release",
        breaks="the tie at exactly max_delay, silently moving the stated bound by one",
        path=TRANSPORT,
        old="        return self.jitter <= self.max_delay",
        new="        return self.jitter < self.max_delay",
    ),
    Mutation(
        name="local-store-binds-the-current-token-to-itself",
        breaks="the induction binding, storing (t -> t) rather than (t-1 -> t)",
        path=LOCAL,
        old="                memory += np.outer(value, previous_key)",
        new="                memory += np.outer(value, key)",
    ),
    Mutation(
        name="local-memory-never-stores",
        breaks="the store, so the memory stays empty and retrieval returns zero",
        path=LOCAL,
        old="                memory += np.outer(value, previous_key)",
        new="                memory += 0.0 * np.outer(value, previous_key)",
    ),
    Mutation(
        name="local-delta-rule-inert",
        breaks="the delta rule, so the readout never learns and sits at its initialisation",
        path=LOCAL,
        old="                    self.grouped_wo += self.config.lr * update",
        new="                    self.grouped_wo += 0.0 * update",
    ),
    Mutation(
        name="local-memory-persists-across-sequences",
        breaks="per-sequence reset, letting the model accumulate the training set",
        path=LOCAL,
        old="        memory = np.zeros((d, d))",
        new="        memory = getattr(self, '_leak', np.zeros((d, d))); self._leak = memory",
    ),
    Mutation(
        name="carry-store-is-read-and-never-applied",
        breaks="the mechanism -- the store would reset whatever the flag said, "
               "so the measurement refuting it would have been the reset model "
               "measured twice and reported as a comparison",
        path=LOCAL,
        old="        if self.config.carry_store and self._carried is not None:",
        new="        if False and self._carried is not None:",
    ),
    Mutation(
        name="lookup-uses-first-occurrence",
        breaks="most-recent lookup, silently answering from stale evidence",
        path=INDUCTION,
        old="        last_seen[token] = position",
        new="        last_seen.setdefault(token, position)",
    ),
    Mutation(
        name="lookup-off-by-one",
        breaks="the lookup, returning what the token WAS rather than what followed it",
        path=INDUCTION,
        old="            one_hot[tokens[previous + 1]] = 1.0",
        new="            one_hot[tokens[previous]] = 1.0",
    ),
    Mutation(
        name="lookup-ignores-the-current-token",
        breaks="input-dependence, turning the lookup into a fixed filter",
        path=INDUCTION,
        old="        previous = last_seen.get(token)",
        new="        previous = position - 1 if position else None",
    ),
]


def restore_any_leftovers() -> None:
    """Recover from a previous run that was killed mid-mutation."""
    for bak in ROOT.rglob("*.py.bak"):
        target = bak.with_suffix("")
        print(f"!! recovering {target.relative_to(ROOT)} from an interrupted run")
        target.write_text(bak.read_text(encoding="utf-8"), encoding="utf-8")
        bak.unlink()


def suite_passes(mutation: "Mutation | None" = None) -> bool:
    """Run the suite, skipping process-spawning tests where they cannot help.

    Those tests cost about half a second each because they fork OS processes.
    Across sixty mutations that doubled the harness runtime, and **a check slow
    enough to skip is a check that eventually is**. They are therefore run only
    when the mutation is in `distributed.py` itself — the only file whose faults
    they are positioned to catch — and skipped otherwise.

    Passing `None` runs everything, which is what the baseline check before any
    mutation does: if the full suite is red, nothing below means anything.
    """
    environment = dict(os.environ)
    if mutation is not None and mutation.path != DISTRIBUTED:
        environment["OPENPLEXUS_SKIP_PROCESS_TESTS"] = "1"
    result = subprocess.run(
        [sys.executable, "-m", "unittest", "discover", "-s", "tests", "-t", ".", "-q"],
        cwd=ROOT, capture_output=True, text=True, env=environment,
    )
    return result.returncode == 0


def apply(mutation: Mutation) -> str | None:
    """Apply a mutation. Returns an error string if the source has moved."""
    source = mutation.path.read_text(encoding="utf-8")
    occurrences = source.count(mutation.old)
    if occurrences != 1:
        return f"expected 1 occurrence of the target text, found {occurrences}"
    bak = mutation.path.with_suffix(".py.bak")
    bak.write_text(source, encoding="utf-8")
    mutation.path.write_text(source.replace(mutation.old, mutation.new), encoding="utf-8")
    return None


def revert(mutation: Mutation) -> None:
    bak = mutation.path.with_suffix(".py.bak")
    if bak.exists():
        mutation.path.write_text(bak.read_text(encoding="utf-8"), encoding="utf-8")
        bak.unlink()


def changed_files() -> set[Path]:
    """Every file this work touches: uncommitted, plus committed since master.

    Union of both, because "what am I about to push" is the question `--changed`
    answers, and either half alone misses the case that motivated it -- a hole
    opened in one commit and noticed two commits later.
    """
    touched: set[Path] = set()
    status = subprocess.run(["git", "status", "--porcelain"], cwd=ROOT,
                            capture_output=True, text=True)
    for line in status.stdout.splitlines():
        name = line[3:].strip().split(" -> ")[-1]
        if name:
            touched.add((ROOT / name).resolve())
    for base in ("origin/master", "master"):
        diff = subprocess.run(["git", "diff", "--name-only", f"{base}...HEAD"],
                              cwd=ROOT, capture_output=True, text=True)
        if diff.returncode == 0:
            touched.update((ROOT / n).resolve()
                           for n in diff.stdout.split() if n)
            break
    return touched


def selected(argv: list[str]) -> list:
    """The mutations to run, from `--only`, `--changed` or `--shard`.

    `--only` is for the local case: after adding a mechanism, run the mutations
    that touch it rather than all eighty-five. `--shard i/n` is for CI, which can
    then run n jobs at once.

    **`--changed` exists because a surviving mutation was invisible locally.**
    The five pre-commit checks run `--verify`, which only asserts that every
    mutation's ORIGINAL text is still present; the full harness is CI-only and
    sharded, because it edits the source for twenty minutes. So a vacuous test
    region survives every local check and is reported later, on a run nobody is
    watching, attached to whichever commit happened to be pushed.

    > *Calibration.* Two mutations on the exact cache --
    > `the-cache-admits-by-RECENCY-not-residual` and
    > `the-cache-read-is-not-gated-by-the-MATCH` -- survived at `b480926` and at
    > least one commit before it. The cache is the project's **first controlled
    > improvement on the corpus**, and its two defining claims, admission by
    > residual and the match gate, had nothing asserting them. Both were found
    > by CI while a refactor was already in flight, and the refactor's own
    > `--verify` failure is what caused anyone to look.

    `--changed` runs only the mutations whose target file this work touches,
    which is seconds rather than twenty minutes and is exactly the set that can
    have been invalidated.
    Sharding is by POSITION, not by hash of the name, so the same mutation lands
    in the same shard across runs and two CI logs can be compared.
    """
    chosen = list(MUTATIONS)
    for index, argument in enumerate(argv):
        if argument == "--changed":
            touched = changed_files()
            chosen = [m for m in MUTATIONS if m.path.resolve() in touched]
        elif argument == "--only" and index + 1 < len(argv):
            wanted = {n.strip() for n in argv[index + 1].split(",")}
            unknown = wanted - {m.name for m in MUTATIONS}
            if unknown:
                raise SystemExit(f"no such mutation: {', '.join(sorted(unknown))}")
            chosen = [m for m in MUTATIONS if m.name in wanted]
        elif argument == "--shard" and index + 1 < len(argv):
            part, _, total = argv[index + 1].partition("/")
            part, total = int(part), int(total)
            if not 0 <= part < total:
                raise SystemExit(f"--shard {part}/{total} is out of range")
            chosen = [m for i, m in enumerate(MUTATIONS) if i % total == part]
    return chosen


LOCK = ROOT / ".mutate.lock"


def claim_the_lock() -> None:
    """Refuse to start if another harness is already running.

    A `.bak` file means the source is edited. It CANNOT say whether a run died
    and left it, or a run is using it right now -- and the two want opposite
    responses: restore, or keep away. Startup restore is right for the first and
    destroys the second.

    That is not hypothetical. A second run started while a background one was
    going, restored its in-flight mutation, and both sets of results became
    meaningless while the second printed a confident "4/4 caught".

    The lock holds a PID. A lock whose process is gone is stale and is cleared;
    a lock whose process is alive stops us.
    """
    if LOCK.exists():
        try:
            owner = int(LOCK.read_text(encoding="utf-8").strip())
        except (OSError, ValueError):
            owner = None
        if owner is not None and _is_running(owner):
            raise SystemExit(
                f"REFUSING TO RUN: another mutation harness is running "
                f"(pid {owner}).\n"
                f"Two harnesses editing the same files produce results that "
                f"look fine and mean nothing.\n"
                f"Wait for it, or stop it and delete {LOCK.name}.")
        print(f"clearing a stale lock from pid {owner}")
    LOCK.write_text(str(os.getpid()), encoding="utf-8")


def release_the_lock() -> None:
    try:
        LOCK.unlink()
    except OSError:
        pass


def _is_running(pid: int) -> bool:
    """Is this pid alive? Conservative: unknown counts as alive.

    A false "alive" costs a wait. A false "dead" costs two harnesses running at
    once, which is the failure this exists to prevent.
    """
    if os.name == "nt":
        out = subprocess.run(["tasklist", "/FI", f"PID eq {pid}"],
                             capture_output=True, text=True)
        return str(pid) in out.stdout
    try:
        os.kill(pid, 0)
    except ProcessLookupError:
        return False
    except PermissionError:
        return True
    return True


def verify(quiet: bool = False) -> int:
    """Assert every mutation's ORIGINAL text is present. One second, no edits.

    A `.bak` file says a file is edited RIGHT NOW. This says something a `.bak`
    cannot: that the source on disk is the source everyone means, whatever
    happened earlier. The two failures it separates want the same response and
    arrive by different routes -- a run killed mid-edit and hand-restored to the
    wrong version, and a `git add -A` issued while a background harness had a
    file open.

    The second is not hypothetical. Commit 3634a23 shipped `rank = strength` --
    `the-tag-admits-the-strongest`, live in the source -- inside the change whose
    entire argument is that admitting the strongest is backwards. Nothing ran the
    suite against the tree being committed, so nothing objected. CI caught it on
    push, which is the right backstop and the wrong place to find out.

    Cheap enough to run before every commit, and it names WHICH mutation is
    present, which a red test does not.
    """
    missing = []
    for mutation in MUTATIONS:
        try:
            source = mutation.path.read_text(encoding="utf-8")
        except OSError as problem:
            missing.append((mutation, str(problem)))
            continue
        found = source.count(mutation.old)
        if found != 1:
            missing.append((mutation, f"original text appears {found} times"))
    if not missing:
        if not quiet:
            print(f"source clean: all {len(MUTATIONS)} originals present")
        return 0
    print("SOURCE IS NOT THE SOURCE ANYONE MEANS.")
    print()
    for mutation, why in missing:
        print(f"  {mutation.name}: {why}")
        print(f"    in {mutation.path.relative_to(ROOT)}")
    print()
    print("Either a mutation is live on disk -- do not commit, do not measure,")
    print("restore it -- or the harness is stale because a refactor moved the")
    print("line it targets, and the mutation needs re-pointing.")
    return 1


def main(argv: list[str] | None = None) -> int:
    argv = sys.argv[1:] if argv is None else argv
    if "--verify" in argv:
        # Deliberately outside the lock: it edits nothing, and the case it most
        # needs to cover is "is a harness halfway through right now".
        return verify()
    claim_the_lock()
    try:
        return _main(argv)
    finally:
        release_the_lock()


def _main(argv: list[str]) -> int:
    restore_any_leftovers()

    mutations = selected(argv)
    if len(mutations) != len(MUTATIONS):
        print(f"running {len(mutations)} of {len(MUTATIONS)} mutations")

    if not suite_passes():
        print("The suite is red before any mutation. Fix that first.")
        return 2

    survived, stale = [], []
    for mutation in mutations:
        problem = apply(mutation)
        if problem:
            stale.append(mutation)
            print(f"SOURCE MOVED  {mutation.name}: {problem}")
            continue
        try:
            caught = not suite_passes(mutation)
        finally:
            revert(mutation)
        if caught:
            print(f"caught        {mutation.name}")
        else:
            survived.append(mutation)
            print(f"SURVIVED      {mutation.name} — breaks {mutation.breaks}")

    print(f"\n{len(mutations) - len(survived) - len(stale)}/{len(mutations)} caught")
    if stale:
        # The names are repeated here even though each was printed above, so a
        # truncated capture cannot hide WHICH mutation went stale. `tail -4` of
        # this run once cost a diagnosis: the summary said one could not be
        # applied and the line naming it had scrolled away.
        print(f"{len(stale)} mutation(s) could not be applied — the harness is stale "
              "and is not checking what it claims to: "
              + ", ".join(m.name for m in stale))
    if survived:
        print("A surviving mutation means the tests covering that mechanism are "
              "vacuous. Strengthen them (rule 10), do not delete the mutation.")
    return 1 if (survived or stale) else 0


if __name__ == "__main__":
    raise SystemExit(main())
