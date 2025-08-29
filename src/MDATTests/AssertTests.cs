using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace MDAT.Tests
{
    /// <summary>
    /// Ignored comment
    /// </summary>
    public class AssertTests
    {
        /// <summary>
        /// Run without expected obj
        /// </summary>
        [Fact]
        public async Task Test_no_expected()
        {
            object value = await Verify.Assert(() =>
                                        Task.FromResult((object)null!), (Expected)null!);
        }

        [Fact]
        public async Task Test_numeric()
        {
            object value = await Verify.Assert(() =>
                                        Task.FromResult(2), new Expected { verify = new VerifyStep[] { new VerifyStep { type = "match", data = 2 } } });
        }

        /// <summary>
        /// Run without expected obj
        /// </summary>
        [Fact]
        public async Task Test_expected_data_is_string()
        {
            object value = await Verify.Assert(() =>
                                        Task.FromResult("{Test!"), new Expected() { verify = new VerifyStep[] { new VerifyStep { data = "{Test!", type = "match", jsonPath = "$" } } });
        }

        /// <summary>
        /// Test Stream output
        /// </summary>
        [Fact]
        public async Task Test_stream_output_as_base64()
        {
            var memory = new MemoryStream(new byte[] { 0x01, 0x02, 0x03, 0x04 });

            object value = await Verify.Assert(() =>
                                        Task.FromResult(memory), new Expected { verify = new VerifyStep[] { new() { data = "AQIDBA==" } } });
        }

        /// <summary>
        /// Test Stream output
        /// </summary>
        [Fact]
        public async Task Test_stream_output_as_base64_result()
        {
            var mockTest = new Mock<IDemoResult>();
            var memory = new MemoryStream(new byte[] { 0x01, 0x02, 0x03, 0x04 });

            mockTest.Setup(m => m.DemoReturnStream()).Returns(memory);
            object value = await Verify.Assert(() => Test(mockTest.Object), new Expected { verify = new VerifyStep[] { new() { data = "{\r\n  \"FileStream\": \"AQIDBA==\",\r\n  \"ContentType\": \"plain/text\",\r\n  \"FileDownloadName\": \"\",\r\n  \"LastModified\": null,\r\n  \"EntityTag\": null,\r\n  \"EnableRangeProcessing\": false\r\n}" } } });
        }


        private static async Task<IActionResult> Test(IDemoResult demoResult)
        {
            await Task.Delay(1);
            return new FileStreamResult(demoResult.DemoReturnStream(), "plain/text");
        }

        public interface IDemoResult
        {
            public Stream DemoReturnStream();
        }

        public class DemoResult : IDemoResult
        {
            public Stream DemoReturnStream()
            {
                throw new NotImplementedException();
            }
        }
    }
}