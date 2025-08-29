using Xunit;

namespace MDAT
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    [Xunit.Sdk.XunitTestCaseDiscoverer("MDAT.MarkdownTheoryDiscoverer", "MDAT")]
    public sealed class MarkdownTheoryAttribute : FactAttribute
    {
        public MarkdownTheoryAttribute(string filePath) => FilePath = filePath;
        public string FilePath { get; }
    }
}