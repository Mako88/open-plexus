# Option record — select by the decode margin

> **RECORD ONLY. This file carries no status.** Chosen, refused, untried or live-both lives
> in [DECISIONS.md](../../DECISIONS.md) alone. Here there are only events, and events do not
> un-happen, so nothing here can go stale. **Absence means untried.**
> Format and the CONFIG block: [README.md](README.md).

---

## What exists

- `openplexus/search.py` — `margin`, which computes the quantity. It is used for
  ambiguity detection in the search line, not for choosing between retrievals.

---

## What was tried, and what came back

### It scores below the summed baseline — `147`

    CONFIG  when    2026-07-29
            source  decision 147
            script  unrecorded
            task    families with exceptions
            model   two retrievals, choose by which decodes with the larger margin
            knobs   selection by decode margin
            scale   unrecorded

    select by decode margin   0.581
    summed baseline           0.688

**Confidence in *an* answer is not evidence about *which retrieval* produced it.** A
retrieval from the wrong address can decode sharply to the wrong thing; the margin
measures how peaked the readout is, and a peaked readout over a superposition is exactly
what a confidently wrong answer looks like.

### The margin is not useless — it is being asked the wrong question — `129`, `130`

    CONFIG  when    2026-07-28
            source  decisions 129 and 130
            script  unrecorded
            task    kinship, ambiguity detection
            model   `search.margin` used to decide WHETHER to search
            knobs   search_gate_margin
            scale   unrecorded

Ambiguity **is** detectable before searching, and gating on it pays **+0.020** over
searching everywhere. So the same quantity that cannot choose between two retrievals can
say whether the current step is ambiguous at all. Different question, and the tell is that
one is about a comparison and the other about a single distribution.
