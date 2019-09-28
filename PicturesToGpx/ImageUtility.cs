using MetadataExtractor;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PicturesToGpx
{
    internal class ImageUtility
    {
        private const string gpsFormat = "yyyy:MM:dd HH:mm:ss.fff UTC";

        internal static Position TryExtractPositionFromFile(string file)
        {
            Console.WriteLine(file);

            IReadOnlyList<Directory> directories;
            try
            {
                directories = ImageMetadataReader.ReadMetadata(file);
            }
            catch (ImageProcessingException)
            {
                Console.WriteLine("Unable to process {0}", file);
                return null;
            }
            if (!directories.Any())
            {
                return null;
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
                return new Position(dateTimeUtc, LatLongParser.ParseString(latitude.Description), LatLongParser.ParseString(longitude.Description));
            }

            return null;
        }


        public static List<Position> FindLatLongsWithTime(string folder)
        {
            var positions = new List<Position>();
            foreach (var file in DirectoryUtilities.FindAllFiles(folder).Where(d => d.EndsWith(".jpg", System.StringComparison.InvariantCultureIgnoreCase)))
            {
                Position position = TryExtractPositionFromFile(file);

                if (position != null)
                {
                    positions.Add(position);
                }
            }

            return positions;
        }
    }
}
