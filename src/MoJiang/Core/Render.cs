using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Linq;
using System.Windows.Media.Imaging;

namespace VRChatGuide
{
    /// <summary>把内容模型渲染成 WPF 控件（双语文案 / 字号缩放都在这里生效）</summary>
    public static class Render
    {
        public static readonly FontFamily UiFont = new FontFamily("Microsoft YaHei UI, Microsoft YaHei, Segoe UI");
        public static readonly FontFamily MonoFont = new FontFamily("Consolas, Cascadia Mono, Courier New");

        static Brush B(string key) => (Brush)Application.Current.Resources[key];

        public static Brush Ink => B("Ink");
        public static Brush Sub => B("Sub");
        public static Brush Accent => B("Accent");
        public static Brush Accent2 => B("Accent2");
        public static Brush Line => B("Line");

        public static double S => State.Data.FontScale;

        public static Action<string> OnImageClick;   // 由主窗口注入：打开大图

        // ── 重点模式 ────────────────────────────────────────────────────────
        // 只用「人工标注」：正文里用 ==…== 包住作者认为重要的短语/句子，
        // 提示条（提示/注意/警告/经验）的正文在重点模式下整条标注。
        // 不做关键词猜测，避免把普通句子标成重点。

        /// <summary>本次页面渲染命中的重点片段数量（主窗口用于状态栏统计）</summary>
        public static int HotCount;

        static readonly Brush HotBg = new SolidColorBrush(Color.FromRgb(0xFF, 0xEE, 0xA8));
        static readonly Brush HotFg = new SolidColorBrush(Color.FromRgb(0x4E, 0x38, 0x00));

        /// <summary>普通文本 run：按 ==…== 切片，标注段高亮，其余原样</summary>
        static void AddText(TextBlock tb, string text, double size, bool bold)
        {
            var weight = bold ? FontWeights.SemiBold : FontWeights.Normal;
            if (text.Length == 0) return;
            if (!State.HotMode || text.IndexOf("==", StringComparison.Ordinal) < 0)
            {
                tb.Inlines.Add(new Run(text) { FontWeight = weight });
                return;
            }

            var parts = text.Split(new[] { "==" }, StringSplitOptions.None);
            for (int p = 0; p < parts.Length; p++)
            {
                if (parts[p].Length == 0) continue;
                if (p % 2 == 1)                    // ==…== 之间
                {
                    HotCount++;
                    tb.Inlines.Add(new Run(parts[p])
                    {
                        FontWeight = FontWeights.SemiBold,
                        Background = HotBg,
                        Foreground = HotFg
                    });
                }
                else
                {
                    tb.Inlines.Add(new Run(parts[p]) { FontWeight = weight });
                }
            }
        }

        /// <summary>去掉 ==…== 标记，得到纯文本（自检与统计用）</summary>
        public static string StripMark(string text) => text.Replace("==", "");

        /// <summary>统计一条内容里人工标注的重点片段数</summary>
        public static int CountMarks(Item it)
        {
            int n = 0;
            foreach (var b in it.Blocks)
            {
                string t = null;
                if (b is Para p) t = p.Text;
                else if (b is Bullets bu) t = string.Join("\u0001", bu.Items);
                else if (b is Tip tp) t = tp.Text;
                else if (b is Head hd) t = hd.Text;
                else if (b is CodeBlock cb) t = cb.Note;
                else if (b is Table tt)
                    t = string.Join("\u0001", tt.Head) + "\u0001" +
                        string.Join("\u0001", tt.Rows.SelectMany(r => r));
                if (t == null) continue;
                int i = 0;
                while ((i = t.IndexOf("==", i, StringComparison.Ordinal)) >= 0) { n++; i += 2; }
            }
            return n / 2;
        }

        // ── 文本 ────────────────────────────────────────────────────────────
        public static TextBlock Inline(string text, double size, Brush fg, UiMode mode, bool bold = false)
        {
            var tb = new TextBlock
            {
                TextWrapping = TextWrapping.Wrap,
                FontFamily = UiFont,
                FontSize = size,
                LineHeight = Math.Round(size * 1.62, 1),
                LineStackingStrategy = LineStackingStrategy.BlockLineHeight,
                Foreground = fg
            };
            foreach (var seg in Txt.Parse(text))
            {
                if (!seg.IsUi)
                {
                    AddText(tb, seg.Zh, size, bold);
                    continue;
                }
                switch (mode)
                {
                    case UiMode.Chinese:
                        tb.Inlines.Add(UiRun(seg.Zh, size, bold));
                        break;
                    case UiMode.English:
                        tb.Inlines.Add(UiRun(seg.En, size, bold));
                        break;
                    default:
                        if (seg.Zh == seg.En)
                        {
                            tb.Inlines.Add(UiRun(seg.Zh, size, bold));
                        }
                        else
                        {
                            tb.Inlines.Add(UiRun(seg.Zh, size, bold));
                            tb.Inlines.Add(new Run(" / ") { FontSize = size * 0.9, Foreground = Sub });
                            tb.Inlines.Add(new Run(seg.En)
                            {
                                FontSize = size * 0.9,
                                Foreground = Sub,
                                FontFamily = UiFont
                            });
                        }
                        break;
                }
            }
            return tb;
        }

        static Run UiRun(string t, double size, bool bold) => new Run(t)
        {
            FontFamily = UiFont,
            FontSize = size,
            FontWeight = bold ? FontWeights.SemiBold : FontWeights.Normal,
            Foreground = new SolidColorBrush(Color.FromRgb(0x1B, 0x3F, 0x66))
        };

        // ── 块 ──────────────────────────────────────────────────────────────
        public static FrameworkElement Make(Block b, UiMode mode)
        {
            switch (b)
            {
                case Para p:
                    return Inline(p.Text, 14.2 * S, Ink, mode);

                case Bullets bul:
                    return MakeBullets(bul, mode);

                case Tip tip:
                    return MakeTip(tip, mode);

                case CodeBlock code:
                    return MakeCode(code);

                case Table tbl:
                    return MakeTable(tbl, mode);

                case Picture pic:
                    return MakePicture(pic);

                case Links lk:
                    return MakeLinks(lk);

                case Head hd:
                    return MakeHead(hd, mode);

                default:
                    return new TextBlock();
            }
        }

        static FrameworkElement MakeHead(Head hd, UiMode mode)        {
            var sp = new StackPanel { Margin = new Thickness(0, 14, 0, 6) };
            sp.Children.Add(new Border
            {
                Height = 2, Width = 26, Background = Accent2,
                HorizontalAlignment = HorizontalAlignment.Left,
                CornerRadius = new CornerRadius(1),
                Margin = new Thickness(0, 0, 0, 6)
            });
            var tb = Inline(hd.Text, 15 * S, Accent, mode, true);
            sp.Children.Add(tb);
            return sp;
        }

        static FrameworkElement MakeLinks(Links lk)        {
            var sp = new StackPanel { Margin = new Thickness(0, 6, 0, 10) };
            var wrap = new WrapPanel();
            foreach (var (label, url, note) in lk.Items)
            {
                var btn = new Button
                {
                    Style = (Style)Application.Current.Resources["GhostButton"],
                    Margin = new Thickness(0, 0, 8, 8),
                    Cursor = Cursors.Hand,
                    ToolTip = url + (string.IsNullOrEmpty(note) ? "" : "\n" + note)
                };
                var inner = new StackPanel { Orientation = Orientation.Horizontal };
                inner.Children.Add(new TextBlock
                {
                    Text = "↗ ", FontSize = 13 * S, Foreground = Accent2,
                    FontFamily = UiFont, VerticalAlignment = VerticalAlignment.Center
                });
                inner.Children.Add(new TextBlock
                {
                    Text = label, FontSize = 12.8 * S, FontFamily = UiFont,
                    Foreground = Accent, VerticalAlignment = VerticalAlignment.Center
                });
                btn.Content = inner;
                btn.Click += (s, e) => OpenUrl(url);
                var frame = new Border
                {
                    Background = (Brush)Application.Current.Resources["ChipBg"],
                    BorderBrush = (Brush)Application.Current.Resources["ChipLine"],
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(8),
                    Margin = new Thickness(0, 0, 8, 8),
                    Child = btn
                };
                wrap.Children.Add(frame);
            }
            sp.Children.Add(wrap);
            return sp;
        }

        public static void OpenUrl(string url)
        {
            try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true }); }
            catch { }
        }


        static FrameworkElement MakeBullets(Bullets bul, UiMode mode)
        {
            var sp = new StackPanel();
            int i = 0;
            foreach (var raw in bul.Items)
            {
                i++;
                var row = new Grid { Margin = new Thickness(0, 0, 0, 7) };
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(bul.Numbered ? 24 : 16) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                FrameworkElement mark;
                if (bul.Numbered)
                {
                    mark = new Border
                    {
                        Width = 19, Height = 19, CornerRadius = new CornerRadius(10),
                        Background = (Brush)Application.Current.Resources["AccentSoft"],
                        VerticalAlignment = VerticalAlignment.Top,
                        Margin = new Thickness(0, 2, 0, 0),
                        Child = new TextBlock
                        {
                            Text = i.ToString(), FontSize = 10.5 * S, Foreground = Accent,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center,
                            FontFamily = UiFont, FontWeight = FontWeights.SemiBold
                        }
                    };
                }
                else
                {
                    mark = new TextBlock
                    {
                        Text = "•", FontSize = 15 * S, Foreground = Accent2,
                        Margin = new Thickness(2, 0, 0, 0), VerticalAlignment = VerticalAlignment.Top,
                        FontFamily = UiFont
                    };
                }
                Grid.SetColumn(mark, 0);
                row.Children.Add(mark);

                var tb = Inline(raw, 14 * S, Ink, mode);
                Grid.SetColumn(tb, 1);
                row.Children.Add(tb);

                sp.Children.Add(row);
            }
            sp.Margin = new Thickness(0, 2, 0, 4);
            return sp;
        }

        static FrameworkElement MakeTip(Tip tip, UiMode mode)
        {
            string kind = tip.Kind;
            Brush bar, soft;
            switch (kind)
            {
                case "警告":
                    bar = (Brush)Application.Current.Resources["Warn"];
                    soft = (Brush)Application.Current.Resources["WarnSoft"]; break;
                case "经验":
                    bar = (Brush)Application.Current.Resources["Ok"];
                    soft = (Brush)Application.Current.Resources["OkSoft"]; break;
                case "注意":
                    bar = (Brush)Application.Current.Resources["Warn"];
                    soft = (Brush)Application.Current.Resources["WarnSoft"]; break;
                default:
                    bar = Accent2;
                    soft = (Brush)Application.Current.Resources["AccentSoft"]; break;
            }

            // 重点模式：提示条右上角加一个「重点」角标，说明这条是作者要你记住的
            var head = new DockPanel { Margin = new Thickness(0, 0, 0, 4), LastChildFill = false };
            head.Children.Add(new TextBlock
            {
                Text = kind, FontFamily = UiFont, FontSize = 11 * S,
                FontWeight = FontWeights.SemiBold, Foreground = bar,
                VerticalAlignment = VerticalAlignment.Center
            });
            if (State.HotMode)
            {
                var tag = new Border
                {
                    Background = HotBg, CornerRadius = new CornerRadius(3),
                    Padding = new Thickness(5, 1, 5, 1),
                    Margin = new Thickness(8, 0, 0, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                    Child = new TextBlock
                    {
                        Text = "重点", FontFamily = UiFont, FontSize = 10.5 * S,
                        FontWeight = FontWeights.SemiBold, Foreground = HotFg
                    }
                };
                DockPanel.SetDock(tag, Dock.Left);
                head.Children.Add(tag);
                HotCount++;
            }

            var inner = new StackPanel();
            inner.Children.Add(head);
            // 提示条本身就是作者挑出来的重点：重点模式下整条标注
            string tipText = tip.Text;
            if (State.HotMode && tipText.IndexOf("==", StringComparison.Ordinal) < 0)
                tipText = "==" + tip.Text + "==";
            inner.Children.Add(Inline(tipText, 13.6 * S, Ink, mode));

            return new Border
            {
                Background = soft,
                BorderBrush = bar,
                BorderThickness = new Thickness(3, 0, 0, 0),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(12, 9, 12, 9),
                Margin = new Thickness(0, 8, 0, 10),
                Child = inner
            };
        }

        static FrameworkElement MakeCode(CodeBlock code)
        {
            var sp = new StackPanel();
            sp.Children.Add(new TextBlock
            {
                Text = code.Text.Replace("\n", Environment.NewLine),
                FontFamily = MonoFont,
                FontSize = 12.6 * S,
                Foreground = new SolidColorBrush(Color.FromRgb(0x21, 0x2C, 0x3A)),
                TextWrapping = TextWrapping.Wrap,
                LineHeight = Math.Round(20 * S, 1),
                LineStackingStrategy = LineStackingStrategy.BlockLineHeight
            });
            if (!string.IsNullOrEmpty(code.Note))
            {
                sp.Children.Add(new TextBlock
                {
                    Text = code.Note,
                    FontFamily = UiFont,
                    FontSize = 11.5 * S,
                    Foreground = Sub,
                    Margin = new Thickness(0, 6, 0, 0),
                    TextWrapping = TextWrapping.Wrap
                });
            }
            return new Border
            {
                Background = (Brush)Application.Current.Resources["CodeBg"],
                BorderBrush = Line,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12, 10, 12, 10),
                Margin = new Thickness(0, 8, 0, 10),
                Child = sp
            };
        }

        static FrameworkElement MakeTable(Table t, UiMode mode)
        {
            int cols = t.Head.Length;
            var grid = new Grid();
            for (int c = 0; c < cols; c++)
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var border = new Border
            {
                BorderBrush = Line,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Margin = new Thickness(0, 8, 0, 12),
                ClipToBounds = true
            };

            var rows = new StackPanel();

            // 表头
            var headRow = new Grid { Background = (Brush)Application.Current.Resources["AccentSoft"] };
            for (int c = 0; c < cols; c++) headRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            for (int c = 0; c < cols; c++)
            {
                var cell = new Border { Padding = new Thickness(10, 8, 10, 8) };
                var tb = Inline(t.Head[c], 12.6 * S, Accent, mode, true);
                cell.Child = tb;
                Grid.SetColumn(cell, c);
                headRow.Children.Add(cell);
            }
            rows.Children.Add(headRow);

            int r = 0;
            foreach (var data in t.Rows)
            {
                r++;
                var row = new Grid
                {
                    Background = r % 2 == 0 ? new SolidColorBrush(Color.FromRgb(0xFA, 0xFC, 0xFE)) : Brushes.White
                };
                for (int c = 0; c < cols; c++) row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                for (int c = 0; c < cols; c++)
                {
                    string val = c < data.Length ? data[c] : "";
                    var cell = new Border
                    {
                        Padding = new Thickness(10, 7, 10, 7),
                        BorderBrush = new SolidColorBrush(Color.FromRgb(0xEC, 0xF1, 0xF7)),
                        BorderThickness = new Thickness(0, 1, 0, 0)
                    };
                    bool first = c == 0;
                    cell.Child = Inline(val, 12.4 * S, first ? Ink : new SolidColorBrush(Color.FromRgb(0x33, 0x42, 0x52)), mode, first);
                    Grid.SetColumn(cell, c);
                    row.Children.Add(cell);
                }
                rows.Children.Add(row);
            }

            border.Child = rows;
            return border;
        }

        static FrameworkElement MakePicture(Picture pic)
        {
            var sp = new StackPanel { Margin = new Thickness(0, 8, 0, 12) };

            var img = new Image
            {
                Source = LoadImage(pic.Key),
                Stretch = Stretch.Uniform,
                HorizontalAlignment = HorizontalAlignment.Left,
                Cursor = Cursors.Hand,
                MaxHeight = 420
            };
            var frame = new Border
            {
                Background = Brushes.White,
                BorderBrush = Line,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(8),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Child = img
            };
            frame.MouseLeftButtonUp += (s, e) => OnImageClick?.Invoke(pic.Key);

            sp.Children.Add(frame);
            if (!string.IsNullOrEmpty(pic.Caption))
            {
                sp.Children.Add(new TextBlock
                {
                    Text = "图：" + pic.Caption + "（点击可放大）",
                    FontFamily = UiFont,
                    FontSize = 11.5 * S,
                    Foreground = Sub,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(2, 6, 0, 0)
                });
            }
            return sp;
        }

        public static BitmapImage LoadImage(string key)
        {
            try
            {
                var img = new BitmapImage();
                img.BeginInit();
                img.UriSource = new Uri($"pack://application:,,,/Assets/{key}.png", UriKind.Absolute);
                img.CacheOption = BitmapCacheOption.OnLoad;
                img.EndInit();
                img.Freeze();
                return img;
            }
            catch { return null; }
        }

        // ── 卡片 ────────────────────────────────────────────────────────────
        public static FrameworkElement Card(Item it, int index, UiMode mode, bool compact = false)
        {
            var sp = new StackPanel();

            // 标题行
            var head = new Grid { Margin = new Thickness(0, 0, 0, 10) };
            head.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            head.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var chips = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 2, 10, 0)
            };

            var chip = new Border
            {
                Background = TagBg(it.Tag),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(7, 2, 7, 2),
                Margin = new Thickness(0, 0, it.Flow == Way.Both ? 0 : 5, 0),
                VerticalAlignment = VerticalAlignment.Top,
                Child = new TextBlock
                {
                    Text = it.TagName, FontFamily = UiFont, FontSize = 11 * S,
                    Foreground = TagFg(it.Tag), FontWeight = FontWeights.SemiBold
                }
            };
            chips.Children.Add(chip);

            // 路线标签：MA 插件 / 古法（通用条目不显示）
            if (it.Flow != Way.Both)
            {
                bool ma = it.Flow == Way.MA;
                chips.Children.Add(new Border
                {
                    Background = new SolidColorBrush(ma ? Color.FromRgb(0xE8, 0xF0, 0xFA) : Color.FromRgb(0xFB, 0xF1, 0xE6)),
                    BorderBrush = new SolidColorBrush(ma ? Color.FromRgb(0xC3, 0xD6, 0xEC) : Color.FromRgb(0xEC, 0xCF, 0xAF)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(6, 1, 6, 1),
                    VerticalAlignment = VerticalAlignment.Top,
                    Child = new TextBlock
                    {
                        Text = it.FlowName,
                        FontFamily = UiFont,
                        FontSize = 10.5 * S,
                        Foreground = new SolidColorBrush(ma ? Color.FromRgb(0x1F, 0x4E, 0x79) : Color.FromRgb(0x8A, 0x4A, 0x14)),
                        FontWeight = FontWeights.SemiBold
                    }
                });
            }

            Grid.SetColumn(chips, 0);
            head.Children.Add(chips);

            var title = new TextBlock
            {
                Text = index > 0 ? $"{index}. {it.Title}" : it.Title,
                FontFamily = UiFont,
                FontSize = 16.5 * S,
                FontWeight = FontWeights.SemiBold,
                Foreground = Ink,
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(title, 1);
            head.Children.Add(title);
            sp.Children.Add(head);

            foreach (var b in it.Blocks)
            {
                var el = Make(b, mode);
                if (el != null) sp.Children.Add(el);
            }

            var card = new Border
            {
                Background = (Brush)Application.Current.Resources["Surface"],
                BorderBrush = Line,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(compact ? 16 : 20, compact ? 14 : 18, compact ? 16 : 20, compact ? 14 : 18),
                Margin = new Thickness(0, 0, 0, 14),
                Child = sp
            };

            // 左侧类型色条
            var wrap = new Grid();
            wrap.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(4) });
            wrap.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            var bar = new Border
            {
                Background = TagFg(it.Tag),
                CornerRadius = new CornerRadius(2),
                Margin = new Thickness(0, 6, 0, 6),
                Opacity = 0.55
            };
            Grid.SetColumn(bar, 0);
            Grid.SetColumn(card, 1);
            wrap.Children.Add(bar);
            wrap.Children.Add(card);
            wrap.Margin = new Thickness(0, 0, 0, 14);
            card.Margin = new Thickness(10, 0, 0, 0);
            return wrap;
        }

        static Brush TagBg(TagKind t) => t switch
        {
            TagKind.Basic => new SolidColorBrush(Color.FromRgb(0xE9, 0xF2, 0xFB)),
            TagKind.Main => new SolidColorBrush(Color.FromRgb(0xE7, 0xF4, 0xEC)),
            TagKind.Advanced => new SolidColorBrush(Color.FromRgb(0xF6, 0xEE, 0xF9)),
            _ => new SolidColorBrush(Color.FromRgb(0xFD, 0xF1, 0xE4))
        };

        static Brush TagFg(TagKind t) => t switch
        {
            TagKind.Basic => new SolidColorBrush(Color.FromRgb(0x1F, 0x4E, 0x79)),
            TagKind.Main => new SolidColorBrush(Color.FromRgb(0x2F, 0x7D, 0x53)),
            TagKind.Advanced => new SolidColorBrush(Color.FromRgb(0x6B, 0x3E, 0x86)),
            _ => new SolidColorBrush(Color.FromRgb(0x9A, 0x5A, 0x1E))
        };

        // ── 章节头 ──────────────────────────────────────────────────────────
        public static FrameworkElement ChapterHead(string badge, string title, string subtitle, string hint)
        {
            var sp = new StackPanel { Margin = new Thickness(0, 0, 0, 6) };

            var top = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 6) };
            top.Children.Add(new Border
            {
                Background = Accent,
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(9, 3, 9, 3),
                Margin = new Thickness(0, 0, 10, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Child = new TextBlock
                {
                    Text = badge, FontFamily = UiFont, FontSize = 12.5 * S,
                    Foreground = Brushes.White, FontWeight = FontWeights.Bold
                }
            });
            top.Children.Add(new TextBlock
            {
                Text = title, FontFamily = UiFont, FontSize = 25 * S,
                FontWeight = FontWeights.Bold, Foreground = Ink,
                VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.Wrap
            });
            sp.Children.Add(top);

            if (!string.IsNullOrEmpty(subtitle))
                sp.Children.Add(new TextBlock
                {
                    Text = subtitle, FontFamily = UiFont, FontSize = 13.5 * S,
                    Foreground = Sub, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(2, 0, 0, 4)
                });

            if (!string.IsNullOrEmpty(hint))
                sp.Children.Add(new TextBlock
                {
                    Text = hint, FontFamily = UiFont, FontSize = 12.5 * S,
                    Foreground = new SolidColorBrush(Color.FromRgb(0x8A, 0x9A, 0xAC)),
                    TextWrapping = TextWrapping.Wrap, Margin = new Thickness(2, 0, 0, 14)
                });

            sp.Children.Add(new Border
            {
                Height = 1, Background = Line, Margin = new Thickness(0, 4, 0, 16)
            });
            return sp;
        }
    }
}
