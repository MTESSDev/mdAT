using System.Globalization;
using System.Reflection;

namespace MDAT.Tests;

/// <summary>A case marked <c>skipped</c>, or left out by a <c>selected</c> one, is listed and reported as ignored, never run.</summary>
public class SkippedCaseTests
{
    // Same signature as the tests of the markdown files written below
    public void Some_method(int number, string text, DateOnly day, int? optional) { }

    private static MethodInfo SomeMethod => typeof(SkippedCaseTests).GetMethod(nameof(Some_method))!;

    private static IReadOnlyList<MarkdownTestCase> Read(string markdown)
    {
        var folder = Directory.CreateTempSubdirectory("mdat-skipped-");

        try
        {
            var path = Path.Combine(folder.FullName, "cases.md");
            File.WriteAllText(path, markdown);

            return new MarkdownTestSource(path).GetCases(SomeMethod);
        }
        finally
        {
            folder.Delete(recursive: true);
        }
    }

    private const string Cases = "# Cases\n\n"
        + "## Runs\n\n``````yaml\nnumber: 1\ntext: a\nday: 2024-05-06\noptional: 2\n``````\n\n"
        + "## Ignored\n\n``````yaml skipped\nnumber: 3\ntext: b\nday: 2024-05-07\noptional: 4\n``````\n\n"
        + "## Unfinished\n\n``````yaml skipped\nthis: is not even: valid [yaml\n``````\n";

    // a case that is not read (it is broken), one marked skipped, and the selected one that is last
    private const string WithSelected = "# Cases\n\n"
        + "## First\n\n``````yaml\nnumber: 1\ntext: a\nday: 2024-05-06\noptional: 2\n``````\n\n"
        + "## Broken\n\n``````yaml\nthis: is not even: valid [yaml\n``````\n\n"
        + "## Marked skipped\n\n``````yaml skipped\nnumber: 3\n``````\n\n"
        + "## Only me\n\n``````yaml selected\nnumber: 9\ntext: z\nday: 2024-05-08\noptional: 1\n``````\n";

    [Test]
    public async Task A_skipped_case_is_listed_with_the_reason_it_is_ignored()
    {
        var cases = Read(Cases);

        await Assert.That(cases.Count).IsEqualTo(3);
        await Assert.That(cases.Select(c => c.DisplayName)).IsEquivalentTo(new[] { "Some_method_Cases_Runs@cases.md", "Some_method_Cases_Ignored@cases.md", "Some_method_Cases_Unfinished@cases.md" });

        await Assert.That(cases[0].SkipReason).IsNull();
        await Assert.That(cases[1].SkipReason).IsEqualTo(MarkdownTestSource.SkippedReason);
        await Assert.That(cases[2].SkipReason).IsEqualTo(MarkdownTestSource.SkippedReason);
    }

    [Test]
    [Arguments("fr-CA", "Marqué comme ignoré (skipped) dans le fichier Markdown")]
    [Arguments("fr-FR", "Marqué comme ignoré (skipped) dans le fichier Markdown")]
    [Arguments("en-US", "Marked as skipped in the markdown file")]
    [Arguments("de-DE", "Marked as skipped in the markdown file")]
    public async Task The_reason_is_in_the_language_of_the_computer(string culture, string expected)
    {
        var previous = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = new CultureInfo(culture);

        try
        {
            await Assert.That(Read(Cases)[1].SkipReason).IsEqualTo(expected);
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }

    [Test]
    public async Task The_arguments_of_a_skipped_case_match_the_parameters_of_the_method()
    {
        var skipped = Read(Cases)[1];

        // the YAML of a skipped case is not read: default values, which the method never receives
        await Assert.That(skipped.Arguments.Length).IsEqualTo(4);
        await Assert.That(skipped.Arguments[0]).IsEqualTo(0);
        await Assert.That(skipped.Arguments[1]).IsNull();
        await Assert.That(skipped.Arguments[2]).IsEqualTo(default(DateOnly));
        await Assert.That(skipped.Arguments[3]).IsNull();
    }

    [Test]
    public async Task The_case_that_runs_keeps_its_values()
    {
        var cases = Read(Cases);

        await Assert.That(cases[0].Arguments[0]).IsEqualTo(1);
        await Assert.That(cases[0].Arguments[1]).IsEqualTo("a");
    }

    [Test]
    public async Task When_a_case_is_selected_the_others_stay_in_the_list_as_ignored()
    {
        var cases = Read(WithSelected);

        await Assert.That(cases.Count).IsEqualTo(4);

        // the others are not read (the broken one does not fail the file) and are ignored with the reason that fits
        await Assert.That(cases[0].SkipReason).IsEqualTo(MarkdownTestSource.NotSelectedReason);
        await Assert.That(cases[1].SkipReason).IsEqualTo(MarkdownTestSource.NotSelectedReason);
        await Assert.That(cases[2].SkipReason).IsEqualTo(MarkdownTestSource.SkippedReason);

        // the selected one runs, with its values
        await Assert.That(cases[3].SkipReason).IsNull();
        await Assert.That(cases[3].Arguments[0]).IsEqualTo(9);
        await Assert.That(cases[3].Arguments[1]).IsEqualTo("z");
    }

    [Test]
    public async Task A_case_both_selected_and_skipped_is_ignored_and_selects_nothing()
    {
        var cases = Read(Cases.Replace("``````yaml skipped\nnumber: 3", "``````yaml selected skipped\nnumber: 3"));

        // nothing else is selected: the case that runs still runs
        await Assert.That(cases[0].SkipReason).IsNull();
        await Assert.That(cases[1].SkipReason).IsEqualTo(MarkdownTestSource.SkippedReason);
    }

    [Test]
    [Arguments("fr-CA", "Un autre cas est marqué selected dans le fichier Markdown")]
    [Arguments("en-US", "Another case is marked selected in the markdown file")]
    public async Task The_reason_of_a_case_that_is_not_selected_is_in_the_language_of_the_computer(string culture, string expected)
    {
        var previous = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = new CultureInfo(culture);

        try
        {
            await Assert.That(Read(WithSelected)[0].SkipReason).IsEqualTo(expected);
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }
}
