namespace MDAT;

public sealed class JsonDiffConfig
{
    public bool AllowAdditionalProperties { get; }
    public bool StrictArrayOrdering { get; }
    public bool TreatNullAndMissingAsEqual { get; }

    public JsonDiffConfig(
        bool allowAdditionalProperties = false,
        bool strictArrayOrdering = true,
        bool treatNullAndMissingAsEqual = false)
    {
        AllowAdditionalProperties = allowAdditionalProperties;
        StrictArrayOrdering = strictArrayOrdering;
        TreatNullAndMissingAsEqual = treatNullAndMissingAsEqual;
    }
}