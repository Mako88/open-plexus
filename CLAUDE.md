# Open Plexus

Read `docs/plan.md` first. It is the one design doc: what we are building, why it might work,
and the steps in order. What a built thing does lives in its code. Findings live in the commit
that produced them and in a reading under `readings/`, never in the doc.

## How to work

- **Work in long stretches without asking.** Take the likelier of two routes yourself and
  switch if it does not pay. Stop only when a step needs John: a decision that changes the
  plan, spending money, or anything that cannot be undone.
- **The GPU is shared.** Ember plays Minecraft on this machine in the evenings, so no job may
  need an unbroken day. Any training job checkpoints every few minutes and resumes from its
  checkpoint; any long reading writes rows as it goes and can be re-run to fill in the rest.
- **Prove the wiring before trusting a reading.** Every arm is run once with the piece under
  test disabled, and the number must move. A store that is not consulted, a prompt the model
  never saw, and a thinking mode left on have each produced a wrong reading in this project's
  past. Check the system prompt, the sampling settings and the thinking switch in the reading
  itself, not in your head.
- **A reading is a JSON file under `readings/`** with the command, the git SHA and the model
  that produced it, committed with the change it measures. Every reading carries a floor (the
  model given nothing) and a ceiling (the model given the whole relevant text) beside the arm.
- **Keep the plan short.** When a step is done, strike it. When a step changes, rewrite it. The
  doc is read whole at the start of every session, so it must stay short enough for that.
- **Push whenever.** There is no CI yet; add it when there is something worth running on it.
- **Toolchain:** Python 3.12 in `.venv` via `uv sync`, torch from the cu126 index. See the
  comment in `pyproject.toml` before touching that pin.
