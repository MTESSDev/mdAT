using YamlDotNet.Serialization;

namespace MDAT.Resolver
{
    public class YamlIncludeNodeDeserializerOptions
    {
        public string DirectoryName { get; set; } = default!;
        public DeserializerBuilder Builder { get; internal set; } = default!;
    }
}