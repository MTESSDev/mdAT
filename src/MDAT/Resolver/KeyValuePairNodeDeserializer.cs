using YamlDotNet.Core.Events;
using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace MDAT.Resolver
{
    internal class KeyValuePairNodeDeserializer : INodeDeserializer
    {
        public bool Deserialize(IParser parser, Type expectedType, Func<IParser, Type, object?> nestedObjectDeserializer, out object? value)
        {
            if (expectedType.IsGenericType && expectedType.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
            {
                parser.Consume<MappingStart>();

                var pairArgs = expectedType.GetGenericArguments();

                object? key = null;
                object? val = null;

                if (parser.Accept<Scalar>(out var skey)
                    && skey.Value.ToLower().Equals("key")
                    && parser.Consume<Scalar>() is { })
                    key = nestedObjectDeserializer(parser, pairArgs[0]);

                if (parser.Accept<Scalar>(out var svalue)
                    && svalue.Value.ToLower().Equals("value")
                    && parser.Consume<Scalar>() is { })
                    val = nestedObjectDeserializer(parser, pairArgs[1]);

                value = Activator.CreateInstance(expectedType, key, val);

                parser.Consume<MappingEnd>();
                return true;
            }

            value = null;
            return false;
        }
    }
}