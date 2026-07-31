# Open Plexus — Goals

What this project is for, what would refute it, and what is deliberately not
being attempted. Nothing below is a measurement. This document states intent and
the conditions under which the intent is wrong; every number in it is either
arithmetic or an inherited result from a prior project, and both are labelled as
such.

This is the first document in the project and the architecture is downstream of
it. That ordering is deliberate and is the correction of a specific, named
mistake — see *§6, Why this exists*.

---

## IN PLAIN TERMS

Today's AI runs in data centres, and it has to. The way neural networks are
trained requires every part of the network to exchange information with every
other part, in lockstep, many times a second. That is only affordable when the
machines sit in one building on a dedicated network. The consequence is that the
scale of an AI system is set by how much capital one organisation can raise.

Meanwhile there are billions of computers, phones and consoles sitting idle in
people's homes. They are already bought and already paid for. They are connected
by the ordinary internet — slow by data-centre standards, unreliable, and
constantly being switched off.

**This project asks whether a neural network can be built that runs on those
machines instead.** Not a faster network: a differently-shaped one, where no part
ever has to wait for a global picture, and where a machine leaving in the middle
of a thought is a normal event rather than a failure.

**And the obvious version of this already exists, which is where the argument has
to start.** Several projects already run large AI models across volunteer
machines over the ordinary internet, and they work. They manage it by keeping the
conventional training method and engineering around its need for lockstep —
splitting the model into stages, routing work to whoever is free, scheduling
around the slow. So *"AI on people's own computers"* is not the thing that would
make this project worth doing. It is done.

What is unclaimed is the narrower thing: **removing the need for the lockstep
rather than tolerating it.** A system built that way could do things the others
structurally cannot — keep learning while it runs, rather than in training runs
that finish; carry on when a machine vanishes mid-thought; rearrange what it
knows as new things arrive, instead of being fitted once and frozen. **That is
the payoff to aim at, and it is a claim about capability rather than about
price.**

Cheapness and openness follow if it works, and they are worth having. They are
not the reason to try, because a system that merely made distributed training
cheaper would be competing with things that already do it.

If it does not work, the useful outcome is a clear, measured statement of
*which specific constraint kills it*. That is worth having on its own.

---

## 1. The goals

**Primary — AGI, by way of a map of concepts.** A neural network distributed
across the internet at scale, trained to learn **how concepts relate to one
another** rather than to predict what comes next.

John's statement of it, 2026-07-28:

> The goal is a system that is able to learn the relationships between concepts
> — with the goal being that once it has a good map of most all concepts, and is
> able to be aware of how a given concept relates to some other concept, my hope
> is that a model with that sort of training, rather than being based on
> predicting things, will then be able to **reason** about things, and
> potentially have something like original thoughts — to operate more similarly
> to the way humans operate, and hopefully be close to AGI.

**Restated by John 2026-07-29**, after a week of work drifted onto next-token
text and had to be walked back (decisions 135-142, notes 046 and 047). Recorded
at his request, in his words:

> I want the system to be able to meaningfully store or learn one given idea, one
> concept, one thing, and how that relates to other things and concepts, and have
> some ability to learn and be aware of the differences and the interrelations
> between those concepts.
>
> We don't care about figuring out what the most likely next token is. We care
> about being able to actually process a query — or video input, or a picture,
> whatever it happens to be — and form a response based on its awareness of all
> of the concepts in that question.
>
> Can it holistically understand a given sentence, or a question, and then form a
> response based on its awareness of all of the concepts in it? **The goal is
> understanding, not prediction.**

**And the multi-modal ambition is part of the goal, not a later luxury.** He
wants as many input and output formats as the design can carry: *"the core of it
is, the goal is understanding, not prediction... I'd love for this to be
multi-modal."* Text is the easiest medium to start in and is **not** ruled out —
the requirement is that the architecture must not be *limited* to it. That is
what `concepts.py`'s surface-to-concept indirection is for, and it is why one
concept must be able to have many surfaces.

**The bet, stated so it can fail:** a system whose training objective is
*relational* rather than *sequential* will reason rather than continue. If a
model with a good concept map turns out to reason no better than a next-token
model of the same size, the central premise is wrong.

**Secondary — an LLM replacement, and it is DEFERRED, not merely lower.** If the
same architecture works as an LLM replacement, or if lessons from building it
transfer to running conventional LLMs across devices without data centres, that
is worth having. **John's ruling: explore it separately, after the main thing.**
It is not a parallel track competing for decisions now.

**The ordering is load-bearing.** Where the two conflict, the primary wins. A
design that would make a better distributed LLM but forecloses generality is the
wrong trade — and a measurement that only makes sense for the secondary goal is
not a reason to change the architecture today.

### 1.1 The clarification that changes what counts as success

**Raw efficiency against a GPU is not the deciding question.** The world already
has billions of idle devices. A model that is *less* efficient per FLOP but runs
on hardware that already exists and is already paid for can still meet both
goals. "Would a single GPU beat this" is a footnote, not a blocker.

What matters instead is whether the thing works on **consumer devices that are
unreliable, heterogeneous, and constantly leaving.**

### 1.1a The differentiator is CAPABILITY, not cost — and the reason is that the cost version is taken

**Approved by John, 2026-07-31.** This document previously argued from cost and
access: data centres are expensive, idle consumer machines are free, so a system
that used them would widen who can build. That argument is not wrong and it is
not sufficient, because **there is an established field already doing it.**

**Decentralised training on volunteer hardware exists and ships.** Learning@home
and Hivemind, Petals, Nous Psyche, Pluralis Agora — large models trained and
served across unreliable volunteer machines over the ordinary internet.

> **Rule 1 applies and the label matters more than the list.** These were read as
> search results and abstracts, not as papers, in a search that was not wide. They
> are **leads**, and no property of any of them may be quoted as established here
> until one is actually read. What is being taken from them is not a number; it is
> the fact that the category is occupied, which a search result is sufficient to
> establish.

**They keep backpropagation and engineer around the synchronisation** — pipeline
stages, expert routing, scheduling around stragglers. That is a real and
successful strategy, and it means *"distributed AI on consumer hardware"* is a
**solved problem statement**, not this project's contribution.

**So the bet has to be the narrower one, and it is this:** that a local rule
**removes the need for the global step**, rather than tolerating it. Everything
in §3 is downstream of that sentence.

**Which forces the payoff to be something the synchronised systems cannot do at
all.** Being cheaper is not it — they are already cheap. What a system with no
global step can be that a scheduled-around one cannot:

- **It never stops learning.** C4 is not a preference; it is the differentiator.
  A system whose training is a run that ends can be distributed with enough
  engineering. A system with no run and no end is a different object.
- **It survives arbitrary churn mid-computation**, rather than treating a
  departure as an exception to recover from. C3.
- **It keeps reorganising what it knows** as new things arrive, instead of
  fitting a structure once over a graph fixed at training time.

**The practical consequence for what gets measured.** A result showing this
architecture can be spread across machines is **not evidence for the project** —
that capability is not in dispute and is not ours. The evidence that counts comes
from the gates the alternatives structurally cannot reach, which is why G6 and G7
were added and why C4 is a constraint rather than an aspiration.

**And the risk this creates, stated rather than discovered later.** If continual
local reorganisation turns out to buy nothing over a frozen global fit, the
project has no differentiator at all — not a weaker one. That is exactly the open
hypothesis §1.2b already records under *"the open hypothesis, labelled as one"*,
and it is now the central one rather than a caveat at the end of a section.

### 1.2 What the model is trained to DO — relationships, not text

**Stated by John, 2026-07-28, and it is the thesis the project exists to test.**

> Most models currently just train on next-token prediction, and therefore at the
> end of the day they're taught to predict text — that's what they're doing. My
> idea here is, instead of focusing on predicting text, train the model to
> understand the relationships between things: to associate a given thing in the
> context of all other things.

The human analogy he gives is the mechanism, not decoration: we learn because
something *matters* — a positive or negative feeling marks an episode worth
keeping, and what gets stored is "doing this leads to that." Association under
salience, not sequence continuation.

**This is a claim about the OBJECTIVE, and it is load-bearing in a way the
architecture already reflects.** An associative store binding `(a, b)` pairs is a
relational substrate; training it to predict token `t+1` asks it to be a language
model built out of the wrong parts. That mismatch is measured, not argued —
character-level bits saturate at ~16,000 characters (decision 63) and the store's
effective rank sits near 3 whatever the width (decision 115), because a character
bigram table is intrinsically low-rank. **Chasing text is chasing the thing this
substrate is worst at.**

#### ⚠ This CONTRADICTS §5's recorded candidate, and §5 is the older document

§5 carries **self-supervised temporal prediction** — each unit predicts its own
next input — as the credit-assignment candidate, argued in
[note 002](docs/archive/notes/002-which-credit-assignment-scheme.md). One of its three
stated advantages is *"it is the same objective family as an LLM."*

**That advantage is now a liability under this section**, and the conflict is
recorded here rather than quietly resolved because both documents were written
deliberately. The reconciliation is that note 002 was choosing how credit is
DELIVERED — locally, with no signal in transit that can be late — and that
argument survives intact. What does not survive is next-input prediction as the
thing being *learned*.

**The replacement is relational self-supervision**: state facts, hide one,
predict it. Fully self-supervised, no marked questions, no labels — so it still
satisfies the "needs no labels" requirement §5 rests on — but relational rather
than sequential. That is the live work and it is tracked in
[DECISIONS.md](DECISIONS.md).

### 1.2b What UNDERSTANDING means here, and why grounding is load-bearing

**Stated by John, 2026-07-30, and this is the positive half of §2's refusal.**
Saying what the project is not aiming at was not enough: a benchmark was proposed
that quietly reintroduced sequence prediction because it was convenient to
measure, and the refusal in §2 did not catch it. This section exists so the next
such proposal is caught by the goal rather than by a conversation.

**The operational definition.** In his words: understanding a thing is *"knowing
what it is in relation to everything else."* Not a definition by properties, and
not a definition by what typically follows it — a concept's meaning is its
**position in the structure of relations to every other concept.**

That is a claim with teeth, because position in a relational structure is
measurable, and it separates cleanly from prediction. A system asked *"what
follows this?"* can succeed by memorising frequencies. A system asked *"what
relation holds between these two, given ones you were told about others?"* cannot.

**The distinction §2 needs and did not state: a training SIGNAL is not an
OBJECTIVE.** Prediction as a mechanism for generating error is unobjectionable
and biology uses it constantly. Prediction as the quantity being optimised
produces a predictor. §2 forbids the second; nothing here forbids the first, and
conflating them is how the wrong benchmark gets adopted for the right reasons.

#### Multimodal grounding is a REQUIREMENT, not a later phase

John, 2026-07-30: an image of a dog, a video of a dog, a sound of a dog and the
word *dog* should all trigger the same concept — *"not as a predictive thing, but
as a recognition of, oh, that's what it is."*

**This is not scope creep, it is the answer to the standard objection against
§1.2's own thesis.** A purely relational system knows how its symbols relate to
one another and nothing about what any of them refers to. Grounding the same
concept in several modalities is what dissolves that, which makes multimodality
load-bearing for the thesis rather than an application of it. A design that
forecloses it has broken §1.2, not deferred a feature.

**And it is TWO problems, which must not be budgeted as one:**

- **Agreement within a modality** — two nodes given the same input produce the
  same concept id. This is the quantiser question already on the gate ladder, and
  John has ruled that a borrowed quantiser is acceptable and possibly preferred.
- **Alignment across modalities** — an image and a word name the same concept.
  This does **not** fall out of quantisation. Two independently quantised
  modalities never agree by accident.

  **REFINED by John, 2026-07-30, and the refinement is the design.** Identity is
  not computed, it is **learned from temporal co-occurrence** — the way a child
  learns that a picture, a bark and the word *dog* are one thing, by meeting them
  together, repeatedly, in varying contexts.

  This narrows the quantiser's job rather than removing it: **the quantiser
  answers ADDRESSING — how a non-text input becomes an id at all — and identity is
  learned on top.** Both halves are recorded in
  [discrete-surface-ids.md](docs/options/discrete-surface-ids.md), including why
  this answers an objection that record had already raised against itself.

  Three consequences follow and are stated so they are not rediscovered:

  - **It is the SAME mechanism as the rest of the system.** Binding co-occurring
    things into an associative store is what the model already does, so grounding
    is that mechanism applied to a stream carrying more than one kind of input —
    not a new component.
  - **Co-occurrence alone binds everything present.** A dog, a sofa and a face all
    co-occur with the word. What disambiguates is **variation across situations**:
    the constant across every occasion is the concept. That is a contrastive
    signal, which this project already has a rule for.
  - **It does not remove the perceptual front-end**, only the requirement that it
    be *aligned*. Something must still map pixels to a representation where
    similar things land near each other. That borrow is far smaller and less
    contentious than an aligned multimodal space, and it is consistent with §2's
    rule on prior art.

  **THE TENSION AND ITS RESOLUTION — John, 2026-07-31.** C1's concept partitioning
  needs a DETERMINISTIC owner per concept, hashable and computable by any node
  without asking; learned identity is negotiated and therefore unhashable. That
  read as a conflict between two commitments the project had made deliberately.

  **The exit is that the concept was never the address.** A concept gets no id: it
  is the **equivalence class** that falls out of the co-occurrence links, reached by
  starting at any member and walking. What needs a stable address is a *percept* —
  an image code, a word — and those already have one. Nothing addresses *dog*.

  Two keys carry the mechanism, and both are hashable:

      owner(surface id)   everything ever learned about one percept, DURABLE
      owner(time bucket)  that two percepts occurred together, TRANSIENT

  **Time is the join; the percept's owner is the accumulator.** A rounded arrival
  timestamp lets two nodes agree that what one saw and another heard happened
  together, computed locally with no message sent — the same property `Ring` gives
  concepts. The link is then written to the percept's owner, where it accumulates
  over that percept's lifetime, so cross-situational learning becomes local counting
  at a fixed address: the sofa fades because it appeared once, the word persists
  because it appears every time. **No gather, and the hot spot is transient because
  nothing durable lives at the time key.**

  It is the fast-store-and-durable-store shape the project already has, with time
  addressing the fast tier and percept id the slow one. The quantiser ruling is
  untouched and identity stays learned. Records:
  [identity-without-a-global-id.md](docs/options/identity-without-a-global-id.md)
  and [time-bucket-join.md](docs/options/time-bucket-join.md), both carrying what it
  costs and what would refute it.

#### Intervention may be load-bearing, and the evidence is that it turned up twice

**Hypothesis, labelled as one, 2026-07-30.** Watching things co-occur cannot
separate *"these appear together"* from *"these are the same thing."* Acting on
one and seeing what follows can — move the dog and see what moves with it.

The reason to take it seriously rather than file it as speculation: **the same
distinction blocked a completely unrelated part of this project on the same day.**
The search for a label-free way to tell which memories are worth keeping failed on
exactly this — counting how often something recurs cannot distinguish recurrence
from demand, and the conclusion there was also that only intervention reaches it.

Two independent routes to *"correlation is not enough, and the missing ingredient
is acting rather than observing"* is worth recording. **It is not evidence that
either conclusion is right**, and it is explicitly not a requirement: it is a
reason to expect a passive stream to be insufficient, and to design so that adding
action later does not require a rewrite.

#### What it will look like from outside, and why that is not the goal

The first interface is expected to be **text chat — slower than a language model,
and that is accepted.** John is explicit that this is a surface rather than the
system: he expects an internal stream of thought running whether or not anyone is
asking, and eventually something simulating a body. §1.2a's *"constant input and
output"* is the same commitment seen from the architecture side.

**So a chat transcript is not evidence about §1.2b**, and fluency is not the
quantity. What is being built is a thing that can be *asked* about relations,
where the asking is an interface onto a structure that exists whether or not
anyone asks.

#### The falsifier

> **If it can only answer what it was told, or only in the modality it was told
> in, it has INDEXED rather than understood.**

So the test is a relation it was never given, that follows from ones it was, with
the concepts introduced through one modality and queried through another. Both
halves are required: composition without cross-modality is a knowledge graph, and
cross-modality without composition is a lookup table.

#### The open hypothesis, labelled as one

Learning a concept's position from its relations is what knowledge-graph embedding
methods already do, and their results are respectable rather than transformative.
**If this project expects to differ, the reason has to be stated in advance rather
than discovered afterwards**, or it will rediscover their ceiling with extra
steps.

The current candidate, and it is a hypothesis with no measurement behind it: those
methods learn one global embedding by centralised optimisation over a graph fixed
at training time, where C4 requires a structure that keeps reorganising as new
things arrive and never freezes. **Whether continual local reorganisation buys
anything over a frozen global fit is untested**, and it is exactly the kind of
claim §1's own standards say to label rather than assume.

### 1.2a Directions John wants explored — not requirements, and not idle either

**Stated 2026-07-28.** None of these is a constraint. Each is a direction he
believes is worth pursuing, recorded so it is not rediscovered as a novelty and
so a design that forecloses one is recognised as having done so.

**Continuous input and output, rather than request-and-response.** In his words:
*"as long as we're awake we have constant input and output, and frankly I would
argue humans are just input-output machines."* A system running across devices on
the internet has no reason to be idle between questions.

This is not decoration on the architecture — **it changes what correctness
means.** A request-response model owes an answer to each input. An always-on
model has inputs arriving and outputs emitted continuously, and *"you don't
expect: this input got in, and I'm going to get this output."* A participant
being behind stops being a failure and becomes a normal state.

> **It bears directly on the barrier problem.** John's framing: event sourcing
> and event-driven design as the way to avoid needing a summation at all — take
> whatever you have at a given time, aware that it may be behind. That is a
> different answer from the deadline in `distributed.py`, which still settles a
> step. **A stream has no step to settle.**

**Self-modifying structure.** The network changing its own connectivity over
time, rather than having a topology fixed at design time — *"being able to modify
its own brain."* Explicitly not a top-level goal, and explicitly something he
wants explored: a continuously-running distributed system that restructures
itself as it learns is where he thinks the interesting behaviour is.

**Both are compatible with C4** (perpetual learning) and neither is implied by
it. C4 says the weights never freeze; these say the *inputs* never stop and the
*structure* is not fixed either.

### 1.3 Scale is a first-class consideration, and benchmarks at this scale are not the goal

**Stated by John, 2026-07-28.** Something that works at one scale routinely fails
at another, and the target scale is far above the one every measurement here has
been taken at.

- **Do not optimise for a benchmark at the current scale** unless the result
  transfers to the scale being aimed at. Meeting a bar at small scale to prove a
  mechanism works before scaling it is legitimate and often necessary; tuning to
  a bar that will not matter later is waste.
- **When a decision is scale-specific, say so where it is made**, with the
  condition that should trigger a re-evaluation. A decision that silently becomes
  wrong at scale is worse than one that was never taken.
- **Give scale-specific choices a seam.** A component that will need replacing at
  scale should be replaceable without touching its callers —
  `openplexus/keys.py`, `openplexus/retrieval.py` and `openplexus/search.py` are
  the existing pattern.

`docs/SCALE.md` is where scale-dependent decisions are registered.

### 1.4 The operative research question

The goals above are a direction, not something a run can settle. The question
that experiments actually answer is:

> **What is the largest class of problems learnable using only local information
> and bounded asynchrony?**

Every design decision should be traceable to that sentence. If a mechanism does
not enlarge that class, or does not preserve locality while enlarging it, it
does not belong here however good the numbers look.

---

## 2. What is explicitly not the goal

Stating these because each one, unstated, quietly redirected the predecessor
project.

- **Not biological fidelity as a target.** Resembling a neuron is not a
  property this project optimises. Biology is nonetheless a first-class
  *reference* and should be used freely — see §2.1, which is the affirmative
  half of this and matters more than the restriction.

- **NOT NEXT-TOKEN PREDICTION.** John, 2026-07-29, asked for this stated
  outright: *"text prediction is not the goal... we don't care about figuring
  out what the most likely next token is."*

  This was implicit in §1 from the start — *"trained to learn how concepts
  relate rather than to predict what comes next"* — and being implicit was not
  enough. Decisions 135–142 spent a week measuring this model in bits per token
  on next-word text, and [note 047](docs/archive/notes/047-what-the-store-can-hold-on-text-is-an-n-gram.md)
  is the finding that closed it: **on a next-token objective the only relation
  the store can express is n-gram shaped, and a count table does that exactly.**
  The objective was the ceiling, not the store.

  **The distinction to keep, so this is not over-corrected:** text as *input* is
  fine and always was. Text-*prediction* as the score is what is excluded. A
  model asked what it HOLDS is a different measurement from one asked what comes
  next, and only the second is bounded by counting.

- **Not efficiency per FLOP.** See §1.1.

- **Not novelty for its own sake.** Prior art that already solves a
  sub-problem gets used and cited. The contribution, if there is one, is the
  system that works under these constraints — not the individual parts.

  **AMENDED 2026-07-28, and the amendment matters.** John: *"if there is a
  solution to a certain problem that meets our stated goals better than any
  existing work does, definitely go for it — at least try it out."*

  The rule forbids novelty as a *goal*, not novelty as an *answer*. Where the
  literature's solution was built for different constraints, taking it is the
  mistake and inventing is the correct move. This project is already doing it:
  `openplexus/search.py` — commit to a branch, follow it, and score it by
  whether its endpoint matches the entity the question names — is not from a
  paper, and neither is gating that search on the decode margin. Both were
  measured before being trusted, which is the part that must not be skipped.

  **The bar for a novel mechanism is the same as for a borrowed one**, and it is
  the only thing keeping this honest: predictions registered first, a control
  that could fire, and a null reported as a null.

- **Not a working product.** This is a research project whose first job is to
  find out whether the central bet is wrong, as cheaply as possible.

### 2.1 Biology as a reference — used deliberately, and often

**Biology is the one existence proof of a system that learns under exactly the
constraints in §3.** It runs on local information, tolerates tens to hundreds of
milliseconds of conduction delay, and loses components continuously without the
whole failing. No engineered system does all three. That makes it the single
most valuable source of hypotheses available here, and it should be consulted
whenever a design question is open. Where evolution has already solved a problem
this project also has, understand that solution before inventing another one.

Two distinctions keep that productive rather than misleading.

**First: separate what neurons *compute* from what they merely had to *cope
with*.** Ion-channel kinetics, all-or-none spikes (axons attenuate; digital
links do not), and metabolic limits are constraints of wetware, and copying them
imports a cost without the reason for it. Dendritic computation, local plasticity
rules, homeostasis, and delay tolerance are candidate *computations*, and those
are worth taking seriously. When borrowing, say which of the two it is.

**Second: biology is a reason to try something, never a reason to keep it.**
"The brain does it this way" is a hypothesis of exactly the same standing as any
other, and it is subject to the same gates in §4 and the same evidence rules in
`CLAUDE.md`. This is where the predecessor drifted: its four headline departures
from conventional design were each justified biologically, and an audit later
found every one of them either inert in every configuration that had produced a
result, or refuted outright. The biological motivation was never the problem —
treating it as evidence was, because it made those mechanisms feel already
justified and so nobody measured them for a year.

So: read biology first and read it widely. Then measure it like anything else.

---

## 3. The three constraints that define the design space

These are the project. Everything else is negotiable.

### C1 — Locality

> **No operation may require globally synchronised state.**

A mechanism needing a population sort, a global mean, a pooled matrix, or a
barrier is a violation, and gets flagged as one **even when it improves the
numbers.**

If an exception is ever admitted, it is named as an exception, in one place, with
what depends on it — never absorbed silently.

#### AMENDED 2026-07-27 — C1 is a means, and this is the end

**John's ruling, in his words: "our real constraints are just *does it work over
the internet* — if something still meets that, it's good to go."**

C1 as written above was a *proxy*. It was adopted because backpropagation is a
global barrier moving data proportional to parameter count, which is why deep
networks need tightly-coupled hardware — so "no global state" seemed like the
same requirement stated structurally. **It is not the same requirement, and note
036 is where that became clear.**

Edmond & Kadmon (arXiv:2502.20580) report that error-feedback dimensionality
scales with **task complexity, not network size** — rank 10 sufficed to match
backprop on CIFAR-10 across an MLP, a CNN and a ViT. That is still a backward
chain, so old-C1 forbids it. But the message is **tens of floats per hop**, and
a backward sweep carrying forty bytes over a 150 ms link is not the thing that
forces a data centre. **The structural rule was ruling out designs the actual
goal permits.**

**So the test is now:** can this run across consumer machines over ordinary
internet links — bounded bytes per step, no dependence on a barrier that stalls
when one machine is slow, and correct behaviour when a machine vanishes?

**What this does NOT license.** A global all-reduce is still out, even a
twelve-byte one: note 036 records that zeroth-order and evolution-strategy
methods look local until you notice their scalar broadcast is a barrier wearing
a small payload. **The distinguishing question is whether progress stalls when
one participant is slow or gone**, not how many bytes moved. A bounded one-hop
message to a named neighbour passes; a collective everyone must join does not.

Every result recorded before this date was measured under the stricter rule, so
none of them is invalidated by the amendment — they were simply achieved with
one hand tied. **Any design admitted under the amended rule must say so
explicitly**, exactly as the old exception clause required.

### C2 — Bounded asynchrony

Information arrives late, out of order, and at varying delay. The design must
state a **bound** it tolerates, and be correct — ideally bit-identical — below
that bound. A design that merely degrades gracefully is weaker than one with a
stated, tested bound, because only the latter can be engineered against.

Intercontinental round trips are ~150 ms. A mechanism whose credit signal must
arrive within a few milliseconds cannot be distributed, no matter how well it
learns locally.

**The bound is now stated, and it is `d_max` ≈ 640 ms** — the first time this
constraint has had a number rather than a principle. It is one parameter doing
two jobs, as note 003 argued: within it a slow source is a straggler, beyond it a
dropout, so C2's bound and C3's churn timeout are the same quantity.

**Measured, not chosen**, and the derivation belongs with the number: it is three
times the 99th-percentile vote round trip on the worst link tested, following
SWIM's rule that a protocol period must be at least 3 × RTT. See
[docs/SCALE.md](docs/SCALE.md) for the grid it came from and what would move it —
**it is a floor from six simulated links, not a universal constant**, and a real
WAN raises it.

### C3 — Churn

**Machines leaving is the normal case, not an edge case.** A consumer device is
switched off, put to sleep, or has its network drop, constantly and without
warning. The system must be designed from the start on the assumption that any
node can vanish mid-computation and that the remainder continues.

This has never been tested in the predecessor project, because nothing ever left.

### C4 — Perpetual learning

**Stated by John, 2026-07-27.** The system never freezes. There is no training
phase that ends and no deployed checkpoint that stops adapting: it learns from
what it sees for as long as it runs.

**This is a constraint, not an aspiration, and it rules things out.** Three
consequences follow immediately and each one has already changed a decision:

- **The deployed regime is ONLINE.** A result obtained with a full-batch
  optimiser over a fixed dataset is evidence about what the features CONTAIN,
  not about what this system can reach. Note 037's offline result is exactly
  that, and it is why the online rerun exists.

  **CORRECTED 2026-07-28.** This first read "online and SINGLE-PASS", which is
  stricter than C4 requires and was my derivation rather than John's
  constraint. **C4 forbids stopping, not revisiting.** A system with a replay
  buffer that never freezes satisfies it completely; people revisit constantly
  and are not frozen checkpoints. The stricter reading made decision 71's
  two-pass result look compromised when it was admissible, and would have ruled
  out replay — which is one of the few known answers to the catastrophic
  forgetting C4 makes first-class.
- **Catastrophic forgetting becomes a first-class failure mode**, not a side
  quest. A model that learns forever also forgets forever. This promoted an
  existing negative result on a different question into a load-bearing one — see
  [DECISIONS.md](DECISIONS.md) for where that stands.
- **Evaluation should be PREQUENTIAL** — predict the next item, then learn from
  it, and score the predictions made along the way. A train/test split measures a
  system that stops, which is the thing C4 forbids.

  **CORRECTED 2026-07-29.** This read *"Every number in this project so far comes
  from a split"*, which stopped being true and then stayed on the page. Decision
  117 scored prequentially — with the n-gram baselines scored prequentially too,
  because a bigram fitted on the whole corpus is not a fair opponent for a model
  given one online pass. **What holds is the weaker claim:** prequential
  evaluation is what C4 demands and is still the exception rather than the norm.

C4 does not conflict with C1–C3 and sharpens them: a node that must keep learning
forever cannot rely on ever having seen the whole corpus, which is the same
situation a node joining late is already in.

---

## 4. Falsification — the gate ladder

The goals in §1 are too large to test. This ladder is what actually gets tested.
Each gate names the outcome that **refutes** the project at that stage, so the
project can be killed cheaply and early rather than expensively and late.

The ordering is by *cost of finding out*, cheapest first. **No gate is skipped,
and no gate is passed on a single run** (rule 3).

| gate | the question, plainly | refuted if |
|---|---|---|
| **G0 — the instrument** ✅ **PASSED** | Is there a task that a random, untrained substrate *cannot already do*, and that is learnable from local information at all? | No such task can be constructed. Then nothing downstream can be measured, and the project has no instrument. |
| **G1 — does it learn** ✅ **PASSED** | Does a purely local objective beat the random substrate on that task? | The margin is null across seeds. The central bet is wrong. |
| **G2 — asynchrony** ✅ **PASSED** | Does the margin survive realistic delay, jitter and reordering, up to a stated bound? | The margin vanishes below the bound the internet actually imposes. |
| **G3 — churn** ✅ **PASSED** | Does the margin survive nodes leaving mid-run and rejoining? | Losing a node degrades the whole rather than a part, or recovery costs more than the node was worth. |
| **G4 — bandwidth** ⚠️ **PASSES ON ONE SEED** | Does the required cross-machine traffic fit consumer broadband? | The traffic needed for the margin exceeds what a home connection carries. |
| **G5 — scale** ⚠️ **CONTESTED** | Does the margin hold or grow as the network grows? | The margin shrinks with scale. Then it is a small-model curiosity, not a route to either goal. |
| **G6 — composition** ⬜ **NOT REACHED** | Can it answer about a relation it was never given, that follows from ones it was? | It can only return what it was told. Then it has INDEXED rather than understood, and §1.2b's definition is not being met however good the recall is. |
| **G7 — grounding** ⬜ **NOT REACHED** | Does a concept introduced through one modality answer when queried through another? | The same concept cannot be reached from two modalities under these constraints. Then §1.2b's answer to symbol grounding fails, and the relational structure is a closed symbol system. |

**G6 and G7 were added 2026-07-30 and they are the operational form of §1.2b.**
The ladder previously stopped at scale, so every gate could be passed by a system
that had understood nothing — which is how a sequence-prediction benchmark came to
be proposed without any gate objecting. They sit in this order because G6 is cheap
(the relational tasks already in `openplexus/tasks/` ask exactly this question) and
G7 is expensive (it needs a representation built from paired data, per §1.2b).

**Both are required and neither substitutes for the other.** Composition without
cross-modality is a knowledge graph; cross-modality without composition is a
lookup table.

**This table is the only place a gate verdict is written.** G4 passes on one seed
with training traffic still unmeasured; G5 was refuted, withdrawn, then refined
three times, and machine *size* rather than machine *count* is what binds. The
numbers behind every verdict, and the retractions, are in
[docs/archive/goals-results-log.md](docs/archive/goals-results-log.md); the
current reading is in [DECISIONS.md](DECISIONS.md).

**G0 is first for a reason, and it is the correction of the predecessor's single
most expensive mistake.** Choosing a benchmark that defeats trivial baselines is
necessary but not sufficient — that benchmark must also leave a learning rule
something to do. In the predecessor, it did not: a frozen random substrate
already scored 0.802, total headroom to a strong non-local model was ~0.19, and
existing non-learning mechanisms took ~40% of it. Nearly a year of learning-rule
work was measured against a ceiling that was never there.

**The G0 acceptance test is therefore explicit:** before any learning mechanism
is written, the task must be shown to have substantial headroom between what a
random frozen substrate achieves and what a strong non-local reference achieves,
with both measured, multi-seed, and with the base rate of a constant predictor
reported alongside.

---

## 5. Sequencing — what is chosen before what

The predecessor's stated regret is the ordering: it picked biologically-motivated
mechanisms first and then looked for a learning rule that fit them. **Credit
assignment is the hard part and the binding constraint, so it is chosen first and
the substrate is chosen to serve it.**

1. **The task** (G0) — the instrument. Nothing is measurable before it exists.
2. **The credit-assignment scheme** — chosen against C1/C2/C3 *before* any
   substrate exists, on paper, with the locality and latency argument written
   out.
3. **The substrate** — the minimum representation that lets the chosen scheme
   work. Not a catalogue of interesting mechanisms.
4. **Distribution** — the transport and the churn model, against a substrate
   that has already passed G1.

> **REVIEWED AND KEPT, 2026-07-29.** Archiving this section was considered and
> declined: the banner names what survives and what does not, and a reader reaches it
> before the prose, so the risk of acting on a rejected candidate is already handled.
> Removing it would also lose the reasoning that makes §1.2 legible as a *change of
> mind* rather than as a position held from the start.
>
> **⚠ SUPERSEDED IN PART, 2026-07-28. Read §1.2 first.** The candidate below is
> next-INPUT prediction, and §1.2 records John's ruling that predicting the next
> thing is precisely what this project is not for. What survives is the argument
> about credit DELIVERY — no signal in transit, so latency costs memory rather
> than credit precision. What does not survive is the second bullet, *"it is the
> same objective family as an LLM"*, which was written as an advantage and is now
> the objection.

The current best candidate for step 2, carried forward as a *hypothesis* and not
a decision, is a **predictive / self-supervised local objective**: each unit's
error comes from comparing its own prediction against its own next input. Three
properties recommend it, and all three are arguments rather than measurements:

- **It dissolves C2 rather than working around it.** There is no broadcast
  signal, so there is nothing that can arrive late.
- **It is the same objective family as an LLM.** Next-token prediction is
  next-input prediction, which matters directly for the secondary goal.
- **It needs no labels.** A network running on strangers' devices cannot assume
  a labelled target at every node. This is a *primary-goal requirement*, not a
  preference.

The decision is deferred to the plan. This section records the candidate so the
plan can argue against it.

---

## 6. Why this exists — the predecessor, and what transfers

This project replaces `plexus` (`Mako88/submenu`, branch
`claude/bio-inspired-neural-model-ohhrp6`; handoff snapshot at
`PLEXUSBRIEF.md`). It is a restart, not a fork, for two stated reasons:

1. **The architecture was built without a plan first.** Mechanisms accumulated
   and the design document was written to describe them, so there was never a
   document that could reject a mechanism.
2. **The framing was "biology, but better."** The right framing is that the
   machines and the network already exist — so build for *those*. The two
   framings do not select the same design, and the second is the one that serves
   the goals.

**The name keeps the lineage on purpose; the architecture does not.** "Open"
carries the reframing — the machines are already out there, already owned by
people, and the network between them is the public internet rather than a
private fabric. Nothing in the predecessor's code is inherited. What is
inherited is its record of what did not work, which is the most useful thing it
produced.

### 6.1 What transfers, and at what confidence

**Nothing here is a measurement of this project.** These are prior results about
a different architecture, recorded so they can be tested rather than repeated,
and so no time is spent rediscovering them. Rule 1 applies: none of these may be
quoted as a property of this system until this system is measured.

| inherited finding | transfers? | why |
|---|---|---|
| **Emission-time indexing makes jitter free below a stated bound** — a run stays bit-identical under arbitrary reordering and lateness below `delay_min`; tolerance is exactly `delay_min − 1` | **High — as a technique, to re-derive** | This is a property of the indexing scheme, not of that model. It is the most defensible idea the predecessor produced and it directly serves C2. Re-derive it here rather than importing it. |
| **A short-term-plasticity-like mechanism carried memory across delays** (0.527 → 0.864 there) | **Medium — as a hypothesis** | Large effect, but measured on that substrate and that task. Treat as a candidate, not a result. |
| **The sparse-event bandwidth arithmetic** — a large sparse network at 1 kHz emits ~10⁸–10⁹ events/s; at 1% of connections crossing the network that is ~50 MB/s, at 10% it is ~500 MB/s | **High — it is arithmetic** | The order of magnitude follows from sparsity and rate, not from architecture. G4 is where this becomes real, and the fraction crossing the network was **never swept** there. |
| **Memory, not compute, is the binding constraint at scale** (~16 bytes per connection) | **Medium-high** | Follows from any design storing per-connection state. The constant is design-specific. |
| **A broadcast supervised error signal is the least local, least scalable part of such a design — and measured inert** | **High — as a warning** | Directly informs §5: do not choose it. |
| **Three-factor learning with eligibility traces did not learn** (−0.003, p = 0.79) | **Low as a result, high as a caution** | Published work reports this family working. The discrepancy is more likely an implementation or task problem than a refutation of the family. Do not treat as settled. |
| Every measured constant — credit windows, decodability scores, neurons per core | **None** | Properties of that architecture and that benchmark. Both are being replaced. |

### 6.2 Prior work to read before building

Recorded from the predecessor's notes and **not yet re-read by anyone on this
project.** Every row needs checking before anything is built on it, and no claim
from them may be quoted until it has been.

| what | who | why it matters here |
|---|---|---|
| **Predictive coding** | Rao & Ballard (1999); Whittington & Bogacz (2017) | Local error from prediction mismatch. Directly the §5 candidate. |
| **e-prop** | Bellec et al. (2020) | Three-factor learning reported working. The most diagnostic discrepancy available. |
| **Forward-Forward** | Hinton (2022) | No backward pass at all. Maximally local. |
| **SORN** | Lazar, Pipa & Triesch (2009) | Closest existing system to the predecessor's substrate. Read for what makes their plastic part pay. |
| Dendritic / two-compartment error | Urbanczik & Senn (2014); Sacramento et al. (2018) | A local error computed within a unit. The natural way to *deliver* a predictive error. |
| Burst-dependent plasticity | Payeur et al. (2021) | Multiplexes credit and activity down one channel. |
| Feedback alignment / DFA | Lillicrap et al. (2016); Nøkland (2016) | Removes weight transport. |
| Reservoir computing | Maass (LSM); Jaeger (ESN) | **The G0 control.** Any result must be reported against a random frozen substrate, because that is what a reservoir already gives for free. |
| Federated / decentralised learning | (survey needed) | **Gap in the predecessor's reading.** This is the field that actually studies unreliable heterogeneous nodes, and it was never consulted. Read before designing distribution. |
| Gossip / epidemic protocols, CRDTs | **READ 2026-07-28** | C1 and C3 are distributed-systems problems with a distributed-systems literature. **No longer a gap:** SWIM and CRDTs were read, the detector was found to eject nodes permanently where SWIM says suspect-and-retry, and it was fixed. A second pass corrected a misreading of SWIM's retry interval — it was in the wrong unit, steps rather than seconds, and a step has no fixed duration. |

The last two rows are additions. The predecessor read neuroscience and machine
learning and did not read distributed systems, which — given that the goal is a
system distributed over unreliable machines — is the more likely place for the
answer to C3 to already exist.

---

## 7. What the stack must satisfy

The implementation language is **not chosen here.** It is an architecture
decision and it follows the plan. What the goals fix is the set of constraints
any choice has to meet:

- **Two different jobs, possibly two different answers.** The *research kernel*
  optimises for speed of asking questions and access to the prior-work
  ecosystem. The *eventual runtime* optimises for shipping to a stranger's
  Windows laptop with no toolchain installed. Assuming one language serves both
  is an assumption, and it gets stated rather than made.
- **The research kernel must not become the project.** The predecessor was
  measured *overhead-bound*, meaning per-operation cost dominated real work.
  Whatever is chosen must make it cheap to find that out early.
- **A reference implementation must exist that is obviously correct and slow**,
  against which any fast path is asserted. A fast path that has never been
  checked against a simple one is an unmeasured claim.
- **GPU availability is not a constraint** on the research kernel (§1.1), and
  the development machine's GPU is too old for current frameworks regardless.
  Do not design around it.

---

## 8. The five questions the plan had to answer

These were written before anything was built, in the order they needed
answering. They are recorded here because the design is downstream of them and
because two of them named the wrong parameter, which is worth a reader knowing.
**Each is answered in a note; none of them is live work.** What is live is in
[DECISIONS.md](DECISIONS.md).

| # | the question | where it was answered | the short version |
|---|---|---|---|
| 1 | What task passes G0? | [note 001](docs/archive/notes/001-what-task-passes-g0.md) | Associative recall, as a task *family with a difficulty dial* — a gap no local rule could close is as useless as no gap. G0 has since passed on MQAR and on the chain task. |
| 2 | Which credit-assignment scheme, and what is the argument that it satisfies C1 and C2? | [note 002](docs/archive/notes/002-which-credit-assignment-scheme.md), [note 008](docs/archive/notes/008-the-task-objective-mismatch.md) | Self-supervised temporal prediction. It converts latency from a *race* into a *buffer depth*: nothing is in transit that can be late, so delay costs memory rather than credit precision. Note 002 §7's proposed structured-filler fix had the sign backwards; note 008 §4 shows irreducible loss contributes no gradient, so random filler is correct. |
| 3 | What is the churn model? | [note 003](docs/archive/notes/003-the-churn-model.md) | The machine is the failure domain; assume no warning; detection is a separate liveness channel, because on a sparse substrate silence is normal. `d_max` is simultaneously the C2 asynchrony bound and the C3 churn timeout. Session lengths are Weibull with shape below 1, so uptime predicts remaining uptime. |
| 4 | What fraction of connections crosses the network? | [note 004](docs/archive/notes/004-the-bandwidth-budget.md), [note 009](docs/archive/notes/009-splitting-the-memory.md) | **The question named the wrong parameter.** The fraction is forced to ~1 under uniform placement. The free quantity is `D`, distinct destination *machines* per emitting unit, and it must be single digits to low tens. That forces local-dominant connectivity with sparse long-range links — derived from a bandwidth budget, not from biology. |
| 5 | Does the distributed-systems literature already answer C3? | [note 003](docs/archive/notes/003-the-churn-model.md) | Partly. The churn measurements transferred directly; federated learning's architecture did not transfer at all, being round-based with a central aggregator — a C1 violation twice over. **CORRECTED 2026-07-29: they are no longer unread.** SWIM and CRDTs were read on 2026-07-28, and SWIM did exactly what this row predicted — it named the false positives note 003 §5 names, and the detector was ejecting nodes permanently where SWIM says suspect-and-retry. A second reading found the retry interval carried in the wrong unit. |

---

## How this document stays honest

**Nothing in this document is a measurement.** That line opened it from the first
day and the document then accumulated 405 lines of running results at the bottom
anyway — gate verdicts, retractions, corrections of corrections — until it
carried two different answers for the same exponent two paragraphs apart.

So the rule is now structural rather than aspirational:

- **Intent and constraints here. Measurements nowhere near here.** Numbers live
  in `experiments/sweeps/`, reasoning in `docs/archive/notes/`, decisions in
  `DECISIONS.md`, which is the option tree and the current position both.
- **The only numbers permitted in this document are arithmetic or inherited**,
  and both are labelled as such — §6.1's table is the whole of it.
- `tests/test_goals_consistency.py` enforces this by refusing measurement-shaped
  text outside the permitted sections. A document that knows everything is a
  document that is always stale.
