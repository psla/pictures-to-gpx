using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PicturesToGpx.Gps
{
    /// <summary>
    ///  Just a reader class for 
    ///  <code>
    ///   [
    /// {"location": [[
    ///                {"latitude": 53.908485},
    /// {"longitude": 17.526583}
    /// ]]},
    /// {"timestamp": "Sun Jul 18 16:54:28 UTC 2010"}
    /// ]
    ///  </code>
    /// </summary>
    public class EndomondoPointConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return true;
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JArray array = JArray.Load(reader);
            JToken timestampToken = array.FirstOrDefault(e => e["timestamp"] != null);
            if (timestampToken == null)
            {
                return null;
            }
            var timestamp = timestampToken["timestamp"].Value<string>();
            JToken locationToken = array.FirstOrDefault(e => e["location"] != null);
            if (locationToken == null)
            {
                return null;
            }

            var latitude = locationToken["location"][0][0]["latitude"].Value<double>();
            var longitude = array.First(e => e["location"] != null)["location"][0][1]["longitude"].Value<double>();
            return new EndomondoJsonReader.Point(
                new EndomondoJsonReader.Location(latitude, longitude),
                timestamp);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
    public class EndomondoJsonReader : IGpsReader
    {
        internal class Location
        {
            internal Location()
            {
            }

            public Location(double latitude, double longitude)
            {
                Latitude = latitude;
                Longitude = longitude;
            }

            [JsonProperty(PropertyName = "latitude")]
            public double Latitude { get; private set; }

            [JsonProperty(PropertyName = "longitude")]
            public double Longitude { get; private set; }
        }

        // [JsonConverter(typeof(EndomondoPointConverter))]
        internal class Point
        {
            internal Point()
            {
            }

            public Point(Location location, string timestamp)
            {
                Location = location;
                Timestamp = timestamp;
            }

            [JsonProperty(PropertyName = "location")]
            public Location Location { get; private set; }

            [JsonProperty(PropertyName = "timestamp")]
            public string Timestamp { get; private set; }

            public DateTimeOffset TimeOffset()
            {
                if(char.IsDigit(Timestamp[0]))
                {
                    return DateTimeOffset.ParseExact(Timestamp, "yyyy-MM-dd H:mm:ss.0", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);
                }

                // "Sun Jul 18 16:54:28 UTC 2010"
                return DateTimeOffset.ParseExact(Timestamp, "ddd MMM dd H:mm:ss UTC yyyy", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);
            }

            internal Position ToPosition()
            {
                return new Position(TimeOffset(), Location.Latitude, Location.Longitude);
            }
        }
        class JsonEntry
        {
            [JsonProperty(PropertyName = "points", ItemConverterType = typeof(EndomondoPointConverter))]
            public List<Point> Points { get; private set; }
        }

        public IEnumerable<Position> Read(Stream stream)
        {
            //var entries = JsonConvert.DeserializeObject<List<JsonEntry>>(File.ReadAllText(filename));
            //Trace.Assert(entries.Count == 1);
            using (StreamReader streamReader = new StreamReader(stream))
            {
                var file = JArray.Parse(streamReader.ReadToEnd());
                var entry = file.FirstOrDefault(f => f["points"] != null);
                if (entry == null)
                {
                    return Enumerable.Empty<Position>();
                }
                var entries = entry.ToObject<JsonEntry>(); // JsonConvert.DeserializeObject<List<JsonEntry>>();

                return entries.Points.Where(p => p != null).Select(p => p.ToPosition());
            }
        }
    }
}
