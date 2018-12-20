using UnityEngine;
using IllusionPlugin;
using System;
//using System.IO;
namespace HSIBL
{
    public static class UIUtils
    {
        internal static void InitStyle()
        {
            scale.x = UnityEngine.Screen.width / Screen.width;
            scale.y = UnityEngine.Screen.height / Screen.height;
            scale.z = 1f;


            myfont = Font.CreateDynamicFontFromOSFont(new string[] { "Segeo UI", "Microsoft YaHei UI", "Microsoft YaHei" }, 20);
            toggleButtonOn = new GUIStyle(GUI.skin.button)
            {
                fontStyle = FontStyle.Bold,
                stretchHeight = false,
                stretchWidth = false,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = false,
                font = myfont,
                margin = new RectOffset(4, 4, 4, 4),
                padding = new RectOffset(6, 6, 6, 12),
                fontSize = 22
            };
            toggleButtonOff = new GUIStyle(GUI.skin.button)
            {
                fontStyle = FontStyle.Bold,
                stretchHeight = false,
                stretchWidth = false,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = false,
                font = myfont,
                margin = new RectOffset(4, 4, 4, 4),
                padding = new RectOffset(6, 6, 6, 12),
                fontSize = 22
            };
            toggleButtonOn.onNormal.textColor = selected;
            toggleButtonOn.onHover.textColor = selectedOnHover;
            toggleButtonOn.normal = toggleButtonOn.onNormal;
            toggleButtonOn.hover = toggleButtonOn.onHover;
            boxstyle = new GUIStyle(GUI.skin.box)
            {
                stretchHeight = false,
                stretchWidth = true,
                alignment = TextAnchor.MiddleLeft,
                wordWrap = true,
                font = myfont,
                fontSize = 22,
                padding = new RectOffset(6, 6, 6, 12)
            };
            windowstyle = new GUIStyle(GUI.skin.window);
            selectstyle = new GUIStyle(GUI.skin.button)
            {
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                fontSize = 22,
                padding = new RectOffset(6, 6, 6, 12),
                margin = new RectOffset(4, 4, 4, 4),
                font = myfont
            };
            selectstyle.onNormal.textColor = selected;
            selectstyle.onHover.textColor = selectedOnHover;
            buttonstyleNoStretch = new GUIStyle(GUI.skin.button)
            {
                fontStyle = FontStyle.Bold,
                stretchHeight = false,
                stretchWidth = false,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = false,
                font = myfont,
                padding = new RectOffset(12, 12, 6, 12),
                margin = new RectOffset(4, 4, 4, 4),
                fontSize = 22
            };

            textFieldStyle = new GUIStyle(GUI.skin.textField)
            {
                fontStyle = FontStyle.Bold,
                font = myfont,
                padding = new RectOffset(6, 6, 6, 12),
                margin = new RectOffset(4, 4, 4, 4),
                fontSize = 22,
                alignment = TextAnchor.MiddleRight
            };

            textFieldStyle2 = new GUIStyle(GUI.skin.textField)
            {
                fontStyle = FontStyle.Bold,
                font = myfont,
                padding = new RectOffset(6, 6, 6, 12),
                margin = new RectOffset(4, 4, 4, 4),
                fontSize = 22,
                alignment = TextAnchor.MiddleLeft
            };

            sliderstyle = new GUIStyle(GUI.skin.horizontalSlider)
            {
                padding = new RectOffset(-10, -10, -4, -4),
                fixedHeight = 16f,
                margin = new RectOffset(22, 22, 22, 22)
            };
            thumbstyle = new GUIStyle(GUI.skin.horizontalSliderThumb)
            {
                fixedHeight = 24f,
                padding = new RectOffset(14, 14, 12, 12)

            };
            labelstyle2 = new GUIStyle(GUI.skin.label)
            {
                font = myfont,
                fontSize = 20,
                margin = new RectOffset(16, 16, 8, 8)
            };
            titlestyle = new GUIStyle(GUI.skin.button)
            {
                fontStyle = FontStyle.Bold,
                font = myfont,
                fontSize = 30,
                padding = new RectOffset(6, 6, 6, 12),
                margin = new RectOffset(4, 4, 4, 4),
                alignment = TextAnchor.MiddleCenter
            };
            titlestyle.onNormal.textColor = selected;
            titlestyle.onHover.textColor = selectedOnHover;
            titlestyle2 = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold,
                font = myfont,
                fontSize = 30,
                padding = new RectOffset(6, 6, 6, 12),
            };
            labelstyle = new GUIStyle(GUI.skin.label)
            {
                font = myfont,
                fontSize = 24,
                padding = new RectOffset(6, 6, 6, 12),
                alignment = TextAnchor.MiddleLeft
            };
            buttonstyleStrechWidth = new GUIStyle(GUI.skin.button)
            {
                stretchHeight = false,
                stretchWidth = true,
                wordWrap = true,
                fontStyle = FontStyle.Bold,
                font = myfont,
                fontSize = 22,
                margin = new RectOffset(10, 10, 5, 5),
                padding = new RectOffset(6, 6, 6, 12)
            };
            buttonstyleStrechWidth.onNormal.textColor = selected;
            buttonstyleStrechWidth.onHover.textColor = selectedOnHover;
            labelstyle3 = new GUIStyle(GUI.skin.label)
            {
                wordWrap = true,
                fontSize = 22
            };

            space = 12f;
            minwidth = Mathf.Round(0.27f * Screen.width);
            styleInitialized = true;

            windowRect.x = ModPrefs.GetFloat("HSIBL", "Window.x", windowRect.x, true);
            windowRect.y = ModPrefs.GetFloat("HSIBL", "Window.y", windowRect.y, true);
            windowRect.width = Mathf.Min(Screen.width - 10f, ModPrefs.GetFloat("HSIBL", "Window.width", windowRect.width, true));
            windowRect.height = Mathf.Min(Screen.height - 10f, ModPrefs.GetFloat("HSIBL", "Window.height", windowRect.height, true));
        }

        internal static float SliderGUI(float value, float min, float max, string labeltext, string valuedecimals)
        {
            return SliderGUI(value, min, max, labeltext, "", valuedecimals);
        }
        internal static float SliderGUI(float value, float min, float max, GUIContent guiContent, string valuedecimals)
        {
            GUILayout.Label(guiContent, labelstyle);
            GUILayout.BeginHorizontal();
            value = GUILayout.HorizontalSlider(value, min, max, sliderstyle, thumbstyle);
            if (float.TryParse(GUILayout.TextField(value.ToString(valuedecimals), textFieldStyle, GUILayout.Width(90)), out float newValue))
                value = newValue;
            GUILayout.EndHorizontal();
            return value;
        }
        internal static float SliderGUI(float value, float min, float max, string labeltext, string tooltip, string valuedecimals)
        {
            GUILayout.Label(new GUIContent(labeltext, tooltip), labelstyle);
            GUILayout.BeginHorizontal();
            value = GUILayout.HorizontalSlider(value, min, max, sliderstyle, thumbstyle);
            if (float.TryParse(GUILayout.TextField(value.ToString(valuedecimals), textFieldStyle, GUILayout.Width(90)), out float newValue))
                value = newValue;
            GUILayout.EndHorizontal();
            return value;
        }
        internal static float SliderGUI(float value, float min, float max, float reset, string labeltext, string valuedecimals)
        {
            return SliderGUI(value, min, max, reset, labeltext, "", valuedecimals);
        }

        internal static float SliderGUI(float value, float min, float max, Func<float> reset, string labeltext, string valuedecimals)
        {
            return SliderGUI(value, min, max, reset, labeltext, "", valuedecimals);
        }
        internal static float SliderGUI(float value, float min, float max, Func<float> reset, GUIContent label, string valuedecimals)
        {
            return SliderGUI(value, min, max, reset, label.text, label.tooltip, valuedecimals);
        }
        internal static float SliderGUI(float value, float min, float max, Func<float> reset, string labeltext, string tooltip, string valuedecimals)
        {
            if (reset == null)
            {
                throw new ArgumentNullException(nameof(reset));
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label(new GUIContent(labeltext, tooltip), labelstyle);
            GUILayout.Space(space);
            if (GUILayout.Button(new GUIContent(GUIStrings.Reset, tooltip), buttonstyleNoStretch))
            {
                value = reset();
            }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            value = GUILayout.HorizontalSlider(value, min, max, sliderstyle, thumbstyle);
            if (float.TryParse(GUILayout.TextField(value.ToString(valuedecimals), textFieldStyle, GUILayout.Width(90)), out float newValue))
                value = newValue;
            GUILayout.EndHorizontal();
            return value;
        }
        internal static float SliderGUI(float value, float min, float max, float reset, GUIContent guiContent, string valuedecimals)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(guiContent, labelstyle);
            GUILayout.Space(space);
            if (GUILayout.Button(new GUIContent(GUIStrings.Reset, guiContent.tooltip), buttonstyleNoStretch))
            {
                value = reset;
            }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            value = GUILayout.HorizontalSlider(value, min, max, sliderstyle, thumbstyle);
            if (float.TryParse(GUILayout.TextField(value.ToString(valuedecimals), textFieldStyle, GUILayout.Width(90)), out float newValue))
                value = newValue;
            GUILayout.EndHorizontal();
            return value;
        }
        internal static float SliderGUI(float value, float min, float max, float reset, string labeltext, string tooltip, string valuedecimals)
        {
            return SliderGUI(value, min, max, reset, new GUIContent(labeltext, tooltip), valuedecimals);
        }
        internal static void ColorPickerGUI(Color value, Color reset, string labeltext, UI_ColorInfo.UpdateColor onSet)
        {
            ColorPickerGUI(value, reset, labeltext, "", onSet);
        }

        internal static void ColorPickerGUI(Color value, Color reset, string labeltext, string tooltip, UI_ColorInfo.UpdateColor onSet)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(new GUIContent(labeltext, tooltip), labelstyle, GUILayout.ExpandWidth(false));
            if (GUILayout.Button(GUIContent.none, GUILayout.ExpandHeight(true)))
            {
                if (Studio.Studio.Instance.colorMenu.updateColorFunc == onSet)
                    Studio.Studio.Instance.colorPaletteCtrl.visible = !Studio.Studio.Instance.colorPaletteCtrl.visible;
                else
                    Studio.Studio.Instance.colorPaletteCtrl.visible = true;
                if (Studio.Studio.Instance.colorPaletteCtrl.visible)
                {
                    Studio.Studio.Instance.colorMenu.updateColorFunc = onSet;
                    Studio.Studio.Instance.colorMenu.SetColor(value, UI_ColorInfo.ControlType.PresetsSample);
                }
            }
            Rect layoutRectangle = GUILayoutUtility.GetLastRect();
            layoutRectangle.xMin += 6;
            layoutRectangle.xMax -= 6;
            layoutRectangle.yMin += 6;
            layoutRectangle.yMax -= 6;
            simpleTexture.SetPixel(0, 0, value);
            simpleTexture.Apply(false);
            GUI.DrawTexture(layoutRectangle, simpleTexture, ScaleMode.StretchToFill, true);
            if (GUILayout.Button(new GUIContent(GUIStrings.Reset, tooltip), buttonstyleNoStretch))
            {
                if (onSet == Studio.Studio.Instance.colorMenu.updateColorFunc)
                    Studio.Studio.Instance.colorMenu.SetColor(reset, UI_ColorInfo.ControlType.PresetsSample);
                onSet(reset);
                
            }
            GUILayout.EndHorizontal();
        }
        internal static int SelectGUI(int selected, GUIContent title, string[] selections)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(title, labelstyle);
            GUILayout.FlexibleSpace();
            GUIContent[] selectionGUIContent = new GUIContent[selections.Length];
            uint num = 0;
            foreach (string s in selections)
            {
                selectionGUIContent[num] = new GUIContent(s, title.tooltip);
                num++;
            }
            selected = GUILayout.SelectionGrid(selected, selectionGUIContent, selections.Length, selectstyle, GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();
            return selected;
        }
        internal static int SelectGUI(int selected, GUIContent title, string[] selections, Action<int> Action)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(title, labelstyle);
            GUILayout.FlexibleSpace();
            GUIContent[] selectionGUIContent = new GUIContent[selections.Length];
            uint num = 0;
            foreach (string s in selections)
            {
                selectionGUIContent[num] = new GUIContent(s, title.tooltip);
                num++;
            }
            int temp = GUILayout.SelectionGrid(selected, selectionGUIContent, selections.Length, selectstyle, GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();
            if (temp == selected)
            {
                return selected;
            }
            else
            {
                Action(temp);
                return temp;
            }
        }
        internal static bool ToggleButton(bool toggle, GUIContent label, Action<bool> Action)
        {
            if (GUILayout.Button(label, (toggle ? toggleButtonOn : toggleButtonOff)))
            {
                toggle = !toggle;
                Action(toggle);
            }
            return toggle;
        }
        internal static bool ToggleButton(bool toggle, GUIContent label)
        {
            if (GUILayout.Button(label, (toggle ? toggleButtonOn : toggleButtonOff)))
            {
                toggle = !toggle;
            }
            return toggle;
        }
        internal static bool ToggleGUI(bool toggle, GUIContent title, string[] switches)
        {
            return ToggleGUI(toggle, title, switches, labelstyle);
        }

        internal static bool ToggleGUI(bool toggle, GUIContent title, string[] switches, GUIStyle titleStyle)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(title, titleStyle);
            GUILayout.FlexibleSpace();
            int temp = 0;
            if (toggle)
            {
                temp = 1;
            }
            temp = GUILayout.SelectionGrid(temp, new GUIContent[]
            {
                new GUIContent(switches[0], title.tooltip),
                new GUIContent(switches[1], title.tooltip)
            }, 2, selectstyle, GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();
            if (temp == 0)
            {
                return false;
            }
            return true;
        }

        internal static bool ToggleGUI(bool toggle, GUIContent title, string[] switches, Action<bool> Action)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(title, labelstyle);
            GUILayout.FlexibleSpace();
            int temp = Convert.ToInt32(toggle);
            temp = GUILayout.SelectionGrid(temp, new GUIContent[]
            {
                new GUIContent(switches[0], title.tooltip),
                new GUIContent(switches[1], title.tooltip)
            }, 2, selectstyle, GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();
            if ((temp != 0) == toggle)
            {
                return toggle;
            }
            else if (temp == 0)
            {
                Action(false);
                return false;
            }
            Action(true);
            return true;

        }
        internal static bool ToggleGUITitle(bool toggle, GUIContent title, string[] switches, Action<bool> Action)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(title, titlestyle2);
            GUILayout.FlexibleSpace();
            int temp = Convert.ToInt32(toggle);
            temp = GUILayout.SelectionGrid(temp, new GUIContent[]
            {
                new GUIContent(switches[0], title.tooltip),
                new GUIContent(switches[1], title.tooltip)
            }, 2, selectstyle, GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();
            if ((temp != 0) == toggle)
            {
                return toggle;
            }
            else if (temp == 0)
            {
                Action(false);
                return false;
            }
            Action(true);
            return true;
        }

        internal static bool ToggleGUITitle(bool toggle, GUIContent title, string[] switches)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(title, titlestyle2);
            GUILayout.FlexibleSpace();
            int temp = Convert.ToInt32(toggle);
            temp = GUILayout.SelectionGrid(temp, new GUIContent[]
            {
                new GUIContent(switches[0], title.tooltip),
                new GUIContent(switches[1], title.tooltip)
            }, 2, selectstyle, GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();
            if (temp == 0)
            {
                return false;
            }
            return true;
        }

        internal static void HorizontalLine()
        {
            GUILayout.Label("", GUILayout.Height(4));
            Color c = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, 0.5f);
            GUI.DrawTexture(GUILayoutUtility.GetLastRect(), Texture2D.whiteTexture, ScaleMode.StretchToFill);
            GUI.color = c;
        }

        internal static class Screen
        {
            internal static float width = 3840;
            internal static float height = 2160;
        }
        static internal Rect LimitWindowRect(Rect windowrect)
        {
            if (windowrect.x <= 0)
            {
                windowrect.x = 5f;
            }
            if (windowrect.y <= 0)
            {
                windowrect.y = 5f;
            }
            if (windowrect.xMax >= Screen.width)
            {
                windowrect.x -= 5f + windowrect.xMax - Screen.width;
            }
            if (windowrect.yMax >= Screen.height)
            {
                windowrect.y -= 5f + windowrect.yMax - Screen.height;
            }
            return windowrect;
        }

        internal static int hsvcolorpicker = 0;
        //internal static float customscale;
        internal static Vector3 scale;
        static Font myfont;
        static Color selected = new Color(0.1f, 0.75f, 1f);
        static Color selectedOnHover = new Color(0.1f, 0.6f, 0.8f);
        internal static float minwidth;
        internal static Rect TooltipRect = new Rect(Screen.width * 0.35f, Screen.height * 0.64f, Screen.width * 0.15f, Screen.height * 0.45f);
        internal static Rect windowRect = new Rect(Screen.width * 0.5f, Screen.height * 0.64f, Screen.width * 0.27f, Screen.height * 0.45f);
        internal static Rect warningRect = new Rect(Screen.width * 0.425f, Screen.height * 0.45f, Screen.width * 0.15f, Screen.height * 0.1f);
        internal static GUIStyle selectstyle;
        internal static GUIStyle buttonstyleNoStretch;
        internal static GUIStyle sliderstyle;
        internal static GUIStyle thumbstyle;
        internal static GUIStyle labelstyle2;
        internal static GUIStyle titlestyle;
        internal static GUIStyle titlestyle2;
        internal static GUIStyle labelstyle;
        internal static GUIStyle buttonstyleStrechWidth;
        internal static GUIStyle toggleButtonOn;
        internal static GUIStyle toggleButtonOff;
        internal static GUIStyle windowstyle;
        internal static Vector2[] scrollPosition = new Vector2[5];
        internal static bool styleInitialized = false;
        internal static float space;
        internal static GUIStyle labelstyle3;
        internal static GUIStyle boxstyle;
        internal static GUIStyle textFieldStyle;
        internal static GUIStyle textFieldStyle2;
        static internal Rect CMWarningRect = new Rect(Screen.width * 0.4f, Screen.height * 0.45f, Screen.width * 0.2f, Screen.height * 0.1f);
        static internal Rect ErrorwindowRect = new Rect(Screen.width * 0.4f, Screen.height * 0.45f, Screen.width * 0.2f, Screen.height * 0.1f);
        private static readonly Texture2D simpleTexture = new Texture2D(1, 1, TextureFormat.ARGB32, false);


    }
}