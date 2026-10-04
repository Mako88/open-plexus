"""The individual layer, out of the red set once it held (THE ORDER, the foundation's
item 1). Read with the parser every run reads with: the small spaCy one takes 'painted the
ball blue' as one name."""

from unfused.graph import GraphArm

MODEL = "stanza"


def arm(tmp_path):
    return GraphArm(tmp_path, model=MODEL, cache=None)


def test_a_thing_is_its_own_node_apart_from_its_words(tmp_path):
    """The individual layer: each thing an identity of its own, the words said of it
    attached and free to change. Two stories' Lilys are two girls who share a name; the
    still room and the sitting room are two rooms; a red ball painted blue is still the
    one ball; 'a ball' then 'the ball' is one ball."""
    a = arm(tmp_path)
    for turn, text in enumerate([
            "Lily found a ball.", "***", "Lily found a ball.", "***",
            "Ada keeps hooks in the still room.", "Ada keeps jars in the sitting room.", "***",
            "Tom had a red ball.", "Tom painted the ball blue.", "Tom threw the ball."]):
        a.hear(turn, text)
    assert hasattr(a, "individuals"), "the graph keeps no individuals apart from words"
    assert len(a.individuals("lily")) == 2, "two stories' Lilys are two girls"
    rooms = a.individuals("room")
    assert len(rooms) == 2, f"the still room and the sitting room are two rooms, not {rooms}"
    balls = a.individuals("ball", episode=True)
    assert len(balls) == 1, f"the painted ball is one ball, not {balls}"
    assert "blue" in a.said_of(balls[0]), "what was said of the ball last holds"
