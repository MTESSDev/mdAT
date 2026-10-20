using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace MDAT.VisualStudio
{
    /// <summary>The test method that holds a position of a C# file.</summary>
    internal readonly struct MethodSpot
    {
        public MethodSpot(string className, string methodName)
        {
            ClassName = className;
            MethodName = methodName;
        }

        public string ClassName { get; }

        public string MethodName { get; }
    }

    /// <summary>
    /// Finds the method (and its class) that holds a position of a C# file, from the text only: no compiler is needed, so it
    /// works on a file that is being edited. The text is read for what it has to say about braces: comments, strings and
    /// characters are skipped, a block whose header is <c>class X</c> is a class, a block directly in a class whose header
    /// ends with a parenthesis is a method.
    /// </summary>
    internal static class MethodLocator
    {
        private static readonly Regex TypeHeader = new Regex(@"\b(?:class|struct|interface|record)\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private static readonly Regex Attributes = new Regex(@"\[[^\[\]]*(?:\[[^\[\]]*\][^\[\]]*)*\]", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private static readonly Regex MethodHeader = new Regex(@"(?<name>[A-Za-z_][A-Za-z0-9_]*)\s*(?:<[^()]*>)?\s*\(.*\)\s*(?:where\b[^()]*)?$", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Singleline);

        private static readonly HashSet<string> NotMethods = new HashSet<string>
        {
            "if", "for", "foreach", "while", "switch", "using", "lock", "catch", "fixed", "when", "else", "do", "try", "finally",
        };

        private enum Kind { Other, Type, Method }

        private sealed class Block
        {
            public Kind Kind;
            public string Name = "";
            public string ClassName = "";
            public int HeaderStart;
        }

        private sealed class Found
        {
            public string ClassName = "";
            public string MethodName = "";
            public int HeaderStart;
            public int End;
        }

        /// <summary>The method that holds <paramref name="position"/> (its attributes and its body), or <c>null</c>.</summary>
        public static MethodSpot? Locate(string text, int position)
        {
            var found = Scan(text).FirstOrDefault(m => position >= m.HeaderStart && position <= m.End);

            return found is null ? (MethodSpot?)null : new MethodSpot(found.ClassName, found.MethodName);
        }

        private static List<Found> Scan(string text)
        {
            var methods = new List<Found>();
            var stack = new Stack<Block>();
            var header = new StringBuilder();
            var headerStart = 0;
            var depthParens = 0;
            var depthBrackets = 0;

            for (var i = 0; i < text.Length; i++)
            {
                var c = text[i];

                // comments
                if (c == '/' && i + 1 < text.Length && text[i + 1] == '/')
                {
                    while (i < text.Length && text[i] != '\n') i++;
                    continue;
                }

                if (c == '/' && i + 1 < text.Length && text[i + 1] == '*')
                {
                    var end = text.IndexOf("*/", i + 2, StringComparison.Ordinal);
                    i = end < 0 ? text.Length : end + 1;
                    continue;
                }

                // preprocessor lines
                if (c == '#' && IsFirstOnLine(text, i))
                {
                    while (i < text.Length && text[i] != '\n') i++;
                    continue;
                }

                // strings and characters: only their place matters
                if (c == '"' || c == '\'' || ((c == '@' || c == '$') && IsStringStart(text, i)))
                {
                    i = SkipLiteral(text, i);
                    header.Append(c == '\'' ? "''" : "\"\"");
                    continue;
                }

                if (c == '(') depthParens++;
                else if (c == ')') depthParens = Math.Max(0, depthParens - 1);
                else if (c == '[') depthBrackets++;
                else if (c == ']') depthBrackets = Math.Max(0, depthBrackets - 1);

                var inside = depthParens > 0 || depthBrackets > 0;

                if (c == '{')
                {
                    var block = inside ? new Block() : Classify(header.ToString(), stack, headerStart);
                    stack.Push(block);

                    if (!inside)
                    {
                        header.Clear();
                        headerStart = i + 1;
                    }

                    continue;
                }

                if (c == '}')
                {
                    if (stack.Count > 0)
                    {
                        var block = stack.Pop();

                        if (block.Kind == Kind.Method)
                        {
                            methods.Add(new Found { ClassName = block.ClassName, MethodName = block.Name, HeaderStart = block.HeaderStart, End = i });
                        }
                    }

                    // a brace inside an attribute or an argument (an initializer) does not end the header
                    if (!inside)
                    {
                        header.Clear();
                        headerStart = i + 1;
                    }

                    continue;
                }

                if (c == ';' && !inside)
                {
                    header.Clear();
                    headerStart = i + 1;
                    continue;
                }

                header.Append(c);
            }

            return methods;
        }

        private static Block Classify(string header, Stack<Block> stack, int headerStart)
        {
            var withoutAttributes = Attributes.Replace(header, " ").Trim();

            var type = TypeHeader.Match(withoutAttributes);
            var parenthesis = withoutAttributes.IndexOf('(');

            // "class X", "record X(int a)": the keyword comes before any parenthesis (a "where T : class" constraint comes after)
            if (type.Success && (parenthesis < 0 || type.Index < parenthesis))
            {
                return new Block { Kind = Kind.Type, Name = type.Groups["name"].Value, HeaderStart = headerStart };
            }

            var parent = stack.Count > 0 ? stack.Peek() : null;

            if (parent is { Kind: Kind.Type })
            {
                var method = MethodHeader.Match(withoutAttributes);

                if (method.Success && !NotMethods.Contains(method.Groups["name"].Value))
                {
                    return new Block { Kind = Kind.Method, Name = method.Groups["name"].Value, ClassName = parent.Name, HeaderStart = headerStart };
                }
            }

            return new Block();
        }

        private static bool IsFirstOnLine(string text, int index)
        {
            for (var i = index - 1; i >= 0 && text[i] != '\n'; i--)
            {
                if (!char.IsWhiteSpace(text[i])) return false;
            }

            return true;
        }

        private static bool IsStringStart(string text, int index)
        {
            var next = index + 1 < text.Length ? text[index + 1] : '\0';

            return next == '"' || ((next == '@' || next == '$') && index + 2 < text.Length && text[index + 2] == '"');
        }

        /// <summary>Index of the last character of the string or character literal that starts at <paramref name="start"/>.</summary>
        private static int SkipLiteral(string text, int start)
        {
            var i = start;
            var verbatim = false;

            while (i < text.Length && (text[i] == '@' || text[i] == '$'))
            {
                verbatim |= text[i] == '@';
                i++;
            }

            if (i >= text.Length) return text.Length;

            var quote = text[i];

            // raw string: """ ... """
            if (quote == '"' && i + 2 < text.Length && text[i + 1] == '"' && text[i + 2] == '"')
            {
                var count = 3;
                while (i + count < text.Length && text[i + count] == '"') count++;

                var end = text.IndexOf(new string('"', count), i + count, StringComparison.Ordinal);
                return end < 0 ? text.Length : end + count - 1;
            }

            for (i++; i < text.Length; i++)
            {
                if (!verbatim && text[i] == '\\')
                {
                    i++;
                    continue;
                }

                if (text[i] == quote)
                {
                    if (verbatim && i + 1 < text.Length && text[i + 1] == quote)
                    {
                        i++;
                        continue;
                    }

                    return i;
                }

                // a regular string or a character does not run over a line
                if (!verbatim && text[i] == '\n') return i;
            }

            return text.Length;
        }
    }

    /// <summary>Finds the markdown file of a test: the one that carries the <c>mdat:</c> marker written for that test.</summary>
    internal static class MarkdownFinder
    {
        private static readonly HashSet<string> SkippedFolders = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "bin", "obj", ".git", ".vs", "node_modules", "packages", "TestResults",
        };

        /// <summary>The folder to search: the project of <paramref name="sourceFilePath"/> (the first folder up with a project file).</summary>
        public static string SearchRoot(string sourceFilePath)
        {
            var start = Path.GetDirectoryName(Path.GetFullPath(sourceFilePath)) ?? "";

            for (var folder = start; !string.IsNullOrEmpty(folder); folder = Path.GetDirectoryName(folder))
            {
                if (Directory.EnumerateFiles(folder, "*.csproj").Any())
                {
                    return folder;
                }
            }

            return start;
        }

        /// <summary>The markdown file and the line of the marker of <c>Class.Method</c> of <paramref name="sourceFilePath"/>, or <c>null</c>.</summary>
        public static (string Path, int Line)? Find(string sourceFilePath, string className, string methodName)
        {
            var marker = $"mdat:{Path.GetFileName(sourceFilePath)}#{className}.{methodName}";

            foreach (var markdown in Markdowns(SearchRoot(sourceFilePath)))
            {
                var line = LineOfMarker(markdown, marker);

                if (line > 0)
                {
                    return (markdown, line);
                }
            }

            return null;
        }

        private static int LineOfMarker(string markdown, string marker)
        {
            try
            {
                var number = 0;

                foreach (var text in ReadLines(markdown))
                {
                    number++;
                    var index = text.IndexOf(marker, StringComparison.Ordinal);

                    // the marker ends there: "mdat:File.cs#Class.Method2" is not the marker of "Class.Method"
                    if (index >= 0 && text.IndexOf("<!--", StringComparison.Ordinal) >= 0
                        && (index + marker.Length >= text.Length || char.IsWhiteSpace(text[index + marker.Length]) || text[index + marker.Length] == '-'))
                    {
                        return number;
                    }
                }
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                // a file that cannot be read is not the one
            }

            return 0;
        }

        /// <summary>Reads the lines of a file without locking it: another program may be rewriting it (a rename fails on a file kept open).</summary>
        internal static IEnumerable<string> ReadLines(string path)
        {
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
            using (var reader = new StreamReader(stream, Encoding.UTF8, true))
            {
                string? line;

                while ((line = reader.ReadLine()) != null)
                {
                    yield return line;
                }
            }
        }

        private static IEnumerable<string> Markdowns(string root)
        {
            var folders = new Stack<string>();
            folders.Push(root);

            while (folders.Count > 0)
            {
                var folder = folders.Pop();
                string[] files;
                string[] subFolders;

                try
                {
                    files = Directory.GetFiles(folder, "*.md");
                    subFolders = Directory.GetDirectories(folder);
                }
                catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
                {
                    continue;
                }

                foreach (var file in files)
                {
                    yield return file;
                }

                foreach (var sub in subFolders)
                {
                    if (!SkippedFolders.Contains(Path.GetFileName(sub)))
                    {
                        folders.Push(sub);
                    }
                }
            }
        }
    }
}
