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
    private int _startPos = 1;
    public MDATYamlTypeResolver(MethodInfo testMethod)
    {
        _testMethod = testMethod;
    }

    bool INodeTypeResolver.Resolve(NodeEvent? nodeEvent, ref Type currentType)
    {
        if (nodeEvent!.Start.Column == 1)
            _startPos = 1;

        if (nodeEvent!.Start.Column != 1 && _startPos == 1)
            _startPos = nodeEvent.Start.Column;

        if (_startPos != 1 && nodeEvent.Start.Column == _startPos && currentType == typeof(object))
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