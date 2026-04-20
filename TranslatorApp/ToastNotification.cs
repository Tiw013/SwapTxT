using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace TranslatorApp
{
    public enum ToastType { Success, Error, Warning, Info }

    /// <summary>
    /// Animated toast notification that appears near the system tray.
    /// Auto-dismisses after a few seconds.
    /// </summary>
    public class ToastNotification : IDisposable
    {
        private ToastWindow _currentToast;

        public void Show(string title, string message, ToastType type = ToastType.Info)
        {
            System.Windows.Application.Current.Dispatcher.BeginInvoke(() =>
            {
                _currentToast?.ForceClose();
                _currentToast = new ToastWindow(title, message, type);
                _currentToast.Show();
            });
        }

        public void Dispose()
        {
            System.Windows.Application.Current.Dispatcher.BeginInvoke(() =>
            {
                _currentToast?.ForceClose();
            });
        }
    }

    public class ToastWindow : Window
    {
        private DispatcherTimer _autoCloseTimer;

        public ToastWindow(string title, string message, ToastType type)
        {
            // Window configuration
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = System.Windows.Media.Brushes.Transparent;
            Topmost = true;
            ShowInTaskbar = false;
            ResizeMode = ResizeMode.NoResize;
            Width = 340;
            Height = 80;

            // Position: bottom-right corner above taskbar
            var workArea = SystemParameters.WorkArea;
            Left = workArea.Right - Width - 16;
            Top = workArea.Bottom - Height - 16;

            // Build UI
            Content = BuildToastContent(title, message, type);

            // Fade-in animation
            Opacity = 0;
            Loaded += (s, e) =>
            {
                var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200));
                BeginAnimation(OpacityProperty, fadeIn);
            };

            // Auto close after 4 seconds
            _autoCloseTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(4)
            };
            _autoCloseTimer.Tick += (s, e) => FadeAndClose();
            _autoCloseTimer.Start();
        }

        private FrameworkElement BuildToastContent(string title, string message, ToastType type)
        {
            var (accentColor, icon) = type switch
            {
                ToastType.Success => (System.Windows.Media.Color.FromRgb(74, 222, 128), "✅"),
                ToastType.Error => (System.Windows.Media.Color.FromRgb(248, 113, 113), "❌"),
                ToastType.Warning => (System.Windows.Media.Color.FromRgb(251, 146, 60), "⚠️"),
                _ => (System.Windows.Media.Color.FromRgb(0, 212, 255), "ℹ️")
            };

            var border = new System.Windows.Controls.Border
            {
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(14, 10, 14, 10),
                Cursor = System.Windows.Input.Cursors.Hand
            };

            border.Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                Color = Colors.Black,
                BlurRadius = 20,
                Opacity = 0.5,
                ShadowDepth = 0
            };

            // Background with left accent bar
            var bgBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(26, 26, 46));
            border.Background = bgBrush;
            border.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromArgb(60, accentColor.R, accentColor.G, accentColor.B));
            border.BorderThickness = new Thickness(1);

            var grid = new System.Windows.Controls.Grid();
            grid.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new GridLength(40) });
            grid.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // Icon
            var iconBlock = new System.Windows.Controls.TextBlock
            {
                Text = icon,
                FontSize = 20,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                VerticalAlignment = System.Windows.VerticalAlignment.Center
            };
            System.Windows.Controls.Grid.SetColumn(iconBlock, 0);

            // Text content
            var textStack = new System.Windows.Controls.StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(4, 0, 0, 0)
            };

            var titleBlock = new System.Windows.Controls.TextBlock
            {
                Text = title,
                Foreground = System.Windows.Media.Brushes.White,
                FontFamily = new System.Windows.Media.FontFamily("Segoe UI Semibold"),
                FontSize = 13,
                TextTrimming = TextTrimming.CharacterEllipsis
            };

            var msgBlock = new System.Windows.Controls.TextBlock
            {
                Text = message,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(176, 176, 204)),
                FontFamily = new System.Windows.Media.FontFamily("Segoe UI"),
                FontSize = 11,
                TextTrimming = TextTrimming.CharacterEllipsis,
                Margin = new Thickness(0, 2, 0, 0)
            };

            textStack.Children.Add(titleBlock);
            textStack.Children.Add(msgBlock);
            System.Windows.Controls.Grid.SetColumn(textStack, 1);

            grid.Children.Add(iconBlock);
            grid.Children.Add(textStack);

            border.Child = grid;

            // Click to close
            border.MouseDown += (s, e) => FadeAndClose();

            return border;
        }

        private void FadeAndClose()
        {
            _autoCloseTimer?.Stop();
            var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(250));
            fadeOut.Completed += (s, e) =>
            {
                Dispatcher.Invoke(() =>
                {
                    try { Close(); } catch { }
                });
            };
            BeginAnimation(OpacityProperty, fadeOut);
        }

        public void ForceClose()
        {
            _autoCloseTimer?.Stop();
            try { Close(); } catch { }
        }
    }
}
