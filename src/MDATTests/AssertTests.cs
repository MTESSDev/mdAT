using MDATTests;
using MDATTests.Models;
using Microsoft.AspNetCore.Mvc;
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
    public class AssertTests
    {
        /// <summary>
        /// Run without expected obj
        /// </summary>
        [TestMethod]
        public async Task Test_no_expected()
        {
            object value = await Verify.Assert(() =>
                                        Task.FromResult((object)null!), (Expected)null!);
        }

        [TestMethod]
        public async Task Test_numeric()
        {
            object value = await Verify.Assert(() =>
                                        Task.FromResult(2), new Expected { verify = new VerifyStep[] { new VerifyStep { type = "match", data = 2 } } });
        }

        /// <summary>
        /// Run without expected obj
        /// </summary>
        [TestMethod]
        public async Task Test_expected_data_is_string()
        {
            object value = await Verify.Assert(() =>
                                        Task.FromResult("{Test!"), new Expected() { verify = new VerifyStep[] { new VerifyStep { data = "{Test!", type = "match", jsonPath = "$" } } });
        }

        /// <summary>
        /// Test Stream output
        /// </summary>
        [TestMethod]
        public async Task Test_stream_output_as_base64()
        {
            var memory = new MemoryStream(new byte[] { 0x01, 0x02, 0x03, 0x04 });

            object value = await Verify.Assert(() =>
                                        Task.FromResult(memory), new Expected { verify = new VerifyStep[] { new() { data = "AQIDBA==" } } });
        }

        /// <summary>
        /// Test Stream output
        /// </summary>
        [TestMethod]
        public async Task Test_stream_output_as_base64_result()
        {
            object value = await Verify.Assert(() => Test(), new Expected { verify = new VerifyStep[] { new() { data = "{\r\n  \"FileStream\": \"AQIDBA==\",\r\n  \"ContentType\": \"plain/text\",\r\n  \"FileDownloadName\": \"\",\r\n  \"LastModified\": null,\r\n  \"EntityTag\": null,\r\n  \"EnableRangeProcessing\": false\r\n}" } } });
        }


        private static async Task<IActionResult> Test()
        {
            await Task.Delay(1);
            var memory = new MemoryStream(new byte[] { 0x01, 0x02, 0x03, 0x04 });
            return new FileStreamResult(memory, "plain/text");
        }
    }
}