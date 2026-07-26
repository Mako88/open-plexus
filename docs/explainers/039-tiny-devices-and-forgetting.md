# 039 — Do tiny devices forget?

## The objection this had to answer

We found that devices holding **one number each** can do the job, in groups of a
few dozen. Good news — but for a single body of data.

Real devices see more than one. So the obvious objection: *if a tiny device learns
one thing and then meets something different, does it keep the first?* If not, all
the tiny-device results are worthless, because no real device only ever sees one
kind of data.

We already knew big models forget badly below a certain size. What nobody had
separated is **which** size matters: the size of the whole network, or the size of
one device.

Those point opposite ways. If it's the device, then a network of tiny devices
forgets catastrophically and the whole idea fails. If it's the network, tiny
devices are fine.

## The answer is the good one

Every device holds one number. What changes is how many are read together:

| devices read together | keeps of what it learned |
|---|---|
| 1 | **0%** |
| 8 | 2% |
| 32 | 20% |
| 64 | 33% |
| 240 | **65%** |

**Nothing about the devices changed.** Only how many were consulted at once. A
lone device keeps nothing; a group keeps most of it.

So what survives a switch to different data doesn't live *in* any device. It lives
in the **pattern across** them — and no single one of them has to be big enough to
hold anything.

With selective storage switched on as well, a group of 32 stops forgetting
entirely.

## What I got wrong, three times over

I predicted all three of these and all three were wrong:

- **"Small devices will forget everything."** My reasoning: a device with one
  number has one thing to overwrite and nowhere to keep anything else. That's true
  of the *device* and false of the *group*.
- **"Grouping won't help — you'd just be pooling devices that have each been
  wiped."** Wrong for the same reason.
- **"Selective storage won't help either, because forgetting happens in a
  different part of the system than storage does."** That argument is actually
  sound, and the conclusion is still wrong: selective storage changes *what the
  learning is trained on*, so the two tasks end up using different parts and
  overlap less. The effect reaches further than where it acts.

All three were wrong in the direction I wanted, which is why the write-up spends
as much space on doubting them as on reporting them.

## The doubts, spelled out

- **The best number sits at a ceiling.** With selective storage and a big group,
  the score is perfect before *and* after — so "no forgetting" is partly "nothing
  was hard enough to lose". The honest version of the result is the other column,
  where a group still loses about a third.
- **A tuning setting hit the edge of what we tried**, so every number here is
  probably a bit low.
- **The single most important cell varies from 0.44 to 0.64 across three runs.**
  That's a wide range to rest a claim on.
- **One switch, between two completely unrelated datasets.** Real life is many
  switches with overlapping content, and losses may pile up.

## Where it leaves things

The objection is answered: **tiny devices don't have to be forgetful, as long as
they're read in company.** That was the last obvious thing standing between the
tiny-device results and being worth taking seriously.
