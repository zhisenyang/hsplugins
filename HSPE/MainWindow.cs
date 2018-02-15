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
        public event Action<Studio.TreeNodeObject, Studio.TreeNodeObject> onParentage;
        public event Action onPostUpdate;
        #endregion

        #region Private Constants
        private const string _config = "configNEO.xml";
        private const string _pluginDir = "Plugins\\HSPE\\";
        private const string _studioSavesDir = "StudioNEOScenes\\";
        #endregion

        #region IK Hack
        public class CustomIK : IK
        {
            public CustomIKSolver solver;

            public override IKSolver GetIKSolver()
            {
                return this.solver;
            }

            protected override void OpenUserManual()
            {
                throw new NotImplementedException();
            }

            protected override void OpenScriptReference()
            {
                throw new NotImplementedException();
            }
        }

        public class CustomIKSolver : IKSolver
        {
            public override bool IsValid(ref string message)
            {
                throw new NotImplementedException();
            }

            public override Point[] GetPoints()
            {
                throw new NotImplementedException();
            }

            public override Point GetPoint(Transform transform)
            {
                throw new NotImplementedException();
            }

            public override void FixTransforms()
            {
            }

            public override void StoreDefaultLocalState()
            {
            }

            protected override void OnInitiate()
            {
            }

            protected override void OnUpdate()
            {
            }
        }
        #endregion

        #region Private Variables
        private ManualBoneController _manualBoneTarget;
        private readonly List<FullBodyBipedEffector> _boneTargets = new List<FullBodyBipedEffector>();
        private readonly List<Vector3> _lastBonesPositions = new List<Vector3>();
        private readonly List<Quaternion> _lastBonesRotations = new List<Quaternion>();
        private readonly List<FullBodyBipedChain> _bendGoalTargets = new List<FullBodyBipedChain>();
        private readonly List<Vector3> _lastBendGoalsPositions = new List<Vector3>();
        private readonly Dictionary<string, KeyCode> _nameToKeyCode = new Dictionary<string, KeyCode>();
        private KeyCode _mainWindowKeyCode = KeyCode.H;
        private string _selectedScenePath;
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
        private readonly Rect[] _advancedModeRects = new Rect[]
        {
                    new Rect(Screen.width - 650, Screen.height - 370, 650, 370),
                    new Rect(Screen.width - 800, Screen.height - 390, 800, 390),
                    new Rect(Screen.width - 950, Screen.height - 410, 950, 410)
        };
        private int _advancedModeWindowSize = 0;
        private Canvas _ui;
        private Text _nothingText;
        private RectTransform _controls;
        private RectTransform _bones;
        private Scrollbar _movementIntensity;
        private Text _intensityValueText;
        private RectTransform _optionsWindow;
        private float _intensityValue = 1f;
        private readonly Button[] _effectorsButtons = new Button[9];
        private readonly Button[] _bendGoalsButtons = new Button[4];
        private readonly Text[] _effectorsTexts = new Text[9];
        private readonly Text[] _bendGoalsTexts = new Text[4];
        private readonly Button[] _positionButtons = new Button[3];
        private readonly Button[] _rotationButtons = new Button[3];
        private bool _charactersAddedByCopy = false;
        private readonly List<ManualBoneController> _copySources = new List<ManualBoneController>();
        private Button _shortcutKeyButton;
        private bool _shortcutRegisterMode = false;
        private KeyCode[] _possibleKeyCodes;
        private IKExecutionOrder _ikExecutionOrder;
        private bool _positionOperationWorld = true;
        private Toggle _optimizeIKToggle;
        private bool _windowMoving;
        private Image _hspeButtonImage;
        #endregion

        #region Public Accessors
        public Dictionary<string, string> femaleShortcuts { get; } = new Dictionary<string, string>();
        public Dictionary<string, string> maleShortcuts { get; } = new Dictionary<string, string>();
        public Dictionary<string, string> boneAliases { get; } = new Dictionary<string, string>();
        public float resolutionRatio { get; private set; } = ((Screen.width / 1920f) + (Screen.height / 1080f)) / 2f;
        public IKExecutionOrder ikExecutionOrder { get { return this._ikExecutionOrder; } }
        public float uiScale { get; private set; }
        #endregion

        #region Unity Methods

        protected virtual void Awake()
        {
            self = this;
            GameObject.Find("StudioScene").transform.FindChild("Canvas Main Menu/04_System/Viewport/Content/Load").GetComponent<Button>().onClick.AddListener(this.LoadCanvasCreated);
            GameObject.Find("StudioScene").transform.FindChild("Canvas Main Menu/04_System/Viewport/Content/Save").GetComponent<Button>().onClick.AddListener(this.OnSceneSave);
            GameObject.Find("StudioScene").transform.FindChild("Canvas Object List/Image Bar/Button Duplicate").GetComponent<Button>().onClick.AddListener(this.OnDuplicate);

            CustomIK ik = new CustomIK();
            CustomIKSolver ikSolver = new CustomIKSolver();
            ik.solver = ikSolver;
            ikSolver.SetPrivate("firstInitiation", false);
            ikSolver.SetPrivateProperty("initiated", true);
            ikSolver.OnPostUpdate = this.CallPostUpdate;
            this._ikExecutionOrder = this.gameObject.AddComponent<IKExecutionOrder>();
            this._ikExecutionOrder.IKComponents = new IK[1] { ik };

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
                    case "uiScale":
                        if (node.Attributes["value"] != null)
                            this.uiScale = Mathf.Clamp(XmlConvert.ToSingle(node.Attributes["value"].Value), 0.5f, 2f);
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
                }
            }

            Action<Studio.TreeNodeObject, Studio.TreeNodeObject> oldDelegate = Studio.Studio.Instance.treeNodeCtrl.onParentage;
            Studio.Studio.Instance.treeNodeCtrl.onParentage = (parent, node) => this.onParentage?.Invoke(parent, node);
            this.onParentage += oldDelegate;
        }

        protected virtual void Start()
        {
            this.SpawnGUI();
        }

        protected virtual void Update()
        {
            int objectCount = Studio.Studio.Instance.dicObjectCtrl.Count;
            if (objectCount != this._lastObjectCount)
            {
                if (objectCount > this._lastObjectCount)
                    this.OnObjectAdded();
                this._lastIndex = Studio.Studio.Instance.sceneInfo.CheckNewIndex();
            }
            this._lastObjectCount = Studio.Studio.Instance.dicObjectCtrl.Count;


            ManualBoneController last = this._manualBoneTarget;
            Studio.TreeNodeObject treeNodeObject = Studio.Studio.Instance.treeNodeCtrl.selectNode;
            if (treeNodeObject != null)
            {
                Studio.ObjectCtrlInfo info;
                if (Studio.Studio.Instance.dicInfo.TryGetValue(treeNodeObject, out info))
                {
                    Studio.OCIChar selected = info as Studio.OCIChar;
                    this._manualBoneTarget = selected != null ? selected.charInfo.gameObject.GetComponent<ManualBoneController>() : null;
                }
            }
            else
                this._manualBoneTarget = null;
            if (last != this._manualBoneTarget)
                this.OnTargetChange(last);
            this.GUILogic();
        }

        protected virtual void LateUpdate()
        {
            if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyDown(KeyCode.S))
                this.StartCoroutine(this.OnSceneSavedShortcut());
        }

        protected virtual void OnGUI()
        {
            GUIUtility.ScaleAroundPivot(Vector2.one * (this.uiScale * this.resolutionRatio), new Vector2(Screen.width, Screen.height));
            if (this._manualBoneTarget != null)
            {
                if (this._manualBoneTarget.drawAdvancedMode)
                {
                    for (int i = 0; i < 3; ++i)
                        GUI.Box(this._advancedModeRect, "");
                    this._advancedModeRect = GUILayout.Window(50, this._advancedModeRect, this._manualBoneTarget.AdvancedModeWindow, "Advanced mode");
                    if (this._advancedModeRect.Contains(Event.current.mousePosition) || (this._manualBoneTarget.colliderEditEnabled && this._manualBoneTarget.colliderEditRect.Contains(Event.current.mousePosition)))
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

                    xmlWriter.WriteStartElement("uiScale");
                    xmlWriter.WriteAttributeString("value", XmlConvert.ToString(this.uiScale));
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

                    xmlWriter.WriteEndElement();
                }
            }
        }
        #endregion

        #region GUI
        private void SpawnGUI()
        {

            byte[] bytes = File.ReadAllBytes(_pluginDir + "Resources\\Icon.png");
            Texture2D texture = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Trilinear;
            texture.LoadImage(bytes);

            RectTransform original = GameObject.Find("StudioScene").transform.Find("Canvas System Menu/01_Button/Button Center").GetComponent<RectTransform>();
            Button hspeButton = GameObject.Instantiate(original.gameObject).GetComponent<Button>();
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


            this._ui = UIUtility.CreateNewUISystem("HSPE");
            this._ui.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920 / this.uiScale, 1080 / this.uiScale);

            {
                Image bg = UIUtility.CreatePanel("BG", this._ui.transform);
                bg.raycastTarget = false;
                bg.color = UIUtility.whiteColor;
                bg.rectTransform.SetRect(Vector2.zero, Vector2.zero, new Vector2(3f, 3f), new Vector2(300f, 470f));
                bg.rectTransform.anchoredPosition = new Vector2(276f, 363f);

                {
                    Image topContainer = UIUtility.CreatePanel("Top Container", bg.rectTransform);
                    topContainer.color = UIUtility.grayColor;
                    topContainer.rectTransform.SetRect(new Vector2(0f, 1f), Vector2.one, new Vector2(4f, -28f), new Vector2(-4f, -4f));
                    MovableWindow mw = UIUtility.MakeObjectDraggable(topContainer.rectTransform, bg.rectTransform, false);
                    mw.onPointerDown += this.OnWindowStartDrag;
                    mw.onDrag += this.OnWindowDrag;
                    mw.onPointerUp += this.OnWindowEndDrag;

                    Text titleText = this.CreateCustomText("Title Text", topContainer.transform, "HSPE");
                    titleText.alignment = TextAnchor.MiddleCenter;
                    titleText.fontStyle = FontStyle.Bold;
                    titleText.rectTransform.SetRect(Vector2.zero, Vector2.one, new Vector2(2f, 2f), new Vector2(-2f, -2f));
                    titleText.color = Color.white;

                    titleText.GetComponent<Outline>().effectDistance = new Vector2(2f, 2f);
                }

                this._nothingText = this.CreateCustomText("Nothing Text", bg.transform, "There is no character selected. Please select a character to begin pose edition.");
                this._nothingText.alignment = TextAnchor.MiddleCenter;
                this._nothingText.fontSize = 16;
                this._nothingText.resizeTextForBestFit = false;
                this._nothingText.rectTransform.SetRect(Vector2.zero, Vector2.one, new Vector2(5f, 5f), new Vector2(-5f, -25f));
                this._nothingText.gameObject.SetActive(false);

                {
                    this._controls = UIUtility.CreateNewUIObject(bg.transform, "Controls");
                    this._controls.SetRect(Vector2.zero, Vector2.one, new Vector2(5f, 5f), new Vector2(-5f, -5f));

                    {
                        this._bones = UIUtility.CreateNewUIObject(this._controls, "Bones");
                        this._bones.SetRect(Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0f, -24f));

                        Button rightShoulder = this.CreateCustomButton("Right Shoulder Button", this._bones, "R. Shoulder");
                        rightShoulder.onClick.AddListener(() => this.SetBoneTarget(FullBodyBipedEffector.RightShoulder));
                        ColorBlock cb = rightShoulder.colors;
                        cb.normalColor = Color.Lerp(Color.red, Color.white, 0.5f);
                        cb.highlightedColor = Color.Lerp(Color.red, Color.white, 0.65f);
                        rightShoulder.colors = cb;
                        Text t = rightShoulder.GetComponentInChildren<Text>();
                        RectTransform buttonRT = rightShoulder.transform as RectTransform;
                        buttonRT.SetRect(new Vector2(0.25f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -35f), Vector2.zero);
                        this._effectorsButtons[(int)FullBodyBipedEffector.RightShoulder] = rightShoulder;
                        this._effectorsTexts[(int)FullBodyBipedEffector.RightShoulder] = t;

                        Button leftShoulder = this.CreateCustomButton("Left Shoulder Button", this._bones, "L. Shoulder");
                        cb = leftShoulder.colors;
                        cb.normalColor = Color.Lerp(Color.red, Color.white, 0.5f);
                        cb.highlightedColor = Color.Lerp(Color.red, Color.white, 0.65f);
                        leftShoulder.colors = cb;
                        leftShoulder.onClick.AddListener(() => this.SetBoneTarget(FullBodyBipedEffector.LeftShoulder));
                        t = leftShoulder.GetComponentInChildren<Text>();
                        buttonRT = leftShoulder.transform as RectTransform;
                        buttonRT.SetRect(new Vector2(0.5f, 1f), new Vector2(0.75f, 1f), new Vector2(0f, -35f), Vector2.zero);
                        this._effectorsButtons[(int)FullBodyBipedEffector.LeftShoulder] = leftShoulder;
                        this._effectorsTexts[(int)FullBodyBipedEffector.LeftShoulder] = t;

                        Button rightArmBendGoal = this.CreateCustomButton("Right Arm Bend Goal Button", this._bones, "R. Elbow Dir.");
                        cb = rightArmBendGoal.colors;
                        cb.normalColor = Color.Lerp(Color.blue, Color.white, 0.5f);
                        cb.highlightedColor = Color.Lerp(Color.blue, Color.white, 0.65f);
                        rightArmBendGoal.colors = cb;
                        rightArmBendGoal.onClick.AddListener(() => this.SetBendGoalTarget(FullBodyBipedChain.RightArm));
                        t = rightArmBendGoal.GetComponentInChildren<Text>();
                        buttonRT = rightArmBendGoal.transform as RectTransform;
                        buttonRT.SetRect(new Vector2(0.125f, 1f), new Vector2(0.375f, 1f), new Vector2(0f, -70f), new Vector2(0f, -35f));
                        this._bendGoalsButtons[(int)FullBodyBipedChain.RightArm] = rightArmBendGoal;
                        this._bendGoalsTexts[(int)FullBodyBipedChain.RightArm] = t;

                        Button leftArmBendGoal = this.CreateCustomButton("Left Arm Bend Goal Button", this._bones, "L. Elbow Dir.");
                        cb = leftArmBendGoal.colors;
                        cb.normalColor = Color.Lerp(Color.blue, Color.white, 0.5f);
                        cb.highlightedColor = Color.Lerp(Color.blue, Color.white, 0.65f);
                        leftArmBendGoal.colors = cb;
                        leftArmBendGoal.onClick.AddListener(() => this.SetBendGoalTarget(FullBodyBipedChain.LeftArm));
                        t = leftArmBendGoal.GetComponentInChildren<Text>();
                        buttonRT = leftArmBendGoal.transform as RectTransform;
                        buttonRT.SetRect(new Vector2(0.625f, 1f), new Vector2(0.875f, 1f), new Vector2(0f, -70f), new Vector2(0f, -35f));
                        this._bendGoalsButtons[(int)FullBodyBipedChain.LeftArm] = leftArmBendGoal;
                        this._bendGoalsTexts[(int)FullBodyBipedChain.LeftArm] = t;

                        Button rightHand = this.CreateCustomButton("Right Hand Button", this._bones, "R. Hand");
                        cb = rightHand.colors;
                        cb.normalColor = Color.Lerp(Color.red, Color.white, 0.5f);
                        cb.highlightedColor = Color.Lerp(Color.red, Color.white, 0.65f);
                        rightHand.colors = cb;
                        rightHand.onClick.AddListener(() => this.SetBoneTarget(FullBodyBipedEffector.RightHand));
                        t = rightHand.GetComponentInChildren<Text>();
                        buttonRT = rightHand.transform as RectTransform;
                        buttonRT.SetRect(new Vector2(0f, 1f), new Vector2(0.25f, 1f), new Vector2(0f, -105f), new Vector2(0f, -70f));
                        this._effectorsButtons[(int)FullBodyBipedEffector.RightHand] = rightHand;
                        this._effectorsTexts[(int)FullBodyBipedEffector.RightHand] = t;

                        Button leftHand = this.CreateCustomButton("Left Hand Button", this._bones, "L. Hand");
                        cb = leftHand.colors;
                        cb.normalColor = Color.Lerp(Color.red, Color.white, 0.5f);
                        cb.highlightedColor = Color.Lerp(Color.red, Color.white, 0.65f);
                        leftHand.colors = cb;
                        leftHand.onClick.AddListener(() => this.SetBoneTarget(FullBodyBipedEffector.LeftHand));
                        t = leftHand.GetComponentInChildren<Text>();
                        buttonRT = leftHand.transform as RectTransform;
                        buttonRT.SetRect(new Vector2(0.75f, 1f), Vector2.one, new Vector2(0f, -105f), new Vector2(0f, -70f));
                        this._effectorsButtons[(int)FullBodyBipedEffector.LeftHand] = leftHand;
                        this._effectorsTexts[(int)FullBodyBipedEffector.LeftHand] = t;

                        Button body = this.CreateCustomButton("Body Button", this._bones, "Body");
                        cb = body.colors;
                        cb.normalColor = Color.Lerp(Color.red, Color.white, 0.5f);
                        cb.highlightedColor = Color.Lerp(Color.red, Color.white, 0.65f);
                        body.colors = cb;
                        body.onClick.AddListener(() => this.SetBoneTarget(FullBodyBipedEffector.Body));
                        t = body.GetComponentInChildren<Text>();
                        buttonRT = body.transform as RectTransform;
                        buttonRT.SetRect(new Vector2(0.375f, 1f), new Vector2(0.625f, 1f), new Vector2(0f, -140f), new Vector2(0f, -105f));
                        this._effectorsButtons[(int)FullBodyBipedEffector.Body] = body;
                        this._effectorsTexts[(int)FullBodyBipedEffector.Body] = t;

                        Button rightThigh = this.CreateCustomButton("Right Thigh Button", this._bones, "R. Thigh");
                        cb = rightThigh.colors;
                        cb.normalColor = Color.Lerp(Color.red, Color.white, 0.5f);
                        cb.highlightedColor = Color.Lerp(Color.red, Color.white, 0.65f);
                        rightThigh.colors = cb;
                        rightThigh.onClick.AddListener(() => this.SetBoneTarget(FullBodyBipedEffector.RightThigh));
                        t = rightThigh.GetComponentInChildren<Text>();
                        buttonRT = rightThigh.transform as RectTransform;
                        buttonRT.SetRect(new Vector2(0.25f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -175f), new Vector2(0f, -140f));
                        this._effectorsButtons[(int)FullBodyBipedEffector.RightThigh] = rightThigh;
                        this._effectorsTexts[(int)FullBodyBipedEffector.RightThigh] = t;

                        Button leftThigh = this.CreateCustomButton("Left Thigh Button", this._bones, "L. Thigh");
                        cb = leftThigh.colors;
                        cb.normalColor = Color.Lerp(Color.red, Color.white, 0.5f);
                        cb.highlightedColor = Color.Lerp(Color.red, Color.white, 0.65f);
                        leftThigh.colors = cb;
                        leftThigh.onClick.AddListener(() => this.SetBoneTarget(FullBodyBipedEffector.LeftThigh));
                        t = leftThigh.GetComponentInChildren<Text>();
                        buttonRT = leftThigh.transform as RectTransform;
                        buttonRT.SetRect(new Vector2(0.5f, 1f), new Vector2(0.75f, 1f), new Vector2(0f, -175f), new Vector2(0f, -140f));
                        this._effectorsButtons[(int)FullBodyBipedEffector.LeftThigh] = leftThigh;
                        this._effectorsTexts[(int)FullBodyBipedEffector.LeftThigh] = t;

                        Button rightLegBendGoal = this.CreateCustomButton("Right Leg Bend Goal Button", this._bones, "R. Knee Dir.");
                        cb = rightLegBendGoal.colors;
                        cb.normalColor = Color.Lerp(Color.blue, Color.white, 0.5f);
                        cb.highlightedColor = Color.Lerp(Color.blue, Color.white, 0.65f);
                        rightLegBendGoal.colors = cb;
                        rightLegBendGoal.onClick.AddListener(() => this.SetBendGoalTarget(FullBodyBipedChain.RightLeg));
                        t = rightLegBendGoal.GetComponentInChildren<Text>();
                        buttonRT = rightLegBendGoal.transform as RectTransform;
                        buttonRT.SetRect(new Vector2(0.125f, 1f), new Vector2(0.375f, 1f), new Vector2(0f, -210f), new Vector2(0f, -175f));
                        this._bendGoalsButtons[(int)FullBodyBipedChain.RightLeg] = rightLegBendGoal;
                        this._bendGoalsTexts[(int)FullBodyBipedChain.RightLeg] = t;

                        Button leftLegBendGoal = this.CreateCustomButton("Left Leg Bend Goal Button", this._bones, "L. Knee Dir.");
                        cb = leftLegBendGoal.colors;
                        cb.normalColor = Color.Lerp(Color.blue, Color.white, 0.5f);
                        cb.highlightedColor = Color.Lerp(Color.blue, Color.white, 0.65f);
                        leftLegBendGoal.colors = cb;
                        leftLegBendGoal.onClick.AddListener(() => this.SetBendGoalTarget(FullBodyBipedChain.LeftLeg));
                        t = leftLegBendGoal.GetComponentInChildren<Text>();
                        buttonRT = leftLegBendGoal.transform as RectTransform;
                        buttonRT.SetRect(new Vector2(0.625f, 1f), new Vector2(0.875f, 1f), new Vector2(0f, -210f), new Vector2(0f, -175f));
                        this._bendGoalsButtons[(int)FullBodyBipedChain.LeftLeg] = leftLegBendGoal;
                        this._bendGoalsTexts[(int)FullBodyBipedChain.LeftLeg] = t;

                        Button rightFoot = this.CreateCustomButton("Right Foot Button", this._bones, "R. Foot");
                        cb = rightFoot.colors;
                        cb.normalColor = Color.Lerp(Color.red, Color.white, 0.5f);
                        cb.highlightedColor = Color.Lerp(Color.red, Color.white, 0.65f);
                        rightFoot.colors = cb;
                        rightFoot.onClick.AddListener(() => this.SetBoneTarget(FullBodyBipedEffector.RightFoot));
                        t = rightFoot.GetComponentInChildren<Text>();
                        buttonRT = rightFoot.transform as RectTransform;
                        buttonRT.SetRect(new Vector2(0f, 1f), new Vector2(0.25f, 1f), new Vector2(0f, -245f), new Vector2(0f, -210f));
                        this._effectorsButtons[(int)FullBodyBipedEffector.RightFoot] = rightFoot;
                        this._effectorsTexts[(int)FullBodyBipedEffector.RightFoot] = t;

                        Button leftFoot = this.CreateCustomButton("Left Foot Button", this._bones, "L. Foot");
                        cb = leftFoot.colors;
                        cb.normalColor = Color.Lerp(Color.red, Color.white, 0.5f);
                        cb.highlightedColor = Color.Lerp(Color.red, Color.white, 0.65f);
                        leftFoot.colors = cb;
                        leftFoot.onClick.AddListener(() => this.SetBoneTarget(FullBodyBipedEffector.LeftFoot));
                        t = leftFoot.GetComponentInChildren<Text>();
                        buttonRT = leftFoot.transform as RectTransform;
                        buttonRT.SetRect(new Vector2(0.75f, 1f), Vector2.one, new Vector2(0f, -245f), new Vector2(0f, -210f));
                        this._effectorsButtons[(int)FullBodyBipedEffector.LeftFoot] = leftFoot;
                        this._effectorsTexts[(int)FullBodyBipedEffector.LeftFoot] = t;

                        {
                            RectTransform buttons = UIUtility.CreateNewUIObject(this._bones, "Buttons");
                            buttons.SetRect(Vector2.zero, new Vector2(0.5f, 1f), new Vector2(0f, 65f), new Vector2(0f, -245f));

                            Button xMoveButton = this.CreateCustomButton("X Move Button", buttons, "↑\nX\n↓");
                            cb = xMoveButton.colors;
                            cb.normalColor = Color.red;
                            cb.highlightedColor = Color.Lerp(Color.red, Color.white, 0.5f);
                            xMoveButton.colors = cb;
                            t = xMoveButton.GetComponentInChildren<Text>();
                            t.resizeTextMaxSize = (int)(t.fontSize * 1.2f);
                            xMoveButton.onClick.AddListener(() => EventSystem.current.SetSelectedGameObject(null));
                            xMoveButton.gameObject.AddComponent<PointerDownHandler>().onPointerDown += (eventData) =>
                            {
                                if (eventData.button == PointerEventData.InputButton.Left)
                                {
                                    this._xMove = true;
                                    this.SetNoControlCondition();
                                }
                            };
                            buttonRT = xMoveButton.transform as RectTransform;
                            buttonRT.SetRect(new Vector2(0f, 0.333f), new Vector2(0.333f, 1f), Vector2.zero, Vector2.zero);
                            this._positionButtons[0] = xMoveButton;

                            Button yMoveButton = this.CreateCustomButton("Y Move Button", buttons, "↑\nY\n↓");
                            cb = yMoveButton.colors;
                            cb.highlightedColor = Color.Lerp(Color.green, Color.white, 0.5f);
                            cb.normalColor = Color.green;
                            yMoveButton.colors = cb;
                            t = yMoveButton.GetComponentInChildren<Text>();
                            t.resizeTextMaxSize = (int)(t.fontSize * 1.2f);
                            yMoveButton.onClick.AddListener(() => EventSystem.current.SetSelectedGameObject(null));
                            yMoveButton.gameObject.AddComponent<PointerDownHandler>().onPointerDown += (eventData) =>
                            {
                                if (eventData.button == PointerEventData.InputButton.Left)
                                {
                                    this._yMove = true;
                                    this.SetNoControlCondition();
                                }
                            };
                            buttonRT = yMoveButton.transform as RectTransform;
                            buttonRT.SetRect(new Vector2(0.333f, 0.333f), new Vector2(0.666f, 1f), Vector2.zero, Vector2.zero);
                            this._positionButtons[1] = yMoveButton;

                            Button zMoveButton = this.CreateCustomButton("Z Move Button", buttons, "↑\nZ\n↓");
                            cb = zMoveButton.colors;
                            cb.highlightedColor = Color.Lerp(Color.blue, Color.white, 0.5f);
                            cb.normalColor = Color.blue;
                            zMoveButton.colors = cb;
                            t = zMoveButton.GetComponentInChildren<Text>();
                            t.resizeTextMaxSize = (int)(t.fontSize * 1.2f);
                            zMoveButton.onClick.AddListener(() => EventSystem.current.SetSelectedGameObject(null));
                            zMoveButton.gameObject.AddComponent<PointerDownHandler>().onPointerDown += (eventData) =>
                            {
                                if (eventData.button == PointerEventData.InputButton.Left)
                                {
                                    this._zMove = true;
                                    this.SetNoControlCondition();
                                }
                            };
                            buttonRT = zMoveButton.transform as RectTransform;
                            buttonRT.SetRect(new Vector2(0.666f, 0.333f), Vector2.one, Vector2.zero, Vector2.zero);
                            this._positionButtons[2] = zMoveButton;

                            Button rotXButton = this.CreateCustomButton("Rot X Button", buttons, "←   →\nRot X");
                            cb = rotXButton.colors;
                            cb.highlightedColor = Color.Lerp(Color.red, Color.white, 0.5f);
                            cb.normalColor = Color.red;
                            rotXButton.colors = cb;
                            t = rotXButton.GetComponentInChildren<Text>();
                            t.resizeTextMaxSize = (int)(t.fontSize * 1.2f);
                            rotXButton.onClick.AddListener(() => EventSystem.current.SetSelectedGameObject(null));
                            rotXButton.gameObject.AddComponent<PointerDownHandler>().onPointerDown += (eventData) =>
                            {
                                if (eventData.button == PointerEventData.InputButton.Left)
                                {
                                    this._xRot = true;
                                    this.SetNoControlCondition();
                                }
                            };
                            buttonRT = rotXButton.transform as RectTransform;
                            buttonRT.SetRect(Vector2.zero, new Vector2(0.333f, 0.333f), Vector2.zero, Vector2.zero);
                            this._rotationButtons[0] = rotXButton;

                            Button rotYButton = this.CreateCustomButton("Rot Y Button", buttons, "←   →\nRot Y");
                            cb = rotYButton.colors;
                            cb.highlightedColor = Color.Lerp(Color.green, Color.white, 0.5f);
                            cb.normalColor = Color.green;
                            rotYButton.colors = cb;
                            t = rotYButton.GetComponentInChildren<Text>();
                            t.resizeTextMaxSize = (int)(t.fontSize * 1.2f);
                            rotYButton.onClick.AddListener(() => EventSystem.current.SetSelectedGameObject(null));
                            rotYButton.gameObject.AddComponent<PointerDownHandler>().onPointerDown += (eventData) =>
                            {
                                if (eventData.button == PointerEventData.InputButton.Left)
                                {
                                    this._yRot = true;
                                    this.SetNoControlCondition();
                                }
                            };
                            buttonRT = rotYButton.transform as RectTransform;
                            buttonRT.SetRect(new Vector2(0.333f, 0f), new Vector2(0.666f, 0.333f), Vector2.zero, Vector2.zero);
                            this._rotationButtons[1] = rotYButton;

                            Button rotZButton = this.CreateCustomButton("Rot Z Button", buttons, "←   →\nRot Z");
                            cb = rotZButton.colors;
                            cb.highlightedColor = Color.Lerp(Color.blue, Color.white, 0.5f);
                            cb.normalColor = Color.blue;
                            rotZButton.colors = cb;
                            t = rotZButton.GetComponentInChildren<Text>();

                            t.resizeTextMaxSize = (int)(t.fontSize * 1.2f);
                            rotZButton.onClick.AddListener(() => EventSystem.current.SetSelectedGameObject(null));
                            rotZButton.gameObject.AddComponent<PointerDownHandler>().onPointerDown += (eventData) =>
                            {
                                if (eventData.button == PointerEventData.InputButton.Left)
                                {
                                    this._zRot = true;
                                    this.SetNoControlCondition();
                                }
                            };
                            buttonRT = rotZButton.transform as RectTransform;
                            buttonRT.SetRect(new Vector2(0.666f, 0f), new Vector2(1f, 0.333f), Vector2.zero, Vector2.zero);
                            this._rotationButtons[2] = rotZButton;
                        }

                        {
                            RectTransform otherButtons = UIUtility.CreateNewUIObject(this._bones, "Other buttons");
                            otherButtons.SetRect(new Vector2(0.5f, 0f), Vector2.one, new Vector2(2.5f, 74f), new Vector2(0f, -245f));


                            Button copyLeftArmButton = this.CreateCustomButton("Copy Right Arm Button", otherButtons, "Copy R. arm");
                            cb = copyLeftArmButton.colors;
                            cb.normalColor = Color.Lerp(UIUtility.purpleColor, Color.black, 0.5f);
                            cb.highlightedColor = UIUtility.purpleColor;
                            copyLeftArmButton.colors = cb;
                            copyLeftArmButton.onClick.AddListener(() =>
                            {
                                if (this._manualBoneTarget != null)
                                    this._manualBoneTarget.CopyLimbToTwin(FullBodyBipedChain.RightArm);
                            });
                            buttonRT = copyLeftArmButton.transform as RectTransform;
                            buttonRT.SetRect(new Vector2(0f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -32.5f), new Vector2(-1.25f, -0f));

                            Button copyRightArmButton = this.CreateCustomButton("Copy Left Arm Button", otherButtons, "Copy L. arm");
                            cb = copyRightArmButton.colors;
                            cb.normalColor = Color.Lerp(UIUtility.purpleColor, Color.black, 0.5f);
                            cb.highlightedColor = UIUtility.purpleColor;
                            copyRightArmButton.colors = cb;
                            copyRightArmButton.onClick.AddListener(() =>
                            {
                                if (this._manualBoneTarget != null)
                                    this._manualBoneTarget.CopyLimbToTwin(FullBodyBipedChain.LeftArm);
                            });
                            buttonRT = copyRightArmButton.transform as RectTransform;
                            buttonRT.SetRect(new Vector2(0.5f, 1f), Vector2.one, new Vector2(1.25f, -32.5f), new Vector2(-0f, -0f));

                            Button copyLeftLegButton = this.CreateCustomButton("Copy Right Leg Button", otherButtons, "Copy R. leg");
                            cb = copyLeftLegButton.colors;
                            cb.normalColor = Color.Lerp(UIUtility.purpleColor, Color.black, 0.5f);
                            cb.highlightedColor = UIUtility.purpleColor;
                            copyLeftLegButton.colors = cb;
                            copyLeftLegButton.onClick.AddListener(() =>
                            {
                                if (this._manualBoneTarget != null)
                                    this._manualBoneTarget.CopyLimbToTwin(FullBodyBipedChain.RightLeg);
                            });
                            buttonRT = copyLeftLegButton.transform as RectTransform;
                            buttonRT.SetRect(new Vector2(0f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -62.5f), new Vector2(-1.25f, -32.5f));

                            Button copyRightLegButton = this.CreateCustomButton("Copy Left LegButton", otherButtons, "Copy L. leg");
                            cb = copyRightLegButton.colors;
                            cb.normalColor = Color.Lerp(UIUtility.purpleColor, Color.black, 0.5f);
                            cb.highlightedColor = UIUtility.purpleColor;
                            copyRightLegButton.colors = cb;
                            copyRightLegButton.onClick.AddListener(() =>
                            {
                                if (this._manualBoneTarget != null)
                                    this._manualBoneTarget.CopyLimbToTwin(FullBodyBipedChain.LeftLeg);
                            });
                            buttonRT = copyRightLegButton.transform as RectTransform;
                            buttonRT.SetRect(new Vector2(0.5f, 1f), Vector2.one, new Vector2(1.25f, -62.5f), new Vector2(-0f, -32.5f));
                        }

                        {
                            Image experimental = UIUtility.CreatePanel("Experimental Features", this._bones);
                            experimental.color = UIUtility.whiteColor;
                            experimental.rectTransform.SetRect(new Vector2(0.5f, 0f), Vector2.one, new Vector2(2.5f, 65f), new Vector2(0f, -310f));

                            Image experimentalHeader = UIUtility.CreatePanel("Header", experimental.rectTransform);
                            experimentalHeader.color = UIUtility.purpleColor;
                            experimentalHeader.rectTransform.SetRect(new Vector2(0f, 1f), Vector2.one, new Vector2(2.5f, -26.5f), new Vector2(-2.5f, -2.5f));

                            Text headerText = this.CreateCustomText("Header Text", experimentalHeader.transform, "Experimental Features");
                            headerText.alignment = TextAnchor.MiddleCenter;
                            headerText.fontStyle = FontStyle.Bold;
                            headerText.rectTransform.SetRect(Vector2.zero, Vector2.one, new Vector2(2f, 2f), new Vector2(-2f, -2f));
                            headerText.color = Color.white;

                            Button advancedModeButton = this.CreateCustomButton("Advanced Mode Button", experimental.rectTransform, "Advanced mode");
                            cb = advancedModeButton.colors;
                            cb.normalColor = Color.Lerp(UIUtility.purpleColor, Color.black, 0.5f);
                            cb.highlightedColor = UIUtility.purpleColor;
                            advancedModeButton.colors = cb;
                            advancedModeButton.onClick.AddListener(this.ToggleAdvancedMode);
                            buttonRT = advancedModeButton.transform as RectTransform;
                            buttonRT.SetRect(new Vector2(0f, 1f), Vector2.one, new Vector2(2.5f, -54f), new Vector2(-2.5f, -29f));
                        }
                        {
                            RectTransform sliderContainer = UIUtility.CreateNewUIObject(this._bones, "Slider Container");
                            sliderContainer.SetRect(Vector2.zero, new Vector2(1f, 0f), new Vector2(0f, 40f), new Vector2(0f, 60f));

                            Text movIntensityTxt = this.CreateCustomText("Movement Intensity Text", sliderContainer, "Mvt. Intensity");
                            movIntensityTxt.alignment = TextAnchor.MiddleLeft;
                            movIntensityTxt.rectTransform.SetRect(Vector2.zero, new Vector2(0.333f, 1f), Vector2.zero, Vector2.zero);

                            this._movementIntensity = UIUtility.CreateScrollbar("Movement Intensity Slider", sliderContainer);
                            this._movementIntensity.size = 0f;
                            this._movementIntensity.numberOfSteps = 15;
                            this._movementIntensity.value = 0.5f;
                            this._movementIntensity.onValueChanged.AddListener(value =>
                            {
                                value *= 14;
                                value -= 7;
                                this._intensityValue = Mathf.Pow(2, value);
                                this._intensityValueText.text = this._intensityValue >= 1f ? "x" + this._intensityValue.ToString("0.##") : "/" + (1f / this._intensityValue).ToString("0.##");
                            });
                            this._movementIntensity.gameObject.AddComponent<PointerDownHandler>().onPointerDown += (eventData) =>
                            {
                                this.SetNoControlCondition();
                                this._blockCamera = true;
                            };
                            RectTransform rt = this._movementIntensity.transform as RectTransform;
                            RectTransform handle = (rt.GetChild(0).GetChild(0) as RectTransform);
                            handle.sizeDelta = new Vector2(14f, handle.sizeDelta.y);
                            rt.SetRect(new Vector2(0.333f, 0f), new Vector2(0.9f, 1f), new Vector2(0f, 3f), new Vector2(0f, -3f));

                            this._intensityValueText = this.CreateCustomText("Movement Intensity Value", sliderContainer, "x1");
                            this._intensityValueText.alignment = TextAnchor.MiddleCenter;
                            this._intensityValueText.resizeTextMaxSize = 14;
                            this._intensityValueText.rectTransform.SetRect(new Vector2(0.9f, 0f), Vector2.one, Vector2.zero, Vector2.zero);

                            RectTransform buttonContainer = UIUtility.CreateNewUIObject(this._bones, "Button Container");
                            buttonContainer.SetRect(Vector2.zero, new Vector2(0.75f, 0f), new Vector2(0f, 20f), new Vector2(0f, 40f));

                            Text positionOpLabel = this.CreateCustomText("Position Operation Label", buttonContainer, "Pos. Operation");
                            positionOpLabel.alignment = TextAnchor.MiddleLeft;
                            positionOpLabel.rectTransform.SetRect(Vector2.zero, new Vector2(0.4444f, 1f), Vector2.zero, Vector2.zero);

                            Button positionOp = this.CreateCustomButton("Position Operation", buttonContainer, "World");
                            Text buttonText = positionOp.GetComponentInChildren<Text>();
                            buttonText.alignment = TextAnchor.MiddleCenter;
                            buttonText.resizeTextMaxSize = 14;
                            positionOp.onClick.AddListener(() =>
                            {
                                this._positionOperationWorld = !this._positionOperationWorld;
                                buttonText.text = this._positionOperationWorld ? "World" : "Local";
                            });
                            buttonRT = positionOp.transform as RectTransform;
                            buttonRT.SetRect(new Vector2(0.4444f, 0f), Vector2.one, Vector2.zero, new Vector2(-50f, 0f));

                            RectTransform checkboxContainer = UIUtility.CreateNewUIObject(this._bones, "Button Container");
                            checkboxContainer.SetRect(Vector2.zero, new Vector2(0.75f, 0f), Vector2.zero, new Vector2(0f, 20f));

                            Text optimizeIKLabel = this.CreateCustomText("Optimize IK Label", checkboxContainer, "Optimize IK");
                            optimizeIKLabel.alignment = TextAnchor.MiddleLeft;
                            optimizeIKLabel.rectTransform.SetRect(Vector2.zero, new Vector2(0.4444f, 1f), Vector2.zero, Vector2.zero);

                            this._optimizeIKToggle = UIUtility.AddCheckboxToObject(UIUtility.CreateNewUIObject(checkboxContainer, "Optimize IK"));
                            this._optimizeIKToggle.onValueChanged.AddListener((b) =>
                            {
                                if (this._manualBoneTarget != null)
                                    this._manualBoneTarget.optimizeIK = this._optimizeIKToggle.isOn;
                            });
                            buttonRT = this._optimizeIKToggle.transform as RectTransform;
                            buttonRT.SetRect(new Vector2(0.4444f, 0f), new Vector2(0.4444f, 1f), Vector2.zero, new Vector2(20f, 0f));

                        }
                    }
                }

                {
                    Button optionsButton = this.CreateCustomButton("Options Button", bg.transform, "Options");
                    RectTransform rt = optionsButton.transform as RectTransform;
                    rt.SetRect(new Vector2(0.75f, 0f), new Vector2(1f, 0f), new Vector2(0f, 5f), new Vector2(-5f, 25f));
                    optionsButton.onClick.AddListener(() =>
                    {
                        if (this._shortcutRegisterMode)
                            this._shortcutKeyButton.onClick.Invoke();
                        this._optionsWindow.gameObject.SetActive(!this._optionsWindow.gameObject.activeSelf);
                    });
                }

            }
            this._ui.gameObject.SetActive(false);

            this.SetBoneTarget(FullBodyBipedEffector.Body);
            this.OnTargetChange(null);

            this._optionsWindow = UIUtility.CreatePanel("Options Window", this._ui.transform).rectTransform;
            this._optionsWindow.GetComponent<Image>().color = UIUtility.whiteColor;
            this._optionsWindow.SetRect(Vector2.zero, Vector2.zero, new Vector2(390f, 3f), new Vector2(620f, 243f));
            this._optionsWindow.anchoredPosition = new Vector2(558f, 192f);
            {

                {
                    Image topContainer = UIUtility.CreatePanel("Top Container", this._optionsWindow);
                    topContainer.color = UIUtility.grayColor;
                    topContainer.rectTransform.SetRect(new Vector2(0f, 1f), Vector2.one, new Vector2(4f, -28f), new Vector2(-4f, -4f));
                    topContainer.gameObject.AddComponent<MovableWindow>().toDrag = this._optionsWindow;
                    MovableWindow mw = UIUtility.MakeObjectDraggable(topContainer.rectTransform, this._optionsWindow, false);
                    mw.onPointerDown += this.OnWindowStartDrag;
                    mw.onDrag += this.OnWindowDrag;
                    mw.onPointerUp += this.OnWindowEndDrag;

                    Text titleText = this.CreateCustomText("Title Text", topContainer.transform, "Options");
                    titleText.alignment = TextAnchor.MiddleCenter;
                    titleText.fontStyle = FontStyle.Bold;
                    titleText.rectTransform.SetRect(Vector2.zero, Vector2.one, new Vector2(2f, 2f), new Vector2(-2f, -2f));
                    titleText.color = Color.white;
                }

                {
                    RectTransform options = UIUtility.CreateNewUIObject(this._optionsWindow, "Options");
                    options.SetRect(Vector2.zero, Vector2.one, new Vector2(5f, 5f), new Vector2(-5f, -31f));
                    {
                        {
                            RectTransform scaleContainer = UIUtility.CreateNewUIObject(options, "Scale Container");
                            scaleContainer.SetRect(new Vector2(0f, 1f), Vector2.one, new Vector2(0f, -20f), Vector2.zero);

                            Text label = this.CreateCustomText("Label", scaleContainer, "UI Scale (x" + this.uiScale.ToString("0.0") + ")");
                            label.rectTransform.SetRect(Vector2.zero, new Vector2(0.75f, 1f), Vector2.zero, Vector2.zero);
                            label.alignment = TextAnchor.MiddleLeft;

                            Button minusButton = this.CreateCustomButton("Minus Button", scaleContainer, "-");
                            (minusButton.transform as RectTransform).SetRect(new Vector2(0.75f, 0f), new Vector2(0.875f, 1f), Vector2.zero, Vector2.zero);
                            minusButton.onClick.AddListener(() =>
                            {
                                this.uiScale = Mathf.Clamp(this.uiScale - 0.1f, 0.5f, 2f);
                                this._ui.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920 / this.uiScale, 1080 / this.uiScale);
                                label.text = "UI Scale (x" + this.uiScale.ToString("0.0") + ")";
                            });
                            Text t = minusButton.GetComponentInChildren<Text>();
                            t.rectTransform.SetRect(Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                            t.fontStyle = FontStyle.Bold;

                            Button plusButton = this.CreateCustomButton("Plus Button", scaleContainer, "+");
                            (plusButton.transform as RectTransform).SetRect(new Vector2(0.875f, 0f), Vector2.one, Vector2.zero, Vector2.zero);
                            plusButton.onClick.AddListener(() =>
                            {
                                this.uiScale = Mathf.Clamp(this.uiScale + 0.1f, 0.5f, 2f);
                                this._ui.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920 / this.uiScale, 1080 / this.uiScale);
                                label.text = "UI Scale (x" + this.uiScale.ToString("0.0") + ")";
                            });
                            t = plusButton.GetComponentInChildren<Text>();
                            t.rectTransform.SetRect(Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                            t.fontStyle = FontStyle.Bold;
                        }

                        {
                            RectTransform advWindowSizeContainer = UIUtility.CreateNewUIObject(options, "Advanced Mode Window Size Container");
                            advWindowSizeContainer.SetRect(new Vector2(0f, 1f), Vector2.one, new Vector2(0f, -60f), new Vector2(0f, -20f));


                            Text label = this.CreateCustomText("Label", advWindowSizeContainer, "Adv. mode win. size");
                            label.rectTransform.SetRect(Vector2.zero, new Vector2(0.333f, 1f), Vector2.zero, Vector2.zero);
                            label.alignment = TextAnchor.MiddleLeft;

                            Button normalButton = this.CreateCustomButton("Normal Button", advWindowSizeContainer, "Normal");
                            (normalButton.transform as RectTransform).SetRect(new Vector2(0.333f, 0f), new Vector2(0.555f, 1f), Vector2.zero, Vector2.zero);
                            normalButton.onClick.AddListener(() =>
                            {
                                this._advancedModeWindowSize = 0;
                                Rect r = this._advancedModeRects[this._advancedModeWindowSize];
                                this._advancedModeRect.xMin = this._advancedModeRect.xMax - r.width;
                                this._advancedModeRect.width = r.width;
                                this._advancedModeRect.yMin = this._advancedModeRect.yMax - r.height;
                                this._advancedModeRect.height = r.height;
                            });

                            Button plusButton = this.CreateCustomButton("Large Button", advWindowSizeContainer, "Large");
                            (plusButton.transform as RectTransform).SetRect(new Vector2(0.555f, 0f), new Vector2(0.777f, 1f), Vector2.zero, Vector2.zero);
                            plusButton.onClick.AddListener(() =>
                            {
                                this._advancedModeWindowSize = 1;
                                Rect r = this._advancedModeRects[this._advancedModeWindowSize];
                                this._advancedModeRect.xMin = this._advancedModeRect.xMax - r.width;
                                this._advancedModeRect.width = r.width;
                                this._advancedModeRect.yMin = this._advancedModeRect.yMax - r.height;
                                this._advancedModeRect.height = r.height;
                            });

                            Button veryLargeButton = this.CreateCustomButton("Very Large Button", advWindowSizeContainer, "Very large");
                            (veryLargeButton.transform as RectTransform).SetRect(new Vector2(0.777f, 0f), Vector2.one, Vector2.zero, Vector2.zero);
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
                        }

                        {
                            RectTransform shortcutKey = UIUtility.CreateNewUIObject(options, "Shortcut Key Container");
                            shortcutKey.SetRect(new Vector2(0f, 1f), Vector2.one, new Vector2(0f, -90f), new Vector2(0f, -60f));

                            Text label = this.CreateCustomText("Label", shortcutKey, "Shortcut Key");
                            label.rectTransform.SetRect(Vector2.zero, new Vector2(0.5f, 1f), Vector2.zero, Vector2.zero);
                            label.alignment = TextAnchor.MiddleLeft;

                            this._shortcutKeyButton = this.CreateCustomButton("Listener Button", shortcutKey, this._mainWindowKeyCode.ToString());
                            (this._shortcutKeyButton.transform as RectTransform).SetRect(new Vector2(0.5f, 0f), Vector2.one, Vector2.zero, Vector2.zero);
                            Text text = this._shortcutKeyButton.GetComponentInChildren<Text>();
                            this._shortcutKeyButton.onClick.AddListener(() =>
                            {
                                this._shortcutRegisterMode = !this._shortcutRegisterMode;
                                text.text = this._shortcutRegisterMode ? "Press a Key" : this._mainWindowKeyCode.ToString();
                            });
                        }
                    }
                }
            }
            this._optionsWindow.gameObject.SetActive(false);
            LayoutRebuilder.ForceRebuildLayoutImmediate(this._ui.transform.GetChild(0).transform as RectTransform);
        }

        private Button CreateCustomButton(string objectName = "New Button", Transform parent = null, string buttonText = "Button")
        {
            Button b = UIUtility.CreateButton(objectName, parent, buttonText);
            b.colors = new ColorBlock()
            {
                colorMultiplier = 1f,
                normalColor = UIUtility.lightGrayColor,
                highlightedColor = UIUtility.greenColor,
                pressedColor = UIUtility.lightGreenColor,
                disabledColor = UIUtility.transparentGrayColor,
                fadeDuration = b.colors.fadeDuration
            };
            Text t = b.GetComponentInChildren<Text>();
            t.color = UIUtility.whiteColor;
            UIUtility.AddOutlineToObject(t.transform);
            return b;
        }

        private Text CreateCustomText(string objectName = "New Text", Transform parent = null, string textText = "Text")
        {
            Text t = UIUtility.CreateText(objectName, parent, textText);
            t.color = UIUtility.whiteColor;
            UIUtility.AddOutlineToObject(t.transform);
            return t;
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
                this._boneTargets.Contains(FullBodyBipedEffector.Body) ||
                this._boneTargets.Contains(FullBodyBipedEffector.LeftShoulder) ||
                this._boneTargets.Contains(FullBodyBipedEffector.LeftThigh) ||
                this._boneTargets.Contains(FullBodyBipedEffector.RightShoulder) ||
                this._boneTargets.Contains(FullBodyBipedEffector.RightThigh))
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
            if (this._manualBoneTarget != null)
                this._manualBoneTarget.drawAdvancedMode = !this._manualBoneTarget.drawAdvancedMode;
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
                if (this._manualBoneTarget != null && this._manualBoneTarget.drawAdvancedMode && this._mouseInAdvMode)
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

            if (this._manualBoneTarget != null)
            {
                this.ResetBoneButtons();
                bool shouldReset = false;
                for (int i = 0; i < this._boneTargets.Count; i++)
                {
                    FullBodyBipedEffector bone = this._boneTargets[i];
                    if (this._manualBoneTarget.IsPartEnabled(bone) == false)
                    {
                        this._boneTargets.RemoveAt(i);
                        --i;
                        shouldReset = true;
                    }
                }

                for (int i = 0; i < this._bendGoalTargets.Count; i++)
                {
                    FullBodyBipedChain bendGoal = this._bendGoalTargets[i];
                    if (this._manualBoneTarget.IsPartEnabled(bendGoal) == false)
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
                    this._effectorsButtons[i].interactable = this._manualBoneTarget.IsPartEnabled((FullBodyBipedEffector)i);

                for (int i = 0; i < this._bendGoalsButtons.Length; i++)
                    this._bendGoalsButtons[i].interactable = this._manualBoneTarget.IsPartEnabled((FullBodyBipedChain)i);
            }

            if (this._xMove || this._yMove || this._zMove || this._xRot || this._yRot || this._zRot)
            {
                this._delta += new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")) / (10f * (Input.GetMouseButton(1) ? 2f : 1f));
                if (this._manualBoneTarget != null)
                {
                    if (this._manualBoneTarget.currentDragType == ManualBoneController.DragType.None)
                        this._manualBoneTarget.StartDrag(this._xMove || this._yMove || this._zMove ? ManualBoneController.DragType.Position : ManualBoneController.DragType.Rotation);
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
                            this._manualBoneTarget.SetBoneTargetPosition(this._boneTargets[i], newPosition, this._positionOperationWorld);
                        if (changeRotation)
                            this._manualBoneTarget.SetBoneTargetRotation(this._boneTargets[i], newRotation);
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
                        this._manualBoneTarget.SetBendGoalPosition(this._bendGoalTargets[i], newPosition, this._positionOperationWorld);
                    }
                }
            }
            else
            {
                this._delta = Vector2.zero;
                if (this._manualBoneTarget != null)
                {
                    if (this._manualBoneTarget.currentDragType != ManualBoneController.DragType.None)
                        this._manualBoneTarget.StopDrag();
                    for (int i = 0; i < this._boneTargets.Count; ++i)
                    {
                        this._lastBonesPositions[i] = this._manualBoneTarget.GetBoneTargetPosition(this._boneTargets[i], this._positionOperationWorld);
                        this._lastBonesRotations[i] = this._manualBoneTarget.GetBoneTargetRotation(this._boneTargets[i]);
                    }
                    for (int i = 0; i < this._bendGoalTargets.Count; ++i)
                        this._lastBendGoalsPositions[i] = this._manualBoneTarget.GetBendGoalPosition(this._bendGoalTargets[i], this._positionOperationWorld);
                }
            }

            if (this._shortcutRegisterMode)
            {
                foreach (KeyCode kc in this._possibleKeyCodes)
                {
                    if (Input.GetKeyDown(kc))
                    {
                        if (kc != KeyCode.Escape && kc != KeyCode.Return)
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
        #endregion

        #region Private Methods
        private void CallPostUpdate()
        {
            this.onPostUpdate?.Invoke();
        }

        private IEnumerator OnSceneSavedShortcut()
        {
            if ((DateTime.Now - Directory.GetFiles(UserData.Create("studioneo/scene"), "*.png").Max(f => File.GetLastWriteTime(f))).TotalSeconds > 1.5f)
            {
                int count = Directory.GetFiles(UserData.Create("studioneo/scene"), "*.png").Length;
                yield return new WaitUntil(() => count != Directory.GetFiles(UserData.Create("studioneo/scene"), "*.png").Length);
            }
            this.OnSceneSave();
        }

        private void OnObjectAdded()
        {
            int i = 0;
            foreach (KeyValuePair<int, Studio.ObjectCtrlInfo> kvp in Studio.Studio.Instance.dicObjectCtrl)
            {
                if (kvp.Key >= this._lastIndex)
                {
                    Studio.OCIChar ociChar = kvp.Value as Studio.OCIChar;
                    if (ociChar != null && ociChar.charInfo.gameObject.GetComponent<ManualBoneController>() == null)
                    {
                        ManualBoneController controller = ociChar.charInfo.gameObject.AddComponent<ManualBoneController>();
                        controller.chara = ociChar;
                        if (this._charactersAddedByCopy && i < this._copySources.Count)
                        {
                            controller.LoadFrom(this._copySources[i]);
                            ++i;
                        }
                    }
                }
            }
        }

        private void OnTargetChange(ManualBoneController last)
        {
            if (last != null)
                last.drawAdvancedMode = false;
            if (this._manualBoneTarget == null)
            {
                this._nothingText.gameObject.SetActive(true);
                this._controls.gameObject.SetActive(false);
            }
            else
            {
                this._optimizeIKToggle.isOn = this._manualBoneTarget.optimizeIK;
                this._nothingText.gameObject.SetActive(false);
                this._controls.gameObject.SetActive(true);
            }
        }

        private void OnDuplicate()
        {
            Studio.TreeNodeObject[] selectedNodes = Studio.Studio.Instance.treeNodeCtrl.selectNodes;
            foreach (Studio.TreeNodeObject obj in selectedNodes)
            {
                Studio.ObjectCtrlInfo info;
                if (Studio.Studio.Instance.dicInfo.TryGetValue(obj, out info))
                {
                    this._charactersAddedByCopy = true;
                    this._copySources.Add((info as Studio.OCIChar).charInfo.gameObject.GetComponent<ManualBoneController>());
                }
            }

        }

        [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
        private bool CameraControllerCondition()
        {
            return this._blockCamera || this._xMove || this._yMove || this._zMove || this._xRot || this._yRot || this._zRot || this._mouseInAdvMode || this._windowMoving || (this._manualBoneTarget != null && this._manualBoneTarget.isDraggingDynamicBone);
        }
        #endregion

        #region Saves
        private void LoadCanvasCreated()
        {
            this.StartCoroutine(this.LoadCanvasCreated_Routine());
        }

        private IEnumerator LoadCanvasCreated_Routine()
        {
            yield return null;
            yield return null;
            yield return null;
            yield return null;
            yield return null;
            yield return null;
            yield return null;
            yield return null;
            GameObject.Find("SceneLoadScene").transform.FindChild("Canvas Load Work/Button Load").GetComponent<Button>().onClick.AddListener(this.OnSceneLoad);
            GameObject.Find("SceneLoadScene").transform.FindChild("Canvas Load Work/Button Import").GetComponent<Button>().onClick.AddListener(this.OnSceneImport);
            GameObject.Find("SceneLoadScene").transform.FindChild("Canvas Load Work/Button Delete").GetComponent<Button>().onClick.AddListener(this.OnSceneMayDelete);
        }

        private void OnSceneLoad()
        {
            this.OnSceneLoad(FindObjectOfType<Studio.SceneLoadScene>().GetChosenScenePath());
        }

        private void OnSceneLoad(string scenePath)
        {
            this._lastObjectCount = 0;
            scenePath = Path.GetFileNameWithoutExtension(scenePath) + ".sav";
            string dir = _pluginDir + _studioSavesDir;
            string path = dir + scenePath;
            if (File.Exists(path) == false)
                return;
            XmlDocument doc = new XmlDocument();
            doc.Load(path);
            this.LoadDefaultVersion(doc);
        }

        private void OnSceneImport()
        {
            this.OnSceneImport(FindObjectOfType<Studio.SceneLoadScene>().GetChosenScenePath());
        }

        private void OnSceneImport(string scenePath)
        {
            scenePath = Path.GetFileNameWithoutExtension(scenePath) + ".sav";
            string dir = _pluginDir + _studioSavesDir;
            string path = dir + scenePath;
            if (File.Exists(path) == false)
                return;
            XmlDocument doc = new XmlDocument();
            doc.Load(path);
            this.LoadDefaultVersion(doc, this._lastIndex);
        }

        private void OnSceneMayDelete()
        {
            this.StartCoroutine(this.OnSceneMayDelete_Routine());
        }

        private IEnumerator OnSceneMayDelete_Routine()
        {
            yield return null;
            this._selectedScenePath = FindObjectOfType<Studio.SceneLoadScene>().GetChosenScenePath();
            GameObject.Find("CheckScene").transform.FindChild("Canvas/Panel/Yes").GetComponent<Button>().onClick.AddListener(this.OnSceneDelete);
        }

        private void OnSceneDelete()
        {
            this.OnSceneDelete(this._selectedScenePath);
        }

        private void OnSceneDelete(string scenePath)
        {
            string completePath = _pluginDir + _studioSavesDir + Path.GetFileNameWithoutExtension(scenePath) + ".sav";
            if (File.Exists(completePath))
                File.Delete(completePath);
        }

        private void OnSceneSave()
        {
            this.OnSceneSave(this.GetLastScenePath());
        }

        private void OnSceneSave(string scenePath)
        {
            string saveFileName = Path.GetFileNameWithoutExtension(scenePath) + ".sav";
            string dir = _pluginDir + _studioSavesDir;
            if (Directory.Exists(dir) == false)
                Directory.CreateDirectory(dir);
            int written = 0;
            string path = dir + saveFileName;
            using (FileStream fileStream = new FileStream(path, FileMode.Create, FileAccess.Write))
            {
                using (XmlTextWriter xmlWriter = new XmlTextWriter(fileStream, Encoding.UTF8))
                {
                    xmlWriter.Formatting = Formatting.Indented;
                    xmlWriter.WriteStartElement("root");
                    xmlWriter.WriteAttributeString("version", HSPE.VersionNum);
                    SortedDictionary<int, Studio.ObjectCtrlInfo> dic = new SortedDictionary<int, Studio.ObjectCtrlInfo>(Studio.Studio.Instance.dicObjectCtrl);
                    foreach (KeyValuePair<int, Studio.ObjectCtrlInfo> kvp in dic)
                    {
                        Studio.OCIChar ociChar = kvp.Value as Studio.OCIChar;
                        if (ociChar != null)
                        {
                            xmlWriter.WriteStartElement("characterInfo");
                            xmlWriter.WriteAttributeString("name", ociChar.charInfo.customInfo.name);
                            xmlWriter.WriteAttributeString("index", XmlConvert.ToString(kvp.Key));
                            ManualBoneController controller = ociChar.charInfo.gameObject.GetComponent<ManualBoneController>();
                            if (controller.optimizeIK == false)
                            {
                                xmlWriter.WriteAttributeString("optimizeIK", XmlConvert.ToString(controller.optimizeIK));
                                written++;
                            }
                            written += controller.SaveXml(xmlWriter);
                            xmlWriter.WriteEndElement();
                        }
                    }
                    xmlWriter.WriteEndElement();
                }
            }
            if (written == 0)
                File.Delete(path);
        }

        private string GetLastScenePath()
        {
            List<KeyValuePair<DateTime, string>> list = (from s in Directory.GetFiles(UserData.Create("studioneo/scene"), "*.png")
                                                         select new KeyValuePair<DateTime, string>(File.GetLastWriteTime(s), s)).ToList();
            CultureInfo currentCulture = Thread.CurrentThread.CurrentCulture;
            Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("ja-JP");
            list.Sort((a, b) => b.Key.CompareTo(a.Key));
            Thread.CurrentThread.CurrentCulture = currentCulture;
            return (from v in list
                    select v.Value).ToList()[0];
        }

        private void LoadDefaultVersion(XmlDocument xmlDoc, int lastIndex = 0)
        {
            if (xmlDoc.DocumentElement == null || xmlDoc.DocumentElement.Name != "root")
                return;
            string v = xmlDoc.DocumentElement.GetAttribute("version");
            SortedDictionary<int, Studio.ObjectCtrlInfo> sortedCharaDic = new SortedDictionary<int, Studio.ObjectCtrlInfo>(Studio.Studio.Instance.dicObjectCtrl);

            foreach (XmlNode node in xmlDoc.DocumentElement.ChildNodes)
            {
                switch (node.Name)
                {
                    case "characterInfo":
                        foreach (KeyValuePair<int, Studio.ObjectCtrlInfo> kvp in sortedCharaDic)
                        {
                            if (kvp.Key >= lastIndex)
                            {
                                Studio.OCIChar ociChar = kvp.Value as Studio.OCIChar;
                                if (ociChar != null && ociChar.charInfo.gameObject.GetComponent<ManualBoneController>() == null)
                                {
                                    ManualBoneController controller = ociChar.charInfo.gameObject.AddComponent<ManualBoneController>();
                                    if (node.Attributes?["optimizeIK"] != null)
                                        controller.optimizeIK = XmlConvert.ToBoolean(node.Attributes["optimizeIK"].Value);
                                    else
                                        controller.optimizeIK = true;
                                    controller.chara = ociChar;
                                    controller.ScheduleLoad(node, v);
                                    break;
                                }
                            }
                        }
                        break;
                }
            }
        }
        #endregion
    }
}
