# Option record — an off-the-shelf LLM as the renderer

> **RECORD ONLY. This file carries no status.** Chosen, refused, untried or live-both lives
> in [DECISIONS.md](../../DECISIONS.md) alone. Here there are only events, and events do not
> un-happen, so nothing here can go stale. **Absence means untried.**
> Format and the CONFIG block: [README.md](README.md).

---

## What exists

- Nothing, and no dependency on any language model anywhere in the tree.

---

## What was tried, and what came back

### Refused on the grounds rule 2 is written on

    CONFIG  when    2026-07-29
            source  note 052 section 3, CLAUDE.md rule 2
            script  none -- nothing built
            task    none
            model   n/a
            knobs   none
            scale   n/a

*No measurement*, and refused rather than untried. **The cheapest demo and the worst thing
for the claim**: a fluent renderer can produce the right sentence from a wrong walk, so the
end-to-end number would be measuring its world knowledge rather than this system's
retrieval.

That is rule 2 exactly — a green end-to-end run cannot say which component worked — and it
is the same reason the concept set rather than the sentence is the scored artifact.

**Revival condition:** a faithfulness test showing it cannot add or drop a fact. The bar
already exists in `render.py` (`content_words(render(...)) - FRAME` must equal the answer
set, and `spoken_faithfully` checked in both directions), so the revival is measurable
rather than a matter of judgement — which is what makes this a refusal that can expire.
