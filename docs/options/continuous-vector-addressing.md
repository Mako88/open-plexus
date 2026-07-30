# Option record — address the store by a continuous vector

> **RECORD ONLY. This file carries no status.** Chosen, refused, untried or live-both lives
> in [DECISIONS.md](../../DECISIONS.md) alone. Here there are only events, and events do not
> un-happen, so nothing here can go stale. **Absence means untried.**
> Format and the CONFIG block: [README.md](README.md).

---

## What exists

- Nothing. No code path keys the store by anything but an integer concept id.
- The three mechanisms this option would remove are all built and measured:
  `openplexus/keys.py` (identity-derived addressing), the occupancy gate's
  structurally-zero read, and `AddressSketch`.

---

## What was tried, and what came back

### Refused on blast radius, with the three casualties named — `note 052 §1`

    CONFIG  when    2026-07-29
            source  note 052
            script  none -- design pass
            task    design pass, nothing built
            model   identity addressing, occupancy gate, hashed AddressSketch
            knobs   none
            scale   n/a

Not a measurement. An argument from what it destroys, and the note is specific rather than
vague about *"a big change"*:

- **Exact addressing.** Note 035 measured interference as `O(N * rho)`. Two similar images
  give two similar keys, which is `rho` rising **by construction** — the thing identity
  addressing exists to avoid, and the reason note 045 keeps similarity in a separate index.
- **The gate's structurally-zero bar.** Decision 148 works because an unwritten address
  reads *exactly* 0.0. With continuous keys, "near a written address" reads *nearly* zero
  and the bar becomes a tuned constant — which is note 049's P3, the thing decisions 147
  and 148 spent a day escaping.
- **The sketch.** `AddressSketch` hashes by sign patterns and needs exact repeats to
  collide. Continuous inputs never repeat exactly.

The note's own summary of the balance: every strength this architecture has measured —
exact recall, exact membership, no interference between distinct things — rests on
discrete identity, and the field does the discrete thing routinely (VQ-VAE, discrete audio
codecs), so it is also the choice with the most precedent.

### The same refusal reached again from the addressing side — `note 067`

    CONFIG  when    2026-07-29
            source  note 067
            script  none -- design pass
            task    design pass
            model   store addressed by `(entity, relation)`
            knobs   none
            scale   n/a

Reading the tree as a tree found a second row proposing content-derived keys for entities,
which is the same thing under another name: **nearby addresses is what is refused, however
the nearness arises.** Note 067 then split the refusal — it is right for entities and does
not transfer to relations, where twenty items must be *comparable* rather than exactly
separated and the entity side of the pair supplies the exactness.

That half has its own record: [structured-relations.md](structured-relations.md).
