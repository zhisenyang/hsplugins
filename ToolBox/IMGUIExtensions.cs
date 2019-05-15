using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace ToolBox
{
    public static class IMGUIExtensions
    {
        private static int _cachedFontSize;

        public static void SetFontSize(int size)
        {
            _cachedFontSize = GUI.skin.label.fontSize;
            foreach (GUIStyle style in GUI.skin)
            {
                style.fontSize = size;
            }
        }

        public static void ResetFontSize()
        {
            foreach (GUIStyle style in GUI.skin)
            {
                style.fontSize = _cachedFontSize;
            }
        }
    }
}
