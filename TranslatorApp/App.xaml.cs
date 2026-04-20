using System;
using System.Windows;
using System.Threading;

namespace TranslatorApp
{
    public partial class App : System.Windows.Application
    {
        private static Mutex _mutex;

        protected override void OnStartup(StartupEventArgs e)
        {
            // Single instance check
            _mutex = new Mutex(true, "TranslatorApp_SingleInstance", out bool createdNew);
            if (!createdNew)
            {
                System.Windows.MessageBox.Show("โปรแกรมกำลังทำงานอยู่แล้วครับ\nดูได้จากไอคอน System Tray มุมขวาล่าง 🌸",
                    "แจ้งเตือน", MessageBoxButton.OK, MessageBoxImage.Information);
                Shutdown();
                return;
            }

            base.OnStartup(e);

            var mainWindow = new MainWindow();
            mainWindow.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _mutex?.ReleaseMutex();
            _mutex?.Dispose();
            base.OnExit(e);
        }
    }
}
