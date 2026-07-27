# 036 — can backpropagation be distributed, and what is it actually buying?

**Status:** literature scan, nothing measured here. **Every number is unverified
by us.** John's question, and it turns out to be a better question than the
premise it replaced.

---

## The premise this project started with, and why it needs restating

Backpropagation was ruled out early because brains do not do it. That was a
reason to look elsewhere, not an argument that it cannot be done — and the
distinction matters now, because **we are not currently paying any price for
avoiding it.**

The store is *activity*, not parameters. The only parameters are `Wo`, and **the
delta rule on `Wo` is the exact gradient** for a single linear readout. We are
not approximating backprop badly; there is nothing to propagate through.

So every result below is about a system we do not yet have. It becomes binding
**the moment we add a hidden layer**, and not before. That is the honest frame.

---

## 1. What backpropagation is actually buying — there is now an answer

Bordelon, Atanasov & Pehlevan (arXiv:2409.17858, ICLR 2025) give the account.
Task hardness is a **source exponent β**; β < 1 means the target lies outside
the initial kernel's reach.

    regime                 loss over time     compute-optimal exponent (β<1)
    lazy / kernel          t^(-β)             αβ/(α+1)
    feature learning       t^(-2β/(1+β))      2αβ/[α(1+β)+2]

**For hard tasks feature learning nearly doubles the exponent. For easy tasks
(β > 1) the two are identical.**

That single line explains the pattern every local-learning paper shows and none
of them names: **parity at MNIST, parity at CIFAR-10, a widening deficit at
CIFAR-100, and 5–13 points at ImageNet.** It is Filipovich's compute-optimal
result reproduced by a dozen mechanisms that have nothing to do with each other.
Deep credit assignment buys entry to the feature-learning regime; without it you
are capped at the kernel exponent.

## 2. The missing ingredient may be RANK, and rank is local

Boeshertz, Pascanu & Clopath (arXiv:2606.11123, June 2026) diagnose why local
feedback rules fail, and it is not what the field assumed. **The updates
collapse in rank.** On CIFAR-10 a 4-layer CNN's gradient-trajectory effective
rank stays under 20 and ends at 12 under feedback alignment, where backprop
reaches nearly 100.

Two fixes, both per-layer:

    CIFAR-100, ResNet-18       feedback alignment      1.4%
                               + BatchNorm            37.1%
                               + Muon                 25.3%
                               + both                 46.1%

**Muon is computed per layer from that layer's own momentum matrix** — five
Newton–Schulz steps orthogonalising it — with no cross-layer information. It is
the one optimiser property in the whole scan that is **C1-native by
construction.**

**This lands uncomfortably close to home.** Note 035 measured our own store's
effective rank at about 3, at every width. Boeshertz measured 12 where backprop
reaches 100 and called it the disease. **We have the same disease at a different
site** — theirs in the updates, ours in the store — and neither of us knew to
look until someone measured rank.

## 3. The reframe that matters most: C1 may be stronger than we need

Edmond & Kadmon (arXiv:2502.20580, Feb 2025) claim **error-feedback
dimensionality scales with task complexity, not network size.** Minimal rank
`r` = the number of output classes: r=10 matched backprop on CIFAR-10 across
MLPs, a CNN and a ViT; CIFAR-100 subsets needed r=50, 75, 100 exactly in step.

**This is not backward-sweep-free** — `δ_l` still depends on `δ_{l+1}`, so the
chain exists. What changes is that the message is **r floats instead of an
activation width**. A backward sweep carrying forty bytes per hop over internet
latency is a different engineering object from one carrying megabytes.

**So "no backward sweep" may be a stronger constraint than the goal requires.**
C1 exists to rule out designs that cannot run over the open internet. A tiny,
bounded, one-hop backward message might not be one of them, and we have never
asked.

**The honest caveat, which nobody in the scan states:** r scales with the OUTPUT
space. At character level r ≈ 64 against a d of 32–256, so there is no saving
for us today; at LLM vocabularies r would be tens of thousands. The result is
encouraging for small output spaces and says nothing kind about large ones.

## 4. The experiment nobody has run

**Rank-(#classes) feedback + Muon-style per-layer orthogonalisation + a fitted
compute-optimal scaling law.** Every component is published; the combination is
not. It directly tests whether the exponent gap is closable, which is the single
question standing between this project and goal 1.

The two halves are not obviously compatible — Boeshertz says updates must be
high-rank, Kadmon says feedback can be rank-10 — but they need not conflict: a
low-rank error times a high-dimensional activation, then orthogonalised, can
still produce a high-rank update. **Nobody has checked.**

Also unrun, and it is the gap our own g11-04 sits in: **no 2025–26 work fits
compute-optimal scaling laws to any local rule.** Filipovich did it for DFA in
2022 and nobody has repeated it since.

## 5. On churn — our gap is confirmed, and there is one measured data point

**CheckFree** (Blagoev, Ersoy & Chen, arXiv:2506.15461) is the only work that
handles permanent loss of *unique, unreplicated* state. A lost pipeline stage is
rebuilt as a gradient-norm-weighted average of its two surviving neighbours,

    W_i = (w_{i-1} W_{i-1} + w_{i+1} W_{i+1}) / (w_{i-1} + w_{i+1}),  w = ‖∇W‖²

with the recovered stage's learning rate scaled by 1.1. Weighting by gradient
norm favours the *less converged* neighbour. Cost: **1–9% perplexity**, no
checkpoint, no redundancy. Stated limits: consecutive losses are unrecoverable,
and the boundary-stage trick taxes convergence when nothing is failing.

**And the strongest confirmation that our gap is real comes from the people
closest to it.** Pluralis, who run a 7.5B model across 303 permissionless
participants, state in their own words that weight redundancy would be required
anyway so that shards are not lost when nodes leave. **The most advanced open
effort in this space has no redundancy-free answer to C3.**

Everything else labelled "churn" in the ML literature means temporary
unavailability with a full replica on every node — **C2 wearing C3's costume**,
and we should say so plainly rather than let a reader assume it is solved.

## 6. What is NOT worth pursuing, with the reason

**Zeroth-order and evolution strategies.** The update is beautifully local given
the loss scalar — but that scalar is the *whole network's* loss, so every worker
needs a **full model replica**, a **global scalar broadcast** (an `all_reduce`,
i.e. a barrier in twelve bytes), and **bit-identical state** for the seed trick
to work. Under C2 a worker that applied a stale update is silently sampling a
different distribution with no error signal. MeZO's own theory concedes the
d-factor slowdown, and its dimension-free result rests on an assumption it
attributes to adequate pre-training. Measured: ZO pretraining a 20M model took
30 hours to reach 8.17 loss where first-order SGD took 2.5 hours to reach 8.02.

**"Forward-only" does not mean "local."** Plain forward-gradient replaces the
backward sweep with a forward *tangent* sweep plus a global scalar broadcast —
the same barrier at lower bandwidth — and a departed mid-chain node breaks it
exactly as it would break backprop.

## 7. What I would carry into the model

1. **Muon-style orthogonalisation of our own updates.** Per-layer, local,
   C1-native, and it is the one thing that turned 1.4% into 46.1% in someone
   else's hands. We have one weight matrix, so this is cheap to try.
2. **Instrument rank, not just loss.** Note 035 found effective rank was the
   diagnostic nothing else surfaced, and Hao et al. (arXiv:2606.21126) show
   accuracy and gradient cosine both *mask* failures that only appear at depth.
3. **Scale a step size to its own estimator's variance.** Qin & Huang found a
   naive forward-gradient trunk scored *worse than a frozen random trunk*
   (2885 against 668 perplexity) purely because Adam normalises per coordinate;
   cutting that learning rate to 0.03 reversed it.
4. **Test accuracy, never train loss.** Singhal et al. document a
   "self-sharpening" collapse: above 95% train accuracy at 33% test, because the
   guess distribution collapses onto what the network already predicts.

## What this note is not

Nothing here was measured by us. The scan flagged its own unverified items and
discarded two fabricated tables it was offered. **Treat every number as a
pointer to read**, and treat §1 and §3 as the two claims worth the reading time.
