using System.Collections.Immutable;
using System.Globalization;
using OpenPlexus.Codes;
using OpenPlexus.Graph;

namespace OpenPlexus.Worlds;

/// <summary>How much of CLUTRR to read.</summary>
public sealed record ClutrrSettings
{
    /// <summary>The fetched <c>clutrr_test.csv</c>.</summary>
    public required string Corpus { get; init; }

    /// <summary>How many stories to read, from the start of the file.</summary>
    public int Stories { get; init; } = 400;

    /// <summary>
    /// The longest chain to keep, or nought for all of them.
    /// </summary>
    /// <remarks>
    /// <b>FOR SEPARATING WHAT WAS LEARNED FROM WHAT WAS COMPOSED, which is the
    /// only question this world exists to ask.</b> A two-hop chain states a rule
    /// almost outright; a ten-hop chain can only be answered by applying rules
    /// learned elsewhere. Reading a band of lengths is how the two are told apart.
    /// </remarks>
    public int Longest { get; init; }

    /// <summary>
    /// Whether people are declared fleeting — one way, person to slot.
    /// </summary>
    /// <remarks>
    /// <b>ON IS THE CHEAP ARM AND IT CANNOT ANSWER, WHICH IS THIS WORLD'S WHOLE
    /// SHAPE.</b> A person exists in one story, so a fleeting index is normally the
    /// right call: the row a person writes into a lasting node grows forever and
    /// buys nothing. Here it costs everything — one way means <c>slot → person</c>
    /// is never written, so a walk reaching a slot can go no further and <b>the
    /// chain cannot be traversed at all</b>. It is <see cref="ClevrSettings.Fleeting"/>'s
    /// exception, louder: there the walk had to ARRIVE at an index, here it has to
    /// travel THROUGH one.
    /// </remarks>
    public bool Fleeting { get; init; }

    /// <summary>
    /// Whether the slots are said through the role channel rather than through
    /// grouping.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>OFF IS THE BASELINE AND IT IS A REAL ONE.</b> Grouping a person with the
    /// slot code they fill writes the same PAIR the role channel writes — the
    /// difference is the kind it lands under, <see cref="Graph.Kind.With"/> against
    /// <see cref="Graph.Kind.Fills"/>, and whether the two fillers of one statement
    /// can reach each other. So this is one mechanism measured ON from something
    /// that already works, which is the trap the plan names.
    /// </para>
    /// <para>
    /// <b>ON, THE SLOT CODES LEAVE THE MOMENT ENTIRELY.</b> The channel derives
    /// them from <see cref="Learning.Occasion.As"/>, so the moment is just the two
    /// people and the front end says which of them fills which slot — which is the
    /// whole point: the cell that results names no person at all.
    /// </para>
    /// </remarks>
    public bool Roled { get; init; }
}

/// <summary>
/// One CLUTRR story: <b>who is related to whom, and the pair being asked about.</b>
/// </summary>
/// <remarks>
/// <b>THE ENGLISH IS IGNORED AND THAT IS NOT A DODGE.</b> The corpus ships the
/// chain as columns — <c>story_edges</c> says which pairs are joined,
/// <c>edge_types</c> with what, <c>query_edge</c> which pair is asked about —
/// exactly as CLEVR ships its scene graph. This project has no language model and
/// claims none; the question under test is whether a relation composes, not
/// whether a sentence can be read.
/// </remarks>
public sealed record Story
{
    /// <summary>Which story, by its position in the file.</summary>
    public required int Index { get; init; }

    /// <summary>How many people are in it.</summary>
    public required int People { get; init; }

    /// <summary>Every stated relation, as <c>(from, to, relation)</c>.</summary>
    public required ImmutableArray<(int From, int To, Kind Relation)> Edges { get; init; }

    /// <summary>Which pair the question is about.</summary>
    public required (int From, int To) Query { get; init; }

    /// <summary>The relation the corpus says holds between them.</summary>
    public required Kind Answer { get; init; }

    /// <summary>The answer as the corpus spells it, for reporting.</summary>
    public required string Says { get; init; }

    /// <summary>
    /// Whether the answer's relation is <b>also stated somewhere in the chain</b>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>THE LINE BETWEEN RECALL AND COMPOSITION, AND WITHOUT IT A SCORE MEANS
    /// NOTHING.</b> When the answer is <i>grandson</i> and one of the premises is
    /// also <i>grandson</i>, the answer's slot code is already in a moment the
    /// graph just read — so a walk can arrive at it by association alone, having
    /// composed nothing. It is not a corrupt row; the corpus is entitled to
    /// generate it, and CLUTRR's own difficulty comes from the chain rather than
    /// from the vocabulary.
    /// </para>
    /// <para>
    /// <b>IT IS EVERY TWO-HOP STORY, WHICH IS WHY THIS EXISTS.</b> All thirty-eight
    /// of them in the first three hundred restate their answer, so a headline
    /// "two-hop chains beat chance" is a claim about recall wearing composition's
    /// clothes — and it was published as composition before anybody checked. Longer
    /// chains restate far less often, so the two must be scored apart or chain
    /// length silently measures contamination.
    /// </para>
    /// </remarks>
    public required bool Restated { get; init; }

    /// <summary>
    /// How many hops the chain is. <b>The one number a result must be broken down
    /// by</b> — see <see cref="ClutrrSettings.Longest"/>.
    /// </summary>
    public int Hops => Edges.Length;

    /// <summary>One person of this story, as a code.</summary>
    public Code Who(int slot) => Clutrr.Who(Index, slot);
}

/// <summary>
/// CLUTRR (Sinha et al., 2019) — <b>kinship composition, and the first real data
/// the role channel has ever had.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>THIS IS WHAT <see cref="Kind.Role"/> WAS BUILT FOR AND HAS NEVER BEEN
/// MEASURED ON.</b> A count between two people can only ever be about those two
/// people. A count between <c>grandson/1</c> and <c>brother/0</c> says
/// <i>whatever fills the second slot of one fills the first slot of the other</i>,
/// which names nobody — so it accumulates across every story and applies to
/// people never seen together. <c>BindingGapTests</c> is the scoreboard, and every
/// number on it was taken on cases built here.
/// </para>
/// <para>
/// <b>A PERSON IS A PER-STORY INDEX AND NEVER A NAME.</b> The corpus reuses
/// <i>Jason</i> across thousands of stories as different people, so a code minted
/// from the name would make one node out of everybody called Jason — which is the
/// hub that destroyed the binding world before indexes existed. It is CLEVR's
/// object index, on a different corpus.
/// </para>
/// <para>
/// <b>AND IT IS THE OPPOSITE WORLD FROM CLEVR, WHICH IS WHY IT IS HERE.</b> No
/// kept CLEVR question is spatial, so a relational mechanism there is row width
/// bought with noise — measured, and the position arm is a refuted row for it.
/// Every question here is relational and nothing else, so this shows what the role
/// channel does or nothing does.
/// </para>
/// </remarks>
public sealed class Clutrr
{
    /// <summary>
    /// The modality a person rides on.
    /// </summary>
    /// <remarks>
    /// <b>Its own, and never a relation's.</b> A relation lives on
    /// <see cref="Kind.Relations"/>, which no front end may use — so a walk can be
    /// narrowed to people or to relations without either reaching the other by
    /// accident.
    /// </remarks>
    public const byte Person = 70;

    private readonly ClutrrSettings _settings;

    /// <param name="settings">How much to read.</param>
    /// <exception cref="FileNotFoundException">The corpus is not there.</exception>
    public Clutrr(ClutrrSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(settings.Stories);

        _settings = settings;

        if (!File.Exists(settings.Corpus))
            throw new FileNotFoundException(
                $"the CLUTRR corpus is not at {settings.Corpus}. Fetch it with: "
                + "bash corpora/fetch.sh", settings.Corpus);

        Stories = Read(settings);

        Relations = [.. Stories
            .SelectMany(story => story.Edges.Select(edge => edge.Relation)
                .Append(story.Answer))
            .Distinct()
            .Order()];
    }

    /// <summary>Every story read, in file order.</summary>
    public IReadOnlyList<Story> Stories { get; }

    /// <summary>
    /// Every relation the corpus names, stated or asked.
    /// </summary>
    /// <remarks>
    /// <b>The candidate set, and it is what makes the question answerable at
    /// all.</b> A walk narrowed to these arrives at a relation or says nothing;
    /// without it the ranking would be over every code in the graph, most of them
    /// people.
    /// </remarks>
    public IReadOnlyList<Kind> Relations { get; }

    /// <summary>
    /// What guessing would score. <b>One over the relations, and not one over the
    /// answers seen</b> — the walk chooses among all of them.
    /// </summary>
    public double Chance => Relations.Count == 0 ? 0.0 : 1.0 / Relations.Count;

    /// <summary>
    /// One person of one story, as a code.
    /// </summary>
    /// <remarks>
    /// <b>Derived from the pair and never counted out</b>, so two machines reading
    /// the same file mint the same person with nothing to ask —
    /// <see cref="Kinds.Named"/> for the reason a hash of the string would not do.
    /// </remarks>
    public static Code Who(int story, int slot) =>
        Kinds.Named(Person, string.Create(
            CultureInfo.InvariantCulture, $"{story}:{slot}"));

    /// <summary>Reads the file into stories, keeping only what is usable.</summary>
    /// <remarks>
    /// <b>A ROW WHOSE QUERY IS NOT ABOUT TWO PEOPLE IT NAMES IS THROWN AWAY</b>,
    /// as is one whose chain is empty. Neither should occur; checking is what
    /// stops a parse failure reading as a world the graph cannot answer.
    /// </remarks>
    private static IReadOnlyList<Story> Read(ClutrrSettings settings)
    {
        var kept = new List<Story>();

        foreach (var row in Rows(settings.Corpus))
        {
            if (kept.Count >= settings.Stories) break;

            var edges = Chain(row);
            if (edges.Length == 0) continue;

            if (settings.Longest > 0 && edges.Length > settings.Longest) continue;

            var query = Pair(row.GetValueOrDefault("query_edge", ""));
            if (query is not var (from, to)) continue;

            var people = edges
                .SelectMany(edge => new[] { edge.From, edge.To })
                .Append(from)
                .Append(to)
                .Max() + 1;

            var says = row.GetValueOrDefault("target_text", "").Trim();
            if (says.Length == 0) continue;

            var answer = Kind.Of(says);

            kept.Add(new Story
            {
                Index = kept.Count,
                People = people,
                Edges = edges,
                Query = (from, to),
                Answer = answer,
                Says = says,
                Restated = edges.Any(edge => edge.Relation == answer),
            });
        }

        return kept;
    }

    /// <summary>The stated edges of one row, as pairs with their relation.</summary>
    /// <remarks>
    /// <b>The two columns are parallel and a row where they disagree is
    /// dropped</b>, because an edge with no relation and a relation with no edge
    /// are both a parse that went wrong rather than a story that is odd.
    /// </remarks>
    private static ImmutableArray<(int From, int To, Kind Relation)> Chain(
        IReadOnlyDictionary<string, string> row)
    {
        var pairs = row.GetValueOrDefault("story_edges", "");
        var kinds = Quoted(row.GetValueOrDefault("edge_types", ""));

        var edges = ImmutableArray.CreateBuilder<(int, int, Kind)>();
        var at = 0;

        foreach (var pair in Pairs(pairs))
        {
            if (at >= kinds.Count) return [];

            edges.Add((pair.From, pair.To, Kind.Of(kinds[at])));
            at++;
        }

        return at == kinds.Count ? edges.ToImmutable() : [];
    }

    /// <summary>Every <c>(a, b)</c> in a Python-style list of tuples.</summary>
    private static IEnumerable<(int From, int To)> Pairs(string text)
    {
        var at = 0;

        while (at < text.Length)
        {
            var open = text.IndexOf('(', at);
            if (open < 0) yield break;

            var close = text.IndexOf(')', open);
            if (close < 0) yield break;

            if (Pair(text[open..(close + 1)]) is var (from, to)) yield return (from, to);

            at = close + 1;
        }
    }

    /// <summary>One <c>(a, b)</c>, or null where it is not one.</summary>
    private static (int From, int To)? Pair(string text)
    {
        var open = text.IndexOf('(');
        var close = text.IndexOf(')');

        if (open < 0 || close < open) return null;

        var parts = text[(open + 1)..close].Split(',');
        if (parts.Length != 2) return null;

        return int.TryParse(parts[0].Trim(), CultureInfo.InvariantCulture, out var from)
            && int.TryParse(parts[1].Trim(), CultureInfo.InvariantCulture, out var to)
                ? (from, to)
                : null;
    }

    /// <summary>Every <c>'quoted'</c> item of a Python-style list of strings.</summary>
    private static IReadOnlyList<string> Quoted(string text)
    {
        var found = new List<string>();
        var at = 0;

        while (at < text.Length)
        {
            var open = text.IndexOf('\'', at);
            if (open < 0) break;

            var close = text.IndexOf('\'', open + 1);
            if (close < 0) break;

            found.Add(text[(open + 1)..close]);
            at = close + 1;
        }

        return found;
    }

    /// <summary>The file as rows keyed by the header.</summary>
    private static IEnumerable<IReadOnlyDictionary<string, string>> Rows(string path)
    {
        using var file = new StreamReader(path);

        var header = Fields(Line(file));
        if (header.Count == 0) yield break;

        while (!file.EndOfStream)
        {
            var fields = Fields(Line(file));
            if (fields.Count == 0) continue;

            var row = new Dictionary<string, string>(StringComparer.Ordinal);

            for (var at = 0; at < header.Count && at < fields.Count; at++)
                row[header[at]] = fields[at];

            yield return row;
        }
    }

    /// <summary>
    /// One logical CSV line, <b>which is not one physical line.</b>
    /// </summary>
    /// <remarks>
    /// <b>A STORY IS PROSE AND PROSE CONTAINS NEWLINES.</b> A quoted field may run
    /// over several lines, so reading line by line splits a row in half and every
    /// column after it lands under the wrong name — which reads as a corpus full
    /// of unanswerable questions rather than as a parse fault.
    /// </remarks>
    private static string Line(StreamReader file)
    {
        var text = new System.Text.StringBuilder();
        var quoted = false;

        while (file.ReadLine() is { } line)
        {
            text.Append(line);

            foreach (var letter in line)
                if (letter == '"') quoted = !quoted;

            if (!quoted) break;

            text.Append('\n');
        }

        return text.ToString();
    }

    /// <summary>One CSV line's fields, honouring quotes and doubled quotes.</summary>
    private static IReadOnlyList<string> Fields(string line)
    {
        if (line.Length == 0) return [];

        var fields = new List<string>();
        var field = new System.Text.StringBuilder();
        var quoted = false;

        for (var at = 0; at < line.Length; at++)
        {
            var letter = line[at];

            if (quoted)
            {
                // A DOUBLED QUOTE IS ONE QUOTE, which is the only escape RFC 4180
                // has and the one every hand-rolled reader forgets.
                if (letter == '"' && at + 1 < line.Length && line[at + 1] == '"')
                {
                    field.Append('"');
                    at++;
                }
                else if (letter == '"') quoted = false;
                else field.Append(letter);

                continue;
            }

            if (letter == '"') quoted = true;
            else if (letter == ',')
            {
                fields.Add(field.ToString());
                field.Clear();
            }
            else field.Append(letter);
        }

        fields.Add(field.ToString());

        return fields;
    }

    /// <inheritdoc cref="ClutrrSettings.Longest"/>
    public int Longest => _settings.Longest;

    /// <inheritdoc cref="ClutrrSettings.Fleeting"/>
    public bool Carried => _settings.Fleeting;

    /// <inheritdoc cref="ClutrrSettings.Roled"/>
    public bool Roled => _settings.Roled;

    public override string ToString() =>
        $"stories={Stories.Count} relations={Relations.Count}";
}
