# The word "the" ruins everything

The last explainer described how a machine could learn that a picture of a dog, a
bark and the word *dog* are one thing: keep a tally of what turns up with what,
and after enough occasions the things that always travel together stand out.

We built it and ran it. Here is what happened.

## The tally is beaten by anything that is simply always there

Put something in every single room — a hum, a wall, the word *the* — and the
tally gets it wrong. Not slightly wrong. The ever-present thing turns up with the
picture more often than the bark does, because the bark is only there sometimes
and the wall is always there. So the tally says the picture means the wall.

This was the test we wrote down in advance as the one that could kill the design,
and it killed the design as written.

## The fix is one question, asked differently

Stop asking *how often did these two meet*. Ask *did they meet more often than
you would expect by luck*.

Something present all the time meets everything by luck. It meets the dog picture
constantly, but it also meets the kettle, the cat and the front door constantly,
so meeting the dog picture tells you nothing. Score it that way and it drops to
exactly zero — not "a bit lower", zero, because it genuinely carries no
information.

With that one change the machine recovered every concept perfectly. The wall
scored nothing. The bark scored everything.

**And the change is affordable.** To ask the new question the machine needs to
know how common the *other* thing is — one extra question, to one specific
machine that already knows the answer. That is allowed under this project's rules.
What is not allowed is asking everybody, and this does not.

## Then the interesting part

We ran a second test to find out how many times a machine has to see something
before it can learn it. The answer is **about sixteen occasions** — much fewer
than expected. Four or five gets you most of the way.

But that test found something we were not looking for, and it is the real news.

We made some concepts common and others rare, which is how every real world
works: a handful of words get used constantly, and most words are rare. The tally
fell apart — and not because the rare things were seen too few times, which would
have been dull and fixable by running longer.

It fell apart because **a common enough thing becomes the wall.**

Here is the actual case. One concept came up as the subject exactly *zero* times
in eight thousand occasions. Its picture still showed up 129 times, drifting
through the background of other people's occasions. And what did the tally think
that picture meant? The three surfaces of the single commonest concept in the
world — met 57, 57 and 62 times — against its own true partners at once each.

Nobody built a distractor. The frequency distribution built one.

## Why that matters more than it sounds

It means the two problems are one problem. We thought we had a special case — a
deliberately planted always-present nuisance — and a separate, vaguer worry about
lopsided worlds. They are the same thing. Anything common enough behaves exactly
like a planted nuisance, and the same single fix handles both.

That is good news about the fix and bad news about the difficulty: the fix is not
an optimisation to apply if there is time. Without it, in a realistically lopsided
world, the machine recovers essentially nothing for most of the concepts there
are.

## The catch, stated plainly

Asking *more often than luck* costs something. When the world is even and there
is no nuisance, the plain tally is actually **better** — it needs fewer sightings
to get there, because the clever question involves dividing one estimate by
another, and dividing two shaky numbers gives you a shakier one.

So neither is the right answer everywhere. Plain counting wins when things are
easy; luck-corrected counting wins the moment anything is common. Both are kept.

## What this does not show

Everything above ran on one computer.

The entire reason for this design is that the picture and the sound arrive at
*different* machines, and the whole difficulty is doing this without them being
allowed to ask each other anything. None of that was tested here, and a pass on
one machine is not a pass across many.

It was done this way on purpose, and the logic is worth stating because it is the
only reason the shortcut was acceptable: spreading the work across machines can
only ever *lose* information — a moment split across a boundary, a message
arriving late, a machine leaving with its share of the tally. It can never add
any. So a method that fails with everything in one place and perfect information
would certainly fail spread out. Failure here would have been conclusive.

Success here is not. That test is still to come.

## Also worth saying

One of our own measurements had a floor we had not noticed. We predicted the
scrambled control — the version with all the real structure destroyed, which
should score terribly — would come out near zero. It came out around 0.35, and a
machine that groups *nothing at all* scores 0.5.

So a score of 0.6 on this scale is not "moderately good". It is barely off the
bottom. Every number we report from it now says what the bottom is.
