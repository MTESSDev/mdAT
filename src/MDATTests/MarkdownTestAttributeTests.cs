using MDATTests;
using MDATTests.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using static MDAT.Tests.MarkdownTestAttributeTests;

//[assembly: TestDataSourceDiscovery(TestDataSourceDiscoveryOption.DuringExecution)]

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
            _ = await Verify.Assert(() => Task.FromResult(val1), new Expected() { verify = new VerifyStep[] { new VerifyStep { data = "{\"Test\":{\"test\":{\"test\":1}}}", type = "match", jsonPath = "$" } } });
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

        private async Task Md2(ObjectOrException<IEnumerable<FormulaireWebFRW1DO>> formList)
        {
            await Task.Delay(1);
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
        /// Test Exception
        /// </summary>
        [TestMethod]
        [MarkdownTest("~/Tests/{method}.md")]
        public async Task Exception_test(FormulaireWebFRW1DO form, Expected expected)
        {
            _ = await Verify.Assert(() => Task.FromResult(ThrowE()), expected);
        }

        private bool ThrowE()
        {
            throw new InvalidOperationException();
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
        /// Nullable byte[]
        /// </summary>
        [TestMethod]
        [MarkdownTest("~/Tests/{method}.md")]
        public async Task Nullable_byte_array(byte[]? form, Expected expected)
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
        /// External file include byte[] dictionary
        /// </summary>
        [TestMethod]
        [MarkdownTest("~/Tests/{method}.md")]
        public async Task External_file_byte_dictionary(Dictionary<string, byte[]> form, Expected expected)
        {
            _ = await Verify.Assert(() => Task.FromResult(form), expected);
        }

        /// <summary>
        /// External file include string dictionary
        /// </summary>
        [TestMethod]
        [MarkdownTest("~/Tests/{method}.md")]
        public async Task External_file_string_dictionary(Dictionary<string, string> form, Expected expected)
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
        /// Test object deserialization 
        /// </summary>
        [TestMethod]
        [MarkdownTest("~\\Tests\\{method}.md")]
        public async Task Test_object_deserialization(Expected expected)
        {
            var test = new MarkdownTestAttribute($"~\\Tests\\test-output-test-object-deserialization.md");
            var method = Utils.GetMethodInfo(() => Md2);

            var tests = test.GetData(method);

            object value = await Verify.Assert(() =>
                                        Task.FromResult(File.ReadAllText(test.ParsedPath)), expected);
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
                                            verify = new VerifyStep[] { new VerifyStep { data = "{ " +
                                                        "\"ClassName\":\"System.ArgumentNullException\",       " +
                                                        "\"Message\":\"Value cannot be null.\"," +
                                                        "\"ParamName\":\"testMethod\"}", type = "match", jsonPath = "$",
                                            allowAdditionalProperties = true } },
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

        /// <summary>
        /// Test output expected
        /// </summary>
        [TestMethod]
        [MarkdownTest("~/Tests/{method}.md")]
        public async Task Output_expected(Dictionary<string, string> form, byte[] bytes, Expected expected)
        {
            _ = await Verify.Assert(() => Task.FromResult(new { form, bytes }), expected);
        }


        /// <summary>
        /// Test jsonPath array count
        /// </summary>
        [TestMethod]
        [MarkdownTest("~/Tests/{method}.md")]
        public async Task JsonPath(Expected expected)
        {
            _ = await Verify.Assert(() => Task.FromResult(new { test = new string[] { "val1", "val2" } }), expected);
        }

        /// <summary>
        /// Test jsonPath array count
        /// </summary>
        [TestMethod]
        [MarkdownTest("~/Tests/{method}.md")]
        public async Task TestObjectOrException(
            ObjectOrException<FormulaireWebFRW1DO> formulaireWebFRW1DO,
            Expected expected)
        {
            var mock = new Mock<IFormulaireWebFRW1DO>();

            mock.Setup(e => e.ReturnVal()).ReturnsOrThrows(formulaireWebFRW1DO);

            _ = await Verify.Assert(() => Task.FromResult(new Test().ReturnTest(mock.Object)), expected);
        }

        /// <summary>
        /// Test jsonPath array count
        /// </summary>
        [TestMethod]
        [MarkdownTest("~/Tests/{method}.md")]
        public async Task TestObjectOrExceptionAsync(
            ObjectOrException<FormulaireWebFRW1DO> formulaireWebFRW1DO,
            Expected expected)
        {
            var mock = new Mock<IFormulaireWebFRW1DO>();

            _ = mock.Setup(e => e.ReturnValAsync()).ReturnsOrThrowsAsync(formulaireWebFRW1DO);

            _ = await Verify.Assert(async () => await new Test().ReturnTestAsync(mock.Object), expected);
        }

        /// <summary>
        /// Test object deserialization 
        /// </summary>
        [TestMethod]
        [MarkdownTest("~\\Tests\\{method}.md")]
        public async Task Test_keypairvalues(List<KeyValuePair<string, string>> input, Expected expected)
        {

            object value = await Verify.Assert(() =>
                                        Task.FromResult(input), expected);
        }

        /// <summary>
        /// Test class
        /// </summary>
        public class Test
        {
            /// <summary>
            /// Return Test
            /// </summary>
            /// <param name="form"></param>
            /// <returns></returns>
            public FormulaireWebFRW1DO ReturnTest(IFormulaireWebFRW1DO form)
            {
                return form.ReturnVal();
            }

            /// <summary>
            /// Return Test
            /// </summary>
            /// <param name="form"></param>
            /// <returns></returns>
            public async Task<FormulaireWebFRW1DO> ReturnTestAsync(IFormulaireWebFRW1DO form)
            {
                return await form.ReturnValAsync();
            }
        }
    }
}