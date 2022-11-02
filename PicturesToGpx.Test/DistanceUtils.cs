using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PicturesToGpx.Test
{
    public static class DistanceUtils
    {
        /// <summary>
        /// Measures distance represented by the list of points. 
        /// </summary>
        public static double TotalDistanceMeters(this IEnumerable<Position> positions)
        {
            Position previous = null;
            double total = 0.0;
            foreach (var position in positions)
            {
                if(previous != null)
                {
                    total += position.DistanceMeters(previous);
                }
                previous = position;
            }
            return total;
        }
    }
}
