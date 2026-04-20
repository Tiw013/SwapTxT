using System.Windows;
using System.Windows.Input;

namespace TranslatorApp
{
    public class HelpWindow : Window
    {
        public HelpWindow()
        {
            Title = "TranslatorApp — วิธีใช้งาน";
            Width = 480;
            Height = 520;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = System.Windows.Media.Brushes.Transparent;
            ResizeMode = ResizeMode.NoResize;

            Content = BuildUI();
        }

        private FrameworkElement BuildUI()
        {
            var outerBorder = new System.Windows.Controls.Border
            {
                CornerRadius = new CornerRadius(16),
                Background = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(15, 15, 26)),
                BorderBrush = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(42, 42, 74)),
                BorderThickness = new Thickness(1)
            };
            outerBorder.Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                Color = System.Windows.Media.Colors.Black,
                BlurRadius = 30,
                Opacity = 0.6,
                ShadowDepth = 0
            };

            var grid = new System.Windows.Controls.Grid();
            grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = new GridLength(64) });
            grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            // Title bar
            var titleBar = new System.Windows.Controls.Border
            {
                Background = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(26, 26, 46)),
                CornerRadius = new CornerRadius(16, 16, 0, 0),
                Padding = new Thickness(20, 0, 20, 0)
            };
            titleBar.MouseDown += (s, e) => { if (e.ChangedButton == MouseButton.Left) DragMove(); };

            var titleGrid = new System.Windows.Controls.Grid();
            var titleText = new System.Windows.Controls.TextBlock
            {
                Text = "❓  วิธีใช้งาน TranslatorApp",
                Foreground = System.Windows.Media.Brushes.White,
                FontFamily = new System.Windows.Media.FontFamily("Segoe UI Semibold"),
                FontSize = 15,
                VerticalAlignment = System.Windows.VerticalAlignment.Center
            };
            var closeBtn = new System.Windows.Controls.Button
            {
                Content = "✕",
                Width = 28, Height = 28,
                Background = System.Windows.Media.Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Foreground = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(176, 176, 204)),
                HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
                VerticalAlignment = System.Windows.VerticalAlignment.Center,
                Cursor = System.Windows.Input.Cursors.Hand,
                FontSize = 13
            };
            closeBtn.Click += (s, e) => Close();
            titleGrid.Children.Add(titleText);
            titleGrid.Children.Add(closeBtn);
            titleBar.Child = titleGrid;

            System.Windows.Controls.Grid.SetRow(titleBar, 0);
            grid.Children.Add(titleBar);

            // Content
            var scroll = new System.Windows.Controls.ScrollViewer
            {
                VerticalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Auto,
                Margin = new Thickness(20)
            };

            var stack = new System.Windows.Controls.StackPanel();

            string[] steps = {
                "1️⃣  เลือก (คลุมดำ) ข้อความที่ต้องการแปล",
                "2️⃣  กด Ctrl + Space (หรือคีย์ลัดที่ตั้งไว้)",
                "3️⃣  รอสักครู่ โปรแกรมจะแปลและวางข้อความกลับอัตโนมัติ",
                "4️⃣  ข้อความจะถูกแทนที่ด้วยคำแปลทันที!"
            };

            foreach (var step in steps)
            {
                var stepBorder = new System.Windows.Controls.Border
                {
                    Background = new System.Windows.Media.SolidColorBrush(
                        System.Windows.Media.Color.FromRgb(22, 33, 62)),
                    CornerRadius = new CornerRadius(10),
                    Padding = new Thickness(16, 12, 16, 12),
                    Margin = new Thickness(0, 0, 0, 8)
                };
                var stepText = new System.Windows.Controls.TextBlock
                {
                    Text = step,
                    Foreground = new System.Windows.Media.SolidColorBrush(
                        System.Windows.Media.Color.FromRgb(176, 176, 204)),
                    FontFamily = new System.Windows.Media.FontFamily("Segoe UI"),
                    FontSize = 13,
                    LineHeight = 20,
                    TextWrapping = TextWrapping.Wrap
                };
                stepBorder.Child = stepText;
                stack.Children.Add(stepBorder);
            }

            // Tips
            var tipBorder = new System.Windows.Controls.Border
            {
                Background = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromArgb(30, 255, 107, 157)),
                BorderBrush = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromArgb(80, 255, 107, 157)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(16, 12, 16, 12),
                Margin = new Thickness(0, 8, 0, 0)
            };
            var tipText = new System.Windows.Controls.TextBlock
            {
                Text = "💡 เคล็ดลับ:\n• โปรแกรมตรวจสอบภาษาอัตโนมัติ\n" +
                       "  หากข้อความเป็นไทย → แปลเป็นอังกฤษ\n" +
                       "  หากข้อความเป็นอังกฤษ → แปลเป็นไทย\n" +
                       "• ใช้งานได้ทุกแอป: Chrome, Word, Excel, Line ฯลฯ",
                Foreground = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(255, 182, 210)),
                FontFamily = new System.Windows.Media.FontFamily("Segoe UI"),
                FontSize = 12,
                LineHeight = 20,
                TextWrapping = TextWrapping.Wrap
            };
            tipBorder.Child = tipText;
            stack.Children.Add(tipBorder);

            scroll.Content = stack;
            System.Windows.Controls.Grid.SetRow(scroll, 1);
            grid.Children.Add(scroll);

            outerBorder.Child = grid;
            return outerBorder;
        }
    }
}
