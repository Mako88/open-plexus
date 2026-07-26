# 18. The one word that decided it

The biggest result so far, and the closest call.

**Two questions answered at once, and a configuration setting turned out to be
the difference between our central idea looking confirmed and looking dead.**

---

## What we asked

**Question one:** is our memory test *learnable*? We knew it was answerable — a
cheat with the answers gets 100%. We knew it was reachable — a hand-written
lookup gets 100%. But **nothing had ever learned it.** If nothing could, all the
room we'd measured would be a mirage.

**Question two, the one John chose:** our learning method works by predicting
what comes next. But 83% of the sequence is meaningless padding. Does the method
find the part that matters, or get lost in the padding?

One experiment answers both. Train the same model two ways:

- **Told the answers** — the model is shown exactly which predictions count.
  This is cheating, and it's the ceiling.
- **Told nothing** — the model just predicts everything and has to work out for
  itself what matters. This is the real method.

---

## The result

Five separate training runs each, from different random starting points.

| how it was trained | padding | score on the task |
|---|---|---|
| told the answers | random | **1.000** |
| **told nothing** | **random** | **1.000** |
| told nothing | patterned | **0.009** |

The bar to beat was 0.344. Pure guessing gets 0.125.

**Row one:** the task is learnable. A model trained from scratch on our own test
reaches a perfect score. Everything downstream is now interpretable — a future
failure means the *approach* failed, not that the test was impossible.

**Row two — the answer to John's question:** a model told *nothing at all* about
which parts matter, just predicting the whole sequence, gets a **perfect score.
Identical to cheating.** Five out of five runs.

**The mismatch we were worried about doesn't exist.** All three of the escape
routes we'd carefully written up are unnecessary. The project carries on with
the test it already has.

**Row three is the interesting one.** Same model, same method, same everything —
except the padding is a predictable pattern instead of random noise. **0.009.**
Not just worse. An order of magnitude *below* random guessing.

---

## The near miss

Here's the part worth sitting with.

Four days ago we decided to use **patterned** padding. The reasoning felt
airtight: our method learns by predicting things, random noise is unpredictable,
so random padding gives the learner nothing to work with.

That's row three. **0.009.**

If we'd run this experiment then, here's what we'd have written:

> *The supervised version reaches a perfect score, so the model works and the
> task is fine. The self-supervised version scores essentially zero. Therefore
> the self-supervised objective doesn't work.*

Every sentence of that is supported by the numbers. **And the conclusion is
completely wrong.** We'd have abandoned the central idea of the project because
of a padding setting.

What caught it wasn't a test — no test could have. It was working out *where a
learner's improvement can actually come from*, using numbers that had been
sitting in a previous experiment for two rounds. Random padding can't be
predicted **by anything**, so there's no improvement available there and the
learner ignores it. Patterned padding offers easy improvement across 83% of the
sequence, so that's where the learner goes.

**One word in a config file. `random` instead of `structured`.**

---

## And a trap worth knowing about

This one generalises well beyond us.

Look at how the training loss behaved — the number everyone watches to see if
training is going well:

| padding | loss went | score on the task |
|---|---|---|
| random | 3.65 → 3.32 *(barely moved)* | **1.000** |
| patterned | 3.05 → 1.82 *(plunged)* | **0.009** |

**The run that solved the task looks stuck. The run that failed completely looks
like it's going great.**

Both readings are exactly backwards, and the loss curve is the single most
common thing anyone monitors.

The reason: loss is an *average over every position*. With random padding, most
positions are unpredictable no matter what — so the average barely moves however
well the model does on the 4% that matter. With patterned padding, that same 83%
is easily improvable, so the average plunges while the task is ignored.

> **The task lived in 4% of the positions, and the headline metric averaged over
> all of them. A number that averages over everything cannot see something that
> lives in a corner.**

Score the thing you care about, at the places you care about.

---

## What this doesn't mean

Being careful, because this is a good result and good results are when
overclaiming happens.

**The model we used is not what we're proposing.** It's an attention model —
every position looks at every earlier position. That's the all-to-all pattern
this entire project exists to *avoid*. It was built to answer whether the
*objective* can work, and it has. Whether a **local** rule can do the same thing
is the actual project, and it's completely open.

**We handed the model a hint.** Its architecture has the "look back and report
what followed" shape built in, so it didn't have to discover that. Whether it
still works without the hint is the obvious next question.

**One setting.** Four pairs, one sequence length. We have difficulty dials and
haven't turned them against a trained model yet.

---

## Where we stand

Gate zero is **complete**. Our test is answerable, reachable, learnable, has a
measured floor, a mechanically-checked answerability guarantee, and 0.82 of
verified room in it.

And the learning method we chose survives its first real test: **given padding
that offers no false trail, it finds the task by itself.**

That's the foundation the rest of the project was waiting on.

---

*Next: nothing yet — this is the current edge. See the [index](README.md).*
