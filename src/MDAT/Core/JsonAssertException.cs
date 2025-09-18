namespace MDAT;

public class JsonAssertException : AssertFailedException
{
    public JsonAssertException(string message) : base(message) { }
}