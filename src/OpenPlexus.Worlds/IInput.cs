using OpenPlexus.Codes;

namespace OpenPlexus.Worlds;

/// <summary>One world pushing moments, at whatever will take them.</summary>
/// <remarks>
/// <para>
/// <b>One world and one stream</b>, and a moment carries every modality at once. A camera, a
/// microphone and a thermometer on one world are three front ends and ONE moment — which is
/// what lets a scope span them, and it is the link this design exists to make.
/// <see cref="Codes.Compound{TFrame}"/> is where they merge.
/// </para>
/// <para>
/// <b>Interleaving them was the error this comment replaces.</b> A composition that asked
/// each modality in turn gave each its own moment, so no scope could ever hold two of them —
/// a limit read as a fact about the architecture when it was a fact about the composition.
/// Modalities were never meant to be apart.
/// </para>
/// <para>
/// <b>Codes rather than a world's own terms</b>, because modalities have to compose in one
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
    /// <b>Distinct per world</b>, because a settlement is the next moment from the same
    /// place. Two worlds on one number would have each answering the other's questions, and
    /// there is normally one.
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
    /// <b>Nothing is a quiet world rather than the end of the stream.</b> A world waiting on
    /// something outside itself has nothing to say for a while and is not finished, so a null
    /// here must not be read as one. A modality that is slower than the others is NOT this
    /// case: it contributes no codes to the moment and the moment still happens.
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

    /// <summary>Which things those codes make up, where the front end can say.</summary>
    /// <remarks>
    /// <b>Or the examination is taken under a different learner.</b> A grouping decides what
    /// fires, so a held-out question asked without one is put to a population whose scopes
    /// were built to be read with it — and the arm reads at its control's score for a reason
    /// that has nothing to do with generalising. That is what the first reading of it said,
    /// and this field is what the second one has.
    /// </remarks>
    public IReadOnlyList<Grouped>? Grouping { get; init; }
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
