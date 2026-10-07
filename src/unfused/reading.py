"""What every script names a reading by."""

from datetime import UTC, datetime


def utc_stamp() -> str:
    """Now, as the stamp in a reading's file name: UTC, to the second."""
    return datetime.now(UTC).strftime("%Y%m%dT%H%M%SZ")
