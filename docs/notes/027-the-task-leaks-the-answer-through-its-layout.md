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

## The fix, which is one line and is not mine to make

Randomise the gap between bindings so the lattice is broken:

    gap = rng.randrange(low, high)      # per binding, not once

With variable spacing, a binding can fall within `delay` of a reward that is not
its own, and "most recent binding" stops being exact. The delay axis would then
measure what it was built to measure.

**It would also invalidate the comparison set for nine sweeps.** Every g9 number
would need re-running to stay comparable, and CLAUDE.md rule 12 is explicit that
changing a default invalidates the comparison set and that a known-better setting
can be worth deliberately *not* adopting until there is time to re-baseline.

So this note records the defect and proposes the fix. **It does not apply it.**

## What should happen before anything is re-run

1. **Measure how much the leak is worth.** Add a `nearest-binding` arm — detect
   bindings by the existing signal, keep the most recent before each reward — and
   see what it scores. If it approaches the oracle, the leak is the whole story
   and the fix is urgent. If it does not, binding-detection is too weak to
   exploit the leak and the fix is merely correct.
2. **Then decide about re-baselining**, with that number in hand.

That arm is cheap and it is the honest next measurement on this line.

---

*Related: [017 — a task with something at stake](017-a-task-with-something-at-stake.md),
[026 — the precision ceiling](026-the-tags-precision-comes-from-its-fade.md),
[008 — the task/objective mismatch](008-the-task-objective-mismatch.md).*
