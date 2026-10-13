using MDAT.VisualStudio;

namespace MDAT.Tests;

/// <summary>
/// How the Visual Studio extension reads the link to the C# test. The parser is compiled in this project as it has no
/// dependency on Visual Studio (the extension itself cannot be built for .NET 8+).
/// </summary>
public class VisualStudioLinkParserTests
{
    private const string Line = "> C# : [Code Tests.cs:12](../Code%20Tests.cs#L12) <!-- mdat:Code Tests.cs#Probe -->";

    [Test]
    public async Task The_link_written_by_mdat_is_found_with_its_target_and_line()
    {
        var links = MdatLinkParser.FindAll(Line).ToList();

        await Assert.That(links.Count).IsEqualTo(1);
        await Assert.That(links[0].Target).IsEqualTo("../Code%20Tests.cs");
        await Assert.That(links[0].Line).IsEqualTo(12);
        await Assert.That(Line.Substring(links[0].Start, links[0].Length)).IsEqualTo("[Code Tests.cs:12](../Code%20Tests.cs#L12)");
    }

    [Test]
    public async Task Only_the_link_itself_is_clickable()
    {
        var found = MdatLinkParser.TryFind(Line, Line.IndexOf("Code Tests.cs:12", StringComparison.Ordinal), out var link);

        await Assert.That(found).IsTrue();
        await Assert.That(link.Line).IsEqualTo(12);

        // the beginning of the line and the comment are not part of the link
        await Assert.That(MdatLinkParser.TryFind(Line, 0, out _)).IsFalse();
        await Assert.That(MdatLinkParser.TryFind(Line, Line.IndexOf("<!--", StringComparison.Ordinal) + 2, out _)).IsFalse();
        await Assert.That(MdatLinkParser.TryFind(Line, Line.Length + 5, out _)).IsFalse();
    }

    [Test]
    [Arguments("[Other](../Other.cs#L3)")]
    [Arguments("[Other](../Other.cs#L3) <!-- a comment -->")]
    [Arguments("[Site](https://example.com/page)  <!-- mdat:x -->")]
    [Arguments("")]
    public async Task The_other_links_of_a_file_are_left_alone(string text)
    {
        await Assert.That(MdatLinkParser.FindAll(text).Any()).IsFalse();
    }

    [Test]
    public async Task A_relative_link_is_resolved_from_the_folder_of_the_markdown_file()
    {
        var markdown = Path.Combine(Path.GetTempPath(), "project", "Tests", "probe.md");
        MdatLinkParser.TryFind(Line, 10, out var link);

        await Assert.That(MdatLinkParser.Resolve(markdown, link))
            .IsEqualTo(Path.Combine(Path.GetTempPath(), "project", "Code Tests.cs"));
    }

    [Test]
    public async Task The_file_forms_of_the_link_are_read_too()
    {
        var markdown = Path.Combine(Path.GetTempPath(), "project", "Tests", "probe.md");

        var relative = "[A.cs:1](file:../A.cs#L1) <!-- mdat:A.cs -->";
        MdatLinkParser.TryFind(relative, 2, out var linkRelative);
        await Assert.That(MdatLinkParser.Resolve(markdown, linkRelative)).IsEqualTo(Path.Combine(Path.GetTempPath(), "project", "A.cs"));

        var full = new Uri(Path.Combine(Path.GetTempPath(), "other folder", "B.cs")).AbsoluteUri;
        var absolute = $"[B.cs:2]({full}#L2) <!-- mdat:B.cs -->";
        MdatLinkParser.TryFind(absolute, 2, out var linkAbsolute);
        await Assert.That(MdatLinkParser.Resolve(markdown, linkAbsolute)).IsEqualTo(Path.Combine(Path.GetTempPath(), "other folder", "B.cs"));
    }

    [Test]
    public async Task Every_link_of_the_real_markdown_files_of_the_project_is_found_and_leads_to_an_existing_file()
    {
        var folder = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Tests");
        var withLink = 0;

        foreach (var markdown in Directory.EnumerateFiles(Path.GetFullPath(folder), "*.md"))
        {
            foreach (var text in File.ReadLines(markdown).Where(l => l.Contains("<!-- mdat:")))
            {
                var links = MdatLinkParser.FindAll(text).ToList();

                await Assert.That(links.Count).IsEqualTo(1);
                await Assert.That(File.Exists(MdatLinkParser.Resolve(markdown, links[0]))).IsTrue();
                withLink++;
            }
        }

        await Assert.That(withLink).IsGreaterThan(0);
    }
}
