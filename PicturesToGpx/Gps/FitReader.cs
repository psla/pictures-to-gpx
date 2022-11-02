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
            System.Console.WriteLine(stream.Length);
            PositionsCollector positionsCollector = new PositionsCollector();

            var decoder = new Decode();
            MesgBroadcaster mesgBroadcaster = new MesgBroadcaster();
            decoder.MesgEvent += mesgBroadcaster.OnMesg;
            mesgBroadcaster.MesgEvent += positionsCollector.OnMesg;
            decoder.MesgEvent += (sender, e) =>
            {
                Console.WriteLine("MesgEvent: {0}", e.mesg);
            };
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
                Console.WriteLine("OnMesg: Received Mesg with global ID#{0}, its name is {1}", e.mesg.Num, e.mesg.Name);

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

                double latitude = ((int)e.mesg.GetFieldValue((byte)latFieldIndex)) / (double)((1L << 32) / 360);
                double longitude = ((int)e.mesg.GetFieldValue((byte)longFieldIndex)) / (double)((1L << 32) / 360);
                object timestampOffset = e.mesg.Fields.ToList()[timestampFieldIndex.Value].GetValue();
                DateTimeOffset time = SecondsFrom1989((uint)timestampOffset);
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
