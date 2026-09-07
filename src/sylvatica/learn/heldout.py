"""The two fixed sets the regression gate measures against. UNCHANGING.

THE DOC: "a fixed held-out perplexity set and a fixed small general-QA set, both
unchanging for the life of the branch." Unchanging is the whole property. A gate
whose yardstick moves cannot tell a regression from a new yardstick, and a
session that adds a few sentences here because they seemed representative has
silently invalidated every gate reading before it.

IF THIS FILE IS EDITED, EVERY PRIOR GATE READING BECOMES INCOMPARABLE. That is
not a style note; it is why `tests/guards/test_gate.py` pins the content hash.
Changing it is a decision with a commit message, not a tidy-up.

WHY THE PROSE IS WRITTEN HERE RATHER THAN FETCHED. The gate measures the CHANGE
in perplexity across a consolidation cycle, never its absolute value -- so the
corpus needs to be fixed, offline, and ordinary English, and nothing else. A
downloaded corpus would add a network dependency and a licence question to a
check that runs after every cycle, in exchange for a number whose absolute value
is never read.

WHAT IT IS FOR. Phase 3 trains an adapter on a house full of invented people. The
risk named in refutation 2 is that it learns those and loses the language it
already had. So the perplexity set is deliberately about NOTHING in any house:
weather, tools, cooking, travel, work. If consolidating a house full of Halloways
makes this text less predictable, the adapter is overwriting rather than adding.
"""

from __future__ import annotations

import hashlib

# Ordinary English about ordinary things, none of it resembling a generated
# house. Varied in sentence length and register on purpose: a corpus of uniform
# short declaratives would only measure the model on uniform short declaratives.
PERPLEXITY_TEXT = """\
The kettle took a long time to boil because the element had furred up over the
winter, and nobody had thought to descale it. Rain moved across the valley in
slow grey sheets, arriving at the house about a minute after it had reached the
far ridge. She had learned to read the weather that way, by watching the far
side of the water and counting.

Bread needs three things and time is the one people skip. You can force a dough
with more yeast and a warm room, and it will rise, and it will taste of yeast
and nothing else. The slow version is mostly waiting, which is why it is the
version most often abandoned.

The train was late again, and the announcement gave no reason, which everyone on
the platform took as the reason. A man near the ticket machine explained to
nobody in particular that it had been better before, without saying before what.
Two students shared a pair of headphones and did not look up once.

Sharpening a chisel is not difficult but it is fussy. The bevel has to stay flat
against the stone, and the temptation is to lift the handle slightly, which
rounds the edge and makes it useless for paring. Most people discover this after
ruining a good tool.

There is a particular quiet in a house where someone is asleep upstairs. It is
not the absence of sound; the boiler still cycles, the fridge still hums. It is
that everything audible has been sorted, without effort, into things that might
wake them and things that will not.

Accounts of the fire disagree about almost everything except the wind. The
buildings were close together, timber-framed above the first floor, and the
gaps between them were narrower at the top than at street level. Whatever
started it, the wind is what made it a disaster rather than an incident.

He kept a notebook for three years and read it once. What surprised him was not
what he had forgotten but how confidently he had recorded things that turned out
to be wrong. The entries were detailed, dated, and in several places simply
untrue.

Coffee is more forgiving than people claim, up to a point, and then it is not
forgiving at all. Water that is merely hot will do. Water that has gone off the
boil for five minutes will do. Water poured over grounds that were milled a week
ago will not, and no technique afterwards recovers it.

The road follows the river for eleven miles and then leaves it abruptly, climbing
through a cutting that was blasted in the 1860s and has been slipping ever since.
Every few winters a section comes down and the road closes, and the villages at
the far end go back to being remote for a month.

Learning an instrument as an adult is mostly about tolerating being bad at
something in a way children are not asked to justify. The physical part comes
slowly but it does come. The harder part is the daily half hour when nothing
audible improves.

Salt draws water out of things, which is the whole of curing and most of
cooking. Put it on early and the surface dries and browns. Put it on late and it
sits on top, tasting of itself. Neither is wrong but they are not the same
decision.

The library closed at eight and the last twenty minutes were always the best,
when the tables emptied and the light outside went from blue to nothing. Someone
would be reshelving at the far end, moving a trolley a few feet at a time.

Most bridges fail at the connections rather than in the spans. The members
themselves are usually sized with room to spare; it is the joints, the bolts and
the places where one material meets another that carry the surprises. Water
finds those places first.

She said the garden had been a mistake from the beginning, planted for a climate
two hundred miles south, and that she had spent fifteen years arguing with it.
Then she said she would not do it differently.
"""

# Short factual questions with unambiguous answers, none of them about anything
# a house generator can produce. Scored by contains-match, the same weak judge
# the exam uses -- weak identically before and after a cycle is the property
# that matters.
GENERAL_QA: tuple[tuple[str, tuple[str, ...]], ...] = (
    ("What is the capital of France?", ("paris",)),
    ("How many days are in a week?", ("seven", "7")),
    ("What colour is the sky on a clear day?", ("blue",)),
    ("What is two plus three?", ("five", "5")),
    ("Which planet do we live on?", ("earth",)),
    ("What do bees make?", ("honey",)),
    ("How many legs does a spider have?", ("eight", "8")),
    ("What is the opposite of hot?", ("cold",)),
    ("What language is mainly spoken in Brazil?", ("portuguese",)),
    ("What is frozen water called?", ("ice",)),
    ("How many months are in a year?", ("twelve", "12")),
    ("What is the largest ocean on Earth?", ("pacific",)),
    ("What do you call a baby dog?", ("puppy", "pup")),
    ("What is the chemical symbol for water?", ("h2o",)),
    ("Who wrote the play Hamlet?", ("shakespeare",)),
    ("What is ten minus four?", ("six", "6")),
    ("What season comes after summer?", ("autumn", "fall")),
    ("What do plants need from the sun?", ("light", "energy", "sunlight")),
    ("What is the capital of Japan?", ("tokyo",)),
    ("How many sides does a triangle have?", ("three", "3")),
    ("What animal is known as man's best friend?", ("dog",)),
    ("What do you use to unlock a door?", ("key",)),
    ("Is the sun a star or a planet?", ("star",)),
    ("What is the first month of the year?", ("january",)),
    ("What do cows drink when they are calves?", ("milk",)),
)


def fingerprint() -> str:
    """A hash of both sets. If this changes, no prior gate reading is comparable."""
    h = hashlib.sha256()
    h.update(PERPLEXITY_TEXT.encode())
    for question, answers in GENERAL_QA:
        h.update(question.encode())
        for a in answers:
            h.update(a.encode())
    return h.hexdigest()[:16]
