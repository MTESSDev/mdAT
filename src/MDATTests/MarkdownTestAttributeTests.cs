using LoxSmoke.DocXml;
using MDATTests;
using MDATTests.Models;
using Moq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Xunit;

namespace MDAT.Tests
{
    public class MarkdownTheoryTests
    {
        public class Params
        {
            public Dictionary<object, object> Test { get; set; }
        }

        public static IEnumerable<object[]> ReusableTestDataProperty => new[]
        {
            new object[]
            {
                new Params
                {
                    Test = new Dictionary<object, object>
                    {
                        { "test", new Dictionary<object, object> { { "test", 1 } } }
                    }
                }
            },
        };

        /// <summary>
        /// Simple test avec données réutilisables
        /// </summary>
        [Theory]
        [MemberData(nameof(ReusableTestDataProperty))]
        public async Task Dynam(Params val1)
        {
            _ = await Verify.Assert(
                () => Task.FromResult(val1),
                new Expected
                {
                    verify = new[]
                    {
                        new VerifyStep
                        {
                            data = "{\"Test\":{\"test\":{\"test\":1}}}",
                            type = "match",
                            jsonPath = "$"
                        }
                    }
                });
        }

        /// <summary>
        /// Markdown data-driven (ex-[MarkdownTest("~/Tests/test.md")])
        /// </summary>
        [MarkdownTheory("~/Tests/test.md")]
        public async Task Md1(int val1, int val2, string expected)
        {
            _ = await Verify.Assert(() => Task.FromResult(Utils.Calculer(val1, val2)), expected);
        }

        [MarkdownTheory("~/Tests/test-justme.md")]
        public async Task Md_Justme(int val1, int val2, string expected)
        {
            _ = await Verify.Assert(() => Task.FromResult(Utils.Calculer(val1, val2)), expected);
        }

        [MarkdownTheory("~/Tests/test-skip.md")]
        public async Task Md_Skip(int val1, int val2, string expected)
        {
            _ = await Verify.Assert(() => Task.FromResult(Utils.Calculer(val1, val2)), expected);
        }

        [MarkdownTheory("~/Tests/{method}.md")]
        public async Task Multi_level(FormulaireWebFRW1DO form, Expected expected)
        {
            _ = await Verify.Assert(() => Task.FromResult(form), expected);
        }

        [MarkdownTheory("~/Tests/{method}.md")]
        public async Task External_file(FormulaireWebFRW1DO form, Expected expected)
        {
            _ = await Verify.Assert(() => Task.FromResult(form), expected);
        }

        [MarkdownTheory("~/Tests/{method}.md")]
        public async Task Exception_test(FormulaireWebFRW1DO form, Expected expected)
        {
            _ = await Verify.Assert(() => Task.FromResult(ThrowE()), expected);
        }

        private static bool ThrowE() => throw new InvalidOperationException();

        [MarkdownTheory("~/Tests/{method}.md")]
        public async Task External_file2(string form, Expected expected)
        {
            _ = await Verify.Assert(() => Task.FromResult(form), expected);
        }

        [MarkdownTheory("~/Tests/{method}.md")]
        public async Task External_file_base64(byte[] form, Expected expected)
        {
            _ = await Verify.Assert(() => Task.FromResult(form), expected);
        }

        [MarkdownTheory("~/Tests/{method}.md")]
        public async Task Nullable_byte_array(byte[]? form, Expected expected)
        {
            _ = await Verify.Assert(() => Task.FromResult(form), expected);
        }

        [MarkdownTheory("~/Tests/{method}.md")]
        public async Task External_file_base64_String(string form, Expected expected)
        {
            _ = await Verify.Assert(() => Task.FromResult(form), expected);
        }

        [MarkdownTheory("~/Tests/{method}.md")]
        public async Task External_file_byte_dictionary(Dictionary<string, byte[]> form, Expected expected)
        {
            _ = await Verify.Assert(() => Task.FromResult(form), expected);
        }

        [MarkdownTheory("~/Tests/{method}.md")]
        public async Task External_file_string_dictionary(Dictionary<string, string> form, Expected expected)
        {
            _ = await Verify.Assert(() => Task.FromResult(form), expected);
        }

        [MarkdownTheory("~/Tests/{method}.md")]
        public async Task Dictionary_object_object(Dictionary<object, object> dict, Expected expected)
        {
            _ = await Verify.Assert(() => Task.FromResult(dict), expected);
        }

        [MarkdownTheory("~/Tests/{method}.md")]
        public async Task Dictionary_object_object_in_Dictionary_object_object(Dictionary<object, object> dict, Expected expected)
        {
            _ = await Verify.Assert(() => Task.FromResult(dict), expected);
        }

        [MarkdownTheory("~/Tests/{method}.md")]
        public async Task Crash_Test(int val1, int val2, Expected expected)
        {
            _ = await Verify.Assert(() => Task.FromResult(Utils.CrashExceptionCheck(val1, val2)), expected);
        }

        // --- Helpers de doc pour la génération/extraction ---
        public void MdWithSummary(FormulaireWebFRW1DO db, Expected expected) { }
        public void MdWithoutSummary(FormulaireWebFRW1DO db, Expected expected) { }

        /// <summary>
        /// Remplace l’ancien test qui appelait .GetData() pour générer un fichier
        /// </summary>
        /*[Fact]
        public async Task Md_Extraction_test()
        {
            var guid = Guid.NewGuid();
            var path = $"~\\Tests\\Generated\\Generated-md-Test-{guid}.md";

            var method = Utils.GetMethodInfo(() => MdWithSummary);
            MarkdownScaffold.CreateFor(method, path);

            var expected = *//* ton Expected d’origine *//*;
            object value = await Verify.Assert(() => Task.FromResult(File.ReadAllText(Extensions.GetCurrentPath(path, false))), expected);
        }*/

        [MarkdownTheory("~\\Tests\\md-json-document-test.md")]
        public async Task Md_JsonDocument_test(JsonDocument jsonDocument, Test1 test1, Expected expected, Expected expected2)
        {
            object value = await Verify.Assert(() => Task.FromResult(jsonDocument), expected);
            object value2 = await Verify.Assert(() => Task.FromResult(test1), expected2);
        }

        public class Test1 { public JsonDocument JsonDocument { get; set; } }

        /*[Fact]
        public async Task Md_Extraction_test_nosummary()
        {
            var guid = Guid.NewGuid();
            var path = $"~\\Tests\\Generated\\Generated-md-Test-nosummary-{guid}.md";
            var method = Utils.GetMethodInfo(() => MdWithoutSummary);

            MarkdownScaffold.CreateFor(method, path);

            var expected = *//* ton Expected *//*;
            object value = await Verify.Assert(() => Task.FromResult(File.ReadAllText(Extensions.GetCurrentPath(path, false))), expected);
        }*/

        [MarkdownTheory("~/Tests/{method}.md")]
        public async Task Output_expected(Dictionary<string, string> form, byte[] bytes, Expected expected)
        {
            _ = await Verify.Assert(() => Task.FromResult(new { form, bytes }), expected);
        }

        [MarkdownTheory("~/Tests/{method}.md")]
        public async Task JsonPath(Expected expected)
        {
            _ = await Verify.Assert(() => Task.FromResult(new { test = new string[] { "val1", "val2" } }), expected);
        }

        [MarkdownTheory("~/Tests/{method}.md")]
        public async Task TestObjectOrException(
            ObjectOrException<FormulaireWebFRW1DO> formulaireWebFRW1DO,
            Expected expected)
        {
            var mock = new Mock<IFormulaireWebFRW1DO>();
            mock.Setup(e => e.ReturnVal()).ReturnsOrThrows(formulaireWebFRW1DO);
            _ = await Verify.Assert(() => Task.FromResult(new Test().ReturnTest(mock.Object)), expected);
        }

        [MarkdownTheory("~/Tests/{method}.md")]
        public async Task TestObjectOrExceptionAsync(
            ObjectOrException<FormulaireWebFRW1DO> formulaireWebFRW1DO,
            Expected expected)
        {
            var mock = new Mock<IFormulaireWebFRW1DO>();
            _ = mock.Setup(e => e.ReturnValAsync()).ReturnsOrThrowsAsync(formulaireWebFRW1DO);
            _ = await Verify.Assert(async () => await new Test().ReturnTestAsync(mock.Object), expected);
        }

        [MarkdownTheory("~\\Tests\\{method}.md")]
        public async Task Test_keypairvalues(List<KeyValuePair<string, string>> input, Expected expected)
        {
            object value = await Verify.Assert(() => Task.FromResult(input), expected);
        }

        public class Test
        {
            public FormulaireWebFRW1DO ReturnTest(IFormulaireWebFRW1DO form) => form.ReturnVal();
            public Task<FormulaireWebFRW1DO> ReturnTestAsync(IFormulaireWebFRW1DO form) => form.ReturnValAsync();
        }
    }

    public static class MarkdownScaffold
    {
        public static void CreateFor(MethodInfo runtimeMethod, string templatedPath)
        {
            if (runtimeMethod is null) throw new ArgumentNullException(nameof(runtimeMethod));
            if (string.IsNullOrWhiteSpace(templatedPath)) throw new ArgumentNullException(nameof(templatedPath));

            var resolvedPath = Extensions.GetCurrentPath(
                templatedPath.Replace("{method}", runtimeMethod.Name.ToKebabCase().ToLowerInvariant()),
                false);

            Directory.CreateDirectory(Path.GetDirectoryName(resolvedPath)!);

            var asm = runtimeMethod.DeclaringType!.Assembly;
            var xmlFilePath = Path.ChangeExtension(asm.Location, ".xml");

            MethodComments? methodComments = null;
            if (File.Exists(xmlFilePath))
            {
                var reader = new DocXmlReader(xmlFilePath);
                methodComments = reader.GetMethodComments(runtimeMethod);
            }

            var sb = new StringBuilder();
            foreach (var p in runtimeMethod.GetParameters())
            {
                var pDoc = methodComments?.Parameters.FirstOrDefault(e => e.Name == p.Name);
                var docLine = (pDoc is { } && pDoc.Value.Text is { }) ? $"# {pDoc.Value.Text}\n" : string.Empty;

                sb.Append(docLine);
                sb.Append(p.Name);
                sb.Append(":\n");
                sb.Append(MarkdownTheoryDiscoverer_Internals.DescribeTypeOfObject(p.ParameterType, "  "));
            }

            var summary = (methodComments is { } && !string.IsNullOrWhiteSpace(methodComments.Summary))
                            ? $"\n\n> {methodComments.Summary.ReplaceLineEndings("\\\n")}"
                            : string.Empty;

            var content = $"# {runtimeMethod.Name}{summary}\n\n## Case 1\n\nDescription\n\n``````yaml\n{sb}``````";
            File.WriteAllText(resolvedPath, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        }
    }

    internal static class MarkdownTheoryDiscoverer_Internals
    {
        internal static string? DescribeTypeOfObject(Type type, string indent, int depth = 0)
        {
            if (depth >= 10) return string.Empty;
            depth++;

            string? obj = null;

            if (type.IsClass && type.FullName is { } fn && !fn.StartsWith("System.", StringComparison.Ordinal))
            {
                foreach (var pi in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    var def = GetDefaultValueForProperty(pi);
                    var inner = DescribeTypeOfObject(pi.PropertyType, indent + "  ", depth);
                    obj += $"{indent}{pi.Name}: {(string.IsNullOrEmpty(inner) ? GetDefaultValue(def) : "\n" + inner)}";
                }
            }
            else if (IsGenericEnumerable(type, out var elemType))
            {
                var props = elemType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                var first = true;
                foreach (var pi in props)
                {
                    var def = GetDefaultValueForProperty(pi);
                    var inner = DescribeTypeOfObject(pi.PropertyType, indent + "    ", depth);
                    obj += $"{indent}{(first ? "- " : "  ")}{pi.Name}: {(string.IsNullOrEmpty(inner) ? GetDefaultValue(def) : "\n" + inner)}";
                    first = false;
                }
            }

            return obj;
        }

        internal static bool IsGenericEnumerable(Type t, out Type elementType)
        {
            elementType = typeof(object);
            if (t == typeof(string)) return false;

            var ienum = t.GetInterfaces()
                .Concat(new[] { t })
                .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));
            if (ienum is null) return false;

            elementType = ienum.GetGenericArguments()[0];
            return true;
        }

        internal static string GetDefaultValue(object? def)
            => def is { } ? new YamlDotNet.Serialization.Serializer().Serialize(def).ReplaceLineEndings("\n") : "null\n";

        internal static object? GetDefaultValueForProperty(PropertyInfo property)
        {
            var defaultAttr = property.GetCustomAttribute(typeof(System.ComponentModel.DefaultValueAttribute));
            if (defaultAttr != null)
                return (defaultAttr as System.ComponentModel.DefaultValueAttribute)?.Value;

            var t = property.PropertyType;
            return t.IsValueType ? Activator.CreateInstance(t) : null;
        }
    }
}