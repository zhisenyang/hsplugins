﻿using System.Diagnostics;
using IllusionPlugin;
using UnityEngine;
using UnityEngine.UI;

namespace UILib
{
    public class Plugin : IEnhancedPlugin
    {
        #region Public Types
        public enum Binary
        {
            Neo,
            Game,
        }
        #endregion

        public string Name { get { return "UIUtility"; } }
        public string Version { get { return "1.0.0"; } }
        public string[] Filter { get { return new[] { "HoneySelect_64", "HoneySelect_32", "StudioNEO_32", "StudioNEO_64" }; } }
        public static Plugin instance { get; private set; }
        public Binary binary { get; set; }
        public void OnApplicationStart()
        {
            instance = this;
            switch (Process.GetCurrentProcess().ProcessName)
            {
                case "HoneySelect_32":
                case "HoneySelect_64":
                    this.binary = Binary.Game;
                    break;
                case "StudioNEO_32":
                case "StudioNEO_64":
                    this.binary = Binary.Neo;
                    break;
            }
        }

        public void OnApplicationQuit()
        {
        }

        public void OnLevelWasLoaded(int level)
        {
            new GameObject("UIUtility", typeof(UIUtility));
        }

        public void OnLevelWasInitialized(int level)
        {
        }

        public void OnUpdate()
        {
        }

        public void OnFixedUpdate()
        {
        }


        public void OnLateUpdate()
        {
        }
    }

    public class UIUtility : MonoBehaviour
    {
        public const RenderMode canvasRenderMode = RenderMode.ScreenSpaceOverlay;
        public const bool canvasPixelPerfect = false;

        public const CanvasScaler.ScaleMode canvasScalerUiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        public const float canvasScalerReferencePixelsPerUnit = 100f;

        public const bool graphicRaycasterIgnoreReversedGraphics = true;
        public const GraphicRaycaster.BlockingObjects graphicRaycasterBlockingObjects = GraphicRaycaster.BlockingObjects.None;

        public static Sprite checkMark;
        public static Sprite backgroundSprite;
        public static Sprite standardSprite;
        public static Sprite inputFieldBackground;
        public static Sprite knob;
        public static Sprite dropdownArrow;
        public static Sprite mask;
        public static readonly Color whiteColor = new Color(1.000f, 1.000f, 1.000f);
        public static readonly Color grayColor = new Color32(100, 99, 95, 255);
        public static readonly Color lightGrayColor = new Color32(150, 149, 143, 255);
        public static readonly Color greenColor = new Color32(0, 160, 0, 255);
        public static readonly Color lightGreenColor = new Color32(0, 200, 0, 255);
        public static readonly Color purpleColor = new Color(0.000f, 0.007f, 1.000f, 0.545f);
        public static readonly Color transparentGrayColor = new Color32(100, 99, 95, 90);
        public static Font defaultFont;
        public static int defaultFontSize;
        public static DefaultControls.Resources resources;

        void Awake()
        {
            if (Plugin.instance.binary == Plugin.Binary.Game)
            {
                foreach (Sprite sprite in Resources.FindObjectsOfTypeAll<Sprite>())
                {
                    switch (sprite.name)
                    {
                        case "Background":
                            backgroundSprite = sprite;
                            break;
                        case "UISprite":
                            standardSprite = sprite;
                            break;
                        case "rect_middle":
                            inputFieldBackground = sprite;
                            break;
                        case "sld_thumb":
                            knob = sprite;
                            break;
                        case "toggle_c":
                            checkMark = sprite;
                            break;
                        case "expand":
                            dropdownArrow = sprite;
                            break;
                        case "UIMask":
                            mask = sprite;
                            break;
                    }
                }
                foreach (Font font in Resources.FindObjectsOfTypeAll<Font>())
                {
                    switch (font.name)
                    {
                        case "mplus-1c-medium":
                            defaultFont = font;
                            break;
                    }
                }
            }
            else
            {
                foreach (Sprite sprite in Resources.FindObjectsOfTypeAll<Sprite>())
                {
                    UnityEngine.Debug.Log("sprite "+ sprite.name);
                    switch (sprite.name)
                    {
                        case "Background":
                            backgroundSprite = sprite;
                            break;
                        case "UISprite":
                            standardSprite = sprite;
                            break;
                        case "InputFieldBackground":
                            inputFieldBackground = sprite;
                            break;
                        case "sp_sn_16_00_02":
                            knob = sprite;
                            break;
                        case "toggle_c":
                            checkMark = sprite;
                            break;
                        case "sp_sn_09_00_03":
                            dropdownArrow = sprite;
                            break;
                        case "UIMask":
                            mask = sprite;
                            break;
                    }
                }
                foreach (Font font in Resources.FindObjectsOfTypeAll<Font>())
                {
                    switch (font.name)
                    {
                        case "Arial":
                            defaultFont = font;
                            break;
                    }
                }
            }
            resources = new DefaultControls.Resources {background = backgroundSprite, checkmark = checkMark, dropdown = dropdownArrow, inputField = inputFieldBackground, knob = knob, mask = mask, standard = standardSprite};
            defaultFontSize = 16;
        }

        public static Canvas CreateNewUISystem(string name = "NewUISystem")
        {
            GameObject go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas c = go.GetComponent<Canvas>();
            c.renderMode = canvasRenderMode;
            //c.pixelPerfect = canvasPixelPerfect;

            CanvasScaler cs = go.GetComponent<CanvasScaler>();
            cs.uiScaleMode = canvasScalerUiScaleMode;
            cs.referencePixelsPerUnit = canvasScalerReferencePixelsPerUnit;
            cs.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

            GraphicRaycaster gr = go.GetComponent<GraphicRaycaster>();
            gr.ignoreReversedGraphics = graphicRaycasterIgnoreReversedGraphics;
            gr.blockingObjects = graphicRaycasterBlockingObjects;

            return c;
        }

        public static RectTransform CreateNewUIObject()
        {
            return CreateNewUIObject(null, "UIObject");
        }

        public static RectTransform CreateNewUIObject(string name)
        {
            return CreateNewUIObject(null, name);
        }

        public static RectTransform CreateNewUIObject(Transform parent)
        {
            return CreateNewUIObject(parent, "UIObject");
        }

        public static RectTransform CreateNewUIObject(Transform parent, string name)
        {
            RectTransform t = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            if (parent != null)
            {
                t.SetParent(parent, false);
                t.localPosition = Vector3.zero;
                t.localScale = Vector3.one;
            }
            return t;
        }

        public static InputField CreateInputField(string objectName = "New Input Field", Transform parent = null, string placeholder = "Placeholder...")
        {
            GameObject go = DefaultControls.CreateInputField(resources);
            go.name = objectName;
            foreach (Text text in go.GetComponentsInChildren<Text>())
            {
                text.font = defaultFont;
                text.resizeTextForBestFit = true;
                text.resizeTextMinSize = 2;
                text.resizeTextMaxSize = 100;
                text.alignment = TextAnchor.MiddleLeft;
                text.rectTransform.offsetMin = new Vector2(5f, 2f);
                text.rectTransform.offsetMax = new Vector2(-5f, -2f);
            }
            go.transform.SetParent(parent, false);

            InputField f = go.GetComponent<InputField>();
            f.placeholder.GetComponent<Text>().text = placeholder;

            return f;
        }

        public static Button CreateButton(string objectName = "New Button", Transform parent = null, string buttonText = "Button")
        {
            GameObject go = DefaultControls.CreateButton(resources);
            go.name = objectName;

            Text text = go.GetComponentInChildren<Text>();
            text.font = defaultFont;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 2;
            text.resizeTextMaxSize = 100;
            text.alignment = TextAnchor.MiddleCenter;
            text.rectTransform.SetRect(Vector2.zero, Vector2.one, new Vector2(2f, 2f), new Vector2(-2f, -2f));
            text.text = buttonText;
            text.color = whiteColor;
            go.transform.SetParent(parent, false);

            Button b = go.GetComponent<Button>();
            b.colors = new ColorBlock()
            {
                colorMultiplier = 1f,
                normalColor = lightGrayColor,
                highlightedColor = greenColor,
                pressedColor = lightGreenColor,
                disabledColor = transparentGrayColor,
                fadeDuration = b.colors.fadeDuration
            };
            return b;
        }

        public static Image CreateImage(string objectName = "New Image", Transform parent = null, Sprite sprite = null)
        {
            GameObject go = DefaultControls.CreateImage(resources);
            go.name = objectName;
            go.transform.SetParent(parent, false);
            Image i = go.GetComponent<Image>();
            i.sprite = sprite;
            return i;
        }

        public static Text CreateText(string objectName = "New Text", Transform parent = null, string textText = "Text")
        {
            GameObject go = DefaultControls.CreateText(resources);
            go.name = objectName;

            Text text = go.GetComponentInChildren<Text>();
            text.font = defaultFont;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 2;
            text.resizeTextMaxSize = 100;
            text.alignment = TextAnchor.UpperLeft;
            text.text = textText;
            text.color = whiteColor;
            go.transform.SetParent(parent, false);

            return text;
        }

        public static Toggle CreateToggle(string objectName = "New Toggle", Transform parent = null, string label = "Label")
        {
            GameObject go = DefaultControls.CreateToggle(resources);
            go.name = objectName;

            Text text = go.GetComponentInChildren<Text>();
            text.font = defaultFont;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 2;
            text.resizeTextMaxSize = 100;
            text.alignment = TextAnchor.MiddleCenter;
            text.rectTransform.SetRect(Vector2.zero, Vector2.one, new Vector2(23f, 1f), new Vector2(-5f, -2f));
            text.text = label;
            go.transform.SetParent(parent, false);

            return go.GetComponent<Toggle>();
        }

        public static Dropdown CreateDropdown(string objectName = "New Dropdown", Transform parent = null, string label = "Label")
        {
            GameObject go = DefaultControls.CreateDropdown(resources);
            go.name = objectName;

            foreach (Text text in go.GetComponentsInChildren<Text>())
            {
                text.font = defaultFont;
                text.resizeTextForBestFit = true;
                text.resizeTextMinSize = 2;
                text.resizeTextMaxSize = 100;
                text.alignment = TextAnchor.MiddleLeft;
                if (text.name.Equals("Label"))
                    text.rectTransform.SetRect(Vector2.zero, Vector2.one, new Vector2(10f, 6f), new Vector2(-25f, -7f));
                else
                    text.rectTransform.SetRect(Vector2.zero, Vector2.one, new Vector2(20f, 1f), new Vector2(-10f, -2f));
            }
            go.transform.SetParent(parent, false);
            return go.GetComponent<Dropdown>();
        }

        public static RawImage CreateRawImage(string objectName = "New Raw Image", Transform parent = null, Texture texture = null)
        {
            GameObject go = DefaultControls.CreateRawImage(resources);
            go.name = objectName;
            go.transform.SetParent(parent, false);
            RawImage i = go.GetComponent<RawImage>();
            i.texture = texture;
            return i;
        }

        public static Scrollbar CreateScrollbar(string objectName = "New Scrollbar", Transform parent = null)
        {
            GameObject go = DefaultControls.CreateScrollbar(resources);
            go.name = objectName;
            go.transform.SetParent(parent, false);
            return go.GetComponent<Scrollbar>();
        }

        public static ScrollRect CreateScrollView(string objectName = "New ScrollView", Transform parent = null)
        {
            GameObject go = DefaultControls.CreateScrollView(resources);
            go.name = objectName;
            go.transform.SetParent(parent, false);
            return go.GetComponent<ScrollRect>();
        }

        public static Slider CreateSlider(string objectName = "New Slider", Transform parent = null)
        {
            GameObject go = DefaultControls.CreateSlider(resources);
            go.name = objectName;
            go.transform.SetParent(parent, false);
            return go.GetComponent<Slider>();
        }
        public static Image CreatePanel(string objectName = "New Panel", Transform parent = null)
        {
            GameObject go = DefaultControls.CreatePanel(resources);
            go.name = objectName;
            go.transform.SetParent(parent, false);
            return go.GetComponent<Image>();
        }

        public static Outline AddOutlineToObject(Transform t)
        {
            return AddOutlineToObject(t, Color.black, new Vector2(1f, -1f));
        }

        public static Outline AddOutlineToObject(Transform t, Color c)
        {
            return AddOutlineToObject(t, c, new Vector2(1f, -1f));
        }

        public static Outline AddOutlineToObject(Transform t, Vector2 effectDistance)
        {
            return AddOutlineToObject(t, Color.black, effectDistance);
        }

        public static Outline AddOutlineToObject(Transform t, Color color, Vector2 effectDistance)
        {
            Outline o = t.gameObject.AddComponent<Outline>();
            o.effectColor = color;
            o.effectDistance = effectDistance;
            return o;
        }

        public static Toggle AddCheckboxToObject(Transform tr)
        {
            Toggle t = tr.gameObject.AddComponent<Toggle>();

            RectTransform bg = CreateNewUIObject(tr.transform, "Background");
            t.targetGraphic = AddImageToObject(bg, standardSprite);

            RectTransform check = CreateNewUIObject(bg, "CheckMark");
            Image checkM = AddImageToObject(check, checkMark);
            checkM.color = Color.black;
            t.graphic = checkM;

            bg.SetRect(Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            check.SetRect(Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            return t;
        }

        public static Image AddImageToObject(Transform t, Sprite sprite = null)
        {
            Image i = t.gameObject.AddComponent<Image>();
            i.type = Image.Type.Sliced;
            i.fillCenter = true;
            i.color = whiteColor;
            i.sprite = sprite == null ? backgroundSprite : sprite;
            return i;
        }
    }

    public static class UIExtensions
    {
        public static void SetRect(this RectTransform self)
        {
            self.anchorMin = Vector2.zero;
            self.anchorMax = Vector2.one;
            self.offsetMin = Vector2.zero;
            self.offsetMax = Vector2.zero;
        }
        public static void SetRect(this RectTransform self, Vector2 anchorMin)
        {
            self.anchorMin = anchorMin;
            self.anchorMax = Vector2.one;
            self.offsetMin = Vector2.zero;
            self.offsetMax = Vector2.zero;
        }
        public static void SetRect(this RectTransform self, Vector2 anchorMin, Vector2 anchorMax)
        {
            self.anchorMin = anchorMin;
            self.anchorMax = anchorMax;
            self.offsetMin = Vector2.zero;
            self.offsetMax = Vector2.zero;
        }
        public static void SetRect(this RectTransform self, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin)
        {
            self.anchorMin = anchorMin;
            self.anchorMax = anchorMax;
            self.offsetMin = offsetMin;
            self.offsetMax = Vector2.zero;
        }
        public static void SetRect(this RectTransform self, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            self.anchorMin = anchorMin;
            self.anchorMax = anchorMax;
            self.offsetMin = offsetMin;
            self.offsetMax = offsetMax;
        }
    }
}