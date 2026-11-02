"""Step 4, writing: what goes in a row. The raw sentence as heard, or the trunk's own restatement
of it with every pronoun replaced by the name it stands for ("John went to the garden", not
"After that he went to the garden"), so a row stands alone and is found by the name a question
asks for. The restatement is written at hearing time by the served trunk given the story so far;
one row a sentence, in the order heard, so a row's place in time is its place in the store.

The trunk's text is not trusted as written (the plan's rule: text the trunk writes for code is
checked by code). Asked to restate every sentence, this trunk rewrote half of 4,000 bAbI rows and
corrupted most of those, swapping names and places from the story so far into sentences that had
no pronoun (2026-11-02). So only a sentence with a pronoun is restated, and the restatement is
kept only if it keeps every other word of the sentence and adds nothing but names the story so
far has used; otherwise the row is the sentence as heard.
"""

from __future__ import annotations

import re

RESTATE_SYSTEM = (
    "You rewrite one sentence of a story so that it stands alone. Replace every pronoun (he, she, "
    "they, it, him, her, them) with the name or names it refers to in the story so far. Keep the "
    "sentence's meaning and words otherwise. Reply with the rewritten sentence only."
)
PRONOUNS = {"he", "she", "they", "it", "him", "her", "them", "his", "hers", "their", "its"}
JOINERS = {"and"}


def words(text: str) -> list[str]:
    return re.findall(r"[A-Za-z']+", text)


def has_pronoun(sentence: str) -> bool:
    return any(w.lower() in PRONOUNS for w in words(sentence))


def names_in(so_far: list[str]) -> set[str]:
    """Capitalised words of the story so far that are not sentence-initial function words: the
    names a restatement may add."""
    out = set()
    for s in so_far:
        for i, w in enumerate(words(s)):
            if w[0].isupper() and (i > 0 or w.lower() not in PRONOUNS | {"the", "then", "after", "following", "afterwards"}):
                out.add(w)
    return out


def restate_messages(so_far: list[str], sentence: str) -> list[dict]:
    story = "\n".join(so_far) if so_far else "(nothing yet)"
    return [{"role": "system", "content": RESTATE_SYSTEM},
            {"role": "user", "content": f"Story so far:\n{story}\n\nSentence: {sentence}"}]


def guarded(sentence: str, reply: str, so_far: list[str]) -> str:
    """The restatement if it passes the check, else the sentence: every non-pronoun word of the
    sentence is kept, and what is added is names the story so far has used (joined by 'and')."""
    line = reply.strip().splitlines()[0].strip() if reply.strip() else ""
    if not line:
        return sentence
    kept = [w.lower() for w in words(sentence) if w.lower() not in PRONOUNS]
    got = [w.lower() for w in words(line)]
    if any(kept.count(w) > got.count(w) for w in set(kept)):
        return sentence
    allowed = {n.lower() for n in names_in(so_far)} | JOINERS
    added = list(got)
    for w in kept:
        added.remove(w)
    if any(w not in allowed for w in added) or not any(w not in JOINERS for w in added):
        return sentence
    return line


def restated(stories, trunk) -> dict[str, list[str]]:
    """Every sentence of every story as a row, by episode, one row a sentence: the sentences with a
    pronoun restated by the trunk and checked, the rest as heard; `trunk.ask_many` takes the
    prompts in one batch so the server's streams are used."""
    out: dict[str, list[str]] = {s.episode: list(s.sentences) for s in stories}
    prompts, where = [], []
    for s in stories:
        for j, sentence in enumerate(s.sentences):
            if has_pronoun(sentence):
                prompts.append(restate_messages(s.sentences[:j], sentence))
                where.append((s.episode, j))
    replies = trunk.ask_many(prompts) if prompts else []
    for (episode, j), reply in zip(where, replies):
        story = next(s for s in stories if s.episode == episode)
        out[episode][j] = guarded(story.sentences[j], reply, story.sentences[:j])
    return out
