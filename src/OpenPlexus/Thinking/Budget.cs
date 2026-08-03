namespace OpenPlexus.Thinking;

/// <summary>
/// How a machine hunts for its own stamina.
/// </summary>
/// <remarks>
/// <b>Both numbers here are world-INDEPENDENT, and that is the whole argument
/// for the trade.</b> Stamina is not: snake wants about 2 and the senses world
/// wants about 8, and the only way anyone has ever found either is by sweeping.
/// Replacing one world-dependent constant with two world-independent ones that
/// work across a wide range is progress; replacing it with a differently-named
/// world-dependent one would not be.
/// </remarks>
public sealed record Budgeting
{
    /// <summary>
    /// How many questions to ask at each candidate before judging it.
    /// </summary>
    /// <remarks>
    /// Too small and the decision is noise; too large and it adapts slowly.
    /// <b>Nothing here is scored on how fast it converges</b>, so this errs long.
    /// </remarks>
    public required int Window { get; init; }

    /// <summary>
    /// How much less silence a bigger budget has to buy before it is worth
    /// paying for.
    /// </summary>
    /// <remarks>
    /// <b>THE BIAS TOWARD THE SMALLER BUDGET IS THE POINT.</b> Accuracy on the
    /// senses world is flat from stamina 8 to 24 while messages rise
    /// twenty-three fold, so overshooting is enormously expensive and completely
    /// invisible in the score. A rule that treated the two directions
    /// symmetrically would drift upward for free.
    /// </remarks>
    public required double Worth { get; init; }

    /// <summary>
    /// The largest stamina this may climb to. <b>A backstop, like
    /// <see cref="Graph.WalkSettings.Horizon"/>.</b>
    /// </summary>
    /// <remarks>
    /// Doubling is unbounded otherwise, and the cost of an over-large budget is
    /// the one failure that takes the process with it — 370,655 messages at
    /// stamina 24 against 16,005 at 8, for no accuracy at all.
    /// </remarks>
    public required double Most { get; init; }
}

/// <summary>
/// A machine hunting for the smallest budget that still reaches what it asks
/// about. <b>Fork 24.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>JOHN'S ASK: a swept dial is a question the system should answer itself.</b>
/// Stamina is the most load-bearing hand-set constant in the design, and the
/// stamina sweep says two things that together make it answerable.
/// </para>
/// <para>
/// <b>Accuracy plateaus long before cost does.</b> 0.7874 at stamina 8 and
/// 0.7874 at 24 — inside each other's error — for 16,005 messages against
/// 370,655. So the target is not <i>enough</i> budget, it is the <b>smallest</b>
/// budget that reaches the plateau, and a measurement watching only accuracy
/// cannot tell the two apart.
/// </para>
/// <para>
/// <b>Silence has a knee and starvation does not.</b> The share of questions
/// that reach nothing falls 0.770 → 0.276 → 0.144 → 0.126 → 0.121 and flattens
/// at stamina 8, exactly where accuracy flattens. <c>Thwarted</c> falls smoothly
/// from 0.9368 to 0.3035 and never plateaus at all, so it cannot say when to
/// stop — see fork 23.
/// </para>
/// <para>
/// <b>So there is no target percentage, which matters.</b> The rule is a
/// gradient: try smaller, try larger, keep the smallest that is not materially
/// worse. "What share of questions should go unanswered" would have been one
/// more constant nobody set.
/// </para>
/// <para>
/// <b>C1-legal by construction.</b> Stamina is set by the ORIGIN when it builds
/// a message and merely spent by nodes, so a machine varying its own budget
/// touches no node state and reads nobody else's data. This class holds no
/// graph, no bus and no codes — it counts its own outcomes and nothing more.
/// </para>
/// </remarks>
public sealed class Budget
{
    private readonly Budgeting _settings;
    private readonly Lock _gate = new();

    /// <summary>The current best estimate. Everything else is a probe around it.</summary>
    private double _stamina;

    /// <summary>Which candidate is being sampled: 0 smaller, 1 current, 2 larger.</summary>
    private int _trying;

    /// <summary>Questions asked, and questions that reached nothing, per candidate.</summary>
    private readonly int[] _asked = new int[3];
    private readonly int[] _silent = new int[3];

    /// <summary>How many times the estimate has moved. Reported, never read.</summary>
    private int _moves;

    /// <param name="start">Where to begin. <b>The answer should not depend on it</b>,
    /// which is what the convergence test asserts.</param>
    public Budget(double start, Budgeting settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(start);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(settings.Window);
        ArgumentOutOfRangeException.ThrowIfNegative(settings.Worth);
        ArgumentOutOfRangeException.ThrowIfLessThan(settings.Most, 1.0);

        _settings = settings;
        _stamina = Math.Min(start, settings.Most);
    }

    /// <summary>The current estimate — what this machine would spend by default.</summary>
    public double Stamina
    {
        get { lock (_gate) return _stamina; }
    }

    /// <summary>How many times the estimate has moved.</summary>
    public int Moves
    {
        get { lock (_gate) return _moves; }
    }

    /// <summary>
    /// The budget to spend on the next question.
    /// </summary>
    /// <remarks>
    /// <b>Never below 1.0.</b> A hop costs at least 1 because a weight cannot
    /// exceed 1.0, so a smaller budget buys nothing at all and would make the
    /// downward probe measure the same nothing forever.
    /// </remarks>
    public double Next()
    {
        lock (_gate)
        {
            return _trying switch
            {
                0 => Math.Max(_stamina / 2.0, 1.0),
                2 => Math.Min(_stamina * 2.0, _settings.Most),
                _ => _stamina,
            };
        }
    }

    /// <summary>
    /// Reports whether the question just asked reached anything at all.
    /// </summary>
    /// <remarks>
    /// <b>Reached WHAT IT WAS NARROWING TO</b>, not merely reached somewhere.
    /// The caller knows what it was looking for and this class deliberately does
    /// not — which is what keeps it free of modality, codes and the graph.
    /// </remarks>
    public void Reached(bool anything)
    {
        lock (_gate)
        {
            _asked[_trying]++;
            if (!anything) _silent[_trying]++;

            if (_asked[_trying] < _settings.Window) return;

            _trying++;
            if (_trying < 3) return;

            Decide();
            _trying = 0;
            Array.Clear(_asked);
            Array.Clear(_silent);
        }
    }

    /// <summary>
    /// All three candidates have been sampled. Take the smallest that is not
    /// materially worse.
    /// </summary>
    /// <remarks>
    /// <b>Order matters and it is not symmetric.</b> Halving is taken whenever it
    /// is not materially worse; doubling only when it is materially better. That
    /// asymmetry is what stops the budget drifting upward for free through a
    /// region where accuracy is flat and cost is not.
    /// </remarks>
    private void Decide()
    {
        var smaller = Silence(0);
        var current = Silence(1);
        var larger = Silence(2);

        // NOTHING REACHED ANYWHERE, AND A HILL-CLIMB IS BLIND HERE. Measured: a
        // budget of 1 against a world needing 8 sees every candidate score the
        // same nothing, so halving reads as "not materially worse" and the
        // controller settles at the bottom having never found the knee.
        //
        // A total failure is not ambiguous, though -- reaching nothing at all
        // means the budget is insufficient or there is nothing to reach, and in
        // both cases more budget is the only lever this class has. So climb, and
        // hand back to the ordinary rule the moment anything reaches.
        if (smaller >= 1.0 && current >= 1.0 && larger >= 1.0)
        {
            if (_stamina >= _settings.Most) return;

            _stamina = Math.Min(_stamina * 2.0, _settings.Most);
            _moves++;
            return;
        }

        // CLIMBING IS TESTED FIRST, AND THE ORDER IS LOAD-BEARING. Measured with
        // it the other way round: at a budget just below the knee, the smaller
        // and current probes are equally hopeless, so halving reads as free and
        // the controller retreats from the very budget the larger probe had just
        // shown to work. It oscillated below the knee forever.
        //
        // A material gain from doubling is strong evidence of being under the
        // knee; equal badness on either side is not evidence of anything.
        if (current - larger > _settings.Worth && _stamina < _settings.Most)
        {
            _stamina = Math.Min(_stamina * 2.0, _settings.Most);
            _moves++;
        }
        else if (smaller - current <= _settings.Worth && _stamina / 2.0 >= 1.0)
        {
            _stamina = Math.Max(_stamina / 2.0, 1.0);
            _moves++;
        }
    }

    private double Silence(int candidate) =>
        _asked[candidate] == 0 ? 0.0 : _silent[candidate] / (double)_asked[candidate];
}
