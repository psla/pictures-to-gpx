using System;
using System.Diagnostics;

namespace PicturesToGpx
{
    public enum PositionUnit
    {
        /// <summary>
        /// EPSG:4326 WGS 84
        /// </summary>
        WGS84,

        /// <summary>
        /// EPSG:3857 WGS 84 / Pseudo-Mercator
        /// </summary>
        Mercator,

        Pixel
    }

    public class Position
    {
        /// <summary>
        /// If the current position is derived from some other position (e.g. pixels computed from mercator), this contains the original one.
        /// </summary>
        private readonly Position derivedFrom;

        public Position(DateTimeOffset time, double latitude, double longitude, PositionUnit unit = PositionUnit.WGS84, Position derivedFrom = null)
        {
            Time = time;
            Latitude = latitude;
            Longitude = longitude;
            Unit = unit;
            this.derivedFrom = derivedFrom;
        }

        public DateTimeOffset Time { get; private set; }

        public double Latitude { get; private set; }

        public double Longitude { get; private set; }

        public PositionUnit Unit { get; } = PositionUnit.WGS84;

        public override string ToString()
        {
            return $"{Time}: {Latitude} {Longitude} [{Unit}], from=({derivedFrom})";
        }

        public long DistanceSquare(Position other)
        {
            return (long)(Math.Pow(Latitude - other.Latitude, 2) +
                Math.Pow(Longitude - other.Longitude, 2));
        }

        public double DistanceMeters(Position other)
        {
            var p1 = this.GetMercator();
            var p2 = other.GetMercator();

            var p1wgs = p1.GetWgs84();
            var p2wgs = p2.GetWgs84();

            var simpsonRule =
                (1 / Math.Cos(LocationUtils.ToRadians(p1wgs.Latitude))
                + 1 / Math.Cos(LocationUtils.ToRadians(p2wgs.Latitude))
                + 4 / Math.Cos(LocationUtils.ToRadians(p1wgs.Latitude + p2wgs.Latitude / 2.0))) / 6.0;

            return Math.Sqrt(p1.DistanceSquare(p2)) / simpsonRule;
        }

        public Position GetMercator()
        {
            if (this.Unit == PositionUnit.Mercator) { return this; }
            if (this.derivedFrom != null) { return this.derivedFrom.GetMercator(); }

            throw new InvalidOperationException("Can't derive mercator position");
        }

        internal Position TryGetWgs84()
        {
            Position result = null;
            if (this.Unit == PositionUnit.WGS84) { return this; }
            if (this.derivedFrom != null) { result = this.derivedFrom.TryGetWgs84(); }
            if (result == null && this.Unit == PositionUnit.Mercator)
            {
                return LocationUtils.FromMercatorToWgs84(this);
            }
            return null;
        }
        public Position GetWgs84()
        {
            var result = TryGetWgs84();
            if (result == null)
            {
                throw new InvalidOperationException("Can't derive WGS84 position");
            }

            return result;
        }
    }
}