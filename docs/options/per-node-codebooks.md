# Option record — per-node codebooks plus translation between them

> **RECORD ONLY. This file carries no status.** Chosen, refused, untried or live-both lives
> in [DECISIONS.md](../../DECISIONS.md) alone. Here there are only events, and events do not
> un-happen, so nothing here can go stale. **Absence means untried.**
> Format and the CONFIG block: [README.md](README.md).

---

## What exists

- Nothing, in either direction. No codebook, no translation layer, and no falsifier.

---

## What was tried, and what came back

### Refused rather than untried, and the reason is the order of the problems — `note 053`

    CONFIG  when    2026-07-29
            source  note 053
            script  none -- a constraint register entry, nothing measured
            task    none
            model   n/a -- nothing in the project is multimodal
            knobs   none
            scale   n/a

Aligning two independently-learned discrete spaces with no paired data is the
unsupervised-translation problem. It is **strictly harder than the project's own goal**,
and solving it as a *precondition* for that goal is the wrong order by a wide margin.

### The failure it would be solving, which is real and has no local detector — `note 053`

    CONFIG  when    2026-07-29
            source  note 053
            script  none -- reasoning from decisions already made
            task    none
            model   n/a
            knobs   none
            scale   n/a

    MERGE   two distinct things -> one id           163 §1 named this
    SPLIT   one thing -> two ids on two nodes       note 053

**SPLIT is worse than MERGE in one specific way: no node can detect it locally.** A merge
is at least visible to the machine that made it — two inputs that should differ produce
the same address, and a probe on that machine finds it. A split is invisible everywhere:
node A wrote to address `x`, node B wrote to `y`, each is internally consistent, and the
disagreement exists only in the relation between two machines that never compare notes.

It is likely rather than hypothetical: a codebook is *fitted from data*, and the project's
premise is that nodes are heterogeneous and constantly arriving and leaving, so any
codebook that adapts per node diverges by construction. A frozen one still splits across
*versions* — a node joining later with a newer encoder disagrees with every node already
running.

### The two options that were preferred, and what one of them costs — `note 053`

    CONFIG  when    2026-07-29
            source  note 053
            script  none -- design pass
            task    none
            model   n/a
            knobs   none
            scale   n/a

    (a) FROZEN GLOBAL CODEBOOK, versioned as part of network identity. A node whose
        version does not match is refused rather than allowed to write
    (b) QUANTISE ONCE AT INGEST. The owning node converts, and only concept ids travel

They compose rather than competing. (b) is what makes the architecture cheap — one
conversion per input, at the edge, outside the learning loop — but **(b) alone does not
prevent SPLIT**, because two nodes can each ingest the same content arriving by different
routes.

**The cost of (a), stated plainly in the note:** a codebook that is part of the network's
identity cannot improve without re-addressing everything already stored, which sits against
C4. The note's own resolution is that C4 governs the *learner* and not the *sensor* — and
it labels that an argument rather than a measurement.

**Blast radius today: zero.** Nothing is multimodal, no quantiser exists, and no result
depends on it. It becomes load-bearing the moment the first non-text modality arrives.

### There is a THIRD option, it was refused without a number, and one arrived later

    CONFIG  when    2026-07-30
            source  note 078, note 053, and docs/options/openea.md
            script  tools/openea_alignment.py
            task    OpenEA EN_DE_15K_V2, zero supervision
            model   mutual nearest neighbours over relational context, bootstrapped
            knobs   mutual agreement; confidence gate off
            scale   15,000 gold links, 74.0% shared relation vocabulary

Note 053 offered **(a) frozen global codebook** and **(b) quantise once at ingest**, and
refused per-node codebooks plus translation on the grounds that aligning two
independently-learned discrete spaces with no paired data is *"strictly harder than the
project's own goal."*

**That refusal has a number against it now, and it is the wrong way round.** Note 077/078
measured an instance of exactly that problem — two independently-built DBpedia graphs with
entity names encoded so string matching cannot cheat, no supervision — and reached
**0.3098**, monotone in evidence and not plateaued. The two notes were written days apart
and neither cites the other: 077 frames itself as answering what note 076 left open about
`Merged`, not as bearing on note 053's SPLIT.

The distinction that matters is that **reconciliation-by-evidence is not translation.**
Note 053 refused a pairwise mapping between two code spaces. What was measured is nodes
agreeing about *which things are the same* from relational context, with no mapping built
and no coordinator — which is the shape `Merged` already has.

**The scope of the evidence, stated so this is not read as more than it is.** `EN_DE`
shares **74.0%** of its relation vocabulary, so the measurement says reconciliation works
when the two sides already describe the world with the same relations. That is not the
distributed case. The hard setting is `D_W` and `D_Y`, which share **0.0%**, and
`tools/fetch_openea.py` **refuses them by name** rather than returning a number, so the
instrument is half-built and the vocabulary-free seed is untried.

So the honest position is not that (c) is available. It is that **(c) stopped being
refused-by-argument and became an open measurement with a named dataset**, which is a
different state and the tree should be able to say so.
