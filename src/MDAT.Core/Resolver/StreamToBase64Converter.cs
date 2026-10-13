using Newtonsoft.Json;

namespace MDAT.Resolver
{
    public class StreamToBase64Converter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(Stream).IsAssignableFrom(objectType);
        }

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            if (value is Stream stream)
            {
                var position = stream.Position;

                using (var memoryStream = new MemoryStream())
                {
                    stream.CopyTo(memoryStream);
                    byte[] byteArray = memoryStream.ToArray();
                    string base64String = Convert.ToBase64String(byteArray);
                    writer.WriteValue(base64String);
                }

                stream.Position = position;
            }
        }

        public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.String)
            {
                string base64String = (string)reader.Value!;
                byte[] byteArray = Convert.FromBase64String(base64String);
                return new MemoryStream(byteArray);
            }

            throw new JsonSerializationException("Invalid token type for Stream conversion");
        }
    }
}