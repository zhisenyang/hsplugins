using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
#if HONEYSELECT
using Harmony;
#elif KOIKATSU || AISHOUJO
using HarmonyLib;
#endif
using UnityEngine;

namespace VideoExport.ScreenshotPlugins
{
    public interface IScreenshotPlugin
    {
        string name { get; }
        Vector2 currentSize { get; }
        bool transparency { get; }
        string extension { get; }

#if HONEYSELECT
        bool Init(HarmonyInstance harmony);
#elif KOIKATSU || AISHOUJO
        bool Init(Harmony harmony);
#endif
        byte[] Capture(bool forcePng = false);
        void DisplayParams();
        void SaveParams();
    }
}
