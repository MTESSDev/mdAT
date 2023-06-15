namespace MDAT;

public class Expected
{
    public string? name { get; set; }
    public string? generateExpectedData { get; set; }
    //public object? data { get; set; }
    public IEnumerable<VerifyStep>? verify { get; set; }
}
