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

## Status

**Goals only.** No plan, no architecture, no code, no stack chosen. Nothing here
has been measured.

- **[GOALS.md](GOALS.md)** — what this is for, the three constraints that define
  the design space, and the gate ladder that would refute it. Start here.
- **[CLAUDE.md](CLAUDE.md)** — the engineering standards the project runs under.

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
