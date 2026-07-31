"""Stand up a driver and N nodes in containers, optionally over a bad link.

    python testbed/run.py --nodes 4
    python testbed/run.py --nodes 4 --delay 80ms --jitter 20ms --loss 2%
    python testbed/run.py --mode bucket --nodes 4 --delay 80ms

**This is what turns G2, G3 and G4 from modelled into measured.** Every latency,
jitter and churn result in this project so far comes from a model of a network.
Here there is an actual one, and `tc netem` sets what is wrong with it.

The first thing to establish is not a curve but an identity: with no impairment,
a network of containers must agree with the single-process model exactly. Until
that holds, every later number is measuring the harness.

Runs on Docker Desktop and on GitHub Actions runners alike -- both give root and
a real kernel, which is what `netem` needs. Verified on both.
"""

from __future__ import annotations

import argparse
import json
import subprocess
import sys
import time

IMAGE = "openplexus-testbed"
NETWORK = "openplexus-testbed"
DRIVER = "openplexus-driver"
PORT = 9999
#: Peer mode only. A different port from the driver's so a stray container
#: from the other mode cannot be answered by mistake.
ASKER = "openplexus-asker"
PEER_PORT = 9500
#: Bucket mode only. Another distinct port, for the same reason as `ASKER`'s:
#: a stray container from another mode must not be able to answer by mistake.
BUCKET_PORT = 9600


def run(command: list[str], **kwargs) -> subprocess.CompletedProcess:
    return subprocess.run(command, capture_output=True, text=True, **kwargs)


def impairment(delay: str | None, jitter: str | None, loss: str | None) -> str:
    """The `tc` command for this link, or an empty string for a clean one.

    Applied inside each node container before it joins. If `tc` fails the node
    exits rather than joining, because a node that quietly ran without the
    impairment would contribute a clean vote to a run labelled impaired -- which
    looks like a result and is not one.
    """
    if not (delay or loss):
        return ""
    parts = ["tc qdisc add dev eth0 root netem"]
    if delay:
        parts.append(f"delay {delay}" + (f" {jitter}" if jitter else ""))
    if loss:
        parts.append(f"loss {loss}")
    return " ".join(parts) + " || exit 3; "


def peer_mode(args) -> int:
    """Time a driver-free walk across peer containers under `tc netem`.

    **This is the measurement notes 094 and 101 both say has never been taken.**
    Their words: `tools/cluster_compose.py` *"can put peers in containers with
    `tc netem`, and has not been pointed at this"*, and the `tc netem` container
    harness *"has still never been pointed at the peer path"*. Every peer number
    in the project is loopback priced at an assumed 50 ms RTT.

    **Impairment is applied to the ASKER as well as the peers**, and that is not
    incidental. `netem delay` shapes egress, so delaying only the peers makes the
    request leg free and the reply leg slow -- half a link. Both sides get it, so
    a stated `--delay 80ms` is about 160 ms of round trip, and the tool reports
    milliseconds per ROUND rather than wall clock for that reason.

    **What this does not duplicate:** the walk, the reader and the round counter
    all already exist -- `openplexus/peer.py`, `search.beam`, and
    `RemoteConcepts.rounds`. `tools/peer_walk_timing.py` is the timing loop and
    already runs in process. This is the container and impairment wiring, which
    is the one part that was missing, and it reuses this file's own image, network
    and `impairment()` rather than standing up a second harness.
    """
    print(f"building {IMAGE} ...", flush=True, file=sys.stderr)
    built = run(["docker", "build", "-q", "-f", "testbed/Dockerfile",
                 "-t", IMAGE, "."])
    if built.returncode:
        print(built.stderr, file=sys.stderr)
        return 1

    run(["docker", "network", "create", NETWORK])
    names = [f"openplexus-peer-{i}" for i in range(args.nodes)]
    run(["docker", "rm", "-f", ASKER, *names])

    tc = impairment(args.delay, args.jitter, args.loss)
    shared = ["-e", "OPENPLEXUS_MODE=peer",
              "-e", f"OPENPLEXUS_PEER_PORT={PEER_PORT}",
              "-e", f"OPENPLEXUS_D_MODEL={args.width}",
              "-e", f"OPENPLEXUS_NODES={args.nodes}",
              "-e", "OPENPLEXUS_VOCAB_SIZE=40",
              "-e", "OPENPLEXUS_CONTEXT_KEYS=1"]
    for index, name in enumerate(names):
        launched = run(["docker", "run", "-d", "--name", name,
                        "--network", NETWORK, "--cap-add=NET_ADMIN", *shared,
                        "-e", f"OPENPLEXUS_NODE_INDEX={index}",
                        IMAGE, "sh", "-c",
                        f"{tc}python -m openplexus.node_main"])
        if launched.returncode:
            print(launched.stderr, file=sys.stderr)
            return 1

    time.sleep(3)          # peers bind before serving; give them the bind
    peers = ",".join(f"{name}:{PEER_PORT}" for name in names)
    depths = " ".join(str(d) for d in args.depths)
    asked = run(["docker", "run", "--name", ASKER, "--network", NETWORK,
                 "--cap-add=NET_ADMIN", IMAGE, "sh", "-c",
                 f"{tc}python tools/peer_walk_timing.py --peers {peers} "
                 f"--depths {depths} --width {args.width} --nodes {args.nodes}"])
    print(asked.stdout or asked.stderr, file=sys.stderr)
    for name in names:
        logs = run(["docker", "logs", name]).stdout.strip().splitlines()
        if logs:
            print(f"  {name}: {logs[0]}", file=sys.stderr)
    if not args.keep:
        run(["docker", "rm", "-f", ASKER, *names])
    return asked.returncode


def bucket_mode(args) -> int:
    """Run the GROUNDING store across containers under `tc netem`.

    **The last untested half of the grounding line.** `g32` and `g33` measured
    the statistic, the join, the bound and the sharding; every one of them
    counted crossings rather than sending them, and `openplexus/buckets.py` says
    so in its own docstring. This sends them, between containers, over a link
    with whatever `netem` was asked for.

    The property asserted is note 014's, unchanged: **a network of containers
    must agree with the single-process model EXACTLY.** Not approximately, and
    not on average — a count is an integer and a wrong one is a bug.

    Impairment goes on the peers AND the driver, for the reason `peer_mode`
    gives: `netem delay` shapes egress, so delaying one side makes half a link.

    **What this does not duplicate.** The image, the network and `impairment()`
    are this file's own and are shared with the other two modes;
    `openplexus/node_main.py` is the launcher; `tools/bucket_drive.py` is the
    driver. This is the container wiring and nothing else.
    """
    print(f"building {IMAGE} ...", flush=True, file=sys.stderr)
    built = run(["docker", "build", "-q", "-f", "testbed/Dockerfile",
                 "-t", IMAGE, "."])
    if built.returncode:
        print(built.stderr, file=sys.stderr)
        return 1

    run(["docker", "network", "create", NETWORK])
    names = [f"openplexus-bucket-{i}" for i in range(args.nodes)]
    run(["docker", "rm", "-f", ASKER, *names])

    tc = impairment(args.delay, args.jitter, args.loss)
    table = ",".join(f"{i}={name}:{BUCKET_PORT}"
                     for i, name in enumerate(names))
    shared = ["-e", "OPENPLEXUS_MODE=bucket",
              "-e", f"OPENPLEXUS_PEER_PORT={BUCKET_PORT}",
              "-e", f"OPENPLEXUS_NODES={args.nodes}",
              "-e", f"OPENPLEXUS_BUCKET_WIDTH={args.bucket_width}",
              "-e", f"OPENPLEXUS_BUCKET_PEERS={table}"]
    for index, name in enumerate(names):
        launched = run(["docker", "run", "-d", "--name", name,
                        "--network", NETWORK, "--cap-add=NET_ADMIN", *shared,
                        "-e", f"OPENPLEXUS_NODE_INDEX={index}",
                        IMAGE, "sh", "-c",
                        f"{tc}python -m openplexus.node_main"])
        if launched.returncode:
            print(launched.stderr, file=sys.stderr)
            return 1

    time.sleep(3)          # peers bind before serving; give them the bind
    peers = ",".join(f"{name}:{BUCKET_PORT}" for name in names)
    asked = run(["docker", "run", "--name", ASKER, "--network", NETWORK,
                 "--cap-add=NET_ADMIN", IMAGE, "sh", "-c",
                 f"{tc}python tools/bucket_drive.py --peers {peers} "
                 f"--width {args.bucket_width} --concepts {args.concepts} "
                 f"--occasions {args.occasions}"
                 + (" --walk" if args.walk else "")])
    print(asked.stdout, flush=True)
    print(asked.stderr, file=sys.stderr)
    for name in names:
        logs = run(["docker", "logs", name]).stdout.strip().splitlines()
        if logs:
            print(f"  {name}: {logs[0]}", file=sys.stderr)
        # A node prints its failed forwards on the way out. A count that is
        # short because a write never landed reads as a weaker signal, so the
        # log is checked rather than the number being trusted alone.
        for line in logs:
            if "FAILED" in line:
                print(f"  {name}: {line}", file=sys.stderr)
    if not args.keep:
        run(["docker", "rm", "-f", ASKER, *names])
    return asked.returncode


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--nodes", type=int, default=2)
    parser.add_argument("--width", type=int, default=16)
    parser.add_argument("--steps", type=int, default=60)
    parser.add_argument("--window", type=int, default=1)
    parser.add_argument("--delay", default=None, help="e.g. 80ms")
    parser.add_argument("--jitter", default=None, help="e.g. 20ms")
    parser.add_argument("--loss", default=None, help="e.g. 2%%")
    parser.add_argument("--absent", default=None,
                        help="comma-separated node indices that stop answering")
    parser.add_argument("--leave-at", type=int, default=0,
                        help="step at which those nodes go silent")
    parser.add_argument("--keep", action="store_true",
                        help="leave containers behind for inspection")
    parser.add_argument("--bucket-width", type=int, default=50,
                        help="bucket mode only: the join's window")
    parser.add_argument("--concepts", type=int, default=6,
                        help="bucket mode only: how many concepts the stream has")
    parser.add_argument("--occasions", type=int, default=60,
                        help="bucket mode only: how many moments to drive")
    parser.add_argument("--walk", action="store_true",
                        help="bucket mode only: also time the READ path")
    parser.add_argument("--mode", choices=("slice", "peer", "bucket"),
                        default="slice",
                        help="slice: the driver-based DIMENSION path, which every "
                             "netem result to date used. peer: point-to-point "
                             "concept reads with no driver, which notes 094 and "
                             "101 both record as never having been run here")
    parser.add_argument("--depths", type=int, nargs="+", default=[1, 2, 3, 5],
                        help="peer mode only: walk depths to time")
    args = parser.parse_args()

    if args.mode == "peer":
        return peer_mode(args)
    if args.mode == "bucket":
        return bucket_mode(args)

    if args.width % args.nodes:
        # slices_for refuses uneven splits; say so here rather than letting the
        # driver fail after the images are built.
        print(f"{args.nodes} nodes do not divide a width of {args.width}",
              file=sys.stderr)
        return 2

    # PROGRESS GOES TO STDERR. This script's stdout is a JSON document and a
    # caller pipes it to a file -- g12-04 did exactly that and produced six
    # artifacts that were not valid JSON, because two progress lines were
    # sitting above the object. The aggregation could not read its own results.
    #
    # A program whose stdout is a data format has no business printing anything
    # else there.
    print(f"building {IMAGE} ...", flush=True, file=sys.stderr)
    built = run(["docker", "build", "-q", "-f", "testbed/Dockerfile",
                 "-t", IMAGE, "."])
    if built.returncode:
        print(built.stderr, file=sys.stderr)
        return 1

    run(["docker", "network", "create", NETWORK])       # fine if it exists
    cleanup = [DRIVER] + [f"openplexus-node-{i}" for i in range(args.nodes)]
    run(["docker", "rm", "-f", *cleanup])

    shared = ["-e", f"OPENPLEXUS_D_MODEL={args.width}",
              "-e", f"OPENPLEXUS_NODES={args.nodes}",
              "-e", f"OPENPLEXUS_DRIVER_PORT={PORT}",
              "-e", f"OPENPLEXUS_STEPS={args.steps}",
              "-e", f"OPENPLEXUS_WINDOW={args.window}",
              "-e", f"OPENPLEXUS_ABSENT={args.absent or ''}",
              "-e", f"OPENPLEXUS_LEAVE_AT={args.leave_at}"]

    started = run(["docker", "run", "-d", "--name", DRIVER,
                   "--network", NETWORK, *shared,
                   IMAGE, "python", "testbed/driver.py"])
    if started.returncode:
        print(started.stderr, file=sys.stderr)
        return 1

    tc = impairment(args.delay, args.jitter, args.loss)
    for index in range(args.nodes):
        command = (f"{tc}python -m openplexus.node_main")
        launched = run(["docker", "run", "-d", "--name", f"openplexus-node-{index}",
                        "--network", NETWORK, "--cap-add=NET_ADMIN", *shared,
                        "-e", f"OPENPLEXUS_NODE_INDEX={index}",
                        "-e", f"OPENPLEXUS_DRIVER_HOST={DRIVER}",
                        "-e", "OPENPLEXUS_DECODER=1",
                        IMAGE, "sh", "-c", command])
        if launched.returncode:
            print(launched.stderr, file=sys.stderr)
            return 1

    print("waiting for the driver ...", flush=True, file=sys.stderr)
    deadline = time.monotonic() + 180
    result = None
    while time.monotonic() < deadline:
        state = run(["docker", "inspect", "-f", "{{.State.Running}}", DRIVER])
        logs = run(["docker", "logs", DRIVER])
        for line in logs.stdout.splitlines():
            if line.startswith("{"):
                result = json.loads(line)
                break
        if result or state.stdout.strip() == "false":
            break
        time.sleep(1)

    if result is None:
        print("driver produced no result", file=sys.stderr)
        print(run(["docker", "logs", DRIVER]).stdout[-2000:], file=sys.stderr)
        for index in range(args.nodes):
            print(run(["docker", "logs",
                       f"openplexus-node-{index}"]).stdout[-500:], file=sys.stderr)
        if not args.keep:
            run(["docker", "rm", "-f", *cleanup])
        return 1

    result["delay"] = args.delay
    result["jitter"] = args.jitter
    result["loss"] = args.loss
    print(json.dumps(result, indent=1))
    if not args.keep:
        run(["docker", "rm", "-f", *cleanup])
    return verdict(result)


def verdict(result: dict) -> int:
    """0 if the run met the bar that applies to it, 1 otherwise.

    **A run WITH a departure is not required to agree**, and demanding that it
    does is demanding the wrong thing: losing a quarter of the store's
    dimensions should change later answers. What it must not do is diverge
    BEFORE the departure step -- a machine switching off cannot reach back and
    change an answer already given.

    `testbed/driver.py` has always applied that rule. This file did not: it
    returned 1 whenever `agrees_with_one_process` was false, so **every churn
    run reported failure for behaving correctly**, and g12-02's first two
    dispatches lost all eighteen cells to it.

    The reason it took two dispatches to find: the local check ran
    `run.py ... | tail`, and a pipeline reports the LAST command's status, so
    the JSON looked right and the exit code was `tail`'s. That is the same
    masking as the `tee` pipelines fixed earlier today, in a file that had no
    `pipefail` to fix.
    """
    if result.get("leave_at"):
        return 0 if result["mismatches_before_departure"] == 0 else 1
    return 0 if result["agrees_with_one_process"] else 1


if __name__ == "__main__":
    raise SystemExit(main())
