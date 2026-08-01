# Open Plexus

An attempt to build AGI that runs on distributed compute over the internet.

---

## Decisions

Every part the data flows through, and every option for it — including the dead
ones, because this is the file that gets read and an option that is not here will
be proposed again.

**✅ in use ⬜ untried ❌ ruled out 🚧 approved, not built 🔀 both kept**

One line each. A ❌ says what killed it and, where it is known, what would bring
it back — refutations expire, and two have already become right later.

---

**1. Input → surfaces.** How raw data becomes an id that can be counted.

- ✅ Random-hyperplane LSH, over features centred per item — *no training and no data: two nodes send an identical input to the same surface every time, where k-means on different samples of the same stream manages under 0.12, and the bit count is the grain dial.*
- 🔀 Fixed transforms of one item — an ear-shaped filterbank, a cepstrum, per-item centring — *legal because a constant is not a codebook, and they are what makes an angle mean anything: on raw waveforms every front end including a random assignment scores the same. They improve the SPACE and not the allocation — k-means gains 0.05 from them and the hash gains 0.01.*
- ⬜ Spending the codes where the data is, without fitting a codebook — *the hash's whole remaining deficit, about 0.44 of purity in every feature space with structure in it. A data-free front end cannot know where the data is; whether the walk can repair the over-segmentation instead is the untested half.*
- ⬜ A codebook learned from co-occurrence — *removes the last borrowed component, but it must stay in sync as it learns, which C1 forbids.*
- ⬜ Per-node codebooks plus translation — *avoids agreement entirely, at the cost of unsupervised translation, which is harder than the goal.*
- ❌ ~~No discretisation — count raw similarity~~ — *every count stays 1, so no statistic can form.* **Revives if** counting is replaced by something that does not need recurrence.
- ❌ ~~Trained k-means quantiser~~ — *clustering by similarity is an identity assignment, which is the walk's job, and two nodes fitted on different samples of one stream agree about almost no item with nothing anywhere reporting it. It is still the better grouping — about twice the purity at a matched code count — and that gap is the price paid.* **Revives if** a per-node front end becomes acceptable, or identity stops being something the walk decides.

**2. What gets stored.** Which co-occurrences are kept.

- ✅ Every count, nothing ever cut.
- ❌ ~~Cut each surface's partners at the biggest score gap~~ — *refuses the ever-present distractor and evicts the word that names the concept.* **Revives if** something else supplies the refusal.
- ❌ ~~Cap how many partners are even considered~~ — *a constant nobody set on purpose, and it turned out to be the thing doing the cutting.*
- ⬜ Forget by age or disuse — *unbounded growth is not survivable on a phone, and nothing currently decides what to drop.*
- ⬜ Archive rather than delete — evict to a store on that node, never in the walk — *a machine that never forgets is fine; one that never compresses cannot form concepts, and an archive nothing waits on is C1-legal where a shared database is not.*

**3. Identity.** What makes several surfaces one thing.

- ✅ No id at all — a concept is what you reach by walking.
- ❌ ~~Freeze partners into groups by mutual agreement~~ — *a hard partition flips whole groups on a small score change, so it gets worse with more data at some point.* **Revives if** a task needs a yes/no answer to "are these the same".
- ❌ ~~Give every concept a global id~~ — *nobody can assign one without a coordinator.*
- ⬜ Identity at more than one grain — *dog and Labrador are the same walk at different resolutions, and nothing currently expresses "narrower than".*

**4. Retrieval.** How a question gets answered.

- ✅ Ranked walk, scored one-way from the asking side.
- ❌ ~~Average the two directions of an edge~~ — *a thing present everywhere scores 1.0 from its own side because that is true, and averaging lets it outrank real partners.*
- ❌ ~~Take the weaker direction~~ — *penalises exactly the hub edges worth keeping.*
- ❌ ~~Take the stronger direction~~ — *stops discriminating; it scored at the floor.*
- ❌ ~~Tune a damping exponent~~ — *five dials were tried and none was the axis.*
- ✅ Walk two steps, carrying the relation TYPES along the path — *the only version that pays. On FB15k-237 the endpoints of a test triple are 0.0000 one hop apart in training and 0.7373 two hops apart, so one step provably cannot reach the answer; typed and ranked, two steps clear the marginal floor by +0.0136 ± 0.0005. Evidence accumulates over every path that reaches a candidate, which is what a thresholded rule lookup cannot do — `sum` beats `max` at every blend weight.*
- ❌ ~~Walk further than one step WITHOUT the types~~ — *reaches the answer and cannot rank it. 0.0082 alone at depth 3, and every combination below the floor at every beam to 256, with an interior maximum at beam 16 so it is not under-searched. Two steps from an entity of average degree 37 is about 1,300 candidates and an untyped walk has nothing to say about which of them the question was about.* **Revives if** something other than relation types supplies that discrimination.
- 🔀 A blend weight that is per query rather than global — *built to test whether the losing queries were the ones where the structure had nothing to say. **They are not**: weighting by how concentrated a query's path evidence is scores +0.0131 against the global weight's +0.0136, with 11,359 losses against 11,302. It survives for a different reason — it cannot blow up. At full weight it scores 0.2379 where the global blend collapses to 0.1278, because a query whose paths spray over hundreds of candidates automatically receives almost none of them. That is the form to use where there is no validation set to choose a weight with, which is every node.*
- ⬜ Walk three steps or more — *0.2597 of answers lie further than two, and nothing has been run there. Typed three-step paths are the obvious extension and cost a fan-out cubed.*
- ⬜ Act on the world to disambiguate — *the only named escape from a confound that counting provably cannot separate.*

**5. The answer.** What a response actually is.

- ⬜ The ranked list itself, cut where the caller wants — *the walk already produces it; nothing extra is decided.*
- ⬜ A set, scored on exactness and completeness — *forces a commitment to a boundary rather than a hedge.*
- ⬜ Refuse when nothing was written there — *the only honest answer for a thing never seen, and the machinery exists.*
- ⬜ Generated one piece at a time — *the only way to produce a novel SEQUENCE, and nothing yet says when it stops.*
- ✅ A relation that was never stated but follows — *composition is novel output from a single query, no generation involved. On FB15k-237 a ranked walk over typed two-step paths, counted and blended with the marginal at a weight chosen on validation, scores **0.2470 against a structureless floor of 0.2334 — a margin of +0.0136 ± 0.0005** over 40,932 queries, where published ComplEx holds +0.0136 and DistMult +0.0076 over that same floor. **The gain is largest where the answer is RARE** (+0.0303, nearly tripling the floor) and smallest where it is common, so it is not the marginal being reinforced. It returned the marginal on CLUTRR's 62 facts, and 272,115 triples is what the difference was. A second seed reproduces every figure exactly — which establishes only that the blend weight is stable to resampling the validation set, because the test measurement has no randomness left in it to vary. **The margin is a dilution**: split by whether any path reached the answer, it is +0.0474 on the 35% where one did and −0.0046 on the 65% where none did, and the weighted average is the headline. The reachable subset is selected by the mechanism's own ability and its floor is twice as high, so this is not a claim about what more reach would buy.*
- ⬜ The answer to an analogy — *find where two parts of the map have the same shape, and read off the missing corner.*
- ⬜ A contradiction the map contains — *an output that was never an input, and nobody asked the question.*
- ⬜ A bridge between two regions that never co-occurred — *the thing that connects distant fields, aimed at deliberately rather than stumbled into.*
- ❌ ~~A fixed frame with slots to fill~~ — *a frame is a traversal with a schedule nobody supplied.* **Revives if** a domain genuinely supplies the frame.

**6. Output.** Turning an answer into something that leaves the system. **Not
necessarily words** — an action is an output, and so is a structure.

- ⬜ Words, fetched from the concept map — *they come from what was learned, so it cannot name what it does not have.*
- ⬜ Words, composed by the system itself — *if it understands, it should be able to work out how to say things; nothing hands it grammar.*
- ⬜ An action on the world — *the same channel intervention needs, which makes acting and answering one mechanism instead of two.*
- ⬜ A structure — a map, a plan, a set of bindings — *the honest output for a system whose knowledge is a shape, and it needs no language at all.*
- ⬜ Template — *structurally incapable of adding a fact, which makes it a floor rather than a goal.*
- ❌ ~~An off-the-shelf LLM~~ — *a fluent renderer writes the right sentence from a wrong walk, so the score measures its world knowledge.* **Revives if** a test exists showing it cannot add or drop a fact.

**7. What changes over time.** The thing that makes it learn.

- ✅ The counts — *and this is currently the whole of what learns.*
- ⬜ Learned representations for relations — *lets a relation never seen sit near ones that were, which counting cannot do.*
- ⬜ Structure that reorganises, not just weights — *C4 claims the system keeps rearranging what it knows, and nothing implements that.*
- ⬜ Predict what comes with what, and learn from being wrong — *counts only go up, so nothing is ever wrong and nothing is ever corrected; predicting RELATIONS is an error signal that is not next-token prediction.*
- ⬜ Compress — keep the boundaries that describe the stream in the fewest bits — *one principle that would supply forgetting, hierarchy and a reason to reorganise, all of which are currently missing.*
- ❌ ~~A trained readout on frozen random projections~~ — *everything durable ends up in one matrix, and the rule was never the limitation.*
- ❌ ~~Replay as a repair for churn~~ — *churn costs capacity, not knowledge.*

**8. Ownership.** Which machine holds which part.

- ✅ Consistent hashing, no directory and no coordinator.
- 🔀 Split by dimension, or split by concept — *dimension is the default; concept is required once capacity has to grow.*
- ❌ ~~Any readout that sums across every machine~~ — *the step C1 forbids, and four gates were passed on top of one before anyone noticed.*

**9. Talking between machines.** How a question crosses the network.

- ✅ Point-to-point reads straight to the holder, no driver in between.
- ✅ Departure by suspicion and a deadline, not by one missed reply.
- ⬜ Surviving hostile participants — *no threat model exists, and the step where two things are judged the same is the obvious target.*

**10. How we know it works.** The measurement, which is a design choice like any other.

- ✅ Prequential — score as the stream arrives.
- ⬜ Learn from live sensors, test on labelled data never trained on — *the only named way to tell whether a microphone-and-camera system learned anything.*
- 🔀 Beat a conventional system on the same input — *run at last, and the answer is "matched, on the part that is not the baseline". Against the same structureless floor on FB15k-237 this project's margin is +0.0136 and ComplEx's is +0.0136, DistMult's +0.0076 — but TransE holds +0.0606 and RotatE +0.1046, so it matches the weaker half of the published field and is nowhere near the stronger. **Absolute MRR is the wrong column and that is the finding**: most of everyone's is the marginal.*
- ✅ FB15k-237, floor established before anything was built on it — *the leak that killed its predecessor is genuinely gone: inverse rules mined from train score 0.45 applied to train and 0.0001 applied to test, and no test triple appears verbatim or reversed. What it hands over instead is the marginal — relation-tail frequency alone reaches MRR 0.2334 — and published DistMult and ComplEx sit at 0.241 and 0.247 (RotatE, ICLR 2019, Table 5, which states the filtered both-directions protocol this audit uses). **So two widely cited results are within 0.014 of a baseline with no structure in it**, and the quantity worth reporting here is the margin over the marginal, not the MRR. The direction survives every tie policy and only the size moves — a counting score puts thousands of entities on exactly zero, so the floor is 0.2305 pessimistic and 0.2597 optimistic, and under the optimistic reading both of those models fall BELOW it.*
- ⬜ Noise that is sticky rather than uniform — *real irrelevant co-occurrence recurs together; ours is white noise, and ideas refuted against it are untested rather than dead.*
- ❌ ~~CLUTRR-symbolic as evidence of composition~~ — *62 facts counted from its two-hop training rows, plus a search over bracketings, answer 100% of the test split at every hop count and reach 1.01 relations per puzzle; a shuffled table scores 0.12. It measures whether a system can find the ORDER to apply knowledge in — left to right, the same facts score 0.28. **Withholding facts does not repair it**: the 4,998 three-hop training rows determine every held-out pair by deduction alone, returning the ceiling to 0.98 with 40 of the 62 withheld.* **Revives if** a configuration is found whose relation algebra is not confluent, or when the question being asked is about search.
- ❌ ~~Train, then test~~ — *measures a system that stops, which is the one thing C4 forbids.*
- ❌ ~~Bits per token on text~~ — *bounded by what an n-gram table does, so it cannot show what structure adds.*

---

## The constraints

An option that breaks one of these is not a candidate, however well it performs.

- **C1 — Nothing waits for the whole.** No update needs every part to have
  exchanged with every other part, and no answer needs a step every machine joins.
  A constant handed out once and frozen is fine — nobody waits for it. An
  agreement that has to be maintained as things change is not.
- **C2 — Messages are late, jittered and out of order.** Nothing may assume otherwise.
- **C3 — A machine vanishing mid-thought is normal**, not an error to recover from.
- **C4 — No training run that ends.** It never stops learning.

Latency is **not** a constraint. Ten minutes for an answer is an optimisation
problem if everything else works.

---

## The one that decides the project

**Does a relational objective buy reasoning?**

A graph database also stores and retrieves relations. What separates this from
one is whether it can produce something it was never told. If it cannot, what
remains is a distributed graph database with a learned front end — which is
useful, and is not this.
