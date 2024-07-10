using System.Windows;
using System.Windows.Shapes;
using Microsoft.ML;

namespace TrackRunner.Runners.LinearRegression;

public sealed class LinearRegressionRunner
{
    private MLContext mlContext;
    private HashSet<PositionInfo> trainData = new HashSet<PositionInfo>();
    private IReadOnlyCollection<Line> trackLines;
    private bool forceStop;
    private PositionInfo lastIntersectingPositionInfo;
    private int lastMaxStep;

    public event EventHandler Collision;

    public LinearRegressionRunner()
    {
        Stop();
    }

    public async IAsyncEnumerable<(Point start, Point end)> Start(Point startPoint, IReadOnlyCollection<Line> track)
    {
        this.trackLines = track;
        this.forceStop = false;

        while (!this.forceStop)
        {
            await foreach ((Point start, Point end) in KeepRunning(startPoint))
            {
                yield return (start, end);
            }

            Collision?.Invoke(this, EventArgs.Empty);
        }
    }

    public void Stop()
    {
        this.trainData = new HashSet<PositionInfo>
        {
            new PositionInfo { AngleInDegrees = 360, DistanceFront = 1, Direction = 1 }
        };
        this.forceStop = true;
    }

    private async IAsyncEnumerable<(Point start, Point end)> KeepRunning(Point startPoint)
    {
        List<PositionInfo> potentialTrainData = new List<PositionInfo>();
        PredictionEngine<PositionInfo, PositionInfoAnglePrediction> predictionEngine = Train();
        Point currentPoint = startPoint;
        bool intersects;
        var delta = new Vector(0, -10);
        int step = 0;

        do
        {
            ++step;
            Point point = currentPoint;
            float angle = 360;
            float angleModificator = 1;
            Vector rotatedPointVector;
            PositionInfo positionInfo = GetPositionInfo(currentPoint, point + delta, angle);

            PositionInfoAnglePrediction? s = predictionEngine.Predict(positionInfo);
            angle = s.AngleInDegrees;
            (point, rotatedPointVector, intersects) = Move(currentPoint, delta, angle);

            if (!intersects)
            {
                yield return (currentPoint, point);
            }
            else
            {
                if (positionInfo.Direction < 1)
                {
                    angleModificator = -1;
                }

                if (this.lastIntersectingPositionInfo != null && this.lastMaxStep == step)
                {
                    angle = this.lastIntersectingPositionInfo.AngleInDegrees;
                }

                angle += (5 * angleModificator);
                positionInfo.AngleInDegrees = angle;
                potentialTrainData.Add(positionInfo);

                foreach (PositionInfo info in potentialTrainData)
                {
                    this.trainData.Add(info);
                }

                this.lastIntersectingPositionInfo = positionInfo;
                this.lastMaxStep = step;
                yield break;
            }

            potentialTrainData.Add(GetPositionInfo(currentPoint, point, angle));

            currentPoint = point;
            delta = rotatedPointVector;

            await Task.Delay(10);
        } while (!intersects && !this.forceStop);
    }

    private PredictionEngine<PositionInfo, PositionInfoAnglePrediction> Train()
    {
        this.mlContext = new MLContext();

        // Load Data
        IDataView data = this.mlContext.Data.LoadFromEnumerable(this.trainData);

        // Define data preparation estimator
        var pipelineEstimator =
            this.mlContext.Transforms.Concatenate("Features", new string[] { nameof(PositionInfo.DistanceFront), nameof(PositionInfo.Direction) })
                .Append(this.mlContext.Transforms.NormalizeMinMax("Features"))
                .Append(this.mlContext.Regression.Trainers.LbfgsPoissonRegression());

        // Train model
        ITransformer trainedModel = pipelineEstimator.Fit(data);

        return this.mlContext.Model.CreatePredictionEngine<PositionInfo, PositionInfoAnglePrediction>(trainedModel);
    }

    private (Point point, Vector rotatedPointVector, bool intersects) Move(Point currentPoint, Vector delta, float angle)
    {
        Point point = currentPoint;
        Point rotatedPoint = GeometryOperations.RotatePoint(point + delta, point, angle);
        Vector rotatedPointVector = rotatedPoint - point;

        point = rotatedPoint;

        var line = CreateLine(currentPoint, point, 2);
        bool intersects = this.trackLines.Any(l => GeometryOperations.Intersect(line, l) != null);

        return (point, rotatedPointVector, intersects);
    }

    private PositionInfo GetPositionInfo(Point currentPoint, Point point, float angle)
    {
        var info = new PositionInfo
        {
            Direction = 1,
            AngleInDegrees = angle
        };

        Vector frontDirection = point - currentPoint;
        Vector leftDirection = new Vector(frontDirection.Y, -frontDirection.X);
        Vector rightDirection = new Vector(-frontDirection.Y, frontDirection.X);

        float leftDistance = float.MaxValue;
        float rightDistance = float.MaxValue;

        foreach (var l in this.trackLines)
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

    private static Line CreateLine(Point p1, Point p2, double thickness)
    {
        var line = new Line();
        line.StrokeThickness = thickness;
        line.X1 = p1.X;
        line.X2 = p2.X;
        line.Y1 = p1.Y;
        line.Y2 = p2.Y;

        return line;
    }
}