using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace MDAT.Resolver
{
    public class YamlIncludeNodeDeserializer : INodeDeserializer
    {
        private static readonly Regex JsonExtensionRegex = new(@"^\.json$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);
        private static readonly Regex YamlExtensionRegex = new(@"^\.y[a]{0,1}ml$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);

        private readonly YamlIncludeNodeDeserializerOptions _options;
        private DeserializerBuilder GetDeserializer()
        {
            DeserializerBuilder deserializer = new DeserializerBuilder()
                .WithTypeConverter(new ByteArayConverter(), e => e.OnBottom())
                //.WithNodeTypeResolver(resolver)
                .WithAttemptingUnquotedStringTypeDeserialization()
                .IgnoreUnmatchedProperties()
                .WithTagMapping(MdatConstants.IncludeTag, typeof(IncludeRef));

            var includeNodeDeserializerOptions = new YamlIncludeNodeDeserializerOptions
            {
                DirectoryName = _options.DirectoryName
            };

            var includeNodeDeserializer = new YamlIncludeNodeDeserializer(includeNodeDeserializerOptions);

            deserializer.WithNodeDeserializer(includeNodeDeserializer, s => s.OnTop());

            return deserializer;
        } 

        public YamlIncludeNodeDeserializer(YamlIncludeNodeDeserializerOptions options)
        {
            _options = options;
        }

        bool INodeDeserializer.Deserialize(IParser parser, Type expectedType, Func<IParser, Type, object?> nestedObjectDeserializer, out object? value, ObjectDeserializer rootDeserializer)
        {
            if (parser.Accept(out Scalar scalar) && scalar != null)
            {
                string fileName = scalar.Value.Replace('/', Path.DirectorySeparatorChar);
                var extension = Path.GetExtension(fileName);

                if (scalar.Tag == MdatConstants.IncludeTag)
                {
                    var includePath = Path.Combine(_options.DirectoryName, fileName);
                    value = ReadIncludedFile(GetDeserializer(), includePath, expectedType);
                    parser.MoveNext();
                    return true;
                }
            }

            value = null;
            return false;
        }

        private static object? ReadIncludedFile(DeserializerBuilder deserializer, string includePath, Type expectedType)
        {
            var extension = Path.GetExtension(includePath);

            if (expectedType == typeof(byte[]))
            {
                return File.ReadAllBytes(includePath);
            }

            if (YamlExtensionRegex.IsMatch(extension))
            {
                if (expectedType == typeof(string))
                {
                    return File.ReadAllText(includePath).ReplaceLineEndings("\n");
                }

                var objYaml = deserializer.Build().Deserialize(new Parser(File.OpenText(includePath)), expectedType);

                /*var json = JsonConvert.SerializeObject(objYaml);

                    var personCopy =  new JsonSerializerSettings()
                    {
                        ContractResolver = new PrivateResolver(),
                        ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor
                    };
                var jsonobj = JsonConvert.DeserializeObject(json, expectedType, personCopy);*/

                return objYaml;
            }

            if (JsonExtensionRegex.IsMatch(extension))
            {
                if (expectedType == typeof(string))
                {
                    return File.ReadAllText(includePath).ReplaceLineEndings("\n");
                }
                return File.ReadAllText(includePath).ReplaceLineEndings("\n");
            }

            return Convert.ToBase64String(File.ReadAllBytes(includePath));
        }
    }

    [ExcludeFromCodeCoverage]
    public class IncludeRef : Dictionary<object, object>
    {
        public string? FileName { get; set; }
    }
}