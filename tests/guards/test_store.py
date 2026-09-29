from unfused.store import HashEmbedder, SqliteStore


def test_write_is_idempotent_and_survives_reopen(tmp_path):
    store = SqliteStore(tmp_path / "s.db", embedder=HashEmbedder())
    store.now = 7
    a = store.write("Vessarine repairs clocks.", "turn:7")
    b = store.write("Vessarine repairs clocks.", "turn:7")
    assert a.id == b.id and store.count() == 1
    store.close()
    again = SqliteStore(tmp_path / "s.db", embedder=HashEmbedder())
    assert again.count() == 1 and again.now == 7
    assert again.search("clocks", k=1)[0].fragment.text == "Vessarine repairs clocks."


def test_archived_fragments_do_not_surface_and_are_still_there(tmp_path):
    store = SqliteStore(tmp_path / "s.db", embedder=HashEmbedder())
    old = store.write("The lanterns are in the scullery.", "turn:1")
    store.write("The lanterns are in the attic.", "turn:2", cites=[old.id])
    store.archive([old.id])
    assert all(h.fragment.id != old.id for h in store.search("lanterns", k=5))
    assert store.get(old.id) is not None


def test_there_is_no_delete():
    assert not any("delete" in name.lower() for name in dir(SqliteStore))


def test_recall_count_only_rises(tmp_path):
    store = SqliteStore(tmp_path / "s.db", embedder=HashEmbedder())
    f = store.write("a fact", "turn:0")
    store.mark_recalled([f.id])
    store.mark_recalled([f.id])
    assert store.get(f.id).recall_count == 2
