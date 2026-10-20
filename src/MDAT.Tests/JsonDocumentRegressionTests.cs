using System.Text.Json;

namespace MDAT.Tests;

/// <summary>
/// Regression found while migrating a real project from mdAT 1.4.1: a call recorded by a mock carries a
/// <see cref="JsonDocument"/> whose root is very often a string holding JSON text. mdAT 1.4 wrote it as an object,
/// so the test cases expect a YAML object; mdAT 1.5.0 to 2.0.0-beta.1 wrote it as a string, which forced the expected
/// value to be a JSON string (about 28 test cases of the migrated project failed).
/// </summary>
public class JsonDocumentRegressionTests
{
    public class AppelDocument
    {
        public string? Gabarit { get; set; }

        public JsonDocument? Donnees { get; set; }

        public Dictionary<string, string>? Options { get; set; }

        public Dictionary<string, string>? ProprietesDocument { get; set; }
    }

    /// <summary>
    /// The document data is given as JSON text. With <paramref name="racineEstUneChaine"/> the text becomes the
    /// string root of the document (the usual form in the real project), otherwise it is the root itself.
    /// </summary>
    [Test]
    [MarkdownDataSource("~/Tests/{method}.md")]
    public async Task Json_document_with_a_json_string_root(string donnees, bool racineEstUneChaine, Expected expected)
    {
        var document = racineEstUneChaine
            ? JsonDocument.Parse(JsonSerializer.Serialize(donnees))
            : JsonDocument.Parse(donnees);

        var appels = new[]
        {
            new AppelDocument
            {
                Gabarit = "UnGabarit",
                Donnees = document,
                Options = new Dictionary<string, string> { ["Format"] = "pdf" },
                ProprietesDocument = new Dictionary<string, string> { ["Titre"] = "Un titre" },
            },
        };

        _ = await Verify.Assert(() => Task.FromResult(appels), expected);
    }
}
