using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using TranslatorApp.Core;

namespace TranslatorApp
{
    public partial class MainWindow : Window
    {
        private NotifyIcon _trayIcon;
        private HotkeyManager _hotkeyManager;
        private ClipboardTranslator _translator;
        private AppSettings _settings;
        private SettingsWindow _settingsWindow;
        private ToastNotification _toast;

        // ─── Startup ───────────────────────────────────────────

        public MainWindow()
        {
            InitializeComponent();
            _settings = AppSettings.Load();
            _translator = new ClipboardTranslator();
            _toast = new ToastNotification();

            // Wire translation events
            _translator.TranslationStarted += OnTranslationStarted;
            _translator.TranslationCompleted += OnTranslationCompleted;
            _translator.TranslationFailed += OnTranslationFailed;
            _translator.NotificationRequested += OnNotificationRequested;

            // Apply engine from settings
            ApplyEngineFromSettings();
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Initialize System Tray icon
            InitializeTrayIcon();

            // Register global hotkey
            InitializeHotkey();

            // Show welcome toast
            ShowToast("✨ TranslatorApp พร้อมใช้งานแล้วค่ะ!", $"กด {_settings.HotkeyModifiers}+{_settings.HotkeyKey} เพื่อแปลข้อความที่เลือก", ToastType.Success);
        }

        // ─── System Tray ───────────────────────────────────────

        private void InitializeTrayIcon()
        {
            _trayIcon = new NotifyIcon
            {
                Visible = true,
                Text = "TranslatorApp - แปลภาษาอัตโนมัติ",
                Icon = CreateAppIcon()
            };

            // Left click → open settings
            _trayIcon.Click += (s, e) =>
            {
                if (((System.Windows.Forms.MouseEventArgs)e).Button == MouseButtons.Left)
                {
                    OpenSettings();
                }
            };

            // Right-click context menu
            var contextMenu = new ContextMenuStrip();
            contextMenu.BackColor = System.Drawing.Color.FromArgb(26, 26, 46);
            contextMenu.ForeColor = System.Drawing.Color.White;
            contextMenu.Font = new Font("Segoe UI", 9.5f);
            contextMenu.Renderer = new DarkMenuRenderer();

            var settingsItem = new ToolStripMenuItem("⚙️  ตั้งค่า");
            settingsItem.Click += (s, e) => OpenSettings();

            var donateItem = new ToolStripMenuItem("☕  เลี้ยงกาแฟนักพัฒนา");
            donateItem.Click += (s, e) => OpenDonate();

            var helpItem = new ToolStripMenuItem("❓  วิธีใช้งาน");
            helpItem.Click += (s, e) => OpenHelp();

            var separator = new ToolStripSeparator();

            var exitItem = new ToolStripMenuItem("✖️  ปิดโปรแกรม");
            exitItem.Click += (s, e) => ExitApp();

            contextMenu.Items.Add(settingsItem);
            contextMenu.Items.Add(donateItem);
            contextMenu.Items.Add(helpItem);
            contextMenu.Items.Add(separator);
            contextMenu.Items.Add(exitItem);

            _trayIcon.ContextMenuStrip = contextMenu;
        }

        private Icon CreateAppIcon()
        {
            // Create a simple icon programmatically (anime girl mascot emoji style)
            // In production, replace with actual .ico file resource
            try
            {
                // Try to load from resources
                var iconStream = System.Reflection.Assembly.GetExecutingAssembly()
                    .GetManifestResourceStream("TranslatorApp.Resources.icon.ico");
                if (iconStream != null)
                    return new Icon(iconStream);
            }
            catch { }

            // Fallback: create a colored icon
            var bmp = new System.Drawing.Bitmap(32, 32);
            using (var g = System.Drawing.Graphics.FromImage(bmp))
            {
                g.Clear(System.Drawing.Color.FromArgb(15, 15, 26));

                // Draw a simple "T" icon with pink color
                using var brush = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(255, 107, 157));
                using var font = new System.Drawing.Font("Segoe UI", 18f, System.Drawing.FontStyle.Bold);
                g.DrawString("T", font, brush, 4f, 2f);
            }
            var handle = bmp.GetHicon();
            return System.Drawing.Icon.FromHandle(handle);
        }

        // ─── Hotkey ────────────────────────────────────────────

        private void InitializeHotkey()
        {
            var (modifiers, key) = ParseHotkey(_settings.HotkeyModifiers, _settings.HotkeyKey);
            _hotkeyManager = new HotkeyManager(modifiers, key, OnHotkeyTriggered);
            _hotkeyManager.Register(this);
        }

        private (ModifierKeys, Key) ParseHotkey(string modStr, string keyStr)
        {
            ModifierKeys modifiers = ModifierKeys.None;
            if (modStr.Contains("Ctrl")) modifiers |= ModifierKeys.Control;
            if (modStr.Contains("Alt")) modifiers |= ModifierKeys.Alt;
            if (modStr.Contains("Shift")) modifiers |= ModifierKeys.Shift;

            if (!Enum.TryParse<Key>(keyStr, out Key key))
                key = Key.Space;

            return (modifiers, key);
        }

        private void OnHotkeyTriggered()
        {
            // Run translation on background thread, clipboard access on STA dispatcher
            Task.Run(async () =>
            {
                await _translator.TranslateSelectedTextAsync();
            });
        }

        // ─── Translation Events ────────────────────────────────

        private void OnTranslationStarted(object sender, TranslationEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                _trayIcon.Text = "🔄 กำลังแปล...";
            });
        }

        private void OnTranslationCompleted(object sender, TranslationEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                _trayIcon.Text = "TranslatorApp - แปลภาษาอัตโนมัติ";

                if (_settings.ShowNotificationToast)
                {
                    string preview = e.TranslatedText?.Length > 50
                        ? e.TranslatedText[..50] + "..."
                        : e.TranslatedText;
                    ShowToast("✅ แปลสำเร็จ!", preview, ToastType.Success);
                }
            });
        }

        private void OnTranslationFailed(object sender, TranslationEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                _trayIcon.Text = "TranslatorApp - แปลภาษาอัตโนมัติ";
                ShowToast("❌ แปลไม่สำเร็จ", e.ErrorMessage, ToastType.Error);
            });
        }

        private void OnNotificationRequested(object sender, TranslationEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                ShowToast("⚠️ แจ้งเตือน", e.ErrorMessage, ToastType.Warning);
            });
        }

        // ─── Settings ──────────────────────────────────────────

        private void OpenSettings()
        {
            if (_settingsWindow != null && _settingsWindow.IsLoaded)
            {
                _settingsWindow.Activate();
                _settingsWindow.Focus();
                return;
            }

            _settingsWindow = new SettingsWindow(_settings);
            _settingsWindow.SettingsSaved += OnSettingsSaved;
            _settingsWindow.Show();
        }

        private void OnSettingsSaved(object sender, AppSettings newSettings)
        {
            _settings = newSettings;

            // Update hotkey
            var (modifiers, key) = ParseHotkey(_settings.HotkeyModifiers, _settings.HotkeyKey);
            _hotkeyManager.UpdateHotkey(modifiers, key);

            // Update translation engine
            ApplyEngineFromSettings();
        }

        private void ApplyEngineFromSettings()
        {
            ITranslationEngine engine = _settings.TranslationEngine switch
            {
                "OpenAI" => new OpenAITranslateEngine(_settings.OpenAIApiKey, _settings.AITone),
                "Gemini" => new GeminiTranslateEngine(_settings.GeminiApiKey, _settings.AITone),
                _ => new GoogleTranslateEngine()
            };
            _translator.SetEngine(engine);
        }

        // ─── Help & Donate ─────────────────────────────────────

        private void OpenDonate()
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = _settings.DonateUrl,
                UseShellExecute = true
            });
        }

        private void OpenHelp()
        {
            var helpWin = new HelpWindow();
            helpWin.Show();
        }

        // ─── Toast Notification ────────────────────────────────

        private void ShowToast(string title, string message, ToastType type = ToastType.Info)
        {
            _toast.Show(title, message, type);
        }

        // ─── Cleanup ───────────────────────────────────────────

        private void ExitApp()
        {
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
            _hotkeyManager?.Dispose();
            _toast?.Dispose();
            System.Windows.Application.Current.Shutdown();
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true; // Prevent closing - only exit via tray menu
        }
    }

    // ─── Dark Context Menu Renderer ────────────────────────────

    public class DarkMenuRenderer : ToolStripProfessionalRenderer
    {
        public DarkMenuRenderer() : base(new DarkColorTable()) { }
    }

    public class DarkColorTable : ProfessionalColorTable
    {
        public override System.Drawing.Color MenuItemSelected =>
            System.Drawing.Color.FromArgb(37, 37, 69);
        public override System.Drawing.Color MenuItemBorder =>
            System.Drawing.Color.FromArgb(255, 107, 157);
        public override System.Drawing.Color MenuBorder =>
            System.Drawing.Color.FromArgb(42, 42, 74);
        public override System.Drawing.Color ToolStripDropDownBackground =>
            System.Drawing.Color.FromArgb(26, 26, 46);
        public override System.Drawing.Color ImageMarginGradientBegin =>
            System.Drawing.Color.FromArgb(22, 22, 38);
        public override System.Drawing.Color ImageMarginGradientMiddle =>
            System.Drawing.Color.FromArgb(22, 22, 38);
        public override System.Drawing.Color ImageMarginGradientEnd =>
            System.Drawing.Color.FromArgb(22, 22, 38);
        public override System.Drawing.Color SeparatorDark =>
            System.Drawing.Color.FromArgb(42, 42, 74);
        public override System.Drawing.Color SeparatorLight =>
            System.Drawing.Color.FromArgb(50, 50, 80);
    }
}
