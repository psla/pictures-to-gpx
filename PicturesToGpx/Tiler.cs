using System;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Runtime;
using System.Threading;

namespace PicturesToGpx
{
    internal class Tiler
    {
        private const int TileWidthHeight = 256;
        private const int MaxZoomLevel = 19;

        // lyrs=h/m/
        //h = roads only
        //m = standard roadmap
        //p = terrain
        //r = somehow altered roadmap
        //s = satellite only
        //t = terrain only
        //y = hybrid
        private static readonly string GoogleMapsUrl = "http://mt1.google.com/vt/lyrs=m&x={0}&y={1}&z={2}";

        private static volatile Fetcher fetcher = new Fetcher();

        public static void SetFetcherPath(string path)
        {
            if (!Directory.Exists(path))
            {
                throw new DirectoryNotFoundException(path);
            }

            fetcher = new Fetcher(path);
        }

        internal static Mapper RenderMap(BoundingBox boundingBox, int widthPx, int heightPx)
        {
            var sw = new Stopwatch();
            sw.Start();
            try
            {
                return RenderMapInternal(boundingBox, widthPx, heightPx);
            }
            finally
            {
                sw.Stop();
                Console.WriteLine("Fetched tiles: {0}ms", sw.ElapsedMilliseconds);
            }
        }

        internal static Mapper RenderMapInternal(BoundingBox boundingBox, int widthPx, int heightPx)
        {
            var zoomLevel = LocationUtils.GetZoomLevel(boundingBox, widthPx, heightPx);
            zoomLevel = Math.Min(zoomLevel, MaxZoomLevel);
            Console.WriteLine("Desired zoomlevel: {0}", zoomLevel);

            var midX = LocationUtils.GetX(zoomLevel, boundingBox.MiddleLongitude);
            var midY = LocationUtils.GetY(zoomLevel, boundingBox.MiddleLatitude);

            var unitsPerPixel = LocationUtils.GetUnitsPerPixel(zoomLevel);
            var noOfTilesPerWidth = (widthPx - 1) / TileWidthHeight + 2; // + 1 for rounding error, +2 because the tile may lie outside of the screen, we need at least 1 full tile on each side.
            var noOfTilesPerHeight = (heightPx - 1) / TileWidthHeight + 2;

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
                        // Console.WriteLine(GoogleMapsUrl, x, y, zoomLevel);
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
}