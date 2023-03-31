using System.Reflection;
using System.Text.RegularExpressions;
using YamlDotNet.Serialization;
using MDAT.Resolver;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Globalization;
using Newtonsoft.Json;

namespace MDAT
{
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

        /* /// <summary>
         /// Load data from a JSON file as the data source for a theory
         /// </summary>
         /// <param name="filePath">The absolute or relative path to the JSON file to load</param>
         public YamlFileDataAttribute(string filePath)
             : this(filePath, null) { }

         /// <summary>
         /// Load data from a JSON file as the data source for a theory
         /// </summary>
         /// <param name="filePath">The absolute or relative path to the JSON file to load</param>
         /// <param name="propertyName">The name of the property on the JSON file that contains the data for the test</param>
         public YamlFileDataAttribute(string filePath, string? propertyName)
         {
             _filePath = filePath;

             if (propertyName != null)
             {
                 _propertyName = propertyName;
             }
         }*/

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
            /*   if (parameters.Length != 2 ||
                   parameters[0].ParameterType != typeof(MethodInfo) ||
                   parameters[1].ParameterType != typeof(object[]) ||
                   methodInfo.ReturnType != typeof(string) ||
                   !methodInfo.IsStatic ||
                   !methodInfo.IsPublic)
               {
                   throw new ArgumentNullException(
                       string.Format(
                           "{0}{1}",
                           "naME",
                           typeof(string).Name,
                           string.Join(", ", typeof(MethodInfo).Name, typeof(object[]).Name)));
               }*/

            //return methodInfo.Invoke(null, new object?[] { methodInfo, data }) as string;

            /* if (data != null)
             {
                 // We want to force call to `data.AsEnumerable()` to ensure that objects are casted to strings (using ToString())
                 // so that null do appear as "null". If you remove the call, and do string.Join(",", new object[] { null, "a" }),
                 // you will get empty string while with the call you will get "null,a".
                 return string.Format(CultureInfo.CurrentCulture, "{0}{1}", methodInfo.Name,
                     string.Join(",", data.AsEnumerable()));
             }

             return null;*/

            if (data != null)
            {
                var name = displayNames[data.GetHashCode()];
                return $"{methodInfo.Name}@{_filePath}_#{JsonConvert.SerializeObject(data)}";
            }

            return null;
        }
    }
}