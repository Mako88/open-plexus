# ARCHIVED — the investigation notes, 001–105

**Replaced by [DECISIONS.md](../../../DECISIONS.md) and
[docs/options/](../../options/)**, on 2026-07-30, the day the option-record migration
finished.

**They are kept, not deleted, and they are still the reference.** Every option record
cites a note by number in its `source` field, and `tools/check_provenance.py` resolves
those citations **here** — so a number in a record can always be followed back to the run
that produced it. What changed is which document is authoritative: **the option record is,
and these are the footnotes.**

---

## Why they were retired

**They were a chronological log, and this project already knows what a chronological log
does.** `DECISIONS.md` reached 6,040 append-only lines, became unreadable whole, and was
therefore read selectively — which on 2026-07-29 produced three wrong recommendations in a
row off claims later entries had superseded. The notes had the same shape: 105 files
ordered by *when* rather than by *what*, so finding what is known about a mechanism meant
knowing when it was measured.

**And the failure arrived on schedule.** On 2026-07-30 `tools/check_provenance.py` found
seven citations that did not resolve, the worst of which was a real measurement — `0.9220`,
the accuracy case for concept partitioning — attributed by two notes to two different
sources, neither of which contained it. One of those notes says in its own text that it
found the claim in the summary document rather than in the note it went on to cite. Full
account: [note 105](105-the-partitioning-accuracy-figure-has-no-source.md), and the
plain-language version is
[explainer 044](../../explainers/044-the-number-was-right-and-the-pointer-was-wrong.md).

An option record is organised by **what the option is**, which is how anyone actually looks
something up.

## The one thing a note did that a record cannot, and where it went instead

A note could hold a **prediction registered before a run**. An option record is an event
log written after the fact, so by the time an entry exists the answer is known and the
anti-retrofit property is gone.

That turned out not to be what notes were doing. **89 of 91 sweep records carry a
`PREDICTIONS` section and `tools/check_rails.py` enforces it; only 18 of these 105 notes
have a prediction section at all**, and several of those are early requirements documents.
**The commitment device was always the sweep record.** The rule now says so directly rather
than implying that a note is where a prediction lives.

For a finding with no sweep — a local probe — the prediction goes in the option-record
entry as a `PREDICTED` line **committed before the run**, and git commit ordering is what
makes it non-retrofittable. That is weaker than a separate artifact and it is labelled as
weaker.

## How to read these

**Look an entry up; never read forward from a point.** Notes were never rewritten when
later work overturned them, so reading in sequence hands you claims that were true when
written and are false now. That is the property that makes them safe as footnotes and
unsafe as a current record — the same reason the decision log is archived beside them.

**If a reference elsewhere in the tree does not resolve, look here before concluding it
does not exist.** Several documents predate the archiving and cite `docs/notes/`.
