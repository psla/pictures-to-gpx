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
            var position = ImageUtility.TryExtractPositionFromFile("WP_20150701_007.jpg");
            Assert.IsNotNull(position);
            Assertions.AreApproximatelyEqual(36.7630839, position.Latitude);
            Assertions.AreApproximatelyEqual(-111.6267836, position.Longitude);
        }
    }
}
