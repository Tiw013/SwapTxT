using SwapTxT.Models;
using SwapTxT.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace SwapTxT
{
    public partial class MainWindow : Window
    {
        private readonly SettingsService _settingsService;
        private AppSettings _settings;

        // Raised when settings are saved so App.xaml.cs can re-register hotkey
        public event Action<AppSettings>? SettingsSaved;

        // Maps modifier combo name → (MOD_* flags)
        private static readonly Dictionary<string, int> ModifierMap = new()
        {
            { "Ctrl",       0x0002 },
            { "Alt",        0x0001 },
            { "Ctrl+Alt",   0x0003 },
            { "Ctrl+Shift", 0x0006 },
        };

        // Maps key name → VK code
        private static readonly Dictionary<string, int> KeyMap = new()
        {
            { "Space", 0x20 },
            { "D",     0x44 }, { "E", 0x45 }, { "T", 0x54 }, { "Q", 0x51 },
            { "F1",  0x70 }, { "F2",  0x71 }, { "F3",  0x72 }, { "F4",  0x73 },
            { "F5",  0x74 }, { "F6",  0x75 }, { "F7",  0x76 }, { "F8",  0x77 },
            { "F9",  0x78 }, { "F10", 0x79 }, { "F11", 0x7A }, { "F12", 0x7B },
        };

        // ─── Presets ──────────────────────────────────────────────────────────────
        private readonly List<string> _modelPresets = new()
        {
            "google/gemini-flash-1.5",
            "google/gemini-pro-1.5",
            "openai/gpt-4o-mini",
            "openai/gpt-4o",
            "anthropic/claude-3-haiku",
            "meta-llama/llama-3.1-8b-instruct"
        };

        private readonly List<string> _tonePresets = new()
        {
            "Standard",
            "Formal / Professional",
            "Casual / Informal",
            "Conversational / Friendly",
            "Slang / Native-like",
            "Coding / Structured"
        };
        // ──────────────────────────────────────────────────────────────────────────

        public MainWindow(SettingsService settingsService, AppSettings settings)
        {
            InitializeComponent();
            _settingsService = settingsService;
            _settings = settings;
            LoadUI();
        }

        // ─── Load current settings into UI ───────────────────────────────────────
        private void LoadUI()
        {
            // Direction
            RbEnToTh.IsChecked = _settings.Direction == LanguageDirection.EnToTh;
            RbThToEn.IsChecked = _settings.Direction == LanguageDirection.ThToEn;

            // Mode
            RbManual.IsChecked = _settings.Mode == TranslationMode.Manual;
            RbAuto.IsChecked = _settings.Mode == TranslationMode.Auto;
            UpdateModeDesc();

            // Hotkey ComboBoxes
            SelectComboByTag(CmbModifier, _settings.HotkeyModifierName);
            SelectComboByTag(CmbHotkeyKey, _settings.HotkeyKeyName);
            UpdateHotkeyPreview();

            // Engine
            RbGoogle.IsChecked = _settings.Engine == TranslationEngine.Google;
            RbAI.IsChecked = _settings.Engine == TranslationEngine.AI;
            PanelAI.Visibility = _settings.Engine == TranslationEngine.AI ? Visibility.Visible : Visibility.Collapsed;

            // AI Provider
            SelectComboByTag(CmbProvider, _settings.AIProvider.ToString());

            // API Key
            TxtApiKey.Text = GetCurrentApiKey();

            // Setup Hybrid ComboBoxes (Model & Tone)
            SetupHybridComboBox(CmbModel, _modelPresets, _settings.RecentCustomModels, _settings.AIModel);
            SetupHybridComboBox(CmbTone, _tonePresets, _settings.RecentCustomTones, _settings.AITone);

            // Preferences
            ChkNotifications.IsChecked = _settings.ShowNotifications;
            ChkStartup.IsChecked = _settings.CheckRunOnStartup();

            // Status badge
            UpdateStatusBadge();
        }

        private void UpdateStatusBadge()
        {
            string engineLabel = _settings.Engine == TranslationEngine.Google ? "Google" : _settings.AIProvider.ToString();
            string modeLabel = _settings.Mode == TranslationMode.Manual ? "Manual" : "Auto";
            TxtStatus.Text = $"Active  •  {_settings.HotkeyDisplayName}  •  {_settings.DirectionLabel}  •  {modeLabel}  •  {engineLabel}";
        }

        private void UpdateModeDesc()
        {
            if (TxtModeDesc == null || RbManual == null) return;
            if (RunModeDesc != null)
            {
                RunModeDesc.Text = RbManual.IsChecked == true
                    ? "Highlight text manually, then press hotkey to translate."
                    : "If text is selected, translates it. Otherwise finds and translates the last block of source-language text in the field.";
            }
        }

        private void UpdateHotkeyPreview()
        {
            if (TxtHotkeyPreview == null) return;
            string mod = GetSelectedTag(CmbModifier) ?? "Ctrl";
            string key = GetSelectedTag(CmbHotkeyKey) ?? "Space";
            TxtHotkeyPreview.Text = $"Hotkey: {mod} + {key}";
        }

        // ─── Event Handlers ───────────────────────────────────────────────────────
        private void TitleBar_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
            => DragMove();

        private void CloseButton_Click(object sender, RoutedEventArgs e) => Hide();
        private void BtnCancel_Click(object sender, RoutedEventArgs e) => Hide();

        private void CoffeeBtn_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            try { Process.Start(new ProcessStartInfo { FileName = _settings.DonateUrl, UseShellExecute = true }); }
            catch { }
        }

        private void AboutBtn_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            try
            {
                var win = new AboutWindow(_settings);
                win.Show();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.ToString(), "Crash Info", MessageBoxButton.OK, MessageBoxImage.Error);
                System.IO.File.WriteAllText("crash.txt", ex.ToString());
            }
        }


        private void Direction_Changed(object sender, RoutedEventArgs e) { }

        private void Mode_Changed(object sender, RoutedEventArgs e) => UpdateModeDesc();

        private void Engine_Changed(object sender, RoutedEventArgs e)
        {
            if (PanelAI == null || RbAI == null) return;
            PanelAI.Visibility = RbAI.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        }

        private void CmbProvider_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PanelModel == null || PanelTone == null || TxtApiKey == null || _settings == null) return;
            var tag = GetSelectedTag(CmbProvider);
            PanelModel.Visibility = tag != "Gemini" ? Visibility.Visible : Visibility.Collapsed;
            PanelTone.Visibility  = Visibility.Visible; // Tone is supported universally for all AI
            TxtApiKey.Text = GetCurrentApiKey();
        }

        // ─── Hybrid ComboBox Logic ──────────────────────────────────────────────
        private bool _isUpdatingCombo = false;

        private void SetupHybridComboBox(System.Windows.Controls.ComboBox cmb, List<string> presets, List<string> recents, string currentValue)
        {
            _isUpdatingCombo = true;
            PopulateHybridComboBox(cmb, presets, recents, "");
            cmb.Text = currentValue;
            _isUpdatingCombo = false;
        }

        private void PopulateHybridComboBox(System.Windows.Controls.ComboBox cmb, List<string> presets, List<string> recents, string filter)
        {
            cmb.Items.Clear();

            // Presets
            bool hasMatches = false;
            foreach (var p in presets)
            {
                if (string.IsNullOrWhiteSpace(filter) || p.Contains(filter, StringComparison.OrdinalIgnoreCase))
                {
                    cmb.Items.Add(new System.Windows.Controls.ComboBoxItem { Content = p, Tag = p });
                    hasMatches = true;
                }
            }

            // Recents
            if (recents != null && recents.Count > 0)
            {
                var filteredRecents = recents.Where(r => string.IsNullOrWhiteSpace(filter) || r.Contains(filter, StringComparison.OrdinalIgnoreCase)).ToList();
                if (filteredRecents.Count > 0)
                {
                    if (hasMatches) // separator if presets exist
                    {
                        var sep = new System.Windows.Controls.ComboBoxItem { Content = "── Recents ──", IsEnabled = false, Focusable = false };
                        sep.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(136, 132, 168));
                        cmb.Items.Add(sep);
                    }
                    foreach (var r in filteredRecents)
                        cmb.Items.Add(new System.Windows.Controls.ComboBoxItem { Content = r, Tag = r });
                }
            }

            // "Use Custom" option if typing and not exactly matched
            if (!string.IsNullOrWhiteSpace(filter))
            {
                bool exactMatch = presets.Any(p => p.Equals(filter, StringComparison.OrdinalIgnoreCase)) ||
                                  (recents != null && recents.Any(r => r.Equals(filter, StringComparison.OrdinalIgnoreCase)));
                
                if (!exactMatch)
                {
                    var customItem = new System.Windows.Controls.ComboBoxItem { Content = $"Use custom: '{filter}'", Tag = filter, FontStyle = System.Windows.FontStyles.Italic };
                    customItem.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(108, 99, 255));
                    cmb.Items.Add(customItem);
                }
            }
        }

        private void HybridComboBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isUpdatingCombo) return;
            if (sender is System.Windows.Controls.TextBox tb && tb.TemplatedParent is System.Windows.Controls.ComboBox cmb)
            {
                if (!tb.IsFocused) return; // STRICT CHECK: Only process when the user is actively typing with a blinking cursor!

                _isUpdatingCombo = true;

                string filter = tb.Text;
                int caret = tb.CaretIndex;

                if (cmb.Name == "CmbModel")
                    PopulateHybridComboBox(cmb, _modelPresets, _settings.RecentCustomModels, filter);
                else if (cmb.Name == "CmbTone")
                    PopulateHybridComboBox(cmb, _tonePresets, _settings.RecentCustomTones, filter);

                if (!string.IsNullOrEmpty(filter))
                    cmb.IsDropDownOpen = true;

                tb.Text = filter;
                tb.CaretIndex = caret;

                _isUpdatingCombo = false;
            }
        }

        private void HybridComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isUpdatingCombo) return;
            
            // Only respond when an item is freshly clicked
            if (sender is System.Windows.Controls.ComboBox cmb && e.AddedItems.Count > 0 && e.AddedItems[0] is System.Windows.Controls.ComboBoxItem item)
            {
                string tag = item.Tag?.ToString() ?? "";
                if (!string.IsNullOrEmpty(tag))
                {
                    Dispatcher.InvokeAsync(() =>
                    {
                        _isUpdatingCombo = true;
                        
                        cmb.IsDropDownOpen = false;

                        // Reset list visually after selection without triggering events
                        if (cmb.Name == "CmbModel")
                            PopulateHybridComboBox(cmb, _modelPresets, _settings.RecentCustomModels, "");
                        else if (cmb.Name == "CmbTone")
                            PopulateHybridComboBox(cmb, _tonePresets, _settings.RecentCustomTones, "");
                            
                        cmb.Text = tag; // ensure text is perfectly set to exact tag
                        
                        _isUpdatingCombo = false;
                    }, System.Windows.Threading.DispatcherPriority.Input);
                }
            }
        }

        private void AddToRecents(List<string> recents, string value, List<string> presets)
        {
            if (string.IsNullOrWhiteSpace(value) || presets.Contains(value)) return;
            recents.RemoveAll(x => x.Equals(value, StringComparison.OrdinalIgnoreCase));
            recents.Insert(0, value);
            if (recents.Count > 5)
                recents.RemoveRange(5, recents.Count - 5);
        }
        // ──────────────────────────────────────────────────────────────────────────

        private void TxtApiKey_TextChanged(object sender, TextChangedEventArgs e) { }

        private void CmbHotkey_Changed(object sender, SelectionChangedEventArgs e)
            => UpdateHotkeyPreview();

        private void ChkNotifications_Changed(object sender, RoutedEventArgs e) { }

        private void ChkStartup_Changed(object sender, RoutedEventArgs e) { }

        // ─── Save ─────────────────────────────────────────────────────────────────
        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            // Direction
            _settings.Direction = RbEnToTh.IsChecked == true
                ? LanguageDirection.EnToTh : LanguageDirection.ThToEn;

            // Mode
            _settings.Mode = RbAuto.IsChecked == true
                ? TranslationMode.Auto : TranslationMode.Manual;

            // Engine
            _settings.Engine = RbAI.IsChecked == true
                ? TranslationEngine.AI : TranslationEngine.Google;

            // Provider
            string providerTag = GetSelectedTag(CmbProvider) ?? "OpenRouter";
            _settings.AIProvider = providerTag switch
            {
                "OpenAI"  => AIProvider.OpenAI,
                "Gemini"  => AIProvider.Gemini,
                _         => AIProvider.OpenRouter
            };

            // API Key
            SetApiKey(_settings.AIProvider, TxtApiKey.Text.Trim());

            // Model — Extract from Hybrid Box
            string? typedModel = CmbModel.Text?.Trim();
            if (!string.IsNullOrEmpty(typedModel))
            {
                _settings.AIModel = typedModel;
                AddToRecents(_settings.RecentCustomModels, typedModel, _modelPresets);
            }

            // Tone — Extract from Hybrid Box
            string? typedTone = CmbTone.Text?.Trim();
            if (!string.IsNullOrEmpty(typedTone))
            {
                _settings.AITone = typedTone;
                AddToRecents(_settings.RecentCustomTones, typedTone, _tonePresets);
            }

            // Hotkey via ComboBoxes
            string modName = GetSelectedTag(CmbModifier) ?? "Ctrl";
            string keyName = GetSelectedTag(CmbHotkeyKey) ?? "Space";
            _settings.HotkeyModifierName = modName;
            _settings.HotkeyKeyName = keyName;
            _settings.HotkeyModifiers = ModifierMap.TryGetValue(modName, out int mods) ? mods : 0x0002;
            _settings.HotkeyKey = KeyMap.TryGetValue(keyName, out int vk) ? vk : 0x20;
            _settings.HotkeyDisplayName = $"{modName}+{keyName}";

            // Preferences
            _settings.ShowNotifications = ChkNotifications.IsChecked == true;
            _settings.SetRunOnStartup(ChkStartup.IsChecked == true);

            // Persist
            _settingsService.Save(_settings);

            // Notify App
            SettingsSaved?.Invoke(_settings);

            TxtSaveStatus.Text = "✓ Saved!";
            UpdateStatusBadge();

            // Auto-hide status after 1.5s
            var timer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(1.5) };
            timer.Tick += (_, __) => { TxtSaveStatus.Text = ""; timer.Stop(); };
            timer.Start();
        }

        // ─── Helpers ─────────────────────────────────────────────────────────────
        private string GetCurrentApiKey() => _settings.AIProvider switch
        {
            AIProvider.OpenAI  => _settings.OpenAIKey,
            AIProvider.Gemini  => _settings.GeminiKey,
            _                  => _settings.OpenRouterKey
        };

        private void SetApiKey(AIProvider provider, string key)
        {
            switch (provider)
            {
                case AIProvider.OpenAI:     _settings.OpenAIKey = key;     break;
                case AIProvider.Gemini:     _settings.GeminiKey = key;     break;
                case AIProvider.OpenRouter: _settings.OpenRouterKey = key; break;
            }
        }

        private static void SelectComboByTag(System.Windows.Controls.ComboBox combo, string tag)
        {
            foreach (System.Windows.Controls.ComboBoxItem item in combo.Items)
            {
                if (item.Tag?.ToString() == tag || item.Content?.ToString() == tag)
                {
                    combo.SelectedItem = item;
                    return;
                }
            }
            if (combo.IsEditable)
            {
                combo.Text = tag;
            }
            else if (combo.Items.Count > 0)
            {
                combo.SelectedIndex = 0;
            }
        }

        private static string? GetSelectedTag(System.Windows.Controls.ComboBox combo)
            => (combo.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Tag?.ToString();
    }
}
