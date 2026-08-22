using OpenPlexus.Codes;

namespace OpenPlexus.Machines;

/// <summary>What a machine decided to say about a moment.</summary>
/// <remarks>
/// <b>A word and an intent rather than an action index</b>, because how a world numbers its
/// doings is that world's business. A chooser handing back <c>2n + 1</c> would have one world's
/// encoding in front of every other one, which is the fault <c>Machines.Bench</c> takes a delegate to
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
/// <b>A rate and no signal</b>, and that is a measurement rather than a shortcut. Two arms read
/// the vote to decide what was worth asking about and both lost to a coin, by ten times and by
/// fifty per ask over eight seeds, so what is left is how often to ask. The refutation and what
/// would bring a signal back are in the plan's table, and <c>ConversingTests</c> holds the
/// grid.
/// </para>
/// <para>
/// <b>Because curiosity is inverted on a conversation</b>. A machine is unsure exactly where
/// nobody can answer, which is the statements, and confident on the questions, which are the only
/// moments a reply can settle. Aiming asks at what the machine does not know aims them away from
/// what the world can tell it.
/// </para>
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
    private readonly double _rate;
    private readonly Random _coins;
    private readonly Func<Code, int?> _naming;

    // What it has already said about the moment on the table. A world that takes several
    // doings a moment asks with the same codes in front of it every time, so without this the
    // vote's word comes back until the budget is gone and the machine asks one question five
    // times rather than five questions once.
    private readonly HashSet<int> _already = [];

    /// <param name="brain">Whose population is read.</param>
    /// <param name="rate">
    /// How often to ask rather than let a moment go — <b>one to ask about everything</b>.
    /// </param>
    /// <param name="seed">The draw, so a run reproduces.</param>
    /// <param name="naming">
    /// Which outcome a code in the moment stands for, or nothing where it stands for none.
    /// </param>
    /// <remarks>
    /// <b>The naming is what lets a machine with no rules ask anything</b>, and it is a delegate
    /// rather than a world for the reason <c>Machines.Bench</c>'s oracle is: a chooser naming one world
    /// would put that world's vocabulary in front of every other one.
    /// </remarks>
    public Curiosity(Brain brain, double rate, int seed, Func<Code, int?> naming)
    {
        ArgumentNullException.ThrowIfNull(brain);
        ArgumentNullException.ThrowIfNull(naming);
        ArgumentOutOfRangeException.ThrowIfNegative(rate);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(rate, 1.0);

        _brain = brain;
        _rate = rate;
        _coins = new Random(seed);
        _naming = naming;
    }

    /// <summary>How many times it made a claim.</summary>
    public long Claims { get; private set; }

    /// <summary>How many times it asked.</summary>
    public long Questions { get; private set; }

    /// <summary>How many moments it had nothing to say about.</summary>
    /// <remarks>
    /// <para>
    /// <b>Nothing to say rather than nothing to ask</b>. A moment carrying no word this world
    /// numbers has no question to put, which is the shape of the problem rather than a decision
    /// this makes.
    /// </para>
    /// <para>
    /// <b>Moments and never calls</b>, which matters once a moment takes more than one. A
    /// chooser is asked until it declines, so every moment ends in a decline unless the budget
    /// ran out first — counting those would make this a count of moments that finished, and
    /// its halves would be a rate over the wrong denominator.
    /// </para>
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
    /// <b>A claim needs a rule and a question does not</b>, which is what breaks the bootstrap
    /// lock. Where the population expects something, saying it is a claim; where it expects
    /// nothing there is nothing to claim, and the only move left is to ask about a word in front
    /// of it. Without that a chooser could only ask about its own expectations, so it would never
    /// ask, never settle, never mint and never come to have one.
    /// </para>
    /// <para>
    /// <b>The rate governs both</b>, and siting it anywhere else was a real confound. A control
    /// forced to ask wherever it has no opinion is not a control, and a quarter of the coin's
    /// asks were being spent by the harness rather than by the arm.
    /// </para>
    /// <para>
    /// <b>And a blind question is drawn from the moment</b>, rather than from the alphabet. A
    /// word the human has just used is a better guess than a word from anywhere in the
    /// vocabulary — the answer to <i>where is mary</i> is normally a word of the story about
    /// mary.
    /// </para>
    /// <para>
    /// <b>Uniformly over it, which is the arm and not the answer</b>. A run at a terminal asks
    /// about <i>is</i> and <i>the</i> as readily as about a room, because those are most of what
    /// a sentence is made of — the same wall <c>Predicting.Salient</c> was built for. Drawing by
    /// rarity is the arm owed here, and it wants a count over what has been typed.
    /// </para>
    /// <para>
    /// <b>And a word already said this moment is not said again</b>, which is what makes a
    /// second doing worth having. The vote does not move between two calls about one moment, so
    /// a machine that repeated itself would spend a budget of five on one question and read as
    /// having asked five times. Skipping the vote's word drops it to the blind draw, which is
    /// the same fall a moment nothing predicted takes.
    /// </para>
    /// <para>
    /// <b>The word rather than the doing</b>, because claiming what was just refused is the
    /// same repeat by a different verb. What the machine has spent is its chances to name
    /// something, and how it named it is not what ran out.
    /// </para>
    /// </remarks>
    public Wondered Choose(IReadOnlyCollection<Code> felt)
    {
        ArgumentNullException.ThrowIfNull(felt);

        var asking = _coins.NextDouble() < _rate;

        var vote = _brain.Voting(felt);

        if (vote.Expects is { } said && Brain.Meant(said) is { } word && _already.Add(word))
        {
            if (asking) Questions++; else Claims++;

            return new Wondered { Word = word, Asking = asking };
        }

        if (!asking) return Nothing();

        var candidates = new List<int>();

        foreach (var code in felt)
            if (_naming(code) is { } outcome && !_already.Contains(outcome))
                candidates.Add(outcome);

        if (candidates.Count == 0) return Nothing();

        // Sorted, because the draw has to reach the same word on two machines holding the same
        // moment. A set walks in whatever order it was built in, and a tie-break by dictionary
        // walk is stable in one process and arbitrary across a merge.
        candidates.Sort();

        Blind++;
        Questions++;

        var drawn = candidates[_coins.Next(candidates.Count)];

        _already.Add(drawn);

        return new Wondered { Word = drawn, Asking = true };
    }

    /// <summary>The moment is over, so what was said about it is forgotten.</summary>
    /// <remarks>
    /// <b>Told rather than worked out</b>, because two moments running can carry the same
    /// codes and a chooser comparing them would call that one moment. Whoever runs the loop is
    /// the only thing that knows where a moment stops.
    /// </remarks>
    public void Cleared() => _already.Clear();

    private Wondered Nothing()
    {
        // Only where the moment got nothing at all. A chooser asked until it declines ends
        // every moment here, so counting each decline would count moments that finished.
        if (_already.Count == 0) Silences++;

        return new Wondered { Word = null, Asking = false };
    }
}
