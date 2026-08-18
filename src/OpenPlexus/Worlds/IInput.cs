using OpenPlexus.Codes;

namespace OpenPlexus.Worlds;

/// <summary>One stream of moments, pushing at whatever will take them.</summary>
/// <remarks>
/// <para>
/// <b>One sense, and a body is several of them.</b> A camera, a microphone and a
/// thermometer are three of these and one <see cref="Body"/>, each stamping its own
/// sequence — so what follows a frame is the next frame rather than whatever arrived next.
/// A single stream carrying every sense would make one sensor's rate a fact about another's.
/// </para>
/// <para>
/// <b>Codes rather than a world's own terms</b>, because senses have to compose in one
/// alphabet. That puts the translation on this side of the seam, which is where the plan
/// has always said it belongs: whether a reading is banded or winnowed is neither a fact
/// about the problem nor a setting on the brain.
/// </para>
/// <para>
/// <b>And nothing here is asked what would follow.</b> A source says what happened and
/// stops; whether anybody predicts, and whether they take the moment at all, is the
/// brain's business. That is the whole of what separates this from a world with a
/// <c>Next</c> on it.
/// </para>
/// </remarks>
public interface IInput
{
    /// <summary>Which source this is.</summary>
    /// <remarks>
    /// <b>Distinct per sense</b>, because a settlement is the next moment from the same
    /// place. Two senses sharing one number would have each answering the other's
    /// questions.
    /// </remarks>
    byte Source { get; }

    /// <summary>How many distinct things this source can say followed.</summary>
    /// <remarks>
    /// <b>What a blind guess is against</b>, so no run has to be told the chance bar
    /// separately and then be told it wrong.
    /// </remarks>
    int Outcomes { get; }

    /// <summary>The next moment, or nothing where this source has nothing to say now.</summary>
    /// <remarks>
    /// <b>Nothing is a quiet sense rather than the end of the stream.</b> A thermometer
    /// read once a minute has nothing to say on almost every pass, and that is the shape a
    /// body of senses running at different rates is in — so a null here must not be read as
    /// a source that has finished.
    /// </remarks>
    Pushed? Push();
}

/// <summary>One thing an examiner may put to a population, and its answer.</summary>
/// <remarks>
/// <b>Codes rather than an observation</b>, because the examination goes to the population
/// and the population has never seen anything else. The front end reads a withheld
/// observation exactly as it reads a pushed one, which is what makes the two scores
/// comparable at all.
/// </remarks>
public readonly record struct Question
{
    /// <summary>What would have been live.</summary>
    public required IReadOnlySet<Code> Codes { get; init; }

    /// <summary>What followed it.</summary>
    public required Code Followed { get; init; }
}

/// <summary>An input keeping part of its stream back, so it can be examined on it.</summary>
/// <remarks>
/// <para>
/// <b>C4 constrains the learner and not the experimenter</b>, and conflating the two is
/// why this was missing for so long. No episode boundary forbids the machine knowing about
/// one — it may not wait for one, switch behaviour at one, or be scored on a lifetime
/// average that assumes one. Nothing in it forbids the person running the experiment from
/// keeping observations back and asking, from outside, what the population would have said.
/// </para>
/// <para>
/// <b>And it is the only anti-memorisation instrument a perceptual world can have.</b> The
/// sharp one is soundness by enumeration, which needs a rule set to enumerate; a world made
/// of photographs never will have one, so the choice there is this or a score that cannot
/// be told from a lookup table.
/// </para>
/// </remarks>
public interface IExamines
{
    /// <summary>Moments this input will never push, with what followed each.</summary>
    /// <remarks>
    /// <b>Fixed before the run and never touched by <see cref="IInput.Push"/></b>, which is
    /// what makes the number mean anything. A held-out set the source could wander into
    /// would measure the same thing the trailing accuracy does, more slowly.
    /// </remarks>
    IReadOnlyList<Question> Exam { get; }
}
