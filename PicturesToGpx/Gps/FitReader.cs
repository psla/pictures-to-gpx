using Dynastream.Fit;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace PicturesToGpx.Gps
{
    public class FitReader : IGpsReader
    {
        public IEnumerable<Position> Read(Stream stream)
        {
            PositionsCollector positionsCollector = new PositionsCollector();

            var decoder = new Decode();
            MesgBroadcaster mesgBroadcaster = new MesgBroadcaster();
            decoder.MesgEvent += mesgBroadcaster.OnMesg;
            mesgBroadcaster.MesgEvent += positionsCollector.OnMesg;
            decoder.MesgDefinitionEvent += mesgBroadcaster.OnMesgDefinition;

            Trace.Assert(decoder.Read(stream));

            return positionsCollector.GetPositions();
        }


        private class PositionsCollector
        {
            List<Position> positions = new List<Position>();

            internal IEnumerable<Position> GetPositions()
            {
                return positions;
            }

            internal void OnMesg(object sender, MesgEventArgs e)
            {
                int? latFieldIndex = e.mesg.Fields.IndexOf(f => f.Name == "PositionLat");
                int? longFieldIndex = e.mesg.Fields.IndexOf(f => f.Name == "PositionLong");
                int? timestampFieldIndex = e.mesg.Fields.IndexOf(f => f.Name == "Timestamp");

                if (latFieldIndex == null)
                {
                    return;
                }

                if (longFieldIndex == null)
                {
                    return;
                }

                if (timestampFieldIndex == null)
                {
                    return;
                }

                List<Field> fields = e.mesg.Fields.ToList();
                object latObj = fields[latFieldIndex.Value].GetValue();
                object longObj = fields[longFieldIndex.Value].GetValue();
                object timestampOffset = fields[timestampFieldIndex.Value].GetValue();

                if (latObj == null || longObj == null || timestampOffset == null)
                {
                    return;
                }

                int rawLat = Convert.ToInt32(latObj);
                int rawLong = Convert.ToInt32(longObj);

                if (rawLat == int.MaxValue || rawLong == int.MaxValue || rawLat == 0x7FFFFFFF || rawLong == 0x7FFFFFFF)
                {
                    return;
                }

                double latitude = rawLat / (double)((1L << 32) / 360);
                double longitude = rawLong / (double)((1L << 32) / 360);

                if (latitude < -90 || latitude > 90 || longitude < -180 || longitude > 180)
                {
                    return;
                }

                uint rawTimestamp = Convert.ToUInt32(timestampOffset);
                DateTimeOffset time = SecondsFrom1989(rawTimestamp);
                var position = new Position(time,
                    latitude,
                    longitude,
                    0.0);
                positions.Add(position);
            }
        }

        private static DateTimeOffset SecondsFrom1989(uint value)
        {
            DateTimeOffset baseDate = new DateTimeOffset(1989, 12, 31, 0, 0, 0, TimeSpan.Zero);
            var dateAdjusted = baseDate.AddSeconds(value);
            return dateAdjusted;
        }
    }
}
