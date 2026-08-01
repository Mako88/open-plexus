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

- **The LSH front end** (README §1). Replace `grouping.cluster` with
  random-hyperplane hashing from `openplexus/sketch.py`. Sweep the bit count, and
  add the agreement test the front end has never had: two nodes, different data
  samples, same seed — do the codes mean the same thing? That last part closes a
  falsifier that has been specified and unwritten since before this restructure.

## In flight

- Nothing.

## Next, in the order I would take them

1. **The LSH front end**, above. It is the only agreed-and-unstarted item, it is
   the one part of the architecture the README states and the code contradicts,
   and it settles the untested half of "independent nodes agree".
2. **An error signal** (README §7). Nothing is currently ever wrong, because
   counts only go up. Predicting *relations* rather than tokens gives a signal
   that can be wrong without being next-token prediction.
3. **Compression** (README §7). One principle that supplies forgetting,
   hierarchy, and a reason to reorganise — three holes that are currently three
   separate open questions.

## Known debts from the restructure

- **CLUTRR composition has to be re-established on the count graph.** The result
  that exists was measured on the store, which is gone. This is kill-list #1 and
  it is the question that decides the project.
- **There is no node entry point.** `node_main.py` started the old store and was
  deliberately not carried over.
- **`openplexus/tasks/asking.py` has no tests and no mutation**, so by this
  project's own rules it is not finished.
- **`experiments/` does not exist.** The harness that ran sweeps went with the old
  tree; whatever replaces it should emit structured rows rather than prose, so
  records are generated rather than written.
