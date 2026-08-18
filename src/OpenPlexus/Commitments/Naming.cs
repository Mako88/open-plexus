using System.Collections.Immutable;
using OpenPlexus.Codes;

namespace OpenPlexus.Commitments;

/// <summary>
/// Codes minted to stand for sets of codes, and the two directions they are read in.
/// </summary>
/// <remarks>
/// <para>
/// <b>Rung five, and it is the only part of the ladder that goes up.</b> Rungs one to
/// four make a commitment narrower; nothing else anywhere makes anything more
/// general, and a machine built only from them can be arbitrarily accurate and hold
/// no concept at all — every multiplexer rule learnt and no representation of *the
/// address*.
/// </para>
/// <para>
/// <b>Its trigger is redundancy rather than failure</b>, so it is the one rung a
/// failure does not summon. What proposes a name is the same sub-scope turning up in
/// commitment after commitment, which is a fact about what has already been learnt
/// rather than about what has just gone wrong.
/// </para>
/// <para>
/// <b>A name is reached by inference and never written as a partner.</b> Nothing
/// emits a minted code, so <see cref="Fold"/> adds it to a moment whose members are
/// all present. `csharp` broke two controls learning that the other arrangement —
/// letting a name join the occasion it completes — makes a name's only partner its
/// own last member.
/// </para>
/// <para>
/// <b>And it is what makes a fixed front end survivable.</b> The red-ball property
/// forbids fitting the quantiser, so no learned feature can live inside it. A name
/// over co-firing codes is a learned feature that lives ABOVE the codes, identical on
/// every machine because its identity derives from its members.
/// </para>
/// </remarks>
public sealed class Naming
{
    /// <summary>The modality a minted name lives in.</summary>
    /// <remarks>
    /// <b>Distinct from <see cref="Commitment.Committed"/> on purpose.</b> A name
    /// stands for a set of codes and a commitment makes a claim about one; both are
    /// codes and can sit in a scope, and telling them apart in a report is worth a
    /// byte.
    /// </remarks>
    public const byte Meant = 211;

    private readonly Dictionary<Code, ImmutableArray<Code>> _means = [];

    /// <summary>What each minted name stands for, in a stable order.</summary>
    public IEnumerable<KeyValuePair<Code, ImmutableArray<Code>>> Means =>
        _means.OrderBy(one => one.Key);

    /// <summary>How many names have been minted.</summary>
    public int Count => _means.Count;

    /// <summary>What a name for these members is called, on every machine, forever.</summary>
    /// <param name="members">What the name stands for, in any order.</param>
    /// <remarks>
    /// <b>Sorted first, because a set has no order.</b> Two nodes that notice the same
    /// redundancy must mint the same code without speaking, or the whole point of a
    /// name — that it means the same thing in two places — is lost.
    /// </remarks>
    public static Code Name(ImmutableArray<Code> members)
    {
        if (members.IsDefaultOrEmpty || members.Distinct().Count() < 2)
            throw new ArgumentException("a name for fewer than two codes says nothing", nameof(members));

        var hash = Hashing.Fold(Hashing.Basis, (ulong)members.Distinct().Count());

        foreach (var code in members.Distinct().Order())
        {
            hash = Hashing.Fold(hash, code.Modality);
            hash = Hashing.Fold(hash, code.Value);
        }

        return new Code(Meant, Hashing.Mix(hash));
    }

    /// <summary>Mints a name for a set of codes.</summary>
    /// <param name="members">What it stands for.</param>
    /// <returns>The name, whether or not it was new.</returns>
    public Code Mint(ImmutableArray<Code> members)
    {
        var name = Name(members);

        _means.TryAdd(name, [.. members.Distinct().Order()]);

        return name;
    }

    /// <summary>Whether this code is one of ours.</summary>
    /// <param name="code">The code to ask about.</param>
    public bool Knows(Code code) => _means.ContainsKey(code);

    /// <summary>
    /// The moment with every name whose members are all present added to it.
    /// </summary>
    /// <param name="moment">What is live.</param>
    /// <remarks>
    /// <b>Run to a fixed point, because a name may stand for a set containing
    /// names.</b> That is the whole of the bootstrapping: a name reached this round
    /// can complete a larger one in the same moment, which is how a second level ever
    /// comes to exist rather than being declared.
    /// </remarks>
    public IReadOnlySet<Code> Fold(IReadOnlySet<Code> moment)
    {
        ArgumentNullException.ThrowIfNull(moment);

        if (_means.Count == 0) return moment;

        var folded = new HashSet<Code>(moment);

        bool again;

        do
        {
            again = false;

            foreach (var (name, members) in _means.OrderBy(one => one.Key))
                if (!folded.Contains(name) && members.All(folded.Contains))
                {
                    folded.Add(name);
                    again = true;
                }
        }
        while (again);

        return folded;
    }

    /// <summary>The scope with every name replaced by what it stands for.</summary>
    /// <param name="scope">A scope that may name names.</param>
    /// <remarks>
    /// <b>What a soundness check has to be asked in.</b> A world knows nothing about
    /// minted codes, so a rule expressed in them can only be checked against the world
    /// once it is spelled back out — and a rewrite that changed what a commitment
    /// CLAIMS would show up here as a rule that stopped being true.
    /// </remarks>
    public ImmutableArray<Code> Unfold(ImmutableArray<Code> scope)
    {
        if (scope.IsDefaultOrEmpty) return scope;

        var spelled = new HashSet<Code>();
        var pending = new Stack<Code>(scope);

        while (pending.Count > 0)
        {
            var code = pending.Pop();

            if (_means.TryGetValue(code, out var members))
                foreach (var member in members) pending.Push(member);
            else spelled.Add(code);
        }

        return [.. spelled.Order()];
    }
}
