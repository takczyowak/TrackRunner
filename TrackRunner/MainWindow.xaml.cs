using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace TrackRunner
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Point startPoint;
        private Point? firstPosition;
        private Point? lastPosition;
        private bool isRunning = false;
        private bool forceStop = false;
        private List<Line> trackLines = new List<Line>();
        private List<Line> pathLines = new List<Line>();

        public MainWindow()
        {
            InitializeComponent();
        }

        private void OnViewportLoaded(object sender, RoutedEventArgs e)
        {
            var startPointCircle = new Ellipse();
            startPointCircle.Stroke = new SolidColorBrush(Colors.Blue);
            startPointCircle.StrokeThickness = 4;
            startPointCircle.Width = 8;
            startPointCircle.Height = 8;
            startPoint = new Point(viewport.ActualWidth / 2, viewport.ActualHeight * 0.9);
            Canvas.SetLeft(startPointCircle, startPoint.X - 4);
            Canvas.SetTop(startPointCircle, startPoint.Y - 4);
            viewport.Children.Add(startPointCircle);
        }

        private void OnViewportMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                var position = e.GetPosition(viewport);

                if (position == lastPosition)
                {
                    return;
                }

                if (lastPosition == null)
                {
                    lastPosition = firstPosition = position;
                    return;
                }

                var line = CreateLine(lastPosition.Value, position, 3, Colors.Black);
                trackLines.Add(line);
                lastPosition = position;
                
                viewport.Children.Add(line);
                return;
            }

            if (e.RightButton == MouseButtonState.Pressed && trackLines.Count > 1 && firstPosition != null && lastPosition != null)
            {
                var line = CreateLine(lastPosition.Value, firstPosition.Value, 3, Colors.Black);
                trackLines.Add(line);

                firstPosition = null;
                lastPosition = null;

                viewport.Children.Add(line);
                return;
            }

            if (e.MiddleButton == MouseButtonState.Pressed)
            {
                foreach (Line line in trackLines)
                {
                    viewport.Children.Remove(line);
                }

                trackLines.Clear();
                firstPosition = null;
                lastPosition = null;
            }
        }

        private async void OnStart(object sender, RoutedEventArgs e)
        {
            isRunning = true;
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

                while (intersects && angle <= 360)
                {
                    (point, rotatedPointVector, intersects) = Move(currentPoint, delta, angle);

                    if (intersects)
                    {
                        angle += (5 * angleModificator);
                    }
                }

                positionInfo = GetPositionInfo(currentPoint, point, angle);
                frontDistanceLabel.Text = positionInfo.DistanceFront.ToString();
                leftDistanceLabel.Text = positionInfo.DistanceLeft.ToString();
                rightDistanceLabel.Text = positionInfo.DistanceRight.ToString();
                angleLabel.Text = positionInfo.AngleInDegrees.ToString();

                currentPoint = point;
                delta = rotatedPointVector;

                await Task.Delay(800);
            } while (!intersects && !forceStop);

            isRunning = false;
            forceStop = false;
        }

        private void OnReset(object sender, RoutedEventArgs e)
        {
            if (isRunning)
            {
                forceStop = true;
            }

            foreach (var l in pathLines)
            {
                viewport.Children.Remove(l);
            }
        }

        private (Point point, Vector rotatedPointVector, bool intersects) Move(Point currentPoint, Vector delta, double angle)
        {
            Point point = currentPoint;
            Point rotatedPoint = GeometryOperations.RotatePoint(point + delta, point, angle);
            Vector rotatedPointVector = rotatedPoint - point;

            point = rotatedPoint;

            var line = CreateLine(currentPoint, point, 2, Colors.DarkOrange);
            bool intersects = trackLines.Any(l => GeometryOperations.Intersect(line, l) != null);

            //if (!intersects && !forceStop)
            //{
                pathLines.Add(line);
                viewport.Children.Add(line);
            //}

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

        private static Line CreateLine(Point p1, Point p2, double thickness, Color color)
        {
            var line = new Line();
            line.Stroke = new SolidColorBrush(color);
            line.StrokeThickness = thickness;
            line.X1 = p1.X;
            line.X2 = p2.X;
            line.Y1 = p1.Y;
            line.Y2 = p2.Y;

            return line;
        }
    }
}