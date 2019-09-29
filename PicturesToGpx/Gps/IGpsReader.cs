using System.Collections.Generic;

namespace PicturesToGpx.Gps
{
    public interface IGpsReader
    {
        // Consider using stream instead of filename.
        IEnumerable<Position> Read(string filename);
    }
}