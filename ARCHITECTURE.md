# Architecture — what each piece must do, and whether it does

**This is a ledger, not an essay.** Every row is a capability the architecture
needs in order to meet [GOALS](GOALS.md), a verdict, and the measurement behind
that verdict. A row with no measurement is **UNTESTED**, never "probably fine".

| document | what it holds |
|---|---|
| [GOALS.md](GOALS.md) | what the project is for, and what would refute it |
| **ARCHITECTURE.md** (this file) | what has to work, and whether it does |
| [STATE.md](STATE.md) | the one question being worked on right now |
| [DECISIONS.md](DECISIONS.md) | history — why each choice was made, never rewritten |

## The rules this file lives by

1. **A verdict needs a number and a decision reference.** No row reads PASSING on
   the strength of code existing, tests passing, or an argument.
2. **New requirements get added the moment they are discovered**, as FAILING or
   UNTESTED. Discovering that something is needed is progress and is recorded as
   such, not deferred until it works.
3. **Solving a piece is not the end of it.** Every row names what it **depends
   on**. When a row changes, **every row that depends on it is re-run**, and
   until it is re-run its verdict reads STALE. Changing one piece invalidating
   another is the normal case here, not the exception — decision 74 is the whole
   argument.
4. **A verdict may go backwards.** PASSING → FAILING is a legitimate update and
   is preferable to a footnote.
5. **Scope is part of the verdict.** "Works on the task built to ask about it" is
   not PASSING. Decisions 150 and 151 exist because that distinction was worth
   four sweeps.

**Verdicts:** `PASSING` measured and holding · `PARTIAL` works in a named scope
and provably not outside it · `FAILING` measured and does not work · `UNTESTED`
no measurement exists · `STALE` was passing, a dependency moved, not yet re-run ·
`CLAIMED` a verdict inherited from earlier work that this ledger has not itself
verified

---

## A. Memory — hold a thing and get it back

| # | must be able to | verdict | evidence |
|---|---|---|---|
| A1 | Store a binding and recall it exactly | **PASSING** | MQAR 0.995 with the store, 0.000 without. Nothing else in the model does this work |
| A2 | Rebind a key — replace, not accumulate | **CLAIMED** | `corrective_writes`, g10-11: 0.0x chance of the stale value after 512 rebindings of 8 cues. Not re-verified here |
| A3 | Keep distinct things distinct | **PASSING** | identity addressing; note 035 measured interference as `O(N·ρ)` in mean key cosine, which is why addresses are exact and similarity lives elsewhere |
| A4 | Survive more facts than it has dimensions | **PARTIAL** | capacity is the standing wall. 134: splitting by concept leaves pooled capacity identical, lone-node 16× at 16 nodes. **156: typing an address does not spend it** — interference is `O(N·ρ)` in WRITES, and typing adds none |

**Depends on:** nothing. A1–A4 are the floor everything else stands on.

## B. Concepts — know that two things are the same kind

| # | must be able to | verdict | evidence |
|---|---|---|---|
| B1 | Discover kinds from data, not be handed them | **PASSING** | g19-00: cluster purity 1.000 from co-occurrence alone |
| B2 | Answer about a thing from its kind | **PASSING** | 143: transfer 0.998 grouped against 0.087 ungrouped, and `permuted` fails at matched address count, so it is the similarity paying |
| B3 | Let a specific fact beat its kind's default | **PASSING** | 148: exception 0.818 vs plain addressing's 0.783, transfer 0.435 vs summing's 0.265. The first arm good at both |
| B4 | Support one concept with **many surfaces** | **UNTESTED** | `concepts.Surfaces` is a seam with two implementations, `OneConceptPerToken` and `Shared`. **Nothing exercises many-surfaces-one-concept**, and GOALS §1 makes multi-modal a goal rather than a later luxury |

**Depends on:** A1, A3. **B3 depends on:** C1.

## C. Knowing what it knows

| # | must be able to | verdict | evidence |
|---|---|---|---|
| C1 | Detect that it holds no fact at an address | **PASSING** | 148: `AddressSketch`, defers on 1.0000 of transfer and 0.0000 of direct/exception, every seed. Bar is structurally zero, nothing tuned |
| C2 | Have that detection mean *"I don't know this"* | **PARTIAL** | 151: occupancy is a property of the ADDRESS, not the knowledge. Informative exactly where an address is read before it is written (153). Blind on kinship, chains, MQAR |
| C4 | **Decline to answer** rather than assert something it does not hold | **UNTESTED** | the gate detects an empty address (C1) and then routes to the neighbours. **Nothing anywhere lets the model say "I do not know"**, and no task scores abstention. John raised this on 2026-07-29: the architecture may be structurally free of LLM overconfidence — the gate is a fact about the store, not a learned probability — but that is a claim with no measurement behind it |
| C3 | Cost nothing where there is nothing to detect | **PASSING** | 150: matches plain addressing seed for seed on MQAR (0.9950) and never defers, while summing the same reads costs 0.113 |

**Depends on:** A1, A3. **C2 is the row note 051 attacks.**

## D. Relations — say *what kind* of connection

| # | must be able to | verdict | evidence |
|---|---|---|---|
| D1 | Store a typed edge `(subject, relation) → object` | **PARTIAL** | `PairKeys` does exactly this and kinship uses it; decision 100 measured mis-keying at 0.020 vs 0.713. But nothing **chooses** the relation — it is whatever the layout supplies |
| D2 | Keep two edge types about one subject apart | **PASSING** | **157**: with pair keys every column returns to within 0.05 of its link-free value (0.8333 / 0.4383 / 0.8150), where untyped they collapsed to 0.13 / 0.03 / 0.12. 156 had already ruled out capacity, leaving addressing as the only candidate |
| D3 | Follow *a specific* relation when reading | **PARTIAL** | **158**: `hop_relation` builds `key(relation, concept)` and `tests/test_typed_hop.py` shows the same sequence at the same position returning THROUGH_IS_A or THROUGH_HAS_A by which relation the hop carries — impossible untyped, where both edges share one address. **The relation is fixed, not chosen**, and 157's LINKED column (0.1275 vs chance 0.125) is still unmoved because the task is not wired to it |

**Depends on:** A1, A3, A4 (typing multiplies distinct addresses, so D costs A4).

## E. Composition — reach what was never stated directly

| # | must be able to | verdict | evidence |
|---|---|---|---|
| E1 | Follow a chain of the same relation | **CLAIMED** | decision 92 measured the hop generalising to unseen depths zero-shot. Not re-verified here |
| E2 | Compose relations that combine by rule | **PARTIAL** | kinship 2-hop 0.443 against 1-hop 0.777. Real but weak |
| E3 | Know when to stop hopping | **CLAIMED** | `halt_gate`, learned. 153 showed occupancy cannot supply this for free |
| E4 | Combine composition with C1's gate | **PARTIAL** | **159**: `index_at_hops` proposes neighbours at the hop's landing concept, gated on emptiness. `tests/test_index_at_hops.py` shows a chain reaching an answer through a dead end it could not reach without, and the fan-out costing 1 extra read where an ungated one would cost 56. **Mechanism only** — no task result yet, and **160**: it cannot currently be combined with `inherit`, so the run that would give it one is blocked |

**Depends on:** A1, D. **E4 depends on:** C1, D3.

## F. The goal itself

| # | must be able to | verdict | evidence |
|---|---|---|---|
| F1 | Answer from awareness rather than prediction | **PARTIAL** | every relational task is scored on a single answer token. That is not next-token prediction (note 047 rules that out as the objective), but it is also not "form a response from awareness of the concepts in the question" |
| F2 | Take a query in more than one modality | **UNTESTED** | depends entirely on B4, which is untested |
| F3 | Produce more than one token of response | **UNTESTED** | **nothing in this project has ever scored a multi-token answer.** Named here because it is in GOALS and has never been on any roadmap |

**Depends on:** everything above.

## G. The deployment constraints

These are GOALS §4's ladder. **This file does not restate their verdicts** — the
gate table in [GOALS §4](GOALS.md) is the only place a gate verdict is written,
and duplicating it is how two documents start disagreeing.

| # | must be able to | verdict | evidence |
|---|---|---|---|
| G-C1 | Stay local — no population statistic, no barrier | **CLAIMED** | verified by inspection at each mechanism, not by measurement — decision 134 and note 044 are where it was argued. `AddressSketch` holds it (a node hashes a key it already has). **This checker downgraded it from PASSING on its first run**, correctly: an argument is not a number |
| G-rest | asynchrony, churn, bandwidth, scale | **see GOALS §4** | G0–G3 passed, G4 passes on one seed with training traffic unmeasured, G5 contested |

---

## What this ledger says right now

**8 PASSING, 7 PARTIAL, 0 FAILING, 4 UNTESTED, 4 CLAIMED.**

**D2 and D3 both moved today** — 157 typed the write, 158 typed the read. What
remains on D3 is that the relation is **fixed rather than chosen**: the mechanism
follows a named edge, and nothing decides which name. Note 051 §5 flags choosing
as unsolved for open queries, and decision 147 is why a learned chooser should
not be attempted before the fixed one is shown to pay on a task.

**No FAILING rows remain.** All three that were failing this morning — D2, D3,
E4 — are PASSING or PARTIAL, and every one moved on a measurement. What the
PARTIALs share is that each is a MECHANISM shown to work in isolation whose value
on a task is unmeasured: D3's relation is fixed rather than chosen, and E4 has
never run on the linked task. **157's LINKED column at 0.1275 is still the
number to move**, and it is now reachable rather than blocked.

The PARTIAL that matters most is **C2**. Typing addresses moves it: `key(entity,
has-value)` reading empty is *"I don't know this entity's value"*, where
`key(entity)` empty only ever meant *"nothing was written here"*.

**C4 and F3 are the two rows nobody had written down.** C4 — declining to
answer — is the one that matters for a claim the project would want to make:
that it is structurally free of confident confabulation because its gate is a
property of the store rather than a learned confidence. **That may well be true
and it is currently untested**, and an untested claim about honesty is a poor
thing to be proud of.

**And the older embarrassment is F3.** The goal is to form a response from
awareness of the concepts in a question. Nothing here has ever produced more than
one token of answer. That is not a criticism of any decision — single-token
scoring is what made everything above measurable — but it has never been written
down as a gap, and a ledger that omitted it would be flattering rather than
factual.

## Re-check obligations, so rule 3 is mechanical rather than remembered

    a change to      re-run
    ---------------  --------------------------------------------------------
    A1 A3 A4         everything. These are the floor
    B1               B2 B3 (grouping quality feeds both)
    C1               C2 C3, and every arm of g19-01
    D (any row)      A4 first — typing spends capacity — then E, then C2
    the hop          E1 E2 E3 E4
    families.py      143 145 148 149 155, all of which are measured on it

**Precedent:** decision 74's failure was a default that moved and invalidated
comparisons nobody re-ran. The byte-identity rail on `family_links` exists
because of it, and it is why row 3 above is a rule rather than an intention.
