using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PicturesToGpx.Test
{
    internal static class Assertions
    {
        internal static void AreApproximatelyEqual(double expected, double actual, double delta = 0.00001)
        {
            Assert.IsTrue(Math.Abs(expected - actual) < delta, $"Expected: {expected} != Actual: {actual}");
        }

        internal static void AreEqual<T>(IEnumerable<T> expected, IEnumerable<T> actual)
        {
            // Memory usage doesn't matter here, it's small! (and we still need to get count in case of failure)
            var expectedList = expected.ToList();
            var actualList = actual.ToList();
            Assert.AreEqual(expectedList.Count, actualList.Count);

            for (int i = 0; i < expectedList.Count; i++)
            {
                Assert.AreEqual(expectedList[i], actualList[i], $"{expectedList[i].ToString()} != {actualList[i].ToString()}");
            }

        }
    }
}
