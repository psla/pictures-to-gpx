using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;

namespace PicturesToGpx
{
    internal class Tiler
    {
        private const int TileWidthHeight = 256;

        // lyrs=h/m/
        //h = roads only
        //m = standard roadmap
        //p = terrain
        //r = somehow altered roadmap
        //s = satellite only
        //t = terrain only
        //y = hybrid
        private static readonly string GoogleMapsUrl = "http://mt1.google.com/vt/lyrs=m&x={0}&y={1}&z={2}";

        private static readonly Fetcher fetcher = new Fetcher();

        internal static Mapper RenderMap(BoundingBox boundingBox, int widthPx, int heightPx)
        {
            var zoomLevel = LocationUtils.GetZoomLevel(boundingBox, widthPx, heightPx);
            Console.WriteLine("Desired zoomlevel: {0}", zoomLevel);

            var midX = LocationUtils.GetX(zoomLevel, boundingBox.MiddleLongitude);
            var midY = LocationUtils.GetY(zoomLevel, boundingBox.MiddleLatitude);

            var unitsPerPixel = LocationUtils.GetUnitsPerPixel(zoomLevel);
            var noOfTilesPerWidth = (widthPx - 1) / TileWidthHeight + 1;
            var noOfTilesPerHeight = (heightPx - 1) / TileWidthHeight + 1;

            var mapper = new Mapper(widthPx, heightPx,
                new BoundingBox(
                    boundingBox.MiddleLatitude - unitsPerPixel * heightPx / 2,
                    boundingBox.MiddleLongitude - unitsPerPixel * widthPx / 2,
                    boundingBox.MiddleLatitude + unitsPerPixel * heightPx / 2,
                    boundingBox.MiddleLongitude + unitsPerPixel * widthPx / 2
                ), new TilerConfig());
            for (int y = midY - noOfTilesPerHeight / 2; y <= midY + noOfTilesPerHeight / 2; y++)
            {
                for (int x = midX - noOfTilesPerWidth / 2; x <= midX + noOfTilesPerWidth / 2; x++)
                {
                    // or too far right. TODO
                    if (y < 0 || x < 0 || x >= Math.Pow(2, zoomLevel) || y >= Math.Pow(2, zoomLevel))
                    {
                        Console.WriteLine(Fetcher.BlackTile);
                    }
                    else
                    {
                        Console.WriteLine(GoogleMapsUrl, x, y, zoomLevel);
                        using (Bitmap b = fetcher.Fetch(string.Format(CultureInfo.InvariantCulture, GoogleMapsUrl, x, y, zoomLevel)))
                        {
                            mapper.DrawTile(LocationUtils.GetBoundingBox(x, y, zoomLevel), b);
                        }
                    }
                }
            }

            return mapper;
        }
    }

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
        private readonly Pen pen = new Pen(Color.Red, 5);
        private bool disposed;

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
                DrawLine(boundingBox.UpperLeft, boundingBox.UpperRight);
                DrawLine(boundingBox.UpperRight, boundingBox.LowerRight);
                DrawLine(boundingBox.LowerRight, boundingBox.LowerLeft);
                DrawLine(boundingBox.LowerLeft, boundingBox.UpperLeft);
            }

            graphics.DrawImage(b, x, y);
        }

        public void Save(string path)
        {
            bitmap.Save(path);
        }

        internal void DrawLine(Position position1, Position position2)
        {
            graphics.DrawLine(pen,
                new Point(GetX(position1), GetY(position1)),
                new Point(GetX(position2), GetY(position2)));
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
                pen.Dispose();
            }

            disposed = true;
        }

        // Converts positions from mercator to pixels in the map
        internal IEnumerable<Position> GetPixels(List<Position> points)
        {
            return points.Select(p => new Position(p.Time, GetY(p.Latitude), GetX(p.Longitude), PositionUnit.Pixel));
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
    }
}