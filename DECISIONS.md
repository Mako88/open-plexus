# Decisions — the option tree

**What this is.** Every architectural component, the options for it, and which
option each is. **One page, scannable, so a settled question stops being
re-opened.** That is the only job it has.

**Why it was rebuilt.** It was a 6,040-line append-only log — unreadable whole, so
read selectively, which on 2026-07-29 produced three wrong recommendations in a row
off claims later entries had already superseded. **A log records; it does not
prevent.** Full account in `CLAUDE.md` rule 14b.

**Mid-swap?** [HANDOFF.md](HANDOFF.md) carries what is OPEN — scratch, overwritten each
swap, and nothing here depends on it. This tree carries what was DECIDED.

**The old log is not deleted:**
[entries 83–171](docs/archive/decisions-log-083-171.md) and
[1–82](docs/archive/decisions-001-082.md). Every attempt below cites its entry number.
**The tree is authoritative; the log is the footnotes.**

---

## THE RULES FOR MAINTAINING THIS DOCUMENT

**The contract, not advice.** `tools/check_decisions.py` enforces what can be enforced.
**The governing constraint, John's:** *"as soon as it's big enough that you have to search
through it, you're gonna be missing things."* So this stays small enough to read whole, and
history lives in `docs/archive/notes/` and the archived log.

0. **ONE LINE PER ENTRY. ALL detail lives in the option record — John, 2026-07-31.**
   Marker, name, the claim in a clause, its citation, the revival condition if it is a ❌,
   and the link. Nothing else. **This replaced five shaving passes and three budget raises
   in a single day**, which is what the budget comment predicted: *"shaving a line at a
   time to stay under a budget is how a document gets worse without anyone deciding to
   make it worse."* The limit was never the problem — the tree was carrying detail that
   belonged elsewhere. **900 → 393 lines**, and the compression immediately exposed a
   duplicate option that two people had missed.
1. **A finding UPDATES AN OPTION; it never appends an entry.** About to add a numbered
   heading? Stop — this is not a log. `tests/test_goals_consistency.py` fails the build.
2. **Exactly one state marker per option: ✅ ❌ ⬜ 🔀.** Not two, not none.
3. **Every ✅ and ❌ cites a decision, sweep or note, or says it rests on NO MEASUREMENT.**
   A state with no measurement is UNTRIED, never "probably fine." A ❌ refused by opinion
   discards a good idea on an invalid measurement — the most expensive error available.
4. **The configuration a claim was measured in lives in the RECORD's CONFIG block**, not
   here. Refutations are conditional on a config and `74` cost a comparison set by
   forgetting that — the record is where it cannot be lost.
5. **Every ❌ states its REVIVAL CONDITION, and it stays HERE.** Refutations expire and one
   nobody can date is one nobody can retire — `107` and `111` both became right later.
   **It is the one detail that does not move to the record**, because a record holds events
   with no status (rule 12) and a revival condition is forward-looking. Checked before the
   compression: `continuous-vector-addressing.md` had no trace of its own, so moving it
   would have deleted it.
6. **Every component carries a `⇒` verdict line**, DECIDED or OPEN, with the answer if
   decided. That line is what makes this scannable, which is the job.
7. **Update the CENSUS when a state changes.** The checker fails on a mismatch.
8. **Refutations are exhaustive; confirmations are not.** Every ❌ belongs here. Something
   that worked and changed nothing else is cited where it supports a state.
9. **At the line budget, find the entry carrying detail and move it to its record.**
   Trimming prose is the wrong move and was made five times on 2026-07-30 before rule 0
   fixed the cause.
10. **Detail lives in an OPTION RECORD; this carries the claim and links it.** A
    measurement belongs to exactly one file. An option's history goes in
    `docs/options/<name>.md`: what was tried, **the state of the model when it was
    tried**, and what came back — and **no status**, which lives only here.
    `tools/check_options.py` enforces the split, and
    [docs/options/README.md](docs/options/README.md) is the format. Every entry carries a
    seven-key **CONFIG block** — `when source script task model knobs scale`, with
    `unrecorded` written rather than a line dropped — and
    `tools/check_provenance.py` requires every measurement in it to appear in a source
    that entry cites. That check found `0.9220` cited to a note that does not contain it
    ([note 105](docs/archive/notes/105-the-partitioning-accuracy-figure-has-no-source.md)).
11. **When first creating an option's record, SCAN THE ARCHIVE for it.** John's
    instruction, 2026-07-30. A record starting empty invites re-running work that
    `docs/archive/notes/`, `docs/archive/decisions-*.md` and the source already answer —
    which happened, when a partitioning result was nearly re-reported as new with
    `note 081` already holding it. **And a reference that does not resolve in the live
    tree is UNCHECKED, not absent:** the notes and the decision log are archived, not
    deleted, so look there before concluding a thing was never measured.
12. **A record holds EVENTS, so it cannot go stale.** *"On this date this configuration
    produced 0.9220"* stays true forever; *"this is what we use"* does not. That is the
    property that makes the split safe where the 6,040-line log was not, and why records
    have no "gaps" or "next steps" section. Absence means untried.

---

## How to read a row

    ✅ CHOSEN      decided, built, and this is what we use
    ❌ REFUTED     measured and it lost. The revival condition is stated
    ⬜ UNTRIED     no measurement. Not "probably fine"
    🔀 LIVE BOTH   two or more kept behind a switch and re-tested as the system
                   changes. A valid END state, not indecision
    ⏸ PAUSED      set down without being refuted. Distinct from ❌: no measurement
                   went against it, so it carries no revival condition — what it
                   needs is a reason to pick it back up

**CENSUS: 32 chosen, 30 refuted, 15 untried, 12 both, 1 paused.** Checked against the body,
because a summary that can drift is how its predecessor caught its own counts.

> **Coverage, stated exactly, because a tree that looks complete and is not is worse than
> the log.** Every ❌ from the log is here; re-proposing a refuted mechanism is the failure
> this prevents. **Confirmations are NOT exhaustive** — for the full chronology of what
> worked, the log is where it is, and every option names its entries.

---

## 1. Input → concepts

**⇒ DECIDED (163 §1): discrete ids, produced at the edge, outside the learning loop.**
Nothing non-text is built.

- ✅ **Discrete surface ids** — every input becomes a concept id, quantised at the edge. `163 §1`, *no measurement*. → [record](docs/options/discrete-surface-ids.md)
- ❌ **Address the store by continuous vector** — destroys exact addressing, the gate's structural zero, and the sketch. `052 §1`. **Revival:** a task where similar-must-share-an-address beats exact separation. → [record](docs/options/continuous-vector-addressing.md)
- ⬜ **Codebook: borrowed FEATURES, our own DETERMINISTIC id rule** — John's choice, 2026-07-30. Ceiling: a frozen feature space caps concepts at another model's distinctions. → [record](docs/options/learned-codebook.md)
- ⬜ **Per-node codebooks plus translation** — unsupervised translation, strictly harder than the goal. → [record](docs/options/per-node-codebooks.md)
- ✅ **`concepts.Merged`** — merge is a read-side gather over `aliases()`, union by minimum id; a late merge is a MISS, never a corruption. *no measurement*. → [record](docs/options/merged-concepts.md)

- ✅ **A concept has no global id — it is an equivalence class reached by walking** — John, 2026-07-31; dissolves the learned-identity vs deterministic-ownership conflict. *no measurement*. → [record](docs/options/identity-without-a-global-id.md)
- ✅ **Rounded timestamp as the cross-node co-occurrence key** — built; reproduces the single-process ceiling exactly, and WIDENING beats overlapping windows at a fifth of the messages. `g33-01`. **Still one process: the accumulator is not sharded.** → [record](docs/options/time-bucket-join.md)
- 🔀 **The co-occurrence statistic: raw count vs chance-corrected** — counting is beaten by anything merely COMMON, designed or Zipfian; correcting costs sample efficiency where neither applies. `g32-01`, `g32-02`. **Its read costs one peer message PER PARTNER**, not per `k`. → [record](docs/options/co-occurrence-statistic.md)
- ✅ **Shard the count table by `owner(surface)`** — `federated.py`: no node holds a row it does not own, the walk agrees with the single-table answer, and `ppmi` is REFUSED rather than approximated because no node knows the world's total. *no sweep — measured while building*. → [record](docs/options/co-occurrence-statistic.md)

**Open — codebook agreement across nodes.** SPLIT (one thing → two ids) only exists
distributed and is unsolved; `g27-01` showed divergence is silent one layer down and the
transport half is fixed. → [record](docs/options/per-node-codebooks.md)

## 2. Addressing — how a concept becomes a store address

**⇒ DECIDED: pair keys for relational work. 🔀 with single-token keys, still the default
and still correct for MQAR.**

- 🔀 **`PairKeys`** — hashed `(previous, token)`, so an entity's ROLES separate. `103`, `104`, `156`/`157`. → [record](docs/options/pair-keys.md)
- 🔀 **`TableKeys`** — one key per token; right where each entity appears once. `103`, `142`. → [record](docs/options/table-keys.md)
- ❌ **`ByConcept`** — destroys exceptions and answers confidently with the category default. `144`/`145`, `049`. **Revival:** a task with no within-category variation to lose. → [record](docs/options/by-concept.md)
- ❌ **Content-derived keys for ENTITIES** — nearby addresses is what §1 refuses, however the nearness arises. `042 §2`, `g10-09` RETRACTED. **Revival:** a task where similar-must-share-an-ADDRESS beats exact separation plus an index. → [record](docs/options/content-derived-entity-keys.md)
- ⬜ **Structured representations for RELATIONS** — the live requirement; GOALS §1 asks for it. `067`, and a local contrastive rule now clears the end-task bar across 18 graphs without needing an invariant. → [record](docs/options/structured-relations.md)
- ⬜ **A better index** — the set answer's quality is bounded by the grouping's. `note 056`, `note 057`, `note 058`. → [record](docs/options/a-better-index.md)

## 3. The store

### 3a. Structure

**⇒ DECIDED: one superposed `d × d` matrix. 🔀 with an exact cache and a settling read.**

- 🔀 **`SuperposedRead`** — summed outer products; beats a bounded cache 8× once bindings exceed slots. `119`, `109`. → [record](docs/options/superposed-read.md)
- 🔀 **`ExactCache`** and **`SettlingRead`** — kept per 14c; the cache is the first controlled corpus gain. `69`, `76`. → [record](docs/options/exact-cache-and-settling-read.md)
- ❌ **Anything recovering per-item information AFTER the sum** — `r = M @ key` is a sum; six mechanisms moved the level, none the slope. `69`. **Revival:** a read that is not a sum. → [record](docs/options/after-the-sum.md)

### 3b. Lifetime

**⇒ OPEN, and the question that has been asked wrong twice.**

- ✅ **Per-sequence, rebuilt every sequence** — the current default, and the guard that makes any cross-sequence claim falsifiable. `62`. → [record](docs/options/per-sequence-store.md)
- ✅ **Use-based eviction** — discard the longest-unused; a persistently-queried fact survives. `note 083`. **A prior gate nobody recorded: consolidation promotes only what was ALREADY predicted correctly**, so the durable store holds what it already gets right. → [record](docs/options/use-based-eviction.md)
- ✅ **The two-timescale loop RUNS and cannot adjudicate** — repair relocates the damage. `note 092`, predicted by `note 068`. → [record](docs/options/two-timescale-loop.md)
- ⬜ **An EXTERNAL persistent store** — eviction becomes archival; a PREFETCH source, never in the traversal loop. John, 2026-07-30. → [record](docs/options/external-persistent-store.md)
- 🔀 **`persistent_lasting`** — a real gain, switched off because turning it on invalidates the text comparison set. `133`, `note 082`. → [record](docs/options/persistent-lasting.md)
- ⬜ **`carry_store`** — no task here can pay for it; unfalsified on the goal, not refuted. `170`, `62`. → [record](docs/options/carry-store.md)

### 3c. Capacity, and the wall that is not one

**⇒ SETTLED and repeatedly re-opened. Read this before proposing anything about
saturation.**

- ✅ **The 16k-character wall is a property of the OBJECTIVE** — a character bigram table is intrinsically low-rank, effective rank ~3 at every width. `115`. → [record](docs/options/saturation-closed.md)
- ❌ **"The wall is a capacity limit"** — `133`'s relabel of its own null. **Revival:** a direct probe showing store or readout below task demand. → [record](docs/options/wall-as-capacity-limit.md)
- ❌ **"Concept partitioning is where the capacity comes from"** — pooled capacity is identical between arrangements; LONE-NODE capacity is the claim that survives. `134`. **Revival:** a measurement where pooled capacity differs. → [record](docs/options/partitioning-as-capacity-source.md)

## 4. Selection & membership — the gate

**⇒ DECIDED: `inherit`. The project's cleanest mechanism and nothing in it is fitted.**
The limit: the sketch knows **emptiness, not relevance**, so it cannot bound an
enumeration over addresses that are all occupied.

- ✅ **`inherit` / occupancy sketch** — answer from your own address if anything was written there, else from neighbours; an unwritten address reads exactly 0.0, so the bar is structural. `148`, `149`, `153`. → [record](docs/options/inherit-gate.md)
- ❌ **Select by norm** — magnitude says nothing about whether it is the right address. `147`. **Revival:** none foreseen; it is rule 7's shape. → [record](docs/options/select-by-norm.md)
- ❌ **Select by decode margin** — confidence in *an* answer is not evidence about *which* retrieval produced it. `147`. **Revival:** the same quantity DOES work on a different question — `129`/`130` gate the search on it. → [record](docs/options/select-by-decode-margin.md)
- ❌ **Sum the two retrievals** — averaging, so it cannot choose. `146`. **Revival MET at `167`:** nothing has to be selected when the answer is a set. → [record](docs/options/sum-the-retrievals.md)

## 5. Composition — reaching what was never stated

**⇒ LARGELY SOLVED (`note 090`/`091`): a chain is walked by `search.beam` and named by a
fold over pairwise rules, with missing rules supplied by GENERATION DELTA.** The open
question is whether an arbitrary domain has an invariant of that kind.

- ✅ **`hop_relation`** — bind a relation token into the hop's key so a hop follows a NAMED edge. `158`, `162`. → [record](docs/options/hop-relation.md)
- ✅ **`hop_relations`** — one relation PER HOP, so a walk follows LINK-then-FACT. `164`. An instrument, not the answer: a schedule the task does not supply is a fitted constant. → [record](docs/options/hop-relations.md)
- ⬜ **Try-all-and-gate** — its viability is a property of RELATION DENSITY, and the dense case is refuted. `108`, `163 §2`. → [record](docs/options/try-all-and-gate.md)
- ⬜ **Learned relation chooser** — strictly harder than two hand-made rules that were already refuted. `147`. → [record](docs/options/learned-relation-chooser.md)
- ✅ **`search.beam` — branch at EVERY step, pruned** — `run()` calls it, `search_beam_width=4`. **A MECHANISM, not a margin** (`note 103`); `note 065`'s +0.2190 does NOT reproduce, the gain is +0.107. → [record](docs/options/beam-search.md)
- ✅ **A hop REPLACES a retrieval, it does not combine with it** — `101`, `102`, `103`. → [record](docs/options/hop-replaces-retrieval.md)
- ❌ **Another mechanism stacked on noisy retrieval** — four tried, all against the same ceiling; the fix is per-step fidelity. `102`, `105`, `107`, `111`. **Two conditions EXPIRED at `121`/`122`.** → [record](docs/options/stacked-on-noisy-retrieval.md)
- 🔀 **`hop_accumulate`: `concat` vs `bind`** — concat wins only because 16 rules in a 128-wide space are linearly separable whatever the labels do; `note 063`/`066` make the fold what generalises. → [record](docs/options/hop-accumulate.md)
- ✅ **GENERATION DELTA, learned from cycles** — 20/20 deltas recovered exactly; end task 0.5201 → 0.9668, and a deliberately WRONG delta scores below random. `note 090`/`091`. **SCOPED by `note 104`**, narrowed by `g23-03`: `dim` is a property of the EXTRACT, not the domain. → [record](docs/options/generation-delta.md)
- ❌ **Naming the missing rule by a learned readout over COUNTED vectors** — below random filling on the end task. `note 088`. **Revival MET at `g23-01`:** a LEARNED representation names it at 0.7821 against random's 0.6642. → [record](docs/options/naming-the-missing-rule.md)
- ⬜ **`index_at_hops` with the position-level index** — blocked on an instrument, not a mechanism: no task has both an address-never-written and a composition. `154`, `note 050`. → [record](docs/options/index-at-hops.md)

## 6. The answer — what a response IS

**⇒ OPEN, and this is the live question.** The enumeration bound is **either supplied, or
it needs a near-oracle grouping** — neither is answering from awareness, which is why this
is PARTIAL and not PASSING.

- ✅ **Set of tokens, scored by `exact` and F1** — the convention, built BEFORE anything produced a set; recall alone is never reported. `165`. → [record](docs/options/set-of-tokens.md)
- ✅ **Emit by gated collection over index-proposed neighbours** — decision 146's refuted mechanism unchanged; the refutation was about the question. `167`. → [record](docs/options/gated-collection.md)
- 🔀 **Bound the enumeration by the biggest similarity gap** — matches the best fixed `branches` without being told the size, but real co-occurrence has NO cliff. `note 058`. **`g33-02`: a per-surface bound is now REQUIRED, not optional — one global `k` cannot express a hub and its spokes.** → [record](docs/options/biggest-similarity-gap.md)
- 🔀 **Fixed `branches`** — a measured CROSSOVER, not a loser: which is right is a property of the grouping's quality. `167`, `note 056`. → [record](docs/options/fixed-branches.md)
- ⬜ **Autoregressive output** — not ruled out by GOALS §2; what argues against it is termination. → [record](docs/options/autoregressive-output.md)
- ❌ **Structured slots** — a fixed frame is a traversal with a fitted schedule. *no measurement*, a scope ruling. **Revival:** a domain where the frame is genuinely supplied by the task. → [record](docs/options/structured-slots.md)
- ⬜ **Declining to answer** — **`g26-01`: the gate CAN decline, exactly** — 1.0000 correct, 0.000 false, on a known entity with an unwritten relation. Still ⬜: nothing in `run()` consults occupancy, and a written-but-wrong answer stays invisible. → [record](docs/options/declining-to-answer.md)

### 6b. Knowing when to stop hopping

**⇒ DECIDED: a learned halting gate. It works and it is not confidence.**

- ✅ **`halt_gate`, learned** — what separates is the CONTENT, not confidence; generalises zero-shot to an untrained depth at 0.992. `086`, `087`, `089`, `092`. → [record](docs/options/halt-gate.md)
- ❌ **Transferring the gate to new terminator tokens** — impossible by construction under a frozen random `Wv`. `089`. **Revival:** a `Wv` in which terminators share structure. → [record](docs/options/gate-transfer.md)
- ❌ **A token-agnostic terminal signal** — there is none, and `value_lr` does not build a terminator class. `093`, `094`. **Revival:** a representation where a terminator class exists to be found. → [record](docs/options/token-agnostic-terminal.md)
- ❌ **Occupancy as a free halting signal** — a traversal writes every address before querying it. `153`. **Revival:** a task where a walk can run off the end of what was written. → [record](docs/options/occupancy-as-halting.md)

## 7. Output → surface

**⇒ OPEN and off the critical path. Blast radius near zero.**

- ✅ **Template realiser** — deterministic, dependency-free, structurally incapable of adding a fact; an empty set DECLINES. *no measurement* — a floor. → [record](docs/options/template-realiser.md)
- ✅ **Retrieval realiser (`render.speak`)** — the words come from the CONCEPT MAP, not the caller. *no measurement*. → [record](docs/options/retrieval-realiser.md)
- ⬜ **Small learned renderer** — with a FAITHFULNESS test rather than an accuracy one. → [record](docs/options/learned-renderer.md)
- ❌ **Off-the-shelf LLM as renderer** — a fluent renderer produces the right sentence from a wrong walk, so the number measures its world knowledge. *no measurement* — rule 2. **Revival:** a faithfulness test showing it cannot add or drop a fact. → [record](docs/options/llm-renderer.md)
- ✅ **No renderer, for programmatic use** — typed bindings beat a sentence for an API. *no measurement*, a scope position. → [record](docs/options/no-renderer.md)

## 8. What learns

**⇒ OPEN. The narrowest description of the whole architectural problem** — `Wk` and `Wv`
are frozen random and the store is rebuilt per sequence, so everything durable is one
linear map.

- ✅ **`Wo` only, delta rule at scored positions** — the exact gradient for one linear readout; the rule is not the limitation, the absence of anything to write to is. `042 §4`. → [record](docs/options/wo-only-delta-rule.md)
- ❌ **`value_lr` / `value_centre` to unfreeze the values** — they move a long way and the plateau does not budge. `114`, `94`, `69`. **Revival:** a task where the value space itself is the bottleneck — and every measurement in that record is on TEXT or terminators, never on adversarial composition. → [record](docs/options/value-lr.md)
- 🔀 **`hidden` readout** — the largest single factor on text, and the answer above the width ~100 crossover. `70`, `71`, `110`. → [record](docs/options/hidden-readout.md)
- ⬜ **Self-modifying structure** — nothing to modify: the store is `d × d`, fixed at construction. `042`. → [record](docs/options/self-modifying-structure.md)

## 9. Distribution

**⇒ THE DRIVER IS GONE FROM THE READ PATH (`note 093`/`094`), and the walk costs 161 ms a
round measured (`g24-01`).** `d_max` is a CHURN TIMEOUT, not a latency budget; John accepted
the measured latency 2026-07-30. Dimension splitting is still the default.

- ✅ **Partition by dimension** — node count ≈ width ÷ 16, not anything softer. `g4-01`. → [record](docs/options/partition-by-dimension.md)
- 🔀 **Partition by concept** — MANDATORY for C4's growing capacity, not merely better; accuracy improves too. `note 081`, `note 105`. Off by default, blocked by six combination refusals. → [record](docs/options/concept-partitioning.md)
- ✅ **`openplexus/peer.py` — point-to-point reads, no driver** — 2 messages per read against 2N; a departure costs a round trip, not the answer. `notes 093`–`099`. **`g27-01`: a diverged peer was SILENT until the fingerprint learned to cover the value table.** → [record](docs/options/peer-transport.md)
- ❌ **The global dimension-summing readout** — the globally synchronised step C1 forbids, surfaced in a footnote to `note 009` §4 after four gates had passed on top of it. **Revival:** none while C1 stands. → [record](docs/options/global-summing-readout.md)
- ✅ **Transport: vote-based, with suspicion and a deadline** — `d_max` ~640 ms is 3× a measured p99, a floor not a constant. `128`, `126`/`127`, `169`. → [record](docs/options/transport-vote-deadline.md)
- ⬜ **Untrusted nodes** — no threat model. **And the merge gate is a SYBIL TARGET**: mutuality is what works and confidence makes it worse, so the obvious hardening is the harmful one. Forks on the endgame. → [record](docs/options/untrusted-nodes.md)
- ⬜ **Slice negotiation** — static by John's explicit choice; nothing needs a coordination protocol yet. → [record](docs/options/slice-negotiation.md)

## 10. The objective and the instruments

**⇒ DECIDED: relational, not next-token. The instruments are all self-designed and that
is the standing weakness** — though `CLUTRR`, `OpenEA` and now `FB15k-237` are external.

- ✅ **Relational objective** — the objective was the ceiling, not the memory: on next-token the only relation the store can express is an n-gram. `047`, `142`, `136`/`139`. → [record](docs/options/relational-objective.md)
- ❌ **Bits per token as evidence about the store** — n-gram bounded, so it cannot show what the store adds. `142`, `047`. **Revival:** an objective over text that is not next-token. → [record](docs/options/bits-per-token.md)
- ❌ **Training on every position** — costs composition 1.000 → 0.40 because the halting GATE is conflicted. `095`–`098`. **Revival MET at `098`** with `gate_objective` set. → [record](docs/options/training-every-position.md)
- ❌ **Perpetual learning as a repair for churn** — churn costs capacity, not knowledge; at 10.6× capacity recall is 0.07 and symmetric, so replay cannot fix it. `091`, `note 081`/`082`. **Revival:** none as a churn repair. → [record](docs/options/perpetual-learning-for-churn.md)
- ❌ **Concept addressing as a fix for text prediction** — a grouping built from SHUFFLED text does as well; the address count did the work. `141`. **Revival:** a text objective that is not next-token. → [record](docs/options/concept-addressing-for-text.md)
- ✅ **`families.py`** — the only instrument where things RESEMBLE each other, so the only one where a concept can mean something. `143`, `166`. → [record](docs/options/families-instrument.md)
- ✅ **`closure.py`** — unmarked stream, so the stated/entailed split IS the recall/reasoning split. **`g14-01`, 8 seeds: G0 passes by the gate as written, but P2 is REFUTED** — the margin over the real floor is +0.092, not the predicted 0.15, and `frozen` scores exactly nothing. → [record](docs/options/closure-instrument.md)
- ❌ **The delta rule as a composer on `closure`** — 0.108 against a 0.190 majority floor, on every one of eight seeds: below the base rate is active misprediction. `g14-01`. **Revival:** a learning rule reaching anything durable beyond the readout. → [record](docs/options/closure-instrument.md)
- 🔀 **`kinship.py`**, **`mqar.py`**, **`chains.py`** — the mechanism testbed, the store's control, and a solved out-degree-1 control. `142`. → [record](docs/options/kinship-mqar-chains.md)
- ❌ **A composition sweep on chains as evidence about composition** — out-degree 1 by construction, so nothing chooses. `108`, `note 103`. **Revival:** none for chains. → [record](docs/options/composition-sweep-on-chains.md)
- ⏸ **`corpus.py`** — PAUSED, not condemned. Closed by `115`/`118`, reopened by `g17-01`. → [record](docs/options/corpus-instrument.md)
- ❌ **`reward_recall.py`** — retired; its requirements list turns out to describe bsuite's Memory Length test, and the list was a search query nobody used as one. `126`. **Revival:** the literature's version. → [record](docs/options/reward-recall.md)
- ✅ **CLUTRR-symbolic** — the first external instrument; graph layer, never the prose. Report per hop bucket and split on ENTITY REPETITION. `note 059`, `note 060`. → [record](docs/options/clutrr-symbolic.md)
- ✅ **OpenEA `EN_DE_15K_V2`** — the acquisition instrument. Zero supervision reaches 583× chance; **mutuality is the merge gate and a confidence gate makes it WORSE**. `note 077`/`078`. → [record](docs/options/openea.md)
- ❌ **`4.540` bits/char, "unigram BEATEN"** — the headline for weeks, and not a measurement of this model; an inherited figure with no provenance outranks every measurement downstream of it. `117`, `118`. **Revival:** none. → [record](docs/options/the-4540-headline.md)
- ❌ **Scoring without a temperature** — a calibration artefact that looks exactly like a null result. `117`. **Revival:** none, it is a defect. → [record](docs/options/scoring-without-temperature.md)
- ❌ **`9.323` as the word-level unigram** — a wrong baseline moves every arm together. `135`. **Revival:** none; baselines come from the dependency-free ruler. → [record](docs/options/word-level-unigram.md)
- ❌ **Everything measured by the g18 harness before the fix** — it trained on the wrong target and survived four sweeps because every arm was wrong identically. **Internal consistency is not evidence.** `138`. **Revival:** none for those numbers. → [record](docs/options/g18-harness.md)
- ✅ **g17-01's premise survives its own correction** — the pivot was not an artefact. `140`. → [record](docs/options/g17-01-premise.md)
- ❌ **Note 050's linked-families task as first designed** — refuted by its own rail on the first run. `155`. **Revival MET at `164`:** the blocker was a hop that could not carry its own relation. → [record](docs/options/linked-families-task.md)

## 11. Verification apparatus

**⇒ DECIDED and deliberately permanent. Moved out of the tree** — every item documents
itself in its own docstring and CLAUDE.md rules 6, 10, 11 and 14 carry the policy, so it
was spending lines in a document whose criterion is being readable in one pass. Nothing in
it has ever been re-litigated, which is the only thing the tree prevents.

- ✅ **The apparatus** — mutation harness (sharded 6 ways in CI; `--verify` is the authority
  on the count), a dependency-free ruler in `tasks/`/`baselines.py`/`answers.py`
  (`note 007`), the rails (`check_workflows`, `check_rails`, `check_duplication`,
  `check_decisions`, `check_options`, `check_provenance`, `check_explainers`), and a
  sensitivity check on any timing assertion — `169`: three attempts at one assertion and **the first two both passed
  when written.**
  → record: [verification-apparatus.md](docs/options/verification-apparatus.md) ·
  full account: [archived](docs/archive/verification-apparatus-2026-07-30.md)

---

## Standing agreements

- **Blanket permission for architectural decisions.** The pending-decisions list is
  a REPORT, not a gate. Decide, proceed, record which calls were made alone.
- **List pending decisions at the end of every response.** John reads from a phone.
- **PREFER THE OPTION THAT SETTLES THE QUESTION — John, 2026-07-30.** Where one option
  gives a decisive signal and another leaves the question open, take the decisive one
  **even when it is harder, slower and more likely to fail.** The only exception is an
  absolutely massive lift, and that bar is high. In his words: *"we've done a lot of
  close enough or nearly there or almost there things, and at the end of the day we
  still have these nine things we have to prove out."*
  - **The ordering rule's twin**: that one picks the question most likely to disprove the
    project, this picks the FORM of it that could. **It applies to instrument design too** —
    a test whose band cannot separate its arms is a "nearly there" wearing an experiment's
    clothes.
- **EVERY OPTION OFFERED TO JOHN CARRIES THREE THINGS — his instruction, 2026-07-30.**
  He switches between this and other work, so assume no context: **what it IS in plain
  terms** and where it sits, **pros and cons**, and **a recommendation** with what happens
  if he does not reply. **AND INCLUDE UNTRIED NOVEL OPTIONS** — *"always feel free to throw
  in untried novel solutions that could work."* A menu of only the already-attempted cannot
  escape a dead end.
- **NEVER OFFER AN OPTION THAT FAILS THE GOALS — John, 2026-07-30.** Something known not
  to scale or not to survive where the project is going is not a choice, however well it
  works today. Record it as a ❌ with the reason; keep it out of any menu. **An invalid
  option costs him the time to evaluate it and risks him picking it.**
- **CHECKPOINT ONTO A BRANCH SO A CI RUN CAN FINISH — John, 2026-07-30, and it is a
  recurring failure rather than a one-off.** `checks.yml` uses
  `concurrency: checks-${{ github.ref }}`, which is **per ref**, so a push to a branch
  cannot cancel master's run and vice versa. The mechanism was already there; the fault
  was pushing the same ref repeatedly.
  - **At a checkpoint, push a `checkpoint/<date>-<n>` branch and leave it alone.** That
    run completes uninterrupted while work continues on master.
  - **Measured:** eight consecutive `checks` runs cancelled on 2026-07-30, so the six
    mutation shards — CI-only — did not execute once across a session that changed four
    modules. The older "batch when a sweep is in flight" rule did not fire because no sweep
    was in flight: **`checks` is starved by ordinary commits.**
- **LATENCY: `d_max` IS A CHURN TIMEOUT, NOT A LATENCY BUDGET — my call, 2026-07-30,
  recorded because it corrects how I had been reporting.** Decision 128 derived 640 ms as
  3× a measured p99, following SWIM's rule for declaring a node dead rather than slow.
  **It was never derived from a user requirement**, and `g24-01` reported "OVER" against it
  as though exceeding it were failure. It is not: it is the point at which a peer is
  treated as gone.
  - **The two requirements are separate.** A walk must survive a machine leaving
    mid-walk — that is what a timeout is for. And an answer must arrive within whatever
    the use case tolerates, **which has never been stated**.
  - John's framing: the endgame is not chat-shaped, so interactive latency may not be the
    requirement at all. At 161 ms a round, depth 10 is ~3.2 s.
  - **So `d_max` stays as the churn timeout and stops being quoted as a deadline on
    answers.** **JOHN'S RULING, 2026-07-30: the measured latency is ACCEPTED.** In his
    words — *"inherently, it's the internet, it's gonna be relatively slow ... that's a
    thing we eat for right now."* At 161 ms a round and 2 rounds a hop, the project's own
    task depths cost: **depth 2 ≈ 0.6 s, depth 3 ≈ 1.0 s, depth 5 ≈ 1.6 s, depth 10 ≈
    3.2 s.** CLUTRR tests 2–10 hops and `run()` works at 2, so the common case is about a
    second. **ACCEPTED, NOT CLOSED — John, 2026-07-30:** *"accepted as far as does the
    project meet its goals, but still something I want to tweak in the future... it drops
    way down the list because it works."* So performance work stays on the list at low
    priority rather than being struck off; the migrating walk is the known ~2×.
- **STANDING PERMISSION TO FIND AND FETCH DATASETS — John, 2026-07-30, widened same day.**
  *"If a new dataset would help prove or disprove something you need proven, always feel
  free to go look for it, and if you find one, download it and use it without requesting
  specific permission."* **So SEARCHING for one is part of it, not just fetching a named
  one.** Scope: evaluation data from its canonical public distribution — not code, models
  or weights. Fetchers pin URL, size and sha256; `data/*/` is gitignored so CI needs a
  fetch step. Name the dataset and why in the commit. Measured cost of the old
  per-dataset rule: `g23-03` ran on already-approved graphs rather than the benchmark
  that answered more.
- **Explain plainly, keep the numbers, do not hide bad news.**
- **Goal ordering:** AGI is primary; being an LLM replacement on consumer machines
  is secondary and must not compete with it.
- **Biology gives policies, not representations**; take mechanisms from computer science.
- **Scheduled wake-ups DO NOT FIRE.** A persistent `Monitor` emitting a heartbeat
  is what works.
- **RUN UNATTENDED, AND KEEP RUNNING — John, 2026-07-30.** Three parts, and they are one
  instruction: *"keep a 5 minute monitor wakeup going as long as there is still a clear next
  step forward toward the goal, always focus on blocking/harder problems before simpler
  stuff, and make any decision necessary to move forward if I'm not around."*
  - **The heartbeat is 5 minutes and stops only when no clear next step exists** — not at
    the end of a task and not at a convenient pause.
  - **MOST LIKELY TO DISPROVE THE PROJECT, FIRST — John's restatement, 2026-07-30.** This
    previously read *"blocking and harder first"*, and hard was a proxy for what he
    actually meant. **The ordering quantity is how likely a question is to show the whole
    thing cannot work**, not how difficult it is and not what it unblocks. In his words:
    *"until we've validated the core principles and core ideas of it, that's my top
    priority... knock out as efficiently as we can the things we need to get to a point
    where we'll know for sure one way or the other."*
    - **Tuning is explicitly DEFERRED until the core is proven.** Making a working
      mechanism better is not a validation step, however much headroom it has.
    - **Order by kill probability, then by dependency, then by cost.** A cheap question
      whose answer invalidates an expensive one comes first — the expensive work is
      wasted if the cheap answer is no.
    - It remains the counter to the gradient CLAUDE.md rule 17 names: every audit yields a
      satisfying provable result and every new mechanism most likely yields a null, so the
      easy work is always the work that feels productive. **Never order by what is ready.**
  - **Decide, do not wait** — any call needed to keep moving, short of countermanding an
    agreed goal or constraint. Record which were made alone.
- **Input and output is John's call** — his framing is that if the AGI goal wins,
  inputs should look like a body: a loop with consequences, not a passive feed.
- **The endgame is undecided** — commercial, open source, or both — and John holds that an
  AGI used the way current chat agents are used would be immoral. Do not assume an answer.
- **LEGIBILITY MAY BE SPENT TO REACH THE GOAL — decided by John, 2026-07-30.** This
  architecture has a property LLMs lack: the route IS the reason, since `beam` returns
  the walk it actually took, so the explanation and the computation are one object
  rather than a post-hoc story. A learned similarity geometry is *less* legible than a
  rule table, so `note 070`'s direction spends some of it. **John's call, in his
  words:** it is necessary for the goals, he cannot inspect anyone else's brain either,
  and *"if we don't meet the goals, then it doesn't matter anyway."* Recorded as a
  decision rather than allowed to happen by default — and the property is worth
  protecting where it costs nothing, which is most places.
- **DECISIONS.md must stay readable in ONE pass — John's primary criterion for it.**
  *"As soon as it's big enough that you have to search through it, you're gonna be
  missing things."* So: no splitting into two files (two files means reading one and
  missing the other), current state here, history in `docs/archive/notes/`. Trim toward this,
  not toward the line budget.
