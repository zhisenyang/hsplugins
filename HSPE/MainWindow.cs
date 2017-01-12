using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml;
using Manager;
using RootMotion.Demos;
using RootMotion.FinalIK;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HSPE
{
    public class MainWindow : MonoBehaviour
    {
        #region Private Constants
        private const string _config = "config.xml";
        private const string _pluginDir = "Plugins\\HSPE\\";
        private const string _studioSavesDir = "StudioScenes\\";
        #endregion

        #region Private Variables
        private CameraControl _cameraController;
        private ManualBoneController _manualBoneTarget;
        private readonly List<FullBodyBipedEffector> _boneTargets = new List<FullBodyBipedEffector>();
        private readonly List<Vector3> _lastBonesPositions = new List<Vector3>();
        private readonly List<Quaternion> _lastBonesRotations = new List<Quaternion>();
        private readonly List<FullBodyBipedChain> _bendGoalTargets = new List<FullBodyBipedChain>();
        private readonly List<Vector3> _lastBendGoalsPositions = new List<Vector3>();
        private string _selectedScenePath = "";
        private int _femaleIndexOffset;
        private int _maleIndexOffset;
        private Vector2 _delta;
        private bool _xMove;
        private bool _yMove;
        private bool _zMove;
        private bool _xRot;
        private bool _yRot;
        private bool _zRot;
        private bool _blockCamera = false;
        private bool _isVisible = false;
        private Rect _advancedModeRect = new Rect(Screen.width - 650, Screen.height - 330, 650, 330);
        private Canvas _ui;
        private Text _nothingText;
        private RectTransform _controls;
        private RectTransform _bones;
        private Toggle _advancedModeToggle;
        private Scrollbar _movementIntensity;
        private Text _intensityValueText;
        private float _intensityValue = 1f;
        private readonly Button[] _effectorsButtons = new Button[9];
        private readonly Button[] _bendGoalsButtons = new Button[4];
        private readonly Button[] _rotationButtons = new Button[3];
        private Toggle _forceBendGoalsToggle;
        #endregion

        #region Unity Methods

        protected virtual void Awake()
        {
            if (GameObject.Find("LoadCanvas"))
            {
                GameObject.Find("LoadCanvas").transform.FindChild("LoadBGPanel").FindChild("LoadSelectPanel").FindChild("BGImage").FindChild("LoadButton").GetComponent<Button>().onClick.AddListener(this.OnSceneLoad);
                GameObject.Find("LoadCanvas").transform.FindChild("LoadBGPanel").FindChild("LoadSelectPanel").FindChild("BGImage").FindChild("ImportButton").GetComponent<Button>().onClick.AddListener(this.OnSceneImport);
                GameObject.Find("LoadCanvas").transform.FindChild("LoadBGPanel").FindChild("LoadSelectPanel").FindChild("BGImage").FindChild("DeleteButton").GetComponent<Button>().onClick.AddListener(this.OnSceneMayDelete);
                foreach (UI_PngView p in GameObject.Find("LoadCanvas").transform.FindChild("LoadBGPanel").FindChild("Images").GetComponentsInChildren<UI_PngView>())
                    p.GetComponent<Button>().onClick.AddListener(() => this._selectedScenePath = FindObjectOfType<PngViewManager>().selectPngPath);
            }
            if (GameObject.Find("SystemCanvas"))
                GameObject.Find("SystemCanvas").transform.FindChild("SystemUIAnime").FindChild("SystemBGImage").FindChild("SaveButton").GetComponent<Button>().onClick.AddListener(this.OnSceneSave);

            string path = _pluginDir + _config;
            if (File.Exists(path) == false)
                return;
            using (FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                using (XmlReader xmlReader = XmlReader.Create(fileStream))
                {
                    while (xmlReader.Read())
                    {
                        if (xmlReader.NodeType == XmlNodeType.Element)
                        {
                            switch (xmlReader.Name)
                            {
                                case "uiScale":
                                    if (xmlReader.GetAttribute("value") != null)
                                        UIUtility.uiScale = Mathf.Clamp(XmlConvert.ToSingle(xmlReader.GetAttribute("value")), 0.5f, 2f);
                                    break;
                            }
                        }
                    }
                }
            }
        }

        protected virtual void Start()
        {
            this._cameraController = FindObjectOfType<CameraControl>();
            this.SpawnGUI();
        }

        protected virtual void Update()
        {
            if (HSPE.level != 1)
                return;

            if (this._femaleIndexOffset < Studio.Instance.FemaleList.Count)
            {
                int i = 0;
                foreach (KeyValuePair<uint, StudioFemale> kvp in Studio.Instance.FemaleList)
                {
                    if (i >= this._femaleIndexOffset && kvp.Value.anmMng.animator.gameObject.GetComponent<ManualBoneController>() == null)
                        kvp.Value.anmMng.animator.gameObject.AddComponent<ManualBoneController>().chara = kvp.Value;
                    ++i;
                }
            }
            this._femaleIndexOffset = Studio.Instance.FemaleList.Count;
            if (this._maleIndexOffset < Studio.Instance.MaleList.Count)
            {
                int i = 0;
                foreach (KeyValuePair<uint, StudioMale> kvp in Studio.Instance.MaleList)
                {
                    if (i >= this._maleIndexOffset && kvp.Value.anmMng.animator.gameObject.GetComponent<ManualBoneController>() == null)
                        kvp.Value.anmMng.animator.gameObject.AddComponent<ManualBoneController>().chara = kvp.Value;
                    ++i;
                }
            }
            this._maleIndexOffset = Studio.Instance.MaleList.Count;

            ManualBoneController last = this._manualBoneTarget;
            if (Studio.Instance.CurrentChara != null && (Studio.Instance.CurrentChara.GetStudioFemale() != null || Studio.Instance.CurrentChara.GetStudioMale() != null))
                this._manualBoneTarget = Studio.Instance.CurrentChara.anmMng.animator.GetComponent<ManualBoneController>();
            else
                this._manualBoneTarget = null;

            if (last != this._manualBoneTarget)
                this.OnTargetChange(last);

            this.GUILogic();
        }

        protected virtual void OnGUI()
        {
            GUIUtility.ScaleAroundPivot(Vector2.one * UIUtility.uiScale, new Vector2(Screen.width, Screen.height));
            if (this._manualBoneTarget != null)
            {
                if (this._advancedModeToggle.isOn)
                {
                    for (int i = 0; i < 3; ++i)
                        GUI.Box(this._advancedModeRect, "");
                    this._manualBoneTarget.draw = true;
                    this._advancedModeRect = GUILayout.Window(50, this._advancedModeRect, this._manualBoneTarget.AdvancedModeWindow, "Advanced mode");
                }
                else
                    this._manualBoneTarget.draw = false;
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
                    xmlWriter.WriteAttributeString("value", XmlConvert.ToString(UIUtility.uiScale));
                    xmlWriter.WriteEndElement();

                    xmlWriter.WriteEndElement();
                }
            }
        }
        #endregion

        #region GUI

        private void SpawnGUI()
        {
            this._ui = UIUtility.CreateNewUISystem();
            {
                Image bg = UIUtility.AddImageToObject(UIUtility.CreateNewUIObject(this._ui.transform, "BG").gameObject);
                bg.raycastTarget = false;
                bg.rectTransform.SetRect(Vector2.zero, Vector2.zero, new Vector2(3f, 3f), new Vector2(387f, 543f));

                {
                    Image topContainer = UIUtility.AddImageToObject(UIUtility.CreateNewUIObject(bg.rectTransform, "Top Container").gameObject, UIUtility.headerSprite);
                    topContainer.color = UIUtility.beigeColor;
                    topContainer.rectTransform.SetRect(new Vector2(0f, 1f), Vector2.one, new Vector2(4f, -28f), new Vector2(-4f, -4f));
                    topContainer.gameObject.AddComponent<MovableWindow>().toDrag = bg.rectTransform;

                    Text titleText = UIUtility.AddTextToObject(UIUtility.CreateNewUIObject(topContainer.transform, "Title Text"), "HSPE");
                    titleText.alignment = TextAnchor.MiddleCenter;
                    titleText.resizeTextForBestFit = true;
                    titleText.fontStyle = FontStyle.Bold;
                    titleText.rectTransform.SetRect(Vector2.zero, Vector2.one, new Vector2(2f, 2f), new Vector2(-2f, -2f));
                    titleText.color = Color.white;
                    UIUtility.AddOutlineToObject(titleText.gameObject);
                }

                this._nothingText = UIUtility.AddTextToObject(UIUtility.CreateNewUIObject(bg.transform, "Nothing Text").gameObject, "There is no character selected. Please select a character to begin pose edition.");
                this._nothingText.rectTransform.SetRect(Vector2.zero, Vector2.one, new Vector2(5f, 5f), new Vector2(-5f, -25f));
                this._nothingText.gameObject.SetActive(false);

                {
                    this._controls = UIUtility.CreateNewUIObject(bg.transform, "Controls");
                    this._controls.SetRect(Vector2.zero, Vector2.one, new Vector2(5f, 5f), new Vector2(-5f, -5f));

                    {
                        this._bones = UIUtility.CreateNewUIObject(this._controls, "Bones");
                        this._bones.SetRect(Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0f, -24f));

                        Button rightShoulder = UIUtility.AddButtonToObject(UIUtility.CreateNewUIObject(this._bones, "Right Shoulder Button").gameObject, "R. Shoulder");
                        rightShoulder.onClick.AddListener(() => this.SetBoneTarget(FullBodyBipedEffector.RightShoulder));
                        Text t = rightShoulder.GetComponentInChildren<Text>();
                        t.resizeTextForBestFit = true;
                        t.resizeTextMaxSize = 100;
                        RectTransform buttonRT = rightShoulder.transform as RectTransform;
                        buttonRT.SetRect(new Vector2(0.25f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -35f), Vector2.zero);
                        this._effectorsButtons[(int) FullBodyBipedEffector.RightShoulder] = rightShoulder;

                        Button leftShoulder = UIUtility.AddButtonToObject(UIUtility.CreateNewUIObject(this._bones, "Left Shoulder Button").gameObject, "L. Shoulder");
                        leftShoulder.onClick.AddListener(() => this.SetBoneTarget(FullBodyBipedEffector.LeftShoulder));
                        t = leftShoulder.GetComponentInChildren<Text>();
                        t.resizeTextForBestFit = true;
                        t.resizeTextMaxSize = 100;
                        buttonRT = leftShoulder.transform as RectTransform;
                        buttonRT.SetRect(new Vector2(0.5f, 1f), new Vector2(0.75f, 1f), new Vector2(0f, -35f), Vector2.zero);
                        this._effectorsButtons[(int) FullBodyBipedEffector.LeftShoulder] = leftShoulder;

                        Button rightArmBendGoal = UIUtility.AddButtonToObject(UIUtility.CreateNewUIObject(this._bones, "Right Arm Bend Goal Button").gameObject, "R. Elbow Dir.");
                        rightArmBendGoal.onClick.AddListener(() => this.SetBendGoalTarget(FullBodyBipedChain.RightArm));
                        t = rightArmBendGoal.GetComponentInChildren<Text>();
                        t.resizeTextForBestFit = true;
                        t.resizeTextMaxSize = 100;
                        buttonRT = rightArmBendGoal.transform as RectTransform;
                        buttonRT.SetRect(new Vector2(0.125f, 1f), new Vector2(0.375f, 1f), new Vector2(0f, -70f), new Vector2(0f, -35f));
                        this._bendGoalsButtons[(int) FullBodyBipedChain.RightArm] = rightArmBendGoal;

                        Button leftArmBendGoal = UIUtility.AddButtonToObject(UIUtility.CreateNewUIObject(this._bones, "Left Arm Bend Goal Button").gameObject, "L. Elbow Dir.");
                        leftArmBendGoal.onClick.AddListener(() => this.SetBendGoalTarget(FullBodyBipedChain.LeftArm));
                        t = leftArmBendGoal.GetComponentInChildren<Text>();
                        t.resizeTextForBestFit = true;
                        t.resizeTextMaxSize = 100;
                        buttonRT = leftArmBendGoal.transform as RectTransform;
                        buttonRT.SetRect(new Vector2(0.625f, 1f), new Vector2(0.875f, 1f), new Vector2(0f, -70f), new Vector2(0f, -35f));
                        this._bendGoalsButtons[(int) FullBodyBipedChain.LeftArm] = leftArmBendGoal;

                        Button rightHand = UIUtility.AddButtonToObject(UIUtility.CreateNewUIObject(this._bones, "Right Hand Button").gameObject, "R. Hand");
                        rightHand.onClick.AddListener(() => this.SetBoneTarget(FullBodyBipedEffector.RightHand));
                        t = rightHand.GetComponentInChildren<Text>();
                        t.resizeTextForBestFit = true;
                        t.resizeTextMaxSize = 100;
                        buttonRT = rightHand.transform as RectTransform;
                        buttonRT.SetRect(new Vector2(0f, 1f), new Vector2(0.25f, 1f), new Vector2(0f, -105f), new Vector2(0f, -70f));
                        this._effectorsButtons[(int) FullBodyBipedEffector.RightHand] = rightHand;

                        Button leftHand = UIUtility.AddButtonToObject(UIUtility.CreateNewUIObject(this._bones, "Left Hand Button").gameObject, "L. Hand");
                        leftHand.onClick.AddListener(() => this.SetBoneTarget(FullBodyBipedEffector.LeftHand));
                        t = leftHand.GetComponentInChildren<Text>();
                        t.resizeTextForBestFit = true;
                        t.resizeTextMaxSize = 100;
                        buttonRT = leftHand.transform as RectTransform;
                        buttonRT.SetRect(new Vector2(0.75f, 1f), Vector2.one, new Vector2(0f, -105f), new Vector2(0f, -70f));
                        this._effectorsButtons[(int) FullBodyBipedEffector.LeftHand] = leftHand;

                        Button body = UIUtility.AddButtonToObject(UIUtility.CreateNewUIObject(this._bones, "Body Button").gameObject, "Body");
                        body.onClick.AddListener(() => this.SetBoneTarget(FullBodyBipedEffector.Body));
                        t = body.GetComponentInChildren<Text>();
                        t.resizeTextForBestFit = true;
                        t.resizeTextMaxSize = 100;
                        buttonRT = body.transform as RectTransform;
                        buttonRT.SetRect(new Vector2(0.375f, 1f), new Vector2(0.625f, 1f), new Vector2(0f, -140f), new Vector2(0f, -105f));
                        this._effectorsButtons[(int) FullBodyBipedEffector.Body] = body;

                        Button rightThigh = UIUtility.AddButtonToObject(UIUtility.CreateNewUIObject(this._bones, "Right Thigh Button").gameObject, "R. Thigh");
                        rightThigh.onClick.AddListener(() => this.SetBoneTarget(FullBodyBipedEffector.RightThigh));
                        t = rightThigh.GetComponentInChildren<Text>();
                        t.resizeTextForBestFit = true;
                        t.resizeTextMaxSize = 100;
                        buttonRT = rightThigh.transform as RectTransform;
                        buttonRT.SetRect(new Vector2(0.25f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -175f), new Vector2(0f, -140f));
                        this._effectorsButtons[(int) FullBodyBipedEffector.RightThigh] = rightThigh;

                        Button leftThigh = UIUtility.AddButtonToObject(UIUtility.CreateNewUIObject(this._bones, "Left Thigh Button").gameObject, "L. Thigh");
                        leftThigh.onClick.AddListener(() => this.SetBoneTarget(FullBodyBipedEffector.LeftThigh));
                        t = leftThigh.GetComponentInChildren<Text>();
                        t.resizeTextForBestFit = true;
                        t.resizeTextMaxSize = 100;
                        buttonRT = leftThigh.transform as RectTransform;
                        buttonRT.SetRect(new Vector2(0.5f, 1f), new Vector2(0.75f, 1f), new Vector2(0f, -175f), new Vector2(0f, -140f));
                        this._effectorsButtons[(int) FullBodyBipedEffector.LeftThigh] = leftThigh;

                        Button rightLegBendGoal = UIUtility.AddButtonToObject(UIUtility.CreateNewUIObject(this._bones, "Right Leg Bend Goal Button").gameObject, "R. Knee Dir.");
                        rightLegBendGoal.onClick.AddListener(() => this.SetBendGoalTarget(FullBodyBipedChain.RightLeg));
                        t = rightLegBendGoal.GetComponentInChildren<Text>();
                        t.resizeTextForBestFit = true;
                        t.resizeTextMaxSize = 100;
                        buttonRT = rightLegBendGoal.transform as RectTransform;
                        buttonRT.SetRect(new Vector2(0.125f, 1f), new Vector2(0.375f, 1f), new Vector2(0f, -210f), new Vector2(0f, -175f));
                        this._bendGoalsButtons[(int) FullBodyBipedChain.RightLeg] = rightLegBendGoal;

                        Button leftLegBendGoal = UIUtility.AddButtonToObject(UIUtility.CreateNewUIObject(this._bones, "Left Leg Bend Goal Button").gameObject, "L. Knee Dir.");
                        leftLegBendGoal.onClick.AddListener(() => this.SetBendGoalTarget(FullBodyBipedChain.LeftLeg));
                        t = leftLegBendGoal.GetComponentInChildren<Text>();
                        t.resizeTextForBestFit = true;
                        t.resizeTextMaxSize = 100;
                        buttonRT = leftLegBendGoal.transform as RectTransform;
                        buttonRT.SetRect(new Vector2(0.625f, 1f), new Vector2(0.875f, 1f), new Vector2(0f, -210f), new Vector2(0f, -175f));
                        this._bendGoalsButtons[(int) FullBodyBipedChain.LeftLeg] = leftLegBendGoal;

                        Button rightFoot = UIUtility.AddButtonToObject(UIUtility.CreateNewUIObject(this._bones, "Right Foot Button").gameObject, "R. Foot");
                        rightFoot.onClick.AddListener(() => this.SetBoneTarget(FullBodyBipedEffector.RightFoot));
                        t = rightFoot.GetComponentInChildren<Text>();
                        t.resizeTextForBestFit = true;
                        t.resizeTextMaxSize = 100;
                        buttonRT = rightFoot.transform as RectTransform;
                        buttonRT.SetRect(new Vector2(0f, 1f), new Vector2(0.25f, 1f), new Vector2(0f, -245f), new Vector2(0f, -210f));
                        this._effectorsButtons[(int) FullBodyBipedEffector.RightFoot] = rightFoot;

                        Button leftFoot = UIUtility.AddButtonToObject(UIUtility.CreateNewUIObject(this._bones, "Left Foot Button").gameObject, "L. Foot");
                        leftFoot.onClick.AddListener(() => this.SetBoneTarget(FullBodyBipedEffector.LeftFoot));
                        t = leftFoot.GetComponentInChildren<Text>();
                        t.resizeTextForBestFit = true;
                        t.resizeTextMaxSize = 100;
                        buttonRT = leftFoot.transform as RectTransform;
                        buttonRT.SetRect(new Vector2(0.75f, 1f), Vector2.one, new Vector2(0f, -245f), new Vector2(0f, -210f));
                        this._effectorsButtons[(int) FullBodyBipedEffector.LeftFoot] = leftFoot;

                        {
                            RectTransform buttons = UIUtility.CreateNewUIObject(this._bones, "Buttons");
                            buttons.SetRect(Vector2.zero, new Vector2(0.5f, 1f), new Vector2(0f, 45f), new Vector2(0f, -245f));

                            Button xMoveButton = UIUtility.AddButtonToObject(UIUtility.CreateNewUIObject(buttons, "X Move Button").gameObject, "↑\nX\n↓");
                            ColorBlock cb = xMoveButton.colors;
                            cb.normalColor = Color.red;
                            cb.highlightedColor = Color.Lerp(Color.red, Color.white, 0.5f);
                            xMoveButton.colors = cb;
                            t = xMoveButton.GetComponentInChildren<Text>();
                            t.fontSize = t.fontSize * 2;
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

                            Button yMoveButton = UIUtility.AddButtonToObject(UIUtility.CreateNewUIObject(buttons, "Y Move Button").gameObject, "↑\nY\n↓");
                            cb = yMoveButton.colors;
                            cb.highlightedColor = Color.Lerp(Color.green, Color.white, 0.5f);
                            cb.normalColor = Color.green;
                            yMoveButton.colors = cb;
                            t = yMoveButton.GetComponentInChildren<Text>();
                            t.fontSize = t.fontSize * 2;
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

                            Button zMoveButton = UIUtility.AddButtonToObject(UIUtility.CreateNewUIObject(buttons, "Z Move Button").gameObject, "↑\nZ\n↓");
                            cb = zMoveButton.colors;
                            cb.highlightedColor = Color.Lerp(Color.blue, Color.white, 0.5f);
                            cb.normalColor = Color.blue;
                            zMoveButton.colors = cb;
                            t = zMoveButton.GetComponentInChildren<Text>();
                            t.fontSize = t.fontSize * 2;
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

                            Button rotXButton = UIUtility.AddButtonToObject(UIUtility.CreateNewUIObject(buttons, "Rot X Button").gameObject, "←   →\nRot X");
                            cb = rotXButton.colors;
                            cb.highlightedColor = Color.Lerp(Color.red, Color.white, 0.5f);
                            cb.normalColor = Color.red;
                            rotXButton.colors = cb;
                            //t = rotXButton.GetComponentInChildren<Text>();
                            //t.fontSize = (int)(t.fontSize * 1.5f);
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

                            Button rotYButton = UIUtility.AddButtonToObject(UIUtility.CreateNewUIObject(buttons, "Rot Y Button").gameObject, "←   →\nRot Y");
                            cb = rotYButton.colors;
                            cb.highlightedColor = Color.Lerp(Color.green, Color.white, 0.5f);
                            cb.normalColor = Color.green;
                            rotYButton.colors = cb;
                            //t = rotYButton.GetComponentInChildren<Text>();
                            //t.fontSize = (int)(t.fontSize * 1.5f);
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

                            Button rotZButton = UIUtility.AddButtonToObject(UIUtility.CreateNewUIObject(buttons, "Rot Z Button").gameObject, "←   →\nRot Z");
                            cb = rotZButton.colors;
                            cb.highlightedColor = Color.Lerp(Color.blue, Color.white, 0.5f);
                            cb.normalColor = Color.blue;
                            rotZButton.colors = cb;
                            //t = rotZButton.GetComponentInChildren<Text>();
                            //t.fontSize = (int)(t.fontSize * 1.5f);
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
                            Image experimental = UIUtility.AddImageToObject(UIUtility.CreateNewUIObject(this._bones, "Experimental Features"));
                            experimental.color = UIUtility.whiteColor;
                            experimental.rectTransform.SetRect(new Vector2(0.5f, 0f), Vector2.one, new Vector2(2.5f, 45f), new Vector2(0f, -245f));

                            Image experimentalHeader = UIUtility.AddImageToObject(UIUtility.CreateNewUIObject(experimental.rectTransform, "Header").gameObject, UIUtility.headerSprite);
                            experimentalHeader.color = UIUtility.purpleColor;
                            experimentalHeader.rectTransform.SetRect(new Vector2(0f, 1f), Vector2.one, new Vector2(2.5f, -26.5f), new Vector2(-2.5f, -2.5f));

                            Text headerText = UIUtility.AddTextToObject(UIUtility.CreateNewUIObject(experimentalHeader.transform, "Header Text"), "Experimental Features");
                            headerText.alignment = TextAnchor.MiddleCenter;
                            headerText.resizeTextForBestFit = true;
                            headerText.fontStyle = FontStyle.Bold;
                            headerText.rectTransform.SetRect(Vector2.zero, Vector2.one, new Vector2(2f, 2f), new Vector2(-2f, -2f));
                            headerText.color = Color.white;
                            UIUtility.AddOutlineToObject(headerText.transform).effectColor = Color.black;

                            Button copyLeftArmButton = UIUtility.AddButtonToObject(UIUtility.CreateNewUIObject(experimental.rectTransform, "Copy Right Arm Button"), "Copy R. arm");
                            copyLeftArmButton.GetComponentInChildren<Text>().resizeTextForBestFit = true;
                            copyLeftArmButton.onClick.AddListener(() =>
                            {
                                if (this._manualBoneTarget != null)
                                    this._manualBoneTarget.CopyLimbToTwin(FullBodyBipedChain.RightArm);
                            });
                            buttonRT = copyLeftArmButton.transform as RectTransform;
                            buttonRT.SetRect(new Vector2(0f, 1f), new Vector2(0.5f, 1f), new Vector2(2.5f, -69f), new Vector2(-1.25f, -29f));

                            Button copyRightArmButton = UIUtility.AddButtonToObject(UIUtility.CreateNewUIObject(experimental.rectTransform, "Copy Left Arm Button"), "Copy L. arm");
                            copyRightArmButton.GetComponentInChildren<Text>().resizeTextForBestFit = true;
                            copyRightArmButton.onClick.AddListener(() =>
                            {
                                if (this._manualBoneTarget != null)
                                    this._manualBoneTarget.CopyLimbToTwin(FullBodyBipedChain.LeftArm);
                            });
                            buttonRT = copyRightArmButton.transform as RectTransform;
                            buttonRT.SetRect(new Vector2(0.5f, 1f), Vector2.one, new Vector2(1.25f, -69f), new Vector2(-2.5f, -29f));

                            Button copyLeftLegButton = UIUtility.AddButtonToObject(UIUtility.CreateNewUIObject(experimental.rectTransform, "Copy Right Leg Button"), "Copy R. leg");
                            copyLeftLegButton.GetComponentInChildren<Text>().resizeTextForBestFit = true;
                            copyLeftLegButton.onClick.AddListener(() =>
                            {
                                if (this._manualBoneTarget != null)
                                    this._manualBoneTarget.CopyLimbToTwin(FullBodyBipedChain.RightLeg);
                            });
                            buttonRT = copyLeftLegButton.transform as RectTransform;
                            buttonRT.SetRect(new Vector2(0f, 1f), new Vector2(0.5f, 1f), new Vector2(2.5f, -109f), new Vector2(-1.25f, -69f));

                            Button copyRightLegButton = UIUtility.AddButtonToObject(UIUtility.CreateNewUIObject(experimental.rectTransform, "Copy Left LegButton"), "Copy L. leg");
                            copyRightLegButton.GetComponentInChildren<Text>().resizeTextForBestFit = true;
                            copyRightLegButton.onClick.AddListener(() =>
                            {
                                if (this._manualBoneTarget != null)
                                    this._manualBoneTarget.CopyLimbToTwin(FullBodyBipedChain.LeftLeg);
                            });
                            buttonRT = copyRightLegButton.transform as RectTransform;
                            buttonRT.SetRect(new Vector2(0.5f, 1f), Vector2.one, new Vector2(1.25f, -109f), new Vector2(-2.5f, -69f));

                            this._advancedModeToggle = UIUtility.AddToggleToObject(UIUtility.CreateNewUIObject(experimental.rectTransform, "Advanced Mode Toggle").gameObject, "Advanced mode");
                            this._advancedModeToggle.isOn = false;
                            this._advancedModeToggle.onValueChanged.AddListener(this.ToggleAdvancedMode);
                            Text toggleText = this._advancedModeToggle.GetComponentInChildren<Text>();
                            toggleText.resizeTextForBestFit = true;
                            toggleText.resizeTextMinSize = 1;
                            RectTransform toggleRT = this._advancedModeToggle.transform as RectTransform;
                            (toggleRT.GetChild(0) as RectTransform).SetRect(Vector2.zero, new Vector2(0f, 1f), new Vector2(0f, 2.5f), new Vector2(15f, -2.5f));
                            toggleRT.GetComponentInChildren<Text>().rectTransform.offsetMin = new Vector2(17.5f, toggleRT.GetComponentInChildren<Text>().rectTransform.offsetMin.y);
                            toggleRT.SetRect(new Vector2(0f, 1f), Vector2.one, new Vector2(2.5f, -129f), new Vector2(-2.5f, -109f));

                            this._forceBendGoalsToggle = UIUtility.AddToggleToObject(UIUtility.CreateNewUIObject(experimental.rectTransform, "Force Bend Goals Toggle"), "Force bend goals weight");
                            this._forceBendGoalsToggle.isOn = true;
                            this._forceBendGoalsToggle.onValueChanged.AddListener(this.ToggleBendGoals);
                            toggleText = _forceBendGoalsToggle.GetComponentInChildren<Text>();
                            toggleText.resizeTextForBestFit = true;
                            toggleText.resizeTextMinSize = 1;
                            toggleRT = this._forceBendGoalsToggle.transform as RectTransform;
                            (toggleRT.GetChild(0) as RectTransform).SetRect(Vector2.zero, new Vector2(0f, 1f), new Vector2(0f, 2.5f), new Vector2(15f, -2.5f));
                            toggleRT.GetComponentInChildren<Text>().rectTransform.offsetMin = new Vector2(17.5f, toggleRT.GetComponentInChildren<Text>().rectTransform.offsetMin.y);
                            toggleRT.SetRect(new Vector2(0f, 1f), Vector2.one, new Vector2(2.5f, -149f), new Vector2(-2.5f, -129f));
                        }
                        {
                            RectTransform sliderContainer = UIUtility.CreateNewUIObject(this._bones, "Container");
                            sliderContainer.SetRect(Vector2.zero, new Vector2(1f, 0f), new Vector2(0f, 20f), new Vector2(0f, 40f));

                            Text movIntensityTxt = UIUtility.AddTextToObject(UIUtility.CreateNewUIObject(sliderContainer, "Movement Intensity Text"), "Mvt. Intensity");
                            movIntensityTxt.alignment = TextAnchor.MiddleLeft;
                            movIntensityTxt.resizeTextForBestFit = true;
                            movIntensityTxt.rectTransform.SetRect(Vector2.zero, new Vector2(0.333f, 1f), Vector2.zero, Vector2.zero);

                            this._movementIntensity = UIUtility.AddScrollbarToObject(UIUtility.CreateNewUIObject(sliderContainer, "Movement Intensity Slider"));
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

                            this._intensityValueText = UIUtility.AddTextToObject(UIUtility.CreateNewUIObject(sliderContainer, "Movement Intensity Value"), "x1");
                            this._intensityValueText.alignment = TextAnchor.MiddleCenter;
                            this._intensityValueText.resizeTextForBestFit = true;
                            this._intensityValueText.rectTransform.SetRect(new Vector2(0.9f, 0f), Vector2.one, Vector2.zero, Vector2.zero);
                        }
                        {
                            RectTransform scaleContainer = UIUtility.CreateNewUIObject(this._bones, "Scale Container");
                            scaleContainer.SetRect(Vector2.zero, new Vector2(1f, 0f), Vector2.zero, new Vector2(0f, 20f));

                            Text label = UIUtility.AddTextToObject(UIUtility.CreateNewUIObject(scaleContainer, "Label"), "UI Scale");
                            label.rectTransform.SetRect(Vector2.zero, new Vector2(0.75f, 1f), Vector2.zero, Vector2.zero);
                            label.alignment = TextAnchor.MiddleLeft;
                            label.resizeTextForBestFit = true;

                            Button minusButton = UIUtility.AddButtonToObject(UIUtility.CreateNewUIObject(scaleContainer, "Minus Button"), "-");
                            (minusButton.transform as RectTransform).SetRect(new Vector2(0.75f, 0f), new Vector2(0.875f, 1f), Vector2.zero, Vector2.zero);
                            minusButton.onClick.AddListener(() =>
                            {
                                UIUtility.uiScale = Mathf.Clamp(UIUtility.uiScale - 0.1f, 0.5f, 2f);
                                this._ui.scaleFactor = UIUtility.uiScale;
                            });
                            t = minusButton.GetComponentInChildren<Text>();
                            t.rectTransform.SetRect(Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                            t.resizeTextForBestFit = true;
                            t.fontStyle = FontStyle.Bold;

                            Button plusButton = UIUtility.AddButtonToObject(UIUtility.CreateNewUIObject(scaleContainer, "Plus Button"), "+");
                            (plusButton.transform as RectTransform).SetRect(new Vector2(0.875f, 0f), Vector2.one, Vector2.zero, Vector2.zero);
                            plusButton.onClick.AddListener(() =>
                            {
                                UIUtility.uiScale = Mathf.Clamp(UIUtility.uiScale + 0.1f, 0.5f, 2f);
                                this._ui.scaleFactor = UIUtility.uiScale;
                            });
                            t = plusButton.GetComponentInChildren<Text>();
                            t.rectTransform.SetRect(Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                            t.resizeTextForBestFit = true;
                            t.fontStyle = FontStyle.Bold;
                        }
                    }
                }
            }
            this._ui.gameObject.SetActive(false);

            this.SetBoneTarget(FullBodyBipedEffector.Body);
            this.OnTargetChange(null);
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
            this.SelectBoneButtons();
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
            this.SelectBoneButtons();
            foreach (Button bu in this._rotationButtons)
                bu.interactable = false;
            EventSystem.current.SetSelectedGameObject(null);
        }

        private void ResetBoneButtons()
        {
            foreach (FullBodyBipedChain bendGoal in this._bendGoalTargets)
            {
                Button b = this._bendGoalsButtons[(int) bendGoal];
                ColorBlock cb = b.colors;
                cb.normalColor = UIUtility.beigeColor;
                b.colors = cb;
            }
            foreach (FullBodyBipedEffector effector in this._boneTargets)
            {
                Button b = this._effectorsButtons[(int) effector];
                ColorBlock cb = b.colors;
                cb.normalColor = UIUtility.beigeColor;
                b.colors = cb;
            }
        }

        private void SelectBoneButtons()
        {
            foreach (FullBodyBipedChain bendGoal in this._bendGoalTargets)
            {
                Button b = this._bendGoalsButtons[(int)bendGoal];
                ColorBlock cb = b.colors;
                cb.normalColor = UIUtility.blueColor;
                b.colors = cb;
            }
            foreach (FullBodyBipedEffector effector in this._boneTargets)
            {
                Button b = this._effectorsButtons[(int)effector];
                ColorBlock cb = b.colors;
                cb.normalColor = UIUtility.blueColor;
                b.colors = cb;
            }
        }

        private void ToggleAdvancedMode(bool b)
        {
            this._advancedModeToggle.isOn = this._manualBoneTarget != null && b;
        }

        private void ToggleBendGoals(bool b)
        {
            if (this._manualBoneTarget != null)
                this._manualBoneTarget.forceBendGoalsWeight = b;
            this._forceBendGoalsToggle.isOn = this._manualBoneTarget == null || b;
        }

        private void GUILogic()
        {
            if (Input.GetKeyDown(KeyCode.H))
            {
                this._isVisible = !this._isVisible;
                this._ui.gameObject.SetActive(this._isVisible);
            }
            //bool characterHasIk = Studio.Instance.CurrentChara != null && Studio.Instance.CurrentChara.GetStudioItem() == null && Studio.Instance.CurrentChara.ikCtrl.ikEnable;
            //this._bones.gameObject.SetActive(characterHasIk);
            //this._nothingText2.gameObject.SetActive(!characterHasIk);
            if (this._xMove || this._yMove || this._zMove || this._xRot || this._yRot || this._zRot)
            {
                _delta += new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")) / (10f * (Input.GetMouseButton(1) ? 2f : 1f));
                if (this._manualBoneTarget)
                {
                    for (int i = 0; i < this._boneTargets.Count; ++i)
                    {
                        Vector3 newPosition = this._lastBonesPositions[i];
                        Quaternion newRotation = this._lastBonesRotations[i];
                        if (this._xMove)
                            newPosition.x += _delta.y * this._intensityValue;
                        if (this._yMove)
                            newPosition.y += _delta.y * this._intensityValue;
                        if (this._zMove)
                            newPosition.z += _delta.y * this._intensityValue;
                        if (this._xRot)
                            newRotation *= Quaternion.AngleAxis(_delta.x * 20f * this._intensityValue, Vector3.right);
                        if (this._yRot)
                            newRotation *= Quaternion.AngleAxis(_delta.x * 20f * this._intensityValue, Vector3.up);
                        if (this._zRot)
                            newRotation *= Quaternion.AngleAxis(_delta.x * 20f * this._intensityValue, Vector3.forward);
                        this._manualBoneTarget.SetBoneTargetPosition(this._boneTargets[i], newPosition);
                        this._manualBoneTarget.SetBoneTargetRotation(this._boneTargets[i], newRotation);
                    }
                    for (int i = 0; i < this._bendGoalTargets.Count; ++i)
                    {
                        Vector3 newPosition = this._lastBendGoalsPositions[i];
                        if (this._xMove)
                            newPosition.x += _delta.y * this._intensityValue;
                        if (this._yMove)
                            newPosition.y += _delta.y * this._intensityValue;
                        if (this._zMove)
                            newPosition.z += _delta.y * this._intensityValue;
                        this._manualBoneTarget.SetBendGoalPosition(this._bendGoalTargets[i], newPosition);
                    }
                }
            }
            else
            {
                _delta = Vector2.zero;
                if (this._manualBoneTarget != null)
                {
                    for (int i = 0; i < this._boneTargets.Count; ++i)
                    {
                        this._lastBonesPositions[i] = this._manualBoneTarget.GetBoneTargetPosition(this._boneTargets[i]);
                        this._lastBonesRotations[i] = this._manualBoneTarget.GetBoneTargetRotation(this._boneTargets[i]);
                    }
                    for (int i = 0; i < this._bendGoalTargets.Count; ++i)
                        this._lastBendGoalsPositions[i] = this._manualBoneTarget.GetBendGoalPosition(this._bendGoalTargets[i]);
                }
            }

            if (Input.GetMouseButtonDown(0))
            {
                if (this._manualBoneTarget != null && this._advancedModeToggle.isOn)
                {
                    Vector2 mousePos = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
                    if (this._advancedModeRect.Contains(mousePos) || (this._manualBoneTarget.colliderEditEnabled && this._manualBoneTarget.colliderEditRect.Contains(mousePos)))
                    {
                        this.SetNoControlCondition();
                        this._blockCamera = true;
                    }
                    
                }
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
        }

        private void SetNoControlCondition()
        {
            this._cameraController.NoCtrlCondition = CameraControllerCondition;
        }
        #endregion

        #region Private Methods
        private void OnTargetChange(ManualBoneController last)
        {
            if (Studio.Instance.CurrentChara == null || Studio.Instance.CurrentChara is StudioItem)
            {
                this._nothingText.gameObject.SetActive(true);
                this._controls.gameObject.SetActive(false);
            }
            else
            {
                this._nothingText.gameObject.SetActive(false);
                this._controls.gameObject.SetActive(true);
                this._forceBendGoalsToggle.isOn = this._manualBoneTarget.forceBendGoalsWeight;
            }
        }

        [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
        private bool CameraControllerCondition()
        {
            return this._xMove || this._yMove || this._zMove || this._xRot || this._yRot || this._zRot || this._blockCamera;
        }
        #endregion

        #region Saves
        private void OnSceneLoad()
        {
            foreach (KeyValuePair<uint, StudioFemale> kvp in Studio.Instance.FemaleList)
                if (kvp.Value.anmMng.animator.gameObject.GetComponent<ManualBoneController>() == null)
                    kvp.Value.anmMng.animator.gameObject.AddComponent<ManualBoneController>().chara = kvp.Value;
            foreach (KeyValuePair<uint, StudioMale> kvp in Studio.Instance.MaleList)
                if (kvp.Value.anmMng.animator.gameObject.GetComponent<ManualBoneController>() == null)
                    kvp.Value.anmMng.animator.gameObject.AddComponent<ManualBoneController>().chara = kvp.Value;
            string scenePath = Path.GetFileNameWithoutExtension(Studio.Instance.SaveFileName) + ".sav";
            string dir = _pluginDir + _studioSavesDir;
            string path = dir + scenePath;
            if (File.Exists(path) == false)
                return;
            this.StartCoroutine(this.SceneLoadRoutine(path));
        }

        private void OnSceneImport()
        {
            int i = 0;
            foreach (KeyValuePair<uint, StudioFemale> kvp in Studio.Instance.FemaleList)
            {
                if (i >= this._femaleIndexOffset && kvp.Value.anmMng.animator.gameObject.GetComponent<ManualBoneController>() == null)
                    kvp.Value.anmMng.animator.gameObject.AddComponent<ManualBoneController>().chara = kvp.Value;
                ++i;
            }
            i = 0;
            foreach (KeyValuePair<uint, StudioMale> kvp in Studio.Instance.MaleList)
            {
                if (i >= this._maleIndexOffset && kvp.Value.anmMng.animator.gameObject.GetComponent<ManualBoneController>() == null)
                    kvp.Value.anmMng.animator.gameObject.AddComponent<ManualBoneController>().chara = kvp.Value;
                ++i;
            }
            string scenePath = Path.GetFileNameWithoutExtension(this._selectedScenePath) + ".sav";
            string dir = _pluginDir + _studioSavesDir;
            string path = dir + scenePath;
            if (File.Exists(path) == false)
                return;
            this.StartCoroutine(this.SceneLoadRoutine(path, this._femaleIndexOffset, this._maleIndexOffset));
        }

        private IEnumerator SceneLoadRoutine(string path, int femaleIndexOffset = 0, int maleIndexOffset = 0)
        {
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();
            if (!this.IsDocumentXml(path))
            {
                using (FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read))
                {
                    using (BinaryReader binaryReader = new BinaryReader(fileStream))
                    {
                        this.LoadVersion_1_0_0(binaryReader, femaleIndexOffset, maleIndexOffset);
                    }
                }
            }
            else
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(path);
                this.LoadDefaultVersion(doc, femaleIndexOffset, maleIndexOffset);
            }
            this.OnTargetChange(null);
        }

        private void OnSceneMayDelete()
        {
            GameObject.Find("DeleteFileCheckCanvas").transform.FindChild("DeleteCheckPanel").FindChild("Image").FindChild("YesButton").GetComponent<Button>().onClick.AddListener(this.OnSceneDelete);
        }

        private void OnSceneDelete()
        {
            string completePath = _pluginDir + _studioSavesDir + Path.GetFileNameWithoutExtension(this._selectedScenePath) + ".sav";
            if (File.Exists(completePath))
                File.Delete(completePath);
        }

        private void OnSceneSave()
        {
            string saveFileName = Path.GetFileNameWithoutExtension(Studio.Instance.SaveFileName) + ".sav";
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
                    xmlWriter.WriteAttributeString("version", HSPE.VersionNum.ToString());
                    int index = 0;
                    foreach (KeyValuePair<uint, StudioFemale> female in Studio.Instance.FemaleList)
                    {
                        xmlWriter.WriteStartElement("femaleCharacterInfo");
                        xmlWriter.WriteAttributeString("name", female.Value.female.customInfo.name);
                        xmlWriter.WriteAttributeString("index", XmlConvert.ToString(index));
                        ManualBoneController controller = female.Value.anmMng.animator.GetComponent<ManualBoneController>();
                        written += controller.SaveXml(xmlWriter);
                        xmlWriter.WriteEndElement();
                        ++index;
                    }
                    index = 0;
                    foreach (KeyValuePair<uint, StudioMale> male in Studio.Instance.MaleList)
                    {
                        xmlWriter.WriteStartElement("maleCharacterInfo");
                        xmlWriter.WriteAttributeString("name", male.Value.male.customInfo.name);
                        xmlWriter.WriteAttributeString("index", XmlConvert.ToString(index));
                        ManualBoneController controller = male.Value.anmMng.animator.GetComponent<ManualBoneController>();
                        written += controller.SaveXml(xmlWriter);
                        xmlWriter.WriteEndElement();
                        ++index;
                    }
                    xmlWriter.WriteEndElement();
                }
            }
            if (written == 0)
                File.Delete(path);
        }

        private void LoadVersion_1_0_0(BinaryReader binaryReader, int femaleOffset = 0, int maleOffset = 0)
        {
            int femaleCount = binaryReader.ReadInt32();
            for (int i = 0; i < femaleCount; ++i)
            {
                int index = femaleOffset + binaryReader.ReadInt32();
                int j = 0;
                foreach (KeyValuePair<uint, StudioFemale> kvp in Studio.Instance.FemaleList)
                {
                    if (j == index)
                    {
                        kvp.Value.ikCtrl.IK_Enable(true);
                        kvp.Value.anmMng.animator.gameObject.GetComponent<ManualBoneController>().LoadBinary(binaryReader);
                        break;
                    }
                    ++j;
                }
            }
            int maleCount = binaryReader.ReadInt32();
            for (int i = 0; i < maleCount; ++i)
            {
                int index = maleOffset + binaryReader.ReadInt32();
                int j = 0;
                foreach (KeyValuePair<uint, StudioMale> kvp in Studio.Instance.MaleList)
                {
                    if (j == index)
                    {
                        kvp.Value.ikCtrl.IK_Enable(true);
                        kvp.Value.anmMng.animator.gameObject.GetComponent<ManualBoneController>().LoadBinary(binaryReader);
                        break;
                    }
                    ++j;
                }
            }
        }

        private void LoadDefaultVersion(XmlDocument xmlDoc, int femaleOffset = 0, int maleOffset = 0)
        {
            if (xmlDoc.DocumentElement == null || xmlDoc.DocumentElement.Name != "root")
                return;
            HSPE.VersionNumber v = new HSPE.VersionNumber(xmlDoc.DocumentElement.GetAttribute("version"));

            foreach (XmlNode node in xmlDoc.DocumentElement.ChildNodes)
            {
                int index = 0;
                switch (node.Name)
                {
                    case "femaleCharacterInfo":
                        //string name = xmlDoc.GetAttribute("name");
                        index = femaleOffset + XmlConvert.ToInt32(node.Attributes["index"].Value);
                        int i = 0;
                        foreach (KeyValuePair<uint, StudioFemale> kvp in Studio.Instance.FemaleList)
                        {
                            if (i == index)
                            {
                                kvp.Value.anmMng.animator.gameObject.GetComponent<ManualBoneController>().LoadXml(node, v);
                                break;
                            }
                            ++i;
                        }
                        break;
                    case "maleCharacterInfo":
                        //string name = xmlReader.GetAttribute("name");
                        index = maleOffset + XmlConvert.ToInt32(node.Attributes["index"].Value);
                        i = 0;
                        foreach (KeyValuePair<uint, StudioMale> kvp in Studio.Instance.MaleList)
                        {
                            if (i == index)
                            {
                                kvp.Value.anmMng.animator.gameObject.GetComponent<ManualBoneController>().LoadXml(node, v);
                                break;
                            }
                            ++i;
                        }
                        break;

                }
            }
        }

        private bool IsDocumentXml(string path)
        {
            try
            {
                using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read))
                {
                    using (XmlTextReader xmlReader = new XmlTextReader(stream))
                    {
                        xmlReader.Read();
                    }
                }
            }
            catch (XmlException)
            {
                return false;
            }
            return true;
        }
        #endregion
    }
}
