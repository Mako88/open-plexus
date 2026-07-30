# Option record — transport: vote-based, with suspicion and a deadline

> **RECORD ONLY. This file carries no status.** Chosen, refused, untried or live-both lives
> in [DECISIONS.md](../../DECISIONS.md) alone. Here there are only events, and events do not
> un-happen, so nothing here can go stale. **Absence means untried.**
> Format and the CONFIG block: [README.md](README.md).

---

## What exists

- `openplexus/transport.py`, the failure detector, and the deadline branch that settles
  short on what arrived.
- `tests/test_transport.py`, `tests/test_deadline_settles_short.py`,
  `tests/test_silent_steps.py`, `tests/test_departure_with_window.py`.
- `docs/SCALE.md` carries `d_max`.

---

## What was tried, and what came back

### `d_max` is about 640 ms, measured over real containers — `128`

    CONFIG  when    2026-07-28
            source  decision 128
            script  unrecorded
            task    a distributed step
            model   4 nodes, width 16
            knobs   tc netem: delay 80 ms + 20 ms jitter + 2% loss
            scale   3x a measured p99

Docker bridge with `tc netem`. **A floor, not a constant** — a real WAN raises it — and the
entry also corrects a number published the cycle before.

Everything in the peer transport's latency arithmetic is priced against this figure, so it
is load-bearing well beyond its own component.

### The detector ejected nodes permanently, and SWIM says not to — `126`, `127`

    CONFIG  when    2026-07-28
            source  decisions 126 and 127
            script  unrecorded
            task    node failure and recovery
            model   failure detector
            knobs   eject against suspect-and-retry
            scale   unrecorded

SWIM says **suspect and retry**. Fixed. `127` also records that the paper was never
unreadable and describes our bug directly, which is CLAUDE.md's search-the-literature rule
paying and being noted as late.

### The deadline's actual branch had NO test until a silent peer existed — `169`

    CONFIG  when    2026-07-29
            source  decision 169
            script  tests/test_deadline_settles_short.py
            task    a step where a peer goes silent
            model   settle short on what arrived
            knobs   none
            scale   three attempts at one assertion

`steps_settled_short` was asserted in exactly one place, **to be empty**. So the branch that
makes the deadline mean anything was never exercised.

The same entry is the sensitivity calibration: **three attempts at one timing assertion, and
the first two both passed when written** — which is why a timing assertion now needs a
sensitivity check before it counts.
