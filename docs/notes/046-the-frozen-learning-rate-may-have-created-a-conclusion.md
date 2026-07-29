# 046 — The frozen learning rate may have created a conclusion

**Status:** an audit with a measurement behind it, written while the sweep that
settles it is in flight. **Affects:** g17-01's conclusion, and through it the
reason the architecture line turned toward addressing at all.
**Precedent:** [note 028](028-the-learning-rate-has-been-frozen-for-seven-sweeps.md),
which found the same defect one line earlier and was not applied here.

---

## IN PLAIN TERMS

The model has a dial for how fast it learns. It was set to one value long ago,
for a different experiment, and every experiment since has carried that value
without looking at it.

Last cycle we ran the model on words instead of letters for the first time, and
it did badly enough that we concluded it cannot learn words at all. That
conclusion is why the whole current line of work exists.

**The dial was never checked at word level.** Turning it down improves the model
by more in one probe than the entire effect the conclusion was based on.

---

## What was measured

20,000 words of Shakespeare, width 128, readout bias on, no cap and no decay.
`floor` is the model exactly as g17-01 ran it; `concept-128` is the new
addressing.

    lr        floor    concept-128
    0.05     10.186    DIVERGED     |Wo| reached 1.6e63
    0.01      9.851    10.353
    0.005     9.804    10.349
    0.001     9.734    10.428       <- floor still improving at the grid edge

Uniform is 10.759 and the word unigram is 8.170 at this data size.

**g17-01's headline is that 90,000 words buys 0.038 bits over uniform.** The
learning rate buys **0.45** at 20,000 words, in the same direction, and had not
stopped moving when the grid ran out.

## Two separate things this explains

**The divergence is not a property of concept addressing.** It looked like one:
collapsing the address space raises recurrence, the store is written far more
often at each address, and the readout blew up. But at lr 0.005 the same arm runs
with no cap and no decay at all. **The brake belongs on the readout, not on the
store** — which is [decision 132](../../DECISIONS.md)'s finding in the other
component: a rate tuned for a store that returns near-nothing, meeting a store
that returns something.

**And it may explain the wall this line was built to attack.** If the floor moves
at 90,000 words the way it moves at 20,000, then *"the model does not learn
word-level text at all"* is partly a statement about one hyper-parameter.

## Why this is worse than note 028's version

Note 028 found `lr 0.05, FIXED` in seven consecutive sweeps and said plainly that
the rate moves the floor — the denominator of every score in that line — by a
factor of three. It was written on 2026-07-28. **g17-01 ran the next day and
froze the same value at a unit the project had never used before.**

A frozen axis is defensible when the configuration has not changed. Moving from
characters to words changes the vocabulary from 65 to 1,733, the address space
from hundreds of pairs to tens of thousands, and the logit scale by an order of
magnitude. It is the *least* defensible place to inherit a constant, and the note
warning about it was one day old.

**The habit that failed is not "sweep the learning rate".** It is: when a
measurement crosses a boundary that invalidates the comparison set — which
g17-01's own docstring says word level does, in those words — every inherited
constant crosses it too.

## What it does NOT invalidate

**The unigram gap is real at any rate measured so far.** The best floor in this
probe is 9.734 against a unigram of 8.170: still 1.56 bits worse than counting.
Nothing here says the model is fine at word level.

**And it does not touch the character-level comparison set**, where 0.05 was
chosen and where the configurations it was chosen for still hold.

**Nor does it rescue the mechanism.** At every rate that runs, `concept-128` is
*worse* than the surface floor — 10.35 against 9.80 at lr 0.005. If that survives
to 90,000 words and three seeds, storing by concept is refuted on its own gate
regardless of what the rate does to the baseline.

## What settles it

g18-00, dispatched 2026-07-29:
`{floor, concept-128, stratified-128} × lr {0.05 … 0.0005} × cap {0, 5}` at
90,000 words. Chosen by `fit_error` — held-out training text — because a rate
picked by the test set is a rate fitted to the test set.

**g18-01 is held until it lands**, with its rate and cap set to a string that
parses as a float in nothing, so it fails on its first line rather than returning
a number measured against a handicapped floor.

## The rail this produced

`lr 0.005` holds `concept-128` at 20,000 words and **blows up at 90,000**: 36.9
bits against a 10.759 uniform, with no NaN anywhere. A setting that survives a
probe need not survive the run, and the cell that results is *finite* — it would
have gone into a table as a number and moved an arm's mean by tens of bits while
still looking like an ordinary row.

So `unstable` now sits beside `diverged`: a calibrated model cannot be much worse
than uniform, because the temperature fit would flatten it. Being above uniform
at all means the calibration text and the test text disagree about what the model
does, and the cell is not a measurement.
