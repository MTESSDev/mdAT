// MDAT.Runner
using System.Reflection;
using MDAT;

class Program
{
    static async Task<int> Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Usage: mdat.runner <path-to-test-assembly.dll> [--filter <substring>]");
            return 2;
        }

        var asmPath = args[0];
        string? filter = null;
        for (int i = 1; i < args.Length; i++)
        {
            if (args[i] == "--filter" && i + 1 < args.Length) filter = args[++i];
        }

        var asm = Assembly.LoadFrom(asmPath);
        var all = MdatDiscovery.Discover(asm);
        var toRun = string.IsNullOrWhiteSpace(filter)
            ? all
            : all.Where(tc => tc.DisplayName.Contains(filter!, StringComparison.OrdinalIgnoreCase)).ToList();

        Console.WriteLine($"Discovered: {all.Count}, Running: {toRun.Count}\n");

        int passed = 0, failed = 0, skipped = 0;
        var sw = System.Diagnostics.Stopwatch.StartNew();

        foreach (var tc in toRun)
        {
            var res = await MdatDiscovery.RunAsync(tc);
            switch (res.Outcome)
            {
                case TestOutcome.Passed:
                    passed++;
                    Console.WriteLine($"[PASS] {tc.DisplayName} ({res.Duration.TotalMilliseconds:N0} ms)");
                    break;
                case TestOutcome.Skipped:
                    skipped++;
                    Console.WriteLine($"[SKIP] {tc.DisplayName} — {res.ErrorMessage}");
                    break;
                case TestOutcome.Failed:
                    failed++;
                    Console.WriteLine($"[FAIL] {tc.DisplayName}\n{res.ErrorMessage}\n{res.ErrorStackTrace}\n");
                    break;
            }
        }

        sw.Stop();
        Console.WriteLine($"\nSummary: Passed={passed} Failed={failed} Skipped={skipped} Total={passed + failed + skipped} Time={sw.Elapsed}");
        return failed == 0 ? 0 : 1;
    }
}