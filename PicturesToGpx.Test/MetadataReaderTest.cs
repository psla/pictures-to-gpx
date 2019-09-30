using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PicturesToGpx.Test
{
    [TestClass]
    public class MetadataReaderTest
    {
        [TestMethod]
        public void ReadMetadataFromLumia()
        {
            var position = ImageUtility.TryExtractPositionFromFile("WP_20141211_001.jpg");
            Assert.IsNotNull(position);
            Assertions.AreApproximatelyEqual(47.6044703, position.Latitude);
            Assertions.AreApproximatelyEqual(-122.1496161, position.Longitude);
        }
    }
}
