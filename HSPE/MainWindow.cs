using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Xml;
using RootMotion.FinalIK;
using Studio;
using UILib;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HSPE
{
    public class MainWindow : MonoBehaviour
    {
        #region Public Static Variables
        public static MainWindow self { get; private set; }
        #endregion

        #region Events
        public event Action<TreeNodeObject, TreeNodeObject> onParentage;
        #endregion

        #region Constants
        private const string _config = "configNEO.xml";
        private const string _pluginDir = "Plugins\\HSPE\\";
        private const string _studioSavesDir = "StudioNEOScenes\\";
        #endregion

        #region Private Variables
        private PoseController _poseTarget;
        private readonly List<FullBodyBipedEffector> _boneTargets = new List<FullBodyBipedEffector>();
        private readonly List<Vector3> _lastBonesPositions = new List<Vector3>();
        private readonly List<Quaternion> _lastBonesRotations = new List<Quaternion>();
        private readonly List<FullBodyBipedChain> _bendGoalTargets = new List<FullBodyBipedChain>();
        private readonly List<Vector3> _lastBendGoalsPositions = new List<Vector3>();
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
        private Rect _advancedModeRect = new Rect(Screen.width - 650, Screen.height - 370, 650, 370);
        private readonly Rect[] _advancedModeRects = {
            new Rect(Screen.width - 650, Screen.height - 370, 650, 370),
            new Rect(Screen.width - 800, Screen.height - 390, 800, 390),
            new Rect(Screen.width - 950, Screen.height - 410, 950, 410)
        };
        private float _mainWindowSize = 1f;
        private int _advancedModeWindowSize = 0;
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
        private bool _positionOperationWorld = true;
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
        private IKExecutionOrder _ikExecutionOrder;
        #endregion

        #region Public Accessors
        public Dictionary<string, string> femaleShortcuts { get; } = new Dictionary<string, string>();
        public Dictionary<string, string> maleShortcuts { get; } = new Dictionary<string, string>();
        public Dictionary<string, string> boneAliases { get; } = new Dictionary<string, string>();
        public float resolutionRatio { get; private set; } = ((Screen.width / 1920f) + (Screen.height / 1080f)) / 2f;
        public float uiScale { get; private set; } = 1f;
        public Texture2D vectorEndCap { get; private set; }
        public Texture2D vectorMiddle { get; private set; }
        public bool crotchCorrectionByDefault { get { return this._crotchCorrectionByDefaultToggle.isOn; } }
        public bool anklesCorrectionByDefault { get { return this._anklesCorrectionByDefaultToggle.isOn; } }
        #endregion

        #region Unity Methods
        protected virtual void Awake()
        {
            self = this;

            this._ikExecutionOrder = this.gameObject.AddComponent<IKExecutionOrder>();
            this._ikExecutionOrder.IKComponents = new IK[0];

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
                        if (node.Attributes["value"] != null)
                            this._advancedModeWindowSize = Mathf.Clamp(XmlConvert.ToInt32(node.Attributes["value"].Value), 0, this._advancedModeRects.Length);
                        break;
                    case "femaleShortcuts":
                        foreach (XmlNode shortcut in node.ChildNodes)
                            if (shortcut.Attributes["path"] != null)
                                this.femaleShortcuts.Add(shortcut.Attributes["path"].Value, shortcut.Attributes["path"].Value.Split('/').Last());
                        break;
                    case "maleShortcuts":
                        foreach (XmlNode shortcut in node.ChildNodes)
                            if (shortcut.Attributes["path"] != null)
                                this.maleShortcuts.Add(shortcut.Attributes["path"].Value, shortcut.Attributes["path"].Value.Split('/').Last());
                        break;
                    case "boneAliases":
                        foreach (XmlNode alias in node.ChildNodes)
                            if (alias.Attributes["key"] != null && alias.Attributes["value"] != null)
                                this.boneAliases.Add(alias.Attributes["key"].Value, alias.Attributes["value"].Value);
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

            Action<TreeNodeObject, TreeNodeObject> oldDelegate = Studio.Studio.Instance.treeNodeCtrl.onParentage;
            Studio.Studio.Instance.treeNodeCtrl.onParentage = (parent, node) => this.onParentage?.Invoke(parent, node);
            this.onParentage += oldDelegate;

            HSExtSave.HSExtSave.RegisterHandler("hspe", null, null, this.OnSceneLoad, this.OnSceneImport, this.OnSceneSave, null, null);
            this._randomId = (int)(UnityEngine.Random.value * UInt32.MaxValue);

        }

        protected virtual void Start()
        {
            Type type = Type.GetType("HSUS.HSUS,HSUS");
            if (type != null)
                this.uiScale = (float)type.GetField("_neoUIScale", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(type.GetProperty("self").GetValue(null, null));
            UnityEngine.Debug.LogError(this.uiScale);
            this.SpawnGUI();
            this._crotchCorrectionByDefaultToggle.isOn = this._crotchCorrectionByDefault;
            this._anklesCorrectionByDefaultToggle.isOn = this._anklesCorrectionByDefault;
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
            TreeNodeObject treeNodeObject = Studio.Studio.Instance.treeNodeCtrl.selectNode;
            if (treeNodeObject != null)
            {
                ObjectCtrlInfo info;
                if (Studio.Studio.Instance.dicInfo.TryGetValue(treeNodeObject, out info))
                {
                    OCIChar selected = info as OCIChar;
                    this._poseTarget = selected != null ? selected.charInfo.gameObject.GetComponent<PoseController>() : null;
                }
            }
            else
                this._poseTarget = null;
            if (last != this._poseTarget)
                this.OnTargetChange(last);
            this.GUILogic();
        }

        protected virtual void OnGUI()
        {
            GUIUtility.ScaleAroundPivot(Vector2.one * (this.uiScale * this.resolutionRatio), new Vector2(Screen.width, Screen.height));
            if (this._poseTarget != null)
            {
                if (this._poseTarget.drawAdvancedMode)
                {
                    for (int i = 0; i < 3; ++i)
                        GUI.Box(this._advancedModeRect, "");
                    this._advancedModeRect = GUILayout.Window(this._randomId, this._advancedModeRect, this._poseTarget.AdvancedModeWindow, "Advanced mode");
                    if (this._advancedModeRect.Contains(Event.current.mousePosition) || (this._poseTarget.colliderEditEnabled && this._poseTarget.colliderEditRect.Contains(Event.current.mousePosition)))
                        this._mouseInAdvMode = true;
                    else
                        this._mouseInAdvMode = false;
                }
                else
                    this._mouseInAdvMode = false;
            }
            GUIUtility.ScaleAroundPivot(Vector2.one, new Vector2(Screen.width, Screen.height));
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
                    xmlWriter.WriteAttributeString("version", HSPE.VersionNum.ToString());

                    xmlWriter.WriteStartElement("mainWindowSize");
                    xmlWriter.WriteAttributeString("value", XmlConvert.ToString(this._mainWindowSize));
                    xmlWriter.WriteEndElement();

                    xmlWriter.WriteStartElement("mainWindowShortcut");
                    xmlWriter.WriteAttributeString("value", this._mainWindowKeyCode.ToString());
                    xmlWriter.WriteEndElement();

                    xmlWriter.WriteStartElement("advancedModeWindowSize");
                    xmlWriter.WriteAttributeString("value", XmlConvert.ToString(this._advancedModeWindowSize));
                    xmlWriter.WriteEndElement();

                    xmlWriter.WriteStartElement("femaleShortcuts");
                    foreach (KeyValuePair<string, string> kvp in this.femaleShortcuts)
                    {
                        xmlWriter.WriteStartElement("shortcut");
                        xmlWriter.WriteAttributeString("path", kvp.Key);
                        xmlWriter.WriteEndElement();
                    }
                    xmlWriter.WriteEndElement();

                    xmlWriter.WriteStartElement("maleShortcuts");
                    foreach (KeyValuePair<string, string> kvp in this.maleShortcuts)
                    {
                        xmlWriter.WriteStartElement("shortcut");
                        xmlWriter.WriteAttributeString("path", kvp.Key);
                        xmlWriter.WriteEndElement();
                    }
                    xmlWriter.WriteEndElement();

                    xmlWriter.WriteStartElement("boneAliases");
                    foreach (KeyValuePair<string, string> kvp in this.boneAliases)
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
            string oldResourcesDir = _pluginDir + "Resources\\";
            if (Directory.Exists(oldResourcesDir))
                Directory.Delete(oldResourcesDir, true);

            AssetBundle bundle = AssetBundle.LoadFromMemory(Properties.Resources.HSPEResources);
            Texture2D texture = null;
            foreach (Texture2D tex in bundle.LoadAllAssets<Texture2D>())
            {
                switch (tex.name)
                {
                    case "Icon":
                        texture = tex;
                        break;
                    case "VectorEndCap":
                        this.vectorEndCap = tex;
                        break;
                    case "VectorMiddle":
                        this.vectorMiddle = tex;
                        break;
                }
            }

            {
                RectTransform original = GameObject.Find("StudioScene").transform.Find("Canvas System Menu/01_Button/Button Center").GetComponent<RectTransform>();
                Button hspeButton = Instantiate(original.gameObject).GetComponent<Button>();
                RectTransform hspeButtonRectTransform = hspeButton.transform as RectTransform;
                hspeButton.transform.SetParent(original.parent, true);
                hspeButton.transform.localScale = original.localScale;
                hspeButtonRectTransform.SetRect(original.anchorMin, original.anchorMax, original.offsetMin, original.offsetMax);
                hspeButtonRectTransform.anchoredPosition = original.anchoredPosition + new Vector2(40f, 0f);
                this._hspeButtonImage = hspeButton.targetGraphic as Image;
                this._hspeButtonImage.sprite = Sprite.Create(texture, new Rect(0f, 0f, 32, 32), new Vector2(16, 16));
                hspeButton.onClick = new Button.ButtonClickedEvent();
                hspeButton.onClick.AddListener(() =>
                {
                    this._isVisible = !this._isVisible;
                    this._ui.gameObject.SetActive(this._isVisible);
                    this._hspeButtonImage.color = this._isVisible ? Color.green : Color.white;
                });
                this._hspeButtonImage.color = Color.white;
            }

            this._ui = Instantiate(bundle.LoadAsset<GameObject>("HSPECanvas")).GetComponent<Canvas>();
            bundle.Unload(false);

            RectTransform bg = (RectTransform)this._ui.transform.Find("BG");
            Transform topContainer = bg.Find("Top Container");
            MovableWindow mw = UIUtility.MakeObjectDraggable(topContainer as RectTransform, bg as RectTransform, false);
            mw.onPointerDown += this.OnWindowStartDrag;
            mw.onDrag += this.OnWindowDrag;
            mw.onPointerUp += this.OnWindowEndDrag;

            this._nothingText = this._ui.transform.Find("BG/Nothing Text").gameObject;
            this._nothingText.gameObject.SetActive(false);
            this._controls = this._ui.transform.Find("BG/Controls");

            Button rightShoulder = this._ui.transform.Find("BG/Controls/Bones Buttons/Right Shoulder Button").GetComponent<Button>();
            rightShoulder.onClick.AddListener(() => this.SetBoneTarget(FullBodyBipedEffector.RightShoulder));
            Text t = rightShoulder.GetComponentInChildren<Text>();
            this._effectorsButtons[(int)FullBodyBipedEffector.RightShoulder] = rightShoulder;
            this._effectorsTexts[(int)FullBodyBipedEffector.RightShoulder] = t;

            Button leftShoulder = this._ui.transform.Find("BG/Controls/Bones Buttons/Left Shoulder Button").GetComponent<Button>();
            leftShoulder.onClick.AddListener(() => this.SetBoneTarget(FullBodyBipedEffector.LeftShoulder));
            t = leftShoulder.GetComponentInChildren<Text>();
            this._effectorsButtons[(int)FullBodyBipedEffector.LeftShoulder] = leftShoulder;
            this._effectorsTexts[(int)FullBodyBipedEffector.LeftShoulder] = t;

            Button rightArmBendGoal = this._ui.transform.Find("BG/Controls/Bones Buttons/Right Arm Bend Goal Button").GetComponent<Button>();
            rightArmBendGoal.onClick.AddListener(() => this.SetBendGoalTarget(FullBodyBipedChain.RightArm));
            t = rightArmBendGoal.GetComponentInChildren<Text>();
            this._bendGoalsButtons[(int)FullBodyBipedChain.RightArm] = rightArmBendGoal;
            this._bendGoalsTexts[(int)FullBodyBipedChain.RightArm] = t;

            Button leftArmBendGoal = this._ui.transform.Find("BG/Controls/Bones Buttons/Left Arm Bend Goal Button").GetComponent<Button>();
            leftArmBendGoal.onClick.AddListener(() => this.SetBendGoalTarget(FullBodyBipedChain.LeftArm));
            t = leftArmBendGoal.GetComponentInChildren<Text>();
            this._bendGoalsButtons[(int)FullBodyBipedChain.LeftArm] = leftArmBendGoal;
            this._bendGoalsTexts[(int)FullBodyBipedChain.LeftArm] = t;

            Button rightHand = this._ui.transform.Find("BG/Controls/Bones Buttons/Right Hand Button").GetComponent<Button>();
            rightHand.onClick.AddListener(() => this.SetBoneTarget(FullBodyBipedEffector.RightHand));
            t = rightHand.GetComponentInChildren<Text>();
            this._effectorsButtons[(int)FullBodyBipedEffector.RightHand] = rightHand;
            this._effectorsTexts[(int)FullBodyBipedEffector.RightHand] = t;

            Button leftHand = this._ui.transform.Find("BG/Controls/Bones Buttons/Left Hand Button").GetComponent<Button>();
            leftHand.onClick.AddListener(() => this.SetBoneTarget(FullBodyBipedEffector.LeftHand));
            t = leftHand.GetComponentInChildren<Text>();
            this._effectorsButtons[(int)FullBodyBipedEffector.LeftHand] = leftHand;
            this._effectorsTexts[(int)FullBodyBipedEffector.LeftHand] = t;

            Button body = this._ui.transform.Find("BG/Controls/Bones Buttons/Body Button").GetComponent<Button>();
            body.onClick.AddListener(() => this.SetBoneTarget(FullBodyBipedEffector.Body));
            t = body.GetComponentInChildren<Text>();
            this._effectorsButtons[(int)FullBodyBipedEffector.Body] = body;
            this._effectorsTexts[(int)FullBodyBipedEffector.Body] = t;

            Button rightThigh = this._ui.transform.Find("BG/Controls/Bones Buttons/Right Thigh Button").GetComponent<Button>();
            rightThigh.onClick.AddListener(() => this.SetBoneTarget(FullBodyBipedEffector.RightThigh));
            t = rightThigh.GetComponentInChildren<Text>();
            this._effectorsButtons[(int)FullBodyBipedEffector.RightThigh] = rightThigh;
            this._effectorsTexts[(int)FullBodyBipedEffector.RightThigh] = t;

            Button leftThigh = this._ui.transform.Find("BG/Controls/Bones Buttons/Left Thigh Button").GetComponent<Button>();
            leftThigh.onClick.AddListener(() => this.SetBoneTarget(FullBodyBipedEffector.LeftThigh));
            t = leftThigh.GetComponentInChildren<Text>();
            this._effectorsButtons[(int)FullBodyBipedEffector.LeftThigh] = leftThigh;
            this._effectorsTexts[(int)FullBodyBipedEffector.LeftThigh] = t;

            Button rightLegBendGoal = this._ui.transform.Find("BG/Controls/Bones Buttons/Right Leg Bend Goal Button").GetComponent<Button>();
            rightLegBendGoal.onClick.AddListener(() => this.SetBendGoalTarget(FullBodyBipedChain.RightLeg));
            t = rightLegBendGoal.GetComponentInChildren<Text>();
            this._bendGoalsButtons[(int)FullBodyBipedChain.RightLeg] = rightLegBendGoal;
            this._bendGoalsTexts[(int)FullBodyBipedChain.RightLeg] = t;

            Button leftLegBendGoal = this._ui.transform.Find("BG/Controls/Bones Buttons/Left Leg Bend Goal Button").GetComponent<Button>();
            leftLegBendGoal.onClick.AddListener(() => this.SetBendGoalTarget(FullBodyBipedChain.LeftLeg));
            t = leftLegBendGoal.GetComponentInChildren<Text>();
            this._bendGoalsButtons[(int)FullBodyBipedChain.LeftLeg] = leftLegBendGoal;
            this._bendGoalsTexts[(int)FullBodyBipedChain.LeftLeg] = t;

            Button rightFoot = this._ui.transform.Find("BG/Controls/Bones Buttons/Right Foot Button").GetComponent<Button>();
            rightFoot.onClick.AddListener(() => this.SetBoneTarget(FullBodyBipedEffector.RightFoot));
            t = rightFoot.GetComponentInChildren<Text>();
            this._effectorsButtons[(int)FullBodyBipedEffector.RightFoot] = rightFoot;
            this._effectorsTexts[(int)FullBodyBipedEffector.RightFoot] = t;

            Button leftFoot = this._ui.transform.Find("BG/Controls/Bones Buttons/Left Foot Button").GetComponent<Button>();
            leftFoot.onClick.AddListener(() => this.SetBoneTarget(FullBodyBipedEffector.LeftFoot));
            t = leftFoot.GetComponentInChildren<Text>();
            this._effectorsButtons[(int)FullBodyBipedEffector.LeftFoot] = leftFoot;
            this._effectorsTexts[(int)FullBodyBipedEffector.LeftFoot] = t;

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

            Button copyLeftArmButton = this._ui.transform.Find("BG/Controls/Other buttons/Copy Limbs/Copy Right Arm Button").GetComponent<Button>();
            copyLeftArmButton.onClick.AddListener(() =>
            {
                if (this._poseTarget != null)
                    this._poseTarget.CopyLimbToTwin(FullBodyBipedChain.RightArm);
            });

            Button copyRightArmButton = this._ui.transform.Find("BG/Controls/Other buttons/Copy Limbs/Copy Left Arm Button").GetComponent<Button>();
            copyRightArmButton.onClick.AddListener(() =>
            {
                if (this._poseTarget != null)
                    this._poseTarget.CopyLimbToTwin(FullBodyBipedChain.LeftArm);
            });

            Button copyLeftLegButton = this._ui.transform.Find("BG/Controls/Other buttons/Copy Limbs/Copy Right Leg Button").GetComponent<Button>();
            copyLeftLegButton.onClick.AddListener(() =>
            {
                if (this._poseTarget != null)
                    this._poseTarget.CopyLimbToTwin(FullBodyBipedChain.RightLeg);
            });

            Button copyRightLegButton = this._ui.transform.Find("BG/Controls/Other buttons/Copy Limbs/Copy Left LegButton").GetComponent<Button>();
            copyRightLegButton.onClick.AddListener(() =>
            {
                if (this._poseTarget != null)
                    this._poseTarget.CopyLimbToTwin(FullBodyBipedChain.LeftLeg);
            });

            Button swapPostButton = this._ui.transform.Find("BG/Controls/Other buttons/Other/Swap Pose Button").GetComponent<Button>();
            swapPostButton.onClick.AddListener(() =>
            {
                if (this._poseTarget != null)
                    this._poseTarget.SwapPose();
            });

            Button advancedModeButton = this._ui.transform.Find("BG/Controls/Buttons/Simple Options/Advanced Mode Button").GetComponent<Button>();
            advancedModeButton.onClick.AddListener(this.ToggleAdvancedMode);

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
                    this._poseTarget.optimizeIK = this._optimizeIKToggle.isOn;
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
            this.OnTargetChange(null);

            this._optionsWindow = (RectTransform)this._ui.transform.Find("Options Window");

            topContainer = this._optionsWindow.Find("Top Container");
            mw = UIUtility.MakeObjectDraggable(topContainer as RectTransform, this._optionsWindow, false);
            mw.onPointerDown += this.OnWindowStartDrag;
            mw.onDrag += this.OnWindowDrag;
            mw.onPointerUp += this.OnWindowEndDrag;

            Vector2 sizeDelta = bg.sizeDelta;
            Text xMoveText = xMoveButton.GetComponentInChildren<Text>();
            Text yMoveText = yMoveButton.GetComponentInChildren<Text>();
            Text zMoveText = zMoveButton.GetComponentInChildren<Text>();
            Text xRotText = rotXButton.GetComponentInChildren<Text>();
            Text yRotText = rotYButton.GetComponentInChildren<Text>();
            Text zRotText = rotZButton.GetComponentInChildren<Text>();

            int moveFontSize = xMoveText.fontSize;
            int rotFontSize = xRotText.fontSize;

            Button normalButton = this._ui.transform.Find("Options Window/Options/Main Window Size Container/Normal Button").GetComponent<Button>();
            normalButton.onClick.AddListener(() =>
            {
                this._mainWindowSize = 1f;
                bg.sizeDelta = sizeDelta * this._mainWindowSize;
                xMoveText.fontSize = (int)(moveFontSize * this._mainWindowSize);
                yMoveText.fontSize = (int)(moveFontSize * this._mainWindowSize);
                zMoveText.fontSize = (int)(moveFontSize * this._mainWindowSize);
                xRotText.fontSize = (int)(rotFontSize * this._mainWindowSize);
                yRotText.fontSize = (int)(rotFontSize * this._mainWindowSize);
                zRotText.fontSize = (int)(rotFontSize * this._mainWindowSize);
            });

            Button largeButton = this._ui.transform.Find("Options Window/Options/Main Window Size Container/Large Button").GetComponent<Button>();
            largeButton.onClick.AddListener(() =>
            {
                this._mainWindowSize = 1.25f;
                bg.sizeDelta = sizeDelta * this._mainWindowSize;
                xMoveText.fontSize = (int)(moveFontSize * this._mainWindowSize);
                yMoveText.fontSize = (int)(moveFontSize * this._mainWindowSize);
                zMoveText.fontSize = (int)(moveFontSize * this._mainWindowSize);
                xRotText.fontSize = (int)(rotFontSize * this._mainWindowSize);
                yRotText.fontSize = (int)(rotFontSize * this._mainWindowSize);
                zRotText.fontSize = (int)(rotFontSize * this._mainWindowSize);
            });

            Button veryLargeButton = this._ui.transform.Find("Options Window/Options/Main Window Size Container/Very Large Button").GetComponent<Button>();
            veryLargeButton.onClick.AddListener(() =>
            {
                this._mainWindowSize = 1.5f;
                bg.sizeDelta = sizeDelta * this._mainWindowSize;
                xMoveText.fontSize = (int)(moveFontSize * this._mainWindowSize);
                yMoveText.fontSize = (int)(moveFontSize * this._mainWindowSize);
                zMoveText.fontSize = (int)(moveFontSize * this._mainWindowSize);
                xRotText.fontSize = (int)(rotFontSize * this._mainWindowSize);
                yRotText.fontSize = (int)(rotFontSize * this._mainWindowSize);
                zRotText.fontSize = (int)(rotFontSize * this._mainWindowSize);
            });
            bg.sizeDelta = sizeDelta * this._mainWindowSize;
            xMoveText.fontSize = (int)(moveFontSize * this._mainWindowSize);
            yMoveText.fontSize = (int)(moveFontSize * this._mainWindowSize);
            zMoveText.fontSize = (int)(moveFontSize * this._mainWindowSize);
            xRotText.fontSize = (int)(rotFontSize * this._mainWindowSize);
            yRotText.fontSize = (int)(rotFontSize * this._mainWindowSize);
            zRotText.fontSize = (int)(rotFontSize * this._mainWindowSize);
            this._optionsWindow.anchoredPosition += new Vector2((sizeDelta.x * this._mainWindowSize) - sizeDelta.x, 0f);

            normalButton = this._ui.transform.Find("Options Window/Options/Advanced Mode Window Size Container/Normal Button").GetComponent<Button>();
            normalButton.onClick.AddListener(() =>
            {
                this._advancedModeWindowSize = 0;
                Rect r = this._advancedModeRects[this._advancedModeWindowSize];
                this._advancedModeRect.xMin = this._advancedModeRect.xMax - r.width;
                this._advancedModeRect.width = r.width;
                this._advancedModeRect.yMin = this._advancedModeRect.yMax - r.height;
                this._advancedModeRect.height = r.height;
            });

            largeButton = this._ui.transform.Find("Options Window/Options/Advanced Mode Window Size Container/Large Button").GetComponent<Button>();
            largeButton.onClick.AddListener(() =>
            {
                this._advancedModeWindowSize = 1;
                Rect r = this._advancedModeRects[this._advancedModeWindowSize];
                this._advancedModeRect.xMin = this._advancedModeRect.xMax - r.width;
                this._advancedModeRect.width = r.width;
                this._advancedModeRect.yMin = this._advancedModeRect.yMax - r.height;
                this._advancedModeRect.height = r.height;
            });

            veryLargeButton = this._ui.transform.Find("Options Window/Options/Advanced Mode Window Size Container/Very Large Button").GetComponent<Button>();
            veryLargeButton.onClick.AddListener(() =>
            {
                this._advancedModeWindowSize = 2;
                Rect r = this._advancedModeRects[this._advancedModeWindowSize];
                this._advancedModeRect.xMin = this._advancedModeRect.xMax - r.width;
                this._advancedModeRect.width = r.width;
                this._advancedModeRect.yMin = this._advancedModeRect.yMax - r.height;
                this._advancedModeRect.height = r.height;
            });
            this._advancedModeRect = this._advancedModeRects[this._advancedModeWindowSize];

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
                        this._poseTarget.crotchJointCorrection = this._crotchCorrectionToggle.isOn;
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
                        this._poseTarget.leftFootJointCorrection = this._leftFootCorrectionToggle.isOn;
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
                        this._poseTarget.rightFootJointCorrection = this._rightFootCorrectionToggle.isOn;
                });
            }
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
            this._advancedModeRects[0] = new Rect(Screen.width - 650, Screen.height - 370, 650, 370);
            this._advancedModeRects[1] = new Rect(Screen.width - 800, Screen.height - 390, 800, 390);
            this._advancedModeRects[2] = new Rect(Screen.width - 950, Screen.height - 410, 950, 410);

            Rect r = this._advancedModeRects[this._advancedModeWindowSize];
            this._advancedModeRect.xMin = this._advancedModeRect.xMax - r.width;
            this._advancedModeRect.width = r.width;
            this._advancedModeRect.yMin = this._advancedModeRect.yMax - r.height;
            this._advancedModeRect.height = r.height;

            this.resolutionRatio = (Screen.width / 1920f + Screen.height / 1080f) / 2f;
        }


        private void SetBoneTarget(FullBodyBipedEffector bone)
        {
            this.ResetBoneButtons();
            if (!Input.GetKey(KeyCode.LeftControl) && !Input.GetKey(KeyCode.RightControl))
            {
                this._boneTargets.Clear();
                this._bendGoalTargets.Clear();
                this._boneTargets.Add(bone);
            }
            else
            {
                if (this._boneTargets.Contains(bone))
                    this._boneTargets.Remove(bone);
                else
                    this._boneTargets.Add(bone);
            }
            this._lastBonesPositions.Resize(this._boneTargets.Count);
            this._lastBonesRotations.Resize(this._boneTargets.Count);
            if (this._bendGoalTargets.Count != 0 ||
                this._boneTargets.Intersect(PoseController.nonRotatableEffectors).Any())
                foreach (Button bu in this._rotationButtons)
                    bu.interactable = false;
            else
                foreach (Button bu in this._rotationButtons)
                    bu.interactable = true;
            EventSystem.current.SetSelectedGameObject(null);
        }

        private void SetBendGoalTarget(FullBodyBipedChain bendGoal)
        {
            this.ResetBoneButtons();
            if (!Input.GetKey(KeyCode.LeftControl) && !Input.GetKey(KeyCode.RightControl))
            {
                this._boneTargets.Clear();
                this._bendGoalTargets.Clear();
                this._bendGoalTargets.Add(bendGoal);
            }
            else
            {
                if (this._bendGoalTargets.Contains(bendGoal))
                    this._bendGoalTargets.Remove(bendGoal);
                else
                    this._bendGoalTargets.Add(bendGoal);
            }
            this._lastBendGoalsPositions.Resize(this._bendGoalTargets.Count);
            foreach (Button bu in this._rotationButtons)
                bu.interactable = false;
            EventSystem.current.SetSelectedGameObject(null);
        }

        private void ResetBoneButtons()
        {
            foreach (FullBodyBipedChain bendGoal in this._bendGoalTargets)
            {
                Button b = this._bendGoalsButtons[(int)bendGoal];
                ColorBlock cb = b.colors;
                cb.normalColor = Color.Lerp(Color.blue, Color.white, 0.5f);
                b.colors = cb;
                this._bendGoalsTexts[(int)bendGoal].fontStyle = FontStyle.Normal;
            }
            foreach (FullBodyBipedEffector effector in this._boneTargets)
            {
                Button b = this._effectorsButtons[(int)effector];
                ColorBlock cb = b.colors;
                cb.normalColor = Color.Lerp(Color.red, Color.white, 0.5f);
                b.colors = cb;
                this._effectorsTexts[(int)effector].fontStyle = FontStyle.Normal;
            }
        }

        private void SelectBoneButtons()
        {
            foreach (FullBodyBipedChain bendGoal in this._bendGoalTargets)
            {
                Button b = this._bendGoalsButtons[(int)bendGoal];
                ColorBlock cb = b.colors;
                cb.normalColor = UIUtility.lightGreenColor;
                b.colors = cb;
                this._bendGoalsTexts[(int)bendGoal].fontStyle = FontStyle.Bold;
            }
            foreach (FullBodyBipedEffector effector in this._boneTargets)
            {
                Button b = this._effectorsButtons[(int)effector];
                ColorBlock cb = b.colors;
                cb.normalColor = UIUtility.lightGreenColor;
                b.colors = cb;
                this._effectorsTexts[(int)effector].fontStyle = FontStyle.Bold;
            }
        }

        private void ToggleAdvancedMode()
        {
            if (this._poseTarget != null)
                this._poseTarget.drawAdvancedMode = !this._poseTarget.drawAdvancedMode;
        }

        private void GUILogic()
        {
            if (this._shortcutRegisterMode == false && Input.GetKeyDown(this._mainWindowKeyCode))
            {
                this._isVisible = !this._isVisible;
                this._ui.gameObject.SetActive(this._isVisible);
                this._hspeButtonImage.color = this._isVisible ? Color.green : Color.white;
            }

            if (Input.GetMouseButtonDown(0))
            {
                if (this._poseTarget != null && this._poseTarget.drawAdvancedMode && this._mouseInAdvMode)
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

            if (this._poseTarget != null)
            {
                this.ResetBoneButtons();
                bool shouldReset = false;
                for (int i = 0; i < this._boneTargets.Count; i++)
                {
                    FullBodyBipedEffector bone = this._boneTargets[i];
                    if (this._poseTarget.IsPartEnabled(bone) == false)
                    {
                        this._boneTargets.RemoveAt(i);
                        --i;
                        shouldReset = true;
                    }
                }

                for (int i = 0; i < this._bendGoalTargets.Count; i++)
                {
                    FullBodyBipedChain bendGoal = this._bendGoalTargets[i];
                    if (this._poseTarget.IsPartEnabled(bendGoal) == false)
                    {
                        this._bendGoalTargets.RemoveAt(i);
                        --i;
                        shouldReset = true;
                    }
                }
                this.SelectBoneButtons();

                if (shouldReset)
                {
                    this._lastBonesPositions.Resize(this._boneTargets.Count);
                    this._lastBonesRotations.Resize(this._boneTargets.Count);
                }

                for (int i = 0; i < this._effectorsButtons.Length; i++)
                    this._effectorsButtons[i].interactable = this._poseTarget.IsPartEnabled((FullBodyBipedEffector)i);

                for (int i = 0; i < this._bendGoalsButtons.Length; i++)
                    this._bendGoalsButtons[i].interactable = this._poseTarget.IsPartEnabled((FullBodyBipedChain)i);
            }

            if (this._xMove || this._yMove || this._zMove || this._xRot || this._yRot || this._zRot)
            {
                this._delta += new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")) / (10f * (Input.GetMouseButton(1) ? 2f : 1f));
                if (this._poseTarget != null)
                {
                    if (this._poseTarget.currentDragType == PoseController.DragType.None)
                        this._poseTarget.StartDrag(this._xMove || this._yMove || this._zMove ? PoseController.DragType.Position : PoseController.DragType.Rotation);
                    for (int i = 0; i < this._boneTargets.Count; ++i)
                    {
                        bool changePosition = false;
                        bool changeRotation = false;
                        Vector3 newPosition = this._lastBonesPositions[i];
                        Quaternion newRotation = this._lastBonesRotations[i];
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
                        if (changePosition)
                            this._poseTarget.SetBoneTargetPosition(this._boneTargets[i], newPosition, this._positionOperationWorld);
                        if (changeRotation)
                            this._poseTarget.SetBoneTargetRotation(this._boneTargets[i], newRotation);
                    }
                    for (int i = 0; i < this._bendGoalTargets.Count; ++i)
                    {
                        Vector3 newPosition = this._lastBendGoalsPositions[i];
                        if (this._xMove)
                            newPosition.x += this._delta.y * this._intensityValue;
                        if (this._yMove)
                            newPosition.y += this._delta.y * this._intensityValue;
                        if (this._zMove)
                            newPosition.z += this._delta.y * this._intensityValue;
                        this._poseTarget.SetBendGoalPosition(this._bendGoalTargets[i], newPosition, this._positionOperationWorld);
                    }
                }
            }
            else
            {
                this._delta = Vector2.zero;
                if (this._poseTarget != null)
                {
                    if (this._poseTarget.currentDragType != PoseController.DragType.None)
                        this._poseTarget.StopDrag();
                    for (int i = 0; i < this._boneTargets.Count; ++i)
                    {
                        this._lastBonesPositions[i] = this._poseTarget.GetBoneTargetPosition(this._boneTargets[i], this._positionOperationWorld);
                        this._lastBonesRotations[i] = this._poseTarget.GetBoneTargetRotation(this._boneTargets[i]);
                    }
                    for (int i = 0; i < this._bendGoalTargets.Count; ++i)
                        this._lastBendGoalsPositions[i] = this._poseTarget.GetBendGoalPosition(this._bendGoalTargets[i], this._positionOperationWorld);
                }
            }

            if (this._shortcutRegisterMode)
            {
                foreach (KeyCode kc in this._possibleKeyCodes)
                {
                    if (Input.GetKeyDown(kc))
                    {
                        if (kc != KeyCode.Escape && kc != KeyCode.Return && kc != KeyCode.Mouse0)
                            this._mainWindowKeyCode = kc;
                        this._shortcutKeyButton.onClick.Invoke();
                        break;
                    }
                }
            }
        }
        #endregion

        #region Public Methods
        public void SetNoControlCondition()
        {
            Studio.Studio.Instance.cameraCtrl.noCtrlCondition = this.CameraControllerCondition;
        }

        public void OnDuplicate(OCIChar source, OCIChar destination)
        {
            PoseController destinationController = destination.charInfo.gameObject.AddComponent<PoseController>();
            destinationController.LoadFrom(source.charInfo.gameObject.GetComponent<PoseController>());
        }
        #endregion

        #region Private Methods
        private void OnObjectAdded()
        {
            foreach (KeyValuePair<int, ObjectCtrlInfo> kvp in Studio.Studio.Instance.dicObjectCtrl)
            {
                if (kvp.Key >= this._lastIndex)
                {
                    OCIChar ociChar = kvp.Value as OCIChar;
                    if (ociChar != null && ociChar.charInfo.gameObject.GetComponent<PoseController>() == null)
                        ociChar.charInfo.gameObject.AddComponent<PoseController>();
                }
            }
        }

        private void OnTargetChange(PoseController last)
        {
            if (last != null)
                last.drawAdvancedMode = false;
            if (this._poseTarget == null)
            {
                this._nothingText.gameObject.SetActive(true);
                this._controls.gameObject.SetActive(false);
            }
            else
            {
                this._optimizeIKToggle.isOn = this._poseTarget.optimizeIK;
                this._crotchCorrectionToggle.isOn = this._poseTarget.crotchJointCorrection;
                this._leftFootCorrectionToggle.isOn = this._poseTarget.leftFootJointCorrection;
                this._rightFootCorrectionToggle.isOn = this._poseTarget.rightFootJointCorrection;
                this._nothingText.gameObject.SetActive(false);
                this._controls.gameObject.SetActive(true);
            }
        }



        [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
        private bool CameraControllerCondition()
        {
            return this._blockCamera || this._xMove || this._yMove || this._zMove || this._xRot || this._yRot || this._zRot || this._mouseInAdvMode || this._windowMoving || (this._poseTarget != null && this._poseTarget.isDraggingDynamicBone);
        }
        #endregion

        #region Saves
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
            int written = 0;
            xmlWriter.WriteStartElement("root");
            xmlWriter.WriteAttributeString("version", HSPE.VersionNum);
            SortedDictionary<int, ObjectCtrlInfo> dic = new SortedDictionary<int, ObjectCtrlInfo>(Studio.Studio.Instance.dicObjectCtrl);
            foreach (KeyValuePair<int, ObjectCtrlInfo> kvp in dic)
            {
                OCIChar ociChar = kvp.Value as OCIChar;
                if (ociChar != null)
                {
                    xmlWriter.WriteStartElement("characterInfo");
                    xmlWriter.WriteAttributeString("name", ociChar.charInfo.customInfo.name);
                    xmlWriter.WriteAttributeString("index", XmlConvert.ToString(kvp.Key));
                    PoseController controller = ociChar.charInfo.gameObject.GetComponent<PoseController>();
                    written += controller.SaveXml(xmlWriter);
                    xmlWriter.WriteEndElement();
                }
            }
            xmlWriter.WriteEndElement();
        }

        private void LoadDefaultVersion(XmlNode node, int lastIndex = -1)
        {
            if (node == null || node.Name != "root")
                return;
            string v = node.Attributes["version"].Value;
            node = node.CloneNode(true);
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
                            PoseController controller = ociChar.charInfo.gameObject.GetComponent<PoseController>();
                            if (controller == null)
                                controller = ociChar.charInfo.gameObject.AddComponent<PoseController>();
                            controller.ScheduleLoad(childNode, v);
                            ++i;
                            break;
                    }
                }
            });
        }
        #endregion
    }
}
