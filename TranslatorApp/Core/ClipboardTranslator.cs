using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Runtime.InteropServices;

namespace TranslatorApp.Core
{
    /// <summary>
    /// Core engine: captures selected text via clipboard, translates it,
    /// and pastes the translation back in place.
    /// </summary>
    public class ClipboardTranslator
    {
        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, int dwFlags, int dwExtraInfo);

        private const byte VK_CONTROL = 0x11;
        private const byte VK_C = 0x43;
        private const byte VK_V = 0x56;
        private const int KEYEVENTF_KEYDOWN = 0x0000;
        private const int KEYEVENTF_KEYUP = 0x0002;

        private ITranslationEngine _engine;
        private bool _isTranslating = false;

        public event EventHandler<TranslationEventArgs> TranslationStarted;
        public event EventHandler<TranslationEventArgs> TranslationCompleted;
        public event EventHandler<TranslationEventArgs> TranslationFailed;
        public event EventHandler<TranslationEventArgs> NotificationRequested;

        public void SetEngine(ITranslationEngine engine)
        {
            _engine = engine;
        }

        /// <summary>
        /// Main workflow: Copy → Translate → Paste
        /// Must be called from STA thread for Clipboard access.
        /// </summary>
        public async Task TranslateSelectedTextAsync()
        {
            if (_isTranslating) return;
            if (_engine == null)
            {
                TranslationFailed?.Invoke(this, new TranslationEventArgs
                {
                    ErrorMessage = "ยังไม่ได้ตั้งค่า Translation Engine"
                });
                return;
            }

            _isTranslating = true;

            try
            {
                // Save original clipboard content
                string originalClipboard = null;
                bool hadText = false;

            await System.Windows.Application.Current.Dispatcher.BeginInvoke((Action)(() => { })); // Not used, just for context
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => { }); // Not used, just for context
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                try
                {
                    if (System.Windows.Clipboard.ContainsText())
                    {
                        originalClipboard = System.Windows.Clipboard.GetText();
                        hadText = true;
                    }
                    // Clear clipboard to detect if copy worked
                    System.Windows.Clipboard.Clear();
                }
                catch { }
            });

            // Step 1: Send Ctrl+C to copy selected text
            SendCopy();
            await Task.Delay(150); // Wait for OS to process copy

            // Step 2: Read from clipboard
            string textToTranslate = string.Empty;

            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                try
                {
                    if (System.Windows.Clipboard.ContainsText())
                    {
                        textToTranslate = System.Windows.Clipboard.GetText();
                    }
                }
                catch { }
            });

            if (string.IsNullOrWhiteSpace(textToTranslate))
            {
                // Restore original clipboard
                if (hadText && originalClipboard != null)
                {
                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        try { System.Windows.Clipboard.SetText(originalClipboard); } catch { }
                    });
                }

                NotificationRequested?.Invoke(this, new TranslationEventArgs
                {
                    ErrorMessage = "กรุณาเลือกข้อความก่อนกดคีย์ลัดนะคะ 📝"
                });
                return;
            }

            // Notify started
            TranslationStarted?.Invoke(this, new TranslationEventArgs
            {
                OriginalText = textToTranslate
            });

            // Step 3: Translate
            string translatedText = await _engine.TranslateAsync(textToTranslate);

            if (string.IsNullOrEmpty(translatedText))
            {
                throw new Exception("ได้รับข้อความว่างจาก Translation Engine");
            }

            // Step 4: Put translated text to clipboard
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                try { System.Windows.Clipboard.SetText(translatedText); } catch { }
            });

            await Task.Delay(80);

            // Step 5: Paste (Ctrl+V)
            SendPaste();

            await Task.Delay(100);

            // Notify success
            TranslationCompleted?.Invoke(this, new TranslationEventArgs
            {
                OriginalText = textToTranslate,
                TranslatedText = translatedText
            });

            // Restore original clipboard after a short delay
            await Task.Delay(500);
            if (hadText && originalClipboard != null)
            {
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    try { System.Windows.Clipboard.SetText(originalClipboard); } catch { }
                });
            }
            }
            catch (Exception ex)
            {
                TranslationFailed?.Invoke(this, new TranslationEventArgs
                {
                    ErrorMessage = $"เกิดข้อผิดพลาด: {ex.Message}"
                });
            }
            finally
            {
                _isTranslating = false;
            }
        }

        private void SendCopy()
        {
            // Press Ctrl+C using keybd_event (more reliable than SendKeys)
            keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYDOWN, 0);
            keybd_event(VK_C, 0, KEYEVENTF_KEYDOWN, 0);
            Thread.Sleep(30);
            keybd_event(VK_C, 0, KEYEVENTF_KEYUP, 0);
            keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, 0);
        }

        private void SendPaste()
        {
            // Press Ctrl+V using keybd_event
            keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYDOWN, 0);
            keybd_event(VK_V, 0, KEYEVENTF_KEYDOWN, 0);
            Thread.Sleep(30);
            keybd_event(VK_V, 0, KEYEVENTF_KEYUP, 0);
            keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, 0);
        }
    }

    public class TranslationEventArgs : EventArgs
    {
        public string OriginalText { get; set; }
        public string TranslatedText { get; set; }
        public string ErrorMessage { get; set; }
    }
}
