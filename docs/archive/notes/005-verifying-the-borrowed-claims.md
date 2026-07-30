# Note 005 — Verifying the borrowed claims in notes 001–004

Notes 001 and 002 were built on second-hand summaries and said so. This is the
pass that checks them. **One central claim is confirmed in the primary source's
own words. One is overstated and is corrected here and in note 002. One thing
nobody had noticed weakens the literature support for our choice.**

---

## IN PLAIN TERMS

We made some claims about what other researchers had found, based on summaries
rather than on the actual papers, and we flagged at the time that this was a
risk.

This note goes and checks. Results: **our main structural idea holds up — the
papers say it themselves, more directly than we did.** One supporting claim was
too strong and is corrected. And we found something we had missed entirely,
which makes the published evidence for our chosen approach *weaker* than we
thought — not wrong, just less supported than we were treating it.

That last one is the useful part. It moves a piece of the plan from "backed by
published results" to "our own bet, which we now know we have to test
ourselves."

---

## 1. What was checked, and how

The paywalled versions were inaccessible. Open-access equivalents were fetched
and their text extracted directly, so the quotations below are from the
documents rather than from summaries of them.

| claim under test | source used | verdict |
|---|---|---|
| Note 002 §2 — error *source* vs error *delivery* is the distinction that matters | Bellec et al., *e-prop* (arXiv 1901.09049) | **Confirmed** |
| Note 002 §5 — predictive coding's backprop-equivalence depends on relaxation to equilibrium | Salvatori et al., *Predictive Coding: Towards a Future of Deep Learning beyond Backpropagation?* (arXiv 2202.09467) | **Overstated — corrected** |

**Still not read:** SORN, Forward-Forward, dendritic-error work, gossip
protocols, SWIM, CRDTs. Note 001's claims about reservoir computing remain
unverified.

## 2. Confirmed — and the paper draws the distinction itself

Note 002 §2 argued that "how learning works" conflates two questions — where the
learning signal *comes from* (source) and how it *reaches* the synapse
(delivery) — and that only the source decides whether C1 and C2 are satisfiable.

The e-prop paper states exactly this split, as its own foundational
factorisation:

> "We subsume here under the term eligibility trace that information which is
> **locally available at a synapse and does not depend on network performance.**
> The online learning signals `L^t_j` are **provided externally**…"

That is the distinction, in the authors' words: the eligibility trace is local
and performance-independent; the learning signal comes from outside. The paper
further describes generating those signals via "broadcast alignment", with
"layer specific direct error broadcasts" using random weight matrices.

**So the trace is local and the signal is broadcast — delivery is solved, source
is not.** Note 002's diagnosis of why the predecessor was stuck stands, and is
better supported than when it was written.

The paper is also explicit that its own signals are approximations of something
harder: "In order to achieve the full learning power of BPTT, this learning
signal would still have to be complex and questionable from a biological
perspective."

## 3. Overstated — the correction

**Note 002 §5 said:** "The literature result that predictive coding approximates
backpropagation depends on that settling. Taking the result without the settling
is not supported by it."

**What the source actually says:**

> "For these results to hold, one of two conditions must be met: either the
> **activity values remain very close to their feedforward pass values** such
> that the prediction error is small, **or else** the layerwise derivatives must
> be held fixed to their feedforward pass values and the network run to
> equilibrium. Moreover, experimental results also empirically show that PC
> approximates BP updates under **less restrictive conditions**, i.e., a small
> output error is enough, and **the energy does not have to be completely
> converged.**"

Three corrections follow:

1. **Relaxation is one of two sufficient conditions, not the only one.** A
   small-prediction-error regime also suffices, and does not require settling.
2. **Full convergence is empirically unnecessary** even in the relaxation route.
3. **There is an exact variant that avoids relaxation entirely** — Z-IL, which
   performs exact backpropagation if weights update after the first non-zero
   inference step per layer — but the source notes it comes "at the cost of
   requiring complex control logic to **synchronize** parameter" updates.

**Point 3 is the one that matters for us, and it does not rescue the approach.**
Z-IL trades a relaxation loop for explicit cross-layer synchronisation, which is
a C1 violation of a different kind rather than an escape from one. Both known
routes to the equivalence result cost something C1 forbids; the third route, the
small-error regime, is a *restriction on the operating point* rather than a
mechanism, and whether it is reachable here is unknown.

**Note 002's conclusion is unchanged. Its argument was too strong and is now
narrower.** Corrected in place per rule 5 rather than softened.

## 4. Newly found — the results are about *supervised* predictive coding

Nobody had noticed this, and it is the most consequential item in this note.

The backprop-equivalence literature describes training a predictive coding
network like this:

> "During training, the highest layer is fixed to an input data point … and the
> **lowest layer is fixed to a label or target vector** in the same way."

**The target is clamped.** Those results are about *supervised* predictive
coding — a network driven by an externally supplied label at its output.

In note 002's taxonomy, **that is a broadcast source.** The label is global
information supplied from outside, exactly the thing C1 forbids and exactly what
the predecessor was already doing.

The consequence:

> **The famous "predictive coding approximates backpropagation" results are
> results about a scheme this project cannot use.** They are not evidence for
> self-supervised temporal prediction, which is what note 002 recommends.

Note 002 already distinguished hierarchical from temporal predictive coding and
preferred the temporal variant. **The distinction was right; the reason given
was incomplete.** The problem is not only the settling loop — it is that the
supporting literature is about a supervised, target-clamped setup, so it does
not transfer regardless of the settling question.

**Net effect on the recommendation:** unchanged in direction, **materially
weaker in support.** Temporal self-supervised prediction moves from "backed by a
literature showing this family works" to "our own bet, whose supporting
literature turns out to be about something else." The argument in note 002 §4 —
latency becoming a buffer rather than a race — is structural and stands on its
own, and it is now carrying more weight than it was.

**And note 002 §6's prediction 1 is promoted accordingly.** Whether a frozen
network's state predicts its own next input was already the gate on the scheme.
With the literature support weakened, it is now the single most important
unmeasured thing in the project.

## 5. What this exercise cost and returned

Roughly one working session. It confirmed one load-bearing claim, corrected one
overstatement, and found one error of omission that changes how much confidence
the plan is entitled to.

**The omission is the argument for doing this before the plan rather than
after.** A plan written yesterday would have recorded "predictive coding is
known to work" as settled background. It would have been wrong in a way that no
later experiment would have flagged, because the experiments would have been
measuring our own system and never revisiting where the confidence came from.

**Recorded as a standing rule candidate, not yet promoted:** a claim about
someone else's work needs the source read before it can carry a decision. This
is arguably already rule 1 — *state no behaviour that has not been observed* —
applied to the literature rather than to our own code. Whether it needs its own
rule or is better handled by noting that rule 1 covers borrowed claims too is a
question for the next standards revision.

## 6. What is still unverified

- **Note 001's reservoir claims.** That a random reservoir fails associative
  recall, and the whole "what reservoirs are and are not good at" model in §3,
  are unread. **That model is steering the entire task choice.**
- **Forward-Forward**, listed in note 002 §3 with a "partial" C2 verdict, was
  assessed entirely from memory.
- **SWIM and gossip protocols**, still the highest-value unread material for
  note 003's false-positive problem.
- **Everything about SORN**, which the predecessor called the closest existing
  system to what it had built.
