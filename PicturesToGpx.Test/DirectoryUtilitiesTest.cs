using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;

namespace PicturesToGpx.Test
{
    [TestClass]
    public class DirectoryUtilitiesTest
    {
        [TestMethod]
        public void FindPointsForFiles_WithOnlyFitFile_ReturnsPositions()
        {
            using (var tempDir = new TempDirectory("Fit_"))
            {
                string targetFit = Path.Combine(tempDir, "sample.fit");
                File.Copy(@"gps\490518450.fit", targetFit);

                var results = DirectoryUtilities.FindPointsForFiles(tempDir).ToList();

                Assert.AreEqual(1, results.Count);
                Assert.AreEqual(targetFit, results[0].Filename);
                Assert.AreEqual(2523, results[0].Positions.Count);
                Assert.AreEqual(47.6047248455718, results[0].Positions[0].Latitude, 0.00001);
                Assert.AreEqual(-122.149795263621, results[0].Positions[0].Longitude, 0.00001);
                Assert.AreEqual(new DateTimeOffset(2015, 11, 22, 18, 57, 04, TimeSpan.Zero), results[0].Positions[0].Time);
            }
        }

        [TestMethod]
        public void FindPointsForFiles_WithOnlyFitGzFile_ReturnsPositions()
        {
            using (var tempDir = new TempDirectory("FitGz_"))
            {
                string targetFitGz = Path.Combine(tempDir, "sample.fit.gz");
                File.Copy(@"gps\490518450.fit.gz", targetFitGz);

                var results = DirectoryUtilities.FindPointsForFiles(tempDir).ToList();

                Assert.AreEqual(1, results.Count);
                Assert.AreEqual(targetFitGz, results[0].Filename);
                Assert.AreEqual(2523, results[0].Positions.Count);
            }
        }

        [TestMethod]
        public void FindPointsForFiles_WithBothFitAndFitGzAndSubdirectories_ReturnsAll()
        {
            using (var tempDir = new TempDirectory("Mixed_"))
            {
                string subDir = tempDir.CreateSubdirectory("sub");
                string fitFile = Path.Combine(tempDir, "sample.fit");
                string fitGzFile = Path.Combine(subDir, "sample.fit.gz");
                string ignoredFile = Path.Combine(tempDir, "readme.txt");

                File.Copy(@"gps\490518450.fit", fitFile);
                File.Copy(@"gps\490518450.fit.gz", fitGzFile);
                File.WriteAllText(ignoredFile, "test data");

                var results = DirectoryUtilities.FindPointsForFiles(tempDir).ToList();

                Assert.AreEqual(2, results.Count);
                Assert.IsTrue(results.Any(r => r.Filename == fitFile && r.Positions.Count == 2523));
                Assert.IsTrue(results.Any(r => r.Filename == fitGzFile && r.Positions.Count == 2523));
            }
        }
    }
}
