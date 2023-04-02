using MDATTests;
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
            var method = GetMethodInfo(() => MdWithSummary);

            test.GetData(method);

            object value = await Verify.Assert(() =>
                                        Task.FromResult(File.ReadAllText(test.ParsedPath)), expected);
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
            var method = GetMethodInfo(() => MdWithoutSummary);

            test.GetData(method);

            object value = await Verify.Assert(() =>
                                        Task.FromResult(File.ReadAllText(test.ParsedPath)), expected);
        }

        public static MethodInfo GetMethodInfo(LambdaExpression expression)
        {
            var outermostExpression = expression.Body as MethodCallExpression;

            if (outermostExpression is null)
            {
                if (expression.Body is UnaryExpression ue && ue.Operand is MethodCallExpression me && me.Object is System.Linq.Expressions.ConstantExpression ce && ce.Value is MethodInfo mi)
                    return mi;
                throw new ArgumentException("Invalid Expression. Expression should consist of a Method call only.");
            }

            var method = outermostExpression.Method;
            if (method is null)
                throw new Exception($"Cannot find method for expression {expression}");

            return method;
        }
    }

    public class FormulaireWebFRW1DO
    {
        /// <summary>
        /// Numéro séquentiel de formulaire
        /// </summary>
        public int FW_NS_FORM_WEB { get; set; }

        /// <summary>
        /// Numéro public de formulaire (GUID)
        /// </summary>
        public Guid FW_N_PUBL_FORM_WEB { get; set; }

        /// <summary>
        /// Numéro de confirmation de transmission (optionnel)
        /// </summary>
        public long? FW_N_CONF { get; set; }

        /// <summary>
        /// Contenu du formulaire JSON, compressé
        /// </summary>
        public string FW_DE_CONT_FORM_WEB { get; set; } = default!;

        /// <summary>
        /// [FK] Numéro de système autorisé
        /// </summary>
        public int FW_NS_SYST_AUTR { get; set; }

        /// <summary>
        /// Code utilisateur ou identifiant de ressource assignée
        /// </summary>
        public string? FW_V_IDEN_UTIL { get; set; }

        /// <summary>
        /// Type de formulaire
        /// </summary>
        /// <example>3003</example>
        public string FW_C_TYPE_FORM_WEB { get; set; } = default!;

    }
}