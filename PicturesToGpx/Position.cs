using System;

namespace PicturesToGpx
{
    internal class Position
    {
        public Position(DateTimeOffset time, double latitude, double longitude)
        {
            Time = time;
            Latitude = latitude;
            Longitude = longitude;
        }

        public DateTimeOffset Time { get; private set; }

        public double Latitude { get; private set; }

        public double Longitude { get; private set; }
    }
}