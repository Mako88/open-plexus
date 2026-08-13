using Xunit.Abstractions;

namespace OpenPlexus.Tests;

/// <summary>
/// The rules that could not be made into checks, printed beside the ones that could —
/// <b>John's, and it is the same move the guards are, for the half that will not mechanise.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>THE FAILURE THIS EXISTS FOR IS NOT NOT-KNOWING, IT IS NOT-CONSULTING.</b> Every
/// foundational mistake on this branch has the same shape: a decision that is locally
/// sensible and globally wrong, where the governing rule was written down somewhere and
/// nothing surfaced it at the moment of choosing. Arms that reached into worlds; a season
/// spent on the front end with no real learner; a mechanism that WON and shipped switched
/// off. The knowledge was present each time and was not cued.
/// </para>
/// <para>
/// <b>SO A GUARD'S REAL JOB IS TO MAKE A GLOBAL CONSTRAINT LOCAL</b> — present at the
/// decision rather than three files away. Where a rule can be evaluated, that is a check and
/// it belongs beside the others. Where it cannot, the next best thing is to put it in front
/// of whoever is already looking, which is what this does: the guards are run as their own
/// command every commit, so these print into that same output.
/// </para>
/// <para>
/// <b>AND IT ASSERTS RATHER THAN ONLY PRINTING, WHICH <c>CheckingTests</c> REQUIRES AND
/// WHICH IS ALSO THE RIGHT SHAPE.</b> A list nobody maintains is worse than none: it goes
/// stale, gets skimmed, and becomes furniture. The bars below keep it a list somebody has
/// to keep — short enough to read, every entry saying what to DO rather than what to
/// believe, and none of them a rule that could have been a check.
/// </para>
/// <para>
/// <b>AN ENTRY LEAVES BY BECOMING A GUARD.</b> That is the only exit, and it is the same
/// rule <c>TRAPS</c> has in the plan: a class earning a check moves out of here into the
/// check.
/// </para>
/// </remarks>
public sealed class RemindingTests(ITestOutputHelper output)
{
    /// <summary>How many reminders a session can be expected to actually read.</summary>
    /// <remarks>
    /// <b>A RATCHET, AND IT ONLY GOES DOWN.</b> Twelve is about a screen. Past that this
    /// stops being a reminder and becomes a second doc, which is the thing it was written
    /// instead of. Raising it wants John and a reason in the commit message; the ordinary
    /// way to make room is to turn one into a guard.
    /// </remarks>
    private const int Room = 12;

    /// <summary>What to do, and why it is not a check.</summary>
    private static readonly (string Doing, string Why)[] Reminders =
    [
        ("A FRONT END ONLY MAKES THINGS RECOGNISABLE. Same input, same code, and nothing "
         + "else. It may report what it saw and where — order, grouping, position — and may "
         + "never assert a relation between two things, nor fuse two facts into one code.",
         "Both failures look like progress, because both raise a score by moving work "
         + "across the seam. A fused pair asserts; a fused position destroys an identity."),

        ("TAKE THE CEILING FIRST. Before building an arm, compute what the world permits "
         + "with no learning at all.",
         "Milliseconds against a runner's hour, and a grid cannot tell a rule that failed "
         + "from a front end that threw the answer away one call earlier."),

        ("A WORLD MODELS REALITY; THE HARNESS CARRIES THE INSTRUMENTS. A withheld set, an "
         + "answer key and a soundness oracle are not aspects of the world. The line is that "
         + "nothing the LEARNER can see is an instrument.",
         "C4 constrains the learner and not the experimenter, and conflating the two costs "
         + "the only anti-memorisation instruments a perceptual world can have."),

        ("A WORLD'S DIALS ARE PINNED FACTS ABOUT THE SCENARIO, never levers swept to find a "
         + "cell where a brain looks good.",
         "A generated world must have parameters, so this cannot be checked by counting "
         + "them. What separates the two is intent at the moment of sweeping."),

        ("AN ISOLATING WORLD IS DELETED WHEN ITS QUESTION CLOSES. Build one whenever it makes "
         + "something attributable — that is usually the cheapest step — but it lives only "
         + "while something is being asked of it.",
         "Worlds accumulate exactly as dials do, and for the same reason: they are what the "
         + "old numbers were taken on."),

        ("AN ASPECT OF THE REAL WORLD THE SPINE DOES NOT MODEL GOES IN BEFORE the ability "
         + "that handles it is built.",
         "Which aspects are missing is a judgement about reality, and no check can hold a "
         + "list of what reality contains."),

        ("ADJUSTING A LOSING ARM IS ALLOWED. A mechanism that lost as BUILT may be worth one "
         + "more shape before it goes.",
         "This repo has read `the loser is deleted` as `delete immediately` more than once. "
         + "What is forbidden is leaving it switched off while nobody decides."),

        ("PRESERVING RECORDED NUMBERS IS NEVER A REASON to keep something, or to not change "
         + "it. The baseline is in the commit and in the test.",
         "Partly checkable and mostly not: it is the reason offered for a decision rather "
         + "than the decision, and it is the root of every count here that grew."),

        ("ASK WHICH HALF OF GENERATE-AND-TEST A PROPOSAL TOUCHES. Where no right rule was "
         + "present, a rule about who WINS cannot reach it.",
         "Four sessions of vote arms went into the half that was not the problem."),

        ("CONTROL A PREDICTION BY CHANGING THE ACTION NAMED IN IT, and floor that against the "
         + "SAME question asked twice. An accuracy cannot tell a world model from a "
         + "next-frame predictor, and a count of the times two arms differed cannot either.",
         "A rule about what an instrument must carry, and the instrument does not exist here "
         + "yet — fork 103 is where it lands. Taken off `csharp`, where three attempts went "
         + "by before the floor was found, and a positive count and a zero one proved "
         + "equally little without it."),

        ("EMIT ON ONSET AND OFFSET, NEVER PER TICK. A front end over a signal that persists "
         + "says what started and what stopped; persistence is the absence of a message, and "
         + "duration comes off it free.",
         "Nothing here samples a persisting stream, so there is nothing yet to check. "
         + "Sampling one manufactures the always-present hub this plan records twice as a "
         + "symptom — fork 51, and the refuted genesis-on-a-never-varied code — and never as "
         + "a cure. It bites the day a camera arrives."),

        ("A REASON IS EASY TO WRITE. Where a rule could be a check, it should be one, and an "
         + "entry here is a debt rather than a home.",
         "This list is the thing most likely to be gamed by its own author, so it says so."),
    ];

    /// <summary>
    /// Prints the reminders, and fails if the list has stopped being one.
    /// </summary>
    /// <remarks>
    /// <b>THE BARS ARE WHAT KEEP IT ALIVE.</b> A list that may grow without limit is a doc;
    /// an entry that says what to believe rather than what to do is a slogan; an entry with
    /// no reason it is not a check is one somebody has not tried to write. Each of those is
    /// a way this decays into furniture, and each fails here.
    /// </remarks>
    [Fact]
    public void The_rules_that_could_not_be_checks_are_still_a_list_worth_reading()
    {
        output.WriteLine("== RULES WITH NO CHECK BEHIND THEM. An entry leaves by becoming one. ==");

        foreach (var (doing, why) in Reminders)
        {
            output.WriteLine("");
            output.WriteLine($"  {doing}");
            output.WriteLine($"    why not a check: {why}");
        }

        Assert.True(Reminders.Length <= Room,
            $"{Reminders.Length} reminders against room for {Room}. Past that this stops "
            + "being something a session reads and becomes the second doc it was written "
            + "instead of. The ordinary way to make room is to turn one into a guard");

        foreach (var (doing, why) in Reminders)
        {
            Assert.False(string.IsNullOrWhiteSpace(why),
                $"a reminder with no reason it is not a check: {doing}. If nobody has tried "
                + "to write one, it may well be checkable -- which is the better outcome");

            // WHAT TO DO RATHER THAN WHAT TO BELIEVE, which is the difference between a
            // reminder and a slogan. An imperative opening is the cheapest thing that tells
            // the two apart, and a slogan cannot pass it without being rewritten into one.
            Assert.True(char.IsUpper(doing[0]) && doing.Split(' ')[0].All(char.IsUpper),
                $"a reminder that does not open with an instruction: {doing}");
        }
    }
}
