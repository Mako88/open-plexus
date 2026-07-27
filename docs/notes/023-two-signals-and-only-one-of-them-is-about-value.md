# 023 — Two signals, and only one of them is about value

**Status:** [g9-05](../../experiments/sweeps/g9-05-a-tag-that-fades.txt) RUN,
32 of 32 cells. The account below was written from a pre-dispatch control and the
sweep did not overturn it. One axis pinned, so
[g9-06](../../experiments/sweeps/g9-06-is-the-tag-capacity-starved.txt) is running.
**Changes:** what [022](022-the-signal-was-there-and-pointing-backwards.md) implied
the tag would do.

---

## IN PLAIN TERMS

The model has to choose what to keep, and the signal telling it what mattered
arrives too late to help. So it keeps everything briefly and, when the signal
lands, throws away all but a handful of recent things.

Two ways to pick that handful. **By clock:** keep whatever happened in the last
N steps. **By content:** mark the few things that look like real information and
keep those, however long ago they happened. The second sounds obviously better,
because you never have to guess N.

Counting what each one actually keeps says otherwise, and the reason is worth
more than the mechanism. Picking by content finds *a* piece of information. But
the task asks about a *particular* one — and the only thing that identifies which
is how close it was to the signal. Picking by clock is not a cruder way of
finding information. It is the only way of finding *that* information.

---

## What was built

A **tag**: a fixed number of marks over writes rather than a span over steps.
Admission is on weak retrieval, because [g9-04](../../experiments/sweeps/g9-04-is-there-a-local-signal.txt)
measured the signal inverted at AUC 0.293 and 0.215. When the reward token
arrives, whatever is still marked survives and the rest of the interval comes
back out of the store.

`admit` is reused with the rank negated, which is the finding expressed as one
minus sign: competitive capture ranks on the same quantity and keeps the
strongest.

## The part that was left out, and it was in note 010 all along

Note 010 took the shape of synaptic tagging from Lehr et al. — a **decaying**
marker. The first build had no decay, and the control says that is not a detail:

| tag of 8 | delay 1 | delay 8 | delay 20 |
|---|---:|---:|---:|
| no fade | 9% | 9% | 16% |
| fade 0.99 | 44% | 44% | 34% |
| fade 0.95 | 100% | 84% | 25% |

Percentage of captures in which the *rewarded* binding survived, over 32
captures.

**Why an un-faded tag fails is a fact about the store, not about the signal.**
Right after a capture the store holds only what survived — a handful of writes —
so everything retrieves weakly for a while. The weakest retrievals a tag will
ever see are the writes made just after the previous capture. Without ageing it
ranks the whole interval at once and fills with those. Measured: an un-faded tag
keeps exactly 2 of 32 rewarded bindings at slots 1, 2 and 4, unmoved by either
dial. It is a recency policy pointing backwards.

## The fade was implemented backwards first, and it looked fine

`admit` keeps the **largest** rank. A tag admitting weak retrievals holds
negative ranks, so a mark fades by its rank growing in magnitude; one admitting
strong retrievals holds positive ranks, where fading means shrinking. The first
version multiplied both by the factor, which releases one end and makes the other
**immortal**.

It produced numbers identical to no fade at every setting from 0.99 down to 0.7.
Not similar — identical, to the digit. A dial that is parsed, validated, stored,
read every step and applied, and does nothing.

It was caught by noticing a variable that did not move its output, which is
CLAUDE.md rule 6, and it is now pinned by
`the-fade-entrenches-instead-of-releasing` in `tools/mutate.py` and by a test
asserting the property in the form that does not mention the arithmetic: a rank
falls whichever end is winning.

## The finding, which is not about the tag

g9-04 measured recency at AUC 0.479 separating a binding-write from a
filler-write, and concluded a window's only virtue was ever *reaching* the
binding. That conclusion is correct and it is about the wrong question.

Recency carries no information about **being a binding**. It carries almost all
the information about **being the rewarded binding**, because the generator puts
the reward token a fixed distance after the cue. So:

    weak retrieval  ->  this write is a binding          (AUC 0.22, real)
    recency         ->  this binding is the rewarded one (the delay, exactly)

A gate needs both, and the two mechanisms each have one. The control shows it
directly: where its reach covers the delay, the window captures the rewarded
binding in **32 of 32** captures and the tag in 14. Where it does not, the window
captures **none** and the tag still captures 11.

**They are not a better and a worse selector of one thing. They select different
things.** The tag was proposed as the fix for the window's cliff, and it is not —
it is the other half of a gate whose first half already worked.

## What that costs the tag's billing

The fade is a time constant, and a time constant is a span with a soft edge. Read
the table again by column: 0.95 and 0.9 are best at short delays and collapse at
20; only 0.99 holds anything at 20. **The matching problem comes back**, in
softer form, which is the strongest reason to expect g9-05 to fall short.

What is not yet known, and what the sweep is for: the tag stores about a quarter
as much as a window that reaches as far, and retrieval goes as `sqrt(d / N)`. A
gate that captures a third as often but stores a quarter as much is not obviously
worse, and no capture count can settle it.

## What the sweep said

The control's account survived contact with 32 trained cells, and the table is
sharper than the account was.

| slots | fade | d1 | d4 | d8 | d20 | spread |
|---|---|---:|---:|---:|---:|---:|
| 8 | 1.0 | -0.19 | -0.21 | -0.22 | -0.18 | 0.04 |
| 8 | 0.99 | +0.00 | -0.00 | -0.02 | -0.03 | **0.03** |
| 8 | 0.95 | +0.24 | +0.21 | +0.15 | -0.17 | 0.41 |
| 8 | 0.9 | +0.24 | +0.23 | +0.16 | -0.22 | 0.46 |
| window | | +0.23 | +0.23 | +0.23 | -0.24 | 0.47 |

**The rows that are flat are flat at zero. The rows that are positive have the
window's cliff.** No cell is both, and being both was the entire claim.

Two things in that table were not in the control.

**The tie.** At delays 1 and 4 the tag matches the window while keeping about a
quarter as much. The control could not settle whether a gate capturing a third as
often but storing a quarter as much comes out ahead; it comes out level. So the
storage saving does pay for the lower capture rate, and the two mechanisms reach
the same recovery **from different signals** — which is the argument for
combining them rather than choosing.

**The un-faded tag is actively harmful**, -0.18 to -0.22 at every delay and both
capacities, worse than not gating at all. The control said it captured almost
nothing; the sweep says capturing the wrong writes costs more than capturing
none. That is the fade earning its place twice.

**Held back by a pinned axis.** `tools/grid.py` reports every delay choosing
`slots 8`, the top of the grid, so these are lower bounds. If a larger pool
produces a row that is both flat and positive, this note is wrong and the tag is
a mechanism after all. That is g9-06's prediction 2.

## What would make this note wrong

- **The tag's recovery is flat across delay and above zero.** Then the reach
  argument above is too pessimistic and the mechanism stands on its own.
- **`tag` and `tag-strongest` score the same.** Then the capacity is the whole
  mechanism, g9-04's inversion is decoration, and the account here of *why* the
  window wins is unsupported even though its arithmetic holds.
- **The un-faded tag scores like the faded ones.** Then the capture counts above
  do not reach the score and nothing in this note counts.

---

*Related: [022 — the signal was there, pointing backwards](022-the-signal-was-there-and-pointing-backwards.md),
[021 — reach has to be matched](021-reach-has-to-be-matched.md),
[010 — tagging and capture](010-tagging-and-capture.md),
[015 — competition for a finite pool](015-we-implemented-the-tag-and-not-the-competition.md).*
