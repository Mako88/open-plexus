"""Putting a length on the front of a message, so a socket read knows when to stop.

TCP is a byte stream with no notion of a message. Without a frame, a reader
cannot tell a short read from the end of a message, and the failure is silent:
the receiver gets half a payload and parses it as a whole one.

Four bytes of big-endian length, then the payload. That is all.

## Why a partial read RAISES rather than returning what arrived

A peer that closes mid-message has not departed politely — it has left a reader
holding a fragment. Returning the fragment would let a truncated payload be
parsed as a complete one, which is the same class of failure as a lost message
read as a zero. Departure is detected by the transport's own timeout; a partial
message is a bug and says so.

## What this does NOT duplicate, and what was searched

Searched by capability — frame, length prefix, send, receive, recv — across
`openplexus/` and `testbed/`.

- **Extracted from the previous `openplexus/distributed.py`**, which is 722 lines
  of a distributed computation this architecture no longer performs. These 25
  lines were the only part `bucket_peer` needed, and carrying the other 700 to
  keep them would have been keeping a whole architecture for a struct.
- **Nothing else frames.** `bucket_peer` is the only transport, and it calls
  these rather than rolling its own.
"""

from __future__ import annotations

import socket
import struct

#: Four bytes, big-endian, unsigned. Big-endian because it is network order and
#: an on-the-wire format that varies by host is a bug waiting for a mixed fleet.
_HEADER = struct.Struct("!I")


def send(sock: socket.socket, payload: bytes) -> None:
    """Write one framed message. `sendall` because a partial write is a bug."""
    sock.sendall(_HEADER.pack(len(payload)) + payload)


def receive(sock: socket.socket) -> bytes:
    """Read one framed message, or raise if the peer went away mid-message."""
    header = _read_exactly(sock, _HEADER.size)
    (length,) = _HEADER.unpack(header)
    return _read_exactly(sock, length)


def _read_exactly(sock: socket.socket, count: int) -> bytes:
    chunks, seen = [], 0
    while seen < count:
        chunk = sock.recv(count - seen)
        if not chunk:
            raise ConnectionError(
                f"peer closed after {seen} of {count} bytes -- a partial "
                f"message is not a departure, it is a bug")
        chunks.append(chunk)
        seen += len(chunk)
    return b"".join(chunks)
