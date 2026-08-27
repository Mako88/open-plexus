# A distributed path toward general intelligence

Status: architectural proposal. Nothing in this document is a finding. Every mechanism is
provisional until a controlled experiment distinguishes it from a simpler alternative.

This plan starts from three constraints:

1. The cognitive substrate is distributed by construction.
2. It does not depend on an existing language model, a pretrained foundation model, or a
   centralized model-training run.
3. It should eventually exploit large numbers of ordinary, heterogeneous, intermittently
   connected devices.

The aim is not to reproduce a language model internally. It is to pursue directly the
capabilities that language models acquire indirectly through sequence prediction and scale,
while adding the persistent learning, grounding, agency, causal reasoning, and revision that
they do not reliably provide.

---

## 1. The thesis

General intelligence is an ongoing process that builds and revises a model of its world,
uses that model to predict the consequences of possible actions, and chooses both actions and
questions in service of persistent purposes.

The proposed learning unit is a falsifiable commitment:

> In a represented context, this relation, state, or consequence is expected to hold.

A commitment can be supported, contradicted, refined, generalized, composed, or retired. Its
identity is derived from its content. Evidence can be accumulated locally and merged without
requiring a node to expose its private state.

Commitments are the epistemic substrate, not the whole architecture. A useful mind also needs
entity identity, relational binding, temporal and causal structure, memory retrieval, action,
goals, uncertainty, inquiry, planning, and metacognition. Each of those must operate on a
shared learned world model.

The central loop is:

```text
observe -> bind -> retrieve -> predict -> choose -> act
   ^          |         |          |         |       |
   |          +------ revise <-----+---------+-------+
   |                    |
   +------------ ask / experiment
```

The architecture succeeds only if capability arises from that loop on continuing streams.
Offline train-then-test phases, hand-installed ontologies, and benchmark-specific switches do
not establish the claim.

---

## 2. The distribution claim

Distribution must provide more than parallel execution of a centralized algorithm. No node
is authoritative, no complete brain must fit on one machine, and loss or reordering of a
message must not corrupt the model.

The internet should not be treated as one low-latency computer. A coherent cognitive cycle
cannot wait for a global round trip, and an embodied agent cannot let a disconnected continent
stall an action. The design therefore has three time scales:

- **Local reflex and working coalition:** milliseconds to seconds, using nearby nodes and
  currently available knowledge.
- **Regional consolidation:** seconds to hours, exchanging evidence, repaired commitments,
  skills, and summaries across a changing colony of nodes.
- **Global cultural learning:** hours to years, sharing independently testable concepts,
  procedures, instruments, and findings between brains.

This distinction is important. Billions of devices are more plausibly a substrate for a
civilization of cooperating cognitive colonies than neurons in one globally synchronous
brain. A single agent may span many devices, but its fast loop must remain locally viable.
The broader network expands memory, experience, experimentation, and consolidation without
becoming a single point of cognitive latency or control.

### Required substrate properties

- Nodes join and vanish during any operation.
- Messages are delayed, duplicated, reordered, or lost.
- Every externally visible operation is idempotent.
- Durable knowledge is content-addressed and independently verifiable.
- Mergeable evidence uses monotone structures where possible.
- Time-sensitive judgment remains local; a lifetime aggregate cannot describe a changing
  world.
- No node can inspect another node's private state.
- A node can abstain when required evidence is absent.
- Progress continues under partitions and converges where the evidence converges.
- Resource budgets are explicit: storage, bandwidth, energy, latency, and trust.

### The security boundary

Internet scale introduces faulty and adversarial participants, not merely unreliable ones.
Evidence from remote nodes cannot be counted as independent merely because it has different
network origins. The substrate eventually needs:

- signed messages and stable device identities;
- replay protection and causal message identifiers;
- provenance for observations, derivations, and transformations;
- resistance to Sybil amplification and colluding evidence sources;
- privacy-preserving disclosure of evidence where feasible;
- quarantine and reputation that affect trust without changing semantic identity;
- capability-based authorization for sensors, actuators, and costly experiments.

Security is not a late deployment concern. An epistemology that assumes honest evidence will
learn arbitrary falsehoods on the public internet.

---

## 3. What the system must learn

The following are capability requirements. They deliberately avoid choosing mechanisms.

### 3.1 Persistent things

The system distinguishes a thing from its observations and maintains its identity across
time, changes of appearance, changes of location, and gaps in observation. It distinguishes
two similar things present together.

### 3.2 Relations and bound roles

It represents relations whose participants have distinguishable roles. From examples of
`gives(Alice, book, Bob)`, it can apply the learned relation to novel people and objects
without confusing giver, gift, and recipient.

### 3.3 Events and time

It represents states, events, durations, order, recurrence, and change. It can distinguish a
present state from its history and can reason at several temporal grains.

### 3.4 Explicit alternatives and negation

It can represent that a claim does not hold, that evidence is absent, that two claims
conflict, and that several explanations remain possible. These are different epistemic
states.

### 3.5 Calibrated uncertainty

It knows when the available evidence does not determine an answer. Confidence reflects
future frequencies under changing conditions, and the system can abstain or seek evidence.

### 3.6 Causal and counterfactual structure

It distinguishes observation from intervention. It can predict how outcomes change when it
acts, compare incompatible hypothetical actions, and revise causal beliefs when interventions
refute them.

### 3.7 Layered memory and learned retrieval

It maintains current state, episodic history, semantic regularities, and procedures. Under a
fixed compute budget it retrieves knowledge that bears on the present question, and it learns
from retrieval failures.

### 3.8 Goals and persistent agency

It represents desired and forbidden states, delayed consequences, resource constraints, and
competing purposes. Goals persist without being confused with predictions or observations.

### 3.9 Planning and skill formation

It composes learned transitions into possible courses of action, tests them in imagination,
executes with feedback, and consolidates successful recurring plans into reusable skills.

### 3.10 Autonomous inquiry

It identifies consequential uncertainty, proposes observations or interventions that
distinguish live hypotheses, and values information by its expected effect on future decisions.

### 3.11 Grounded communication

It learns signals as attributes of shared concepts and intentions. Language is acquired as a
social sensor and actuator grounded in perception, action, correction, and joint attention.
It is not learned first as an isolated text-completion task.

### 3.12 Metacognition and revision

It models the reliability, cost, and domain of its own strategies. It can localize a failure,
revise the beliefs responsible for it, retain unrelated knowledge, and choose when to think,
ask, act, or defer.

### 3.13 Social learning without epistemic surrender

It can accept testimony, instruction, demonstrations, and artifacts from other agents while
preserving provenance and the possibility of falsification. Consensus is evidence about what
agents report, not automatic evidence that the report is true.

---

## 4. Proposed cognitive organization

This section contains mechanisms to test, not permanent architecture.

### 4.1 Codes and concepts

Front ends emit stable codes for discriminable features without naming their meaning. Codes
co-occurring across senses and time become evidence for latent persistent concepts. A concept
is a content-addressed node whose meaning is its learned relations, predictive commitments,
and grounding history rather than a human-assigned label.

New concepts are minted when the current representation systematically conflates cases that
require different predictions or actions. Merging concepts requires positive evidence that
their distinctions make no difference across an adequately diverse set of contexts.

The machinery may contain structural inductive biases—binding slots, time, evidence, and
composition—without containing a hand-written inventory of worldly kinds.

### 4.2 Commitment templates and bindings

A commitment should eventually express more than a conjunction of present codes predicting
one future code. A candidate form is:

```text
context(pattern, bindings, temporal_window)
    -> expectation(predicate, bound_arguments, time_relation)
```

Patterns contain constants, variables, and applied relations. Matching returns a binding
environment. The expectation is grounded with that same environment, preserving role
identity across prediction, repair, and explanation.

Repair adds a discriminating condition or relation to a failed commitment. It does not edit
away the original evidence. Generalization removes a condition only when a sequentially valid
comparison supports the broader rule.

### 4.3 Evidence and uncertainty

Each commitment carries separate records for:

- support and contradiction;
- opportunities where it abstained;
- observation versus intervention;
- recency-sensitive local performance;
- provenance and dependence between evidence sources;
- a time-uniform uncertainty interval or equivalent sequential evidence measure.

Repeated optional inspection of ordinary fixed-sample p-values is invalid. Candidate creation,
repair, pruning, and generalization should use confidence sequences, e-values, or another
sequentially valid method. A simpler first control is geometric alpha spending across looks
and candidate families.

### 4.4 Working coalition

Only a bounded subset of the model participates in a cognitive cycle. A local coalition is
assembled from:

- the current sensory and internal state;
- active goals;
- retrieved concepts and episodes;
- commitments whose scopes match;
- candidate actions and questions;
- unresolved prediction errors.

Competition for the coalition is based on expected decision relevance, uncertainty reduction,
urgency, and resource cost. This is an attention mechanism in the functional sense, without
requiring dense global attention over all stored knowledge.

### 4.5 Memory

Use four logically distinct stores even if they later share a physical substrate:

- **Current state:** keyed, revisable claims such as `location(Mary)` with ordering and
  conflict semantics.
- **Episodes:** immutable, provenance-bearing event sequences.
- **Semantic model:** consolidated concepts, relations, and commitments.
- **Procedures:** policies or plan fragments with applicability and outcome evidence.

Retrieval begins with explicit structural keys and bounded graph expansion. Learned retrieval
then predicts which keys, relations, episodes, or procedures will change a decision. Its
evaluation must compare no retrieval, random retrieval, structural retrieval, and learned
retrieval under identical budgets.

### 4.6 Action, intervention, and planning

Actions are represented as events the agent can initiate, with preconditions, costs,
permissions, and uncertain effects. Choosing an action creates an intervention marker so its
result is not statistically confused with passive observation.

Planning initially enumerates short action sequences through learned transition commitments.
It preserves multiple possible successor states and their evidence intervals. Search depth,
branching, and simulation effort are metacognitive choices charged against a resource budget.

Repeated successful plan fragments become procedures. Procedures remain falsifiable and can
be specialized when context predicts failure.

### 4.7 Inquiry

Question generation searches for an affordable observation or intervention expected to alter
a consequential choice. A first approximation is:

```text
value(question) = expected reduction in decision loss - action cost - delay cost
```

The evaluation must include noisy but surprising sources. A system rewarded for raw surprise
or prediction progress alone can spend forever watching noise.

### 4.8 Development and consolidation

The system learns continuously, but not every learning process must run at the same speed.
Idle resources can replay episodes, test compressions, search for equivalent concepts, and
propose reusable procedures. Consolidation creates candidates; it does not rewrite accepted
knowledge without evidence.

A developmental curriculum supplies increasingly rich environments without installing their
solutions. The same learner and dials cross every stage.

### 4.9 Lessons that transfer from Persistence

The separate Persistence project implements continuity infrastructure around language models.
Its language model supplies most interpretation, summarization, and judgment, so its complete design
cannot serve as the cognitive substrate proposed here. Several of its distinctions are independent of
that substrate and should be retained.

#### Memory intrinsic properties are separate from activation

Persistence stores a fragment once while allowing its relevance, order, and collapsed state to differ
between working contexts. The analogous rule here is:

- provenance, evidence, semantic identity, and learned content belong to a memory artifact;
- salience, retrieval score, presentation grain, and coalition membership belong to the artifact's
  relationship with a current task or context.

This prevents a fact that is currently irrelevant from becoming intrinsically unimportant. It also
allows different agents or working coalitions to activate one shared artifact differently without
giving it several semantic identities.

#### Artifact identity is separate from storage and delivery identity

Persistence discovered that a database row identifier could not identify an utterance across peer
stores. It now uses an originator-minted message identifier that remains constant across relays, while
the local row identifier and per-copy relay depth keep their separate jobs.

Generalize that distinction throughout this architecture:

- a concept, commitment, observation, procedure, or utterance has stable semantic identity;
- each replica has local storage identity and version state;
- each delivery has path, hop, time, and trust metadata;
- each derivation has its own identity and points to the artifacts it used.

Conflating any two of these makes deduplication, provenance, replay protection, and causal audit
unreliable.

#### Each peer owns its memory; a room owns no minds

Persistence's federated shape is one single-owner runtime and continuity store per peer, with rooms as
message channels. Each peer records its own experience of the shared conversation. The room does not
own or merge their private memories.

That is a sound default for the proposed federation of brains. Shared infrastructure transports
artifacts and establishes common referents. It does not become the authoritative world model. Peers
can disagree, miss different observations, assign different trust, and later converge through evidence.

Within one distributed brain, single-owner storage is too restrictive; evidence must replicate. The
principle that still transfers is semantic ownership: a transport, index, cache, or coalition must not
silently become the authority that decides what the brain believes.

#### Provenance travels with the claim

Persistence attaches named and typed sources to fragments and preserves attribution through relay.
The proposed system should go further by making provenance recursively derivational:

```text
claim -> observations / testimony / prior claims -> instruments and agents -> conditions
```

A human report, another brain's conclusion, a sensor observation, and a locally derived prediction can
express the same content while carrying different evidence. Relaying must never reattribute the content
to the relayer. Trust modifies how evidence is used; it does not alter the content's identity.

#### Important self-change is proposed before it is adopted

Persistence protects selected identity fragments and changes them through first-class proposals. A
proposal cannot be created and accepted in the same turn, and acceptance applies the proposal and its
status atomically.

The non-LLM analogue should apply to core goals, identity commitments, trust policy, safety boundaries,
and changes to the learning architecture. A proposed change carries:

- the exact old and proposed state;
- supporting and contradicting evidence;
- the expected consequence and rollback condition;
- who or what may authorize it;
- a deliberation interval or independent evaluation;
- an atomic adoption record.

This is not a rule that important beliefs never change. It gives high-impact revision a slower,
inspectable path than ordinary learning.

#### Memory operations are visible and recoverable

Persistence exposes the context budget to the peer and provides explicit operations to summarize,
detach, archive, restore, and reprioritize memory. Its default is archive rather than erase, with an
append-only audit record.

The transferable requirement is metacognitive legibility: the agent can observe its memory and compute
limits, predict what a consolidation will discard, and inspect what actually changed. Episodic source
material should remain recoverable until a declared retention or privacy policy permits deletion.

At large scale, keeping everything forever is neither physically nor ethically viable. Reversibility
therefore needs explicit retention classes, privacy deletion, cryptographic erasure where required, and
measured consolidation loss. Preserve-by-default is a development posture, not an unlimited storage
claim.

#### Wakes are temporal intentions, not merely timers

Persistence lets a peer schedule an autonomous wake with a note to its future runtime. The equivalent
here is a durable temporal intention containing a trigger, purpose, relevant context keys, expiry,
authority, and cancellation condition. When it fires, it competes for a working coalition like any
other event; it does not automatically gain permission to act.

#### Attention gates belong before expensive cognition

Persistence's multi-peer work moved its addressing and wake filter ahead of the language-model call.
That prevents every room message from waking every peer and producing quadratic cost.

This becomes a general distributed rule: cheap, inspectable routing based on identity, subscription,
causal relevance, and resource policy occurs before coalition assembly and remote retrieval. The gate
must be allowed to abstain and its misses must be measured, because a cheap gate can save nearly all
compute by silently excluding what the agent needed to notice.

#### Private state and deliberate sharing are distinct

Persistence distinguishes a peer's private working material from what it intentionally says or shares
with a room. A distributed brain likewise needs private per-agent memory, restricted internal state,
and explicit publication. Provenance and privacy labels must survive summarization, derivation, caching,
and relay.

#### Persistence mechanisms that do not transfer directly

The following are useful controls or prototypes, but they currently rely on capabilities supplied by a
language model:

- natural-language fragments as the primary semantic representation;
- model-authored summaries as consolidation;
- self-assigned scalar importance and confidence;
- lexical full-text retrieval over recent conversation;
- a prompt as the complete working coalition;
- tagged model output as the source of memory operations and actions;
- a model deciding in prose what constitutes identity or relationship memory.

For this program, consolidation must preserve tested relational structure, confidence must derive from
evidence, retrieval must operate on learned concepts and decision relevance, and memory actions must be
available to the native policy. Persistence's implementations can serve as behavioral baselines while
those replacements are developed.

---

## 5. Physical distribution

### 5.1 The unit of placement

Place immutable concepts, commitments, evidence shards, episode segments, and procedure
records independently by content identity. A node caches the material its local experience
and active coalitions use. Replication follows observed demand and durability requirements.

Do not make a physical node equivalent to a concept or neuron. Physical placement changes;
semantic identity must not.

### 5.2 Round-scoped interaction

Every cognitive interaction has a content-derived or collision-resistant identifier and an
explicit local deadline. A round carries its moment, retrieved context, candidate actions,
votes, and provenance. No mutable fleet-wide field may implicitly hold the current round.

Late responses can update evidence for future decisions but cannot silently change a decision
already made. Retrying a message is safe. Duplicate settlement is detectable.

### 5.3 Local decisions, mergeable learning

Nodes decide from their local coalition and current evidence. Durable counts and immutable
findings merge asynchronously. Recency-weighted beliefs about current conditions remain local
or are published with their time and observation domain.

This prevents convergence requirements from making adaptation impossible. The shared history
can converge while present judgment differs legitimately by place and time.

### 5.4 Heterogeneous resources

Devices advertise capabilities and budgets rather than pretending to be interchangeable.
Placement accounts for:

- latency and connectivity;
- memory and durable storage;
- compute and accelerator type;
- energy and thermal limits;
- sensor or actuator access;
- trust and privacy domain;
- expected availability.

Useful work must be divisible into small, interruptible, independently checkable tasks.
Speculative consolidation and experiment search are better global workloads than the embodied
fast loop.

### 5.5 A federation of brains

The long-term network should support both private agents and shared cognitive services.
Brains exchange falsifiable artifacts: concepts with grounding recipes, commitments with
provenance, procedures with applicability evidence, and experiments with results.

They should not merge all belief indiscriminately. A foreign concept is imported as a
hypothesis until local or trusted evidence grounds it. This permits cumulative culture without
turning popularity into truth.

---

## 6. Obtaining language-model-like capabilities directly

The project should measure the underlying capabilities rather than surface imitation.

### Broad knowledge

Use distributed episodic storage, semantic consolidation, teaching, observation, and artifact
exchange. Retrieve a small relevant subgraph instead of activating a dense model containing
everything on every inference.

### Compositional language

Learn communication through grounded interaction. Discover recurring signal structures that
bind to concepts, roles, events, goals, and discourse state. Test novel combinations of known
parts, not memorized utterances.

### In-context adaptation

Treat the current interaction as revisable working and episodic state. New facts immediately
affect retrieval and prediction without changing a global parameter array.

### Few-example learning

Reuse established relational structure and mint only the new distinctions required by failed
predictions. A new concept should inherit applicable commitments through bindings, then retain
or reject them based on evidence.

### Reasoning

Compose explicit learned relations and simulate consequences in the working coalition. Keep
derivation provenance so an answer can be checked, revised, or reproduced elsewhere.

### Generation

Generate actions or utterances from communicative goals, candidate semantic structures, and
learned realization procedures. Fluency is evaluated separately from truth and task success.

### Creativity

Search for novel compositions of concepts and procedures, then filter them through predicted
utility, constraint satisfaction, and experiment. Novelty without evaluation is mutation, not
creative competence.

The hoped-for efficiency comes from sparse activation, structural reuse, persistent memory,
local online learning, and bounded retrieval. That efficiency is a hypothesis to measure, not
an assumption.

---

## 7. Experimental program

Every stage uses generated families of worlds with held-out entities, structures, seeds, and
regimes. Tests distinguish interpolation from structural transfer. Each claimed mechanism has
an ablation and a cheaper control.

### Stage 0: substrate equivalence

Build a deterministic reference implementation and the distributed substrate.

Required demonstrations:

- one local holder and one-node fleet produce identical decisions for every dial;
- delayed, duplicated, reordered, and dropped messages do not corrupt evidence;
- cancellation and retry settle a round exactly once;
- concurrent rounds do not exchange state;
- configuration disagreement is detected before evidence is combined;
- loss of any non-quorum subset preserves available knowledge as specified.

Do not proceed while cognitive results can be explained by substrate differences.

### Stage 1: continuing predictive learning

Worlds contain deterministic, stochastic, switching, and partially observable processes with
no episode boundary.

The learner must acquire useful commitments, repair conflated contexts, retire noise, track a
regime change, and abstain when evidence is insufficient. False discovery is measured across
the whole continuing search, not per round.

### Stage 2: persistent identity and relations

Worlds contain multiple similar objects, occlusion, attribute change, and relations with
permuted roles.

Held-out tests introduce novel fillers and novel combinations. Success requires identity
tracking and role-correct transfer. Surface co-occurrence controls must fail where binding
succeeds.

### Stage 3: memory and retrieval

Relevant evidence is displaced beyond working capacity and mixed with distractors. Questions
require one-hop, multi-hop, temporal, and provenance-sensitive retrieval.

Compare no retrieval, random retrieval, fixed structural retrieval, and learned retrieval at
the same bandwidth and compute. Report recall of decision-relevant evidence and downstream
decision quality separately.

### Stage 4: causality and intervention

Generated worlds include confounding, common causes, delayed effects, ineffective actions,
and changing causal regimes.

The agent must distinguish observation from intervention, select experiments that separate
hypotheses, and transfer a learned causal schema to new entities. Correlational prediction is
the control, not the baseline assumed to be causal.

### Stage 5: grounded communication

Two agents share an environment with partially different observations. Signals begin
arbitrary. Demonstration, correction, reference, questions, and joint action give them use.

The system must acquire labels, predicates, role-sensitive expressions, negation, temporal
reference, and novel compositions. Evaluation asks about unseen situations and requires action
as well as verbal response.

Natural language is introduced only after the mechanism succeeds on controlled emergent and
synthetic languages. A human-authored primer is instruction to be tested, not ontology to be
installed.

### Stage 6: goals, planning, and inquiry

Worlds require delayed multi-step action, resource tradeoffs, information gathering, and safe
abstention. Some observations are noisy and interesting but irrelevant.

Measure achieved goals, model calibration, cost, unnecessary actions, experiment value, and
transfer of learned procedures. Compare reactive action, planning without inquiry, and joint
planning with inquiry.

### Stage 7: metacognition and revision

The agent faces distribution shift, unreliable teachers, conflicting testimony, broken
procedures, and limited deliberation budgets.

It must allocate effort, identify the failing assumption, revise locally, preserve unrelated
competence, and recover from a trusted source becoming unreliable.

### Stage 8: federation and cumulative culture

Multiple independently developed brains exchange concepts, experiments, and procedures across
adversarial and unreliable networks.

Measure the value and cost of imported knowledge, time to local grounding, resistance to
false consensus, provenance retention, privacy leakage, and improvement that exceeds adding
the agents' isolated experience.

### Stage 9: heterogeneous device deployment

Move an already useful brain from simulated nodes to containers, then to nearby phones and
computers, then to a wider voluntary federation. Each transition must preserve substrate
equivalence and state its energy, latency, bandwidth, and failure costs.

Hardware scale is earned by a capability that can use it. It is not evidence for the
capability by itself.

---

## 8. Evaluation discipline

Maintain separate status for three questions:

- **Engineering:** Does the implementation satisfy its distributed and deterministic
  contracts?
- **Architectural:** Does the mechanism perform the operation it claims to perform?
- **Scientific:** Does controlled evidence show that the operation causes generalization or
  capability on the target?

A green implementation test does not establish an architectural capability. A capability on
an instrument does not establish target improvement. A target score does not identify its
cause without an ablation.

For each experiment, record before running:

- the claim;
- the alternative mechanism or cheaper control;
- the measurement and uncertainty method;
- the result that would refute the proposal;
- the compute, storage, bandwidth, energy, and data exposure;
- the target worlds and held-out structural dimensions.

Report scaling curves rather than a single largest run. The central economic measurement is
capability per unit of energy, bandwidth, storage, experience, and wall-clock time—not only
capability per parameter or device.

---

## 9. Safety and governance requirements

Agency, distributed persistence, and internet-scale execution create risks before anything
deserves the name AGI.

- Actuation begins in generated worlds and sandboxes.
- Every real actuator has explicit capabilities, rate limits, and a local human-controlled
  revocation path.
- Goals and observations are separate types; an inferred prediction cannot become authority
  to act.
- Imported procedures run with least privilege and declared resource bounds.
- Important decisions retain causal provenance and the evidence available at the time.
- The system can be stopped locally even when the wider network persists.
- Replication is budgeted and permissioned; cognitive value never grants a right to consume
  another device.
- Privacy boundaries constrain learning and retrieval, not merely display.
- Evaluation includes deception, reward tampering, collusion, unsafe information seeking, and
  goal persistence after revocation.

The desired endpoint is not an immortal uncontrolled process distributed across unwilling
machines. It is a voluntary, inspectable federation whose cognitive artifacts can persist
without granting every artifact agency.

---

## 10. Immediate build order

This branch proposes the following order. It does not replace the evidence-driven order on the
main experimental branch unless John explicitly chooses to merge the programs.

1. **Make the distributed substrate semantically equivalent to the local substrate.** Replace
   fleet-wide transient state with round-scoped messages, add idempotent settlement, validate
   configuration fingerprints, and test every dial on one holder versus a one-holder fleet.
2. **Make continuing statistical search valid.** Add a sequentially valid repair bar and
   measure population-wide false discoveries under indefinite optional looks.
3. **Introduce bindings without an ontology.** Begin with role-separated input modalities,
   then add variable-bearing commitment templates whose matcher returns the environment used
   to ground an expectation.
4. **Separate current state, episodes, semantic commitments, and procedures.** Implement a
   keyed current-state store and immutable episodic records before learned retrieval.
5. **Add bounded structural retrieval.** Establish fixed-budget controls before training a
   retrieval policy.
6. **Represent interventions.** Build the smallest confounded world in which passive
   correlation and action have different consequences.
7. **Add explicit alternatives, evidence intervals, and abstention.** Evaluate deterministic,
   stochastic, hidden-context, and switching worlds together.
8. **Let uncertainty generate experiments.** Choose questions by expected decision-relevant
   information gain and test against surprise-seeking controls.
9. **Compose transitions into plans and consolidate successful fragments into procedures.**
10. **Ground a synthetic social language, then a constrained natural-language primer.** Do
    not ask fluency to stand in for identity, binding, causality, or retrieval.
11. **Federate independently learned artifacts with provenance and adversarial controls.**
12. **Scale onto heterogeneous voluntary devices only when measurements identify useful work
    for them.**

The first concrete vertical slice should be small: two unreliable nodes, one continuing
relational world, persistent object identity, a role-bound prediction, one controllable
intervention, bounded retrieval of a displaced fact, and an autonomously selected question.
That slice touches the complete cognitive loop without requiring natural language or internet
scale. Every later scale claim should preserve and extend it.

---

## 11. Open research questions

- What is the smallest representation that supports novel role binding while preserving
  deterministic content identity across nodes?
- When should two independently minted concepts be treated as equivalent, related, or merely
  correlated?
- Which evidence summaries remain mergeable when observations are dependent, adversarial, or
  drawn from different regimes?
- How should a local coalition price remote retrieval against latency and uncertainty?
- Which parts of current state require non-monotone revision, and what distributed history is
  sufficient to make that revision auditable?
- Can causal structure emerge from repaired commitments without installing causal primitives
  beyond the distinction between observation and intervention?
- How can procedures be composed and generalized without recreating an opaque program search
  problem?
- What intrinsic objectives produce useful inquiry without wireheading, noise fixation, or
  uncontrolled resource acquisition?
- At what layer should trust attach: device, observer, instrument, derivation, claim, or a
  conditional combination of them?
- How much global sharing improves a brain after bandwidth, correlated evidence, privacy, and
  adversarial behavior are priced honestly?
- Which language capabilities truly require linguistic scale, and which fall out of grounded
  concepts, bindings, memory, and social interaction?
- What result would demonstrate that sparse structural reuse is actually cheaper than dense
  learned representations at comparable capability?

These questions are part of the research program. Prematurely resolving them in prose would
turn the plan into an implementation prescription unsupported by evidence.

---

## 12. The bet in one paragraph

Build many small, unreliable holders of falsifiable knowledge that collectively maintain a
learned relational and causal world model. Keep the fast cognitive loop local, let durable
evidence and reusable discoveries spread asynchronously, and make every imported belief retain
its provenance and ability to be contradicted. Acquire language through grounded social use,
reason through bound relations and simulated interventions, retrieve rather than activate the
whole memory, and choose questions as well as actions. Scale only after controlled worlds show
which capability benefits from added devices and after the cost of those devices is measured.
