namespace MDAT
{
    public class VerifyStep
    {
        public string? type { get; set; }
        public string jsonPath { get; set; } = "$";
        public bool allowAdditionalProperties { get; set; }
        public object? data { get; set; }
    }
}