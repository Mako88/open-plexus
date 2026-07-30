# Option record — `corpus.py`

> **RECORD ONLY. This file carries no status.** Chosen, refused, untried or live-both lives
> in [DECISIONS.md](../../DECISIONS.md) alone. Here there are only events, and events do not
> un-happen, so nothing here can go stale. **Absence means untried.**
> Format and the CONFIG block: [README.md](README.md).

---

## What exists

- `openplexus/tasks/corpus.py`, `tests/test_corpus.py`, `tests/test_ngram.py`.
- The whole g11 and g18 sweep families, and decisions 47 and 60–79.

---

## What was tried, and what came back

### Closed by 115 and 118, then reopened by g17-01 without anyone re-deciding

    CONFIG  when    2026-07-29
            source  decisions 115, 118 and 135-142
            script  openplexus/tasks/corpus.py
            task    corpus, character and word level
            model   various
            knobs   many
            scale   seven sweeps and about thirty decision entries

`115` closed saturation and `118` retracted the headline result taken on this instrument.
`g17-01` then reopened a line of work on it, and decisions **135–142 were measured here
without anyone re-deciding that it was the instrument.**

That is why the state is paused rather than refused: nothing about the instrument was
condemned, and a lot was measured on it after its own closure entry, which is a process
observation rather than a result.

### What it did establish, and it is load-bearing — `047`, `142`

    CONFIG  when    2026-07-27
            source  decisions 47 and 142
            script  unrecorded
            task    corpus, character level
            model   superposed store
            knobs   store on against off
            scale   unrecorded

**The objective was the ceiling, not the memory.** On a next-token objective the only
relation the store can express is *"what followed this"* — an n-gram — so this instrument
is what proved the project needed a relational objective. Record:
[relational-objective.md](relational-objective.md).

### And it carries the retractions

    CONFIG  when    2026-07-29
            source  decisions 117, 118, 135 and 138
            script  unrecorded
            task    corpus, character and word level
            model   various
            knobs   temperature; the g18 target
            scale   142 cells across four sweeps in 138's case

Four of the tree's refutations were taken here and three of them are retractions of numbers
this instrument produced. Records: [the-4540-headline.md](the-4540-headline.md),
[scoring-without-temperature.md](scoring-without-temperature.md),
[word-level-unigram.md](word-level-unigram.md), [g18-harness.md](g18-harness.md).
