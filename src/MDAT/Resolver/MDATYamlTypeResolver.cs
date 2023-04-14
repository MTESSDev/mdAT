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
    private bool setNext = false;
    public MDATYamlTypeResolver(MethodInfo testMethod)
    {
        _testMethod = testMethod;
    }

    bool INodeTypeResolver.Resolve(NodeEvent? nodeEvent, ref Type currentType)
    {
        if (nodeEvent is null) return false;

        if (nodeEvent.Start.Column == 1
            && nodeEvent.Start.Line == nodeEvent.End.Line
            && nodeEvent.Start.Index == nodeEvent.End.Index
            && nodeEvent.Start.Column == nodeEvent.End.Column
            && nodeEvent is MappingStart node && node.Style == MappingStyle.Block)
            return false;

        if (nodeEvent.Start.Column == 1
         && (nodeEvent is Scalar scal && scal.IsKey
         || nodeEvent is Scalar scal2 && scal2.IsKey))
        {
            setNext = true;
            return false;
        }

        if (setNext)
        {
            setNext = false;

            var parameters = _testMethod.GetParameters();

            if (parameters.Count() != 0 && _pos > parameters.Count()) throw new Exception($"Method '{_testMethod.Name}' has '{parameters.Count()}' parameters, can't get param number '{_pos}'");

            Type type = parameters[_pos++].ParameterType;
            currentType = type;
            return true;
        }

        return false;
    }

}