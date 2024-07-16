using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using TrackDetector.Win32;
using Clipboard = System.Windows.Clipboard;
using DataFormats = System.Windows.DataFormats;
using DataObject = System.Windows.DataObject;
using IDataObject = System.Windows.IDataObject;
using Point = System.Windows.Point;

namespace TrackDetector;

public static class ScreenCapturer
{
    public static BitmapSource Capture(Window window, FrameworkElement workspace)
    {
        int width = (int)workspace.ActualWidth;
        int height = (int)workspace.ActualHeight;

        var wih = new WindowInteropHelper(window);
        IntPtr hWnd = wih.Handle;
        Int32Rect rect = GetWindowActualRect(hWnd);

        WpfScreen screen = WpfScreen.FromPoint(rect.X, rect.Y);
        Point relativeLocation = workspace.TranslatePoint(new Point(0, 0), window);

        BitmapSource captured = CaptureRegion(
            IntPtr.Zero,
            rect.X + (int)(relativeLocation.X * screen.Scale) + 1,
            rect.Y + (int)(relativeLocation.Y * screen.Scale) + 1,
            (int)(width * screen.Scale),
            (int)(height * screen.Scale),
            true);

        return captured;
    }

    private static BitmapSource CaptureRegion(IntPtr hWnd, int x, int y, int width, int height, bool addToClipboard)
    {
        IntPtr sourceDC = IntPtr.Zero;
        IntPtr targetDC = IntPtr.Zero;
        IntPtr compatibleBitmapHandle = IntPtr.Zero;
        BitmapSource bitmap = null;

        try
        {
            sourceDC = User32.GetWindowDC(hWnd);
            targetDC = Gdi32.CreateCompatibleDC(sourceDC);

            compatibleBitmapHandle = Gdi32.CreateCompatibleBitmap(sourceDC, width, height);

            Gdi32.SelectObject(targetDC, compatibleBitmapHandle);

            Gdi32.BitBlt(targetDC, 0, 0, width, height, sourceDC, x, y, Gdi32.SRCCOPY);

            bitmap = Imaging.CreateBitmapSourceFromHBitmap(
                compatibleBitmapHandle,
                IntPtr.Zero,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());

            if (addToClipboard)
            {
                IDataObject data = new DataObject();
                data.SetData(DataFormats.Dib, bitmap, false);
                Clipboard.SetDataObject(data, false);
            }
        }
        catch (Exception ex)
        { }
        finally
        {
            Gdi32.DeleteObject(compatibleBitmapHandle);

            User32.ReleaseDC(IntPtr.Zero, sourceDC);
            User32.ReleaseDC(IntPtr.Zero, targetDC);
        }

        return bitmap;
    }

    private static Int32Rect GetWindowActualRect(IntPtr hWnd)
    {
        User32.GetWindowRect(hWnd, out Win32Rect windowRect);
        User32.GetClientRect(hWnd, out Win32Rect clientRect);

        int sideBorder = (windowRect.Width - clientRect.Width) / 2 + 1;

        var topLeftPoint = new Win32Point(windowRect.Left - sideBorder, windowRect.Top - sideBorder);

        var actualRect = new Int32Rect(
            topLeftPoint.X,
            topLeftPoint.Y,
            windowRect.Width + sideBorder * 2,
            windowRect.Height + sideBorder * 2);

        return actualRect;
    }
}
