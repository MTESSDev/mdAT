namespace MDAT.Tests;

/// <summary>
/// Where the test explorer takes you when you open a case: its title in the markdown file (or the start of its
/// document in a YAML file), not the C# method shared by all the cases.
/// </summary>
public class TestCaseLocationTests
{
    [Test]
    [MarkdownDataSource("~/Tests/adapter-locations.md")]
    public async Task A_markdown_case_points_to_its_title(string heading)
    {
        var location = TestContext.Current!.Metadata.TestDetails.Location;

        await Assert.That(Path.GetFileName(location.TestFilePath)).IsEqualTo("adapter-locations.md");

        var lines = await File.ReadAllLinesAsync(location.TestFilePath);

        // the line is 1-based and holds the title of the case
        await Assert.That(lines[location.TestLineNumber - 1]).IsEqualTo(heading);
    }

    [Test]
    [YamlFileDataSource("~/Tests/adapter-locations.yaml")]
    public async Task A_yaml_case_points_to_the_start_of_its_document(string heading)
    {
        var location = TestContext.Current!.Metadata.TestDetails.Location;

        await Assert.That(Path.GetFileName(location.TestFilePath)).IsEqualTo("adapter-locations.yaml");

        var lines = await File.ReadAllLinesAsync(location.TestFilePath);

        await Assert.That(lines[location.TestLineNumber - 1]).IsEqualTo("---");
        // the document of the case follows its separator
        await Assert.That(lines[location.TestLineNumber]).IsEqualTo("heading: " + heading);
    }

    [Test]
    [MarkdownDataSource("~/Tests/adapter-simple.md")]
    public async Task The_cases_of_a_file_have_different_lines(int a, string b)
    {
        // The file is shared by three tests, each with its own link at the top: the link written for one of them must not
        // move the lines found for another. Even on the very first run, the line of the case holds its title.
        var location = TestContext.Current!.Metadata.TestDetails.Location;

        await Assert.That(location.TestLineNumber).IsGreaterThan(0);
        await Assert.That(Path.GetFileName(location.TestFilePath)).IsEqualTo("adapter-simple.md");

        var lines = await File.ReadAllLinesAsync(location.TestFilePath);

        await Assert.That(lines[location.TestLineNumber - 1]).StartsWith("## Case");
        await Assert.That(b).IsNotNull();
        await Assert.That(a).IsGreaterThan(0);
    }
}
