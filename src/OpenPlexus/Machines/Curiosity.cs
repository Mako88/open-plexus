using OpenPlexus.Codes;

namespace OpenPlexus.Machines;

/// <summary>When a machine asks about a moment rather than asserting about it.</summary>
/// <remarks>
/// <para>
/// <b>An arm rather than a setting</b>. Asking costs the one thing a conversation is short of,
/// which is the patience of whoever is answering, so how often to spend it is a question with a
/// wrong answer at both ends. A machine that never asks never settles anything and a machine that
/// asks about every line is one nobody talks to twice.
/// </para>
/// <para>
/// <b>The kill line is pre-registered</b>, and it is about the asks rather than the score.
/// <see cref="Unsure"/> dies unless it reaches the accuracy <see cref="Always"/> reaches while
/// asking fewer times, or beats <see cref="Coin"/> at the same number of asks. A rise in accuracy
/// bought by asking more often is not a finding about curiosity.
/// </para>
/// </remarks>
public enum Curious
{
    /// <summary>Ask where the vote is not confident, and claim where it is.</summary>
    /// <remarks>
    /// <b>Off <c>Vote.Margin</c>, which needs no body and nothing told</b>. The margin over the
    /// weight is the same confidence a round already records, so this reads a statistic the
    /// machine keeps for other reasons rather than adding one.
    /// </remarks>
    Unsure,

    /// <summary>Ask where the winning expectation has little evidence behind it.</summary>
    /// <remarks>
    /// <para>
    /// <b>Off <c>Vote.Weight</c>, because a margin measures disagreement and not ignorance</b>.
    /// An unopposed winner leads the runner-up by its whole weight, so one commitment with two
    /// observations behind it reads as maximally confident — which is measured here rather than
    /// argued: <see cref="Unsure"/> asked four hundred times about statements nobody could
    /// answer and four times about the questions, because every question moment had a single
    /// advocate and therefore a perfect margin.
    /// </para>
    /// <para>
    /// <b>Weight is how much evidence is behind the winner</b>, so a thin one is the thing a
    /// question is for. It is the same statistic the vote already keeps.
    /// </para>
    /// </remarks>
    Untested,

    /// <summary>Ask whenever there is anything to ask about.</summary>
    /// <remarks>
    /// <b>The ceiling</b>, and it is what a rate has to be read against. Every settlement this
    /// world can produce is produced here, so it says what the population would be worth if
    /// nobody minded being interrogated.
    /// </remarks>
    Always,

    /// <summary>Ask at a rate, about whatever the vote happened to land on.</summary>
    /// <remarks>
    /// <b>The control that says the choosing is doing the work</b>. Run at the rate
    /// <see cref="Unsure"/> produced, this asks the same number of times and picks the moments
    /// without reading anything — so a gain that survives here was never about curiosity.
    /// </remarks>
    Coin,
}

/// <summary>What a machine decided to say about a moment.</summary>
/// <remarks>
/// <b>A word and an intent rather than an action index</b>, because how a world numbers its
/// doings is that world's business. A chooser handing back <c>2n + 1</c> would have one world's
/// encoding in front of every other one, which is the fault <c>Bench</c> takes a delegate to
/// avoid.
/// </remarks>
public readonly record struct Wondered
{
    /// <summary>Which outcome it would name, or nothing where it had nothing to say.</summary>
    public required int? Word { get; init; }

    /// <summary>Whether that word is a question rather than a claim.</summary>
    public required bool Asking { get; init; }
}

/// <summary>A chooser that decides whether to speak, and whether what it says is a question.</summary>
/// <remarks>
/// <para>
/// <b>The population is read and never written</b>. <see cref="Brain.Voting"/> is the same three
/// read-only calls the failure census is built out of, so a session with a chooser and one
/// without differ in what is said and in nothing else.
/// </para>
/// <para>
/// <b>And it names no world</b>. What it returns is a word and an intent, and turning that into
/// an action index is the join's job.
/// </para>
/// </remarks>
public sealed class Curiosity
{
    private readonly Brain _brain;
    private readonly Curious _wondering;
    private readonly double _bar;
    private readonly Random _coins;
    private readonly Func<Code, int?> _naming;

    /// <param name="brain">Whose population is read.</param>
    /// <param name="wondering">Which arm.</param>
    /// <param name="bar">
    /// The confidence below which <see cref="Curious.Unsure"/> asks, or the rate at which
    /// <see cref="Curious.Coin"/> does.
    /// </param>
    /// <param name="seed">The draw, so a run reproduces.</param>
    /// <param name="naming">
    /// Which outcome a code in the moment stands for, or nothing where it stands for none.
    /// </param>
    /// <remarks>
    /// <b>The naming is what lets a machine with no rules ask anything</b>, and it is a delegate
    /// rather than a world for the reason <c>Bench</c>'s oracle is: a chooser naming one world
    /// would put that world's vocabulary in front of every other one.
    /// </remarks>
    public Curiosity(Brain brain, Curious wondering, double bar, int seed, Func<Code, int?> naming)
    {
        ArgumentNullException.ThrowIfNull(brain);
        ArgumentNullException.ThrowIfNull(naming);
        ArgumentOutOfRangeException.ThrowIfNegative(bar);

        _brain = brain;
        _wondering = wondering;
        _bar = bar;
        _coins = new Random(seed);
        _naming = naming;
    }

    /// <summary>How many times it made a claim.</summary>
    public long Claims { get; private set; }

    /// <summary>How many times it asked.</summary>
    public long Questions { get; private set; }

    /// <summary>How many moments it had nothing to say about.</summary>
    /// <remarks>
    /// <b>Nothing to say rather than nothing to ask</b>. A moment no commitment fires on has no
    /// word attached to it, so there is no question to put — which is the shape of the problem
    /// rather than a decision this makes.
    /// </remarks>
    public long Silences { get; private set; }

    /// <summary>How many of the questions were about a word nothing had predicted.</summary>
    /// <remarks>
    /// <b>The bootstrap, counted so it can be seen to end</b>. A run whose asks stay blind to the
    /// last round has a population that never came to expect anything, which reads identically to
    /// a run that learnt until this number is beside the others.
    /// </remarks>
    public long Blind { get; private set; }

    /// <summary>What to say about a moment.</summary>
    /// <param name="felt">The codes the moment arrives as.</param>
    /// <remarks>
    /// <para>
    /// <b>A claim needs a rule and a question does not</b>, which is the rule that breaks the
    /// bootstrap lock. Where the population expects something, saying it is a claim and the arm
    /// decides whether to risk it or check it; where the population expects nothing, there is
    /// nothing to claim and the only move left is to ask about a word that is in front of it.
    /// </para>
    /// <para>
    /// <b>And the blind question is drawn from the moment</b>, rather than from the alphabet. A word the
    /// human has just used is a far better guess than a word from anywhere in the vocabulary, and
    /// it costs nothing to prefer — the answer to <i>where is mary</i> is normally a word of the
    /// story about mary.
    /// </para>
    /// </remarks>
    public Wondered Choose(IReadOnlyCollection<Code> felt)
    {
        ArgumentNullException.ThrowIfNull(felt);

        var vote = _brain.Voting(felt);

        if (vote.Expects is not { } said || Brain.Meant(said) is not { } word) return Blindly(felt);

        // A weight of nought is reachable and silent: every accuracy starts there, so the first
        // rounds of any run vote with weights of exactly nought and a lead divided by that is a
        // NaN. An unweighted vote is not a confident one, which is the reading that makes the
        // guard a decision rather than a patch.
        var confidence = vote.Weight > 0 ? vote.Margin / vote.Weight : 0.0;

        var asking = _wondering switch
        {
            Curious.Always => true,
            Curious.Coin => _coins.NextDouble() < _bar,
            Curious.Untested => vote.Weight < _bar,
            _ => confidence < _bar,
        };

        if (asking) Questions++; else Claims++;

        return new Wondered { Word = word, Asking = asking };
    }

    /// <summary>A question about a word in the moment, where nothing predicted one.</summary>
    /// <remarks>
    /// <b>Uniform over the moment, which is the arm and not the answer</b>. A run at a terminal
    /// asks about <i>is</i> and <i>the</i> as readily as about a room, because those are most of
    /// what a sentence is made of — the same wall <c>Predicting.Salient</c> was built for, where
    /// hiding every word in turn spent the demand on the words that carry least. Drawing by
    /// rarity is the arm owed here, and it wants a count over what has been typed.
    /// </remarks>
    private Wondered Blindly(IReadOnlyCollection<Code> felt)
    {
        var candidates = new List<int>();

        foreach (var code in felt)
            if (_naming(code) is { } outcome)
                candidates.Add(outcome);

        if (candidates.Count == 0)
        {
            Silences++;

            return new Wondered { Word = null, Asking = false };
        }

        // Sorted, because the draw has to reach the same word on two machines holding the same
        // moment. A set walks in whatever order it was built in, and a tie-break by dictionary
        // walk is stable in one process and arbitrary across a merge.
        candidates.Sort();

        Blind++;
        Questions++;

        return new Wondered { Word = candidates[_coins.Next(candidates.Count)], Asking = true };
    }
}
