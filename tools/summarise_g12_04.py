"""Score g12-04, and turn the round-trip distribution into a d_max.

The number this exists to produce is **`d_max` in milliseconds**. Note 003
unified it as the C2 asynchrony bound and the C3 churn timeout and never
measured it; C2 says a design must state a bound it tolerates, and every waiting
parameter in this project is currently counted in steps.

SWIM's rule, from the paper: the protocol period must be at least three times the
round-trip estimate, and the estimate is the mean or the 99th percentile of the
round-trip distribution. `t_prime_floor_ms` is that rule evaluated at p99.
"""

from __future__ import annotations

import statistics

from tools.recovery import load

#: The order the cells are worth reading in -- clean first, then one impairment
#: at a time, then everything at once. Anything not listed still prints.
ORDER = ("clean", "delay-20", "delay-80", "delay-80-jitter-20", "loss-2",
         "delay-80-jitter-20-loss-2")

#: In-process on loopback, no impairment, measured before dispatch. The floor
#: any real link has to beat, and the evidence for P2.
IN_PROCESS_P99_MS = 24.19


def name_of(record: dict) -> str:
    """Which cell a record came from, read from what it RAN WITH.

    Rule 11b: the workflow says what SHOULD have run and the filename says what
    was meant to be fetched; only the data says what happened.
    """
    parts = []
    if record.get("delay"):
        parts.append(f"delay-{record['delay']}")
    if record.get("jitter"):
        parts.append(f"jitter-{record['jitter']}")
    if record.get("loss"):
        parts.append(f"loss-{record['loss']}")
    return "-".join(parts) if parts else "clean"


def main() -> None:
    records = load()
    if not records:
        print("NO RECORDS -- the matrix produced nothing, which is a failure "
              "of the run rather than a result")
        return

    timed = [r for r in records if r.get("votes_timed")]
    if not timed:
        print("!! records returned but NONE carries a latency sample. The "
              "driver ran without the instrumentation, so nothing here is "
              "about round trips.")
        return
    if len(timed) != len(records):
        print(f"!! {len(records) - len(timed)} of {len(records)} records have "
              f"no latency sample and are dropped")

    # The apostrophe in "T' floor" lives in a variable rather than inline: an
    # escaped quote inside an f-string expression is a syntax error, and this
    # one reached CI because nothing imports a summariser at check time.
    t_floor = "T' floor"
    print(f"\n{'cell':<28} {'n':>6} {'mean':>9} {'p50':>9} {'p99':>9} "
          f"{'max':>9} {t_floor:>10}")
    for record in sorted(timed, key=lambda r: (
            ORDER.index(name_of(r)) if name_of(r) in ORDER else 99)):
        print(f"{name_of(record):<28} {record['votes_timed']:>6} "
              f"{record['rtt_ms_mean']:>9.2f} {record['rtt_ms_p50']:>9.2f} "
              f"{record['rtt_ms_p99']:>9.2f} {record['rtt_ms_max']:>9.2f} "
              f"{record['t_prime_floor_ms']:>10.1f}")
    print("all figures in milliseconds; T' floor is SWIM's 3 x p99")

    by_name = {name_of(r): r for r in timed}
    print("\nPREDICTIONS")

    ratios = [(name_of(r), r["rtt_ms_p99"] / max(r["rtt_ms_mean"], 1e-9))
              for r in timed]
    worst = min(ratio for _, ratio in ratios)
    print(f"  P1  p99 exceeds the mean by >2x everywhere: smallest ratio "
          f"{worst:.1f}x -> {'CONFIRMED' if worst > 2 else 'REFUTED'}")
    print("      The rail. If a mean would do, the percentiles were not worth "
          "building.")

    if "clean" in by_name:
        clean = by_name["clean"]["rtt_ms_p99"]
        near = clean <= IN_PROCESS_P99_MS * 2
        print(f"  P2  the clean link's p99 is compute-dominated -- within 2x of "
              f"the in-process {IN_PROCESS_P99_MS} ms: {clean:.2f} ms -> "
              f"{'CONFIRMED' if near else 'REFUTED'}")

    if "clean" in by_name and "delay-80" in by_name:
        moved = by_name["delay-80"]["rtt_ms_p50"] - by_name["clean"]["rtt_ms_p50"]
        print(f"  P3  80 ms of delay moves the MEDIAN by roughly 80 ms: "
              f"{moved:+.1f} ms -> "
              f"{'CONFIRMED' if 40 <= moved <= 160 else 'REFUTED'}")

    if "delay-80" in by_name and "delay-80-jitter-20" in by_name:
        plain = (by_name["delay-80"]["rtt_ms_p99"]
                 - by_name["delay-80"]["rtt_ms_p50"])
        jittered = (by_name["delay-80-jitter-20"]["rtt_ms_p99"]
                    - by_name["delay-80-jitter-20"]["rtt_ms_p50"])
        print(f"  P4  jitter widens p50-to-p99: {plain:.1f} ms -> "
              f"{jittered:.1f} ms -> "
              f"{'CONFIRMED' if jittered > plain else 'REFUTED'}")

    if "clean" in by_name and "loss-2" in by_name:
        d50 = by_name["loss-2"]["rtt_ms_p50"] - by_name["clean"]["rtt_ms_p50"]
        d99 = by_name["loss-2"]["rtt_ms_p99"] - by_name["clean"]["rtt_ms_p99"]
        print(f"  P5  2% loss moves p99 far more than p50: p50 {d50:+.1f} ms, "
              f"p99 {d99:+.1f} ms -> "
              f"{'CONFIRMED' if d99 > d50 else 'REFUTED'}")

    impaired = [r["t_prime_floor_ms"] for r in timed if name_of(r) != "clean"]
    if impaired:
        largest = max(impaired)
        print(f"  P6  T' floor lands in the HUNDREDS of ms on impaired links: "
              f"largest {largest:.0f} ms -> "
              f"{'CONFIRMED' if largest >= 100 else 'REFUTED'}")
        print(f"\n  d_max, MEASURED: **{largest:.0f} ms** on the worst link "
              f"tested.")
        print("  That is the C2 bound and the C3 timeout, in a unit that can "
              "be checked against a network -- and it replaces "
              "RETRY_AFTER_STEPS, which counts steps of no fixed duration.")
        print("  It is a FLOOR from these links, not a universal constant: a "
              "worse link raises it, and nothing here tested one.")

    disagreed = [name_of(r) for r in records
                 if r.get("agrees_with_one_process") is False
                 and not r.get("absent")]
    if disagreed:
        print(f"\n!! {disagreed} disagreed with the single-process model with "
              f"nobody departing. A wrong answer makes its timings "
              f"uninterpretable -- fix that before reading anything above.")


if __name__ == "__main__":
    main()
