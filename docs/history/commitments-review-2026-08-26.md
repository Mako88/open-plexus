# Review of OpenPlexus `commitments`

Reviewed on 2026-08-26 at commit `afd9fd12`.

This is an architecture-wide review of the `commitments` branch, covering the project
doctrine, learning loop, representation, generated worlds, deployment, transport, tests,
and current research direction. It is not a claim that every line has been exhaustively
audited.

## Overall assessment

OpenPlexus is not currently an AGI, but it is a coherent and unusually falsifiable attempt
at a continual relational world-model learner. The central bet is respectable: a prediction
can be wrong about something specific in a way a raw co-occurrence count cannot, and the
residue of repairing those errors might become a learned representation.

The implementation resembles an online classifier system combined with rule
specialisation, rudimentary inductive logic programming, abstraction by recurring
sub-scopes, and distributed monotone evidence. Its scientific discipline is stronger than
most speculative AGI projects: losing mechanisms are deleted, negative results are
recorded, mechanisms must be shown to run, generated worlds retain enumerable ground truth,
and population size and soundness are reported beside accuracy.

The principal limitation is the representation language. The learner's native expression
is still mostly:

> A conjunction of codes in one set-valued moment predicts one code in the successor
> moment.

That is useful, but it does not yet provide stable individuals, general role-filler binding,
learned relation arguments, negated consequences, quantification, causal structure,
long-horizon credit, or backward planning. Several richer distinctions currently exist only
because a front end annotates them. The project is therefore better described today as a
continual rule-learning experiment than as a concept-understanding system.

The plan is candid about most of these limitations. That candour is evidence in the
project's favour.

## Code-review findings

### P1: Distributed votes ignore `CommittingSettings.Deciding`

`Fleet` stores the brain settings but calls `gathering.Decide()` without passing
`_dials.Deciding`. `Gathering.Decide()` calls `Population.Decide` without an argument, so it
always receives the default `Deciding.Grounded` behaviour.

Consequently, a brain configured with `Deciding.Anyway` silently becomes a different brain
when deployed over a fleet. This violates the project's one-brain rule and can make an arm
measure correctly in-process while doing something else on the distributed path.

Relevant code:

- `src/OpenPlexus.Brain/Fleet.cs`, around line 178.
- `src/OpenPlexus.Brain/Asker.cs`, around lines 311-323.
- `src/OpenPlexus.Brain/Commitments/Population.cs`, `Population.Decide`.

Suggested repair: give `Gathering.Decide` a required `Deciding` argument, pass
`_dials.Deciding` from `Fleet`, and add a one-holder equivalence test for both enum values.

### P1: Moment ingestion is not retry-safe

`Brain.ReceiveAsync` advances `_seen[source]` before either asynchronous council operation
has completed. If voting or settlement throws or is cancelled, the caller never receives a
successful `Took = true` response, but replaying the moment is refused as a duplicate.

Moving the sequence assignment after the awaits is not sufficient: some holders may already
have learned, and a replay could then double-settle them. The operation needs a stable round
identity and holder-side idempotency if retry is meant to be safe. If the intended contract
is at-most-once delivery with possible loss, it should be stated and measured because it is a
material weakening of C2/C3 survival.

Relevant code:

- `src/OpenPlexus.Brain/Brain.cs`, around lines 306-342.
- `src/OpenPlexus.Brain/Holder.cs`, whose own documentation notes duplicate settlement is
  not handled.

### P1 for multi-source use: Fleet round state is shared mutable state

`Fleet` stores `_moment` and `_fleeting` in instance fields between `AskAsync` and
`TellAsync`. Two overlapping `Brain.ReceiveAsync` calls can overwrite those fields, causing
one vote to be settled using another moment. `Brain` also mutates `_seen`, `_typical`, and
`Supposals` without synchronisation.

Current `Round` usage is serial, so this defect is latent in today's principal runner.
However, source stamps are keyed per source and the route contemplates multiple inputs into
one brain, making concurrent receipt a plausible extension.

Suggested repair: either make serial receipt an enforced invariant with a mutex and a test,
or make `ICouncil.AskAsync` return a round-scoped context that is passed explicitly to
settlement.

Relevant code:

- `src/OpenPlexus.Brain/Fleet.cs`, around lines 59-60 and 160-187.
- `src/OpenPlexus.Brain/Brain.cs`, `ReceiveAsync`.

### P2: Distributed configuration agreement is trusted rather than checked

The host documentation correctly acknowledges that separate processes can be launched with
different dials. Nothing in the roster or wire handshake verifies the settings, seed,
codebook, or build identity. A mixed fleet therefore produces a scientifically invalid run
that may still look plausible.

Suggested repair: include a deterministic compatibility fingerprint in the roster handshake
and reject mismatches before the first measured round.

Relevant code:

- `src/OpenPlexus.Host/Program.cs`, around lines 275-287.
- `src/OpenPlexus.Brain/Bus/Posted.cs`, `Roster` and announcement handling.

## What is particularly strong

- Brain, front-end, world, join, and measurement responsibilities are deliberately
  separated.
- The learner operates continually without artificial train/test episode boundaries.
- Abstention is explicit instead of being counted as an ordinary error.
- Generated diagnostic worlds preserve enumerable ground truth and avoid corpus leakage.
- Identifiers and iteration order are designed for deterministic distributed execution.
- Mechanisms have reachability and exercise guards, not merely unit tests of isolated code.
- Accuracy is reported with population size, silence, coverage, and soundness, resisting
  easy memorisation wins.
- The project records refutations and standing objections rather than allowing them to
  disappear between sessions.
- The target-versus-instrument distinction makes it harder to mistake a diagnostic score
  for progress on the actual objective.

## Scientific and architectural risks

### A single spine world is not evidence of generality

`Roaming` is an excellent development target, but repeated adaptive work against one world
family can specialise both the mechanisms and the experimenter to that distribution even
when the world has no helpful switches. Diagnostic worlds establish attribution; they do
not establish external validity.

Keep the spine as the development target, but add a sealed transfer battery whose worlds,
seeds, symbol permutations, and modality combinations are not used to choose mechanisms.
The same frozen brain and dials should be evaluated across it.

### The distributed constraints may consume research effort before capability is established

C1-C4 create interesting and legitimate constraints, but a large share of the implementation
and tests now protects transport, sharding, liveness, and deterministic aggregation while
several foundational representational abilities remain open. Distribution should continue
to have equivalence tests, but most experimental effort should go toward capabilities that
cannot yet be expressed.

### Adaptive project-level selection is not corrected by the learner's significance test

The new per-round correction addresses candidate selection within a learning round. It does
not address hundreds of adaptive mechanism comparisons made against the same worlds and
seeds. A sealed evaluation set and predeclared checkpoint criteria are needed to keep the
research process from overfitting its instruments.

### Competitive baselines are still too distant

The open linear-probe comparison should move near the top of the route. Also compare against
an online decision tree or rule learner, retrieval/n-gram baseline, XCS-style baseline, and a
small recurrent model. The important claim is that commitments buy transfer,
compositionality, continual adaptation, or inspectability—not merely that they eventually
beat chance.

## Recommended near-term priorities

1. Add one-holder `Alone`/`Fleet` parity tests for every brain dial and fix `Deciding`.
2. Decide and enforce the delivery contract: retry-safe settlement or explicitly measured
   at-most-once loss.
3. Make rounds concurrency-safe before attaching multiple sources to one brain.
4. Build a sealed transfer battery with frozen dials and symbol permutations.
5. Move the linear-probe and simple-rule-learner comparisons upward.
6. Gate richer work on sharp representation tests: object permanence, novel role fillers,
   subject/object reversal, explicit negative consequences, multi-hop inference,
   intervention versus observation, backward planning, and surface-symbol renaming.
7. Split `Population`. It currently owns matching, indexing, voting, genesis, repair,
   abstraction, generalisation, subsumption, culling, and extensive instrumentation. The
   roadmap already calls for this, and the review agrees.

## Possible omissions from `THE ARCHITECTURE`

The current list is strong on concepts, attributes, relations, labels, temporal properties,
identity, negation, multiple grains, belief malleability, falsifiable learning, and original
conclusions. The following requirements are not stated strongly enough, even where pieces of
their mechanisms appear in `THE ROUTE`.

### Relations bind roles, not just participants

Saying that relations are concepts does not require the machine to distinguish who filled
which role. Without role binding, `Mary follows John` and `John follows Mary` can collapse
into the same bag, and a relation learned over one pair cannot necessarily generalise to a
novel pair.

Suggested architecture entry:

> **And a relation binds distinguishable roles to its participants.** What is learned of
> *Mary follows John* must preserve who follows and who is followed, while the relation
> itself transfers to participants never seen in those roles before.

### It distinguishes observation, intervention, and counterfactual alternatives

`IActed` and `Intervened` provide part of this mechanically, and the destination mentions
asking what would happen if it acted. The architecture does not explicitly require a causal
distinction or an answer about an alternative that did not occur. Prediction alone can learn
correlation while failing the stated counterfactual bet.

Suggested architecture entry:

> **And it distinguishes seeing from doing.** It can represent what followed an observation,
> what followed its intervention, and what it expects would have followed an action it did
> not take.

### It retrieves relevant knowledge under bounded resources

The route recognizes the need for a third store read by key, but retrieval is not an
architecture requirement. A model that contains the right fact and cannot select it does not
understand in an operationally useful sense. Loading every belief into every moment is not a
scalable substitute.

Suggested architecture entry:

> **And it brings the relevant part of what it knows to bear.** Understanding may grow
> without limit while each thought remains bounded, so relevance and retrieval are learned
> capabilities rather than a world placing the answer in the moment.

### It represents uncertainty and the limits of its knowledge

Belief malleability records how hard a belief is to shift, but that is not the same as
representing uncertainty, ambiguity, or missing knowledge. Abstention exists as a mechanism,
yet the architecture does not require calibrated knowing-when-it-does-not-know.

Suggested architecture entry:

> **And it can say what its evidence does not determine.** Competing explanations,
> insufficient evidence, and genuine unpredictability remain distinguishable, and confidence
> is calibrated by outcomes rather than installed as a threshold.

### Original thought includes goal-directed search, not only novel conclusions

The existing final entry requires conclusions that were never stated. That permits passive
deduction but does not require forming a question, selecting an experiment, or sequencing
actions toward a goal. The route contains pieces of curiosity and backward rule reading, but
the architecture does not make autonomous problem formulation part of success.

Suggested architecture entry:

> **And it forms and pursues questions of its own.** It identifies a gap in what it
> understands, chooses observations or actions expected to reduce it, and stops when the
> question is settled.

### It revises contradictions without erasing unrelated knowledge

Negation makes contradiction expressible and malleability makes evidence persistent, but the
architecture never explicitly requires coherent revision under a changing world. Continual
learning can otherwise mean accumulating mutually incompatible rules and letting a local vote
hide the conflict.

Suggested architecture entry:

> **And revision is local to what the evidence contradicts.** A changed belief can replace
> what no longer holds without erasing unrelated knowledge, while incompatible live beliefs
> remain visible until evidence resolves them.

These six entries are requirements rather than proposed mechanisms. Role binding, causal
intervention, retrieval, calibrated uncertainty, autonomous inquiry, and coherent revision
could each be implemented several ways and therefore belong in the architecture if they are
part of the intended destination.

## Implementation directions for the proposed requirements

These are candidate arms rather than architectural decisions. Each should earn its place on
an isolating world before reaching the spine.

### Make repair's statistical test valid under repeated observation

`Correcting.Gates` pays for the candidates and eligible parents examined within one round.
The same accumulating tables are still examined repeatedly over an indefinitely long stream.
An ordinary fixed-sample p-value is not valid under that optional stopping: given enough
looks, noise eventually crosses the bar even when every within-round correction is honest.

The preferred direction is an always-valid test per `(commitment, candidate code)`:

- A time-uniform confidence sequence for the difference between the candidate's hit and miss
  proportions.
- A mixture sequential probability ratio or e-value serving the same role.
- E-Bonferroni across the candidates initially; online FDR only if the simpler correction is
  measured as too conservative.
- The existing `Alpha` remains the standard of evidence rather than introducing another
  threshold.

A cheaper control is deterministic alpha spending over repeated examinations of one parent:

```text
first examination     alpha / 2
second examination    alpha / 4
third examination     alpha / 8
...
```

That control may be too conservative, but it is valid and gives the sequential mechanism
something honest to beat. The decisive noise-world reading is whether the rate of false
children tends toward zero as the run continues, while genuine rules remain discoverable.

### Add role binding in two stages

`Unifying.Any(modality, name)` already provides much of the matcher needed for variables.
Use that before replacing `Code` with a general graph or term representation.

The first stage is role-separated observation. One relation instance can enter a moment as:

```text
relation:follows
subject:<mary-id>
object:<john-id>
```

The subject and object modalities preserve direction. A variable scope can then express the
equivalent of:

```text
follows, Any(subject, X), Any(object, Y)
```

The isolating world should contain both `Mary follows John` and `John follows Mary`. A
bag-of-participants control must conflate them and a role-bound arm must not. Novel people in
the same roles test whether the relation transfers rather than memorises.

The second stage is variables in expectations. Variables currently affect whether a scope
fires, while `Commitment.Expects` remains one fixed `Code`. That cannot express a rule whose
consequence contains one of the bound participants. A narrow extension could be:

```text
Expectation := Constant(Code)
             | Bound(byte modality, int variable)
             | Applied(Code relation, Term[] arguments)
```

`Unifying.Fires` already returns a binding, which can ground the latter two shapes. Constants
should retain the existing hot path; grounding should be paid only by commitments that
contain variables. Tests should include novel fillers, subject/object reversal, two relations
sharing one participant, a repeated variable such as `same(X, X)`, and identical grounding
on one machine and a fleet.

### Build retrieval as a brain-side keyed store

Do not solve retrieval by having a world put the relevant fact back into every question
moment. Separate:

- General commitments: reusable predictive rules.
- Current state: facts believed to hold now.
- Keyed or episodic memory: retained facts that are not continuously active.

A minimal retained assertion needs a stable entity or relation key, its value, source stamp,
and evidence. An inverted `Code -> assertion IDs` index can supply candidates. A question
provides cues rather than answers, and the brain retrieves at most `k` assertions into a
bounded working moment.

Compare no retrieval, random `k`, overlap-based retrieval, and learned retrieval. Report
recall-at-k of the necessary assertion, answer accuracy conditional on successful retrieval,
overall answer accuracy, work, memory, and sensitivity to distractors. This separates a
failed selector from a failed reasoner.

### Keep estimated accuracy separate from uncertainty

A fresh commitment right once currently has an accuracy of one and can outrank a mature rule
that is right ninety-nine times in a hundred. Hit count should not become vote strength, but
the amount of evidence must affect epistemic uncertainty somewhere.

Keep two terms:

```text
estimate     local recency-weighted accuracy
uncertainty  an interval derived from effective evidence
```

A first decision rule can answer only when the winner's lower bound exceeds every rival's
upper bound, abstaining otherwise. This does not multiply confidence and accuracy into one
number. Deterministic, coin, switching, and hidden-context worlds should distinguish low
evidence, irreducible randomness, distribution shift, and a missing condition.

### Turn intervention codes into a causal control

An intervention code in a scope makes actions representable but does not establish causal
reasoning. Build a confounded isolating world such as:

```text
weather -> umbrella
weather -> wet ground
```

Seeing an umbrella predicts wet ground; forcing an umbrella does not make the ground wet.
Train on observations and interventions, then separately ask what follows seeing the
umbrella, forcing it, and not taking it while holding the prior state fixed.

The simplest correctness control enumerates the available actions, adds
`Intervened(action)` to a copy of the current moment, and asks what each hypothetical moment
predicts. It is expensive and therefore useful as a control. Optimisation can follow only
after the distinction is learned.

### Make autonomous inquiry reduce learnable uncertainty

`Drives.Learning` reads progress from actions the population already advocates, so it cannot
explore an entirely unknown action. Add an arm that considers every available action and
ranks it by expected reduction in uncertainty after observing its result.

This is preferable to raw surprise. A mastered action becomes uninteresting; an
irreducibly noisy action also becomes uninteresting once more samples cease reducing
uncertainty; a poorly understood but learnable action remains valuable until learned.

An isolating world should offer one informative action, several mastered actions, and one
coin action. The chooser should discover the informative one and eventually stop choosing
it. Language questions can use the same rule: ask the question whose possible answers most
reduce uncertainty among live competing beliefs.

### Make current-state revision keyed and causally ordered

Do not represent a changing state fact and a general rule in the same store. `Mary is in the
kitchen` is a current keyed value; `people often sleep in bedrooms` is a general commitment.

A minimal state entry is:

```text
key       location(Mary)
value     kitchen
stamp     source sequence
history   previous supported values
```

A later observation replaces the active value for that key while preserving its history.
Use source sequence rather than wall-clock time for causal order. Concurrent incomparable
updates can remain a visible multi-value conflict until further evidence resolves them.
General rules retain their evidence and recency estimates rather than being deleted because
one individual's current state changed.

The core test is that Mary can move without either forgetting that she was previously in the
kitchen or continuing to answer that she is currently there.

## Improvements to current implementation seams

### Make council operations round-scoped

Replace the stateful conceptual split:

```text
Vote AskAsync(moment)
Learnt TellAsync(outcome)
```

with a round-scoped exchange such as:

```text
RoundVote AskAsync(RoundId id, moment)
Learnt TellAsync(RoundVote round, outcome)
```

The returned value carries the exact moment and metadata being settled. This removes
`Fleet._moment` and `_fleeting`, prevents overlapping sources from crossing rounds, and
provides an identity for holder-side deduplication and retry safety.

### Add substrate-equivalence tests

For every brain dial, run identical stamped moments through `Alone` and through a one-holder
`Fleet`, then assert identical votes, learning counts, resident commitments, and tables.
Repeat with the reorderings, delays, duplicates, and failures the transport claims to
survive. This would catch the ignored `Deciding` dial directly.

### Validate and fingerprint the brain configuration

Validate `CommittingSettings` when constructed: `Recency` in `(0, 1]`, `Alpha` in `(0, 1)`,
positive floor, capacity and budget, and defined enum values. Derive a compatibility
fingerprint from the canonical settings, code-format version, front-end configuration and
build identity. Exchange it in the roster handshake and refuse a mismatch before a measured
round begins.

### Split `Population` along mechanisms without splitting transaction ownership

A possible decomposition is:

```text
PopulationIndex    matching and resident commitments
Voting             weighing and deciding
Genesis            initial proposals
Repairing          blame, gates and candidate selection
Abstraction        naming and rewriting
Maintenance        generalisation, subsumption and culling
PopulationMetrics  experimental counters and snapshots
```

One owner can still serialise mutation initially. The aim is to make mechanisms independently
readable and testable, not to introduce services or more locks.

### Detect commitment-identity collisions

A 64-bit identity is adequate for present experiments but treats a collision as though the
commitment were already held. At larger populations, move to 128 bits. In diagnostic builds,
retain the canonical scope and expectation behind each identity and fail if the same identity
arrives with different content. Include the identity-format version in the fleet fingerprint.

### Report software, architecture and scientific status separately

The structural guards are valuable, but a green guard set is not evidence for the research
hypothesis. Report three independent statuses:

- Software correctness and regressions.
- Architecture, reachability and reproducibility invariants.
- Scientific claims, controls, effect sizes and current uncertainty.

A scientific null result should not look like a software failure, and a structurally green
build should not look like support for AGI.

## Suggested implementation order

1. Fix `Alone`/`Fleet` parity, round identity, retry semantics and configuration fingerprints.
2. Replace repeatedly inspected fixed-sample significance tests with a sequentially valid
   repair gate.
3. Add role-separated relation observations, then variable-bearing expectations.
4. Add keyed current-state memory and bounded retrieval.
5. Add explicit uncertainty and contradiction handling.
6. Use interventions to establish causal distinctions.
7. Build autonomous inquiry on expected uncertainty reduction.
8. Increase language and visual complexity only after these internal capabilities pass.

This order tests whether the internal language can express understanding before asking a
difficult front end to manufacture it.

## Verification

Verification was repeated against clean HEAD `afd9fd12` after the in-progress correction
work was committed.

- `dotnet build OpenPlexus.slnx -c Release --nologo`: passed with zero warnings.
- The repository's 72 fast structural guards: all passed.
- A broad unsharded non-sweep run was stopped after more than ten minutes because the
  repository expects the expensive suite to run in CI shards. No failure was emitted before
  it was stopped; this is not represented as a full-suite pass.
- No source code was changed as part of the review.
