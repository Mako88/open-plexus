using System.Collections.Immutable;
using System.Globalization;

namespace OpenPlexus.Worlds;

/// <summary>How much plain English to show, and from where.</summary>
public sealed record PrimerSettings
{
    /// <summary>
    /// The Tatoeba English export — <c>id[tab]eng[tab]sentence</c>, one to a line.
    /// </summary>
    public required string Corpus { get; init; }

    /// <summary>How many sentences to read from the top of it.</summary>
    public required int Sentences { get; init; }
}

/// <summary>
/// Plain English, shown to the graph as sentences and asked nothing.
/// </summary>
/// <remarks>
/// <para>
/// <b>JOHN'S ASK, 2026-08-04, AND IT TESTS A CEILING RATHER THAN CHASING A
/// SCORE.</b> Six bAbI tasks come back at exactly nought because their answers —
/// <i>yes</i>, <i>no</i>, <i>maybe</i>, the counting words — never appear as words
/// anywhere in the corpus, only in the answer column. An answer here is a code the
/// walk ARRIVED at, and a code enters the graph only by being observed, so there
/// is no node to arrive at. <b>The question this world exists to settle is whether
/// that nought is a VOCABULARY problem or a TASK problem.</b>
/// </para>
/// <para>
/// <b>It is shown in the SAME RUN with no reset, which C4 requires anyway.</b>
/// There is no train-then-test here; the English is simply what the system saw
/// before the task, the way anything else it has seen is.
/// </para>
/// <para>
/// <b>The codes are <see cref="Babi.Of"/>'s, which is the whole point.</b> A
/// primer with its own tokenizer would mint a different code for <i>yes</i> and
/// prove nothing at all — the word has to land on the same node the task will ask
/// about.
/// </para>
/// <para>
/// <b>Tatoeba rather than prose.</b> Short everyday sentences are where
/// <i>yes</i> and <i>no</i> actually occur; narrative uses them only in dialogue.
/// It is CC BY 2.0 FR and fetched rather than vendored, like the others.
/// </para>
/// </remarks>
public sealed class Primer
{
    /// <param name="settings">Where the English is and how much to read.</param>
    /// <exception cref="FileNotFoundException">The export is not there.</exception>
    public Primer(PrimerSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(settings.Sentences);

        if (!File.Exists(settings.Corpus))
            throw new FileNotFoundException(
                "the Tatoeba English export is not there. Fetch it with: "
                + "bash corpora/fetch.sh", settings.Corpus);

        Lines = Read(File.ReadLines(settings.Corpus), settings.Sentences);
    }

    /// <summary>The sentences, in the order the file lists them.</summary>
    public IReadOnlyList<Sentence> Lines { get; }

    /// <summary>
    /// Turns the export's lines into sentences that ask nothing.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>NO STORY CODE, and that is a property of the data rather than a
    /// choice.</b> Tatoeba sentences are unrelated to one another, so there is no
    /// telling to name — and <see cref="Sentence.Story"/> stays at nought with
    /// nothing in <see cref="Sentence.Words"/> naming it, which is what makes the
    /// fleeting code come back null.
    /// </para>
    /// <para>
    /// <b>A one-word line is dropped.</b> A sentence joins the codes in it to each
    /// other, so a line with a single word writes nothing and would only be a
    /// moment the count says happened.
    /// </para>
    /// </remarks>
    /// <param name="lines">The export, as read.</param>
    /// <param name="wanted">How many usable sentences to stop at.</param>
    public static IReadOnlyList<Sentence> Read(IEnumerable<string> lines, int wanted)
    {
        ArgumentNullException.ThrowIfNull(lines);

        var read = new List<Sentence>(wanted);

        foreach (var line in lines)
        {
            if (read.Count == wanted) break;
            if (string.IsNullOrWhiteSpace(line)) continue;

            // id [tab] lang [tab] text. Anything else is a line this does not
            // understand, and guessing at it would be a parser nobody asked for.
            var parts = line.Split('\t');
            if (parts.Length < 3) continue;

            if (!int.TryParse(
                    parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out _))
                continue;

            var words = Babi.Words(parts[2]).Select(Babi.Of).ToImmutableArray();
            if (words.Length < 2) continue;

            read.Add(new Sentence { Story = 0, Words = words });
        }

        return read;
    }
}
