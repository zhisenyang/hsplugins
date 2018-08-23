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
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

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

        private class CanvasData
        {
            public float scaleFactor;
            public float scaleFactor2;
            public Vector2 referenceResolution;
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
        private bool _eyesBlink = false;
        private bool _cameraSpeedShortcuts = true;
        private bool _alternativeCenterToObject = true;
        private bool _fingersFkCopyButtons = true;

        private bool _ssaoEnabled = true;
        private bool _bloomEnabled = true;
        private bool _ssrEnabled = true;
        private bool _dofEnabled = true;
        private bool _vignetteEnabled = true;
        private bool _fogEnabled = true;
        private bool _sunShaftsEnabled = false;

        private GameObject _go;
        private RoutinesComponent _routines;
        private Binary _binary;
        private Sprite _searchBarBackground;
        private float _lastCleanup;
        private Dictionary<Canvas, CanvasData> _scaledCanvases = new Dictionary<Canvas, CanvasData>();
        private int _lastScreenWidth;
        private int _lastScreenHeight;
        #endregion

        #region Public Accessors
        public string Name { get { return "HSUS"; } }
        public string Version { get { return "1.5.0"; } }
        public string[] Filter { get { return new[] {"HoneySelect_64", "HoneySelect_32", "StudioNEO_32", "StudioNEO_64"}; } }
        public static HSUS self { get; private set; }
        public bool optimizeCharaMaker { get { return this._optimizeCharaMaker; } }
        public Sprite searchBarBackground { get { return this._searchBarBackground; } }
        public bool improveNeoUI { get { return this._improveNeoUI; } }
        public bool optimizeNeo { get { return this._optimizeNeo; } }
        public bool enableGenericFK { get { return this._enableGenericFK; } }
        public KeyCode debugShortcut { get { return this._debugShortcut; } }
        public bool improvedTransformOperations { get { return this._improvedTransformOperations; } }
        public bool autoJointCorrection { get { return this._autoJointCorrection; } }
        public bool eyesBlink { get { return this._eyesBlink; } }
        public bool cameraSpeedShortcuts { get { return this._cameraSpeedShortcuts; } }
        public bool alternativeCenterToObject { get { return this._alternativeCenterToObject; } }
        public bool dofEnabled { get { return this._dofEnabled; } }
        public bool ssaoEnabled { get { return this._ssaoEnabled; } }
        public bool bloomEnabled { get { return this._bloomEnabled; } }
        public bool ssrEnabled { get { return this._ssrEnabled; } }
        public bool vignetteEnabled { get { return this._vignetteEnabled; } }
        public bool fogEnabled { get { return this._fogEnabled; } }
        public bool sunShaftsEnabled { get { return this._sunShaftsEnabled; } }
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
                    case "eyesBlink":
                        if (node.Attributes["enabled"] != null)
                            this._eyesBlink = XmlConvert.ToBoolean(node.Attributes["enabled"].Value);
                        break;
                    case "cameraSpeedShortcuts":
                        if (node.Attributes["enabled"] != null)
                            this._cameraSpeedShortcuts = XmlConvert.ToBoolean(node.Attributes["enabled"].Value);
                        break;
                    case "alternativeCenterToObject":
                        if (node.Attributes["enabled"] != null)
                            this._alternativeCenterToObject = XmlConvert.ToBoolean(node.Attributes["enabled"].Value);
                        break;
                    case "fingersFkCopyButtons":
                        if (node.Attributes["enabled"] != null)
                            this._fingersFkCopyButtons = XmlConvert.ToBoolean(node.Attributes["enabled"].Value);
                        break;
                    case "vsync":
                        if (node.Attributes["enabled"] != null && XmlConvert.ToBoolean(node.Attributes["enabled"].Value) == false)
                            QualitySettings.vSyncCount = 0;
                        break;
                    case "postProcessing":
                        foreach (XmlNode childNode in node.ChildNodes)
                        {
                            switch (childNode.Name)
                            {
                                case "depthOfField":
                                    if (childNode.Attributes["enabled"] != null)
                                        this._dofEnabled = XmlConvert.ToBoolean(childNode.Attributes["enabled"].Value);
                                    break;
                                case "ssao":
                                    if (childNode.Attributes["enabled"] != null)
                                        this._ssaoEnabled = XmlConvert.ToBoolean(childNode.Attributes["enabled"].Value);
                                    break;
                                case "bloom":
                                    if (childNode.Attributes["enabled"] != null)
                                        this._bloomEnabled = XmlConvert.ToBoolean(childNode.Attributes["enabled"].Value);
                                    break;
                                case "ssr":
                                    if (childNode.Attributes["enabled"] != null)
                                        this._ssrEnabled = XmlConvert.ToBoolean(childNode.Attributes["enabled"].Value);
                                    break;
                                case "vignette":
                                    if (childNode.Attributes["enabled"] != null)
                                        this._vignetteEnabled = XmlConvert.ToBoolean(childNode.Attributes["enabled"].Value);
                                    break;
                                case "fog":
                                    if (childNode.Attributes["enabled"] != null)
                                        this._fogEnabled = XmlConvert.ToBoolean(childNode.Attributes["enabled"].Value);
                                    break;
                                case "sunShafts":
                                    if (childNode.Attributes["enabled"] != null)
                                        this._sunShaftsEnabled = XmlConvert.ToBoolean(childNode.Attributes["enabled"].Value);
                                    break;
                            }
                        }
                        break;
                }
            }
            UIUtility.Init();
            HarmonyInstance harmony = HarmonyInstance.Create("com.joan6694.hsplugins.hsus");
            foreach (Type type in Assembly.GetExecutingAssembly().GetTypes())
            {
                try
                {
                    List<HarmonyMethod> harmonyMethods = type.GetHarmonyMethods();
                    if (harmonyMethods == null || harmonyMethods.Count <= 0)
                        continue;
                    HarmonyMethod attributes = HarmonyMethod.Merge(harmonyMethods);
                    new PatchProcessor(harmony, type, attributes).Patch();
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.Log("HSUS: Exception occured when patching: " + e.ToString());
                }
            }

            //Manual Patching for various reasons:
            {
                ItemFKCtrl_InitBone_Patches.ManualPatch(harmony);
                ItemFKCtrl_LateUpdate_Patches.ManualPatch(harmony);
                if (this._binary == Binary.Neo)
                    HSSNAShortcutKeyCtrlOverride_Update_Patches.ManualPatch(harmony);
            }
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

                    {
                        xmlWriter.WriteStartElement("eyesBlink");
                        xmlWriter.WriteAttributeString("enabled", XmlConvert.ToString(this._eyesBlink));
                        xmlWriter.WriteEndElement();
                    }

                    {
                        xmlWriter.WriteStartElement("cameraSpeedShortcuts");
                        xmlWriter.WriteAttributeString("enabled", XmlConvert.ToString(this._cameraSpeedShortcuts));
                        xmlWriter.WriteEndElement();
                    }

                    {
                        xmlWriter.WriteStartElement("alternativeCenterToObject");
                        xmlWriter.WriteAttributeString("enabled", XmlConvert.ToString(this._alternativeCenterToObject));
                        xmlWriter.WriteEndElement();
                    }

                    {
                        xmlWriter.WriteStartElement("fingersFkCopyButtons");
                        xmlWriter.WriteAttributeString("enabled", XmlConvert.ToString(this._fingersFkCopyButtons));
                        xmlWriter.WriteEndElement();
                    }

                    {
                        xmlWriter.WriteStartElement("vsync");
                        xmlWriter.WriteAttributeString("enabled", XmlConvert.ToString(QualitySettings.vSyncCount != 0));
                        xmlWriter.WriteEndElement();
                    }

                    {
                        xmlWriter.WriteStartElement("postProcessing");

                        {
                            xmlWriter.WriteStartElement("depthOfField");
                            xmlWriter.WriteAttributeString("enabled", XmlConvert.ToString(this._dofEnabled));
                            xmlWriter.WriteEndElement();
                        }

                        {
                            xmlWriter.WriteStartElement("ssao");
                            xmlWriter.WriteAttributeString("enabled", XmlConvert.ToString(this._ssaoEnabled));
                            xmlWriter.WriteEndElement();
                        }

                        {
                            xmlWriter.WriteStartElement("bloom");
                            xmlWriter.WriteAttributeString("enabled", XmlConvert.ToString(this._bloomEnabled));
                            xmlWriter.WriteEndElement();
                        }

                        {
                            xmlWriter.WriteStartElement("ssr");
                            xmlWriter.WriteAttributeString("enabled", XmlConvert.ToString(this._ssrEnabled));
                            xmlWriter.WriteEndElement();
                        }

                        {
                            xmlWriter.WriteStartElement("vignette");
                            xmlWriter.WriteAttributeString("enabled", XmlConvert.ToString(this._vignetteEnabled));
                            xmlWriter.WriteEndElement();
                        }

                        {
                            xmlWriter.WriteStartElement("fog");
                            xmlWriter.WriteAttributeString("enabled", XmlConvert.ToString(this._fogEnabled));
                            xmlWriter.WriteEndElement();
                        }

                        {
                            xmlWriter.WriteStartElement("sunShafts");
                            xmlWriter.WriteAttributeString("enabled", XmlConvert.ToString(this._sunShaftsEnabled));
                            xmlWriter.WriteEndElement();
                        }

                        xmlWriter.WriteEndElement();
                    }
                    xmlWriter.WriteEndElement();
                }
            }
        }

        public void OnLevelWasLoaded(int level)
        {
            UIUtility.SetCustomFont("mplus-1c-medium");
        }

        public void OnLevelWasInitialized(int level)
        {
            this._go = new GameObject("HSUS");
            this._go.AddComponent<ObjectTreeDebug>();
            this._routines = this._go.AddComponent<RoutinesComponent>();
            if (level == 3)
                this.SetProcessAffinity();
            switch (this._binary)
            {
                case Binary.Game:
                    if (this._optimizeCharaMaker)
                        this.InitFasterCharaMakerLoading();
                    if (level == 21 && string.IsNullOrEmpty(this._defaultFemaleChar) == false)
                        this.LoadCustomDefault(Path.Combine(Path.Combine(Path.Combine(UserData.Path, "chara"), "female"), this._defaultFemaleChar).Replace("\\", "/"));
                    break;
                case Binary.Neo:
                    if (level == 3)
                    {
                        if (this._deleteConfirmation)
                            this.InitDeleteConfirmationDialog();
                        if (this._improveNeoUI)
                            this.ImproveNeoUI();
                        if (this._improvedTransformOperations)
                            GameObject.Find("StudioScene").transform.Find("Canvas Guide Input").gameObject.AddComponent<TransformOperations>();
                        if (this._fingersFkCopyButtons)
                            this.InitFingersFKCopyButtons();
                    }
                    break;
            }
            this.InitUIScale();
            if (this._disableShortcutsWhenTyping)
                this._go.AddComponent<ShortcutsDisabler>();
        }

        public void OnUpdate()
        {
            if (Time.unscaledTime - this._lastCleanup > 30f)
            {
                Resources.UnloadUnusedAssets();
                this._lastCleanup = Time.unscaledTime;
                if (EventSystem.current.sendNavigationEvents)
                    EventSystem.current.sendNavigationEvents = false;
            }

            if (this._lastScreenWidth != Screen.width || this._lastScreenHeight != Screen.height)
                this.OnWindowResize();
            this._lastScreenWidth = Screen.width;
            this._lastScreenHeight = Screen.height;
        }

        public void OnLateUpdate()
        {
        }

        public void OnFixedUpdate()
        {
        }

        #endregion

        #region Private Methods
        private void OnWindowResize()
        {
            this._routines.ExecuteDelayed(this.ApplyUIScale, 2);
        }

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
                foreach (Canvas c in Resources.FindObjectsOfTypeAll<Canvas>())
                {
                    if (this._scaledCanvases.ContainsKey(c) == false && this.ShouldScaleUI(c))
                    {
                        CanvasScaler cs = c.GetComponent<CanvasScaler>();
                        if (cs != null)
                        {
                            switch (cs.uiScaleMode)
                            {
                                case CanvasScaler.ScaleMode.ConstantPixelSize:
                                    this._scaledCanvases.Add(c, new CanvasData() { scaleFactor = c.scaleFactor, scaleFactor2 = cs.scaleFactor});
                                    break;
                                case CanvasScaler.ScaleMode.ScaleWithScreenSize:
                                    this._scaledCanvases.Add(c, new CanvasData() { scaleFactor = c.scaleFactor, referenceResolution = cs.referenceResolution});
                                    break;
                            }
                        }
                        else
                        {
                            this._scaledCanvases.Add(c, new CanvasData() { scaleFactor = c.scaleFactor });
                        }
                    }
                }
                Dictionary<Canvas, CanvasData> newScaledCanvases = new Dictionary<Canvas, CanvasData>();
                foreach (KeyValuePair<Canvas, CanvasData> pair in this._scaledCanvases)
                {
                    if (pair.Key != null)
                        newScaledCanvases.Add(pair.Key, pair.Value);
                }
                this._scaledCanvases = newScaledCanvases;
                this.ApplyUIScale();
            }, 10);
        }

        private void ApplyUIScale()
        {
            float usedScale = this._binary == Binary.Game ? this._gameUIScale : this._neoUIScale;
            if (usedScale != 1f) //Fuck you shortcutshsparty
            {
                Type t = Type.GetType("ShortcutsHSParty.DefaultMenuController,ShortcutsHSParty");
                if (t != null)
                {
                    MonoBehaviour component = ((MonoBehaviour)Object.FindObjectOfType(t));
                    if (component != null)
                        component.enabled = false;
                }
            }
            foreach (KeyValuePair<Canvas, CanvasData> pair in this._scaledCanvases)
            {
                if (pair.Key != null && this.ShouldScaleUI(pair.Key))
                {
                    //pair.Key.scaleFactor = pair.Value.scaleFactor * usedScale;
                    CanvasScaler cs = pair.Key.GetComponent<CanvasScaler>();
                    if (cs != null)
                    {
                        switch (cs.uiScaleMode)
                        {
                            case CanvasScaler.ScaleMode.ConstantPixelSize:
                                cs.scaleFactor = pair.Value.scaleFactor2 * usedScale;
                                break;
                            case CanvasScaler.ScaleMode.ScaleWithScreenSize:
                                cs.referenceResolution = pair.Value.referenceResolution / usedScale;
                                break;
                        }
                    }
                    else
                    {
                        pair.Key.scaleFactor = pair.Value.scaleFactor * usedScale;
                    }
                }
            }
        }

        private bool ShouldScaleUI(Canvas c)
        {
            bool ok = true;
            string path = c.transform.GetPathFrom(null);
            if (this._binary == Binary.Neo)
            {
                switch (path)
                {
                    case "StartScene/Canvas":
                    case "VectorCanvas":
                        ok = false;
                        break;
                }
            }
            else
            {
                switch (path)
                {
                    case "LogoScene/Canvas":
                    case "LogoScene/Canvas (1)":
                    case "CustomScene/CustomControl/CustomUI/BackGround":
                    case "CustomScene/CustomControl/CustomUI/Fusion":
                    case "TitleScene/Canvas":
                    case "GameScene/Canvas":
                    case "MapSelectScene/Canvas":
                    case "SubtitleUserInterface":
                    case "ADVScene/Canvas":
                        ok = false;
                        break;
                }
            }
            Canvas parent = c.GetComponentInParent<Canvas>();
            return ok && c.isRootCanvas && (parent == null || parent == c) && c.name != "HSPE";
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
                panel.rectTransform.SetRect(Vector2.zero, Vector2.one, new Vector2(640f / 2, 360f / 2), new Vector2(-640f / 2, -360f / 2));
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

        private void ImproveNeoUI()
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

        private void InitFingersFKCopyButtons()
        {
            RectTransform toggle = GameObject.Find("StudioScene/Canvas Main Menu/02_Manipulate/00_Chara/02_Kinematic/00_FK/Toggle Right Hand").transform as RectTransform;
            Button b = UIUtility.CreateButton("Copy Right Fingers Button", toggle.parent, "From Anim");
            b.transform.SetRect(toggle.anchorMin, toggle.anchorMax, new Vector2(toggle.offsetMax.x + 4f, toggle.offsetMin.y), new Vector2(toggle.offsetMax.x + 64f, toggle.offsetMax.y));
            b.onClick.AddListener(() =>
            {
                TreeNodeObject treeNodeObject = Studio.Studio.Instance.treeNodeCtrl.selectNode;
                if (treeNodeObject == null)
                    return;
                ObjectCtrlInfo info;
                if (!Studio.Studio.Instance.dicInfo.TryGetValue(treeNodeObject, out info))
                    return;
                OCIChar selected = info as OCIChar;
                if (selected == null)
                    return;
                this.CopyToFKBoneOfGroup(selected.listBones, OIBoneInfo.BoneGroup.RightHand);
            });
            Text text = b.GetComponentInChildren<Text>();
            text.rectTransform.SetRect();
            text.color = Color.white;
            Image image = b.GetComponent<Image>();
            image.sprite = null;
            image.color = new Color32(89, 88, 85, 255);
            toggle = GameObject.Find("StudioScene/Canvas Main Menu/02_Manipulate/00_Chara/02_Kinematic/00_FK/Toggle Left Hand").transform as RectTransform;
            b = UIUtility.CreateButton("Copy Left Fingers Button", toggle.parent, "From Anim");
            b.transform.SetRect(toggle.anchorMin, toggle.anchorMax, new Vector2(toggle.offsetMax.x + 4f, toggle.offsetMin.y), new Vector2(toggle.offsetMax.x + 64f, toggle.offsetMax.y));
            b.onClick.AddListener(() =>
            {
                TreeNodeObject treeNodeObject = Studio.Studio.Instance.treeNodeCtrl.selectNode;
                if (treeNodeObject == null)
                    return;
                ObjectCtrlInfo info;
                if (!Studio.Studio.Instance.dicInfo.TryGetValue(treeNodeObject, out info))
                    return;
                OCIChar selected = info as OCIChar;
                if (selected == null)
                    return;
                this.CopyToFKBoneOfGroup(selected.listBones, OIBoneInfo.BoneGroup.LeftHand);
            });
            text = b.GetComponentInChildren<Text>();
            text.rectTransform.SetRect();
            text.color = Color.white;
            image = b.GetComponent<Image>();
            image.sprite = null;
            image.color = new Color32(89, 88, 85, 255);
        }

        private void CopyToFKBoneOfGroup(List<OCIChar.BoneInfo> listBones, OIBoneInfo.BoneGroup group)
        {
            List<GuideCommand.EqualsInfo> infos = new List<GuideCommand.EqualsInfo>();
            foreach (OCIChar.BoneInfo bone in listBones)
            {
                if (bone.guideObject != null && bone.guideObject.transformTarget != null && bone.boneGroup == group)
                {
                    Vector3 oldValue = bone.guideObject.changeAmount.rot;
                    bone.guideObject.changeAmount.rot = bone.guideObject.transformTarget.localEulerAngles;
                    infos.Add(new GuideCommand.EqualsInfo()
                    {
                        dicKey = bone.guideObject.dicKey,
                        oldValue = oldValue,
                        newValue = bone.guideObject.changeAmount.rot
                    });
                }
            }
            UndoRedoManager.Instance.Push(new GuideCommand.RotationEqualsCommand(infos.ToArray()));
        }

        private void LoadCustomDefault(string path)
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

        private void SetProcessAffinity()
        {
            this._routines.ExecuteDelayed(() =>
            {
                const long affinityMask = 1;
                Process proc = Process.GetCurrentProcess();
                proc.ProcessorAffinity = (IntPtr)affinityMask;

                foreach (ProcessThread thread in proc.Threads)
                    thread.ProcessorAffinity = (IntPtr)affinityMask;
            });
            this._routines.ExecuteDelayed(() =>
            {
                const long affinityMask = -1;
                Process proc = Process.GetCurrentProcess();
                proc.ProcessorAffinity = (IntPtr)affinityMask;

                foreach (ProcessThread thread in proc.Threads)
                    thread.ProcessorAffinity = (IntPtr)affinityMask;
            }, 30);
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

    //[HarmonyPatch(typeof(GuideObjectManager), "Add", new[] { typeof(Transform), typeof(int) })]
    //public class Testetetetetet
    //{
    //    public static void Prefix(Transform _target, int _dicKey, Dictionary<Transform, GuideObject> ___dicGuideObject)
    //    {
    //        UnityEngine.Debug.LogError("Adding target " + _target.GetPathFrom(null) + "\n" + _dicKey + "Contained ? " + ___dicGuideObject.ContainsKey(_target));
    //    }
    //}
}