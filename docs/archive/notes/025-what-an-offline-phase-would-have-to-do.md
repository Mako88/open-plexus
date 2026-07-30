# 025 — What an offline phase would have to do

**Status:** requirements only. Nothing built, nothing measured, no prior art read
yet — deliberately in that order.
**Why now:** the g9 line has produced a gate that works, and its remaining
limitation is structural rather than a tuning problem.

---

## IN PLAIN TERMS

Every filter this project has built has to decide what to keep at the moment it
knows least — as the information arrives. The reward signal that says "that
mattered" turns up later, and the best current mechanism handles that by keeping
a small set of candidates alive until it does.

The obvious alternative is to stop deciding in the moment: write things down
cheaply, and go back over them later when you know more. Sleep is the biological
version and it is the one thing on the source list that transfers.

This writes down what such a phase would have to do *here* before looking at how
anyone else has done it — because three times now this project has derived a
requirement, built it, and then discovered the literature had a better-specified
version. Writing the requirements first turns that list into a search query.

---

## Why the g9 result makes this the next question rather than an alternative

The tag works: bounded capacity over writes, a fade, and capture by a late
signal, recovering about +0.2 of the oracle's advantage, flat across delay, at
node widths from 64 down to 8
([g9-06](../../experiments/sweeps/g9-06-is-the-tag-capacity-starved.txt),
[g9-09](../../experiments/sweeps/g9-09-a-small-node-in-a-wide-network.txt)).

> # THE PREMISE BELOW IS WRONG, AND IT WAS MEASURED THE SAME DAY
>
> The paragraph that followed said the tag's shortfall is write-time ignorance —
> that it cannot tell which bindings will matter. **It can. It keeps every one of
> them.** Counted over 8 sequences at `slots` 32, `fade` 0.95:
>
> | arm | writes kept | rewarded kept | recall | precision |
> |---|---:|---:|---:|---:|
> | oracle | 32 | 32 | 100% | **100%** |
> | tag | 929–965 | 32 | **100%** | **3.4%** |
>
> Identical at delays 1, 8 and 20 — including delay 20, where the window keeps
> **none**. The tag has perfect recall of what matters and keeps about
> twenty-nine useless writes for every useful one.
>
> **So the missing four fifths are interference, not ignorance.** Retrieval goes
> as `sqrt(d / N)`, and the tag's `N` is thirty times the oracle's while
> containing everything the oracle contains.
>
> That redirects this note. **Replay addresses write-time ignorance and there is
> no write-time ignorance left to address.** What is needed is PRECISION: the
> same recall with fewer passengers. An offline phase might still deliver that —
> revisiting could discard as well as rescue — but it is no longer the obvious
> mechanism, and R1–R5 below were written against the wrong problem. They are
> kept because the constraints (R3 cost, R4 locality, R5 measured against the
> tag) survive the change of target.

**It recovers about a fifth of what the oracle gets, and the remaining four
fifths have a name.** The oracle knows at write time which writes will matter.
The tag does not, and cannot: `reward_recall` chooses rewarded cues uniformly out
of the same alphabet as filler, so nothing local separates a rewarded binding
from an unrewarded one until the reward arrives
([g9-04](../../experiments/sweeps/g9-04-is-there-a-local-signal.txt)). The tag's
whole job is to keep enough candidates alive to be *able* to act when the signal
lands.

That is a holding operation. **Replay is the alternative to holding**: if you
cannot tell at storage time what mattered, do not try — revisit later, when you
can.

## The five things it would have to do here

**R1 — Revisit something that is no longer in the fast store.** Otherwise it is
the tag with extra steps. The tag already holds candidates *in* the store; an
offline phase has to reach material the online pass discarded or never
consolidated, which means something must survive the online pass in a cheaper
form than a binding does.

**R2 — Use information unavailable at write time, and name it.** The only
candidate this task offers is the reward token and what followed it. A replay
that re-scores writes on the same local signals they were ranked by at write time
learns nothing — it would be `tag_relative` run twice.

**R3 — Cost less than storing everything.** If the offline phase needs a
transcript of the sequence, it has lost to the trivial baseline of a bigger
store, and note 024's arithmetic applies: a record that does not shrink with the
node crosses the node's own memory below about width 2.

**R4 — Respect C1.** No global synchronisation. An offline phase is a natural
place to accidentally introduce one — "at the end of the sequence, every node
pools its traces" is exactly the reduction C1 forbids, and it is the shape a
first implementation reaches for.

**R5 — Be measurable against the tag on the same axis.** Recovery of the
oracle's advantage, on `reward_recall`, at the same delays and node widths, with
`tag` as the arm to beat rather than `none`. A replay that beats the ungated
floor but not the tag is not a result.

## What makes this hard here, specifically

**The store is superposed and cannot be enumerated.** A cache can be walked; a
`d × d` matrix of summed outer products cannot. Note 015 already ran into this
from the other side — "a superposed memory cannot name which of its bindings
answered; it can only be asked again and told whether the answer held up".

So a replay has nothing to iterate over unless something else is kept, and R3
says that something has to be cheap. The obvious cheap thing is **token ids**,
which `derived_keys` already makes sufficient to regenerate any key or value
(note 012). A node that kept a ring of recent token ids could re-present them to
itself and rebuild the bindings it chose not to keep.

**That is the design this note is pointing at, and it is not built.** It also has
an unattractive property worth naming now: re-presenting tokens is re-running the
sequence, which costs compute proportional to what is replayed, and
`tools/step_rate.py` measured a node's compute as 21x to 380x under-used. So the
budget probably exists. That is an argument from a measurement, not from hope,
and it should be checked before it is relied on.

## What would make this a bad idea

- **If the tag's ceiling is not where the oracle's advantage actually goes.** The
  gap between +0.2 and 1.0 is assumed here to be the write-time-ignorance
  problem. It might be interference, or capacity, or the untrained readout. That
  should be measured before a mechanism is built against it, and it has not been.
- **If replay only works with a transcript.** Then it is a bigger store wearing a
  biological name, and R3 kills it.
- **If it needs the whole network to pause.** R4 kills it.

## Prior art, searched against R1–R5 — AND NOT READ

> **Everything in this section is from search-result summaries. No paper was
> read.** Three attempts to fetch a source failed: the arXiv review exceeded the
> fetch size limit, bioRxiv returned 403, and the NSF record 404'd. Note 005
> exists precisely because a borrowed claim gated a design decision and turned
> out to describe a variant this project cannot use, so **none of this may gate
> anything** until a paper is actually read. It is recorded to make the reading
> cheaper, not to substitute for it.

The search was run against the requirements above rather than against the topic,
which is the point of writing them first. One hit lands directly on R2 and it
specifies something the requirements did not:

**Replay appears to be REVERSE, and triggered by reward receipt.** The summary of
*Experience replay is associated with efficient nonlocal learning* (Science,
2021) describes backward replay of nonlocal experience occurring **after receipt
of a reward**, with a 160 ms state-to-state lag, linked to efficient learning of
action values and to credit assignment specifically.

R2 said an offline phase must use information unavailable at write time and
named the reward token as this task's only candidate. It did not say **when** to
replay or **in which direction**, and both are free parameters that a first
implementation would have picked arbitrarily. "Backwards from the reward" is a
much more specific mechanism than "revisit later", and it is the one shape that
makes sense of a delayed reward: the credit flows back along the path that
produced it.

That maps onto this system without much translation. A node holding a ring of
recent token ids (R3, and cheap because `derived_keys` regenerates keys and
values from a token) could, on receiving a reward token, re-present the ids
**backwards** from the reward rather than forwards from anywhere. Which is
approximately what the tag's fade already approximates crudely — marks nearer
the reward survive — and would replace an exponential weighting with an actual
traversal.

Two further summaries worth checking when a paper can be read:

- Reward's effect on memory reported as **emerging only after a 24-hour delay**
  and stronger after a longer rest interval (Nature Communications, 2018). If
  that is what it says, the effect being claimed is about consolidation over
  time rather than about a mechanism a node runs within a sequence, and it may
  not transfer at all.
- A 2026 arXiv preprint pairing sparse local learning with consolidation and
  replay, described as using an explicit local error signal and a **budgeted
  synapse-selection mechanism** — which is the tag's bounded capacity under
  another name, and is the closest thing found to this project's own design.

**What this does NOT settle:** whether reverse replay is a finding about human
MEG decoding that happens to be describable as credit assignment, or a mechanism
with a computational specification anyone could implement. That distinction is
exactly what note 005 was written about, and it needs the paper.

## Next, in order

1. **Read one of these properly.** The Science 2021 paper is the one that would
   change the design, because reverse-from-reward is a mechanism and "revisit
   later" is not. BACKLOG also names Filipchuk et al. (2022), whose transferable
   claim is only that an offline phase exists at all — weaker, and now
   superseded as a reason by the search above.
2. **Measure where the tag's missing four fifths actually go**, before building
   anything against an assumption about them. The gap between +0.2 and 1.0 is
   assumed here to be write-time ignorance; it could be interference, capacity,
   or the untrained readout, and an arm that gives the tag the oracle's
   SELECTION while keeping its capacity would separate those.
3. **Then build**, with the direction and trigger taken from a paper rather than
   chosen.

---

*Related: [010 — tagging and capture](010-tagging-and-capture.md),
[023 — two signals](023-two-signals-and-only-one-of-them-is-about-value.md),
[024 — what the gate costs a tiny node](024-what-the-gate-costs-a-tiny-node.md),
[012 — broadcast the token](012-broadcast-the-token.md).*
