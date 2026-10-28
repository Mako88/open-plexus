"""The story grouping is checked against the data: every supporting line number maps to a
sentence the question had heard, and on task 1 the answer word is in that sentence, since a
one-fact question's one fact is where the person last went."""

from openplexus.babi import stories


def test_supporting_lines_map_to_heard_sentences():
    for s in stories([1, 2, 3]):
        for q in s.questions:
            assert len(q.supporting) == s.task
            assert all(0 <= i < q.heard for i in q.supporting), (s.episode, q)
            if s.task == 1:
                assert q.answer in s.sentences[q.supporting[0]], (s.episode, q)


def test_the_example_from_the_plan_of_attack():
    s = next(s for s in stories([1]) if s.index == 0)
    q = s.questions[2]
    assert q.text == "Where is Sandra?" and q.answer == "kitchen"
    assert s.sentences[q.supporting[0]] == "Sandra journeyed to the kitchen."
    assert q.heard == 6 and len(s.questions) == 5
