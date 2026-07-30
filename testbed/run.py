"""Stand up a driver and N nodes in containers, optionally over a bad link.

    python testbed/run.py --nodes 4
    python testbed/run.py --nodes 4 --delay 80ms --jitter 20ms --loss 2%

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
    parser.add_argument("--mode", choices=("slice", "peer"), default="slice",
                        help="slice: the driver-based DIMENSION path, which every "
                             "netem result to date used. peer: point-to-point "
                             "concept reads with no driver, which notes 094 and "
                             "101 both record as never having been run here")
    parser.add_argument("--depths", type=int, nargs="+", default=[1, 2, 3, 5],
                        help="peer mode only: walk depths to time")
    args = parser.parse_args()

    if args.mode == "peer":
        return peer_mode(args)

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
