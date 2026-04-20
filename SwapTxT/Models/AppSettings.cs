using Microsoft.Win32;
using Newtonsoft.Json;

namespace SwapTxT.Models
{
    public enum TranslationEngine { Google, AI }
    public enum AIProvider { OpenAI, Gemini, OpenRouter }
    public enum TranslationMode { Manual, Auto }
    public enum LanguageDirection { EnToTh, ThToEn }

    public class AppSettings
    {
        public TranslationEngine Engine { get; set; } = TranslationEngine.Google;
        public AIProvider AIProvider { get; set; } = AIProvider.OpenRouter;
        public TranslationMode Mode { get; set; } = TranslationMode.Manual;
        public LanguageDirection Direction { get; set; } = LanguageDirection.ThToEn;

        // API Keys
        public string OpenAIKey { get; set; } = "";
        public string GeminiKey { get; set; } = "";
        public string OpenRouterKey { get; set; } = "";

        // AI Model (used for OpenAI and OpenRouter)
        public string AIModel { get; set; } = "google/gemini-flash-1.5";
        public List<string> RecentCustomModels { get; set; } = new List<string>();

        // AI Tone
        public string AITone { get; set; } = "Standard";
        public List<string> RecentCustomTones { get; set; } = new List<string>();

        // Hotkey — stored as modifier name + key name for ComboBox binding
        public int HotkeyModifiers { get; set; } = 0x0002; // MOD_CONTROL
        public int HotkeyKey { get; set; } = 0x20;         // VK_SPACE
        public string HotkeyDisplayName { get; set; } = "Ctrl+Space";
        public string HotkeyModifierName { get; set; } = "Ctrl";
        public string HotkeyKeyName { get; set; } = "Space";

        // Notifications
        public bool ShowNotifications { get; set; } = true;

        // Donate URL
        public string DonateUrl { get; set; } = "https://www.buymeacoffee.com/tixs";

        [JsonIgnore]
        public static string AppVersion => "1.0.2";

        [JsonIgnore]
        public string SourceLang => Direction == LanguageDirection.EnToTh ? "en" : "th";

        [JsonIgnore]
        public string TargetLang => Direction == LanguageDirection.EnToTh ? "th" : "en";

        [JsonIgnore]
        public string DirectionLabel => Direction == LanguageDirection.EnToTh ? "EN → TH" : "TH → EN";

        // ─── Windows Registry: Run on Startup ──────────────────────────
        private const string StartupRegKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
        private const string StartupAppName = "SwapTxT";

        public bool CheckRunOnStartup()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(StartupRegKey, false);
                return key?.GetValue(StartupAppName) != null;
            }
            catch { return false; }
        }

        public void SetRunOnStartup(bool enable)
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(StartupRegKey, true);
                if (key == null) return;
                if (enable)
                {
                    string? exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
                    if (exePath != null)
                        key.SetValue(StartupAppName, $"\"{exePath}\"");
                }
                else
                {
                    key.DeleteValue(StartupAppName, throwOnMissingValue: false);
                }
            }
            catch { }
        }
    }
}
