# 030 — the benchmark does not discriminate, and what would

**Status:** measured, by counting, in seconds. No training, no vectors.
**Affects:** what the g9 line's numbers mean, and what to measure next.
**Does not affect:** whether any g9 number is correct. They are.

---

## IN PLAIN TERMS

The device stores things by blending them together into one big pattern. That is
the interesting, brain-like part, and the reason the project exists.

Two tests were run against the most boring possible alternative — a lookup table
that writes things down separately.

On the language test the table wins by a wide margin. On the test this project
built its main line of work around, the table is **perfect**, using a fraction of
the memory, with none of the machinery.

That does not mean the blended store is wrong. It means neither test is capable
of showing that it is right, and a test that cannot tell two things apart cannot
be used to choose between them.

---

## The two measurements

**[g10-06](../../experiments/sweeps/g10-06-a-cache-that-persists.txt)** — a
bounded per-key cache on character text, never cleared:

    32 slots, persisting     4.153 bits/char     2,752 token ids
    the vector store         5.734 bits/char     4,096 numbers at width 16

1.58 bits better, at two thirds of the memory, on the same stream under the same
rule.

**[g10-07](../../experiments/sweeps/g10-07-can-a-cache-do-the-gating-task.txt)**
— the same structure on `reward_recall`, the task the entire g9 gating line
exists for:

    cache of 24 entries      1.000 first-ask accuracy     48 integers
    cache of 16 entries      0.674                        32 integers
    the vector store         about 0.65                   4,096 numbers

24 is exactly `n_pairs`. The cache gets no reward signal and no oracle.

---

## What this changes about the g9 numbers

**Nothing about their correctness, and a great deal about their meaning.**

    in a CACHE          the gate buys MEMORY: 24 entries down to 4, at
                        identical accuracy
    in the VECTOR STORE the gate buys ACCURACY: 0.65 up to 1.000, because
                        superposition degrades as bindings accumulate

Every recovery ratio in the g9 line measures the second. The oracle's advantage —
the denominator of all of it — exists **because** 24 superposed bindings
interfere. For a structure that keeps them apart there is no advantage to
recover, because there is no deficit.

So the tag is a real mechanism, measured correctly, solving a problem that a
different storage choice does not have. That is worth knowing before more is
built on it, and it is not an argument that the work was wasted: *how to select
what to keep under superposition* remains the question the project's distribution
story requires an answer to.

---

## What a discriminating benchmark needs

A cache and a superposed store differ in four ways. `reward_recall` and
character-level text exercise none of them, which is why both fail to
discriminate.

1. **Generalisation by similarity.** A store returns something for a key it has
   never seen; a table returns nothing. Every query in `reward_recall` is an
   exact cue.

   > **MEASURED, and this was TWO properties wearing one name.**
   > [g10-09](../../experiments/sweeps/g10-09-is-there-similarity-to-generalise.txt).
   >
   > *Between items:* **unavailable.** `derived_keys` draws each token's key
   > independently, so off-diagonal overlaps are accidental — mean +0.0005
   > against a diagonal of 0.2522. Token 5 does not resemble token 6 and no task
   > exercising that can be built while keys are per-token.
   >
   > *Of a degraded query:* the store retrieves at **0.93-0.97** from a
   > half-destroyed key, and that number stands.
   >
   > **But the comparison was ill-posed and is RETRACTED.** A cache is indexed
   > by TOKEN ID, so it never receives a corrupted key — it scores **1.000** on
   > the same condition. The corruption exists only inside the store's own
   > representation. "A cache has no partial credit" compared the store against
   > a failure mode the cache does not have.
   >
   > With a genuinely corrupted TOKEN — what a damaged transmission actually
   > produces, since `derived_keys` sends ids and not vectors — the store gets
   > 0.090 against the cache's 0.015. Six times chance against chance: real,
   > tiny, and on garbage inputs.
   >
   > **So property 1 is empty in both halves**, and no discriminating task
   > follows from it.
2. **Graceful degradation.** A full table evicts an entry entirely; a store gets
   uniformly noisier. Neither task removes anything.
3. **Slicing by DIMENSION.** The whole distribution story is that a node holds
   part of *every* vector. A table shards by KEY — which is a DHT, and is
   exactly John's third question rather than a way around it.
4. **Compositional binding.** Retrieving with the result of a previous
   retrieval. `reward_recall` never chains.

**The third is the one already built.** `testbed/` runs nodes over an impaired
link and `distributed.py` supports departure mid-sequence. Losing a node from a
dimension-sliced store degrades every answer slightly; losing a node from a
key-sharded table loses those keys completely. **That is a measurable difference
between the two architectures, on machinery that exists**, and no benchmark in
this project currently measures it.

> **MEASURED, AND THE IMPLICATION ABOVE IS REFUTED.**
> [g10-08](../../experiments/sweeps/g10-08-which-degrades-better.txt):
>
>              structure    intact   one node lost    fall   relative
>       dimension-sliced     0.656           0.469  -0.188        29%
>       key-sharded (24)     1.000           0.776  -0.224        22%
>
> The MECHANISM is as described — the store falls smoothly, the table loses a
> quarter of its keys outright. **The outcome is the opposite of what this
> section implies.** The cache ends far higher, and it also falls by a smaller
> *fraction* of what it had. "Graceful" was doing no work: the store is not even
> relatively more robust.
>
> **And this section had the ordering wrong.** `reward_recall` was already known
> not to discriminate, so a cache starting 0.34 ahead is still ahead after both
> lose something. Churn can only be a tiebreaker on a task where the two are
> competitive INTACT, and no such task exists here. Churn was put first because
> the machinery existed — and machinery existing is not the same as the
> measurement being interpretable.
>
> The corrected ordering: **(1) find a task where the superposed store is
> competitive intact, (2) then ask whether it degrades better.**

The fourth is bAbI task 2, already on BACKLOG as the first item needing a new
mechanism rather than a new measurement.

---

## What I am not claiming

**That the associative store should be replaced.** Two tasks failing to
discriminate is evidence about the tasks. A cache has no story for goal 1 at all,
and the project's reason for a superposed store was never that it wins at
character prediction.

**That a cache scales.** `reward_recall` presents 24 bindings; a cache holding
one slot per binding is affordable there and would not be at a million. The
crossover is unmeasured.

**That this is bad news.** A benchmark that cannot fail is worse than one that
can, and finding out in an afternoon of counting is the cheapest possible way to
learn it.
