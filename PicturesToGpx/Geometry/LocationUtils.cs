using System;
using System.Collections.Generic;
using System.Linq;

namespace PicturesToGpx
{
    [System.Runtime.InteropServices.Guid("40BA4066-2DB3-420F-B1AF-AB0ECD4BD625")]
    public static class LocationUtils
    {
        private const int TileWidth = 256;
        private const int TileHeight = 256;

        private const double RADIUS = 6378137.0; /* in meters on the equator */
        private const double CIRCUMFERENCE = 2 * Math.PI * RADIUS; /* in meters on the equator */

        private const double MetersPerTileAtZeroWidth = CIRCUMFERENCE;
        private const double MetersPerTileAtZeroHeight = (85.05 / 90) * CIRCUMFERENCE;

        public static Position ToMercator(Position position)
        {
            return new Position(position.Time, lat2y(position.Latitude), lon2x(position.Longitude));
        }

        // This should really be injectable, because this is Google specific ;)
        //
        // 0,0; 1,0; 2,0; 3,0
        // 0,1; 1,1; 2,1; 3,1
        //
        //
        internal static int GetX(int zoomLevel, double longitude)
        {
            return (int)((longitude + CIRCUMFERENCE / 2) / (CIRCUMFERENCE / Math.Pow(2, zoomLevel)));
        }

        internal static double GetUnitsPerPixel(int zoomLevel)
        {
            return CIRCUMFERENCE / Math.Pow(2, zoomLevel) / 256;
        }

        internal static object TilesPerZoomlevel(int zoomLevel, int widthPx)
        {
            throw new NotImplementedException();
        }

        internal static int GetY(int zoomLevel, double latitude)
        {
            return (int)((CIRCUMFERENCE / 2 - latitude) / (CIRCUMFERENCE / Math.Pow(2, zoomLevel)));
        }

        public static double y2lat(double aY)
        {
            return ToDegrees(Math.Atan(Math.Exp(aY / RADIUS)) * 2 - Math.PI / 2);
        }
        public static double x2lon(double aX)
        {
            return ToDegrees(aX / RADIUS);
        }

        /* These functions take their angle parameter in degrees and return a length in meters */

        public static double lat2y(double aLat)
        {
            // https://wiki.openstreetmap.org/wiki/Mercator
            return Math.Log(Math.Tan(Math.PI / 4 + ToRadians(aLat) / 2)) * RADIUS;
        }
        public static double lon2x(double aLong)
        {
            return ToRadians(aLong) * RADIUS;
        }

        private static double ToRadians(double degrees)
        {
            return (degrees / 180.0) * Math.PI;
        }

        private static double ToDegrees(double radians)
        {
            return radians * 180.0 / Math.PI;
        }

        internal static BoundingBox GetBoundingBox(int x, int y, int zoomLevel)
        {
            return new BoundingBox(
                CIRCUMFERENCE / 2 - (y) * 256 * GetUnitsPerPixel(zoomLevel),
                x * 256 * GetUnitsPerPixel(zoomLevel) - CIRCUMFERENCE / 2,
                CIRCUMFERENCE / 2 - (y - 1) * 256 * GetUnitsPerPixel(zoomLevel),
                (x + 1) * 256 * GetUnitsPerPixel(zoomLevel) - CIRCUMFERENCE / 2);
        }

        internal static BoundingBox GetBoundingBox(List<Position> points)
        {
            return new BoundingBox(points.Min(x => x.Latitude), points.Min(x => x.Longitude),
                points.Max(x => x.Latitude), points.Max(x => x.Longitude)
                );
        }

        /// <summary>
        /// Gets the optimal zoomelevel for the given width and height of the frame. Optimal means the largest zoomlevel in which the <paramref name="boundingBox"/> still fits.
        /// </summary>
        internal static int GetZoomLevel(BoundingBox boundingBox, int widthPx, int heightPx)
        {
            var noOfTilesWidth = (double)widthPx / TileWidth;
            var noOfTilesHeight = (double)heightPx / TileHeight;

            var width = (boundingBox.MaxLongitude - boundingBox.MinLongitude);
            double scaleFactorWidth = MetersPerTileAtZeroWidth / width * noOfTilesWidth;
            var widthZoomLevel = Math.Log(scaleFactorWidth, 2);
            var height = (boundingBox.MaxLatitude - boundingBox.MinLatitude);
            double scaleFactorHeight = MetersPerTileAtZeroHeight / height * noOfTilesHeight;
            var heightZoomLevel = Math.Log(scaleFactorHeight, 2);

            Console.WriteLine("Desired zoomlevel x={0} (sf={2}), y={1} (sf={3})", widthZoomLevel, heightZoomLevel, scaleFactorWidth, scaleFactorHeight);

            return (int)Math.Min(widthZoomLevel, heightZoomLevel);
        }
    }
}
