# Handoff — state of play, 2026-07-27

**For the next session.** Read this, then `GOALS.md`, then `BACKLOG.md`. The
notes in `docs/notes/` are the reasoning; `DECISIONS.md` is the running log John
reads. This file is a snapshot and goes stale — trust the notes over it where
they disagree.

---

## Where the model actually is

Tiny Shakespeare, character level, the standard benchmark:

    uniform                        6.000 bits/char
    OUR MODEL, width 128           5.494
    OUR MODEL + exact cache        5.311   (width 128, 128 slots)
    unigram (letter frequency)     4.829   <- WE STILL LOSE TO THIS
    backprop attention, width 16   4.197   (our own baseline, ~10k params)
    bigram                         3.583
    trigram                        2.951
    char-LSTM (published)          ~1.45

**The single most important sentence: every component passes its capability test
in isolation and the whole fails.** Recall 1.00 at 32 bindings, readout 1.00 on
clean input, delta rule exact for the layer it updates. The failure is the
composition — superposition destroys the per-item information downstream needs.

## The four findings the rest depends on

1. **Note 033 — the ceiling was a bigram.** `M += value ⊗ key(t-1)` makes a
   retrieval the sum of values that followed this token: a bigram count table.
   Measured at cosine 0.9455 against exactly that table. **"Beat a bigram" was
   the architecture's ceiling, not its target.**
2. **Note 034 — the ceiling moves, and costs.** Keys derived from a token PAIR
   take a two-token-context task from 0.533 (chance) to 1.000. Price: 469
   distinct keys against 66 tokens on this corpus.
3. **Note 035 — the store's effective rank is ~3, at every width.** Not a
   defect: a character bigram table over 66 symbols is genuinely low-rank. This
   is why width sweeps have always been flat.
4. **The exact cache works** — first controlled improvement on the corpus, and
   the control matters: quadrupling width buys 0.089 bits, a comparably sized
   cache buys 0.244.

## What has been refuted, so it is not re-proposed

Each has a test pinning it. **Do not re-try these without reading the test.**

- **readout bias** (g11-02) — imports the marginal, collapses `reward_recall`.
- **competitive retrieval** (`retrieval_steps`) — settling is for AUTO-associative
  memories; ours is hetero-associative, so iterating is power iteration onto the
  dominant singular direction and *forgets the query*. 0.924 → 0.128.
- **orthogonal updates** (`orthogonal_every`) — the rank collapse is real (2.22
  of a 32 window) but ours is the DATA being low-rank, not the rule failing.
  Cure for someone else's disease.
- **pair keys as a win** — they lose at every width 16–128 and the gap does not
  narrow (g11-04). The ceiling result stands; paying for it needs something
  other than width.

**All four failed for one reason and it is the through-line: `r = M @ key` is a
SUM, and nothing applied after a sum recovers what the sum destroyed.**

## In flight / next

- **The exact cache is the live mechanism.** `cache_slots`, off by default.
  Next: a sweep on slots × sharpness × width, and whether it interacts with
  pair keys (the cache may be what makes the higher ceiling affordable).
- **g11-04 must be re-run on a DATA axis**, not width — see its sweep file. The
  control failed because capping the corpus to fit the budget saturated the
  baseline.
- **`tools/summarise_g11_04.py` produces no output in CI** though it runs
  locally on the same artifacts. Fix before the re-run.
- BACKLOG.md has the rest.

## Two rules bought with real failures this week

- **When a sweep axis enters the cost quadratically, estimate from the MOST
  expensive cell.** g11-03 lost four of six cells to this.
- **When re-scoping a sweep to fit a budget, check the control can still fire.**
  g11-04 was fully spent and answered nothing because of this.

## Constraints — note the amendment

C1/C2/C3 are in GOALS.md. **C1 was amended 2026-07-27** at John's direction: the
real constraint is *"does it work over the internet"* — bounded bytes per hop,
no barrier that stalls when a participant is slow or gone. A global all-reduce
is still out even at twelve bytes. Everything measured before that date was
measured under the stricter rule.

## Working agreement with John

- **He has given blanket permission for architectural decisions** provided they
  do not contradict the goals or constraints. Document what you decide in
  DECISIONS.md and tell him; do not block on him for technical calls.
- **List pending decisions at the end of every response** — he checks from his
  phone and will otherwise miss them.
- He is not deeply versed in modern ML internals. Explain plainly, keep the
  numbers, and do not hide bad news.
- Standing operational rules: sweeps are GitHub Actions DISPATCH-ONLY via
  `gh workflow run`, one matrix at a time, cost stated first. Nothing heavy runs
  locally. **Never use bash heredocs** (they hang the shell). **Never
  `git commit -m` with backticks** — write the message with the Write tool and
  use `git commit -F`. Five checks before every commit: `mutate.py --verify`,
  `unittest discover`, `check_workflows.py`, `check_rails.py`,
  `check_duplication.py`.

## The standard this project holds itself to

Pre-register predictions before every sweep and score them honestly, including
the refuted ones. **Seven conclusions were caught wrong this week by that
discipline**, two of them mine after I had written them down as findings. A
mechanism measured only on the task it was designed for is not measured. When a
mechanism adds state, compare against a model given the same amount of state —
g10-09 was retracted for missing exactly that.

**Open questions John has raised that are not yet answered** are listed at the
end of DECISIONS.md and in BACKLOG.md: unfreezing the value projections, how
input and output work in a deployed distributed system, event-sourcing-style
eventually-consistent reads between nodes, and whether optimising for language
is the right proxy for the AGI goal.
