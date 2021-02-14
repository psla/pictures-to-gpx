using Microsoft.VisualStudio.TestTools.UnitTesting;
using PicturesToGpx.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PicturesToGpx.Test
{
    [TestClass]
    public class ExifParserTest
    {
        [TestMethod]
        public void TestParseRational()
        {
            Assertions.AreApproximatelyEqual(3.946, ExifParser.ParseRationalOrDouble("1973/500"));
        }

        [TestMethod]
        public void TestParseDouble()
        {
            Assertions.AreApproximatelyEqual(3.946, ExifParser.ParseRationalOrDouble("3.946"));
        }
    }
}
