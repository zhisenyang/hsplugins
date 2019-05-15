using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace VideoExport.ScreenshotPlugins
{
    public interface IScreenshotPlugin
    {
        string name { get; }
        Vector2 currentSize { get; }
        bool transparency { get; }
        string extension { get; }

        bool Init();
        byte[] Capture(bool forcePng = false);
        void DisplayParams();
        void SaveParams();
    }
}
