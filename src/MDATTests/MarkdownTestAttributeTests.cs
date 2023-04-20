using MDATTests;
using MDATTests.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;

namespace MDAT.Tests
{
    /// <summary>
    /// Ignored comment
    /// </summary>
    [TestClass]
    public class MarkdownTestAttributeTests
    {

        /// <summary/>
        public class Params
        {
            public Dictionary<object, object> Test { get; set; }
        }

        static IEnumerable<object[]> ReusableTestDataProperty
        {
            get
            {
                return new[]
                {
                    new object[] { new Params() { Test = new Dictionary<object, object> { { "test", new Dictionary<object, object> { { "test", 1 } } } } } },
                };
            }
        }

        /// <summary>
        /// Simple test, addition 2 numbers, compare expected result
        /// </summary>
        [TestMethod]
        [DynamicData("ReusableTestDataProperty")]
        public async Task Dynam(Params val1)
        {
            _ = await Verify.Assert(() => Task.FromResult(val1), new Expected() { data = "{\"Test\":{\"test\":{\"test\":1}}}" });
        }

        /// <summary>
        /// Simple test, addition 2 numbers, compare expected result
        /// </summary>
        [TestMethod]
        [MarkdownTest("~/Tests/test.md")]
        public async Task Md1(int val1, int val2, string expected)
        {
            _ = await Verify.Assert(() => Task.FromResult(Utils.Calculer(val1, val2)), expected);
        }

        /// <summary>
        /// Multi level object
        /// </summary>
        [TestMethod]
        [MarkdownTest("~/Tests/{method}.md")]
        public async Task Multi_level(FormulaireWebFRW1DO form, Expected expected)
        {
            _ = await Verify.Assert(() => Task.FromResult(form), expected);
        }

        /// <summary>
        /// External file include
        /// </summary>
        [TestMethod]
        [MarkdownTest("~/Tests/{method}.md")]
        public async Task External_file(FormulaireWebFRW1DO form, Expected expected)
        {
            _ = await Verify.Assert(() => Task.FromResult(form), expected);
        }

        /// <summary>
        /// External file include 2
        /// </summary>
        [TestMethod]
        [MarkdownTest("~/Tests/{method}.md")]
        public async Task External_file2(string form, Expected expected)
        {
            _ = await Verify.Assert(() => Task.FromResult(form), expected);
        }

        /// <summary>
        /// External file include base64
        /// </summary>
        [TestMethod]
        [MarkdownTest("~/Tests/{method}.md")]
        public async Task External_file_base64(byte[] form, Expected expected)
        {
            _ = await Verify.Assert(() => Task.FromResult(form), expected);
        }

        /// <summary>
        /// External file include base64 string
        /// </summary>
        [TestMethod]
        [MarkdownTest("~/Tests/{method}.md")]
        public async Task External_file_base64_String(string form, Expected expected)
        {
            _ = await Verify.Assert(() => Task.FromResult(form), expected);
        }

        /// <summary>
        /// Dictionary_object_object
        /// </summary>
        [TestMethod]
        [MarkdownTest("~/Tests/{method}.md")]
        public async Task Dictionary_object_object(Dictionary<object, object> dict, Expected expected)
        {
            _ = await Verify.Assert(() => Task.FromResult(dict), expected);
        }

        /// <summary>
        /// Dictionary_object_object in Dictionary_object_object 
        /// </summary>
        [TestMethod]
        [MarkdownTest("~/Tests/{method}.md")]
        public async Task Dictionary_object_object_in_Dictionary_object_object(Dictionary<object, object> dict, Expected expected)
        {
            _ = await Verify.Assert(() => Task.FromResult(dict), expected);
        }

        /// <summary>
        /// Exception test
        /// </summary>
        [TestMethod]
        [MarkdownTest("~/Tests/{method}.md")]
        public async Task Crash_Test(int val1, int val2, Expected expected)
        {
            _ = await Verify.Assert(() => Task.FromResult(Utils.CrashExceptionCheck(val1, val2)), expected);
        }

        /// <summary>
        /// Simple summary test for validation
        /// With multiline
        /// </summary>
        /// <param name="db">Database mock</param>
        /// <param name="expected">Expected result</param>
        /// <returns></returns>
        public void MdWithSummary(FormulaireWebFRW1DO db, Expected expected) { }


        public void MdWithoutSummary(FormulaireWebFRW1DO db, Expected expected) { }

        /// <summary>
        /// Test XML comments extraction and file generation
        /// This test whole document integrity
        /// </summary>
        [TestMethod]
        [MarkdownTest("~\\Tests\\{method}.md")]
        public async Task Md_Extraction_test(Expected expected)
        {
            var guid = Guid.NewGuid();

            var test = new MarkdownTestAttribute($"~\\Tests\\Generated\\Generated-md-Test-{guid}.md");
            var method = Utils.GetMethodInfo(() => MdWithSummary);

            test.GetData(method);

            object value = await Verify.Assert(() =>
                                        Task.FromResult(File.ReadAllText(test.ParsedPath)), expected);
        }

        /// <summary>
        /// Test tests naming values
        /// </summary>
        [TestMethod]
        [MarkdownTest("~\\Tests\\{method}.md")]
        public async Task Test_Tests_Naming(Expected expected)
        {
            var test = new MarkdownTestAttribute($"~\\Tests\\test.md");
            var method = Utils.GetMethodInfo(() => Md1);

            var tests = test.GetData(method);

            object value = await Verify.Assert(() =>
                                        Task.FromResult(test.GetDisplayName(method, tests.LastOrDefault())!), expected);
        }

        /// <summary>
        /// Test XML comments extraction and file generation
        /// This test whole document integrity
        /// </summary>
        [TestMethod]
        public async Task Md_no_method_test()
        {
            var guid = Guid.NewGuid();

            var test = new MarkdownTestAttribute($"~\\Tests\\Generated\\Generated-md-Test-{guid}.md");

            object value = await Verify.Assert(() =>
                                        Task.FromResult(test.GetData(null!)), new Expected()
                                        {
                                            data = "{ " +
                                                        "\"ClassName\":\"System.ArgumentNullException\",       " +
                                                        "\"Message\":\"Value cannot be null.\"," +
                                                        "\"ParamName\":\"testMethod\"}",
                                            allowAdditionalProperties = true
                                        });
        }

        /// <summary>
        /// Test XML comments extraction and file generation
        /// This test whole document integrity
        /// </summary>
        [TestMethod]
        [MarkdownTest("~\\Tests\\{method}.md")]
        public async Task Md_Extraction_test_nosummary(Expected expected)
        {
            var guid = Guid.NewGuid();

            var test = new MarkdownTestAttribute($"~\\Tests\\Generated\\Generated-md-Test-nosummary-{guid}.md");
            var method = Utils.GetMethodInfo(() => MdWithoutSummary);

            test.GetData(method);

            object value = await Verify.Assert(() =>
                                        Task.FromResult(File.ReadAllText(test.ParsedPath)), expected);
        }
    }
}