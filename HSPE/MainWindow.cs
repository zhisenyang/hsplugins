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
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using Manager;
using RootMotion.FinalIK;
using UnityEngine;
using UnityEngine.EventSystems;
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
        private Vector2 _delta;
        private Vector3 _lastPosition;
        private Quaternion _lastRotation;
        private bool _horizontalPlaneMove;
        private bool _verticalMove;
        private bool _xRot;
        private bool _yRot;
        private bool _zRot;
        private bool _isVisible = false;
        private Canvas _ui;
        private Text _nothingText;
        private Text _nothingText2;
        private RectTransform _controls;
        private RectTransform _bones;
        private Toggle _advancedModeToggle;
        private Text _targetText;
        private Button[] _effectorsButtons = new Button[9];
        private Button[] _bendGoalsButtons = new Button[4];
        private Button[] _rotationButtons = new Button[3];
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
                    if (i >= this._femaleIndexOffset)
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
                    if (i >= this._maleIndexOffset)
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

            Text titleText = UIUtility.AddTextToObject(UIUtility.CreateNewUIObject(topContainer.transform, "Title Text"), "HSPE");
            titleText.alignment = TextAnchor.MiddleLeft;
            titleText.resizeTextForBestFit = true;
            titleText.rectTransform.SetRect(Vector2.zero, new Vector2(0.5f, 1f), new Vector2(2.5f, 2.5f), new Vector2(-2.5f, -2.5f));

            this._targetText = UIUtility.AddTextToObject(UIUtility.CreateNewUIObject(topContainer.transform, "Target Text").gameObject, "Target Text");
            this._targetText.fontStyle = FontStyle.Bold;
            this._targetText.alignment = TextAnchor.MiddleRight;
            this._targetText.resizeTextForBestFit = true;
            this._targetText.resizeTextMinSize = 1;
            this._targetText.resizeTextMaxSize = (int)(UIUtility.defaultFontSize * 0.75f);
            this._targetText.rectTransform.SetRect(new Vector2(0.5f, 0f), Vector2.one, new Vector2(2.5f, 2.5f), new Vector2(-5f, -2.5f));

            this._nothingText2 = UIUtility.AddTextToObject(UIUtility.CreateNewUIObject(this._controls, "Nothing Text 2"), "The IK system is not enabled on this character.");
            this._nothingText2.rectTransform.SetRect(Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0f, -30f));

            this._bones = UIUtility.CreateNewUIObject(this._controls, "Bones");
            this._bones.SetRect(Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0f, -30f));

            Button rightShoulder = UIUtility.AddButtonToObject(UIUtility.CreateNewUIObject(this._bones, "Right Shoulder Button").gameObject, "Right Shoulder");
            rightShoulder.onClick.AddListener(() => this.SetBoneTarget(FullBodyBipedEffector.RightShoulder));
            rightShoulder.GetComponentInChildren<Text>().resizeTextForBestFit = true;
            RectTransform buttonRT = rightShoulder.transform as RectTransform;
            buttonRT.SetRect(new Vector2(0.25f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -30f), Vector2.zero);
            this._effectorsButtons[(int)FullBodyBipedEffector.RightShoulder] = rightShoulder;

            Button leftShoulder = UIUtility.AddButtonToObject(UIUtility.CreateNewUIObject(this._bones, "Left Shoulder Button").gameObject, "Left Shoulder");
            leftShoulder.onClick.AddListener(() => this.SetBoneTarget(FullBodyBipedEffector.LeftShoulder));
            leftShoulder.GetComponentInChildren<Text>().resizeTextForBestFit = true;
            buttonRT = leftShoulder.transform as RectTransform;
            buttonRT.SetRect(new Vector2(0.5f, 1f), new Vector2(0.75f, 1f), new Vector2(0f, -30f), Vector2.zero);
            this._effectorsButtons[(int)FullBodyBipedEffector.LeftShoulder] = leftShoulder;

            Button rightArmBendGoal = UIUtility.AddButtonToObject(UIUtility.CreateNewUIObject(this._bones, "Right Arm Bend Goal Button").gameObject, "Right Elbow Direction");
            rightArmBendGoal.onClick.AddListener(() => this.SetBendGoalTarget(FullBodyBipedChain.RightArm));
            rightArmBendGoal.GetComponentInChildren<Text>().resizeTextForBestFit = true;
            buttonRT = rightArmBendGoal.transform as RectTransform;
            buttonRT.SetRect(new Vector2(0.125f, 1f), new Vector2(0.375f, 1f), new Vector2(0f, -60f), new Vector2(0f, -30f));
            this._bendGoalsButtons[(int)FullBodyBipedChain.RightArm] = rightArmBendGoal;

            Button leftArmBendGoal = UIUtility.AddButtonToObject(UIUtility.CreateNewUIObject(this._bones, "Left Arm Bend Goal Button").gameObject, "Left Elbow Direction");
            leftArmBendGoal.onClick.AddListener(() => this.SetBendGoalTarget(FullBodyBipedChain.LeftArm));
            leftArmBendGoal.GetComponentInChildren<Text>().resizeTextForBestFit = true;
            buttonRT = leftArmBendGoal.transform as RectTransform;
            buttonRT.SetRect(new Vector2(0.625f, 1f), new Vector2(0.875f, 1f), new Vector2(0f, -60f), new Vector2(0f, -30f));
            this._bendGoalsButtons[(int)FullBodyBipedChain.LeftArm] = leftArmBendGoal;

            Button rightHand = UIUtility.AddButtonToObject(UIUtility.CreateNewUIObject(this._bones, "Right Hand Button").gameObject, "Right Hand");
            rightHand.onClick.AddListener(() => this.SetBoneTarget(FullBodyBipedEffector.RightHand));
            rightHand.GetComponentInChildren<Text>().resizeTextForBestFit = true;
            buttonRT = rightHand.transform as RectTransform;
            buttonRT.SetRect(new Vector2(0f, 1f), new Vector2(0.25f, 1f), new Vector2(0f, -90f), new Vector2(0f, -60f));
            this._effectorsButtons[(int)FullBodyBipedEffector.RightHand] = rightHand;

            Button leftHand = UIUtility.AddButtonToObject(UIUtility.CreateNewUIObject(this._bones, "Left Hand Button").gameObject, "Left Hand");
            leftHand.onClick.AddListener(() => this.SetBoneTarget(FullBodyBipedEffector.LeftHand));
            leftHand.GetComponentInChildren<Text>().resizeTextForBestFit = true;
            buttonRT = leftHand.transform as RectTransform;
            buttonRT.SetRect(new Vector2(0.75f, 1f), Vector2.one, new Vector2(0f, -90f), new Vector2(0f, -60f));
            this._effectorsButtons[(int)FullBodyBipedEffector.LeftHand] = leftHand;

            Button body = UIUtility.AddButtonToObject(UIUtility.CreateNewUIObject(this._bones, "Body Button").gameObject, "Body");
            body.onClick.AddListener(() => this.SetBoneTarget(FullBodyBipedEffector.Body));
            body.GetComponentInChildren<Text>().resizeTextForBestFit = true;
            buttonRT = body.transform as RectTransform;
            buttonRT.SetRect(new Vector2(0.375f, 1f), new Vector2(0.625f, 1f), new Vector2(0f, -120f), new Vector2(0f, -90f));
            this._effectorsButtons[(int)FullBodyBipedEffector.Body] = body;

            Button rightThigh = UIUtility.AddButtonToObject(UIUtility.CreateNewUIObject(this._bones, "Right Thigh Button").gameObject, "Right Thigh");
            rightThigh.onClick.AddListener(() => this.SetBoneTarget(FullBodyBipedEffector.RightThigh));
            rightThigh.GetComponentInChildren<Text>().resizeTextForBestFit = true;
            buttonRT = rightThigh.transform as RectTransform;
            buttonRT.SetRect(new Vector2(0.25f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -150f), new Vector2(0f, -120f));
            this._effectorsButtons[(int)FullBodyBipedEffector.RightThigh] = rightThigh;

            Button leftThigh = UIUtility.AddButtonToObject(UIUtility.CreateNewUIObject(this._bones, "Left Thigh Button").gameObject, "Left Thigh");
            leftThigh.onClick.AddListener(() => this.SetBoneTarget(FullBodyBipedEffector.LeftThigh));
            leftThigh.GetComponentInChildren<Text>().resizeTextForBestFit = true;
            buttonRT = leftThigh.transform as RectTransform;
            buttonRT.SetRect(new Vector2(0.5f, 1f), new Vector2(0.75f, 1f), new Vector2(0f, -150f), new Vector2(0f, -120f));
            this._effectorsButtons[(int)FullBodyBipedEffector.LeftThigh] = leftThigh;

            Button rightLegBendGoal = UIUtility.AddButtonToObject(UIUtility.CreateNewUIObject(this._bones, "Right Leg Bend Goal Button").gameObject, "Right Knee Direction");
            rightLegBendGoal.onClick.AddListener(() => this.SetBendGoalTarget(FullBodyBipedChain.RightLeg));
            rightLegBendGoal.GetComponentInChildren<Text>().resizeTextForBestFit = true;
            buttonRT = rightLegBendGoal.transform as RectTransform;
            buttonRT.SetRect(new Vector2(0.125f, 1f), new Vector2(0.375f, 1f), new Vector2(0f, -180f), new Vector2(0f, -150f));
            this._bendGoalsButtons[(int)FullBodyBipedChain.RightLeg] = rightLegBendGoal;

            Button leftLegBendGoal = UIUtility.AddButtonToObject(UIUtility.CreateNewUIObject(this._bones, "Left Leg Bend Goal Button").gameObject, "Left Knee Direction");
            leftLegBendGoal.onClick.AddListener(() => this.SetBendGoalTarget(FullBodyBipedChain.LeftLeg));
            leftLegBendGoal.GetComponentInChildren<Text>().resizeTextForBestFit = true;
            buttonRT = leftLegBendGoal.transform as RectTransform;
            buttonRT.SetRect(new Vector2(0.625f, 1f), new Vector2(0.875f, 1f), new Vector2(0f, -180f), new Vector2(0f, -150f));
            this._bendGoalsButtons[(int)FullBodyBipedChain.LeftLeg] = leftLegBendGoal;

            Button rightFoot = UIUtility.AddButtonToObject(UIUtility.CreateNewUIObject(this._bones, "Right Foot Button").gameObject, "Right Foot");
            rightFoot.onClick.AddListener(() => this.SetBoneTarget(FullBodyBipedEffector.RightFoot));
            rightFoot.GetComponentInChildren<Text>().resizeTextForBestFit = true;
            buttonRT = rightFoot.transform as RectTransform;
            buttonRT.SetRect(new Vector2(0f, 1f), new Vector2(0.25f, 1f), new Vector2(0f, -210f), new Vector2(0f, -180f));
            this._effectorsButtons[(int)FullBodyBipedEffector.RightFoot] = rightFoot;

            Button leftFoot = UIUtility.AddButtonToObject(UIUtility.CreateNewUIObject(this._bones, "Left Foot Button").gameObject, "Left Foot");
            leftFoot.onClick.AddListener(() => this.SetBoneTarget(FullBodyBipedEffector.LeftFoot));
            leftFoot.GetComponentInChildren<Text>().resizeTextForBestFit = true;
            buttonRT = leftFoot.transform as RectTransform;
            buttonRT.SetRect(new Vector2(0.75f, 1f), Vector2.one, new Vector2(0f, -210f), new Vector2(0f, -180f));
            this._effectorsButtons[(int)FullBodyBipedEffector.LeftFoot] = leftFoot;

            RectTransform buttons = UIUtility.CreateNewUIObject(this._bones, "Buttons");
            buttons.SetRect(Vector2.zero, new Vector2(0.666f, 1f), Vector2.zero, new Vector2(0f, -210f));

            Button horizontalPlaneButton = UIUtility.AddButtonToObject(UIUtility.CreateNewUIObject(buttons, "Horizontal Plane Button").gameObject, "↑\n← X & Z →\n↓");
            horizontalPlaneButton.onClick.AddListener(() => EventSystem.current.SetSelectedGameObject(null));
            horizontalPlaneButton.gameObject.AddComponent<PointerDownHandler>().onPointerDown += () =>
            {
                this._horizontalPlaneMove = true;
                this.SetNoControlCondition();
            };
            buttonRT = horizontalPlaneButton.transform as RectTransform;
            buttonRT.SetRect(new Vector2(0f, 0.333f), new Vector2(0.666f, 1f), Vector2.zero, Vector2.zero);

            Button verticalButton = UIUtility.AddButtonToObject(UIUtility.CreateNewUIObject(buttons, "Vertical Button").gameObject, "↑\nY\n↓");
            verticalButton.onClick.AddListener(() => EventSystem.current.SetSelectedGameObject(null));
            verticalButton.gameObject.AddComponent<PointerDownHandler>().onPointerDown += () =>
            {
                this._verticalMove = true;
                this.SetNoControlCondition();
            };
            buttonRT = verticalButton.transform as RectTransform;
            buttonRT.SetRect(new Vector2(0.666f, 0.333f), Vector2.one, Vector2.zero, Vector2.zero);

            Button rotXButton = UIUtility.AddButtonToObject(UIUtility.CreateNewUIObject(buttons, "Rot X Button").gameObject, "Rot X");
            rotXButton.onClick.AddListener(() => EventSystem.current.SetSelectedGameObject(null));
            rotXButton.gameObject.AddComponent<PointerDownHandler>().onPointerDown += () =>
            {
                this._xRot = true;
                this.SetNoControlCondition();
            };
            buttonRT = rotXButton.transform as RectTransform;
            buttonRT.SetRect(Vector2.zero, new Vector2(0.333f, 0.333f), Vector2.zero, Vector2.zero);
            this._rotationButtons[0] = rotXButton;

            Button rotYButton = UIUtility.AddButtonToObject(UIUtility.CreateNewUIObject(buttons, "Rot Y Button").gameObject, "Rot Y");
            rotYButton.onClick.AddListener(() => EventSystem.current.SetSelectedGameObject(null));
            rotYButton.gameObject.AddComponent<PointerDownHandler>().onPointerDown += () =>
            {
                this._yRot = true;
                this.SetNoControlCondition();
            };
            buttonRT = rotYButton.transform as RectTransform;
            buttonRT.SetRect(new Vector2(0.333f, 0f), new Vector2(0.666f, 0.333f), Vector2.zero, Vector2.zero);
            this._rotationButtons[1] = rotYButton;

            Button rotZButton = UIUtility.AddButtonToObject(UIUtility.CreateNewUIObject(buttons, "Rot Z Button").gameObject, "Rot Z");
            rotZButton.onClick.AddListener(() => EventSystem.current.SetSelectedGameObject(null));
            rotZButton.gameObject.AddComponent<PointerDownHandler>().onPointerDown += () =>
            {
                this._zRot = true;
                this.SetNoControlCondition();
            };
            buttonRT = rotZButton.transform as RectTransform;
            buttonRT.SetRect(new Vector2(0.666f, 0f), new Vector2(1f, 0.333f), Vector2.zero, Vector2.zero);
            this._rotationButtons[2] = rotZButton;

            RectTransform options = UIUtility.CreateNewUIObject(this._bones, "Options");
            options.SetRect(new Vector2(0.666f, 0f), Vector2.one, Vector2.zero, new Vector2(0f, -210f));

            this._advancedModeToggle = UIUtility.AddToggleToObject(UIUtility.CreateNewUIObject(options, "Advanced Mode Toggle").gameObject, "Advanced mode");
            this._advancedModeToggle.isOn = false;
            this._advancedModeToggle.onValueChanged.AddListener(this.ToggleAdvancedMode);
            Text toggleText = this._advancedModeToggle.GetComponentInChildren<Text>();
            toggleText.resizeTextForBestFit = true;
            toggleText.resizeTextMinSize = 1;
            RectTransform toggleRT = this._advancedModeToggle.transform as RectTransform;
            (toggleRT.GetChild(0) as RectTransform).SetRect(Vector2.zero, new Vector2(0f, 1f), new Vector2(0f, 2.5f), new Vector2(15f, -2.5f));
            toggleRT.GetComponentInChildren<Text>().rectTransform.offsetMin = new Vector2(17.5f, toggleRT.GetComponentInChildren<Text>().rectTransform.offsetMin.y);
            toggleRT.SetRect(new Vector2(0f, 1f), Vector2.one, new Vector2(2.5f, -20f), new Vector2(-2.5f, 0f));

            this._ui.gameObject.SetActive(false);
        }

        private void SetBoneTarget(FullBodyBipedEffector bone)
        {
            this.ResetBoneButton();
            this._boneTarget = bone;
            this._isTargetBendGoal = false;
            Button b = this._effectorsButtons[(int)this._boneTarget];
            ColorBlock cb = b.colors;
            cb.normalColor = UIUtility.blueColor;
            b.colors = cb;
            switch (bone)
            {
                case FullBodyBipedEffector.LeftFoot:
                case FullBodyBipedEffector.LeftHand:
                case FullBodyBipedEffector.RightFoot:
                case FullBodyBipedEffector.RightHand:
                    foreach (Button bu in this._rotationButtons)
                        bu.interactable = true;
                    break;
                case FullBodyBipedEffector.Body:
                case FullBodyBipedEffector.LeftShoulder:
                case FullBodyBipedEffector.LeftThigh:
                case FullBodyBipedEffector.RightShoulder:
                case FullBodyBipedEffector.RightThigh:
                    foreach (Button bu in this._rotationButtons)
                        bu.interactable = false;
                    break;
            }
            EventSystem.current.SetSelectedGameObject(null);
        }

        private void SetBendGoalTarget(FullBodyBipedChain bendGoal)
        {
            this.ResetBoneButton();
            this._bendGoalTarget = bendGoal;
            this._isTargetBendGoal = true;
            Button b = this._bendGoalsButtons[(int)this._bendGoalTarget];
            ColorBlock cb = b.colors;
            cb.normalColor = UIUtility.blueColor;
            b.colors = cb;
            foreach (Button bu in this._rotationButtons)
                bu.interactable = false;
            EventSystem.current.SetSelectedGameObject(null);
        }

        private void ResetBoneButton()
        {
            if (this._isTargetBendGoal)
            {
                Button b = this._bendGoalsButtons[(int)this._bendGoalTarget];
                ColorBlock cb = b.colors;
                cb.normalColor = UIUtility.beigeColor;
                b.colors = cb;
            }
            else
            {
                Button b = this._effectorsButtons[(int) this._boneTarget];
                ColorBlock cb = b.colors;
                cb.normalColor = UIUtility.beigeColor;
                b.colors = cb;
            }
        }

        private void ToggleAdvancedMode(bool b)
        {
            if (this._manualBoneTarget)
                this._manualBoneTarget.advancedMode = b;
            this._advancedModeToggle.isOn = this._manualBoneTarget != null && b;
        }
        private void GUILogic()
        {
            if (Input.GetKeyDown(KeyCode.H))
            {
                this._isVisible = !this._isVisible;
                this._ui.gameObject.SetActive(this._isVisible);
            }
            bool characterHasIk = Studio.Instance.CurrentChara != null && Studio.Instance.CurrentChara.GetStudioItem() == null && Studio.Instance.CurrentChara.ikCtrl.ikEnable;
            this._bones.gameObject.SetActive(characterHasIk);
            this._nothingText2.gameObject.SetActive(!characterHasIk);

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
                if (Studio.Instance.CurrentChara is StudioFemale)
                    this._targetText.text = ("Target: " + Studio.Instance.CurrentChara.GetStudioFemale().female.customInfo.name);
                else if (Studio.Instance.CurrentChara is StudioMale)
                    this._targetText.text = ("Target: " + Studio.Instance.CurrentChara.GetStudioMale().male.customInfo.name);
            }

            if (this._manualBoneTarget != null)
                this._manualBoneTarget.advancedMode = this._advancedModeToggle.isOn;
            if (last != this._manualBoneTarget && last != null)
                last.advancedMode = false;

        }

        [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
        private bool CameraControllerCondition()
        {
            return this._horizontalPlaneMove || this._verticalMove || this._xRot || this._yRot || this._zRot;
        }
        #endregion

        #region Saves
        private void OnSceneLoad()
        {
            string scenePath = Path.GetFileNameWithoutExtension(Studio.Instance.SaveFileName) + ".sav";
            string dir = "Plugins\\HSPE\\StudioScenes";
            string path = dir + "\\" + scenePath;
            if (File.Exists(path) == false)
                return;
            using (FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                if (!this.IsDocumentXml(path))
                {
                    using (BinaryReader binaryReader = new BinaryReader(fileStream))
                    {
                        this.LoadVersion_1_0_0(binaryReader);
                    }
                }
                else
                {
                    using (XmlReader xmlReader = XmlReader.Create(fileStream))
                    {
                        this.LoadDefaultVersion(xmlReader);
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
                if (!this.IsDocumentXml(path))
                {
                    using (BinaryReader binaryReader = new BinaryReader(fileStream))
                    {
                        this.LoadVersion_1_0_0(binaryReader, this._femaleIndexOffset, this._maleIndexOffset);
                    }
                }
                else
                {
                    using (XmlReader xmlReader = XmlReader.Create(fileStream))
                    {
                        this.LoadDefaultVersion(xmlReader, this._femaleIndexOffset, this._maleIndexOffset);
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
            if (File.Exists(completePath))
                File.Delete(completePath);
        }

        private void OnSceneSave()
        {
            string saveFileName = Path.GetFileNameWithoutExtension(Studio.Instance.SaveFileName) + ".sav";
            string dir = "Plugins\\HSPE\\StudioScenes";
            if (Directory.Exists(dir) == false)
                Directory.CreateDirectory(dir);
            int written = 0;
            string path = dir + "\\" + saveFileName;
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
            int idx = 0;
            foreach (KeyValuePair<uint, StudioFemale> kvp in Studio.Instance.FemaleList)
            {
                if (idx >= femaleOffset)
                    kvp.Value.anmMng.animator.gameObject.AddComponent<ManualBoneController>().chara = kvp.Value;
                ++idx;
            }
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
            idx = 0;
            foreach (KeyValuePair<uint, StudioMale> kvp in Studio.Instance.MaleList)
            {
                if (idx >= maleOffset)
                    kvp.Value.anmMng.animator.gameObject.AddComponent<ManualBoneController>().chara = kvp.Value;
                ++idx;
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

        private void LoadDefaultVersion(XmlReader xmlReader, int femaleOffset = 0, int maleOffset = 0)
        {
            bool shouldContinue = xmlReader.Read();
            if (xmlReader.NodeType != XmlNodeType.Element || xmlReader.Name != "root")
                return;
            HSPE.VersionNumber v = new HSPE.VersionNumber(xmlReader.GetAttribute("version"));
            if (!shouldContinue)
                return;
            while (xmlReader.Read())
            {
                if (xmlReader.NodeType == XmlNodeType.Element)
                {
                    int index = 0;
                    switch (xmlReader.Name)
                    {
                        case "femaleCharacterInfo":
                            //string name = xmlReader.GetAttribute("name");
                            index = femaleOffset + XmlConvert.ToInt32(xmlReader.GetAttribute("index"));
                            int i = 0;
                            foreach (KeyValuePair<uint, StudioFemale> kvp in Studio.Instance.FemaleList)
                            {
                                if (i == index)
                                {
                                    ManualBoneController bone = kvp.Value.anmMng.animator.gameObject.AddComponent<ManualBoneController>();
                                    bone.chara = kvp.Value;
                                    bone.LoadXml(xmlReader, v);
                                }
                                ++i;
                            }
                            break;
                        case "maleCharacterInfo":
                            //string name = xmlReader.GetAttribute("name");
                            index = maleOffset + XmlConvert.ToInt32(xmlReader.GetAttribute("index"));
                            i = 0;
                            foreach (KeyValuePair<uint, StudioMale> kvp in Studio.Instance.MaleList)
                            {
                                if (i == index)
                                {
                                    ManualBoneController bone = kvp.Value.anmMng.animator.gameObject.AddComponent<ManualBoneController>();
                                    bone.chara = kvp.Value;
                                    bone.LoadXml(xmlReader, v);
                                }
                                ++i;
                            }
                            break;
                            
                    }
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
            catch (XmlException e)
            {
                return false;
            }
            return true;
        }
        #endregion
    }
}
