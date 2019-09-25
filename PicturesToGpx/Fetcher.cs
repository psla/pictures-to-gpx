using System;
using System.Drawing;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;

namespace PicturesToGpx
{
    internal class Fetcher
    {
        internal Fetcher(string cacheDir = @"G:\tmp\tile-cache")
        {
            this.cacheDir = cacheDir;
        }

        internal static readonly string BlackTile = "BLACKTILE";

        private readonly string cacheDir;

        private static readonly Regex AsciiRegex = new Regex("[^a-zA-Z0-9]", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        internal Bitmap Fetch(string url)
        {
            if (url == BlackTile)
            {
                return new Bitmap(256, 256);
            }

            var normalizedFilename = AsciiRegex.Replace(url, "-");
            Console.WriteLine("Fetching {0} to {1}", url, normalizedFilename);
            var targetPath = Path.Combine(cacheDir, normalizedFilename + ".png");

            if (File.Exists(targetPath))
            {
                return new Bitmap(targetPath);
            }

            using (var wc = new WebClient())
            {
                wc.Headers.Add("user-agent", "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)");
                wc.DownloadFile(url, targetPath);
            }

            return new Bitmap(targetPath);
        }
    }
}