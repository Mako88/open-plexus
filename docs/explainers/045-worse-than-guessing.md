# Our own learning rule did worse than guessing

We built a test for ourselves, ran it properly for the first time, and our
learning rule came out **below the score you get by not thinking at all.**

That is the finding. This page explains what the test was, what "below guessing"
means, and why it is useful news rather than just bad news.

---

## The test

The system is shown a stream of facts about a small group of people. Something
like:

    Ana is Ben's mother
    Ben is Cara's father
    Ana is Cara's grandmother

The first two are **stated** — somebody told us. The third is **entailed** — it
was never stated anywhere, and the only way to know it is to put the first two
together.

Nothing in the stream marks which is which. They all look identical. That is the
whole point of the design: if you score the stated ones and the entailed ones
separately, you have split **remembering** from **working it out**, without ever
telling the model which kind it is looking at.

The entailed half is the real test. Remembering is easy. Working it out is the
thing this project exists to find out about.

---

## The four contestants

    always guess          just answer whichever relationship is commonest
    untrained             our system with the learning switched off
    our learning rule     our system, learning
    a conventional model  standard machine learning, every advantage given

The last one is not a rival. It is a **ruler**. It tells us how much of this task
is possible at all. If even it does badly, the test is impossible and nothing
measured on it means anything.

---

## The scores

Eight independent runs, and here is the entailed half — the working-it-out half:

    always guess          0.190
    untrained             0.000
    our learning rule     0.108
    a conventional model  0.282

Read the first and third lines together. **Always guessing scores 0.190. Our
learning rule scores 0.108.**

It would do better having learned nothing and just guessing.

---

## What that actually means

It is tempting to read a low score as "it is nearly there but weak." That is not
what this is.

Scoring *below* the guessing line means the system is not making faint, mostly
right decisions. It is making confident decisions that are **pointed the wrong
way**. The learning is not too little — it is pushing in a direction that costs
accuracy.

An analogy: if you knew nothing about horses and picked winners at random you
would have some hit rate. Scoring below that is not ignorance. It means you have
a theory, and your theory is worse than a coin.

---

## Why this is useful news

**We found it on purpose, on a test built to make it findable.**

The stream deliberately mixes stated and entailed facts with no marker, so the
system cannot quietly do well by only remembering. And the four contestants
include a "learned nothing" control and a "guess the commonest" floor. Without
those two lines on the table, 0.108 would look like modest progress.

This is exactly the trap the previous version of this project fell into and lost
about a year to: measuring a learning rule against a bar that was never there.

**It also points somewhere specific.** Almost nothing in our system currently
learns. One single layer at the very end adjusts itself; everything underneath is
frozen at random values and never changes. So the honest description of the
result is not "learning does not work here" — it is "there is almost nothing here
that *can* learn, and the one thing that can is not enough to compose two facts."

That is the next thing to work on, and now it has a number attached instead of an
opinion.

---

## The other half of the story

The ruler — the conventional model — scored 0.282 against the guessing line's
0.190.

Before the run, we wrote down a prediction that it would beat guessing by more
than 0.15. **It beat it by 0.092.** So that prediction is refuted, and it matters,
because it means there is less room on this test than we thought. The gap between
"guessing" and "the best anyone could do here" is fairly narrow.

We had also been quoting a much bigger number — 0.28 — as this test's headroom.
That is the distance from the *untrained* control, which scores a flat zero. A
control that gets nothing right at all is a floor the way a wall is a floor.
Measured against the honest floor, there is about a third as much room as we had
been saying.

Both numbers are now written down side by side, rather than the flattering one
being kept.

---

## And one process note

For a while, two of our main documents described this test as passed, quoting a
score from **a single trial run** done before the real experiment was dispatched.
The experiment's own file still said "answer: pending" the entire time.

When the real eight-run version finally went, the numbers held — nothing moved by
more than 0.011. So this cost nothing.

But it held by luck rather than by anything checking. Nothing in the project
notices when a result that says "still pending" gets quoted as finished. That is
now written down as something worth checking automatically.
