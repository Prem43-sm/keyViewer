using System.Windows;
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

        ToggleButton.Content = "Start Overlay";
        StatusText.Text = "Status: Stopped";
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
            StatusText.Text = "Status: Running";
            _settings.OverlayEnabled = true;
            SettingsService.Save(_settings);
        }
        catch (Exception ex)
        {
            global::System.Windows.MessageBox.Show($"Unable to start the global hook: {ex.Message}", "Keyboard Mouse Overlay", MessageBoxButton.OK, MessageBoxImage.Warning);
            _isRunning = false;
            StatusText.Text = "Status: Start failed";
        }
    }

    private void StopMonitoring()
    {
        _inputMonitor.Stop();
        _isRunning = false;
        ToggleButton.Content = "Start Overlay";
        StatusText.Text = "Status: Stopped";
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
    }

    private void OpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_settings is null)
        {
            return;
        }

        _settings.Opacity = OpacitySlider.Value;
        SettingsService.Save(_settings);
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

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        _inputMonitor.Dispose();
        _overlayWindow.Close();
    }
}