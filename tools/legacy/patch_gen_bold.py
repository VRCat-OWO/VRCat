import io

p = r"C:\Users\Administrator\Desktop\源码\VRChat改模教程\tools\import_dwgx.py"
s = io.open(p, encoding="utf-8-sig").read()


def rep(old, new, tag):
    global s
    assert old in s, "MISS " + tag
    s = s.replace(old, new, 1)


# 1) clean(): 保证 == 成对
rep('''    # 内容标记是 [[中文|English]]，源码里出现方括号会误解析
    t = t.replace("[[", "［［").replace("]]", "］］")
    return t''',
    '''    # 内容标记是 [[中文|English]]，源码里出现方括号会误解析
    t = t.replace("[[", "［［").replace("]]", "］］")
    # 重点标记 ==…== 必须成对，残缺就整段不标
    if t.count("==") % 2:
        t = t.replace("==", "")
    return t''',
    "clean marks")

# 2) 原文的加粗就是站长的重点 → 转成 ==…==
rep('''BLOCK_RE = re.compile(r"<(h4|h3|p|ol|ul|table|pre)[^>]*>(.*?)</\\1>", re.S)''',
    '''BOLD_RE = re.compile(r"</?(?:b|strong)[^>]*>")


def bold_marks(html):
    """站点用 <b>/<strong> 标注的重点，转换成本程序的 ==…== 标记"""
    return BOLD_RE.sub("==", html)


BLOCK_RE = re.compile(r"<(h4|h3|p|ol|ul|table|pre)[^>]*>(.*?)</\\1>", re.S)''',
    "bold regex")

# 3) to_blocks 里把所有片段先做加粗转换（pre 代码块除外）
rep('''    for m in BLOCK_RE.finditer(body):
        tag, inner = m.group(1), m.group(2)''',
    '''    for m in BLOCK_RE.finditer(body):
        tag, inner = m.group(1), m.group(2)
        if tag != "pre":
            inner = bold_marks(inner)''',
    "apply bold")

# 4) 一条教程如果完全没有重点，就把它的摘要行标重点（保证每条都有）
rep('''            title = f'{g["title"]}（{g["mins"]} 分钟）'
            lines = [f'            new Item({cs(title)}, TagKind.{tag},',
                     f'                new Para({cs(g["sub"])}),']''',
    '''            title = f'{g["title"]}（{g["mins"]} 分钟）'
            body_blocks = to_blocks(g["body"])
            marks = sum(str(b).count("==") for b in body_blocks)
            sub = f'=={g["sub"]}==' if marks == 0 else g["sub"]
            lines = [f'            new Item({cs(title)}, TagKind.{tag},',
                     f'                new Para({cs(sub)}),']''',
    "sub fallback")

rep('''            lines += [x + "," for x in block_calls(to_blocks(g["body"]), " " * 16)]''',
    '''            lines += [x + "," for x in block_calls(body_blocks, " " * 16)]''',
    "reuse blocks")

io.open(p, "w", encoding="utf-8", newline="").write(s)
print("generator patched")
