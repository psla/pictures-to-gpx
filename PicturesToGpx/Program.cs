using GeoTimeZone;
using MKCoolsoft.GPXLib;
using Newtonsoft.Json;
using PicturesToGpx.Gps;
using SharpAvi;
using SharpAvi.Codecs;
using SharpAvi.Output;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

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

            Settings settings = JsonConvert.DeserializeObject<Settings>(File.ReadAllText(args[0]), new JsonSerializerSettings
            {
                Culture = CultureInfo.InvariantCulture,
                DateParseHandling = DateParseHandling.DateTimeOffset,
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
            });
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
            var allPoints = CacheOrExecute(Path.Combine(settings.WorkingDirectory, "cached-positions.json"), () => ImageUtility.FindLatLongsWithTime(folder));
            if (!string.IsNullOrEmpty(settings.GpsInputDirectory))
            {
                Console.WriteLine("Adding points from Endomondo");
                var gpsPoints = CacheOrExecute(Path.Combine(settings.WorkingDirectory, "endomondo-positions.json"), () => FindAllPointsFromGpx(settings.GpsInputDirectory));
                allPoints = EnumerableUtils.Merge(allPoints, gpsPoints, (x, y) => x.Time < y.Time).ToList();
            }

            if (!string.IsNullOrEmpty(settings.GoogleTimelineKmlFile))
            {
                Console.WriteLine("Adding Google Timeline points");
                var googleTimelinePoints = new GoogleTimelineKmlReader().Read(settings.GoogleTimelineKmlFile);
                allPoints = EnumerableUtils.Merge(allPoints, googleTimelinePoints, (x, y) => x.Time < y.Time).ToList();
            }
            allPoints = allPoints.Where(p => (settings.StartTime == null || p.Time > settings.StartTime) && (settings.EndTime == null || p.Time < settings.EndTime)).ToList();
            WritePointsAsGpx(settings.OutputDirectory, allPoints);
            CreateMapFromPoints(allPoints, settings);
        }

        private static List<Position> FindAllPointsFromGpx(string folder)
        {
            var points = new List<Position>();
            var endomondoReader = new EndomondoJsonReader();
            foreach (var file in DirectoryUtilities.FindAllFiles(folder))
            {
                if (file.EndsWith(".json", System.StringComparison.InvariantCultureIgnoreCase))
                {
                    Console.WriteLine("Parsing {0}", file);
                    points.AddRange(endomondoReader.Read(file));
                }
            }

            return points;
        }

        private static void CreateDirectoryIfNotExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        private static void CreateMapFromPoints(List<Position> points, Settings settings)
        {
            points = points.Select(LocationUtils.ToMercator).ToList();
            var boundingBox = LocationUtils.GetBoundingBox(points);

            var mapper = Tiler.RenderMap(boundingBox, settings.VideoConfig.Width, settings.VideoConfig.Height);

            points = mapper.GetPixels(points).ToList();
            points = points.SkipTooClose(15).ToList();
            points = points.SmoothLineChaikin(settings.SofteningSettings);

            mapper.Save(Path.Combine(settings.OutputDirectory, "empty-map.png"));

            var writer = new AviWriter(Path.Combine(settings.OutputDirectory, "map.avi"))
            {
                FramesPerSecond = settings.VideoConfig.Framerate,
                EmitIndex1 = true
            };

            IAviVideoStream stream = new NullVideoStream(settings.VideoConfig.Width, settings.VideoConfig.Height);

            if (settings.VideoConfig.ProduceVideo)
            {
                var encoder = new MotionJpegVideoEncoderWpf(settings.VideoConfig.Width, settings.VideoConfig.Height, 70);
                stream = writer.AddEncodingVideoStream(encoder, true, settings.VideoConfig.Width, settings.VideoConfig.Height);
                stream.Width = settings.VideoConfig.Width;
                stream.Height = settings.VideoConfig.Height;
            }

            double lengthSeconds = settings.VideoConfig.VideoDuration.TotalSeconds;
            double totalDistanceMeters = 0;

            double yieldFrame = Math.Max(1, (points.Count / (lengthSeconds * settings.VideoConfig.Framerate)));

            double nextFrame = 1;
            int wroteFrames = 0;
            for (int i = 1; i < points.Count; i++)
            {
                var previousPoint = mapper.FromPixelsToMercator(points[i - 1]);
                var currentPoint = mapper.FromPixelsToMercator(points[i]);
                totalDistanceMeters += previousPoint.DistanceMeters(currentPoint);

                if (mapper.IsStashed)
                {
                    mapper.StashPop();
                }

                mapper.DrawLine(points[i - 1], points[i]);
                if (settings.DisplayDistance || settings.DisplayDateTime)
                {
                    mapper.Stash();
                }
                if (settings.DisplayDistance)
                {
                    mapper.WriteText(string.Format("{0:0}km", totalDistanceMeters / 1000));
                }

                if (settings.DisplayDateTime)
                {
                    // mapper.WriteText(points[i].Time.ToString(), settings.VideoConfig.Height - 200);
                    Position positionWgs84 = currentPoint.GetWgs84();
                    var ianaTz = TimeZoneLookup.GetTimeZone(positionWgs84.Latitude, positionWgs84.Longitude).Result;
                    TimeSpan offset = TimeZoneConverter.TZConvert.GetTimeZoneInfo(ianaTz).GetUtcOffset(points[i].Time);
                    mapper.WriteText(points[i].Time.ToUniversalTime().Add(offset).ToString("MM/dd hh tt"), settings.VideoConfig.Height - 100);
                }

                if (i >= nextFrame)
                {
                    byte[] frameData = mapper.GetBitmap();
                    stream.WriteFrame(true, frameData, 0, frameData.Length);
                    wroteFrames++;

                    nextFrame += yieldFrame;
                }
            }
            if (mapper.IsStashed)
            {
                mapper.StashPop();
            }
            byte[] lastFrameData = mapper.GetBitmap();
            stream.WriteFrame(true, lastFrameData, 0, lastFrameData.Length);
            writer.Close();
            // DrawBoundingBox(boundingBox, mapper);
            string path = Path.Combine(settings.OutputDirectory, "complete-map.png");
            mapper.Save(path);
            Console.WriteLine("Wrote frames: {0}, points.Count={1}, yieldFrame={2}, path={3}", wroteFrames, points.Count, yieldFrame, path);
        }

        private static List<Position> CacheOrExecute(string cacheFile, Func<List<Position>> extractPositions)
        {
            List<Position> sortedPoints = File.Exists(cacheFile)
                ? JsonConvert.DeserializeObject<List<Position>>(File.ReadAllText(cacheFile))
                : extractPositions().OrderBy(x => x.Time).ToList();

            if (!File.Exists(cacheFile))
            {
                File.WriteAllText(cacheFile, JsonConvert.SerializeObject(sortedPoints));
            }

            return sortedPoints;
        }

        private static void WritePointsAsGpx(string outputPath, List<Position> sortedPoints)
        {
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

            gpx.SaveToFile(Path.Combine(outputPath, "track.gpx"));
            File.WriteAllText(Path.Combine(outputPath, "track.json"), JsonConvert.SerializeObject(sortedPoints));
        }
    }

    internal class NullVideoStream : IAviVideoStream
    {
        public NullVideoStream(int width, int height)
        {
            Width = width;
            Height = height;
        }

        public int Width { get; set; }
        public int Height { get; set; }
        public BitsPerPixel BitsPerPixel { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public FourCC Codec { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public int FramesWritten => throw new NotImplementedException();

        public int Index => throw new NotImplementedException();

        public string Name { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public void WriteFrame(bool isKeyFrame, byte[] frameData, int startIndex, int length)
        {
        }

        public Task WriteFrameAsync(bool isKeyFrame, byte[] frameData, int startIndex, int length)
        {
            return Task.CompletedTask;
        }
    }
}
