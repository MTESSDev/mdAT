using System.Reflection;
using System.Threading;

namespace MDAT
{
    public static class MdatDiscovery
    {
        public static IReadOnlyList<TestCase> Discover(Assembly asm)
        {
            var cases = new List<TestCase>();

            foreach (var type in asm.GetTypes())
            {
                if (!type.IsClass || type.IsAbstract) continue;

                var methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                foreach (var m in methods)
                {
                    var fact = m.GetCustomAttribute<FactAttribute>();
                    var theory = m.GetCustomAttribute<TheoryAttribute>();
                    if (fact is null && theory is null) continue;

                    // Fact simple
                    if (fact is not null && m.GetCustomAttribute<MarkdownTheoryAttribute>() is null)
                    {
                        cases.Add(new TestCase(
                            m,
                            Array.Empty<object?>(),
                            fact.DisplayName ?? $"{type.Name}.{m.Name}",
                            fact.Skip,
                            fact.TimeoutMs
                        ));
                        continue;
                    }

                    // Theories (InlineData)
                    var inlines = m.GetCustomAttributes<InlineDataAttribute>().ToList();
                    foreach (var inline in inlines)
                    {
                        cases.Add(new TestCase(
                            m,
                            inline.Data,
                            theory?.DisplayName ?? $"{type.Name}.{m.Name}({FormatArgs(inline.Data)})",
                            theory?.Skip,
                            theory?.TimeoutMs
                        ));
                    }

                    var providers = m.GetCustomAttributes().OfType<Attribute>()
                     .OfType<ITestDataProvider>()
                     .ToList();

                    var skip = theory?.Skip ?? fact?.Skip;
                    var timeout = theory?.TimeoutMs ?? fact?.TimeoutMs;

                    foreach (var prov in providers)
                    {
                        foreach (var row in prov.GetData(m))
                        {
                            var dn = prov.GetDisplayName(m, row)
                                     ?? $"{type.Name}.{m.Name}({string.Join(", ", row.Select(a => a ?? "null"))})";
                            cases.Add(new TestCase(m, row, dn, skip, timeout));
                        }
                    }

                    // MarkdownTheory
                    var md = m.GetCustomAttribute<MarkdownTheoryAttribute>();
                    if (md is not null)
                    {
                        var resolvedPath = MarkdownCaseLoader.ResolvePath(m, md.FilePath);
                        if (!File.Exists(resolvedPath))
                        {
                            MarkdownCaseLoader.CreateMarkdownSkeletonIfMissing(m, resolvedPath);
                            // continue;  // comme tu avais
                        }
                        else
                        {
                            foreach (var row in MarkdownCaseLoader.Load(m, resolvedPath))
                            {
                                var dn = $"{type.Name}.{m.Name}@{resolvedPath}{row.Label}";
                                cases.Add(new TestCase(m, row.Arguments, dn, skip, timeout));
                            }
                        }
                    }
                }
            }

            return cases;
        }

        public static async Task<Microsoft.VisualStudio.TestPlatform.ObjectModel.TestResult> RunAsync(TestCase tc)
        {
            if (!string.IsNullOrWhiteSpace(tc.SkipReason))
                return new Microsoft.VisualStudio.TestPlatform.ObjectModel.TestResult(tc, TestOutcome.Skipped, TimeSpan.Zero, tc.SkipReason);

            object? instance = null;
            try
            {
                if (!tc.Method.IsStatic)
                    instance = Activator.CreateInstance(tc.Method.DeclaringType!);

                var args = tc.Arguments ?? Array.Empty<object?>();
                var start = DateTime.UtcNow;

                var ret = tc.Method.Invoke(instance, args);
                if (ret is Task t)
                {
                    if (tc.TimeoutMs is { } ms)
                    {
                        var done = await Task.WhenAny(t, Task.Delay(ms));
                        if (done != t) throw new AssertFailedException($"Timeout after {ms} ms.");
                    }
                    await t; // exceptions surfacent ici
                }

                var dur = DateTime.UtcNow - start;
                return new Microsoft.VisualStudio.TestPlatform.ObjectModel.TestResult(tc, TestOutcome.Passed, dur);
            }
            catch (TargetInvocationException tex)
            {
                var e = tex.InnerException ?? tex;
                return new Microsoft.VisualStudio.TestPlatform.ObjectModel.TestResult(tc, TestOutcome.Failed, TimeSpan.Zero, e.Message, e.ToString());
            }
            catch (Exception e)
            {
                return new Microsoft.VisualStudio.TestPlatform.ObjectModel.TestResult(tc, TestOutcome.Failed, TimeSpan.Zero, e.Message, e.ToString());
            }
        }

        private static string FormatArgs(object?[] data)
            => string.Join(", ", data.Select(a => a is null ? "null" : a is string s ? $"\"{s}\"" : a.ToString()));
    }
}
