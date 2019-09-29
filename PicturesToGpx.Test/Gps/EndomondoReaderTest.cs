using Microsoft.VisualStudio.TestTools.UnitTesting;
using PicturesToGpx.Gps;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PicturesToGpx.Test.Gps
{
    [TestClass]
    public class EndomondoReaderTest
    {
        [TestMethod]
        public void TestReadingJson()
        {
            var reader = new EndomondoJsonReader();
            var points = reader.Read("gps\\endomondo.json").ToList();

            Assert.AreEqual(53.908485, points[0].Latitude);
            Assert.AreEqual(17.526583, points[0].Longitude);
            // UTC
            Assert.AreEqual(new DateTimeOffset(2010, 07, 18, 16, 54, 28, TimeSpan.Zero), points[0].Time);
        }
    }
}
