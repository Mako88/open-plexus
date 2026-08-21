using System.Reflection;
using System.Reflection.Emit;
using System.Text.RegularExpressions;
using OpenPlexus.Commitments;
using Xunit.Abstractions;

namespace OpenPlexus.Tests;

/// <summary>
/// Every mechanism under <c>src/OpenPlexus/Brain/</c> is reached by something that drives a
/// brain, and a test is not one of those things.
/// </summary>
/// <remarks>
/// <para>
/// <b>John's, 2026-08-21, and the folder is the point.</b> <see cref="DeadCodeTests"/> asks
/// whether a public member is called anywhere and counts a test as a caller, so a mechanism
/// the brain never executes passes it while reading as live. Deciding case by case which
/// callers count is a judgement inside a check; putting the brain in one directory moves that
/// judgement into the tree, where it can be seen and argued with.
/// </para>
/// <para>
/// <b>The line is what the guard is.</b> Inside <c>Brain/</c> is the thing being tuned: the
/// front ends, the learner, and the choosers that read a population. Outside it is what drives
/// one — a world, a runner, the fleet and its transport, and the terminal harness in
/// <c>OpenPlexus.Talk</c>. A red here says a piece of the brain is executed by nothing, and it
/// closes by being wired into a run or deleted with a revival row.
/// </para>
/// <para>
/// <b>And moving a file out of <c>Brain/</c> is not a third door.</b> Where the line falls is
/// asserted below by name, so a mechanism cannot reach green by being relabelled as something
/// that drives the brain.
/// </para>
/// <para>
/// <b>Reachability is read off the compiled code rather than the text.</b> The textual version
/// of this guard credited <c>Sorted</c> to a modality constant of the same name in
/// <see cref="OpenPlexus.Codes.Joined"/>, and through that one collision three further types
/// came out live — this repo's two-ideas-one-name trap arriving inside the instrument meant to
/// catch dead mechanisms. What is walked instead is every signature and every method body's
/// metadata tokens, so a name means a type or it means nothing.
/// </para>
/// </remarks>
public sealed class DrivenTests(ITestOutputHelper output)
{
    /// <summary>The directory the brain lives in.</summary>
    private static string Inside =>
        Path.Combine(Tree.Repo(), "src", "OpenPlexus", "Brain");

    /// <summary>Where a type declaration starts, in source.</summary>
    /// <remarks>
    /// <b>Names rather than namespaces</b>, because the brain has no namespace of its own. A
    /// namespace <c>OpenPlexus.Brain</c> would sit beside a class already called
    /// <see cref="OpenPlexus.Machines.Brain"/>, and one spelling resolving two ways in
    /// different files is worse than a folder that does not match. Nothing is ambiguous while
    /// no name is declared on both sides of the line, which is asserted below.
    /// </remarks>
    private static readonly Regex Declares = new(
        @"^\s*(?:public|internal)\s+"
        + @"(?:(?:sealed|abstract|static|partial|readonly|record|file)\s+)*"
        + @"(?:class|interface|enum|struct|record)\s+(?:class\s+|struct\s+)?"
        + @"([A-Za-z_][A-Za-z0-9_]*)",
        RegexOptions.Multiline);

    /// <summary>Type names declared under a directory of <c>src</c>, or outside it.</summary>
    /// <param name="under">The directory.</param>
    /// <param name="within">Whether to take the files under it or the ones outside it.</param>
    private static IReadOnlySet<string> Names(string under, bool within) =>
        Tree.Sources("src")
            .Where(path => path.StartsWith(under, StringComparison.Ordinal) == within)
            .SelectMany(path => Declares.Matches(Bare(File.ReadAllText(path))))
            .Select(hit => hit.Groups[1].Value)
            .ToHashSet(StringComparer.Ordinal);

    /// <summary>Source with its comments taken out.</summary>
    /// <remarks>
    /// <b>A cref is not a use.</b> This is <see cref="DeadCodeTests"/>'s trick and it is
    /// needed for the same reason: the documentation beside a dead mechanism is exactly what
    /// keeps it looking alive.
    /// </remarks>
    private static string Bare(string source) =>
        Regex.Replace(
            Regex.Replace(source, @"/\*.*?\*/", " ", RegexOptions.Singleline), @"//.*", " ");

    /// <summary>The assemblies a deployment is composed out of.</summary>
    /// <remarks>
    /// <b>The harness is a driver.</b> <c>OpenPlexus.Talk</c> composes the conversation
    /// deployment, and the chooser it wires is reached from there and from nowhere else.
    /// </remarks>
    private static IReadOnlyList<Assembly> Composed() =>
        [typeof(Population).Assembly, Assembly.Load("OpenPlexus.Talk")];

    /// <summary>The outermost type a type is declared in, which is the one with a file.</summary>
    private static Type Outermost(Type type)
    {
        while (type.DeclaringType is { } outer) type = outer;

        return type;
    }

    /// <summary>A type's outermost name, with its generic arity taken off.</summary>
    private static string Spelt(Type type) => Outermost(type).Name.Split('`')[0];

    /// <summary>Every type a type is written in terms of, one level deep.</summary>
    /// <remarks>
    /// <para>
    /// <b>Signatures and bodies both</b>, because either alone misses a class of use. An enum
    /// handed to a constructor never appears in the caller's instructions at all — its members
    /// are constants the compiler folds — so it is reachable only through the parameter type.
    /// A static call reaches nothing but a body.
    /// </para>
    /// <para>
    /// <b>And compiler-written types are walked as themselves.</b> A lambda becomes a nested
    /// class, so calls made inside it belong to a type whose declaring type is where the source
    /// was, and <see cref="Outermost"/> is what puts them back on the right side of the line.
    /// </para>
    /// </remarks>
    private static IEnumerable<Type> Uses(Type type)
    {
        const BindingFlags Every =
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
            | BindingFlags.Static | BindingFlags.DeclaredOnly;

        foreach (var one in Unwrap(type.BaseType)) yield return one;

        foreach (var face in type.GetInterfaces())
            foreach (var one in Unwrap(face)) yield return one;

        foreach (var field in type.GetFields(Every))
            foreach (var one in Unwrap(field.FieldType)) yield return one;

        foreach (var property in type.GetProperties(Every))
            foreach (var one in Unwrap(property.PropertyType)) yield return one;

        var bodies = type.GetMethods(Every).Cast<MethodBase>()
            .Concat(type.GetConstructors(Every))
            .Concat(type.TypeInitializer is { } prepared
                ? [prepared]
                : Array.Empty<MethodBase>());

        foreach (var body in bodies)
        {
            if (body is MethodInfo returning)
                foreach (var one in Unwrap(returning.ReturnType)) yield return one;

            foreach (var taken in body.GetParameters())
                foreach (var one in Unwrap(taken.ParameterType)) yield return one;

            foreach (var one in Referenced(body)) yield return one;
        }
    }

    /// <summary>The named types inside a type, so an array of a list of one still counts.</summary>
    /// <param name="type">The type to take apart, or nothing.</param>
    private static IEnumerable<Type> Unwrap(Type? type)
    {
        if (type is null || type.IsGenericParameter) yield break;

        if (type.HasElementType)
        {
            foreach (var one in Unwrap(type.GetElementType())) yield return one;

            yield break;
        }

        yield return type.IsConstructedGenericType ? type.GetGenericTypeDefinition() : type;

        foreach (var argument in type.GenericTypeArguments)
            foreach (var one in Unwrap(argument)) yield return one;
    }

    /// <summary>Every type a method body names, resolved from its metadata tokens.</summary>
    /// <param name="body">The method or constructor.</param>
    private static IEnumerable<Type> Referenced(MethodBase body)
    {
        var instructions = Walked(body);

        if (instructions is null) yield break;

        var onType = body.DeclaringType is { IsGenericType: true } owner
            ? owner.GetGenericArguments()
            : null;

        var onMethod = body.IsGenericMethodDefinition ? body.GetGenericArguments() : null;

        foreach (var token in Tokens(instructions))
        {
            MemberInfo? named;

            // A token the walk mis-stepped onto resolves to nothing, and a body it cannot read
            // is one this guard sees less of rather than one it may guess about.
            try
            {
                named = body.Module.ResolveMember(token, onType, onMethod);
            }
            catch (Exception thrown)
                when (thrown is ArgumentException or BadImageFormatException)
            {
                continue;
            }

            foreach (var one in Unwrap(named as Type ?? named?.DeclaringType)) yield return one;
        }
    }

    /// <summary>A method's instructions, or nothing where it has none.</summary>
    /// <param name="body">The method or constructor.</param>
    private static byte[]? Walked(MethodBase body)
    {
        try
        {
            return body.GetMethodBody()?.GetILAsByteArray();
        }
        catch (Exception thrown)
            when (thrown is InvalidOperationException or NotSupportedException)
        {
            return null;
        }
    }

    /// <summary>What every opcode's operand is, keyed by the opcode.</summary>
    /// <remarks>
    /// <b>Read off the runtime rather than written out</b>, because a hand-copied table of two
    /// hundred opcodes is a place for one wrong operand width to send the walk into the middle
    /// of an instruction and quietly stop reporting.
    /// </remarks>
    private static readonly IReadOnlyDictionary<short, OpCode> Opcodes =
        typeof(OpCodes)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.FieldType == typeof(OpCode))
            .Select(field => (OpCode)field.GetValue(null)!)
            .ToDictionary(one => one.Value);

    /// <summary>The metadata tokens in a body, by walking its instructions.</summary>
    /// <param name="instructions">The body's bytes.</param>
    private static IEnumerable<int> Tokens(byte[] instructions)
    {
        var at = 0;

        while (at < instructions.Length)
        {
            short opcode = instructions[at];

            if (opcode == 0xFE && at + 1 < instructions.Length)
            {
                opcode = unchecked((short)(0xFE00 | instructions[at + 1]));
                at += 2;
            }
            else
            {
                at += 1;
            }

            if (!Opcodes.TryGetValue(opcode, out var known)) yield break;

            switch (known.OperandType)
            {
                case OperandType.InlineNone:
                    break;

                case OperandType.ShortInlineBrTarget:
                case OperandType.ShortInlineI:
                case OperandType.ShortInlineVar:
                    at += 1;
                    break;

                case OperandType.InlineVar:
                    at += 2;
                    break;

                case OperandType.InlineField:
                case OperandType.InlineMethod:
                case OperandType.InlineTok:
                case OperandType.InlineType:
                    if (at + 4 > instructions.Length) yield break;

                    yield return BitConverter.ToInt32(instructions, at);

                    at += 4;
                    break;

                case OperandType.InlineSwitch:
                    if (at + 4 > instructions.Length) yield break;

                    at += 4 + (4 * BitConverter.ToInt32(instructions, at));
                    break;

                case OperandType.InlineI8:
                case OperandType.InlineR:
                    at += 8;
                    break;

                default:
                    at += 4;
                    break;
            }
        }
    }

    /// <summary>Every brain type that nothing outside the brain reaches.</summary>
    /// <param name="inside">The names declared under <c>Brain/</c>.</param>
    private static IReadOnlyList<Type> Unreached(IReadOnlySet<string> inside)
    {
        var composed = Composed();
        var all = composed.SelectMany(one => one.GetTypes()).ToList();

        bool IsBrain(Type type) => inside.Contains(Spelt(type));

        var seen = all.Where(one => !IsBrain(one)).ToHashSet();
        var queue = new Queue<Type>(seen);
        var reached = new HashSet<Type>();

        while (queue.Count > 0)
            foreach (var used in Uses(queue.Dequeue()))
            {
                if (!composed.Contains(used.Assembly) || !seen.Add(used)) continue;

                if (IsBrain(used)) reached.Add(Outermost(used));

                queue.Enqueue(used);
            }

        return all
            .Where(one => one.DeclaringType is null && IsBrain(one))
            .Where(one => !reached.Contains(one))
            .OrderBy(one => one.Name, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>
    /// <b>Nothing in the brain is executed by nothing.</b>
    /// </summary>
    [Fact]
    public void Every_mechanism_in_the_brain_is_reached_by_something_that_drives_it()
    {
        var inside = Names(Inside, within: true);

        Assert.NotEmpty(inside);

        var unreached = Unreached(inside);

        output.WriteLine(
            $"{inside.Count} types declared under Brain/, {unreached.Count} reached by nothing "
            + "outside it"
            + (unreached.Count == 0
                ? string.Empty
                : ":\n  " + string.Join("\n  ", unreached.Select(Spelt))));

        Assert.True(unreached.Count == 0,
            $"{unreached.Count} mechanism(s) under `Brain/` that no world, runner, fleet or "
            + "harness reaches, so no run executes them: "
            + $"{string.Join(", ", unreached.Select(Spelt))}. Wire each into a run, or delete "
            + "it and leave a revival row.");
    }

    /// <summary>
    /// <b>The line is where it says it is</b>, so a red cannot be closed by moving a file.
    /// </summary>
    /// <remarks>
    /// <b>The companion every structural guard here carries.</b> Without it this file passes
    /// for an empty brain, for a brain everything was moved out of, and for a name matched on
    /// both sides of the line at once.
    /// </remarks>
    [Fact]
    public void And_the_line_is_where_it_says_it_is()
    {
        var inside = Names(Inside, within: true);
        var outside = Names(Inside, within: false);

        // The thing being tuned: a front end, the learner, a chooser that reads a population.
        Assert.Contains("Population", inside);
        Assert.Contains("Commitment", inside);
        Assert.Contains("Code", inside);
        Assert.Contains("Brain", inside);
        Assert.Contains("Drives", inside);

        // And what drives it. A world is a problem, a runner is the join, the fleet is a
        // deployment, and none of the three is the thing being measured.
        Assert.Contains("Multiplexer", outside);
        Assert.Contains("MultiplexerRun", outside);
        Assert.Contains("Fleet", outside);
        Assert.Contains("Round", outside);
        Assert.Contains("HybridBus", outside);

        Assert.DoesNotContain("Population", outside);
        Assert.DoesNotContain("Fleet", inside);

        // And no name is on both sides, which is what lets a folder stand in for a namespace.
        // Two things sharing one spelling is how `Sorted` came out reachable through a
        // modality constant, and a collision across the line would put a world's type on the
        // brain's list of the dead.
        var both = inside
            .Intersect(outside, StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToList();

        Assert.True(both.Count == 0,
            $"{both.Count} name(s) declared inside `Brain/` and outside it: "
            + $"{string.Join(", ", both)}. One name is one thing here, and this check reads "
            + "names.");
    }
}
