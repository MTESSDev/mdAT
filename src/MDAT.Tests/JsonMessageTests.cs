namespace MDAT.Tests;

/// <summary>
/// How the differences are written: every kind of value has its own description, and the items of an array are expanded one
/// level. The expected texts are the ones of the engine mdAT used before 2.0 (Quibble.Xunit), so a test written for 1.x reads
/// the same messages.
/// </summary>
public class JsonMessageTests
{
    private static string Messages(string expected, string actual)
    {
        try
        {
            JsonAssert.Equal(expected, actual);
            return "(equal)";
        }
        catch (JsonAssertException ex)
        {
            return ex.UserMessage;
        }
    }

    [Test]
    [Arguments("null", "0", "Type mismatch at $.\nExpected null but was the number 0.")]
    [Arguments("true", "\"true\"", "Type mismatch at $.\nExpected the boolean true but was the string true.")]
    [Arguments("false", "1", "Type mismatch at $.\nExpected the boolean false but was the number 1.")]
    [Arguments("\"abc\"", "5", "Type mismatch at $.\nExpected the string abc but was the number 5.")]
    [Arguments("5", """{"a":1}""", "Type mismatch at $.\nExpected the number 5 but was an object.")]
    [Arguments("[]", "{}", "Type mismatch at $.\nExpected an empty array but was an object.")]
    [Arguments("[1]", "{}", "Type mismatch at $.\nExpected an array with 1 item but was an object.")]
    [Arguments("[1,2]", "\"\"", "Type mismatch at $.\nExpected an array with 2 items but was the string .")]
    [Arguments("{}", "[1]", "Type mismatch at $.\nExpected an object but was an array with 1 item.")]
    [Arguments("true", "false", "Boolean value mismatch at $.\nExpected true but was false.")]
    [Arguments("false", "true", "Boolean value mismatch at $.\nExpected false but was true.")]
    public async Task Every_kind_of_value_has_its_own_description(string expected, string actual, string message)
    {
        await Assert.That(Messages(expected, actual)).IsEqualTo(message);
    }

    [Test]
    public async Task Scalar_items_of_an_array_are_described_one_per_line()
    {
        var message = Messages("[]", """["str",true,false,null,1.50]""");

        await Assert.That(message).IsEqualTo(
            "Array mismatch at $.\nAdditional items:\n + [0]: the string str\n + [1]: the boolean true\n + [2]: the boolean false\n + [3]: null\n + [4]: the number 1.50");
    }

    [Test]
    public async Task Array_and_object_items_are_expanded_one_level()
    {
        var inArray = Messages("[]", """[[true,null,"s",1,{},[],[1],{"a":1},[1,2],{"a":1,"b":2}]]""");

        await Assert.That(inArray).IsEqualTo(
            "Array mismatch at $.\nAdditional item:\n + [0]: [\n  true\n  null\n  's'\n  1\n  {}\n  []\n  [ 1 item ]\n  { 1 property }\n  [ 2 items ]\n  { 2 properties }\n]");

        var inObject = Messages("[]", """[{"t":true,"f":false,"n":null,"s":"s","i":1,"o":{},"a":[],"a1":[1],"o1":{"a":1},"a2":[1,2],"o2":{"a":1,"b":2}}]""");

        await Assert.That(inObject).IsEqualTo(
            "Array mismatch at $.\nAdditional item:\n + [0]: {\n  't': true\n  'f': false\n  'n': null\n  's': 's'\n  'i': 1\n  'o': {}\n  'a': []\n  'a1': [ 1 item ]\n  'o1': { 1 property }\n  'a2': [ 2 items ]\n  'o2': { 2 properties }\n}");

        var empty = Messages("[]", "[[],{}]");

        await Assert.That(empty).IsEqualTo("Array mismatch at $.\nAdditional items:\n + [0]: [\n]\n + [1]: {\n}");
    }

    [Test]
    public async Task Properties_are_listed_with_their_type()
    {
        var message = Messages("{}", """{"a":1,"b":"s","c":true,"d":false,"e":null,"f":[],"g":{}}""");

        await Assert.That(message).IsEqualTo(
            "Object mismatch at $.\nAdditional properties:\n + 'a' (number)\n + 'b' (string)\n + 'c' (bool)\n + 'd' (bool)\n + 'e' (null)\n + 'f' (array)\n + 'g' (object)");

        var missing = Messages("""{"a":1,"b":[1]}""", "{}");

        await Assert.That(missing).IsEqualTo("Object mismatch at $.\nMissing properties:\n - 'a' (number)\n - 'b' (array)");
    }

    [Test]
    public async Task Paths_use_dots_for_simple_names_and_brackets_for_the_others()
    {
        JsonAssertException? error = null;

        try
        {
            JsonAssert.Equal("""{"a b":1,"1x":1,"under_score":1,"":1}""", """{"a b":2,"1x":2,"under_score":2,"":2}""");
        }
        catch (JsonAssertException ex)
        {
            error = ex;
        }

        await Assert.That(error).IsNotNull();
        await Assert.That(error!.DiffMessages.Count).IsEqualTo(4);
        await Assert.That(error.DiffMessages[0]).StartsWith("Number value mismatch at $['a b'].");
        await Assert.That(error.DiffMessages[1]).StartsWith("Number value mismatch at $.1x.");
        await Assert.That(error.DiffMessages[2]).StartsWith("Number value mismatch at $['under_score'].");
        await Assert.That(error.DiffMessages[3]).StartsWith("Number value mismatch at $[''].");
        await Assert.That(error.UserMessage).StartsWith("Found 4 differences.\n# 1: ");
    }
}
