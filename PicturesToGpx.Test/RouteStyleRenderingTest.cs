using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;

namespace PicturesToGpx.Test
{
    [TestClass]
    public class RouteStyleRenderingTest
    {
        [TestMethod]
        public void RouteStyleSettings_HasSensibleDefaults()
        {
            var style = new Settings.RouteStyleSettings();
            Assert.AreEqual(5.0f, style.LineWidth);
            Assert.IsTrue(style.EnableCasing);
            Assert.AreEqual(8.0f, style.CasingWidth);
            Assert.AreEqual("#B014181C", style.CasingColor);
            Assert.IsTrue(style.EnableShadow);
            Assert.AreEqual(11.0f, style.ShadowWidth);
            Assert.AreEqual("#30000000", style.ShadowColor);
            Assert.IsTrue(style.EnableLeadingDot);
            Assert.AreEqual(6.0f, style.LeadingDotRadius);
        }

        [TestMethod]
        public void Mapper_GetPointF_ComputesSubpixelFloatingPointCoordinates()
        {
            var bbox = new BoundingBox(0, 0, 100, 100);
            using (var mapper = new Mapper(1000, 1000, bbox, new TilerConfig()))
            {
                var p = new Position(DateTimeOffset.UtcNow, 25.5, 50.25, PositionUnit.Mercator);
                PointF pt = mapper.GetPointF(p);

                // X = (50.25 - 0) / (100 / 1000) = 502.5
                // Y = 1000 - (25.5 - 0) / (100 / 1000) = 745.0
                Assert.AreEqual(502.5f, pt.X, 0.001f);
                Assert.AreEqual(745.0f, pt.Y, 0.001f);
            }
        }

        [TestMethod]
        public void Mapper_DrawLine_DrawsMultiLayerStrokeWithoutError()
        {
            var bbox = new BoundingBox(0, 0, 100, 100);
            var style = new Settings.RouteStyleSettings
            {
                EnableShadow = true,
                EnableCasing = true,
                LineWidth = 5.0f
            };

            using (var mapper = new Mapper(200, 200, bbox, new TilerConfig(), style))
            {
                var p1 = new Position(DateTimeOffset.UtcNow, 10.0, 10.0, PositionUnit.Pixel);
                var p2 = new Position(DateTimeOffset.UtcNow.AddSeconds(1), 100.0, 100.0, PositionUnit.Pixel);

                mapper.DrawLine(p1, p2, Color.Red);

                byte[] bmpData = mapper.GetBitmap();
                Assert.IsNotNull(bmpData);
                Assert.IsTrue(bmpData.Length > 0);
            }
        }

        [TestMethod]
        public void Mapper_DrawLeadingDot_RendersGlowingMarker()
        {
            var bbox = new BoundingBox(0, 0, 100, 100);
            using (var mapper = new Mapper(200, 200, bbox, new TilerConfig()))
            {
                var p = new Position(DateTimeOffset.UtcNow, 100.0, 100.0, PositionUnit.Pixel);
                mapper.DrawLeadingDot(p, Color.Cyan);

                byte[] bmpData = mapper.GetBitmap();
                Assert.IsNotNull(bmpData);
                Assert.IsTrue(bmpData.Length > 0);
            }
        }

        [TestMethod]
        public void Mapper_DrawLeadingDot_RepeatedCallsWithMultipleColors_ExecutesSuccessfully()
        {
            var bbox = new BoundingBox(0, 0, 100, 100);
            using (var mapper = new Mapper(200, 200, bbox, new TilerConfig()))
            {
                var colors = new[] { Color.Red, Color.Blue, Color.Green, Color.Orange, Color.Purple };
                for (int i = 0; i < 50; i++)
                {
                    var p = new Position(DateTimeOffset.UtcNow.AddSeconds(i), 10.0 + i, 10.0 + i, PositionUnit.Pixel);
                    mapper.DrawLeadingDot(p, colors[i % colors.Length]);
                }

                byte[] bmpData = mapper.GetBitmap();
                Assert.IsNotNull(bmpData);
                Assert.IsTrue(bmpData.Length > 0);
            }
        }

        [TestMethod]
        public void Mapper_GetPixels_PreservesSubpixelPrecision()
        {
            var bbox = new BoundingBox(0, 0, 100, 100);
            using (var mapper = new Mapper(1000, 1000, bbox, new TilerConfig()))
            {
                var raw = new List<Position>
                {
                    new Position(DateTimeOffset.UtcNow, 50.12345, 50.67891, PositionUnit.Mercator)
                };

                var pixelPositions = new List<Position>(mapper.GetPixels(raw));
                Assert.AreEqual(1, pixelPositions.Count);
                Assert.AreEqual(PositionUnit.Pixel, pixelPositions[0].Unit);

                // Longitude (x in pixels) should retain decimal precision, not truncated to int
                Assert.AreEqual(506.7891, pixelPositions[0].Longitude, 0.001);
            }
        }
    }
}
