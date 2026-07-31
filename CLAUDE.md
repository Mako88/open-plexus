# Engineering standards for Open Plexus

This project is trying to find out whether a neural network can learn using only
local information and bounded asynchrony — so that it can run on consumer
devices that are unreliable, heterogeneous, and constantly leaving. It fails if
no such rule can be found, and it fails *worse* if one appears to be found and
the measurement was wrong. See [GOALS.md](GOALS.md) for what would refute it.

That only means anything if the measurements are trustworthy, so the standards
below are about one thing: **making the code deserve the conclusions drawn from
it.**

They are written as commitments, not as warnings. Everyone building something
non-trivial ships a value that turns out to be disconnected, a bound looser than
they meant, or a claim that outran its evidence — that is what building at the
edge of what you understand feels like from the inside. The answer is not more
care. It is a system that catches those things regardless of how careful anyone
was feeling that day, so that attention can go to the actual problem.

Each rule carries a **calibration** note: a concrete number from this project
showing where the bar has to sit. **They start empty on purpose.** A standard
without a number attached drifts within a month, and "wide enough to admit the
broken case" is far more useful when you can see how wide that turned out to be.
Each unfilled calibration says what kind of instance belongs in it — when one
happens here, write it down. They are reference data, not a record of anyone's
mistakes.

---

## The standard

**Every claim about how the system works is backed by a test or a
measurement.** Everything below is a way of making that true in a specific
situation.

The failure mode this document is built against is not the crash. It is the
*thing that looks connected and is not* — a mechanism that runs, produces
plausible output, and is doing nothing. A flag read once and never applied. A
cache that never hits. A retry on a call that cannot fail. A validation that
runs after the write. Those cost more than crashes, because they invalidate
every observation taken while they were present, silently and retroactively —
and unlike a crash, nothing announces them.

---

## Making claims

**1. State no behaviour that has not been observed.** Not in chat, not in a
docstring, not in a commit message, not in the README. If it has not been run
and watched, say so, and say what would settle it. "Should work" and "this
fixes it" are predictions; label them as such until something confirms them.

**This applies to borrowed claims too, and that is where it gets violated.** A
statement about what someone else measured is a claim about behaviour like any
other. Read the source before letting it carry a decision — a summary tells you
what a result *is called*, not what was actually run, and the gap between those
two is where the expensive errors live. Cite what you read; mark what you only
summarised; never let the second kind gate a design choice.

The reason this needs saying, when rule 1 arguably covers it already: an
unverified borrowed claim does not fail like an unverified local claim. A wrong
claim about our own code gets caught the moment something exercises it. A wrong
claim about the literature is filed under *established*, sits upstream of every
experiment, and **no downstream measurement will ever reach it** — because every
measurement is conditioned on it being true.

> *Calibration.* Two verification passes, two catches, both cheap and both
> upstream of everything. The evidence recommending our credit-assignment scheme
> turned out to describe a *supervised* variant this project cannot use
> ([note 005](docs/archive/notes/005-verifying-the-borrowed-claims.md)). The benchmark
> chosen to have headroom turned out to be *already solved* in the variant
> specified — one query rather than many — which is the exact failure the note
> proposing it was written to prevent
> ([note 006](docs/archive/notes/006-verifying-the-reservoir-claims.md)). Neither would
> have been caught by any experiment downstream of it.
>
> *Calibration, and this one is INTERNAL, which is the version the rule did not
> anticipate.* The rule is written about the literature. The same gap opened between
> two of this project's own documents in a single day. `DECISIONS.md` carried
> `0.9220` — concept partitioning improving accuracy — citing `note 081`, **which
> contains no partitioning measurement at all.** Note 090 quoted the figure against
> one baseline crediting note 065, note 103 quoted it against a different baseline
> crediting *"note 081's companion measurement"*, and note 103 says in its own text
> that it found the claim **in the tree** rather than in the note it went on to cite.
> The tree cited the note, the note cited the tree, and neither held the run.
>
> **The number was real.** Re-run it reproduces to four decimal places in seventy
> seconds ([note 105](docs/archive/notes/105-the-partitioning-accuracy-figure-has-no-source.md)),
> so nothing was fabricated and the conclusion stands. **That is what makes it the
> instructive case rather than the alarming one:** every figure was correct and only
> the pointer was wrong, so no amount of re-reading the numbers would have found it.
> Eight notes were written after it and none did.
>
> It was found by `tools/check_provenance.py` on its first run, over two records,
> because a check that follows the pointer does not get tired. `script` is now a
> required field beside `source` for the same reason: **the run that settles a
> citation dispute is worth more than the citation.**

**A reference that does not resolve is UNCHECKED, not absent — look in
`docs/archive/` before concluding the thing does not exist.** John's instruction,
2026-07-30. This project archives rather than deletes, so the failure mode is not
a missing file, it is a *moved* one: the decision log lives in
`docs/archive/decisions-*.md` and all 105 investigation notes now live in
`docs/archive/notes/`. A grep of the live tree that comes back empty has therefore
not established anything, and the distance between *"I could not find it"* and
*"it was never measured"* is the whole width of rule 12's most expensive error —
discarding a good idea on an invalid measurement.

`tools/check_provenance.py` searches the archive itself for exactly this reason, so
a record whose sources moved keeps resolving. **A person has to remember; the
checker does not.**

**2. Observe the quantity the change claims to move, not a downstream proxy.**
A green end-to-end run cannot tell you which of six components is working. When
you change a specific thing, measure that thing directly — then treat the
end-to-end result as a summary, not a diagnosis.

> *Calibration, and it is the good version: ZERO runs went by.* `g31-01` proposed a
> label-free proxy for *"is this write worth making"* — count whether the address
> recurs later — so the quantity could travel to data this project did not generate.
> The proxy is wrong: **recurrence is not demand.** MQAR's filler is drawn from a
> small key range, so a filler address recurs constantly, and the proxy reads the
> stream as 99.9% worth writing where the oracle says 1.1%. Bars of `0.1x` against
> `92.0x`, three orders of magnitude, at every granularity.
>
> **It cost one run because the proxy was required to reproduce the direct quantity
> before being used**, and that requirement was registered as the gate rather than
> noticed afterwards. The proxy was not refined; it was abandoned, and the conclusion
> — that worth-writing is a fact about future demand and is therefore reachable only
> by intervention, not by counting — is worth more than the cross-source table would
> have been.
>
> *And a second catch inside the first.* The control's own first version read `23.0x`
> instead of `92.0x`: it counted every non-filler position as should-store, where
> `g28-01` counts only what `harness.oracle_mask` marks, against filler alone. Two
> classes, one name — **the same mis-specification `g28-01`'s P1 made, one sweep
> earlier.** It was caught only because the control had a published number to hit.
> A control with nothing to reproduce would have read `23.0x` and been believed.

**A variable that never changes does not look like a variable — it looks like
the background.** Before concluding that a mechanism is refuted, list what was
held constant across every experiment that tested it. A constant chosen once,
early, for a plausible reason, and never varied since is the likeliest place for
a wrong answer to hide, because nothing downstream can contradict it and every
individual experiment remains sound.

> *Calibration.* Twice. The projection scale was pinned at a value one step from
> where the mechanism diverges, and silently produced the width curve the
> project's headline came from ([g3-02](experiments/sweeps/g3-02-whats-carried.txt)).
> And an interference account was "refuted" by two correct, well-designed sweeps
> that moved `n_pairs` and `n_keys` — neither of which touches the load, because
> the store binds every consecutive pair and the real load is `seq_len`, held at
> 96 in every sweep the project had ever run
> ([g1-10](experiments/sweeps/g1-10-the-real-load.txt)). The account was right
> for four experiments while looking refuted for two of them.
>
> *Calibration, second kind: a constant CARRIED from another configuration.*
> Twice in consecutive sweeps, and the second time after the first was written
> up as a named risk.
> [g9-09](experiments/sweeps/g9-09-a-small-node-in-a-wide-network.txt) froze
> `slots` 32 and `fade` 0.95 at values chosen for `d_model` 32 in one process,
> then swept node width — and its own file named that as the standing risk before
> dispatch. It was real: g9-10 found the best capacity is not 32 anywhere, and
> g9-09's decline from +0.16 to +0.11 was partly a mistuned tag.
>
> Then [g9-11](experiments/sweeps/g9-11-how-far-should-the-union-reach.txt) froze
> `slots` 4 — g9-10's best **at node 32** — and ran at node 64, where the tag
> alone scores -0.35. Every absolute number in it is at a mistuned capacity.
>
> **So naming the frozen axis as a risk demonstrably does not prevent it.** The
> failure is not forgetting to look; it is looking at the value and not at the
> configuration it came from. What saved g9-11 was that its reported quantity was
> a DIFFERENCE within a cell, so the mistuning cancelled — which was luck in the
> summariser's design rather than care in the grid's.
>
> The cheap habit that would catch it: **when a sweep pins a value taken from an
> earlier sweep, write down which cell it came from, next to the pin.** A line
> reading `slots 4, from g9-10 at NODE 32` sitting above `width 64` is visible in
> a way that `slots 4, FIXED` is not.
>
> **And here is what it cost, measured.** g9-11 was re-run with the identical
> grid and `slots` corrected from 4 to 16, which is g9-10's best at every node
> width it tested. The `tag` arm at delay 8, flat across all four reaches in both
> runs:
>
>     slots 4, carried from node 32    -0.35
>     slots 16, chosen for this grid   +0.23
>
> **A single constant carried from another configuration was worth 0.58 of
> recovery** — more than twice the largest effect any mechanism in the g9 line
> has produced. At delay 20 the same correction is worth about 1.4.
>
> So the failure is not cosmetic and it is not rare: it has now happened twice in
> consecutive sweeps, and the one time it was measured it dominated every
> mechanism being compared.
>
> Writing provenance next to the pin also found `fade` carried from `d_model` 32,
> then `lr` frozen across seven sweeps, then `KEY_SCALE` and `DECAY` arriving by
> import and appearing in no grid at all
> ([note 028](docs/archive/notes/028-the-learning-rate-has-been-frozen-for-seven-sweeps.md)).
> Two cycles of inventory found what seven sweeps of warnings did not.

**A measurement is conditional on the configuration it was taken in. Name the
condition, and re-validate the comparison set when the condition moves.**

A mechanism does not have an effect. It has an effect *given* a task, a readout,
an optimiser and a set of defaults — and when one of those changes, every number
measured beside it becomes a claim about a configuration that no longer exists.
Those numbers are not wrong. They are **conditional**, and the condition moved.

The practice, in order:

- **When a load-bearing component changes, enumerate what it invalidates BEFORE
  building anything on top of it.** Say it out loud in the record, as a list.
  The failure mode is not discovering the invalidation; it is discovering it
  piecemeal, months later, one surprised result at a time.
- **Re-check in order of BLAST RADIUS, not convenience.** The number to re-run
  first is the one that other *arguments* rest on, not the one that is cheapest
  or most recent.
- **Do not stack a new mechanism on a number whose condition has moved.** That
  is how a project accumulates a comparison set it cannot interpret.
- **Seams make this affordable**, which is the real argument for them.
  `openplexus/keys.py` and `openplexus/retrieval.py` exist so a component can be
  swapped in a file rather than a refactor — and re-validation is exactly the
  situation where that stops being tidiness and starts being the difference
  between a re-check and a rewrite.

> *Calibration.* Three times in one day, all in the same direction, and the third
> was found only because the first two had been written down.
>
> **Sparse keys** were measured on MQAR, came out worse, and the knob was left
> off "with a measurement saying not to reach for it". On the corpus they are
> worth 0.18 bits (decision 67). Then the readout changed and they REVERSE — a
> clean crossover, three seeds: linear readout 5.222 dense against 4.794 sparse,
> two-layer readout 4.487 dense against 4.586 sparse (decision 74). Sparsity was
> never a representational improvement; it was compensation for a readout that
> could not disentangle overlap.
>
> **Then the enumeration paid.** Once that pattern was named, the obvious next
> question was which OTHER numbers were taken beside the linear readout — and the
> answer is *all of them*: the exact cache's 0.19 bits, every refutation in the
> comparison set, and the whole g11 line. The cache re-check was run
> deliberately rather than stumbled into, and it was chosen first because
> decision 61's argument for item-partitioning the distributed model rests on it.
>
> **The cost of not doing this** is visible in the same day's record: g11-06
> spent four hours of runner time answering an exponent question that its grid
> could not answer, because the arm had converged below the grid's lower bound
> and nobody had probed for it.

**A hyperparameter swept on one arm of a comparison must be swept on all of
them.** Tuning your own side and comparing against an untuned baseline produces a
better number by exactly the mechanism that produced the wrong one — and it is
undetectable from outside, because the result looks carefully measured.

> *Calibration.* The project's headline figure — the price of locality — was
> measured with the local rule's projection scale and the attention baseline's
> initialisation scale both pinned at untuned defaults. Sweeping only the local
> side would have reported the price falling from "4–6×" to about 2×. Sweeping
> **both** put it at **4.0×**, against a like-for-like 3.0× at the old settings:
> the correction went *against* the favoured hypothesis, and the number nearly
> doubled relative to the one-sided version
> ([g1-08](experiments/sweeps/g1-08-the-honest-price.txt)).

**A prediction written after the controls have run is a weaker test than it
looks.** Record which came first. When a control has already shown how the
mechanism behaves, the prediction is a summary rather than a commitment, and it
should not be counted as evidence that the mechanism was understood in advance.

> *Calibration.* [g7-04](experiments/sweeps/g7-04-when-does-forgetting-pay.txt) is
> the first sweep here where **all four predictions held**, which reads as a
> milestone and is mostly an artefact. Three pre-dispatch controls had already
> established that the decay grid was mis-parameterised, what the corrected one
> did, and that consolidation was harmful once forgetting was sensible. By the
> time the predictions were written they were not guesses. The discipline still
> paid — the controls cost minutes and saved a wasted matrix — but the clean
> scorecard is not the achievement it appears to be, and saying so is cheaper than
> letting a run of successes accumulate unexamined.

**A tool that hard-codes a property of one experiment will be wrong about the
next one, and the direction of the error is not predictable.** Read the grid.

> *Calibration.* The same reporting tool was wrong three times about the same
> sweep family. Twice it was over-confident, fitting an exponent through crossings
> that were bounds. Then, fixed, it was *under*-confident: it computed its own
> resolution as `log(2) / log(span)` — a factor of two hard-coded from the first
> sweep's power-of-two widths — and reported `±0.33, UNRESOLVED` for
> [g5-03](experiments/sweeps/g5-03-a-finer-ruler.txt), whose grid steps are
> 1.25–1.33× and which actually resolves to `±0.14`. **The under-confident failure
> is the more dangerous one**: an over-confident number invites checking, while
> "unresolved" invites another sweep that was never needed.

**Tests check code against claim. Nothing here checked claim against sense.**
Unit tests check the code does what the docstring says; the mutation harness
checks the tests would notice if it stopped. A quantity that is computed exactly
as described, described exactly as implemented, and named something the
implementation does not earn passes every one of those layers, because they all
agree with each other. **For each derived quantity, write down the property that
makes it that quantity and test that property directly** — in a form that does
not mention how it is computed.

> *Calibration.* `surprise` was the margin between the best score and the
> arriving token's. The code computed a margin, the docstring said margin, the
> tests checked margin, and 67 mutations passed. It was published as a finding —
> that the salience gate promotes filler exclusively — and the finding was an
> artefact. **John caught it by asking why a repeating pattern was not becoming
> less surprising.** The measure grew 266% across eight repeats of one identical
> cycle. No test in this repository was positioned to notice, because none of
> them was about the meaning of the word.
>
> The first meaning test written afterwards **asserted the wrong meaning** — that
> surprise must be unchanged when every score is scaled, which is false, since
> scaling scores is a temperature change. It failed on its first run and was
> replaced by the true property: surprise depends on the whole prediction, not
> only on the best score and the arriving one. So the discipline is a better
> class of check than the ones around it, **not a guarantee** — and a meaning
> test that passes on the first run deserves suspicion, not satisfaction.

**Search for prior art when the requirements are written, not when the code
is.** A list of properties a task or mechanism must have is a search query. Run
it before building, because the version in the literature is better specified
than the one derived at a desk, and because discovering it afterwards costs the
build twice — once to write and once to reconcile.

> *Calibration.* Three times, each cheaper to catch than the last.
> [Note 010](docs/archive/notes/010-tagging-and-capture.md): tagging and capture read
> properly only after the mechanism was half-built, and the reading changed it.
> [Note 020](docs/archive/notes/020-the-capacity-equation-checked.md): `SNR = sqrt(d/N)`
> derived empirically and checked against an analytic bound many sweeps later,
> which agreed *and* named a term never varied. `reward_recall`: built from note
> 017's five-point requirements list, which turns out to describe **bsuite's
> Memory Length test** — a T-maze parameterised by length, testing how many steps
> an agent can hold one bit. The list was a search query and was not used as one.

**An experiment is a sweep even when it is called a control.** Sweeps go to
Actions; the test suite and targeted mutation runs stay local because they are
seconds. The line is not "is it a sweep" — it is **how long does it hold the
machine** — and the way it gets crossed is by calling something a control, a
probe or a quick check and skipping the costing that a sweep would have had.

> *Calibration.* `g8_05_which_advantage.py` ran locally for **over ten minutes**
> and was described, in the same breath, as "a quick control at one cell before
> the full run". It was an experiment: three arms, three half-lives, three seeds,
> a trained model per cell. Nothing about it needed the local machine, it had no
> costing, and John caught it rather than the rule. Two of the three
> pre-dispatch controls before it were seconds and belonged where they ran; this
> one was not and did not.

**Commit messages go through `-F <file>`, and the FILE is built by the Write
tool or a quoted heredoc — never by `printf`, never by an unquoted heredoc.**

The hazard was never `-m`. It is letting a shell interpret the text at all, and
`printf`'s format string is the same class of interpreter as double-quote
expansion. Backticks inside a
double-quoted shell argument are command substitution. A message containing
`` `none` `` runs `none`, prints "command not found" to the terminal, and commits
the sentence with the word silently deleted — so the shell edits the permanent
record and the only symptom is an error about something you never ran.

> *Calibration, the same failure through a different interpreter.* Commit
> `6d72e11` was written with `printf ... > file` and `git commit -F file`, which
> obeys the rule as it was previously worded. Its message contains
> `80ms/20ms/2%`, `printf` read the `%` as a format specifier, hit the `+` of a
> following `+/-`, called it an invalid format character and **stopped after
> writing everything before it**. 549 bytes of about 2,500 were committed. Every
> prediction scored, the corrected figure and a criticism of the grid's own range
> were lost from the permanent record.
>
> `printf` exited non-zero and `git commit` ran anyway, so the only symptom was a
> warning line above a successful commit. **A rule that names one mechanism does
> not protect against the class**, which is the second time that sentence has had
> to be written about this one rule.

> *Calibration, and this one is not a gap in the rule.* Commit `28e0ae7` was
> written with `-m` and a double-quoted message containing `` `norm` `` and
> `` `value_lr` ``. Both ran as commands, both printed "command not found", and
> both words are **missing from the permanent record** — "reading of  at
> d'=1.01", "This gives  -- already in the model". The paragraph above predicts
> this outcome exactly, in those words.
>
> The rule was not unclear; it was **skipped because the message felt short**.
> Four commits that session went through `-F` with a Write-built file and the
> shorter ones drifted to `-m`. **There is no length below which `-m` is safe** —
> the hazard is one backtick, and short messages are not less likely to contain
> one. `DECISIONS.md` carried the correct text, which is the only reason this
> cost nothing.

> **AND THE RULE IS NOW A CHECK, because four calibrations of one rule is evidence
> that more care is not available.** `tools/check_commit_messages.py` scans recent
> messages for the *symptom* rather than the process, so it does not care how the
> message was produced: **a word vanishing leaves its spaces behind.** Run over 400
> commits it found nine hits — four genuine mangled messages, of which **two were not
> in the list below**, and five later commits that quote the damage.
>
> The rule was believed to have failed four times. It had failed **six**. Rule 18's
> instruction — prefer a rule that makes the mistake structurally impossible — with
> the evidence that the warnings were not working being the warnings themselves.
>
> **And it caught a SEVENTH four commits after it was written.** `6a50139f` reads
> "The  metric does NOT transfer" — `` `alone` `` eaten by a double-quoted `-m`, on
> master, in the same session that added the checker. So the check is doing the job
> the five warnings above it could not, and the drift back to `-m` happens even with
> the rule freshly in mind. **The reasoning survived** because it also lives in
> `experiments/g5_01_scaling.py`, which is the general mitigation: a commit message
> is the only home for nothing.

> **It is a net, not a wall, and `-F` stays.** A word eaten at the end of a line
> leaves no double space, and a `printf` truncation removes everything after the `%`
> and leaves no signature at all — which is how commit `6d72e11` lost 2,000 of 2,500
> bytes.

> *Calibration.* Commit `18388e5`. The line "PREDICTION 3 REFUTED BACKWARDS.
> `none` was predicted to rise" was committed as "PREDICTION 3 REFUTED BACKWARDS.
>  was predicted to rise". Every other commit that day used `-F` and was fine;
> this one used `-m` because the message felt short enough to inline. **The rule
> that only applies when the content looks risky is a rule that fails on the
> content you did not think was risky.** This is the same failure as the heredoc
> rule and belongs next to it.

**A caveat printed next to a number does not attach to the number.** If a value
is a *bound* — a crossing that sat at the edge of the grid, a run that hit its
budget — the code has to refuse to use it as a value. Annotating it and computing
through it produces a figure that looks measured and is not.

> *Calibration.* Twice, one sweep apart, in the same tool.
> [g5-01](experiments/sweeps/g5-01-does-scale-help.txt) fitted a scaling exponent
> to the sequence lengths that crossed the bar and silently dropped the one that
> did not — the most informative point in the sweep, since it said the
> requirement had run off the end of the grid. That was fixed by making the tool
> extrapolate its own fit to every missing point. Then
> [g5-02](experiments/sweeps/g5-02-how-finely-can-it-split.txt) printed *"AT THE
> EDGE OF THE GRID, breaking point not located"* against two of its three rows
> and fitted an exponent through them anyway, reporting `seq_len^1.00` for a
> quantity the grid bounds only to `[0.00, 1.00]`. **The first fix did not
> generalise because it was written against the specific shape of the first
> mistake.**

**A local timing measured on one seed does not convert to runner time by a
factor anyone has guessed.** `ubuntu-latest` has 2 vCPU. A job running
`--workers 2` gives each seed ONE core, while the same code timed locally had
the whole machine and numpy's threading. Measure the ratio, do not assume it.

> *Calibration.* g11-06's `matched` arm, estimated from a local single-seed
> timing with "a 3x allowance for a slower hosted runner":
>
>     chars      estimated wall     actual wall
>     62,500              ~2 min       18.4 min
>     125,000             ~5 min       29.1 min
>     250,000             ~9 min       53.9 min
>     1,000,000          ~39 min      ~190 min (extrapolated)
>
> **The real factor is about 7.6x, not 3x**, and the 1,000,000 cell came within
> sight of a 300-minute timeout that was set assuming 39 minutes. Two of the
> three previous cost mistakes in this repo were timeouts (g11-03 lost four of
> six cells) and one was the cap introduced to prevent one (g11-04). Use ~8x
> local single-seed time for a 2-worker job on `ubuntu-latest` until something
> better is measured.

**Before fitting a scaling exponent, probe the BOTTOM of the range and confirm
the arm is still moving there.** A control that cannot fire and an arm that has
already converged are the same defect — the grid does not contain the phenomenon
— and both produce a flat line that looks measured.

> *Calibration.* Three consecutive sweeps, and the rule as previously written
> caught only the first. [g11-04](experiments/sweeps/g11-04-does-our-loss-fall-with-width-like-backprops.txt)
> lost its CONTROL: the backprop baseline was data-limited at the capped corpus
> and fitted `b = -0.0021`, R² 0.13. g11-05 was re-scoped onto a data axis, the
> control fired — and then its ARMS turned out to have converged at **16,000
> characters**, while the sweep's smallest point was 62,500. Five points,
> fifteen jobs, every one of them past saturation, so a flat exponent was
> guaranteed by the grid. g11-06 was dispatched with the same lower bound before
> this was known.
>
> **The probe that would have caught it costs three minutes locally**: run the
> arm at 4k, 8k, 16k, 32k characters and look at where it stops moving. Against
> roughly two hours of runner time per matrix, and it produced a better sentence
> than the sweep did — *this model extracts everything it can from sixteen
> thousand characters, and sixty times more text adds nothing* — where the sweep
> produced `b = -0.0010`.

**And a sweep that does not contain its own answer has not swept.** If every arm
chooses a value at an *edge* of the grid, the optimum lies outside it, every arm
is under-tuned, and the rule above was satisfied while the number stayed
provisional. Check it mechanically — `tools/grid.py`.

> *Calibration.* [g4-01](experiments/sweeps/g4-01-no-global-readout.txt) swept the
> learning rate on all four arms, as required, over `{0.02, 0.05, 0.1}`. The
> interior value was chosen **zero times in twenty-four** arm-choices: every row
> pinned at an edge. Worse, the write-up called two of those rows healthy
> *because their arms disagreed* — but they disagreed by sitting at opposite
> edges, which is a grid too narrow in both directions rather than a grid working.
> The count went from "four of six" to "six of six" only once the check existed as
> code. **Printing a diagnostic is not a check; it is a hope that someone reads
> the line correctly — and here the person who wrote the line was the one who
> misread it.**

**3. Reproduce before you believe.** One green run, one fast benchmark, one
successful manual click is an anecdote. Anything that will be acted on gets
repeated — enough times to separate the effect from the noise, and on input you
did not choose to make it pass.

> *Calibration, and the useful part is WHAT made it visible.* Three runs of
> `g23-02` at identical seeds returned **0.3641, 0.3680 and 0.3583**. A stochastic
> mechanism drifting by 0.01 between runs reads as seed noise, and the reported
> `±0.0065` absorbed it completely.
>
> **The deterministic arm is what exposed it.** A counting baseline was added to
> answer a different question — does the objective beat plain counting — and it
> moved too: 0.2544 / 0.2485 / 0.2534. Counting has no randomness beyond the split,
> so a varying counted arm meant **the split itself was moving**. `graph_rules`
> iterated a `set` of relation strings under Python's per-process hash
> randomisation, so tie-breaks in *"the commonest r3 for this pair"* resolved
> differently, changing the rules and therefore the holdout.
>
> **So the habit is not "sort your sets".** It is that **a deterministic opponent
> is a reproducibility check on the whole harness**, and it costs nothing. Two
> published figures had to be superseded because it was added late rather than
> first.

**4. Report negative results as results, in the OPTION RECORD for the thing that
was tried.** A refutation that narrows the search is worth more than an unmeasured
success, and it is the only thing that stops the same dead end being explored
twice — so it goes where someone about to propose the mechanism will be standing,
which is `docs/options/<name>.md` and the tree's row above it.

**And the prediction goes in the artifact that PRODUCES the number, written before
the run.** A record is an event log written afterwards, so an entry cannot carry a
commitment — by the time it exists the answer is known. A sweep record can, and
`tools/check_rails.py` requires every sweep file to carry `PREDICTIONS`. For a
local probe with no sweep, put a `PREDICTED` line in the option-record entry and
**commit it before running**, so git ordering is the evidence. That is weaker than
a separate artifact and should be described as weaker.

> *Calibration, and it retired the note.* **Investigation notes are archived** —
> `docs/archive/notes/`, all 105 of them, 2026-07-30. They were kept as the home
> for predictions, and they were not doing that job: **89 of 91 sweep records carry
> a `PREDICTIONS` section under an enforced rail, against 18 of 105 notes**, several
> of which are requirements documents rather than experiments. So the rule was
> describing a discipline that lived somewhere else.
>
> What they were was a **chronological log**, which this project has already
> measured the cost of once at 6,040 lines. The bill arrived the same way:
> `tools/check_provenance.py` found seven citations that did not resolve, including
> a real measurement two notes attributed to two different sources, neither holding
> it ([note 105](docs/archive/notes/105-the-partitioning-accuracy-figure-has-no-source.md)).
> **A document organised by WHEN cannot be consulted by someone who only knows
> WHAT.**

**5. Correct the record when an observation refutes a written claim.**
Falsified claims get fixed, not softened. If the README says the system does X
and it does not do X, the README changes — it does not become "the system aims
to do X".

> *Calibration.* — unfilled. Record the first documented claim that measurement
> contradicted, and what the document says now.

---

## Building

**6. Ship the connection test with the mechanism.** Not a test that it runs
without raising — a test that its *input reaches its output*. Perturb the
input; assert the output moves. This is the single highest-value test you will
write, because it is the only one that catches the failure mode named above.

Ask it of every seam: does this config value change behaviour if I change it?
Does this cache return something different when populated? Does this parameter
appear in the result?

> *Calibration.* The MQAR generator's filler drew from the whole key range, so a
> filler token could be byte-identical to a query token while requiring a
> different output. The mechanism *appeared* to be creating difficulty —
> distracting material a model has to learn to discard — and was actually
> creating **impossibility**: no model could have told the two apart, and the
> benchmark would have pinned everything at the base rate for a reason having
> nothing to do with recall. Found in the **first sequence ever generated**, by
> printing it and reading it, before any test existed. Age: minutes, because it
> was looked at. Had it not been, every G0 number would have been a measurement
> of an impossible task, and the flatness would have looked like a result.
> `test_a_used_key_never_appears_as_filler` now guards it and
> `filler-collides-with-keys` in `tools/mutate.py` confirms that guard bites.

**7. Distrust any criterion that cancels its own input.** If a decision divides
by, normalises against, or subtracts something that moves *with* the variable it
is meant to respond to, it is blind by construction and will look fine. Ask this
of every ratio, threshold-relative-to-baseline, and percentage-change gate,
explicitly, when you write it.

> *Calibration.* — unfilled. Record the first criterion that cancelled what was
> supposed to drive it — and which direction it ended up pointing.

**8. Measure at the granularity of the decision.** A statistic collected over
one kind of object does not describe an object of another kind. A per-request
average does not describe a user's session. A distribution over all rows does
not describe the one row being scored. Whenever a number crosses from where it
was gathered to where it is used, say out loud what it is a statistic *of*.

Watch especially for accumulators reporting their own initial value: any running
average, EMA, or rolling window must either carry a sample count or be
explicitly named as something other than a measurement.

> *Calibration.* — unfilled. Record the first statistic used at the wrong
> granularity: what it read, and what the correct-granularity value was.

**9. One implementation per behaviour, behind the smallest surface that does
the job.** Two halves of one habit, and both of them are cheaper here than the
usual arguments for them suggest.

**Do not duplicate logic.** Rule 13 says a rationale lives in exactly one place;
this is the same commitment for code. The ordinary cost of duplication is that
a change has to be made twice. The cost *here* is worse and is a direct
consequence of rule 12: when a bug is fixed in one copy and not the other, the
surviving copy keeps producing plausible numbers, and every measurement taken
through it is invalid while looking exactly like the corrected ones. A
duplicated code path is a fix that did not land, wearing the appearance of one
that did.

So: extract the shared thing rather than parallelising it. If two call sites
genuinely need to differ, make the difference a parameter and name it, so that
the divergence is visible in one place instead of implied by two files drifting.
Copying a block is occasionally right — when the two are about to diverge for
real reasons — and then it is worth a comment saying so, because otherwise the
next reader will helpfully merge them back.

**Keep the public surface minimal.** Every public type, method and field is a
promise, and rule 15 makes that literal: a doc comment states the contract a
caller may rely on. Default to the narrowest thing that works — private until
something outside genuinely needs it, one obvious way to do each thing, no
parameter that exists only because it was easy to pass through, no accessor
exposing internal state so a test can reach it.

This is not tidiness. **A small surface is what makes the internals replaceable,
and this project will need to replace internals repeatedly** — the gate ladder
in `GOALS.md` is a plan for finding out that things are wrong. A mechanism whose
guts can be rewritten without touching a caller can be refuted cheaply. One with
a wide surface has to be argued about instead, and mechanisms that are expensive
to remove are the ones that stay in past their evidence.

> *Calibration.* — unfilled. Record the first bug that had to be fixed in more
> than one place, and whether every copy was found the first time. Then record
> the first mechanism that was kept longer than its evidence justified because
> removing it meant changing its callers.

---

## Tests

**10. A test must fail when the thing it names is broken — verify that it
does.** Passing is not evidence; a test has never demonstrated anything until
you have seen it go red for the right reason. Break the mechanism deliberately,
confirm the test notices, then put it back. Automate this where you can, so a
new mechanism arrives with a check that its test is real.

Watch particularly for an assertion on a quantity that something *else* pins,
and for bounds so wide they admit the broken case.

> *Calibration.* — unfilled. Record the first test that survived deliberate
> breakage: what it asserted, and what the value actually was.

**The same applies to any experimental condition or A/B arm.** Before running
it, ask what outcome would *refute* the prediction attached to it. If the
predicted outcome is guaranteed by how the condition is built, it is not
evidence however it comes out — and it will read as confirmation.

**A test that something did NOT change needs a companion asserting that
something DID.** An unchanged-assertion passes whenever the mechanism is
disconnected, which is precisely the case it exists to catch.

> *Calibration.* The C1 locality test for the composed readout perturbed group
> 1's output weights and asserted group 0's hidden layer did not move. It
> passed — and it passed because the fixture zeroes `wo`, so multiplying group
> 1's weights by three multiplied **zero** by three and nothing moved anywhere.
> The companion assertion, that the perturbed group DID move, is what caught it.
> Every locality, isolation or independence test in this repository has this
> shape and needs the pair.

**11. A failing test is a claim about the production code until shown
otherwise.** Fix the code so the assertion holds. Widening a bound, deleting an
assertion, or special-casing the input converts a caught bug into a silent one
*and* destroys the evidence that it existed.

Changing the test is right when the intended behaviour genuinely changed. Then
say which decision changed it, and **split rather than loosen** — keep an
assertion for the old path where it still applies, add one for the new.

A test that passes while *vacuous* is the opposite problem: there the test is
what is wrong, and strengthening it is the fix. Rule 10 covers those.

> *Calibration.* — unfilled. Record the first assertion that was loosened
> instead of investigated, and what it was later found to have been hiding.

---

## Keeping the record straight

**11b. Fetch into a fresh directory, never suppress the fetcher's errors, and
verify a run's identity FROM THE DATA before reading a number off it.**

This cost a near-miss of the worst available kind. Re-reading g9-11's artifacts
with

    gh run download <id> -D "$SCRATCH/g911" >/dev/null 2>&1

landed on a directory that already held a DIFFERENT run's artifacts from an
earlier cycle. Artifact names repeat between runs of the same workflow, so `gh`
failed with "file exists", `2>&1` swallowed it, and the analysis ran cleanly on
the wrong run. It produced a coherent story: a fabricated results table, a
tripwire recorded backwards, a bad calibration propagated into this file.

All of it was wrong. A pristine re-download confirmed the published numbers to
three decimals.

**Stale data is worse than missing data, because it analyses cleanly.** A failed
download announces itself; a silently skipped one does not, and everything
downstream looks like a finding.

The check that costs nothing: every sweep record carries a `condition` string
written by the script from the parameters it actually ran with. Assert on that
before reading anything. The workflow file says what SHOULD have run and the
directory name says what you MEANT to fetch; only the data says what happened.

**And the failure mode to fear here is retraction, not error.** Publishing a
wrong number is recoverable. Retracting a correct finding destroys a real result
and the record of how it was reached, and it does so with all the outward
appearance of rigour.


**12. A bug fix is not finished when the tests pass.** It is finished when the
audit file records what the fix invalidated, and each affected decision is
marked re-validated, superseded, or pending. A fix does not only correct the
future; it removes the evidence under choices already made, and those choices
stay in force because **nothing in a default value points back at the run that
chose it.**

Sort before assuming the worst — work out which past results the broken code
path could actually have touched, and say so. Be equally careful in the other
direction: a direction abandoned because it "did not help" may have been tested
through a broken mechanism. **Discarding a good idea on an invalid measurement
is the most expensive error available.**

**Changing a default invalidates the comparison set** — the same rule applied to
a parameter rather than a bug. A known-better setting can be worth deliberately
*not* adopting until you are ready to re-baseline: list the results that stop
being comparable, and re-run the ones that still matter.

**Then fix the class, not the instance.** Before closing a bug, ask what *kind*
of mistake it is, enumerate the other places that kind could live, and write the
check over the enumeration rather than over the one case — so the next instance
fails the suite instead of waiting to be noticed.

> *Calibration.* — unfilled. Record the first mistake that recurred: how many
> times it was fixed as a one-off before anyone swept for the class.

**13. Put the reasoning where the reader will be standing.** Someone about to
change a threshold reads the test that guards it, not the design note. To keep
one rationale from drifting across five files:

| where | what belongs there |
|---|---|
| Code comment | Why *this line* is this way. Short, and only where it would otherwise read as arbitrary. Local, single-call-site gotchas belong here rather than in a doc. |
| Doc comment / `<summary>` | The contract the caller can rely on. See rule 15. |
| Scope-local `ARCHITECTURE.md` | Stable, cross-cutting understanding of this area. See rule 14. |
| Option record | What was tried, the configuration it was tried in, and what came back. **Events only, no status** — `docs/options/README.md` is the format and `tools/check_options.py` enforces it. |
| Sweep record | The prediction, written before dispatch, and the cost. `tools/check_rails.py` requires both. |
| `DECISIONS.md` row | Which option a component IS, with the citation and the revival condition. One or two lines. |
| Explainer | The same finding for someone who does not work in this field. See rule 14. |
| Audit file | What a later fix invalidated, and which decisions still rest on it. |

**There is no "investigation note" row any more.** Notes are archived — see rule 4's
calibration — and their three jobs split cleanly: what was tried goes to the option
record, the prediction goes to the sweep record, and the readable version goes to an
explainer. **A finding about the METHOD rather than about an option goes in this file, as
a calibration.** Nothing needs a fourth home, and a fourth home is how the last one
started.

When a number appears in more than one, **the test docstring is canonical** — it
is the one under continuous execution. What breaks if the assertion stops
holding, with the concrete number from when it did, belongs there; that is where
the history lives.

**And every open item and sweep record opens in plain language.** One short
paragraph, headed `IN PLAIN TERMS`, before the technical body: what is being
asked, why anyone should care, and what a yes or a no would mean. No jargon, no
numbers that need prior context to parse.

This is not a courtesy. Anyone picking this repo up — including you after a
break, and including an assistant after a context reset — needs to reconstruct
*what question is live* without reading twenty files in order. Write it before
the prediction, not after the result: if the plain-language version cannot be
written without the answer in hand, the question is not sharp enough to run yet.

---

## Documentation and planning

**14. Externalize hard-won understanding, into the nearest doc.** When effort
went into figuring out a non-obvious behaviour, capture it so it does not have
to be re-derived — and so it stops living only in someone's head or a chat log.
**The trigger is not only shipped code.** Discoveries and corrections made while
investigating or debugging count, and are the most valuable kind.

Three constraints keep it from bloating into something nobody reads:

- **Scope-local only.** It goes in the `ARCHITECTURE.md` nearest the code, never
  ballooning a higher-level one — only the relevant doc is loaded for a given
  task, and a top-level doc that knows everything is a top-level doc that is
  always stale.
- **Navigation, not catalogue.** No volatile lists of class, queue, or method
  names; no exact counts written as though they were invariants. Document what
  is stable and cross-cutting.
- **A doc update is a change like any other.** Surface it for review. Do not
  rewrite silently.

> *Calibration.* — unfilled. Record the first behaviour that had to be
> re-derived from scratch because nobody wrote it down the first time.

**And externalize it for the non-specialist too.** Every concept the project
introduces gets a plain-language explainer in `docs/explainers/`, written for
someone who does not work in this field: no jargon without a definition, no
number that needs another document to interpret, short enough to read on a
phone. Rule 13's `IN PLAIN TERMS` paragraph keeps an individual note readable;
this keeps the *project* readable.

This is not outreach, and it is not a courtesy. **A project its owner cannot
follow is a project where nobody can tell it that it is wrong.** Every safeguard
in this document depends on a claim being challengeable, and a claim nobody can
read cannot be challenged — so the explainers are load-bearing for the method,
not decoration on top of it. They are also the cheapest available check on
one's own understanding: an idea that cannot be explained without jargon is
usually an idea that is not yet understood.

An explainer that stops making sense is a defect in the explainer. Fix it there
rather than expecting the reader to work harder.

**14b. Two top-level documents. `DECISIONS.md` is a TREE, not a log.** A document
that carries intent *and* results *and* a todo list goes stale in all three at
once, and a reader cannot tell which part is which.

| document | holds | never holds |
|---|---|---|
| `DECISIONS.md` | every component, its options, which option each is, and the attempts under each | a chronological entry. **New findings update an option's state and its attempt list** |
| `GOALS.md` | intent, the constraints, the gate ladder, what would refute the project | any measurement. The only numbers permitted are arithmetic or inherited from the predecessor, and both are labelled |

**This replaced a three-document structure on 2026-07-29, and the reason is the
calibration below.** `DECISIONS.md` was an append-only log of 6,040 lines. Nothing
could read it whole, so it was read selectively — and that is not a discipline
failure, it is what happens when a reference outgrows a context window.

> *Calibration, and it is the most expensive one in this file.* Decision 115 closed
> saturation explicitly — *"saturation is not an open problem and should stop being
> treated as one"* — and eliminated store capacity, readout capacity and persistent
> representation **by name**. Note 042 then built an architecture case on the same
> wall; decision 133 ran its falsifier, was refuted, and **relabelled the wall a
> "capacity limit"**; decision 134 superseded 133's follow-on one entry later. On
> 2026-07-29 all three produced three wrong recommendations in a row.
>
> Every entry was individually reasonable. The log's own Index stopped being
> maintained at entry 134 and entry 132 had an Index row with no body. **A log
> records; it does not prevent** — and "do not re-propose these" cannot catch a
> re-label of a null after the fact, which is what 133 did.

Four rules keep the tree from collapsing back into a log:

- **A finding updates an option, it does not append an entry.** If you are writing
  `## 172.`, stop: the finding belongs on an option's state or in its attempt list.
  `tests/test_goals_consistency.py` asserts this.
- **Every ✅ and ❌ cites a decision, a sweep or a note — or says in words that it
  rests on no measurement.** This is the archived ledger's rule (*a row with no
  measurement is UNTESTED, never "probably fine"*) and `check_decisions.py`
  enforces it. A ❌ with no citation is a mechanism refused by opinion, and
  discarding a good idea on an invalid measurement is the most expensive error
  available.
- **Every attempt carries the configuration it was measured in.** A refutation is
  conditional on a config; decision 74 cost a comparison set by forgetting it.
- **🔀 is a valid END state.** Two options kept behind a switch and re-tested as
  the system changes is not indecision — refutations expire, and 107 and 111 both
  became right later when their inputs moved.

Superseded documents go to `docs/archive/` with a header saying what replaced
them, rather than being deleted — the retractions in them are usually the useful
part, and every attempt in the tree cites the archived log by entry number.

**The capability ledger stays archived, decided 2026-07-29.** Folding
`ARCHITECTURE.md`'s 24 rows back in was considered and declined: the tree already
carries what they asserted, distributed across the components they belonged to, and
**two documents describing components is the drift this restructure exists to end.**
What was worth keeping was its RULE, and `check_decisions.py` enforces it — a state
with no measurement is UNTRIED, never "probably fine" — along with its census check,
which caught its own summary counts the first time a verdict changed. The gate ladder
in `GOALS.md §4` is untouched and remains the only place a gate verdict is written.

**And prefer MORE documents to LONGER ones.** John's instruction, 2026-07-28:
keep each note small and add another rather than growing one. A note that grows
is a note nobody re-reads, and the growth is always the same shape — a result
table copied out of the sweep record it already lives in.

The concrete rule: **the tree carries the CLAIM and links the table.** A
measurement belongs to exactly one file, which is its sweep record; anywhere else
quotes the one line that is currently load-bearing and links the rest. Within one
session the old `STATE.md` went 21 KB → 36 KB entirely by duplicating tables that
already existed two directories away.

> *Calibration.* On 2026-07-28 the four top-level documents totalled **503,000
> characters**, of which `DECISIONS.md` was 318,000. `GOALS.md` opened with
> *"nothing below is a measurement"* and closed with 405 lines of running
> results, carrying `T^0.67` as the live answer for minimum machine width while
> quoting `T^0.82` for the same quantity two paragraphs later, with the
> consequences still computed from the older figure. `HANDOFF.md` carried
> *"prequential 4.540 ... unigram BEATEN"* as the project's headline text result
> for weeks; decision 118 established it was an offline backprop probe on frozen
> features, was not prequential, and was not the model.
>
> **The failure mode is not size, it is a stale claim wearing a current
> document's authority.** Both errors above were found by reading the source
> rather than by any test, and both had propagated into work that was planned
> around them.

**14c. A refuted alternative stays in the tree, behind its own switch.** John's
instruction, 2026-07-28: *"any time you build multiple potential solutions for a
given component, keep them around to swap between down the road if needed."*

This is already the practice and it is worth naming so it survives a tidy-up:
`keys.py` holds `TableKeys` beside `PairKeys`; `retrieval.py` holds
`SuperposedRead`, `ExactCache` and `SettlingRead`; `hop_accumulate` keeps `bind`
*"as the measured alternative, not as a fallback"*; `search_branches` keeps 0, 1
and n as off, greedy and branching.

**The reason is that refutations expire.** Decision 107 declined a traversal and
decision 111 declined search, both on correct arithmetic, and both conditions
were measured away later — the mechanisms became right when their inputs moved.
A deleted alternative cannot be re-measured when that happens; a switched-off one
costs a config field.

Two things keep it from becoming clutter:

- **The default is the measured winner**, and every alternative is off by
  default, so no earlier result changes when one is added.
- **A kept alternative carries WHY it lost**, at its definition, with the
  decision number. An option nobody can date is an option nobody can retire.

**15. Document the contract, not the implementation.** A doc comment says what
a caller can rely on. A good one stays true after the internals are rewritten;
if a rewrite falsifies it, it was describing implementation. Put one on every
public type and method — which is cheap, because rule 9 keeps that set small.

**No ticket numbers in code comments.** They add nothing for a reader of the
code, and the tracker outlives neither the code nor its own schema. The same
default extends to notes and design files — unless the reference is genuinely
the useful anchor, such as naming the change a note is tracking.

**16. A plan opens with the problem, and reads at three zoom levels.** Purpose
first, structure follows.

- **Standalone summary first.** Plain language, readable by someone with zero
  knowledge of the codebase, before any technical body.
- **Three deliberate zoom levels** — mental model, scope at a glance, full
  detail — and a reader must never be forced down a level to understand the one
  above. Split the high-level story from deep per-change detail rather than
  interleaving them. Cut context the audience already has. Keep only diagrams
  that are load-bearing.
- **Standalone voice.** Impersonal, present tense, declarative: "This change
  adds X." "Chosen: X. Rejected: Y, because Z." It is the artifact, not meeting
  minutes — no references to the conversation or to who produced it.

A plan is the *input* to an implementation plan, not the full spec itself.

---

## Keeping the work pointed at the goal

**17. Alternate between verifying and building.** These standards reward
verification, and that is a real hazard rather than a virtue. Every audit yields
a satisfying, recordable, provably-correct result; a new mechanism most likely
yields a null. There is a gradient here, it points away from the goal, and
following it feels like productive work the whole time.

So, concretely:

- **After a block of verification work, the next block builds something** — even
  if it is likely to fail. A null from a new mechanism is worth more than a
  fifth confirmed audit.
- **Never open a new investigation while an existing one still has work in it.**
  This replaces a flat cap of two, which John retired once the runs got long
  enough that the cap was mostly enforcing idleness. The queue is still where
  creep accumulates, so the test is no longer "how many are open" but **"is
  there anything I could be doing on something already open?"** If yes, do that.
  If everything open is genuinely blocked — on a sweep, on a build, on a
  decision only John can make — starting something new beats waiting, and
  carries no urgency with it.
- **Waiting is not a state this project has any use for.** Long runs go to the
  background and something else gets built while they run. `tools/mutate.py` is
  seven minutes of nothing; a sweep is hours of it.
- **Retire a condition in the same change that adds it.**
- **Order by BLOCKING, then by HARD — never by what is ready.** John's instruction,
  2026-07-30. This is the same gradient as the bullet above, seen from the queue
  rather than from the day: at any moment the easiest available task is the one
  that feels most productive, and it is almost never the one the goal is waiting
  on. Take the thing that unblocks something else first; among the rest, take the
  hardest. A session that ends having cleared five easy items and left the blocker
  untouched has gone backwards.
- **When in doubt, ask what would move the goal**, not what would make the
  record more accurate.
- **A measurement revised twice is no longer the bottleneck.** Stop measuring it
  and go build the thing it was measuring. Three revisions is a signal about the
  *measurement* — that it is harder to make fairly than it looks — not about the
  quantity being measured. Publish the bound, name the caveat as permanent, and
  move.
- **Scaffolding that is not labelled scaffolding becomes load-bearing.** A
  component that exists only because the benchmark needs it gets named as such
  in the commit that adds it, and checked against the project's own constraints
  *before* anything is measured on top of it.

> *Calibration.* Nineteen commits, from `8e1393c` to `a236ab9`: **5,082 lines
> added, 55 of them in `openplexus/`.** One percent. The rest was sweep notes,
> explainers and experiment scripts — five successive measurements of a single
> ratio, which was corrected four times, every correction moving the same way.
>
> Every individual round cleared this bar. Each had a prediction written first, a
> confound that was real, a correction that was necessary, and a build step
> somewhere in it. **The rule as originally written is local, and a local rule
> cannot see a run of five.** That is why the two bullets above are counters
> rather than principles.
>
> The deeper cost is what went unbuilt. The model carries a single global readout
> because MQAR asks for one answer per query, and that readout sums across every
> dimension — which, once the memory is split across machines, is exactly the
> globally synchronised step C1 forbids. So four gates were passed and five
> sweeps run on a model that violates this project's first constraint, and it
> surfaced in a footnote to the bandwidth arithmetic
> ([note 009](docs/archive/notes/009-splitting-the-memory.md) §4). Rigour on the wrong
> question is still the wrong question.

---

## Adding to this document

**18. Write the standard, not the incident.** A rule earns its place by telling
someone who was not there what to do next time. So:

- **Lead with the commitment.** "Statistics are gathered at the granularity of
  the decision" — not "we once averaged the wrong thing."
- **Attach the calibration, keep it subordinate.** The number is what makes a
  rule enforceable; put it in the aside, not the headline.
- **Prefer a rule that makes the mistake structurally impossible** over one that
  asks for more care. An automated check is worth more than a rule saying "write
  good assertions." If a proposed rule cannot be turned into a check, say so
  plainly rather than pretending vigilance will hold.
- **Assume good faith and real constraints.** These standards exist because the
  work is genuinely hard, not because anyone was careless. A document that reads
  as a list of accusations gets defended against; one that reads as a bar worth
  clearing gets upheld.
- **Retire a rule when it stops paying.** A standard nobody applies is worse
  than no standard, because it makes the others look optional.

---

**19. Search before you build, and leave the tree smaller than you found it.**
Rule 9 forbids duplicating logic. This is the half that makes rule 9 reachable,
because **you cannot avoid duplicating what you did not know existed** — and the
failure mode is not carelessness, it is that a plausible name for a thing is not
the name it has.

So, before writing a mechanism: grep for what it does, not for what you would
have called it. Search the tests too — a test named for a behaviour is evidence
the behaviour is implemented somewhere, and it names the module.

**A NEGATIVE SEARCH RESULT IS NOT A FINDING UNTIL IT WAS A WIDE ONE.** This is
rule 1's *"a reference that does not resolve is UNCHECKED, not absent"* applied
to code, and it is the half that keeps failing. Searching one plausible
directory and concluding is the same error as searching the live tree and
concluding a note was never written. **Search every place work lives** —
`openplexus/`, `tools/`, `tests/`, `testbed/`, `experiments/` and
`docs/archive/` — and search by capability, not by the directory you expect it
in.

> *Calibration, 2026-07-30, and it is a near-miss rather than a cost.* Before
> building a peer-path latency measurement I searched `.github/workflows/` for
> `cluster|container|netem`, got nothing, and told John *"no workflow runs the
> container harness at all."* True, and useless: `tc netem` has been run
> **repeatedly** — sweeps `g12-01`, `g12-03`, `g12-04`, notes 014 and 086,
> `tests/test_impairment.py` — from `testbed/run.py`, which is a directory I
> never looked in and which says in its own docstring that it is verified on
> Docker Desktop *and* on Actions runners.
>
> **John caught it, not the search.** Nothing was duplicated, and only because
> notes 094 and 101 each say in their own words that the harness *"has never
> been pointed at the peer path"* — so the gap was documented twice by someone
> who had done the wide search already. **The rule held by inheritance, not by
> being followed**, and the correction is cheap: the right target turned out to
> be `testbed/run.py`, which already runs on CI, rather than the generator I was
> about to extend.

**AND EVERY NEW MODULE SAYS WHAT IT DOES NOT DUPLICATE**, in its own docstring,
naming what was searched. John's instruction, 2026-07-30: unchecked duplication
is a known failure mode of assistant-written code, and he asked for something
structural rather than another rule asking for care.

`tools/check_rails.py` R6 enforces it on `openplexus/` and `tools/`. It cannot
tell whether a search happened — what it can do is refuse a module that never
asked the question, and *"this does not duplicate X because Y"* cannot be written
without going to look at X.

> *Calibration.* **65 of 66 modules violated it the day it was written**, which is
> the honest measure of how much this was being done. They are baselined and the
> list can only shrink. The one that already complied is `tools/cluster_driver.py`,
> whose *"What this adds, and what it deliberately does NOT re-implement"* section
> is exactly why it did not repeat the `cluster_node.py` mistake its own
> predecessor made — so the practice was already here, in the one file that had
> been burned.
>
> **The first probe of the ratchet passed for the wrong reason** and is worth
> recording: a throwaway module written to prove the check bites was given the
> docstring *"a module that never says what it does not duplicate"*, which
> contains the marker phrase. It passed legitimately. A check verified with a
> test that cannot fail is rule 10's shape, arriving inside the tool built to
> enforce rule 19.

And **every commit carries some consolidation.** Not a separate cleanup pass
later: the moment to merge two things is while both are in your head, and a
refactor deferred is a refactor that gets argued about instead. If a commit adds
code and removes none, that is worth a sentence explaining why — usually it is
fine, occasionally it is the signal that something was rebuilt rather than found.

Deleting a duplicate is the same commit as noticing it. Extract the shared piece,
delete the loser, and keep the docstring from whichever explains itself better —
which is not always the one that survives.

> *Calibration.* — 2026-07-30. `tools/cluster_node.py` was written to run a node
> as a container. `openplexus/node_main.py` already did that, better: it sizes
> itself against cgroup limits via `deployment.plan`, and it exposes the decoder
> seeding as `OPENPLEXUS_DECODER` — the exact fix for a vacuous exactness check
> that the duplicate then rediscovered from scratch. `tests/test_node_main.py`
> named the module in its own filename. **Nothing was searched for, because
> "cluster node" felt like a new thing rather than the thing that exists.**
> John predicted it before it was found: *"I want to say tests have previously
> been done cross-container, so I assume there are configs for it somewhere."*
> He was right, and the duplicate is deleted.
>
> Record the next mechanism rebuilt rather than found, and whether a grep would
> have surfaced it.

---

## Conventions

**Python 3.14.** The task and measurement layer takes **no dependencies** — see
[note 007](docs/archive/notes/007-the-stack-and-the-first-code.md); a generator with no
library semantics to reason about is auditable line by line, and that layer is
the reference implementation everything else is asserted against.

**numpy 2.5.1 is installed** (approved 2026-07-25, when training a model from
scratch became the blocking step and pure Python stopped being reasonable). It
is for the *model* layer only. Anything in `openplexus/tasks/` or
`openplexus/baselines.py` importing it is a defect: those are the ruler, and the
ruler stays dependency-free. The consumer-device runtime remains undecided.

- **A scale-dependent choice goes in [`docs/SCALE.md`](docs/SCALE.md) WHEN IT IS
  MADE, and says so at its own definition.**

  Every measurement here is made at one size, and a default chosen at width 64
  carries no warning label when it is read at width 8192. The register records
  what was chosen, at what size, what would trigger revisiting it, and what to
  try instead.

  The config docstring carries the trigger too, not just the register — that is
  where someone reading the code will be. And the row is added when the choice
  is made rather than when it breaks, because a register written afterwards is a
  post-mortem.

  > *Why this exists.* `hop_accumulate="concat"` beat a true binding 1.000 to
  > 0.812 — but only because sixteen rules in a 128-wide space are linearly
  > separable whatever the labels do. That is a property of having few rules,
  > not of concatenation being right, and nothing in the result says so. John
  > asked for these to be swappable and documented rather than discovered later.

- **Run all these checks before every commit:**
  ```
  python tools/mutate.py --verify
  python -m unittest discover -s tests -t . -q
  python tools/check_workflows.py
  python tools/check_rails.py
  python tools/check_duplication.py
  python tools/check_decisions.py
  python tools/check_options.py
  python tools/check_provenance.py
  python tools/check_explainers.py
  ```

  > *Measured, and correcting a figure this document carried.* The comment in
  > `checks.yml` said "85 mutations at roughly fifteen seconds each. Sharded it
  > is about two minutes a job instead of twenty in one." The first full run to
  > complete — `57d8112`, which finished only because no push superseded it — was
  > **168 mutations across six shards, 18 to 35 minutes each**, so serial time is
  > about two and a half hours, not twenty minutes. All 168 were caught. The
  > old figure is the one that would make a local full run look affordable, which
  > is why it is corrected here rather than annotated.
  >
  > *And a correction to the correction, which is the more useful entry.* That
  > count was first written as **169**, from a `--verify` run taken after a
  > mutation had already been added — so the figure describing the CI run was read
  > off the working tree rather than off the run. Six shards at 28 is 168, and the
  > arithmetic was there to check at the time. **A number quoted from the wrong
  > snapshot is the same defect as rule 11b's stale download**, in a place nobody
  > thought to look because it was only a count.
  >
  > *Do not compare shard membership across runs after adding a mutation.* Shards
  > are by POSITION, so inserting one mid-list shifts everything after it into a
  > different shard. The `checks.yml` comment claims two logs can be compared line
  > for line; that holds only while the list is unchanged.

  **`--changed` is NOT in that list any more — mutations run in CI.** John asked
  for this on 2026-07-28: a local `--changed` was costing ten to twenty minutes
  per commit and blocking every experiment while it ran, and `checks.yml`
  already runs the FULL harness sharded six ways on every push. Running it
  locally was buying a few minutes of earliness for a large share of the
  session's wall-clock.

  **What that trades away, stated so it is a choice and not a drift.** A
  surviving mutation now surfaces in CI after the push rather than before it, so
  a vacuous test region can be committed. The mitigation is that CI is *watched*
  — if a push goes red on the mutation shards, that is the same signal arriving
  a few minutes later, and it must be treated as blocking rather than noted.
  Two mutations survived on 2026-07-28 (`the-selector-never-reaches-the-rule`,
  and both gate mutations before it) and each needed a behavioural test; that
  class of finding is exactly what now arrives late.

  **`--changed` is here because `--verify` does not catch a vacuous test
  region.** `--verify` asserts every mutation's original text is present; it
  says nothing about whether the suite would notice the mechanism breaking.
  That question is the full harness, which is twenty minutes and therefore
  CI-only — so a surviving mutation passes every local check and is reported
  later, on a run nobody is watching, against whichever commit was pushed next.

  > *Calibration.* `the-cache-admits-by-RECENCY-not-residual` and
  > `the-cache-read-is-not-gated-by-the-MATCH` both survived at `b480926` and at
  > least one commit before. The exact cache is the project's **first
  > controlled improvement on the corpus**, and its two defining claims —
  > admission by residual, and the match gate — had nothing asserting them. They
  > were found only because an unrelated refactor made `--verify` fail and
  > someone went looking. `--changed` runs the mutations whose target file this
  > work touches, which is seconds and is exactly the set at risk.
  The full mutation harness runs in CI, sharded; locally it is
  `python tools/mutate.py --only <the mutations just added>`, because a full
  run edits the source for twenty minutes and every experiment refuses to run
  while it does.

  **Mutation testing takes the tree EXCLUSIVELY.** It edits source in place,
  mutation by mutation, so anything else running against the repo meanwhile
  reads a mutated file. Do not start a test run, an experiment or a probe while
  it is going, and do not edit source while it is going either — it restores
  from what it read at the start.

  **Do not `git add` or commit while it runs.** Staging reads the working tree,
  so a commit made mid-run can capture a live mutation — the same failure as
  `3634a23` shipping `rank = strength`, arrived at from the other direction.
  Wait for it, then `--verify`, then stage.

  **Renaming a variable can make a mutation stale, and stale is not caught.**
  Repointing `key` to `hop_key` left `the-hop-re-encodes-into-value-space`
  targeting text that no longer existed; the run reported `65/66 caught` and
  named the one it could not apply. A mutation that cannot be applied is not a
  passing mutation — it is a claim nothing is checking, which is what `--verify`
  exists to surface. Re-point it in the same commit as the rename.

  > *Calibration.* A background `--changed` was left running while a full suite
  > ran against the same tree. It reported **7 failures in `test_reward_gate`**,
  > none of them real and none of them in the files being changed. A second run
  > reported 3, a different set — the tell was that the failures moved.
  > Phantom failures cost more than the serialised wait: they look exactly like
  > a regression in code nobody touched.

  **And if it is killed, `--verify` before anything else.** A mutation is live
  on disk between the edit and the restore, so an interrupted run leaves one
  there. Killing a stale `--changed` left `the-candidates-are-listed-backwards`
  applied in `local_memory.py` — a `reversed(pending)` that changes no length,
  no count and no magnitude. `--verify` names the mutation and the file, and the
  fix is to apply the entry's `new` → `old` swap by hand.

  **Stopping the background TASK does not stop the harness.** It kills the shell
  wrapper and leaves the Python process running, still editing source. The tell
  is `.mutate.lock`: the harness holds it and a second run refuses with the live
  pid, which is the only reason this was noticed rather than committed. Confirm
  with `tasklist //FI "PID eq <pid>"`, kill with `taskkill //F //PID <pid>`,
  then delete the lock and `--verify`. Two full check runs happened against a
  tree that process was still mutating, and both passed — passing under a live
  harness is luck, not evidence.

  **`tasklist` filtering lies often enough not to trust it, and NEVER delete the
  lock to "clear" it.** A `tasklist //FI "PID eq ..."` came back empty for a pid
  that was in fact alive; the lock was deleted as stale on that basis, a second
  harness started, and TWO were then editing the same file. The tell was that
  `--verify` named a *different* live mutation on each run — a live mutation
  does not move, so a moving one means something is still cycling. Enumerate
  with `Get-CimInstance Win32_Process -Filter "Name='python.exe'"` and read the
  command lines; that shows the truth. Delete the lock only after the pid it
  names is confirmed dead.

  > *Note.* `mutate.py` exits **1** on a lock refusal, correctly. It looked like
  > 0 because the invocation ended in `| tail`, which is the same masking the
  > workflows were fixed for. `set -o pipefail` applies to local shell
  > invocations too, not just CI.

  **`--verify` comes first and takes a second.** It asserts every mutation’s
  ORIGINAL text is present, which is the only check that catches the source on
  disk not being the source anyone means. Commit `3634a23` shipped
  `rank = strength` — `the-tag-admits-the-strongest`, live — inside the change
  whose entire argument is that admitting the strongest is backwards, because
  `git add -A` ran while a background harness had the file open. Nothing ran the
  suite against the tree being committed, so nothing objected. It also catches
  the harness going stale: on its first run it found `storage-mask-ignored`
  pointing at a line a refactor had moved.
  The second is not optional and not a nice-to-have: **rule 10 is unenforceable
  without it.** It breaks each named mechanism on purpose and requires the suite
  to go red; a mutation that survives marks a vacuous region of the test set. It
  also fails loudly when a refactor moves a line it targets, rather than going
  quietly green while checking nothing.
- **New mechanisms default to off**, so existing results stay reproducible and
  the comparison against not-having-it is free.
- **Add a mutation when you add a mechanism.** A mechanism with no mutation has
  tests nobody has seen fail.
- **A reference implementation stays dependency-free and obviously correct.**
  Any faster path is asserted against it rather than replacing it.
- **A check that guards a long job must run in a second, not at the end.** A
  configuration error that kills a twenty-minute run on its first line should be
  caught before launch; a guard that fires when the run finishes has already
  spent it. `tools/check_workflows.py` is that check for CI: it reads every
  `python experiments/*.py` line out of every workflow and compares the flags
  against the script's own `--help`. It takes about a second, and it turns a
  spent matrix into an error before anything is dispatched.
- **Repo-specific rails are a ratchet, not a rule.** `tools/check_rails.py`
  enforces three conventions that have already cost a result: a summariser
  reporting a recovery ratio imports `tools.recovery`; a sweep file carries
  PREDICTIONS and COST sections; an experiment goes through
  `experiments/harness.py`, which is where `refuse_if_mutating()` lives.

  Legacy violations are exempt in `tools/rails_baseline.json` — thirty-seven
  sweeps predate the COST convention and eleven scripts predate the harness —
  because a check that fails on everything gets suppressed. **A file not in the
  baseline must comply, and an exemption that no longer applies is an error**,
  so the list can only shrink without a visible diff. Generic lint is a solved
  problem; these encode the specific failures, which is the only reason they
  earn a check.
- **A copy that has not drifted is catchable; one that has is not.**
  `tools/check_duplication.py` AST-normalises function bodies and flags two that
  share a shape. It caught `load_baseline` copied between it and
  `check_rails.py` within minutes of being written.

  **Its stated justification was wrong and the tool measured that.** BACKLOG
  asked for it on the grounds that it would have found the five hand-copied
  recovery refusals; run over the pre-port tree it finds none of them, because
  those copies had already diverged — one had lost its floor check, three chose
  the learning rate differently — and divergence is exactly what defeats a
  structural hash. So it is PREVENTION and not detection, and the thing that
  catches a drifted copy is still `tools/mutate.py`: a mutation in one path the
  tests do not notice.
- **Commit frequency is a resource decision, not only a hygiene one.** Every
  push runs `checks.yml`, which is seven jobs -- the suite plus six mutation
  shards. A session that commits ten times queues seventy jobs, and a sweep's
  fifteen sit behind them. Measured: four superseded `checks` runs were sitting
  in front of `sweep-g9-11` while it had not started a single job.

  `checks.yml` now cancels superseded runs for the same ref, which fixes the
  recurrence. It does not fix the underlying habit — **batch related work into
  one commit when a sweep is in flight**, and cancel superseded runs by hand if
  one is already starving.
- **One sweep matrix in flight at a time.** A matrix takes every runner, so a
  second sweep pushed while one is running does not overlap — its jobs queue,
  seize the runners the moment the first finishes, and starve the first's
  aggregate step. Enforced by a shared `concurrency` group rather than
  remembered.
- **A sweep that loses seeds still reports.** Aggregation runs on `always()` and
  prints how many seeds returned. A matrix where two jobs died is a result with
  two seeds missing; reporting the survivors as though they were the whole
  matrix is worse than either reporting nothing or reporting the loss.
- **A tool that edits source in place needs an out-of-process backup.**
  `try`/`finally` does not run when the process is killed, and a timeout will
  eventually leave an edit in the working tree.
- **A long-running job is not a reason to stop working.** While one is in
  flight, pick up something that does not depend on its result — a probe to
  build, a claim in the repo that has never been checked, a mechanism that lacks
  a test. There is always local work.
- **Arm the wake-up in the same action that launches the job.** A finished run
  looks identical to a running one until someone asks. Launching and watching
  are one step, not two.
- **Never end a turn with neither more work nor an armed wake-up.** Writing
  "continuing on the next thing" and then stopping is a dead stop that nothing
  recovers from. Either keep going, or schedule the return. Prose is not a third
  option.
