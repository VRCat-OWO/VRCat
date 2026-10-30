using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

namespace VRChatGuideSetup
{
    internal static class Program
    {
        public const string AppName = "模匠";
        public const string AppTitle = "模匠 · VRChat 改模指南";
        public const string AppVersion = "1.6.1";
        public const string AppExe = "MoJiang.exe";
        public const string RegKey = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\VRChatGuide";

        public static string DefaultDirOverride;

        public static string DefaultDir
        {
            get
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    @"Programs\模匠");
            }
        }

        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            bool silent = Has(args, "--silent") || Has(args, "/S");
            bool uninstall = Has(args, "--uninstall");

            foreach (var a in args)
            {
                if (a.StartsWith("--dir=", StringComparison.OrdinalIgnoreCase))
                    DefaultDirOverride = a.Substring(6).Trim('"');
            }

            if (uninstall) { Uninstaller.Run(silent); return; }

            if (silent)
            {
                int code = Installer.Install(DefaultDirOverride ?? DefaultDir, true, true);
                Environment.Exit(code);
                return;
            }

            Application.Run(new SetupForm());
        }

        static bool Has(string[] args, string flag)
        {
            foreach (var a in args)
                if (string.Equals(a, flag, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }
    }

    internal static class Installer
    {
        public static string DesktopDir
        {
            get { return Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory); }
        }

        public static string StartMenuDir
        {
            get
            {
                string d = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Programs), Program.AppName);
                Directory.CreateDirectory(d);
                return d;
            }
        }

        public static int Install(string dir, bool desktop, bool startMenu)
        {
            return Install(dir, desktop, startMenu, null, null);
        }

        public static int Install(string dir, bool desktop, bool startMenu,
            Action<int, string> progress, Func<bool> cancelled)
        {
            try
            {
                Directory.CreateDirectory(dir);

                using (var res = Assembly.GetExecutingAssembly().GetManifestResourceStream("payload.zip"))
                {
                    if (res == null)
                    {
                        MessageBox.Show("安装包不完整：找不到内置文件。请重新下载安装程序。",
                            Program.AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return 2;
                    }

                    using (var zip = new ZipArchive(res, ZipArchiveMode.Read))
                    {
                        int total = zip.Entries.Count;
                        int done = 0;
                        foreach (var e in zip.Entries)
                        {
                            if (cancelled != null && cancelled()) return 3;
                            done++;
                            string rel = e.FullName.Replace('/', Path.DirectorySeparatorChar);
                            string dest = Path.Combine(dir, rel);

                            if (e.FullName.EndsWith("/"))
                            {
                                Directory.CreateDirectory(dest);
                            }
                            else
                            {
                                string parent = Path.GetDirectoryName(dest);
                                if (!string.IsNullOrEmpty(parent)) Directory.CreateDirectory(parent);
                                using (var src = e.Open())
                                using (var dst = File.Create(dest))
                                {
                                    src.CopyTo(dst, 81920);
                                }
                            }

                            if (progress != null)
                                progress(done * 100 / Math.Max(1, total), rel);
                        }
                    }
                }

                string self = Assembly.GetExecutingAssembly().Location;
                string uninst = Path.Combine(dir, "Uninstall.exe");
                if (!File.Exists(uninst))
                {
                    try { File.Copy(self, uninst, true); } catch { }
                }

                string exe = Path.Combine(dir, Program.AppExe);
                if (desktop)
                    Shortcut.Create(Path.Combine(DesktopDir, Program.AppName + ".lnk"), exe, dir);
                if (startMenu)
                    Shortcut.Create(Path.Combine(StartMenuDir, Program.AppName + ".lnk"), exe, dir);

                WriteUninstallEntry(dir, exe, uninst);
                return 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show("安装失败：\n" + ex.Message, Program.AppName,
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return 1;
            }
        }

        static void WriteUninstallEntry(string dir, string exe, string uninst)
        {
            using (var k = Registry.CurrentUser.CreateSubKey(Program.RegKey))
            {
                k.SetValue("DisplayName", Program.AppTitle);
                k.SetValue("DisplayVersion", Program.AppVersion);
                k.SetValue("Publisher", "VRChat Modding Guide");
                k.SetValue("InstallLocation", dir);
                k.SetValue("DisplayIcon", exe + ",0");
                k.SetValue("UninstallString", "\"" + uninst + "\" --uninstall");
                k.SetValue("QuietUninstallString", "\"" + uninst + "\" --uninstall --silent");
                k.SetValue("NoModify", 1, RegistryValueKind.DWord);
                k.SetValue("NoRepair", 1, RegistryValueKind.DWord);
                k.SetValue("EstimatedSize", DirSizeKb(dir), RegistryValueKind.DWord);
            }
            using (var k = Registry.CurrentUser.CreateSubKey(@"Software\VRChatGuide"))
            {
                k.SetValue("InstallDir", dir);
                k.SetValue("Version", Program.AppVersion);
            }
        }

        public static int DirSizeKb(string dir)
        {
            try
            {
                long sum = 0;
                foreach (var f in Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories))
                {
                    try { sum += new FileInfo(f).Length; } catch { }
                }
                return (int)Math.Min(int.MaxValue, sum / 1024);
            }
            catch { return 0; }
        }
    }

    internal static class Uninstaller
    {
        public static void Run(bool silent)
        {
            string dir = Program.DefaultDirOverride;
            if (string.IsNullOrEmpty(dir))
            {
                try
                {
                    using (var k = Registry.CurrentUser.OpenSubKey(@"Software\VRChatGuide"))
                        if (k != null) dir = k.GetValue("InstallDir") as string;
                }
                catch { }
            }
            if (string.IsNullOrEmpty(dir))
                dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            if (!silent)
            {
                var r = MessageBox.Show(
                    "确定要卸载「" + Program.AppName + "」吗？\n\n安装位置：" + dir,
                    Program.AppName + " 卸载", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (r != DialogResult.Yes) return;
            }

            try
            {
                KillRunning();
                DeleteIfExists(Path.Combine(Installer.DesktopDir, Program.AppName + ".lnk"));
                RemoveStartMenu();
                Registry.CurrentUser.DeleteSubKeyTree(Program.RegKey, false);
                Registry.CurrentUser.DeleteSubKeyTree(@"Software\VRChatGuide", false);

                try
                {
                    string st = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        "VRChatModdingGuide");
                    if (Directory.Exists(st)) Directory.Delete(st, true);
                }
                catch { }

                Process.Start(new ProcessStartInfo("cmd.exe",
                    "/c timeout /t 2 >nul & rmdir /s /q \"" + dir + "\"")
                {
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true
                });

                if (!silent)
                    MessageBox.Show("卸载完成。", Program.AppName,
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                if (!silent)
                    MessageBox.Show("卸载时出现问题：\n" + ex.Message, Program.AppName,
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        static void KillRunning()
        {
            try
            {
                string[] names = new[] { "MoJiang", "VRChatGuide" };
                foreach (string nm in names)
                foreach (var p in Process.GetProcessesByName(nm))
                {
                    try { p.Kill(); p.WaitForExit(3000); } catch { }
                }
            }
            catch { }
        }

        static void DeleteIfExists(string path)
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
                catch { System.Threading.Thread.Sleep(120); }
            }
        }

        /// <summary>清掉开始菜单里的快捷方式与文件夹（资源管理器占用时重试）</summary>
        static void RemoveStartMenu()
        {
            try
            {
                string sm = Installer.StartMenuDir;
                if (!Directory.Exists(sm)) return;
                foreach (string f in Directory.GetFiles(sm, "*.lnk")) DeleteIfExists(f);
                for (int i = 0; i < 6; i++)
                {
                    try { Directory.Delete(sm, true); return; }
                    catch { System.Threading.Thread.Sleep(150); }
                }
            }
            catch { }
        }
    }

    internal static class Shortcut
    {
        public static void Create(string lnkPath, string target, string workDir)
        {
            try
            {
                Type shellType = Type.GetTypeFromProgID("WScript.Shell");
                if (shellType == null) return;
                object shell = Activator.CreateInstance(shellType);
                object lnk = shellType.InvokeMember("CreateShortcut",
                    BindingFlags.InvokeMethod, null, shell, new object[] { lnkPath });

                Type lt = lnk.GetType();
                lt.InvokeMember("TargetPath", BindingFlags.SetProperty, null, lnk, new object[] { target });
                lt.InvokeMember("WorkingDirectory", BindingFlags.SetProperty, null, lnk, new object[] { workDir });
                lt.InvokeMember("IconLocation", BindingFlags.SetProperty, null, lnk, new object[] { target + ",0" });
                lt.InvokeMember("Description", BindingFlags.SetProperty, null, lnk, new object[] { Program.AppName });
                lt.InvokeMember("Save", BindingFlags.InvokeMethod, null, lnk, null);
            }
            catch { }
        }
    }

    internal sealed class SetupForm : Form
    {
        const int W = 520, H = 318;

        readonly TextBox _dirBox;
        readonly CheckBox _desktop, _launch;
        readonly ProgressBar _bar;
        readonly Label _status;
        readonly Button _install, _close;
        volatile bool _busy;

        public SetupForm()
        {
            Text = Program.AppName + " 安装程序";
            ClientSize = new Size(W, H);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.White;
            Font = new Font("Microsoft YaHei UI", 9f);
            try { Icon = Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location); } catch { }

            // 顶部：图标 + 名字 + 一行副标题，就这三样
            var head = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(W, 66),
                BackColor = Color.FromArgb(31, 78, 121)
            };
            try
            {
                head.Controls.Add(new PictureBox
                {
                    Image = Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location).ToBitmap(),
                    Location = new Point(22, 17),
                    Size = new Size(32, 32),
                    SizeMode = PictureBoxSizeMode.Zoom,
                    BackColor = Color.Transparent
                });
            }
            catch { }
            head.Controls.Add(new Label
            {
                Text = Program.AppName,
                ForeColor = Color.White,
                Font = new Font("Microsoft YaHei UI", 15f, FontStyle.Bold),
                Location = new Point(64, 11),
                AutoSize = true,
                BackColor = Color.Transparent
            });
            head.Controls.Add(new Label
            {
                Text = "VRChat 改模指南 · 16 章 124 条 · 完全离线",
                ForeColor = Color.FromArgb(203, 219, 237),
                Font = new Font("Microsoft YaHei UI", 8.5f),
                Location = new Point(66, 40),
                AutoSize = true,
                BackColor = Color.Transparent
            });
            Controls.Add(head);

            int left = 24;
            int inner = W - 48;

            Controls.Add(new Label
            {
                Text = "安装到",
                Location = new Point(left, 82),
                AutoSize = true,
                ForeColor = Color.FromArgb(58, 70, 86)
            });

            _dirBox = new TextBox
            {
                Location = new Point(left, 102),
                Width = inner - 70,
                Text = Program.DefaultDirOverride ?? Program.DefaultDir,
                BorderStyle = BorderStyle.FixedSingle
            };
            Controls.Add(_dirBox);

            var browse = new Button
            {
                Text = "更改…",
                Location = new Point(left + inner - 64, 101),
                Width = 64,
                Height = 24,
                FlatStyle = FlatStyle.System
            };
            browse.Click += OnBrowse;
            Controls.Add(browse);

            _desktop = new CheckBox
            {
                Text = "桌面快捷方式",
                Location = new Point(left, 138),
                AutoSize = true,
                Checked = true
            };
            _launch = new CheckBox
            {
                Text = "装完就打开",
                Location = new Point(left + 170, 138),
                AutoSize = true,
                Checked = true
            };
            Controls.Add(_desktop);
            Controls.Add(_launch);

            _bar = new ProgressBar
            {
                Location = new Point(left, 176),
                Width = inner,
                Height = 8,
                Style = ProgressBarStyle.Continuous,
                Maximum = 100
            };
            Controls.Add(_bar);

            _status = new Label
            {
                Location = new Point(left, 192),
                Width = inner,
                Height = 22,
                Text = "准备就绪 · 不需要联网",
                ForeColor = Color.FromArgb(122, 136, 152)
            };
            Controls.Add(_status);

            var bottom = new Panel
            {
                Location = new Point(0, H - 58),
                Size = new Size(W, 58),
                BackColor = Color.FromArgb(248, 250, 253)
            };
            bottom.Paint += delegate (object s, PaintEventArgs e)
            {
                e.Graphics.DrawLine(new Pen(Color.FromArgb(223, 230, 239)), 0, 0, bottom.Width, 0);
            };
            Controls.Add(bottom);

            _install = new Button
            {
                Text = "安装",
                Width = 120,
                Height = 32,
                Location = new Point(W - 24 - 120, 13),
                BackColor = Color.FromArgb(31, 78, 121),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Microsoft YaHei UI", 9.5f)
            };
            _install.FlatAppearance.BorderSize = 0;
            _install.Click += OnInstall;

            _close = new Button
            {
                Text = "退出",
                Width = 84,
                Height = 32,
                Location = new Point(W - 24 - 120 - 8 - 84, 13),
                FlatStyle = FlatStyle.System
            };
            _close.Click += delegate { if (!_busy) Close(); };

            bottom.Controls.Add(_install);
            bottom.Controls.Add(_close);
        }

        void OnBrowse(object sender, EventArgs e)
        {
            using (var d = new FolderBrowserDialog())
            {
                d.Description = "选择安装文件夹";
                if (Directory.Exists(_dirBox.Text)) d.SelectedPath = _dirBox.Text;
                if (d.ShowDialog(this) == DialogResult.OK) _dirBox.Text = d.SelectedPath;
            }
        }

        void OnInstall(object sender, EventArgs e)
        {
            if (_busy) return;
            string dir = _dirBox.Text.Trim();
            if (dir.Length == 0)
            {
                MessageBox.Show("请填写安装位置。", Program.AppName);
                return;
            }

            try
            {
                Directory.CreateDirectory(dir);
                using (File.Create(Path.Combine(dir, ".write-test"))) { }
                File.Delete(Path.Combine(dir, ".write-test"));
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "这个文件夹不可写入：\n" + ex.Message + "\n\n请换一个位置（建议使用默认的当前用户目录）。",
                    Program.AppName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _busy = true;
            _install.Enabled = false;
            _close.Enabled = false;
            _dirBox.Enabled = false;
            _bar.Value = 0;
            _status.Text = "正在准备安装…";

            var th = new Thread(delegate ()
            {
                int code = Installer.Install(dir, _desktop.Checked, true,
                    delegate (int pct, string name)
                    {
                        BeginInvoke(new Action(delegate
                        {
                            _bar.Value = Math.Min(100, pct);
                            _status.Text = "正在释放文件： " + name;
                        }));
                    },
                    null);

                BeginInvoke(new Action(delegate
                {
                    _busy = false;
                    if (code == 0)
                    {
                        _bar.Value = 100;
                        _status.Text = "安装完成，可以开始使用了。";
                        _install.Text = "完成";
                        _install.Enabled = true;
                        _close.Enabled = true;
                        _install.Click -= OnInstall;
                        _install.Click += delegate
                        {
                            if (_launch.Checked) Launch(Path.Combine(dir, Program.AppExe), dir);
                            Close();
                        };
                        if (_launch.Checked)
                        {
                            Launch(Path.Combine(dir, Program.AppExe), dir);
                            Close();
                        }
                    }
                    else
                    {
                        _status.Text = "安装未完成";
                        _bar.Value = 0;
                        _install.Enabled = true;
                        _close.Enabled = true;
                    }
                }));
            });
            th.SetApartmentState(ApartmentState.STA);
            th.IsBackground = true;
            th.Start();
        }

        static void Launch(string exe, string dir)
        {
            try
            {
                Process.Start(new ProcessStartInfo(exe)
                {
                    WorkingDirectory = dir,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show("启动失败：\n" + ex.Message);
            }
        }
    }
}
