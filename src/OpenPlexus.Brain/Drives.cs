using System.Collections.Immutable;
using OpenPlexus.Codes;
using OpenPlexus.Commitments;

namespace OpenPlexus.Machines;

/// <summary>What a chooser reading a population WANTS.</summary>
/// <remarks>
/// <para>
/// <b>Two arms that both do something</b>, and the second is fork 146's. A body's want is a
/// fact about that body and is handed in; a desire to UNDERSTAND is a fact about the learner
/// and cannot be handed in at all, its term being a brain internal no world may name.
/// </para>
/// <para>
/// <b>An arm on the chooser rather than a brain dial</b>, on <c>Regulating</c>'s own
/// reasoning: which drive holds the body is a fact about an experiment, and a bar demanding a
/// second world of it would demand one world's setting be measured on another.
/// </para>
/// <para>
/// <b>And <see cref="Told"/> is the default only because the body is the only caller.</b>
/// Nothing may ship switched off while nobody decides, so the decision is registered rather
/// than left here: <c>OutstandingTests.The_learning_trend_is_read_by_something_that_chooses</c>
/// fails the build until a spine world turns <see cref="Learning"/> on. That is a stronger
/// bar than a dial default, and it is why this one is allowed to sit at the old arm.
/// </para>
/// </remarks>
internal enum Wanting
{
    /// <summary>The world says how much this body wants an outcome.</summary>
    Told,

    /// <summary>How fast the rules that would be tested are still learning.</summary>
    Learning,
}

/// <summary>
/// A chooser that reads a population — <b>a third factor from the body's own variables</b>,
/// rather than a reward handed in.
/// </summary>
/// <remarks>
/// <para>
/// <b>The last idea owed off <c>csharp</c></b>, and that branch's own build of it lost. What
/// is inherited is where the signal comes from rather than the mechanism: a body with
/// variables of its own to be in trouble about. <c>Worlds.Homeostat</c> is that body and
/// <c>Worlds.IActed</c> is the verb; this is the half between them.
/// </para>
/// <para>
/// <b>It is the learner arm of three over one seam</b>, which
/// <c>Worlds.IActed</c> named before anything filled it — a random chooser, an
/// oracle and a learner. The other two are the controls and they stay: a chooser that beats
/// neither is a chooser that has not been measured.
/// </para>
/// <para>
/// <b>It asks a commitment what it would DO</b>, rather than what would follow, which is the
/// reverse of every other reader here. A commitment whose scope holds an action code says
/// <i>doing this, in a state like this, that follows</i> — so the action is recovered from
/// the scope by the <c>doing</c> mapping and the expectation is read as it always was. That is
/// <i>what would the world look like if I did X</i> falling out of the machinery that
/// exists, which is what the plan's bet says it should do.
/// </para>
/// <para>
/// <b>And it is one pass over the population</b>, rather than one per action. The other
/// arrangement is to build a hypothetical moment per candidate and match each — which costs
/// <c>Doings</c> matcher passes a round and reads the mapping forwards. Splitting each scope
/// once costs one, and the two arrangements agree on which commitments advocate what because
/// a scope holding an action code cannot fire on a moment without it.
/// </para>
/// <para>
/// <b>The preference is handed in at the join and never built here</b>, because what a body
/// wants is a fact about that body. The <c>wanting</c> mapping reads an expected outcome and says
/// how much this body wants it; a chooser that decided that itself would be the library
/// deciding what a world is for, which is the same fault as a world naming a brain type one
/// layer out.
/// </para>
/// <para>
/// <b>And that argument does not reach a desire to UNDERSTAND</b>, which is fork 146 and the
/// drive John asked for. Wanting to know is a fact about the learner rather than about any
/// body, and the term it reads — <c>Commitment.Progress</c> — is a brain internal that
/// <c>SeparationTests</c> forbids a world from naming. So a want that a world hands in cannot
/// be that drive, and this is where it has to be built. The line above is right about a
/// hunger and says nothing about curiosity.
/// </para>
/// <para>
/// <b>And what it may read is what the learner feels</b>, which is the line that separates it
/// from the oracle. The oracle on <c>Worlds.Homeostat</c> reads <c>Lowest</c> off the world's own
/// state; this reads the codes the front end emitted and nothing else, so its preference is
/// computed from a body's variables as the body reports them rather than as the world holds
/// them.
/// </para>
/// <para>
/// <b>It ranks by want and breaks ties by the vote's weight</b>, rather than multiplying the
/// two. One weight doing two jobs is this design's recurring fault and its cheap detector is
/// already a guard; a want scaled by a confidence is a number that cannot say which of them
/// moved it.
/// </para>
/// <para>
/// <b>So it exploits and never explores, which is stated rather than fixed.</b> An action the
/// population says nothing about is not a candidate, so a chooser that has never done a thing
/// has no reason to start. That is a real property of this build and the arm that would
/// answer it is a different one; naming it here stops the next reading being attributed to
/// the preference.
/// </para>
/// <para>
/// <b>And <see cref="Untold"/> is a control arm nobody meant to run</b> unless it is counted.
/// Where the population advocates for no action at all this has to do something, and silence
/// drifting a chooser toward the random bar for free is how an arm scores like its control.
/// It is handed in and <see cref="Untold"/> counts the rounds it decided.
/// </para>
/// </remarks>
internal sealed class Drives
{
    private readonly Population _held;
    private readonly Func<Code, int?> _doing;
    private readonly Func<Code, IReadOnlyCollection<Code>, double> _wanting;
    private readonly Func<int?> _untold;
    private readonly Wanting _arm;

    // What it has already said about the moment on the table. A world that takes several
    // doings a moment asks with the same codes in front of it every time, and the population
    // does not move between two calls -- so without this the best advocate comes back until
    // the budget is gone and a machine that could have said a verb and a thing says the verb
    // twice. `Curiosity` carries the same set for the same reason.
    private readonly HashSet<int> _already = [];

    /// <param name="held">The population to read.</param>
    /// <param name="doing">Which action a code names, or nothing where it names none.</param>
    /// <param name="wanting">How much this body wants an expected outcome.</param>
    /// <param name="untold">What to do on a round the population advocates nothing.</param>
    /// <param name="arm">What it wants.</param>
    public Drives(
        Population held,
        Func<Code, int?> doing,
        Func<Code, IReadOnlyCollection<Code>, double> wanting,
        Func<int?> untold,
        Wanting arm = Wanting.Told)
    {
        ArgumentNullException.ThrowIfNull(held);
        ArgumentNullException.ThrowIfNull(doing);
        ArgumentNullException.ThrowIfNull(wanting);
        ArgumentNullException.ThrowIfNull(untold);

        _held = held;
        _doing = doing;
        _wanting = wanting;
        _untold = untold;
        _arm = arm;
    }

    /// <summary>Rounds the population advocated nothing and the fallback decided.</summary>
    /// <remarks>
    /// <b>Reported beside the score</b>, because a fallback is a control arm nobody meant to
    /// run. An arm silent on most rounds is its own fallback wearing a learner's name, and
    /// nothing in a score can tell the two apart.
    /// </remarks>
    public long Untold { get; private set; }

    /// <summary>Rounds a commitment named the action.</summary>
    public long Told { get; private set; }

    /// <summary>What to do, given what is felt.</summary>
    /// <param name="felt">The codes the front end emitted for the state acted in.</param>
    /// <remarks>
    /// <b>The action's own code is not in <paramref name="felt"/></b>, and that is what makes
    /// this askable at all. A state reported before anything is done about it carries no
    /// action, so every scope holding one is a claim about a state this one could become.
    /// </remarks>
    public int? Choose(IReadOnlyCollection<Code> felt)
    {
        ArgumentNullException.ThrowIfNull(felt);

        var present = felt as IReadOnlySet<Code> ?? felt.ToHashSet();

        var advocates = new Dictionary<int, List<Commitment>>();

        // One snapshot, taken under the gate by `All` itself, and the groups below keep its
        // order. `Weigh` breaks a tie between two advocates by which came first and says so,
        // so a group in any other order would decide differently on a machine that held the
        // same rules -- and `All` is already sorted by identity, which is the order that
        // reader documents.
        foreach (var commitment in _held.All)
        {
            if (Named(commitment.Scope) is not int doing) continue;

            // The rest of the scope must already hold, or this commitment is a claim about a
            // state that is not the one being acted in. A scope of the action alone passes,
            // and it should: `do this and that follows` is the shallowest thing a commitment
            // about acting can say.
            if (!commitment.Scope.All(code =>
                _doing(code) is not null || present.Contains(code)))
            {
                continue;
            }

            if (!advocates.TryGetValue(doing, out var behind))
                advocates[doing] = behind = [];

            behind.Add(commitment);
        }

        var best = default(int?);
        var wanted = double.NegativeInfinity;
        var weighed = double.NegativeInfinity;

        // In action order, so a tie between two actions nothing separates resolves the same
        // way on every machine. A dictionary walk is stable in one process and arbitrary
        // across a merge, which this repo has already been bitten by.
        foreach (var doing in advocates.Keys.Order())
        {
            if (_already.Contains(doing)) continue;

            var vote = Population.Decide(
                [_held.Weigh([.. advocates[doing]])]);

            if (vote.Expects is not Code expects) continue;

            var want = _arm is Wanting.Learning
                ? Learning(advocates[doing], expects)
                : _wanting(expects, felt);

            // Want first and weight only to break a tie. Multiplying them would make a
            // confident prediction of a poor outcome trade against an unsure one of a good
            // outcome on a scale nobody chose.
            if (want < wanted || (want == wanted && vote.Weight <= weighed)) continue;

            (best, wanted, weighed) = (doing, want, vote.Weight);
        }

        if (best is null) Untold++;
        else Told++;

        var said = best ?? _untold();

        // The fallback's draw is remembered too, because what has been spent is a chance to
        // say something and not the route it was said by. A fallback repeating itself is the
        // same wasted second doing an advocate repeating itself would be.
        if (said is { } spoken) _already.Add(spoken);

        return said;
    }

    /// <summary>How fast the rules that would be tested are still learning.</summary>
    /// <param name="behind">Everything advocating this action.</param>
    /// <param name="expects">What the vote among them settled on.</param>
    /// <remarks>
    /// <para>
    /// <b>Over the advocates for the WINNER alone</b>, because those are the rules whose
    /// prediction is on the line. An advocate for a losing expectation is not tested by doing
    /// this, so counting it would read the crowd's state rather than the claim's.
    /// </para>
    /// <para>
    /// <b>The mean rather than the best</b>, a maximum over a set being the noisiest reader
    /// of a trend there is: one rule mid-swing would carry an action whose rules are settled.
    /// </para>
    /// <para>
    /// <b>Signed, which is <see cref="Commitment.Progress"/>'s own choice.</b> A magnitude
    /// would rank a rule falling apart level with one coming good, and a channel that cannot
    /// be predicted swings both ways forever — which is how a want built to sate would come
    /// back as surprise.
    /// </para>
    /// <para>
    /// <b>And it exploits exactly as far as the population reaches</b>, which is the class's
    /// own stated limit and worth repeating here: an action nothing advocates is not a
    /// candidate, so this is curiosity about what is already half-known rather than about
    /// what has never been tried. Naming it stops a reading being attributed to the want.
    /// </para>
    /// </remarks>
    private static double Learning(List<Commitment> behind, Code expects)
    {
        var total = 0.0;
        var counted = 0;

        foreach (var one in behind)
        {
            if (one.Expects != expects) continue;

            total += one.Progress;
            counted++;
        }

        return counted == 0 ? 0.0 : total / counted;
    }

    /// <summary>The moment is over, so what was said about it is forgotten.</summary>
    /// <remarks>
    /// <b>Told rather than worked out</b>, because two moments running can carry the same
    /// codes and a chooser comparing them would call that one moment.
    /// </remarks>
    public void Cleared() => _already.Clear();

    /// <summary>Which action this scope names, where exactly one code names one.</summary>
    /// <param name="scope">The codes that must all be present.</param>
    /// <remarks>
    /// <b>Exactly one</b>, because two would be a claim about doing two things at once, and
    /// <c>Worlds.IActed.Do</c> takes one. Nothing forbids repair from minting
    /// such a scope — it separates on whatever code discriminates, and an action code is an
    /// ordinary one — so this is a shape that occurs rather than one that cannot.
    /// </remarks>
    private int? Named(ImmutableArray<Code> scope)
    {
        var found = default(int?);

        foreach (var code in scope)
        {
            if (_doing(code) is not int doing) continue;
            if (found is not null) return null;

            found = doing;
        }

        return found;
    }
}
