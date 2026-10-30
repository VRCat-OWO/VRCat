using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRChatGuide
{
    public enum UiMode { Both = 0, Chinese = 1, English = 2 }

    /// <summary>教程条目类型标签</summary>
    public enum TagKind { Basic, Main, Advanced, Tool }

    /// <summary>改模路线：MA 插件方案 / 古法手动方案（Both = 两条路线通用）</summary>
    public enum Way { Both = 0, MA = 1, Classic = 2 }

    public abstract class Block
    {
        public abstract string Plain();
    }

    public sealed class Para : Block
    {
        public string Text = "";
        public Para(string t) { Text = t; }
        public override string Plain() => Txt.Plain(Text);
    }

    /// <summary>小节标题（长文里的「一、二、三」）</summary>
    public sealed class Head : Block
    {
        public string Text = "";
        public Head(string t) { Text = t; }
        public override string Plain() => Text;
    }

    public sealed class Bullets : Block    {
        public string[] Items = Array.Empty<string>();
        public bool Numbered;
        public Bullets(bool numbered, params string[] items) { Numbered = numbered; Items = items; }
        public override string Plain() => string.Join(" ", Items.Select(Txt.Plain));
    }

    public sealed class Tip : Block
    {
        /// <summary>提示 / 注意 / 警告 / 经验</summary>
        public string Kind = "提示";
        public string Text = "";
        public Tip(string kind, string text) { Kind = kind; Text = text; }
        public override string Plain() => Txt.Plain(Text);
    }

    public sealed class CodeBlock : Block
    {
        public string Text = "";
        public string Note = "";
        public CodeBlock(string text, string note = "") { Text = text; Note = note; }
        public override string Plain() => Text;
    }

    public sealed class Table : Block
    {
        public string[] Head;
        public string[][] Rows;
        public Table(string[] head, params string[][] rows) { Head = head; Rows = rows; }
        public override string Plain() =>
            string.Join(" ", Head.Select(Txt.Plain)) + " " +
            string.Join(" ", Rows.SelectMany(r => r).Select(Txt.Plain));
    }

    public sealed class Picture : Block
    {
        public string Key;      // Assets 内文件名（不含扩展名）
        public string Caption = "";
        public Picture(string key, string caption = "") { Key = key; Caption = caption; }
        public override string Plain() => Caption;
    }

    public sealed class Item
    {
        public string Title = "";
        public TagKind Tag = TagKind.Basic;
        public Way Flow = Way.Both;
        public Block[] Blocks = Array.Empty<Block>();
        public Item(string title, TagKind tag, params Block[] blocks) { Title = title; Tag = tag; Blocks = blocks; }

        /// <summary>「MA 插件」路线专属条目</summary>
        public static Item Ma(string title, TagKind tag, params Block[] blocks)
        {
            var it = new Item(title, tag, blocks);
            it.Flow = Way.MA;
            return it;
        }

        /// <summary>「古法」路线专属条目</summary>
        public static Item Classic(string title, TagKind tag, params Block[] blocks)
        {
            var it = new Item(title, tag, blocks);
            it.Flow = Way.Classic;
            return it;
        }

        public bool VisibleIn(Way w) => Flow == Way.Both || Flow == w;

        public string FlowName => Flow == Way.MA ? "MA 插件" : Flow == Way.Classic ? "古法" : null;

        public string Plain() =>
            Title + " " + string.Join(" ", Blocks.Select(b => b.Plain()));

        public string TagName => Tag switch
        {
            TagKind.Basic => "基础",
            TagKind.Main => "主线",
            TagKind.Advanced => "进阶",
            _ => "工具"
        };
    }

    public sealed class Category
    {
        public string Badge = "01";
        public string Title = "";
        public string Subtitle = "";
        public string Hint = "";
        public Item[] Items = Array.Empty<Item>();
        public Category(string badge, string title, string subtitle, string hint, params Item[] items)
        { Badge = badge; Title = title; Subtitle = subtitle; Hint = hint; Items = items; }

        public string Plain() => Title + " " + string.Join(" ", Items.Select(i => i.Plain()));

        public Item[] For(Way w) => Items.Where(i => i.VisibleIn(w)).ToArray();
        public int CountFor(Way w) => Items.Count(i => i.VisibleIn(w));
    }

    /// <summary>
    /// 文本里的 Unity 界面双语文案标记：[[中文|English]]
    /// 渲染时按当前模式（双语 / 中文界面 / English UI）决定显示哪一段。
    /// </summary>
    public static class Txt
    {
        const string Open = "[[";
        const string Close = "]]";

        public struct Seg
        {
            public bool IsUi;
            public string Zh;
            public string En;
        }

        public static List<Seg> Parse(string s)
        {
            var list = new List<Seg>();
            if (string.IsNullOrEmpty(s)) return list;
            int i = 0;
            var sb = new StringBuilder();
            while (i < s.Length)
            {
                int a = s.IndexOf(Open, i, StringComparison.Ordinal);
                if (a < 0)
                {
                    sb.Append(s, i, s.Length - i);
                    break;
                }
                sb.Append(s, i, a - i);
                int b = s.IndexOf(Close, a + 2, StringComparison.Ordinal);
                if (b < 0) { sb.Append(s, a, s.Length - a); break; }
                if (sb.Length > 0) { list.Add(new Seg { IsUi = false, Zh = sb.ToString(), En = sb.ToString() }); sb.Clear(); }
                string inner = s.Substring(a + 2, b - a - 2);
                int bar = inner.IndexOf('|');
                string zh = bar < 0 ? inner : inner.Substring(0, bar);
                string en = bar < 0 ? inner : inner.Substring(bar + 1);
                list.Add(new Seg { IsUi = true, Zh = zh, En = en });
                i = b + 2;
            }
            if (sb.Length > 0) list.Add(new Seg { IsUi = false, Zh = sb.ToString(), En = sb.ToString() });
            return list;
        }

        public static string Plain(string s)
        {
            var sb = new StringBuilder();
            foreach (var seg in Parse(s))
            {
                if (!seg.IsUi) sb.Append(seg.Zh);
                else if (seg.Zh == seg.En) sb.Append(seg.Zh);
                else sb.Append(seg.Zh).Append(' ').Append(seg.En);
            }
            return sb.ToString();
        }

        public static string ForMode(string s, UiMode mode)
        {
            var sb = new StringBuilder();
            foreach (var seg in Parse(s))
            {
                if (!seg.IsUi) { sb.Append(seg.Zh); continue; }
                sb.Append(mode switch
                {
                    UiMode.Chinese => seg.Zh,
                    UiMode.English => seg.En,
                    _ => seg.Zh + " · " + seg.En
                });
            }
            return sb.ToString();
        }
    }
}
