using System.Runtime.InteropServices;
using System.Windows.Forms;
using KeyboardMouseOverlay.Models;

namespace KeyboardMouseOverlay.Core;

public sealed class InputMonitor : IDisposable
{
    public event EventHandler<InputAction>? ActionTriggered;

    private readonly object _syncRoot = new();
    private readonly HashSet<Keys> _modifierKeys = new();
    private readonly HashSet<Keys> _activeNonModifierKeys = new();
    private readonly HashSet<Keys> _pressedKeys = new();
    private readonly NativeMethods.HookProc _keyboardHookCallback;
    private readonly NativeMethods.HookProc _mouseHookCallback;
    private IntPtr _keyboardHookId;
    private IntPtr _mouseHookId;
    private DateTime _lastMouseDownTime = DateTime.MinValue;
    private Keys _lastMouseButton = Keys.None;

    public InputMonitor()
    {
        _keyboardHookCallback = KeyboardHookCallback;
        _mouseHookCallback = MouseHookCallback;
    }

    public bool IsRunning => _keyboardHookId != IntPtr.Zero || _mouseHookId != IntPtr.Zero;

    public void Start()
    {
        lock (_syncRoot)
        {
            if (IsRunning)
            {
                return;
            }

            try
            {
                _keyboardHookId = NativeMethods.SetWindowsHookEx(NativeMethods.WH_KEYBOARD_LL, _keyboardHookCallback, IntPtr.Zero, 0);
                _mouseHookId = NativeMethods.SetWindowsHookEx(NativeMethods.WH_MOUSE_LL, _mouseHookCallback, IntPtr.Zero, 0);

                if (_keyboardHookId == IntPtr.Zero || _mouseHookId == IntPtr.Zero)
                {
                    throw new InvalidOperationException("Unable to install both global input hooks.");
                }
            }
            catch
            {
                Stop();
                throw;
            }
        }
    }

    public void Stop()
    {
        lock (_syncRoot)
        {
            if (_keyboardHookId != IntPtr.Zero)
            {
                NativeMethods.UnhookWindowsHookEx(_keyboardHookId);
                _keyboardHookId = IntPtr.Zero;
            }

            if (_mouseHookId != IntPtr.Zero)
            {
                NativeMethods.UnhookWindowsHookEx(_mouseHookId);
                _mouseHookId = IntPtr.Zero;
            }

            _modifierKeys.Clear();
            _activeNonModifierKeys.Clear();
            _pressedKeys.Clear();
        }
    }

    private IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            var structure = Marshal.PtrToStructure<NativeMethods.KBDLLHOOKSTRUCT>(lParam);
            var key = (Keys)structure.vkCode;
            var rawEventType = wParam.ToInt32();
            if (rawEventType == NativeMethods.WM_KEYDOWN || rawEventType == NativeMethods.WM_SYSKEYDOWN)
            {
                if (_pressedKeys.Add(key))
                {
                    HandleKeyDown(key);
                }
            }
            else if (rawEventType == NativeMethods.WM_KEYUP || rawEventType == NativeMethods.WM_SYSKEYUP)
            {
                _pressedKeys.Remove(key);
                HandleKeyUp(key);
            }
        }

        return NativeMethods.CallNextHookEx(_keyboardHookId, nCode, wParam, lParam);
    }

    private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            var hookStruct = Marshal.PtrToStructure<NativeMethods.MSLLHOOKSTRUCT>(lParam);
            var message = wParam.ToInt32();

            switch (message)
            {
                case NativeMethods.WM_LBUTTONDOWN:
                    RaiseMouseAction("Left Click", Keys.LButton, hookStruct.pt.x, hookStruct.pt.y);
                    break;
                case NativeMethods.WM_RBUTTONDOWN:
                    RaiseMouseAction("Right Click", Keys.RButton, hookStruct.pt.x, hookStruct.pt.y);
                    break;
                case NativeMethods.WM_MBUTTONDOWN:
                    RaiseMouseAction("Middle Click", Keys.MButton, hookStruct.pt.x, hookStruct.pt.y);
                    break;
                case NativeMethods.WM_MOUSEWHEEL:
                    var delta = (short)((hookStruct.mouseData >> 16) & 0xFFFF);
                    RaiseMouseAction(delta >= 0 ? "Scroll Up" : "Scroll Down", Keys.None, hookStruct.pt.x, hookStruct.pt.y);
                    break;
            }
        }

        return NativeMethods.CallNextHookEx(_mouseHookId, nCode, wParam, lParam);
    }

    private void HandleKeyDown(Keys key)
    {
        if (IsModifierKey(key))
        {
            _modifierKeys.Add(key);
        }
        else
        {
            _activeNonModifierKeys.Clear();
            _activeNonModifierKeys.Add(key);
        }

        var label = BuildShortcutLabel();
        if (!string.IsNullOrWhiteSpace(label))
        {
            var cursorPosition = GetCursorPosition();
            RaiseKeyboardAction(label, cursorPosition.X, cursorPosition.Y);
        }
    }

    private void HandleKeyUp(Keys key)
    {
        if (IsModifierKey(key))
        {
            _modifierKeys.Remove(key);
        }
        else
        {
            _activeNonModifierKeys.Remove(key);
        }
    }

    private string? BuildShortcutLabel()
    {
        var sequence = new List<string>();

        foreach (var modifier in new[] { Keys.LControlKey, Keys.RControlKey, Keys.LShiftKey, Keys.RShiftKey, Keys.LMenu, Keys.RMenu, Keys.LWin, Keys.RWin })
        {
            if (_modifierKeys.Contains(modifier))
            {
                sequence.Add(GetModifierName(modifier));
            }
        }

        foreach (var key in _activeNonModifierKeys)
        {
            sequence.Add(GetKeyName(key));
        }

        if (sequence.Count == 0)
        {
            return null;
        }

        return string.Join(" + ", sequence);
    }

    private void RaiseMouseAction(string label, Keys button, int x, int y)
    {
        if (button != Keys.None && button == _lastMouseButton && (DateTime.UtcNow - _lastMouseDownTime).TotalMilliseconds < 300)
        {
            label = "Double Click";
        }

        _lastMouseButton = button;
        _lastMouseDownTime = DateTime.UtcNow;

        RaiseAction(new InputAction
        {
            Label = label,
            Kind = ActionKind.Mouse,
            X = x,
            Y = y,
        });
    }

    private void RaiseKeyboardAction(string label, double x, double y)
    {
        RaiseAction(new InputAction
        {
            Label = label,
            Kind = ActionKind.Keyboard,
            X = x,
            Y = y,
        });
    }

    private void RaiseAction(InputAction action)
    {
        ActionTriggered?.Invoke(this, action);
    }

    private static Point GetCursorPosition()
    {
        NativeMethods.GetCursorPos(out var point);
        return new Point(point.x, point.y);
    }

    private static bool IsModifierKey(Keys key)
    {
        return key is Keys.LControlKey or Keys.RControlKey or Keys.LShiftKey or Keys.RShiftKey or Keys.LMenu or Keys.RMenu or Keys.LWin or Keys.RWin;
    }

    private static string GetModifierName(Keys key)
    {
        return key switch
        {
            Keys.LControlKey or Keys.RControlKey => "Ctrl",
            Keys.LShiftKey or Keys.RShiftKey => "Shift",
            Keys.LMenu or Keys.RMenu => "Alt",
            Keys.LWin or Keys.RWin => "Win",
            _ => "Key",
        };
    }

    private static string GetKeyName(Keys key)
    {
        return key switch
        {
            Keys.Space => "Space",
            Keys.Enter => "Enter",
            Keys.Escape => "Esc",
            Keys.Tab => "Tab",
            Keys.Back => "Backspace",
            Keys.Up => "↑",
            Keys.Down => "↓",
            Keys.Left => "←",
            Keys.Right => "→",
            Keys.F1 => "F1",
            Keys.F2 => "F2",
            Keys.F3 => "F3",
            Keys.F4 => "F4",
            Keys.F5 => "F5",
            Keys.F6 => "F6",
            Keys.F7 => "F7",
            Keys.F8 => "F8",
            Keys.F9 => "F9",
            Keys.F10 => "F10",
            Keys.F11 => "F11",
            Keys.F12 => "F12",
            _ => key.ToString().Length == 1 ? key.ToString().ToUpperInvariant() : key.ToString(),
        };
    }

    public void Dispose()
    {
        Stop();
    }

    private static class NativeMethods
    {
        public const int WH_KEYBOARD_LL = 13;
        public const int WH_MOUSE_LL = 14;
        public const int WM_KEYDOWN = 0x0100;
        public const int WM_KEYUP = 0x0101;
        public const int WM_SYSKEYDOWN = 0x0104;
        public const int WM_SYSKEYUP = 0x0105;
        public const int WM_LBUTTONDOWN = 0x0201;
        public const int WM_RBUTTONDOWN = 0x0204;
        public const int WM_MBUTTONDOWN = 0x0207;
        public const int WM_MOUSEWHEEL = 0x020A;

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MSLLHOOKSTRUCT
        {
            public POINT pt;
            public int mouseData;
            public int flags;
            public int time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct KBDLLHOOKSTRUCT
        {
            public int vkCode;
            public int scanCode;
            public int flags;
            public int time;
            public IntPtr dwExtraInfo;
        }

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool UnhookWindowsHookEx(IntPtr hHook);

        [DllImport("user32.dll")]
        public static extern IntPtr CallNextHookEx(IntPtr hHook, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern bool GetCursorPos(out POINT lpPoint);

        public delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);
    }
}

public readonly record struct Point(double X, double Y);
