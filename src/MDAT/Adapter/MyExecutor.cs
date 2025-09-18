using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using System.Reflection;

namespace MDAT.Adapter
{
    [ExtensionUri(MyDiscoverer.ExecutorUri)]
    public sealed class MyExecutor : ITestExecutor
    {
        private volatile bool _cancel;

        public void RunTests(IEnumerable<string> sources, IRunContext runCtx, IFrameworkHandle handle)
        {
            foreach (var src in sources)
            {
                var asm = Assembly.LoadFrom(src);
                var cases = MDAT.MdatDiscovery.Discover(asm);

                foreach (var c in cases)
                {
                    if (_cancel) return;
                    var vstc = new Microsoft.VisualStudio.TestPlatform.ObjectModel.TestCase(c.DisplayName, new Uri(MyDiscoverer.ExecutorUri), src)
                    {
                        FullyQualifiedName = $"{c.Method.DeclaringType!.FullName}.{c.Method.Name}"
                    };

                    handle.RecordStart(vstc);
                    var res = MDAT.MdatDiscovery.RunAsync(c).GetAwaiter().GetResult();

                    Microsoft.VisualStudio.TestPlatform.ObjectModel.TestOutcome outcome = res.Outcome;

                    var tr = new TestResult(vstc)
                    {
                        Case = vstc,
                        Outcome = outcome,
                        Duration = res.Duration,
                        ErrorMessage = res.ErrorMessage,
                        ErrorStackTrace = res.ErrorStackTrace
                    };

                    handle.RecordResult(tr);
                    handle.RecordEnd(vstc, outcome);
                }
            }
        }

        public void RunTests(IEnumerable<Microsoft.VisualStudio.TestPlatform.ObjectModel.TestCase> tests, IRunContext runCtx, IFrameworkHandle handle)
        {
            // Version simple : on redécouvre dans l’assembly et on filtre par DisplayName
            foreach (var t in tests)
            {
                var asm = Assembly.LoadFrom(t.Source);
                var cases = MDAT.MdatDiscovery.Discover(asm);
                var match = cases.FirstOrDefault(c => c.DisplayName == t.DisplayName)
                         ?? cases.FirstOrDefault(c => $"{c.Method.DeclaringType!.FullName}.{c.Method.Name}" == t.FullyQualifiedName);

                if (match is null)
                {
                    handle.RecordEnd(t, TestOutcome.Skipped);
                    continue;
                }

                handle.RecordStart(t);
                var res = MDAT.MdatDiscovery.RunAsync(match).GetAwaiter().GetResult();

                var outcome = res.Outcome switch
                {
                    MDAT.TestOutcome.Passed => TestOutcome.Passed,
                    MDAT.TestOutcome.Skipped => TestOutcome.Skipped,
                    _ => TestOutcome.Failed
                };

                var tr = new TestResult(t)
                {
                    Outcome = outcome,
                    Duration = res.Duration,
                    ErrorMessage = res.ErrorMessage,
                    ErrorStackTrace = res.ErrorStackTrace
                };

                handle.RecordResult(tr);
                handle.RecordEnd(t, outcome);
            }
        }

        public void Cancel() => _cancel = true;
    }
}
