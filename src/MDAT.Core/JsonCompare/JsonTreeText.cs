using System.Text;

namespace MDAT.JsonCompare;

/// <summary>Human readable descriptions of JSON values used in assertion messages.</summary>
internal static class JsonTreeText
{
    public static string Describe(JsonTree value) => value.Kind switch
    {
        JsonKind.Null => "null",
        JsonKind.True => "the boolean true",
        JsonKind.False => "the boolean false",
        JsonKind.Number => $"the number {value.Text}",
        JsonKind.String => $"the string {value.Text}",
        JsonKind.Array => value.Items.Count switch
        {
            0 => "an empty array",
            1 => "an array with 1 item",
            var n => $"an array with {n} items",
        },
        _ => "an object",
    };

    /// <summary>Type name shown next to a property in "Missing/Additional property" sections.</summary>
    public static string TypeName(JsonTree value) => value.Kind switch
    {
        JsonKind.Null => "null",
        JsonKind.True or JsonKind.False => "bool",
        JsonKind.Number => "number",
        JsonKind.String => "string",
        JsonKind.Array => "array",
        _ => "object",
    };

    /// <summary>Value shown for an array item: scalars are described, containers are expanded one level.</summary>
    public static string RenderItem(JsonTree value)
    {
        if (value.IsScalar)
        {
            return Describe(value);
        }

        var text = new StringBuilder();

        if (value.Kind == JsonKind.Array)
        {
            text.Append('[');
            foreach (var item in value.Items)
            {
                text.Append("\n  ").Append(Inline(item));
            }
            text.Append("\n]");
        }
        else
        {
            text.Append('{');
            foreach (var property in value.Properties)
            {
                text.Append("\n  '").Append(property.Key).Append("': ").Append(Inline(property.Value));
            }
            text.Append("\n}");
        }

        return text.ToString();
    }

    private static string Inline(JsonTree value) => value.Kind switch
    {
        JsonKind.Null => "null",
        JsonKind.True => "true",
        JsonKind.False => "false",
        JsonKind.Number => value.Text,
        JsonKind.String => "'" + value.Text + "'",
        JsonKind.Array => value.Items.Count switch
        {
            0 => "[]",
            1 => "[ 1 item ]",
            var n => $"[ {n} items ]",
        },
        _ => value.Properties.Count switch
        {
            0 => "{}",
            1 => "{ 1 property }",
            var n => $"{{ {n} properties }}",
        },
    };

    public static string PropertyPath(string parent, string property)
    {
        return IsPlainName(property) ? $"{parent}.{property}" : $"{parent}['{property.Replace("\\", "\\\\").Replace("'", "\\'")}']";
    }

    private static bool IsPlainName(string name)
    {
        if (name.Length == 0)
        {
            return false;
        }

        foreach (var c in name)
        {
            if (!(c is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9'))
            {
                return false;
            }
        }

        return true;
    }
}
