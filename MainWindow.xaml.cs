using System.Windows;
using System.Windows.Media;
using KeyboardMouseOverlay.Core;
using KeyboardMouseOverlay.Models;
using KeyboardMouseOverlay.Overlay;
using KeyboardMouseOverlay.Services;

namespace KeyboardMouseOverlay;

public partial class MainWindow : Window
{
    private readonly OverlaySettings _settings;
    private readonly InputMonitor _inputMonitor;
    private readonly OverlayWindow _overlayWindow;
    private bool _isRunning;

    public MainWindow()
    {
        _settings = SettingsService.Load();
        _inputMonitor = new InputMonitor();
        _inputMonitor.ActionTriggered += OnInputTriggered;

        InitializeComponent();

        _overlayWindow = new OverlayWindow(_settings);

        TextSizeSlider.Value = _settings.TextSize;
        OpacitySlider.Value = _settings.Opacity;
        DisplayDurationSlider.Value = _settings.DisplayDurationSeconds;
        HorizontalOffsetSlider.Value = _settings.HorizontalOffset;
        VerticalOffsetSlider.Value = _settings.VerticalOffset;
        UpdateDistanceLabels();
        DisplayDurationValueText.Text = $"{_settings.DisplayDurationSeconds:0.0} s";
        TextSizeValueText.Text = $"{_settings.TextSize:0} px";
        OpacityValueText.Text = $"{_settings.Opacity:P0}";

        ToggleButton.Content = "Start Overlay";
        SetStatus("Overlay stopped", "#475569");
        _overlayWindow.Hide();
    }

    private void ToggleButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isRunning)
        {
            StopMonitoring();
            return;
        }

        StartMonitoring();
    }

    private void ExitButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void StartMonitoring()
    {
        try
        {
            _inputMonitor.Start();
            _isRunning = true;
            ToggleButton.Content = "Stop Overlay";
            SetStatus("Overlay running", "#15803D");
            _settings.OverlayEnabled = true;
            SettingsService.Save(_settings);
        }
        catch (Exception ex)
        {
            global::System.Windows.MessageBox.Show($"Unable to start the global hook: {ex.Message}", "KeyViewer", MessageBoxButton.OK, MessageBoxImage.Warning);
            _isRunning = false;
            SetStatus("Start failed", "#B91C1C");
        }
    }

    private void StopMonitoring()
    {
        _inputMonitor.Stop();
        _isRunning = false;
        ToggleButton.Content = "Start Overlay";
        SetStatus("Overlay stopped", "#475569");
        _overlayWindow.Hide();
    }

    private void OnInputTriggered(object? sender, InputAction e)
    {
        if (!_isRunning || !_settings.OverlayEnabled)
        {
            return;
        }

        Dispatcher.BeginInvoke(new Action(() => _overlayWindow.ShowAction(e)));
    }

    private void TextSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_settings is null)
        {
            return;
        }

        _settings.TextSize = TextSizeSlider.Value;
        SettingsService.Save(_settings);
        if (TextSizeValueText is not null)
        {
            TextSizeValueText.Text = $"{_settings.TextSize:0} px";
        }
    }

    private void OpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_settings is null)
        {
            return;
        }

        _settings.Opacity = OpacitySlider.Value;
        SettingsService.Save(_settings);
        if (OpacityValueText is not null)
        {
            OpacityValueText.Text = $"{_settings.Opacity:P0}";
        }
    }

    private void DisplayDurationSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_settings is null)
        {
            return;
        }

        _settings.DisplayDurationSeconds = DisplayDurationSlider.Value;
        SettingsService.Save(_settings);
        if (DisplayDurationValueText is not null)
        {
            DisplayDurationValueText.Text = $"{_settings.DisplayDurationSeconds:0.0} s";
        }
    }

    private void HorizontalOffsetSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_settings is null)
        {
            return;
        }

        _settings.HorizontalOffset = HorizontalOffsetSlider.Value;
        SettingsService.Save(_settings);
        UpdateDistanceLabels();
    }

    private void VerticalOffsetSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_settings is null)
        {
            return;
        }

        _settings.VerticalOffset = VerticalOffsetSlider.Value;
        SettingsService.Save(_settings);
        UpdateDistanceLabels();
    }

    private void UpdateDistanceLabels()
    {
        if (HorizontalOffsetValueText is not null)
        {
            HorizontalOffsetValueText.Text = $"{_settings.HorizontalOffset:+0;-0;0} px";
        }

        if (VerticalOffsetValueText is not null)
        {
            VerticalOffsetValueText.Text = $"{_settings.VerticalOffset:+0;-0;0} px";
        }
    }

    private void SetStatus(string statusText, string colorHex)
    {
        StatusText.Text = statusText;
        StatusText.Foreground = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(colorHex)!);
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        _inputMonitor.Dispose();
        _overlayWindow.Close();
    }
}