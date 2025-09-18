// MDAT.Core
using System;
using System.Reflection;

namespace MDAT
{
    public enum TestOutcome { Passed, Failed, Skipped }

    public sealed record TestCase(
        MethodInfo Method,
        object?[]? Arguments,
        string DisplayName,
        string? SkipReason,
        int? TimeoutMs
    );

    public sealed record TestResult(
        TestCase Case,
        TestOutcome Outcome,
        TimeSpan Duration,
        string? ErrorMessage = null,
        string? ErrorStackTrace = null
    );
}
