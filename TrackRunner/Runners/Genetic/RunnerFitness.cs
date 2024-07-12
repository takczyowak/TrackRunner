using System.Windows;
using GeneticSharp;

namespace TrackRunner.Runners.Genetic;

public sealed class RunnerFitness : IFitness
{
    private readonly Point startPoint;
    private readonly IReadOnlyCollection<(Point start, Point end)> trackLines;
    private readonly IReadOnlyCollection<(Point start, Point end)> checkpoints;

    public RunnerFitness(Point startPoint, IReadOnlyCollection<(Point start, Point end)> trackLines, IReadOnlyCollection<(Point start, Point end)> checkpoints)
    {
        this.startPoint = startPoint;
        this.trackLines = trackLines;
        this.checkpoints = checkpoints;
    }

    public double Evaluate(IChromosome chromosome)
    {
        var runnerChromosome = (RunnerChromosome)chromosome;
        PositionInfo[] positions = chromosome.GetGenes().Select(g => g.Value).Cast<PositionInfo>().ToArray();
        var path = new List<(Point start, Point end)>();
        Point currentPoint = this.startPoint;
        bool intersects;
        bool checkpointReached;
        var delta = new Vector(0, -10);
        int step = 0;

        do
        {
            ++step;
            Point point = currentPoint;
            float angle = 360;
            Vector rotatedPointVector;
            PositionInfo positionInfo = GetPositionInfo(currentPoint, point + delta, angle);
            PositionInfo bestMatchPosition = null;
            double smallestDifference = double.MaxValue;

            foreach (PositionInfo position in positions)
            {
                double difference = Math.Abs(position.Direction - positionInfo.Direction) + Math.Abs(position.DistanceFront - positionInfo.DistanceFront);
                if (difference < smallestDifference)
                {
                    smallestDifference = difference;
                    bestMatchPosition = position;
                }
            }

            (point, rotatedPointVector, intersects, checkpointReached) = Move(currentPoint, delta, bestMatchPosition.AngleInDegrees);
            path.Add((currentPoint, point));
            currentPoint = point;
            delta = rotatedPointVector;
        } while (!intersects && !checkpointReached && step < 100);

        runnerChromosome.Path = path;
        return checkpointReached ? 1.0 / step * 100.0 : (intersects ? step / 100.0 : 0.0);
    }

    private (Point point, Vector rotatedPointVector, bool intersects, bool checkpointReached) Move(Point currentPoint, Vector delta, float angle)
    {
        Point point = currentPoint;
        Point rotatedPoint = GeometryOperations.RotatePoint(point + delta, point, angle);
        Vector rotatedPointVector = rotatedPoint - point;

        point = rotatedPoint;
        bool intersects = this.trackLines.Any(l => GeometryOperations.Intersect((currentPoint, point), l) != null);
        bool checkpointReached = this.checkpoints.Any(l => GeometryOperations.Intersect((currentPoint, point), l) != null);

        return (point, rotatedPointVector, intersects, checkpointReached);
    }

    private PositionInfo GetPositionInfo(Point currentPoint, Point point, float angle)
    {
        var info = new PositionInfo
        {
            Direction = 1,
            AngleInDegrees = angle
        };

        Vector frontDirection = point - currentPoint;
        var leftDirection = new Vector(frontDirection.Y, -frontDirection.X);
        var rightDirection = new Vector(-frontDirection.Y, frontDirection.X);

        float leftDistance = float.MaxValue;
        float rightDistance = float.MaxValue;

        foreach ((Point start, Point end) l in this.trackLines)
        {
            float? frontDistance = GeometryOperations.GetRayToLineIntersectionDistance(currentPoint, frontDirection, l);
            if (frontDistance.HasValue && frontDistance.Value < info.DistanceFront)
            {
                info.DistanceFront = frontDistance.Value;
            }

            float? left = GeometryOperations.GetRayToLineIntersectionDistance(currentPoint, leftDirection, l);
            float? right = GeometryOperations.GetRayToLineIntersectionDistance(currentPoint, rightDirection, l);
            if (left < leftDistance)
            {
                leftDistance = left.Value;
            }

            if (right < rightDistance)
            {
                rightDistance = right.Value;
            }
        }

        if (info.DistanceFront < 3)
        {
            info.Direction = rightDistance / (leftDistance + rightDistance) * 2.0f;
        }

        return info;
    }
}
