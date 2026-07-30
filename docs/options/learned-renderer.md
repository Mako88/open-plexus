# Option record — a small learned renderer trained on our own concept sets

> **RECORD ONLY. This file carries no status.** Chosen, refused, untried or live-both lives
> in [DECISIONS.md](../../DECISIONS.md) alone. Here there are only events, and events do not
> un-happen, so nothing here can go stale. **Absence means untried.**
> Format and the CONFIG block: [README.md](README.md).

---

## What exists

- Nothing. The bar it would have to clear does exist, in `openplexus/render.py`'s
  `content_words` check and `spoken_faithfully`.

---

## What was tried, and what came back

### Specified with a FAITHFULNESS test rather than an accuracy one

    CONFIG  when    2026-07-29
            source  decision 163 section 3, openplexus/render.py
            script  none -- nothing built
            task    none
            model   n/a
            knobs   none
            scale   n/a

The specification, written before anything is built:

- **perturb the set and the text must move**
- **hold the set and the text must contain nothing the set does not**

That is deliberately not an accuracy test. A renderer scored on how well its output reads
can be fluent and unfaithful at once, and the whole reason the template realiser exists is
that a fluent renderer **can produce the right sentence from a wrong walk**.

The paired shape — something must change AND something must not — is the same rule that
caught the vacuous C1 locality test, where an unchanged-assertion passed because the fixture
zeroed the weights it was perturbing.
