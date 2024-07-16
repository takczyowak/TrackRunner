using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace TrackDetector;

public partial class MainWindow
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void OnDoClick(object sender, RoutedEventArgs e)
    {
        BitmapSource screen = ScreenCapturer.Capture(this, workspace);

        using (var fileStream = new FileStream(@"D:\dsdadsadas.png", FileMode.Create))
        {
            BitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(screen));
            encoder.Save(fileStream);
        }
    }

    private void OnTopPanelMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        DragMove();
    }

    private void OnCloseWindowClick(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
