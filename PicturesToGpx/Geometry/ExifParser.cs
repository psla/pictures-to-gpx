using MetadataExtractor;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PicturesToGpx.Geometry
{
    public static class ExifParser
    {
        private static readonly Regex regex = new Regex(@"^(\d+)/(\d+)$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public static double ParseRationalOrDouble(String dilutionOfPrecision)
        {
            Match match = regex.Match(dilutionOfPrecision);
            if (match.Success)
            {
                return int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture) /
                    (double) int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
            }
            return double.Parse(dilutionOfPrecision, CultureInfo.InvariantCulture);
        }
    }
}
