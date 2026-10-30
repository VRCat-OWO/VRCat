using System;

namespace VRChatGuide
{
    public static partial class Content
    {
        // ══════════════════════════════════════════════════════════════════
        // 05 外观改造
        // ══════════════════════════════════════════════════════════════════
        static Category Cat05() => new Category("05", "外观改造", "改色、改身高、换头、加耳朵尾巴 —— 把模型改成「你的」", "改模的第二层 · 每条 20~60 分钟",
            new Item("改色：永远改副本", TagKind.Main,
                new Para("改模翻车最多的不是技术，是「手滑改了原材质」。原则只有一句：复制材质再改，永不改原材质。"),
                new Bullets(true,
                    "选中材质，Ctrl+D 复制，重命名成 Hair_Pink 这类可读的名字。",
                    "在 lilToon 面板里改 Main Color、1st~3rd Shade Color、Outline Color；有必要再换 Main Texture。",
                    "只改颜色时，白贴图模型也能出效果；要渐变、花纹、刺绣就得动贴图（Photoshop / Krita / Substance）。",
                    "==眼睛、高光通常有单独的 Mask 和 Emission，改色时一起调，否则会出现「高光还是旧色」。==",
                    "要做游戏内可换色：多材质 + Int 参数切换（简单但吃材质槽），或用 Radial Puppet 驱动 _Color 的 HSV 动画。"),
                new Tip("经验", "想让人看不出是同一个模型，改「阴影色 + 描边色」比改主色有效得多。")),

            new Item("改身高 / 改体型", TagKind.Main,
                new Table(new[] { "方案", "做法", "注意" },
                    new[] { "Unity 整体缩放", "缩放 Armature 根骨骼，不要缩放父级空物体", "同步调 View Position、PhysBone Radius、碰撞体" },
                    new[] { "Blender 精确改", "在 Blender 里对骨骼缩放后应用，再导出", "比例最自然，推荐" },
                    new[] { "比例工具", "immersive_scaler / imscaler 等插件按真人比例缩放", "会一并处理视角与碰撞体" },
                    new[] { "客户端缩放", "VRChat 自带 Avatar Scaling，游戏内调身高", "不改变上传数据，适合临时用" }),
                new Bullets(true,
                    "==改完身高必须回头查四项：View Position、PhysBone 的 Radius、PhysBone Collider 尺寸、Bounds Size。==",
                    "Bounds 超过 4 米会把性能等级往下拉。",
                    "改太小（< 1.2 米）时，手部标准碰撞体要跟着缩，不然抓东西会飘。"),
                new Tip("警告", "只改视觉大小、不管碰撞体，会出现「别人摸不到你的头」「抓尾巴抓空」。")),

            new Item("换头（最需要耐心的一步）", TagKind.Advanced,
                new Bullets(true,
                    "骨骼：新头的脖子骨骼要能接到原 Avatar 的 Neck / Head，Blender 里对齐位置后再导出。",
                    "表情：新头的 BlendShape 名字和原体不同时，口型（Viseme）与 Eye Look 全都要重新映射。",
                    "==权重：脖子接缝处权重过渡要平滑，否则转头会撕裂。==",
                    "材质：头与身体的肤色要统一，同贴图或同色值，否则有「戴面具」感。",
                    "比例：头身比失调是换头最常见的问题，导入前先用参考图核对。"),
                new Tip("注意", "换头往往牵动整套表情系统。如果只是想换脸，优先找「同一作者的同款替换头」，能省掉 80% 的工作。")),

            new Item("加耳朵 / 尾巴 / 翅膀", TagKind.Main,
                new Bullets(true,
                    "配饰 prefab 拖到 Avatar 下 → [ModularAvatar] Setup Outfit（或手动把骨骼父级挂到 Head / Hips）。",
                    "在配饰根骨加 VRC PhysBone：Endpoint Position 必填，Pull 0.2 / Spring 0.8 / Gravity 0.1。",
                    "尾巴这类单链尤其容易漏 Endpoint Position —— 漏了就完全不动。",
                    "防穿模：优先 Limits（Angle），其次加 VRCPhysBoneCollider（腿 / 臀）。",
                    "做成独立开关：MA Object Toggle + MA Menu Item。"),
                new Tip("经验", "尾巴想「甩得好看」，关键在于把 Immobile 调高（0.5~0.8），否则坐下或移动时会鬼畜。")),

            new Item("头发改造", TagKind.Main,
                new Bullets(false,
                    "换发型：整块替换网格后要重新挂 PhysBone（新骨骼名不同，配置不能照抄）。",
                    "加呆毛 / 双马尾：加骨骼 → 加 PhysBone → 用 Limits 限制回弹 → 调高 Immobile 防抖。",
                    "头发穿脸：Ignore Transforms 排除贴脸刘海 + 头部 Collider + Limits 三重保险。",
                    "长发坐姿穿地：给裙摆/长发加 Hips 到地面的 Collider，或用 Limits 限死下垂角度。"),
                new Tip("注意", "头发是性能黑洞：一个发型拆成 6 个 PhysBone 组件很常见，但每多一个组件就多吃一份预算。优先合并参数相近的链。")));

        // ══════════════════════════════════════════════════════════════════
        // 06 Blender 操作
        // ══════════════════════════════════════════════════════════════════
        static Category Cat06() => new Category("06", "Blender 操作", "Unity 里做不了的事：骨骼、权重、体型、减面", "只用 Unity 也能改模，但天花板在这里",
            new Item("导出设置（Unity 专用）", TagKind.Advanced,
                new Para("Blender 和 Unity 的坐标系、单位都不同。导出参数错一个，模型就会躺倒、翻转或者变成巨人。"),
                new Table(new[] { "设置", "值", "说明" },
                    new[] { "Limit to", "Selected Objects", "只选 Avatar 根，别把摄像机灯光带出去" },
                    new[] { "Object Types", "Armature + Mesh", "去掉 Camera / Light" },
                    new[] { "Scale", "1.0", "改了就整体缩放" },
                    new[] { "Apply Scalings", "FBX Units Scale", "避免 100 倍问题" },
                    new[] { "Forward / Up", "-Z Forward / Y Up", "导出后在 Unity 里直立、面朝 +Z" },
                    new[] { "Apply Transform", "勾选（若有此项）", "把物体变换烘进数据" },
                    new[] { "Add Leaf Bones", "不勾", "会多出无用末端骨" },
                    new[] { "Smoothing", "Normals Only", "保留硬边" },
                    new[] { "Animation", "不勾", "除非要带动作" }),
                new Tip("警告", "导出后先在 Unity 里看一眼：直立、面朝 +Z、身高 1.6 左右。任何一条不对，回 Blender 重导，别在 Unity 里硬拉。")),

            new Item("常用操作清单", TagKind.Advanced,
                new Para("==这张表按「想做什么」查做法，找不到再回章节里看细节。=="),
                new Table(new[] { "需求", "操作要点" },
                    new[] { "改骨骼名匹配衣服", "编辑模式重命名，或用 Batch Rename；导出后 Unity 重新 Configure" },
                    new[] { "加骨骼（呆毛、尾巴）", "编辑模式挤出骨骼 → 指定父级 → 权重刷到对应网格" },
                    new[] { "修穿模", "Weight Paint 手刷，或 Data Transfer 修改器把身体权重传给定衣服" },
                    new[] { "减面", "Decimate（Collapse 0.5 起试）；面部、手部慎用" },
                    new[] { "改体型", "编辑模式移动顶点，或改 Shape Key（推荐，可逆）" },
                    new[] { "合并网格", "Ctrl+J 前先统一材质与权重，可减少 Skinned Mesh 数量" },
                    new[] { "加新表情", "编辑 Shape Key → 导出时勾选 Import BlendShapes" })),
            new Item("改完怎么回 Unity", TagKind.Advanced,
                new Bullets(false,
                    "覆盖式：新 fbx 替换同名同路径的旧 fbx，材质和组件引用自动保留。最省事，但风险高，先备份。",
                    "并存式：新 fbx 放 00_Base_v2/，在场景里把 Avatar 的 Skinned Mesh Renderer → Mesh 换成新网格，再重绑骨骼。安全，推荐。"),
                new Tip("经验", "改 Blender 之前先确认 Unity 里能跑。这样万一新模型出问题，你还有一个确定能用的版本对照。")));
    }
}
