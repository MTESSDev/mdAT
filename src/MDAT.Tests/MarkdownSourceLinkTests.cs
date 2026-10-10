using System.Text;

namespace MDAT.Tests;

/// <summary>The link to the C# test kept at the top of a markdown test file.</summary>
public class MarkdownSourceLinkTests
{
    private const string Title = "# Probe\n\n> Summary of the test\n\n## Case 1\n\nText\n";

    /// <summary>A temporary folder with the markdown file in <c>Tests</c> and the C# file next to <c>Tests</c>.</summary>
    private sealed class Sandbox : IDisposable
    {
        private readonly DirectoryInfo _root = Directory.CreateTempSubdirectory("mdat-link-");

        public Sandbox(string markdownContent = Title, string markdownName = "probe.md", string sourceName = "Code Tests.cs", bool withSource = true, byte[]? markdownBytes = null)
        {
            Directory.CreateDirectory(Path.Combine(_root.FullName, "Tests"));

            Markdown = Path.Combine(_root.FullName, "Tests", markdownName);
            Source = Path.Combine(_root.FullName, sourceName);

            File.WriteAllBytes(Markdown, markdownBytes ?? new UTF8Encoding(false).GetBytes(markdownContent));

            if (withSource)
            {
                File.WriteAllText(Source, "// test code");
            }
        }

        public string Markdown { get; }

        public string Source { get; }

        public string Text => File.ReadAllText(Markdown);

        public byte[] Bytes => File.ReadAllBytes(Markdown);

        public void Dispose()
        {
            foreach (var file in _root.EnumerateFiles("*", SearchOption.AllDirectories))
            {
                file.Attributes = FileAttributes.Normal;
            }

            _root.Delete(recursive: true);
        }
    }

    [Test]
    public async Task The_link_is_inserted_after_the_title()
    {
        using var sandbox = new Sandbox();

        var changed = MarkdownSourceLink.Update(sandbox.Markdown, sandbox.Source, 12, "Probe");

        await Assert.That(changed).IsTrue();
        await Assert.That(sandbox.Text).IsEqualTo(
            "# Probe\n\n> C# : [Code Tests.cs:12](../Code%20Tests.cs#L12) <!-- mdat:Code Tests.cs#Probe -->\n\n> Summary of the test\n\n## Case 1\n\nText\n");
    }

    [Test]
    public async Task The_line_number_is_corrected_when_the_test_moves()
    {
        using var sandbox = new Sandbox();
        MarkdownSourceLink.Update(sandbox.Markdown, sandbox.Source, 12, "Probe");

        var changed = MarkdownSourceLink.Update(sandbox.Markdown, sandbox.Source, 20, "Probe");

        await Assert.That(changed).IsTrue();
        await Assert.That(sandbox.Text).IsEqualTo(
            "# Probe\n\n> C# : [Code Tests.cs:20](../Code%20Tests.cs#L20) <!-- mdat:Code Tests.cs#Probe -->\n\n> Summary of the test\n\n## Case 1\n\nText\n");
    }

    [Test]
    public async Task The_file_is_not_written_again_when_the_link_has_not_changed()
    {
        using var sandbox = new Sandbox();
        MarkdownSourceLink.Update(sandbox.Markdown, sandbox.Source, 12, "Probe");

        // an old date shows a rewrite, even on a file system with a coarse clock
        var oldDate = new DateTime(2001, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        File.SetLastWriteTimeUtc(sandbox.Markdown, oldDate);
        var before = sandbox.Bytes;

        var changed = MarkdownSourceLink.Update(sandbox.Markdown, sandbox.Source, 12, "Probe");

        await Assert.That(changed).IsFalse();
        await Assert.That(File.GetLastWriteTimeUtc(sandbox.Markdown)).IsEqualTo(oldDate);
        await Assert.That(sandbox.Bytes.SequenceEqual(before)).IsTrue();
    }

    [Test]
    public async Task A_link_that_is_missing_makes_exactly_one_write_and_the_next_calls_none()
    {
        using var sandbox = new Sandbox();
        var oldDate = new DateTime(2001, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        File.SetLastWriteTimeUtc(sandbox.Markdown, oldDate);

        var first = MarkdownSourceLink.Update(sandbox.Markdown, sandbox.Source, 12, "Probe");
        var writtenAt = File.GetLastWriteTimeUtc(sandbox.Markdown);
        File.SetLastWriteTimeUtc(sandbox.Markdown, oldDate);
        var second = MarkdownSourceLink.Update(sandbox.Markdown, sandbox.Source, 12, "Probe");
        var third = MarkdownSourceLink.Update(sandbox.Markdown, sandbox.Source, 12, "Probe");

        await Assert.That(first).IsTrue();
        await Assert.That(writtenAt).IsNotEqualTo(oldDate);
        await Assert.That(second).IsFalse();
        await Assert.That(third).IsFalse();
        await Assert.That(File.GetLastWriteTimeUtc(sandbox.Markdown)).IsEqualTo(oldDate);
    }

    [Test]
    public async Task Bom_and_windows_line_endings_are_kept()
    {
        var content = new UTF8Encoding(false).GetBytes("# Probe\r\n\r\n## Case 1\r\n\r\nText\r\n");
        var bytes = new byte[] { 0xEF, 0xBB, 0xBF }.Concat(content).ToArray();

        using var sandbox = new Sandbox(markdownBytes: bytes);
        MarkdownSourceLink.Update(sandbox.Markdown, sandbox.Source, 5, "Probe");

        var result = sandbox.Bytes;

        await Assert.That(result.Take(3).SequenceEqual(new byte[] { 0xEF, 0xBB, 0xBF })).IsTrue();

        var text = new UTF8Encoding(false).GetString(result, 3, result.Length - 3);

        await Assert.That(text).IsEqualTo(
            "# Probe\r\n\r\n> C# : [Code Tests.cs:5](../Code%20Tests.cs#L5) <!-- mdat:Code Tests.cs#Probe -->\r\n\r\n## Case 1\r\n\r\nText\r\n");
    }

    [Test]
    public async Task Only_the_link_line_changes_when_the_line_moves_and_the_rest_is_left_as_it_was()
    {
        const string edited = "# Probe\n\n> C# : [Code Tests.cs:1](../Code%20Tests.cs#L1) <!-- mdat:Code Tests.cs#Probe -->\n\nAnalyst wrote this\n\n## Case 1\n";

        using var sandbox = new Sandbox(edited);

        MarkdownSourceLink.Update(sandbox.Markdown, sandbox.Source, 77, "Probe");

        await Assert.That(sandbox.Text).IsEqualTo(
            "# Probe\n\n> C# : [Code Tests.cs:77](../Code%20Tests.cs#L77) <!-- mdat:Code Tests.cs#Probe -->\n\nAnalyst wrote this\n\n## Case 1\n");
    }

    [Test]
    public async Task What_follows_the_title_on_its_line_stays_on_that_line()
    {
        using var sandbox = new Sandbox("# Probe   \n\n## Case 1\n\nText\n");

        MarkdownSourceLink.Update(sandbox.Markdown, sandbox.Source, 3, "Probe");

        await Assert.That(sandbox.Text).IsEqualTo(
            "# Probe   \n\n> C# : [Code Tests.cs:3](../Code%20Tests.cs#L3) <!-- mdat:Code Tests.cs#Probe -->\n\n## Case 1\n\nText\n");
    }

    [Test]
    public async Task Closing_hashes_of_the_title_are_not_separated_from_it()
    {
        using var sandbox = new Sandbox("# Probe #\n\n## Case 1\n");

        MarkdownSourceLink.Update(sandbox.Markdown, sandbox.Source, 3, "Probe");

        await Assert.That(sandbox.Text).StartsWith("# Probe #\n\n> C# : ");
        await Assert.That(sandbox.Text).EndsWith("-->\n\n## Case 1\n");
    }

    [Test]
    public async Task The_new_lines_use_the_line_ending_of_the_title_even_in_a_mixed_file()
    {
        // a file written with LF, with one stray CRLF further down: the inserted lines follow the title, not the stray one
        using var sandbox = new Sandbox("# Probe\n\n## Case 1\r\n\nText\n");

        MarkdownSourceLink.Update(sandbox.Markdown, sandbox.Source, 3, "Probe");

        await Assert.That(sandbox.Text).IsEqualTo(
            "# Probe\n\n> C# : [Code Tests.cs:3](../Code%20Tests.cs#L3) <!-- mdat:Code Tests.cs#Probe -->\n\n## Case 1\r\n\nText\n");
    }

    [Test]
    public async Task A_file_that_does_not_end_with_a_line_ending_keeps_that_way()
    {
        using var sandbox = new Sandbox("# Probe");

        MarkdownSourceLink.Update(sandbox.Markdown, sandbox.Source, 3, "Probe");

        await Assert.That(sandbox.Text).IsEqualTo(
            "# Probe" + Environment.NewLine + Environment.NewLine + "> C# : [Code Tests.cs:3](../Code%20Tests.cs#L3) <!-- mdat:Code Tests.cs#Probe -->");
    }

    [Test]
    public async Task The_links_of_all_the_tests_of_a_file_are_written_in_one_go_and_in_order()
    {
        using var sandbox = new Sandbox();

        var entries = new[]
        {
            new SourceLinkEntry(sandbox.Source, 40, "Second"),
            new SourceLinkEntry(sandbox.Source, 10, "First"),
            new SourceLinkEntry(sandbox.Source, 25, "Middle"),
        };

        var changed = MarkdownSourceLink.Update(sandbox.Markdown, entries);

        await Assert.That(changed).IsTrue();

        // whatever the order the tests were found in, the lines come out by increasing line of the C# file
        await Assert.That(sandbox.Text).IsEqualTo(
            "# Probe\n\n"
            + "> C# : [Code Tests.cs:10](../Code%20Tests.cs#L10) <!-- mdat:Code Tests.cs#First -->\n\n"
            + "> C# : [Code Tests.cs:25](../Code%20Tests.cs#L25) <!-- mdat:Code Tests.cs#Middle -->\n\n"
            + "> C# : [Code Tests.cs:40](../Code%20Tests.cs#L40) <!-- mdat:Code Tests.cs#Second -->\n\n"
            + "> Summary of the test\n\n## Case 1\n\nText\n");
    }

    [Test]
    public async Task Writing_all_the_links_at_once_does_not_write_again_when_nothing_changed()
    {
        using var sandbox = new Sandbox();

        var entries = new[]
        {
            new SourceLinkEntry(sandbox.Source, 10, "First"),
            new SourceLinkEntry(sandbox.Source, 40, "Second"),
        };

        MarkdownSourceLink.Update(sandbox.Markdown, entries);

        var oldDate = new DateTime(2001, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        File.SetLastWriteTimeUtc(sandbox.Markdown, oldDate);

        // the same links, found in another order
        var again = MarkdownSourceLink.Update(sandbox.Markdown, entries.Reverse());

        await Assert.That(again).IsFalse();
        await Assert.That(File.GetLastWriteTimeUtc(sandbox.Markdown)).IsEqualTo(oldDate);
    }

    [Test]
    public async Task Only_the_link_that_moved_is_corrected_in_a_file_with_several_links()
    {
        using var sandbox = new Sandbox();

        MarkdownSourceLink.Update(sandbox.Markdown, new[]
        {
            new SourceLinkEntry(sandbox.Source, 10, "First"),
            new SourceLinkEntry(sandbox.Source, 40, "Second"),
        });

        MarkdownSourceLink.Update(sandbox.Markdown, new[]
        {
            new SourceLinkEntry(sandbox.Source, 10, "First"),
            new SourceLinkEntry(sandbox.Source, 44, "Second"),
        });

        await Assert.That(sandbox.Text).IsEqualTo(
            "# Probe\n\n"
            + "> C# : [Code Tests.cs:10](../Code%20Tests.cs#L10) <!-- mdat:Code Tests.cs#First -->\n\n"
            + "> C# : [Code Tests.cs:44](../Code%20Tests.cs#L44) <!-- mdat:Code Tests.cs#Second -->\n\n"
            + "> Summary of the test\n\n## Case 1\n\nText\n");
    }

    [Test]
    public async Task Two_tests_sharing_a_file_each_keep_their_own_line()
    {
        using var sandbox = new Sandbox();

        MarkdownSourceLink.Update(sandbox.Markdown, sandbox.Source, 10, "First");
        MarkdownSourceLink.Update(sandbox.Markdown, sandbox.Source, 40, "Second");

        var afterBoth = sandbox.Text;

        // calling again in any order changes nothing: the lines do not take each other's place
        var again = new[]
        {
            MarkdownSourceLink.Update(sandbox.Markdown, sandbox.Source, 40, "Second"),
            MarkdownSourceLink.Update(sandbox.Markdown, sandbox.Source, 10, "First"),
        };

        await Assert.That(again.Any(c => c)).IsFalse();
        await Assert.That(sandbox.Text).IsEqualTo(afterBoth);
        await Assert.That(afterBoth).Contains("[Code Tests.cs:10]");
        await Assert.That(afterBoth).Contains("[Code Tests.cs:40]");
    }

    [Test]
    public async Task A_file_without_title_gets_the_link_at_the_top()
    {
        using var sandbox = new Sandbox("Only text\n\n## Case 1\n");

        MarkdownSourceLink.Update(sandbox.Markdown, sandbox.Source, 3, "Probe");

        await Assert.That(sandbox.Text).IsEqualTo(
            "> C# : [Code Tests.cs:3](../Code%20Tests.cs#L3) <!-- mdat:Code Tests.cs#Probe -->\n\nOnly text\n\n## Case 1\n");
    }

    [Test]
    public async Task Nothing_is_written_when_the_c_sharp_file_does_not_exist()
    {
        using var sandbox = new Sandbox(withSource: false);
        var before = sandbox.Text;

        var changed = MarkdownSourceLink.Update(sandbox.Markdown, sandbox.Source, 3, "Probe");

        await Assert.That(changed).IsFalse();
        await Assert.That(sandbox.Text).IsEqualTo(before);
    }

    [Test]
    public async Task Nothing_is_written_without_a_line_or_a_file()
    {
        using var sandbox = new Sandbox();
        var before = sandbox.Text;

        await Assert.That(MarkdownSourceLink.Update(sandbox.Markdown, sandbox.Source, 0, "Probe")).IsFalse();
        await Assert.That(MarkdownSourceLink.Update(sandbox.Markdown, "", 3, "Probe")).IsFalse();
        await Assert.That(MarkdownSourceLink.Update(sandbox.Markdown, null, 3, "Probe")).IsFalse();
        await Assert.That(MarkdownSourceLink.Update(Path.Combine(Path.GetDirectoryName(sandbox.Markdown)!, "missing.md"), sandbox.Source, 3, "Probe")).IsFalse();
        await Assert.That(sandbox.Text).IsEqualTo(before);
    }

    [Test]
    public async Task A_read_only_file_is_left_alone_without_any_error()
    {
        using var sandbox = new Sandbox();
        File.SetAttributes(sandbox.Markdown, FileAttributes.ReadOnly);
        var before = sandbox.Text;

        var changed = MarkdownSourceLink.Update(sandbox.Markdown, sandbox.Source, 3, "Probe");

        await Assert.That(changed).IsFalse();
        await Assert.That(sandbox.Text).IsEqualTo(before);
        await Assert.That(Directory.GetFiles(Path.GetDirectoryName(sandbox.Markdown)!, "*.mdat-tmp").Length).IsEqualTo(0);
    }

    [Test]
    public async Task Parallel_updates_of_the_same_file_do_not_corrupt_it()
    {
        using var sandbox = new Sandbox();

        Parallel.For(0, 24, i => MarkdownSourceLink.Update(sandbox.Markdown, sandbox.Source, 100 + i, "Test" + i));

        var text = sandbox.Text;

        await Assert.That(text).StartsWith("# Probe\n\n");
        await Assert.That(text).Contains("## Case 1");

        for (var i = 0; i < 24; i++)
        {
            var marker = "<!-- mdat:Code Tests.cs#Test" + i + " -->";

            // exactly once each
            await Assert.That(text.Split(marker).Length - 1).IsEqualTo(1);
        }
    }
}
