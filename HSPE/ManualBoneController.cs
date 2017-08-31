using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using Manager;
using RootMotion.FinalIK;
using UnityEngine;

namespace HSPE
{
    public class ManualBoneController : MonoBehaviour
    {
        #region Private Static Variables
        private static readonly Vector3 _armDamScale = new Vector3(0.82f, 1f, 1.1f);
        #endregion

        #region Private Types
        private enum CoordType
        {
            Position,
            Rotation,
            Scale
        }

        private enum SelectedTab
        {
            BonesPosition = 0,
            BoobsEditor,
            DynamicBonesEditor,
            Count
        }

        private class TransformData
        {
            public EditableValue<Vector3> position;
            public EditableValue<Quaternion> rotation;
            public EditableValue<Vector3> scale;
            public EditableValue<Vector3> originalPosition;
            public EditableValue<Quaternion> originalRotation;
            public EditableValue<Vector3> originalScale;
        }

        private class BoobData
        {
            public EditableValue<Vector3> originalGravity;
            public EditableValue<Vector3> originalForce;
        }

        private class ColliderData
        {
            public EditableValue<Vector3> originalCenter;
            public EditableValue<float> originalRadius;
            public EditableValue<float> originalHeight;
            public EditableValue<DynamicBoneCollider.Direction> originalDirection;
            public EditableValue<DynamicBoneCollider.Bound> originalBound;
        }

        private class DynamicBoneData
        {
            public EditableValue<float> originalWeight;
            public EditableValue<Vector3> originalGravity;
            public EditableValue<Vector3> originalForce;
            public EditableValue<DynamicBone.FreezeAxis> originalFreezeAxis;
        }

        private delegate void TabDelegate();
        #endregion

        #region Private Variables
        private Animator _animator;
        private FullBodyBipedIK _body;
        private CameraGL _cam;
        private Material _mat;
        private Material _xMat;
        private Material _yMat;
        private Material _zMat;
        private Material _colliderMat;
        private Transform _boneTarget;
        private DynamicBoneCollider _colliderTarget;
        private DynamicBone_Ver02 _rightBoob;
        private DynamicBone_Ver02 _leftBoob;
        private readonly Dictionary<FullBodyBipedEffector, int> _effectorToIndex = new Dictionary<FullBodyBipedEffector, int>(); 
        private readonly HashSet<GameObject> _openedBones = new HashSet<GameObject>();
        private readonly HashSet<Transform> _colliderObjects = new HashSet<Transform>();
        private DynamicBone[] _dynamicBones = new DynamicBone[0];
        private CoordType _boneEditionCoordType = CoordType.Rotation;
        private Vector2 _boneEditionScroll;
        private float _inc = 1f;
        private int _incIndex = 0;
        private readonly Dictionary<GameObject, TransformData> _dirtyBones = new Dictionary<GameObject, TransformData>();
        private readonly Dictionary<DynamicBone_Ver02, BoobData> _dirtyBoobs = new Dictionary<DynamicBone_Ver02, BoobData>(2);
        private readonly Dictionary<DynamicBoneCollider, ColliderData> _dirtyColliders = new Dictionary<DynamicBoneCollider, ColliderData>();
        private readonly Dictionary<DynamicBone, DynamicBoneData> _dirtyDynamicBones = new Dictionary<DynamicBone, DynamicBoneData>();
        private bool _isFemale = false;
        private float _cachedSpineStiffness;
        private float _cachedPullBodyVertical;
        private int _cachedSolverIterations;
        private Transform _legKneeDamL;
        private Transform _legKneeDamR;
        private Transform _legKneeBackL;
        private Transform _legKneeBackR;
        private Transform _legUpL;
        private Transform _legUpR;
        private Transform _elbowDamL;
        private Transform _elbowDamR;
        private Transform _armUpDamL1;
        private Transform _armUpDamR1;
        private Transform _armUpDamL2;
        private Transform _armUpDamR2;
        private Transform _armUpDamL3;
        private Transform _armUpDamR3;
        private Transform _armElbouraDamL;
        private Transform _armElbouraDamR;
        private Transform _armLow1L;
        private Transform _armLow1R;
        private Transform _armLow2L;
        private Transform _armLow2R;
        private Transform _armLow3L;
        private Transform _armLow3R;
        private Transform _siriDamL;
        private Transform _siriDamR;
        private Transform _kosi;
        private readonly Dictionary<Transform, string> _boneEditionShortcuts = new Dictionary<Transform, string>();
        private SelectedTab _selectedTab = SelectedTab.BonesPosition;
        private readonly Dictionary<SelectedTab, TabDelegate> _tabFunctions = new Dictionary<SelectedTab, TabDelegate>();
        private float _repeatTimer = 0f;
        private bool _repeatCalled = false;
        private float _repeatBeforeDuration = 0.5f;
        private Rect _colliderEditRect = new Rect(Screen.width - 650, Screen.height - 690, 450, 300);
        private Vector2 _dynamicBonesScroll;
        private DynamicBone _dynamicBoneTarget;
        private bool _removeShortcutMode;
        private readonly Quaternion _shounderBaseRotL = Quaternion.AngleAxis(90, Vector3.up) * Quaternion.AngleAxis(70, Vector3.right);
        private readonly Quaternion _shounderBaseRotR = Quaternion.AngleAxis(90, Vector3.up) * Quaternion.AngleAxis(70, Vector3.right);
        #endregion

        #region Public Accessors
        public StudioChara chara { get; set; }
        public bool isEnabled { get { return this.chara.ikCtrl.ikEnable; } }
        public bool draw { get; set; }
        public bool forceBendGoalsWeight { get; set; }
        public Rect colliderEditRect { get { return this._colliderEditRect; } }
        public bool colliderEditEnabled { get { return this._colliderTarget != null; } }
        #endregion

        #region Unity Methods
        void Awake()
        {
            this._isFemale = this.GetComponentInParent<CharInfo>() is CharFemale;
            this._tabFunctions.Add(SelectedTab.BonesPosition, this.BonesPosition);
            this._tabFunctions.Add(SelectedTab.BoobsEditor, this.BoobsEditor);
            this._tabFunctions.Add(SelectedTab.DynamicBonesEditor, this.DynamicBonesEditor);
            this._effectorToIndex.Add(FullBodyBipedEffector.Body, 0);
            this._effectorToIndex.Add(FullBodyBipedEffector.LeftThigh, 1);
            this._effectorToIndex.Add(FullBodyBipedEffector.RightThigh, 2);
            this._effectorToIndex.Add(FullBodyBipedEffector.LeftHand, 3);
            this._effectorToIndex.Add(FullBodyBipedEffector.RightHand, 4);
            this._effectorToIndex.Add(FullBodyBipedEffector.LeftFoot, 5);
            this._effectorToIndex.Add(FullBodyBipedEffector.RightFoot, 6);
            this._effectorToIndex.Add(FullBodyBipedEffector.LeftShoulder, 7);
            this._effectorToIndex.Add(FullBodyBipedEffector.RightShoulder, 8);
            this._mat = new Material(Shader.Find("Unlit/Transparent"));
            this._xMat = new Material(Shader.Find("Custom/SpriteDefaultZAlways"));
            this._xMat.color = Color.red;
            this._yMat = new Material(Shader.Find("Custom/SpriteDefaultZAlways"));
            this._yMat.color = Color.green;
            this._zMat = new Material(Shader.Find("Custom/SpriteDefaultZAlways"));
            this._zMat.color = Color.blue;
            this._colliderMat = new Material(Shader.Find("Custom/SpriteDefaultZAlways"));
            this._colliderMat.color = Color.Lerp(Color.green, Color.white, 0.5f);
            this._cam = Camera.current.GetComponent<CameraGL>();
            if (this._cam == null)
                this._cam = Camera.current.gameObject.AddComponent<CameraGL>();
            this._cam.onPostRender += this.DrawGizmos;
            this._body = this.GetComponent<FullBodyBipedIK>();
            this._body.solver.OnPostSolve = this.OnPostSolve;
            this._animator = this.GetComponent<Animator>();
            foreach (DynamicBoneCollider c in this.GetComponentsInChildren<DynamicBoneCollider>())
                this._colliderObjects.Add(c.transform);
            this.forceBendGoalsWeight = true;
            this._dynamicBones = this.transform.parent.GetComponentsInChildren<DynamicBone>();
            if (this._dynamicBones != null)
                UnityEngine.Debug.Log(this._dynamicBones.Length);
        }

        void Start()
        {
            this._isFemale = this.chara is StudioFemale;

            this._legKneeDamL = this._body.solver.leftLegMapping.bone1.GetChild(0);
            this._legKneeDamR = this._body.solver.rightLegMapping.bone1.GetChild(0);
            if (this._isFemale)
            {
                this._legKneeBackL = this._body.solver.leftLegMapping.bone1.FindDescendant("cf_J_LegKnee_back_L");
                this._legKneeBackR = this._body.solver.rightLegMapping.bone1.FindDescendant("cf_J_LegKnee_back_R");
                this._legUpL = this.transform.FindDescendant("cf_J_LegUpDam_L");
                this._legUpR = this.transform.FindDescendant("cf_J_LegUpDam_R");
                this._elbowDamL = this.transform.FindDescendant("cf_J_ArmElbo_dam_01_L");
                this._elbowDamR = this.transform.FindDescendant("cf_J_ArmElbo_dam_01_R");
                this._armUpDamL1 = this.transform.FindDescendant("cf_J_ArmUp01_dam_L");
                this._armUpDamR1 = this.transform.FindDescendant("cf_J_ArmUp01_dam_R");
                this._armUpDamL2 = this.transform.FindDescendant("cf_J_ArmUp02_dam_L");
                this._armUpDamR2 = this.transform.FindDescendant("cf_J_ArmUp02_dam_R");
                this._armUpDamL3 = this.transform.FindDescendant("cf_J_ArmUp03_dam_L");
                this._armUpDamR3 = this.transform.FindDescendant("cf_J_ArmUp03_dam_R");
                this._armElbouraDamL = this.transform.FindDescendant("cf_J_ArmElboura_dam_L");
                this._armElbouraDamR = this.transform.FindDescendant("cf_J_ArmElboura_dam_R");
                this._armLow1L = this._body.solver.leftArmMapping.bone2.FindChild("cf_J_ArmLow01_s_L");
                this._armLow1R = this._body.solver.rightArmMapping.bone2.FindChild("cf_J_ArmLow01_s_R");
                this._armLow2L = this._body.solver.leftArmMapping.bone2.FindChild("cf_J_ArmLow02_dam_L");
                this._armLow2R = this._body.solver.rightArmMapping.bone2.FindChild("cf_J_ArmLow02_dam_R");
                this._armLow3L = this._body.solver.leftArmMapping.bone2.FindChild("cf_J_Hand_Wrist_dam_L");
                this._armLow3R = this._body.solver.rightArmMapping.bone2.FindChild("cf_J_Hand_Wrist_dam_R");
                this._siriDamL = this.transform.FindDescendant("cf_J_SiriDam_L");
                this._siriDamR = this.transform.FindDescendant("cf_J_SiriDam_R");
                this._kosi = this.transform.FindDescendant("cf_J_Kosi02_s");
                this._boneEditionShortcuts.Add(this.transform.FindDescendant("cf_J_Hand_s_L"), "L. Hand");
                this._boneEditionShortcuts.Add(this.transform.FindDescendant("cf_J_Hand_s_R"), "R. Hand");
                this._boneEditionShortcuts.Add(this.transform.FindDescendant("cf_J_Foot02_L"), "L. Foot");
                this._boneEditionShortcuts.Add(this.transform.FindDescendant("cf_J_Foot02_R"), "R. Foot");
                this._boneEditionShortcuts.Add(this.transform.FindDescendant("cf_J_FaceRoot"), "Face");
                this._leftBoob = ((CharFemaleBody) this.chara.body).getDynamicBone(CharFemaleBody.DynamicBoneKind.BreastL);
                this._rightBoob = ((CharFemaleBody) this.chara.body).getDynamicBone(CharFemaleBody.DynamicBoneKind.BreastR);
            }
            else
            {
                this._legKneeBackL = this._body.solver.leftLegMapping.bone1.FindDescendant("cm_J_LegKnee_back_s_L");
                this._legKneeBackR = this._body.solver.rightLegMapping.bone1.FindDescendant("cm_J_LegKnee_back_s_R");
                this._legUpL = this.transform.FindDescendant("cm_J_LegUpDam_L");
                this._legUpR = this.transform.FindDescendant("cm_J_LegUpDam_R");
                this._elbowDamL = this.transform.FindDescendant("cm_J_ArmElbo_dam_02_L");
                this._elbowDamR = this.transform.FindDescendant("cm_J_ArmElbo_dam_02_R");
                this._armUpDamL1 = this.transform.FindDescendant("cm_J_ArmUp01_dam_L");
                this._armUpDamR1 = this.transform.FindDescendant("cm_J_ArmUp01_dam_R");
                this._armUpDamL2 = this.transform.FindDescendant("cm_J_ArmUp02_dam_L");
                this._armUpDamR2 = this.transform.FindDescendant("cm_J_ArmUp02_dam_R");
                this._armUpDamL3 = this.transform.FindDescendant("cm_J_ArmUp03_dam_L");
                this._armUpDamR3 = this.transform.FindDescendant("cm_J_ArmUp03_dam_R");
                this._armElbouraDamL = this.transform.FindDescendant("cm_J_ArmElboura_dam_L");
                this._armElbouraDamR = this.transform.FindDescendant("cm_J_ArmElboura_dam_R");
                this._armLow1L = this._body.solver.leftArmMapping.bone2.FindChild("cm_J_ArmLow01_s_L");
                this._armLow1R = this._body.solver.rightArmMapping.bone2.FindChild("cm_J_ArmLow01_s_R");
                this._armLow2L = this._body.solver.leftArmMapping.bone2.FindChild("cm_J_ArmLow02_dam_L");
                this._armLow2R = this._body.solver.rightArmMapping.bone2.FindChild("cm_J_ArmLow02_dam_R");
                this._armLow3L = this._body.solver.leftArmMapping.bone2.FindChild("cm_J_Hand_Wrist_dam_L");
                this._armLow3R = this._body.solver.rightArmMapping.bone2.FindChild("cm_J_Hand_Wrist_dam_R");
                this._siriDamL = this.transform.FindDescendant("cm_J_SiriDam_L");
                this._siriDamR = this.transform.FindDescendant("cm_J_SiriDam_R");
                this._kosi = this.transform.FindDescendant("cm_J_Kosi02_s");
                this._boneEditionShortcuts.Add(this.transform.FindDescendant("cm_J_Hand_s_L"), "L. Hand");
                this._boneEditionShortcuts.Add(this.transform.FindDescendant("cm_J_Hand_s_R"), "R. Hand");
                this._boneEditionShortcuts.Add(this.transform.FindDescendant("cm_J_Foot02_L"), "L. Foot");
                this._boneEditionShortcuts.Add(this.transform.FindDescendant("cm_J_Foot02_R"), "R. Foot");
                this._boneEditionShortcuts.Add(this.transform.FindDescendant("cm_J_FaceRoot"), "Face");
            }

            this._cachedSpineStiffness = this._body.solver.spineStiffness;
            this._cachedPullBodyVertical = this._body.solver.pullBodyVertical;
            this._cachedSolverIterations = this._body.solver.iterations;
            this._body.solver.spineStiffness = 0f;
            this._body.solver.pullBodyVertical = 0f;
            this._body.solver.iterations = 1;
            this._body.fixTransforms = true;
        }

        void Update()
        {
            if (this._repeatCalled)
                this._repeatTimer += Time.unscaledDeltaTime;
            else
                this._repeatTimer = 0f;
            this._repeatCalled = false;
            DynamicBone[] dynamicBones = this.transform.parent.GetComponentsInChildren<DynamicBone>();
            bool shouldCleanList = false;
            foreach (DynamicBone db in this._dynamicBones)
                if (dynamicBones.Contains(db) == false)
                {
                    shouldCleanList = true;
                    break;
                }
            if (shouldCleanList)
            {
                List<DynamicBone> toDelete = new List<DynamicBone>();
                foreach (KeyValuePair<DynamicBone, DynamicBoneData> kvp in this._dirtyDynamicBones)
                    if (dynamicBones.Contains(kvp.Key) == false)
                        toDelete.Add(kvp.Key);
                foreach (DynamicBone db in toDelete)
                    this._dirtyDynamicBones.Remove(db);
                this._dynamicBoneTarget = null;
            }
            this._dynamicBones = dynamicBones;
        }

        void LateUpdate()
        {
            if (!this.isEnabled)
                return;
            if (this.forceBendGoalsWeight)
                for (int i = 0; i < 4; ++i)
                    this.chara.ikCtrl.drivingRig.bendGoals[i].weight = 1f;
            this._body.solver.Update();
        }

        void OnDestroy()
        {
            this._cam.onPostRender -= this.DrawGizmos;
            this._animator.enabled = true;
            this._body.solver.spineStiffness = this._cachedSpineStiffness;
            this._body.solver.pullBodyVertical = this._cachedPullBodyVertical;
            this._body.solver.iterations = this._cachedSolverIterations;
        }

        void OnGUI()
        {
            GUIUtility.ScaleAroundPivot(Vector2.one * UIUtility.uiScale, new Vector2(Screen.width, Screen.height));
            if (this._colliderTarget)
                this._colliderEditRect = GUILayout.Window(2, this._colliderEditRect, this.ColliderEditor, "Collider Editor" + (this.IsColliderDirty(this._colliderTarget) ? "*" : ""));
        }
        #endregion

        #region Public Methods
        public void SetBoneTargetRotation(FullBodyBipedEffector type, Quaternion targetRotation)
        { 
            if (this.isEnabled)
                this.chara.ikCtrl.drivingRig.effectorTargets[this._effectorToIndex[type]].target.rotation = targetRotation;
        }

        public Quaternion GetBoneTargetRotation(FullBodyBipedEffector type)
        {
            if (!this.isEnabled)
                return Quaternion.identity;
            return this.chara.ikCtrl.drivingRig.effectorTargets[this._effectorToIndex[type]].target.rotation;
        }

        public void SetBoneTargetPosition(FullBodyBipedEffector type, Vector3 targetPosition)
        {
            if (this.isEnabled)
                this.chara.ikCtrl.drivingRig.effectorTargets[this._effectorToIndex[type]].target.position = targetPosition;
        }

        public Vector3 GetBoneTargetPosition(FullBodyBipedEffector type)
        {
            if (!this.isEnabled)
                return Vector3.zero;
            return this.chara.ikCtrl.drivingRig.effectorTargets[this._effectorToIndex[type]].target.position;
        }

        public void SetBendGoalPosition(FullBodyBipedChain type, Vector3 position)
        {
            if (this.isEnabled)
                this.chara.ikCtrl.drivingRig.bendGoals[(int)type].transform.position = position;
        }

        public Vector3 GetBendGoalPosition(FullBodyBipedChain type)
        {
            if (!this.isEnabled)
                return Vector3.zero;
            return this.chara.ikCtrl.drivingRig.bendGoals[(int)type].transform.position;
        }

        public void CopyLimbToTwin(FullBodyBipedChain limb)
        {
            Transform effectorSrc;
            Transform bendGoalSrc;
            Transform effectorDest;
            Transform bendGoalDest;
            Transform root;
            switch (limb)
            {
                case FullBodyBipedChain.LeftArm:
                    effectorSrc = this.chara.ikCtrl.drivingRig.effectorTargets[this._effectorToIndex[FullBodyBipedEffector.LeftHand]].target;
                    bendGoalSrc = this.chara.ikCtrl.drivingRig.bendGoals[(int)limb].transform;
                    effectorDest = this.chara.ikCtrl.drivingRig.effectorTargets[this._effectorToIndex[FullBodyBipedEffector.RightHand]].target;
                    bendGoalDest = this.chara.ikCtrl.drivingRig.bendGoals[(int)FullBodyBipedChain.RightArm].transform;
                    root = this._body.solver.spineMapping.spineBones[this._body.solver.spineMapping.spineBones.Length - 2];
                    break;
                case FullBodyBipedChain.LeftLeg:
                    effectorSrc = this.chara.ikCtrl.drivingRig.effectorTargets[this._effectorToIndex[FullBodyBipedEffector.LeftFoot]].target;
                    bendGoalSrc = this.chara.ikCtrl.drivingRig.bendGoals[(int)limb].transform;
                    effectorDest = this.chara.ikCtrl.drivingRig.effectorTargets[this._effectorToIndex[FullBodyBipedEffector.RightFoot]].target;
                    bendGoalDest = this.chara.ikCtrl.drivingRig.bendGoals[(int)FullBodyBipedChain.RightLeg].transform;
                    root = this._body.solver.spineMapping.spineBones[0];
                    break;
                case FullBodyBipedChain.RightArm:
                    effectorSrc = this.chara.ikCtrl.drivingRig.effectorTargets[this._effectorToIndex[FullBodyBipedEffector.RightHand]].target;
                    bendGoalSrc = this.chara.ikCtrl.drivingRig.bendGoals[(int)limb].transform;
                    effectorDest = this.chara.ikCtrl.drivingRig.effectorTargets[this._effectorToIndex[FullBodyBipedEffector.LeftHand]].target;
                    bendGoalDest = this.chara.ikCtrl.drivingRig.bendGoals[(int)FullBodyBipedChain.LeftArm].transform;
                    root = this._body.solver.spineMapping.spineBones[this._body.solver.spineMapping.spineBones.Length - 2];
                    break;
                case FullBodyBipedChain.RightLeg:
                    effectorSrc = this.chara.ikCtrl.drivingRig.effectorTargets[this._effectorToIndex[FullBodyBipedEffector.RightFoot]].target;
                    bendGoalSrc = this.chara.ikCtrl.drivingRig.bendGoals[(int)limb].transform;
                    effectorDest = this.chara.ikCtrl.drivingRig.effectorTargets[this._effectorToIndex[FullBodyBipedEffector.LeftFoot]].target;
                    bendGoalDest = this.chara.ikCtrl.drivingRig.bendGoals[(int)FullBodyBipedChain.LeftLeg].transform;
                    root = this._body.solver.spineMapping.spineBones[0];
                    break;
                default:
                    effectorSrc = null;
                    bendGoalSrc = null;
                    effectorDest = null;
                    bendGoalDest = null;
                    root = null;
                    break;
            }

            Vector3 localPos = root.InverseTransformPoint(effectorSrc.position);
            localPos.x *= -1f;
            Vector3 effectorPosition = root.TransformPoint(localPos);

            localPos = root.InverseTransformPoint(bendGoalSrc.position);
            localPos.x *= -1f;
            Vector3 bendGoalPosition = root.TransformPoint(localPos);

            Quaternion rot = effectorSrc.localRotation;
            rot.w *= -1f;
            rot.x *= -1f;

            this.StartCoroutine(this.CopyLibmToTwin_Routine(effectorDest, effectorPosition, rot, bendGoalDest, bendGoalPosition));
        }

        public void AdvancedModeWindow(int id)
        {
            GUILayout.BeginHorizontal();
            for (int i = 0; i < (int) SelectedTab.Count; ++i)
                if (this.ShouldDisplayTab((SelectedTab)i) && GUILayout.Button(((SelectedTab) i).ToString()))
                    this._selectedTab = (SelectedTab) i;
            GUILayout.EndHorizontal();
            this._tabFunctions[this._selectedTab]();
            GUI.DragWindow();
        }
        #endregion

        #region GUI

        private bool ShouldDisplayTab(SelectedTab t)
        {
            switch (t)
            {
                case SelectedTab.BoobsEditor:
                    return (this._isFemale);
            }
            return true;
        }

        private bool RepeatControl()
        {
            this._repeatCalled = true;
            if (Mathf.Approximately(this._repeatTimer, 0f))
                return true;
            return Event.current.type == EventType.Repaint && this._repeatTimer > this._repeatBeforeDuration;
        }

        private void IncEditor()
        {
            GUILayout.BeginVertical();
            if (GUILayout.Button("+", GUILayout.MinHeight(37f)))
            {
                this._incIndex = Mathf.Clamp(this._incIndex + 1, -4, 1);
                this._inc = Mathf.Pow(10, this._incIndex);
            }
            if (GUILayout.Button("-", GUILayout.MinHeight(37f)))
            {
                this._incIndex = Mathf.Clamp(this._incIndex - 1, -4, 1);
                this._inc = Mathf.Pow(10, this._incIndex);
            }
            GUILayout.EndVertical();

        }

        private void ColliderEditor(int id)
        {
            GUILayout.BeginVertical();

            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("Center:");
            GUILayout.BeginHorizontal();
            Vector3 center = this.Vector3Editor(this._colliderTarget.m_Center);
            if (center != this._colliderTarget.m_Center)
            {
                this.SetColliderDirty(this._colliderTarget);
                if (this._dirtyColliders[this._colliderTarget].originalCenter.hasValue == false)
                    this._dirtyColliders[this._colliderTarget].originalCenter = this._colliderTarget.m_Center;
            }
            this._colliderTarget.m_Center = center;
            this.IncEditor();
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            GUILayout.BeginHorizontal(GUI.skin.box);
            GUILayout.Label("Radius\t", GUILayout.ExpandWidth(false));
            float radius = GUILayout.HorizontalSlider(this._colliderTarget.m_Radius, 0f, 1f);
            if (radius != this._colliderTarget.m_Radius)
            {
                this.SetColliderDirty(this._colliderTarget);
                if (this._dirtyColliders[this._colliderTarget].originalRadius.hasValue == false)
                    this._dirtyColliders[this._colliderTarget].originalRadius = this._colliderTarget.m_Radius;
            }
            this._colliderTarget.m_Radius = radius;
            GUILayout.Label(this._colliderTarget.m_Radius.ToString("0.000"), GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal(GUI.skin.box);
            GUILayout.Label("Height\t", GUILayout.ExpandWidth(false));
            float height = GUILayout.HorizontalSlider(this._colliderTarget.m_Height, 2 * this._colliderTarget.m_Radius, 3f);
            if (height < this._colliderTarget.m_Radius * 2)
                height = this._colliderTarget.m_Radius * 2;
            if (height != this._colliderTarget.m_Height)
            {
                this.SetColliderDirty(this._colliderTarget);
                if (this._dirtyColliders[this._colliderTarget].originalHeight.hasValue == false)
                    this._dirtyColliders[this._colliderTarget].originalHeight = this._colliderTarget.m_Height;
            }
            this._colliderTarget.m_Height = height;
            GUILayout.Label(this._colliderTarget.m_Height.ToString("0.000"), GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal(GUI.skin.box);
            GUILayout.Label("Direction:");
            DynamicBoneCollider.Direction direction = this._colliderTarget.m_Direction;
            if (GUILayout.Toggle(this._colliderTarget.m_Direction == DynamicBoneCollider.Direction.X, "X"))
                this._colliderTarget.m_Direction = DynamicBoneCollider.Direction.X;
            if (GUILayout.Toggle(this._colliderTarget.m_Direction == DynamicBoneCollider.Direction.Y, "Y"))
                this._colliderTarget.m_Direction = DynamicBoneCollider.Direction.Y;
            if (GUILayout.Toggle(this._colliderTarget.m_Direction == DynamicBoneCollider.Direction.Z, "Z"))
                this._colliderTarget.m_Direction = DynamicBoneCollider.Direction.Z;
            if (direction != this._colliderTarget.m_Direction)
            {
                this.SetColliderDirty(this._colliderTarget);
                if (this._dirtyColliders[this._colliderTarget].originalDirection.hasValue == false)
                    this._dirtyColliders[this._colliderTarget].originalDirection = direction;
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal(GUI.skin.box);
            GUILayout.Label("Bound:");
            DynamicBoneCollider.Bound bound = this._colliderTarget.m_Bound;
            if (GUILayout.Toggle(this._colliderTarget.m_Bound == DynamicBoneCollider.Bound.Inside, "Inside"))
                this._colliderTarget.m_Bound = DynamicBoneCollider.Bound.Inside;
            if (GUILayout.Toggle(this._colliderTarget.m_Bound == DynamicBoneCollider.Bound.Outside, "Outside"))
                this._colliderTarget.m_Bound = DynamicBoneCollider.Bound.Outside;
            if (bound != this._colliderTarget.m_Bound)
            {
                this.SetColliderDirty(this._colliderTarget);
                if (this._dirtyColliders[this._colliderTarget].originalBound.hasValue == false)
                    this._dirtyColliders[this._colliderTarget].originalBound = bound;
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            Color c = GUI.color;
            GUI.color = Color.red;
            if (GUILayout.Button("Reset", GUILayout.ExpandWidth(false)))
                this.SetColliderNotDirty(this._colliderTarget);
            GUI.color = c;
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();

            GUI.DragWindow();
        }

        private void BonesPosition()
        {
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical(GUILayout.ExpandWidth(true));
            GUILayout.Label("Character Tree", GUI.skin.box);
            this._boneEditionScroll = GUILayout.BeginScrollView(this._boneEditionScroll, GUI.skin.box, GUILayout.ExpandHeight(true));
            //foreach (Transform t in Resources.FindObjectsOfTypeAll<Transform>())
            //    if (t.parent == null)
            //        this.DisplayObjectTree(t.gameObject, 0);
            this.DisplayObjectTree(this.transform.GetChild(0).gameObject, 0);
            GUILayout.EndScrollView();
            //if (this._boneTarget != null)
            //    foreach (Component c in this._boneTarget.GetComponents<Component>())
            //        GUILayout.Label(c.GetType().Name);
            GUILayout.Label("Legend:");
            GUILayout.BeginHorizontal();
            Color co = GUI.color;
            GUI.color = Color.cyan;
            GUILayout.Button("Selected");
            GUI.color = Color.magenta;
            GUILayout.Button("Changed*");
            GUI.color = this._colliderMat.color;
            GUILayout.Button("Collider");
            GUI.color = co;
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            GUILayout.BeginVertical(GUI.skin.box, GUILayout.MinWidth(350f));
            {
                GUILayout.BeginHorizontal(GUI.skin.box);
                if (GUILayout.Toggle(this._boneEditionCoordType == CoordType.Position, "Position"))
                    this._boneEditionCoordType = CoordType.Position;
                if (GUILayout.Toggle(this._boneEditionCoordType == CoordType.Rotation, "Rotation"))
                    this._boneEditionCoordType = CoordType.Rotation;
                if (GUILayout.Toggle(this._boneEditionCoordType == CoordType.Scale, "Scale"))
                    this._boneEditionCoordType = CoordType.Scale;
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                GUILayout.BeginVertical();
                switch (this._boneEditionCoordType)
                {
                    case CoordType.Position:
                        Vector3 position = Vector3.zero;
                        if (this._boneTarget != null)
                            position = this._boneTarget.localPosition;
                        Color c = GUI.color;
                        GUI.color = Color.red;
                        GUILayout.BeginHorizontal();
                        GUILayout.Label("X:\t" + position.x.ToString("0.0000"));
                        GUILayout.BeginHorizontal(GUILayout.MaxWidth(160f));
                        if (GUILayout.RepeatButton((-this._inc).ToString("+0.####;-0.####")) && this.RepeatControl() && this._boneTarget != null)
                        {
                            position -= this._inc * Vector3.right;
                            this.SetBoneDirty(this._boneTarget.gameObject);
                        }
                        if (GUILayout.RepeatButton(this._inc.ToString("+0.####;-0.####")) && this.RepeatControl() && this._boneTarget != null)
                        {
                            position += this._inc * Vector3.right;
                            this.SetBoneDirty(this._boneTarget.gameObject);
                        }
                        GUILayout.EndHorizontal();
                        GUILayout.EndHorizontal();
                        GUI.color = c;

                        GUI.color = Color.green;
                        GUILayout.BeginHorizontal();
                        GUILayout.Label("Y:\t" + position.y.ToString("0.0000"));
                        GUILayout.BeginHorizontal(GUILayout.MaxWidth(160f));
                        if (GUILayout.RepeatButton((-this._inc).ToString("+0.####;-0.####")) && this.RepeatControl() && this._boneTarget != null)
                        {
                            position -= this._inc * Vector3.up;
                            this.SetBoneDirty(this._boneTarget.gameObject);
                        }
                        if (GUILayout.RepeatButton(this._inc.ToString("+0.####;-0.####")) && this.RepeatControl() && this._boneTarget != null)
                        {
                            position += this._inc * Vector3.up;
                            this.SetBoneDirty(this._boneTarget.gameObject);
                        }
                        GUILayout.EndHorizontal();
                        GUILayout.EndHorizontal();
                        GUI.color = c;

                        GUI.color = Color.blue;
                        GUILayout.BeginHorizontal();
                        GUILayout.Label("Z:\t" + position.z.ToString("0.0000"));
                        GUILayout.BeginHorizontal(GUILayout.MaxWidth(160f));
                        if (GUILayout.RepeatButton((-this._inc).ToString("+0.####;-0.####")) && this.RepeatControl() && this._boneTarget != null)
                        {
                            position -= this._inc * Vector3.forward;
                            this.SetBoneDirty(this._boneTarget.gameObject);
                        }
                        if (GUILayout.RepeatButton(this._inc.ToString("+0.####;-0.####")) && this.RepeatControl() && this._boneTarget != null)
                        {
                            position += this._inc * Vector3.forward;
                            this.SetBoneDirty(this._boneTarget.gameObject);
                        }
                        GUILayout.EndHorizontal();
                        GUILayout.EndHorizontal();
                        GUI.color = c;
                        if (this._boneTarget != null && this.IsBoneDirty(this._boneTarget.gameObject))
                        {
                            TransformData td = this._dirtyBones[this._boneTarget.gameObject];
                            td.position = position;
                            if (!td.originalPosition.hasValue)
                                td.originalPosition = this._boneTarget.localPosition;
                        }
                        break;
                    case CoordType.Rotation:
                        Quaternion rotation = Quaternion.identity;
                        if (this._boneTarget != null)
                            rotation = this._boneTarget.localRotation;
                        c = GUI.color;
                        GUI.color = Color.red;
                        GUILayout.BeginHorizontal();
                        GUILayout.Label("X (Pitch):\t" + rotation.eulerAngles.x.ToString("0.00"));
                        GUILayout.BeginHorizontal(GUILayout.MaxWidth(160f));
                        if (GUILayout.RepeatButton((-this._inc).ToString("+0.####;-0.####")) && this.RepeatControl() && this._boneTarget != null)
                        {
                            rotation *= Quaternion.AngleAxis(-this._inc, Vector3.right);
                            this.SetBoneDirty(this._boneTarget.gameObject);
                        }
                        if (GUILayout.RepeatButton(this._inc.ToString("+0.####;-0.####")) && this.RepeatControl() && this._boneTarget != null)
                        {
                            rotation *= Quaternion.AngleAxis(this._inc, Vector3.right);
                            this.SetBoneDirty(this._boneTarget.gameObject);
                        }
                        GUILayout.EndHorizontal();
                        GUILayout.EndHorizontal();
                        GUI.color = c;

                        GUI.color = Color.green;
                        GUILayout.BeginHorizontal();
                        GUILayout.Label("Y (Yaw):\t" + rotation.eulerAngles.y.ToString("0.00"));
                        GUILayout.BeginHorizontal(GUILayout.MaxWidth(160f));
                        if (GUILayout.RepeatButton((-this._inc).ToString("+0.####;-0.####")) && this.RepeatControl() && this._boneTarget != null)
                        {
                            rotation *= Quaternion.AngleAxis(-this._inc, Vector3.up);
                            this.SetBoneDirty(this._boneTarget.gameObject);
                        }
                        if (GUILayout.RepeatButton(this._inc.ToString("+0.####;-0.####")) && this.RepeatControl() && this._boneTarget != null)
                        {
                            rotation *= Quaternion.AngleAxis(this._inc, Vector3.up);
                            this.SetBoneDirty(this._boneTarget.gameObject);
                        }
                        GUILayout.EndHorizontal();
                        GUILayout.EndHorizontal();
                        GUI.color = c;

                        GUI.color = Color.blue;
                        GUILayout.BeginHorizontal();
                        GUILayout.Label("Z (Roll):\t" + rotation.eulerAngles.z.ToString("0.00"));
                        GUILayout.BeginHorizontal(GUILayout.MaxWidth(160f));
                        if (GUILayout.RepeatButton((-this._inc).ToString("+0.####;-0.####")) && this.RepeatControl() && this._boneTarget != null)
                        {
                            rotation *= Quaternion.AngleAxis(-this._inc, Vector3.forward);
                            this.SetBoneDirty(this._boneTarget.gameObject);
                        }
                        if (GUILayout.RepeatButton(this._inc.ToString("+0.####;-0.####")) && this.RepeatControl() && this._boneTarget != null)
                        {
                            rotation *= Quaternion.AngleAxis(this._inc, Vector3.forward);
                            this.SetBoneDirty(this._boneTarget.gameObject);
                        }
                        GUILayout.EndHorizontal();
                        GUILayout.EndHorizontal();
                        GUI.color = c;
                        if (this._boneTarget != null && this.IsBoneDirty(this._boneTarget.gameObject))
                        {
                            TransformData td = this._dirtyBones[this._boneTarget.gameObject];
                            td.rotation = rotation;
                            if (!td.originalRotation.hasValue)
                                td.originalRotation = this._boneTarget.localRotation;
                        }
                        break;
                    case CoordType.Scale:
                        Vector3 scale = Vector3.one;
                        if (this._boneTarget != null)
                            scale = this._boneTarget.localScale;

                        c = GUI.color;
                        GUI.color = Color.red;
                        GUILayout.BeginHorizontal();
                        GUILayout.Label("X:\t" + scale.x.ToString("0.0000"));
                        GUILayout.BeginHorizontal(GUILayout.MaxWidth(160f));
                        if (GUILayout.RepeatButton((-this._inc).ToString("+0.####;-0.####")) && this.RepeatControl() && this._boneTarget != null)
                        {
                            scale -= this._inc * Vector3.right;
                            this.SetBoneDirty(this._boneTarget.gameObject);
                        }
                        if (GUILayout.RepeatButton(this._inc.ToString("+0.####;-0.####")) && this.RepeatControl() && this._boneTarget != null)
                        {
                            scale += this._inc * Vector3.right;
                            this.SetBoneDirty(this._boneTarget.gameObject);
                        }
                        GUILayout.EndHorizontal();
                        GUILayout.EndHorizontal();
                        GUI.color = c;

                        GUI.color = Color.green;
                        GUILayout.BeginHorizontal();
                        GUILayout.Label("Y:\t" + scale.y.ToString("0.0000"));
                        GUILayout.BeginHorizontal(GUILayout.MaxWidth(160f));
                        if (GUILayout.RepeatButton((-this._inc).ToString("+0.####;-0.####")) && this.RepeatControl() && this._boneTarget != null)
                        {
                            scale -= this._inc * Vector3.up;
                            this.SetBoneDirty(this._boneTarget.gameObject);
                        }
                        if (GUILayout.RepeatButton(this._inc.ToString("+0.####;-0.####")) && this.RepeatControl() && this._boneTarget != null)
                        {
                            scale += this._inc * Vector3.up;
                            this.SetBoneDirty(this._boneTarget.gameObject);
                        }
                        GUILayout.EndHorizontal();
                        GUILayout.EndHorizontal();
                        GUI.color = c;

                        GUI.color = Color.blue;
                        GUILayout.BeginHorizontal();
                        GUILayout.Label("Z:\t" + scale.z.ToString("0.0000"));
                        GUILayout.BeginHorizontal(GUILayout.MaxWidth(160f));
                        if (GUILayout.RepeatButton((-this._inc).ToString("+0.####;-0.####")) && this.RepeatControl() && this._boneTarget != null)
                        {
                            scale -= this._inc * Vector3.forward;
                            this.SetBoneDirty(this._boneTarget.gameObject);
                        }
                        if (GUILayout.RepeatButton(this._inc.ToString("+0.####;-0.####")) && this.RepeatControl() && this._boneTarget != null)
                        {
                            scale += this._inc * Vector3.forward;
                            this.SetBoneDirty(this._boneTarget.gameObject);
                        }
                        GUILayout.EndHorizontal();
                        GUILayout.EndHorizontal();
                        GUI.color = c;

                        GUILayout.BeginHorizontal();
                        GUILayout.Label("X/Y/Z");
                        GUILayout.BeginHorizontal(GUILayout.MaxWidth(160f));
                        if (GUILayout.RepeatButton((-this._inc).ToString("+0.####;-0.####")) && this.RepeatControl() && this._boneTarget != null)
                        {
                            scale -= this._inc * Vector3.one;
                            this.SetBoneDirty(this._boneTarget.gameObject);
                        }
                        if (GUILayout.RepeatButton(this._inc.ToString("+0.####;-0.####")) && this.RepeatControl() && this._boneTarget != null)
                        {
                            scale += this._inc * Vector3.one;
                            this.SetBoneDirty(this._boneTarget.gameObject);
                        }
                        GUILayout.EndHorizontal();
                        GUILayout.EndHorizontal();
                        GUI.enabled = true;
                        if (this._boneTarget != null && this.IsBoneDirty(this._boneTarget.gameObject))
                        {
                            TransformData td = this._dirtyBones[this._boneTarget.gameObject];
                            td.scale = scale;
                            if (!td.originalScale.hasValue)
                                td.originalScale = this._boneTarget.localScale;
                        }
                        break;
                }
                GUILayout.EndVertical();

                this.IncEditor();

                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Reset Pos.") && this._boneTarget != null && this.IsBoneDirty(this._boneTarget.gameObject))
                {
                    this._dirtyBones[this._boneTarget.gameObject].position.Reset();
                    this.SetBoneNotDirtyIf(this._boneTarget.gameObject);
                    this._dirtyBones[this._boneTarget.gameObject].originalPosition.Reset();
                }
                if (GUILayout.Button("Reset Rot.") && this._boneTarget != null && this.IsBoneDirty(this._boneTarget.gameObject))
                {
                    this._dirtyBones[this._boneTarget.gameObject].rotation.Reset();
                    this.SetBoneNotDirtyIf(this._boneTarget.gameObject);
                    this._dirtyBones[this._boneTarget.gameObject].originalRotation.Reset();
                }
                if (GUILayout.Button("Reset Scale") && this._boneTarget != null && this.IsBoneDirty(this._boneTarget.gameObject))
                {
                    this._dirtyBones[this._boneTarget.gameObject].scale.Reset();
                    this.SetBoneNotDirtyIf(this._boneTarget.gameObject);
                    this._dirtyBones[this._boneTarget.gameObject].originalScale.Reset();
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginVertical(GUI.skin.box);
                GUIStyle style = GUI.skin.GetStyle("Label");
                TextAnchor bak = style.alignment;
                style.alignment = TextAnchor.MiddleCenter;
                GUILayout.Label("Shortcuts", style);
                style.alignment = bak;
                GUILayout.BeginHorizontal();
                foreach (KeyValuePair<Transform, string> kvp in this._boneEditionShortcuts)
                    if (GUILayout.Button(kvp.Value))
                        this.GoToObject(kvp.Key.gameObject);
                GUILayout.EndHorizontal();
                Dictionary<string, string> customShortcuts = this._isFemale ? MainWindow.self.femaleShortcuts : MainWindow.self.maleShortcuts;
                int i = 0;
                string toRemove = null;
                foreach (KeyValuePair<string, string> kvp in customShortcuts)
                {
                    if (i % 3 == 0)
                        GUILayout.BeginHorizontal();
                    if (GUILayout.Button(kvp.Value))
                    {
                        if (this._removeShortcutMode)
                        {
                            toRemove = kvp.Key;
                            this._removeShortcutMode = false;
                        }
                        else
                            this.GoToObject(kvp.Key);                        
                    }
                    if ((i + 1) % 3 == 0)
                        GUILayout.EndHorizontal();
                    ++i;
                }
                if (toRemove != null)
                    customShortcuts.Remove(toRemove);
                if (i != 12)
                {
                    if (i % 3 == 0)
                        GUILayout.BeginHorizontal();
                    if (GUILayout.Button("+ Add Shortcut", GUILayout.ExpandWidth(false)) && this._boneTarget != null)
                    {
                        string path = this._boneTarget.GetPathFrom(this.transform);
                        if (customShortcuts.ContainsKey(path) == false)
                            customShortcuts.Add(path, this._boneTarget.name);
                        this._removeShortcutMode = false;
                    }
                    if ((i + 1) % 3 == 0)
                        GUILayout.EndHorizontal();
                    ++i;
                }
                if (i % 3 != 0)
                    GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                Color color = GUI.color;
                if (this._removeShortcutMode)
                    GUI.color = Color.red;
                if (GUILayout.Button(this._removeShortcutMode ? "Click on a shortcut" : "Remove Shortcut"))
                    this._removeShortcutMode = !this._removeShortcutMode;
                GUI.color = color;
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
            }
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }

        private void BoobsEditor()
        {
            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("Right boob" + (this.IsBoobDirty(this._rightBoob) ? "*" : ""));
            this.DisplaySingleBoob(this._rightBoob);
            GUILayout.EndVertical();

            GUILayout.BeginVertical();
            GUILayout.FlexibleSpace();
            this.IncEditor();
            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();

            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("Left boob" + (this.IsBoobDirty(this._leftBoob) ? "*" : ""));
            this.DisplaySingleBoob(this._leftBoob);
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
        }

        private void DynamicBonesEditor()
        {
            GUILayout.BeginHorizontal();
            this._dynamicBonesScroll = GUILayout.BeginScrollView(this._dynamicBonesScroll, false, true, GUI.skin.horizontalScrollbar, GUI.skin.verticalScrollbar, GUI.skin.box, GUILayout.ExpandWidth(false));
            foreach (DynamicBone db in this._dynamicBones)
            {
                if (db.m_Root == null)
                    continue;
                Color c = GUI.color;
                if (this.IsDynamicBoneDirty(db))
                    GUI.color = Color.magenta;
                if (ReferenceEquals(db, this._dynamicBoneTarget))
                    GUI.color = Color.cyan;
                if (GUILayout.Button(db.m_Root.name + (this.IsDynamicBoneDirty(db) ? "*" : "")))
                    this._dynamicBoneTarget = db;
                GUI.color = c;
            }
            GUILayout.EndScrollView();

            GUILayout.BeginVertical(GUI.skin.box, GUILayout.ExpandWidth(true));
            GUILayout.BeginHorizontal();
            float w = 0f;
            if (this._dynamicBoneTarget != null)
                w = this._dynamicBoneTarget.GetWeight();
            GUILayout.Label("Weight\t", GUILayout.ExpandWidth(false));
            w = GUILayout.HorizontalSlider(w, 0f, 1f);
            if (this._dynamicBoneTarget != null)
            {
                if (!Mathf.Approximately(this._dynamicBoneTarget.GetWeight(), w))
                {
                    this.SetDynamicBoneDirty(this._dynamicBoneTarget);
                    if (this._dirtyDynamicBones[this._dynamicBoneTarget].originalWeight.hasValue == false)
                        this._dirtyDynamicBones[this._dynamicBoneTarget].originalWeight = this._dynamicBoneTarget.GetWeight();
                }
                this._dynamicBoneTarget.SetWeight(w);
            }
            GUILayout.Label(w.ToString("0.000"), GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();

            GUILayout.BeginVertical();
            GUILayout.Label("Gravity");
            Vector3 g = Vector3.zero;
            if (this._dynamicBoneTarget != null)
                g = this._dynamicBoneTarget.m_Gravity;
            g = this.Vector3Editor(g, Color.red);
            if (this._dynamicBoneTarget != null)
            {
                if (this._dynamicBoneTarget.m_Gravity != g)
                {
                    this.SetDynamicBoneDirty(this._dynamicBoneTarget);
                    if (this._dirtyDynamicBones[this._dynamicBoneTarget].originalGravity.hasValue == false)
                        this._dirtyDynamicBones[this._dynamicBoneTarget].originalGravity = this._dynamicBoneTarget.m_Gravity;
                }
                this._dynamicBoneTarget.m_Gravity = g;
            }
            GUILayout.EndVertical();

            GUILayout.BeginVertical();
            GUILayout.Label("Force");
            Vector3 f = Vector3.zero;
            if (this._dynamicBoneTarget != null)
                f = this._dynamicBoneTarget.m_Force;
            f = this.Vector3Editor(f, Color.blue);
            if (this._dynamicBoneTarget != null)
            {
                if (this._dynamicBoneTarget.m_Force != f)
                {
                    this.SetDynamicBoneDirty(this._dynamicBoneTarget);
                    if (this._dirtyDynamicBones[this._dynamicBoneTarget].originalForce.hasValue == false)
                        this._dirtyDynamicBones[this._dynamicBoneTarget].originalForce = this._dynamicBoneTarget.m_Force;
                }
                this._dynamicBoneTarget.m_Force = f;
            }
            GUILayout.EndVertical();

            GUILayout.BeginHorizontal();
            GUILayout.Label("FreezeAxis\t", GUILayout.ExpandWidth(false));
            DynamicBone.FreezeAxis fa = DynamicBone.FreezeAxis.None;
            if (this._dynamicBoneTarget != null)
                fa = this._dynamicBoneTarget.m_FreezeAxis;
            if (GUILayout.Toggle(fa == DynamicBone.FreezeAxis.None, "None"))
                fa = DynamicBone.FreezeAxis.None;
            if (GUILayout.Toggle(fa == DynamicBone.FreezeAxis.X, "X"))
                fa = DynamicBone.FreezeAxis.X;
            if (GUILayout.Toggle(fa == DynamicBone.FreezeAxis.Y, "Y"))
                fa = DynamicBone.FreezeAxis.Y;
            if (GUILayout.Toggle(fa == DynamicBone.FreezeAxis.Z, "Z"))
                fa = DynamicBone.FreezeAxis.Z;
            if (this._dynamicBoneTarget != null)
            {
                if (this._dynamicBoneTarget.m_FreezeAxis != fa)
                {
                    this.SetDynamicBoneDirty(this._dynamicBoneTarget);
                    if (this._dirtyDynamicBones[this._dynamicBoneTarget].originalFreezeAxis.hasValue == false)
                        this._dirtyDynamicBones[this._dynamicBoneTarget].originalFreezeAxis = this._dynamicBoneTarget.m_FreezeAxis;
                }
                this._dynamicBoneTarget.m_FreezeAxis = fa;
            }
            GUILayout.EndHorizontal();
 
            if (GUILayout.Button("Reset", GUILayout.ExpandWidth(false)) && this._dynamicBoneTarget != null)
                this.SetDynamicBoneNotDirty(this._dynamicBoneTarget);

            GUILayout.EndVertical();

            GUILayout.BeginVertical();
            GUILayout.FlexibleSpace();
            this.IncEditor();
            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
        }

        private void DisplaySingleBoob(DynamicBone_Ver02 boob)
        {
            GUILayout.BeginVertical();
            GUILayout.Label("Gravity");
            Vector3 gravity = boob.Gravity;
            gravity = this.Vector3Editor(gravity, Color.red);
            if (gravity != boob.Gravity)
            {
                this.SetBoobDirty(boob);
                if (this._dirtyBoobs[boob].originalGravity.hasValue == false)
                    this._dirtyBoobs[boob].originalGravity = boob.Gravity;
            }
            boob.Gravity = gravity;
            GUILayout.EndVertical();

            GUILayout.BeginVertical();
            GUILayout.Label("Force");
            Vector3 force = boob.Force;
            force = this.Vector3Editor(force, Color.blue);
            if (force != boob.Force)
            {
                this.SetBoobDirty(boob);
                if (this._dirtyBoobs[boob].originalForce.hasValue == false)
                    this._dirtyBoobs[boob].originalForce = boob.Force;
            }
            boob.Force = force;
            GUILayout.EndVertical();

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            Color c = GUI.color;
            GUI.color = Color.red;
            if (GUILayout.Button("Reset", GUILayout.ExpandWidth(false)))
                this.SetBoobNotDirty(boob);
            GUI.color = c;
            GUILayout.EndHorizontal();
        }

        private void DisplayObjectTree(GameObject go, int indent)
        {
            Color c = GUI.color;
            if (this._dirtyBones.ContainsKey(go))
                GUI.color = Color.magenta;
            if (this._colliderObjects.Contains(go.transform))
                GUI.color = this._colliderMat.color;
            if (this._boneTarget == go.transform)
                GUI.color = Color.cyan;
            GUILayout.BeginHorizontal();
            GUILayout.Space(indent * 20f);
            if (go.transform.childCount != 0)
            {
                if (GUILayout.Toggle(this._openedBones.Contains(go), "", GUILayout.ExpandWidth(false)))
                {
                    if (this._openedBones.Contains(go) == false)
                        this._openedBones.Add(go);
                }
                else
                {
                    if (this._openedBones.Contains(go))
                        this._openedBones.Remove(go);
                }
            }
            else
                GUILayout.Space(20f);
            if (GUILayout.Button(go.name + (this.IsBoneDirty(go) ? "*" : ""), GUILayout.ExpandWidth(false)))
            {
                this._boneTarget = go.transform;
                this._colliderTarget = go.GetComponent<DynamicBoneCollider>();
            }
            GUI.color = c;
            GUILayout.EndHorizontal();
            if (this._openedBones.Contains(go))
                for (int i = 0; i < go.transform.childCount; ++i)
                    this.DisplayObjectTree(go.transform.GetChild(i).gameObject, indent + 1);
        }

        private Vector3 Vector3Editor(Vector3 value)
        {
            GUILayout.BeginVertical();
            Color c = GUI.color;
            GUI.color = Color.red;
            GUILayout.BeginHorizontal();
            GUILayout.Label("X:\t" + value.x.ToString("0.0000"));
            GUILayout.BeginHorizontal(GUILayout.MaxWidth(160f));
            if (GUILayout.RepeatButton((-this._inc).ToString("+0.####;-0.####")) && this.RepeatControl())
                value -= this._inc * Vector3.right;
            if (GUILayout.RepeatButton(this._inc.ToString("+0.####;-0.####")) && this.RepeatControl())
                value += this._inc * Vector3.right;
            GUILayout.EndHorizontal();
            GUILayout.EndHorizontal();
            GUI.color = c;

            GUI.color = Color.green;
            GUILayout.BeginHorizontal();
            GUILayout.Label("Y:\t" + value.y.ToString("0.0000"));
            GUILayout.BeginHorizontal(GUILayout.MaxWidth(160f));
            if (GUILayout.RepeatButton((-this._inc).ToString("+0.####;-0.####")) && this.RepeatControl())
                value -= this._inc * Vector3.up;
            if (GUILayout.RepeatButton(this._inc.ToString("+0.####;-0.####")) && this.RepeatControl())
                value += this._inc * Vector3.up;
            GUILayout.EndHorizontal();
            GUILayout.EndHorizontal();
            GUI.color = c;

            GUI.color = Color.blue;
            GUILayout.BeginHorizontal();
            GUILayout.Label("Z:\t" + value.z.ToString("0.0000"));
            GUILayout.BeginHorizontal(GUILayout.MaxWidth(160f));
            if (GUILayout.RepeatButton((-this._inc).ToString("+0.####;-0.####")) && this.RepeatControl())
                value -= this._inc * Vector3.forward;
            if (GUILayout.RepeatButton(this._inc.ToString("+0.####;-0.####")) && this.RepeatControl())
                value += this._inc * Vector3.forward;
            GUILayout.EndHorizontal();
            GUILayout.EndHorizontal();
            GUI.color = c;
            GUILayout.EndHorizontal();
            return value;
        }
        private Vector3 Vector3Editor(Vector3 value, Color color)
        {
            GUILayout.BeginVertical();
            Color c = GUI.color;
            GUI.color = color;
            GUILayout.BeginHorizontal();
            GUILayout.Label("X:\t" + value.x.ToString("0.0000"));
            GUILayout.BeginHorizontal(GUILayout.MaxWidth(160f));
            if (GUILayout.RepeatButton((-this._inc).ToString("+0.####;-0.####")) && this.RepeatControl())
                value -= this._inc * Vector3.right;
            if (GUILayout.RepeatButton(this._inc.ToString("+0.####;-0.####")) && this.RepeatControl())
                value += this._inc * Vector3.right;
            GUILayout.EndHorizontal();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Y:\t" + value.y.ToString("0.0000"));
            GUILayout.BeginHorizontal(GUILayout.MaxWidth(160f));
            if (GUILayout.RepeatButton((-this._inc).ToString("+0.####;-0.####")) && this.RepeatControl())
                value -= this._inc * Vector3.up;
            if (GUILayout.RepeatButton(this._inc.ToString("+0.####;-0.####")) && this.RepeatControl())
                value += this._inc * Vector3.up;
            GUILayout.EndHorizontal();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Z:\t" + value.z.ToString("0.0000"));
            GUILayout.BeginHorizontal(GUILayout.MaxWidth(160f));
            if (GUILayout.RepeatButton((-this._inc).ToString("+0.####;-0.####")) && this.RepeatControl())
                value -= this._inc * Vector3.forward;
            if (GUILayout.RepeatButton(this._inc.ToString("+0.####;-0.####")) && this.RepeatControl())
                value += this._inc * Vector3.forward;
            GUILayout.EndHorizontal();
            GUILayout.EndHorizontal();
            GUI.color = c;
            GUILayout.EndHorizontal();
            return value;
        }
        #endregion

        #region Private Methods

        private IEnumerator CopyLibmToTwin_Routine(Transform effector, Vector3 effectorTargetPos, Quaternion effectorTargetRot, Transform bendGoal, Vector3 bendGoalTargetPos)
        {
            float startTime = Time.unscaledTime - Time.unscaledDeltaTime;
            Vector3 effectorStartPos = effector.position;
            Vector3 bendGoalStartPos = bendGoal.position;
            Quaternion effectorStartRot = effector.localRotation;
            while (Time.unscaledTime - startTime < 0.1f)
            {
                float v = (Time.unscaledTime - startTime) * 10f;
                effector.position = Vector3.Lerp(effectorStartPos, effectorTargetPos, v);
                effector.localRotation = Quaternion.Lerp(effectorStartRot, effectorTargetRot, v);
                bendGoal.position = Vector3.Lerp(bendGoalStartPos, bendGoalTargetPos, v);
                yield return null;
            }
            effector.position = effectorTargetPos;
            effector.localRotation = effectorTargetRot;
            bendGoal.position = bendGoalTargetPos;
        }

        private void GoToObject(string path)
        {
            this.GoToObject(this.transform.FindChild(path).gameObject);
        }

        private void GoToObject(GameObject go)
        {
            if (ReferenceEquals(go, this.transform.GetChild(0).gameObject))
                return;
            GameObject goBak = go;
            this._boneTarget = go.transform;
            go = go.transform.parent.gameObject;
            while (go.transform != this.transform.GetChild(0))
            {
                this._openedBones.Add(go);
                go = go.transform.parent.gameObject;
            }
            this._openedBones.Add(go);
            Vector2 scroll = new Vector2(0f, -GUI.skin.button.CalcHeight(new GUIContent("a"), 100f) - 4);
            this.GetScrollPosition(this.transform.GetChild(0).gameObject, goBak, 0, ref scroll);
            this._boneEditionScroll = scroll;
        }

        private bool GetScrollPosition(GameObject root, GameObject go, int indent, ref Vector2 scrollPosition)
        {
            scrollPosition = new Vector2(indent * 20f, scrollPosition.y + GUI.skin.button.CalcHeight(new GUIContent("a"), 100f) + 4);
            if (ReferenceEquals(root, go))
                return true;
            if (this._openedBones.Contains(root))
                for (int i = 0; i < root.transform.childCount; ++i)
                    if (this.GetScrollPosition(root.transform.GetChild(i).gameObject, go, indent + 1, ref scrollPosition))
                        return true;
            return false;
        }

        private void SetBoneDirty(GameObject go)
        {
            if (!this.IsBoneDirty(go))
                this._dirtyBones.Add(go, new TransformData());
        }

        private void SetBoneNotDirtyIf(GameObject go)
        {
            if (this.IsBoneDirty(go))
            {
                TransformData data = this._dirtyBones[go];
                if (data.position.hasValue == false && data.originalPosition.hasValue)
                {
                    go.transform.localPosition = data.originalPosition;
                    data.originalPosition.Reset();
                }
                if (data.rotation.hasValue == false && data.originalRotation.hasValue)
                {
                    go.transform.localRotation = data.originalRotation;
                    data.originalRotation.Reset();
                }
                if (data.scale.hasValue == false && data.originalScale.hasValue)
                {
                    go.transform.localScale = data.originalScale;
                    data.originalScale.Reset();
                }
                if (data.position.hasValue == false && data.rotation.hasValue == false && data.scale.hasValue == false)
                {
                    this._dirtyBones.Remove(go);
                }
            }
        }

        private bool IsBoneDirty(GameObject go)
        {
            return (this._dirtyBones.ContainsKey(go));
        }

        private void SetBoobNotDirty(DynamicBone_Ver02 boob)
        {
            if (this.IsBoobDirty(boob))
            {
                BoobData data = this._dirtyBoobs[boob];
                if (data.originalGravity.hasValue)
                {
                    boob.Gravity = data.originalGravity;
                    data.originalGravity.Reset();
                }
                if (data.originalForce.hasValue)
                {
                    boob.Force = data.originalForce;
                    data.originalForce.Reset();
                }
                this._dirtyBoobs.Remove(boob);
            }
        }

        private void SetBoobDirty(DynamicBone_Ver02 boob)
        {
            if (!this.IsBoobDirty(boob))
                this._dirtyBoobs.Add(boob, new BoobData());
        }

        private bool IsBoobDirty(DynamicBone_Ver02 boob)
        {
            return (this._dirtyBoobs.ContainsKey(boob));
        }

        private void SetColliderNotDirty(DynamicBoneCollider collider)
        {
            if (this.IsColliderDirty(collider))
            {
                ColliderData data = this._dirtyColliders[collider];
                if (data.originalCenter.hasValue)
                {
                    collider.m_Center = data.originalCenter;
                    data.originalCenter.Reset();
                }
                if (data.originalRadius.hasValue)
                {
                    collider.m_Radius = data.originalRadius;
                    data.originalRadius.Reset();
                }
                if (data.originalHeight.hasValue)
                {
                    collider.m_Height = data.originalHeight;
                    data.originalHeight.Reset();
                }
                if (data.originalDirection.hasValue)
                {
                    collider.m_Direction = data.originalDirection;
                    data.originalDirection.Reset();
                }
                if (data.originalBound.hasValue)
                {
                    collider.m_Bound = data.originalBound;
                    data.originalBound.Reset();
                }
                this._dirtyColliders.Remove(collider);
            }
        }

        private void SetColliderDirty(DynamicBoneCollider collider)
        {
            if (!this.IsColliderDirty(collider))
                this._dirtyColliders.Add(collider, new ColliderData());
        }

        private bool IsColliderDirty(DynamicBoneCollider collider)
        {
            return this._dirtyColliders.ContainsKey(collider);
        }

        private void SetDynamicBoneNotDirty(DynamicBone bone)
        {
            if (this.IsDynamicBoneDirty(bone))
            {
                DynamicBoneData data = this._dirtyDynamicBones[bone];
                if (data.originalWeight.hasValue)
                {
                    bone.SetWeight(data.originalWeight);
                    data.originalWeight.Reset();
                }
                if (data.originalGravity.hasValue)
                {
                    bone.m_Gravity = data.originalGravity;
                    data.originalGravity.Reset();
                }
                if (data.originalForce.hasValue)
                {
                    bone.m_Force = data.originalForce;
                    data.originalForce.Reset();
                }
                if (data.originalFreezeAxis.hasValue)
                {
                    bone.m_FreezeAxis = data.originalFreezeAxis;
                    data.originalFreezeAxis.Reset();
                }
                this._dirtyDynamicBones.Remove(bone);
            }
        }

        private void SetDynamicBoneDirty(DynamicBone bone)
        {
            if (this.IsDynamicBoneDirty(bone) == false)
                this._dirtyDynamicBones.Add(bone, new DynamicBoneData());
        }

        private bool IsDynamicBoneDirty(DynamicBone bone)
        {
            return this._dirtyDynamicBones.ContainsKey(bone);
        }

        private void OnPostSolve()
        {
            if (this.isEnabled)
            {
                this._legKneeDamL.rotation = Quaternion.Lerp(this._body.solver.leftLegMapping.bone1.rotation, this._body.solver.leftLegMapping.bone2.rotation, 0.5f);
                this._legKneeDamR.rotation = Quaternion.Lerp(this._body.solver.rightLegMapping.bone1.rotation, this._body.solver.rightLegMapping.bone2.rotation, 0.5f);
                this._legKneeBackL.rotation = this._body.solver.leftLegMapping.bone2.rotation;
                this._legKneeBackR.rotation = this._body.solver.rightLegMapping.bone2.rotation;
                this._legUpL.rotation = this._body.solver.leftLegMapping.bone1.rotation;
                this._legUpL.localRotation *= Quaternion.AngleAxis(Quaternion.Angle(this._legUpL.parent.rotation, this._body.solver.leftLegMapping.bone1.rotation) / 6f, Vector3.right);
                this._legUpR.rotation = this._body.solver.rightLegMapping.bone1.rotation;
                this._legUpR.localRotation *= Quaternion.AngleAxis(Quaternion.Angle(this._legUpR.parent.rotation, this._body.solver.rightLegMapping.bone1.rotation) / 6f, Vector3.right);
                this._elbowDamL.rotation = Quaternion.Lerp(this._elbowDamL.parent.rotation, this._body.solver.leftArmMapping.bone2.rotation, 0.65f);
                this._elbowDamR.rotation = Quaternion.Lerp(this._elbowDamR.parent.rotation, this._body.solver.rightArmMapping.bone2.rotation, 0.65f);
                this._armElbouraDamL.rotation = Quaternion.Lerp(this._armElbouraDamL.parent.rotation, this._body.solver.leftArmMapping.bone2.rotation, 0.5f);
                this._armElbouraDamR.rotation = Quaternion.Lerp(this._armElbouraDamR.parent.rotation, this._body.solver.rightArmMapping.bone2.rotation, 0.5f);
                //float shoulderAngle = Extensions.NormalizeAngle(Extensions.DirectionalAngle(this._shounderBaseRotL * Vector3.forward, this._body.solver.leftArmMapping.bone1.localRotation * Vector3.forward, Vector3.right));
                //this._armUpDamL1.localRotation = Quaternion.AngleAxis(shoulderAngle / 5f, Vector3.right);
                //this._armUpDamL2.localRotation = Quaternion.AngleAxis(shoulderAngle / 8f, Vector3.right);
                //this._armUpDamL3.localRotation = Quaternion.AngleAxis(shoulderAngle / 10f, Vector3.right);
                //shoulderAngle = Extensions.NormalizeAngle(Extensions.DirectionalAngle(this._shounderBaseRotR * Vector3.forward, this._body.solver.rightArmMapping.bone1.localRotation * Vector3.forward, Vector3.right));
                //this._armUpDamR1.localRotation = Quaternion.AngleAxis(shoulderAngle / 5f, Vector3.right);
                //this._armUpDamR2.localRotation = Quaternion.AngleAxis(shoulderAngle / 8f, Vector3.right);
                //this._armUpDamR3.localRotation = Quaternion.AngleAxis(shoulderAngle / 10f, Vector3.right);
                this._armUpDamL3.localScale = Vector3.Lerp(Vector3.one, _armDamScale, Quaternion.Angle(this._armElbouraDamL.parent.rotation, this._body.solver.leftArmMapping.bone2.rotation) / 90f);
                this._armUpDamR3.localScale = Vector3.Lerp(Vector3.one, _armDamScale, Quaternion.Angle(this._armElbouraDamR.parent.rotation, this._body.solver.rightArmMapping.bone2.rotation) / 90f);
                float handAngle = Extensions.DirectionalAngle(Vector3.forward, this._body.solver.leftArmMapping.bone2.InverseTransformDirection(this._body.solver.leftArmMapping.bone3.forward), Vector3.right);
                this._armLow1L.localRotation = Quaternion.AngleAxis(Mathf.LerpAngle(0f, handAngle, 0.25f), Vector3.right);
                this._armLow2L.localRotation = Quaternion.AngleAxis(Mathf.LerpAngle(0f, handAngle, 0.5f), Vector3.right);
                this._armLow3L.localRotation = Quaternion.AngleAxis(Mathf.LerpAngle(0f, handAngle, 0.75f), Vector3.right);
                handAngle = Extensions.DirectionalAngle(Vector3.forward, this._body.solver.rightArmMapping.bone2.InverseTransformDirection(this._body.solver.rightArmMapping.bone3.forward), Vector3.right);
                this._armLow1R.localRotation = Quaternion.AngleAxis(Mathf.LerpAngle(0f, handAngle, 0.25f), Vector3.right);
                this._armLow2R.localRotation = Quaternion.AngleAxis(Mathf.LerpAngle(0f, handAngle, 0.5f), Vector3.right);
                this._armLow3R.localRotation = Quaternion.AngleAxis(Mathf.LerpAngle(0f, handAngle, 0.75f), Vector3.right);
                this._siriDamL.rotation = Quaternion.Slerp(this._siriDamL.parent.rotation, this._body.solver.leftLegMapping.bone1.rotation, 0.4f);
                this._siriDamR.rotation = Quaternion.Slerp(this._siriDamR.parent.rotation, this._body.solver.rightLegMapping.bone1.rotation, 0.4f);
                this._kosi.rotation = Quaternion.Lerp(this._kosi.parent.rotation, Quaternion.Lerp(this._body.solver.leftLegMapping.bone1.rotation, this._body.solver.rightLegMapping.bone1.rotation, 0.5f), 0.25f);
            }

            foreach (KeyValuePair<GameObject, TransformData> kvp in this._dirtyBones)
            {
                if (kvp.Value.position.hasValue)
                    kvp.Key.transform.localPosition = kvp.Value.position;
                if (kvp.Value.rotation.hasValue)
                    kvp.Key.transform.localRotation = kvp.Value.rotation;
                if (kvp.Value.scale.hasValue)
                    kvp.Key.transform.localScale = kvp.Value.scale;
            }
        }

        private void DrawGizmos()
        {
            if (!this.draw)
                return;
            GL.PushMatrix();
            GL.LoadProjectionMatrix(Studio.Instance.MainCamera.projectionMatrix);
            GL.MultMatrix(Studio.Instance.MainCamera.transform.worldToLocalMatrix);
            switch (this._selectedTab)
            {
                case SelectedTab.BonesPosition:
                    if (this._boneTarget != null)
                    {
                        float size = 0.025f;
                        Vector3 topLeftForward = this._boneTarget.position + (this._boneTarget.rotation * ((Vector3.up + Vector3.left + Vector3.forward) * size)),
                                topRightForward = this._boneTarget.position + (this._boneTarget.rotation * ((Vector3.up + Vector3.right + Vector3.forward) * size)),
                                bottomLeftForward = this._boneTarget.position + (this._boneTarget.rotation * ((Vector3.down + Vector3.left + Vector3.forward) * size)),
                                bottomRightForward = this._boneTarget.position + (this._boneTarget.rotation * ((Vector3.down + Vector3.right + Vector3.forward) * size)),
                                topLeftBack = this._boneTarget.position + (this._boneTarget.rotation * ((Vector3.up + Vector3.left + Vector3.back) * size)),
                                topRightBack = this._boneTarget.position + (this._boneTarget.rotation * ((Vector3.up + Vector3.right + Vector3.back) * size)),
                                bottomLeftBack = this._boneTarget.position + (this._boneTarget.rotation * ((Vector3.down + Vector3.left + Vector3.back) * size)),
                                bottomRightBack = this._boneTarget.position + (this._boneTarget.rotation * ((Vector3.down + Vector3.right + Vector3.back) * size));

                        this._mat.SetPass(0);
                        GL.Begin(GL.LINES);

                        GL.Vertex(topLeftForward);
                        GL.Vertex(topRightForward);

                        GL.Vertex(topRightForward);
                        GL.Vertex(bottomRightForward);

                        GL.Vertex(bottomRightForward);
                        GL.Vertex(bottomLeftForward);

                        GL.Vertex(bottomLeftForward);
                        GL.Vertex(topLeftForward);

                        GL.Vertex(topLeftBack);
                        GL.Vertex(topRightBack);

                        GL.Vertex(topRightBack);
                        GL.Vertex(bottomRightBack);

                        GL.Vertex(bottomRightBack);
                        GL.Vertex(bottomLeftBack);

                        GL.Vertex(bottomLeftBack);
                        GL.Vertex(topLeftBack);

                        GL.Vertex(topLeftBack);
                        GL.Vertex(topLeftForward);

                        GL.Vertex(topRightBack);
                        GL.Vertex(topRightForward);

                        GL.Vertex(bottomRightBack);
                        GL.Vertex(bottomRightForward);

                        GL.Vertex(bottomLeftBack);
                        GL.Vertex(bottomLeftForward);
                        GL.End();

                        GL.Begin(GL.LINES);
                        this._xMat.SetPass(0);
                        GL.Vertex(this._boneTarget.position);
                        GL.Vertex(this._boneTarget.position + (this._boneTarget.rotation * (Vector3.right * size * 2)));
                        GL.End();

                        GL.Begin(GL.LINES);
                        this._yMat.SetPass(0);
                        GL.Vertex(this._boneTarget.position);
                        GL.Vertex(this._boneTarget.position + (this._boneTarget.rotation * (Vector3.up * size * 2)));
                        GL.End();

                        GL.Begin(GL.LINES);
                        this._zMat.SetPass(0);
                        GL.Vertex(this._boneTarget.position);
                        GL.Vertex(this._boneTarget.position + (this._boneTarget.rotation * (Vector3.forward * size * 2)));
                        GL.End();

                        if (this._colliderTarget)
                            this.DrawCollider();
                    }
                    break;
                case SelectedTab.BoobsEditor:
                    if (this._isFemale)
                    {
                        float scale = 20f;
                        Vector3 origin = this._leftBoob.Bones[this._leftBoob.Bones.Count - 1].position;
                        Vector3 final = origin + (this._leftBoob.Gravity) * scale;
                        GL.Begin(GL.LINES);
                        this._xMat.SetPass(0);
                        this.DrawVector(origin, final);
                        GL.End();

                        origin = final;
                        final += this._leftBoob.Force * scale;

                        GL.Begin(GL.LINES);
                        this._zMat.SetPass(0);
                        this.DrawVector(origin, final);
                        GL.End();

                        origin = this._leftBoob.Bones[this._leftBoob.Bones.Count - 1].position;

                        GL.Begin(GL.LINES);
                        this._colliderMat.SetPass(0);
                        this.DrawVector(origin, final);
                        GL.End();

                        origin = this._rightBoob.Bones[this._rightBoob.Bones.Count - 1].position;
                        final = origin + (this._rightBoob.Gravity) * scale;
                        GL.Begin(GL.LINES);
                        this._xMat.SetPass(0);
                        this.DrawVector(origin, final);
                        GL.End();

                        origin = final;
                        final += this._rightBoob.Force * scale;

                        GL.Begin(GL.LINES);
                        this._zMat.SetPass(0);
                        this.DrawVector(origin, final);
                        GL.End();

                        origin = this._rightBoob.Bones[this._rightBoob.Bones.Count - 1].position;

                        GL.Begin(GL.LINES);
                        this._colliderMat.SetPass(0);
                        this.DrawVector(origin, final);
                        GL.End();

                    }
                    break;
                case SelectedTab.DynamicBonesEditor:
                    if (this._dynamicBoneTarget != null)
                    {
                        Transform end = this._dynamicBoneTarget.m_Root ?? this._dynamicBoneTarget.transform;
                        while (end.childCount != 0)
                            end = end.GetChild(0);
                        float scale = 10f;
                        Vector3 origin = end.position;
                        
                        Vector3 final = origin + (this._dynamicBoneTarget.m_Gravity) * scale;
                        GL.Begin(GL.LINES);
                        this._xMat.SetPass(0);
                        this.DrawVector(origin, final);
                        GL.End();

                        origin = final;
                        final += this._dynamicBoneTarget.m_Force * scale;

                        GL.Begin(GL.LINES);
                        this._zMat.SetPass(0);
                        this.DrawVector(origin, final);
                        GL.End();

                        origin = end.position;

                        GL.Begin(GL.LINES);
                        this._colliderMat.SetPass(0);
                        this.DrawVector(origin, final);
                        GL.End();
                    }
                    break;
            }
            this._xMat.SetPass(0);
            GL.PopMatrix();
        }

        private void DrawVector(Vector3 start, Vector3 end, float scale = 20f)
        {
            Vector3 dir = end - start;
            Quaternion rot = Quaternion.AngleAxis(15f, Vector3.ProjectOnPlane(Studio.Instance.MainCamera.transform.position - end, -dir));

            GL.Vertex(start);
            GL.Vertex(end);

            GL.Vertex(end);
            Vector3 arrow1 = end + rot * (-dir * 0.1f);
            GL.Vertex(arrow1);

            GL.Vertex(end);
            Vector3 arrow2 = end + Quaternion.Inverse(rot) * (-dir * 0.1f);
            GL.Vertex(arrow2);

            GL.Vertex(arrow1);
            GL.Vertex(arrow2);
        }

        private void DrawCollider()
        {
            GL.Begin(GL.LINES);
            this._colliderMat.SetPass(0);

            float radius = this._colliderTarget.m_Radius * Mathf.Abs(this._colliderTarget.transform.lossyScale.x);
            float num = this._colliderTarget.m_Height * 0.5f - this._colliderTarget.m_Radius;
            {
                Vector3 position1 = this._colliderTarget.m_Center;
                Vector3 position2 = this._colliderTarget.m_Center;
                Quaternion orientation = Quaternion.identity;
                Vector3 dir = Vector3.zero;
                switch (this._colliderTarget.m_Direction)
                {
                    case DynamicBoneCollider.Direction.X:
                        position1.x -= num;
                        position2.x += num;
                        orientation = this._colliderTarget.transform.rotation * Quaternion.AngleAxis(90f, Vector3.up);
                        dir = Vector3.right;
                        break;
                    case DynamicBoneCollider.Direction.Y:
                        position1.y -= num;
                        position2.y += num;
                        orientation = this._colliderTarget.transform.rotation * Quaternion.AngleAxis(90f, Vector3.right);
                        dir = Vector3.up;
                        break;
                    case DynamicBoneCollider.Direction.Z:
                        position1.z -= num;
                        position2.z += num;
                        orientation = this._colliderTarget.transform.rotation;
                        dir = Vector3.forward;
                        break;
                }
                position1 = this._colliderTarget.transform.TransformPoint(position1);
                position2 = this._colliderTarget.transform.TransformPoint(position2);
                dir = this._colliderTarget.transform.TransformDirection(dir);
                for (int i = 1; i < 10; ++i)
                    this.DrawCircle(Vector3.Lerp(position1, position2, i / 10f), radius, orientation);
                for (int i = 0; i < 8; ++i)
                {
                    float angle = 360 * (i / 8f) * Mathf.Deg2Rad;
                    Vector3 offset = orientation * (new Vector3(Mathf.Cos(angle), Mathf.Sin(angle))) * radius;
                    GL.Vertex(position1 + offset);
                    GL.Vertex(position2 + offset);
                }
                Vector3[] prev = new Vector3[8];
                Vector3 prevCenter1 = Vector3.zero;
                Vector3 prevCenter2 = Vector3.zero;
                for (int i = 0; i < 8; ++i)
                {
                    float angle = 360 * (i / 8f) * Mathf.Deg2Rad;
                    prev[i] = orientation * (new Vector3(Mathf.Cos(angle), Mathf.Sin(angle))) * radius;
                }
                for (int i = 0; i < 6; ++i)
                {
                    float v = (i / 5f) * 0.95f;
                    float angle = Mathf.Asin(v);
                    float radius2 = radius * Mathf.Cos(angle);
                    Vector3 center1 = position1 - dir * v * radius;
                    Vector3 center2 = position2 + dir * v * radius;
                    this.DrawCircle(center1, radius2, orientation);
                    this.DrawCircle(center2, radius2, orientation);

                    if (i != 0)
                        for (int j = 0; j < 8; ++j)
                        {
                            float angle2 = 360 * (j / 8f) * Mathf.Deg2Rad;
                            Vector3 offset = orientation * (new Vector3(Mathf.Cos(angle2), Mathf.Sin(angle2))) * radius2;
                            GL.Vertex(prevCenter1 + prev[j]);
                            GL.Vertex(center1 + offset);

                            GL.Vertex(prevCenter2 + prev[j]);
                            GL.Vertex(center2 + offset);
                            prev[j] = offset;
                        }
                    prevCenter1 = center1;
                    prevCenter2 = center2;
                }
            }
            GL.End();
        }

        private void DrawCircle(Vector3 position, float radius, Quaternion orientation)
        {
            Vector3 prev = position + orientation * (Vector3.right) * radius;
            Vector3 first = prev;
            for (int i = 1; i < 36; ++i)
            {
                float angle = i * 10 * Mathf.Deg2Rad;
                Vector3 next = position + orientation * (new Vector3(Mathf.Cos(angle), Mathf.Sin(angle))) * radius;
                GL.Vertex(prev);
                GL.Vertex(next);
                prev = next;
            }
            GL.Vertex(prev);
            GL.Vertex(first);
        }
        #endregion

        #region Saves
        public void LoadBinary(BinaryReader binaryReader)
        {
            this.LoadVersion_1_0_0(binaryReader);
        }

        public void LoadXml(XmlNode xmlNode, HSPE.VersionNumber v)
        {
            this.LoadDefaultVersion(xmlNode);
        }
        public int SaveXml(XmlTextWriter xmlWriter)
        {
            int written = 0;
            if (this._dirtyBones.Count != 0)
            {
                xmlWriter.WriteStartElement("advancedObjects");
                foreach (KeyValuePair<GameObject, TransformData> kvp in this._dirtyBones)
                {
                    Transform t = kvp.Key.transform.parent;
                    string n = kvp.Key.transform.name;
                    while (t != this.transform)
                    {
                        n = t.name + "/" + n;
                        t = t.parent;
                    }
                    xmlWriter.WriteStartElement("object");
                    xmlWriter.WriteAttributeString("name", n);

                    if (kvp.Value.position.hasValue)
                    {
                        xmlWriter.WriteAttributeString("posX", XmlConvert.ToString(kvp.Value.position.value.x));
                        xmlWriter.WriteAttributeString("posY", XmlConvert.ToString(kvp.Value.position.value.y));
                        xmlWriter.WriteAttributeString("posZ", XmlConvert.ToString(kvp.Value.position.value.z));
                    }

                    if (kvp.Value.rotation.hasValue)
                    {
                        xmlWriter.WriteAttributeString("rotW", XmlConvert.ToString(kvp.Value.rotation.value.w));
                        xmlWriter.WriteAttributeString("rotX", XmlConvert.ToString(kvp.Value.rotation.value.x));
                        xmlWriter.WriteAttributeString("rotY", XmlConvert.ToString(kvp.Value.rotation.value.y));
                        xmlWriter.WriteAttributeString("rotZ", XmlConvert.ToString(kvp.Value.rotation.value.z));
                    }

                    if (kvp.Value.scale.hasValue)
                    {
                        xmlWriter.WriteAttributeString("scaleX", XmlConvert.ToString(kvp.Value.scale.value.x));
                        xmlWriter.WriteAttributeString("scaleY", XmlConvert.ToString(kvp.Value.scale.value.y));
                        xmlWriter.WriteAttributeString("scaleZ", XmlConvert.ToString(kvp.Value.scale.value.z));
                    }
                    xmlWriter.WriteEndElement();
                    ++written;
                }
                xmlWriter.WriteEndElement();
            }
            if (this._dirtyBoobs.Count != 0)
            {
                xmlWriter.WriteStartElement("boobs");
                foreach (KeyValuePair<DynamicBone_Ver02, BoobData> kvp in this._dirtyBoobs)
                {
                    xmlWriter.WriteStartElement(kvp.Key == this._leftBoob ? "left" : "right");

                    if (kvp.Value.originalGravity.hasValue)
                    {
                        xmlWriter.WriteAttributeString("gravityX", XmlConvert.ToString(kvp.Key.Gravity.x));
                        xmlWriter.WriteAttributeString("gravityY", XmlConvert.ToString(kvp.Key.Gravity.y));
                        xmlWriter.WriteAttributeString("gravityZ", XmlConvert.ToString(kvp.Key.Gravity.z));
                    }

                    if (kvp.Value.originalForce.hasValue)
                    {
                        xmlWriter.WriteAttributeString("forceX", XmlConvert.ToString(kvp.Key.Force.x));
                        xmlWriter.WriteAttributeString("forceY", XmlConvert.ToString(kvp.Key.Force.y));
                        xmlWriter.WriteAttributeString("forceZ", XmlConvert.ToString(kvp.Key.Force.z));
                    }

                    xmlWriter.WriteEndElement();
                    ++written;
                }
                xmlWriter.WriteEndElement();
            }
            if (this._dirtyColliders.Count != 0)
            {
                xmlWriter.WriteStartElement("colliders");
                foreach (KeyValuePair<DynamicBoneCollider, ColliderData> kvp in this._dirtyColliders)
                {
                    Transform t = kvp.Key.transform.parent;
                    string n = kvp.Key.transform.name;
                    while (t != this.transform)
                    {
                        n = t.name + "/" + n;
                        t = t.parent;
                    }
                    xmlWriter.WriteStartElement("collider");
                    xmlWriter.WriteAttributeString("name", n);

                    if (kvp.Value.originalCenter.hasValue)
                    {
                        xmlWriter.WriteAttributeString("centerX", XmlConvert.ToString(kvp.Key.m_Center.x));
                        xmlWriter.WriteAttributeString("centerY", XmlConvert.ToString(kvp.Key.m_Center.y));
                        xmlWriter.WriteAttributeString("centerZ", XmlConvert.ToString(kvp.Key.m_Center.z));
                    }
                    if (kvp.Value.originalRadius.hasValue)
                        xmlWriter.WriteAttributeString("radius", XmlConvert.ToString(kvp.Key.m_Radius));
                    if (kvp.Value.originalHeight.hasValue)
                        xmlWriter.WriteAttributeString("height", XmlConvert.ToString(kvp.Key.m_Height));
                    if (kvp.Value.originalDirection.hasValue)
                        xmlWriter.WriteAttributeString("direction", XmlConvert.ToString((int)kvp.Key.m_Direction));
                    if (kvp.Value.originalBound.hasValue)
                        xmlWriter.WriteAttributeString("bound", XmlConvert.ToString((int)kvp.Key.m_Bound));
                    xmlWriter.WriteEndElement();
                    ++written;
                }
                xmlWriter.WriteEndElement();
            }
            if (this.forceBendGoalsWeight == false)
            {
                xmlWriter.WriteStartElement("forceBendGoalsWeight");
                xmlWriter.WriteAttributeString("value", XmlConvert.ToString(this.forceBendGoalsWeight));
                xmlWriter.WriteEndElement();
                ++written;
            }
            if (this._dirtyDynamicBones.Count != 0)
            {
                xmlWriter.WriteStartElement("dynamicBones");
                foreach (KeyValuePair<DynamicBone, DynamicBoneData> kvp in this._dirtyDynamicBones)
                {
                    xmlWriter.WriteStartElement("dynamicBone");
                    xmlWriter.WriteAttributeString("root", kvp.Key.m_Root != null ? kvp.Key.m_Root.name : kvp.Key.name);

                    if (kvp.Value.originalWeight.hasValue)
                        xmlWriter.WriteAttributeString("weight", XmlConvert.ToString(kvp.Key.GetWeight()));
                    if (kvp.Value.originalGravity.hasValue)
                    {
                        xmlWriter.WriteAttributeString("gravityX", XmlConvert.ToString(kvp.Key.m_Gravity.x));
                        xmlWriter.WriteAttributeString("gravityY", XmlConvert.ToString(kvp.Key.m_Gravity.y));
                        xmlWriter.WriteAttributeString("gravityZ", XmlConvert.ToString(kvp.Key.m_Gravity.z));
                    }
                    if (kvp.Value.originalForce.hasValue)
                    {
                        xmlWriter.WriteAttributeString("forceX", XmlConvert.ToString(kvp.Key.m_Force.x));
                        xmlWriter.WriteAttributeString("forceY", XmlConvert.ToString(kvp.Key.m_Force.y));
                        xmlWriter.WriteAttributeString("forceZ", XmlConvert.ToString(kvp.Key.m_Force.z));
                    }
                    if (kvp.Value.originalFreezeAxis.hasValue)
                        xmlWriter.WriteAttributeString("freezeAxis", XmlConvert.ToString((int)kvp.Key.m_FreezeAxis));
                    xmlWriter.WriteEndElement();
                    ++written;
                }
                xmlWriter.WriteEndElement();
            }
            return written;
        }

        private void LoadVersion_1_0_0(BinaryReader binaryReader)
        {
            binaryReader.ReadInt32();
            for (int i = 0; i < 9; ++i)
            {
                Vector3 pos;
                Quaternion rot;

                pos.x = binaryReader.ReadSingle();
                pos.y = binaryReader.ReadSingle();
                pos.z = binaryReader.ReadSingle();

                rot.w = binaryReader.ReadSingle();
                rot.x = binaryReader.ReadSingle();
                rot.y = binaryReader.ReadSingle();
                rot.z = binaryReader.ReadSingle();

                this.SetBoneTargetPosition((FullBodyBipedEffector)i, pos);
                this.SetBoneTargetRotation((FullBodyBipedEffector)i, rot);
            }
            binaryReader.ReadInt32();
            for (int i = 0; i < 4; ++i)
            {
                Vector3 pos;

                pos.x = binaryReader.ReadSingle();
                pos.y = binaryReader.ReadSingle();
                pos.z = binaryReader.ReadSingle();

                this.SetBendGoalPosition((FullBodyBipedChain)i, pos);
            }
            int count = binaryReader.ReadInt32();
            for (int i = 0; i < count; ++i)
            {
                string name = binaryReader.ReadString();
                GameObject obj = this.transform.FindDescendant(name).gameObject;
                Vector3 pos;
                Quaternion rot;
                binaryReader.ReadSingle();
                binaryReader.ReadSingle();
                binaryReader.ReadSingle();

                binaryReader.ReadSingle();
                binaryReader.ReadSingle();
                binaryReader.ReadSingle();
                binaryReader.ReadSingle();

                pos.x = binaryReader.ReadSingle();
                pos.y = binaryReader.ReadSingle();
                pos.z = binaryReader.ReadSingle();

                rot.w = binaryReader.ReadSingle();
                rot.x = binaryReader.ReadSingle();
                rot.y = binaryReader.ReadSingle();
                rot.z = binaryReader.ReadSingle();
                this._dirtyBones.Add(obj, new TransformData() { position = pos, rotation = rot, originalPosition = obj.transform.localPosition, originalRotation = obj.transform.localRotation});
            }
        }

        private void LoadDefaultVersion(XmlNode xmlNode)
        {
            XmlNode objects = xmlNode.FindChildNode("advancedObjects");
            if (objects != null)
            {
                foreach (XmlNode node in objects.ChildNodes)
                {
                    if (node.Name == "object")
                    {
                        string name = node.Attributes["name"].Value;
                        GameObject obj = this.transform.FindChild(name).gameObject;
                        TransformData data = new TransformData();
                        if (node.Attributes["posX"] != null && node.Attributes["posY"] != null && node.Attributes["posZ"] != null)
                        {
                            Vector3 pos;
                            pos.x = XmlConvert.ToSingle(node.Attributes["posX"].Value);
                            pos.y = XmlConvert.ToSingle(node.Attributes["posY"].Value);
                            pos.z = XmlConvert.ToSingle(node.Attributes["posZ"].Value);
                            data.position = pos;
                            data.originalPosition = obj.transform.localPosition;
                        }
                        if (node.Attributes["rotW"] != null && node.Attributes["rotX"] != null && node.Attributes["rotY"] != null && node.Attributes["rotZ"] != null)
                        {
                            Quaternion rot;
                            rot.w = XmlConvert.ToSingle(node.Attributes["rotW"].Value);
                            rot.x = XmlConvert.ToSingle(node.Attributes["rotX"].Value);
                            rot.y = XmlConvert.ToSingle(node.Attributes["rotY"].Value);
                            rot.z = XmlConvert.ToSingle(node.Attributes["rotZ"].Value);
                            data.rotation = rot;
                            data.originalRotation = obj.transform.localRotation;
                        }
                        if (node.Attributes["scaleX"] != null && node.Attributes["scaleY"] != null && node.Attributes["scaleZ"] != null)
                        {
                            Vector3 scale;
                            scale.x = XmlConvert.ToSingle(node.Attributes["scaleX"].Value);
                            scale.y = XmlConvert.ToSingle(node.Attributes["scaleY"].Value);
                            scale.z = XmlConvert.ToSingle(node.Attributes["scaleZ"].Value);
                            data.scale = scale;
                            data.originalScale = obj.transform.localScale;
                        }
                        if (data.position.hasValue || data.rotation.hasValue || data.scale.hasValue)
                            this._dirtyBones.Add(obj, data);
                    }
                }
            }
            if (this._isFemale)
            {
                XmlNode boobs = xmlNode.FindChildNode("boobs");
                if (boobs != null)
                {
                    foreach (XmlNode node in boobs.ChildNodes)
                    {
                        DynamicBone_Ver02 boob = null;
                        switch (node.Name)
                        {
                            case "left":
                                boob = this._leftBoob;
                                break;
                            case "right":
                                boob = this._rightBoob;
                                break;
                        }
                        if (boob != null)
                        {
                            BoobData data = new BoobData();
                            if (node.Attributes["gravityX"] != null && node.Attributes["gravityY"] != null && node.Attributes["gravityZ"] != null)
                            {
                                Vector3 gravity;
                                gravity.x = XmlConvert.ToSingle(node.Attributes["gravityX"].Value);
                                gravity.y = XmlConvert.ToSingle(node.Attributes["gravityY"].Value);
                                gravity.z = XmlConvert.ToSingle(node.Attributes["gravityZ"].Value);
                                data.originalGravity = boob.Gravity;
                                boob.Gravity = gravity;
                            }
                            if (node.Attributes["forceX"] != null && node.Attributes["forceY"] != null && node.Attributes["forceZ"] != null)
                            {
                                Vector3 force;
                                force.x = XmlConvert.ToSingle(node.Attributes["forceX"].Value);
                                force.y = XmlConvert.ToSingle(node.Attributes["forceY"].Value);
                                force.z = XmlConvert.ToSingle(node.Attributes["forceZ"].Value);
                                data.originalForce = boob.Force;
                                boob.Force = force;
                            }
                            if (data.originalGravity.hasValue || data.originalForce.hasValue)
                                this._dirtyBoobs.Add(boob, data);
                        }
                    }
                }
            }
            XmlNode colliders = xmlNode.FindChildNode("colliders");
            if (colliders != null)
            {
                foreach (XmlNode node in colliders.ChildNodes)
                {
                    DynamicBoneCollider collider = this.transform.FindChild(node.Attributes["name"].Value).GetComponent<DynamicBoneCollider>();
                    ColliderData data = new ColliderData();
                    if (node.Attributes["centerX"] != null && node.Attributes["centerY"] != null && node.Attributes["centerZ"] != null)
                    {
                        Vector3 center;
                        center.x = XmlConvert.ToSingle(node.Attributes["centerX"].Value);
                        center.y = XmlConvert.ToSingle(node.Attributes["centerY"].Value);
                        center.z = XmlConvert.ToSingle(node.Attributes["centerZ"].Value);
                        data.originalCenter = collider.m_Center;
                        collider.m_Center = center;
                    }
                    if (node.Attributes["radius"] != null)
                    {
                        float radius = XmlConvert.ToSingle(node.Attributes["radius"].Value);
                        data.originalRadius = collider.m_Radius;
                        collider.m_Radius = radius;
                    }
                    if (node.Attributes["height"] != null)
                    {
                        float height = XmlConvert.ToSingle(node.Attributes["height"].Value);
                        data.originalHeight = collider.m_Height;
                        collider.m_Height = height;
                    }
                    if (node.Attributes["direction"] != null)
                    {
                        int direction = XmlConvert.ToInt32(node.Attributes["direction"].Value);
                        data.originalDirection = collider.m_Direction;
                        collider.m_Direction = (DynamicBoneCollider.Direction)direction;
                    }
                    if (node.Attributes["bound"] != null)
                    {
                        int bound = XmlConvert.ToInt32(node.Attributes["bound"].Value);
                        data.originalBound = collider.m_Bound;
                        collider.m_Bound = (DynamicBoneCollider.Bound)bound;
                    }
                    if (data.originalCenter.hasValue || data.originalRadius.hasValue || data.originalHeight.hasValue || data.originalDirection.hasValue || data.originalBound.hasValue)
                        this._dirtyColliders.Add(collider, data);
                }
            }
            XmlNode forceBendGoalsNode = xmlNode.FindChildNode("forceBendGoalsWeight");
            if (forceBendGoalsNode != null && forceBendGoalsNode.Attributes["value"] != null)
                this.forceBendGoalsWeight = XmlConvert.ToBoolean(forceBendGoalsNode.Attributes["value"].Value);
            XmlNode dynamicBonesNode = xmlNode.FindChildNode("dynamicBones");
            if (dynamicBonesNode != null)
            {
                foreach (XmlNode node in dynamicBonesNode.ChildNodes)
                {
                    string root = node.Attributes["root"].Value;
                    DynamicBone db = null;
                    foreach (DynamicBone bone in this._dynamicBones)
                        if (bone.m_Root.name.Equals(root))
                            db = bone;
                    DynamicBoneData data = new DynamicBoneData();

                    if (node.Attributes["weight"] != null)
                    {
                        float weight = XmlConvert.ToSingle(node.Attributes["weight"].Value);
                        data.originalWeight = db.GetWeight();
                        db.SetWeight(weight);
                    }
                    if (node.Attributes["gravityX"] != null && node.Attributes["gravityY"] != null && node.Attributes["gravityZ"] != null)
                    {
                        Vector3 gravity;
                        gravity.x = XmlConvert.ToSingle(node.Attributes["gravityX"].Value);
                        gravity.y = XmlConvert.ToSingle(node.Attributes["gravityY"].Value);
                        gravity.z = XmlConvert.ToSingle(node.Attributes["gravityZ"].Value);
                        data.originalGravity = db.m_Gravity;
                        db.m_Gravity = gravity;
                    }
                    if (node.Attributes["forceX"] != null && node.Attributes["forceY"] != null && node.Attributes["forceZ"] != null)
                    {
                        Vector3 force;
                        force.x = XmlConvert.ToSingle(node.Attributes["forceX"].Value);
                        force.y = XmlConvert.ToSingle(node.Attributes["forceY"].Value);
                        force.z = XmlConvert.ToSingle(node.Attributes["forceZ"].Value);
                        data.originalForce = db.m_Force;
                        db.m_Force = force;
                    }
                    if (node.Attributes["freezeAxis"] != null)
                    {
                        DynamicBone.FreezeAxis axis = (DynamicBone.FreezeAxis)XmlConvert.ToInt32(node.Attributes["freezeAxis"].Value);
                        data.originalFreezeAxis = db.m_FreezeAxis;
                        db.m_FreezeAxis = axis;
                    }
                    if (data.originalWeight.hasValue || data.originalGravity.hasValue || data.originalForce.hasValue || data.originalFreezeAxis.hasValue)
                        this._dirtyDynamicBones.Add(db, data);
                }
            }
        }
        #endregion
    }
}