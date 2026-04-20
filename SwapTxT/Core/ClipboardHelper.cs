using SwapTxT.Models;
using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace SwapTxT.Core
{
    /// <summary>
    /// Handles clipboard manipulation for translation.
    /// Manual mode  — translates only the user-highlighted (selected) text.
    /// Auto mode    — translates the currently highlighted text; if nothing is
    ///                highlighted it falls back to finding the last source-language
    ///                block in the entire field.
    /// All clipboard operations run on an STA thread.
    /// </summary>
    public static class ClipboardHelper
    {
        /// <summary>
        /// Executes the translation workflow.
        /// Returns the original text that was translated (or null if nothing was done).
        /// </summary>
        public static async Task<(string? original, string? translated)> TranslateAtCursorAsync(
            AppSettings settings,
            Func<string, string, string, Task<string>> translateFunc)
        {
            if (settings.Mode == TranslationMode.Manual)
                return await DoManualModeAsync(settings, translateFunc);
            else
                return await DoAutoModeAsync(settings, translateFunc);
        }

        // ─── Win32 P/Invoke Declarations ─────────────────────────────────────────
        [DllImport("user32.dll", SetLastError = true)]
        static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        static extern IntPtr GetKeyboardLayout(uint idThread);

        private const uint INPUT_KEYBOARD  = 1;
        private const uint KEYEVENTF_KEYUP = 0x0002;
        private const ushort VK_CONTROL    = 0x11;
        private const ushort VK_SHIFT      = 0x10;
        private const ushort VK_C          = 0x43;
        private const ushort VK_V          = 0x56;
        private const ushort VK_A          = 0x41;
        private const ushort VK_INSERT     = 0x2D;
        private const uint   WM_PASTE      = 0x0302;

        [StructLayout(LayoutKind.Sequential)]
        struct INPUT
        {
            public uint type;
            public KEYBDINPUT ki;
            // Padding to match union size on 64-bit
            public int _padding1;
            public int _padding2;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint   dwFlags;
            public uint   time;
            public IntPtr dwExtraInfo;
        }

        private static INPUT KeyDown(ushort vk) => new INPUT
        {
            type = INPUT_KEYBOARD,
            ki   = new KEYBDINPUT { wVk = vk }
        };

        private static INPUT KeyUp(ushort vk) => new INPUT
        {
            type = INPUT_KEYBOARD,
            ki   = new KEYBDINPUT { wVk = vk, dwFlags = KEYEVENTF_KEYUP }
        };

        // Ctrl+Insert — layout-agnostic copy
        private static void SendCtrlC()
        {
            var inputs = new[]
            {
                KeyDown(VK_CONTROL),
                KeyDown(VK_INSERT),
                KeyUp(VK_INSERT),
                KeyUp(VK_CONTROL),
            };
            SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
        }

        // Ctrl+V via SendInput (reliable on all apps)
        // Falls back to WM_PASTE message to the target window handle.
        private static void SendCtrlV(IntPtr targetHwnd)
        {
            // First: restore focus to the original app window
            if (targetHwnd != IntPtr.Zero)
                SetForegroundWindow(targetHwnd);
            Thread.Sleep(30); // brief wait for focus to settle

            // Method 1: SendInput Ctrl+V
            var inputs = new[]
            {
                KeyDown(VK_CONTROL),
                KeyDown(VK_V),
                KeyUp(VK_V),
                KeyUp(VK_CONTROL),
            };
            SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());

            Thread.Sleep(40);

            // Method 2: WM_PASTE — works for Win32 edit controls, terminals etc.
            if (targetHwnd != IntPtr.Zero)
                PostMessage(targetHwnd, WM_PASTE, IntPtr.Zero, IntPtr.Zero);
        }

        // Ctrl+A via SendInput
        private static void SendCtrlA()
        {
            var inputs = new[]
            {
                KeyDown(VK_CONTROL),
                KeyDown(VK_A),
                KeyUp(VK_A),
                KeyUp(VK_CONTROL),
            };
            SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
        }

        // ─── Manual Mode ─────────────────────────────────────────────────────────
        /// <summary>
        /// Copies current selection, translates it, pastes back in-place.
        /// Works regardless of the active keyboard language.
        /// </summary>
        private static async Task<(string? original, string? translated)> DoManualModeAsync(
            AppSettings settings,
            Func<string, string, string, Task<string>> translateFunc)
        {
            // Capture the target window NOW before any async gaps
            IntPtr targetHwnd = GetForegroundWindow();

            string? backup = GetClipboardSafe();

            // Copy selected text
            SendCtrlC();
            await Task.Delay(120);

            string original = GetClipboardSafe() ?? "";
            if (string.IsNullOrWhiteSpace(original))
            {
                RestoreClipboard(backup);
                return (null, null);
            }

            // Translate
            string translated = await translateFunc(original, settings.SourceLang, settings.TargetLang);
            if (string.IsNullOrEmpty(translated))
            {
                RestoreClipboard(backup);
                return (original, null);
            }

            // Paste back — restore focus to original window, then paste
            SetClipboardSafe(translated);
            await Task.Delay(200);
            SendCtrlV(targetHwnd);
            await Task.Delay(300);

            RestoreClipboard(backup);
            return (original, translated);
        }

        // ─── Auto Mode ────────────────────────────────────────────────────────────
        /// <summary>
        /// In Auto mode:
        ///   1. First try to read the currently selected (highlighted) text and translate it.
        ///   2. If nothing is selected, read the ENTIRE field with Ctrl+A → Ctrl+C and
        ///      translate all of it — this handles mixed-language text like
        ///      "ฉัน hungry. มากๆ" as a single unit instead of only the last block.
        /// </summary>
        private static async Task<(string? original, string? translated)> DoAutoModeAsync(
            AppSettings settings,
            Func<string, string, string, Task<string>> translateFunc)
        {
            // Capture the target window NOW before any async gaps or focus changes
            IntPtr targetHwnd = GetForegroundWindow();

            string? backup = GetClipboardSafe();

            // ── Step 1: Try to read currently selected text ───────────────────
            ClearClipboardSafe();
            await Task.Delay(50);
            SendCtrlC();
            await Task.Delay(120);
            string selected = GetClipboardSafe() ?? "";

            string? textToTranslate;

            if (!string.IsNullOrWhiteSpace(selected))
            {
                // User has text highlighted — translate exactly that
                textToTranslate = selected.Trim();
            }
            else
            {
                // ── Step 2: No selection — select all and translate the whole field
                SendCtrlA();
                await Task.Delay(80);
                SendCtrlC();
                await Task.Delay(120);

                string fullText = GetClipboardSafe() ?? "";
                if (string.IsNullOrWhiteSpace(fullText))
                {
                    RestoreClipboard(backup);
                    return (null, null);
                }

                textToTranslate = fullText.Trim();
            }

            if (string.IsNullOrEmpty(textToTranslate))
            {
                RestoreClipboard(backup);
                return (null, null);
            }

            // ── Step 3: Translate ─────────────────────────────────────────────
            string translated = await translateFunc(textToTranslate, settings.SourceLang, settings.TargetLang);
            if (string.IsNullOrEmpty(translated))
            {
                RestoreClipboard(backup);
                return (textToTranslate, null);
            }

            // ── Step 4: Paste back — use the saved HWND to ensure correct target
            SetClipboardSafe(translated);
            await Task.Delay(200);
            SendCtrlV(targetHwnd);
            await Task.Delay(300);

            RestoreClipboard(backup);
            return (textToTranslate, translated);
        }


        // ─── Smart Language Block Detection ──────────────────────────────────────
        /// <summary>
        /// Finds the last contiguous block of characters matching the source language.
        /// TH→EN source = Thai Unicode (\u0E00–\u0E7F).
        /// EN→TH source = Latin letters (A-Za-z) + common English punctuation.
        /// </summary>
        public static string? FindLastSourceBlock(string text, LanguageDirection direction)
        {
            if (string.IsNullOrEmpty(text)) return null;

            bool IsSourceChar(char c) => direction == LanguageDirection.ThToEn
                ? IsThaiChar(c)
                : IsLatinChar(c);

            int end = -1;
            int start = -1;

            for (int i = text.Length - 1; i >= 0; i--)
            {
                if (IsSourceChar(text[i]))
                {
                    if (end == -1) end = i;
                    start = i;
                }
                else if (end != -1)
                {
                    // Allow short gaps (spaces/punctuation) inside blocks
                    int nonSourceCount = 0;
                    int j = i;
                    while (j >= 0 && !IsSourceChar(text[j]))
                    {
                        nonSourceCount++;
                        j--;
                    }
                    if (nonSourceCount > 3 || j < 0)
                    {
                        start = i + 1;
                        break;
                    }
                }
            }

            if (end == -1) return null;

            string block = text.Substring(start, end - start + 1).Trim();
            return string.IsNullOrWhiteSpace(block) ? null : block;
        }

        private static bool IsThaiChar(char c) =>
            c >= '\u0E00' && c <= '\u0E7F';

        private static bool IsLatinChar(char c) =>
            (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') ||
            c == '\'' || c == '-';

        // ─── STA-Safe Clipboard Helpers ───────────────────────────────────────────
        private static string? GetClipboardSafe()
        {
            string? result = null;
            RunOnSta(() =>
            {
                for (int i = 0; i < 5; i++)
                {
                    try
                    {
                        if (System.Windows.Clipboard.ContainsText())
                            result = System.Windows.Clipboard.GetText();
                        break;
                    }
                    catch { Thread.Sleep(30); }
                }
            });
            return result;
        }

        private static void ClearClipboardSafe()
        {
            RunOnSta(() =>
            {
                try { System.Windows.Clipboard.Clear(); }
                catch { }
            });
        }

        private static void SetClipboardSafe(string text)
        {
            if (text == null) return;

            RunOnSta(() =>
            {
                for (int i = 0; i < 5; i++)
                {
                    try
                    {
                        // SetDataObject with copy=true tells Windows to keep this data
                        // alive even after this thread exits — the critical fix for data loss.
                        System.Windows.Clipboard.SetDataObject(text, true);
                        break;
                    }
                    catch { Thread.Sleep(30 * (i + 1)); }
                }
            });
        }

        private static void RestoreClipboard(string? text)
        {
            if (text == null) return;
            SetClipboardSafe(text);
        }

        private static void RunOnSta(Action action)
        {
            if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA)
            {
                action();
            }
            else
            {
                var t = new Thread(() => action());
                t.SetApartmentState(ApartmentState.STA);
                t.Start();
                t.Join();
            }
        }
    }
}
