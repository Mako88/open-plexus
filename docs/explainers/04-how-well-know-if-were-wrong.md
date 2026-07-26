# 4. How will we know if this doesn't work?

Most projects define success. This one also defines **failure**, on purpose and
in advance.

The reason is simple: a goal you can't fail is a goal you can't test. "Build
great distributed AI" can absorb any result. "This specific thing should happen,
and if it doesn't, we were wrong" cannot.

So the project is arranged as six gates. Each one asks a question, and each one
names the answer that **kills the project** at that stage.

The formal version is [GOALS.md §4](../../GOALS.md).

---

## The six gates

They're deliberately ordered **cheapest to find out first** — so if we're wrong,
we find out in a month instead of after two years.

| | question | we were wrong if |
|---|---|---|
| **0** | Do we even have a fair test? | We can't build one. |
| **1** | Does it learn at all? | Local-only learning does nothing. |
| **2** | Does it survive slow, out-of-order messages? | The benefit vanishes at real internet speeds. |
| **3** | Does it survive machines leaving? | Losing one machine damages the whole, not a part. |
| **4** | Does it fit down a home internet connection? | It needs more bandwidth than people have. |
| **5** | Does it get better as more people join? | The benefit shrinks with size. |

Gate 5 is the one that matters most for the actual dream, and it's last because
it's the most expensive to reach. Gates 2, 3 and 4 are the three rules from
[explainer 3](03-the-three-rules.md), turned into tests.

---

## Gate 0 is the interesting one

**"Do we even have a fair test?"** sounds like paperwork. It is the single most
expensive mistake the previous version of this project made, and it's worth
understanding properly, because it's a trap that looks nothing like a trap.

### The trap

Here's a strange fact: **a completely random, untrained network is already
surprisingly good.**

Wire up a big tangle of connections at random. Don't train it at all. Attach a
simple reader to the output. That thing can already do a lot of useful tasks —
well enough that it's a whole respected field of study.

Now think about what that means for testing.

If your test is one that a random tangle already scores 80% on, and the best
imaginable system scores 99%, then **there is only 19% of room in which your
learning could possibly show up.** And if a couple of simple non-learning tricks
grab a chunk of that, your actual learning rule is fighting for scraps.

It's the same as giving a test so easy that a coin-flip gets 80%. Your best
student scores 85%, your worst scores 82%, and you conclude that teaching doesn't
work. **The teaching might be fine. The test can't see it.**

### What actually happened

In the previous project, the random untrained version scored **0.802**. Total
room between that and a strong conventional system was about **0.19**. Existing
tricks that involved no learning at all took roughly **40%** of that.

So roughly a year of work on learning rules was measured in a space barely wide
enough to show anything — and every result came back "no effect," which was
read as *the learning doesn't work* when it may well have meant *the test can't
see it.*

Nobody was careless. It's genuinely hard to notice, because every individual
result looks like a clean, honest null.

### What we do about it

**Before writing a single line of any learning mechanism, we have to prove the
test has room in it.** Specifically:

- Measure what a random untrained network scores.
- Measure what a strong conventional system scores.
- Show there's a **big** gap between them.
- Run it many times, not once, because small effects that appear in a couple of
  runs routinely evaporate when you do twenty.
- Report what a *totally stupid* answer scores too — always guessing the most
  common option — because if that scores 56% and your clever system scores 55%,
  you've learned something important and it isn't good news.

Only when that gap is demonstrated does anything else start.

That's why it's gate 0. Everything downstream is measured with this instrument,
so if the instrument can't see, no measurement afterwards means anything.

---

## The general principle

You'll see this shape everywhere in the project:

> **Ask what result would prove you wrong. If nothing could, you're not running
> an experiment — you're running a demonstration.**

It applies to the project, to each experiment, and even to individual tests. A
test that would pass whether or not the thing it checks is broken is worse than
no test, because it produces confidence instead of information.

---

*That's the end of the current set. New explainers get added as the project
introduces new ideas — see the [index](README.md).*
