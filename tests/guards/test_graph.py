"""The graphed arm, on the small parser so the guards stay fast."""

from unfused.exam.world import Question
from unfused.graph import PARSER, GraphArm, extract, nlp

MODEL = PARSER


def q(text, answer=None):
    return Question(text, answer, "k", "direct", 0, 0)


def arm(tmp_path, **kw):
    return GraphArm(tmp_path, model=MODEL, cache=None, **kw)


def test_a_particle_before_a_place_is_not_an_argument():
    events = extract(nlp(MODEL)("Mary went back to the bathroom."))
    go = next(e for e in events if e["lemma"] == "go")
    assert ["prep:to", "n:bathroom"] in go["edges"]
    assert not any(t == "n:back" for _, t in go["edges"])


def test_a_lesson_is_a_path_followed_for_another_name(tmp_path):
    a = arm(tmp_path)
    a.hear(0, "Ada keeps the kettle in the shed.")
    a.hear(1, "Bren keeps the rope in the attic.")
    a.teach("Where does Ada keep the kettle?", "shed")
    assert a.answer(q("Where does Bren keep the rope?")) == "attic"


def test_the_latest_event_wins(tmp_path):
    a = arm(tmp_path)
    a.hear(0, "Ada keeps the kettle in the shed.")
    a.teach("Where does Ada keep the kettle?", "shed")
    a.hear(1, "Ada keeps the kettle in the attic.")
    assert a.answer(q("Where does Ada keep the kettle?")) == "attic"


def test_a_word_held_every_time_is_frame_not_a_slot(tmp_path):
    a = arm(tmp_path)
    for i, (who, other) in enumerate([("Ada", "Bren"), ("Cael", "Dov"), ("Eli", "Fen")]):
        a.hear(i, f"{who} is a cousin of {other}.")
        a.teach(f"Whose cousin is {who}?", other)
    shape, fillers = a.shape("Whose cousin is Ada?")
    assert shape == "Whose cousin is <0>?" and fillers == ["ada"]


def test_nothing_learnt_is_nothing_said(tmp_path):
    a = arm(tmp_path)
    a.hear(0, "Ada keeps the kettle in the shed.")
    # asked with the telling's own verb, the question is matched against the episode;
    # asked another way, only a lesson could say how the two wordings meet
    assert a.answer(q("Where does Ada keep the kettle?")) == "shed"
    assert a.answer(q("Where is Ada's kettle kept?")) == "I don't know."


def test_what_did_not_happen_or_only_might_is_marked_on_the_verb():
    """The verb alone is the lemma, and its mood says it did not happen or only might,
    so 'doesn't keep' is the verb 'keep' heard again and never a keeping."""
    lemmas = {e["lemma"]: e["mood"] for t in ("Ada doesn't keep the kettle in the attic.",
                                              "Ada might move the kettle to the attic.")
              for e in extract(nlp(MODEL)(t))}
    assert lemmas.get("keep") == "not" and lemmas.get("move") == "might"


def test_a_denial_or_a_hedge_never_makes_an_answer_the_latest(tmp_path):
    a = arm(tmp_path)
    a.hear(0, "Ada keeps the kettle in the shed.")
    a.teach("Where does Ada keep the kettle?", "shed")
    a.hear(1, "Ada doesn't keep the kettle in the attic.")
    a.hear(2, "Ada might keep the kettle in the cellar.")
    assert a.answer(q("Where does Ada keep the kettle?")) == "shed"


def test_a_wording_no_lesson_used_borrows_the_nearest_taught_shape(tmp_path):
    a = arm(tmp_path)
    a.hear(0, "Ada keeps the kettle in the shed.")
    a.hear(1, "Bren keeps the rope in the attic.")
    a.teach("Where does Ada keep the kettle?", "shed")
    assert a.answer(q("In which room does Bren keep the rope?")) == "attic"


def test_a_conversation_teaches_with_nothing_labelled(tmp_path):
    a = arm(tmp_path)
    a.turn(0, "Ada keeps the kettle in the shed.")
    a.turn(1, "Bren keeps the rope in the attic.")
    assert a.turn(2, "Where is Ada's kettle kept?") == "I don't know."
    a.turn(3, "No, it's the shed.")
    assert a.turn(4, "Where is Bren's rope kept?") == "attic"
    a.turn(5, "Yes, that's right.")
    # a question is never stored as a telling
    assert not a.db.execute("SELECT 1 FROM events WHERE heard LIKE '%?'").fetchone()


def test_a_yes_confirms_the_answer_given_even_once_right_is_a_word_heard(tmp_path):
    a = arm(tmp_path)
    a.turn(0, "Bren keeps the rope in the attic.")
    # 'right' is now a word the graph holds, as a colour is
    a.turn(1, "The answer was right.")
    taught = []
    a.teach = lambda question, answer: taught.append(answer)
    a.pending = ("Where does Bren keep the rope?", "attic")
    a.turn(2, "Yes, that's right.")
    a.pending = ("Where does Bren keep the rope?", "shed")
    a.turn(3, "No, it's the attic.")
    assert taught == ["attic", "attic"], taught


def test_a_later_event_on_the_same_arguments_replaces_an_earlier_one(tmp_path):
    a = arm(tmp_path)
    a.hear(0, "Mary went to the kitchen.")
    a.hear(1, "Mary got the football.")
    a.hear(2, "Mary dropped the football.")
    a.hear(3, "Mary might get the football.")
    went, got, dropped, might = (f"e:{i}" for i, in a.db.execute(
        "SELECT id FROM events ORDER BY turn"))
    assert a.replaced(got) == 2
    # other arguments, or a later event that only might happen, replace nothing
    assert not a.replaced(went) and not a.replaced(dropped)


def test_a_telling_after_a_question_is_heard_not_taken_as_its_answer(tmp_path):
    a = arm(tmp_path)
    a.turn(0, "Ada keeps the kettle in the shed.")
    a.turn(1, "Where does Ada keep the kettle?")
    a.turn(2, "Bren keeps the rope in the attic.")
    assert not a.db.execute("SELECT 1 FROM learnt").fetchone()
    assert a.db.execute("SELECT 1 FROM events WHERE heard LIKE 'Bren%'").fetchone()


def test_a_question_whose_plans_find_nothing_is_taken_apart(tmp_path):
    a = arm(tmp_path)
    a.hear(0, "Ada is Bren's cousin.")
    a.hear(1, "Bren keeps bees.")
    a.hear(2, "Cara is Dov's cousin.")
    a.hear(3, "Dov sells apples.")
    a.teach("Who is Ada's cousin?", "Bren")
    a.teach("What does Bren do for a living?", "bees")
    # never taught as a whole: the cousin first, then the trade
    assert a.apart("What does Cara's cousin do for a living?", 0) == "apples"
    # and of someone never mentioned, nothing
    assert a.answer(q("What does Edda's cousin do for a living?")) == "I don't know."


def test_a_clause_about_something_is_a_relation_solved_for_it(tmp_path):
    a = arm(tmp_path)
    a.hear(0, "Ada keeps the jars in the cellar.")
    a.hear(1, "The jars are ochre.")
    a.hear(2, "Bren's lamps are kept in the attic.")
    a.hear(3, "The lamps are green.")
    a.hear(4, "Cara is Bren's cousin.")
    a.teach("Where does Ada keep the jars?", "cellar")
    a.teach("Where does Bren keep the lamps?", "attic")
    a.teach("What colour are the jars?", "ochre")
    # the place relation was taught in two wordings; the clause is solved for the thing
    # through the one this fact was told in, then the thing's colour asked
    assert a.answer(q("What colour are the things Bren keeps in the attic?")) == "green"
    # and a clause inside a clause, 'cousin' the frame of the inner one and not a name
    a.teach("Whose cousin is Cara?", "Bren")
    assert a.answer(q("What colour are the things Cara's cousin keeps in the attic?")) == "green"


def test_a_word_never_heard_is_learnt_as_the_name_it_stood_for(tmp_path):
    a = arm(tmp_path)
    a.hear(0, "The jars are ochre.")
    a.hear(1, "The lamps are green.")
    a.teach("What colour are the jars?", "ochre")
    a.teach("What colour are the pots?", "ochre")
    # one fact is one piece of evidence however often it is asked, and one is not enough
    a.teach("What colour are the pots?", "ochre")
    assert a.aliases("pots") == []
    a.hear(2, "The jars are red.")
    a.teach("What colour are the pots?", "red")
    assert a.aliases("pots") == ["jars"]
    a.hear(3, "The jars are blue.")
    assert a.answer(q("What colour are the pots?")) == "blue"
    # a person nobody told of is never taken for one somebody did
    assert "edda" not in {n for _, _, n in a.unheard("Where does Edda keep the pots?")}


def test_a_word_never_heard_is_narrowed_over_the_questions_it_is_heard_in(tmp_path):
    """No lesson says what 'pots' stands for. Each question it is heard in allows the
    things its other names keep, and two hearings leave only what both allow."""
    a = arm(tmp_path)
    a.hear(0, "Ada keeps the jars in the cellar.")
    a.hear(1, "Ada keeps the rope in the attic.")
    a.hear(2, "Bren keeps the jars in the shed.")
    a.hear(3, "Bren keeps the kettle in the barn.")
    a.teach("Where does Ada keep the rope?", "attic")
    a.teach("Where does Bren keep the kettle?", "barn")
    a.answer(q("Where does Ada keep the pots?"))
    # one hearing cannot show a word names something rather than being frame
    assert a.pinned("pots") is None
    assert a.answer(q("Where does Bren keep the pots?")) == "shed"
    assert a.pinned("pots") == "jars"


def test_a_word_never_heard_is_voted_for_at_its_first_hearing(tmp_path):
    """One hearing allows the jars and the rope; 'pots' is far nearer the jars, so that
    one hearing answers, where the hearings alone would wait for a second."""
    a = arm(tmp_path)
    a.hear(0, "Ada keeps the jars in the cellar.")
    a.hear(1, "Ada keeps the rope in the attic.")
    a.teach("Where does Ada keep the rope?", "attic")
    assert a.answer(q("Where does Ada keep the pots?")) == "cellar"
    assert a.pinned("pots") is None and a.voted("pots") == "jars"


def test_a_compound_whose_first_word_is_a_known_name_is_two_names(tmp_path):
    """'Who is Ada cousins with?' is parsed as one compound, 'Ada cousins'. Cut whole
    to its known ending, Ada stayed in the frame and every person was a shape of their
    own, so a lesson about one taught nothing about another."""
    a = arm(tmp_path)
    a.hear(0, "Ada and Bren are cousins.")
    a.hear(1, "Cal and Dot are cousins.")
    template, spans = a.template("Who is Ada cousins with?")
    assert [n for _, _, n in spans] == ["ada", "cousins"]
    a.teach("Who is Ada cousins with?", "Bren")
    a.teach("Who is Bren cousins with?", "Ada")
    assert a.answer(q("Who is Cal cousins with?")) == "dot"


def test_a_word_spelt_like_the_encoder_key_is_a_vector():
    from unfused.graph import vectors

    w, m = vectors(["boat", "model"])
    assert w.shape == m.shape


def test_a_blank_is_filled_from_what_is_in_focus(tmp_path):
    a = arm(tmp_path)
    for i, text in enumerate(["Max had a kite.", "Max flew the kite in the park.",
                              "The kite went up high."]):
        a.hear(i, text)
    a.hear(3, "***")
    for i, text in enumerate(["Lily had a ball.", "Lily threw the ball to her dog.",
                              "The dog ran fast."], start=4):
        a.hear(i, text)
    # the kite is a story ago, past a break; the ball and the dog are this episode's,
    # and a ball is thrown
    assert a.episode() == 3
    assert a.answer(q("Then Lily threw what again?")) == "ball"


def test_a_blank_is_filled_by_what_filled_the_verb(tmp_path):
    a = arm(tmp_path)
    for i, text in enumerate(["Lily ate a cake.", "Then Lily hugged Max."]):
        a.hear(i, text)
    # both were objects, and Max is the later; only the cake was eaten
    assert a.answer(q("Lily ate what again?")) == "cake"


def test_what_a_wh_word_asks_for_is_learnt_from_lessons(tmp_path):
    a = arm(tmp_path)
    a.hear(0, "Tom had a cup.")
    a.hear(1, "Ann saw the cup.")
    a.teach("Tom had what?", "cup")
    a.teach("Who saw the cup?", "Ann")
    a.hear(2, "***")
    for i, text in enumerate(["Lily had a ball.", "Max saw the ball.", "Lily saw Max."], 3):
        a.hear(i, text)
    # both were seen, and Max the later; lessons said 'what' is answered with a word
    # written small, so what is in focus for it is the ball
    assert a.focused("Then Lily saw what again?") == "ball"


def test_a_pronoun_is_resolved_against_the_episode(tmp_path):
    a = arm(tmp_path)
    a.hear(0, "Tom found a red ball.")
    a.hear(1, "He threw it over the fence.")
    edges = set(a.db.execute(
        "SELECT edges.label, edges.node FROM edges JOIN events ON events.id = edges.event "
        "WHERE events.lemma = 'throw'").fetchall())
    assert not any(n.startswith("p:") for _, n in edges)
    # onto the individuals already heard, not new ones of the same name
    assert ("nsubj", a.individuals("tom")[0]) in edges
    assert ("obj", a.individuals("ball")[0]) in edges and "red" in a.said_of(a.individuals("ball")[0])


def test_a_mention_opens_or_joins_an_individual_by_what_the_parse_marks(tmp_path):
    """'a ball' opens an individual, 'the ball' joins the one in mind, and a name joins
    its namesake in the episode; across a break nothing joins."""
    a = arm(tmp_path)
    for turn, text in enumerate(["Lily found a ball.", "Lily threw the ball.",
                                 "Tom found a ball.", "***", "Lily found the ball."]):
        a.hear(turn, text)
    assert len(a.individuals("lily")) == 2 and len(a.individuals("ball")) == 3
    first, second = sorted(a.individuals("ball"))[:2]
    thrown = a.db.execute("SELECT edges.node FROM edges JOIN events ON events.id = "
                          "edges.event WHERE events.lemma = 'throw' AND edges.label = "
                          "'obj'").fetchone()[0]
    assert thrown == first != second


def test_steps_stay_loaded_across_sentences_and_never_go_stale(tmp_path):
    a = arm(tmp_path)
    a.hear(0, "Ada keeps the kettle in the shed.")
    a.hear(1, "Bren keeps the rope in the attic.")
    for node in ("n:ada", "n:bren", "n:shed", "n:attic", "e:1", "e:2"):
        a.around(node)
    a.hear(2, "Ada keeps the kettle in the attic.")
    assert "n:bren" in a._steps
    assert all(steps == a._around(node) for node, steps in a._steps.items())
    assert "n:attic" not in a._steps or len(a._steps["n:attic"]) == 2


def test_a_question_is_matched_as_an_event_with_one_slot_free(tmp_path):
    a = arm(tmp_path)
    for i, text in enumerate(["The princess loved her veil.", "Roxy put the leaves under "
                              "her feet.", "Tom is cousins with Ann.",
                              "Sam is cousins with Bob."]):
        a.hear(i, text)
    # a possessed noun is read through, a place asked by 'where' is any link of place,
    # and a name hanging off another argument ('with Ann') still has to be there
    assert a.matched("Who loved her veil?") == "princess"
    assert a.matched("Where did Roxy put the leaves?") == "feet"
    assert a.matched("Who is cousins with Ann?") == "tom"
    assert a.matched("Who hated her veil?") is None


def test_a_blank_is_read_as_the_slot_it_stands_in(tmp_path):
    a = arm(tmp_path)
    # read as a clause of its own, as a word joined to another, through a particle
    assert a.blank("She sat down and read what?") == ("read", "obj")
    assert a.blank("In the end, Tom and what had a great time?") == ("have", "nsubj")
    assert a.blank("The man brought him back to what?") == ("bring", "prep:to")
