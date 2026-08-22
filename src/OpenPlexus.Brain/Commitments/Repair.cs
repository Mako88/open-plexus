using OpenPlexus.Codes;

namespace OpenPlexus.Commitments;

/// <summary>Which rule picks the condition a repair adds.</summary>
/// <remarks>
/// <b>Two rules that both do something</b>, rather than a mechanism and its own
/// absence. A boolean here would put the control arm in the code forever as a
/// way of not running repair properly; naming both makes it a comparison.
/// </remarks>
internal enum Choosing
{
    /// <summary>The condition that most separates the hits from the misses.</summary>
    Separating,

    /// <summary>Any condition the failures contained, drawn uniformly.</summary>
    Present,
}

/// <summary>What a separating condition must do besides separate.</summary>
/// <remarks>
/// <para>
/// <b>Fork 86</b>, and it is the one that killed ILP wearing this design's clothes. The ladder
/// extends only when nothing in the current language separates the failures from the hits, and
/// that is the whole of what makes the bias EARNED rather than declared. On a wide alphabet
/// something always separates — a code present in exactly the failures and nowhere else is a
/// perfect discriminator and a perfect memorisation, and the admission rule cannot tell them
/// apart. Measured: on a lesson whose answers are unreachable by conjunction at all,
/// <c>Tally.Wanting</c> reads nought while repair churns.
/// </para>
/// <para>
/// <b>So the second half of the bar</b> is that the CHILD must be judgeable, and it needs no
/// new number. <see cref="CommittingSettings.Floor"/> already means <i>enough firings for a
/// proportion to have any power</i>; a conjunctive child keeps the parent's firings the
/// condition was present in, so a condition present in fewer of them than the floor mints a
/// rule that can never itself be tested. Admitting one is admitting a rule nothing can ever
/// refute, which is what memorising IS.
/// </para>
/// <para>
/// <b>And what it costs is repairs</b>, which is the reading rather than a worry. A language
/// that stops separating says so, and a machine that repairs less may well score less — those
/// are the two halves of the same arm and both are printed.
/// </para>
/// </remarks>
internal enum Admitting
{
    /// <summary>Anything that clears the separation bar, which is every earlier reading.</summary>
    Anything,

    /// <summary>And the child it makes must be able to clear the floor itself.</summary>
    Testable,
}

/// <summary>What counts as surprising enough to mint on.</summary>
/// <remarks>
/// <b>Two rules that both do something, exactly as <see cref="Choosing"/> is.</b> A
/// boolean would put the arm in the code forever as a way of not running genesis
/// properly; naming both makes it a comparison.
/// </remarks>
internal enum Surprising
{
    /// <summary>Any failure at all. What ran before anything gated this.</summary>
    AnyFailure,

    /// <summary>Nothing that fired even proposed what arrived.</summary>
    Unaccounted,
}

/// <summary>How wide a scope genesis is allowed to mint.</summary>
/// <remarks>
/// <para>
/// <b>Two rules that both do something</b>, exactly as <see cref="Surprising"/> is. A boolean
/// would put the arm in the code forever as a way of not minting properly.
/// </para>
/// <para>
/// <b>John's</b>, and the observation behind it is that a conjunction is currently DISCOVERED.
/// Genesis mints one code a commitment, so <i>this and that predict the other</i> is reachable
/// only by narrowing a one-code rule after it has failed enough times to clear the repair
/// floor — which is a price paid to find out something a told statement said outright. An
/// assertion is not a guess: it hands over the scope and the claim together.
/// </para>
/// <para>
/// <b>And the risk is named before the reading.</b> A whole-moment scope fires only where every
/// one of its codes is present, so on a world whose moments never repeat it fires once and
/// never again. It pays exactly where a later moment is a SUPERSET — a question naming what a
/// statement named — and is dead weight everywhere else. Fork <b>63</b>.
/// </para>
/// </remarks>
internal enum Rooting
{
    /// <summary>One code a commitment, so a conjunction is only ever reached by repair.</summary>
    Singly,

    /// <summary>One code each, and the whole moment as one scope beside them.</summary>
    Wholly,
}

/// <summary>Whether the machine answers when nothing behind the answer has been right.</summary>
/// <remarks>
/// <para>
/// <b>John's, and it answers a tie</b> rather than breaking one. The vote ranks
/// expectations by weight and breaks a tie by code order, so on a first telling — where every
/// accuracy is still nought — the machine answers from a hash and is then corrected on a guess
/// it had no basis for. <c>LessonTests</c> reads half of what claiming the rarest word gets
/// wrong as exactly that.
/// </para>
/// <para>
/// <b>The signature is a weight of nought and it needs no number.</b> A weight IS the best
/// advocate's accuracy, so nought means nothing that fired has ever been right about what it
/// is advocating. That is not a thin margin between two well-evidenced answers; it is no
/// evidence at all, and it is the one condition here that is a fact rather than a level.
/// </para>
/// <para>
/// <b>And on a world that is WATCHED it costs the learning nothing.</b> Settlement tallies
/// everything that fired whatever the vote said, and <c>Brain</c> computes <i>wrong</i> as the
/// vote not matching what arrived — so a silent vote is a failed round, genesis still runs and
/// repair still runs. What stops is the assertion.
/// </para>
/// <para>
/// <b>And an ACTED world costs more than that</b>, which is a correction rather
/// than a caveat: this said the learning was untouched full stop, and that was written from the
/// watched case alone. <see cref="Machines.Curiosity"/> asks <see cref="Machines.Brain.Voting"/>
/// what the population expects and branches on the answer, so a moment with nothing grounded
/// has no claim to make and the machine falls to a blind ask or to silence. Declining removes
/// DOINGS there, and a doing is how a settlement is obtained — a third of the asks on
/// <c>Worlds.Conversing</c>, which is learning rather than assertion.
/// </para>
/// <para>
/// <b>It is coherent rather than a fault</b>, and it ships. Claiming a groundless guess in a
/// conversation is the thing this exists to stop, and a chooser that declines to claim where
/// nothing is grounded is the same rule one seam out. What was wrong was the remark, which
/// let a dial named for the assertion reach the chooser unrecorded.
/// </para>
/// <para>
/// <b>It is not <c>Speaking</c>, which is refuted twice in the plan.</b> That refused an
/// untested commitment its VOTE, so a different and better-tested rule won instead — silencing
/// the right rule to reseat a wrong one. This silences the ROUND. Nobody wins in anybody's
/// place, and no rule is refused anything.
/// </para>
/// </remarks>
internal enum Deciding
{
    /// <summary>Answer whatever the ranking put first, however little is behind it.</summary>
    Anyway,

    /// <summary>Say nothing where the winning weight is nought.</summary>
    Grounded,
}

/// <summary>Whether a minted commitment is credited with the round that minted it.</summary>
/// <remarks>
/// <para>
/// <b>Genesis mints a rule on a round it was demonstrably right about</b>, and hands it a blank
/// record. The scope was live, the outcome arrived, and the commitment says exactly that — but
/// <see cref="Population.Settle"/> has already run by the time it exists, so it is never told.
/// </para>
/// <para>
/// <b>And a blank record is not neutral here.</b> An accuracy starts at nought and the vote is
/// a maximum over accuracies, so a fresh commitment loses every vote until it has fired again —
/// which on a conversation means waiting for the statement to be repeated. A rule that is
/// already correct sits mute for as long as it takes to hear the same thing twice.
/// </para>
/// <para>
/// <b>What it risks is every one-code mint arriving at a perfect accuracy</b> together, which
/// flattens the vote among them and hands the tie to code order. That is why it is an arm and
/// not a repair.
/// </para>
/// </remarks>
internal enum Crediting
{
    /// <summary>Nothing, so a mint starts at nought and must fire again to matter.</summary>
    Nothing,

    /// <summary>The round that minted it counts as a hit, because it was one.</summary>
    Birth,
}

/// <summary>
/// Whether subsumption may read a member's entailment of its CATEGORY — <b>fork 85, and
/// the half of it that survived being measured.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>A category in the moment was not a rewrite of the rules</b>, and the compression was never
/// behind one. Genesis roots on a category code exactly as it roots on anything else, so
/// the coarse rule ALREADY EXISTED — <c>ReturningTests</c> measured 606 rules folded against
/// 196 once this is on, at three seeds and an unmoved score. What did not exist was any way
/// to see that <i>this look predicts Y</i> is narrower than <i>that kind of look predicts
/// Y</i>: <see cref="Commitment.Narrows"/> is a subset test, and the two scopes are the same
/// length with no code in common.
/// </para>
/// <para>
/// <b>So the operator fork 85 asked for is deleted</b>, and this is what is left. Proposing
/// the coarse claim as a new commitment with a fresh record was built, measured and removed:
/// it cost 60 rules over three seeds where this costs none, because a recast must fire
/// <see cref="CommittingSettings.Floor"/> times before anything may judge it and the claim it
/// makes was already reachable. See the plan's revival row — what would bring it back is a
/// vocabulary the brain holds that no moment carries.
/// </para>
/// <para>
/// <b>And it is inert without <c>Population.Sorts</c></b>, so <see cref="Never"/> is a control
/// rather than a shipped default. A run that was never told which codes are alternatives
/// cannot tell the two apart, which is why turning this on reproduces every number recorded
/// before it existed.
/// </para>
/// </remarks>
internal enum Coarsening
{
    /// <summary>
    /// A category is a code like any other, so nothing knows a member entails it.
    /// </summary>
    /// <remarks>
    /// <b>The control, and it has to be named to be one.</b> Folding categories into the
    /// moment without this is what says how much of the compression was the FOLD — and the
    /// answer is 652 rules to 606, which is nearly none.
    /// </remarks>
    Never,

    /// <summary>Subsumption reads the entailment wherever it finds one.</summary>
    Judged,
}

/// <summary>What it takes for a narrower commitment to survive beside a general one.</summary>
/// <remarks>
/// <para>
/// <b>The one mechanism here that prefers generality</b>, and it was written up as never
/// firing on evidence that does not say that. <c>Judged.Narrowed</c> reads nought
/// everywhere, and that was read as subsumption doing nothing. It counts something
/// narrower: unsound residents that a resident SOUND one already covers. Swapping the
/// rule below moves the resident count from 116 to 228 at eleven bits, so the clause
/// fires constantly — the correction is recorded here because the wrong version was
/// committed first.
/// </para>
/// <para>
/// <b>WHAT `Narrowed` actually says is that nothing survives under a sound parent</b>,
/// which is a fact about which rules are left rather than about whether the mechanism
/// runs.
/// </para>
/// </remarks>
internal enum Subsuming
{
    /// <summary>
    /// The general one must be AT LEAST AS ACCURATE. What ran before anything questioned it.
    /// </summary>
    /// <remarks>
    /// <b>And it deletes more than the other rule</b>, not less, which was the surprise.
    /// The worry was that a memorised child is always a hair better and so always kept.
    /// It is kept — but a hair the other way is enough to delete it, and over a run that
    /// is most of them: this holds 116 residents at eleven bits where demanding
    /// SIGNIFICANCE holds 228.
    /// </remarks>
    Weaker,

    /// <summary>
    /// The narrower one must be SIGNIFICANTLY better, by the test repair already owns.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A child fires less often than its parent</b>, so its advantage has to clear its own
    /// smaller sample. That is a two-proportion test, which is exactly what the
    /// repair gate uses to decide whether a condition separates hits from misses — same
    /// arithmetic, same <see cref="CommittingSettings.Alpha"/>, no new number.
    /// </para>
    /// <para>
    /// <b>And it is a large, uniform win under noise</b>, and a wash without it. On the
    /// noisy multiplexer every repair gate gains about five points and roughly doubles
    /// its sound rules — 0.737 to 0.787, 0.725 to 0.779, 0.731 to 0.778 — while on the
    /// clean world it is level at six bits and slightly behind at eleven.
    /// </para>
    /// <para>
    /// <b>Which is the first result in this family whose direction follows</b> from a
    /// property of the world. A significance test is what sampling error calls for,
    /// and on a clean world a hair of advantage is real signal rather than luck — so
    /// demanding proof throws away something true. It is still not one setting for every
    /// world, but it is the first one whose right value the machine could in principle
    /// detect for itself.
    /// </para>
    /// </remarks>
    Insignificant,
}

/// <summary>
/// WHICH commitments repair may touch — <b>one of the two axes `Mending` used to be.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>It was one setting deciding two independent things</b>, while being named for one, which
/// is a line on this repo&apos;s own trap list. The four cells it shipped were a gate and
/// a timing crossed together, so every comparison against it moved both axes at once — and
/// a row in the plan&apos;s defect list about a world reaching into the brain rested on
/// exactly that. Separated, every finding taken under the old names is still readable:
/// </para>
/// <para>
/// <c>Outvoted</c> was <see cref="Ungated"/> with <see cref="Repairing.AfterFailure"/>.
/// <c>Uncovered</c> was <see cref="Uncovered"/> with <see cref="Repairing.EveryRound"/>.
/// <c>Improving</c> was <see cref="Improving"/> with <see cref="Repairing.EveryRound"/>.
/// <c>Neglected</c> was <see cref="Uncovered"/> with <see cref="Repairing.AfterFailure"/>.
/// </para>
/// <para>
/// <b>And it dissolved rather than being decided.</b> <c>Neglected</c> lost or tied on
/// both worlds measured, so the arm rule said delete it — while being the only cell that
/// isolated the gate axis, which is a cost that rule does not anticipate. Under two axes it
/// is not a cell at all, so nothing has to be preserved and nothing has to be given up.
/// </para>
/// <para>
/// <b>And two arrangements nobody had enumerated become reachable</b> — an ungated repair
/// every round, and the improving signal after a failure. Neither has been measured. That
/// they were unreachable rather than refused is the clearest evidence the list was hiding a
/// grid.
/// </para>
/// </remarks>
internal enum Mending
{
    /// <summary>
    /// Anything that failed and cleared the floor and the budget. What ships.
    /// </summary>
    /// <remarks>
    /// <b>Named for the absence of a gate rather than for the vote</b>, which is what
    /// separating the axes revealed. The old <c>Outvoted</c> meant <i>the vote had to
    /// be wrong</i>, and that was never a property of the GATE — it was the timing beside
    /// it doing the work. What this cell actually says is that no commitment is refused.
    /// </remarks>
    Ungated,

    /// <summary>
    /// And no child of it may have fired — the failure has to be one nothing accounts for.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>FORK 37&apos;S driver, and it needs no bookkeeping at all.</b> <i>Whether a parent
    /// still has failures no child covers</i> reads like it wants a parent-to-children
    /// index. It does not: a child&apos;s scope is its parent&apos;s plus a condition, so a
    /// child can only fire where the parent fires. If no commitment among those firing
    /// NARROWS this one, then this failure is in no child&apos;s territory — which is the
    /// whole of the question, answered from the firing set.
    /// </para>
    /// <para>
    /// <b>And its sign flips with the timing beside it</b>, which is why the split matters.
    /// Every round on the clean multiplexer it is the best thing measured; after a failure
    /// on the same world it is six and a half standard errors behind no gate at all. On
    /// <c>Worlds.Arranged</c> it is inert in both timings to three metrics. Its size
    /// is a fact about the world and its sign a fact about the timing, which is why no
    /// single sentence about it has ever been safe.
    /// </para>
    /// <para>
    /// <b>AND IT SELF-LIMITS THE WAY <c>Budget</c> WAS BUILT TO FAKE.</b> A parent forks,
    /// its child takes over a region, and the parent stops forking there — so the cap on
    /// children per parent stops being the thing that prevents a runaway. Fork 37 is
    /// about that cap having an interior optimum nobody can hunt; this is the signal it
    /// says would replace it.
    /// </para>
    /// </remarks>
    Uncovered,

    /// <summary>
    /// And only while forking this commitment has ever paid.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A per-commitment signal rather than a per-round gate</b>, which is what fork 45
    /// asks for. <see cref="Uncovered"/> is the best thing measured on the clean
    /// multiplexer, and the difference between the worlds is not which rounds it fires on
    /// — it is that on one world specialising WORKS and on the other there is nothing to
    /// gain. Nothing in the machine was asking which.
    /// </para>
    /// <para>
    /// <b>And the answer is already lying about in the population.</b> A parent whose
    /// children are no more accurate than itself has learnt that splitting it buys
    /// nothing; one whose children beat it has learnt the opposite. That is a fact the
    /// commitments already hold about themselves, it needs no world to interpret it, and
    /// it is the only feedback in reach that distinguishes <i>this rule needs
    /// specialising</i> from <i>this rule is simply being outvoted</i>.
    /// </para>
    /// <para>
    /// <b>The first fork is always allowed</b>, because the signal does not exist until it
    /// has been tried. A parent with no children has no evidence either way, and
    /// refusing on no evidence would make this a way of never repairing at all — which
    /// is the shape of arm this repo deletes rather than keeps.
    /// </para>
    /// <para>
    /// <b>And it should have made <c>Budget</c> unnecessary.</b> It did not self-limit at
    /// all, because a memorised child beats its parent easily — a narrow child that
    /// has stored a corner of the drawn set is far more accurate than the general parent
    /// it came from, so <i>has forking paid</i> answers YES forever.
    /// </para>
    /// </remarks>
    Improving,
}

/// <summary>
/// WHEN repair runs — <b>the other axis</b>, and the one that was doing most of the work.
/// </summary>
/// <remarks>
/// <para>
/// <b>The plan says an outvoted commitment accrues its own hits and misses</b>, and
/// waiting for the vote to be wrong meant it could never spend them — so how hard the
/// machine searched was a function of how good its answers already were.
/// </para>
/// <para>
/// <b>Separating this from the gate showed it was the load-bearing half.</b>
/// Every-round repair leads on both worlds measured, while the gate beside it is inert on
/// one of them to three metrics. The defect row calling this family a dial whose best value
/// moves with the world was written when the setting was read as a list.
/// </para>
/// </remarks>
internal enum Repairing
{
    /// <summary>Every round, whatever the vote said. What ships.</summary>
    /// <remarks>
    /// <para>
    /// <b>The default because it won everywhere it was asked</b>, which took a ledger to
    /// see. Sixteen standard errors of hard-round coverage at eleven bits skewed, six
    /// of true rules found, two on <c>Worlds.Arranged</c> — the world fork 58
    /// predicted it would die on — and inside one standard error on both even multiplexers.
    /// <c>RepairingTests</c> holds the grid and <c>LineageTests</c> holds the mechanism.
    /// </para>
    /// <para>
    /// <b>And it is the only thing here that separates readout from search.</b>
    /// Under <see cref="AfterFailure"/> the vote decides what repair may run on, so a
    /// weighing rule and a search rule are one change — measured, not argued: the three
    /// weighing arms this repo held built populations equal PER SEED under this timing and
    /// quite different ones under the other. Every vote arm in this repo's history moved two
    /// things at once, which is why two of the three could finally be deleted.
    /// </para>
    /// </remarks>
    EveryRound,

    /// <summary>Only on a round the population got wrong.</summary>
    /// <remarks>
    /// <b>What shipped until the ledger, and it starves a lineage of blame.</b> Under skew
    /// nearly every round the vote gets wrong is a round the rare outcome arrived on, and a
    /// commitment that expected what arrived cannot be a culprit — so the rules that would
    /// carry the hard rounds are never offered to repair at all, four hundred to one against
    /// the common ones at six bits. It is kept as the control that shows this, and because
    /// the blame it withholds is a dose response rather than a switch.
    /// </remarks>
    AfterFailure,
}

/// <summary>What a repair budget is spent on.</summary>
/// <remarks>
/// <para>
/// <b>A parent's children are recorded as a list</b>, and the same name goes in twice, so the
/// budget has always counted ATTEMPTS. <see cref="Population.Repair"/> notes the child
/// before asking whether it was new, and a repair that reaches a scope the population
/// already holds is exactly as expensive to the budget as one that mints something. What
/// <see cref="CommittingSettings.Budget"/> limits is therefore how many times a parent may
/// separate, not how many distinct children it may have.
/// </para>
/// <para>
/// <b>And the ladder says that is most of it.</b> <c>LineageTests</c> counts collisions at
/// twenty to fifty times the births at every majority rung, so a parent under a budget of
/// sixty-four spends nearly all of it re-deriving what it holds. That would explain the
/// plan's standing puzzle about this number better than any property of the search does —
/// an interior optimum moving with the relevant bits is what a re-derivation limit looks
/// like from outside, because how often a parent re-derives moves with the width.
/// </para>
/// <para>
/// <b>And it is only askable now</b>, which is why it was not asked before. Loosening the
/// budget on the skewed world bought nothing, measured over eight seeds — but under
/// <see cref="Repairing.AfterFailure"/> the lineages that needed the budget were never
/// blamed, so nothing was waiting on it. The question is a different one once blame
/// reaches them.
/// </para>
/// </remarks>
internal enum Budgeting
{
    /// <summary>Every separation a parent makes spends one. What has always run.</summary>
    Attempts,

    /// <summary>
    /// Only a child this parent has not reached before spends one.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Distinct children rather than successful adds</b>, which is the difference that
    /// matters when something is culled. A child minted, deleted and re-derived is one
    /// child a parent has thought of twice; counting adds would charge it twice and make
    /// the budget depend on what the CULL did, which is not a fact about the search.
    /// </para>
    /// <para>
    /// <b>And it is a synonym for <see cref="Attempts"/> under the shipped forking rule</b>,
    /// which is arithmetic rather than a world. <see cref="Population.Repair"/> charges an
    /// attempt and adds a name in the same two lines, and <see cref="Forking.Distinct"/>
    /// refuses a parent every code it has already spent — so two codes are two scopes are
    /// two identities, and the set and the counter move together forever. Under
    /// <see cref="Forking.Repeated"/> it is instead a FREE budget, measured at 36 repairs
    /// against 452. There is no third thing it could be.
    /// </para>
    /// <para>
    /// <b>So it is kept as that check rather than as an arm.</b> What it asserts is that
    /// <see cref="CommittingSettings.Budget"/>'s meaning does not depend on which counter it
    /// reads while forking is distinct, and it goes red the day forking changes —
    /// <c>BudgetingTests</c> holds it.
    /// </para>
    /// </remarks>
    Children,

    /// <summary>
    /// One attempt for every <see cref="CommittingSettings.Floor"/> misses the parent has
    /// taken — <b>a budget EARNED from evidence rather than granted for a lifetime.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The other two are both totals and C4 REFUSES A TOTAL.</b> <i>No episode boundary,
    /// so nothing may depend on train-then-test</i> — and a count a parent may spend across
    /// its whole life is a bet that its life is one episode. Measured: where the target
    /// moves, a capped parent never recovers because it spent its allowance being wrong
    /// before the move, and the two arms above are a total that binds and a total that
    /// cannot reach its own cap.
    /// </para>
    /// <para>
    /// <b>And it needs no new number</b>, which is the only reason it is buildable.
    /// <see cref="CommittingSettings.Floor"/> already means <i>enough misses to test a
    /// proportion by</i>, and <see cref="Population.Repair"/> already refuses a parent under
    /// it. This is that same rule applied repeatedly rather than once: the first attempt
    /// costs the floor, the second costs another, and a parent that stops being wrong stops
    /// earning. <see cref="CommittingSettings.Budget"/> is not read at all.
    /// </para>
    /// <para>
    /// <b>And the horizon is in events rather than time</b>, which is this design's own rule.
    /// A rate per thousand rounds would bind too and would be a clock — the thing settlement
    /// was built to avoid. Misses are events, they are already counted, and they are exactly
    /// the evidence that a repair is wanted.
    /// </para>
    /// <para>
    /// <b>What it risks is being free in disguise</b>, and that is what the grid is for. A
    /// parent wrong on most rounds earns attempts quickly, so on a world where the population
    /// is mostly wrong this may not bind at all — which is the fault <see cref="Children"/>
    /// turned out to have, and the reason it is compared against both of them rather than
    /// against a story.
    /// </para>
    /// </remarks>
    Earned,

    /// <summary>
    /// <see cref="Earned"/>'s rate, capped by what the parent has been RIGHT about —
    /// <b>one attempt per <see cref="CommittingSettings.Floor"/> misses and per
    /// <see cref="CommittingSettings.Floor"/> hits, whichever is scarcer.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b><see cref="Earned"/> reads misses alone</b>, so it funds a parent in proportion to
    /// how wrong it is. Measured on <c>Worlds.Arranged</c>, whose true rules are one
    /// code and where any repair is therefore damage: 0.651 unseen at 95 sound rules against
    /// the shipped total's 0.725 at 709. The parents doing the most damage are the ones
    /// earning fastest, which is a rate reading free rather than a budget. See
    /// <c>ArrangingTests</c>.
    /// </para>
    /// <para>
    /// <b>And the missing term is already counted.</b> A child fires only where its added
    /// code is present, so it covers a SUBSET of what its parent was right about — a parent's
    /// hits bound what any repair of it can preserve. Funding a parent past that spends the
    /// search on territory that does not exist, and a parent with no hits has nothing for a
    /// narrower version of it to keep.
    /// </para>
    /// <para>
    /// <b>So the allowance is the smaller of the two streams</b>, in floors, which is
    /// John's curve on how right a rule is. A parent always right earns nothing because it
    /// has no misses, one almost always wrong earns nothing because it has no hits, and one
    /// sometimes right earns most — being sometimes right IS holding a truth without enough
    /// specificity, which is the case narrowing exists for.
    /// </para>
    /// <para>
    /// <b>It needs no new number.</b> <see cref="CommittingSettings.Floor"/> is already the
    /// unit of <i>enough evidence to test a proportion by</i> and both counters are already
    /// on the commitment; <see cref="CommittingSettings.Budget"/> is not read, so C4's
    /// objection to a lifetime total does not transfer.
    /// </para>
    /// <para>
    /// <b>The cap is on the ALLOWANCE and not on the run's repairs</b>, which is worth
    /// writing down because the first version of this note said the second and was wrong
    /// within one reading. Per parent at any instant this allows no more than
    /// <see cref="Earned"/> — that is arithmetic, <c>min(hits, misses) &lt;= misses</c>. What
    /// does not follow is that a run repairs less: refusing one repair changes the population,
    /// which changes what fires, which moves every counter afterwards. Measured at six bits
    /// even, this arm repairs 152 against <see cref="Earned"/>'s 146. A winner-take-all argmax
    /// is chaotic in its evidence and a total over twenty thousand rounds is downstream of it.
    /// </para>
    /// <para>
    /// <b>And the ratio it applies is <c>min(hits/misses, 1)</c></b>, so it is inert on any
    /// parent right half the time or better and bites hardest where a rule is almost always
    /// wrong. A rule right one round in ten is allowed a ninth of what
    /// <see cref="Earned"/> grants it. That is where the refuted arm's damage was — 1281
    /// unsound rules on <c>Worlds.Arranged</c> — so the cap is aimed at the population
    /// the reading blamed rather than at the whole of it.
    /// </para>
    /// <para>
    /// <b>What would drop it before a grid runs: being indistinguishable from
    /// <see cref="Earned"/>.</b> That is <see cref="Children"/>'s fault — a free budget
    /// wearing a limit's name — and it happens wherever parents hold more hits than misses,
    /// so the cap never binds. <c>BudgetingTests</c> asserts the two arms DIFFER rather than
    /// which way, because a prediction written into a wiring check fails two ways and reads
    /// the same.
    /// </para>
    /// </remarks>
    Curved,
}

/// <summary>Whether a parent may propose a fork it has already made.</summary>
/// <remarks>
/// <para>
/// <b>Repair is deterministic and its table moves slowly</b>, so a parent proposes the same
/// child over and over. <see cref="Conditions.Discriminator"/> returns the argmax of a tally
/// that changes by one entry a firing, so the winner is stable for thousands of rounds — and
/// the code it names is still in the tally next time, because a commitment's table skips its
/// OWN scope and knows nothing of its children's. Measured: collisions run twenty to fifty
/// times the births at every majority rung.
/// </para>
/// <para>
/// <b>Which makes <see cref="CommittingSettings.Budget"/> a re-derivation limit rather than a
/// search limit</b>, and that is already written down as a finding. A parent under a budget of
/// two hundred and fifty-six spends nearly all of it arriving where it already is. What has
/// never been tried is spending those attempts somewhere else.
/// </para>
/// <para>
/// <b>Quantity is the one account of the uncovered rounds the evidence confirms.</b> A
/// child fires only where its added code is present, so covering what a parent is right about
/// takes MANY children — and <c>uncovered</c> falls monotonically as the budget rises, 1354 at
/// eight to 472 free. If a parent's attempts bought distinct children, its effective search
/// would be twenty to fifty times what every number here was taken under, at the same budget.
/// </para>
/// <para>
/// <b>It is the opposite half of the search from the step length</b>, which is why it is worth
/// trying after that failed. A two-code step made each attempt reach DEEPER and lost
/// coverage by overshooting; this makes each attempt reach somewhere ELSE at the same depth.
/// Nothing about the chain's length changes, so the failure that closed fork 74 does not
/// apply to it.
/// </para>
/// </remarks>
internal enum Forking
{
    /// <summary>
    /// The best code in the table, whether or not this parent has already forked on it.
    /// <b>The control</b>, and what every number taken before this shipped was taken under.
    /// </summary>
    /// <remarks>
    /// <b>Kept rather than deleted</b>, which is the exception this repo's arm rule allows. A
    /// loser goes with a revival row — but this one is the baseline every recorded reading in
    /// the tree sits on, and deleting it would make the whole history unreproducible rather
    /// than merely superseded. It is the only arm here whose job is to be the old number.
    /// </remarks>
    Repeated,

    /// <summary>The best code this parent has NOT already forked on.</summary>
    /// <remarks>
    /// <para>
    /// <b>The correction still divides by the whole table</b>, which is the part that could have
    /// gone wrong silently. Excluding a parent's spent codes shrinks the candidate set, and
    /// a Bonferroni over a smaller set is a LOOSER bar — so a parent that had forked fifty
    /// times would face an easier test than one that had forked once, and every late child
    /// would enter on a bar its siblings never had to clear. The search that found the tenth
    /// candidate is the same search that found the first, so it is charged the same.
    /// </para>
    /// <para>
    /// <b>And it is not a looser budget wearing a different name.</b> Raising
    /// <see cref="CommittingSettings.Budget"/> buys more attempts at the same argmax; this buys
    /// a different child per attempt. The two are separable exactly because the budget curve
    /// has already been taken — coverage rises with the budget and flattens past 128, which is
    /// what a search running out of NEW proposals looks like from outside.
    /// </para>
    /// </remarks>
    Distinct,
}

/// <summary>Every number the commitment machinery is allowed to have.</summary>
internal sealed record CommittingSettings
{
    /// <summary>
    /// What has to be true of a failure before genesis mints on it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The plan says `Surprise` gates genesis and has never run</b>, and on a wide
    /// front end that is not a detail. Genesis mints one commitment per live code,
    /// and a winnowed eight-by-eight thumbnail is 128 live codes over a sheet of 2,560
    /// cells. With ten outcomes the complete one-code space is 25,600 — and minting on
    /// every failure took the population to 23,762 against a capacity of 2,000. Genesis
    /// was not promiscuous, it was ENUMERATING, and a complete enumeration of
    /// <c>code → outcome</c> is a lookup table however it is scored.
    /// </para>
    /// <para>
    /// <b>And the division of labour is XCS's own.</b> Genesis exists for a moment
    /// nothing accounts for; the wrongness of a rule that DID account for it is what
    /// repair is for. A failure where something fired and proposed the right answer and
    /// was outvoted needs no new commitment — it needs the vote to weigh better, which
    /// accuracy already does. Minting there fills the population with restatements of
    /// claims it already holds.
    /// </para>
    /// <para>
    /// <b>So it is promiscuous early and quiet late</b>, which is the shape the plan asked
    /// for. With nothing held, nothing proposes anything and every failure mints.
    /// Once the outcome space is covered, the right answer is usually proposed by
    /// something among the hundreds that fire, and genesis stops on its own rather than
    /// against a number somebody chose.
    /// </para>
    /// </remarks>
    public Surprising Surprising { get; init; } = Surprising.Unaccounted;

    /// <summary>What a separating condition must do besides separate.</summary>
    /// <remarks>
    /// <b>Anything is the control and is every earlier reading.</b> See <see cref="Admitting"/>
    /// for why a condition that separates perfectly can still be a memorisation.
    /// </remarks>
    public Admitting Admitting { get; init; } = Admitting.Anything;

    /// <summary>How wide a scope genesis may mint.</summary>
    /// <remarks>
    /// <b>Singly is the control and is every earlier reading.</b> See <see cref="Rooting"/> for
    /// what the wide arm is for and where it is dead weight.
    /// </remarks>
    public Rooting Rooting { get; init; } = Rooting.Wholly;

    /// <summary>Whether a mint is credited with the round that minted it.</summary>
    /// <remarks>
    /// <b>Nothing is the control and is every earlier reading.</b> See <see cref="Crediting"/>
    /// for what a blank record costs and what crediting risks.
    /// </remarks>
    public Crediting Crediting { get; init; } = Crediting.Nothing;

    /// <summary>Whether the machine answers with nothing behind the answer.</summary>
    /// <remarks>
    /// <b>Grounded ships and Anyway is the control</b>, which is every reading taken before
    /// this existed. See <see cref="Deciding"/> for why a weight of nought is a signature
    /// rather than a level.
    /// </remarks>
    public Deciding Deciding { get; init; } = Deciding.Grounded;

    /// <summary>How fast the local estimate forgets, in 0..1.</summary>
    /// <remarks>
    /// <b>Widrow-Hoff's rate, and fork 27 in one number.</b> At one it remembers only
    /// the last firing; near zero it becomes the lifetime average the G-Counters
    /// already provide, and the second estimate stops earning its keep.
    /// </remarks>
    public double Recency { get; init; } = 0.1;

    /// <summary>How many misses before a commitment may be repaired at all.</summary>
    /// <remarks>
    /// <b>Below it no test of a proportion has any power</b>, so a gate that admitted
    /// repairs there would be admitting them on nothing.
    /// </remarks>
    public int Floor { get; init; } = 20;

    /// <summary>How much noise the separation bar admits, before correction.</summary>
    public double Alpha { get; init; } = 0.05;

    /// <summary>How many times one commitment may ever separate.</summary>
    /// <remarks>
    /// <para>
    /// <b>It was written down as a runaway guard</b>, and it is a level. At eight it
    /// bound before it guarded — repairs flat from twenty thousand rounds to four
    /// hundred thousand, and the score flat with it. Raised, and the same shape
    /// appeared one width up: at twenty bits it plateaus under this and crosses the
    /// target when it is loosened.
    /// </para>
    /// <para>
    /// <b>And removing it is worse at every width</b>, so it is not a cap to delete
    /// either — unbounded repair over-specialises and the score falls. An interior
    /// optimum is what a LEVEL has, and <c>BudgetCurveTests</c> is where it is read.
    /// </para>
    /// <para>
    /// <b>And the optimum does not move with the relevant bits</b>, which is the puzzle this
    /// number carried for the life of the branch. Six bits and eleven both peak on the
    /// 128-to-256 plateau and both are worse free, so this is not a per-world number and
    /// no world is reaching into the brain through it. The moving optimum was measured
    /// under <see cref="Repairing.AfterFailure"/>, where the lineages that would have spent
    /// the budget were never blamed — so what moved with the width was the blame coupling,
    /// which is the third thing that coupling has turned out to be.
    /// </para>
    /// <para>
    /// <b>So sixty-four was the level and it was too low</b>, measured at four worlds. At
    /// eleven bits even this leads it by 2.6 standard errors of trailing accuracy, 2.9 of
    /// sound rules and 3.8 of hard-round coverage; at six bits it is level on accuracy and
    /// ahead on coverage. Nothing measured is worse.
    /// </para>
    /// <para>
    /// <b>And then it fell from two hundred and fifty-six to eight</b>, which is not a retraction
    /// of any of that. Every level above the vocabulary was the same arm while a parent
    /// re-proposed the same child, so the readings above are about how much RE-DERIVATION a
    /// lineage was allowed. Under <see cref="Forking.Distinct"/> an attempt buys a new child
    /// and this caps the search for the first time — which is why the optimum moved from
    /// "as large as measured" to a small number, and why fork 66's answer was right about the
    /// machine it was taken on.
    /// </para>
    /// <para>
    /// <b>Eight rather than four on three columns of four</b>, and neither is free. Two is
    /// worse than repeating on every world; free floods `Arranged` to its capacity and is five
    /// standard errors down there. The two dials are chosen together or not at all.
    /// </para>
    /// </remarks>
    public int Budget { get; init; } = 8;

    /// <summary>How many commitments may be resident before the worst are dropped.</summary>
    public int Capacity { get; init; } = 2000;

    /// <summary>Whether repair waits for the VOTE to be wrong, or only for a commitment to be.</summary>
    /// <remarks>
    /// <para>
    /// <b>The plan already says these are two different things</b>, and the code treats them
    /// as one. <i>An outvoted commitment still accrues its own hits and misses, which
    /// keeps C1 and stops the winner monopolising the learning.</i> It accrues them and
    /// then cannot act on them: covering and repair run only on a round the WINNER got
    /// wrong, so a commitment that fired, was wrong, and was outvoted banks a miss it can
    /// never spend.
    /// </para>
    /// <para>
    /// <b>Which makes the machine's search depth a function of its own accuracy</b>, and
    /// nobody designed that. Measured rather than suspected:
    /// concentrating the vote costs 169 repairs to 105 at six bits and 12.3 true rules to
    /// 8.7 at eleven, and on a noisy world where both arms fail the same rounds they
    /// repair identically and differ only in score.
    /// </para>
    /// <para>
    /// <b>And the gate turns out to be load-bearing</b>, which refutes the paragraph above as
    /// an argument for removing it. On <c>Worlds.Multiplexer</c> at eleven bits,
    /// dropping it takes 0.944 to 0.983 under the best-advocate vote. On
    /// <c>Worlds.Arranged</c> it is a disaster: 1.000 falls to 0.752, because the
    /// gated arm repairs NINE times in twenty thousand rounds and the ungated one mints
    /// 1,349 children that then compete in the vote. Sound rules rose from 36 to 137 and
    /// unsound ones from 178 to 325, and the score went with the second number.
    /// </para>
    /// <para>
    /// <b>So <i>only fix what is broken</i> is the right rule</b>, and this is another dial
    /// whose value moves with the world — the disease and not the cure. Where the true
    /// rules are one code, repair has nothing useful to do and the brake protects the
    /// population from itself; where they are three-code conjunctions, the brake starves
    /// the only mechanism that can reach them. The readout cannot tell those apart because
    /// being right and having nothing left to learn are the same observation to it.
    /// </para>
    /// <para>
    /// <b>And the plan already names the signal that could.</b> Fork 37: <i>the driver
    /// nobody has wired is whether a parent still has failures no child covers</i>. That
    /// is vote-independent and world-independent, and it separates the two cases exactly —
    /// a sound one-code rule has no uncovered failures and would not be repaired; an
    /// over-general one has them and would. It is the honest answer to all three of these
    /// dials and it is still unwired.
    /// </para>
    /// <para>
    /// <b>And <see cref="Mending.Uncovered"/> is not "repair every round".</b>
    /// <see cref="Population.Repair"/> already refuses anything under
    /// <see cref="Floor"/> misses, over <see cref="Budget"/> children, or without a
    /// condition past the separation bar and its correction. Every gate the design
    /// specifies stays; what goes is the one it did not.
    /// </para>
    /// <para>
    /// <b>Genesis is not moved with it, and that is deliberate.</b> It mints per
    /// live code, so running it on every round would walk the whole
    /// <c>code → outcome</c> space -- the refutation that put `Surprising` back. Repair
    /// adds ONE child to ONE parent that has already earned it, which is a bounded thing
    /// to try.
    /// </para>
    /// </remarks>
    public Mending Mending { get; init; } = Mending.Ungated;

    /// <inheritdoc cref="Commitments.Repairing"/>
    public Repairing Repairing { get; init; } = Repairing.EveryRound;

    /// <inheritdoc cref="Commitments.Budgeting"/>
    public Budgeting Budgeting { get; init; } = Budgeting.Attempts;

    /// <summary>What it takes for a narrower commitment to survive beside a general one.</summary>
    /// <remarks>
    /// <b>The arm for a clause that has never fired.</b> See <see cref="Subsuming"/>: the
    /// design's one preference for generality requires the general rule to be at least as
    /// accurate, and a memorised child is always a shade better. Whether demanding
    /// SIGNIFICANCE instead is what the plan meant all along is a measurement rather than
    /// a reading of it.
    /// </remarks>
    public Subsuming Subsuming { get; init; } = Subsuming.Weaker;

    /// <summary>How the condition to add is picked.</summary>
    /// <remarks>
    /// <b>The most important comparison in step one</b>, and it is a choice between two
    /// rules rather than a level. If choosing the separating condition does not
    /// beat choosing any condition the failures happened to contain, repair is doing
    /// nothing and the whole bet is dead — and without the second arm a run cannot
    /// tell a mechanism from the narrowing that ANY added condition buys.
    /// </remarks>
    public Choosing Choosing { get; init; } = Choosing.Separating;

    /// <inheritdoc cref="Commitments.Coarsening"/>
    public Coarsening Coarsening { get; init; } = Coarsening.Judged;

    /// <inheritdoc cref="Commitments.Forking"/>
    public Forking Forking { get; init; } = Forking.Distinct;

    /// <inheritdoc cref="Commitments.Broadening"/>
    public Broadening Broadening { get; init; } = Broadening.Named;

}

/// <summary>
/// Which condition to add to a commitment that is failing, and whether to add one at
/// all.
/// </summary>
/// <remarks>
/// <para>
/// <b>The gate is the whole difference between this and overfitting.</b> There is
/// ALWAYS a code that separates the misses from the hits better than the others do,
/// so an ungated repair mints a child at every failure and specialises until each
/// commitment covers one instance. That is ILP's cause of death, and it is the same
/// machine that minted 715 names on `csharp`'s pure-noise control.
/// </para>
/// <para>
/// <b>The correction is the part that matters.</b> Testing four hundred candidates
/// and keeping the best one clears any fixed bar on noise alone — the bar has to be
/// paid for out of how many candidates were looked at, or it is decorative.
/// </para>
/// </remarks>
internal static class Conditions
{
    /// <summary>
    /// The code to add to a commitment's scope, or nothing if none has earned it.
    /// </summary>
    /// <param name="parent">The commitment that is failing.</param>
    /// <param name="dials">The gate's numbers.</param>
    /// <param name="blind">The control arm's generator, when it is running.</param>
    /// <param name="spent">
    /// Codes this parent has already forked on, refused under
    /// <see cref="Forking.Distinct"/> — and nothing at all under the rule that ships.
    /// </param>
    /// <remarks>
    /// <para>
    /// <b>The condition must be more present in the hits</b>, which is the opposite of
    /// what is easy to say. A conjunctive child <c>X and Z</c> keeps the firings
    /// where Z was there, so to keep the hits and shed the misses, Z has to be what
    /// the hits had. A code that is more present in the MISSES is the right condition
    /// for a NEGATED one — <c>X and not Z</c> — which is rung two and is not built.
    /// Getting this backwards mints a child that is reliably wrong.
    /// </para>
    /// <para>
    /// <b>The other row of the two-by-two is never stored.</b> How often a code was
    /// absent in each is the commitment's own counts minus what
    /// <see cref="Commitment.Separations"/> holds.
    /// </para>
    /// </remarks>
    public static Code? Discriminator(
        Commitment parent,
        CommittingSettings dials,
        Random? blind,
        IReadOnlySet<Code>? spent = null)
    {
        ArgumentNullException.ThrowIfNull(parent);
        ArgumentNullException.ThrowIfNull(dials);

        if (parent.Misses < dials.Floor || parent.Hits == 0) return null;

        // What this parent has already forked on, and only where the arm asks. Read once
        // here rather than at each comparison, so the shipped rule pays a null check and
        // not a lookup a candidate.
        var refuse = dials.Forking == Forking.Distinct ? spent : null;

        if (dials.Choosing == Choosing.Present)
        {
            ArgumentNullException.ThrowIfNull(blind);

            // The arm draws from the codes present in the misses, which is the
            // fairest control available: an arm drawing from every code would lose
            // to anything at all, and beating a straw man says nothing.
            // And it honours the same refusal, or the control would be searching a wider
            // set than the mechanism and the comparison would be about two things.
            var present = parent.Separations
                .Where(one => one.Value.InMisses > 0)
                .Select(one => one.Key)
                .Where(code => refuse?.Contains(code) != true)
                .Order()
                .ToList();

            return present.Count == 0 ? null : present[blind.Next(present.Count)];
        }

        Code? best = null;
        var strongest = 0.0;
        var candidates = 0;

        foreach (var (code, seen) in parent.Separations)
        {
            // Counted before the refusal, which is the whole of why the bar stays honest.
            // A Bonferroni over the un-spent codes alone would loosen as a parent forks, so
            // its tenth child would enter on a bar its first never had to clear. The search
            // that reached the tenth candidate is the same search.
            candidates++;

            if (refuse?.Contains(code) == true) continue;

            // A MINTED NAME under the arm that asks whether repair should reach for one, and
            // counted before it is refused for the same reason the line above is: a bar that
            // loosened when the arm changed would be two things moving, and the whole of this
            // arm is which code may be CHOSEN.
            //
            // This is where a name reaches the population, which the arm's first shape got
            // wrong. Genesis was the suspect and genesis never roots on one -- every one-code
            // scope over a name comes from the REWRITE rather than from covering, so
            // excluding it there changed not one number on either world.
            if (dials.Broadening == Broadening.Unrooted && Naming.Names(code)) continue;

            // And the child has to be judgeable, where the arm asks. A conjunctive child keeps
            // the parent's firings this code was present in, so fewer of those than the floor
            // is a rule that can never clear the floor itself -- one nothing will ever be able
            // to refute, which is what memorising is. Counted among the candidates first, for
            // the same reason the refusal is: the search that reached it is the same search.
            if (dials.Admitting == Admitting.Testable && seen.InHits < dials.Floor) continue;

            var z = Divergence(seen.InHits, parent.Hits, seen.InMisses, parent.Misses);

            if (z <= strongest) continue;

            strongest = z;
            best = code;
        }

        if (best is null || candidates == 0) return null;

        // Bonferroni, and deliberately the blunt one. A sharper correction would be
        // less conservative and harder to argue about; what this has to survive is
        // somebody asking whether the bar was paid for, and the blunt answer is the
        // one that is obviously yes.
        return Normal.Tail(strongest) * candidates <= dials.Alpha ? best : null;
    }

    /// <summary>
    /// The code this parent's table would have picked SECOND — <b>an instrument for fork 74
    /// and it mints nothing.</b>
    /// </summary>
    /// <param name="parent">The commitment being asked about.</param>
    /// <param name="dials">Every number the machinery is allowed to have.</param>
    /// <remarks>
    /// <para>
    /// <b>Fork 74 asks whether a chain could be walked in one pass</b>, and this is the half of
    /// it that is decidable. A four-code truth costs three miss floors because a fresh
    /// child re-earns one before it may add the next. If a parent's table could pick both
    /// codes at once the chain would collapse — but the table is not conditioned on the
    /// first code, so whether it can is a question about the world rather than about the
    /// arithmetic.
    /// </para>
    /// <para>
    /// <b>So the reading is whether this agrees with the child's later choice.</b> A
    /// child picks from its OWN table, re-earned over its own firings and therefore
    /// conditioned on the code its parent added. Agreement means one table could have picked
    /// both and the pass is a saving; disagreement means conditioning is what the second
    /// choice needed, which is exactly what a chain buys and a single pass cannot.
    /// </para>
    /// <para>
    /// <b>And it is subject to the same bar as the winner</b>, or it would be comparing a
    /// certified choice against an uncertified one. The correction divides among the same
    /// candidate count, because the search that found the runner-up is the same search.
    /// </para>
    /// </remarks>
    public static Code? Runner(Commitment parent, CommittingSettings dials)
    {
        ArgumentNullException.ThrowIfNull(parent);
        ArgumentNullException.ThrowIfNull(dials);

        if (parent.Misses < dials.Floor || parent.Hits == 0) return null;

        Code? best = null, second = null;
        double strongest = 0.0, next = 0.0;
        var candidates = 0;

        foreach (var (code, seen) in parent.Separations)
        {
            candidates++;

            var z = Divergence(seen.InHits, parent.Hits, seen.InMisses, parent.Misses);

            if (z > strongest)
            {
                (second, next) = (best, strongest);
                (best, strongest) = (code, z);
            }
            else if (z > next)
            {
                (second, next) = (code, z);
            }
        }

        return second is not null && Normal.Tail(next) * candidates <= dials.Alpha
            ? second
            : null;
    }


    /// <summary>
    /// The code whose ABSENCE separates this commitment's misses from its hits, or
    /// nothing — <b>rung two's candidate, as an instrument rather than a rung.</b>
    /// </summary>
    /// <param name="parent">The commitment being asked about.</param>
    /// <param name="dials">Every number the machinery is allowed to have.</param>
    /// <remarks>
    /// <para>
    /// <b>Nothing calls this to mint anything, and that is the point.</b> The plan's rule
    /// is that a rung is admitted when no expression in the current language separates the
    /// failures from the hits, and that choosing one before a failure asks is
    /// hand-specified bias by a side door. So the honest order is to measure whether the
    /// rounds nothing separates would have separated on an ABSENCE, and to build the rung
    /// only if they would. This answers that and changes no behaviour.
    /// </para>
    /// <para>
    /// <b>The same statistic with its arguments swapped</b>, which is the whole of the
    /// difference. <see cref="Discriminator"/> wants a code that leads in the HITS,
    /// because a conjunctive child keeps the firings that code was in. A negated child
    /// keeps the firings it was ABSENT from, so its condition must lead in the MISSES —
    /// the plan says this in one sentence and says that conflating the two is how one
    /// description fits neither.
    /// </para>
    /// <para>
    /// <b>And the candidate set is bounded by what has been seen here.</b> Under sparse
    /// coding almost every code is absent from almost every moment, so a negated candidate
    /// drawn from everything would be hopeless and the correction below would be paying for
    /// a search nobody should have run. <i>Z, which I have seen here before, is missing
    /// now</i> — so a code with no hits behind it is not admissible, and the table repair
    /// already keeps is where that is read.
    /// </para>
    /// <para>
    /// <b>It costs a second walk of the table</b>, and only where nothing separated, which
    /// is a small share of a small share of rounds. Said out loud because the walk it
    /// mirrors is deliberately last in its own chain for exactly this reason.
    /// </para>
    /// </remarks>
    /// <returns>The code whose absence separates, or nothing.</returns>
    public static Code? Absent(Commitment parent, CommittingSettings dials)
    {
        ArgumentNullException.ThrowIfNull(parent);
        ArgumentNullException.ThrowIfNull(dials);

        if (parent.Misses < dials.Floor || parent.Hits == 0) return null;

        Code? best = null;
        var strongest = 0.0;
        var candidates = 0;

        foreach (var (code, seen) in parent.Separations)
        {
            // Seen here before, or it is not a thing that is missing.
            if (seen.InHits == 0) continue;

            candidates++;

            var z = Divergence(seen.InMisses, parent.Misses, seen.InHits, parent.Hits);

            if (z <= strongest) continue;

            strongest = z;
            best = code;
        }

        if (best is null || candidates == 0) return null;

        // the same blunt correction, over the admissible set rather than the whole table.
        return Normal.Tail(strongest) * candidates <= dials.Alpha ? best : null;
    }

    /// <summary>
    /// How many standard errors one hit rate leads another, positive when the first leads.
    /// </summary>
    /// <param name="hits">Firings the first one got right.</param>
    /// <param name="fired">Firings the first one settled at all.</param>
    /// <param name="otherHits">Firings the second one got right.</param>
    /// <param name="otherFired">Firings the second one settled at all.</param>
    /// <remarks>
    /// <b>THE SAME ARITHMETIC AS <see cref="Divergence"/> under a name that fits the
    /// second use.</b> That one asks whether a code was present more often in the hits
    /// than in the misses; this asks whether one commitment is more accurate than
    /// another. Both are the pooled two-proportion z, and calling it by the first
    /// question's parameter names at the second question's call site is how a formula
    /// quietly gets used for something it does not answer.
    /// </remarks>
    public static double Ahead(long hits, long fired, long otherHits, long otherFired) =>
        Divergence(hits, fired, otherHits, otherFired);

    /// <summary>
    /// How many standard errors apart two shares are, positive when the first leads.
    /// </summary>
    /// <param name="inHits">Firings that hit with the code present.</param>
    /// <param name="hits">Firings that hit.</param>
    /// <param name="inMisses">Firings that missed with the code present.</param>
    /// <param name="misses">Firings that missed.</param>
    /// <remarks>
    /// <para>
    /// <b>The pooled two-proportion z, one-sided.</b> One-sided because the question
    /// is not whether the shares differ — it is whether the code was there when the
    /// commitment was RIGHT, and a code that leads the other way is evidence for a
    /// rung this design has not built.
    /// </para>
    /// <para>
    /// <b>And this is why no frequency weight belongs here</b>, which is worth saying beside
    /// the arithmetic because the proposal is a natural one and keeps arriving. A rare word
    /// looks important because it DISCRIMINATES, and rarity is a proxy for that; this reads
    /// discrimination directly, off the code's own hits and misses. Weighting the bar by how
    /// rare a code is would put an estimate on top of the measurement it estimates.
    /// </para>
    /// <para>
    /// <b>The curve the proposal is reaching for is already here</b>, and it is a confidence
    /// cliff rather than a frequency one. Luhn's is the classical shape — resolving power
    /// rising with rarity, peaking, then falling — and its lower cutoff exists to throw away
    /// what is too rare to be sure about. That cutoff is what a significance test does
    /// without anybody drawing it, so the drop-off arrives for the right reason.
    /// </para>
    /// <para>
    /// <b>Where rarity does belong is search ORDER</b>, never admission. Repair ranks
    /// candidates under a budget, and a prior that tries the discriminating ones first can
    /// only find a good condition sooner — it can never let a bad one through, because this
    /// still decides. Unbuilt, and it is the safe half of the idea.
    /// </para>
    /// </remarks>
    public static double Divergence(long inHits, long hits, long inMisses, long misses)
    {
        if (hits <= 0 || misses <= 0) return 0.0;

        var inHit = inHits / (double)hits;
        var inMiss = inMisses / (double)misses;

        var pooled = (inHits + inMisses) / (double)(hits + misses);

        var spread = pooled * (1.0 - pooled) * ((1.0 / hits) + (1.0 / misses));

        return spread <= 0.0 ? 0.0 : (inHit - inMiss) / Math.Sqrt(spread);
    }
}

/// <summary>The one piece of the normal distribution this needs.</summary>
/// <remarks>
/// <b>Written out because the framework does not carry it</b>, and a bar that cannot
/// be turned into a probability is a bar nobody can argue about.
/// </remarks>
internal static class Normal
{
    /// <summary>The chance of exceeding this many standard errors, one-sided.</summary>
    /// <param name="z">How many standard errors.</param>
    public static double Tail(double z) => 0.5 * Erfc(z / Math.Sqrt(2.0));

    /// <summary>The complementary error function, to about seven figures.</summary>
    /// <param name="x">Where to evaluate it.</param>
    /// <remarks>
    /// <b>The Chebyshev fit from Numerical Recipes</b>, whose fractional error is
    /// below 1.2e-7 everywhere — far tighter than any bar this is compared against,
    /// and it needs no table.
    /// </remarks>
    public static double Erfc(double x)
    {
        var z = Math.Abs(x);
        var t = 2.0 / (2.0 + z);
        var y = t * Math.Exp(
            (-z * z) - 1.26551223 + (t * (1.00002368 + (t * (0.37409196 + (t * (0.09678418
            + (t * (-0.18628806 + (t * (0.27886807 + (t * (-1.13520398 + (t * (1.48851587
            + (t * (-0.82215223 + (t * 0.17087277))))))))))))))))));

        return x >= 0.0 ? y : 2.0 - y;
    }
}
