using System.IO;

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
            var directory = args[0];
            if (!Directory.Exists(directory))
            {
                using (var se = new StreamWriter(System.Console.OpenStandardError()))
                {
                    se.WriteLine("Provided directory doesn't exist");
                    return;
                }
            }

            foreach (var file in DirectoryUtilities.FindAllFiles(directory))
            {
                System.Console.WriteLine(file);
            }
        }
    }
}
