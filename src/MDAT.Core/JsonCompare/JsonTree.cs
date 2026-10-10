using System.Numerics;
using System.Text.Json;

namespace MDAT.JsonCompare;

internal enum JsonKind { Null, True, False, Number, String, Array, Object }

/// <summary>
/// Minimal immutable JSON tree used by the comparison engine.
/// Numbers keep their raw text (used in messages) next to an exact canonical form (used for equality).
/// </summary>
internal sealed class JsonTree
{
    private Dictionary<string, JsonTree>? _propertiesByName;

    private JsonTree(JsonKind kind) { Kind = kind; }

    public JsonKind Kind { get; }

    /// <summary>Raw text of a number, or the content of a string.</summary>
    public string Text { get; private init; } = "";

    /// <summary>Exact canonical form of a number: 1, 1.0, 1e0 and 10e-1 share the same key, -0 equals 0.</summary>
    public string NumberKey { get; private init; } = "";

    public IReadOnlyList<JsonTree> Items { get; private init; } = Array.Empty<JsonTree>();

    public IReadOnlyList<KeyValuePair<string, JsonTree>> Properties { get; private init; } = Array.Empty<KeyValuePair<string, JsonTree>>();

    public bool IsScalar => Kind != JsonKind.Array && Kind != JsonKind.Object;

    public static JsonTree Parse(string? json, string parameterName)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(json);

            using var document = JsonDocument.Parse(json);
            return From(document.RootElement);
        }
        catch (Exception ex) when (ex is JsonException or ArgumentNullException)
        {
            throw new ArgumentException($"Failed to parse the value passed for {parameterName} as a JSON value. See inner exception for details.", parameterName, ex);
        }
    }

    private static JsonTree From(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Null:
                return new JsonTree(JsonKind.Null);
            case JsonValueKind.True:
                return new JsonTree(JsonKind.True);
            case JsonValueKind.False:
                return new JsonTree(JsonKind.False);
            case JsonValueKind.Number:
                var raw = element.GetRawText();
                return new JsonTree(JsonKind.Number) { Text = raw, NumberKey = CanonicalNumber(raw) };
            case JsonValueKind.String:
                return new JsonTree(JsonKind.String) { Text = element.GetString()! };
            case JsonValueKind.Array:
                return new JsonTree(JsonKind.Array) { Items = element.EnumerateArray().Select(From).ToList() };
            case JsonValueKind.Object:
                var seen = new HashSet<string>(StringComparer.Ordinal);
                var properties = new List<KeyValuePair<string, JsonTree>>();
                foreach (var property in element.EnumerateObject())
                {
                    // Ambiguous: which value would be the right one? Do not silently pick one.
                    if (!seen.Add(property.Name))
                    {
                        throw new JsonException($"Duplicate property '{property.Name}'.");
                    }

                    properties.Add(new(property.Name, From(property.Value)));
                }
                return new JsonTree(JsonKind.Object) { Properties = properties };
            default:
                throw new JsonException($"Unsupported JSON value kind '{element.ValueKind}'.");
        }
    }

    /// <summary>Canonical text of a JSON number (already validated by the JSON reader), exact whatever its size.</summary>
    private static string CanonicalNumber(string raw)
    {
        var text = raw;
        var negative = text[0] == '-';

        if (negative)
        {
            text = text[1..];
        }

        BigInteger exponent = 0;
        var exponentIndex = text.IndexOfAny(new[] { 'e', 'E' });

        if (exponentIndex >= 0)
        {
            exponent = BigInteger.Parse(text[(exponentIndex + 1)..], System.Globalization.CultureInfo.InvariantCulture);
            text = text[..exponentIndex];
        }

        var dotIndex = text.IndexOf('.');
        var digits = text;

        if (dotIndex >= 0)
        {
            digits = text[..dotIndex] + text[(dotIndex + 1)..];
            exponent -= text.Length - dotIndex - 1;
        }

        digits = digits.TrimStart('0');

        if (digits.Length == 0)
        {
            return "0";
        }

        var significant = digits.TrimEnd('0');
        exponent += digits.Length - significant.Length;

        return $"{(negative ? "-" : "")}{significant}E{exponent}";
    }

    public JsonTree? Property(string name)
    {
        if (Properties.Count == 0)
        {
            return null;
        }

        _propertiesByName ??= Properties.ToDictionary(p => p.Key, p => p.Value, StringComparer.Ordinal);

        return _propertiesByName.GetValueOrDefault(name);
    }
}
