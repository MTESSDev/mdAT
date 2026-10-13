using System.Text.RegularExpressions;
using MDAT.VisualStudio;

namespace MDAT.Tests;

/// <summary>
/// How F7 of the Visual Studio extension finds the method under the cursor and the markdown file of a test. The code is
/// compiled in this project as it has no dependency on Visual Studio.
/// </summary>
public class VisualStudioNavigationTests
{
    private const string Source = """"
        using System;

        namespace Demo
        {
            /// <summary>A class.</summary>
            public class FirstTests
            {
                private const string Text = "{ not a block } // nor a comment";

                // a comment with a brace {
                [Test]
                [Arguments(new[] { 1, 2 }, "a}")]
                public async Task First_method(int[] values, string text)
                {
                    var interpolated = $"{values.Length} {{ }}";
                    var verbatim = @"C:\folder\""quoted"" {";
                    var raw = """
                        { """;
                    char brace = '}';

                    void LocalFunction()
                    {
                        if (values.Length > 0) { }
                    }
                }

                public int Property { get; set; }

                /// <summary>Documentation.</summary>
                [MarkdownDataSource("~/Tests/{method}.md")]
                public void Second_method<T>(T value) where T : class
                {
                }

                public class Nested
                {
                    public void Nested_method() { }
                }

                public FirstTests() { }

                public void Third_method() => Console.WriteLine();

                public void Fourth_method()
                {
                }
            }

            public record Person(string Name)
            {
                public void Record_method() { }
            }
        }
        """";

    private static MethodSpot? At(string marker, int offset = 0) => MethodLocator.Locate(Source, Source.IndexOf(marker, StringComparison.Ordinal) + offset);

    [Test]
    public async Task A_position_in_the_body_of_a_method_gives_the_method_and_its_class()
    {
        var spot = At("var interpolated");

        await Assert.That(spot).IsNotNull();
        await Assert.That(spot!.Value.ClassName).IsEqualTo("FirstTests");
        await Assert.That(spot.Value.MethodName).IsEqualTo("First_method");
    }

    [Test]
    public async Task The_attributes_and_the_documentation_above_a_method_belong_to_it()
    {
        await Assert.That(At("[Test]")!.Value.MethodName).IsEqualTo("First_method");
        await Assert.That(At("[Arguments")!.Value.MethodName).IsEqualTo("First_method");
        await Assert.That(At("/// <summary>Documentation.")!.Value.MethodName).IsEqualTo("Second_method");
        await Assert.That(At("[MarkdownDataSource")!.Value.MethodName).IsEqualTo("Second_method");
        await Assert.That(At("public void Second_method")!.Value.MethodName).IsEqualTo("Second_method");
    }

    [Test]
    public async Task Braces_in_strings_comments_and_characters_do_not_count()
    {
        // after all of them, the following method is still found
        await Assert.That(At("if (values.Length > 0)")!.Value.MethodName).IsEqualTo("First_method");
        await Assert.That(At("Fourth_method()")!.Value.MethodName).IsEqualTo("Fourth_method");
    }

    [Test]
    public async Task A_method_of_a_nested_class_and_of_a_record_get_their_own_class()
    {
        await Assert.That(At("Nested_method")!.Value.ClassName).IsEqualTo("Nested");
        await Assert.That(At("Record_method")!.Value.ClassName).IsEqualTo("Person");
    }

    [Test]
    public async Task A_generic_constraint_is_not_taken_for_a_class()
    {
        var spot = At("Second_method<T>");

        await Assert.That(spot!.Value.ClassName).IsEqualTo("FirstTests");
    }

    [Test]
    public async Task Elsewhere_in_the_class_there_is_no_method()
    {
        await Assert.That(At("public int Property")).IsNull();
        await Assert.That(At("private const string Text")).IsNull();
        await Assert.That(At("using System;")).IsNull();
        await Assert.That(MethodLocator.Locate("", 0)).IsNull();
    }

    [Test]
    public async Task Every_marker_of_the_markdown_files_of_the_project_leads_to_its_method_and_back()
    {
        var projectFolder = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));
        var checkedMarkers = 0;

        foreach (var markdown in Directory.EnumerateFiles(Path.Combine(projectFolder, "Tests"), "*.md"))
        {
            foreach (var text in MarkdownFinder.ReadLines(markdown))
            {
                var match = Regex.Match(text, @"mdat:(?<file>[^#\s]+)#(?<class>\w+)\.(?<method>\w+)");

                if (!match.Success) continue;

                var sourcePath = Path.Combine(projectFolder, match.Groups["file"].Value);
                var source = string.Join("\n", MarkdownFinder.ReadLines(sourcePath));
                var method = match.Groups["method"].Value;

                // the cursor on the name of the method in its declaration
                // the declaration of the method in its class (two classes of a file can have a method of the same name)
                var classStart = Regex.Match(source, @"\bclass\s+" + match.Groups["class"].Value + @"\b").Index;
                var declaration = Regex.Match(source.Substring(classStart), @"\b" + method + @"\s*[(<]");
                await Assert.That(declaration.Success).IsTrue();

                var spot = MethodLocator.Locate(source, classStart + declaration.Index + 1);
                await Assert.That(spot).IsNotNull();
                await Assert.That(spot!.Value.ClassName).IsEqualTo(match.Groups["class"].Value);
                await Assert.That(spot.Value.MethodName).IsEqualTo(method);

                var found = MarkdownFinder.Find(sourcePath, spot.Value.ClassName, spot.Value.MethodName);
                await Assert.That(found).IsNotNull();
                await Assert.That(found!.Value.Path).IsEqualTo(markdown);

                checkedMarkers++;
            }
        }

        await Assert.That(checkedMarkers).IsGreaterThan(10);
    }

    [Test]
    public async Task The_marker_of_a_method_is_not_the_marker_of_a_method_that_starts_with_its_name()
    {
        var root = Directory.CreateTempSubdirectory("mdat-find-");

        try
        {
            File.WriteAllText(Path.Combine(root.FullName, "Demo.csproj"), "<Project />");
            Directory.CreateDirectory(Path.Combine(root.FullName, "Tests"));
            Directory.CreateDirectory(Path.Combine(root.FullName, "obj"));

            var source = Path.Combine(root.FullName, "Demo.cs");
            File.WriteAllText(Path.Combine(root.FullName, "Tests", "longer.md"), "# Longer\n\n> C# : [Demo.cs:9](../Demo.cs#L9) <!-- mdat:Demo.cs#DemoTests.Add_more -->\n");
            File.WriteAllText(Path.Combine(root.FullName, "Tests", "short.md"), "# Short\n\n\n> C# : [Demo.cs:5](../Demo.cs#L5) <!-- mdat:Demo.cs#DemoTests.Add -->\n");
            File.WriteAllText(Path.Combine(root.FullName, "obj", "ignored.md"), "<!-- mdat:Demo.cs#DemoTests.Hidden -->\n");

            var found = MarkdownFinder.Find(source, "DemoTests", "Add");

            await Assert.That(found).IsNotNull();
            await Assert.That(Path.GetFileName(found!.Value.Path)).IsEqualTo("short.md");
            await Assert.That(found.Value.Line).IsEqualTo(4);

            await Assert.That(MarkdownFinder.Find(source, "DemoTests", "Hidden")).IsNull();
            await Assert.That(MarkdownFinder.Find(source, "DemoTests", "Missing")).IsNull();
            await Assert.That(MarkdownFinder.SearchRoot(source)).IsEqualTo(root.FullName);
        }
        finally
        {
            root.Delete(recursive: true);
        }
    }
}
