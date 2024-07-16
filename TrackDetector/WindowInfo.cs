using System.Windows;

namespace TrackDetector;

public sealed class WindowInfo
{
    public WindowInfo()
    {
    }

    public WindowInfo(Int32Rect window, Rect workspace)
    {
        WindowPosition = window;
        WorkspacePosition = workspace;
    }

    public Int32Rect WindowPosition { get; }
    public Rect WorkspacePosition { get; }
}
