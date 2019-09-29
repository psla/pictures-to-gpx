using MKCoolsoft.GPXLib;
using Newtonsoft.Json;
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
            var allPoints = FindOrCacheAllPositionsFromPictures(folder, settings.WorkingDirectory);
            var gpsPoints = FindAllPointsFromGpx(settings.GpsInputDirectory);
            allPoints = allPoints.Where(p => (settings.StartTime == null || p.Time > settings.StartTime) && (settings.EndTime == null || p.Time < settings.EndTime)).ToList();
            WritePointsAsGpx(settings.OutputDirectory, allPoints);
            CreateMapFromPoints(allPoints, settings);
        }

        private static List<Position> FindAllPointsFromGpx(string folder)
        {
            foreach (var file in DirectoryUtilities.FindAllFiles(folder).Where(d => d.EndsWith(".tcx", System.StringComparison.InvariantCultureIgnoreCase)))
            {
            }
            return null;
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
            points = points.SmoothLineChaikin(new GeometryUtils.ChaikinSettings());

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

        private static List<Position> FindOrCacheAllPositionsFromPictures(string folder, string workingDir)
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
