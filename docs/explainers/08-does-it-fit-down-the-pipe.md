# 8. Does it fit down the pipe?

All these machines have to send each other messages, constantly. Home internet
is slow. Does the traffic actually fit?

This is the first explainer where the answer comes from **arithmetic** rather
than argument. That's a nice change — you can check it.

---

## The thing everyone gets wrong first

The obvious way to ask this: *"What fraction of the connections have to go over
the internet, rather than staying inside one machine?"*

If it's a small fraction, we're fine. If it's most of them, we're sunk.

**It's essentially all of them, and there's nothing we can do about that.**

Here's why. Imagine the full-size version: about 33 million parts, spread over
about 31,000 machines. That's roughly a thousand parts per machine — which means
each machine holds about **one thirty-thousandth** of the whole network.

So when a part needs to send something to another part, what's the chance that
other part happens to be on the same machine? About 1 in 31,000. **Basically
never.**

So the answer is: 99.997% of connections cross the network. That fraction isn't
a dial we can turn. It's just what happens when you cut a network into thousands
of pieces.

**Which means we were asking the wrong question.**

---

## The right question

Here's the thing the first question misses.

**You don't send a separate message for every connection. You send one message
per destination *machine*.**

If a part needs to tell sixty other parts something, and all sixty live on the
same computer in Ohio, that's **one message** to Ohio. The machine in Ohio then
hands it to all sixty locally, for free.

So what actually costs bandwidth isn't *how many connections* cross the
network — it's **how many separate machines each part has to contact.**

Call that number **D**.

**And D is something we control**, because it depends on *where we put things*.
Scatter a part's connections randomly across the world and D is huge. Cluster
them onto a handful of machines and D is small. That's a design decision.

*(One catch: if you place things randomly, the aggregation trick saves you
nothing. We checked — with a thousand connections scattered over 31,000
machines, they land on about 984 different machines. Almost no two share a
destination. So the saving is only available if you deliberately arrange for
it.)*

---

## So what can we afford?

One important detail: home internet is **lopsided**. Your download speed is
typically 5 to 20 times faster than your *upload* speed. And we're sending, so
**upload is what binds.**

Running the numbers:

| Machines each part contacts (**D**) | Upload needed |
|---|---|
| 1 | 2.7 Mbps — fine on anything |
| 3 | 8.2 Mbps — fine on a poor connection |
| 10 | 27 Mbps — fine on a good connection |
| 30 | 82 Mbps — needs fibre |
| 100 | 273 Mbps — needs fibre |
| 1000 | 2,725 Mbps — forget it |

So:

- **Poor-but-common upload (10 Mbps)** → D can be about **4**
- **Good consumer upload (40 Mbps)** → D can be about **15**

> **Each part must talk to somewhere around ten machines, not a thousand.**

Demanding. Not impossible.

---

## What that forces about the design

If each part has a thousand connections and they must fit onto about fifteen
machines, that's about seventy connections per destination.

**That can't happen by accident.** It requires the network to be built in
*neighbourhoods* — groups of parts that mostly talk to each other, with only
thin connections between groups. And then we have to place whole neighbourhoods
on the same machine.

This is a real result, and worth noticing what kind of result it is: **a
budget spreadsheet just told us something about the shape of the brain we're
building.** Not a preference, not a biological analogy — arithmetic about home
internet connections says the network has to be locally clustered.

**And it rescues the bad news from earlier.** If a part's connections mostly
stay in its own neighbourhood, and the neighbourhood is on one machine, then
most connections *don't* cross the network after all. The scary 99.997% assumed
we scattered things randomly. Once you cluster, both numbers get better at
once — because they were never two separate things. They were both consequences
of where we put things.

**The cost, honestly:** a network of tightly-knit islands with thin bridges
between them might just be *less capable* than a well-mixed one. Isolation has a
price and we don't know what it is. That's the most important unknown in this
explainer, and it needs measuring rather than arguing about.

---

## Two nice surprises

### The slowness we tolerate is what makes it affordable

Each message we send is tiny — about 16 bytes. But every internet packet carries
about 42 bytes of addressing wrapper, regardless of contents.

**So sending events one at a time is about 72% packaging.** Ruinous.

The fix is obvious: wait a moment, collect everything going to the same
destination, send it as one bundle.

But *waiting* is only acceptable because we already decided we can tolerate
delay:

| how long we bundle for | wasted on packaging |
|---|---|
| 1 millisecond | 11% |
| 20 milliseconds | 0.6% |
| 150 milliseconds | **0.1%** |

A design that needed millisecond-fast delivery would bleed 11% on wrapping and
couldn't bundle its way out. Ours can wait a fifth of a second, so the waste
essentially vanishes.

> **The delay tolerance pays for itself twice** — once by letting us span the
> internet at all, and again by making the bandwidth affordable.

When one decision solves two unrelated problems, it's usually a sign you've cut
along a real seam rather than made a concession.

### The heartbeat worry was unfounded

[Explainer 7](07-machines-keep-leaving.md) flagged a problem: because our system
is mostly silent, machines need to send a regular "still alive" signal, and we
worried that might eat the bandwidth budget.

It doesn't. Even at 100 neighbours pinging 10 times a second, it's **under 5% of
the budget** — and in realistic configurations, well under 1%.

Reason: heartbeats are per *machine*, not per *part*. There are a thousand parts
per machine but only one heartbeat. The worry was reasonable; the arithmetic
dismisses it.

---

## Honest status

**The arithmetic is sound; the inputs are guesses.** Numbers like "a thousand
connections per part" and "16 bytes per message" are borrowed from the previous
project — an architecture we're deliberately not rebuilding.

If those are wrong by a factor of a few, the specific figures move.

**But the main conclusion doesn't move**, and it's worth understanding why: it
follows from each machine holding a *tiny fraction* of the network, which is
true for any large network on any hardware. That's the part to trust.

**And none of this has been measured on anything, because nothing exists yet.**

---

*Next: nothing yet — this is the current edge. See the [index](README.md).*
