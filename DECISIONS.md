# Decisions — the option tree

**What this is.** Every architectural component, the options for it, and which
option each is. **One page, scannable, so a settled question stops being
re-opened.** That is the only job it has.

**Why it was rebuilt.** It was a 6,040-line append-only log — unreadable whole, so
read selectively, which on 2026-07-29 produced three wrong recommendations in a row
off claims later entries had already superseded. **A log records; it does not
prevent.** Full account in `CLAUDE.md` rule 14b.

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
10. **Detail lives in notes and the log; this carries the claim and links them.** A
    measurement belongs to exactly one file.

---

## How to read a row

    ✅ CHOSEN      decided, built, and this is what we use
    ❌ REFUTED     measured and it lost. The revival condition is stated
    ⬜ UNTRIED     no measurement. Not "probably fine"
    🔀 LIVE BOTH   two or more kept behind a switch and re-tested as the system
                   changes. A valid END state, not indecision

**CENSUS: 29 chosen, 29 refuted, 15 untried, 12 both, 1 paused.** Checked against the body,
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

- ✅ **Discrete surface ids** — every input becomes a concept id. Keeps exact
  addressing, the structurally-zero gate, and the sketch, all of which rest on
  identity. Field precedent: VQ-VAE, discrete audio codecs.
  - `163 §1` John accepted. Targets: video, audio, text, images; PDFs enter as
    text. *measured in:* no measurement — a design ruling on 052 §1's analysis
  - Cost named at the time: a stock encoder is a large pretrained model in the
    pipeline, so *"our system plus a pretrained encoder"* is a different claim
- ❌ **Address the store by continuous vector** — refuted on blast radius, not on a
  number. Destroys exact addressing (interference is `O(N·ρ)` and similar images
  raise ρ by construction), the gate's structurally-zero bar becomes a tuned
  threshold, and `AddressSketch` needs exact repeats to collide.
  - `052 §1` reasoned, not measured. **Revival condition:** a task where
    similar-things-must-share-an-address beats exact separation.
- ⬜ **Codebook learned by us, append-only** — new distinctions get NEW ids;
  existing ids never move, so nothing is re-addressed. The occupancy gate already
  answers *"was anything ever written here"* with a structural zero, which is a
  novelty detector — so minting a concept on novelty is reachable with parts that
  exist and are measured (148).
  - John raised this 2026-07-29. **Split it honestly:** the codebook (which
    concepts exist) is ours; the FEATURE SPACE (what makes two images similar at
    all) is where off-the-shelf earns its place.
- ⬜ **Per-node codebooks plus translation between them** — refused rather than
  untried: aligning two independently-learned discrete spaces with no paired data
  is the unsupervised-translation problem, strictly harder than the project's goal.
  Solving it as a *precondition* is the wrong order.
  - [note 053](docs/notes/053-two-nodes-must-agree-on-what-a-picture-is.md)
- ✅ **`concepts.Merged` — the MERGE direction, built, and it does NOT move `of`.** The
  obvious design (remap the loser's surfaces) strands every binding it means to preserve,
  because `ByConcept` builds the key from the concept id. So writes always land on a
  surface's own concept and the merge is a **read-side** gather over `aliases()`. Cost is
  `k` reads at `k` addresses for a class of `k`; a later lazy consolidation shrinks it
  without breaking a read, which re-keying cannot promise.
  - **Union by MINIMUM id, not by rank** — rank makes the representative depend on arrival
    order, and `Surfaces.of` promises the same answer on every node forever. Minimum makes
    it a property of the merge SET, so **propagation is lazy and needs no coordinator**
  - **A late merge is a MISS, never a corruption**, and un-merging is free: `of` never
    moved, so dropping an alias strands nothing. 19 tests, 2 mutations
  - **What drives it:** `note 077`/`078` — mutual agreement, not a confidence threshold

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

- 🔀 **`PairKeys`** — hashed `(previous, token)`, so an entity's ROLES separate.
  - `103` single-token keys collapse when an entity appears in two facts: 0.884 at
    one appearance → 0.303 at two. *measured in:* 14 people, 10 facts
  - `104` pair keys largely fix it: 0.918 / 0.628. Residual at 2+ appearances is
    the same entity in the same ROLE, which pair keys cannot separate
  - `156` typing an address costs nothing and pays at low load
  - `157` typed writes stop link/fact collision: every column within 0.05 of its
    link-free value (0.8333 / 0.4383 / 0.8150) where untyped collapsed to
    0.13 / 0.03 / 0.12. *measured in:* families with `family_links`
- 🔀 **`TableKeys`** — one key per token. Default, and right where each entity
  appears once.
- ❌ **`ByConcept`** — map every surface to its concept's address. Destroys
  exceptions.
  - `144`,`145` the majority wins and the dissenting fact goes to 0.000. `143`
    bought transfer (0.998) and `144` measured the price: exception 0.371, and it
    says a sibling's value 86.6% of the time
  - `049` **the load-bearing correction:** the store never collided. A fact at the
    surface key and a default at the concept key are *different addresses*.
    `ByConcept` was a READ POLICY, which is why 148 cost a sketch and not a
    representation
- ❌ **Content-derived keys for ENTITIES, i.e. similar entities on NEARBY addresses** —
  this entry exists because reading the tree as a tree found it contradicting §1, and
  **`note 067` then split it: the refusal is right for entities and does not transfer to
  relations.**
  - `042 §2` ranked it third by blast radius — *"the store has no notion of similarity
    at all"* — and `g10-09` was **RETRACTED**, its cache indexed by token id so the
    question was never asked
  - **§1 already refuses this under another name.** "Address the store by continuous
    vector" is ❌ *because* nearby addresses raise `ρ` and interference is `O(N·ρ)`,
    which also turns the gate's structurally-zero bar into a tuned threshold. **Nearby
    addresses is what is refused, however the nearness arises** — and with thousands of
    entities that is fatal
  - **Resolution is note 045's and already the architecture:** similarity lives in a
    SEPARATE INDEX; `ContentIndex` proposes, nothing addresses by it. **Revival:** a task
    where similar-things-must-share-an-ADDRESS beats exact separation plus an index
- ⬜ **Structured representations for RELATIONS — the half `067` split off, and it is
  the live requirement.** Twenty relations, which must be **comparable** rather than
  exactly separated, and the store addresses by `(entity, relation)` where the entity
  supplies the exactness — so `O(N·ρ)` does not bite. `067` measured that generalising
  composition is impossible without it (0.056 held out), and **GOALS §1 asks for exactly
  this**: *"be aware of the differences and interrelations between them"*
- ⬜ **A better index, which is what 042 §2 was actually reaching for** — and
  `note 056` made it load-bearing rather than a nicety.
  - The set answer's enumeration works at index purity ≳ 0.99 and degrades fast
    below it (0.750 at purity 0.951, 0.167 at 0.795). **So the grouping's quality
    bounds the answer**, which is a far more tractable target than re-keying the
    store — and it is measured rather than argued
  - **`note 057`: purity looks like the SUFFICIENT STATISTIC.** Two very different
    routes to it — starving the index of data, and making families share their
    attributes — land answer quality in the same neighbourhood at matched purity
    (0.417 and 0.333 at purity ~0.70). **A hypothesis, not a result**: one matched
    pair at n=12, unseparated from noise
  - **Overlap does not break the index; it makes purity expensive.** With full data
    ONE private attribute suffices (purity 0.997 sharing three of four). At ten
    streams, sharing three of four costs 0.28 purity and 0.50 exact
  - `143` is the first result for `concepts.py` and `048` is why `families.py`
    exists: every other instrument's entities are arbitrary, so nothing resembles
    anything. **Still untried:** what makes an index good, as opposed to what makes
    a grouping hard — which is now measured on two axes

## 3. The store

### 3a. Structure

**⇒ DECIDED: one superposed `d × d` matrix. 🔀 with an exact cache and a settling
read.**

- 🔀 **`SuperposedRead`** — summed outer products. Earns its place.
  - `119` beats a bounded exact cache **8×** once bindings exceed slots
  - `109` capacity ~**d²**: width 32 → 16 bindings, 64 → 96, 128 → 384, at 90%
    recovery. *measured in:* direct outer products, **no decay, no cap** — the
    model's own write path reduces it
- 🔀 **`ExactCache`**, 🔀 **`SettlingRead`** — kept per 14c. The cache was the
  project's first controlled corpus improvement.
- ❌ **Anything recovering per-item information AFTER the sum** — `r = M @ key` is
  a sum. Readout bias, competitive retrieval, orthogonal updates and pair-keys-for-
  recovery all failed for this one reason.
  - `69` and the whole g11 line. **Do not re-propose.**

### 3b. Lifetime

**⇒ OPEN, and the question that has been asked wrong twice.**

- ✅ **Per-sequence, rebuilt every sequence** — current default.
  - `62` confirmed empirically: with `learn=False`, predictions are byte-identical
    whether or not another sequence ran first
- ✅ **Use-based eviction — `note 083`, and it is what makes C4's *forever* meaningful.**
  Discard whatever has gone longest unused and a persistently-queried fact survives
  **1.000 with zero variance** after 4,000 facts through 150 slots; random eviction gets
  0.717. Recency and frequency are indistinguishable here (both are true of the same facts
  by construction). **Bounded in content, unbounded in TIME** — fixed storage cannot hold
  everything, which is arithmetic, so this is the reachable form of the constraint.
  - **Unexplained:** random is *worse* on persistent than abandoned (0.717 vs 0.783,
    3 seeds, ~1.5 sd). An inversion in a control arm, recorded not smoothed
  - **The cost:** a useful fact nobody asks about inside its window is gone before it can
    be promoted. Every fixture here is built not to pay it
- ✅ **The two-timescale loop RUNS — `note 092`, and it cannot adjudicate.** Contradiction,
  blame, promotion and eviction assembled: 30% of facts corrupted, recall back to **1.000**
  after six passes, with blame falling 115 → 20 so it converges rather than oscillating. It
  damages nothing when nothing is wrong.
  - **But repair moves the damage to whichever side it does not trust.** Corrupt the direct
    fact and repair takes 0.697 → 1.000; corrupt the DERIVATION and it takes 1.000 → 0.697.
    Identical corruption, relocated. **`note 068` predicted exactly this** — *"a wrong
    derived fact becomes a premise"* — before anything was built
  - **What is missing is REDUNDANCY:** a derivation against a read is a two-way disagreement
    with no majority; two independent derivations against a read is a three-way vote.
    Untried. Trusting the direct fact always is just "detect only", which trades one failure
    for the other
- ⬜ **An EXTERNAL persistent store — John, 2026-07-30, and it is sound.** Eviction becomes
  *archival* rather than deletion. **The key already exists:** `derived_keys` means
  `keys.pair(entity, relation)` is rebuilt from two token ids, so `(entity, relation) →
  value` is an ordinary key-value pair needing no translation. `ownership.Ring` is already
  consistent hashing, which **is** the DHT addressing layer.
  - **It cannot be in the traversal loop.** `docs/SCALE.md`: ten sequential hops at ~50 ms
    is ~500 ms against `d_max`'s 640 ms, ~20% headroom, and a DHT lookup is several hops of
    its own. **So it is a PREFETCH source, not a read path** — `lasting` becomes a cache
    over it rather than the bottom of the stack
  - **It cannot replace the vectors.** `note 070`/`077`/`078` need similarity over the whole
    space; a key-value store cannot answer *"which entity relates most like this one"*
  - **It moves the hard question rather than removing it:** 083's *"what will be used"*
    becomes *"what to prefetch"*. Better failure mode though — a wrong prefetch is a slow
    answer where a wrong eviction was a lost one
- 🔀 **`persistent_lasting`** — a consolidated slow store surviving sequences.
  **A real gain, switched off.**
  - `133` beats baseline by **0.074–0.083 bits at EVERY data point**, and its own
    control (consolidation without persistence) is *worse* than baseline
    everywhere, so the attribution is clean. *measured in:* Tiny Shakespeare,
    character level, 4k–125k chars, 3 seeds
  - `133` **and it does not move the data wall**: +0.0124 past 16k, under the 0.04
    seed spread, not monotone. Store norm **0.4 at every corpus size** → a
    fixed-size cache holding a moving window, not a map that grows
  - **`note 082` explains 133's window mechanically, and rehabilitates the mechanism.**
    On a stream at 10× capacity it takes recall of the asked-about facts from **0.020 to
    1.000**, and **recall tracks the correctness signal one-to-one** (0.9→0.915,
    0.7→0.705, 0.5→0.540) — so the whole thing reduces to that signal, which `note 080`
    measures at six sd, label-free. **Bounded, not unbounded:** the slow store saturates
    in turn (1.1× → 0.965, 4.2× → 0.419), so it buys `total ÷ useful` and not infinity.
    **And a fact never asked about inside its window is unrecoverable** — the cost the
    fixture is built not to pay
  - `131`,`132` the first two passes measured the INSTRUMENT: a store saturated at
    `lasting_cap` before the run started, then a write rate 100× too large
  - **Off by default** because turning it on invalidates the text comparison set.
    Since `115` says character-level bits is the wrong target, that set may not be
    worth protecting — a cheap decision currently made by inertia
- ⬜ **`carry_store`** — carry the raw fast store between sequences. In a mutation
  and two unit tests, and **no experiment.** Correctly so: every relational task
  here **redraws its facts per sequence on purpose** (`047`'s condition), so
  nothing should survive, and `62`'s guard says carrying would answer from the
  training set.
  - **`170`: there is no task in this repository on which persistence could pay.**
    Persistence is *unfalsified on the goal*, not refuted. The blocker is an
    instrument needing something genuinely stable across sequences and something
    genuinely not

### 3c. Capacity, and the wall that is not one

**⇒ SETTLED and repeatedly re-opened. Read this before proposing anything about
saturation.**

- ✅ **The 16k-character wall is a property of the OBJECTIVE, not the
  architecture.**
  - `115` **SATURATION IS CLOSED.** A character bigram table over 66 symbols is
    intrinsically low-rank — effective rank **~3 at every width** (32/64/128/256),
    so *"the store is not failing to use its width. There is nothing there to
    use."* 16,000 characters is how long it takes to estimate a bigram table
  - `115` eliminated the competitors **by name**: store capacity (`109`), readout
    capacity (`110`), persistent representation (`114`)
  - `110` at widths 64–128 store and readout both **exceed task demand**, so
    decision 63 is not a capacity limit
  - `113` width is NOT flat — our arms improve with it (5.730 at d=16 → 5.494 at
    d=128, R² 0.92). A width sweep tests a claim nobody makes
- ❌ **"The wall is a capacity limit"** — `133`'s relabel of its own null.
  Contradicts `110` and `115`.
- ❌ **"Concept partitioning is where the capacity comes from"** — `133`'s
  follow-on, superseded by `134` one entry later: pooled capacity is **identical**
  between arrangements (128/256/512/1024/2048 at 1/2/4/8/16 nodes).
- **`170` is the entry about this pattern:** 115's closure lived in one place and
  nothing pointed at it. **A ratchet on proposals does not catch a re-label after
  the fact.** This tree is the fix.
- **Real capacity crossover, still live:** `110` the linear readout holds 2.00
  items/dimension at every width where the store scales as d². They cross near
  **width ~100**, above which the readout binds. → `hidden`.

## 4. Selection & membership — the gate

**⇒ DECIDED: `inherit`. The project's cleanest mechanism and nothing in it is
fitted.**

- ✅ **`inherit` / occupancy sketch** — answer from your own address if **anything**
  was written there, else from your neighbours'.
  - `148` 0.8100 DIRECT / 0.4350 TRANSFER / 0.8183 EXCEPTION — the first arm good
    at all three. The gate is **exact: 1.0000 of TRANSFER, 0.0000 of DIRECT and
    EXCEPTION, every seed**. *measured in:* families with exceptions, 3 seeds
  - **Why it works:** membership is *"is there anything here"*, not *"who has
    more"*, and with a hashed sketch an unwritten address reads **exactly 0.0** —
    so the threshold is structurally zero and nothing is tuned
  - `149` not a fitted constant: ordering holds in every cell across `n_values`
    4/8/16 and `family_size` 3/4/6
  - `150` MQAR: matches plain **seed for seed** (0.9950) and never defers; summing
    the same extra reads costs 0.113. Also rules out sketch false negatives
  - `153` **where it pays:** occupancy is informative exactly where an address is
    READ BEFORE IT IS WRITTEN within the sequence. Families qualifies; chains,
    kinship and MQAR write every address before querying it
  - `161` it was never read-gated and nobody had counted its reads
- ❌ **Select by norm** — magnitude at an address says nothing about whether it is
  the right address. Collapsed to 0.247 on exceptions where plain addressing holds
  0.783. `147`
- ❌ **Select by decode margin** — confidence in *an* answer is not evidence about
  *which retrieval* produced it. 0.581, below the summed baseline's 0.688. `147`
- ❌ **Sum the two retrievals** — averaging, so it cannot choose. `146`
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

- ✅ **`hop_relation`** — bind a relation token into the hop's key, so a hop follows
  a NAMED edge. `158`
- ✅ **`hop_relations`** — one relation PER HOP, so a walk follows LINK-then-FACT.
  - `164` LINK→FACT reaches the linked family's value; LINK→LINK stops at its
    representative; `hop_relation=LINK` (the pre-164 mechanism at its best setting)
    also stops there. Stable across 3 seeds
  - **Labelled an instrument, not the answer.** A schedule the task does not supply
    is a fitted constant (`162`)
- ⬜ **Try-all-and-gate** — follow every relation type, keep the one whose address is not
  empty: `r` reads, no new mechanism, and **the gate doing selection**, which is the one
  selection rule here that has ever worked. `163 §2` John: *"potentially the actual end
  solution."*
  - **Its viability is a property of RELATION DENSITY and the dense case is refuted.** The
    gate selects only where exactly one candidate address is occupied, and `search.py`
    records the split: `(subject, relation)` names one person **94.9%** of the time while
    `(FACT, subject)` *"names one of several relations about half the time"* — so on ten
    relations it is undecided about half the time, which is why `search.py` exists (`108`)
  - **Where it works is where it is unnecessary** (few sparse relations, i.e. `families.py`,
    where `hop_relations` suffices) and where it is needed it is refuted. **Revival:** a task
    with several individually-sparse relations, which nothing here has
  - **And `note 090` took a different route entirely** — supplying the DISPLACEMENT rather
    than choosing the relation — so this and the learned chooser below are alternatives to
    a problem now solved another way. Kept as ⬜ rather than ❌ because neither was measured
- ⬜ **Learned relation chooser** — `147`: two hand-made selection rules were refuted before
  membership worked, and a learned chooser is strictly harder. See 090's route above.
- 🔀 **`search.py` beam search** — built and tested; `run` still does not call it, but
  `note 091` drives it end to end from `tools/`, so it is no longer only scaffolding.
  - **The 🔀 argument in one option:** refused at `111` (the verifier is built from the
    same noisy retrievals it must adjudicate), **revived at `121` when width was measured
    NOT to fix fidelity**, built at `123`, closed at `130` (+0.020) with `125`'s +0.269
    traversal win. A refutation that expired
  - **`note 061`: it is what CLUTRR needs**, verified against the code — the task names
    BOTH endpoints, so `108`'s missing disambiguator is handed over, and `depth` is
    observable rather than fitted
  - **`note 064`'s durable fact, kept though 065 superseded its conclusion:** the entity
    hop is **0.9889 and FLAT** (the store does not degrade as it fills) while the relation
    decode is **0.9348** — six times the error rate. `walk_from` branched only at the ROOT,
    hedging at the 0.974 step and committing blindly at the 0.906 ones, so its +0.009 was
    **measured at the wrong place by its own construction**
- ✅ **`search.beam` — branch at EVERY step, pruned.** Beats single-step branching on
  every seed of both harnesses, which is the qualitative claim and it holds.
  - **`note 075`: `note 065`'s +0.2190 does NOT reproduce.** `beam` lands within 0.007 of
    065's mean; `search` is high by 0.12, so the gain is **+0.107**. Not width, not the
    `allowed` mask, not `branches` — all tested. 065's config is unrecovered, so take
    differences against `tools/clutrr_recovery.py`'s own baseline
  - **713/713 on the plain subset is reached — under PARTITIONING**, `note 081`'s
    companion measurement: 4 concept nodes give beam 0.9220 against 0.8877 monolithic,
    because a node carries interference only from what it owns
  - **Cost 4× the reads** (`width × branches × depth`); unpruned is `branches^h`, a
    million walks at ten hops, so pruning is what makes it exist. **G4 unanswered** —
    `123` had beam 4 at 3.2× on kinship. `search` is untouched as the comparison (14c)
  - **⇒ It corrects `note 063`**, which put the ceiling on route-finding rather than
    naming: right at 0.659, wrong once the route is solved, so the fold is the next work
- ✅ **A hop REPLACES a retrieval, it does not combine with it** — `101`. `102` built
  the accumulator and recorded that the stated reason for choosing it was wrong.
- ❌ **Another mechanism stacked on noisy retrieval** — four tried, all failed
  against the same 0.915/0.35 ceiling. `102`, `105`, `107`, `111`. **Do not
  re-propose:** the fix is per-step fidelity, not another layer.
  - `105` hops and pair keys **do not compose, and the combination produced numbers
    anyway** — the failure mode this repo's standards are built against
  - `106` composition degrades under repeated entities *gracefully*, and the 1.000
    that preceded it was the degenerate case
  - `107` the traversal mechanism is not worth building; the blocker is per-step
    fidelity. **Condition expired at `121`/`122`**
- 🔀 **`hop_accumulate`: `concat` vs `bind`** — concat wins 1.000 to 0.812, **but only
  because 16 rules in a 128-wide space are linearly separable whatever the labels do**, which
  is a property of having few rules. `bind` is kept for that reason. *measured in:* 16 rules,
  10 relations, 128-wide
  - **`note 063`: SCALE.md's trigger — "a rule table in the hundreds" — is MET.** CLUTRR has
    1,393 distinct chains, and **99.8% of test chains are unseen** while only 6.6% of adjacent
    PAIRS are. A readout over a whole chain must generalise to what it never saw; a **fold
    over pairwise rules** only asks what it was trained on, median 144 times each
  - **`note 066` corrects 063 both ways.** Intermediates are NOT unlabelled — a 2-hop answer
    IS a labelled pairwise rule (4,076 of them, 62 unambiguous), and 3-hop puzzles label
    `(derived, base)`, so the task supplies its own curriculum. But 063's "6.6% unseen"
    counted *stated* pairs where the fold needs `(accumulated, next)` with the accumulated
    side **derived**: **120 asked for, 97 derivable**, converged in two rounds
  - **The fold is right 98.8% where it can act** (596/603) against 0.42% irreducible
    ambiguity, and **completes only 52.6%** — tabulation's ceiling, not the fold's error.
    **The bottleneck moved twice:** 063 route-finding → 065 route solved, naming → 066 the
    rules available to name with. Unexplained: the **3-hop cell (0.524) is below 4-hop
    (0.732)**
- ✅ **GENERATION DELTA, learned from cycles — `note 090`, and it CLOSES the ceiling.**
  A chain plus its query is a loop, so the chain's deltas must sum to the answer's: one
  equation per puzzle, 9,074 of them, 20 unknowns, null space **1** (the gauge), and
  **20/20 deltas recovered exactly**. Fill a gap with any relation of the right delta and
  the chain stays arithmetically correct.
  - **End task 0.5201 → 0.9668** symbolically, against 1.0000 for an oracle handed the
    true rules. **CONTROL: a deliberately WRONG delta scores 0.5681, below random's
    0.6081** — so the displacement is the mechanism, not the filling. Fills also FALL,
    720 against random's 1,152, because a delta-preserving fill lands where the table
    already knows
  - **`note 091` end to end, the model recovering its own chains: 0.8578**, with chain
    recovery 0.8770. Roughly the product, slightly better because a mis-recovered chain
    can still compose right. `tools/generation_delta.py` reproduces both
  - **`note 087`: the fold is PERFECT given coverage** — supply every missing rule and
    puzzles complete 1.0000, so 066's *"tabulation's ceiling"* understates it. The gap was
    **31 rules**, all spouse/in-law, **never stated in any split**
  - **`note 089`'s hand-coded features were mostly NOISE.** Its oracle scored 0.7382; the
    marry clause cost 0.125 and gender+affinity a further 0.058. **The feature it measured
    as least learnable (generation, 0.350 from profiles) is the only one that mattered** —
    profiles are ADJACENCY and generation is GLOBAL, so the answer was a different kind of
    signal, not a better regressor
  - **Refutation, and it is the live question: kinship has an ADDITIVE INVARIANT.** Whether
    an arbitrary relational domain has a conserved quantity is untested, and a domain
    without one gets nothing from this
- ❌ **Naming the missing rule, by any learned readout — `note 088`.** Extensional
  relations reach 0.223 held-out (`note 070`, +0.099 paired, t=11.6) and score **0.5995 end
  task, BELOW random filling's 0.6081 ± 0.0055.** 070's holdout was a random quarter; the
  rules that matter are an adversarially withheld family, and this is the measurement that
  separates them. `majority` is worse still (0.5620), so systematic error costs more than
  noise. **Revival: only if a mechanism beats 0.6081 end-task, which is the bar 090 clears.**
  - `note 067` `bind` over RANDOM relations: 0.056 held out against chance 0.050. Kept
    under 14c as the measured comparison
  - `note 084` self-training does not lift it, frozen from round 1: **bootstrapping needs
    new FEATURES, not new labels** — 078's rounds added graph columns, this adds only
    pseudo-labels over the same space
  - `note 085` **associativity VERIFIES what it cannot generate.** Holds on the known table
    (0.933), determines held-out rules at 0.059 (chance — 15% density), and as a filter
    separates **0.5645 from 0.0162** with 98.4% of rejections genuinely wrong. Propagating
    it iteratively fills **zero cells in zero rounds** (`note 090`), so deduction is settled
    as unable to supply the rules
  - `note 071` structured vectors in the ADDRESS need the gate: raw reads return another of
    that entity's facts 0.592–0.775, `AddressSketch` recovers 1.0000/0.0005 at 24 bits
- ⬜ **`index_at_hops` combined with the position-level index** — `159`/`160`/`161`
  built the pieces; `154` measured that the guard's premise is false (a hop key
  sits at cosine **0.96** to a single token's row, so it *does* name a concept).
  Blocked on an instrument, not a mechanism: **no task has both** an
  address-never-written and a composition
  ([note 050](docs/notes/050-the-missing-instrument-composition-over-things-never-stated.md)).

## 6. The answer — what a response IS

**⇒ OPEN, and this is the live question. It is where the project's stated goal
lives, and until 2026-07-29 nothing here had ever scored a multi-token answer.**

- ✅ **Set of tokens, scored by `exact` and F1** — the measurement convention.
  - `165` `openplexus/answers.py`. **Recall alone is never reported:** emitting the
    whole alphabet scores recall **1.000** and F1 0.400. That trap fired within one
    commit — removing the gate in `167` *raised* recall while precision fell
  - `165` degenerates exactly: on singletons `exact` IS the old accuracy, and
    `single_token_accuracy` recovers it and raises on anything else
- ✅ **Emit by gated collection over index-proposed neighbours** — `167`.
  **This is decision 146's refuted mechanism, unchanged.** 146 found it can only
  average rather than select and 147 refuted the ways to choose — and **neither
  objection applies to a set answer, because nothing has to be selected.** The
  refutation was about the question.
- 🔀 **Bound the enumeration by the biggest similarity gap** — an argmax over gaps, not a
  threshold, the same move `148` made replacing a tuned bar with a structurally-zero read.
  - Matches the best fixed `branches` at family sizes 3–6 **without being told the size**,
    where no single fixed value works across all of them. `look` is a **ceiling** not a
    target: flat 6→16, but 0.500 at look=4 for a family of 6, so it must exceed the group.
    *measured in:* families, index purity **1.000**, cliff ~0.45 wide against within-family
    steps of ~0.01
  - **`note 058`: real word co-occurrence has NO cliff, and the shape is the finding.**
    Largest gap **0.059** against the task's **0.424**, after four confounds were closed
    (weighting off, content-word slice, centring confirmed, shuffled control at 0.002).
    **At no setting is the profile bimodal** — language decays in steps of 0.02–0.03 where
    the task falls 0.45 at once. **A cliff rule needs a cliff and language provides a
    slope**, so the crossover needs purity ≳0.99 *and* bimodality, and one real dataset
    supplies neither
- 🔀 **Fixed `branches`** — the count supplied. `167`: the peak sits at
  `family_size − 1` in every row and collapses either side (1.000 → 0.500 → 0.083).
  - **`note 056`: this is a measured CROSSOVER, not a loser.** Degrading the
    grouping, the gap rule falls **faster**: at purity 0.795 it scores 0.167 against
    fixed's 0.417, at 0.951 it is 0.750 against 1.000, and they draw level only at
    purity ≳ 0.99
  - **Why:** given the count, a noisy ranking can only hand you wrong *candidates*.
    Deriving the count, it hands you wrong candidates **and** a wrong count — two
    error sources against one. The tell is over-emission: size 2.58 against a true
    2.00, precision 0.708
  - Decision 74's shape again, which is what 🔀 is for: **which one is right is a
    property of the grouping's quality, not of either mechanism**

> **So F3's remaining gap is sharper than "the size is supplied":** the enumeration
> bound is **either supplied, or it needs a near-oracle grouping.** Neither is
> answering from awareness, which is why this row is PARTIAL and not PASSING.
- ⬜ **Autoregressive output** — *not* ruled out by GOALS §2, which forbids
  next-token prediction as the TRAINING OBJECTIVE, a different thing from
  autoregression as an output MECHANISM. What argues against it is **termination**:
  it needs a learned end-token, where a gated walk stops where `148` reads
  structurally zero.
- ❌ **Structured slots** — not a peer of the others. A fixed frame is a traversal
  with a fixed relation schedule, which `162` already calls a fitted constant.
- ⬜ **Declining to answer** — the archived ledger's row C4. **Nothing anywhere lets
  the model say "I do not know", and no task scores abstention**, while the gate is
  a fact about the store rather than a learned probability. An untested claim about
  honesty.

### 6b. Knowing when to stop hopping

**⇒ DECIDED: a learned halting gate. It works and it is not confidence.**

- ✅ **`halt_gate`, learned** — reads the retrieval and decides whether to hop again.
  - `086` a halting signal exists **and it is not confidence** — what separates is
    the CONTENT
  - `087` the gate learns which hop to read, and mixed depths reach **1.000**. Two
    defects found on the way, each of which looked like a working mechanism, and
    **the mutation harness caught what the tests did not**
  - `088` three depths at once, and the gain has an upper edge
  - `092` **it generalises to a depth it never trained on, zero-shot** (0.992)
  - `089` it is a token detector, measured: `halt_w` sits **+8.3 sd** on one token's
    value vector, and the sign was the opposite of what was predicted
- ❌ **Transferring the gate to new terminator tokens** — impossible *by
  construction*: two markers have unrelated value vectors. `089`. **Do not
  re-propose.**
- ❌ **A token-agnostic terminal signal** — `093` there is none, and that is what
  points at frozen `Wv`. `094` `value_lr` does not build a terminator class, and
  making separators targets breaks the gate.
- ❌ **Occupancy as a free halting signal** — `153`: half the gate can go where the
  index cannot, and it has **nothing to say** there. Chain start/middle/end at
  0.893/0.791/0.898 is not a signal.

## 7. Output → surface

**⇒ OPEN and off the critical path. Blast radius near zero, which is why it does
not gate anything above.**

- ✅ **Template realiser** — `openplexus/render.py`, deterministic, dependency-free,
  **structurally incapable of adding a fact.** John's ruling: templates first.
  - *no measurement* — a floor, not a mechanism with a number. What it contributes
    is a **bar**: `content_words(render(...)) - FRAME` must EQUAL the answer set, so
    dropping a value fails as well as inventing one, and `FRAME` is a fixed 25-word
    list a reader can check in full
  - **Written where it is trivially true, so the rungs above have something to fail
    against** rather than being graded on how well they read
  - Empty set **declines** rather than rendering a hole — the surface for row C4 if
    anything earns it
- ✅ **Retrieval realiser** — `render.speak`. **The words come from the CONCEPT MAP,
  not the caller**, so the model supplies its own vocabulary and `render` arranges
  it. No new model, no next-token prediction.
  - *no measurement*, same reason. `Shared.surfaces` already stated the design
    problem — *"which surface to use is a choice the concept itself does not
    contain"* — and this is where it gets made rather than dodged
  - **The default policy is arbitrary and says so** (lowest token id: deterministic,
    agrees across nodes); most frequent wins when counts are given, with a
    connection test asserting counts MOVE the choice
  - **Neither is the eventual answer:** with surfaces in several modalities the
    choice belongs to the QUERY, and nothing is multimodal yet
  - `spoken_faithfully` is the same bar one level down — whether a CONCEPT was
    invented rather than a word — **checked in both directions**, because a realiser
    that dropped a concept passes any invents-nothing test trivially
- ⬜ **Small learned renderer trained on our own concept sets** — with a
  **faithfulness** test rather than an accuracy one: perturb the set and the text
  must move; hold the set and the text must contain nothing the set does not.
- ❌ **Off-the-shelf LLM as renderer** — cheapest demo, worst for the claim. A
  fluent renderer **can produce the right sentence from a wrong walk**, so the
  end-to-end number would measure its world knowledge. Rule 2 exactly.
  - *no measurement* — refused on the same grounds rule 2 is written on, that a
    green end-to-end run cannot say which component worked. **Revival condition:** a
    faithfulness test showing it cannot add or drop a fact
- ✅ **No renderer, for programmatic use** — for a query API or an agent tool, a set
  of typed bindings is *better* than a sentence, so rendering is optional for a
  whole class of uses rather than merely deferrable.
  - *no measurement* — a scope position, not a result. It is here because it is
    what makes component 7 non-blocking for everything above it

## 8. What learns

**⇒ OPEN. The narrowest description of the whole architectural problem.**

- ✅ **`Wo` only, delta rule at scored positions** — the exact gradient for a single
  linear readout, so it is not an approximation of backprop; there is nothing to
  backpropagate through.
  - `042 §4` **the rule is not the limitation; the absence of anything for it to
    write to is.** `Wk` and `Wv` are frozen random, the store is rebuilt per
    sequence, so **everything durable is one linear map**
- ❌ **`value_lr` / `value_centre` to unfreeze the values** — the values move a long
  way, stay spread, and the plateau does not budge. `114` it works and it does not
  help; `94` it does not build a terminator class. **Do not re-propose as a fix for
  collapse.**
- 🔀 **`hidden` readout** — the largest single factor on text (`83`), and the answer
  when the readout/store crossover binds above width ~100 (`110`).
- ⬜ **Self-modifying structure** — nothing to modify: the store is `d × d`, fixed
  at construction. `042` is right that 3b and 10 are prerequisites, not
  alternatives. Reserve the seam, build when a task can tell whether it helped.

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

- ✅ **Partition by dimension** — every node computes `M_slice @ key_slice` and
  inherits the sum. Current default.
  - `g4-01` a lone node's answer holds at 16 dims (0.949) and degrades fast below:
    8 → 0.681, 4 → 0.412. **So node count ≈ width ÷ 16**, not anything softer
- 🔀 **Partition by concept** — `concept_nodes`, `partitioned.py`, `ConceptStore`.
  Built, off by default, and **refuses to combine with `consolidation` or
  `carry_store`** — both refusals labelled temporary in the source.
  - `134` **pooled capacity is IDENTICAL** to dimension splitting at every node
    count. **Lone-node capacity is not:** 2048 against 128 at 16 nodes, a factor of
    sixteen. *measured in:* 5 seeds, 50 cells, per-node memory held equal at ~4,096
    numbers
  - **A SECOND thing it buys, which `134` did not have: PARALLEL SEARCH.** John asked
    2026-07-30 whether the beam could be distributed. It is **serial in depth and
    parallel in width** — step *k+1* needs step *k*'s landing entity, but the 16
    expansions within a step are independent. Under CONCEPT splitting each read goes to
    one owner, so `ownership.Ring` (*"which node owns a concept, without a directory or
    a coordinator"*) makes it **16 point-to-point messages to named neighbours** — which
    amended C1 explicitly permits. **So the beam's 4× cost becomes 4× the NODES and 1×
    the TIME.** Under dimension splitting every read needs every node and it really is
    4× the traffic. *no measurement* — reasoned from the mechanism's own structure, and
    it is a stronger argument for this option than 134's independence case
  - **`note 081` MAKES IT MANDATORY, and this is the strongest argument for it.**
    Capacity is fixed at `~0.023·d²` per store, and both knobs fail: no decay saturates
    (recall **0.07 at 10.6×**, and *symmetric* — oldest beats recent, so it is
    interference, not forgetting, and **replay cannot fix it**), while decay windows
    (0.990 on the last 100, **0.000 on anything older**). **C4 needs capacity that GROWS
    and this is the only mechanism that provides it** — `nodes × per-node`, since each
    node holds a FULL-WIDTH store. Distribution was framed as how to use spare machines;
    it is how the project satisfies its own fourth constraint
  - **And `note 081` costs the GATE at load.** `148`'s structurally-zero read is 1.26 at
    half capacity and **1.03 at 10.6×** — gone. `note 080`'s contradiction signal is the
    same quantity, so **the credit loop closes only inside the window.** Gate health is a
    function of LIVE load, not total writes
  - Its case used to be independence and churn resilience; **081 supersedes that**. Under
    dimension splitting a node can never answer alone however large the system gets
  - **BLOCKER, `note 072`: under the `kinship` layout it would cap at TWENTY nodes.**
    Ownership is `previous_concept = tokens[t-1]`, and kinship puts the relation there, so
    **100.0% of CLUTRR's 7,132 traversal bindings are owned by a relation** (`sister`
    alone 20.2%) against 0.0% under `closure`. Both options were chosen alone and the
    *pair* is the defect — `157` picked kinship for a 4.7× collision reduction without
    ownership in view. **Worse than the 16-node dimension ceiling it exists to fix**
  - **PARTLY fixed by `note 073`, BUILT as `PairKeys(route="first-concept")`**, default
    unchanged. The traversal binding moves relation-owned → **entity**-owned, markers stop
    owning content (31.6% → **0.0%**), busiest drops 26.6% → **11.8%**. **073's "0.0%
    relation-owned" is CORRECTED** — it scored 2 of 4 keys per block; `pair(relation,
    entity)` is still relation-owned at 22.3% of all keys, though its value is a separator
    the traversal never reads. `concept_nodes` still 0
  - **And `docs/SCALE.md`: bandwidth scales with WIDTH**, so dimension splitting must grow
    `d` to buy capacity (832 KB per message at Wikidata scale, ~266 MB per query) where
    concept splitting holds `d` at 512 and adds nodes (~640 KB per query). *arithmetic on
    measured capacity, no G4 run at these widths*
- ✅ **`openplexus/peer.py` — point-to-point reads and writes, no driver.**
  `notes 093`–`099`. Every read goes to the peer owning the concept; every write reaches
  every holder. **2 messages per read against 2N for broadcast** — 256x at 256 peers — and
  the serialisation point goes with it.
  - **A driver-free `beam` traversal is exact** (`note 094`): identical walk to one
    process, and a misrouted control changes it, so the routing produces the answer.
    `search` takes `reader=` so a caller injects routing and `search` never imports a
    transport
  - **Consistent hashing** (`note 095`): a peer joining moves **1.4%** of concepts at 64
    peers where `concept % peers` moved 98.4%, landing on the ideal `1/n` to a tenth of a
    point
  - **A departure costs a round trip, not the answer** (`note 097`): reads walk
    `Ring.holders`, and writes fan out so there is something to fall back to. **Both halves
    are needed and either alone looks fine.** Losing every holder returns zeros and
    **counts** them, because an uncounted zero decodes to whatever the readout prefers
  - **Fingerprinted** (`note 096`/`099`): peer count, ring seed, key seed/spread/width/
    start/route/markers, and the **wire-format version**, pinned to every struct on the
    wire by a test. **It caught `PROTOCOL` 3 the day after it was written**
  - ❌ **One read per round trip** (`note 100`/`101`): a walk's 77 reads at depth 10 cost
    `77 × RTT` = 3,850 ms. `read_many` batches a hop's independent reads into one round,
    giving 1,000 ms — **necessary and not sufficient, since `d_max` is 640 ms**
  - 🔀 **A MIGRATING walk** is where the remaining 2× is (`note 101`): `owner` routes a
    hop's look-up and the next hop's follow to the **same concept**, so 12 of 19 rounds
    ask a peer the round before already used. One peer visit per hop is ~`depth × RTT/2`.
    **Blocked on PRUNING**, which ranks all `width` walks together, so the caller is a
    rendezvous every hop — and that is the second round trip. `distributed.py`'s deadline
    is the shape of the fix: settle a step on what arrived, so a slow node costs a
    candidate rather than stalling. `width`-way is bounded, so not C1's collective
  - **Costs, stated:** the retrieval strategy moves to the owning node, because a remote
    store cannot return a `d×d` matrix (512 KB against 2 KB). A write waits for `R` holders,
    which is not `N` but is not free. Batching trades round trips for bytes, right only
    while latency dominates. **Untried:** ordering (writes race, store is additive),
    re-replication after departure, negotiation rather than refusal, real latency
- ❌ **The global dimension-summing readout** — this is the globally synchronised
  step **C1 forbids**, the project's own first constraint. Surfaced in a footnote
  to [note 009](docs/notes/009-splitting-the-memory.md) §4 **after four gates were
  passed and five sweeps run on top of it.**
  - `combine="vote"` mitigates the BANDWIDTH (4 bytes per node, ~8 KB at 1024
    nodes) and not the violation. A concept-partitioned read is a **selection**,
    which is what removes it
- ✅ **Transport: vote-based, with suspicion and a deadline** — sound and ahead of
  the rest.
  - `128` `d_max` ~**640 ms** = 3× a measured p99. *measured in:* 4 nodes, width
    16, Docker bridge with `tc netem`, delay to 80 ms + 20 ms jitter + 2% loss.
    **A floor, not a constant** — a real WAN raises it
  - `126`,`127` the detector ejected nodes permanently; SWIM says suspect and
    retry. Fixed
  - `169` the deadline's actual branch — settle short on what arrived — had **no
    test until a silent peer existed**. `steps_settled_short` was asserted in one
    place, to be empty
- ⬜ **Untrusted nodes** — no threat model at all. A node that lies about occupancy
  or writes to addresses it does not own. **Forks on the project's endgame:**
  open-source-and-runs-everywhere implies it; a controlled network does not.
- ⬜ **Slice negotiation** — static by John's explicit choice; a node that
  negotiates its own slice is a coordination protocol and nothing needs one yet.

## 10. The objective and the instruments

**⇒ DECIDED: relational, not next-token. The instruments are all self-designed and
that is the standing weakness.**

- ✅ **Relational objective** — GOALS §1. Next-token prediction is an explicit
  non-goal in §2.
  - `047` **the objective was the ceiling, not the memory.** The only relation the
    store can express on a next-token objective is *"what followed this"*, which is
    an n-gram, and a counting table does that exactly and cheaply
  - `142` the store carries MQAR **completely** (0.995 vs 0.000) and the prior that
    wins on text costs 0.279 there
  - `136`,`139` at word level the store contributes nothing (9.185 vs 9.187), and
    its contribution is exactly substitutable by a learned prior
- ❌ **Bits per token as evidence about the store** — the objective is n-gram
  bounded, so it cannot show what the store adds. `142`, `047`. **Do not
  re-propose.**
- ❌ **Training on every position** — costs composition **1.000 → 0.40**. `095`–`098`
  is the whole line: `095` the gate is not outvoted, it is CONFLICTED, which is a
  mechanism problem; `096` letting the gate see WHERE it is triples all-position
  accuracy **and is still not enough**; `097` density raises the level and does not
  remove the decay; `098` giving the gate its OWN objective is what removes it.
  **Do not re-propose all-position training without a separate gate objective.**
- ❌ **Perpetual learning as a repair for churn** — `091`: it does not heal churn,
  **because churn costs capacity** rather than knowledge. Treat its +0.008 as a
  direction, not a number.
  - **C4 IS NOW TESTED — `note 081`/`082`, and `091`/`092` failed only because their
    tasks never saturated.** At 10.6× capacity a single store gives recall **0.07**, and
    *symmetrically* — oldest beats recent, so it is **interference, not forgetting, and
    replay cannot fix it.** Decay converts that into a window (0.990 on the last 100,
    **0.000 older**). **The answer is two multipliers:** consolidation for selectivity
    (`total ÷ useful`) and partitioning for capacity (`node count`). Neither suffices;
    forever exceeds any fixed multiple, so **what to shed is still open**
- ❌ **Concept addressing as a fix for text prediction** — 0.540 bits at bias 0, and
  a grouping built from SHUFFLED text does as well. **The address count did the
  work, not the concepts.** `141`
- ✅ **`families.py`** — the only instrument where things RESEMBLE each other, so the
  only one where a concept can mean something. `143` is its first result; `166`
  gave it a set-valued question.
- ✅ **`closure.py`** — unmarked stream of stated and entailed facts, no question
  marker, so the stated/entailed split IS the recall/reasoning split.
  - `g14-01` passes G0: entailed headroom **0.277** against a frozen 0.000.
    `095` measured the marker as most of the remaining gap, which is what this
    removes
- 🔀 **`kinship.py`** the mechanism testbed · **`mqar.py`** the store's control, the
  only instrument isolating the store from a prior (`142`) · **`chains.py`** solved
  at 1.000, out-degree 1 by construction, a control.
- ❌ **A composition sweep on chains as evidence about composition** — a chain is
  out-degree 1 by construction. `108`. **Do not re-propose.**
- ⏸ **`corpus.py`** — PAUSED, not condemned. Closed by 115/118, reopened by g17-01,
  and 135–142 measured on it without anyone re-deciding it was the instrument.
- ❌ **`reward_recall.py`** — retired, `126`.
- ✅ **CLUTRR-symbolic — RUN, and it is the first external instrument.** Graph layer,
  never the prose, so results are *"CLUTRR-symbolic"* and published text numbers are not
  comparable. `gen_train23_test2to10`, layout **`kinship`** (collisions 35.9% → 7.7%,
  `157`'s mechanism on someone else's data). Reproduce with
  `tools/clutrr_recovery.py`.
  - **Report per hop bucket and split on ENTITY REPETITION** — `note 059`: test is 37.8%
    repeated where train is 0%, so a falling curve reads as depth and is really `103`'s
    addressing. `note 060`: the `hops=1` floor is **0.0856**, not chance, because
    sequence length leaks the hop count
  - **`note 075`: note 065's +0.219 does NOT reproduce.** `beam` lands within 0.007;
    `search` is high by 0.12, so the gain is **+0.107**. Not a width effect and not the
    `allowed` mask or `branches` — both tested. **065's config is still unrecovered**, so
    take differences against `clutrr_recovery.py`'s own baseline
  - **What it cannot test: concept acquisition.** `note 076` — entities carry 1–2 edges,
    so two surfaces of one concept share nothing by arithmetic
- ✅ **OpenEA `EN_DE_15K_V2` — the acquisition instrument, FETCHED with John's approval.**
  Two DBpedia graphs, 15,000 gold links, URIs **encoded** so string matching cannot cheat.
  Chosen on measurement: 74% shared relation vocabulary, and every entity has ≥4 edges
  where CLUTRR has 5.9%. `tools/fetch_openea.py` verifies size and sha256; GPL data,
  evaluation use.
  - **`note 077`** zero supervision, bag of (relation, direction): hits@1 **0.0389** at
    **583× chance**, and monotone in evidence — 0.0024 at one edge to 0.1502 at sixteen,
    which is why CLUTRR could not see it
  - **`note 078`** bootstrapping on mutual nearest neighbours reaches **0.3098, 8×**, not
    plateaued. **A confidence gate makes it WORSE** (0.2334 at ≥0.9, 0.0855 at ≥0.98) and
    does not buy precision, so **mutuality is the merge gate and magnitude is not**. Seed
    precision self-corrects 0.263 → 0.676 untuned
  - **Not the hard setting:** `D_W`/`D_Y` share **0%** of their relations, so round 0 has
    nothing to compare. A vocabulary-free seed is untried and is the case a real network
    faces
- ❌ **`4.540` bits/char, "unigram BEATEN"** — the project's headline text result for
  weeks, and **not a measurement of this model.** `117`: the named configuration scores
  5.665–5.742 against a prequential unigram of 4.776, **1.1 bits away**. `118`: the figure
  appears **only in HANDOFF.md** — no sweep, no entry — and traces to note 037's 4.525,
  which that note says is *"trained with ordinary backpropagation, offline"* on frozen
  features. **Wrong twice: not the model under its own rule, and the opposite of
  prequential.** Kept because the failure is reusable — **an inherited headline with no
  provenance outranks every measurement downstream of it**
- ❌ **Scoring without a temperature** — `117`'s first attempt read 5.920 against a
  uniform 5.954, i.e. *the model learning nothing*. The delta rule targets a
  one-hot, so raw scores sit in about [0, 1] and a softmax over that range is nearly
  uniform. A calibration artefact that looks exactly like a null result.
- ❌ **`9.323` as the word-level unigram** — `135` it was never that, and the
  temperature grid was too narrow at word level.
- ❌ **Everything measured by the g18 harness before the fix** — `138`
  **RETRACTION: it trained on the wrong target.** Survived four sweeps and 142
  cells because every arm was wrong identically: internally consistent, both rails
  passing, a monotone ordering with a tidy explanation. **What caught it was a
  figure the project had already measured.** Internal consistency is not evidence.
- ✅ **g17-01's premise survives its own correction** — `140` the pivot was not an
  artefact, which is the one thing in this section that held.
- ❌ **Note 050's linked-families task as first designed** — `155` refuted by its own
  rail on the first run, and the rail was a p90 calibration that flagged what chance
  produces. Worth keeping as the example of a fairness check paying immediately.

## 11. Verification apparatus

**⇒ DECIDED and deliberately permanent. Moved out of the tree** — every item documents
itself in its own docstring and CLAUDE.md rules 6, 10, 11 and 14 carry the policy, so it
was spending lines in a document whose criterion is being readable in one pass. Nothing in
it has ever been re-litigated, which is the only thing the tree prevents.

- ✅ **The apparatus** — mutation harness (sharded 6 ways in CI; `--verify` is the
  authority on the count), a dependency-free ruler in `tasks/`/`baselines.py`/`answers.py`
  (`note 007`), the rails (`check_workflows`, `check_rails`, `check_duplication`,
  `check_decisions`), and a sensitivity check on any timing assertion — `169`: three
  attempts at one assertion and **the first two both passed when written.**
  - Full account: [archived](docs/archive/verification-apparatus-2026-07-30.md)

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
