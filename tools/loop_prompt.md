# The loop prompt

The text a background task should fire to continue this project unattended.

**Kept in the repository on purpose.** The previous version named `g11-04` as
work in flight and `BACKLOG.md` as the todo list, months after both were retired,
and every turn began by working out that its own instructions were wrong. A
prompt held only in a scheduler drifts because nothing reviews it; a prompt in
the repo is reviewed by whoever changes what it points at.

**When something here goes stale, fix it in the same change that made it stale.**

---

    Continue Open Plexus in D:\repos\open-plexus (github.com/Mako88/open-plexus).
    Always act -- never reply "nothing to do".

    READ STATE.md FIRST. It is the only document kept current. GOALS.md is intent
    and constraints; DECISIONS.md is history and is never rewritten. If STATE.md
    and DECISIONS.md disagree, STATE.md wins.

    Order of preference:
      1. Score any finished GitHub Actions sweep against its registered
         predictions, honestly, including the refuted ones.
      2. Act on results -- the sweep file, a note if there is reasoning worth
         keeping, the DECISIONS entry, and the STATE.md update.
      3. Take the next item from STATE.md.
      4. If blocked on John, do the largest piece of work that does not depend
         on his answer.

    Standing constraints:
      - Sweeps are GitHub Actions DISPATCH-ONLY via `gh workflow run`, one matrix
        at a time. Estimate cost from the MOST EXPENSIVE cell and state it per
        cell -- g11-03 lost four of six cells to estimating from a cheap one.
      - Nothing heavy runs locally. `python tools/mutate.py --only <name>` for a
        single mutation.
      - NEVER use bash heredocs.
      - NEVER `git commit -m`. Write the message with the Write tool and use
        `git commit -F`.
      - Run `python tools/check_all.py` as the LAST thing before every commit.
        Not as one compound shell command -- a shell reports only the last
        statement's exit code, and that once reported success while two of five
        checks were failing.
      - Long jobs go to the background so work continues while they run.

    C1 WAS AMENDED 2026-07-27: the real constraint is "does it work over the
    internet" -- bounded bytes per hop, and no barrier that stalls when one
    participant is slow or gone. A global all-reduce is still out even at twelve
    bytes. See GOALS.md.

    THE GOAL IS A MAP OF CONCEPTS, not next-token prediction: a system that
    learns how concepts relate, in the hope that it reasons rather than
    continues. The LLM-replacement track is DEFERRED, not merely secondary.
    GOALS.md 1.2.

    Do not chase a benchmark at the current scale unless the result transfers to
    the scale being aimed at. When a decision IS scale-specific, say so where it
    is made, with the trigger to revisit, and register it in docs/SCALE.md.

    Pre-register predictions before every sweep. List decisions pending from John
    at the end of every response.
