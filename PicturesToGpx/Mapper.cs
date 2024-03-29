﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
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
        private readonly Bitmap bitmap;
        private readonly double unitsPerPixelWidth;
        private readonly double unitsPerPixelHeight;
        private readonly Graphics graphics;
        private readonly Dictionary<Color, Pen> pens = new Dictionary<Color, Pen>();
        private bool disposed;
        private Font drawFont;
        private SolidBrush drawBrush;
        private byte[] stash = null;

        public Mapper(int width, int height, BoundingBox boundingBox, TilerConfig config)
        {
            this.width = width;
            this.height = height;
            this.boundingBox = boundingBox;
            this.config = config;
            bitmap = new Bitmap(width, height);
            unitsPerPixelWidth = (boundingBox.MaxLongitude - boundingBox.MinLongitude) / width;
            unitsPerPixelHeight = (boundingBox.MaxLatitude - boundingBox.MinLatitude) / height;
            graphics = Graphics.FromImage(bitmap);
            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            drawFont = new Font("Arial", height / 70);
            drawBrush = new SolidBrush(Color.Black);
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
            var pen = GetPen(color);
            graphics.DrawLine(pen,
                new Point(GetX(position1), GetY(position1)),
                new Point(GetX(position2), GetY(position2)));
        }

        private Pen GetPen(Color color)
        {
            Pen pen;
            if(pens.TryGetValue(color, out pen))
            {
                return pen;
            }

            pen = new Pen(color, 5);
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
                foreach (var pen in pens)
                {
                    pen.Value.Dispose();
                }
                pens.Clear();
                drawFont.Dispose();
                drawBrush.Dispose();
            }

            disposed = true;
        }

        // Converts positions from mercator to pixels in the map
        internal IEnumerable<Position> GetPixels(List<Position> points)
        {
            return points.Select(p => new Position(p.Time, GetY(p.Latitude), GetX(p.Longitude), PositionUnit.Pixel, p));
        }

        internal byte[] GetBitmap()
        {
            BitmapData bitmapData = null;

            try
            {
                bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, bitmap.PixelFormat);
                int numbytes = bitmapData.Stride * bitmap.Height;
                var buffer = new byte[numbytes];
                // byte[] bytedata = new byte[numbytes];

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

        internal void WriteText(string text, int y = 0)
        {
            graphics.DrawString(text, drawFont, drawBrush, 0, y);
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
        ///  we need to be able to get real lat longs back. This is far from ideal, but an approx measurement like that will have to do.
        /// </summary>
        internal Position FromPixelsToMercator(Position position)
        {
            Trace.Assert(position.Unit == PositionUnit.Pixel);
            return new Position(position.Time,
                unitsPerPixelHeight * position.Latitude + this.boundingBox.MinLatitude,
                unitsPerPixelWidth * position.Longitude + this.boundingBox.MinLongitude,
                PositionUnit.Mercator,
                position);
        }
    }
}