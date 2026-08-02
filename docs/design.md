# Method-level design

What every piece is and what every method does, in words. No bodies, no
implementations — this is the mental model, and the code is meant to match it
exactly. If they ever disagree, this file is wrong and gets fixed.

**Status: every type below exists as a stub and the solution builds. No method
has a body.** Each unimplemented field shows up as a `CS0169` build warning, so
the warning count is a rough progress bar — 27 at the point the stubs landed.

Scope: **snake**, running on one machine, with every boundary shaped so the
same code runs across many. Static background is out of scope for now — see
open fork 1b in [architecture.md](architecture.md).

---

## Project structure

One project and one test project. Folders, not assemblies.

```
OpenPlexus.sln
src/OpenPlexus/
  Codes/        what an identity is, and how raw input becomes one
  Graph/        Node, Cluster — the things that hold counts
  Bus/          the hybrid bus, the ring, the wire
  Learning/     onsets, offsets, and the join that forms connections
  Thinking/     messages, chains, thoughts
  Machines/     input and output, the world boundary
  Worlds/       snake
tests/OpenPlexus.Tests/
```

**Why not seven assemblies.** A project boundary should be earned by a
dependency rule worth enforcing. Folders move for free; projects do not. The
one boundary worth defending early is that `Graph/` and `Codes/` touch no I/O
and no bus, so results measured against them cannot be explained by transport.
That is a rule a test can assert without a `.csproj`.

---

## `Codes/`

### `Code`

A quantised fragment of one observation from one modality. Several fire for the
same thing. **Never a concept** — a concept is what you reach by walking.

- **`Modality`** — which front end produced it. Two front ends never produce
  the same code, so a picture and a sound cannot collide by accident.
- **`Value`** — the bits. For snake, an offset in the head-centred view plus
  what is in that cell.
- **`Prefix(k)`** — the top `k` bits. Only used by the cluster-placement
  locality arm (fork 3): LSH codes near in Hamming distance share prefixes, so
  hashing on a prefix puts similar codes on the same machine.

### `IQuantizer`

Turns one raw observation into the codes present in it. **The same input
produces the same codes on every machine, forever** — this is the red-ball
property, and it is why the quantizer is built from the shared seed and never
fitted to data. Two quantizers fitted on different samples agree about under
0.12 of items, which no amount of walking recovers.

- **`Modality`** — what this quantizer is a front end for.
- **`Codify(observation)`** — the codes present. Not "the code" — several.

---

## `Graph/`

### `Node`

One code, and its own row of counts. Holds edges, holds no address, knows
nothing about the network.

**State**

- **`_code`** — this node's identity. Never changes.
- **`_together`** — partner code → count. *How many occasions that code and I
  both fired on.* **The node's whole row, and the only thing that learns.**
- **`_seen`** — how many occasions this node fired on at all. Its own marginal.
- **`_settings`** — the swept dials. Identical on every node, or the same route
  gets priced differently depending on where it is standing.

**Reads and writes**

- **`Code`** — reads `_code`.
- **`Seen`** — reads `_seen`. Public because a *neighbour* needs it to weigh an
  edge pointing here.
- **`Note()`** — "I fired on this occasion." Adds one to `_seen`.
- **`Observe(other)`** — "that code fired on the same occasion I did." Adds one
  to that partner's entry. **`Note` and `Observe` together are the entirety of
  learning.**
- **`Together(other)`** — reads back one cell.
- **`Partners()`** — every code this node has ever co-occurred with. The
  fan-out of one hop.

**Thinking**

- **`Fire(message)`** — a message arrived carrying a chain, a budget and an
  accumulated strength. **Returns what should be sent next and what the
  accounting did. It does not send anything.** In order: weigh every partner;
  price the step; drop partners with zero weight and partners already in the
  arriving chain; for each survivor work out the new budget and drop it if that
  is not positive; build one outgoing message per survivor with this node
  appended to the chain; report `k-1` splits if `k` survived, or one death if
  none did.

  **`Fire` returning its output instead of sending it is deliberate.** A node
  is then testable with no bus, no cluster and no network — perturb the row,
  assert the outgoing set moves. That is the whole of the wiring problem made
  assertable.

- **`WeightOf(partner, marginals)`** — how strong the edge is: shared count
  divided by *the partner's* marginal — **how well the partner predicts me**.
  That is what refuses a thing present everywhere: it co-occurs with you
  constantly and predicts nothing in particular. Measured at 0.0000 for a
  distractor against 0.9800 for a real link. **This is the method that cannot
  work as written across machines** — that marginal lives on the partner's
  machine. Open fork 2.

### `IMarginals`

**Fork 2, made visible on purpose.** One method, `SeenOf(code)`, and its whole
job is to be the seam where this design collides with C1 — so it is an
interface you can see rather than a dictionary lookup buried in a loop.
**When fork 2 is resolved this interface should disappear. If it is still here
later, the fork went quiet.**
- **`Fuel(weight)`** — what a route is *paid* for taking the edge. Either the
  weight (survive by walking strong edges) or how surprising the edge was
  (survive by walking unlikely ones). Deliberately separate from the score;
  they are the same number under one setting, which is why they looked like one
  thing until surprise needed them apart.
- **`PriceOfAStep(weights)`** — what leaving here costs. Three arms: a flat
  charge; the mean edge (**refuted** — half a node's edges beat its own mean so
  a route gains budget forever and reaches everything, which answers nothing);
  or the strongest edge, an opportunity cost, **the only one measured to bound
  the walk**.

### `Cluster`

A set of nodes and an address. **The cluster subscribes to the bus, not the
node** — a small change from what we said earlier, and it is where the message
economy lives.

**State**

- **`_nodes`** — code → node, for every node this cluster holds.
- **`_address`** — where messages for those nodes are sent.
- **`_bus`**, **`_ring`** — how it sends, and how it works out where.

**Methods**

- **`Address`** — reads `_address`.
- **`Holds(code)`** — whether this cluster owns that node.
- **`Admit(code)`** — creates the node for a code this cluster owns and has not
  seen before. Nodes come into existence on first mention; nothing pre-creates
  the graph.
- **`Deliver(envelope)`** — the economy. Unpacks the many messages in the
  envelope, hands each to its node's `Fire`, collects every outgoing message,
  **regroups them by owning cluster**, and sends one envelope per destination.
  A node forking to 200 partners across 12 clusters produces 12 sends. Every
  hop that stays inside this cluster never touches the wire at all.
- **`ReportTo(thought)`** — batches arrivals and accounting back to the machine
  that started the thought, addressed by the message's return address.

---

## `Bus/`

### `Ring`

Which cluster owns which code. **Computed locally, agreed globally, no
directory and nobody to ask.**

- **`OwnerOf(code)`** — the address of the cluster holding that code. Every
  machine computes the same answer from the code and the shared seed. This is
  what lets a machine join a network it has never spoken to and route
  correctly immediately.
- **`Join(address)`**, **`Leave(address)`** — membership changes as machines
  arrive and vanish.
- **`Clusters`** — the current membership view.

**Views differ between machines while membership is changing, and that is
allowed.** A misrouted message is a lost count, not a corruption — the
statistics are counts over many occasions.

### `IBus`

- **`Subscribe(cluster)`** — a cluster becomes reachable. Returns a handle;
  disposing it leaves the bus.
- **`SendAsync(address, envelope)`** — get this envelope to that cluster.
- **`Deaths`** — fires when a machine leaves, so thoughts waiting on routes
  through it can release their state.

### `HybridBus`

- **`_local`** — clusters in this process. `SendAsync` to one of these is a
  **direct method call, not awaited on the sender's path, no serialization at
  all.**
- **`_peers`** — machines reachable over the wire. Same call, real latency.
- **`_seed`** — the shared constant every quantizer and every ring is built
  from. Handed out once and frozen, which C1 permits.

**The speed difference between local and remote is not a wart, it is the
experiment** — codes that land together are cheap to walk between, and that is
what a column is.

---

## `Learning/`

The path that **forms** connections. Everything above uses them.

### `LiveSet`

What is currently on, for one input machine. This is what makes the stream
discrete without sampling it.

- **`_live`** — code → when it started.
- **`Update(codes, now)`** — takes the codes present in this frame and returns
  **what started** and **what stopped**, by diffing against what was live.
  Everything that persists produces nothing at all.
- **`Live`** — the codes currently on.

### `IRendezvous`

How a node learns who it fired with.

- **`JoinAsync(onset, live)`** — a code just started while these were already
  live; make sure every one of them ends up with the others in its row.

### `LocalRendezvous`

The whole live set is on one machine, so the join is free: for each onset, pair
it with everything live and write both rows.

**It does not test the hard part**, and saying so is the point of naming it
`Local`. Two machines seeing different halves of the same moment is the case
that needs `buckets.Join`'s shape — a bucket owner computed by hash, noticing
an overlap and then being discarded. Fork 1.

### `Occasion`

- **`Codes`**, **`At`** — what fired together and when. What crosses the wire
  on the learning path, as opposed to a `Message`, which is the thinking path.

---

## `Thinking/`

### `Message`

What travels. The first two fields are the ones the Python did not need and a
network does.

- **`Broadcast`** — which thought this belongs to. **Without it, two thoughts
  in flight mix their chains and their death counts.** Non-negotiable under
  continuous input, where there are always many in flight.
- **`ReturnTo`** — where arrivals and death reports go.
- **`To`** — the code this message is addressed to.
- **`Held`** — budget remaining.
- **`Chain`** — every node walked, in order. The cycle check and the
  explanation, in one field, carried for free.
- **`Carried`** — accumulated path strength. The score, as against `Held`,
  which is the fuel.

### `Envelope`

Many messages for one cluster, sent as one. The unit the wire actually carries.

### `Thought`

One broadcast, on the machine that started it.

- **`_arrivals`** — endpoint code → best chain, summed score, strongest single
  route, how many routes reached it.
- **`_live`**, **`_splits`**, **`_deaths`** — the accounting.
- **`Receive(arrival)`** — accumulates. Keeps the **strongest single** chain as
  the explanation, because a summed score is no route's strength and reporting
  the last arrival would make the explanation whichever branch happened to
  finish last.
- **`Best(n)`** — the top arrivals **right now**. Readable at any time, which
  is what continuous operation requires: the system acts on what has arrived so
  far and later arrivals refine it.
- **`Balanced()`** — whether `origins + splits - deaths == live`. Exact in one
  process, and not across a network, which is why this is asserted rather than
  trusted.
- **`Release()`** — drop the state. Called on settle or on a death event.
  **Termination is housekeeping now, not correctness.**

---

## `Machines/`

### `InputMachine`

The world boundary. Holds an address, holds no edges, is in no walk — which is
why an arbitrary sensor can be attached without the graph knowing what it is.

- **`_quantizer`**, **`_liveSet`**, **`_rendezvous`**, **`_bus`**, **`_address`**.
- **`ObserveAsync(frame)`** — the whole input path in one place: quantize the
  frame; diff against the live set for onsets and offsets; **learn** by joining
  each onset with what was already live; **think** by starting a thought from
  the onsets. Persistence produces neither.
- **`Think(codes)`** — opens a `Thought`, mints a broadcast id, and sends one
  message per code to its owning cluster.

### `OutputMachine`

- **`_codes`** — the codes that mean an action. For snake, four.
- **`Choose(thought)`** — **arrival narrows, then rank.** The candidates are
  exactly the chains that reached one of this machine's codes; among those,
  the best-scoring wins.

  **This is the least designed part of the system and the honest thing is to
  say so.** On `master` the ranking step is 🚧 — approved, never built — and
  *nothing has ever turned a chain into an output*. Prediction-ranks and
  brevity-breaks-ties come after something works at all.

---

## `Worlds/`

### `Snake`

- **`Step(action)`** — advances one tick.
- **`View()`** — the **head-centred local** grid. Centred so the same situation
  in two places is one observation, which is what makes anything recur at all;
  local so food is usually unseen, which is what gives *act to disambiguate*
  something to disambiguate.
- **`Energy`** — depletes, food restores it, running out **ends** the run
  rather than resetting. **Nothing declares food good.** A policy that does not
  eat gets fewer steps of experience — selection without a reward, and the
  first source of preference this design has.
- **`Alive`** — whether the run is over.

### `SnakeQuantizer`

Lives here rather than in `Codes/` so that folder stays free of any world.

- **`Codify(view)`** — one code per visible cell, carrying the cell's offset
  from the head and its contents. **One-hot over contents, not a hyperplane**:
  `EMPTY WALL BODY FOOD` are 0 1 2 3 and a hyperplane over those numbers would
  make wall-and-body near and empty-and-food far, which is arithmetic nobody
  meant. No seed needed — a fixed transform is legal precisely because a
  constant is not a codebook.
- **Open question, left open in the code:** whether an `EMPTY` cell emits a
  code at all. Emitting them makes empty space a first-class observation and
  costs a code per cell; withholding them makes "nothing there" mean nothing
  rather than something. Under onsets the cost is small either way.

---

## What is deliberately not here

- **Forgetting.** `_together` counts do not decay yet. Half-life is a real dial
  on `master` and unswept; adding it before anything walks would be a dial
  nobody set on purpose.
- **Prediction.** Needed for the ranking step and for deciding when to ask
  rather than watch. Not until a chain causes something.
- **The distributed rendezvous.** Interface now, `Local` implementation now,
  bucket owners when a second machine exists. Building the hard one before it
  has a caller is how `master` grew three dead modules.
