# 105 — the partitioning accuracy figure has no source

2026-07-30. `tools/check_provenance.py`, on its first run, over the two option records
that existed before the migration. An audit finding rather than an experiment, plus the
measurement dispatched to settle it.

## IN PLAIN TERMS

The tree says splitting the memory across four machines makes the model **more** accurate,
not just able to hold more — `0.9220` against `0.8877`. That pair of numbers is quoted in
two places and cited to a third, and **the measurement it came from is not in this
repository.** No note contains it, no sweep record contains it, no script output contains
it. Two notes quote it, credit two different sources, and pair it with two different
baselines.

The capacity argument for partitioning is not affected — that one is measured, in note
081, and it is the argument that makes partitioning mandatory. What is unsupported is the
smaller claim sitting next to it, that partitioning also helps accuracy. A yes means the
claim was right all along and now has a run behind it; a no means a number that reads as
measured was never measured, and the tree has been carrying it as evidence.

## What was found

`0.9220` and the pair it forms with `0.8877` appear in exactly these places:

    DECISIONS.md:405   "4 concept nodes give beam 0.9220 against 0.8877 monolithic"
    DECISIONS.md:660   "beam 0.9220 at 4 nodes against 0.8877 monolithic"   cites note 081
    note 103:75        "Note 081's companion measurement has 4 concept nodes giving
                        beam 0.9220 against 0.8877 monolithic"
    note 090:84        "Note 065 measured chain recovery at 0.8805 monolithic and
                        0.9220 partitioned"

and nowhere else. Specifically:

- **note 081 contains no partitioning accuracy measurement at all.** It has no `0.9220`,
  no `0.8877`, no `713`, and no mention of a companion. Its subject is capacity: recall
  `0.07` at 10.6× overload, and decay windows at `0.990` on the last hundred and `0.000`
  older.
- **note 065 contains no partitioning arm.** The strings `partition`, `concept_nodes` and
  `nodes` do not occur in it. Its `0.8805` is the beam mean of a single-process run whose
  configuration note 074 established is unrecoverable.
- **the two quotations disagree about the baseline.** Note 090 pairs `0.9220` with
  `0.8805`; note 103 pairs it with `0.8877`. Those are two different measurements —
  065's original and 075's re-measurement — so at most one pairing can be a difference
  taken within one run.

## How it propagated, which is the reusable part

Note 103's own text says how: *"I nearly wrote it up as a lead before finding it already
in the tree."* It found the claim in `DECISIONS.md`, which cited note 081, and cited it
onward as *"note 081's companion measurement"* — a source it did not open. The tree cites
the note, the note cites the tree, and neither holds the run.

**This is CLAUDE.md rule 1's borrowed-claim failure inside one repository.** The rule is
written about the literature — *"a summary tells you what a result is called, not what was
actually run"* — and the same gap opens between two of our own documents in a day. It is
decision 118's shape exactly: `4.540 bits, unigram BEATEN` was carried as the headline
text result for weeks and appeared **only in a scratch session-swap document**, with no
sweep and no entry behind it.

**And nothing downstream could have caught it.** Every experiment run since is conditioned
on the claim rather than testing it, which is the reason rule 1 singles borrowed claims
out from unverified claims generally.

## What is NOT in doubt

- **The capacity argument.** Note 081's numbers are in note 081, and they are what makes
  partitioning mandatory for C4: a single store recovers `0.07` at 10.6× capacity,
  symmetrically in age, so it is interference rather than forgetting and replay cannot fix
  it.
- **That partitioning helps accuracy at all.** Note 103 ran it at eight nodes, seed 0,
  and those numbers ARE in note 103: `0.9058` at eight nodes against `0.8770` monolithic,
  same script, same seed. The direction is measured. The size and the four-node figure are
  what have no source.

## The prediction, registered before the run returned

`tools/clutrr_recovery.py --concept-nodes 4 --seeds 0 1 2` was dispatched before this
paragraph was written, and its output had not been read when the predictions below were
recorded.

1. **Partitioning at four nodes beats monolithic.** Note 103's eight-node run is +0.029 at
   seed 0 and the mechanism — a node carries interference only from what it owns — has no
   reason to reverse at four. Refuted if the difference is negative or within seed spread.
2. **The gain is SMALLER than 0.9220 − 0.8877 = 0.0343.** Note 103's eight-node gain is
   +0.0288 with more nodes, and more nodes should mean less interference per node, so a
   four-node gain above the eight-node one would be the surprising direction.
3. **The absolute figure lands near 0.90, not 0.9220.** Monolithic on this script is
   0.8770 at seed 0 (note 103) and 0.8877 as a three-seed mean (note 075), so a
   four-node result at 0.9220 would require a gain twice the eight-node one.

Prediction 3 is the one that decides whether `0.9220` was a real number quoted without its
provenance, or a number that was never taken.

## Outcome — the number is real, and reproduces to four decimal places

    tools/clutrr_recovery.py --concept-nodes 4 --seeds 0 1 2
    width 64, decay 1.0, branches 4, beam width 4, layout kinship, route current

     seed   search     beam      plain
        0   0.7880   0.9049    713/713
        1   0.8290   0.9389    713/713
        2   0.8098   0.9223    712/713

    mean search 0.8089, beam 0.9220, gain over 065's reported search +0.1131
    scored 1146 puzzles, 0 skipped as unreachable in the stated direction
    70 seconds, local

**Mean beam at four concept nodes is `0.9220`.** Against note 075's monolithic three-seed
beam mean of `0.8877` — same script, same three seeds — the difference is **+0.0343**, and
the pair is a like-for-like comparison after all. `713/713` on the plain subset is reached
at two of the three seeds and `712/713` at the third. Search also gains: `0.8089` against
note 075's monolithic `0.7810`.

**Predictions scored.**

1. **HELD.** Four nodes beat monolithic.
2. **REFUTED.** The gain is not smaller than `0.0343`; it is `0.0343`.
3. **REFUTED, and this is the finding.** The absolute figure did not land near 0.90. It
   landed on `0.9220` exactly, to four decimals.

**So nothing was fabricated and nothing was misquoted — the run happened and was never
written down.** The defect was entirely citational: two notes attributed a real
measurement to sources that do not contain it, and the tree inherited the attribution.
That is a smaller failure than the one this note was opened on, and it is worth saying
plainly rather than leaving the alarming version standing.

**It is also the failure that is hardest to catch by reading**, because every number
involved was correct. Only the pointer was wrong, and a wrong pointer is invisible until
someone follows it. Nobody had, in the eight notes written since.

**What this does NOT settle.** Note 090's pairing of `0.9220` with `0.8805` is still wrong
— `0.8805` is note 065's unrecoverable-configuration mean, not this script's monolithic
baseline, and note 074 is the entry that established the difference. The correct baseline
for `0.9220` is `0.8877`.

## What changes regardless of the outcome

- **`tools/check_provenance.py` runs in CI**, and every option record is subject to it.
  The rule this replaces was the habit of citing what you read; the habit did not hold for
  one day.
- **`DECISIONS.md` stops citing note 081 for an accuracy claim note 081 does not make.**
  Whichever way the run goes, that citation was wrong.
- **Note 090's sentence about note 065 is wrong as written** and note 090 is not edited,
  per the notes' own rule. This note is where that is recorded.
