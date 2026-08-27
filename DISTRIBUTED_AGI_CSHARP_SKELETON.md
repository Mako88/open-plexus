# Greenfield C# skeleton for the distributed AGI research plan

Status: implementation handoff for critique. This document defines candidate seams and a first vertical
slice. It does not claim that the mechanisms will produce general intelligence, and it does not replace
the experimental order in `docs/plan.md`.

Companion: `DISTRIBUTED_AGI_RESEARCH_PLAN.md` states the architectural thesis, requirements, experimental
stages, and lessons from Persistence. This document translates that proposal into a new C# solution. The
existing Open Plexus implementation supplies findings, comparison arms, generated-world ideas, and
distributed failure lessons. The new implementation does not depend on its runtime types.

---

## 1. Implementation stance

Use conventional mechanisms:

- immutable value records for semantic artifacts and messages;
- explicit content identity rather than runtime object identity;
- unification over relational patterns;
- append-only episodes and derivations;
- keyed current state with explicit conflict handling;
- indexed semantic memory and bounded retrieval;
- actor-like holders with mailboxes and idempotent messages;
- mergeable evidence shards plus local time-sensitive estimates;
- beam search for initial planning;
- generated worlds and controlled ablations for every claimed capability.

Do not begin by creating a class named `Understanding`, `Intelligence`, or `Consciousness`. Those are
claims about the behavior of the integrated system. The code should contain small operations that can
fail independently.

Build the new system beside the existing one under `greenfield/`. Keep its solution, projects, tests,
configuration, and executable independent. This makes architectural comparisons honest: an experiment
can run the existing brain, the greenfield brain, or both without a compatibility layer quietly deciding
what either can represent.

Use a small number of assemblies with enforced dependency direction. Distribution is replaceable
infrastructure around a local semantic engine; generated worlds depend on public brain contracts; the
brain never depends on a world.

### Non-goals for the skeleton

- Raw image or audio perception
- Natural-language fluency
- Internet-scale deployment
- A hand-authored worldly ontology
- A production security system
- An optimal planner
- A learned retrieval policy
- A claim of consciousness or moral status

The first slice should prove that representation, learning, memory, intervention, inquiry, and
distribution can participate in one continuing loop.

---

## 2. Greenfield solution layout

```text
greenfield/
  Plexus.Greenfield.slnx
  Directory.Build.props

  src/Plexus.Core/
    Representation/
      SemanticId.cs
      RelationId.cs
      Term.cs
      GroundFact.cs
      FactPattern.cs
      BindingSet.cs
      Unifier.cs
      CanonicalEncoding.cs

    Knowledge/
      Commitment.cs
      ExpectationTemplate.cs
      Evidence.cs
      EvidencePolicy.cs
      Provenance.cs
      Derivation.cs

    Memory/
      MemoryContracts.cs

    Cognition/
      CognitionContracts.cs

    Agency/
      AgencyContracts.cs

  src/Plexus.Engine/
    Memory/
      CurrentState.cs
      EpisodeStore.cs
      SemanticStore.cs
      ProcedureStore.cs
      Retrieval.cs

    Cognition/
      WorkingCoalition.cs
      CognitiveCycle.cs
      Predictor.cs
      Settler.cs
      AttentionPolicy.cs

    Learning/
      CandidateGenerator.cs
      Repairer.cs
      Generalizer.cs
      Subsumption.cs

    Agency/
      Goal.cs
      Operator.cs
      Planner.cs
      InquiryPolicy.cs
      Decision.cs

  src/Plexus.Distributed/
    RoundId.cs
    MessageId.cs
    Envelope.cs
    CognitiveNode.cs
    IdempotencyLedger.cs
    ConfigurationFingerprint.cs
    InProcessNode.cs
    UnreliableTransport.cs

  src/Plexus.Worlds/
    RelationalInterventionWorld.cs
    WorldContracts.cs

  src/Plexus.Host/
    Program.cs

  tests/Plexus.Core.Tests/
    RepresentationTests.cs
    RoleBindingTests.cs

  tests/Plexus.Engine.Tests/
    MemorySeparationTests.cs
    RetrievalTests.cs
    InterventionTests.cs
    InquiryTests.cs

  tests/Plexus.Distributed.Tests/
    SubstrateEquivalenceTests.cs
    DistributedRoundTests.cs

  tests/Plexus.Acceptance.Tests/
    RelationalInterventionWorldTests.cs
```

Dependency direction:

```text
Plexus.Core <- Plexus.Engine
Plexus.Core <- Plexus.Worlds
Plexus.Core <- Plexus.Distributed
Plexus.Engine <- Plexus.Distributed
Plexus.Core + Engine + Distributed + Worlds <- Plexus.Host
Plexus.Core + Engine + Distributed + Worlds <- Plexus.Acceptance.Tests
```

`Plexus.Core` contains immutable semantic types and contracts with no database, transport, host, or
world dependency. `Plexus.Engine` contains the single-process reference brain. `Plexus.Distributed`
adapts that engine to holders and unreliable messages. `Plexus.Worlds` supplies instruments through
public observation/action contracts. `Plexus.Host` is composition only.

These names are a map, not a demand to create every file before the first test needs it. Create an empty
project only when the first type assigned to it exists.

---

## 3. Semantic identity and canonical form

Distributed holders must independently derive the same identity for the same semantic artifact.
Identity must not depend on process-randomized hashes, array object identity, insertion order, machine
endianness, or a parent repair path.

```csharp
namespace Plexus.Core.Representation;

public readonly record struct SemanticId(ulong High, ulong Low)
{
    public override string ToString() => $"{High:x16}{Low:x16}";
}

public readonly record struct RelationId(SemanticId Value);
public readonly record struct EntityId(SemanticId Value);
public readonly record struct CommitmentId(SemanticId Value);
public readonly record struct ProcedureId(SemanticId Value);
```

Use at least 128 bits for new semantic identifiers. The encoder owns ordering and normalization:

```csharp
public interface ICanonicalEncoding<in T>
{
    void Write(T value, IBufferWriter<byte> destination);
}

public interface IContentIdentity
{
    SemanticId Of<T>(T value, ICanonicalEncoding<T> encoding);
}
```

Canonical encodings must be versioned. Set-like inputs are sorted by their canonical byte encoding.
Sequence-like inputs retain order. Tests must calculate identities in fresh processes and across local
and fleet substrates.

Never rely on generated record equality for an array or `ImmutableArray<T>` when semantic equality is
required. Use an explicit structural comparer and derive identity from the canonical encoding.

Collision handling begins as a guard: retain canonical bytes beside a newly observed identity in debug
and test builds, and fail if one identity is presented with different bytes. A later network protocol can
carry a longer digest when peers disagree.

---

## 4. Relations, terms, and facts

The representation supplies variables and role positions without supplying concepts such as `person`,
`room`, or `owns`.

```csharp
namespace Plexus.Core.Representation;

public readonly record struct VariableId(int Value);

public abstract record Term
{
    private Term() { }

    public sealed record Constant(EntityId Entity) : Term;
    public sealed record Variable(VariableId VariableId) : Term;
}

public sealed record GroundFact
{
    public required RelationId Relation { get; init; }
    public required ImmutableArray<EntityId> Arguments { get; init; }
}

public sealed record FactPattern
{
    public required RelationId Relation { get; init; }
    public required ImmutableArray<Term> Arguments { get; init; }
}
```

Role identity is positional in the first implementation. `gives(giver, gift, recipient)` has three
distinguishable argument positions. If positional roles later prevent transfer across representations,
replace the argument array with explicit learned role identifiers. Do not install English role names in
the brain.

An entity is initially only an identity with groundings and relations:

```csharp
public readonly record struct GroundingId(SemanticId Value);

public sealed record ConceptRecord(
    EntityId Id,
    ImmutableHashSet<GroundingId> Groundings);
```

Its meaning is obtained by querying the facts and commitments in which it participates.

---

## 5. Bindings and unification

Matching must return the environment used to ground the expectation. A Boolean `Matches` result loses
the information required for role-correct transfer.

```csharp
namespace Plexus.Core.Representation;

public sealed class BindingSet
{
    private readonly Dictionary<VariableId, EntityId> values = [];

    public bool TryGet(VariableId variable, out EntityId entity) =>
        values.TryGetValue(variable, out entity);

    public bool TryBind(VariableId variable, EntityId entity)
    {
        if (values.TryGetValue(variable, out var existing))
        {
            return existing == entity;
        }

        values.Add(variable, entity);
        return true;
    }

    public BindingSet Copy()
    {
        var copy = new BindingSet();
        foreach (var pair in values)
        {
            copy.values.Add(pair.Key, pair.Value);
        }
        return copy;
    }
}

public interface IUnifier
{
    bool TryMatch(FactPattern pattern, GroundFact fact, BindingSet bindings);

    IEnumerable<BindingSet> MatchAll(
        IReadOnlyList<FactPattern> patterns,
        IReadOnlyCollection<GroundFact> facts);

    GroundFact Ground(FactPattern pattern, BindingSet bindings);
}
```

`MatchAll` starts as indexed backtracking:

1. Select the pattern with the fewest candidate facts.
2. Try each candidate against a copy of the current bindings.
3. Recurse into the remaining patterns.
4. Yield complete environments.

Index facts by relation and arity. Add argument-position indexes only when a measurement identifies the
need.

---

## 6. Time, observations, and interventions

Observation and intervention are different evidence sources even when their resulting facts are equal.

```csharp
namespace Plexus.Core.Knowledge;

public readonly record struct SourceId(SemanticId Value);
public readonly record struct ObservationId(SemanticId Value);
public readonly record struct SequenceNumber(ulong Value);

public enum AcquisitionKind
{
    Observed,
    Intervened,
    Told,
    Derived,
}

public sealed record Observation
{
    public required ObservationId Id { get; init; }
    public required SourceId Source { get; init; }
    public required SequenceNumber Sequence { get; init; }
    public required AcquisitionKind Acquisition { get; init; }
    public required ImmutableArray<GroundFact> Facts { get; init; }
    public ActionId? Intervention { get; init; }
}
```

Do not use wall-clock time as the only ordering mechanism. Each source supplies a monotone sequence. A
distributed envelope may add observed-at and received-at timestamps for diagnosis without pretending
they establish a global order.

```csharp
public enum TimeRelation
{
    SameMoment,
    NextMoment,
    Eventually,
}
```

This deliberately small temporal vocabulary is machinery, not a worldly ontology. Expand it only when a
world falsifies the available representation.

---

## 7. Commitments and expectations

```csharp
namespace Plexus.Core.Knowledge;

public sealed record Commitment
{
    public required CommitmentId Id { get; init; }
    public required ImmutableArray<FactPattern> Scope { get; init; }
    public required ExpectationTemplate Expectation { get; init; }
    public required TimeRelation Timing { get; init; }
    public required Provenance Provenance { get; init; }
}

public abstract record ExpectationTemplate
{
    private ExpectationTemplate() { }

    public sealed record Holds(FactPattern Pattern) : ExpectationTemplate;
    public sealed record DoesNotHold(FactPattern Pattern) : ExpectationTemplate;
}

public sealed record Prediction
{
    public required PredictionId Id { get; init; }
    public required CommitmentId Commitment { get; init; }
    public required BindingSet Bindings { get; init; }
    public required GroundFact ExpectedFact { get; init; }
    public required bool ExpectedToHold { get; init; }
    public required ObservationId BasedOn { get; init; }
}
```

`PredictionId` identifies one advocated expectation at one opportunity. It is distinct from the stable
commitment identity. This lets settlement be idempotent without making an observation part of the rule's
identity.

Begin with explicit negation only. Absence of a fact is not proof of its negation. A world must report a
closed observation domain before failure to observe can settle `DoesNotHold`.

---

## 8. Evidence, uncertainty, and settlement

Durable mergeable evidence and local regime-sensitive judgment are separate.

```csharp
namespace Plexus.Core.Knowledge;

public readonly record struct NodeId(SemanticId Value);

public readonly record struct EvidenceCounts(
    ulong Supports,
    ulong Contradictions,
    ulong Abstentions)
{
    public EvidenceCounts Merge(EvidenceCounts other) => new(
        Math.Max(Supports, other.Supports),
        Math.Max(Contradictions, other.Contradictions),
        Math.Max(Abstentions, other.Abstentions));
}

public sealed record EvidenceRecord
{
    public required CommitmentId Commitment { get; init; }
    public required ImmutableDictionary<NodeId, EvidenceCounts> Shards { get; init; }
}

public enum EvidenceVerdict
{
    Insufficient,
    Supported,
    Refuted,
    Indistinguishable,
}

public interface IEvidencePolicy
{
    EvidenceVerdict Evaluate(
        Commitment commitment,
        EvidenceRecord durableEvidence,
        LocalEstimate currentRegime);
}
```

The counters shown above are state-based G-Counter components: each node only increments its own shard,
and merge takes component-wise maxima. If deltas are sent instead, they require unique settlement ids and
an idempotency ledger.

`LocalEstimate` may use exponential decay or a bounded recent window, but it must name its observation
domain and must not merge as if it were timeless truth.

Candidate creation, repair, generalization, and pruning need a sequentially valid decision rule. Put
that behind `IEvidencePolicy`; do not scatter fixed p-value checks through learning classes.

```csharp
public interface ISettler
{
    Settlement Settle(Prediction prediction, Observation outcome);
}

public sealed record Settlement(
    PredictionId Prediction,
    CommitmentId Commitment,
    SettlementKind Kind,
    ObservationId Outcome);

public enum SettlementKind
{
    Support,
    Contradiction,
    Abstention,
}
```

Settlement is pure. Persisting it and incrementing evidence is a separate idempotent operation.

---

## 9. Provenance and derivation

Provenance is a graph of how an artifact came to exist, not a descriptive string.

```csharp
namespace Plexus.Core.Knowledge;

public readonly record struct ArtifactId(SemanticId Value);
public readonly record struct DerivationId(SemanticId Value);

public sealed record Provenance(
    ImmutableHashSet<SourceId> Sources,
    ImmutableHashSet<ArtifactId> Inputs,
    DerivationId? Derivation);

public sealed record Derivation(
    DerivationId Id,
    string Operation,
    ImmutableArray<ArtifactId> Inputs,
    ImmutableArray<ArtifactId> Outputs,
    ConfigurationFingerprint Configuration);
```

`Operation` is diagnostic metadata; correctness must not depend on parsing it. A later version can
replace it with a stable `OperationId`.

An imported claim keeps the original speaker or instrument as its source. The relayer, cache, and local
store appear in delivery and storage metadata, not as the author of the claim.

---

## 10. Four memory responsibilities

The stores may share a database initially, but their semantics remain distinct.

### Current state

```csharp
namespace Plexus.Core.Memory;

public readonly record struct StateKey(RelationId Relation, EntityId Subject);

public sealed record StateClaim(
    StateKey Key,
    GroundFact Fact,
    ObservationId Observation,
    SourceId Source,
    SequenceNumber Sequence,
    ClaimStatus Status);

public enum ClaimStatus
{
    Current,
    Superseded,
    Conflicting,
}

public interface ICurrentState
{
    IReadOnlyCollection<StateClaim> Read(StateKey key);
    IReadOnlyCollection<GroundFact> Snapshot();
    StateUpdateResult Apply(Observation observation);
}
```

`StateKey(Relation, Subject)` is sufficient for the first world. It is not universally correct. Some
relations are multi-valued, some are symmetric, and some state keys involve several participants. Make
key selection a learned or relation-specific policy later; do not hide that problem in overwrite order.

### Episodes

```csharp
public interface IEpisodeStore
{
    ValueTask AppendAsync(Observation observation, CancellationToken ct);

    IAsyncEnumerable<Observation> QueryAsync(
        EpisodeQuery query,
        CancellationToken ct);
}
```

Episodes are immutable. Corrections add linked observations; they do not alter the historical record.

### Semantic memory

```csharp
public interface ISemanticStore
{
    ValueTask AddAsync(Commitment commitment, CancellationToken ct);
    ValueTask<Commitment?> FindAsync(CommitmentId id, CancellationToken ct);

    IAsyncEnumerable<Commitment> MatchScopeAsync(
        IReadOnlyCollection<GroundFact> facts,
        CancellationToken ct);

    ValueTask ApplyAsync(Settlement settlement, CancellationToken ct);
}
```

Index commitments by relation, arity, and constants present in their scopes. Do not scan the entire
population in the target implementation.

### Procedures

```csharp
public sealed record Procedure(
    ProcedureId Id,
    ImmutableArray<FactPattern> Applicability,
    ImmutableArray<OperatorId> Steps,
    EvidenceRecord Evidence,
    Provenance Provenance);

public interface IProcedureStore
{
    IAsyncEnumerable<Procedure> FindApplicableAsync(
        WorkingCoalition coalition,
        RetrievalBudget budget,
        CancellationToken ct);
}
```

Do not implement procedure consolidation in the first slice. Preserve the seam and use single operators.

---

## 11. Bounded retrieval and working coalition

```csharp
namespace Plexus.Core.Memory;

public readonly record struct RetrievalBudget(
    int MaximumArtifacts,
    int MaximumExpansions);

public sealed record RetrievalQuery(
    ImmutableArray<GroundFact> CurrentFacts,
    ImmutableArray<Goal> Goals,
    ImmutableArray<RelationId> RequestedRelations);

public sealed record RetrievedArtifact(
    ArtifactId Artifact,
    double Score,
    RetrievalReason Reason);

public interface IRetriever
{
    ValueTask<IReadOnlyList<RetrievedArtifact>> RetrieveAsync(
        RetrievalQuery query,
        RetrievalBudget budget,
        CancellationToken ct);
}
```

The first retriever is deterministic structural retrieval:

1. Seed with entities and relations in the present state and active goals.
2. Fetch commitments indexed by those relations and constants.
3. Expand at most `MaximumExpansions` neighboring concepts.
4. Rank by structural overlap, evidence verdict, goal relevance, and estimated cost.
5. Return at most `MaximumArtifacts`.

```csharp
namespace Plexus.Core.Cognition;

public sealed record WorkingCoalition(
    Observation Trigger,
    ImmutableArray<GroundFact> CurrentFacts,
    ImmutableArray<Commitment> Commitments,
    ImmutableArray<Goal> Goals,
    ImmutableArray<Procedure> Procedures,
    ResourceBudget Budget);
```

The coalition is immutable and round-scoped. It is not stored as mutable fleet state.

Always retain retrieval controls:

- no retrieval;
- random artifacts under the same budget;
- structural retrieval;
- learned retrieval when it exists.

Measure retrieval recall and downstream decision quality separately.

---

## 12. Learning services

```csharp
namespace Plexus.Engine.Learning;

public interface ICandidateGenerator
{
    IEnumerable<Commitment> Generate(
        Observation before,
        Observation after);
}

public interface IRepairer
{
    IEnumerable<Commitment> ProposeRepairs(
        Commitment parent,
        IReadOnlyCollection<Observation> supporting,
        IReadOnlyCollection<Observation> contradicting);
}

public interface IGeneralizer
{
    IEnumerable<Commitment> ProposeGeneralizations(Commitment commitment);
}

public interface ISubsumption
{
    SubsumptionResult Compare(Commitment left, Commitment right);
}
```

The first candidate generator enumerates bounded patterns from adjacent observations. It may introduce
variables when two or more ground examples share relational structure with different entities.

Repair adds one discriminating fact pattern at a time. Its identity is derived from the resulting scope
and expectation, never from `parent.Id + condition`, so independent repair paths converge.

Generalization removes one scope pattern at a time and must clear the evidence policy. Subsumption keeps
the more general commitment when predictive evidence is indistinguishable and the general commitment
covers strictly more bindings.

Candidate-family accounting belongs around these services. A repair that pays only for its own optional
look while hundreds of other candidates are tried is not statistically valid.

---

## 13. Goals, actions, planning, and inquiry

```csharp
namespace Plexus.Core.Agency;

public readonly record struct GoalId(SemanticId Value);
public readonly record struct ActionId(SemanticId Value);
public readonly record struct OperatorId(SemanticId Value);

public sealed record Goal(
    GoalId Id,
    FactPattern Desired,
    double Priority,
    Provenance Provenance);

public sealed record Operator(
    OperatorId Id,
    FactPattern Action,
    ImmutableArray<FactPattern> Preconditions,
    ImmutableArray<ExpectationTemplate> Effects,
    CostEstimate Cost,
    EvidenceRecord Evidence);

public sealed record PlannedAction(
    ActionId Id,
    OperatorId Operator,
    BindingSet Bindings);
```

Goals, facts, and predictions are different types. Observing or predicting a desirable state must not
grant authority to act.

```csharp
public abstract record Decision
{
    private Decision() { }

    public sealed record Act(PlannedAction Action) : Decision;
    public sealed record Ask(Experiment Experiment) : Decision;
    public sealed record Answer(ImmutableArray<GroundFact> Facts) : Decision;
    public sealed record Abstain(string Reason) : Decision;
}

public interface IPlanner
{
    Decision Choose(
        WorkingCoalition coalition,
        IReadOnlyCollection<Operator> operators,
        PlanningBudget budget);
}
```

The first planner uses bounded beam search. A search state contains a set of possible world states, path
cost, predicted goal satisfaction, and uncertainty. It must preserve alternatives rather than selecting
the most likely successor at every step.

```csharp
public sealed record Hypothesis(
    CommitmentId Commitment,
    EvidenceVerdict Verdict);

public sealed record Experiment(
    PlannedAction Action,
    ImmutableArray<Hypothesis> Distinguishes,
    double ExpectedDecisionImprovement,
    double Cost);

public interface IInquiryPolicy
{
    Experiment? Choose(
        WorkingCoalition coalition,
        IReadOnlyCollection<Hypothesis> liveHypotheses,
        IReadOnlyCollection<Operator> availableInterventions);
}
```

The first inquiry policy can enumerate intervention outcomes and approximate:

```text
expected value = expected reduction in decision loss - intervention cost - delay cost
```

Compare it with random inquiry, surprise seeking, and never asking.

---

## 14. One cognitive cycle

The coordinator sequences pure or narrowly stateful services. It must not contain the learning policy.

```csharp
namespace Plexus.Engine.Cognition;

public interface ICognitiveCycle
{
    ValueTask<CycleResult> RunAsync(
        Observation observation,
        ImmutableArray<Goal> activeGoals,
        CancellationToken ct);
}

public sealed class CognitiveCycle : ICognitiveCycle
{
    private readonly ISettler settler;
    private readonly ICurrentState currentState;
    private readonly IEpisodeStore episodes;
    private readonly IRetriever retriever;
    private readonly IPredictor predictor;
    private readonly IPlanner planner;

    public async ValueTask<CycleResult> RunAsync(
        Observation observation,
        ImmutableArray<Goal> activeGoals,
        CancellationToken ct)
    {
        // 1. Settle previously issued predictions against this observation.
        // 2. Append the immutable observation to episodic memory.
        // 3. Apply its state claims with explicit ordering/conflict semantics.
        // 4. Retrieve a bounded set of relevant semantic artifacts.
        // 5. Assemble an immutable working coalition.
        // 6. Match commitments and issue grounded predictions.
        // 7. Generate candidate actions and informative interventions.
        // 8. Plan within the resource budget.
        // 9. Return a decision and the durable changes to publish.
        throw new NotImplementedException();
    }
}
```

The actual implementation should make the nine operations explicit methods or collaborators. The stub
shows ownership, not the desired final method length.

Do not make settlement failure prevent the observation from entering episodic memory. Use an atomic
unit of work for state that must agree, and an outbox for distributed publications.

---

## 15. Distribution contracts

Every interaction is round-scoped. An envelope distinguishes message identity from semantic payload
identity.

```csharp
namespace Plexus.Distributed;

public readonly record struct RoundId(Guid Value);
public readonly record struct MessageId(Guid Value);

public sealed record ConfigurationFingerprint(SemanticId Value);

public sealed record Envelope<TPayload>
{
    public required MessageId Message { get; init; }
    public required RoundId Round { get; init; }
    public required NodeId Sender { get; init; }
    public required ConfigurationFingerprint Configuration { get; init; }
    public required TPayload Payload { get; init; }
}

public sealed record MatchRequest(
    ImmutableArray<GroundFact> Facts,
    ImmutableArray<ArtifactId> Retrieved);

public sealed record NodeVote(
    ImmutableArray<Prediction> Predictions,
    ImmutableArray<ArtifactId> MissingDependencies,
    EvidenceVerdict Verdict);

public sealed record SettlementDelta(
    Settlement Settlement,
    EvidenceCounts NewLocalCounts);
```

`NewLocalCounts` is the sender's new monotone state, not an increment to apply blindly.

```csharp
public interface IIdempotencyLedger
{
    ValueTask<bool> TryBeginAsync(MessageId message, CancellationToken ct);
    ValueTask CompleteAsync(MessageId message, CancellationToken ct);
}

public interface ICognitiveNode
{
    NodeId Id { get; }

    ValueTask<NodeVote> MatchAsync(
        Envelope<MatchRequest> request,
        CancellationToken ct);

    ValueTask ApplyAsync(
        Envelope<SettlementDelta> delta,
        CancellationToken ct);
}
```

A concrete node may own a `Channel<INodeCommand>` mailbox. Public interfaces remain request-scoped so a
local in-process holder and a remote actor can implement the same contract.

### Round rules

- The request contains every transient input required to answer it.
- A node does not read fleet-wide mutable current-moment fields.
- A deadline closes decision collection, not evidence collection.
- Late votes may update future evidence but cannot mutate a returned decision.
- Duplicate messages do not duplicate settlement.
- Cancellation does not mark an uncompleted message as settled.
- Retrying after an ambiguous result returns the recorded outcome or completes the operation once.
- A configuration mismatch produces an explicit refusal.
- Concurrent rounds are expected and tested.

### Local/distributed equivalence

For one holder, every supported dial and seeded world must produce the same ordered semantic outputs
through:

```text
LocalEngine(holder)
DistributedEngine([holder])
```

Normalize away diagnostic timestamps and transport identifiers before comparison. Do not normalize away
semantic ordering, prediction identity, evidence, or abstention.

---

## 16. First generated vertical-slice world

Build one continuing relational intervention world. It should be small enough to enumerate completely
and rich enough to exercise the whole loop.

### World vocabulary

The front end may expose stable anonymous codes for:

- three persistent entities;
- two locations;
- one container relation with distinct container and contained roles;
- one controllable move action;
- one hidden regime variable;
- one consequence observable only after acting or asking.

The learner is not told human labels for any of them.

### Required sequence

1. The system observes different entities occupying locations.
2. One entity changes an irrelevant visible attribute while retaining identity.
3. The system sees the same relation with novel fillers and permuted roles.
4. A relevant fact leaves working capacity and must be retrieved by structural key.
5. Passive observations support two competing causal explanations.
6. One available intervention distinguishes them.
7. The system chooses that intervention because the answer changes a pending decision.
8. The resulting observation settles the causal commitment.
9. One holder disappears and rejoins while evidence continues to converge.

### Success claims

- Identity survives the irrelevant attribute change.
- A role-bound commitment transfers to novel fillers without reversing roles.
- The displaced fact is retrieved under a fixed budget.
- The agent abstains before the confounding evidence is resolved.
- It selects the discriminating intervention over a surprising irrelevant observation.
- It uses the intervention result to choose an action that reaches the goal.
- A local engine and a one-holder distributed engine are semantically equivalent.
- Duplicate, reordered, and delayed evidence messages do not change the final durable evidence.

Each claim needs an isolating control. One end-to-end score is insufficient.

---

## 17. Implementation increments

### Increment 0: freeze the contracts in tests

Add tests for canonical identity, structural equality, unification, idempotent settlement, concurrent
round isolation, and one-holder substrate equivalence. Stubs may throw outside the specific tested path.

Exit when the tests fail for the intended missing mechanisms and compile against the proposed public
surface.

### Increment 1: ground facts and role binding

Implement canonical encoding, fact indexes, `BindingSet`, and `Unifier`. Add worlds with novel fillers
and role permutations.

Exit when a rule transfers by binding and the conjunction-only control cannot solve the permutation.

### Increment 2: commitment templates and evidence

Issue grounded predictions, settle them once, and store mergeable evidence shards. Put the current
sequential repair bar behind `IEvidencePolicy` rather than replacing it during this increment.

Exit when repeated, reordered settlement converges and optional looks retain the measured false-discovery
bound.

### Increment 3: separate memory semantics

Add keyed current state, immutable episodes, and an in-memory `ISemanticStore`. Keep procedures empty.

Exit when a correction supersedes current state without rewriting history and unrelated state remains
unchanged.

### Increment 4: bounded structural retrieval

Move a required fact beyond the coalition budget. Implement relation/entity indexes and deterministic
top-k expansion.

Exit when structural retrieval beats no retrieval and random retrieval at equal cost on held-out world
seeds.

### Increment 5: observation versus intervention

Introduce action identifiers, intervention-marked observations, operators, and the smallest confounded
world.

Exit when an observational predictor remains uncertain and intervention evidence selects the correct
conditional effect.

### Increment 6: inquiry and shallow planning

Implement one-step value of information, then bounded beam search over learned operators.

Exit when the agent pays for information only when it changes a decision, rejects irrelevant surprise,
and completes a held-out multi-step instance.

### Increment 7: unreliable multi-holder execution

Put the vertical slice through actor mailboxes with delay, duplication, loss, cancellation, concurrent
rounds, and configuration mismatch.

Exit when semantic results match the deterministic reference within the stated availability contract.

Do not begin grounded language until these increments establish identity, binding, memory, causality,
and inquiry without language concealing their absence.

---

## 18. Test skeleton

Names below describe claims. Match repository naming conventions when implementing them.

```csharp
public sealed class RoleBindingTests
{
    [Fact]
    public void A_relation_transfers_to_novel_fillers_without_reversing_roles() { }

    [Fact]
    public void One_variable_keeps_one_value_across_every_pattern() { }
}

public sealed class MemorySeparationTests
{
    [Fact]
    public void A_newer_state_supersedes_the_current_value_without_rewriting_the_episode() { }

    [Fact]
    public void Independent_sources_may_leave_a_state_explicitly_conflicted() { }
}

public sealed class InterventionTests
{
    [Fact]
    public void Passive_correlation_does_not_settle_an_interventional_expectation() { }
}

public sealed class InquiryTests
{
    [Fact]
    public void The_chosen_question_distinguishes_hypotheses_that_change_the_decision() { }

    [Fact]
    public void Irrelevant_surprise_does_not_win_the_inquiry_budget() { }
}

public sealed class DistributedRoundTests
{
    [Fact]
    public async Task Concurrent_rounds_do_not_exchange_moments() { }

    [Fact]
    public async Task A_cancelled_request_can_be_retried_and_settled_once() { }

    [Fact]
    public async Task A_late_vote_cannot_change_a_returned_decision() { }
}

public sealed class SubstrateEquivalenceTests
{
    [Theory]
    [MemberData(nameof(AllDialsAndSeededWorlds))]
    public async Task One_holder_and_a_one_holder_fleet_are_semantically_identical(
        EngineSettings settings,
        WorldSeed seed) { }
}
```

Before accepting each test, disconnect or invert the mechanism and show that the test fails. A test that
remains green does not establish that the mechanism is exercised.

Property-based tests are particularly appropriate for:

- canonical identity under input permutation where order is semantically irrelevant;
- unification under variable renaming;
- evidence convergence under every delivery order and duplication pattern;
- state ordering under generated source sequences;
- planner invariance under irrelevant entity renaming.

---

## 19. Questions for Claude's critique

Claude should challenge the seams before implementing them.

1. Does positional role identity provide enough transfer for the first slice, or should learned role
   identifiers exist from the start?
2. Is `StateKey(Relation, Subject)` a useful controlled restriction, or will it distort the first world?
3. Should negative expectations be first-class now, given that closed-world observation semantics are
   not yet implemented?
4. Which useful behaviors from the existing commitment experiments must become black-box comparison
   arms, rather than code copied into the greenfield engine?
5. Are the proposed `Core`, `Engine`, `Distributed`, `Worlds`, and `Host` dependency boundaries minimal,
   or does one lack an independently testable reason to exist?
6. Is state-based per-node evidence affordable, or should settlement ids plus delta messages be used?
7. Which sequential evidence method best supports candidate creation, repair, generalization, and
   indefinite optional looks under one accounting model?
8. How should canonical encodings be versioned without giving semantically identical artifacts different
   permanent identities after an encoder upgrade?
9. What is the smallest vertical-slice world that separates binding, retrieval, causal intervention, and
   inquiry without one mechanism handing another the answer?
10. Which proposed interfaces create abstraction before evidence, and can be delayed?
11. Which invariants should become greenfield structural guards rather than ordinary behavioral tests?
12. Should the greenfield work remain a directory in this repository while the first slice is evaluated,
    or become a separate repository after its experimental contracts stabilize?

Claude should return a critique before a large implementation. The useful output is not agreement; it is
a smaller first change, the assumptions most likely to fail, and tests that would expose those failures.

---

## 20. Recommended first pull request

The first implementation should not add the complete directory tree. It should add only:

- `SemanticId` and canonical encoding for relational patterns;
- `Term`, `GroundFact`, `FactPattern`, and `BindingSet`;
- an indexed `Unifier`;
- one role-permutation generated world;
- role-binding and cross-process determinism tests;
- a minimal `Plexus.Host` command that runs the role-permutation world through the local engine and prints
  the learned commitment, grounded bindings, prediction, and settlement.

The pull request is successful if it establishes one new representational capability and supplies a
failing control. It should not claim that the broader architecture is implemented.

After that reading, either proceed to evidence settlement in the greenfield engine or delete the failed
representation and record why it failed. The existing Open Plexus brain remains available as a comparison
arm; neither implementation must conform to the other.
