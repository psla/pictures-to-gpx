using Newtonsoft.Json;
using System;
using System.Collections.Generic;
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

        [JsonConstructor]
        public Position(DateTimeOffset time, double latitude, double longitude, double dilutionOfPrecision, PositionUnit unit = PositionUnit.WGS84, Position derivedFrom = null)
        {
            Time = time;
            Latitude = latitude;
            Longitude = longitude;
            DilutionOfPrecision = dilutionOfPrecision;
            Unit = unit;
            this.derivedFrom = derivedFrom;
        }

        public Position(DateTimeOffset time, double latitude, double longitude, PositionUnit unit = PositionUnit.WGS84, Position derivedFrom = null)
            : this(time, latitude, longitude, 0, unit, derivedFrom)
        {
        }

        public DateTimeOffset Time { get; private set; }

        public double Latitude { get; private set; }

        public double Longitude { get; private set; }

        /// <summary>
        /// An interpreted value of dilution of precision from GPS, or 0 if not available.
        /// This value may not be persisted when units change.
        /// 
        /// "we were trained to watch our PDOP values with the rough idea that values below 6 were good enough and values below 4 were great.  Values at 9 or higher meant that the user shouldn’t rely on the accuracy of that data and should wait until a better PDOP value could be attained by the satellites moving into preferable positioning in the sky (or spreading out)."
        /// 
        /// Delta output location / delta measured data
        /// 
        /// 1	Ideal	Highest possible confidence level to be used for applications demanding the highest possible precision at all times.
        /// 1-2	Excellent At this confidence level, positional measurements are considered accurate enough to meet all but the most sensitive applications.
        /// 2-5	Good Represents a level that marks the minimum appropriate for making accurate decisions.Positional measurements could be used to make reliable in-route navigation suggestions to the user.
        /// 5-10	Moderate Positional measurements could be used for calculations, but the fix quality could still be improved. A more open view of the sky is recommended.
        /// 10-20	Fair Represents a low confidence level. Positional measurements should be discarded or used only to indicate a very rough estimate of the current location.
        /// >20	Poor At this level, measurements are inaccurate by as much as 300 meters with a 6-meter accurate device (50 DOP × 6 meters) and should be discarded.
        /// 
        /// TODO: consider making it nullable or optional instead of overloading the meaning of 0.
        /// </summary>
        public double DilutionOfPrecision { get; private set; }

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

        public override bool Equals(object obj)
        {
            return obj is Position position &&
                   EqualityComparer<Position>.Default.Equals(derivedFrom, position.derivedFrom) &&
                   Time.Equals(position.Time) &&
                   Latitude == position.Latitude &&
                   Longitude == position.Longitude &&
                   Unit == position.Unit;
        }

        public override int GetHashCode()
        {
            var hashCode = -189829872;
            hashCode = hashCode * -1521134295 + EqualityComparer<Position>.Default.GetHashCode(derivedFrom);
            hashCode = hashCode * -1521134295 + EqualityComparer<DateTimeOffset>.Default.GetHashCode(Time);
            hashCode = hashCode * -1521134295 + Latitude.GetHashCode();
            hashCode = hashCode * -1521134295 + Longitude.GetHashCode();
            hashCode = hashCode * -1521134295 + Unit.GetHashCode();
            return hashCode;
        }
    }
}