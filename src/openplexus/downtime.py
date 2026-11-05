"""When the system may use the card, read from the card itself and from its own work, nothing of
the machine it runs on (John, 2026-11-04: it must work for anyone who sets it up). Downtime is
the card with enough free memory and low utilisation for a few minutes running. Contention is
the system's own step rate falling far below what it was: a game or any other tenant on the card
stops a training run cold (2026-11-01, 2026-11-04), which is the one signal every machine shares.
"""

from __future__ import annotations

import statistics
import subprocess
import time
from dataclasses import dataclass


@dataclass
class Card:
    total_gib: float
    used_gib: float
    util: float  # percent

    @property
    def free_gib(self) -> float:
        return self.total_gib - self.used_gib


def card(run=subprocess.run) -> Card:
    """The first NVIDIA card as nvidia-smi reports it; torch's view where nvidia-smi is missing."""
    try:
        out = run(["nvidia-smi", "--query-gpu=memory.total,memory.used,utilization.gpu",
                   "--format=csv,noheader,nounits"], capture_output=True, text=True, check=True).stdout
        total, used, util = (float(x) for x in out.strip().splitlines()[0].split(","))
        return Card(total / 1024, used / 1024, util)
    except (OSError, subprocess.CalledProcessError, ValueError, IndexError):
        import torch

        free, total = torch.cuda.mem_get_info()
        return Card(total / 2**30, (total - free) / 2**30, 0.0)


def idle(need_gib: float, max_util: float = 15.0, minutes: float = 3.0, sample_s: float = 15.0,
         read=card, sleep=time.sleep) -> bool:
    """True when every sample over `minutes` had `need_gib` free and utilisation under `max_util`."""
    for _ in range(max(1, int(minutes * 60 / sample_s))):
        c = read()
        if c.free_gib < need_gib or c.util > max_util:
            return False
        sleep(sample_s)
    return True


class Rate:
    """The system's own progress, as (time, steps) beats: contended when the recent rate falls
    under `floor` of the run's typical rate, read over `window_s`."""

    def __init__(self, floor: float = 0.2, window_s: float = 300.0, warm: int = 5):
        self.floor, self.window_s, self.warm = floor, window_s, warm
        self.beats: list[tuple[float, int]] = []
        self.rates: list[float] = []

    def beat(self, t: float, steps: int) -> None:
        if self.beats and steps > self.beats[-1][1] and t > self.beats[-1][0]:
            self.rates.append((steps - self.beats[-1][1]) / (t - self.beats[-1][0]))
        self.beats.append((t, steps))

    def contended(self, now: float) -> bool:
        if len(self.rates) < self.warm or not self.beats:
            return False
        typical = statistics.median(self.rates)
        recent = [(t, s) for t, s in self.beats if t >= now - self.window_s]
        if now - self.beats[-1][0] < self.window_s and len(recent) >= 2:
            rate = (recent[-1][1] - recent[0][1]) / max(1e-9, recent[-1][0] - recent[0][0])
        else:
            # no beat for a whole window, or one beat in it: the rate since the last beat is at most one step a window
            rate = 1.0 / max(1e-9, now - self.beats[-1][0])
        return rate < self.floor * typical
