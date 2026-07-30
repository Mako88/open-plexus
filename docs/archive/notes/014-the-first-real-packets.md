# Note 014 — The first real packets, and what lock-step costs

Every latency, jitter and churn claim in this project has come from a *model* of
a network. `distributed.py` opened real sockets, but on loopback, where there is
no delay to survive — its own docstring says delay and loss "come after, and are
meaningless until this holds".

This is after. Nodes now run in containers on a Docker bridge with `tc netem`
setting what is wrong with the link, and the first two questions have answers.

## 1. Correctness survives a bad link

Four nodes, width 16, 40 steps, **80 ms delay ± 20 ms jitter, 2% loss**:

    agrees_with_one_process   true
    mismatches                0

Bit-identical to the single-process model, over a link losing one packet in
fifty. That is not a surprise — TCP retransmits, and votes carry their own step
index so late arrivals are reassembled rather than misfiled — but it had never
been observed, and both of those mechanisms were until now arguments rather than
measurements.

**The driver reports agreement, not just accuracy.** Under an impaired link a
network can be slow or it can be wrong, and a run that merely finished does not
distinguish them. Agreement is the only field that does.

## 2. Lock-step costs a round trip per token, and the window removes it

Same four nodes, same impaired link, varying only how far ahead the driver may
run:

    window   seconds/step   agrees
         1        0.12472     true
         8        0.01714     true

**7.3× faster, and still exact.**

At window 1 every node must answer before anyone moves, so each token costs a
full round trip — and 0.125 s/step is about what an 80 ms link with 20 ms of
jitter and some retransmission should cost. At window 8, eight of those round
trips are in flight at once and the link stops being the bottleneck.

This matters more than a speed number, because **window 1 is precisely the
global synchronisation C1 forbids.** Bounded asynchrony was argued for as a
constraint the design had to satisfy. It turns out to also be the thing that
makes the design usable at all: on this link, obeying C1 is worth 7.3×.

For scale, the clean-link run is 0.00048 s/step, so an 80 ms link costs 260× at
lock-step and 36× with a window of 8. The remaining gap is the link, not the
protocol.

## What this does NOT establish

**These are single runs.** One measurement per configuration, no repeats, no
seeds, no error bars. Timing is the noisiest thing this project has ever
measured and every other result here reports per-seed values. **The 7.3× is an
observation, not a measured effect**, and it needs a proper repeated sweep before
it goes in GOALS. It is recorded here rather than there for exactly that reason.

**The topology is a Docker bridge, not the internet.** No NAT, no asymmetric
routes, no congestion from anything else, and `netem` is applied to each node's
egress only — so the driver's packets are undelayed and the round trip is
one-way-impaired. A real link is worse in ways this cannot show.

**It is very small.** Width 16, four nodes, 40 steps. Nothing here says what
happens at the widths and cluster sizes the tiny-node results are about.

**Churn is untested here.** `absent` and `leave_at` work in-process, and the
slice handshake that makes them mean anything under a real network was only just
fixed — see the connection-order tests. Departure over an impaired link is the
obvious next measurement and has not been made.

## Why the fix that preceded this mattered

The driver used to index connections by arrival order while a comment claimed it
asked each node its identity. On loopback, arrival order is spawn order and
usually right.

**Under 80 ms of delay and 20 ms of jitter, arrival order is whatever the link
decides.** Every departure measurement made on this testbed would have removed a
node chosen at random and reported it as node 0 — completing normally, producing
a plausible number, and being wrong in a way nothing here would have caught.
