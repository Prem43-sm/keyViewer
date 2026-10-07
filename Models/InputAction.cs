namespace KeyboardMouseOverlay.Models;

public enum ActionKind
{
    Keyboard,
    Mouse,
}

public sealed class InputAction
{
    public required string Label { get; init; }
    public ActionKind Kind { get; init; }
    public double X { get; init; }
    public double Y { get; init; }
}
