using System.Text.Json;
using System.Text.Json.Nodes;

namespace MDAT.Tests;

[TestClass]
public class NewtonsoftConvertersTests
{
    /// <summary>
    /// Test JsonDocument Load
    /// This test whole document integrity
    /// </summary>
    [TestMethod]
    [MarkdownTest("~\\Tests\\md-json-document-test.md")]
    public async Task Md_JsonDocument_test(JsonDocument jsonDocument, Test1 test1, Expected expected, Expected expected2)
    {
        object value = await Verify.Assert(() =>
                                      Task.FromResult(jsonDocument), expected);

        object value2 = await Verify.Assert(() =>
                                   Task.FromResult(test1), expected2);

    }

    /// <summary>
    /// Test SystemTextJsonFullConverter - JsonNode
    /// </summary>
    [TestMethod]
    [DoNotParallelize]
    [MarkdownTest("~\\Tests\\{method}.md")]
    public async Task Test_JsonNode(List<JsonNode> nodes, Expected expected)
    {
        _ = await Verify.Assert(() =>
                                    Task.FromResult(nodes), expected);
    }

    /// <summary>
    /// Test SystemTextJsonFullConverter - JsonValue
    /// </summary>
    [TestMethod]
    [DoNotParallelize]
    [MarkdownTest("~\\Tests\\{method}.md")]
    public async Task Test_JsonValue(List<JsonValue> values, Expected expected)
    {
        _ = await Verify.Assert(() =>
                                    Task.FromResult(values), expected);
    }

    /// <summary>
    /// Test SystemTextJsonFullConverter - JsonArray
    /// </summary>
    [TestMethod]
    [DoNotParallelize]
    [MarkdownTest("~\\Tests\\{method}.md")]
    public async Task Test_JsonArray(JsonArray array, Expected expected)
    {
        _ = await Verify.Assert(() =>
                                    Task.FromResult(array), expected);
    }

    /// <summary>
    /// Test SystemTextJsonFullConverter - JsonElement
    /// </summary>
    [TestMethod]
    [DoNotParallelize]
    [MarkdownTest("~\\Tests\\{method}.md")]
    public async Task Test_JsonElement(JsonDocument doc, Expected expected)
    {
        JsonElement elem = doc.RootElement; // un objet complet

        _ = await Verify.Assert(() =>
                                    Task.FromResult(elem), expected);
    }


    public class Test1
    {
        public JsonDocument JsonDocument { get; set; }
    }
}
