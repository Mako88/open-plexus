"""Kinds learnt from how words connect, out of the red set once it held (THE ORDER,
the foundation's item 3)."""

from unfused.graph import PARSER, GraphArm

MODEL = PARSER


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


def test_kinds_change_at_sizes_of_the_graph_never_at_who_reads_them(tmp_path):
    """Kinds are read at fixed sizes of the graph, from that size's own events, so an arm
    that reads them after every sentence holds the same kinds as one that reads them
    once: a comparison of two versions is never moved by which of them asked."""
    told = ["Tom had a red ball.", "Ann had a blue ball.", "Tom had a red cup.",
            "Ann had a blue hat.", "The hat was green.", "The cup was red.",
            "Tom saw a green cup.", "Ann saw a red hat."]
    often, once = arm(tmp_path / "often"), arm(tmp_path / "once")
    for turn, text in enumerate(told):
        often.hear(turn, text)
        often.concepts()
        once.hear(turn, text)
    assert often.concepts() == once.concepts()
