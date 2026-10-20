using System.Collections.Concurrent;
using System.Reflection;

namespace MDAT;

/// <summary>
/// All the tests of an assembly that read each markdown file, found once by reading the attributes of the assembly.
/// It lets the first test that reaches a markdown file write the links of every test that shares it in one go.
/// </summary>
internal static class SourceLinkPlan
{
    private static readonly ConcurrentDictionary<Assembly, Lazy<Dictionary<string, List<SourceLinkEntry>>>> ByAssembly = new();

    private const BindingFlags AllMethods = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

    /// <summary>The tests that read <paramref name="markdownFullPath"/>, including <paramref name="own"/>, the one being discovered.</summary>
    public static IReadOnlyList<SourceLinkEntry> For(Assembly assembly, string markdownFullPath, SourceLinkEntry own)
    {
        var plan = ByAssembly.GetOrAdd(assembly, a => new Lazy<Dictionary<string, List<SourceLinkEntry>>>(() => Build(a))).Value;

        var entries = plan.TryGetValue(markdownFullPath, out var found) ? found.ToList() : new List<SourceLinkEntry>();

        // the scan can miss a test (a type that cannot be loaded, ...): the test being discovered always has its link
        if (!entries.Any(e => e.TestName == own.TestName && e.SourceFilePath == own.SourceFilePath))
        {
            entries.Add(own);
        }

        return entries;
    }

    private static Dictionary<string, List<SourceLinkEntry>> Build(Assembly assembly)
    {
        var plan = new Dictionary<string, List<SourceLinkEntry>>(StringComparer.OrdinalIgnoreCase);

        foreach (var type in TypesOf(assembly))
        {
            foreach (var method in type.GetMethods(AllMethods))
            {
                IEnumerable<MarkdownDataSourceAttribute> attributes;

                try
                {
                    attributes = method.GetCustomAttributes<MarkdownDataSourceAttribute>(inherit: false).ToList();
                }
                catch (Exception ex) when (ex is TypeLoadException or FileNotFoundException or BadImageFormatException)
                {
                    continue;
                }

                foreach (var attribute in attributes)
                {
                    try
                    {
                        var path = Path.GetFullPath(new MarkdownTestSource(attribute.FilePathTemplate).ResolvePath(method));

                        if (!plan.TryGetValue(path, out var entries))
                        {
                            plan[path] = entries = new List<SourceLinkEntry>();
                        }

                        entries.Add(new SourceLinkEntry(attribute.DeclaredSourceFilePath, attribute.DeclaredSourceLine, $"{type.Name}.{method.Name}"));
                    }
                    catch (Exception ex) when (ex is ArgumentException or NotSupportedException or IOException)
                    {
                        // a path that cannot be resolved is left to the data source of that test
                    }
                }
            }
        }

        return plan;
    }

    private static IEnumerable<Type> TypesOf(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(t => t is not null)!;
        }
    }
}
