namespace OpenPlexus.Tests;

/// <summary>
/// <b>A fact that checks nothing is a measurement, and a measurement belongs to
/// <c>sweeps.yml</c> RATHER THAN TO EVERY PUSH.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>The budget for the failure class that cost forty-five minutes a push.</b>
/// <c>RepairingTests</c> held six facts and not one assertion — eight seeds at twenty
/// thousand rounds an arm, writing rows to a test output nobody reads on CI, seventeen
/// minutes of every run. It was a sweep in everything but the trait, and
/// <c>WideningTests</c> beside it carries that trait for grids of identical shape.
/// </para>
/// <para>
/// <b>And nothing could see it, which is why this is a check rather than a fix.</b> A fact
/// with no assertion is GREEN — greener than a real test, since it cannot fail. It reads as
/// coverage, it runs forever, and the only symptom is a clock. <c>SweepListTests</c> guards
/// the sweeps that declare themselves and <c>ShardTests</c> guards where a class runs;
/// neither can notice a measurement that never said it was one.
/// </para>
/// <para>
/// <b>THE RULE IS DELIBERATELY SYNTACTIC.</b> The point is not to judge whether an assertion
/// is a good one — it is that SOMETHING must be able to fail. A grid may keep every row it
/// prints; it just has to say it is a grid.
/// </para>
/// </remarks>
public sealed class CheckingTests
{
    /// <summary>What counts as a test being able to fail.</summary>
    /// <remarks>
    /// <b>`Assert` covers xunit, and the others are the ways this repo already writes one.</b>
    /// A helper that asserts on the caller's behalf still names one of these inside itself,
    /// and the check reads the helper's own body as well — so a fact delegating its whole
    /// verification to a private method is not flagged.
    /// </remarks>
    private static readonly string[] Checks = ["Assert.", "Assert)", "Throws", "Verify("];

    /// <summary>How a measurement announces itself: it PRINTS.</summary>
    /// <remarks>
    /// <para>
    /// <b>The first version of this flagged every fact with no assertion and was wrong on
    /// its first run.</b> <c>ClusterTests.Two_nodes_that_are_partners_can_fire_at_once</c>
    /// asserts nothing and is a perfectly good test: it fires sixty-four deliveries at a
    /// mutual pair and fails by DEADLOCKING, which its <c>WaitAsync</c> turns into a thrown
    /// timeout. A test whose failure mode is an exception can fail, so the rule <i>no
    /// assertion means no test</i> is simply false.
    /// </para>
    /// <para>
    /// <b>So the rule reads the tell instead of the absence.</b> What a measurement does that
    /// a test never does is WRITE A ROW: a grid exists to print arms against columns for a
    /// human to read later. A fact that prints and cannot fail is a sweep, and that pair is
    /// exactly what <c>RepairingTests</c> was for seventeen minutes of every push.
    /// </para>
    /// </remarks>
    private static readonly string[] Prints = ["WriteLine", "Write("];

    /// <summary>
    /// The file holding this check's own specimens, which are deliberately faulty.
    /// </summary>
    /// <remarks>
    /// <b>A check that reads source must exempt its own companion</b>, or the fixture proving
    /// it can fail becomes the thing it fails on. The exemption is one file by name rather
    /// than a pattern, so nothing else can drift under it.
    /// </remarks>
    private const string Specimens = "CheckingTests.cs";

    /// <summary>The trait that says a method is a measurement rather than a test.</summary>
    private const string Declared = "Trait(Sweeps.Kind, Sweeps.Name)";

    [Fact]
    public void Every_fact_either_checks_something_or_declares_itself_a_sweep()
    {
        var silent = new List<string>();

        foreach (var path in Tree.Sources("tests"))
        {
            if (Path.GetFileName(path) == Specimens) continue;

            var text = File.ReadAllText(path);

            // THE HELPERS COUNT, because a fact whose verification lives in a private method
            // of the same file is checking something -- it is only the whole FILE having no
            // assertion anywhere that would make that reading wrong, and that case is caught
            // by the fact's own body being empty of one too.
            var helpers = Helpers(text);

            foreach (var fact in Facts(text))
            {
                if (fact.Sweep) continue;

                if (!Prints.Any(print =>
                        fact.Body.Contains(print, StringComparison.Ordinal)))
                    continue;

                if (Checks.Any(check =>
                        fact.Body.Contains(check, StringComparison.Ordinal))
                    || helpers.Any(helper =>
                        fact.Body.Contains(helper, StringComparison.Ordinal)))
                    continue;

                silent.Add($"{Path.GetFileName(path)}: {fact.Name}");
            }
        }

        Assert.True(silent.Count == 0,
            $"{silent.Count} fact(s) print a row, cannot fail, and are not marked as sweeps "
            + "— which is a measurement running on every push. Either check what it "
            + $"measures, or give it `[{Declared}]` and an entry in `sweeps.yml` so it runs "
            + "when it is asked for:\n  " + string.Join("\n  ", silent.Take(10)));
    }

    /// <summary>
    /// How a naming count is printed — <b>the numerator, in the two spellings this repo
    /// has.</b>
    /// </summary>
    /// <remarks>
    /// <b>The terminators are what keep this off three unrelated ideas called `Named`.</b>
    /// <c>Joining.Named</c> is a front-end arm, <c>Cifar.Named</c> and <c>Encoded.Named</c>
    /// are label lookups, and <c>Roaming.Named</c> is a list of rooms — the repo's own trap
    /// about two ideas sharing one word, live in the check that would otherwise trip on it.
    /// A count reaches output through an interpolation hole, so it is followed by a brace, a
    /// comma or a format specifier; a method call is followed by <c>(</c> and an indexer by
    /// <c>[</c>.
    /// </remarks>
    private static readonly string[] Counts = ["Names.Count", ".Named}", ".Named,", ".Named:"];

    /// <summary>What makes a naming count readable.</summary>
    /// <remarks>
    /// <b>Any one of them, because they answer the same question three ways.</b>
    /// <c>Eligible</c> is how many scopes were there to name, <c>Asked</c> is how many
    /// chances the gate got, and <c>Speaking</c> and <c>PerEligible</c> are those two as
    /// shares. The check does not care which a grid prints, only that the numerator is not
    /// alone.
    /// </remarks>
    private static readonly string[] Denominators =
        ["Eligible", "Asked", "Speaking", "PerEligible", "Stackable"];

    /// <summary>
    /// A printed naming count carries what it is a count OF.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The budget for a rate cap that was read as a finding.</b> Rung five is asked once
    /// a sweep and answers with one pair, so twenty thousand rounds at a sweep of a thousand
    /// cannot mint more than twenty names. Eight cells of a grid came back at exactly
    /// seventeen across two tasks, two spans and two capacities, and that constant was
    /// written up as a result — it was seventeen of twenty asks, and no dial in the grid
    /// could have moved it.
    /// </para>
    /// <para>
    /// <b>And the trap it belongs to is already written down</b>: an exact partition of what
    /// arrived says nothing about what never did. <c>Tally.Eligible</c> and
    /// <c>Tally.Asked</c> existed the whole time and no grid printed either, so every naming
    /// reading on this branch was a numerator.
    /// </para>
    /// <para>
    /// <b>It reads the whole method rather than the one statement</b>, which is deliberately
    /// loose. Where a denominator goes in a row is a layout choice and a grid printing it in
    /// a second line is not the fault being guarded; printing no denominator anywhere in the
    /// measurement is.
    /// </para>
    /// </remarks>
    [Fact]
    public void Every_printed_naming_count_has_a_denominator()
    {
        var bare = new List<string>();

        foreach (var path in Tree.Sources("tests"))
        {
            if (Path.GetFileName(path) == Specimens) continue;

            var text = File.ReadAllText(path);

            foreach (var fact in Facts(text))
            {
                // Inside an interpolation and nowhere else, which is what separates a
                // reading from an invariant. `CensusTests` asserts two arms mint the same
                // number and `WithheldTests` asserts an examination moves it not at all --
                // neither is a magnitude anybody reads, so neither wants a denominator, and
                // the first version of this check flagged both.
                var printed = fact.Body
                    .Split('\n')
                    .Where(line => line.Contains("$\"", StringComparison.Ordinal))
                    .Where(line => Counts.Any(count =>
                        line.Contains(count, StringComparison.Ordinal)))
                    .ToList();

                if (printed.Count == 0) continue;

                if (Denominators.Any(against =>
                        fact.Body.Contains(against, StringComparison.Ordinal)))
                    continue;

                bare.Add($"{Path.GetFileName(path)}: {fact.Name}");
            }
        }

        Assert.True(bare.Count == 0,
            $"{bare.Count} measurement(s) print a naming count with nothing to read it "
            + "against. An absolute name count is capped by the sweep calendar rather than "
            + "by the gate, so two cells can report the same number for opposite reasons. "
            + "Print `Tally.Eligible`, `Tally.Asked`, `Tally.Speaking` or "
            + $"`Tally.PerEligible` beside it:\n  " + string.Join("\n  ", bare.Take(10)));
    }

    /// <summary>The private methods of a file that assert, by name.</summary>
    private static IEnumerable<string> Helpers(string text)
    {
        foreach (var (name, body, _, _) in Methods(text))
            if (Checks.Any(check => body.Contains(check, StringComparison.Ordinal)))
                yield return name;
    }

    /// <summary>Every `[Fact]` or `[Theory]` in a file, with its body and its trait.</summary>
    private static IEnumerable<(string Name, string Body, bool Sweep, bool Fact)> Facts(
        string text) => Methods(text).Where(method => method.Fact);

    /// <summary>
    /// Every method of a file: its name, its body, and whether it is a declared sweep.
    /// </summary>
    /// <remarks>
    /// <b>Brace-matched rather than split on the next attribute.</b> Taking the text between
    /// one fact and the next would fold any helper sitting between them into the first
    /// fact's body, and a helper that asserts would then excuse a fact that does not — which
    /// is the exact case this check exists to catch, forgiven by its own reader.
    /// </remarks>
    private static IEnumerable<(string Name, string Body, bool Sweep, bool Fact)> Methods(
        string text)
    {
        var lines = text.Split('\n');

        for (var index = 0; index < lines.Length; index++)
        {
            var line = lines[index].Trim();

            if (!line.StartsWith("public ", StringComparison.Ordinal)
                && !line.StartsWith("private ", StringComparison.Ordinal)) continue;

            var open = line.IndexOf('(');

            if (open < 0 || line.Contains(" class ", StringComparison.Ordinal)) continue;

            var before = line[..open];
            var name = before[(before.LastIndexOf(' ') + 1)..];

            if (name.Length == 0) continue;

            // THE ATTRIBUTES ABOVE, walked back over comments and blank lines so a doc
            // comment between `[Fact]` and the signature cannot hide the trait.
            var fact = false;
            var sweep = false;

            for (var above = index - 1; above >= 0; above--)
            {
                var previous = lines[above].Trim();

                if (previous.Length == 0
                    || previous.StartsWith("///", StringComparison.Ordinal)
                    || previous.StartsWith("//", StringComparison.Ordinal)) continue;

                if (!previous.StartsWith('[')) break;

                if (previous.StartsWith("[Fact", StringComparison.Ordinal)
                    || previous.StartsWith("[Theory", StringComparison.Ordinal)) fact = true;

                if (previous.Contains(Declared, StringComparison.Ordinal)) sweep = true;
            }

            yield return (name, Body(lines, index), sweep, fact);
        }
    }

    /// <summary>The body of the method starting at <paramref name="index"/>.</summary>
    private static string Body(string[] lines, int index)
    {
        var body = new System.Text.StringBuilder();
        var depth = 0;
        var started = false;

        for (var at = index; at < lines.Length; at++)
        {
            body.Append(lines[at]).Append('\n');

            foreach (var character in lines[at])
            {
                if (character == '{') { depth++; started = true; }
                else if (character == '}') depth--;
            }

            // An expression body never opens a brace, so a `=> Something();` method would
            // otherwise swallow the rest of the file.
            if (!started && lines[at].Contains(';', StringComparison.Ordinal)) break;

            if (started && depth <= 0) break;
        }

        return body.ToString();
    }

    [Fact]
    public void The_check_can_still_tell_a_silent_fact_from_a_checking_one()
    {
        // THE COMPANION, and this file needs one more than most: a reader that finds no
        // facts at all passes every file in the tree and reads exactly like a suite with
        // nothing wrong in it.
        const string source = """
            public sealed class Thing
            {
                [Fact]
                public void It_checks() { Assert.True(true); }

                [Fact]
                public void It_measures() { output.WriteLine("a row"); }

                [Fact]
                [Trait(Sweeps.Kind, Sweeps.Name)]
                public void It_is_a_declared_grid() { output.WriteLine("a row"); }

                private void Helping() { Assert.True(true); }

                [Fact]
                public void It_delegates() { Helping(); }
            }
            """;

        var facts = Facts(source).ToList();

        Assert.Equal(4, facts.Count);
        Assert.Contains(facts, fact => fact.Name == "It_measures" && !fact.Sweep);
        Assert.Contains(facts, fact => fact.Name == "It_is_a_declared_grid" && fact.Sweep);

        // And the tell is the printing rather than the missing assertion, which is the
        // correction the first run forced: a fact that neither prints nor asserts may still
        // fail by throwing, and one of those is a real deadlock test in this suite.
        Assert.Contains(
            Prints, print => facts.Single(fact => fact.Name == "It_measures").Body
                .Contains(print, StringComparison.Ordinal));

        Assert.DoesNotContain(
            Prints, print => facts.Single(fact => fact.Name == "It_checks").Body
                .Contains(print, StringComparison.Ordinal));

        // AND THE HELPER IS FOUND, so a fact that verifies through one is not flagged.
        Assert.Contains("Helping", Helpers(source));

        // AND THE BRACE MATCHING HOLDS: the measuring fact must not have swallowed the
        // helper below it, which is what excused this whole class of fault before.
        var measuring = facts.Single(fact => fact.Name == "It_measures");

        Assert.DoesNotContain("Assert.", measuring.Body, StringComparison.Ordinal);
    }
}
