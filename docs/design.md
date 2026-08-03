# Method-level design

What every piece is and what every method does, in words. No bodies — this is
the mental model, and the code is meant to match it exactly. **If they ever
disagree, this file is wrong and gets fixed.**

**Status, 2026-08-02: everything described here is built and tested.** 28 source
files, 0 unimplemented fields. The loop runs end to end: a frame becomes codes,
onsets form connections, a broadcast walks the graph, chains come back carrying
their reasoning, and an action gets taken.

**The test count is deliberately not written here.** It rots between commits and
a stale number in the first paragraph is exactly the drift this file exists to
avoid — `DocsTests` checks what can be checked mechanically instead.

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
  Worlds/       snake, the senses world, and the binding world built to fail
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
- **`Reflect`** — `Reflection?`, **fork 21, and null is off.** Off is the
  control, so the mechanism can be measured against the same code with nothing
  else changed.

### `Reflection`

**A conclusion becomes an observation.** Borrowed from *Physarum polycephalum*,
which solves a maze with no brain and no global view: tubes carrying high flux
thicken and low-flux tubes atrophy, entirely locally — which is the only kind of
mechanism C1 permits. A route walked often enough is minted as a direct edge, so
the composition stops being re-derived from scratch every time.

- **`Threshold`** — the score an arrival must reach to be worth minting. **The
  nucleation threshold**, borrowed from crystallisation: a new phase forms only
  above a critical size, because below it the surface cost exceeds the volume
  gain. Without it every thought writes everything and the graph collapses into
  a complete one.
- **`Weight`** — what a concluded occasion counts against an observed one.
  **Below 1.0, or a belief reinforces itself as fast as evidence does.**
- **`Names`** — how many arrivals at most are written back.

**`Adaptive` was here and is gone — fork 23.** It scaled the write by
`Thought.Hunger` so compression would switch itself on where it helps and off
where it costs. **Measured twice and it does not work:** inverse cost exists to
exhaust the budget, so starvation is the normal way a route ends at every scale,
and the adaptive arm landed on top of fixed instead of on top of off.

**The risk is that the system learns its own hallucinations** — confirmation
bias, literally — which is why there are two dials rather than one and why the
default is off.

### `Node`

One code and its own row of counts. Holds edges, holds no address, knows nothing
about the network — no list of other nodes, no view of the graph, no total, no
shared clock. **That is C1 holding, in one class.**

**A connection is a count.** There is no edge object and no connect operation:
an entry going from absent to 1 *is* the connection forming.

- **`Code`**, **`Seen`** — its identity and its own marginal.
- **`Note(by = 1.0)`** — "I fired on this occasion." Adds `by` to the marginal.
- **`Observe(other, by = 1.0)`** — "that code fired with me." Adds `by` to that
  partner's entry. **Writes only this node's row**; the partner writes its own,
  because a node holding both directions would be keeping data it does not own.

  **A count became a weight for fork 21**, so a conclusion can be written down
  more lightly than an observation. `by` **must match between the `Observe` and
  the `Note` that go with it** — the same number is the numerator and the
  denominator of one edge, and a pair written heavier than it was noted scores
  above 1.0, which is the ever-present-partner failure the forward weighting
  exists to prevent. Non-positive is refused: it would move `together` without
  moving `seen` at all.
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
- **`BroadcastAsync(envelope, ct, ready)`** — to every cluster at once.
  **Returns who it went to**, because under a broadcast the sender cannot work
  that out from the ring and the whole point is that it needed no address.

  **`ready` is called with that list before any cluster is asked**, and an origin
  has to record its thought inside that window. Dispatch is `Task.Run`, so a
  cluster can reply before the call returns, and a report for an unknown
  broadcast is dropped. Measured: registering afterwards lost those reports
  entirely, leaving a thought that never settled and held no arrivals.
- **`SendAsync(machine, report)`** — the return path.
- **`Deaths`** — fires when a **cluster** leaves. Cluster granularity, because a
  route is stranded by the departure of whatever holds its next node.
- **`Faults`** — a delivery that threw. A send that returns before delivery has
  no other way to report failure, and swallowing is how a thing turns out never
  to have been wired up.
- **`WhenQuiet()`** — completes when nothing is in flight. Not a C1 violation
  and nothing in the thinking loop waits on it; it exists so a harness can ask
  whether the dust settled without a sleep.

  **IT IS NOT A "THE WALK FINISHED" SIGNAL, and reading it as one was a bug.**
  In-flight reaches zero in the gap between a cluster handling a message and
  dispatching what that message produced — fork 12. A harness that reads a
  thought there gets "nothing reached", which is indistinguishable in a score
  from a graph that genuinely had nothing to say. **`Thought.Settled` is the
  signal**, and where a harness still has to give up waiting, every expiry is
  counted rather than absorbed.
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

**A `Window` carries departed codes forward**, so a thing that stopped before
the next began can still be linked to it. **Those join ONE WAY — the past
records the future and not the reverse**, which is the whole of what makes an
edge temporal: a broadcast of what just happened can walk forward, and one of
what follows cannot walk back. Simultaneity stays symmetric, because nothing
came first. `Span = 0` is the old behaviour exactly, and is the default —
sweeping it 0 to 8 moved nothing measurable, see fork 19.

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
- **`Accounting`** — splits, deaths, **halts** and **starvation** counted
  separately. Reporting a horizon kill as an ordinary death would hide the
  constant; reporting a starved route as an ordinary one would hide **why** the
  walk stopped, which is the signal fork 21 regulates itself with.

  **Starved means the route ran out of budget rather than out of anywhere to
  go.** It costs nothing to know — the node already tells the two cases apart to
  decide whether to fan out — and nothing is consulted or shared to report it.
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
  - **`Starved` / `Hunger`** — how many deaths were routes that could not pay
    for the hop they were on, and that as a share of all deaths. **Measured, and
    it is INVERTED** — higher at the budget where compression hurts. Reported so
    nobody tries it again blind. Fork 23.
  - **`Thwarted`** — the share of *died strength* the budget killed, rather than
    the share of deaths. **John's correction, and it goes the right way at 5.1
    standard errors.** Strength decays multiplicatively, so a route cut off after
    one hop dies strong and one that petered out over four dies weak — same
    event, opposite meanings, and a count throws that away.

    **Still not wired to anything.** It swings 1.19× against an effect that runs
    0.18 to 0.83, so a linear scaling cannot gate compression and sharpening it
    would take an exponent nobody set.
  - **`Balanced()`** — **not a tautology.** The live count comes from splits and
    deaths; the in-flight counts come from the routing named in each report. Two
    independent quantities agreeing is a real check, and it runs on every real
    thought.

### `Consequence`

**FORK 18'S METRIC — what this project scores, by John's call.** Not survival,
which is disqualified: the arm that lives longest circles and eats nothing,
133.71 mean steps against the chain's 92.85 and two fruit against forty. Not
passive prediction either, which asks *what comes next* and can be answered well
by something with no idea it is present in the world at all.

**The question is "what will the world look like if I do X".** A world model
rather than a sequence model, and the difference is that it can be asked a
counterfactual.

- **`Settle(knowing, otherwise, blind, actual)`** — one prediction made with the
  **true** action in the question, one with a **different** action, one drawn
  without consulting the graph, all against what the world actually did.
- **`Knowing`**, **`Counterfactual`**, **`Blind`** — precision of each.
- **`Gap`** — `Knowing − Counterfactual`. **The number.**

**THE CONTROL IS THE SAME PREDICTION WITH A DIFFERENT ACTION IN IT**, and it is
what makes this measure understanding rather than familiarity. Same graph, same
budget, same walk, same narrowing, same number of codes named — only the action
inside the question differs.

- If naming the true action predicts better, the graph holds something about
  **its own effect on the world**.
- If the two score alike, it is predicting the next frame regardless of what it
  does — **exactly the thing that looks like understanding and is not**, and no
  accuracy number alone would separate them.

**Scored against everything present rather than only what changed.** Whatever is
predictable without knowing the action is equally predictable in both arms, so it
cancels in the gap; there is nothing to strip out by hand.

**A step where either arm named nothing is not counted**, rather than counted as
a miss — otherwise the gap would depend on how often each arm stayed silent, and
silence is a property of the budget rather than the model.

### `Budget` / `Budgeting`

**Fork 24 — a machine hunting for its own stamina.** Holds no graph, no bus and
no codes: it counts its own outcomes and nothing more.

**C1-legal by construction.** Stamina is set by the *origin* when it builds a
message and merely spent by nodes, so a machine varying its own budget touches
no node state and reads nobody else's data.

- **`Next()`** — the budget for the next question. Rotates through **half,
  current, double**, so both directions are probed rather than drifted through.
  Never below 1.0: a hop costs at least 1, so anything less buys nothing and the
  downward probe would measure the same nothing forever.
- **`Reached(anything)`** — whether that question reached **what it was narrowing
  to**, not merely reached somewhere. The caller knows what it was looking for
  and this class deliberately does not.
- **`Stamina`**, **`Moves`** — where it is and how often it has shifted. `Moves`
  is the wiring check: a controller that never moved and one that converged
  instantly look identical otherwise.
- **`Window`, `Worth`, `Most`** — samples per candidate, how much less silence a
  bigger budget must buy, and the ceiling. **Both dials are world-independent**,
  which is the argument for the trade: stamina is not, and the only way anyone
  has found it is by sweeping.

**Climbing is tested before halving, and the order is load-bearing.** Measured
the other way round: just below the knee, the smaller and current probes are
equally hopeless, so halving reads as free and the controller retreats from the
budget the larger probe just showed to work. It oscillated below the knee
forever.

**A total failure is treated as unambiguous.** When no candidate reaches
anything, a hill-climb is blind — every option scores the same nothing — so it
climbs, and hands back to the ordinary rule the moment anything reaches.

**MEASURED, AND THE TARGET IS WRONG.** It converges from both directions (12.0
from a start of 2, 11.3 from 24), but the plateau it aims at is an artefact of
run length: at 300 moments stamina 8 ties 24, and at 1200 moments 24 wins by
seven standard errors. **Off by default** — see fork 24.

---

## `Machines/`

### `InputMachine<TFrame>`

Holds an address, holds no edges, is in no walk — which is why an arbitrary
sensor can attach without the graph knowing what it is.

- **`ObserveAsync(frame, now)`** — quantise; diff for onsets; **learn** by
  joining the onsets with what was already live; **think** by broadcasting from
  the onsets. Persistence produces neither. **Learning happens before thinking**,
  because C4 forbids a run that stops so there is no "before training".
- **`ThinkAsync(origins, stamina = null)`** — **broadcasts, and never consults
  the ring.** An origin has no address by nature: for *what is this thing I am
  sensing* you cannot route, not knowing what you are looking for. Seeds one
  pending unit per cluster reached.

  **The thought is recorded inside the bus's `ready` callback, before the first
  cluster is asked, and that order is load-bearing.** Dispatch is `Task.Run`, so
  a cluster can finish and report back before `BroadcastAsync` returns — and a
  report for an unknown broadcast is dropped by design. Registering afterwards
  silently lost those: the thought then never settled and held **no arrivals at
  all**, which downstream is indistinguishable from a graph that had nothing to
  say. Measured, and covered by a bus that replies inside the window.
- **`ReflectAsync(thought, now)`** — **fork 21: a conclusion becomes an
  observation.** Takes the arrivals above the nucleation threshold, drops any the
  walk started from, and joins them as an occasion at the discount weight.

  **The reached codes are the onsets and the origins are merely live**, which is
  not a detail: onsets pair with everything present, live never pairs with live,
  so the origins do not re-pair with each other. That coincidence was counted
  when it was observed, and counting it again on every thought would inflate
  exactly the association the walk set out from.

  Returns how many conclusions were written, and **zero when reflection is off** —
  which is what lets a run report say whether the mechanism was even running.
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

### `RunReport`

**Everything a snake run can say about itself, printed at the end of it.** The
answer to a failure this project keeps having: a number is swept, barely moves,
and much later it turns out something was never wired the way anyone thought.

- **`Nodes`**, **`Edges`**, **`Spread`**, **`ChainLengths`**, **`Silence`**,
  **`Deepest`**, **`NoveltyGap`**.
- **`Complaints`** — **the part that matters.** Every entry is a quantity that
  would be out of range only if something had come unwired, and a test reads the
  list on every run. **Adding a number here without a range says nothing; the
  range is the check.** The sharpest one is `Deepest < 2`: a walk that never
  leaves its origin is a one-hop lookup wearing the name of a flood, and nothing
  else in the report would show it.

### `Senses` / `SensesSettings`

**The second world, and it shares no code with snake.** No space, no movement,
no actions, no energy, nothing to lose, no time pressure. **If a finding holds
here too it was about the architecture; if it does not, it was about snake.**

An occasion shows either **sight with sound**, or **sound with touch**. **Sight
and touch are never shown together, not once** — enforced in `Moment()` rather
than left to a caller, and asserted over 5,000 moments before any result is read
from the world.

The question is: *given a sight, what does it feel like?* **A memoriser scores
exactly zero**, because the pair being asked about has never occurred. Getting
it right requires walking sight → sound → touch.

- **`Concepts`**, **`CodesPerSense`**, **`Noise`** — all required.
  `CodesPerSense` must exceed one, or identity is a lookup table and *a concept
  is what you reach by walking* does no work.
- **`Scrambled`** — **the control, and it tests the DATA rather than the code.**
  Each sense is paired with a random concept instead of the right one. Every
  mechanism runs identically; only the structure the world contains is
  destroyed. If accuracy survives it, it was never composition.

### `SensesRun` / `SensesResult`

The senses world wired to the graph, **scored prequentially** — a question is
asked, settled, and learning carries on, because C4 forbids a run that stops.

- **`RunAsync(moments, every)`** — shows moments and stops every *n* to ask.
  **Fork 21 reflects on what was observed and never on what was asked**, or the
  score would climb because the measurement had leaked into the training.
- **`AskAsync(concept)`** — broadcasts the sight codes, narrows with
  `BestOf(Touch, 1)`. **Waits on `Thought.Settled`, not on the bus** — see
  `WhenQuiet`.
- **`AskAsync(concept, votes)`** — **John's design**: the same question asked
  several times *at once*, with distinct broadcast ids, and the majority taken.
  A thought is already identified by its broadcast id, so concurrent thoughts
  about one question are not a special case — they are what the accounting was
  built for. One round trip rather than *n*.

  **It exists because the walk disagrees with itself.** An identical question
  does not always get an identical answer — **0.8833 agreement, measured** —
  because delivery is concurrent. Voting recovers what one walk drops: **0.9688
  → 0.9974 over 8 seeds, about 4.7 standard errors.**

  **This is C2 being paid for rather than complained about.** The constraint says
  messages are late, jittered and out of order; redundancy is the ordinary answer,
  and it costs queries rather than coordination.

  **Silence gets no vote** — a walk that reached nothing has no opinion, and
  counting it would let the quietest arm decide. Ties break on the code, so the
  answer does not depend on which thought finished first, which is the very thing
  being voted on.
- **`SensesResult`** — the same self-reporting `RunReport` gives snake, plus
  `Reflected` and `Reflecting` so a run says out loud whether fork 21 was even
  running. Its world-specific complaint is **`Deepest < 3`**: sight reaches touch
  only through sound, so a correct answer is a chain of length three and
  anything shallower is not the task.
- **`Unsettled`** — questions read before their walk finished. **Counted rather
  than absorbed**, because "nothing reached" and "not finished yet" are
  indistinguishable in a score. See fork 22.

### `Binding` / `BindingSettings` / `Scene`

**The third world, and the only one built in the expectation that it would
fail.** Two objects in a scene, each with a colour and a shape, and the question
is *which shape belongs to which colour*.

**An occasion is a SET of co-occurring codes**, so a red ball beside a blue box
and a blue ball beside a red box produce the identical set. The binding lives
nowhere in what the machine receives, and this world is the smallest honest
statement of that.

- **`Concepts`**, **`CodesPerAttribute`** — required. Several codes per
  attribute for the same reason `CodesPerSense` exists: one code apiece would
  make identity a lookup.
- **`Bound`** — **the control, and it runs the opposite way from
  `Scrambled`.** There the control destroys structure and is expected to fail;
  here it *adds* structure the counts can see and is expected to succeed. It
  exists because "scored at chance" and "the harness never measured anything"
  look identical from outside.
- **`Scene`** — `Codes` is what the world shows; `Colours` and `Shapes` are
  indexed **by object**, and that shared index *is* the binding. It is the one
  thing in the record that `Codes` does not contain.
- **`Next()`** — draws two distinct concepts and emits four codes **ordered by
  concept, never by object**. Ordering by object would smuggle the binding past
  the front door and every number here would be measuring the leak.
- **`Apart(seed, purpose)`** — mixes a seed rather than offsetting it. **A
  seeded `Random` in .NET normalises by magnitude**, so `new Random(~s)` *is*
  `new Random(s + 1)`, and consecutive seeds produce streams that agree with
  each other far more than chance allows. That understates every standard error
  taken across those seeds; see the trap in `architecture.md`.

### `BindingRun` / `BindingResult`

The binding world wired to the graph, scored prequentially, with the question
asked about **the scene just shown** — the binding is a fact about that scene
and about nothing else.

- **`RunAsync(moments, every, votes)`** — shows scenes and stops every *n* to
  ask which shape one of the objects had, alternating between the two so nothing
  rests on which was drawn first.
- **The question is asked with the queried object's colour and nothing else, and
  that is the finding arriving early.** There is no way here to say *this one*.
  An object can only be named by its attributes, and its attributes are what is
  being asked about — so broadcasting the whole scene as context would broadcast
  the answer's competitor on exactly equal terms. **The failure shows up before
  the answer: the question cannot be posed.**
- **`Echoed` / `Echo`** — how often the answer named the queried colour's *own*
  concept. **The mechanism rather than the score**, and it is what turns a null
  result into a description: a colour co-occurs with its own kind's shape in
  every scene it appears in, so the counts point there whichever object it
  belonged to. Measured at 92% in **both** arms.
- **`Swapped`** — how many of the *asked* scenes actually swapped. The fairness
  of the coin in the subsample that was scored, rather than in the long run.
- **`SawBoth` / `Forced`** — how often both candidates were in reach. **The
  check that stops this world scoring chance for the wrong reason:** a forced
  choice is only forced if both candidates were reached, and a weak edge is
  expensive under inverse cost. At stamina 4 only 16% of questions are forced; at
  12 it is 97%, which is why the headline is measured there.
- **`Complaints`** — load-bearing here more than anywhere else in the project.
  Every other world's headline is a number going *up*, where a disconnected dial
  shows up as a disappointment. **This world predicts a number that stays flat,
  and a broken harness produces exactly that.**

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

## The measuring tools, which live in the test project

**Not part of the system.** They measure it, so they sit with the tests rather
than in the library.

### The sweep harness — `Sweep` / `Measured`

**John's ask, 2026-08-02.** Every measurement here used to be a throwaway test
file that printed a line and was deleted, so the seed loop, the averaging and
the spread were rewritten each time — and the spread was usually what got
dropped.

- **`Sweep.ArmAsync(arm, seeds, run)`** — one arm across seeds 1..n.
- **`Sweep.AcrossAsync(seeds, arms)`** — several arms over the same seeds.
- **`Sweep.Table(arms)`** — a markdown table with sigma against the first arm,
  ready to paste into the architecture doc.
- **`Measured.Mean`, `.StdErr`, `.Separation(other)`** — **the spread is not
  optional, and that is the point of the type.** Every bare mean this project
  has published has had to be retracted or hedged: *chain loses to repeat* at 30
  seeds became *indistinguishable* at 200, and a fork-21 table went in with
  "spread not computed" written across it. A harness that cannot report a mean
  without its standard error cannot make that mistake again.
- **`Separation` returns 0 when neither arm has spread**, so two arms measured
  once each are never reported as different.

### The doc check — `DocsTests`

**John's ask: incremental doc updates miss things.** They do, and the failure is
specific — something gets built, the doc it belongs in is not touched, and later
nobody can tell whether the omission means *undocumented* or *deleted*. This
runs on every `dotnet test`, so a gap cannot outlive the commit that opened it.

- **Every public type appears in this file.** Found `Senses`, `SensesRun`,
  `SensesSettings`, `SensesResult` and `RunReport` missing on its first run —
  the whole second world and the run report.
- **Every heading names a type that exists** — the ghost-reference direction.
- **Every fork number the code cites is in the architecture index.** This is why
  forks are never renumbered, and now it is checked rather than remembered.
- **It throws rather than skipping if it cannot find `docs/`.** A check that
  passes silently when it could not read the thing it checks is worse than no
  check, because it reports green for a question it never asked.

---

## What is deliberately absent

- **Prediction ranking chains.** `Foresight` scores predictions; nothing yet
  uses that score to rank one chain over another. That is the middle tier of
  output selection and the reason prediction was built.
- **Forgetting.** Designed on `master` — half-life, aged on read, clocked on the
  node's own occasions — and unbuilt here. Fork 17.
- **The wire.** No second machine exists, so `IPeer` was deleted rather than
  left as an interface nothing implements.
- **The distributed rendezvous.** Fork 1.
