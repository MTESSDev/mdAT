namespace MDAT
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class FactAttribute : Attribute
    {
        public string? DisplayName { get; set; }
        public string? Skip { get; set; }
        public int? TimeoutMs { get; set; }
        public string[]? Traits { get; set; }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class TheoryAttribute : Attribute
    {
        public string? DisplayName { get; set; }
        public string? Skip { get; set; }
        public int? TimeoutMs { get; set; }
        public string[]? Traits { get; set; }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public sealed class InlineDataAttribute : Attribute
    {
        public object?[] Data { get; }
        public InlineDataAttribute(params object?[] data) => Data = data;
    }

    /// <summary>Exécute une théorie à partir d’un fichier Markdown contenant des blocs YAML (``````yaml ... ``````).</summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class MarkdownTheoryAttribute : TheoryAttribute
    {
        public MarkdownTheoryAttribute(string filePath) => FilePath = filePath;
        public string FilePath { get; }
    }
}