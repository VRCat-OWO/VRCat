using System;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Tutorial = VRChatGuide.Content;

namespace VRChatGuide
{
    /// <summary>
    /// 无人值守自检：把每个页面离屏渲染成 PNG，检查是否真的画出了东西。
    /// 用法：VRChatGuide.exe --selftest
    /// </summary>
    public static class SelfTest
    {
        public static int Run()
        {
            var dir = Path.Combine(Path.GetTempPath(), "vrcguide-selftest");
            Directory.CreateDirectory(dir);
            var log = new StringBuilder();
            int fails = 0;

            log.AppendLine("VRChat Guide self-test " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            log.AppendLine("pages=" + (Tutorial.All.Length + 3));

            MainWindow win = null;
            try
            {
                win = new MainWindow
                {
                    WindowStartupLocation = WindowStartupLocation.Manual,
                    Left = -20000,
                    Top = -20000,
                    ShowInTaskbar = false,
                    Width = 1360,
                    Height = 900
                };
                win.Show();
                win.UpdateLayout();

                int total = Tutorial.All.Length + 3;
                for (int i = 0; i < total; i++)
                {
                    try
                    {
                        win.GoTo(i);
                        win.UpdateLayout();
                        DoEvents();

                        string name = $"page{i:00}.png";
                        var stat = Capture(win, Path.Combine(dir, name));
                        log.AppendLine($"{name}  colors={stat.Item1}  nonWhite={stat.Item2:P2}  ok={(stat.Item1 > 40 ? "yes" : "NO")}");
                        if (stat.Item1 <= 40) fails++;
                    }
                    catch (Exception ex)
                    {
                        fails++;
                        log.AppendLine($"page{i:00} EXCEPTION {ex.GetType().Name}: {ex.Message}");
                    }
                }

                // 三种 Unity 界面模式各渲染一次（第 3 章，含大量菜单路径）
                foreach (var mode in new[] { UiMode.Both, UiMode.Chinese, UiMode.English })
                {
                    win.ApplyMode(mode);
                    win.GoTo(2);
                    win.UpdateLayout();
                    DoEvents();
                    string name = $"mode-{mode}.png";
                    var stat = Capture(win, Path.Combine(dir, name));
                    log.AppendLine($"{name}  colors={stat.Item1}  nonWhite={stat.Item2:P2}");
                    if (stat.Item1 <= 40) fails++;
                }

                // 搜索
                win.SearchTest("PhysBone");
                win.UpdateLayout();
                DoEvents();
                var s = Capture(win, Path.Combine(dir, "search.png"));
                log.AppendLine($"search.png  colors={s.Item1}  nonWhite={s.Item2:P2}");
                if (s.Item1 <= 40) fails++;

                // 两条改模路线：同为第 4 章，内容应完全不同
                foreach (var way in new[] { Way.MA, Way.Classic })
                {
                    win.ApplyWay(way);
                    win.GoTo(3);
                    win.UpdateLayout();
                    DoEvents();
                    string name = "way-" + (way == Way.MA ? "MA" : "Classic") + ".png";
                    var stat = Capture(win, Path.Combine(dir, name));
                    log.AppendLine($"{name}  colors={stat.Item1}  nonWhite={stat.Item2:P2}");
                    if (stat.Item1 <= 40) fails++;

                    var counts = new System.Text.StringBuilder();
                    foreach (var c in Tutorial.All)
                    {
                        counts.Append(c.CountFor(way)).Append(' ');
                        if (c.CountFor(way) == 0)
                        {
                            fails++;
                            log.AppendLine($"  空章节 {c.Badge} in {way}");
                        }
                    }
                    log.AppendLine($"  路线 {way} 各章条目数 = {counts.ToString().Trim()}");
                }
                win.ApplyWay(Way.MA);

                // 重点模式：每一条教程都必须至少有一处人工标注（提示条也算）
                win.ApplyHot(false);
                win.GoTo(2);
                win.UpdateLayout();
                DoEvents();
                var off = Capture(win, Path.Combine(dir, "hot-off.png"));

                win.ApplyHot(true);
                win.GoTo(2);
                win.UpdateLayout();
                DoEvents();
                int hot = Render.HotCount;
                var on = Capture(win, Path.Combine(dir, "hot-on.png"));
                log.AppendLine($"hot-off.png  colors={off.Item1}  hot=0");
                log.AppendLine($"hot-on.png   colors={on.Item1}  重点片段={hot}");
                if (hot <= 0) { fails++; log.AppendLine("  重点模式没有命中任何片段"); }
                if (on.Item1 <= off.Item1) { fails++; log.AppendLine("  重点模式开启后颜色数没有增加"); }

                // 逐条检查：121 条教程里不能有「开重点模式什么也没标」的
                int unmarked = 0, marks = 0, tipItems = 0;
                foreach (var c in Tutorial.All)
                    foreach (var it in c.Items)
                    {
                        int n = Render.CountMarks(it);
                        int tips = 0;
                        foreach (var b in it.Blocks) if (b is Tip) tips++;
                        if (tips > 0) tipItems++;
                        marks += n;
                        if (n + tips == 0)
                        {
                            unmarked++;
                            log.AppendLine($"  未标注重点: {c.Badge} {it.Title}");
                        }
                    }
                log.AppendLine($"人工重点: 显式标注={marks} 处，含提示条的条目={tipItems}，未标注条目={unmarked}");
                if (unmarked > 0) fails++;

                // 统计全书重点命中数（离线遍历所有页面，不截图）
                int hotTotal = 0, hotPages = 0;
                for (int i = 0; i < Tutorial.All.Length; i++)
                {
                    win.GoTo(i);
                    win.UpdateLayout();
                    DoEvents();
                    if (Render.HotCount > 0) hotPages++;
                    hotTotal += Render.HotCount;
                }
                log.AppendLine($"全书重点标注合计={hotTotal}  命中页={hotPages}/{Tutorial.All.Length}");
                if (hotTotal < 100) { fails++; log.AppendLine("  重点标注偏少（<100）"); }
                win.ApplyHot(false);

                // 内容完整性：每条教程都要有标题 + 至少两个块，且正文不为空
                int items = 0, blocks = 0, thin = 0, empty = 0;
                foreach (var c in Tutorial.All)
                {
                    foreach (var it in c.Items)
                    {
                        items++;
                        blocks += it.Blocks.Length;
                        if (it.Blocks.Length == 0) { thin++; log.AppendLine($"  内容过少: {c.Badge} {it.Title}"); }
                        if (it.Plain().Trim().Length < 20) { empty++; log.AppendLine($"  内容为空: {c.Badge} {it.Title}"); }
                        if (it.Title.Trim().Length == 0) { fails++; log.AppendLine($"  缺标题: {c.Badge}"); }
                    }
                }
                if (thin > 0) fails++;
                if (empty > 0) fails++;
                log.AppendLine($"内容: 章节={Tutorial.All.Length} 条目={items} 块={blocks} 过少={thin} 空={empty}");
            }
            catch (Exception ex)
            {
                fails++;
                log.AppendLine("FATAL " + ex);
            }
            finally
            {
                try { win?.Close(); } catch { }
            }

            log.AppendLine(fails == 0 ? "RESULT: PASS" : $"RESULT: FAIL ({fails})");
            File.WriteAllText(Path.Combine(dir, "log.txt"), log.ToString(), Encoding.UTF8);
            Console.WriteLine(log.ToString());
            return fails == 0 ? 0 : 1;
        }

        static void DoEvents()
        {
            var frame = new System.Windows.Threading.DispatcherFrame();
            System.Windows.Threading.Dispatcher.CurrentDispatcher.BeginInvoke(
                System.Windows.Threading.DispatcherPriority.Background,
                new Action(() => frame.Continue = false));
            System.Windows.Threading.Dispatcher.PushFrame(frame);
        }

        /// <summary>返回 (颜色种类数, 非白像素比例)</summary>
        static (int, double) Capture(Window win, string path)
        {
            var root = (FrameworkElement)win.Content;
            int w = Math.Max(800, (int)root.ActualWidth);
            int h = Math.Max(600, (int)root.ActualHeight);

            var rtb = new RenderTargetBitmap(w, h, 96, 96, PixelFormats.Pbgra32);
            var dv = new DrawingVisual();
            using (var dc = dv.RenderOpen())
            {
                dc.DrawRectangle(win.Background ?? Brushes.White, null, new Rect(0, 0, w, h));
                dc.DrawRectangle(new VisualBrush(root), null, new Rect(0, 0, w, h));
            }
            rtb.Render(dv);

            var enc = new PngBitmapEncoder();
            enc.Frames.Add(BitmapFrame.Create(rtb));
            using (var fs = File.Create(path)) enc.Save(fs);

            // 统计
            int stride = w * 4;
            var px = new byte[stride * h];
            rtb.CopyPixels(px, stride, 0);

            var set = new System.Collections.Generic.HashSet<int>();
            long nonWhite = 0;
            int step = 3;
            long samples = 0;
            for (int y = 0; y < h; y += step)
            {
                for (int x = 0; x < w; x += step)
                {
                    int o = y * stride + x * 4;
                    byte b = px[o], g = px[o + 1], r = px[o + 2];
                    set.Add((r >> 2 << 12) | (g >> 2 << 6) | (b >> 2));
                    samples++;
                    if (r < 240 || g < 240 || b < 240) nonWhite++;
                }
            }
            return (set.Count, samples == 0 ? 0 : (double)nonWhite / samples);
        }
    }
}
