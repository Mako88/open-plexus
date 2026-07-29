# 047 — What the store can hold on text is an n-gram, and counting does it better

**Status:** an argument, not a measurement. Every number it leans on was measured
elsewhere; what is new is the account that ties them together.
**Affects:** why decisions 139–142 came out as they did, and what a better
instrument for this store would look like.

---

## IN PLAIN TERMS

The memory stores "what came after this". On ordinary text, that is exactly what
a word-counting table stores — and a counting table does it better, because it
counts exactly where the memory has to superpose approximately.

On the recall test the thing being remembered is arbitrary and stated once, so
there is nothing to count and the memory is the only mechanism that can work.

**That is the whole difference, and it is a property of the tasks rather than of
the memory.**

---

## The argument

On a next-token objective the store's binding is always the same shape:

    key(recent tokens)  ->  value(the token that followed)

With `TableKeys` the key is the previous token, so the relation stored is
`token -> next token`: **a bigram**. With `PairKeys` it is `(t-2, t-1) -> t`: **a
trigram**. Change the key scheme and the order changes; the *shape* does not.

An n-gram count table stores that same relation, exactly, with no interference
and no learning rate. So on text the store is attempting a job a count table
already does, and doing it in a representation that can only approximate.

**Its ceiling on text is the corresponding n-gram model.** Not because of a
capacity limit or an addressing defect, but because that is the relation it is
able to express.

## What this explains that was previously unexplained

**Decision 118 — "the unigram has never been beaten by this model".** It has
stood for weeks as a fact without a mechanism. If the store is a bigram in
vector form and superposition costs it more than counting gains, then hovering
just above a unigram is what the architecture *should* do.

**Note 033's bigram ceiling** stops being a limit of one key scheme and becomes
an instance of the general shape.

**g18-02's single-key result** — the store 0.68 bits *worse* than not existing
when addressed as a bigram — reads as: an approximate bigram competing with an
exact prior, and losing.

**And g18-06's null on rare repeats**, which is the one that prompted this. A
repeated rare word only helps if the *same continuation* follows it, because
what the store holds is "what followed X last time". Repetition of a word does
not imply repetition of its successor, so there is usually nothing useful to
recall. The store is not failing to retrieve — there is no fact there to be
retrieved.

## Why MQAR is different in kind, not in degree

MQAR's binding is **arbitrary, stated once, and not inferable from any corpus
statistic**. No amount of counting predicts which value was paired with which key
in *this* sequence, because the pairing is redrawn every time.

So counting scores at the trivial floor and the store scores 0.995 — measured,
decision 142 — and `nostore` scores zero, because there is nothing else in the
model that can do it.

> **The store pays exactly where an association is arbitrary, stated, and not
> inferable from statistics.** Text is nearly all inferable; that is what makes
> it text.

## What this does NOT claim

**Not that the store is an n-gram table.** It superposes many bindings, degrades
gracefully, survives churn, and distributes across machines — none of which a
count table does, and all of which are the point of the project. The claim is
narrower: *the relation it can express on a next-token objective* is n-gram
shaped, so on that objective a count table is its ceiling.

**Not that text is worthless as a benchmark.** It remains the honest check that
the model is not being flattered by a generator it was designed against. It is a
poor instrument for measuring *this component*, which is a different statement.

**Not that decision 142's query-marker candidate is refuted.** It is untested and
this note does not test it. What the note does is make it less interesting: if
there is no recallable fact at the position, being told to recall does not help.

## The conclusion this note was one step short of — John, 2026-07-29

Written before he read it, the note stops at *"text is a poor instrument for
measuring this component"*. He arrived at the same place from the design end and
went one step further:

> *"With this model we don't want it to be predicting necessarily. We just want it
> to be aware... the point isn't to predict what comes next, but rather to
> generate something that has meaning because of its awareness of the meaning of
> a bunch of different things."*

**That is the same finding, and his statement of it is the stronger one.** If the
only relation the store can express on a next-token objective is n-gram shaped,
then the limit is not the store, not the addressing and not the corpus — **it is
the objective**. Everything above is a long way of discovering that the question
was wrong.

Worth keeping the distinction sharp, because it is easy to over-correct: **text
as INPUT is not the problem. Text-PREDICTION as the score is.** A model that
reads text and is asked what it holds is a different measurement from one asked
what comes next, and only the second is bounded by counting.

**And it dates the drift.** STATE's instrument table has said since 2026-07-28
that `corpus.py` is *"the text line, closed by decisions 115 and 118"* and that
`closure.py` is *"THE PRIMARY INSTRUMENT... matches the stated goal"*. g17-01
reopened the closed line for a narrow and defensible reason — note 045's index
needs units that can carry meaning, and characters cannot — and decisions 135
through 142 went a long way down it without re-asking whether it was the right
question.

Nobody re-decided that text was the instrument. It was reopened for one purpose
and then inherited, which is the same failure mode as the constants in note 046,
one level up: **an inherited QUESTION rather than an inherited constant.**

## What would refute this note

**A task with stated, arbitrary facts embedded in natural text**, where the store
contributes and a count table cannot. If the store's contribution tracks how much
of an association is inferable from statistics, that task should show a gap where
plain text shows none.

If it shows none there either, the account is wrong and the store's problem is
not about what it can express.

**That is also the design for a better instrument**, and it is what this note
recommends over another pass at plain text: `closure.py` already generates
entailed facts, and the relational tasks already have arbitrary bindings. The
missing thing is a task with both — natural-text statistics *and* stated facts
that counting cannot reach.

## The standing caveat

This is an argument from the shape of the write rule plus results measured for
other reasons. **It has not been tested as a prediction.** The refutation above
is stated so that it can be, and this note should not be cited as a result until
something has run against it.
