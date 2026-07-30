# 044 — Which reads can be routed, and which cannot

**IN PLAIN TERMS.** If each machine only holds some of the concepts, then before
you can look something up you have to know *which machine to ask*. It turns out
one part of the model can say what it is asking for, and another part genuinely
cannot — it asks a blurred question that is a bit of every concept at once. That
part cannot be spread across machines at all, in its current form.

---

## The question this answers

Item 0b of STATE.md is wiring `ConceptStore` into the model. Everything measured
about it so far is a property of the store — capacity, balance, survival. The
falsifier that matters is whether the model can still learn through it, and
attempting the wiring surfaced a blocker before a line of it was written.

## Routing needs a concept id, and a key vector cannot supply one

`ConceptStore.read(concept, key)` takes both. The key vector says *what to
retrieve*; the concept id says *whom to ask*. A key vector cannot be inverted to
a concept — with random keys there is no inversion, and with content-derived keys
(item 0c) it would be approximate at best.

So every read site in the model has to be able to name what it is asking for.
Going through them:

| read site | key it uses | routable? |
|---|---|---|
| the ordinary retrieval, `local_memory.py:2148` | `key`, from token `t` | **yes** — the token id is right there |
| the corrective write, `:1897` | `previous_key`, from token `t-1` | **yes** |
| the un-reward subtraction, `:2005` | `key_written`, kept in `pending` | **yes**, if the id is kept beside it |
| **the hop, `:2317`** | `hop_key = weights @ self.wk` | **NO** |
| the search walk, `search.py:175` | `keys.pair(fact_token, current)` | **yes** |

## The hop is the one that cannot, and the reason is its whole design

    weights = np.exp(pooled); weights /= weights.sum()
    hop_key = weights @ self.wk

**`hop_key` is a softmax mixture of every token's key row.** It is deliberately
soft — the comment at `:2312` says so directly: *"a hard decode gives the next hop
no gradient of confidence, so a wrong first hop is silently asserted rather than
hedged."* That was the right call for accuracy on one machine.

It is also, exactly, a question that names no concept. Under concept
partitioning there is no node to send it to. The options are all bad:

- **Ask every node.** That is the collective amended C1 forbids, and it would
  make the hop the most expensive operation in the model rather than a cheap one.
- **Ask the top-weighted node.** That is a hard decode wearing a soft one's
  clothes; it pays the softmax's cost and gets the argmax's behaviour.
- **Ask the top `b` nodes and mix what comes back.** Sound, and it is `b` times
  the traffic — which is search's cost profile without search's disambiguation.

## And the search walk already solved it, for a different reason

`walk_from` decodes to a **hard token** at every step and rebuilds the key from
it (`search.py:172-175`). Decision 123 built that for accuracy: a branch that
commits can be *scored* against the target, where a blur cannot.

**That same commitment is what makes the walk routable.** Two mechanisms argued
for on unrelated grounds — accuracy on kinship, and locality across nodes — pick
out the same design. That is the strongest kind of agreement available here,
because neither was chosen with the other in mind.

> **So concept partitioning does not need the hop; it needs the walk.** The hop
> and the walk are alternatives for the same job (decision 130 measured the walk
> at +0.269 over concat), and the walk already won on accuracy. This says it also
> wins on locality, and that the soft hop is a single-machine mechanism.

Per the standing agreement both stay in the tree, swappable. This is a statement
about which one a *partitioned* model can use, not a deletion.

## The tension worth naming, because it points at item 0c

Partitioning **removes** interference: a read for concept `c` no longer sums over
bindings on other nodes. With random keys that is pure gain, since a key retrieves
nothing but its own binding anyway.

**With content-derived keys it would not be.** The point of similar concepts
landing on nearby keys is that a query can retrieve a *related* binding — and
routing sends the query to one node, which is precisely where the related
concepts are not. **Items 0b and 0c pull against each other**, and neither note
had noticed.

The resolution is probably that ownership should be derived from the same content
the keys are, so similar concepts land on the same node — which is what the hash
ring deliberately does *not* do, because it spreads for balance. That is a real
design question and it is unanswered here.

## What this does not do

No measurement. This is a reading of the code, and its claims are checkable by
reading the same lines. The learn-through falsifier is still unrun; what changed
is that it now has a defined scope — **partitioned reads are routable everywhere
except the soft hop**, so the falsifier runs against the walk.
