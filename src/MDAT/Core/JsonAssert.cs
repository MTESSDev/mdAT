using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MDAT;

public static class JsonAssert
{
    /// <summary>
    /// Compare deux JSON. Si AllowAdditionalProperties=true, on ignore les propriétés
    /// supplémentaires présentes dans l’"actual" (sous-ensemble requis).
    /// </summary>
    public static void EqualOverrideDefault(string? expectedJson, string? actualJson, JsonDiffConfig? config = null)
    {
        config ??= new JsonDiffConfig();

        JToken expected = ParseOrNull(expectedJson, "Expected");
        JToken actual = ParseOrNull(actualJson, "Actual");

        // Si on tolère des propriétés en plus dans "actual", on « retaille » actual
        if (config.AllowAdditionalProperties && expected.Type == JTokenType.Object && actual.Type == JTokenType.Object)
        {
            actual = StripAdditionalProperties((JObject)actual, (JObject)expected);
        }

        if (!DeepEqual(expected, actual, config, out var path, out var expAt, out var actAt))
        {
            var prettyExpected = expected.ToString(Formatting.Indented);
            var prettyActual = actual.ToString(Formatting.Indented);
            throw new JsonAssertException(
                $"JSON mismatch at path {path}.\nExpected: {expAt}\nActual:   {actAt}\n\nExpected (normalized):\n{prettyExpected}\n\nActual (normalized):\n{prettyActual}"
            );
        }
    }

    private static JToken ParseOrNull(string? json, string label)
    {
        try
        {
            return json is null ? JValue.CreateNull() : JToken.Parse(json);
        }
        catch (Exception ex)
        {
            throw new JsonAssertException($"{label} is not valid JSON: {ex.Message}\n{json}");
        }
    }

    private static JObject StripAdditionalProperties(JObject actual, JObject expected)
    {
        var result = new JObject();
        foreach (var ep in expected.Properties())
        {
            if (!actual.TryGetValue(ep.Name, out var av))
            {
                // Laisse "missing" comme null pour que la comparaison aval décide
                result[ep.Name] = JValue.CreateNull();
                continue;
            }

            if (ep.Value is JObject eo && av is JObject ao)
                result[ep.Name] = StripAdditionalProperties(ao, eo);
            else
                result[ep.Name] = av.DeepClone();
        }
        return result;
    }

    private static bool DeepEqual(JToken expected, JToken actual, JsonDiffConfig cfg,
                                  out string path, out string expectedStr, out string actualStr)
        => DeepEqualInternal(expected, actual, cfg, "$", out path, out expectedStr, out actualStr);

    private static bool DeepEqualInternal(JToken expected, JToken actual, JsonDiffConfig cfg,
                                          string cur, out string path, out string expStr, out string actStr)
    {
        // Valeurs scalaires
        if (expected is JValue ev && actual is JValue av)
        {
            if (Equals(ev.Value, av.Value))
            {
                path = cur; expStr = ev.ToString(Formatting.None); actStr = av.ToString(Formatting.None);
                return true;
            }
            path = cur; expStr = ev.ToString(Formatting.None); actStr = av.ToString(Formatting.None);
            return false;
        }

        // Objets
        if (expected is JObject eo && actual is JObject ao)
        {
            // chaque prop attendue doit exister et matcher
            foreach (var ep in eo.Properties())
            {
                var childPath = $"{cur}.{ep.Name}";
                if (!ao.TryGetValue(ep.Name, out var avp))
                {
                    if (cfg.TreatNullAndMissingAsEqual && ep.Value.Type == JTokenType.Null)
                        continue;

                    path = childPath; expStr = ep.Value.ToString(Formatting.None); actStr = "<missing>";
                    return false;
                }

                if (!DeepEqualInternal(ep.Value, avp!, cfg, childPath, out path, out expStr, out actStr))
                    return false;
            }

            // Si on n’autorise pas d’extra props : on vérifie l’inverse
            if (!cfg.AllowAdditionalProperties)
            {
                foreach (var ap in ao.Properties())
                {
                    if (eo.Property(ap.Name) is null)
                    {
                        path = $"{cur}.{ap.Name}";
                        expStr = "<no such property>";
                        actStr = ap.Value.ToString(Formatting.None);
                        return false;
                    }
                }
            }

            path = cur; expStr = eo.ToString(Formatting.None); actStr = ao.ToString(Formatting.None);
            return true;
        }

        // Tableaux
        if (expected is JArray ea && actual is JArray aa)
        {
            if (cfg.StrictArrayOrdering)
            {
                if (ea.Count != aa.Count)
                {
                    path = cur + ".length"; expStr = ea.Count.ToString(); actStr = aa.Count.ToString(); return false;
                }
                for (int i = 0; i < ea.Count; i++)
                {
                    if (!DeepEqualInternal(ea[i], aa[i], cfg, $"{cur}[{i}]", out path, out expStr, out actStr))
                        return false;
                }
                path = cur; expStr = ea.ToString(Formatting.None); actStr = aa.ToString(Formatting.None);
                return true;
            }
            else
            {
                // égalité “multiset” simple
                var used = new bool[aa.Count];
                for (int i = 0; i < ea.Count; i++)
                {
                    var ok = false;
                    for (int j = 0; j < aa.Count; j++)
                    {
                        if (used[j]) continue;
                        if (DeepEqualInternal(ea[i], aa[j], cfg, $"{cur}[{i}~{j}]", out _, out _, out _))
                        { used[j] = true; ok = true; break; }
                    }
                    if (!ok)
                    {
                        path = $"{cur}[{i}]"; expStr = ea[i].ToString(Formatting.None); actStr = "<no match>";
                        return false;
                    }
                }
                if (ea.Count != aa.Count)
                {
                    path = cur + ".length"; expStr = ea.Count.ToString(); actStr = aa.Count.ToString(); return false;
                }
                path = cur; expStr = ea.ToString(Formatting.None); actStr = aa.ToString(Formatting.None);
                return true;
            }
        }

        // Types différents
        path = cur; expStr = expected.Type.ToString(); actStr = actual.Type.ToString();
        return false;
    }
}
