# 034 — lifting the bigram ceiling with a pair key

**Status:** implemented behind `context_keys`, off by default, with the ceiling
measured on both sides. **The bits-per-character question is not answered here**
and needs a sweep.

PREDICTIONS (pre-registered in `tests/test_context_keys.py` before running):

- P1 single-token binding cannot resolve a step that needs two tokens of context
- P2 the pair key resolves it
- P3 the retrieval becomes a *trigram* count vector, by the same argument that
  made it a bigram one
- P4 the price is capacity: more distinct keys, so the store fills sooner

COST: nothing dispatched. Three local probes, each seconds; 10 tests; 3 mutations.

MEASURED ON: a synthetic two-context sequence (P1, P2), uniform random tokens
and 4000 characters of Tiny Shakespeare (P3, P4).

---

## What note 033 left

`M += value(t) ⊗ key(t-1)` binds adjacent tokens, so a retrieval is the sum of
the values of everything that has followed this token — a bigram count table.
Measured at cosine 0.9455 against exactly that table at low load, falling to 0.88
as items superpose. **No trigram is ever written down, so none can be
represented.**

## The change is one line

    key = context_key(token(t-1), token(t))     instead of     key = Wk[token]

`previous_key` is then the key of `(t-2, t-1)` and the query at `t` is the key
of `(t-1, t)`, so the store becomes a trigram table. The write rule, the
retrieval, the readout and the delta rule are untouched.

The key is derived from `(seed, t-1, t)` and cached, never tabulated: a `vocab²`
table is 16 million rows at `vocab 4096`, and not holding it is the same
argument `derived_keys` rests on. A node still receives token ids.

## P1 and P2: the ceiling, as something the model can or cannot do

A sequence of blocks `A B C` and `D B E`, drawn in balanced random order. Every
step is determined by its predecessor except `B`, which is followed by `C` or
`E` according to what came *before* it. Scoring only the `B` steps:

    single-token key     0.533
    pair key             1.000

**Chance is 0.5.** The bigram model is at chance and cannot leave it however long
it trains; the pair key is exact. This is the ceiling claim stated as a
capability rather than a derivation, and it is the discriminating benchmark the
project has been missing — `reward_recall` is answered perfectly by a hash table
(g10-07), and this is not.

**The randomness in the sequence is load-bearing**, and a mutation proved it. A
repeating `A B C D B E` cycle makes every position predictable from any other at
a fixed offset, so `the-context-key-queries-the-WRONG-pair` — querying `(t-2, t)`
instead of `(t-1, t)` — scored perfectly on an alignment it never had. It
survived the periodic test and is caught by the shuffled one.

## P3: the retrieval really is a trigram table

Same probe as note 033, against the trigram count vector, on Tiny Shakespeare:

    writes    vs trigram    vs bigram
        40        0.8293      -0.0887
        80        0.8548       0.6102
       160        0.6882       0.2419
       320        0.6918       0.3005
       640        0.5684       0.2635
      1280        0.5520       0.3002
      2560        0.5328       0.3434

**Confirmed at low load and confirmed to degrade**, which is the same
interference signature. The residual bigram cosine is not zero because on real
text a trigram table and a bigram table are correlated by construction — that is
a property of English, not of the store.

## P4: and here is the bill

**The plateau is 0.53 where single-token binding held 0.88.** The reason is
simply that there are more keys: 469 distinct pairs occur in 4000 characters of
Shakespeare, against 66 tokens — seven times more, though far below the 4356 that
uniform text would produce. Capacity goes as `sqrt(d/N)` (note 020), so the store
fills seven times sooner.

On uniform random tokens it is much worse — 0.9377 at 80 writes collapsing to
0.5014 at 640, and *no pair repeats at all* below 80 writes. **Real text is
kinder than the worst case by a wide margin**, and that is worth saying plainly
because the uniform-token version of this probe looks like a refutation.

## So what is actually known

**Known:** the ceiling was real, and it moves. A step needing two tokens of
context goes from chance to exact.

**Known:** the price is a sevenfold increase in distinct keys on real text, and
the signal-to-noise of a retrieval roughly halves.

**Not known, and not guessable from a cosine:** whether bits per character
improves. A higher ceiling with a noisier store could land either side of the
5.256 the model currently scores. The two effects are measured in different
units and only a run puts them in the same one.

**Not measured:** the interaction with `decay`. Every probe here used `decay =
1.0`, which is the worst case for a scheme whose problem is too many keys — decay
bounds how many items superpose at once, so it should help the pair key more
than it helps the single-token key. That is a free parameter the sweep must
cover rather than inherit.

## What this does not claim

It does not claim the model now beats a bigram. It claims the architecture is no
longer *forbidden* from doing so, which is a different and smaller thing. The
project has published a retraction this week for exactly this kind of
overreach (g10-09), and the distance between "can represent" and "does predict"
is where that one went wrong.
