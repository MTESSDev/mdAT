﻿using YamlDotNet.Serialization;

namespace MDAT.Resolver
{
    public class YamlIncludeNodeDeserializerOptions
    {
        public string DirectoryName { get; set; } = default!;
    }
}