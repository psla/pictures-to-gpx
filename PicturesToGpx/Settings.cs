﻿using Newtonsoft.Json;
using System;
using System.IO;
using System.Text.RegularExpressions;

namespace PicturesToGpx
{
    public class Settings
    {

        public static Settings Example()
        {
            return new Settings()
            {
                GoogleTimelineKmlFile = @"C:\somepath\to\Location History.kml",
                GoogleTimelineJsonFile = @"C:\somepath\to\Location History.json"
            };
        }

        public class VideoSettings
        {
            public bool ProduceVideo { get; set; } = true;

            /// <summary>
            /// How long the produced video should take
            /// </summary>
            public TimeSpan VideoDuration { get; set; } = TimeSpan.FromSeconds(4.5);

            public int Width { get; set; } = 1920;

            public int Height { get; set; } = 1080;

            public int Framerate { get; set; } = 30;
        }

        public class GpsSoftenerSettings
        {
            /// <summary>
            ///  A value between 0.6 and 0.95. How close to the actual vertex should the curve begin.
            /// </summary>
            public double LineSofteningRatio { get; set; } = 0.75;

            /// <summary>
            /// How many iterations softening algorithm should go through. Depending on line thickness, 3 may be good enough.
            /// </summary>
            public int MaxNumberOfIterations { get; set; } = 3;

            /// <summary>
            /// If the GPS pixels are close to each other, this is the radius in which they are ignored (and treated as one).
            /// This is important for softening results.
            /// </summary>
            public int ClusterSizePx { get; set; } = 7;
        }

        public class ChaikinSettings
        {
            public double WhereToRound { get; set; } = 0.75;

            public int MaxIterationCount { get; set; } = 3;
        }

        // Alphanumeric project name
        [JsonProperty(Required = Required.Always)]
        public string ProjectName { get; set; } = "project_name";

        /// <summary>
        /// A directory in which pictures will be searched for.
        /// </summary>
        public string PicturesInputDirectory { get; set; } = @"F:\Fotografie\2019\2019-07_Maroko_Morocco";

        /// <summary>
        /// GPS files (GPX, TCX) will be searched on in here.
        /// </summary>
        public string GpsInputDirectory { get; set; }

        /// <summary>
        /// A KML file from Google timeline.
        /// </summary>
        public string GoogleTimelineKmlFile { get; set; }

        /// <summary>
        /// A JSON file from Google timeline. Use this if you want to exclude low-accuracy points.
        /// </summary>
        public string GoogleTimelineJsonFile { get; set; }

        /// <summary>
        /// A minimum accurracy in meters which is considered as a valid location from Google Timeline log.
        /// Google timeline can include accuraccy based on wifi networks and/or BTS, and those can be grossly inaccurate.
        /// 
        /// In one corner case, the position was 1000 miles away (with an accuracy of around 250meters). If you are getting a lot of wrong locations, tweak this number.
        /// e.g. a 50m accuracy should still have high recall (lots of points), but filter out a lot of noise.
        /// </summary>
        public int GoogleTimelineMinimumAccuracyMeters { get; set; } = 500;


        /// <summary>
        ///  Where to store images & videos.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string OutputDirectory { get; set; } = @"G:\tmp\PicturesToGpx-Morocco\out";

        /// <summary>
        /// If provided, this is a minimum date time for all points -- points found before this date will be ignored.
        /// </summary>
        public DateTimeOffset? StartTime { get; set; } = new DateTime(2019, 07, 07);

        /// <summary>
        /// If provided, this is a maximum date time for all points -- points found after this date (and time) will be ignored.
        /// </summary>
        public DateTimeOffset? EndTime { get; set; } = new DateTime(2019, 07, 14, 14, 00, 00);

        /// <summary>
        ///  A directory in which intermediate files will be stored. E.g. cached location points.
        /// </summary>
        public string WorkingDirectory { get; set; }

        /// <summary>
        ///  Directory in which tiles are cached. This path can be reused by multiple projects.
        /// </summary>
        public string TileCacheDirectory { get; set; } = @"G:\tmp\tile-cache";

        public TilerConfig TilerConfig { get; set; } = new TilerConfig();

        public VideoSettings VideoConfig { get; set; } = new VideoSettings();

        public ChaikinSettings SofteningSettings { get; set; } = new ChaikinSettings();

        public bool DisplayDistance { get; set; } = true;

        /// <summary>
        /// Whether or not the date & time should be displayed.
        /// </summary>
        public bool DisplayDateTime { get; set; } = true;

        public Settings()
        {
            WorkingDirectory = Path.Combine(Path.GetTempPath(), ProjectName);
        }

        private void Validate()
        {
            if (!Regex.IsMatch(ProjectName, "^[a-zA-Z0-9_]+$"))
            {
                throw new ArgumentException($"Invalid project name {ProjectName}");
            }
        }
    }
}
