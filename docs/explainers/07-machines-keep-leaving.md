# 7. Machines keep leaving. How bad is that?

Better than we expected — and for a reason that's genuinely surprising.

This is the first time in this project we've gone and read what other people
actually measured, rather than reasoning from first principles. It was worth it.

---

## The worry

Our whole plan depends on running on strangers' computers. Strangers close their
laptops. That's not a rare accident — it's constant, and it happens without
warning.

So: how often do people actually leave, and is it survivable?

## What people have measured

Researchers studied real file-sharing networks — the systems where millions of
ordinary people share files from home computers. Those are the closest existing
thing to what we want to run on, and they've been measured carefully.

The headline number looks terrible: **a typical visit lasts a couple of
minutes.** One study found a median of 2.4 minutes.

That sounds fatal. It isn't, and here's why.

---

## The surprising bit

> **Most visits are very short. But most machines *currently online* are
> long-stayers.**

Read that twice, because it sounds like a contradiction and isn't.

**An analogy: a coffee shop.** Over a day, hundreds of people pop in for two
minutes to grab a takeaway. A handful of people sit there all afternoon with a
laptop.

Now ask two different questions:

- *"How long does a typical visit last?"* → **About two minutes.** The takeaway
  crowd dominates, because there are so many of them.
- *"If I walk in right now and pick someone at random, how long have they been
  here?"* → **Hours.** The all-afternoon people are the ones actually sitting
  there at any given instant.

Both answers are correct. They're answers to different questions.

**We care about the second question.** We don't care about the average visit —
we care about who's actually available to do work right now. And that population
is dramatically more stable than the visit statistics suggest.

The scary number was answering a question we weren't asking.

---

## The genuinely useful bit

There's a second finding that's even more helpful.

**How long a machine has already been online predicts how much longer it'll
stay.**

This is not obvious, and it's the opposite of what you'd naively assume. The
naive assumption — the one built into most simple models — is that leaving is
random and unpredictable, like a coin flip each minute. If that were true, a
machine that's been up for three hours would be no more likely to stay than one
that just arrived.

**The measurements say otherwise.** The longer a machine has been there, the
*less* likely it is to leave in the next minute. Stability compounds.

The researchers also found that a machine that stayed a long time on its last
visit tends to stay a long time on its next one. And that machines available
yesterday tend to be available today.

**So stability is predictable** — and that's something you can build on.

## Why that matters for our rules

Remember [rule 1](03-the-three-rules.md): nothing is allowed to need the big
picture.

You might think "put important things on the reliable machines" breaks that
rule. Surely you need a central list ranking everyone by reliability?

**No — and this distinction matters.** A machine knows how long *it* has been
running. And it can see how long each of *its own neighbours* has been
reachable. That's all local information, sitting right there, no coordination
with anyone.

So "prefer to trust the neighbours who've already proven they stick around" is
something every machine works out independently, from what it can already see.

- **A global ranking of everyone** — breaks rule 1. Needs everyone to report in.
- **Each machine preferring its own steadiest neighbours** — perfectly fine.
  Nobody talks to anybody extra.

Easy to confuse. Only one is forbidden.

---

## A trap specific to our design

Here's a problem we found while writing this down, and it's an awkward one.

**How do you notice that a machine has left?** Obvious answer: you stop hearing
from it.

**That doesn't work for us.** Our design is deliberately *sparse* — most parts
stay quiet most of the time, and only occasionally send anything. Silence is the
normal state. The previous project's version was quiet about 98% of the time.

So "I haven't heard from you" means either *you're gone* or *you had nothing to
say*, and there's no way to tell which.

**The fix is straightforward but not free:** machines have to send an explicit
"still here, nothing to report" signal on a regular basis. A heartbeat.

That costs bandwidth, and bandwidth is the thing we're most worried about
running out of. So it's a real cost, noted now rather than discovered later.

---

## The neat part

Two of our three rules turn out to be the same mechanism.

Recall from [explainer 6](06-who-is-to-blame.md): each part makes a prediction
and holds onto it until the matching input arrives. Because messages can be
slow, it has to be willing to wait a while — call it the *waiting time*.

Now notice what happens when you're already waiting:

- **Input turns up within the waiting time?** It was just slow. Fine — compare,
  learn, carry on. Nothing is lost.
- **Waiting time runs out and nothing came?** That machine is gone. Stop
  expecting it.

**Same mechanism. One setting. Both rules handled.**

"Late" and "gone" turn out to be the same event observed at different timeouts,
which is a good sign — when two problems you listed separately collapse into
one, it usually means the design is cutting along a real joint.

**The catch, stated honestly:** the waiting time has to be long enough to
tolerate a slow connection from the other side of the world, but short enough to
notice a dead machine promptly. Those pull in opposite directions, and in
between is a zone where a slow machine gets wrongly declared dead. That's a real
cost of the neat unification, not a free win, and it needs measuring rather than
arguing about.

---

## What we owe when someone leaves

Our answer: **nothing global.**

Nothing stops and waits. Nothing gets rebuilt. Nothing needs to be told. Each
part that was listening to the departed machine simply notices, stops waiting,
and carries on. The network is now slightly smaller and slightly less capable.

That's how brains handle it, and it's the only version compatible with rule 1 —
any organised repair effort needs someone in charge of organising it.

**One thing we've deliberately not decided:** whether things a machine *learned*
should be backed up elsewhere before it vanishes. That's a real trade-off —
backups cost bandwidth, which is our tightest budget. We're leaving it until we
can measure it rather than guess.

---

## Honest status

Better than the other explainers, for once. **The churn numbers are from a paper
we actually read**, not a summary of a summary.

Still unverified: everything about how the machine-learning world handles this,
and — more importantly — there's a body of work on detecting failed machines
without false alarms that we haven't read, which is precisely the problem the
"waiting time" trap creates. That's the most valuable unread thing right now.

---

*Next: nothing yet — this is the current edge. See the [index](README.md).*
