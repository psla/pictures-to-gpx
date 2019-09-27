using MetadataExtractor;
using MKCoolsoft.GPXLib;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PicturesToGpx
{
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
            var points = JsonConvert.DeserializeObject<List<Position>>(File.ReadAllText(pointPath)).Skip(300).Take(1000).ToList();



            points = points.Select(LocationUtils.ToMercator).ToList();
            var boundingBox = LocationUtils.GetBoundingBox(points);

            var mapper = Tiler.RenderMap(boundingBox, outgoingPicturePath, 1920, 1080);

            points = mapper.GetPixels(points).ToList();
            points = points.SkipTooClose().ToList();
            // points = points.SmoothLineChaikin(new GeometryUtils.ChaikinSettings());

            mapper.Save(@"F:\tmp\map.png");

            for (int i = 1; i < points.Count; i++)
            {
                mapper.DrawLine(points[i - 1], points[i]);
            }

            // DrawBoundingBox(boundingBox, mapper);
            mapper.Save(@"F:\tmp\map2.png");
        }

        private static void DrawBoundingBox(BoundingBox boundingBox, Mapper mapper)
        {
            mapper.DrawLine(boundingBox.LowerLeft, boundingBox.UpperLeft);
            mapper.DrawLine(boundingBox.UpperLeft, boundingBox.UpperRight);
            mapper.DrawLine(boundingBox.UpperRight, boundingBox.LowerRight);
            mapper.DrawLine(boundingBox.LowerRight, boundingBox.LowerLeft);
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
