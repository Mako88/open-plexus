using OpenPlexus.Machines;
using OpenPlexus.Commitments;
using OpenPlexus.Worlds;
using Xunit.Abstractions;

namespace OpenPlexus.Tests;

/// <summary>
/// Step one, end to end, on the world it is judged on.
/// </summary>
/// <remarks>
/// <b>The arm is the point of this file.</b> A learner that specialises will get
/// better at almost anything, so a score on its own says nothing about whether
/// CHOOSING the condition did any work. Every claim here is made against the arm that
/// adds a condition drawn at random from the ones present in the failures.
/// </remarks>
public sealed class StepOneTests(ITestOutputHelper output)
{
    private const int Rounds = 30000;

    private static Learned Run(int address, Choosing choosing, int seed) =>
        new MultiplexerRun(
            new MultiplexerSettings { Address = address },
            new Brain(new CommittingSettings { Choosing = choosing }, seed),
            seed).Run(Rounds);

    [Fact]
    public void It_learns_the_six_bit_multiplexer()
    {
        var learned = Run(address: 2, choosing: Choosing.Separating, seed: 1);

        output.WriteLine(
            $"recent={learned.Recent:F3} resident={learned.Resident} "
            + $"sound={learned.Sound} unsound={learned.Unsound} found={learned.Found}");

        Assert.True(learned.Recent > 0.97, $"only {learned.Recent:F3} over the last tenth");

        // An accuracy can be reached by memorising, so the count goes beside it. The
        // world's own basis holds eight rules; a learner at ten thousand commitments
        // has not found the structure whatever it scores.
        Assert.True(learned.Resident < 100, $"{learned.Resident} commitments resident");

        // And soundness is the number this is actually judged on -- rules that are
        // TRUE of the world, checked by enumeration rather than against one basis.
        //
        // A raw count is the wrong bar and was set at one for a while. Subsumption
        // compresses, so a population that got BETTER holds fewer sound rules than
        // one that never dropped a redundant specific -- and the bar then punishes
        // exactly the mechanism it was meant to reward. The share is what survives
        // compression.
        Assert.True(learned.Sound > 10, $"only {learned.Sound} sound commitments");

        Assert.True(
            learned.Sound / (double)(learned.Sound + learned.Unsound) > 0.3,
            $"{learned.Sound} sound against {learned.Unsound} not");

        // Silence is a control arm nobody meant to run, and it is a handful of rounds at
        // the very start rather than one.
        //
        // It was one until genesis stopped rooting on codes that have never varied, and
        // the extra rounds are that gate's warm-up arriving where it was predicted to.
        // A code is only eligible once it has been ABSENT, and in the first moment nothing
        // has, so the earliest rounds mint less and there is briefly nothing to fire. Every
        // code here is present about half the time, so it resolves within a handful of
        // draws and never recurs.
        //
        // The bar is on the warm-up and not on the rate, which is why it stays this tight.
        // Twenty is still nothing against thirty thousand, and a run that went quiet LATER
        // would be a population being destroyed rather than a table filling up — that is
        // the failure this assertion is for, and it can still fire.
        Assert.True(learned.Silent <= 20, $"silent on {learned.Silent} rounds");
    }

    [Fact]
    public void Choosing_the_condition_beats_choosing_any_condition_present()
    {
        // The single most important number in step one. If discriminative-Z does not
        // beat random-Z, repair is doing nothing and the bet is dead -- every other
        // assertion in this file would be measuring the narrowing that ANY added
        // condition buys.
        //
        // Five seeds and counted in both directions, because a small sample can look
        // like a mechanism and can hide a real effect just as easily.
        var beaten = 0;

        foreach (var seed in new[] { 1, 2, 3, 4, 5 })
        {
            var gated = Run(address: 2, choosing: Choosing.Separating, seed);
            var blind = Run(address: 2, choosing: Choosing.Present, seed);

            output.WriteLine(
                $"seed={seed} gated={gated.Recent:F3}/{gated.Sound}/{gated.SoundByRepair} "
                + $"blind={blind.Recent:F3}/{blind.Sound}/{blind.SoundByRepair}");

            if (gated.Recent > blind.Recent) beaten++;

            // AND THE SHARPER STATEMENT: the blind arm never learns a rule that is
            // TRUE. It is not merely behind on score -- REPAIR gives it nothing sound
            // at all, at any budget, while minting far more than the gated arm does.
            //
            // Read off what repair produced rather than off the population, which is
            // what this always meant. A one-code scope cannot be true of this world, so
            // while genesis mints one code at a time the two are the same number and
            // counting the population answered a question about the operator. They come
            // apart the moment genesis may mint a wider scope: the whole moment decides
            // the multiplexer, so a wide root hands over a sound rule having learnt
            // nothing, and this assertion would read as repair working.
            Assert.Equal(0, blind.SoundByRepair);
        }

        Assert.Equal(5, beaten);
    }

    /// <summary>
    /// <b>What the wide root costs rung five</b>, which is the only rung that broadens.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Two files had to pin the narrow root to keep their baseline, because under the wide one
    /// the whole population names nothing at all — and that is a reading rather than a fixture
    /// problem. Rung five's trigger is REDUNDANCY: it looks for a sub-scope several
    /// commitments share and rewrites them in terms of it. Genesis minting the conjunction
    /// outright is one commitment where repair would have built a family, so there may be no
    /// family left to share anything.
    /// </para>
    /// <para>
    /// <b>Which would be a cost worth refusing a default over.</b> A specialise-only machine
    /// is arbitrarily accurate and conceptless, and rung five is the whole of the answer to
    /// that — so a root that pays on the conversation and starves the one broadening operator
    /// is not a trade this project may take quietly.
    /// </para>
    /// <para>
    /// <b>The kill line, written before the grid ran</b>: the wide root dies as a default if
    /// it names fewer than the narrow one does.
    /// </para>
    /// <para>
    /// <b>And the answer is a NULL</b>, which is said here rather than left to be read as a
    /// pass. Rung five names about a third of a name a run on this world, under either
    /// root, so what the grid establishes is that the wide root does not starve it HERE and
    /// nothing more. The two files that had to pin the narrow root build their populations on
    /// a sparse repair budget precisely to leave something nameable, and that is where the
    /// reading with teeth would be taken — against <c>SplitNamingTests</c>' own dials rather
    /// than against a world where the operator barely fires.
    /// </para>
    /// </remarks>
    [Fact]
    public void What_the_wide_root_costs_the_one_rung_that_broadens()
    {
        var named = new Dictionary<Rooting, List<int>>
        {
            [Rooting.Singly] = [], [Rooting.Wholly] = [],
        };

        var stacked = new Dictionary<Rooting, List<int>>
        {
            [Rooting.Singly] = [], [Rooting.Wholly] = [],
        };

        // The denominator beside the count, which `CheckingTests` bars printing without. An
        // absolute name count is capped by the sweep calendar as much as by the gate, so two
        // cells reporting the same number can be reporting it for opposite reasons.
        output.WriteLine(
            $"{"root",-8}{"seed",-6}{"named",8}{"stacked",9}{"eligible",10}"
            + $"{"per",12}{"sound",8}");

        foreach (var rooting in new[] { Rooting.Singly, Rooting.Wholly })
        foreach (var seed in new[] { 1, 2, 3 })
        {
            var ran = new MultiplexerRun(
                new MultiplexerSettings { Address = 2 },
                new Brain(new CommittingSettings { Rooting = rooting }, seed),
                seed).Run(Rounds);

            named[rooting].Add(ran.Named);
            stacked[rooting].Add(ran.Stacked);

            output.WriteLine(
                $"{rooting.ToString().ToLowerInvariant(),-8}{seed,-6}{ran.Named,8}"
                + $"{ran.Stacked,9}{ran.Tally.Eligible,10}{ran.Tally.PerEligible,12:F3}"
                + $"{ran.Sound,8}");
        }

        output.WriteLine(
            $"singly named {named[Rooting.Singly].Average():F1}, wholly "
            + $"{named[Rooting.Wholly].Average():F1}");

        // The kill line. A default is being decided on this, so what is asserted is the thing
        // that would stop it.
        Assert.True(
            named[Rooting.Wholly].Average() >= named[Rooting.Singly].Average(),
            $"the wide root named {named[Rooting.Wholly].Average():F1} against "
            + $"{named[Rooting.Singly].Average():F1} for the narrow one, so stating the "
            + "conjunction starves the one operator that broadens and the root may not ship");

        // And the null is asserted as a null. A comparison where both arms are near nought is
        // a check that cannot fire, and this repo has a line about one of those reading exactly
        // like a check that passes -- so the level is barred rather than described, and this
        // goes red the day the multiplexer starts naming enough for the line above to mean
        // something. That is the day to take the reading somewhere it has teeth.
        Assert.True(
            named[Rooting.Singly].Average() < 1.0 && stacked[Rooting.Singly].Average() == 0.0,
            $"rung five now names {named[Rooting.Singly].Average():F1} and stacks "
            + $"{stacked[Rooting.Singly].Average():F1} on this world, so the comparison above "
            + "has stopped being a null and is worth reading as a comparison");
    }

    /// <summary>
    /// Declining to answer, on the second world its entry asks for.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The conversation is where it was measured</b>, and it is not a fair second world.
    /// There the reading was the paper's own mark, where a declined question and a wrong one
    /// are both unconfirmed, so silence could only cost. Here the trailing accuracy is taken
    /// over rounds the vote SPOKE on, so precision and coverage come apart and can be read
    /// against each other directly.
    /// </para>
    /// <para>
    /// <b>And this world tells the machine nothing</b>, which is the other half of why it is
    /// worth taking. A conversation hands over statements and a multiplexer hands over
    /// nothing; if declining only paid where something was TOLD, it would be a fact about
    /// being taught rather than about the vote.
    /// </para>
    /// <para>
    /// <b>What would drop it</b>: the trailing accuracy falling, which would mean the rounds
    /// it gives up are rounds it was getting right. And it dies as an arm rather than as a
    /// default if silence does not move at all, because then nothing ran.
    /// </para>
    /// <para>
    /// <b>And the answer is a NULL, said as one.</b> It declines a few rounds in thirty
    /// thousand and the accuracy is unmoved, so this world confirms the arm costs nothing and
    /// cannot say more than that. The level is barred rather than described, so the day the
    /// multiplexer starts declining enough for the comparison to mean something, this goes red
    /// and says so.
    /// </para>
    /// <para>
    /// <b>Which is a reading about WHEN the mechanism matters rather than where.</b> Declining
    /// fires while the population is young, because a weight of nought is an advocate that has
    /// never been settled — and on a thirty-thousand-round run the young phase is a rounding
    /// error, while a lesson told once is young for the whole of it. The conversation is not a
    /// friendlier world for this; it is a shorter one.
    /// </para>
    /// </remarks>
    [Fact]
    public void What_declining_to_answer_buys_on_a_world_that_tells_it_nothing()
    {
        var recent = new Dictionary<Deciding, List<double>>
        {
            [Deciding.Anyway] = [], [Deciding.Grounded] = [],
        };

        var silent = new Dictionary<Deciding, List<double>>
        {
            [Deciding.Anyway] = [], [Deciding.Grounded] = [],
        };

        output.WriteLine($"{"deciding",-10}{"seed",-6}{"recent",9}{"silent",11}{"sound",8}");

        foreach (var deciding in new[] { Deciding.Anyway, Deciding.Grounded })
        foreach (var seed in new[] { 1, 2, 3 })
        {
            var ran = new MultiplexerRun(
                new MultiplexerSettings { Address = 2 },
                new Brain(new CommittingSettings { Deciding = deciding }, seed),
                seed).Run(Rounds);

            var quiet = ran.Tally.Rounds == 0
                ? 0.0
                : ran.Tally.Silent / (double)ran.Tally.Rounds;

            recent[deciding].Add(ran.Recent);
            silent[deciding].Add(quiet);

            output.WriteLine(
                $"{deciding.ToString().ToLowerInvariant(),-10}{seed,-6}{ran.Recent,9:F3}"
                + $"{quiet,11:F5}{ran.Sound,8}");
        }

        output.WriteLine(
            $"anyway {recent[Deciding.Anyway].Average():F3} recent, "
            + $"{silent[Deciding.Anyway].Average():F5} silent");

        output.WriteLine(
            $"grounded {recent[Deciding.Grounded].Average():F3} recent, "
            + $"{silent[Deciding.Grounded].Average():F5} silent");

        // The arm ran, asserted before anything is read off it. A default decided on two arms
        // that behaved identically would be a dial nobody turned.
        Assert.True(
            silent[Deciding.Grounded].Average() > silent[Deciding.Anyway].Average(),
            "declining went quiet on no more rounds than answering anyway did, so nothing "
            + "ran and the accuracies beside it say nothing");

        // The kill line. What it still says has to be at least as often right, or the rounds
        // it gives up are rounds it was getting right.
        Assert.True(
            recent[Deciding.Grounded].Average() >= recent[Deciding.Anyway].Average(),
            $"declining read {recent[Deciding.Grounded].Average():F3} over the rounds it "
            + $"spoke on against {recent[Deciding.Anyway].Average():F3} for answering "
            + "anyway, so it is giving up rounds it was getting right");

        // And the null is asserted as a null, which is the same move as the rung-five grid
        // below and for the same reason. A comparison where one arm is five rounds in thirty
        // thousand cannot fire, and a check that cannot fire reads exactly like a check that
        // passes. What this world establishes is that the arm costs nothing here.
        Assert.True(silent[Deciding.Grounded].Average() < 0.01,
            $"declining now goes quiet on {silent[Deciding.Grounded].Average():F5} of this "
            + "world's rounds, so the comparison above has stopped being a null and is worth "
            + "reading as a comparison");
    }

    [Fact]
    public void The_arm_that_cannot_choose_overfits_instead()
    {
        // What an ungated repair actually does, rather than what it fails to do. It
        // specialises without limit: more children, more residents, and nothing true
        // to show for either. This is ILP's cause of death reproduced on purpose.
        var gated = Run(address: 3, choosing: Choosing.Separating, seed: 1);
        var blind = Run(address: 3, choosing: Choosing.Present, seed: 1);

        output.WriteLine(
            $"gated resident={gated.Resident} repaired={gated.Repaired} sound={gated.Sound}");
        output.WriteLine(
            $"blind resident={blind.Resident} repaired={blind.Repaired} sound={blind.Sound}");

        // The multiple was five and is three, and the fall is the gated arm improving
        // rather than the blind one calming down. Under `Forking.Repeated` a parent
        // re-proposed the same child until its table drifted, so the gated arm minted a
        // few hundred children and the blind one -- drawing a DIFFERENT code each time
        // by construction -- minted ten times as many for free. The ratio was measuring
        // how little the gated arm searched. Now both search, the gated arm mints 3,608
        // and the blind one 13,477, and the gap is what over-specialising actually costs.
        Assert.True(blind.Repaired > gated.Repaired * 3,
            $"the blind arm minted {blind.Repaired} against {gated.Repaired}");

        // And the residents say the same thing without a multiple in it, which is why
        // this is added rather than the line above being loosened on its own. More rules
        // held, none of them true.
        Assert.True(blind.Resident > gated.Resident,
            $"the blind arm held {blind.Resident} against {gated.Resident}");

        // And the sharp one, which got sharper rather than weaker. Nothing sound at all
        // against 378, where the bar below was written when the gated arm held about
        // thirty. Repair's choice of condition passes its own kill condition by more
        // under a search than it did under a re-derivation.
        Assert.Equal(0, blind.SoundByRepair);
        Assert.True(gated.Sound > 20, $"only {gated.Sound} sound commitments");
    }

    [Fact]
    public void Eleven_bits_is_reported_and_carries_no_bar()
    {
        // THE SCALING NUMBER. What predicts whether any of this reaches perception is
        // how the cost grows with the number of relevant bits, and a bar here would
        // only encourage tuning against it before that curve is known.
        var six = Run(address: 2, choosing: Choosing.Separating, seed: 1);
        var eleven = Run(address: 3, choosing: Choosing.Separating, seed: 1);

        output.WriteLine($"six  recent={six.Recent:F3} sound={six.Sound} resident={six.Resident}");
        output.WriteLine(
            $"eleven recent={eleven.Recent:F3} sound={eleven.Sound} resident={eleven.Resident}");

        Assert.True(eleven.Sound > 0, "eleven bits learnt nothing true at all");
    }

    [Fact]
    public void A_fixed_seed_reproduces_a_run_exactly()
    {
        // Fork 12, which this project has already reopened twice. Repair decides from
        // a dictionary of tallies and the population is walked every round, so an
        // unstable iteration order would move the learned structure without anything
        // failing.
        // Side by side rather than one after the other, which is the harder question.
        // Consecutive runs are free to agree through anything ambient they happen to
        // share; concurrent ones are not, and a learner with a static in it fails here
        // and passes there.
        var arms = Fixture.Abreast(
            () => Run(address: 2, choosing: Choosing.Separating, seed: 8),
            () => Run(address: 2, choosing: Choosing.Separating, seed: 8),
            () => Run(address: 2, choosing: Choosing.Present, seed: 8),
            () => Run(address: 2, choosing: Choosing.Present, seed: 8),
            () => Run(address: 2, choosing: Choosing.Separating, seed: 9));

        Assert.Equal(arms[0], arms[1]);
        Assert.Equal(arms[2], arms[3]);

        Assert.NotEqual(arms[0], arms[4]);
    }
}
