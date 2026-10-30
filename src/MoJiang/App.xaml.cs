using System;
using System.Windows;
using System.Windows.Threading;

namespace VRChatGuide
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            DispatcherUnhandledException += (s, ex) =>
            {
                MessageBox.Show(
                    "程序遇到一个错误：\n\n" + ex.Exception.Message +
                    "\n\n如果反复出现，请把这段信息发给作者。",
                    "VRChat 改模教程", MessageBoxButton.OK, MessageBoxImage.Warning);
                ex.Handled = true;
            };

                        foreach (var arg in e.Args)
            {
                if (string.Equals(arg, "--selftest", StringComparison.OrdinalIgnoreCase))
                {
                    ShutdownMode = ShutdownMode.OnExplicitShutdown;
                    int code = SelfTest.Run();
                    Shutdown(code);
                    return;
                }
            }

            bool noSplash = false;
            foreach (var arg in e.Args)
            {
                if (string.Equals(arg, "--nosplash", StringComparison.OrdinalIgnoreCase)) noSplash = true;
                if (string.Equals(arg, "--splash-test", StringComparison.OrdinalIgnoreCase))
                {
                    ShutdownMode = ShutdownMode.OnExplicitShutdown;
                    Splash.Qa(@"%TEMP%\vrcguide-splash");
                    return;
                }
            }

            State.Load();

            var win = new MainWindow();
            MainWindow = win;

            if (noSplash)
            {
                win.Show();
                return;
            }

            // 先播启动动画，结束后淡入主窗口
            Splash.Play(win);
        }
    }
}
