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
                }
                else
                {
                    if (lastDrawnPoint.DistanceSquare(element) > distanceToSkipSquared)
                    {
                        yield return element;
                        lastDrawnPoint = element;
                    }
                    else if (lastProcessedPoint != null && lastProcessedPoint.DistanceSquare(element) > distanceToSkipSquared)
                    {
                        yield return lastProcessedPoint;
                        lastDrawnPoint = lastProcessedPoint;
                    }
                }

                if (lastDrawnPoint == null)
                {
                    lastDrawnPoint = element;
                }
                lastProcessedPoint = element;
            }
        }

        public static List<Position> SmoothLineChaikin(this List<Position> input, Settings.ChaikinSettings settings)
        {
            if (input == null || input.Count < 2 || settings == null || settings.MaxIterationCount <= 0)
            {
                return input;
            }

            double ratioA = 1.0 - settings.WhereToRound;
            double ratioB = settings.WhereToRound;

            var output = input;
            int iterationCount = 0;
            while (iterationCount < settings.MaxIterationCount)
            {
                var currentInput = output;
                output = new List<Position>(currentInput.Count * 2);
                output.Add(currentInput[0]);

                for (int i = 0; i < currentInput.Count - 1; i++)
                {
                    var p0 = currentInput[i];
                    var p1 = currentInput[i + 1];
                    Debug.Assert(p0.Unit == p1.Unit);

                    double totalSec = (p1.Time - p0.Time).TotalSeconds;

                    // Q_i = (1 - u)*P0 + u*P1
                    output.Add(new Position(
                        p0.Time.AddSeconds(totalSec * ratioA),
                        (p1.Latitude - p0.Latitude) * ratioA + p0.Latitude,
                        (p1.Longitude - p0.Longitude) * ratioA + p0.Longitude,
                        p0.Unit,
                        p0));

                    // R_i = u*P0 + (1 - u)*P1
                    output.Add(new Position(
                        p0.Time.AddSeconds(totalSec * ratioB),
                        (p1.Latitude - p0.Latitude) * ratioB + p0.Latitude,
                        (p1.Longitude - p0.Longitude) * ratioB + p0.Longitude,
                        p0.Unit,
                        p1));
                }

                output.Add(currentInput[currentInput.Count - 1]);
                iterationCount++;
            }

            return output;
        }

        /// <summary>
        /// Calculates the total distance in meters across a sequence of positions, skipping time gaps greater than <paramref name="maxGapHours"/>.
        /// </summary>
        public static double CalculateTotalDistance(this IEnumerable<Position> positions, double maxGapHours = 5.0)
        {
            Position previous = null;
            double total = 0.0;
            foreach (var position in positions)
            {
                if (previous != null)
                {
                    if ((position.Time - previous.Time).TotalHours < maxGapHours)
                    {
                        total += position.DistanceMeters(previous);
                    }
                }
                previous = position;
            }
            return total;
        }
    }
}
