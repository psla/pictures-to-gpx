using GeoTimeZone;
using MKCoolsoft.GPXLib;
using Newtonsoft.Json;
using PicturesToGpx.Gps;
using SharpAvi;
using SharpAvi.Codecs;
using SharpAvi.Output;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PicturesToGpx
{
    internal static class Program
    {
        enum Operation { GenerateSingleOutput, GeneratePreviews };

        /// <summary>
        /// First argument: project file path to generate a single output OR "generatePreviews" to generate previews for images.
        /// </summary>
        /// <param name="args"></param>
        private static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("First parameter is the project file path. An example project file should look like this:");
                Console.WriteLine();
                Console.WriteLine(JsonConvert.SerializeObject(new Settings(), Formatting.Indented));
                return;
            }

            string configFile = "";
            Operation operation = Operation.GenerateSingleOutput;
            if (args[0] == "generatePreviews")
            {
                configFile = args[1];
                operation = Operation.GeneratePreviews;
            }
            else
            {
                configFile = args[0];
            }

            if (!File.Exists(configFile))
            {
                Console.WriteLine("Project file {0} does not exist", configFile);
                return;
            }

            Settings settings = JsonConvert.DeserializeObject<Settings>(File.ReadAllText(configFile), new JsonSerializerSettings
            {
                Culture = CultureInfo.InvariantCulture,
                DateParseHandling = DateParseHandling.DateTimeOffset,
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
            });

            switch (operation)
            {
                case Operation.GenerateSingleOutput: GenerateMap(settings); break;
                case Operation.GeneratePreviews: GeneratePreviews(settings); break;
            }
        }

        private static void GeneratePreviews(Settings settings)
        {
            foreach (var filePoints in DirectoryUtilities.FindPointsForFiles(settings.GpsInputDirectory))
            {
                if (!filePoints.Positions.Any())
                {
                    Console.WriteLine("No points found for file {0}", filePoints.Filename);
                    continue;
                }
                var startTime = filePoints.Positions.Min(p => p.Time);
                string filename = Path.GetFileNameWithoutExtension(filePoints.Filename);
                string outputImagePath = Path.Combine(settings.OutputDirectory, filename + ".png");

                if(File.Exists(outputImagePath))
                {
                    Console.WriteLine("Skipping {0} -- already exists", outputImagePath);
                    continue;
                }

                settings.StillConfig = new Settings.StillSettings { PopulatedMapPath = outputImagePath };
                Console.WriteLine("Outputting to {0}", outputImagePath);
                CreateMapFromPoints(filePoints.Positions, settings);
                File.SetCreationTimeUtc(outputImagePath, startTime.UtcDateTime);
                File.SetLastWriteTime(outputImagePath, startTime.UtcDateTime);
            }
        }

        private static void GenerateMap(Settings settings)
        {
            if (settings.StartTime > settings.EndTime)
            {
                Console.Error.WriteLine("Start time is later than end time");
                return;
            }

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
            Console.WriteLine("Loaded {0} positions from pictures", allPoints.Count);
            allPoints = allPoints.Where(p => p.DilutionOfPrecision < 10 && p.DilutionOfPrecision > -0.01).ToList();
            Console.WriteLine("After filtering, remaining {0} positions from pictures", allPoints.Count);
            if (!string.IsNullOrEmpty(settings.GpsInputDirectory))
            {
                Console.WriteLine("Loading points from Endomondo");
                var gpsPoints = CacheOrExecute(Path.Combine(settings.WorkingDirectory, "endomondo-positions.json"), () => FindAllPointsFromGpx(settings.GpsInputDirectory)).ToList();
                Console.WriteLine("Found {0} points from Endomondo", gpsPoints.Count);
                allPoints = EnumerableUtils.Merge(allPoints, gpsPoints, (x, y) => x.Time < y.Time).ToList();
            }

            if (!string.IsNullOrEmpty(settings.GoogleTimelineKmlFile))
            {
                Console.WriteLine("Loading Google Timeline points from KML");
                var googleTimelinePoints = new GoogleTimelineKmlReader().Read(settings.GoogleTimelineKmlFile).ToList();
                Console.WriteLine("Found {0} Google Timeline points", googleTimelinePoints.Count);
                allPoints = EnumerableUtils.Merge(allPoints, googleTimelinePoints, (x, y) => x.Time < y.Time).ToList();
            }
            if (!string.IsNullOrEmpty(settings.GoogleTimelineJsonFile))
            {
                Console.WriteLine("Loading Google Timeline points from JSON");
                var googleTimelinePoints = new GoogleTimelineJsonReader(settings.GoogleTimelineMinimumAccuracyMeters).Read(settings.GoogleTimelineJsonFile).ToList();
                Console.WriteLine("Found {0} Google Timeline points", googleTimelinePoints.Count);
                allPoints = EnumerableUtils.Merge(allPoints, googleTimelinePoints, (x, y) => x.Time < y.Time).ToList();
            }
            var pointsWithinTimerframe = allPoints.Where(p => (settings.StartTime == null || p.Time > settings.StartTime) && (settings.EndTime == null || p.Time < settings.EndTime)).ToList();
            if (!pointsWithinTimerframe.Any())
            {
                Console.Error.WriteLine("Did not fine any points within timeframe: [ {0}, {1} )", settings.StartTime, settings.EndTime);
                Console.Error.WriteLine("Oldest point: {0}", allPoints.Min(p => p.Time));
                Console.Error.WriteLine("Newest point: {0}", allPoints.Max(p => p.Time));
                return;
            }
            WritePointsAsGpx(settings.OutputDirectory, pointsWithinTimerframe);
            CreateMapFromPoints(pointsWithinTimerframe, settings);
        }

        private static List<Position> FindAllPointsFromGpx(string folder)
        {
            return DirectoryUtilities.FindPointsForFiles(folder).SelectMany(element => element.Positions).ToList();
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
            points = points.SkipTooClose(8).ToList();
            points = points.SmoothLineChaikin(settings.SofteningSettings);

            if (!string.IsNullOrEmpty(settings.StillConfig?.EmptyMapPath))
            {
                mapper.Save(settings.StillConfig?.EmptyMapPath);
                Console.WriteLine("Empty map saved");
            }

            AviWriter writer = null;

            IAviVideoStream stream = null; 

            if (settings.VideoConfig.ProduceVideo)
            {
                Console.WriteLine("Generating video");

                writer = new AviWriter(Path.Combine(settings.OutputDirectory, "map.avi"))
                {
                    FramesPerSecond = settings.VideoConfig.Framerate,
                    EmitIndex1 = true
                };

                stream = new NullVideoStream(settings.VideoConfig.Width, settings.VideoConfig.Height);

                var encoder = new MJpegWpfVideoEncoder(settings.VideoConfig.Width, settings.VideoConfig.Height, 70);
                stream = writer.AddEncodingVideoStream(encoder, true, settings.VideoConfig.Width, settings.VideoConfig.Height);
                stream.Width = settings.VideoConfig.Width;
                stream.Height = settings.VideoConfig.Height;
            }

            double lengthSeconds = settings.VideoConfig.VideoDuration.TotalSeconds;
            double totalDistanceMeters = 0;

            double yieldFrame = Math.Max(1, (points.Count / (lengthSeconds * settings.VideoConfig.Framerate)));

            DateTimeOffset lastDay = GetTimeInGpsCoordinatesZone(mapper.FromPixelsToMercator(points[0]).GetWgs84(), points[0].Time);
            double nextFrame = 1;
            var colors = settings.DayColors.Select(c => ColorTranslator.FromHtml(c)).ToList();
            Trace.Assert(colors.Count > 0);
            int colorIndex = 0;
            int wroteFrames = 0;
            for (int i = 1; i < points.Count; i++)
            {
                var previousPoint = mapper.FromPixelsToMercator(points[i - 1]);
                var currentPoint = mapper.FromPixelsToMercator(points[i]);
                totalDistanceMeters += previousPoint.DistanceMeters(currentPoint);

                var currentDay = GetTimeInGpsCoordinatesZone(currentPoint.GetWgs84(), points[i].Time);
                Color currentColor = colors[colorIndex];
                if (lastDay.DayOfYear != currentDay.DayOfYear)
                {
                    lastDay = currentDay;
                    colorIndex = (colorIndex + 1) % colors.Count;
                }

                if (mapper.IsStashed)
                {
                    mapper.StashPop();
                }

                mapper.DrawLine(points[i - 1], points[i], currentColor);
                if (settings.VideoConfig.ProduceVideo)
                {
                    if (settings.DisplayDistance || settings.DisplayDateTime)
                    {
                        mapper.Stash();
                    }
                    PrintDistance(settings, mapper, totalDistanceMeters);

                    if (settings.DisplayDateTime)
                    {
                        // mapper.WriteText(points[i].Time.ToString(), settings.VideoConfig.Height - 200);
                        var localTime = GetTimeInGpsCoordinatesZone(currentPoint.GetWgs84(), points[i].Time);
                        mapper.WriteText(currentDay.ToString("MM/dd hh tt"), settings.VideoConfig.Height - 100);
                    }

                    if (i >= nextFrame)
                    {
                        byte[] frameData = mapper.GetBitmap();
                        stream.WriteFrame(true, frameData, 0, frameData.Length);
                        wroteFrames++;

                        nextFrame += yieldFrame;
                    }
                }
            }
            if (mapper.IsStashed)
            {
                mapper.StashPop();
            }
            byte[] lastFrameData = mapper.GetBitmap();
            stream?.WriteFrame(true, lastFrameData, 0, lastFrameData.Length);
            PrintDistance(settings, mapper, totalDistanceMeters);
            writer?.Close();

            if (!string.IsNullOrEmpty(settings.StillConfig?.PopulatedMapPath))
            {
                mapper.Save(settings.StillConfig?.PopulatedMapPath);
            }

            Console.WriteLine("Wrote frames: {0}, points.Count={1}, yieldFrame={2}, path={3}", wroteFrames, points.Count, yieldFrame, settings.StillConfig?.PopulatedMapPath);
        }

        private static void PrintDistance(Settings settings, Mapper mapper, double totalDistanceMeters)
        {
            if (settings.DisplayDistance)
            {
                mapper.WriteText(string.Format("{0:0}km", totalDistanceMeters / 1000));
            }
        }

        private static DateTimeOffset GetTimeInGpsCoordinatesZone(Position positionWgs84, DateTimeOffset dateTime)
        {
            var ianaTz = TimeZoneLookup.GetTimeZone(positionWgs84.Latitude, positionWgs84.Longitude).Result;
            TimeSpan offset = TimeZoneConverter.TZConvert.GetTimeZoneInfo(ianaTz).GetUtcOffset(dateTime);
            return dateTime.ToUniversalTime().Add(offset);
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
