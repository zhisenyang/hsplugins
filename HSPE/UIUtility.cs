using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UI;

namespace HSPE
{
    public class UIUtility : MonoBehaviour
    {
        #region Public Static Variables
        public static RenderMode canvasRenderMode = RenderMode.ScreenSpaceOverlay;
        public static bool canvasPixelPerfect = false;

        public static CanvasScaler.ScaleMode canvasScalerUiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        public static Vector2 canvasScalerReferenceResolution = new Vector2(1280f, 720f);
        public static CanvasScaler.ScreenMatchMode canvasScreenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        public static float canvasScalerReferencePixelsPerUnit = 100f;

        public static bool graphicRaycasterIgnoreReversedGraphics = true;
        public static GraphicRaycaster.BlockingObjects graphicRaycasterBlockingObjects = GraphicRaycaster.BlockingObjects.None;

        public static Sprite backgroundSprite;
        public static Sprite headerSprite;
        public static Sprite checkMark;
        public static Sprite slideBackground;
        public static Sprite handle;
        public static Color whiteColor = new Color(1.000f, 1.000f, 1.000f, 0.878f);
        public static Color beigeColor = new Color(1.000f, 0.793f, 0.572f, 0.757f);
        public static Color greenColor = new Color(0.278f, 1.000f, 0.435f, 1.000f);
        public static Color yellowColor = new Color(0.993f, 1.000f, 0.463f, 1.000f);
        public static Color greyColor = new Color(0.784f, 0.784f, 0.784f, 0.502f);
        public static Color purpleColor = new Color(0.000f, 0.007f, 1.000f, 0.545f);
        public static Color blueColor = new Color(0.270f, 0.647f, 0.780f, 1.000f);
        public static Color redColor = new Color(0.650f, 0.066f, 0.011f, 1.000f);
        public static Font defaultFont;
        public static int defaultFontSize;
        #endregion
        void Start()
        {
            backgroundSprite = GameObject.Find("SystemCanvas").transform.FindChild("SystemUIAnime").FindChild("SystemBGImage").GetComponent<Image>().sprite;
            checkMark = GameObject.Find("SystemCanvas").transform.FindChild("SystemUIAnime").FindChild("SystemBGImage").FindChild("Toggle").GetChild(0).GetChild(0).GetComponent<Image>().sprite;
            defaultFont = GameObject.Find("SystemCanvas").transform.FindChild("SystemUIAnime").FindChild("SystemBGImage").FindChild("Toggle").GetComponentInChildren<Text>().font;
            defaultFontSize = GameObject.Find("CharaImoprtCanvas").transform.FindChild("BGPanel").FindChild("HideObj").FindChild("UIs").GetComponentInChildren<Text>().fontSize;
            headerSprite = GameObject.Find("CharaImoprtCanvas").transform.FindChild("BGPanel").FindChild("NamePanel").GetComponentInChildren<Image>().sprite;
            slideBackground = GameObject.Find("SystemCanvas").GetComponentInChildren<Scrollbar>().GetComponent<Image>().sprite;
            handle = GameObject.Find("SystemCanvas").GetComponentInChildren<Scrollbar>().transform.GetChild(0).GetChild(0).GetComponent<Image>().sprite;
        }

        public static Canvas CreateNewUISystem(string name = "NewUISystem")
        {
            GameObject go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas c = go.GetComponent<Canvas>();
            c.renderMode = canvasRenderMode;
            c.pixelPerfect = canvasPixelPerfect;

            CanvasScaler cs = go.GetComponent<CanvasScaler>();
            cs.uiScaleMode = canvasScalerUiScaleMode;
            cs.referenceResolution = canvasScalerReferenceResolution;
            cs.screenMatchMode = canvasScreenMatchMode;
            cs.referencePixelsPerUnit = canvasScalerReferencePixelsPerUnit;

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
            t.color = Color.black;
            t.font = defaultFont;
            t.fontSize = defaultFontSize;
            t.resizeTextMinSize = 1;
            t.alignByGeometry = true;
            t.resizeTextMaxSize = defaultFontSize;
            t.alignment = TextAnchor.UpperLeft;
            return t;
        }

        public static Button AddButtonToObject(Transform t, string s = "Button")
        {
            return AddButtonToObject(t.gameObject, s);
        }

        public static Button AddButtonToObject(GameObject go, string t = "Button")
        {
            Button b = go.AddComponent<Button>();
            RectTransform bRT = b.transform as RectTransform;
            bRT.sizeDelta = new Vector2(160f, 30f);
            Image img = AddImageToObject(go);
            img.raycastTarget = false;
            b.targetGraphic = img;
            b.colors = new ColorBlock()
            {
                colorMultiplier = 1f,
                normalColor = beigeColor,
                highlightedColor = greenColor,
                pressedColor = yellowColor,
                disabledColor = greyColor,
                fadeDuration = b.colors.fadeDuration
            };
            RectTransform text = CreateNewUIObject(b.transform, "Text");
            Text textObj = AddTextToObject(text.gameObject, t);
            textObj.alignment = TextAnchor.MiddleCenter;
            textObj.resizeTextMinSize = 1;
            text.SetRect(Vector2.zero, Vector2.one, new Vector2(2.5f, 2.5f), new Vector2(-2.5f, -2.5f));
            return b;
        }

        public static Outline AddOutlineToObject(Transform t)
        {
            return AddOutlineToObject(t.gameObject);
        }

        public static Outline AddOutlineToObject(GameObject go)
        {
            Outline o = go.AddComponent<Outline>();
            o.effectColor = purpleColor;
            o.effectDistance = new Vector2(0.75f, -0.75f);
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
            t.targetGraphic = AddImageToObject(bg.gameObject, headerSprite);

            RectTransform check = CreateNewUIObject(bg, "CheckMark");
            t.graphic = AddImageToObject(check.gameObject, checkMark);

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

        public static Scrollbar AddScrollbarToObject(Transform t)
        {
            return AddScrollbarToObject(t.gameObject);
        }

        public static Scrollbar AddScrollbarToObject(GameObject go)
        {
            Scrollbar s = go.AddComponent<Scrollbar>();
            s.direction = Scrollbar.Direction.LeftToRight;
            s.size = 0f;
            s.numberOfSteps = 25;
            s.value = 0.5f;
            s.colors = new ColorBlock()
            {
                colorMultiplier = 1f,
                normalColor = beigeColor,
                highlightedColor = greenColor,
                pressedColor = yellowColor,
                disabledColor = greyColor,
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
            s.targetGraphic.color = beigeColor;
            return s;
        }
    }
}
