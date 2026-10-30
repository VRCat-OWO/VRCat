# -*- coding: utf-8 -*-
"""
「模匠」程序图标生成器：两种版式
  A（默认）头像剪影 + 星芒
  B        「模」字标 + 星芒
共同底：深海军蓝圆角瓷砖 + 青→紫渐变描边 + 青色外发光。

输出：
  src/MoJiang/Assets/app.ico          A 版多尺寸（16/24/32/48/64/128/256）
  src/MoJiang/Assets/icon-1024.png    A 版 1024
  src/MoJiang/Assets/icon-b-1024.png  B 版 1024
  dist/icon-variants.png      两版对照（浅底 / 深底 / 小尺寸）

切换版式：python tools/make_icon2.py b    → 用 B 版覆盖 app.ico
"""
import os
import sys

import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageFont

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
ASSETS = os.path.join(ROOT, "src", "MoJiang", "Assets")
DIST = os.path.join(ROOT, "dist")

N = 1024
SS = 4
S = N * SS

NAVY_HI = np.array([0x12, 0x22, 0x3A], dtype=float)
NAVY_LO = np.array([0x07, 0x0E, 0x1A], dtype=float)
CYAN = np.array([0x2A, 0xD8, 0xFF], dtype=float)
VIOLET = np.array([0x7C, 0x5C, 0xFF], dtype=float)
WHITE = np.array([0xFF, 0xFF, 0xFF], dtype=float)
SPARK = np.array([0x9C, 0xF9, 0xFF], dtype=float)
FONT_BOLD = r"C:\Windows\Fonts\msyhbd.ttc"


def lerp(a, b, t):
    t = np.clip(t, 0.0, 1.0)
    if np.isscalar(t):
        return a + (b - a) * t
    return a[None, None, :] + (b - a)[None, None, :] * t[:, :, None]


def layer(color, alpha):
    c = np.broadcast_to(np.asarray(color, dtype=float), (S, S, 3))
    a = np.clip(np.asarray(alpha, dtype=float), 0, 255)
    return np.dstack([c, a]).astype(np.uint8)


def rounded_mask(box, radius):
    m = Image.new("L", (S, S), 0)
    ImageDraw.Draw(m).rounded_rectangle(box, radius=radius, fill=255)
    return m


def tile_background():
    """深色瓷砖 + 渐变 + 径向光 + 斜向高光 + 描边 + 外发光"""
    yy, xx = np.mgrid[0:S, 0:S].astype(np.float32)
    u = xx / (S - 1)
    v = yy / (S - 1)

    base = lerp(NAVY_HI, NAVY_LO, (v * 0.95 + 0.05) ** 0.85)
    glow = np.exp(-(((u - 0.5) ** 2) / 0.26 + ((v - 0.34) ** 2) / 0.30))
    base = base + (CYAN[None, None, :] - base) * (glow * 0.30)[:, :, None]
    sheen = np.clip(1.0 - np.abs(u * 1.4 + v * 0.9 - 0.55) * 2.2, 0, 1) ** 2
    base = base + np.array([22, 30, 44], dtype=float)[None, None, :] * sheen[:, :, None]

    box = [int(0.005 * S), int(0.005 * S), int(0.995 * S), int(0.995 * S)]
    radius = int(0.225 * S)
    tile = rounded_mask(box, radius).filter(ImageFilter.GaussianBlur(SS * 0.8))
    rgba = Image.fromarray(np.clip(np.dstack([base, np.asarray(tile, dtype=float)]), 0, 255).astype(np.uint8), "RGBA")

    inset = int(0.028 * S)
    inner = rounded_mask([box[0] + inset, box[1] + inset, box[2] - inset, box[3] - inset],
                         max(4, radius - inset))
    rim_np = np.clip(np.asarray(rounded_mask(box, radius), float) - np.asarray(inner, float), 0, 255) / 255.0
    rim_col = lerp(CYAN, VIOLET, u * 1.02)
    rgba = Image.alpha_composite(rgba, Image.fromarray(layer(rim_col, rim_np * 235.0), "RGBA"))

    halo = Image.fromarray((rim_np * 255).astype(np.uint8), "L").filter(ImageFilter.GaussianBlur(SS * 9))
    rgba = Image.alpha_composite(rgba, Image.fromarray(
        layer(CYAN, np.asarray(halo, float) / 255.0 * 130.0), "RGBA"))
    return rgba, u, v


def add_spark(rgba, cx=0.712, cy=0.243, R=0.072, r=0.019):
    sp = Image.new("L", (S, S), 0)
    d = ImageDraw.Draw(sp)
    px, py = int(cx * S), int(cy * S)
    R, r = int(R * S), int(r * S)
    pts = []
    for i in range(8):
        ang = np.pi / 4 * i - np.pi / 2
        rad = R if i % 2 == 0 else r
        pts.append((px + np.cos(ang) * rad, py + np.sin(ang) * rad))
    d.polygon(pts, fill=255)
    sp_np = np.asarray(sp, float) / 255.0
    glow = np.asarray(sp.filter(ImageFilter.GaussianBlur(SS * 5)), float) / 255.0
    rgba = Image.alpha_composite(rgba, Image.fromarray(layer(SPARK, glow * 150.0), "RGBA"))
    rgba = Image.alpha_composite(rgba, Image.fromarray(layer([0xF2, 0xFF, 0xFF], sp_np * 255.0), "RGBA"))
    return rgba


def build_avatar():
    """A 版：头 + 肩剪影"""
    rgba, u, v = tile_background()

    mark = Image.new("L", (S, S), 0)
    md = ImageDraw.Draw(mark)
    hr = int(0.132 * S)
    hcx, hcy = int(0.5 * S), int(0.372 * S)
    md.ellipse([hcx - hr, hcy - hr, hcx + hr, hcy + hr], fill=255)
    md.ellipse([int(0.288 * S), int(0.545 * S), int(0.712 * S), int(1.075 * S)], fill=255)
    mark_np = np.asarray(mark, float) / 255.0
    mark_np[int(0.815 * S):, :] = 0
    mark_np[:, :int(0.245 * S)] = 0
    mark_np[:, int(0.755 * S):] = 0

    mark_col = lerp(WHITE, CYAN * 0.85 + WHITE * 0.15, (v - 0.22) / 0.62)
    rgba = Image.alpha_composite(rgba, Image.fromarray(layer(mark_col, mark_np * 255.0), "RGBA"))

    shade = np.clip((v - 0.62) / 0.30, 0, 1) ** 1.5
    rgba = Image.alpha_composite(rgba, Image.fromarray(
        np.dstack([np.zeros((S, S, 3)), mark_np * shade * 70.0]).astype(np.uint8), "RGBA"))
    return add_spark(rgba)


def build_glyph():
    """B 版：「模」字标"""
    rgba, u, v = tile_background()

    glyph = Image.new("L", (S, S), 0)
    font = ImageFont.truetype(FONT_BOLD, int(0.60 * S))
    ImageDraw.Draw(glyph).text((S // 2, int(0.535 * S)), "模", font=font, fill=255, anchor="mm")
    g_np = np.asarray(glyph, float) / 255.0

    glyph_col = lerp(WHITE, CYAN * 0.9 + WHITE * 0.1, (v - 0.2) / 0.66)
    # 字标外发光，让笔画在深底上更亮
    glow = np.asarray(glyph.filter(ImageFilter.GaussianBlur(SS * 6)), float) / 255.0
    rgba = Image.alpha_composite(rgba, Image.fromarray(layer(CYAN, glow * 110.0), "RGBA"))
    rgba = Image.alpha_composite(rgba, Image.fromarray(layer(glyph_col, g_np * 255.0), "RGBA"))
    return add_spark(rgba, cx=0.775, cy=0.755, R=0.062, r=0.017)


def sheet(a, b, path):
    W, H = 900, 420
    img = Image.new("RGB", (W, H), (245, 247, 251))
    d = ImageDraw.Draw(img)
    d.rectangle([0, 0, W, H // 2], fill=(245, 247, 251))
    d.rectangle([0, H // 2, W, H], fill=(16, 20, 28))
    for k, icon in enumerate((a, b)):
        for row in (0, H // 2):
            big = icon.resize((132, 132), Image.LANCZOS)
            img.paste(big, (40 + k * 200, row + 18), big)
            x = 40 + k * 200
            x += 150
            for s in (64, 48, 32, 24, 16):
                small = icon.resize((s, s), Image.LANCZOS)
                img.paste(small, (x, row + (168 - s) // 2), small)
                x += s + 14
            break
    # 下半区：两版放大对照
    for k, icon in enumerate((a, b)):
        big = icon.resize((150, 150), Image.LANCZOS)
        img.paste(big, (60 + k * 220, H // 2 + 25), big)
        x = 60 + k * 220 + 170
        for s in (48, 32, 24, 16):
            small = icon.resize((s, s), Image.LANCZOS)
            img.paste(small, (x, H // 2 + (175 - s) // 2), small)
            x += s + 14
    img.save(path)


def write_ico(icon, path):
    sizes = [16, 24, 32, 48, 64, 128, 256]
    frames = [icon.resize((s, s), Image.LANCZOS) for s in sizes]
    frames[-1].save(path, format="ICO", sizes=[(s, s) for s in sizes], append_images=frames[:-1])


def main():
    os.makedirs(ASSETS, exist_ok=True)
    os.makedirs(DIST, exist_ok=True)
    a = build_avatar().resize((N, N), Image.LANCZOS)
    b = build_glyph().resize((N, N), Image.LANCZOS)

    a.save(os.path.join(ASSETS, "icon-1024.png"))
    b.save(os.path.join(ASSETS, "icon-b-1024.png"))
    sheet(a, b, os.path.join(DIST, "icon-variants.png"))

    which = (sys.argv[1].lower() if len(sys.argv) > 1 else "a")
    write_ico(b if which == "b" else a, os.path.join(ASSETS, "app.ico"))
    print("app.ico <-", "B 版（模字标）" if which == "b" else "A 版（头像剪影）")
    print("artifacts:", os.path.join(ASSETS, "icon-1024.png"), "|", os.path.join(DIST, "icon-variants.png"))


if __name__ == "__main__":
    main()
