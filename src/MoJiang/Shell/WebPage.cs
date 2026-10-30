using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;

namespace VRChatGuide
{
    /// <summary>内嵌 VCC 中文镜像站（WebView2），失败时降级为说明面板</summary>
    public sealed class WebPage : Grid
    {
        public const string HomeUrl = "https://vcc.vrczh.org/";

        static readonly FontFamily Font = new FontFamily("Microsoft YaHei UI, Microsoft YaHei, Segoe UI");

        WebView2 _web;
        TextBlock _urlText;
        Border _fallback;
        TextBlock _fallbackMsg;
        bool _ready;
        bool _inited;
        bool _failed;

        public WebPage()
        {
            RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            Children.Add(BuildToolbar());

            var body = new Grid { Background = Brushes.White };
            _web = new WebView2 { Visibility = Visibility.Collapsed };
            body.Children.Add(_web);

            _fallback = BuildFallback();
            body.Children.Add(_fallback);

            Grid.SetRow(body, 1);
            Children.Add(body);

            Loaded += OnLoaded;
        }

        FrameworkElement BuildToolbar()
        {
            var bar = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(0xFB, 0xFC, 0xFE)),
                BorderBrush = (Brush)Application.Current.Resources["Line"],
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(10, 6, 10, 6)
            };

            var g = new Grid();
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var left = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            left.Children.Add(NavBtn("←", "后退", () => { if (_ready && _web.CoreWebView2.CanGoBack) _web.CoreWebView2.GoBack(); }));
            left.Children.Add(NavBtn("→", "前进", () => { if (_ready && _web.CoreWebView2.CanGoForward) _web.CoreWebView2.GoForward(); }));
            left.Children.Add(NavBtn("⟳", "刷新", () =>
            {
                if (_ready && _web.CoreWebView2 != null) _web.CoreWebView2.Reload();
                else { _inited = false; _failed = false; TryInit(); }
            }));
            left.Children.Add(NavBtn("⌂", "回到首页", () => { if (_ready) _web.CoreWebView2.Navigate(HomeUrl); }));
            Grid.SetColumn(left, 0);
            g.Children.Add(left);

            var urlWrap = new Border
            {
                Background = Brushes.White,
                BorderBrush = (Brush)Application.Current.Resources["Line"],
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(7),
                Margin = new Thickness(8, 0, 8, 0),
                Padding = new Thickness(10, 4, 10, 4)
            };
            _urlText = new TextBlock
            {
                Text = HomeUrl,
                FontFamily = Font,
                FontSize = 11.5,
                Foreground = new SolidColorBrush(Color.FromRgb(0x4A, 0x5A, 0x6C)),
                TextTrimming = TextTrimming.CharacterEllipsis,
                VerticalAlignment = VerticalAlignment.Center
            };
            urlWrap.Child = _urlText;
            Grid.SetColumn(urlWrap, 1);
            g.Children.Add(urlWrap);

            var right = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            right.Children.Add(NavBtn("↗ 系统浏览器", "用默认浏览器打开", () => Render.OpenUrl(HomeUrl), wide: true));
            Grid.SetColumn(right, 2);
            g.Children.Add(right);

            bar.Child = g;
            return bar;
        }

        FrameworkElement NavBtn(string text, string tip, Action click, bool wide = false)
        {
            var b = new Button
            {
                Style = (Style)Application.Current.Resources["GhostButton"],
                Content = text,
                ToolTip = tip,
                FontSize = wide ? 11.5 : 13,
                Padding = new Thickness(wide ? 9 : 7, wide ? 4 : 3, wide ? 9 : 7, wide ? 4 : 3),
                Margin = new Thickness(1, 0, 1, 0)
            };
            b.Click += (s, e) => click();
            return b;
        }

        Border BuildFallback()
        {
            var sp = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                MaxWidth = 560
            };
            sp.Children.Add(new TextBlock
            {
                Text = "VCC 中文镜像", FontFamily = Font, FontSize = 20,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(0x1F, 0x4E, 0x79)),
                HorizontalAlignment = HorizontalAlignment.Center
            });
            sp.Children.Add(new TextBlock
            {
                Text = HomeUrl, FontFamily = Font, FontSize = 13,
                Foreground = new SolidColorBrush(Color.FromRgb(0x2E, 0x74, 0xB5)),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 6, 0, 14)
            });
            _fallbackMsg = new TextBlock
            {
                Text = "正在加载内嵌浏览器…",
                FontFamily = Font, FontSize = 12.5,
                Foreground = new SolidColorBrush(Color.FromRgb(0x5A, 0x6A, 0x7D)),
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Center,
                LineHeight = 22
            };
            sp.Children.Add(_fallbackMsg);

            var btnRow = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 16, 0, 0)
            };
            var retry = new Button
            {
                Style = (Style)Application.Current.Resources["GhostButton"],
                Content = "重新加载",
                FontSize = 12.5,
                Padding = new Thickness(14, 6, 14, 6),
                Margin = new Thickness(4, 0, 4, 0)
            };
            retry.Click += (s, e) => { _inited = false; _failed = false; TryInit(); };
            var open = new Button
            {
                Style = (Style)Application.Current.Resources["GhostButton"],
                Content = "用系统浏览器打开 ↗",
                FontSize = 12.5,
                Padding = new Thickness(14, 6, 14, 6),
                Margin = new Thickness(4, 0, 4, 0)
            };
            open.Click += (s, e) => Render.OpenUrl(HomeUrl);
            btnRow.Children.Add(retry);
            btnRow.Children.Add(open);
            sp.Children.Add(btnRow);

            return new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(0xF8, 0xFA, 0xFD)),
                Child = sp
            };
        }

        void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (!_inited) TryInit();
        }

        async void TryInit()
        {
            if (_inited) return;
            _inited = true;
            _failed = false;
            _fallbackMsg.Text = "正在加载内嵌浏览器…（首次启动需要几秒）";

            try
            {
                string dataDir = Path.Combine(State.Dir, "WebView2");
                Directory.CreateDirectory(dataDir);

                var env = await CoreWebView2Environment.CreateAsync(null, dataDir);
                await _web.EnsureCoreWebView2Async(env);

                var core = _web.CoreWebView2;
                core.Settings.AreDevToolsEnabled = false;
                core.Settings.IsStatusBarEnabled = false;
                core.Settings.AreDefaultContextMenusEnabled = true;
                core.Settings.IsZoomControlEnabled = true;

                core.NavigationStarting += (s, args) => _urlText.Text = args.Uri;
                core.NavigationCompleted += (s, args) =>
                {
                    if (!args.IsSuccess && !_failed)
                    {
                        ShowFallback("页面加载失败（可能没有网络，或该站点暂时不可用）。\n可以点「重新加载」重试，或直接用系统浏览器打开。");
                    }
                    else
                    {
                        _web.Visibility = Visibility.Visible;
                        _fallback.Visibility = Visibility.Collapsed;
                    }
                };
                core.NewWindowRequested += (s, args) =>
                {
                    args.Handled = true;
                    Render.OpenUrl(args.Uri);
                };
                core.ProcessFailed += (s, args) =>
                {
                    ShowFallback("内嵌浏览器进程异常退出。\n可以点「重新加载」，或用系统浏览器打开。");
                };

                _ready = true;
                _web.Visibility = Visibility.Visible;
                _fallback.Visibility = Visibility.Collapsed;
                core.Navigate(HomeUrl);
            }
            catch (Exception ex)
            {
                ShowFallback("这台机器上没有可用的 WebView2 运行时，内置浏览器无法启动。\n\n" +
                             "解决办法：安装 Microsoft Edge WebView2 Runtime（首次安装 VRChat 或 Edge 一般已自带），" +
                             "或者直接用系统浏览器打开镜像站。\n\n技术信息：" + ex.Message);
            }
        }

        void ShowFallback(string msg)
        {
            _failed = true;
            _web.Visibility = Visibility.Collapsed;
            _fallback.Visibility = Visibility.Visible;
            _fallbackMsg.Text = msg;
        }
    }
}
