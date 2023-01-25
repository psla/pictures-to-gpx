using System.Collections.Generic;
using System.Diagnostics;

namespace PicturesToGpx
{
    public static class GeometryUtils
    {
        // TODO: List of (x,y) in pixels instead of positions!
        public static IEnumerable<Position> SkipTooClose(this IEnumerable<Position> input, int distanceToSkip = 10)
        {
            long distanceToSkipSquared = distanceToSkip * distanceToSkip;
            Position lastDrawnPoint = null;
            Position lastProcessedPoint = null;
            foreach (var element in input)
            {
                if (lastDrawnPoint == null)
                {
                    yield return element;
                    lastDrawnPoint = element;
                }
                else
                {
                    if (lastDrawnPoint.DistanceSquare(element) > distanceToSkipSquared)
                    {
                        yield return element;
                        lastDrawnPoint = element;
                    }
                    else if (lastProcessedPoint.DistanceSquare(element) > distanceToSkipSquared)
                    {
                        // the idea is that if the previous point is further away than minimum distance,
                        // even if it was close to the former point, we are still going to draw it for the best approximation.
                        // 
                        // it would be even better to draw all points every X pixels, but only draw a FRAME every X distance. But that some other time :)
                        yield return lastProcessedPoint;
                    }
                }

                lastProcessedPoint = element;
            }
        }

        public static List<Position> SmoothLineChaikin(this List<Position> input, Settings.ChaikinSettings settings)
        {
            if (input.Count < 2)
            {
                return input;
            }

            var output = input;
            int iterationCount = 0;
            do
            {
                input = output;
                output = new List<Position>();
                output.Add(input[0]);
                for (int i = 0; i < input.Count - 2; i++)
                {
                    Debug.Assert(input[i].Unit == input[i + 1].Unit);
                    output.Add(
                        new Position(
                        input[i].Time.AddSeconds((input[i + 1].Time - input[i].Time).TotalSeconds * settings.WhereToRound),
                        (input[i + 1].Latitude - input[i].Latitude) * settings.WhereToRound + input[i].Latitude,
                        (input[i + 1].Longitude - input[i].Longitude) * settings.WhereToRound + input[i].Longitude, input[i].Unit));

                    double whereToRoundSecond = (1 - settings.WhereToRound);
                    output.Add(new Position(
                        input[i + 1].Time.AddSeconds((input[i + 2].Time - input[i + 1].Time).TotalSeconds * (1 - settings.WhereToRound)),
                        (input[i + 2].Latitude - input[i + 1].Latitude) * (1 - settings.WhereToRound) + input[i + 1].Latitude,
                        (input[i + 2].Longitude - input[i + 1].Longitude) * whereToRoundSecond + input[i + 1].Longitude, input[i].Unit));
                }
                output.Add(input[input.Count - 1]);
                iterationCount++;
            } while (output.Count != input.Count && iterationCount < settings.MaxIterationCount);
            return output;
        }

    }
}
