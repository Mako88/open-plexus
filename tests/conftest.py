"""The pushback list prints on every run, whatever pytest's capture is doing.

A FILE NOBODY READS IS NOT A STANDING OBJECTION. `tests/pushback.py` asserts its
own count, which stops one being dropped silently -- but a green test is
invisible by design, and an objection nobody sees is an objection nobody
settles. The terminal-summary hook runs outside capture, so the list arrives in
front of whoever ran the suite, every time.
"""

from __future__ import annotations


def pytest_terminal_summary(terminalreporter, exitstatus, config):
    from pushback import report

    terminalreporter.write_sep("=", "pushback")
    terminalreporter.write_line(report())
