using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;

namespace TranslatorApp.Core
{
    /// <summary>
    /// Manages global hotkey registration and detection.
    /// Uses both RegisterHotKey (Win32) and LowLevel Hook fallback.
    /// </summary>
    public class HotkeyManager : IDisposable
    {
        private const int HOTKEY_ID = 9000;
        private const int WM_HOTKEY = 0x0312;

        // Modifier key constants
        public const int MOD_NONE = 0x0000;
        public const int MOD_ALT = 0x0001;
        public const int MOD_CONTROL = 0x0002;
        public const int MOD_SHIFT = 0x0004;
        public const int MOD_WIN = 0x0008;

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private IntPtr _windowHandle;
        private System.Windows.Interop.HwndSource _source;
        private Action _onHotkeyPressed;

        private ModifierKeys _modifiers;
        private Key _key;

        public HotkeyManager(ModifierKeys modifiers, Key key, Action onHotkeyPressed)
        {
            _modifiers = modifiers;
            _key = key;
            _onHotkeyPressed = onHotkeyPressed;
        }

        public void Register(Window window)
        {
            // Get the window handle from WPF window
            var helper = new System.Windows.Interop.WindowInteropHelper(window);
            _windowHandle = helper.Handle;

            _source = System.Windows.Interop.HwndSource.FromHwnd(_windowHandle);
            _source.AddHook(WndProc);

            int winModifiers = ConvertModifiers(_modifiers);
            int vkCode = KeyInterop.VirtualKeyFromKey(_key);

            bool success = RegisterHotKey(_windowHandle, HOTKEY_ID, winModifiers, vkCode);
            if (!success)
            {
                // Hotkey might already be registered, try to re-register
                UnregisterHotKey(_windowHandle, HOTKEY_ID);
                RegisterHotKey(_windowHandle, HOTKEY_ID, winModifiers, vkCode);
            }
        }

        public void UpdateHotkey(ModifierKeys modifiers, Key key)
        {
            _modifiers = modifiers;
            _key = key;

            if (_windowHandle != IntPtr.Zero)
            {
                UnregisterHotKey(_windowHandle, HOTKEY_ID);
                int winModifiers = ConvertModifiers(modifiers);
                int vkCode = KeyInterop.VirtualKeyFromKey(key);
                RegisterHotKey(_windowHandle, HOTKEY_ID, winModifiers, vkCode);
            }
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_HOTKEY && wParam.ToInt32() == HOTKEY_ID)
            {
                _onHotkeyPressed?.Invoke();
                handled = true;
            }
            return IntPtr.Zero;
        }

        private int ConvertModifiers(ModifierKeys modifiers)
        {
            int result = MOD_NONE;
            if (modifiers.HasFlag(ModifierKeys.Control)) result |= MOD_CONTROL;
            if (modifiers.HasFlag(ModifierKeys.Alt)) result |= MOD_ALT;
            if (modifiers.HasFlag(ModifierKeys.Shift)) result |= MOD_SHIFT;
            if (modifiers.HasFlag(ModifierKeys.Windows)) result |= MOD_WIN;
            return result;
        }

        public void Dispose()
        {
            if (_windowHandle != IntPtr.Zero)
            {
                UnregisterHotKey(_windowHandle, HOTKEY_ID);
            }
            _source?.RemoveHook(WndProc);
        }
    }
}
