# -*- coding: utf-8 -*-
"""
把 dwgx/vrc-mod-guide 的原创深度指南(guides.js)与资源清单(data.js)
转换成 VRChatGuide 的内容模型，生成 app/Content5.cs。

用法：python tools/import_dwgx.py
源文件放在 src_dwgx/ 下（从 https://dwgx.github.io/vrc-mod-guide/ 取得）
"""
import io
import os
import re
import sys
import html as htmlmod

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
SRC = os.path.join(ROOT, "input", "src_dwgx")
OUT = os.path.join(ROOT, "src", "MoJiang", "Content", "Content5.cs")
SITE = "https://dwgx.github.io/vrc-mod-guide/"

# ── 章节元信息（顺序参考站点 GORDER 学习路线） ─────────────────────────────
CHAPTERS = {
    0: dict(badge="11", title="精选指南 · 基础与上传",
            subtitle="从素体到第一个上传：环境、Rig、Blendshape、UV、SDK 面板、Blueprint ID",
            hint="整理自 dwgx 的 VRC 改模资源中心"),
    1: dict(badge="12", title="精选指南 · 表情与动画",
            subtitle="表情系统、Playable Layers、菜单、参数预算、Viseme、Blend Tree、道具交互",
            hint="看懂 FX 层与参数，才能做出真正好用的菜单"),
    2: dict(badge="13", title="精选指南 · 物理与外观",
            subtitle="PhysBone 调参、Constraints、着色器选择、材质合并、透明与自发光",
            hint="决定模型「好不好看、动不动得自然」的一章"),
    3: dict(badge="14", title="精选指南 · 性能与排错",
            subtitle="Quest 适配、性能等级、Fallback、报错排查、许可与来源安全",
            hint="上传之前最后一关：能不能被别人看到、会不会被拉黑"),
}

GROUP = {
    "ma-complete": 0, "zero-to-upload": 0, "sdk-upload-panel": 0, "local-testing": 0,
    "editor-debug-tools": 0, "rig-humanoid": 0, "blendshape-basics": 0, "uv-texture-basics": 0,
    "viewpoint-thumbnail": 0, "blueprint-id-update": 0, "trust-rank-system": 0,
    "where-to-get-avatars": 0, "cross-source-import": 0, "vroid-workflow": 0, "kitbash-basemodel": 0,

    "expression-gesture": 1, "playable-layers": 1, "expression-menu": 1, "parameter-memory": 1,
    "viseme-eyelook": 1, "blendtree-animation": 1, "custom-animation-emote": 1,
    "particle-effects": 1, "pickup-props": 1, "face-eye-tracking": 1, "face-customization": 1,
    "color-customization": 1, "shape-changer-toggle": 1, "sps-dps-intro": 1, "avatar-audio": 1,

    "physbone-tuning": 2, "dynamicbone-to-physbone": 2, "constraints": 2, "shader-intro": 2,
    "poiyomi-features": 2, "liltoon-vs-poiyomi": 2, "texture-recolor": 2,
    "mesh-material-merge": 2, "pbr-realistic-material": 2, "transparency-render-queue": 2,
    "emission-glow": 2, "blender-workflow": 2, "ma-advanced": 2, "contacts-advanced": 2,
    "vrcfury": 2,

    "quest-optimize": 3, "performance-safety": 3, "fallback-impostor": 3, "avatar-filesize": 3,
    "avatar-scaling": 3, "gogoloco": 3, "osc-control": 3, "audiolink": 3, "fbt-calibration": 3,
    "error-troubleshooting": 3, "ingame-avatar-issues": 3, "write-defaults": 3,
    "license-redistribution": 3, "avatar-safety-piracy": 3, "contacts": 3,
}

RES_GROUPS = [
    ("工具下载（官方与主流）", "TOOLS", 10,
     "全部优先用 VCC / ALCOM 装；下表是原站逐条核实过的官方与主流工具。"),
    ("图文教程与文档", "ARTICLES", 10,
     "官方文档与社区长文，遇到不确定的操作先查这里。"),
    ("素体 · 日系（Booth）", "BASES", 10,
     "商品图版权归作者，这里只放链接；价格与版本以 Booth 页面为准。"),
    ("进阶系统（移动 / OSC / 约束 / 许可）", "ADVANCED", 8,
     "移动系统、OSC、Constraints、商用与二配布许可。"),
    ("World 制作 / Udon", "WORLD", 8,
     "不只是改 avatar，做世界也一样能从这些文档起步。"),
    ("多语言教程（韩 / 法 / 德 / 西 / 俄 / 日 / 意）", "MULTILANG", 10,
     "官方 wiki 的多语言版本与各国社区教程。"),
    ("细分技术（头发 / 粒子 / AudioLink / FBT）", "TECH", 8,
     "按技术点拆分的进阶读物。"),
]

SECTIONS = [("素体 · 日系", "#bases"), ("素体 · 兽人/男性", "#furry"), ("衣装 / 配件", "#clothes"),
            ("质感 / 化妆", "#texture"), ("免费资源", "#free"), ("中文 B站视频", "#bili"),
            ("英语实战帖", "#pro"), ("多语言教程", "#multilang"), ("World 制作", "#world"),
            ("优化 / Quest", "#optimize"), ("报错排查", "#trouble")]


def clean(t):
    t = re.sub(r"<br\s*/?>", " ", t)
    t = re.sub(r"<[^>]+>", "", t)
    t = htmlmod.unescape(t)
    t = t.replace("\u00a0", " ")
    t = re.sub(r"[ \t\r\n]+", " ", t).strip()
    # 内容标记是 [[中文|English]]，源码里出现方括号会误解析
    t = t.replace("[[", "［［").replace("]]", "］］")
    # 重点标记 ==…== 必须成对，残缺就整段不标
    if t.count("==") % 2:
        t = t.replace("==", "")
    return t


def cs(s):
    return '"' + s.replace("\\", "\\\\").replace('"', '\\"') + '"'


BOLD_PAIR = re.compile(r"<(?:b|strong)[^>]*>(.*?)</(?:b|strong)>", re.S)
RULE_HINT = re.compile(
    r"必须|务必|一定|不要|切勿|禁止|否则|会|失败|上限|预算|备份|推荐|注意|优先|只能|"
    r"建议|避免|检查|区别|原因|解决|默认|限制|风险|报错|坑|省")


def plain(html):
    t = re.sub(r"<[^>]+>", "", html)
    t = htmlmod.unescape(t)
    return re.sub(r"\s+", " ", t).strip()


CJK = re.compile(r"[\u4e00-\u9fff]")
CJK_PUNCT = re.compile(r"[。！？；，：]")


def bold_qualifies(txt):
    """加粗的必须是「句子」才算重点：工具名、菜单项、选项名一律不算"""
    if len(txt) < 10:
        return False
    if not CJK.search(txt):          # 纯英文的菜单/选项名（As child keep world pose 之类）
        return False
    if RULE_HINT.search(txt):
        return True
    return len(txt) >= 22 and CJK_PUNCT.search(txt) is not None


def bold_marks(html):
    def sub(m):
        inner = m.group(1)
        return "==" + inner + "==" if bold_qualifies(plain(inner)) else inner
    return BOLD_PAIR.sub(sub, html)


BLOCK_RE = re.compile(r"<(h4|h3|p|ol|ul|table|pre)[^>]*>(.*?)</\1>", re.S)


def lead_mark(txt):
    """把段落的第一句变成重点（该节的核心结论）"""
    for i, ch in enumerate(txt):
        if ch in "。！？；" and i >= 10:
            return "==" + txt[:i + 1] + "==" + txt[i + 1:]
    if len(txt) >= 12:
        return "==" + txt + "=="
    return txt


def to_blocks(body):
    out = []
    after_head = False
    for m in BLOCK_RE.finditer(body):
        tag, inner = m.group(1), m.group(2)
        if tag != "pre":
            inner = bold_marks(inner)
        if tag in ("h4", "h3"):
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
                out.append(("Para", txt))
        elif tag in ("ol", "ul"):
            after_head = False
            items = [clean(x) for x in re.findall(r"<li[^>]*>(.*?)</li>", inner, re.S)]
            items = [x for x in items if x]
            if len(items) == 1:
                out.append(("Para", items[0]))
            elif items:
                out.append(("B", tag == "ol", items))
        elif tag == "table":
            rows = re.findall(r"<tr[^>]*>(.*?)</tr>", inner, re.S)
            grid = []
            for r in rows:
                cells = [clean(c) for c in re.findall(r"<t[hd][^>]*>(.*?)</t[hd]>", r, re.S)]
                if cells:
                    grid.append(cells)
            after_head = False
            if len(grid) >= 2 and len(grid[0]) >= 2:
                head = grid[0]
                body_rows = [r for r in grid[1:] if len(r) == len(head)]
                if body_rows:
                    out.append(("T", head, body_rows))
                else:
                    out.append(("Para", "，".join(grid[0])))
            elif grid:
                out.append(("Para", "，".join(grid[0])))
        elif tag == "pre":
            txt = clean(inner)
            if txt:
                out.append(("Code", txt))
    return out


def block_calls(blocks, pad):
    """每个块生成一条 C# 构造调用（不带结尾逗号）"""
    lines = []
    for b in blocks:
        k = b[0]
        if k == "Head":
            lines.append(f'{pad}new Head({cs(b[1])})')
        elif k == "Para":
            lines.append(f'{pad}new Para({cs(b[1])})')
        elif k == "B":
            _, numbered, items = b
            args = ", ".join(cs(x) for x in items)
            lines.append(f'{pad}new Bullets({"true" if numbered else "false"}, {args})')
        elif k == "T":
            _, head, rows = b
            hargs = ", ".join(cs(x) for x in head)
            rargs = ",\n".join(pad + "    new[] { " + ", ".join(cs(x) for x in r) + " }" for r in rows)
            lines.append(f'{pad}new Table(new[] {{ {hargs} }},\n{rargs})')
        elif k == "Code":
            lines.append(f'{pad}new CodeBlock({cs(b[1])})')
    return lines


def parse_guides():
    s = io.open(os.path.join(SRC, "guides.js"), encoding="utf-8").read()
    pat = re.compile(
        r'\{\s*id:"([^"]+)",\s*title:"([^"]*)",\s*sub:"([^"]*)",\s*mins:(\d+),'
        r'\s*tags:\[(.*?)\],\s*body:`(.*?)`\s*\}', re.S)
    guides = []
    for m in pat.finditer(s):
        gid, title, sub, mins, tags, body = m.groups()
        guides.append(dict(id=gid, title=clean(title), sub=clean(sub), mins=int(mins),
                           tags=[clean(t) for t in re.findall(r'"([^"]*)"', tags)], body=body))
    return guides


def order_guides(guides):
    idx = io.open(os.path.join(SRC, "index.html"), encoding="utf-8", errors="ignore").read()
    m = re.search(r"GORDER=\[(.*?)\]", idx, re.S)
    order = re.findall(r'"([^"]+)"', m.group(1)) if m else []
    pos = {gid: i for i, gid in enumerate(order)}
    return sorted(guides, key=lambda g: (pos.get(g["id"], 999), guides.index(g)))


def parse_data():
    s = io.open(os.path.join(SRC, "data.js"), encoding="utf-8").read()
    arrays = {}
    for m in re.finditer(r"const (\w+)\s*=\s*\[", s):
        name, start = m.group(1), m.end()
        nxt = s.find("\nconst ", start)
        block = s[start:nxt if nxt > 0 else len(s)]
        entries = []
        for obj in re.findall(r"\{([^{}]*)\}", block):
            fields = dict(re.findall(r'(\w+):"([^"]*)"', obj))
            if not fields.get("url"):
                continue
            title = (fields.get("n") or fields.get("t") or fields.get("title")
                     or fields.get("name") or "").strip()
            if not title:
                continue
            entries.append(dict(title=clean(title), url=fields["url"].strip(),
                                tag=clean(fields.get("tag", "")), verify=fields.get("v", "")))
        arrays[name] = entries
    return arrays


def check_balance(text):
    """跳过字符串与注释后统计括号，返回最终深度（namespace + class = 2 表示闭合）"""
    depth, i, n, in_str, esc = 0, 0, len(text), False, False
    while i < n:
        c = text[i]
        if in_str:
            if esc:
                esc = False
            elif c == "\\":
                esc = True
            elif c == '"':
                in_str = False
            i += 1
            continue
        if c == '"':
            in_str = True
        elif c == "/" and i + 1 < n and text[i + 1] == "/":
            j = text.find("\n", i)
            i = n if j < 0 else j
            continue
        elif c == "(":
            depth += 1
        elif c == ")":
            depth -= 1
        i += 1
    return depth


def build():
    guides = order_guides(parse_guides())
    by_ch = {0: [], 1: [], 2: [], 3: []}
    unmapped = []
    for g in guides:
        ch = GROUP.get(g["id"])
        if ch is None:
            unmapped.append(g["id"])
            ch = 3
        by_ch[ch].append(g)

    out = ["// <auto-generated> 由 tools/import_dwgx.py 从 dwgx/vrc-mod-guide 生成，请勿手改",
           "// 来源：https://dwgx.github.io/vrc-mod-guide/ （原创深度指南 60 篇 + 资源清单）",
           "// </auto-generated>",
           "namespace VRChatGuide",
           "{",
           "    internal static class Content5",
           "    {",
           "        internal static Category[] All() => new[] { Chapter0(), Chapter1(), Chapter2(), Chapter3(), Resources() };",
           ""]

    counts = {}
    for ch in range(4):
        meta = CHAPTERS[ch]
        items = by_ch[ch]
        counts[meta["badge"]] = len(items)
        out.append(f'        static Category Chapter{ch}() => new Category("{meta["badge"]}",')
        out.append(f'            {cs(meta["title"])},')
        out.append(f'            {cs(meta["subtitle"])},')
        out.append(f'            {cs(meta["hint"] + " · " + str(len(items)) + " 篇")},')
        texts = []
        for g in items:
            tag = "Advanced" if any("进阶" in t for t in g["tags"]) else "Main"
            title = f'{g["title"]}（{g["mins"]} 分钟）'
            body_blocks = to_blocks(g["body"])
            marks = sum(str(b).count("==") for b in body_blocks)
            sub = f'=={g["sub"]}==' if marks == 0 else g["sub"]
            lines = [f'            new Item({cs(title)}, TagKind.{tag},',
                     f'                new Para({cs(sub)}),']
            lines += [x + "," for x in block_calls(body_blocks, " " * 16)]
            lines.append(f'                new Links(("原文 · dwgx 资源中心", '
                         f'{cs(SITE + "#guides")}, {cs(g["id"])})))')
            texts.append("\n".join(lines))
        out.append(",\n".join(texts) + ");")
        out.append("")

    # 15 资源库导航
    data = parse_data()
    res_items = []
    intro = ["            new Item(\"这个资源库怎么用\", TagKind.Basic,",
             '                new Para("==点链接回到原站可以看到完整列表、可信度标记和商品价格；Booth 商品图版权归作者，本程序只做索引。=="),',
             "                new Bullets(false,",
             '                    "工具优先用 VCC / ALCOM 安装，别下散装 unitypackage。",',
             '                    "素体要看清对应体型和许可，商用 / 二配布限制写在作者页面里。",',
             '                    "视频类资源需要联网，离线时只有文中的要点可用。"),',
             f'                new Links(("VRC 改模资源中心（原站）", {cs(SITE)}, "视频 / 文章 / 素体 / 衣装 大全")))']
    res_items.append("\n".join(intro))

    for caption, key, cap, note in RES_GROUPS:
        entries = [e for e in data.get(key, []) if e.get("verify") in ("high", "")] or data.get(key, [])
        seen, uniq = set(), []
        for e in entries:
            if e["title"] in seen:
                continue
            seen.add(e["title"])
            uniq.append(e)
        pick = uniq[:cap]
        if not pick:
            continue
        lines = [f'            new Item({cs(caption)}, TagKind.Tool,',
                 f'                new Para({cs("==" + note + "==")}),']
        args = ",\n".join(f'                    ({cs(e["title"])}, {cs(e["url"])}, {cs(e["tag"])})'
                          for e in pick)
        lines.append(f'                new Links({args}))')
        res_items.append("\n".join(lines))

    last = ["            new Item(\"其他分区（原站直达）\", TagKind.Tool,",
            '                new Para("==素体（兽人/男性）、衣装、质感/化妆、免费资源、中文社区、B站视频、World、其他平台素体等分区都在原站，条目太多不逐条收录。=="),']
    args = ",\n".join(f'                    ({cs(n)}, {cs(SITE + a)}, "分区直达")' for n, a in SECTIONS)
    last.append(f'                new Links({args}))')
    res_items.append("\n".join(last))

    out.append('        static Category Resources() => new Category("15",')
    out.append('            "资源库导航（dwgx）",')
    out.append('            "工具、素体、衣装、视频、多语言教程的直达入口，全部来自 VRC 改模资源中心",')
    out.append('            "原站共 134+ 条资源，这里挑出每类里可信度最高的若干条 · 8 组",')
    out.append(",\n".join(res_items) + ");")
    out.append("    }")
    out.append("}")
    out.append("")

    text = "\n".join(out)
    depth = check_balance(text)
    if depth != 0:
        print(f"!! 括号不平衡，depth={depth}（应为 0），未写入文件", file=sys.stderr)
        sys.exit(1)

    io.open(OUT, "w", encoding="utf-8", newline="\r\n").write(text)
    print("wrote", OUT)
    print("chapters:", counts, "资源组:", len(res_items))
    if unmapped:
        print("未映射:", unmapped)


if __name__ == "__main__":
    build()
