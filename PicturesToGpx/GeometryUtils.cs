using System.Collections.Generic;

namespace PicturesToGpx
{
    public static class GeometryUtils
    {
        // TODO: List of (x,y) in pixels instead of positions!
        public static IEnumerable<Position> SkipTooClose(this IEnumerable<Position> input, int distanceToSkip = 10)
        {
            long distanceToSkipSquared = distanceToSkip * distanceToSkip;
            Position lastPoint = null;
            foreach (var element in input)
            {
                if (lastPoint == null)
                {
                    yield return element;
                    lastPoint = element;
                }
                else
                {
                    if (lastPoint.DistanceSquare(element) > distanceToSkipSquared)
                    {
                        yield return element;
                        lastPoint = element;
                    }
                }
            }
        }

        public class ChaikinSettings
        {
            public double WhereToRound { get; set; } = 0.75;

            public int MaxIterationCount { get; set; } = 2;
        }

        public static List<Position> SmoothLineChaikin(this List<Position> input, ChaikinSettings settings)
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
                    output.Add(
                        new Position(
                        input[i].Time.AddSeconds((input[i + 1].Time - input[i].Time).TotalSeconds * settings.WhereToRound),
                        (input[i + 1].Latitude - input[i].Latitude) * settings.WhereToRound + input[i].Latitude,
                        (input[i + 1].Longitude - input[i].Longitude) * settings.WhereToRound + input[i].Longitude));

                    double whereToRoundSecond = (1 - settings.WhereToRound);
                    output.Add(new Position(
                        input[i + 1].Time.AddSeconds((input[i + 2].Time - input[i + 1].Time).TotalSeconds * (1 - settings.WhereToRound)),
                        (input[i + 2].Latitude - input[i + 1].Latitude) * (1 - settings.WhereToRound) + input[i + 1].Latitude,
                        (input[i + 2].Longitude - input[i + 1].Longitude) * whereToRoundSecond + input[i + 1].Longitude));
                }
                output.Add(input[input.Count - 1]);
                iterationCount++;
            } while (output.Count != input.Count && iterationCount < settings.MaxIterationCount);
            return output;
        }

    }
}
