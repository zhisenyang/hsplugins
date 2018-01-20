using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml;
using CustomMenu;
using Harmony;
using IllusionPlugin;
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

        private GameObject _go;
        private RoutinesComponent _routines;
        private HashSet<Canvas> _scaledCanvases = new HashSet<Canvas>();
        private Binary _binary;
        private Sprite _searchBarBackground;


        #endregion

        #region Public Accessors
        public string Name { get { return "HSUS"; } }
        public string Version { get { return "1.2.1"; } }
        public string[] Filter { get { return new[] { "HoneySelect_64", "HoneySelect_32", "StudioNEO_32", "StudioNEO_64" }; } }
        public static HSUS self { get; private set; }
        public bool optimizeCharaMaker { get { return this._optimizeCharaMaker; } }
        public Sprite searchBarBackground { get { return this._searchBarBackground; } }
        public bool improveNeoUI { get { return this._improveNeoUI; } }
        public bool optimizeNeo { get { return this._optimizeNeo; } }
        public bool enableGenericFK { get { return this._enableGenericFK; } }
        public KeyCode debugShortcut { get { return this._debugShortcut; } }
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
                        UnityEngine.Debug.Log(type.FullName);
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
            return;
            List<string> assemblies = new List<string>()
            {
                //"WideSliderManaged",
                //"ShortcutsHSParty",
                //"SBPRAnimationPlugin",
                //"PCAnimationPlugin",
                //"MoreAccessories",
                //"Kisama",
                //"HSUS",
                //"SkinTexMod",
                //"HSUncensor2SkinTexModAdapter",
                //"HSUncensor2",
                //"HSStudioNEOAddon",
                //"HSStudioInvisible",
                //"HSPENeo",
                //"HSPE",
                //"HoneyShot",
                //"HSStudioAddonPlugin",
                //"HaremAnimationPlugin",
                //"GgmodForHS_Studio",
                //"GgmodForHS_NEO",
                //"GgmodForHS",
                //"CMModForHS",
                //"CharExtSave",
                //"AdjustMod",
                //"HSStudioNEOExtSave",
                //"AdditionalBoneModifierStudioNEO",
                //"AdditionalBoneModifierStudio",
                //"AdditionalBoneModifier",
                //"UnityEngine.UI.Translation",
                //"UnityEngine.UI",
                "Assembly-UnityScript",
                "Assembly-CSharp",
                "Assembly-CSharp-firstpass",
                //"UnityEngine"
            };
            string invalidChars = System.Text.RegularExpressions.Regex.Escape(new string(System.IO.Path.GetInvalidFileNameChars()));
            string invalidRegStr = string.Format(@"([{0}]*\.+$)|([{0}]+)", invalidChars);

            int j = 0;
            foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (string s in assemblies)
                {
                    if (a.FullName.StartsWith(s))
                    {

                        try
                        {
                            foreach (Type type in a.GetTypes())
                            {
                                StringBuilder b = new StringBuilder();
                                b.Append("extern alias tr;\n");
                                b.Append("using System.Collections.Generic;\n");
                                b.Append("using Harmony;\n");
                                b.Append("using UnityEngine;\n");
                                b.Append("using System.Diagnostics;\n");

                                b.Append("namespace Profile\n");
                                b.Append("{\n");


                                if (type.GetCustomAttributes(true).Any(att => att is HarmonyAttribute || att.ToString().EndsWith("HarmonyPatch") || att is CompilerGeneratedAttribute) || type.IsGenericType || type.IsNested || type.IsVisible == false || type.IsNotPublic || type.FullName.StartsWith("Harmony"))
                                    continue;
                                foreach (MethodInfo methodInfo in type.GetMethods())
                                {
                                    if (methodInfo.IsGenericMethod || methodInfo.GetCustomAttributes(true).Any(att => att is CompilerGeneratedAttribute))
                                        continue;
                                    StringBuilder param = new StringBuilder();
                                    ParameterInfo[] parameters = methodInfo.GetParameters();
                                    if (parameters.Any(p => p.ParameterType.IsNestedPrivate || type.IsNotPublic || type.IsVisible == false))
                                        continue;
                                    param.Append("new []{");
                                    for (int i = 0; i < parameters.Length; i++)
                                    {                                        
                                        param.Append("typeof(" + this.GetTypeName(parameters[i].ParameterType, false).Replace("&", "") + ")" + (i != parameters.Length - 1 ? ", " : ""));
                                    }
                                    param.Append("}");

                                    StringBuilder param2 = new StringBuilder();
                                    for (int i = 0; i < parameters.Length; i++)
                                    {
                                        ParameterInfo inf = parameters[i];
                                        param2.Append(this.GetTypeName(inf.ParameterType, true) + " " + (inf.Name.Equals("object") || inf.Name.Equals("default") ? "@":"") + inf.Name + (i != parameters.Length - 1 ? ", " : ""));
                                    }
                                    string n = type.FullName + "." + methodInfo.Name + "_" + j;
                                    b.Append("[HarmonyPatch(typeof(" +(type.FullName.Contains("IniFile") || type.FullName.Contains(".Translation") ? "tr::": "") + (s.StartsWith("Assembly") || s.Equals("UnityEngine") ? "" : s + ".") + type.FullName + "), \"" + methodInfo.Name + "\"" + (parameters.Length != 0 ? ", " + param.ToString() : "") + ")]\n");
                                    b.Append("public class " + type.Name + "_" + methodInfo.Name + "_" + j + "_Patches\n");
                                    b.Append("{\n");

                                    b.Append("public static void Prefix(" + param2.ToString() + ")\n");
                                    b.Append("{\n");
                                    b.Append("Stopwatch sw;\n");
                                    b.Append("if (Profile.self._stopwatches.TryGetValue(\"" + n + "\", out sw) == false)\n");
                                    b.Append("{\n");
                                    b.Append("    sw = new Stopwatch();\n");
                                    b.Append("    Profile.self._stopwatches.Add(\"" + n + "\", sw);\n");
                                    b.Append("}\n");
                                    b.Append("sw.Start();\n");
                                    b.Append("}\n");

                                    b.Append("public static void Postfix(" + param2.ToString() + ")\n");
                                    b.Append("{\n");
                                    b.Append("Stopwatch sw = Profile.self._stopwatches[\"" + n + "\"];\n");
                                    b.Append("sw.Stop();\n");
                                    b.Append("Dictionary<string, long> __t;\n");
                                    b.Append("if (Profile.self._times.TryGetValue(Time.frameCount, out __t) == false)\n");
                                    b.Append("{\n");
                                    b.Append("    __t = new Dictionary<string, long>();\n");
                                    b.Append("    Profile.self._times.Add(Time.frameCount, __t);\n");
                                    b.Append("}\n");
                                    b.Append("if (__t.ContainsKey(\"" + n + "\"))\n");
                                    b.Append("    __t[\"" + n + "\"] += sw.ElapsedMilliseconds;\n");
                                    b.Append("else\n");
                                    b.Append("    __t.Add(\"" + n + "\", sw.ElapsedMilliseconds);\n");
                                    b.Append("sw.Reset();\n");
                                    b.Append("}\n");

                                    b.Append("}\n");
                                    ++j;
                                }

                                b.Append("}\n");

                    

                                File.WriteAllText(@"C:\Users\Joan\hsplugins\Profile\Profile\" + System.Text.RegularExpressions.Regex.Replace(type.FullName, invalidRegStr, "_") + ".cs", b.ToString());
                            }

                        }
                        catch (Exception e)
                        {
                            UnityEngine.Debug.Log("mabite " + e);
                        }
                        break;
                    }
                }
            }
        }

        private string GetTypeName(Type info, bool forSignature)
        {
            string paramName;
            if (info.IsByRef)
            {
                paramName = (forSignature ? "ref ":"") + this.GetTypeName(info.GetElementType(), forSignature);
            }
            else if (info.IsArray)
            {
                paramName = this.GetTypeName(info.GetElementType(), forSignature) + "[]";
            }
            else if (info.IsGenericType||info.IsGenericTypeDefinition)
            {
                paramName = info.FullName;
                int j;
                if ((j = paramName.IndexOf('`')) >= 0)
                    paramName = paramName.Remove(j);
                paramName += "<";
                Type[] args = info.GetGenericArguments();
                for (int i = 0; i < args.Length; i++)
                {
                    Type t = info.GetGenericArguments()[i];
                    paramName += this.GetTypeName(t, forSignature) + (i != args.Length - 1 ? ", " : "");
                }
                paramName += ">";
            }
            else
                paramName = info.FullName;
            return paramName.Replace("&", "").Replace('+', '.');
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
                foreach (Studio.ItemGroupList gr in Resources.FindObjectsOfTypeAll<Studio.ItemGroupList>())
                {
                    UnityEngine.Debug.Log(gr.transform.GetPathFrom(null));
                    break;
                }
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

    //[HarmonyPatch(typeof(HsvColor), "ToRgb", new[]{typeof(HsvColor)
    //})]
    //public class Testetetetetet
    //{
    //    public static void Prefix(HsvColor hsv)
    //    {
    //        float num = (float)(hsv.H / 60.0);
    //        int num2 = (int)Math.Floor((double)num) % 6;
    //        float num3 = num - (float)Math.Floor((double)num);
    //        float num4 = (float)(hsv.V * (1.0 - hsv.S));
    //        float num5 = (float)(hsv.V * (1.0 - hsv.S * num3));
    //        float num6 = (float)(hsv.V * (1.0 - hsv.S * (1.0 - num3)));
    //        UnityEngine.Debug.Log("mabiiiiiiiiiiiiiiiite" + num + " " + num2 + " " + num3 + " " + num4 + " " + num5 + " " + num6);
    //    }
    //}
}