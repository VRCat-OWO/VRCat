using System;

namespace VRChatGuide
{
    public static partial class Content
    {
        /// <summary>作者的爱发电主页（带来源参数，便于统计入口）</summary>
        public const string SponsorUrl = "https://afdian.com/a/7891mek?utm_source=copylink&utm_medium=link";

        /// <summary>第 16 章：赞助入口（纯离线页面，按钮调系统浏览器打开爱发电）</summary>
        public static Category Cat16()
        {
            return new Category("16", "赞助作者", "爱发电 · 让这份手册继续更新",
                "如果它帮你省下了几个小时，可以请作者喝一杯",

                new Item("为什么会有赞助入口", TagKind.Basic,
                    new Para("「模匠」是完全离线的免费手册：没有广告、没有登录、没有功能阉割，下载下来就能一直用。"),
                    new Bullets(false,
                        "手册里的示例操作要跟着新版 SDK 重跑一遍，每轮大更新大约是几十个小时的核对。",
                        "示意图、截图要做到能看清菜单名，需要专门摆场景重拍。",
                        "为了保持判断尽量中立，里面所有推荐的工具都只用官方或免费渠道的版本。"),
                    new Tip("经验", "==如果这份手册帮你少熬一个通宵，那它就是有用的==。赞助与否，都不影响你使用任何一章内容。")),

                new Item("赞助入口：爱发电", TagKind.Tool,
                    new Para("点下面的按钮会用系统浏览器打开作者的爱发电主页，微信 / 支付宝都可以，金额随意，也可以只留一句话。"),
                    new Links(
                        ("去爱发电赞助模匠", SponsorUrl, "在浏览器打开爱发电主页"),
                        ("复制链接到手机", SponsorUrl, "同一个地址，手机浏览器也能打开")),
                    new Para("主页地址（可直接复制）：" + SponsorUrl),
                    new Tip("提示", "==顶部「♥ 赞助作者」按钮、左侧导航第 16 章，都会回到这一页==；地址本身也不会变，收藏一次就够。")),

                new Item("赞助与不赞助", TagKind.Basic,
                    new Bullets(true,
                        "赞助不换取任何特权：章节、重点标注、离线包，全部一视同仁。",
                        "不赞助也能用全部内容：这页存在的意义只是给愿意支持的人一个入口。",
                        "想加什么章节、哪里写错了，都可以在爱发电主页留言，和赞助无关也会看。",
                        "手滑了想退，直接在爱发电里申请，不用跟作者解释。"),
                    new Para("除了赞助，帮忙把手册转给正在被改模折磨的人，也是很有用的支持。"),
                    new Tip("注意", "==赞助完全自愿，不弹窗、不提醒、不影响任何功能==。这一章你不点开，程序也不会来打扰你。")));
        }
    }
}
