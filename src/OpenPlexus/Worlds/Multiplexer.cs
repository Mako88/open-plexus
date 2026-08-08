using System.Collections.Immutable;
using OpenPlexus.Codes;

namespace OpenPlexus.Worlds;

/// <summary>How wide the multiplexer is, how noisy, and whether its target moves.</summary>
public sealed record MultiplexerSettings
{
    /// <summary>
    /// How many address bits. <b>The world is <c>Address + 2^Address</c> bits
    /// wide</b> — two gives the six-bit multiplexer and three the eleven-bit, which
    /// are the two sizes the literature reports most often.
    /// </summary>
    public int Address { get; init; } = 2;

    /// <summary>
    /// The share of rounds whose outcome is flipped, in 0..1.
    /// </summary>
    /// <remarks>
    /// <b>Zero is the world the published numbers are against, so it is the
    /// default.</b> Noise is here because the repair gate is the one mechanism that
    /// cannot be tested on a clean world: on a deterministic target every failure
    /// really is explained by some absent condition, so a gate that admits
    /// everything scores exactly as well as one that admits only what it should.
    /// </remarks>
    public double Noise { get; init; }

    /// <summary>
    /// How many rounds between redrawing which data bit each address selects.
    /// </summary>
    /// <remarks>
    /// <b>Zero never moves it, and that is the standard world.</b> Above zero this
    /// is the switching multiplexer, which exists to answer fork 27: monotone
    /// counters converge but cannot track, so the local decaying estimate is either
    /// earning its keep here or earning it nowhere.
    /// </remarks>
    public int Switch { get; init; }

    /// <summary>
    /// How many of the <c>2^Bits</c> assignments are never drawn.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>FORK 48: THE ONE WORLD WHERE DEPTH IS GENUINELY NEEDED WITHHELD NOTHING, so
    /// every instrument that wants a held-out set was blind exactly where it was most
    /// wanted.</b> A generated world can hold assignments back and the learner cannot
    /// tell, because there is no boundary to notice — the world simply never emits them,
    /// which is what C4 asks and what a train-then-test split is not.
    /// </para>
    /// <para>
    /// <b>TAKEN FROM THE END OF THE ASSIGNMENT ORDER, so the split is a position rather
    /// than a sample</b> — <see cref="Cifar"/> and <see cref="Monk"/> both do this, and
    /// for the reason those give: a held-out set chosen by the world's own generator
    /// moves with the seed, and two seeds are then scored against two different
    /// questions.
    /// </para>
    /// </remarks>
    public int Withheld { get; init; }

    /// <summary>
    /// How many extra bits are shown that are ALWAYS ONE and mean nothing.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>FORK 51: A CODE LIVE IN EVERY MOMENT SEPARATES NOTHING, AND NO WORLD HERE HAD
    /// ONE.</b> Every multiplexer code is a (position, value) pair present about half the
    /// time, so background — the thing that is simply always there — could not be studied
    /// on the world where everything else is exactly known.
    /// </para>
    /// <para>
    /// <b>WHAT THE LEARNER DOES WITH IT IS NOT THIS FILE'S BUSINESS, AND THE WORLD'S JOB
    /// IS ONLY TO POSE IT.</b> A code present in every moment is present in every hit and
    /// every miss alike, so any statistic asking what SEPARATES the two has nothing to
    /// find in it — and whether anything downstream exploits that, or pays for the code
    /// anyway in candidates and in stored counts, is exactly what this dial is for
    /// measuring rather than for asserting.
    /// </para>
    /// <para>
    /// <b>Always one rather than randomly constant, so the answer key is untouched.</b>
    /// The function ignores these bits entirely, so a scope's soundness is decided the
    /// same way whether they are enumerated or not — they add candidates and entries and
    /// no information, which is precisely the thing being measured.
    /// </para>
    /// </remarks>
    public int Clutter { get; init; }
}

/// <summary>One round: what was shown, and what should follow it.</summary>
/// <remarks>
/// <b>Two fields and not a frame</b> — how a round becomes occasions is the runner's
/// business, because a world is a PROBLEM and the frame protocol is not part of one.
/// </remarks>
public readonly record struct Round
{
    /// <summary>One code per bit, carrying its position and its value.</summary>
    public required ImmutableArray<Code> Cues { get; init; }

    /// <summary>What the multiplexer says, before <see cref="MultiplexerSettings.Noise"/>.</summary>
    public required Code Answer { get; init; }

    /// <summary>What was actually emitted, which noise may have flipped.</summary>
    public required Code Outcome { get; init; }

    /// <summary>Two rounds are the same when they SAY the same thing.</summary>
    /// <remarks>
    /// <b>Written out because the compiler's answer here is wrong and silent.</b>
    /// A synthesised record equality compares <see cref="ImmutableArray{T}"/> by the
    /// identity of the array behind it, so two rounds built from the same draw
    /// compare UNEQUAL and two that differ compare unequal for the wrong reason.
    /// Every determinism check written against it would have passed by never being
    /// able to fail.
    /// </remarks>
    /// <param name="other">The round to compare against.</param>
    public bool Equals(Round other) =>
        Answer == other.Answer
        && Outcome == other.Outcome
        && Cues.AsSpan().SequenceEqual(other.Cues.AsSpan());

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        var hash = new HashCode();

        hash.Add(Answer);
        hash.Add(Outcome);
        foreach (var code in Cues) hash.Add(code);

        return hash.ToHashCode();
    }
}

/// <summary>One commitment the world would hold if it had learnt itself perfectly.</summary>
public readonly record struct Truth
{
    /// <summary>
    /// The address bits, and the one data bit they select.
    /// </summary>
    /// <remarks>
    /// <b>Held in <see cref="Code"/> order, because a scope is a SET.</b> Two scopes
    /// naming the same codes in different orders are the same scope, so the order is
    /// canonicalised at the source rather than compared around.
    /// </remarks>
    public required ImmutableArray<Code> Scope { get; init; }

    /// <summary>What follows whenever that scope is satisfied.</summary>
    public required Code Expects { get; init; }

    /// <summary>Two rules are the same when they say the same thing about the same scope.</summary>
    /// <remarks>
    /// <b>The answer key is compared for a living</b> — how many true rules were
    /// found EXACTLY is the number step one is judged on — so reference equality
    /// here would make that score permanently zero and blame the learner.
    /// </remarks>
    /// <param name="other">The rule to compare against.</param>
    public bool Equals(Truth other) =>
        Expects == other.Expects && Scope.AsSpan().SequenceEqual(other.Scope.AsSpan());

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        var hash = new HashCode();

        hash.Add(Expects);
        foreach (var code in Scope) hash.Add(code);

        return hash.ToHashCode();
    }
}

/// <summary>
/// Several cues arrive together and only some carry the outcome.
/// </summary>
/// <remarks>
/// <para>
/// <b>THE WORLD THE PLAN ASKS FOR, AND NOT AN ANALOGY FOR IT.</b> Address bits
/// select which data bit is the answer, so every bit is present in every round,
/// most of them are irrelevant, and WHICH ones are irrelevant changes from round to
/// round. `csharp`'s plan lists that world under the ones it is missing; this is it,
/// and it is also the benchmark XCS was published against, so the external baseline
/// costs nothing.
/// </para>
/// <para>
/// <b>IT IS GENERATED, WHICH IS HALF OF WHY IT IS FIRST.</b> A corpus can contain
/// its own answer and then a score measures the leak — the trap has already cost
/// this project once. Nothing drawn from a generator can.
/// </para>
/// <para>
/// <b>AND ITS GROUND TRUTH IS A KNOWN RULE SET, WHICH IS THE OTHER HALF.</b> An
/// accuracy can be reached by memorising, so accuracy alone cannot tell a learner
/// that found the structure from one that stored the instances. <see cref="Truths"/>
/// is the answer key: how many of those were found EXACTLY is the number step one is
/// judged on, and the resident commitment count beside it is what catches the
/// learner that scored well by holding ten thousand rules.
/// </para>
/// <para>
/// <b>WHAT IT DOES NOT TEST IS THE BET.</b> Its cues are already symbols, so nothing
/// here reaches a quantiser and nothing here says whether a substrate that
/// manufactures symbols repairs the interface that killed this family of systems.
/// This measures the LEARNER. Step one passing is not evidence for the thesis and
/// must never be written up as though it were.
/// </para>
/// </remarks>
public sealed class Multiplexer : IWorld<IReadOnlyList<int>>, IWithholds<IReadOnlyList<int>>
{
    /// <inheritdoc/>
    public int Outcomes => 2;

    /// <summary>
    /// The same round, said in the world's own terms rather than in codes.
    /// </summary>
    /// <remarks>
    /// <b>EXPLICIT, BECAUSE A WORLD KEEPS ITS OWN VOICE.</b> <see cref="Next"/> says
    /// what this world has always said and is what its own tests read; this says the
    /// same thing in the shape every world shares, so that one brain can be handed
    /// any of them without a world knowing a brain exists.
    /// </remarks>
    Turn<IReadOnlyList<int>> IWorld<IReadOnlyList<int>>.Next()
    {
        var shown = Next();

        return new Turn<IReadOnlyList<int>>
        {
            // WRITTEN AS LAMBDAS BECAUSE THE PACKING NOW DECLARES ITS WIDTH. A method
            // group cannot be converted where the method has an optional argument, and
            // that is the compiler asking the right question: this world is bits, so it
            // takes the default stride and says so.
            Seen = [.. shown.Cues
                .OrderBy(code => Codes.Bits.Position(code))
                .Select(code => Codes.Bits.Value(code))],
            Outcome = (int)shown.Outcome.Value,
        };
    }

    /// <summary>The modality for one bit, carrying its position and its value.</summary>
    /// <remarks>
    /// <b>One code per (position, value) and never one per position.</b> A code
    /// standing for a position with its value attached is what lets a scope say
    /// *bit three is zero*; a code standing for the position alone could only say
    /// *bit three exists*, which is true in every round and separates nothing.
    /// </remarks>
    public const byte Bit = 100;

    /// <summary>The modality for what the multiplexer says.</summary>
    public const byte Said = 101;

    private readonly MultiplexerSettings _settings;
    private readonly Random _rng;
    private readonly HashSet<int>? _kept;

    /// <summary>Which data bit each address value selects.</summary>
    private int[] _selects;

    private long _rounds;

    /// <param name="settings">The shape of the world.</param>
    /// <param name="seed">The world's own generator.</param>
    public Multiplexer(MultiplexerSettings settings, int seed)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentOutOfRangeException.ThrowIfLessThan(settings.Address, 1);

        // FIVE ADDRESS BITS IS THE THIRTY-SEVEN-BIT MULTIPLEXER, which is the
        // largest size the literature reports. Beyond it there is nothing to
        // compare against, so the guard is a reminder rather than a limitation.
        ArgumentOutOfRangeException.ThrowIfGreaterThan(settings.Address, 5);
        ArgumentOutOfRangeException.ThrowIfNegative(settings.Noise);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(settings.Noise, 1.0);
        ArgumentOutOfRangeException.ThrowIfNegative(settings.Switch);

        ArgumentOutOfRangeException.ThrowIfNegative(settings.Withheld);
        ArgumentOutOfRangeException.ThrowIfNegative(settings.Clutter);

        var assignments = 1 << (settings.Address + (1 << settings.Address));

        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(settings.Withheld, assignments);

        _settings = settings;
        _rng = new Random(seed);

        // NULL RATHER THAN A FULL SET WHEN NOTHING IS HELD BACK, so the draw's rejection
        // test is skipped entirely rather than merely always passing. That is what keeps
        // the generator consumed identically to before this existed.
        _kept = settings.Withheld == 0
            ? null
            : [.. Enumerable.Range(0, assignments - settings.Withheld)];

        // THE FIRST MAPPING IS THE IDENTITY WHATEVER `Switch` SAYS, so a run that
        // never switches is exactly the published world and its numbers stay
        // comparable. A switching run differs from it only after the first flip.
        _selects = [.. Enumerable.Range(0, Data)];
    }

    /// <summary>How many data bits there are.</summary>
    public int Data => 1 << _settings.Address;

    /// <summary>
    /// How many bits actually carry the function, which is what an assignment is.
    /// </summary>
    /// <remarks>
    /// <b>SEPARATE FROM <see cref="Bits"/> SO CLUTTER AND WITHHOLDING STAY
    /// ORTHOGONAL.</b> A withheld assignment is a setting of the bits that MATTER;
    /// counting the always-one ones into it would make almost every assignment
    /// unreachable and the held-out set mostly fiction.
    /// </remarks>
    public int Informative => _settings.Address + Data;

    /// <summary>How many bits are shown in one round, clutter included.</summary>
    public int Bits => Informative + _settings.Clutter;

    /// <summary>What a blind guess scores.</summary>
    /// <remarks>
    /// <b>A half, because the outcome is one bit</b> — and it is stated here rather
    /// than assumed at the call site, because an arm that quietly falls back to
    /// silence drifts toward this for free and the drift reads as a mechanism.
    /// </remarks>
    public static double Chance => 0.5;

    /// <summary>
    /// The assignments this world never draws, with what the function says about each.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>THE INSTRUMENT FORK 48 WAS ABOUT, AND THE MULTIPLEXER IS THE WORLD THAT NEEDED
    /// IT MOST.</b> Depth is genuinely required here — a rule shorter than the address
    /// plus one is unsound — so this is where a held-out score can say something no other
    /// generated world can, and it was the one world that held nothing back.
    /// </para>
    /// <para>
    /// <b>IT READS THE CURRENT MAPPING, exactly as <see cref="Truths"/> does.</b> On a
    /// switching run the answer to a withheld assignment moves with the target, and
    /// scoring against the answer it had at the start would measure the switch.
    /// </para>
    /// <para>
    /// <b>AND IT CARRIES <see cref="Round.Answer"/> RATHER THAN THE EMITTED OUTCOME.</b>
    /// Noise flips what a learner is TOLD, and nothing here is ever told to anyone — an
    /// examination asks what the population would say about a case the world kept, so the
    /// thing to mark it against is what the function says rather than what a lie would
    /// have said.
    /// </para>
    /// </remarks>
    public IReadOnlyList<Turn<IReadOnlyList<int>>> Withheld
    {
        get
        {
            if (_settings.Withheld == 0) return [];

            var total = 1 << Informative;

            return
            [
                .. Enumerable.Range(total - _settings.Withheld, _settings.Withheld)
                    .Select(assignment =>
                    {
                        var bits = Spread(assignment);

                        var address = 0;
                        for (var which = 0; which < _settings.Address; which++)
                            address = (address << 1) | bits[which];

                        return new Turn<IReadOnlyList<int>>
                        {
                            Seen = bits,
                            Outcome = bits[_settings.Address + _selects[address]],
                        };
                    }),
            ];
        }
    }

    /// <summary>
    /// The informative bits as one whole number, most significant first.
    /// </summary>
    /// <remarks>
    /// <b>Clutter is not part of an assignment</b>, being the same in every one of them.
    /// </remarks>
    private int Assignment(int[] bits)
    {
        var packed = 0;
        for (var which = 0; which < Informative; which++) packed = (packed << 1) | bits[which];
        return packed;
    }

    /// <summary>The inverse of <see cref="Assignment"/>, with the clutter put back.</summary>
    private int[] Spread(int assignment)
    {
        var bits = new int[Bits];

        for (var which = Informative - 1; which >= 0; which--)
        {
            bits[which] = assignment & 1;
            assignment >>= 1;
        }

        for (var which = Informative; which < Bits; which++) bits[which] = 1;

        return bits;
    }

    /// <summary>One round of the world.</summary>
    public Round Next()
    {
        // THE MAPPING MOVES BEFORE THE ROUND IT AFFECTS, so a run that switches
        // every N rounds has the old target for exactly N of them. Moving it after
        // would put one round of the new target under the old count.
        if (_settings.Switch > 0 && _rounds > 0 && _rounds % _settings.Switch == 0)
            _selects = [.. Enumerable.Range(0, Data).OrderBy(_ => _rng.Next())];

        _rounds++;

        var bits = new int[Bits];

        // DRAWN AND REDRAWN RATHER THAN PICKED FROM WHAT IS LEFT, AND THAT IS THE ONLY
        // SHAPE THAT KEEPS EVERY NUMBER THIS WORLD HAS EVER PRODUCED. Picking an index
        // out of the allowed assignments would take ONE draw from the generator where
        // this takes `Bits` of them, so the whole stream shifts and no measurement taken
        // before today stays comparable. With nothing withheld the loop below never
        // rejects, so the generator is consumed exactly as it always was.
        do
        {
            // THE INFORMATIVE BITS ARE DRAWN AND THE CLUTTER IS NOT, so a clutter dial
            // takes nothing from the generator and a run with none is bit-for-bit the run
            // that existed before the dial did.
            for (var which = 0; which < Informative; which++) bits[which] = _rng.Next(2);
            for (var which = Informative; which < bits.Length; which++) bits[which] = 1;
        }
        while (_kept is not null && !_kept.Contains(Assignment(bits)));

        var address = 0;
        for (var which = 0; which < _settings.Address; which++)
            address = (address << 1) | bits[which];

        var answer = bits[_settings.Address + _selects[address]];

        // NOISE FLIPS WHAT IS EMITTED AND NEVER WHAT IS TRUE. `Answer` is what the
        // function says and `Outcome` is what the learner sees, so a run can report
        // how much of its own failure was the world lying to it.
        var outcome = _settings.Noise > 0 && _rng.NextDouble() < _settings.Noise
            ? 1 - answer
            : answer;

        return new Round
        {
            Cues = [.. Enumerable.Range(0, Bits).Select(which => Of(which, bits[which]))],
            Answer = Says(answer),
            Outcome = Says(outcome),
        };
    }

    /// <summary>
    /// Every commitment a perfect learner would hold, and nothing it would not.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The count is <c>2^Address * 2</c> and it is derived rather than quoted.</b>
    /// One rule per (address value, value of the bit that address selects): the
    /// address bits are pinned, the selected data bit is pinned, and every other
    /// data bit is left unsaid because it genuinely does not matter. Eight rules at
    /// six bits, sixteen at eleven.
    /// </para>
    /// <para>
    /// <b>It reads the CURRENT mapping</b>, so on a switching run the answer key
    /// moves with the target. Scoring a run against the key it started with would
    /// measure the switch rather than the recovery.
    /// </para>
    /// </remarks>
    public ImmutableArray<Truth> Truths()
    {
        var truths = ImmutableArray.CreateBuilder<Truth>(Data * 2);

        for (var address = 0; address < Data; address++)
            for (var value = 0; value < 2; value++)
            {
                var scope = ImmutableArray.CreateBuilder<Code>(_settings.Address + 1);

                for (var which = 0; which < _settings.Address; which++)
                    scope.Add(Of(which, (address >> (_settings.Address - 1 - which)) & 1));

                scope.Add(Of(_settings.Address + _selects[address], value));

                // SORTED BECAUSE A SCOPE IS A SET. The order is already ascending by
                // construction, so this changes nothing today -- it is here so that
                // it still changes nothing when the construction moves.
                scope.Sort();

                truths.Add(new Truth { Scope = scope.ToImmutable(), Expects = Says(value) });
            }

        return truths.ToImmutable();
    }

    /// <summary>The widest gap this will enumerate across.</summary>
    /// <remarks>
    /// <b>Sixty-five thousand assignments per commitment, which is affordable across
    /// a population and is where affordable stops.</b> The alternative is sampling,
    /// and a soundness score that sampled would be a probability wearing the clothes
    /// of a proof.
    /// </remarks>
    public const int Widest = 16;

    /// <summary>Whether a scope pins enough that its soundness can be settled exactly.</summary>
    /// <param name="scope">The codes that must be present.</param>
    /// <remarks>
    /// <b>A COUNT OF SOUND RULES IS A LIE IF THE UNCHECKABLE ONES ARE COUNTED AS
    /// UNSOUND.</b> A one-code scope in a twenty-bit world leaves nineteen free, so
    /// what cannot be settled is reported as its own number rather than folded into
    /// the bad news.
    /// </remarks>
    public bool Checkable(ImmutableArray<Code> scope) =>
        !scope.IsDefaultOrEmpty
        && Bits - scope.Select(code => (int)(code.Value >> 1)).Distinct().Count() <= Widest;

    /// <summary>
    /// Whether a scope really does entail an expectation, for every world it allows.
    /// </summary>
    /// <param name="scope">The codes that must be present.</param>
    /// <param name="expects">What is claimed to follow.</param>
    /// <remarks>
    /// <para>
    /// <b>THE ANSWER KEY IS ONE BASIS AND THE WORLD ADMITS SEVERAL, WHICH
    /// <see cref="Truths"/> ALONE CANNOT SAY.</b> Pinning both data bits an address
    /// pair could select — *this address bit is zero, and the two bits it might
    /// choose between are both zero* — is a TRUE rule, and it is not in the key. A
    /// learner scored only against the key is being marked on which basis it picked.
    /// </para>
    /// <para>
    /// <b>So this asks whether the rule is true rather than whether it is mine</b>,
    /// by enumerating every assignment the scope leaves open and checking the
    /// function agrees on all of them. That is exact rather than sampled, and it is
    /// basis-independent, which is what a ground-truth score was supposed to be.
    /// </para>
    /// </remarks>
    public bool Sound(ImmutableArray<Code> scope, Code expects)
    {
        if (scope.IsDefaultOrEmpty) return false;

        // ENUMERATION IS EXPONENTIAL IN THE FREE BITS, so this is honest about where
        // it stops rather than quietly sampling.
        if (!Checkable(scope))
            throw new InvalidOperationException("too many free bits to enumerate exactly");

        var pinned = new int?[Bits];

        foreach (var code in scope)
        {
            if (code.Modality != Bit) return false;

            var position = (int)(code.Value >> 1);
            var value = (int)(code.Value & 1);

            if (position >= Bits) return false;

            // A SCOPE PINNING A BIT BOTH WAYS IS SATISFIED BY NOTHING, so it entails
            // everything vacuously -- and calling that sound would let a learner
            // score by minting contradictions.
            if (pinned[position] is { } already && already != value) return false;

            pinned[position] = value;
        }

        var free = Enumerable.Range(0, Bits).Where(one => pinned[one] is null).ToList();

        for (var draw = 0; draw < 1 << free.Count; draw++)
        {
            var bits = new int[Bits];

            for (var which = 0; which < Bits; which++) bits[which] = pinned[which] ?? 0;
            for (var which = 0; which < free.Count; which++) bits[free[which]] = (draw >> which) & 1;

            var address = 0;
            for (var which = 0; which < _settings.Address; which++)
                address = (address << 1) | bits[which];

            if (Says(bits[_settings.Address + _selects[address]]) != expects) return false;
        }

        return true;
    }

    /// <summary>The code for one bit's position and value.</summary>
    /// <param name="position">Which bit, address bits first.</param>
    /// <param name="value">Its value, zero or one.</param>
    public static Code Of(int position, int value)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(position);
        ArgumentOutOfRangeException.ThrowIfNegative(value);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 1);

        return new Code(Bit, (ulong)((position << 1) | value));
    }

    /// <summary>The code for what the multiplexer says.</summary>
    /// <param name="value">Zero or one.</param>
    public static Code Says(int value)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(value);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 1);

        return new Code(Said, (ulong)value);
    }
}
