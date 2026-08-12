using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using OpenPlexus.Codes;
using Xunit.Abstractions;

namespace OpenPlexus.Tests;

/// <summary>
/// Every public member is either CALLED, or named here with the reason it is
/// not — <b>a budget, like the dials and the doc and the clones and the row.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>JOHN'S CALL, 2026-08-04: DEAD CODE IS THE WORST.</b> Nothing in this build
/// fails when a public member loses its last caller, so a mechanism can be
/// written, documented, cited in the plan, and never once run — and it reads
/// exactly like a mechanism that works. <b>Two were found by hand the day this
/// was written</b>: `Question.Conjoining`, the conjunction question, which had
/// never had a caller since the day it was written; and `Drives.Improving`, an
/// internal signal computed every step and read by nothing, which had been
/// described to John as one of three signals the system has.
/// </para>
/// <para>
/// <b>THE SHAPE IS <c>DialTests</c>'s, ON PURPOSE.</b> Either something uses it,
/// or somebody has written down why not — and "nobody has got to it yet" is a
/// perfectly good reason as long as it is written where it can be counted. What
/// is not allowed is silence.
/// </para>
/// <para>
/// <b>USE IS TEXTUAL AND COMMENTS DO NOT COUNT.</b> A <c>cref</c> naming a type
/// keeps its documentation honest and is exactly how a dead mechanism stays
/// looking alive, so the scan strips comments before it looks. <b>That is the
/// whole trick</b>: `Code.Prefix` is cited by fork 3 in prose and called by
/// nothing.
/// </para>
/// </remarks>
public sealed class DeadCodeTests(ITestOutputHelper output)
{
    /// <summary>
    /// Public members with no caller, each with the reason it survives.
    /// </summary>
    /// <remarks>
    /// <b>A REASON, NOT AN EXCUSE.</b> Several of these say outright that the
    /// thing should be wired or deleted and nobody has done it — which is the
    /// point of writing it down rather than the failure of it.
    /// </remarks>
    /// <remarks>
    /// <b>EMPTY, AND THE THREE WAYS IT GOT THERE ARE WORTH KEEPING.</b> Sixteen
    /// entries were resolved rather than re-explained, and none of the three moves
    /// was "write a better reason":
    /// <list type="bullet">
    /// <item><b>Made non-public.</b> Five were used inside their own file and
    /// nowhere else — a world's own modality byte, its own cue, its own code
    /// constructors. A member with no caller outside its type was never public
    /// code; it was a private detail with the wrong keyword, and the budget is what
    /// noticed.</item>
    /// <item><b>Deleted.</b> Three had no caller anywhere at all. <c>Code.Prefix</c>
    /// was written for open fork 3, cited in prose, and never once run — and step
    /// 8's grains reached the same idea by another road, so the fork can write its
    /// three lines again if it ever wants them.</item>
    /// <item><b>Asserted.</b> Eight were computed every run and printed in a
    /// <c>ToString</c>, which is precisely how a quantity goes on looking alive:
    /// shown to whoever reads the output and free to be wrong forever. Each now has
    /// a test comparing it to something.</item>
    /// </list>
    /// <b>THE LIST BEING EMPTY IS NOT THE POINT AND SHOULD NOT BECOME ONE.</b> A
    /// written reason is a perfectly good outcome — "nobody has got to it yet"
    /// included. What is not allowed is silence, and what this file buys is that
    /// the next unwired mechanism arrives ALONE rather than among sixteen.
    /// </remarks>
    private static readonly Dictionary<string, string> Unused =
        new(StringComparer.Ordinal);

    /// <summary>What a record or a runtime generates and nobody writes.</summary>
    private static readonly HashSet<string> Generated = new(StringComparer.Ordinal)
    {
        "Equals", "GetHashCode", "ToString", "Deconstruct", "PrintMembers",
        "CompareTo", "Dispose", "DisposeAsync", "GetEnumerator", "Clone",
    };

    [Fact]
    public void Every_public_member_is_called_or_has_a_written_reason_it_is_not()
    {
        var source = Sources();

        var orphans = new List<string>();

        foreach (var type in typeof(Code).Assembly.GetTypes().Where(one => one.IsPublic))
        {
            foreach (var member in Members(type))
            {
                var name = $"{type.Name}.{member}";

                // ITS OWN DECLARATION IS NOT A USE. Every other file counts, which
                // includes the tests -- a member exercised only by a test is doing
                // something, even if only holding a claim in place.
                var used = source
                    .Where(file => !file.Key.EndsWith($"{type.Name}.cs", StringComparison.Ordinal))
                    .Any(file => Regex.IsMatch(file.Value, Calls(member)));

                if (used || Unused.ContainsKey(name)) continue;

                orphans.Add(name);
            }
        }

        foreach (var one in orphans.Order(StringComparer.Ordinal)) output.WriteLine(one);

        Assert.True(orphans.Count == 0,
            $"{orphans.Count} public member(s) nothing calls and nobody has "
            + "explained. Wire it, delete it, or write down why it stays:\n  "
            + string.Join("\n  ", orphans.Order(StringComparer.Ordinal).Take(20)));
    }

    [Fact]
    public void And_the_list_does_not_rot_into_a_record_of_what_used_to_be_dead()
    {
        // THE OTHER DIRECTION, and without it the list becomes a graveyard of
        // members that have since been wired up — which is the exact failure the
        // doc's ticked boxes and the fork index are both checked for.
        var source = Sources();

        var revived = new List<string>();

        foreach (var (name, _) in Unused)
        {
            var member = name.Split('.')[1];
            var owner = name.Split('.')[0];

            if (source
                .Where(file => !file.Key.EndsWith($"{owner}.cs", StringComparison.Ordinal))
                .Any(file => Regex.IsMatch(file.Value, Calls(member))))
                revived.Add(name);
        }

        Assert.True(revived.Count == 0,
            $"named here as unused and now called: {string.Join(", ", revived)}. "
            + "Take it off the list.");
    }

    [Fact]
    public void The_budget_is_visible_and_does_not_grow()
    {
        // THE POINT OF THE FILE. The number is what it is today; having one is what
        // stops the next unwired mechanism arriving unnoticed beside these. IT
        // SHOULD ONLY EVER FALL — every entry is something to wire or delete.
        //
        // AT NOUGHT, AND THAT MAKES THIS THE STRICTEST THE CHECK CAN BE: the next
        // public member to lose its last caller fails the test above outright, with
        // nowhere to sit quietly. Raising this is a deliberate edit and should read
        // as one.
        Assert.Empty(Unused);
    }

    [Fact]
    public void And_a_public_type_the_library_itself_never_names_is_not_wired()
    {
        // THE HOLE THE MEMBER SCAN CANNOT SEE, and `Winnow` fell straight through
        // it: built, documented, measured, and reaching NO WORLD -- while every
        // member read as used because `WinnowTests` names them. A member scan asks
        // whether anything calls a method; it never asks whether the library
        // itself has heard of the TYPE.
        //
        // AND TESTS DO NOT COUNT HERE, WHICH THEY DELIBERATELY DO ABOVE. That
        // asymmetry is the point rather than an inconsistency: a world's
        // `RunAsync` exists for the harness to call, so a test IS its caller and
        // counting it is right. Nothing exists for a test to CONSTRUCT -- a type
        // the library never names is a mechanism wired to nothing, whatever its
        // own tests do with it.
        var source = Sources()
            .Where(file => file.Key.Contains(
                $"{Path.DirectorySeparatorChar}src{Path.DirectorySeparatorChar}",
                StringComparison.Ordinal))
            .ToList();

        var orphans = new List<string>();

        foreach (var type in typeof(Code).Assembly.GetTypes().Where(one => one.IsPublic))
        {
            var name = type.Name.Split('`')[0];

            var named = source
                .Where(file => !file.Key.EndsWith($"{name}.cs", StringComparison.Ordinal))
                .Any(file => Regex.IsMatch(file.Value, @"\b" + Regex.Escape(name) + @"\b"));

            if (!named && !Unwired.ContainsKey(name)) orphans.Add(name);
        }

        Assert.True(orphans.Count == 0,
            $"{orphans.Count} public type(s) nothing in `src` names. Wire it, "
            + "delete it, or write down why it stays: "
            + string.Join(", ", orphans.Order(StringComparer.Ordinal)));
    }

    /// <summary>Public types the library never names, each with its reason.</summary>
    /// <remarks>
    /// <b>TEN ENTRY POINTS AND ONE ORPHAN, WHICH IS THE WHOLE VALUE OF THE
    /// CHECK.</b> A world's run exists for a harness to call, so a test IS its
    /// caller and the library never naming it is correct. `Winnow` is not that: it
    /// is a mechanism, and a mechanism the library has never heard of is wired to
    /// nothing however thoroughly its own tests exercise it.
    /// </remarks>
    private static readonly Dictionary<string, string> Unwired = new(StringComparer.Ordinal)
    {
        ["BabiRun"] = Harness,
        ["BindingRun"] = Harness,
        ["ClevrRun"] = Harness,
        ["ComposedRun"] = Harness,
        ["HomeostatRun"] = Harness,
        ["MotifRun"] = Harness,
        ["SensesRun"] = Harness,
        ["SnakeRun"] = Harness,
        ["TendingRun"] = Harness,
        ["ClutrrRun"] = Harness,
        ["LatentRun"] = Harness,
        ["MultiplexerRun"] = Harness,
        ["GradedRun"] = Harness,
        ["CifarRun"] = Harness,
        ["ArrangedRun"] = Harness,
        ["MonkRun"] = Harness,

        ["Posted"] = "A TRANSPORT IS CHOSEN BY WHOEVER COMPOSES THE SYSTEM, so the "
            + "library naming one would be the library deciding how it is deployed -- "
            + "the same fault as a world naming a brain type, one layer out. `IBus` is "
            + "what `src` knows about; which bus is a container's or a harness's "
            + "decision, and `HybridBus` sits the same way.",

        ["Felt"] = "used by `Sensing`, which shares its file — the own-file rule "
            + "cannot see a caller sitting beside it.",

        // AND BOTH ENTRIES CHANGED THEIR REASON RATHER THAN THEIR STATUS, WHICH IS THE
        // MORE INTERESTING OUTCOME. `Asker` came off this list because `Fleet` names it,
        // and the two that remain are not unmounted any more -- they are the deployment,
        // and a library that named its own deployment would be deciding it.
        //
        // `Holder` READ AS WIRED FOR EXACTLY AS LONG AS A TUPLE FIELD WAS SPELT LIKE
        // IT. `HybridBus.AskAsync` named its second element `Holder`, and a scan for
        // whether the library NAMES a type answers yes to that for free. Renaming the
        // field is what put it on this list, and it is the sharper half of the finding: a
        // budget can be satisfied by a coincidence.
        ["Holder"] = Composed,
        ["Fleet"] = Composed,

        ["Roaming"] = "A WORLD, ON THE SAME FOOTING AS `Returning`: `Trial` drives it "
            + "through `IWorld`, so there is no run for `src` to name and naming the world "
            + "itself would be the library knowing which problem it is pointed at. "
            + "`RoamingTests` is its caller.",

        ["Returning"] = "A WORLD, AND THE LIBRARY NAMES `IWorld` RATHER THAN ANY OF "
            + "THEM. It has no run of its own because `Trial` drives it directly, so "
            + "there is not even a harness entry point for `src` to mention -- and a "
            + "world the library named would be the library knowing what problem it is "
            + "being pointed at, which is the fault `SeparationTests` guards from the "
            + "other side. `ReturningTests` is its caller.",

        ["Alternating"] = "A DERIVATION MEASURED BEFORE IT IS ADMITTED, on the same "
            + "footing as `Unifying`. It finds the groups of codes that are alternatives, "
            + "which is what a category would be minted over -- and something in `src` "
            + "calling it would mean the operator had been admitted, when what the "
            + "measurement is FOR is deciding whether to admit it. `ReturningTests` reads "
            + "it: the appearances come back exactly and the twins do not, so a category "
            + "reaches a kind and never an individual. A category MAY enter a scope now -- "
            + "`Sorting` carries the vocabulary and `Population.Recast` reads it -- and this "
            + "entry stays because the DERIVATION is still the experimenter's. The day a "
            + "front end runs it on its own stream is the day it comes off.",

        ["Unifying"] = "A PRICE AND NOT YET A MECHANISM, which is fork 33's own "
            + "instruction: probe unification's cost BEFORE the ladder's escalation "
            + "policy is designed, not after. Something in `src` calling it would mean "
            + "rung four had been admitted -- and the admission is the decision the "
            + "price exists to inform, so wiring it before that decision is taken would "
            + "be answering the question by building the answer. `UnifyingCostTests` is "
            + "what it is for; the day repair may propose a scope naming no argument is "
            + "the day this entry comes off.",

        ["Probe"] = "A CONTROL ARM, SO THE LIBRARY NAMING IT WOULD BE THE FAULT. It "
            + "is the dullest learner there is, run over the same features the "
            + "commitment population reads, and what it measures is how much of a "
            + "problem is in the FRONT END rather than in the learner. Something "
            + "inside `src` calling it would mean the architecture had started "
            + "consulting its own yardstick, which is the one thing it must never do.",

        // `Winnow` WAS THE ENTRY THIS CHECK WAS WRITTEN FOR, and it is gone because
        // it is now mounted rather than because the reason was reworded. `GradedRun`
        // consumes it as one of two front-end arms, so the library has finally heard
        // of the type its own plan called its defence.
    };

    /// <summary>Why a world's run is not named by the library.</summary>
    private const string Harness =
        "a world's run is the HARNESS's entry point, so a test is its rightful "
        + "caller and the library naming it would be the surprise.";

    /// <summary>
    /// Why a role in a deployment is not named by the library it is deployed from.
    /// </summary>
    /// <remarks>
    /// <b>THE SAME REASON `Posted` CARRIES, ONE LAYER UP, AND IT REPLACED A BETTER-KNOWN
    /// ONE.</b> These two used to say <i>fork 52's transport is built and the learner is
    /// not on it</i>, which was the honest state and is exactly what this list is for.
    /// The learner is on it now, and what is left is not a gap: a library that constructed
    /// its own holders would be a library that had decided how many machines there are.
    /// </remarks>
    private const string Composed =
        "A DEPLOYMENT IS CHOSEN BY WHOEVER COMPOSES THE SYSTEM, so the library naming one "
        + "would be the library deciding how it is run -- the same fault as a world naming "
        + "a brain type, one layer out. `ICouncil` is what `src` knows about, and whether "
        + "the council behind it is one population or twelve machines on twelve ports is a "
        + "container's decision. `Posted` sits the same way, and `FleetTests` is what runs "
        + "a whole learner over these.";

    /// <summary>
    /// What using a member actually looks like, as against merely spelling it.
    /// </summary>
    /// <remarks>
    /// <b>A BARE WORD IS NOT A USE, AND THE COMPANION CHECK CAUGHT ME ASSUMING IT
    /// WAS.</b> <c>Better</c>, <c>Same</c>, <c>Symbol</c> and <c>Thing</c> are
    /// ordinary words appearing as unrelated identifiers all over the tree, so
    /// matching the name alone reports a dead member as live — <b>a check that
    /// produces false PASSES</b>, which is the one failure this file exists to
    /// prevent. A use is a member access, a named argument, or an initialiser.
    /// </remarks>
    private static string Calls(string member) =>
        @"\." + Regex.Escape(member) + @"\b|\b" + Regex.Escape(member) + @"\s*[:=][^=]";

    /// <summary>Every source file, with comments stripped.</summary>
    /// <remarks>
    /// <b>A <c>cref</c> IS NOT A CALL, and that is the whole trick.</b> Doc
    /// comments are how a dead mechanism goes on looking alive — the compiler
    /// checks that they RESOLVE, which is a guarantee about spelling and not about
    /// use.
    /// </remarks>
    private static Dictionary<string, string> Sources()
    {
        var root = Tree.Repo();

        return Directory
            .EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}",
                StringComparison.Ordinal))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}",
                StringComparison.Ordinal))

            // AND NOT THIS FILE, which names every member on the list and would
            // otherwise report all of them as used — the list keeping itself
            // alive, which is the funniest way this check could be worth nothing.
            .Where(path => !path.EndsWith("DeadCodeTests.cs", StringComparison.Ordinal))
            .ToDictionary(
                path => path,
                path => Regex.Replace(File.ReadAllText(path), @"^\s*///.*$", "", RegexOptions.Multiline));
    }

    /// <summary>The public members of one type that a caller could name.</summary>
    private static IEnumerable<string> Members(Type type)
    {
        const BindingFlags Declared =
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static
            | BindingFlags.DeclaredOnly;

        foreach (var one in type.GetMembers(Declared))
        {
            if (one is MethodBase { IsSpecialName: true }) continue;
            if (one.GetCustomAttribute<CompilerGeneratedAttribute>() is not null) continue;
            if (Generated.Contains(one.Name)) continue;
            if (one.Name.StartsWith('<') || one.Name.StartsWith("op_", StringComparison.Ordinal))
                continue;

            // CONSTRUCTORS ARE NAMED BY THEIR TYPE, and the type's own use is a
            // different question from a member's.
            if (one is ConstructorInfo) continue;

            // AN ENUM MEMBER IS A VALUE RATHER THAN A CALL, and a refuted arm being
            // deleted is what `Attending` and `Gardening` are already policed by.
            if (type.IsEnum) continue;

            yield return one.Name;
        }
    }
}
