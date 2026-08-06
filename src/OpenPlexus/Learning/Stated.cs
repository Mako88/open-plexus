using System.Collections.Immutable;
using OpenPlexus.Codes;
using OpenPlexus.Graph;

namespace OpenPlexus.Learning;

/// <summary>
/// A relation INSTANCE as a node, with a role-typed arm to each thing it
/// relates — <b>the general form of what <see cref="Worlds.Clutrr"/> had written
/// out for two arguments by hand.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>AN EDGE IS A LABEL ON A PAIR AND CANNOT BE REASONED ABOUT; A NODE CAN.</b>
/// <see cref="Edge"/> holds a partner and a <see cref="Kind"/>, so every relation
/// this design can hold is binary, drawn from a closed vocabulary, and has no
/// address of its own. Minting a node for the OCCASION of a relation gives all
/// three back: <c>gave(alice, bob, book)</c> is one node with three arms, and the
/// node is a code like any other, so it can be walked to, counted with, chunked,
/// and stood in a row.
/// </para>
/// <para>
/// <b>MEASURED ON <see cref="Worlds.Clutrr"/> AT TWO ORDERS OF MAGNITUDE UNDER
/// THE SLOT HUB, KEEPING BOTH LEVELS.</b> What is new here is only that it is no
/// longer that world's private arrangement — the plan lists positing, a goal that
/// is not the current state, an answer that is no code it has seen, and binding
/// beyond kinship as four separate gaps, and a structure whose parts are bound to
/// roles and can be CONSTRUCTED is the common half of all four.
/// </para>
/// <para>
/// <b>A STAR AND NEVER A CLIQUE, WHICH IS THE FINDING THAT COST THE MOST.</b> An
/// occasion pairs everything in it, so stating the instance, all the fillers and
/// the type in ONE moment also writes filler-to-filler and filler-to-type — and a
/// relation co-occurring with every code it ever related is exactly the superhub
/// <see cref="Kind.Code"/> warns about. Written that way it timed the walk out
/// entirely, which is one hub traded for a worse one. So each arm is its own small
/// moment.
/// </para>
/// <para>
/// <b>AND THE INSTANCE IS FLEETING WHATEVER THE CALLER SAYS.</b> One lasting node
/// per stated relation would grow the TYPE's row by an entry per statement
/// forever, which is the hub this exists to avoid. The fillers may last or not —
/// that is a fact about the world and the caller's to state; the instance does not
/// get a say.
/// </para>
/// </remarks>
public static class Stated
{
    /// <summary>
    /// The modality every relation instance is minted into.
    /// </summary>
    /// <remarks>
    /// <b>Brain-side, beside <see cref="Chunk.Minted"/>, and fixed forever</b> —
    /// changing it renumbers every instance ever minted on every machine. Distinct
    /// from <see cref="Kind.Relations"/> because a relation's TYPE lasts and an
    /// instance of it does not, and a walk that could not tell them apart would
    /// rank one occasion against the rule it is an instance of.
    /// </remarks>
    /// <remarks>
    /// <b>Private until something needs to ASK whether a code is an instance</b>,
    /// which nothing does yet — <c>DeadCodeTests</c>'s rule, and the first walk that
    /// wants to rank occasions apart from rules is what makes it public.
    /// </remarks>
    private const byte Instances = 201;

    /// <summary>
    /// The code for one stated occasion of a relation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>DERIVED FROM THE RELATION, ITS FILLERS IN ORDER, AND WHEN IT WAS
    /// STATED</b> — so two machines reading the same stream mint the same instance
    /// with nothing to ask, which is the red-ball property. The same arithmetic
    /// <see cref="Kind.Role"/> and <see cref="Chunk"/> already agree by; nothing
    /// here is counted out or drawn.
    /// </para>
    /// <para>
    /// <b>THE CLOCK IS IN IT ON PURPOSE, AND WITHOUT IT THIS WOULD NOT BE AN
    /// INSTANCE.</b> Saying the same thing about the same people twice is two
    /// occasions, and folding them onto one code would accumulate a count on a node
    /// whose whole job is to be seen once — which is the thing the type-level cell
    /// in <see cref="Star"/> exists to carry instead.
    /// </para>
    /// <para>
    /// <b>ORDER MATTERS, WHICH IS THE POINT.</b> <c>gave(alice, bob)</c> and
    /// <c>gave(bob, alice)</c> are different facts, so the fold is over the fillers
    /// as given and never sorted — the opposite of <see cref="Chunk"/>, whose
    /// members name a SET.
    /// </para>
    /// </remarks>
    /// <param name="relation">What is being stated.</param>
    /// <param name="fillers">What fills each slot, slot nought first.</param>
    /// <param name="at">The observing machine's clock.</param>
    public static Code Instance(Kind relation, IReadOnlyList<Code> fillers, long at)
    {
        ArgumentNullException.ThrowIfNull(fillers);

        if (fillers.Count == 0)
            throw new ArgumentException(
                "a relation with no arguments relates nothing; the instance would "
                + "be a node with no arms and the type-level cell alone says it",
                nameof(fillers));

        var hash = Agreed.Fold(Agreed.Basis, relation.Code.Value);

        for (var slot = 0; slot < fillers.Count; slot++)
            hash = Agreed.Fold(hash, fillers[slot].Value);

        return new Code(Instances, Agreed.Mix(Agreed.Fold(hash, (ulong)at)));
    }

    /// <summary>
    /// The moments that write one stated relation into the graph.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>ONE SMALL MOMENT PER ARM, PLUS ONE FOR THE TYPE.</b> Each arm joins a
    /// filler to the instance and carries which slot that filler occupies, so the
    /// role channel gets <c>relation/n</c> against the filler — the cell
    /// <see cref="Kind.Role"/> says is what buys transfer, because it names no
    /// argument at all and therefore accumulates across every pair that ever fills
    /// those slots.
    /// </para>
    /// <para>
    /// <b>THE TYPE-LEVEL CELL IS THE LAST MOMENT AND IT IS NOT OPTIONAL.</b> An
    /// instance is seen once, so nothing accumulates on it and a walk arriving there
    /// learns only that this happened. Joining the instance to
    /// <see cref="Kind.Code"/> is what lets a walk get from an occasion to the rule
    /// it is an occasion of.
    /// </para>
    /// <para>
    /// <b>Returned rather than written</b>, for the reason <see cref="Node.Fire"/>
    /// returns rather than sends: what the moments ARE is a claim worth asserting
    /// on its own, and a caller that has to have a machine to check it cannot.
    /// </para>
    /// </remarks>
    /// <param name="relation">What is being stated.</param>
    /// <param name="fillers">What fills each slot, slot nought first.</param>
    /// <param name="at">The observing machine's clock.</param>
    /// <param name="lasting">
    /// Which fillers OUTLIVE this statement. <b>Null is none of them</b>, which is
    /// the safe reading — a filler wrongly called lasting grows a row forever, and
    /// one wrongly called fleeting merely fails to accumulate. The instance is never
    /// in here whatever this says.
    /// </param>
    public static ImmutableArray<Coded> Star(
        Kind relation,
        IReadOnlyList<Code> fillers,
        long at,
        IReadOnlySet<Code>? lasting = null)
    {
        ArgumentNullException.ThrowIfNull(fillers);

        var instance = Instance(relation, fillers, at);
        var alone = new HashSet<Code> { instance };

        var moments = ImmutableArray.CreateBuilder<Coded>(fillers.Count + 1);

        for (var slot = 0; slot < fillers.Count; slot++)
        {
            var filler = fillers[slot];

            if (filler == instance)
                throw new ArgumentException(
                    "a filler collided with the instance minted for its own "
                    + "statement, and a code cannot be its own partner",
                    nameof(fillers));

            moments.Add(new Coded
            {
                Codes = [filler, instance],

                // THE INSTANCE ALWAYS, THE FILLER ONLY IF THE WORLD SAYS SO.
                Passing = lasting is not null && lasting.Contains(filler)
                    ? alone
                    : new HashSet<Code> { instance, filler },

                Relating = relation,
                Filling = new Dictionary<Code, int> { [filler] = slot },
            });
        }

        // WHAT KIND OF STATEMENT IT WAS. One way, because the instance is
        // fleeting: the type records what met it and does not record into it.
        moments.Add(new Coded { Codes = [instance, relation.Code], Passing = alone });

        return moments.MoveToImmutable();
    }
}
