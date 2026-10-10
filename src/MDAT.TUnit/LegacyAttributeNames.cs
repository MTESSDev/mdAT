namespace MDAT;

/// <summary>
/// The name used by mdAT 1.x: exactly the same as <see cref="MarkdownDataSourceAttribute"/>.
/// Both names are supported, so test files written for mdAT 1.x do not have to change.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class MarkdownTestAttribute : MarkdownDataSourceAttribute
{
    public MarkdownTestAttribute(string filePath) : base(filePath)
    {
    }
}

/// <summary>
/// The name used by mdAT 1.x: exactly the same as <see cref="YamlFileDataSourceAttribute"/>.
/// Both names are supported, so test files written for mdAT 1.x do not have to change.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class YamlFileDataAttribute : YamlFileDataSourceAttribute
{
    public YamlFileDataAttribute(string filePath) : base(filePath)
    {
    }
}
