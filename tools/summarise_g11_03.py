"""Does binding on a token PAIR buy anything in bits per character?

Note 033 proved the single-token store is a bigram count table and **cannot**
represent a trigram, so the bigram bar was a ceiling. Note 034 lifted it — 0.533
to 1.000 on a step a bigram cannot resolve — and measured the price: seven times
as many distinct keys on this corpus, so a store that fills seven times sooner.

**Those two effects are in different units.** A higher ceiling and a noisier
store can land either side of the 5.466 bits the model scores today, and only
this table puts them in the same unit.

## Why the comparison is PAIRED

The two arms differ by one flag and share everything else — seed, corpus, split,
temperature grid. So the honest statistic is the per-seed DIFFERENCE, not two
means with their own spreads: a seed that is hard for one arm is hard for the
other, and subtracting removes that shared difficulty. Note 029 records the run
where the unpaired version reported a lead the paired version showed was noise.

A difference is reported as significant only when it exceeds the spread ACROSS
seeds of that same difference.
"""

from __future__ import annotations

from tools.recovery import load, mean_and_error, require


def main() -> int:
    rows = require(load(), "width", "context", "bits_calibrated", "seed",
                   "uniform")
    if not rows:
        print("no records matched")
        return 1

    widths = sorted({r["width"] for r in rows})
    first = rows[0]
    bars = {name: first[name]
            for name in ("uniform", "unigram", "bigram", "trigram")}
    print(f"vocabulary {first['vocab_size']}, "
          f"{first['test_characters']} scored test characters\n")
    print("the bars, on this corpus and this split:")
    for name, value in bars.items():
        print(f"  {name:>8} {value:.3f} bits/char")
    print("\n  bigram is no longer the model's CEILING with context keys on,")
    print("  which is the whole reason this sweep exists")

    def cell(width, context, field):
        return {r["seed"]: r[field] for r in rows
                if r["width"] == width and bool(r["context"]) is context}

    for field in ("bits_calibrated", "bits_raw", "accuracy"):
        print(f"\n== {field} ==")
        print(f"{'width':>7}{'single-token':>20}{'pair key':>20}"
              f"{'paired difference':>26}")
        for width in widths:
            off, on = cell(width, False, field), cell(width, True, field)
            if not off or not on:
                print(f"{width:>7}{'missing':>20}")
                continue
            shared = sorted(set(off) & set(on))
            if not shared:
                print(f"{width:>7}   arms share no seed, so nothing is paired")
                continue
            a, ae = mean_and_error([off[s] for s in shared])
            b, be = mean_and_error([on[s] for s in shared])
            d, de = mean_and_error([on[s] - off[s] for s in shared])
            print(f"{width:>7}{a:>13.3f} +/-{ae:.3f}{b:>13.3f} +/-{be:.3f}"
                  f"{d:>18.3f} +/-{de:.3f}")

    print("\n== the verdict, per width ==")
    print("  bits: LOWER is better, so a negative difference means the pair")
    print("  key won\n")
    any_win = False
    for width in widths:
        off = cell(width, False, "bits_calibrated")
        on = cell(width, True, "bits_calibrated")
        shared = sorted(set(off) & set(on))
        if not shared:
            print(f"  width {width:>4}: missing an arm")
            continue
        d, de = mean_and_error([on[s] - off[s] for s in shared])
        if abs(d) <= de:
            verdict = "no difference the seeds can distinguish"
        elif d < 0:
            verdict = f"the PAIR KEY wins by {-d:.3f} bits"
            any_win = True
        else:
            verdict = (f"the pair key LOSES by {d:.3f} bits -- the capacity "
                       f"cost outweighs the higher ceiling")
        print(f"  width {width:>4}: {verdict}")

    print()
    if any_win:
        print("  At least one width pays for the extra keys. Whether it also")
        print("  beats the BIGRAM bar is the separate question above, and a")
        print("  win over the single-token arm is not a win over the bar.")
    else:
        print("  **No width pays for the extra keys.** The ceiling was real and")
        print("  it moved, but the store cannot afford the resolution at these")
        print("  widths. That is a capacity result, not a refutation of the")
        print("  ceiling -- and the next question is whether it crosses over at")
        print("  a width we can reach, or never.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
