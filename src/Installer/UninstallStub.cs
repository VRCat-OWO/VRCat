using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

namespace VRChatGuideSetup
{
    /// <summary>
    /// 独立的卸载器（随安装一起释放到安装目录）。
    /// 只做四件事：结束正在运行的进程、删快捷方式、删注册表项、交棒给 cmd 删目录。
    /// 名字必须与 SetupApp.cs 里的 Program.AppName / AppExe 保持一致。
    /// </summary>
    internal static class UninstallMain
    {
        const string AppName = "模匠";
        const string ExeStem = "MoJiang";
        const string RegKey = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\VRChatGuide";
        const string LegacyReg = @"Software\VRChatGuide";

        [STAThread]
        static void Main(string[] args)
        {
            bool silent = false;
            string dir = null;
            foreach (string a in args)
            {
                if (string.Equals(a, "--silent", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(a, "/S", StringComparison.OrdinalIgnoreCase)) silent = true;
                if (a.StartsWith("--dir=", StringComparison.OrdinalIgnoreCase))
                    dir = a.Substring(6).Trim('"');
            }

            if (string.IsNullOrEmpty(dir))
            {
                try
                {
                    using (var k = Registry.CurrentUser.OpenSubKey(LegacyReg))
                        if (k != null) dir = k.GetValue("InstallDir") as string;
                }
                catch { }
            }
            if (string.IsNullOrEmpty(dir))
                dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            if (!silent)
            {
                var r = MessageBox.Show(
                    "确定要卸载「" + AppName + "」吗？\n\n安装位置：" + dir,
                    AppName + " 卸载", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (r != DialogResult.Yes) return;
            }

            KillRunning();

            string myExe = Assembly.GetExecutingAssembly().Location;
            DeleteFile(Path.Combine(DesktopDir(), AppName + ".lnk"));
            DeleteFile(Path.Combine(DesktopDir(), "VRChat 改模教程.lnk"));   // 旧名残留
            RemoveShortcutDir(Path.Combine(StartMenuRoot(), AppName));
            RemoveShortcutDir(Path.Combine(StartMenuRoot(), "VRChat 改模教程"));

            try { Registry.CurrentUser.DeleteSubKeyTree(RegKey, false); } catch { }
            try { Registry.CurrentUser.DeleteSubKeyTree(LegacyReg, false); } catch { }

            try
            {
                string st = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "VRChatModdingGuide");
                if (Directory.Exists(st)) Directory.Delete(st, true);
            }
            catch { }

            // 自己也在要删的目录里：交给 cmd 等两秒再删，避开文件占用
            try
            {
                Process.Start(new ProcessStartInfo("cmd.exe",
                    "/c timeout /t 2 >nul & rmdir /s /q \"" + dir + "\"")
                {
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true
                });
            }
            catch { }

            if (!silent)
                MessageBox.Show("卸载完成。", AppName, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        static string DesktopDir()
        {
            return Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        }

        static string StartMenuRoot()
        {
            return Environment.GetFolderPath(Environment.SpecialFolder.Programs);
        }

        static void KillRunning()
        {
            foreach (string nm in new[] { ExeStem, "VRChatGuide" })
            {
                try
                {
                    foreach (var p in Process.GetProcessesByName(nm))
                    {
                        try { p.Kill(); p.WaitForExit(3000); } catch { }
                    }
                }
                catch { }
            }
        }

        static void DeleteFile(string path)
        {
            for (int i = 0; i < 6; i++)
            {
                try
                {
                    if (!File.Exists(path)) return;
                    File.SetAttributes(path, FileAttributes.Normal);
                    File.Delete(path);
                    return;
                }
                catch { Thread.Sleep(120); }
            }
        }

        static void RemoveShortcutDir(string dir)
        {
            try
            {
                if (!Directory.Exists(dir)) return;
                foreach (string f in Directory.GetFiles(dir, "*.lnk")) DeleteFile(f);
                for (int i = 0; i < 6; i++)
                {
                    try { Directory.Delete(dir, true); return; }
                    catch { Thread.Sleep(150); }
                }
            }
            catch { }
        }
    }
}
