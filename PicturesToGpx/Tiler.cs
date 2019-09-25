using System;

namespace PicturesToGpx
{
    internal class Tiler
    {

        // lyrs=h/m/
        //h = roads only
        //m = standard roadmap
        //p = terrain
        //r = somehow altered roadmap
        //s = satellite only
        //t = terrain only
        //y = hybrid
        private static readonly string GoogleMapsUrl = "http://mt1.google.com/vt/lyrs=m&x={0}&y={1}&z={2}";
        private static readonly string BlackTile = "BLACKTILE";


        internal static void RenderEmptyMap(BoundingBox boundingBox, string outgoingPicturePath, int widthPx, int heightPx)
        {
            var zoomLevel = LocationUtils.GetZoomLevel(boundingBox);
            Console.WriteLine("Desired zoomlevel: {0}", zoomLevel);

            var midX = LocationUtils.GetX(zoomLevel, boundingBox.MiddleLatitude);
            var midY = LocationUtils.GetX(zoomLevel, boundingBox.MiddleLongitude);

            var noOfTilesPerWidth = (widthPx - 1) / 256 + 1;
            var noOfTilesPerHeight = (heightPx - 1) / 256 + 1;
            for (int y = midY - noOfTilesPerHeight / 2; y <= midY + noOfTilesPerHeight / 2; y++)
            {
                for (int x = midX - noOfTilesPerWidth / 2; x <= midX + noOfTilesPerWidth / 2; x++)
                {
                    // or too far right. TODO
                    if (y < 0 || x < 0)
                    {
                        Console.WriteLine(BlackTile);
                    }
                    Console.WriteLine(GoogleMapsUrl, x, y, zoomLevel);
                }
            }
        }
    }
}