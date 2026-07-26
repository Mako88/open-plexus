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


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--nodes", type=int, default=2)
    parser.add_argument("--width", type=int, default=16)
    parser.add_argument("--steps", type=int, default=60)
    parser.add_argument("--window", type=int, default=1)
    parser.add_argument("--delay", default=None, help="e.g. 80ms")
    parser.add_argument("--jitter", default=None, help="e.g. 20ms")
    parser.add_argument("--loss", default=None, help="e.g. 2%%")
    parser.add_argument("--keep", action="store_true",
                        help="leave containers behind for inspection")
    args = parser.parse_args()

    if args.width % args.nodes:
        # slices_for refuses uneven splits; say so here rather than letting the
        # driver fail after the images are built.
        print(f"{args.nodes} nodes do not divide a width of {args.width}",
              file=sys.stderr)
        return 2

    print(f"building {IMAGE} ...", flush=True)
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
              "-e", f"OPENPLEXUS_WINDOW={args.window}"]

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

    print("waiting for the driver ...", flush=True)
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
    return 0 if result["agrees_with_one_process"] else 1


if __name__ == "__main__":
    raise SystemExit(main())
