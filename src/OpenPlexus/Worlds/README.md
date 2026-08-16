# What a folder here means

John's, 2026-08-16, and the point is that a rule the tree already follows should be visible
without reading a doc. A world's category decides what may be done to it, and until now that
lived in `docs/plan.md` and in whoever had read it most recently.

Every file keeps `namespace OpenPlexus.Worlds`, so a folder is a claim about the world and
never a compilation boundary. Moving a file between them is the decision, and it is the only
way a world changes category.

## `Spine/`

**One world, and everything is wired up to it.** `Roaming` is TextWorld's shape — a house,
people walking round it, things picked up and put down — generated here rather than ported so
its ground truth can still be enumerated. It grows to each tier rather than closing: word
order, then twins, then acting.

A world is moved in here by John and by nobody else. Step one of the order of the work is a
mechanism for every architecture requirement and then this world wired through all of them,
so *is it wired to the spine yet* is the question that decides whether a mechanism counts as
built.

## `Isolating/`

**Constructed to close one question, and deleted when it closes.** Ground truth is
enumerable, the state is small, and one axis moves at a time — which is what lets soundness,
overshoot and hard-round coverage be read at all. A benchmark varies its parser, vocabulary,
quest length and room count at once, and a number off one cannot be attributed to any of them.

These are built freely. What is forbidden is leaving one here while nobody decides: a world
whose question has shut is deleted, and the finding lives in the commit and in the test.

## `Corpora/`

**Somebody else's text or pictures, with a published baseline.** The ground truth cannot be
enumerated, so every instrument that needs enumeration goes dark and what is left is a score
against a bar somebody else measured. That is worth having and it is not worth confusing with
the two above.

A corpus can contain its own answer, which an `Isolating/` world cannot, so a score here is
read beside a count of its twins rather than on its own.

## The root

`IWorld.cs` is the seam — `Turn`, `IWorld`, `IActed`, `IWithholds` — and `Kinds` and `Seeds`
are shared arithmetic. Nothing here is a world, so nothing here has a category.
