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
                using var reader = new JsonTextReader(new StringReader(netJson.RootElement.ToString()));
                writer.WriteToken(reader); // Écrit correctement le contenu JSON dans le flux
            }
        }

        public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, Newtonsoft.Json.JsonSerializer serializer)
        {
            throw new JsonSerializationException("Invalid json document.");
        }
    }
}