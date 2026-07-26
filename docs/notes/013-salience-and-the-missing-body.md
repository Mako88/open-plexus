# Note 013 — The body, and why this benchmark cannot have one

John's observation, which is the right diagnosis of a measured failure:

> *It may need a "body" that triggers storage. A real brain gets chemicals
> flooding due to external events. And it's stronger for things that are very
> good and very bad.*

[g7-04](../../experiments/sweeps/g7-04-when-does-forgetting-pay.txt) had measured
consolidate-on-use as monotonically harmful, and the reason was exactly this: the
gate fired on every **correct** prediction, which once the model works is most of
them. The store filled with the routine. A gate that fires on the *tails* — very
wrong and very right — fires rarely by construction.

So it was built. Three things came out, and the third is decisive.

## 1. It needs a compensatory process, and biology says so in a title

Consolidating on correctness is self-limiting: being correct means the retrieval
was already good, so promoting it adds nothing extreme. **Consolidating on
surprise is positive feedback.** A large surprise promotes a large retrieval,
which enlarges the store, which enlarges later retrievals and later surprises.
Unbounded, it reaches NaN — measured, not predicted.

Zenke and Gerstner, in the source list that prompted this, put it in a title:
*Hebbian plasticity requires compensatory processes on multiple timescales*.
Hebbian storage is unstable by construction and biology pairs it with something
that pulls the total back down.

`lasting_cap` is that, in its crudest form: when the consolidated store exceeds a
norm, scale the whole thing back. Scaling rather than editing individual entries
is what keeps it local and what makes it synaptic scaling rather than
bookkeeping. The config now refuses a salience gate without a cap, because the
combination has been measured to diverge.

## 2. Stabilised, it still loses

    no consolidation                      0.625
    fires on every correct prediction     0.482
    salience 2.5, cap 1.0                 0.537
    salience 1.5, cap 4.0                 0.293
    salience 1.5, cap 16.0                0.065

Better than the ungated version, worse than not consolidating at all — and
**monotonically worse the more the store is allowed to hold.** The trend points at
a cap of zero, which is no consolidation.

## 3. THE MEASURE WAS BROKEN — corrected below

**Everything in the original section 3 was measured with a surprise definition
that does not measure surprise.** John asked why a repeating pattern was not
becoming less surprising with each repeat, which is what surprise is supposed to
do. It was not, and here is the trace that shows it — mean surprise per repeat of
one 16-token cycle:

    margin surprise, decay 1.0      0.042  0.064  0.091  0.119  0.146  0.174  0.201
    normalised surprise, decay 1.0  2.764  2.687  2.613  2.541  2.471  2.405  2.342

The measure in use was the **margin** between the best score and the arriving
token's. Scores grow as the memory fills, so the margin grows with them: it **rose
266%** across eight repeats of an identical cycle, while the model's predictions
were getting better. It was measuring how big the numbers had become.

The negative log of the normalised score is scale-free and falls monotonically,
which is what John expected and what surprise means. The model now uses it.

## 3 (corrected). It is selective, and not selective enough

With scale-free surprise, over six sequences at salience 2.5:

    binding from a    positions    fired      rate
              pair           48        0    0.0000
             query           72       25    0.3472
            answer           72        2    0.0278
            filler         4410      201    0.0456

  > **Query positions fire at 7.6× the filler rate.** That is a real signal, and
  > it is the right one: in autoregressive MQAR the binding at a query position is
  > the key paired with its answer, which is exactly what a later query needs.

But filler is 92% of the sequence, so 88% of promotions are still filler by sheer
volume. Enrichment of 7.6× is not enough when the base rate is that lopsided.

Performance follows, and improves monotonically as the bar rises:

    no consolidation                    0.625
    fires on every correct prediction   0.482
    salience 2.0                        0.488
    salience 2.5                        0.505
    salience 3.0                        0.537
    salience 4.0                        0.562

Still below not consolidating at all, and the trend points at 0.625 in the limit
of never firing.

## What the correction changes, and what it does not

**Changed:** the original claim that the gate promotes filler *exclusively*, with
zero enrichment for anything useful. That was an artefact of a measure tracking
magnitude rather than error, and it is withdrawn. The gate is genuinely selective.

**Unchanged:** consolidation still does not beat plain forgetting on this task.
Every threshold tested loses, and the trend is monotone toward the no-consolidation
number.

**Sharpened:** the requirement is no longer "surprise is anti-correlated with
usefulness here", which was too strong. It is that **the base rate is against
it** — a 7.6× enriched signal drowns when the uninformative class is 92% of the
data. A task with a heavier tail of genuinely informative rare events would give
the same mechanism a far better ratio to work with, which is the same conclusion
as before but for a defensible reason.

---

*Original section 3, retained because the correction is the point:*

## 3 (original, withdrawn): it promotes filler, exclusively

Replaying one sequence and recording which positions the gate fires on, by what
kind of position produced the binding:

    binding from a    positions    fired     rate
              pair            8        0    0.000
             query           12        0    0.000
            answer           12        0    0.000
            filler          735       44    0.060

  > **Every single promotion came from filler. Not one from a pair.**

*This was read as surprise being anti-correlated with usefulness. With a working
measure it is not: queries fire at 7.6× the filler rate. The zeros above are what
a scale-dependent measure produces, not what the mechanism does.*

## What this does and does not say about the idea

**It does not refute it.** The mechanism does what John described: it fires on the
tails, it is local, it needs the compensatory process biology also needs, and with
that process it is stable. Every part of the design works.

**It confirms, for a third mechanism, that this benchmark cannot test the
question.** [Note 010](010-tagging-and-capture.md) reached the same conclusion for
tagging and capture; [note 011](011-what-rests-on-the-oracle.md) recorded that
nothing implementable had closed the gap to the oracle. This is the sharpest
version yet, because it is not an argument about what MQAR lacks — it is a count.
Forty-four promotions, forty-four from filler.

## What a task would need

The body John describes works because in a real environment **surprising things
are often worth remembering**. A novel sound, an unexpected outcome, a face you
did not expect — the correlation between salience and importance is a property of
the world, not of the brain.

MQAR breaks that correlation deliberately. Its filler exists to be uninformative
and is generated at random, which is precisely what makes it surprising.

So the requirement is now specific:

  > **A task whose surprising content is its informative content.** Real language
  > has this: rare words carry more information than common ones, which is the
  > oldest quantitative fact in the field. MQAR has it exactly backwards.

That is the same conclusion three separate mechanisms have now reached from three
directions, and it is a stronger argument for changing the benchmark than any of
them made alone.
