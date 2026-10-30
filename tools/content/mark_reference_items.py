# -*- coding: utf-8 -*-
"""给 Content1-4 里没有提示条的 13 条「速查 / 清单 / 链接」条目补一条人工重点。

提示条（Tip）本身在重点模式下会整条标注，所以只有这些没有 Tip 的条目需要补。
补的都是一句「怎么用这张表」的结论，人工挑选，不用关键词猜。
"""
import io
import os
import re

BASE = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), "app")

# (文件, 条目标题前缀, 模式, 文本)
# wrap = 把条目里已有的第一段正文整体标重点；insert = 在最前面插一段新的重点
JOBS = [
    ("Content1.cs", "名词速查表", "wrap", None),
    ("Content2.cs", "换装五大坑", "insert",
     "先按「症状 → 原因 → 处理」对号入座，别急着重做整件衣服。"),
    ("Content2.cs", "换装后自检 6 项", "wrap", None),
    ("Content3.cs", "常用操作清单", "insert",
     "这张表按「想做什么」查做法，找不到再回章节里看细节。"),
    ("Content4.cs", "性能自查流程", "insert",
     "按顺序走完这几步，性能等级基本不会翻车。"),
    ("Content4.cs", "交互组件", "insert",
     "每个组件都要算性能账：能少用一个就少用一个。"),
    ("Content4.cs", "自定义动画层要点", "insert",
     "动画层越多越容易互相打架，能合并就合并。"),
    ("Content4.cs", "20 条症状速查", "insert",
     "先看「最可能原因」那一列，八成问题在这里就有答案。"),
    ("Content4.cs", "参数与同步类问题", "insert",
     "参数超预算会静默失效，先查预算再看菜单。"),
    ("Content4.cs", "性能与被屏蔽", "insert",
     "被屏蔽多半是性能等级太低，先看等级再看模型。"),
    ("Content4.cs", "必看官方文档", "wrap", None),
    ("Content4.cs", "插件与工具", "insert",
     "工具统一用 VCC / ALCOM 装，版本不会互相踩。"),
    ("Content4.cs", "社区教程", "insert",
     "跟着视频做时先拿一个空项目练手，别直接上正式模型。"),
]

PARA_RE = re.compile(r'new Para\("((?:[^"\\]|\\.)*)"\)')


def find_item(s, prefix):
    i = s.find('new Item("' + prefix)
    if i < 0:
        i = s.find('new Item("')
        while i >= 0:
            j = s.find('")', i)
            title = s[i + 10:j]
            if title.startswith(prefix):
                return i
            i = s.find('new Item("', i + 1)
        return -1
    return i


def main():
    total = 0
    for fname, prefix, mode, text in JOBS:
        path = os.path.join(BASE, fname)
        s = io.open(path, encoding="utf-8-sig").read()
        start = find_item(s, prefix)
        assert start >= 0, "找不到条目: " + prefix
        # 条目的结束位置：下一个 new Item( / new Category( / Item.Ma( / Item.Classic(
        nxt = [s.find(k, start + 10) for k in
               ("\n            new Item(", "\n            Item.Ma(", "\n            Item.Classic(",
                "\n        static Category", "\n    }\n}")]
        nxt = [x for x in nxt if x > 0]
        end = min(nxt) if nxt else len(s)
        block = s[start:end]

        if mode == "wrap":
            m = PARA_RE.search(block)
            assert m, "条目没有 Para: " + prefix
            raw = m.group(1)
            assert "==" not in raw, "已经是重点: " + prefix
            new_block = block[:m.start()] + 'new Para("==' + raw + '==")' + block[m.end():]
        else:
            # 在 new Item( ... ,  之后插入一段 Para
            m = re.search(r'new Item\("[^"]*",\s*TagKind\.\w+,', block)
            assert m, "Item 头解析失败: " + prefix
            ins = m.end()
            new_block = block[:ins] + '\n                new Para("==' + text + '=="),' + block[ins:]

        s = s[:start] + new_block + s[end:]
        io.open(path, "w", encoding="utf-8", newline="").write(s)
        total += 1
        print("marked:", prefix, "(%s)" % mode)

    print("共标注条目:", total)


if __name__ == "__main__":
    main()
