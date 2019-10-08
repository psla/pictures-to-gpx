using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PicturesToGpx.Gps
{
    public class GoogleTimelineJsonReader : IGpsReader
    {
        private int minimumAccuracy;

        /// <param name="minimumAccuracy">the accuracy, in meters, that is required to extract point.</param>
        public GoogleTimelineJsonReader(int minimumAccuracy)
        {
            this.minimumAccuracy = minimumAccuracy;

        }

        public IEnumerable<Position> Read(string filename)
        {
            // Unfortunately reads the text into memory
            var timeline = JsonConvert.DeserializeObject<GoogleTimelineJson>(File.ReadAllText(filename));
            return timeline.Locations.Where(x => x.Accuracy <= this.minimumAccuracy).Select(p => p.ToPosition());
        }

        private class Location
        {
            [JsonProperty("accuracy")]
            public int Accuracy { get; set; }

            [JsonProperty("timestampMs")]
            public long TimestampMs { get; set; }

            [JsonProperty("latitudeE7")]
            public long LatitudeE7 { get; set; }

            [JsonProperty("longitudeE7")]
            public long LongitudeE7 { get; set; }

            internal Position ToPosition()
            {
                return new Position(DateTimeOffset.FromUnixTimeMilliseconds(TimestampMs), LatitudeE7 * 1e-7, LongitudeE7 * 1e-7);
            }
        }
        private class GoogleTimelineJson
        {
            [JsonProperty("locations")]
            public List<Location> Locations { get; set; }
        }
    }
}
