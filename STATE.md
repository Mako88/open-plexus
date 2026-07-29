# State — open questions and work in flight

**This is the only document in this project that is kept current.** Everything in
it is either live work, an open question, or a standing agreement. When something
here is settled it leaves, and an entry goes in [DECISIONS.md](DECISIONS.md).

Three documents, three jobs:

| document | what it holds | when to read it |
|---|---|---|
| [GOALS.md](GOALS.md) | what the project is for, the constraints, what would refute it | before deciding whether a mechanism belongs here at all |
| [DECISIONS.md](DECISIONS.md) | a chronological log of what was chosen and why — **history, never rewritten** | when you need the reasoning behind a specific past choice, looked up by entry |
| **STATE.md** (this file) | what is true now, what is open, what is running | first, and every session |

If this file and DECISIONS.md disagree, **this file wins**. If this file does not
mention something in the log, that thing is closed.

---

## IN PLAIN TERMS

The project is trying to build a neural network that runs across ordinary
people's computers over the ordinary internet, instead of inside a data centre.

Its central piece is a **memory that stores associations** — told "A is B's
parent", it can later answer a question about A and B.

**On the task that memory was built for, it does everything.** On a recall test
where the answer was stated earlier in the same passage, the model scores 0.995,
and switching the memory off drops it to zero. Nothing else in the model is doing
that work.

**On ordinary text, it contributes nothing at all** — and that took most of
2026-07-29 to establish, because a bug made it look like something worse. What
predicts text is a simple frequency prior: which words are common. The memory
adds no bits on top of it, at any width, any learning rate, any addressing
scheme, and any amount of text held at once.

Three explanations for the gap have been measured and killed: that text is easy
for other reasons; that the memory is addressed wrongly; that it is asked to hold
too much at once. **One is left and it has not been tested.** The recall test
announces its questions — it says *here comes a question* before asking. Ordinary
text never does. A memory that cannot tell "recall this" from "guess the next
word" would be drowned out by the prior at every position where guessing is
right, which in text is nearly all of them.

Testing that means changing the model or the task rather than measuring what is
there, so it is the first decision on this line that is John's rather than the
evidence's.

---

## THE BLOCKER: retrieval fidelity — and it is NOT a width limit

*(This heading read "and it is a width limit" until 2026-07-29. Decision 121
refuted that on the task in July and the warning box below has said so since, so
the heading spent months asserting what its own section retracts. Same defect as
`IN PLAIN TERMS` had, found in the same pass: **a heading is the part most people
read and the part least often revisited.**)*

Every end-to-end relational result is capped by how often a single retrieval is
right. **Four mechanisms have failed against the same number**, each correct in
itself:

| mechanism | decision | reached |
|---|---|---|
| the accumulator (hold both retrievals) | 102 | matched the 1-hop model exactly |
| pair keys, beyond their own collision | 105 | unusable with hops at all |
| traversal (a hop that builds pair keys) | 107 | +0.05 over a broken one |
| search (generate and verify) | 111 | +0.03 for k² the compute |

The number: **0.915** when an entity appears in one fact as a subject, **~0.35**
when it appears in several. Three chained retrievals at 0.7 compound to 0.46,
which is every end-to-end kinship result.

**Do not build a fifth mechanism on top of this.** All four were measured before
being built, which is the only reason three were never written.

**Decision 112 said width fixes it outright:**

    as configured   0.915      no decay   0.927      no cap   0.915
    width 128       1.000      width 256  1.000

### ⚠ REFUTED ON THE TASK — g13-01 landed, decision 121

Full table in
[the sweep record](experiments/sweeps/g13-01-does-width-fix-fidelity.txt). What
is live: **a fourfold width increase buys 0.020 and saturates.** Out-degree 1 is
perfect at width 64 already, so there was never anything there for width to fix,
and **decision 112's 0.915 was never a bound on task performance** — it ablated
raw retrieval where this trains `Wo`, and a linear readout recovers the argmax
from a retrieval that is not itself clean.

**Everything left sits at out-degree ≥ 2, just above 1/k, and no width closes
it.** The blocker is decision 108's **ambiguity**, not capacity.

---

## Which instrument, and why — asked by John 2026-07-28

Six task modules exist and that is two too many to be honest about. The split:

| task | role now |
|---|---|
| **`closure.py`** | **THE PRIMARY INSTRUMENT.** Unmarked stream of facts, some implied by others. Matches the stated goal — relational, no question marker, self-supervised in form. Passes G0 (decision g14-01): entailed headroom 0.277 against a frozen 0.000 |
| `kinship.py` | **the mechanism testbed.** Marked questions, so it isolates a mechanism cleanly — the whole search line (g13-01…05) is measured on it and those numbers stay comparable. Kept for that, not as the goal |
| `chains.py` | solved at 1.000, out-degree 1 by construction. A control, not a target |
| `mqar.py` | **NO LONGER JUST HISTORY.** Decision 142: the store scores 0.995 here and **zero** with it ablated, and the readout bias that wins on text *costs* 0.279. It is the only instrument that isolates the store from a prior, which makes it the control every other result now needs |
| `corpus.py` | **the text line, closed by 115 and 118 — REOPENED BY g17-01 AND PAUSED AGAIN 2026-07-29.** See below: the reopening was never re-decided, and what it measured says the *objective* was the limit |
| `reward_recall.py` | **retired** (John's call, decision 126) |

### The text line: what reopening it cost and what it bought

g17-01 reopened `corpus.py` at word level for one narrow, defensible reason —
note 045's index needs units that can carry meaning and characters cannot — and
decisions 135–142 then went a long way down it **without anyone re-deciding that
text was the instrument.** Note 046's failure one level up: an inherited
*question* rather than an inherited constant.

**What it bought** is worth keeping, because it is not nothing: a bug that had
survived four sweeps (138), the corrected bar (135), the ablation habit that now
protects every result, and note 047's account of why the store cannot win at
prediction — *the only relation it can express on a next-token objective is
n-gram shaped, and counting does that exactly.*

**The distinction that matters, and it is John's, 2026-07-29:** the point is not
to predict what comes next but to be *aware* of enough concepts to respond.

> **Text as INPUT is not the problem. Text-PREDICTION as the score is.** A model
> asked what it HOLDS is a different measurement from one asked what comes next,
> and only the second is bounded by counting.

So this is **paused, not condemned**. A text task scored on what the model holds
rather than what it guesses is untried and is not what 135–142 measured.

**The gap I should name rather than let sit:** *everything above is
self-designed.* **CLUTRR is the only external benchmark that would make a number
comparable to anyone else's, and it has been "next" for several cycles without
being run.** `kinship.py` borrows CLUTRR's design and says in its own docstring
that calling a number here a "CLUTRR score" would be wrong. Until that runs, every
result is this project grading its own homework.

## Open work, in order

> **PIVOTED TO ARCHITECTURE, 2026-07-28.** John: *"we're gonna have to redo all
> the tests anyway once some core pieces change, so let's get the core pieces
> right first."* Component work is paused, not abandoned — items 2 onward below
> are the queue it resumes into.
>
> [Note 042](docs/notes/042-an-architecture-pass-before-more-component-work.md)
> is the pass. Its finding: **the model has nowhere to keep a concept map.** The
> store is rebuilt every sequence and the only durable parameter is one
> `vocab × d` linear map (decision 62) — one fact that explains decision 63, 115
> and g14-01 at once.

### ⇒⇒ START HERE: two pathways, and the task decides which one pays

**Decision 142**, the control the rest of this section rests on, measured across
three seeds:

    on TEXT    the prior wins and the store adds nothing   (139, 141)
    on MQAR    the store wins and the prior COSTS 0.279    (142)

    MQAR   bias0 floor 0.9950   nostore 0.0000
           bias1 floor 0.7158   nostore 0.0000
           trivial floor 0.3438 -- what a SMART guesser scores

**The store carries MQAR completely.** `nostore` scores *zero* — not chance,
zero, because a model with nothing to retrieve does not guess, it emits a
constant.

**And the readout bias, which is worth 1.5 bits on text, costs 0.279 accuracy
here.** A prior with nothing to predict does not sit idle; it competes with the
retrieval for the same readout, and on a task with no exploitable marginals it is
pure interference.

> So *"the store contributes nothing on text"* is **not** a statement about the
> store being weak. It is a statement about text having marginals a linear prior
> can exploit, and the store having no advantage over that — while on a task with
> no marginals the store is everything and the prior is a liability.

Every text record below carried the sentence *"this does not touch the relational
line"* as an **inference**. It is now a measurement.

### And the mixture escape hatch is closed — g18-06

142 left an obvious way out: text is a mixture, so if the store does its job on
the thin *binding* slice, the mean would barely move while the mechanism works
exactly as designed. It does not.

                        floor      nostore      gap
    all                 9.1857     9.1873     +0.0016
    repeat              7.9178     7.9215     +0.0037
    RARE repeat        11.0963    11.0947     -0.0016      6.3% of positions
    novel              10.8058    10.8046     -0.0012

**Nothing, anywhere** — including where the token appeared earlier in the same
chunk *and* is rare enough in training that only binding could predict it.

*(`repeat` alone is confounded and the run showed it: repeats are 56% of
positions and score 7.92 against novel's 10.81, but `nostore` scores 7.92 too.
"Occurred earlier" correlates hard with "is common". Hence the rare class.)*

> So the difference between MQAR at 0.995 and text is **not** that text has
> marginals the prior takes first. **On text the store fails at the very task it
> aces on MQAR.**

### The mechanical hypothesis — TESTED AND NOT SUPPORTED

The idea: with `context_keys` the address at position `t` is `hash(t-1, t)`, so
retrieving what followed an earlier occurrence of a rare word needs the
*preceding* token to match too. The same word in a different context has a
different address and the earlier binding is unreachable. MQAR does not have this
problem — its query is a bare key after a marker, so the pair repeats exactly.

**The test was g18-06's rare-repeat split under SINGLE keys**, where the address
is the token alone and the earlier binding is reachable by construction. Three
seeds:

    single keys, RARE-repeat gap (nostore - floor, positive = store helps)
      seed 0    +0.0984
      seed 1    -0.2132
      seed 2    +0.1125
      mean      -0.0008

**Zero, with a ±0.2 swing across seeds.** Single keys do not rescue the store on
the one slice where binding is the only route.

Both schemes, three seeds, which is the complete answer:

                       pair keys                    single keys
                    floor  nostore     gap       floor  nostore     gap
    all             9.1858   9.1873  +0.0015    9.3162   9.1873  -0.1289
    repeat          7.9179   7.9215  +0.0036    8.1256   7.9215  -0.2041
    RARE repeat    11.0962  11.0947  -0.0015   11.0955  11.0947  -0.0008
    novel          10.8059  10.8046  -0.0013   10.8375  10.8046  -0.0329

**Every slice within 0.004 of zero**, except where single keys are actively
harmful on common repeats. And the two columns say one thing worth keeping: pair
keys are stable across seeds, single keys are not — under pair keys almost every
address is written once, so there is nothing for a seed to change.

> ⚠ **I reported this as confirmed from seed 0 alone before the other two
> landed** — "the first time all night the store has helped on text". That was
> one cell, and it was wrong. The failure I had spent the night documenting,
> committed within minutes of writing the memory about it.

So the hypothesis is **unsupported**, not established: the store contributes
nothing on text under either addressing scheme, and why text differs from MQAR is
still open.

### Over-subscription is dead too — the chunk sweep, three seeds each

MQAR binds 4 pairs per sequence; a 256-token text chunk binds 255. The measured
retrieval law is `sqrt(d/N)`, so that is a factor of eight before anything else.
Shrinking the chunk shrinks the load:

     chunk   rare-gap   all-gap   rare share   rare n
        16    +0.0001   +0.0004      0.0112      214
        64    -0.0008   +0.0010      0.0326      653
       256    -0.0015   +0.0015      0.0627     1263

**Cutting the load from 255 bindings to 15 changes nothing.** Every gap within
0.0015 of zero, no trend. The stated confound — a shorter chunk offers fewer
repeats to recall — is visible and does not rescue it: 214 positions at chunk 16
is enough to show a 0.10 effect, and there is none.

> At **MQAR's own load** of a handful of bindings, the store still contributes
> nothing to text. So the difference is not how much it is holding.

### ⇒ AND THE CANDIDATE BELOW IS PROBABLY THE WRONG QUESTION — 2026-07-29

Everything from here down measures the store **against a next-token objective**,
and [note 047](docs/notes/047-what-the-store-can-hold-on-text-is-an-n-gram.md)
argues that objective is the limit: the only relation the store can express there
is n-gram shaped, and a count table does that exactly and cheaply.

John, reading it: *"the point isn't to predict what comes next, but rather to
generate something that has meaning because of its awareness of the meaning of a
bunch of different things."*

So the query-marker candidate below is not refuted — it is **less interesting**.
If there is no recallable fact at a position, being told to recall does not help,
and on text most positions have no such fact.

**What replaces it, pending John's word:** the concept-addressing machinery is
built and tested and has only ever been scored by prediction. The instruments
where the store demonstrably matters — `closure.py`, `kinship.py`, `mqar.py` —
have never had it pointed at them.

### The candidate itself, kept for the record — and it is not built

**MQAR has an explicit query marker and text has none.** MQAR says *here comes a
question* before the key, so the model knows when to retrieve rather than predict
from the prior. Text never announces it.

If that is the difference, the store is not weak and not over-subscribed — it is
never told when to answer, and a mechanism that cannot tell recall from
prediction will be drowned by the prior at every position where prediction is
right, which on text is nearly all of them.

That is testable — a marker token before a held-out position, or a gate trained
to decide retrieve-vs-predict — and it is the first thing on this line that would
be a *build* rather than a measurement.

### the store's contribution on text is substitutable by a prior

**Decision 139**, measured on the corrected harness with its reproduction gate
passing. This is the first valid measurement in the line and it replaces every
number in the two sections below it.

    arm                          0.05      0.02     0.005
    characters bias0 floor      5.423     5.385     5.395
    characters bias0 nostore    6.000     6.000     6.000
    characters bias1 floor      5.395     5.377     5.203
    characters bias1 nostore    5.421     5.280     5.195

    words bias0   floor 10.700   nostore 10.759
    words bias1   floor  9.186   nostore  9.187

    characters   bigram 3.884   unigram 4.852   uniform  6.000
    words        bigram 7.848   unigram 8.068   uniform 10.759

**Both halves matter and the second does not cancel the first:**

    with NO prior available    the store is worth +0.615 bits   (characters)
    with a prior available     the store is worth -0.008 bits   (characters)
                               and +0.002 bits                  (words)

Every character-level number this project holds was measured at `bias 0`, where
the store is the only thing that can learn, and there it genuinely carries 0.615
bits. **But a readout bias does the same job slightly better and the two do not
add.** Give the model a prior and the store's contribution goes to zero, at both
units, across four rates and both key schemes.

> So the claim is **not** "the store has never contributed". It is that what it
> contributes on text is **prior-shaped** — and a prior is a `vocab`-length
> vector where the store is `d × d` plus a key scheme plus a write rule.

**It explains decision 118**, unexplained for weeks: the unigram has never been
beaten on text. If the store's contribution is prior-shaped, a unigram is roughly
the ceiling and the model has been sitting just above it.

**It does not touch the relational line.** MQAR, kinship and the chains are
solved through this store and no prior solves them — the bindings *are* the
answer. This is about text, where a prior is most of what there is to know.

**And it re-poses g17-01's premise rather than restoring it.** *"The model does
not learn word-level text at all"* was measured on a mistrained readout. What is
true is narrower and duller: the model learns word-level text about as well as a
prior does, 1.12 bits short of counting.

### g17-01's premise SURVIVES its own correction — decision 140

g18-04, its exact configuration with only the target fixed:

    bias0 floor     10.750      g17-01 recorded 10.721, off by 0.029
    bias0 nostore   10.759      uniform, exactly
    bias1 floor      9.932
    bias1 nostore    9.364      the store is worth -0.568

**The corrected model learns 0.009 bits over uniform where g17-01 reported
0.038.** So note 042's architecture pass and the week of addressing work rest on
a real finding. **What was void was my measurement of it, not it.**

And the second half is decision 139's claim in its strong form, stated where the
project actually lived: at `lr 0.05` — the rate every text sweep ever used — the
store is **0.568 bits worse than not existing** once a prior is available. Tuned,
the same comparison is +0.002.

> **The learning rate decides whether the store is harmless or harmful, and never
> whether it helps.** Across everything measured on a correct harness — two
> units, two key schemes, two widths, seven rates — its best contribution on text
> is +0.002 bits.

### ⇒ WHERE THIS LEAVES THE LINE, and the call is John's

| | |
|---|---|
| g17-01's premise | **stands** (140) |
| the store's contribution on text | **prior-shaped, ≤ +0.002 bits** (139) |
| concept addressing as the fix | **measured — worth 0.540 bits, and not because of concepts** (141) |

The third row is the honest gap. Decisions 136 and 137 refuted concept
addressing on void numbers, and g18-01's K sweep was withdrawn on those same
numbers — **neither the refutation nor the withdrawal was supported.**

**g18-01 ran on the corrected harness and the sign was backwards — decision
141.** 128 cells, three seeds,
[run 30434436216](https://github.com/Mako88/open-plexus/actions/runs/30434436216):

    bias 0 -- no prior, every character-level number's configuration
      floor         10.700        concept-64    10.159      +0.540
      shuffled-64   10.143        permuted-64   10.329

    bias 1 -- a prior available
      floor          9.186        concept-1024   9.279      -0.093

**Address density is worth 0.540 bits where the store is the only learner** —
the largest effect any addressing change has produced here. **And it does not
need to mean anything:** a grouping built from a SCRAMBLED corpus reaches 10.143,
slightly better than the learned one. The address count is doing the work, not
the concepts.

**A prior subsumes all of it.** At bias 1 no grouped arm beats the floor: 0.540
without a prior is less than the 1.514 a prior buys alone, and they do not add.

> So note 045 is **neither vindicated nor refuted**. Its claim is that addresses
> derived from *meaning* pay; meaning contributes nothing here. But 0.540 bits is
> real and reproducible — it is simply not about concepts, and a cheaper
> mechanism captures all of it.

**Why 136/137 had the sign backwards:** on a mistrained readout the retrievals
are noise, so denser addresses make the noise more consistent and the readout
fits it harder. Corrected, denser addresses carry more usable signal. That is the
second time the void harness produced a wrong number *with a satisfying
explanation attached*.

**Reversing a lean, and the reason is the interesting part.** The argument for
leaving it unmeasured was that a mechanism cannot buy 0.10 bits when the store
contributes 0.002. That confuses *this* addressing's contribution with a ceiling
on *all* addressing — 0.002 is what the current scheme gets, not a bound on what
a different one could. Withdrawing on that reasoning would repeat decision 112's
error: assuming an axis is flat because a different axis is.

The settled rate survives the correction, so the configuration stands: g18-02
corrected puts 5e-6 at 9.186 against 9.210 and 9.221 either side, still an
interior optimum.

### ⇒ The other open question, and it is no longer addressing

Note 042 turned this line toward *where facts are stored*. Three corrected
sweeps say the store contributes nothing on text beyond a prior — so the question
is **what the store is for on this task at all**, or whether text was simply the
wrong instrument and the relational tasks were the right one from the start.

---

### How that was reached: everything below was measured on a mistrained model

**Decision 138, and it is a retraction.** The g18 harness trained the readout on
the **current** token where the model's answer at step `t` predicts token `t+1`.

    character floor, as g18 measured it       5.9965    uniform is 6.000
    character floor, target corrected         5.4227
    decision 63, the comparison set          ~5.53

**How it hid:** the readout still learns — `|Wo|` reaches 0.88 — and the
temperature fit then flattens a signal-free score vector to uniform. So it
presents as *"the store contributes nothing"* rather than as a bug. Every arm was
mistrained equally, so the tables were internally consistent, the rails passed,
and the ordering across five addressing schemes was monotone and had a tidy
explanation. **What caught it was a reproduction, not a rail:** the character
floor came back at 5.986 where decision 63 says 5.53, and that had no innocent
reading.

**Void:** every model number in the sections below — g18-00's three passes,
g18-02, g18-03's first pass, and decisions 136 and 137 entire.

**Survives:** decision 135's bar correction (counts from `NGram`, no model) and
the address-space measurements (computed from the stream). And note 046's point,
strengthened: its rule was applied to the learning rate, and then a *training
convention* was inherited from the same script without being checked.

**And it puts g17-01 in question** — `run(piece, piece, ...)` is its line, so
*"the model does not learn word-level text at all"*, the finding that turned this
whole line toward addressing, was measured on a mistrained model.

**Corrected sweeps are dispatched.** The sections below are left standing,
unedited, with this notice at the top: a record that deletes its wrong numbers
cannot be checked.

---

### ~~The store contributes NOTHING at word level~~ — VOID, decision 138

**Decision 136**, and it displaces the addressing question rather than answering
it.

    the model, tuned                          9.185 bits/word
    the same model, NOTHING ever written
      to the store -- the readout bias alone  9.187
    word unigram                              8.068
    uniform                                  10.759

Those first two are the same model with its memory switched off, agreeing to
three decimals. **Every word-level bit this model earns is the readout bias**,
and that bias is 1.12 bits worse than counting how often each word appears.

**The learning rate was not teaching the model to use its memory. It was turning
the memory off:**

    lr 0.05     floor 10.108    the store is HARMFUL, by 0.92 against nostore
    lr 5e-6     floor  9.185    the store is INERT, to three decimals

No rate between 5e-4 and 2e-6 makes it positive. So the 0.98 bits that looked
like a recovered baseline is the model shedding a component that was hurting it.

**Which makes the next question this**, and it is bigger than addressing: *is
there any rate, width or key scheme at which this store contributes a single
positive bit at word level?* If not, the architecture line's next move is not a
better address — it is finding out what the store is for.

**g18-02 asks exactly that**, and its sharpest arm is `single` keys. A single key
addresses the previous token alone, which makes the store **a bigram in vector
form** — note 033's ceiling, except that here the ceiling is the target:

    word bigram (NGram)     7.848    what the shape can reach
    word unigram            8.068
    the model, tuned        9.185    pair keys
    the model, NO STORE     9.187    the bias alone

A bigram beats the bias-only model by **1.34 bits**. If the store cannot approach
that when addressed exactly the way a bigram is addressed, the problem is not the
address space — it is the store. g17-01 reported single keys diverging at word
level, at `lr 0.05` with no cap, and both of those are gone.

**Its rate is swept rather than inherited**, because 5e-6 was settled for pair
keys at width 128, and carrying it into a different key scheme would be
[note 046](docs/notes/046-the-frozen-learning-rate-may-have-created-a-conclusion.md)'s
mistake one more time.

**Not a general claim.** At character level the store has no bias to fall back on
and reaches 5.17 against a 6.00 uniform, so it is doing something there. This is
word level, width 128, pair keys, one epoch, one seed.

### g18-02 answered it: three axes, none of them it — decision 137

24 of 24 cells,
[run 30430499110](https://github.com/Mako88/open-plexus/actions/runs/30430499110).
Each arm against its OWN matched ablation, at the rate chosen on held-out
training text:

    pair   d128    store 9.185 against nostore 9.187    +0.002
    pair   d512    store 9.184 against nostore 9.187    +0.002
    single d128    store 9.778 against nostore 9.187    -0.591
    single d512    store 9.869 against nostore 9.187    -0.682

- **The rate is not it** — three rates over two orders of magnitude, best +0.002.
- **The width is not it** — quadrupling the store moves it 0.001 bits. "Too
  small to hold anything useful" dies with the rest.
- **The key scheme is not it, in the direction that mattered most.** Single keys
  make the store a bigram in vector form, and a word bigram beats the bias-only
  model by 1.34 bits. Addressed exactly that way, **the store is 0.68 bits worse
  than not existing.**

**The rail holds:** `nostore` is identical to three decimals across both widths
and both key schemes — spread 0.000.

> **So the problem is not the address.** Whatever the store retrieves, the
> readout cannot turn it into a better prediction than the prior it already has,
> and mixing it in costs accuracy.

### ⇒ g18-01 is WITHDRAWN before dispatch — reverse this if you disagree

Written, checked, pre-registered, settled at lr 5e-6 / cap 5.0: 128 cells over
the whole K axis, three seeds, both controls. **Not run.** Its gate asks whether
some grouping beats the floor; the floor is an inert store, the groupings are
already behind it at K=128 in five ways, and g18-02 says nothing makes the store
contribute at all. It would measure how much each grouping harms a component
that does nothing.

Decision 112's move, and g17-01's. **The script and workflow stay in the tree**,
so this is one `gh workflow run` to reverse.

### ⇒⇒ AND THE FIRST CHARACTER-LEVEL ABLATION SAYS 5.188

**One cell, and it is the most alarming number in this file.**

    the project's best text result ever      5.172   g11-07, eighteen compositions
    NO STORE AT ALL, bias on, one arm        5.188   measured 2026-07-29
    character unigram                        4.852   still beaten by neither
    uniform                                  6.000

A model with **no memory whatsoever** lands within 0.016 bits of the best result
this project has ever recorded on text.

**It is not a like-for-like comparison and must not be quoted as one.** 5.172 was
measured at a different width, over more data, with the bias OFF and eighteen
arms composed. 5.188 is width 128, 90,000 characters, one epoch, one seed, bias
ON. **And the section below is the reason it cannot yet be made like-for-like:**
this harness does not reproduce the character-level floor at all, so its
character numbers — including this 5.188 — are measurements of an instrument
until that is fixed.

> If it holds, **the store has never contributed anything on text at either
> unit**, and every text number this project holds is a statement about a linear
> readout rather than about the memory.

### ⚠ AND g18-03's FIRST PASS FAILED ITS OWN REPRODUCTION — read before quoting anything above

[Run 30431349857](https://github.com/Mako88/open-plexus/actions/runs/30431349857),
24 of 24 cells. **Its gate reads REFUTED and that verdict is not available**, for
one reason:

    characters bias0 floor     5.986      this harness
    decision 63, same model   ~5.53       the comparison set

5.986 against a 6.000 uniform is a model that has learned essentially nothing.
The character arm is therefore **not the character model**, and a run whose
control does not reproduce measures the instrument.

**The obvious cause was five inherited constants** — the character line uses
single keys, `decay 0.997`, no cap, `key_scale 0.5` and `lr 0.05`, and the first
pass carried the word-level settings across. Note 046's mistake for the third
time in one night.

**Restoring all five did not fix it.** A local cell at exactly g15-01's settings
returns **5.9965**. So the difference is something else in this harness, not yet
identified, and the diagnostic is running.

> **`P0`, added to g18-03: the character `bias 0` floor must land within 0.10 of
> 5.53 before any other prediction on that unit may be scored.** The run is held
> until it does. This is decision 131's lesson, which cost a whole matrix the
> first time.

**What the word half of the pass does say, and it is consistent:** `words bias1`
reproduces g18-02 exactly (floor 9.185, nostore 9.187), and `words bias0` has the
store worth **0.054 bits** over knowing nothing — so it is not literally inert,
it contributes about a twentieth of a bit, and the bias supplies twenty times
more on its own.

**Why none of this was caught earlier:** the ablation was never run.
`readout_bias` has been off by default since it was added, so no character-level
result was ever compared against a model that could express a prior — and there
was no arm that removed the store while keeping everything else.

### Store by CONCEPT, not by surface — the line this became

**SUPERSEDED by decision 136, above.** Kept because it is how the question was
posed and because the pieces it asks for are built and tested. What changed: the
floor it aims at was measured at the worst corner of an unswept grid, and at the
tuned corner the store contributes nothing at all — so "make the address space
denser" answers a question that is not the binding one.

g17-01's calibration found word-level text unlearnable, and the reason it gave is
address sparsity: the store is keyed by word PAIRS, and at word level almost
every address is seen once. Too many addresses, each too rare.

    uniform                                10.759
    the model, as g17-01 measured it       10.721   <- NOT the floor; see above
    the model, tuned (decision 136)         9.185
    the same model with NO STORE            9.187   <- the real floor
    word unigram                            8.068   <- the bar that matters
    word bigram                             7.848

**The bar was corrected on 2026-07-29 (decision 135).** It stood at 9.323 here
and in g17-01's record, hand-rolled beside the calibration instead of taken from
`openplexus/ngram.py`. The real gap is **2.65 bits, not 1.40** — the finding is
unchanged and bigger than it was written down as. Two further instrument facts
from the same check: the stock temperature grid **pins at its own edge** at word
level once the model has a readout bias, and `readout_bias` is worth 0.52 bits
here against a default of off.

**But the store is no longer required to be addressed by surfaces.**
`openplexus/concepts.py` split them: `surface -> content vector -> concept id ->
store`. If concepts are GROUPS of words rather than individual words, the address
space collapses and recurrence rises — a few hundred concepts instead of 1,733
words, each seen many times instead of once. **The readout still predicts
surfaces**, so nothing is lost on the output side: store by concept, emit by
word.

> **The question as posed:** does storing by concept rather than by surface make
> word-level text learnable at all?
>
> **Answered at K=128, and the answer is no** — every grouping arm is at or
> behind a floor that is itself an inert store. The K axis and the controls are
> still worth running as a verdict (g18-01), but the gate has already failed in
> five different ways.

### BUILT, 2026-07-29 — the pieces, and what they measured

`openplexus/grouping.py` (spherical k-means over content vectors) and
`keys.ByConcept` (any key source, addressed by concept instead of surface) are
written, tested, and committed. `experiments/g18_01_...` holds the sweep.

**The address space collapses exactly as the proposal says**, measured on 90,000
words before dispatch:

    arm              concepts   addresses   recurrence
    floor                1733      36,299         2.48
    concept-128           179       3,438        26.18
    stratified-128        379      19,490         4.62
    permuted-128          179       3,080        29.22
    shuffled-128          178       2,596        34.67

**And that is what broke it.** The first concept cell overflowed to NaN, `|Wo|`
reaching 1.6e63. Pair keys over surfaces never did, because the defect *was* the
brake: almost every address was written once, so the sparsity that made the model
useless was the only thing holding the store's norm down.

### g18-00, three passes — how the headline above was reached

118 cells over three CI passes — runs
[30425355572](https://github.com/Mako88/open-plexus/actions/runs/30425355572),
[30425842494](https://github.com/Mako88/open-plexus/actions/runs/30425842494),
[30426222929](https://github.com/Mako88/open-plexus/actions/runs/30426222929) —
plus one local ablation. Full record in
[the sweep file](experiments/sweeps/g18-00-what-does-each-arm-need-to-run-at-all.txt).

The rate was frozen at `lr=0.05` from character level, and **note 028 had audited
exactly that defect one line earlier**. Sweeping it and the store's cap moved the
floor from 10.195 to 9.185 — before the ablation showed what that movement was.

**Five addressing schemes at K=128**, each at its own best rate:

    floor            9.185     no grouping
    stratified-128   9.185     only the rare tail grouped
    current-128      9.252     one coordinate of the pair grouped
    context-128      9.591     the other coordinate
    concept-128      9.985     both coordinates
    nostore          9.187     nothing written at all

Read with `nostore`, that is not "collapse costs resolution". **The store was not
using any resolution**, so grouping did not spend any — what the groupings did was
make an inert component harmful again, in proportion to how much they collapse.

**Two rails were added on the way and both fired.** `diverged` catches a NaN;
**`unstable` catches a calibrated model worse than uniform**, which cannot happen
unless the calibration and test text disagree about what the model does. The
second exists because concept-128 at lr 0.005 returned 36.9 bits with no NaN
anywhere — it would have entered a table as a number.

**Everything is chosen by `fit_error`** — held-out TRAINING text — never by the
test set.

**What was true and is now wrong:** the earlier instruction here said *"do not
start by fixing word-level text directly (decay, cap, learning rate) — that is a
separate and much longer line."* The mechanism could not be measured without
touching it: concept addressing does not run at the stock learning rate at all,
and the rate turned out to be the finding. The two questions were one.

### 0. THE ARCHITECTURE LINE — where the work actually is

**Approved by John: items 1 and 2 of note 042.** They are the same design seen
from two sides — a persistent store partitioned by concept — so building either
alone means building it twice.

| | change | status |
|---|---|---|
| **0a** | **persistent slow store** | **DONE and REFUTED in its strong form** (decision 133). Worth **0.08 bits at every scale** — keep it — but it does not move the wall |
| **0b** | **concept partitioning** | **PARTS BUILT, NOT WIRED.** `ownership.Ring` + `partitioned.ConceptStore` + replication exist and are tested. The capacity falsifier ran (g16-01, decision 134). **The falsifier that matters has not: can the model still LEARN through it?** |
| **0d** | **repair / anti-entropy** | **NEW GAP, surfaced by John's own question.** Replication depletes and never recovers. Nothing is built |
| **0c** | **addresses that mean something** | **NOW THE LIVE WORK** (was "content-derived keys"). [Note 045](docs/notes/045-addresses-that-mean-something.md) scopes it, and **its cheapest gate has passed**. The direct route is REFUSED on our own evidence |

**0c is the deeper limit, and John named it before I did.** *"We need a way to
represent the same concept"* — a picture of a dog, a drawing, a sound, the word.
That is the same problem as 0c's one-line description, seen from the other end: a
concept can only have one address today, because the address IS the token id.
Note 045 works it through. Two things it settled:

- **Similarity does NOT go in the key vector.** Note 035 already said so —
  interference is `O(N·ρ)` in mean key cosine — and the refusal is on our own
  record rather than on taste. Instead: keep hash-derived ids for the store,
  add a separate **index** from a content vector to concept ids, and read by
  committing to a hard id. That is the third independent argument for
  commit-to-a-token (accuracy, decision 123; routability, note 044; now
  capacity), and it dissolves note 044's ring-versus-similarity tension —
  only the index needs locality-sensitive placement.
- **P4 passed.** Co-occurrence vectors built by one local accumulation per
  observed pair carry real structure (`king → prince, crown`; `father → son,
  brother`), where the same construction on a **shuffled corpus returns only
  high-frequency words**. Caveat that changed the plan: mean off-diagonal cosine
  is 0.50 against hash keys' 0.0005, and the standard fix over-corrects, so
  **weighting is a swept axis rather than a default**.

**And "sweep everything, then filter" was tested and half-refuted** (John's
proposal). At equal read budget, one view at top-30 beats the union of three
weightings at top-10 (recall 0.104 against 0.086) — **depth beats variety**,
because the views largely return the same candidates. Retrieve-then-filter
stands; diversifying by weighting does not.

> **None of those numbers are results.** Single corpus, single seed, no
> pre-registration — they are for deciding what to build. The comparison John
> asked for is a proper sweep with an **exhaustive-retrieval ceiling arm**, the
> current model as floor, and **reads per answered position quoted beside every
> accuracy** so neither can be cited without the other.

**0b's remaining risk is the one nothing has touched.** Everything measured so far
is a property of the *store* — capacity, balance, survival. The model has never
read or written through it. Routing needs the token id where `Retrieval.read`
takes a key vector, so the store cannot sit behind the existing seam unchanged,
and until a model trains through it the arrangement is a data structure with good
properties rather than a component.

**0d, in full, because John asked the question that found it.** *Do concepts
redistribute when a node drops?* **No.** The ring is unchanged, so a read falls
through to the next surviving holder and keeps working — but the replica count is
never restored:

    nodes lost (of 20)   survival   mean live holders per concept
                     0      1.000                            3.00
                     6      0.967                            2.03
                    10      0.873                            1.43
                    14      0.656                            0.84

**So the 0.896-at-half-the-network figure is the single-event best case, not the
steady state.** C3's premise is that churn is continuous; under continuous churn
the count walks to zero and survival follows. The degradation is invisible until
it is total, which is the worst shape a failure can have.
`test_redundancy_DEPLETES_because_nothing_repairs_it` asserts the defect so it
cannot be forgotten — it fails the day repair lands.

The fix is standard: on losing a holder, a survivor copies the concept to the next
distinct node clockwise. Consistent hashing already names who that is, so it is a
local exchange between ring neighbours with **no coordinator** — which is why it
fits C1. Cost is `width²` numbers per concept moved, constant background traffic
under constant churn, and that wants measuring against `d_max` (~640 ms) rather
than assumed. That is anti-entropy and hinted handoff, in the DHT literature
GOALS §6.2 has listed unread since the project began.

**0a's result, and it redirects the whole line.** `persist-slow-decay` beats the
baseline at every data point, and its store norm is **0.4 at every corpus size** —
decay balances writes at a fixed point, so it reaches equilibrium immediately.

> **A decaying persistent store is a fixed-size cache, not a map.** Without decay
> it diverges to NaN instead. Persistence adds *lifetime*, not *capacity*.

**So the wall is a CAPACITY limit, not a lifetime limit**, and decision 63 re-reads
as: 16,000 characters is where a `d × d` store plus a `vocab × d` readout runs out
of room. Note 042 said the wall was about having nowhere to accumulate; there is
now somewhere, and it did not move.

**0b's falsifier then ran, and corrected me twice** (g16-01, decision 134):

    arrangement nodes    pooled     ALONE   node sees
    concept     16         2048      2048   64 of 64 dims
    dimension   16         2048       128   16 of 256 dims

**Pooled capacity is IDENTICAL** at every node count — so "concept partitioning
adds capacity" was wrong, and so was the floor version of it. What differs is
**lone-node** capacity: concept scales with the network, dimension is stuck at one
node's worth from four nodes onward. Sixteenfold at 16 nodes.

> Under dimension splitting, growing the network makes every node's view thinner
> while the total stays the same, so **a node can never answer alone however
> large the system gets.** Under concept splitting a node owns whole concepts, so
> its standalone capability grows with the network.

**That is what amended C1 cares about** — a read requiring every node is the
barrier the constraint forbids. It is the only capability difference between the
two, and it is the reason to build 0b.

**0a's falsifier is decision 63's 16,000-character wall**, and two runs have
measured the *instrument* rather than the hypothesis:

- **g15-01 first pass (decision 131)** — the slow store's norm was pinned at its
  cap from the smallest data point. It tested a **saturated** store.
- **cap sweep (decision 132)** — every cap pinned exactly, because `lasting` has
  only `+=`: the fast store brakes with `memory *= decay` and the slow one had
  no equivalent. Note 018's defect, mirrored. Fixed with `lasting_decay`, and a
  brake alone was not enough — **the write rate was ~100× too large**, tuned for
  a store that gets rebuilt rather than one that persists.

`persist-slow` (consolidation 0.005) and `persist-slow-decay` are the first
settings where the store tracks the corpus instead of saturating. **That run is
the first time the question actually gets asked.**

> **If the wall does not move with the store genuinely accumulating and the gate
> firing tens of thousands of times, that is a real refutation** — note 042's
> account would be wrong and the proposal needs rethinking rather than retuning.

### 1. ✅ CLOSED — the search line landed. Decision 130

    concat      0.327    what we had -- BELOW the 0.466 shortcut floor
    walk        0.596    pair-key traversal, which decision 107 declined
    search4     0.604    search everywhere, which decision 111 declined
    gate-q50    0.624    search where it helps  (+0.020 +/-0.005 over search4)

**The gate keeps `search4`'s accuracy at out-degree ≥ 2 exactly (0.539) and
recovers most of `walk`'s at out-degree 1** — the trade g13-03 said was
available. Five of five predictions confirmed.

Both refusals — 107 and 111 — were correct arithmetic on the numbers of their
day, and both conditions were measured away before anything was rebuilt.
**Nothing had to be undone**, because both declined to *build*.

> **The threshold generalises; the number does not.** `gate-q50` fires at a
> margin of 0.663 at width 256, and that constant is not the mechanism — it is
> the median of the model's own training margins, computed without labels and
> without touching the test set. Width-dependent (`docs/SCALE.md`); this is a
> width-256 result.

**Still unaccounted for: 0.624 against g13-02's retrieval-chain ceiling of
1.000.** Nothing decomposes that gap. Composition on top of clean retrievals is
still inherited from decision 102 rather than re-measured, which is the most
likely place for it to hide.

<details>
<summary>How the line got here (superseded detail)</summary>

### BUILD SEARCH — its blocking condition is measured gone

Decision 111 refused search on one ground: *"you cannot search your way out of
noisy primitives, because the verifier is built from the primitives."*

**g13-01 measured the primitive at 1.000 (±0.000, 8 seeds) at out-degree 1.** A
verifier built from a retrieval that is right every time is trustworthy. The
refusal was conditional and **the condition has expired** — this is sequencing
catching up, exactly as decision 111 said it would ("revisit it the moment
retrieval fidelity moves").

What remains is ambiguity: at out-degree ≥ 2 the store returns *a* relation the
subject genuinely holds, and nothing in the question says which one leads to the
target. Search is the mechanism that resolves that — try a branch, retrieve its
endpoint, check it against the asked object.

**The ceiling is now measured, and it justifies the build** (g13-02, decision
122, 8 seeds, five of five predictions confirmed):

    step 1 at out-degree 1   1.000     search's job is to get here
    step 2 at a unique pair  1.000     0.971 overall; decision 107's 0.960 reproduces
    step 3 at out-degree 1   1.000     same operation as step 1
                             -----
    traversal with search    1.000     against the 0.87 that justified it

**The asymmetry is why it works.** Step 2's ambiguity is 5.1% of sequences where
step 1's is 50% — a `(subject, relation)` pair names one person almost always,
where `(FACT, subject)` names one of several relations half the time. The
traversal's weak steps are its two ends and its middle is sound, which is exactly
what makes a verifier built from step 2 trustworthy.

### 1a. BUILT, and not yet wired — decision 123

`openplexus/search.py` exists, with 10 tests and 2 mutations, both caught. It
takes the top `b` candidates from the first decode, **commits** to each, walks
the graph, and scores each walk by whether its endpoint matches the object the
question names — the disambiguator that was in the question all along and that
nothing had ever used.

**The wire cost is answered and it is affordable** (`tools/search_cost.py`):

    branches   decodes   x greedy   positions/s   (1024 nodes, depth 2, 10 Mbps)
           1         4       1.0x        39,062
           4        13       3.2x        12,019
          16        49      12.2x         3,189

Beam 4 costs **3.2×** the decode traffic and still supports ~12,000 answered
positions per second. Depth is harsher and only mildly: 3.2× at depth 2 to 3.7×
at depth 5. **Bandwidth is not what binds search.**

> The pooled decode is a collective, and note 009 §4 has carried that as an
> outstanding C1 item since long before search. Search does not create it — the
> readout already requires it — but it makes it `b(2d-1)/d` times more frequent,
> which raises the stakes on item 6 below.

**It has never seen a generated sequence.** The tests run on a hand-built store
of four facts. The unit test says the mechanism is correct; whether it survives
distractors, decay and a cap is the next measurement.

### 1b. MEASURED — g13-03, decision 125. Traversal is the win; search needs a gate

Full table in
[the sweep record](experiments/sweeps/g13-03-does-search-pay.txt). What is live:

- **Traversal is worth +0.269** and clears the 0.466 first-relation floor that
  nothing on this task had cleared. Decision 107 declined it at a costed "+0.05";
  that verdict did not survive the primitives moving.
- **Search overall is a tie** (+0.008 ±0.018) and the split says why:
  **−0.054 at out-degree 1, +0.092 at out-degree ≥ 2.** It does exactly what it
  was built for and damages the case it was not, and the test set is half of
  each.
- **`search8` is 0.024 WORSE than `search4`, at 6 SE.** "Search wider" is not the
  way to close the gap.

### THE NEXT MECHANISM: gate the search on ambiguity — signal MEASURED

**g13-04, decision 129: yes, at width ≥ 128.** The decode margin — the gap
between the first decode's top two candidates — separates ambiguous from
unambiguous at **AUC 0.803**, against decision 93's 0.628 for identity-free
confidence signals fitted *with* the labels.

    decode margin      d64 0.710    d128 0.841    d256 0.858
    endpoint margin    d64 0.480    d128 0.447    d256 0.448

Two things to carry into the build:

- **The expensive signal is below chance.** The endpoint margin — available only
  after paying for the walks — is *anti*-correlated, so a gate must decide
  **before** walking. That is also the cheap direction; both arguments agree.
- **It is width-dependent** and belongs in `docs/SCALE.md` as such. A wider store
  holds a cleaner superposition, so a peaked decode gets more peaked and a
  contested one more contested. Sound at 256, weak at 64.

**Build it: walk greedily, branch only where the margin is narrow.** A perfect
gate is worth roughly **+0.03 over search-everywhere** plus the walks saved.

> **The threshold is the honest problem.** AUC measures separability across all
> thresholds; a gate needs one, and picking it on the test set would be fitting a
> number rather than measuring one. Use a held-out split, or derive it from the
> decode's own scale. **And the number to beat is `search4`'s overall, not
> `walk`'s** — a gate that merely matches search-everywhere has bought compute
> savings and no accuracy.

Also still open: **re-measure composition** rather than inheriting decision 102's
1.000, which was taken on a different configuration.

### 1b. Two loose ends from g13-01, both cheap and both unexplained

- **`hop2-concat` gains MORE from width than the primitive does** (+0.051 against
  +0.021), from a far lower base. That is backwards from the compounding story
  and nothing accounts for it.
- **`hop2-concat` is below the floor that matters** — 0.327 against a
  first-relation floor of 0.466. Decision 102 recorded concat *matching* the
  one-hop model; on this instrument it loses to the one-hop shortcut.

</details>

### 2. `carry_store` — two measurements with OPPOSITE SIGNS, and nobody has reconciled them

Decision 116, notes corpus, train-then-test — `carry_store` **helps a lot**, and
superadditively with `hidden` (0.26 and 0.45 alone, **0.88 together**):

    chunk    linear   linear+carry   hidden 128   hidden+carry
       64     6.024          5.765        5.574          5.140
      256     5.914          5.755        5.393          5.137

Decision 117, Shakespeare, prequential, 250k chars — `carry_store` **hurts**:

    model, hidden 128                  5.665
    model, hidden 128 + carry_store    5.737

**An earlier version of this document called it "the cheapest unclaimed win",
citing 116 and not 117.** That is the same error the 2026-07-28 restructure was
about — quoting one measurement as current while another qualifies it.

The two differ in corpus, vocabulary, regime *and* chunk order, so neither
refutes the other and no one-line fix is available. The discriminating
measurement is a 2×2 — `{carry off, on} × {shuffled, sequential chunks}` — in
**one** regime, on Shakespeare, prequential. `carry_store`'s own docstring says
it is correct only when consecutive calls carry consecutive text, so chunk order
is the hypothesis and it has never been the swept axis.

Needs a committed instrument, same as kinship did.

### 3. A relational self-supervised objective — RAISED, on John's question

> *"Would it make sense to move this up higher, since it seems like it might
> shift a lot of things?"* — John, 2026-07-28. **Yes.** GOALS §1.2 now records
> the objective as the project's thesis rather than an implementation detail,
> and §5's recorded candidate (next-INPUT prediction) is marked as contradicting
> it. Everything below this point is measured under an objective the goals no
> longer endorse, which is exactly the "old assumption still being acted on"
> failure mode. It moves above the housekeeping and below only the two items
> that block it mechanically.

All-position (next-token) training was never required by the goal — it was
imported from how LLMs train, and it costs composition 1.000 → 0.40. Decision 98
stopped the *decay* by giving the gate its own objective (`which_hop`); it did
not close the level.

**Masked-link prediction** — state facts, hide one, predict it — is
self-supervised without marked questions, and relational rather than sequential.
That is much closer to what the task is about. Not built.

### 4. External benchmarks, so the numbers mean something to someone else

**CLUTRR** is the direct external check on our 0.992 zero-shot depth result
(train short chains, test longer). Then **bAbI task 2**, and knowledge-graph link
prediction. Keep bits/char as a diagnostic that the substrate works, not as the
score that matters.

### 5. A C4 test that the model cannot already pass

**C4 — perpetual learning — is still untested**, and two attempts to build a case
where continued learning helps both failed: decision 91 (a departure costs
capacity, and capacity is not something learning rebuilds) and decision 92 (the
mechanism already generalises). Neither says perpetual learning is worthless.
Both say **this task is too easy to need it**.

Related and unbuilt: **replay**. C4 forbids stopping, not revisiting (decision
78), and replay is one of the few known answers to the catastrophic forgetting
C4 makes first-class. A bounded buffer of past chunks, resampled. Cheap to try.

### 6. RESOLVED — and the real C1 gap is somewhere else entirely

**The sum is not the problem.** `answer = parts.sum(0)` is the numpy reference's
convenience. The deployed path sends each node's **argmax in 8 bytes**
(`combine="vote"`), and `distributed.py:419` says why that is different in kind:
*"Absence costs a voter, not a term of a sum, which is why this degrades where
summing amputates."* Bounded bytes per hop, and a missing node degrades the vote.
**Amended C1 is satisfied by the wire format.**

#### ⚠ But the DRIVER has no failure detector, and that IS a barrier

`distributed.py:427` settles a step only when it has a vote from every node it
expects:

    while settled < sent and pending[settled][1] >= expected[settled]:

A **declared** departure works — `absent` and `leave_at` adjust `expected`, which
is what g12-02 measured across 18 cells with no hang. An **undeclared** one does
not: the step never reaches its count, the window fills, the driver stops
sending, and 30 seconds later `select` raises `TimeoutError`.

**That is precisely what amended C1 forbids** — a barrier that stalls when a
participant is slow or gone. And C3 says departure is the normal case, arriving
without warning.

**BUILT (decision 126).** `run(deadline=...)` settles a step after a stated wait
with whatever votes arrived; off by default, because it costs bit-identity — the
property G2 was passed on. A node terminated without warning now leaves the run
running. Two related bugs fell out: a send to a reset peer propagated, and **a
reset was never treated as a hang-up at all**, so on any platform reporting a
dead peer as a reset the existing hang-up branch never fired.

**Still short of SWIM, and note 039 — now read from the paper — says how.**
Detection runs on the data path rather than a probe channel; there is no indirect
probing, so a slow node and a gone node are indistinguishable; and the driver is
the sole detector, a coordinator by another name. Suspicion-with-recovery is in.

**MEASURED — g12-04, decision 128. `d_max` ≈ 640 ms.**

    clean                            p50   0.61   p99   2.54   3xp99     7.6
    delay 80ms jitter 20ms loss 2%   p50  87.22   p99 211.88   3xp99   635.6

Full table in
[the sweep record](experiments/sweeps/g12-04-what-is-the-round-trip.txt). This is
simultaneously the C2 asynchrony bound and the C3 churn timeout — note 003's "two
constraints, one parameter" — and the first time either has been a number rather
than a count of steps. **A floor from six links, not a universal constant.**

**Next: replace `RETRY_AFTER_STEPS` with a duration.** Eight steps is under 3 ms
on the clean link and several seconds on the worst — one constant meaning two
things three orders of magnitude apart.

Two things worth carrying forward from that sweep:

- **Quote the p99−p50 gap, not the p99/mean ratio.** Once a fixed delay
  dominates, mean and p99 converge (1.01× at delay 80) because a constant moves
  both. The gap is what a timeout must cover: 1.0 → 16.0 → 124.7 ms as jitter
  then loss are added.
- **Loss is multiplicative with delay, not additive.** 2% loss alone is
  invisible; the same 2% on an 80 ms link doubles the p99, because a retransmit
  costs a round trip.

> SWIM also achieves **≤135 bytes per packet regardless of group size**, by
> separating detection from dissemination. That is amended C1's requirement met
> in a published system — an existence proof, not a trade-off to haggle over.

**Every churn result in the project was measured with departures announced in
advance.**

### 6b. CONCURRENCY COSTS d² PER CONVERSATION, and that inverts the usual picture

Raised by John on 2026-07-28: *"assuming ~65,000 nodes and a chat interface,
would we need another 65,000 nodes for each concurrent interaction?"*

**No — but the reason concurrency is expensive is worse than node count.** Read
from `openplexus/distributed.py`, a node holds three things:

    values    vocab x own.width     shared parameter, read-only
    readout   vocab x own.width     the learned parameter, shared
    memory    own.width x d_model   PER-SEQUENCE working state

The parameters are shared across conversations; only `memory` is per-conversation
— its docstring says so directly, *"per-sequence working state, not a
parameter"*. So a second conversation needs a second store, not a second network.

**The arithmetic is the problem.** Per node the store is `(d/P)·d`, so across the
network it is **d² per conversation**. At width 1M, and at the float64 the code
actually allocates, that is **~8 TB of aggregate store for one conversation** —
128 MB on each of 65,000 nodes — against a shared readout of ~6 MB per node at a
50k vocabulary.

**The per-conversation state is roughly twenty times the shared parameters.**
That is the inverse of a transformer, where weights dominate and the KV cache is
secondary, and it means **concurrency is bounded by node RAM rather than by node
count.**

#### ✅ AND IT CAN SCALE BY NODES AFTER ALL — John asked, and my first answer was too pessimistic

John's requirement, 2026-07-28: *"we can't control what nodes are actually going
to be running the code, so the requirements per node need to be as minimal as
possible, and everything that is at all possible to scale by adding nodes should
be the way we scale rather than requiring heavier nodes."*

**The store is d² in TOTAL but d²/P per node, so splitting further already
shrinks each node's share.** What stops that is the floor of ~16 dimensions per
node, below which a node has no standalone opinion (g4-01: 16 dims → 0.949,
8 → 0.681, 4 → 0.412). At width 1M that caps the split at ~62,500 nodes and
~128 MB per node per conversation.

**But concurrency does not have to reuse the same nodes.** Give conversation A to
one set of ~62,500 and conversation B to a different set. Then:

    per-node RAM        constant, one conversation's slice
    concurrency         linear in node count
    what is replicated  the LEARNED parameters, ~6 MB per node

The parameters are three orders of magnitude smaller than the store, so
replicating them across sets is cheap. **Concurrency scales by adding nodes, as
required.**

**The cost is real and it is a distributed-systems problem, not a modelling
one.** Under C4 the readout never stops learning, so disjoint node sets drift
apart — each set learns from its own conversations. Reconciling them is exactly
gossip, CRDTs and anti-entropy, which GOALS §6.2 has flagged as **unread** since
the beginning and which note 003 named as the highest-value gap.

Two things still follow, and neither is measured:

- It is the same d² that decision 109 measured capacity scaling by. A bounded
  cache is `slots × d`, not `d²` — so item 7 below is not only about churn
  tolerance, it is about how cheap a conversation is.
- **Nothing serves two conversations today.** `Node` holds exactly one `memory`
  with a `reset()`, so multi-session serving is unimplemented, unmeasured, and
  not costed. This entry is architecture read off the code, not a result.

### 7. Item-partitioning vs dimension-partitioning

`partitions` splits the store by DIMENSION, so every node computes the same
`M_slice @ key_slice` and **inherits the sum**. Partitioning by ITEM makes a read
a SELECTION across nodes. It is also partial-tolerant by construction: lose a node
holding dimensions and the retrieved vector has holes; lose a node holding items
and you take the best of whoever answered.

Decision 61 opened this and decision 119 bears on it — the superposed store beats
a bounded cache by a factor of eight when bindings exceed slots, so "just keep
items separately" is not free.

### 8. The distributed path cannot run a gated model

`distributed.Node.step` is a **reimplementation** of the model's inner loop, not a
call into it. A config carrying gate settings is accepted, ignored, and answered
anyway — measured, with two tests pinning it. **This scopes every "the split is
exact" claim in the project**: exactness was measured on the ungated inner loop.

The fix is a step-wise API on `LocalAssociativeMemory` that the node calls, not a
second gate implementation on `Node`. The second is what will be tempting. It is
a real refactor and wants its own cycle.

### 9. Housekeeping, none of it blocking

- ~~**The Docker testbed is not in CI.**~~ **WRONG, and it was carried into this
  document from the archived backlog without being checked.** Three sweeps run
  the testbed on Actions in real containers — `sweep-g12-01`, `sweep-g12-02`
  (churn, 18 of 18 cells, nodes vanishing mid-run) and `sweep-g12-03` — plus
  `testbed-identity.yml`. **The model has run distributed across containers, in
  CI and locally.** What has *not* run distributed is the relational work:
  kinship, hops and search are single-process only, and `Node.step` still cannot
  run a gated model at all (item 8).
- **`KeySource` needs the conformance suite retrieval has** — no shape check, no
  purity check, and nothing proving the suite bites. Before any combinatorial
  sweep over keys, because a broken implementation inside a grid does not
  announce itself.
- **`mutate.py --changed` should select by HUNK, not by file.** 60 of 134
  mutations for `local_memory.py` is twenty minutes, which is the long local run
  the rule exists to avoid — so it degenerates exactly where the work happens.
- **`orthogonal_every` cannot be re-checked without being reimplemented.**
  Decision 54 refuted it as "a cure for someone else's disease" because there was
  no per-layer structure to orthogonalise. With a `hidden` readout there is, so
  the refutation may not survive. Do not bundle this into another sweep —
  implementing a mechanism and re-checking a refutation together produces a
  number nobody can attribute.
- **Per-job parallelism in sweeps.** Every job trains serially on a ~4-core
  runner. A `--workers` option cuts wall-clock by roughly the core count on every
  sweep from now on. Costed nowhere; measure before believing the factor.
- **Uneven slices.** `slices_for` refuses any split that does not divide evenly.
  Real machines will not offer round numbers, and heterogeneous node sizes need
  this first.

### 10. Self-imposed limits found in the 2026-07-28 audit

John's standing test: **the only real constraints are that it runs across devices
over the internet, and that the model is as capable as possible.** Anything else
limiting the design is self-imposed and has to justify itself. Decision 78
audited four; these are what a fresh pass found still standing.

| limit | is it real? |
|---|---|
| `hop_accumulate="concat"` is **refused alongside `hidden`** | **Self-imposed, and it costs something measurable.** Decision 116 put `hidden` at 0.45 bits, and `concat` is what lets a readout see every hop — so the best readout and the composition mechanism cannot currently be used together. The refusal says only that "the two have not been made to compose", which is a to-do wearing a constraint's clothes |
| The store is **rebuilt every chunk** | Inherited from the recall tasks, where it is correct, and never re-examined for anything else. `carry_store` exists and its two measurements disagree (item 2) |
| `orthogonal_every` refused alongside `hidden` | **Correct** — it would orthogonalise a different matrix than the one it was measured on. But it blocks re-checking decision 54, which was refuted *because* there was no per-layer structure and now there is |
| Character level | Approved for removal by decision 78 and by John again on 2026-07-28. Still not done; needs its own plan because it invalidates the comparison set |
| `hops` + `context_keys` | **Was** self-imposed past its evidence. Lifted on 2026-07-28 exactly where search supplies the pair-key walk, and it still stands everywhere else |
| `slices_for` refusing uneven splits | Self-imposed. Real machines are not round numbers |

---

## In flight

**Nothing is dispatched.** No sweep matrix is running. The most recent runs are
the pre-commit checks for decision 119.

Newest sweep records, all landed: `g12-01`, `g12-02`, `g12-03` (the asynchrony
window on a real impaired link), `g11-06` through `g11-08`.

### ⚠ An unattributed churn probe landed, and it challenges decision 119

A background probe from a previous session returned while these documents were
being reorganised. Chains, 6 chains at 2 hops, floor 0.167, fraction of the
machine removed down the rows:

    CACHE SLOTS 8          superposed    both    cache only
      0% removed                0.995   0.770        0.082
     75% removed                0.690   0.340        0.045
     fall                         31%     56%          45%

    CACHE SLOTS 128        superposed    both    cache only
      0% removed                0.995   1.000        1.000
     75% removed                0.690   0.915        0.932
     fall                         31%      8%           7%

**Decision 119 says the store wins when bindings exceed slots and *ties* when
they do not. At 128 slots against ~44 bindings this is not a tie** — the cache
holds 0.932 where the store falls to 0.690, and falls 7% against the store's 31%.
Churn is the one axis where the store's degrade-gracefully story was supposed to
be structural, and this points the other way.

**Do not act on it yet, and do not quote it.** Rule 11b: verify a run's identity
from the data before reading a number off it. This output carries **no condition
string, no script name, no seed count, and no record of a pre-registered
prediction**, and it was not launched from this session. It is a number without a
provenance, which is the exact shape of the g9-11 near-miss.

**What it needs, in order:** find the script that produced it; confirm the arms
mean what the column headings say — in particular whether `superposed` is running
with the same width and cap as the other two; then re-run it with a condition
string and seeds. If it survives that, it belongs in the log as a decision and
item 7 below (item- vs dimension-partitioning) moves up the list.

---

## Waiting on John

Listed here because they are calls that are his rather than mine — but per the
standing agreement this is **a report, not a gate**. If he does not answer, I
decide, proceed, and say which calls were made without him.

> **ANSWERED 2026-07-28, and it closes two of the three below.** John, in his
> words: *"I'm good with any functionality and/or adjustments that get us closer
> to our goals. As long as it doesn't contradict with those (primarily being:
> runs on the internet, ideally results in AGI, but works as an LLM replacement
> as a secondary goal [but when they conflict, the AGI goal takes priority])."*
>
> So **search and moving off character level are both approved in advance**, and
> the test for any mechanism is the goals themselves rather than his sign-off:
> does it run over the internet (amended C1), does it serve AGI first. Item 2
> below is no longer a decision — it is a costed piece of work whose only
> remaining requirement is that the re-baselining is planned rather than
> discovered.

1. **Input and output.** He wants to talk this through rather than have it
   decided. His framing: if the AGI goal wins, inputs should look like a body — a
   loop with consequences, not a passive feed. Related work of his own:
   `Mako88/Persistence` (self-curated memory, a sensory block, scheduled
   wake-ups), and a robot project he would like to wire up. The output side is
   where C1 is already violated, so it is not purely speculative.
2. **Moving off character level.** A character bigram table is low-rank because
   English is, so part of the measured ceiling is the task — and concepts cannot
   be represented over characters, which puts it directly against the relational
   direction. **It invalidates every number in the comparison set**, so it should
   happen once, deliberately, with the re-validation costed in advance rather
   than discovered. This one needs its own plan.
3. ~~**`reward_recall`'s layout leak.**~~ **CLOSED 2026-07-28 — John: "if it's
   just a failure in a test (not the model itself), and the test is no longer
   useful, definitely just abandon it."** The leak is real (nearest binding
   before a reward is always the rewarded one, 160/160) and measured **inert**.
   The task is not fixed and not re-baselined. `reward_recall` is retired as an
   instrument: decision 119 showed it does not discriminate the mechanisms the
   g9 line measured on it, and the live work is relational. The three tests in
   `test_reward_recall.py` that pin the leak stay, now as documentation of a
   retired task rather than as a pending fix.

---

## Where the model actually is

Kept short deliberately. Full records are in `experiments/sweeps/`.

**On text** — and the headline here was wrong for a long time:

    uniform                        6.000 bits/char
    OUR MODEL, best ever measured  5.172   g11-07, best of eighteen compositions
    unigram (letter frequency)     4.829   <- NOT beaten, ever
    backprop attention, width 16   4.197   our own baseline, ~10k params
    bigram                         3.583
    char-LSTM (published)          ~1.45

    NOT THE MODEL, and a real result: MLP-128 on frozen features   4.525
    (note 037 — ordinary backpropagation, OFFLINE, deliberately)

**The unigram has never been beaten by this model** (decision 118). A line
claiming `prequential 4.540 ... unigram BEATEN` stood in the handoff for weeks and
was wrong twice over: 4.540 is note 037's offline backprop probe on frozen
features, not the model under its own learning rule, and it is not prequential.
Three independent measurements of the model agree — 5.466, 5.172, 5.665 — and
none reaches 4.829.

**What note 037 does establish is worth more than the mislabelled claim:** the
retrieval *carries* enough information to beat a unigram and a linear readout
cannot extract it. That is a statement about the features, and it is why `hidden`
exists. Whether a LOCAL rule can train such a readout is where note 036 starts.

**On relational tasks:**

    2-hop chain, fixed hops=2                 1.000   (was 0.000)
    3-hop chain, fixed hops=3                 1.000
    depths 1+2+3 mixed, gated                 1.000   on all three
    1-hop model on a 2-hop chain              0.000   <- the control still fails
    depth 3, gated, HALF the machine gone     0.928
    zero-shot transfer to an untrained depth  0.992
    chains linked end-to-start, 4 joins in 6  0.630   <- 1.000 was the disjoint case

**On scale and the wire:**

    token broadcast to all nodes            5 bytes
    each node's reply, combine="vote"       8 bytes
    per answered position, 1024 nodes      ~8 KB

A node's readout spans the whole vocabulary from its own slice, so its argmax is a
*complete opinion*, not a fragment. The binding constraint is **dimensions per
node, not node count**: below ~16 dimensions a node stops having a standalone
opinion, so nodes ≈ width ÷ 16. At width 8192 that is ~512 nodes and ~410M learned
parameters — GPT-2-large scale, not frontier scale. Measured on MQAR at width ≤
128 with no hops; outside that it is extrapolation.

---

## Do not re-propose these

Each has a measurement pinning it. **Read the decision before proposing it
again** — this list exists because several of these were proposed twice.

| proposal | why not | where |
|---|---|---|
| Anything that recovers per-item information *after* the sum | `r = M @ key` is a SUM. Readout bias, competitive retrieval, orthogonal updates and pair keys all failed for this one reason | 69, and the g11 line |
| Another mechanism on top of noisy retrieval | Four have failed against the same 0.915/0.35. Fidelity first | 102, 105, 107, 111 |
| ~~Search / beam over branches~~ | **NO LONGER ON THIS LIST (decision 121).** 111 refused it because the verifier was built from noisy primitives; g13-01 measured the primitive at 1.000 at out-degree 1. The condition expired and search is item 1 | 111, 121 |
| Transfer of the halting gate to new terminator tokens | `halt_w` sits +8.3 sd on one token's value vector. Two markers have unrelated random value vectors, so transfer is **impossible by construction** | 89 |
| A width × sequence-length sweep to explain "width doesn't help" | Nobody claims that. Our arms *do* scale with width; the flat axis is DATA. Withdrawn before dispatch after ten minutes of reading source | 112, 113 |
| More data on the text corpus | The model converges at ~16,000 characters. The store is per-sequence working memory, so `Wo` is the only durable parameter and one linear map converges fast | 63, 115 |
| Store or readout capacity as the saturation cause | ~96 bindings at d=64 scaling as d²; 2.00 readout items per dimension. Both exceed what the tasks demand | 109, 110 |
| `value_centre`, or `value_lr` as a fix for collapse | `value_lr` does not collapse at a sane rate. The values move a long way, stay spread out, and the plateau does not budge | 114 |
| Replacing the superposed store with a cache | The store wins by a factor of eight when bindings exceed slots, and ties when they do not — **but see the churn probe below, which challenges the "ties" half** | 119 |
| A composition sweep on chains as evidence about composition | A chain has **out-degree 1 by construction** — the row that already scores 0.915. Every composition result on chains was measured where no search was needed | 108 |

---

## Working agreement with John

- **Blanket permission for architectural decisions.** The pending-decisions list
  is a REPORT, not a gate. If he does not answer, decide and proceed — document it
  in DECISIONS.md and say which calls were made without him.
- **List pending decisions at the end of every response.** He reads from a phone.
- He is not deeply versed in modern ML internals. **Explain plainly, keep the
  numbers, do not hide bad news.**
- **Goal ordering:** AGI is primary; being an LLM replacement that runs on
  distributed consumer machines is secondary and must not compete with it.
- **Biology gives policies, not representations.** Biology has been a good source
  of control policies here (tagging and capture) and a poor source of
  representations (superposition, Hebbian outer products, frozen random
  projections). Take mechanisms from computer science where the problem is
  well understood.
- **Scheduled wake-ups DO NOT FIRE in his setup.** He phones into a desktop
  session, which keeps it non-idle; cron never fires, and `ScheduleWakeup` was
  tried and also did not. **What works is a persistent `Monitor`** emitting a
  heartbeat line. Do not end a turn relying on anything else.

## Standing operational rules

- Sweeps are GitHub Actions **dispatch-only** via `gh workflow run`, one matrix at
  a time, cost stated first and estimated **from the most expensive cell**.
  Nothing heavy runs locally.
- **Never use bash heredocs.** **Never `git commit -m` with backticks** — write
  the message to a file and use `git commit -F`.
- **Run `python tools/check_all.py` before every commit**, then `mutate.py
  --changed` separately. **Do not run the checks as one compound shell command** —
  a shell reports only the last statement's exit code, and on 2026-07-28 that
  reported success while two of the five were failing. `check_all.py` runs each
  as its own subprocess and fails if any fails.
- **Batch commits when a sweep is in flight** — every push queues seven check jobs
  ahead of the matrix, and a second push cancels the first run.
- **Mutations run in CI, not locally** — John, 2026-07-29, and there was no
  excuse: `.github/workflows/checks.yml` has sharded them six ways since before
  this rule existed. `--changed` after touching `local_memory.py` selects **80 of
  the 165 mutations**, each re-running an 85-second suite, which is roughly two
  hours during which nothing else can be edited or measured. Run locally only
  when iterating on **one or two specific mutations**; everything else is a push.
- **The mutation harness takes the tree exclusively.** Stopping the background
  task does not stop it: that kills the shell wrapper and leaves the Python
  process editing source. Two full check runs once passed against a tree that was
  still being mutated.

  **To actually stop one:** kill the `python tools/mutate.py` processes AND the
  `unittest discover` child by PID (`taskkill //F //PID ...`), then
  `git checkout --` the mutated file and confirm with `mutate.py --verify`, which
  prints `source clean: all N originals present`. A run killed mid-swap leaves a
  live mutation on disk and `git status` showing one modified file — which looks
  exactly like ordinary uncommitted work.

## The standard this project holds itself to

Pre-register predictions before every sweep and score them honestly, including the
refuted ones. A mechanism measured only on the task it was designed for is not
measured. When a mechanism adds state, compare against a model given the same
amount of state — g10-09 was retracted for missing exactly that.

**Probe the bottom of a scaling range locally before spending a matrix on it.**
g11-05 swept 62,500 characters upward, entirely above the model's saturation
point, so its flat exponent was guaranteed by the grid.
