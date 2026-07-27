# 027 — `reward_recall` leaks the answer through its layout

**Status:** measured by reading the generator's output, no training, seconds.
**Affects:** the interpretation of g9-02 through g9-10 — not their numbers.
**Needs a decision that is not mine:** whether to fix the generator, which would
invalidate the comparison set for nine sweeps.

---

## IN PLAIN TERMS

The test was built so that a device cannot tell, as information arrives, which
items will later be asked about. That part works.

But the items are laid out at *evenly spaced intervals*, and the "this mattered"
signal always arrives a short fixed distance after the item it refers to — much
shorter than the spacing. So the item nearest to each signal is always the right
one. Always. Not usually.

That means a very simple rule solves the test perfectly: **keep the most recent
item before each signal.** No mechanism this project has built uses that rule,
which is why the test looked hard. It is not the mechanisms that were failing a
hard test; it is a test that was easier than it looked, being failed in
interesting ways.

---

## The measurement

Offsets from each binding to the next reward token, 40 sequences, delay 8:

    rewarded bindings     offset 7, 100% of them, ONE distinct offset
    unrewarded bindings   offsets 38, 69, 100, 131, 162, 193, 224, 255, ...

and directly:

    the nearest binding before a reward IS the rewarded one:  160/160 = 100%

Identical structure at delays 1 and 20 — modal offset `delay - 1`, every rewarded
binding on it, no unrewarded binding within 31 steps of it.

## Why, from the generator

`reward_recall.generate` lays the body out with a **constant** gap:

    gap = max(0, (body_len - config.n_pairs * 2) // config.n_pairs)

So bindings sit on a lattice — spacing 31 at the settings every g9 sweep uses.
The reward for a rewarded cue is placed `delay` steps after that cue, and
`delay` is 1 to 20 in every sweep run. **A distance of at most 20 cannot reach
past a spacing of 31**, so the nearest preceding binding is always the rewarded
one, by construction rather than by chance.

The `while kinds[place] != "filler"` nudge that moves a reward off a binding does
not change this; it moves the reward *later*, never past another binding.

## What it does and does not invalidate

**It does not invalidate any measurement.** Every recovery figure in g9-02 to
g9-10 is a correct measurement of what those arms scored on this task.

**It does invalidate the DIFFICULTY the task was believed to pose.** Note 017
built `reward_recall` so the storage decision could not wait for the marker, and
that is still true — nothing local predicts *reward* at write time. But the task
also affords a local rule that does not need to predict anything: *detect
bindings, keep the most recent one before each reward.* That rule needs only
binding-detection and recency, both of which are local and both of which this
project has measured.

**So the delay axis is not measuring what it was built to measure.** g9-03's
diagonal cliff is real, and it is a fact about a window counting in STEPS while
the answer lives at a fixed number of BINDINGS. A gate counting in bindings, and
taking the most recent, would not have a cliff at all.

**And note 026's ceiling is a ceiling on the wrong quantity.** It bounds gates
that rank on binding-ness alone at 16.7% precision, one binding in six. A gate
that also uses "most recent" reaches 100% precision with one write per capture,
because on this layout the two together are exact.

## What this is an instance of

The same class as the MQAR filler bug, and found the same way. That one was
caught in the first sequence ever generated, by printing it and reading it: the
filler drew from the whole key range, so a filler token could be byte-identical
to a query token, and the benchmark was creating *impossibility* while appearing
to create difficulty. This is the mirror image — apparent difficulty that is
actually a solved problem — and it survived nine sweeps because nobody looked at
the *spacing* of what the generator emitted, only at what it contained.

`tests/test_reward_recall.py` checks the sequence contains what it should. It
does not check that the layout withholds what it should.

## The fix I first proposed WOULD NOT HAVE WORKED, and it was measured

The obvious fix is to randomise the gap between bindings so the lattice breaks:

    gap = rng.randrange(low, high)      # per binding, not once

**That does not fix it.** Built as a local variant and measured over 40
sequences per setting, with `jitter` as the fraction by which the gap varies:

| jitter | nearest-binding rule | rewarded modal offset | unrewarded at that offset |
|---:|---|---|---:|
| 0.0 (shipped) | 160/160 = **100%** | 7, 100% of them | **0 of 651** |
| 0.25 | 159/159 = 100% | 7, 100% | 0 of 650 |
| 0.5 | 158/158 = 100% | 7, 100% | 0 of 648 |
| 0.9 | 140/156 = 90% | 7, 92% | 0 of 644 |

Even with the gap varying between roughly 3 and 59, **no unrewarded binding
lands at offset 7**, and the nearest-binding rule stays exact until the jitter is
extreme.

**Because the lattice was never the discriminator.** The reward is placed at
`cue_position + delay` and nudged to the next filler slot, so the offset from a
rewarded binding to its own reward is `delay - 1` — a CONSTANT — whatever the
spacing does. Randomising the gap changes how far apart the *other* bindings sit;
it does not stop the rewarded one sitting at a known distance.

## The fix that would work

**Randomise the delay per rewarded pair**, not per task:

    reward_due[position + rng.randint(low, high)] = ...

Then the offset from a rewarded binding to its reward is a distribution rather
than a constant, and no fixed offset identifies it. `delay` stops being a task
parameter and becomes a task *property* — which also means the delay axis every
g9 sweep is built on would have to be re-thought, not just re-run.

That is a bigger change than one line and a bigger decision than re-baselining.

**Still not mine to make**, and now for a better reason: the cheap fix is
useless, and the real one changes what the task is.

## MEASURED: the leak is real and this project cannot exploit it

The arm proposed below was built (`tag_newest`: of what the tag marked, protect
only the most recent, excluding the write made at the reward itself). Recall and
precision of rewarded bindings, 8 sequences, no training:

| delay | arm | kept | recall | precision |
|---|---|---:|---:|---:|
| 1 | tag 32/0.95 | 961 | 100% | 3.3% |
| 1 | **newest 1 of 32** | 32 | **100%** | **100%** |
| 8 | newest 1 of 32 | 32 | 0% | 0% |
| 20 | newest 1 of 32 | 32 | 0% | 0% |

**At delay 1 the rule is exact** — one write kept per capture and it is always
the rewarded binding, which is the leak in its purest form. **At delay 8 and 20
it finds nothing at all.**

The reason is the gap between "most recent MARK" and "most recent BINDING". At
delay 8 there are seven filler writes between the binding and its reward, and the
tag marks filler too — g9-04's signal gives about 4.5x enrichment over the base
rate, nowhere near enough for the last mark to be the binding. At delay 1 there
are no intervening writes, so the distinction vanishes.

**So the leak is real and inert.** Exploiting it needs binding-detection far
better than anything this project has, and the delay-1 column is the only place
it bites — where a window of reach 1 also gets it, and where g9-02 already
described the gate as trivial for exactly that reason.

**That downgrades the fix from urgent to correct.** The g9 numbers at delay 4, 8
and 20 are not inflated by the leak, because nothing measured can reach it. The
generator should still be fixed — a task that affords a perfect local rule is not
posing what it claims — but it does not invalidate the comparison set, and
re-baselining nine sweeps for it would buy accuracy in the write-up rather than
in the numbers.

**What remains true and uncomfortable:** the difficulty at delay 1 is not real,
and every "delay 1" cell in g9-02 through g9-10 is measuring a task with a
trivial solution available. Reading those columns as evidence about short delays
overstates the case.

That arm is cheap and it is the honest next measurement on this line.

---

*Related: [017 — a task with something at stake](017-a-task-with-something-at-stake.md),
[026 — the precision ceiling](026-the-tags-precision-comes-from-its-fade.md),
[008 — the task/objective mismatch](008-the-task-objective-mismatch.md).*
