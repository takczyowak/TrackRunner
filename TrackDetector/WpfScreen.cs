using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using Microsoft.VisualBasic;
using TrackDetector.Win32;
using static System.Formats.Asn1.AsnWriter;
using Point = System.Windows.Point;

namespace TrackDetector;

public class WpfScreen
{
    public IntPtr Handle { get; set; }
    public Rect Bounds { get; set; }
    public Rect NativeBounds { get; set; }
    public Rect WorkingArea { get; set; }
    public string Name { get; set; }
    public string AdapterName { get; set; }
    public string FriendlyName { get; set; }
    public int Dpi { get; set; }
    public double Scale => Dpi / 96d;
    public bool IsPrimary { get; set; }

    public static WpfScreen FromPoint(int left, int top)
    {
        var handle = User32.MonitorFromPoint(new Win32Point { X = left, Y = top }, 2);

        return ParseMonitor(handle, IntPtr.Zero);
    }

    private static WpfScreen ParseMonitor(IntPtr monitorHandle, IntPtr hdc)
    {
        var info = new Win32Monitor(); //TODO: MonitorInfo not getting filled with data.
        var a = User32.GetMonitorInfo(new HandleRef(null, monitorHandle), info);

        var name = new string(info.Device).TrimEnd((char)0);

        var monitor = new WpfScreen
        {
            Handle = monitorHandle,
            Name = name,
            FriendlyName = name,
            NativeBounds = new Rect(info.Monitor.Left, info.Monitor.Top,
                info.Monitor.Right - info.Monitor.Left,
                info.Monitor.Bottom - info.Monitor.Top),
            Bounds = new Rect(info.Monitor.Left, info.Monitor.Top,
                info.Monitor.Right - info.Monitor.Left,
                info.Monitor.Bottom - info.Monitor.Top),
            WorkingArea = new Rect(info.Work.Left, info.Work.Top,
                info.Work.Right - info.Work.Left,
                info.Work.Bottom - info.Work.Top),
            IsPrimary = (info.Flags & 0x00000001) != 0
        };

        #region Screen DPI

        try
        {
            User32.GetDpiForMonitor(monitorHandle, DpiTypes.Effective, out var aux, out _);
            monitor.Dpi = aux > 0 ? (int)aux : 96;
        }
        catch (Exception ex)
        {
            try
            {
                var h = Gdi32.CreateCompatibleDC(IntPtr.Zero);
                monitor.Dpi = Gdi32.GetDeviceCaps(h, 88);
                Gdi32.DeleteDC(h);
            }
            catch (Exception e)
            {
            }
        }

        #endregion

        return monitor;
    }
}
