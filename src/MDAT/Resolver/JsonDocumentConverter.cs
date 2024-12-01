using Newtonsoft.Json;
using System.Text.Json;

namespace MDAT.Resolver
{
    public class JsonDocumentConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(JsonDocument).IsAssignableFrom(objectType);
        }

        public override void WriteJson(JsonWriter writer, object? value, Newtonsoft.Json.JsonSerializer serializer)
        {
            if (value is JsonDocument netJson)
            {
                writer.WriteValue(netJson.RootElement.ToString());
            }
        }

        public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, Newtonsoft.Json.JsonSerializer serializer)
        {
            throw new JsonSerializationException("Invalid json document.");
        }
    }
}