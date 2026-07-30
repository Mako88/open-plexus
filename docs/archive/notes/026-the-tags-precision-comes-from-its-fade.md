# 026 — There is a precision ceiling, and the tag reaches it with its fade

**Status:** four no-training controls, minutes of compute, no sweep. Every number
here is a count of what a gate keeps, scored against `position_kinds()`.
**Changes:** what [023](023-two-signals-and-only-one-of-them-is-about-value.md)
and [025](025-what-an-offline-phase-would-have-to-do.md) said the tag's
limitation is, and how much of g9-06's headline belongs to the mechanism.

---

## IN PLAIN TERMS

The filter keeps about thirty things for every one that turns out to matter. That
sounds like a filter doing badly. It is not: on this task only one item in six is
ever asked about, and nothing a device can see distinguishes the one from the
other five until much later. **So a perfect filter would still keep six things
per useful one** — and the filter is already most of the way to that limit.

Which means the remaining room is not in "spot the important item better". It is
in "work out which of the six". And the only clue for that is *how close it
happened to the signal*, which is the older, cruder mechanism this one was
supposed to replace.

The uncomfortable part is that the newer mechanism appears to be getting its
result from exactly that clue, wearing a different name.

---

## The ceiling

`reward_recall` presents 24 bindings and rewards 4, so **one binding in six is
rewarded**, and g9-04 established nothing local separates them — the generator
picks rewarded cues with `rng.sample(cues, n_rewarded)` out of the same alphabet
as the filler.

A gate that identified bindings *perfectly* and knew nothing else would therefore
keep six writes per useful one: **precision 16.7%**. That is not a property of
any mechanism. It is a property of the task, and it bounds every gate that ranks
on binding-ness.

## Where each mechanism sits against it

Counted over 8 sequences, 32 rewarded bindings available, no training:

| delay | arm | kept | bindings kept | recall | precision | of ceiling |
|---|---|---:|---:|---:|---:|---:|
| 8 | tag 8 / 0.95 | 230 | 33 | 84% | 11.7% | **70%** |
| 8 | tag 32 / 0.95 | 941 | 46 | 100% | 3.4% | 20% |
| 8 | window 8 | 254 | 32 | 100% | 12.6% | **76%** |
| 8 | window 32 | 968 | 32 | 100% | 3.3% | 20% |
| 20 | tag 8 / 0.95 | 230 | 14 | 25% | 3.5% | 21% |
| 20 | tag 32 / 0.95 | 965 | 38 | 100% | 3.3% | 20% |
| 20 | window 8 | 256 | 0 | 0% | 0.0% | 0% |
| 20 | window 32 | 992 | 32 | 100% | 3.2% | 19% |

**Three things, and the third is the one that matters.**

**1. The tag at its best capacity is at 70% of the ceiling.** Binding-detection is
close to exhausted. Improving the *signal* — which is what `tag_relative` did and
what the inverted ranking does — can win at most another 30% of a 16.7% ceiling.
That is a small prize and it is why those improvements only ever paid where
capacity was starved.

**2. A matched window is at 76% — slightly AHEAD of the tag.** The mechanism that
"only reaches, never selects" (g9-04, recency at AUC 0.479 for binding-vs-filler)
is the more precise of the two whenever its reach is right.

**3. The tag keeps 33 bindings at delay 8 and 27 of them are REWARDED.** Nothing
local can predict reward. So the tag is not picking rewarded bindings out of
bindings — it is picking bindings *near the reward*, because **the fade is a soft
window anchored at the capture**. A mark's rank decays with age and capture
happens at the reward, so what survives is what was recent when the reward
arrived.

## What that costs the headline

g9-06's result — a gate that does not have to be told the delay — is real as a
measurement and its explanation was wrong. The flatness is measured at `slots`
32, and at `slots` 32 recall is 100% at every delay **because the pool is large
enough to keep essentially everything**: precision 3.4%, twenty percent of the
ceiling, the worst of any setting here.

So the tag buys delay-independence by admitting so much that the delay stops
mattering. At `slots` 8, where it is precise, recall falls from 84% at delay 8 to
25% at delay 20 — a cliff of the window's own shape, in a softer form.

**The honest statement: the tag is a soft window with a capacity bound.** Its
fade supplies the reach, its capacity supplies the bound, and g9-04's inverted
signal supplies about 4.5x enrichment of bindings over the base rate — real, and
worth roughly what the measurements have shown it worth, which is a lot when the
pool is starved and nothing when it is not.

## What this does NOT say

- **It does not refute g9-06 or g9-09.** Those recoveries were measured with
  training and stand. What changes is the account of *why*.
- **It is not a sweep.** Everything here is a count over 8 sequences at one seed
  with an untrained readout. The relationship between what a gate keeps and what
  a trained model then scores is exactly what g9-05 showed can surprise —
  the un-faded tag captured 9% and scored -0.20.
- **It does not settle delay 20.** Nothing reaches more than 21% of the ceiling
  there. Whether that is the real limit or an artefact of the settings tried has
  not been measured.

## What to build next, and what not to

**Not replay, and not a better binding signal.** Note 025 has already been
corrected once for aiming at write-time ignorance; this narrows it further. A
better binding-detector competes for at most 30% of a 16.7% ceiling.

**The open question is whether anything can identify WHICH of the six bindings
without being told the delay.** The window does it by being told. The fade does
it by guessing a time constant. Nothing yet does it from the data.

If nothing can, then `reward_recall`'s ceiling for any delay-agnostic gate is
about 20% of the oracle's advantage — which is approximately what the tag scores
— and that is a result about the task rather than about any mechanism. **That is
the most important open question on this line and it is cheap to attack: it needs
a probe, not a sweep.**

---

*Related: [023 — two signals](023-two-signals-and-only-one-of-them-is-about-value.md),
[025 — what an offline phase would have to do](025-what-an-offline-phase-would-have-to-do.md),
[020 — the capacity equation](020-the-capacity-equation-checked.md).*
