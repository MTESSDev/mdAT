using System;
using System.IO;
using System.Text.RegularExpressions;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace MDAT.Resolver
{
    public class YamlIncludeNodeDeserializer : INodeDeserializer
    {
        private static readonly Regex JsonExtensionRegex = new(@"^\.json$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);
        private static readonly Regex RamlExtensionRegex = new(@"^\.y[a]{0,1}ml$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);

        private readonly YamlIncludeNodeDeserializerOptions _options;

        public YamlIncludeNodeDeserializer(YamlIncludeNodeDeserializerOptions options)
        {
            _options = options;
        }

        bool INodeDeserializer.Deserialize(IParser parser, Type expectedType, Func<IParser, Type, object?> nestedObjectDeserializer, out object? value)
        {
            if (parser.Accept(out Scalar scalar) && scalar != null)
            {
                string fileName = scalar.Value.Replace('/', Path.DirectorySeparatorChar);
                var extension = Path.GetExtension(fileName);

                if (scalar.Tag == MdatConstants.IncludeTag || (scalar.Tag != MdatConstants.IncludeTag && (RamlExtensionRegex.IsMatch(extension) || JsonExtensionRegex.IsMatch(extension))))
                {
                    var includePath = Path.Combine(_options.DirectoryName, fileName);
                    value = ReadIncludedFile(_options.Builder, includePath, expectedType);
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

            if(expectedType == typeof(string))
            {
                return File.ReadAllText(includePath);
            }

            if (RamlExtensionRegex.IsMatch(extension))
            {
                return deserializer.Build().Deserialize(new Parser(File.OpenText(includePath)), expectedType);
            }

            if (JsonExtensionRegex.IsMatch(extension))
            {
                return File.ReadAllText(includePath);
            }

            throw new NotSupportedException($"The file extension '{extension}' is not supported in a '{MdatConstants.IncludeTag}' tag.");
        }
    }

    internal static class IncludeNodeDeserializerBuilder
    {

        public static IDeserializer Build(string directoryName)
        {
            var builder = new DeserializerBuilder();

            var includeNodeDeserializerOptions = new YamlIncludeNodeDeserializerOptions
            {
                DirectoryName = directoryName
            };

            var includeNodeDeserializer = new YamlIncludeNodeDeserializer(includeNodeDeserializerOptions);

            return builder
                .WithTagMapping(MdatConstants.IncludeTag, typeof(IncludeRef))
                .WithNodeDeserializer(includeNodeDeserializer, s => s.OnTop())
                .Build();
        }
    }

    public class IncludeRef : Dictionary<object, object>
    {
        public string? FileName { get; set; }
    }
}