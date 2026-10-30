using System;

namespace VRChatGuide
{
    public static partial class Content
    {
        // ══════════════════════════════════════════════════════════════════
        // 07 优化与 Quest
        // ══════════════════════════════════════════════════════════════════
        static Category Cat07() => new Category("07", "优化与 Quest", "模型能不能被别人看见，取决于这一章", "上线前必查 · 约 1 小时",
            new Item("PC 性能等级阈值", TagKind.Main,
                new Para("性能等级不是玄学，就是一张阈值表。任何一项超标，整体等级就掉一档。"),
                new Table(new[] { "项目", "Excellent", "Good", "Medium", "Poor" },
                    new[] { "三角面", "32,000", "70,000", "70,000", "70,000" },
                    new[] { "贴图内存", "40 MB", "75 MB", "110 MB", "150 MB" },
                    new[] { "Skinned Mesh", "1", "2", "8", "16" },
                    new[] { "普通 Mesh", "4", "8", "16", "24" },
                    new[] { "材质槽", "4", "8", "16", "32" },
                    new[] { "骨骼", "75", "150", "256", "400" },
                    new[] { "PhysBone 组件", "4", "8", "16", "32" },
                    new[] { "PhysBone 影响骨骼", "16", "64", "128", "256" },
                    new[] { "PhysBone 碰撞体", "4", "8", "16", "32" },
                    new[] { "Animator", "1", "4", "16", "32" },
                    new[] { "Constraint", "100", "250", "300", "350" }),
                new Tip("警告", "只要任何一个网格关闭了 [[启用读写|Read/Write Enabled]]，三角面会显示 Mesh Read/Write Disabled，直接判 Very Poor。"),
                new Picture("ranks", "五个等级与 PC / Quest 的关键阈值")),

            new Item("优化九连（按收益排序）", TagKind.Main,
                new Bullets(true,
                    "合并材质槽：同贴图材质合并，或用 AAO 的 Merge Material、手动做图集。",
                    "合并网格：同类小物件合并，减少 Skinned Mesh 数量。",
                    "降贴图分辨率：非重点部位 2048 → 1024，总贴图内存盯住 75 MB。",
                    "删无用骨骼：手指末端、装饰骨，AAO 的 Trace and Optimize 能自动去掉。",
                    "收敛 PhysBone：合并参数相近的组件，用 Limits 代替 Collider，Ignore Transforms 排除不动的骨骼。",
                    "减面：Decimate / Meshia，面部、手指、轮廓保持高面。",
                    "关掉不必要 Pass：描边、边缘光、阴影按需，半透明材质尽量少。",
                    "==去掉多余 Animator：能用 FX 层解决就不要新建 Animator。==",
                    "少用 Constraint：有深度的约束链成本很高，能用骨骼层级解决的不用约束。"),
                new Tip("经验", "目标：日常社交 ≥ Good，展会 / 多人活动 ≥ Medium。Very Poor 会被别人默认屏蔽成 Fallback。")),

            new Item("Quest / Android 适配", TagKind.Main,
                new Table(new[] { "项目", "Excellent", "Good", "Medium", "Poor" },
                    new[] { "三角面", "7,500", "10,000", "15,000", "20,000" },
                    new[] { "材质槽", "1", "1", "2", "4" },
                    new[] { "贴图内存", "10 MB", "18 MB", "25 MB", "40 MB" },
                    new[] { "PhysBone 组件", "0", "4", "6", "8" },
                    new[] { "PhysBone 影响骨骼", "0", "16", "32", "64" }),
                new Bullets(true,
                    "单独做一个低模版本：面数 ≤ 10k、单材质、贴图 1024。",
                    "用 Per-Platform Avatar Overrides，在 Descriptor 里给 Android 指定另一套网格 / 材质 / 菜单。",
                    "或用 AAO 的 Trace and Optimize + 材质合并，把材质槽压到 1。",
                    "上传时平台勾 Android，单独发布一次。"),
                new Tip("警告", "移动端有硬上限（8 个 PhysBone、64 个受影响骨骼、150 个 Constraint）。超了会被直接移除组件，Show Avatar 也救不回来。")),

            new Item("性能自查流程", TagKind.Tool,
                new Para("==按顺序走完这几步，性能等级基本不会翻车。=="),
                new Bullets(true,
                    "Build 面板看 Performance Rank，先定位是哪一项把等级拉低的（颜色会标出来）。",
                    "点开 Avatar Stats，看 Original 和 Perf Filtered 两栏，确认到底是哪一项超标。",
                    "优先处理材质槽和三角面，这两项收益最大。",
                    "最后处理 PhysBone：数量 → 影响骨骼数 → 碰撞体。",
                    "改一项就 Build & Test 一次，别一次改五处。")));

        // ══════════════════════════════════════════════════════════════════
        // 08 进阶
        // ══════════════════════════════════════════════════════════════════
        static Category Cat08() => new Category("08", "进阶功能", "面部追踪、PhysBone 玩法、交互组件、动画层", "跑通主线之后再看",
            new Item("面部追踪 Face Tracking", TagKind.Advanced,
                new Bullets(true,
                    "前提：模型要有 ARKit 52 标准 BlendShape（eyeBlinkLeft、jawOpen、mouthSmileLeft…），或用 Unified Expressions 之类的映射 prefab。",
                    "Descriptor → Face Tracking 开启 → 把头部网格加入 Face Tracking Meshes。",
                    "在 Blendshape Sets 里把追踪键映射到模型的 shape 名。",
                    "客户端侧装 VRCFaceTracking，摄像头或 iPhone 走 OSC（UDP 9000）。"),
                new Tip("注意", "面捕会吃性能统计里的 Face Tracking Operations（Excellent ≤ 2000）和 Forwarded Parameters（≤ 10）。别一次映射几百条。")),

            new Item("PhysBone 玩法", TagKind.Advanced,
                new Table(new[] { "玩法", "做法" },
                    new[] { "可抓取互动", "Allow Grabbing / Posing 设 Everyone；Grab Movement 0.2~0.4 手感最好" },
                    new[] { "被抓时触发反应", "组件设 Parameter = Tail，Animator 里用 Tail_IsGrabbed 做表情或音效" },
                    new[] { "尾巴摆幅可调", "用 Radial Puppet 驱动骨骼动画，不要试图实时改 PhysBone 参数" },
                    new[] { "耳朵随声音动", "用 Viseme 或音量参数驱动骨骼旋转" }),
                new Tip("警告", "PhysBone 的 Spring / Pull 等属性在初始化时固定，不能靠动画实时修改 —— 官方明确不支持，会随版本失效。")),

            new Item("交互组件（Contacts 等）", TagKind.Advanced,
                new Para("==每个组件都要算性能账：能少用一个就少用一个。=="),
                new Bullets(false,
                    "VRC Contact Sender / Receiver 可以做触碰开关：摸头变色、被戳有反应。",
                    "SPS 等社区插件属 PC only，且需要对方也有对应组件才生效。",
                    "这些都会计入性能统计（Contacts 在 Excellent 只允许 8 个），不要滥加。",
                    "做之前先想清楚：这个交互是「别人能感受到」，还是「只有自己爽」。")),

            new Item("自定义动画层要点", TagKind.Advanced,
                new Para("==动画层越多越容易互相打架，能合并就合并。=="),
                new Bullets(true,
                    "全 Avatar 的 Write Defaults 必须统一，混用是玄学 bug 的源头。",
                    "转场一律取消 Has Exit Time，时长设 0，否则开关会有延迟感。",
                    "动画 clip 只写必要属性，写得越多越容易覆盖别人（或自己）的状态。",
                    "调试用 Gesture Manager，能在编辑器里直接试手势和菜单，不用进游戏。")));

        // ══════════════════════════════════════════════════════════════════
        // 09 排错手册
        // ══════════════════════════════════════════════════════════════════
        static Category Cat09() => new Category("09", "排错手册", "按症状查：先看现象，再看原因，最后照做", "收藏这一页，能省掉一半求助时间",
            new Item("20 条症状速查", TagKind.Tool,
                new Para("==先看「最可能原因」那一列，八成问题在这里就有答案。=="),
                new Table(new[] { "症状", "最可能原因", "处理" },
                    new[] { "Avatar is not in T-pose", "Rig 姿势不对", "配置 → Pose → Enforce T-Pose" },
                    new[] { "模型变巨人 / 蚂蚁", "缩放系数错", "fbx 模型页缩放系数设 1；检查 Blender 单位" },
                    new[] { "模型躺倒 / 侧躺", "坐标轴不对", "重新导出：Forward -Z、Up Y" },
                    new[] { "材质粉紫", "着色器丢失或不兼容", "重挂 lilToon；Quest 换兼容 shader" },
                    new[] { "Mesh Read/Write Disabled", "fbx 里关了读写", "模型页勾选 Read/Write Enabled" },
                    new[] { "三角面 / 材质槽超标", "没优化", "见「优化九连」；用 AAO 合并" },
                    new[] { "说话嘴不动", "Viseme 未配置或名字不符", "LipSync → Auto Detect；核对 shape 名" },
                    new[] { "眼睛不跟随", "没有 Eye Look shape 或未启用", "填 Eyes 骨骼和四个 Look shape" },
                    new[] { "头发 / 尾巴完全不动", "单链缺 Endpoint Position", "填 +Y 0.1 起" },
                    new[] { "头发乱飞穿头", "没有限制", "Limits（Angle）或头部 Collider；调高 Immobile" },
                    new[] { "换装后衣服不见了", "Write Defaults 混用", "统一 WD；或改用 VRCFury 生成" },
                    new[] { "菜单里没有我的开关", "参数或菜单没挂上", "检查 Descriptor 的 Expressions 两栏" },
                    new[] { "别人看不到我的开关效果", "参数没勾 Synced", "Parameters 里勾 Synced" },
                    new[] { "参数超预算报错", "256 bit 用尽", "合并 Bool → Int，降低 Float bit" },
                    new[] { "上传卡在 Building", "网络 / 杀软 / 缓存", "清 Library、关杀软、换网络重来" },
                    new[] { "上传成功但别人看不到", "私有或性能 Very Poor 被屏蔽", "设 Public；提升 Perf Rank" },
                    new[] { "进游戏变成默认小人", "没上传成功或平台不符", "Content Manager 确认；检查 PC / Android" },
                    new[] { "身体局部透明 / 破洞", "法线反了或 Cull 设置错", "Blender 重算法线；Cull 改 Off 测试" },
                    new[] { "换头后表情错乱", "shape 名映射错", "重新映射 Viseme / Eye Look" },
                    new[] { "视角在头顶或胸口", "View Position 错", "放到两眼之间后重新 Build" })),

            new Item("参数与同步类问题", TagKind.Tool,
                new Para("==参数超预算会静默失效，先查预算再看菜单。=="),
                new Bullets(true,
                    "开关点了没反应：先在 Gesture Manager 里试，能通说明是上传或同步问题。",
                    "自己能看见、别人看不见：参数没勾 Synced。",
                    "换世界后衣服被重置：参数没勾 Saved。",
                    "多个开关互相干扰：多半是 Write Defaults 混用，或者两层动画写了同一个属性。",
                    "菜单项超过 8 个：新建 Sub-Menu 分层，别硬塞。")),

            new Item("性能与被屏蔽", TagKind.Tool,
                new Para("==被屏蔽多半是性能等级太低，先看等级再看模型。=="),
                new Bullets(false,
                    "别人的画质设置里 Minimum Displayed Performance Rank 设成了 Poor 或 Medium，你的模型就会被换成 Fallback。",
                    "PC 默认允许 Very Poor，但移动端默认只显示 Medium 以上。",
                    "「我被替换成机器狗了」不是 bug，是性能等级太低。",
                    "先看 Avatar Stats 里红字那一行，再从收益最高的项开始砍。")));

        // ══════════════════════════════════════════════════════════════════
        // 10 资源
        // ══════════════════════════════════════════════════════════════════
        static Category Cat10() => new Category("10", "资源与链接", "官方文档、插件、社区教程、模型商店", "收藏备用",
            new Item("必看官方文档", TagKind.Tool,
                new Para("==遇到本程序没覆盖的细节，第一时间查官方文档，比任何教程都准。=="),
                new Links(("官方创作者文档", "https://creators.vrchat.com/", "总入口"),
                          ("首次上传 Avatar", "https://creators.vrchat.com/avatars/creating-your-first-avatar/", ""),
                          ("性能等级阈值", "https://creators.vrchat.com/avatars/avatar-performance-ranking-system/", ""),
                          ("PhysBone 全参数", "https://creators.vrchat.com/common-components/physbones/", ""),
                          ("表情菜单与控件", "https://creators.vrchat.com/avatars/expression-menu-and-controls/", ""),
                          ("动画参数", "https://creators.vrchat.com/avatars/animator-parameters/", ""),
                          ("面部追踪", "https://creators.vrchat.com/avatars/face-tracking/", ""),
                          ("VCC 官方文档", "https://vcc.docs.vrchat.com/", ""))),

            new Item("插件与工具", TagKind.Tool,
                new Para("==工具统一用 VCC / ALCOM 装，版本不会互相踩。=="),
                new Links(("VCC 中文镜像", "https://vcc.vrczh.org/", "国内加速"),
                          ("Modular Avatar", "https://modular-avatar.nadena.dev/docs/intro", "含换装教程"),
                          ("VRCFury", "https://vrcfury.com/", "下载与文档"),
                          ("lilToon", "https://booth.pm/ja/items/1712767", "BOOTH"),
                          ("AAO 优化器", "https://vpm.anatawa12.com/", "VPM"),
                          ("ALCOM", "https://vrc-get.anatawa12.com/en/alcom/", "社区包管理器"),
                          ("VRM 转换器", "https://github.com/esperecyan/VRMConverterForVRChat", "VRoid → VRChat"),
                          ("immersive_scaler", "https://github.com/teamneoneko/immersive_scaler", "比例缩放"))),

            new Item("社区教程", TagKind.Tool,
                new Para("==跟着视频做时先拿一个空项目练手，别直接上正式模型。=="),
                new Links(("B站 · 保姆级改模教程", "https://www.bilibili.com/video/BV1r8PTzSEyZ/", "全流程"),
                          ("B站 · Modular Avatar 教学", "https://www.bilibili.com/video/BV15V7DztEcR/", "换装"),
                          ("B站 · 不适配衣服改造", "https://www.bilibili.com/opus/929314847085035521", "Unity/Blender"),
                          ("B站 · VRM 转 VRC 流程", "https://www.bilibili.com/opus/887419902127767571", "骨骼与权重"),
                          ("VRCD 中文文档库", "https://docs.vrcd.org.cn/", "社区文档"),
                          ("PhysBone 设置指南", "https://www.tripo3d.ai/zh/blog/vrchat-physbone-settings", "图文"),
                          ("lilToon 改色步骤", "https://livestreamers.co.jp/vrchat/guides/avatar-color-liltoon", "日文图文"))),

            new Item("模型与服装来源", TagKind.Tool,
                new Bullets(false,
                    "==BOOTH（booth.pm）—— 日本模型 / 服装主战场，务必读作者页的「利用規約」（禁止再配布、禁止商用等）。==",
                    "Gumroad —— 欧美资产，注意 License 类型。",
                    "VRoid Studio —— 免费捏人，导出 .vrm 后用转换器进 Unity。",
                    "Sketchfab —— 注意授权是 CC0 还是仅可查看。"),
                new Tip("警告", "买下来的模型不等于可以再发布。二次配布、转卖、拿去商用都可能违反作者条款，上传前先看一遍作者说明。")));
    }
}
