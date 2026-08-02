# Method-level design

What every piece is and what every method does, in words. No bodies, no
implementations — this is the mental model, and the code is meant to match it
exactly. If they ever disagree, this file is wrong and gets fixed.

**Status: every type exists. `Code`, `Node`, `LiveSet`, `Snake`,
`SnakeQuantizer`, `Thought`, `Ring`, `HybridBus`, `Cluster`,
`LocalRendezvous`, `InputMachine` and `OutputMachine` are all implemented and
tested.** The `CS0169` progress bar is at **0**, down from 27 — every field has
a body behind it. What remains is the end-to-end snake run.

**147 tests pass, and sixty-nine mutations have been run to confirm they bite.**

**A CHAIN HAS CAUSED A MOVE.** On a 200-step budget with seed 1 the snake took
5 steps before dying, and **2 of them were chosen by a chain that reached an
action code**; the blind control, with the one wire cut that lets an action
into the occasion, chose 0 of 3. That is the first time in this project —
including `master` — that a chain of reasoning has caused anything.

**And the flood does not scale.** Those 5 steps halted 275,280 routes at the
horizon on a graph of 13 nodes. See open fork 8.

**And the chain beats random but not the board's geometry.** Over 200 seeds:
chain 6.575 mean steps, random 3.990, repeat-last-action 6.250. Chain over
random is about five standard errors; chain over repeat is under one. See open
fork 10, including the 30-seed reading that got this backwards.
A test has proved nothing until it has been seen to fail for the right reason.

**Five mutations have SURVIVED across the project, and all five are recorded
rather than hidden** — a surviving mutation marks a vacuous region of the test
set, and pretending otherwise is worse than the gap. Three were then closed by
better tests; two are kept and labelled.

| Mutation | Caught by |
|---|---|
| edge weight ignores the partner's marginal | the distractor test |
| `Best` pricing charges nothing | budget-never-rises |
| the cycle check is removed | the revisit test |
| persistence resets a start time | the duration test |
| running out of energy resets the run | ends-rather-than-resets |
| the view is board-absolute, not head-centred | offsets-from-the-head |
| food restores no energy | eating-restores |
| the offset is dropped from a code | three quantiser tests |
| the contents are dropped from a code | different-contents |
| `Sum` degrades to `Max` | sum-gathers-evidence |
| the chain kept is the last, not the strongest | strongest-chain |
| the live count ignores deaths | thought-is-over |
| another broadcast's accounting is accepted | broadcast-refused |
| release keeps the state | releasing-drops-state |
| ties ignore chain length | tie-breaks-on-shorter |
| the ring becomes a modulo | only-its-own-codes-move |
| the splitmix finaliser is removed | adjacent-codes-do-not-land-together |
| `Leave` does nothing | departed-codes-do-move |
| the seed is dropped from cluster placement | a-different-seed-places-codes |
| delivery becomes synchronous | sending-returns-before-the-receiver |
| leaving fires no death | leaving-fires-a-death |
| envelopes go to whoever subscribed first | envelope-reaches-the-cluster |
| faults are swallowed | receiver-that-throws-surfaces |
| `WhenQuiet` ignores work in flight | quiet-waits-for-a-delivery |
| an unknown address is dropped | nothing-local-is-a-bug-not-a-drop |
| a stale handle evicts its successor | stale-handle-does-not-evict |
| one envelope per message rather than per destination | partners-in-one-cluster |
| every partner is sent to the local cluster | partners-across-clusters |
| reports are merged across thoughts | two-thoughts-reported-separately |
| arrivals are never reported | arrivals-come-back |
| nodes are not created on first mention | node-comes-into-existence |
| the row is written without its lock | concurrent-deliveries-do-not-lose-counts |
| the node's lock is held across the weighing | partners-can-fire-at-once (deadlocks) |
| only onsets note the occasion | shared-count-never-exceeds-marginal |
| silence is not silent | occasion-with-no-onset-writes-nothing |
| only one direction of a pair is written | onset-joins-in-both-directions |
| live-to-live pairs are written | both-already-live-gain-nothing |
| two onsets in a frame are counted twice | two-onsets-are-one-coincidence |
| onsets never join the live set | onset-joins-with-everything-live |
| silence still starts a thought | frame-that-changed-nothing |
| learning is skipped | onset-writes-counts-and-starts-a-thought |
| onsets are also reported as already live | what-just-started-is-not-live |
| only the first origin per cluster is sent | origins-cost-one-envelope |
| accounting is folded before arrivals | thought-walks-to-what-was-learned |
| narrowing ignores the machine's codes | arrival-narrows |
| nothing reached still returns a code | nothing-reached-is-a-real-answer |

**Survived, on `Ring`:**

| Mutation | Why it survives |
|---|---|
| the seed is dropped from the code hash | **Fixed.** The seed was folded into *both* the code hash and cluster placement, and each covered for the other — removing either alone changed no answer. One implementation per behaviour, so it now lives on cluster placement alone, where a mutation does bite. |
| the address tie-break is removed | Only reachable on a 64-bit collision between two cluster points, which no test produces. Kept because `List.Sort` is unstable, so a collision would otherwise let insertion order reach the answer. **A claim about improbability, not about tested behaviour.** |
| the `Join` idempotence guard is removed | Joining twice re-adds the same points at the same positions, so no lookup changes its answer. The guard saves memory, not correctness, and no test can see it. |

**Survived on `HybridBus`, then closed:**

| Mutation | Why it survived, and what fixed it |
|---|---|
| routing ignores the address and takes the first subscriber | The test addressed the cluster that happened to be subscribed **first**, so "always pick the first" was indistinguishable from correct routing. **Fixed** by addressing the second subscriber. |
| the handle's double-dispose guard is removed | `Leave` already refuses to remove an address that is not there, so a plain double dispose fires one death either way. The guard is only load-bearing when an address **rejoins** — a previous life's handle must not evict its successor, which C3 makes an ordinary case. **Fixed** by testing that. |

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
- **`DeliverAsync(envelope)`** — the economy. Unpacks the many messages in the
  envelope, hands each to its node's `Fire`, collects every outgoing message,
  **regroups them by owning cluster**, and sends one envelope per destination.
  A node forking to 200 partners across 12 clusters produces 12 sends. Every
  hop that stays inside this cluster never touches the wire at all. Measured by
  counting sends, because a test that only checked what *arrived* could not
  tell one envelope of three messages from three envelopes of one.
- **Reports are keyed by machine AND broadcast.** One envelope can carry
  messages from more than one thought, and merging their accounting is exactly
  what the broadcast id exists to prevent.
- **`Admit` does not check ownership, deliberately.** A message addressed here
  under a ring view that has since moved on would be refused, and refusing
  loses the count where accepting keeps it. **The consequence is recorded
  rather than prevented**: while views disagree, two clusters can each hold a
  partial row for one code and nothing merges them. That is the lost-count
  scale of error C2 already admits, not a corruption.

### `LocalMarginals`

**Fork 2, now load-bearing.** `Node.Fire` needs the *partner's* marginal, and
partners live in other clusters. This reads them straight out of whatever
clusters happen to be in this process — **a C1 violation, named and kept in one
place so it cannot be mistaken for a solved problem.** It works exactly as far
as one process and no further: a second machine's nodes are simply not here,
every edge pointing at one would weigh zero, and no route would ever leave.

### `Node` is thread-safe, and the lock is never held across a weighing

Two envelopes can be delivered to one cluster at once, so a node can be fired
by two thoughts at once. `Fire` **snapshots the row and releases before it
weighs anything** — weighing reads partners' nodes, and holding this node's
lock while doing that deadlocks against a partner firing back. Edges are
mutual, so that is an ordinary case rather than a corner. The mutation that
holds the lock across the weighing hangs the test until its timeout.

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
- **`replicas`** is a constructor argument with no default: how many points
  each cluster occupies on the ring, and **the dial that decides how evenly
  load falls**. Measured over 8 clusters and 20,000 codes against an even
  2,500 — `16`: 2035–3448, `64`: 1837–3152, `256`: 2230–2883, `1024`:
  2449–2617. So the dial is real, and 64 is not a good value for it.
- **Only the departed cluster's codes move** when one leaves, and everything
  that moves when one arrives moves *to* it — both asserted, and they are the
  whole reason for a ring rather than a modulo.

**Views differ between machines while membership is changing, and that is
allowed.** A misrouted message is a lost count, not a corruption — the
statistics are counts over many occasions.

### `IReceiveEnvelopes` / `IReceiveReports`

What the bus actually hands things to. **The bus takes these rather than a
`Cluster`, so it does not know what a cluster is** — transport and graph stay
separable, and the bus is testable without a single node existing. `Cluster`
implements the first; a machine implements the second.

### `IBus`

- **`Subscribe(cluster)`**, **`Subscribe(machine)`** — becomes reachable.
  Returns a handle; disposing it leaves the bus. **A handle from a previous
  life cannot evict the subscriber that replaced it** — C3 says a cluster
  vanishing is normal, so one returning under the same address is normal too.
- **`SendAsync(address, envelope)`** — the thinking path, outbound.
  **Returns before delivery happens.** A sender never waits on a receiver.
- **`SendAsync(address, report)`** — the thinking path, back to the origin.
- **`Deaths`** — fires when a **cluster** leaves. *Cluster* granularity rather
  than machine, because a route is stranded by the departure of whatever holds
  its next node, and a machine leaving is every one of its clusters leaving.

**`SendAsync(machine, occasion)` was removed**: the learning path has no
transport yet, because `LocalRendezvous` writes directly and the distributed
one is unbuilt. It comes back with a caller.

### `Report`

Everything one cluster owes one machine for one broadcast, batched into a
single send — the return path's counterpart to `Envelope`.

### `HybridBus`

**Only the local half exists.** There is no second machine, so there is no
wire. `_peers` and the seed are gone from this class — a field nothing writes
is dead state wearing the appearance of a feature, and the seed belongs to
`Ring` and the quantisers that actually use it.

- **`_clusters` / `_machines`** — everything in this process. A send to one of
  these is a direct call dispatched to the thread pool: **the sender returns
  before the receiver has finished**, which is what makes a fan-out parallel
  rather than a queue. Asserted two ways — a send returning while the receiver
  is still inside `DeliverAsync`, and two receivers each waiting for the other
  to start, which serial delivery cannot satisfy.
- **An address that is not local throws.** With no wire, an unknown address can
  only be a routing bug, and a silent drop would be indistinguishable from the
  ordinary C2 message loss it is not. When the wire lands, that same case
  becomes a lost message.
- **`Faults`** — a delivery that threw. A send that returns before delivery has
  no other way to report failure, and swallowing is how a thing turns out never
  to have been wired up.
- **`WhenQuiet()`** — completes when nothing is in flight. **Not a C1 violation
  and not a barrier the design relies on**: it observes one process's own
  dispatch queue, no distributed agreement is involved, and nothing in the
  thinking loop waits on it. It exists so a test or a harness can ask whether
  the dust has settled without a sleep.

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

### `Occasion` and `IRendezvous`

An `Occasion` is one moment's change: **what started**, and **what was already
there**. A frame's onsets are one occasion, not one each — they came from a
single observation, and splitting them would count a pair of simultaneous
onsets twice.

- **`JoinAsync(occasion)`** — make sure everything in it ends up in the others'
  rows.

**What a join writes, and the reasoning that fixes it:**

- **Everything present notes the occasion — including what was already live and
  did not itself start.** This looked optional and is not. `seen` is the
  denominator of every edge weight, so a code present through many events that
  noted none of them would carry a tiny marginal against a large shared count
  and score **above 1.0** — turning the ever-present background into the
  strongest partner in the graph, the exact failure the forward weighting
  exists to prevent. Noting keeps `together(x, y) <= seen(y)`, which is
  asserted over a 400-step random run.
- **Onset-to-everything, never live-to-live.** Two codes that were both already
  there did not just coincide; they coincided whenever they started and that
  was counted then. Incrementing them again on every unrelated onset would
  inflate precisely the stable background the weighting has to refuse.
- **Two onsets in one frame are one coincidence, not two.**
- **Both directions are written, each by its own node**, because a node holding
  the other's row would be keeping data it does not own.

### `LocalClusters`

Every cluster in this process, and how to reach the node for a code — shared by
`LocalMarginals` and `LocalRendezvous` so there is one rule, not two. **A code
whose ring owner is not in this process throws rather than dropping the
write**: with no wire that can only be a wiring error, and a silent drop would
look exactly like the ordinary count loss it is not.

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
- **`Balanced()`** — whether `origins + splits - deaths == live`. **In one
  process this catches a slip, not a network fault** — the live count is moved
  by the same call that moves splits and deaths, so it holds by construction
  unless those two paths diverge. Across a network it cannot hold at all, since
  C2 loses reports and one lost death leaves the count above zero forever. That
  is why nothing waits on it.
- **`Receive(accounting)` refuses another broadcast's report.** Mixing two
  thoughts' death counts is exactly what the broadcast id exists to prevent.
- **Ties in `Best` break on the shorter chain**, then on the endpoint so the
  order is deterministic. That is the agreed brevity rule and it costs nothing
  here, but it only ever fires on an *exact* score tie, so it is **not** an
  implementation of brevity as a ranking principle.
- **A late arrival for a released thought is dropped, not refused.** C2 says
  late is normal and there is nothing left for it to refine.
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

### `SnakeSettings`

Every constant named and **none defaulted** — width, height, starting energy,
energy per step, energy per fruit. A constant that never changes looks like the
background, so requiring each one is how a number gets set on purpose.
**`Sight` is `int?` and `null` means the whole board, still centred** — the two
are arms of one experiment, not a feature and its disabled state.

### `Snake`

- **`Step(action)`** — advances one tick. **A dead run does not step**, it
  throws. Collision is checked before the tail vacates, so reversing into the
  neck is fatal; note that a length-3 snake cannot self-intersect any other
  way, because the tail leaves a cell in the same step the head could reach it.
- **`Length`** — grows by one per fruit.
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
