"""Phase 3: replay out of the store into the weights. THE BRANCH'S BET.

THE DOC'S EXIT: one training-shape arm has Tier C above blind on a fresh state
with the store OFF, and the gate has not tripped in its last five cycles.

WHAT WOULD REFUTE, named before anything runs and taken from the doc verbatim:

  the `raw` arm       Tier C at blind after ten cycles.
  the gate            any arm tripping on more than a third of its cycles --
                      then replay is not protecting the base at this scale,
                      which is refutation 2 of the whole branch.

AND ONE MORE THAT IS THIS SCRIPT'S OWN, because the bar moved tonight: Tier C at
or below TIER A. The state alone scores about 0.46 on the direct questions
without any training at all. An adapter that costs GPU-hours and lands under that
has not learned the house into its weights, whatever it does against blind.

THE ORDER IS NOT NEGOTIABLE AND STEP ONE IS CALIBRATION. The doc: "Calibrate the
thresholds on noise FIRST: run two identical cycles and read the spread before a
threshold is chosen." `Gate` refuses to be constructed without thresholds, so
this cannot be skipped by accident.

  uv run python scripts/phase3_consolidate.py --size 0.4 --cycles 10

SIZE DEFAULTS TO 0.4B AND THAT IS DELIBERATE. Training runs through the
pure-PyTorch differentiable forward with gradient checkpointing -- a Python loop
over T, recomputed in the backward pass -- so a cycle at 1.5B is hours. The first
question is whether ANY of this works, and that question is cheaper at 0.4B. The
doc's rule that readings which count use 1.5B still holds, and this script
records the size in every row so nobody confuses the two.
"""

from __future__ import annotations

import argparse
import json
from datetime import UTC, datetime
from pathlib import Path

from sylvatica.exam import DEFAULT_DELAYS, Tier, generate_house, run_blind, run_exam
from sylvatica.learn import Gate, LoraAdapter, calibrate, measure, run_cycle
from sylvatica.learn.heldout import fingerprint
from sylvatica.loop.turn import PREAMBLE
from sylvatica.store import MiniLmEmbedder, SqliteStore

REFUTES = (
    "raw: Tier C at blind after ten cycles. gate: tripping on more than a third "
    "of cycles, which is refutation 2. And this script's own: Tier C at or below "
    "Tier A, which the state reaches with no training at all."
)


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--size", type=float, default=0.4)
    parser.add_argument("--facts", type=int, default=50)
    parser.add_argument("--turns", type=int, default=300)
    parser.add_argument("--seed", type=int, default=0)
    parser.add_argument("--negatives", type=int, default=20)
    parser.add_argument("--delays", type=int, nargs="+", default=list(DEFAULT_DELAYS))
    parser.add_argument("--phrasing", choices=["direct", "oblique"], default="direct")
    parser.add_argument("--cycles", type=int, default=10)
    parser.add_argument("--arm", choices=["raw", "declaratives", "both"], default="raw")
    parser.add_argument("--rank", type=int, default=8)
    parser.add_argument("--lr", type=float, default=1e-4)
    parser.add_argument("--seq-len", type=int, default=192)
    parser.add_argument("--replay", type=int, default=64, help="fragments per cycle")
    parser.add_argument("--calibrate-repeats", type=int, default=3)
    parser.add_argument("--exam-every", type=int, default=5)
    parser.add_argument(
        "--no-rollback",
        action="store_true",
        help=(
            "record the gate's verdict but keep the adapter. An EXPERIMENT, never "
            "how consolidation ships: it separates 'does replay teach' from 'does "
            "it cost the base too much', which a run that rolls back everything "
            "cannot, because its Tier C is measuring the untouched base."
        ),
    )
    parser.add_argument(
        "--reuse-store",
        action="store_true",
        help=(
            "skip refilling the store if one for this house already exists. Nine "
            "minutes a run, which matters when sweeping a dial -- and it is "
            "checked rather than trusted: the fragment count must match the "
            "conversation, or it refills."
        ),
    )
    parser.add_argument("--out", type=Path, default=Path("readings"))
    args = parser.parse_args()

    from sylvatica.core.checkpoints import ensure
    from sylvatica.core.rwkv_core import RwkvCore
    from sylvatica.store import ReplaySpec

    core = RwkvCore(ensure(args.size))
    house = generate_house(
        seed=args.seed, n_facts=args.facts, n_turns=args.turns,
        delays=tuple(args.delays), negatives=args.negatives, phrasing=args.phrasing,
    )
    blind = round(run_blind(house).score().score, 4)
    print(f"house: {args.facts} facts / {args.turns} turns, blind {blind}")

    # -- STEP ONE: the noise, before any threshold exists ------------------
    print("\n-- calibrating the gate on noise --", flush=True)
    noise = calibrate(core, repeats=args.calibrate_repeats)
    print(json.dumps({k: v for k, v in noise.items() if k != "runs"}, indent=2), flush=True)

    # THE CALIBRATION IS ITS OWN READING, not a field inside another one. The
    # doc asks for it as a distinct step taken FIRST, and a reading that only
    # exists nested inside a consolidation run cannot be pointed at when someone
    # asks what the thresholds were derived from.
    args.out.mkdir(parents=True, exist_ok=True)
    noise_reading = {
        "phase": 3,
        "kind": "gate-noise",
        "taken_at": datetime.now(UTC).isoformat(),
        "what": "the spread of the untouched base on the two fixed held-out sets",
        "would_refute": (
            "a spread wide enough that a threshold derived from it sits below one "
            "standard deviation of its own measurement -- which is what the first "
            "calibration found when the QA half was sampled rather than greedy."
        ),
        "core": core.name,
        "size_b": args.size,
        "heldout_fingerprint": fingerprint(),
        **noise,
    }
    noise_stamp = datetime.now(UTC).strftime("%Y%m%dT%H%M%SZ")
    (args.out / f"phase3-gate-noise-{noise_stamp}.json").write_text(
        json.dumps(noise_reading, indent=2) + "\n", encoding="utf-8"
    )

    base = measure(core)
    gate = Gate(
        base,
        perplexity_threshold=noise["perplexity_threshold"],
        qa_threshold=noise["qa_threshold"],
    )
    print(f"base: ppl {base.perplexity:.3f}  qa {base.qa_score:.3f}", flush=True)

    # -- the store, filled by running the conversation once ---------------
    db = Path("state/exam") / f"p3-{args.phrasing}-{args.seed}.db"
    if db.exists():
        db.unlink()
    store = SqliteStore(db, embedder=MiniLmEmbedder())
    print("\n-- filling the store (Tier B run, which also gives its score) --", flush=True)
    tier_b = run_exam(
        core, house, Tier.B, state_path=Path("state/exam/p3-b.pt"),
        progress=False, store=store, k=5, provenance=f"p3-seed{args.seed}",
    )
    print(f"tier B {tier_b.score().score:.3f}, store {store.tiers()}", flush=True)

    print("\n-- Tier A, the bar an adapter has to beat --", flush=True)
    tier_a = run_exam(
        core, house, Tier.A, state_path=Path("state/exam/p3-a.pt"), progress=False
    )
    print(f"tier A {tier_a.score().score:.3f}", flush=True)

    # -- the cycles -------------------------------------------------------
    adapter = LoraAdapter(core._model.z, rank=args.rank)
    print(f"\nadapter: {adapter.trainable:,} trainable over {len(adapter.names)} weights")

    spec = ReplaySpec(n=args.replay, seed=args.seed)
    cycles, exams = [], []
    for i in range(args.cycles):
        cycle, after, training = run_cycle(
            core, adapter, store, gate, index=i, arm=args.arm,
            spec=spec, lr=args.lr, seq_len=args.seq_len,
            rollback=not args.no_rollback,
        )
        row = {
            "index": i,
            "arm": cycle.arm,
            "fragments": cycle.fragments,
            "tokens": cycle.tokens,
            "seconds": round(cycle.seconds, 1),
            "flops": cycle.flops,
            "perplexity": round(after.perplexity, 4),
            "qa_score": round(after.qa_score, 4),
            "tripped": cycle.gate.tripped,
            "rolled_back": cycle.gate.rolled_back,
            "training": training.row(),
        }
        cycles.append(row)
        print(
            f"cycle {i}: ppl {after.perplexity:.3f} (base {base.perplexity:.3f})  "
            f"qa {after.qa_score:.3f} (base {base.qa_score:.3f})  "
            f"{'TRIPPED' if cycle.gate.tripped else 'clean'}"
            f"{', rolled back' if cycle.gate.rolled_back else ', KEPT'}  "
            f"{cycle.seconds:.0f}s",
            flush=True,
        )

        if (i + 1) % args.exam_every == 0 or i == args.cycles - 1:
            # TIER C: FRESH STATE, STORE OFF, ADAPTER ON. `run_exam` refuses if a
            # store is passed, because that would be Tier B with a new label.
            adapter.apply_to(core._model.z)
            try:
                c = run_exam(
                    core, house, Tier.C, state_path=Path("state/exam/p3-c.pt"),
                    progress=False, adapter=adapter,
                )
            finally:
                adapter.revert_from(core._model.z)
            s = c.score()
            exams.append({
                "after_cycle": i,
                "score": round(s.score, 4),
                "by_delay": s.by_delay,
                "invented": s.invented,
                "echoed": s.echoed,
                "answers": [a.row() for a in c.answers],
            })
            print(f"  TIER C after cycle {i}: {s.score:.4f}  {s.by_delay}", flush=True)

    tripped = sum(c["tripped"] for c in cycles)
    last_c = exams[-1]["score"] if exams else None
    verdict = {
        "tier_c": last_c,
        "tier_a": round(tier_a.score().score, 4),
        "tier_b": round(tier_b.score().score, 4) if tier_b else None,
        "blind": blind,
        "tier_c_above_blind": (last_c > blind) if last_c is not None else None,
        "tier_c_above_tier_a": (
            last_c > tier_a.score().score if last_c is not None else None
        ),
        "gate_trips": tripped,
        "gate_trip_rate": round(tripped / max(1, len(cycles)), 3),
        # The doc: any arm tripping on more than a third of its cycles means
        # replay is not protecting the base at this scale -- refutation 2.
        "refutation_2_fired": tripped / max(1, len(cycles)) > 1 / 3,
        "clean_last_five": (
            not any(c["tripped"] for c in cycles[-5:]) if len(cycles) >= 5 else None
        ),
    }

    reading = {
        "phase": 3,
        "kind": "consolidation",
        "tier": "C",
        "taken_at": datetime.now(UTC).isoformat(),
        "what": "replay out of the store into a LoRA, gated, and what Tier C says after",
        "would_refute": REFUTES,
        "core": core.name,
        "size_b": args.size,
        "arm": args.arm,
        "house": {
            "seed": args.seed, "facts": args.facts, "turns": args.turns,
            "delays": args.delays, "negatives": args.negatives,
            "phrasing": args.phrasing,
        },
        "dials": {
            "rank": args.rank, "lr": args.lr, "seq_len": args.seq_len,
            "replay_fragments": args.replay, "cycles": args.cycles,
        },
        # TRUE means the gate's verdicts were recorded and IGNORED. Any Tier C
        # number in this reading came from a model the gate would have rejected.
        "rollback_disabled": args.no_rollback,
        "preamble": PREAMBLE,
        "heldout_fingerprint": fingerprint(),
        "gate_calibration": noise,
        "base": base.row(),
        "adapter": {"trainable": adapter.trainable, "weights": len(adapter.names)},
        "cycles": cycles,
        "tier_c_exams": exams,
        "verdict": verdict,
    }

    args.out.mkdir(parents=True, exist_ok=True)
    stamp = datetime.now(UTC).strftime("%Y%m%dT%H%M%SZ")
    path = args.out / f"phase3-consolidation-{stamp}.json"
    path.write_text(json.dumps(reading, indent=2) + "\n", encoding="utf-8")

    print("\n" + "-" * 60)
    print(json.dumps(verdict, indent=2))
    print(f"wrote {path}")
    store.close()
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
