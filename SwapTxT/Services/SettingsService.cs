using Newtonsoft.Json;
using SwapTxT.Models;
using System;
using System.IO;

namespace SwapTxT.Services
{
    public class SettingsService
    {
        private static readonly string SettingsDir =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SwapTxT");
        private static readonly string SettingsFile =
            Path.Combine(SettingsDir, "settings.json");

        public AppSettings Load()
        {
            try
            {
                if (!File.Exists(SettingsFile))
                    return new AppSettings();

                var json = File.ReadAllText(SettingsFile);
                return JsonConvert.DeserializeObject<AppSettings>(json) ?? new AppSettings();
            }
            catch
            {
                return new AppSettings();
            }
        }

        public void Save(AppSettings settings)
        {
            Directory.CreateDirectory(SettingsDir);
            var json = JsonConvert.SerializeObject(settings, Formatting.Indented);
            File.WriteAllText(SettingsFile, json);
        }
    }
}
