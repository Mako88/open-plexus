# Option record — every input becomes a discrete concept id

> **RECORD ONLY. This file carries no status.** Chosen, refused, untried or live-both lives
> in [DECISIONS.md](../../DECISIONS.md) alone. Here there are only events, and events do not
> un-happen, so nothing here can go stale. **Absence means untried.**
> Format and the CONFIG block: [README.md](README.md).

---

## What exists

- `openplexus/concepts.py` — `Surfaces`, `Identity`, `ByTable`. One concept, many
  surfaces; every token in the vocabulary maps somewhere, and the same token maps to the
  same concept on every node forever.
- Nothing non-text. No quantiser is built, for any modality.

---

## What was tried, and what came back

### The blast-radius argument — `note 052 §1`

    CONFIG  when    2026-07-29
            source  note 052
            script  none -- design pass
            task    design pass, nothing built
            model   the model as it stood: identity addressing, hashed sketch
            knobs   none
            scale   n/a

The question is how a non-text input reaches a concept when a text token is already a
discrete id and an image patch or an audio frame is a vector. Two answers: quantise to a
discrete surface id, or address the store by a continuous vector.

The note's argument is that quantising is *"choosing to keep what exists"*, and that the
alternative has a blast radius close to total. Its own statement of what quantising still
costs: something must learn the quantiser, a bad one merges two things that should stay
distinct, **and this architecture cannot recover from that because it will then address
them identically.** That failure is silent.

What it changed about the roadmap was additive — *"fit `ContentIndex` across paired
streams"* rather than *"re-key the store"*.

### John's ruling — `163 §1`

    CONFIG  when    2026-07-29
            source  decision 163
            script  none -- a ruling
            task    n/a
            model   n/a
            knobs   none
            scale   n/a

> *"I agree with you on the exact addressing using a quantizer."*

**Target modalities: video, audio, text and images.** PDFs come in as text. Off-the-shelf
where it works, our own where it does not — John named both directions himself: the
distributed setting may rule out a stock solution, and there may equally be something
better for this case than what exists.

### The quantiser answers ADDRESSING, not IDENTITY — John, 2026-07-30

    CONFIG  when    2026-07-30
            source  GOALS.md §1.2b
            script  none -- a ruling that narrows an earlier one
            task    n/a
            model   n/a
            knobs   none
            scale   n/a

> *"I think fundamentally that's what needs to happen rather than using an external
> quantizer"* — on learning that a picture, a sound and a word name one thing by
> **experiencing them together over time**, the way a child does.

**Read against `163 §1` this looks like a reversal and is not one**, and separating the
two questions is what keeps this record from drifting:

- **ADDRESSING** — how does a non-text input become a discrete id at all, so the store
  can be keyed by it? `163 §1` stands. A quantiser, borrowed where it works.
- **IDENTITY** — how does the system come to hold that *this* image id and *that* word id
  name the same concept? **Not the quantiser's job, and it cannot do it**: two
  independently quantised modalities never agree by accident. Learned from temporal
  co-occurrence, varied across contexts.

**And this record already contains the argument for the change.** The entry above states
the quantiser's cost: *"a bad one merges two things that should stay distinct, and this
architecture cannot recover from that because it will then address them identically. That
failure is silent."*

**Learned identity removes exactly that failure mode.** A merge becomes an association the
system can weaken on later evidence, rather than an address collision it can never see. So
the objection this record raised against quantised identity is answered by not asking the
quantiser to supply identity — which is a narrowing of `163`, not a contradiction of it.

**The open tension, recorded because it is not resolved.** Concept partitioning needs a
DETERMINISTIC owner per concept — consistent hashing over a stable id, computable by any
node without asking. Learned identity is negotiated and therefore not stable, so it cannot
be hashed. Two directions exist and neither is chosen: a cheap deterministic id for routing
with learned structure for meaning, or convergence between nodes by gossip. See
[concept-partitioning.md](concept-partitioning.md).

Three things recorded with the ruling:

- **C1 does not bite here.** A quantiser is preprocessing, run once per input at the edge,
  before anything reaches the store. It is not in the learning loop, so stock encoders are
  genuinely available.
- **Candidates exist for all four modalities** — residual-VQ audio codecs, the
  VQ-VAE/VQGAN family for images, frame tokenisers plus temporal handling for video.
- **The cost, named at the time:** a stock tokeniser is a large pretrained model in the
  pipeline. It touches nothing the learner does, but *"our system plus a pretrained
  encoder"* is a different claim from *"our system"*, and it is better named now than in
  a write-up.

### The ordering principle behind it, with a correction — `163`

    CONFIG  when    2026-07-29
            source  decision 163
            script  none -- a ruling
            task    n/a
            model   n/a
            knobs   none
            scale   n/a

John: *"I would definitely like to shift over to doing these things because they have such
a large blast radius first… so that the tweaks that start happening are gonna stick around
rather than be completely wasted time because of the architecture changing underneath it."*

The correction recorded beside it: discrete surfaces is the option under which the
architecture does *not* change, so multimodality becomes additive. **The decision on that
list with real blast radius over existing measurements is §3 — what an answer is — and not
§1.**

### The addressing half is now the WHOLE job — John, 2026-07-31

    CONFIG  when    2026-07-31
            source  GOALS.md, the grounding section
            script  none -- design pass
            task    n/a
            model   n/a
            knobs   none
            scale   n/a

The entry above split addressing from identity and left the routing question open. It is
now answered: **a surface id is the only thing that gets a durable address**, because a
concept gets none — it is an equivalence class reached by walking, per
[identity-without-a-global-id.md](identity-without-a-global-id.md).

That makes this record's scope narrower and firmer than it was. A quantiser must produce an
id that is **stable for one percept**, and it is no longer being asked to make an image and
a word agree. The failure mode this record warned about — a bad quantiser silently merging
two things by addressing them identically — is unchanged in kind but now applies only
within a modality, where it is far easier to check.
