import io

p = r"C:\Users\Administrator\Desktop\源码\VRChat改模教程\app\Render.cs"
s = io.open(p, encoding="utf-8-sig").read()

start = s.index("        // ── 重点模式 ──")
end = s.index("        // ── 文本 ──")
new = '''        // ── 重点模式 ────────────────────────────────────────────────────────
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
                else if (b is Bullets bu) t = string.Join("\\u0001", bu.Items);
                else if (b is Tip tp) t = tp.Text;
                else if (b is Head hd) t = hd.Text;
                else if (b is CodeBlock cb) t = cb.Note;
                else if (b is Table tt)
                    t = string.Join("\\u0001", tt.Head) + "\\u0001" +
                        string.Join("\\u0001", tt.Rows.SelectMany(r => r));
                if (t == null) continue;
                int i = 0;
                while ((i = t.IndexOf("==", i, StringComparison.Ordinal)) >= 0) { n++; i += 2; }
            }
            return n / 2;
        }

'''
s = s[:start] + new + s[end:]

io.open(p, "w", encoding="utf-8", newline="").write(s)
print("Render hot engine replaced")
