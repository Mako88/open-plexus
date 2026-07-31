# How two machines notice the same moment

A child learns that a picture of a dog, the sound of barking, and the word *dog*
are all one thing by meeting them together, over and over. Nobody hands them a
list. The world just keeps presenting the three at once until the connection is
obvious.

We want a machine to learn the same way. There is one problem, and it is not the
one you would expect.

## The problem

The picture and the sound do not arrive at the same machine.

This system is spread across many computers. One of them is handling images.
Another is handling audio. Neither can see what the other is doing, and — this is
the hard constraint the whole project is built around — **neither is allowed to
ask.** Asking means waiting, waiting means the slowest machine sets the pace, and
that is exactly the thing that forces ordinary AI into data centres.

So: how do two machines that cannot talk to each other agree that what they each
saw happened at the same moment?

## The answer is a clock

Both machines look at the time and round it off. Ten twenty-three and fifteen
seconds. They both do the arithmetic, and they both get the same answer, because
arithmetic on the same number gives the same result everywhere.

That rounded time becomes an **address** — a place to leave a note. Both machines
send theirs to whichever computer is responsible for that address.

Nobody asked anybody anything. They just both did the same sum.

## What happens at that address

One machine is now holding all the notes from that second:

> *a picture of something furry, a barking sound, the word "dog", a sofa, a face*

It writes down every pairing — the picture goes with the sound, the picture goes
with the word, the picture goes with the sofa — and sends each pairing off to be
filed under the thing it is about.

**Then it throws the whole lot away.**

That last part is the bit people find surprising, so it is worth being blunt about
it: **the time slot is a meeting place, not a filing cabinet.** It exists so two
strangers can discover they were looking at the same instant. Once the pairings
have been filed, it has done its only job and it is gone.

## Where the real work happens

Each individual thing — that specific picture, that specific word — has its own
permanent home on some machine. And that home keeps a tally.

After a few months, the tally for the picture might read:

    the barking sound      appeared together 400 times
    the word "dog"         appeared together 380 times
    a face                 appeared together  12 times
    a sofa                 appeared together   3 times

Here is what that tally solves.

**Any single moment is nearly useless.** When somebody said "dog," there was also
a sofa in the room, and a face, and a window. One snapshot cannot tell you which
of those is the dog. All of them were there.

**But the sofa was only there once.** The barking was there every single time. The
thing that shows up *whenever* the picture shows up is the thing that belongs with
it. The rest fades on its own, without anyone deciding to delete it.

That is not a clever trick. It is roughly what happens to a person who hears a
word in a hundred different rooms: the rooms cancel out and the meaning is what is
left.

## So where is the concept?

Nowhere. There is no file called "dog."

There is a picture with a tally, and a word with a tally, and each tally points
hard at the other. Ask about the word, follow the strong links, and you arrive at
the picture. That cluster of things that all point at each other **is** the
concept — a shape in the connections, not an entry in a list.

Which means nothing has to decide what a concept *is*, or give it a name, or get
every machine to agree on that name. That turns out to matter enormously, because
getting scattered machines to agree on names is one of the genuinely hard problems
in distributed computing, and this design simply never has to.

## What would show this is wrong

Put something in the room *every single time*. A distractor that never leaves.

By the counting rule above it is indistinguishable from the real thing — it
appeared just as often, so it scores just as highly. If the system cannot separate
them, then watching is not enough, and the missing ingredient is **doing**: pick
the thing up and see what moves with it.

That test needs no cameras and no microphones. It can be run on made-up symbols in
a few minutes, which is what makes it the first thing worth trying rather than the
last.
