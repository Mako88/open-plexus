# Note 007 — The stack, and what gets built first

[GOALS.md §7](../../GOALS.md) deferred the implementation language until there
was something to satisfy constraints for. There now is.

---

## IN PLAIN TERMS

Time to write actual code. This picks the language and decides what the very
first piece is.

The choice is Python, with **no external libraries at all** for this first
layer — which is unusual and deliberate. The first thing we build is the *test
itself*: the memory game our future system will be measured against. It needs no
network, no learning, and no maths library, and building it first means the
measuring instrument exists before anything that gets measured by it.

---

## 1. The decision

**Python 3.14, standard library only, for the task and measurement layer.**

Against [GOALS.md §7](../../GOALS.md)'s four constraints:

| constraint | how this satisfies it |
|---|---|
| Research kernel optimises for speed of asking questions and access to the prior-work ecosystem | Python is where the entire relevant literature lives — Zoology, e-prop implementations, reservoir toolkits. Nothing else is close. |
| The research kernel must not become the project | Already installed (3.14.4, verified). Zero setup, zero build step, zero toolchain. |
| A reference implementation must exist that is obviously correct and slow | **This is why the task layer takes no dependencies.** A generator that anyone can read top to bottom, with no library semantics to reason about, is auditable in a way a vectorised one is not. The fast version, when it exists, gets asserted against this one. |
| GPU availability is not a constraint | Nothing here touches a GPU. |

**Deferred, not decided:** the eventual consumer-device runtime. Shipping to a
stranger's laptop with no toolchain is a different problem with a different
answer, and pretending one language serves both is the assumption
[GOALS.md §7](../../GOALS.md) said to state rather than make. **Stated: we are
choosing the research kernel only.**

**numpy is not installed on this machine** — verified, not assumed. That is
fine and is part of why the task layer avoids it. It becomes an explicit
install step when models arrive, and that step is a decision to take on
purpose rather than a dependency that creeps in.

**Tests use `unittest` from the standard library**, written as plain
`TestCase` classes. `pytest` runs those unchanged if it is ever adopted, so
this locks nothing in while keeping the suite runnable on a bare Python.

## 2. What gets built first, and why it is the task

Rule 16 requires a build block after a verification block, and six notes of
analysis is more than enough verification. But the choice of *what* to build is
not free:

**The measuring instrument comes before the thing measured.** G0 is first in
the gate ladder precisely because everything downstream is read through it. So
the first code is the benchmark, not the model.

This also has a property worth naming: **it is buildable now.** It needs no
substrate, no credit-assignment scheme, no transport, and no decisions this
project has not yet made. It is the largest piece of durable work available
that depends on nothing unresolved — which is the test John's standing
preference sets for not building scaffolding a later phase replaces.

**Multi-query associative recall**, per [note 006](006-verifying-the-reservoir-claims.md)'s
correction: `K` key–value pairs in a sequence, with **all `K`** queried.

## 3. What the first commit contains

- **`openplexus/tasks/mqar.py`** — the generator. Pure stdlib.
- **`tests/test_mqar.py`** — including connection tests per rule 6: perturb an
  input, assert the output moves.
- **`tools/mutate.py`** — the harness that verifies the tests can actually fail.
  **This is not optional and not deferred.** Rule 10 is unenforceable without
  it, and [CLAUDE.md](../../CLAUDE.md)'s Conventions section has been carrying
  it as an unfilled placeholder since the first commit. Building code without it
  would violate the project's own standard on the project's first day.

## 4. The filler modes carry an argument

The generator takes a `filler` parameter with three modes, and this is
[note 002 §7](002-which-credit-assignment-scheme.md)'s unresolved tension made
executable rather than argued.

The tension: note 001 wants irrelevant material in the sequence, because a
random substrate cannot choose what to discard. Note 002 objects that random
material is *unpredictable*, which starves a predictive objective.

- **`random`** — filler drawn uniformly. Maximises the retention challenge,
  starves prediction. Note 001's preference.
- **`structured`** — filler follows a deterministic cycle. Still irrelevant, so
  still has to be discarded; but predictable, so a predictive objective has
  signal. **Note 002 §7's proposed resolution.**
- **`none`** — no filler. The control that isolates the filler's effect.

**Both notes' positions are now conditions in one sweep rather than an argument
in two documents.** Whether structured filler actually resolves the tension is
measurable, and it becomes measurable the moment there is a substrate.

## 5. What this does not do

- **No model.** Nothing learns yet. The generator and the base rate are the
  floor and the ruler, not a result.
- **The base rate is measured, not asserted.** Rule 6's habit applied to our own
  arithmetic — what a constant predictor scores gets *run*, because a base rate
  reasoned about is a base rate that can be wrong.
- **No claim that the task is right.** [Note 006 §9](006-verifying-the-reservoir-claims.md)
  records that a random frozen reservoir was not among the architectures the
  source tested, so its position on this benchmark is still inferred.
