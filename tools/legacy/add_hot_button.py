import io

p = r"C:\Users\Administrator\Desktop\源码\VRChat改模教程\app\MainWindow.cs"
s = io.open(p, encoding="utf-8-sig").read()


def rep(old, new, tag):
    global s
    assert old in s, "MISS " + tag
    s = s.replace(old, new, 1)


# 1) 字段
rep("        RadioButton _rbBoth, _rbZh, _rbEn, _rbMa, _rbCl;",
    "        RadioButton _rbBoth, _rbZh, _rbEn, _rbMa, _rbCl;\n        Button _hotBtn;",
    "field")

# 2) 顶栏：改模方式后插入「重点」按钮
rep("""            right.Children.Add(new Border
            {
                Width = 1, Height = 22, Background = LineC,
                Margin = new Thickness(14, 0, 14, 0)
            });

            var fontBox""",
    """            right.Children.Add(new Border
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

            var fontBox""",
    "header button")

# 3) UpdateHotBtn / ApplyHot
rep("""        void ChangeFont(double delta)""",
    """        void UpdateHotBtn()
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

        void ChangeFont(double delta)""",
    "hot methods")

# 4) 渲染前清零计数，并在状态栏显示命中数
rep("""            if (_host == null) return;
            string q = _searchBox?.Text?.Trim() ?? "";""",
    """            if (_host == null) return;
            Render.HotCount = 0;
            string q = _searchBox?.Text?.Trim() ?? "";""",
    "reset counter")

rep("""            _statusRight.Text = (State.Mode switch
            {
                UiMode.Chinese => "中文界面用词",
                UiMode.English => "English UI 用词",
                _ => "中文 · English 双语对照"
            }) + "　|　" + (State.Workflow == Way.MA ? "MA 插件路线" : "古法路线");""",
    """            _statusRight.Text = (State.Mode switch
            {
                UiMode.Chinese => "中文界面用词",
                UiMode.English => "English UI 用词",
                _ => "中文 · English 双语对照"
            }) + "　|　" + (State.Workflow == Way.MA ? "MA 插件路线" : "古法路线")
              + (State.HotMode ? $"　|　重点已标注 {Render.HotCount} 处" : "");""",
    "status text")

# 5) 关于
rep('''                "· VCC 镜像页内嵌 https://vcc.vrczh.org/ ，装环境时不用切浏览器。\\n" +''',
    '''                "· 顶部「重点」按钮：开启后正文里必须注意 / 容易翻车的句子会高亮，状态栏显示本页命中的处数。\\n" +
                "· VCC 镜像页内嵌 https://vcc.vrczh.org/ ，装环境时不用切浏览器。\\n" +''',
    "about")

io.open(p, "w", encoding="utf-8", newline="").write(s)
print("MainWindow patched")
