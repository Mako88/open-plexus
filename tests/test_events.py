"""An event reads from the sentence or the trunk's restatement; replaying the true events
counts what every person holds at every question of task 7, which is the counting tool's
ceiling and a check that a gift moves the object from giver to receiver."""

import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(ROOT / "src"))

from openplexus.babi import stories  # noqa: E402
from openplexus.events import count_word, event_of, holdings, who_is_asked  # noqa: E402


def test_an_event_reads_from_the_sentence_or_the_restatement():
    assert event_of("Mary grabbed the milk there.") == ("took", "mary", "milk")
    assert event_of("John put down the football.") == ("dropped", "john", "football")
    assert event_of("Sandra handed the apple to Daniel.") == ("gave", "sandra", "apple", "daniel")
    assert event_of("Fred went back to the garden.") is None
    assert event_of("Mary|took|milk") == ("took", "mary", "milk")
    assert event_of(" Sandra | gave | apple | Daniel. ") == ("gave", "sandra", "apple", "daniel")
    assert event_of("none") is None
    assert who_is_asked("How many objects is Mary carrying?") == "mary"
    assert count_word(0) == "none" and count_word(2) == "two"


def test_replaying_the_true_events_counts_right_at_every_question():
    misses = []
    for s in stories([7]):
        events = [event_of(x) for x in s.sentences]
        for q in s.questions:
            held = holdings(events[:q.heard])
            said = count_word(len(held.get(who_is_asked(q.text), set())))
            if said != q.answer:
                misses.append((s.episode, q.heard, q.answer, said))
    assert not misses, misses[:5]
