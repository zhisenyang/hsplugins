using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ToolBox.Extensions {
    internal static class IMGUIExtensions
    {
        public static void SetGlobalFontSize(int size)
        {
            foreach (GUIStyle style in GUI.skin)
                style.fontSize = size;
            GUI.skin = GUI.skin;
        }

        public static void ResetFontSize()
        {
            SetGlobalFontSize(0);
        }

        private static readonly GUIStyle _customBoxStyle = new GUIStyle { normal = new GUIStyleState { background = Texture2D.whiteTexture } };
#if HONEYSELECT || PLAYHOME || KOIKATSU
        private static readonly Color _backgroundColor = new Color(1f, 1f, 1f, 0.5f);
#elif AISHOUJO || HONEYSELECT2
        private static readonly Color _backgroundColor = new Color(0f, 0f, 0f, 0.5f);
#endif

        public static void DrawBackground(Rect rect)
        {
            Color c = GUI.backgroundColor;
            GUI.backgroundColor = _backgroundColor;
            GUI.Box(rect, "", _customBoxStyle);
            GUI.backgroundColor = c;

        }


        private static Canvas _canvas = null;
        public static RectTransform CreateUGUIPanelForIMGUI()
        {
            if (_canvas == null)
            {
                GameObject g = GameObject.Find("IMGUIBackgrounds");
                if (g != null)
                    _canvas = g.GetComponent<Canvas>();
                if (_canvas == null)
                {
                    GameObject go = new GameObject("IMGUIBackgrounds", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                    go.hideFlags |= HideFlags.HideInHierarchy;
                    _canvas = go.GetComponent<Canvas>();
                    _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    _canvas.pixelPerfect = true;
                    _canvas.sortingOrder = 999;

                    CanvasScaler cs = go.GetComponent<CanvasScaler>();
                    cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                    cs.referencePixelsPerUnit = 100;
                    cs.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

                    GraphicRaycaster gr = go.GetComponent<GraphicRaycaster>();
                    gr.ignoreReversedGraphics = true;
                    gr.blockingObjects = GraphicRaycaster.BlockingObjects.None;
                }
            }
            GameObject background = new GameObject("Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            background.transform.SetParent(_canvas.transform, false);
            background.transform.localPosition = Vector3.zero;
            background.transform.localRotation = Quaternion.identity;
            background.transform.localScale = Vector3.one;
            RawImage image = background.GetComponent<RawImage>();
            image.color = new Color32(127, 127, 127, 2);
            image.raycastTarget = true;
            RectTransform rt = (RectTransform)background.transform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0f, 1f);
            return rt;
        }

        public static void FitRectTransformToRect(RectTransform transform, Rect rect)
        {
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)_canvas.transform, new Vector2(rect.xMin, rect.yMax), _canvas.worldCamera, out Vector2 min) && RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)_canvas.transform, new Vector2(rect.xMax, rect.yMin), _canvas.worldCamera, out Vector2 max))
            {
                transform.offsetMin = new Vector2(min.x, -min.y);
                transform.offsetMax = new Vector2(max.x, -max.y);
            }
        }

        private static readonly Stack<Color> _colorStack = new Stack<Color>(new[] {Color.white});
        public static void PushColor(Color c)
        {
            _colorStack.Push(c);
            GUI.color = c;
        }

        public static void PopColor()
        {
            _colorStack.Pop();
            GUI.color = _colorStack.Peek();
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