# -*- coding: utf-8 -*-
"""
生成「模匠」宣传片：1920×1080 / 30fps / 43 秒，H.264 + AAC，可直接传 B 站。

画面全部由真实程序截图 + 程序图标 + 启动动画帧组成，文字用 PIL 绘制；
编码用 imageio-ffmpeg 自带的 ffmpeg 二进制，帧数据通过 stdin 管道喂进去。
"""
import math
import os
import subprocess
import sys

import numpy as np
from PIL import Image, ImageDraw, ImageFont, ImageFilter

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
ASSETS = os.path.join(ROOT, "input", "promo-assets")
DIST = os.path.join(ROOT, "dist")
OUT = os.path.join(DIST, "模匠_宣传片_1080p.mp4")
AUDIO = os.path.join(DIST, "promo-audio.wav")
FRAME_DIR = os.path.join(DIST, "promo-frames")

W, H, FPS = 1920, 1080, 30
DUR = 43.0

F_BOLD = r"C:\Windows\Fonts\msyhbd.ttc"
F_REG = r"C:\Windows\Fonts\msyh.ttc"

C_WHITE = (255, 255, 255)
C_SUB = (168, 190, 214)
C_DIM = (120, 142, 168)
C_ACC = (42, 216, 255)
C_ACC2 = (124, 92, 255)
C_PINK = (255, 128, 160)

SCENES = [
    ("intro", 0.0, 4.8),
    ("main", 4.8, 11.4),
    ("switch", 11.4, 18.0),
    ("content", 18.0, 23.6),
    ("splash", 23.6, 28.8),
    ("offline", 28.8, 33.0),
    ("sponsor", 33.0, 37.6),
    ("end", 37.6, 43.0),
]

_font_cache = {}
_tile_cache = {}
_img_cache = {}


def font(size, bold=True):
    key = (size, bold)
    if key not in _font_cache:
        _font_cache[key] = ImageFont.truetype(F_BOLD if bold else F_REG, size)
    return _font_cache[key]


def text_tile(text, size, color, bold=True, tracking=0):
    key = (text, size, color, bold, tracking)
    if key in _tile_cache:
        return _tile_cache[key]
    f = font(size, bold)
    probe = ImageDraw.Draw(Image.new("RGBA", (8, 8)))
    if tracking:
        widths = [probe.textlength(ch, font=f) for ch in text]
        wid = int(sum(widths) + tracking * (len(text) - 1)) + 8
    else:
        wid = int(probe.textlength(text, font=f)) + 8
    asc, desc = f.getmetrics()
    hgt = asc + desc + 8
    tile = Image.new("RGBA", (max(1, wid), hgt), (0, 0, 0, 0))
    d = ImageDraw.Draw(tile)
    if tracking:
        x = 4
        for ch, wch in zip(text, widths):
            d.text((x, 4), ch, font=f, fill=color)
            x += wch + tracking
    else:
        d.text((4, 4), text, font=f, fill=color)
    _tile_cache[key] = tile
    return tile


def src(name):
    key = name
    if key not in _img_cache:
        _img_cache[key] = Image.open(os.path.join(ASSETS, name)).convert("RGBA")
    return _img_cache[key]


def rounded(size, radius):
    m = Image.new("L", size, 0)
    ImageDraw.Draw(m).rounded_rectangle([0, 0, size[0] - 1, size[1] - 1], radius=radius, fill=255)
    return m


def card(name, hgt, radius=18, shadow=26):
    """把截图裁成圆角卡片（保持原始宽高比），带柔和投影"""
    key = ("card", name, hgt, radius, shadow)
    if key in _img_cache:
        return _img_cache[key]
    im = src(name)
    wdt = int(round(hgt * im.width / im.height))
    im = im.resize((wdt, hgt), Image.LANCZOS)
    mask = rounded((wdt, hgt), radius)
    body = Image.new("RGBA", (wdt, hgt), (0, 0, 0, 0))
    body.paste(im, (0, 0), mask)
    # 细边框
    ImageDraw.Draw(body).rounded_rectangle(
        [0, 0, wdt - 1, hgt - 1], radius=radius, outline=(255, 255, 255, 46), width=1)
    pad = shadow
    out = Image.new("RGBA", (wdt + pad * 2, hgt + pad * 2), (0, 0, 0, 0))
    sh = Image.new("RGBA", out.size, (0, 0, 0, 0))
    ImageDraw.Draw(sh).rounded_rectangle(
        [pad, pad + 6, pad + wdt, pad + 6 + hgt], radius=radius, fill=(0, 8, 20, 150))
    sh = sh.filter(ImageFilter.GaussianBlur(shadow * 0.55))
    out = Image.alpha_composite(out, sh)
    out.paste(body, (pad, pad), body)
    _img_cache[key] = out
    return out


def bg_base():
    if "bg" in _img_cache:
        return _img_cache["bg"]
    yy, xx = np.mgrid[0:H, 0:W].astype(np.float32)
    u = xx / (W - 1)
    v = yy / (H - 1)
    top = np.array([12, 20, 34], float)
    bot = np.array([4, 7, 13], float)
    img = top[None, None, :] + (bot - top)[None, None, :] * (v ** 0.85)[:, :, None]
    glow = np.exp(-(((u - 0.5) ** 2) / 0.16 + ((v - 0.34) ** 2) / 0.20))
    acc = np.array([16, 92, 140], float)
    img += (acc - img) * (glow * 0.55)[:, :, None]
    glow2 = np.exp(-(((u - 0.86) ** 2) / 0.06 + ((v - 0.9) ** 2) / 0.08))
    vio = np.array([58, 40, 116], float)
    img += (vio - img) * (glow2 * 0.35)[:, :, None]
    # 细网格
    gx = ((xx % 80) < 1).astype(np.float32)
    gy = ((yy % 80) < 1).astype(np.float32)
    grid = np.clip(gx + gy, 0, 1) * 9.0
    img += grid[:, :, None]
    # 暗角
    vig = 1.0 - 0.55 * (((u - 0.5) ** 2 * 1.6 + (v - 0.5) ** 2 * 1.2))
    img *= np.clip(vig, 0.35, 1.0)[:, :, None]
    out = Image.fromarray(np.clip(img, 0, 255).astype(np.uint8), "RGB").convert("RGBA")
    _img_cache["bg"] = out
    return out


def blit(base, tile, pos, alpha=1.0):
    if alpha <= 0.004 or tile is None:
        return
    x, y = int(pos[0]), int(pos[1])
    if alpha >= 0.996:
        base.paste(tile, (x, y), tile)
        return
    m = tile.getchannel("A").point(lambda v: int(v * alpha))
    base.paste(tile, (x, y), m)


def blit_c(base, tile, cx, cy, alpha=1.0):
    blit(base, tile, (cx - tile.width // 2, cy - tile.height // 2), alpha)


def ease_out(p):
    return 1 - (1 - p) ** 3


def smooth(p):
    p = min(max(p, 0.0), 1.0)
    return p * p * (3 - 2 * p)


def appear(p, t0, dur=0.55):
    """从 t0 开始，用 dur 秒完成 0→1 的柔和出现"""
    return smooth((p - t0) / dur) if p > t0 else 0.0


def pill(text, size=30, fg=C_WHITE, bg=(42, 216, 255, 30), line=(42, 216, 255, 110), pad=(26, 12)):
    key = ("pill", text, size, fg, bg, line, pad)
    if key in _tile_cache:
        return _tile_cache[key]
    t = text_tile(text, size, fg)
    wid = t.width + pad[0] * 2 - 8
    hgt = t.height + pad[1] * 2 - 8
    out = Image.new("RGBA", (wid, hgt), (0, 0, 0, 0))
    d = ImageDraw.Draw(out)
    d.rounded_rectangle([0, 0, wid - 1, hgt - 1], radius=hgt // 2, fill=bg, outline=line, width=1)
    out.paste(t, (pad[0] - 4, pad[1] - 4), t)
    _tile_cache[key] = out
    return out


# ── 各场景 ────────────────────────────────────────────────────────────────

def sc_intro(c, p, t):
    ic = src("icon.png")
    s = 0.92 + 0.08 * ease_out(min(1, p / 0.9))
    side = int(560 * s)
    ico = ic.resize((side, side), Image.LANCZOS)
    ic_a = appear(p, 0.0, 0.9)
    blit_c(c, ico, W // 2, 300, 0.30 * ic_a)

    a = appear(p, 0.06, 0.40)
    ty = 520 - 44 * (1 - ease_out(min(1, max(0.0, (p - 0.06) / 0.45))))
    tt = text_tile("模匠", 168, C_WHITE, tracking=18)
    blit_c(c, tt, W // 2, int(ty), a)

    a2 = appear(p, 0.26, 0.42)
    t2 = text_tile("VRChat 改模指南", 52, (200, 232, 248), tracking=6)
    blit_c(c, t2, W // 2, int(650 + 26 * (1 - ease_out(min(1, max(0.0, (p - 0.26) / 0.45))))), a2)

    a3 = appear(p, 0.46, 0.45)
    grow = int(240 * ease_out(min(1, max(0.0, (p - 0.46) / 0.45))))
    if grow > 4:
        bar = Image.new("RGBA", (grow, 6), (0, 0, 0, 0))
        d = ImageDraw.Draw(bar)
        for x in range(grow):
            k = x / max(1, grow - 1)
            col = (int(42 + (124 - 42) * k), int(216 + (92 - 216) * k), 255, int(230 * a3))
            d.line([(x, 0), (x, 6)], fill=col)
        blit_c(c, bar, W // 2, 712, 1.0)

    a4 = appear(p, 0.64, 0.45)
    t4 = text_tile("16 章 · 124 条 · 完全离线 · Windows", 34, C_SUB, bold=False, tracking=3)
    blit_c(c, t4, W // 2, 796, a4 * 0.95)


def sc_main(c, p, t):
    # 左文右图
    a = appear(p, 0.0, 0.6)
    t1 = text_tile("一套完整的改模链路", 76, C_WHITE)
    blit(c, t1, (120 + 40 * (1 - ease_out(min(1, p / 0.7))), 300), a)

    a2 = appear(p, 0.18, 0.6)
    lines = ["从 Unity 环境、模型导入、材质贴图", "到参数菜单、上传与性能优化", "每一步都写清为什么，以及哪里会翻车"]
    for i, ln in enumerate(lines):
        tt = text_tile(ln, 36, C_SUB, bold=False)
        blit(c, tt, (126, 416 + i * 62), a2)

    a3 = appear(p, 0.42, 0.7)
    blit(c, pill("16 章 / 124 条 / 6 张图例", 30), (126, 640), a3)

    # 右侧主界面：缓慢推近
    zoom = 1.0 + 0.05 * smooth(p)
    card_h = int(560 * zoom)
    cd = card("main.png", card_h)
    cd = cd.resize((int(cd.width * 1.0), int(cd.height * 1.0)), Image.LANCZOS)
    ax = appear(p, 0.02, 0.22)
    x = int(902 + 70 * (1 - ease_out(min(1, max(0.0, (p - 0.02) / 0.26)))))
    blit(c, cd, (x, int(H // 2 - cd.height // 2 + 20)), ax)


def sc_switch(c, p, t):
    a = appear(p, 0.0, 0.6)
    t1 = text_tile("三个开关，随时切换", 74, C_WHITE)
    blit_c(c, t1, W // 2, 190, a)

    a2 = appear(p, 0.12, 0.6)
    t2 = text_tile("顶部一排按钮，改完立刻看到效果", 34, C_SUB, bold=False)
    blit_c(c, t2, W // 2, 262, a2)

    items = [
        ("hot.png", "重点", "人工标注 256 处要点\n开一下就全亮起来"),
        ("english.png", "Unity 界面", "中文 / English / 双语\n菜单路径同步变化"),
        ("classic.png", "改模方式", "MA 插件 / 古法\n两套不同写法"),
    ]
    hgt = 330
    gap = 62
    cards = [card(n, hgt) for n, _, _ in items]
    total = sum(cc.width for cc in cards) + gap * (len(cards) - 1)
    x = (W - total) // 2
    for i, ((nm, label, desc), cc) in enumerate(zip(items, cards)):
        st = 0.04 + i * 0.07
        aa = appear(p, st, 0.30)
        dy = int(70 * (1 - ease_out(min(1, max(0.0, (p - st) / 0.55)))))
        y = 470 + dy
        blit(c, cc, (x, y), aa)
        lb = text_tile(label, 40, C_ACC)
        blit(c, lb, (x + 10, y + cc.height + 6), aa)
        for j, dl in enumerate(desc.split("\n")):
            dt = text_tile(dl, 26, C_SUB, bold=False)
            blit(c, dt, (x + 10, y + cc.height + 58 + j * 36), aa * 0.95)
        x += cc.width + gap


def sc_content(c, p, t):
    a = appear(p, 0.0, 0.6)
    big = text_tile("16 章 124 条", 96, C_WHITE, tracking=4)
    blit_c(c, big, W // 2, 200, a)

    a2 = appear(p, 0.15, 0.6)
    t2 = text_tile("其中 60 篇来自 dwgx「VRC 改模资源中心」的深度指南", 36, C_SUB, bold=False)
    blit_c(c, t2, W // 2, 296, a2)

    # 三张卡片扇形排开
    spec = [("ch11.png", 400, -430, 0.06), ("ch13.png", 470, 0, 0.02), ("checklist.png", 400, 430, 0.09)]
    for nm, hgt, dx, st in spec:
        aa = appear(p, st, 0.30)
        dy = int(120 * (1 - ease_out(min(1, max(0.0, (p - st) / 0.35)))))
        cc = card(nm, hgt)
        blit_c(c, cc, W // 2 + dx, 690 + dy, aa)

    a3 = appear(p, 0.9, 0.7)
    blit_c(c, pill("11–15 章：精选指南与资源导航", 28), W // 2, 980, a3)


def sc_splash(c, p, t):
    a = appear(p, 0.0, 0.6)
    t1 = text_tile("矢量启动动画", 74, C_WHITE)
    blit(c, t1, (120, 250), a)

    a2 = appear(p, 0.14, 0.6)
    lines = ["透明背景，画面上只有 VRChat 图标", "轮廓从左到右画出 → 填充与字标浮现 → 淡出", "约 1.75 秒，点击 / 任意键即可跳过"]
    for i, ln in enumerate(lines):
        tt = text_tile(ln, 34, C_SUB, bold=False)
        blit(c, tt, (126, 356 + i * 58), a2)

    a3 = appear(p, 0.34, 0.7)
    blit(c, pill("--nosplash 可完全跳过", 28), (126, 566), a3)

    # 三个阶段缩略图
    frames = [("splash0.png", "0.3s 描边"), ("splash1.png", "0.8s 填充"), ("splash2.png", "1.6s 完成")]
    x = 126
    for i, (nm, cap) in enumerate(frames):
        st = 0.12 + i * 0.06
        aa = appear(p, st, 0.32)
        im = src(nm)
        hh = 200
        thumb = im.resize((int(im.width * hh / im.height), hh), Image.LANCZOS)
        frame = Image.new("RGBA", (thumb.width + 28, thumb.height + 28), (0, 0, 0, 0))
        ImageDraw.Draw(frame).rounded_rectangle(
            [0, 0, frame.width - 1, frame.height - 1], radius=14,
            fill=(14, 24, 40, 210), outline=(255, 255, 255, 34), width=1)
        frame.paste(thumb, (14, 14), thumb)
        blit(c, frame, (x, 660), aa)
        ct = text_tile(cap, 24, C_DIM, bold=False)
        blit(c, ct, (x + 4, 660 + frame.height + 6), aa)
        x += frame.width + 34

    # 右侧：模拟浅色桌面，证明背景是透明的
    a4 = appear(p, 0.04, 0.30)
    pw, ph = 900, 470
    panel = Image.new("RGBA", (pw, ph), (0, 0, 0, 0))
    d = ImageDraw.Draw(panel)
    for yy in range(ph):
        k = yy / (ph - 1)
        d.line([(0, yy), (pw, yy)], fill=(int(226 - 26 * k), int(234 - 22 * k), int(246 - 14 * k), 255))
    pm = rounded((pw, ph), 22)
    panel.putalpha(pm)
    ImageDraw.Draw(panel).rounded_rectangle([0, 0, pw - 1, ph - 1], radius=22,
                                            outline=(255, 255, 255, 160), width=2)
    blit_c(c, panel, 1440, 500, a4)
    im = src("splash2.png")
    hh = 430
    big = im.resize((int(im.width * hh / im.height), hh), Image.LANCZOS)
    blit_c(c, big, 1440, 500, appear(p, 0.10, 0.32))
    cap = text_tile("透明背景 · 只有图标", 24, (70, 92, 120), bold=False)
    blit_c(c, cap, 1440, 500 + ph // 2 + 26, a4 * 0.9)


def sc_offline(c, p, t):
    a = appear(p, 0.0, 0.6)
    t1 = text_tile("离线可用 · 安装不联网", 74, C_WHITE)
    blit_c(c, t1, W // 2, 180, a)

    a2 = appear(p, 0.14, 0.6)
    t2 = text_tile("内置 VCC 中文镜像与上传前检查清单", 34, C_SUB, bold=False)
    blit_c(c, t2, W // 2, 252, a2)

    spec = [("web.png", "VCC 中文镜像 · 内嵌浏览器", -450, 0.03), ("checklist.png", "上传前检查清单 · 进度自动保存", 450, 0.09)]
    for nm, cap, dx, st in spec:
        aa = appear(p, st, 0.30)
        cc = card(nm, 470)
        dy = int(90 * (1 - ease_out(min(1, max(0.0, (p - st) / 0.7)))))
        blit_c(c, cc, W // 2 + dx, 620 + dy, aa)
        ct = text_tile(cap, 30, C_SUB, bold=False)
        blit_c(c, ct, W // 2 + dx, 900 + dy, aa * 0.95)


def sc_sponsor(c, p, t):
    a = appear(p, 0.0, 0.6)
    t1 = text_tile("喜欢的话，可以请我喝一杯", 68, C_WHITE)
    blit(c, t1, (120, 300), a)

    a2 = appear(p, 0.16, 0.6)
    for i, ln in enumerate(["顶部「♥ 赞助作者」按钮 / 第 16 章", "爱发电主页，微信支付宝都行"]):
        tt = text_tile(ln, 34, C_SUB, bold=False)
        blit(c, tt, (126, 410 + i * 58), a2)

    a3 = appear(p, 0.36, 0.7)
    blit(c, pill("afdian.com/a/7891mek", 34, fg=(255, 226, 236),
                 bg=(255, 128, 160, 26), line=(255, 128, 160, 120)), (126, 560), a3)

    a4 = appear(p, 0.56, 0.7)
    t4 = text_tile("完全自愿 · 不弹窗 · 不影响任何功能", 30, C_DIM, bold=False)
    blit(c, t4, (126, 650), a4)

    a5 = appear(p, 0.03, 0.30)
    cc = card("sponsor.png", 520)
    blit(c, cc, (int(978 + 80 * (1 - ease_out(min(1, max(0.0, (p - 0.08) / 0.6))))),
                 int(H // 2 - cc.height // 2 + 30)), a5)


def sc_end(c, p, t):
    ic = src("icon.png")
    side = int(230 * (0.94 + 0.06 * ease_out(min(1, p / 0.8))))
    ico = ic.resize((side, side), Image.LANCZOS)
    a = appear(p, 0.0, 0.8)
    blit_c(c, ico, W // 2, 296, a)

    a2 = appear(p, 0.10, 0.42)
    tt = text_tile("模匠", 132, C_WHITE, tracking=14)
    blit_c(c, tt, W // 2, 520, a2)

    a3 = appear(p, 0.22, 0.42)
    t3 = text_tile("VRChat 改模指南", 46, (200, 232, 248), tracking=4)
    blit_c(c, t3, W // 2, 620, a3)

    a4 = appear(p, 0.36, 0.42)
    p1 = pill("Windows 10 / 11 · 免安装绿色版", 30)
    p2 = pill("爱发电 afdian.com/a/7891mek", 30, fg=(255, 226, 236),
              bg=(255, 128, 160, 26), line=(255, 128, 160, 120))
    x = (W - (p1.width + p2.width + 28)) // 2
    blit(c, p1, (x, 706), a4)
    blit(c, p2, (x + p1.width + 28, 706), a4)

    a5 = appear(p, 0.54, 0.45)
    t5 = text_tile("教程整理自 VRChat 官方文档 · Modular Avatar · dwgx 资源中心", 26, C_DIM, bold=False)
    blit_c(c, t5, W // 2, 826, a5)


RENDER = {
    "intro": sc_intro, "main": sc_main, "switch": sc_switch, "content": sc_content,
    "splash": sc_splash, "offline": sc_offline, "sponsor": sc_sponsor, "end": sc_end,
}


def compose(t):
    canvas = bg_base().copy()
    for name, t0, t1 in SCENES:
        if t0 <= t < t1:
            p = (t - t0) / (t1 - t0)
            local = t - t0
            fade_in = min(1.0, local / 0.45)
            fade_out = min(1.0, (t1 - t) / 0.45)
            alpha = min(fade_in, fade_out)
            layer = Image.new("RGBA", (W, H), (0, 0, 0, 0))
            RENDER[name](layer, p, t)
            blit(canvas, layer, (0, 0), alpha)
            break

    # 常驻角标
    if t > 5.0:
        a = min(1.0, (t - 5.0) / 0.6)
        ico = src("icon.png").resize((40, 40), Image.LANCZOS)
        blit(canvas, ico, (56, H - 92), a * 0.9)
        nm = text_tile("模匠 · VRChat 改模指南", 26, C_DIM, bold=False)
        blit(canvas, nm, (108, H - 84), a * 0.9)

    # 底部进度线
    done = int((W - 120) * (t / DUR))
    if done > 0:
        bar = Image.new("RGBA", (done, 4), (0, 0, 0, 0))
        d = ImageDraw.Draw(bar)
        for x in range(done):
            k = x / max(1, W - 120)
            d.line([(x, 0), (x, 4)], fill=(int(42 + (124 - 42) * k), int(216 + (92 - 216) * k), 255, 200))
        blit(canvas, bar, (60, H - 34), 1.0)
    return canvas.convert("RGB")


def main():
    os.makedirs(FRAME_DIR, exist_ok=True)
    for f in os.listdir(FRAME_DIR):
        fp = os.path.join(FRAME_DIR, f)
        if os.path.isfile(fp):
            os.remove(fp)

    import imageio_ffmpeg
    ff = imageio_ffmpeg.get_ffmpeg_exe()
    total = int(DUR * FPS)
    cmd = [
        ff, "-y",
        "-f", "rawvideo", "-pix_fmt", "rgb24", "-s", f"{W}x{H}", "-r", str(FPS), "-i", "pipe:0",
        "-i", AUDIO,
        "-c:v", "libx264", "-preset", "medium", "-crf", "19",
        "-pix_fmt", "yuv420p", "-profile:v", "high", "-level", "4.1",
        "-c:a", "aac", "-b:a", "192k", "-ar", "44100",
        "-shortest", "-movflags", "+faststart",
        OUT,
    ]
    proc = subprocess.Popen(cmd, stdin=subprocess.PIPE, stdout=subprocess.DEVNULL, stderr=subprocess.PIPE)
    marks = {int(round(s * FPS)): name for name, s, _ in
             [(n, (t0 + t1) / 2, 0) for n, t0, t1 in SCENES]}
    try:
        for i in range(total):
            t = i / FPS
            frame = compose(t)
            if i in marks:
                frame.save(os.path.join(FRAME_DIR, "scene_%s.png" % marks[i]))
            proc.stdin.write(frame.tobytes())
            if i % 150 == 0:
                print("  frame %d/%d" % (i, total), flush=True)
    finally:
        proc.stdin.close()
        err = proc.stderr.read().decode("utf-8", "ignore")
        proc.wait()
    if proc.returncode != 0:
        print("ffmpeg failed:\n", err[-3000:])
        sys.exit(1)
    size = os.path.getsize(OUT) / 1024 / 1024
    print("video:", OUT, "%.1f MB" % size)


if __name__ == "__main__":
    main()
