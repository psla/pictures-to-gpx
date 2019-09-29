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
            Assertions.AreApproximatelyEqual(5543147.20, p.Latitude, 0.005); // Y
            Assertions.AreApproximatelyEqual(3718070.99, p.Longitude, 0.005); // X
        }

        [TestMethod]
        public void TestParseNegativeOne()
        {
            var p = LocationUtils.ToMercator(new Position(DateTime.Now, -44.5, -33.4));
            Assertions.AreApproximatelyEqual(-5543147.20, p.Latitude, 0.005); // Y
            Assertions.AreApproximatelyEqual(-3718070.99, p.Longitude, 0.005); // X
        }

        [TestMethod]
        public void TestParseItBack()
        {
            var p = LocationUtils.FromMercatorToWgs84(new Position(DateTime.Now, 5543147.20, 3718070.99, PositionUnit.Mercator));
            Assertions.AreApproximatelyEqual(44.5, p.Latitude, 0.0000001);
            Assertions.AreApproximatelyEqual(33.4, p.Longitude, 0.0000001); 
        }
    }
}
