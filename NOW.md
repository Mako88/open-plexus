# Now

What is being worked on, and what has been agreed but not started.

**The invariant that makes this worth having:** every 🚧 in
[README.md](README.md) appears here, and nothing appears here that is not in the
README. An approved piece of work cannot go quiet, which is how the LSH front end
was agreed and then dropped for two sessions — it sat as ⬜ in the tree, identical
to options nobody had ever considered, and the next planning pass never saw it.

Delete a line when it is done. This file is disposable and nothing may cite it.

---

## Agreed, not started

- Nothing.

## In flight

- Nothing.

## Next, in the order I would take them

1. **CLUTRR composition on the count graph**, below. Agreed with John after the
   LSH front end, and it is kill-list #1.
2. **An error signal** (README §7). Nothing is currently ever wrong, because
   counts only go up. Predicting *relations* rather than tokens gives a signal
   that can be wrong without being next-token prediction.
3. **Compression** (README §7). One principle that supplies forgetting,
   hierarchy, and a reason to reorganise — three holes that are currently three
   separate open questions.

## Known debts from the restructure

- **Kill-list #1 has lost its instrument and needs a new one.** 62 facts counted
  from CLUTRR's two-hop rows plus a bracketing search answer 100% of the split
  (`clutrr_ceiling.py`), and withholding facts does not repair it — the three-hop
  rows determine every held-out pair by deduction, ceiling back to 0.98
  (`clutrr_headroom.py`). Both were cheap and both were run before building on
  the benchmark, which is the only reason two dead ends cost hours rather than
  weeks. **The question that decides the project now has nothing pointed at it.**
  The next candidate is FB15k-237, which exists because its predecessor leaked
  the same way; **run the leakage audit on it before anything else**, since that
  is exactly what these two runs would have saved.
- **There is no node entry point.** `node_main.py` started the old store and was
  deliberately not carried over.
- **`openplexus/tasks/asking.py` has no tests and no mutation**, so by this
  project's own rules it is not finished.
- **`experiments/` has two scripts and no harness.** `surfaces_bits.py` and
  `surfaces_pipeline.py` each carry their own argument parsing and their own JSON
  writing. That is fine for two and is a copy at four.
- **The link columns in `surfaces_pipeline.py` step in tenths.** They are shares
  over ten words, so a single code moving changes a column by 0.1 and no
  difference smaller than that can be read. Whatever compares front ends
  downstream next needs a finer denominator than the vocabulary.
- **The front end is not wired to anything that runs as a node**, because there is
  no node entry point (above). It is used by the two sweeps and nowhere else.
