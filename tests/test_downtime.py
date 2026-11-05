"""Downtime is read from the card, contention from the system's own step rate, and a round is
kept only when it holds every skill and gains something."""

import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(ROOT / "src"))

from openplexus.downtime import Card, Rate, card, idle  # noqa: E402
from openplexus.verdict import paired_se, verdict  # noqa: E402


def test_the_card_is_read_from_nvidia_smi():
    def run(args, **kw):
        return subprocess.CompletedProcess(args, 0, stdout="11264, 2048, 7\n")
    c = card(run)
    assert c.total_gib == 11.0 and c.used_gib == 2.0 and c.util == 7.0 and c.free_gib == 9.0


def test_idle_needs_every_sample_free():
    seq = iter([Card(11, 1, 3), Card(11, 1, 5), Card(11, 6, 40)])
    assert not idle(9.0, minutes=0.75, sample_s=15, read=lambda: next(seq), sleep=lambda s: None)
    assert idle(9.0, minutes=0.75, sample_s=15, read=lambda: Card(11, 1, 3), sleep=lambda s: None)
    assert not idle(9.0, read=lambda: Card(11, 3, 3), sleep=lambda s: None)  # not enough free


def test_contention_is_the_step_rate_collapsing():
    r = Rate(floor=0.2, window_s=300, warm=3)
    for i in range(6):
        r.beat(60.0 * i, 10 * i)  # ten steps a minute
    assert not r.contended(330)
    r.beat(600, 51)  # one step in the next five minutes
    assert r.contended(600)
    q = Rate(floor=0.2, window_s=300, warm=3)
    for i in range(6):
        q.beat(60.0 * i, 10 * i)
    assert q.contended(300 + 1200)  # no beat for twenty minutes
    assert not Rate().contended(10)  # too early to say


def test_a_round_is_kept_only_when_it_holds_and_gains():
    assert paired_se(25, 7) == (18 / 32 ** 0.5)
    second_sleep = verdict({"gold": (13, 14), "asking": (5, 18)}, {"floor": (25, 7), "facts": (46, 3)})
    assert not second_sleep["keep"] and second_sleep["why"].startswith("lost asking")
    kept = verdict({"gold": (13, 14), "asking": (10, 12)}, {"floor": (25, 7)})
    assert kept["keep"] and kept["lines"]["floor"] == 3.2
    assert not verdict({"gold": (10, 10)}, {"floor": (12, 10)})["keep"]
