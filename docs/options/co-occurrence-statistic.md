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
- `openplexus/federated.py`: `Federation` splits those tables by `owner(surface)` using the
  same `Ring` the join uses, counts every crossing, and **refuses `occasions`** so `ppmi`
  cannot be computed by a node rather than being quietly approximated.
- `tests/test_grounding.py`, `tests/test_occasions.py`, `tests/test_federated.py`, and nine
  mutations in `tools/mutate.py`.
- **`grounding.py` itself still has no distribution in it** and says so in its own
  docstring — the split lives in `federated.py`, so the single-table path stays available
  as the reference every federated answer is checked against.
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

### The read path costs one peer message PER PARTNER, and that is the bill — `openplexus/federated.py`

    CONFIG  when    2026-07-31
            source  experiments/sweeps/g33-03-what-the-read-path-costs.txt
            script  experiments/g33_03_what_the_read_path_costs.py
            task    occasions, 3 surfaces, presence 0.7, noise 3, 1 distractor
            model   Federation, 8 nodes, conditional, k 2, 4,000 occasions
            knobs   concepts 16 / 32 / 64
            scale   48, 96 and 192 surfaces

**Labelled as unpredicted, and the sweep record says so first.** These were taken
at a terminal while `federated.py` was being written; `g33-03` is a REPRODUCTION
of them, written because `check_provenance` refused a record citing figures that
lived nowhere. Weaker evidence than `g32-01` or `g33-01`, one seed, no error bars.

Remote reads for one walk, after memoising each surface's ranking within a walk:

    surfaces    mean fan-out    remote reads per walk    ratio
          48            48.0                    124.8      2.6
          96            96.0                    249.6      2.6
         192           168.4                    439.2      2.6

**The cost is a constant times the FAN-OUT** — how many distinct partners a
surface has ever been seen beside — and in a stream with noise and a distractor
that is close to the whole vocabulary. It is not a constant times `k`.

Memoising is worth **3x** and changes no answer: the naive walk re-ranks a
surface once per edge touching it, which measured at about eight times fan-out
before and 2.6 after. A node ranking one surface twice inside a single query has
asked its peers the same question twice.

**What this does not settle.** Whether a cheap local prefilter — rank by raw
count, which needs no peer, then pay for the top `m` only — preserves the answer.
`g32-02` is the reason to doubt it: under frequency skew, **60 of 60** surfaces
of the rarest concepts had a different concept's surface as their best raw-count
partner, so the filter would discard the true partner before the exact statistic
ever saw it. Untried.

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

### It solves 26 of 29 PUBLISHED conditions, and fails where the answer is absent — `g34-01`

    CONFIG  when    2026-07-31
            source  experiments/sweeps/g34-01-external-word-learning-trials.txt
            script  experiments/g34_01_external_word_learning_trials.py
            task    xsl.py -- 29 published cross-situational word-learning conditions
            model   mutual top-k over counts; no join, no federation
            knobs   arm in {count, conditional}, bound in {1, derived}
            scale   12-18 word-object pairs, 18-81 trials per condition

**The first grounding measurement on stimuli this project did not design.**
Ground truth comes from the files, not from humans — no published accuracy is
reachable without an RData reader, so this is external *stimuli* and not an
external *benchmark*.

The derived bound recovers **26 of 29** conditions exactly, mean **0.9569**
against a floor of 0.6667 for a two-surface concept. Fixed bound 1 reaches
**0.9007**.

**The three failures are `filt0E_3L`, `filt0E_6L` and `filt0E_9L`, all at
0.5833 with a largest ranking gap of exactly 0.000.** In that condition a pair is
only ever presented alongside one other pair, so a word co-occurs with two objects
on identically the same trials and `conditional` returns exactly **1.0** for both.
Tripling the trials — 18, 36, 54 — returns the same 0.5833, so **the information
is absent rather than scarce**, and no function of co-occurrence counts can reach
it.

**And nothing else does either**, which corrects this entry's first version. The
four surfaces are a closed, fully symmetric clique — every pairwise `conditional`
inside it is exactly **1.000** and nothing outside it ever appears with any of
them — so two assignments are consistent with every observation and a
one-word-one-object constraint keeps both. *Mutual exclusivity* was named here as
the missing ingredient and it is not one: it halves a hypothesis space of two to
a hypothesis space of two. The condition is a designed control proving
co-occurrence has a ceiling, and **it motivates building nothing.**

**Raw counting survives the frequency variation an experimenter actually uses.**
Every `freq369` condition — pairs shown 3, 6 and 9 times — scores 1.0000 under
`count`. That narrows `g32-02`'s skew finding to the skew it was measured at,
`zipf` 2.0, where the commonest concept took 4,992 occasions and the rarest zero.

### The damping exponent has no useful interior — `g36-06`

    CONFIG  when    2026-07-31
            source  experiments/sweeps/g36-06-can-a-softer-denominator-save-the-word.txt
            script  experiments/g36_06_can_a_softer_denominator_save_the_word.py
            task    MNIST 4,000 images + FSDD 3,000 recordings + 10 words,
                    3,000 occasions, noise 2, 1 distractor present EVERY occasion
            model   `grounding.damped(alpha)`, new and off by default
            knobs   alpha 0/0.25/0.5/0.75/1.0 x arms together/alternating;
                    50 codes pinned from g36-04; 3 seeds
            scale   word survival, distractor admission, link purity, class size

**John asked whether the denominator could be softened so connections matter more
than frequency.** The diagnosis behind the question is right and measured: a word
is present **845.4** times against **60.0** for any single code, so `conditional`
handicaps it fourteen-fold for being shared across fewer types.

`damped(alpha)` exposes `c_xy / c_y**alpha` as one axis; alpha 0, 0.5 and 1
reproduce `count`, `weighted` and `conditional` exactly.

**The fix does not work.** Softening restores the word's presence in a neighbour
list — 0.0200 to 0.9400 — while end-to-end linking goes to **0.0000**. Alpha 1.0
is the best value in both arms (0.6667 and 0.8476).

**The low end fails by FRAGMENTING, not by merging**, which is the opposite of
the registered prediction. Mean class size at alpha 0.0 is **1.99** against 4.26
and 8.89 at alpha 1.0. Every surface ranks the ever-present distractor first, the
bound is spent on it, and almost no pair is mutual.

**So `word` and `link_img` are not two views of one quantity.** A word can be in
an image code's list while no image code is in any word's list, because a hub can
be mutual with only as many spokes as its OWN bound admits.

**And the volume asymmetry is necessary but not sufficient.** In `alternating`
the word survives at every alpha including 1.0 — 0.8389 to 0.9865 — and a word is
just as common there. What `together` adds is a rival that is well-correlated
AND rare.

**Kept as a switched-off alternative** per rule 14c, with the revival condition
named: a stream with no ever-present distractor, where the trade might have an
interior. Every number here is measured with one, because that is `g32-01`'s
falsifier condition.

### `count` and `local_conditional` are ONE arm — 2026-07-31

    CONFIG  when    2026-07-31
            source  experiments/sweeps/g36-06-can-a-softer-denominator-save-the-word.txt
            script  experiments/g36_06_can_a_softer_denominator_save_the_word.py
            task    as above
            model   the five named statistics
            knobs   none -- arithmetic, confirmed on the three-modality stream
            scale   full neighbour ranking, 20 surfaces

For a fixed surface `x`, `local_conditional` divides every candidate's score by
`count(x)`, which is a **constant**. Dividing a list by a constant preserves its
order exactly, so it induces the identical ranking to `raw_count`. Confirmed
directly: identical full orderings on every surface checked.

**With `g32-01`'s finding that `ppmi` and `conditional` agree above chance, the
five named statistics are THREE distinct rankings**, not five. Any grid sweeping
all five is reporting two of them twice.

This is the third instance of the identical-arms failure in this line, and the
check remains arithmetic on the arms before dispatch rather than a run.

### Breadth is WORSE than frequency at spotting the distractor — `g38-02`

    CONFIG  when    2026-07-31
            source  experiments/sweeps/g38-02-does-breadth-separate-the-distractor.txt
            script  experiments/g38_02_does_breadth_separate_the_distractor.py
            task    MNIST + FSDD + 10 words, 3,000 occasions, noise 2 drawn
                    UNIFORMLY, 1 distractor present every occasion
            model   none -- a property of the co-occurrence table
            knobs   arms together/alternating; 50 codes pinned; 3 seeds
            scale   distractor / word ratio per column

**A proposal refuted before it was built, which is the entry's whole value.**
`g38-01` found three scalar dials all failing at opposite ends, so the next idea
was deliberately not a fourth: a distractor should be distinguishable from a word
**without consulting any edge**, from a surface's own row and therefore free of
any remote read. A word is common but focused; a distractor is common and
indiscriminate.

**Measured, the separation is 1.24x by breadth against 3.56x by plain
frequency** — the proposed quantity is about three times worse at the one job it
was proposed for. `exp(entropy)` of the partner counts was the right refinement
of the idea; raw partner count separates by **1.01x**, which is nothing.

**The cause is identified rather than guessed.** A word's partner count is
**109.2** of a possible 110 — it has met essentially every surface — because
`NOISE` draws two other words UNIFORMLY per occasion. Over 3,000 occasions every
word appears beside every digit's codes many times, so the word is not focused in
this data.

**Scope, stated precisely: refuted ON THE DATA THE FAILURE WAS MEASURED ON.**
Real speech is not uniform over scenes, and a realistic stream would concentrate
a word's partners far more. That is enough to stop the build — a mechanism cannot
be justified by a stream nobody has run — and not enough to call the idea wrong
in general.

**And it found an instrument defect worth more than the proposal.** `zipf` makes
surface FREQUENCIES uneven; nothing makes *which surfaces co-occur* uneven. Every
statistic measured on this generator is therefore answering a slightly wrong
question. Registered, not fixed — changing the generator invalidates the
comparison set, so it is a decision rather than a cleanup.

### THE CURVE HAS NOT FLATTENED — every absolute figure is a lower bound — `g39-01`

    CONFIG  when    2026-07-31
            source  experiments/sweeps/g39-01-what-does-the-learning-curve-look-like.txt
            script  experiments/g39_01_what_does_the_learning_curve_look_like.py
            task    MNIST + FSDD + 10 words, ONE pass, scored at 8 checkpoints,
                    no split and nothing replayed
            model   grounding.reach, conditional, beam 16, depth 1, mean
            knobs   occasions 25..3000; arms together/alternating; 3 seeds
            scale   link precision with a coverage companion; chance 0.1000

**The first prequential measurement in the grounding line**, which `GOALS.md` §3
has required since it was written and recorded as *"still the exception rather
than the norm"*.

**`link` goes 0.5643 at 1,500 occasions to 0.9665 at 3,000 — a rise of 0.4022
over the last doubling**, against a registered refutation threshold of 0.05. The
curve is steepest where the data ends.

**So stream length is `CLAUDE.md`'s constant-that-looks-like-background**, held at
3,000 across g32 through g38 without ever being varied. Arm-vs-arm comparisons at
equal length survive. **Absolute figures become lower bounds**, and every *"X does
not help"* becomes a claim that might only be true at this length — which is the
most expensive error class this project names.

**And the shape inverts a prediction usefully.** Coverage arrives almost at once
— **0.6733** at 25 occasions, **0.8867** at 100 — while precision sits at chance
until about 400. **The mechanism finds nearly everything immediately and spends
the whole stream sorting it**, so the work is discrimination rather than
discovery, and a recall-shaped metric would have looked good from occasion 25.

**`g38-01`'s advantage is a large-data effect.** Both mechanisms sit within 0.05
of chance until 400 occasions and the partition is briefly ahead at two
checkpoints. The walk's lead arrives late and is then decisive.

**`distractor` is 1.0000 at every checkpoint from 25 onward.** Experience does
not fix it either — five axes now, none of which separates the link from the
distractor.

**The cheap follow-up is not done:** 3,000 occasions is one pass over FSDD, and
cycling the stream is both trivially available and what a system that learns
forever would do. **Nothing establishes where the curve flattens.**

### The HARD CUT refused the distractor, not the statistic — `g39-03`

    CONFIG  when    2026-07-31
            source  experiments/sweeps/g39-03-is-the-distractor-failure-structural.txt
            script  experiments/g39_03_is_the_distractor_failure_structural.py
            task    MNIST + FSDD + 10 words, arm together, 1 distractor present
                    every occasion, stream extended by re-pairing
            model   grounding.reach, damped(alpha), beam 16, depth 1
            knobs   alpha 0.5/0.75/1.0 x combiner min/geometric/mean/max, at
                    3,000 and 24,000 occasions; 50 codes; 3 seeds
            scale   link, coverage, distractor admission; chance 0.1000

**The re-check `g39-01` obliged, at the length `g39-02` showed is converged, on
the one failure nothing had fixed. 0 of 24 settings keep the link and refuse the
distractor; `distractor` is 1.0000 in every cell.**

**And it names what was doing the refusing.** `g36-06` measured the distractor
falling to 0.0000 as the exponent rose. Under `reach` the exponent moves it **not
at all**. The difference is the mechanism: that run scored admission through
`equivalence_classes`, which applies a derived cut and then requires mutuality;
this one applies neither.

**So the exponent never refused the distractor — the hard cut did.** An
ever-present surface scores low under `conditional` and falls below the cliff,
and it fails mutuality because it is in everyone's list and nobody is in its.
Remove the cut and the ranking still contains it, further down, where a walk
reading the whole ranking finds it.

**One line for the whole trade: a hard cut refuses the distractor and evicts the
word; a ranked walk keeps the word and admits the distractor.** Five axes failed
because none of them was the axis. **Stop looking for a statistic that refuses
it** — the refusal has to come from a structural constraint.

**The rail held exactly**: `mean` at alpha 1.0 and 3,000 occasions reproduces
`g38-03`'s cell to four decimals, 0.9665 at 0.9933.

**Alpha and combiner move COVERAGE, not precision.** Link is 0.9664 to 1.0000 in
all 24 cells while coverage ranges 0.5933 to 1.0000 — invisible in either earlier
sweep, and it means those axes decide how much is found rather than how right it
is. The best cells are alpha 1.0 with `mean` or `max`: **0.9867 at 1.0000**.

**Alpha 0 and 0.25 were excluded on `g36-06`'s evidence**, which was taken under
the other mechanism — and since the two mechanisms disagree about exactly this
column, that exclusion is now weaker than when written.

### Partial presence CANCELS; correlation is the real boundary — `g39-06`

    CONFIG  when    2026-07-31
            source  experiments/sweeps/g39-06-what-about-a-thing-present-almost-always.txt
            script  experiments/g39_06_what_about_a_thing_present_almost_always.py
            task    MNIST + FSDD + 10 words, 12,000 occasions, one distractor at
                    varying presence, plus a correlated confound arm
            model   conditional read FORWARD from the word; no cut, no mutuality
            knobs   presence 0.5/0.7/0.9/0.95/1.0; correlated 100% on digit 3
                    and 10% elsewhere; 50 codes; 3 seeds
            scale   rank, score gap and admission; g39-05's columns

**`g39-05`'s caveat is answered and dissolves.** A distractor present
independently with probability `p` contributes `p` to BOTH terms of
`count(x,y)/count(y)`, so it cancels. Measured: the score is **0.2805 to
0.2808** across presences from 1.0 down to 0.5, and the margin **0.4488 to
0.4491**. A lamp in most rooms is refused exactly as a lamp in all rooms is.

**Since `g39-04`'s account of why `forward` works is the same cancellation read
from the other side, this is evidence for the ACCOUNT and not only the result.**

**The real boundary is CORRELATION and it is far closer than any earlier number
suggested.** A confound present on every occasion of one digit and 10% elsewhere
is still refused — rank 7.7 against a want of 4.7 — but the margin collapses
from **0.4490 to 0.0096**, a factor of 47. **Refused by a hair, not by a
margin.**

The arithmetic says why it survives: that confound is about 63% specific to its
concept while a true image code is essentially 100% specific. **A confound must
be MORE concentrated on a concept than the concept's own surfaces before it
displaces them.** A stronger one would cross, and nothing locates where.

**No statistic over co-occurrence can fix that**, and it is not a defect of
`forward`: a surface genuinely more common around one concept IS evidence about
it, and the data contains nothing distinguishing spurious correlation from real
membership. `g32-01` names intervention as the only escape; it remains untested.

**A PROCESS NOTE THAT COST A WRONG ROW.** The correlated arm was first reported
pooled over ten words, where it reads margin **0.4494** and looks fine — the
correlation touches one digit and nine irrelevant words diluted it. `g39-05`'s
own caveats say exactly this one run earlier: *"a single bad word could sit far
lower without moving it."* Caught by re-reading that section, not by any check.
