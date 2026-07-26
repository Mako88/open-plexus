# 3. What are the three rules everything has to obey?

Three constraints. They come straight from the fact that the machines are
strangers' home computers connected by the ordinary internet. Everything else in
the project is negotiable; these are not.

The formal versions are in [GOALS.md §3](../../GOALS.md).

---

## Rule 1 — Locality

> **Nothing is ever allowed to need the big picture.**

No part of the system may require an answer that can only be computed by looking
at everything at once — a total, an average across all machines, a ranking, or
"wait here until everyone catches up."

**Why:** the moment one step needs the whole picture, every machine has to stop
and report in. That's the lockstep problem from
[explainer 2](02-why-ai-needs-data-centres.md), and it's what forces a data
centre.

**The hard part is that this rule will be tempting to break.** Some trick that
peeks at the global picture will make the numbers look better. It has to be
rejected anyway — a version that only works when everyone reports in is not a
version that runs on the internet. So the rule is written as: *this is a
violation even when it improves the results.*

---

## Rule 2 — Bounded asynchrony

> **Information arrives late, out of order, and at unpredictable speed. Say how
> late is survivable, and be exactly right below that.**

"Asynchrony" just means things don't happen in a neat order. Messages overtake
each other. Some arrive quickly, some crawl.

The subtle part is the word *bounded*. It's not enough to say "we cope with
delays." We have to name a specific number — say, a fifth of a second — and
guarantee that **below that number, the result is exactly the same as if nothing
had been delayed at all.**

**Why bother being that strict?** Because "it degrades gracefully" can't be
engineered against. If you only know things get gradually worse, you can never
say whether a given setup will work. If you know the exact bound, you can check
it, test it, and build to it.

For scale: a message between continents takes roughly **150 milliseconds** — about
a fifth of a second. Any mechanism that needs its information faster than that
cannot be spread across the world, no matter how well it works on one machine.

---

## Rule 3 — Churn

> **Machines leaving is normal, not an emergency.**

Someone shuts their laptop. Wi-Fi drops. A game launches and wants the graphics
card back. On consumer hardware this isn't a rare fault — it's the constant
background condition.

So the system must assume, from day one, that **any machine can vanish
mid-thought** and the rest carries on.

**This is the least developed of the three, and we're saying so.** In the
previous version of this project it was a stated principle that was never once
tested — because nothing ever left. It's now an open question with a name:
*what does "a machine left" actually mean, concretely?*

We also suspect the answer partly already exists. Computer science has spent
decades on unreliable machines dropping in and out of networks — it just isn't
the field AI researchers usually read. Checking that literature is on the list,
and it's cheap.

---

## Where biology fits

Biology is the one existence proof that all three rules can hold at once and
still produce intelligence. Brain cells only see what touches them (rule 1).
Signals take a long time (rule 2). You lose cells continuously without noticing
(rule 3). Nothing engineered manages all three.

**So we read biology closely, and often.** When evolution has already solved a
problem we have, we'd be foolish not to look.

But two limits keep that useful rather than misleading:

**Separate what brain cells *compute* from what they merely had to *put up
with*.** A lot of biological detail exists because cells are wet, made of
chemistry, and running on sugar. Copying that imports the cost without the
benefit. What's worth borrowing is the *computation* — how a cell decides, learns
and stays stable — not the plumbing.

**Biology is a reason to try something, never a reason to keep it.** "The brain
does it this way" makes an idea worth testing, and gives it no head start
whatsoever once tested.

That second point is not a hypothetical caution. In the previous version of this
project, four headline design choices were each justified by biology, and when
someone finally checked, every one turned out to be either doing nothing at all
or actively unhelpful. **The biology wasn't the problem — treating it as proof
was**, because it made those pieces feel already justified, so nobody measured
them for a year.

---

*Next: [How will we know if this doesn't work?](04-how-well-know-if-were-wrong.md)*
