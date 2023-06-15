using System.ComponentModel;

namespace MDAT
{
    public class VerifyStep
    {
        [DefaultValue("match")]
        public string? type { get; set; }

        [DefaultValue("$")]
        public string jsonPath { get; set; } = "$";
        public bool allowAdditionalProperties { get; set; }
        public object? data { get; set; }
    }
}