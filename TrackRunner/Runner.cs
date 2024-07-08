using System.Windows;
using System.Windows.Shapes;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Trainers;
using Microsoft.ML.Transforms;

namespace TrackRunner
{
    public class Runner
    {
        private MLContext mlContext;
        private HashSet<PositionInfo> trainData = new HashSet<PositionInfo>();
        private IReadOnlyCollection<Line> trackLines;
        private bool forceStop;
        private bool isRunning;
        private double bestDistance = double.MaxValue;
        private PositionInfo lastIntersectingPositionInfo;
        private int lastMaxStep;

        public event EventHandler Collision;

        public Runner()
        {
            Stop();
        }

        public async IAsyncEnumerable<(Point start, Point end)> Start(Point startPoint, Point endPoint, IReadOnlyCollection<Line> track)
        {
            trackLines = track;
            forceStop = false;
            isRunning = true;

            while (!this.forceStop)
            {
                await foreach ((Point start, Point end) in KeepRunning(startPoint, endPoint))
                {
                    yield return (start, end);
                }

                Collision?.Invoke(this, EventArgs.Empty);
            }
        }

        public void Stop()
        {
            this.bestDistance = double.MaxValue;
            this.trainData = new HashSet<PositionInfo>
            {
                new PositionInfo { AngleInDegrees = 360, DistanceFront = 1, DistanceLeft = 1, DistanceRight = 1 }
            };
            forceStop = true;
        }

        private async IAsyncEnumerable<(Point start, Point end)> KeepRunning(Point startPoint, Point endPoint)
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
                Vector rotatedPointVector = delta;
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
                    if (positionInfo.DistanceLeft > positionInfo.DistanceRight)
                    {
                        angleModificator = -1;
                    }

                    if (this.lastIntersectingPositionInfo != null && this.lastMaxStep == step)
                    {
                        angle = this.lastIntersectingPositionInfo.AngleInDegrees;
                    }

                    lastMaxStep = step;
                    angle += (5 * angleModificator);
                    (point, rotatedPointVector, intersects) = Move(currentPoint, delta, angle);
                    positionInfo.AngleInDegrees = angle;

                    lastIntersectingPositionInfo = positionInfo;
                    potentialTrainData.Add(positionInfo);

                    double distance = Math.Pow(currentPoint.X - endPoint.X, 2) + Math.Pow(currentPoint.Y - endPoint.Y, 2);
                    foreach (PositionInfo info in potentialTrainData)
                    {
                        this.trainData.Add(info);
                    }
                    this.bestDistance = distance;

                    yield break;
                }

                potentialTrainData.Add(GetPositionInfo(currentPoint, point, angle));

                currentPoint = point;
                delta = rotatedPointVector;

                await Task.Delay(10);
            } while (!intersects && !forceStop);

            isRunning = false;
        }

        private PredictionEngine<PositionInfo, PositionInfoAnglePrediction> Train()
        {
            mlContext = new MLContext();

            // Load Data
            IDataView data = mlContext.Data.LoadFromEnumerable(trainData);

            // Define data preparation estimator
            var pipelineEstimator =
                mlContext.Transforms.Concatenate("Features", new string[] { nameof(PositionInfo.DistanceFront), nameof(PositionInfo.DistanceLeft), nameof(PositionInfo.DistanceRight) })
                    .Append(mlContext.Transforms.NormalizeMinMax("Features"))
                    .Append(mlContext.Regression.Trainers.LbfgsPoissonRegression());

            // Train model
            ITransformer trainedModel = pipelineEstimator.Fit(data);

            return mlContext.Model.CreatePredictionEngine<PositionInfo, PositionInfoAnglePrediction>(trainedModel);
        }

        private (Point point, Vector rotatedPointVector, bool intersects) Move(Point currentPoint, Vector delta, float angle)
        {
            Point point = currentPoint;
            Point rotatedPoint = GeometryOperations.RotatePoint(point + delta, point, angle);
            Vector rotatedPointVector = rotatedPoint - point;

            point = rotatedPoint;

            var line = CreateLine(currentPoint, point, 2);
            bool intersects = trackLines.Any(l => GeometryOperations.Intersect(line, l) != null);

            return (point, rotatedPointVector, intersects);
        }

        private PositionInfo GetPositionInfo(Point currentPoint, Point point, float angle)
        {
            var info = new PositionInfo
            {
                AngleInDegrees = angle
            };

            Vector frontDirection = point - currentPoint;
            Vector leftDirection = new Vector(frontDirection.Y, -frontDirection.X);
            Vector rightDirection = new Vector(-frontDirection.Y, frontDirection.X);

            foreach (var l in trackLines)
            {
                float? frontDistance = GeometryOperations.GetRayToLineIntersectionDistance(currentPoint, frontDirection, l);
                if (frontDistance.HasValue && frontDistance.Value < info.DistanceFront)
                {
                    info.DistanceFront = frontDistance.Value;
                }

                float? leftDistance = GeometryOperations.GetRayToLineIntersectionDistance(currentPoint, leftDirection, l);
                if (leftDistance.HasValue && leftDistance.Value < info.DistanceLeft)
                {
                    info.DistanceLeft = leftDistance.Value;
                }

                float? rightDistance = GeometryOperations.GetRayToLineIntersectionDistance(currentPoint, rightDirection, l);
                if (rightDistance.HasValue && rightDistance.Value < info.DistanceRight)
                {
                    info.DistanceRight = rightDistance.Value;
                }
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
}
