092 — The memory loop composes, and cannot tell which side is wrong
==================================================================

**Status:** measured, gate passed, and it confirms note 068's warning with a symmetry sharp
enough to be the finding. **The four pieces do compose. What they cannot do is adjudicate.**

---

## IN PLAIN TERMS

Four mechanisms had been measured separately and never run together: noticing that two
answers disagree, working out which stored fact is to blame, copying what proved right into
a store that does not fade, and discarding whatever has gone longest unused.

**Assembled, they repair damage completely** — 30% of facts corrupted, and after six passes
the recall is back to 100%, with the disagreements it keeps finding falling from 115 to 20 as
it converges rather than oscillating.

**But that only holds while the reasoning is sound and the memory is at fault.** Break the
reasoning instead and the loop confidently overwrites correct memories with wrong
conclusions — **and the damage is exactly the same size.** It has no way to tell the two
situations apart, because from the inside they look identical: two answers disagree, and
nothing says which to keep.

---

## The measurements

Note 080's fixture: state `a-r1->b`, `b-r2->c` and also `a-r3->c`, so `key(a, r3)` is both
readable and derivable. 60 triangles, 6 rounds, width 256, 5 seeds.

    condition                              recall   blamed
    nothing corrupted, detect only          1.000      9.6
    nothing corrupted, detect + REPAIR      1.000      1.8
    30% direct facts corrupted, detect      0.697    115.2
    30% direct facts corrupted, + REPAIR    1.000     19.6

**GATE passes: repair damages nothing when nothing is wrong**, and it *lowers* spurious
blame from 9.6 to 1.8 by settling the small disagreements interference produces.

**And it converges.** Blame falls from 115 to 20 once the corruption is repaired, so the loop
stops finding contradictions rather than cycling through them.

## The symmetry, which is the finding

    what is corrupted   fraction   detect only   + REPAIR
    direct fact              0.3         0.697      1.000
    derivation PATH          0.3         1.000      0.697

**Identical corruption, relocated, and repair moves the damage to whichever side it does not
trust.** Repairing from the derivation assumes the derivation is right. When it is, the loop
is perfect; when it is not, the loop is precisely as destructive.

> **Note 068 wrote this down before anything was built:** *"a wrong derived fact becomes a
> premise, so the gate must decide what gets written back."* This is that, measured. The
> warning was right and the mechanism it warned about is the one assembled here.

## What is missing, and it is one thing

**Redundancy.** A contradiction between a derivation and a read is a two-way disagreement
with no majority. A contradiction between **two independent derivations** and a read is a
three-way vote, and the odd one out is the suspect.

That is the direction note 080's mechanism already points: it detects disagreement between
*two routes to the same address*, and nothing stops there being three. **Untried**, and it is
the next piece rather than a tuning knob.

Two weaker alternatives, both worse and both worth naming so they are not rediscovered:

    trust the direct fact always     = "detect only", which is 1.000 when the path
                                       is wrong and 0.697 when the memory is. It
                                       trades one failure for the other rather than
                                       resolving anything
    trust the more-confirmed side    blame counts as a reputation. Plausible, and it
                                       needs a history the current loop does not keep

## What is NOT claimed

**Not that the pieces are wired into the model.** This is a standalone loop over an
associative store, exercising the mechanisms notes 079, 080, 082 and 083 measured. `run()`
does not call any of it.

**Not that 30% is a realistic corruption rate**, nor that corruption arrives as a clean
one-or-the-other split. Real damage would hit both sides, and the symmetric result says the
loop's behaviour there is a mixture nobody has measured.

**And the repair itself is crude:** subtract the old value at the key, add the derived one.
That is a write, not a learning rule, and it assumes the binding can be cleanly removed —
which a superposed store only approximately allows.
