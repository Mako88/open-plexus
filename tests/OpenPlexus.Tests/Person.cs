using System.Text;

namespace OpenPlexus.Tests;

/// <summary>
/// Somebody at the other end of the conversation — <b>scripted, so a suite can hold one.</b>
/// </summary>
/// <param name="says">What it volunteers, one line a round, and nothing once they run out.</param>
/// <param name="answers">
/// What it says back when it is asked something, in turn. <b>Its own words rather than the
/// house's</b>, which is the whole of what the answerer being a person means: what settles the
/// round is what somebody said, and a world that knew better would be answering itself.
/// </param>
/// <remarks>
/// <para>
/// <b>A reader that owns its writer</b>, on <c>Tutor</c>'s shape and for its reason: seeing
/// the prompt is how it knows a reply is wanted rather than a statement. A prompt is a line
/// with no newline after it, which is the one thing separating asking from saying.
/// </para>
/// <para>
/// <b>It never runs out and never hangs up unless told to.</b> A reader returning nothing
/// ends the conversation, so a script that stopped early would end every house after the
/// first and read as a world that drops its people — which is a harness fault wearing a
/// finding's clothes.
/// </para>
/// </remarks>
internal sealed class Person(
    IReadOnlyList<string>? says = null, IReadOnlyList<string>? answers = null) : TextReader
{
    private readonly Queue<string> _says = new(says ?? []);
    private readonly Queue<string> _answers = new(answers ?? []);
    private readonly Watched _printed = new();

    /// <summary>Where the machine's words are shown, and where a question is noticed.</summary>
    public TextWriter Printed => _printed;

    /// <summary>Every question it was put, in order.</summary>
    public IReadOnlyList<string> Asked => _printed.Asked;

    /// <summary>Everything it was told, in order.</summary>
    public IReadOnlyList<string> Told => _printed.Told;

    /// <summary>What it has said back, in order.</summary>
    public List<string> Replied { get; } = [];

    /// <summary>Whether it leaves the next time it is read.</summary>
    public bool Leaving { get; set; }

    /// <inheritdoc/>
    public override string? ReadLine()
    {
        if (Leaving) return Worlds.Roaming.Over;

        var line = Round(_printed.Waiting() ? _answers : _says);

        Replied.Add(line);

        return line;
    }

    /// <summary>The next line of a script, which starts again where it ends.</summary>
    /// <remarks>
    /// <b>Round rather than out</b>, because a script that ran dry would leave somebody
    /// shrugging at everything from that point on — and a run long enough to matter would be
    /// mostly the shrug. Saying nothing is still sayable, as a blank line in the script.
    /// </remarks>
    private static string Round(Queue<string> script)
    {
        if (script.Count == 0) return string.Empty;

        var line = script.Dequeue();

        script.Enqueue(line);

        return line;
    }

    /// <summary>What the machine said, and whether the last of it wanted an answer.</summary>
    private sealed class Watched : TextWriter
    {
        private readonly StringBuilder _line = new();

        /// <summary>Every question, without its prompt.</summary>
        public List<string> Asked { get; } = [];

        /// <summary>Every claim, without its prompt.</summary>
        public List<string> Told { get; } = [];

        /// <inheritdoc/>
        public override Encoding Encoding => Encoding.UTF8;

        /// <inheritdoc/>
        public override void Write(char one)
        {
            if (one != '\n')
            {
                if (one != '\r') _line.Append(one);

                return;
            }

            var said = _line.ToString().Trim();

            _line.Clear();

            if (said.StartsWith(". ", StringComparison.Ordinal)) Told.Add(said[2..]);
        }

        /// <summary>Whether a question is on the table, which reading it takes off.</summary>
        public bool Waiting()
        {
            var said = _line.ToString().Trim();

            if (!said.StartsWith("? ", StringComparison.Ordinal)) return false;

            _line.Clear();

            Asked.Add(said[2..]);

            return true;
        }
    }
}
