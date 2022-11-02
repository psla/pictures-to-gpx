using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PicturesToGpx.Gps
{
    public interface IGpsReader
    {
        /// <summary>
        /// Reads a list of positions from a stream.
        /// </summary>
        /// <param name="stream">inpt stream. Must be closed by the caller after IEnumerable is consumed.</param>
        IEnumerable<Position> Read(Stream stream);
    }
}