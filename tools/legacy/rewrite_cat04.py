import io

f = r"C:\Users\Administrator\Desktop\源码\VRChat改模教程\app\Content2.cs"
t = io.open(f, encoding="utf-8-sig").read()
idx = t.index("        static Category Cat04()")
head = t[:idx]

body = '''        static Category Cat04() => new Category("04", "换装与改模",
            "改模最高频的操作：把衣服、配饰、发型装到一个不是它们的 Avatar 上",
            "顶部「改模方式」切 MA 插件 / 古法，两条路线的步骤不同 · 约 2 小时",

            new Item("MA 插件 还是 古法？先分清", TagKind.Basic,
                new Para("两条路线做的事完全一样（让衣服跟着 Avatar 的骨骼动），区别只在「谁来干这些活」：插件在[[播放|Play]] 或上传时自动处理，古法全靠你手动点。"),
                new Table(new[] { "维度", "MA 插件路线", "古法路线（手动）" },
                    new[] { "核心工具", "Modular Avatar / VRCFury", "Unity 原生组件 + 手写动画层" },
                    new[] { "换装方式", "prefab 拖进 Avatar，插件按骨骼名合并", "手动改 Root Bone 与 Bones 数组" },
                    new[] { "开关与菜单", "加 MA Menu Item，参数自动生成", "手建 Menu / Parameters，FX 层写状态机" },
                    new[] { "可逆性", "删组件即还原，资产更新可整包替换", "改动写进场景与控制器，只能靠备份回滚" },
                    new[] { "学习成本", "低，10 分钟装好第一套衣服", "高，但能看清每一步到底在改什么" },
                    new[] { "适合场景", "90% 的日常改模", "奇葩模型、插件失效自救、跟老教程" }),
                new Tip("提示", "两条路线不是信仰之争：先用 MA 把手感练出来，再用古法完整做一遍，之后遇到任何模型都不慌。本页会跟着顶部「改模方式」切换内容。")),

            // ── MA 插件路线 ─────────────────────────────────────────────
            Item.Ma("MA 路线总览：两条分支怎么选", TagKind.Main,
                new Table(new[] { "路线", "适合", "原理", "风险" },
                    new[] { "Modular Avatar", "新手首选，90% 场景够用", "按骨骼名合并，Play 模式即生效", "骨骼名不一致会合并失败" },
                    new[] { "VRCFury", "想要完全可逆、自动处理 WD", "上传瞬间才合并，控制器文件不动", "和 MA 混用时只选一种" }),
                new Picture("merge", "换装的本质：衣服骨骼被合并进 Avatar 骨架，共用骨骼指向 Avatar"),
                new Tip("经验", "只装一个就够。同一件衣服用 MA 就别再叠 VRCFury，否则骨骼会被合并两次。")),

            Item.Ma("Modular Avatar 一键换装", TagKind.Main,
                new Bullets(true,
                    "导入衣服的 unitypackage。",
                    "把衣服 prefab 直接拖到 Avatar 根物体下 —— 不要 Unpack Prefab，这是 MA 的核心优势。",
                    "右键衣服根物体 → [[ModularAvatar] Setup Outfit]。",
                    "它会自动加 MA Merge Armature：按骨骼名合并，共用骨骼替换成 Avatar 的，独有骨骼（裙摆等）保留原位。",
                    "进 [[播放|Play]] 模式验证：衣服跟着身体动，就算成功。",
                    "关掉或删掉原来的衣服网格。",
                    "需要跟随身体形变的衣服（鞋、胸、胖瘦）加 MA Blendshape Sync，把 Avatar 的同名 shape 加进列表。",
                    "做成开关：衣服根物体加 MA Object Toggle，再加 MA Menu Item 起个名字，游戏内菜单就出现了。"),
                new Tip("经验", "MA 是按骨骼名字匹配的。遇到 J_Bip_C_Hips 和 Hips 这种，先在 Blender 里批量改名，比事后救火省事。")),

            Item.Ma("MA 菜单与参数自动化", TagKind.Main,
                new Bullets(true,
                    "开关用 MA Object Toggle：Target 指向衣服根物体，勾上 Include Children。",
                    "菜单入口用 MA Menu Item：填名字（游戏里的显示文字）、可选图标、默认值。",
                    "占位参数在 MA Menu Item 里手动填 Parameter 名即可复用已有参数，不填就自动生成一个 Bool。",
                    "MA 会在上传时把所有参数合并进 Expression Parameters，你不需要自己建 Menu / Parameters 资产。",
                    "要节省同步预算：多个互斥选项合成一个 Int 参数，或用 MA Menu Item 的子菜单（Sub Menu）分组。"),
                new Picture("params", "菜单 / 参数 / FX 层的关系，以及 256 bit 同步预算"),
                new Tip("警告", "参数总预算 256 bit：Bool 1 bit、Int 8 bit、Float 8 bit。用 5 个 Bool 做 5 套衣服，不如用 1 个 Int。")),

            Item.Ma("VRCFury 方案（另一条插件分支）", TagKind.Main,
                new Bullets(false,
                    "衣服拖到 Avatar 下，加 VRCFury → Armature Link：Armature Root 指向衣服骨骼根，Base 指向 Avatar 骨骼。",
                    "Link Mode 一般选 Merge；整体替换选 Replace。",
                    "开关用 VRCFury → Toggle，填 Target Object，菜单项和参数自动生成。",
                    "优点：上传时才合并，控制器文件完全不动，删掉组件就回到原样。",
                    "它会自动处理 Write Defaults，这一点比手写动画层省心太多。"),
                new Tip("注意", "同一件衣服只用一种方案。MA 和 VRCFury 同时作用在同一套骨骼上，会出现双重合并。")),

            // ── 古法路线 ───────────────────────────────────────────────
            Item.Classic("古法路线总览：手动要干哪些活", TagKind.Main,
                new Para("古法 = 不依赖 MA / VRCFury，全部用 Unity 原生组件完成。插件帮你做的五件事，你要一件件自己接手："),
                new Table(new[] { "环节", "古法要做什么", "做错的表现" },
                    new[] { "骨骼对齐", "改 Skinned Mesh Renderer 的 Root Bone 与 Bones 数组", "衣服悬空、原地不动、整体偏移" },
                    new[] { "独有骨骼", "裙摆 / 飘带 / 耳朵的根，拖到 Avatar 对应骨骼下", "动起来甩飞、穿模、原地抽搐" },
                    new[] { "材质", "新建 lilToon 材质、贴图 sRGB 按用途设置", "衣服粉紫、发白、全黑" },
                    new[] { "物理", "手动加 VRCPhysBone + 碰撞体，调 Pull / 弹性", "裙摆穿腿、头发钉死不动" },
                    new[] { "开关与菜单", "手建 Menu / Parameters 资产 + FX 层状态机", "上传后衣服消失、菜单里没有这一项" },
                    new[] { "更新资产", "全靠自己备份的 zip 回滚", "作者出新版时无从下手" }),
                new Picture("merge", "古法做的是同一件事，只是每一步都手动：把衣服骨骼接进 Avatar 骨架"),
                new Tip("提示", "古法不是「落后」，是「可控」。插件失效、模型结构奇葩、或者要读懂别人发的旧教程时，这套手法是唯一解。")),

            Item.Classic("古法换装：手动重绑全流程", TagKind.Main,
                new Bullets(true,
                    "先把整个项目文件夹（不含 Library）压一份 zip —— 古法没有「删组件就能还原」这回事。",
                    "导入衣服，这次要 Unpack Prefab：不改内部引用就绑不上 Avatar。",
                    "展开衣服的 Armature，找到与 Avatar 同名的骨骼，把衣服网格 [[Skinned Mesh Renderer]] → Root Bone 拖成 Avatar 的 Hips。",
                    "点开 Bones 数组，逐个把元素替换成 Avatar 的对应骨骼；数量对不上时先确认是不是多了一个辅助根骨。",
                    "衣服独有的骨骼（裙摆、飘带）保留自身层级，在层级窗口里把它的根拖到 Avatar 对应骨骼（Hips / Chest / Head）下面。",
                    "材质：新建材质挂 lilToon，按用途设置贴图 sRGB（颜色图勾上、法线/遮罩不勾），确认没有粉紫。",
                    "物理：给裙摆、飘带加 VRCPhysBone 并配碰撞体；注意不要和 Avatar 原有的碰撞体重复。",
                    "进 [[播放|Play]] 手动检查：站立、走路、坐下、挥手各看一遍，再 Build & Test。"),
                new Tip("经验", "手动做一遍非常值得。你会真正理解 MA 帮你做了什么，也才能在插件失效时自己 debug。")),

            Item.Classic("古法：手写 FX 开关与菜单", TagKind.Main,
                new Para("古法的「开关」没有任何自动化，全部是这三个东西串起来：菜单（入口）→ 参数（状态）→ FX 层（动画）。"),
                new Bullets(true,
                    "创建 VRCExpressionsMenu 和 VRCExpressionParameters 两个资产，拖进 [[Avatar Descriptor|Avatar Descriptor]] 的 Expressions 栏。",
                    "在 Parameters 里加一个 Bool，例如 Outfit_Hoodie，勾上 Saved 和 Synced。",
                    "在 Menu 里加一个 Toggle，绑定这个参数，起名「卫衣」。每页最多 8 项，超了要建子菜单。",
                    "FX 层建两个状态 Off / On，分别播放「关闭 / 开启该物体」的动画 clip；转场条件就是参数真假，取消 Has Exit Time，转场时长 0。",
                    "开关的那条 clip 要写全所有受影响的物体（含子物体），漏一个就会出现「开关只关掉一半」。",
                    "全 Avatar 的 Write Defaults 必须统一，要么全开要么全关。"),
                new CodeBlock("FX Controller → Layer \"Outfit\"\n├─ State Off (默认)   Motion = Hoodie_SetActive_false\n└─ State On            Motion = Hoodie_SetActive_true\n   Transitions:\n     Off → On   condition  Outfit_Hoodie == true   Has Exit Time = false   Duration = 0\n     On  → Off  condition  Outfit_Hoodie == false  同上",
                    "古法最常翻车的地方：忘了设 Saved / Synced、忘了取消 Has Exit Time、Write Defaults 混用。"),
                new Tip("警告", "参数总预算 256 bit：Bool 1 bit、Int 8 bit、Float 8 bit。5 个 Bool 做 5 套衣服，不如 1 个 Int 省一半预算。")),

            // ── 两条路线通用 ───────────────────────────────────────────
            new Item("换装五大坑（两条路线都会遇到）", TagKind.Main,
                new Table(new[] { "症状", "原因", "处理" },
                    new[] { "上传后衣服不见", "Animator 默认关闭了它，或 Write Defaults 混用", "检查 FX / 组件状态；统一 WD；MA 路线可改用 VRCFury" },
                    new[] { "衣服悬空 / 错位", "骨骼名不一致，没合并上", "MA：检查 Merge Armature 的 Target；古法：核对 Bones 数组" },
                    new[] { "衣服比例不对", "衣服基于另一体型制作", "Blender 里缩放贴合后重新导出" },
                    new[] { "腋下 / 胸口穿模", "权重或 BlendShape 没联动", "MA：加 Blendshape Sync；古法：手动修权重；都加 PhysBone Limits" },
                    new[] { "材质粉紫 / 发白", "材质丢失、着色器不兼容", "重挂 lilToon 材质；检查贴图 sRGB" })),

            new Item("换装后自检 6 项", TagKind.Tool,
                new Para("两条路线做完都过一遍，能省掉 80% 的「上传后才发现」："),
                new Bullets(false,
                    "走路、坐下、挥手、躺下：衣服四个动作都跟得上？",
                    "弯胳膊、抬腿：腋下、裆部有没有破面或穿模？",
                    "衣服材质在游戏里和编辑器一致（没有粉紫、没有全黑）？",
                    "菜单里能点到这一项，切换后模型状态和预期一致？",
                    "参数预算还有余量（[[Avatar Descriptor|Avatar Descriptor]] → Performance 页看）？",
                    "桌面上有一份这次改动之前的项目备份 zip？")));
    }
}
'''

io.open(f, "w", encoding="utf-8", newline="\r\n").write(head + body)
print("Cat04 rewritten, file len =", len(head + body))
