using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PicturesToGpx.Gps
{
    public static class GpsReaderUtil
    {

        /// <summary>
        /// Read all data from filename
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="filename"></param>
        /// <returns></returns>
        public static IEnumerable<Position> Read(this IGpsReader reader, string filename)
        {
            using (var stream = File.OpenRead(filename))
            {
                return reader.Read(stream).ToList();
            }
        }
    }
}
