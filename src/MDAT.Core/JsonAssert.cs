using MDAT.JsonCompare;

namespace MDAT;

/// <summary>Options of a JSON comparison.</summary>
public sealed class JsonDiffConfig
{
    public JsonDiffConfig(bool allowAdditionalProperties, bool ignoreArrayOrder = false)
    {
        AllowAdditionalProperties = allowAdditionalProperties;
        IgnoreArrayOrder = ignoreArrayOrder;
    }

    /// <summary>When true, properties found only in the actual JSON are not reported.</summary>
    public bool AllowAdditionalProperties { get; }

    /// <summary>When true, two arrays are equal if they hold the same values, in any order (duplicates are counted).</summary>
    public bool IgnoreArrayOrder { get; }

    public override string ToString() => $"AllowAdditionalProperties={AllowAdditionalProperties}, IgnoreArrayOrder={IgnoreArrayOrder}";
}

/// <summary>Thrown when two JSON documents differ.</summary>
public sealed class JsonAssertException : Exception
{
    public JsonAssertException(IReadOnlyList<string> diffMessages, string expected, string actual)
        : base(BuildMessage(diffMessages, expected, actual))
    {
        DiffMessages = diffMessages;
        UserMessage = BuildUserMessage(diffMessages);
    }

    /// <summary>One message per difference.</summary>
    public IReadOnlyList<string> DiffMessages { get; }

    /// <summary>The differences without the expected and actual documents.</summary>
    public string UserMessage { get; }

    private static string BuildUserMessage(IReadOnlyList<string> diffMessages)
    {
        if (diffMessages.Count == 1)
        {
            return diffMessages[0];
        }

        var numbered = diffMessages.Select((message, index) => $"# {index + 1}: {message}");
        return $"Found {diffMessages.Count} differences.\n{string.Join("\n", numbered)}";
    }

    private static string BuildMessage(IReadOnlyList<string> diffMessages, string expected, string actual)
    {
        return $"{BuildUserMessage(diffMessages)}{Environment.NewLine}Expected: {expected}{Environment.NewLine}Actual: {actual}";
    }
}

/// <summary>Asserts that two JSON documents are equal, with a readable description of the differences.</summary>
public static class JsonAssert
{
    public static void Equal(string expected, string actual) => EqualOverrideDefault(expected, actual, new JsonDiffConfig(false));

    public static void EqualOverrideDefault(string expected, string actual, JsonDiffConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        var actualTree = JsonTree.Parse(actual, "leftJsonString");
        var expectedTree = JsonTree.Parse(expected, "rightJsonString");

        var differences = JsonComparer.Compare(expectedTree, actualTree, config);

        if (differences.Count > 0)
        {
            throw new JsonAssertException(differences, expected, actual);
        }
    }
}
