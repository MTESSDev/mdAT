namespace MDAT;

/// <summary>
/// Thrown when a test case file cannot be turned into arguments for the test method (invalid YAML, wrong parameter, ...).
/// </summary>
public class MdatTestDefinitionException : Exception
{
    public MdatTestDefinitionException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
