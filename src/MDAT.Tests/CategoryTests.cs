using System.Reflection;

namespace MDAT.Tests;

/// <summary>
/// The tags of a case: <c>category=Smoke,Fast priority=High</c> in the line of its code block, added to <c>tags:</c> in the
/// front matter of the file. They are the categories and the properties of the test in the test explorer.
/// </summary>
public class CategoryTests
{
    /// <summary>What TUnit reports for each case of <c>test-categories.md</c>.</summary>
    [Test]
    [MarkdownDataSource("~/Tests/test-categories.md")]
    public async Task Md_Categories(string name, string expectedCategories, string expectedProperties)
    {
        // the cases of the file marked skipped are ignored: if one of them reaches this method, it is a mistake
        await Assert.That(new[] { "ignored", "ignored too", "unfinished" }.Contains(name)).IsFalse();

        var details = TestContext.Current!.Metadata.TestDetails;

        var categories = details.Categories.OrderBy(c => c, StringComparer.Ordinal);
        var properties = details.CustomProperties.SelectMany(p => p.Value.Select(value => $"{p.Key}={value}")).OrderBy(p => p, StringComparer.Ordinal);

        await Assert.That(string.Join(",", categories)).IsEqualTo(expectedCategories);
        await Assert.That(string.Join(";", properties)).IsEqualTo(expectedProperties);
    }

    /// <summary>
    /// <c>test-categories-selected.md</c> has a selected case: it is the only one that runs, the others are ignored
    /// (if one of them ran, its name would not be "focus").
    /// </summary>
    [Test]
    [MarkdownDataSource("~/Tests/test-categories-selected.md")]
    public async Task Md_Selected(string name, string expectedCategories)
    {
        var categories = TestContext.Current!.Metadata.TestDetails.Categories.OrderBy(c => c, StringComparer.Ordinal);

        await Assert.That(name).IsEqualTo("focus");
        await Assert.That(string.Join(",", categories)).IsEqualTo(expectedCategories);
    }

    [Test]
    public async Task The_ignored_cases_of_the_real_files_are_listed_with_their_tags()
    {
        var all = new MarkdownTestSource("~/Tests/test-categories.md").GetCases(typeof(CategoryTests).GetMethod(nameof(Md_Categories))!);
        var ignored = all.Where(c => c.SkipReason is not null).ToList();

        await Assert.That(all.Count).IsEqualTo(6);
        await Assert.That(ignored.Count).IsEqualTo(3);
        await Assert.That(ignored.All(c => c.SkipReason == MarkdownTestSource.SkippedReason)).IsTrue();

        // an ignored case keeps the tags of the file and its own, wherever they are written in the line
        await Assert.That(ignored[0].Categories).IsEquivalentTo(new[] { "Facturation", "Lent" });
        await Assert.That(ignored[1].Categories).IsEquivalentTo(new[] { "Facturation", "Lent" });
        await Assert.That(ignored[1].Properties.Select(p => $"{p.Key}={p.Value}")).IsEquivalentTo(new[] { "domaine=Taxes", "priorite=Basse" });
        await Assert.That(ignored[2].Categories).IsEquivalentTo(new[] { "Facturation", "Brouillon" });
    }

    [Test]
    public async Task In_the_real_file_with_a_selected_case_only_that_case_runs()
    {
        var all = new MarkdownTestSource("~/Tests/test-categories-selected.md").GetCases(typeof(CategoryTests).GetMethod(nameof(Md_Selected))!);

        await Assert.That(all.Count).IsEqualTo(4);
        await Assert.That(all.Count(c => c.SkipReason is null)).IsEqualTo(1);

        await Assert.That(all[0].SkipReason).IsEqualTo(MarkdownTestSource.NotSelectedReason);
        await Assert.That(all[1].SkipReason).IsNull();
        await Assert.That(all[2].SkipReason).IsEqualTo(MarkdownTestSource.SkippedReason);
        await Assert.That(all[3].SkipReason).IsEqualTo(MarkdownTestSource.NotSelectedReason);

        // the ignored cases keep their categories (those of the file and their own)
        await Assert.That(all[0].Categories).IsEquivalentTo(new[] { "Facturation", "Autre" });
        await Assert.That(all[2].Categories).IsEquivalentTo(new[] { "Facturation", "Jamais" });
    }

    // Same signature as the tests of the markdown files written below
    public void Some_method(string x) { }

    private static MethodInfo SomeMethod => typeof(CategoryTests).GetMethod(nameof(Some_method))!;

    private static IReadOnlyList<MarkdownTestCase> Read(string markdown)
    {
        var folder = Directory.CreateTempSubdirectory("mdat-categories-");

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

    private static string Case(string info = "") => $"## Case\n\n``````yaml {info}\nx: a\n``````\n\n";

    [Test]
    public async Task The_categories_of_the_file_and_of_the_case_add_up_without_repeating()
    {
        var cases = Read("---\ntags:\n  category: Billing, Smoke\n---\n# Title\n\n" + Case("category=Smoke,Fast"));

        await Assert.That(cases.Count).IsEqualTo(1);
        await Assert.That(cases[0].Categories).IsEquivalentTo(new[] { "Billing", "Smoke", "Fast" });
    }

    [Test]
    public async Task The_categories_of_the_front_matter_can_be_a_list()
    {
        var cases = Read("---\ntags:\n  category:\n    - Billing\n    - Slow\n---\n# Title\n\n" + Case());

        await Assert.That(cases[0].Categories).IsEquivalentTo(new[] { "Billing", "Slow" });
    }

    [Test]
    public async Task A_file_without_tags_gives_cases_without_categories()
    {
        await Assert.That(Read("# Title\n\n" + Case())[0].Categories.Count).IsEqualTo(0);
        await Assert.That(Read("---\ntitle: nothing here\n---\n# Title\n\n" + Case())[0].Categories.Count).IsEqualTo(0);
    }

    private static string[] Properties(MarkdownTestCase testCase) => testCase.Properties.Select(p => $"{p.Key}={p.Value}").ToArray();

    [Test]
    public async Task A_name_other_than_category_is_a_property_and_not_a_category()
    {
        var cases = Read("---\ntags:\n  area: Taxes\n---\n# Title\n\n" + Case("priority=High flag"));

        await Assert.That(cases[0].Categories.Count).IsEqualTo(0);
        await Assert.That(Properties(cases[0])).IsEquivalentTo(new[] { "area=Taxes", "priority=High" });
    }

    [Test]
    public async Task A_property_of_the_case_replaces_the_one_of_the_file_with_the_same_name()
    {
        var cases = Read("---\ntags:\n  priority: High\n  area: Taxes\n---\n# Title\n\n" + Case("priority=Low"));

        await Assert.That(Properties(cases[0])).IsEquivalentTo(new[] { "area=Taxes", "priority=Low" });
    }

    [Test]
    public async Task A_list_in_the_front_matter_gives_one_property_per_item_and_a_value_can_be_quoted()
    {
        var cases = Read("---\ntags:\n  env: [dev, test]\n---\n# Title\n\n" + Case("owner=\"Billing team\""));

        await Assert.That(Properties(cases[0])).IsEquivalentTo(new[] { "env=dev", "env=test", "owner=Billing team" });
    }

    [Test]
    public async Task An_ignored_case_keeps_its_properties_too()
    {
        var cases = Read("# Title\n\n" + Case("skipped priority=High"));

        await Assert.That(cases[0].SkipReason).IsNotNull();
        await Assert.That(Properties(cases[0])).IsEquivalentTo(new[] { "priority=High" });
    }

    [Test]
    public async Task A_value_with_spaces_can_be_quoted_and_the_name_is_not_case_sensitive()
    {
        var cases = Read("# Title\n\n" + Case("Category=\"Very slow, Heavy\""));

        await Assert.That(cases[0].Categories).IsEquivalentTo(new[] { "Very slow", "Heavy" });
    }

    [Test]
    public async Task A_front_matter_that_cannot_be_read_does_not_stop_the_cases()
    {
        var cases = Read("---\ntags: [unclosed\n  - : :\n---\n# Title\n\n" + Case("category=Smoke"));

        await Assert.That(cases.Count).IsEqualTo(1);
        await Assert.That(cases[0].Categories).IsEquivalentTo(new[] { "Smoke" });
    }

    [Test]
    public async Task The_front_matter_is_not_taken_for_a_title_of_the_case()
    {
        var cases = Read("---\ntags:\n  category: Billing\n---\n# Some_method\n\n" + Case());

        await Assert.That(cases[0].DisplayName).IsEqualTo("Some_method_Case@cases.md");
    }

    [Test]
    public async Task An_ignored_case_keeps_its_categories_so_that_it_can_still_be_filtered()
    {
        var cases = Read("# Title\n\n" + Case("skipped category=Smoke"));

        await Assert.That(cases[0].SkipReason).IsNotNull();
        await Assert.That(cases[0].Categories).IsEquivalentTo(new[] { "Smoke" });
    }
}
