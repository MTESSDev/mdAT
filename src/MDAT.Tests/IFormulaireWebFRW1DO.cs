using MDATTests.Models;

namespace MDAT.Tests
{
    /// <summary>
    /// Test Interface
    /// </summary>
    public interface IFormulaireWebFRW1DO
    {
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public FormulaireWebFRW1DO ReturnVal();

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public Task<FormulaireWebFRW1DO> ReturnValAsync();

    }
}