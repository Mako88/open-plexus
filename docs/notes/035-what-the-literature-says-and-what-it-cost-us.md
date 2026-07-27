# 035 — the literature pass, and the three measurements it bought

**Status:** four parallel scans of 2025–26 work, then three probes run on our own
code to turn pointers into project facts. **The probes are the part to believe**;
everything attributed to a paper is a pointer to read.

PREDICTIONS: registered inside each probe before running. Two of five were
refuted, and both refutations changed the conclusion — recorded below.

COST: nothing dispatched. Four literature scans, three local probes of seconds
each.

MEASURED ON: Tiny Shakespeare and uniform random tokens, widths 32–256.

---

## 1. The store uses about three dimensions, and widening does not help

Nazari & Rusch (arXiv:2602.04852, Feb 2026) argue that **effective rank**, not
`d`, is the capacity that matters, and that key conditioning enters their bound
squared. So we measured it: `er(S) = ‖S‖_F² / ‖S‖₂²`, on our own store.

    d     kappa(K)   er(S) uniform   er(S) real   minus mean   share of d
    32        4.38            1.14         2.21         2.70         8.4%
    64      396.26            1.17         1.90         2.62         4.1%
   128        5.44            1.24         2.24         2.93         2.3%
   256        2.93            1.19         2.00         2.88         1.1%

**The effective rank is about 3 and does not grow with `d`.** At width 256 the
store behaves as though it had 2.88 dimensions — one percent of what it was
given.

**Two predictions were refuted here and both mattered.** The first version of
this probe ran on uniform random tokens and reported er ≈ 3 as a devastating
architectural finding. It is mostly an artefact: with uniform tokens every pair
is equally likely, so `S ≈ (Σ values) ⊗ (Σ keys)`, which is **rank one** — a
store saying "everything follows everything". That is a property of the input.
The rerun on real text is above. Second, we predicted that removing the rank-1
mean direction would raise the rank "by a lot"; it raises 2.21 to 2.70.

**So the corrected reading, which is stronger than the one we wanted:** the store
faithfully holds a bigram count table (note 033 measured that at cosine 0.88+),
and **a character bigram table over 66 symbols is a low-rank object** — English
is dominated by a few very frequent characters. The store is not failing to use
its width. There is nothing there to use.

**This explains every flat width sweep in the project.** g10-02 asked whether
underfitting was structural or width-limited; the answer is that width was never
the binding constraint at character level. **It also makes a live prediction
against a running sweep**: g11-03's width axis should be roughly FLAT for the
single-token arm. That is falsifiable within hours and we did not know it when
the sweep was dispatched.

**One incidental finding worth keeping:** `kappa(K)` is 396 at `d = 64` against
~5 everywhere else, because vocab is 66 and **a square random key matrix is
near-singular**. Any configuration where vocabulary ≈ width is silently badly
conditioned.

## 2. A missing scalar, not a missing mechanism

g11-01 measured corrective writes fixing rebinding and costing capacity, and we
recorded a trade. The 2025–26 delta-rule literature (Schlag et al. 2021; Wang,
Shi & Fox arXiv:2501.12352) treats the delta rule as a **strictly better
estimator** of the same object, so either it does not transfer to frozen random
keys or our implementation differed.

It differed by a scalar: every published variant **gates** the correction and
ours applied all of it.

    gate     rebinding   capacity at load 256
    Hebbian      0.500      0.997
    0.25         0.922      0.986
    1.00         1.000      0.618

Shipped as `write_gate`. Detail in the config docstring and
`tests/test_write_gate.py`.

## 3. What we are missing is competition, not linearity

**A claim from one scan needs correcting before it propagates.** It described
this model as "a two-layer linear system" in which "no capability can emerge that
is not a linear function of the input" — quoting note 033's own loose phrasing
back at us. **That is wrong.** The retrieval is

    r(t) = Σ_i value(i) · ⟨key(i-1), key(t)⟩

which is a **quadratic interaction between positions**. The binding IS a
multiplicative nonlinearity, and it is the reason the store can hold pair
associations at all.

What we lack is a **competitive** nonlinearity — no softmax, no threshold, no
winner-take-all. The read is a linear read of a bilinearly built store, so
retrieval returns a weighted *average* rather than a selection. That is exactly
the distinction the capacity results turn on: **linear associative capacity
O(d) against softmax O(e^{d/2})**, and Xu et al. (arXiv:2602.01744, Feb 2026)
measure +15.5 points on hardest-case retrieval from reinstating competition on
linear-attention baselines.

**Iterating a retrieval is local** — it re-reads one node's own store — so this
does not obviously cost us C1. It remains untried (note 033, assumption 4).

## 4. What the scans say about the goal, stated without softening

**Nobody has done what this project is trying to do, and nobody has shown it can
be done.** Both halves are load-bearing.

- **The gap is real.** Every published churn solution assumes a departing node's
  state is recoverable — because nodes are bit-identical replicas (PCCL,
  Covenant-72B, Decoupled DiLoCo) or because a pipeline stage is replicated
  across its occupants (SWARM, GWTF). **No published system handles a node
  permanently removing unique, unreplicated state.** Our novelty is not
  "decentralised"; it is "no full replica and no recoverable state".
- **The state of the art without a data centre is Covenant-72B** (arXiv:2603.08163,
  March 2026): ~70 permissionless peers, MMLU 67.1, beating LLaMA-2-70B on half
  the tokens. Minimum hardware to participate: **8× B200 per peer, each holding a
  full 72B replica.** That is "no *single* data centre", not consumer devices.
- **The strongest negative result is a scaling exponent.** Filipovich et al.
  (arXiv:2210.14593) fit compute-optimal scaling for Direct Feedback Alignment
  against backprop: exponents **−0.040 against −0.071**, with DFA closer to a
  *shallow* network's −0.019. A weak local rule does not merely lose a constant;
  **it loses the exponent, so the gap widens with scale and is invisible small.**
- Two independent 2026 groups measure the same curve for locality itself: local
  self-supervised learning at parity through 64×64 images then −6.03 points at
  full ImageNet (arXiv:2601.21683); layer-local training −2.40 on CIFAR-10
  widening to −25.6 on ImageNet-100 (arXiv:2606.06539), with the memory
  justification empirically refuted.

**The action item this generates is a scaling-exponent test on ourselves**, and
it is the most decision-relevant experiment available: it is the one measurement
that could say the goal is unreachable, and it is cheap.

## 5. Biology as policy, not representation — the scoreboard

John's steer was that biology may be the wrong template. The scan was asked for
the strongest version of **both** sides.

**For:** recall-gated plasticity (Lindsey & Litwin-Kumar, eLife 2024) proves a
separation — learnable timescale linear in repetitions under gating against
logarithmic for cascade models — and, pointedly, a two-store control **with the
gate removed performs no better than one store**. Mistake gating
(arXiv:2604.14336) reaches 98% on EMNIST using 12% of backprop's updates with
zero hyperparameters. Hebbian binding used as a *representation* inside a vision
transformer: **+0.3 points best case, −6.9 typical** (arXiv:2605.02920).

**Against, and these are the honest dents:**

- **Biology's own control policy loses to the statistical one.** Holding a
  two-timescale architecture fixed, an EMA merge scores 89.26 and a
  curvature-aware merge 92.12 (arXiv:2606.24007). Biology picked the right
  variable and a mediocre function for it.
- **Titans** (NeurIPS 2025) is a real benchmark win from a biological idea —
  surprise-gated writes — at 760M scale. An independent reimplementation
  (arXiv:2510.09551) found the **write policy reproduces and the architecture
  does not**, which is our distinction, but the win should be conceded.
- **When storage is cheap, consolidation is the wrong idea.** Search over raw
  uncompressed history scores 66.79 on LoCoMo where every engineered memory
  system scores 18–38 (arXiv:2511.21726). Biological consolidation is
  goal-agnostic lossy compression, and that is the property being punished.

**The convergence worth noting.** The best 2026 hybrid memory (HOLA,
arXiv:2607.02303) pairs a compressed store with a bounded exact cache and routes
by `β·‖e‖` — novelty times commitment. **That is synaptic tagging and capture**,
which the g9 line already implements. Biology supplied the policy; the
literature supplies the structure it should sit in.

## 6. What this changes, in order

1. **Stop treating key separation 0.56 as a defect.** Zahn et al.
   (arXiv:2601.15313) derive interference as O(N·ρ) in mean key cosine and
   measure collapse at N=5 for ρ > 0.6 — and their *recommended fix is
   hash-derived keys*, which is what we already have. **We are in the regime the
   literature says to be in.** Similarity does not belong in the key vector; it
   belongs in a compositional id or a sparse retrieval path.
2. **Try competition in the retrieval.** Cheap, local, untried, and the capacity
   literature says it is the difference between O(d) and O(e^{d/2}).
3. **Run the scaling-exponent test.** It is the only measurement here that
   speaks to whether goal 1 is reachable.
4. **Do not widen anything at character level** until §1's prediction is scored
   against g11-03.

## What this note is not

It is three measurements and a reading list. **Every number attributed to a paper
is unverified by us**, and two of the four scans flagged results they could not
confirm from source. The three tables above are ours and were run against
predictions written first.
