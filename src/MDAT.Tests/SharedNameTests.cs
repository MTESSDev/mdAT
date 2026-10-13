namespace MDAT.Tests;

/// <summary>
/// Two classes with a test of the same name read the same markdown file ({method}.md): the file keeps one link per
/// class and method, so that the two lines do not take each other's place at every discovery.
/// </summary>
public class SharedNameFirstTests
{
    [Test]
    [MarkdownDataSource("~/Tests/{method}.md")]
    public async Task Shared_name_check(int a)
    {
        await Assert.That(a).IsGreaterThan(0);
    }
}

public class SharedNameSecondTests
{
    [Test]
    [MarkdownDataSource("~/Tests/{method}.md")]
    public async Task Shared_name_check(int a)
    {
        var markdown = await File.ReadAllTextAsync(TestContext.Current!.Metadata.TestDetails.Location.TestFilePath);

        // both classes have their own line in the file they share
        await Assert.That(markdown).Contains("<!-- mdat:SharedNameTests.cs#SharedNameFirstTests.Shared_name_check -->");
        await Assert.That(markdown).Contains("<!-- mdat:SharedNameTests.cs#SharedNameSecondTests.Shared_name_check -->");
        await Assert.That(a).IsGreaterThan(0);
    }
}
