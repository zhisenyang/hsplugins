using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;
#if HONEYSELECT
using CustomMenu;
using IllusionPlugin;
using Studio;
#elif KOIKATSU
using BepInEx;
using ChaCustom;
#endif
using StudioFileCheck;
using Harmony;
using ToolBox;
using UILib;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace HSUS
{
#if KOIKATSU
    [BepInPlugin(GUID: "com.joan6694.illusionplugins.kkus", Name: "KKUS", Version: HSUS._version)]
#endif
    public class HSUS :
#if HONEYSELECT
        IEnhancedPlugin
#elif KOIKATSU
        BaseUnityPlugin
#endif
    {
#if HONEYSELECT
        internal const string _version = "1.6.1";
#elif KOIKATSU
        internal const string _version = "1.0.0";
#endif

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
        internal delegate bool TranslationDelegate(ref string text);

        #endregion

        #region Private Variables
        private const string _config = "config.xml";
#if HONEYSELECT
        private const string _pluginDir = "Plugins\\HSUS\\";
#elif KOIKATSU
        private const string _pluginDir = "BepInEx\\KKUS\\";
#endif

        internal bool _optimizeCharaMaker = true;
        internal bool _asyncLoading = false;
        internal float _gameUIScale = 1f;
        internal float _neoUIScale = 1f;
        internal bool _deleteConfirmation = true;
        internal bool _disableShortcutsWhenTyping = true;
        internal string _defaultFemaleChar = "";
        internal string _defaultMaleChar = "";
        internal bool _improveNeoUI = true;
        internal bool _optimizeNeo = true;
        internal bool _enableGenericFK = true;
#if HONEYSELECT
        internal bool _debugEnabled = true;
#elif KOIKATSU
        internal bool _debugEnabled = false;
#endif
        internal KeyCode _debugShortcut = KeyCode.RightControl;
        internal bool _improvedTransformOperations = true;
        internal bool _autoJointCorrection = true;
        internal bool _eyesBlink = false;
        internal bool _cameraSpeedShortcuts = true;
        internal bool _alternativeCenterToObject = true;
        internal bool _fingersFkCopyButtons = true;
        internal bool _fourKManagerDithering = true;
#if HONEYSELECT
        internal bool _automaticMemoryClean = true;
#elif KOIKATSU
        internal bool _automaticMemoryClean = false;
#endif
        internal int _automaticMemoryCleanInterval = 30;

#if HONEYSELECT
        internal bool _ssaoEnabled = true;
        internal bool _bloomEnabled = true;
        internal bool _ssrEnabled = true;
        internal bool _dofEnabled = true;
        internal bool _vignetteEnabled = true;
        internal bool _fogEnabled = true;
        internal bool _sunShaftsEnabled = false;
#elif KOIKATSU
        internal bool _ssaoEnabled = true;
        internal bool _bloomEnabled = true;
        internal bool _selfShadowEnabled = true;
        internal bool _dofEnabled = false;
        internal bool _vignetteEnabled = true;
        internal bool _fogEnabled = false;
        internal bool _sunShaftsEnabled = false;
#endif

        internal static HSUS _self;
        internal GameObject _go;
        internal RoutinesComponent _routines;
        private Binary _binary;
        internal Sprite _searchBarBackground;
        private float _lastCleanup;
        private Dictionary<Canvas, CanvasData> _scaledCanvases = new Dictionary<Canvas, CanvasData>();
        private int _lastScreenWidth;
        private int _lastScreenHeight;
        internal TranslationDelegate _translationMethod;
        internal readonly List<IEnumerator> _asyncMethods = new List<IEnumerator>();
        #endregion

        #region Public Accessors
#if HONEYSELECT
        public string Name { get { return "HSUS"; } }
        public string Version { get { return _version; } }
        public string[] Filter { get { return new[] {"HoneySelect_64", "HoneySelect_32", "StudioNEO_32", "StudioNEO_64"}; } }
        public static HSUS self { get { return _self; } } //Keeping this for legacy
        public bool optimizeCharaMaker { get { return this._optimizeCharaMaker; } } //Keeping this for legacy
        public Sprite searchBarBackground { get { return this._searchBarBackground; } } //Keeping this for legacy
#endif
        public KeyCode debugShortcut { get { return this._debugShortcut; } }
        #endregion

        #region Unity Methods
#if HONEYSELECT
        public void OnApplicationStart()
#elif KOIKATSU
        void Awake()
#endif
        {
            _self = this;
#if KOIKATSU
            SceneManager.sceneLoaded += this.SceneLoaded;
#endif
            switch (Process.GetCurrentProcess().ProcessName)
            {
#if HONEYSELECT
                case "HoneySelect_32":
                case "HoneySelect_64":                    
#elif KOIKATSU
                case "Koikatu":
#endif
                    this._binary = Binary.Game;
                    break;
#if HONEYSELECT
                case "StudioNEO_32":
                case "StudioNEO_64":
#elif KOIKATSU
                case "CharaStudio":
#endif
                    this._binary = Binary.Neo;
                    break;
            }

            Type t = Type.GetType("UnityEngine.UI.Translation.TextTranslator,UnityEngine.UI.Translation");
            if (t != null)
            {
                MethodInfo info = t.GetMethod("Translate", BindingFlags.Public | BindingFlags.Static);
                if (info != null)
                    this._translationMethod = (TranslationDelegate)Delegate.CreateDelegate(typeof(TranslationDelegate), info);
            }

            string path = _pluginDir + _config;
            if (File.Exists(path))
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(path);

                foreach (XmlNode node in doc.DocumentElement.ChildNodes)
                {
                    switch (node.Name)
                    {
                        case "optimizeCharaMaker":
                            if (node.Attributes["enabled"] != null)
                                this._optimizeCharaMaker = XmlConvert.ToBoolean(node.Attributes["enabled"].Value);
                            foreach (XmlNode childNode in node.ChildNodes)
                            {
                                switch (childNode.Name)
                                {
                                    case "asyncLoading":
                                        if (childNode.Attributes["enabled"] != null)
                                            this._asyncLoading = XmlConvert.ToBoolean(childNode.Attributes["enabled"].Value);
                                        break;
                                }
                            }
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
                        case "defaultMaleChar":
                            if (node.Attributes["path"] != null)
                                this._defaultMaleChar = node.Attributes["path"].Value;
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
                        case "debug":
                            if (node.Attributes["enabled"] != null)
                                this._debugEnabled = XmlConvert.ToBoolean(node.Attributes["enabled"].Value);
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
                        case "fourKManagerDithering":
                            if (node.Attributes["enabled"] != null)
                                this._fourKManagerDithering = XmlConvert.ToBoolean(node.Attributes["enabled"].Value);
                            break;
                        case "automaticMemoryClean":
                            if (node.Attributes["enabled"] != null)
                                this._automaticMemoryClean = XmlConvert.ToBoolean(node.Attributes["enabled"].Value);
                            if (node.Attributes["interval"] != null)
                                this._automaticMemoryCleanInterval = XmlConvert.ToInt32(node.Attributes["interval"].Value);
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
#if HONEYSELECT
                                case "ssr":
                                    if (childNode.Attributes["enabled"] != null)
                                        this._ssrEnabled = XmlConvert.ToBoolean(childNode.Attributes["enabled"].Value);
                                    break;
#elif KOIKATSU
                                    case "selfShadow":
                                        if (childNode.Attributes["enabled"] != null)
                                            this._selfShadowEnabled = XmlConvert.ToBoolean(childNode.Attributes["enabled"].Value);
                                        break;
#endif
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
#if HONEYSELECT
                if (this._binary == Binary.Neo)
                {
                    HSSNAShortcutKeyCtrlOverride_Update_Patches.ManualPatch(harmony);
                    ABMStudioNEOSaveLoadHandler_OnLoad_Patches.ManualPatch(harmony);
                }
                ItemFKCtrl_InitBone_Patches.ManualPatch(harmony);
                TonemappingColorGrading_Ctor_Patches.ManualPatch(harmony);
                UI_ColorInfo_ConvertValueFromText_Patches.ManualPatch(harmony);
#elif KOIKATSU
                CostumeInfo_Init_Patches.ManualPatch(harmony);
#endif
            }
        }

#if KOIKATSU
        private void SceneLoaded(Scene scene, LoadSceneMode loadMode)
        {
            if (loadMode == LoadSceneMode.Single)
            {
                this.OnLevelWasInitialized(scene.buildIndex);
                this.OnLevelWasLoaded(scene.buildIndex);
            }
            else
            {
                this.InitUIScale();
            }
        }
#endif
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
                    xmlWriter.WriteAttributeString("version", _version);

                    {
                        xmlWriter.WriteStartElement("optimizeCharaMaker");
                        xmlWriter.WriteAttributeString("enabled", XmlConvert.ToString(this._optimizeCharaMaker));
#if HONEYSELECT
                        {
                            xmlWriter.WriteStartElement("asyncLoading");
                            xmlWriter.WriteAttributeString("enabled", XmlConvert.ToString(this._asyncLoading));
                            xmlWriter.WriteEndElement();
                        }
#endif
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

#if HONEYSELECT
                    {
                        xmlWriter.WriteStartElement("disableShortcutsWhenTyping");
                        xmlWriter.WriteAttributeString("enabled", XmlConvert.ToString(this._disableShortcutsWhenTyping));
                        xmlWriter.WriteEndElement();
                    }
#endif

                    {
                        xmlWriter.WriteStartElement("defaultFemaleChar");
                        xmlWriter.WriteAttributeString("path", this._defaultFemaleChar);
                        xmlWriter.WriteEndElement();
                    }

#if KOIKATSU
                    {
                        xmlWriter.WriteStartElement("defaultMaleChar");
                        xmlWriter.WriteAttributeString("path", this._defaultMaleChar);
                        xmlWriter.WriteEndElement();
                    }
#endif

#if HONEYSELECT
                    {
                        xmlWriter.WriteStartElement("improveNeoUI");
                        xmlWriter.WriteAttributeString("enabled", XmlConvert.ToString(this._improveNeoUI));
                        xmlWriter.WriteEndElement();
                    }
#endif

                    {
                        xmlWriter.WriteStartElement("optimizeNeo");
                        xmlWriter.WriteAttributeString("enabled", XmlConvert.ToString(this._optimizeNeo));
                        xmlWriter.WriteEndElement();
                    }

#if HONEYSELECT
                    {
                        xmlWriter.WriteStartElement("enableGenericFK");
                        xmlWriter.WriteAttributeString("enabled", XmlConvert.ToString(this._enableGenericFK));
                        xmlWriter.WriteEndElement();
                    }
#endif

                    {
                        xmlWriter.WriteStartElement("debug");
                        xmlWriter.WriteAttributeString("enabled", XmlConvert.ToString(this._debugEnabled));
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

#if HONEYSELECT
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
#endif

                    {
                        xmlWriter.WriteStartElement("vsync");
                        xmlWriter.WriteAttributeString("enabled", XmlConvert.ToString(QualitySettings.vSyncCount != 0));
                        xmlWriter.WriteEndElement();
                    }

#if HONEYSELECT
                    {
                        xmlWriter.WriteStartElement("fourKManagerDithering");
                        xmlWriter.WriteAttributeString("enabled", XmlConvert.ToString(this._fourKManagerDithering));
                        xmlWriter.WriteEndElement();
                    }
#endif

                    {
                        xmlWriter.WriteStartElement("automaticMemoryClean");
                        xmlWriter.WriteAttributeString("enabled", XmlConvert.ToString(this._automaticMemoryClean));
                        xmlWriter.WriteAttributeString("interval", XmlConvert.ToString(this._automaticMemoryCleanInterval));
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

#if HONEYSELECT
                        {
                            xmlWriter.WriteStartElement("ssr");
                            xmlWriter.WriteAttributeString("enabled", XmlConvert.ToString(this._ssrEnabled));
                            xmlWriter.WriteEndElement();
                        }
#elif KOIKATSU
                        {
                            xmlWriter.WriteStartElement("selfShadow");
                            xmlWriter.WriteAttributeString("enabled", XmlConvert.ToString(this._selfShadowEnabled));
                            xmlWriter.WriteEndElement();
                        }
#endif

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
#if HONEYSELECT
            UIUtility.SetCustomFont("mplus-1c-medium");
#elif KOIKATSU
            UIUtility.SetCustomFont(this._binary == Binary.Game ? "JKG-M_3" : "mplus-1c-medium");
#endif
        }

        public void OnLevelWasInitialized(int level)
        {
            this._go = new GameObject("HSUS");
            this._go.AddComponent<ObjectTreeDebug>();
            this._routines = this._go.AddComponent<RoutinesComponent>();

#if HONEYSELECT
            if (level == 3)
#elif KOIKATSU
            if (level == 1)
#endif
                this.SetProcessAffinity();
            switch (this._binary)
            {
                case Binary.Game:
#if HONEYSELECT
                    if (this._optimizeCharaMaker)
                        this.InitFasterCharaMakerLoading();
                    if (level == 21 && string.IsNullOrEmpty(this._defaultFemaleChar) == false)
                        this.LoadCustomDefault(Path.Combine(Path.Combine(Path.Combine(UserData.Path, "chara"), "female"), this._defaultFemaleChar).Replace("\\", "/"));
#elif KOIKATSU
                    if (level == 2)
                    {
                        this.ExecuteDelayed(() =>
                        {
                            switch (CustomBase.Instance.modeSex)
                            {
                                case 0:
                                    if (string.IsNullOrEmpty(this._defaultMaleChar) == false)
                                        this.LoadCustomDefault(UserData.Path + "chara/male/" + this._defaultMaleChar);
                                    break;
                                case 1:
                                    if (string.IsNullOrEmpty(this._defaultFemaleChar) == false)
                                        this.LoadCustomDefault(UserData.Path + "chara/female/" + this._defaultFemaleChar);
                                    break;
                            }
                        });
                    }
#endif
                    break;
                case Binary.Neo:
#if HONEYSELECT
                    if (level == 3)
#elif KOIKATSU
                    if (level == 1)
#endif
                    {
                        if (this._deleteConfirmation)
                            this.InitDeleteConfirmationDialog();
#if HONEYSELECT
                        if (this._improveNeoUI)
                            this.ImproveNeoUI();
                        if (this._fingersFkCopyButtons)
                            this._routines.ExecuteDelayed(this.InitFingersFKCopyButtons, 1);
#endif
                        if (this._improvedTransformOperations)
                            GameObject.Find("StudioScene").transform.Find("Canvas Guide Input").gameObject.AddComponent<TransformOperations>();
                        //UnityEngine.Debug.LogError("currentDisplay " + Camera.main.targetDisplay);
                        //foreach (Display display in Display.displays)
                        //{
                        //    UnityEngine.Debug.LogError("display " + display.systemWidth + " " + display.systemHeight);
                        //}
                        //Display.displays[1].Activate();
                        //Camera secondCamera = new GameObject("Second Camera", typeof(Camera)).GetComponent<Camera>();
                        //secondCamera.targetDisplay = 1;
                    }
                    break;
            }
            this.InitUIScale();
#if HONEYSELECT
            if (this._disableShortcutsWhenTyping)
                this._go.AddComponent<ShortcutsDisabler>();
#endif
        }

#if HONEYSELECT
        public void OnUpdate()
#elif KOIKATSU
        void Update()
#endif
        {
            if (this._automaticMemoryClean)
            {
                if (Time.unscaledTime - this._lastCleanup > this._automaticMemoryCleanInterval)
                {
                    Resources.UnloadUnusedAssets();
                    GC.Collect();
                    this._lastCleanup = Time.unscaledTime;
                    if (EventSystem.current.sendNavigationEvents)
                        EventSystem.current.sendNavigationEvents = false;
                }
            }

            if (this._lastScreenWidth != Screen.width || this._lastScreenHeight != Screen.height)
                this.OnWindowResize();
            this._lastScreenWidth = Screen.width;
            this._lastScreenHeight = Screen.height;

            if (this._asyncMethods.Count != 0)
            {
                IEnumerator method = this._asyncMethods[0];
                if (method.MoveNext() == false)
                    this._asyncMethods.RemoveAt(0);
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
        private void OnWindowResize()
        {
#if HONEYSELECT
            this._routines.ExecuteDelayed(this.ApplyUIScale, 2);
#elif KOIKATSU
            this.ExecuteDelayed(this.ApplyUIScale, 2);
#endif
        }

#if HONEYSELECT
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
                foreach (SmClothesLoad f in Resources.FindObjectsOfTypeAll<SmClothesLoad>())
                {
                    SmClothesLoad_Data.Init(f);
                    break;
                }
                foreach (SmCharaLoad f in Resources.FindObjectsOfTypeAll<SmCharaLoad>())
                {
                    SmCharaLoad_Data.Init(f);
                    break;
                }
                foreach (SmClothes_F f in Resources.FindObjectsOfTypeAll<SmClothes_F>())
                {
                    SmClothes_F_Data.Init(f);
                    break;
                }
                foreach (SmAccessory f in Resources.FindObjectsOfTypeAll<SmAccessory>())
                {
                    SmAccessory_Data.Init(f);
                    break;
                }
                foreach (SmSwimsuit f in Resources.FindObjectsOfTypeAll<SmSwimsuit>())
                {
                    SmSwimsuit_Data.Init(f);
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
            }, 10);
        }
#endif

        private void InitUIScale()
        {
#if HONEYSELECT
            this._routines.ExecuteDelayed(() =>
#elif KOIKATSU
            this.ExecuteDelayed(() =>
#endif
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
                    MonoBehaviour component = (MonoBehaviour)Object.FindObjectOfType(t);
                    if (component != null)
                        component.enabled = false;
                }
            }
            foreach (KeyValuePair<Canvas, CanvasData> pair in this._scaledCanvases)
            {
                if (pair.Key != null && this.ShouldScaleUI(pair.Key))
                {
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
            string path = c.transform.GetPathFrom((Transform)null);
            if (this._binary == Binary.Neo)
            {
                switch (path)
                {
#if HONEYSELECT
                    case "StartScene/Canvas":
                    case "VectorCanvas":
                    case "New Game Object"://AdjustMod/SkintexMod
#elif KOIKATSU
                    case "SceneLoadScene/Canvas Load":
                    case "SceneLoadScene/Canvas Load Work":
                    case "ExitScene/Canvas":
                    case "NotificationScene/Canvas":
                    case "CheckScene/Canvas":
#endif
                        ok = false;
                        break;
                }
            }
            else
            {
                switch (path)
                {
#if HONEYSELECT
                    case "LogoScene/Canvas":
                    case "LogoScene/Canvas (1)":
                    case "CustomScene/CustomControl/CustomUI/BackGround":
                    case "CustomScene/CustomControl/CustomUI/Fusion":
                    case "GameScene/Canvas":
                    case "MapSelectScene/Canvas":
                    case "SubtitleUserInterface":
                    case "ADVScene/Canvas":
#elif KOIKATSU
                    case "CustomScene/CustomRoot/BackUIGroup/CvsBackground":
                    case "CustomScene/CustomRoot/FrontUIGroup/CustomUIGroup/CvsCharaName":
                    case "AssetBundleManager/scenemanager/Canvas":
                    case "FreeHScene/Canvas":
                    case "ExitScene":
                    case "CustomScene/CustomRoot/FrontUIGroup/CvsCaptureFront":
#endif
                    case "TitleScene/Canvas":
                        ok = false;
                        break;
                }
            }
            Canvas parent = c.GetComponentInParent<Canvas>();
            return ok && c.isRootCanvas && (parent == null || parent == c);
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

#if HONEYSELECT
        private void ImproveNeoUI()
        {
            RectTransform rt = GameObject.Find("StudioScene").transform.Find("Canvas Main Menu/01_Add/02_Item/Scroll View Item") as RectTransform;
            rt.offsetMax += new Vector2(60f, 0f);
            rt = GameObject.Find("StudioScene").transform.Find("Canvas Main Menu/01_Add/02_Item/Scroll View Item/Viewport") as RectTransform;
            rt.offsetMax += new Vector2(60f, 0f);
            rt = GameObject.Find("StudioScene").transform.Find("Canvas Main Menu/01_Add/02_Item/Scroll View Item/Viewport/Content") as RectTransform;
            rt.offsetMax += new Vector2(60f, 0f);

            VerticalLayoutGroup group = GameObject.Find("StudioScene").transform.Find("Canvas Main Menu/01_Add/02_Item/Scroll View Item/Viewport/Content").GetComponent<VerticalLayoutGroup>();
            group.childForceExpandWidth = true;
            group.padding = new RectOffset(group.padding.left + 4, group.padding.right + 24, group.padding.top, group.padding.bottom);
            GameObject.Find("StudioScene").transform.Find("Canvas Main Menu/01_Add/02_Item/Scroll View Item/Viewport/Content").GetComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            Text t = GameObject.Find("StudioScene").transform.Find("Canvas Main Menu/01_Add/02_Item/Scroll View Item/node/Text").GetComponent<Text>();
            t.resizeTextForBestFit = true;
            t.resizeTextMinSize = 2;
            t.resizeTextMaxSize = 100;
        }

        private void InitFingersFKCopyButtons()
        {
            RectTransform toggle = GameObject.Find("StudioScene/Canvas Main Menu/02_Manipulate/00_Chara/02_Kinematic/00_FK/Toggle Right Hand").transform as RectTransform;
            Button b = UIUtility.CreateButton("Copy Right Fingers Button", toggle.parent, "From Anim");
            RectTransform rt = (RectTransform)b.transform;
            rt.SetRect(toggle.anchorMin, toggle.anchorMax, new Vector2(toggle.offsetMax.x + 4f, toggle.offsetMin.y), new Vector2(toggle.offsetMax.x + 64f, toggle.offsetMax.y));
            
            GameObject go = GameObject.Find("StudioScene/Canvas Main Menu/02_Manipulate/00_Chara/02_Kinematic/00_FK/Toggle Right Hand Control View");
            if (go != null)
            {
                rt.offsetMin += new Vector2(18, 0f);
                rt = (RectTransform)go.transform;
                rt.anchoredPosition -= new Vector2(11f, 0f);
            }

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
            rt = (RectTransform)b.transform;
            b.transform.SetRect(toggle.anchorMin, toggle.anchorMax, new Vector2(toggle.offsetMax.x + 4f, toggle.offsetMin.y), new Vector2(toggle.offsetMax.x + 64f, toggle.offsetMax.y));
            go = GameObject.Find("StudioScene/Canvas Main Menu/02_Manipulate/00_Chara/02_Kinematic/00_FK/Toggle Left Hand Control View");
            if (go != null)
            {
                rt.offsetMin += new Vector2(18, 0f);
                rt = (RectTransform)go.transform;
                rt.anchoredPosition -= new Vector2(11f, 0f);
            }

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
#endif
#if HONEYSELECT
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

#elif KOIKATSU
        private void LoadCustomDefault(string path)
        {
            ChaControl chaCtrl = Singleton<CustomBase>.Instance.chaCtrl;
            CustomBase.Instance.chaCtrl.chaFile.LoadFileLimited(path, chaCtrl.sex, true, true, true, true, true);
            chaCtrl.ChangeCoordinateType(true);
            chaCtrl.Reload(false, false, false, false);
            CustomBase.Instance.updateCustomUI = true;
            CustomHistory.Instance.Add5(chaCtrl, chaCtrl.Reload, false, false, false, false);
        }
#endif

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