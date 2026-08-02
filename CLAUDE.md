# How to work on this

The goal is in [README.md](README.md). This is how to not fool yourself while
chasing it.

**Its predecessor was 1,617 lines.** Every rule in it was written after a real
incident and every one was individually justified, and it still had to be read
in full before any work could start. Worse, its own evidence was that written
warnings kept failing to prevent their own repeat — four times for one rule. So
the standing policy is: **a rule that keeps being broken becomes a check, not a
longer paragraph.**

---

## The one rule everything else serves

**State no behaviour that has not been observed.** Not in chat, not in a
docstring, not in a commit message. "Should work" and "this fixes it" are
predictions — label them until something confirms them.

This applies hardest to borrowed claims. A statement about what someone else
measured is a claim about behaviour like any other, and a wrong one gets filed
under *established*, sits upstream of everything, and no downstream measurement
will ever reach it.

**And a diagnosis is a claim about behaviour.** *"It failed because X, so build
Y"* is two unobserved claims wearing the clothes of an explanation. Check the
failure is repairable before proposing the repair — that check has cost minutes
and saved builds, repeatedly.

---

## Measuring

- **Observe the quantity the change claims to move**, not a downstream proxy. A
  green end-to-end run cannot say which of six components is working.
- **A constant that never changes looks like the background.** Before concluding
  a mechanism is refuted, list what was held fixed. `tools/check_constants.py`
  refuses a pinned number that says nothing about where it came from — one such
  number was worth 0.71 on the headline it was under.
- **Sweep a hyperparameter on every arm or on none.** Tuning your own side
  against an untuned baseline is undetectable from outside.
- **Compute what doing nothing scores, before reading any number.** It is rarely
  zero, and it moves when the grid gains an axis.
- **A caveat printed next to a number does not attach to the number.** A bound is
  not a value; refuse to compute through it.
- **A sweep that pins at the edge of its grid has not swept.**
- **Reproduce before you believe**, and match the seed count to how rare the
  failure would be. Three seeds miss a one-in-eight failure about two thirds of
  the time — a perfect score is a reason to run more, not fewer.
- **Score predictions mechanically.** One cell of eighty-four once failed a
  prediction that reading the tables would have passed.
- **A control tests the DATA, not the CODE.** A shuffle asks whether a pattern is
  real; it says nothing about whether the measurement is right, and it once
  removed the precondition for a bug and then reported the bug absent.
- **When new code re-derives something an existing tool computes, print both.**
  Three lines is exactly the size at which nobody thinks to check.

---

## Building

- **Ship the connection test with the mechanism** — perturb the input, assert the
  output moves. Not that it runs without raising.
- **A test that something did NOT change needs a companion asserting something
  DID.** An unchanged-assertion passes whenever the mechanism is disconnected.
- **A test has proved nothing until you have seen it go red for the right
  reason.** `tools/mutate.py` automates that; a surviving mutation marks a
  vacuous region of the test set.
- **Add a mutation when you add a mechanism**, and re-point it in the same commit
  as any refactor that moves its target.
- **A failing test is a claim about the code until shown otherwise.** Widening a
  bound converts a caught bug into a silent one.
- **One implementation per behaviour.** A duplicated path is a fix that did not
  land, wearing the appearance of one that did.
- **Search before you build, and by capability rather than by the name you would
  have used.** A negative search result is not a finding until it was a wide one.
- **New mechanisms default to off**, so earlier results stay reproducible.

---

## Recording

- **The README carries the claim. The measurement lives with the run.** No result
  tables in prose.
- **A finding updates a line; it never appends an entry.** The predecessor
  reached 6,040 lines by appending, and became unreadable, and was therefore read
  selectively, which produced three wrong recommendations in one day.
- **Every ruled-out option says what would revive it.** Refutations are
  conditional on their configuration and at least two have become right later.
- **Approved-but-not-built is a state.** An option nobody has considered and one
  approved four hours ago must not look identical — that is exactly how an
  agreed piece of work went missing for two sessions.
- **Prefer generated records to written ones.** Every failure worth recording
  here was a human summary outrunning its source.

---

## Running

- **`python tools/preflight.py` before every commit.** One command, nothing
  piped, every exit code checked — a suite piped through `tail` reports `tail`'s
  status, which is always 0.
- **Commit messages go through `-F` or a quoted heredoc, never `-m`.** Backticks
  in a double-quoted shell argument run as commands and the word vanishes from
  the permanent record. This happened seven times before it became a check.
- **Do not run the mutation harness while anything else touches the tree.** It
  edits source in place, and a commit made mid-run can capture a live mutation.
- **A red mutation shard is blocking, not a note.** One sat red for a whole
  session, on the path that decides whether a departed node stops answering.
- **Long jobs go to the background and something else gets built meanwhile.**
  Never end a turn with neither more work nor an armed wake-up.
- **Rewrite `NOW.md` at the end of every turn.** It is the state a tick or a
  compaction reads back; anything only in the conversation is gone. Rewrite, do
  not append — a finding updates a line. The five-minute working monitor that
  reads it is `.claude/skills/monitor`.

---

## Conventions

**Python 3.14. numpy is for the model layer only** — anything a result is
measured *against* stays dependency-free, because the ruler must be obviously
correct.

**Alternate verifying and building.** Every audit yields a satisfying provable
result and every new mechanism most likely yields a null. The gradient points
away from the goal and following it feels productive the whole time.
