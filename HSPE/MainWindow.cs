﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml;
using Harmony;
using HSPE.AMModules;
using RootMotion.FinalIK;
using Studio;
using ToolBox;
using UILib;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Vectrosity;
#if KOIKATSU
using ExtensibleSaveFormat;
using TMPro;
#endif

namespace HSPE
{
    public class MainWindow : MonoBehaviour
    {
        #region Public Static Variables
        internal static MainWindow _self;
        #endregion

        #region Private Types
        private class FKBoneEntry
        {
            public Toggle toggle;
            public Text text;
            public GameObject target;
        }
        #endregion

        #region Constants
#if HONEYSELECT
        private const string _config = "configNEO.xml";
        internal const string _pluginDir = "Plugins\\HSPE\\";
        private const string _studioSavesDir = "StudioNEOScenes\\";
#elif KOIKATSU
        private const string _config = "config.xml";
        internal const string _pluginDir = "BepInEx\\KKPE\\";
        private const string _extSaveKey = "kkpe";
#endif
        internal static readonly GUIStyle _customBoxStyle = new GUIStyle {normal = new GUIStyleState {background = Texture2D.whiteTexture}};
        #endregion

        #region Private Variables
        internal PoseController _poseTarget;
        internal CameraEventsDispatcher _cameraEventsDispatcher;
        private HashSet<TreeNodeObject> _selectedNodes;
        private readonly List<FullBodyBipedEffector> _ikBoneTargets = new List<FullBodyBipedEffector>();
        private readonly Vector3[] _lastIKBonesPositions = new Vector3[14];
        private readonly Quaternion[] _lastIKBonesRotations = new Quaternion[14];
        private readonly List<FullBodyBipedChain> _ikBendGoalTargets = new List<FullBodyBipedChain>();
        private readonly Vector3[] _lastIKBendGoalsPositions = new Vector3[4];
        private readonly Dictionary<string, KeyCode> _nameToKeyCode = new Dictionary<string, KeyCode>();
        private KeyCode _mainWindowKeyCode = KeyCode.H;
        private int _lastObjectCount = 0;
        private int _lastIndex = 0;
        private Vector2 _delta;
        private bool _xMove;
        private bool _yMove;
        private bool _zMove;
        private bool _xRot;
        private bool _yRot;
        private bool _zRot;
        private bool _mouseInAdvMode = false;
        private bool _blockCamera = false;
        private bool _isVisible = false;
        internal Rect _advancedModeRect = new Rect(Screen.width - 650, Screen.height - 370, 650, 370);
        private float _mainWindowSize = 1f;
        private Canvas _ui;
        private GameObject _nothingText;
        private Transform _controls;
        private Slider _movementIntensity;
        private Text _intensityValueText;
        private RectTransform _optionsWindow;
        private float _intensityValue = 1f;
        private readonly Button[] _effectorsButtons = new Button[9];
        private readonly Button[] _bendGoalsButtons = new Button[4];
        private readonly Text[] _effectorsTexts = new Text[9];
        private readonly Text[] _bendGoalsTexts = new Text[4];
        private readonly Button[] _positionButtons = new Button[3];
        private readonly Button[] _rotationButtons = new Button[3];
        private Button _shortcutKeyButton;
        private bool _shortcutRegisterMode = false;
        private KeyCode[] _possibleKeyCodes;
        private bool _positionOperationWorld = false;
        private Toggle _optimizeIKToggle;
        private bool _windowMoving;
        private Image _hspeButtonImage;
        private int _randomId;
        private Toggle _crotchCorrectionToggle;
        private Toggle _leftFootCorrectionToggle;
        private Toggle _rightFootCorrectionToggle;
        private Toggle _crotchCorrectionByDefaultToggle;
        private Toggle _anklesCorrectionByDefaultToggle;
        private bool _crotchCorrectionByDefault;
        private bool _anklesCorrectionByDefault;
        private int _lastScreenWidth = Screen.width;
        private int _lastScreenHeight = Screen.height;
        private Button _copyLeftArmButton;
        private Button _copyRightArmButton;
        private Button _copyLeftLegButton;
        private Button _copyRightLegButton;
        private Button _swapPostButton;
        private Transform _ikBonesButtons;
        private Transform _fkBonesButtons;
        private bool _currentModeIK = true;
        private ScrollRect _fkScrollRect;
        private GameObject _fkBoneTogglePrefab;
        private ToggleGroup _fkToggleGroup;
        private Quaternion _lastFKBonesRotation;
        private readonly List<FKBoneEntry> _fkBoneEntries = new List<FKBoneEntry>();
        private Dictionary<Transform, GuideObject> _dicGuideObject = new Dictionary<Transform, GuideObject>();
        #endregion

        #region Public Accessors
        public Texture2D vectorEndCap { get; private set; }
        public Texture2D vectorMiddle { get; private set; }
        public bool crotchCorrectionByDefault { get { return this._crotchCorrectionByDefaultToggle.isOn; } }
        public bool anklesCorrectionByDefault { get { return this._anklesCorrectionByDefaultToggle.isOn; } }
        #endregion

        #region Unity Methods
        protected virtual void Awake()
        {
            _self = this;

            if (Resources.FindObjectsOfTypeAll<IKExecutionOrder>().Length == 0)
                this.gameObject.AddComponent<IKExecutionOrder>().IKComponents = new IK[0];

            string path = _pluginDir + _config;
            if (File.Exists(path) == false)
                return;
            XmlDocument doc = new XmlDocument();
            doc.Load(path);

            string[] names = Enum.GetNames(typeof(KeyCode));
            this._possibleKeyCodes = (KeyCode[])Enum.GetValues(typeof(KeyCode));

            for (int i = 0; i < names.Length && i < this._possibleKeyCodes.Length; i++)
                this._nameToKeyCode.Add(names[i], this._possibleKeyCodes[i]);

            foreach (XmlNode node in doc.DocumentElement.ChildNodes)
            {
                switch (node.Name)
                {
                    case "mainWindowSize":
                        if (node.Attributes["value"] != null)
                            this._mainWindowSize = XmlConvert.ToSingle(node.Attributes["value"].Value);
                        break;
                    case "mainWindowShortcut":
                        if (node.Attributes["value"] != null && this._nameToKeyCode.ContainsKey(node.Attributes["value"].Value))
                            this._mainWindowKeyCode = this._nameToKeyCode[node.Attributes["value"].Value];
                        break;
                    case "advancedModeWindowSize":
                        if (node.Attributes["x"] != null && node.Attributes["y"] != null)
                        {
                            this._advancedModeRect.xMin = this._advancedModeRect.xMax - XmlConvert.ToInt32(node.Attributes["x"].Value);                            
                            this._advancedModeRect.yMin = this._advancedModeRect.yMax - XmlConvert.ToInt32(node.Attributes["y"].Value);                            
                        }
                        break;
                    case "femaleShortcuts":
                        foreach (XmlNode shortcut in node.ChildNodes)
                            if (shortcut.Attributes["path"] != null)
                                BonesEditor._femaleShortcuts.Add(shortcut.Attributes["path"].Value, shortcut.Attributes["path"].Value.Split('/').Last());
                        break;
                    case "maleShortcuts":
                        foreach (XmlNode shortcut in node.ChildNodes)
                            if (shortcut.Attributes["path"] != null)
                                BonesEditor._maleShortcuts.Add(shortcut.Attributes["path"].Value, shortcut.Attributes["path"].Value.Split('/').Last());
                        break;
                    case "itemShortcuts":
                        foreach (XmlNode shortcut in node.ChildNodes)
                            if (shortcut.Attributes["path"] != null)
                                BonesEditor._itemShortcuts.Add(shortcut.Attributes["path"].Value, shortcut.Attributes["path"].Value.Split('/').Last());
                        break;
                    case "boneAliases":
                        foreach (XmlNode alias in node.ChildNodes)
                            if (alias.Attributes["key"] != null && alias.Attributes["value"] != null)
                                BonesEditor._boneAliases.Add(alias.Attributes["key"].Value, alias.Attributes["value"].Value);
                        break;
                    case "crotchCorrectionByDefault":
                        if (node.Attributes["value"] != null)
                            this._crotchCorrectionByDefault = XmlConvert.ToBoolean(node.Attributes["value"].Value);
                        break;
                    case "anklesCorrectionByDefault":
                        if (node.Attributes["value"] != null)
                            this._anklesCorrectionByDefault = XmlConvert.ToBoolean(node.Attributes["value"].Value);
                        break;
                }
            }
            PoseController.InstallOnParentageEvent();
            OCIChar_ChangeChara_Patches.onChangeChara += this.OnCharacterReplaced;
            OCIChar_LoadClothesFile_Patches.onLoadClothesFile += this.OnLoadClothesFile;
            OCIChar_SetCoordinateInfo_Patches.onSetCoordinateInfo += this.OnCoordinateReplaced;
            this._cameraEventsDispatcher = Camera.main.gameObject.AddComponent<CameraEventsDispatcher>();
            this._dicGuideObject = (Dictionary<Transform, GuideObject>)GuideObjectManager.Instance.GetPrivate("dicGuideObject");
#if HONEYSELECT
            HSExtSave.HSExtSave.RegisterHandler("hspe", null, null, this.OnSceneLoad, this.OnSceneImport, this.OnSceneSave, null, null);
#elif KOIKATSU
            ExtendedSave.CardBeingLoaded += this.OnCharaLoad;
            ExtendedSave.CardBeingSaved += this.OnCharaSave;
            ExtendedSave.SceneBeingLoaded += this.OnSceneLoad;
            ExtendedSave.SceneBeingImported += this.OnSceneImport;
            ExtendedSave.SceneBeingSaved += this.OnSceneSave;
#endif
            this._randomId = (int)(UnityEngine.Random.value * UInt32.MaxValue);

        }

        void Start()
        {
            this.SpawnGUI();
            this._crotchCorrectionByDefaultToggle.isOn = this._crotchCorrectionByDefault;
            this._anklesCorrectionByDefaultToggle.isOn = this._anklesCorrectionByDefault;
            this._selectedNodes = (HashSet<TreeNodeObject>)Studio.Studio.Instance.treeNodeCtrl.GetPrivate("hashSelectNode");
        }

        protected virtual void Update()
        {
            if (this._lastScreenWidth != Screen.width || this._lastScreenHeight != Screen.height)
                this.OnWindowResize();
            this._lastScreenWidth = Screen.width;
            this._lastScreenHeight = Screen.height;

            int objectCount = Studio.Studio.Instance.dicObjectCtrl.Count;
            if (objectCount != this._lastObjectCount)
            {
                if (objectCount > this._lastObjectCount)
                    this.OnObjectAdded();
                this._lastIndex = Studio.Studio.Instance.sceneInfo.CheckNewIndex();
            }
            this._lastObjectCount = Studio.Studio.Instance.dicObjectCtrl.Count;


            PoseController last = this._poseTarget;
            TreeNodeObject treeNodeObject = this._selectedNodes.FirstOrDefault();
            if (treeNodeObject != null)
            {
                ObjectCtrlInfo info;
                if (Studio.Studio.Instance.dicInfo.TryGetValue(treeNodeObject, out info))
                    this._poseTarget = info.guideObject.transformTarget.GetComponent<PoseController>();
            }
            else
                this._poseTarget = null;
            if (last != this._poseTarget)
                this.OnTargetChange(last);
            this.GUILogic();

            //if (Input.GetKeyDown(KeyCode.A))
            //{
            //    PoseController[] controllers = GameObject.FindObjectsOfType<PoseController>();
            //    string xml;
            //    using (StringWriter stream = new StringWriter())
            //    using (XmlTextWriter writer = new XmlTextWriter(stream))
            //    {
            //        writer.WriteStartElement("character");
            //        controllers[1].SaveXml(writer);
            //        writer.WriteEndElement();
            //        xml = stream.ToString();
            //    }
            //    XmlDocument xmlDocument = new XmlDocument();
            //    xmlDocument.LoadXml(xml);
            //    controllers[0].ScheduleLoad(xmlDocument.FirstChild, (b) => {});
            //}

            this.StaticUpdate();
        }

        // "Static" Update stuff for the other classes
        private void StaticUpdate()
        {
            if (AdvancedModeModule._repeatCalled)
                AdvancedModeModule._repeatTimer += Time.unscaledDeltaTime;
            else
                AdvancedModeModule._repeatTimer = 0f;
            AdvancedModeModule._repeatCalled = false;
        }

        protected virtual void OnGUI()
        {
            if (this._poseTarget != null)
            {
                if (PoseController._drawAdvancedMode)
                {
                    Color c = GUI.backgroundColor;
                    GUI.backgroundColor = new Color(0.6f, 0.6f, 0.6f, 0.2f);
                    for (int i = 0; i < 3; ++i)
                        GUI.Box(this._advancedModeRect, "", _customBoxStyle);
                    GUI.backgroundColor = c;
                    this._advancedModeRect = GUILayout.Window(this._randomId, this._advancedModeRect, this._poseTarget.AdvancedModeWindow, "Advanced mode");
                    this._mouseInAdvMode = this._advancedModeRect.Contains(Event.current.mousePosition);
                }
                else
                    this._mouseInAdvMode = false;
            }
        }
        protected virtual void OnDestroy()
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
#if HONEYSELECT
                    xmlWriter.WriteAttributeString("version", HSPE.versionNum.ToString());
#elif KOIKATSU
                    xmlWriter.WriteAttributeString("version", KKPE.versionNum.ToString());
#endif

                    xmlWriter.WriteStartElement("mainWindowSize");
                    xmlWriter.WriteAttributeString("value", XmlConvert.ToString(this._mainWindowSize));
                    xmlWriter.WriteEndElement();

                    xmlWriter.WriteStartElement("mainWindowShortcut");
                    xmlWriter.WriteAttributeString("value", this._mainWindowKeyCode.ToString());
                    xmlWriter.WriteEndElement();

                    xmlWriter.WriteStartElement("advancedModeWindowSize");
                    xmlWriter.WriteAttributeString("x", XmlConvert.ToString((int)this._advancedModeRect.width));
                    xmlWriter.WriteAttributeString("y", XmlConvert.ToString((int)this._advancedModeRect.height));
                    xmlWriter.WriteEndElement();

                    xmlWriter.WriteStartElement("femaleShortcuts");
                    foreach (KeyValuePair<string, string> kvp in BonesEditor._femaleShortcuts)
                    {
                        xmlWriter.WriteStartElement("shortcut");
                        xmlWriter.WriteAttributeString("path", kvp.Key);
                        xmlWriter.WriteEndElement();
                    }
                    xmlWriter.WriteEndElement();

                    xmlWriter.WriteStartElement("maleShortcuts");
                    foreach (KeyValuePair<string, string> kvp in BonesEditor._maleShortcuts)
                    {
                        xmlWriter.WriteStartElement("shortcut");
                        xmlWriter.WriteAttributeString("path", kvp.Key);
                        xmlWriter.WriteEndElement();
                    }
                    xmlWriter.WriteEndElement();

                    xmlWriter.WriteStartElement("itemShortcuts");
                    foreach (KeyValuePair<string, string> kvp in BonesEditor._itemShortcuts)
                    {
                        xmlWriter.WriteStartElement("shortcut");
                        xmlWriter.WriteAttributeString("path", kvp.Key);
                        xmlWriter.WriteEndElement();
                    }
                    xmlWriter.WriteEndElement();

                    xmlWriter.WriteStartElement("boneAliases");
                    foreach (KeyValuePair<string, string> kvp in BonesEditor._boneAliases)
                    {
                        xmlWriter.WriteStartElement("alias");
                        xmlWriter.WriteAttributeString("key", kvp.Key);
                        xmlWriter.WriteAttributeString("value", kvp.Value);
                        xmlWriter.WriteEndElement();
                    }
                    xmlWriter.WriteEndElement();

                    xmlWriter.WriteStartElement("crotchCorrectionByDefault");
                    xmlWriter.WriteAttributeString("value", XmlConvert.ToString(this._crotchCorrectionByDefaultToggle.isOn));
                    xmlWriter.WriteEndElement();

                    xmlWriter.WriteStartElement("anklesCorrectionByDefault");
                    xmlWriter.WriteAttributeString("value", XmlConvert.ToString(this._anklesCorrectionByDefaultToggle.isOn));
                    xmlWriter.WriteEndElement();

                    xmlWriter.WriteEndElement();
                }
            }
        }
        #endregion

        #region GUI
        private void SpawnGUI()
        {
#if HONEYSELECT
            string oldResourcesDir = _pluginDir + "Resources\\";
            if (Directory.Exists(oldResourcesDir))
                Directory.Delete(oldResourcesDir, true);
#endif

#if HONEYSELECT
            AssetBundle bundle = AssetBundle.LoadFromMemory(Properties.Resources.HSPEResources);
#elif KOIKATSU
            AssetBundle bundle = AssetBundle.LoadFromMemory(Properties.ResourcesKOI.KKPEResources);
#endif
            Texture2D texture = bundle.LoadAsset<Texture2D>("Icon");
            this.vectorEndCap = bundle.LoadAsset<Texture2D>("VectorEndCap");
            this.vectorMiddle = bundle.LoadAsset<Texture2D>("VectorMiddle");
            VectorLine.SetEndCap("vector", EndCap.Back, 0f, -1f, 1f, 4f, this.vectorMiddle, this.vectorEndCap);
            VectorLine.canvas.sortingOrder -= 40;

            {
                RectTransform original = GameObject.Find("StudioScene").transform.Find("Canvas System Menu/01_Button/Button Center").GetComponent<RectTransform>();
                Button hspeButton = Instantiate(original.gameObject).GetComponent<Button>();
                RectTransform hspeButtonRectTransform = hspeButton.transform as RectTransform;
                hspeButton.transform.SetParent(original.parent, true);
                hspeButton.transform.localScale = original.localScale;
                hspeButtonRectTransform.SetRect(original.anchorMin, original.anchorMax, original.offsetMin, original.offsetMax);
#if HONEYSELECT
                hspeButtonRectTransform.anchoredPosition = original.anchoredPosition + new Vector2(40f, 0f);
#elif KOIKATSU
                hspeButtonRectTransform.anchoredPosition = original.anchoredPosition + new Vector2(40f, 80f);
#endif
                this._hspeButtonImage = hspeButton.targetGraphic as Image;
                this._hspeButtonImage.sprite = Sprite.Create(texture, new Rect(0f, 0f, 32, 32), new Vector2(16, 16));
                hspeButton.onClick = new Button.ButtonClickedEvent();
                hspeButton.onClick.AddListener(() =>
                {
                    this._isVisible = !this._isVisible;
                    this._ui.gameObject.SetActive(this._isVisible);
                    this._hspeButtonImage.color = this._isVisible ? Color.green : Color.white;
                });
                EventTrigger.Entry entry = new EventTrigger.Entry() {eventID = EventTriggerType.PointerClick, callback = new EventTrigger.TriggerEvent()};
                entry.callback.AddListener(eventData =>
                {
                    PointerEventData pointerEventData = eventData as PointerEventData;
                    if (pointerEventData != null)
                    {
                        if (pointerEventData.button == PointerEventData.InputButton.Right && this._poseTarget != null)
                            this._poseTarget.ToggleAdvancedMode();
                    }
                });
                hspeButton.gameObject.AddComponent<EventTrigger>().triggers.Add(entry);
                this._hspeButtonImage.color = Color.white;
            }

#if HONEYSELECT
            this._ui = Instantiate(bundle.LoadAsset<GameObject>("HSPECanvas")).GetComponent<Canvas>();
#elif KOIKATSU
            this._ui = Instantiate(bundle.LoadAsset<GameObject>("KKPECanvas")).GetComponent<Canvas>();
#endif
            this._fkBoneTogglePrefab = bundle.LoadAsset<GameObject>("FKBoneTogglePrefab");
            bundle.Unload(false);

            RectTransform bg = (RectTransform)this._ui.transform.Find("BG");
            Transform topContainer = bg.Find("Top Container");
            MovableWindow mw = UIUtility.MakeObjectDraggable(topContainer as RectTransform, (RectTransform)bg, false);
            mw.onPointerDown += this.OnWindowStartDrag;
            mw.onDrag += this.OnWindowDrag;
            mw.onPointerUp += this.OnWindowEndDrag;

            Toggle ikToggle = this._ui.transform.Find("BG/Top Container/Buttons/IK").GetComponent<Toggle>();
            ikToggle.onValueChanged.AddListener((b) =>
            {
                this._ikBonesButtons.gameObject.SetActive(ikToggle.isOn);
                this._fkBonesButtons.gameObject.SetActive(!ikToggle.isOn);
                this._currentModeIK = ikToggle.isOn;
            });
            Toggle fkToggle = this._ui.transform.Find("BG/Top Container/Buttons/FK").GetComponent<Toggle>();
            fkToggle.onValueChanged.AddListener((b) =>
            {
                this._fkBonesButtons.gameObject.SetActive(fkToggle.isOn);
                this._ikBonesButtons.gameObject.SetActive(!fkToggle.isOn);
                this._currentModeIK = !fkToggle.isOn;
            });

            this._nothingText = this._ui.transform.Find("BG/Nothing Text").gameObject;
            //this._nothingText.gameObject.SetActive(false);
            this._controls = this._ui.transform.Find("BG/Controls");
            this._ikBonesButtons = this._ui.transform.Find("BG/Controls/IK Bones Buttons");
            this._fkBonesButtons = this._ui.transform.Find("BG/Controls/FK Bones Buttons");

            Button rightShoulder = this._ui.transform.Find("BG/Controls/IK Bones Buttons/Right Shoulder Button").GetComponent<Button>();
            rightShoulder.onClick.AddListener(() => this.SetBoneTarget(FullBodyBipedEffector.RightShoulder));
            Text t = rightShoulder.GetComponentInChildren<Text>();
            this._effectorsButtons[(int)FullBodyBipedEffector.RightShoulder] = rightShoulder;
            this._effectorsTexts[(int)FullBodyBipedEffector.RightShoulder] = t;

            Button leftShoulder = this._ui.transform.Find("BG/Controls/IK Bones Buttons/Left Shoulder Button").GetComponent<Button>();
            leftShoulder.onClick.AddListener(() => this.SetBoneTarget(FullBodyBipedEffector.LeftShoulder));
            t = leftShoulder.GetComponentInChildren<Text>();
            this._effectorsButtons[(int)FullBodyBipedEffector.LeftShoulder] = leftShoulder;
            this._effectorsTexts[(int)FullBodyBipedEffector.LeftShoulder] = t;

            Button rightArmBendGoal = this._ui.transform.Find("BG/Controls/IK Bones Buttons/Right Arm Bend Goal Button").GetComponent<Button>();
            rightArmBendGoal.onClick.AddListener(() => this.SetBendGoalTarget(FullBodyBipedChain.RightArm));
            t = rightArmBendGoal.GetComponentInChildren<Text>();
            this._bendGoalsButtons[(int)FullBodyBipedChain.RightArm] = rightArmBendGoal;
            this._bendGoalsTexts[(int)FullBodyBipedChain.RightArm] = t;

            Button leftArmBendGoal = this._ui.transform.Find("BG/Controls/IK Bones Buttons/Left Arm Bend Goal Button").GetComponent<Button>();
            leftArmBendGoal.onClick.AddListener(() => this.SetBendGoalTarget(FullBodyBipedChain.LeftArm));
            t = leftArmBendGoal.GetComponentInChildren<Text>();
            this._bendGoalsButtons[(int)FullBodyBipedChain.LeftArm] = leftArmBendGoal;
            this._bendGoalsTexts[(int)FullBodyBipedChain.LeftArm] = t;

            Button rightHand = this._ui.transform.Find("BG/Controls/IK Bones Buttons/Right Hand Button").GetComponent<Button>();
            rightHand.onClick.AddListener(() => this.SetBoneTarget(FullBodyBipedEffector.RightHand));
            t = rightHand.GetComponentInChildren<Text>();
            this._effectorsButtons[(int)FullBodyBipedEffector.RightHand] = rightHand;
            this._effectorsTexts[(int)FullBodyBipedEffector.RightHand] = t;

            Button leftHand = this._ui.transform.Find("BG/Controls/IK Bones Buttons/Left Hand Button").GetComponent<Button>();
            leftHand.onClick.AddListener(() => this.SetBoneTarget(FullBodyBipedEffector.LeftHand));
            t = leftHand.GetComponentInChildren<Text>();
            this._effectorsButtons[(int)FullBodyBipedEffector.LeftHand] = leftHand;
            this._effectorsTexts[(int)FullBodyBipedEffector.LeftHand] = t;

            Button body = this._ui.transform.Find("BG/Controls/IK Bones Buttons/Body Button").GetComponent<Button>();
            body.onClick.AddListener(() => this.SetBoneTarget(FullBodyBipedEffector.Body));
            t = body.GetComponentInChildren<Text>();
            this._effectorsButtons[(int)FullBodyBipedEffector.Body] = body;
            this._effectorsTexts[(int)FullBodyBipedEffector.Body] = t;

            Button rightThigh = this._ui.transform.Find("BG/Controls/IK Bones Buttons/Right Thigh Button").GetComponent<Button>();
            rightThigh.onClick.AddListener(() => this.SetBoneTarget(FullBodyBipedEffector.RightThigh));
            t = rightThigh.GetComponentInChildren<Text>();
            this._effectorsButtons[(int)FullBodyBipedEffector.RightThigh] = rightThigh;
            this._effectorsTexts[(int)FullBodyBipedEffector.RightThigh] = t;

            Button leftThigh = this._ui.transform.Find("BG/Controls/IK Bones Buttons/Left Thigh Button").GetComponent<Button>();
            leftThigh.onClick.AddListener(() => this.SetBoneTarget(FullBodyBipedEffector.LeftThigh));
            t = leftThigh.GetComponentInChildren<Text>();
            this._effectorsButtons[(int)FullBodyBipedEffector.LeftThigh] = leftThigh;
            this._effectorsTexts[(int)FullBodyBipedEffector.LeftThigh] = t;

            Button rightLegBendGoal = this._ui.transform.Find("BG/Controls/IK Bones Buttons/Right Leg Bend Goal Button").GetComponent<Button>();
            rightLegBendGoal.onClick.AddListener(() => this.SetBendGoalTarget(FullBodyBipedChain.RightLeg));
            t = rightLegBendGoal.GetComponentInChildren<Text>();
            this._bendGoalsButtons[(int)FullBodyBipedChain.RightLeg] = rightLegBendGoal;
            this._bendGoalsTexts[(int)FullBodyBipedChain.RightLeg] = t;

            Button leftLegBendGoal = this._ui.transform.Find("BG/Controls/IK Bones Buttons/Left Leg Bend Goal Button").GetComponent<Button>();
            leftLegBendGoal.onClick.AddListener(() => this.SetBendGoalTarget(FullBodyBipedChain.LeftLeg));
            t = leftLegBendGoal.GetComponentInChildren<Text>();
            this._bendGoalsButtons[(int)FullBodyBipedChain.LeftLeg] = leftLegBendGoal;
            this._bendGoalsTexts[(int)FullBodyBipedChain.LeftLeg] = t;

            Button rightFoot = this._ui.transform.Find("BG/Controls/IK Bones Buttons/Right Foot Button").GetComponent<Button>();
            rightFoot.onClick.AddListener(() => this.SetBoneTarget(FullBodyBipedEffector.RightFoot));
            t = rightFoot.GetComponentInChildren<Text>();
            this._effectorsButtons[(int)FullBodyBipedEffector.RightFoot] = rightFoot;
            this._effectorsTexts[(int)FullBodyBipedEffector.RightFoot] = t;

            Button leftFoot = this._ui.transform.Find("BG/Controls/IK Bones Buttons/Left Foot Button").GetComponent<Button>();
            leftFoot.onClick.AddListener(() => this.SetBoneTarget(FullBodyBipedEffector.LeftFoot));
            t = leftFoot.GetComponentInChildren<Text>();
            this._effectorsButtons[(int)FullBodyBipedEffector.LeftFoot] = leftFoot;
            this._effectorsTexts[(int)FullBodyBipedEffector.LeftFoot] = t;

            this._fkScrollRect = this._fkBonesButtons.GetComponentInChildren<ScrollRect>();
            this._fkScrollRect.movementType = ScrollRect.MovementType.Clamped;
            this._fkToggleGroup = this._fkBonesButtons.GetComponentInChildren<ToggleGroup>();

            Button xMoveButton = this._ui.transform.Find("BG/Controls/Buttons/MoveRotateButtons/X Move Button").GetComponent<Button>();
            xMoveButton.onClick.AddListener(() => EventSystem.current.SetSelectedGameObject(null));
            xMoveButton.gameObject.AddComponent<PointerDownHandler>().onPointerDown += (eventData) =>
            {
                if (eventData.button == PointerEventData.InputButton.Left)
                {
                    this._xMove = true;
                    this.SetNoControlCondition();
                }
            };
            this._positionButtons[0] = xMoveButton;

            Button yMoveButton = this._ui.transform.Find("BG/Controls/Buttons/MoveRotateButtons/Y Move Button").GetComponent<Button>();
            yMoveButton.onClick.AddListener(() => EventSystem.current.SetSelectedGameObject(null));
            yMoveButton.gameObject.AddComponent<PointerDownHandler>().onPointerDown += (eventData) =>
            {
                if (eventData.button == PointerEventData.InputButton.Left)
                {
                    this._yMove = true;
                    this.SetNoControlCondition();
                }
            };
            this._positionButtons[1] = yMoveButton;

            Button zMoveButton = this._ui.transform.Find("BG/Controls/Buttons/MoveRotateButtons/Z Move Button").GetComponent<Button>();
            zMoveButton.onClick.AddListener(() => EventSystem.current.SetSelectedGameObject(null));
            zMoveButton.gameObject.AddComponent<PointerDownHandler>().onPointerDown += (eventData) =>
            {
                if (eventData.button == PointerEventData.InputButton.Left)
                {
                    this._zMove = true;
                    this.SetNoControlCondition();
                }
            };
            this._positionButtons[2] = zMoveButton;

            Button rotXButton = this._ui.transform.Find("BG/Controls/Buttons/MoveRotateButtons/Rot X Button").GetComponent<Button>();
            rotXButton.onClick.AddListener(() => EventSystem.current.SetSelectedGameObject(null));
            rotXButton.gameObject.AddComponent<PointerDownHandler>().onPointerDown += (eventData) =>
            {
                if (eventData.button == PointerEventData.InputButton.Left)
                {
                    this._xRot = true;
                    this.SetNoControlCondition();
                }
            };
            this._rotationButtons[0] = rotXButton;

            Button rotYButton = this._ui.transform.Find("BG/Controls/Buttons/MoveRotateButtons/Rot Y Button").GetComponent<Button>();
            rotYButton.onClick.AddListener(() => EventSystem.current.SetSelectedGameObject(null));
            rotYButton.gameObject.AddComponent<PointerDownHandler>().onPointerDown += (eventData) =>
            {
                if (eventData.button == PointerEventData.InputButton.Left)
                {
                    this._yRot = true;
                    this.SetNoControlCondition();
                }
            };
            this._rotationButtons[1] = rotYButton;

            Button rotZButton = this._ui.transform.Find("BG/Controls/Buttons/MoveRotateButtons/Rot Z Button").GetComponent<Button>();
            rotZButton.onClick.AddListener(() => EventSystem.current.SetSelectedGameObject(null));
            rotZButton.gameObject.AddComponent<PointerDownHandler>().onPointerDown += (eventData) =>
            {
                if (eventData.button == PointerEventData.InputButton.Left)
                {
                    this._zRot = true;
                    this.SetNoControlCondition();
                }
            };
            this._rotationButtons[2] = rotZButton;

            this._copyLeftArmButton = this._ui.transform.Find("BG/Controls/Other buttons/Copy Limbs/Copy Right Arm Button").GetComponent<Button>();
            this._copyLeftArmButton.onClick.AddListener(() =>
            {
                if (this._poseTarget != null)
                    ((CharaPoseController)this._poseTarget).CopyLimbToTwin(FullBodyBipedChain.RightArm, OIBoneInfo.BoneGroup.RightArm);
            });

            this._copyRightArmButton = this._ui.transform.Find("BG/Controls/Other buttons/Copy Limbs/Copy Left Arm Button").GetComponent<Button>();
            this._copyRightArmButton.onClick.AddListener(() =>
            {
                if (this._poseTarget != null)
                    ((CharaPoseController)this._poseTarget).CopyLimbToTwin(FullBodyBipedChain.LeftArm, OIBoneInfo.BoneGroup.LeftArm);
            });

            this._copyLeftLegButton = this._ui.transform.Find("BG/Controls/Other buttons/Copy Limbs/Copy Right Leg Button").GetComponent<Button>();
            this._copyLeftLegButton.onClick.AddListener(() =>
            {
                if (this._poseTarget != null)
                    ((CharaPoseController)this._poseTarget).CopyLimbToTwin(FullBodyBipedChain.RightLeg, OIBoneInfo.BoneGroup.RightLeg);
            });

            this._copyRightLegButton = this._ui.transform.Find("BG/Controls/Other buttons/Copy Limbs/Copy Left LegButton").GetComponent<Button>();
            this._copyRightLegButton.onClick.AddListener(() =>
            {
                if (this._poseTarget != null)
                    ((CharaPoseController)this._poseTarget).CopyLimbToTwin(FullBodyBipedChain.LeftLeg, OIBoneInfo.BoneGroup.LeftLeg);
            });

            this._swapPostButton = this._ui.transform.Find("BG/Controls/Other buttons/Other/Swap Pose Button").GetComponent<Button>();
            this._swapPostButton.onClick.AddListener(() =>
            {
                if (this._poseTarget != null)
                    ((CharaPoseController)this._poseTarget).SwapPose();
            });

            Button advancedModeButton = this._ui.transform.Find("BG/Controls/Buttons/Simple Options/Advanced Mode Button").GetComponent<Button>();
            advancedModeButton.onClick.AddListener(() =>
            {
                if (this._poseTarget != null)
                    this._poseTarget.ToggleAdvancedMode();
            });

            this._movementIntensity = this._ui.transform.Find("BG/Controls/Buttons/Simple Options/Intensity Container/Movement Intensity Slider").GetComponent<Slider>();
            this._movementIntensity.onValueChanged.AddListener(value =>
            {
                value = this._movementIntensity.value;
                value -= 7;
                this._intensityValue = Mathf.Pow(2, value);
                this._intensityValueText.text = this._intensityValue >= 1f ? "x" + this._intensityValue.ToString("0.##") : "/" + (1f / this._intensityValue).ToString("0.##");
            });
            this._movementIntensity.gameObject.AddComponent<PointerDownHandler>().onPointerDown += (eventData) =>
            {
                this.SetNoControlCondition();
                this._blockCamera = true;
            };

            this._intensityValueText = this._ui.transform.Find("BG/Controls/Buttons/Simple Options/Intensity Container/Movement Intensity Value").GetComponent<Text>();

            Button positionOp = this._ui.transform.Find("BG/Controls/Buttons/Simple Options/Pos Operation Container/Position Operation").GetComponent<Button>();
            Text buttonText = positionOp.GetComponentInChildren<Text>();
            positionOp.onClick.AddListener(() =>
            {
                this._positionOperationWorld = !this._positionOperationWorld;
                buttonText.text = this._positionOperationWorld ? "World" : "Local";
            });

            this._optimizeIKToggle = this._ui.transform.Find("BG/Controls/Buttons/Simple Options/Optimize IK Container/Optimize IK").GetComponent<Toggle>();
            this._optimizeIKToggle.onValueChanged.AddListener((b) =>
            {
                if (this._poseTarget != null)
                    ((CharaPoseController)this._poseTarget).optimizeIK = this._optimizeIKToggle.isOn;
            });

            Button optionsButton = this._ui.transform.Find("BG/Controls/Buttons/Simple Options/Options Button").GetComponent<Button>();
            optionsButton.onClick.AddListener(() =>
            {
                if (this._shortcutRegisterMode)
                    this._shortcutKeyButton.onClick.Invoke();
                this._optionsWindow.gameObject.SetActive(!this._optionsWindow.gameObject.activeSelf);
            });
            this._ui.gameObject.SetActive(false);

            this.SetBoneTarget(FullBodyBipedEffector.Body);
            //this.OnTargetChange(null);

            this._optionsWindow = (RectTransform)this._ui.transform.Find("Options Window");

            topContainer = this._optionsWindow.Find("Top Container");
            mw = UIUtility.MakeObjectDraggable(topContainer as RectTransform, this._optionsWindow, false);
            mw.onPointerDown += this.OnWindowStartDrag;
            mw.onDrag += this.OnWindowDrag;
            mw.onPointerUp += this.OnWindowEndDrag;

            Vector2 sizeDelta = bg.sizeDelta;
#if HONEYSELECT
            Text xMoveText = xMoveButton.GetComponentInChildren<Text>();
            Text yMoveText = yMoveButton.GetComponentInChildren<Text>();
            Text zMoveText = zMoveButton.GetComponentInChildren<Text>();
            Text xRotText = rotXButton.GetComponentInChildren<Text>();
            Text yRotText = rotYButton.GetComponentInChildren<Text>();
            Text zRotText = rotZButton.GetComponentInChildren<Text>();
            int moveFontSize = xMoveText.fontSize;
            int rotFontSize = xRotText.fontSize;
#endif

            Button normalButton = this._ui.transform.Find("Options Window/Options/Main Window Size Container/Normal Button").GetComponent<Button>();
            normalButton.onClick.AddListener(() =>
            {
                this._mainWindowSize = 1f;
                bg.sizeDelta = sizeDelta * this._mainWindowSize;
#if HONEYSELECT
                xMoveText.fontSize = (int)(moveFontSize * this._mainWindowSize);
                yMoveText.fontSize = (int)(moveFontSize * this._mainWindowSize);
                zMoveText.fontSize = (int)(moveFontSize * this._mainWindowSize);
                xRotText.fontSize = (int)(rotFontSize * this._mainWindowSize);
                yRotText.fontSize = (int)(rotFontSize * this._mainWindowSize);
                zRotText.fontSize = (int)(rotFontSize * this._mainWindowSize);
#endif
            });

            Button largeButton = this._ui.transform.Find("Options Window/Options/Main Window Size Container/Large Button").GetComponent<Button>();
            largeButton.onClick.AddListener(() =>
            {
                this._mainWindowSize = 1.25f;
                bg.sizeDelta = sizeDelta * this._mainWindowSize;
#if HONEYSELECT
                xMoveText.fontSize = (int)(moveFontSize * this._mainWindowSize);
                yMoveText.fontSize = (int)(moveFontSize * this._mainWindowSize);
                zMoveText.fontSize = (int)(moveFontSize * this._mainWindowSize);
                xRotText.fontSize = (int)(rotFontSize * this._mainWindowSize);
                yRotText.fontSize = (int)(rotFontSize * this._mainWindowSize);
                zRotText.fontSize = (int)(rotFontSize * this._mainWindowSize);
#endif
            });

            Button veryLargeButton = this._ui.transform.Find("Options Window/Options/Main Window Size Container/Very Large Button").GetComponent<Button>();
            veryLargeButton.onClick.AddListener(() =>
            {
                this._mainWindowSize = 1.5f;
                bg.sizeDelta = sizeDelta * this._mainWindowSize;
#if HONEYSELECT
                xMoveText.fontSize = (int)(moveFontSize * this._mainWindowSize);
                yMoveText.fontSize = (int)(moveFontSize * this._mainWindowSize);
                zMoveText.fontSize = (int)(moveFontSize * this._mainWindowSize);
                xRotText.fontSize = (int)(rotFontSize * this._mainWindowSize);
                yRotText.fontSize = (int)(rotFontSize * this._mainWindowSize);
                zRotText.fontSize = (int)(rotFontSize * this._mainWindowSize);
#endif
            });
            bg.sizeDelta = sizeDelta * this._mainWindowSize;
#if HONEYSELECT
            xMoveText.fontSize = (int)(moveFontSize * this._mainWindowSize);
            yMoveText.fontSize = (int)(moveFontSize * this._mainWindowSize);
            zMoveText.fontSize = (int)(moveFontSize * this._mainWindowSize);
            xRotText.fontSize = (int)(rotFontSize * this._mainWindowSize);
            yRotText.fontSize = (int)(rotFontSize * this._mainWindowSize);
            zRotText.fontSize = (int)(rotFontSize * this._mainWindowSize);
#endif
            this._optionsWindow.anchoredPosition += new Vector2((sizeDelta.x * this._mainWindowSize) - sizeDelta.x, 0f);

            Slider xSlider = this._ui.transform.Find("Options Window/Options/Advanced Mode Window Size Container/Width Slider").GetComponent<Slider>();
            xSlider.onValueChanged.AddListener((f) =>
            {
                this._advancedModeRect.xMin = this._advancedModeRect.xMax - xSlider.value;
            });
            xSlider.maxValue = Screen.width * 0.8f;

            Slider ySlider = this._ui.transform.Find("Options Window/Options/Advanced Mode Window Size Container/Height Slider").GetComponent<Slider>();
            ySlider.onValueChanged.AddListener((f) =>
            {
                this._advancedModeRect.yMin = this._advancedModeRect.yMax - ySlider.value;
            });
            ySlider.maxValue = Screen.height * 0.8f;

            this._shortcutKeyButton = this._ui.transform.Find("Options Window/Options/Shortcut Key Container/Listener Button").GetComponent<Button>();
            Text text = this._shortcutKeyButton.GetComponentInChildren<Text>();
            this._shortcutKeyButton.onClick.AddListener(() =>
            {
                this._shortcutRegisterMode = !this._shortcutRegisterMode;
                text.text = this._shortcutRegisterMode ? "Press a Key" : this._mainWindowKeyCode.ToString();
            });

            this._crotchCorrectionByDefaultToggle = this._ui.transform.Find("Options Window/Options/Joint Correction Container/Crotch/Toggle").GetComponent<Toggle>();

            this._anklesCorrectionByDefaultToggle = this._ui.transform.Find("Options Window/Options/Joint Correction Container/Ankles/Toggle").GetComponent<Toggle>();

            this._optionsWindow.gameObject.SetActive(false);
            LayoutRebuilder.ForceRebuildLayoutImmediate(this._ui.transform.GetChild(0).transform as RectTransform);

            // Additional UI
#if HONEYSELECT
            {
                RectTransform parent = GameObject.Find("StudioScene").transform.Find("Canvas Main Menu/02_Manipulate/00_Chara/06_Joint") as RectTransform;
                RawImage container = UIUtility.CreateRawImage("Additional Container", parent);
                container.rectTransform.SetRect(Vector2.zero, new Vector2(1f, 0f), new Vector2(0f, -60f), Vector2.zero);
                container.color = new Color32(105, 108, 111, 255);

                GameObject textPrefab = parent.Find("Text Left Leg").gameObject;
                Text crotchText = Instantiate(textPrefab).GetComponent<Text>();
                crotchText.rectTransform.SetParent(parent);
                crotchText.rectTransform.localPosition = Vector3.zero;
                crotchText.rectTransform.SetRect(textPrefab.transform);
                crotchText.rectTransform.localScale = textPrefab.transform.localScale;
                crotchText.rectTransform.SetParent(container.rectTransform);
                crotchText.rectTransform.offsetMin += new Vector2(0f, -20f);
                crotchText.rectTransform.offsetMax += new Vector2(0f, -20f);
                crotchText.text = " Crotch";
                GameObject togglePrefab = parent.Find("Toggle Left Leg").gameObject;
                this._crotchCorrectionToggle = Instantiate(togglePrefab).GetComponent<Toggle>();
                RectTransform rt = this._crotchCorrectionToggle.transform as RectTransform;
                rt.SetParent(parent);
                rt.localPosition = Vector3.zero;
                rt.SetRect(togglePrefab.transform);
                rt.localScale = togglePrefab.transform.localScale;
                rt.SetParent(container.rectTransform);
                rt.offsetMin += new Vector2(0f, -20f);
                rt.offsetMax += new Vector2(0f, -20f);
                this._crotchCorrectionToggle.onValueChanged = new Toggle.ToggleEvent();
                this._crotchCorrectionToggle.onValueChanged.AddListener((b) =>
                {
                    if (this._poseTarget != null)
                        ((CharaPoseController)this._poseTarget).crotchJointCorrection = this._crotchCorrectionToggle.isOn;
                });

                Text leftFootText = Instantiate(textPrefab).GetComponent<Text>();
                leftFootText.rectTransform.SetParent(parent);
                leftFootText.rectTransform.localPosition = Vector3.zero;
                leftFootText.rectTransform.SetRect(textPrefab.transform);
                leftFootText.rectTransform.localScale = textPrefab.transform.localScale;
                leftFootText.rectTransform.SetParent(container.rectTransform);
                leftFootText.rectTransform.offsetMin += new Vector2(0f, -40f);
                leftFootText.rectTransform.offsetMax += new Vector2(0f, -40f);
                leftFootText.text = " Left Ankle";
                this._leftFootCorrectionToggle = Instantiate(togglePrefab).GetComponent<Toggle>();
                rt = this._leftFootCorrectionToggle.transform as RectTransform;
                rt.SetParent(parent);
                rt.localPosition = Vector3.zero;
                rt.SetRect(togglePrefab.transform);
                rt.localScale = togglePrefab.transform.localScale;
                rt.SetParent(container.rectTransform);
                rt.offsetMin += new Vector2(0f, -40f);
                rt.offsetMax += new Vector2(0f, -40f);
                this._leftFootCorrectionToggle.onValueChanged = new Toggle.ToggleEvent();
                this._leftFootCorrectionToggle.onValueChanged.AddListener((b) =>
                {
                    if (this._poseTarget != null)
                        ((CharaPoseController)this._poseTarget).leftFootJointCorrection = this._leftFootCorrectionToggle.isOn;
                });

                Text rightFootText = Instantiate(textPrefab).GetComponent<Text>();
                rightFootText.rectTransform.SetParent(parent);
                rightFootText.rectTransform.localPosition = Vector3.zero;
                rightFootText.rectTransform.SetRect(textPrefab.transform);
                rightFootText.rectTransform.localScale = textPrefab.transform.localScale;
                rightFootText.rectTransform.SetParent(container.rectTransform);
                rightFootText.rectTransform.offsetMin += new Vector2(0f, -60f);
                rightFootText.rectTransform.offsetMax += new Vector2(0f, -60f);
                rightFootText.text = " Right Ankle";
                this._rightFootCorrectionToggle = Instantiate(togglePrefab).GetComponent<Toggle>();
                rt = this._rightFootCorrectionToggle.transform as RectTransform;
                rt.SetParent(parent);
                rt.localPosition = Vector3.zero;
                rt.SetRect(togglePrefab.transform);
                rt.localScale = togglePrefab.transform.localScale;
                rt.SetParent(container.rectTransform);
                rt.offsetMin += new Vector2(0f, -60f);
                rt.offsetMax += new Vector2(0f, -60f);
                this._rightFootCorrectionToggle.onValueChanged = new Toggle.ToggleEvent();
                this._rightFootCorrectionToggle.onValueChanged.AddListener((b) =>
                {
                    if (this._poseTarget != null)
                        ((CharaPoseController)this._poseTarget).rightFootJointCorrection = this._rightFootCorrectionToggle.isOn;
                });
            }
#elif KOIKATSU
            {
                RectTransform parent = GameObject.Find("StudioScene").transform.Find("Canvas Main Menu/02_Manipulate/00_Chara/06_Joint") as RectTransform;
                RawImage container = UIUtility.CreateRawImage("Additional Container", parent);
                container.rectTransform.SetRect(Vector2.zero, Vector2.zero, new Vector2(0f, -60f), new Vector2(parent.rect.width, 0f));
                container.color = new Color32(110, 110, 116, 223);

                GameObject prefab = parent.Find("Left Leg (1)").gameObject;
                RectTransform crotchContainer = Instantiate(prefab).transform as RectTransform;
                crotchContainer.SetParent(container.rectTransform);
                crotchContainer.pivot = new Vector2(0f, 1f);
                crotchContainer.localPosition = Vector3.zero;
                crotchContainer.localScale = prefab.transform.localScale;
                crotchContainer.anchoredPosition = new Vector2(0f, 0f);
                crotchContainer.GetComponentInChildren<TextMeshProUGUI>().text = "Crotch";

                this._crotchCorrectionToggle = crotchContainer.GetComponentInChildren<Toggle>();
                this._crotchCorrectionToggle.onValueChanged = new Toggle.ToggleEvent();
                this._crotchCorrectionToggle.onValueChanged.AddListener((b) =>
                {
                    if (this._poseTarget != null)
                        ((CharaPoseController)this._poseTarget).crotchJointCorrection = this._crotchCorrectionToggle.isOn;
                });

                RectTransform leftFootContainer = Instantiate(prefab).transform as RectTransform;
                leftFootContainer.SetParent(container.rectTransform);
                leftFootContainer.pivot = new Vector2(0f, 1f);
                leftFootContainer.localPosition = Vector3.zero;
                leftFootContainer.localScale = prefab.transform.localScale;
                leftFootContainer.anchoredPosition = new Vector2(0f, -20f);
                leftFootContainer.GetComponentInChildren<TextMeshProUGUI>().text = "Left Ankle";
                this._leftFootCorrectionToggle = leftFootContainer.GetComponentInChildren<Toggle>();
                this._leftFootCorrectionToggle.onValueChanged = new Toggle.ToggleEvent();
                this._leftFootCorrectionToggle.onValueChanged.AddListener((b) =>
                {
                    if (this._poseTarget != null)
                        ((CharaPoseController)this._poseTarget).leftFootJointCorrection = this._leftFootCorrectionToggle.isOn;
                });

                RectTransform rightFootContainer = Instantiate(prefab).transform as RectTransform;

                rightFootContainer.SetParent(container.rectTransform);
                rightFootContainer.pivot = new Vector2(0f, 1f);
                rightFootContainer.localPosition = Vector3.zero;
                rightFootContainer.localScale = prefab.transform.localScale;
                rightFootContainer.anchoredPosition = new Vector2(0f, -40f);

                rightFootContainer.GetComponentInChildren<TextMeshProUGUI>().text = "Right Ankle";
                this._rightFootCorrectionToggle = rightFootContainer.GetComponentInChildren<Toggle>();
                this._rightFootCorrectionToggle.onValueChanged = new Toggle.ToggleEvent();
                this._rightFootCorrectionToggle.onValueChanged.AddListener((b) =>
                {
                    if (this._poseTarget != null)
                        ((CharaPoseController)this._poseTarget).rightFootJointCorrection = this._rightFootCorrectionToggle.isOn;
                });
            }
#endif
        }

        private void OnWindowStartDrag(PointerEventData data)
        {
            this.SetNoControlCondition();
            this._windowMoving = true;
        }

        private void OnWindowDrag(PointerEventData data)
        {
            this._windowMoving = true;
        }

        private void OnWindowEndDrag(PointerEventData data)
        {
            this._windowMoving = false;
        }

        private void OnWindowResize()
        {
            if (this._advancedModeRect.xMax > Screen.width)
                this._advancedModeRect.x -= this._advancedModeRect.xMax - Screen.width;
            if (this._advancedModeRect.yMax > Screen.height)
                this._advancedModeRect.y -= this._advancedModeRect.yMax - Screen.height;
        }


        private void SetBoneTarget(FullBodyBipedEffector bone)
        {
            this.ResetBoneButtons();
            if (!Input.GetKey(KeyCode.LeftControl) && !Input.GetKey(KeyCode.RightControl))
            {
                this._ikBoneTargets.Clear();
                this._ikBendGoalTargets.Clear();
                this._ikBoneTargets.Add(bone);
            }
            else
            {
                if (this._ikBoneTargets.Contains(bone))
                    this._ikBoneTargets.Remove(bone);
                else
                    this._ikBoneTargets.Add(bone);
            }
            this.SelectBoneButtons();
            EventSystem.current.SetSelectedGameObject(null);
        }

        private void SetBendGoalTarget(FullBodyBipedChain bendGoal)
        {
            this.ResetBoneButtons();
            if (!Input.GetKey(KeyCode.LeftControl) && !Input.GetKey(KeyCode.RightControl))
            {
                this._ikBoneTargets.Clear();
                this._ikBendGoalTargets.Clear();
                this._ikBendGoalTargets.Add(bendGoal);
            }
            else
            {
                if (this._ikBendGoalTargets.Contains(bendGoal))
                    this._ikBendGoalTargets.Remove(bendGoal);
                else
                    this._ikBendGoalTargets.Add(bendGoal);
            }
            this.SelectBoneButtons();
            EventSystem.current.SetSelectedGameObject(null);
        }

        private void ResetBoneButtons()
        {
            foreach (FullBodyBipedChain bendGoal in this._ikBendGoalTargets)
            {
                Button b = this._bendGoalsButtons[(int)bendGoal];
                ColorBlock cb = b.colors;
                cb.normalColor = Color.Lerp(Color.blue, Color.white, 0.5f);
                cb.highlightedColor = cb.normalColor;
                b.colors = cb;
                this._bendGoalsTexts[(int)bendGoal].fontStyle = FontStyle.Normal;
            }
            foreach (FullBodyBipedEffector effector in this._ikBoneTargets)
            {
                Button b = this._effectorsButtons[(int)effector];
                ColorBlock cb = b.colors;
                cb.normalColor = Color.Lerp(Color.red, Color.white, 0.5f);
                cb.highlightedColor = cb.normalColor;
                b.colors = cb;
                this._effectorsTexts[(int)effector].fontStyle = FontStyle.Normal;
            }
        }

        private void SelectBoneButtons()
        {
            foreach (FullBodyBipedChain bendGoal in this._ikBendGoalTargets)
            {
                Button b = this._bendGoalsButtons[(int)bendGoal];
                ColorBlock cb = b.colors;
                cb.normalColor = UIUtility.lightGreenColor;
                cb.highlightedColor = cb.normalColor;
                b.colors = cb;
                this._bendGoalsTexts[(int)bendGoal].fontStyle = FontStyle.Bold;
            }
            foreach (FullBodyBipedEffector effector in this._ikBoneTargets)
            {
                Button b = this._effectorsButtons[(int)effector];
                ColorBlock cb = b.colors;
                cb.normalColor = UIUtility.lightGreenColor;
                cb.highlightedColor = cb.normalColor;
                b.colors = cb;
                this._effectorsTexts[(int)effector].fontStyle = FontStyle.Bold;
            }
        }

        private void GUILogic()
        {
            if (this._shortcutRegisterMode)
            {
                foreach (KeyCode kc in this._possibleKeyCodes)
                {
                    if (Input.GetKeyDown(kc))
                    {
                        if (kc != KeyCode.Escape && kc != KeyCode.Return && kc != KeyCode.Mouse0 && kc != KeyCode.Mouse1 && kc != KeyCode.Mouse2 && kc != KeyCode.Mouse3 && kc != KeyCode.Mouse4 && kc != KeyCode.Mouse5 && kc != KeyCode.Mouse6)
                            this._mainWindowKeyCode = kc;
                        this._shortcutKeyButton.onClick.Invoke();
                        break;
                    }
                }
            }
            else if (Input.GetKeyDown(this._mainWindowKeyCode))
            {
                this._isVisible = !this._isVisible;
                this._ui.gameObject.SetActive(this._isVisible);
                this._hspeButtonImage.color = this._isVisible ? Color.green : Color.white;
            }

            if (Input.GetMouseButtonDown(0))
            {
                if (this._poseTarget != null && PoseController._drawAdvancedMode && this._mouseInAdvMode)
                    this.SetNoControlCondition();
            }

            if (Input.GetMouseButtonUp(0))
            {
                this._xMove = false;
                this._yMove = false;
                this._zMove = false;
                this._xRot = false;
                this._yRot = false;
                this._zRot = false;
                this._blockCamera = false;
            }

            if (this._isVisible == false)
                return;

            if (this._poseTarget == null)
                return;
            CharaPoseController charaPoseTarget = this._poseTarget as CharaPoseController;
            bool isCharacter = charaPoseTarget != null;
            if (this._xMove || this._yMove || this._zMove || this._xRot || this._yRot || this._zRot)
            {
                this._delta += (new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")) * (Input.GetKey(KeyCode.LeftShift) ? 4f : 1f) / (Input.GetKey(KeyCode.LeftControl) ? 6f : 1f)) / 10f;

                if (this._poseTarget._currentDragType == PoseController.DragType.None)
                    this._poseTarget.StartDrag(this._xMove || this._yMove || this._zMove ? PoseController.DragType.Position : PoseController.DragType.Rotation);
                if (this._currentModeIK)
                {
                    if (isCharacter)
                    {
                        for (int i = 0; i < this._ikBoneTargets.Count; ++i)
                        {
                            bool changePosition = false;
                            bool changeRotation = false;
                            Vector3 newPosition = this._lastIKBonesPositions[i];
                            Quaternion newRotation = this._lastIKBonesRotations[i];
                            if (this._xMove)
                            {
                                newPosition.x += this._delta.y * this._intensityValue;
                                changePosition = true;
                            }
                            if (this._yMove)
                            {
                                newPosition.y += this._delta.y * this._intensityValue;
                                changePosition = true;
                            }
                            if (this._zMove)
                            {
                                newPosition.z += this._delta.y * this._intensityValue;
                                changePosition = true;
                            }
                            if (this._xRot)
                            {
                                newRotation *= Quaternion.AngleAxis(this._delta.x * 20f * this._intensityValue, Vector3.right);
                                changeRotation = true;
                            }
                            if (this._yRot)
                            {
                                newRotation *= Quaternion.AngleAxis(this._delta.x * 20f * this._intensityValue, Vector3.up);
                                changeRotation = true;
                            }
                            if (this._zRot)
                            {
                                newRotation *= Quaternion.AngleAxis(this._delta.x * 20f * this._intensityValue, Vector3.forward);
                                changeRotation = true;
                            }
                            FullBodyBipedEffector bone = this._ikBoneTargets[i];
                            if (changePosition && charaPoseTarget.IsPartEnabled(bone))
                                charaPoseTarget.SetBoneTargetPosition(bone, newPosition, this._positionOperationWorld);
                            if (changeRotation && charaPoseTarget.IsPartEnabled(bone))
                                charaPoseTarget.SetBoneTargetRotation(this._ikBoneTargets[i], newRotation);
                        }
                        for (int i = 0; i < this._ikBendGoalTargets.Count; ++i)
                        {
                            Vector3 newPosition = this._lastIKBendGoalsPositions[i];
                            if (this._xMove)
                                newPosition.x += this._delta.y * this._intensityValue;
                            if (this._yMove)
                                newPosition.y += this._delta.y * this._intensityValue;
                            if (this._zMove)
                                newPosition.z += this._delta.y * this._intensityValue;
                            FullBodyBipedChain bendGoal = this._ikBendGoalTargets[i];
                            if (charaPoseTarget.IsPartEnabled(bendGoal))
                                charaPoseTarget.SetBendGoalPosition(bendGoal, newPosition, this._positionOperationWorld);
                        }
                    }
                }
                else
                {
                    bool changeRotation = false;
                    Quaternion newRotation = this._lastFKBonesRotation;
                    if (this._xRot)
                    {
                        newRotation *= Quaternion.AngleAxis(this._delta.x * 20f * this._intensityValue, Vector3.right);
                        changeRotation = true;
                    }
                    if (this._yRot)
                    {
                        newRotation *= Quaternion.AngleAxis(this._delta.x * 20f * this._intensityValue, Vector3.up);
                        changeRotation = true;
                    }
                    if (this._zRot)
                    {
                        newRotation *= Quaternion.AngleAxis(this._delta.x * 20f * this._intensityValue, Vector3.forward);
                        changeRotation = true;
                    }
                    if (changeRotation)
                        this._poseTarget.SeFKBoneTargetRotation(GuideObjectManager.Instance.selectObject, newRotation);
                }
            }
            else
            {
                this._delta = Vector2.zero;
                if (this._poseTarget._currentDragType != PoseController.DragType.None)
                    this._poseTarget.StopDrag();
                if (this._currentModeIK)
                {
                    if (isCharacter)
                    {
                        for (int i = 0; i < this._ikBoneTargets.Count; ++i)
                        {
                            this._lastIKBonesPositions[i] = charaPoseTarget.GetBoneTargetPosition(this._ikBoneTargets[i], this._positionOperationWorld);
                            this._lastIKBonesRotations[i] = charaPoseTarget.GetBoneTargetRotation(this._ikBoneTargets[i]);
                        }
                        for (int i = 0; i < this._ikBendGoalTargets.Count; ++i)
                            this._lastIKBendGoalsPositions[i] = charaPoseTarget.GetBendGoalPosition(this._ikBendGoalTargets[i], this._positionOperationWorld);
                    }
                }
                else
                {
                    if (GuideObjectManager.Instance.selectObject != null)
                        this._lastFKBonesRotation = this._poseTarget.GetFKBoneTargetRotation(GuideObjectManager.Instance.selectObject);
                }
            }

            if (this._currentModeIK)
            {
                for (int i = 0; i < this._effectorsButtons.Length; i++)
                    this._effectorsButtons[i].interactable = isCharacter && charaPoseTarget.IsPartEnabled((FullBodyBipedEffector)i);

                for (int i = 0; i < this._bendGoalsButtons.Length; i++)
                    this._bendGoalsButtons[i].interactable = isCharacter && charaPoseTarget.IsPartEnabled((FullBodyBipedChain)i);

                for (int i = 0; i < this._positionButtons.Length; i++)
                    this._positionButtons[i].interactable = isCharacter && charaPoseTarget.target.ikEnabled;
            }

            bool interactableRotation = false;
            if (this._currentModeIK)
            {
                if (isCharacter)
                    interactableRotation = charaPoseTarget.target.ikEnabled && this._ikBendGoalTargets.Count == 0 && this._ikBoneTargets.Intersect(CharaPoseController.nonRotatableEffectors).Any() == false;
            }
            else
            {
                OCIChar.BoneInfo bone;
                interactableRotation = this._poseTarget.target.fkEnabled && GuideObjectManager.Instance.selectObject != null && this._poseTarget.target.fkObjects.TryGetValue(GuideObjectManager.Instance.selectObject.transformTarget.gameObject, out bone) && bone.active;
            }

            for (int i = 0; i < this._rotationButtons.Length; i++)
                this._rotationButtons[i].interactable = interactableRotation;
        }
        #endregion

        #region Public Methods
        public void SetNoControlCondition()
        {
            Studio.Studio.Instance.cameraCtrl.noCtrlCondition = this.CameraControllerCondition;
        }

        public void OnDuplicate(ObjectCtrlInfo source, ObjectCtrlInfo destination)
        {
            PoseController destinationController;
            if (destination is OCIChar)
                destinationController = destination.guideObject.transformTarget.gameObject.AddComponent<CharaPoseController>();
            else
                destinationController = destination.guideObject.transformTarget.gameObject.AddComponent<PoseController>();
            destinationController.LoadFrom(source.guideObject.transformTarget.gameObject.GetComponent<PoseController>());
        }
        #endregion

        #region Private Methods
        private void OnObjectAdded()
        {
            foreach (KeyValuePair<int, ObjectCtrlInfo> kvp in Studio.Studio.Instance.dicObjectCtrl)
            {
                if (kvp.Key >= this._lastIndex)
                {
                    switch (kvp.Value.objectInfo.kind)
                    {
                        case 0:
                            if (kvp.Value.guideObject.transformTarget.GetComponent<PoseController>() == null)
                                kvp.Value.guideObject.transformTarget.gameObject.AddComponent<CharaPoseController>();
                            break;
                        case 1:
                            if (kvp.Value.guideObject.transformTarget.GetComponent<PoseController>() == null)
                            {
                                PoseController controller = kvp.Value.guideObject.transformTarget.gameObject.AddComponent<PoseController>();
                                if (controller._collidersEditor._isLoneCollider == false)
                                    this.ExecuteDelayed(() => { controller.enabled = false; }, 2);
                            }
                            break;
                    }
                }
            }
        }

        private void OnTargetChange(PoseController last)
        {
            if (this._poseTarget != null)
            {
                bool isCharacter = this._poseTarget.target.type == GenericOCITarget.Type.Character;
                if (isCharacter)
                {
                    CharaPoseController poseTarget = (CharaPoseController)this._poseTarget;

                    this._optimizeIKToggle.isOn = poseTarget.optimizeIK;
                    this._crotchCorrectionToggle.isOn = poseTarget.crotchJointCorrection;
                    this._leftFootCorrectionToggle.isOn = poseTarget.leftFootJointCorrection;
                    this._rightFootCorrectionToggle.isOn = poseTarget.rightFootJointCorrection;
                }
                this._optimizeIKToggle.interactable = isCharacter;
                this._copyLeftArmButton.interactable = isCharacter;
                this._copyRightArmButton.interactable = isCharacter;
                this._copyLeftLegButton.interactable = isCharacter;
                this._copyRightLegButton.interactable = isCharacter;
                this._swapPostButton.interactable = isCharacter;
                this._nothingText.gameObject.SetActive(false);
                this._controls.gameObject.SetActive(true);
                this._poseTarget.target.RefreshFKBones();
                this.RefreshFKBonesList();
            }
            else
            {
                this._nothingText.gameObject.SetActive(true);
                this._controls.gameObject.SetActive(false);
            }
            PoseController.SelectionChanged(this._poseTarget);
        }

        private void OnCharacterReplaced(OCIChar chara)
        {
            if (this._poseTarget != null && this._poseTarget.target.oci == chara)
                this.ExecuteDelayed(this.RefreshFKBonesList);
        }

        private void OnLoadClothesFile(OCIChar chara)
        {
            if (this._poseTarget != null && this._poseTarget.target.oci == chara)
                this.ExecuteDelayed(this.RefreshFKBonesList);
        }

#if HONEYSELECT
        private void OnCoordinateReplaced(OCIChar chara, CharDefine.CoordinateType coord, bool force)
#elif KOIKATSU
        private void OnCoordinateReplaced(OCIChar chara, ChaFileDefine.CoordinateType type, bool force)
#endif
        {
            if (this._poseTarget != null && this._poseTarget.target.oci == chara)
                this.ExecuteDelayed(this.RefreshFKBonesList);
        }

        private void RefreshFKBonesList()
        {
            int i = 0;
            List<KeyValuePair<GameObject, OCIChar.BoneInfo>> list = this._poseTarget.target.fkObjects.ToList();

            for (; i < list.Count; ++i)
            {
                FKBoneEntry entry;
                KeyValuePair<GameObject, OCIChar.BoneInfo> pair = list[i];
                if (i < this._fkBoneEntries.Count)
                    entry = this._fkBoneEntries[i];
                else
                {
                    entry = new FKBoneEntry();
                    entry.toggle = GameObject.Instantiate(this._fkBoneTogglePrefab).GetComponent<Toggle>();
                    entry.text = entry.toggle.GetComponentInChildren<Text>();

                    entry.toggle.transform.SetParent(this._fkScrollRect.content);
                    entry.toggle.transform.localScale = Vector3.one;
                    entry.toggle.group = this._fkToggleGroup;
                    this._fkBoneEntries.Add(entry);
                }
                if (pair.Key == null)
                {
                    entry.toggle.gameObject.SetActive(false);
                    entry.target = null;
                    continue;
                }

                entry.text.text = pair.Key.name;

                entry.toggle.onValueChanged = new Toggle.ToggleEvent();
                entry.toggle.isOn = GuideObjectManager.Instance.selectObject.transformTarget == pair.Key.transform;
                entry.toggle.onValueChanged.AddListener((b) =>
                {
                    if (entry.toggle.isOn)
                    {
                        if (this._poseTarget.target.fkEnabled == false || pair.Value.active == false)
                        {
                            entry.toggle.isOn = false;
                            this.SelectCurrentFKBoneEntry();
                            return;
                        }
                        GuideObjectManager.Instance.selectObject = this._dicGuideObject[pair.Key.transform];
                    }
                });
                entry.target = pair.Key;
                entry.toggle.gameObject.SetActive(true);
            }
            for (; i < this._fkBoneEntries.Count; ++i)
            {
                FKBoneEntry entry = this._fkBoneEntries[i];

                entry.toggle.gameObject.SetActive(false);
                entry.target = null;
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate(this._fkScrollRect.content);
        }

        private FKBoneEntry SelectCurrentFKBoneEntry()
        {
            FKBoneEntry entry = null;
            if (GuideObjectManager.Instance.selectObject != null)
            {
                entry = this._fkBoneEntries.Find(e => e.target == GuideObjectManager.Instance.selectObject.transformTarget.gameObject);
                if (entry != null)
                    entry.toggle.isOn = true;                
            }
            return entry;
        }

        [HarmonyPatch(typeof(GuideSelect), "OnPointerClick", new []{ typeof(PointerEventData) })]
        private static class GuideSelect_OnPointerClick_Patches
        {
            private static void Postfix()
            {
                _self._fkToggleGroup.SetAllTogglesOff();
                FKBoneEntry entry = _self.SelectCurrentFKBoneEntry();
                if (entry != null)
                {
                    _self._fkScrollRect.content.anchoredPosition = new Vector2(_self._fkScrollRect.content.anchoredPosition.x, _self._fkScrollRect.transform.InverseTransformPoint(_self._fkScrollRect.content.position).y - _self._fkScrollRect.transform.InverseTransformPoint(entry.toggle.transform.position).y - 10);
                    _self._fkScrollRect.normalizedPosition = new Vector2(_self._fkScrollRect.normalizedPosition.x, Mathf.Clamp01(_self._fkScrollRect.normalizedPosition.y));
                }
            }
        }

        private bool CameraControllerCondition()
        {
            return this._blockCamera || this._xMove || this._yMove || this._zMove || this._xRot || this._yRot || this._zRot || this._mouseInAdvMode || this._windowMoving || (this._poseTarget != null && this._poseTarget.isDraggingDynamicBone);
        }
        #endregion

        #region Saves
#if HONEYSELECT
        private void OnSceneLoad(string scenePath, XmlNode node)
        {
            this._lastObjectCount = 0;
            scenePath = Path.GetFileNameWithoutExtension(scenePath) + ".sav";
            string dir = _pluginDir + _studioSavesDir;
            string path = dir + scenePath;
            if (File.Exists(path))
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(path);
                node = doc;
            }
            if (node != null)
                node = node.FirstChild;
            this.LoadDefaultVersion(node);
        }

        private void OnSceneImport(string scenePath, XmlNode node)
        {
            scenePath = Path.GetFileNameWithoutExtension(scenePath) + ".sav";
            string dir = _pluginDir + _studioSavesDir;
            string path = dir + scenePath;
            if (File.Exists(path))
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(path);
                node = doc;
            }
            if (node != null)
                node = node.FirstChild;
            int max = -1;
            foreach (KeyValuePair<int, ObjectCtrlInfo> pair in Studio.Studio.Instance.dicObjectCtrl)
            {
                if (pair.Key > max)
                    max = pair.Key;
            }
            this.LoadDefaultVersion(node, max);
        }

        private void OnSceneSave(string scenePath, XmlTextWriter xmlWriter)
        {
            xmlWriter.WriteStartElement("root");
            xmlWriter.WriteAttributeString("version", HSPE.versionNum);
            SortedDictionary<int, ObjectCtrlInfo> dic = new SortedDictionary<int, ObjectCtrlInfo>(Studio.Studio.Instance.dicObjectCtrl);
            foreach (KeyValuePair<int, ObjectCtrlInfo> kvp in dic)
            {
                if (kvp.Value is OCIChar)
                {
                    xmlWriter.WriteStartElement("characterInfo");
                    xmlWriter.WriteAttributeString("name", ((OCIChar)kvp.Value).charInfo.customInfo.name);
                    xmlWriter.WriteAttributeString("index", XmlConvert.ToString(kvp.Key));

                    this.SaveElement(kvp.Value, xmlWriter);

                    xmlWriter.WriteEndElement();
                }
                else if (kvp.Value is OCIItem)
                {
                    xmlWriter.WriteStartElement("itemInfo");
                    xmlWriter.WriteAttributeString("name", ((OCIItem)kvp.Value).treeNodeObject.textName);
                    xmlWriter.WriteAttributeString("index", XmlConvert.ToString(kvp.Key));

                    this.SaveElement(kvp.Value, xmlWriter);

                    xmlWriter.WriteEndElement();
                }
            }
            xmlWriter.WriteEndElement();
        }

        private void LoadDefaultVersion(XmlNode node, int lastIndex = -1)
        {
            this.ExecuteDelayed(() => {
                UnityEngine.Debug.LogError("objects in scene " + Studio.Studio.Instance.dicObjectCtrl.Count);
            }, 6);
            UnityEngine.Debug.LogError("loading scene");
            if (node == null || node.Name != "root")
            {
                return;
                
            }
            string v = node.Attributes["version"].Value;
            this.ExecuteDelayed(() =>
            {
                List<KeyValuePair<int, ObjectCtrlInfo>> dic = new SortedDictionary<int, ObjectCtrlInfo>(Studio.Studio.Instance.dicObjectCtrl).Where(p => p.Key > lastIndex).ToList();
                int i = 0;
                foreach (XmlNode childNode in node.ChildNodes)
                {
                    switch (childNode.Name)
                    {
                        case "characterInfo":

                            OCIChar ociChar = null;
                            while (i < dic.Count && (ociChar = dic[i].Value as OCIChar) == null)
                                ++i;
                            if (i == dic.Count)
                                break;
                            this.LoadElement(ociChar, childNode);
                            ++i;
                            break;
                        case "itemInfo":
                            OCIItem ociItem = null;
                            while (i < dic.Count && (ociItem = dic[i].Value as OCIItem) == null)
                                ++i;
                            if (i == dic.Count)
                                break;
                            this.LoadElement(ociItem, childNode);
                            ++i;
                            break;
                    }
                }
            });
        }
#elif KOIKATSU
        private void OnCharaLoad(ChaFile file)
        {
            PluginData data = ExtendedSave.GetExtendedDataById(file, _extSaveKey);
            if (data == null)
                return;
            XmlDocument doc = new XmlDocument();
            doc.LoadXml((string)data.data["characterInfo"]);
            XmlNode node = doc.FirstChild;
            if (node == null)
                return;
            string v = node.Attributes["version"].Value;
            this.ExecuteDelayed(() =>
            {
                foreach (KeyValuePair<int, ObjectCtrlInfo> pair in Studio.Studio.Instance.dicObjectCtrl)
                {
                    OCIChar ociChar = pair.Value as OCIChar;
                    if (ociChar != null && ociChar.charInfo.chaFile == file)
                        this.LoadElement(ociChar, node);
                }
            });
        }

        private void OnCharaSave(ChaFile file)
        {
            foreach (KeyValuePair<int, ObjectCtrlInfo> pair in Studio.Studio.Instance.dicObjectCtrl)
            {
                OCIChar ociChar = pair.Value as OCIChar;
                if (ociChar != null && ociChar.charInfo.chaFile == file)
                {
                    using (StringWriter stringWriter = new StringWriter())
                    using (XmlTextWriter xmlWriter = new XmlTextWriter(stringWriter))
                    {
                        xmlWriter.WriteStartElement("characterInfo");

                        xmlWriter.WriteAttributeString("version", KKPE.versionNum);
                        xmlWriter.WriteAttributeString("name", ociChar.charInfo.chaFile.parameter.fullname);

                        this.SaveElement(ociChar, xmlWriter);

                        xmlWriter.WriteEndElement();

                        PluginData data = new PluginData();
                        data.version = KKPE.saveVersion;
                        data.data.Add("characterInfo", stringWriter.ToString());
                        ExtendedSave.SetExtendedDataById(file, _extSaveKey, data);
                    }
                }
            }
        }

        private void OnSceneLoad(string path)
        {
            PluginData data = ExtendedSave.GetSceneExtendedDataById(_extSaveKey);
            if (data == null)
                return;
            XmlDocument doc = new XmlDocument();
            doc.LoadXml((string)data.data["sceneInfo"]);
            XmlNode node = doc.FirstChild;
            if (node == null)
                return;
            this.LoadSceneGeneric(node);
        }

        private void OnSceneImport(string path)
        {
            PluginData data = ExtendedSave.GetSceneExtendedDataById(_extSaveKey);
            if (data == null)
                return;
            XmlDocument doc = new XmlDocument();
            doc.LoadXml((string)data.data["sceneInfo"]);
            XmlNode node = doc.FirstChild;
            if (node == null)
                return;
            int max = -1;
            foreach (KeyValuePair<int, ObjectCtrlInfo> pair in Studio.Studio.Instance.dicObjectCtrl)
            {
                if (pair.Key > max)
                    max = pair.Key;
            }
            this.LoadSceneGeneric(node, max);
        }

        private void LoadSceneGeneric(XmlNode node, int lastIndex = -1)
        {
            if (node == null || node.Name != "root")
                return;
            string v = node.Attributes["version"].Value;
            this.ExecuteDelayed(() =>
            {
                List<KeyValuePair<int, ObjectCtrlInfo>> dic = new SortedDictionary<int, ObjectCtrlInfo>(Studio.Studio.Instance.dicObjectCtrl).Where(p => p.Key > lastIndex).ToList();
                int i = 0;
                foreach (XmlNode childNode in node.ChildNodes)
                {
                    switch (childNode.Name)
                    {
                        case "itemInfo":
                            OCIItem ociItem = null;
                            while (i < dic.Count && (ociItem = dic[i].Value as OCIItem) == null)
                                ++i;
                            if (i == dic.Count)
                                break;
                            this.LoadElement(ociItem, childNode);
                            ++i;
                            break;
                    }
                }
            });
        }

        private void OnSceneSave(string path)
        {
            using (StringWriter stringWriter = new StringWriter())
            using (XmlTextWriter xmlWriter = new XmlTextWriter(stringWriter))
            {

                xmlWriter.WriteStartElement("root");
                xmlWriter.WriteAttributeString("version", KKPE.versionNum);
                SortedDictionary<int, ObjectCtrlInfo> dic = new SortedDictionary<int, ObjectCtrlInfo>(Studio.Studio.Instance.dicObjectCtrl);
                foreach (KeyValuePair<int, ObjectCtrlInfo> kvp in dic)
                {
                    OCIItem item = kvp.Value as OCIItem;
                    if (item != null)
                    {
                        xmlWriter.WriteStartElement("itemInfo");
                        xmlWriter.WriteAttributeString("name", item.treeNodeObject.textName);
                        xmlWriter.WriteAttributeString("index", XmlConvert.ToString(kvp.Key));

                        this.SaveElement(item, xmlWriter);

                        xmlWriter.WriteEndElement();
                    }
                }
                xmlWriter.WriteEndElement();

                PluginData data = new PluginData();
                data.version = KKPE.saveVersion;
                data.data.Add("sceneInfo", stringWriter.ToString());
                ExtendedSave.SetSceneExtendedDataById(_extSaveKey, data);
            }
        }
#endif

        private void LoadElement(OCIChar oci, XmlNode node)
        {
            PoseController controller = oci.guideObject.transformTarget.GetComponent<PoseController>();
            if (controller == null)
                controller = oci.guideObject.transformTarget.gameObject.AddComponent<CharaPoseController>();
            bool controllerEnabled = true;
            if (node.Attributes != null && node.Attributes["enabled"] != null)
                controllerEnabled = XmlConvert.ToBoolean(node.Attributes["enabled"].Value);
            controller.ScheduleLoad(node, e =>
            {
                controller.enabled = controllerEnabled;
            });

        }

        private void LoadElement(OCIItem oci, XmlNode node)
        {
            PoseController controller = oci.guideObject.transformTarget.GetComponent<PoseController>();
            if (controller == null)
                controller = oci.guideObject.transformTarget.gameObject.AddComponent<PoseController>();
            bool controllerEnabled = false;
            if (node.Attributes != null && node.Attributes["enabled"] != null)
                controllerEnabled = XmlConvert.ToBoolean(node.Attributes["enabled"].Value);
            controller.ScheduleLoad(node, e =>
            {
                controller.enabled = controllerEnabled;
            });

        }

        private void SaveElement(ObjectCtrlInfo oci, XmlTextWriter xmlWriter)
        {
            PoseController controller = oci.guideObject.transformTarget.GetComponent<PoseController>();
            xmlWriter.WriteAttributeString("enabled", XmlConvert.ToString(controller.enabled));
            controller.SaveXml(xmlWriter);
        }
        #endregion
    }
}
