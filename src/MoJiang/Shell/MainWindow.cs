using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Web.WebView2.Wpf;
using Tutorial = VRChatGuide.Content;

namespace VRChatGuide
{
    public sealed class MainWindow : Window
    {
        // ── 主题 ────────────────────────────────────────────────────────────
        static Brush R_(string k) => (Brush)Application.Current.Resources[k];
        static readonly FontFamily Font = new FontFamily("Microsoft YaHei UI, Microsoft YaHei, Segoe UI");
        Brush Bg => R_("Bg");
        Brush Surface => R_("Surface");
        Brush Ink => R_("Ink");
        Brush Sub => R_("Sub");
        Brush LineC => R_("Line");
        Brush Accent => R_("Accent");
        Brush Accent2 => R_("Accent2");
        Brush AccentSoft => R_("AccentSoft");

        // ── 导航条目 ────────────────────────────────────────────────────────
        enum Kind { Category, Checklist, Gallery, Web }

        sealed class Nav
        {
            public Kind Kind;
            public int Index;
            public string Badge = "";
            public string Title = "";
            public string Sub = "";
        }

        readonly List<Nav> _navs = new List<Nav>();
        readonly List<NavBtn> _navButtons = new List<NavBtn>();

        StackPanel _navPanel;
        ContentControl _host;
        RadioButton _rbBoth, _rbZh, _rbEn, _rbMa, _rbCl;
        Button _hotBtn;
        TextBox _searchBox;
        TextBlock _searchHint;
        TextBlock _statusLeft, _statusRight;
        WebPage _webPage;

        int _current = 0;

        public MainWindow()
        {
            Title = "模匠 · VRChat 改模指南";
            Width = State.Data.WinW;
            Height = State.Data.WinH;
            MinWidth = 1080;
            MinHeight = 700;
            Background = Bg;
            FontFamily = new FontFamily("Microsoft YaHei UI, Microsoft YaHei, Segoe UI");
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            try { Icon = BitmapFrame.Create(new Uri("pack://application:,,,/Assets/app.ico")); } catch { }

            Render.OnImageClick = ShowLightbox;

            BuildNavs();
            Content = BuildShell();
            Navigate(Math.Clamp(State.Data.LastNav, 0, _navs.Count - 1));

            if (State.Data.WinMax) WindowState = WindowState.Maximized;
            Closing += (s, e) =>
            {
                if (WindowState == WindowState.Normal)
                {
                    State.Data.WinW = Width;
                    State.Data.WinH = Height;
                }
                State.Data.WinMax = WindowState == WindowState.Maximized;
                State.Save();
            };
        }

        void BuildNavs()
        {
            _navs.Clear();
            for (int i = 0; i < Tutorial.All.Length; i++)
            {
                var c = Tutorial.All[i];
                _navs.Add(new Nav
                {
                    Kind = Kind.Category,
                    Index = i,
                    Badge = c.Badge,
                    Title = c.Title,
                    Sub = c.Items.Length + " 条"
                });
            }
            _navs.Add(new Nav { Kind = Kind.Gallery, Badge = "图", Title = "图例", Sub = Tutorial.Gallery.Length + " 张" });
            _navs.Add(new Nav { Kind = Kind.Checklist, Badge = "✓", Title = "检查清单", Sub = "可勾选" });
            _navs.Add(new Nav { Kind = Kind.Web, Badge = "V", Title = "VCC 镜像", Sub = "内嵌网页" });
        }

        Visual BuildShell()
        {
            var root = new Grid();
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(64) });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(28) });

            root.Children.Add(BuildHeader());
            root.Children.Add(BuildBody());
            var status = BuildStatus();
            Grid.SetRow(status, 2);
            root.Children.Add(status);
            return root;
        }

        // ── 顶栏 ────────────────────────────────────────────────────────────
        FrameworkElement BuildHeader()
        {
            var bar = new Border
            {
                Background = Surface,
                BorderBrush = LineC,
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(18, 0, 18, 0)
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // 左：图标 + 标题
            var left = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            left.Children.Add(new Border
            {
                Width = 40, Height = 40, CornerRadius = new CornerRadius(11),
                Background = Accent, Margin = new Thickness(0, 0, 12, 0),
                Child = new TextBlock
                {
                    Text = "改", FontSize = 20, FontWeight = FontWeights.Bold,
                    Foreground = Brushes.White, FontFamily = Font,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            });
            var titles = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            titles.Children.Add(new TextBlock
            {
                Text = "模匠", FontSize = 19, FontWeight = FontWeights.Bold,
                Foreground = Ink, FontFamily = Font
            });
            titles.Children.Add(new TextBlock
            {
                Text = $"VRChat 改模指南 · {Tutorial.All.Length} 章 {Tutorial.ItemCount} 条 · MA/古法 · 重点标注 · v1.6.1",
                FontSize = 11.5, Foreground = Sub, FontFamily = Font, Margin = new Thickness(0, 1, 0, 0)
            });
            left.Children.Add(titles);
            Grid.SetColumn(left, 0);
            grid.Children.Add(left);

            // 右：Unity 界面切换 + 字号 + 关于
            var right = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center
            };
            right.Children.Add(new TextBlock
            {
                Text = "Unity 界面", FontSize = 11.5, Foreground = Sub, FontFamily = Font,
                VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0)
            });

            var segWrap = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(0xF2, 0xF5, 0xFA)),
                BorderBrush = LineC, BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(9), Padding = new Thickness(3)
            };
            var seg = new StackPanel { Orientation = Orientation.Horizontal };
            var rbBoth = _rbBoth = MakeSeg("双语", UiMode.Both);
            var rbZh = _rbZh = MakeSeg("中文界面", UiMode.Chinese);
            var rbEn = _rbEn = MakeSeg("English UI", UiMode.English);
            seg.Children.Add(rbBoth);
            seg.Children.Add(rbZh);
            seg.Children.Add(rbEn);
            segWrap.Child = seg;

            switch (State.Mode)
            {
                case UiMode.Chinese: rbZh.IsChecked = true; break;
                case UiMode.English: rbEn.IsChecked = true; break;
                default: rbBoth.IsChecked = true; break;
            }
            right.Children.Add(segWrap);

            // 改模方式：MA 插件 / 古法手动
            right.Children.Add(new Border
            {
                Width = 1, Height = 22, Background = LineC,
                Margin = new Thickness(12, 0, 12, 0)
            });
            right.Children.Add(new TextBlock
            {
                Text = "改模方式", FontSize = 11.5, Foreground = Sub, FontFamily = Font,
                VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0)
            });

            var wayWrap = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(0xF2, 0xF5, 0xFA)),
                BorderBrush = LineC, BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(9), Padding = new Thickness(3)
            };
            var waySeg = new StackPanel { Orientation = Orientation.Horizontal };
            var rbMa = _rbMa = MakeWaySeg("MA 插件", Way.MA);
            var rbCl = _rbCl = MakeWaySeg("古法", Way.Classic);
            waySeg.Children.Add(rbMa);
            waySeg.Children.Add(rbCl);
            wayWrap.Child = waySeg;
            if (State.Workflow == Way.Classic) rbCl.IsChecked = true; else rbMa.IsChecked = true;
            right.Children.Add(wayWrap);

            right.Children.Add(new Border
            {
                Width = 1, Height = 22, Background = LineC,
                Margin = new Thickness(12, 0, 12, 0)
            });

            _hotBtn = new Button
            {
                Style = (Style)Application.Current.Resources["ChipToggle"],
                Content = "重点",
                ToolTip = "重点模式：把正文里必须注意、容易翻车的句子高亮出来（再点一次关闭）",
                VerticalAlignment = VerticalAlignment.Center
            };
            _hotBtn.Click += (s2, e2) => ApplyHot(!State.HotMode);
            right.Children.Add(_hotBtn);
            UpdateHotBtn();

            right.Children.Add(new Border
            {
                Width = 1, Height = 22, Background = LineC,
                Margin = new Thickness(12, 0, 12, 0)
            });

            var fontBox = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            fontBox.Children.Add(SponsorChip());
            fontBox.Children.Add(GhostBtn("A-", () => ChangeFont(-0.06), "缩小正文字号"));
            fontBox.Children.Add(GhostBtn("A+", () => ChangeFont(0.06), "放大正文字号"));
            fontBox.Children.Add(GhostBtn("?", About, "关于本程序"));
            right.Children.Add(fontBox);

            Grid.SetColumn(right, 1);
            grid.Children.Add(right);

            bar.Child = grid;
            return bar;
        }

        /// <summary>顶部「♥ 赞助作者」粉色小胶囊，点了跳到第 16 章</summary>
        FrameworkElement SponsorChip()
        {
            var chip = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(0xFF, 0xF1, 0xF4)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0xFF, 0xC7, 0xD6)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(9),
                Padding = new Thickness(11, 5, 12, 5),
                Cursor = Cursors.Hand,
                Margin = new Thickness(0, 0, 10, 0),
                VerticalAlignment = VerticalAlignment.Center,
                ToolTip = "支持「模匠」：打开第 16 章，里面是爱发电赞助入口",
                Child = new TextBlock
                {
                    Text = "♥ 赞助作者",
                    FontSize = 12.5,
                    FontFamily = Font,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(Color.FromRgb(0xC2, 0x2E, 0x5A))
                }
            };
            var hoverBg = new SolidColorBrush(Color.FromRgb(0xFF, 0xE3, 0xEA));
            var idleBg = chip.Background;
            chip.MouseEnter += (s, e) => chip.Background = hoverBg;
            chip.MouseLeave += (s, e) => chip.Background = idleBg;
            chip.MouseLeftButtonUp += (s, e) => OpenSponsor();
            return chip;
        }

        /// <summary>跳到赞助章节；找不到就直接开浏览器兜底</summary>
        void OpenSponsor()
        {
            for (int i = 0; i < _navs.Count; i++)
            {
                var n = _navs[i];
                if (n.Kind == Kind.Category && Tutorial.All[n.Index].Badge == "16") { Navigate(i); return; }
            }
            Render.OpenUrl(global::VRChatGuide.Content.SponsorUrl);
        }

        RadioButton MakeSeg(string text, UiMode mode)
        {
            var rb = new RadioButton
            {
                Style = (Style)Application.Current.Resources["SegButton"],
                Content = text,
                GroupName = "uimode",
                Margin = new Thickness(0, 0, 2, 0)
            };
            rb.Checked += (s, e) =>
            {
                if (State.Mode == mode) return;
                State.Mode = mode;
                State.Save();
                RefreshAll();
            };
            return rb;
        }

        RadioButton MakeWaySeg(string text, Way way)
        {
            var rb = new RadioButton
            {
                Style = (Style)Application.Current.Resources["SegButton"],
                Content = text,
                GroupName = "waymode",
                Margin = new Thickness(0, 0, 2, 0),
                ToolTip = way == Way.MA
                    ? "显示使用 Modular Avatar / VRCFury 插件的改模流程"
                    : "显示不依赖插件、全手动（Unity 原生组件）的改模流程"
            };
            rb.Checked += (s, e) =>
            {
                if (State.Workflow == way) return;
                State.Workflow = way;
                State.Save();
                RefreshAll();
            };
            return rb;
        }

        Button GhostBtn(string text, Action onClick, string tip = null)
        {
            var b = new Button
            {
                Style = (Style)Application.Current.Resources["GhostButton"],
                Content = text,
                Margin = new Thickness(2, 0, 2, 0),
                ToolTip = tip
            };
            b.Click += (s, e) => onClick();
            return b;
        }

        void UpdateHotBtn()
        {
            if (_hotBtn == null) return;
            bool on = State.HotMode;
            _hotBtn.Content = on ? "重点 · 开" : "重点";
            _hotBtn.Background = new SolidColorBrush(on
                ? Color.FromRgb(0xFF, 0xEE, 0xA8)
                : Color.FromRgb(0xF2, 0xF5, 0xFA));
            _hotBtn.BorderBrush = new SolidColorBrush(on
                ? Color.FromRgb(0xE3, 0xBE, 0x4A)
                : Color.FromRgb(0xDF, 0xE6, 0xEF));
            _hotBtn.Foreground = new SolidColorBrush(on
                ? Color.FromRgb(0x4E, 0x38, 0x00)
                : Color.FromRgb(0x5A, 0x6A, 0x7D));
        }

        public void ApplyHot(bool on)
        {
            State.HotMode = on;
            State.Save();
            UpdateHotBtn();
            RefreshContentOnly();
        }

        void ChangeFont(double delta)
        {
            var s = Math.Round(Math.Clamp(State.Data.FontScale + delta, 0.86, 1.34), 2);
            if (Math.Abs(s - State.Data.FontScale) < 0.001) return;
            State.Data.FontScale = s;
            State.Save();
            RefreshAll();
        }

        // ── 主体 ────────────────────────────────────────────────────────────
        FrameworkElement BuildBody()
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(296) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var side = new Border
            {
                Background = Surface,
                BorderBrush = LineC,
                BorderThickness = new Thickness(0, 0, 1, 0)
            };
            var sg = new Grid();
            sg.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            sg.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            // 搜索框
            var searchWrap = new Grid { Margin = new Thickness(14, 14, 14, 8) };
            _searchBox = new TextBox
            {
                FontFamily = Font, FontSize = 12.5, Padding = new Thickness(10, 7, 10, 7),
                BorderBrush = LineC, BorderThickness = new Thickness(1),
                Background = new SolidColorBrush(Color.FromRgb(0xF8, 0xFA, 0xFD)),
                Foreground = Ink, VerticalContentAlignment = VerticalAlignment.Center
            };
            _searchBox.TextChanged += (s, e) =>
            {
                _searchHint.Visibility = string.IsNullOrEmpty(_searchBox.Text)
                    ? Visibility.Visible : Visibility.Collapsed;
                RefreshContentOnly();
            };
            _searchHint = new TextBlock
            {
                Text = "搜索：PhysBone / 材质 / 报错…",
                FontSize = 12, Foreground = new SolidColorBrush(Color.FromRgb(0xA6, 0xB3, 0xC2)),
                Margin = new Thickness(12, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
                IsHitTestVisible = false
            };
            searchWrap.Children.Add(_searchBox);
            searchWrap.Children.Add(_searchHint);
            Grid.SetRow(searchWrap, 0);
            sg.Children.Add(searchWrap);

            _navPanel = new StackPanel { Margin = new Thickness(10, 2, 10, 16) };
            var navScroll = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Content = _navPanel
            };
            Grid.SetRow(navScroll, 1);
            sg.Children.Add(navScroll);

            side.Child = sg;
            Grid.SetColumn(side, 0);
            grid.Children.Add(side);

            _host = new ContentControl { HorizontalContentAlignment = HorizontalAlignment.Stretch };
            Grid.SetColumn(_host, 1);
            grid.Children.Add(_host);

            var wrap = new Grid();
            wrap.Children.Add(grid);
            Grid.SetRow(wrap, 1);
            return wrap;
        }

        FrameworkElement BuildStatus()
        {
            var g = new Grid
            {
                Background = new SolidColorBrush(Color.FromRgb(0xEE, 0xF2, 0xF7))
            };
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            _statusLeft = new TextBlock
            {
                FontFamily = Font, FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(0x6B, 0x7B, 0x8D)),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(18, 0, 0, 0)
            };
            _statusRight = new TextBlock
            {
                FontFamily = Font, FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(0x8A, 0x9A, 0xAC)),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 18, 0)
            };
            Grid.SetColumn(_statusRight, 1);
            g.Children.Add(_statusLeft);
            g.Children.Add(_statusRight);
            return g;
        }

        // ── 导航列表 ────────────────────────────────────────────────────────
        sealed class NavBtn
        {
            public Border Root;
            public TextBlock Title;
            public TextBlock Badge;
            public Border BadgeBox;
            public int Index;
            public bool Selected;
        }

        void BuildNavList()
        {
            _navPanel.Children.Clear();
            _navButtons.Clear();

            string lastGroup = null;
            for (int i = 0; i < _navs.Count; i++)
            {
                var n = _navs[i];
                string group =
                    n.Kind == Kind.Category ? Tutorial.GroupOf(Tutorial.All[n.Index], i)
                    : "工具";
                if (group != lastGroup)
                {
                    lastGroup = group;
                    _navPanel.Children.Add(new TextBlock
                    {
                        Text = group,
                        FontSize = 10.5,
                        Foreground = new SolidColorBrush(Color.FromRgb(0x9A, 0xA8, 0xB8)),
                        Margin = new Thickness(12, i == 0 ? 2 : 14, 0, 6),
                        FontFamily = Font
                    });
                }
                _navPanel.Children.Add(MakeNavButton(n, i));
            }
        }

        FrameworkElement MakeNavButton(Nav n, int index)
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(30) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var badgeBox = new Border
            {
                Width = 22, Height = 22, CornerRadius = new CornerRadius(6),
                Background = new SolidColorBrush(Color.FromRgb(0xEE, 0xF3, 0xFA)),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Left
            };
            var badge = new TextBlock
            {
                Text = n.Badge, FontSize = 10.5,
                Foreground = Accent,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                FontFamily = Font
            };
            badgeBox.Child = badge;
            Grid.SetColumn(badgeBox, 0);
            grid.Children.Add(badgeBox);

            var mid = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            var title = new TextBlock
            {
                Text = n.Title, FontSize = 13, Foreground = Ink, FontFamily = Font,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            var sub = new TextBlock
            {
                Text = n.Kind == Kind.Category
                    ? Tutorial.All[n.Index].CountFor(State.Workflow) + " 条"
                    : n.Sub,
                FontSize = 10.5, Foreground = Sub, FontFamily = Font,
                Margin = new Thickness(0, 1, 0, 0)
            };
            mid.Children.Add(title);
            mid.Children.Add(sub);
            Grid.SetColumn(mid, 1);
            grid.Children.Add(mid);

            var root = new Border
            {
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(9, 6, 9, 6),
                Margin = new Thickness(0, 1, 0, 1),
                Background = Brushes.Transparent,
                Cursor = Cursors.Hand,
                Child = grid
            };

            var btn = new NavBtn
            {
                Root = root, Title = title, Badge = badge, BadgeBox = badgeBox, Index = index
            };
            root.MouseEnter += (s, e) => { if (!btn.Selected) root.Background = new SolidColorBrush(Color.FromRgb(0xF1, 0xF5, 0xFA)); };
            root.MouseLeave += (s, e) => { if (!btn.Selected) root.Background = Brushes.Transparent; };
            root.MouseLeftButtonUp += (s, e) =>
            {
                _searchBox.Text = "";
                Navigate(index);
            };

            _navButtons.Add(btn);
            return root;
        }

        void SetNavSelection(int index)
        {
            foreach (var b in _navButtons)
            {
                b.Selected = b.Index == index;
                b.Root.Background = b.Selected ? AccentSoft : Brushes.Transparent;
                b.Title.Foreground = b.Selected ? Accent : Ink;
                b.Title.FontWeight = b.Selected ? FontWeights.SemiBold : FontWeights.Normal;
                b.BadgeBox.Background = b.Selected
                    ? Accent
                    : new SolidColorBrush(Color.FromRgb(0xEE, 0xF3, 0xFA));
                b.Badge.Foreground = b.Selected ? Brushes.White : Accent;
            }
        }

        // ── 页面切换 ────────────────────────────────────────────────────────
        void Navigate(int index)
        {
            if (index < 0 || index >= _navs.Count) return;
            _current = index;
            State.Data.LastNav = index;
            State.Save();
            if (_navButtons.Count != _navs.Count) BuildNavList();
            SetNavSelection(index);
            RefreshContentOnly();
        }

        /// <summary>重建正文（不重建导航）</summary>
        void RefreshContentOnly()
        {
            if (_host == null) return;
            Render.HotCount = 0;
            string q = _searchBox?.Text?.Trim() ?? "";

            if (q.Length > 0)
            {
                _host.Content = BuildSearchPage(q);
                _statusLeft.Text = $"搜索「{q}」";
                _statusRight.Text = "清空搜索框可返回章节";
                return;
            }

            var n = _navs[_current];
            switch (n.Kind)
            {
                case Kind.Category:
                    _host.Content = BuildCategoryPage(Tutorial.All[n.Index]);
                    _statusLeft.Text = $"{Tutorial.All[n.Index].Badge} · {Tutorial.All[n.Index].Title}　（{Tutorial.All[n.Index].CountFor(State.Workflow)} 条 · {(State.Workflow == Way.MA ? "MA 插件路线" : "古法路线")}）";
                    break;

                case Kind.Gallery:
                    _host.Content = BuildGalleryPage();
                    _statusLeft.Text = $"图例 · {Tutorial.Gallery.Length} 张示意图";
                    break;

                case Kind.Checklist:
                    _host.Content = BuildChecklistPage();
                    _statusLeft.Text = "上传前检查清单（进度自动保存）";
                    break;

                case Kind.Web:
                    _host.Content = GetWebPage();
                    _statusLeft.Text = "VCC 中文镜像 · vcc.vrczh.org（内置浏览器）";
                    break;
            }

            _statusRight.Text = (State.Mode switch
            {
                UiMode.Chinese => "中文界面用词",
                UiMode.English => "English UI 用词",
                _ => "中文 · English 双语对照"
            }) + "　|　" + (State.Workflow == Way.MA ? "MA 插件路线" : "古法路线")
              + (State.HotMode ? $"　|　重点已标注 {Render.HotCount} 处" : "");
        }

        void RefreshAll()
        {
            BuildNavList();
            SetNavSelection(_current);
            RefreshContentOnly();
        }

        // ── 章节页 ──────────────────────────────────────────────────────────
        FrameworkElement BuildCategoryPage(Category cat)
        {
            var stack = new StackPanel { MaxWidth = 1020, HorizontalAlignment = HorizontalAlignment.Left };
            stack.Children.Add(Render.ChapterHead(cat.Badge, cat.Title, cat.Subtitle, cat.Hint));

            var items = cat.For(State.Workflow);
            int i = 0;
            foreach (var item in items)
            {
                i++;
                stack.Children.Add(Render.Card(item, i, State.Mode));
            }

            var footer = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(0xF8, 0xFA, 0xFD)),
                BorderBrush = LineC, BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(14, 10, 14, 10),
                Margin = new Thickness(0, 6, 0, 0),
                Child = new TextBlock
                {
                    Text = "本章共 " + items.Length + " 条（" +
                           (State.Workflow == Way.MA ? "MA 插件路线" : "古法路线") +
                           "）· 顶部「改模方式」可切换成另一条路线的写法 · 遇到问题先看「09 排错手册」。",
                    FontSize = 11.5, Foreground = Sub, FontFamily = Font,
                    TextWrapping = TextWrapping.Wrap
                }
            };
            stack.Children.Add(footer);

            return Scroll(stack);
        }

        FrameworkElement Scroll(UIElement content)
        {
            return new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Padding = new Thickness(26, 22, 26, 40),
                Content = content,
                Background = Bg
            };
        }

        // ── 搜索页 ──────────────────────────────────────────────────────────
        FrameworkElement BuildSearchPage(string q)
        {
            var stack = new StackPanel { MaxWidth = 1020, HorizontalAlignment = HorizontalAlignment.Left };
            stack.Children.Add(Render.ChapterHead("⌕", $"搜索结果：{q}", "",
                $"只搜索当前路线（{(State.Workflow == Way.MA ? "MA 插件" : "古法")}）的教程；切到另一条路线会搜到不同的步骤"));

            int hits = 0;
            foreach (var cat in Tutorial.All)
            {
                var matched = cat.For(State.Workflow).Where(it =>
                    it.Plain().IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0).ToArray();
                if (matched.Length == 0) continue;

                stack.Children.Add(new TextBlock
                {
                    Text = $"{cat.Badge} · {cat.Title}（{matched.Length}）",
                    FontSize = 12.5, Foreground = Accent2, FontFamily = Font,
                    FontWeight = FontWeights.SemiBold, Margin = new Thickness(2, 10, 0, 8)
                });

                int i = 0;
                foreach (var it in matched)
                {
                    i++;
                    hits++;
                    stack.Children.Add(Render.Card(it, 0, State.Mode, true));
                }
            }

            if (hits == 0)
            {
                stack.Children.Add(new Border
                {
                    Background = Surface, BorderBrush = LineC, BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(10), Padding = new Thickness(20),
                    Margin = new Thickness(0, 10, 0, 0),
                    Child = new TextBlock
                    {
                        Text = "没找到相关内容。\n试试：PhysBone、材质、报错、换装、Quest、上传\n（当前只搜「" +
                               (State.Workflow == Way.MA ? "MA 插件" : "古法") + "」路线，另一条路线的写法可以切换后再搜）",
                        FontSize = 13, Foreground = Sub, FontFamily = Font,
                        TextWrapping = TextWrapping.Wrap, LineHeight = 24
                    }
                });
            }
            return Scroll(stack);
        }

        // ── 图例页 ──────────────────────────────────────────────────────────
        FrameworkElement BuildGalleryPage()
        {
            var stack = new StackPanel { MaxWidth = 1020, HorizontalAlignment = HorizontalAlignment.Left };
            stack.Children.Add(Render.ChapterHead("图", "图例",
                "六张示意图对应六个最容易搞混的概念，点击图片可放大查看",
                "图里的文字说明与正文一致，可以直接截图发给朋友"));

            var wrap = new WrapPanel();
            foreach (var (key, title, caption) in Tutorial.Gallery)
            {
                var card = new StackPanel();
                var img = new Image
                {
                    Source = Render.LoadImage(key),
                    Stretch = Stretch.Uniform,
                    Height = 210,
                    Cursor = Cursors.Hand
                };
                var frame = new Border
                {
                    Background = Brushes.White, BorderBrush = LineC,
                    BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(10),
                    Padding = new Thickness(6), Child = img
                };
                frame.MouseLeftButtonUp += (s, e) => ShowLightbox(key);
                card.Children.Add(frame);
                card.Children.Add(new TextBlock
                {
                    Text = title, FontSize = 13, FontWeight = FontWeights.SemiBold,
                    Foreground = Ink, FontFamily = Font, Margin = new Thickness(2, 8, 0, 2)
                });
                card.Children.Add(new TextBlock
                {
                    Text = caption, FontSize = 11, Foreground = Sub, FontFamily = Font,
                    TextWrapping = TextWrapping.Wrap, Margin = new Thickness(2, 0, 0, 0)
                });

                var holder = new Border
                {
                    Width = 470, Margin = new Thickness(0, 0, 16, 18),
                    Background = Surface, BorderBrush = LineC, BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(12), Padding = new Thickness(12),
                    Child = card
                };
                wrap.Children.Add(holder);
            }
            stack.Children.Add(wrap);
            return Scroll(stack);
        }

        void ShowLightbox(string key)
        {
            BitmapImage src = Render.LoadImage(key);
            if (src == null) return;

            var win = new Window
            {
                Title = "图例",
                Width = Math.Min(1400, SystemParameters.WorkArea.Width * 0.9),
                Height = Math.Min(900, SystemParameters.WorkArea.Height * 0.9),
                Background = new SolidColorBrush(Color.FromRgb(0x0E, 0x16, 0x20)),
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                Owner = this,
                ShowInTaskbar = false
            };
            var img = new Image { Source = src, Stretch = Stretch.Uniform, Margin = new Thickness(16) };
            var b = new Border { Child = img, Cursor = Cursors.Hand };
            b.MouseLeftButtonUp += (s, e) => win.Close();
            win.Content = b;
            win.KeyDown += (s, e) => { if (e.Key == Key.Escape) win.Close(); };
            win.ShowDialog();
        }

        // ── 检查清单页 ──────────────────────────────────────────────────────
        FrameworkElement BuildChecklistPage()
        {
            var stack = new StackPanel { MaxWidth = 1020, HorizontalAlignment = HorizontalAlignment.Left };
            stack.Children.Add(Render.ChapterHead("✓", "上传前检查清单",
                "每勾一项都会保存，下次打开还在", "全部勾完再点 Build & Publish"));

            int total = Tutorial.Checklist.Sum(g => g.Items.Length);
            int done = Tutorial.Checklist.Sum(g => g.Items.Count(it => State.IsChecked(g.Group + "|" + it)));

            var bar = new Border
            {
                Background = Surface, BorderBrush = LineC, BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12), Padding = new Thickness(18, 14, 18, 14),
                Margin = new Thickness(0, 0, 0, 16)
            };
            var barSp = new StackPanel();
            var head = new Grid();
            head.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            head.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            head.Children.Add(new TextBlock
            {
                Text = "完成进度", FontSize = 14, FontWeight = FontWeights.SemiBold,
                Foreground = Ink, FontFamily = Font
            });
            var pct = new TextBlock
            {
                Text = $"{done} / {total}　({(total == 0 ? 0 : done * 100 / total)}%)",
                FontSize = 13, Foreground = Accent, FontFamily = Font,
                FontWeight = FontWeights.SemiBold
            };
            Grid.SetColumn(pct, 1);
            head.Children.Add(pct);
            barSp.Children.Add(head);

            var pb = new ProgressBar
            {
                Height = 8, Margin = new Thickness(0, 10, 0, 0),
                Minimum = 0, Maximum = total, Value = done,
                Foreground = Accent2,
                Background = new SolidColorBrush(Color.FromRgb(0xEC, 0xF1, 0xF7)),
                BorderThickness = new Thickness(0)
            };
            barSp.Children.Add(pb);
            bar.Child = barSp;
            stack.Children.Add(bar);

            TextBlock pctRef = pct;
            foreach (var (group, items) in Tutorial.Checklist)
            {
                var sp = new StackPanel();
                sp.Children.Add(new TextBlock
                {
                    Text = group, FontSize = 13.5, FontWeight = FontWeights.SemiBold,
                    Foreground = Accent, FontFamily = Font, Margin = new Thickness(0, 2, 0, 8)
                });

                foreach (var it in items)
                {
                    string key = group + "|" + it;
                    var cb = new CheckBox
                    {
                        Content = it,
                        IsChecked = State.IsChecked(key),
                        FontFamily = Font, FontSize = 13, Foreground = Ink,
                        Margin = new Thickness(0, 0, 0, 9),
                        Cursor = Cursors.Hand,
                        VerticalContentAlignment = VerticalAlignment.Center
                    };
                    cb.Checked += (s, e) => { State.SetChecked(key, true); State.Save(); UpdateProgress(); };
                    cb.Unchecked += (s, e) => { State.SetChecked(key, false); State.Save(); UpdateProgress(); };
                    sp.Children.Add(cb);
                }

                stack.Children.Add(new Border
                {
                    Background = Surface, BorderBrush = LineC, BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(12), Padding = new Thickness(18, 14, 18, 8),
                    Margin = new Thickness(0, 0, 0, 14), Child = sp
                });
            }

            void UpdateProgress()
            {
                int d = Tutorial.Checklist.Sum(g => g.Items.Count(it => State.IsChecked(g.Group + "|" + it)));
                pb.Value = d;
                pctRef.Text = $"{d} / {total}　({(total == 0 ? 0 : d * 100 / total)}%)";
            }

            return Scroll(stack);
        }

        // ── VCC 网页页 ──────────────────────────────────────────────────────
        FrameworkElement GetWebPage()
        {
            if (_webPage == null) _webPage = new WebPage();
            return _webPage;
        }

        // ── 自检用入口 ──────────────────────────────────────────────────────
        public void GoTo(int index)
        {
            if (index < 0 || index >= _navs.Count) return;
            if (_searchBox != null) _searchBox.Text = "";
            Navigate(index);
        }

        public void ApplyMode(UiMode m)
        {
            State.Mode = m;
            SyncSegs();
            RefreshAll();
        }

        public void ApplyWay(Way w)
        {
            State.Workflow = w;
            SyncSegs();
            RefreshAll();
        }

        /// <summary>把顶部两组分段按钮的选中态同步成当前状态（自检与状态恢复时用）</summary>
        void SyncSegs()
        {
            if (_rbBoth == null) return;
            _rbBoth.IsChecked = State.Mode == UiMode.Both;
            _rbZh.IsChecked = State.Mode == UiMode.Chinese;
            _rbEn.IsChecked = State.Mode == UiMode.English;
            _rbMa.IsChecked = State.Workflow == Way.MA;
            _rbCl.IsChecked = State.Workflow == Way.Classic;
        }

        public void SearchTest(string q)
        {
            if (_searchBox != null) _searchBox.Text = q;
        }

        void About()
        {
            MessageBox.Show(
                "模匠 · VRChat 改模指南  v1.6.1\n\n" +
                $"内置 {Tutorial.All.Length} 个章节、{Tutorial.ItemCount} 条教程、{Tutorial.Gallery.Length} 张图例。\n\n" +
                "· 顶部「Unity 界面」可在中文界面 / English UI / 双语之间切换，正文里的 Unity 菜单路径会同步变化。\n" +
                "· 顶部「改模方式」可在 MA 插件 / 古法 之间切换：\n   MA 插件 = 用 Modular Avatar、VRCFury 等插件自动换装；\n   古法 = 不装插件，手动重绑骨骼、手写 FX 动画层与菜单。\n" +
                "· 11–14 章是 dwgx「VRC 改模资源中心」60 篇原创深度指南的离线整合，15 章是它的资源导航。\n" +
                "· 顶部「重点」按钮：开启后把每条教程里人工标注的要点高亮出来（256 处正文要点 + 42 条提示整条标注），状态栏显示本页处数。\n" +
                "· 启动动画：透明背景，只有按 VRChat.png 重画的矢量图标（约 1.75 秒，点击/按键跳过，--nosplash 完全跳过）。\n" +
                "· VCC 镜像页内嵌 https://vcc.vrczh.org/ ，装环境时不用切浏览器。\n" +
                "· 赞助入口：顶部「♥ 赞助作者」按钮或左侧第 16 章 → 爱发电 https://afdian.com/a/7891mek\n" +
                "  （完全自愿，不弹窗、不影响任何功能）\n" +
                "· 检查清单与界面设置保存在：\n  " + State.Dir + "\n\n" +
                "教程内容整理自 VRChat 官方创作者文档、Modular Avatar / VRCFury 官方文档与中文社区教程。",
                "关于", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
