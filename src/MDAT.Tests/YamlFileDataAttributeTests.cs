
using System.Diagnostics.CodeAnalysis;

namespace MDAT.Tests
{
    //[TestClass]
    [ExcludeFromCodeCoverage]
    public class YamlFileDataAttributeTests
    {
        [Test]
        [YamlFileDataSource("~\\Tests\\test.yaml")]
        public async Task YamlFileDataAttributeTestAsync(int val1, int val2, string expected)
        {
            object value = await Verify.Assert(() =>
                                            Task.FromResult(
                                                    Calculer(val1, val2)), expected);
        }

        private int Calculer(int val1, int val2)
        {
            return val1 + val2;
        }
    }

}