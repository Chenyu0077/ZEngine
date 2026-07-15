using System;
using System.Collections.Generic;
using FairyGUI;

namespace ZEngine.Manager.UI
{
    public class EmojiUtil
    {
        public static Dictionary<uint, Emoji> GlobalEmojies { get; private set; }

        void InitEmojies()
        {
            GlobalEmojies = new Dictionary<uint, Emoji>();
            // 按你实际导入的码点范围添加
            for (uint i = 0x1f600; i <= 0x1f64f; i++)  // 😀~🙏
            {
                string url = UIPackage.GetItemURL("Emoji", Convert.ToString(i, 16));
                if (url != null)
                    GlobalEmojies.Add(i, new Emoji(url));
            }
        }

    }
}