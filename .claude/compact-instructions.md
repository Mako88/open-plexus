Instructions for summarising this session (Open Plexus, John's project, run from docs/plan.md).

The reader of the summary is the next Claude, resuming mid-stretch with no other memory of the session
beyond docs/plan.md, CLAUDE.md, the memory index and git. Write what those do not already hold; do not
restate the plan or the code.

Keep, in this order:

1. John's words. Every decision, rule, correction or preference John stated this session, quoted
   verbatim where short, with the date. Mark which ones are already written into plan.md or memory
   and which are not yet (those are the ones most at risk). Never soften a correction into a
   suggestion. John dictates, so keep his phrasing; do not paraphrase a rule into a different rule.
2. What is running right now. Every background task, detached process or server: its task id or
   PID, the exact command, its log file and part file, what it is measuring and against which tag,
   and what to do when it finishes. Say whose process it is (mine, or not mine: never end one that
   is not mine). Note any game or other tenant on the card and the last check of who is in front.
3. The state of the system. The lineage round (state/system ledger: last kept round, what was rolled
   back and why), which adapters the server is loading, which store the readings use.
4. Readings this session. For each: the reading file name (readings/hotpot-<UTC stamp>.json), the
   arm, n, the score with its paired made-right/made-wrong against its reference, and the verdict
   (kept, deleted with revival commit, or undecided). Numbers exactly as read, never rounded into a
   different conclusion. Say which readings are committed and which are still untracked.
5. The work in progress: the step of the plan, what the current change is meant to settle, the
   files and functions it touched (path and name), what is committed and pushed (short SHA) and
   what is not, and the exact next command or edit.
6. Failures and their fixes from this session that are not yet in memory, one line each.
7. The pending list: what John asked for that is not done, in his order, then my own next steps.

Leave out: tool output, file dumps, long code (name the function instead, unless a snippet is the
only record of something uncommitted), reasoning that led nowhere, and pleasantries. Do keep the
tone of the session: John wants pushback and plain statements, and work written to future selves
with encouragement; a line on what went well is worth keeping.
