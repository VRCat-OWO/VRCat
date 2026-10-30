using System;
using System.Collections.Generic;
using System.Linq;

namespace VRChatGuide
{
    /// <summary>可点击的外链组</summary>
    public sealed class Links : Block
    {
        public (string Label, string Url, string Note)[] Items;

        public Links(params (string, string, string)[] items) { Items = items; }

        public override string Plain() => string.Join(" ", Items.Select(x => x.Label + " " + x.Note));
    }
}
