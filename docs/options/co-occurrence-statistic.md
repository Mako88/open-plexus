# Option record — which STATISTIC over co-occurrence counts says two surfaces are one thing

> **RECORD ONLY. This file carries no status.** Chosen, refused, untried or live-both lives
> in [DECISIONS.md](../../DECISIONS.md) alone. Here there are only events, and events do not
> un-happen, so nothing here can go stale. **Absence means untried.**
> Format and the CONFIG block: [README.md](README.md).

---

## What exists

- `openplexus/grounding.py`: `CoOccurrence` accumulates `count(x, y)` and `count(x)` per
  surface; `raw_count`, `frequency_weighted`, `conditional` and `ppmi` score a candidate
  partner; `neighbours` ranks; `equivalence_classes` keeps mutual top-`k` edges and returns
  connected components; `class_f1` and `score_classes` score a recovery.
- `openplexus/tasks/occasions.py`: the instrument. A stream of moments with known ground
  truth, a `presence` knob, a `zipf` knob and a persistent-distractor knob.
- `tests/test_grounding.py`, `tests/test_occasions.py`, and five mutations in
  `tools/mutate.py`.
- **No distribution.** No bucket, no join, no ownership. `grounding.py` says so in its own
  docstring and says why.
- [`content.py`](../../openplexus/content.py)'s `ContentIndex` predates all of it and
  accumulates co-occurrence into a superposed *vector*, which cannot hold a per-neighbour
  count and so cannot compute any of these statistics.

---

## What was tried, and what came back

### Raw counting is defeated by a distractor present every time — `g32-01`

    CONFIG  when    2026-07-31
            source  experiments/sweeps/g32-01-can-counting-tell-the-distractor.txt
            script  experiments/g32_01_can_counting_tell_the_distractor.py
            task    occasions, 64 concepts, 3 surfaces, presence 0.7, noise 3
            model   none -- counting only, no store and no vectors
            knobs   statistic, zipf, distractors, shuffled control; k 2; 3 seeds
            scale   8,000 occasions per stream

One surface present on every occasion costs `count` **0.3044** of f1 at zipf 0.0 —
1.0000 down to 0.6956 — and costs `weighted`, `conditional` and `ppmi` **0.0000** each,
all three staying at 1.0000.

Mutuality alone is not a sufficient defence, which the unit-test world had been too small
to settle. Normalising by the neighbour's own frequency is.

### The repair costs a remote read PER CANDIDATE — `g32-01`, an argument not a measurement

    CONFIG  when    2026-07-31
            source  experiments/sweeps/g32-01-can-counting-tell-the-distractor.txt
            script  none -- a locality argument, nothing measured
            task    n/a
            model   n/a
            knobs   none
            scale   n/a

`count(x, y)` and `count(x)` already sit at `owner(x)`. `count(y)` is a bounded message to
one named peer, which amended C1 permits where a collective everyone must join does not.

**This entry first said the repair costs ONE HOP, and that was wrong** — corrected the same
day, from building the distributed version. One hop is right for a single *pair*; ranking a
surface's partners needs `count(y)` for every candidate, so the cost grows with the partner
list. That is `peer.py`'s profile rather than a barrier's and it is not one message.

**Nothing has measured either version.** It is a reading of the constraint against the
arithmetic, and the container run is what would test it.

### PPMI is not deployable at all, and only building it showed that — `g33-01`

    CONFIG  when    2026-07-31
            source  openplexus/grounding.py, CoOccurrence.moment
            script  none -- found while writing openplexus/buckets.py
            task    n/a
            model   n/a
            knobs   none
            scale   n/a

PPMI divides by the number of occasions the **whole system** has seen. No node can know
that without a collective, and amended C1 forbids collectives — so the statistic that won
`g32-01` is a reference rather than a design.

It surfaced only when the join was built: the single-process accumulator maintains that
total for free, and nothing in `g32-01` had any reason to ask where it comes from.

### PPMI and the conditional are ONE arm above chance — `g32-01`

    CONFIG  when    2026-07-31
            source  experiments/sweeps/g32-01-can-counting-tell-the-distractor.txt
            script  experiments/g32_01_can_counting_tell_the_distractor.py
            task    occasions, as above
            model   none
            knobs   statistic
            scale   12 real cells

Identical to four decimals in every real cell. For a fixed surface `count(x)` and the
occasion total are constants, so PPMI is monotone in `count(x,y)/count(y)`, which is the
conditional. They order every above-chance pair identically and differ only in that PPMI
refuses the rest — **0 of 40** above-chance rankings differ on a random index against
**40 of 40** full rankings.

Two of four arms were one experiment, and a grid that probed below chance would separate
them. Nothing here does.

### The scoring metric has a floor of 0.5, not 0 — `g32-01`

    CONFIG  when    2026-07-31
            source  experiments/sweeps/g32-01-can-counting-tell-the-distractor.txt
            script  experiments/g32_01_can_counting_tell_the_distractor.py
            task    occasions, 3 surfaces per concept
            model   none
            knobs   shuffled control
            scale   36 streams

The shuffled control was predicted near zero and returned **0.3189** to **0.5078**. A
three-surface concept recovered entirely alone is perfectly precise and a third recalled,
which is f1 **0.5** — so *recovered nothing* scores 0.5, and the control scores below it
because grouping wrongly is worse than not grouping.

Carried at `class_f1`'s own definition, because that is where a reader stands.

### `captured` understates the harm by one to two orders of magnitude — `g32-01`

    CONFIG  when    2026-07-31
            source  experiments/sweeps/g32-01-can-counting-tell-the-distractor.txt
            script  experiments/g32_01_can_counting_tell_the_distractor.py
            task    occasions, as above
            model   none
            knobs   distractors 0 and 1
            scale   3 seeds

Where `count` loses to `ppmi`, the f1 gaps are **0.3044**, **0.3837** and **0.0908**
against `captured` gaps of **0.0174**, **0.0104** and **0.0156**.

Mutuality caps a distractor's degree at `k`, so it almost never *joins* a class — it
*displaces*, taking the top slot a true partner needed. The registered falsifier's own
metric counts joins and therefore measures the wrong thing.

### A concept needs about 16 occasions — `g32-02`

    CONFIG  when    2026-07-31
            source  experiments/sweeps/g32-02-how-many-occasions-does-a-concept-need.txt
            script  experiments/g32_02_how_many_occasions_does_a_concept_need.py
            task    occasions, 64 concepts, 3 surfaces, presence 0.7, noise 3, no distractor
            model   none
            knobs   stream length 256 to 16000, zipf 0.0; k 2; 3 seeds
            scale   uniform frequencies

Whole-stream f1 under `count`: **0.7468** at about 4 occasions each, **0.8863** at 8,
**0.9950** at 16, **1.0000** from 31. Far more sample-efficient than the probe predicted.

### Chance correction COSTS sample efficiency in the easy regime — `g32-02`

    CONFIG  when    2026-07-31
            source  experiments/sweeps/g32-02-how-many-occasions-does-a-concept-need.txt
            script  experiments/g32_02_how_many_occasions_does_a_concept_need.py
            task    occasions, uniform, no distractor
            model   none
            knobs   statistic, stream length; per-concept scoring
            scale   pooled over seven lengths

Per concept, uniform: `count` **0.6248** against `ppmi` **0.5439** at 2-3 occasions,
**0.8322** against **0.7332** at 4-7, **0.9714** against **0.9503** at 8-15.

PMI is a ratio of two estimates and is higher-variance where counts are small. At a single
occasion `ppmi` scores **0.3991**, below the 0.5 floor — it groups wrongly rather than
failing to group.

### Skew is not only starvation, and a common concept IS a distractor — `g32-02`

    CONFIG  when    2026-07-31
            source  experiments/sweeps/g32-02-how-many-occasions-does-a-concept-need.txt
            script  experiments/g32_02_how_many_occasions_does_a_concept_need.py
            task    occasions, 64 concepts, no distractor, zipf 1.0 and 2.0
            model   none
            knobs   per-concept scoring bucketed by that concept's subject count
            scale   8,000 occasions

At matched subject count, `count` scores **0.5056** on skewed concepts seen 16-31 times
against **0.9984** on uniform concepts seen as often. `ppmi` is untouched at zipf 1.0 —
**1.0000** in every bucket — and carries a mid-range penalty at zipf 2.0 that closes by
32-64 occasions.

Probed directly rather than inferred: **60 of 60** surfaces of the twenty rarest concepts
have a surface of a *different* concept as their best raw-count partner. Concept 45 was the
subject **0** times in 8,000 occasions, its surface 135 was present on **129** of them
entirely as noise, and its strongest partners are three surfaces of concept 0 — the
subject of 4,992 occasions — met **62**, **57** and **57** times against its own two
partners at **1** each.

So the designed distractor and the frequency tail are the same failure. What defeats raw
counting is anything merely common, however it got that way.

**The bucket comparison confounds subject count with stream length** — uniform low-count
concepts come from short streams and so meet less noise. The direct probe is what carries
the conclusion. The clean control is one rare concept in an otherwise uniform world at
fixed stream length, and it has not been run.
