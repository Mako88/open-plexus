# Method-level design

What every piece is and what every method does, in words. No bodies — this is
the mental model, and the code is meant to match it exactly. **If they ever
disagree, this file is wrong and gets fixed.**

**Status, 2026-08-02: everything described here is built and tested.** 136
tests, 0 unimplemented fields, 23 source files, ~3,100 lines. The loop runs end
to end: a frame becomes codes, onsets form connections, a broadcast walks the
graph, chains come back carrying their reasoning, and an action gets taken.

**A chain causes moves.** On the arm in use, 200 seeds: the chain survives 92.85
mean steps against random's 37.41 — about twelve standard errors — and takes 40
fruit against random's 25. It does **not** beat repeating one turn on survival,
because repeating a turn is a circle; see fork 10.

**No single run is evidence.** Delivery is concurrent, so a fixed seed does not
reproduce exactly — measured over 20 repeats on three seeds, one varied in its
trajectory. That is C2 rather than a defect. Every number here is over seeds,
with a spread.

---

## Project structure

```
src/OpenPlexus/
  Codes/        what an identity is
  Graph/        Node, Cluster — the things that hold counts
  Bus/          the hybrid bus and the ring
  Learning/     onsets, offsets, and the join that forms connections
  Thinking/     messages, chains, thoughts
  Machines/     input and output, the world boundary
  Worlds/       snake
tests/OpenPlexus.Tests/
```

**One project and folders, not seven assemblies.** A project boundary should be
earned by a dependency rule worth enforcing; folders move for free. The rule
worth defending is that `Graph/` and `Codes/` touch no I/O and no bus, so a
result measured against them cannot be explained by transport.

---

## `Codes/`

### `Code`

A quantised fragment of one observation from one modality. Several fire for the
same thing. **Never a concept** — a concept is what you reach by walking, and
nobody holds one.

- **`Modality`** — which front end produced it. Two front ends never collide.
- **`Value`** — the bits.
- **`Prefix(k)`** — the top `k` bits. Unused today; it is what fork 3's locality
  arm would hash.
- **`CompareTo`** — modality then value. Exists so anything iterating codes can
  do so deterministically; a dictionary's order is not stable across runs.

### `IQuantizer<TObservation>`

Turns one raw observation into the codes present in it.

**The same input produces the same codes on every machine, forever** — the
red-ball property. That is why a quantiser is a fixed transform and never
fitted: two quantisers fitted on different samples agree about under 0.12 of
items, and no amount of walking recovers that.

---

## `Graph/`

### `WalkSettings`

The swept dials. **Identical on every node**, or the same route is priced
differently depending where it stands.

- **`Stamina`** — what a route starts with, **in perfect hops**. A step costs
  `1/weight`, a weight cannot exceed 1.0, so a hop costs at least 1 and a budget
  of *B* buys at most *B* steps. That is what bounds the walk, and it makes the
  number mean something.
- **`Value`** — `Strength` or `Lift`. Lift divides the path strength by the
  endpoint's own prevalence, so confident *and* landing somewhere rare scores
  high. C1-legal where PPMI is not, because the global occasion total is the
  same for every candidate and cancels in a ranking. **Untried here.**
- **`Accumulate`** — `Sum` or `Max` over the routes reaching one endpoint.
- **`Horizon`** — the longest chain a route may carry. **A backstop that has not
  fired since the cost became inverse** — zero halts over 200 seeds at horizon
  50 with stamina 4. Kept because an unbounded walk is the failure that takes
  the process with it, and every route it kills is counted so a run that hit it
  cannot look like one that finished.

### `Node`

One code and its own row of counts. Holds edges, holds no address, knows nothing
about the network — no list of other nodes, no view of the graph, no total, no
shared clock. **That is C1 holding, in one class.**

**A connection is a count.** There is no edge object and no connect operation:
an entry going from absent to 1 *is* the connection forming.

- **`Code`**, **`Seen`** — its identity and its own marginal.
- **`Note()`** — "I fired on this occasion." Adds one to the marginal.
- **`Observe(other)`** — "that code fired with me." Adds one to that partner's
  entry. **Writes only this node's row**; the partner writes its own, because a
  node holding both directions would be keeping data it does not own.
- **`Together(other)`**, **`Partners()`** — reads.
- **`Fire(message)`** — the whole of thinking, and **it takes only the
  message**. There is no way to hand a node another node's data, which is what
  makes the C1 claim structural rather than asserted. In order:
  1. Snapshot the row under the lock and release. Two thoughts can fire one
     node at once.
  2. **Weigh the edge it arrived on**: the sender put its own
     `together(sender, me)` in the message; divide by this node's own marginal.
  3. **Charge `1/weight` for that hop.** Die if it cannot pay.
  4. Multiply the carried strength by the weight; that is the arrival.
  5. **Refuse the fan-out if the budget is 1 or less** — no hop can cost less
     than 1, so nothing is affordable. The one prune a sender can still do, and
     it needs nothing from anyone.
  6. Otherwise send one message per partner not already in the chain, each
     carrying this node's own count for that partner.
  7. Report `k-1` splits, or one death if nothing survived.

  **It returns what should be sent rather than sending it**, which is why a node
  is testable with no bus, no cluster and no network.

### `Cluster`

A set of nodes and an address. **What subscribes to the bus** — individual nodes
on the wire would be tens of thousands of tiny messages. It decides nothing
about what fires.

- **`Holds(code)`** — whether the ring says this cluster owns it.
- **`Admit(code)`** — nodes come into existence on first mention. **Ownership is
  not checked**: a message addressed here under a ring view that has moved on
  would be refused, and refusing loses the count where accepting keeps it. The
  consequence is recorded rather than prevented — while views disagree two
  clusters can hold partial rows for one code and nothing merges them, which is
  the lost-count scale of error C2 already admits.
- **`DeliverAsync(envelope)`** — the economy. Fires each named node, collects
  every outgoing message, **regroups by owning cluster**, and sends one envelope
  per destination. Wire cost scales with distinct clusters reached, never with
  nodes. Then reports back, keyed by machine **and** broadcast, because one
  envelope can carry more than one thought.
  - **On a broadcast it fires only nodes it already holds** and books the whole
    envelope as **one pending unit**: the unit forks into however many codes
    this cluster holds, or dies if it holds none. **Every cluster replies,
    including with nothing**, because silence is otherwise indistinguishable
    from a route still walking.
  - **Sends onward before reporting, and that order is load-bearing.** A
    delivery that sends before it finishes is what stops the bus going quiet
    mid-thought. Reporting first was measured and destabilised whole runs.

### `LocalClusters`

Every cluster in this process and how to reach the node for a code — shared by
the rendezvous so there is one rule, not two. **A code whose ring owner is not
in this process throws rather than dropping the write**: with no wire that can
only be a wiring error.

---

## `Bus/`

### `Ring`

Which cluster owns which code. **Computed locally, agreed globally, no directory
and nobody to ask** — every machine gets the same answer from the code and the
shared seed, which is what lets a machine join a network it has never spoken to
and route correctly immediately.

- **`OwnerOf(code)`**, **`Join`**, **`Leave`**, **`Clusters`**.
- **`replicas`** is a required argument: how many points each cluster occupies.
  Measured over 8 clusters and 20,000 codes against an even 2,500 — `16`:
  2035–3448, `64`: 1837–3152, `256`: 2230–2883, `1024`: 2449–2617.
- **Views may differ while membership changes.** A misrouted message is a lost
  count, not a corruption.
- **`string.GetHashCode` is refused in a comment where it would be natural** —
  it is randomised per process, so the ring would place a code differently in
  every process and the one property this class cannot lose would fail silently.

### `IBus` / `HybridBus`

- **`Subscribe(cluster)` / `Subscribe(machine)`** — disposing the handle leaves,
  and **leaving is not silent**. A handle from a previous life cannot evict the
  subscriber that replaced it.
- **`SendAsync(cluster, envelope)`** — **returns before delivery happens**. A
  sender never waits on a receiver, which is what makes a fan-out parallel
  rather than a queue.
- **`BroadcastAsync(envelope)`** — to every cluster at once. **Returns who it
  went to**, because under a broadcast the sender cannot work that out from the
  ring and the whole point is that it needed no address.
- **`SendAsync(machine, report)`** — the return path.
- **`Deaths`** — fires when a **cluster** leaves. Cluster granularity, because a
  route is stranded by the departure of whatever holds its next node.
- **`Faults`** — a delivery that threw. A send that returns before delivery has
  no other way to report failure, and swallowing is how a thing turns out never
  to have been wired up.
- **`WhenQuiet()`** — completes when nothing is in flight. Not a C1 violation
  and nothing in the thinking loop waits on it; it exists so a harness can ask
  whether the dust settled without a sleep.
- **`Messages`** — every message the bus carried. What a real network would have
  had to send.
- **Only the local half exists.** An address that is not local **throws**: with
  no wire that can only be a routing bug, and a silent drop would be
  indistinguishable from ordinary C2 loss.

---

## `Learning/`

The path that **forms** connections. Everything in `Thinking/` uses them.

### `LiveSet`

What is currently on. **This is what makes a continuous stream discrete without
sampling it.** Sampling on a tick counts a persistent code every tick, so it
co-occurs with everything that happens while it is there — that is the
ever-present hub the weighting exists to refuse, manufactured on purpose.

- **`Update(present, now)`** — returns what **started** and what **stopped**.
  Everything that persists produces nothing. **A persisting code keeps its
  original start time**; refreshing it would silently make every duration zero.

### `Occasion` / `IRendezvous` / `LocalRendezvous`

An `Occasion` is one moment's change: **what started**, and **what was already
there**. A frame's onsets are one occasion, not one each.

**What a join writes:**

- **Everything present notes the occasion — including what was already live.**
  This looked optional and is not: `seen` is the denominator of every edge
  weight, so a persistent code that noted nothing would carry a tiny marginal
  against a large shared count and score **above 1.0**, turning the background
  into the strongest partner in the graph. Noting keeps
  `together(x,y) <= seen(y)`, asserted over a 400-step random run.
- **Onset-to-everything, never live-to-live.** Two codes both already present
  coincided when they started and that was counted then.
- **Two onsets in one frame are one coincidence.**
- **Both directions written, each by its own node.**

`LocalRendezvous` writes directly because every cluster is in one process. **It
does not test the hard part**, which is fork 1.

---

## `Thinking/`

- **`BroadcastId.New()`** — a Guid, not a counter, because a shared sequence
  would need every machine to agree what comes next.
- **`Message`** — `Broadcast`, `ReturnTo`, `To`, `Held`, `Chain`, `Carried`,
  and **`Together`**: the sender's own count for the addressee, which is the
  half of the edge weight the receiver cannot know. `Chain` is the cycle check
  and the explanation in one field, carried for free.
- **`Envelope`** — many messages for one cluster. **`Everywhere`** marks a
  broadcast, and **a broadcast never creates a node**: a routed message brings a
  code into existence on arrival, but a question put to everyone must not put
  every code on every cluster.
- **`Arrival`** — endpoint, summed score, the **strongest single** chain, and
  how many routes arrived. Recorded at **every node a route passes through**,
  not only where it stops.
- **`Accounting`** — splits, deaths, and **halts** counted separately, because
  reporting a horizon kill as an ordinary death would hide the constant.
- **`Fired`** — what a node hands back: outgoing, the arrival, the accounting.
- **`Report`** — what a cluster owes one machine for one broadcast: `From`,
  arrivals, **`Handled`** and **`SentInto`**. Those last two are John's design
  for fork 5 — a node knows which cluster it is in, so it reports not only that
  it forked but **where the forks went**.
- **`Thought`** — one broadcast, on the machine that started it. Never moves.
  - **`Receive(report)`** — arrivals first, accounting last, because the
    accounting can settle it.
  - **`SentInto` / `InFlightTo` / `Lost(cluster)`** — the per-cluster live
    count. **A departure is exact rather than a question**: the routes heading
    into a dead cluster are written off as deaths and the thought settles.
    **A negative count is allowed and clamping it was a bug** — reports arrive
    out of order, and clamping threw that away permanently, wrong on 100 of 256
    real thoughts.
  - **`Best(n)`** — readable at any time. Ties break on the shorter chain.
  - **`BestAmong(codes, n)`** — **arrival narrows**. Not `Best` then filter: the
    top *n* overall can contain none of these codes.
  - **`Balanced()`** — **not a tautology.** The live count comes from splits and
    deaths; the in-flight counts come from the routing named in each report. Two
    independent quantities agreeing is a real check, and it runs on every real
    thought.

---

## `Machines/`

### `InputMachine<TFrame>`

Holds an address, holds no edges, is in no walk — which is why an arbitrary
sensor can attach without the graph knowing what it is.

- **`ObserveAsync(frame, now)`** — quantise; diff for onsets; **learn** by
  joining the onsets with what was already live; **think** by broadcasting from
  the onsets. Persistence produces neither. **Learning happens before thinking**,
  because C4 forbids a run that stops so there is no "before training".
- **`ThinkAsync(origins)`** — **broadcasts, and never consults the ring.** An
  origin has no address by nature: for *what is this thing I am sensing* you
  cannot route, not knowing what you are looking for. Seeds one pending unit per
  cluster reached.
- **`DeliverAsync(report)`** — folds it in. **Settling is not releasing**, and
  getting that wrong destroyed the answer at the moment it became final.
- **`OnDeath(cluster)`** — every thought writes off the routes heading there.

### `OutputMachine`

- **`Choose(thought)`** — **arrival narrows, then rank.** Candidates are exactly
  the chains that reached this machine's codes; among those the best score wins.
  **Null when nothing arrived** — the only honest answer for a situation nothing
  was written about.
- **`Explain(thought)`** — the winning chain as well as the code.

**The agreed shape is three steps — arrival narrows, prediction ranks, brevity
breaks ties — and only the first is built.** Prediction ranking needs a
predictor that does not exist, and inventing an internal score in its place is
the move this design refuses.

---

## `Worlds/`

### `Snake`

- **`Step(action)`** / **`Steer(turn)`** / **`Absolute(turn)`** — a turn is
  `Ahead`, `Left` or `Right`. **Back does not exist**, so reversing into the
  neck is not an action rather than a fatal one.
- **`View()`** — **head-centred and rotated with the heading.** Centring made
  the same situation in two places one observation; rotation extends that to two
  orientations. Measured: 51.3 mean steps against 6.5 unrotated, and new codes
  per step falling from 0.98 to 0.19.
- **`Energy`** — depletes, food restores, running out **ends** the run rather
  than resetting. **Nothing declares food good**; a policy that does not eat
  gets fewer steps of experience.

### `SnakeQuantizer` / `SnakeSense`

- One code per **non-empty** visible cell, carrying its offset and contents.
  **Empty cells emit nothing** — an occasion is a clique, so codes per frame set
  how dense the graph is: 46,536 routes halted with them against 6 without.
- **One-hot over contents, not a hyperplane** — `EMPTY WALL BODY FOOD` are
  0 1 2 3 and a hyperplane over those would make wall-and-body near and
  empty-and-food far, which is arithmetic nobody meant.
- `SnakeSense` adds **what the body just did** as a second modality. **The
  action has to be in the occasion** or an action code has no edges and no chain
  can ever reach one.

### `Policy` / `SnakeRun`

- **`Policy`** — `Chain`, `Random`, `Repeat`. Controls that change **one**
  thing: the graph learns identically under all three and only the choice
  differs.
- **`PlayAsync(steps, blind, policy)`** — the loop, closed. **Falling back to a
  random move is counted, not hidden**, which makes random play the arm this is
  measured against rather than a silent default. **The run waits for each
  thought to settle before acting, and that is the harness, not the
  architecture** — snake is turn-based; a continuous world would act on the best
  chain arrived so far.
- **`RunResult`** — counts, not claims. `ChosenByChain`, `EchoedLast`,
  `Unbalanced`, `Halted`, `Messages`, `Ate`.

---

## The small types, so nothing here is unnamed

| | |
|---|---|
| `ArrivalValue` | `Strength` \| `Lift` — how an arrival is valued |
| `Accumulate` | `Sum` \| `Max` — how routes reaching one endpoint combine |
| `ClusterAddress`, `MachineAddress` | **Machines carry addresses; nodes do not** |
| `IReceiveEnvelopes`, `IReceiveReports` | What the bus hands things to, so **it does not know what a cluster is** |
| `Changes` | What started and what stopped between two frames, and `Quiet` |
| `Routed` | One cluster and how many routes went into it |
| `Cell` | `Empty` \| `Wall` \| `Body` \| `Food` |
| `SnakeAction` | The four board directions. Internal to the world now — a policy only ever names a `Turn` |
| `SnakeView`, `Seen` | The rotated window, and one cell of it |
| `SnakeSettings` | Width, height, sight, and the three energy numbers. **Every one required**, because a constant that never changes looks like the background |
| `SnakeFrame` | A view plus the code for what the body just did |

---

## What is deliberately absent

- **Temporal edges.** Every edge is within-moment: the rendezvous joins an
  onset with what was live in the *same* occasion, so nothing links moment *t*
  to moment *t+1*. **This is why prediction loses to a blind guess** — fork 19.
- **Prediction ranking chains.** `Foresight` scores predictions; nothing yet
  uses that score to rank one chain over another.
- **Forgetting.** Designed on `master` — half-life, aged on read, clocked on the
  node's own occasions — and unbuilt here. Fork 17.
- **The wire.** No second machine exists, so `IPeer` was deleted rather than
  left as an interface nothing implements.
- **The distributed rendezvous.** Fork 1.
