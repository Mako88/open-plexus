# Note 010 — Tagging and capture, and why it does not rescue this benchmark

[g7-01](../../experiments/sweeps/g7-01-the-oracle-gate.txt) established that
perfect judgement about what to store is worth everything: at 384 steps, devices
holding one number go from 0.404 to 1.000 when only the four useful bindings are
kept. It also stated the blocker — **nothing available at storage time
distinguishes a useful binding from a useless one**, since on MQAR both are
equally unpredictable when first seen, and surprise would actively prefer the
filler.

John's source list pointed at the biological answer to exactly that blocker, so
this note reads it rather than guessing.

## What the mechanism actually is

From Lehr, Luboeinski and Tetzlaff (*Scientific Reports*, 2022),
[s41598-022-22430-7](https://www.nature.com/articles/s41598-022-22430-7):

- A synapse is **tagged** when its early-phase weight change exceeds a threshold
  — the paper's condition is that `h(t) − h₀` exceeds `θ_tag`.
- **Plasticity-related proteins** are synthesised when enough early-phase change
  accumulates *across a neuron's synapses*, against a threshold the paper gives
  as `θ_pro(NM) = 1/(NM + 0.001)` — so more neuromodulator means a lower bar.
- **Consolidation requires both**: late-phase change depends on the proteins
  *and* on the tag being present. Tags are transient; the paper notes they have
  vanished roughly three hours after learning.

Two properties matter for us.

**It is not a storage-time decision.** The tag is set immediately and cheaply;
what decides whether the change survives arrives *later*. That is precisely the
"retroactive" in the paper's title, and precisely the shape g7-01 said the
problem needs.

**And the protein signal is per-neuron, not global.** It is computed from one
neuron's own synapses. In our terms that is a *machine-local* quantity — a
machine could compute it from its own rows without consulting anyone. **So the
mechanism is C1-compatible**, which was not obvious and is the main reason it is
worth taking seriously.

## What it would look like here

1. Every binding is written weakly into a fast-decaying store, and tagged.
2. Some later signal marks a stretch as worth keeping.
3. Tagged bindings present when that signal arrives are consolidated into a
   durable store; everything else decays.

The storage decision moves from "is this worth keeping?" — unanswerable at the
time — to "was that worth having kept?", which is answerable once the evidence
arrives.

## Why it still does not rescue MQAR

**On this benchmark the later signal never comes in time.**

- The tag threshold cannot select. Our early-phase change is
  `M += v ⊗ k_prev` with unit-norm projections, so every binding produces the
  same magnitude of change. Every synapse tags, or none does. Making the change
  depend on novelty — storing `(v − M k_prev) ⊗ k_prev`, the delta rule applied
  to the fast weights — would give the threshold something to bite on, but on
  MQAR pair bindings and filler bindings are both novel on first sight, so it
  separates nothing.
- The protein signal is per-neuron and per-*period*, not per-binding. It gates
  **when** consolidation happens, not **which** bindings deserve it.
- And the only event that distinguishes a pair from filler is **the query**,
  which in MQAR arrives after the pair is needed and never recurs. Consolidating
  on use is worth nothing when each thing is used exactly once, at the end.

So the honest reading is:

  > **Tagging and capture is the right mechanism for the general problem and MQAR
  > is the wrong benchmark to show it on.** It converts a storage-time decision
  > into a retroactive one, which is exactly the conversion needed — but it pays
  > off only where relevance recurs, or is signalled before the thing is needed.
  > MQAR has neither property.

g7-01 anticipated this outcome and named it: *"a task where usefulness IS locally
predictable is needed... a finding about the benchmark rather than about the
mechanism."* This note is that finding, now with a specific mechanism attached
and a specific reason it does not fit.

## What it implies for the next benchmark

If tagging and capture is to be tested rather than argued about, the task has to
supply at least one of:

- **Recurrence** — the same bindings queried repeatedly, so consolidating on use
  pays off on the second occasion. This is the ordinary situation in language and
  is absent here by construction.
- **An in-sequence relevance signal** arriving while the tag is still alive —
  which is what the paper's three-hour tag lifetime is for.

**Neither requires abandoning what has been measured.** A recurrent variant of
MQAR — the same pairs queried several times across a long sequence — would keep
every existing result comparable while making consolidation-on-use meaningful.
That is a small change to the generator and it is the cheapest way to make this
mechanism testable.

## What is not claimed

The paper models spiking networks with calcium-based plasticity and hours-long
protein dynamics. Nothing here reproduces that, and no claim is made that our
`M += v ⊗ k` abstraction is a model of it. What is borrowed is the *control
structure* — tag now, decide later, consolidate on the conjunction — which is
architecture-independent. The numerical forms above are quoted to say what the
paper does, not adopted.
