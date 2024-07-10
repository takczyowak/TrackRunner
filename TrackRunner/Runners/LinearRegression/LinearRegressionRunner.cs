using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Trainers;

namespace TrackRunner.Runners.LinearRegression;

public sealed class LinearRegressionRunner
{
    private MLContext mlContext;
    private HashSet<PositionInfo> trainData = new();
    private IReadOnlyCollection<(Point start, Point end)> trackLines;
    private bool forceStop;
    private bool isRunning;
    private PositionInfo lastIntersectingPositionInfo;
    private int lastMaxStep;

    public LinearRegressionRunner()
    {
        Stop();
    }

    public event EventHandler Collision;

    public void Start(Point startPoint, IReadOnlyCollection<Line> track, Action<Point, Point, Color> drawLine)
    {
        this.trackLines = track.Select(t => (new Point(t.X1, t.Y1), new Point(t.X2, t.Y2))).ToArray();
        this.forceStop = false;

        Task.Run(
            async () =>
            {
                this.isRunning = true;
                while (!this.forceStop)
                {
                    await foreach ((Point start, Point end) in KeepRunning(startPoint))
                    {
                        drawLine(start, end, Colors.DarkOrange);
                    }

                    Collision?.Invoke(this, EventArgs.Empty);
                }

                this.isRunning = false;
            });
    }

    public void Stop()
    {
        this.trainData = new HashSet<PositionInfo>
        {
            new() { AngleInDegrees = 360, DistanceFront = 1, Direction = 1 }
        };
        this.forceStop = true;
    }

    private async IAsyncEnumerable<(Point start, Point end)> KeepRunning(Point startPoint)
    {
        var potentialTrainData = new List<PositionInfo>();
        PredictionEngine<PositionInfo, AnglePrediction> predictionEngine = Train();
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

            AnglePrediction? s = predictionEngine.Predict(positionInfo);
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

                angle += 5 * angleModificator;
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

    private PredictionEngine<PositionInfo, AnglePrediction> Train()
    {
        this.mlContext = new MLContext();

        // Load Data
        IDataView data = this.mlContext.Data.LoadFromEnumerable(this.trainData);

        // Define data preparation estimator
        EstimatorChain<RegressionPredictionTransformer<PoissonRegressionModelParameters>>? pipelineEstimator =
            this.mlContext.Transforms.Concatenate("Features", nameof(PositionInfo.DistanceFront), nameof(PositionInfo.Direction))
                .Append(this.mlContext.Transforms.NormalizeMinMax("Features"))
                .Append(this.mlContext.Regression.Trainers.LbfgsPoissonRegression());

        // Train model
        ITransformer trainedModel = pipelineEstimator.Fit(data);

        return this.mlContext.Model.CreatePredictionEngine<PositionInfo, AnglePrediction>(trainedModel);
    }

    private (Point point, Vector rotatedPointVector, bool intersects) Move(Point currentPoint, Vector delta, float angle)
    {
        Point point = currentPoint;
        Point rotatedPoint = GeometryOperations.RotatePoint(point + delta, point, angle);
        Vector rotatedPointVector = rotatedPoint - point;

        point = rotatedPoint;
        bool intersects = this.trackLines.Any(l => GeometryOperations.Intersect((currentPoint, point), l) != null);

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
