# Option record — untrusted nodes

> **RECORD ONLY. This file carries no status.** Chosen, refused, untried or live-both lives
> in [DECISIONS.md](../../DECISIONS.md) alone. Here there are only events, and events do not
> un-happen, so nothing here can go stale. **Absence means untried.**
> Format and the CONFIG block: [README.md](README.md).

---

## What exists

- Nothing, in either direction. **No threat model at all.**
- The two capabilities a hostile node would abuse do exist: it can assert occupancy, and it
  can write to addresses it does not own.

---

## What was tried, and what came back

### Named as absent, with the two concrete attacks

    CONFIG  when    2026-07-30
            source  DECISIONS.md component 9, and decision 148 for the gate's structural
                    zero
            script  none -- nothing built
            task    none
            model   peer transport with no authentication and no attestation
            knobs   none
            scale   n/a

A node that **lies about occupancy** poisons the one selection rule this project has that
works — the occupancy gate reads exactly 0.0 for an unwritten address, and that structural
zero is trusted precisely because nothing can fake it locally. Remotely, something can.

A node that **writes to addresses it does not own** corrupts a store with no way for the
owner to notice, because the store is additive and a write is not attributable after the
sum.

### It forks on the project's endgame, which is undecided

    CONFIG  when    2026-07-30
            source  DECISIONS.md standing agreements
            script  none
            task    none
            model   n/a
            knobs   none
            scale   n/a

Open-source-and-runs-everywhere **implies** a threat model. A controlled network does not.
So this cannot be scoped until the endgame is, and the standing agreements record that the
endgame is deliberately open — recommendations must not quietly assume an answer.

That is why it is untried rather than deferred: the work is not blocked on effort, it is
blocked on a decision nobody has made.

### The merge gate is a SYBIL TARGET, and that puts this inside the learning rule

    CONFIG  when    2026-07-30
            source  note 078, and docs/options/openea.md
            script  tools/openea_alignment.py
            task    OpenEA EN_DE_15K_V2, zero supervision
            model   concepts.Merged driven by mutual nearest neighbours
            knobs   mutual agreement as the merge gate; confidence gate off
            scale   15,000 gold links

The two attacks named above are attacks on *storage*. This is an attack on *acquisition*,
and it is the one that matters more, because it was surfaced by the mechanism the project
chose on measurement rather than by a hypothetical.

`note 078` established that **mutuality is the merge gate and magnitude is not**: mutual
nearest neighbours reach **0.3098**, and adding a confidence gate makes it strictly worse —
**0.2334** at `sim >= 0.9` and **0.0855** at `sim >= 0.98`. The reason it works is that
mutual best match means *"neither has a better candidate"*, which is a statement about the
structure of the neighbourhood rather than about any one node's certainty.

**A statement about the structure of a neighbourhood is precisely what a population of
colluding nodes rewrites.** Enough sybils asserting each other as best candidates
manufacture mutuality for a pair that has none, and the gate has no second quantity to
fall back on — the project deliberately removed the one it had, because on honest data the
confidence gate was *harmful*. So the hardening that would seem obvious is the thing
already measured to cost accuracy.

That connection is recorded here rather than acted on. It is not a refutation of mutuality,
which is measured and is the right gate on honest data. It is the statement that
**component 9's threat model reaches into component 1's acquisition rule**, so a decision
about the endgame is not only an operational decision — it changes which merge gate is
admissible.

Nothing is measured against an adversary. No sybil experiment exists.
