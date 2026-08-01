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

## What happened, and what is actually left

P8 REFUTED at -0.1264 and P9 HELD. Comparing locally is better than reading a
rate as an absolute (-0.5130 to -0.4231 for the arm) and it is WORSE than the
raw rule at full coverage, -0.1425 against -0.0500, so it is not the fix.

The obvious explanation was estimation noise -- twelve asks resolve a rate to
about +-0.13 against a 0.16 signal -- and sweeping it refutes that too:

    asks per pair       raw   comparative   shadows only
            12      -0.0500      -0.1425       +0.2042
            48      -0.0070      -0.0580       +0.2343
           192      -0.0104      -0.0784       +0.2246

Sixteen times the asks moves nothing across zero for EITHER OF THOSE TWO RULES,
and `shadows only` sits near +0.22 throughout.

**"The shortfall is structural and not sampling" is how I first wrote that, and
it was too general.** It holds for a rule that multiplies a score by a rate,
where a noisier rate is still an unbiased multiplier. A CLASSIFIER has a
completely different sensitivity -- it is asking which side of a boundary a
value falls, and noise flips that. The threshold rule below is unusable at 12
asks per pair and exact at 384, so the sweep had to be re-run per rule rather
than concluded once.

## A threshold, registered before it is written

Every rule tried so far is CONTINUOUS -- it multiplies a score by a rate, so
every asked candidate is demoted by something. Measuring the per-pair rates at
192 asks says the structure is not continuous at all:

    refusal rate per pair, shadow_alone 0.30
    true partner   n=216  min 0.292  median 0.375  max 0.474
    shadow         n=108  min 0.135  median 0.214  max 0.292

**Zero of 216 true partners fall below the highest shadow rate.** A threshold at
0.287 classifies 323 of 324 pairs, against 66.7% for calling everything a part.
So the oracle is reachable and a continuous rule was the wrong shape.

The threshold must be LEARNED, not passed in -- a two-means split over the rates
the arm actually observed, which needs no privileged knowledge.

    P10  a learned threshold, demoting only the low group, makes ask-mutual
         beat watching by >0.05 at budget 0.10
    P11  and at shadow_alone 0.0 it does NOT beat its own watch by >0.02
    P12  the rule has a NOTHING-TO-FIND state: at shadow_alone 0.0 it demotes
         true partners on under 20% of queries

### What happened: P10 refuted, P11 held, P12 refuted at 100%

**And the rule is nonetheless right.** Swept against asks per pair, against an
oracle that calls `is_shadow` and reaches +0.2042:

    asks per pair   threshold      raw   oracle
               12     -0.0802  -0.0500  +0.2042
               48     +0.1314  -0.0070  +0.2343
              192     +0.2170  -0.0104  +0.2246
              384     +0.2256  -0.0050  +0.2256

**At 384 the learned threshold MATCHES THE ORACLE to four decimals**, using
nothing but rates the arm paid for. So a legitimate rule does exist, and P10
failed on allocation: at budget 0.10 the arm spends about 400 asks over 53-odd
pairs, roughly 7 each, where the rule needs 48 before it beats doing nothing.

**The constraint has moved from "what to ask" to "how often to ask it."** Every
arm here nominates a fresh pair each draw, which is the worst possible spend for
a rule that needs a resolved rate per pair.

**P12 is the one I expect to fail, and it is registered because of that.** A
two-means split always returns two groups, so at 0.0 -- where the shadow is a
part and refuses 0.7326 against a true partner's 0.3917 -- the more detachable
group is the TRUE PARTNERS, and a rule that must always demote somebody will
demote them. A confound detector with no way to report "nothing here" is a
different kind of broken from one that reports the wrong thing.

## The open problem before the threshold was tried

It is not about budget, policy, or noise:
`shadows only` is an ORACLE -- it calls `is_shadow`, which no arm may do. Every
legitimate rule tried so far demotes true partners often enough to pay back the
whole win, and `separation` takes a MIN over true partners, so one wrongly
demoted part costs the entire query. **What is missing is a legitimate rule that
approximates the oracle**, and nothing here has found one.

## The price of asking, registered before it is measured

P10 failed on allocation: about 7 asks per pair where the rule needs 48. The
obvious repair is an arm that REVISITS -- nominate as `ask-mutual` does, then
stay on that pair until it has a resolved rate.

The arithmetic is uncomfortable before anything is run. 108 pairs at 48 asks is
5,184 asks against a stream of 4,000 occasions, so a rule that works may still
cost more than watching the world at all.

    ask-repeat    nominates by mutual predictability, then re-asks the same
                  pair REVISITS times before moving on

    P13  ask-repeat beats watching by >0.05 at some budget at or under 2.0
    P14  and the smallest budget that does it is ABOVE 0.5, so asking is not
         cheap and the earlier grid could not have found it
    P15  at equal budget ask-repeat beats ask-mutual, since the only difference
         is how the same number of asks is spread

P14 is the one worth being wrong about. If a budget under 0.5 does it, the
grid was simply too coarse and nothing about cost follows; if none under 2.0
does, intervention works and does not pay for itself here, which is a result
about the mechanism and not about this implementation.

### What happened: P15 HELD, P13 REFUTED, P14 NOT MEASURABLE

**P15 held.** At equal budget, concentrating asks beats spreading them:
ask-repeat -0.3356 against ask-mutual's -0.4708. The only difference is how the
same asks are spread, so that is the allocation effect on its own.

**P14 cannot be measured and saying so is the finding.** Budget stops binding
above about 0.25: an ask consumes a draw, the run stops at `occasions` draws,
and at budgets 0.25, 1.0 and 2.0 the arm reaches 14 pairs and 4,001 draws every
time. The equal-exposure invariant caps asking with the same ceiling it caps
watching, so a budget axis cannot price this. Recorded rather than reported as
a refutation, which is what the identical rows would have looked like.

**P13 refuted, and lengthening the stream makes it worse, not better:**

    occasions   ask-repeat    watch   on target
         4000      -0.3283  -0.2947           5
        12000      -0.4061  -0.2918          22
        30000      -0.5138  -0.2888          52

Both arms get the same stream, so this is not exposure. **More coverage scores
worse**, which is the signature of a demotion landing on true partners.

### Why, and it is the mutation's failure mode in mirror image

The arm's learned cut is **0.6278**. The oracle boundary is **0.2870**.

8 of the 14 pairs it learns from are pairs the metric never scores, and their
median refusal rate is **0.6667** -- far HIGHER than any scored pair, because a
background surface asked about the wrong query is hard to detach rather than
easy. So the two-means split separates scored from UNSCORED, lands above
everything the metric reads, and demotes every scored pair including all true
partners.

`an-unasked-pair-votes-on-the-boundary` guards the same failure with the sign
reversed: it drags the cut down until nothing is demoted. Both are the threshold
being learned from a population it will not be applied to.

**So the next change is where the cut is learned, not how.** A threshold fitted
per query, over the candidates for that query alone, uses no privileged
knowledge and cannot be polluted by pairs the demotion never touches. Nothing
here has run it.

## WHAT P12 WOULD TAKE, measured rather than argued

P12 is refuted at 100% and the useful question is what a NOTHING-TO-FIND state
would need. Splitting the scored pairs at 96 asks each, with and without a real
confound:

    alone    cut     low mean   high mean   low group is
     0.30  0.2969     0.2105      0.3833    36/38 shadows
     0.00  0.5540     0.3779      0.7300    0/72 shadows

**The split itself is nearly perfect where there is something to find** -- every
one of the 36 shadows, and 2 false positives. Where there is not, it demotes all
72 true partners, because at 0.0 the true partners genuinely ARE the more
detachable group and the rule is reporting that correctly.

**So the two cases are distinguishable, and not in the obvious way.** The signal
is the low group's ABSOLUTE level, 0.2105 against 0.3779. The scale-free
candidate -- the ratio between the groups -- does not work at all: 0.55 against
0.52, which is nothing.

An absolute cutoff would be a tuned constant unless it is anchored to something
the arm can compute, and no such anchor is known here. **That is the whole of
what is missing**, and it is smaller than "the rule has no nothing-to-find
state" made it sound.

## DOES ASKING CORRUPT THE COUNTS? P16 AND P17, REGISTERED FIRST

An arm covering 25 pairs scores -0.3067 where the coverage curve says 27 pairs
is -0.1846, and the curve builds its index from unconditioned watches while an
arm feeds every ask-occasion into the same index. An ask-occasion is drawn
CONDITIONED on the candidate being present, so it is not a sample of the world:
every candidate an arm asks about is over-represented in the counts that arm
then scores.

`learn_from_asks=False` keeps the intervention and throws away the occasion it
produced. Refusals are still recorded and draws are still charged, so the arm
pays exactly what it paid before and only stops LEARNING from a biased sample.

    P16  an arm that does not learn from its own asks beats one that does,
         by >0.05 at matched budget
    P17  and it beats watching, which no arm has done

P16 without P17 would mean the bias is real and something else is also wrong.
Neither holding would refute the hypothesis outright and send the 0.12 back to
being unexplained, which is where it is now.

### P18 and P19, registered before the arm that can test this exists

Every arm here asks CONTINUOUSLY once it can: `spend_on_ask` needs only that the
ask allowance is unspent, so the arm stops watching the moment it starts asking.
That is why the budget axis stopped binding above 0.25, and it is why turning
learning off froze the index at one observation.

`interleave=True` asks only while the share of draws spent asking is under
`budget`, and watches otherwise, so the two are mixed through the run rather
than run end to end. **Default off**, so every earlier number stands.

    P18  interleaved, an arm that does NOT learn from its asks beats one that
         does, by >0.05 at matched budget -- the poisoning hypothesis, finally
         asked in a form that can answer it
    P19  and both interleaved arms keep an index within 20% of what pure
         watching accumulates, which is what makes P18 readable at all

**P19 is the guard P16 lacked.** If the index is starved again, P18's number is
vacuity for the same reason the last one was, and a run that cannot show P19 is
not evidence about P18 whichever way it comes out.

### What happened: P19 HELD, P18 REFUTED, and the poisoning idea is dead

    arm          budget  learns  per query  observed  on target
    watch           0.0    True    -0.2967      4000          0
    ask-repeat     0.10    True    -0.2954      4000          3
    ask-repeat     0.10   False    -0.2975      3600          4
    ask-mutual     0.10    True    -0.4099      4000         52
    ask-mutual     0.10   False    -0.4253      3600         54

P19 held: 3600 against 4000, so no index was starved and the numbers mean
something this time. **P18 refuted.** Not learning from its own asks is worth
-0.0021 to ask-repeat and -0.0154 to ask-mutual, which is to say nothing and
slightly negative. **Asking does not poison the counts.**

### And the 0.33 discrepancy is RESOLUTION, not coverage

`ask-mutual` lands 52 scored pairs and reaches -0.4099 where the coverage curve
puts 54 pairs at -0.0844. With poisoning dead, what is left is that the curve
resolves every covered pair at 96 asks and the arm at budget 0.10 spreads about
400 asks over 52 pairs -- roughly SEVEN each.

That is consistent with the asks-per-pair sweep already here: the threshold rule
is unusable at 12 asks per pair (-0.0802 at full coverage) and exact at 384. At
seven it is worse than unusable, and a misclassified pair does not merely fail
to help, it demotes a true partner.

**So the constraint is the PRODUCT, pairs times asks-per-pair, and the budget
bounds it.** An arm can have 52 pairs at 7 asks or 8 pairs at 48, and the curve
that reaches +0.19 needed 108 at 96, which is 10,368 asks against a stream of
4,000. Every separate explanation tried here -- policy, budget, noise, pricing,
metric, coverage, poisoning -- has been a face of that one number.

### P16 is an ARTEFACT and is withdrawn

    arm          budget   learns   per query   on target
    watch           0.0     True     -0.2967           0
    ask-repeat     0.25     True     -0.3189           6
    ask-repeat     0.25    False     -0.0741           1
    ask-mutual     0.10     True     -0.3297          46
    ask-mutual     0.10    False     -0.3171           1

Not learning from its own asks is worth +0.245 to ask-repeat. P17 still fails --
-0.0741 does not beat watching -- but the size of that is far past anything
registered.

**The obvious artefact is refuted.** Discarding ask-occasions leaves the index
with less data, and less data could shrink a difference toward zero for free.
Watching on a truncated stream says otherwise: 4000 observations -0.2967, 400
-0.3124, 100 -0.3496, 50 -0.3562. A sparser index scores WORSE, so the gain is
not sparsity.

**The blind arm lands ONE scored pair, and one demoted pair cannot move an
average over 36 queries by 0.245.** That is what made it worth chasing rather
than reporting, and the chase found it:

    observations   separation
             400     -0.3124
              50     -0.3562
               8     -0.2315
               2     -0.1019
               0     +0.0000

**Separation goes to ZERO as the index empties**, because an empty index scores
everything at zero and the difference of two zeros is zero. The blind arm's
index observes exactly ONE occasion, on every seed.

The cause is a pre-existing property of the arm that the flag exposed:
`spend_on_ask` requires `len(seen) > 4`, and one watched occasion already
contains more than four surfaces. With learning ON, ask-occasions kept feeding
the index, so it never mattered. With learning OFF, the arm watches once, crosses
the threshold, and asks for the rest of the stream while its counts stay frozen
at one observation.

So -0.0741 is not the confound being separated. It is the metric reading nothing
at all, and it happens to look like a large win because vacuity sits at zero and
every real score here is negative. **P16 is withdrawn.**

**The hypothesis it was meant to test is still untested.** Testing it needs an
arm that keeps watching while it asks -- the flag as written cannot separate
"does not learn from asks" from "does not learn". That is a change to the arm's
schedule and nothing here has made it.

## THE COVERAGE CURVE REFUTES "IT NEEDS NEAR-TOTAL COVERAGE"

That claim was inferred from three points -- 6, 25 and 108 -- and never
measured. Measured, with each covered pair resolved at 96 asks so this is
coverage and not noise:

    covered   per query   threshold
          0     -0.2967     -0.2967
         12     -0.2850     -0.2473
         27     -0.2358     -0.1846
         54     -0.0844     -0.0142
         81     +0.0533     +0.0899
        108     +0.1889     +0.1940

**Smooth and monotonic, with no cliff.** Twelve pairs of 108 already beat
watching by 0.05, and it crosses zero near 60. Coverage buys separation in
proportion, which is the opposite of what "near-total" claimed.

### And that opens the real question

**An arm covering 25 pairs scores -0.3067 where this curve says 27 pairs is
-0.1846.** Same rule, same world, similar coverage, and 0.12 apart. The arms are
losing something that is not coverage, not budget, not the metric and not the
sampler, because each of those is now measured.

One candidate, and it is a HYPOTHESIS with no measurement behind it yet: this
curve builds its index from `occasions` unconditioned watches, while an arm
feeds every ask-occasion into the same index -- and an ask-occasion is drawn
conditioned on the candidate being present, so it is not a sample from the
world. If that is it, ASKING CORRUPTS THE COUNTS IT IS TRYING TO CORRECT, and
the fix is to intervene without learning from the intervention. Nothing here has
tested that, and it should be tested before it is believed.

## IS THE MIN UNFAIR TO THE ARMS? A NULL

`separation` takes a MIN over true partners, inherited from g39-06, and a min
makes one wrongly demoted part cost a whole query. That is a metric choice
rather than a fact about the mechanism, so it is worth knowing whether the arms
fail on strictness or on substance. Both forms, same runs:

    arm            budget      min      mean
    watch             0.0  -0.2967  -0.2864
    ask-repeat       0.25  -0.3189  -0.2906
    ask-mutual       0.10  -0.3297  -0.2865

**The min exaggerates the damage and does not cause it.** Under the mean the
arms stop being actively harmed -- ask-repeat goes from 0.022 below watching to
0.004 below -- and NEITHER FORM SHOWS AN ARM BEATING IT. Reported both ways and
neither is swapped in: the min asks whether EVERY real surface outranks the
confound, which is the stricter and the registered question.

So "partial coverage is not partial credit" is too strong and is corrected here:
under the mean, partial coverage does earn partial credit, and at 25 pairs of
108 the credit is simply too small to move a 36-query average. The obstacle is
the amount of coverage and not the shape of the metric.

## RE-PRICING THE ASK BUYS COVERAGE AND NOT SEPARATION

`World(config, charge_per_ask=1)` charges an ask one action instead of every
occasion the rejection search looked at. **Default is off**, so every earlier
number stands.

                          per query   on target
    watching                -0.2967           0
    ask-repeat, rejection   -0.3189           6
    ask-repeat, one draw    -0.3067          25

**Four times the coverage and still below watching.** I had written that the gap
between the arms and the ceiling was "bookkeeping in the simulator, not a limit
on asking". That was a prediction stated as a conclusion and it is refuted:
the accounting WAS capping coverage, and coverage was not what was missing.

The reason is one already measured here and it governs throughout: `separation`
takes a MIN over true partners, so a single wrongly demoted part costs the whole
query while a correctly demoted confound helps one term. **Partial coverage is
not partial credit**, at 6 pairs or at 25. The ceiling reaches +0.1444 at 108.

So the arms need near-total coverage rather than more of it, and nothing here
has shown a way to buy that under any accounting.

## P20 AND P21: A POLICY THAT WALKS THE GRAPH, registered before it exists

**John's observation, and it is the only lead here that is not another face of
the product bound.** Every policy in this file nominates by DIRECT association
-- `conditional`, then mutual predictability -- and both are one-hop.
`grep -c "pathways|flood|reach|routed"` in this file returns 0: it builds a
graph out of every moment and then reads single edges from it.

**A confound is a TWO-hop fact.** A shadow is tied to its concept's surfaces
only THROUGH the concept: it has no reason of its own to co-occur with any
particular surface, so its neighbourhood is the concept's neighbourhood and
nothing else. A true partner has its own. That difference is visible in the
graph and invisible to any single edge.

    ask-structural   nominates the candidate whose neighbourhood is most nearly
                     CONTAINED in the query's, which is what "tied to it only
                     through the concept" looks like from outside

    P20  ask-structural lands >30 of the 108 scored pairs on target at budget
         0.10, matching what mutual predictability manages
    P21  and it beats watching by >0.05, which no policy has done

### What happened: BOTH REFUTED, and not in the way that was expected

    arm              per query   on target   shadow
    watch              -0.2967           0     0.0%
    ask-mutual         -0.4099          52    48.2%
    ask-structural     -0.3106           9      0.5%

**P20 refuted at 9 of 108, and P21 with it.** The interesting number is the last
column: containment nominates a shadow 0.5% of the time against mutual
predictability's 48%. It does not fail to EXPLOIT the structure, it fails to
FIND the confound at all.

**Why, and it is the background again.** Containment rewards a small
neighbourhood entirely inside the query's, and the surfaces present in every
occasion are inside everyone's. So the argmax goes to whatever has the fewest
partners — a rare noise surface — and a shadow, which meets the background and
its concept's surfaces like everything else, is not distinctively contained.
This is `ask-targeted`'s failure with the sign reversed: that one was pulled to
the most ubiquitous thing, this one to the least.

**What is NOT refuted is John's lead.** The claim was that a confound is a
two-hop fact a one-hop policy cannot express, and that stands — what is refuted
is that CONTAINMENT is the two-hop quantity that shows it. A measure that
discounted the ever-present background before comparing neighbourhoods has not
been tried, and the same fix worked for `ask-mutual`, where reading the reverse
direction was what the background could not fake.

**P21 was the one I expected to fail and the one that matters.** The product bound
says an arm must RESOLVE a rate per pair, and structure changes which pairs get
asked, not how many asks each needs. If P20 holds and P21 fails, structure picks
targets no better or worse than mutual predictability and the bound is
untouched. If both hold, the bound was an artefact of asking about the wrong
things.

## P22: THE SAME TWO-HOP IDEA, WITH THE BACKGROUND DISCOUNTED

Containment failed because a surface in every occasion is inside everyone's
neighbourhood, so the argmax went to whatever had the fewest partners. **That is
the third policy here defeated by the ever-present background** -- `conditional`
scores it 1.0, containment counts it as shared, and only `ask-mutual` survived
it, by reading the direction the background cannot fake.

So weight each shared partner by how much its presence says. A surface present
in every occasion has `P = 1` and contributes NOTHING; a rare one contributes
most. `informative` overlap is containment with that weighting:

    weight(partner) = 1 - seen(partner) / occasions

    P22  ask-informed lands >30 of the 108 scored pairs on target at budget
         0.10, which containment managed 9 of
    P23  and nominates a shadow on >20% of its asks, against containment's 0.5%

### What happened: BOTH REFUTED, and the registered reading applies

    arm              per query   on target   shadow
    ask-mutual         -0.4099          52    48.2%
    ask-structural     -0.3106           9      0.5%
    ask-informed       -0.3056           5      0.1%

**P22 refuted at 5 of 108, P23 at 0.1%.** Discounting the background did not
change who gets asked about; it narrowed the choice further. The weighting was
the repair that rescued `ask-mutual`, and here it made the measure worse.

**The registered reading stands: containment does not find confounds in this
world, weighted or not.** Both variants go to a candidate with a small, tidy
neighbourhood, and a shadow's neighbourhood is neither small nor tidy — it meets
its concept's surfaces AND the background, exactly as a true partner does. The
asymmetry that distinguishes them is DIRECTIONAL, which is why reading
`P(query | candidate)` works and why measuring overlap does not.

**Scoping this precisely, because the registration was broad.** What is refuted
is the CONTAINMENT FAMILY: shared-neighbourhood measures, with and without a
frequency discount. A two-hop quantity that is directional the way `ask-mutual`
is directional has not been built, and nothing here says one cannot exist. What
is refuted is the version anyone would write first, twice.

**P23 was the diagnostic and P22 the claim.** P23 failing means the weighting did
not change WHO gets asked about, and the two-hop direction is then out of ideas
in this world rather than merely unimplemented. Neither says anything yet about
beating watching -- the product bound is untouched by which pairs are chosen.

## A RECORDED NUMBER DRIFTED AND NOTHING NOTICED

**`ask-mutual` lands 46 of 108, not the 53 recorded when P5 was scored.** Found
by accident: a set-ask extension to `World.ask` produced 46, I treated that as a
claim about my own code, and reverting it gave 46 as well. The extension was
innocent; the figure had drifted earlier, under the `charge_per_ask` restructure
of `ask`, and every run since has quoted a number that no longer reproduced.

**P5's verdict is unaffected** — its threshold was 30 and 46 clears it — which is
exactly why nothing caught it. A mechanically scored prediction protects the
VERDICT and says nothing about a figure quoted in prose beside it.

Corrected throughout. Recorded because *"a finding updates a line, it never
appends an entry"* assumes the line still holds, and this one had stopped.

## STOP BUILDING NOMINATORS. THE ARITHMETIC SAYS THEY CANNOT WIN

Five policies have been built here: `ask-random`, `ask-targeted`, `ask-mutual`,
`ask-structural`, `ask-informed`. The obvious sixth is a DIRECTIONAL two-hop
measure, since direction is what separates the one that works from the four that
do not. **It should not be built, and the reason is already measured.**

`ask-mutual` lands **52 of the 108 scored pairs** — half of everything the
metric reads, at 48% shadows — and still scores -0.4099 against watching's
-0.2967. **A better nominator is not the constraint**, because the constraint is
the PRODUCT: pairs times asks-per-pair. Choosing better pairs moves one factor
and the budget then takes it back out of the other.

**The lever that is left is the OTHER factor: asks per pair.** A true partner
refuses at 0.3837 and a shadow at 0.2222, so the signal is a 0.16 gap and
resolving it takes ~48 asks. **A question with a wider gap would need fewer**,
and nothing here has tried changing the QUESTION rather than the target:

- ask both directions of the same pair and compare, which is two facts per pair
  rather than one and is the same asymmetry `ask-mutual` exploits, spent on
  resolution instead of on selection;
- ask about a SET rather than a pair, so one refusal constrains many candidates
  at once -- the thing that would actually break a product bound rather than
  trade along it.

Neither is built. Both are about the price of a fact, which is the factor five
policies could not touch.

## P24 AND P25: ONE ASK, MANY FACTS. Registered before the extension exists

The product bound is pairs times asks-per-pair, and five policies moved only the
first factor. **A set-ask moves neither factor — it changes how many facts one
ask buys**, which is the only shape of change that breaks a product rather than
trading along it.

Ask for a candidate without ANY of several queries. **Compliance is the
informative outcome**: if it came back without all of them, it is detached from
all of them, and one ask has settled N pairs. Refusal says only that at least
one held, which is weak — so the value depends entirely on how often compliance
happens.

**And it happens most where it is wanted.** A shadow refuses at 0.2222, so it
COMPLIES 78% of the time; a true partner complies 62%. The outcome that carries
N facts is the outcome a confound produces most.

    P24  ask-set at budget 0.10 lands >60 of the 108 scored pairs on target,
         against ask-mutual's 46, because one ask now settles several
    P25  and it beats watching by >0.05, which no arm has done

### What happened: BOTH REFUTED, and a small positive margin that is NOT a win

    arm          per query   on target   pairs   shadow
    watch          -0.2967           0       0     0.0%
    ask-mutual     -0.4099          52     100    48.2%
    ask-set        -0.2881          13     180    35.0%

**P24 refuted at 13 of 108** — one ask naming four queries settles four pairs
only when it complies, and compliance concentrates the coverage on candidates
that detach easily rather than spreading it. **P25 refuted at +0.0086 against a
threshold of 0.05.**

**And ask-set is nonetheless the first arm here to score above watching at all.**
At 12 seeds the margin is **+0.0102**, so it survived four times the seeds — more
than the withdrawn +0.0164 managed. But the per-seed ranges overlap
([-0.3019, -0.2857] against [-0.3057, -0.2640]), so **this is not established
and must not be reported as a result.** This project has withdrawn two claims of
exactly this size and shape, both mine, both on the day they were made.

**The paired comparison settles it, and the answer is yes-but-tiny:**

    PAIRED, 20 seeds, same world on each side
      ask-set beats watch on 16/20 seeds
      mean difference +0.0085, sd of the mean 0.0025
      worst -0.0134, best +0.0250

**3.4 standard errors above zero. THE FIRST ARM IN THIS FILE TO BEAT WATCHING,
and it is established rather than suggested.** Twenty paired seeds, the loser
losing by less than the winner wins, and the sign holding on four out of five.

**And it is 6x smaller than P25 asked for, so P25 stays refuted.** Against the
oracle's swing from -0.2967 to +0.2256 it recovers about **1.6%** of what the
mechanism can do. The product bound is DENTED, not broken: a cheaper fact buys a
real improvement and nowhere near the one available.

### WHAT ACTUALLY WORKS: asking a candidate against ITS OWN neighbourhood

Of the scored pairs each arm demotes, how many are confounds — measured, not
reasoned, because the last explanation here lasted one commit:

    arm          on target   shadow   true
    ask-mutual          51      0.2   51.0
    ask-set             12      4.0    8.2

**`ask-mutual` demotes real parts and essentially nothing else.** 51 of 51
scored pairs are true partners. Its 48% shadow ASK rate never becomes a shadow
SCORED pair, because it asks a shadow against whichever query made it notice the
shadow — and that query is usually not one of that shadow's own concept's
surfaces, so `separation` never reads the pair. It pays for the right suspects
and files them under the wrong questions.

**`ask-set` asks a candidate against ITS OWN top partners**, so when the
candidate is a shadow, those partners ARE its concept's surfaces and the pair is
one the metric reads. A third of its hits are confounds against `ask-mutual`'s
0.4%.

**The design principle, and it is the useful part:** ask about a candidate
relative to what IT predicts, not relative to the query that made you notice it.
Nomination and interrogation had been the same step in every arm here, and
separating them is what put demotions where they help.

**The margin is still +0.0085 and the oracle's swing is +0.52**, so this is a
mechanism identified rather than a problem solved. What it explains is why five
arms with better coverage did worse: coverage of the metric is not the same as
coverage of the CONFOUNDS, and every earlier arm optimised the first.

### THE SET MECHANISM IS NOT WHAT WORKS. The control says so

Sweeping `SET_SIZE`, every cell shown, paired against watching on 12 seeds:

    SET_SIZE    margin   sd/mean   wins
           1   +0.0075    0.0020   10/12
           2   +0.0164    0.0032   12/12
           4   +0.0102    0.0031   10/12
           8   +0.0077    0.0033    9/12
          16   +0.0078    0.0037    9/12

**`SET_SIZE = 1` names one query, so it IS an ordinary single ask — and it
already delivers +0.0075 of the +0.0102.** One ask buying many facts is not what
beats watching. The gain survives when the set is removed.

**So my explanation was wrong.** I wrote that this "changed the price of a fact
rather than the choice of which fact to buy". The control says the opposite: the
price is incidental, and what changed is WHICH PAIRS get asked about. `ask-set`
nominates a candidate and then asks it against ITS OWN top partners rather than
against the query it was nominated for — a different pair population entirely,
not a better-targeted version of the same one.

**Which also qualifies "stop building nominators".** That argument was that
better targeting cannot help, because `ask-mutual` lands half the scored pairs
and still loses. It holds for choosing better among the SAME pairs. It does not
cover choosing a different population, which is what this does, and the effect
is small but real.

**The peak at 2 is not being claimed.** +0.0164 at 12/12 is the best cell of a
sweep of my own arm's hyperparameter, with no matching sweep on any other arm,
which is exactly the tuning this project's rules forbid reporting. The whole
sweep is here so the best cell cannot be read as the result.

**What this establishes and what it does not.** Established: an autonomous arm
CAN beat watching, so the wall is not absolute. Not established, and now REFUTED: that
set-asking is the mechanism. It is not; the gain is there at set size one.

**P25 was the real one.** Every previous failure traced back to a pair needing
~48 asks to resolve a 0.16 gap. If a set-ask buys N pairs per ask, the effective
budget multiplies and that is the first mechanism here that touches the binding
factor. If P24 holds and P25 fails, the bound survives even a cheaper fact, and
the constraint is deeper than accounting.

**Requires extending `World.ask`, whose `absent` is a single surface.** The
extension must keep the current behaviour exactly, since every number in this
file was measured through it.

## P26 AND P27: the two things that helped, combined. Registered first

Two findings here point the same way and have never been used together.

**P15 held:** concentrating asks on fewer pairs beats spreading them, because
the threshold rule needs ~48 asks to resolve a 0.16 gap.

**And the new one:** asking a candidate against ITS OWN top partners lands on
scored CONFOUND pairs, where asking it against the query that nominated it lands
on real parts — `ask-set` demotes 4 confounds to 8 parts, `ask-mutual` 0.2 to 51.

`ask-aimed` does both: nominate by mutual predictability, interrogate against
the candidate's own neighbourhood, and STAY on that candidate for `REVISITS`
asks before moving on.

    P26  paired against watching over 20 seeds, ask-aimed beats it by >0.02 —
         more than double ask-set's +0.0085, since it should resolve the pairs
         ask-set only samples
    P27  and its demoted scored pairs are >50% confounds, against ask-set's 33%

**P27 is the mechanism and P26 the payoff.** P27 failing means concentration
does not preserve the aim — that staying on one candidate spends the budget on
whichever of its partners happen to be real parts. That would be the two
findings interfering rather than compounding, which is worth knowing and is not
what I expect.

## WHAT AN ASK COSTS, and a leak that has to be closed before it is used

The arms are coverage-limited and the reason is priced, not chosen:

    asking about a true partner   8.63 draws
    asking about a shadow         2.91 draws
    asking about background       1.00 draws
    watching                      1.00 draws

`World.ask` rejection-samples until the world yields the requested
configuration, so an ask costs 3 to 9 draws. Under the equal-exposure invariant
that charges asking up to 8.63x watching, and THAT is what starves every arm --
not a policy, not a budget, and not anything about intervention. **A real
intervening agent acts once.** So the coverage cap is a fact about this harness.

**I called that a LEAK and it is not one.** The claim was that an arm timing its
asks would separate the confound without reading a refusal, on the grounds that
8.63 against 2.91 is a wider ratio than the refusal rates' 1.73. Checking what
the cost actually reports:

    candidate       P(present)    1/P   measured cost
    true partner        0.1223   8.17            8.63
    shadow              0.3597   2.78            2.91
    background          1.0000   1.00            1.00

`ask` rejects on `present` and never on `absent` -- the refusal is decided by
the first qualifying occasion -- so the cost is 1/P(present) and nothing else.
That is the candidate's MARGINAL, which watching counts for free and which is
precisely the quantity that cannot separate a confound: it is why watching sits
at -0.2967. The timing channel is redundant with counting, not ahead of it.

Two ratios over different quantities are not comparable, and comparing them is
how the wrong claim was reached.

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

#: How many times `ask-repeat` stays on a pair. **48 was swept, not chosen here**: it is the smallest value tested at which the learned threshold beats
#: doing nothing (+0.1314 against watching's -0.2967, where 12 asks gives
#: -0.0802). Not tuned on any arm -- the ceiling picked it before an arm used it.
REVISITS = 48

#: How many queries one set-ask covers. **4 is chosen here**, as the largest set
#: that still complies often enough to be worth it: a shadow complies 0.7778 per
#: pair, so four independent ones comply about 0.37 of the time, and a set that
#: almost never complies buys nothing however many pairs it names.
SET_SIZE = 4

#: Budgets for the price sweep, **chosen here** to bracket 1.0, where an arm asks
#: as much as it watches. P13 and P14 are about that region and the main grid
#: cannot reach it. Measured afterwards to stop binding above 0.25, since an ask
#: consumes a draw and the run ends at `occasions` draws.
PRICES = (0.25, 0.5, 1.0, 2.0)


def world_config(seed: int, alone: float) -> OccasionConfig:
    return OccasionConfig(concepts=12, surfaces=3, presence=0.7, noise=2,
                          distractors=1, shadows=12, shadow_alone=alone,
                          occasions=4000, seed=seed)


def separation(index: CoOccurrence, config: OccasionConfig, statistic,
               refusals: dict, rule: str = '') -> float:
    """`g39-06`'s quantity: weakest TRUE partner minus the confound.

    Positive means every real surface of the concept outranks the thing that
    merely follows it around. Averaged over concepts, so one lucky concept
    cannot carry the arm.
    """
    cut = learned_threshold(refusals) if rule == 'threshold' else 0.0
    scores = []
    for concept in range(config.concepts):
        own = [concept * config.surfaces + m for m in range(config.surfaces)]
        shadow = config.shadow_of(concept)
        for query in own:
            partners = [s for s in own if s != query]
            if not partners:
                continue
            weakest = min(adjusted(index, statistic, p, query, refusals,
                                   rule, cut) for p in partners)
            confound = adjusted(index, statistic, shadow, query, refusals,
                                rule, cut)
            scores.append(weakest - confound)
    return sum(scores) / len(scores) if scores else 0.0


def adjusted(index: CoOccurrence, statistic, candidate: int, query: int,
             refusals: dict, rule: str = '', cut: float = 0.0) -> float:
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
    rate = refused / asked
    if rule == 'per-query':
        # THE CUT, FITTED WHERE IT IS APPLIED. A global split learns from pairs
        # the demotion never touches -- the arm's cut is 0.6278 against an
        # oracle boundary of 0.2870 because unscored pairs refuse at 0.6667.
        # This one can only see candidates for this query.
        local = {k: v for k, v in refusals.items() if k[1] == query}
        here = learned_threshold(local)
        return score * rate if here and rate < here else score
    if rule == 'threshold':
        # A SPLIT, NOT A GRADIENT. Below the learned cut the candidate
        # detaches more easily than the rest and is demoted; above it,
        # nothing happens, because being hard to detach is what a part
        # looks like and there is no credit to give for it.
        return score * rate if rate < cut else score
    if rule != 'comparative':
        return score * rate
    # AGAINST THE OTHER CANDIDATES FOR THIS QUERY, not against 1.0. Only a
    # candidate detached more easily than its neighbours is demoted, and the
    # cap means being HARDER to detach than average is never a bonus.
    peers = [r / a for (c, q), (a, r) in refusals.items()
             if q == query and a]
    typical = sum(peers) / len(peers) if peers else 0.0
    if typical <= 0.0:
        return score * rate
    return score * min(1.0, rate / typical)


def learned_threshold(refusals: dict, rounds: int = 40) -> float:
    """Split the observed refusal rates in two, and return the boundary.

    **Nothing here may look at what a surface IS.** It reads only rates the arm
    paid for, so the arm could run this itself.

    Two means, seeded at the extremes and iterated to a fixed point. The measured
    structure it is built for: true partners run 0.292 to 0.474 and shadows 0.135
    to 0.292 at 192 asks, with zero of 216 true partners below the highest
    shadow, so a boundary exists and the only question is finding it unaided.
    """
    rates = sorted(r / a for a, r in refusals.values() if a)
    if len(rates) < 2:
        return 0.0
    low, high = rates[0], rates[-1]
    if high - low < 1e-9:
        return 0.0
    for _ in range(rounds):
        boundary = (low + high) / 2
        under = [r for r in rates if r < boundary]
        over = [r for r in rates if r >= boundary]
        if not under or not over:
            return 0.0
        moved_low = sum(under) / len(under)
        moved_high = sum(over) / len(over)
        if abs(moved_low - low) < 1e-9 and abs(moved_high - high) < 1e-9:
            break
        low, high = moved_low, moved_high
    return (low + high) / 2


def wrongly_demoted(config: OccasionConfig, statistic,
                    per_pair: int = 48) -> float:
    """At `shadow_alone` 0.0, how often does the threshold demote a real part?

    **The question P12 asks, and the one a confound detector has to answer.**
    Here the shadow genuinely cannot be had without its concept, so there is no
    confound to find and the correct behaviour is to demote nothing. A two-means
    split always returns two groups, so it will demote whichever group detaches
    more easily -- and at 0.0 that is the TRUE PARTNERS, refused 0.3917 against
    the shadow's 0.7326.

    Returns the share of queries where at least one true partner falls below the
    learned cut.
    """
    world = World(config)
    refusals: dict = {}
    for concept in range(config.concepts):
        own = [concept * config.surfaces + m for m in range(config.surfaces)]
        for query in own:
            for cand in [s for s in own if s != query] + [
                    config.shadow_of(concept)]:
                asked = refused = 0
                for _ in range(per_pair):
                    refused += world.ask(present=cand, absent=query).refused
                    asked += 1
                refusals[(cand, query)] = (asked, refused)
    cut = learned_threshold(refusals)
    queries = harmed = 0
    for concept in range(config.concepts):
        own = [concept * config.surfaces + m for m in range(config.surfaces)]
        for query in own:
            queries += 1
            for cand in [s for s in own if s != query]:
                asked, refused = refusals[(cand, query)]
                if refused / asked < cut:
                    harmed += 1
                    break
    return harmed / queries if queries else 0.0


def informative(index: CoOccurrence, candidate: int, here: set) -> float:
    """Containment, with each shared partner weighted by what it says.

    **The repair containment needed, and the same one `ask-mutual` needed.** A
    surface present in every occasion is inside every neighbourhood, so counting
    it as shared makes containment a measure of how FEW partners a thing has.
    Weighting by `1 - seen/occasions` sends it to zero and leaves the rare
    partners -- the ones whose co-occurrence was a fact about the world rather
    than about the room -- carrying the comparison.
    """
    theirs = index.partners(candidate)
    if not theirs:
        return 0.0
    total = shared = 0.0
    for partner in theirs:
        weight = 1.0 - index.seen(partner) / max(index.occasions, 1)
        total += weight
        if partner in here:
            shared += weight
    return shared / total if total else 0.0


def containment(index: CoOccurrence, candidate: int, here: set) -> float:
    """How much of `candidate`'s neighbourhood lies inside `here`.

    **The two-hop quantity a one-hop policy cannot express.** A shadow appears
    with its concept and for no reason of its own, so everything it meets is
    something the concept's surfaces also meet: its neighbourhood is a subset of
    theirs. A true surface has partners of its own -- noise it happened to
    co-occur with, other concepts it turns up beside -- so its neighbourhood
    leaks outside.

    Ties broken toward the smaller neighbourhood, since a candidate met once is
    trivially contained and says nothing.
    """
    theirs = set(index.partners(candidate))
    if not theirs:
        return 0.0
    inside = len(theirs & here) / len(theirs)
    return inside * (1.0 - 1.0 / (1.0 + len(theirs)))


def run_arm(arm: str, config: OccasionConfig, budget: float, statistic,
            rng: random.Random, learn_from_asks: bool = True,
            interleave: bool = False) -> dict:
    """One arm on one world. Every arm spends `config.occasions` draws."""
    world = World(config)
    index = CoOccurrence()
    refusals: dict = {}
    asks = int(config.occasions * budget) if arm != "watch" else 0
    seen: list[int] = []
    shadow_asks = 0
    made = 0
    observed = 0
    staying: tuple | None = None

    while world.drawn < config.occasions:
        spend_on_ask = arm != "watch" and asks > 0 and len(seen) > 4
        if interleave and spend_on_ask:
            # MIX THEM THROUGH THE RUN. Without this an arm asks the moment
            # it can and never watches again, which froze one index at a
            # single observation and made -0.0741 look like a result.
            spend_on_ask = made / max(world.drawn, 1) < budget
        if not spend_on_ask:
            occasion = world.watch()
            index.observe(occasion.surfaces)
            observed += 1
            seen.extend(occasion.surfaces)
            continue

        if arm == "ask-repeat" and staying is not None:
            # STAY ON THE PAIR. Nominating a fresh pair every draw is the worst
            # available spend for a rule that needs a resolved rate per pair.
            candidate, query = staying
            answer = world.ask(present=candidate, absent=query)
            asks -= 1
            made += 1
            if answer.occasion is not None:
                # AN ASK-OCCASION IS NOT A SAMPLE OF THE WORLD. It was
                # drawn conditioned on the candidate being present, so
                # learning from it over-represents exactly the surfaces
                # this arm chose to ask about.
                if learn_from_asks:
                    index.observe(answer.occasion.surfaces)
                    observed += 1
                    seen.extend(answer.occasion.surfaces)
                was, refused = refusals.get((candidate, query), (0, 0))
                refusals[(candidate, query)] = (was + 1,
                                                refused + answer.refused)
                shadow_asks += config.is_shadow(candidate)
                if was + 1 >= REVISITS:
                    staying = None
            continue

        if arm == "ask-set":
            # ONE ASK, MANY FACTS. Ask a candidate without ANY of several
            # queries: COMPLIANCE means it was had without all of them, so
            # every pair is settled at once. A refusal says only that at least
            # one held and cannot be attributed, so it buys a single ordinary
            # ask instead -- which is what makes this a cheaper fact rather
            # than a vaguer one.
            query = rng.choice(seen)
            partners = index.partners(query)
            if not partners:
                continue
            candidate = max(partners, key=lambda p: min(
                statistic(index, p, query), statistic(index, query, p)))
            if candidate == query:
                continue
            targets = [t for t in index.partners(candidate)
                       if t != candidate][:SET_SIZE] or [query]
            answer = world.ask(present=candidate, absent=targets)
            asks -= 1
            made += 1
            if answer.occasion is not None:
                if learn_from_asks:
                    index.observe(answer.occasion.surfaces)
                    seen.extend(answer.occasion.surfaces)
                shadow_asks += config.is_shadow(candidate)
                if not answer.refused:
                    for target in targets:
                        was, refused = refusals.get((candidate, target), (0, 0))
                        refusals[(candidate, target)] = (was + 1, refused)
                else:
                    single = world.ask(present=candidate, absent=targets[0])
                    asks -= 1
                    made += 1
                    was, refused = refusals.get((candidate, targets[0]), (0, 0))
                    refusals[(candidate, targets[0])] = (
                        was + 1, refused + single.refused)
            continue

        query = rng.choice(seen)
        if arm == "ask-random":
            candidate = rng.randrange(config.vocabulary)
        else:
            partners = index.partners(query)
            if not partners:
                candidate = rng.randrange(config.vocabulary)
            elif arm == "ask-informed":
                # THE SAME TWO-HOP IDEA, WITH THE BACKGROUND DISCOUNTED. A
                # surface in every occasion says nothing about who it meets, so
                # it weighs zero here; a rare one weighs most. Containment
                # counted it like any other partner, which is why the argmax
                # went to whatever had the fewest.
                here = set(index.partners(query))
                candidate = max(partners, key=lambda p: informative(
                    index, p, here))
            elif arm == "ask-structural":
                # WALK THE GRAPH INSTEAD OF READING ONE EDGE. A shadow is tied
                # to this query only THROUGH the concept, so it co-occurs with
                # what the query co-occurs with and nothing else -- its
                # neighbourhood is CONTAINED in the query's. A true partner
                # brings its own. Containment is a two-hop fact and no single
                # edge shows it.
                here = set(index.partners(query))
                candidate = max(partners, key=lambda p: containment(
                    index, p, here))
            elif arm in ("ask-mutual", "ask-repeat"):
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
        made += 1
        if answer.occasion is not None:
            if learn_from_asks:
                index.observe(answer.occasion.surfaces)
                observed += 1
                seen.extend(answer.occasion.surfaces)
            was, refused = refusals.get((candidate, query), (0, 0))
            refusals[(candidate, query)] = (was + 1, refused + answer.refused)
            shadow_asks += config.is_shadow(candidate)
            if arm == "ask-repeat" and was + 1 < REVISITS:
                staying = (candidate, query)

    tallies = list(refusals.values())
    wanted = scored_pairs(config)
    return {
        "arm": arm, "budget": budget,
        "separation": separation(index, config, statistic, refusals),
        "separation_comparative": separation(index, config, statistic,
                                             refusals, 'comparative'),
        "separation_threshold": separation(index, config, statistic,
                                           refusals, 'threshold'),
        "separation_per_query": separation(index, config, statistic,
                                           refusals, 'per-query'),
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
        "observed": observed,
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
        "separation_comparative": separation(index, config, statistic,
                                             refusals, 'comparative'),
        "separation_threshold": separation(index, config, statistic,
                                           refusals, 'threshold'),
        "separation_per_query": separation(index, config, statistic,
                                           refusals, 'per-query'),
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
        for arm in ("watch", "ask-random", "ask-targeted", "ask-mutual",
                    "ask-structural", "ask-informed",
                    "ask-set"):
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
                   "separation_comparative": mean("separation_comparative"),
                   "separation_threshold": mean("separation_threshold"),
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
    # -0.0500 and asking 46 of 108 well-chosen pairs reaches -0.5130, so a
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
    # IS THE COMPARATIVE RULE WRONG, OR STARVED? At 46 of 108 pairs many
    # queries have ONE asked candidate, and a candidate compared only against
    # itself is never demoted. This runs the same rule at full coverage.
    full = sum(t["separation_comparative"] for t in tops) / len(tops)
    rows.append({"arm": "ceiling (comparative)", "separation": full})
    print(f"  comparative, full coverage      {full:>10.4f}  <- the same rule "
          f"the arm could not afford")
    # THE THRESHOLD, ON SCORED PAIRS ONLY. The arm learns its cut from every
    # ask it made, and most of those are background pairs that detach for
    # free, so its split lands between background and everything else rather
    # than between confounds and parts. This is the rule without that.
    cut = sum(t["separation_threshold"] for t in tops) / len(tops)
    rows.append({"arm": "ceiling (threshold)", "separation": cut})
    print(f"  threshold, full coverage        {cut:>10.4f}  <- learned from "
          f"scored pairs alone, which is what the arm cannot arrange")
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

    comp = {(r["arm"], r["budget"]): r["separation_comparative"]
            for r in rows if "separation_comparative" in r}
    p8 = comp.get(("ask-mutual", 0.10), 0.0) - comp.get(("watch", 0.0), 0.0)
    verdicts.append(("P8", "comparative demotion: ask-mutual beats watch "
                     "by >0.05", p8 > 0.05, f"{p8:+.4f}"))

    control_rows = []
    for seed in SEEDS:
        control_rows.append(run_arm("ask-mutual", world_config(seed, 0.0), 0.10,
                                    statistic, random.Random(seed)))
        control_rows.append(run_arm("watch", world_config(seed, 0.0), 0.0,
                                    statistic, random.Random(seed)))
    def comparative_mean(arm):
        got = [r["separation_comparative"] for r in control_rows
               if r["arm"] == arm]
        return sum(got) / len(got)
    p9 = comparative_mean("ask-mutual") - comparative_mean("watch")
    verdicts.append(("P9", "and at shadow_alone 0.0 it does NOT beat watch "
                     "by >0.02", not p9 > 0.02, f"{p9:+.4f}"))

    thresh = {(r["arm"], r["budget"]): r["separation_threshold"]
              for r in rows if "separation_threshold" in r}
    p10 = thresh.get(("ask-mutual", 0.10), 0.0) - thresh.get(("watch", 0.0), 0.0)
    verdicts.append(("P10", "learned threshold: ask-mutual beats watch by "
                     ">0.05", p10 > 0.05, f"{p10:+.4f}"))

    def control_mean(arm, key):
        got = [r[key] for r in control_rows if r["arm"] == arm]
        return sum(got) / len(got)
    p11 = control_mean("ask-mutual", "separation_threshold") - \
        control_mean("watch", "separation_threshold")
    verdicts.append(("P11", "and at shadow_alone 0.0 it does NOT beat watch "
                     "by >0.02", not p11 > 0.02, f"{p11:+.4f}"))

    # P12: DOES IT KNOW HOW TO FIND NOTHING? Registered expecting a failure.
    wrong = wrongly_demoted(world_config(SEEDS[0], 0.0), statistic)
    verdicts.append(("P12", "a NOTHING-TO-FIND state: at alone 0.0 it demotes "
                     "true partners on <20% of queries", wrong < 0.20,
                     f"{wrong:.1%} of queries"))

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
