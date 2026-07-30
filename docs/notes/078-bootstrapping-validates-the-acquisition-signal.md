078 — Bootstrapping validates the acquisition signal, and the merge gate is MUTUALITY
====================================================================================

**Status:** measured, `EN_DE_15K_V2`, zero supervision. **Recorded as validation and then
stopped** — John: *"validate that an idea will meet our goal, and then continue to the next
hardest problem before continuing to refine."* This is not tuned and is not meant to be.

---

## IN PLAIN TERMS

Note 077 found that relating-the-same-way identifies concepts at 583x chance but only 3.9%
outright. **Feeding the confident answers back in as features raises that to 31%** — eight
times better — and it was still improving when I stopped.

**And the way to decide which answers to trust is not a confidence score.** Requiring that
two entities pick *each other* as their best match works; adding any threshold on top makes
it worse. The system's own agreement is the gate, not a number anyone tunes.

---

## The measurement

Seed on mutual nearest neighbours, add one feature per seed counting how often an entity
neighbours it, re-rank, repeat. `EN_DE_15K_V2`, 15,000 pairs, no seed alignments.

    round      hits@1    hits@10       MRR    seeds   seed precision
        0      0.0389     0.1565    0.0787        -                -
        1      0.0757     0.1879    0.1147    1,273            0.263
        3      0.1245     0.2479    0.1682    2,759            0.323
        6      0.2220     0.3224    0.2583    4,303            0.550
        9      0.2785     0.3886    0.3183    5,150            0.622
       12      0.2930     0.4023    0.3326    5,881            0.676
      best     0.3098     0.4230    0.3515                        -

**8.0x round 0, and not plateaued** — so 0.31 is a floor on what this mechanism reaches,
not a ceiling.

## The finding that matters architecturally: no confidence gate

    seed rule            best hits@1
    mutual NN only            0.3098
    mutual NN, sim >= 0.9     0.2334
    mutual NN, sim >= 0.98    0.0855

**Stricter is monotonically worse, and it does not even buy precision** — at round 1 the
strict floor's seeds are 0.249 correct against the ungated 0.263. Essentially identical.

**So similarity MAGNITUDE carries no information about correctness here; mutuality carries
all of it.** A high cosine means "these two are alike", which many wrong pairs also are. A
mutual best match means "neither has a better candidate", which is a statement about the
whole field rather than about one pair.

> **This answers the merge-posture question with a measurement rather than a preference.**
> The provisional scheme John approved supposed a threshold plus promotion; what the data
> says is simpler — require mutual agreement and require nothing else. The promotion happens
> anyway: **seed precision climbs 0.263 → 0.676 across rounds with no tuning at all**, because
> better features make better seeds make better features.

**And it tolerates being wrong.** Round 1 improves the result while 74% of its seeds are
incorrect. Wrong seeds contribute a noise feature; right ones contribute signal; signal wins.
That is a property worth knowing before choosing how cautious the real merge should be.

## What is NOT claimed

**Not a competitive alignment number.** Published methods use 3,000 seed links, attribute
triples and learned embeddings. This is counts and cosines with zero supervision, and the
point was whether the signal compounds, not whether it wins a leaderboard.

**Not that wrong FEATURES cost what wrong MERGES cost.** A wrong seed here adds a noise
coordinate. A wrong merge in `concepts.Merged` makes reads gather another entity's facts,
which is a different and larger error. Tolerance of the first does not license the second, and
this note does not measure the second.

**Not run on the hard setting.** `D_W` and `D_Y` share no relation vocabulary, so round 0
has nothing to compare and the loop has no starting point. Whether a vocabulary-free seed
(degree, triangle counts) gets it started is untried, and it is the case a real system faces
— two nodes that have never agreed on anything.

**And not wired into the model.** `Merged` exists, this says what could drive it, and nothing
joins them. The read-side fan-out a merged class costs has still never been measured.
