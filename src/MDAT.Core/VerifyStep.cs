using System.ComponentModel;

namespace MDAT
{
    public class VerifyStep
    {
        [DefaultValue("match")]
        public string? type { get; set; } = "match";

        [DefaultValue("$")]
        public string jsonPath { get; set; } = "$";
        public bool allowAdditionalProperties { get; set; }

        /// <summary>Arrays are equal when they hold the same values, whatever their order.</summary>
        public bool ignoreArrayOrder { get; set; }
        public object? data { get; set; }
    }
}