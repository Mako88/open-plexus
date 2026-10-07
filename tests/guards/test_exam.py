from unfused.arms import Blind, Recall
from unfused.exam.run import judge, run
from unfused.exam.world import Question, generate_house
from unfused.store import HashEmbedder


def q(answer, form="direct", stale=None):
    return Question("Where does Vessarine keep the lanterns?", answer, "room", form, 1, 2,
                    stale)


def test_judge_counts_contains_match():
    assert judge(q("attic"), "In the attic.")["correct"]
    assert not judge(q("attic"), "I don't know.")["correct"]


def test_a_negative_answered_is_invented_and_a_refusal_is_not():
    assert judge(q(None, "negative"), "In the cellar.")["invented"]
    assert not judge(q(None, "negative"), "I don't know.")["invented"]


def test_the_old_place_is_stale_not_just_wrong():
    r = judge(q("attic", "update", stale="scullery"), "The scullery.")
    assert r["stale"] and not r["correct"]


class Parrot:
    """A faculty stub that answers with its first note, so plumbing is checked without a model."""

    name = "parrot"

    def chat(self, system, user, budget=32):
        lines = [x[2:] for x in user.splitlines() if x.startswith("- ")]
        return lines[0] if lines else "I don't know."


def test_recall_answers_from_disk_after_reopening(tmp_path):
    house = generate_house(0)
    result = run(house, lambda: Recall(tmp_path, Parrot(), HashEmbedder()), reopen_every=10,
                 limit=40)
    assert len(result["rows"]) == 40
    assert result["summary"]["score"] > 0


def test_blind_answers_every_negative_so_invents_on_all_of_them():
    house = generate_house(0)
    result = run(house, lambda: Blind([house]))
    assert result["summary"]["invented"] == 1.0


class Reader(Parrot):
    """Parrot, plus a mention list of the capitalised words, so linking can be checked."""

    def chat(self, system, user, budget=32):
        from unfused.linked import MENTIONS, RESOLVE

        if system == MENTIONS:
            import json
            return json.dumps([w.strip("?.,'s") for w in user.split() if w[:1].isupper()])
        if system == RESOLVE:
            return "[]"
        return super().chat(system, user, budget)


def test_linked_recall_reaches_a_fragment_through_a_shared_name(tmp_path):
    from unfused.linked import LinkedRecall

    arm = LinkedRecall(tmp_path, Reader(), HashEmbedder(), k=1)
    arm.hear(0, "Vessarine is Tolmick's cousin.")
    arm.hear(1, "It has been raining all week.")
    arm.hear(2, "Tolmick binds books for a living.")
    arm.close()
    arm = LinkedRecall(tmp_path, Reader(), HashEmbedder(), k=1)
    notes = arm.recall("What does Vessarine's cousin do for a living?")
    assert any("binds books" in n for n in notes)
    assert notes == sorted(notes, key=lambda n: int(n.split("]")[0].split()[-1]))


def test_a_number_is_matched_in_either_form_and_whole():
    count = Question("How many people keep things in the attic?", "2", "count", "count", 5, 9)
    assert judge(count, "2 people.")["correct"]
    assert not judge(count, "12 people.")["correct"]

    def n(a):
        return Question("How many?", a, "k", "f", 0, 0)

    assert judge(n("two"), "2")["correct"] and judge(n("2"), "There are two.")["correct"]
    assert not judge(n("12"), "2")["correct"] and not judge(n("one"), "none")["correct"]



def test_a_story_is_asked_about_what_it_told_before():
    import spacy

    from unfused.exam.stories import make, right, sentences

    text = ("Tom had a red ball. Mr. Brown lived next door. Tom threw the ball over the "
            "fence. Mr. Brown was not happy. Then Tom found the ball under the big tree.")
    assert sentences(text)[1] == "Mr. Brown lived next door."
    s = make(text, 0, spacy.load("en_core_web_sm"))
    # the blank is a wh-word standing where the answer was, as a word of its own
    assert s.answer in ("ball", "tree", "tom")
    assert {"what", "who"} & set(s.question.lower().strip("?").split())
    assert s.answer not in s.question.lower().split()
    assert right(s.answers, f"It was the {s.answer}.") and not right(s.answers, "the moon")


def test_a_check_asks_a_told_sentence_in_a_parents_words():
    import spacy

    from unfused.exam.stories import asked

    got = {q: t.lemma_ for q, t in asked(spacy.load("en_core_web_sm")(
        "Roxy put the leaves under her feet."))}
    assert got == {"What did Roxy put under her feet?": "leaf",
                   "Where did Roxy put the leaves?": "foot",
                   "Who put the leaves under her feet?": "Roxy"}, got
    # no doer named, or a verb under an auxiliary: nothing a parent's 'did' can ask
    assert not asked(spacy.load("en_core_web_sm")("She was playing in the park."))


def test_a_check_two_tellings_answer_accepts_both_and_is_asked_once():
    import spacy

    from unfused.exam.stories import _checks, sentences

    text = "Lily found a shell. Lily found a crab. She was happy. It was a fun day."
    sents = sentences(text)
    checks = _checks(text, sents, list(spacy.load("en_core_web_sm").pipe(sents)), None)
    got = [(c.question, c.answers) for c in checks]
    assert got.count(("What did Lily find?", ("crab", "shell"))) == 1, got
    assert all(q != "What did Lily find?" or a == ("crab", "shell") for q, a in got), got
    # told with a pronoun and more besides, a telling still answers; 'they' is not Lily
    from unfused.exam.stories import _answers, _asks

    nlp = spacy.load("en_core_web_sm")
    (_, _, asking), = [a for a in _asks(nlp("Lily saw a bird.")) if a[0].startswith("What")]
    told = [w for _, _, w in _asks(nlp("Then she saw dark clouds in the sky."), pronoun=True)]
    assert any(_answers(asking, w) for w in told), told
    told = [w for _, _, w in _asks(nlp("Then they saw dark clouds in the sky."), pronoun=True)]
    assert not any(_answers(asking, w) for w in told), told


def test_a_far_check_is_asked_once_the_story_is_told_about_an_early_sentence():
    import spacy

    from unfused.exam.stories import FAR, _checks, sentences

    text = ("Lily found a shell. Tom kicked the ball. Ben ate the cake. Mia lost her hat. "
            "Sam saw a dog. They were happy. It was a fun day. The sun was warm. "
            "Everyone went home.")
    sents = sentences(text)
    checks = _checks(text, sents, list(spacy.load("en_core_web_sm").pipe(sents)), None)
    far = [c for c in checks if c.form == "far"]
    assert far and all(c.at == len(sents) for c in far), checks
    # each about a sentence at least FAR before the end, and never one a near check asks
    near = {c.question for c in checks if c.form == "check"}
    assert all(c.question not in near for c in far), checks
    assert all(any(c.answer in s.lower() for s in sents[:len(sents) - FAR]) for c in far)
