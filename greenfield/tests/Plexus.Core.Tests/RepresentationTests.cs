using Plexus.Core.Representation;

namespace Plexus.Core.Tests;

/// <summary>
/// What an identity has to survive before two holders can share one.
/// </summary>
/// <remarks>
/// Every claim here is about determinism rather than about learning. A brain that learns well
/// and derives a different identity in each process is a brain that cannot be distributed at
/// all.
/// </remarks>
public sealed class RepresentationTests
{
    [Fact]
    public void One_artifact_has_one_identity_in_every_process() =>
        Pending.Claim(
            "canonical encoding and IContentIdentity, checked by encoding the same fact in a "
            + "second process and comparing the bytes as well as the identity");

    [Fact]
    public void A_set_encodes_the_same_whatever_order_it_was_built_in() =>
        Pending.Claim("canonical ordering of set-like inputs by their own encoded bytes");

    [Fact]
    public void A_sequence_encodes_differently_when_its_order_differs() =>
        Pending.Claim(
            "the companion to the set claim: sorting sequence-like inputs too would make "
            + "gives(a, b, c) and gives(c, b, a) one artifact");

    [Fact]
    public void Two_facts_built_from_equal_arguments_are_equal() =>
        Pending.Claim(
            "structural equality on GroundFact, which is written out because generated record "
            + "equality would compare the argument array by object identity");

    [Fact]
    public void An_identity_presented_with_different_bytes_fails_the_guard() =>
        Pending.Claim(
            "the collision guard that keeps canonical bytes beside each identity in test "
            + "builds");

    [Fact]
    public void An_encoder_version_travels_with_the_identity_it_produced() =>
        Pending.Claim(
            "ICanonicalEncoding.Version reaching the identity, so that an encoder upgrade is a "
            + "migration rather than a silent renaming of every artifact");
}
