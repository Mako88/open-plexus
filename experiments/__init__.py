"""Experiment scripts. A package only so one sweep can import another rather
than copy it -- `tools/check_duplication.py` refuses the copy, and the audit owns
the ranking machinery every FB15k run has to share.

`tools/check_imports.py` deliberately does not scan this package: several scripts
here do work at import time and loading them all would turn a one-second gate
into a sweep.
"""
