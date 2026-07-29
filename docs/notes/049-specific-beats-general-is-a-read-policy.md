# 049 — "Specific beats general" is a read policy, not a new representation

**Status:** a design, nothing built. Written to correct something I told John
about decision 144 an hour after telling him.
**Answers:** the question STATE now carries — *can a store hold a family default
and a per-entity override at the same time?*

---

## The correction

Reporting decision 144 I said the next mechanism is *"a store that can hold a
family default and a per-entity override at once — a representation question
rather than an addressing one"*.

**That overstates it.** The store is one `d × d` matrix addressed by key vectors.
A fact written at the **surface** key and a family default written at the
**concept** key are two different addresses in the same store. They do not
collide, and nothing about the representation prevents both existing.

What collides is narrower: **`ByConcept` maps every token to its concept**, so
the surface address is never written and never read. The override has nowhere to
live — not because the store cannot hold it, but because the addressing throws
the surface away before the store ever sees it.

So this is a **read policy**, and the fix is small.

## The mechanism

    write   at BOTH addresses: the surface key and the concept key
    read    the surface first. If what comes back is strong enough to be a
            real binding, use it. Otherwise fall back to the concept.

That is inheritance with override, and it is the oldest idea in the book — which
is the point. GOALS' standing rule is to take mechanisms from computer science
where the problem is well understood, and "specific overrides general, with a
fallback" is understood to the point of dullness.

It also matches what the two arms of g19-01 already do *separately*:

    ungrouped   surface addressing      exception 0.783   transfer 0.061
    concept     concept addressing      exception 0.371   transfer 0.471

**Each arm is good at exactly what the other is bad at.** A reader that consults
both should get the better of the two at every position, and the experiment is
whether it does.

## What has to be decided rather than assumed

**"Strong enough" needs a threshold, and a tuned constant is not a mechanism.**
Decision 130's gate faced this and answered it: fire on the median of the model's
own training margins, computed without labels and without touching the test set.
The same trick applies — the surface retrieval's norm against the distribution of
norms the model has already seen.

**And the fallback must not be a second guess.** If the surface read is weak and
the concept read is also weak, answering anyway is worse than the arm that had
one address, because two weak reads sum to a confident wrong answer. That is
decision 69's lesson about sums and it is the failure most likely here.

## PREDICTIONS, to register before building

  P1  THE GATE. A two-level reader scores within 0.05 of `ungrouped` on
      EXCEPTION **and** within 0.05 of `concept` on TRANSFER. Anything less on
      either side means it is not getting the better of the two, it is getting
      an average.

  P2  THE RAIL. On the no-exception task it does not fall below decision 143's
      `concept` numbers (direct 0.997, transfer 0.998). Adding a fallback must
      not cost anything where the fallback is never needed.

  P3  THE FALSIFIER. The threshold generalises across `n_values` and
      `family_size` without being re-tuned. If it has to move per configuration,
      it is a fitted constant wearing a mechanism's clothes, and decision 130's
      version of this was explicit that the *threshold* generalises while the
      *number* does not.

**What would refute the approach:** P1 failing with both reads available. The
information would be present at two addresses, the model would have both, and it
still could not choose — which would say the problem is selection rather than
storage, and that is a different and harder question.

## The cost, honestly

This touches `run`, which is the one file where a change invalidates the
comparison set. It is a second read per position — the wire cost doubles at a
queried position, which matters for C1 and should be quoted rather than
discovered. And `retrieval.py` already holds the seam for how a store is read, so
it belongs there rather than in another branch inside `run`.

**Not built. Not started.** The threshold question above is the part that needs a
decision rather than a keyboard.
