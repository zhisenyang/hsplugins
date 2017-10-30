using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

namespace HSPE
{
    public class UIUtility : MonoBehaviour
    {
        public const RenderMode canvasRenderMode = RenderMode.ScreenSpaceOverlay;
        public const bool canvasPixelPerfect = false;

        public const CanvasScaler.ScaleMode canvasScalerUiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        public const float canvasScalerReferencePixelsPerUnit = 100f;

        public const bool graphicRaycasterIgnoreReversedGraphics = true;
        public const GraphicRaycaster.BlockingObjects graphicRaycasterBlockingObjects = GraphicRaycaster.BlockingObjects.None;

        public static Sprite backgroundSprite;
        //public static Sprite headerSprite;
        public static Sprite checkMark;
        public static Sprite checkBox;
        public static Sprite slideBackground;
        public static Sprite handle;
        public static readonly Color whiteColor = new Color(1.000f, 1.000f, 1.000f);
        public static readonly Color grayColor = new Color32(100, 99, 95, 255);
        public static readonly Color lightGrayColor = new Color32(150, 149, 143, 255);
        public static readonly Color greenColor = new Color32(0, 160, 0, 255);
        public static readonly Color lightGreenColor = new Color32(0, 200, 0, 255);
        public static readonly Color purpleColor = new Color(0.000f, 0.007f, 1.000f, 0.545f);
        public static readonly Color transparentGrayColor = new Color32(100, 99, 95, 90);
        public static Font defaultFont;
        public static int defaultFontSize;
        public static float uiScale = 1f;

        void Start()
        {
            foreach (Sprite sprite in Resources.FindObjectsOfTypeAll<Sprite>())
            {
                switch (sprite.name)
                {
                    case "Background":
                        backgroundSprite = sprite;
                        break;
                    case "UISprite":
                        checkBox = sprite;
                        break;
                    case "toggle_c":
                        checkMark = sprite;
                        break;
                }
            }
            slideBackground = backgroundSprite;
            handle = backgroundSprite;
            foreach (Font font in Resources.FindObjectsOfTypeAll<Font>())
            {
                switch (font.name)
                {
                    case "Arial":
                        defaultFont = font;
                        break;
                }
            }
            defaultFontSize = 16;
            //headerSprite = GameObject.Find("CharaImoprtCanvas").transform.FindChild("BGPanel").FindChild("NamePanel").GetComponentInChildren<Image>().sprite;
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
            cs.referenceResolution = new Vector2(1920 / uiScale, 1080 / uiScale);

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

        public static Image AddImageToObject(Transform t, Sprite sprite = null)
        {
            return AddImageToObject(t.gameObject, sprite);
        }

        public static Image AddImageToObject(GameObject go, Sprite sprite = null)
        {
            Image i = go.AddComponent<Image>();
            i.type = Image.Type.Sliced;
            i.fillCenter = true;
            i.color = whiteColor;
            i.sprite = sprite == null ? backgroundSprite : sprite;
            return i;
        }

        public static Text AddTextToObject(Transform t, string text = "")
        {
            return AddTextToObject(t.gameObject, text);
        }

        public static Text AddTextToObject(GameObject go, string text = "")
        {
            Text t = go.AddComponent<Text>();
            t.text = text;
            t.color = whiteColor;
            t.font = defaultFont;
            t.fontSize = defaultFontSize;
            t.resizeTextMinSize = 1;
            t.alignByGeometry = true;
            t.resizeTextMaxSize = defaultFontSize;
            t.alignment = TextAnchor.UpperLeft;
            t.supportRichText = true;
            AddOutlineToObject(go);

            return t;
        }

        public static Button AddButtonToObject(Transform t, string s = "Button", bool margin = true)
        {
            return AddButtonToObject(t.gameObject, s, margin);
        }

        public static Button AddButtonToObject(GameObject go, string t = "Button", bool margin = true)
        {
            Button b = go.AddComponent<Button>();
            b.transition = Selectable.Transition.ColorTint;
            RectTransform bRT = b.transform as RectTransform;
            bRT.sizeDelta = new Vector2(160f, 30f);
            Image img = AddImageToObject(go);
            img.raycastTarget = false;
            img.color = Color.white;
            b.targetGraphic = img;
            b.colors = new ColorBlock()
            {
                colorMultiplier = 1f,
                normalColor = lightGrayColor,
                highlightedColor = greenColor,
                pressedColor = lightGreenColor,
                disabledColor = transparentGrayColor,
                fadeDuration = b.colors.fadeDuration
            };
            RectTransform text = CreateNewUIObject(b.transform, "Text");
            Text textObj = AddTextToObject(text.gameObject, t);
            textObj.alignment = TextAnchor.MiddleCenter;
            textObj.resizeTextMinSize = 1;
            if (margin)
                text.SetRect(Vector2.zero, Vector2.one, new Vector2(2.5f, 2.5f), new Vector2(-2.5f, -2.5f));
            else
                text.SetRect(Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            return b;
        }

        public static Outline AddOutlineToObject(Transform t)
        {
            return AddOutlineToObject(t.gameObject);
        }

        public static Outline AddOutlineToObject(GameObject go)
        {
            Outline o = go.AddComponent<Outline>();
            o.effectColor = Color.black;
            o.effectDistance = new Vector2(1f, -1f);
            return o;
        }

        public static Toggle AddToggleToObject(Transform t, string text = "Label")
        {
            return AddToggleToObject(t.gameObject, text);
        }

        public static Toggle AddToggleToObject(GameObject go, string text = "Label")
        {
            Toggle t = go.AddComponent<Toggle>();

            RectTransform bg = CreateNewUIObject(go.transform, "Background");
            t.targetGraphic = AddImageToObject(bg.gameObject, checkBox);

            RectTransform check = CreateNewUIObject(bg, "CheckMark");
            Image checkM = AddImageToObject(check.gameObject, checkMark);
            checkM.color = Color.black;
            t.graphic = checkM;

            RectTransform label = CreateNewUIObject(go.transform, "Label");
            Text te = AddTextToObject(label.gameObject, text);
            te.alignment = TextAnchor.MiddleLeft;
            te.fontSize = (int)(defaultFontSize * 0.75f);

            RectTransform rt = t.transform as RectTransform;
            rt.sizeDelta = new Vector2(160f, 20f);

            bg.anchorMin = new Vector2(0f, 1f);
            bg.anchorMax = new Vector2(0f, 1f);
            bg.anchoredPosition = new Vector2(10f, -10f);
            bg.sizeDelta = new Vector2(20f, 20f);

            check.sizeDelta = new Vector2(20f, 20f);

            label.anchorMin = Vector2.zero;
            label.anchorMax = Vector2.one;
            label.offsetMin = new Vector2(23f, 0f);
            label.offsetMax = new Vector2(5f, 0f);

            return t;
        }

        public static Toggle AddCheckboxToObject(Transform t)
        {
            return AddCheckboxToObject(t.gameObject);
        }

        public static Toggle AddCheckboxToObject(GameObject go)
        {
            Toggle t = go.AddComponent<Toggle>();

            RectTransform bg = CreateNewUIObject(go.transform, "Background");
            t.targetGraphic = AddImageToObject(bg.gameObject, checkBox);

            RectTransform check = CreateNewUIObject(bg, "CheckMark");
            Image checkM = AddImageToObject(check.gameObject, checkMark);
            checkM.color = Color.black;
            t.graphic = checkM;

            bg.SetRect(Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            check.SetRect(Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            return t;
        }

        public static Scrollbar AddScrollbarToObject(Transform t)
        {
            return AddScrollbarToObject(t.gameObject);
        }

        public static Scrollbar AddScrollbarToObject(GameObject go)
        {
            Scrollbar s = go.AddComponent<Scrollbar>();
            s.direction = Scrollbar.Direction.LeftToRight;
            s.size = 0f;
            s.numberOfSteps = 15;
            s.value = 0.5f;
            s.colors = new ColorBlock()
            {
                colorMultiplier = 1f,
                normalColor = lightGrayColor,
                highlightedColor = greenColor,
                pressedColor = lightGreenColor,
                disabledColor = transparentGrayColor,
                fadeDuration = s.colors.fadeDuration
            };

            Image background = AddImageToObject(go, slideBackground);
            RectTransform rt = s.transform as RectTransform;
            rt.sizeDelta = new Vector2(160f, 20f);

            RectTransform slideArea = CreateNewUIObject(rt, "Slide Area");
            slideArea.SetRect(Vector2.zero, Vector2.one, new Vector2(10f, 10f), new Vector2(-10f, -10f));

            s.handleRect = CreateNewUIObject(slideArea, "Handle");
            s.handleRect.SetRect(Vector2.zero, new Vector2(0.2f, 1f), new Vector2(-10f, -10f), new Vector2(10f, 10f));
            s.targetGraphic = AddImageToObject(s.handleRect, handle);
            s.targetGraphic.color = grayColor;
            return s;
        }
    }
}
