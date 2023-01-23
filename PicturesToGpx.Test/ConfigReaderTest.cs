using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PicturesToGpx.Test
{
    [TestClass]
    public class ConfigReaderTest
    {
        [TestMethod]
        public void MultipleJsonFiles()
        {
            Settings settings = ConfigReader.ReadConfig(@"Configs/MultipleGoogleTimelines.txt");
            Assert.IsNotNull(settings);
            CollectionAssert.AreEqual(new[] {
                "U:\\wspolne\\GPS\\2022-12\\piotr-takeout-20230123T032835Z-001\\Takeout\\Location History\\Records.json",
                "U:\\wspolne\\GPS\\2022-12\\ola-takeout-20230123T032033Z-001\\Takeout\\Location History\\Records.json", 
            }, settings.GetGoogleTimelineFiles());
        }
    }
}
