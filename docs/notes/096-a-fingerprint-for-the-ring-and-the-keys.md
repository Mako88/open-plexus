096 — A fingerprint for the ring and the keys, and a check that guarded nothing
==============================================================================

**Status:** built, and a surviving mutation showed the first version of the guard was
untested. It closes note 095's last gap — *"the seed is a shared constant... and nothing
detects it."*

---

## IN PLAIN TERMS

Two peers can disagree about where a fact lives, or about how a key is built, and **nothing
about the disagreement is visible.** A read routed by the wrong ring reaches a peer that
never received the write, comes back as zeros, and a zero vector decodes to whichever token
the readout happens to prefer. That is an answer, not an error, and note 086 records a whole
run of them.

**Now both sides compute a fingerprint of what they must agree about, and a mismatch is
refused rather than served.**

---

## What it covers

Derived on each side from its own configuration, never exchanged as data, so agreement means
the configurations match rather than that one side was told what to claim:

    routing      peer count, ring seed
    key source   its seed, spread, width, start token, ROUTE and markers

**`route` is in there deliberately.** `current` against `first-concept` puts every binding at
a different address (note 073), and that is precisely the kind of difference a reader would
never notice.

## The mutation that survived, and why

`a-config-mismatch-is-served-anyway` disables the **peer's** check. It survived every test in
the file.

**Because `RemoteConcepts` refuses before it sends anything**, so going through the client
never exercises the peer's side at all. Four tests asserting that mismatched callers are
refused were all asserting the *client's* refusal.

> **A peer must not depend on callers being well behaved.** It owns the data, and a caller
> with a stale ring is exactly what churn produces — the client-side check protects a
> cooperative caller from itself, and the peer-side one protects the data from everyone else.
> They are two guards, and only one of them was tested.

The fix is a raw-socket test: claim a wrong fingerprint, ask for a read, and assert nothing
comes back. Mutation now caught, **189 total, 1288 tests.**

## What is NOT claimed

**Not that the fingerprint covers everything two peers must share.** It covers routing and
key construction. It does not cover the value or readout matrices — note 086's
`cluster_node` fingerprint hashed those, and this hashes neither, so two peers with different
`wv` would still agree here and disagree in fact.

**Not that refusal is graceful.** The client raises and the peer hangs up. A network should
presumably re-derive the ring and retry rather than fail, and nothing here does that.

**And not versioned.** Two peers on different code with the same config produce the same
fingerprint, so a protocol change is invisible to it.
