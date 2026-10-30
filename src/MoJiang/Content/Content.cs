using System;

namespace VRChatGuide
{
    public static partial class Content
    {
        public static readonly Category[] All = Build();

        static Category[] Build()
        {
            var list = new System.Collections.Generic.List<Category>
            {
                Cat01(), Cat02(), Cat03(), Cat04(), Cat05(),
                Cat06(), Cat07(), Cat08(), Cat09(), Cat10()
            };
            // 11–15：来自 dwgx 的 VRC 改模资源中心（见 tools/import_dwgx.py）
            list.AddRange(Content5.All());
            list.Add(Cat16());
            return list.ToArray();
        }

        /// <summary>章节分组名（左侧导航用）</summary>
        public static string GroupOf(Category c, int index)
        {
            switch (c.Badge)
            {
                case "01": case "02": return "开始";
                case "11": case "12": case "13": case "14": return "精选指南 · dwgx";
                case "15": return "资源库";
                case "16": return "支持作者";
            }
            if (index < 6) return "主线";
            if (index < 8) return "进阶";
            return "工具";
        }

        public static int ItemCount
        {
            get
            {
                int n = 0;
                foreach (var c in All) n += c.Items.Length;
                return n;
            }
        }

        /// <summary>上传前检查清单（可勾选，进度会保存）</summary>
        public static readonly (string Group, string[] Items)[] Checklist =
        {
            ("模型与骨骼", new[]
            {
                "T-Pose 正确，Avatar 图标是绿色人形",
                "位置 (0,0,0)、朝向 +Z、身高 1.4~1.9 米",
                "View Position 在两眼之间",
                "LipSync 已识别（Viseme 全绿或已手填）",
                "Eye Look 已启用，四个方向 shape 正确",
            }),
            ("材质与贴图", new[]
            {
                "所有材质已复制并换成 lilToon / Poiyomi",
                "贴图无丢失，模型没有粉紫块",
                "总贴图内存 ≤ 75 MB（Quest ≤ 18 MB）",
                "每个网格都开启了 Read/Write",
            }),
            ("性能", new[]
            {
                "三角面 ≤ 70k（目标 ≤ 32k；Quest ≤ 10k）",
                "材质槽 ≤ 8（目标 ≤ 4；Quest 1）",
                "Skinned Mesh ≤ 2，骨骼 ≤ 150",
                "PhysBone 组件 ≤ 8，影响骨骼 ≤ 64",
                "每个 PhysBone 都有合理的 Limits / Radius",
                "没有把人形骨骼当 PhysBone Root",
            }),
            ("菜单与参数", new[]
            {
                "菜单每页 ≤ 8 项，图标清晰",
                "参数总 bit ≤ 256，换装参数勾了 Saved + Synced",
                "所有开关都在 Gesture Manager 里实测通过",
                "全 Avatar 的 Write Defaults 已统一",
            }),
            ("上线前", new[]
            {
                "Build & Test 进游戏实测过（含第三人称看自己）",
                "已备份项目，已确认模型的使用条款",
                "缩略图、名称、内容标签都填好了",
            }),
        };

        public static readonly (string Key, string Title, string Caption)[] Gallery =
        {
            ("flow", "改模总流程", "从选模型到上传的 8 个环节，以及那条「一次只改一个变量」的铁律"),
            ("hierarchy", "Avatar 物体层级", "组件该挂在哪个物体上，以及四个最常见的层级错误"),
            ("merge", "换装骨骼合并原理", "衣服骨骼如何并进 Avatar 骨架：共用骨骼替换、独有骨骼保留"),
            ("params", "菜单 / 参数 / FX 层", "三者怎么串起来，以及 256 bit 同步预算怎么省"),
            ("physbone", "PhysBone 骨链设置", "Endpoint Position、Limits 与起步参数表"),
            ("ranks", "性能等级与阈值", "五个等级的阶梯图，PC 与 Quest 的关键数字"),
        };
    }
}
