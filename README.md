# Open Plexus

Can a neural network learn using only **local information** and **bounded
asynchrony** — so that it runs on consumer devices that are unreliable,
heterogeneous, and constantly leaving?

Today's AI runs in data centres because training requires every part of the
network to exchange information with every other part, in lockstep, many times a
second. That is only affordable when the machines sit in one building on a
dedicated network, and it means the scale of an AI system is set by how much
capital one organisation can raise.

Meanwhile there are billions of computers, phones and consoles sitting idle in
people's homes — already bought, already paid for, connected by the ordinary
internet. This project asks whether a network can be built for *those* machines:
not a faster network, a differently-shaped one, where no part ever waits for a
global picture and a machine leaving mid-thought is a normal event.

## The documents, and which one to read

Three documents carry the project, and they are deliberately kept from doing each
other's jobs — a document that holds intent *and* results *and* a todo list goes
stale in all three at once, which is how this repository once ended up quoting two
different answers for the same exponent two paragraphs apart.

| document | what it holds | when |
|---|---|---|
| **[STATE.md](STATE.md)** | what is true now, what is open, what is running | **first, and every session** |
| **[GOALS.md](GOALS.md)** | what this is for, the constraints, what would refute it | before deciding whether a mechanism belongs here at all |
| **[DECISIONS.md](DECISIONS.md)** | a chronological log of what was chosen and why — history, never rewritten | when you need the reasoning behind one past choice |

**STATE.md is the only one kept current.** Where it and the log disagree, it wins.

- **[docs/explainers/](docs/explainers/)** — plain-language explanations of
  everything here, in reading order, written for someone who does not work in
  this field. **Start here if you want the ideas rather than the specification.**
- **[docs/notes/](docs/notes/)** — the reasoning: question, prediction made before
  the run, result. Never edited afterwards except to record the outcome.
- **[experiments/sweeps/](experiments/sweeps/)** — every measurement, with the
  predictions registered before dispatch and scored honestly, including the
  refuted ones.
- **[docs/archive/](docs/archive/)** — superseded records, kept for their
  reasoning and clearly marked as history.
- **[CLAUDE.md](CLAUDE.md)** — the engineering standards the project runs under.

## Status

**G0–G3 passed; G4 passes on one seed; G5 is contested.** The live work is
relational reasoning — composition over chained facts — and the current blocker is
retrieval fidelity, which decision 112 measured as a width limit. See
[STATE.md](STATE.md).

## The three constraints

| | |
|---|---|
| **Locality** | No operation may require globally synchronised state — even when violating it improves the numbers. |
| **Bounded asynchrony** | Information arrives late, out of order, at varying delay. The design states a bound and is correct below it. |
| **Churn** | Machines leaving is the normal case, not an edge case. |

## What refutes it

Six gates, ordered by cost of finding out, each with the outcome that kills the
project at that stage. `G0` is first and is the correction of the predecessor's
most expensive mistake: **prove the benchmark leaves a learning rule something to
do, before writing a learning rule.** See [GOALS.md §4](GOALS.md).

## Relationship to plexus

Open Plexus replaces `plexus` (`Mako88/submenu`, branch
`claude/bio-inspired-neural-model-ohhrp6`). It is a restart rather than a fork,
for two reasons: that architecture was built without a plan first, and it was
framed as "biology, but better" rather than "the machines and the network
already exist — build for those."

No code is inherited. What is inherited is its record of what did not work,
which is the most useful thing it produced. [GOALS.md §6](GOALS.md) states what
transfers and at what confidence; most of it transfers at *none*, and says so.

## Licence

MIT.
