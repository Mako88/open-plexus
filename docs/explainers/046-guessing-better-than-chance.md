# The system learned to guess, and the guess was good

For the first time, something in this project **learned** what relationships mean
well enough to fill in a rule nobody taught it — and it beat guessing.

That sentence has a lot of caveats attached, and they are at the bottom. But the
result is real and it is the first of its kind here.

---

## The problem

The system answers questions by walking a chain. *Who is Ana to Cara?* It finds
Ana is Ben's mother, Ben is Cara's father, and combines those two steps into one
answer.

Combining steps needs a **rule**: mother-then-father gives grandmother. The system
learns those rules from examples.

But it will always run into a combination it was never taught. Then it has to
guess.

## Why guessing is a surprisingly high bar

You might expect a random guess to score near zero. It doesn't — **random guessing
scores 0.664** on this test.

The reason is that family relationships are heavily constrained. There aren't that
many possible answers, and a wrong guess often still lands somewhere the rest of
the chain can recover from. So beating random is harder than it sounds.

**The previous attempt at a smarter guess scored 0.5995 — worse than random.**
That is the thing this had to avoid repeating.

## What was tried

Give every relationship a set of numbers — a "vector" — that captures how it
behaves. Learn those numbers from examples using a simple local rule:

- When two relationships combine into a third, **pull** the combination toward the
  right answer.
- **Push** it away from all the wrong answers.

That second half is the important one. Learning only from correct examples tends
to produce something that says everything is similar to everything. You need the
wrong answers too.

**Everything that rule needs comes from a single example on a single machine.** No
central coordinator, no waiting for other machines, no global view. That matters
because it is the whole point of the project.

## The result

Ten runs. The score is how often the final answer is right:

```
refuse to guess              0.596
guess at random              0.664
guess deliberately wrong     0.633
the learned guess            0.782
exact mathematical solution  0.965
```

**0.782 against 0.664.** It won on all ten runs out of ten.

It closes about **39%** of the distance between guessing and the exact solution —
where the exact solution is a piece of algebra that only works because family
relationships happen to have a tidy mathematical structure.

## Why this one is more believable than usual

**The prediction was written and committed before the code existed.** Not after
the numbers came in. Anyone can check the order in the project history.

**The failure was made to happen on purpose.** If you let the system peek at the
rules it is being tested on, the score jumps from 0.244 to 0.419 on a related
measurement. That leak is now a permanent automated test — if someone breaks the
barrier, the score nearly doubles and the test catches it rather than everyone
celebrating.

**There is a deliberately-wrong arm.** Guessing badly on purpose scores 0.633,
*below* random. That tells you a confidently wrong system is worse than a random
one here — so scoring under the bar would not be a near miss, it would be evidence
of something actively broken.

## The caveats, and they are not small

**Family relationships are unusually tidy.** They have a neat mathematical
structure — each relationship moves you a fixed number of generations up or down.
That is exactly why the "exact solution" arm scores 0.965. We already measured
that a large real-world knowledge base has **no such structure at all**, not even
approximately. So this may be a result about families rather than about learning.

**The chains were handed over correct.** This test gives the system the right
chain of steps and only asks it to fill gaps. Making the system find its own
chains costs about 0.11 elsewhere, and that has not been run here.

**One number disagrees with the record and nobody knows why.** The previous
measurement put random guessing at 0.608; this one measures the same thing at
0.664. The comparison that matters is internal — everything was measured in the
same run with the same settings — but a figure the project cites as a target
disagreeing with a fresh measurement is exactly the sort of thing that has bitten
us before. It is written down, unexplained, rather than smoothed over.

## What it means

The project's central claim is that a system can learn using only information
available where it sits, with no global coordination.

Until today, everything that durably learned here was a single output layer, and
on one test our learning rule scored **worse than always guessing the commonest
answer**.

This is the first evidence that a representation — a learned notion of what a
relationship *is* — can be built from local information and be good for something.

It is one domain, one task, and a friendly one. But it is the first time the
answer has been yes.
