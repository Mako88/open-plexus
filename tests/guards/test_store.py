"""The store keeps everything, and the one rule it must never break.

NOTHING IS EVER DELETED. `archived_at` is the strongest thing that happens to a
row. That is what makes the store the lossless record against which the state's
forgetting can be MEASURED -- if the store could lose things too, an exam could
never tell which memory failed. It is also what makes a correction auditable: a
correction is a new fragment that cites the old one, and both survive.

The guard against a `delete` method is not paranoia about a careless commit. It
is that a store which can delete is a different design, and the phases after this
one -- consolidation deciding a fragment is now in the weights, idle deciding it
is cold -- both produce a moment where deleting looks like the tidy thing to do.

Everything here uses `HashEmbedder`, so it runs in milliseconds with no model
download and no GPU. What it cannot check is whether retrieval RANKS well; that
is a reading, not a test, and it belongs to the Tier B exam.
"""

from __future__ import annotations

import time

import numpy as np
import pytest

from sylvatica.store import (
    Fragment,
    HashEmbedder,
    ReplaySpec,
    SqliteStore,
    Store,
    episode,
    fragment_id,
)


@pytest.fixture
def store(tmp_path):
    s = SqliteStore(tmp_path / "t.db", embedder=HashEmbedder(dims=64))
    yield s
    s.close()


def test_the_store_has_no_way_to_delete_anything():
    """THE RULE, AS A TEST. Archiving is the strongest thing that happens.

    A store that can delete is a different design from this one, and the next two
    phases each create a moment where deleting looks tidy: consolidation deciding
    a fragment now lives in the weights, and the idle scheduler deciding a
    fragment is cold. Neither may erase.
    """
    forbidden = [
        name
        for name in dir(SqliteStore)
        if any(word in name.lower() for word in ("delete", "remove", "purge", "drop"))
    ]
    assert not forbidden, (
        f"SqliteStore grew {forbidden}. Nothing is ever deleted -- see the module "
        "docstring. If a phase needs a fragment gone, it archives it."
    )

    import inspect

    from sylvatica.store import sqlite as module

    source = inspect.getsource(module)
    assert "DELETE FROM" not in source.upper(), "a DELETE statement reached the store"


def test_it_satisfies_the_protocol_the_rest_of_the_system_sees(store):
    assert isinstance(store, Store)


def test_a_fragment_id_is_the_content_and_not_the_node():
    """WHAT MAKES GOSSIP IDEMPOTENT IN PHASE 5.

    Two nodes that saw the same turn must write the SAME id, or a fragment
    arriving from a peer is a duplicate rather than a no-op -- and de-duplicating
    it would need coordination, which constraint C1 forbids.
    """
    a = episode("the roof was replaced", "thread:main#turn:7", node_id="alpha")
    b = episode("the roof was replaced", "thread:main#turn:7", node_id="beta")
    assert a.id == b.id, "the same turn on two nodes produced two ids"

    other = episode("the roof was replaced", "thread:main#turn:8", node_id="alpha")
    assert a.id != other.id, "two different turns collided"
    assert fragment_id("summary", "x", "p") != fragment_id("episode", "x", "p")


def test_writing_the_same_fragment_twice_is_a_no_op(store):
    f = episode("Osric repairs clocks", "thread:main#turn:1")
    store.write(f)
    store.write(f)
    store.write(episode("Osric repairs clocks", "thread:main#turn:1"))
    assert store.tiers()["total"] == 1


def test_every_turn_is_written_and_comes_back(store):
    store.write_turn("Marta keeps eleven beehives", "thread:main#turn:1")
    store.write_turn("the kettle is on the shelf", "thread:main#turn:2")
    assert store.tiers()["total"] == 2
    hits = store.search("beehives", k=2)
    assert hits and "beehives" in hits[0].fragment.text


def test_search_returns_why_it_ranked_as_well_as_what(store):
    """The hybrid weights are an ARM. An arm cannot be tuned off a number that
    has already had the weights folded into it, so each ranker's position is
    kept beside the fused score."""
    for i, text in enumerate(
        ["eleven beehives behind the shed", "clocks in the front room", "a storm took the tiles"]
    ):
        store.write_turn(text, f"thread:main#turn:{i}")
    hits = store.search("beehives shed", k=3)
    assert hits
    assert any(h.lexical_rank is not None for h in hits)
    assert any(h.vector_rank is not None for h in hits)


def test_archiving_hides_a_fragment_from_search_and_keeps_it(store):
    f = store.write_turn("the gate was left open", "thread:main#turn:1")
    assert store.search("gate", k=3)

    store.archive([f.id])
    assert not store.search("gate", k=3), "an archived fragment came back by default"
    assert store.search("gate", k=3, include_archived=True), "archiving lost the row"
    assert store.get(f.id) is not None
    assert store.tiers()["archived"] == 1


def test_recall_count_only_ever_goes_up(store):
    """A G-COUNTER, so two nodes can count the same recall and merge by taking
    the maximum without agreeing on anything first."""
    f = store.write_turn("the dairy is cold", "thread:main#turn:1")
    assert store.get(f.id).recall_count == 0
    store.mark_recalled([f.id])
    store.mark_recalled([f.id])
    got = store.get(f.id)
    assert got.recall_count == 2
    assert got.last_recalled_at is not None


def test_consolidated_is_what_moves_a_fragment_out_of_the_hot_tier(store):
    a = store.write_turn("one", "thread:main#turn:1")
    store.write_turn("two", "thread:main#turn:2")
    assert store.tiers() == {"total": 2, "hot": 2, "cold": 0, "archived": 0}

    store.mark_consolidated([a.id])
    assert store.tiers() == {"total": 2, "hot": 1, "cold": 1, "archived": 0}


def test_replay_prefers_what_the_store_is_not_reaching(store):
    """REHEARSAL WEIGHTS LOW `recall_count` ON PURPOSE.

    A fragment that keeps being retrieved is already being served by the store.
    Replay is for what the store is NOT reaching, because that is the part
    consolidation has to carry into the weights.
    """
    now = time.time()
    for i in range(40):
        store.write(
            episode(f"fact number {i}", f"thread:main#turn:{i}", created_at=now + i)
        )
    ids = [r["id"] for r in store.db.execute("SELECT id FROM fragments LIMIT 10")]
    for _ in range(20):
        store.mark_recalled(ids)

    sample = store.sample_for_replay(ReplaySpec(n=20, recent=0.5, rehearsal=0.5, seed=3))
    assert sample
    rehearsed = [f for f in sample if f.id in ids]
    assert len(rehearsed) < len(sample) / 2, (
        "replay drew heavily from the fragments retrieval already serves"
    )


def test_replay_is_deterministic_for_a_seed(store):
    for i in range(30):
        store.write_turn(f"thing {i}", f"thread:main#turn:{i}")
    spec = ReplaySpec(n=10, seed=7)
    a = [f.id for f in store.sample_for_replay(spec)]
    b = [f.id for f in store.sample_for_replay(spec)]
    assert a == b, "a reading names a seed; if the seed does not pin the mix it names nothing"


def test_the_store_survives_being_closed_and_reopened(tmp_path):
    """The whole point of the slow memory: it is on disk, and it is still there."""
    path = tmp_path / "t.db"
    s = SqliteStore(path, embedder=HashEmbedder(dims=64))
    s.write_turn("eleven beehives behind the shed", "thread:main#turn:1")
    s.close()

    again = SqliteStore(path, embedder=HashEmbedder(dims=64))
    assert again.tiers()["total"] == 1
    hits = again.search("beehives", k=1)
    assert hits and "beehives" in hits[0].fragment.text
    again.close()


def test_a_search_with_no_words_returns_nothing_rather_than_raising(store):
    store.write_turn("something", "thread:main#turn:1")
    assert store.search("", k=3) == []
    assert store.search("   ", k=3) == []


def test_punctuation_in_a_query_does_not_raise(store):
    """FTS5 `MATCH` takes a query LANGUAGE, and a person's sentence is not one.

    An apostrophe or a bare AND is a syntax error, and the failure would arrive
    mid-exam as a crash rather than as a bad answer.
    """
    store.write_turn("Marta's beehives are behind the shed", "thread:main#turn:1")
    for query in ["Marta's beehives", "beehives AND shed", 'a "quoted" thing', "what? (really)"]:
        store.search(query, k=3)


def test_a_deadline_that_has_passed_still_returns_something(store):
    """C3: A CLUSTER VANISHING MID-THOUGHT IS NORMAL. A search that runs out of
    time returns fewer memories, never an exception -- a store whose failure mode
    is raising cannot be sharded later without every caller changing."""
    store.write_turn("eleven beehives", "thread:main#turn:1")
    hits = store.search("beehives", k=3, deadline=time.monotonic() - 1.0)
    assert isinstance(hits, list)


def test_an_embedding_supplied_by_the_caller_is_kept(store):
    v = np.ones(64, dtype=np.float32) / 8.0
    f = Fragment(
        id="deadbeef",
        kind="fact",
        text="a supplied vector",
        created_at=time.time(),
        provenance="thread:main#turn:1",
        node_id="local",
        embedding=v,
    )
    store.write(f)
    assert np.allclose(store.get("deadbeef").embedding, v)


def test_corrections_cite_what_they_correct(store):
    """A correction is a NEW fragment, and both survive. That is what auditable
    means here -- the old claim and the thing that replaced it are both readable."""
    old = store.write_turn("the roof was replaced in autumn", "thread:main#turn:1")
    new = store.write(
        Fragment(
            id=fragment_id("fact", "the roof was replaced in spring", "thread:main#turn:9"),
            kind="fact",
            text="the roof was replaced in spring",
            created_at=time.time(),
            provenance="thread:main#turn:9",
            node_id="local",
            cites=(old.id,),
        )
    )
    store.archive([old.id])
    assert store.get(old.id) is not None
    assert store.get(new.id).cites == (old.id,)
