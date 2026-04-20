using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using TranslatorApp.Core;

namespace TranslatorApp
{
    public partial class SettingsWindow : Window
    {
        private AppSettings _settings;
        public event EventHandler<AppSettings> SettingsSaved;

        private static readonly SolidColorBrush SelectedBorder =
            new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 107, 157));
        private static readonly SolidColorBrush NormalBorder =
            new SolidColorBrush(System.Windows.Media.Color.FromRgb(42, 42, 74));
        private static readonly SolidColorBrush SelectedBg =
            new SolidColorBrush(System.Windows.Media.Color.FromRgb(37, 37, 60));
        private static readonly SolidColorBrush NormalBg =
            new SolidColorBrush(System.Windows.Media.Color.FromRgb(15, 15, 26));

        public SettingsWindow(AppSettings settings)
        {
            InitializeComponent();
            _settings = settings;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadSettings();
        }

        private void LoadSettings()
        {
            // Hotkey
            SetComboByText(ModifierCombo, _settings.HotkeyModifiers);
            SetComboByText(KeyCombo, _settings.HotkeyKey);
            UpdateHotkeyPreview();

            // Startup
            StartupCheckBox.IsChecked = _settings.CheckRunOnStartup();
            ToastCheckBox.IsChecked = _settings.ShowNotificationToast;

            // Engine
            bool isAI = _settings.TranslationEngine != "Google";
            GoogleRadio.IsChecked = !isAI;
            AIRadio.IsChecked = isAI;
            AISettingsPanel.Visibility = isAI ? Visibility.Visible : Visibility.Collapsed;

            UpdateOptionBorderStyle(isAI);

            // AI Provider
            if (_settings.AIProvider == "Gemini")
                ProviderCombo.SelectedIndex = 1;
            else
                ProviderCombo.SelectedIndex = 0;

            UpdateApiKeyLabel();

            // API Key - show placeholder
            if (!string.IsNullOrEmpty(_settings.OpenAIApiKey) && _settings.AIProvider == "OpenAI")
                ApiKeyBox.Password = _settings.OpenAIApiKey;
            else if (!string.IsNullOrEmpty(_settings.GeminiApiKey) && _settings.AIProvider == "Gemini")
                ApiKeyBox.Password = _settings.GeminiApiKey;

            // Tone
            SetComboByText(ToneCombo, _settings.AITone);

            // Version
            VersionText.Text = $"v{AppSettings.AppVersion}";
            InfoVersion.Text = AppSettings.AppVersion;
        }

        private void SetComboByText(System.Windows.Controls.ComboBox combo, string text)
        {
            foreach (System.Windows.Controls.ComboBoxItem item in combo.Items)
            {
                if (item.Content?.ToString() == text)
                {
                    combo.SelectedItem = item;
                    return;
                }
            }
            if (combo.Items.Count > 0)
                combo.SelectedIndex = 0;
        }

        // ─── Engine Selection ──────────────────────────────────

        private void GoogleOption_Click(object sender, MouseButtonEventArgs e)
        {
            GoogleRadio.IsChecked = true;
            AIRadio.IsChecked = false;
            AISettingsPanel.Visibility = Visibility.Collapsed;
            UpdateOptionBorderStyle(false);
        }

        private void AIOption_Click(object sender, MouseButtonEventArgs e)
        {
            AIRadio.IsChecked = true;
            GoogleRadio.IsChecked = false;
            AISettingsPanel.Visibility = Visibility.Visible;
            UpdateOptionBorderStyle(true);
        }

        private void UpdateOptionBorderStyle(bool aiSelected)
        {
            if (aiSelected)
            {
                GoogleOptionBorder.BorderBrush = NormalBorder;
                GoogleOptionBorder.Background = NormalBg;
                AIOptionBorder.BorderBrush = SelectedBorder;
                AIOptionBorder.Background = SelectedBg;
            }
            else
            {
                GoogleOptionBorder.BorderBrush = SelectedBorder;
                GoogleOptionBorder.Background = SelectedBg;
                AIOptionBorder.BorderBrush = NormalBorder;
                AIOptionBorder.Background = NormalBg;
            }
        }

        private void ProviderCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateApiKeyLabel();
        }

        private void UpdateApiKeyLabel()
        {
            if (ApiKeyLabel == null) return;
            bool isGemini = ProviderCombo?.SelectedIndex == 1;
            ApiKeyLabel.Text = isGemini ? "Gemini API Key" : "OpenAI API Key";
        }

        // ─── Hotkey Preview ────────────────────────────────────

        private void UpdateHotkeyPreview()
        {
            if (HotkeyPreviewText == null) return;
            string mod = (ModifierCombo?.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString() ?? "Ctrl";
            string key = (KeyCombo?.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString() ?? "Space";
            HotkeyPreviewText.Text = $"{mod} + {key}";
        }

        // ─── Test API Key ──────────────────────────────────────

        private async void TestApiKey_Click(object sender, RoutedEventArgs e)
        {
            TestResultText.Visibility = Visibility.Visible;
            TestResultText.Text = "🔄 กำลังทดสอบ...";
            TestResultText.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(176, 176, 204));

            try
            {
                string apiKey = ApiKeyBox.Password;
                bool isGemini = ProviderCombo.SelectedIndex == 1;

                ITranslationEngine testEngine = isGemini
                    ? new GeminiTranslateEngine(apiKey, "มาตรฐาน")
                    : new OpenAITranslateEngine(apiKey, "มาตรฐาน") as ITranslationEngine;

                string result = await testEngine.TranslateAsync("Hello, world!");

                if (!string.IsNullOrEmpty(result))
                {
                    TestResultText.Text = $"✅ API Key ใช้งานได้! ทดสอบ: \"{result}\"";
                    TestResultText.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(74, 222, 128));
                }
            }
            catch (Exception ex)
            {
                TestResultText.Text = $"❌ {ex.Message}";
                TestResultText.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(248, 113, 113));
            }
        }

        // ─── API Help Link ─────────────────────────────────────

        private void ApiHelp_Click(object sender, RoutedEventArgs e)
        {
            bool isGemini = ProviderCombo.SelectedIndex == 1;
            string url = isGemini
                ? "https://aistudio.google.com/app/apikey"
                : "https://platform.openai.com/api-keys";

            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }

        // ─── Save / Cancel ─────────────────────────────────────

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            // Hotkey
            _settings.HotkeyModifiers = (ModifierCombo.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString() ?? "Ctrl";
            _settings.HotkeyKey = (KeyCombo.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString() ?? "Space";

            // Startup
            bool runOnStartup = StartupCheckBox.IsChecked == true;
            _settings.SetRunOnStartup(runOnStartup);
            _settings.ShowNotificationToast = ToastCheckBox.IsChecked == true;

            // Engine
            bool isAI = AIRadio.IsChecked == true;
            bool isGemini = ProviderCombo.SelectedIndex == 1;

            if (!isAI)
            {
                _settings.TranslationEngine = "Google";
            }
            else
            {
                _settings.TranslationEngine = isGemini ? "Gemini" : "OpenAI";
                _settings.AIProvider = isGemini ? "Gemini" : "OpenAI";

                string apiKey = ApiKeyBox.Password;
                if (isGemini)
                    _settings.GeminiApiKey = apiKey;
                else
                    _settings.OpenAIApiKey = apiKey;

                _settings.AITone = (ToneCombo.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString() ?? "มาตรฐาน";
            }

            _settings.Save();
            SettingsSaved?.Invoke(this, _settings);

            // Show save confirmation
            ShowSaveConfirmation();
        }

        private void ShowSaveConfirmation()
        {
            // Flash the save button with success feedback
            var saveBtn = (System.Windows.Controls.Button)FindName("DonateButton");

            System.Windows.MessageBox.Show("💾 บันทึกตั้งค่าเรียบร้อยแล้วค่ะ!", "TranslatorApp",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        // ─── Donate ────────────────────────────────────────────

        private void DonateButton_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = _settings.DonateUrl,
                UseShellExecute = true
            });
        }

        // ─── Window Controls ───────────────────────────────────

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
