# The difference between "seen again" and "wanted again"

A machine with a small memory watching an endless stream has to decide, moment by
moment, what to keep. It cannot keep everything. So it needs a rule that looks at
what just arrived and says *yes, worth remembering* or *no, let it go*.

Getting that rule right is one of the things this project has to prove it can do.
And it has been stuck.

## Why it is stuck

There is a number that says how good the rule has to be.

If most of what goes past is worth keeping, the rule barely matters — keep
everything and you are mostly right. If almost none of it is worth keeping, the
rule has to be very sharp indeed, because one mistake in the wrong direction fills
your memory with junk.

On the test we built, almost none of it is worth keeping: about **one position in a
hundred**. To end up with a memory that is even half useful stuff, the rule has to
fire on the good material roughly **ninety times** more often than on the rest.

The best rule anyone here has found fires about **eight times** more often.

That is not close. It is not a matter of adjusting a dial. And it has held item
five of the project's checklist — *can this thing learn forever?* — stuck for a
while.

## The idea that would have unstuck it

Here is the thing about that "one in a hundred": **we chose it.**

The test is one we wrote ourselves. We decided how much padding to put in it. So
the ninety-times bar might not be a fact about streams of information in general.
It might just be a fact about a test we made up.

Nobody had ever checked, because checking looked impossible. To know how much of a
real stream is worth keeping, you would need someone to have gone through it and
labelled every position — and real data does not come with labels. Only our own
made-up data does, because we made up the labels too.

So the idea was to find a definition that needs no labels at all:

> **A thing is worth remembering if you need it again later.**

That sounds obviously right, and it is checkable by counting. Go through the
stream, and for each thing, look ahead: does it come up again? If yes, keeping it
would have paid. If no, it was waste.

No labels. Works on anything. Cheap.

## Why it does not work

It was tested against the case where the answer is already known — our own made-up
data, where the labels exist and say *one in a hundred*.

The counting method looked at the same stream and said **ninety-nine in a
hundred**.

Not slightly off. Backwards, near enough, and off by a factor of about a thousand.

The reason is a distinction that is easy to miss and turns out to be the whole
thing. **"Comes up again" and "is needed again" are not the same.**

Think of a page of text. The letter *e* comes up again constantly. That does not
mean writing down every *e* you see is useful — nobody is ever going to ask you
what the four hundredth *e* was. It recurs, but nothing depends on it.

Our padding is like that. It is drawn from a small pool, so it repeats endlessly.
By the counting rule, nearly every scrap of padding scores as *worth keeping*,
because it will certainly show up again. It is just that nobody will ever want it.

Making the definition stricter does not help. It was tried at four levels of
strictness and the gap never closed.

## What is left, and it is more useful than the table would have been

The lesson is not "that definition was badly chosen." It is something firmer:

> **Whether a thing is worth remembering is a fact about the future demand on it,
> and no amount of looking at the thing itself will tell you.**

You cannot read it off the symbols. Counting cannot get there in principle, not
just in this attempt. Which leaves two honest routes:

- **Use data that says out loud what it will ask for.** That is our own made-up
  test, which is where we started, and it brings the made-up bar back with it.
- **Take the thing away and see what breaks.** Remember it, then don't, and
  compare. That measures demand directly instead of guessing at it.

The second is more work. It is also the only one that answers the question, so it
is what happens next.

## The part that cost nothing, and why

This was found in a single run, and not by luck.

Before the new method was allowed to say anything about real data, it had to
reproduce a number we already knew — the ninety-times figure, on the data where
the labels exist. It read ninety-two. That part was right.

Then the same method, on the same stream, counting instead of reading labels, read
a completely different answer. That is what killed it.

Without that requirement, the method would have gone straight to the real data,
produced a confident-looking table, and the table would have been wrong in a way
nothing later would have caught — because there would have been nothing to check
it against.

**A new way of measuring has to reproduce the old way before it is allowed to
disagree with it.** That is not bureaucracy. It is the difference between one
wasted afternoon and a wrong number sitting under everything built afterwards.

## And a smaller catch inside the bigger one

The check that saved this nearly failed itself.

The first version of it read **twenty-three** instead of ninety-two. It was
counting a slightly different set of positions than the original figure counted —
close enough to look right, different enough to be wrong.

It was caught only because there was a published number to hit. If the check had
been written without one to compare against, twenty-three would have been
believed, and every later comparison would have been made against a bar that was
four times too low.

The same mistake had been made one experiment earlier, in the same way, by the same
kind of reasoning. Two things with similar names turned out to be different things.
That keeps happening, and the only reliable defence found so far is boring:
**always have a number you are required to reproduce.**
