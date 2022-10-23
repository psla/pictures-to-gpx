using Microsoft.VisualStudio.TestTools.UnitTesting;
using PicturesToGpx.Gps;
using System;
using System.Linq;

namespace PicturesToGpx.Test.Gps
{
    [TestClass]
    public class GoogleTimelineJsonReaderTest
    {
        [TestMethod]
        public void TestReadingFile()
        {
            var reader = new GoogleTimelineJsonReader(40);
            var positions = reader.Read(@"Gps\GoogleTimeline.json").ToList();

            Assert.AreEqual(1, positions.Count);
            Assert.AreEqual(
                new DateTimeOffset(2016, 12, 18, 00, 46, 26, 813, TimeSpan.Zero).ToUnixTimeMilliseconds(), 
                positions[0].Time.ToUnixTimeMilliseconds());
            Assert.AreEqual(
                new DateTimeOffset(2016, 12, 18, 00, 46, 26, 813, TimeSpan.Zero),
                positions[0].Time);

            Assert.AreEqual(45.708188, positions[0].Latitude, 0.000001);
            Assert.AreEqual(-11.2042061, positions[0].Longitude, 0.000001);
        }

        [TestMethod]
        public void TestReadingFileIgnoresAccuracy()
        {
            var reader = new GoogleTimelineJsonReader(15);
            var positions = reader.Read(@"Gps\GoogleTimeline.json").ToList();

            Assert.AreEqual(0, positions.Count);
        }


        [TestMethod]
        public void TestReadingFileFrom2022()
        {
            var reader = new GoogleTimelineJsonReader(40);
            var positions = reader.Read(@"Gps\GoogleTimeline2022.json").ToList();

            Assert.AreEqual(1, positions.Count);
            Assert.AreEqual(
                new DateTimeOffset(2016, 12, 18, 07, 21, 21, 0, TimeSpan.Zero),
                positions[0].Time);

            Assert.AreEqual(47.70544, positions[0].Latitude, 0.000001);
            Assert.AreEqual(-122.2042971, positions[0].Longitude, 0.000001);
        }

    }
}
