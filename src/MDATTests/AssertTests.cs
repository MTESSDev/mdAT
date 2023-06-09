﻿using MDATTests;
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
    }
}