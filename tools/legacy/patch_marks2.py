import io

# ── 1) 生成器：只保留「像规则/结论」的加粗，短工具名不标；每节首句做小标题结论 ──
p = r"C:\Users\Administrator\Desktop\源码\VRChat改模教程\tools\import_dwgx.py"
s = io.open(p, encoding="utf-8-sig").read()


def rep(old, new, tag):
    global s
    assert old in s, "MISS " + tag
    s = s.replace(old, new, 1)


rep('''BOLD_RE = re.compile(r"</?(?:b|strong)[^>]*>")


def bold_marks(html):
    """站点用 <b>/<strong> 标注的重点，转换成本程序的 ==…== 标记"""
    return BOLD_RE.sub("==", html)''',
    '''BOLD_PAIR = re.compile(r"<(?:b|strong)[^>]*>(.*?)</(?:b|strong)>", re.S)
RULE_HINT = re.compile(
    r"必须|务必|一定|不要|切勿|禁止|否则|会|失败|上限|预算|备份|推荐|注意|优先|只能|"
    r"建议|避免|检查|区别|原因|解决|默认|限制|风险|报错|坑|省")


def plain(html):
    t = re.sub(r"<[^>]+>", "", html)
    t = htmlmod.unescape(t)
    return re.sub(r"\\s+", " ", t).strip()


def bold_marks(html):
    """只把「句子型」的加粗当成重点：短的（工具名、菜单项）一律还原成普通文字"""
    def sub(m):
        inner = m.group(1)
        txt = plain(inner)
        if len(txt) < 14:
            return inner
        if len(txt) >= 22 or RULE_HINT.search(txt):
            return "==" + inner + "=="
        return inner
    return BOLD_PAIR.sub(sub, html)''',
    "bold predicate")

# 2) 每节（h3/h4）之后的第一段，若没有加粗重点，就给首句打标
rep('''def to_blocks(body):
    out = []''',
    '''def lead_mark(txt):
    """把段落的第一句变成重点（该节的核心结论）"""
    for i, ch in enumerate(txt):
        if ch in "。！？；" and i >= 10:
            return "==" + txt[:i + 1] + "==" + txt[i + 1:]
    if len(txt) >= 12:
        return "==" + txt + "=="
    return txt


def to_blocks(body):
    out = []
    after_head = False''',
    "lead mark helper")

rep('''        if tag in ("h4", "h3"):
            txt = clean(inner)
            if txt:
                out.append(("Head", txt))
        elif tag == "p":
            txt = clean(inner)
            if txt:
                out.append(("Para", txt))''',
    '''        if tag in ("h4", "h3"):
            txt = clean(inner)
            if txt:
                out.append(("Head", txt))
                after_head = True
        elif tag == "p":
            txt = clean(inner)
            if txt:
                if after_head and "==" not in txt:
                    txt = lead_mark(txt)
                after_head = False
                out.append(("Para", txt))''',
    "lead mark apply")

rep('''        elif tag in ("ol", "ul"):
            items = [clean(x) for x in re.findall(r"<li[^>]*>(.*?)</li>", inner, re.S)]''',
    '''        elif tag in ("ol", "ul"):
            after_head = False
            items = [clean(x) for x in re.findall(r"<li[^>]*>(.*?)</li>", inner, re.S)]''',
    "reset after head (list)")

rep('''            if len(grid) >= 2 and len(grid[0]) >= 2:''',
    '''            after_head = False
            if len(grid) >= 2 and len(grid[0]) >= 2:''',
    "reset after head (table)")

io.open(p, "w", encoding="utf-8", newline="").write(s)
print("generator: bold filtered + section leads")

# ── 2) 提示条：重点模式下整条正文标注 ──
p2 = r"C:\Users\Administrator\Desktop\源码\VRChat改模教程\app\Render.cs"
r = io.open(p2, encoding="utf-8-sig").read()
old = """            var inner = new StackPanel();
            inner.Children.Add(head);
            inner.Children.Add(Inline(tip.Text, 13.6 * S, Ink, mode));"""
new = """            var inner = new StackPanel();
            inner.Children.Add(head);
            // 提示条本身就是作者挑出来的重点：重点模式下整条标注
            string tipText = tip.Text;
            if (State.HotMode && tipText.IndexOf("==", StringComparison.Ordinal) < 0)
                tipText = "==" + tip.Text + "==";
            inner.Children.Add(Inline(tipText, 13.6 * S, Ink, mode));"""
assert old in r
r = r.replace(old, new, 1)
io.open(p2, "w", encoding="utf-8", newline="").write(r)
print("tip body marked")
