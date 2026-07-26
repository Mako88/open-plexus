# 9. Checking our homework

Several earlier explainers made claims about what other researchers had found —
and quietly admitted at the bottom that we hadn't actually read the papers, only
summaries of them.

This is what happened when we went and read them.

**Short version: our main idea survived. One supporting claim was too strong.
And we found something we'd missed that makes our chosen approach *less*
supported by existing evidence than we'd been assuming.** That last one is the
valuable find, and it's the reason for doing this before building anything.

---

## What we were checking

Two claims were doing heavy lifting.

**Claim 1 — the head office idea.** From
[explainer 6](06-who-is-to-blame.md): that "how learning works" secretly mixes
up two questions — *where does the blame assessment come from* and *how does the
message get delivered* — and that only the first one decides whether you need a
data centre.

**Claim 2 — the settling trap.** Also from explainer 6: that one popular
approach requires the whole network to bounce messages back and forth until it
settles down, before any learning happens. We said the famous results supporting
that approach *depend* on the settling.

---

## Claim 1: confirmed — and they say it themselves

We were slightly nervous about this one, because it's the load-bearing idea in
the whole project and we'd invented the framing ourselves.

We needn't have worried. The paper we checked builds its entire method on
exactly that split, and describes it more plainly than we did. In their words,
one part is "**locally available at a synapse and does not depend on network
performance**," while the blame signals are "**provided externally**."

That's our distinction, stated by the researchers as the foundation of their
own method. **The local part is solved; the external part is the problem.**

So our diagnosis of why the previous project got stuck — that it kept improving
the delivery while never questioning the source — holds up better than when we
wrote it.

---

## Claim 2: we overstated it

We said the results "depend on" the settling. That's not right, and here's what
the source actually says.

There are **two** ways to get the result, and only one involves settling. The
other works if the network's activity stays close to where a normal
forward-pass would have put it. And even in the settling version, experiments
show it doesn't have to settle *completely* — close enough is enough.

There's also a variant that skips the settling entirely and gets an *exact*
match. **But** — and this is the bit that matters for us — it does so by
requiring careful synchronised timing of updates across layers.

**That's not an escape. It's the same problem wearing a different hat.** We're
trying to avoid everyone having to coordinate; a method that avoids the
back-and-forth by requiring precise coordination has just moved the difficulty
rather than removed it.

So: our conclusion stands, our reasoning was too strong, and it's been corrected
in place rather than quietly softened. **The rule we work to says a claim that
turns out wrong gets fixed, not fudged** — including when it's our own claim and
fixing it is mildly embarrassing.

---

## The thing we'd missed entirely

Here's the find, and it's worth the whole exercise.

We'd been treating "predicting things is a known-good approach — there's a
literature showing it works" as comfortable background support for our plan.

Then we read how those experiments were actually run:

> During training, the top of the network is fed the input, **and the bottom of
> the network is clamped to the correct answer.**

**They hand it the right answer.**

That's supervised learning — the exact setup we've spent this whole project
trying to get away from, because handing every part of the network the correct
answer is precisely the thing that requires a central authority and a data
centre.

So the well-known results supporting "prediction-based learning works" are
results about **a version we can't use.**

**Our approach isn't refuted by this.** It's just less supported than we'd been
assuming. It moves from *"a known technique, adapted"* to *"our own bet, whose
apparent supporting evidence turns out to be about something else."*

That's a real demotion, and it changes what we're entitled to be confident
about.

---

## What it changes

**One test just became the most important thing in the project.**

We'd already identified it: *does a network's own internal state actually
predict what it's about to receive?* If not, there's nothing for our scheme to
learn from and the whole approach is dead.

Previously that was one open question among several — uncomfortable, but with
the reassurance that "this family of methods is known to work."

**That reassurance turns out not to apply.** So the question isn't just open,
it's the one thing standing between us and a plan built on an assumption nobody
has checked. It needs running early, and it's cheap.

---

## Why do this before building?

Because of *when* a mistake like this would otherwise surface.

If we'd written the plan first, "prediction-based learning is known to work"
would have gone in as settled background. Then we'd have built the thing, run
experiments, and measured our own system — **never revisiting where the original
confidence came from.**

If it then failed, we'd have concluded our implementation was wrong, or our
network was too small, or our test was unfair. We'd have spent months debugging
a system whose foundational assumption had never been checked, because it was
filed under "established" rather than "assumed."

**Nothing in the experiments would have caught it**, because the experiments
would all have been downstream of it. That's the failure mode this project keeps
finding in different disguises: not being wrong, but being *confidently wrong in
a way that later evidence can't reach.*

Cost of checking: an afternoon. Cost of not checking: potentially the project.

---

## Still unchecked

Being honest about what's still on credit:

- **The reservoir claims in [explainer 5](05-what-makes-a-fair-test.md)** — that
  a random untrained network can't do our proposed memory task. **That belief is
  steering our entire choice of test**, and it's unread.
- The failure-detection work relevant to
  [explainer 7](07-machines-keep-leaving.md)'s wrongly-declared-dead problem.
- The system the previous project called the closest existing thing to what it
  had built.

---

*Next: nothing yet — this is the current edge. See the [index](README.md).*
