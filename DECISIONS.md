# Decisions — the option tree

**What this is.** Every architectural component, the options for it, and which
option each is. **One page, scannable, so a settled question stops being
re-opened.** That is the only job it has.

**Why it was rebuilt.** This was a 6,040-line append-only log. Nothing could read
it whole, so it was read selectively — and on 2026-07-29 that produced three wrong
recommendations in a row, each built on a claim a later entry had already
superseded. Decision 115 closed saturation and three later entries reopened it. A
log records; it does not prevent.

**The old log is not deleted.** It is
[docs/archive/decisions-log-083-171.md](docs/archive/decisions-log-083-171.md),
and entries 1–82 are
[docs/archive/decisions-001-082.md](docs/archive/decisions-001-082.md). Every
attempt below cites its entry number, so the reasoning is one lookup away. **The
tree is authoritative; the log is the footnotes.**

---

## How to read a row

    ✅ CHOSEN      decided, built, and this is what we use
    ❌ REFUTED     measured and it lost. The revival condition is stated
    ⬜ UNTRIED     no measurement. Not "probably fine"
    🔀 LIVE BOTH   two or more kept behind a switch, deliberately, and re-tested
                   as the system changes. A valid END state, not indecision

**Every attempt carries the configuration it was measured in.** A refutation is
conditional on a config, and decision 74 cost a comparison set by forgetting that:
sparse keys were measured worse, then a readout change reversed them cleanly. So
`measured in:` is part of the record, not decoration.

**🔀 exists because refutations expire.** 107 declined a traversal and 111 declined
search on correct arithmetic; both conditions were measured away later and both
mechanisms became right. A deleted alternative cannot be re-measured.

**CENSUS: 22 chosen, 28 refuted, 16 untried, 12 both, 1 paused.** Checked against the
body by `tools/check_decisions.py`, because a summary that can drift from what it
summarises is how `check_architecture.py` caught its own counts the first time a
verdict changed.

> **Coverage, stated exactly, because a tree that looks complete and is not is
> worse than the log.** Every ❌ in the archived log is here — the refutations and
> the retractions are what this document exists to hold, and re-proposing one is the
> failure it prevents. **Confirmations are NOT exhaustive:** an entry that measured a
> mechanism working and changed nothing else is cited where it supports an option's
> state and otherwise left in the log. That is deliberate. If you want the full
> chronology of what worked, the log is where it is, and every option here names its
> entry numbers.

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

**Open sub-question — codebook agreement across nodes.** Two nodes that quantise
the same input differently write to different addresses and the memory fragments
**with no node able to detect it locally.** 163 §1 named the MERGE direction (two
things → one id); note 053 adds SPLIT (one thing → two ids), which only exists
distributed. Recommended: quantise once at ingest, codebook versioned as part of
network identity. Falsifier to build first: two nodes given identical input must
emit identical ids, with the companion that different input must differ.

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
- ❌ **Content-derived keys, i.e. similar concepts on NEARBY addresses** — and this
  entry exists because reading the tree as a tree found it contradicting §1.
  - `042 §2` ranked it third by blast radius and framed it as *"the store has no
    notion of similarity at all — `dog` and `wolf` are as unrelated as `dog` and
    `7`"*. `g10-09` tried it and was **RETRACTED**: the cache was indexed by token
    id, so the question was never asked
  - **But §1 already refuses this mechanism under a different name.** "Address the
    store by continuous vector" is ❌ *because* similar things landing near each
    other raises `ρ`, and interference is `O(N·ρ)` — which also turns the gate's
    structurally-zero bar into a tuned threshold and stops `AddressSketch`
    colliding. **Nearby addresses is the thing being refused, whether the
    nearness comes from a raw vector or from learned content statistics**
  - **The resolution is note 045's and it is already the architecture:**
    similarity lives in a SEPARATE INDEX, and the store stays exactly addressed.
    `ContentIndex` proposes candidates by similarity; nothing addresses by it. So
    042 §2's complaint is answered rather than open — **the store does not need a
    notion of similarity, because the index has one**
  - **Revival condition:** a task where similar-things-must-share-an-ADDRESS beats
    exact separation plus a similarity index. Nothing has ever needed that
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
- 🔀 **`persistent_lasting`** — a consolidated slow store surviving sequences.
  **A real gain, switched off.**
  - `133` beats baseline by **0.074–0.083 bits at EVERY data point**, and its own
    control (consolidation without persistence) is *worse* than baseline
    everywhere, so the attribution is clean. *measured in:* Tiny Shakespeare,
    character level, 4k–125k chars, 3 seeds
  - `133` **and it does not move the data wall**: +0.0124 past 16k, under the 0.04
    seed spread, not monotone. Store norm **0.4 at every corpus size** → a
    fixed-size cache holding a moving window, not a map that grows
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

**⇒ DECIDED: typed hops with a per-hop schedule, as an INSTRUMENT. The final read
path is not chosen.**

- ✅ **`hop_relation`** — bind a relation token into the hop's key, so a hop follows
  a NAMED edge. `158`
- ✅ **`hop_relations`** — one relation PER HOP, so a walk follows LINK-then-FACT.
  - `164` LINK→FACT reaches the linked family's value; LINK→LINK stops at its
    representative; `hop_relation=LINK` (the pre-164 mechanism at its best setting)
    also stops there. Stable across 3 seeds
  - **Labelled an instrument, not the answer.** A schedule the task does not supply
    is a fitted constant (`162`)
- ⬜ **Try-all-and-gate** — follow every relation type, keep the one whose address
  is not empty. Costs `r` reads, needs no new mechanism, and is **the gate doing
  selection again** — the one selection rule here that has ever worked.
  - `163 §2` John: *"potentially the actual end solution."* **This is the intended
    final form.**
- ⬜ **Learned relation chooser** — `147` is the argument for not attempting it yet:
  two hand-made selection rules were refuted before membership worked, and a
  learned chooser is strictly harder.
- 🔀 **`search.py` beam search** — built, tested, and **deliberately not wired into
  `run`**, labelled as scaffolding so it does not become load-bearing.
  - `111` **refused first:** search does not pay, because the verifier is built from
    the same noisy retrievals it is meant to adjudicate
  - `121` width does NOT fix retrieval fidelity on the task, and `112` was never a
    bound on it — **which is what expired 111's condition**
  - `122` step 2 reproduces at 0.971 and the traversal ceiling is 1.000, so the
    build is justified. `123` built and proved standalone; beam 4 costs 3.2× the
    traffic
  - `129` ambiguity IS detectable before searching, **and the expensive signal is
    below chance** — the endpoint margin is not the fallback
  - `125` traversal is the win (+0.269); search helps only where ambiguity is
  - `130` the gate pays +0.020 over search-everywhere, and the search line closes
  - **This is the 🔀 argument in one option:** refused at 111, revived at 121 when
    its condition was measured away, and it is the reason the switch exists
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
- 🔀 **`hop_accumulate`: `concat` vs `bind`** — concat wins 1.000 to 0.812, **but
  only because 16 rules in a 128-wide space are linearly separable whatever the
  labels do.** That is a property of having few rules. `bind` is kept for exactly
  this reason. *measured in:* 16 composition rules, 10 relations, 128-wide
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
- 🔀 **Bound the enumeration by the biggest similarity gap** — an argmax over gaps,
  not a threshold, which is the same move `148` made when it replaced a tuned
  membership bar with a structurally-zero read.
  - matches the best fixed `branches` at family sizes 3/4/5/6 **without being told
    the size**, where no single fixed value works across sizes
  - `look` becomes a **ceiling** rather than a target: flat from 6 to 16, but 0.500
    at look=4 for a family of 6, so it must exceed the group
  - *measured in:* families, index purity **1.000**, cliff ~0.45 wide against
    within-family steps of ~0.01
  - **`note 058`: real word co-occurrence has NO such cliff.** Largest gap **0.015**
    against the synthetic task's **0.424** — 28× smaller — with every token's top
    eight neighbours inside 0.02 of each other at ~0.96. A shuffled-text control
    gives 0.002, so real text carries ~7× chance structure and ~1/28th of what the
    task supplies. **So the crossover has a second clause: this needs purity ≳ 0.99
    AND a bimodal profile, and one real dataset supplies neither**
  - **The confound that mattered was tested and the finding held.** `ContentIndex`
    has a `power` argument that down-weights common context and **defaults to off**,
    with a docstring saying it is *"the one that moved `king` to `richard`"* — so the
    first run measured real text with the mechanism for real text disabled. At
    `power` 0.75 the largest gap goes 0.015 → **0.025**: a 67% improvement and still
    **17× short**, with the profile shape unchanged. Centring is active and is not
    the cause
  - **A content-word slice buys another 2.4× and saturates**: rank 200-800,
    400-1000 and 1000-2000 all land at 0.057–0.059, so **~7× short** is where it
    settles and no confound accounts for it. **The shape is the real finding** — at
    no setting does the profile become bimodal. Real neighbourhoods decay smoothly
    in steps of 0.02–0.03; the task falls 0.45 in one. **A cliff rule needs a cliff
    and language provides a slope**
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

- ⬜ **Template realiser** — deterministic, ~50 lines, **structurally incapable of
  adding a fact.** Recommended first: if templates read sensibly, the concept set
  genuinely carries the answer.
- ⬜ **Retrieval realiser** — emit the surfaces each visited concept already
  carries. `concepts.Surfaces` exists precisely because one concept has many
  surfaces, so a concept can carry its own words. No new model, no next-token
  prediction, real words out.
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

**⇒ DECIDED: split by dimension today. Concept splitting is built as a seam and is
not on. The readout still violates C1.**

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
  - **So its case is INDEPENDENCE and churn resilience, not capacity.** Under
    dimension splitting a node can never answer alone however large the system
    gets
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
  direction, not a number. Also the precedent that matters for anything
  self-modifying: **C4 is still untested after two attempts, both times because the
  task was too easy to need it** (`091`, `092`).
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
- ⬜ **CLUTRR or any external benchmark** — **the standing gap. Until one runs, this
  project is grading its own homework.** It has been "next" for several cycles,
  which is itself the finding.
  - **`note 058` put a number on what that costs, and it is large.** The set
    answer's enumeration depends on a bimodal similarity profile; the synthetic task
    has one with a 0.424 gap and real word co-occurrence has 0.015. **So this stops
    being a completeness item and becomes the measurement that decides whether the
    answer line means anything.** Needs a data fetch, which is John's call

### 10b. Retracted numbers — never quote these

**⇒ SETTLED. Every one of these was internally consistent and wrong.**

- ❌ **`4.540` bits/char, "unigram BEATEN"** — carried as the project's headline text
  result for weeks. **It is not a measurement of this model.**
  - `117` the reproduction FAILED: the configuration the record names scores
    5.665–5.742 against a prequential unigram of 4.776. **1.1 bits away**
  - `118` the archaeology took ten minutes: `4.540` appears **only in HANDOFF.md** —
    no sweep, no experiment, no decision entry. Its source is note 037's **4.525**,
    which that note states plainly is *"trained with ordinary backpropagation,
    offline, deliberately"* on frozen features. **So it was wrong twice: not the
    model under its own rule, and not prequential — the opposite of prequential**
  - Kept because the failure is reusable: **an inherited headline with no
    provenance outranks every measurement downstream of it**, and nothing
    downstream can contradict it
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

**⇒ DECIDED and deliberately permanent. Listed so effort does not go here hunting
for stopgaps.**

- ✅ **Mutation harness** — 172 mutations, sharded 6 ways in CI. **Measured on
  `57d8112`: 168 mutations, 28 per shard, 18–35 minutes each, all caught** — so
  serial is ~2.5 hours, not the twenty minutes the comment claimed.
  - `168` shards are by POSITION, so inserting a mutation mid-list shifts
    everything after it. Two logs compare line-for-line only while the list is
    unchanged
- ✅ **Dependency-free ruler** — `tasks/`, `baselines.py`, `answers.py` take no
  dependencies, because they are what everything else is asserted against.
  - `note 007` the stack decision. *no measurement* — a convention, and the
    argument is that a generator with no library semantics is auditable line by
    line
- ✅ **The rails** — `check_workflows` (flags vs `--help`, one second, turns a spent
  matrix into an error), `check_rails`, `check_duplication`, `check_decisions`.
  - *no measurement* for the rails as a policy — they are conventions, and each
    encodes a specific failure that already cost a result rather than generic lint
  - `check_duplication`'s stated justification was **wrong and its own tool
    measured that**: run over the pre-port tree it finds none of the five
    hand-copied recovery refusals it was requested for, because those copies had
    already diverged. So it is PREVENTION, not detection, and the thing that
    catches a drifted copy is still a mutation
- ✅ **Sensitivity checks on any timing assertion** — `169`: three attempts at one
  assertion (a race, a vacuous bound, then a real check) and **the first two both
  passed when written.**

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
