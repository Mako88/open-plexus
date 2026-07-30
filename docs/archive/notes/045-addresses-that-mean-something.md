# 045 — Addresses that mean something

**IN PLAIN TERMS.** Right now every concept in the model has an address that is a
random number. Nothing is near anything else — "dog" and "wolf" are as unrelated
as "dog" and "7". This note works out how to fix that, and finds that the obvious
fix is one the project already has evidence against. What survives is a different
shape, and it happens to be the same shape that has already won twice for
unrelated reasons.

---

## Where this came from

John, 2026-07-29, describing what he wants a concept to be: *"there is a picture
of a dog, and there is a drawing of a dog, and there is the sound of a dog"* —
one concept, many surfaces. Separately, STATE.md item 0c has said for weeks that
every key is a random draw, so **the store has no notion of similarity at all**.

**These are the same problem.** A concept can only have one address, because the
address IS the token id; there is no mechanism by which a picture and a word
could arrive at the same place. Both wants are satisfied by one change — make the
address depend on what the thing means — and neither is satisfied without it.

It also reframes item 4's *"move off character level"*. Words-as-arbitrary-ids
have the same defect as characters, with less of it. **The change that matters is
arbitrary addresses to addresses that mean something**; leaving characters is a
prerequisite, because a unit has to be able to carry meaning before its address
can be derived from it.

## The obvious design, and why the record says no

Make the key vectors overlap: similar concepts get correlated keys, so a read at
one partially retrieves the other. `FamilyKeys` in `tests/test_keys.py` is
already a working prototype — a shared family vector plus a token-specific one.

**Note 035 argues directly against this, and it is our own note.** Zahn et al.
(arXiv:2601.15313) derive interference as `O(N·ρ)` in mean key cosine and report
collapse at `N=5` for `ρ > 0.6`; the recommended fix is hash-derived keys, which
is what we already use. Note 035's conclusion was blunt:

> Similarity does not belong in the key vector; it belongs in a compositional id
> or a sparse retrieval path.

The mechanism is not in dispute even if the paper's numbers are (rule 1: those
figures are unverified by us). `retrieval = M @ key` sums every stored value
weighted by key overlap. **Deliberate overlap is deliberate interference.** The
store's whole capacity argument rests on keys being near-orthogonal — g10-09
measured off-diagonal overlap at 0.0005 mean against 0.2522 self.

So the direct route buys similarity by spending capacity, and capacity is already
the measured wall (decision 133). That is the wrong trade to make blind.

## The design that survives: keep the keys, add an index

Split the two jobs the address is currently being asked to do at once.

    WHAT IT IS      concept id     hash-derived key, orthogonal, unchanged
    WHAT IT MEANS   content vector a separate space where similar things are near

- **The store stays exactly as it is.** Keyed by concept id, keys near-orthogonal,
  capacity untouched. Nothing measured about it is invalidated.
- **A new structure — the index — maps a content vector to concept ids.** Nearest
  neighbours in meaning-space.
- **A read becomes two steps:** content vector to candidate concept ids, then an
  ordinary exact keyed read for each candidate.

**That second step is a hard commitment to a token id, and this is the third time
that design has come out on top.** Decision 123 chose it for accuracy — a branch
that commits can be scored, a blur cannot. Note 044 found it is the only form the
model has that can be routed across nodes at all. Now it is what lets similarity
exist without costing capacity. Three unrelated arguments, one design; that is
the strongest signal available here, because none of the three was chosen with
the others in mind.

**It also dissolves the tension note 044 could not resolve.** That note worried
that content-derived keys pull against the hash ring, which spreads concepts for
balance and therefore separates the similar ones. With the split there is no
conflict: **the store keeps hash placement, and only the INDEX needs
locality-sensitive placement.** Two structures, two placement rules, each
appropriate. Locality-sensitive hashing (Indyk & Motwani, 1998) is the standard
answer for the second and is unread here.

## Where content vectors come from, without a backward pass

**Co-occurrence.** A token's content vector is the running sum of the (random,
orthogonal) vectors of the tokens it appears near, normalised. Two tokens
appearing in similar company end up pointing the same way.

This is distributional semantics — the idea behind word2vec and GloVe — but it
does not need their machinery. **It is one accumulation per observed pair**, which
is the same local, online, no-gradient operation the store already performs. No
barrier, no backward pass, no global statistic. C1 holds without argument.

**And it is what makes a second modality possible later.** A picture's content
vector would be built the same way, from what it co-occurs with. Nothing about
the mechanism is textual.

## On multimodality, which is downstream and should stay there

The standard answer is CLIP — millions of curated image-caption pairs, a
contrastive objective, backpropagation. **All three are ruled out here**, so
copying it is not available.

The escape route is the one John independently proposed for whale song: train
each modality separately, then find the transform that lines the two spaces up.
Cross-lingual embedding alignment (Mikolov et al. 2013; Conneau et al. 2017)
recovers a working dictionary from monolingual data alone, because both spaces
end up with similar internal geometry. **It needs no pairs**, which is exactly the
constraint that rules CLIP out.

> **Its failure mode is the thing to design against.** If the two spaces do NOT
> share structure, the alignment still returns a confident-looking mapping — of
> nothing. Same risk as the whale case. That is a reason to build the check
> before the mechanism, not a reason to avoid it.

**None of this is next.** It is recorded so that the addressing work is done in a
form a second modality could use, not so that a second modality gets built.

## What decides it

**The instrument already exists.** g10-09 measured the property and its baseline:

    query condition                    store    cache
    exact                              1.000    1.000
    WRONG TOKEN (a different id)       0.090    0.015

**0.090 is the number that must move.** It is what "similarity generalisation"
is worth today, on random addresses: six times chance, on garbage input. If a
content index is worth anything, asking about a token whose facts were never
stored — but which is similar to one that was — must beat it clearly.

Pre-registering the shape, before anything is built:

- **P1.** With a content index, a concept whose facts were never stored is
  answered above 0.090, and above whatever `FamilyKeys` with a random assignment
  of families achieves. **The second control is the load-bearing one**: an index
  built from co-occurrence must beat an index built from noise, or it has learned
  nothing and is only exploiting that some answer is commoner than others.
- **P2.** Store capacity is UNCHANGED against the current model, within noise.
  This is the whole point of the split, and if capacity moves, the two structures
  are not as separate as this note claims.
- **P3.** The cost is read amplification: `b` keyed reads per answer instead of
  one, the same profile decision 123 costed for search at 3.2x for `b=4`. If it
  is worse than that, something is wrong with the design rather than the tuning.
- **P4.** Co-occurrence vectors carry recoverable structure at all — measurable
  directly and cheaply, exactly as g10-09 measured that hash keys carry none.
  **P4 gates the rest**: if there is no structure, an index over it cannot help
  and nothing below it is worth running.

### P4 is answered, and it passes — with a caveat that changes the design

Run before anything else, on Shakespeare, 180,430 words over a 2,000-word
vocabulary, window 4, width 256. Nearest neighbours by cosine:

    king      prince, crown, right, ay, fall
    father    son, brother, life, upon, mind
    he        his, it, that, him, this

The control is what makes it readable: **the same construction on a SHUFFLED
corpus returns only high-frequency words for every query** — `the`, `of`, `and`
— which is frequency and not meaning. So the structure is in the word order, and
one accumulation per observed pair recovers it without a gradient.

**The caveat is large enough to be part of the design rather than a footnote.**
Mean off-diagonal cosine is **0.50**, against 0.0005 for hash keys. Everything
resembles everything, because every word co-occurs with `the` and `and`. Ranking
still works — relative order survives a constant offset — but a 0.50 floor would
wreck any locality-sensitive placement, which depends on distance meaning
something in absolute terms.

**And the standard fixes are not free.** Down-weighting context tokens by
`1/sqrt(frequency)` drops the mean to 0.22 and sharpens some queries
dramatically — `king` goes to `richard, edward, henry`, which are the kings in
the plays — while destroying others: `father` and `love` collapse into function
words. It over-corrects for rare tokens. PPMI and subsampling are the usual
answers and neither has been tried here.

> **So the weighting is a real parameter with a large effect, not a detail to
> pick a default for.** It has to be measured, and P1 cannot be scored against a
> single arbitrary choice of it. That is a change to the plan: the sweep needs a
> weighting axis.

## What this note does not do

Nothing is built and nothing is measured. This is a design and a set of
predictions, and its main content is a REFUSAL: the direct route — similarity in
the key vector — is rejected on evidence this project already recorded, not on
taste. If P4 fails, the whole note is wrong and cheaply so.
