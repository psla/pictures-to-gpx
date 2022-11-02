using Microsoft.VisualStudio.TestTools.UnitTesting;
using PicturesToGpx.Gps;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PicturesToGpx.Test.Gps
{
    [TestClass]
    public class GoogleTimelineKmlReaderTest
    {
        [TestMethod]
        public void TestReadingFile()
        {
            var reader = new GoogleTimelineKmlReader();
            var positions = reader.Read(@"Gps\GoogleTimeline.kml");
            Assertions.AreEqual(new List<Position>()
            {
                new Position(new DateTimeOffset(2016,12,18,00,46,26, TimeSpan.Zero), 45.705188, -12.2042061),
                new Position(new DateTimeOffset(2016, 12, 18, 00, 47, 39, TimeSpan.Zero), 45.7053364,-12.20418839999999)
            },
            positions.ToList());
        }
    }
}
