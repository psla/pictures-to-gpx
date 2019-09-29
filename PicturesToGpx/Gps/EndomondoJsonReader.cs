﻿using Newtonsoft.Json;
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
            var timestamp = array.First(e => e["timestamp"] != null)["timestamp"].Value<string>();
            var latitude = array.First(e => e["location"] != null)["location"][0][0]["latitude"].Value<double>();
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

        public IEnumerable<Position> Read(string filename)
        {
            //var entries = JsonConvert.DeserializeObject<List<JsonEntry>>(File.ReadAllText(filename));
            //Trace.Assert(entries.Count == 1);
            var file = JArray.Parse(File.ReadAllText(filename));
            var entry = file.First(f => f["points"] != null);
            var entries = entry.ToObject<JsonEntry>(); // JsonConvert.DeserializeObject<List<JsonEntry>>();

            return entries.Points.Select(p => p.ToPosition());
        }
    }
}
