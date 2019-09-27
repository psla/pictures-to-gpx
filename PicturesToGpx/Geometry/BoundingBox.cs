using System;
using System.Diagnostics;

namespace PicturesToGpx
{
    internal class BoundingBox
    {
        internal double MiddleLatitude => (MaxLatitude - MinLatitude) / 2 + MinLatitude;
        internal double MiddleLongitude => (MaxLongitude - MinLongitude) / 2 + MinLongitude;

        public BoundingBox(double minLatitude, double minLongitude, double maxLatitude, double maxLongitude)
        {
            Debug.Assert(minLatitude < maxLatitude);
            Debug.Assert(minLongitude < maxLongitude);
            MinLatitude = minLatitude;
            MinLongitude = minLongitude;
            MaxLatitude = maxLatitude;
            MaxLongitude = maxLongitude;
        }

        public double MinLatitude { get; }

        public double MinLongitude { get; }

        public double MaxLatitude { get; }

        public double MaxLongitude { get; }

        public Position UpperLeft => new Position(DateTimeOffset.Now, MaxLatitude, MinLongitude);
        public Position UpperRight => new Position(DateTimeOffset.Now, MaxLatitude, MaxLongitude);
        public Position LowerLeft => new Position(DateTimeOffset.Now, MinLatitude, MinLongitude);
        public Position LowerRight => new Position(DateTimeOffset.Now, MinLatitude, MaxLongitude);
    }
}