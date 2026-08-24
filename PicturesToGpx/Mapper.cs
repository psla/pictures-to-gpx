using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;

namespace PicturesToGpx
{
    internal class Mapper : IDisposable
    {
        private readonly int width;
        private readonly int height;
        private readonly BoundingBox boundingBox;
        private readonly TilerConfig config;
        private readonly Settings.RouteStyleSettings style;
        private readonly Bitmap bitmap;
        private readonly double unitsPerPixelWidth;
        private readonly double unitsPerPixelHeight;
        private readonly Graphics graphics;
        private readonly Dictionary<Color, Pen> pens = new Dictionary<Color, Pen>();
        private Pen shadowPen;
        private Pen casingPen;
        private bool disposed;
        private byte[] stash = null;

        public Graphics Graphics => graphics;
        public int Width => width;
        public int Height => height;
        public Settings.RouteStyleSettings Style => style;

        public Mapper(int width, int height, BoundingBox boundingBox, TilerConfig config, Settings.RouteStyleSettings style = null)
        {
            this.width = width;
            this.height = height;
            this.boundingBox = boundingBox;
            this.config = config ?? new TilerConfig();
            this.style = style ?? new Settings.RouteStyleSettings();
            bitmap = new Bitmap(width, height);
            unitsPerPixelWidth = (boundingBox.MaxLongitude - boundingBox.MinLongitude) / width;
            unitsPerPixelHeight = (boundingBox.MaxLatitude - boundingBox.MinLatitude) / height;
            graphics = Graphics.FromImage(bitmap);
            graphics.SmoothingMode = SmoothingMode.HighQuality;
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            graphics.CompositingQuality = CompositingQuality.HighQuality;
        }

        public PointF GetPointF(Position position)
        {
            if (position.Unit == PositionUnit.Pixel)
            {
                return new PointF((float)position.Longitude, (float)position.Latitude);
            }

            float x = (float)((position.Longitude - boundingBox.MinLongitude) / unitsPerPixelWidth);
            float y = (float)(height - ((position.Latitude - boundingBox.MinLatitude) / unitsPerPixelHeight));
            return new PointF(x, y);
        }

        private int GetX(double longitude)
        {
            return (int)((longitude - boundingBox.MinLongitude) / unitsPerPixelWidth);
        }

        private int GetX(Position position)
        {
            if (position.Unit == PositionUnit.Pixel)
            {
                return (int)position.Longitude;
            }

            return GetX(position.Longitude);
        }

        private int GetY(Position position)
        {
            if (position.Unit == PositionUnit.Pixel)
            {
                return (int)position.Latitude;
            }

            return GetY(position.Latitude);
        }

        private int GetY(double latitude)
        {
            return height - (int)((latitude - boundingBox.MinLatitude) / unitsPerPixelHeight);
        }

        public void DrawTile(BoundingBox boundingBox, Bitmap b)
        {
            int x = GetX(boundingBox.MinLongitude);
            int y = GetY(boundingBox.MinLatitude);

            if (config.DrawTilesBoundingBox)
            {
                DrawBoundingBox(boundingBox);
            }

            graphics.DrawImage(b, x, y);
        }

        public void Save(string path)
        {
            bitmap.Save(path);
        }

        internal void DrawLine(Position position1, Position position2, Color color)
        {
            var pt1 = GetPointF(position1);
            var pt2 = GetPointF(position2);

            // Layer 1: Ambient Drop Shadow
            if (style.EnableShadow)
            {
                var shadow = GetShadowPen();
                if (shadow != null)
                {
                    graphics.DrawLine(shadow, pt1, pt2);
                }
            }

            // Layer 2: Contrast Casing / Border
            if (style.EnableCasing)
            {
                var casing = GetCasingPen();
                if (casing != null)
                {
                    graphics.DrawLine(casing, pt1, pt2);
                }
            }

            // Layer 3: Vibrant Core Route
            var corePen = GetCorePen(color);
            graphics.DrawLine(corePen, pt1, pt2);
        }

        internal void DrawLeadingDot(Position position, Color coreColor)
        {
            if (!style.EnableLeadingDot)
            {
                return;
            }

            var pt = GetPointF(position);
            float r = style.LeadingDotRadius;

            // 1. Soft outer shadow halo
            using (var haloBrush = new SolidBrush(Color.FromArgb(70, 0, 0, 0)))
            {
                graphics.FillEllipse(haloBrush, pt.X - (r + 3.0f), pt.Y - (r + 3.0f), (r + 3.0f) * 2.0f, (r + 3.0f) * 2.0f);
            }

            // 2. Crisp outer casing / white ring
            using (var whiteBrush = new SolidBrush(Color.White))
            {
                graphics.FillEllipse(whiteBrush, pt.X - (r + 1.0f), pt.Y - (r + 1.0f), (r + 1.0f) * 2.0f, (r + 1.0f) * 2.0f);
            }

            // 3. Vibrant inner day color core
            using (var coreBrush = new SolidBrush(coreColor))
            {
                graphics.FillEllipse(coreBrush, pt.X - (r - 1.5f), pt.Y - (r - 1.5f), (r - 1.5f) * 2.0f, (r - 1.5f) * 2.0f);
            }

            // 4. Specular center highlight
            using (var centerHighlight = new SolidBrush(Color.FromArgb(220, 255, 255, 255)))
            {
                graphics.FillEllipse(centerHighlight, pt.X - 1.5f, pt.Y - 1.5f, 3.0f, 3.0f);
            }
        }

        private Pen GetShadowPen()
        {
            if (shadowPen != null)
            {
                return shadowPen;
            }

            try
            {
                Color color = ColorTranslator.FromHtml(style.ShadowColor);
                shadowPen = new Pen(color, style.ShadowWidth)
                {
                    StartCap = LineCap.Round,
                    EndCap = LineCap.Round,
                    LineJoin = LineJoin.Round
                };
            }
            catch
            {
                shadowPen = new Pen(Color.FromArgb(48, 0, 0, 0), style.ShadowWidth)
                {
                    StartCap = LineCap.Round,
                    EndCap = LineCap.Round,
                    LineJoin = LineJoin.Round
                };
            }

            return shadowPen;
        }

        private Pen GetCasingPen()
        {
            if (casingPen != null)
            {
                return casingPen;
            }

            try
            {
                Color color = ColorTranslator.FromHtml(style.CasingColor);
                casingPen = new Pen(color, style.CasingWidth)
                {
                    StartCap = LineCap.Round,
                    EndCap = LineCap.Round,
                    LineJoin = LineJoin.Round
                };
            }
            catch
            {
                casingPen = new Pen(Color.FromArgb(176, 20, 24, 28), style.CasingWidth)
                {
                    StartCap = LineCap.Round,
                    EndCap = LineCap.Round,
                    LineJoin = LineJoin.Round
                };
            }

            return casingPen;
        }

        private Pen GetCorePen(Color color)
        {
            Pen pen;
            if (pens.TryGetValue(color, out pen))
            {
                return pen;
            }

            pen = new Pen(color, style.LineWidth)
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round,
                LineJoin = LineJoin.Round
            };
            pens[color] = pen;
            return pen;
        }

        private void DrawBoundingBox(BoundingBox boundingBox)
        {
            DrawLine(boundingBox.LowerLeft, boundingBox.UpperLeft, Color.Red);
            DrawLine(boundingBox.UpperLeft, boundingBox.UpperRight, Color.Red);
            DrawLine(boundingBox.UpperRight, boundingBox.LowerRight, Color.Red);
            DrawLine(boundingBox.LowerRight, boundingBox.LowerLeft, Color.Red);
        }

        // Public implementation of Dispose pattern callable by consumers.
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // Protected implementation of Dispose pattern.
        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
            {
                return;
            }

            if (disposing)
            {
                if (shadowPen != null)
                {
                    shadowPen.Dispose();
                    shadowPen = null;
                }
                if (casingPen != null)
                {
                    casingPen.Dispose();
                    casingPen = null;
                }
                foreach (var pen in pens)
                {
                    pen.Value.Dispose();
                }
                pens.Clear();
                graphics.Dispose();
                bitmap.Dispose();
            }

            disposed = true;
        }

        // Converts positions from mercator to subpixel positions on the canvas
        internal IEnumerable<Position> GetPixels(List<Position> points)
        {
            return points.Select(p =>
            {
                double x = (p.Longitude - boundingBox.MinLongitude) / unitsPerPixelWidth;
                double y = height - ((p.Latitude - boundingBox.MinLatitude) / unitsPerPixelHeight);
                return new Position(p.Time, y, x, PositionUnit.Pixel, p);
            });
        }

        internal byte[] GetBitmap()
        {
            BitmapData bitmapData = null;

            try
            {
                bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, bitmap.PixelFormat);
                int numbytes = bitmapData.Stride * bitmap.Height;
                var buffer = new byte[numbytes];

                Marshal.Copy(bitmapData.Scan0, buffer, 0, buffer.Length);

                return buffer;
            }
            finally
            {
                if (bitmapData != null)
                {
                    bitmap.UnlockBits(bitmapData);
                }
            }
        }

        /// <summary>
        /// Stashes the current bitmap so that it can be restored.
        /// E.g. one can draw text (e.g. distance) and render the frame, and then restore the frame to the previous status.
        /// </summary>
        /// <remarks>
        /// Stash, as implemented today, overrides any existing stash.
        /// </remarks>
        internal void Stash()
        {
            stash = GetBitmap();
        }

        internal bool IsStashed => stash != null;

        /// <summary>
        ///  If stash is present, it pops the stash
        /// </summary>
        internal void StashPop()
        {
            BitmapData bitmapData = null;
            try
            {
                bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.WriteOnly, bitmap.PixelFormat);
                Marshal.Copy(stash, 0, bitmapData.Scan0, stash.Length);
            }
            finally
            {
                if (bitmapData != null)
                {
                    bitmap.UnlockBits(bitmapData);
                }
            }
        }

        /// <summary>
        ///  Once the point is converted to pixels, and then interpolated based on pixels (instead of interpolated based on mercator),
        ///  we need to be able to get real lat longs back.
        /// </summary>
        internal Position FromPixelsToMercator(Position position)
        {
            Trace.Assert(position.Unit == PositionUnit.Pixel);
            return new Position(position.Time,
                boundingBox.MinLatitude + (height - position.Latitude) * unitsPerPixelHeight,
                unitsPerPixelWidth * position.Longitude + this.boundingBox.MinLongitude,
                PositionUnit.Mercator,
                position);
        }
    }
}