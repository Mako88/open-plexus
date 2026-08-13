using System.Collections.Immutable;
using System.Globalization;
using System.Text.Json;
using OpenPlexus.Codes;

namespace OpenPlexus.Worlds;

/// <summary>
/// How much of CLEVR to read, and how much of the binding the front end is
/// allowed to hand over.
/// </summary>
/// <remarks>
/// <b>The three arms are the same three the binding world has</b>, deliberately.
/// That world was built here to measure a ceiling and then lift it; this is the
/// same measurement on scenes somebody else generated, so the arms have to line
/// up or the comparison says nothing.
/// </remarks>
public sealed record ClevrSettings
{
    /// <summary>The extracted <c>CLEVR_v1.0</c> directory.</summary>
    public required string Corpus { get; init; }

    /// <summary>How many scenes to read, from the start of the validation split.</summary>
    public int Scenes { get; init; } = 200;

    /// <summary>
    /// Whether the front end says which attributes belong to which object.
    /// </summary>
    /// <remarks>
    /// <b>OFF IS THE CEILING FORK 25 MEASURED, ON SOMEBODY ELSE'S SCENES.</b> A
    /// scene of a red cube and a blue sphere emits exactly the codes a scene of a
    /// blue cube and a red sphere does, so with this off no amount of counting
    /// separates them — and CLEVR is built almost entirely out of that confusion.
    /// It rides <see cref="Codes.IQuantizer{TObservation}.Bind"/>, which has had no
    /// reader since the walk went.
    /// </remarks>
    public bool Segmented { get; init; } = true;

    /// <summary>
    /// Whether each object gets an index code of its own.
    /// </summary>
    /// <remarks>
    /// <b>Without it there is nothing for a conjunction to agree on.</b> There are
    /// only fifteen attribute values in the whole of CLEVR, so every attribute
    /// co-occurs with every other one across enough scenes and the global counts
    /// say nothing at all. The index is what makes <i>this</i> large metal thing a
    /// thing rather than a coincidence of two common codes.
    /// </remarks>
    public bool Tagged { get; init; } = true;

    /// <summary>
    /// Whether those indexes are declared fleeting — one way, index to attribute.
    /// </summary>
    /// <remarks>
    /// <b>OFF HERE, AND THAT IS NOT THE USUAL RECOMMENDATION.</b> Everywhere else
    /// a fleeting index is the right call, because the row an index writes into a
    /// lasting node grows forever and buys nothing — a question carries the index
    /// it asks about, so the walk starts there and never arrives at one. This
    /// world's question is the exception: it does <b>not</b> know which object it
    /// means, it knows two of that object's attributes, so the walk has to be able
    /// to arrive at an index to find out. Turning it on is the arm that measures
    /// what that costs.
    /// </remarks>
    public bool Fleeting { get; init; }
}

/// <summary>One CLEVR scene, as codes.</summary>
public sealed record Sighting
{
    /// <summary>Which scene, by the corpus's own numbering.</summary>
    public required int Scene { get; init; }

    /// <summary>Every attribute of every object, and the object indexes.</summary>
    public required ImmutableArray<Code> Codes { get; init; }

    /// <summary>
    /// Which codes belong to which object. <b>Null when the segmented arm is
    /// off</b>, which is the flat set fork 25 measured the ceiling of.
    /// </summary>
    public IReadOnlyDictionary<Code, int>? Groups { get; init; }

    /// <summary>The object indexes, when they are declared fleeting.</summary>
    public IReadOnlySet<Code>? Fleeting { get; init; }
}

/// <summary>
/// One CLEVR question that this architecture can be asked at all.
/// </summary>
/// <remarks>
/// <b>Taken from the question's PROGRAM and not from its English.</b> CLEVR ships
/// a functional program beside every question, and using it is the difference
/// between measuring binding and measuring a parser. This project has no language
/// model and is not pretending to — the claim under test is that attributes can
/// be recombined, not that a sentence can be read. <b>The bAbI world does take raw
/// text</b>, so the two are not both dodging it.
/// </remarks>
public sealed record Referred
{
    /// <summary>Which scene it is about.</summary>
    public required int Scene { get; init; }

    /// <summary>
    /// Which object of that scene the filters pick out.
    /// </summary>
    /// <remarks>
    /// <b>Resolved by running the filter chain against the scene, and a question
    /// that does not pick out exactly one object is thrown away.</b> CLEVR
    /// guarantees this for its own programs — <c>unique</c> is in the chain — but
    /// checking it here is what lets the index arm exist: the ceiling arm has to
    /// be able to hand the walk the very index the question is not supposed to
    /// have, and nothing can do that without knowing which object is meant.
    /// </remarks>
    public required int Slot { get; init; }

    /// <summary>
    /// The attribute values the question filters by — <b>the conjunction</b>.
    /// </summary>
    public required ImmutableArray<Code> Origins { get; init; }

    /// <summary>The index of the object meant. <b>The ceiling arm's origin.</b></summary>
    public Code Tag => Clevr.Thing(Scene, Slot);

    /// <summary>Which attribute is being asked for.</summary>
    public required byte Asking { get; init; }

    /// <summary>The attribute value the corpus says is right.</summary>
    public required Code Answer { get; init; }

    /// <summary>The answer as the corpus writes it, for reporting.</summary>
    public required string Says { get; init; }
}

/// <summary>
/// CLEVR (Johnson et al., 2017) — attribute binding with conjunctive reference,
/// on scenes nobody here generated.
/// </summary>
/// <remarks>
/// <para>
/// <b>THIS IS THE BINDING WORLD'S EXPERIMENT, RUN ON SOMEBODY ELSE'S DATA.</b>
/// Fork 25 built a world specifically to fail — two objects with swapped
/// attributes emitting one code set — measured the ceiling, and then lifted it
/// with grouping and a per-object index. Every one of those numbers was taken on
/// scenes generated by the same hands that built the mechanism. CLEVR is fifteen
/// thousand scenes of exactly that confusion, generated in 2017 by people who had
/// never heard of this project.
/// </para>
/// <para>
/// <b>No vision, and none is needed.</b> The scene graphs ship as JSON with each
/// object's colour, size, shape and material already separated, which is the
/// front end this architecture would otherwise have to fake. What is being tested
/// is what a learner does with a segmented signal, which is the split
/// <see cref="Codes.IQuantizer{TObservation}.Bind"/> exists to carry.
/// </para>
/// <para>
/// <b>ONLY THE QUESTIONS THIS SYSTEM CAN EXPRESS AN ANSWER TO ARE KEPT</b>, and
/// that is most of CLEVR thrown away. A walk returns one endpoint, so counting
/// (<i>how many cubes</i>), existence (<i>are there any</i>), comparison and
/// spatial relations are all structurally unanswerable here — as is anything
/// needing two hops through a relation. What is left is <c>query_&lt;attribute&gt;</c>
/// over a pure filter chain: <i>what shape is the large metal thing</i>. That is
/// the conjunctive reference, and it is the only part of CLEVR this architecture
/// is making a claim about.
/// </para>
/// </remarks>
public sealed class Clevr
{
    /// <summary>One of eight colours.</summary>
    public const byte Colour = 50;

    /// <summary>One of two sizes.</summary>
    public const byte Size = 51;

    /// <summary>One of three shapes.</summary>
    public const byte Shape = 52;

    /// <summary>One of two materials.</summary>
    private const byte Material = 53;

    /// <summary>The index standing for one object. See <see cref="ClevrSettings.Tagged"/>.</summary>
    public const byte Object = 54;

    /// <summary>
    /// The index standing for one scene — <b>which picture is being looked at.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>WITHOUT IT THE QUESTION IS NOT ASKABLE, AND THAT WAS MEASURED THE HARD
    /// WAY.</b> <i>The large metal thing</i> names one object of one scene and
    /// several hundred objects of the corpus, so a conjunction with no scene in it
    /// found the object meant about as often as picking one at random — which read
    /// as the architecture failing and was the harness never having asked the
    /// question.
    /// </para>
    /// <para>
    /// <b>It is not knowledge of the answer.</b> Somebody asking <i>what shape is
    /// the big metal thing</i> is looking at a picture while they ask, and which
    /// picture is the one thing they certainly know. It is always fleeting: minted
    /// per scene and never seen again, so it records one way and no lasting node
    /// grows a row entry for it.
    /// </para>
    /// </remarks>
    public const byte Where = 55;

    /// <summary>
    /// What the corpus calls each attribute, and which modality it becomes.
    /// </summary>
    /// <remarks>
    /// <b>The four keys are the object's own JSON fields and the four
    /// <c>filter_</c> and <c>query_</c> functions are named after them</b>, so one
    /// table serves the scenes and the programs both.
    /// </remarks>
    private static readonly (string Name, byte Modality)[] Attributes =
    [
        ("color", Colour), ("size", Size), ("shape", Shape), ("material", Material),
    ];

    private readonly ClevrSettings _settings;
    private readonly Dictionary<int, List<Referred>> _asking = [];

    /// <summary>
    /// Every object of every scene read, by attribute. <b>The world's own record,
    /// which nothing on the thinking path ever sees.</b>
    /// </summary>
    /// <remarks>
    /// It exists to resolve which object a question means, which is needed twice:
    /// to throw away questions that do not name exactly one, and to let the
    /// ceiling arm hand the walk the index the question is not supposed to have.
    /// </remarks>
    private readonly Dictionary<int, List<Dictionary<byte, Code>>> _objects = [];

    /// <param name="settings">How much to read, and which arms are on.</param>
    /// <exception cref="FileNotFoundException">The corpus is not there.</exception>
    public Clevr(ClevrSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(settings.Scenes);

        _settings = settings;

        Scenes = ReadScenes(
            Beside(settings.Corpus, "scenes", "CLEVR_val_scenes.json"), settings, _objects);

        foreach (var question in ReadQuestions(
                     Beside(settings.Corpus, "questions", "CLEVR_val_questions.json"), _objects))
        {
            if (!_asking.TryGetValue(question.Scene, out var list))
                _asking[question.Scene] = list = [];

            list.Add(question);
        }

        Asked = _asking.Values.Sum(list => list.Count);

        // CHANCE IS PER ATTRIBUTE AND THE QUESTIONS ARE NOT EVENLY SPLIT BETWEEN
        // THEM. Colour has eight values and material two, so a run that happened
        // to draw mostly material questions would look far better against one
        // flat number. This is the blind draw the actual mix of questions faces.
        var answers = _asking.Values
            .SelectMany(list => list)
            .GroupBy(question => question.Asking)
            .ToList();

        var values = Scenes
            .SelectMany(scene => scene.Codes)
            .GroupBy(code => code.Modality)
            .ToDictionary(group => group.Key, group => group.Distinct().Count());

        Chance = Asked == 0 ? 0.0 : answers.Sum(group =>
            group.Count() / (double)Asked / Math.Max(values.GetValueOrDefault(group.Key, 1), 1));
    }

    /// <summary>The scenes, in the order the corpus numbers them.</summary>
    public IReadOnlyList<Sighting> Scenes { get; }

    /// <summary>How many answerable questions were found across those scenes.</summary>
    public int Asked { get; }

    /// <summary>
    /// A blind draw, weighted by how the questions actually divide between the
    /// four attributes.
    /// </summary>
    public double Chance { get; }

    /// <summary>What is asked about one scene. Empty is normal.</summary>
    public IReadOnlyList<Referred> About(int scene) =>
        _asking.TryGetValue(scene, out var list) ? list : [];

    /// <summary>The code for one attribute value, whatever object has it.</summary>
    public static Code Of(byte modality, string value) => Kinds.Named(modality, value);

    /// <summary>The index standing for one object of one scene.</summary>
    /// <remarks>
    /// <b>Scene and slot both, or object 0 of every scene would be one node</b> —
    /// which is a hub joining every scene to every other and the exact opposite of
    /// what an index is for.
    /// </remarks>
    internal static Code Thing(int scene, int slot)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(scene);
        ArgumentOutOfRangeException.ThrowIfNegative(slot);

        return Kinds.Named(Object, string.Create(
            CultureInfo.InvariantCulture, $"{scene}:{slot}"));
    }

    /// <inheritdoc cref="Where"/>
    public static Code Seen(int scene)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(scene);
        return new Code(Where, (ulong)scene);
    }

    /// <summary>Turns the scenes file into occasions.</summary>
    /// <param name="path">The scenes JSON.</param>
    /// <param name="settings">How many to read, and which arms are on.</param>
    /// <param name="objects">Filled in with each scene's objects, by attribute.</param>
    private static IReadOnlyList<Sighting> ReadScenes(
        string path,
        ClevrSettings settings,
        Dictionary<int, List<Dictionary<byte, Code>>> objects)
    {
        using var file = File.OpenRead(path);
        using var json = JsonDocument.Parse(file);

        var read = new List<Sighting>();

        foreach (var scene in json.RootElement.GetProperty("scenes").EnumerateArray())
        {
            if (read.Count >= settings.Scenes) break;

            var index = scene.GetProperty("image_index").GetInt32();

            var codes = new List<Code>();
            var groups = new Dictionary<Code, int>();
            var indexes = new HashSet<Code>();
            var held = new List<Dictionary<byte, Code>>();
            var slot = 0;

            foreach (var thing in scene.GetProperty("objects").EnumerateArray())
            {
                var mine = new Dictionary<byte, Code>();
                held.Add(mine);

                foreach (var (name, modality) in Attributes)
                {
                    var code = Of(modality, thing.GetProperty(name).GetString()!);
                    codes.Add(code);
                    mine[modality] = code;

                    // AN ATTRIBUTE VALUE IS ONE NODE ACROSS THE WHOLE CORPUS, so
                    // two objects sharing a colour share its code and the group
                    // map cannot hold both. That is the binding problem itself and
                    // not a parsing detail: whichever object claims the code, the
                    // OTHER one's colour is then ungrouped and pairs with
                    // everything. The index below is what makes the pairing
                    // recoverable anyway.
                    groups.TryAdd(code, slot);
                }

                if (settings.Tagged)
                {
                    var tag = Thing(index, slot);
                    codes.Add(tag);
                    groups.TryAdd(tag, slot);
                    indexes.Add(tag);
                }

                slot++;
            }

            objects[index] = held;

            // WHICH PICTURE THIS IS, ungrouped so it pairs with every object, and
            // always fleeting -- see Where.
            var here = Seen(index);
            codes.Add(here);
            indexes.Add(here);

            read.Add(new Sighting
            {
                Scene = index,
                Codes = [.. codes],
                Groups = settings.Segmented ? groups : null,

                // THE SCENE IS FLEETING WHATEVER THE ARM SAYS; the arm is about
                // the OBJECT indexes, which the walk has to be able to arrive at.
                Fleeting = settings is { Tagged: true, Fleeting: true }
                    ? indexes
                    : new HashSet<Code> { here },
            });
        }

        return read;
    }

    /// <summary>
    /// Every question about one of these scenes that is a pure filter chain
    /// ending in a query.
    /// </summary>
    /// <remarks>
    /// <b>The rejected functions are rejected for a reason each.</b>
    /// <c>relate</c> and the <c>same_*</c> family need a second hop from a first
    /// answer, which is a different claim; <c>count</c>, <c>exist</c> and the
    /// comparisons need an answer that is not a code the graph holds. What
    /// survives is a conjunction of attribute values naming one object.
    /// </remarks>
    private static IEnumerable<Referred> ReadQuestions(
        string path, Dictionary<int, List<Dictionary<byte, Code>>> objects)
    {
        using var file = File.OpenRead(path);
        using var json = JsonDocument.Parse(file);

        foreach (var question in json.RootElement.GetProperty("questions").EnumerateArray())
        {
            var scene = question.GetProperty("image_index").GetInt32();
            if (!objects.TryGetValue(scene, out var held)) continue;

            var origins = new List<Code>();
            byte asking = 0;
            var refused = false;

            foreach (var step in question.GetProperty("program").EnumerateArray())
            {
                var function = step.GetProperty("function").GetString()!;

                if (function is "scene" or "unique") continue;

                if (Named(function, "filter_") is { } filtered)
                {
                    origins.Add(Of(filtered, step.GetProperty("value_inputs")[0].GetString()!));
                    continue;
                }

                if (Named(function, "query_") is { } queried)
                {
                    asking = queried;
                    continue;
                }

                refused = true;
                break;
            }

            // TWO OR MORE, OR IT IS NOT A CONJUNCTION. A single filter names a
            // whole class rather than an object, and the answer is then whatever
            // is commonest -- which measures the corpus and not the binding.
            if (refused || asking == 0 || origins.Count < 2) continue;

            // WHICH OBJECT THE FILTERS ACTUALLY NAME, worked out by running them.
            // Exactly one, or this is not a reference and the arms that hand the
            // walk an index would have nothing to hand it.
            var matched = -1;
            var many = false;

            for (var slot = 0; slot < held.Count; slot++)
            {
                if (!origins.All(code =>
                        held[slot].TryGetValue(code.Modality, out var mine) && mine == code))
                    continue;

                if (matched >= 0) { many = true; break; }
                matched = slot;
            }

            if (many || matched < 0) continue;

            var says = question.GetProperty("answer").GetString()!;

            // AND THE CORPUS HAS TO AGREE WITH THE SCENE. If the object the
            // filters picked does not carry the answer the corpus wrote down,
            // something has been misread and the question is dropped rather than
            // scored against a resolution nobody can trust.
            if (!held[matched].TryGetValue(asking, out var truth) || truth != Of(asking, says))
                continue;

            yield return new Referred
            {
                Scene = scene,
                Slot = matched,
                Origins = [.. origins],
                Asking = asking,
                Answer = truth,
                Says = says,
            };
        }
    }

    /// <summary>Which modality a <c>filter_</c> or <c>query_</c> function is about.</summary>
    private static byte? Named(string function, string prefix)
    {
        if (!function.StartsWith(prefix, StringComparison.Ordinal)) return null;

        var attribute = function[prefix.Length..];

        foreach (var (name, modality) in Attributes)
            if (string.Equals(name, attribute, StringComparison.Ordinal)) return modality;

        return null;
    }

    private static string Beside(string corpus, string folder, string file)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(corpus);

        var path = Path.Combine(corpus, folder, file);

        return File.Exists(path) ? path : throw new FileNotFoundException(
            $"the CLEVR corpus is not at {path} — see ClevrTests for how to fetch it");
    }

    /// <inheritdoc cref="ClevrSettings.Segmented"/>
    public bool Segmented => _settings.Segmented;

    /// <inheritdoc cref="ClevrSettings.Tagged"/>
    public bool Tagged => _settings.Tagged;

    /// <inheritdoc cref="ClevrSettings.Fleeting"/>
    public bool Fleeting => _settings.Fleeting;
}
