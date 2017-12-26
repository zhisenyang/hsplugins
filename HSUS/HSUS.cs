using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using CustomMenu;
using Harmony;
using IllusionPlugin;
using Manager;
using UILib;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace HSUS
{
    public class HSUS : IEnhancedPlugin
    {
        #region Public Types
        public enum Binary
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

        private GameObject _go;
        private RoutinesComponent _routines;
        private HashSet<Canvas> _scaledCanvases = new HashSet<Canvas>();
        #endregion

        #region Public Accessors
        public string Name { get { return "HSUS"; } }
        public string Version { get { return "1.1.0"; } }
        public string[] Filter { get { return new[] { "HoneySelect_64", "HoneySelect_32", "StudioNEO_32", "StudioNEO_64" }; } }
        public Binary binary { get; private set; }
        public static HSUS self { get; private set; }
        #endregion

        #region Unity Methods
        public void OnApplicationStart()
        {
            self = this;

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
            HarmonyInstance harmony = HarmonyInstance.Create("com.joan6694.hsplugins.hsus");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
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
                }
            }
            UIUtility.Init();
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
            if (this._deleteConfirmation && this.binary == Binary.Neo && level == 3)
                this.InitDeleteConfirmationDialog();
            if (this._disableShortcutsWhenTyping)
                this._go.AddComponent<ShortcutsDisabler>();
            if (this.binary == Binary.Game && level == 21 && string.IsNullOrEmpty(this._defaultFemaleChar) == false)
                this.LoadCustomDefault(Path.Combine(Path.Combine(Path.Combine(UserData.Path, "chara"), "female"), this._defaultFemaleChar).Replace("\\", "/"));
            if (this._improveNeoUI && this.binary == Binary.Neo && level == 3)
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
                foreach (SubMenuControl ct in Resources.FindObjectsOfTypeAll<SubMenuControl>())
                {
                    ct.gameObject.AddComponent<SubMenuControlCustom>().LoadFrom(ct);
                    Object.Destroy(ct);
                }
                foreach (SmClothes_F f in Resources.FindObjectsOfTypeAll<SmClothes_F>())
                {
                    SmClothes_F_Data.Init(f);
                    break;
                }
                foreach (SmCharaLoad f in Resources.FindObjectsOfTypeAll<SmCharaLoad>())
                {
                    f.gameObject.AddComponent<SmCharaLoadCustom>().LoadFrom(f);
                    Object.Destroy(f);
                }
                foreach (SmAccessory f in Resources.FindObjectsOfTypeAll<SmAccessory>())
                {
                    SmAccessory_Data.Init(f);
                    break;
                }
                foreach (SmHair_F f in Resources.FindObjectsOfTypeAll<SmHair_F>())
                {
                    f.gameObject.AddComponent<SmHair_FCustom>().LoadFrom(f);
                    Object.Destroy(f);
                }
                foreach (SmKindColorD f in Resources.FindObjectsOfTypeAll<SmKindColorD>())
                {
                    f.gameObject.AddComponent<SmKindColorDCustom>().LoadFrom(f);
                    Object.Destroy(f);
                }
                foreach (SmKindColorDS f in Resources.FindObjectsOfTypeAll<SmKindColorDS>())
                {
                    f.gameObject.AddComponent<SmKindColorDSCustom>().LoadFrom(f);
                    Object.Destroy(f);
                }
                foreach (SmFaceSkin f in Resources.FindObjectsOfTypeAll<SmFaceSkin>())
                {
                    f.gameObject.AddComponent<SmFaceSkinCustom>().LoadFrom(f);
                    Object.Destroy(f);
                }
                foreach (SmSwimsuit f in Resources.FindObjectsOfTypeAll<SmSwimsuit>())
                {
                    f.gameObject.AddComponent<SmSwimsuitCustom>().LoadFrom(f);
                    Object.Destroy(f);
                }
                foreach (SmClothesLoad f in Resources.FindObjectsOfTypeAll<SmClothesLoad>())
                {
                    f.gameObject.AddComponent<SmClothesLoadCustom>().LoadFrom(f);
                    Object.Destroy(f);
                }
            }, 10);
        }

        private void InitUIScale()
        {
            this._routines.ExecuteDelayed(() =>
            {
                float usedScale = this.binary == Binary.Game ? this._gameUIScale : this._neoUIScale;
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
            if (this.binary == Binary.Neo)
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
                    c.transform.SetAsLastSibling();

                    Image bg = UIUtility.AddImageToObject(UIUtility.CreateNewUIObject(c.transform, "Background"));
                    bg.rectTransform.SetRect();
                    bg.sprite = null;
                    bg.color = new Color(0f, 0f, 0f, 0.5f);
                    bg.raycastTarget = true;

                    Image panel = UIUtility.AddImageToObject(UIUtility.CreateNewUIObject(bg.transform, "Panel"));
                    panel.rectTransform.SetRect(Vector2.zero, Vector2.one, new Vector2(800, 450), new Vector2(-800, -450));
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
                customControl.chainfo.chaFile.Load(path, false, true);
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
                charMale.Reload(false, false, false);
                charMale.maleStatusInfo.visibleSon = false;
            }
            else
            {
                CharFemale charFemale = customControl.chainfo as CharFemale;
                charFemale.Reload(false, false, false);
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
}