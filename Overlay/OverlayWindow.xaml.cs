using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Threading;
using KeyboardMouseOverlay.Models;

namespace KeyboardMouseOverlay.Overlay;

public partial class OverlayWindow : Window
{
    private const int GwlExstyle = -20;
    private const int WsExTransparent = 0x20;
    private const int WsExLayered = 0x80000;
    private const int WsExNoActivate = 0x08000000;

    private readonly OverlaySettings _settings;
    private readonly DispatcherTimer _hideTimer;
    private readonly DispatcherTimer _cursorTrackingTimer;

    public OverlayWindow(OverlaySettings settings)
    {
        InitializeComponent();
        _settings = settings;
        _hideTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(settings.DisplayDurationSeconds) };
        _hideTimer.Tick += (_, _) => Hide();
        _cursorTrackingTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
        _cursorTrackingTimer.Tick += TrackCursor;
        SourceInitialized += OnSourceInitialized;
    }

    public void ShowAction(InputAction action)
    {
        if (string.IsNullOrWhiteSpace(action.Label))
        {
            return;
        }

        ActionText.Text = action.Label;
        ActionText.FontSize = _settings.TextSize;
        UpdateWindowStyle();
        SetPosition(action.X, action.Y);
        Opacity = _settings.Opacity;

        _hideTimer.Stop();
        _hideTimer.Interval = TimeSpan.FromSeconds(_settings.DisplayDurationSeconds);
        _hideTimer.Start();

        if (!IsVisible)
        {
            Show();
        }
        else
        {
            Topmost = true;
        }

        _cursorTrackingTimer.Start();
    }

    public new void Hide()
    {
        _cursorTrackingTimer.Stop();
        base.Hide();
    }

    private void TrackCursor(object? sender, EventArgs e)
    {
        var cursorPosition = System.Windows.Forms.Cursor.Position;
        SetPosition(cursorPosition.X, cursorPosition.Y);
    }

    private void UpdateWindowStyle()
    {
        var brush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(204, 17, 24, 39));
        MainBorder.Background = brush;
    }

    private void SetPosition(double cursorX, double cursorY)
    {
        new WindowInteropHelper(this).EnsureHandle();
        var transformFromDevice = PresentationSource.FromVisual(this)?.CompositionTarget?.TransformFromDevice
            ?? System.Windows.Media.Matrix.Identity;

        MainBorder.Measure(new global::System.Windows.Size(double.PositiveInfinity, double.PositiveInfinity));
        var labelWidth = Math.Ceiling(MainBorder.DesiredSize.Width);
        var labelHeight = Math.Ceiling(MainBorder.DesiredSize.Height);

        var screen = Screen.AllScreens
            .FirstOrDefault(s => s.Bounds.Contains((int)cursorX, (int)cursorY)) ?? Screen.PrimaryScreen ?? Screen.AllScreens.FirstOrDefault();

        var bounds = screen?.Bounds ?? new Rectangle(0, 0, (int)SystemParameters.PrimaryScreenWidth, (int)SystemParameters.PrimaryScreenHeight);
        var cursor = transformFromDevice.Transform(new global::System.Windows.Point(cursorX, cursorY));
        var screenTopLeft = transformFromDevice.Transform(new global::System.Windows.Point(bounds.Left, bounds.Top));
        var screenBottomRight = transformFromDevice.Transform(new global::System.Windows.Point(bounds.Right, bounds.Bottom));
        var horizontalOffset = transformFromDevice.Transform(new global::System.Windows.Vector(_settings.HorizontalOffset, 0)).X;
        var verticalOffset = transformFromDevice.Transform(new global::System.Windows.Vector(0, _settings.VerticalOffset)).Y;

        var left = GetAxisPosition(
            cursor.X,
            labelWidth,
            screenTopLeft.X,
            screenBottomRight.X,
            horizontalOffset);
        var top = GetAxisPosition(
            cursor.Y,
            labelHeight,
            screenTopLeft.Y,
            screenBottomRight.Y,
            verticalOffset);

        Left = left;
        Top = top;
        Width = labelWidth;
        Height = labelHeight;
    }

    private static double GetAxisPosition(double cursor, double labelLength, double screenStart, double screenEnd, double offset)
    {
        var gap = Math.Abs(offset);
        var preferredPosition = offset >= 0 ? cursor + gap : cursor - gap - labelLength;
        var alternatePosition = offset >= 0 ? cursor - gap - labelLength : cursor + gap;
        var maxPosition = screenEnd - labelLength;

        if (preferredPosition >= screenStart && preferredPosition <= maxPosition)
        {
            return preferredPosition;
        }

        if (alternatePosition >= screenStart && alternatePosition <= maxPosition)
        {
            return alternatePosition;
        }

        return Math.Clamp(preferredPosition, screenStart, Math.Max(screenStart, maxPosition));
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        var helper = new WindowInteropHelper(this);
        IntPtr handle = helper.Handle;
        if (handle == IntPtr.Zero)
        {
            return;
        }

        var exStyle = NativeMethods.GetWindowLong(handle, GwlExstyle);
        NativeMethods.SetWindowLong(handle, GwlExstyle, exStyle | WsExTransparent | WsExLayered | WsExNoActivate);
    }

    protected override void OnClosed(EventArgs e)
    {
        _hideTimer.Stop();
        _cursorTrackingTimer.Stop();
        base.OnClosed(e);
    }

    private static class NativeMethods
    {
        [DllImport("user32.dll")]
        public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
    }
}
