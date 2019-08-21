using MetadataExtractor;
using System;
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
                    Console.WriteLine("[{0} {1}]: {2}, {3}", date.Description, time.Description, latitude.Description, longitude.Description);
                }
                /*
                foreach (var directory in directories)
                {
                    foreach (var tag in directory.Tags)
                    {
                        Console.WriteLine($"[{directory.Name}] {tag.Name} = {tag.Description}");
                    }

                    if (directory.HasError)
                    {
                        foreach (var error in directory.Errors)
                        {
                            Console.WriteLine($"ERROR: {error}");
                        }
                    }
                }
                return;
                */
            }
        }
    }
}
