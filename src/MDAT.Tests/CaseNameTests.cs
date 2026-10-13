using System.Reflection;
using System.Text.Json.Nodes;

namespace MDAT.Tests;

/// <summary>The name of a test case is built from the method and the titles above the case, without repeating the method.</summary>
public class CaseNameTests
{
    // Same signature as the tests of the markdown files written below
    public void Some_method_name(string x) { }

    private static MethodInfo SomeMethod => typeof(CaseNameTests).GetMethod(nameof(Some_method_name))!;

    private static string[] Names(string markdown)
    {
        var folder = Directory.CreateTempSubdirectory("mdat-names-");

        try
        {
            var path = Path.Combine(folder.FullName, "cases.md");
            File.WriteAllText(path, markdown);

            return new MarkdownTestSource(path).GetCases(SomeMethod).Select(c => c.DisplayName).ToArray();
        }
        finally
        {
            folder.Delete(recursive: true);
        }
    }

    private static string Cases(string title) =>
        $"{title}\n\n## Case 1\n\n``````yaml\nx: a\n``````\n\n## Case 2\n\n### Detail\n\n``````yaml\nx: b\n``````\n";

    [Test]
    public async Task A_title_equal_to_the_method_name_is_not_repeated()
    {
        var names = Names(Cases("# Some_method_name"));

        await Assert.That(names).IsEquivalentTo(new[] { "Some_method_name_Case 1@cases.md", "Some_method_name_Case 2_Detail@cases.md" });
    }

    [Test]
    [Arguments("# Some method name")]
    [Arguments("# SOME-METHOD-NAME")]
    [Arguments("# some_method_name")]
    public async Task The_comparison_ignores_case_spaces_and_separators(string title)
    {
        var names = Names(Cases(title));

        await Assert.That(names[0]).IsEqualTo("Some_method_name_Case 1@cases.md");
    }

    [Test]
    public async Task A_title_that_is_not_the_method_name_is_kept()
    {
        var names = Names(Cases("# Addition rules"));

        await Assert.That(names[0]).IsEqualTo("Some_method_name_Addition rules_Case 1@cases.md");
        await Assert.That(names[1]).IsEqualTo("Some_method_name_Addition rules_Case 2_Detail@cases.md");
    }

    [Test]
    public async Task A_file_with_only_the_method_title_gives_the_method_and_the_file()
    {
        var names = Names("# Some_method_name\n\n``````yaml\nx: a\n``````\n");

        await Assert.That(names.Single()).IsEqualTo("Some_method_name@cases.md");
    }

    [Test]
    public async Task The_real_files_of_the_project_follow_the_same_rule()
    {
        var method = typeof(NewtonsoftConvertersTests).GetMethod(nameof(NewtonsoftConvertersTests.Test_JsonNode))!;

        var names = new MarkdownTestSource("~\\Tests\\test-json-node.md").GetCases(method).Select(c => c.DisplayName).ToArray();

        await Assert.That(names.First()).IsEqualTo("Test_JsonNode_Case 1@test-json-node.md");
        await Assert.That(names.First()).DoesNotContain("Test_JsonNode_Test_JsonNode");
    }
}
