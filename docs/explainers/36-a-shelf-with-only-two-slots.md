# A shelf with only two slots

Three separate experiments this week ran into the same wall from three different
directions. It took all three before it was obvious what the wall was, and the
answer is simpler than any of the individual results.

Here it is up front: **each thing in this system keeps only a handful of
connections, and that handful is a shelf with a fixed number of slots.** Almost
everything that has gone wrong recently is something being pushed off that shelf.

## What the shelf is for

The machine learns by noticing what turns up alongside what. But *everything*
turns up alongside *something* eventually, so if it kept every connection it ever
noticed, everything would end up connected to everything, which is the same as
knowing nothing.

So it keeps only the strongest few. It works out how many by looking for the
point where the scores fall off a cliff — three strong connections and then a big
drop means keep three. Nobody sets the number; each thing decides for itself.

That is a good design and it still works. The trouble is what happens when a shelf
is full.

## The first way we hit it: a word with too many things to name

Imagine the word *dog*. It has to connect to pictures of dogs, sounds of dogs,
videos of dogs. But every picture of a dog only has to connect to one word.

So the word needs a big shelf and the picture needs a small one. If we give
everything the same size shelf, one of those two is wrong. Make it small and the
word can't reach all its pictures. Make it big and every picture starts collecting
junk.

Letting each thing choose its own shelf size fixed most of this. But it does not
create more shelf — it just stops us picking one wrong number for everybody.

## The second way: showing two things at once pushes the word off

Here is the one that surprised us.

We gave the machine a picture of a digit and a recording of someone saying that
digit **at the same moment, every single time**, plus the written word.

You would expect that to help. More evidence, same concept.

It made things worse. And when we looked at why, the picture's shelf had the
*sound* on it and nothing else. The word was gone.

The reason is almost obvious once you see it. If the picture and the sound always
arrive together, they are each other's most reliable companion — perfectly so. The
word is only *usually* there. So the sound wins both slots and the word is pushed
off the end.

**The picture learned what it sounds like and forgot what it is called.**

Show them at *different* times instead and the problem vanishes entirely. The word
stays, and — this is the good bit — the picture and the sound still find each
other, through the word, even though they have never once appeared together.

## The third way: turning down the correction

Then the owner of this project spotted something real. The machine has ten words
but a hundred picture-and-sound codes, so each word turns up about fourteen times
as often as any single code — not because words matter less, but because ten
things are sharing work that a hundred things share elsewhere.

The machine corrects for how common something is, so that a thing which is always
around doesn't win by being always around. That correction was necessary; we
measured it. But it reads the word's commonness as unimportance.

Good catch, and exactly the right question: **can we just turn the correction
down?**

We built a dial and tried it. The answer is no, and the way it failed is the
interesting part.

Turning the dial down *did* put the word back on the picture's shelf. But nothing
got connected, because connection needs the feeling to be **mutual** — the word
has to be on the picture's shelf *and* the picture on the word's. With the
correction turned down, every shelf everywhere filled up with the one thing that
is always present, and nothing was mutual with anything. The machine didn't blur
things together. It fell apart into a thousand separate pieces.

We had predicted it would blur. It did the opposite.

## So all three are one thing

- A word can't reach all its pictures — **its shelf is full.**
- A picture forgets its name when it always hears a sound — **its shelf is full.**
- Turning down the correction breaks everything — **every shelf is full of the
  same wrong thing.**

Three findings, one shelf.

And that reframes what to try next. We spent a while looking for a better *scoring
rule* — a cleverer way to decide which connections are strong. None of that
changes how many slots there are. **No amount of rescoring makes a shelf bigger.**

## What we think the answer is, and have not built

If a word genuinely needs more slots than a picture does, then perhaps the shelf
size should depend on **what kind of thing it is**, not just on that thing's own
scores.

A word is a name for things. A picture is one thing. Those are different jobs and
it is not obvious they should get the same allowance.

That is untried. We are recording it as the first idea in this line that the
measurements actually *push us toward*, rather than one that merely survives them
— which is a distinction worth keeping, because most ideas are the second kind and
it is easy to mistake one for the other.

## The one that stayed good

Worth ending on, because it is the strongest result here and it is easy to lose
behind three failures.

**Two senses that have never once appeared together can still find each other,
through a shared name.** A picture the machine has only ever seen on its own, and
a recording it has only ever heard on its own, end up correctly connected.

That is the whole point of the project's answer to how a concept holds itself
together across different senses. It works, on real photographs and real
recordings of real people, and it works better than showing them together does.
