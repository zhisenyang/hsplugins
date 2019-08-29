using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Xml;
using FBSAssist;
#if HONEYSELECT
using CustomMenu;
using IllusionPlugin;
using Studio;
using StudioFileCheck;
#elif KOIKATSU
using BepInEx;
using ChaCustom;
using UnityEngine.SceneManagement;
#endif
using Harmony;
using ToolBox;
using UILib;
using UnityEngine;
using UnityEngine.EventSystems;
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
        internal const string _version = "1.8.0b";
#elif KOIKATSU
        internal const string _version = "1.0.1";
#endif

        #region Private Types
        internal enum Binary
        {
            Neo,
            Game,
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
        internal bool _removeIsNew = true;
        internal bool _asyncLoading = false;
        internal Color _subFoldersColor = Color.cyan;
        internal float _gameUIScale = 1f;
        internal float _neoUIScale = 1f;
        internal bool _deleteConfirmation = true;
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
#if HONEYSELECT
        internal bool _miniProfilerEnabled = true;
        internal bool _miniProfilerStartCollapsed = true;
#endif
        internal bool _improvedTransformOperations = true;
        internal bool _autoJointCorrection = true;
        internal bool _eyesBlink = false;
        internal bool _cameraSpeedShortcuts = true;
        internal bool _alternativeCenterToObject = true;
        internal bool _fingersFkCopyButtons = true;
        internal bool _automaticMemoryClean = false;
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
#if HONEYSELECT
        internal Color _fkHairColor = Color.white;
        internal Color _fkNeckColor = Color.white;
        internal Color _fkChestColor = Color.white;
        internal Color _fkBodyColor = Color.white;
        internal Color _fkRightHandColor = Color.white;
        internal Color _fkLeftHandColor = Color.white;
        internal Color _fkSkirtColor = Color.white;
        internal Color _fkItemsColor = Color.white;
#endif

        internal static HSUS _self;
        internal GameObject _go;
        internal RoutinesComponent _routines;
        internal Binary _binary;
        internal Sprite _searchBarBackground;
        internal Sprite _buttonBackground;
        private float _lastCleanup;
        private int _lastScreenWidth;
        private int _lastScreenHeight;
        internal TranslationDelegate _translationMethod;
        internal readonly List<IEnumerator> _asyncMethods = new List<IEnumerator>();
        internal string _currentCharaPathGame = "";
        internal string _currentClothesPathGame = "";
        #endregion

        #region Public Accessors
#if HONEYSELECT
        public string Name { get { return "HSUS"; } }
        public string Version { get { return _version; } }
        public string[] Filter { get { return new[] {"HoneySelect_64", "HoneySelect_32", "StudioNEO_32", "StudioNEO_64", "Honey Select Unlimited_64", "Honey Select Unlimited_32" }; } }
#endif
        public bool optimizeCharaMaker { get { return this._optimizeCharaMaker; } }
        public static HSUS self { get { return _self; } }
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
                case "Honey Select Unlimited_32":
                case "Honey Select Unlimited_64":                    
#elif KOIKATSU
                case "Koikatsu Party":
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

            UnityEngine.Debug.Log("ColorSpace " + QualitySettings.activeColorSpace);

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
                                    case "removeIsNew":
                                        if (childNode.Attributes["enabled"] != null)
                                            this._removeIsNew = XmlConvert.ToBoolean(childNode.Attributes["enabled"].Value);
                                        break;
                                    case "subFoldersColor":
                                        ColorUtility.TryParseHtmlString("#" + childNode.Attributes["value"].Value, out this._subFoldersColor);
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
#if HONEYSELECT
                            if (node.Attributes["miniProfilerEnabled"] != null)
                                this._miniProfilerEnabled = XmlConvert.ToBoolean(node.Attributes["miniProfilerEnabled"].Value);
                            if (node.Attributes["miniProfilerStartCollapsed"] != null)
                                this._miniProfilerStartCollapsed = XmlConvert.ToBoolean(node.Attributes["miniProfilerStartCollapsed"].Value);
#endif
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
                        case "automaticMemoryClean":
                            if (node.Attributes["enabled"] != null)
                                this._automaticMemoryClean = XmlConvert.ToBoolean(node.Attributes["enabled"].Value);
                            if (node.Attributes["interval"] != null)
                                this._automaticMemoryCleanInterval = XmlConvert.ToInt32(node.Attributes["interval"].Value);
                            break;
#if HONEYSELECT
                        case "fkColors":
                            foreach (XmlNode childNode in node.ChildNodes)
                            {
                                switch (childNode.Name)
                                {
                                    case "hair":
                                        ColorUtility.TryParseHtmlString("#" + childNode.Attributes["color"].Value, out this._fkHairColor);
                                        break;
                                    case "neck":
                                        ColorUtility.TryParseHtmlString("#" + childNode.Attributes["color"].Value, out this._fkNeckColor);
                                        break;
                                    case "chest":
                                        ColorUtility.TryParseHtmlString("#" + childNode.Attributes["color"].Value, out this._fkChestColor);
                                        break;
                                    case "body":
                                        ColorUtility.TryParseHtmlString("#" + childNode.Attributes["color"].Value, out this._fkBodyColor);
                                        break;
                                    case "rightHand":
                                        ColorUtility.TryParseHtmlString("#" + childNode.Attributes["color"].Value, out this._fkRightHandColor);
                                        break;
                                    case "leftHand":
                                        ColorUtility.TryParseHtmlString("#" + childNode.Attributes["color"].Value, out this._fkLeftHandColor);
                                        break;
                                    case "skirt":
                                        ColorUtility.TryParseHtmlString("#" + childNode.Attributes["color"].Value, out this._fkSkirtColor);
                                        break;
                                    case "items":
                                        ColorUtility.TryParseHtmlString("#" + childNode.Attributes["color"].Value, out this._fkItemsColor);
                                        break;
                                }
                            }
                            break;
#endif
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
                UIScale.Init();
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

                        {
                            xmlWriter.WriteStartElement("removeIsNew");
                            xmlWriter.WriteAttributeString("enabled", XmlConvert.ToString(this._removeIsNew));
                            xmlWriter.WriteEndElement();
                        }

                        {
                            xmlWriter.WriteStartElement("subFoldersColor");
                            xmlWriter.WriteAttributeString("value", ColorUtility.ToHtmlStringRGB(this._subFoldersColor));
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
#if HONEYSELECT
                        xmlWriter.WriteAttributeString("miniProfilerEnabled", XmlConvert.ToString(this._miniProfilerEnabled));
                        xmlWriter.WriteAttributeString("miniProfilerStartCollapsed", XmlConvert.ToString(this._miniProfilerStartCollapsed));
#endif
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

                    {
                        xmlWriter.WriteStartElement("automaticMemoryClean");
                        xmlWriter.WriteAttributeString("enabled", XmlConvert.ToString(this._automaticMemoryClean));
                        xmlWriter.WriteAttributeString("interval", XmlConvert.ToString(this._automaticMemoryCleanInterval));
                        xmlWriter.WriteEndElement();
                    }

#if HONEYSELECT
                    {
                        xmlWriter.WriteStartElement("fkColors");
                        {
                            xmlWriter.WriteStartElement("hair");
                            xmlWriter.WriteAttributeString("color", ColorUtility.ToHtmlStringRGB(this._fkHairColor));
                            xmlWriter.WriteEndElement();
                        }
                        {
                            xmlWriter.WriteStartElement("neck");
                            xmlWriter.WriteAttributeString("color", ColorUtility.ToHtmlStringRGB(this._fkNeckColor));
                            xmlWriter.WriteEndElement();
                        }
                        {
                            xmlWriter.WriteStartElement("chest");
                            xmlWriter.WriteAttributeString("color", ColorUtility.ToHtmlStringRGB(this._fkChestColor));
                            xmlWriter.WriteEndElement();
                        }
                        {
                            xmlWriter.WriteStartElement("body");
                            xmlWriter.WriteAttributeString("color", ColorUtility.ToHtmlStringRGB(this._fkBodyColor));
                            xmlWriter.WriteEndElement();
                        }
                        {
                            xmlWriter.WriteStartElement("rightHand");
                            xmlWriter.WriteAttributeString("color", ColorUtility.ToHtmlStringRGB(this._fkRightHandColor));
                            xmlWriter.WriteEndElement();
                        }
                        {
                            xmlWriter.WriteStartElement("leftHand");
                            xmlWriter.WriteAttributeString("color", ColorUtility.ToHtmlStringRGB(this._fkLeftHandColor));
                            xmlWriter.WriteEndElement();
                        }
                        {
                            xmlWriter.WriteStartElement("skirt");
                            xmlWriter.WriteAttributeString("color", ColorUtility.ToHtmlStringRGB(this._fkSkirtColor));
                            xmlWriter.WriteEndElement();
                        }
                        {
                            xmlWriter.WriteStartElement("items");
                            xmlWriter.WriteAttributeString("color", ColorUtility.ToHtmlStringRGB(this._fkItemsColor));
                            xmlWriter.WriteEndElement();
                        }
                        xmlWriter.WriteEndElement();
                    }
#endif

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
            if (this._debugEnabled)
                this._go.AddComponent<DebugConsole>();
            this._routines = this._go.AddComponent<RoutinesComponent>();

            this.SetProcessAffinity();
            switch (this._binary)
            {
                case Binary.Game:
#if HONEYSELECT
                    if (this._optimizeCharaMaker)
                        OptimizeCharaMaker.Do(level);
                    this._currentCharaPathGame = "";
                    this._currentClothesPathGame = "";
#endif
                    DefaultChars.Do(level);
                    break;
                case Binary.Neo:
#if HONEYSELECT
                    if (level == 3)
#elif KOIKATSU
                    if (level == 1)
#endif
                    {
#if HONEYSELECT
                        if (this._improveNeoUI)
                            ImproveNeoUI.Do();
                        if (this._fingersFkCopyButtons)
                            FingersFKCopyButtons.Do();
#endif
                        if (this._improvedTransformOperations)
                            ImprovedTransformOperations.Do();
                        if (this._deleteConfirmation)
                            DeleteConfirmation.Do();

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
            UIScale.Init();
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
                try
                {
                    if (method.MoveNext() == false)
                        this._asyncMethods.RemoveAt(0);
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogError(e);
                    this._asyncMethods.RemoveAt(0);
                }
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
            this._routines.ExecuteDelayed(UIScale.Do, 2);
#elif KOIKATSU
            this.ExecuteDelayed(UIScale.Do, 2);
#endif
        }

        private void SetProcessAffinity()
        {
            this._routines.ExecuteDelayed(() =>
            {
                const long affinityMask = 0;
                Process proc = Process.GetCurrentProcess();
                proc.ProcessorAffinity = (IntPtr)affinityMask;

                foreach (ProcessThread thread in proc.Threads)
                    thread.ProcessorAffinity = (IntPtr)affinityMask;
            });
            this._routines.ExecuteDelayed(() =>
            {
                const long affinityMask = Int64.MaxValue;
                Process proc = Process.GetCurrentProcess();
                proc.ProcessorAffinity = (IntPtr)affinityMask;

                foreach (ProcessThread thread in proc.Threads)
                    thread.ProcessorAffinity = (IntPtr)affinityMask;
            }, 300);
        }
#endregion

    }

    //[HarmonyPatch(typeof(Studio.Info), "LoadItemLoadInfoCoroutine", new[] { typeof(string), typeof(string) })]
    //public class Testetetetetet
    //{
    //    public static void Prefix(string _bundlePath, string _regex)
    //    {
    //        string[] files = (string[])Studio.Info.Instance.CallPrivate("FindAllAssetName", _bundlePath, _regex);
    //        UnityEngine.Debug.LogError(_bundlePath);
    //        if (files != null)
    //            UnityEngine.Debug.LogError(files.Length);
    //        else
    //        {
    //            UnityEngine.Debug.LogError("null");
    //            foreach (KeyValuePair<string, AssetBundleManager.BundlePack> pair in AssetBundleManager.ManifestBundlePack)
    //            {
    //                UnityEngine.Debug.LogError("lol " + pair.Key);
    //            }
    //        }
    //    }
    //}
}