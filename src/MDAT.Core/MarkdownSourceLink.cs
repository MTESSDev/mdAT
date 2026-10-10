using Markdig;
using Markdig.Extensions.Yaml;
using Markdig.Syntax;
using System.Collections.Concurrent;
using System.Text;

namespace MDAT;

/// <summary>A C# test that reads a markdown file: where it is written, and its name.</summary>
/// <param name="SourceFilePath">Full path of the C# file holding the test.</param>
/// <param name="Line">1-based line of the test in the C# file.</param>
/// <param name="TestName">Name of the test (class and method): one link is kept per test.</param>
public sealed record SourceLinkEntry(string SourceFilePath, int Line, string? TestName = null);

/// <summary>
/// Keeps, at the top of a markdown test file, a link to the C# test that reads it, so that one click goes from the
/// cases to the code:
/// <code>
/// # Md1
///
/// > C# : [MarkdownTestAttributeTests.cs:49](../MarkdownTestAttributeTests.cs#L49) &lt;!-- mdat:MarkdownTestAttributeTests.cs --&gt;
/// </code>
/// The line is inserted after the title when it is missing and corrected when the line of the C# test moves. The rest
/// of the file is left byte for byte as it was (encoding, BOM, line endings) and nothing is written when nothing changes.
/// </summary>
public static class MarkdownSourceLink
{
    private static readonly ConcurrentDictionary<string, object> Locks = new(StringComparer.OrdinalIgnoreCase);
    private static readonly UTF8Encoding Utf8 = new(false);
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder().UseYamlFrontMatter().Build();

    /// <summary>
    /// Inserts or corrects the link to the C# file in the markdown file.
    /// Never throws: a file that cannot be read or written (read-only, locked, other encoding, C# file not found at
    /// that path, ...) is left as it is.
    /// </summary>
    /// <param name="markdownPath">The markdown test file.</param>
    /// <param name="sourceFilePath">Full path of the C# file holding the test.</param>
    /// <param name="sourceLine">1-based line of the test in the C# file.</param>
    /// <param name="testName">
    /// Name of the C# test. A markdown file shared by several tests keeps one line per test (and per C# file), so that the
    /// lines do not take each other's place.
    /// </param>
    /// <returns>True when the markdown file was changed.</returns>
    public static bool Update(string markdownPath, string? sourceFilePath, int sourceLine, string? testName = null)
    {
        if (string.IsNullOrWhiteSpace(sourceFilePath) || sourceLine <= 0)
        {
            return false;
        }

        return Update(markdownPath, new[] { new SourceLinkEntry(sourceFilePath, sourceLine, testName) });
    }

    /// <summary>
    /// Inserts or corrects the links of all the tests that read the markdown file, in one single write. Doing it for all the
    /// tests at once keeps the lines of the cases right for every test of a shared file: no link added later by another test
    /// moves them.
    /// </summary>
    /// <returns>True when the markdown file was changed.</returns>
    public static bool Update(string markdownPath, IEnumerable<SourceLinkEntry> entries)
    {
        if (string.IsNullOrWhiteSpace(markdownPath))
        {
            return false;
        }

        try
        {
            var markdown = Path.GetFullPath(markdownPath);

            if (!File.Exists(markdown))
            {
                return false;
            }

            var existing = entries
                .Where(e => !string.IsNullOrWhiteSpace(e.SourceFilePath) && e.Line > 0)
                .Select(e => e with { SourceFilePath = Path.GetFullPath(e.SourceFilePath) })
                .Where(e => File.Exists(e.SourceFilePath))
                .ToList();

            if (existing.Count == 0)
            {
                return false;
            }

            lock (Locks.GetOrAdd(markdown, _ => new object()))
            {
                return UpdateFile(markdown, existing);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            return false;
        }
    }

    /// <summary>The line written in the markdown file, e.g. <c>&gt; C# : [Tests.cs:49](../Tests.cs#L49) &lt;!-- mdat:Tests.cs --&gt;</c>.</summary>
    public static string BuildLine(string markdownPath, string sourceFilePath, int sourceLine, string? testName = null)
    {
        var relative = RelativeLink(Path.GetFullPath(markdownPath), Path.GetFullPath(sourceFilePath));

        return relative is null
            ? throw new ArgumentException("The C# file cannot be reached with a relative path from the markdown file.", nameof(sourceFilePath))
            : $"> C# : [{Path.GetFileName(sourceFilePath)}:{sourceLine}]({relative}#L{sourceLine}) {Marker(sourceFilePath, testName)}";
    }

    private static string Marker(string sourceFilePath, string? testName) =>
        $"<!-- mdat:{Path.GetFileName(sourceFilePath)}{(string.IsNullOrEmpty(testName) ? "" : "#" + testName)} -->";

    /// <summary>
    /// Runs <paramref name="action"/> while no other thread reads or rewrites the same markdown file through this class.
    /// Reading a file that another test is rewriting would otherwise fail from time to time.
    /// </summary>
    public static T Synchronized<T>(string markdownPath, Func<T> action)
    {
        lock (Locks.GetOrAdd(Path.GetFullPath(markdownPath), _ => new object()))
        {
            return action();
        }
    }

    private static bool UpdateFile(string markdown, List<SourceLinkEntry> entries)
    {
        // A read-only file is left alone. This is checked on purpose: the file is replaced by renaming a new one over it, which
        // fails on Windows but silently succeeds on Linux, where only the permissions of the folder count.
        if ((File.GetAttributes(markdown) & FileAttributes.ReadOnly) != 0)
        {
            return false;
        }

        var bytes = File.ReadAllBytes(markdown);

        // UTF-16 files are not handled: left untouched
        if (bytes.Length >= 2 && ((bytes[0] == 0xFF && bytes[1] == 0xFE) || (bytes[0] == 0xFE && bytes[1] == 0xFF)))
        {
            return false;
        }

        var hasBom = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF;
        var bomLength = hasBom ? 3 : 0;
        var text = Utf8.GetString(bytes, bomLength, bytes.Length - bomLength);
        var newline = text.Contains("\r\n") ? "\r\n" : text.Contains('\n') ? "\n" : Environment.NewLine;

        var updated = text;

        // A new link goes right under the title, above the ones already there: inserting them from the last to the first
        // leaves them in order (by file, then line), whatever the order the tests were found in.
        foreach (var entry in entries.OrderByDescending(e => Path.GetFileName(e.SourceFilePath), StringComparer.Ordinal)
                                     .ThenByDescending(e => e.Line)
                                     .ThenByDescending(e => e.TestName, StringComparer.Ordinal))
        {
            var relative = RelativeLink(markdown, entry.SourceFilePath);

            // another drive: no relative link is possible, and an absolute one would not work for the other developers
            if (relative is null)
            {
                continue;
            }

            var marker = Marker(entry.SourceFilePath, entry.TestName);
            var link = $"> C# : [{Path.GetFileName(entry.SourceFilePath)}:{entry.Line}]({relative}#L{entry.Line}) {marker}";

            updated = ReplaceOrInsert(updated, link, marker, newline);
        }

        if (string.Equals(updated, text, StringComparison.Ordinal))
        {
            return false;
        }

        Write(markdown, hasBom, updated);
        return true;
    }

    private static string ReplaceOrInsert(string text, string link, string marker, string newline)
    {
        var markerIndex = text.IndexOf(marker, StringComparison.Ordinal);

        if (markerIndex >= 0)
        {
            // the line that holds the marker is replaced as a whole, up to its end (without the line ending)
            var lineStart = text.LastIndexOf('\n', markerIndex) + 1;
            var lineEnd = text.IndexOf('\n', markerIndex);

            if (lineEnd < 0)
            {
                lineEnd = text.Length;
            }
            else if (lineEnd > lineStart && text[lineEnd - 1] == '\r')
            {
                lineEnd--;
            }

            return text[..lineStart] + link + text[lineEnd..];
        }

        var document = Markdown.Parse(text, Pipeline);

        var title = document.OfType<HeadingBlock>().FirstOrDefault(h => h.Level == 1);

        if (title is not null)
        {
            return InsertAfterLine(text, title.Span.End, link, newline);
        }

        var frontMatter = document.OfType<YamlFrontMatterBlock>().FirstOrDefault();

        if (frontMatter is not null)
        {
            return InsertAfterLine(text, frontMatter.Span.End, link, newline);
        }

        return link + newline + newline + text;
    }

    /// <summary>
    /// Inserts the link below the line that holds the end of a block. Anything that follows the block on its line (spaces,
    /// closing hashes) stays on that line, and the new lines use the line ending of that line.
    /// </summary>
    private static string InsertAfterLine(string text, int blockEnd, string link, string fallbackNewline)
    {
        var newlineIndex = text.IndexOf('\n', Math.Min(blockEnd, text.Length));
        var lineEnd = newlineIndex < 0 ? text.Length : newlineIndex;
        var newline = fallbackNewline;

        if (newlineIndex >= 0)
        {
            if (newlineIndex > 0 && text[newlineIndex - 1] == '\r')
            {
                lineEnd = newlineIndex - 1;
                newline = "\r\n";
            }
            else
            {
                newline = "\n";
            }
        }

        return text[..lineEnd] + newline + newline + link + text[lineEnd..];
    }

    /// <summary>Path of <paramref name="source"/> from the folder of <paramref name="markdown"/>, written as a markdown link target.</summary>
    private static string? RelativeLink(string markdown, string source)
    {
        var relative = Path.GetRelativePath(Path.GetDirectoryName(markdown)!, source);

        if (Path.IsPathRooted(relative))
        {
            return null;
        }

        var segments = relative.Split(new[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries)
                               .Select(s => s is ".." or "." ? s : Uri.EscapeDataString(s));

        return string.Join("/", segments);
    }

    private static void Write(string path, bool withBom, string text)
    {
        var temporary = path + ".mdat-tmp";

        try
        {
            using (var stream = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                if (withBom)
                {
                    stream.Write(new byte[] { 0xEF, 0xBB, 0xBF });
                }

                stream.Write(Utf8.GetBytes(text));
            }

            File.Move(temporary, path, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporary))
            {
                File.Delete(temporary);
            }
        }
    }
}
