# 27. Somebody already built the ruler

*John asked: are there standard tests for prototypes our size? We're probably
not the first to need these test shapes.*

*He was right, and the answer stung a bit.*

---

## What a test is for, here

This project can't be judged by "does it write good essays." It's far too small.
So instead there's a ladder of tiny tasks, each one asking a single question:
can it hold a fact for four steps? For twenty? Can it still do it when the
network drops half its messages?

The tasks are deliberately toy-sized. The bet is that a toy task can *predict*
what a big one would do — if the small version fails at twenty steps, the big
version has a problem at twenty steps too, and you found it for a hundredth of
the cost.

That bet is the whole reason the ladder exists.

## The first surprise: one of ours isn't ours

The task we've leaned on hardest is called MQAR. It shows the model a list of
pairs — *cat→7, tree→3, lamp→9* — then a pile of unrelated words, then asks
"cat?" and checks whether it says 7.

I've been treating it as a house instrument. It isn't. It comes from published
work on recall in efficient language models, and it was built for exactly the
reason we use it: a small synthetic whose behaviour predicts the big-model case.

So the worry that quietly sat under a lot of this — *is a tiny made-up task even
legitimate evidence?* — was answered by other people before we showed up. It is
legitimate. That's the point of it.

## The second surprise: the ladder exists too

DeepMind published something called **bsuite**. Their description of it is,
almost word for word, the thing written at the top of our gate ladder:
targeted unit tests, each isolating one core capability, with difficulty that
**varies smoothly** rather than being one fixed challenge.

That's the ladder. That's the dials. Arrived at independently, which is mildly
reassuring about the reasoning and mildly embarrassing about the reading.

And then it gets more specific. One of their tests is **Memory Length**: a
T-maze where the agent sees a signal, walks some number of steps, and has to
still know the signal at the end. The number of steps is the dial.

That is `reward_recall`. The task built here two weeks ago, from a five-point
list of requirements written out from first principles. Show a fact, wait a
configurable number of steps, then let a late signal say whether the fact
mattered. Same shape. Theirs is better specified.

## The bit that's actually funny

The bsuite paper explains what its credit-assignment tests are for, and picks an
illustration:

> an algorithm might completely fail at credit assignment beyond n = 20 steps

Our gate works at delays 1, 4 and 8, and goes *negative* at 20.

The number is a coincidence — twenty is a round number and it's their example,
not their result. But the *shape* isn't a coincidence. Their instrument was
built to produce exactly the kind of finding we produced. Which is a decent sign
the finding is real, and a very clear sign we should have looked first.

## Why we still can't just run it

bsuite assumes an **agent**: something that takes actions and has a policy for
choosing them. This project has neither. There's a memory that stores things and
gets asked about them; there's nobody in there deciding what to do.

Run bsuite against this and you'd measure the absence of a policy, elaborately.

So the honest position is: the *task shapes* transfer, the suite doesn't. Worth
taking is the framing — describing the delay dial the way they describe theirs,
so our numbers can be read next to a literature instead of only next to
themselves.

## The pattern, which is the real finding

This has now happened three times.

1. **Tagging and capture** — the biological mechanism was half-built here before
   the papers were read properly. The reading changed the design.
2. **The capacity equation** — `SNR = sqrt(d/N)` was derived from our own
   sweeps, then checked against a published bound many months of work later. The
   bound agreed *and* named a term we had never varied.
3. **This one** — a task built from a requirements list, where the requirements
   list turned out to be a description of a test that already exists.

Every time, the version in the literature was better specified than ours.

And the fix is embarrassingly cheap: **a list of properties a thing must have is
a search query.** Write the requirements, then search, *then* build. Doing it in
that order costs an afternoon. Doing it the other way costs the build twice —
once to write it and once to reconcile it with what you should have started
from.

That's now a rule in CLAUDE.md rather than a lesson I keep re-learning.

---

*Previous: [26. Three wrong answers and a right one](26-three-wrong-answers-and-a-right-one.md)*
