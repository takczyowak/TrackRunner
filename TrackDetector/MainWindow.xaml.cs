using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using Microsoft.ML.Data;
using Application = System.Windows.Application;
using Brushes = System.Windows.Media.Brushes;
using Point = System.Windows.Point;
using Rectangle = System.Windows.Shapes.Rectangle;
using Size = System.Windows.Size;

namespace TrackDetector;

public partial class MainWindow
{
    private List<Rectangle> boxes;
    private WindowInfo windowInfo;

    public MainWindow()
    {
        InitializeComponent();

        this.boxes = new List<Rectangle>();
        this.windowInfo = new WindowInfo();
    }

    private static Rectangle CreateBox(int[] points)
    {
        var box = new Rectangle();
        Canvas.SetLeft(box, points[0]);
        Canvas.SetTop(box, points[1]);
        box.Width = points[2] - points[0];
        box.Height = points[3] - points[1];
        box.StrokeThickness = 2;
        box.Stroke = Brushes.DarkOrange;

        return box;
    }

    private void OnDoClick(object sender, RoutedEventArgs e)
    {
        _ = Task.Run(
            (Action)(
                async () =>
                {
                    await foreach (int[] boxPoints in PredictionLoop())
                    {
                        Application.Current.Dispatcher.Invoke(
                            (Action)(() =>
                            {
                                this.boxes.Clear();
                                workspace.Children.Clear();

                                for (int i = 0; i < boxPoints.Length; i += 4)
                                {
                                    int[] points = boxPoints.Skip(i).Take(4).ToArray();
                                    Rectangle box = CreateBox(points);
                                    this.boxes.Add(box);
                                    workspace.Children.Add(box);
                                }
                            }));
                    }
                }));
    }

    private async IAsyncEnumerable<int[]> PredictionLoop()
    {
        while (true)
        {
            BitmapSource screen = ScreenCapturer.Capture(() => this.windowInfo);
            ITrakDetector.ModelOutput output;
            using (Stream bmp = new MemoryStream())
            {
                BitmapEncoder enc = new BmpBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(screen));
                enc.Save(bmp);
                bmp.Seek(0, SeekOrigin.Begin);
                output = ITrakDetector.Predict(new ITrakDetector.ModelInput { Image = MLImage.CreateFromStream(bmp) });
            }

            if (output.PredictedBoundingBoxes != null)
            {
                List<int> boundingBoxes = new List<int>();
                for (int i = 0; i < output.Score.Length; i++)
                {
                    if(output.Score[i] > 0.9)
                    {
                        boundingBoxes.AddRange(output.PredictedBoundingBoxes.Skip(i*4).Take(4).Select(p => (int)p));
                    }
                }

                yield return boundingBoxes.ToArray();
            }

            await Task.Yield();
        }
    }

    private void OnTopPanelMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        DragMove();
        var wih = new WindowInteropHelper(this);
        IntPtr hWnd = wih.Handle;
        Int32Rect rect = ScreenCapturer.GetWindowActualRect(hWnd);
        Point relativeLocation = workspace.TranslatePoint(new Point(0, 0), this);

        this.windowInfo = new WindowInfo(rect, new Rect(relativeLocation, new Size(workspace.ActualWidth, workspace.ActualHeight)));
    }

    private void OnCloseWindowClick(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
