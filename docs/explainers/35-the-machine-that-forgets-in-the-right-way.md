# The machine that forgets in the right way

The last explainer ended with a promise: the counting trick worked on one
computer, and the whole point was that it should work on many. This is what
happened when we actually spread it out.

## First, the thing we had been quietly avoiding

Every test so far ran inside one program. When two "machines" needed to tell each
other something, that was a function call. The message was *counted*, not sent.

We said so every time, because a program pretending to be a network is exactly
the kind of thing that looks like it works and is doing nothing. But saying so
does not fix it.

So: four containers, a real network between them, real packets. Each one holds
only its own share and refuses to answer questions about anybody else's. Then the
same data is run the old way in one program, and the two are compared.

**They match exactly.** Then we made the connection bad on purpose — a fortieth
of a second of delay each way, with wobble — and they still match exactly.

## The bill for doing it the simple way

The same work took **two and a half seconds** on a good connection and **four
minutes** on the slow one.

That is not the network being slow. It is us opening a fresh connection for every
single message, which was a deliberate shortcut to keep the first version simple.
It is now a measured cost rather than a footnote, and fixing it is a known job.

The number is written down with a warning attached: **it prices the shortcut, not
the design.** Quoting it as "how slow this architecture is" would be wrong.

## Then a machine left, which is supposed to be normal

This project assumes computers switch off constantly. So the question is never
"does it survive" but "what does one departure cost".

Here is the awkward part. The older part of the system kept three copies of
everything. This part keeps **one**. When a machine goes, everything it knew is
gone for good, and nothing anywhere has a spare.

And it is worse than it first sounds, because of a second effect that is easy to
miss. To judge whether two things belong together, a machine has to ask how common
the *other* thing is — and if the machine holding that answer has left, nobody can
judge it. **So a departure damages things it never held.**

Which raises a genuinely uncomfortable question. The whole point of a concept
having several appearances — a picture, a sound, a word — is that you can reach it
several ways. Does that make it *more* robust when a machine leaves, or *less*?
More ways to reach it, or more ways to lose a piece of it?

## We measured the wrong thing first

The first answer looked bad. Counting how many concepts were *touched* by a
departure, more appearances was clearly worse: with five appearances each, losing
one machine of eight touched nearly half of all concepts, against a fifth when
concepts had only two.

That reads like a warning. It would have meant this project's commitment to
multiple modalities fights its commitment to surviving churn.

**It was the wrong question.** "Touched" counts a concept as damaged if a single
one of its appearances went — even if four others survived and it is still
perfectly findable.

So we asked a better one: after the dust settles, can each surviving appearance
still reach at least one of its own?

```
appearances per concept    one machine lost    HALF the network lost
                     2               0.89                     0.55
                     3               0.99                     0.82
                     5               1.00                     0.96
```

**More appearances is dramatically better.** With five, losing *half the network*
leaves 96% of survivors still connected to their own concept.

The reason is obvious once you see it and invisible before: a thing needs only
**one** surviving partner to remain findable. Five appearances give it four
chances; two give it one.

## So the copies were there all along

We had been treating "keep spare copies of everything" as a missing feature.

It turns out the several appearances of a concept *are* the spare copies. Nobody
designed that; it falls out of a concept being a pattern across several things
rather than an entry in a list.

Keeping real spares would still help — the two-appearance case is genuinely
fragile — but it moved from *"we must build this"* to *"this would be an
improvement"*, which is a much smaller worry.

## A note on how nearly this went wrong

The bad-looking answer and the good-looking answer came from the same run. The
difference was which question got asked.

We nearly published the bad one. What stopped it was noticing that the reassuring
number could not be trusted either: the scoring method had a hidden floor that
moved depending on how many appearances a concept had, so a column of numbers that
looked flat was actually three different scales printed side by side.

Neither reading was safe. So a new measurement was written that means the same
thing at every size, the prediction about it was written down **before** running,
and then it was run.

That is slower than picking whichever number tells a better story. It is the only
way to end up with an answer rather than a preference.
