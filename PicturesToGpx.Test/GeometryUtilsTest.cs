using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace PicturesToGpx.Test
{
    [TestClass]
    public class GeometryUtilsTest
    {
        [TestMethod]
        public void CalculateTotalDistance_CalculatesAccurateDistance()
        {
            var time = new DateTimeOffset(2026, 7, 1, 12, 0, 0, TimeSpan.Zero);
            var points = new List<Position>
            {
                new Position(time, 50.0, 20.0),
                new Position(time.AddMinutes(10), 50.01, 20.0),
                new Position(time.AddMinutes(20), 50.01, 20.01),
            };

            double distance = points.CalculateTotalDistance();
            Assert.IsTrue(distance > 1500 && distance < 2500, "Distance should be approximately 1.8km, got: " + distance);
        }

        [TestMethod]
        public void CalculateTotalDistance_IgnoresGapsExceedingMaxHours()
        {
            var time = new DateTimeOffset(2026, 7, 1, 12, 0, 0, TimeSpan.Zero);
            var points = new List<Position>
            {
                new Position(time, 50.0, 20.0),
                new Position(time.AddMinutes(10), 50.01, 20.0), // ~1.1km
                // Gap of 10 hours and jumping to another region
                new Position(time.AddHours(10), 52.0, 21.0),
                new Position(time.AddHours(10).AddMinutes(10), 52.01, 21.0), // ~1.1km
            };

            double distanceWithGapExcluded = points.CalculateTotalDistance(maxGapHours: 5.0);
            double d1 = points[0].DistanceMeters(points[1]);
            double d2 = points[2].DistanceMeters(points[3]);

            Assert.AreEqual(d1 + d2, distanceWithGapExcluded, 0.01);
        }

        [TestMethod]
        public void CalculateTotalDistance_EmptyAndSinglePoint_ReturnsZero()
        {
            Assert.AreEqual(0.0, new List<Position>().CalculateTotalDistance());
            Assert.AreEqual(0.0, new List<Position> { new Position(DateTimeOffset.UtcNow, 50.0, 20.0) }.CalculateTotalDistance());
        }

        [TestMethod]
        public void SkipTooClose_ReducesDensePoints()
        {
            var points = new List<Position>
            {
                new Position(DateTimeOffset.UtcNow, 0.001, 0.001, PositionUnit.Pixel),
                new Position(DateTimeOffset.UtcNow, 0.002, 0.002, PositionUnit.Pixel),
                new Position(DateTimeOffset.UtcNow, 100.0, 100.0, PositionUnit.Pixel),
            };

            var decimated = new List<Position>(points.SkipTooClose(10));
            Assert.AreEqual(2, decimated.Count);
        }
    }
}
