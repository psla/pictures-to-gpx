using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml;

namespace PicturesToGpx.Gps
{
    public class GoogleTimelineKmlReader : IGpsReader
    {
        Regex parseCoords = new Regex(@"^(-?\d+\.?\d*) (-?\d+\.?\d*) \d+$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        public IEnumerable<Position> Read(string filename)
        {
            var doc = new XmlDocument();
            var nm = new XmlNamespaceManager(doc.NameTable);
            nm.AddNamespace("kml", "http://www.opengis.net/kml/2.2");
            nm.AddNamespace("gx", "http://www.google.com/kml/ext/2.2");
            doc.Load(filename);
            var whenNodes = doc.SelectNodes("//kml:when", nm);
            var coordNodes = doc.SelectNodes("//gx:coord", nm);
            if (whenNodes.Count != coordNodes.Count)
            {
                throw new InvalidOperationException("File was in an incorrect format");
            }

            for (int i = 0; i < whenNodes.Count; i++)
            {
                var offset = DateTimeOffset.Parse(whenNodes[i].FirstChild.Value);
                Match match = parseCoords.Match(coordNodes[i].FirstChild.Value);
                if (!match.Success)
                {
                    throw new InvalidOperationException("Can't parse: " + coordNodes[i].Value);
                }
                var position = new Position(offset, 
                    double.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture), 
                    double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture));

                yield return position;
            }
        }
    }
}
