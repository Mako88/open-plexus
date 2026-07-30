# 058 — Real co-occurrence has no cliff

**Status:** measured, local probe. **P2 confirmed, P3 control fired.** This is a
result about the instrument, not about the mechanism, and it reaches back over
decisions 167 and 171 and notes 056 and 057.

**Why it exists:** the cliff rule works because on `families.py` a token's ranked
neighbour similarities are sharply bimodal — siblings at 0.947–0.970, strangers at
0.438–0.585 — so an argmax over gaps lands on the group boundary every time. Notes
056 and 057 measured what happens when the grouping is made *hard*. **Neither asked
whether a real grouping has that shape at all.**

---

## IN PLAIN TERMS

The model decides which things belong together by finding a sharp drop in how similar
its neighbours are: the family sits above the drop, everything else below. On the
synthetic task that drop is a cliff — a fall of 0.45 where the steps inside the family
are 0.01.

**On real text there is no cliff.** Every neighbour of every word sits at about the
same similarity, 0.96, and the largest fall anywhere in the list is 0.015. There is
nothing to cut at, so the rule is cutting noise.

This does not mean the model is broken. It means **the thing that made the rule work
was a property of the task, not of language**, and any claim that rests on the cliff
has to say which of those two it is standing on.

---

## The measurement

No labels and no download: the *shape* of the profile is the whole question, and Tiny
Shakespeare is already in `data/`. Word-level, top 600 by frequency, `ContentIndex`
fitted in 2,000-token chunks.

    source                gap ratio    largest gap
    families (synthetic)      26.84          0.424
    real text                  9.01          0.015
    SHUFFLED text              7.56          0.002

    ranked neighbour cosines, real tokens
      token   5:  +0.967 +0.964 +0.964 +0.963 +0.963 +0.963 +0.962 +0.962
      token  50:  +0.976 +0.973 +0.972 +0.972 +0.971 +0.971 +0.970 +0.970
      token 200:  +0.979 +0.976 +0.976 +0.976 +0.975 +0.974 +0.974 +0.973

    the same view on one families entity
      +0.969 +0.961 +0.947 +0.500 +0.471 +0.467 +0.452 +0.438

**P2 CONFIRMED.** Real text shows no cliff comparable to the synthetic task's. The
largest gap is **28× smaller**.

**P3 CONTROL FIRED, and it matters.** Shuffled text gives 0.002 against real text's
0.015, so real co-occurrence *does* carry structure and this is not merely measuring
the index's own geometry — which is the confound decision 141 caught when a grouping
built from shuffled text did as well as one built from real text. **Real text carries
about seven times more structure than chance and about one twenty-eighth of what the
task provides.**

> **Quote the ABSOLUTE gap, not the ratio.** The gap ratio is scale-free and reads
> 9.01 against 7.56 — a difference that sounds modest and hides the fact that one is
> 0.015 and the other 0.002. The ratio was the statistic this probe was designed
> around and it is the wrong one; the note keeps both so the mistake is visible.

## What it does and does not license

**It does not refute the cliff rule as implemented.** At the purity `families.py` is
calibrated for, the rule is exact and asks for no constant, and every number in
decisions 167/171 and notes 056/057 stands *under that condition* — which was named
at the time rather than being rescued now.

**It does mean the condition is not met by real word co-occurrence**, so the
crossover in note 056 has a second clause. The gap rule needs purity ≳ 0.99 **and a
bimodal profile**, and real text as measured here supplies neither.

**And the wider implication is about `ContentIndex`, not the cliff.** Every token's
top-eight neighbours sit within 0.02 of each other at ~0.96. A representation where
everything is similar to everything carries little discriminative structure, and
`families.py` may be flattering it: each entity there appears beside two or three
*private* attributes and nothing else, which is close to the easiest possible
co-occurrence problem.

## Three confounds, named rather than resolved

Any of these could be most of the effect, and none is ruled out here.

1. **The slice is function words.** Top-600-by-frequency in Shakespeare is dominated
   by words that genuinely do co-occur with everything. A mid-frequency content-word
   slice is the obvious next cut.
2. **Centring.** `tools/mutate.py` carries `content-vectors-are-not-centred`, so
   centring exists and is load-bearing somewhere. Cosines uniformly at 0.96 is what
   an uncentred, all-positive representation looks like, and the probe did not check
   whether centring is active on this path.
3. **Chunking and window.** 2,000-token chunks were chosen so the window would not
   straddle the corpus, not because anything measured that size.

**So the honest headline is narrow:** on this slice, with this fitting, real
co-occurrence has no cliff and carries 28× less similarity spread than the task the
rule was built on. Whether that is language or is this pipeline is the next question,
and confound 2 is the cheapest to settle.

## What this changes about sequencing

The standing gap has been that every instrument is self-designed — *"until CLUTRR
runs, this project is grading its own homework."* **This is the first measurement that
puts a number on what that costs**, and it is large. It moves an external benchmark
from a completeness item to the thing that decides whether the answer line means
anything, because the answer's enumeration provably depends on a profile shape that
one real dataset does not have.
