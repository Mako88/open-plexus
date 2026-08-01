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
CONCEPTS = ROOT / "openplexus" / "concepts.py"
KEYS = ROOT / "openplexus" / "keys.py"
PEER = ROOT / "openplexus" / "peer.py"
RETRIEVAL = ROOT / "openplexus" / "retrieval.py"
SPLIT = ROOT / "experiments" / "g6_01_forgetting.py"
# Experiment code is not usually mutated -- experiments are read once and
# discarded. This one is, because its generator returned a wrong SET rather
# than crashing, which is how a sweep becomes a confident wrong answer.
CHURN = ROOT / "experiments" / "g4_02_machine_churn.py"
TRANSPORT = ROOT / "openplexus" / "transport.py"
DEPLOYMENT = ROOT / "openplexus" / "deployment.py"
NODE_MAIN = ROOT / "openplexus" / "node_main.py"
RELATION_CONTRASTIVE = ROOT / "tools" / "relation_contrastive.py"
NGRAM = ROOT / "openplexus" / "ngram.py"
REWARD_RECALL = ROOT / "openplexus" / "tasks" / "reward_recall.py"
KINSHIP = ROOT / "openplexus" / "tasks" / "kinship.py"
CLOSURE = ROOT / "openplexus" / "tasks" / "closure.py"
CONTENT = ROOT / "openplexus" / "content.py"
OCCASIONS = ROOT / "openplexus" / "tasks" / "occasions.py"
XSL = ROOT / "openplexus" / "tasks" / "xsl.py"
MNIST = ROOT / "openplexus" / "tasks" / "mnist.py"
SPOKEN = ROOT / "openplexus" / "tasks" / "spoken.py"
GROUNDING = ROOT / "openplexus" / "grounding.py"
BUCKETS = ROOT / "openplexus" / "buckets.py"
FEDERATED = ROOT / "openplexus" / "federated.py"
BUCKET_SERVICE = ROOT / "openplexus" / "bucket_service.py"
BUCKET_PEER = ROOT / "openplexus" / "bucket_peer.py"
OWNERSHIP = ROOT / "openplexus" / "ownership.py"
PARTITIONED = ROOT / "openplexus" / "partitioned.py"
SEARCH = ROOT / "openplexus" / "search.py"
CORPUS = ROOT / "openplexus" / "tasks" / "corpus.py"
SLOT_COST = ROOT / "tools" / "slot_cost.py"
RECOVERY = ROOT / "tools" / "recovery.py"
TESTBED = ROOT / "testbed" / "run.py"
ANSWERS = ROOT / "openplexus" / "answers.py"
RENDER = ROOT / "openplexus" / "render.py"
FAMILIES = ROOT / "openplexus" / "tasks" / "families.py"
CLUTRR = ROOT / "openplexus" / "tasks" / "clutrr.py"
SURFACES = ROOT / "openplexus" / "surfaces.py"


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
        name="a-failed-tc-lets-the-node-join-anyway",
        breaks="the guarantee that an impaired run was impaired. The node would "
               "join with a clean link and its vote would be recorded as a "
               "latency measurement",
        path=TESTBED,
        old='    return " ".join(parts) + " || exit 3; "',
        new='    return " ".join(parts) + " ; "',
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
        name="the-cliff-rule-takes-the-SMALLEST-gap",
        breaks="the one thing that makes the answer's size unfitted. The rule "
               "still reads the index, still derives a count from data, still "
               "returns a plausible set -- it just cuts where the ranking is "
               "FLATTEST instead of where it falls off, which on families means "
               "stopping inside the family after one or two siblings. So recall "
               "collapses while PRECISION STAYS PERFECT, and precision is the "
               "column decision 167 taught us to watch. Decision 171's whole "
               "claim is that an argmax over gaps replaces a fitted constant; "
               "reversing the argmax keeps the shape and loses the content",
        path=GROUNDING,
        old="    return max(range(len(gaps)), key=gaps.__getitem__) + 1",
        new="    return min(range(len(gaps)), key=gaps.__getitem__) + 1",
    ),
    Mutation(
        name="a-revisited-entity-gets-a-fresh-slot",
        breaks="note 059's whole finding, silently, and in the direction that makes "
               "the benchmark look easier than it is. Renumbering per EDGE rather "
               "than per NODE gives an entity that appears in four edges four "
               "different slots, so the store never sees the repeat -- and repeated "
               "entities are this project's measured weak point (103: 0.884 -> "
               "0.303). The 433 hard test puzzles would quietly become easy ones, "
               "max_appearances would stay at 2 everywhere, the confound split "
               "would report nothing, and CLUTRR's scores would come out HIGH for a "
               "reason having nothing to do with the model",
        path=CLUTRR,
        old="        if node not in order:\n            order[node] = config.entity_base + len(order)",
        new="        order[node] = config.entity_base + len(order)",
    ),
    Mutation(
        name="forward-secretly-symmetrises",
        breaks="the one combiner that refuses an ever-present distractor "
               "starts mixing in the backward direction, where the distractor "
               "scores 1.0 because it genuinely is always there",
        path=GROUNDING,
        old='    "forward": lambda a, b: a,',
        new='    "forward": lambda a, b: (a + b) / 2.0,',
    ),
    Mutation(
        name="the-edge-weight-is-one-directional",
        breaks="strength stops being symmetric, so an edge means something "
               "different depending on which end asks -- and the soft "
               "mutuality that refuses an ever-present distractor is gone",
        path=GROUNDING,
        old="    return COMBINERS[combine](statistic(index, one, other),\n"
            "                              statistic(index, other, one))",
        new="    return statistic(index, other, one)",
    ),
    Mutation(
        name="the-walk-ignores-its-beam",
        breaks="reach expands every partner, so the SEARCH budget stops "
               "bounding anything and a query costs O(N**depth)",
        path=GROUNDING,
        old="            for score, other in scored[:beam]:",
        new="            for score, other in scored:",
    ),
    Mutation(
        name="the-path-strength-does-not-decay",
        breaks="a long weak route scores the same as a short strong one, so "
               "the ranking stops meaning distance at all",
        path=GROUNDING,
        old="                travelled = carried * score",
        new="                travelled = score",
    ),
    Mutation(
        name="the-damping-exponent-is-ignored",
        breaks="every alpha collapses to conditional, so the sweep's axis is "
               "flat and reports one statistic under five names",
        path=GROUNDING,
        old="        return index.together(surface, other) / (common ** alpha)",
        new="        return index.together(surface, other) / common",
    ),
    Mutation(
        name="the-audio-sample-is-a-prefix",
        breaks="a draw becomes a prefix, and FSDD filenames start with the "
               "digit, so a sample silently becomes one digit's recordings",
        path=SPOKEN,
        old="    return sorted(random.Random(seed).sample(paths, count))",
        new="    return paths[:count]",
    ),
    Mutation(
        name="ownership-falls-back-to-modulo",
        breaks="the only reason the ring exists. `hash % nodes` remaps NEARLY "
               "EVERY key when the node count changes, and C3's premise is "
               "that machines leave without warning constantly -- so one "
               "machine joining would relocate the whole store. The ring moves "
               "about 1/n instead. The two are indistinguishable on a static "
               "network and differ completely the moment membership moves, "
               "which is the only regime this project cares about",
        path=OWNERSHIP,
        old="        index = int(np.searchsorted(self._positions, at, "
            "side=\"left\"))",
        new="        index = at % len(self._owners)",
    ),
    Mutation(
        name="every-node-gets-one-position-on-the-ring",
        breaks="load balance. With a single label per node the ring is lumpy: "
               "a node landing next to another owns almost nothing while a "
               "node with a large gap owns far too much -- and a departure "
               "dumps its whole share on one successor rather than scattering "
               "it, which is the property C3 wants",
        path=OWNERSHIP,
        old="REPLICAS = 64",
        new="REPLICAS = 1",
    ),
    Mutation(
        name="every-surface-of-the-subject-is-always-present",
        breaks="the only thing that makes the grounding falsifier an experiment. "
               "With every surface of a concept present whenever it is the "
               "subject, the true partner and the ever-present distractor have "
               "IDENTICAL counts, so raw counting ties rather than failing and "
               "every arm's result follows from arithmetic. The run would still "
               "produce a scorecard, and it would be measuring construction",
        path=OCCASIONS,
        # RE-POINTED with `noise-can-be-drawn-from-the-subject-itself`, same
        # cause: `draw_occasion` was extracted from `generate`, so the body
        # lost one level of indentation. Nothing else changed.
        old="            present = [s for s in own if rng.random() < config.presence]",
        new="            present = list(own)",
    ),
    Mutation(
        name="the-images-drift-out-of-step-with-their-labels",
        breaks="the only thing that makes gate G7's first real test scorable. "
               "An off-by-one stride pairs every image with the NEXT one's "
               "label, so a perfectly good grouping is scored against the wrong "
               "answer and reads as a mechanism that learned nothing -- a null "
               "result manufactured by the ruler",
        path=MNIST,
        old="    images = [pixels[i * stride:(i + 1) * stride] for i in range(take)]",
        new="    images = [pixels[i * stride:(i + 2) * stride] for i in range(take)]",
    ),
    Mutation(
        name="a-trial-presents-only-the-CORRECT-word-object-pairing",
        breaks="the whole difficulty the experiments were built to pose. A "
               "trial shows several words and several objects UNPAIRED, so on "
               "any one trial a word is equally consistent with every object "
               "present; emitting each word beside only its own object hands "
               "the answer over, and the mechanism would score perfectly on a "
               "task nobody ran",
        path=XSL,
        old="            tuple(sorted([self.word(v) for v in line]\n"
            "                         + [self.object(v) for v in line]))",
        new="            tuple(sorted([self.word(line[0]), self.object(line[0])]))",
    ),
    Mutation(
        name="a-chain-links-every-modality-to-every-other",
        breaks="the only thing that makes G7's question a question. `chain` and "
               "`star` exist so some of a concept's surfaces are NEVER seen "
               "together and can only be reached through what sits between "
               "them; returning the complete group instead means every pair was "
               "directly observed, `apart()` is empty, and the walk is scored on "
               "links it never had to infer",
        path=OCCASIONS,
        old='        if self.pairings == "complete":\n'
            "            return (every,)",
        new="        if True:\n"
            "            return (every,)",
    ),
    Mutation(
        name="noise-can-be-drawn-from-the-subject-itself",
        breaks="the separation between signal and distraction. Noise is the "
               "sofa -- a thing that happened to be in the room -- and drawing "
               "it from the subject's own surfaces hands the mechanism extra "
               "co-occurrence for free, so `presence` stops describing the "
               "stream and every recovery score is inflated by an amount nobody "
               "controls",
        path=OCCASIONS,
        # RE-POINTED when `draw_occasion` was extracted from `generate` so an
        # askable world could draw from the identical distribution. The body
        # moved out of a `for` loop into a module-level function, so it lost one
        # level of indentation and nothing else. `--verify` caught it, which is
        # the whole reason that check runs first.
        old="            if s // config.surfaces != subject]",
        new="            if True]",
    ),
    Mutation(
        name="a-link-does-not-have-to-be-returned",
        breaks="mutuality, which is the only thing stopping a surface present on "
               "every occasion from attaching itself to the entire world. It is "
               "in everyone's top list and nobody is in its; a one-sided rule "
               "gives it an edge to every surface there is, and the walk returns "
               "one class containing everything. Measured on OpenEA as the merge "
               "gate that works where a confidence gate does not",
        path=GROUNDING,
        old="            if surface in top.get(other, ()):        # mutual, or no edge",
        new="            if True:",
    ),
    Mutation(
        name="the-neighbour-list-is-padded-out-to-k",
        breaks="the difference between a cap and a quota. A statistic returning "
               "zero is refusing to claim a link, and keeping those entries "
               "manufactures edges out of no evidence -- so a surface with one "
               "real partner acquires k-1 invented ones, and the invented ones "
               "are whichever happened to sort first",
        path=GROUNDING,
        old="    scored = [(score, other) for score, other in scored if score > 0.0]",
        new="    scored = list(scored)",
    ),
    Mutation(
        name="a-moment-is-a-list-rather-than-a-set",
        breaks="what an occasion IS. A surface appearing twice in one moment "
               "would count as its own partner and as a doubly-strong partner "
               "to everything else present, so a duplicate anywhere upstream "
               "silently reweights the whole table",
        path=GROUNDING,
        old="        present = sorted(set(surfaces))",
        new="        present = sorted(surfaces)",
    ),
    Mutation(
        name="the-node-rounds-the-TRUE-time-not-its-own-clock",
        breaks="the entire reason a bucket exists. Two machines agree because "
               "each does the same arithmetic on ITS OWN clock, and they "
               "disagree by exactly as much as their clocks do. Reading the "
               "true time makes every node perfectly synchronised for free, so "
               "the skew axis becomes inert and every impaired cell reports the "
               "clean number -- a mechanism that runs, produces plausible "
               "output, and is measuring a world that does not exist",
        path=BUCKETS,
        old="        reading = observation.when + self._offset[observation.observer]",
        new="        reading = observation.when",
    ),
    Mutation(
        name="a-pair-is-counted-in-every-bucket-that-saw-it",
        breaks="the one-bucket-decides rule that makes overlapping windows "
               "safe. Without it a pair is counted once per SHARED bucket, so "
               "the multiplier is how well the two observers' clocks agree "
               "rather than how often the two things co-occurred. Measured at "
               "5x before it was fixed, and invisible downstream because the "
               "counts still look like counts",
        path=BUCKETS,
        old="            if ((one[1] + other[1]) // 2) // width == bucket:",
        new="            if True:",
    ),
    Mutation(
        name="a-marginal-is-counted-in-every-bucket-it-reaches",
        breaks="the denominator every chance-corrected statistic divides by. "
               "With `spread` on, a surface reaches 2*spread+1 buckets, so its "
               "`seen` is inflated by that factor while its pair counts are "
               "not -- which drives conditional and PPMI down for every surface "
               "uniformly and looks like a weaker signal rather than a bug",
        path=BUCKETS,
        old="            if reading // width == bucket:\n"
            "                self.index.note(surface)\n"
            "                noted = True",
        new="            if True:\n"
            "                self.index.note(surface)\n"
            "                noted = True",
    ),
    Mutation(
        name="an-observation-that-missed-its-bucket-is-taken-anyway",
        breaks="the deadline, which is the only thing making a time key "
               "transient. A bucket is discarded once its grace expires and "
               "there is nothing durable to add a late observation to; "
               "accepting one regardless makes lateness free, so the C2 axis "
               "reports the clean number at every delay",
        path=BUCKETS,
        old="            if arrives > closes:\n"
            "                continue",
        new="            if False:\n"
            "                continue",
    ),
    Mutation(
        name="the-LAST-reading-into-a-bucket-wins",
        breaks="the marginal of anything that recurs, once windows overlap. A "
               "bucket holds each surface once and counts its marginal only at "
               "the bucket the reading centres on; with `spread` on, several "
               "neighbouring moments write the same surface into one bucket, so "
               "keeping the last writer leaves a reading centred elsewhere and "
               "the marginal is counted NOWHERE. Measured at c(distractor)=1 "
               "against 8,000, which read as a flaw in overlapping windows "
               "rather than as a bug",
        path=BUCKETS,
        old="            if observation.surface in held:\n"
            "                if held[observation.surface] // config.width == bucket:\n"
            "                    continue",
        new="            if False:\n"
            "                if held[observation.surface] // config.width == bucket:\n"
            "                    continue",
    ),
    Mutation(
        name="dropped-observations-count-against-the-message-bill",
        breaks="what the bandwidth figure is a statistic OF. An observation lost "
               "before it left its observer sent nothing, so counting it in the "
               "denominator reports plain rounding as costing LESS than one "
               "message each -- a saving that did not happen, on a number that "
               "exists to price the network",
        path=BUCKETS,
        old="        sent = self.delivered + self.lost_late",
        new="        sent = self.delivered + self.lost_late + self.lost_dropped",
    ),
    Mutation(
        name="a-closed-peer-goes-on-serving",
        breaks="what it means for a node to LEAVE. Without the accept timeout "
               "the serve loop never reads `_running` again, and closing a "
               "socket another thread is blocked on inside `accept` wakes it on "
               "Windows but NOT on Linux -- so a shut-down peer keeps answering "
               "and keeps taking forwards. Any churn measurement over this "
               "harness would be measuring nodes that had not actually gone. "
               "NOTE: this bites on LINUX, which is where the harness runs. It "
               "may survive a local Windows run, and that platform gap is the "
               "bug itself rather than a weakness in the mutation",
        path=BUCKET_PEER,
        old="        self._listener.settimeout(0.25)",
        new="        self._listener.settimeout(None)",
    ),
    Mutation(
        name="the-reply-goes-out-before-the-writes-land",
        breaks="what a reply MEANS. Forwarding after replying lets a FLUSH "
               "return while its NOTE and LINK messages are still in flight, so "
               "a caller reading a count straight afterwards reads one that has "
               "not arrived. In ONE PROCESS this races fast enough to pass "
               "every test; across three OS processes it did not, which is how "
               "it was found",
        path=BUCKET_PEER,
        old="            for destination, forward in outbox:\n"
            "                self._forward(destination, forward)\n"
            '            send(connection, json.dumps(reply).encode("utf-8"))',
        new='            send(connection, json.dumps(reply).encode("utf-8"))\n'
            "        for destination, forward in outbox:\n"
            "            self._forward(destination, forward)",
    ),
    Mutation(
        name="an-undeliverable-write-leaves-no-evidence",
        breaks="the only thing a server thread can do about a lost message. "
               "Forwarding has no caller to catch anything, so a swallowed "
               "failure loses the write AND the record of it -- and g33-01 "
               "measured what missing writes do to a recovery: they read as a "
               "weaker signal rather than as a fault, on a run that reports "
               "itself healthy",
        path=BUCKET_PEER,
        old="            self.failures.append((destination, message, str(unreachable)))",
        new="            pass",
    ),
    Mutation(
        name="a-node-serves-a-key-it-does-not-own",
        breaks="the only thing that makes a SEPARATED store different from a "
               "separable one. A service answering for another node's surface "
               "returns the right number from the wrong arrangement -- every "
               "count still matches the single-process reference, every test "
               "about totals still passes, and the claim that a row lives at "
               "its owner is silently false",
        path=BUCKET_SERVICE,
        old="        if not self.owns(key):",
        new="        if False:",
    ),
    Mutation(
        name="a-missing-marginal-is-treated-as-zero",
        breaks="the loudness that catches a broken fetch. A candidate whose "
               "marginal never arrived scores zero under every "
               "chance-corrected statistic, so the walk returns each surface "
               "alone and the result reads as a null rather than as a message "
               "that was not sent. That exact failure happened once already, in "
               "the first federated walk",
        path=BUCKET_SERVICE,
        old="        raise KeyError(",
        new="        return 0 or KeyError(",
    ),
    Mutation(
        name="one-node-writes-BOTH-halves-of-a-pair",
        breaks="the locality this module exists to demonstrate. A pair is two "
               "rows on two machines and each owner writes only its own "
               "direction; writing both puts `owner(x)` in possession of a row "
               "it does not own, which is the shared state amended C1 forbids. "
               "Every number would be identical, because the arithmetic is the "
               "same -- only the ownership is wrong",
        path=FEDERATED,
        old="        for surface, partner in ((one, other), (other, one)):",
        new="        for surface, partner in ((one, other), (one, other)):",
    ),
    Mutation(
        name="a-departed-owner-reports-a-marginal-of-zero",
        breaks="the difference between a node that LEFT and a surface nobody "
               "ever saw. Zero is an ordinary count, so it drives every "
               "chance-corrected score to zero and the candidate is silently "
               "ranked last instead of being dropped as unscoreable -- and the "
               "unreachable counter never moves, so a degraded run is "
               "indistinguishable from a healthy one",
        path=FEDERATED,
        old='            raise KeyError(\n'
            '                f"node {target} owns surface {surface} and has departed. "',
        new='            return 0 or KeyError(\n'
            '                f"node {target} owns surface {surface} and has departed. "',
    ),
    Mutation(
        name="a-node-reads-a-peers-marginal-from-its-own-table",
        breaks="the boundary between what a node holds and what it must ask "
               "for. `count(y)` lives at `owner(y)`; served locally it comes "
               "back 0 for anything this node does not hold, so every "
               "chance-corrected score collapses and the walk returns each "
               "surface alone. It also makes the read look FREE, which is the "
               "one number this module was built to produce",
        path=FEDERATED,
        old="        return self._federation.seen(surface, asker=self._home)",
        new="        return self._table.seen(surface)",
    ),
    Mutation(
        name="crossing-a-node-boundary-is-not-counted",
        breaks="the price of the only statistic that works. `conditional` needs "
               "one peer read per candidate partner and the whole point of a "
               "counted federation is that the figure is measured rather than "
               "argued; a silent counter reports the design as free",
        path=FEDERATED,
        old="        if asker is not None and asker != target:\n"
            "            self.remote_reads += 1",
        new="        if False:\n"
            "            self.remote_reads += 1",
    ),
    Mutation(
        name="a-node-passes-off-its-own-share-as-the-worlds-total",
        breaks="the refusal that keeps PPMI honest. A node knows how many "
               "occasions IT saw and nothing about the rest; returning that as "
               "`occasions` makes `ppmi` run and produce a plausible number "
               "computed against the wrong denominator on every node, which is "
               "worse than the statistic being unavailable",
        path=FEDERATED,
        old="        raise NotImplementedError(",
        new="        return self._table.occasions or NotImplementedError(",
    ),
    Mutation(
        name="the-composition-search-only-brackets-from-the-left",
        breaks="the measurement that says what CLUTRR-symbolic is worth. "
               "Reducing left to right instead of over every span answers "
               "0.2757 of the test split rather than 1.0000, and 0.0252 at ten "
               "hops -- so the benchmark would read as hard, the ceiling would "
               "be invisible, and a model scoring 0.5 on it would look like "
               "evidence of composition rather than like half of what a table "
               "of 62 counted facts does",
        path=CLUTRR,
        old="            for split in range(start + 1, stop):",
        new="            for split in range(start + 1, start + 2):",
    ),
    Mutation(
        name="a-three-hop-row-is-counted-as-a-composition-fact",
        breaks="the line between what the data STATES and what has to be "
               "inferred. A three-hop row constrains the algebra without "
               "determining it, and folding one into the table means the "
               "ceiling is computed from inference and then reported as the "
               "cost of counting",
        path=CLUTRR,
        old="        if len(puzzle.chain) == 2:",
        new="        if len(puzzle.chain) >= 2:",
    ),
    Mutation(
        name="the-front-end-draws-its-planes-without-the-seed",
        breaks="the only reason the hash replaced a trained quantiser. The "
               "planes are the shared constant, and drawn afresh they are "
               "per-process: two nodes send the same input to different "
               "surfaces, so a write and a read go to different machines and "
               "the count that should have accumulated never does. NOTHING "
               "ELSE MOVES -- one node's own scores are identical, purity is "
               "identical, and every single-process measurement in this "
               "repository still passes",
        path=SURFACES,
        old="        self._planes = np.random.default_rng(seed).normal(",
        new="        self._planes = np.random.default_rng().normal(",
    ),
    Mutation(
        name="an-input-with-no-content-gets-a-code-anyway",
        breaks="the refusal that keeps a surface from being made out of "
               "silence. A zero vector sits on every plane at once, so its "
               "sign pattern is the tie-break rather than the input -- and "
               "every empty input in the stream lands on ONE code together, "
               "which then co-occurs with everything and reads as the hub the "
               "walk is built to refuse",
        path=SURFACES,
        old="        if not np.any(vector):\n            return -1",
        new="        if False:\n            return -1",
    ),
    Mutation(
        name="the-batch-packs-its-bits-the-other-way-round",
        breaks="the agreement between one matrix product and one call per row. "
               "The partition is IDENTICAL either way and every purity, every "
               "distinct-code count and every collision rate comes out the "
               "same, so nothing measured on codes alone can see it -- but a "
               "node quantising in batches and a node quantising one arrival "
               "at a time would name the same input differently, which is the "
               "exact failure the hash exists to make impossible",
        path=SURFACES,
        old="        weights = 1 << np.arange(self.bits - 1, -1, -1)",
        new="        weights = 1 << np.arange(self.bits)",
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


#: What `--help` prints. **It exists because `--help` used to run the whole
#: suite**: unrecognised flags were ignored, so the one command every tool in the
#: world treats as safe took the tree exclusively, mutated `corpus.py`, and left
#: an experiment that was already running to read mutated source for as long as
#: it took someone to notice. 2026-07-29, and the experiment's results were void.
USAGE = """tools/mutate.py -- verify the tests can fail

  --verify          check the source is clean; edits nothing, takes no lock
  --changed         only mutations touching files changed against origin/master
  --only NAME[,..]  named mutations
  --shard N/TOTAL   one shard of the mutation set, for CI
  --help            this text

**This edits source in place and takes the tree exclusively.** Nothing else may
read the repository while it runs -- see CLAUDE.md on stopping one safely.
Mutations belong in CI; run them locally only for one or two named cases."""


def main(argv: list[str] | None = None) -> int:
    argv = sys.argv[1:] if argv is None else argv
    if "--help" in argv or "-h" in argv:
        # BEFORE the lock and before anything is written, for the same reason
        # `--verify` is: the whole point is that it does nothing.
        print(USAGE)
        return 0
    unknown = [a for a in argv if a.startswith("-")
               and a.split("=")[0] not in ("--verify", "--changed", "--only",
                                           "--shard", "--help", "-h")]
    if unknown:
        # SILENTLY IGNORING A FLAG IS HOW `--help` RAN THE SUITE. A typo in a
        # flag should cost an error, not a tree-exclusive run.
        print("unknown option(s): " + " ".join(unknown))
        print()
        print(USAGE)
        return 2
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
