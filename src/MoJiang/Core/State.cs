using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace VRChatGuide
{
    public sealed class StateData
    {
        public int UiMode { get; set; } = 0;
        public int Way { get; set; } = 1;          // 1 = MA 插件，2 = 古法
        public bool Hot { get; set; } = false;     // 重点模式：高亮正文里的关键句
        public double FontScale { get; set; } = 1.0;
        public int LastNav { get; set; } = 0;
        public double WinW { get; set; } = 1320;
        public double WinH { get; set; } = 860;
        public bool WinMax { get; set; } = false;
        public List<string> Checked { get; set; } = new List<string>();
    }

    public static class State
    {
        public static StateData Data = new StateData();

        public static string Dir =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "VRChatModdingGuide");

        static string File_ => Path.Combine(Dir, "state.json");

        public static UiMode Mode
        {
            get => (UiMode)Math.Clamp(Data.UiMode, 0, 2);
            set => Data.UiMode = (int)value;
        }

        /// <summary>当前改模路线（MA 插件 / 古法），只允许这两个值</summary>
        public static Way Workflow
        {
            get => Data.Way == 2 ? Way.Classic : Way.MA;
            set => Data.Way = (int)value;
        }

        /// <summary>重点模式：开启后正文里判定为关键句的片段会高亮</summary>
        public static bool HotMode
        {
            get => Data.Hot;
            set => Data.Hot = value;
        }

        public static void Load()
        {
            try
            {
                if (File.Exists(File_))
                {
                    var json = File.ReadAllText(File_);
                    var d = JsonSerializer.Deserialize<StateData>(json);
                    if (d != null)
                    {
                        if (d.FontScale < 0.85 || d.FontScale > 1.35) d.FontScale = 1.0;
                        if (d.WinW < 1000 || d.WinW > 8000) d.WinW = 1320;
                        if (d.WinH < 640 || d.WinH > 5000) d.WinH = 860;
                        Data = d;
                    }
                }
            }
            catch { /* 状态文件损坏时忽略，用默认值 */ }
        }

        public static void Save()
        {
            try
            {
                Directory.CreateDirectory(Dir);
                File.WriteAllText(File_, JsonSerializer.Serialize(Data,
                    new JsonSerializerOptions { WriteIndented = true }));
            }
            catch { }
        }

        public static bool IsChecked(string key) => Data.Checked.Contains(key);

        public static void SetChecked(string key, bool value)
        {
            if (value)
            {
                if (!Data.Checked.Contains(key)) Data.Checked.Add(key);
            }
            else Data.Checked.Remove(key);
        }
    }
}
