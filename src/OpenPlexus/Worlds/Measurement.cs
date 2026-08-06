namespace OpenPlexus.Worlds;

/// <summary>
/// What every world's result has in common: the machinery underneath it, and the
/// range checks that would only fail if something had come unwired.
/// </summary>
/// <remarks>
/// <para>
/// <b><see cref="Complaints"/> is the part that matters.</b> Every entry is a
/// quantity that would be out of range only if something had come unwired, and a
/// test asserts the list is empty on every run. Adding a number to a result
/// without a range says nothing; the range is the check.
/// </para>
/// <para>
/// <b>It is a base class because the list was three lists.</b> Snake, senses and
/// binding each wrote the same six checks against their own field names, and the
/// only way to add a seventh was to remember all three — which is the shape of
/// every drift this project has had. A world now says what is peculiar to it and
/// inherits the rest, and <c>DuplicationTests</c> is what stops a fourth world
/// starting a fourth copy.
/// </para>
/// </remarks>
public abstract record Measurement
{
    /// <inheritdoc cref="Worlds.Plumbing"/>
    public required Plumbing Plumbing { get; init; }

    /// <inheritdoc cref="Worlds.Plumbing.Nodes"/>
    public int Nodes => Plumbing.Nodes;

    /// <inheritdoc cref="Worlds.Plumbing.Edges"/>
    public int Edges => Plumbing.Edges;

    /// <inheritdoc cref="Worlds.Plumbing.Widest"/>
    public int Widest => Plumbing.Widest;

    /// <inheritdoc cref="Worlds.Plumbing.Spread"/>
    public IReadOnlyList<int> Spread => Plumbing.Spread;

    /// <inheritdoc cref="Chains"/>
    public IReadOnlyDictionary<int, int> ChainLengths => Plumbing.ChainLengths;

    /// <inheritdoc cref="Worlds.Plumbing.Messages"/>
    public long Messages => Plumbing.Messages;

    /// <inheritdoc cref="Worlds.Plumbing.Unbalanced"/>
    public int Unbalanced => Plumbing.Unbalanced;

    /// <summary>
    /// Walks read before they had finished — fork 22.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Counted rather than absorbed.</b> A walk that had not finished produces
    /// "nothing reached", which is indistinguishable from a graph that genuinely
    /// had nothing to say — so a run where this is not zero has a silent-count
    /// that cannot be trusted.
    /// </para>
    /// <para>
    /// <b>IT LIVES HERE AND NOT ON <see cref="Questioned"/>, AND IT WAS MOVED
    /// BECAUSE A WORLD WENT WITHOUT IT.</b> <c>Homeostat</c> asks nothing and
    /// still walks, so it sat outside the only place this check existed: it
    /// discarded what <c>SettleAsync</c> returned, and an unfinished walk there
    /// reads as "the graph proposed no action" and is converted into a random
    /// act by the bootstrap. That is fork 22 wearing the costume of the very
    /// result step 4 rests on. <b>Anything that walks can suffer it, so anything
    /// that walks inherits the check.</b>
    /// </para>
    /// </remarks>
    public required int Unsettled { get; init; }

    /// <inheritdoc cref="Worlds.Plumbing.Deepest"/>
    public int Deepest => Plumbing.Deepest;

    /// <summary>
    /// How far a route has to walk before this world's task is possible at all.
    /// </summary>
    /// <remarks>
    /// <b>Not a constant, because the worlds genuinely differ.</b> A colour and a
    /// shape occur in the same binding scene, so a chain of two is the whole task
    /// there; sight reaches touch only through sound, so nothing shallower than
    /// three could be the senses task.
    /// </remarks>
    protected abstract int Composes { get; }

    /// <summary>How this world says a walk never got going. See <see cref="Composes"/>.</summary>
    protected abstract string Stalled { get; }

    /// <summary>Whatever only this world can check. Appends to the same list.</summary>
    protected abstract void Peculiar(List<string> wrong);

    /// <summary>
    /// Everything out of the range it would be in if this world were wired the
    /// way it is supposed to be.
    /// </summary>
    public IReadOnlyList<string> Complaints
    {
        get
        {
            var wrong = new List<string>();

            if (Nodes == 0) wrong.Add("no node ever came into existence");
            if (Edges == 0) wrong.Add("no connection was ever formed");
            if (Messages == 0) wrong.Add("the bus carried nothing");
            if (Unbalanced > 0) wrong.Add($"{Unbalanced} thoughts did not balance");

            // A route that never leaves its origin means the walk is not walking,
            // and every result would be about one-hop association.
            if (Deepest < Composes) wrong.Add($"{Stalled} — deepest chain {Deepest}");

            if (Spread.Count(count => count > 0) < 2)
                wrong.Add("every node landed on one cluster");

            if (Unsettled > 0)
                wrong.Add($"{Unsettled} walks were read before they had finished");

            Peculiar(wrong);

            return wrong;
        }
    }

    /// <summary>
    /// The <c>|| WRONG:</c> tail a result's own line ends with, or nothing at all.
    /// </summary>
    protected string Wrong =>
        Complaints.Count == 0 ? "" : $" || WRONG: {string.Join("; ", Complaints)}";
}

/// <summary>
/// A world that shows something, asks about it, and scores the answer — <b>the
/// senses world and the binding world, which differ in the question and in
/// nothing else about their harness.</b>
/// </summary>
public abstract record Questioned : Measurement
{
    /// <summary>Moments shown.</summary>
    public required int Moments { get; init; }

    /// <summary>Questions asked.</summary>
    public required int Asked { get; init; }

    /// <summary>Of those, how many were answered right.</summary>
    public required int Right { get; init; }

    /// <summary>Of those, how many the graph had nothing at all to say about.</summary>
    public required int Silent { get; init; }

    /// <summary>What a blind guess would score.</summary>
    public required double Chance { get; init; }

    /// <summary>Routes killed by the horizon rather than by economics.</summary>
    public required long Halted { get; init; }

    /// <inheritdoc cref="Worlds.Reflections"/>
    public required Reflections Reflections { get; init; }

    /// <summary>
    /// The most distinct agreement levels any one question's candidates fell
    /// into — <b>whether <see cref="Graph.Accumulate.Agreement"/> ever had
    /// anything to rank in this whole run.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>ONE ACROSS A WHOLE RUN MEANS THE ARM WAS INERT BY CONSTRUCTION AND ITS
    /// SCORE IS <see cref="Graph.Accumulate.Sum"/>'S, EXACTLY.</b> See
    /// <see cref="Thinking.Thought.Divides"/>. It is required rather than
    /// defaulted because a world that forgot to set it would read as a walk that
    /// arrived nowhere, and a check that cannot fire reads as one that passed.
    /// </para>
    /// <para>
    /// <b>THE MAXIMUM OVER THE RUN, NOT THE MEAN.</b> The claim being checked is
    /// that NO question ever had a spread to rank by, so one question that did is
    /// enough to refute it — and averaging would bury that single case under
    /// every question with a single candidate.
    /// </para>
    /// </remarks>
    public required int Divides { get; init; }

    /// <inheritdoc cref="Worlds.Reflections.Wrote"/>
    public int Reflected => Reflections.Wrote;

    /// <inheritdoc cref="Worlds.Reflections.On"/>
    public bool Reflecting => Reflections.On;

    /// <summary>What this world calls the thing it shows. "moments", "scenes".</summary>
    protected abstract string Shown { get; }

    /// <summary>The share of questions answered correctly.</summary>
    public double Accuracy => Asked == 0 ? 0.0 : Right / (double)Asked;

    /// <summary>Whatever only this world can check, beyond the asking.</summary>
    protected abstract void Beyond(List<string> wrong);

    /// <inheritdoc/>
    protected override void Peculiar(List<string> wrong)
    {
        ArgumentNullException.ThrowIfNull(wrong);

        if (Moments == 0) wrong.Add($"the run showed no {Shown}");
        if (Asked == 0) wrong.Add("nothing was ever asked");
        if (Asked > 0 && Silent >= Asked) wrong.Add("every question went unanswered");

        // FORK 21'S OWN WIRING CHECK. A dial that is on and does nothing is a
        // sweep arm that looks distinct and is not, which is exactly how this
        // project has fooled itself before.
        if (Reflecting && Reflected == 0)
            wrong.Add("reflection was switched on and wrote nothing");
        if (!Reflecting && Reflected > 0)
            wrong.Add($"reflection was off and still wrote {Reflected}");

        Beyond(wrong);
    }
}
