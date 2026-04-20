using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace SwapTxT.Core
{
    /// <summary>
    /// Registers a system-wide hotkey using Win32 RegisterHotKey API.
    /// Fires the HotkeyPressed event when the hotkey is detected.
    /// </summary>
    public class GlobalHotkey : IDisposable
    {
        private const int WM_HOTKEY = 0x0312;
        private const int HOTKEY_ID = 9000;

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private HwndSource? _source;
        private Window? _window;

        public event Action? HotkeyPressed;

        private int _modifiers;
        private int _key;

        public void Register(Window owner, int modifiers, int key)
        {
            _modifiers = modifiers;
            _key = key;
            _window = owner;

            var helper = new WindowInteropHelper(owner);
            helper.EnsureHandle();
            _source = HwndSource.FromHwnd(helper.Handle);
            _source?.AddHook(HwndHook);

            UnregisterHotKey(helper.Handle, HOTKEY_ID);
            bool ok = RegisterHotKey(helper.Handle, HOTKEY_ID, modifiers, key);
            if (!ok)
                throw new InvalidOperationException($"Failed to register hotkey (modifiers={modifiers}, key={key}). It may be in use by another app.");
        }

        public void Reregister(int modifiers, int key)
        {
            if (_window == null) return;
            _modifiers = modifiers;
            _key = key;
            var handle = new WindowInteropHelper(_window).Handle;
            UnregisterHotKey(handle, HOTKEY_ID);
            RegisterHotKey(handle, HOTKEY_ID, modifiers, key);
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_HOTKEY && wParam.ToInt32() == HOTKEY_ID)
            {
                HotkeyPressed?.Invoke();
                handled = true;
            }
            return IntPtr.Zero;
        }

        public void Dispose()
        {
            if (_window != null)
            {
                var handle = new WindowInteropHelper(_window).Handle;
                UnregisterHotKey(handle, HOTKEY_ID);
            }
            _source?.RemoveHook(HwndHook);
        }
    }
}
