074 — Note 065's baseline has no committed script, and it blocks the partitioning result
=======================================================================================

**Status: a gap in the record, not a measurement.** Found while trying to run the
comparison the concept-partitioning work exists to produce. Recorded because a hole nobody
has written down is indistinguishable from solid ground.

---

## IN PLAIN TERMS

Note 065 reports the largest single mechanism gain in this project — **+0.219 chain
recovery, and 713 out of 713 on the clean subset.** The traversal path for concept
partitioning is now built, and the obvious next step is to run the same measurement with
the store split across nodes and see whether it costs anything.

**That cannot be done, because nothing in the repository records how 065's number was
produced.**

---

## What is missing, specifically

The repository holds the pieces and not the arrangement:

    openplexus/tasks/clutrr.py    the data, and `tests/test_clutrr.py` covers the
                                  LOADER only -- no model is constructed in it
    openplexus/search.py          `beam`, tested on synthetic fixtures
    openplexus/models/…           the model

**No committed script runs the model over CLUTRR puzzles and calls `beam` on the result.**
So the configuration behind 0.8805 is unrecorded: width, seed, `context_keys`, whether the
model learns as it reads, the retrieval strategy, `spread`. Rebuilding it means guessing,
and a rebuilt harness that produced a different number could not be attributed — harness or
config, with no way to tell.

## Why this is the blocking problem and not a nuisance

A partitioning result is only meaningful as a *difference* from the monolithic baseline. Two
numbers from two harnesses differ for two reasons at once, which is the failure mode the
reproduce-a-known-number rule exists to prevent — and that rule has fired three times in
this session alone (069's baseline, 070's protocol, 071's gate).

**So the partitioned CLUTRR measurement is not merely unfinished. It is unattemptable until
the baseline is reproducible.**

## What would close it

    build tools/clutrr_recovery.py, and gate it on reproducing 065's OWN numbers
    to four decimals across its three seeds:

        search(b=4)        0.6588 / 0.6632 / 0.6623
        beam(w=4, b=4)     0.8735 / 0.8831 / 0.8848
        plain subset       713/713

**Three seeds matching to four decimals is not luck**, so hitting them would establish the
configuration was recovered rather than invented. Missing them says the config is wrong and
says nothing about partitioning, which is exactly the attribution that matters.

## The general form, which is worth more than this instance

Notes 070 through 073 each ship a tool — `relation_profiles.py`,
`relation_addressing.py`, `ownership_balance.py` — because a number with no script is not
reproducible. **Note 065 predates that habit and is the most-cited number in the project.**
Any earlier note may have the same hole, and the ones whose numbers are load-bearing are
worth auditing for it: `SCALE.md`'s capacity fit and decision 134's lone-node capacity are
both quoted constantly and neither was checked here.

## What is NOT claimed

**Not that 065 is wrong.** Nothing here casts doubt on 0.8805; the walk was measured and
the note's reasoning is intact. What is missing is the ability to measure *alongside* it.

**And not that guessing the config is hopeless** — the gate above is exactly the instrument
for finding out, and a few candidate configurations may land it quickly. That is the next
piece of work rather than a reason to report a partitioning number without a baseline.
