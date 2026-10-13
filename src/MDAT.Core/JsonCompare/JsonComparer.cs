namespace MDAT.JsonCompare;

/// <summary>Compares an expected JSON tree with an actual one and lists the differences as readable messages.</summary>
internal sealed class JsonComparer
{
    private readonly JsonDiffConfig _config;

    private JsonComparer(JsonDiffConfig config)
    {
        _config = config;
    }

    public static IReadOnlyList<string> Compare(JsonTree expected, JsonTree actual, JsonDiffConfig config)
    {
        var messages = new List<string>();
        new JsonComparer(config).Walk(expected, actual, "$", messages);
        return messages;
    }

    private void Walk(JsonTree expected, JsonTree actual, string path, List<string> messages)
    {
        if (TypeClass(expected) != TypeClass(actual))
        {
            messages.Add($"Type mismatch at {path}.\nExpected {JsonTreeText.Describe(expected)} but was {JsonTreeText.Describe(actual)}.");
            return;
        }

        switch (expected.Kind)
        {
            case JsonKind.Null:
                break;
            case JsonKind.True:
            case JsonKind.False:
                if (expected.Kind != actual.Kind)
                {
                    messages.Add($"Boolean value mismatch at {path}.\nExpected {(expected.Kind == JsonKind.True ? "true" : "false")} but was {(actual.Kind == JsonKind.True ? "true" : "false")}.");
                }
                break;
            case JsonKind.Number:
                if (expected.NumberKey != actual.NumberKey)
                {
                    messages.Add($"Number value mismatch at {path}.\nExpected {expected.Text} but was {actual.Text}.");
                }
                break;
            case JsonKind.String:
                if (!string.Equals(expected.Text, actual.Text, StringComparison.Ordinal))
                {
                    messages.Add($"String value mismatch at {path}.\nExpected {expected.Text} but was {actual.Text}.");
                }
                break;
            case JsonKind.Object:
                WalkObject(expected, actual, path, messages);
                break;
            default:
                WalkArray(expected, actual, path, messages);
                break;
        }
    }

    private void WalkObject(JsonTree expected, JsonTree actual, string path, List<string> messages)
    {
        var missing = expected.Properties.Where(p => actual.Property(p.Key) is null).ToList();
        var additional = _config.AllowAdditionalProperties
            ? new List<KeyValuePair<string, JsonTree>>()
            : actual.Properties.Where(p => expected.Property(p.Key) is null).ToList();

        var sections = new List<string>();

        if (missing.Count > 0)
        {
            sections.Add(Section(missing.Count == 1 ? "Missing property:" : "Missing properties:",
                                 missing.Select(p => $" - '{p.Key}' ({JsonTreeText.TypeName(p.Value)})")));
        }

        if (additional.Count > 0)
        {
            sections.Add(Section(additional.Count == 1 ? "Additional property:" : "Additional properties:",
                                 additional.Select(p => $" + '{p.Key}' ({JsonTreeText.TypeName(p.Value)})")));
        }

        if (sections.Count > 0)
        {
            messages.Add($"Object mismatch at {path}.\n{string.Join("\n", sections)}");
        }

        foreach (var property in expected.Properties)
        {
            var other = actual.Property(property.Key);

            if (other is not null)
            {
                Walk(property.Value, other, JsonTreeText.PropertyPath(path, property.Key), messages);
            }
        }
    }

    private void WalkArray(JsonTree expected, JsonTree actual, string path, List<string> messages)
    {
        List<int> missing;
        List<int> additional;

        if (_config.IgnoreArrayOrder)
        {
            (missing, additional) = MatchUnordered(expected.Items, actual.Items);
        }
        else
        {
            var matches = ArrayMatcher.MatchInOrder(expected.Items, actual.Items, Equivalent);
            var matchedActual = new HashSet<int>(matches.Where(m => m >= 0));

            missing = Enumerable.Range(0, expected.Items.Count).Where(i => matches[i] < 0).ToList();
            additional = Enumerable.Range(0, actual.Items.Count).Where(j => !matchedActual.Contains(j)).ToList();

            // The same positions are left over on both sides: the items at those positions replace each other
            if (missing.Count > 0 && missing.SequenceEqual(additional))
            {
                foreach (var index in missing)
                {
                    Walk(expected.Items[index], actual.Items[index], $"{path}[{index}]", messages);
                }

                return;
            }
        }

        var sections = new List<string>();

        if (missing.Count > 0)
        {
            sections.Add(Section(missing.Count == 1 ? "Missing item:" : "Missing items:",
                                 missing.Select(i => $" - [{i}]: {JsonTreeText.RenderItem(expected.Items[i])}")));
        }

        if (additional.Count > 0)
        {
            sections.Add(Section(additional.Count == 1 ? "Additional item:" : "Additional items:",
                                 additional.Select(i => $" + [{i}]: {JsonTreeText.RenderItem(actual.Items[i])}")));
        }

        if (sections.Count > 0)
        {
            messages.Add($"Array mismatch at {path}.\n{string.Join("\n", sections)}");
        }
    }

    /// <summary>
    /// True when <paramref name="actual"/> satisfies <paramref name="expected"/> under the configuration:
    /// the same comparison as <see cref="Walk"/>, without building any message.
    /// </summary>
    private bool Equivalent(JsonTree expected, JsonTree actual)
    {
        if (TypeClass(expected) != TypeClass(actual))
        {
            return false;
        }

        switch (expected.Kind)
        {
            case JsonKind.True:
            case JsonKind.False:
                return expected.Kind == actual.Kind;
            case JsonKind.Number:
                return expected.NumberKey == actual.NumberKey;
            case JsonKind.String:
                return string.Equals(expected.Text, actual.Text, StringComparison.Ordinal);
            case JsonKind.Object:
                if (!_config.AllowAdditionalProperties && expected.Properties.Count != actual.Properties.Count)
                {
                    return false;
                }

                return expected.Properties.All(p => actual.Property(p.Key) is { } other && Equivalent(p.Value, other));
            case JsonKind.Array:
                if (expected.Items.Count != actual.Items.Count)
                {
                    return false;
                }

                if (_config.IgnoreArrayOrder)
                {
                    var (missing, _) = MatchUnordered(expected.Items, actual.Items);
                    return missing.Count == 0;
                }

                return expected.Items.Zip(actual.Items, Equivalent).All(equivalent => equivalent);
            default:
                return true;
        }
    }

    /// <summary>
    /// Pairs every expected item with a distinct equivalent actual item (maximum bipartite matching, so a
    /// complete pairing is found whenever one exists) and returns the indexes left over on each side.
    /// </summary>
    private (List<int> Missing, List<int> Additional) MatchUnordered(IReadOnlyList<JsonTree> expected, IReadOnlyList<JsonTree> actual)
    {
        var compatible = new bool[expected.Count, actual.Count];

        for (var i = 0; i < expected.Count; i++)
        {
            for (var j = 0; j < actual.Count; j++)
            {
                compatible[i, j] = Equivalent(expected[i], actual[j]);
            }
        }

        var expectedOfActual = new int[actual.Count];
        Array.Fill(expectedOfActual, -1);

        for (var i = 0; i < expected.Count; i++)
        {
            TryAssign(i, new bool[actual.Count]);
        }

        var pairedExpected = new HashSet<int>(expectedOfActual.Where(i => i >= 0));

        return (Enumerable.Range(0, expected.Count).Where(i => !pairedExpected.Contains(i)).ToList(),
                Enumerable.Range(0, actual.Count).Where(j => expectedOfActual[j] < 0).ToList());

        bool TryAssign(int i, bool[] visited)
        {
            for (var j = 0; j < actual.Count; j++)
            {
                if (!compatible[i, j] || visited[j])
                {
                    continue;
                }

                visited[j] = true;

                if (expectedOfActual[j] < 0 || TryAssign(expectedOfActual[j], visited))
                {
                    expectedOfActual[j] = i;
                    return true;
                }
            }

            return false;
        }
    }

    private static string Section(string title, IEnumerable<string> lines) => $"{title}\n{string.Join("\n", lines)}";

    private static int TypeClass(JsonTree value) => value.Kind is JsonKind.True or JsonKind.False ? (int)JsonKind.True : (int)value.Kind;
}
