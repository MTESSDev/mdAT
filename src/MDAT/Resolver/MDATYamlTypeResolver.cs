using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace MDAT.Resolver;

public sealed class MDATYamlTypeResolver : INodeTypeResolver
{
    public int Position { get => _pos; }

    private readonly MethodInfo _testMethod;
    private int _pos = 0;
    public MDATYamlTypeResolver(MethodInfo testMethod)
    {
        _testMethod = testMethod;
    }

    bool INodeTypeResolver.Resolve(NodeEvent? nodeEvent, ref Type currentType)
    {
        if (currentType == typeof(object))
        {
            if (nodeEvent is MappingStart || nodeEvent is Scalar || nodeEvent is SequenceStart)
            {
                var parameters = _testMethod.GetParameters();

                if (parameters.Count() != 0 && _pos > parameters.Count()) throw new Exception($"Method '{_testMethod.Name}' has '{parameters.Count()}' parameters, can't get param number '{_pos}'");

                Type type = parameters[_pos++].ParameterType;
                currentType = type;
                return true;
            }
        }

        return false;
    }

}