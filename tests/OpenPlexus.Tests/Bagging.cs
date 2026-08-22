using OpenPlexus.Codes;

namespace OpenPlexus.Tests;

/// <summary>Reading a moment the way a story arm does.</summary>
internal static class Bagging
{
    /// <summary>The statements as ordered word lists, which is the shape a world built.</summary>
    /// <param name="moment">What the world showed.</param>
    public static IReadOnlyList<IReadOnlyList<Code>> Said(this Coded moment) =>
        moment.Groups is { } parts ? [.. parts.Select(one => one.Codes)] : [];

    /// <summary>Every word of every statement, which is what a bag-of-words arm sees.</summary>
    /// <param name="moment">What the world showed.</param>
    public static IReadOnlySet<Code> Words(this Coded moment) => Storied.Of(moment).Words;

    /// <summary>The question as an ordered word list, empty where the world asks none.</summary>
    /// <param name="moment">What the world showed.</param>
    public static IReadOnlyList<Code> Question(this Coded moment) =>
        moment.Asked?.Codes ?? [];

}
