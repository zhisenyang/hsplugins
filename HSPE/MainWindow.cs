// Decompiled with JetBrains decompiler
// Type: ShortcutsHS.CharaCustomWindow
// Assembly: ShortcutsHS, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 62AE9985-56CF-46BD-982F-B15D5A0C4B01
// Assembly location: C:\Program Files (x86)\HoneySelect\illusion\HoneySelect\Plugins\ShortcutsHS.dll

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Manager;
using RootMotion.FinalIK;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace HSPE
{
    public class MainWindow : MonoBehaviour
    {
        #region Private Variables
        private CameraControl _cameraController;
        private ManualBoneController _manualBoneTarget;
        private FullBodyBipedEffector _boneTarget;
        private FullBodyBipedChain _bendGoalTarget;
        private bool _isTargetBendGoal;
        private string _selectedScenePath = "";
        private int _femaleIndexOffset;
        private int _maleIndexOffset;
        private HashSet<StudioChara> _manualBonesCharas = new HashSet<StudioChara>();
        private Vector2 _delta;
        private Vector3 _lastPosition;
        private Quaternion _lastRotation;
        private bool _horizontalPlaneMove;
        private bool _verticalMove;
        private bool _xRot;
        private bool _yRot;
        private bool _zRot;
        private bool _isVisible = false;
        private Rect _windowRect = new Rect(0f, Screen.height * 0.5f, Screen.width * 0.2f, Screen.height * 0.5f);
        private Canvas _ui;
        private Text _nothingText;
        private Toggle _manualPoseToggle;
        private RectTransform _controls;
        private RectTransform _bones;
        private Toggle _controllersToggle;
        private Toggle _advancedModeToggle;
        private Text _targetText;
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
        }

        protected virtual void Start()
        {
            this._cameraController = FindObjectOfType<CameraControl>();
            this._cameraController.NoCtrlCondition += CameraControllerCondition;
            this.SpawnGUI();
        }

        protected virtual void Update()
        {
            if (HSPE.level != 1)
                return;
            if (Input.GetKeyDown(KeyCode.H))
            {
                this._isVisible = !this._isVisible;
                this._ui.gameObject.SetActive(this._isVisible);
            }

            if (Studio.Instance.CurrentChara == null)
            {
                if (this._nothingText.gameObject.activeSelf == false)
                {
                    this._nothingText.gameObject.SetActive(true);
                    this._controls.gameObject.SetActive(false);
                }
            }
            else
            {
                if (this._nothingText.gameObject.activeSelf)
                {
                    this._nothingText.gameObject.SetActive(false);
                    this._controls.gameObject.SetActive(true);
                }
                if (Studio.Instance.CurrentChara is StudioFemale)
                    this._targetText.text = ("Target: " + Studio.Instance.CurrentChara.GetStudioFemale().female.customInfo.name);
                else if (Studio.Instance.CurrentChara is StudioMale)
                    this._targetText.text = ("Target: " + Studio.Instance.CurrentChara.GetStudioMale().male.customInfo.name);
                else
                    this._targetText.text = "";
            }


            this._femaleIndexOffset = Studio.Instance.FemaleList.Count;
            this._maleIndexOffset = Studio.Instance.MaleList.Count;
            ManualBoneController last = this._manualBoneTarget;
            if (Studio.Instance.CurrentChara != null && (Studio.Instance.CurrentChara.GetStudioFemale() != null || Studio.Instance.CurrentChara.GetStudioMale() != null))
                this._manualBoneTarget = Studio.Instance.CurrentChara.anmMng.animator.GetComponent<ManualBoneController>();
            else
                this._manualBoneTarget = null;
            if (this._manualBoneTarget != null)
            {
                this._controllersToggle.isOn = this._manualBoneTarget.showControllers;
                this._manualBoneTarget.advancedMode = this._advancedModeToggle.isOn;
            }

            if (last != this._manualBoneTarget && last != null)
                last.advancedMode = false;

            if (this._manualBoneTarget == null)
            {
                if (this._bones.gameObject.activeSelf)
                {
                    this._bones.gameObject.SetActive(false);
                    this._manualPoseToggle.isOn = false;
                }
            }
            else
            {
                if (this._bones.gameObject.activeSelf == false)
                {
                    this._bones.gameObject.SetActive(true);
                    this._manualPoseToggle.isOn = true;
                }
            }

            if (this._manualBoneTarget)
                this._manualBoneTarget.draw = this.gameObject.activeSelf;
            if (this._horizontalPlaneMove || this._verticalMove || this._xRot || this._yRot || this._zRot)
            {
                _delta += new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")) / 10f;
                if (this._manualBoneTarget)
                {
                    Vector3 newPosition = this._lastPosition;
                    Quaternion newRotation = this._lastRotation;
                    
                    if (this._horizontalPlaneMove)
                        newPosition += Quaternion.AngleAxis(Studio.Instance.MainCamera.transform.rotation.eulerAngles.y, Vector3.up) * new Vector3(_delta.x, 0f, _delta.y);
                    if (this._verticalMove)
                        newPosition.y += _delta.y;
                    if (this._xRot)
                        newRotation *= Quaternion.AngleAxis(_delta.x * 5f, Vector3.right);
                    if (this._yRot)
                        newRotation *= Quaternion.AngleAxis(_delta.x * 5f, Vector3.up);
                    if (this._zRot)
                        newRotation *= Quaternion.AngleAxis(_delta.x * 5f, Vector3.forward);
                    if (!this._isTargetBendGoal)
                    {
                        this._manualBoneTarget.SetBoneTargetPosition(this._boneTarget, newPosition);
                        this._manualBoneTarget.SetBoneTargetRotation(this._boneTarget, newRotation);
                    }
                    else
                        this._manualBoneTarget.SetBendGoalPosition(this._bendGoalTarget, newPosition);
                }
            }
            else
            {
                _delta = Vector2.zero;
                if (this._manualBoneTarget)
                {
                    if (!this._isTargetBendGoal)
                    {
                        this._lastPosition = this._manualBoneTarget.GetBoneTargetPosition(this._boneTarget);
                        this._lastRotation = this._manualBoneTarget.GetBoneTargetRotation(this._boneTarget);
                    }
                    else
                        this._lastPosition = this._manualBoneTarget.GetBendGoalPosition(this._bendGoalTarget);
                }
            }

            if (Input.GetMouseButtonUp(0))
            {
                this._horizontalPlaneMove = false;
                this._verticalMove = false;
                this._xRot = false;
                this._yRot = false;
                this._zRot = false;
            }
        }

        protected virtual void OnGUI()
        {
            return;
            if (this._isVisible)
                GUILayout.Window(0, this._windowRect, this.PoseEdition, "Pose Editor");
        }
        #endregion

        #region GUI
        private void SpawnGUI()
        {
            this._ui = UIUtility.CreateNewUISystem();
            Image bg = UIUtility.AddImageToObject(UIUtility.CreateNewUIObject(this._ui.transform, "BG").gameObject);

            bg.rectTransform.SetRect(Vector2.zero, new Vector2(0.2f, 0.5f), Vector2.zero, Vector2.zero);

            this._nothingText = UIUtility.AddTextToObject(UIUtility.CreateNewUIObject(bg.transform, "Nothing Text").gameObject, "There is no character selected. Please select a character to begin pose edition.");
            this._nothingText.rectTransform.SetRect(Vector2.zero, Vector2.one, new Vector2(5f, 5f), new Vector2(-5f, -5f));
            this._nothingText.gameObject.SetActive(false);

            this._controls = UIUtility.CreateNewUIObject(bg.transform, "Controls");
            this._controls.SetRect(Vector2.zero, Vector2.one, new Vector2(5f, 5f), new Vector2(-5f, -5f));

            Image topContainer = UIUtility.AddImageToObject(UIUtility.CreateNewUIObject(this._controls, "Top Container").gameObject);
            topContainer.color = UIUtility.beigeColor;
            topContainer.rectTransform.SetRect(new Vector2(0f, 1f), Vector2.one, new Vector2(0f, -25f), Vector2.zero);

            this._manualPoseToggle = UIUtility.AddToggleToObject(UIUtility.CreateNewUIObject(topContainer.transform, "Manual Pose Toggle").gameObject, "Manual pose");
            this._manualPoseToggle.isOn = this._manualBoneTarget != null;
            this._manualPoseToggle.onValueChanged.AddListener(this.ManualPoseToggle);
            RectTransform toggleRT = this._manualPoseToggle.transform as RectTransform;
            toggleRT.SetRect(Vector2.zero, new Vector2(0.5f, 1f), new Vector2(2.5f, 2.5f), new Vector2(-2.5f, -2.5f));

            this._targetText = UIUtility.AddTextToObject(UIUtility.CreateNewUIObject(topContainer.transform, "Target Text").gameObject, "Target Text");
            this._targetText.fontStyle = FontStyle.Bold;
            this._targetText.alignment = TextAnchor.MiddleRight;
            this._targetText.resizeTextForBestFit = true;
            this._targetText.resizeTextMinSize = 1;
            this._targetText.resizeTextMaxSize = (int)(UIUtility.defaultFontSize * 0.75f);
            this._targetText.rectTransform.SetRect(new Vector2(0.5f, 0f), Vector2.one, new Vector2(2.5f, 2.5f), new Vector2(-5f, -2.5f));

            this._bones = UIUtility.CreateNewUIObject(this._controls, "Bones");
            this._bones.SetRect(Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0f, -30f));

            Button rightShoulder = UIUtility.AddButtonToObject(UIUtility.CreateNewUIObject(this._bones, "Right Shoulder Button").gameObject, "Right Shoulder");
            rightShoulder.onClick.AddListener(() => this.SetBoneTarget(FullBodyBipedEffector.RightShoulder));
            rightShoulder.GetComponentInChildren<Text>().resizeTextForBestFit = true;
            RectTransform buttonRT = rightShoulder.transform as RectTransform;
            buttonRT.SetRect(new Vector2(0.25f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -30f), Vector2.zero);

            Button leftShoulder = UIUtility.AddButtonToObject(UIUtility.CreateNewUIObject(this._bones, "Left Shoulder Button").gameObject, "Left Shoulder");
            leftShoulder.onClick.AddListener(() => this.SetBoneTarget(FullBodyBipedEffector.LeftShoulder));
            leftShoulder.GetComponentInChildren<Text>().resizeTextForBestFit = true;
            buttonRT = leftShoulder.transform as RectTransform;
            buttonRT.SetRect(new Vector2(0.5f, 1f), new Vector2(0.75f, 1f), new Vector2(0f, -30f), Vector2.zero);

            Button rightArmBendGoal = UIUtility.AddButtonToObject(UIUtility.CreateNewUIObject(this._bones, "Right Arm Bend Goal Button").gameObject, "Right Elbow Direction");
            rightArmBendGoal.onClick.AddListener(() => this.SetBendGoalTarget(FullBodyBipedChain.RightArm));
            rightArmBendGoal.GetComponentInChildren<Text>().resizeTextForBestFit = true;
            buttonRT = rightArmBendGoal.transform as RectTransform;
            buttonRT.SetRect(new Vector2(0.125f, 1f), new Vector2(0.375f, 1f), new Vector2(0f, -60f), new Vector2(0f, -30f));

            Button leftArmBendGoal = UIUtility.AddButtonToObject(UIUtility.CreateNewUIObject(this._bones, "Left Arm Bend Goal Button").gameObject, "Left Elbow Direction");
            leftArmBendGoal.onClick.AddListener(() => this.SetBendGoalTarget(FullBodyBipedChain.LeftArm));
            leftArmBendGoal.GetComponentInChildren<Text>().resizeTextForBestFit = true;
            buttonRT = leftArmBendGoal.transform as RectTransform;
            buttonRT.SetRect(new Vector2(0.625f, 1f), new Vector2(0.875f, 1f), new Vector2(0f, -60f), new Vector2(0f, -30f));

            Button rightHand = UIUtility.AddButtonToObject(UIUtility.CreateNewUIObject(this._bones, "Right Hand Button").gameObject, "Right Hand");
            rightHand.onClick.AddListener(() => this.SetBoneTarget(FullBodyBipedEffector.RightHand));
            rightHand.GetComponentInChildren<Text>().resizeTextForBestFit = true;
            buttonRT = rightHand.transform as RectTransform;
            buttonRT.SetRect(new Vector2(0f, 1f), new Vector2(0.25f, 1f), new Vector2(0f, -90f), new Vector2(0f, -60f));

            Button leftHand = UIUtility.AddButtonToObject(UIUtility.CreateNewUIObject(this._bones, "Left Hand Button").gameObject, "Left Hand");
            leftHand.onClick.AddListener(() => this.SetBoneTarget(FullBodyBipedEffector.LeftHand));
            leftHand.GetComponentInChildren<Text>().resizeTextForBestFit = true;
            buttonRT = leftHand.transform as RectTransform;
            buttonRT.SetRect(new Vector2(0.75f, 1f), Vector2.one, new Vector2(0f, -90f), new Vector2(0f, -60f));

            Button body = UIUtility.AddButtonToObject(UIUtility.CreateNewUIObject(this._bones, "Body Button").gameObject, "Body");
            body.onClick.AddListener(() => this.SetBoneTarget(FullBodyBipedEffector.Body));
            body.GetComponentInChildren<Text>().resizeTextForBestFit = true;
            buttonRT = body.transform as RectTransform;
            buttonRT.SetRect(new Vector2(0.375f, 1f), new Vector2(0.625f, 1f), new Vector2(0f, -120f), new Vector2(0f, -90f));

            Button rightThigh = UIUtility.AddButtonToObject(UIUtility.CreateNewUIObject(this._bones, "Right Thigh Button").gameObject, "Right Thigh");
            rightThigh.onClick.AddListener(() => this.SetBoneTarget(FullBodyBipedEffector.RightThigh));
            rightThigh.GetComponentInChildren<Text>().resizeTextForBestFit = true;
            buttonRT = rightThigh.transform as RectTransform;
            buttonRT.SetRect(new Vector2(0.25f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -150f), new Vector2(0f, -120f));

            Button leftThigh = UIUtility.AddButtonToObject(UIUtility.CreateNewUIObject(this._bones, "Left Thigh Button").gameObject, "Left Thigh");
            leftThigh.onClick.AddListener(() => this.SetBoneTarget(FullBodyBipedEffector.LeftThigh));
            leftThigh.GetComponentInChildren<Text>().resizeTextForBestFit = true;
            buttonRT = leftThigh.transform as RectTransform;
            buttonRT.SetRect(new Vector2(0.5f, 1f), new Vector2(0.75f, 1f), new Vector2(0f, -150f), new Vector2(0f, -120f));

            Button rightLegBendGoal = UIUtility.AddButtonToObject(UIUtility.CreateNewUIObject(this._bones, "Right Leg Bend Goal Button").gameObject, "Right Knee Direction");
            rightLegBendGoal.onClick.AddListener(() => this.SetBendGoalTarget(FullBodyBipedChain.RightLeg));
            rightLegBendGoal.GetComponentInChildren<Text>().resizeTextForBestFit = true;
            buttonRT = rightLegBendGoal.transform as RectTransform;
            buttonRT.SetRect(new Vector2(0.125f, 1f), new Vector2(0.375f, 1f), new Vector2(0f, -180f), new Vector2(0f, -150f));

            Button leftLegBendGoal = UIUtility.AddButtonToObject(UIUtility.CreateNewUIObject(this._bones, "Left Leg Bend Goal Button").gameObject, "Left Knee Direction");
            leftLegBendGoal.onClick.AddListener(() => this.SetBendGoalTarget(FullBodyBipedChain.LeftLeg));
            leftLegBendGoal.GetComponentInChildren<Text>().resizeTextForBestFit = true;
            buttonRT = leftLegBendGoal.transform as RectTransform;
            buttonRT.SetRect(new Vector2(0.625f, 1f), new Vector2(0.875f, 1f), new Vector2(0f, -180f), new Vector2(0f, -150f));

            Button rightFoot = UIUtility.AddButtonToObject(UIUtility.CreateNewUIObject(this._bones, "Right Foot Button").gameObject, "Right Foot");
            rightFoot.onClick.AddListener(() => this.SetBoneTarget(FullBodyBipedEffector.RightFoot));
            rightFoot.GetComponentInChildren<Text>().resizeTextForBestFit = true;
            buttonRT = rightFoot.transform as RectTransform;
            buttonRT.SetRect(new Vector2(0f, 1f), new Vector2(0.25f, 1f), new Vector2(0f, -210f), new Vector2(0f, -180f));

            Button leftFoot = UIUtility.AddButtonToObject(UIUtility.CreateNewUIObject(this._bones, "Left Foot Button").gameObject, "Left Foot");
            leftFoot.onClick.AddListener(() => this.SetBoneTarget(FullBodyBipedEffector.LeftFoot));
            leftFoot.GetComponentInChildren<Text>().resizeTextForBestFit = true;
            buttonRT = leftFoot.transform as RectTransform;
            buttonRT.SetRect(new Vector2(0.75f, 1f), Vector2.one, new Vector2(0f, -210f), new Vector2(0f, -180f));

            RectTransform buttons = UIUtility.CreateNewUIObject(this._bones, "Buttons");
            buttons.SetRect(Vector2.zero, new Vector2(0.666f, 1f), Vector2.zero, new Vector2(0f, -210f));

            Button horizontalPlaneButton = UIUtility.AddButtonToObject(UIUtility.CreateNewUIObject(buttons, "Horizontal Plane Button").gameObject, "↑\n← X & Z →\n↓");
            horizontalPlaneButton.GetComponentInChildren<Image>().raycastTarget = true;
            horizontalPlaneButton.gameObject.AddComponent<PointerDownHandler>().onPointerDown += () => this._horizontalPlaneMove = true;
            buttonRT = horizontalPlaneButton.transform as RectTransform;
            buttonRT.SetRect(new Vector2(0f, 0.333f), new Vector2(0.666f, 1f), Vector2.zero, Vector2.zero);

            Button verticalButton = UIUtility.AddButtonToObject(UIUtility.CreateNewUIObject(buttons, "Vertical Button").gameObject, "↑\nY\n↓");
            verticalButton.GetComponentInChildren<Image>().raycastTarget = true;
            verticalButton.gameObject.AddComponent<PointerDownHandler>().onPointerDown += () => this._verticalMove = true;
            buttonRT = verticalButton.transform as RectTransform;
            buttonRT.SetRect(new Vector2(0.666f, 0.333f), Vector2.one, Vector2.zero, Vector2.zero);

            Button rotXButton = UIUtility.AddButtonToObject(UIUtility.CreateNewUIObject(buttons, "Rot X Button").gameObject, "Rot X");
            rotXButton.GetComponentInChildren<Image>().raycastTarget = true;
            rotXButton.gameObject.AddComponent<PointerDownHandler>().onPointerDown += () => this._xRot = true;
            buttonRT = rotXButton.transform as RectTransform;
            buttonRT.SetRect(Vector2.zero, new Vector2(0.333f, 0.333f), Vector2.zero, Vector2.zero);

            Button rotYButton = UIUtility.AddButtonToObject(UIUtility.CreateNewUIObject(buttons, "Rot Y Button").gameObject, "Rot Y");
            rotYButton.GetComponentInChildren<Image>().raycastTarget = true;
            rotYButton.gameObject.AddComponent<PointerDownHandler>().onPointerDown += () => this._yRot = true;
            buttonRT = rotYButton.transform as RectTransform;
            buttonRT.SetRect(new Vector2(0.333f, 0f), new Vector2(0.666f, 0.333f), Vector2.zero, Vector2.zero);

            Button rotZButton = UIUtility.AddButtonToObject(UIUtility.CreateNewUIObject(buttons, "Rot Z Button").gameObject, "Rot Z");
            rotZButton.GetComponentInChildren<Image>().raycastTarget = true;
            rotZButton.gameObject.AddComponent<PointerDownHandler>().onPointerDown += () => this._zRot = true;
            buttonRT = rotZButton.transform as RectTransform;
            buttonRT.SetRect(new Vector2(0.666f, 0f), new Vector2(1f, 0.333f), Vector2.zero, Vector2.zero);

            RectTransform options = UIUtility.CreateNewUIObject(this._bones, "Options");
            options.SetRect(new Vector2(0.666f, 0f), Vector2.one, Vector2.zero, new Vector2(0f, -210f));

            this._controllersToggle = UIUtility.AddToggleToObject(UIUtility.CreateNewUIObject(options, "Controllers Toggle").gameObject, "Show controllers");
            this._controllersToggle.isOn = true;
            this._controllersToggle.onValueChanged.AddListener(this.ToggleControllers);
            Text toggleText = this._controllersToggle.GetComponentInChildren<Text>();
            toggleText.resizeTextForBestFit = true;
            toggleText.resizeTextMinSize = 1;
            toggleRT = this._controllersToggle.transform as RectTransform;
            (toggleRT.GetChild(0) as RectTransform).SetRect(Vector2.zero, new Vector2(0f, 1f), new Vector2(0f, 2.5f), new Vector2(15f, -2.5f));
            toggleRT.GetComponentInChildren<Text>().rectTransform.offsetMin = new Vector2(17.5f, toggleRT.GetComponentInChildren<Text>().rectTransform.offsetMin.y);
            toggleRT.SetRect(new Vector2(0f, 1f), Vector2.one, new Vector2(2.5f, -20f), new Vector2(-2.5f, 0f));

            this._advancedModeToggle = UIUtility.AddToggleToObject(UIUtility.CreateNewUIObject(options, "Advanced Mode Toggle").gameObject, "Advanced mode");
            this._advancedModeToggle.isOn = false;
            this._advancedModeToggle.onValueChanged.AddListener(this.ToggleAdvancedMode);
            toggleText = this._advancedModeToggle.GetComponentInChildren<Text>();
            toggleText.resizeTextForBestFit = true;
            toggleText.resizeTextMinSize = 1;
            toggleRT = this._advancedModeToggle.transform as RectTransform;
            (toggleRT.GetChild(0) as RectTransform).SetRect(Vector2.zero, new Vector2(0f, 1f), new Vector2(0f, 2.5f), new Vector2(15f, -2.5f));
            toggleRT.GetComponentInChildren<Text>().rectTransform.offsetMin = new Vector2(17.5f, toggleRT.GetComponentInChildren<Text>().rectTransform.offsetMin.y);
            toggleRT.SetRect(new Vector2(0f, 1f), Vector2.one, new Vector2(2.5f, -40f), new Vector2(-2.5f, -20f));

            this._ui.gameObject.SetActive(false);
        }

        private void ManualPoseToggle(bool b)
        {
            if (b)
            {
                if (this._manualBonesCharas.Contains(Studio.Instance.CurrentChara) == false && (Studio.Instance.CurrentChara is StudioFemale || Studio.Instance.CurrentChara is StudioMale))
                {
                    this._manualBoneTarget = Studio.Instance.CurrentChara.anmMng.animator.gameObject.AddComponent<ManualBoneController>();
                    this._manualBoneTarget.chara = Studio.Instance.CurrentChara;
                    this._manualBonesCharas.Add(Studio.Instance.CurrentChara);
                }
            }
            else
            {
                if (this._manualBonesCharas.Contains(Studio.Instance.CurrentChara))
                {
                    Destroy(Studio.Instance.CurrentChara.anmMng.animator.gameObject.GetComponent<ManualBoneController>());
                    this._manualBoneTarget = null;
                    this._manualBonesCharas.Remove(Studio.Instance.CurrentChara);
                }
            }
            this._manualPoseToggle.isOn = this._manualBonesCharas.Contains(Studio.Instance.CurrentChara);
        }

        private void SetBoneTarget(FullBodyBipedEffector bone)
        {
            this._boneTarget = bone;
            this._isTargetBendGoal = false;
        }

        private void SetBendGoalTarget(FullBodyBipedChain bendGoal)
        {
            this._bendGoalTarget = bendGoal;
            this._isTargetBendGoal = true;
        }

        private void ToggleControllers(bool b)
        {
            if (this._manualBoneTarget)
                this._manualBoneTarget.showControllers = b;
            this._controllersToggle.isOn = this._manualBoneTarget != null && b;
        }

        private void ToggleAdvancedMode(bool b)
        {
            if (this._manualBoneTarget)
                this._manualBoneTarget.advancedMode = b;
            this._advancedModeToggle.isOn = this._manualBoneTarget != null && b;
        }
        #endregion

        #region Private Methods
        private bool CameraControllerCondition()
        {
            return this._horizontalPlaneMove || this._verticalMove || this._xRot || this._yRot || this._zRot;
        }

        private void PoseEdition(int id)
        {
            if (Studio.Instance.CurrentChara == null)
            {
                GUILayout.Label("There is no character selected. Please select a character to begin pose edition.");
                return;
            }
            GUILayout.BeginHorizontal();
            if (GUILayout.Toggle(this._manualBonesCharas.Contains(Studio.Instance.CurrentChara), "Manual pose"))
            {
                if (this._manualBonesCharas.Contains(Studio.Instance.CurrentChara) == false && (Studio.Instance.CurrentChara is StudioFemale || Studio.Instance.CurrentChara is StudioMale))
                {
                    this._manualBoneTarget = Studio.Instance.CurrentChara.anmMng.animator.gameObject.AddComponent<ManualBoneController>();
                    this._manualBoneTarget.chara = Studio.Instance.CurrentChara;
                    this._manualBonesCharas.Add(Studio.Instance.CurrentChara);
                }
            }
            else
            {
                if (this._manualBonesCharas.Contains(Studio.Instance.CurrentChara))
                {
                    Destroy(Studio.Instance.CurrentChara.anmMng.animator.gameObject.GetComponent<ManualBoneController>());
                    this._manualBoneTarget = null;
                    this._manualBonesCharas.Remove(Studio.Instance.CurrentChara);
                }
            }
            if (this._manualBoneTarget != null)
            {
                if (Studio.Instance.CurrentChara is StudioFemale)
                    GUILayout.Label("Target: " + Studio.Instance.CurrentChara.GetStudioFemale().female.customInfo.name);
                else
                    GUILayout.Label("Target: " + Studio.Instance.CurrentChara.GetStudioMale().male.customInfo.name);
            }
            GUILayout.EndHorizontal();
            if (this._manualBoneTarget == null)
                return;
            GUILayout.BeginVertical();
            this.DisplayBoneList();
            GUILayout.EndVertical();
            GUILayout.BeginHorizontal();
            this._manualBoneTarget.advancedMode = GUILayout.Toggle(this._manualBoneTarget.advancedMode, "Advanced mode");
            this._manualBoneTarget.showControllers = GUILayout.Toggle(this._manualBoneTarget.showControllers, "Show Controllers");
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            this._horizontalPlaneMove = GUILayout.RepeatButton("Λ\n|\nX & Z\n<--        -->\nMove\n|\nV", GUILayout.MinWidth(100f), GUILayout.MinHeight(100f));
            this._verticalMove = GUILayout.RepeatButton("Λ\n|\nY Move\n|\nV", GUILayout.MinWidth(50f), GUILayout.MinHeight(100f));
            GUILayout.EndHorizontal();
            if (!this._isTargetBendGoal)
            {
                GUILayout.BeginHorizontal();
                this._xRot = GUILayout.RepeatButton("<--    -->\nX Rot", GUILayout.MinWidth(50f), GUILayout.MinHeight(50f));
                this._yRot = GUILayout.RepeatButton("<--    -->\nY Rot", GUILayout.MinWidth(50f), GUILayout.MinHeight(50f));
                this._zRot = GUILayout.RepeatButton("<--    -->\nZ Rot", GUILayout.MinWidth(50f), GUILayout.MinHeight(50f));
                GUILayout.EndHorizontal();
            }
            Vector3 position;
            Quaternion rotation = Quaternion.identity;
            if (!this._isTargetBendGoal)
            {
                position = this._manualBoneTarget.GetBoneTargetPosition(this._boneTarget);
                rotation = this._manualBoneTarget.GetBoneTargetRotation(this._boneTarget);
            }
            else
                position = this._manualBoneTarget.GetBendGoalPosition(this._bendGoalTarget);
            //GUILayout.BeginHorizontal();
            //if (GUILayout.Button("Reset Pos", GUILayout.MinWidth(75f), GUILayout.MinHeight(75f)))
            //    position = Vector3.zero;
            //if (!this._isTargetBendGoal && GUILayout.Button("Reset Rot", GUILayout.MinWidth(75f), GUILayout.MinHeight(75f)))
            //    rotation = Quaternion.identity;
            //if (!this._isTargetBendGoal && GUILayout.Button("Reset Both", GUILayout.MinWidth(75f), GUILayout.MinHeight(75f)))
            //{
            //    position = Vector3.zero;
            //    rotation = Quaternion.identity;
            //}
            //GUILayout.EndHorizontal();
            GUILayout.BeginVertical();
            GUILayout.Label("Position: X " + position.x.ToString("0.00") + " Y " + position.y.ToString("0.00") + " Z " + position.z.ToString("0.00"));
            if (!this._isTargetBendGoal)
            {
                GUILayout.Label("Rotation (euler): X " + rotation.eulerAngles.x.ToString("0.00") + " Y " + rotation.eulerAngles.y.ToString("0.00") + " Z " + rotation.eulerAngles.z.ToString("0.00"));
                GUILayout.Label("Rotation: W " + rotation.w.ToString("0.00") + " X " + rotation.x.ToString("0.00") + " Y " + rotation.y.ToString("0.00") + " Z " + rotation.z.ToString("0.00"));
            }
            GUILayout.EndVertical();
            if (!this._isTargetBendGoal)
            {
                this._manualBoneTarget.SetBoneTargetPosition(this._boneTarget, position);
                this._manualBoneTarget.SetBoneTargetRotation(this._boneTarget, rotation);
            }
            else
                this._manualBoneTarget.SetBendGoalPosition(this._bendGoalTarget, position);
        }

        private void DisplayBoneList()
        {
            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();
            GUILayout.Space(60f);
            this.DisplayBoneSingle(FullBodyBipedEffector.LeftShoulder);
            this.DisplayBoneSingle(FullBodyBipedEffector.RightShoulder);
            GUILayout.Space(60f);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            this.DisplayBendGoalSingle(FullBodyBipedChain.LeftArm);
            this.DisplayBendGoalSingle(FullBodyBipedChain.RightArm);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Space(20f);
            this.DisplayBoneSingle(FullBodyBipedEffector.LeftHand);
            GUILayout.Space(80f);
            this.DisplayBoneSingle(FullBodyBipedEffector.RightHand);
            GUILayout.Space(20f);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Space(100f);
            this.DisplayBoneSingle(FullBodyBipedEffector.Body);
            GUILayout.Space(100f);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Space(60f);
            this.DisplayBoneSingle(FullBodyBipedEffector.LeftThigh);
            this.DisplayBoneSingle(FullBodyBipedEffector.RightThigh);
            GUILayout.Space(60f);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            this.DisplayBendGoalSingle(FullBodyBipedChain.LeftLeg);
            this.DisplayBendGoalSingle(FullBodyBipedChain.RightLeg);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Space(20f);
            this.DisplayBoneSingle(FullBodyBipedEffector.LeftFoot);
            GUILayout.Space(80f);
            this.DisplayBoneSingle(FullBodyBipedEffector.RightFoot);
            GUILayout.Space(20f);
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        private void DisplayBoneSingle(FullBodyBipedEffector type)
        {
            Color c = GUI.color;
            if (!this._isTargetBendGoal && this._boneTarget == type)
                GUI.color = Color.red;
            if (GUILayout.Button(type.ToString()))
            {
                this._boneTarget = type;
                this._isTargetBendGoal = false;
            }
            GUI.color = c;
        }

        private void DisplayBendGoalSingle(FullBodyBipedChain type)
        {
            Color c = GUI.color;
            if (this._isTargetBendGoal && this._bendGoalTarget == type)
                GUI.color = Color.red;
            if (GUILayout.Button(type + "BendGoal"))
            {
                this._bendGoalTarget = type;
                this._isTargetBendGoal = true;
            }
            GUI.color = c;
        }

        private void OnSceneLoad()
        {
            this._manualBonesCharas.Clear();
            string scenePath = Path.GetFileNameWithoutExtension(Studio.Instance.SaveFileName) + ".sav";
            string dir = "Plugins\\HSPE\\StudioScenes";
            string path = dir + "\\" + scenePath;
            if (File.Exists(path) == false)
                return;
            using (FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                using (BinaryReader binaryReader = new BinaryReader(fileStream))
                {
                    int femaleCount = binaryReader.ReadInt32();
                    for (int i = 0; i < femaleCount; ++i)
                    {
                        int index = binaryReader.ReadInt32();
                        int j = 0;
                        foreach (KeyValuePair<uint, StudioFemale> kvp in Studio.Instance.FemaleList)
                        {
                            if (j == index)
                            {
                                ManualBoneController bone = kvp.Value.anmMng.animator.gameObject.AddComponent<ManualBoneController>();
                                bone.LoadBinary(binaryReader);
                                bone.chara = kvp.Value;
                                this._manualBonesCharas.Add(kvp.Value);
                                break;
                            }
                            ++j;
                        }
                    }
                    int maleCount = binaryReader.ReadInt32();
                    for (int i = 0; i < maleCount; ++i)
                    {
                        int index = binaryReader.ReadInt32();
                        int j = 0;
                        foreach (KeyValuePair<uint, StudioMale> kvp in Studio.Instance.MaleList)
                        {
                            if (j == index)
                            {
                                ManualBoneController bone = kvp.Value.anmMng.animator.gameObject.AddComponent<ManualBoneController>();
                                bone.LoadBinary(binaryReader);
                                bone.chara = kvp.Value;
                                this._manualBonesCharas.Add(kvp.Value);
                                break;
                            }
                            ++j;
                        }
                    }
                }
            }
        }

        private void OnSceneImport()
        {
            string scenePath = Path.GetFileNameWithoutExtension(this._selectedScenePath) + ".sav";
            string dir = "Plugins\\HSPE\\StudioScenes";
            string path = dir + "\\" + scenePath;
            if (File.Exists(path) == false)
                return;
            using (FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                using (BinaryReader binaryReader = new BinaryReader(fileStream))
                {
                    int femaleCount = binaryReader.ReadInt32();
                    for (int i = 0; i < femaleCount; ++i)
                    {
                        int index = this._femaleIndexOffset + binaryReader.ReadInt32();
                        int j = 0;
                        foreach (KeyValuePair<uint, StudioFemale> kvp in Studio.Instance.FemaleList)
                        {
                            if (j == index)
                            {
                                ManualBoneController bone = kvp.Value.anmMng.animator.gameObject.AddComponent<ManualBoneController>();
                                bone.LoadBinary(binaryReader);
                                bone.chara = kvp.Value;
                                //kvp.Value.anmMng.animator.enabled = false;
                                this._manualBonesCharas.Add(kvp.Value);
                                break;
                            }
                            ++j;
                        }
                    }
                    int maleCount = binaryReader.ReadInt32();
                    for (int i = 0; i < maleCount; ++i)
                    {
                        int index = this._maleIndexOffset + binaryReader.ReadInt32();
                        int j = 0;
                        foreach (KeyValuePair<uint, StudioMale> kvp in Studio.Instance.MaleList)
                        {
                            if (j == index)
                            {
                                ManualBoneController bone = kvp.Value.anmMng.animator.gameObject.AddComponent<ManualBoneController>();
                                bone.LoadBinary(binaryReader);
                                bone.chara = kvp.Value;
                                //kvp.Value.anmMng.animator.enabled = false;
                                this._manualBonesCharas.Add(kvp.Value);
                                break;
                            }
                            ++j;
                        }
                    }
                }
            }
        }

        private void OnSceneMayDelete()
        {
            GameObject.Find("DeleteFileCheckCanvas").transform.FindChild("DeleteCheckPanel").FindChild("Image").FindChild("YesButton").GetComponent<Button>().onClick.AddListener(this.OnSceneDelete);
        }

        private void OnSceneDelete()
        {
            string completePath = "Plugins\\HSPE\\StudioScenes\\" + Path.GetFileNameWithoutExtension(this._selectedScenePath) + ".sav";
            UnityEngine.Debug.Log(completePath);
            if (File.Exists(completePath))
                File.Delete(completePath);
        }

        private void OnSceneSave()
        {
            string saveFileName = Path.GetFileNameWithoutExtension(Studio.Instance.SaveFileName) + ".sav";
            string dir = "Plugins\\HSPE\\StudioScenes";
            if (Directory.Exists(dir) == false)
                Directory.CreateDirectory(dir);
            int femaleCount = Studio.Instance.FemaleList.ToList().FindAll(x => x.Value.anmMng.animator.GetComponent<ManualBoneController>() != null).Count;
            int maleCount = Studio.Instance.MaleList.ToList().FindAll(x => x.Value.anmMng.animator.GetComponent<ManualBoneController>() != null).Count;
            if (femaleCount == 0 && maleCount == 0)
                return;
            using (FileStream fileStream = new FileStream(dir + "\\" + saveFileName, FileMode.Create, FileAccess.Write))
            {
                using (BinaryWriter binaryWriter = new BinaryWriter(fileStream))
                {
                    binaryWriter.Write(femaleCount);
                    int index = 0;
                    foreach (KeyValuePair<uint, StudioFemale> female in Studio.Instance.FemaleList)
                    {
                        ManualBoneController controller = female.Value.anmMng.animator.GetComponent<ManualBoneController>();
                        if (controller != null)
                        {
                            binaryWriter.Write(index);
                            controller.SaveBinary(binaryWriter);
                        }
                        ++index;
                    }
                    binaryWriter.Write(maleCount);
                    index = 0;
                    foreach (KeyValuePair<uint, StudioMale> male in Studio.Instance.MaleList)
                    {
                        ManualBoneController controller = male.Value.anmMng.animator.GetComponent<ManualBoneController>();
                        if (controller != null)
                        {
                            binaryWriter.Write(index);
                            controller.SaveBinary(binaryWriter);
                        }
                        ++index;
                    }
                }
            }
        }
        #endregion
    }
}
