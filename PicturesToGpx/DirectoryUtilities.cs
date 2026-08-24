using PicturesToGpx.Gps;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace PicturesToGpx
{
    public static class DirectoryUtilities
    {
        public class FilePoints
        {
            public string Filename { get; set; }
            public List<Position> Positions { get; set; }
        }

        public static IEnumerable<string> FindAllFiles(string directory)
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

        public static IEnumerable<FilePoints> FindPointsForFiles(string gpsInputDirectory)
        {
            var points = new List<Position>();
            var endomondoReader = new EndomondoJsonReader();
            var fitReader = new FitReader();
            Console.WriteLine("Enumerating {0}", gpsInputDirectory);
            foreach (var file in FindAllFiles(gpsInputDirectory))
            {
                // not ideal, better if we iterated through all readers.

                if (file.EndsWith(".json", StringComparison.InvariantCultureIgnoreCase))
                {
                    Console.WriteLine("Parsing {0}", file);
                    yield return new FilePoints { Filename = file, Positions = endomondoReader.Read(file).ToList() };
                }
                else if (file.EndsWith(".fit.gz", StringComparison.InvariantCultureIgnoreCase))
                {
                    Console.WriteLine("Parsing fit.gz {0}", file);
                    using (var stream = File.OpenRead(file))
                    using (var gzipStream = new GZipStream(stream, CompressionMode.Decompress))
                    using (var memoryStream = new MemoryStream())
                    {
                        gzipStream.CopyTo(memoryStream);
                        memoryStream.Seek(0, SeekOrigin.Begin);
                        yield return new FilePoints { Filename = file, Positions = fitReader.Read(memoryStream).ToList() };
                    }
                }
                else if (file.EndsWith(".fit", StringComparison.InvariantCultureIgnoreCase))
                {
                    Console.WriteLine("Parsing fit {0}", file);
                    using (var stream = File.OpenRead(file))
                    {
                        yield return new FilePoints { Filename = file, Positions = fitReader.Read(stream).ToList() };
                    }
                } else
                {
                    Console.WriteLine("Skipping {0}", file);
                }
            }
        }
    }
}
