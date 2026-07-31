# Option record — bound the enumeration by the biggest similarity gap

> **RECORD ONLY. This file carries no status.** Chosen, refused, untried or live-both lives
> in [DECISIONS.md](../../DECISIONS.md) alone. Here there are only events, and events do not
> un-happen, so nothing here can go stale. **Absence means untried.**
> Format and the CONFIG block: [README.md](README.md).

---

## What exists

- The rule as measured in `167` and `note 056`: an argmax over consecutive similarity gaps,
  not a threshold — the same move decision 148 made when it replaced a tuned bar with a
  structurally-zero read.
- `LocalMemoryConfig.index_branches` and `index_sharpness` are the knobs it sits behind.

---

## What was tried, and what came back

### It matches the best fixed count without being told the size — `167`

    CONFIG  when    2026-07-29
            source  decision 167, and notes 056 and 058 for the cliff's width
            script  unrecorded
            task    families, index purity 1.000
            model   argmax over consecutive similarity gaps
            knobs   look ahead 4, 6 and 16; family sizes 3 to 6
            scale   cliff about 0.45 wide against within-family steps of ~0.01

Matches the best fixed `branches` at family sizes 3–6 **without being told the size**, where
no single fixed value works across all of them.

**`look` is a CEILING, not a target.** Flat from 6 to 16, but **0.500 at look=4 for a family
of 6** — so it must exceed the group, and setting it too low is the one way to break it.

The cliff it exploits is ~0.45 wide against within-family steps of about 0.01, which is what
makes an argmax over gaps well-posed here at all.

### Degrade the grouping and it falls FASTER than a fixed count — `note 056`

    CONFIG  when    2026-07-29
            source  note 056
            script  unrecorded
            task    families, index purity degraded
            model   gap rule against fixed branches
            knobs   purity 0.795, 0.951 and >= 0.99
            scale   unrecorded

    purity   gap rule   fixed
     0.795      0.167   0.417
     0.951      0.750   1.000
    >=0.99      level    level

**Why:** given the count, a noisy ranking can only hand you wrong *candidates*. Deriving the
count, it hands you wrong candidates **and** a wrong count — two error sources against one.
The tell is over-emission: size 2.58 against a true 2.00, precision 0.708.

So this is a measured **crossover**, not a loser, and which one is right is a property of the
grouping's quality rather than of either mechanism. Record for the other side:
[fixed-branches.md](fixed-branches.md).

### Real language provides a slope where the rule needs a cliff — `note 058`

    CONFIG  when    2026-07-29
            source  note 058
            script  unrecorded
            task    real word co-occurrence against the families task
            model   similarity profile over a content-word slice
            knobs   weighting off, content-word slice, centring confirmed, shuffled control
            scale   four confounds closed; shuffled control at 0.002

    largest gap, real co-occurrence   0.059
    largest gap, the task             0.424

**At no setting is the profile bimodal.** Language decays in steps of 0.02–0.03 where the
task falls 0.45 at once. So the crossover needs purity ≳0.99 **and** bimodality, and one
real dataset supplies neither.

**The shape is the finding, not the number** — a cliff rule needs a cliff.

### It resolves a hub-and-spoke tension a fixed count CANNOT express — `g33-04`

    CONFIG  when    2026-07-31
            source  experiments/sweeps/g33-04-does-a-derived-bound-fix-the-star.txt
            script  experiments/g33_04_does_a_derived_bound_fix_the_star.py
            task    occasions, 64 concepts, 8,000 occasions, pairings complete/chain/star
            model   grounding.cliff over conditional scores; look 16; no join
            knobs   bound in {2, 3, derived}, surfaces 3-5; 3 seeds
            scale   uniform frequency, 1 distractor

**A second, independent argument for this option from a different direction.**
`g33-02` found a single global bound cannot express a star: the hub needs a bound
at least its own degree while a spoke needs one, and no value is both.

Derived, on `star` at five surfaces: bridged **0.8429** and f1 **0.9202**, against
a fixed 2's **0.1667** and **0.4847**, and a fixed 3's collapse to a largest class
of **0.8962** of all surfaces. Largest for the derived arm never exceeds
**0.0208** in any of nine cells, so **the rule is self-limiting** where a fixed
bound large enough for the hub is not.

On `complete` it matches the best fixed bound at 3 and 4 surfaces and reaches
**1.0000** at 5 where the best fixed value in that grid reaches 0.9236 — the
decision-167 property reproducing on a different world and a different statistic.

**It loses on chains and the cause is stated:** a mid-chain surface's two true
neighbours score asymmetrically, because the rarer one scores higher, so the
biggest gap falls between them rather than after them. Bridged degrades
**0.9792, 0.7344, 0.6215** with length against a fixed 2's flat 1.0000 — while f1
is HIGHER at every length, because the fixed bound over-links a leaf. A trade,
not a loss.

### The rule's answer on an even slope is decided by FLOATING POINT — `test_grounding`

    CONFIG  when    2026-07-31
            source  tests/test_grounding.py, TheCliff
            script  none -- a property of the arithmetic, asserted in a test
            task    n/a
            model   grounding.cliff
            knobs   none
            scale   n/a

`[0.5, 0.4, 0.3, 0.2, 0.1]` returns 2 and `[5.0, 4.0, 3.0, 2.0, 1.0]` returns 1 —
the same ranking with the same gaps, because `0.5 - 0.4` and `0.4 - 0.3` differ in
binary and an argmax over gaps has nothing else to separate them.

Note 058 measured real co-occurrence as exactly that shape. **So on slope-shaped
data this rule does not merely degrade: its output is determined by representation
noise**, and a result taken from it there would be unreproducible for a reason no
seed controls. Asserted rather than commented so the caution cannot be read as
theoretical.

### The ranking IS bimodal on stimuli we did not design — `g34-01`

    CONFIG  when    2026-07-31
            source  experiments/sweeps/g34-01-external-word-learning-trials.txt
            script  experiments/g34_01_external_word_learning_trials.py
            task    xsl.py -- 29 published cross-situational word-learning conditions
            model   grounding.cliff over conditional scores; look 16
            knobs   bound in {1, derived}
            scale   12-18 pairs, 18-81 trials per condition

**The question `g33-04` left open.** Mean largest gap across the 26 solvable
conditions is about **0.5** — the same order as the designed families task's
0.424, and an order of magnitude above note 058's **0.059** for real language.

So the cliff this rule needs exists outside a world this project built, and the
derived bound beats a fixed one there: **0.9569** mean f1 against **0.9007**, and
26 of 29 conditions exact against 14.

**The `filt3E`–`filt9E` family is where it earns that**, and the mechanism is the
one `g33-04` named on chains. Those conditions cut at **2.0** rather than the
true 1, and score **1.0000** where a fixed bound of 1 scores **0.8333** — the
second candidate enters the ranking and is then removed by mutuality, while a
true partner whose score is asymmetric gets in where a cut of 1 excluded it.

**And the caveat that survives.** These are laboratory experiments designed to be
learnable from co-occurrence. Note 058's slope remains the only measurement on
natural language, and nothing here bridges the two.

### It is what makes MORE MODALITIES safe rather than harmful — `g36-02`

    CONFIG  when    2026-08-01
            source  experiments/sweeps/g36-02-do-more-modalities-learn-faster.txt
            script  experiments/g36_02_do_more_modalities_learn_faster.py
            task    occasions, 64 concepts, presence 0.7, noise 3, 1 distractor
            model   conditional; bound fixed 2 or derived; no join
            knobs   surfaces 2/3/5/8, occasions per concept 2..64; 3 seeds
            scale   floor-free `connected`, bar 0.95

**A third independent argument for this option**, after `g33-02`'s hub and
`g33-04`'s grid.

Under a FIXED bound of 2, adding modalities **caps** what can ever be learned: 5
surfaces plateaus at **0.7531** and 8 at **0.5781**, and neither improves from 16
occasions per concept to 64 — thirty-two times the data buys nothing. 2 and 3
surfaces reach **1.0000** on the same axis.

Under the derived bound every surface count reaches 1.0000, and more modalities
is better at every stream length: **0.3542, 0.4201, 0.4969, 0.5677** at the
sparsest column.

**So the failure is a ceiling rather than a delay**, and experience does not
repair it. The derived bound is what makes multimodality — which `GOALS.md`
requires — compatible with the mechanism at all.

**And the two bounds do NOT cross on this axis**: derived is equal or better in
all 24 cells. `g33-04`'s measured crossover is on the PAIRING STRUCTURE axis
(chains), so the reason both arms are kept is that one and not this one.

### The bound is a BUDGET, not a threshold, and it evicts — `g36-04`, `g36-05`

    CONFIG  when    2026-07-31
            source  experiments/sweeps/g36-05-what-the-bound-evicts.txt
            script  experiments/g36_05_what_the_bound_evicts.py
            task    MNIST 4,000 images + FSDD 3,000 recordings + 10 words,
                    3,000 occasions, noise 2, 1 distractor
            model   conditional; bound derived; no join
            knobs   50 codes, arms image+word / together / alternating; 3 seeds
            scale   share of image codes whose OWN word survives the bound

The first measurement of what the derived rule DROPS rather than what it keeps,
and it is the mechanism behind `g36-04`'s headline.

When a picture and a sound share **every** occasion, they become each other's
strongest partner and the word is evicted: it survives the bound for **0.0200**
of image codes against **0.9867** when the picture is alone and **0.9797** when
the two senses alternate. Its mean rank falls 1.04 -> **6.70**. Of the 2.05
partners kept, **2.03 are audio codes**.

`mean bound` moves 1.09 -> 2.05, not 1.09 -> 6.7. **The cliff substitutes rather
than widens** — it keeps roughly one partner per modality present, which is the
derived rule behaving exactly as designed and is why the effect is an eviction
rather than a dilution.

**This is a design decision and not only a finding: do NOT build spoke-to-spoke
linking as a fix for the hub problem.** `together` is that arrangement reached
from the data side, and `g36-04` prices it end to end — the bridged route beats
the direct one in all three code counts, 0.9580 against 0.8853 at 50 codes. The
walk already bridges two senses that never co-occur.

**Untested and it is the interesting part:** `together` is 100% simultaneity and
`alternating` is 0%. Nothing measures 10% or 30%, and reality is in there.

### The alternative that removes the ceiling entirely — John, 2026-07-31

    CONFIG  when    2026-07-31
            source  experiments/sweeps/g36-05-what-the-bound-evicts.txt
            script  experiments/g36_05_what_the_bound_evicts.py
            task    MNIST images + FSDD spoken digits + 10 words
            model   conditional; bound derived, `look` ceiling 16
            knobs   50 codes, arms image+word / together / alternating; 3 seeds
            scale   share of image codes whose OWN word survives the bound

**John's instruction, 2026-07-31: *"I don't think we want a ceiling at all."***
His reasoning is that a concept IS a web of connections, so traversal is
intrinsic rather than a cost to be minimised away — and that latency is an
optimisation problem to be solved after the capability is proved, not before.

**`grounding.reach` is that, and the distinction it turns on is WHERE THE BUDGET
SITS.** `neighbours` and `equivalence_classes` bound the REPRESENTATION: each
surface keeps a few partners and the rest are discarded before any question is
asked. `reach` bounds the SEARCH: every edge stays and `beam`/`depth` limit how
far one question travels. An unbounded representation with a bounded search is
affordable; an unbounded search over it is `O(N**depth)` and is not.

**Soft mutuality costs NOTHING extra in messages, which was not obvious.**
`strength` evaluates both directions, and both need only `count(x,y)`,
`count(x)` and `count(y)`. `owner(x)` holds the first two, so it is still **one
remote read**, exactly as `conditional` alone.

**THE COMBINER IS A SWEPT PARAMETER AND NOT A CHOICE MADE BY ARGUMENT.** A first
version justified `min` by claiming a mean would rank an ever-present distractor
above a real partner; **a test refuted that on its first run** — the mean puts
them in the correct order. The figures live in
`tests/test_reach.py::test_a_MEAN_also_ranks_them_correctly_here`, which is the
canonical home for them because it is the copy under continuous execution.
And thinking it through the other way, `min`
takes the WEAKER direction, which on a hub edge is the small one: a word's edge
to an image code is near 1.0 from the word's side and small from the code's. So
`min` weakens exactly the edges `g36-05` found being evicted at **0.0200**.
`COMBINERS` holds min/geometric/mean/max and the doubt is recorded at the
definition rather than left to be discovered.

**WHAT SWITCHING WOULD INVALIDATE, enumerated before anything is built on top.**
Not at risk: C1 itself — a deep traversal is many bounded one-hop messages, and
what John relaxed is latency, not the constraint. Also safe: the statistic
findings and the container/join results. **Conditional:** every number scored
through `equivalence_classes`, which is g32-02's threshold, g34-01, g35-02 and
all of g36. **Superseded if the bound goes:** `g33-02` and `g33-04`, which are
about the bounded representation. **Actively worse:** `g33-03`'s read cost.

**Both mechanisms are kept**, per rule 14c, so the default is unchanged and no
earlier number moves.

**And a tripwire is LOST, which is the risk least likely to be noticed.** A
partition has mean class size as a collapse alarm. A ranking cannot collapse,
which sounds like an improvement and means a recall-shaped metric will read well
here for reasons unrelated to the mechanism working. New scorers need their own
floor.

### The walk WINS the link and LOSES the distractor — `g38-01`

    CONFIG  when    2026-07-31
            source  experiments/sweeps/g38-01-does-the-weighted-walk-beat-the-partition.txt
            script  experiments/g38_01_does_the_weighted_walk_beat_the_partition.py
            task    MNIST + FSDD + 10 words, 3,000 occasions, 1 distractor
                    present every occasion
            model   `grounding.reach`, conditional, beam 8, depth 2
            knobs   combiner min/geometric/mean/max; arms together/alternating;
                    50 codes pinned from g36-04; 3 seeds
            scale   link@k against a shuffled floor at chance 0.1000

**The first measurement of bounding the SEARCH instead of the REPRESENTATION.**

`mean` scores **0.9589** on the cell where the bound failed, against the
incumbent's **0.6667** — and **0.9829** against **0.9220** on the cell where it
had not. It wins both, and no stored count changed: the word was never missing,
only past a bound that keeps two partners while it sat at rank 6.70.

**It is not adoptable, because `mean` admits the ever-present distractor for
every word — 1.0000 against the incumbent's 0.0000.** That is `g32-01`'s
falsifier firing on the new mechanism, and trading it back would undo a measured
result to buy an unmeasured one.

**`min` reaches nothing at all** (`reached` 0.00). Its weaker direction for a
true partner is about 0.07 while the distractor's is about 0.28, so the top of
every list is distractor and noise. The doubt registered at `strength`'s
definition was right; the magnitude attached to it was a guess and was wrong by
an order of magnitude.

**`max`'s clean distractor column is an ARTEFACT of tie-breaking**, not a
property: everything saturates near 1.0 and ties break by ascending surface id,
which image codes win by holding lower numbers. The tell is `link@k` at
**0.1000**, exactly chance and exactly the shuffled floor.

**THIRD DIAL OF THIS SHAPE.** `damped`'s exponent had the word at one end and the
distractor at the other with nothing between (`g36-06`); the combiner repeats it
exactly. Three scalar knobs, three two-ended failures — **evidence the conflict
is structural rather than untuned, and the next proposal should not be a fourth
knob.**

**Where the arithmetic points.** The distractor's damage enters through its
backward direction being exactly 1.0, which no combiner can weigh away because it
is TRUE. It is a property of the surface, so a rule reading a surface's own
BREADTH — how many different things it is present with — would refuse it without
consulting any edge. Untried, and not a knob.

### Depth 1 is enough, and a wide beam there is FREE — `g38-03`

    CONFIG  when    2026-07-31
            source  experiments/sweeps/g38-03-how-deep-may-a-query-walk.txt
            script  experiments/g38_03_how_deep_may_a_query_walk.py
            task    MNIST + FSDD + 10 words, arm `together`, 1 distractor
            model   `grounding.reach`, conditional, combiner `mean`
            knobs   beam 2/4/8/16/32 x depth 1/2/3; 50 codes pinned; 3 seeds
            scale   link@k with a coverage companion; messages counted

**John's question — how deep may a query walk before it costs too much — and the
axis `g38-01` pinned without sweeping.**

**Depth 1, beam 16: link@k 0.9665 at 0.9933 coverage for 109 messages.** Depth 2
at the same beam costs **809** messages for a lower **0.9200**; depth 3 costs
**3117** for the same. Deeper is worse AND dearer.

**And at depth 1 the cost is FLAT in beam** — 109 messages whether the beam is 2
or 32 — because scoring a surface's partners is the cost and expanding them is
not, and at depth 1 nothing is expanded. So the best setting is also the cheapest
available. For comparison, `g33-03` measured 439 messages per walk at 192
surfaces on the bounded mechanism.

**It also converts `g38-01`'s comparison into a starker one.** The incumbent
partition on the same cell and seeds is **0.6667 at 0.0533 coverage** — it finds
one image code in twenty and is right about two thirds of that one. The walk
finds essentially all of them.

**`distractor` is 1.0000 in all fifteen cells**, so the search budget is a fourth
axis that cannot separate the link from the distractor. Three scalar dials and a
budget, all failing the same way.

**Two things this does NOT settle.** The two-hop query — an image code reaching
an audio code through a shared word, which is what motivated depth 2 — was not
run; only word-to-image, which co-occurs directly and needs one hop by
construction. And **predictions were not committed before this run**, which every
other sweep this session did; the record says so at the top.

### The curve flattens at 12,000, and the walk's lead is a QUARTER of the reported one — `g39-02`

    CONFIG  when    2026-07-31
            source  experiments/sweeps/g39-02-where-does-the-curve-flatten.txt
            script  experiments/g39_02_where_does_the_curve_flatten.py
            task    MNIST + FSDD + 10 words, arm `together`, stream extended by
                    RE-PAIRING to 24,000 occasions, 1 distractor
            model   grounding.reach, conditional, beam 16, depth 1, mean
            knobs   occasions 3,000/6,000/12,000/24,000; 50 codes; 3 seeds
            scale   link precision with coverage companion; chance 0.1000

**The follow-up `g39-01` demanded, and it corrects `g38-01` rather than
confirming it.**

    occasions      walk    partition     gap
         3,000   0.9665       0.6667   0.2998
        24,000   0.9867       0.9216   0.0651

**The walk gains 0.0202 from eight times the data; the partition gains 0.2549.**
So the incumbent was far more penalised by the short stream, and `g38-01`'s
headline gap is about a quarter of what it reported. The walk still wins; it wins
by 0.065 rather than 0.30.

**The curve is flat from 12,000** — `link` moves 0.0000 over the final doubling —
so 1,200 occasions per digit is the plateau and absolute figures at 3,000
understate the walk by only 0.02.

**Repeating the stream would have been a no-op and the design says why.**
`conditional` is a ratio, so an exact repeat multiplies both terms and changes
nothing. The extension re-pairs each recording with a different image and redraws
the noise, which is genuinely new co-occurrence from the same underlying data.

**The partition improves NON-MONOTONICALLY** — 0.6667, 0.5185, 0.8833, 0.9216 —
because a hard partition is discrete and a small score change flips whole
components. The walk's ranking rises smoothly. **That is an argument for the walk
no cell of `g38-01` could have shown.**

**And `distractor` is 1.0000 at 24,000 occasions.** Experience does not repair it
either. Five axes now — three scalar dials, the search budget, and stream length
— and none separates the link from the distractor. That is enough to stop looking
for a sixth knob and call it structural.

### SOLVED, by removing things: `forward` at alpha 1.0 — `g39-04`

    CONFIG  when    2026-07-31
            source  experiments/sweeps/g39-04-does-the-forward-score-refuse-the-distractor.txt
            script  experiments/g39_04_does_the_forward_score_refuse_the_distractor.py
            task    MNIST + FSDD + 10 words, 1 distractor present every
                    occasion, stream extended by re-pairing to 24,000
            model   grounding.reach, damped(alpha), combiner `forward`, beam 16
            knobs   arms together/alternating x occasions 3,000/24,000 x alpha
                    0.5/0.75/1.0 x depth 1/2; 50 codes; 3 seeds
            scale   link, coverage, distractor admission; chance 0.1000

**The week's open problem, closed.** **8 of 24** settings keep the link and refuse
the distractor, against `g39-03`'s **0 of 24** for every symmetrising combiner —
and the eight are exactly the eight at `alpha = 1.0`, in both arms, at both
stream lengths, at both depths.

At 24,000 occasions on `together`: link **0.9867**, coverage **1.0000**,
distractor **0.0000**. The incumbent partition on the same cell is **0.9216**.

**`forward` at alpha 1.0 is plain `conditional` read from the query's own side.**
So the working rule is: keep every edge, drop the cut, drop mutuality, drop
symmetrisation — rank and read. **A simplification, not a mechanism.**

**Symmetrising was what admitted the distractor.** From a word's side its own
codes score ~1.00 and the distractor ~0.28; from the distractor's side everything
scores 1.00, because it genuinely is always present. Every combining rule mixes
that true-but-useless number in. `forward` never sees it.

**The one-sided-rule objection was tested, not argued.** `grounding.py`'s header
warns a one-sided rule lets a hub attach to everything. At depth 2 `distractor`
is **0.0000** in every cell — the objection is about the DISTRACTOR'S list, and a
query starting at the word never reads it.

**And alpha is the deciding variable**, which `g39-03` had found it was not under
the symmetric combiners: at 0.5 the distractor is admitted at 1.0000, because a
square root under-corrects for a surface fifty times more common.

**`strength`'s original justification is retired.** It was introduced as soft
mutuality with a doubt attached; the doubt was right about `min` and the answer
lay off the min-to-max axis entirely. `SYMMETRIC` now names the four that
combine.

**Not settled:** depth 3 and beyond, where the one-sided objection gets stronger
with distance; one code count and one beam.
