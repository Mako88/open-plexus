# How an option record is written

**One file per option in [DECISIONS.md](../../DECISIONS.md).** The tree says which option a
component *is*; a record says what was *tried* and what came back. `tools/check_options.py`
enforces the split, and this file is the format it enforces against.

---

## The one rule

**A record holds EVENTS. It carries no status, in markers or in prose.**

*"On 2026-07-14 this configuration produced 0.9220"* stays true forever. *"This is what we
use"* stops being true the day something better lands, and nothing in the file announces
it. The 6,040-line log this project replaced went wrong by holding conclusions next to
each other with no way to tell a live one from a superseded one — three wrong
recommendations in a single day. **A file that structurally cannot state a conclusion
cannot be mistaken for one.**

So: no ✅ ❌ ⬜ 🔀 outside the header, and none of it in words either. **Absence means
untried** — there is no "gaps" or "next steps" section, because those are status and they
rot.

---

## The shape

    # Option record — <the option, named as the tree names it>

    > the standard header, copied verbatim from any existing record

    ## What exists

    - the files, classes and config fields, so a reader knows what is real code

    ## What was tried, and what came back

    ### <what was tried> — <the citation>

        CONFIG  when    ...
                ...

    the result

---

## The CONFIG block

**Every `###` entry carries one, and it comes before the prose.** A number is a claim about
a configuration, not about a mechanism, and this project has twice quoted one across
regimes where it did not hold — the beam's headline was CLUTRR chain recovery at depths
2–10 and got read as kinship at hops 2, where the gain is about five times smaller.

The block is fixed-key so that scanning down a file compares like with like:

    CONFIG  when    2026-07-30
            source  note 103, g21-01
            script  experiments/g21_01_does_the_beam_pay_in_run.py
            task    kinship, hops 2
            model   width 256, single-token keys, linear readout
            knobs   search_beam_width 4, search_prune_every 1, concept_nodes 0
            scale   32 cells, 8 seeds

| key | what goes in it |
|---|---|
| `when` | the date the result was **written down**, which for an archived log entry is when it landed in the log. Entries 1–77 all read 2026-07-27 because they were imported in one commit; that is the honest bound, not the date the work was done |
| `source` | where the number is written down — a note, a sweep, a decision entry. Never this file. `tools/check_provenance.py` reads this field, so a **range is written `notes 093-101`**, not "093 to 101" |
| `script` | **what produced the number** — the experiment, tool or test, with its arguments where they matter. The path must exist |
| `task` | the instrument and its parameters. `design pass` when nothing ran |
| `model` | width, keys, readout, store — what the model WAS, not what was varied |
| `knobs` | the config fields this entry is about, with their values |
| `scale` | seeds, cells, puzzles, corpus size. What the error bars rest on |

**`source` and `script` are different questions and both are required.** John's
instruction, 2026-07-30: *"any numbers that are cited should also cite the location of the
script/test that was used to get the number."* Note 105 is why it is not redundant — a
real measurement, reproducible to four decimals, that two notes attributed to sources
which did not contain it. The `source` field would have been wrong in both. The `script`
field would have re-run it in seventy seconds and closed the question on the spot.

**A key that cannot be recovered is written `unrecorded`, never omitted.** An absent field
reads as "not relevant"; `unrecorded` reads as "nobody wrote it down", which is the thing a
later reader has to know. `note 074` is the case in point — no committed script reproduces
`note 065`'s configuration, so every entry quoting it says so in the block rather than in a
footnote someone skips.

---

## Two habits that are not checkable

**Scan `docs/archive/notes/`, `docs/archive/` and the source before writing the first entry.**
Rule 11 of the tree. A record that starts empty invites re-running work that already has an
answer — which happened on 2026-07-30, when a partitioning result was nearly re-reported as
new with `note 081` already holding it.

**Quote the one line that is load-bearing and link the rest.** A measurement belongs to
exactly one file, which is its note or its sweep record. A record that copies a whole table
is a second copy to keep correct, and the copy is the one that will drift.

---

## The one thing that IS checked about the numbers

`tools/check_provenance.py` takes every measurement-shaped numeral in an entry — a
decimal, a percentage, a thousands-grouped integer — and requires it to appear in a file
that entry's `source` field names. A figure derived while writing the record goes in
`tools/provenance_baseline.json`, where the exemption is visible and can only be removed.

**It found something on its first run over the two records that already existed.** The
figure `0.9220`, carried in `DECISIONS.md` as the accuracy case for concept partitioning,
appears in no note, sweep or script output anywhere in the repository — and the two notes
that quote it credit two different sources and pair it with two different baselines.
`note 105` is what came of that.

---

## WHAT A GREEN RUN DOES NOT MEAN

Stated here because the checks are convincing enough to be over-read, and an over-read
check is worse than none — it converts an open question into a settled one for free.

**Every number present is sourced. Nothing verifies a number that should be here is not
MISSING.** The asymmetry is total: a false claim is caught, an omission is invisible.
These 85 records were compressed out of 105 archived notes in a single pass on
2026-07-30, and **the compression judgement — what was carried forward against what was
left in the archive — has been reviewed by nobody.** It is recoverable, because nothing
was deleted and every citation resolves, but recoverable is not the same as checked.

**A number can also be attached to the wrong claim and still pass**, since the checker
asks whether a figure exists in the source, not whether it means what the sentence says.
A figure transposed between two rows of one table is invisible to it.

**And nothing checks the `What exists` section against the code.** If a config field or a
class is renamed, the record goes stale silently. The `script` field's paths *are*
checked — that caught three wrong filenames during the migration — but the file list is
prose.

The honest summary: these checks catch **citation** failures, which is one class, and it
is the class that had cost this project the most. They are a net, not a wall.
