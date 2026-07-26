# 11. The first code — and the bug in the first thing it printed

Ten explainers of argument. Time to build something.

**What got built is the *test*, not the thing being tested.** And within about a
minute it revealed a flaw that would have quietly ruined every measurement we
took.

---

## Why build the test first

The whole project is arranged around one idea: **you can't trust a result if you
can't trust the ruler.** [Explainer 4](04-how-well-know-if-were-wrong.md) put
"do we even have a fair test?" as gate zero, ahead of everything, because every
later number is read through it.

So the first code is the memory game from
[explainer 10](10-the-test-we-nearly-built.md): show the network some key-value
pairs, bury them in distracting material, then ask about **all** of them.

It also happens to be the only substantial thing we can build right now that
doesn't depend on a decision we haven't made. No learning, no networking, no
choices about the network's shape. **Just the question, written down precisely
enough to run.**

---

## The bug

Here's the first sequence it ever produced. Top row is what the network sees;
bottom row is what it's supposed to answer, where `-1` means "no answer required
here":

```
tokens   2  10   4   8   0  11   0   1   2   3   4   5   6   2   0 ...
targets -1  -1  -1  -1  -1  -1  -1  -1  -1  -1  -1  -1  -1  10  -1 ...
```

The pairs are `2→10`, `4→8`, `0→11`. At position 13 the token is `2`, and the
network is asked for `10`. Correct.

**But look at position 8.** The token is also `2` — and there the answer is
`-1`, meaning *don't answer*.

**Identical input. Opposite required behaviour. No way to tell them apart.**

Position 8's `2` was meant to be meaningless filler. It happened to collide with
a key that gets queried later. The network sees the same symbol in both places
and is expected to stay quiet in one and answer in the other.

That's not a hard task. **That's an impossible one.**

---

## Why this one is nasty

It wouldn't have crashed. It wouldn't have looked wrong. It would have produced
a perfectly reasonable-looking benchmark that **every model failed at equally.**

And we'd have had a story ready: *"our approach can't do content-based recall."*
It's what we predicted, after all. We'd have written it up, believed it, and
possibly abandoned a working idea — never suspecting that the test was asking
for something no system could do.

**The failure mode isn't being wrong. It's being wrong in a way that looks
exactly like an honest result.** That's the thing this project keeps meeting in
new disguises.

**The fix took one line:** filler is now drawn only from symbols this particular
sequence *doesn't* use as keys. It still looks exactly like a key — so it's just
as distracting, which is the whole point — but it's never actually one.

---

## How it got caught

Not by a clever tool. **By printing the thing and reading it.**

That's worth dwelling on, because the temptation with code that generates data
is to check it runs, check the shapes look right, and move on. All of which this
would have passed.

Age of the bug: **minutes** — because someone looked. Had nobody looked, it
would have lived until it produced a flat, plausible, entirely meaningless set
of results.

---

## Testing the tests

Now the part that's genuinely unusual, and it's
[explainer 4](04-how-well-know-if-were-wrong.md)'s principle turned on our own
tooling.

We wrote eighteen tests. They pass. **So what?**

A test that passes tells you one of two things: the code works, *or* the test
doesn't actually check anything. From the outside those look identical — both
are a green tick.

So we built a small tool that **deliberately breaks the code** and demands that
the tests notice.

It makes six specific sabotages, one at a time:

- make filler collide with keys again (the bug above)
- only ask about the first pair instead of all of them
- ignore the random seed, so every example comes out identical
- give the wrong answer at every question
- disconnect the distraction setting so two different modes secretly do the same thing
- collapse the answer alphabet so the scoring baseline is wrong

For each: break it, run the tests, **require them to fail**, put it back.

**All six were caught.** Which means those eighteen tests are doing real work,
and we know that rather than assuming it.

A sabotage that *survived* would tell us something valuable and unwelcome —
that the tests covering that mechanism are decorative. The rule is to strengthen
the test, never to delete the awkward sabotage.

The tool also shouts if the code has moved underneath it. A sabotage that can no
longer find the line it meant to break would otherwise report success while
having done nothing at all — a green tick meaning the opposite of what it looks
like. Same failure mode again, one level up.

---

## A test that failed, and who was right

On the first run, one test failed. That's the interesting case, because there
are two possible culprits: the code, or the test.

Our rule says **assume the code is guilty until shown otherwise** — because the
tempting move is to loosen the test until it goes green, and that converts a
caught bug into a hidden one while destroying the evidence it existed.

This time the code was innocent. The test was checking that our predictable
filler pattern really is predictable — but it checked by squashing all the
filler together into one list and looking for a repeating rhythm. The gaps where
questions had been removed threw the rhythm off. The filler *was* perfectly
predictable from its position; the test was looking in the wrong place.

So the test was rewritten to check the actual property. **And the test's own
notes now record what the wrong version did and the number it produced** — 152
matches where 180 were expected — so nobody re-derives that confusion later.

---

## Where this leaves us

- The benchmark exists and is precisely defined.
- Eighteen tests, all of which have been *seen to fail* when they should.
- Six sabotages, all caught.
- One real bug found and fixed before it could contaminate anything.

**Nothing has learned anything yet.** There's no network, no model, no result.
We've built the ruler.

Next is measuring what a completely stupid answer scores on it — always guessing
the most common option — because until we know that, we can't tell a weak
positive from an elaborate way of guessing.

---

*Next: nothing yet — this is the current edge. See the [index](README.md).*
