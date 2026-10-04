"""Kinds learnt from how words connect, out of the red set once it held (THE ORDER,
the foundation's item 3)."""

from unfused.graph import GraphArm

MODEL = "en_core_web_trf"


def arm(tmp_path):
    return GraphArm(tmp_path, model=MODEL, cache=None)


def test_kinds_are_learnt_from_how_words_connect(tmp_path):
    """The concept layer: two words are of a kind where they connect alike to things of
    alike kinds (THE ORDER, kinds). Colours fall together and things apart, from hearing
    alone, with no label naming either."""
    a = arm(tmp_path)
    for turn, text in enumerate([
            "Tom had a red ball.", "Ann had a blue ball.", "Tom had a red cup.",
            "Ann had a blue hat.", "The hat was green.", "The cup was red.",
            "Tom saw a green cup.", "Ann saw a red hat."]):
        a.hear(turn, text)
    assert hasattr(a, "kind_of"), "the graph learns no kinds"
    colours = {a.kind_of(w) for w in ("red", "blue", "green")}
    things = {a.kind_of(w) for w in ("ball", "cup", "hat")}
    assert len(colours) == 1 and len(things) == 1 and colours != things, (
        f"colours {colours}, things {things}")
