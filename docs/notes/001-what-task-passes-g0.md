# Note 001 — What task passes G0?

Answers open question 1 in [GOALS.md §8](../../GOALS.md). Produces a
recommendation and a protocol, **not a result.** Nothing here has been measured.

---

## IN PLAIN TERMS

Before we can test whether our idea learns, we need a test that can *tell*. The
last version of this project spent a year on a test that could not — the
untrained version already scored so well that there was almost no room left to
show an improvement.

This note works out how to avoid that. The main finding is that the fix is not
"pick a harder test." It is **pick a test that is hard in a direction the
untrained version has no answer for**, which is a different and more specific
thing. It also argues for picking a *family* of tests with a difficulty dial
rather than a single test, so we get a curve instead of a verdict.

A recommendation comes out of it. Nothing has been run.

---

## 1. The question

G0 requires a task where a random frozen substrate scores poorly, a strong
non-local reference scores well, and **the gap between them is large.** What
task has that property, and how is the trap that caught the predecessor avoided
rather than merely noted?

## 2. Why the predecessor's task failed — diagnosed, not just recorded

The recorded failure is "the benchmark left the learning rule almost nothing to
do": a frozen random column scored 0.802, total headroom to a strong non-local
model was ~0.19, and non-learning mechanisms took ~40% of it.

That is the symptom. **The cause is that the task played to the substrate's
strengths.** A random recurrent network with a trained linear readout is a
reservoir, and reservoirs are specifically, well-documentedly good at
short-horizon temporal mixing. The benchmark asked for short-horizon temporal
mixing. It was, in effect, a test of the thing the untrained system was already
built to do.

This matters because it changes the corrective action. "Make the task harder" is
the wrong lesson — a task can be made arbitrarily hard while still being hard in
a direction the reservoir handles gracefully, and the gap will not open up. The
right lesson is:

> **Choose a task whose difficulty lies in a direction the random substrate has
> no answer for.**

That requires knowing what a random substrate is and is not good at.

## 3. What a random reservoir is good at, and what it is not

**Good at:**

- Short-horizon temporal mixing — combining recent inputs nonlinearly.
- Random-feature expansion — projecting input into a high-dimensional space
  where a linear readout can separate things it could not separate before.
- Holding recent history in a fading, undifferentiated way.

**Predicted to be bad at** — and *predicted* is doing real work in that sentence,
see §7:

- Dependencies longer than its memory decay.
- **Selective retention.** A reservoir's memory is indiscriminate: everything
  decays at the same rate whether it mattered or not. It cannot choose to keep
  one thing and drop another.
- Content-addressed retrieval — "what was paired with X, seen a while ago?"
- Compositional recombination of parts seen separately.

**The second is the most interesting one for this project.** A reservoir's
capacity is finite and it spends that capacity uniformly on everything it has
recently seen, including noise. *Deciding what is worth keeping* is precisely
the kind of thing a learning rule could do and a random substrate structurally
cannot. That makes selective retention a promising axis on which to make a task
hard — the difficulty is aimed squarely at the gap we want to open.

## 4. Three properties a G0 task needs

### P1 — Its difficulty lies where the random substrate has no answer

Per §2 and §3. Long-range, selective, content-addressed, or compositional —
not "more of the same, faster."

### P2 — The self-supervised objective and the task metric are the same quantity

[GOALS.md §5](../../GOALS.md) names a predictive / self-supervised local
objective as the leading credit-assignment candidate: each unit predicts its own
next input.

If the task is scored by something *other* than prediction quality, a null
result has two explanations that cannot be separated — the objective does not
learn, or the objective learns fine but what it learns does not transfer to the
metric. That ambiguity is expensive and avoidable.

**Choosing a task whose native metric is next-symbol prediction collapses it.**
The thing being optimised and the thing being scored become the same number.

> **CORRECTED by [g1-01](../../experiments/sweeps/g1-01-predictability.txt).**
> The generator built from this note **does not satisfy P2**, and the claim above
> was made without checking that it would. At a query position the target is the
> paired value — but that value is *never emitted into the token stream*; the
> next token after a query is filler. So "predict your next input" and "answer
> the query" are different questions in this generator, and the objective and
> the metric are **not** one quantity.
>
> The literature's framing is autoregressive: the query is *followed by its
> answer* in the sequence, so next-token prediction at a query position **is**
> the task. Ours is a classification framing — targets as labels beside the
> stream rather than tokens within it. Both are legitimate; only the first
> satisfies P2, and P2 is the reason this task was chosen.
>
> **Fix:** an autoregressive mode that emits the value token after each query.
> Until then the predictability gate has not actually been asked.

It also serves the secondary goal directly: next-token prediction is what a
language model does, so a result here is a result about that objective family
rather than an analogy to it.

### P3 — A difficulty dial

**G0 as written contains an unresolved tension, and this is where it surfaces.**
G0 demands a large gap. But a gap that *no* local rule could ever close is
equally useless: if a task genuinely requires a global backward pass, then a
local null tells us nothing about locality — only that the task was too hard. We
would have swapped a test that could not show success for one that could not
show failure informatively.

The frontier cannot be known in advance. So:

> **Do not pick a task. Pick a task family with a difficulty parameter, and let
> G0's output be a curve rather than a verdict.**

Consequences:

- G0's deliverable is *(base rate, reservoir score, reference score)* as a
  function of difficulty — not three numbers.
- The operating point is chosen where the gap is widest, and recorded as a
  decision with the run behind it.
- Difficulty can be re-tuned later without changing instruments. Moving it
  still invalidates the comparison set (rule 12), so it stays a deliberate
  re-baseline rather than a quiet edit.

## 5. Candidate task families

> **CORRECTED by [note 006](006-verifying-the-reservoir-claims.md) §3.** The
> row below specifies **a single query**, and that variant is *already solved*
> by models much weaker than attention — building it would have produced a
> benchmark everything passes, which is the exact G0 failure this note exists to
> prevent. The discriminating variant is **multi-query associative recall
> (MQAR)**: `K` pairs embedded in a sequence, with *all `K`* queried. **`K` is
> not one dial among four — it is what makes the task discriminating at all.**
> The recommendation in this section stands with that amendment.

| family | what it demands | reservoir *predicted* to | strong reference | dials |
|---|---|---|---|---|
| **Associative recall** — pairs appear, then one is queried | content-addressed retrieval | fail | transformers and LSTMs are documented to solve it | vocabulary size, sequence length, query distance, number of pairs |
| **Dyck / matched brackets** | stack-like nesting | fail beyond shallow depth | known solvable | nesting depth, sequence length, bracket types |
| **Copy / delayed recall** | verbatim retention across a gap | fail beyond decay | known solvable | gap length, payload size |
| **Sparse parity over a window** | attend to k of n positions, ignore the rest | fail | known solvable | k, window size, noise fraction |

**Recommendation: associative recall**, on three grounds.

1. It satisfies **P2 natively** — the task *is* next-symbol prediction, so the
   objective and the metric are one quantity with no translation layer.
2. It satisfies **P1** on the most interesting axis: it requires keeping a
   specific pair while discarding everything else, which is selective retention
   (§3), not fading mixing.
3. It has **four independent dials** (P3), so the difficulty curve is
   explorable in more than one direction — and if the gap fails to open on one
   dial, the others are still available before the family is abandoned.

**Sparse parity is the recommended second**, because its dial includes an
explicit noise fraction, which targets selective retention even more directly.

## 6. What G0 actually runs

In order. Each step gates the next.

1. **The base rate.** What a constant predictor scores — always guess the most
   common answer. Reported alongside every other number, permanently. Without
   it, a weak positive is indistinguishable from a strong nothing.
2. **The random frozen substrate**, with a trained readout.

   > **CORRECTED by [g0-02](../../experiments/sweeps/g0-02-frozen-reservoir.txt).**
   > This step originally called the frozen substrate "the floor that matters".
   > On MQAR it is not: it scores **0.180**, *below* the 0.344 one-line-heuristic
   > floor and barely above the 0.125 base rate. **The bar is
   > `max(trivial_floor, frozen)`**, and here that is the heuristic. Reporting a
   > model against the frozen substrate alone would credit as progress something
   > a five-line function already beats.

   Whatever the numbers, this step needs a **connection control**: fit the same
   pipeline to decode information the state provably holds, such as the token
   currently being presented. A broken pipeline and a substrate that genuinely
   cannot do the task produce identical output, and the second is what we
   expect — which is exactly when nobody looks harder.
3. **A strong non-local reference**, trained with a global backward pass, on the
   same data budget. This establishes that the information is present and the
   task is learnable at all. A reference that also fails means the task is
   broken, not hard.
4. **Multi-seed throughout**, per rule 3. Effects visible at three seeds
   routinely vanish at twenty.
5. **Across the difficulty dial**, producing the curve from P3.

The reference model is a *measuring instrument*, not a component. Nothing about
using a transformer to establish headroom implies anything about the
architecture being built.

## 7. Predictions, written now

Recorded before any run so they cannot be retrofitted (rule 4). Every one of
these is an argument, not a measurement.

1. **A random frozen substrate scores at or near the base rate on associative
   recall** once query distance exceeds its memory decay — a much larger gap
   than the predecessor's 0.802.
2. **A strong non-local reference scores near ceiling** on the same
   configuration.
3. **The gap widens with query distance**, then closes again at extreme distance
   as the reference itself degrades. The operating point is at the maximum.
4. **The noise fraction dial opens the gap faster than the length dial**, if
   §3's selective-retention argument is right. This is the sharpest prediction
   here, because the argument is the load-bearing one and this is what would
   refute it.

**What would refute the recommendation:** a random reservoir scoring well above
base rate on associative recall at long query distance. That would be genuinely
surprising, would disqualify the task family, and would be worth more than a
confirmation — it would mean §3's model of what reservoirs cannot do is wrong,
and that model is currently steering the whole task choice.

## 8. What this note does not settle

- **It does not choose the substrate.** A "random frozen substrate" is required
  as a *baseline* regardless of what gets built; it is not a commitment to build
  a reservoir.
- **It does not choose the credit-assignment scheme.** P2 assumes the predictive
  objective is the leading candidate, which [GOALS.md §5](../../GOALS.md) records
  as a hypothesis.
- **It does not show a local rule can close the gap.** That is G1, and it is the
  actual bet. G0 only establishes that there is a gap to close and an instrument
  that can see it.

## 9. Open sub-questions

1. **What data budget?** The predecessor found the same effect measured +0.074
   or +0.060 depending only on how much data the readout was fitted with. Budget
   is part of the measurement and must be fixed before the run, not chosen after.
2. **What form does the input take?** Associative recall is usually posed over
   discrete tokens; this project's substrate is likely to be event-based and
   continuous-time. The encoding is a design decision that could itself destroy
   the task, and it needs its own connection test (rule 6).
3. **Does the "strong reference" need to be strong, or just non-local?** A small
   LSTM is easier to trust and easier to run than a transformer. Cheaper, and
   probably sufficient to establish headroom.
