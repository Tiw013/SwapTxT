using AutoUpdaterDotNET;
using Hardcodet.Wpf.TaskbarNotification;
using SwapTxT.Core;
using SwapTxT.Models;
using SwapTxT.Services;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using WinForms = System.Windows.Forms;
using WpfApp = System.Windows.Application;

namespace SwapTxT
{
    public partial class App : WpfApp
    {
        private WinForms.NotifyIcon? _trayIcon;
        private GlobalHotkey? _hotkey;
        private MainWindow? _settingsWindow;
        private readonly SettingsService _settingsService = new();
        private AppSettings _settings = new();
        private readonly TranslationService _translationService = new();
        private bool _isTranslating = false;

        // Hidden ghost window — needed to host the GlobalHotkey HWND
        private System.Windows.Window? _ghostWindow;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            try
            {
                try { _settings = _settingsService.Load(); }
                catch (Exception ex) { throw new Exception($"Settings Load Error: {ex.Message}"); }

                try { BuildTrayIcon(); }
                catch (Exception ex) { throw new Exception($"Tray Icon Error: {ex.Message}"); }

                try { SetupHotkey(); }
                catch (Exception ex) { throw new Exception($"Hotkey Setup Error: {ex.Message}"); }
                
                try { OpenSettings(); }
                catch (Exception ex) { throw new Exception($"UI Load Error: {ex.Message}"); }

                // Check for updates in background immediately after UI is ready
                System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(() => 
                {
                    InitAutoUpdater();
                }), System.Windows.Threading.DispatcherPriority.Background);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message, "SwapTxT Crash", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }
        }

        private void InitAutoUpdater()
        {
            try
            {
                // Configure AutoUpdater to check GitHub
                AutoUpdater.RunUpdateAsAdmin = false;
                AutoUpdater.DownloadPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SwapTxT", "Updates");
                
                // Point to your GitHub update.xml
                AutoUpdater.Start("https://raw.githubusercontent.com/Tiw013/SwapTxT/main/update.xml");
            }
            catch { /* Fail silently to not disturb user */ }
        }

        // ─── System Tray ─────────────────────────────────────────────
        private void BuildTrayIcon()
        {
            _trayIcon = new WinForms.NotifyIcon
            {
                Visible = true,
                Text    = $"SwapTxT v{AppSettings.AppVersion} — Translation Assistant",
                Icon    = GetNativeIcon() ?? System.Drawing.SystemIcons.Application
            };

            // Left-click or double-click → open settings
            _trayIcon.MouseClick += (s, e) =>
            {
                if (e.Button == WinForms.MouseButtons.Left) OpenSettings();
            };

            BuildContextMenuStrip();
        }

        private void BuildContextMenuStrip()
        {
            var menu = new WinForms.ContextMenuStrip();
            menu.BackColor = System.Drawing.Color.FromArgb(0x1A, 0x1A, 0x2E);
            menu.ForeColor = System.Drawing.Color.White;
            menu.Font = new Font("Segoe UI", 9.5f);
            menu.Renderer = new DarkMenuRenderer();
            menu.ShowImageMargin = false;

            // ── Header (non-clickable)
            var header = new WinForms.ToolStripMenuItem($"SwapTxT  v{AppSettings.AppVersion}");
            header.ForeColor = System.Drawing.Color.FromArgb(0x6C, 0x63, 0xFF);
            header.Font = new Font("Segoe UI Semibold", 9.5f);
            header.Enabled = false;
            menu.Items.Add(header);
            menu.Items.Add(new WinForms.ToolStripSeparator());

            // ── Direction toggle
            _menuDirection = new WinForms.ToolStripMenuItem(GetDirectionLabel());
            _menuDirection.Click += (s, e) => OnToggleDirection();
            menu.Items.Add(_menuDirection);

            // ── Mode toggle
            _menuMode = new WinForms.ToolStripMenuItem(GetModeLabel());
            _menuMode.Click += (s, e) => OnToggleMode();
            menu.Items.Add(_menuMode);

            menu.Items.Add(new WinForms.ToolStripSeparator());

            // ── Settings
            var settingsItem = new WinForms.ToolStripMenuItem("⚙️   Settings");
            settingsItem.Click += (s, e) => OpenSettings();
            menu.Items.Add(settingsItem);

            // ── About
            var aboutItem = new WinForms.ToolStripMenuItem("ℹ️   About SwapTxT");
            aboutItem.Click += (s, e) => OpenAbout();
            menu.Items.Add(aboutItem);

            // ── Donate
            var donateItem = new WinForms.ToolStripMenuItem("☕   Buy Me a Coffee");
            donateItem.ForeColor = System.Drawing.Color.FromArgb(0xFF, 0xD0, 0x42);
            donateItem.Click += (s, e) =>
            {
                try { Process.Start(new ProcessStartInfo { FileName = _settings.DonateUrl, UseShellExecute = true }); }
                catch { }
            };
            menu.Items.Add(donateItem);

            menu.Items.Add(new WinForms.ToolStripSeparator());

            // ── Quit
            var quitItem = new WinForms.ToolStripMenuItem("✕   Quit SwapTxT");
            quitItem.ForeColor = System.Drawing.Color.FromArgb(0xF8, 0x71, 0x71);
            quitItem.Click += (s, e) =>
            {
                if (_trayIcon != null) { _trayIcon.Visible = false; _trayIcon.Dispose(); }
                Environment.Exit(0);
            };
            menu.Items.Add(quitItem);

            _trayIcon!.ContextMenuStrip = menu;
        }

        // WinForms menu items we need to update
        private WinForms.ToolStripMenuItem? _menuDirection;
        private WinForms.ToolStripMenuItem? _menuMode;

        private Icon? GetNativeIcon()
        {
            try
            {
                var uri = new Uri("pack://application:,,,/Resources/icon.png", UriKind.Absolute);
                var streamInfo = System.Windows.Application.GetResourceStream(uri);
                if (streamInfo != null)
                {
                    using (var bitmap = new Bitmap(streamInfo.Stream))
                    {
                        return Icon.FromHandle(bitmap.GetHicon());
                    }
                }
            }
            catch { }
            return null;
        }

        private System.Windows.Media.ImageSource? TryLoadIcon()
        {
            try
            {
                var uri = new Uri("pack://application:,,,/Resources/icon.png", UriKind.Absolute);
                // BitmapFrame is often more robust for icon sources
                return System.Windows.Media.Imaging.BitmapFrame.Create(uri, 
                    System.Windows.Media.Imaging.BitmapCreateOptions.None, 
                    System.Windows.Media.Imaging.BitmapCacheOption.OnLoad);
            }
            catch { return null; }
        }


        private void OnToggleDirection()
        {
            _settings.Direction = _settings.Direction == LanguageDirection.EnToTh
                ? LanguageDirection.ThToEn : LanguageDirection.EnToTh;
            _settingsService.Save(_settings);
            RefreshTrayMenu();
        }

        private void OnToggleMode()
        {
            _settings.Mode = _settings.Mode == TranslationMode.Manual
                ? TranslationMode.Auto : TranslationMode.Manual;
            _settingsService.Save(_settings);
            RefreshTrayMenu();
        }

        private void RefreshTrayMenu()
        {
            if (_menuDirection != null) _menuDirection.Text = GetDirectionLabel();
            if (_menuMode != null)      _menuMode.Text = GetModeLabel();
            if (_trayIcon != null)
                _trayIcon.Text = $"SwapTxT  •  {_settings.DirectionLabel}  •  {GetModeLabelShort()}";
        }

        private string GetDirectionLabel() =>
            $"↔  Direction: {_settings.DirectionLabel}  (click to flip)";

        private string GetModeLabel() =>
            $"{(_settings.Mode == TranslationMode.Manual ? "✋" : "⚡")}  Mode: {(_settings.Mode == TranslationMode.Manual ? "Manual Select" : "Auto Detect")}";

        private string GetModeLabelShort() =>
            _settings.Mode == TranslationMode.Manual ? "Manual" : "Auto";

        // ─── Settings Window ──────────────────────────────────────────────────────
        private void OpenSettings()
        {
            if (_settingsWindow == null || !_settingsWindow.IsLoaded)
            {
                _settingsWindow = new MainWindow(_settingsService, _settings);
                _settingsWindow.SettingsSaved += OnSettingsSaved;
            }
            _settingsWindow.Show();
            _settingsWindow.Activate();
        }

        private void OpenAbout()
        {
            var win = new AboutWindow(_settings);
            win.Show();
        }

        private void OnSettingsSaved(AppSettings newSettings)
        {
            _settings = newSettings;
            RefreshTrayMenu();
            // Re-register hotkey with new key combo + MOD_NOREPEAT
            _hotkey?.Reregister(_settings.HotkeyModifiers | 0x4000, _settings.HotkeyKey);
        }

        // ─── Global Hotkey ────────────────────────────────────────────────────────
        private void SetupHotkey()
        {
            // Create an invisible ghost window to host the HWND for RegisterHotKey
            _ghostWindow = new System.Windows.Window
            {
                Width = 0, Height = 0,
                WindowStyle = WindowStyle.None,
                ShowInTaskbar = false,
                Visibility = Visibility.Hidden
            };
            _ghostWindow.Show(); // must be shown to get a real HWND

            _hotkey = new GlobalHotkey();
            _hotkey.HotkeyPressed += OnHotkeyPressed;

            try
            {
                // MOD_NOREPEAT (0x4000) prevents auto-repeat and also helps avoid
                // conflicts with Thai IME (which uses Ctrl+Space to switch input).
                int modsWithNoRepeat = _settings.HotkeyModifiers | 0x4000;
                _hotkey.Register(_ghostWindow, modsWithNoRepeat, _settings.HotkeyKey);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Failed to register hotkey '{_settings.HotkeyDisplayName}'.\n{ex.Message}\n\nPlease change the hotkey in Settings.",
                    "SwapTxT — Hotkey Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // ─── Translation Trigger ──────────────────────────────────────────────────
        private async void OnHotkeyPressed()
        {
            if (_isTranslating) return;
            
            _isTranslating = true;

            try
            {
                // Immediate feedback
                if (_settings.ShowNotifications)
                {
                    _trayIcon?.ShowBalloonTip(500, "SwapTxT", "Translating...", WinForms.ToolTipIcon.Info);
                }

                // Safety timeout: reset _isTranslating after 15 seconds if it gets stuck
                var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                
                // Give key-release time
                await Task.Delay(80);

                var result = await ClipboardHelper.TranslateAtCursorAsync(
                    _settings,
                    async (text, sl, tl) => await _translationService.TranslateAsync(_settings, text, sl, tl));

                if (result.translated != null && _settings.ShowNotifications)
                {
                    string preview = result.translated.Length > 55
                        ? result.translated.Substring(0, 52) + "..."
                        : result.translated;
                    _trayIcon?.ShowBalloonTip(
                        2000,
                        "SwapTxT ✔  Translated",
                        preview,
                        WinForms.ToolTipIcon.Info);
                }
            }
            catch (Exception ex)
            {
                string message = ex.Message;
                if (ex.InnerException != null) message += "\nDetails: " + ex.InnerException.Message;
                
                _trayIcon?.ShowBalloonTip(
                    5000,
                    "SwapTxT — Translation Error",
                    message,
                    WinForms.ToolTipIcon.Error);

                System.Windows.Application.Current.Dispatcher.Invoke(() => 
                {
                    System.Windows.MessageBox.Show(
                        "Detailed Error from AI Server:\n\n" + message, 
                        "Translation API Error", 
                        System.Windows.MessageBoxButton.OK, 
                        System.Windows.MessageBoxImage.Error);
                });
                
                // If it's a type init error, it's critical
                if (ex is TypeInitializationException || ex.InnerException is TypeInitializationException)
                {
                    System.Windows.MessageBox.Show("A critical component failed to initialize. Please restart the app.\n\n" + message, "SwapTxT — Critical Error");
                }
            }
            finally
            {
                _isTranslating = false;
            }
        }


        protected override void OnExit(ExitEventArgs e)
        {
            _hotkey?.Dispose();
            if (_trayIcon != null) { _trayIcon.Visible = false; _trayIcon.Dispose(); }
            base.OnExit(e);
        }
    }

    // ─── Dark WinForms Context Menu Renderer ─────────────────────────────────────
    public class DarkMenuRenderer : WinForms.ToolStripProfessionalRenderer
    {
        public DarkMenuRenderer() : base(new DarkColorTable()) { }
    }

    public class DarkColorTable : WinForms.ProfessionalColorTable
    {
        public override System.Drawing.Color MenuItemSelected =>
            System.Drawing.Color.FromArgb(0x25, 0x25, 0x45);
        public override System.Drawing.Color MenuItemBorder =>
            System.Drawing.Color.FromArgb(0x6C, 0x63, 0xFF);
        public override System.Drawing.Color MenuBorder =>
            System.Drawing.Color.FromArgb(0x2D, 0x2D, 0x55);
        public override System.Drawing.Color ToolStripDropDownBackground =>
            System.Drawing.Color.FromArgb(0x1A, 0x1A, 0x2E);
        public override System.Drawing.Color ImageMarginGradientBegin =>
            System.Drawing.Color.FromArgb(0x16, 0x16, 0x26);
        public override System.Drawing.Color ImageMarginGradientMiddle =>
            System.Drawing.Color.FromArgb(0x16, 0x16, 0x26);
        public override System.Drawing.Color ImageMarginGradientEnd =>
            System.Drawing.Color.FromArgb(0x16, 0x16, 0x26);
        public override System.Drawing.Color SeparatorDark =>
            System.Drawing.Color.FromArgb(0x2D, 0x2D, 0x55);
        public override System.Drawing.Color SeparatorLight =>
            System.Drawing.Color.FromArgb(0x35, 0x35, 0x60);
    }
}
