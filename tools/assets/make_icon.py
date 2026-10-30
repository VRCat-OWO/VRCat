# -*- coding: utf-8 -*-
"""
生成「模匠」的程序图标：深海军蓝圆角瓷砖 + 青色描边 + 头像剪影 + 星芒。

输出：
  src/MoJiang/Assets/app.ico       多尺寸（16/24/32/48/64/128/256）
  src/MoJiang/Assets/icon-1024.png 1024 预览
  dist/icon-preview.png    浅底/深底对照 + 小尺寸可读性检查

思路：全部用超采样（4×）矢量绘制再降采样，保证边缘干净；
不依赖 WPF，所以可以在打包前单独生成、单独检查。
"""
import os

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
ASSETS = os.path.join(ROOT, "src", "MoJiang", "Assets")
DIST = os.path.join(ROOT, "dist")

N = 1024          # 最终尺寸
SS = 4            # 超采样倍数
S = N * SS

# 配色
NAVY_HI = np.array([0x12, 0x22, 0x3A], dtype=float)   # 上部
NAVY_LO = np.array([0x07, 0x0E, 0x1A], dtype=float)   # 下部
CYAN = np.array([0x2A, 0xD8, 0xFF], dtype=float)
VIOLET = np.array([0x7C, 0x5C, 0xFF], dtype=float)
WHITE = np.array([0xFF, 0xFF, 0xFF], dtype=float)
SPARK = np.array([0x9C, 0xF9, 0xFF], dtype=float)


def lerp(a, b, t):
    t = np.clip(t, 0.0, 1.0)
    if np.isscalar(t):
        return a + (b - a) * t
    return a[None, None, :] + (b - a)[None, None, :] * t[:, :, None]


def layer(color, alpha):
    """把 3 元色和一个 S×S 的 alpha 拼成 RGBA 图层"""
    c = np.broadcast_to(np.asarray(color, dtype=float), (S, S, 3))
    a = np.clip(np.asarray(alpha, dtype=float), 0, 255)
    return np.dstack([c, a]).astype(np.uint8)


def rounded_mask(size, box, radius, fill=255):
    m = Image.new("L", (size, size), 0)
    d = ImageDraw.Draw(m)
    d.rounded_rectangle(box, radius=radius, fill=fill)
    return m


def main():
    yy, xx = np.mgrid[0:S, 0:S].astype(np.float32)
    u = xx / (S - 1)
    v = yy / (S - 1)

    # ── 瓷砖底色：竖向渐变 + 中上偏青的径向光 ──────────────────────────
    base = lerp(NAVY_HI, NAVY_LO, (v * 0.95 + 0.05) ** 0.85)
    glow = np.exp(-(((u - 0.5) ** 2) / 0.26 + ((v - 0.34) ** 2) / 0.30))
    base = base + (CYAN[None, None, :] - base) * (glow * 0.30)[:, :, None]
    # 左上一道斜向高光，避免死板
    sheen = np.clip(1.0 - np.abs(u * 1.4 + v * 0.9 - 0.55) * 2.2, 0, 1) ** 2
    base = base + np.array([22, 30, 44], dtype=float)[None, None, :] * sheen[:, :, None]

    tile_box = [int(0.005 * S), int(0.005 * S), int(0.995 * S), int(0.995 * S)]
    tile_r = int(0.225 * S)
    tile = rounded_mask(S, tile_box, tile_r)
    # 边缘轻微柔化（超采样后降采样也不会出现硬锯齿）
    tile = tile.filter(ImageFilter.GaussianBlur(SS * 0.8))

    img = np.dstack([base, np.asarray(tile, dtype=float)])
    img = np.clip(img, 0, 255)

    rgba = Image.fromarray(img.astype(np.uint8), "RGBA")

    # ── 内描边：青 → 紫 横向渐变 ────────────────────────────────────────
    inset = int(0.028 * S)
    outer = rounded_mask(S, tile_box, tile_r)
    inner_box = [tile_box[0] + inset, tile_box[1] + inset, tile_box[2] - inset, tile_box[3] - inset]
    inner = rounded_mask(S, inner_box, max(4, tile_r - inset))
    rim = Image.fromarray(np.clip(np.asarray(outer, float) - np.asarray(inner, float), 0, 255).astype(np.uint8), "L")
    rim_np = np.asarray(rim, dtype=float) / 255.0
    rim_col = lerp(CYAN, VIOLET, u * 1.02)
    rim_alpha = rim_np * 0.92
    rim_layer = layer(rim_col if rim_col.ndim == 3 else CYAN, rim_alpha * 255.0)
    rgba = Image.alpha_composite(rgba, Image.fromarray(rim_layer, "RGBA"))

    # 描边外发光
    halo = rim.filter(ImageFilter.GaussianBlur(SS * 9))
    halo_np = np.asarray(halo, dtype=float) / 255.0
    halo_layer = layer(CYAN, halo_np * 130.0)
    rgba = Image.alpha_composite(rgba, Image.fromarray(halo_layer, "RGBA"))

    # ── 头像剪影：头 + 肩 ──────────────────────────────────────────────
    mark = Image.new("L", (S, S), 0)
    md = ImageDraw.Draw(mark)
    # 头
    hr = int(0.132 * S)
    hcx, hcy = int(0.5 * S), int(0.372 * S)
    md.ellipse([hcx - hr, hcy - hr, hcx + hr, hcy + hr], fill=255)
    # 肩：椭圆下缘裁掉
    sx0, sy0 = int(0.288 * S), int(0.545 * S)
    sx1, sy1 = int(0.712 * S), int(1.075 * S)
    md.ellipse([sx0, sy0, sx1, sy1], fill=255)
    mark_np = np.asarray(mark, dtype=float) / 255.0
    # 只保留到 y = 0.815 处（肩线）
    cut = int(0.815 * S)
    mark_np[cut:, :] = 0
    mark_np[:, :int(0.245 * S)] = 0
    mark_np[:, int(0.755 * S):] = 0

    # 剪影颜色：上白下青
    mark_col = lerp(WHITE, CYAN * 0.85 + WHITE * 0.15, (v - 0.22) / 0.62)
    mark_layer = layer(mark_col if mark_col.ndim == 3 else WHITE, mark_np * 255.0)
    rgba = Image.alpha_composite(rgba, Image.fromarray(mark_layer, "RGBA"))

    # 剪影内部的一点立体感：底部加暗
    shade = np.clip((v - 0.62) / 0.30, 0, 1) ** 1.5
    shy = np.dstack([np.zeros((S, S, 3)), (mark_np * shade * 70.0)]).astype(np.uint8)
    rgba = Image.alpha_composite(rgba, Image.fromarray(shy, "RGBA"))

    # ── 星芒（右上） ──────────────────────────────────────────────────
    sp = Image.new("L", (S, S), 0)
    sd = ImageDraw.Draw(sp)
    cx, cy = int(0.712 * S), int(0.243 * S)
    R, r = int(0.072 * S), int(0.019 * S)
    pts = []
    for i in range(8):
        ang = np.pi / 4 * i - np.pi / 2
        rad = R if i % 2 == 0 else r
        pts.append((cx + np.cos(ang) * rad, cy + np.sin(ang) * rad))
    sd.polygon(pts, fill=255)
    sp_np = np.asarray(sp, dtype=float) / 255.0
    # 星芒加一圈柔光
    glow2 = np.asarray(sp.filter(ImageFilter.GaussianBlur(SS * 5)), dtype=float) / 255.0
    rgba = Image.alpha_composite(rgba, Image.fromarray(layer(SPARK, glow2 * 150.0), "RGBA"))
    rgba = Image.alpha_composite(rgba, Image.fromarray(layer([0xF2, 0xFF, 0xFF], sp_np * 255.0), "RGBA"))

    # ── 降采样输出 ───────────────────────────────────────────────────
    big = rgba.resize((N, N), Image.LANCZOS)
    os.makedirs(ASSETS, exist_ok=True)
    os.makedirs(DIST, exist_ok=True)
    big.save(os.path.join(ASSETS, "icon-1024.png"))

    sizes = [16, 24, 32, 48, 64, 128, 256]
    frames = [big.resize((s, s), Image.LANCZOS) for s in sizes]
    frames[-1].save(os.path.join(ASSETS, "app.ico"), format="ICO",
                    sizes=[(s, s) for s in sizes], append_images=frames[:-1])

    # ── 预览图：浅底 / 深底 + 各尺寸 ─────────────────────────────────
    W, H = 880, 400
    sheet = Image.new("RGB", (W, H), (245, 247, 251))
    d = ImageDraw.Draw(sheet)
    d.rectangle([0, 0, W, H // 2], fill=(245, 247, 251))
    d.rectangle([0, H // 2, W, H], fill=(16, 20, 28))

    for row_top in (0, H // 2):
        row_h = H // 2
        x = 30
        ic = big.resize((150, 150), Image.LANCZOS)
        sheet.paste(ic, (x, row_top + (row_h - 150) // 2), ic)
        x += 150 + 40
        for s in (128, 96, 64, 48, 32, 24, 16):
            small = big.resize((s, s), Image.LANCZOS)
            sheet.paste(small, (x, row_top + (row_h - s) // 2), small)
            x += s + 22
    sheet.save(os.path.join(DIST, "icon-preview.png"))
    print("icon written:", os.path.join(ASSETS, "app.ico"))
    print("preview:", os.path.join(DIST, "icon-preview.png"))


if __name__ == "__main__":
    main()
