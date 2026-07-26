# Note 008 — The task/objective mismatch, and the three ways out

[g1-01](../../experiments/sweeps/g1-01-predictability.txt) found that the
predictive objective and MQAR are mismatched. That sweep note said each way out
needs arguing before anything is built. This is that argument.

**It also corrects a claim made in the previous turn's summary that went further
than the evidence, and inverts a conclusion in
[note 002 §7](002-which-credit-assignment-scheme.md).**

---

## IN PLAIN TERMS

Our learning method and our test don't fit together, and we found out cheaply.
This works out what to do about it.

Along the way it corrects two things we'd said. One was a claim about *why* they
don't fit that sounded good and went beyond what we'd measured. The other is
better: a decision we made earlier turns out to be **exactly backwards**, and
fixing it costs nothing.

Then it lays out three ways forward, says what each would cost, and recommends
one — while being clear that this is a direction change and the choice belongs
to John rather than to this note.

---

## 1. What is actually established

Three measurements, all held:

- A frozen substrate cannot predict the answer token above base rate: **0.135
  against 0.140** (g1-01, autoregressive).
- Structured filler is highly predictable: **0.824 against 0.042**.
- Random filler is not predictable at all: **0.029 against 0.035**.

And one count, computed here at the reference configuration:

| position kind | share |
|---|---|
| filler | **83.3%** |
| pair | 8.3% |
| query | 4.2% |
| answer | 4.2% |

## 2. A claim from last turn that went too far

The previous summary said the task "has a step, not a slope" — that the answer
is unpredictable until retrieval works and perfectly predictable afterwards,
with nothing in between, so a gradient-following learner has nothing to climb.

**That is a claim about an optimisation landscape, and nothing here measured
one.** What was measured is that a *frozen* substrate sits at chance. That is
consistent with a step, and equally consistent with a smooth slope whose bottom
end happens to be where a random substrate sits.

The output-space version is probably not even true: a mechanism that retrieves
correctly 30% of the time would presumably predict the answer about 30% of the
time. Partial retrieval buying partial prediction is a slope.

The real question — whether a *path in parameter space* leads from "random" to
"retrieving" — is a claim about trainability that this project cannot settle
without training something. **It should not have been stated as though the
measurements showed it.** Rule 1, applied to our own summary rather than to the
literature.

The symptom stands. The explanation was decoration.

## 3. The reframe: reducible loss, not "signal"

Dropping the overstatement leaves a sharper question, and it is the useful one.

A predictive objective does not care whether a position is "predictable". It
cares about **reducible loss** — how much better the model could do at that
position than it currently does. That is where gradient comes from.

Sorting the reference configuration on that basis:

| positions | share | reducible loss, structured filler | reducible loss, random filler |
|---|---|---|---|
| filler | 83.3% | **large** — 0.824 achievable, and easy | **none** — 0.029, irreducible by construction |
| answer | 4.2% | large, but only after retrieval works | large, but only after retrieval works |

**With structured filler, 83% of positions offer a large, easy, entirely
task-irrelevant loss reduction.** A predictive objective would descend into
continuing the counting cycle, because that is where almost all the available
improvement is.

**With random filler, those same 83% of positions offer nothing at all.** The
loss there is irreducible — no model can predict a uniform draw — so no gradient
flows toward it. The only place loss can decrease is the answer positions.

## 4. Note 002 §7 was inverted, and the fix is free

Note 002 §7 argued: random filler is unpredictable, therefore it **starves** a
predictive objective, therefore structured filler is the resolution.

That reasoning conflates *signal* with *useful signal*, and gets the sign wrong.

> **Random filler is the correct choice for training a predictive objective,
> and structured filler is the harmful one.**
>
> Starving the objective of *useless* signal is not a cost. Irreducible loss
> contributes no gradient — it is a constant added to the objective, not a
> distraction. What matters is where the *reducible* loss lives, and with random
> filler the only place it lives is the answer positions, which is exactly where
> we want the learner looking.

The note's stated worry — "the learning signal is dominated by irreducible error"
— is true and turns out not to matter. A large constant does not compete for
gradient. A large *reducible* term does.

This costs nothing to act on: `filler="random"` is already implemented, tested
and swept. **It is a one-word change that was previously believed to be the
wrong one.** Note 002 §7's entry is corrected in place.

**Caveat, stated rather than buried:** this is an argument about where gradient
comes from, not a measurement of a trained model. It is the same *kind* of claim
as §2's overstatement, and it should be labelled as such — an argument, pending a
trained model to check it against. The difference is that it follows from
measured quantities rather than asserting a new one.

## 5. The three directions

### (a) A task with graded predictable content

Replace or supplement MQAR with something language-like, where partial context
buys partial predictability — natural text, a Markov source, a formal grammar
with statistics.

**For:** it is the only option that also serves goal 2 directly. Next-token
prediction over structured sequences *is* what a language model does, so a
result transfers rather than analogises. It supplies the graded structure a
predictive objective wants, at every position rather than 4.2% of them.

**Against:** it is the largest change. It abandons a benchmark that took five
sweeps to build, validate and de-bug, whose floor is characterised, whose
answerability is mechanically checked, and which is now known to discriminate a
frozen substrate from a capable one. Building an equally trustworthy language
benchmark is not a small job, and the G0 discipline would have to be redone for
it — including finding what its trivial floor is, which took a refuted
prediction to discover last time.

### (b) Split the roles — MQAR measures, something else trains

Keep MQAR as the capability benchmark. Train the predictive objective on a
different distribution.

**For:** preserves everything already built. Honest about what each artefact is
for. Common practice — pretrain on one distribution, evaluate on another.

**Against:** it abandons note 001's property P2 on purpose, and P2 was the
stated reason for choosing MQAR at all. Losing it reintroduces exactly the
ambiguity P2 was meant to remove: a null result could mean the objective does
not learn, or that what it learns does not transfer to the benchmark, and the
two are inseparable. That ambiguity cost the predecessor dearly.

### (c) Reconsider the objective

Note 002 ranked dendritic error second. If the predictive objective needs a
task shape MQAR cannot supply, that ranking may deserve revisiting.

**For:** the evidence for the predictive objective is weaker than when it was
chosen — note 005 found its supporting literature describes a supervised variant
we cannot use, so it now rests on the §4 latency argument alone.

**Against:** the latency argument is *structural* and remains the strongest
thing in the project. Dendritic error is a *delivery* mechanism (note 002 §2)
and does not answer the source question; adopting it means reintroducing a
broadcast source, which is the thing measured inert in the predecessor. **This
direction most likely walks back into the trap note 002 was written to escape**,
and should not be taken on the strength of one task-pairing problem.

## 6. Recommendation

**(a), with (b) as the fallback, and (c) rejected for now.**

The reasoning is that this is not really a choice about MQAR. It is a choice
about what the project is for. Goal 2 is replacing pieces of a language model;
the objective already chosen is next-token prediction; a task made of uniformly
random symbols was always going to be a strange vehicle for that, and g1-01 is
the measurement that made the strangeness concrete.

**MQAR is not discarded under (a).** It stays as a capability probe — that is
what five sweeps have made it good at, and its floor, answerability check and
frozen-substrate baseline all remain valid. What changes is that it stops being
asked to *train* anything.

Which means (a) and (b) differ less than they appear: both keep MQAR for
measurement, both introduce a second distribution. **(a) is (b) with the second
distribution chosen to serve goal 2 rather than chosen for convenience.**

**What would change this recommendation:** evidence that a predictive objective
can bootstrap on MQAR after all — which would be a trained model reaching above
the trivial floor with random filler. That is worth trying *before* committing to
(a), because it is cheap now that numpy is available and it would make the whole
question moot.

## 7. What this does not settle

- **No trained model exists.** Everything above reasons about where gradient
  would come from. That is an argument.
- **The graded-task option has no candidate.** "Something language-like" is not
  a specification, and choosing badly here is exactly the mistake note 001 was
  written to prevent — including the part where the trivial floor turns out to
  be much higher than expected.
- **This is a direction change and the choice is John's.** The note records the
  argument so the decision has something to sit on, and does not act on it.
