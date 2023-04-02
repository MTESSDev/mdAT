using System.Reflection;
using System.Text.RegularExpressions;
using YamlDotNet.Serialization;
using MDAT.Resolver;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Globalization;
using Newtonsoft.Json;
using System.Diagnostics.CodeAnalysis;

namespace MDAT
{
    [ExcludeFromCodeCoverage]
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class YamlFileDataAttribute : Attribute, ITestDataSource
    {
        private readonly string _filePath;
        private readonly string _propertyName;

        private Dictionary<int, string> displayNames = new Dictionary<int, string>();

        public YamlFileDataAttribute(string filePath)
        {
            _filePath = filePath;
        }
         
        /// <inheritDoc />
        public IEnumerable<object[]> GetData(MethodInfo testMethod)
        {
            if (testMethod == null) { throw new ArgumentNullException(nameof(testMethod)); }

            var path = _filePath.Replace("~\\", "..\\..\\..\\");

            // Get the absolute path to the JSON file
            path = Path.IsPathRooted(path)
                    ? path
                    : Path.GetRelativePath(Directory.GetCurrentDirectory(), path);

            if (!File.Exists(path))
            {
                throw new Exception($"Could not find file at path: {path}");
            }

            // Load the file
            var fileData = File.ReadAllText(path);

            if (string.IsNullOrEmpty(_propertyName))
            {
                IEnumerable<object[]> retour;

                var docs = Regex.Split(fileData, @"^---", RegexOptions.Multiline);

                List<object[]> to = new List<object[]>();


                foreach (var doc in docs)
                {
                    if (string.IsNullOrWhiteSpace(doc)) continue;


                    IDeserializer deserializer = NewDeserilizer(testMethod);

                    var ggs = deserializer.Deserialize<Dictionary<string, object>>("---\r\n" + doc);

                    if (ggs is null) continue;

                    var arr = ggs.Values.ToArray();

                    displayNames.Add(arr.GetHashCode(), "Nom " + arr[0]);

                    to.Add(arr);
                }


                return to;

            }

            return null;
        }

        private static IDeserializer NewDeserilizer(MethodInfo testMethod)
        {
            IDeserializer deserializer = new DeserializerBuilder()

              .WithNodeTypeResolver(new MDATYamlTypeResolver(testMethod))
              .IgnoreUnmatchedProperties()
              .Build();
            return deserializer;
        }

        public string? GetDisplayName(MethodInfo methodInfo, object?[]? data)
        {
            var parameters = methodInfo.GetParameters();

            if (data != null)
            {
                var name = displayNames[data.GetHashCode()];
                return $"{methodInfo.Name}@{_filePath}_#{JsonConvert.SerializeObject(data)}";
            }

            return null;
        }
    }
}