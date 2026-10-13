using Newtonsoft.Json;
using System.Text.Json;

namespace MDAT.Tests;

/// <summary>
/// How the System.Text.Json types are written in the actual JSON. A JsonDocument whose root is a string holding
/// a JSON object or array is written as that object or array (mdAT 1.4 behaviour).
/// </summary>
public class SystemTextJsonSerializationTests
{
    public class Row
    {
        public string? Name { get; set; }

        public JsonDocument? Donnees { get; set; }
    }

    private static string Serialize(object value) => JsonConvert.SerializeObject(value, Extensions.JsonSerializeSettings());

    [Test]
    public async Task Document_with_a_string_root_holding_a_json_object_is_written_as_an_object()
    {
        var document = JsonDocument.Parse("\"{\\\"UneCle\\\": \\\"UneValeur\\\", \\\"UneAutreCle\\\": \\\"UneAutreValeur\\\"}\"");

        JsonAssert.Equal("""{"UneCle":"UneValeur","UneAutreCle":"UneAutreValeur"}""", Serialize(document));

        await Assert.That(document.RootElement.ValueKind).IsEqualTo(JsonValueKind.String);
    }

    [Test]
    public async Task Document_with_a_string_root_holding_a_json_array_is_written_as_an_array()
    {
        var document = JsonDocument.Parse("\"[1, 2, {\\\"a\\\": null}]\"");

        JsonAssert.Equal("""[1,2,{"a":null}]""", Serialize(document));

        await Assert.That(document.RootElement.ValueKind).IsEqualTo(JsonValueKind.String);
    }

    [Test]
    [Arguments("\"hello\"", "\"hello\"")]
    [Arguments("\"\"", "\"\"")]
    [Arguments("\"123\"", "\"123\"")]
    [Arguments("\"{not json\"", "\"{not json\"")]
    [Arguments("\"[1, 2\"", "\"[1, 2\"")]
    public async Task Document_with_another_string_root_stays_a_string(string documentJson, string expectedJson)
    {
        JsonAssert.Equal(expectedJson, Serialize(JsonDocument.Parse(documentJson)));

        await Task.CompletedTask;
    }

    [Test]
    [Arguments("""{"a":[1,2]}""")]
    [Arguments("""[1,"x",null]""")]
    [Arguments("12.50")]
    [Arguments("true")]
    [Arguments("null")]
    public async Task Document_with_another_root_is_written_unchanged(string json)
    {
        JsonAssert.Equal(json, Serialize(JsonDocument.Parse(json)));

        await Task.CompletedTask;
    }

    [Test]
    public async Task Element_follows_the_same_rule_as_document()
    {
        using var embedded = JsonDocument.Parse("\"{\\\"a\\\": 1}\"");
        using var plain = JsonDocument.Parse("\"hello\"");

        JsonAssert.Equal("""{"a":1}""", Serialize(embedded.RootElement));
        // used to be written as the invalid JSON text: hello
        JsonAssert.Equal("\"hello\"", Serialize(plain.RootElement));

        await Task.CompletedTask;
    }

    [Test]
    public async Task Expected_object_matches_a_document_holding_the_object_as_text()
    {
        var rows = new[]
        {
            new Row { Name = "n", Donnees = JsonDocument.Parse("\"{\\\"UneCle\\\": \\\"UneValeur\\\"}\"") },
            new Row { Name = "m", Donnees = JsonDocument.Parse("""{"UneCle":"UneValeur"}""") },
        };

        // the same expected object, whatever the form of the document
        var expected = new Expected
        {
            verify = new[]
            {
                new VerifyStep { jsonPath = "$[0].Donnees", data = new Dictionary<object, object> { ["UneCle"] = "UneValeur" } },
                new VerifyStep { jsonPath = "$[1].Donnees", data = new Dictionary<object, object> { ["UneCle"] = "UneValeur" } },
            },
        };

        _ = await Verify.Assert(() => Task.FromResult(rows), expected);
    }
}
