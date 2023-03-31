using System.Reflection;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace MDAT.Resolver
{
    public class MDATYamlNodeDeserializer : INodeDeserializer
    {
        private readonly MethodInfo _testMethod;

        public MDATYamlNodeDeserializer(MethodInfo testMethod)
        {
            _testMethod = testMethod;
        }

        public bool Deserialize(IParser reader, Type expectedType, Func<IParser, Type, object?> nestedObjectDeserializer, out object? value)
        {
            value = null;

            if (expectedType.FullName.StartsWith("System.") && reader.Current is MappingStart)
                throw new InvalidOperationException("Block");
            return false;
        }
    }
}