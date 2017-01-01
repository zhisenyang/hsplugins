using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml;
using Manager;
using RootMotion.Demos;
using RootMotion.FinalIK;
using UnityEngine;
using UnityEngine.UI;

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

        private class TransformData
        {
            private Vector3 _position = Vector3.zero;
            private Quaternion _rotation = Quaternion.identity;
            private Vector3 _scale = Vector3.one;
            private bool _hasPosition;
            private bool _hasRotation;
            private bool _hasScale;
            private Vector3 _originalPosition = Vector3.zero;
            private Quaternion _originalRotation = Quaternion.identity;
            private Vector3 _originalScale = Vector3.one;
            private bool _hasOriginalPosition;
            private bool _hasOriginalRotation;
            private bool _hasOriginalScale;

            public Vector3 position
            {
                get { return this._position; }
                set
                {
                    this._position = value;
                    this._hasPosition = true;
                }
            }

            public Quaternion rotation
            {
                get { return this._rotation; }
                set
                {
                    this._rotation = value;
                    this._hasRotation = true;
                }
            }

            public Vector3 scale
            {
                get { return this._scale; }
                set
                {
                    this._scale = value;
                    this._hasScale = true;
                }
            }
            public Vector3 originalPosition
            {
                get { return this._originalPosition; }
                set
                {
                    this._originalPosition = value;
                    this._hasOriginalPosition = true;
                }
            }

            public Quaternion originalRotation
            {
                get { return this._originalRotation; }
                set
                {
                    this._originalRotation = value;
                    this._hasOriginalRotation = true;
                }
            }

            public Vector3 originalScale
            {
                get { return this._originalScale; }
                set
                {
                    this._originalScale = value;
                    this._hasOriginalScale = true;
                }
            }

            public bool hasPosition { get { return this._hasPosition; } }
            public bool hasRotation { get { return this._hasRotation; } }
            public bool hasScale { get { return this._hasScale; } }
            public bool hasOriginalPosition { get { return this._hasOriginalPosition; } }
            public bool hasOriginalRotation { get { return this._hasOriginalRotation; } }
            public bool hasOriginalScale { get { return this._hasOriginalScale; } }

            public void ResetPosition()
            {
                this._hasPosition = false;
            }

            public void ResetRotation()
            {
                this._hasRotation = false;
            }

            public void ResetScale()
            {
                this._hasScale = false;
            }
            public void ResetOriginalPosition()
            {
                this._hasOriginalPosition = false;
            }

            public void ResetOriginalRotation()
            {
                this._hasOriginalRotation = false;
            }

            public void ResetOriginalScale()
            {
                this._hasScale = false;
            }
        }
        #endregion

        #region Private Variables
        private Animator _animator;
        private FullBodyBipedIK _body;
        private CameraGL _cam;
        private Material _mat;
        private Transform _advancedTarget;
        private readonly Dictionary<FullBodyBipedEffector, int> _effectorToIndex = new Dictionary<FullBodyBipedEffector, int>(); 
        private readonly HashSet<GameObject> _openedGameObjects = new HashSet<GameObject>();
        private bool _advancedCoordWorld = false;
        private CoordType _advancedCoordType = CoordType.Rotation;
        private Vector2 _advancedScroll;
        private float _inc = 1f;
        private readonly Dictionary<GameObject, TransformData> _dirtyObjects = new Dictionary<GameObject, TransformData>();
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
        private Transform _armUpDamL;
        private Transform _armUpDamR;
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
        private Dictionary<Transform, string> _shortcuts = new Dictionary<Transform, string>(); 
        #endregion

        #region Public Accessors
        public StudioChara chara { get; set; }
        public bool isEnabled { get { return this.chara.ikCtrl.ikEnable; } }
        public bool draw { get; set; }
        #endregion

        #region Unity Methods
        void Awake()
        {
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
            this._cam = Camera.current.GetComponent<CameraGL>();
            if (this._cam == null)
                this._cam = Camera.current.gameObject.AddComponent<CameraGL>();
            this._cam.onPostRender += DrawGizmos;
            this._body = this.GetComponent<FullBodyBipedIK>();
            this._body.solver.OnPostSolve = OnPostSolve;
            this._animator = this.GetComponent<Animator>();
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
                this._armUpDamL = this.transform.FindDescendant("cf_J_ArmUp03_dam_L");
                this._armUpDamR = this.transform.FindDescendant("cf_J_ArmUp03_dam_R");
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
                this._shortcuts.Add(this.transform.FindDescendant("cf_J_Hand_s_L"), "L. Hand");
                this._shortcuts.Add(this.transform.FindDescendant("cf_J_Hand_s_R"), "R. Hand");
                this._shortcuts.Add(this.transform.FindDescendant("cf_J_Foot02_L"), "L. Foot");
                this._shortcuts.Add(this.transform.FindDescendant("cf_J_Foot02_R"), "R. Foot");
                this._shortcuts.Add(this.transform.FindDescendant("cf_J_FaceRoot"), "Face");
            }
            else
            {
                this._legKneeBackL = this._body.solver.leftLegMapping.bone1.FindDescendant("cm_J_LegKnee_back_s_L");
                this._legKneeBackR = this._body.solver.rightLegMapping.bone1.FindDescendant("cm_J_LegKnee_back_s_R");
                this._legUpL = this.transform.FindDescendant("cm_J_LegUpDam_L");
                this._legUpR = this.transform.FindDescendant("cm_J_LegUpDam_R");
                this._elbowDamL = this.transform.FindDescendant("cm_J_ArmElbo_dam_02_L");
                this._elbowDamR = this.transform.FindDescendant("cm_J_ArmElbo_dam_02_R");
                this._armUpDamL = this.transform.FindDescendant("cm_J_ArmUp03_dam_L");
                this._armUpDamR = this.transform.FindDescendant("cm_J_ArmUp03_dam_R");
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
                this._shortcuts.Add(this.transform.FindDescendant("cm_J_Hand_s_L"), "L. Hand");
                this._shortcuts.Add(this.transform.FindDescendant("cm_J_Hand_s_R"), "R. Hand");
                this._shortcuts.Add(this.transform.FindDescendant("cm_J_Foot02_L"), "L. Foot");
                this._shortcuts.Add(this.transform.FindDescendant("cm_J_Foot02_R"), "R. Foot");
                this._shortcuts.Add(this.transform.FindDescendant("cm_J_FaceRoot"), "Face");
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
        }

        void LateUpdate()
        {
            if (!this.isEnabled)
                return;
            for (int i = 0; i < 4; ++i)
                this.chara.ikCtrl.drivingRig.bendGoals[i].weight = 1f;
            this._body.solver.Update();
        }

        void OnDestroy()
        {
            this._cam.onPostRender -= DrawGizmos;
            this._animator.enabled = true;
            this._body.solver.spineStiffness = this._cachedSpineStiffness;
            this._body.solver.pullBodyVertical = this._cachedPullBodyVertical;
            this._body.solver.iterations = this._cachedSolverIterations;
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
            GUILayout.BeginVertical(GUILayout.ExpandWidth(true));
            GUILayout.Label("Character Tree");
            this._advancedScroll = GUILayout.BeginScrollView(_advancedScroll, GUI.skin.box, GUILayout.ExpandHeight(true));
            this.DisplayObjectTree(this.transform.GetChild(0).gameObject, 0);
            GUILayout.EndScrollView();
            GUILayout.EndVertical();
            GUILayout.BeginVertical(GUI.skin.box, GUILayout.MinWidth(350f));
            {
                GUILayout.BeginHorizontal(GUI.skin.box);
                GUILayout.Label("Frame of reference: ");
                this._advancedCoordWorld = GUILayout.Toggle(this._advancedCoordWorld, "World");
                this._advancedCoordWorld = !GUILayout.Toggle(!this._advancedCoordWorld, "Local");
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal(GUI.skin.box);
                if (GUILayout.Toggle(this._advancedCoordType == CoordType.Position, "Position"))
                    this._advancedCoordType = CoordType.Position;
                if (GUILayout.Toggle(this._advancedCoordType == CoordType.Rotation, "Rotation"))
                    this._advancedCoordType = CoordType.Rotation;
                if (GUILayout.Toggle(this._advancedCoordType == CoordType.Scale, "Scale"))
                    this._advancedCoordType = CoordType.Scale;
                GUILayout.EndHorizontal();
                switch (this._advancedCoordType)
                {
                    case CoordType.Position:
                        Vector3 position = Vector3.zero;
                        if (this._advancedTarget != null)
                            position = this._advancedCoordWorld ? this._advancedTarget.position : this._advancedTarget.localPosition;
                        GUILayout.BeginHorizontal();
                        GUILayout.Label("X: " + position.x.ToString("0.0000"));
                        GUILayout.BeginHorizontal(GUILayout.MaxWidth(200f));
                        if (GUILayout.RepeatButton((-this._inc).ToString("+0.###;-0.###")) && this._advancedTarget != null)
                        {
                            position -= this._inc * Vector3.right;
                            this.SetObjectDirty(this._advancedTarget.gameObject);
                        }
                        if (GUILayout.RepeatButton(this._inc.ToString("+0.###;-0.###")) && this._advancedTarget != null)
                        {
                            position += this._inc * Vector3.right;
                            this.SetObjectDirty(this._advancedTarget.gameObject);
                        }
                        GUILayout.EndHorizontal();
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        GUILayout.Label("Y: " + position.y.ToString("0.0000"));
                        GUILayout.BeginHorizontal(GUILayout.MaxWidth(200f));
                        if (GUILayout.RepeatButton((-this._inc).ToString("+0.###;-0.###")) && this._advancedTarget != null)
                        {
                            position -= this._inc * Vector3.up;
                            this.SetObjectDirty(this._advancedTarget.gameObject);
                        }
                        if (GUILayout.RepeatButton(this._inc.ToString("+0.###;-0.###")) && this._advancedTarget != null)
                        {
                            position += this._inc * Vector3.up;
                            this.SetObjectDirty(this._advancedTarget.gameObject);
                        }
                        GUILayout.EndHorizontal();
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        GUILayout.Label("Z: " + position.z.ToString("0.0000"));
                        GUILayout.BeginHorizontal(GUILayout.MaxWidth(200f));
                        if (GUILayout.RepeatButton((-this._inc).ToString("+0.###;-0.###")) && this._advancedTarget != null)
                        {
                            position -= this._inc * Vector3.forward;
                            this.SetObjectDirty(this._advancedTarget.gameObject);
                        }
                        if (GUILayout.RepeatButton(this._inc.ToString("+0.###;-0.###")) && this._advancedTarget != null)
                        {
                            position += this._inc * Vector3.forward;
                            this.SetObjectDirty(this._advancedTarget.gameObject);
                        }
                        GUILayout.EndHorizontal();
                        GUILayout.EndHorizontal();
                        if (this._advancedTarget != null && this.IsObjectDirty(this._advancedTarget.gameObject))
                        {
                            TransformData td = this._dirtyObjects[this._advancedTarget.gameObject];
                            td.position = this._advancedCoordWorld ? this._advancedTarget.TransformPoint(position) : position;
                            if (!td.hasOriginalPosition)
                                td.originalPosition = this._advancedTarget.localPosition;
                        }
                        break;
                    case CoordType.Rotation:
                        Quaternion rotation = Quaternion.identity;
                        if (this._advancedTarget != null)
                            rotation = this._advancedCoordWorld ? this._advancedTarget.rotation : this._advancedTarget.localRotation;
                        GUILayout.BeginHorizontal();
                        GUILayout.Label("X (Pitch): " + rotation.eulerAngles.x.ToString("0.00"));
                        GUILayout.BeginHorizontal(GUILayout.MaxWidth(200f));
                        if (GUILayout.RepeatButton((-this._inc).ToString("+0.###;-0.###")) && this._advancedTarget != null)
                        {
                            rotation *= Quaternion.AngleAxis(-this._inc, Vector3.right);
                            this.SetObjectDirty(this._advancedTarget.gameObject);
                        }
                        if (GUILayout.RepeatButton(this._inc.ToString("+0.###;-0.###")) && this._advancedTarget != null)
                        {
                            rotation *= Quaternion.AngleAxis(this._inc, Vector3.right);
                            this.SetObjectDirty(this._advancedTarget.gameObject);
                        }
                        GUILayout.EndHorizontal();
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        GUILayout.Label("Y (Yaw): " + rotation.eulerAngles.y.ToString("0.00"));
                        GUILayout.BeginHorizontal(GUILayout.MaxWidth(200f));
                        if (GUILayout.RepeatButton((-this._inc).ToString("+0.###;-0.###")) && this._advancedTarget != null)
                        {
                            rotation *= Quaternion.AngleAxis(-this._inc, Vector3.up);
                            this.SetObjectDirty(this._advancedTarget.gameObject);
                        }
                        if (GUILayout.RepeatButton(this._inc.ToString("+0.###;-0.###")) && this._advancedTarget != null)
                        {
                            rotation *= Quaternion.AngleAxis(this._inc, Vector3.up);
                            this.SetObjectDirty(this._advancedTarget.gameObject);
                        }
                        GUILayout.EndHorizontal();
                        GUILayout.EndHorizontal();
                        GUILayout.BeginHorizontal();
                        GUILayout.Label("Z (Roll): " + rotation.eulerAngles.z.ToString("0.00"));
                        GUILayout.BeginHorizontal(GUILayout.MaxWidth(200f));
                        if (GUILayout.RepeatButton((-this._inc).ToString("+0.###;-0.###")) && this._advancedTarget != null)
                        {
                            rotation *= Quaternion.AngleAxis(-this._inc, Vector3.forward);
                            this.SetObjectDirty(this._advancedTarget.gameObject);
                        }
                        if (GUILayout.RepeatButton(this._inc.ToString("+0.###;-0.###")) && this._advancedTarget != null)
                        {
                            rotation *= Quaternion.AngleAxis(this._inc, Vector3.forward);
                            this.SetObjectDirty(this._advancedTarget.gameObject);
                        }
                        GUILayout.EndHorizontal();
                        GUILayout.EndHorizontal();
                        if (this._advancedTarget != null && this.IsObjectDirty(this._advancedTarget.gameObject))
                        {
                            TransformData td = this._dirtyObjects[this._advancedTarget.gameObject];
                            td.rotation = this._advancedCoordWorld ? rotation * Quaternion.Inverse(this._advancedTarget.parent.rotation) : rotation;
                            if (!td.hasOriginalRotation)
                                td.originalRotation = this._advancedTarget.localRotation;
                        }
                        break;
                    case CoordType.Scale:
                        Vector3 scale = Vector3.one;
                        if (this._advancedTarget != null)
                            scale = this._advancedCoordWorld ? this._advancedTarget.lossyScale : this._advancedTarget.localScale;
                        GUI.enabled = !this._advancedCoordWorld;
                        GUILayout.BeginHorizontal();
                        GUILayout.Label("X: " + scale.x.ToString("0.0000"));
                        GUILayout.BeginHorizontal(GUILayout.MaxWidth(200f));
                        if (GUILayout.RepeatButton((-this._inc).ToString("+0.###;-0.###")) && this._advancedTarget != null)
                        {
                            scale -= this._inc * Vector3.right;
                            this.SetObjectDirty(this._advancedTarget.gameObject);
                        }
                        if (GUILayout.RepeatButton(this._inc.ToString("+0.###;-0.###")) && this._advancedTarget != null)
                        {
                            scale += this._inc * Vector3.right;
                            this.SetObjectDirty(this._advancedTarget.gameObject);
                        }
                        GUILayout.EndHorizontal();
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        GUILayout.Label("Y: " + scale.y.ToString("0.0000"));
                        GUILayout.BeginHorizontal(GUILayout.MaxWidth(200f));
                        if (GUILayout.RepeatButton((-this._inc).ToString("+0.###;-0.###")) && this._advancedTarget != null)
                        {
                            scale -= this._inc * Vector3.up;
                            this.SetObjectDirty(this._advancedTarget.gameObject);
                        }
                        if (GUILayout.RepeatButton(this._inc.ToString("+0.###;-0.###")) && this._advancedTarget != null)
                        {
                            scale += this._inc * Vector3.up;
                            this.SetObjectDirty(this._advancedTarget.gameObject);
                        }
                        GUILayout.EndHorizontal();
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        GUILayout.Label("Z: " + scale.z.ToString("0.0000"));
                        GUILayout.BeginHorizontal(GUILayout.MaxWidth(200f));
                        if (GUILayout.RepeatButton((-this._inc).ToString("+0.###;-0.###")) && this._advancedTarget != null)
                        {
                            scale -= this._inc * Vector3.forward;
                            this.SetObjectDirty(this._advancedTarget.gameObject);
                        }
                        if (GUILayout.RepeatButton(this._inc.ToString("+0.###;-0.###")) && this._advancedTarget != null)
                        {
                            scale += this._inc * Vector3.forward;
                            this.SetObjectDirty(this._advancedTarget.gameObject);
                        }
                        GUILayout.EndHorizontal();
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        GUILayout.Label("X/Y/Z");
                        GUILayout.BeginHorizontal(GUILayout.MaxWidth(200f));
                        if (GUILayout.RepeatButton((-this._inc).ToString("+0.###;-0.###")) && this._advancedTarget != null)
                        {
                            scale -= this._inc * Vector3.one;
                            this.SetObjectDirty(this._advancedTarget.gameObject);
                        }
                        if (GUILayout.RepeatButton(this._inc.ToString("+0.###;-0.###")) && this._advancedTarget != null)
                        {
                            scale += this._inc * Vector3.one;
                            this.SetObjectDirty(this._advancedTarget.gameObject);
                        }
                        GUILayout.EndHorizontal();
                        GUILayout.EndHorizontal();
                        GUI.enabled = true;
                        if (this._advancedTarget != null && this.IsObjectDirty(this._advancedTarget.gameObject))
                        {
                            if (!this._advancedCoordWorld)
                            {
                                TransformData td = this._dirtyObjects[this._advancedTarget.gameObject];
                                td.scale = scale;
                                if (!td.hasOriginalScale)
                                    td.originalScale = this._advancedTarget.localScale;
                            }
                        }
                        break;
                }
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("0.001"))
                    this._inc = 0.001f;
                if (GUILayout.Button("0.01"))
                    this._inc = 0.01f;
                if (GUILayout.Button("0.1"))
                    this._inc = 0.1f;
                if (GUILayout.Button("1"))
                    this._inc = 1f;
                if (GUILayout.Button("10"))
                    this._inc = 10f;
                GUILayout.EndHorizontal();

                if (this._advancedTarget != null)
                {
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Reset Pos.") && this.IsObjectDirty(this._advancedTarget.gameObject))
                    {
                        this._dirtyObjects[this._advancedTarget.gameObject].ResetPosition();
                        this.SetObjectNotDirtyIf(this._advancedTarget.gameObject);
                    }
                    if (GUILayout.Button("Reset Rot.") && this.IsObjectDirty(this._advancedTarget.gameObject))
                    {
                        this._dirtyObjects[this._advancedTarget.gameObject].ResetRotation();
                        this.SetObjectNotDirtyIf(this._advancedTarget.gameObject);
                    }
                    if (GUILayout.Button("Reset Scale") && this.IsObjectDirty(this._advancedTarget.gameObject))
                    {
                        this._dirtyObjects[this._advancedTarget.gameObject].ResetScale();
                        this.SetObjectNotDirtyIf(this._advancedTarget.gameObject);
                    }
                    GUILayout.EndHorizontal();
                }
                GUILayout.BeginVertical(GUI.skin.box);
                GUIStyle style = GUI.skin.GetStyle("Label");
                TextAnchor bak = style.alignment;
                style.alignment = TextAnchor.MiddleCenter;
                GUILayout.Label("Shortcuts", style);
                style.alignment = bak;
                GUILayout.BeginHorizontal();
                foreach (KeyValuePair<Transform, string> kvp in this._shortcuts)
                    if (GUILayout.Button(kvp.Value))
                        this.GoToObject(kvp.Key.gameObject);
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
            }
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
            GUI.DragWindow();
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

        private void GoToObject(GameObject go)
        {
            this._advancedTarget = go.transform;
            go = go.transform.parent.gameObject;
            while (go.transform != this.transform.GetChild(0))
            {
                this._openedGameObjects.Add(go);
                go = go.transform.parent.gameObject;
            }
            this._openedGameObjects.Add(go);
        }

        private void DisplayObjectTree(GameObject go, int indent)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(indent * 20f);
            if (go.transform.childCount != 0)
            {
                if (GUILayout.Toggle(this._openedGameObjects.Contains(go), "", GUILayout.ExpandWidth(false)))
                {
                    if (this._openedGameObjects.Contains(go) == false)
                        this._openedGameObjects.Add(go);
                }
                else
                {
                    if (this._openedGameObjects.Contains(go))
                        this._openedGameObjects.Remove(go);
                }
            }
            else
                GUILayout.Space(21f);
            Color c = GUI.color;
            if (this._dirtyObjects.ContainsKey(go))
                GUI.color = Color.magenta;
            if (this._advancedTarget == go.transform)
                GUI.color = Color.cyan;
            if (GUILayout.Button(go.name + (this.IsObjectDirty(go) ? "*" : ""), GUILayout.ExpandWidth(false)))
                this._advancedTarget = go.transform;
            GUI.color = c;
            GUILayout.EndHorizontal();
            if (this._openedGameObjects.Contains(go))
                for (int i = 0; i < go.transform.childCount; ++i)
                    this.DisplayObjectTree(go.transform.GetChild(i).gameObject, indent + 1);
        }

        private void SetObjectDirty(GameObject go)
        {
            if (!this.IsObjectDirty(go))
            {
                this._dirtyObjects.Add(go, new TransformData());
            }
        }

        private void SetObjectNotDirtyIf(GameObject go)
        {
            if (this.IsObjectDirty(go))
            {
                TransformData data = this._dirtyObjects[go];
                if (data.hasPosition == false && data.hasRotation == false && data.hasScale == false)
                {
                    this._dirtyObjects.Remove(go);
                    if (data.hasOriginalPosition)
                        go.transform.localPosition = data.originalPosition;
                    if (data.hasOriginalRotation)
                        go.transform.localRotation = data.originalRotation;
                    if (data.hasOriginalScale)
                        go.transform.localScale = data.originalScale;
                }
            }
        }

        private bool IsObjectDirty(GameObject go)
        {
            return (this._dirtyObjects.ContainsKey(go));
        }


        private void OnPostSolve()
        {
            if (!this.isEnabled)
                return;
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
            this._armUpDamL.localScale = Vector3.Lerp(Vector3.one, _armDamScale, Quaternion.Angle(this._armElbouraDamL.parent.rotation, this._body.solver.leftArmMapping.bone2.rotation) / 90f);
            this._armUpDamR.localScale = Vector3.Lerp(Vector3.one, _armDamScale, Quaternion.Angle(this._armElbouraDamR.parent.rotation, this._body.solver.rightArmMapping.bone2.rotation) / 90f);
            float handAngle = Extensions.DirectionalAngle(Vector3.forward, this._body.solver.leftArmMapping.bone2.InverseTransformDirection(this._body.solver.leftArmMapping.bone3.forward), Vector3.right);
            this._armLow1L.localRotation = Quaternion.AngleAxis(Mathf.LerpAngle(0f, handAngle, 0.25f), Vector3.right);
            this._armLow2L.localRotation = Quaternion.AngleAxis(Mathf.LerpAngle(0f, handAngle, 0.5f), Vector3.right);
            this._armLow3L.localRotation = Quaternion.AngleAxis(Mathf.LerpAngle(0f, handAngle, 0.75f), Vector3.right);
            handAngle = Extensions.DirectionalAngle(Vector3.forward, this._body.solver.rightArmMapping.bone2.InverseTransformDirection(this._body.solver.rightArmMapping.bone3.forward), Vector3.right);
            this._armLow1R.localRotation = Quaternion.AngleAxis(Mathf.LerpAngle(0f, handAngle, 0.25f), Vector3.right);
            this._armLow2R.localRotation = Quaternion.AngleAxis(Mathf.LerpAngle(0f, handAngle, 0.5f), Vector3.right);
            this._armLow3R.localRotation = Quaternion.AngleAxis(Mathf.LerpAngle(0f, handAngle, 0.75f), Vector3.right);
            this._siriDamL.rotation = this._body.solver.leftLegMapping.bone1.rotation;
            this._siriDamL.localRotation *= Quaternion.AngleAxis(Quaternion.Angle(this._siriDamL.parent.rotation, this._body.solver.leftLegMapping.bone1.rotation) / 2f, Vector3.right);
            this._siriDamR.rotation = this._body.solver.rightLegMapping.bone1.rotation;
            this._siriDamR.localRotation *= Quaternion.AngleAxis(Quaternion.Angle(this._siriDamR.parent.rotation, this._body.solver.rightLegMapping.bone1.rotation) / 2f, Vector3.right);
            this._kosi.rotation = Quaternion.Lerp(this._kosi.parent.rotation, Quaternion.Lerp(this._body.solver.leftLegMapping.bone1.rotation, this._body.solver.rightLegMapping.bone1.rotation, 0.5f), 0.25f);

            foreach (KeyValuePair<GameObject, TransformData> kvp in this._dirtyObjects)
            {
                if (kvp.Value.hasPosition)
                    kvp.Key.transform.localPosition = kvp.Value.position;
                if (kvp.Value.hasRotation)
                    kvp.Key.transform.localRotation = kvp.Value.rotation;
                if (kvp.Value.hasScale)
                    kvp.Key.transform.localScale = kvp.Value.scale;
            }
        }

        private void DrawGizmos()
        {
            if (!this.draw || !this.isEnabled)
                return;
            GL.PushMatrix();
            this._mat.SetPass(0);
            GL.LoadProjectionMatrix(Studio.Instance.MainCamera.projectionMatrix);
            GL.MultMatrix(Studio.Instance.MainCamera.transform.worldToLocalMatrix);
            GL.Begin(GL.LINES);
            if (this._advancedTarget)
                this.GLDrawCube(this._advancedTarget.position, this._advancedTarget.rotation, 0.025f, true, true, true);
            GL.End();
            GL.PopMatrix();
        }

        private void GLDrawCube(Vector3 position, Quaternion rotation, float size, bool up = false, bool forward = false, bool right = false)
        {
            Vector3 topLeftForward = position + (rotation * ((Vector3.up + Vector3.left + Vector3.forward) * size)),
                    topRightForward = position + (rotation * ((Vector3.up + Vector3.right + Vector3.forward) * size)),
                    bottomLeftForward = position + (rotation * ((Vector3.down + Vector3.left + Vector3.forward) * size)),
                    bottomRightForward = position + (rotation * ((Vector3.down + Vector3.right + Vector3.forward) * size)),
                    topLeftBack = position + (rotation * ((Vector3.up + Vector3.left + Vector3.back) * size)),
                    topRightBack = position + (rotation * ((Vector3.up + Vector3.right + Vector3.back) * size)),
                    bottomLeftBack = position + (rotation * ((Vector3.down + Vector3.left + Vector3.back) * size)),
                    bottomRightBack = position + (rotation * ((Vector3.down + Vector3.right + Vector3.back) * size));
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
            if (up)
            {
                GL.Vertex(position);
                GL.Vertex(position + (rotation * (Vector3.up * size * 2)));
            }
            if (right)
            {
                GL.Vertex(position);
                GL.Vertex(position + (rotation * (Vector3.right * size * 2)));
            }
            if (forward)
            {
                GL.Vertex(position);
                GL.Vertex(position + (rotation * (Vector3.forward * size * 2)));
            }
        }
        #endregion

        #region Saves
        public void LoadBinary(BinaryReader binaryReader)
        {
            this.LoadVersion_1_0_0(binaryReader);
        }

        public void LoadXml(XmlReader xmlReader, HSPE.VersionNumber v)
        {
            this.LoadDefaultVersion(xmlReader);
        }
        public int SaveXml(XmlTextWriter xmlWriter)
        {
            xmlWriter.WriteStartElement("advancedObjects");
            xmlWriter.WriteAttributeString("count", this._dirtyObjects.Count.ToString());
            foreach (KeyValuePair<GameObject, TransformData> kvp in this._dirtyObjects)
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

                if (kvp.Value.hasPosition)
                {
                    xmlWriter.WriteAttributeString("posX", XmlConvert.ToString(kvp.Value.position.x));
                    xmlWriter.WriteAttributeString("posY", XmlConvert.ToString(kvp.Value.position.y));
                    xmlWriter.WriteAttributeString("posZ", XmlConvert.ToString(kvp.Value.position.z));
                }

                if (kvp.Value.hasRotation)
                {
                    xmlWriter.WriteAttributeString("rotW", XmlConvert.ToString(kvp.Value.rotation.w));
                    xmlWriter.WriteAttributeString("rotX", XmlConvert.ToString(kvp.Value.rotation.x));
                    xmlWriter.WriteAttributeString("rotY", XmlConvert.ToString(kvp.Value.rotation.y));
                    xmlWriter.WriteAttributeString("rotZ", XmlConvert.ToString(kvp.Value.rotation.z));
                }

                if (kvp.Value.hasScale)
                {
                    xmlWriter.WriteAttributeString("scaleX", XmlConvert.ToString(kvp.Value.scale.x));
                    xmlWriter.WriteAttributeString("scaleY", XmlConvert.ToString(kvp.Value.scale.y));
                    xmlWriter.WriteAttributeString("scaleZ", XmlConvert.ToString(kvp.Value.scale.z));
                }
                xmlWriter.WriteEndElement();
            }
            xmlWriter.WriteEndElement();
            return this._dirtyObjects.Count;
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
                this._dirtyObjects.Add(obj, new TransformData() { position = pos, rotation = rot, originalPosition = obj.transform.localPosition, originalRotation = obj.transform.localRotation});
            }
        }

        private void LoadDefaultVersion(XmlReader xmlReader)
        {
            bool shouldContinue;
            while ((shouldContinue = xmlReader.Read()) == true && xmlReader.NodeType == XmlNodeType.Whitespace)
                ;
            if (xmlReader.NodeType != XmlNodeType.Element || xmlReader.Name != "advancedObjects")
                return;
            int count = XmlConvert.ToInt32(xmlReader.GetAttribute("count"));
            if (!shouldContinue)
                return;
            int i = 0;
            while (i < count && xmlReader.Read())
            {
                if (xmlReader.NodeType == XmlNodeType.Element)
                {
                    if (xmlReader.Name == "object")
                    {
                        string name = xmlReader.GetAttribute("name");
                        GameObject obj = this.transform.Find(name).gameObject;
                        TransformData data = new TransformData();
                        if (xmlReader.GetAttribute("posX") != null && xmlReader.GetAttribute("posY") != null && xmlReader.GetAttribute("posZ") != null)
                        {
                            Vector3 pos;
                            pos.x = XmlConvert.ToSingle(xmlReader.GetAttribute("posX"));
                            pos.y = XmlConvert.ToSingle(xmlReader.GetAttribute("posY"));
                            pos.z = XmlConvert.ToSingle(xmlReader.GetAttribute("posZ"));
                            data.position = pos;
                            data.originalPosition = obj.transform.localPosition;
                        }
                        if (xmlReader.GetAttribute("rotW") != null && xmlReader.GetAttribute("rotX") != null && xmlReader.GetAttribute("rotY") != null && xmlReader.GetAttribute("rotZ") != null)
                        {
                            Quaternion rot;
                            rot.w = XmlConvert.ToSingle(xmlReader.GetAttribute("rotW"));
                            rot.x = XmlConvert.ToSingle(xmlReader.GetAttribute("rotX"));
                            rot.y = XmlConvert.ToSingle(xmlReader.GetAttribute("rotY"));
                            rot.z = XmlConvert.ToSingle(xmlReader.GetAttribute("rotZ"));
                            data.rotation = rot;
                            data.originalRotation = obj.transform.localRotation;
                        }
                        if (xmlReader.GetAttribute("scaleX") != null && xmlReader.GetAttribute("scaleY") != null && xmlReader.GetAttribute("scaleZ") != null)
                        {
                            Vector3 scale;
                            scale.x = XmlConvert.ToSingle(xmlReader.GetAttribute("scaleX"));
                            scale.y = XmlConvert.ToSingle(xmlReader.GetAttribute("scaleY"));
                            scale.z = XmlConvert.ToSingle(xmlReader.GetAttribute("scaleZ"));
                            data.scale = scale;
                            data.originalScale = obj.transform.localScale;
                        }
                        if (data.hasPosition || data.hasRotation || data.hasScale)
                            this._dirtyObjects.Add(obj, data);
                        ++i;
                    }
                }
            }
        }
        #endregion
    }
}