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

        public IEnumerable<Position> Read(Stream stream)
        {
            // Unfortunately reads the text into memory
            using (StreamReader reader = new StreamReader(stream))
            using (JsonTextReader jsonReader = new JsonTextReader(reader))
            {
                JsonSerializer serializer = new JsonSerializer();
                var timeline = serializer.Deserialize<GoogleTimelineJson>(jsonReader);
                return timeline.Locations.Where(x => x.Accuracy <= this.minimumAccuracy && x.LatitudeE7 != 0).Select(p => p.ToPosition()).ToList();
            }
        }

        private class Location
        {
            [JsonProperty("accuracy")]
            public int Accuracy { get; set; }

            [JsonProperty("timestampMs")]
            public long TimestampMs { get; set; }


            [JsonProperty("timestamp")]
            public DateTimeOffset Timestamp { get; set; }

            [JsonProperty("latitudeE7")]
            public long LatitudeE7 { get; set; }

            [JsonProperty("longitudeE7")]
            public long LongitudeE7 { get; set; }

            internal Position ToPosition()
            {
                DateTimeOffset offset = DateTimeOffset.FromUnixTimeMilliseconds(TimestampMs);
                if(TimestampMs == 0)
                {
                    offset = Timestamp;
                }
                return new Position(offset, LatitudeE7 * 1e-7, LongitudeE7 * 1e-7);
            }
        }
        private class GoogleTimelineJson
        {
            [JsonProperty("locations")]
            public List<Location> Locations { get; set; }
        }
    }
}
