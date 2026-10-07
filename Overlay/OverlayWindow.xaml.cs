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

    public OverlayWindow(OverlaySettings settings)
    {
        InitializeComponent();
        _settings = settings;
        _hideTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(settings.DisplayDurationSeconds) };
        _hideTimer.Tick += (_, _) => Hide();
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
    }

    private void UpdateWindowStyle()
    {
        var brush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(204, 17, 24, 39));
        MainBorder.Background = brush;
    }

    private void SetPosition(double cursorX, double cursorY)
    {
        ActionText.Measure(new global::System.Windows.Size(double.PositiveInfinity, double.PositiveInfinity));

        var labelWidth = ActionText.DesiredSize.Width + 28;
        var labelHeight = ActionText.DesiredSize.Height + 28;

        var screen = Screen.AllScreens
            .FirstOrDefault(s => s.Bounds.Contains((int)cursorX, (int)cursorY)) ?? Screen.PrimaryScreen ?? Screen.AllScreens.FirstOrDefault();

        var bounds = screen?.Bounds ?? new Rectangle(0, 0, (int)SystemParameters.PrimaryScreenWidth, (int)SystemParameters.PrimaryScreenHeight);

        var left = cursorX + _settings.HorizontalOffset;
        var top = cursorY + _settings.VerticalOffset;

        if (left + labelWidth > bounds.Right)
        {
            left = cursorX - labelWidth - _settings.HorizontalOffset;
        }

        if (top + labelHeight > bounds.Bottom)
        {
            top = cursorY - labelHeight - _settings.VerticalOffset;
        }

        if (left < bounds.Left)
        {
            left = bounds.Left;
        }

        if (top < bounds.Top)
        {
            top = bounds.Top + 8;
        }

        Left = left;
        Top = top;
        Width = labelWidth;
        Height = labelHeight;
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

    private static class NativeMethods
    {
        [DllImport("user32.dll")]
        public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
    }
}
