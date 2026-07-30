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
history lives in `docs/notes/` and the archived log.

1. **A finding UPDATES AN OPTION; it never appends an entry.** About to add a numbered
   heading? Stop — this is not a log. `tests/test_goals_consistency.py` fails the build.
2. **Exactly one state marker per option: ✅ ❌ ⬜ 🔀.** Not two, not none.
3. **Every ✅ and ❌ cites a decision, sweep or note, or says it rests on NO MEASUREMENT.**
   A state with no measurement is UNTRIED, never "probably fine." A ❌ refused by opinion
   discards a good idea on an invalid measurement — the most expensive error available.
4. **Every claim names the configuration it was measured in.** Refutations are conditional
   on a config; `74` cost a whole comparison set by forgetting that.
5. **Every ❌ states its REVIVAL CONDITION.** Refutations expire, and one nobody can date
   is one nobody can retire — `107` and `111` both became right later.
6. **Every component carries a `⇒` verdict line**, DECIDED or OPEN, with the answer if
   decided. That line is what makes this scannable, which is the job.
7. **Update the CENSUS when a state changes.** The checker fails on a mismatch.
8. **Refutations are exhaustive; confirmations are not.** Every ❌ belongs here. Something
   that worked and changed nothing else is cited where it supports a state.
9. **At the line budget, trim the NEWEST writing first.** Process narration is true,
   already in commits and notes, and not what anyone consults this for.
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
    ([note 105](docs/notes/105-the-partitioning-accuracy-figure-has-no-source.md)).
11. **When first creating an option's record, SCAN THE ARCHIVE for it.** John's
    instruction, 2026-07-30, and the reason is that a record starting empty invites
    re-running work that `docs/notes/`, `docs/archive/` and the source already answer —
    which happened on 2026-07-30, when a partitioning result was nearly re-reported as
    new with `note 081` already holding it. Grep all three before writing the first entry.
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

**CENSUS: 30 chosen, 29 refuted, 15 untried, 11 both, 1 paused.** Checked against the body,
because a summary that can drift is how its predecessor caught its own counts.

> **Coverage, stated exactly, because a tree that looks complete and is not is worse than
> the log.** Every ❌ from the log is here; re-proposing a refuted mechanism is the failure
> this prevents. **Confirmations are NOT exhaustive** — for the full chronology of what
> worked, the log is where it is, and every option names its entries.

---

## 1. Input → concepts

How anything that is not already a discrete symbol becomes something the store can
address. **Deliberately not named after a mechanism** — a quantiser is one option,
not the category.

**⇒ DECIDED at the category level (163 §1): discrete ids, produced at the edge,
outside the learning loop.** Nothing non-text is built.

- ✅ **Discrete surface ids** — every input becomes a concept id, quantised at the edge.
  Keeps exact addressing, the structurally-zero gate and the sketch, all of which rest on
  identity. Targets video, audio, text and images; PDFs enter as text. `163 §1` John's
  ruling on note 052 §1's blast-radius argument, *no measurement*. Cost named at the time:
  *"our system plus a pretrained encoder"* is a different claim from *"our system"*.
  → record: [discrete-surface-ids.md](docs/options/discrete-surface-ids.md)
- ❌ **Address the store by continuous vector** — refuted on blast radius, not on a
  number. Destroys exact addressing (`O(N·ρ)`, and similar images raise ρ by
  construction), the gate's structurally-zero bar, and the sketch's need for exact
  repeats. `052 §1` reasoned, not measured. **Revival:** a task where
  similar-things-must-share-an-address beats exact separation.
  → record: [continuous-vector-addressing.md](docs/options/continuous-vector-addressing.md)
- ⬜ **Codebook learned by us, append-only** — new distinctions get NEW ids; existing ids
  never move. The occupancy gate is already a novelty detector, so minting a concept on
  novelty is reachable with measured parts (148). **Split it honestly:** the codebook is
  ours, the FEATURE SPACE is where off-the-shelf earns its place.
  → record: [learned-codebook.md](docs/options/learned-codebook.md)
- ⬜ **Per-node codebooks plus translation between them** — refused rather than untried:
  aligning two independently-learned discrete spaces with no paired data is the
  unsupervised-translation problem, strictly harder than the project's goal.
  → record: [per-node-codebooks.md](docs/options/per-node-codebooks.md)
- ✅ **`concepts.Merged` — the MERGE direction, and it does NOT move `of`.** Remapping the
  loser's surfaces strands every binding it means to preserve, so a merge is a **read-side
  gather** over `aliases()`. Union by MINIMUM id, not rank, which makes the class a
  property of the merge SET and propagation lazy with no coordinator. A late merge is a
  MISS, never a corruption. *no measurement* on the mechanism itself — 19 tests, 2
  mutations; what drives it is `note 077`/`078`, mutual agreement rather than confidence.
  → record: [merged-concepts.md](docs/options/merged-concepts.md)

**Open sub-question — codebook agreement across nodes.** Two nodes that quantise
the same input differently write to different addresses and the memory fragments
**with no node able to detect it locally.** `Merged` answers the MERGE direction; note
053's SPLIT (one thing → two ids), which only exists distributed, is still open.
Recommended: quantise once at ingest, codebook versioned as part of network identity.
Falsifier to build first: two nodes given identical input must emit identical ids, with
the companion that different input must differ.

## 2. Addressing — how a concept becomes a store address

**⇒ DECIDED: pair keys for relational work, identity-derived. 🔀 with single-token
keys, which are still the default and still correct for MQAR.**

- 🔀 **`PairKeys`** — hashed `(previous, token)`, so an entity's ROLES separate. `103`
  single-token keys collapse when an entity appears in two facts, 0.884 → 0.303; `104`
  pair keys largely fix it, 0.918 / 0.628, and the residual is the same entity in the
  same ROLE, which they cannot separate. `156`/`157` typed writes stop link/fact
  collision. *measured in:* 14 people 10 facts, and families with `family_links`.
  → record: [pair-keys.md](docs/options/pair-keys.md)
- 🔀 **`TableKeys`** — one key per token. Right where each entity appears once: `103`
  hop 1 is 0.959 at one appearance, and `142` the store carries MQAR completely under it.
  → record: [table-keys.md](docs/options/table-keys.md)
- ❌ **`ByConcept`** — map every surface to its concept's address. **Destroys exceptions,
  and confidently answers with the category's default**, which is the most dangerous shape
  a wrong answer has. `144`/`145` the majority wins and the dissenter goes to 0.000; `143`
  bought transfer at 0.9983. `049` **the load-bearing correction:** the store never
  collided — surface and concept are *different addresses*, so this was a READ POLICY, and
  that is why 148 cost a sketch rather than a representation. *measured in:* families,
  3 seeds. **Revival:** a task with no within-category variation to lose.
  → record: [by-concept.md](docs/options/by-concept.md)
- ❌ **Content-derived keys for ENTITIES, i.e. similar entities on NEARBY addresses** —
  §1 already refuses this under another name: **nearby addresses is what is refused,
  however the nearness arises.** `042 §2` ranked it third by blast radius and `g10-09`
  was **RETRACTED**, its cache indexed by token id so the question was never asked.
  Resolution is note 045's and already the architecture — similarity lives in a separate
  index. **Revival:** a task where similar-things-must-share-an-ADDRESS beats exact
  separation plus an index.
  → record: [content-derived-entity-keys.md](docs/options/content-derived-entity-keys.md)
- ⬜ **Structured representations for RELATIONS — the half `067` split off, and the live
  requirement.** Twenty relations must be **comparable** rather than exactly separated,
  and the store addresses by `(entity, relation)` so the entity supplies the exactness.
  `067` generalising composition is impossible without it (0.056 held out against chance
  0.050); `note 071` such a vector must not enter the address unguarded. **GOALS §1 asks
  for exactly this.**
  → record: [structured-relations.md](docs/options/structured-relations.md)
- ⬜ **A better index** — `note 056` made it load-bearing rather than a nicety: the set
  answer works at purity ≳ 0.99 and degrades fast below (0.750 at 0.951, 0.167 at 0.795),
  **so the grouping's quality bounds the answer.** `note 057` purity looks like the
  sufficient statistic, recorded as a hypothesis at n=12. `note 058` real language has no
  cliff. **Still untried:** what makes an index GOOD, as opposed to what makes a grouping
  hard.
  → record: [a-better-index.md](docs/options/a-better-index.md)

## 3. The store

### 3a. Structure

**⇒ DECIDED: one superposed `d × d` matrix. 🔀 with an exact cache and a settling
read.**

- 🔀 **`SuperposedRead`** — summed outer products, and it earns its place: `119` beats a
  bounded exact cache **8×** once bindings exceed slots. `109` capacity ~**d²**, at 90%
  recovery. *measured in:* direct outer products, **no decay, no cap** — the model's own
  write path reduces it.
  → record: [superposed-read.md](docs/options/superposed-read.md)
- 🔀 **`ExactCache`** and 🔀 **`SettlingRead`** — kept per 14c. The cache is the project's
  first controlled corpus gain, `69` **+0.19 bits** at 128 slots in `g11-06` — and `76`
  found it was mostly compensation for a weak readout. `SettlingRead` has unit tests and
  no experiment.
  → record: [exact-cache-and-settling-read.md](docs/options/exact-cache-and-settling-read.md)
- ❌ **Anything recovering per-item information AFTER the sum** — `r = M @ key` is a sum.
  Readout bias, competitive retrieval, orthogonal updates and pair-keys-for-recovery all
  failed for this one reason: `69` six mechanisms move the LEVEL and none the SLOPE.
  *measured in:* corpus, 4k–250k characters. **Do not re-propose.** **Revival:** a read
  that is not a sum.
  → record: [after-the-sum.md](docs/options/after-the-sum.md)

### 3b. Lifetime

**⇒ OPEN, and the question that has been asked wrong twice.**

- ✅ **Per-sequence, rebuilt every sequence** — current default. `62` confirmed
  empirically: with `learn=False`, predictions are byte-identical whether or not another
  sequence ran first, which is the guard that makes any cross-sequence claim falsifiable.
  → record: [per-sequence-store.md](docs/options/per-sequence-store.md)
- ✅ **Use-based eviction — `note 083`, and it is what makes C4's *forever* meaningful.**
  Discard whatever has gone longest unused: a persistently-queried fact survives **1.000
  with zero variance** where random eviction gets 0.717. **Bounded in content, unbounded
  in TIME** — fixed storage cannot hold everything, which is arithmetic. *measured in:*
  4,000 facts through 150 slots, 3 seeds. **Unexplained:** random is *worse* on persistent
  than abandoned.
  → record: [use-based-eviction.md](docs/options/use-based-eviction.md)
- ✅ **The two-timescale loop RUNS — `note 092`, and it cannot adjudicate.** Contradiction,
  blame, promotion and eviction assembled: recall back to **1.000** after six passes, blame
  falling 115 → 20. **But repair moves the damage to whichever side it does not trust** —
  identical corruption, relocated, exactly as `note 068` predicted before anything was
  built. What is missing is REDUNDANCY, and it is untried. *measured in:* 30% of facts
  corrupted.
  → record: [two-timescale-loop.md](docs/options/two-timescale-loop.md)
- ⬜ **An EXTERNAL persistent store — John, 2026-07-30, and it is sound.** Eviction becomes
  *archival*. The key and the routing already exist (`derived_keys`, `ownership.Ring`).
  **It cannot be in the traversal loop** — `docs/SCALE.md` leaves ~20% headroom and a DHT
  lookup is several hops — so it is a PREFETCH source and `lasting` becomes a cache over
  it. It moves the hard question rather than removing it, to a better failure mode.
  → record: [external-persistent-store.md](docs/options/external-persistent-store.md)
- 🔀 **`persistent_lasting`** — a consolidated slow store surviving sequences. **A real
  gain, switched off.** `133` beats baseline by **0.074–0.083 bits at EVERY data point**
  with a clean control, **and does not move the data wall** (+0.0124, under the seed
  spread). `note 082` rehabilitates it on a saturating stream — 0.020 → 1.000 — and shows
  it reduces to `note 080`'s correctness signal, bounded rather than unbounded. *measured
  in:* Tiny Shakespeare 4k–125k chars, 3 seeds. Off by default because turning it on
  invalidates the text comparison set, which `115` says may not be worth protecting.
  → record: [persistent-lasting.md](docs/options/persistent-lasting.md)
- ⬜ **`carry_store`** — carry the raw fast store between sequences. In a mutation and two
  unit tests, and **no experiment.** Correctly so: every relational task here redraws its
  facts per sequence on purpose (`047`), and `62`'s guard says carrying would answer from
  the training set. **`170`: there is no task in this repository on which persistence could
  pay** — unfalsified on the goal, not refuted.
  → record: [carry-store.md](docs/options/carry-store.md)

### 3c. Capacity, and the wall that is not one

**⇒ SETTLED and repeatedly re-opened. Read this before proposing anything about
saturation.**

- ✅ **The 16k-character wall is a property of the OBJECTIVE, not the architecture.**
  `115` **SATURATION IS CLOSED**: a character bigram table over 66 symbols is
  intrinsically low-rank, effective rank **~3 at every width**, so *"the store is not
  failing to use its width. There is nothing there to use."* It eliminated the competitors
  **by name** — store capacity (`109`), readout capacity (`110`), persistent
  representation (`114`) — and `113` shows width is not flat, so a width sweep tests a
  claim nobody makes. *measured in:* corpus, character level, widths 32–256.
  → record: [saturation-closed.md](docs/options/saturation-closed.md)
- ❌ **"The wall is a capacity limit"** — `133`'s relabel of its own null, contradicting
  `110` and `115`. *measured in:* Tiny Shakespeare, 3 seeds. **Revival:** a direct probe
  showing store or readout below task demand, which is what 109 and 110 measured the
  other way.
  → record: [wall-as-capacity-limit.md](docs/options/wall-as-capacity-limit.md)
- ❌ **"Concept partitioning is where the capacity comes from"** — `133`'s follow-on,
  superseded by `134` one entry later: pooled capacity is **identical** between
  arrangements. What differs is LONE-NODE capacity, which is a different claim and is the
  one that survives. *measured in:* per-node memory held equal, 5 seeds, 50 cells.
  **Revival:** a measurement where pooled capacity differs.
  → record: [partitioning-as-capacity-source.md](docs/options/partitioning-as-capacity-source.md)
- **`170` is the entry about this pattern:** 115's closure lived in one place and
  nothing pointed at it. **A ratchet on proposals does not catch a re-label after
  the fact.** This tree is the fix.
- **Real capacity crossover, still live:** `110` the linear readout holds 2.00
  items/dimension at every width where the store scales as d². They cross near
  **width ~100**, above which the readout binds. → `hidden`.

## 4. Selection & membership — the gate

**⇒ DECIDED: `inherit`. The project's cleanest mechanism and nothing in it is
fitted.**

- ✅ **`inherit` / occupancy sketch** — answer from your own address if **anything** was
  written there, else from your neighbours'. `148` 0.8100 / 0.4350 / 0.8183, the first arm
  good at all three, and the gate is **exact** — 1.0000 of TRANSFER, 0.0000 of DIRECT and
  EXCEPTION, every seed. **Why it works:** membership is *"is there anything here"*, so an
  unwritten address reads **exactly 0.0** and the threshold is structural, not tuned.
  `149` not a fitted constant; `150` costs nothing on MQAR; `153` it pays exactly where an
  address is read before it is written. *measured in:* families with exceptions, 3 seeds.
  → record: [inherit-gate.md](docs/options/inherit-gate.md)
- ❌ **Select by norm** — magnitude at an address says nothing about whether it is the
  right address, and it moves with the concept's popularity rather than the query. `147`
  0.247 on exceptions where plain addressing holds 0.783. *measured in:* families with
  exceptions. **Revival:** none foreseen — it is rule 7's shape, a criterion that cancels
  its own input.
  → record: [select-by-norm.md](docs/options/select-by-norm.md)
- ❌ **Select by decode margin** — confidence in *an* answer is not evidence about *which
  retrieval* produced it. `147` 0.581, below the summed baseline's 0.688. *measured in:*
  families with exceptions. **Revival:** the same quantity DOES work on a different
  question — `129`/`130` gate the search on it for +0.020.
  → record: [select-by-decode-margin.md](docs/options/select-by-decode-margin.md)
- ❌ **Sum the two retrievals** — averaging, so it cannot choose. `146`. *measured in:*
  families with exceptions. **Revival condition MET at `167`:** nothing has to be selected
  when the answer is a set, and the mechanism returns unchanged.
  → record: [sum-the-retrievals.md](docs/options/sum-the-retrievals.md)
- **The limit, and it is what `167` ran into:** the sketch knows **emptiness, not
  relevance.** It cannot bound an enumeration over addresses that are all
  occupied.

## 5. Composition — reaching what was never stated

**⇒ LARGELY SOLVED, `note 090`/`091`: a chain is walked by `search.beam` and named by a
fold over pairwise rules, with missing rules supplied by GENERATION DELTA — a conserved
quantity learned exactly from loop constraints. End task 0.5201 → 0.9668 symbolically and
0.8578 with the model recovering its own chains.** The open question is no longer how to
compose but whether an arbitrary domain has an invariant of this kind; kinship's is
additive and nothing else has been tried.

- ✅ **`hop_relation`** — bind a relation token into the hop's key, so a hop follows a
  NAMED edge. `158`, and `162` is why it blocks before the choosing question matters.
  *measured in:* kinship, where the query states the relation.
  → record: [hop-relation.md](docs/options/hop-relation.md)
- ✅ **`hop_relations`** — one relation PER HOP, so a walk follows LINK-then-FACT. `164`
  LINK→FACT reaches the linked family's value where both LINK→LINK and the pre-164
  mechanism stop at its representative. **Labelled an instrument, not the answer:** a
  schedule the task does not supply is a fitted constant (`162`). *measured in:* families
  with links, 3 seeds.
  → record: [hop-relations.md](docs/options/hop-relations.md)
- ⬜ **Try-all-and-gate** — follow every relation type, keep the one whose address is not
  empty: `r` reads, no new mechanism, and **the gate doing selection**. `163 §2` John:
  *"potentially the actual end solution."* **Its viability is a property of RELATION
  DENSITY and the dense case is refuted** — `108`, `(subject, relation)` names one person
  94.9% of the time while `(FACT, subject)` is undecided about half the time. **Where it
  works is where it is unnecessary.** Kept ⬜ because it was never measured, and `note 090`
  solved the problem another way.
  → record: [try-all-and-gate.md](docs/options/try-all-and-gate.md)
- ⬜ **Learned relation chooser** — `147`: two hand-made selection rules were refuted
  before membership worked, and a learned chooser is strictly harder. Also superseded by
  090's route.
  → record: [learned-relation-chooser.md](docs/options/learned-relation-chooser.md)
- ✅ **`search.py` beam search — `run()` CALLS IT, `search_beam_width=4` by default.**
  Branches at EVERY step, where `search` hedges only at the root — which `note 064`
  measured as the wrong place, since the relation decode is 0.974 there and ~0.91
  mid-chain. **+0.041 ±0.013 over `search4`** on `run()`'s own task (kinship, hops 2,
  8 seeds, `note 103`); on CLUTRR chain recovery the gain is +0.107, **a different task
  and depth — do not quote one for the other**. `search_prune_every` stays 1: period 2
  costs −0.016 ±0.006 and is a knob for meeting `d_max`, not a default.
  → record: [docs/options/beam-search.md](docs/options/beam-search.md)
- ✅ **`search.beam` — branch at EVERY step, pruned.** Beats single-step branching on every
  seed of both harnesses. **`note 075`: `note 065`'s +0.2190 does NOT reproduce** — the gain
  is **+0.107**, and 065's config is unrecovered. **713/713 on the plain subset is reached
  under PARTITIONING**, beam **0.9220** at 4 nodes against 0.8877 monolithic (`note 105`).
  **A MECHANISM, not a margin** (`note 103`). Cost 4× the reads; unpruned is
  `branches^h`. **⇒ It corrects `note 063`**, which put the ceiling on route-finding rather
  than naming. *measured in:* CLUTRR chain recovery, width 64, 3 seeds.
  → record: [beam-search.md](docs/options/beam-search.md)
- ✅ **A hop REPLACES a retrieval, it does not combine with it** — `101`. `102` built the
  accumulator and recorded that the stated reason for choosing it was wrong. `103`'s oracle
  is what showed the readout was getting nothing from hop 1. *measured in:* kinship.
  → record: [hop-replaces-retrieval.md](docs/options/hop-replaces-retrieval.md)
- ❌ **Another mechanism stacked on noisy retrieval** — four tried, all failed against the
  same 0.915/0.35 ceiling: `102`, `105`, `107`, `111`. **Do not re-propose:** the fix is
  per-step fidelity, not another layer. `105` is the one where the combination **produced
  numbers anyway**. *measured in:* kinship. **Two conditions EXPIRED at `121`/`122`**, which
  is why the traversal was eventually built.
  → record: [stacked-on-noisy-retrieval.md](docs/options/stacked-on-noisy-retrieval.md)
- 🔀 **`hop_accumulate`: `concat` vs `bind`** — concat wins 1.000 to 0.812, **but only
  because 16 rules in a 128-wide space are linearly separable whatever the labels do.**
  `bind` is kept for that reason. **`note 063`: SCALE.md's trigger is MET** — CLUTRR has
  1,393 chains, 99.8% of test chains unseen, so a **fold over pairwise rules** is what
  generalises. **`note 066` corrects 063 both ways**, and the fold is right **98.8% where it
  can act** while completing only **52.6%** — tabulation's ceiling. *measured in:* 16 rules,
  10 relations, 128-wide; then CLUTRR.
  → record: [hop-accumulate.md](docs/options/hop-accumulate.md)
- ✅ **GENERATION DELTA, learned from cycles — `note 090`, and it CLOSES the ceiling.**
  9,074 loop equations, 20 unknowns, null space **1**, and **20/20 deltas recovered
  exactly**. End task **0.5201 → 0.9668** symbolically, and **0.8578 end to end** with the
  model recovering its own chains (`note 091`). **CONTROL: a deliberately WRONG delta scores
  0.5681, below random's 0.6081** — so the displacement is the mechanism, not the filling.
  **SCOPED by `note 104`:** DBpedia EN and DE have **no additive invariant**, and not
  approximately, so it is *"solved wherever a conserved quantity exists"*. **Revival of the
  general case:** invariants per SUB-DOMAIN, a different computation, unbuilt. *measured in:*
  CLUTRR kinship.
  → record: [generation-delta.md](docs/options/generation-delta.md)
- ❌ **Naming the missing rule, by any learned readout — `note 088`.** Scores **0.5995 end
  task, BELOW random filling's 0.6081 ± 0.0055**, and `majority` is worse still, so
  systematic error costs more than noise. Note 070's 0.223 held-out was a random quarter;
  the rules that matter are an adversarially withheld family. `note 084` self-training does
  not lift it; `note 085` associativity **verifies what it cannot generate**. *measured in:*
  CLUTRR, 10 seeds. **Revival: a mechanism that beats 0.6081 end-task**, which is the bar
  090 clears.
  → record: [naming-the-missing-rule.md](docs/options/naming-the-missing-rule.md)
- ⬜ **`index_at_hops` combined with the position-level index** — `159`/`160`/`161` built
  the pieces; `154` measured that the guard's premise is false, a hop key sits at cosine
  **0.96** to a single token's row. Blocked on an instrument, not a mechanism: **no task has
  both** an address-never-written and a composition (`note 050`).
  → record: [index-at-hops.md](docs/options/index-at-hops.md)

## 6. The answer — what a response IS

**⇒ OPEN, and this is the live question. It is where the project's stated goal
lives, and until 2026-07-29 nothing here had ever scored a multi-token answer.**

- ✅ **Set of tokens, scored by `exact` and F1** — the measurement convention, `165`, built
  BEFORE anything produced a set. **Recall alone is never reported:** emitting the whole
  alphabet scores recall 1.000 and F1 0.400, and that trap fired within one commit. It
  degenerates exactly, so every earlier singleton number stays comparable. *measured in:*
  `openplexus/answers.py`, dependency-free.
  → record: [set-of-tokens.md](docs/options/set-of-tokens.md)
- ✅ **Emit by gated collection over index-proposed neighbours** — `167`. **This is decision
  146's refuted mechanism, unchanged**: 146 found it can only average and 147 refuted the
  ways to choose, and **neither objection applies to a set answer, because nothing has to be
  selected.** The refutation was about the question. *measured in:* families, set-valued.
  → record: [gated-collection.md](docs/options/gated-collection.md)
- 🔀 **Bound the enumeration by the biggest similarity gap** — an argmax over gaps, not a
  threshold, the same move `148` made. Matches the best fixed `branches` at family sizes 3–6
  **without being told the size**, with `look` a ceiling rather than a target. **`note 058`:
  real word co-occurrence has NO cliff** — largest gap 0.059 against the task's 0.424, and
  **at no setting is the profile bimodal**. *measured in:* families at index purity 1.000.
  → record: [biggest-similarity-gap.md](docs/options/biggest-similarity-gap.md)
- 🔀 **Fixed `branches`** — the count supplied. `167`: the peak sits at `family_size − 1` in
  every row and collapses either side. **`note 056`: a measured CROSSOVER, not a loser** —
  degrade the grouping and the gap rule falls faster, because deriving the count adds a
  second error source. Decision 74's shape again, which is what 🔀 is for: **which is right
  is a property of the grouping's quality, not of either mechanism.**
  → record: [fixed-branches.md](docs/options/fixed-branches.md)

> **So F3's remaining gap is sharper than "the size is supplied":** the enumeration
> bound is **either supplied, or it needs a near-oracle grouping.** Neither is
> answering from awareness, which is why this row is PARTIAL and not PASSING.
- ⬜ **Autoregressive output** — *not* ruled out by GOALS §2, which forbids next-token
  prediction as the TRAINING OBJECTIVE, a different thing from autoregression as an output
  MECHANISM. What argues against it is **termination**: it needs a learned end-token, where
  a gated walk stops where `148` reads structurally zero.
  → record: [autoregressive-output.md](docs/options/autoregressive-output.md)
- ❌ **Structured slots** — not a peer of the others. A fixed frame is a traversal with a
  fixed relation schedule, which `162` already calls a fitted constant. *no measurement* — a
  scope ruling. **Revival:** a domain where the frame genuinely is supplied by the task.
  → record: [structured-slots.md](docs/options/structured-slots.md)
- ⬜ **Declining to answer** — the archived ledger's row C4. **Nothing anywhere lets the
  model say "I do not know", and no task scores abstention**, while the gate is a fact about
  the store rather than a learned probability. An untested claim about honesty.
  → record: [declining-to-answer.md](docs/options/declining-to-answer.md)

### 6b. Knowing when to stop hopping

**⇒ DECIDED: a learned halting gate. It works and it is not confidence.**

- ✅ **`halt_gate`, learned** — reads the retrieval and decides whether to hop again. `086`
  a halting signal exists **and it is not confidence** — what separates is the CONTENT.
  `087` mixed depths reach **1.000**, and **the mutation harness caught two defects the
  tests did not**. `092` **it generalises to a depth it never trained on, zero-shot**
  (0.992). `089` it is a token detector, measured, and the sign was the opposite of what was
  predicted. *measured in:* chains and kinship at mixed depths.
  → record: [halt-gate.md](docs/options/halt-gate.md)
- ❌ **Transferring the gate to new terminator tokens** — impossible *by construction*: two
  markers have unrelated value vectors under a frozen random `Wv`. `089`. **Do not
  re-propose.** **Revival:** a `Wv` in which terminators share structure.
  → record: [gate-transfer.md](docs/options/gate-transfer.md)
- ❌ **A token-agnostic terminal signal** — `093` there is none, and that is what points at
  frozen `Wv`. `094` `value_lr` does not build a terminator class, and making separators
  targets breaks the gate. *measured in:* chains with several terminator markers.
  **Revival:** a representation where a terminator class exists to be found.
  → record: [token-agnostic-terminal.md](docs/options/token-agnostic-terminal.md)
- ❌ **Occupancy as a free halting signal** — `153`: half the gate can go where the index
  cannot, and it has **nothing to say** there. Chain start/middle/end at 0.893/0.791/0.898
  is not a signal, because a traversal writes every address before querying it. **Revival:**
  a task where a walk can run off the end of what was written.
  → record: [occupancy-as-halting.md](docs/options/occupancy-as-halting.md)

## 7. Output → surface

**⇒ OPEN and off the critical path. Blast radius near zero, which is why it does
not gate anything above.**

- ✅ **Template realiser** — `openplexus/render.py`, deterministic, dependency-free,
  **structurally incapable of adding a fact.** John's ruling: templates first.
  *no measurement* — a floor, not a mechanism with a number. It contributes a **bar**:
  `content_words(render(...)) - FRAME` must EQUAL the answer set, so dropping a value fails
  as well as inventing one. Empty set **declines** rather than rendering a hole.
  → record: [template-realiser.md](docs/options/template-realiser.md)
- ✅ **Retrieval realiser** — `render.speak`. **The words come from the CONCEPT MAP, not the
  caller**, so the model supplies its own vocabulary and `render` arranges it.
  *no measurement*. The default policy is arbitrary and says so, with a connection test
  asserting counts MOVE the choice; `spoken_faithfully` is the same bar one level down,
  **checked in both directions**. Neither is the eventual answer — with several modalities
  the choice belongs to the QUERY.
  → record: [retrieval-realiser.md](docs/options/retrieval-realiser.md)
- ⬜ **Small learned renderer trained on our own concept sets** — with a **faithfulness**
  test rather than an accuracy one: perturb the set and the text must move; hold the set and
  the text must contain nothing the set does not.
  → record: [learned-renderer.md](docs/options/learned-renderer.md)
- ❌ **Off-the-shelf LLM as renderer** — cheapest demo, worst for the claim. A fluent
  renderer **can produce the right sentence from a wrong walk**, so the end-to-end number
  would measure its world knowledge. Rule 2 exactly. *no measurement* — refused on the
  grounds rule 2 is written on. **Revival condition:** a faithfulness test showing it cannot
  add or drop a fact.
  → record: [llm-renderer.md](docs/options/llm-renderer.md)
- ✅ **No renderer, for programmatic use** — for a query API or an agent tool, a set of typed
  bindings is *better* than a sentence. *no measurement* — a scope position, and it is what
  makes component 7 non-blocking for everything above it.
  → record: [no-renderer.md](docs/options/no-renderer.md)

## 8. What learns

**⇒ OPEN. The narrowest description of the whole architectural problem.**

- ✅ **`Wo` only, delta rule at scored positions** — the exact gradient for a single linear
  readout, so it is not an approximation of backprop; there is nothing to backpropagate
  through. `042 §4` **the rule is not the limitation; the absence of anything for it to
  write to is** — `Wk` and `Wv` frozen random and the store rebuilt per sequence, so
  **everything durable is one linear map**.
  → record: [wo-only-delta-rule.md](docs/options/wo-only-delta-rule.md)
- ❌ **`value_lr` / `value_centre` to unfreeze the values** — the values move a long way,
  stay spread, and the plateau does not budge. `114` it works and it does not help; `94` it
  does not build a terminator class; `69` it costs −0.45 on text. *measured in:* corpus, and
  chains with terminator markers. **Do not re-propose as a fix for collapse.** **Revival:**
  a task where the value space itself is the bottleneck.
  → record: [value-lr.md](docs/options/value-lr.md)
- 🔀 **`hidden` readout** — the largest single factor on text (`70`, overstated there and
  corrected by `71`), and the answer when the readout/store crossover binds above width ~100
  (`110`). **Two "refuted" mechanisms partially recover under it** (`74`, `76`, `77`), which
  is the calibration behind measurements being conditional on their configuration.
  → record: [hidden-readout.md](docs/options/hidden-readout.md)
- ⬜ **Self-modifying structure** — nothing to modify: the store is `d × d`, fixed at
  construction. `042` is right that 3b and 10 are prerequisites, not alternatives. Reserve
  the seam, build when a task can tell whether it helped.
  → record: [self-modifying-structure.md](docs/options/self-modifying-structure.md)

## 9. Distribution

**⇒ THE DRIVER IS GONE FROM THE READ PATH, `note 093`/`094` — AND THE PATH MISSES `d_max`
BEYOND DEPTH 7, `note 101`.** A read goes to the one peer holding the fact: 2 messages
rather than 2N, no sum, so C1's collective is off the read path. Writes reach every
holder (`note 098`), a departure costs a round trip rather than the answer (`note 097`),
routing is consistent hashing (`note 095`), and the wire plus the ring are fingerprinted
(`note 096`/`099`). **But C2 is a deadline on a WALK**, and a hop is two dependent round
trips, so depth 10 costs 1,000 ms against 640 ms even with a hop's reads batched.
**Dimension splitting remains the default and `concept_nodes` is still 0**; the peer
transport is a parallel path nothing in `run()` uses yet.

- ✅ **Partition by dimension** — every node computes `M_slice @ key_slice` and inherits the
  sum. Current default. `g4-01` a lone node's answer holds at 16 dims (0.949) and degrades
  fast below: 8 → 0.681, 4 → 0.412. **So node count ≈ width ÷ 16**, not anything softer.
  *measured in:* kinship.
  → record: [partition-by-dimension.md](docs/options/partition-by-dimension.md)
- 🔀 **Partition by concept** — `concept_nodes`, `partitioned.py`, `ConceptStore`, and
  `peer.py` over sockets. **`note 081` makes it MANDATORY, not merely better:** C4 needs
  capacity that GROWS and this is the only mechanism that supplies it (`nodes × per-node`,
  each node holding a full-width store), since no-decay saturates and decay windows forget.
  Accuracy improves too — beam **0.9220 at 4 nodes against 0.8877** monolithic
  (`note 105`, re-measured after the tree was found citing this to a note that does not
  contain it). **Blocked
  from being the default by six combination refusals** in `LocalMemoryConfig`, and off by
  default (`concept_nodes = 0`).
  → record: [docs/options/concept-partitioning.md](docs/options/concept-partitioning.md)
- ✅ **`openplexus/peer.py` — point-to-point reads and writes, no driver.** `notes 093`–`099`.
  **2 messages per read against 2N for broadcast** — 256× at 256 peers — and the
  serialisation point goes with it. A driver-free `beam` traversal is **exact** and a
  misrouted control changes it; consistent hashing moves **1.4%** of concepts on a join
  where modulo moved 98.4%; a departure costs a round trip, not the answer, and **both
  halves are needed — either alone looks fine**; the wire format is fingerprinted and
  **caught `PROTOCOL` 3 the day after it was written**. Batching takes depth 10 from
  3,850 ms to **1,000 ms**, which is **necessary and not sufficient against `d_max` 640 ms**.
  **A MIGRATING walk is where the remaining 2× is, and it is NOT BUILT** — `note 102` prices
  the rendezvous at 0.089 with its period unmeasurable, so a walk must meet, not meet every
  hop. *measured in:* loopback only, priced at an assumed 50 ms RTT.
  → record: [peer-transport.md](docs/options/peer-transport.md)
- ❌ **The global dimension-summing readout** — the globally synchronised step **C1
  forbids**, the project's own first constraint. Surfaced in a footnote to
  [note 009](docs/notes/009-splitting-the-memory.md) §4 **after four gates were passed and
  five sweeps run on top of it.** `combine="vote"` mitigates the BANDWIDTH and not the
  violation; a concept-partitioned read is a **selection**, which is what removes it.
  **Revival:** none while C1 stands, and the arithmetic refuses it independently.
  → record: [global-summing-readout.md](docs/options/global-summing-readout.md)
- ✅ **Transport: vote-based, with suspicion and a deadline** — sound and ahead of the rest.
  `128` `d_max` ~**640 ms** = 3× a measured p99, **a floor and not a constant**. `126`/`127`
  the detector ejected nodes permanently where SWIM says suspect and retry. `169` the
  deadline's actual branch had **no test until a silent peer existed**. *measured in:* 4
  nodes, width 16, Docker bridge with `tc netem` at 80 ms + 20 ms jitter + 2% loss.
  → record: [transport-vote-deadline.md](docs/options/transport-vote-deadline.md)
- ⬜ **Untrusted nodes** — no threat model at all. A node that lies about occupancy or writes
  to addresses it does not own. **Forks on the project's endgame:**
  open-source-and-runs-everywhere implies it; a controlled network does not.
  → record: [untrusted-nodes.md](docs/options/untrusted-nodes.md)
- ⬜ **Slice negotiation** — static by John's explicit choice; a node that negotiates its own
  slice is a coordination protocol and nothing needs one yet.
  → record: [slice-negotiation.md](docs/options/slice-negotiation.md)

## 10. The objective and the instruments

**⇒ DECIDED: relational, not next-token. The instruments are all self-designed and
that is the standing weakness.**

- ✅ **Relational objective** — GOALS §1; next-token prediction is an explicit non-goal in
  §2. `047` **the objective was the ceiling, not the memory** — the only relation the store
  can express on a next-token objective is an n-gram. `142` the store carries MQAR
  **completely** (0.995 vs 0.000); `136`/`139` at word level it contributes nothing and is
  exactly substitutable by a learned prior. *measured in:* corpus and MQAR.
  → record: [relational-objective.md](docs/options/relational-objective.md)
- ❌ **Bits per token as evidence about the store** — the objective is n-gram bounded, so it
  cannot show what the store adds. `142`, `047`. **Do not re-propose.** **Revival:** an
  objective over text that is not next-token.
  → record: [bits-per-token.md](docs/options/bits-per-token.md)
- ❌ **Training on every position** — costs composition **1.000 → 0.40**. `095`–`098` is the
  whole line, ending at `098`: giving the gate its OWN objective is what removes the decay.
  *measured in:* composition over chains. **Do not re-propose without a separate gate
  objective** — which is also the **revival condition, and `098` says it is met** with
  `gate_objective` set.
  → record: [training-every-position.md](docs/options/training-every-position.md)
- ❌ **Perpetual learning as a repair for churn** — `091` it does not heal churn, **because
  churn costs capacity** rather than knowledge; treat +0.008 as a direction. **C4 IS NOW
  TESTED (`note 081`/`082`), and `091`/`092` failed only because their tasks never
  saturated**: at 10.6× capacity recall is **0.07** and *symmetric*, so it is interference
  and replay cannot fix it. **The answer is two multipliers**, consolidation and
  partitioning, and neither suffices. **Revival:** none as a churn repair.
  → record: [perpetual-learning-for-churn.md](docs/options/perpetual-learning-for-churn.md)
- ❌ **Concept addressing as a fix for text prediction** — 0.540 bits at bias 0, and **a
  grouping built from SHUFFLED text does as well.** The address count did the work, not the
  concepts. `141`. *measured in:* corpus, character level. **Revival:** a text objective
  that is not next-token.
  → record: [concept-addressing-for-text.md](docs/options/concept-addressing-for-text.md)
- ✅ **`families.py`** — the only instrument where things RESEMBLE each other, so the only
  one where a concept can mean something. `143` is its first result; `166` gave it a
  set-valued question, the first in the repository a single token cannot answer.
  → record: [families-instrument.md](docs/options/families-instrument.md)
- ✅ **`closure.py`** — unmarked stream of stated and entailed facts, no question marker, so
  the stated/entailed split IS the recall/reasoning split. `g14-01` passes G0 with entailed
  headroom **0.277** against a frozen 0.000.
  → record: [closure-instrument.md](docs/options/closure-instrument.md)
- 🔀 **`kinship.py`** the mechanism testbed and `run()`'s own task · **`mqar.py`** the
  store's control, the only instrument isolating it from a prior (`142`) · **`chains.py`**
  solved at 1.000, out-degree 1 by construction, a control.
  → record: [kinship-mqar-chains.md](docs/options/kinship-mqar-chains.md)
- ❌ **A composition sweep on chains as evidence about composition** — a chain is out-degree
  1 by construction, so nothing chooses. `108`, and `note 103` measures it from the other
  side. **Do not re-propose.** **Revival:** none for chains; the instrument for the question
  is one with genuine out-degree.
  → record: [composition-sweep-on-chains.md](docs/options/composition-sweep-on-chains.md)
- ⏸ **`corpus.py`** — PAUSED, not condemned. Closed by 115/118, reopened by g17-01, and
  135–142 measured on it without anyone re-deciding it was the instrument.
  → record: [corpus-instrument.md](docs/options/corpus-instrument.md)
- ❌ **`reward_recall.py`** — retired, `126`. Its requirements list turns out to describe
  **bsuite's Memory Length test**: the list was a search query and was not used as one.
  **Revival:** the literature's version, if a memory-length instrument is wanted.
  → record: [reward-recall.md](docs/options/reward-recall.md)
- ✅ **CLUTRR-symbolic — the first external instrument.** Graph layer, never the prose, so
  results are *"CLUTRR-symbolic"* and published text numbers are not comparable.
  `gen_train23_test2to10`, layout **`kinship`** (collisions 35.9% → 7.7%). **Report per hop
  bucket and split on ENTITY REPETITION** — `note 059`, test is 37.8% repeated where train
  is 0%, so a falling curve reads as depth and is really addressing. `note 060` the `hops=1`
  floor is **0.0856**, not chance. **What it cannot test: concept acquisition** (`note 076`).
  → record: [clutrr-symbolic.md](docs/options/clutrr-symbolic.md)
- ✅ **OpenEA `EN_DE_15K_V2` — the acquisition instrument, FETCHED with John's approval.**
  Two DBpedia graphs, 15,000 gold links, URIs **encoded** so string matching cannot cheat.
  Chosen on two measurements: **74.0%** shared relation vocabulary, and a degree floor CLUTRR
  cannot reach. `note 077` zero supervision reaches hits@1 **0.0389** at 583× chance and is
  monotone in evidence; `note 078` bootstrapping reaches **0.3098**, and **a confidence gate
  makes it WORSE**, so mutuality is the merge gate and magnitude is not. **Not the hard
  setting:** `D_W`/`D_Y` share **0.0%** of their relations, and a vocabulary-free seed is
  untried.
  → record: [openea.md](docs/options/openea.md)
- ❌ **`4.540` bits/char, "unigram BEATEN"** — the project's headline text result for weeks,
  and **not a measurement of this model.** `117` the named configuration scores 5.665–5.742
  against a prequential unigram of 4.776. `118` the figure appears in no sweep and no entry,
  and traces to an offline backprop probe on frozen features. **Wrong twice.** Kept because
  the failure is reusable — **an inherited headline with no provenance outranks every
  measurement downstream of it.** **Revival:** none.
  → record: [the-4540-headline.md](docs/options/the-4540-headline.md)
- ❌ **Scoring without a temperature** — `117`'s first attempt read 5.920 against a uniform
  5.954. The delta rule targets a one-hot, so raw scores sit in about [0, 1] and a softmax
  over that range is nearly uniform. **A calibration artefact that looks exactly like a null
  result.** **Revival:** none — it is a defect, not a setting.
  → record: [scoring-without-temperature.md](docs/options/scoring-without-temperature.md)
- ❌ **`9.323` as the word-level unigram** — `135` it was never that, and the temperature
  grid was too narrow at word level. **A wrong baseline moves every arm together.**
  **Revival:** none; baselines are computed by the dependency-free ruler.
  → record: [word-level-unigram.md](docs/options/word-level-unigram.md)
- ❌ **Everything measured by the g18 harness before the fix** — `138` **RETRACTION: it
  trained on the wrong target.** Survived four sweeps and 142 cells because every arm was
  wrong identically. **What caught it was a figure the project had already measured.
  Internal consistency is not evidence.** **Revival:** none for those numbers; the corrected
  harness is a different instrument.
  → record: [g18-harness.md](docs/options/g18-harness.md)
- ✅ **g17-01's premise survives its own correction** — `140` the pivot was not an artefact,
  which is the one thing in this line that held, and it was sorted deliberately rather than
  retracted with its neighbours.
  → record: [g17-01-premise.md](docs/options/g17-01-premise.md)
- ❌ **Note 050's linked-families task as first designed** — `155` refuted by its own rail on
  the first run, a p90 calibration that flagged what chance produces. The example of a
  fairness check paying immediately. **Revival condition MET at `164`:** the blocker was that
  a hop could not carry its own relation.
  → record: [linked-families-task.md](docs/options/linked-families-task.md)

## 11. Verification apparatus

**⇒ DECIDED and deliberately permanent. Moved out of the tree** — every item documents
itself in its own docstring and CLAUDE.md rules 6, 10, 11 and 14 carry the policy, so it
was spending lines in a document whose criterion is being readable in one pass. Nothing in
it has ever been re-litigated, which is the only thing the tree prevents.

- ✅ **The apparatus** — mutation harness (sharded 6 ways in CI; `--verify` is the authority
  on the count), a dependency-free ruler in `tasks/`/`baselines.py`/`answers.py`
  (`note 007`), the rails (`check_workflows`, `check_rails`, `check_duplication`,
  `check_decisions`, `check_options`, `check_provenance`), and a sensitivity check on any
  timing assertion — `169`: three attempts at one assertion and **the first two both passed
  when written.**
  → record: [verification-apparatus.md](docs/options/verification-apparatus.md) ·
  full account: [archived](docs/archive/verification-apparatus-2026-07-30.md)

---

## Standing agreements

- **Blanket permission for architectural decisions.** The pending-decisions list is
  a REPORT, not a gate. Decide, proceed, record which calls were made alone.
- **List pending decisions at the end of every response.** John reads from a phone.
- **Explain plainly, keep the numbers, do not hide bad news.**
- **Goal ordering:** AGI is primary; being an LLM replacement on consumer machines
  is secondary and must not compete with it.
- **Biology gives policies, not representations.** Take mechanisms from computer
  science where the problem is well understood.
- **Scheduled wake-ups DO NOT FIRE.** A persistent `Monitor` emitting a heartbeat
  is what works.
- **RUN UNATTENDED, AND KEEP RUNNING — John, 2026-07-30.** Three parts, and they are one
  instruction: *"keep a 5 minute monitor wakeup going as long as there is still a clear next
  step forward toward the goal, always focus on blocking/harder problems before simpler
  stuff, and make any decision necessary to move forward if I'm not around."*
  - **The heartbeat is 5 minutes, and its stopping condition is the absence of a clear next
    step** — not the absence of an answer, not the end of a task, and not the arrival of a
    convenient pause. While one exists, the loop stays armed.
  - **Blocking and harder first.** This is the counter to the gradient CLAUDE.md rule 17
    names: every audit yields a satisfying provable result and every new mechanism most
    likely yields a null, so the easy work is always the work that feels productive. Order
    by what is blocking, then by what is hard — never by what is ready.
  - **Decide, do not wait.** The existing blanket permission covered architectural calls;
    this widens it to **any** decision needed to keep moving, with one boundary: it may not
    countermand a goal or constraint already agreed. Record which calls were made alone.
- **Input and output is John's call** — his framing is that if the AGI goal wins,
  inputs should look like a body: a loop with consequences, not a passive feed.
- **The endgame is undecided** — commercial, open source, or both — and John holds
  that an AGI used the way current chat agents are used would be immoral.
  Recommendations must not quietly assume an answer.
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
  missing the other), current state here, history in `docs/notes/`. Trim toward this,
  not toward the line budget.
