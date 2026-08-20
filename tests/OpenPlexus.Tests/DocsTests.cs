using System.Text;
using System.Text.RegularExpressions;

namespace OpenPlexus.Tests;

/// <summary>
/// The doc, checked against the code that is actually there — and against a size
/// budget.
/// </summary>
/// <remarks>
/// <para>
/// <b>JOHN'S CALL, 2026-08-03: the docs got too big to load, so they stopped
/// being read.</b> `architecture.md` reached 1,646 lines and `design.md` 756, and
/// a doc nobody opens is worse than no doc because it still gets cited. Both were
/// deleted; git holds them.
/// </para>
/// <para>
/// <b>What every piece does now lives</b> in the XML comments beside the code, and
/// the COMPILER enforces those. `GenerateDocumentationFile` is on, so a
/// `param` naming an argument that does not exist (CS1572/1573) or a `cref`
/// pointing at a deleted type (CS1574) fails the build. That check cannot go
/// stale, which no markdown file can promise. It found five ghost references to
/// types deleted weeks earlier on the day it was switched on.
/// </para>
/// <para>
/// <b>And it was on for the library only</b>, which meant the tests rotted freely. The
/// sentence above was written about one project and read as being about the tree, so a test
/// citing a deleted type compiled quietly — five files went on explaining themselves in
/// terms of two vote arms that had been removed. It is on for this project now, with
/// CS1591 muted so the rule is <i>what a comment SAYS must resolve</i> rather than <i>every
/// member must have one</i>. Switching it on found twenty-six: six crefs into things that
/// no longer exist, and twenty param tags that had drifted off their signatures.
/// </para>
/// <para>
/// <b>So this file is down to what a compiler cannot check.</b> Is the one remaining
/// doc still small, and do the fork numbers the code cites still resolve.
/// </para>
/// </remarks>
public sealed class DocsTests
{
    /// <summary>
    /// The most words ONE item may spend. <b>The cap that replaced the doc-wide
    /// one</b> — see the test that reads it.
    /// </summary>
    /// <remarks>
    /// <b>Raised from 45 on 2026-08-11</b>, and by what it cost rather than by feel. John's
    /// rule for this budget is that it must stop a doc growing without ever stopping
    /// information getting written down, and that day it did the second thing: a fork row
    /// carrying a closed finding and a new design decision would not fit, so <i>R scales
    /// with load</i> — John's own, from the fork he wrote — was deleted to afford the
    /// sentence beside it. A cap that is paid for by deleting content is a cap set wrong.
    /// Sixty still refuses the essay the rule was written against; eleven items were 38% of
    /// the doc when it was, and none of them was near this.
    /// </remarks>
    private const int Item = 60;

    /// <summary>
    /// The most words the WHOLE doc may spend. <b>A ratchet with one exception, and John
    /// wrote the exception.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>John's test, 2026-08-12, and it is the one that decides.</b> If it is long enough
    /// that you would hesitate to load all of it, it is too long. The whole point of collapsing
    /// several docs into one was that a session could read the plan WHOLE before starting. A
    /// doc read in pages is the pile of docs it replaced, wearing one filename.
    /// </para>
    /// <para>
    /// <b>And the per-item cap is why this was needed.</b> Which is a correction to the comment
    /// above it. Capping the item and not the doc fixed the right fault — an item becoming
    /// an essay — and gave up the only thing that bounded the total. Both budgets were green
    /// at nearly twenty-five thousand words, because twelve new ideas cost twelve lines and
    /// nothing ever said stop. The session that measured this had read the doc in pages all
    /// day without once noticing it was the failure.
    /// </para>
    /// <para>
    /// <b>And it costs no information, which is the objection that retired the old doc-wide
    /// cap.</b> That objection was written when there was nowhere else to put things. There is
    /// now: a finding belongs to the commit that produced it and the test that asserts it, a
    /// mechanism to the XML comment the compiler enforces, a trap with a check to the check.
    /// A doc-wide cap does not delete information — it evicts it to the home that keeps it
    /// honest. What it must never do is delete an item to afford another.
    /// </para>
    /// <para>
    /// <b>THE TARGET IS SIX THOUSAND</b>, roughly eight thousand tokens, which is loadable
    /// without thinking about it. Every pass lowers this constant to what it achieved, so the
    /// doc can never grow back past its own best by ACCIDENT.
    /// </para>
    /// <para>
    /// <b>And it may be raised for something genuinely new — John, 2026-08-13, in those
    /// words.</b> A ratchet that only ever falls says a new idea can only be afforded by
    /// deleting an old one, which is a doc-wide cap deciding what the project may think
    /// about. That is not what it is for. The three conditions are his: <b>the existing
    /// items are reasonable.</b> The new one duplicates nothing, and the doc is still in a
    /// state you would load whole.
    /// </para>
    /// <para>
    /// <b>The third condition is the only one</b> that is not a judgement call, and it is also
    /// the one this check cannot make. So a raise is a deliberate edit to this constant
    /// and reads as one in a diff — the escape hatch is a NUMBER somebody had to type,
    /// rather than prose. Compaction still lowers it every pass; what is refused is a raise
    /// that pays for a rewording.
    /// </para>
    /// </remarks>
    // And 9,809 is the first deliberate raise, which is the rule above being used rather
    // than an exception to it. It buys the rule itself: two lines saying the budget may
    // rise for something genuinely new. A cap that could only fall would have had to be
    // paid for by deleting an item, which is the failure the paragraph describes.
    //
    // 9,831 is the second, and it buys John's own direction: which world is the spine. The
    // doc named no world at all before it, so a session asking which one to grow had the
    // handoff and nothing that outlives a handoff. The three conditions are met -- nothing
    // else in the route says it, no item was deleted, and forty-five words is not the
    // difference between loading this and not.
    //
    // And 9,911 is the third, for two ideas John raised in conversation that the doc had no
    // home for and that a reply loses at the next context window. Text as an IMAGE, which is
    // the only perceptual world whose ground truth stays enumerable; and whether a word should
    // be one hash at all, given that `walked` and `walking` are currently as unrelated as
    // `walked` and `kitchen`. Both duplicate nothing here, and eighty words is not the
    // difference between loading this and not.
    //
    // And 9,953 is the fourth, for rung three arriving on real English. `Recalled` speaks
    // `Recited` now, so the sequence rung reads a corpus somebody else wrote rather than a
    // generated sentence -- and whether that PAYS is a question the route had no leaf for,
    // the only rung-three item being fork 105's on `Handing`. Forty-two words, no item
    // deleted, and nothing else here says it.
    //
    // And 9,949 is the ratchet doing its ordinary work in the same session. Compaction
    // reworded the fork 107 gate and the rung-three leaf once each had a number behind it,
    // which is four words back. A raise that is not spent stays spent otherwise.
    //
    // And 9,961 is the fifth, for a world that can be ACTED IN. `IWorld.Next` was a pull for
    // the life of the branch, so no learner ever chose anything and *original thought* was
    // the one architecture line with nothing under it at all. `IActed` is the verb, and the
    // two leaves it earns say what is built and what a policy is still handed in for.
    // Twelve words after the addition was tightened, no item deleted, and the route said
    // none of it -- the third condition being what a leaf reading "nothing" cannot meet.
    //
    // And 9,959 is the ratchet again in the same session. `Homeostat` is no longer stranded,
    // so the `Drives` leaf saying so was corrected and came in two words shorter.
    //
    // And 10,064 is the sixth raise, for John's ORDER OF THE WORK and the fork it settles.
    // The doc said which world is the spine and what every requirement wants; it never said
    // in which ORDER, so a session could read the route and start refining rung one with two
    // requirements unstarted -- which is the drift the last two handoffs both had to correct
    // by hand. Four sentences that outlive a context window against a correction that has
    // now cost two sessions. And the repair-budget leaf is settled the same day it is
    // quoted, `Attempts` going and `Earned` shipping, so its replacement carries the
    // decision and the curve John wants over it rather than the open question it replaced.
    //
    // And 10,052 is the ratchet's ordinary work again: the repair-budget leaf was written
    // as settled ahead of its grid, the grid refuted it, and the correction is shorter than
    // the claim was.
    //
    // And 10,058, raised by six for `Drives`. The rule allows a rise for something genuinely
    // new, and a chooser reading a population is a mechanism this branch did not have -- its
    // leaf duplicates nothing and fork 111 is what its refutation opened. Deleting an item to
    // afford it was the alternative, and a guard must not cost information.
    //
    // And 10,072 is the seventh raise, and the smallest: fourteen words for `Widening`'s
    // deletion. Three arms and a gate went in one commit, and the row REPLACES the one the
    // gate already had rather than sitting beside it -- which is what keeps a three-armed
    // refutation to a single line. The evidence column stays a reason and not a readout, so
    // both arms' readings are in the commit and in `PopulationTests`, and what the row buys
    // is the two facts a session would otherwise rebuild the mechanism to rediscover: a drop
    // usually makes a sound scope unsound, and no significance gate reaches that under
    // `Floor`.
    //
    // And 10,077, five words for the architecture line that had nothing under it. *What it
    // is told must be falsifiable* read `NOW -- nothing`, and it is the only entry John's
    // order of the work names as owed a mechanism however bad. The replacement leaf says
    // what the mechanism is and the OPEN beside it says which half of fork 104 is still
    // open, which the old pair could not, since a leaf reading `nothing` cannot narrow.
    //
    // And 10,076 is the ratchet's ordinary work: the stranded leaf named seven worlds and
    // the live count is eight entries across five, so the number came out and the test that
    // prints it went in. A count in a doc rots the first time the list moves.
    //
    // And 10,097, twenty-one words for the reading that came back on the mechanism above.
    // The leaf saying a told statement settles was written the same session, and the grid
    // says no arm reaches that world's marginal -- so a route entry claiming a mechanism
    // where nothing can be shown to work is the exact drift this doc exists to prevent.
    // The OPEN beside it names the next world rather than the next arm, which is the part
    // no commit message survives long enough to say.
    //
    // And 10,127 is the eighth raise, thirty words for the world that came off the stranded
    // list. `Rhythm` is the only one here whose ANSWER moves and the only one whose moment
    // holds a single code, so it is the one place `malleability is the record` can be right
    // or wrong and the one place repair can be held still while the vote is not. That leaf
    // duplicates nothing: the branch above it had a mechanism and no world that could test
    // it. Deleting an item to afford it was the alternative, and a guard must not cost
    // information.
    //
    // And 10,164 is the ninth raise, thirty-seven words for the question the world that came
    // off the list next asked. `Motif` manufactures rung five's redundancy on purpose, and
    // the branch above the new leaf claimed a recursion -- a name standing for a name -- with
    // nowhere it had ever been watched. Naming a scope shortens it, so the rung consumes what
    // depth needs, and that is a limit on the mechanism rather than a number about one run.
    // It duplicates nothing above it and it names its own axis, which is what makes it
    // closeable rather than a note.
    //
    // And 10,205 is the tenth raise, forty-one words for the spine's last tier. One OPEN
    // became a NOW and an OPEN: `Roaming` can be acted in, and nothing on it can rank a
    // chooser because a house has nothing to want. The second half is what a single leaf
    // could not carry -- a tier that is built and a tier that is blocked read identically
    // from one line, and John's order asks for a mechanism for every architecture entry
    // rather than a measurement, so which of the two this is has to be sayable.
    // And 10,419 is the eleventh raise, two hundred and fourteen words for the push
    // architecture: a world becomes a set of inputs, a moment settles the one before it, and
    // the brain answers with what it did. Seven leaves and one new entry, because the seam
    // moves under every branch at once and a single leaf could not say which half is decided.
    //
    // IT WAS PAID FOR BY A COMPACTION THAT FAILED, and the failure is the reason to record
    // it. Eleven SETTLED leaves looked like the doc's own rule going unapplied -- built and
    // decided means gone from here -- and deleting ten of them freed two hundred and fifty
    // one words. `Every_fork_the_code_cites_is_in_the_index` then went red on forks 25, 27,
    // 33, 48, 66, 88 and 96: a SETTLED leaf is where a closed fork's NUMBER lives, and the
    // XML comments cite those numbers. So the leaves are the index rather than residue, the
    // deletion was reverted, and this is a raise rather than a trade. A guard must not cost
    // information, and the only reason it did not is that another guard was watching.
    // And 10,467 is the twelfth raise, forty-eight words for two phases John added to the
    // order of the work on 2026-08-18. Three is auditing TRAPS and DO NOT RE-TRY, four is
    // splitting `Population`, and refining moves from three to five. The order paragraph is
    // the one place a phase can live: `OutstandingTests` takes only entries computable
    // without judgement, and which traps have earned a check is a judgement an entry at a
    // time.
    // And 10,567 is the thirteenth raise, a hundred words for John's
    // reordering on 2026-08-18 and the decision under it. The seam moves to phase one,
    // because every mechanism built against `Bench` would have to be ported once it goes;
    // clearing the reds moves to three; refining moves to six. The amendment beside it is
    // the seam carrying its own repair — a phase that leaves the suite unreadable makes the
    // next one blind.
    //
    // The leaf is the other half. What is predicted is a SET and what is done is a set, so
    // scoring becomes precision and recall and every baseline is re-taken. That sentence
    // cost a raise rather than a trade on purpose: preserving recorded numbers is never a
    // reason to keep anything, and a doc that had to delete an item to say so would be
    // paying the same fee twice.
    // And 10,709 is the fourteenth raise, a hundred and forty-two words for how a predictor
    // acts. A commitment read BACKWARDS is a plan, which needs no machinery that is not
    // already here — and it took the whole branch to notice, because every leaf under
    // `Original thought` was about what to build and none was about reading what exists the
    // other way round.
    //
    // Two of the four are constraints rather than questions. A goal and a prediction are one
    // type once an expectation is a set, so the set decision bought the goal's grammar for
    // nothing. And a drive that cannot be sated is a fault in the design — John's, and it
    // belongs here because a preference term is chosen once and lived with, which is a
    // different kind of decision from a dial.
    //
    // And 10,721 is the fifteenth raise, twelve words for what phase two's second half turned
    // up. Two leaves were claiming more than a run reaches: adhesion is derived offline, and
    // nesting a commitment inside a scope is a property of the type rather than a mechanism —
    // a `Committed` code is a dictionary key and never enters a moment, so nothing can root
    // on one anywhere. Correcting a leaf that reads as built is the one thing this budget
    // must never price out, a doc whose claims outrun its code being worse than a long one.
    //
    // And 10,789 is the fifteenth raise, eighty words for a refutation row and the
    // two leaves around it. Carrying a decider's identity into the next moment was built,
    // measured and deleted in one session: it reached the architecture line nothing else
    // reaches, moved no score, grew the table by half, and cost `Rhythm` the one-code moment
    // that makes it this suite's only repair-held-still control.
    //
    // A revival row is the one kind of content this budget must never price out. The rule
    // above it says a loser is deleted and leaves one, so refusing the words would leave the
    // repo with a deletion and no record of why -- and the next session would build it again.
    // The row carries the number that says where it could pay and why that is not enough.
    //
    // And 10,893 is the sixteenth raise, a hundred and four words for a design conversation
    // with John on 2026-08-19 that the doc had no home for. Its content is that SELECTING was
    // never a mechanism: a transcript arrives as one moment, so nothing is ever built from
    // the statements and the machine is asked to comprehend in a single shot. Statements as
    // moments removes the problem, and what is left of it is reading a commitment backwards,
    // which fork 115 already carries.
    //
    // Two more came out of the same conversation. Questionhood is handed over by the world
    // rather than learnt, and a final `?` with rung three would say it. And the world that
    // holds repair still does it with a one-code moment, which any brain-side code breaks --
    // one already did, this session. A control whose codes never vary would hold on
    // principle rather than by accident.
    //
    // And 10,982 is the seventeenth raise, eighty-nine words for John changing the order on
    // 2026-08-19. The conversation harness comes before the rest of phase two, and the reason
    // is a leaf that was already here: a primer moves no counter, so reading a corpus is a
    // no-op and a world that ASKS is what makes it teach anything. His own curriculum --
    // teach it English, then examine it -- is measured to cost rather than pay, so the
    // conversation is not the exam after the reading. It is what makes the reading work.
    //
    // The second line is what to leave alone, which is the half a direction change usually
    // loses. Naming the three on the path names the six that are not, and a session reading
    // only the ordering above would have finished adhesion first for a phase's sake.
    // And 11,142 is the nineteenth raise, a hundred and forty-six words for John's session on
    // 2026-08-19 -- the one that asked whether feeding sentences puts commitments in the brain.
    // It did not, and the six leaves are what came out of finding out. Two are NOW: a typed
    // sentence is a moment rather than a typed line, and a statement claims its rarest word so
    // that being told is falsifiable at all. Four are open and three of them are John's -- what
    // a moment carries beside its own sentence, what picks a statement's claim without an
    // experimenter, whether an outvote is enough where a monotone counter cannot retract, and
    // one brain process with worlds attached over a stream.
    //
    // The raise is taken rather than paid for by compaction, and that is the judgement. The
    // leaves it would have come out of are the ones printed by the length check, and every one
    // of them is already a sentence a clause short of unreadable -- so the words would have
    // been bought by losing content, which is the one thing a guard may never cost.
    //
    // And 11,204 is the twentieth raise, sixty-two words for the second half of the same
    // session. John's objection was that the repair floor's twenty misses look arbitrary, and
    // it turned out not to be a threshold problem at all: genesis mints one code a commitment,
    // so a conjunction a statement STATES is reachable only by narrowing a one-code rule after
    // it has failed enough times. `Rooting` lets an assertion mint its whole scope, and the
    // three leaves are that, the reading that it still does not reach one telling, and the
    // fork that renumbered behind it.
    //
    // And 11,236 is the twenty-first raise, thirty-two words for the two leaves that came out
    // of chasing one-shot learning to its actual blocker. Crediting a mint with the round that
    // made it is one; the other says one telling still fails, and it fails on the CLAIM rather
    // than on the vote or the gate -- the rarest word so far is a tie on first hearing.
    //
    // And 11,248 is the twenty-second raise, twelve words for the leaf that closes John's
    // one-shot question and the one that says what is still unweighed behind it. A statement
    // claims every word in turn rather than picking one, and told once it answers an exam it
    // has never sat.
    //
    // And 11,294 is the twenty-third raise, forty-six words for the refuted row a deleted arm
    // owes. Background codes were let into a wide genesis scope and it lost; the row says by
    // how much and what would bring it back, and a row without one is a superstition. Fork
    // 114 closed in the same pass and paid part of it back.
    //
    // And 11,326 is the twenty-fourth raise, thirty-two words for the leaf that says a round
    // has no step putting what fired back into the moment. It is measured rather than
    // asserted: on a lesson where half the answers are stated and half follow from two
    // statements, the stated half reads 1.000 from one telling and the implied half 0.000 at
    // one, five and twenty. It is written as its own leaf because the horizon beside it is a
    // different thing -- that one is across occasions and this one is within a round.
    //
    // And 11,414 is the twenty-fifth raise, eighty-eight words for a refuted row and the leaf
    // beside it. A second hop -- conclusions made live so one rule can meet another -- was
    // built in three shapes and every one read nought on the implied half while costing the
    // run's own accuracy. The row is long because three shapes died rather than one, and a row
    // whose revival condition does not say which shapes were tried would send the next session
    // straight back down the same three.
    //
    // And 11,502 is the twenty-sixth raise, eighty-eight words for a second refuted row and the
    // leaf beside it. Claiming only a sentence's least-said words was meant to unify two
    // claiming rules and cost less; it cost more of what mattered and is deleted. The leaf is
    // what the run that refuted it showed instead: claiming every word makes a rule wrong on
    // its own sentence's other claims, so repair churns for ever while minting saturates.
    //
    // And 11,520 is the twenty-seventh raise, eighteen words for the rule a leak turned out to
    // need: a source owing moments is drained before a new line is read. It is one line and it
    // is worth one, because reading early advanced a scripted source past a sentence still
    // arriving and put an examination's answer live for moments nobody asked about.
    //
    // And 11,567 is the twenty-eighth raise, forty-seven words for a refuted row. A refusal was
    // made to settle on a reserved outcome so that whatever proposed the guess would be wrong
    // about it; it bought nothing over eight passes and taught the machine to stop asking. The
    // revival condition is the useful half — recording a refusal pays only once something
    // AVOIDS what it was refused, and no chooser here does.
    //
    // And 11,631 is the twenty-ninth raise, sixty-four words for John switching the first north
    // star. The CONVERSATION is the spine world -- a block told, a window where the machine may
    // ask, then a fixed examination -- and it stays until it is exhausted. `Roaming` is kept
    // rather than retired because it is where a sound, a look and a sentence can be one moment,
    // and a parked world rots, so its reason is written beside it.
    //
    // And 11,660 is the thirtieth raise, twenty-nine words for fork 86 closing and 126 closing
    // behind it. A separating condition must now leave a child that can clear the floor itself,
    // or it is a rule nothing could ever refute -- which costs no score on two lessons, leaves
    // a sixth of the population, and makes the ladder's trigger fire where it never had.
    //
    // And 11,703 is the thirty-first raise, forty-three words for a refuted row. A question
    // carrying the topic while statements stayed bare was fork 125's cheapest shape -- bare
    // statements so genesis can root, a carried story so a SELECTING front end has something
    // to walk. Nought on the implied half under every front end this repo has, and worse on
    // the stated half than carrying nothing.
    //
    // And 11,806 is the thirty-second raise, a hundred and three words for a mechanism, the
    // arm it revives, and one fork of John's. A moment now takes more than one doing: the world says
    // whether it will take another and the chooser says whether it has more, so a machine can
    // ask, be refused, and ask again. That is the revival row for recording a refusal, which
    // was dropped because nothing avoided what it was refused and now something does. John's
    // fork is an exam whose problem is unsolved and whose progress is partial, which is a way
    // of measuring rather than another tier of tasks.
    //
    // And 11,870 is the thirty-third raise, sixty-four words for a refuted row and the entry
    // it closes. A refusal as a code in the moment: `no` says the answer is not this word, the
    // counters are monotone and cannot hold a negative, so it enters positively instead.
    // Nought over eight seeds in two shapes, and the reason is what the words buy -- the
    // chooser had already refused to repeat what it said, so the fact was acted on before the
    // code arrived.
    //
    // And 11,912 is the thirty-fourth raise, forty-two words for one entry closing and one
    // sharpening. Claiming, width and crediting all reproduce on drawn lessons, so none of
    // those readings was about the single hand-written text -- and a drawn lesson knows every
    // truth it states, which is what lets the second entry say the gap is the seat rather
    // than the search.
    //
    // And 11,962 is the thirty-fifth raise, fifty words for why fork 86's bar does not ship.
    // What it costs is a function of how young the population is: it refuses a child that
    // cannot clear the floor, so it blocks repair exactly while nothing can clear one yet.
    // Free at saturation and most of the examination before it, which is not what *costs no
    // score* says and is not a thing any reading taken at one telling count could have said.
    //
    // And 11,975 is the thirty-sixth raise, thirteen words for a refutation row rewritten. The
    // vote gate is deleted and its row now says what killed it rather than what once did: it
    // silences the right rule instead of reseating a wrong one.
    //
    // And 12,024 is the thirty-seventh raise, forty-nine words for the seat entries rewritten
    // around what coverage showed. The gap between what a population HOLDS and what it answers
    // is the seat exactly, so the axis is time rather than the vote rule: more tellings close
    // it unaided, and what is wanted is a correct young rule outranking a wrong old one
    // sooner. That names the next arm, which is what a route entry is for.
    //
    // And 12,124 is the thirty-eighth raise, a hundred words for John's, 2026-08-19.
    // A label is welcome and what is required beside it is everything the thing stands to,
    // which corrects a reading of the architecture rather than the rule. And fork 129, which
    // is the wall both designs hit: `csharp` refuted widening a walk three ways and asked for
    // a likeness the graph did not compute, this branch refuted a similarity code, and what
    // neither tried is likeness read off the POPULATION -- two codes alike where the
    // commitments naming them expect the same things, which never asks whether they
    // co-occurred. Four tries, two designs, one target, and that is worth the words.
    // And 12,155 is the thirty-ninth raise, thirty-one words for the derivation going live.
    // `Alternating` was taken offline and the entry against it said that re-taking it orphans
    // every scope holding a category. Half of that is now built and the finding sits with it:
    // one moment at a time over monotone counts, reaching what a list reaches, with the places
    // closing at 1,250 sightings and the looks at 3,750. The half that is left is the store,
    // and it is a new item rather than a rewording -- a category's name IS its members, so a
    // group that grows is a new category beside the old and never an edit to it.
    // And 12,182 is the fortieth raise, twenty-seven words for the scaling exponent, which
    // was the one entry here reading UNMEASURED while naming itself the number that predicts
    // whether any of this reaches perception. It is measured: 2,003, 3,468 and 7,920 rounds
    // to target at six, eleven and twenty bits, eight seeds, every seed reaching at every
    // width. The input space grows sixteen thousand fold across that and the cost grows
    // 3.95, so it tracks the depth of a scope rather than the space -- and the second item
    // is where the cost went instead, the population going 19, 797 and 1,824 sound rules.
    // A reading that turns an OPEN into a NOW and a guard is what the budget is for.
    // And 12,219 is the forty-first raise, thirty-seven words for two statistics that were in
    // this repo at once for one idea. `Alternating.BySpace` takes the share of company two
    // codes share as a SET, so a partner seen once weighs what a partner seen a thousand
    // times weighs. Fork 98's statistic in `RecalledTests` takes the cosine of the COUNTED
    // company, and it is the one that priced a category at five points under the bag -- so
    // the reading that pays was taken on an object the shipped mechanism is not. On bAbI they
    // disagree: weighing the counts drops `is` out of the places and loses the `to` against
    // `where` group entirely. That is an item rather than a finding, because neither
    // threshold is calibrated against the other and the ranking wants a sweep.
    // And 12,225 is the forty-second raise, six words, for the item above turning from a
    // question into a refutation. Weighing company beats a set of it on every bAbI task at
    // every threshold -- three pure classes against two, one and nought -- and the set
    // reading recovers nothing at all on the richest of the three. What is left open is that
    // `ByTime` is what a moving world needs and a weighed adhesion does not exist, so the
    // OPEN is a smaller question than the one it replaces rather than the same one reworded.
    // And 12,261 is the forty-third raise, thirty-six words from a design conversation, which
    // is the condition this budget names rather than an exception to it. Two items are new:
    // a beginner language course as the primer, John's, being one referent through video,
    // audio and text ordered for a learner with no language -- ostension by construction and
    // the teacher signal this design trades corpus scale for. And fork 107 gating every
    // sensor, text as an image being the one crossing that keeps the ground-truth
    // instruments alive, so it is a bridge rather than one more modality.
    //
    // Paid down where it could be. The exponent and the likeness readings state their shape
    // and leave their numbers in the guards that now hold them, and the rule about where
    // rarity belongs went to `Repair.Divergence` rather than here, a mechanism living beside
    // its code being what this doc says it is for.
    // And 12,312 is the forty-fourth raise, fifty-one words, and both items are refutations
    // rather than questions. The seat had one leaf saying the gap was measured and one saying
    // the axis was TIME; it now has three, because splitting a wrong answer into absent,
    // outranked and tied says the two ages are two failures and that neither specificity nor
    // a crowd separates a tied pair. An OPEN that names which of two failures is unanswered
    // is a smaller question than one that named neither.
    //
    // The rest is a trap with no check behind it, which is where this doc says those live. A
    // check that races the signal it waits on passed on this machine for the life of the
    // branch and went red once on a runner, and no existing entry covers it -- the nearest is
    // about a cost differing by platform, which is a reading rather than a correctness bug.
    // And 12,400 is the forty-fifth raise, eighty-eight words, and it buys two findings and a
    // named blocker. Crediting a mint with the round that made it converts a TIE into an
    // OUTRANKING for the identical score, because the newest mint is the strongest and the
    // older ones have missed since -- so it breaks the tie by recency, and recency is not
    // correctness. And the three arms together answer a paper never sat, told once, on eight
    // drawn lessons rather than on the one hand-written text that reading came from.
    //
    // The blocker was why neither default moved and it was work rather than an opinion. The
    // wide root hands genesis a sound multiplexer rule, and `blind.Sound` reading nought is
    // what says repair rather than genesis did the learning -- the single most load-bearing
    // assertion in step one. Re-taking it faithfully wanted sound-by-BIRTH, and re-taking it
    // any other way would have traded a sharp claim for a weak one.
    //
    // And 12,431 is the forty-sixth raise, thirty-one words, for that blocker being built and
    // the root shipping. `Population.Births` records the operator per commitment, so the
    // assertion now says what it always meant: random-Z learns nothing sound, and the sound
    // rules the blind arm holds were handed to it by genesis. Lineage attribution was tried
    // first and is not sharp enough -- repair grows children to the width of the moment, so
    // the whole-moment scope shares its entry with them.
    // And down to 12,407, which is a fall and is written as one. The seat had grown to eight
    // leaves in a session and four of them said one thing between them; what is left says the
    // root ships, what crediting does instead, what the three do together, and which half is
    // still open. A budget falling because an item was compacted rather than deleted is the
    // only kind of fall this doc wants.
    // And 12,470 is the forty-fifth raise, sixty-three words, and it closes the seat rather
    // than adding to it. John's, mid-session: if it has not learnt enough to decide between two
    // things, it should acknowledge that. The vote was breaking a tie by CODE ORDER, so the
    // machine asserted an answer out of a hash and was then corrected on a guess -- and the
    // signature is a weight of nought, which needs no threshold because it is the best
    // advocate's own accuracy. Three leaves: that it declines, that declining is free where
    // there is evidence, and what is still open about the cell where there is none.
    // And down two to 12,468, correcting a leaf written in the same session. It asked whether
    // a machine that ASKS turns a decline into a question, which is `Untested` on the weight
    // wearing different words -- refuted in the table below, losing fiftyfold to a coin per
    // ask. The revival row asks for a signal saying whether a reply CAN settle, and whether
    // the machine is sure is not that. This repo's own trap list warns that a row names an
    // axis in the mechanism's words rather than the comparison's, which is how a search
    // misses it; the leaf now names the refutation so the next session cannot walk into it.
    // And 12,493 is the forty-sixth raise, twenty-five words, for a second world that came
    // back a NULL and said something anyway. The multiplexer declines five rounds in thirty
    // thousand at an unmoved accuracy, because a weight of nought is an advocate never settled
    // and that phase is a rounding error on a long run. So the axis declining lives on is how
    // YOUNG a population is rather than which world it is -- the conversation is not a
    // friendlier world for it, it is a shorter one.
    // And down ninety-five to 12,398, the seat being decided and a decided thing leaving the
    // route. Eight leaves became five with nothing dropped, the tie's three refutations being
    // three tellings of one finding: nothing in a record separates two rules at one weight.
    // And 12,416 is the forty-seventh raise, eighteen words, for a cost the dial's own remark
    // denied. `Deciding` said it stops the assertion and not the learning, which was written
    // from the watched case; `Curiosity` reads the same vote, so on a world the machine ACTS
    // in a moment with nothing grounded has no claim to make and a third of the asks go. The
    // session is seventy-seven words down on where it started even so.
    // And 12,432 is the forty-eighth raise, sixteen words, for fork 107's ceiling being taken.
    // A word drawn as pixels is the one crossing that keeps ground truth enumerable and the
    // front end had never been priced on it, so the leaf could not say whether the fork was
    // blocked there. It is not, and `LetteringTests` holds the numbers. The first draft of the
    // leaf carried them and cost twenty-seven words more; `The_plan_looks_forward` caught it,
    // correctly -- a finding lives in the commit and the test, and a doc that starts keeping
    // them is the pile of docs this one replaced.
    private const int Whole = 10_986;

    /// <summary>
    /// Every section the plan is allowed to have, in order.
    /// </summary>
    /// <remarks>
    /// <b>Anything built and decided is not on this list</b>, because it is in the code.
    /// <para>
    /// <b>John's shape, 2026-08-12, and it went from nine sections to four.</b> The goal, the
    /// architecture, the constraints and the first north star were four headings saying one
    /// thing — here is what must be true when this is finished — and `TO BUILD` was a fifth
    /// listing what is not built yet, which is what a route leaf already says. So the doc
    /// splits on the only line that matters to a reader: what is FIXED against what MOVES.
    /// </para>
    /// <para>
    /// <b>And `open defects` went with them.</b> It was added when whole areas were being
    /// deferred mid-rabbit-hole and a defect could sit unowned for weeks. Handoffs and a CI
    /// that goes green every session carry that now, and a defect that outlives a session is
    /// a `BROKEN` leaf against the requirement it blocks.
    /// </para>
    /// <para>
    /// <b>And the fork index went earlier</b>, which is what the route becoming a tree bought.
    /// Ninety-four flat rows that had to be read whole to find the one bearing on your work.
    /// The numbers still resolve, because
    /// <see cref="Every_fork_the_code_cites_is_in_the_index"/> reads the whole doc rather
    /// than one section of it.
    /// </para>
    /// </remarks>
    private static readonly string[] Sections =
    [
        // Four, and the split is the fixed against the moving. `The destination` holds what
        // must be true when this is finished -- the bet, the requirements, the machine's
        // invariants, the first target, and what the field already knows. None of it moves.
        // `THE ROUTE` is where everything moves, and it is the only section a normal session
        // edits.
        //
        // `THE ORDER` is John's, and it is first because it is the one thing a session acts
        // on: what is being worked on, in what order, with a finished item STRUCK rather
        // than marked done. The ordering used to live in the route's preamble beside the
        // phases it had outgrown, and a second copy of it lived in each handoff commit --
        // two lists that disagreed about whether the intentional reds came first.
        "THE ORDER",
        "THE DESTINATION",
        "THE ROUTE",

        // And the two that are cross-cutting by nature. A refutation belongs to the ARM it
        // killed and a trap to the failure CLASS, neither of which is a requirement -- so
        // folding them into the route would scatter them past finding.
        "DO NOT RE-TRY",
        "TRAPS",
    ];

    /// <summary>
    /// The budget for PROSE, as against structure.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>John's rule, 2026-08-04: prefer cutting prose over cutting lists.</b> A
    /// total word budget is indifferent to what gets retired, so when the doc goes
    /// over, whatever is easiest to delete goes — and that is usually a table row
    /// or a bullet, because a paragraph reads as though it is holding an argument
    /// together. It is the wrong instinct: a bullet is a reminder, and a reminder
    /// is all this doc has to be. <b>The connective tissue can be rederived; the
    /// item cannot.</b>
    /// </para>
    /// <para>
    /// <b>It sits well above the current count and far below the total</b>, so
    /// prose has room to exist where a bullet genuinely will not do, and no room
    /// to creep back into being the default shape.
    /// </para>
    /// </remarks>
    private const int Prose = 400;

    /// <summary>
    /// Whether a line is structure rather than prose.
    /// </summary>
    /// <remarks>
    /// <b>Deliberately syntactic, like the findings rules.</b> The point is not to
    /// judge whether a paragraph is earning its place — it is to make the
    /// distinction mechanical enough that nobody has to argue about it. A heading,
    /// a bullet, a table row, a quote, and the wrapped continuation of a bullet
    /// all count as structure.
    /// </remarks>
    private static bool Structural(string line)
    {
        var trimmed = line.TrimStart();

        if (trimmed.Length == 0) return true;

        if ("-|#>".Contains(trimmed[0], StringComparison.Ordinal)) return true;

        // An asterisk is a bullet only with a space after it. `**` opens bold,
        // and this doc leads nearly every sentence with it — counting those as
        // structure would let prose pass the budget by shouting, which is the
        // one way this check could be worth nothing.
        if (trimmed[0] == '*')
            return trimmed.Length > 1 && trimmed[1] == ' ';

        // `1.` and friends — an ordered list is a list.
        if (char.IsAsciiDigit(trimmed[0]) && trimmed.Contains('.', StringComparison.Ordinal))
            return true;

        // A wrapped bullet is still a bullet. Markdown continues a list item on an
        // indented line, and counting those as prose would make the rule punish
        // line wrapping rather than paragraphs.
        return line.StartsWith("  ", StringComparison.Ordinal);
    }

    private static string Repo() => Tree.Repo();

    private static string Docs() => Tree.Docs();

    private static string Plan() => File.ReadAllText(Path.Combine(Docs(), "plan.md"));

    /// <summary>The lines under one heading, at any depth, the heading excluded.</summary>
    /// <remarks>
    /// <b>BY DEPTH RATHER THAN BY `##`</b>, because collapsing nine sections into four made
    /// `THE ARCHITECTURE` a subsection — and a reader hard-coded to one level would have
    /// silently returned nothing for it, which is a correspondence check passing on an empty
    /// list.
    /// </remarks>
    private static string[] Section(string heading)
    {
        var lines = Plan().Split('\n');

        var start = Array.FindIndex(lines, line =>
            line.TrimEnd().EndsWith(' ' + heading, StringComparison.Ordinal)
            && line.StartsWith('#'));

        Assert.True(start >= 0, $"the plan has no `{heading}` heading");

        var depth = lines[start].TakeWhile(character => character == '#').Count();

        return lines
            .Skip(start + 1)
            .TakeWhile(line => !line.StartsWith('#')
                || line.TakeWhile(character => character == '#').Count() > depth)
            .ToArray();
    }

    /// <summary>
    /// The bullets of a section, each with how deep it is nested.
    /// </summary>
    /// <remarks>
    /// <b>Two spaces a level, which is what markdown nests by</b> — so a branch is depth
    /// nought, the requirement under it is depth one, and anything at two or deeper is a
    /// leaf. The route's shape is load-bearing rather than cosmetic, which is why three
    /// checks read it this way instead of matching prose.
    /// <para>
    /// <b>And a wrapped bullet is one bullet, which is not a detail.</b> The first version of
    /// this read single lines, so a leaf's revival clause was invisible whenever it fell past
    /// the line break — and two dead leaves passed the revival check purely because of where
    /// their text happened to wrap. A guard whose verdict moves with line width is worse than
    /// no guard, because it reads as green.
    /// </para>
    /// </remarks>
    private static List<(int Depth, string Text)> Nested(IEnumerable<string> lines)
    {
        var bullets = new List<(int Depth, string Text)>();

        foreach (var line in lines)
        {
            var trimmed = line.TrimStart();
            var indent = line.Length - trimmed.Length;

            if (trimmed.StartsWith("- ", StringComparison.Ordinal))
                bullets.Add((indent / 2, trimmed[2..].Trim()));
            else if (bullets.Count > 0 && trimmed.Length > 0 && indent > 0)
                bullets[^1] = (bullets[^1].Depth, $"{bullets[^1].Text} {trimmed.Trim()}");
        }

        return bullets;
    }

    /// <summary>
    /// Words too common to mean an entry is talking about the same thing as the
    /// architecture line above it.
    /// </summary>
    /// <remarks>
    /// <b>Short enough to be obviously incomplete, which is deliberate.</b> A missing
    /// stopword only WEAKENS the correspondence check, since a spurious match lets a row
    /// pass; it can never redden a doc that is right. So the list is grown when a real edit
    /// slips through rather than guessed at up front.
    /// </remarks>
    private static readonly HashSet<string> Common =
    [
        "that", "this", "what", "with", "from", "have", "must", "never", "always",
        "which", "when", "then", "than", "into", "about", "also", "does", "over",
        "under", "every", "each", "their", "there", "here", "been", "were", "will",
        "would", "could", "rather", "itself", "part", "held", "only",
    ];

    /// <summary>The words of a line that could carry a subject.</summary>
    private static HashSet<string> Significant(string text) =>
        Regex
            .Matches(text, "[A-Za-z]{4,}")
            .Select(match => match.Value.ToLowerInvariant())
            .Where(word => !Common.Contains(word))
            .ToHashSet(StringComparer.Ordinal);

    /// <summary>
    /// What a route leaf may open with. <b>A closed set.</b> Because the point of a status
    /// token is that a reader can sort by it without reading the clause.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>`Broken` arrived when `open defects` went.</b> That section was retired because
    /// handoffs and a green CI carry what it used to, but a defect and an open question are
    /// different urgencies and `OPEN` flattens them — one wants investigating and the other
    /// wants fixing. The distinction cost one token rather than a section.
    /// </para>
    /// <para>
    /// <b>`Dead` and `Settled` came back out</b>, because the route holds what is decided and
    /// what is untried and neither of those is either. A refuted arm belongs in
    /// `DO NOT RE-TRY` with the row that says what would revive it; a question that closed is
    /// a finding, and its home is the commit that closed it. Leaving the two tokens legal
    /// left the door open for a session to write a finding down and have a guard bless it.
    /// </para>
    /// </remarks>
    private static readonly string[] Statuses =
        ["NOW", "OPEN", "BLOCKED", "BROKEN"];

    /// <summary>The route's leaves — the fork-bearing lines, whatever branch they hang off.</summary>
    private static List<string> Leaves() =>
        Nested(Section("THE ROUTE"))
            .Where(bullet => bullet.Depth >= 2)
            .Select(bullet => bullet.Text)
            .ToList();

    [Fact]
    public void The_route_tracks_the_architecture_one_for_one()
    {
        // The check that makes keeping the two sections apart safe rather than merely tidy.
        // `The architecture` is the one section that forbids mechanisms, so the route may
        // not be nested inside it -- an edit to a child would drag an edit into the parent
        // and the property would die quietly. Holding them one for one is what buys the
        // separation, and nothing but this says so.
        //
        // It already had something to catch. The route claimed a row per architecture line
        // in its own opening sentence and had twelve against thirteen: *what it is told
        // must be something it can be wrong about* had no row, its obstacle folded into the
        // line above instead. The prose rule had been there for weeks and read as true.
        var must = Nested(Section("THE ARCHITECTURE"))
            .Where(bullet => bullet.Depth == 0)
            .Select(bullet => bullet.Text)
            .ToList();

        Assert.NotEmpty(must);

        var route = Nested(Section("THE ROUTE"));

        var branch = route.FindIndex(bullet =>
            bullet.Depth == 0
            && bullet.Text.Contains("WHAT IT MUST DO", StringComparison.Ordinal));

        Assert.True(branch >= 0, "`THE ROUTE` has no `WHAT IT MUST DO` branch");

        var entries = route
            .Skip(branch + 1)
            .TakeWhile(bullet => bullet.Depth > 0)
            .Where(bullet => bullet.Depth == 1)
            .Select(bullet => bullet.Text)
            .ToList();

        Assert.True(must.Count == entries.Count,
            $"`THE ARCHITECTURE` has {must.Count} lines and `WHAT IT MUST DO` has "
            + $"{entries.Count} entries. Every architecture line gets one, in the same "
            + "order -- an architecture line with no entry is a requirement nothing is "
            + "carrying, and that is the state this check was written to end.");

        // And order, by a word the two share. Matching prose against prose would make the
        // check a style rule; requiring one significant word in common says the entry is
        // about that line without dictating how it is worded.
        //
        // WHAT IT DOES NOT CATCH, said out loud so nobody trusts it further than it goes:
        // two adjacent lines sharing vocabulary can be swapped and still pass. *Told, never
        // architected* and *what it is told must be settleable* are exactly that pair.
        var adrift = entries
            .Select((entry, index) => (entry, index))
            .Where(row => !Significant(row.entry).Overlaps(Significant(must[row.index])))
            .Select(row => $"entry {row.index + 1} `{row.entry}` shares no word with "
                + $"`{Opening(must[row.index])}`")
            .ToList();

        Assert.True(adrift.Count == 0,
            "an entry has drifted off the architecture line it is meant to carry:\n  "
            + string.Join("\n  ", adrift));
    }

    /// <summary>
    /// Every requirement has at least one mechanism, however bad.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Phase two of the order of the work, made checkable.</b> That phase is <i>a
    /// mechanism for every entry of THE ARCHITECTURE, however bad</i>, and whether it held
    /// was answered by reading thirteen entries and counting — which is a session's word for
    /// a state the build should be able to say. An entry whose leaves are all OPEN is a
    /// requirement nothing carries, and the neighbouring check cannot see it: that one holds
    /// the two sections one for one, so a row with an entry and no mechanism passes it.
    /// </para>
    /// <para>
    /// <b>NOW is the token that means built</b>, and the other five do not. SETTLED is a
    /// question closed rather than a mechanism standing, BLOCKED and BROKEN are the opposite
    /// of one, and DEAD is one that was deleted. So a branch carried entirely by SETTLED
    /// leaves reads as finished and holds nothing up.
    /// </para>
    /// <para>
    /// <b>And it says nothing about whether the mechanism is any good</b>, which is the whole
    /// point of <i>however bad</i>. Phase six is where that is asked, and a check demanding a
    /// mechanism WORK would be phase six wearing phase two's clothes.
    /// </para>
    /// </remarks>
    [Fact]
    public void Every_requirement_has_a_mechanism()
    {
        var route = Nested(Section("THE ROUTE"));

        var branch = route.FindIndex(bullet =>
            bullet.Depth == 0
            && bullet.Text.Contains("WHAT IT MUST DO", StringComparison.Ordinal));

        Assert.True(branch >= 0, "`THE ROUTE` has no `WHAT IT MUST DO` branch");

        var under = route.Skip(branch + 1).TakeWhile(bullet => bullet.Depth > 0).ToList();

        Assert.NotEmpty(under);

        var without = Unbuilt(under);

        Assert.True(without.Count == 0,
            $"{without.Count} of `WHAT IT MUST DO`'s entries carry no NOW leaf, so an "
            + "architecture line has an entry and no mechanism:\n  "
            + string.Join("\n  ", without)
            + "\nPhase two is a mechanism for every one of them, however bad.");
    }

    /// <summary>The entries under a branch with no <c>NOW</c> leaf beneath them.</summary>
    /// <param name="under">Everything nested under the branch, in order.</param>
    /// <remarks>
    /// <b>A helper rather than a loop in the test</b>, so the companion can put a bare entry
    /// to it. A detector that accepts everything passes in silence and reads exactly like a
    /// doc in order, which is the fault every companion in this file exists for.
    /// </remarks>
    private static List<string> Unbuilt(IEnumerable<(int Depth, string Text)> under)
    {
        var without = new List<string>();

        string? entry = null;
        var built = false;

        foreach (var bullet in under)
        {
            if (bullet.Depth == 1)
            {
                if (entry is not null && !built) without.Add(entry);

                entry = bullet.Text;
                built = false;
                continue;
            }

            if (bullet.Text.StartsWith("**NOW**", StringComparison.Ordinal)) built = true;
        }

        if (entry is not null && !built) without.Add(entry);

        return without;
    }

    [Fact]
    public void No_entry_carries_more_than_one_decided_mechanism()
    {
        // John's rule about what this doc is for, made mechanical. A `NOW` is the mechanism
        // DECIDED for an entry, so there is one of it; what a built thing actually does is
        // in the code, where the compiler enforces every reference and nothing can drift.
        //
        // The route had sixty-five of them against twenty-five entries, and the surplus was
        // readings -- what a run scored, which world an arm won on, what a dial cost. A
        // finding written here goes stale in silence, and this is the check that says so.
        var route = Nested(Section("THE ROUTE"));

        var crowded = new List<string>();

        string? entry = null;
        var decided = 0;

        foreach (var bullet in route)
        {
            if (bullet.Depth == 1)
            {
                if (decided > 1) crowded.Add($"{entry} carries {decided}");

                entry = bullet.Text;
                decided = 0;
                continue;
            }

            if (bullet.Depth >= 2
                && bullet.Text.StartsWith("**NOW**", StringComparison.Ordinal)) decided++;
        }

        if (decided > 1) crowded.Add($"{entry} carries {decided}");

        Assert.True(crowded.Count == 0,
            $"{crowded.Count} entry/entries carry more than one NOW leaf, so the route is "
            + "describing what is built rather than what is decided:\n  "
            + string.Join("\n  ", crowded)
            + "\nOne NOW an entry. Everything else it said belongs in the XML comment "
            + "beside the mechanism, or in the commit that measured it.");
    }

    [Fact]
    public void The_order_names_only_work_the_route_still_holds_open()
    {
        // John's: `THE ORDER` is the source of truth for what is next, and a list nobody
        // strikes reads as a plan while being a record. So an item leaves when its work
        // leaves -- which is checkable, because every item names the fork it is, and a
        // fork that has closed is no longer OPEN in the route.
        //
        // This is what makes the end-of-session strike mechanical rather than a habit. A
        // session that finishes fork 107 and forgets the list cannot reach green.
        var order = string.Join("\n", Section("THE ORDER"));

        var named = Regex
            .Matches(order, @"[Ff]ork \*\*(\d{1,3})\*\*")
            .Select(match => match.Groups[1].Value)
            .ToList();

        Assert.NotEmpty(named);

        var live = Live();

        var closed = named.Where(number => !live.Contains(number)).ToList();

        Assert.True(closed.Count == 0,
            $"`THE ORDER` names fork(s) the route no longer holds open: "
            + string.Join(", ", closed)
            + ". Either the work is done and the item should be STRUCK, or the route lost a "
            + "leaf it still needs.");
    }

    /// <summary>The forks the route still holds open, as a set of numbers.</summary>
    /// <remarks>
    /// <b>`BLOCKED` counts as live and `DEAD` does not</b>, because blocked is work waiting
    /// on other work and dead is work refuted. A closed fork's number stays listed in the
    /// route's preamble so the code's citations still resolve, which is why this reads the
    /// LEAVES rather than the whole section.
    /// </remarks>
    private static HashSet<string> Live() =>
        Leaves()
            .Where(leaf => leaf.StartsWith("**OPEN**", StringComparison.Ordinal)
                || leaf.StartsWith("**BLOCKED**", StringComparison.Ordinal)
                || leaf.StartsWith("**BROKEN**", StringComparison.Ordinal))
            .SelectMany(leaf => Regex
                .Matches(leaf, @"\*\*(\d{1,3})\*\*")
                .Select(match => match.Groups[1].Value))
            .ToHashSet(StringComparer.Ordinal);

    [Fact]
    public void The_order_check_can_still_fail()
    {
        // THE COMPANION. Without it the check above passes for an `ORDER` naming no forks
        // at all, and for a `Live` that returns every number in the doc -- both of which
        // read exactly like a list somebody is keeping up to date.
        var live = Live();

        Assert.NotEmpty(live);
        Assert.DoesNotContain("12", live);

        Assert.NotEmpty(Regex.Matches(
            string.Join("\n", Section("THE ORDER")), @"[Ff]ork \*\*(\d{1,3})\*\*"));
    }

    [Fact]
    public void Every_leaf_carries_exactly_one_status()
    {
        // A leaf is a line a reader should be able to sort without reading. That only works
        // if the token is at the front and there is exactly one of it -- a leaf saying both
        // OPEN and SETTLED is a row that has been half-updated, which is the failure this
        // doc's fork table had in a dozen places and no check could see.
        var leaves = Leaves();

        Assert.NotEmpty(leaves);

        var wrong = leaves
            .Where(leaf =>
                Statuses.Count(status =>
                    leaf.Contains($"**{status}**", StringComparison.Ordinal)) != 1
                || !Statuses.Any(status =>
                    leaf.StartsWith($"**{status}**", StringComparison.Ordinal)))
            .ToList();

        Assert.True(wrong.Count == 0,
            $"{wrong.Count} route leaf/leaves must OPEN with exactly one of "
            + string.Join(", ", Statuses) + " in bold:\n  "
            + string.Join("\n  ", wrong.Take(10).Select(Opening)));
    }

    [Fact]
    public void The_route_checks_can_still_fail()
    {
        // The companion the three above need, and for the reason every other companion in
        // this file exists: a predicate that accepts everything passes in silence and reads
        // exactly like a doc that is in order.
        Assert.Equal(
            [(0, "a branch"), (1, "an entry"), (2, "a leaf")],
            Nested(["- a branch", "  - an entry", "    - a leaf", "not a bullet"]));

        // The one that was missing, and its absence let two dead leaves pass the revival
        // check on where their text wrapped. A leaf is a bullet, not a line.
        Assert.Equal(
            [(2, "a leaf that revives when the wrapped half is read")],
            Nested(["    - a leaf that revives when", "      the wrapped half is read"]));

        Assert.False(Significant("Malleability is the record").Overlaps(
            Significant("AND IT LEARNS BY BEING WRONG AND FINDING OUT")));

        Assert.True(Significant("Malleability is the record").Overlaps(
            Significant("AND HOW HARD A BELIEF IS TO SHIFT IS ITS OWN RECORD")));

        // And the stopwords must still bite, or the overlap test above passes on any two
        // English sentences and the order half of the correspondence check is worth nothing.
        Assert.False(Significant("What it must never do").Overlaps(
            Significant("EVERY INPUT IS AN ATTRIBUTE, WHICH MUST NEVER BE THE THING")));

        // A branch whose second entry is carried by an OPEN alone. The first has a mechanism
        // and the third's NOW must not reach back over the entry between them, which is the
        // off-by-one a running flag invites.
        Assert.Equal(
            ["carried by nothing"],
            Unbuilt(
            [
                (1, "carried"), (2, "**NOW** — a mechanism"),
                (1, "carried by nothing"), (2, "**OPEN** — a question"),
                (1, "carried too"), (2, "**NOW** — another"),
            ]));

        // And a branch in order must come back empty, or the check above is a constant.
        Assert.Empty(Unbuilt([(1, "carried"), (2, "**OPEN** — a question"),
            (2, "**NOW** — a mechanism")]));

    }

    /// <summary>
    /// What a finding looks like in prose, so the doc can be kept clear of them.
    /// </summary>
    /// <remarks>
    /// <b>Deliberately syntactic rather than clever.</b> The point is not to
    /// detect the idea of a result — it is to make the rule so mechanical that
    /// nobody has to argue about whether a paragraph counts.
    /// </remarks>
    private static readonly (string What, string Pattern)[] Findings =
    [
        ("a measured score", @"\d\.\d{3,}"),
        ("a spread", @"±|\+-"),
        ("a sigma count", @"(?i)\bsigma\b"),
        ("a result marker", @"✅|❌"),
        ("a measured comparison", @"\d[\d,.]* (?:\w+ )*against \d"),
    ];

    /// <summary>
    /// What a claim looks like when it is dated rather than stated — <b>the doc's own rule
    /// about citing a TIME, made mechanical.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The rule was written and then broken three times in one file.</b> The plan already
    /// says never to cite a time, because <i>one session ago</i> is true when written and
    /// false forever after. What it did not say is that a DEFAULT is a time: <i>the shipped
    /// timing</i> names whatever ships at the moment of reading, so a row comparing
    /// <i>the shipped timing</i> against a named arm reverses the day that arm ships. One
    /// such row ended up asserting the opposite of the mechanism it described.
    /// </para>
    /// <para>
    /// <b>So a row names the arm and never the seat.</b> <c>AfterFailure</c> means the same
    /// thing forever; <i>the shipped timing</i> does not. Introducing this rule turned up
    /// three rows already rotted — two comparing a default against the arm that had since
    /// become it, and one calling a check unarmed that has been armed and load-bearing since.
    /// </para>
    /// </remarks>
    private static readonly (string What, string Pattern)[] Dated =
    [
        ("a default named as a seat rather than an arm", @"(?i)\bthe shipped \w+"),
        ("a claim dated by the session", @"(?i)\b(this|last|next) (session|morning|afternoon|week)\b"),
        ("a claim dated by the day", @"(?i)\b(today|yesterday|tomorrow)\b(?!'s)"),

        // AND THE HISTORICAL RECORD, John's, and it is the same rule the findings check makes
        // about numbers. The commit history is the record; this doc is where the project is
        // going. A stamp says WHEN a decision was taken, which `git blame` answers exactly and
        // this file answers staler every week -- and provenance is the half that does work,
        // because `John's` says who may change a line where a date says nothing.
        //
        // The two below are the same fault wearing prose. A doc narrating its own edits grows
        // a changelog nobody asked for: the ordering carried `reordered` twice in three days,
        // which is a diff written longhand where the list should simply have moved. State what
        // is true and let the history be history.
        ("a date stamp", @"\d{4}-\d{2}-\d{2}"),
        ("a change narrated rather than stated", @"(?i)\bno longer\b|\b(has|have) since\b"),
        ("a doc narrating its own edits", @"(?i)\breordered\b|\bused to\b|\bonce already\b"),
    ];

    [Fact]
    public void No_single_item_outgrows_a_line()
    {
        // John's call, 2026-08-04: cap the item, not the doc. A doc-wide ceiling
        // punishes having twelve ideas, which is the wrong thing to discourage. It
        // made several sessions trim good sentences to afford new ones, and the
        // trimming produced worse prose than one pass would have.
        //
        // What actually goes wrong is an item becoming an essay. Eleven had, and
        // between them they were 38% of the doc while saying what the XML comments
        // beside the code already said better. So the rule is per item: name the
        // thing, say enough to recognise it on return, stop. Twelve new ideas now
        // cost twelve lines and nothing has to be retired to make room.
        var swollen = new List<string>();

        foreach (var path in Directory.EnumerateFiles(Docs(), "*.md"))
            foreach (var item in Items(File.ReadAllText(path)))
            {
                var words = item
                    .Split([' ', '\n', '\r', '\t'], StringSplitOptions.RemoveEmptyEntries)
                    .Length;

                if (words > Item)
                    swollen.Add($"{Path.GetFileName(path)}: {words} words — {Opening(item)}");
            }

        Assert.True(swollen.Count == 0,
            $"{swollen.Count} item(s) over the per-item cap of {Item} words. Say what "
            + "the thing is and enough to recognise it later; the reasoning belongs in "
            + "the XML comment beside the mechanism:\n  "
            + string.Join("\n  ", swollen.Take(10)));
    }

    /// <summary>
    /// The doc as a list of ITEMS — bullets and table rows, each with its wrapped
    /// continuation lines folded back in.
    /// </summary>
    /// <remarks>
    /// <b>A wrapped bullet is ONE item</b>, or the cap would be punishing line width
    /// rather than length. Headings and paragraphs are not items; the prose budget
    /// governs those.
    /// </remarks>
    private static IEnumerable<string> Items(string doc)
    {
        var item = new StringBuilder();

        foreach (var line in doc.Split('\n'))
        {
            var trimmed = line.TrimStart();

            var starts = trimmed.StartsWith("- ", StringComparison.Ordinal)
                || (trimmed.StartsWith('|')
                    && !trimmed.StartsWith("|---", StringComparison.Ordinal));

            if (starts)
            {
                if (item.Length > 0) yield return item.ToString();
                item.Clear();
                item.Append(trimmed);
            }
            else if (item.Length > 0 && line.StartsWith("  ", StringComparison.Ordinal))
            {
                item.Append(' ').Append(trimmed);
            }
            else if (item.Length > 0)
            {
                yield return item.ToString();
                item.Clear();
            }
        }

        if (item.Length > 0) yield return item.ToString();
    }

    /// <summary>Enough of an item to find it by.</summary>
    private static string Opening(string item) =>
        item.Length <= 60 ? item : string.Concat(item.AsSpan(0, 60), "...");

    [Fact]
    public void The_item_cap_can_tell_a_wrapped_bullet_from_a_long_one()
    {
        // THE COMPANION, and without it the cap above passes for a reader that
        // splits every bullet at its line breaks and therefore never sees a long
        // one. Two lines of one item must count as one item of both.
        var wrapped = Items("- one two three\n  four five six\n").Single();

        Assert.Equal("- one two three four five six", wrapped);

        Assert.Equal(2, Items("- one\n- two\n").Count());
        Assert.Equal(2, Items("| a | b |\n|---|---|\n| c | d |\n").Count());
    }

    [Fact]
    public void The_doc_holds_these_sections_and_no_others()
    {
        // The other half of capping the item rather than the doc. Nothing else stops
        // a new prose section appearing beside the lists — which is exactly how
        // "What a whole session of this says" arrived, 289 words of findings in the
        // one doc whose own first rule is that findings live in the commit.
        //
        // Adding a section is a decision and should cost a deliberate edit here.
        var found = File.ReadLines(Path.Combine(Docs(), "plan.md"))
            .Where(line => line.StartsWith("## ", StringComparison.Ordinal))
            .Select(line => line[3..].Trim())
            .ToList();

        Assert.Equal(Sections, found);
    }

    [Fact]
    public void The_whole_doc_still_fits_in_one_reading()
    {
        // The budget that was missing, and its absence is why this doc reached twenty-five
        // thousand words with every other check green. Read the constant's remarks: the rule
        // is John's and it is about whether a session LOADS the thing, not about tidiness.
        var words = Directory
            .EnumerateFiles(Docs(), "*.md")
            .Sum(path => File.ReadAllText(path)
                .Split([' ', '\n', '\r', '\t'], StringSplitOptions.RemoveEmptyEntries)
                .Length);

        Assert.True(words <= Whole,
            $"the plan is {words} words against a budget of {Whole}. It is meant to be read "
            + "WHOLE at the start of a session, so this is the check that says it still can "
            + "be. Move a finding to its commit and its test, a mechanism to the XML comment "
            + "beside it, a trap to the check that catches it -- and never delete one item "
            + "to afford another.");
    }

    [Fact]
    public void The_doc_is_mostly_structure_and_not_mostly_prose()
    {
        var wordy = Directory
            .EnumerateFiles(Docs(), "*.md")
            .Select(path => (
                Name: Path.GetFileName(path),
                Words: File.ReadAllLines(path)
                    .Where(line => !Structural(line))
                    .Sum(line => line.Split(
                        [' ', '\t'], StringSplitOptions.RemoveEmptyEntries).Length)))
            .Where(doc => doc.Words > Prose)
            .ToList();

        Assert.True(wordy.Count == 0,
            "over the prose budget of " + Prose + " words: "
            + string.Join(", ", wordy.Select(doc => $"{doc.Name} at {doc.Words}"))
            + ". Turn a paragraph into bullets rather than deleting a list item — "
            + "the connective tissue is what can be rederived.");
    }

    [Fact]
    public void The_prose_check_can_still_tell_the_two_apart()
    {
        // THE COMPANION, and without it the check above passes for a predicate
        // that calls everything structural.
        Assert.True(Structural("- a bullet"));
        Assert.True(Structural("| a | table | row |"));
        Assert.True(Structural("## a heading"));
        Assert.True(Structural("  a wrapped bullet"));
        Assert.True(Structural("1. an ordered item"));

        Assert.False(Structural("A sentence that is just a sentence."));
        Assert.False(Structural("**Bold prose is still prose.**"));
    }

    [Fact]
    public void There_is_still_only_one_doc()
    {
        // The companion, and without it the budget is trivial to defeat: split the
        // doc in two and every file is comfortably under the cap while the total
        // is unchanged. A second doc is a decision, not an accident, so it should
        // cost a deliberate edit here.
        var docs = Directory.EnumerateFiles(Docs(), "*.md").Select(Path.GetFileName).ToList();

        Assert.Equal(["plan.md"], docs);
    }

    [Fact]
    public void The_plan_looks_forward_and_records_no_findings()
    {
        // John's call, 2026-08-03: the plan is where the project is going, and a
        // result is something that already happened. The two were mixed, and the
        // findings won -- roughly half the doc was scores, and the sections
        // saying what to build next were the ones getting compacted to make room
        // under the word budget.
        //
        // Worse, a finding written here goes stale silently. The commit that
        // produced a number is the honest home for it, the comment beside the
        // mechanism is where anyone touching that mechanism will actually see it,
        // and the test that asserts it is the only copy that cannot drift.
        //
        // The guards are not findings. `Do not re-try` and `TRAPS` say what not
        // to do, which is a forward-facing instruction -- so they stay, and this
        // is what keeps their evidence column a reason rather than a readout.
        var plan = Plan();
        var lines = plan.Split('\n');

        var recorded = new List<string>();

        foreach (var (what, pattern) in Findings)
            foreach (var line in lines)
                if (Regex.IsMatch(line, pattern))
                    recorded.Add($"{what}: {line.Trim()}");

        Assert.True(recorded.Count == 0,
            "the plan records findings, and it is meant to be forward-facing. "
            + "Put the number in the commit, in the XML comment beside the "
            + "mechanism, or in the test that asserts it:\n"
            + string.Join("\n", recorded.Take(10)));
    }

    [Fact]
    public void No_row_is_dated_rather_than_stated()
    {
        // A row that names a seat instead of an arm reverses itself when the seat changes
        // hands, and it does so silently -- nothing goes red, the sentence still parses, and
        // it now says the opposite of what it was written to say. That is worse than a stale
        // number, which at least looks like a number nobody has re-taken.
        var lines = Plan().Split('\n');

        var dated = new List<string>();

        foreach (var (what, pattern) in Dated)
            foreach (var line in lines)
                if (Regex.IsMatch(line, pattern))
                    dated.Add($"{what}: {line.Trim()}");

        Assert.True(dated.Count == 0,
            "the plan dates a claim instead of stating it. Name the arm, not the seat it "
            + "currently holds -- `AfterFailure` means the same thing forever and `the "
            + "shipped timing` does not:\n" + string.Join("\n", dated.Take(10)));
    }

    [Fact]
    public void The_dating_check_can_still_fail()
    {
        // The companion, for the same reason the one below has one. A pattern set that
        // matches nothing passes forever and reads exactly like a doc with no dated claims
        // in it.
        var dated = new[]
        {
            "Free under the shipped timing carries no hard round at all",
            "which is what this session's grid settled",
            "`Surprise` is one, today",
            "the order of the work is John's, 2026-08-16 and reordered after",
            "genesis no longer roots on a code that never varied",
            "the binding world failed and has since lifted",
        };

        Assert.All(dated, line => Assert.True(
            Dated.Any(rule => Regex.IsMatch(line, rule.Pattern)),
            $"nothing in the rule set notices this is dated: {line}"));

        // And every rule is tripped by one of them, which the direction above cannot say. A
        // pattern added without an example beside it is exercised by nothing, passes forever,
        // and reads exactly like a rule that is working -- and three were added at once here,
        // which is how a set drifts into decoration one entry at a time.
        Assert.All(Dated, rule => Assert.True(
            dated.Any(line => Regex.IsMatch(line, rule.Pattern)),
            $"no example trips this rule, so nothing exercises it: {rule.What}"));

        // And a row that merely mentions a day is not dated, which is what the lookahead is
        // for. Asserted, because a rule that reddens on ordinary prose gets deleted rather
        // than obeyed.
        Assert.DoesNotContain(Dated, rule =>
            Regex.IsMatch("the depth cap is why it is not today's problem", rule.Pattern));

        // Nor is a corpus name that happens to carry digits and dashes, which is the way the
        // date rule could most easily have been written too wide.
        Assert.DoesNotContain(Dated, rule =>
            Regex.IsMatch("`tasks_1-20_v1-2` is enumerable and stays", rule.Pattern));
    }

    [Fact]
    public void The_forward_facing_check_can_still_fail()
    {
        // The companion, and without it the check above passes for a pattern set
        // that matches nothing. Every rule is asserted against a line that must
        // trip it, so a regex quietly broken by an edit is caught here rather
        // than by the doc slowly refilling with results.
        var findings = new[]
        {
            "Binding — 0.5240, now 0.8798 on the world built to be impossible",
            "0.8077 ± 0.0215 against a chance of 0.0833",
            "12.2 sigma apart, 25.7 clear of chance",
            "| **12** | ✅ CLOSED by 22's fix |",
            "5,000,003 messages against 1,111 on a 12-clique",
        };

        Assert.All(findings, line => Assert.True(
            Findings.Any(rule => Regex.IsMatch(line, rule.Pattern)),
            $"nothing in the rule set notices this is a finding: {line}"));
    }

    [Fact]
    public void Every_fork_the_code_cites_is_in_the_index()
    {
        // The ghost-reference problem that has bitten this project before, which
        // is why forks are deliberately never renumbered. The code cites fork
        // numbers in a dozen places; this asserts each one still resolves.
        var plan = Plan();

        var listed = Regex
            .Matches(plan, @"\*\*(\d{1,3})\*\*")
            .Select(match => match.Groups[1].Value)
            .ToHashSet(StringComparer.Ordinal);

        Assert.NotEmpty(listed);

        var cited = new SortedSet<string>(StringComparer.Ordinal);

        foreach (var path in Tree.Sources("src"))
            // Three digits and anchored, because two silently truncated the first fork past
            // ninety-nine. `fork 106` matched as `10`, so the check reported a dangling
            // citation of a fork nobody had written about while the one actually cited went
            // unchecked -- a guard describing a repo that is not there, which is the fault
            // this file exists to catch in other people's work.
            foreach (Match match in Regex.Matches(File.ReadAllText(path), @"[Ff]ork (\d{1,3})\b"))
                cited.Add(match.Groups[1].Value);

        Assert.NotEmpty(cited);

        var dangling = cited.Where(number => !listed.Contains(number)).ToList();

        Assert.True(dangling.Count == 0,
            $"the code cites forks the index does not list: {string.Join(", ", dangling)}");
    }

    /// <summary>The test files, by the name a comment would call them.</summary>
    private static HashSet<string> Suites() =>
        Tree.Sources("tests")
            .Select(Path.GetFileNameWithoutExtension)
            .Where(name => name is not null && name.EndsWith("Tests", StringComparison.Ordinal))
            .Select(name => name!)
            .ToHashSet(StringComparer.Ordinal);

    [Fact]
    public void Every_suite_the_library_names_in_a_comment_still_exists()
    {
        // The same ghost reference as the fork check, through the one door it leaves open.
        // A `<see cref="..."/>` is enforced by the compiler and a fork number by the check
        // above; a suite named in PROSE is enforced by nothing at all. So a library comment
        // saying *measured in `SomeTests`* keeps compiling forever after that file is
        // renamed, and the reader who goes looking finds nothing and cannot tell whether
        // the measurement moved or never existed.
        //
        // And it is a real path rather than a hypothetical one. Comments in this library
        // now cite suites for their numbers -- that is deliberate, since the plan forbids
        // findings living in the doc, so the citation is how a mechanism points at its own
        // evidence. Which makes the citation load-bearing and therefore worth a budget.
        var suites = Suites();

        Assert.NotEmpty(suites);

        var cited = new SortedSet<string>(StringComparer.Ordinal);

        foreach (var path in Tree.Sources("src"))
            foreach (Match match in Regex.Matches(File.ReadAllText(path), @"\b(\w+Tests)\b"))
                cited.Add(match.Groups[1].Value);

        // NOT `Assert.NotEmpty` ON THE CITATIONS, because a library that names no suite is
        // a perfectly good library and this check has nothing to say about it. The fork
        // check can demand citations exist; this one may only demand they resolve.
        var dangling = cited.Where(name => !suites.Contains(name)).ToList();

        Assert.True(dangling.Count == 0,
            "the library names suites that do not exist — either the file was renamed and "
            + "the comment was not, or the measurement was never written: "
            + string.Join(", ", dangling));
    }

    [Fact]
    public void The_suite_check_can_still_fail()
    {
        // The companion, because a lookup over an empty set passes everything. If `Suites`
        // returned nothing the check above would find every citation dangling and fail
        // loudly, which is the safe direction -- but if its matching were loose enough to
        // accept anything, it would pass in silence. This pins both ends.
        var suites = Suites();

        Assert.Contains(nameof(DocsTests), suites);
        Assert.DoesNotContain("AWeatherBalloonTests", suites);
    }

    [Fact]
    public void Every_refuted_row_says_what_would_revive_it()
    {
        // A refutation is conditional on its configuration, and this project has
        // already had to revive two arms whose reason for being dead had quietly
        // expired -- the empty-cell workaround and the temporal window. A row
        // without a revival condition is a superstition rather than a finding,
        // so the shape is enforced rather than encouraged.
        var lines = Plan().Split('\n');

        var start = Array.FindIndex(lines, line =>
            line.StartsWith("## DO NOT RE-TRY", StringComparison.Ordinal));

        Assert.True(start >= 0, "the refuted section is gone");

        var rows = lines
            .Skip(start)
            .TakeWhile(line => !line.StartsWith("## ", StringComparison.Ordinal) || line == lines[start])
            .Where(line => line.StartsWith("| ", StringComparison.Ordinal))
            .Where(line => !line.Contains("---", StringComparison.Ordinal))
            .Skip(1)
            .ToList();

        Assert.NotEmpty(rows);

        var malformed = rows
            .Where(row => row.Split('|', StringSplitOptions.TrimEntries)
                .Where(cell => cell.Length > 0).Count() != 3)
            .ToList();

        Assert.True(malformed.Count == 0,
            "a refuted row must be `what | what refuted it | what would revive it`, " +
            $"all on one line: {string.Join(" // ", malformed)}");
    }

    [Fact]
    public void The_library_is_built_with_the_doc_contract_switched_on()
    {
        // The check that protects the other check. Everything above assumes the
        // compiler is enforcing the XML comments; someone removing
        // GenerateDocumentationFile to quiet a warning would silently take the
        // real doc check with it, and nothing else would notice.
        var project = File.ReadAllText(
            Path.Combine(Repo(), "src", "OpenPlexus", "OpenPlexus.csproj"));

        Assert.Contains("<GenerateDocumentationFile>true", project, StringComparison.Ordinal);

        // And the assembly's own XML file is beside it, which is the same claim
        // made against the build output rather than against the intent.
        Assert.True(
            File.Exists(Path.Combine(AppContext.BaseDirectory, "OpenPlexus.xml")),
            "the library built without its documentation file");
    }
}
