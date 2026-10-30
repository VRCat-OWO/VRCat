# 模匠 · VRChat 改模指南 —— 一键构建
#
# 用法（在 VRCat 目录下）：
#   powershell -ExecutionPolicy Bypass -File .\build.ps1
#   powershell -ExecutionPolicy Bypass -File .\build.ps1 -SkipAssets   # 跳过图例/图标重生成
#
# 产物（全部落在 .\dist\）：
#   模匠_安装程序.exe      安装包（内嵌 payload.zip，约 64 MB）
#   模匠_绿色版.zip        免安装绿色版
#   app\                  发布输出（安装包的 payload 来源）
#
# 前置：.NET SDK 8+ ；卸载器与安装包用 .NET Framework 4.8 的 csc.exe 编译
#       （Windows 自带，无需额外安装）

param(
    [switch]$SkipAssets
)

$ErrorActionPreference = "Stop"

$root  = $PSScriptRoot
$proj  = Join-Path $root "src\MoJiang"          # WPF 应用
$inst  = Join-Path $root "src\Installer"        # 安装器 / 卸载器源码
$dist  = Join-Path $root "dist"                 # 构建产物
$stage = Join-Path $dist "app"
$icon  = Join-Path $proj "Assets\app.ico"
$csc   = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"

$version = "1.6.1"
$summary = "16 章 / 124 条教程 / 6 张图例 / 中英 Unity 界面对照 / MA·古法双路线 / 内嵌 VCC 中文镜像"

New-Item -ItemType Directory -Path $dist -Force | Out-Null

# ── 1. 素材（图例 PNG） ────────────────────────────────────────────────
$py = Get-Command python -ErrorAction SilentlyContinue
if (-not $SkipAssets -and $py) {
    Write-Host "[1/6] 生成图例 PNG ..." -ForegroundColor Cyan
    & python (Join-Path $root "tools\assets\make_diagrams.py")
} else {
    Write-Host "[1/6] 跳过图例生成（用 src\MoJiang\Assets 里已有的 PNG）" -ForegroundColor DarkGray
}

# ── 1.5 可选：从 dwgx 原始数据重新生成 11–15 章 ────────────────────────
$dwgx = Join-Path $root "input\src_dwgx\guides.js"
if ((Test-Path $dwgx) -and $py) {
    Write-Host "[1.5/6] 重新生成 11–15 章（dwgx 精选指南） ..." -ForegroundColor Cyan
    & python (Join-Path $root "tools\content\import_dwgx.py")
}

# ── 2. 发布自包含程序 ──────────────────────────────────────────────────
Write-Host "[2/6] 发布自包含 exe（win-x64） ..." -ForegroundColor Cyan
Push-Location $proj
& dotnet publish -c Release -r win-x64 --self-contained true `
    -p:PublishSingleFile=false -p:DebugType=none -o $stage
Pop-Location

# ── 3. 运行时说明 ──────────────────────────────────────────────────────
Write-Host "[3/6] 生成 README.txt ..." -ForegroundColor Cyan
$readme = @"
模匠 · VRChat 改模指南 v$version
================================
离线桌面教程：$summary

运行要求：Windows 10/11 x64（自带 .NET 运行时，不需要另装）
数据位置：%APPDATA%\VRChatModdingGuide\state.json（进度、勾选、主题）
卸载：设置 → 应用 → 模匠，或运行安装目录下的 Uninstall.exe
"@
[System.IO.File]::WriteAllText((Join-Path $stage "README.txt"), $readme,
    (New-Object System.Text.UTF8Encoding($true)))

# ── 4. 轻量卸载器 ──────────────────────────────────────────────────────
Write-Host "[4/6] 编译卸载程序 ..." -ForegroundColor Cyan
& $csc /nologo /target:winexe /platform:anycpu /optimize+ /codepage:65001 `
    /win32icon:"$icon" `
    /r:System.dll /r:System.Core.dll /r:System.Windows.Forms.dll /r:System.Drawing.dll `
    /out:"$stage\Uninstall.exe" (Join-Path $inst "UninstallStub.cs")

# ── 5. payload + 安装程序 ──────────────────────────────────────────────
Write-Host "[5/6] 打包 payload 并编译安装程序 ..." -ForegroundColor Cyan
Add-Type -AssemblyName System.IO.Compression.FileSystem
$payload = Join-Path $dist "payload.zip"
Remove-Item $payload -Force -ErrorAction SilentlyContinue
[System.IO.Compression.ZipFile]::CreateFromDirectory($stage, $payload,
    [System.IO.Compression.CompressionLevel]::Optimal, $false)

& $csc /nologo /target:winexe /platform:anycpu /optimize+ /codepage:65001 `
    /win32icon:"$icon" `
    /resource:"$payload",payload.zip `
    /r:System.dll /r:System.Core.dll /r:System.Windows.Forms.dll /r:System.Drawing.dll `
    /r:System.IO.Compression.dll /r:System.IO.Compression.FileSystem.dll /r:Microsoft.CSharp.dll `
    /out:"$dist\模匠_安装程序.exe" (Join-Path $inst "SetupApp.cs")

Remove-Item $payload -Force -ErrorAction SilentlyContinue

# ── 6. 绿色版 ──────────────────────────────────────────────────────────
Write-Host "[6/6] 生成绿色版 zip ..." -ForegroundColor Cyan
$tmp = Join-Path $env:TEMP ("mojiang_" + [guid]::NewGuid().ToString("N").Substring(0, 6))
New-Item -ItemType Directory -Path $tmp | Out-Null
Copy-Item "$stage\*" $tmp -Recurse -Force
Remove-Item "$tmp\Uninstall.exe", "$tmp\README.txt" -Force -ErrorAction SilentlyContinue
$usage = @"
模匠 · VRChat 改模指南 v$version（绿色版）
==========================================
1. 整个文件夹放哪都行，双击 MoJiang.exe 运行
2. 不需要安装，不需要联网，不写注册表
3. 进度保存在 %APPDATA%\VRChatModdingGuide\state.json

启动参数：
  --nosplash        跳过启动动画
  --selftest        离屏自检，输出截图与 log.txt 到 %TEMP%\vrcguide-selftest
  --splash-test     只跑启动动画并抓帧到 %TEMP%\vrcguide-splash
"@
[System.IO.File]::WriteAllText((Join-Path $tmp "使用说明.txt"), $usage,
    (New-Object System.Text.UTF8Encoding($true)))
[System.IO.Compression.ZipFile]::CreateFromDirectory($tmp,
    (Join-Path $dist "模匠_绿色版.zip"),
    [System.IO.Compression.CompressionLevel]::Optimal, $false)
Remove-Item $tmp -Recurse -Force

Get-ChildItem $dist -File | Select-Object Name, @{n = 'MB'; e = { [math]::Round($_.Length / 1MB, 1) } } |
    Format-Table -Auto
Write-Host "完成。" -ForegroundColor Green
