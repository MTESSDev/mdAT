using YamlDotNet.Core.Events;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using Microsoft.Extensions.Primitives;

namespace MDAT.Resolver;

internal class StringValuesConverter : IYamlTypeConverter
{
    private static readonly Type _sequenceStartType = typeof(SequenceStart);
    private static readonly Type _sequenceEndType = typeof(SequenceEnd);

    public bool Accepts(Type type) =>
        type == typeof(StringValues) || type == typeof(StringValues?);

    public object? ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
    {
        List<string> strList = new List<string>();

        if (parser.Current is Scalar)
        {
            strList.Add(GetScalarValue(parser));
        }
        else if (parser.Current!.GetType() == _sequenceStartType)
        {
            parser.Consume<SequenceStart>();
            do
            {
                strList.Add(GetScalarValue(parser));
                parser.MoveNext();
            } while (parser.Current.GetType() != _sequenceEndType);
        }
        else
        {
            parser.MoveNext();
            return null;
        }

        parser.MoveNext();

        return new StringValues(strList.ToArray());
    }

    public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer)
    {
        throw new NotImplementedException();
    }

    private static string GetScalarValue(IParser parser)
    {
        Scalar? scalar;

        scalar = parser.Current as Scalar;

        if (scalar == null)
        {
            throw new InvalidDataException("Failed to retrieve scalar value.");
        }

        return scalar.Value;
    }
}