099 — The wire format is pinned to its version, by a check rather than a rule
============================================================================

**Status:** built, and the pin is verified by breaking it deliberately. It closes the gap
note 096 named and note 098 walked into one commit later.

---

## IN PLAIN TERMS

Two peers can speak different versions of the protocol and **agree that they agree**, because
the handshake compared configuration and said nothing about the wire format. Then every
request is misparsed — a read is read as some other kind of message, about some other
concept — and answered confidently.

**The version now rides in the fingerprint, so a dialect mismatch is refused.** And forgetting
to bump it is a failing test rather than a lapse of discipline.

---

## The two parts

**The version is in the digest.** `fingerprint()` covers `PROTOCOL` alongside the routing and
the key source, so peers on different formats refuse each other with the machinery that
already existed. A mutation removing it is caught.

**And the format is pinned to the version.** A test asserts `_REQUEST.format` equals the
layout the declared `PROTOCOL` describes:

    PROTOCOL   layout      what changed
           1   !iii        reads only: (concept, previous, token)
           2   !Biii       a kind byte, and the write payload after the header

**Verified by breaking it:** changing the layout to `!Bqii` without touching `PROTOCOL` makes
the test fail, saying *"the request layout changed without PROTOCOL changing, so two peers on
different code would agree on the fingerprint and misparse each other."*

> CLAUDE.md rule 18 asks for **a rule that makes the mistake structurally impossible** over one
> that asks for more care, and says plainly that if a proposed rule cannot be turned into a
> check, say so rather than pretending vigilance will hold. *"Remember to bump the protocol
> version"* is exactly the kind of rule that does not hold — **note 098's author forgot it one
> commit after note 096 wrote it down.** This is the check.

## What is NOT claimed

**Not negotiation.** A mismatch is refused, not resolved. A real network presumably wants a
peer to speak an older dialect rather than decline, and nothing here does that — so a rolling
upgrade would partition the network cleanly in half.

**Not a version for the STORE.** The digest covers the wire and the addressing. It does not
cover the value or readout matrices, so two peers with different `wv` still agree — note 096's
limitation, unchanged.

**And the pin is a single expected layout.** It knows one format per version and refuses an
unknown `PROTOCOL`, which is right while there is one, and would need a table if peers ever
had to support several at once.

---

## A process failure while writing this, recorded because the outcome was luck

Verifying the pin meant deliberately breaking it: rewrite `_REQUEST` as `!Bqii`, run the
test, restore the file. The test failed as intended and the restore reported success.

**The restore wrote back byte-identical content, and Windows' timestamp granularity meant
Python did not notice.** So `__pycache__` kept the *broken* bytecode: the source read
`!Biii` and the imported module reported `!Bqii`. Two tests then failed for a reason
invisible in the source.

**The mutation harness caught it and I overrode it.** It said *"The suite is red before any
mutation. Fix that first."* — precisely the right refusal — and I committed and pushed
anyway, reading it as noise from a tool rather than as the tool doing its job.

**The commit turned out to be correct**, because the source was always right and CI compiles
fresh. That is luck, not process: had the source been wrong, the same sequence would have
shipped it.

> **Two lessons, and the second is the general one.** A verification that rewrites a source
> file and restores it must clear `__pycache__`, because identical content plus coarse
> timestamps defeats mtime invalidation. And *"the suite is red"* from any tool is a stop
> condition — the whole value of a check is that it is believed when it is inconvenient.
