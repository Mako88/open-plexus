"""Does this memory beat a bigram on real text?

The table is **width x memory_cap**, in bits per character, against three reference
points measured on the same corpus and the same split.

Three things print separately because a single headline merges them:

- **which bar it clears.** Uniform is not a bar; a model that learns only that
  `e` is common clears it. Unigram says the base rate was learned. Bigram is the
  bar, and it is the fair one for this model because binding the previous token
  to the current one IS a bigram in vector form.
- **raw against calibrated.** The readout is trained discriminatively, so its
  scores are not on a softmax's scale. A large gap means badly scaled; a small
  gap means the raw number was already honest.
- **accuracy**, which does not depend on the temperature at all. Good accuracy
  with bad bits means miscalibration; bad both means it did not learn.

The temperature is fitted on held-out TRAINING chunks. If the chosen value sits
at the edge of `TEMPERATURES`, the calibrated number is a BOUND, and that is
printed rather than left for a reader to notice.
"""

from __future__ import annotations

from tools.recovery import load, mean_and_error

#: Must match experiments/g10_01_first_language.py.
TEMPERATURES = tuple(round(0.01 * 1.3 ** i, 4) for i in range(0, 30))


def main() -> int:
    rows = load()
    if not rows:
        print("no records matched")
        return 1

    widths = sorted({r["width"] for r in rows})
    chunks = sorted({r["cap"] for r in rows})
    first = rows[0]
    bars = {name: first[name]
            for name in ("uniform", "unigram", "bigram", "trigram")}
    print(f"vocabulary {first['vocab_size']}, "
          f"{first['test_characters']} scored test characters\n")
    print("the bars, on this corpus and this split:")
    for name, value in bars.items():
        print(f"  {name:>8} {value:.3f} bits/char")
    print("\n  beating uniform is not evidence of anything; bigram is the bar")

    def cells(width, chunk, field):
        return [r[field] for r in rows
                if r["width"] == width and r["cap"] == chunk]

    for field in ("bits_calibrated", "bits_raw", "accuracy"):
        print(f"\n== {field} ==")
        print(f"{'width':>7}" + "".join(f"{'cap ' + str(c):>18}" for c in chunks))
        for width in widths:
            line = f"{width:>7}"
            for chunk in chunks:
                values = cells(width, chunk, field)
                if not values:
                    line += f"{'missing':>18}"
                    continue
                mean, error = mean_and_error(values)
                line += f"{mean:>11.3f} +/-{error:.3f}"
            print(line)

    print("\n== which bar does each cell clear? ==")
    for width in widths:
        for chunk in chunks:
            values = cells(width, chunk, "bits_calibrated")
            if not values:
                print(f"  width {width:>4} cap {chunk:>5}: missing")
                continue
            mean, error = mean_and_error(values)
            cleared = [n for n, v in bars.items() if mean < v]
            best = cleared[-1] if cleared else "NOTHING, not even uniform"
            print(f"  width {width:>4} cap {chunk:>5}: {mean:.3f} +/- {error:.3f}"
                  f"   clears {best}")

    print("\n== is the calibration PINNED at a grid edge? ==")
    edges = 0
    for width in widths:
        for chunk in chunks:
            chosen = cells(width, chunk, "temperature")
            if not chosen:
                continue
            at_edge = [t for t in chosen
                       if t in (TEMPERATURES[0], TEMPERATURES[-1])]
            if at_edge:
                edges += 1
                print(f"  width {width:>4} cap {chunk:>5}: PINNED at "
                      f"{sorted(set(at_edge))} -- the bits above are a BOUND")
    if not edges:
        print("  no cell pinned; every calibrated number is a value")

    print("\n== the diagnosis ==")
    best = min(
        ((w, c, mean_and_error(cells(w, c, "bits_calibrated"))[0])
         for w in widths for c in chunks if cells(w, c, "bits_calibrated")),
        key=lambda t: t[2], default=None)
    if best is None:
        print("  no usable cell")
        return 1
    width, chunk, bits = best
    print(f"  best cell: width {width} cap {chunk} at {bits:.3f} bits/char")
    if bits < bars["bigram"]:
        print("  -> IT BEATS THE BIGRAM. Goal 2 has its first positive evidence")
        print("     and this is the most important result in the project")
    elif bits < bars["unigram"]:
        print("  -> beats the unigram and not the bigram: base rates learned,")
        print("     transitions not. A specific diagnosis, and a fixable one")
    elif bits < bars["uniform"]:
        print("  -> beats uniform ONLY. It has not learned the base rate of")
        print("     English characters, which is a different and worse failure")
        print("     than failing to learn transitions")
    else:
        print("  -> below uniform. Something is wrong with the setup, not with")
        print("     the model: uniform is what knowing nothing costs")

    spread = [mean_and_error(cells(width, c, "bits_calibrated"))[0]
              for c in chunks if cells(width, c, "bits_calibrated")]
    if len(spread) > 1:
        moved = max(spread) - min(spread)
        print(f"\n  the cap moves the best width by {moved:.3f} bits")
        if moved < 0.05:
            print("  -> the cap's VALUE does not matter once it binds at all,")
            print("     so it is a stability requirement rather than a dial")
        else:
            print("  -> the cap is a real dial and not just a safety rail; it")
            print("     is a frozen axis anywhere else it is set")

    by_width = [mean_and_error(
        [v for c in chunks for v in cells(w, c, "bits_calibrated")])[0]
        for w in widths
        if any(cells(w, c, "bits_calibrated") for c in chunks)]
    if len(by_width) > 1:
        print(f"\n  WIDTH moves it by {max(by_width) - min(by_width):.3f} bits, "
              f"from {max(by_width):.3f} to {min(by_width):.3f}")
        print("  -> this is the axis that decides whether g10-02's underfitting")
        print("     is structural or width-limited. Little movement across an")
        print("     fourfold width means the ceiling is not capacity")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
