using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using TrackRunner.Runners.Genetic;
using TrackRunner.Runners.LinearRegression;

namespace TrackRunner;

public partial class MainWindow : Window
{
    private Point startPoint;
    private Point? firstPosition;
    private Point? lastPosition;
    private Point? lastCheckpointPosition;
    private List<Line> trackLines = new();
    private List<Line> pathLines = new();
    private List<Line> checkpoints = new();

    //private LinearRegressionRunner linearRegressionRunner = new();
    private GeneticRunner geneticRunner;

    public MainWindow()
    {
        InitializeComponent();
        //this.linearRegressionRunner.Collision += OnLinearRegressionRunnerCollision;
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

    private void OnViewportLoaded(object sender, RoutedEventArgs e)
    {
        var startPointCircle = new Ellipse();
        startPointCircle.Stroke = new SolidColorBrush(Colors.Blue);
        startPointCircle.StrokeThickness = 4;
        startPointCircle.Width = 8;
        startPointCircle.Height = 8;
        this.startPoint = new Point(viewport.ActualWidth / 2, viewport.ActualHeight * 0.9);

        Canvas.SetLeft(startPointCircle, this.startPoint.X - 4);
        Canvas.SetTop(startPointCircle, this.startPoint.Y - 4);
        viewport.Children.Add(startPointCircle);
    }

    private void OnViewportMouseDown(object sender, MouseButtonEventArgs e)
    {
        Point position = e.GetPosition(viewport);
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            if (Keyboard.IsKeyDown(Key.LeftShift))
            {
                if (this.lastCheckpointPosition.HasValue)
                {
                    Line checkpoint = CreateLine(this.lastCheckpointPosition.Value, position, 3, Colors.Red);
                    this.checkpoints.Add(checkpoint);
                    this.lastCheckpointPosition = null;
                    viewport.Children.Add(checkpoint);
                }
                else
                {
                    this.lastCheckpointPosition = position;
                }

                return;
            }

            if (position == this.lastPosition)
            {
                return;
            }

            if (this.lastPosition == null)
            {
                this.lastPosition = this.firstPosition = position;
                return;
            }

            Line line = CreateLine(this.lastPosition.Value, position, 3, Colors.Black);
            this.trackLines.Add(line);
            this.lastPosition = position;

            viewport.Children.Add(line);
            return;
        }

        if (e.RightButton == MouseButtonState.Pressed && this.trackLines.Count > 1 && this.firstPosition != null && this.lastPosition != null)
        {
            Line line = CreateLine(this.lastPosition.Value, this.firstPosition.Value, 3, Colors.Black);
            this.trackLines.Add(line);

            this.firstPosition = null;
            this.lastPosition = null;

            viewport.Children.Add(line);
            return;
        }

        if (e.MiddleButton == MouseButtonState.Pressed)
        {
            foreach (Line line in this.trackLines.Concat(this.checkpoints))
            {
                viewport.Children.Remove(line);
            }

            this.trackLines.Clear();
            this.checkpoints.Clear();
            this.firstPosition = null;
            this.lastPosition = null;
        }
    }

    private void OnStart(object sender, RoutedEventArgs e)
    {
        void drawLine(Point start, Point end, Color color)
        {
            Application.Current.Dispatcher.Invoke(
                new Action(
                    () =>
                    {
                        Line line = CreateLine(start, end, 2, color);
                        this.pathLines.Add(line);
                        viewport.Children.Add(line);
                    }));
        }

        this.geneticRunner = new GeneticRunner(this.startPoint, this.trackLines, this.checkpoints, 200, 400, drawLine, ClearPaths);
        this.geneticRunner.Start();
        //this.linearRegressionRunner.Start(this.startPoint, this.trackLines, drawLine);
    }

    private void OnReset(object sender, RoutedEventArgs e)
    {
        //this.linearRegressionRunner.Stop();
        this.geneticRunner?.Stop();

        foreach (Line l in this.pathLines)
        {
            viewport.Children.Remove(l);
        }
    }

    private void ClearPaths()
    {
        Application.Current.Dispatcher.Invoke(
            new Action(
                () =>
                {
                    foreach (Line l in this.pathLines)
                    {
                        viewport.Children.Remove(l);
                    }
                    this.pathLines.Clear();
                }));
    }

    private void OnLinearRegressionRunnerCollision(object sender, EventArgs e)
    {
        ClearPaths();
    }
}
