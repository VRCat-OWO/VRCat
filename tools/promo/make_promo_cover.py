# -*- coding: utf-8 -*-
"""
B 站投稿封面：1146×717（B 站推荐比例，≤5 MB）。
复用宣传片的字体与卡片工具，左侧品牌字 + 右侧真机界面。
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import numpy as np
from PIL import Image, ImageDraw

import make_promo_video as mv

W, H = 1146, 717
ROOT = mv.ROOT
OUT = os.path.join(ROOT, "dist", "模匠_封面_1146x717.png")


def bg():
    base = mv.bg_base().resize((W, H), Image.LANCZOS).convert("RGBA")
    yy, xx = np.mgrid[0:H, 0:W].astype(np.float32)
    u, v = xx / (W - 1), yy / (H - 1)
    glow = np.exp(-(((u - 0.22) ** 2) / 0.10 + ((v - 0.30) ** 2) / 0.16))
    acc = np.array([18, 104, 156], float)
    a = np.asarray(base).astype(float)
    a[:, :, :3] = a[:, :, :3] + (acc - a[:, :, :3]) * (glow * 0.5)[:, :, None]
    return Image.fromarray(np.clip(a, 0, 255).astype(np.uint8), "RGBA")


def main():
    c = bg()

    # 右侧：主界面卡片，故意出血到画布外，构图更饱满
    card = mv.card("main.png", 404)
    mv.blit(c, card, (458, 150), 1.0)

    # 左侧品牌
    ico = mv.src("icon.png").resize((104, 104), Image.LANCZOS)
    mv.blit(c, ico, (62, 62), 1.0)

    t1 = mv.text_tile("模匠", 92, (255, 255, 255), tracking=8)
    mv.blit(c, t1, (58, 176), 1.0)

    t2 = mv.text_tile("VRChat 改模指南", 34, (198, 232, 248), tracking=3)
    mv.blit(c, t2, (62, 306), 1.0)

    line = mv.text_tile("16 章 · 124 条 · 完全离线", 28, (150, 178, 204), bold=False)
    mv.blit(c, line, (64, 366), 1.0)

    # 三个标签
    tags = [
        mv.pill("MA / 古法 双路线", 23),
        mv.pill("重点标注", 23),
        mv.pill("中英双语界面", 23),
    ]
    y = 424
    x = 64
    for tg in tags:
        if x + tg.width > 408:
            x = 64
            y += tg.height + 14
        mv.blit(c, tg, (x, y), 1.0)
        x += tg.width + 14

    # 底部条
    d = ImageDraw.Draw(c)
    d.rectangle([0, H - 74, W, H], fill=(8, 16, 28, 235))
    bot = mv.text_tile("Windows 10 / 11 · 免安装绿色版 · 内置 VCC 镜像", 24, (150, 178, 204), bold=False)
    mv.blit(c, bot, (48, H - 58), 1.0)
    af = mv.text_tile("爱发电 afdian.com/a/7891mek", 24, (255, 186, 204), bold=False)
    mv.blit(c, af, (W - af.width - 40, H - 58), 1.0)

    out = c.convert("RGB")
    out.save(OUT, optimize=True)
    print("cover:", OUT, "%.2f MB" % (os.path.getsize(OUT) / 1024 / 1024), out.size)


if __name__ == "__main__":
    main()
