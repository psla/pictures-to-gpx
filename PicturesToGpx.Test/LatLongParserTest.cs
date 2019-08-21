using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PicturesToGpx.Test
{
    [TestClass]
    public class LatLongParserTest
    {
        [TestMethod]
        public void TestParsePositiveOne()
        {
            Assertions.AreApproximatelyEqual(47.7053278, LatLongParser.ParseString(@"47° 42' 19.18"""));
        }

        [TestMethod]
        public void TestParseNegativeOne()
        {
            Assertions.AreApproximatelyEqual(-122.204028, LatLongParser.ParseString(@"-122° 12' 14.5"""));
        }

        [TestMethod]
        public void TestParseWithNoFractionalSeconds()
        {
            Assertions.AreApproximatelyEqual(33.600556, LatLongParser.ParseString(@"33° 36' 2"""));
        }
    }
}
