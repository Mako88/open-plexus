# Open Plexus

Read `docs/plan.md` first. It is the one design doc: what we are building, why it might work,
and the steps in order. What a built thing does lives in its code. Findings live in the commit
that produced them and in a reading under `readings/`, never in the doc.

## How to work

- **Work in long stretches without asking.** Take the likelier of two routes yourself and
  switch if it does not pay. Stop only when a step needs John: a decision that changes the
  plan, spending money, or anything that cannot be undone.
- **Consult a Fable session when a step needs a design decision the plan does not make**, or
  when a reading refutes something the plan says and the next step is not obvious. The plan
  was written with Fable, and a design question is cheaper to ask than to guess. Put the
  question in a sentence with the reading beside it, and record the answer where it belongs:
  in the plan if it changes a step, in the commit if it explains a result. Implementation
  choices inside a step are yours.
- **The GPU is shared.** Ember plays Minecraft on this machine in the evenings, so no job may
  need an unbroken day. Any training job checkpoints every few minutes and resumes from its
  checkpoint; any long reading writes rows as it goes and can be re-run to fill in the rest.
- **Prove the wiring before trusting a reading.** Every arm is run once with the piece under
  test disabled, and the number must move. A store that is not consulted, a prompt the model
  never saw, and a thinking mode left on have each produced a wrong reading in this project's
  past. Check the system prompt, the sampling settings and the thinking switch in the reading
  itself, not in your head.
- **Size a run to its question.** Before a run, name the comparison it must settle and the
  smallest n at which the expected gap is about 2.5 standard errors; run the arm whose result
  changes the next step first; stop an arm once it is clearly above or below its reference
  (`--reference` in the search script). Smoke on 8, read from 100, and 500 only when two arms
  are close.
- **A reading is a JSON file under `readings/`** with the command, the git SHA and the model
  that produced it, committed with the change it measures. Every reading carries a floor (the
  model given nothing) and a ceiling (the model given the whole relevant text) beside the arm.
- **Keep the plan short.** When a step is done, strike it. When a step changes, rewrite it. The
  doc is read whole at the start of every session, so it must stay short enough for that.
- **Push whenever.** There is no CI yet; add it when there is something worth running on it.
- **Toolchain:** Python 3.12 in `.venv` via `uv sync`, torch from the cu126 index. See the
  comment in `pyproject.toml` before touching that pin.
