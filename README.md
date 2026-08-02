# Open Plexus

An attempt to build AGI that runs on distributed compute over the internet.

---

## Decisions

Every part the data flows through, and every option for it — including the dead
ones, because this is the file that gets read and an option that is not here will
be proposed again.

**✅ in use ⬜ untried ❌ ruled out 🚧 approved, not built**

Several options can be in use at once; ✅ is not exclusive. One line each. A ❌
says what killed it and, where it is known, what would bring it back —
refutations are conditional on their configuration, and two have become right
later.

---

**1. Input → surfaces.** How raw data becomes an id that can be counted.

- ✅ Random-hyperplane LSH, over features centred per item — *no training and no data. Two nodes send an identical input to the same surface every time, where k-means fitted on different samples of one stream agrees on under 0.12. The bit count is the grain dial.*
- ✅ Fixed transforms of one item — an ear-shaped filterbank, a cepstrum, per-item centring — *legal because a constant is not a codebook. They are what makes an angle mean anything: on raw waveforms every front end including random assignment scores the same. They improve the space rather than the allocation — k-means gains 0.05 from them and the hash gains 0.01.*
- ✅ Prediction survives the hash — *the front end costs something and not much: predicting the next observation scores 0.650 through `Hyperplanes` against 0.717 with exact-view identity, three seeds, shuffled control 0.000. Quantising loses information that matters, and less than it saves.*
- ✅ One-hot for a categorical cell — *`EMPTY WALL BODY FOOD` are 0 1 2 3 and a hyperplane over those numbers would make wall-and-body near and empty-and-food far, which is arithmetic nobody meant.*
- ⬜ SEVERAL coarse hashes per item instead of one fine one — *the first LEGAL repair for the hash's 0.44 purity deficit, which is decisive: at the hash's purity no walk clears chance on the senses graph and at k-means' both do. Four families of 6 bits, each alone 62 codes with 63 items behind every one, conjunction **0.9845** — never built, so nothing is starved. Data-free, so two nodes agree exactly.*
- ⬜ A codebook learned from co-occurrence — *removes the last borrowed component, but it must stay in sync as it learns, which C1 forbids.*
- ⬜ Per-node codebooks plus translation — *avoids agreement entirely, at the cost of unsupervised translation, which is harder than the goal.*
- ❌ ~~No discretisation — count raw similarity~~ — *every count stays 1, so no statistic can form.* **Revives if** counting is replaced by something that does not need recurrence.
- ❌ ~~Trained k-means quantiser~~ — *clustering by similarity is an identity assignment, which is the walk's job, and two nodes fitted on different samples agree about almost no item with nothing reporting it. It is still the better grouping, about twice the purity at a matched code count, and that gap is the price.* **Revives if** a per-node front end becomes acceptable.

**2. What gets stored.** Which co-occurrences are kept.

- ✅ Every count, nothing ever cut on the strength of a rule — *what leaves does so by fading or by memory pressure, never by a judgement that it was unimportant. That is the distinction from the two cutting rules below.*
- ❌ ~~Cut each surface's partners at the biggest score gap~~ — *refuses the ever-present distractor and evicts the word that names the concept.* **Revives if** something else supplies the refusal.
- ❌ ~~Cap how many partners are even considered~~ — *a constant nobody set on purpose, and it turned out to be doing the cutting.*
- ✅ Edges weaken over time, aged on read rather than by sweeping — *`CoOccurrence(half_life=...)`, John's design, off by default so every earlier result is reproducible. Each cell carries the tick it was aged to; a count is the sum over its observations of `retain ** (now - then)`, computed incrementally. Firing refreshes a node, so use IS the lifespan and no second mechanism is needed.*
- ✅ The clock is this node's own occasions — *not wall time and not a global total. A node nobody talks to must not forget because the rest of the world got busy, and a shared clock is the collective C1 forbids.*
- ✅ The archive keeps the EDGES, and reinstatement carries a boost — *`evict` hands back the marginal and the whole row decayed to now; `reinstate` puts them back multiplied by the boost. A node loaded back empty has been forgotten in the only sense that matters, and one restored at its old weight is the weakest thing again immediately — asserted both ways.*
- ❌ ~~Decay is not a loss of accuracy, only of memory~~ — *refuted the day it was written. Two partners built to identical raw counts — `together 50`, `seen 100` each — score **0.1111 against 0.8889** at a half-life of 100. An eightfold gap from timing alone.*
- ✅ Decay reweights toward recency, and that is the mechanism — *a ratio survives only where numerator and denominator share a history. Where they differ, recent evidence wins, which is what makes a recently used thing easier to reach. The half-life is a real dial and has to be swept.*
- ⬜ Evict from the archive as well — *the disk store moves the problem rather than solving it; a machine that never deletes still fills up, and nothing decides what leaves permanently.*
- ⬜ Forget by age or disuse, without the archive — *the version where eviction is deletion. Kept because it is what the above reduces to if archiving turns out to cost more than it returns.*

**3. Identity.** What makes several surfaces one thing.

- ✅ No id at all — a concept is what you reach by walking.
- ❌ ~~Freeze partners into groups by mutual agreement~~ — *a hard partition flips whole groups on a small score change, so past some point it gets worse with more data.* **Revives if** a task needs a yes/no answer to "are these the same".
- ❌ ~~Give every concept a global id~~ — *nobody can assign one without a coordinator.*
- ⬜ Identity at more than one grain — *dog and Labrador are the same walk at different resolutions, and nothing expresses "narrower than".*

**4. Retrieval.** How a question gets answered.

- ✅ Ranked walk, scored one-way from the asking side.
- ✅ Walk two steps, carrying the relation types along the path — *the only version that pays. Test endpoints are 0.0000 one hop apart in training and 0.7373 two hops apart, so one step cannot reach the answer. Evidence accumulates over every route reaching a candidate, which a thresholded rule lookup cannot do: `sum` beats `max` at every blend weight.*
- ✅ A blend weight that is per query rather than global — *built to test whether the losses were the queries where structure had nothing to say. They are not: +0.0131 against a global +0.0136, with slightly more losses. Kept because it cannot blow up — at full weight it holds 0.2379 where a global blend collapses to 0.1278, which is the form to use where there is no validation set, meaning every node.*
- 🚧 Broadcast flood — *input goes to every node; a node linked to something in the broadcast re-broadcasts and appends itself, so a route arrives carrying its whole chain of reasoning. Built as `openplexus/broadcast.py`. Not measured.*
- 🚧 Stamina in place of a floor — *a route carries a budget the edges it walks refuel, so a route that walked strong edges can afford a weak one where a floor would have cut it at the first. That is what stamina buys and a floor cannot express.*
- ❌ ~~Stamina removes a tuned constant~~ — *it does not. The starting budget has a scale and has to be swept exactly as a floor does: on the senses graph at 6 bits, `best` pricing reaches 0.3 audio codes at 0.002, 19.4 at 0.05 and everything at 0.25. One knob replaced another.*
- ❌ ~~Pricing a step at the node's MEAN edge weight~~ — *about half a node's edges are above its own mean, so a route taking above-mean steps gains forever. Measured unbounded at every budget from 0.002 to 0.25: all 44 audio codes reached, `gave_up` 1.00, agreement 0.1082 against a chance of 0.108.* **Revives if** the mean is replaced by a high quantile, which was untried.
- ✅ Pricing a step at the node's STRONGEST edge — *an opportunity cost: the step is charged what the best step there would have cost, so stamina never rises and only a route taking near-best edges keeps its budget. The one pricing measured to bound the walk.*
- ❌ ~~Many origins in place of edge kinds~~ — *broadcasting the image code AND the words from the same occasion scores **below chance on all three seeds** (0.1012, 0.0960, 0.1019 against 0.108), at 3–10× the cost. More origins made it monotonically worse.* **Revives if** an origin's stamina is scaled by how much it predicts.
- ⬜ Gate the ORIGINS, not only the edges — *the origins that hurt were the word nodes, which are hubs. `forward` decides which edges a route walks and says nothing about where a route starts, so a hub origin is ungated and floods.*
- ❌ ~~The broadcast flood as a cross-modal walk~~ — *from one origin, over a swept budget and three seeds: 0.1213, 0.1218, 0.1084 against a chance of 0.108. Chance is inside the spread, so this is a null and not a small win. `equivalence_classes` on the same graph reaches almost nothing, so the flood did not lose to something better.* **Revives if** the graph gets sparser or the origins get gated.
- ❌ ~~Many surfaces per input, each predicting only its own slice~~ — *nine overlapping 3×3 windows on snake, one predictor each. Asked whether the WHOLE next observation was foreseen it scores **0.510**, against a whole-view code's 0.650 and exact identity's 0.717, three seeds.* **Revives if** the columns can read each other.
- ⬜ A per-column score is not a whole-observation score — *the same run reads 0.847 if each window is scored on its own slice, because being right about eight of nine easy sub-tasks is not foreseeing the observation. Both are printed; only the strict one compares across arms.*
- ✅ Columns that read the one neighbour they are heading towards — *what a window lacks is what is about to enter it, and that comes from the direction of travel. Conditioning on one neighbour rather than eight recovers most of the loss: **0.592 / 0.542 / 0.726** against independent columns' 0.510 / 0.400 / 0.620, better on every seed, against a whole-view code's 0.650.*
- ❌ ~~Refuel on SURPRISE, or value the arrival by RARITY~~ — *both at chance on the senses graph, swept over grids that span their own per-step deficits. The second is John's "aha grounded in logic": fund on strength so every surviving route is supported, then rank the ones landing somewhere rare.* **Revives if** measured on a graph a walk can find anything in.
- ✅ Rarity-weighted ranking is C1-legal where `ppmi` is not — *PPMI divides by the global occasion total, which no node can know. That total is identical for every candidate, so it **cancels in a ranking** — `seen(endpoint)` alone is one node's own marginal.*
- ✅ A set statistic cannot see a ranking — *agreement over everything reached is blind to any reordering, so a valuation rule measured against it moves by nothing whatever it does. Scoring only the best few is what makes a score measurable at all.*
- ✅ A broadcast expresses preference as ECONOMICS, not as selection — *a sequential walk ranks its frontier and picks; a broadcast cannot rank anything globally, but it can make one kind of edge cheaper to keep walking. That is the general form, and it survives the particular measure above failing.*
- ⬜ Walk toward surprise rather than strength, some other way — *every walk here expands the strongest edge, so nothing it returns can be unexpected. A path unlikely a priori that composes confidently is where a new idea would live, and the economics form above did not find it.*
- ⬜ A surprise statistic one node can compute alone — *`grounding.ppmi` is that measure, and `federated` and `bucket_service` both refuse to serve it because it divides by a global occasion total. Walking on surprise needs a local one first.*
- ❌ ~~The typed flood, at either depth~~ — *matched on one query set through one scoring loop, `out/fb15k237-flood-matched.txt`: the capped two-step enumeration takes **+0.0159**, the flood's best is +0.0081 at depth 2 and +0.0052 at depth 3.* **Revives if** the reach at depth 3 is scored differently.
- ⬜ Score depth-3 routes some other way — *arrival RISES with depth, 0.3633 to 0.3833, while the margin falls. The routes reach the answer and the scoring cannot rank it, which is a different problem from not getting there.*
- ⬜ Walk four steps or more — *0.2597 of answers lie further than two, and nothing has been run past three. Cost is a fan-out to the fourth.*
- ⬜ Act on the world to disambiguate — *the only named escape from a confound that counting cannot separate.*
- ❌ ~~Average the two directions of an edge~~ — *a thing present everywhere scores 1.0 from its own side because that is true, and averaging lets it outrank real partners.*
- ❌ ~~Take the weaker direction~~ — *penalises exactly the hub edges worth keeping.*
- ❌ ~~Take the stronger direction~~ — *stops discriminating; it scored at the floor.*
- ❌ ~~Tune a damping exponent~~ — *five dials were tried and none was the axis.*
- ❌ ~~Walk further than one step without the types~~ — *reaches the answer and cannot rank it: 0.0082 at depth 3, every combination below the floor at every beam to 256, with an interior maximum at 16, so it is not under-searched. Two steps from a degree-37 entity is about 1,300 candidates and nothing says which one the question was about.* **Revives if** something other than types supplies that discrimination.

**5. The answer.** What a response actually is.

- ✅ A relation that was never stated but follows — *composition is novel output from a single query, no generation involved. Typed two-step paths on FB15k-237 clear a structureless floor by +0.0136 ± 0.0005, the margin published ComplEx holds over that same floor.*
- ✅ Report that margin as a dilution, both halves — *+0.0474 where a route reaches the answer, −0.0046 where none does, at 35% reached. The reached third is selected by the mechanism's own ability, so it says nothing about what more reach would buy.*
- ⬜ The ranked list itself, cut where the caller wants — *the walk already produces it; nothing extra is decided.*
- ⬜ A set, scored on exactness and completeness — *forces a commitment to a boundary rather than a hedge.*
- ⬜ Refuse when nothing was written there — *the only honest answer for a thing never seen, and the machinery exists.*
- ⬜ Generated one piece at a time — *the only way to produce a novel sequence, and nothing yet says when it stops.*
- ⬜ The answer to an analogy — *find where two parts of the map have the same shape, and read off the missing corner. A route shape is a first-class thing — `PathTypes` counts them — so two regions spanned by the same shapes is the same question one level up.*
- ⬜ A contradiction the map contains — *an output that was never an input, and nobody asked the question. `pathways.flood` already produces the raw material: it returns every route to an endpoint, so two routes composing to incompatible kinds is a contradiction, computable and currently discarded.*
- ⬜ A bridge between two regions that never co-occurred — *the thing that connects distant fields, aimed at deliberately rather than stumbled into.*
- ❌ ~~A fixed frame with slots to fill~~ — *a frame is a traversal with a schedule nobody supplied.* **Revives if** a domain genuinely supplies the frame.

**6. Output.** Turning an answer into something that leaves the system. Not
necessarily words — an action is an output, and so is a structure.

- 🚧 Any node is an input or an output; the MACHINES carry the addresses — *John's design, 2026-08-01. A machine broadcasts an input carrying the id of the output machine it wants, so completed chains and death reports come back addressed. Machines are not nodes, hold no edges and are in no walk — which is why an arbitrary sensor or actuator can be attached without the graph knowing what it is.*
- 🚧 Which chain to render: arrival narrows, prediction ranks, brevity breaks ties — *agreed 2026-08-01. **No step needs a new mechanism or a new constant**: the three are the addressing design, the prediction error signal and §7's compression principle, each doing a second job.*
- 🚧 Arrival narrows the field — *the input names an output machine, so the candidates are exactly the chains that REACH it. Selection is routing.*
- 🚧 Prediction ranks what is left — *keep the chain whose consequences best predict what arrives next, which moves the choice from an invented internal score to something the world can settle.*
- ⬜ Words, fetched from the concept map — *they come from what was learned, so it cannot name what it does not have.*
- ⬜ Words, composed by the system itself — *if it understands, it should be able to work out how to say things; nothing hands it grammar.*
- ⬜ An action on the world — *the same channel intervention needs, which makes acting and answering one mechanism instead of two.*
- ⬜ A structure — a map, a plan, a set of bindings — *the honest output for a system whose knowledge is a shape, and it needs no language at all.*
- ⬜ Template — *structurally incapable of adding a fact, which makes it a floor rather than a goal.*
- ❌ ~~An off-the-shelf LLM~~ — *a fluent renderer writes the right sentence from a wrong walk, so the score measures its world knowledge.* **Revives if** a test exists showing it cannot add or drop a fact.

**7. What changes over time.** The thing that makes it learn.

- ✅ The counts — *currently the whole of what learns.*
- ✅ Predict what comes next, and learn from being wrong — *`prediction.Predictor`, the first mechanism here that can be wrong. Scored **prequentially**: `learn` returns the surprise measured before the count is taken, so the model never sees the answer before being asked. On snake, hit@1 rises 0.49 → 0.72 over 4,000 steps, three seeds, against a shuffled control that stays under 0.10.*
- ✅ Bind the state and the action into one surface — *`(state, action)` becomes a surface of its own, so an interaction is an ordinary pairwise count and a composite surface has an owner like any other. Costs `states × actions` surfaces, which is the whole price.*
- ✅ Binding and factoring are MEMORY and GENERALISATION — *on a pair it has SEEN, bound wins 0.742 to 0.455; on one held out and never learned from, **bound scores 0.000 and factored 0.162 against a chance of 0.0019**, because a bound surface for an uncounted pair has nothing to look up. John's criterion for DEMONSTRATING understanding, and the world scores it.*
- ✅ Bind on demand — *the bound surface where the pair has any evidence, factoring where it has none. Takes the better number on BOTH columns on every seed, 0.742 seen and 0.162 held out. **No threshold**: "ever counted" has an answer where "counted enough" is a dial. Costs both writes.*
- ⬜ Factoring needs its halves to OVERLAP — *it composes two views of the SAME outcome, so where `left` and `right` share none, `min` scores everything at zero and it offers nothing.*
- ⬜ Prediction error decides when to ask rather than watch — *the second hole one mechanism closes, and it is not built. The asking budget is still a fixed fraction nobody chose.*
- ⬜ Learned representations for relations — *lets a relation never seen sit near ones that were, which counting cannot do.*
- ⬜ Structure that reorganises, not just weights — *C4 claims the system keeps rearranging what it knows, and nothing implements that.*
- ⬜ Compress — keep the boundaries that describe the stream in the fewest bits — *one principle that would supply forgetting, hierarchy and a reason to reorganise, all of which are missing.*
- ❌ ~~A trained readout on frozen random projections~~ — *everything durable ends up in one matrix, and the rule was never the limitation.*
- ❌ ~~Replay as a repair for churn~~ — *churn costs capacity, not knowledge.*

**8. Ownership.** Which machine holds which part.

- ✅ Consistent hashing, no directory and no coordinator.
- ✅ Split by concept — *`ownership.Ring` maps a concept to its owner, and `federated` and `buckets.Join` both use that one ring rather than two rules that could drift. Pooled capacity matches dimension splitting, but lone-node capacity is sixteen times larger at sixteen nodes and grows with the network.*
- ❌ ~~Split by dimension~~ — *the driver-based arrangement in the deleted `openplexus/distributed.py`, and the C1 violation the ring exists to avoid: it leaves a node stuck at one node's worth of capacity forever.* **Revives if** a driver becomes acceptable, which C1 currently forbids.
- ❌ ~~Any readout that sums across every machine~~ — *the step C1 forbids, and four gates were passed on top of one before anyone noticed.*

**9. Talking between machines.** How a question crosses the network.

- ✅ Point-to-point reads straight to the holder, no driver in between.
- ✅ Departure by suspicion and a deadline, not by one missed reply.
- ⬜ Surviving hostile participants — *no threat model exists, and the step where two things are judged the same is the obvious target.*

**10. How we know it works.** The measurement, which is a design choice like any other.

- ✅ Prequential — score as the stream arrives.
- ✅ FB15k-237, floor established before anything was built on it — *its predecessor's leak is gone: rules mined from train score 0.45 on train and 0.0001 on test. What it hands over instead is the marginal, MRR 0.2334, and published DistMult and ComplEx sit at 0.241 and 0.247 — within 0.014 of a baseline with no structure in it. Report the margin, never the MRR.*
- ✅ Beat a conventional system on the same input — *run, and the answer is "matched, on the part that is not the baseline". Against one floor: ours +0.0136, ComplEx +0.0136, DistMult +0.0076, but TransE +0.0606 and RotatE +0.1046. It matches the weaker half of the field and is nowhere near the stronger. Absolute MRR is the wrong column; most of everyone's is the marginal.*
- ✅ The word arrives as bytes through the same hash as everything else — *`tasks/written.py`: four written forms per digit, sometimes corrupted, sometimes silent, sometimes naming another digit. 302 word surfaces at 10 bits, about 72 per digit, which the system has to discover are one thing.*
- ❌ ~~One word node per class~~ — *a maximally strong hub, since everything about a digit necessarily co-occurs with it, and `link_img` was mostly it: 0.900/0.900/1.000 over three seeds against a multiplicity-only control's 0.100/0.100/0.105 at 10 bits, kmeans, `alternating`.* **Revives if** a task genuinely supplies one token per concept.
- ❌ ~~`cross 1.0000` as the cross-modal result~~ — *one cell of a narrow regime. Under that channel `cross` runs 0.81–1.00 at 6 bits and falls to 0.000 at 10 with `crossed` also 0.000, so nothing was reached; the byte channel is lower everywhere and is the only one reaching anything at 10 bits.*
- ✅ An interactive environment, not a recorded corpus — *John's position, 2026-08-01: this kind of model may need to ACT to learn at all. It is the only source supplying an error signal for free, and prediction, the trigger for asking and an output channel all need one. `tasks/snake.py` is the first, chosen over ARC-AGI-3 because it supplies the recurrence ARC withholds by design.*
- ✅ A LOCAL, head-centred view — *centred so the same situation in two places is one observation, which is what makes anything recur at all: 233 distinct views over 2,000 steps. Local so the food is usually unseen, which is what gives "act to disambiguate" something to disambiguate. `sight=None` is the full board, so the two are arms of one experiment.*
- ⬜ Something to learn in open space — *a centred view of a featureless region is the same view whichever way you went, so a step away from walls, food or the body teaches nothing and prediction is trivially right. Board size has to be chosen relative to sight, and this bounds what the task can show.*
- ⬜ Learn from live sensors, test on labelled data never trained on — *the only named way to tell whether a microphone-and-camera system learned anything.*
- ⬜ Noise that is sticky rather than uniform — *real irrelevant co-occurrence recurs together; ours is white noise, so ideas refuted against it are untested rather than dead.*
- ❌ ~~CLUTRR-symbolic as evidence of composition~~ — *62 facts counted from its two-hop rows, plus a bracketing search, answer 100% of the test split at every hop count; a shuffled table scores 0.12. What it measures is finding the order to apply knowledge in — the same facts folded left to right score 0.28.* **Revives if** a configuration is found whose relation algebra is not confluent.
- ❌ ~~Withholding CLUTRR's facts to make it an instrument~~ — *the 4,998 three-hop rows determine every held-out pair by deduction alone, returning the ceiling to 0.98 with 40 of the 62 withheld. The facts were never withheld, only restated.*
- ❌ ~~Train, then test~~ — *measures a system that stops, which is the one thing C4 forbids.*
- ❌ ~~Bits per token on text~~ — *bounded by what an n-gram table does, so it cannot show what structure adds.*

**11. Time.** How the stream's order gets into the graph, or fails to.

*Numbered last and belongs between 1 and 2. Renumbering would strand fourteen
`decision N` references across nine files, which is the ghost-reference problem
this project has already had once.*

- ✅ An occasion is a SET — *everything in one moment met everything else and nothing came first. Simple, and it means the graph cannot tell "A then B" from "A with B" anywhere.*
- ✅ A window carries recent moments forward, written ONE WAY — *`moments.Window`, John's original design, found missing after the restructure; `span=0` is the old behaviour exactly. **The storage was already directional and nothing used it**: `observed_with` writes one row where `pair` writes both, so time cost no new storage, only a decision about which direction to write.*
- ⬜ Weight the window by how far back — *the obvious refinement, and it needs fractional counts the accumulator does not hold.*
- ⬜ Duration — *nothing represents how long anything lasted, only that it was present.*
- ❌ ~~A global clock~~ — *two nodes comparing when things happened is the agreement C1 forbids. `buckets.Join` is the C1-legal version: a bucket is a rendezvous that notices a coincidence and is discarded, and nothing is ever looked up by time.*
- ❌ ~~Leaving sequence to the predictor~~ — *`Predictor` had to be handed `(state, action, next)` explicitly because the graph held no ordering, which puts time outside the representation where nothing else can use it.*
- ✅ The graph can predict from order alone once it holds any — *0.153 with the window on against **0.000** with it off, the control being that no temporal edge means no candidate to offer.*
- ⬜ Bound surfaces in the GRAPH, not only in the predictor — *the graph reaches 0.153 where `Predictor` reaches 0.717 on the same world, because co-occurrence pools over actions. **The same limitation as `factored` losing to `bound`**, and the same repair.*

---

## The constraints

An option that breaks one of these is not a candidate, however well it performs.

- **C1 — Nothing waits for the whole.** No update needs every part to have
  exchanged with every other part, and no answer needs a step every machine
  joins. A constant handed out once and frozen is fine — nobody waits for it. An
  agreement that has to be maintained as things change is not.
- **C2 — Messages are late, jittered and out of order.** Nothing may assume otherwise.
- **C3 — A machine vanishing mid-thought is normal**, not an error to recover from.
- **C4 — No training run that ends.** It never stops learning.

Latency is not a constraint. Ten minutes for an answer is an optimisation
problem if everything else works.

---

## The one that decides the project

**Does a relational objective buy reasoning?**

A graph database also stores and retrieves relations. What separates this from
one is whether it can produce something it was never told. If it cannot, what
remains is a distributed graph database with a learned front end — which is
useful, and is not this.
