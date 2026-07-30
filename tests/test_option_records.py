"""The CONFIG block is a check, and a check nobody has seen fail is a claim.

`tools/check_options.py` grew a sixth rule on John's request, 2026-07-30: every entry in
an option record states the configuration the measurement was taken in, in a fixed-key
block, *"in case that invalidates the result later"*.

The rule it replaces was a sentence at the bottom of that file admitting the model state
was *"the field most likely to be forgotten and the hardest to detect"*. CLAUDE.md rule 18
says to turn that into a check or say plainly it cannot be one, and rule 10 says the check
does not count until it has been watched going red for the right reason.

So this file breaks a record on purpose, one way per test:

- a whole entry with no block at all
- an entry whose block drops ONE key, which is the realistic failure -- `scale` and
  `knobs` are the ones a writer in a hurry leaves out
- an entry that states a status in prose, which the markers alone would not catch
- a record short enough that the header exemption used to swallow it whole

**Why the missing-key case is the important one.** An entry with no block is obvious to
any reader. An entry with five of six keys looks complete, and the missing one is exactly
the field that would have said the number came from somewhere else -- which is how the
beam's CLUTRR figure was nearly quoted as a kinship figure five times too large.
"""

from __future__ import annotations

import sys
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(ROOT))

from tools.check_options import (  # noqa: E402
    CONFIG_KEYS, EXEMPT, OPTIONS, config_blocks, main, record_problems,
)

#: A record that passes, used as the base every broken case is derived from. Kept minimal
#: on purpose: if the checker only passes on a rich file, it is matching prose and not
#: structure.
GOOD = """# Option record — a thing

> **RECORD ONLY. This file carries no status.** It lives in DECISIONS.md, which is the
> one place a status belongs. Only events are recorded here.

## What exists

- `openplexus/nothing.py`

## What was tried, and what came back

### It was tried — `note 001`

    CONFIG  when    2026-07-30
            source  note 001
            script  tools/nothing.py
            task    kinship, hops 2
            model   width 64
            knobs   nothing 1
            scale   3 seeds

It came back 0.500.
"""


class TheConfigCheckBites(unittest.TestCase):

    def test_the_good_record_passes(self):
        """Without this the failing cases below prove nothing -- a checker that rejects
        everything 'catches' every break and is worthless."""
        self.assertEqual(record_problems("good.md", GOOD), [])

    def test_an_entry_with_no_block_is_caught(self):
        broken = GOOD.replace("    CONFIG  when    2026-07-30\n", "")
        for key in CONFIG_KEYS[1:]:
            broken = "\n".join(l for l in broken.splitlines()
                               if not l.strip().startswith(key)) + "\n"
        problems = record_problems("broken.md", broken)
        self.assertTrue(
            any("no CONFIG block" in p for p in problems),
            f"an entry with no configuration at all passed: {problems}")

    def test_dropping_a_single_key_is_caught(self):
        """The realistic failure. Five of six keys reads as complete."""
        for key in CONFIG_KEYS:
            with self.subTest(key=key):
                # Drop just this key's line, keeping CONFIG itself where `when` shares it.
                lines = []
                for line in GOOD.splitlines():
                    stripped = line.strip()
                    if stripped.startswith(f"{key} ") or stripped.startswith(
                            f"CONFIG  {key} "):
                        if key == "when":
                            lines.append("    CONFIG")
                        continue
                    lines.append(line)
                broken = "\n".join(lines) + "\n"
                problems = record_problems("broken.md", broken)
                self.assertTrue(
                    any(key in p and "omits" in p for p in problems),
                    f"a block missing `{key}` passed as complete: {problems}")

    def test_a_short_record_is_not_exempt_from_the_status_checks(self):
        """The header exclusion was `text[:1400]`, a byte count, so **every record under
        1400 characters had an empty body and was checked for nothing**. It passed
        because the only two records that existed were long, and 84 short ones were about
        to land on top of it. `GOOD` is deliberately under that length, so this test would
        have failed before `split_header` replaced the count with the blockquote."""
        self.assertLess(len(GOOD), 1400, "the fixture must sit inside the old window")
        broken = GOOD.replace("It came back 0.500.", "It came back 0.500. ✅")
        self.assertTrue(
            any("outside its header" in p
                for p in record_problems("broken.md", broken)),
            "a status marker in a short record passed, which means the header exemption "
            "is swallowing the whole file again")

    def test_a_status_in_prose_is_caught(self):
        broken = GOOD.replace("It came back 0.500.", "It came back 0.500 and this is "
                                                     "what we use.")
        self.assertTrue(
            any("status in prose" in p for p in record_problems("broken.md", broken)),
            "a conclusion written as a sentence passed, which is the same failure as a "
            "marker and harder to see")


class EveryRecordInTheTreeComplies(unittest.TestCase):

    def test_the_checker_passes_over_the_real_records(self):
        self.assertEqual(main(), 0, "tools/check_options.py is failing; run it for why")

    def test_every_record_has_at_least_one_entry(self):
        """A record with no `###` entry passes every other check vacuously, because
        there is nothing for the config rule to apply to. An option nobody has tried is
        a legal thing to have -- it says so in one entry, rather than in an empty file."""
        empty = [p.name for p in sorted(OPTIONS.glob("*.md"))
                 if p.name not in EXEMPT
                 and not config_blocks(p.read_text(encoding="utf-8"))]
        self.assertEqual(
            empty, [],
            f"{empty} have no entries, so the config rule applies to nothing in them. "
            f"An untried option still has a record: one entry saying what was reasoned, "
            f"when, and that nothing ran.")


if __name__ == "__main__":
    unittest.main()
