using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace MDAT.VisualStudio
{
    /// <summary>A link to a C# test written by mdAT in a markdown file: <c>[Tests.cs:49](../Tests.cs#L49)</c>.</summary>
    internal readonly struct MdatLink
    {
        public MdatLink(int start, int length, string target, int line)
        {
            Start = start;
            Length = length;
            Target = target;
            Line = line;
        }

        /// <summary>Position of the link in its line.</summary>
        public int Start { get; }

        public int Length { get; }

        /// <summary>Address of the C# file as written in the markdown file, without the line.</summary>
        public string Target { get; }

        /// <summary>1-based line of the test in the C# file.</summary>
        public int Line { get; }

        public bool Contains(int column) => column >= Start && column < Start + Length;
    }

    /// <summary>
    /// Finds the links that mdAT keeps at the top of a markdown test file
    /// (<c>&gt; C# : [Tests.cs:49](../Tests.cs#L49) &lt;!-- mdat:Tests.cs#Class.Method --&gt;</c>). Only a link followed by the
    /// <c>mdat:</c> comment is taken: the other links of the file are none of our business.
    /// </summary>
    internal static class MdatLinkParser
    {
        private const string Marker = "<!--";

        private static readonly Regex Pattern = new Regex(
            @"\[[^\]]*\]\((?<target>[^)\s]+?)#L(?<line>[0-9]+)\)(?=\s*<!--\s*mdat:)",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public static IEnumerable<MdatLink> FindAll(string lineText)
        {
            // cheap test first: this runs on every line of every text file that is opened
            if (string.IsNullOrEmpty(lineText) || lineText.IndexOf(Marker, StringComparison.Ordinal) < 0 || lineText.IndexOf("mdat:", StringComparison.Ordinal) < 0)
            {
                yield break;
            }

            foreach (Match match in Pattern.Matches(lineText))
            {
                if (int.TryParse(match.Groups["line"].Value, out var line))
                {
                    yield return new MdatLink(match.Index, match.Length, match.Groups["target"].Value, line);
                }
            }
        }

        public static bool TryFind(string lineText, int column, out MdatLink link)
        {
            foreach (var candidate in FindAll(lineText))
            {
                if (candidate.Contains(column))
                {
                    link = candidate;
                    return true;
                }
            }

            link = default;
            return false;
        }

        /// <summary>
        /// Full path of the C# file of <paramref name="link"/>, from the markdown file that holds it. Reads the relative links
        /// (<c>../Tests.cs</c>), the <c>file:</c> ones and the full addresses (<c>file:///C:/Dev/Tests.cs</c>).
        /// </summary>
        public static string Resolve(string markdownPath, MdatLink link)
        {
            var target = link.Target;

            if (target.StartsWith("file:", StringComparison.OrdinalIgnoreCase))
            {
                if (target.StartsWith("file://", StringComparison.OrdinalIgnoreCase)
                    && Uri.TryCreate(target, UriKind.Absolute, out var uri))
                {
                    return uri.LocalPath;
                }

                target = target.Substring("file:".Length);
            }

            var relative = Uri.UnescapeDataString(target).Replace('/', Path.DirectorySeparatorChar);

            return Path.GetFullPath(Path.Combine(Path.GetDirectoryName(markdownPath) ?? "", relative));
        }
    }
}
