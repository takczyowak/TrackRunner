﻿using System.Windows;
using System.Windows.Shapes;
using Microsoft.ML;

namespace TrackRunner
{
    public class Runner
    {
        private MLContext mlContext;
        private List<PositionInfo> trainData = new List<PositionInfo>();
        private IReadOnlyCollection<Line> trackLines;
        private bool forceStop;
        private bool isRunning;

        public Runner()
        {
            mlContext = new MLContext(seed: 0);
        }

        public event EventHandler Collision;

        public async IAsyncEnumerable<(Point start, Point end)> Start(Point startPoint, IReadOnlyCollection<Line> track)
        {
            trackLines = track;
            forceStop = false;
            isRunning = true;

            await foreach ((Point start, Point end) in TrainingRun(startPoint))
            {
                yield return (start, end);
            }

            if (!isRunning)
            {
                yield break;
            }

            Collision?.Invoke(this, EventArgs.Empty);
            
            await foreach ((Point start, Point end) in KeepRunning(startPoint))
            {
                yield return (start, end);
            }
        }

        public void Stop()
        {
            forceStop = true;
        }

        private async IAsyncEnumerable<(Point start, Point end)> KeepRunning(Point startPoint)
        {
            var predictionEngine = Train();
            var currentPoint = startPoint;
            bool intersects;
            Vector delta = new Vector(0, -10);

            do
            {
                intersects = true;
                var point = currentPoint;
                double angle = 0;
                double angleModificator = 1;
                Vector rotatedPointVector = delta;
                PositionInfo positionInfo = GetPositionInfo(currentPoint, point + delta, angle);
                var s = predictionEngine.Predict(positionInfo);

                if (positionInfo.DistanceLeft > positionInfo.DistanceRight)
                {
                    angleModificator = -1;
                }

                while (intersects && angle <= 360)
                {
                    (point, rotatedPointVector, intersects) = Move(currentPoint, delta, angle);

                    if (intersects)
                    {
                        angle += (5 * angleModificator);
                    }
                    else
                    {
                        yield return (currentPoint, point);
                    }
                }

                positionInfo = GetPositionInfo(currentPoint, point, angle);

                currentPoint = point;
                delta = rotatedPointVector;

                await Task.Delay(500);
            } while (!intersects && !forceStop);

            isRunning = false;
            forceStop = false;
            trainData.Clear();
        }

        private async IAsyncEnumerable<(Point start, Point end)> TrainingRun(Point startPoint)
        {
            var currentPoint = startPoint;
            bool intersects;
            Vector delta = new Vector(0, -10);

            do
            {
                intersects = true;
                var point = currentPoint;
                double angle = 0;
                double angleModificator = 1;
                Vector rotatedPointVector = delta;
                PositionInfo positionInfo = GetPositionInfo(currentPoint, point + delta, angle);
                if (positionInfo.DistanceLeft > positionInfo.DistanceRight)
                {
                    angleModificator = -1;
                }

                bool didIntersect = false;
                while (intersects && angle <= 360)
                {
                    (point, rotatedPointVector, intersects) = Move(currentPoint, delta, angle);

                    if (intersects)
                    {
                        didIntersect = true;
                        angle += (5 * angleModificator);
                    }
                    else
                    {
                        trainData.Add(GetPositionInfo(currentPoint, point, angle));
                        if (didIntersect)
                        {
                            yield break;
                        }
                        yield return (currentPoint, point);
                    }
                }

                currentPoint = point;
                delta = rotatedPointVector;

                await Task.Delay(500);
            } while (!intersects && !forceStop);

            isRunning = false;
        }

        private PredictionEngine<PositionInfo, PositionInfoAnglePrediction> Train()
        {
            var data = mlContext.Data.LoadFromEnumerable(trainData);
            var pipeline = mlContext.Transforms.CopyColumns(outputColumnName: "Label", inputColumnName: nameof(PositionInfo.AngleInDegrees));
            pipeline.Append(mlContext.Transforms.Concatenate("Features", nameof(PositionInfo.DistanceFront), nameof(PositionInfo.DistanceLeft), nameof(PositionInfo.DistanceRight)));
            pipeline.Append(mlContext.Regression.Trainers.FastTree());
            var model = pipeline.Fit(data);

            return mlContext.Model.CreatePredictionEngine<PositionInfo, PositionInfoAnglePrediction>(model);
        }

        private (Point point, Vector rotatedPointVector, bool intersects) Move(Point currentPoint, Vector delta, double angle)
        {
            Point point = currentPoint;
            Point rotatedPoint = GeometryOperations.RotatePoint(point + delta, point, angle);
            Vector rotatedPointVector = rotatedPoint - point;

            point = rotatedPoint;

            var line = CreateLine(currentPoint, point, 2);
            bool intersects = trackLines.Any(l => GeometryOperations.Intersect(line, l) != null);

            return (point, rotatedPointVector, intersects);
        }

        private PositionInfo GetPositionInfo(Point currentPoint, Point point, double angle)
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
                double? frontDistance = GeometryOperations.GetRayToLineIntersectionDistance(currentPoint, frontDirection, l);
                if (frontDistance.HasValue && frontDistance.Value < info.DistanceFront)
                {
                    info.DistanceFront = frontDistance.Value;
                }

                double? leftDistance = GeometryOperations.GetRayToLineIntersectionDistance(currentPoint, leftDirection, l);
                if (leftDistance.HasValue && leftDistance.Value < info.DistanceLeft)
                {
                    info.DistanceLeft = leftDistance.Value;
                }

                double? rightDistance = GeometryOperations.GetRayToLineIntersectionDistance(currentPoint, rightDirection, l);
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
