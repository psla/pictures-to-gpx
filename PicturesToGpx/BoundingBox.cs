using System;
using System.Diagnostics;

namespace PicturesToGpx
{
    internal class BoundingBox
    {
        internal double MiddleLatitude => (MaxLatitude - MinLatitude) / 2 + MinLatitude;
        internal double MiddleLongitude => (MaxLongitude - MinLongitude) / 2 + MinLongitude;
        private readonly double minLatitude;
        private readonly double minLongitude;
        private readonly double maxLatitude;
        private readonly double maxLongitude;

        public BoundingBox(double minLatitude, double minLongitude, double maxLatitude, double maxLongitude)
        {
            Debug.Assert(minLatitude < maxLatitude);
            Debug.Assert(minLongitude < maxLongitude);
            this.minLatitude = minLatitude;
            this.minLongitude = minLongitude;
            this.maxLatitude = maxLatitude;
            this.maxLongitude = maxLongitude;
        }

        public double MinLatitude => minLatitude;

        public double MinLongitude => minLongitude;

        public double MaxLatitude => maxLatitude;

        public double MaxLongitude => maxLongitude;

        public Position UpperLeft => new Position(DateTimeOffset.Now, MaxLatitude, MinLongitude);
        public Position UpperRight => new Position(DateTimeOffset.Now, MaxLatitude, MaxLongitude);
        public Position LowerLeft => new Position(DateTimeOffset.Now, MinLatitude, MinLongitude);
        public Position LowerRight => new Position(DateTimeOffset.Now, MinLatitude, MaxLongitude);
    }
}