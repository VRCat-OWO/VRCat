using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace VRChatGuide
{
    /// <summary>
    /// 启动动画（简洁版）：透明背景，画面上只有 VRChat 图标本身。
    /// 图标按 VRChat.png 的比例重画成纯矢量：圆角对话泡泡 + 右下尾巴 + VRChat 字标。
    /// 节奏：淡入放大 → 轮廓从左到右画出 → 填充与字标浮现 → 停一拍 → 淡出放大，交棒主窗口。
    /// 点击 / 任意键 / Esc 跳过，命令行 --nosplash 完全跳过。
    /// </summary>
    public static class Splash
    {
        static readonly Color C1 = Color.FromRgb(0x00, 0xD1, 0xFF);
        static readonly Color C2 = Color.FromRgb(0x2D, 0x7B, 0xFF);
        static readonly Color C3 = Color.FromRgb(0x7A, 0x5C, 0xFF);
        static readonly Color Ink0 = Color.FromRgb(0x06, 0x0B, 0x14);
        static readonly Color Ink1 = Color.FromRgb(0x13, 0x22, 0x3C);

        public const double TotalMs = 1750;

        const double T_IN = 0.02;
        const double T_DRAW = 0.16;
        const double T_FILL = 0.52;
        const double T_WORD = 0.62;
        const double T_OUT = 1.18;
        const double OUT_LEN = 0.30;

        /// <summary>播放启动动画，结束后淡入主窗口</summary>
        public static void Play(Window main)
        {
            var sp = Build(out var start);
            bool done = false;
            void Reveal()
            {
                if (done) return;
                done = true;
                main.Opacity = 0;
                main.Show();
                main.BeginAnimation(UIElement.OpacityProperty,
                    new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(240))
                    { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });
                sp.Close();
            }

            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(TotalMs) };
            timer.Tick += (s, e) => { timer.Stop(); Reveal(); };
            timer.Start();

            sp.MouseDown += (s, e) => { timer.Stop(); Reveal(); };
            sp.KeyDown += (s, e) => { timer.Stop(); Reveal(); };
            sp.Closed += (s, e) => { timer.Stop(); Reveal(); };

            sp.Show();
            sp.Activate();
            start();
        }

        // ── 界面：透明窗口 + 一个图标 ───────────────────────────────────────
        static Window Build(out Action start)
        {
            var win = new Window
            {
                WindowStyle = WindowStyle.None,
                AllowsTransparency = true,
                Background = Brushes.Transparent,
                ShowInTaskbar = false,
                ResizeMode = ResizeMode.NoResize,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                Width = 660,
                Height = 390,
                Topmost = true
            };

            // 图标几何（1000×520 视口，比例按 VRChat.png 量取）
            var bodyGeo = Geometry.Parse(
                "M 60,4 H 940 A 56,56 0 0 1 996,60 V 300 A 56,56 0 0 1 940,356 " +
                "H 60 A 56,56 0 0 1 4,300 V 60 A 56,56 0 0 1 60,4 Z");
            var tailGeo = Geometry.Parse("M 800,330 L 892,492 L 962,330 Z");

            var bubble = new Path
            {
                Data = new GeometryGroup { Children = { bodyGeo, tailGeo } },
                Fill = new LinearGradientBrush
                {
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(1, 1),
                    GradientStops =
                    {
                        new GradientStop(Ink1, 0.0),
                        new GradientStop(Ink0, 1.0)
                    }
                },
                Opacity = 0,
                Effect = new DropShadowEffect
                {
                    BlurRadius = 44, ShadowDepth = 0, Opacity = 0.55,
                    Color = Color.FromRgb(0x00, 0x9C, 0xFF)
                }
            };

            var outline = new Path
            {
                Data = bodyGeo,
                Stroke = new LinearGradientBrush
                {
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(1, 0),
                    GradientStops =
                    {
                        new GradientStop(C1, 0.0),
                        new GradientStop(C2, 0.55),
                        new GradientStop(C3, 1.0)
                    }
                },
                StrokeThickness = 22,
                StrokeLineJoin = PenLineJoin.Round,
                Opacity = 0
            };
            var outlineMask = new RectangleGeometry(new Rect(0, 0, 0, 520));
            outline.Clip = outlineMask;

            var tailStroke = new Path
            {
                Data = tailGeo,
                Stroke = new SolidColorBrush(C1),
                StrokeThickness = 15,
                StrokeLineJoin = PenLineJoin.Round,
                Opacity = 0,
                RenderTransformOrigin = new Point(0.83, 0.0)
            };
            var tailScale = new ScaleTransform(0.3, 0.3);
            tailStroke.RenderTransform = tailScale;

            // 字标 VRChat
            var word = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            var letters = new List<TextBlock>();
            var wordBrush = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(0, 1),
                GradientStops =
                {
                    new GradientStop(Colors.White, 0.0),
                    new GradientStop(Color.FromRgb(0xC8, 0xF3, 0xFF), 0.6),
                    new GradientStop(C1, 1.0)
                }
            };
            const string text = "VRChat";
            for (int i = 0; i < text.Length; i++)
            {
                var tb = new TextBlock
                {
                    Text = text[i].ToString(),
                    FontFamily = new FontFamily("Segoe UI Semibold, Segoe UI, Microsoft YaHei UI"),
                    FontSize = 196,
                    FontWeight = FontWeights.Bold,
                    Foreground = wordBrush,
                    Opacity = 0,
                    Margin = new Thickness(0, 0, i == text.Length - 1 ? 0 : 8, 0),
                    RenderTransform = new TranslateTransform(0, 18)
                };
                letters.Add(tb);
                word.Children.Add(tb);
            }

            var wordHost = new Grid { Width = 1000, Height = 356, VerticalAlignment = VerticalAlignment.Top };
            wordHost.Children.Add(word);

            var logo = new Canvas { Width = 1000, Height = 520 };
            logo.Children.Add(bubble);
            logo.Children.Add(wordHost);
            logo.Children.Add(outline);
            logo.Children.Add(tailStroke);

            var logoBox = new Viewbox
            {
                Width = 570,
                Height = 297,
                Child = logo,
                Stretch = Stretch.Uniform,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                RenderTransformOrigin = new Point(0.5, 0.5)
            };
            var logoScale = new ScaleTransform(0.88, 0.88);
            logoBox.RenderTransform = logoScale;

            var host = new Grid();
            host.Children.Add(logoBox);
            win.Content = host;

            start = () =>
            {
                logoBox.BeginAnimation(UIElement.OpacityProperty, Anim(0, 1, T_IN, 0.30, EaseOut));
                logoScale.BeginAnimation(ScaleTransform.ScaleXProperty, Anim(0.88, 1, T_IN, 0.55, EaseOut));
                logoScale.BeginAnimation(ScaleTransform.ScaleYProperty, Anim(0.88, 1, T_IN, 0.55, EaseOut));

                outline.BeginAnimation(UIElement.OpacityProperty, Anim(0, 1, T_DRAW, 0.10));
                outlineMask.BeginAnimation(RectangleGeometry.RectProperty,
                    new RectAnimation(new Rect(0, 0, 0, 520), new Rect(0, 0, 1010, 520),
                        new Duration(TimeSpan.FromSeconds(0.62)))
                    { BeginTime = TimeSpan.FromSeconds(T_DRAW), EasingFunction = EaseInOut });

                bubble.BeginAnimation(UIElement.OpacityProperty, Anim(0, 1, T_FILL, 0.26));
                tailStroke.BeginAnimation(UIElement.OpacityProperty, Anim(0, 1, T_FILL + 0.08, 0.16));
                tailScale.BeginAnimation(ScaleTransform.ScaleXProperty, Anim(0.3, 1, T_FILL + 0.06, 0.34, Back));
                tailScale.BeginAnimation(ScaleTransform.ScaleYProperty, Anim(0.3, 1, T_FILL + 0.06, 0.34, Back));

                for (int i = 0; i < letters.Count; i++)
                {
                    double t = T_WORD + i * 0.05;
                    letters[i].BeginAnimation(UIElement.OpacityProperty, Anim(0, 1, t, 0.26, EaseOut));
                    ((TranslateTransform)letters[i].RenderTransform)
                        .BeginAnimation(TranslateTransform.YProperty, Anim(18, 0, t, 0.34, EaseOut));
                }

                logoBox.BeginAnimation(UIElement.OpacityProperty,
                    new DoubleAnimationUsingKeyFrames
                    {
                        KeyFrames =
                        {
                            new EasingDoubleKeyFrame(1, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(T_OUT))),
                            new EasingDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(T_OUT + OUT_LEN)))
                        }
                    });
                logoScale.BeginAnimation(ScaleTransform.ScaleXProperty, Anim(1, 1.06, T_OUT, OUT_LEN, EaseIn));
                logoScale.BeginAnimation(ScaleTransform.ScaleYProperty, Anim(1, 1.06, T_OUT, OUT_LEN, EaseIn));
            };

            return win;
        }

        // ── 自检：抓几个时间点的帧（--splash-test） ─────────────────────────
        public static void Qa(string dir)
        {
            dir = Environment.ExpandEnvironmentVariables(dir);
            System.IO.Directory.CreateDirectory(dir);
            var win = Build(out var start);
            var log = new System.Text.StringBuilder();
            double[] times = { 0.30, 0.80, 1.10 };
            int idx = 0;
            win.Loaded += (s1, e1) =>
            {
                start();
                var sw = System.Diagnostics.Stopwatch.StartNew();
                var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(25) };
                timer.Tick += (s2, e2) =>
                {
                    if (idx >= times.Length) return;
                    if (sw.Elapsed.TotalSeconds < times[idx]) return;
                    win.UpdateLayout();
                    var rtb = new RenderTargetBitmap((int)win.Width, (int)win.Height, 96, 96, PixelFormats.Pbgra32);
                    rtb.Render((Visual)win.Content);
                    var enc = new PngBitmapEncoder();
                    enc.Frames.Add(BitmapFrame.Create(rtb));
                    using (var fs = System.IO.File.Create(System.IO.Path.Combine(dir, "frame" + idx + ".png")))
                        enc.Save(fs);
                    log.AppendLine("frame" + idx + ".png  t=" + times[idx].ToString("0.00") + "s");
                    idx++;
                    if (idx >= times.Length)
                    {
                        timer.Stop();
                        System.IO.File.WriteAllText(System.IO.Path.Combine(dir, "log.txt"), log.ToString());
                        Application.Current.Shutdown(0);
                    }
                };
                timer.Start();
            };
            win.Show();
        }

        // ── 小工具 ─────────────────────────────────────────────────────────
        static IEasingFunction EaseOut => new CubicEase { EasingMode = EasingMode.EaseOut };
        static IEasingFunction EaseIn => new CubicEase { EasingMode = EasingMode.EaseIn };
        static IEasingFunction EaseInOut => new CubicEase { EasingMode = EasingMode.EaseInOut };
        static IEasingFunction Back => new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.5 };

        static DoubleAnimation Anim(double from, double to, double beginSec, double durSec, IEasingFunction ease = null)
        {
            var a = new DoubleAnimation(from, to, TimeSpan.FromSeconds(durSec))
            { BeginTime = TimeSpan.FromSeconds(beginSec) };
            if (ease != null) a.EasingFunction = ease;
            return a;
        }
    }
}
