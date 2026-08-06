using OpenPlexus.Codes;
using OpenPlexus.Graph;
using OpenPlexus.Learning;

namespace OpenPlexus.Tests;

/// <summary>
/// An act that was ASSIGNED records what followed it in its own cell.
/// </summary>
/// <remarks>
/// <para>
/// <b>THE ONE CELL THAT ANSWERS THE QUESTION THIS PROJECT EXISTS FOR.</b> The goal
/// is <i>what would the world look like if I did X</i>, and every count here is
/// over acts something already wanted to take — so <see cref="Kind.After"/> holds
/// <c>P(outcome | act)</c> among acts a policy selected, which is not
/// <c>P(outcome | do(act))</c> and cannot be turned into it by counting more of
/// the same. Wanting an act and the act working are confounded wherever one thing
/// chose both.
/// </para>
/// <para>
/// <b>An act drawn without consulting the state breaks that confound by
/// construction, which is what a randomised trial is.</b> These assert only that
/// the two kinds of evidence land somewhere separate — what a walk then makes of
/// them is a measurement and not a mechanism.
/// </para>
/// </remarks>
public sealed class MeddledTests
{
    private static Code C(ulong value) => Fixture.C(value);

    /// <summary>An act at <c>C(1)</c> followed by an outcome at <c>C(9)</c>.</summary>
    private static async Task<Bench> Followed(IReadOnlySet<Code>? forced)
    {
        var bench = new Bench(Fixture.Dials(stamina: 10.0));

        await new LocalRendezvous(bench.Local).JoinAsync(new Occasion
        {
            Onsets = [C(9)], Live = [], Recent = [C(1)], At = 5, Forced = forced,
        });

        return bench;
    }

    [Fact]
    public async Task An_act_nothing_chose_writes_a_DIFFERENT_cell_from_one_it_did()
    {
        var chosen = await Followed(null);
        var assigned = await Followed(new HashSet<Code> { C(1) });

        // THE CONTROL: every occasion ever written before this existed.
        Assert.Equal(1.0, chosen.Node(C(1)).Together(C(9), Kind.After));
        Assert.Equal(0.0, chosen.Node(C(1)).Together(C(9), Kind.Meddled));

        // AND THE INTERVENTION, in a cell of its own.
        Assert.Equal(1.0, assigned.Node(C(1)).Together(C(9), Kind.Meddled));
    }

    [Fact]
    public async Task And_it_REPLACES_the_observational_cell_rather_than_joining_it()
    {
        // OR ONE OCCASION IS COUNTED TWICE AND THE OBSERVATIONAL ROW ABSORBS THE
        // EVIDENCE IT EXISTS TO BE COMPARED AGAINST. A cell is (partner, kind), so
        // writing both would make `After` hold the interventions too and the
        // comparison would be a subset against its own superset.
        var assigned = await Followed(new HashSet<Code> { C(1) });

        Assert.Equal(0.0, assigned.Node(C(1)).Together(C(9), Kind.After));

        // AND IT IS ONE ENTRY, NOT TWO. The row is the scaling wall.
        Assert.Equal(1, assigned.Node(C(1)).Entries);
    }

    [Fact]
    public async Task It_is_still_written_ONE_WAY()
    {
        // The past records the future and the future records nothing about the
        // past -- the property that makes an edge mean `then`. Forcing the act
        // must not quietly buy a reverse edge.
        var assigned = await Followed(new HashSet<Code> { C(1) });

        Assert.Equal(0.0, assigned.Node(C(9)).Together(C(1), Kind.Meddled));
        Assert.Equal(0.0, assigned.Node(C(9)).Together(C(1), Kind.After));
    }

    [Fact]
    public async Task Forcing_the_OUTCOME_changes_nothing_about_the_act()
    {
        // THE FLAG IS ABOUT THE SENDER AND NEVER THE RECEIVER. What was assigned
        // is the thing that CAME FIRST; whether the outcome was also assigned says
        // nothing about whether the act caused it, and reading the wrong end would
        // file ordinary evidence as interventional.
        var bench = await Followed(new HashSet<Code> { C(9) });

        Assert.Equal(1.0, bench.Node(C(1)).Together(C(9), Kind.After));
        Assert.Equal(0.0, bench.Node(C(1)).Together(C(9), Kind.Meddled));
    }

    [Fact]
    public async Task A_SIMULTANEOUS_pair_is_untouched_by_it()
    {
        // SIMULTANEITY SAYS NOTHING CAUSAL, so there is nothing here to separate.
        // Moving it as well would cost a cell and buy no distinction.
        var bench = new Bench(Fixture.Dials(stamina: 10.0));

        await new LocalRendezvous(bench.Local).JoinAsync(new Occasion
        {
            Onsets = [C(1), C(2)],
            Live = [],
            At = 5,
            Forced = new HashSet<Code> { C(1) },
        });

        Assert.Equal(1.0, bench.Node(C(1)).Together(C(2), Kind.With));
        Assert.Equal(0.0, bench.Node(C(1)).Together(C(2), Kind.Meddled));
    }

    [Fact]
    public void The_flag_survives_the_window_or_the_cell_can_never_be_written()
    {
        // AN ACT AND ITS OUTCOME ARE NEVER IN ONE MOMENT -- that is the whole
        // reason `Window` exists -- so by the time the outcome arrives, the fact
        // that nothing chose the act is a moment old. Carrying the code and
        // dropping the flag would leave `Kind.Meddled` reachable only where cause
        // and effect coincide, which is nowhere.
        var window = new Window(span: 2);

        window.Carry([C(1), C(2)], [], now: 10, forced: new HashSet<Code> { C(1) });

        Assert.Contains(C(1), window.Forced(10));
        Assert.Contains(C(1), window.Forced(11));

        // AND ONLY THE ONE THAT WAS ASSIGNED.
        Assert.DoesNotContain(C(2), window.Forced(10));

        // AND IT EXPIRES WITH THE CARRY RATHER THAN OUTLIVING IT.
        Assert.Empty(window.Forced(12));
        Assert.DoesNotContain(C(1), window.Recent(12));
    }

    [Fact]
    public void A_code_that_comes_back_is_no_longer_forced()
    {
        // `Carry` DROPS WHAT HAS RESTARTED, and the flag has to go with it -- a
        // code that is live again is not a carried one, and its next departure
        // says for itself whether anything chose it.
        var window = new Window(span: 4);

        window.Carry([C(1)], [], now: 10, forced: new HashSet<Code> { C(1) });
        window.Carry([], [C(1)], now: 11);

        Assert.Empty(window.Forced(11));
    }

    [Fact]
    public void Saying_nothing_is_exactly_the_behaviour_before_this_existed()
    {
        // THE CONTROL FOR EVERY MEASUREMENT ALREADY TAKEN. A front end that never
        // mentions forcing must write what it always wrote.
        var window = new Window(span: 2);

        window.Carry([C(1)], [], now: 10);

        Assert.Contains(C(1), window.Recent(10));
        Assert.Empty(window.Forced(10));
    }
}
