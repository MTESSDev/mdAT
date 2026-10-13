namespace MDAT.Tests;

/// <summary>Tests of the JSON comparison engine (<see cref="JsonAssert"/>).</summary>
public class JsonAssertTests
{
    private static JsonAssertException? Diff(string expected, string actual, bool allowAdditionalProperties = false, bool ignoreArrayOrder = false)
    {
        try
        {
            JsonAssert.EqualOverrideDefault(expected, actual, new JsonDiffConfig(allowAdditionalProperties, ignoreArrayOrder));
            return null;
        }
        catch (JsonAssertException ex)
        {
            return ex;
        }
    }

    [Test]
    public async Task Equal_documents_do_not_throw()
    {
        await Assert.That(Diff("""{"a":[1,2,{"b":null}],"c":"x"}""", """{ "c": "x", "a": [1, 2, { "b": null }] }""")).IsNull();
    }

    [Test]
    [Arguments("1", "1.0")]
    [Arguments("1e2", "100")]
    [Arguments("100", "1E2")]
    [Arguments("-0", "0")]
    [Arguments("0.10", "0.1")]
    public async Task Numbers_are_equal_whatever_their_spelling(string expected, string actual)
    {
        await Assert.That(Diff(expected, actual)).IsNull();
    }

    [Test]
    public async Task Large_numbers_are_compared_exactly()
    {
        var diff = Diff("12345678901234567890", "12345678901234567891");

        await Assert.That(diff).IsNotNull();
        await Assert.That(diff!.UserMessage).IsEqualTo("Number value mismatch at $.\nExpected 12345678901234567890 but was 12345678901234567891.");
    }

    [Test]
    public async Task Messages_describe_the_difference_and_where_it_is()
    {
        var diff = Diff("""{"a":{"b":[1,2,3]}}""", """{"a":{"b":[1,9,3]}}""");

        await Assert.That(diff!.UserMessage).IsEqualTo("Number value mismatch at $.a.b[1].\nExpected 2 but was 9.");
    }

    [Test]
    public async Task Missing_and_additional_properties_are_listed()
    {
        var diff = Diff("""{"a":1,"b":2}""", """{"b":2,"c":3}""");

        await Assert.That(diff!.UserMessage).IsEqualTo("Object mismatch at $.\nMissing property:\n - 'a' (number)\nAdditional property:\n + 'c' (number)");
    }

    [Test]
    public async Task Additional_properties_can_be_allowed()
    {
        await Assert.That(Diff("""{"a":1}""", """{"a":1,"b":[1]}""", allowAdditionalProperties: true)).IsNull();
        await Assert.That(Diff("""{"a":1,"b":2}""", """{"a":1}""", allowAdditionalProperties: true)).IsNotNull();
    }

    [Test]
    public async Task Property_names_with_quotes_are_escaped_in_paths()
    {
        var diff = Diff("""{"it's":1}""", """{"it's":2}""");

        await Assert.That(diff!.UserMessage).Contains("$['it\\'s']");
    }

    [Test]
    public async Task Duplicate_properties_are_rejected_instead_of_silently_ignored()
    {
        ArgumentException? error = null;

        try
        {
            Diff("""{"a":1,"a":2}""", """{"a":1}""");
        }
        catch (ArgumentException ex)
        {
            error = ex;
        }

        await Assert.That(error).IsNotNull();
        await Assert.That(error!.InnerException!.Message).Contains("Duplicate property 'a'");
    }

    [Test]
    public async Task Invalid_json_is_reported_with_the_name_of_the_faulty_side()
    {
        ArgumentException? error = null;

        try
        {
            JsonAssert.Equal("[1,", "[1]");
        }
        catch (ArgumentException ex)
        {
            error = ex;
        }

        await Assert.That(error!.ParamName).IsEqualTo("rightJsonString");
    }

    [Test]
    public async Task Every_repeated_item_of_an_array_is_reported()
    {
        // Quibble drops the extra "null" from its message and may even report an empty "Array mismatch"
        var diff = Diff("[true,null,3]", "[true,null,null,3]");

        await Assert.That(diff!.UserMessage).IsEqualTo("Array mismatch at $.\nAdditional item:\n + [2]: null");
    }

    [Test]
    public async Task Array_items_are_paired_by_value_not_by_spelling()
    {
        // 1 and 1.0 are the same value: only the extra item is a difference
        var diff = Diff("[1,2]", "[1.0,2,3]");

        await Assert.That(diff!.UserMessage).IsEqualTo("Array mismatch at $.\nAdditional item:\n + [2]: the number 3");
    }

    [Test]
    public async Task Array_items_that_replace_each_other_are_compared_in_depth()
    {
        var diff = Diff("""[{"id":1,"v":"a"},{"id":2,"v":"b"}]""", """[{"id":1,"v":"a"},{"id":2,"v":"c"}]""");

        await Assert.That(diff!.UserMessage).IsEqualTo("String value mismatch at $[1].v.\nExpected b but was c.");
    }

    [Test]
    public async Task Array_order_matters_by_default()
    {
        await Assert.That(Diff("[1,2,3]", "[3,2,1]")).IsNotNull();
    }

    [Test]
    public async Task Array_order_can_be_ignored()
    {
        await Assert.That(Diff("[1,2,3]", "[3,1,2]", ignoreArrayOrder: true)).IsNull();
        await Assert.That(Diff("""{"a":["x",{"b":[2,1]}]}""", """{"a":[{"b":[1,2]},"x"]}""", ignoreArrayOrder: true)).IsNull();
    }

    [Test]
    public async Task Ignoring_array_order_still_counts_duplicates()
    {
        var diff = Diff("[1,1,2]", "[1,2,2]", ignoreArrayOrder: true);

        await Assert.That(diff!.UserMessage).IsEqualTo("Array mismatch at $.\nMissing item:\n - [1]: the number 1\nAdditional item:\n + [2]: the number 2");
    }

    [Test]
    public async Task Ignoring_array_order_reports_the_values_that_differ()
    {
        var diff = Diff("[1,2,3]", "[3,9,1]", ignoreArrayOrder: true);

        await Assert.That(diff!.UserMessage).IsEqualTo("Array mismatch at $.\nMissing item:\n - [1]: the number 2\nAdditional item:\n + [1]: the number 9");
    }

    [Test]
    public async Task Ignoring_array_order_finds_the_best_pairing_with_allowed_additional_properties()
    {
        // {"a":1} fits both actual items, {"a":1,"b":2} fits only one: a greedy pairing would fail here
        var diff = Diff("""[{"a":1},{"a":1,"b":2}]""", """[{"a":1,"b":2},{"a":1,"c":3}]""", allowAdditionalProperties: true, ignoreArrayOrder: true);

        await Assert.That(diff).IsNull();
    }

    [Test]
    public async Task Exception_message_contains_the_compared_documents()
    {
        var diff = Diff("1", "2");

        await Assert.That(diff!.Message).Contains("Expected: 1");
        await Assert.That(diff.Message).Contains("Actual: 2");
        await Assert.That(diff.DiffMessages.Count).IsEqualTo(1);
    }
}
