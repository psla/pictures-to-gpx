using System;

namespace PicturesToGpx
{
    public enum PositionUnit
    {
        WGS84,
        Mercator,
        Pixel
    }

    public class Position
    {
        public Position(DateTimeOffset time, double latitude, double longitude, PositionUnit unit = PositionUnit.WGS84)
        {
            Time = time;
            Latitude = latitude;
            Longitude = longitude;
            Unit = unit;
        }

        public DateTimeOffset Time { get; private set; }

        public double Latitude { get; private set; }

        public double Longitude { get; private set; }

        public PositionUnit Unit { get; }

        public override string ToString()
        {
            return $"{Time}: {Latitude} {Longitude}";
        }


        public long DistanceSquare(Position other)
        {
            return (long)(Math.Pow(Latitude - other.Latitude, 2) +
                Math.Pow(Longitude - other.Longitude, 2));
        }
    }
}