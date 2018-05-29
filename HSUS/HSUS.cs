using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;
using CustomMenu;
using Harmony;
using IllusionPlugin;
using Studio;
using UILib;
using UnityEngine;
using UnityEngine.UI;

namespace HSUS
{
    public class HSUS : IEnhancedPlugin
    {
        #region Private Types
        private enum Binary
        {
            Neo,
            Game,
        }
        #endregion

        #region Private Variables
        private const string _config = "config.xml";
        private const string _pluginDir = "Plugins\\HSUS\\";

        private bool _optimizeCharaMaker = true;
        private float _gameUIScale = 1f;
        private float _neoUIScale = 1f;
        private bool _deleteConfirmation = true;
        private bool _disableShortcutsWhenTyping = true;
        private string _defaultFemaleChar = "";
        private bool _improveNeoUI = true;
        private bool _optimizeNeo = true;
        private bool _enableGenericFK = true;
        private KeyCode _debugShortcut = KeyCode.RightControl;
        private bool _improvedTransformOperations = true;
        private bool _autoJointCorrection = true;

        private GameObject _go;
        private RoutinesComponent _routines;
        private HashSet<Canvas> _scaledCanvases = new HashSet<Canvas>();
        private Binary _binary;
        private Sprite _searchBarBackground;
        private float _lastCleanup;
        #endregion

        #region Public Accessors
        public string Name { get { return "HSUS"; } }
        public string Version { get { return "1.3.2"; } }
        public string[] Filter { get { return new[] { "HoneySelect_64", "HoneySelect_32", "StudioNEO_32", "StudioNEO_64" }; } }
        public static HSUS self { get; private set; }
        public bool optimizeCharaMaker { get { return this._optimizeCharaMaker; } }
        public Sprite searchBarBackground { get { return this._searchBarBackground; } }
        public bool improveNeoUI { get { return this._improveNeoUI; } }
        public bool optimizeNeo { get { return this._optimizeNeo; } }
        public bool enableGenericFK { get { return this._enableGenericFK; } }
        public KeyCode debugShortcut { get { return this._debugShortcut; } }
        public bool improvedTransformOperations { get { return this._improvedTransformOperations; } }
        public bool autoJointCorrection { get { return this._autoJointCorrection; } }
        public RoutinesComponent routines { get { return this._routines; } }
        #endregion

        #region Unity Methods
        public void OnApplicationStart()
        {
            self = this;

            switch (Process.GetCurrentProcess().ProcessName)
            {
                case "HoneySelect_32":
                case "HoneySelect_64":
                    this._binary = Binary.Game;
                    break;
                case "StudioNEO_32":
                case "StudioNEO_64":
                    this._binary = Binary.Neo;
                    break;
            }
            string path = _pluginDir + _config;
            if (File.Exists(path) == false)
                return;
            XmlDocument doc = new XmlDocument();
            doc.Load(path);

            foreach (XmlNode node in doc.DocumentElement.ChildNodes)
            {
                switch (node.Name)
                {
                    case "optimizeCharaMaker":
                        if (node.Attributes["enabled"] != null)
                            this._optimizeCharaMaker = XmlConvert.ToBoolean(node.Attributes["enabled"].Value);
                        break;
                    case "uiScale":
                        foreach (XmlNode n in node.ChildNodes)
                        {
                            switch (n.Name)
                            {
                                case "game":
                                    if (n.Attributes["scale"] != null)
                                        this._gameUIScale = XmlConvert.ToSingle(n.Attributes["scale"].Value);
                                    break;
                                case "neo":
                                    if (n.Attributes["scale"] != null)
                                        this._neoUIScale = XmlConvert.ToSingle(n.Attributes["scale"].Value);
                                    break;
                            }
                        }
                        break;
                    case "deleteConfirmation":
                        if (node.Attributes["enabled"] != null)
                            this._deleteConfirmation = XmlConvert.ToBoolean(node.Attributes["enabled"].Value);
                        break;
                    case "disableShortcutsWhenTyping":
                        if (node.Attributes["enabled"] != null)
                            this._disableShortcutsWhenTyping = XmlConvert.ToBoolean(node.Attributes["enabled"].Value);
                        break;
                    case "defaultFemaleChar":
                        if (node.Attributes["path"] != null)
                            this._defaultFemaleChar = node.Attributes["path"].Value;
                        break;
                    case "improveNeoUI":
                        if (node.Attributes["enabled"] != null)
                            this._improveNeoUI = XmlConvert.ToBoolean(node.Attributes["enabled"].Value);
                        break;
                    case "enableGenericFK":
                        if (node.Attributes["enabled"] != null)
                            this._enableGenericFK = XmlConvert.ToBoolean(node.Attributes["enabled"].Value);
                        break;
                    case "optimizeNeo":
                        if (node.Attributes["enabled"] != null)
                            this._optimizeNeo = XmlConvert.ToBoolean(node.Attributes["enabled"].Value);
                        break;
                    case "debugShortcut":
                        if (node.Attributes["value"] != null)
                        {
                            string value = node.Attributes["value"].Value;
                            if (Enum.IsDefined(typeof(KeyCode), value))
                                this._debugShortcut = (KeyCode)Enum.Parse(typeof(KeyCode), value);
                        }
                        break;
                    case "improvedTransformOperations":
                        if (node.Attributes["enabled"] != null)
                            this._improvedTransformOperations = XmlConvert.ToBoolean(node.Attributes["enabled"].Value);
                        break;
                    case "autoJointCorrection":
                        if (node.Attributes["enabled"] != null)
                            this._autoJointCorrection = XmlConvert.ToBoolean(node.Attributes["enabled"].Value);
                        break;
                }
            }
            UIUtility.Init();
            HarmonyInstance harmony = HarmonyInstance.Create("com.joan6694.hsplugins.hsus");
            if (this._binary == Binary.Game)
            {
                foreach (Type type in Assembly.GetExecutingAssembly().GetTypes())
                {
                    switch (type.FullName)
                    {
                        case "Studio.ItemFKCtrl_InitBone_Patches":
                            continue;
                    }
                    try
                    {
                        List<HarmonyMethod> harmonyMethods = type.GetHarmonyMethods();
                        if (harmonyMethods != null && harmonyMethods.Count > 0)
                        {
                            HarmonyMethod attributes = HarmonyMethod.Merge(harmonyMethods);
                            new PatchProcessor(harmony, type, attributes).Patch();
                        }
                    }
                    catch (Exception e)
                    {
                        UnityEngine.Debug.Log("HSUS: Exception occured when patching: " + e.ToString());
                    }
                }
            }
            else
            {
                harmony.PatchAll(Assembly.GetExecutingAssembly());
            }

            QualitySettings.pixelLightCount = 16; //TODO A ENLEVER!!!
        }

        public void OnApplicationQuit()
        {
            if (Directory.Exists(_pluginDir) == false)
                Directory.CreateDirectory(_pluginDir);
            string path = _pluginDir + _config;
            using (FileStream fileStream = new FileStream(path, FileMode.Create, FileAccess.Write))
            {
                using (XmlTextWriter xmlWriter = new XmlTextWriter(fileStream, Encoding.UTF8))
                {
                    xmlWriter.Formatting = Formatting.Indented;

                    xmlWriter.WriteStartElement("root");
                    xmlWriter.WriteAttributeString("version", this.Version);

                    {
                        xmlWriter.WriteStartElement("optimizeCharaMaker");
                        xmlWriter.WriteAttributeString("enabled", XmlConvert.ToString(this._optimizeCharaMaker));
                        xmlWriter.WriteEndElement();
                    }

                    {
                        xmlWriter.WriteStartElement("uiScale");

                        {
                            xmlWriter.WriteStartElement("game");
                            xmlWriter.WriteAttributeString("scale", XmlConvert.ToString(this._gameUIScale));
                            xmlWriter.WriteEndElement();
                        }

                        {
                            xmlWriter.WriteStartElement("neo");
                            xmlWriter.WriteAttributeString("scale", XmlConvert.ToString(this._neoUIScale));
                            xmlWriter.WriteEndElement();
                        }

                        xmlWriter.WriteEndElement();
                    }

                    {
                        xmlWriter.WriteStartElement("deleteConfirmation");
                        xmlWriter.WriteAttributeString("enabled", XmlConvert.ToString(this._deleteConfirmation));
                        xmlWriter.WriteEndElement();
                    }

                    {
                        xmlWriter.WriteStartElement("disableShortcutsWhenTyping");
                        xmlWriter.WriteAttributeString("enabled", XmlConvert.ToString(this._disableShortcutsWhenTyping));
                        xmlWriter.WriteEndElement();
                    }

                    {
                        xmlWriter.WriteStartElement("defaultFemaleChar");
                        xmlWriter.WriteAttributeString("path", this._defaultFemaleChar);
                        xmlWriter.WriteEndElement();
                    }

                    {
                        xmlWriter.WriteStartElement("improveNeoUI");
                        xmlWriter.WriteAttributeString("enabled", XmlConvert.ToString(this._improveNeoUI));
                        xmlWriter.WriteEndElement();
                    }

                    {
                        xmlWriter.WriteStartElement("optimizeNeo");
                        xmlWriter.WriteAttributeString("enabled", XmlConvert.ToString(this._optimizeNeo));
                        xmlWriter.WriteEndElement();
                    }

                    {
                        xmlWriter.WriteStartElement("enableGenericFK");
                        xmlWriter.WriteAttributeString("enabled", XmlConvert.ToString(this._enableGenericFK));
                        xmlWriter.WriteEndElement();
                    }

                    {
                        xmlWriter.WriteStartElement("debugShortcut");
                        xmlWriter.WriteAttributeString("value", this._debugShortcut.ToString());
                        xmlWriter.WriteEndElement();
                    }

                    {
                        xmlWriter.WriteStartElement("improvedTransformOperations");
                        xmlWriter.WriteAttributeString("enabled", XmlConvert.ToString(this._improvedTransformOperations));
                        xmlWriter.WriteEndElement();
                    }

                    {
                        xmlWriter.WriteStartElement("autoJointCorrection");
                        xmlWriter.WriteAttributeString("enabled", XmlConvert.ToString(this._autoJointCorrection));
                        xmlWriter.WriteEndElement();
                    }

                    xmlWriter.WriteEndElement();
                }
            }
        }

        public void OnLevelWasLoaded(int level)
        {
            UIUtility.Init();
        }

        public void OnLevelWasInitialized(int level)
        {
            this._go = new GameObject("HSUS");
            this._go.AddComponent<ObjectTreeDebug>();
            this._routines = this._go.AddComponent<RoutinesComponent>();

            if (this._optimizeCharaMaker)
                this.InitFasterCharaMakerLoading();
            this.InitUIScale();
            if (this._deleteConfirmation && this._binary == Binary.Neo && level == 3)
                this.InitDeleteConfirmationDialog();
            if (this._disableShortcutsWhenTyping)
                this._go.AddComponent<ShortcutsDisabler>();
            if (this._binary == Binary.Game && level == 21 && string.IsNullOrEmpty(this._defaultFemaleChar) == false)
                this.LoadCustomDefault(Path.Combine(Path.Combine(Path.Combine(UserData.Path, "chara"), "female"), this._defaultFemaleChar).Replace("\\", "/"));
            if (this._improveNeoUI && this._binary == Binary.Neo && level == 3)
            {
                RectTransform rt = GameObject.Find("StudioScene").transform.FindChild("Canvas Main Menu/01_Add/02_Item/Scroll View Item") as RectTransform;
                rt.offsetMax += new Vector2(60f, 0f);
                rt = GameObject.Find("StudioScene").transform.FindChild("Canvas Main Menu/01_Add/02_Item/Scroll View Item/Viewport") as RectTransform;
                rt.offsetMax += new Vector2(60f, 0f);
                rt = GameObject.Find("StudioScene").transform.FindChild("Canvas Main Menu/01_Add/02_Item/Scroll View Item/Viewport/Content") as RectTransform;
                rt.offsetMax += new Vector2(60f, 0f);

                VerticalLayoutGroup group = GameObject.Find("StudioScene").transform.FindChild("Canvas Main Menu/01_Add/02_Item/Scroll View Item/Viewport/Content").GetComponent<VerticalLayoutGroup>();
                group.childForceExpandWidth = true;
                group.padding = new RectOffset(group.padding.left + 4, group.padding.right + 24, group.padding.top, group.padding.bottom);
                GameObject.Find("StudioScene").transform.FindChild("Canvas Main Menu/01_Add/02_Item/Scroll View Item/Viewport/Content").GetComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

                Text t = GameObject.Find("StudioScene").transform.FindChild("Canvas Main Menu/01_Add/02_Item/Scroll View Item/node/Text").GetComponent<Text>();
                t.resizeTextForBestFit = true;
                t.resizeTextMinSize = 2;
                t.resizeTextMaxSize = 100;
            }
        }

        public void OnUpdate()
        {
            if (Time.unscaledTime - this._lastCleanup > 30f)
            {
                Resources.UnloadUnusedAssets();
                this._lastCleanup = Time.unscaledTime;
            }
        }
        public void OnLateUpdate()
        {
        }

        public void OnFixedUpdate()
        {
        }
        #endregion

        #region Private Methods
        private void InitFasterCharaMakerLoading()
        {
            this._routines.ExecuteDelayed(() =>
            {
                foreach (Sprite sprite in Resources.FindObjectsOfTypeAll<Sprite>())
                {
                    bool shouldBreak = false;
                    switch (sprite.name)
                    {
                        case "rect_middle":
                            this._searchBarBackground = sprite;
                            shouldBreak = true;
                            break;
                    }
                    if (shouldBreak)
                        break;
                }
                UIUtility.SetCustomFont("mplus-1c-medium");
                foreach (SmClothes_F f in Resources.FindObjectsOfTypeAll<SmClothes_F>())
                {
                    SmClothes_F_Data.Init(f);
                    break;
                }
                foreach (SmCharaLoad f in Resources.FindObjectsOfTypeAll<SmCharaLoad>())
                {
                    SmCharaLoad_Data.Init(f);
                    break;
                }
                foreach (SmAccessory f in Resources.FindObjectsOfTypeAll<SmAccessory>())
                {
                    SmAccessory_Data.Init(f);
                    break;
                }
                foreach (SmHair_F f in Resources.FindObjectsOfTypeAll<SmHair_F>())
                {
                    SmHair_F_Data.Init(f);
                    break;  
                }
                foreach (SmKindColorD f in Resources.FindObjectsOfTypeAll<SmKindColorD>())
                {
                    SmKindColorD_Data.Init(f);
                    break;
                }
                foreach (SmKindColorDS f in Resources.FindObjectsOfTypeAll<SmKindColorDS>())
                {
                    SmKindColorDS_Data.Init(f);
                    break;
                }
                foreach (SmFaceSkin f in Resources.FindObjectsOfTypeAll<SmFaceSkin>())
                {
                    SmFaceSkin_Data.Init(f);
                    break;
                }
                foreach (SmSwimsuit f in Resources.FindObjectsOfTypeAll<SmSwimsuit>())
                {
                    SmSwimsuit_Data.Init(f);
                    break;
                }
                foreach (SmClothesLoad f in Resources.FindObjectsOfTypeAll<SmClothesLoad>())
                {
                    SmClothesLoad_Data.Init(f);
                    break;
                }
            }, 10);
        }

        private void InitUIScale()
        {
            this._routines.ExecuteDelayed(() =>
            {
                float usedScale = this._binary == Binary.Game ? this._gameUIScale : this._neoUIScale;
                foreach (Canvas c in Resources.FindObjectsOfTypeAll<Canvas>())
                {
                    if (this._scaledCanvases.Contains(c) == false && this.ShouldScaleUI(c))
                    {
                        CanvasScaler cs = c.GetComponent<CanvasScaler>();
                        if (cs != null)
                        {
                            switch (cs.uiScaleMode)
                            {
                                case CanvasScaler.ScaleMode.ConstantPixelSize:
                                    cs.scaleFactor *= usedScale;
                                    break;
                                case CanvasScaler.ScaleMode.ScaleWithScreenSize:
                                    cs.referenceResolution = cs.referenceResolution / usedScale;
                                    break;
                            }
                        }
                        else
                            c.scaleFactor *= usedScale;
                        this._scaledCanvases.Add(c);
                    }
                }
                HashSet<Canvas> newScaledCanvases = new HashSet<Canvas>();
                foreach (Canvas c in this._scaledCanvases)
                {
                    if (c != null)
                        newScaledCanvases.Add(c);
                }
                this._scaledCanvases = newScaledCanvases;
            }, 10);
        }

        private bool ShouldScaleUI(Canvas c)
        {
            bool ok = true;
            string path = c.transform.GetPathFrom(null);
            if (this._binary == Binary.Neo)
            {
                if (ok)
                    ok = path != "StartScene/Canvas";
            }
            else
            {
                if (ok)
                    ok = path != "LogoScene/Canvas";
                if (ok)
                    ok = path != "LogoScene/Canvas (1)";
                if (ok)
                    ok = path != "CustomScene/CustomControl/CustomUI/BackGround";
                if (ok)
                    ok = path != "CustomScene/CustomControl/CustomUI/Fusion";
                if (ok)
                    ok = path != "TitleScene/Canvas";
                if (ok)
                    ok = path != "GameScene/Canvas";
                if (ok)
                    ok = path != "MapSelectScene/Canvas";
                if (ok)
                    ok = path != "SubtitleUserInterface";
                if (ok)
                    ok = path != "ADVScene/Canvas";
            }
            Canvas parent = c.GetComponentInParent<Canvas>();
            return c.isRootCanvas && (parent == null || parent == c) && c.name != "HSPE" && ok;
        }

        private void InitDeleteConfirmationDialog()
        {
                this._routines.ExecuteDelayed(() =>
                {
                    Canvas c = UIUtility.CreateNewUISystem("HSUSDeleteConfirmation");
                    c.sortingOrder = 40;
                    c.transform.SetParent(GameObject.Find("StudioScene").transform);
                    c.transform.localPosition = Vector3.zero;
                    c.transform.localScale = Vector3.one;
                    c.transform.SetRect();
                    c.transform.SetAsLastSibling();
                    
                    Image bg = UIUtility.CreateImage("Background", c.transform);
                    bg.rectTransform.SetRect();
                    bg.sprite = null;
                    bg.color = new Color(0f, 0f, 0f, 0.5f);
                    bg.raycastTarget = true;

                    Image panel = UIUtility.CreatePanel("Panel", bg.transform);
                    panel.rectTransform.SetRect(Vector2.zero, Vector2.one, new Vector2(640f/2, 360f/2), new Vector2(-640f/2, -360f/2));
                    panel.color = Color.gray;

                    Text text = UIUtility.CreateText("Text", panel.transform, "Are you sure you want to delete this object?");
                    text.rectTransform.SetRect(new Vector2(0f, 0.5f), Vector2.one, new Vector2(10f, 10f), new Vector2(-10f, -10f));
                    text.color = Color.white;
                    text.resizeTextForBestFit = true;
                    text.resizeTextMaxSize = 100;
                    text.alignByGeometry = true;
                    text.alignment = TextAnchor.MiddleCenter;

                    Button yes = UIUtility.CreateButton("YesButton", panel.transform, "Yes");
                    (yes.transform as RectTransform).SetRect(Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(10f, 10f), new Vector2(-10f, -10f));
                    text = yes.GetComponentInChildren<Text>();
                    text.resizeTextForBestFit = true;
                    text.resizeTextMaxSize = 100;
                    text.alignByGeometry = true;
                    text.alignment = TextAnchor.MiddleCenter;

                    Button no = UIUtility.CreateButton("NoButton", panel.transform, "No");
                    (no.transform as RectTransform).SetRect(new Vector2(0.5f, 0f), new Vector2(1f, 0.5f), new Vector2(10f, 10f), new Vector2(-10f, -10f));
                    text = no.GetComponentInChildren<Text>();
                    text.resizeTextForBestFit = true;
                    text.resizeTextMaxSize = 100;
                    text.alignByGeometry = true;
                    text.alignment = TextAnchor.MiddleCenter;

                    c.gameObject.AddComponent<DeleteConfirmation>();
                    c.gameObject.SetActive(false);

                }, 20);
        }

        public void LoadCustomDefault(string path)
        {
            CustomControl customControl = Resources.FindObjectsOfTypeAll<CustomControl>()[0];
            int personality = customControl.chainfo.customInfo.personality;
            string name = customControl.chainfo.customInfo.name;
            bool isConcierge = customControl.chainfo.customInfo.isConcierge;
            bool flag = false;
            bool flag2 = false;
            if (customControl.modeCustom == 0)
            {
                customControl.chainfo.chaFile.Load(path);
                customControl.chainfo.chaFile.ChangeCoordinateType(customControl.chainfo.statusInfo.coordinateType);
                if (customControl.chainfo.chaFile.customInfo.isConcierge)
                {
                    flag = true;
                    flag2 = true;
                }
            }
            else
            {
                customControl.chainfo.chaFile.LoadBlockData(customControl.chainfo.customInfo, path);
                customControl.chainfo.chaFile.LoadBlockData(customControl.chainfo.chaFile.coordinateInfo, path);
                customControl.chainfo.chaFile.ChangeCoordinateType(customControl.chainfo.statusInfo.coordinateType);
                flag = true;
                flag2 = true;
            }
            customControl.chainfo.customInfo.isConcierge = isConcierge;
            if (customControl.chainfo.Sex == 0)
            {
                CharMale charMale = customControl.chainfo as CharMale;
                charMale.Reload();
                charMale.maleStatusInfo.visibleSon = false;
            }
            else
            {
                CharFemale charFemale = customControl.chainfo as CharFemale;
                charFemale.Reload();
                charFemale.UpdateBustSoftnessAndGravity();
            }
            if (flag)
            {
                customControl.chainfo.customInfo.personality = personality;
            }
            if (flag2)
            {
                customControl.chainfo.customInfo.name = name;
            }
            //this.UpdateLimitMainMenu();
            customControl.SetSameSetting();
            customControl.noChangeSubMenu = true;
            customControl.ChangeSwimTypeFromLoad();
            customControl.noChangeSubMenu = false;
            customControl.UpdateCharaName();
            customControl.UpdateAcsName();
        }

        #endregion
    }

    [HarmonyPatch(typeof(SetRenderQueue), "Awake")]
    public class SetRenderQueue_Awake_Patches
    {
        public static bool Prefix(SetRenderQueue __instance)
        {
            Renderer renderer = __instance.GetComponent<Renderer>();
            int[] queues = (int[])__instance.GetPrivate("m_queues");
            if (renderer != null)
            {
                Material[] materials = renderer.materials;
                int num = 0;
                while (num < materials.Length && num < queues.Length)
                {
                    materials[num].renderQueue = queues[num];
                    num++;
                }
            }
            else
            {
                __instance.ExecuteDelayed(() =>
                {
                    renderer = __instance.GetComponent<Renderer>();
                    if (renderer == null)
                        return;
                    Material[] materials = renderer.materials;
                    int num = 0;
                    while (num < materials.Length && num < queues.Length)
                    {
                        materials[num].renderQueue = queues[num];
                        num++;
                    }
                }, 3);
            }
            return false;
        }
    }

    [HarmonyPatch(typeof(StartScene), "Start")]
    public class StartScene_Start_Patches
    {
        public static bool Prepare()
        {
            return HSUS.self.optimizeNeo;
        }
        public static bool Prefix(System.Object __instance)
        {
            if (__instance as StartScene)
            {
                Studio.Info.Instance.LoadExcelData();
                Manager.Scene.Instance.SetFadeColor(Color.black);
                Manager.Scene.Instance.LoadReserv("Studio", true);
                return false;
            }
            return true;
        }
    }
    //[HarmonyPatch(typeof(HSColorSet), "SetSpecularRGB", new[] { typeof(Color) })]
    //public class Testetetetetet
    //{
    //    public static void Prefix(Color rgb)
    //    {
    //        UnityEngine.Debug.Log("rgb " + rgb);
    //    }
    //}

    //[HarmonyPatch(typeof(ColorChange), "SetHSColor", new[] { typeof(Material), typeof(HSColorSet), typeof(bool), typeof(bool), typeof(HSColorSet), typeof(bool), typeof(bool) })]
    //public class gfdsgfds
    //{
    //    public static void Prefix(Material mat, global::HSColorSet color, bool chgDif = true, bool chgSpe = true, global::HSColorSet color2 = null, bool chgDif2 = true, bool chgSpe2 = true)
    //    {
    //        UnityEngine.Debug.Log("spec color id " + Manager.Character.Instance._SpecColor + " " + mat.HasProperty(Manager.Character.Instance._SpecColor) + " " + Shader.PropertyToID("_SpecColor") + " " + mat.HasProperty("_SpecColor") + " " + mat.shader + " " + mat.GetColor("_SpecColor"));
            
    //        UnityEngine.Debug.Log(mat + " " + (color != null ? color.rgbDiffuse + " " + color.rgbSpecular + " " + color.rgbaDiffuse : "") + " " + chgDif + " " + chgSpe + " " + (color2 != null ? color2.rgbDiffuse + " " + color2.rgbSpecular + " " + color2.rgbaDiffuse : "") + " " + chgDif2 + " " + chgSpe2);
    //    }
    //}
}