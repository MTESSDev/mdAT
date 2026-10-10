namespace MDAT.Tests;

/// <summary>
/// Tests of the TUnit data sources: every case of a markdown file is its own test and the arguments reach
/// the test method as built from the YAML, whatever their type.
/// </summary>
public class AdapterTests
{
    public class Poco
    {
        public string? A { get; set; }

        public int B { get; private set; }
    }

    public class WithDictionary
    {
        public Dictionary<object, object>? D { get; set; }
    }

    [Test]
    [MarkdownDataSource("~/Tests/adapter-simple.md")]
    public async Task Simple_values(int a, string b)
    {
        await Assert.That(b).IsNotNull();
        await Assert.That(a).IsGreaterThan(0);
    }

    [Test]
    [MarkdownDataSource("~/Tests/adapter-dateonly.md")]
    public async Task DateOnly_and_TimeOnly_arrive_intact(DateOnly date, TimeOnly time)
    {
        await Assert.That(date.Year).IsEqualTo(2024);
        await Assert.That(time.Hour).IsGreaterThan(0);
    }

    [Test]
    [MarkdownDataSource("~/Tests/adapter-dictionary.md")]
    public async Task Dictionary_of_objects_arrives_intact(WithDictionary value)
    {
        await Assert.That(value.D).IsNotNull();
        await Assert.That(value.D!["k"]).IsTypeOf<Dictionary<object, object>>();
    }

    [Test]
    [MarkdownDataSource("~/Tests/adapter-expected.md")]
    public async Task Expected_with_verification_steps(Dictionary<string, object> input, Expected expected)
    {
        await Assert.That(input.Count).IsEqualTo(1);
        await Assert.That(expected.verify!.Count()).IsEqualTo(1);
    }

    [Test]
    [MarkdownDataSource("~/Tests/adapter-ignore-array-order.md")]
    public async Task Array_order_is_ignored_when_the_test_case_asks_for_it(List<int> values, List<Poco> items, Expected expected)
    {
        _ = await Verify.Assert(() => Task.FromResult(new { values, items }), expected);
    }
}
