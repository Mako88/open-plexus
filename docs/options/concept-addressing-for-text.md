# Option record — concept addressing as a fix for text prediction

> **RECORD ONLY. This file carries no status.** Chosen, refused, untried or live-both lives
> in [DECISIONS.md](../../DECISIONS.md) alone. Here there are only events, and events do not
> un-happen, so nothing here can go stale. **Absence means untried.**
> Format and the CONFIG block: [README.md](README.md).

---

## What exists

- `openplexus/concepts.py`'s `ByTable` and the address-density experiments around
  decision 141.

---

## What was tried, and what came back

### The address COUNT did the work, not the concepts — `141`

    CONFIG  when    2026-07-29
            source  decision 141
            script  unrecorded
            task    corpus, character level
            model   store addressed by a discovered grouping
            knobs   grouping from real text against grouping from SHUFFLED text
            scale   unrecorded

**0.540 bits at bias 0** — and a grouping built from **shuffled** text does as well. The
control is what makes this a refutation rather than a result: if the grouping's *content*
mattered, destroying it would cost something, and it costs nothing.

So what was measured was address density, which is a capacity knob, not concept structure.

### And decision 144 explains it from the other side — `144`

    CONFIG  when    2026-07-29
            source  decision 144
            script  experiments/g19_01_can_grouping_answer_what_was_never_stated.py
            task    families with exceptions
            model   concept addressing
            knobs   grouping on against off
            scale   3 seeds

**Text is nothing but exceptions.** Every word has its own continuations, so every grouped
address holds a dozen competing values, and the majority-wins behaviour that makes concept
addressing work on families is exactly what destroys it on text. Record:
[by-concept.md](docs/options/by-concept.md).

**Revival:** a text objective that is not next-token, since the exception density is a
property of the target rather than of the medium. Record:
[bits-per-token.md](bits-per-token.md).
