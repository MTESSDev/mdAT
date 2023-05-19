namespace MDAT;

public class ObjectOrException<T>
{
    public T? Value { get; set; }
    public TestException? Exception { get; set; }
}
