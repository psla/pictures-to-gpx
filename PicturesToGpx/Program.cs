using MKCoolsoft.GPXLib;
using Newtonsoft.Json;
using SharpAvi.Codecs;
using SharpAvi.Output;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace PicturesToGpx
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("First parameter is the project file path. An example project file should look like this:");
                Console.WriteLine();
                Console.WriteLine(JsonConvert.SerializeObject(new Settings(), Formatting.Indented));
                return;
            }

            if (!File.Exists(args[0]))
            {
                Console.WriteLine("Project file {0} does not exist", args[0]);
                return;
            }

            Settings settings = JsonConvert.DeserializeObject<Settings>(File.ReadAllText(args[0]), new JsonSerializerSettings { Culture = CultureInfo.InvariantCulture, });
            CreateDirectoryIfNotExists(settings.WorkingDirectory);
            CreateDirectoryIfNotExists(settings.OutputDirectory);

            var folder = settings.PicturesInputDirectory;
            if (!Directory.Exists(folder))
            {
                using (var se = new StreamWriter(System.Console.OpenStandardError()))
                {
                    se.WriteLine("Provided directory doesn't exist, Pictures directory: {0}", folder);
                    return;
                }
            }
            CreateGpxFromPicturesInFolder(folder, settings.WorkingDirectory);
            CreateMapFromPoints(Path.Combine(settings.WorkingDirectory, "cached-positions.json"), settings);
        }

        private static void CreateDirectoryIfNotExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        private static void CreateMapFromPoints(string pointPath, Settings settings)
        {
            var points = JsonConvert.DeserializeObject<List<Position>>(File.ReadAllText(pointPath)).Skip(1).Take(2000).ToList();

            points = points.Select(LocationUtils.ToMercator).ToList();
            var boundingBox = LocationUtils.GetBoundingBox(points);

            var mapper = Tiler.RenderMap(boundingBox, settings.VideoConfig.Width, settings.VideoConfig.Height);

            points = mapper.GetPixels(points).ToList();
            points = points.SkipTooClose(15).ToList();
            points = points.SmoothLineChaikin(new GeometryUtils.ChaikinSettings());

            mapper.Save(Path.Combine(settings.OutputDirectory, "empty-map.png"));

            var writer = new AviWriter(Path.Combine(settings.OutputDirectory, "map.avi"))
            {
                FramesPerSecond = settings.VideoConfig.Framerate,
                EmitIndex1 = true
            };

            var encoder = new MotionJpegVideoEncoderWpf(settings.VideoConfig.Width, settings.VideoConfig.Height, 70);
            var stream = writer.AddEncodingVideoStream(encoder, true, settings.VideoConfig.Width, settings.VideoConfig.Height);
            stream.Width = settings.VideoConfig.Width;
            stream.Height = settings.VideoConfig.Height;

            double lengthSeconds = 4.0;
            int yieldFrame = Math.Max(1, (int)(points.Count / (lengthSeconds * 30)));

            int wroteFrames = 0;
            for (int i = 1; i < points.Count; i++)
            {
                mapper.DrawLine(points[i - 1], points[i]);
                if ((i - 1) % yieldFrame == 0)
                {
                    byte[] frameData = mapper.GetBitmap();
                    stream.WriteFrame(true, frameData, 0, frameData.Length);
                    wroteFrames++;
                }
            }
            byte[] lastFrameData = mapper.GetBitmap();
            stream.WriteFrame(true, lastFrameData, 0, lastFrameData.Length);
            writer.Close();
            // DrawBoundingBox(boundingBox, mapper);
            mapper.Save(Path.Combine(settings.OutputDirectory, "complete-map.png"));
            Console.WriteLine("Wrote frames: {0}, points.Count={1}, yieldFrame={2}", wroteFrames, points.Count, yieldFrame);
        }

        private static void DrawBoundingBox(BoundingBox boundingBox, Mapper mapper)
        {
            mapper.DrawLine(boundingBox.LowerLeft, boundingBox.UpperLeft);
            mapper.DrawLine(boundingBox.UpperLeft, boundingBox.UpperRight);
            mapper.DrawLine(boundingBox.UpperRight, boundingBox.LowerRight);
            mapper.DrawLine(boundingBox.LowerRight, boundingBox.LowerLeft);
        }

        private static void CreateGpxFromPicturesInFolder(string folder, string workingDir)
        {
            // TODO: Multiple tracks, group by day (in a timezone)
            var cachedPoints = Path.Combine(workingDir, "cached-positions.json");

            List<Position> sortedPoints = File.Exists(cachedPoints)
                ? JsonConvert.DeserializeObject<List<Position>>(File.ReadAllText(cachedPoints))
                : ImageUtility.FindLatLongsWithTime(folder).OrderBy(x => x.Time).ToList();

            if (!File.Exists(cachedPoints))
            {
                File.WriteAllText(cachedPoints, JsonConvert.SerializeObject(sortedPoints));
            }


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

    }
}
