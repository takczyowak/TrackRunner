using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using TrackDetector.Win32;

namespace TrackDetector;

public static class ScreenCapturer
{
    public static BitmapSource Capture(Func<WindowInfo> getWindowInfo)
    {
        WindowInfo windowInfo = getWindowInfo();
        WpfScreen screen = WpfScreen.FromPoint((int)windowInfo.WindowPosition.X, (int)windowInfo.WindowPosition.Y);

        BitmapSource captured = CaptureRegion(
            IntPtr.Zero,
            (int)windowInfo.WindowPosition.X + (int)(windowInfo.WorkspacePosition.X * screen.Scale) + 1,
            (int)windowInfo.WindowPosition.Y + (int)(windowInfo.WorkspacePosition.Y * screen.Scale) + 1,
            (int)(windowInfo.WorkspacePosition.Width * screen.Scale),
            (int)(windowInfo.WorkspacePosition.Height * screen.Scale));

        return captured;
    }

    private static BitmapSource CaptureRegion(IntPtr hWnd, int x, int y, int width, int height)
    {
        IntPtr sourceDC = IntPtr.Zero;
        IntPtr targetDC = IntPtr.Zero;
        IntPtr compatibleBitmapHandle = IntPtr.Zero;

        try
        {
            sourceDC = User32.GetWindowDC(hWnd);
            targetDC = Gdi32.CreateCompatibleDC(sourceDC);

            compatibleBitmapHandle = Gdi32.CreateCompatibleBitmap(sourceDC, width, height);

            Gdi32.SelectObject(targetDC, compatibleBitmapHandle);

            Gdi32.BitBlt(targetDC, 0, 0, width, height, sourceDC, x, y, Gdi32.SRCCOPY);

            return Imaging.CreateBitmapSourceFromHBitmap(compatibleBitmapHandle, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
        }
        catch (Exception ex)
        { }
        finally
        {
            Gdi32.DeleteObject(compatibleBitmapHandle);

            User32.ReleaseDC(IntPtr.Zero, sourceDC);
            User32.ReleaseDC(IntPtr.Zero, targetDC);
        }

        return null;
    }

    public static Int32Rect GetWindowActualRect(IntPtr hWnd)
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
