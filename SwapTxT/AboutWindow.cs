using SwapTxT.Models;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

// Explicit WPF aliases — prevent collision with System.Drawing / System.Windows.Forms
using WpfBrushes    = System.Windows.Media.Brushes;
using WpfButton     = System.Windows.Controls.Button;
using WpfColor      = System.Windows.Media.Color;
using WpfControl    = System.Windows.Controls.Control;
using WpfCursors    = System.Windows.Input.Cursors;
using WpfFontFamily = System.Windows.Media.FontFamily;
using WpfGradStop   = System.Windows.Media.GradientStop;
using WpfLinGrad    = System.Windows.Media.LinearGradientBrush;
using WpfOrientation = System.Windows.Controls.Orientation;
using WpfPoint      = System.Windows.Point;
using WpfScrollBar  = System.Windows.Controls.Primitives.ScrollBar;
using WpfSolidBrush = System.Windows.Media.SolidColorBrush;
using WpfHA         = System.Windows.HorizontalAlignment;
using WpfVA         = System.Windows.VerticalAlignment;

namespace SwapTxT
{
    public class AboutWindow : Window
    {
        private readonly AppSettings _settings;

        public AboutWindow(AppSettings settings)
        {
            _settings = settings;
            Title = "About SwapTxT";
            Width = 430;
            Height = 530;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = WpfBrushes.Transparent;
            ResizeMode = ResizeMode.NoResize;
            ShowInTaskbar = false;
            Content = BuildUI();
        }

        // ── Colour helper ─────────────────────────────────────────────────────────
        private static WpfSolidBrush C(byte r, byte g, byte b)
            => new WpfSolidBrush(WpfColor.FromRgb(r, g, b));

        private static WpfLinGrad Grad(WpfPoint a, WpfPoint b, WpfColor c0, WpfColor c1)
        {
            var lg = new WpfLinGrad { StartPoint = a, EndPoint = b };
            lg.GradientStops.Add(new WpfGradStop(c0, 0));
            lg.GradientStops.Add(new WpfGradStop(c1, 1));
            return lg;
        }

        // ── Main UI ───────────────────────────────────────────────────────────────
        private FrameworkElement BuildUI()
        {
            var outer = new Border
            {
                CornerRadius    = new CornerRadius(18),
                Background      = C(0x0D, 0x0D, 0x1A),
                BorderBrush     = C(0x2D, 0x2D, 0x55),
                BorderThickness = new Thickness(1),
                Margin          = new Thickness(12)
            };

            var rootGrid = new Grid();
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(60) });
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(60) });

            // ── Title Bar ─────────────────────────────────────────────────────
            var titleBar = new Border
            {
                Background    = C(0x15, 0x15, 0x2B),
                CornerRadius  = new CornerRadius(18, 18, 0, 0),
                Padding       = new Thickness(20, 0, 16, 0)
            };
            titleBar.MouseLeftButtonDown += (s, e) =>
            {
                if (((MouseButtonEventArgs)e).ChangedButton == MouseButton.Left) DragMove();
            };

            var titleGrid = new Grid();
            // App icon badge (gradient S)
            var iconBadge = new Border
            {
                Width       = 28, Height = 28,
                CornerRadius = new CornerRadius(8),
                Margin      = new Thickness(0, 0, 10, 0),
                Background  = Grad(new WpfPoint(0, 0), new WpfPoint(1, 1),
                                   WpfColor.FromRgb(0x6C, 0x63, 0xFF),
                                   WpfColor.FromRgb(0xA7, 0x8B, 0xFA)),
                HorizontalAlignment = WpfHA.Left,
                VerticalAlignment   = WpfVA.Center,
                Child = new TextBlock
                {
                    Text                = "S",
                    Foreground          = WpfBrushes.White,
                    FontSize            = 14,
                    FontWeight          = FontWeights.Bold,
                    FontFamily          = new WpfFontFamily("Segoe UI"),
                    HorizontalAlignment = WpfHA.Center,
                    VerticalAlignment   = WpfVA.Center
                }
            };
            var titleText = new StackPanel
            {
                Orientation         = WpfOrientation.Horizontal,
                VerticalAlignment   = WpfVA.Center,
                HorizontalAlignment = WpfHA.Left
            };
            titleText.Children.Add(iconBadge);
            titleText.Children.Add(new TextBlock
            {
                Text          = "About SwapTxT",
                Foreground    = C(0xF0, 0xEE, 0xFF),
                FontFamily    = new WpfFontFamily("Segoe UI"),
                FontSize      = 15,
                FontWeight    = FontWeights.SemiBold,
                VerticalAlignment = WpfVA.Center
            });
            titleGrid.Children.Add(titleText);

            var closeBtn = MakeIconButton("✕", C(0x88, 0x84, 0xA8));
            closeBtn.HorizontalAlignment = WpfHA.Right;
            closeBtn.Click += (s, e) => Close();
            titleGrid.Children.Add(closeBtn);
            titleBar.Child = titleGrid;
            Grid.SetRow(titleBar, 0);
            rootGrid.Children.Add(titleBar);

            // ── Content scroll area ───────────────────────────────────────────
            var scroll = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Margin = new Thickness(20, 16, 14, 8)
            };
            if (System.Windows.Application.Current != null && System.Windows.Application.Current.TryFindResource("DarkScrollViewer") is Style scrollStyle)
            {
                scroll.Style = scrollStyle;
            }


            var stack = new StackPanel();

            // Logo
            var logoPanel = new StackPanel
            {
                HorizontalAlignment = WpfHA.Center,
                Margin = new Thickness(0, 0, 0, 16)
            };
            var bigBadge = new Border
            {
                Width       = 62, Height = 62,
                CornerRadius = new CornerRadius(16),
                HorizontalAlignment = WpfHA.Center,
                Margin      = new Thickness(0, 0, 0, 10),
                Background  = Grad(new WpfPoint(0, 0), new WpfPoint(1, 1),
                                   WpfColor.FromRgb(0x6C, 0x63, 0xFF),
                                   WpfColor.FromRgb(0xA7, 0x8B, 0xFA)),
                Child = new TextBlock
                {
                    Text                = "S",
                    Foreground          = WpfBrushes.White,
                    FontSize            = 28,
                    FontWeight          = FontWeights.Bold,
                    FontFamily          = new WpfFontFamily("Segoe UI"),
                    HorizontalAlignment = WpfHA.Center,
                    VerticalAlignment   = WpfVA.Center
                }
            };
            logoPanel.Children.Add(bigBadge);
            logoPanel.Children.Add(new TextBlock
            {
                Text                = "SwapTxT",
                Foreground          = C(0xF0, 0xEE, 0xFF),
                FontSize            = 22,
                FontWeight          = FontWeights.Bold,
                FontFamily          = new WpfFontFamily("Segoe UI"),
                HorizontalAlignment = WpfHA.Center
            });
            logoPanel.Children.Add(new TextBlock
            {
                Text                = $"Version {AppSettings.AppVersion}",
                Foreground          = C(0x88, 0x84, 0xA8),
                FontSize            = 12,
                FontFamily          = new WpfFontFamily("Segoe UI"),
                HorizontalAlignment = WpfHA.Center,
                Margin              = new Thickness(0, 2, 0, 0)
            });
            stack.Children.Add(logoPanel);

            // Cards
            stack.Children.Add(InfoCard(
                "🌐  What is SwapTxT?",
                "SwapTxT translates text in-place in any app. Select text, press " +
                "your hotkey, and it is instantly replaced with the translation. " +
                "Supports Google Translate (free) and AI providers."));
            stack.Children.Add(new Border { Height = 9 });
            stack.Children.Add(InfoCard(
                "👨‍💻  Developer",
                "Developed with ❤️ by TIXS\n" +
                "If you enjoy SwapTxT, please consider supporting the developer!"));
            stack.Children.Add(new Border { Height = 9 });
            stack.Children.Add(InfoCard(
                "✨  Features",
                "• Global hotkey in any app — works with Thai keyboard\n" +
                "• Manual select or Auto-detect (full text) mode\n" +
                "• Google Translate (free) or Custom AI\n" +
                "• OpenAI · Gemini · OpenRouter support\n" +
                "• Run on startup · Notifications"));
            stack.Children.Add(new Border { Height = 12 });

            scroll.Content = stack;
            Grid.SetRow(scroll, 1);
            rootGrid.Children.Add(scroll);

            // ── Footer: Check Version + Donate ────────────────────────────────
            var footer = new Border
            {
                Background      = C(0x15, 0x15, 0x2B),
                CornerRadius    = new CornerRadius(0, 0, 18, 18),
                BorderBrush     = C(0x2D, 0x2D, 0x55),
                BorderThickness = new Thickness(0, 1, 0, 0),
                Padding         = new Thickness(20, 0, 20, 0)
            };
            var footerGrid = new Grid();
            footerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            footerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
            footerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Check version button
            var checkVerBtn = new WpfButton
            {
                Content             = "🔄  Check for Updates",
                Height              = 36,
                Padding             = new Thickness(16, 0, 16, 0),
                Background          = C(0x1E, 0x1E, 0x38),
                Foreground          = C(0x88, 0x84, 0xA8),
                BorderBrush         = C(0x2D, 0x2D, 0x55),
                BorderThickness     = new Thickness(1),
                FontSize            = 12,
                FontFamily          = new WpfFontFamily("Segoe UI"),
                Cursor              = WpfCursors.Hand,
                VerticalAlignment   = WpfVA.Center,
                HorizontalAlignment = WpfHA.Left
            };
            checkVerBtn.Template = RoundedButtonTemplate(10);
            checkVerBtn.Click += OnCheckVersion;
            Grid.SetColumn(checkVerBtn, 0);
            footerGrid.Children.Add(checkVerBtn);

            var coffeeBtn = new Border
            {
                CornerRadius        = new CornerRadius(10),
                Background          = Grad(new WpfPoint(0, 0), new WpfPoint(1, 0),
                                          WpfColor.FromRgb(0xFF, 0xD0, 0x42),
                                          WpfColor.FromRgb(0xFF, 0x9A, 0x00)),
                Cursor              = WpfCursors.Hand,
                Padding             = new Thickness(16, 0, 16, 0),
                Height              = 36,
                VerticalAlignment   = WpfVA.Center,
                Child               = new StackPanel
                {
                    Orientation         = WpfOrientation.Horizontal,
                    VerticalAlignment   = WpfVA.Center,
                    HorizontalAlignment = WpfHA.Center,
                    Children = {
                        new TextBlock { Text = "💖", FontSize = 13, VerticalAlignment = WpfVA.Center },
                        new TextBlock { 
                            Text = "  Support Developer", 
                            Foreground = C(0x3D, 0x20, 0x00), 
                            FontFamily = new WpfFontFamily("Segoe UI Semibold"), 
                            FontSize = 12, 
                            VerticalAlignment = WpfVA.Center 
                        }
                    }
                }
            };
            coffeeBtn.MouseLeftButtonUp += (s, e) =>
            {
                try { Process.Start(new ProcessStartInfo { FileName = _settings.DonateUrl, UseShellExecute = true }); }
                catch { }
            };
            Grid.SetColumn(coffeeBtn, 2);
            footerGrid.Children.Add(coffeeBtn);

            footer.Child = footerGrid;
            Grid.SetRow(footer, 2);
            rootGrid.Children.Add(footer);

            outer.Child = rootGrid;
            return outer;
        }

        private void OnCheckVersion(object sender, RoutedEventArgs e)
        {
            try
            {
                // Force an immediate update check from your specific GitHub XML
                AutoUpdaterDotNET.AutoUpdater.Start("https://raw.githubusercontent.com/Tiw013/SwapTxT/main/update.xml");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Could not check for updates: " + ex.Message);
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────────
        private static WpfButton MakeIconButton(string label, WpfSolidBrush fg)
        {
            var btn = new WpfButton
            {
                Content         = label,
                Width           = 32,
                Height          = 32,
                Background      = WpfBrushes.Transparent,
                BorderThickness = new Thickness(0),
                Foreground      = fg,
                VerticalAlignment = WpfVA.Center,
                Cursor          = WpfCursors.Hand,
                FontSize        = 14
            };
            return btn;
        }

        private static ControlTemplate RoundedButtonTemplate(double radius)
        {
            var bd = new FrameworkElementFactory(typeof(Border));
            bd.SetValue(Border.BackgroundProperty,      new TemplateBindingExtension(WpfControl.BackgroundProperty));
            bd.SetValue(Border.BorderBrushProperty,     new TemplateBindingExtension(WpfControl.BorderBrushProperty));
            bd.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(WpfControl.BorderThicknessProperty));
            bd.SetValue(Border.CornerRadiusProperty,    new CornerRadius(radius));
            bd.SetValue(Border.PaddingProperty,         new TemplateBindingExtension(WpfControl.PaddingProperty));
            var cp = new FrameworkElementFactory(typeof(ContentPresenter));
            cp.SetValue(ContentPresenter.HorizontalAlignmentProperty, WpfHA.Center);
            cp.SetValue(ContentPresenter.VerticalAlignmentProperty,   WpfVA.Center);
            bd.AppendChild(cp);
            var t = new ControlTemplate(typeof(WpfButton)) { VisualTree = bd };
            return t;
        }

        private static Border InfoCard(string title, string body)
        {
            var sp = new StackPanel();
            sp.Children.Add(new TextBlock
            {
                Text       = title,
                Foreground = C(0xA7, 0x8B, 0xFA),
                FontSize   = 12,
                FontWeight = FontWeights.SemiBold,
                FontFamily = new WpfFontFamily("Segoe UI"),
                Margin     = new Thickness(0, 0, 0, 5)
            });
            sp.Children.Add(new TextBlock
            {
                Text         = body,
                Foreground   = C(0xB0, 0xAE, 0xCC),
                FontSize     = 11.5,
                FontFamily   = new WpfFontFamily("Segoe UI"),
                TextWrapping = TextWrapping.Wrap,
                LineHeight   = 19
            });
            return new Border
            {
                Background      = C(0x15, 0x15, 0x2B),
                BorderBrush     = C(0x2D, 0x2D, 0x55),
                BorderThickness = new Thickness(1),
                CornerRadius    = new CornerRadius(12),
                Padding         = new Thickness(14, 11, 14, 11),
                Child           = sp
            };
        }
    }
}
