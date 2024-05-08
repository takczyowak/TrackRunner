using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace TrackRunner
{
    public partial class MainWindow : Window
    {
        private Point startPoint;
        private Point? firstPosition;
        private Point? lastPosition;
        private List<Line> trackLines = new List<Line>();
        private List<Line> pathLines = new List<Line>();
        private Runner runner = new Runner();

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
            await foreach ((Point start, Point end) in runner.Start(startPoint, trackLines))
            {
                var line = CreateLine(start, end, 2, Colors.DarkOrange);
                pathLines.Add(line);
                viewport.Children.Add(line);
            }
        }

        private void OnReset(object sender, RoutedEventArgs e)
        {
            runner.Stop();

            foreach (var l in pathLines)
            {
                viewport.Children.Remove(l);
            }
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