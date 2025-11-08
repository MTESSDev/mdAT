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
                JsonDocument doc => doc.RootElement.GetRawText(),
                JsonElement elem => elem.ToString(),
                _ => throw new JsonSerializationException($"Type System.Text.Json non supporté : {value.GetType()}")
            };

            writer.WriteRawValue(json);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, Newtonsoft.Json.JsonSerializer serializer)
        {
            throw new JsonSerializationException("Invalid json document.");
        }
    }
}