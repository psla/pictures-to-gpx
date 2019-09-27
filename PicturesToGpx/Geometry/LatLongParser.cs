using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace PicturesToGpx
{
    public class LatLongParser
    {
        private static readonly Regex regex = new Regex(@"^(-?\d+)° (\d+)' (\d+(\.\d+)?)""$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public static double ParseString(string value)
        {
            var match = regex.Match(value);
            if (!match.Success)
            {
                throw new ArgumentException($"'{value}' can't be parsed as lat/lng string");
            }

            double seconds = double.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture);
            double minutes = double.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
            double degrees = double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
            var minutesWithSeconds = (seconds / 60 + minutes) / 60;
            if (degrees < 0)
            {
                return degrees - minutesWithSeconds;
            }
            return degrees + minutesWithSeconds;
        }
    }
}