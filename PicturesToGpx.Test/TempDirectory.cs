using System;
using System.IO;

namespace PicturesToGpx.Test
{
    /// <summary>
    /// Helper to create and automatically clean up a temporary directory upon disposal.
    /// </summary>
    public sealed class TempDirectory : IDisposable
    {
        public string Path { get; }

        public TempDirectory(string prefix = "PicturesToGpxTest_")
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), prefix + Guid.NewGuid().ToString());
            Directory.CreateDirectory(Path);
        }

        public string CreateSubdirectory(string name)
        {
            return Directory.CreateDirectory(System.IO.Path.Combine(Path, name)).FullName;
        }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                try
                {
                    Directory.Delete(Path, true);
                }
                catch
                {
                    // Ignore deletion failures during test cleanup
                }
            }
        }

        public override string ToString() => Path;

        public static implicit operator string(TempDirectory tempDir) => tempDir?.Path;
    }
}
