using System;
using UnityEngine;

namespace ToolBox.Extensions {
    internal static class IMGUIExtensions
    {
        public static void SetGlobalFontSize(int size)
        {
            foreach (GUIStyle style in GUI.skin)
            {
                style.fontSize = size;
            }
            GUI.skin = GUI.skin;
        }

        public static void ResetFontSize()
        {
            SetGlobalFontSize(0);
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