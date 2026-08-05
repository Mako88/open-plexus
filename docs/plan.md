# Where this is going

- **The only doc, and it holds nothing that is finished.** What a built mechanism
  does lives in its XML comments, where the compiler enforces every reference.
- **Findings live in the commit** that produced them, and in the test that asserts
  them. Never here.
- **One line an item.** A cap per ITEM, not per doc — twelve new ideas cost twelve
  lines and nothing has to be retired to make room.
- **Built and decided means GONE FROM HERE, and it means no arm either** — John,
  2026-08-04. A winner becomes the code; losers are deleted, leaving a revival row.

---

## The goal

- **Understand rather than perform** — answer *what would the world look like if I
  did X*, which a sequence model cannot be.
- **Learning is a co-occurrence count.** Everything else is plumbing around it.

## The constraints

- **C1** — no node reads another's data.
- **C2** — messages are late, jittered, out of order.
- **C4** — no episode boundary, so nothing may depend on train-then-test.
- **Counts only ever rise.** The G-Counter property buys convergence with no
  coordinator, which is what buys C1 and C2.
- **Nothing is unlearned, only outvoted or evicted.** Decay is not available;
  eviction on "not touched since" is.
- **Codes must be identical on every machine forever** — so nothing fitted, ever.
- **A front end may say what it is looking at, never what to conclude.** Five
  channels do; a sixth needs an argument against the other five.

---

## TO BUILD

### Look necessary for the goal, and absent

- **A world with a real-valued signal to point `Winnow` at.** The front end is
  built and measured; nothing here reads a number rather than a symbol. CLEVR's
  `3d_coords` is real data in a world that scores, and the loader discards it.
- **A goal that is not the current state.** `Drives` wants to stay in bounds and
  the rollout predicts; nothing can hold a state that is NOT current and steer
  toward it. Planning needs a target, and sub-goals are what serve many tasks.
- **An answer that is no code it has ever seen.** Answering is ARRIVING somewhere,
  so yes/no and counting questions ask for a token the world never shows.
  `BabiTests` names the tasks; `PrimerTests` says why feeding it English does not
  fix it.
- **Temporal abstraction.** `Chunk` names a SET; a macro-act is a SEQUENCE. A
  sibling of `Chunk` whose name is derived from members IN ORDER rather than sorted
  — everything else carries over. An act is already a code.

### Worlds that are missing

- **One where several cues arrive together and only some carry the outcome** — the
  write-path gate blocks, and no world here can show what that buys.
- **One whose dynamics BRANCH** — a cycle is an attractor, so the rollout's
  compounding error is untested.
- **One an arm can bootstrap in** — every credit arm on `Tending` is a coin toss.
- **One that NEEDS variable binding** — `Clutrr`, `gSCAN`. The mechanism is built and
  measured only on constructed cases; `BindingGapTests` is the scoreboard.

### Owed re-runs

- **The eligibility trace, re-run against a silence nobody had checked.** It wrote
  more and changed nothing, measured against an arm that was quiet for budget
  reasons.

### Mechanisms

- **A span the brain reads off the stream** — a carried pair that never recurs is
  noise, so the share of `Kind.After` cells whose count passed one separates a
  cycle from independent draws. A node's own row statistic; see `WalkSettings.Span`.
- **Chunk candidates BELOW a whole moment.** A chunk covering the moment writes
  `name`-to-member and destroys member-to-member, which IS the task on `Senses`
  and is why agreement went inert. Pair-merging (Sequitur, BPE) composes.
- **The adapter is the only thing between a world and the brain** — John, 2026-08-05.
  `Tending` bands its own moisture and calls `Grains` itself; that is a world
  deciding how it is coded. `IQuantizer` is already the interface.
- **Fork 24 lives on one world.** A controller six worlds never call is on the
  wrong side of that line.

- **A reason to seek** — every cell is written for acts TAKEN, so no walk reaches an
  untried one. Revives once something can explore.
- **Depth wants its own control** — every rollout step is a whole walk.
- **A self-set beam** — a width the system sets itself; `Surprise.Rate` or a node's
  own row statistics.
- **Conditioning the prediction itself**, rather than suppressing the observation.
- **Chunk candidates below a whole moment** — pair-merging (Sequitur, BPE)
  composes; utility belongs per chunk (Minton, SOAR).
- **Hierarchy** — walk a thousand chunks, not a million nodes. What step 3 is for.
- **Multi-token output** — fork 11 built the addressing; a world that wants two is
  what is left.
- **Cold storage** — what makes an evicted count recoverable rather than gone.
- **A row cap that varies by node** — a node seen ten thousand times has more to say
  than one seen twice. `k · log(seen)` is local and scale-free. Measure it on
  `Skew`, the one setting where a cap is not inert.
- **Space-Saving for the row cap** — the eviction scan is linear in the cap. Real
  text is what needs it: swallowing any is ruinous at a cap the questions still
  work under.

### Housekeeping

- **The knob pass, LAST.** A dial swept before the structural work measures a
  system about to change under it.

### The wire, when the remote half lands

- **Only the local half of `HybridBus` exists.**
- **C2 is untested and the harness is why** — every reader here waits for quiet, so
  lateness becomes waiting. Needs a reader on a DEADLINE, or two machines.
- **Coalesce a settling wave into one send** — flush on idle *or* size *or* time.
- **Bits, not JSON** — a sixth of a packed message is the `Guid` broadcast id.
- **Split `Chain`** — an approximate-membership filter for the hop, full chain
  rebuilt at the origin.
- **UDP, not TCP** — head-of-line blocking would stall every thought behind one lost
  packet. QUIC's unreliable datagram extension (RFC 9221).
- **Fork 1, the distributed rendezvous** — smaller than it looks; the counts need no
  protocol, only the join does.

---

## DO NOT RE-TRY

**A refutation is conditional on its configuration, so a row without a revival
condition is a superstition.**

| what | what refuted it | what would revive it |
|---|---|---|
| `StepCost.Best` / `Local` / `Constant` | Factorial message growth where inverse cost is polynomial | **MET by `Toll.Traffic`** |
| `Refuel` | Nothing is paid back, so it did nothing | Anything returning budget to a route |
| Sender-*weighing*, `IMarginals` | A C1 violation, and behaviour was identical without it | Never. `Message.Seen` is the legal version |
| Absolute actions, unrotated view | One move in four instantly fatal | A body with no heading |
| Survival as the score | Circling wins. Snake cannot discriminate policies at all | Homeostatic drives, where standing still stops paying |
| A beam over partners | A constant nobody set, doing the cutting | A width the system sets itself and reports |
| Clusters by modality | Splits picture from sound, the one link this design exists to make | Never |
| Clusters by time of creation | Two machines compute different owners for one code | Placement agreement without a coordinator |
| `Adaptive` reflection on `Hunger` | Inverted: it wrote most where it helped least | A signal that discriminates |
| A deeper walk for prediction | Monotonically worse without edge kinds | **Edge kinds**, and that refutation reproduced |
| `ArrivalValue.Lift`, `Accumulate.Max` | Swept, inert, both explanations refuted. **Deleted** | Lift in the **cost** |
| Naming fewer predicted codes | Half true: coarse ranking informs, fine does not | **REVIVED at one code** |
| `Window` span | Null on snake, worse on `Babi`, and the whole task on `Rhythm` | **Something making a carried edge worth its ROW.** Kinds were half; a weight is not the other |
| A carried-edge discount | Moves along the frontier the budget already describes, and starves the walk below it | A world where carried and simultaneous edges compete in one row |
| `includeEmpty: true` | Ruinous under `Best` pricing | **Revived — no clear winner since** |
| `Pricing.Balanced` | Times out — the geometric mean sits between the marginals, so weights rise and the walk explodes | A bound not relying on the weight being one marginal's reciprocal |
| `Pricing.Driven` | Two local rules, both worse in both worlds; a per-hop choice puts routes on different scales | A local quantity predicting which arm wins, on a world where they differ |
| `Accumulate.Fused` | Half of agreement's lift and all its cost; inverted orders tie identically under RRF | Many candidates, or a fusion separating by something other than position |
| The carried negative discount (`Message.Against`) | Inert — an arm ignoring it reproduced the arm reading it. **Deleted** | A world where an act's harm is confined to a few states |
| `Driven` / `Delayed` / `Topped` — credit as a heavier write | All three peak far below the bar at their own best budget. **Deleted** | Anything making a heavier write into *this was done here* mean something else |
| ΔP over the credit cell (`Attending.Contingent`) | Not refuted, DOMINATED: it ties the one-sided count and inhibition clears both. **Deleted** | A world where some states are recoverable and others are not, so the base rate varies BY STATE |
| `Ranked` as step 4's fix | The lift was the bootstrap's coin toss, and a varying code thins every edge | Anything making the walk prefer a partner other than the one it took last |
| Widening the walk — `Kindred`, `Foreseeing`, `Backing` | All three: louder and below the bar, or silent everywhere | **A likeness the GRAPH DID NOT COMPUTE** — step 8 and nothing short of it |
| A trained quantiser — k-means | Two machines fitted on different samples code the same input differently | Never fitted |
| `Question.Path` — a relation per hop | A fixed path trades coverage against precision with no middle. **Deleted** | `Downstream` wants the reverse temporal edge, which `Kind.Before` now writes |
| `Kind.Informed` — a cell for what surprised | No walk reaches an untried act. **Deleted** | Anything that can explore |
| `Attending.Marked` — the credit cell, unconditioned | Peaks below the blind bar. It was `Credited`'s CONTROL, so ruling out the extra cell and the staleness goes with it. **Deleted** | A second cell written on anything but the outcome — re-take the control |

---

## TRAPS

**Named so nobody reintroduces them.**

- **One weight doing two jobs is this design's recurring fault** — it ranks a
  partner AND prices the hop. It has bitten five times.
- **A dial measured at one setting of another may be measuring that one.** Sweep at
  two run lengths, never with a third pinned.
- **Measure one mechanism ON from a known baseline, never one OFF from all-on.**
  The second direction read small for everything on 2026-08-05; whatever was
  already broken was doing the damage.
- **Two arms can peak at different budgets**, so one sweep compares one at its best
  and the other on its way up. Compare PEAK TO PEAK.
- **A ranking arm needs something to rank.** `Homeostat` at stamina 4 offers a
  choice on one step in six hundred. Read `Choices`.
- **A check can be wired and unable to fire**, which reads as passing. Arm anything
  that has always read zero.
- **A dial can be declared, documented, passed everywhere and connected to
  nothing.** Every run reports `Complaints`; read them.
- **AND A DIAL CAN BE SET ON THE SETTINGS AND READ FROM A PARAMETER.**
  `InputMachine` took `span` and `gated` as arguments while `WalkSettings` held
  the same names unread. **A sweep that cannot reach is silent, not wrong, and
  silence reads as free.**
- **A number in a commit message is a claim, not a record.** An attribution
  everybody trusted was taken through exactly that unread dial, and cost two
  sessions before anybody re-measured it.
- **A fallback is a control arm nobody meant to run** — silence drifts an arm toward
  the random bar for free. Report silence beside the score.
- **And the fallback is often the only exploration there is**, so curing silence can
  remove it and read as harm.
- **A silence has two causes wanting opposite fixes** — an empty cell, or a walk that
  cannot afford to reach one. Spend more and see if the voice returns.
- **A small sample can look like a mechanism.** One seed drew a clean learning curve
  six flattened; twelve seeds showed a gap thirty-two closed.
- **A mean over a population the problem created cannot see it.** Read `Widest`.
- **A `cref` is not a call and a `ToString` is not an assertion** — both are how a
  dead mechanism goes on looking alive. `DeadCodeTests` is the budget.
- **Copies drift where nothing fails.** `DuplicationTests` is the budget.
- **The test suite is serial on purpose** — parallel load hid a real disagreement.
- **Closed in code**: consecutive seeds are not independent (`Seeds.Apart`);
  `Measured.Separation` returned zero for no spread; `WhenQuiet()` was not a finish
  signal; walks were read before finishing (fork 22).

---

## OPEN DEFECTS

- **`Accumulate.Agreement` reads EXACTLY equal to `Sum`** on `Composed` and
  `Ranking`, and **the minted name is NOT why** — it ties identically with
  chunking suppressed outright. Not the arrival-order fix either. Two
  explanations are spent and the defect is untouched.
- **Fork 24 probes on questions that are SCORED.** `Budget.Next` answers at half
  the settled stamina while hunting, so a world needing depth answers those
  wrong. `Moves` reads nought, so it never stops. Bypassing it restores `Senses`
  above its pre-cashing baseline.

---

## FORK NUMBERS THE CODE CITES

**Never renumbered** — `DocsTests` asserts each resolves.

| | |
|---|---|
| **1** | The distributed rendezvous. Open |
| **3** | Cluster placement: uniform hash against prefix locality. Open |
| **5** | A death writes off routes into the dead cluster. Closed |
| **6** | Broadcast the origin, route the hops. Closed |
| **11** | A finished thought is published and routed by code, so N actuators act on one broadcast. Closed |
| **12** | A fixed seed reproduces a run exactly. Closed — REOPENED and reclosed 2026-08-05; `Receive` folded arrivals in delivery order |
| **18** | Prediction conditional on the next action. Answered by edge kinds |
| **20** | Split budgets — deep to act, shallow to predict. Closed |
| **21** | Compression as an edge. A trade; off |
| **22** | A transiently-zero live count dropped later reports. Closed |
| **23** | Compression self-regulating? Not on any signal found yet |
| **24** | Budget controller aims at a moving target. Off by default |
| **25** | The binding world — built to fail, failed as predicted, since lifted |
