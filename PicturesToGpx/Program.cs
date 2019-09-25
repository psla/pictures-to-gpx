using MetadataExtractor;
using MKCoolsoft.GPXLib;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PicturesToGpx
{
    public static class LocationUtils
    {
        private const int TileWidth = 256;
        private const int TileHeight = 256;

        private const double RADIUS = 6378137.0; /* in meters on the equator */
        private const double CIRCUMFERENCE = 2 * Math.PI * RADIUS; /* in meters on the equator */
        private const int ScreenWidth = 1920;
        private const int ScreenHeight = 1080;

        private const double MetersPerTileAtZero = CIRCUMFERENCE;

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
                y * 256 * GetUnitsPerPixel(zoomLevel),
                x * 256 * GetUnitsPerPixel(zoomLevel) - CIRCUMFERENCE / 2,
                (y + 1) * 256 * GetUnitsPerPixel(zoomLevel),
                (x + 1) * 256 * GetUnitsPerPixel(zoomLevel) - CIRCUMFERENCE / 2);
        }

        internal static BoundingBox GetBoundingBox(List<Position> points)
        {
            return new BoundingBox(points.Min(x => x.Latitude), points.Min(x => x.Longitude),
                points.Max(x => x.Latitude), points.Max(x => x.Longitude)
                );
        }

        internal static int GetZoomLevel(BoundingBox boundingBox)
        {
            var noOfTilesWidth = Math.Ceiling((double)ScreenWidth / TileWidth);
            var noOfTilesHeight = Math.Ceiling((double)ScreenHeight / TileHeight);


            var width = (boundingBox.MaxLongitude - boundingBox.MinLongitude);
            var widthZoomLevel = Math.Log(MetersPerTileAtZero / width * noOfTilesWidth);
            var height = (boundingBox.MaxLatitude - boundingBox.MinLatitude);
            var heightZoomLevel = Math.Log(MetersPerTileAtZero / height * noOfTilesHeight);

            return (int)Math.Min(widthZoomLevel, heightZoomLevel);
        }
    }

    internal static class Program
    {
        private const string gpsFormat = "yyyy:MM:dd HH:mm:ss.fff UTC";
        private const string TileCache = @"G:\tmp\tile-cache";

        private static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                using (var se = new StreamWriter(System.Console.OpenStandardError()))
                {
                    se.WriteLine("Please provide a directory in which files exist");
                    return;
                }
            }
            var folder = args[0];
            if (!System.IO.Directory.Exists(folder))
            {
                using (var se = new StreamWriter(System.Console.OpenStandardError()))
                {
                    se.WriteLine("Provided directory doesn't exist");
                    return;
                }
            }
            // CreateGpxFromPicturesInFolder(folder);
            CreateMapFromPoints(@"F:\tmp\test-track2.json", @"F:\tmp\test-track2.png");
        }

        private static void CreateMapFromPoints(string pointPath, string outgoingPicturePath)
        {
            var points = JsonConvert.DeserializeObject<List<Position>>(File.ReadAllText(pointPath));

            points = points.Select(LocationUtils.ToMercator).ToList();
            var boundingBox = LocationUtils.GetBoundingBox(points);

            var zoomLevel = LocationUtils.GetZoomLevel(boundingBox);
            Console.WriteLine("Desired zoomlevel: {0}", zoomLevel);
            var mapper = Tiler.RenderMap(boundingBox, outgoingPicturePath, 1920, 1080);

            mapper.Save(@"F:\tmp\map.png");

            for (int i = 1; i < points.Count; i++)
            {
                mapper.DrawLine(points[i - 1], points[i]);
            }
            mapper.Save(@"F:\tmp\map2.png");
        }

        private static void CreateGpxFromPicturesInFolder(string folder)
        {

            // TODO: Multiple tracks, group by day (in a timezone)
            List<Position> sortedPoints = FindLatLongsWithTime(folder).OrderBy(x => x.Time).ToList();
            var points = sortedPoints.Select(p => new Wpt((decimal)p.Latitude, (decimal)p.Longitude) { Time = p.Time.UtcDateTime, TimeSpecified = true }).ToList();
            if (!points.Any())
            {
                using (var se = new StreamWriter(System.Console.OpenStandardError()))
                {
                    se.WriteLine("Couldn't find any pictures with GPS coordinates");
                    return;
                }
            }
            var gpx = new GPXLib();
            int trackIndex = 0;
            DateTime lastPointTime = points[trackIndex].Time;
            foreach (var point in points)
            {
                if (point.Time.Subtract(lastPointTime).TotalHours > 5)
                {
                    trackIndex++;
                }
                lastPointTime = point.Time;
                gpx.AddTrackPoint($"maintrack{trackIndex}", 0, point);
            }


            gpx.SaveToFile(@"F:\tmp\test-track2.gpx");
            File.WriteAllText(@"F:\tmp\test-track2.json", JsonConvert.SerializeObject(sortedPoints));
        }

        private static List<Position> FindLatLongsWithTime(string folder)
        {
            var positions = new List<Position>();
            foreach (var file in DirectoryUtilities.FindAllFiles(folder).Where(d => d.EndsWith(".jpg", System.StringComparison.InvariantCultureIgnoreCase)))
            {
                Console.WriteLine(file);
                var directories = ImageMetadataReader.ReadMetadata(file);
                if (!directories.Any())
                {
                    continue;
                }

                var latitude = directories.SelectMany(x => x.Tags).FirstOrDefault(t => t.Name == "GPS Latitude");
                var longitude = directories.SelectMany(x => x.Tags).FirstOrDefault(t => t.Name == "GPS Longitude");
                var date = directories.SelectMany(x => x.Tags).FirstOrDefault(t => t.Name == "GPS Date Stamp");
                var time = directories.SelectMany(x => x.Tags).FirstOrDefault(t => t.Name == "GPS Time-Stamp");


                if (latitude != null && longitude != null && date != null && time != null)
                {
                    var dateTime = $"{date.Description} {time.Description}";
                    Console.WriteLine(dateTime);
                    Console.WriteLine(gpsFormat);
                    var dateTimeUtc = DateTimeOffset.ParseExact(dateTime, gpsFormat, null, System.Globalization.DateTimeStyles.AssumeUniversal);

                    Console.WriteLine("[{0}]: {1}, {2}", dateTimeUtc, latitude.Description, longitude.Description);
                    positions.Add(new Position(dateTimeUtc, LatLongParser.ParseString(latitude.Description), LatLongParser.ParseString(longitude.Description)));
                }
            }

            return positions;
        }
    }
}
