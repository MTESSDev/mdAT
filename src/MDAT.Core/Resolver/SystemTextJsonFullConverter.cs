using Newtonsoft.Json;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace MDAT.Resolver
{
    public class SystemTextJsonFullConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(JsonNode).IsAssignableFrom(objectType)
           || typeof(JsonDocument).IsAssignableFrom(objectType)
           || typeof(JsonElement).IsAssignableFrom(objectType);
        }

        public override void WriteJson(JsonWriter writer, object? value, Newtonsoft.Json.JsonSerializer serializer)
        {
            if (value is null)
            {
                writer.WriteNull();
                return;
            }

            string json = value switch
            {
                JsonObject obj => obj.ToJsonString(),
                JsonArray arr => arr.ToJsonString(),
                JsonValue val => val.ToJsonString(),
                JsonNode node => node.ToJsonString(),
                JsonDocument doc => ElementToJson(doc.RootElement),
                JsonElement elem => ElementToJson(elem),
                _ => throw new JsonSerializationException($"Type System.Text.Json non supporté : {value.GetType()}")
            };

            writer.WriteRawValue(json);
        }

        /// <summary>
        /// JSON text of an element. A string holding a JSON object or array is written as that object or array,
        /// as mdAT 1.4 did: the document of a service often carries its payload as JSON text.
        /// Any other string stays a string.
        /// </summary>
        private static string ElementToJson(JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.String && TryGetEmbeddedJson(element.GetString(), out var embedded))
            {
                return embedded;
            }

            return element.GetRawText();
        }

        private static bool TryGetEmbeddedJson(string? text, out string json)
        {
            json = "";

            var trimmed = text?.Trim();

            if (string.IsNullOrEmpty(trimmed) || (trimmed[0] != '{' && trimmed[0] != '['))
            {
                return false;
            }

            try
            {
                using var document = JsonDocument.Parse(trimmed);
                json = document.RootElement.GetRawText();
                return true;
            }
            catch (System.Text.Json.JsonException)
            {
                return false;
            }
        }

        public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, Newtonsoft.Json.JsonSerializer serializer)
        {
            throw new JsonSerializationException("Invalid json document.");
        }
    }
}