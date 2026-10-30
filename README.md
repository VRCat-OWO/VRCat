# 模匠 · VRChat 改模指南

VRChat 改模教程的 Windows 桌面客户端：16 章 / 124 条，完全离线，自带运行时。
本目录是整理后的**源码**，不含构建产物与演示素材。

```
VRCat/
├─ build.ps1                    一键构建（发布 → 卸载器 → 安装包 → 绿色版）
├─ src/
│  ├─ MoJiang/                  WPF 桌面程序（net8.0-windows，程序集 MoJiang.exe）
│  │  ├─ MoJiang.csproj         项目文件（AssemblyName=MoJiang，RootNamespace=VRChatGuide）
│  │  ├─ App.xaml / App.xaml.cs 应用入口 + 全局样式（GhostButton / SegButton / ChipToggle）
│  │  ├─ app.manifest           DPI 感知与 Win10/11 兼容声明
│  │  ├─ Core/                  与界面无关的地基
│  │  │  ├─ Models.cs           Category / Item / Block / TagKind / Way 等内容模型
│  │  │  ├─ State.cs            进度、勾选、主题持久化（%APPDATA%\VRChatModdingGuide）
│  │  │  ├─ Links.cs            外链常量（VCC 镜像、dwgx、爱发电等）
│  │  │  └─ Render.cs           区块渲染器：段落/列表/提示/代码/表格/图片/链接，含 ==重点== 解析
│  │  ├─ Shell/                 窗口与浏览外壳
│  │  │  ├─ MainWindow.cs       主窗口：侧栏、顶部开关、导航、搜索、赞助入口
│  │  │  ├─ SplashStart.cs      矢量启动动画（透明背景，约 1.75 秒）
│  │  │  └─ WebPage.cs          内嵌 WebView2 容器（VCC 中文镜像、外链页）
│  │  ├─ Content/               教程正文（纯数据，改内容只动这里）
│  │  │  ├─ Content.cs          装配入口 Build()：章节顺序、分组映射 GroupOf()
│  │  │  ├─ Content1–4.cs       第 1–10 章
│  │  │  ├─ Content5.cs         第 11–15 章（由 tools/content/import_dwgx.py 生成）
│  │  │  └─ ContentSponsor.cs   第 16 章「赞助作者」
│  │  ├─ Diagnostics/
│  │  │  └─ SelfTest.cs         --selftest 离屏渲染自检，输出截图与 log.txt
│  │  └─ Assets/                运行时资源（pack://application:,,,/Assets/…）
│  │     ├─ app.ico             多尺寸图标（16–256）
│  │     ├─ icon-1024.png / icon-b-1024.png   图标源图（A/B 两版）
│  │     └─ flow/hierarchy/merge/params/physbone/ranks.png   6 张图例
│  └─ Installer/                安装与卸载（.NET Framework 4.8，csc.exe 直编）
│     ├─ SetupApp.cs            安装窗口、静默安装 --silent --dir=…、注册表与快捷方式
│     └─ UninstallStub.cs       独立卸载器 Uninstall.exe（结束进程、删快捷方式/注册表/目录）
├─ tools/
│  ├─ content/                  正文生成与标注
│  │  ├─ import_dwgx.py         从 dwgx guides.js 生成第 11–15 章 → Content5.cs
│  │  ├─ mark_key_bullets.py    批量给要点加 ==重点== 标记
│  │  └─ mark_reference_items.py  标记参考资料类条目
│  ├─ assets/                   静态资源生成
│  │  ├─ make_diagrams.py       生成 6 张图例 PNG → src\MoJiang\Assets
│  │  ├─ make_icon.py           图标 A 版（头像 + 火花）
│  │  ├─ make_icon2.py          图标 A/B 版生成与切换（app.ico + 1024 预览）
│  │  └─ build_docx.py          教程 Markdown → docx（需要 docs\ 里的 md）
│  ├─ promo/                    宣传片与封面
│  │  ├─ make_promo_video.py    1920×1080/30fps 成片（帧走管道喂 ffmpeg）
│  │  ├─ make_promo_audio.py    配乐合成（numpy 环境垫音）
│  │  └─ make_promo_cover.py    B 站封面 1146×717
│  ├─ dev/check_parens.py       括号/引号配平检查
│  └─ legacy/                   历史一次性改造脚本（保留备查，正常构建不需要）
└─ .gitignore
```

## 构建

```powershell
# 需要 .NET SDK 8+；安装包与卸载器用 Windows 自带的 .NET Framework 4.8 csc.exe
cd VRCat
powershell -ExecutionPolicy Bypass -File .\build.ps1

# 只要编译验证，不产出安装包
dotnet build .\src\MoJiang\MoJiang.csproj -c Release
```

产物在 `dist\`：`模匠_安装程序.exe`、`模匠_绿色版.zip`、`app\`（安装包 payload 来源）。

## 运行参数

| 参数 | 作用 |
| --- | --- |
| 无 | 正常启动（带启动动画） |
| `--nosplash` | 跳过启动动画 |
| `--selftest` | 离屏渲染全部章节并截图，输出到 `%TEMP%\vrcguide-selftest` |
| `--splash-test` | 只跑启动动画并抓帧，输出到 `%TEMP%\vrcguide-splash` |

## 需要自备的输入数据（未纳入源码）

| 工具 | 需要的输入 |
| --- | --- |
| `tools/content/import_dwgx.py` | `input\src_dwgx\guides.js`（dwgx 资源中心原始数据） |
| `tools/promo/make_promo_video.py` | `input\promo-assets\` 下的界面截图（`--selftest` 产出后拷入） |
| `tools/assets/build_docx.py` | `docs\VRChat改模完全教程.md` |

缺输入时对应脚本跳过即可，`build.ps1` 已做存在性判断，不影响出包。

## 改动的常见入口

| 想改什么 | 动哪里 |
| --- | --- |
| 章节正文、提示、要点标注 | `src/MoJiang/Content/Content*.cs`（`==文字==` 即重点） |
| 章节顺序、分组 | `Content.cs` 的 `Build()` 与 `GroupOf()` |
| 顶部开关行为、导航、搜索 | `Shell/MainWindow.cs` |
| 区块排版、字号、间距 | `Core/Render.cs` |
| 启动动画时长与形态 | `Shell/SplashStart.cs`（`TotalMs` 与各时间点常量） |
| 安装窗口文案与选项 | `Installer/SetupApp.cs` |
| 卸载清理逻辑 | `Installer/UninstallStub.cs` |
| 图标 | `tools/assets/make_icon2.py` 重新生成到 `Assets\` |
