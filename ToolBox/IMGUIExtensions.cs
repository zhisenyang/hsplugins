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

        public static void HorizontalSliderWithValue(string label, float value, float left, float right, string valueFormat = "", Action<float> onChanged = null)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.ExpandWidth(false));
            float newValue = GUILayout.HorizontalSlider(value, left, right);
            string valueString = newValue.ToString(valueFormat);
            string newValueString = GUILayout.TextField(valueString, 5, GUILayout.Width(50f));

            if (newValueString != valueString)
            {
                float parseResult;
                if (float.TryParse(newValueString, out parseResult))
                    newValue = parseResult;
            }
            GUILayout.EndHorizontal();

            if (onChanged != null && !Mathf.Approximately(value, newValue))
                onChanged(newValue);
        }
    }
}
