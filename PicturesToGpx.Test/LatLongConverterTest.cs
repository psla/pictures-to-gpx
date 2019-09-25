using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace PicturesToGpx.Test
{
    [TestClass]
    public class LatLongConverterTest
    {
        [TestMethod]
        public void TestParsePositiveOne()
        {
            var p = LocationUtils.ToMercator(new Position(DateTime.Now, 44.5, 33.4));
            Assertions.AreApproximatelyEqual(5543147.20, p.Latitude, 0.01); // Y
            Assertions.AreApproximatelyEqual(3718070.99, p.Longitude, 0.01); // X
        }
    }
}
