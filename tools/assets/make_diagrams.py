# -*- coding: utf-8 -*-
"""Generate tutorial diagram PNGs (clean vector-ish style) for the WPF app."""
import os, math
from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
OUT = os.path.join(ROOT, "src", "MoJiang", "Assets")
os.makedirs(OUT, exist_ok=True)

S = 2  # supersample factor
BG = (247, 249, 252)
INK = (27, 36, 48)
SUB = (90, 105, 125)
LINE = (196, 208, 222)
BLUE = (31, 78, 121)
BLUE2 = (46, 116, 181)
LIGHT = (232, 240, 250)
LIGHT2 = (238, 246, 240)
WARN = (198, 106, 44)
WARNBG = (253, 240, 230)
GREEN = (56, 142, 92)
GREENBG = (232, 245, 236)

F_TITLE = r"C:\Windows\Fonts\msyhbd.ttc"
F_BODY = r"C:\Windows\Fonts\msyh.ttc"


def f(path, size):
    return ImageFont.truetype(path, int(size * S))


class C:
    def __init__(self, w, h):
        self.w, self.h = w, h
        self.im = Image.new("RGB", (w * S, h * S), BG)
        self.d = ImageDraw.Draw(self.im)

    def rr(self, x, y, w, h, fill=(255, 255, 255), outline=LINE, r=12, width=2):
        self.d.rounded_rectangle([x * S, y * S, (x + w) * S, (y + h) * S],
                                 radius=r * S, fill=fill, outline=outline, width=int(width * S))

    def rect(self, x, y, w, h, fill=(255, 255, 255), outline=LINE, width=2):
        self.d.rectangle([x * S, y * S, (x + w) * S, (y + h) * S], fill=fill,
                         outline=outline, width=int(width * S))

    def txt(self, x, y, t, font, fill=INK, anchor="la"):
        self.d.text((x * S, y * S), t, font=font, fill=fill, anchor=anchor)
        b = self.d.textbbox((x * S, y * S), t, font=font, anchor=anchor)
        if b[2] / S > self.w - 6 or b[3] / S > self.h - 4:
            print("  OVERFLOW:", t[:40], "right=", round(b[2] / S), "bottom=", round(b[3] / S))

    def ctext(self, cx, cy, t, font, fill=INK):
        self.d.text((cx * S, cy * S), t, font=font, fill=fill, anchor="mm")

    def tw(self, t, font):
        b = self.d.textbbox((0, 0), t, font=font)
        return (b[2] - b[0]) / S

    def wrap(self, t, font, maxw):
        lines, cur = [], ""
        for ch in t:
            if ch == "\n":
                lines.append(cur); cur = ""; continue
            if self.tw(cur + ch, font) <= maxw:
                cur += ch
            else:
                lines.append(cur); cur = ch
        lines.append(cur)
        return lines

    def para(self, x, y, maxw, t, font, fill=INK, lh=None):
        lh = lh or (font.size / S * 1.5)
        for i, ln in enumerate(self.wrap(t, font, maxw)):
            self.txt(x, y + i * lh, ln, font, fill)
        return y + len(self.wrap(t, font, maxw)) * lh

    def arrow(self, x1, y1, x2, y2, color=SUB, width=2.5, head=9):
        self.d.line([x1 * S, y1 * S, x2 * S, y2 * S], fill=color, width=int(width * S))
        ang = math.atan2(y2 - y1, x2 - x1)
        for sgn in (1, -1):
            a = ang + math.pi + sgn * 0.42
            px = x2 - head * math.cos(ang + sgn * 0.42)
            py = y2 - head * math.sin(ang + sgn * 0.42)
            self.d.polygon([(x2 * S, y2 * S), (px * S, py * S),
                            ((x2 - head * 0.75 * math.cos(ang)) * S,
                             (y2 - head * 0.75 * math.sin(ang)) * S)], fill=color)

    def save(self, name):
        self.im = self.im.resize((self.w, self.h), Image.LANCZOS)
        p = os.path.join(OUT, name)
        self.im.save(p, "PNG", optimize=True)
        print("saved", p, os.path.getsize(p) // 1024, "KB")


def header(c, title, subtitle):
    c.txt(60, 44, title, f(F_TITLE, 34), BLUE)
    c.txt(60, 96, subtitle, f(F_BODY, 18), SUB)
    c.d.line([60 * S, 138 * S, (c.w - 60) * S, 138 * S], fill=LINE, width=int(1.5 * S))


# ── 1. 总流程 ────────────────────────────────────────────────────────────────
def flow():
    c = C(1600, 760)
    header(c, "改模总流程", "任何一步没做完就不要往后走，否则排错成本翻倍")
    steps = [
        ("① 选模型", "BOOTH / Gumroad\nVRoid / 自制"),
        ("② 导入 Unity", "fbx + 贴图\nScale=1  Read/Write✔"),
        ("③ 适配 Rig", "Humanoid 骨骼映射\nEnforce T-Pose"),
        ("④ 材质", "复制材质 → lilToon\n挂贴图"),
        ("⑤ Descriptor", "视角 / 口型 / 眼睛\nPhysBone"),
        ("⑥ 改模", "换装 / 改色\n改体型 / 加配件"),
        ("⑦ 优化", "面数 / 材质槽\nPhysBone 收敛"),
        ("⑧ 上传", "Build & Test\n→ Publish"),
    ]
    bw, gap = 168, 28
    x0, y0, bh = 60, 210, 150
    for i, (t, sub) in enumerate(steps):
        x = x0 + (i % 4) * (bw + gap)
        y = y0 + (i // 4) * 210
        c.rr(x, y, bw, bh, fill=(255, 255, 255), outline=BLUE2, r=14, width=2)
        c.rect(x, y, bw, 34, fill=BLUE, outline=BLUE, width=0)
        c.d.rounded_rectangle([x * S, y * S, (x + bw) * S, (y + 34) * S], radius=14 * S, fill=BLUE)
        c.d.rectangle([x * S, (y + 20) * S, (x + bw) * S, (y + 34) * S], fill=BLUE)
        c.ctext(x + bw / 2, y + 17, t, f(F_TITLE, 19), (255, 255, 255))
        c.para(x + 12, y + 48, bw - 24, sub, f(F_BODY, 15), SUB, lh=24)
        if i % 4 != 3:
            c.arrow(x + bw + 4, y + bh / 2, x + bw + gap - 4, y + bh / 2)
    # 第二行回流箭头
    c.d.line([(x0 + 3 * bw + 2 * gap + bw / 2) * S, (y0 + bh + 6) * S,
              (x0 + 3 * bw + 2 * gap + bw / 2) * S, (y0 + 168) * S], fill=SUB, width=int(2.5 * S))
    c.arrow(x0 + 60, y0 + 210 + 75 - 300 + 300, x0 + 60, y0 + 210 + 75)
    c.rr(60, 596, 1480, 96, fill=LIGHT, outline=BLUE2, r=12, width=2)
    c.txt(84, 616, "记忆口诀", f(F_TITLE, 18), BLUE)
    c.txt(200, 616, "导入 → Rig → 材质 → 组件 → 菜单 → 优化 → 测试 → 上传。每改一处立即 Build & Test，一次只改一个变量。",
          f(F_BODY, 17), INK)
    c.txt(200, 650, "改崩了就回滚上一个能跑的备份，别在现场修 3 小时。", f(F_BODY, 16), SUB)
    c.save("flow.png")


# ── 2. 层级结构 ─────────────────────────────────────────────────────────────
def hierarchy():
    c = C(1600, 820)
    header(c, "Avatar 物体层级结构", "改模时 90% 的“找不到东西”都出在层级")
    c.rr(60, 180, 700, 560, fill=(255, 255, 255), outline=BLUE, r=14, width=2.5)
    c.rect(60, 180, 700, 40, fill=BLUE, outline=BLUE, width=0)
    c.d.rounded_rectangle([60 * S, 180 * S, 760 * S, 220 * S], radius=14 * S, fill=BLUE)
    c.d.rectangle([60 * S, 202 * S, 760 * S, 220 * S], fill=BLUE)
    c.txt(76, 199, "Avatar Root   (VRC Avatar Descriptor)", f(F_TITLE, 18), (255, 255, 255))
    rows = [
        ("Armature", "骨骼树：Hips / Spine / Head / Hand…", BLUE2),
        ("├─ Hips", "PhysBone 的 Root 不要用人形骨骼", WARN),
        ("└─ Hair / Tail 骨骼", "挂 VRC PhysBone（Endpoint Position 必填）", GREEN),
        ("Body (SkinnedMeshRenderer)", "网格 + 材质 + BlendShape 表情", BLUE2),
        ("Clothes (MA Merge Armature)", "衣服骨骼合并到 Avatar", BLUE2),
        ("Accessories (MA Object Toggle)", "菜单开关控制显隐", BLUE2),
    ]
    y = 250
    for name, note, col in rows:
        c.rr(84, y, 652, 62, fill=(252, 253, 255), outline=LINE, r=8, width=1.5)
        c.d.rectangle([84 * S, (y + 10) * S, 90 * S, (y + 52) * S], fill=col)
        c.txt(104, y + 10, name, f(F_TITLE, 16), INK)
        c.txt(104, y + 34, note, f(F_BODY, 14), SUB)
        y += 74

    c.rr(820, 180, 720, 250, fill=WARNBG, outline=WARN, r=12, width=2)
    c.txt(844, 200, "❌ 常见错误", f(F_TITLE, 18), WARN)
    c.para(844, 236, 672, "1. 用父级空物体缩放来“改身高” → 视角、碰撞体、PhysBone 一起错位", f(F_BODY, 15), INK, lh=26)
    c.para(844, 268, 672, "2. 把衣服 Prefab Unpack 后再改 → 作者更新时无法整包替换", f(F_BODY, 15), INK, lh=26)
    c.para(844, 300, 672, "3. 人形骨骼（Head/Hips）当 PhysBone Root → 物理直接崩", f(F_BODY, 15), INK, lh=26)
    c.para(844, 332, 672, "4. 禁用物体也算性能 → 删不掉的组件记得真删", f(F_BODY, 15), INK, lh=26)

    c.rr(820, 456, 720, 284, fill=GREENBG, outline=GREEN, r=12, width=2)
    c.txt(844, 476, "✅ 推荐层级（下划线开头排最前）", f(F_TITLE, 18), GREEN)
    tree = [
        "Assets/_Project/00_Base        基础模型 + 原贴图",
        "Assets/_Project/01_Outfits     每套衣服一个子文件夹",
        "Assets/_Project/03_Materials   自己复制出来的材质",
        "Assets/_Project/05_Controllers FX / Menu / Parameters",
        "Assets/_Project/06_Prefabs     组装好的最终 Prefab",
    ]
    yy = 514
    for t in tree:
        c.txt(852, yy, t, f(F_BODY, 14), (40, 60, 50))
        yy += 42
    c.save("hierarchy.png")


# ── 3. 换装合并原理 ─────────────────────────────────────────────────────────
def merge():
    c = C(1600, 780)
    header(c, "换装原理：骨骼合并（Merge Armature）", "衣服不是“贴”上去的，而是把骨骼并进 Avatar 的骨架上")
    # left: before
    c.txt(70, 176, "合并前", f(F_TITLE, 18), SUB)
    c.rr(60, 208, 430, 470, fill=(255, 255, 255), outline=LINE, r=12, width=2)
    c.txt(80, 224, "Avatar_Prefab", f(F_TITLE, 15), INK)
    c.rr(80, 258, 390, 180, fill=LIGHT, outline=BLUE2, r=10, width=1.5)
    c.txt(96, 272, "Armature（Avatar 骨骼）", f(F_BODY, 14), BLUE)
    for i, b in enumerate(["Hips", "Spine", "Head", "UpperArm.L"]):
        c.rr(96 + (i % 2) * 180, 300 + (i // 2) * 56, 168, 44, fill=(255, 255, 255), outline=LINE, r=8, width=1)
        c.ctext(96 + (i % 2) * 180 + 84, 322 + (i // 2) * 56, b, f(F_BODY, 14), INK)
    c.rr(80, 452, 390, 200, fill=(255, 253, 250), outline=WARN, r=10, width=1.5)
    c.txt(96, 466, "Outfit_Prefab（独立骨骼）", f(F_BODY, 14), WARN)
    for i, b in enumerate(["Hips", "Spine", "Head", "Skirt_01", "Skirt_02"]):
        c.rr(96 + (i % 2) * 180, 494 + (i // 2) * 50, 168, 40, fill=(255, 255, 255), outline=LINE, r=8, width=1)
        c.ctext(96 + (i % 2) * 180 + 84, 514 + (i // 2) * 50, b, f(F_BODY, 13), INK)

    c.arrow(510, 440, 610, 440, BLUE2, 3, 11)
    c.txt(512, 404, "Setup Outfit", f(F_TITLE, 15), BLUE2)
    c.txt(512, 462, "按骨骼名匹配", f(F_BODY, 13), SUB)

    # right: after
    c.txt(660, 176, "合并后（Play 模式 / 上传时）", f(F_TITLE, 18), GREEN)
    c.rr(650, 208, 430, 470, fill=(255, 255, 255), outline=LINE, r=12, width=2)
    c.txt(670, 224, "Avatar_Prefab", f(F_TITLE, 15), INK)
    c.rr(670, 258, 390, 250, fill=GREENBG, outline=GREEN, r=10, width=1.5)
    c.txt(686, 272, "Armature（合并后）", f(F_BODY, 14), GREEN)
    for i, b in enumerate(["Hips ✔共用", "Spine ✔共用", "Head ✔共用", "Skirt_01 ◇新增", "Skirt_02 ◇新增"]):
        c.rr(686 + (i % 2) * 180, 300 + (i // 2) * 56, 168, 44, fill=(255, 255, 255), outline=LINE, r=8, width=1)
        c.ctext(686 + (i % 2) * 180 + 84, 322 + (i // 2) * 56, b, f(F_BODY, 12), INK)
    c.rr(670, 526, 390, 126, fill=(252, 253, 255), outline=LINE, r=10, width=1.5)
    c.txt(686, 540, "衣服网格 Skinned Mesh", f(F_BODY, 13), SUB)
    c.txt(686, 564, "Bones 引用 → Avatar 骨骼", f(F_BODY, 13), INK)
    c.txt(686, 588, "独有骨骼保留自己的 PhysBone", f(F_BODY, 13), INK)
    c.txt(686, 612, "换装 = 关掉/打开物体，不是换模型", f(F_BODY, 13), SUB)

    # right column notes
    c.rr(1130, 208, 400, 214, fill=LIGHT, outline=BLUE2, r=12, width=2)
    c.txt(1152, 226, "三条路线怎么选", f(F_TITLE, 17), BLUE)
    for i, t in enumerate(["① Modular Avatar（首选）", "② VRCFury（可逆，自动 WD）", "③ 手动重绑（理解原理）"]):
        c.txt(1152, 262 + i * 34, t, f(F_BODY, 15), INK)
    c.rr(1130, 446, 400, 232, fill=WARNBG, outline=WARN, r=12, width=2)
    c.txt(1152, 464, "⚠ 合并失败三大原因", f(F_TITLE, 17), WARN)
    for i, t in enumerate(["骨骼名不一致（J_Bip_C_Hips vs Hips）", "衣服基于另一体型（Scale 不同）",
                           "没点 Setup Outfit / Target 指错"]):
        c.para(1152, 500 + i * 52, 360, "· " + t, f(F_BODY, 14), INK, lh=24)
    c.save("merge.png")


# ── 4. 参数系统 ─────────────────────────────────────────────────────────────
def params():
    c = C(1600, 700)
    header(c, "表情菜单 / 参数 / FX 层 的关系", "改模里所有“开关”都由这三者串起来")
    boxes = [
        (70, 210, 420, 150, "VRCExpressionsMenu", "游戏内圆形菜单，每页最多 8 项\n控件：Button / Toggle / SubMenu / Puppet", BLUE2, LIGHT),
        (590, 210, 420, 150, "VRCExpressionParameters", "参数表：名字 / 类型 / 默认值\nSaved：换世界保留   Synced：别人可见", BLUE2, LIGHT),
        (1110, 210, 420, 150, "FX Animator", "同名参数驱动动画\n控制物体显隐 / 材质 / BlendShape", BLUE2, LIGHT),
    ]
    for x, y, w, h, t, sub, col, bg in boxes:
        c.rr(x, y, w, h, fill=(255, 255, 255), outline=col, r=12, width=2)
        c.rect(x, y, w, 38, fill=col, outline=col, width=0)
        c.d.rounded_rectangle([x * S, y * S, (x + w) * S, (y + 38) * S], radius=12 * S, fill=col)
        c.d.rectangle([x * S, (y + 24) * S, (x + w) * S, (y + 38) * S], fill=col)
        c.txt(x + 16, y + 10, t, f(F_TITLE, 16), (255, 255, 255))
        c.para(x + 16, y + 54, w - 32, sub, f(F_BODY, 14), SUB, lh=26)

    c.arrow(492, 285, 586, 285, BLUE2, 3, 10)
    c.arrow(1012, 285, 1106, 285, BLUE2, 3, 10)
    c.txt(470, 246, "绑定参数名", f(F_BODY, 13), SUB)
    c.txt(1000, 246, "同名参数", f(F_BODY, 13), SUB)
    c.arrow(1310, 366, 1310, 470, GREEN, 3, 10)
    c.arrow(800, 366, 800, 470, GREEN, 3, 10)
    c.arrow(280, 366, 280, 470, GREEN, 3, 10)
    c.txt(1230, 404, "运行时状态", f(F_BODY, 13), GREEN)

    c.rr(70, 470, 420, 170, fill=GREENBG, outline=GREEN, r=12, width=2)
    c.txt(90, 486, "控件速查", f(F_TITLE, 16), GREEN)
    for i, t in enumerate(["Button：按一下复位（一次性动作）", "Toggle：开关（衣服 / 道具）",
                           "Radial：0~1 滑杆（颜色 / 幅度）"]):
        c.txt(90, 520 + i * 34, t, f(F_BODY, 14), INK)
    c.rr(590, 470, 420, 170, fill=LIGHT, outline=BLUE2, r=12, width=2)
    c.txt(610, 486, "同步预算 256 bit", f(F_TITLE, 16), BLUE)
    for i, t in enumerate(["Bool = 1 bit", "Int = 8 bit（0~255）", "Float = 8 bit（-1~1）"]):
        c.txt(610, 520 + i * 34, t, f(F_BODY, 14), INK)
    c.rr(1110, 470, 420, 170, fill=WARNBG, outline=WARN, r=12, width=2)
    c.txt(1130, 486, "省预算的写法", f(F_TITLE, 16), WARN)
    for i, t in enumerate(["多个 Bool → 合并成 1 个 Int", "非必要参数别勾 Synced", "换装参数一定要勾 Saved"]):
        c.txt(1130, 520 + i * 34, t, f(F_BODY, 14), INK)
    c.save("params.png")


# ── 5. PhysBone 链 ─────────────────────────────────────────────────────────
def physbone():
    c = C(1600, 760)
    header(c, "PhysBone 骨链设置", "头发 / 尾巴 / 耳朵不动，99% 是缺 Endpoint Position")
    # chain drawing
    pts = [(150, 300), (300, 360), (450, 430), (600, 500), (740, 560)]
    for i in range(len(pts) - 1):
        c.d.line([pts[i][0] * S, pts[i][1] * S, pts[i + 1][0] * S, pts[i + 1][1] * S],
                 fill=BLUE2, width=int(6 * S))
    for i, (x, y) in enumerate(pts):
        r = 14 if i == 0 else 11
        c.d.ellipse([(x - r) * S, (y - r) * S, (x + r) * S, (y + r) * S], fill=BLUE if i == 0 else (255, 255, 255),
                    outline=BLUE2, width=int(3 * S))
    # endpoint
    ex, ey = 740 + 120, 560 + 60
    c.d.line([pts[-1][0] * S, pts[-1][1] * S, ex * S, ey * S], fill=WARN, width=int(4 * S))
    c.d.ellipse([(ex - 9) * S, (ey - 9) * S, (ex + 9) * S, (ey + 9) * S], fill=WARN)
    c.txt(ex + 18, ey - 12, "Endpoint Position", f(F_TITLE, 15), WARN)
    c.txt(ex + 18, ey + 14, "（必须 > 0，通常 +Y 0.1）", f(F_BODY, 13), SUB)
    c.txt(120, 340, "Root Transform", f(F_TITLE, 15), BLUE)
    c.txt(120, 366, "头发根骨 / 尾巴第一根骨", f(F_BODY, 13), SUB)
    # limits cone
    cx, cy = 420, 300
    c.d.arc([(cx - 150) * S, (cy - 150) * S, (cx + 150) * S, (cy + 150) * S], 20, 160, fill=LINE, width=int(2 * S))
    c.d.line([cx * S, cy * S, (cx + 140) * S, (cy + 60) * S], fill=SUB, width=int(1.5 * S))
    c.d.line([cx * S, cy * S, (cx + 60) * S, (cy + 140) * S], fill=SUB, width=int(1.5 * S))
    c.txt(cx + 40, cy - 40, "Limits（优先用）", f(F_BODY, 13), SUB)

    # right panel settings table
    c.rr(900, 180, 640, 300, fill=(255, 255, 255), outline=BLUE2, r=12, width=2)
    c.txt(920, 196, "参数起点值（可直接抄）", f(F_TITLE, 17), BLUE)
    rows = [("Pull", "0.2", "回弹力度"), ("Spring", "0.8", "弹性"), ("Gravity", "0.2~0.4", "重力下垂"),
            ("Immobile", "0.3~0.6", "移动时抑制摆动（防晕）"), ("Radius", "0.02~0.05", "碰撞 / 抓取半径（米）"),
            ("Limits", "Angle 20~40°", "比碰撞体省性能")]
    y = 236
    for k, v, note in rows:
        c.txt(920, y, k, f(F_TITLE, 14), INK)
        c.txt(1090, y, v, f(F_BODY, 14), WARN)
        c.txt(1240, y, note, f(F_BODY, 13), SUB)
        y += 38

    c.rr(900, 500, 640, 220, fill=WARNBG, outline=WARN, r=12, width=2)
    c.txt(920, 516, "⚠ 四条硬规则", f(F_TITLE, 17), WARN)
    for i, t in enumerate(["单链 / 单层子骨骼：必须填 Endpoint Position",
                           "单个组件最多影响 256 个 transform",
                           "人形骨骼（Hips/Head/四肢）不能当 Root",
                           "同一物体不要同时放 Constraint 和 PhysBone"]):
        c.txt(920, 552 + i * 38, "· " + t, f(F_BODY, 14), INK)
    c.save("physbone.png")


# ── 6. 性能等级 ────────────────────────────────────────────────────────────
def ranks():
    c = C(1600, 740)
    header(c, "性能等级（PC / Quest）", "你的模型跑在别人的显卡上：目标 Good，底线 Medium")
    levels = [("Excellent", 32000, "32,000 面", GREEN, GREENBG),
              ("Good", 70000, "70,000 面", BLUE2, LIGHT),
              ("Medium", 70000, "70,000 面", BLUE, LIGHT),
              ("Poor", 70000, "70,000 面", WARN, WARNBG),
              ("Very Poor", 999999, "无上限 → 可能被屏蔽", (176, 58, 58), (253, 235, 235))]
    x = 70
    bw, gap = 270, 24
    for i, (name, tri, label, col, bg) in enumerate(levels):
        h = 300 - i * 34
        c.rr(x, 560 - h, bw, h, fill=bg, outline=col, r=12, width=2.5)
        c.txt(x + 16, 560 - h + 16, name, f(F_TITLE, 20), col)
        c.txt(x + 16, 560 - h + 50, label, f(F_BODY, 14), SUB)
        x += bw + gap
    c.d.line([60 * S, 574 * S, 1540 * S, 574 * S], fill=LINE, width=int(2 * S))

    c.rr(70, 604, 760, 96, fill=(255, 255, 255), outline=LINE, r=10, width=1.5)
    c.txt(88, 618, "PC 关键阈值", f(F_TITLE, 16), BLUE)
    c.txt(88, 646, "三角面 32k/70k ｜ 材质槽 4/8 ｜ 贴图 40/75MB ｜ 骨骼 75/150 ｜ PhysBone 4/8", f(F_BODY, 14), INK)
    c.txt(88, 670, "SkinnedMesh 1/2 ｜ Mesh Read/Write 关闭 → 直接 Very Poor", f(F_BODY, 14), SUB)
    c.rr(860, 604, 680, 96, fill=(255, 255, 255), outline=LINE, r=10, width=1.5)
    c.txt(878, 618, "Quest 关键阈值", f(F_TITLE, 16), WARN)
    c.txt(878, 646, "三角面 7.5k/10k ｜ 材质槽 1 ｜ 贴图 10/18MB ｜ PhysBone 0/4 组件", f(F_BODY, 14), INK)
    c.txt(878, 670, "超出硬上限 → 组件被直接移除（Show Avatar 也救不回）", f(F_BODY, 14), SUB)
    c.save("ranks.png")


flow(); hierarchy(); merge(); params(); physbone(); ranks()
print("done")
