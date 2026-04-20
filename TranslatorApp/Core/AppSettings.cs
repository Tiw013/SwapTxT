using System;
using System.IO;
using System.Text.Json;
using Microsoft.Win32;

namespace TranslatorApp.Core
{
    /// <summary>
    /// Application settings with JSON persistence to AppData folder.
    /// </summary>
    public class AppSettings
    {
        private static readonly string SettingsFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "TranslatorApp");

        private static readonly string SettingsFile = Path.Combine(SettingsFolder, "settings.json");

        // Hotkey Settings
        public string HotkeyModifiers { get; set; } = "Ctrl";
        public string HotkeyKey { get; set; } = "Space";

        // Startup Setting
        public bool RunOnStartup { get; set; } = false;

        // Translation Engine Settings
        public string TranslationEngine { get; set; } = "Google"; // "Google", "OpenAI", "Gemini"
        public string OpenAIApiKey { get; set; } = "";
        public string GeminiApiKey { get; set; } = "";
        public string AITone { get; set; } = "มาตรฐาน"; // มาตรฐาน, ทางการ, เป็นกันเอง
        public string AIProvider { get; set; } = "OpenAI"; // OpenAI, Gemini

        // UI Settings
        public bool ShowNotificationToast { get; set; } = true;

        // Donate URL
        public string DonateUrl { get; set; } = "https://promptpay.io/0812345678";

        // Auto Update XML URL
        public string UpdateCheckUrl { get; set; } = "";

        // App Version
        public static string AppVersion => "1.0.0";

        // ─── Save / Load ───────────────────────────────────────

        public void Save()
        {
            try
            {
                Directory.CreateDirectory(SettingsFolder);
                string json = JsonSerializer.Serialize(this, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                File.WriteAllText(SettingsFile, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Settings save error: {ex.Message}");
            }
        }

        public static AppSettings Load()
        {
            try
            {
                if (File.Exists(SettingsFile))
                {
                    string json = File.ReadAllText(SettingsFile);
                    var settings = JsonSerializer.Deserialize<AppSettings>(json);
                    return settings ?? new AppSettings();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Settings load error: {ex.Message}");
            }
            return new AppSettings();
        }

        // ─── Windows Startup Registry ──────────────────────────

        public void SetRunOnStartup(bool enable)
        {
            const string keyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
            const string appName = "TranslatorApp";

            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(keyPath, true);
                if (enable)
                {
                    string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                    // For .NET 6+ executables
                    if (exePath.EndsWith(".dll"))
                        exePath = exePath.Replace(".dll", ".exe");
                    key?.SetValue(appName, $"\"{exePath}\"");
                }
                else
                {
                    key?.DeleteValue(appName, false);
                }
                RunOnStartup = enable;
                Save();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Startup registry error: {ex.Message}");
            }
        }

        public bool CheckRunOnStartup()
        {
            const string keyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
            const string appName = "TranslatorApp";

            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(keyPath, false);
                return key?.GetValue(appName) != null;
            }
            catch
            {
                return false;
            }
        }
    }
}
