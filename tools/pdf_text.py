"""Extract a PDF to UTF-8 text, so a paper can actually be read.

## Why this exists

GOALS section 6.2 lists prior work that must be read before anything is built on
it, and rule 1 says a summary tells you what a result is CALLED, not what was
run. That rule is only followable if the papers can be opened.

The SWIM paper was written off as unreadable -- the fetch reported "unparseable
binary" and note 039 was published against a Wikipedia summary with a rule-1
caveat attached. **It was not unparseable.** The PDF was fine and sitting on
disk; the console is cp1252 and a single `fi` ligature aborted the extraction, so
the failure looked like a bad download rather than an encoding error.

Ten pages of primary source were behind that, including the two things the
summary did not have: the concrete parameter relation `T' >= 3 x RTT`, and the
paper describing this project's own bug in its own words.

**So: write to a file, never to the console.** The lesson generalises past PDFs
-- a tool that dies on the first character it cannot print will make a readable
source look unavailable.

    python tools/pdf_text.py paper.pdf out.txt
"""

from __future__ import annotations

import sys

from pypdf import PdfReader


def main() -> None:
    if len(sys.argv) < 3:
        raise SystemExit("usage: pdf_text.py <in.pdf> <out.txt>")
    reader = PdfReader(sys.argv[1])
    with open(sys.argv[2], "w", encoding="utf-8") as handle:
        handle.write(f"[{len(reader.pages)} pages]\n")
        for index, page in enumerate(reader.pages):
            handle.write(f"\n===== PAGE {index + 1} =====\n")
            handle.write(page.extract_text() or "")
    print(f"wrote {sys.argv[2]}, {len(reader.pages)} pages")


if __name__ == "__main__":
    main()
