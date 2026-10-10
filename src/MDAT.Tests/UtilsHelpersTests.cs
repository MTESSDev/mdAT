using Newtonsoft.Json;

namespace MDAT.Tests;

/// <summary>The small helpers of <see cref="Extensions"/> that the markdown files rely on.</summary>
public class UtilsHelpersTests
{
    [Test]
    [Arguments("Md1", "md1")]
    [Arguments("ab", "ab")]
    [Arguments("A", "A")]
    [Arguments("AbCd", "ab-cd")]
    [Arguments("Test_JsonNode", "test-json-node")]
    [Arguments("Some_method_name", "some-method-name")]
    [Arguments("XMLHttp", "x-m-l-http")]
    public async Task A_method_name_becomes_the_name_of_its_markdown_file(string methodName, string expected)
    {
        await Assert.That(methodName.ToKebabCase()).IsEqualTo(expected);
    }

    [Test]
    public async Task A_null_method_name_is_refused()
    {
        string? name = null;
        ArgumentNullException? error = null;

        try
        {
            _ = name!.ToKebabCase();
        }
        catch (ArgumentNullException ex)
        {
            error = ex;
        }

        await Assert.That(error).IsNotNull();
    }

    [Test]
    [Arguments("{}", true)]
    [Arguments("[1,2]", true)]
    [Arguments("12.5", true)]
    [Arguments("\"text\"", true)]
    [Arguments("not json", false)]
    [Arguments("{\"a\":", false)]
    public async Task Text_is_recognized_as_json_or_not(string text, bool isJson)
    {
        await Assert.That(text.ValidateJSON()).IsEqualTo(isJson);
    }

    [Test]
    public async Task Both_kinds_of_separator_become_the_one_of_the_platform()
    {
        var separator = Path.DirectorySeparatorChar;

        await Assert.That("Tests\\sub/file.md".ReplacePlatformCompatiblePath()).IsEqualTo($"Tests{separator}sub{separator}file.md");
        await Assert.That("file.md".ReplacePlatformCompatiblePath()).IsEqualTo("file.md");
    }

    [Test]
    public async Task Nested_key_value_pairs_become_nested_dictionaries()
    {
        // what YamlDotNet gives for a mapping inside a mapping: arrays of key/value pairs
        var parsed = new Dictionary<object, object>
        {
            ["a"] = 1,
            ["b"] = new object[]
            {
                new KeyValuePair<object, object>("x", 2),
                new KeyValuePair<object, object>("y", new object[] { new KeyValuePair<object, object>("z", 3) }),
            },
        };

        var fixedDictionary = parsed.FixDict();

        await Assert.That(JsonConvert.SerializeObject(fixedDictionary)).IsEqualTo("""{"a":1,"b":{"x":2,"y":{"z":3}}}""");
    }
}
