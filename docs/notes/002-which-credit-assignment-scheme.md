# Note 002 — Which credit-assignment scheme, and does it satisfy C1 and C2?

Answers open question 2 in [GOALS.md §8](../../GOALS.md). Produces a
recommendation and the argument for it. **Nothing here has been measured.**

---

## IN PLAIN TERMS

Every learning system has to answer one question: when the answer comes out
wrong, which internal part was to blame? That is the hard problem, and the
standard answer is the thing that forces a data centre.

This note picks our answer. The main finding is a distinction the previous
project never drew: there are two separate things that get called "how learning
works," and only one of them decides whether the system can be spread across the
internet. The previous project spent its effort on the half that could not fix
the problem.

The recommendation is **each part predicts what it is about to receive next, and
learns from the difference.** The argument for it is one specific property:
under this scheme, a slow network costs *memory* instead of causing a *race*.
That is the whole case, and §4 is where it is made.

---

## 1. The question

[GOALS.md §5](../../GOALS.md) requires the credit-assignment scheme to be chosen
before any substrate exists, on paper, with the locality and latency argument
written out. This is that argument.

The scheme must satisfy:

- **C1 — locality.** No operation may require globally synchronised state.
- **C2 — bounded asynchrony.** State a delay bound and be correct below it.
- **C3 — churn.** A machine vanishing mid-computation is normal.

## 2. The distinction the predecessor did not draw

The prior-work table in [GOALS.md §6.2](../../GOALS.md) lists seven mechanisms as
though they were alternatives. They are not. They fall into two categories that
answer different questions:

| | question it answers |
|---|---|
| **Error source** | Where does the learning signal *come from*? |
| **Error delivery** | How does that signal *reach* the weight being changed? |

- **Sources:** supervised broadcast error; self-supervised prediction;
  Forward-Forward's goodness objective.
- **Delivery:** eligibility traces, dendritic compartments, burst multiplexing,
  feedback alignment.

**Only the source determines whether C1 and C2 are satisfiable.** A delivery
mechanism moves a signal more cleverly; it cannot make a signal local that was
computed from global information. If the source needs the whole network's
output, no amount of clever routing fixes it.

**This is the structural diagnosis of why the predecessor was stuck.** It chose
sophisticated delivery — eligibility traces, a routed modulator, dendritic
branches, a `dfa` mode — and never questioned the source, which stayed a
supervised broadcast error throughout. Its measured dead ends follow directly:
the credit window of 12 steps against the 150 needed for intercontinental lag is
a *delivery* measurement of a *source* problem. The window was never going to
open far enough, because the signal always had somewhere to travel from.

The corollary is that its delivery machinery is not discredited. Eligibility
traces may be perfectly good. They were attached to the wrong source.

## 3. The sources, against the three constraints

| source | C1 locality | C2 asynchrony | C3 churn |
|---|---|---|---|
| **Supervised broadcast error** | **Fails.** The error is computed from a global output compared to a global target. | **Fails.** Must arrive while the trace still holds the activity it refers to — a race, and one already measured to be lost. | Fails. A missing node corrupts the global output. |
| **Self-supervised temporal prediction** | **Passes.** Error is computed from what the unit already receives. Nothing is consulted. | **Passes — see §4.** Delay costs buffer depth, not correctness. | **Passes.** A vanished input is locally observable; nothing global needs to notice. |
| **Forward-Forward** | Mostly passes — the objective is per-layer and local. | **Partial.** Requires agreement on which pass is running, and on where negative data comes from. That is shared state, though an epoch tag may be enough rather than a barrier. | Unclear. Untested here. |

## 4. The central argument: latency becomes a buffer, not a race

This is the whole case for the recommendation, and it is worth stating precisely
because it is easy to wave at and hard to believe until it is spelled out.

**Under a broadcast source**, a unit acts at time `t`, and an error about that
action arrives at `t + d`. For the update to be correct, the unit must still be
holding a trace of what it did at `t`. Traces decay. So there is a **race**: the
signal must arrive before the memory of the thing it refers to fades. Widening
the trace to survive a longer delay makes it less specific — it now refers to
everything the unit did over a long window, so the credit gets blurrier the
further it has to travel. *Latency and precision trade against each other, and
the trade is unavoidable.*

That is exactly the shape of the predecessor's measured wall: a 12-step window,
against ~150 steps for an intercontinental round trip, with widening attempts
buying a little and costing accuracy.

**Under a temporal-prediction source**, a unit predicts at time `t` what it will
receive at `t + 1`. The comparison happens when that input actually arrives. If
the input comes from a machine 150 ms away, it simply arrives 150 ms later — and
the unit compares it against the prediction it stored.

**There is no signal that can be late, because there is no signal in transit.**
The error is *manufactured locally at the moment of comparison*, out of two
things the unit holds: its own prediction, and the input that just landed.

What the delay costs is **buffer depth** — the unit must retain its prediction
until the corresponding input arrives. That is a memory cost, it is bounded, it
is known in advance from the delay bound, and crucially **it does not degrade
the precision of the credit.** A prediction held for 150 ms and then compared is
exactly as sharp as one held for 1 ms.

> **Latency converts from a race condition into a memory cost.** This is the
> single property that recommends the scheme, and it is what C2 was written to
> ask for: a stated bound, with exactness below it.

The bound is then a design parameter: buffer for `d_max`, and any input arriving
within `d_max` produces a bit-identical update regardless of when it turned up
or in what order relative to other inputs.

**This is an argument, not a measurement.** It is a claim about the structure of
the scheme, and structural claims can still be wrong in implementation — §6
states what would show that.

## 5. The variant matters: temporal prediction, not relaxation

"Predictive coding" names two related things, and **one of them violates C1.**

**Hierarchical predictive coding** (Rao & Ballard 1999; Whittington & Bogacz
2017) has each layer predict the layer below, then **relaxes the whole network
to equilibrium** before weights update. Inference is an iterative settling
process: messages pass up and down repeatedly until the state converges.

That settling loop is many round trips through the network per input. Over
links with a 150 ms round trip it is ruinous, and while it is not a global
barrier in the strict sense, it is a repeated global back-and-forth — the thing
C1 exists to forbid. **The literature result that predictive coding approximates
backpropagation depends on that settling.** Taking the result without the
settling is not supported by it.

**Temporal prediction** — predict the *next input in time*, compare when it
arrives — needs no relaxation. One pass. The target is supplied by the future
rather than by convergence.

**We want the temporal variant.** It is also the variant that matches the
secondary goal: next-token prediction is temporal prediction, and a language
model does not settle to equilibrium between tokens.

**This distinction has to be carried carefully into the reading**, because both
go by the same name and the hierarchical one is far better represented in the
neuroscience literature. Any number quoted from a predictive-coding paper needs
checking for which variant produced it.

## 6. Predictions, and what would refute this

Written before anything is built (rule 4).

1. **A unit's own recent state predicts its next input above chance**, on the
   substrate eventually chosen. **This is the gate on the entire scheme.** If a
   frozen network's state carries no information about what it is about to
   receive, a predictive objective has nothing to learn from and the
   recommendation is dead. It costs one probe to find out and it should be run
   before any mechanism is built.
2. **Buffered comparison is exact below the delay bound.** Two runs, one with
   delays and reordering below `d_max` and one without, produce bit-identical
   weights. If they do not, the §4 argument has a hole in the implementation.
3. **Credit precision does not degrade with the delay bound**, unlike the
   broadcast case. Widening `d_max` costs memory and nothing else.

**What would refute the choice outright:** prediction being learnable but
producing representations that do not help the task. That is the standard risk
of any self-supervised objective, and note 001's P2 was chosen specifically to
reduce it — if the task's own metric *is* next-symbol prediction, learning to
predict and doing the task are the same activity.

## 7. A tension with note 001 that must be resolved before building

Note 001 recommends **noise fraction** as a difficulty dial, on the argument
that irrelevant input targets a reservoir's inability to retain selectively.
That is a good argument for opening the G0 gap.

**It works against the predictive objective.** If most of the sequence is
random, most of what a unit is asked to predict is unpredictable in principle.
The learning signal is then dominated by irreducible error, and gradient
information about the predictable part is a small fraction of a large noisy
quantity.

Both arguments are sound and they pull in opposite directions. Naming the
options rather than resolving it here:

- **Accept it and measure the interaction** — sweep the noise dial for both the
  G0 gap and the learnability of the predictive objective, and take the
  operating point where both are acceptable. There may be no such point, which
  is itself a finding.
- **Make the noise structured rather than random**, so it is predictable but
  irrelevant. This separates "hard to retain selectively" from "impossible to
  predict", which is what actually collides. Currently the most promising
  option, and it is free at task-design time.
- **Score prediction only on predictable positions.** Rejected on sight: it
  requires knowing which positions those are, which is global knowledge about
  the task, and it would not survive contact with real data.

**This tension exists because the task and the objective were chosen in
adjacent notes. Finding it now is the intended benefit of writing the argument
before the code**, and it is exactly the kind of thing that would otherwise
have surfaced as an unexplained null six months in.

## 8. What this does not settle

- **It does not choose the substrate.** It constrains it: units need somewhere
  to hold a prediction and a buffer sized by the delay bound.
- **It does not choose the delivery mechanism.** Within a unit, the error still
  has to reach the weights. Eligibility traces and dendritic compartments remain
  live candidates and are *not* discredited by §2 — they were attached to the
  wrong source, which is a different fault.
- **It does not establish that any of this learns.** That is G1.
- **The prior work has still not been re-read.** Every claim here about e-prop,
  predictive coding and Forward-Forward is from a second-hand summary. The
  §5 distinction in particular needs checking against the sources before it is
  relied on, and it is currently load-bearing.
