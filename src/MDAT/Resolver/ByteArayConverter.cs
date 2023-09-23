using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Quibble;
using System.Text.Json.Serialization;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace MDAT.Resolver
{
    internal class ByteArayConverter : IYamlTypeConverter
    {
        public bool Accepts(Type type)
        {
            return type == typeof(byte[]);
        }

        public object? ReadYaml(IParser parser, Type type)
        {
            var value = parser.Consume<Scalar>().Value;

            if (string.IsNullOrWhiteSpace(value) || value.Equals("null", StringComparison.InvariantCultureIgnoreCase)) return null;

            if (type == typeof(byte[]))
            {
                return Convert.FromBase64String(value);
            }

            return Type.GetType(value, throwOnError: true)!;
        }

        public void WriteYaml(IEmitter emitter, object? value, Type type)
        {
            var val = JsonConvert.SerializeObject(value!);
            var ttt = JToken.Parse(val);
            emitter.Emit(new Scalar(AnchorName.Empty, TagName.Empty, ttt.ToString(), ScalarStyle.Any, true, false));
        }
    }
}