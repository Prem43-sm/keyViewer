namespace KeyboardMouseOverlay.Models;

public sealed class OverlaySettings
{
    public bool OverlayEnabled { get; set; } = true;
    public double TextSize { get; set; } = 28;
    public double Opacity { get; set; } = 0.9;
    public double DisplayDurationSeconds { get; set; } = 1.2;
    public double HorizontalOffset { get; set; } = 18;
    public double VerticalOffset { get; set; } = 18;
    public string Theme { get; set; } = "Dark Glass";
}
