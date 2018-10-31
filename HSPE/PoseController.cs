using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using Harmony;
using HSPE.AMModules;
using RootMotion.FinalIK;
using Studio;
using ToolBox;
using UnityEngine;
#if KOIKATSU
using Manager;
#endif

namespace HSPE
{
    public class PoseController : MonoBehaviour
    {
        #region Constants
        private static readonly Dictionary<FullBodyBipedEffector, int> _effectorToIndex = new Dictionary<FullBodyBipedEffector, int>()
        {
            { FullBodyBipedEffector.Body, 0 },
            { FullBodyBipedEffector.LeftShoulder, 1 },
            { FullBodyBipedEffector.LeftHand, 3 },
            { FullBodyBipedEffector.RightShoulder, 4 },
            { FullBodyBipedEffector.RightHand, 6 },
            { FullBodyBipedEffector.LeftThigh, 7 },
            { FullBodyBipedEffector.LeftFoot, 9 },
            { FullBodyBipedEffector.RightThigh, 10 },
            { FullBodyBipedEffector.RightFoot, 12 }
        };
        private static readonly Dictionary<FullBodyBipedChain, int> _chainToIndex = new Dictionary<FullBodyBipedChain, int>()
        {
            { FullBodyBipedChain.LeftArm, 2 },
            { FullBodyBipedChain.RightArm, 5 },
            { FullBodyBipedChain.LeftLeg, 8 },
            { FullBodyBipedChain.RightLeg, 11 }
        };

        public static readonly HashSet<FullBodyBipedEffector> nonRotatableEffectors = new HashSet<FullBodyBipedEffector>()
        {
            FullBodyBipedEffector.Body,
            FullBodyBipedEffector.LeftShoulder,
            FullBodyBipedEffector.LeftThigh,
            FullBodyBipedEffector.RightShoulder,
            FullBodyBipedEffector.RightThigh
        };
        #endregion

        #region Patches
#if HONEYSELECT
        [HarmonyPatch(typeof(CharBody), "LateUpdate")]
        private class CharBody_Patches
        {
            public static event Action<CharBody> onPreLateUpdate;
            public static event Action<CharBody> onPostLateUpdate;

            public static void Prefix(CharBody __instance)
            {
                if (onPreLateUpdate != null)
                    onPreLateUpdate(__instance);

            }
            public static void Postfix(CharBody __instance)
            {
                if (onPostLateUpdate != null)
                    onPostLateUpdate(__instance);
            }
        }
#elif KOIKATSU
        [HarmonyPatch(typeof(Character), "LateUpdate")]
        private class Character_Patches
        {
            public static event Action onPreLateUpdate;
            public static event Action onPostLateUpdate;

            public static void Prefix()
            {
                if (onPreLateUpdate != null)
                    onPreLateUpdate();

            }
            public static void Postfix()
            {
                if (onPostLateUpdate != null)
                    onPostLateUpdate();
            }
        }
#endif
        [HarmonyPatch(typeof(IKSolver), "Update")]
        private class IKSolver_Patches
        {
            public static event Action<IKSolver> onPostUpdate;
            [HarmonyBefore("com.joan6694.hsplugins.instrumentation")]
            public static void Postfix(IKSolver __instance)
            {
                if (onPostUpdate != null)
                    onPostUpdate(__instance);
            }
        }
        [HarmonyPatch(typeof(IKExecutionOrder), "LateUpdate")]
        private class IKExecutionOrder_Patches
        {
            public static event Action onPostLateUpdate;
            public static void Postfix()
            {
                if (onPostLateUpdate != null)
                    onPostLateUpdate();
            }
        }        
        [HarmonyPatch(typeof(FKCtrl), "LateUpdate")]
        private class FKCtrl_Patches
        {
            public static event Action<FKCtrl> onPreLateUpdate;
            public static event Action<FKCtrl> onPostLateUpdate;
            public static void Prefix(FKCtrl __instance)
            {
                if (onPreLateUpdate != null)
                    onPreLateUpdate(__instance);
            }
            public static void Postfix(FKCtrl __instance)
            {
                if (onPostLateUpdate != null)
                    onPostLateUpdate(__instance);
            }
        }

        [HarmonyPatch(typeof(OCIChar), "LoadClothesFile", new[] { typeof(string) })]
        private class OCIChar_LoadClothesFile_Patches
        {
            public static event Action<OCIChar> onLoadClothesFile;
            public static void Postfix(OCIChar __instance, string _path)
            {
                if (onLoadClothesFile != null)
                    onLoadClothesFile(__instance);
            }
        }

        [HarmonyPatch(typeof(OCIChar), "ChangeChara", new[] { typeof(string) })]
        private class OCIChar_ChangeChara_Patches
        {
            public static event Action<OCIChar> onChangeChara;
            public static void Postfix(OCIChar __instance, string _path)
            {
                if (onChangeChara != null)
                    onChangeChara(__instance);
            }
        }

#if HONEYSELECT
        [HarmonyPatch(typeof(OCIChar), "SetCoordinateInfo", new[] {typeof(CharDefine.CoordinateType), typeof(bool) })]
#elif KOIKATSU
        [HarmonyPatch(typeof(OCIChar), "SetCoordinateInfo", new[] {typeof(ChaFileDefine.CoordinateType), typeof(bool) })]        
#endif
        private class OCIChar_SetCoordinateInfo_Patches
        {
#if HONEYSELECT
            public static event Action<OCIChar, CharDefine.CoordinateType, bool> onSetCoordinateInfo; 
            public static void Postfix(OCIChar __instance, CharDefine.CoordinateType _type, bool _force)
#elif KOIKATSU
            public static event Action<OCIChar, ChaFileDefine.CoordinateType, bool> onSetCoordinateInfo;
            public static void Postfix(OCIChar __instance, ChaFileDefine.CoordinateType _type, bool _force)
#endif
            {
                if (onSetCoordinateInfo != null)
                    onSetCoordinateInfo(__instance, _type, _force);
            }
        }

        #endregion

        #region Public Types
        public enum DragType
        {
            None,
            Position,
            Rotation,
            Both
        }
        #endregion

        #region Private Variables
        private FullBodyBipedIK _body;
        private float _cachedSpineStiffness;
        private float _cachedPullBodyVertical;
        private readonly Dictionary<int, Vector3> _oldPosValues = new Dictionary<int, Vector3>();
        private readonly Dictionary<int, Vector3> _oldRotValues = new Dictionary<int, Vector3>();
        private bool _lockDrag = false;
        private bool _optimizeIK = true;
        private bool _isFemale;
        private OCIChar _chara;

        private Transform _siriDamL;
        private Transform _siriDamR;
        private Transform _kosi;
        private Quaternion _siriDamLOriginalRotation;
        private Quaternion _siriDamROriginalRotation;
        private Quaternion _kosiOriginalRotation;
        private Quaternion _siriDamLRotation;
        private Quaternion _siriDamRRotation;
        private Quaternion _kosiRotation;
        private bool _lastCrotchJointCorrection = false;

        private Transform _leftFoot2;
        private Quaternion _leftFoot2OriginalRotation;
        private float _leftFoot2ParentOriginalRotation;
        private float _leftFoot2Rotation;
        private bool _lastLeftFootJointCorrection = false;

        private Transform _rightFoot2;
        private Quaternion _rightFoot2OriginalRotation;
        private float _rightFoot2ParentOriginalRotation;
        private float _rightFoot2Rotation;
        private bool _lastrightFootJointCorrection = false;

        private BonesEditor _bonesEditor;
        private BoobsEditor _boobsEditor;
        private DynamicBonesEditor _dynamicBonesEditor;
        private BlendShapesEditor _blendShapesEditor;
        private readonly List<AdvancedModeModule> _modules = new List<AdvancedModeModule>();
        private AdvancedModeModule _currentModule;
        private bool _drawAdvancedMode = false;
        private Action _scheduleNextIKPostUpdate = null;
        private FullBodyBipedChain _nextLimbCopy;
        private List<GuideCommand.EqualsInfo> _additionalRotationEqualsCommands = new List<GuideCommand.EqualsInfo>();
        #endregion

        #region Public Accessors
        public bool isIKEnabled { get { return this._chara.oiCharInfo.enableIK; } }
        public bool drawAdvancedMode
        {
            get { return this._drawAdvancedMode; }
            set
            {
                this._drawAdvancedMode = value;
                foreach (AdvancedModeModule module in this._modules)
                    module.drawAdvancedMode = value;
            }
        }
        public DragType currentDragType { get; private set; }
        public bool optimizeIK
        {
            get { return this._optimizeIK; }
            set
            {
                this._optimizeIK = value;
                if (this._body != null)
                {
                    if (value)
                    {
                        this._body.solver.spineStiffness = 0f;
                        this._body.solver.pullBodyVertical = 0f;
                    }
                    else
                    {
                        this._body.solver.spineStiffness = this._cachedSpineStiffness;
                        this._body.solver.pullBodyVertical = this._cachedPullBodyVertical;
                    }
                }
            }
        }
        public bool colliderEditEnabled { get { return this._bonesEditor.colliderEditEnabled; } }
        public Rect colliderEditRect { get { return this._bonesEditor.colliderEditRect; } }
        public bool isDraggingDynamicBone { get { return this._dynamicBonesEditor.isDraggingDynamicBone || this._boobsEditor != null && this._boobsEditor.isDraggingDynamicBone; } }
        public bool crotchJointCorrection { get; set; }
        public bool leftFootJointCorrection { get; set; }
        public bool rightFootJointCorrection { get; set; }
        #endregion

        #region Unity Methods
        void Awake()
        {
            foreach (KeyValuePair<int, ObjectCtrlInfo> pair in Studio.Studio.Instance.dicObjectCtrl)
            {
                OCIChar ociChar = pair.Value as OCIChar;
                if (ociChar != null && ociChar.charInfo.gameObject == this.gameObject)
                {
                    this._chara = ociChar;
                    break;
                }
            }
#if HONEYSELECT
            this._isFemale = this._chara.charInfo.Sex == 1;
            this._body = this._chara.animeIKCtrl.IK;
#elif KOIKATSU
            this._isFemale = this._chara.charInfo.sex == 1;
            this._body = this._chara.finalIK;
#endif

            this._bonesEditor = this.gameObject.AddComponent<BonesEditor>();
            this._bonesEditor.chara = this._chara;
            this._modules.Add(this._bonesEditor);
            if (this._isFemale)
            {
                this._boobsEditor = this.gameObject.AddComponent<BoobsEditor>();
                this._boobsEditor.chara = this._chara;
                this._modules.Add(this._boobsEditor);
            }
#if HONEYSELECT
            if (this._isFemale)
            {
                this._siriDamL = this.transform.Find("BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_Kosi02/cf_J_SiriDam_L");
                this._siriDamR = this.transform.Find("BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_Kosi02/cf_J_SiriDam_R");
                this._kosi = this.transform.Find("BodyTop/p_cf_anim/cf_J_Root/cf_N_height/cf_J_Hips/cf_J_Kosi01/cf_J_Kosi02/cf_J_Kosi02_s");
            }
            else
            {
                this._siriDamL = this.transform.Find("BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Kosi01/cm_J_Kosi02/cm_J_SiriDam_L");
                this._siriDamR = this.transform.Find("BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Kosi01/cm_J_Kosi02/cm_J_SiriDam_R");
                this._kosi = this.transform.Find("BodyTop/p_cm_anim/cm_J_Root/cm_N_height/cm_J_Hips/cm_J_Kosi01/cm_J_Kosi02/cm_J_Kosi02_s");
            }
#elif KOIKATSU
            this._siriDamL = this.transform.FindDescendant("cf_d_siri_L");
            this._siriDamR = this.transform.FindDescendant("cf_d_siri_R");
            this._kosi = this.transform.FindDescendant("cf_s_waist02");
#endif
            this._leftFoot2 = this._body.solver.leftLegMapping.bone3.GetChild(0);
            this._leftFoot2OriginalRotation = this._leftFoot2.localRotation;
            this._leftFoot2ParentOriginalRotation = 357.7f;

            this._rightFoot2 = this._body.solver.rightLegMapping.bone3.GetChild(0);
            this._rightFoot2OriginalRotation = this._rightFoot2.localRotation;
            this._rightFoot2ParentOriginalRotation = 357.7f;


            this._siriDamLOriginalRotation = this._siriDamL.localRotation;
            this._siriDamROriginalRotation = this._siriDamR.localRotation;
            this._kosiOriginalRotation = this._kosi.localRotation;

            this._dynamicBonesEditor = this.gameObject.AddComponent<DynamicBonesEditor>();
            this._modules.Add(this._dynamicBonesEditor);
            this._blendShapesEditor = this.gameObject.AddComponent<BlendShapesEditor>();
            this._blendShapesEditor.chara = this._chara;
            this._modules.Add(this._blendShapesEditor);

            this._currentModule = this._bonesEditor;
            this._currentModule.isEnabled = true;
            if (this._chara == null)
                UnityEngine.Debug.LogError("chara is null, this should not happen");
            IKSolver_Patches.onPostUpdate += this.IKSolverOnPostUpdate;
            IKExecutionOrder_Patches.onPostLateUpdate += this.IKExecutionOrderOnPostLateUpdate;
            FKCtrl_Patches.onPreLateUpdate += this.FKCtrlOnPreLateUpdate;
            FKCtrl_Patches.onPostLateUpdate += this.FKCtrlOnPostLateUpdate;
#if HONEYSELECT
            CharBody_Patches.onPreLateUpdate += this.CharBodyOnPreLateUpdate;
            CharBody_Patches.onPostLateUpdate += this.CharBodyOnPostLateUpdate;
#elif KOIKATSU
            Character_Patches.onPreLateUpdate += this.CharacterOnPreLateUpdate;
            Character_Patches.onPostLateUpdate += this.CharacterOnPostLateUpdate;
#endif
            OCIChar_ChangeChara_Patches.onChangeChara += this.OnCharacterReplaced;
            OCIChar_LoadClothesFile_Patches.onLoadClothesFile += this.OnLoadClothesFile;
            OCIChar_SetCoordinateInfo_Patches.onSetCoordinateInfo += this.OnCoordinateReplaced;
            MainWindow.self.onParentage += this.OnParentage;

            this.crotchJointCorrection = MainWindow.self.crotchCorrectionByDefault;
            this.leftFootJointCorrection = MainWindow.self.anklesCorrectionByDefault;
            this.rightFootJointCorrection = MainWindow.self.anklesCorrectionByDefault;
        }

        void Start()
        {
            this._cachedSpineStiffness = this._body.solver.spineStiffness;
            this._cachedPullBodyVertical = this._body.solver.pullBodyVertical;

            if (this._optimizeIK)
            {
                this._body.solver.spineStiffness = 0f;
                this._body.solver.pullBodyVertical = 0f;
            }
            else
            {
                this._body.solver.spineStiffness = this._cachedSpineStiffness;
                this._body.solver.pullBodyVertical = this._cachedPullBodyVertical;
            }
            this._body.solver.OnPreRead = this.IKSolverOnPreRead;
        }

        void Update()
        {
            if (this.isIKEnabled == false)
            {
                if (this._scheduleNextIKPostUpdate != null)
                {
                    Action tempAction = this._scheduleNextIKPostUpdate;
                    this._scheduleNextIKPostUpdate = null;
                    tempAction();
                }

                this.InitJointCorrection();
            }
        }

        void OnDestroy()
        {
            this._body.solver.spineStiffness = this._cachedSpineStiffness;
            this._body.solver.pullBodyVertical = this._cachedPullBodyVertical;
            IKSolver_Patches.onPostUpdate -= this.IKSolverOnPostUpdate;
            IKExecutionOrder_Patches.onPostLateUpdate -= this.IKExecutionOrderOnPostLateUpdate;
            FKCtrl_Patches.onPreLateUpdate -= this.FKCtrlOnPreLateUpdate;
            FKCtrl_Patches.onPostLateUpdate -= this.FKCtrlOnPostLateUpdate;
#if HONEYSELECT
            CharBody_Patches.onPreLateUpdate -= this.CharBodyOnPreLateUpdate;
            CharBody_Patches.onPostLateUpdate -= this.CharBodyOnPostLateUpdate;
#elif KOIKATSU
            Character_Patches.onPreLateUpdate -= this.CharacterOnPreLateUpdate;
            Character_Patches.onPostLateUpdate -= this.CharacterOnPostLateUpdate;
#endif
            OCIChar_ChangeChara_Patches.onChangeChara -= this.OnCharacterReplaced;
            OCIChar_LoadClothesFile_Patches.onLoadClothesFile -= this.OnLoadClothesFile;
            OCIChar_SetCoordinateInfo_Patches.onSetCoordinateInfo -= this.OnCoordinateReplaced;
            MainWindow.self.onParentage -= this.OnParentage;
        }
        #endregion

        #region Public Methods
        public void LoadFrom(PoseController other)
        {
            if (other == null)
                return;
            this.optimizeIK = other.optimizeIK;
            this.crotchJointCorrection = other.crotchJointCorrection;
            this.leftFootJointCorrection = other.leftFootJointCorrection;
            this.rightFootJointCorrection = other.rightFootJointCorrection;
            this._bonesEditor.LoadFrom(other._bonesEditor);
            if (this._isFemale)
                this._boobsEditor.LoadFrom(other._boobsEditor);
            this._dynamicBonesEditor.LoadFrom(other._dynamicBonesEditor);
            this._blendShapesEditor.LoadFrom(other._blendShapesEditor);
        }

        public void StartDrag(DragType dragType)
        {
            if (this._lockDrag)
                return;
            this.currentDragType = dragType;
        }

        public void StopDrag()
        {
            if (this._lockDrag)
                return;
            GuideCommand.EqualsInfo[] moveCommands = new GuideCommand.EqualsInfo[this._oldPosValues.Count];
            int i = 0;
            if (this.currentDragType == DragType.Position || this.currentDragType == DragType.Both)
            {
                foreach (KeyValuePair<int, Vector3> kvp in this._oldPosValues)
                {
                    moveCommands[i] = new GuideCommand.EqualsInfo()
                    {
                        dicKey = kvp.Key,
                        oldValue = kvp.Value,
                        newValue = Studio.Studio.Instance.dicChangeAmount[kvp.Key].pos
                    };
                    ++i;
                }
            }
            GuideCommand.EqualsInfo[] rotateCommands = new GuideCommand.EqualsInfo[this._oldRotValues.Count + this._additionalRotationEqualsCommands.Count];
            i = 0;
            if (this.currentDragType == DragType.Rotation || this.currentDragType == DragType.Both)
            {
                foreach (KeyValuePair<int, Vector3> kvp in this._oldRotValues)
                {
                    rotateCommands[i] = new GuideCommand.EqualsInfo()
                    {
                        dicKey = kvp.Key,
                        oldValue = kvp.Value,
                        newValue = Studio.Studio.Instance.dicChangeAmount[kvp.Key].rot
                    };
                    ++i;
                }
            }
            foreach (GuideCommand.EqualsInfo info in this._additionalRotationEqualsCommands)
            {
                rotateCommands[i] = info;
                ++i;
            }
            UndoRedoManager.Instance.Push(new Commands.MoveRotateEqualsCommand(moveCommands, rotateCommands));
            this.currentDragType = DragType.None;
            this._oldPosValues.Clear();
            this._oldRotValues.Clear();
            this._additionalRotationEqualsCommands.Clear();
        }

        public bool IsPartEnabled(FullBodyBipedEffector part)
        {
            return this.isIKEnabled && this._chara.listIKTarget[_effectorToIndex[part]].active;
        }

        public bool IsPartEnabled(FullBodyBipedChain part)
        {
            return this.isIKEnabled && this._chara.listIKTarget[_chainToIndex[part]].active;
        }

        public void SetBoneTargetRotation(FullBodyBipedEffector type, Quaternion targetRotation)
        {
            OCIChar.IKInfo info = this._chara.listIKTarget[_effectorToIndex[type]];
            if (this.isIKEnabled && info.active)
            {
                if (this.currentDragType != DragType.None)
                {
                    if (this._oldRotValues.ContainsKey(info.guideObject.dicKey) == false)
                        this._oldRotValues.Add(info.guideObject.dicKey, info.guideObject.changeAmount.rot);
                    info.guideObject.changeAmount.rot = targetRotation.eulerAngles;
                }
            }
        }

        public Quaternion GetBoneTargetRotation(FullBodyBipedEffector type)
        {
            OCIChar.IKInfo info = this._chara.listIKTarget[_effectorToIndex[type]];
            if (!this.isIKEnabled || info.active == false)
                return Quaternion.identity;
            return info.guideObject.transformTarget.localRotation;
        }

        public void SetBoneTargetPosition(FullBodyBipedEffector type, Vector3 targetPosition, bool world = true)
        {
            OCIChar.IKInfo info = this._chara.listIKTarget[_effectorToIndex[type]];
            if (this.isIKEnabled && info.active)
            {
                if (this.currentDragType != DragType.None)
                {
                    if (this._oldPosValues.ContainsKey(info.guideObject.dicKey) == false)
                        this._oldPosValues.Add(info.guideObject.dicKey, info.guideObject.changeAmount.pos);
                    info.guideObject.changeAmount.pos = world ? info.guideObject.transformTarget.parent.InverseTransformPoint(targetPosition) : targetPosition;
                }
            }
        }

        public Vector3 GetBoneTargetPosition(FullBodyBipedEffector type, bool world = true)
        {
            OCIChar.IKInfo info = this._chara.listIKTarget[_effectorToIndex[type]];
            if (!this.isIKEnabled || info.active == false)
                return Vector3.zero;
            return world ? info.guideObject.transformTarget.position : info.guideObject.transformTarget.localPosition;
        }

        public void SetBendGoalPosition(FullBodyBipedChain type, Vector3 targetPosition, bool world = true)
        {
            OCIChar.IKInfo info = this._chara.listIKTarget[_chainToIndex[type]];
            if (this.isIKEnabled && info.active)
            {
                if (this.currentDragType != DragType.None)
                {
                    if (this._oldPosValues.ContainsKey(info.guideObject.dicKey) == false)
                        this._oldPosValues.Add(info.guideObject.dicKey, info.guideObject.changeAmount.pos);
                    info.guideObject.changeAmount.pos = world ? info.guideObject.transformTarget.parent.InverseTransformPoint(targetPosition) : targetPosition;
                }
            }
        }

        public Vector3 GetBendGoalPosition(FullBodyBipedChain type, bool world = true)
        {
            OCIChar.IKInfo info = this._chara.listIKTarget[_chainToIndex[type]];
            if (!this.isIKEnabled || info.active == false)
                return Vector3.zero;
            return world ? info.guideObject.transformTarget.position : info.guideObject.transformTarget.localPosition;
        }

        public void CopyLimbToTwin(FullBodyBipedChain limb)
        {
            this._scheduleNextIKPostUpdate = this.CopyLimbToTwinInternal;
            this._nextLimbCopy = limb;
        }

        public void AdvancedModeWindow(int id)
        {
            GUILayout.BeginHorizontal();
            foreach (AdvancedModeModule module in this._modules)
            {
                if (GUILayout.Button(module.displayName))
                {
                    this._currentModule = module;
                    module.isEnabled = true;
                    foreach (AdvancedModeModule module2 in this._modules)
                    {
                        if (module2 != module)
                            module2.isEnabled = false;
                    }
                }
            }

            Color c = GUI.color;
            GUI.color = AdvancedModeModule._redColor;
            if (GUILayout.Button("Close", GUILayout.ExpandWidth(false)))
                this.drawAdvancedMode = false;
            GUI.color = c;
            GUILayout.EndHorizontal();
            this._currentModule.GUILogic();
            GUI.DragWindow();
        }

        public void SwapPose()
        {
            this._scheduleNextIKPostUpdate = this.SwapPoseInternal;
        }
        #endregion

        #region Private Methods
        private void IKSolverOnPreRead()
        {
            foreach (AdvancedModeModule module in this._modules)
                module.IKSolverOnPreRead();
        }

        private void IKSolverOnPostUpdate(IKSolver solver)
        {
            if (this._body.solver != solver)
                return;
            if (this._scheduleNextIKPostUpdate != null)
            {
                Action tempAction = this._scheduleNextIKPostUpdate;
                this._scheduleNextIKPostUpdate = null;
                tempAction();
            }
            this.InitJointCorrection();
            foreach (AdvancedModeModule module in this._modules)
                module.IKSolverOnPostUpdate();
        }

        private void IKExecutionOrderOnPostLateUpdate()
        {
            foreach (AdvancedModeModule module in this._modules)
                module.IKExecutionOrderOnPostLateUpdate();
        }

        private void FKCtrlOnPreLateUpdate(FKCtrl ctrl)
        {
            if (this._chara.fkCtrl != ctrl)
                return;
            foreach (AdvancedModeModule module in this._modules)
                module.FKCtrlOnPreLateUpdate();
        }

        private void FKCtrlOnPostLateUpdate(FKCtrl ctrl)
        {
            if (this._chara.fkCtrl != ctrl)
                return;
            foreach (AdvancedModeModule module in this._modules)
                module.FKCtrlOnPostLateUpdate();
        }

#if HONEYSELECT
        private void CharBodyOnPreLateUpdate(CharBody charBody)
        {
            if (this._chara.charBody != charBody)
                return;
            

            foreach (AdvancedModeModule module in this._modules)
                module.CharBodyPreLateUpdate();
        }

        private void CharBodyOnPostLateUpdate(CharBody charBody)
        {
            if (this._chara.charBody != charBody)
                return;
            this.ApplyJointCorrection();

            foreach (AdvancedModeModule module in this._modules)
                module.CharBodyPostLateUpdate();
        }
#elif KOIKATSU
        private void CharacterOnPreLateUpdate()
        {
            foreach (AdvancedModeModule module in this._modules)
                module.CharacterPreLateUpdate();
        }

        private void CharacterOnPostLateUpdate()
        {
            this.ApplyJointCorrection();

            foreach (AdvancedModeModule module in this._modules)
                module.CharacterPostLateUpdate();
        }
#endif
        private void OnCharacterReplaced(OCIChar chara)
        {
            if (this._chara != chara)
                return;
            foreach (AdvancedModeModule module in this._modules)
                module.OnCharacterReplaced();
        }
        private void OnLoadClothesFile(OCIChar chara)
        {
            if (this._chara != chara)
                return;
            foreach (AdvancedModeModule module in this._modules)
                module.OnLoadClothesFile();
        }

#if HONEYSELECT
        private void OnCoordinateReplaced(OCIChar chara, CharDefine.CoordinateType type, bool force)
#elif KOIKATSU
        private void OnCoordinateReplaced(OCIChar chara, ChaFileDefine.CoordinateType type, bool force)
#endif
        {
            if (this._chara != chara)
                return;
            foreach (AdvancedModeModule module in this._modules)
                module.OnCoordinateReplaced(type, force);
        }

        private void OnParentage(TreeNodeObject parent, TreeNodeObject child)
        {
            foreach (AdvancedModeModule module in this._modules)
                module.OnParentage(parent, child);
        }

        private FullBodyBipedEffector GetTwinBone(FullBodyBipedEffector effector)
        {
            switch (effector)
            {
                case FullBodyBipedEffector.LeftFoot:
                    return FullBodyBipedEffector.RightFoot;
                case FullBodyBipedEffector.RightFoot:
                    return FullBodyBipedEffector.LeftFoot;
                case FullBodyBipedEffector.LeftHand:
                    return FullBodyBipedEffector.RightHand;
                case FullBodyBipedEffector.RightHand:
                    return FullBodyBipedEffector.LeftHand;
                case FullBodyBipedEffector.LeftThigh:
                    return FullBodyBipedEffector.RightThigh;
                case FullBodyBipedEffector.RightThigh:
                    return FullBodyBipedEffector.LeftThigh;
                case FullBodyBipedEffector.LeftShoulder:
                    return FullBodyBipedEffector.RightShoulder;
                case FullBodyBipedEffector.RightShoulder:
                    return FullBodyBipedEffector.LeftShoulder;
            }
            return effector;
        }

        private FullBodyBipedChain GetTwinBone(FullBodyBipedChain chain)
        {
            switch (chain)
            {
                case FullBodyBipedChain.LeftArm:
                    return FullBodyBipedChain.RightArm;
                case FullBodyBipedChain.RightArm:
                    return FullBodyBipedChain.LeftArm;
                case FullBodyBipedChain.LeftLeg:
                    return FullBodyBipedChain.RightLeg;
                case FullBodyBipedChain.RightLeg:
                    return FullBodyBipedChain.LeftLeg;
            }
            return chain;
        }

        private void CopyLimbToTwinInternal()
        {
            FullBodyBipedChain limb = this._nextLimbCopy;
            FullBodyBipedEffector effectorSrc;
            FullBodyBipedChain bendGoalSrc;
            FullBodyBipedEffector effectorDest;
            FullBodyBipedChain bendGoalDest;
            Transform effectorSrcRealBone;
            Transform effectorDestRealBone;
            Transform root;
            switch (limb)
            {
                case FullBodyBipedChain.LeftArm:
                    effectorSrc = FullBodyBipedEffector.LeftHand;
                    effectorSrcRealBone = this._body.references.leftHand;
                    effectorDestRealBone = this._body.references.rightHand;
                    root = this._body.solver.spineMapping.spineBones[this._body.solver.spineMapping.spineBones.Length - 2];
                    break;
                case FullBodyBipedChain.LeftLeg:
                    effectorSrc = FullBodyBipedEffector.LeftFoot;
                    effectorSrcRealBone = this._body.references.leftFoot;
                    effectorDestRealBone = this._body.references.rightFoot;
                    root = this._body.solver.spineMapping.spineBones[0];
                    break;
                case FullBodyBipedChain.RightArm:
                    effectorSrc = FullBodyBipedEffector.RightHand;
                    effectorSrcRealBone = this._body.references.rightHand;
                    effectorDestRealBone = this._body.references.leftHand;
                    root = this._body.solver.spineMapping.spineBones[this._body.solver.spineMapping.spineBones.Length - 2];
                    break;
                case FullBodyBipedChain.RightLeg:
                    effectorSrc = FullBodyBipedEffector.RightFoot;
                    effectorSrcRealBone = this._body.references.rightFoot;
                    effectorDestRealBone = this._body.references.leftFoot;
                    root = this._body.solver.spineMapping.spineBones[0];
                    break;
                default:
                    effectorSrc = FullBodyBipedEffector.RightHand;
                    effectorSrcRealBone = null;
                    effectorDestRealBone = null;
                    root = null;
                    break;
            }
            bendGoalSrc = limb;
            bendGoalDest = this.GetTwinBone(limb);
            effectorDest = this.GetTwinBone(effectorSrc);

            Vector3 localPos = root.InverseTransformPoint(this._chara.listIKTarget[_effectorToIndex[effectorSrc]].guideObject.transformTarget.position);
            localPos.x *= -1f;
            Vector3 effectorPosition = root.TransformPoint(localPos);
            localPos = root.InverseTransformPoint(this._chara.listIKTarget[_chainToIndex[bendGoalSrc]].guideObject.transformTarget.position);
            localPos.x *= -1f;
            Vector3 bendGoalPosition = root.TransformPoint(localPos);
            this.StartDrag(DragType.Both);
            this._lockDrag = true;


            this.SetBoneTargetPosition(effectorDest, effectorPosition);
            this.SetBendGoalPosition(bendGoalDest, bendGoalPosition);
            this.SetBoneTargetRotation(effectorDest, this.GetBoneTargetRotation(effectorDest));

            this._scheduleNextIKPostUpdate = () =>
            {
                Quaternion rot = effectorSrcRealBone.localRotation;
                rot = new Quaternion(rot.x, -rot.y, -rot.z, rot.w);
                effectorDestRealBone.localRotation = rot; //Setting real bone local rotation
                OCIChar.IKInfo effectorDestInfo = this._chara.listIKTarget[_effectorToIndex[effectorDest]];
                effectorDestInfo.guideObject.transformTarget.rotation = effectorDestRealBone.rotation; //Using real bone rotation to set IK target rotation;
                this.SetBoneTargetRotation(effectorDest, effectorDestInfo.guideObject.transformTarget.localRotation); //Setting again the IK target with its own local rotation through normal means so it isn't ignored by neo while saving
                this._lockDrag = false;
                this.StopDrag();
            };
        }

        private void SwapPoseInternal()
        {
            this.StartDrag(DragType.Both);
            this._lockDrag = true;

            this._additionalRotationEqualsCommands = new List<GuideCommand.EqualsInfo>();
            HashSet<Transform> done = new HashSet<Transform>();
            foreach (OCIChar.BoneInfo bone in this._chara.listBones)
            {
                Transform twinBoneTransform = null;
                Transform boneTransform = bone.guideObject.transformTarget;
                switch (bone.boneGroup)
                {
                    case OIBoneInfo.BoneGroup.Hair:
                        continue;

                    case OIBoneInfo.BoneGroup.Skirt:
                        twinBoneTransform = this.GetSkirtTwinBone(boneTransform);
                        break;

                    case OIBoneInfo.BoneGroup.Neck:
                    case OIBoneInfo.BoneGroup.Body:
                    case OIBoneInfo.BoneGroup.Breast:
                    case OIBoneInfo.BoneGroup.RightLeg:
                    case OIBoneInfo.BoneGroup.LeftLeg:
                    case OIBoneInfo.BoneGroup.RightArm:
                    case OIBoneInfo.BoneGroup.LeftArm:
                    case OIBoneInfo.BoneGroup.RightHand:
                    case OIBoneInfo.BoneGroup.LeftHand:
                    default:
                        twinBoneTransform = this._bonesEditor.GetTwinBone(boneTransform);
                        if (twinBoneTransform == null)
                            twinBoneTransform = boneTransform;
                        break;
                }
                OCIChar.BoneInfo twinBone;
                if (twinBoneTransform != null && this._bonesEditor.fkObjects.TryGetValue(twinBoneTransform.gameObject, out twinBone))
                {
                    if (done.Contains(boneTransform) || done.Contains(twinBoneTransform))
                        continue;
                    done.Add(boneTransform);
                    if (twinBoneTransform != boneTransform)
                        done.Add(twinBoneTransform);

                    if (twinBoneTransform == boneTransform)
                    {
                        Quaternion rot = Quaternion.Euler(bone.guideObject.changeAmount.rot);
                        rot = new Quaternion(rot.x, -rot.y, -rot.z, rot.w);

                        Vector3 oldRotValue = bone.guideObject.changeAmount.rot;

                        bone.guideObject.changeAmount.rot = rot.eulerAngles;

                        this._additionalRotationEqualsCommands.Add(new GuideCommand.EqualsInfo()
                        {
                            dicKey = bone.guideObject.dicKey,
                            oldValue = oldRotValue,
                            newValue = bone.guideObject.changeAmount.rot
                        });
                    }
                    else
                    {
                        Quaternion rot = Quaternion.Euler(bone.guideObject.changeAmount.rot);
                        rot = new Quaternion(rot.x, -rot.y, -rot.z, rot.w);
                        Quaternion twinRot = Quaternion.Euler(twinBone.guideObject.changeAmount.rot);
                        twinRot = new Quaternion(twinRot.x, -twinRot.y, -twinRot.z, twinRot.w);

                        Vector3 oldRotValue = bone.guideObject.changeAmount.rot;
                        Vector3 oldTwinRotValue = twinBone.guideObject.changeAmount.rot;

                        bone.guideObject.changeAmount.rot = twinRot.eulerAngles;
                        twinBone.guideObject.changeAmount.rot = rot.eulerAngles;

                        this._additionalRotationEqualsCommands.Add(new GuideCommand.EqualsInfo()
                        {
                            dicKey = bone.guideObject.dicKey,
                            oldValue = oldRotValue,
                            newValue = bone.guideObject.changeAmount.rot

                        });
                        this._additionalRotationEqualsCommands.Add(new GuideCommand.EqualsInfo()
                        {
                            dicKey = twinBone.guideObject.dicKey,
                            oldValue = oldTwinRotValue,
                            newValue = twinBone.guideObject.changeAmount.rot
                        });
                    }
                }
            }

            foreach (KeyValuePair<FullBodyBipedEffector, int> pair in _effectorToIndex)
            {
                switch (pair.Key)
                {
                    case FullBodyBipedEffector.Body:
                    case FullBodyBipedEffector.LeftShoulder:
                    case FullBodyBipedEffector.LeftHand:
                    case FullBodyBipedEffector.LeftThigh:
                    case FullBodyBipedEffector.LeftFoot:
                        FullBodyBipedEffector twin = this.GetTwinBone(pair.Key);
                        Vector3 position = this._chara.listIKTarget[pair.Value].guideObject.transformTarget.localPosition;
                        position.x *= -1f;
                        Vector3 twinPosition = this._chara.listIKTarget[_effectorToIndex[twin]].guideObject.transformTarget.localPosition;
                        twinPosition.x *= -1f;
                        this.SetBoneTargetPosition(pair.Key, twinPosition, false);
                        this.SetBoneTargetPosition(twin, position, false);
                        break;
                }
            }

            foreach (KeyValuePair<FullBodyBipedChain, int> pair in _chainToIndex)
            {
                switch (pair.Key)
                {
                    case FullBodyBipedChain.LeftArm:
                    case FullBodyBipedChain.LeftLeg:
                        FullBodyBipedChain twin = this.GetTwinBone(pair.Key);
                        Vector3 position = this._chara.listIKTarget[pair.Value].guideObject.transformTarget.localPosition;
                        position.x *= -1f;
                        Vector3 twinPosition = this._chara.listIKTarget[_chainToIndex[twin]].guideObject.transformTarget.localPosition;
                        twinPosition.x *= -1f;
                        this.SetBendGoalPosition(pair.Key, twinPosition, false);
                        this.SetBendGoalPosition(twin, position, false);
                        break;
                }
            }

            this._scheduleNextIKPostUpdate = () =>
            {
                foreach (KeyValuePair<FullBodyBipedEffector, int> pair in _effectorToIndex)
                {
                    switch (pair.Key)
                    {
                        case FullBodyBipedEffector.LeftHand:
                        case FullBodyBipedEffector.LeftFoot:
                            FullBodyBipedEffector twin = this.GetTwinBone(pair.Key);
                            Quaternion rot = this._chara.listIKTarget[pair.Value].guideObject.transformTarget.localRotation;
                            rot = new Quaternion(-rot.x, rot.y, rot.z, -rot.w);
                            Quaternion twinRot = this._chara.listIKTarget[_effectorToIndex[twin]].guideObject.transformTarget.localRotation;
                            twinRot = new Quaternion(-twinRot.x, twinRot.y, twinRot.z, -twinRot.w);
                            this.SetBoneTargetRotation(pair.Key, twinRot);
                            this.SetBoneTargetRotation(twin, rot);
                            break;
                    }
                }

                this._lockDrag = false;
                this.StopDrag();
            };
        }

        private Transform GetSkirtTwinBone(Transform bone)
        {
            int id = int.Parse(bone.name.Substring(8, 2));
            string path = "";
            switch (id)
            {
                case 00:
                case 04:
                    return bone;
                    
                case 01:
                    path = bone.GetPathFrom(this.transform).Replace("cf_J_sk_01", "cf_J_sk_07");
                    break;
                case 07:
                    path = bone.GetPathFrom(this.transform).Replace("cf_J_sk_07", "cf_J_sk_01");
                    break;
                case 02:
                    path = bone.GetPathFrom(this.transform).Replace("cf_J_sk_02", "cf_J_sk_06");
                    break;
                case 06:
                    path = bone.GetPathFrom(this.transform).Replace("cf_J_sk_06", "cf_J_sk_02");
                    break;
                case 03:
                    path = bone.GetPathFrom(this.transform).Replace("cf_J_sk_03", "cf_J_sk_05");
                    break;
                case 05:
                    path = bone.GetPathFrom(this.transform).Replace("cf_J_sk_05", "cf_J_sk_03");
                    break;
            }
            return this.transform.Find(path);
        }

        private void InitJointCorrection()
        {
            if (this.crotchJointCorrection)
            {
                this._siriDamLRotation = Quaternion.Lerp(Quaternion.identity, this._body.solver.leftLegMapping.bone1.rotation, 0.4f);
                this._siriDamRRotation = Quaternion.Lerp(Quaternion.identity, this._body.solver.rightLegMapping.bone1.rotation, 0.4f);
                this._kosiRotation = Quaternion.Lerp(Quaternion.identity, Quaternion.Lerp(this._body.solver.leftLegMapping.bone1.localRotation, this._body.solver.rightLegMapping.bone1.localRotation, 0.5f), 0.25f);
            }

            if (this.leftFootJointCorrection)
                this._leftFoot2Rotation = Mathf.LerpAngle(0f, this._leftFoot2.parent.localRotation.eulerAngles.x - this._leftFoot2ParentOriginalRotation, 0.9f);

            if (this.rightFootJointCorrection)
                this._rightFoot2Rotation = Mathf.LerpAngle(0f, this._rightFoot2.parent.localRotation.eulerAngles.x - this._rightFoot2ParentOriginalRotation, 0.9f);
        }

        private void ApplyJointCorrection()
        {
            if (this.crotchJointCorrection)
            {
                this._siriDamL.localRotation = this._siriDamLRotation;
                this._siriDamR.localRotation = this._siriDamRRotation;
                this._kosi.localRotation = this._kosiRotation;
            }
            else if (this._lastCrotchJointCorrection)
            {
                this._siriDamL.localRotation = this._siriDamLOriginalRotation;
                this._siriDamR.localRotation = this._siriDamROriginalRotation;
                this._kosi.localRotation = this._kosiOriginalRotation;
            }

            if (this.leftFootJointCorrection)
                this._leftFoot2.localRotation = Quaternion.AngleAxis(this._leftFoot2Rotation, Vector3.right);
            else if (this._lastLeftFootJointCorrection)
                this._leftFoot2.localRotation = this._leftFoot2OriginalRotation;

            if (this.rightFootJointCorrection)
                this._rightFoot2.localRotation = Quaternion.AngleAxis(this._rightFoot2Rotation, Vector3.right);
            else if (this._lastrightFootJointCorrection)
                this._rightFoot2.localRotation = this._rightFoot2OriginalRotation;

            this._lastCrotchJointCorrection = this.crotchJointCorrection;
            this._lastLeftFootJointCorrection = this.leftFootJointCorrection;
            this._lastrightFootJointCorrection = this.rightFootJointCorrection;
        }
        #endregion

        #region Saves
        public void ScheduleLoad(XmlNode node)
        {
            this.StartCoroutine(this.LoadDefaultVersion_Routine(node.CloneNode(true)));
        }

        public int SaveXml(XmlTextWriter xmlWriter)
        {
            int written = 0;
            if (this.optimizeIK == false)
            {
                xmlWriter.WriteAttributeString("optimizeIK", XmlConvert.ToString(this.optimizeIK));
                written++;
            }
            xmlWriter.WriteAttributeString("crotchCorrection", XmlConvert.ToString(this.crotchJointCorrection));
            xmlWriter.WriteAttributeString("leftAnkleCorrection", XmlConvert.ToString(this.leftFootJointCorrection));
            xmlWriter.WriteAttributeString("rightAnkleCorrection", XmlConvert.ToString(this.rightFootJointCorrection));
            written++;
            foreach (AdvancedModeModule module in this._modules)
                written += module.SaveXml(xmlWriter);
            return written;
        }

        private IEnumerator LoadDefaultVersion_Routine(XmlNode xmlNode)
        {
            yield return null;
            yield return null;
            yield return null;
            this.optimizeIK = xmlNode.Attributes?["optimizeIK"] == null || XmlConvert.ToBoolean(xmlNode.Attributes["optimizeIK"].Value);
            this.crotchJointCorrection = xmlNode.Attributes?["crotchCorrection"] != null && XmlConvert.ToBoolean(xmlNode.Attributes["crotchCorrection"].Value);
            this.leftFootJointCorrection = xmlNode.Attributes?["leftAnkleCorrection"] != null && XmlConvert.ToBoolean(xmlNode.Attributes["leftAnkleCorrection"].Value);
            this.rightFootJointCorrection = xmlNode.Attributes?["rightAnkleCorrection"] != null && XmlConvert.ToBoolean(xmlNode.Attributes["rightAnkleCorrection"].Value);

            foreach (AdvancedModeModule module in this._modules)
                module.LoadXml(xmlNode);
        }
        #endregion
    }
}