using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using System.Reflection;

namespace MDAT.Adapter
{
    [FileExtension(".dll")]
    [FileExtension(".exe")]
    [DefaultExecutorUri(ExecutorUri)]
    public sealed class MyDiscoverer : ITestDiscoverer
    {
        public const string ExecutorUri = "executor://mdat/v1";

        public void DiscoverTests(IEnumerable<string> sources, IDiscoveryContext ctx,
                                  IMessageLogger log, ITestCaseDiscoverySink sink)
        {
            foreach (var src in sources)
            {
                try
                {
                    var asm = Assembly.LoadFrom(src);
                    var cases = MDAT.MdatDiscovery.Discover(asm);
                    foreach (var c in cases)
                    {
                        var tc = new Microsoft.VisualStudio.TestPlatform.ObjectModel.TestCase(c.DisplayName, new Uri(ExecutorUri), src)
                        {
                            FullyQualifiedName = $"{c.Method.DeclaringType!.FullName}.{c.Method.Name}"
                        };
                        sink.SendTestCase(tc);
                    }
                }
                catch (Exception ex)
                {
                    log.SendMessage(TestMessageLevel.Error, $"MDAT discover failed for {src}: {ex}");
                }
            }
        }
    }
}