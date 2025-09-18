using System.Reflection;

namespace MDAT;

public interface ITestDataProvider
{
    IEnumerable<object?[]> GetData(MethodInfo testMethod);
    string? GetDisplayName(MethodInfo methodInfo, object?[]? data);
}