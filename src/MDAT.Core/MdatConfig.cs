using YamlDotNet.Serialization;

namespace MDAT;

public static class MdatConfig
{
    public static List<ConfigTypeConverter> ListeTypeConverter { get; set; } = new List<ConfigTypeConverter>();

    public static void AddYamlTypeConverter(IYamlTypeConverter typeConverter, Action<IRegistrationLocationSelectionSyntax<IYamlTypeConverter>>? where = null)
    {
        ListeTypeConverter.Add(new ConfigTypeConverter() { TypeConverter = typeConverter, Where = where });
    }
}

public class ConfigTypeConverter
{
    public IYamlTypeConverter? TypeConverter { get; set; }
    public Action<IRegistrationLocationSelectionSyntax<IYamlTypeConverter>>? Where { get; set; }
}