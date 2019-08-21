using System.Collections.Generic;
using System.IO;

namespace PicturesToGpx
{
    internal static class DirectoryUtilities
    {
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
    }
}
