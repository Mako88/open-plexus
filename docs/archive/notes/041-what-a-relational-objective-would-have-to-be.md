# 041 — What a relational objective would have to be

**IN PLAIN TERMS.** The model currently learns by being asked questions that are
marked as questions. Real learning does not come with a question mark. This works
out what a self-supervised version would look like — state some facts, hide one,
make the model recover it — and finds two problems that have to be settled before
any of it is built.

---

## Why this note exists

GOALS §1.2 now records the project's thesis in John's words: *"instead of
focusing on predicting text, train the model to understand the relationships
between things: to associate a given thing in the context of all other things."*

§5's recorded candidate — self-supervised **temporal** prediction, each unit
predicting its own next input — is marked as superseded in part, because
next-input prediction is the thing being moved away from.

**What replaces it has never been specified.** STATE.md carries "masked-link
prediction: state facts, hide one, predict it" as a sentence, and a sentence is
not a design. This is the design, written before code because the last three
mechanisms that went the other way each cost a rebuild.

## The shape

    facts stated:   FACT a child b   FACT b SO c   FACT d un e
    one hidden:     FACT a child b   FACT b  ?  c   FACT d un e
    target:         SO

No query marker, no answer position, no question format. **The mask is the only
marker**, and it is the same at every position, so nothing about the layout says
which fact matters — which is the defect note 027 found in `reward_recall`, where
the answer could be read off the spacing.

## Problem 1: most maskable positions are not recoverable, and that is not a flaw

Mask a distractor's object and **nothing in the sequence determines it.** The
model can only guess from the marginal distribution. Mask the relation of a fact
on the composed path and the rule table plus the other facts determine it.

The instinct is to mask only recoverable positions. **That instinct is wrong
here**, for a reason this project has already measured: note 008 §4 established
that irreducible loss contributes **no gradient**, and that random filler is
therefore the correct choice rather than the harmful one. The proposed
"structured filler" fix had the sign backwards.

So mask uniformly, and **report the split** — accuracy on recoverable positions
against unrecoverable ones — so the ceiling is visible instead of being averaged
into a single number nobody can interpret. An unrecoverable position's floor is
the marginal distribution of that token type, and it should be measured, not
assumed.

## Problem 2: it is not obvious this is harder than a bigram, and that is the risk

**This is the failure mode that cost the project its first year.** G0 exists
because the predecessor measured a learning rule against a benchmark a frozen
random substrate already solved.

A masked fact is `FACT S R O` with one of `S`, `R`, `O` hidden. The store binds
adjacent pairs. So:

- mask `O`: `key(S, R) -> O` is a **single stored binding**, and g13-02 measured
  that retrieval at 1.000 at a unique pair. **This is recall, not reasoning.**
- mask `R`: `key(FACT, S) -> R` is likewise one binding, measured at 0.915 at
  out-degree 1.
- mask `S`: no binding points at it, since the store keys on what precedes.

**Two of the three cases are the retrievals the project has already solved.**
Masking a fact the sequence states is not a relational objective at all — it is
the associative recall of MQAR wearing kinship's vocabulary.

### The version that is not trivial

Hide a fact **that is entailed rather than stated**. State `a child b` and
`b child c`, do not state the relation between `a` and `c`, and ask for it. That
is composition, and no single binding holds it.

But then the target is not in the sequence, so it is not masking — it is a
question without a question mark, which is what the current task already is.

**The distinction that survives:** what makes an objective relational is not the
mask, it is whether the answer requires COMPOSING two stored facts. A mask is a
delivery mechanism for the question; it is not the thing that makes the question
hard.

## What this note concludes, before anything is built

1. **Masking alone is not the objective.** Masked stated facts measure retrieval,
   which is measured. The mask is worth having for a different reason — it
   removes the marked-question format, which decision 95 measured as most of the
   remaining gap — but it does not by itself make the task relational.

2. **The objective needs entailed targets**, and the honest form is: state a
   subset of a graph's edges, and score the model on edges the subset **implies**
   but does not contain. Self-supervised, because the generator knows the closure
   and the model never sees it.

3. **Measure the trivial floors FIRST**, both of them, before any learning rule
   is run against this: a frozen random substrate, and a model that answers from
   the marginal distribution of relations. G0's acceptance test, applied to a
   task the project designed rather than borrowed.

4. **The split must be reported** — stated versus entailed targets — or the
   entailed cases, which are the whole point, get averaged away by the stated
   ones, which are already solved.

## What this does NOT settle

Whether the model can do it. Nothing here is measured; this is a design argument
that ends with a measurement to run, and it ends there deliberately — the point
of writing it before the code was to find the trivial-recall trap before spending
a sweep on it, and that trap was found.
