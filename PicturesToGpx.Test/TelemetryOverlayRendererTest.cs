using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace PicturesToGpx.Test
{
    [TestClass]
    public class TelemetryOverlayRendererTest
    {
        private const string GoldenFileName = "TelemetryBar_Golden.png";

        [TestMethod]
        public void DrawBottomBar_WithNullOrEmptyValues_DoesNotCrash()
        {
            // Null graphics
            TelemetryOverlayRenderer.DrawBottomBar(null, 1920, 1080, DateTimeOffset.Now, 1000);

            // Zero dimensions
            using (var bmp = new Bitmap(100, 100))
            using (var g = Graphics.FromImage(bmp))
            {
                TelemetryOverlayRenderer.DrawBottomBar(g, 0, 0, DateTimeOffset.Now, 1000);
                TelemetryOverlayRenderer.DrawBottomBar(g, 100, 100, null, null, null, null);
            }
        }

        [TestMethod]
        public void DrawBottomBar_DrawsOntoBitmapSuccessfully()
        {
            int width = 800;
            int height = 450;
            using (var bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb))
            using (var g = Graphics.FromImage(bmp))
            {
                // Clear with solid background
                g.Clear(Color.CornflowerBlue);

                var testTime = new DateTimeOffset(2026, 7, 21, 16, 45, 0, TimeSpan.FromHours(2));
                TelemetryOverlayRenderer.DrawBottomBar(g, width, height, testTime, 673000, Color.Red, "Day 3");

                // Verify bottom region has been modified
                int barHeight = Math.Max(40, (int)(height * 0.05));
                int barY = height - barHeight + 5;
                Color pixelInBar = bmp.GetPixel(width / 2, barY);

                // Pixel in the bar should not be pure CornflowerBlue
                Assert.AreNotEqual(Color.CornflowerBlue.ToArgb(), pixelInBar.ToArgb(), "The telemetry bar region should be drawn over the background.");
            }
        }

        [TestMethod]
        public void DrawBottomBar_MatchesGoldenScreenshot()
        {
            int width = 1280;
            int height = 720;

            using (var actualBmp = new Bitmap(width, height, PixelFormat.Format32bppArgb))
            using (var g = Graphics.FromImage(actualBmp))
            {
                g.Clear(Color.FromArgb(240, 240, 240));

                var testTime = new DateTimeOffset(2026, 7, 21, 16, 45, 0, TimeSpan.FromHours(2));
                TelemetryOverlayRenderer.DrawBottomBar(g, width, height, testTime, 673000, Color.Red, "Day 3");

                string goldenPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Golden", GoldenFileName);
                if (!File.Exists(goldenPath))
                {
                    // If running in development source directory
                    goldenPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "Golden", GoldenFileName);
                }

                // If golden file does not exist yet, save the baseline
                if (!File.Exists(goldenPath))
                {
                    string targetDir = Path.GetDirectoryName(goldenPath);
                    if (!Directory.Exists(targetDir))
                    {
                        Directory.CreateDirectory(targetDir);
                    }
                    actualBmp.Save(goldenPath, ImageFormat.Png);
                    Assert.Inconclusive("Golden screenshot did not exist. Created baseline at: " + goldenPath);
                    return;
                }

                using (var goldenBmp = new Bitmap(goldenPath))
                {
                    Assert.AreEqual(goldenBmp.Width, actualBmp.Width, "Width mismatch with golden screenshot.");
                    Assert.AreEqual(goldenBmp.Height, actualBmp.Height, "Height mismatch with golden screenshot.");

                    int mismatchedPixels = 0;
                    int totalPixels = width * height;

                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            Color expected = goldenBmp.GetPixel(x, y);
                            Color actual = actualBmp.GetPixel(x, y);

                            if (expected.ToArgb() != actual.ToArgb())
                            {
                                mismatchedPixels++;
                            }
                        }
                    }

                    // Strict pixel comparison: allow up to 0.05% difference for minor subpixel font anti-aliasing variations across environments
                    double mismatchRatio = (double)mismatchedPixels / totalPixels;
                    Assert.IsTrue(mismatchRatio <= 0.0005,
                        string.Format("Screenshot test failed. Mismatched pixels: {0}/{1} ({2:P3}).", mismatchedPixels, totalPixels, mismatchRatio));
                }
            }
        }
    }
}
