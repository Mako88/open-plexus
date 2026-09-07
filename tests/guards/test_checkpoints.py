"""Which checkpoint gets picked, and what the size cap refuses.

No network: `published` is the only function here that needs the Hub, and it is
stubbed. What is checked is the ARITHMETIC on filenames, because that is what
silently picks the wrong weights.
"""

from __future__ import annotations

import pytest

from sylvatica.core import checkpoints

# A snapshot of what the Hub listed on 2026-09-06. It is allowed to go stale --
# `published()` is the live check and these are fixtures for the picking logic.
LISTED = [
    "rwkv7-g1d-0.1b-20260129-ctx8192.pth",
    "rwkv7a-g1d-0.1b-20260212-ctx8192.pth",
    "rwkv7-g1d-0.4b-20260210-ctx8192.pth",
    "rwkv7-g1i-1.5b-20260805-ctx16384.pth",
    "rwkv7-g1j-1.5b-20260831-ctx16384.pth",
    "rwkv7-g1i-2.9b-20260805-ctx16384.pth",
    "rwkv7-g1j-2.9b-20260831-ctx16384.pth",
    "rwkv7-g1i-7.2b-20260805-ctx16384.pth",
    "rwkv7-g1j-13.3b-20260831-ctx16384.pth",
]


@pytest.fixture
def hub(monkeypatch):
    monkeypatch.setattr(checkpoints, "published", lambda repo=checkpoints.REPO: sorted(
        LISTED, key=lambda f: (checkpoints.parse_size(f) or 1e9, checkpoints.parse_date(f), f)
    ))


def test_size_and_date_come_off_the_name():
    assert checkpoints.parse_size("rwkv7-g1j-1.5b-20260831-ctx16384.pth") == 1.5
    assert checkpoints.parse_size("rwkv7-g1d-0.4b-20260210-ctx8192.pth") == 0.4
    assert checkpoints.parse_date("rwkv7-g1j-1.5b-20260831-ctx16384.pth") == 20260831
    assert checkpoints.parse_size("something-else.pth") is None
    assert checkpoints.parse_date("something-else.pth") == 0


def test_the_newest_release_of_a_size_wins(hub):
    """TWO RELEASES OF ONE SIZE IS THE NORMAL CASE, not an edge one.

    The line ships a new letter every few weeks and leaves the old files up.
    Alphabetical order puts `g1i` before `g1j`, so without the date tie-break a
    reading would be taken on the SUPERSEDED checkpoint while the commit message
    named only the size -- and nothing would look wrong.
    """
    assert checkpoints.pick(1.5) == "rwkv7-g1j-1.5b-20260831-ctx16384.pth"
    assert checkpoints.pick(2.9) == "rwkv7-g1j-2.9b-20260831-ctx16384.pth"


def test_the_nearest_size_wins_when_the_exact_one_is_absent(hub):
    assert checkpoints.parse_size(checkpoints.pick(0.5)) == 0.4
    assert checkpoints.parse_size(checkpoints.pick(3.0)) == 2.9


def test_anything_over_three_billion_refuses_to_download():
    """The doc's rule, enforced where it can fire rather than in a reviewer's head.

    The failure it prevents -- 11 GB of VRAM swapping on a model that does not
    fit -- looks like slowness, not like a mistake, so it can go undiagnosed for
    a session.
    """
    with pytest.raises(ValueError, match="caps unattended downloads at 3B"):
        checkpoints.fetch("rwkv7-g1j-7.2b-20260831-ctx16384.pth")
    with pytest.raises(ValueError, match="caps unattended downloads at 3B"):
        checkpoints.fetch("rwkv7-g1j-13.3b-20260831-ctx16384.pth")
