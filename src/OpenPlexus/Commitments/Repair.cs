using OpenPlexus.Codes;

namespace OpenPlexus.Commitments;

/// <summary>Which rule picks the condition a repair adds.</summary>
/// <remarks>
/// <b>Two rules that both do something, rather than a mechanism and its own
/// absence.</b> A boolean here would put the control arm in the code forever as a
/// way of not running repair properly; naming both makes it a comparison.
/// </remarks>
public enum Choosing
{
    /// <summary>The condition that most separates the hits from the misses.</summary>
    Separating,

    /// <summary>Any condition the failures contained, drawn uniformly.</summary>
    Present,
}

/// <summary>What counts as surprising enough to mint on.</summary>
/// <remarks>
/// <b>Two rules that both do something, exactly as <see cref="Choosing"/> is.</b> A
/// boolean would put the arm in the code forever as a way of not running genesis
/// properly; naming both makes it a comparison.
/// </remarks>
public enum Surprising
{
    /// <summary>Any failure at all. What ran before anything gated this.</summary>
    AnyFailure,

    /// <summary>Nothing that fired even proposed what arrived.</summary>
    Unaccounted,
}

/// <summary>How the advocates for one expectation are added up.</summary>
public enum Weighing
{
    /// <summary>Every advocate adds its weight. What ran before anything questioned it.</summary>
    Summing,

    /// <summary>An expectation is worth its best advocate and no more.</summary>
    Strongest,
}

/// <summary>What has to go wrong before repair is attempted at all.</summary>
public enum Mending
{
    /// <summary>The vote had to be wrong. What ran before anything questioned it.</summary>
    Outvoted,

    /// <summary>
    /// And no child of it may have fired — the failure has to be one nothing accounts for.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>FORK 37'S DRIVER, AND IT NEEDS NO BOOKKEEPING AT ALL.</b> <i>Whether a parent
    /// still has failures no child covers</i> reads like it wants a parent-to-children
    /// index. It does not: a child's scope is its parent's plus a condition, so a child
    /// can only fire where the parent fires. If no commitment among those firing NARROWS
    /// this one, then this failure is in no child's territory — which is the whole of the
    /// question, answered from the firing set.
    /// </para>
    /// <para>
    /// <b>IT IS WHAT THE OTHER TWO ARE STANDING IN FOR.</b> The readout cannot tell being
    /// right from having nothing left to learn, so <see cref="Outvoted"/> stops repairing
    /// a world it has already solved and also stops repairing one it has not.
    /// This asks the question directly and is indifferent to the vote and to the world.
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
    /// The vote had to be wrong AND no child of the culprit may have fired.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>THE CONJUNCTION OF THE OTHER TWO, AND IT IS NOT A FOURTH FREE PARAMETER.</b>
    /// Each of the signals above is right on one world and wrong on the other, and they
    /// fail in opposite directions: <see cref="Outvoted"/> asks <i>did the population get
    /// this wrong</i>, which is too coarse where the answer needs specialising;
    /// <see cref="Uncovered"/> asks <i>has anything taken responsibility for this
    /// failure</i>, which is too eager where a child covers only the sliver its extra
    /// code fires in. Neither is a level, so combining them adds nothing to tune.
    /// </para>
    /// <para>
    /// <b>AND THE MEASUREMENTS SAY EXACTLY WHERE EACH BREAKS.</b> On
    /// <see cref="Worlds.Arranged"/> the vote is already right, so <see cref="Outvoted"/>
    /// repairs nine times in twenty thousand rounds and holds 1.000 while
    /// <see cref="Uncovered"/> mints 1,349 children and falls to 0.752. On
    /// <see cref="Worlds.Multiplexer"/> at eleven bits it is the reverse: 0.944 against
    /// 0.983, with 90 sound rules to 80 and 13.0 of the key found to 12.3.
    /// </para>
    /// <para>
    /// <b>AND IT KEEPS ONE HALF AND LOSES THE OTHER, WHICH SETTLES THE QUESTION AGAINST
    /// ALL THREE.</b> On <see cref="Worlds.Arranged"/> it is 1.000 and byte-identical to
    /// <see cref="Outvoted"/>, because a vote that is already right makes the conjunction
    /// collapse to it. On the multiplexer it gives back nearly everything: 0.939 at eleven
    /// bits against 0.983, and 0.922 at six against 0.990.
    /// </para>
    /// <para>
    /// <b>AND NOT BY REPAIRING LESS, WHICH IS THE PART WORTH KEEPING.</b> It makes 1,158
    /// repairs to <see cref="Uncovered"/>'s 1,182 and gets 61 sound rules to 90, 10.0 of
    /// the key to 13.0. It repairs the WRONG PARENTS. The rules most in need of
    /// specialising are the ones individually wrong while the population is collectively
    /// right — so gating on the population being wrong is precisely the filter that hides
    /// them, and on a world where the answer is already correct those same rules are
    /// harmless and forking them only adds voters.
    /// </para>
    /// <para>
    /// <b>SO NO COMBINATION OF THESE THREE IS BEST ON BOTH, AND UNDER NOISE A THIRD ONE
    /// WINS.</b> <see cref="Outvoted"/> takes the arranged world, <see cref="Uncovered"/>
    /// takes the clean multiplexer, and this takes the noisy one — 0.771 against 0.737 and
    /// 0.725. Three worlds, three winners. The answer is not a better gate on the round;
    /// it is that WHICH RULE NEEDS SPECIALISING and WHETHER THE POPULATION GOT THIS WRONG
    /// are different questions whose alignment is a fact about the world&apos;s rule
    /// structure, and no per-round switch can stand in for both.
    /// </para>
    /// </remarks>
    Neglected,

    /// <summary>
    /// And only while forking this commitment has ever paid.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A PER-COMMITMENT SIGNAL RATHER THAN A PER-ROUND GATE, WHICH IS WHAT FORK 45
    /// ASKS FOR.</b> <see cref="Uncovered"/> is the best thing measured on the clean
    /// multiplexer and ruinous on <see cref="Worlds.Arranged"/>, and the difference is
    /// not which rounds it fires on — it is that on one world specialising WORKS and on
    /// the other there is nothing to gain. Nothing in the machine was asking which.
    /// </para>
    /// <para>
    /// <b>AND THE ANSWER IS ALREADY LYING ABOUT IN THE POPULATION.</b> A parent whose
    /// children are no more accurate than itself has learnt that splitting it buys
    /// nothing; one whose children beat it has learnt the opposite. That is a fact the
    /// commitments already hold about themselves, it needs no world to interpret it, and
    /// it is the only feedback in reach that distinguishes <i>this rule needs
    /// specialising</i> from <i>this rule is simply being outvoted</i>.
    /// </para>
    /// <para>
    /// <b>THE FIRST FORK IS ALWAYS ALLOWED, BECAUSE THE SIGNAL DOES NOT EXIST UNTIL IT
    /// HAS BEEN TRIED.</b> A parent with no children has no evidence either way, and
    /// refusing on no evidence would make this a way of never repairing at all — which
    /// is the shape of arm this repo deletes rather than keeps.
    /// </para>
    /// <para>
    /// <b>AND IT SHOULD HAVE MADE <c>Budget</c> UNNECESSARY. IT DID NOT SELF-LIMIT AT
    /// ALL.</b> It is the best thing measured on the clean multiplexer at both widths,
    /// and on <see cref="Worlds.Arranged"/> it repairs MORE than the arm it was built to
    /// restrain and lands in the same place.
    /// </para>
    /// <para>
    /// <b>BECAUSE A MEMORISED CHILD BEATS ITS PARENT EASILY, WHICH MAKES THE SIGNAL
    /// ACTIVELY MISLEADING RATHER THAN MERELY WEAK.</b> A narrow child that has stored a
    /// corner of the drawn set is far more accurate than the general parent it came
    /// from, so <i>has forking paid</i> answers YES forever — and every child it licenses
    /// is a voter that is wrong about the world.
    /// </para>
    /// <para>
    /// <b>WHICH IS THE SAME FAULT AS EVERY OTHER GATE HERE, AND SAYING SO IS WORTH MORE
    /// THAN A FOURTH ATTEMPT.</b> <see cref="Outvoted"/>, <see cref="Uncovered"/>,
    /// <see cref="Neglected"/> and this are all built from observed accuracy or observed
    /// failure, and on a world whose drawn set can be memorised every one of them is
    /// fooled the same way. The instrument that is not fooled is soundness by
    /// enumeration, and that belongs to the experimenter rather than to the machine.
    /// </para>
    /// <para>
    /// <b>SO THE NEXT THING TO TRY IS NOT A GATE.</b> Generality is the only proxy for
    /// held-out truth the machine has, and the one mechanism that prefers it —
    /// subsumption — requires the general rule to be AT LEAST AS ACCURATE. A memorised
    /// child is always a shade better, so the clause never fires. Subsuming where the
    /// child's advantage is not significant, using the two-proportion test repair
    /// already owns, is the shape that follows from all of this.
    /// </para>
    /// </remarks>
    Improving,
}

/// <summary>Every number the commitment machinery is allowed to have.</summary>
public sealed record CommittingSettings
{
    /// <summary>
    /// What has to be true of a failure before genesis mints on it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>THE PLAN SAYS `Surprise` GATES GENESIS AND HAS NEVER RUN, AND ON A WIDE
    /// FRONT END THAT IS NOT A DETAIL.</b> Covering mints one commitment per live code,
    /// and a winnowed eight-by-eight thumbnail is 128 live codes over a sheet of 2,560
    /// cells. With ten outcomes the complete one-code space is 25,600 — and minting on
    /// every failure took the population to 23,762 against a capacity of 2,000. Genesis
    /// was not promiscuous, it was ENUMERATING, and a complete enumeration of
    /// <c>code → outcome</c> is a lookup table however it is scored.
    /// </para>
    /// <para>
    /// <b>AND THE DIVISION OF LABOUR IS XCS'S OWN.</b> Covering exists for a moment
    /// nothing accounts for; the wrongness of a rule that DID account for it is what
    /// repair is for. A failure where something fired and proposed the right answer and
    /// was outvoted needs no new commitment — it needs the vote to weigh better, which
    /// accuracy already does. Minting there fills the population with restatements of
    /// claims it already holds.
    /// </para>
    /// <para>
    /// <b>SO IT IS PROMISCUOUS EARLY AND QUIET LATE, WHICH IS THE SHAPE THE PLAN ASKED
    /// FOR.</b> With nothing held, nothing proposes anything and every failure mints.
    /// Once the outcome space is covered, the right answer is usually proposed by
    /// something among the hundreds that fire, and genesis stops on its own rather than
    /// against a number somebody chose.
    /// </para>
    /// </remarks>
    public Surprising Surprising { get; init; } = Surprising.Unaccounted;

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

    /// <summary>How many children one commitment may ever mint.</summary>
    /// <remarks>
    /// <para>
    /// <b>IT WAS WRITTEN DOWN AS A RUNAWAY GUARD AND IT IS A LEVEL.</b> At eight it
    /// bound before it guarded — repairs flat from twenty thousand rounds to four
    /// hundred thousand, and the score flat with it. Raised, and the same shape
    /// appeared one width up: at twenty bits it plateaus under this and crosses the
    /// target when it is loosened.
    /// </para>
    /// <para>
    /// <b>AND REMOVING IT IS WORSE AT EVERY WIDTH</b>, so it is not a cap to delete
    /// either — unbounded repair over-specialises and the score falls. An interior
    /// optimum is what a LEVEL has, and this one moves with the number of relevant
    /// bits while nothing reads that.
    /// </para>
    /// </remarks>
    public int Budget { get; init; } = 64;

    /// <summary>How many commitments may be resident before the worst are dropped.</summary>
    public int Capacity { get; init; } = 2000;

    /// <summary>How sharply the vote favours the accurate over the many.</summary>
    /// <remarks>
    /// <para>
    /// <b>THE STRENGTH-VERSUS-ACCURACY REFUTATION ARRIVES THROUGH THE VOTE, WHICH IS
    /// WHERE NOBODY LOOKS FOR IT.</b> Summing accuracy over everything that advocates
    /// an expectation lets three commitments that are right half the time outvote one
    /// that is always right — so the population's COUNT decides and its accuracy does
    /// not, which is strength-based fitness wearing a different hat.
    /// </para>
    /// <para>
    /// <b>Raising accuracy to a power before summing is XCS's own answer</b>, and it
    /// is why its fitness is a steep function of accuracy rather than accuracy
    /// itself. At one this is a plain sum and the fault is back.
    /// </para>
    /// </remarks>
    public double Sharpness { get; init; } = 5.0;

    /// <summary>Whether an expectation is worth its voters added up, or its best one.</summary>
    /// <remarks>
    /// <para>
    /// <b>AND <see cref="Sharpness"/> TURNS OUT TO BE A WORKAROUND FOR THE SHAPE OF THIS
    /// ONE.</b> A sum over N advocates scales with N however steeply each is weighted,
    /// so raising the power does not remove the count from the decision — it only makes
    /// the count need more members to win. The fault the doc above names is not that the
    /// weights are too flat; it is that the aggregate is a SUM.
    /// </para>
    /// <para>
    /// <b>WHICH IS WHY THE PEAK MOVES BETWEEN WORLDS, AND THAT IS THE PART THAT MATTERS.</b>
    /// On <see cref="Worlds.Arranged"/> the score reaches its exact target at a power of
    /// ten and sits a fifth short at five; on <see cref="Worlds.Multiplexer"/> five is the
    /// peak and twenty is worse at both widths. A dial with a per-world optimum is a
    /// world reaching into the brain by the back door — the one thing this design says
    /// it will not have — so the answer cannot be to tune it.
    /// </para>
    /// <para>
    /// <b><see cref="Weighing.Strongest"/> IS SCALE-FREE, WHICH IS THE PROPERTY BEING
    /// TESTED.</b> An expectation is worth its best advocate and no more, so a thousand
    /// mediocre rules cannot outvote one that is always right at ANY power, and the
    /// number of voters stops being part of the answer. Whether that costs the
    /// robustness a crowd buys is exactly what the arm is for.
    /// </para>
    /// </remarks>
    public Weighing Weighing { get; init; } = Weighing.Summing;

    /// <summary>Whether repair waits for the VOTE to be wrong, or only for a commitment to be.</summary>
    /// <remarks>
    /// <para>
    /// <b>THE PLAN ALREADY SAYS THESE ARE TWO DIFFERENT THINGS AND THE CODE TREATS THEM
    /// AS ONE.</b> <i>An outvoted commitment still accrues its own hits and misses, which
    /// keeps C1 and stops the winner monopolising the learning.</i> It accrues them and
    /// then cannot act on them: covering and repair run only on a round the WINNER got
    /// wrong, so a commitment that fired, was wrong, and was outvoted banks a miss it can
    /// never spend.
    /// </para>
    /// <para>
    /// <b>WHICH MAKES HOW HARD THE MACHINE SEARCHES A FUNCTION OF HOW GOOD ITS ANSWERS
    /// ALREADY ARE, AND NOBODY DESIGNED THAT.</b> Measured rather than suspected:
    /// concentrating the vote costs 169 repairs to 105 at six bits and 12.3 true rules to
    /// 8.7 at eleven, and on a noisy world where both arms fail the same rounds they
    /// repair identically and differ only in score.
    /// </para>
    /// <para>
    /// <b>AND THE GATE TURNS OUT TO BE LOAD-BEARING, WHICH REFUTES THE PARAGRAPH ABOVE AS
    /// AN ARGUMENT FOR REMOVING IT.</b> On <see cref="Worlds.Multiplexer"/> at eleven bits,
    /// dropping it takes 0.944 to 0.983 with <see cref="Weighing.Strongest"/>. On
    /// <see cref="Worlds.Arranged"/> it is a disaster: 1.000 falls to 0.752, because the
    /// gated arm repairs NINE times in twenty thousand rounds and the ungated one mints
    /// 1,349 children that then compete in the vote. Sound rules rose from 36 to 137 and
    /// unsound ones from 178 to 325, and the score went with the second number.
    /// </para>
    /// <para>
    /// <b>SO <i>ONLY FIX WHAT IS BROKEN</i> IS THE RIGHT RULE AND THIS IS ANOTHER DIAL
    /// WHOSE VALUE MOVES WITH THE WORLD — the disease and not the cure.</b> Where the true
    /// rules are one code, repair has nothing useful to do and the brake protects the
    /// population from itself; where they are three-code conjunctions, the brake starves
    /// the only mechanism that can reach them. The readout cannot tell those apart because
    /// being right and having nothing left to learn are the same observation to it.
    /// </para>
    /// <para>
    /// <b>AND THE PLAN ALREADY NAMES THE SIGNAL THAT COULD.</b> Fork 37: <i>the driver
    /// nobody has wired is whether a parent still has failures no child covers</i>. That
    /// is vote-independent and world-independent, and it separates the two cases exactly —
    /// a sound one-code rule has no uncovered failures and would not be repaired; an
    /// over-general one has them and would. It is the honest answer to all three of these
    /// dials and it is still unwired.
    /// </para>
    /// <para>
    /// <b>AND <see cref="Mending.Uncovered"/> IS NOT "REPAIR EVERY ROUND".</b>
    /// <see cref="Population.Mend"/> already refuses anything under
    /// <see cref="Floor"/> misses, over <see cref="Budget"/> children, or without a
    /// condition past the separation bar and its correction. Every gate the design
    /// specifies stays; what goes is the one it did not.
    /// </para>
    /// <para>
    /// <b>COVERING IS NOT MOVED WITH IT, AND THAT IS DELIBERATE.</b> Genesis mints per
    /// live code, so running it on every round would walk the whole
    /// <c>code → outcome</c> space -- the refutation that put `Surprising` back. Repair
    /// adds ONE child to ONE parent that has already earned it, which is a bounded thing
    /// to try.
    /// </para>
    /// </remarks>
    public Mending Mending { get; init; } = Mending.Outvoted;

    /// <summary>How the condition to add is picked.</summary>
    /// <remarks>
    /// <b>THE MOST IMPORTANT COMPARISON IN STEP ONE, and it is a choice between two
    /// rules rather than a level.</b> If choosing the separating condition does not
    /// beat choosing any condition the failures happened to contain, repair is doing
    /// nothing and the whole bet is dead — and without the second arm a run cannot
    /// tell a mechanism from the narrowing that ANY added condition buys.
    /// </remarks>
    public Choosing Choosing { get; init; } = Choosing.Separating;
}

/// <summary>
/// Which condition to add to a commitment that is failing, and whether to add one at
/// all.
/// </summary>
/// <remarks>
/// <para>
/// <b>THE GATE IS THE WHOLE DIFFERENCE BETWEEN THIS AND OVERFITTING.</b> There is
/// ALWAYS a code that separates the misses from the hits better than the others do,
/// so an ungated repair mints a child at every failure and specialises until each
/// commitment covers one instance. That is ILP's cause of death, and it is the same
/// machine that minted 715 names on `csharp`'s pure-noise control.
/// </para>
/// <para>
/// <b>THE CORRECTION IS THE PART THAT MATTERS.</b> Testing four hundred candidates
/// and keeping the best one clears any fixed bar on noise alone — the bar has to be
/// paid for out of how many candidates were looked at, or it is decorative.
/// </para>
/// </remarks>
public static class Repair
{
    /// <summary>
    /// The code to add to a commitment's scope, or nothing if none has earned it.
    /// </summary>
    /// <param name="parent">The commitment that is failing.</param>
    /// <param name="dials">The gate's numbers.</param>
    /// <param name="blind">The control arm's generator, when it is running.</param>
    /// <remarks>
    /// <para>
    /// <b>THE CONDITION MUST BE MORE PRESENT IN THE HITS, WHICH IS THE OPPOSITE OF
    /// WHAT IS EASY TO SAY.</b> A conjunctive child <c>X and Z</c> keeps the firings
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
    public static Code? Discriminator(Commitment parent, CommittingSettings dials, Random? blind)
    {
        ArgumentNullException.ThrowIfNull(parent);
        ArgumentNullException.ThrowIfNull(dials);

        if (parent.Misses < dials.Floor || parent.Hits == 0) return null;

        if (dials.Choosing == Choosing.Present)
        {
            ArgumentNullException.ThrowIfNull(blind);

            // THE ARM DRAWS FROM THE CODES PRESENT IN THE MISSES, which is the
            // fairest control available: an arm drawing from every code would lose
            // to anything at all, and beating a straw man says nothing.
            var present = parent.Separations
                .Where(one => one.Value.InMisses > 0)
                .Select(one => one.Key)
                .Order()
                .ToList();

            return present.Count == 0 ? null : present[blind.Next(present.Count)];
        }

        Code? best = null;
        var strongest = 0.0;
        var candidates = 0;

        foreach (var (code, seen) in parent.Separations)
        {
            candidates++;

            var z = Divergence(seen.InHits, parent.Hits, seen.InMisses, parent.Misses);

            if (z <= strongest) continue;

            strongest = z;
            best = code;
        }

        if (best is null || candidates == 0) return null;

        // BONFERRONI, AND DELIBERATELY THE BLUNT ONE. A sharper correction would be
        // less conservative and harder to argue about; what this has to survive is
        // somebody asking whether the bar was paid for, and the blunt answer is the
        // one that is obviously yes.
        return Normal.Tail(strongest) * candidates <= dials.Alpha ? best : null;
    }

    /// <summary>
    /// How many standard errors apart two shares are, positive when the first leads.
    /// </summary>
    /// <param name="inHits">Firings that hit with the code present.</param>
    /// <param name="hits">Firings that hit.</param>
    /// <param name="inMisses">Firings that missed with the code present.</param>
    /// <param name="misses">Firings that missed.</param>
    /// <remarks>
    /// <b>The pooled two-proportion z, one-sided.</b> One-sided because the question
    /// is not whether the shares differ — it is whether the code was there when the
    /// commitment was RIGHT, and a code that leads the other way is evidence for a
    /// rung this design has not built.
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
public static class Normal
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
