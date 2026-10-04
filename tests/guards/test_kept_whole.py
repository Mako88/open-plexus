"""Nothing the parse gives dropped, out of the red set once it held (THE ORDER, the
foundation's item 2)."""

from unfused.graph import PARSER, extract, nlp

MODEL = PARSER


def test_nothing_the_parse_gives_is_dropped():
    """John's, 2026-10-04: keep all of what is heard. Every word of a sentence is held
    by a node or a link of its own: the article, the auxiliary, the adverb, the marker
    joining two clauses, and the adjective apart from its noun. A verb's lemma is the
    verb alone, with negation, modals and particles as links of their own, so 'will not
    give back' and 'gave' are one verb heard twice."""
    text = "The little girl did not quickly give the red ball back because she was sad."
    doc = nlp(MODEL)(text)
    events = extract(doc)
    held = {w for e in events for _, t in e["edges"] for w in t.split(":", 1)[-1].split()}
    held |= {e["lemma"] for e in events}
    missing = sorted({t.lower_ for t in doc if not t.is_punct} - held
                     - {t.lemma_.lower() for t in doc if t.lemma_.lower() in held})
    glued = sorted(e["lemma"] for e in events if " " in e["lemma"])
    folded = sorted(t for e in events for _, t in e["edges"]
                    if t.startswith("n:") and len(t[2:].split()) > 1)
    assert not missing and not glued and not folded, (
        f"dropped {missing}, lemmas glued {glued}, adjectives folded into {folded}")
