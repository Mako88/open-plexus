using System.Collections.Immutable;

namespace OpenPlexus.Codes;

/// <summary>
/// A real-valued reading said as a SPARSE SET OF WINNERS — <b>step 8's other
/// half,</b> and the front end for a signal that is not already a symbol.
/// </summary>
/// <remarks>
/// <para>
/// <b>The plan asks for "a hash spending its bits where the data is without
/// being fitted".</b> That reads as a contradiction until the adaptivity moves.
/// A projection chosen to suit the data is a fitted codebook, which the red-ball
/// property forbids outright — two machines fitted on different samples agree
/// about under 0.12 of items. So the projection cannot adapt. <b>What CAN adapt
/// is which of its outputs survive,</b> decided per observation and decided the same
/// way on every machine. Nothing is remembered between readings, so there is
/// nothing to fit.
/// </para>
/// <para>
/// <b>This is the fruit fly's olfactory circuit</b>, and it is an LSH scheme —
/// Dasgupta, Stevens and Navlakha, <i>Science</i> 2017. Fifty receptor types
/// project to two thousand Kenyon cells over sparse random connections, and one
/// inhibitory neuron then silences all but the strongest few per cent. The tag is
/// the set that survived. Their result is that this is locality-sensitive
/// hashing: similar inputs get overlapping tags, with the guarantee falling out
/// of the randomness rather than out of any training.
/// </para>
/// <para>
/// <b>It expands where textbook LSH contracts</b>, and that is the part worth
/// copying. Classical LSH projects down into a short dense code; this
/// projects UP and then throws most of it away. The expansion is what preserves
/// similarity on little data — and a sparse set of fired codes is exactly what
/// this architecture already eats, so the fly's output form needs no adapting at
/// all. <b>A dense code would have to be unpacked into codes; this IS codes.</b>
/// </para>
/// <para>
/// <b>What it is for, and why <see cref="Grains"/> IS NOT ENOUGH.</b> Grains take
/// a reading that has ALREADY been banded and say it again more coarsely, so the
/// hierarchy is the similarity — but it is a hierarchy per DIMENSION, and two
/// readings that differ a little in every dimension share no band at any grain.
/// This reads the dimensions TOGETHER: a cell fires on a weighted sum of several,
/// so a small move anywhere leaves most winners standing. <b>They are the two
/// halves of one step and neither is the other's arm</b> — grains generalise
/// along one axis at a time, this generalises across them.
/// </para>
/// <para>
/// <b>The projection is a constant</b> of the design and not of a run, which is why
/// no seed is taken. A seed parameter would let two machines be handed
/// different ones, and the red-ball property would be gone without anything
/// failing — the codes would simply mean different things in two places. Making
/// it impossible to pass is stronger than documenting that nobody should.
/// </para>
/// <para>
/// <b>And it is derived by arithmetic rather than from <see cref="Random"/>.</b>
/// <see cref="Worlds.Seeds"/> already records that a seeded generator normalises
/// its seed by magnitude, and <see cref="Worlds.Kinds.Named"/> already refuses
/// <see cref="string.GetHashCode()"/> for being randomised per process. A
/// codebook that must be identical on every machine FOREVER should not rest on a
/// library's internals at all: the connections here are a stated mix of the cell
/// number and the sample number, so the whole codebook is defined by the
/// arithmetic in this file.
/// </para>
/// </remarks>
public sealed class Winnow
{
    /// <summary>
    /// What separates this codebook from every other use of the mixer.
    /// <b>Fixed forever</b> — changing it renumbers every code this front end has
    /// ever emitted, on every machine, which is the one edit that cannot be made.
    /// </summary>
    private const uint Codebook = 0x_5F1E_5C01;

    /// <summary>
    /// How many distinct tags are worth remembering before the question is
    /// settled.
    /// </summary>
    /// <remarks>
    /// <b>A bound on the watching and not on the front end.</b> The question this
    /// answers is <i>did the sheet collapse</i>, and a front end that has emitted
    /// this many distinct tags has answered it — counting further would grow
    /// without limit for no more information. Saturating for the same reason
    /// <see cref="Wirings"/> saturates.
    /// </remarks>
    private const int Watch = 4096;

    private readonly byte _modality;
    private readonly int _inputs;
    private readonly int _winners;

    /// <summary>The distinct tags seen so far, to <see cref="Watch"/>.</summary>
    /// <remarks>
    /// <b>Hashes rather than the tags themselves</b>, because what is being
    /// counted is how many there are and never which. A collision would undercount
    /// and so can only ever make this report a collapse that is not there — the
    /// safe direction for a check whose whole job is to complain.
    /// </remarks>
    private readonly HashSet<int> _tags = [];

    /// <summary>Guards <see cref="_tags"/> and <see cref="_emitted"/>.</summary>
    /// <remarks>
    /// <b>One machine may hand readings in from several threads</b>, and a torn
    /// count here would read as a collapse. Taken after the tag is computed, so
    /// nothing that costs anything happens inside it.
    /// </remarks>
    private readonly Lock _watching = new();

    private long _emitted;

    /// <summary>
    /// Which inputs each cell listens to. <b>Cell <c>c</c> reads
    /// <c>_wiring[c]</c>, and nothing else ever.</b>
    /// </summary>
    private readonly ImmutableArray<ImmutableArray<int>> _wiring;

    /// <param name="modality">
    /// Which front end this is. <b>One modality, unlike <see cref="Grains"/></b> —
    /// every code here is a winner among peers, so they are all the same kind of
    /// claim and there is nothing to keep apart.
    /// </param>
    /// <param name="inputs">How many numbers a reading has.</param>
    /// <param name="cells">
    /// How many cells the reading is projected onto. <b>Many more than
    /// <paramref name="inputs"/></b> — the fly runs forty times as many, and the
    /// expansion is where the similarity survives.
    /// </param>
    /// <param name="samples">
    /// How many inputs each cell listens to. <b>Few, and that is the whole of
    /// "sparse"</b> — the fly's Kenyon cells take about six of fifty. A cell
    /// reading everything would fire on the total and say nothing about shape.
    /// </param>
    /// <param name="winners">
    /// How many cells survive. <b>The fly keeps about a twentieth</b>, and this is
    /// the only place anything adapts to the reading.
    /// </param>
    public Winnow(byte modality, int inputs, int cells, int samples, int winners)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(inputs);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(cells);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(samples);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(winners);

        // A cell cannot listen to an input twice, so it cannot want more distinct
        // inputs than exist. Sampling with replacement would quietly weight one
        // input double in that cell, which is a claim nobody made.
        ArgumentOutOfRangeException.ThrowIfGreaterThan(samples, inputs);

        // And winnowing to everything is not winnowing. With every cell surviving
        // there is no competition, the tag is the same set for every reading, and
        // the one adaptive step in the design has been switched off by arithmetic.
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(winners, cells);

        // And a narrow reading cannot fill a wide sheet -- but this reads the
        // declared width and not the real one, so it is necessary and not
        // sufficient. Fifty inputs whose variation is really three-dimensional pass
        // it comfortably and still collapse onto a handful of tags, which is the
        // CLEVR failure wearing a disguise this arithmetic cannot see. `Distinct`
        // is the empirical half and the only one that can catch that.
        //
        // There are only C(inputs, samples) distinct wirings,
        // so beyond that every further cell REPEATS one -- it fires identically on
        // every reading forever and can never separate two of them. Measured on
        // CLEVR's `3d_coords`: three inputs over four thousand objects produced
        // three distinct tags at 128 cells and ONE at 2,000, against four thousand
        // for the same front end on a fifty-number reading.
        //
        // It is not a cap on cells, it is a statement about the reading. The fly
        // expands fifty receptors onto two thousand cells because C(50, 6) is
        // astronomical; expansion buys similarity only where there is room to
        // expand INTO, and asking for more cells than that is claiming a resolution
        // the sense does not have.
        var distinct = Wirings(inputs, samples);

        if (cells > distinct)
            throw new ArgumentOutOfRangeException(nameof(cells),
                $"{inputs} inputs sampled {samples} at a time give only {distinct} "
                + $"distinct wirings, so {cells} cells cannot all differ. Widen the "
                + "reading or narrow the sheet -- a repeated cell fires identically "
                + "on every reading and separates nothing.");

        _modality = modality;
        _inputs = inputs;
        _winners = winners;
        _wiring = Wire(inputs, cells, samples);
    }

    /// <summary>
    /// How many distinct sets of inputs a cell could possibly listen to.
    /// </summary>
    /// <remarks>
    /// <b>Saturating rather than exact.</b> The honest answer for a real
    /// sense overflows and the question is only ever "is it bigger than the
    /// sheet". C(50, 6) is about sixteen million and C(1000, 6) is past
    /// anything a <see cref="long"/> holds, so the multiplication stops as soon as
    /// it passes what any caller could ask for.
    /// </remarks>
    private static long Wirings(int inputs, int samples)
    {
        var total = 1L;

        for (var step = 0; step < samples; step++)
        {
            total = total * (inputs - step) / (step + 1);

            // Nothing above this can change an answer, since `cells` is an `int`.
            if (total > int.MaxValue) return int.MaxValue;
        }

        return total;
    }

    /// <summary>
    /// The sparse random connections, built once from the arithmetic alone.
    /// </summary>
    /// <remarks>
    /// <b>REJECTION RATHER THAN A SHUFFLE</b>, because a shuffle needs a generator and
    /// the point is to need nothing. Each draw is a mix of the cell, the
    /// sample slot and a retry counter, taken modulo the inputs; a collision with
    /// an input this cell already has is redrawn. <b>The retry is part of the
    /// definition</b> — two machines collide identically and redraw identically,
    /// so the wiring is a pure function of the three shape numbers.
    /// </remarks>
    private static ImmutableArray<ImmutableArray<int>> Wire(int inputs, int cells, int samples)
    {
        var wiring = ImmutableArray.CreateBuilder<ImmutableArray<int>>(cells);

        for (var cell = 0; cell < cells; cell++)
        {
            var taken = new List<int>(samples);

            for (var slot = 0; slot < samples; slot++)
            {
                // BOUNDED BY CONSTRUCTION. There are strictly more inputs than a
                // cell wants, so a free one always exists; the retry count only
                // has to be large enough that finding it is not luck.
                for (var retry = 0; ; retry++)
                {
                    var drawn = (int)(Mix(cell, slot, retry) % (uint)inputs);
                    if (taken.Contains(drawn)) continue;

                    taken.Add(drawn);
                    break;
                }
            }

            // SORTED, so the wiring reads the same however it was drawn. Nothing
            // downstream depends on it, which is exactly why it should not be
            // allowed to vary.
            taken.Sort();
            wiring.Add([.. taken]);
        }

        return wiring.MoveToImmutable();
    }

    /// <summary>
    /// The stated mix — <b>the whole codebook, in four lines.</b>
    /// </summary>
    /// <remarks>
    /// The same avalanche <see cref="Worlds.Seeds.Apart"/> uses, over three
    /// numbers rather than two. <b>It is written out here rather than called</b>
    /// because that one returns a seed for a generator and this one returns the
    /// draw itself, and a shared helper would invite someone to change the
    /// constants for the other caller's benefit.
    /// </remarks>
    private static uint Mix(int cell, int slot, int retry)
    {
        unchecked
        {
            var mixed = Codebook ^ (uint)cell;
            mixed = (mixed ^ (mixed >> 16)) * 0x7FEB_352D;
            mixed ^= (uint)slot * 0x9E37_79B9;
            mixed = (mixed ^ (mixed >> 15)) * 0x846C_A68B;
            mixed ^= (uint)retry * 0x85EB_CA6B;
            mixed = (mixed ^ (mixed >> 16)) * 0x2545_F491;

            return mixed ^ (mixed >> 16);
        }
    }

    /// <summary>How many cells a reading is spread over.</summary>
    public int Cells => _wiring.Length;

    /// <summary>
    /// How many DISTINCT tags this front end has actually emitted, up to
    /// <see cref="Watch"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The honest version of the constructor's guard</b>, and the only one that can
    /// see a collapse. That check compares the sheet against C(inputs,
    /// samples) — the width the caller DECLARED — and real data routinely varies in
    /// far fewer dimensions than it has numbers. Fifty inputs whose variation is
    /// really three-dimensional pass the arithmetic and still emit a handful of
    /// tags forever, which is exactly the CLEVR failure the guard was written for
    /// and exactly the case it cannot detect.
    /// </para>
    /// <para>
    /// <b>A count and not a verdict.</b> Nothing here decides what "too few" is —
    /// that depends on how many things the world contains, which a front end has no
    /// business knowing. It is reported beside <see cref="Emitted"/> so a caller
    /// can say; see <c>WinnowTests</c>.
    /// </para>
    /// </remarks>
    public int Distinct
    {
        get { lock (_watching) return _tags.Count; }
    }

    /// <summary>How many readings this front end has been handed.</summary>
    /// <remarks>
    /// <b>The denominator, and without it <see cref="Distinct"/> says nothing.</b>
    /// Three distinct tags over three readings is a front end working perfectly;
    /// three over four thousand is the collapse.
    /// </remarks>
    public long Emitted
    {
        get { lock (_watching) return _emitted; }
    }

    /// <summary>
    /// The codes present in one reading.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The reading is centred first, which is the fly's divisive
    /// normalisation.</b> A tag should say what SHAPE the reading has and not how
    /// loud it was — a smell twice as strong is the same smell, and a garden
    /// uniformly damper is the same arrangement of plants. Without centring, the
    /// cells reading the most inputs win every time whatever arrives, and the tag
    /// stops varying at all.
    /// </para>
    /// <para>
    /// <b>Ties break on the cell number, and that is not a detail.</b> A reading
    /// where several cells sum equal is exactly what a coarse or repetitive world
    /// produces, and leaving those to the sort's own order would make the tag
    /// depend on something nobody chose — which is the fault this session found
    /// sitting in the walk's accumulator.
    /// </para>
    /// </remarks>
    /// <param name="reading">
    /// One observation, as numbers. <b>As long as this front end was built for</b>
    /// — a reading of a different length is a different sense.
    /// </param>
    public ImmutableArray<Code> Of(IReadOnlyList<double> reading)
    {
        ArgumentNullException.ThrowIfNull(reading);

        // A reading of the wrong length is a different sense, and quietly reading
        // the first few numbers of it would emit codes that look exactly like this
        // front end's and mean something else.
        if (reading.Count != _inputs)
            throw new ArgumentException(
                $"this front end reads {_inputs} numbers and was handed "
                + $"{reading.Count}", nameof(reading));

        // WHAT SHAPE, NOT HOW LOUD. See above.
        var mean = 0.0;
        for (var i = 0; i < reading.Count; i++) mean += reading[i];
        mean /= reading.Count;

        var fired = new double[_wiring.Length];

        for (var cell = 0; cell < _wiring.Length; cell++)
        {
            var sum = 0.0;
            foreach (var input in _wiring[cell]) sum += reading[input] - mean;
            fired[cell] = sum;
        }

        // The inhibition, and it is the only adaptive step in the design. Every
        // cell competes with every other and the strongest few survive, so which
        // bits get spent is decided by THIS reading -- which is what the plan
        // asked for, without a codebook having been fitted to anything.
        var order = new int[_wiring.Length];
        for (var cell = 0; cell < order.Length; cell++) order[cell] = cell;

        Array.Sort(order, (left, right) =>
        {
            var strength = fired[right].CompareTo(fired[left]);
            return strength != 0 ? strength : left.CompareTo(right);
        });

        var codes = ImmutableArray.CreateBuilder<Code>(_winners);

        // Emitted in cell order rather than in strength order. The graph is handed
        // a SET, and how strongly each winner fired is not something a count can
        // hold -- so a stable order is worth more than a meaningless one.
        var winners = order.Take(_winners).Order().ToArray();
        foreach (var cell in winners) codes.Add(new Code(_modality, (ulong)cell));

        // What the sheet is actually resolving, counted rather than assumed. See
        // `Distinct` -- the constructor's guard reads the declared width, and this
        // is the half of it that can see a reading whose real dimension is lower.
        var tag = 17;
        foreach (var cell in winners) tag = (tag * 31) + cell;

        lock (_watching)
        {
            _emitted++;
            if (_tags.Count < Watch) _tags.Add(tag);
        }

        return codes.MoveToImmutable();
    }
}
