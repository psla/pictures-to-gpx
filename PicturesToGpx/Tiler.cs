using System;
using System.Drawing;
using System.Globalization;

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

        internal static void RenderEmptyMap(BoundingBox boundingBox, string outgoingPicturePath, int widthPx, int heightPx)
        {
            var zoomLevel = LocationUtils.GetZoomLevel(boundingBox);
            Console.WriteLine("Desired zoomlevel: {0}", zoomLevel);

            var midX = LocationUtils.GetX(zoomLevel, boundingBox.MiddleLatitude);
            var midY = LocationUtils.GetX(zoomLevel, boundingBox.MiddleLongitude);

            var unitsPerPixel = LocationUtils.GetUnitsPerPixel(zoomLevel);
            var noOfTilesPerWidth = (widthPx - 1) / TileWidthHeight + 1;
            var noOfTilesPerHeight = (heightPx - 1) / TileWidthHeight + 1;

            var mapper = new Mapper(widthPx, heightPx,
                new BoundingBox(
                    boundingBox.MiddleLatitude - unitsPerPixel * heightPx / 2,
                    boundingBox.MiddleLongitude - unitsPerPixel * widthPx / 2,
                    boundingBox.MiddleLatitude + unitsPerPixel * heightPx / 2,
                    boundingBox.MiddleLongitude + unitsPerPixel * widthPx / 2
                ));
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

            mapper.Save(@"F:\tmp\map.png");
        }
    }

    internal class Mapper
    {
        private readonly int width;
        private readonly int height;
        private readonly BoundingBox boundingBox;
        private readonly Bitmap bitmap;
        private readonly double unitsPerPixelWidth;
        private readonly double unitsPerPixelHeight;
        private readonly Graphics graphics;

        public Mapper(int width, int height, BoundingBox boundingBox)
        {
            this.width = width;
            this.height = height;
            this.boundingBox = boundingBox;

            bitmap = new Bitmap(width, height);
            unitsPerPixelWidth = (boundingBox.MaxLongitude - boundingBox.MinLongitude) / width;
            unitsPerPixelHeight = (boundingBox.MaxLatitude - boundingBox.MinLatitude) / height;
            graphics = Graphics.FromImage(bitmap);
        }

        public void DrawTile(BoundingBox boundingBox, Bitmap b)
        {
            int x = (int)((boundingBox.MinLongitude - this.boundingBox.MinLongitude) / unitsPerPixelWidth);
            int y = (int)((boundingBox.MinLatitude - this.boundingBox.MinLatitude) / unitsPerPixelHeight);
            graphics.DrawImage(b, x - b.Width, y - b.Height);
        }

        public void Save(string path)
        {
            bitmap.Save(path);
        }
    }
}