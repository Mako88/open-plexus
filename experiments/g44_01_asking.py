"""g44-01: can a system that ASKS separate a confound one that WATCHES cannot?

**Predictions registered before the instrument existed**, recovered from git
after the restructure deleted the file that held them. They are binding and they
are scored mechanically at the bottom of this run.

## The boundary this is about

Everything else here WATCHES. Moments go past, things turn up together, and what
keeps turning up together becomes one thing. That works until something is
present for a reason of its own: a lamp switched on whenever this dog is in the
room co-occurs with the dog exactly as strongly as the dog's bark does.
`g39-06` measured the collapse — a merely-common distractor is refused at 0.4490
and a genuinely correlated one at **0.0096**.

That is not a flaw in one statistic. **A stream of observations contains nothing
that separates "always there when" from "part of"**, so every counting method
reads the same numbers.

## The escape, and which way round the question goes

Ask for the lamp WITHOUT the dog. If the world can produce it, the lamp was never
part of the dog.

**The other direction does not work here and getting it backwards would invert
the result.** Asking for the dog without the lamp fails always, because the lamp
is present whenever the dog is — while asking for the dog without its own BARK
often succeeds, because a true surface appears only `presence` of the time. That
test would mark the confound as the most constitutive thing in the world.

So an ask is `world.ask(present=candidate, absent=surface_of_the_concept)`, and a
REFUSAL means the candidate could not be had without the concept, which is what a
part looks like. A candidate that comes back alone is not one.

    refused often   -> cannot be detached -> leave its score alone
    complied often  -> it is its own thing -> demote it

An unasked pair keeps its score, so an arm can only ever demote what it paid to
test, and no arm gets credit for a question it did not ask.

## The arms

    watch         the observer, on the same budget. THE FLOOR, and the thing
                  that must be beaten for the direction to survive
    ask-random    the same budget spent on RANDOM pairs. **The control that
                  decides whether ASKING helps or whether CHOOSING WHAT TO ASK
                  helps** -- without it, "intervention works" cannot be told
                  from "a differently sampled stream works"
    ask-targeted  asks about its own current best-scoring partner, which is
                  where a confound hides by construction

Every arm spends the same number of occasions, watched or asked, so **no arm
sees more of the world than another** -- otherwise this measures sample size.

## A fourth arm, and its predictions are registered here BEFORE it is written

`ask-targeted` failed for a reason worth building on: the background surfaces are
present in EVERY occasion, so `conditional(background | anything)` is 1.0, the
largest the statistic can take, and asking about the best-scoring partner asks
about the background on every draw. It lands 1 of the 108 pairs the metric
reads. That is argmax-on-association finding the most ubiquitous thing -- the
confound failure, happening to the confound detector.

The asymmetry it missed: the background predicts nothing in REVERSE. It is
present whenever the concept is AND whenever it is not, so `P(query|background)`
is small, while a shadow appears with its own concept and rarely otherwise, so it
predicts that concept well. **Mutual predictability separates them, and it uses
nothing the arm is not allowed to know** -- no arm may be told which surfaces are
concepts.

    ask-mutual    asks about the partner maximising min(P(c|q), P(q|c))

    P5  it lands >30 of the 108 scored pairs on target at budget 0.10,
        against ask-targeted's 1
    P6  and beats watching by >0.05, which no arm has done
    P7  and >40% of its asks are shadow pairs, which is the reason if it works

P5 is the one that matters. P6 without P5 would mean it helped for a reason this
explanation does not name, and P5 without P6 kills the direction properly: the
right questions asked, and the confound still ahead.

## A comparative demotion, registered before it is written

P5 and P7 held and P6 did not, and the ceiling split says the fault is in
`adjusted` rather than in any policy: allowed to demote shadows alone it reaches
+0.2042 against watching's -0.2967, and allowed to demote true partners alone,
-0.5509. A raw refusal rate is being read as an absolute when a true surface at
`presence` 0.7 is genuinely detachable most of the time.

So compare a candidate against the OTHER candidates asked about for the same
query -- which is legitimate, since nothing tells the arm which surfaces are
concepts -- and demote only what is detached more easily than its neighbours:

    factor = min(1.0, refusal_rate / mean refusal_rate for that query)

    P8  ask-mutual with this demotion beats watching by >0.05 at budget 0.10,
        which no arm has done
    P9  and at shadow_alone 0.0 it does NOT beat its own watch by >0.02, since
        there the shadow is a part and there is nothing to find

P9 is the one that can embarrass this. A rule that always improves separation
improves it in the world where the confound is constitutive too, and that would
make P8 a property of the arithmetic rather than a finding about asking.

    python experiments/g44_01_asking.py --json out/g44-01.json
"""

from __future__ import annotations

import argparse
import pathlib
import random
import sys
import time

ROOT = pathlib.Path(__file__).resolve().parents[1]
if str(ROOT) not in sys.path:
    sys.path.insert(0, str(ROOT))

from experiments import harness  # noqa: E402
from openplexus.grounding import STATISTICS, CoOccurrence  # noqa: E402
from openplexus.tasks.asking import World  # noqa: E402
from openplexus.tasks.occasions import OccasionConfig  # noqa: E402

#: Budgets to sweep, as a fraction of the stream spent on asks. **The axis P3 is
#: about**: at a budget where refusals are near zero the advantage should be near
#: zero, and if it is not, whatever helps is not the refusal. Swept, and 0.10 is
#: the cell P1 and P2 name, so it is in the grid because the registered
#: prediction put it there rather than because it was chosen here.
BUDGETS = (0.0, 0.05, 0.10, 0.25)

#: Seeds. Three is this project's floor and is chosen here as that floor.
SEEDS = (0, 1, 2)

#: The statistic. `conditional` is the one measured to refuse an ever-present
#: distractor (g39-04), so the confound this run is about is the one it cannot.
STATISTIC = "conditional"

#: How independent a shadow is. **0.30 is chosen here** as clearly detectable by
#: asking while leaving it present on every occasion its concept is, so counting
#: still cannot see it. 0.0 is run as the control: a shadow that can never be
#: had alone is constitutive by construction and NO arm should separate it.
ALONE = 0.30


def world_config(seed: int, alone: float) -> OccasionConfig:
    return OccasionConfig(concepts=12, surfaces=3, presence=0.7, noise=2,
                          distractors=1, shadows=12, shadow_alone=alone,
                          occasions=4000, seed=seed)


def separation(index: CoOccurrence, config: OccasionConfig, statistic,
               refusals: dict) -> float:
    """`g39-06`'s quantity: weakest TRUE partner minus the confound.

    Positive means every real surface of the concept outranks the thing that
    merely follows it around. Averaged over concepts, so one lucky concept
    cannot carry the arm.
    """
    scores = []
    for concept in range(config.concepts):
        own = [concept * config.surfaces + m for m in range(config.surfaces)]
        shadow = config.shadow_of(concept)
        for query in own:
            partners = [s for s in own if s != query]
            if not partners:
                continue
            weakest = min(adjusted(index, statistic, p, query, refusals)
                          for p in partners)
            confound = adjusted(index, statistic, shadow, query, refusals)
            scores.append(weakest - confound)
    return sum(scores) / len(scores) if scores else 0.0


def adjusted(index: CoOccurrence, statistic, candidate: int, query: int,
             refusals: dict) -> float:
    """The counted score, demoted by how easily the candidate came alone.

    An unasked pair is unchanged: an arm may only demote what it paid to test.

    **THIS RULE IS THE THING THAT IS WRONG, and the ceiling split says so.**
    Allowed to demote only shadows it reaches +0.2042, beating the confound
    outright from watching's -0.2967; allowed to demote only true partners,
    -0.5509. Both together is -0.0500, which is the win and the damage very
    nearly cancelling and which is why the full ceiling looked mediocre.

    The reason is that a refusal RATE is read here as an absolute. A true
    surface at `presence` 0.7 is genuinely detachable most of the time -- it is
    refused 0.3837 against a shadow's 0.2222 -- so being detachable is not the
    same as not being a part, and only the COMPARISON between candidates
    carries the signal. Multiplying by the raw rate spends that signal on a
    quantity it does not measure.

    The control holds the reading: at `shadow_alone` 0.0 the same shadows-only
    demotion reaches -0.0135, not +0.2042, so this fires when there is a
    detachable confound and not otherwise.
    """
    score = statistic(index, candidate, query)
    asked, refused = refusals.get((candidate, query), (0, 0))
    if not asked:
        return score
    return score * (refused / asked)


def run_arm(arm: str, config: OccasionConfig, budget: float, statistic,
            rng: random.Random) -> dict:
    """One arm on one world. Every arm spends `config.occasions` draws."""
    world = World(config)
    index = CoOccurrence()
    refusals: dict = {}
    asks = int(config.occasions * budget) if arm != "watch" else 0
    seen: list[int] = []
    shadow_asks = 0

    while world.drawn < config.occasions:
        spend_on_ask = arm != "watch" and asks > 0 and len(seen) > 4
        if not spend_on_ask:
            occasion = world.watch()
            index.observe(occasion.surfaces)
            seen.extend(occasion.surfaces)
            continue

        query = rng.choice(seen)
        if arm == "ask-random":
            candidate = rng.randrange(config.vocabulary)
        else:
            partners = index.partners(query)
            if not partners:
                candidate = rng.randrange(config.vocabulary)
            elif arm == "ask-mutual":
                # ASK ABOUT WHAT PREDICTS THIS AND IS PREDICTED BY IT. A surface
                # present in every occasion scores 1.0 one way and nearly
                # nothing the other, so the minimum of the two directions is
                # what the background cannot fake.
                candidate = max(partners, key=lambda p: min(
                    statistic(index, p, query), statistic(index, query, p)))
            else:
                # THE POLICY: ask about the partner that currently looks most
                # like part of this, which is where a confound hides. It is also
                # where the BACKGROUND is, and that is why it lands 1 of 108.
                candidate = max(partners,
                                key=lambda p: statistic(index, p, query))
        if candidate == query:
            continue
        answer = world.ask(present=candidate, absent=query)
        asks -= 1
        if answer.occasion is not None:
            index.observe(answer.occasion.surfaces)
            seen.extend(answer.occasion.surfaces)
            was, refused = refusals.get((candidate, query), (0, 0))
            refusals[(candidate, query)] = (was + 1, refused + answer.refused)
            shadow_asks += config.is_shadow(candidate)

    tallies = list(refusals.values())
    wanted = scored_pairs(config)
    return {
        "arm": arm, "budget": budget,
        "separation": separation(index, config, statistic, refusals),
        "refusal_rate": (sum(r for _, r in tallies) / sum(a for a, _ in tallies)
                         if tallies else 0.0),
        "pairs_tested": len(refusals),
        # OF THE PAIRS IT PAID TO TEST, HOW MANY DOES THE METRIC READ? A count
        # of pairs asked says nothing without this: two arms can both test 60
        # pairs while one of them tests 60 pairs nobody scores.
        "on_target": len(set(refusals) & wanted),
        "scored": len(wanted),
        # P7: is it asking about SHADOWS? If an arm wins without this, it won
        # for a reason the explanation does not name.
        "shadow_share": (shadow_asks / (config.occasions * budget)
                         if budget and arm != "watch" else 0.0),
        "drawn": world.drawn,
    }


def scored_pairs(config: OccasionConfig) -> set:
    """Exactly the (candidate, query) pairs `separation` reads.

    Every other pair an arm asks about is spend that cannot move the number,
    however sensible the question was.
    """
    pairs = set()
    for concept in range(config.concepts):
        own = [concept * config.surfaces + m for m in range(config.surfaces)]
        for query in own:
            for candidate in [s for s in own if s != query]:
                pairs.add((candidate, query))
            pairs.add((config.shadow_of(concept), query))
    return pairs


def discrimination(config: OccasionConfig, per_pair: int = 40) -> dict:
    """Do shadows and true partners get DIFFERENT refusal rates, or the same?

    **The check that says whether the ceiling means anything.** Separation is a
    DIFFERENCE of scores and the demotion MULTIPLIES them, so if everything is
    scaled by the same factor the difference shrinks toward zero while
    discriminating nothing at all -- and a ceiling that improved for that reason
    would be an artefact of arithmetic.

    It is not. Measured at `shadow_alone` 0.30: true partners are refused 0.3837
    of the time and shadows 0.2222, so the confound is demoted harder. And the
    control inverts it: at 0.0 the shadow cannot be had without its concept at
    all, is refused 0.7326 against a true partner's 0.3917, and is correctly
    treated as the most constitutive thing present.

    That is the causal claim behaving as stated in both directions, which is the
    strongest evidence in this run and is separate from any policy finding one.
    """
    world = World(config)
    tallies = {"true": [0, 0], "shadow": [0, 0]}
    for concept in range(config.concepts):
        own = [concept * config.surfaces + m for m in range(config.surfaces)]
        shadow = config.shadow_of(concept)
        for query in own:
            for kind, candidates in (("true", [s for s in own if s != query]),
                                     ("shadow", [shadow])):
                for candidate in candidates:
                    for _ in range(per_pair):
                        answer = world.ask(present=candidate, absent=query)
                        tallies[kind][0] += 1
                        tallies[kind][1] += answer.refused
    rates = {k: (r / a if a else 0.0) for k, (a, r) in tallies.items()}
    return {"arm": "discrimination", "true_refusal": rates["true"],
            "shadow_refusal": rates["shadow"],
            "discrimination": rates["true"] - rates["shadow"],
            "alone": config.shadow_alone}


def ceiling(config: OccasionConfig, statistic, rng: random.Random,
            per_pair: int = 12, restrict: str = "") -> dict:
    """What asking could do if it asked about EVERY pair the metric scores.

    **Not an arm.** It spends whatever it needs and no policy could afford it.
    It exists because a refuted prediction has two possible causes and they need
    telling apart: the mechanism cannot separate the confound, or the POLICY
    never asked about the pairs the metric reads.

    It is the second, and by a margin no guess would have reached. `ask-targeted`
    tests 60 distinct pairs against the 108 that separation scores, and the two
    counts being the same size is a coincidence: the OVERLAP is 1. The
    `on target` column carries that number now, because a count of pairs tested
    is unreadable without it.

    If this is positive, intervention works and the policy is the problem. If it
    is negative, the direction is refuted and no policy saves it -- which is
    exactly what g44-01 was registered to find out.
    """
    world = World(config)
    index = CoOccurrence()
    refusals: dict = {}
    for _ in range(config.occasions):
        index.observe(world.watch().surfaces)

    for concept in range(config.concepts):
        own = [concept * config.surfaces + m for m in range(config.surfaces)]
        shadow = config.shadow_of(concept)
        for query in own:
            for candidate in [s for s in own if s != query] + [shadow]:
                for _ in range(per_pair):
                    answer = world.ask(present=candidate, absent=query)
                    was, refused = refusals.get((candidate, query), (0, 0))
                    refusals[(candidate, query)] = (was + 1,
                                                    refused + answer.refused)
    if restrict == "shadows":
        refusals = {k: v for k, v in refusals.items() if config.is_shadow(k[0])}
    elif restrict == "true":
        refusals = {k: v for k, v in refusals.items()
                    if not config.is_shadow(k[0])}
    tallies = list(refusals.values())
    return {
        "arm": f"ceiling ({restrict})" if restrict else "ceiling (not an arm)",
        "separation": separation(index, config, statistic, refusals),
        "refusal_rate": (sum(r for _, r in tallies) / sum(a for a, _ in tallies)
                         if tallies else 0.0),
        "pairs_tested": len(refusals),
        "drawn": world.drawn,
    }


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--json", type=pathlib.Path, default=None)
    # Chosen here as the control described above; 0.0 makes the confound
    # constitutive by construction and no arm should separate it.
    parser.add_argument("--alone", type=float, default=ALONE)
    args = parser.parse_args()

    started = time.time()
    statistic = STATISTICS[STATISTIC]
    print(f"g44-01  shadow_alone {args.alone}, statistic {STATISTIC}, "
          f"{len(SEEDS)} seeds")
    print(f"{'arm':<14}{'budget':>8}{'separation':>13}{'refusals':>11}"
          f"{'pairs':>8}{'on target':>11}{'shadow':>8}{'drawn':>8}")
    print("-" * 62)

    rows: list[dict] = []
    summary: dict = {}
    for budget in BUDGETS:
        for arm in ("watch", "ask-random", "ask-targeted", "ask-mutual"):
            if arm == "watch" and budget != BUDGETS[0]:
                continue
            got = []
            for seed in SEEDS:
                config = world_config(seed, args.alone)
                got.append(run_arm(arm, config, budget, statistic,
                                   random.Random(1000 + seed)))
            mean = lambda key: sum(g[key] for g in got) / len(got)  # noqa: E731
            row = {"arm": arm, "budget": budget, "alone": args.alone,
                   "separation": mean("separation"),
                   "refusal_rate": mean("refusal_rate"),
                   "pairs_tested": mean("pairs_tested"),
                   "on_target": mean("on_target"),
                   "scored": mean("scored"),
                   "shadow_share": mean("shadow_share"),
                   "drawn": mean("drawn")}
            rows.append(row)
            summary[(arm, budget)] = row["separation"]
            hit = f"{row['on_target']:.0f}/{row['scored']:.0f}"
            print(f"{arm:<14}{budget:>8}{row['separation']:>13.4f}"
                  f"{row['refusal_rate']:>11.4f}{row['pairs_tested']:>8.0f}"
                  f"{hit:>11}{row['shadow_share']:>8.1%}"
                  f"{row['drawn']:>8.0f}")

    # THE CEILING, before any prediction is read. A refuted prediction has two
    # causes and this is what tells them apart.
    tops = [ceiling(world_config(seed, args.alone), statistic,
                    random.Random(seed)) for seed in SEEDS]
    top = sum(t["separation"] for t in tops) / len(tops)
    rate = sum(t["refusal_rate"] for t in tops) / len(tops)
    pairs = sum(t["pairs_tested"] for t in tops) / len(tops)
    rows.append({"arm": "ceiling", "separation": top, "refusal_rate": rate,
                 "pairs_tested": pairs})
    print(f"{'ceiling*':<14}{'-':>8}{top:>13.4f}{rate:>11.4f}{pairs:>8.0f}"
          f"{'-':>8}")
    print("  * not an arm: asks about every pair the metric scores, at any "
          "cost. It says whether the MECHANISM can separate the confound, "
          "separately from whether a POLICY found it.")

    # DOES REFUSAL DISCRIMINATE, or does it shrink everything equally? A
    # difference of scores under a multiplicative demotion moves toward zero
    # for free, so the ceiling means nothing until this is read.
    split = discrimination(world_config(SEEDS[0], args.alone))
    control = discrimination(world_config(SEEDS[0], 0.0))
    rows.extend([split, control | {"arm": "discrimination (control)"}])
    print(f"\nDoes refusal DISCRIMINATE, or just shrink everything?")
    for label, got in (("confound", split), ("control, alone=0.0", control)):
        print(f"  {label:<20} true {got['true_refusal']:.4f}  shadow "
              f"{got['shadow_refusal']:.4f}  difference "
              f"{got['discrimination']:+.4f}")
    print("  A positive difference demotes the confound harder than a real "
          "partner. The control must be NEGATIVE: a shadow that cannot be had "
          "alone IS constitutive, and asking should say so.")

    # WHICH HALF OF THE DEMOTION DOES THE WORK? Asking everything reaches
    # -0.0500 and asking 53 of 108 well-chosen pairs reaches -0.5130, so a
    # SUBSET is worse than none. This splits the ceiling to say why.
    print("\nThe ceiling, split by what it is allowed to demote:")
    for restrict, label in (("shadows", "shadows only"), ("true", "true only")):
        halves = [ceiling(world_config(seed, args.alone), statistic,
                          random.Random(seed), restrict=restrict)
                  for seed in SEEDS]
        got = sum(h["separation"] for h in halves) / len(halves)
        rows.append({"arm": f"ceiling ({restrict})", "separation": got})
        print(f"  {label:<16}{got:>10.4f}")
    print("  Demoting a true partner lowers a MIN and demoting the shadow "
          "lowers one term, so partial coverage is not partial credit.")
    # THE CONTROL, and without it "demote the shadow" is true by construction.
    # At alone=0.0 the shadow IS constitutive, so demoting shadows must NOT
    # rescue separation -- if it does, this is arithmetic and not evidence.
    guard = [ceiling(world_config(seed, 0.0), statistic, random.Random(seed),
                     restrict="shadows") for seed in SEEDS]
    held = sum(g["separation"] for g in guard) / len(guard)
    rows.append({"arm": "ceiling (shadows, alone=0.0)", "separation": held})
    print(f"  control, alone=0.0, shadows only  {held:>10.4f}  <- must stay "
          f"low: a shadow that cannot be had alone is a PART, and demoting it "
          f"is the error this whole run is trying not to make")

    floor = summary[("watch", BUDGETS[0])]
    print(f"\nPREDICTIONS, registered before this file existed:")
    verdicts = []

    at_ten = summary.get(("ask-targeted", 0.10), 0.0)
    p1 = at_ten - floor > 0.05
    verdicts.append(("P1", "ask-targeted beats watch by >0.05 at budget 0.10",
                     p1, f"{at_ten - floor:+.4f}"))

    random_ten = summary.get(("ask-random", 0.10), 0.0)
    p2 = at_ten - random_ten > 0.02
    verdicts.append(("P2", "and it is the TARGETING: beats ask-random by >0.02",
                     p2, f"{at_ten - random_ten:+.4f}"))

    low = summary.get(("ask-targeted", 0.05), 0.0) - floor
    p3 = (at_ten - floor) >= low
    verdicts.append(("P3", "the advantage grows with the budget that buys "
                     "refusals", p3, f"{low:+.4f} -> {at_ten - floor:+.4f}"))

    coverage = {(r["arm"], r["budget"]): r for r in rows if "on_target" in r}
    mutual = coverage.get(("ask-mutual", 0.10), {})
    landed = mutual.get("on_target", 0.0)
    verdicts.append(("P5", "ask-mutual lands >30 of 108 scored pairs on target",
                     landed > 30, f"{landed:.0f}/108"))

    mutual_ten = summary.get(("ask-mutual", 0.10), 0.0)
    verdicts.append(("P6", "and beats watching by >0.05",
                     mutual_ten - floor > 0.05, f"{mutual_ten - floor:+.4f}"))

    share = mutual.get("shadow_share", 0.0)
    verdicts.append(("P7", "and >40% of its asks are shadow pairs",
                     share > 0.40, f"{share:.1%}"))

    for name, claim, held, detail in verdicts:
        print(f"  {name} {'HELD ' if held else 'REFUTED'}  {claim}  [{detail}]")
        rows.append({"arm": "prediction", "name": name, "claim": claim,
                     "held": held, "detail": detail})

    print("\nP4 needs the merely-common distractor and is not scored here: this "
          "world's distractor is refused by counting alone, so it is a separate "
          "run rather than a column.")
    harness.emit(args.json, rows, started=started, budgets=list(BUDGETS),
                 seeds=list(SEEDS), statistic=STATISTIC, alone=args.alone)
    print(f"COST: {time.time() - started:.1f}s wall, one process")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
