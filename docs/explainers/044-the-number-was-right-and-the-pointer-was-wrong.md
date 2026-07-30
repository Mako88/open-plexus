# 44. Every number was right. The pointer was wrong.

This project has a rule that every claim has to say where its evidence came from. We
follow it. Every result in our records carries a citation — "see note 81", "see decision
134" — so anyone can go and check.

Today we built a tool that actually **follows** those citations, and pointed it at our own
files.

## What it does

It is not clever. For each result we have written down, it takes the numbers out of the
sentence, opens the file the sentence says they came from, and checks whether the numbers
are in there.

That is the whole tool. It took an hour to write.

## What it found on its first run

We were about to move a large amount of measurement text from one document into eighty-five
new ones. Copying numbers by hand is exactly where a digit gets changed and nobody notices,
so the tool was built first as insurance against that.

It never caught a mistyped number. **It caught seven citations that pointed at the wrong
place**, including this one:

> Splitting the memory across four machines makes the model more accurate — 0.9220 against
> 0.8877 — see note 81.

Note 81 does not contain those numbers. It does not mention the experiment. It is about
something else entirely.

Two other documents quoted the same result. One credited a different source, which also
does not contain it. The other credited "note 81's companion measurement" — and said, in
its own text, that it had found the claim **in the summary document** rather than in the
note it went on to cite.

So the summary pointed at the note, the note pointed back at the summary, and **neither one
held the actual experiment.**

## The number was real

We re-ran it. Seventy seconds. It came back at 0.9220 — the same figure, to four decimal
places.

Nothing was made up. The experiment happened, it gave that answer, and somebody simply
never wrote down where. Everything downstream of it is fine.

## Why this is the interesting kind of mistake

Think about what would have caught it.

Not re-reading the numbers — every number was correct. Not checking the arithmetic — the
arithmetic was right. Not a second opinion on the conclusion — the conclusion was right
too. Not our test suite, which tests code and not documents.

The only thing that finds a broken pointer is **following the pointer**, and following
pointers is exactly the work a person skips when everything looks fine. Eight documents
were written after this one, by someone with every reason to check, and none of them did.

This is a general shape and we have now hit it three times:

- A headline result about text was carried for weeks. It appeared in exactly one scratch
  document, with no experiment behind it, and turned out to describe a different method
  entirely.
- A batch of experiments survived four rounds of review because every arm was wrong in
  the same way — internally consistent, nothing contradicting anything else. What caught
  it was a number from outside that system.
- And now this one.

**A closed loop agrees with itself.** You cannot get out of it by looking harder from
inside.

## What changed

Every result we record now has to say two separate things: **where the number is written
down**, and **what program produced it**. Those sound like the same question and they are
not. In this case the first was wrong in two documents and the second would have re-run
the experiment in seventy seconds and ended the argument on the spot.

Both fields are now required, and a check refuses to accept a record without them. Not a
guideline — a check, because we already had the guideline and it is the guideline that
failed.

## The uncomfortable part

We found this by building the tool. We did not find it by being careful, and we had been
careful — the citation rule exists precisely because this project takes evidence seriously,
and it is followed conscientiously.

Being careful produces a citation. It does not produce a *correct* citation, because
nothing about writing one tells you whether it resolves. Those are different activities,
and only one of them can be automated.

So the honest summary is: the discipline worked as designed and was not sufficient, the
tool that closed the gap was trivial, and the reason it did not exist earlier is that
nobody had noticed the gap was there.
