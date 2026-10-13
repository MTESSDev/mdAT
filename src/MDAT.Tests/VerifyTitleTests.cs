using System.Text.Json.Nodes;

namespace MDAT.Tests;

/// <summary>The title written before the raw result of a test names the type the way it is written in C#.</summary>
public class VerifyTitleTests
{
    [Test]
    [Arguments(typeof(int), "Int32")]
    [Arguments(typeof(string), "String")]
    [Arguments(typeof(byte[]), "Byte[]")]
    [Arguments(typeof(int[,]), "Int32[,]")]
    [Arguments(typeof(int?), "Int32?")]
    [Arguments(typeof(List<JsonNode>), "List<JsonNode>")]
    [Arguments(typeof(Dictionary<string, byte[]>), "Dictionary<String, Byte[]>")]
    [Arguments(typeof(Dictionary<object, List<int?>>), "Dictionary<Object, List<Int32?>>")]
    [Arguments(typeof(IEnumerable<KeyValuePair<string, string>>), "IEnumerable<KeyValuePair<String, String>>")]
    public async Task Type_title_is_readable(Type type, string expected)
    {
        await Assert.That(Verify.TypeTitle(type)).IsEqualTo(expected);
    }

    [Test]
    public async Task Anonymous_type_title_does_not_show_the_compiler_name()
    {
        await Assert.That(Verify.TypeTitle(new { Name = "x", Count = 1 }.GetType())).IsEqualTo("anonymous object");
    }

    // The three tests below read what Verify.Assert really writes in the output of the test.

    [Test]
    public async Task Output_of_a_generic_result_has_a_readable_title()
    {
        var expected = new Expected { verify = new[] { new VerifyStep { data = "[1]" } } };

        _ = await Verify.Assert(() => Task.FromResult(new List<JsonNode?> { JsonValue.Create(1) }), expected);

        var output = TestContext.Current!.GetStandardOutput();

        await Assert.That(output).Contains("Actual raw for \"List<JsonNode>\":");
        await Assert.That(output).DoesNotContain("`1");
    }

    [Test]
    public async Task Output_of_a_dictionary_result_has_a_readable_title()
    {
        var expected = new Expected { verify = new[] { new VerifyStep { data = """{"a":"AQI="}""" } } };

        _ = await Verify.Assert(() => Task.FromResult(new Dictionary<string, byte[]> { ["a"] = new byte[] { 1, 2 } }), expected);

        var output = TestContext.Current!.GetStandardOutput();

        await Assert.That(output).Contains("Actual raw for \"Dictionary<String, Byte[]>\":");
        await Assert.That(output).DoesNotContain("`2");
    }

    [Test]
    public async Task Name_given_in_the_expected_replaces_the_type_title()
    {
        var expected = new Expected { name = "Résultat du service", verify = new[] { new VerifyStep { data = "[1]" } } };

        _ = await Verify.Assert(() => Task.FromResult(new List<int> { 1 }), expected);

        await Assert.That(TestContext.Current!.GetStandardOutput()).Contains("Actual raw for \"Résultat du service\":");
    }
}
