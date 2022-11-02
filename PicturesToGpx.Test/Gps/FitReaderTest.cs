using Microsoft.VisualStudio.TestTools.UnitTesting;
using PicturesToGpx.Gps;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PicturesToGpx.Test.Gps
{
    [TestClass]
    public class FitReaderTest
    {
        [TestMethod]
        public void TestReadingFitFile()
        {
            var reader = new FitReader();
            using (var stream = File.OpenRead(@"gps\\490518450.fit.gz"))
            using (var gzipStream = new GZipStream(stream, CompressionMode.Decompress))
            using (var memoryStream = new MemoryStream())
            {
                gzipStream.CopyTo(memoryStream);
                memoryStream.Seek(0, SeekOrigin.Begin);
                var points = reader.Read(memoryStream).ToList();
                Assert.AreEqual(2523, points.Count);
                Assert.AreEqual(47.6047248455718, points[0].Latitude, 0.00001);
                Assert.AreEqual(-122.149795263621, points[0].Longitude, 0.00001);
                // UTC
                Assert.AreEqual(new DateTimeOffset(2015, 11, 22, 18, 57, 04, TimeSpan.Zero), points[0].Time);
                // Should be at least 35.64 according to ride with gps.
                Assert.AreEqual(35.59, points.TotalDistanceMeters(), 0.01);
            }
        }
    }
}
