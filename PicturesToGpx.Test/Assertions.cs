using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace PicturesToGpx.Test
{
    internal static class Assertions
    {
        internal static void AreApproximatelyEqual(double expected, double actual, double delta = 0.00001)
        {
            Assert.IsTrue(Math.Abs(expected - actual) < delta, $"Expected: {expected} != Actual: {actual}");
        }
    }
}
