using PicturesToGpx.Gps;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace PicturesToGpx
{
    internal static class DirectoryUtilities
    {
        public class FilePoints
        {
            public string Filename { get; set; }
            public List<Position> Positions { get; set; }
        }

        internal static IEnumerable<string> FindAllFiles(string directory)
        {
            if (!Directory.Exists(directory))
            {
                throw new DirectoryNotFoundException(directory);
            }

            foreach (var file in Directory.EnumerateFiles(directory))
            {
                yield return Path.Combine(directory, file);
            }

            foreach (var subdirectory in Directory.EnumerateDirectories(directory))
            {
                foreach (var file in FindAllFiles(Path.Combine(directory, subdirectory)))
                {
                    yield return file;
                }
            }
        }

        internal static IEnumerable<FilePoints> FindPointsForFiles(string gpsInputDirectory)
        {
            var points = new List<Position>();
            var endomondoReader = new EndomondoJsonReader();
            var fitReader = new FitReader();
            foreach (var file in FindAllFiles(gpsInputDirectory))
            {
                // not ideal, better if we iterated through all readers.

                if (file.EndsWith(".json", StringComparison.InvariantCultureIgnoreCase))
                {
                    Console.WriteLine("Parsing {0}", file);
                    yield return new FilePoints { Filename = file, Positions = endomondoReader.Read(file).ToList() };
                }

                if (file.EndsWith("fit.gz", StringComparison.InvariantCultureIgnoreCase))
                {
                    using (var stream = File.OpenRead(file))
                    using (var gzipStream = new GZipStream(stream, CompressionMode.Decompress))
                    using (var memoryStream = new MemoryStream())
                    {
                        gzipStream.CopyTo(memoryStream);
                        memoryStream.Seek(0, SeekOrigin.Begin);
                        yield return new FilePoints { Filename = file, Positions = fitReader.Read(memoryStream).ToList() };
                    }
                }
            }
        }
    }
}
