# Note 038 — A relational task, and the G0 question nobody has asked of it

**Written before any of it is built**, because CLAUDE.md's rule is to search for
prior art when the requirements are written, not when the code is — and note 017
records this project building `reward_recall` from a five-point requirements list
that turned out to describe **bsuite's Memory Length test**. The list was a
search query and was not used as one. This time it was.

## IN PLAIN TERMS

We want the model to learn how things relate to each other rather than which
letter comes next. Before building a test for that, two questions have to be
answered in this order:

1. **Does a good test already exist?** Yes. It is called CLUTRR.
2. **Could our model pass any version of it?** *Unknown, and this is the one
   that matters.* Our model may be structurally unable to do the task at all, in
   which case every number it produced would be chance, and chance looks exactly
   like a hard problem.

## 1. What CLUTRR is, verified rather than recalled

Sinha et al., **arXiv:1908.06177** — *CLUTRR: A Diagnostic Benchmark for
Inductive Reasoning from Text*. Read from the abstract and the authors' own
write-up; **the full paper has NOT been read**, and the marked items below are
unverified.

- **Kinship relations over short stories.** Given a set of stated relations,
  infer an unstated one: A is B's son, C is B's son, C has an uncle D, therefore
  D is A's uncle.
- **22 kinship relations, 15 composition rules.**
- **Train on 2–4 supporting relations, test on up to 10.** This is the whole
  design and it is why the benchmark matters here — see §3.
- **Curated noise facts** for a separate robustness axis.
- Generator code exists at `facebookresearch/clutrr`.
- The authors report **a substantial gap between NLU models (BERT, MAC) and a
  graph network working directly on SYMBOLIC inputs**, with the symbolic model
  generalising better.
- **"Systematic generalisation is hard"** — every model degrades as clause
  complexity rises.

*Unverified:* baseline accuracy numbers, whether the generator emits symbolic
triples directly or only semi-synthetic prose, sequence lengths, and the exact
train/test split sizes. All are in the full paper and should be read before
anything is built against them.

## 2. Why it fits this project better than bits per character

Decision 78 approved moving off character level: a character bigram table is
low-rank because English is, so part of the ceiling measured all day is the
TASK. And concepts cannot be represented over letters, so no change of objective
helps while the units are letters.

CLUTRR's units are **entities and relations**. That is the level John's
relational-objective proposal is actually about.

**The symbolic form is the relevant one.** Our model reads token sequences, not
prose, and the authors' own finding is that a model consuming symbolic input
outperforms one consuming the stories. The natural-language layer is a
difficulty this project has no reason to buy.

## 3. The property that makes it worth the switch

**Decision 69: six mechanisms moved the LEVEL and none moved the SLOPE.** Every
number this project has is "how well does it do", and the thing actually wanted
is "does it keep working as the problem grows".

CLUTRR's primary axis is exactly that — **train on 2–4 hops, test on up to 10**.
Its headline metric is generalisation to lengths never trained on. That is a
slope measurement by construction, and it is the measurement this project has
been unable to make.

## 4. THE G0 QUESTION, WHICH MUST BE ANSWERED FIRST

> *G0 — is there a task that a random, untrained substrate cannot already do,
> and that is learnable from local information at all?*

G0 is marked passed **for MQAR and `reward_recall`**. It says nothing about this
task, and there is a specific reason to think the answer might be no.

**Our model has no multi-hop mechanism.** The readout consumes ONE retrieval:

    r = M @ key      then      answer = Wo @ r

Two-hop inference needs something like `M @ (M @ key)` — a retrieval used as the
key for a second retrieval — and nothing in the model does that. The only thing
resembling it is `retrieval_steps`, which maps back through the store rather
than forward, and which was refuted (and partly recovered in g11-07, at +0.03).

**So CLUTRR at 2–10 hops may be structurally impossible for this model**, in
which case every cell would sit at chance — 1 in 22 — and *chance is
indistinguishable from a hard problem*. That is note 006's failure inverted: it
chose a benchmark that turned out to be already solved; this would be one that
turns out to be unsolvable, and both produce numbers that look like results.

## 5. What to build, and in what order

**Build the task with hop count as a DIAL, starting at one.**

- **1 hop is plain cued recall**, which this model demonstrably does — MQAR is
  exactly that. It is the positive control, and if the model fails at 1 hop the
  implementation is broken rather than the model.
- **2 hops is the first real question**, and the first place the architecture
  might simply have no mechanism.
- **Increasing hops gives a CURVE**, not a pass/fail. Where it breaks is the
  measurement, and a curve that falls to chance at 2 is as informative as one
  that holds to 5 — provided 1 hop works, which is what makes the zero readable.

**The floor must be stated and checked before anything is measured**, per rule
11b and the g8-01 retraction: with 22 relations, guessing is 1/22 = 0.045, and a
model at the floor has not failed the task, it has failed to be measured.

**And the shortcut check**, which is what the MQAR filler bug was: can the answer
be reached without composing? If the query entity appears adjacent to the answer
anywhere in the presentation, a one-hop model scores well and nothing about
composition was tested. **Generate one example and read it** before generating a
million — that bug was found in the first sequence ever produced, by printing it.

## What this note is NOT

Not a decision to adopt CLUTRR wholesale. Its natural-language layer is probably
unwanted and its difficulty may be far above our floor. **What is adopted is its
STRUCTURE** — relations, composition rules, and train-short/test-long — at a
scale this model might survive.

And nothing here is measured. Every claim about CLUTRR is a borrowed claim, cited
so it can be checked, and §4's concern is a prediction rather than a finding.
