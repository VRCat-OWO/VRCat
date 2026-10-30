# -*- coding: utf-8 -*-
"""给 Content1-4 里每条教程的「最关键那一句」补一处人工重点。

规则：每条教程最多补 1 处，只从 Bullets（步骤/要点列表）里挑，
挑的是含「必须/不要/一定/否则/切记/记得/别」的第一条——也就是这条教程里
最容易翻车、最需要记住的约束。已经有了 == 标注的条目跳过。
"""
import io
import os
import re

BASE = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), "app")
FILES = ["Content1.cs", "Content2.cs", "Content3.cs", "Content4.cs"]
STRONG = ["必须", "不要", "一定", "否则", "切记", "记得", "别", "禁止", "千万", "务必", "唯一"]


def bullet_spans(block):
    """返回 block 内每个 Bullets(...) 的参数区间 [(start, end), ...]"""
    spans = []
    for m in re.finditer(r"new Bullets\(", block):
        i = m.end()
        depth = 1
        in_str = False
        esc = False
        while i < len(block) and depth > 0:
            c = block[i]
            if in_str:
                if esc:
                    esc = False
                elif c == "\\":
                    esc = True
                elif c == '"':
                    in_str = False
            else:
                if c == '"':
                    in_str = True
                elif c == "(":
                    depth += 1
                elif c == ")":
                    depth -= 1
                    if depth == 0:
                        break
            i += 1
        spans.append((m.end(), i))
    return spans


def string_literals(text, base_offset):
    """在参数区间里找 C# 字符串字面量，返回 [(值, 绝对起点, 绝对终点)]"""
    out = []
    i = 0
    while i < len(text):
        if text[i] != '"':
            i += 1
            continue
        j = i + 1
        esc = False
        while j < len(text):
            c = text[j]
            if esc:
                esc = False
            elif c == "\\":
                esc = True
            elif c == '"':
                break
            j += 1
        out.append((text[i + 1:j], base_offset + i, base_offset + j + 1))
        i = j + 1
    return out


def main():
    added = 0
    for fname in FILES:
        path = os.path.join(BASE, fname)
        s = io.open(path, encoding="utf-8-sig").read()
        idx = [m.start() for m in re.finditer(r"new Item\(|Item\.Ma\(|Item\.Classic\(", s)]
        pieces = []
        changed = 0
        for k, start in enumerate(idx):
            end = idx[k + 1] if k + 1 < len(idx) else len(s)
            block = s[start:end]
            if "==" in block:
                continue                      # 已经有重点，不动
            target = None
            for a, b in bullet_spans(block):
                for val, sa, sb in string_literals(block[a:b], start + a):
                    if len(val) < 14 or "==" in val:
                        continue
                    if any(w in val for w in STRONG):
                        target = (sa, sb, val)
                        break
                if target:
                    break
            if not target:
                continue
            sa, sb, val = target
            pieces.append((start, sa, sb, val))
            changed += 1

        # 逆序替换（同一文件内位置不重叠）
        for start, sa, sb, val in reversed(pieces):
            s = s[:sa] + '"==' + val + '=="' + s[sb:]
        if changed:
            io.open(path, "w", encoding="utf-8", newline="").write(s)
            print(fname, "补重点条目:", changed)
            added += changed
    print("合计补入:", added)


if __name__ == "__main__":
    main()
