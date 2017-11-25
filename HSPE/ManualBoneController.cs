using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Permissions;
using System.Xml;
using RootMotion.FinalIK;
using Studio;
using UnityEngine;
using UnityEngine.UI;

namespace HSPE
{
    public class ManualBoneController : MonoBehaviour
    {
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

            public TransformData() { }

            public TransformData(TransformData other)
            {
                this.position = other.position;
                this.rotation = other.rotation;
                this.scale = other.scale;
                this.originalPosition = other.originalPosition;
                this.originalRotation = other.originalRotation;
                this.originalScale = other.originalScale;
            }
        }

        private class BoobData
        {
            public EditableValue<Vector3> gravity;
            public EditableValue<Vector3> force;
            public EditableValue<Vector3> originalGravity;
            public EditableValue<Vector3> originalForce;

            public BoobData() { }

            public BoobData(BoobData other)
            {
                this.force = other.force;
                this.gravity = other.gravity;
                this.originalGravity = other.originalGravity;
                this.originalForce = other.originalForce;
            }
        }

        private class ColliderData
        {
            public EditableValue<Vector3> originalCenter;
            public EditableValue<float> originalRadius;
            public EditableValue<float> originalHeight;
            public EditableValue<DynamicBoneCollider.Direction> originalDirection;
            public EditableValue<DynamicBoneCollider.Bound> originalBound;

            public ColliderData() { }

            public ColliderData(ColliderData other)
            {
                this.originalCenter = other.originalCenter;
                this.originalRadius = other.originalRadius;
                this.originalHeight = other.originalHeight;
                this.originalDirection = other.originalDirection;
                this.originalBound = other.originalBound;
            }
        }

        private class DynamicBoneData
        {
            public EditableValue<float> originalWeight;
            public EditableValue<Vector3> originalGravity;
            public EditableValue<Vector3> originalForce;
            public EditableValue<DynamicBone.FreezeAxis> originalFreezeAxis;

            public DynamicBoneData() { }

            public DynamicBoneData(DynamicBoneData other)
            {
                this.originalWeight = other.originalWeight;
                this.originalGravity = other.originalGravity;
                this.originalForce = other.originalForce;
                this.originalFreezeAxis = other.originalFreezeAxis;
            }
        }

        private delegate void TabDelegate();
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
        private CameraGL _cam;
        private Material _mat;
        private Material _xMat;
        private Material _yMat;
        private Material _zMat;
        private Material _colliderMat;
        private Transform _boneTarget;
        private Transform _twinBoneTarget;
        private DynamicBoneCollider _colliderTarget;
        private DynamicBone_Ver02 _rightBoob;
        private DynamicBone_Ver02 _leftBoob;
        private readonly Dictionary<FullBodyBipedEffector, int> _effectorToIndex = new Dictionary<FullBodyBipedEffector, int>();
        private readonly Dictionary<FullBodyBipedChain, int> _chainToIndex = new Dictionary<FullBodyBipedChain, int>();
        private readonly HashSet<GameObject> _openedBones = new HashSet<GameObject>();
        private readonly HashSet<GameObject> _ignoredObjects = new HashSet<GameObject>();
        private readonly Dictionary<GameObject, OCIChar.BoneInfo> _fkObjects = new Dictionary<GameObject, OCIChar.BoneInfo>();
        private readonly HashSet<Transform> _colliderObjects = new HashSet<Transform>();
        private List<DynamicBone> _dynamicBones = new List<DynamicBone>();
        private CoordType _boneEditionCoordType = CoordType.Rotation;
        private Vector2 _boneEditionScroll;
        private float _inc = 1f;
        private int _incIndex = 0;
        private Dictionary<GameObject, TransformData> _dirtyBones = new Dictionary<GameObject, TransformData>();
        private readonly Dictionary<DynamicBone_Ver02, BoobData> _dirtyBoobs = new Dictionary<DynamicBone_Ver02, BoobData>(2);
        private readonly Dictionary<DynamicBoneCollider, ColliderData> _dirtyColliders = new Dictionary<DynamicBoneCollider, ColliderData>();
        private readonly Dictionary<DynamicBone, DynamicBoneData> _dirtyDynamicBones = new Dictionary<DynamicBone, DynamicBoneData>();

        private bool _isFemale = false;
        private float _cachedSpineStiffness;
        private float _cachedPullBodyVertical;
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
        private readonly Dictionary<int, Vector3> _oldPosValues = new Dictionary<int, Vector3>();
        private readonly Dictionary<int, Vector3> _oldRotValues = new Dictionary<int, Vector3>();
        private Vector3 _oldFKRotationValue = Vector3.zero;
        private Vector3 _oldFKTwinRotationValue = Vector3.zero;
        private bool _lastShouldSaveValue = false;
        private bool _lockDrag = false;
        private bool _symmetricalEdition = false;
        private readonly Color _redColor = Color.red;
        private readonly Color _greenColor = Color.green;
        private readonly Color _blueColor = Color.Lerp(Color.blue, Color.cyan, 0.5f);
        private string _currentAlias = "";
        private bool _optimizeIK = true;
        #endregion

        #region Public Accessors
        public OCIChar chara { get; set; }
        public bool isIKEnabled { get { return this.chara.oiCharInfo.enableIK; } }
        public bool drawAdvancedMode { get; set; }
        public Rect colliderEditRect { get { return this._colliderEditRect; } }
        public bool colliderEditEnabled { get { return this._colliderTarget != null; } }
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
        #endregion

        #region Unity Methods
        void Awake()
        {
            this._tabFunctions.Add(SelectedTab.BonesPosition, this.BonesPosition);
            this._tabFunctions.Add(SelectedTab.BoobsEditor, this.BoobsEditor);
            this._tabFunctions.Add(SelectedTab.DynamicBonesEditor, this.DynamicBonesEditor);
            this._effectorToIndex.Add(FullBodyBipedEffector.Body, 0);
            this._effectorToIndex.Add(FullBodyBipedEffector.LeftShoulder, 1);
            this._chainToIndex.Add(FullBodyBipedChain.LeftArm, 2);
            this._effectorToIndex.Add(FullBodyBipedEffector.LeftHand, 3);
            this._effectorToIndex.Add(FullBodyBipedEffector.RightShoulder, 4);
            this._chainToIndex.Add(FullBodyBipedChain.RightArm, 5);
            this._effectorToIndex.Add(FullBodyBipedEffector.RightHand, 6);
            this._effectorToIndex.Add(FullBodyBipedEffector.LeftThigh, 7);
            this._chainToIndex.Add(FullBodyBipedChain.LeftLeg, 8);
            this._effectorToIndex.Add(FullBodyBipedEffector.LeftFoot, 9);
            this._effectorToIndex.Add(FullBodyBipedEffector.RightThigh, 10);
            this._chainToIndex.Add(FullBodyBipedChain.RightLeg, 11);
            this._effectorToIndex.Add(FullBodyBipedEffector.RightFoot, 12);

            this._mat = new Material(Shader.Find("Unlit/Color")) { color = Color.white };
            this._xMat = new Material(Shader.Find("Unlit/Color")) { color = this._redColor };
            this._yMat = new Material(Shader.Find("Unlit/Color")) { color = this._greenColor };
            this._zMat = new Material(Shader.Find("Unlit/Color")) { color = this._blueColor };
            this._colliderMat = new Material(Shader.Find("Unlit/Color")) { color = Color.Lerp(this._greenColor, Color.white, 0.5f) };
            this._cam = Studio.Studio.Instance.cameraCtrl.mainCmaera.GetComponent<CameraGL>();
            if (this._cam == null)
                this._cam = Studio.Studio.Instance.cameraCtrl.mainCmaera.gameObject.AddComponent<CameraGL>();
            this._cam.onPostRender += this.DrawGizmos;
            foreach (DynamicBoneCollider c in this.GetComponentsInChildren<DynamicBoneCollider>(true))
                this._colliderObjects.Add(c.transform);
            this._dynamicBones = this.GetComponentsInChildren<DynamicBone>(true).ToList();
            MainWindow.self.onParentage += this.OnParentage;
            MainWindow.self.onPostUpdate += this.OnPostUpdate;
        }

        void Start()
        {
            this._isFemale = this.chara.charInfo.Sex == 1;
            this._body = this.chara.animeIKCtrl.IK;

            if (this._isFemale)
            {
                this._boneEditionShortcuts.Add(this.transform.FindDescendant("cf_J_Hand_s_L"), "L. Hand");
                this._boneEditionShortcuts.Add(this.transform.FindDescendant("cf_J_Hand_s_R"), "R. Hand");
                this._boneEditionShortcuts.Add(this.transform.FindDescendant("cf_J_Foot02_L"), "L. Foot");
                this._boneEditionShortcuts.Add(this.transform.FindDescendant("cf_J_Foot02_R"), "R. Foot");
                this._boneEditionShortcuts.Add(this.transform.FindDescendant("cf_J_FaceRoot"), "Face");
                this._leftBoob = ((CharFemaleBody)this.chara.charBody).getDynamicBone(CharFemaleBody.DynamicBoneKind.BreastL);
                this._rightBoob = ((CharFemaleBody)this.chara.charBody).getDynamicBone(CharFemaleBody.DynamicBoneKind.BreastR);
            }
            else
            {
                this._boneEditionShortcuts.Add(this.transform.FindDescendant("cm_J_Hand_s_L"), "L. Hand");
                this._boneEditionShortcuts.Add(this.transform.FindDescendant("cm_J_Hand_s_R"), "R. Hand");
                this._boneEditionShortcuts.Add(this.transform.FindDescendant("cm_J_Foot02_L"), "L. Foot");
                this._boneEditionShortcuts.Add(this.transform.FindDescendant("cm_J_Foot02_R"), "R. Foot");
                this._boneEditionShortcuts.Add(this.transform.FindDescendant("cm_J_FaceRoot"), "Face");
            }
            foreach (OCIChar.BoneInfo bone in this.chara.listBones)
                this._fkObjects.Add(bone.guideObject.transformTarget.gameObject, bone);

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

        }

        void Update()
        {
            if (this._repeatCalled)
                this._repeatTimer += Time.unscaledDeltaTime;
            else
                this._repeatTimer = 0f;
            this._repeatCalled = false;
            DynamicBone[] dynamicBones = this.GetComponentsInChildren<DynamicBone>(true);
            List<DynamicBone> toDelete = null;
            foreach (DynamicBone db in this._dynamicBones)
                if (dynamicBones.Contains(db) == false)
                {
                    if (toDelete == null)
                        toDelete = new List<DynamicBone>();
                    toDelete.Add(db);
                }
            if (toDelete != null)
            {
                foreach (DynamicBone db in toDelete)
                {
                    if (this._dirtyDynamicBones.ContainsKey(db))
                        this._dirtyDynamicBones.Remove(db);
                    this._dynamicBones.Remove(db);
                }
                this._dynamicBoneTarget = null;
            }
            List<DynamicBone> toAdd = null;
            foreach (DynamicBone db in dynamicBones)
                if (this._dynamicBones.Contains(db) == false)
                {
                    if (toAdd == null)
                        toAdd = new List<DynamicBone>();
                    toAdd.Add(db);
                }
            if (toAdd != null)
                foreach (DynamicBone db in toAdd)
                    this._dynamicBones.Add(db);
        }

        void OnDestroy()
        {
            MainWindow.self.onParentage -= this.OnParentage;
            MainWindow.self.onPostUpdate -= this.OnPostUpdate;
            this._cam.onPostRender -= this.DrawGizmos;
            this._body.solver.spineStiffness = this._cachedSpineStiffness;
            this._body.solver.pullBodyVertical = this._cachedPullBodyVertical;
        }

        void OnGUI()
        {
            GUIUtility.ScaleAroundPivot(Vector2.one * (MainWindow.self.uiScale * MainWindow.self.resolutionRatio), new Vector2(Screen.width, Screen.height));
            if (this._colliderTarget)
            {
                for (int i = 0; i < 3; ++i)
                    GUI.Box(this._colliderEditRect, "");
                this._colliderEditRect = GUILayout.Window(2, this._colliderEditRect, this.ColliderEditor, "Collider Editor" + (this.IsColliderDirty(this._colliderTarget) ? "*" : ""));
            }
            GUIUtility.ScaleAroundPivot(Vector2.one, new Vector2(Screen.width, Screen.height));
        }
        #endregion

        #region Public Methods
        public void LoadFrom(ManualBoneController other)
        {
            if (other == null)
                return;
            foreach (GameObject openedBone in other._openedBones)
            {
                Transform obj = this.transform.FindChild(openedBone.transform.GetPathFrom(other.transform));
                if (obj != null)
                    this._openedBones.Add(obj.gameObject);
            }
            foreach (GameObject ignoredObject in other._ignoredObjects)
            {
                Transform obj = this.transform.FindChild(ignoredObject.transform.GetPathFrom(other.transform));
                if (obj != null)
                    this._ignoredObjects.Add(obj.gameObject);
            }
            foreach (KeyValuePair<GameObject, TransformData> kvp in other._dirtyBones)
            {
                Transform obj = this.transform.FindChild(kvp.Key.transform.GetPathFrom(other.transform));
                if (obj != null)
                    this._dirtyBones.Add(obj.gameObject, new TransformData(kvp.Value));
            }
            foreach (KeyValuePair<DynamicBone_Ver02, BoobData> kvp in other._dirtyBoobs)
            {
                Transform obj = this.transform.FindChild(kvp.Key.transform.GetPathFrom(other.transform));
                if (obj != null)
                {
                    DynamicBone_Ver02 db = obj.GetComponent<DynamicBone_Ver02>();
                    if (kvp.Value.originalForce.hasValue)
                        db.Force = kvp.Key.Force;
                    if (kvp.Value.originalGravity.hasValue)
                        db.Gravity = kvp.Key.Gravity;
                    this._dirtyBoobs.Add(db, new BoobData(kvp.Value));
                }
            }
            foreach (KeyValuePair<DynamicBoneCollider, ColliderData> kvp in other._dirtyColliders)
            {
                Transform obj = this.transform.FindChild(kvp.Key.transform.GetPathFrom(other.transform));
                if (obj != null)
                {
                    DynamicBoneCollider col = obj.GetComponent<DynamicBoneCollider>();
                    if (kvp.Value.originalCenter.hasValue)
                        col.m_Center = kvp.Key.m_Center;
                    if (kvp.Value.originalBound.hasValue)
                        col.m_Bound = kvp.Key.m_Bound;
                    if (kvp.Value.originalDirection.hasValue)
                        col.m_Direction = kvp.Key.m_Direction;
                    if (kvp.Value.originalHeight.hasValue)
                        col.m_Height = kvp.Key.m_Height;
                    if (kvp.Value.originalRadius.hasValue)
                        col.m_Radius = kvp.Key.m_Radius;
                    this._dirtyColliders.Add(col, new ColliderData(kvp.Value));

                }
            }
            foreach (KeyValuePair<DynamicBone, DynamicBoneData> kvp in other._dirtyDynamicBones)
            {
                Transform obj = this.transform.FindChild(kvp.Key.transform.GetPathFrom(other.transform));
                if (obj != null)
                {
                    DynamicBone db = obj.GetComponent<DynamicBone>();
                    if (kvp.Value.originalForce.hasValue)
                        db.m_Force = kvp.Key.m_Force;
                    if (kvp.Value.originalFreezeAxis.hasValue)
                        db.m_FreezeAxis = kvp.Key.m_FreezeAxis;
                    if (kvp.Value.originalGravity.hasValue)
                        db.m_Gravity = kvp.Key.m_Gravity;
                    if (kvp.Value.originalWeight.hasValue)
                        db.SetWeight(kvp.Key.GetWeight());
                    this._dirtyDynamicBones.Add(db, new DynamicBoneData(kvp.Value));

                }
            }
            if (other._boneTarget != null)
            {
                Transform t = this.transform.FindChild(other._boneTarget.GetPathFrom(other.transform));
                if (t != null)
                    this._boneTarget = t;
            }
            if (other._colliderTarget != null)
            {
                Transform t = this.transform.FindChild(other._colliderTarget.transform.GetPathFrom(other.transform));
                if (t != null)
                    this._colliderTarget = t.GetComponent<DynamicBoneCollider>();
            }
            if (other._dynamicBoneTarget != null)
            {
                Transform t = this.transform.FindChild(other._dynamicBoneTarget.transform.GetPathFrom(other.transform));
                if (t != null)
                    this._dynamicBoneTarget = t.GetComponent<DynamicBone>();
            }
            this._dynamicBonesScroll = other._dynamicBonesScroll;
            this._boneEditionScroll = other._boneEditionScroll;
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
            if (this.currentDragType == DragType.Position || this.currentDragType == DragType.Both)
            {
                int i = 0;
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
            GuideCommand.EqualsInfo[] rotateCommands = new GuideCommand.EqualsInfo[this._oldRotValues.Count];
            if (this.currentDragType == DragType.Rotation || this.currentDragType == DragType.Both)
            {
                int i = 0;
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
            UndoRedoManager.Instance.Push(new Commands.MoveRotateEqualsCommand(moveCommands, rotateCommands));
            this.currentDragType = DragType.None;
            this._oldPosValues.Clear();
            this._oldRotValues.Clear();
        }

        public bool IsPartEnabled(FullBodyBipedEffector part)
        {
            return this.isIKEnabled && this.chara.listIKTarget[this._effectorToIndex[part]].active;
        }

        public bool IsPartEnabled(FullBodyBipedChain part)
        {
            return this.isIKEnabled && this.chara.listIKTarget[this._chainToIndex[part]].active;
        }

        public void SetBoneTargetRotation(FullBodyBipedEffector type, Quaternion targetRotation)
        {
            if (this.isIKEnabled && this.chara.listIKTarget[this._effectorToIndex[type]].active)
            {
                GuideObject target = this.chara.listIKTarget[this._effectorToIndex[type]].guideObject;
                if (this.currentDragType != DragType.None)
                {
                    if (this._oldRotValues.ContainsKey(target.dicKey) == false)
                        this._oldRotValues.Add(target.dicKey, target.changeAmount.rot);
                    target.changeAmount.rot = targetRotation.eulerAngles;
                }
            }
        }

        public Quaternion GetBoneTargetRotation(FullBodyBipedEffector type)
        {
            if (!this.isIKEnabled || this.chara.listIKTarget[this._effectorToIndex[type]].active == false)
                return Quaternion.identity;
            return this.chara.listIKTarget[this._effectorToIndex[type]].guideObject.transformTarget.localRotation;
        }

        public void SetBoneTargetPosition(FullBodyBipedEffector type, Vector3 targetPosition, bool world = true)
        {
            if (this.isIKEnabled && this.chara.listIKTarget[this._effectorToIndex[type]].active)
            {
                GuideObject target = this.chara.listIKTarget[this._effectorToIndex[type]].guideObject;
                if (this.currentDragType != DragType.None)
                {
                    if (this._oldPosValues.ContainsKey(target.dicKey) == false)
                        this._oldPosValues.Add(target.dicKey, target.changeAmount.pos);
                    if (world)
                        target.changeAmount.pos = target.transformTarget.parent.InverseTransformPoint(targetPosition);
                    else
                        target.changeAmount.pos = targetPosition;
                }
            }
        }

        public Vector3 GetBoneTargetPosition(FullBodyBipedEffector type, bool world = true)
        {
            if (!this.isIKEnabled || this.chara.listIKTarget[this._effectorToIndex[type]].active == false)
                return Vector3.zero;
            if (world)
                return this.chara.listIKTarget[this._effectorToIndex[type]].guideObject.transformTarget.position;
            return this.chara.listIKTarget[this._effectorToIndex[type]].guideObject.transformTarget.localPosition;
        }

        public void SetBendGoalPosition(FullBodyBipedChain type, Vector3 targetPosition, bool world = true)
        {
            if (this.isIKEnabled && this.chara.listIKTarget[this._chainToIndex[type]].active)
            {
                GuideObject target = this.chara.listIKTarget[this._chainToIndex[type]].guideObject;
                if (this.currentDragType != DragType.None)
                {
                    if (this._oldPosValues.ContainsKey(target.dicKey) == false)
                        this._oldPosValues.Add(target.dicKey, target.changeAmount.pos);
                    if (world)
                        target.changeAmount.pos = target.transformTarget.parent.InverseTransformPoint(targetPosition);
                    else
                        target.changeAmount.pos = targetPosition;
                }
            }
        }

        public Vector3 GetBendGoalPosition(FullBodyBipedChain type, bool world = true)
        {
            if (!this.isIKEnabled || this.chara.listIKTarget[this._chainToIndex[type]].active == false)
                return Vector3.zero;
            if (world)
                return this.chara.listIKTarget[this._chainToIndex[type]].guideObject.transformTarget.position;
            return this.chara.listIKTarget[this._chainToIndex[type]].guideObject.transformTarget.localPosition;
        }

        public void CopyLimbToTwin(FullBodyBipedChain limb)
        {
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
                    bendGoalSrc = FullBodyBipedChain.LeftArm;
                    effectorDest = FullBodyBipedEffector.RightHand;
                    bendGoalDest = FullBodyBipedChain.RightArm;
                    effectorSrcRealBone = this._body.references.leftHand;
                    effectorDestRealBone = this._body.references.rightHand;
                    root = this._body.solver.spineMapping.spineBones[this._body.solver.spineMapping.spineBones.Length - 2];
                    break;
                case FullBodyBipedChain.LeftLeg:
                    effectorSrc = FullBodyBipedEffector.LeftFoot;
                    bendGoalSrc = FullBodyBipedChain.LeftLeg;
                    effectorDest = FullBodyBipedEffector.RightFoot;
                    bendGoalDest = FullBodyBipedChain.RightLeg;
                    effectorSrcRealBone = this._body.references.leftFoot;
                    effectorDestRealBone = this._body.references.rightFoot;
                    root = this._body.solver.spineMapping.spineBones[0];
                    break;
                case FullBodyBipedChain.RightArm:
                    effectorSrc = FullBodyBipedEffector.RightHand;
                    bendGoalSrc = FullBodyBipedChain.RightArm;
                    effectorDest = FullBodyBipedEffector.LeftHand;
                    bendGoalDest = FullBodyBipedChain.LeftArm;
                    effectorSrcRealBone = this._body.references.rightHand;
                    effectorDestRealBone = this._body.references.leftHand;
                    root = this._body.solver.spineMapping.spineBones[this._body.solver.spineMapping.spineBones.Length - 2];
                    break;
                case FullBodyBipedChain.RightLeg:
                    effectorSrc = FullBodyBipedEffector.RightFoot;
                    bendGoalSrc = FullBodyBipedChain.RightLeg;
                    effectorDest = FullBodyBipedEffector.LeftFoot;
                    bendGoalDest = FullBodyBipedChain.LeftLeg;
                    effectorSrcRealBone = this._body.references.rightFoot;
                    effectorDestRealBone = this._body.references.leftFoot;
                    root = this._body.solver.spineMapping.spineBones[0];
                    break;
                default:
                    effectorSrc = FullBodyBipedEffector.RightHand;
                    effectorDest = FullBodyBipedEffector.RightHand;
                    bendGoalSrc = FullBodyBipedChain.LeftArm;
                    bendGoalDest = FullBodyBipedChain.LeftArm;
                    effectorSrcRealBone = null;
                    effectorDestRealBone = null;
                    root = null;
                    break;
            }

            Vector3 localPos = root.InverseTransformPoint(this.chara.listIKTarget[this._effectorToIndex[effectorSrc]].guideObject.transformTarget.position);
            localPos.x *= -1f;
            Vector3 effectorPosition = root.TransformPoint(localPos);
            localPos = root.InverseTransformPoint(this.chara.listIKTarget[this._chainToIndex[bendGoalSrc]].guideObject.transformTarget.position);
            localPos.x *= -1f;
            Vector3 bendGoalPosition = root.TransformPoint(localPos);
            this.StartDrag(DragType.Both);
            this._lockDrag = true;

            this.SetBoneTargetPosition(effectorDest, effectorPosition);
            this.SetBendGoalPosition(bendGoalDest, bendGoalPosition);
            this.StartCoroutine(this.ExecuteDelayed(() =>
            {
                Quaternion rot = effectorSrcRealBone.localRotation;
                rot.Set(-rot.x, rot.y, rot.z, -rot.w);
                rot = effectorDestRealBone.parent.rotation * rot;
                rot *= Quaternion.Inverse(this.chara.listIKTarget[this._effectorToIndex[effectorDest]].guideObject.transformTarget.parent.rotation);

                this.SetBoneTargetRotation(effectorDest, rot);

                this._lockDrag = false;
                this.StopDrag();
            }));
        }

        public void AdvancedModeWindow(int id)
        {
            GUILayout.BeginHorizontal();
            for (int i = 0; i < (int)SelectedTab.Count; ++i)
                if (this.ShouldDisplayTab((SelectedTab)i) && GUILayout.Button(((SelectedTab)i).ToString()))
                    this._selectedTab = (SelectedTab)i;
            Color c = GUI.color;
            GUI.color = this._redColor;
            if (GUILayout.Button("Close"))
                this.drawAdvancedMode = false;
            GUI.color = c;
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

        private void IncEditor(int maxHeight = 75, bool label = false)
        {
            GUILayout.BeginVertical();
            if (label)
                GUILayout.Label("10^1", GUI.skin.box, GUILayout.MaxWidth(45));
            Color c = GUI.color;
            GUI.color = Color.white;
            float maxWidth = label ? 45 : 20;
            GUILayout.BeginHorizontal(GUILayout.MaxWidth(maxWidth));
            GUILayout.FlexibleSpace();
            this._incIndex = Mathf.RoundToInt(GUILayout.VerticalSlider(this._incIndex, 1f, -5f, GUILayout.MaxHeight(maxHeight)));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUI.color = c;
            this._inc = Mathf.Pow(10, this._incIndex);
            if (label)
                GUILayout.Label("10^-5", GUI.skin.box, GUILayout.MaxWidth(45));
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
            GUI.color = this._redColor;
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
            this.DisplayObjectTree(this.transform.GetChild(0).gameObject, 0);
            GUILayout.EndScrollView();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Alias", GUILayout.ExpandWidth(false));
            this._currentAlias = GUILayout.TextField(this._currentAlias, GUILayout.ExpandWidth(true));
            if (GUILayout.Button("Save", GUILayout.ExpandWidth(false)))
            {
                if (this._boneTarget != null)
                {
                    this._currentAlias = this._currentAlias.Trim();
                    if (this._currentAlias.Length == 0)
                    {
                        if (MainWindow.self.boneAliases.ContainsKey(this._boneTarget.name))
                            MainWindow.self.boneAliases.Remove(this._boneTarget.name);
                    }
                    else
                    {
                        if (MainWindow.self.boneAliases.ContainsKey(this._boneTarget.name) == false)
                            MainWindow.self.boneAliases.Add(this._boneTarget.name, this._currentAlias);
                        else
                            MainWindow.self.boneAliases[this._boneTarget.name] = this._currentAlias;
                    }
                }
                else
                    this._currentAlias = "";
            }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Legend:");
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
                OCIChar.BoneInfo fkBoneInfo = null;
                if (this._boneTarget != null && this.chara.oiCharInfo.enableFK)
                    this._fkObjects.TryGetValue(this._boneTarget.gameObject, out fkBoneInfo);
                OCIChar.BoneInfo fkTwinBoneInfo = null;
                if (this._symmetricalEdition && this._twinBoneTarget != null && this.chara.oiCharInfo.enableFK)
                    this._fkObjects.TryGetValue(this._twinBoneTarget.gameObject, out fkTwinBoneInfo);
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
                        {
                            if (this.IsBoneDirty(this._boneTarget.gameObject) && this._dirtyBones[this._boneTarget.gameObject].position.hasValue)
                                position = this._dirtyBones[this._boneTarget.gameObject].position;
                            else
                                position = this._boneTarget.localPosition;
                        }
                        bool shouldSaveValue = false;
                        Color c = GUI.color;
                        GUI.color = this._redColor;
                        GUILayout.BeginHorizontal();
                        GUILayout.Label("X:\t" + position.x.ToString("0.00000"));
                        GUILayout.BeginHorizontal(GUILayout.MaxWidth(160f));
                        if (GUILayout.RepeatButton((-this._inc).ToString("+0.#####;-0.#####")) && this.RepeatControl() && this._boneTarget != null)
                        {
                            shouldSaveValue = true;
                            position -= this._inc * Vector3.right;
                        }
                        if (GUILayout.RepeatButton(this._inc.ToString("+0.#####;-0.#####")) && this.RepeatControl() && this._boneTarget != null)
                        {
                            shouldSaveValue = true;
                            position += this._inc * Vector3.right;
                        }
                        GUILayout.EndHorizontal();
                        GUILayout.EndHorizontal();
                        GUI.color = c;

                        GUI.color = this._greenColor;
                        GUILayout.BeginHorizontal();
                        GUILayout.Label("Y:\t" + position.y.ToString("0.00000"));
                        GUILayout.BeginHorizontal(GUILayout.MaxWidth(160f));
                        if (GUILayout.RepeatButton((-this._inc).ToString("+0.#####;-0.#####")) && this.RepeatControl() && this._boneTarget != null)
                        {
                            shouldSaveValue = true;
                            position -= this._inc * Vector3.up;
                        }
                        if (GUILayout.RepeatButton(this._inc.ToString("+0.#####;-0.#####")) && this.RepeatControl() && this._boneTarget != null)
                        {
                            shouldSaveValue = true;
                            position += this._inc * Vector3.up;
                        }
                        GUILayout.EndHorizontal();
                        GUILayout.EndHorizontal();
                        GUI.color = c;

                        GUI.color = this._blueColor;
                        GUILayout.BeginHorizontal();
                        GUILayout.Label("Z:\t" + position.z.ToString("0.00000"));
                        GUILayout.BeginHorizontal(GUILayout.MaxWidth(160f));
                        if (GUILayout.RepeatButton((-this._inc).ToString("+0.#####;-0.#####")) && this.RepeatControl() && this._boneTarget != null)
                        {
                            shouldSaveValue = true;
                            position -= this._inc * Vector3.forward;
                        }
                        if (GUILayout.RepeatButton(this._inc.ToString("+0.#####;-0.#####")) && this.RepeatControl() && this._boneTarget != null)
                        {
                            shouldSaveValue = true;
                            position += this._inc * Vector3.forward;
                        }
                        GUILayout.EndHorizontal();
                        GUILayout.EndHorizontal();
                        GUI.color = c;
                        if (Event.current.rawType == EventType.Repaint)
                        {
                            if (this._boneTarget != null && shouldSaveValue)
                            {
                                this.SetBoneDirty(this._boneTarget.gameObject);
                                TransformData td = this._dirtyBones[this._boneTarget.gameObject];
                                td.position = position;
                                if (!td.originalPosition.hasValue)
                                    td.originalPosition = this._boneTarget.localPosition;

                                if (this._symmetricalEdition && this._twinBoneTarget != null)
                                {
                                    position.x *= -1f;
                                    this.SetBoneDirty(this._twinBoneTarget.gameObject);
                                    td = this._dirtyBones[this._twinBoneTarget.gameObject];
                                    td.position = position;
                                    if (!td.originalPosition.hasValue)
                                        td.originalPosition = this._twinBoneTarget.localPosition;
                                }
                            }
                            this._lastShouldSaveValue = shouldSaveValue;
                        }
                        break;
                    case CoordType.Rotation:
                        Quaternion rotation = Quaternion.identity;
                        if (this._boneTarget != null)
                        {
                            if (fkBoneInfo != null && fkBoneInfo.active)
                                rotation = fkBoneInfo.guideObject.transformTarget.localRotation;
                            else if (this.IsBoneDirty(this._boneTarget.gameObject) && this._dirtyBones[this._boneTarget.gameObject].rotation.hasValue)
                                rotation = this._dirtyBones[this._boneTarget.gameObject].rotation;
                            else
                                rotation = this._boneTarget.localRotation;
                        }
                        shouldSaveValue = false;
                        c = GUI.color;
                        GUI.color = this._redColor;
                        GUILayout.BeginHorizontal();
                        GUILayout.Label("X (Pitch):\t" + rotation.eulerAngles.x.ToString("0.00"));
                        GUILayout.BeginHorizontal(GUILayout.MaxWidth(160f));
                        if (GUILayout.RepeatButton((-this._inc).ToString("+0.#####;-0.#####")) && this._boneTarget != null)
                        {
                            shouldSaveValue = true;
                            if (this.RepeatControl())
                                rotation *= Quaternion.AngleAxis(-this._inc, Vector3.right);
                        }
                        if (GUILayout.RepeatButton(this._inc.ToString("+0.#####;-0.#####")) && this._boneTarget != null)
                        {
                            shouldSaveValue = true;
                            if (this.RepeatControl())
                                rotation *= Quaternion.AngleAxis(this._inc, Vector3.right);
                        }
                        GUILayout.EndHorizontal();
                        GUILayout.EndHorizontal();
                        GUI.color = c;

                        GUI.color = this._greenColor;
                        GUILayout.BeginHorizontal();
                        GUILayout.Label("Y (Yaw):\t" + rotation.eulerAngles.y.ToString("0.00"));
                        GUILayout.BeginHorizontal(GUILayout.MaxWidth(160f));
                        if (GUILayout.RepeatButton((-this._inc).ToString("+0.#####;-0.#####")) && this._boneTarget != null)
                        {
                            shouldSaveValue = true;
                            if (this.RepeatControl())
                                rotation *= Quaternion.AngleAxis(-this._inc, Vector3.up);
                        }
                        if (GUILayout.RepeatButton(this._inc.ToString("+0.#####;-0.#####")) && this._boneTarget != null)
                        {
                            shouldSaveValue = true;
                            if (this.RepeatControl())
                                rotation *= Quaternion.AngleAxis(this._inc, Vector3.up);
                        }
                        GUILayout.EndHorizontal();
                        GUILayout.EndHorizontal();
                        GUI.color = c;

                        GUI.color = this._blueColor;
                        GUILayout.BeginHorizontal();
                        GUILayout.Label("Z (Roll):\t" + rotation.eulerAngles.z.ToString("0.00"));
                        GUILayout.BeginHorizontal(GUILayout.MaxWidth(160f));
                        if (GUILayout.RepeatButton((-this._inc).ToString("+0.#####;-0.#####")) && this._boneTarget != null)
                        {
                            shouldSaveValue = true;
                            if (this.RepeatControl())
                                rotation *= Quaternion.AngleAxis(-this._inc, Vector3.forward);
                        }
                        if (GUILayout.RepeatButton(this._inc.ToString("+0.#####;-0.#####")) && this._boneTarget != null)
                        {
                            shouldSaveValue = true;
                            if (this.RepeatControl())
                                rotation *= Quaternion.AngleAxis(this._inc, Vector3.forward);
                        }
                        GUILayout.EndHorizontal();
                        GUILayout.EndHorizontal();
                        GUI.color = c;
                        if (Event.current.rawType == EventType.Repaint)
                        {
                            if (this._boneTarget != null)
                            {
                                if (shouldSaveValue)
                                {
                                    Quaternion symmetricalRotation = new Quaternion(-rotation.x, rotation.y, rotation.z, -rotation.w);
                                    if (fkBoneInfo != null && fkBoneInfo.active)
                                    {
                                        if (this._lastShouldSaveValue == false)
                                            this._oldFKRotationValue = fkBoneInfo.guideObject.changeAmount.rot;
                                        fkBoneInfo.guideObject.changeAmount.rot = rotation.eulerAngles;

                                        if (this._symmetricalEdition && fkTwinBoneInfo != null && fkTwinBoneInfo.active)
                                        {
                                            if (this._lastShouldSaveValue == false)
                                                this._oldFKTwinRotationValue = fkTwinBoneInfo.guideObject.changeAmount.rot;
                                            fkTwinBoneInfo.guideObject.changeAmount.rot = symmetricalRotation.eulerAngles;
                                        }
                                    }

                                    this.SetBoneDirty(this._boneTarget.gameObject);
                                    TransformData td = this._dirtyBones[this._boneTarget.gameObject];
                                    td.rotation = rotation;
                                    if (!td.originalRotation.hasValue)
                                        td.originalRotation = this._boneTarget.localRotation;

                                    if (this._symmetricalEdition && this._twinBoneTarget != null)
                                    {
                                        this.SetBoneDirty(this._twinBoneTarget.gameObject);
                                        td = this._dirtyBones[this._twinBoneTarget.gameObject];
                                        td.rotation = symmetricalRotation;
                                        if (!td.originalRotation.hasValue)
                                            td.originalRotation = this._twinBoneTarget.localRotation;
                                    }
                                }
                                else if (fkBoneInfo != null && fkBoneInfo.active && this._lastShouldSaveValue)
                                {
                                    GuideCommand.EqualsInfo[] infos;
                                    if (this._symmetricalEdition && fkTwinBoneInfo != null && fkTwinBoneInfo.active)
                                        infos = new GuideCommand.EqualsInfo[2];
                                    else
                                        infos = new GuideCommand.EqualsInfo[1];
                                    infos[0] = new GuideCommand.EqualsInfo()
                                    {
                                        dicKey = fkBoneInfo.guideObject.dicKey,
                                        oldValue = this._oldFKRotationValue,
                                        newValue = fkBoneInfo.guideObject.changeAmount.rot
                                    };
                                    if (this._symmetricalEdition && fkTwinBoneInfo != null && fkTwinBoneInfo.active)
                                        infos[1] = new GuideCommand.EqualsInfo()
                                        {
                                            dicKey = fkTwinBoneInfo.guideObject.dicKey,
                                            oldValue = this._oldFKTwinRotationValue,
                                            newValue = fkTwinBoneInfo.guideObject.changeAmount.rot
                                        };
                                    UndoRedoManager.Instance.Push(new GuideCommand.RotationEqualsCommand(infos));
                                }
                            }
                            this._lastShouldSaveValue = shouldSaveValue;
                        }
                        break;
                    case CoordType.Scale:
                        Vector3 scale = Vector3.one;
                        if (this._boneTarget != null)
                        {
                            if (this.IsBoneDirty(this._boneTarget.gameObject) && this._dirtyBones[this._boneTarget.gameObject].scale.hasValue)
                                scale = this._dirtyBones[this._boneTarget.gameObject].scale;
                            else
                                scale = this._boneTarget.localScale;
                        }
                        shouldSaveValue = false;
                        c = GUI.color;
                        GUI.color = this._redColor;
                        GUILayout.BeginHorizontal();
                        GUILayout.Label("X:\t" + scale.x.ToString("0.00000"));
                        GUILayout.BeginHorizontal(GUILayout.MaxWidth(160f));
                        if (GUILayout.RepeatButton((-this._inc).ToString("+0.#####;-0.#####")) && this.RepeatControl() && this._boneTarget != null)
                        {
                            shouldSaveValue = true;
                            scale -= this._inc * Vector3.right;
                        }
                        if (GUILayout.RepeatButton(this._inc.ToString("+0.#####;-0.#####")) && this.RepeatControl() && this._boneTarget != null)
                        {
                            shouldSaveValue = true;
                            scale += this._inc * Vector3.right;
                        }
                        GUILayout.EndHorizontal();
                        GUILayout.EndHorizontal();
                        GUI.color = c;

                        GUI.color = this._greenColor;
                        GUILayout.BeginHorizontal();
                        GUILayout.Label("Y:\t" + scale.y.ToString("0.00000"));
                        GUILayout.BeginHorizontal(GUILayout.MaxWidth(160f));
                        if (GUILayout.RepeatButton((-this._inc).ToString("+0.#####;-0.#####")) && this.RepeatControl() && this._boneTarget != null)
                        {
                            shouldSaveValue = true;
                            scale -= this._inc * Vector3.up;
                        }
                        if (GUILayout.RepeatButton(this._inc.ToString("+0.#####;-0.#####")) && this.RepeatControl() && this._boneTarget != null)
                        {
                            shouldSaveValue = true;
                            scale += this._inc * Vector3.up;
                        }
                        GUILayout.EndHorizontal();
                        GUILayout.EndHorizontal();
                        GUI.color = c;

                        GUI.color = this._blueColor;
                        GUILayout.BeginHorizontal();
                        GUILayout.Label("Z:\t" + scale.z.ToString("0.00000"));
                        GUILayout.BeginHorizontal(GUILayout.MaxWidth(160f));
                        if (GUILayout.RepeatButton((-this._inc).ToString("+0.#####;-0.#####")) && this.RepeatControl() && this._boneTarget != null)
                        {
                            shouldSaveValue = true;
                            scale -= this._inc * Vector3.forward;
                        }
                        if (GUILayout.RepeatButton(this._inc.ToString("+0.#####;-0.#####")) && this.RepeatControl() && this._boneTarget != null)
                        {
                            shouldSaveValue = true;
                            scale += this._inc * Vector3.forward;
                        }
                        GUILayout.EndHorizontal();
                        GUILayout.EndHorizontal();
                        GUI.color = c;

                        GUILayout.BeginHorizontal();
                        GUILayout.Label("X/Y/Z");
                        GUILayout.BeginHorizontal(GUILayout.MaxWidth(160f));
                        if (GUILayout.RepeatButton((-this._inc).ToString("+0.#####;-0.#####")) && this.RepeatControl() && this._boneTarget != null)
                        {
                            shouldSaveValue = true;
                            scale -= this._inc * Vector3.one;
                        }
                        if (GUILayout.RepeatButton(this._inc.ToString("+0.#####;-0.#####")) && this.RepeatControl() && this._boneTarget != null)
                        {
                            shouldSaveValue = true;
                            scale += this._inc * Vector3.one;
                        }
                        GUILayout.EndHorizontal();
                        GUILayout.EndHorizontal();
                        GUI.enabled = true;
                        if (Event.current.rawType == EventType.Repaint)
                        {
                            if (this._boneTarget != null && shouldSaveValue)
                            {
                                this.SetBoneDirty(this._boneTarget.gameObject);
                                TransformData td = this._dirtyBones[this._boneTarget.gameObject];
                                td.scale = scale;
                                if (!td.originalScale.hasValue)
                                    td.originalScale = this._boneTarget.localScale;

                                if (this._symmetricalEdition && this._twinBoneTarget != null)
                                {
                                    this.SetBoneDirty(this._twinBoneTarget.gameObject);
                                    td = this._dirtyBones[this._twinBoneTarget.gameObject];
                                    td.scale = scale;
                                    if (!td.originalScale.hasValue)
                                        td.originalScale = this._twinBoneTarget.localScale;
                                }
                            }
                            this._lastShouldSaveValue = shouldSaveValue;
                        }
                        break;
                }
                GUILayout.EndVertical();

                this.IncEditor();

                GUILayout.EndHorizontal();
                this._symmetricalEdition = GUILayout.Toggle(this._symmetricalEdition, "Symmetrical");

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Reset Pos.") && this._boneTarget != null && this.IsBoneDirty(this._boneTarget.gameObject))
                {
                    this._dirtyBones[this._boneTarget.gameObject].position.Reset();
                    this.SetBoneNotDirtyIf(this._boneTarget.gameObject);

                    if (this._symmetricalEdition && this._twinBoneTarget != null)
                    {
                        this._dirtyBones[this._twinBoneTarget.gameObject].position.Reset();
                        this.SetBoneNotDirtyIf(this._twinBoneTarget.gameObject);
                    }
                }
                if ((fkBoneInfo == null || fkBoneInfo.active == false) && GUILayout.Button("Reset Rot.") && this._boneTarget != null && this.IsBoneDirty(this._boneTarget.gameObject))
                {
                    this._dirtyBones[this._boneTarget.gameObject].rotation.Reset();
                    this.SetBoneNotDirtyIf(this._boneTarget.gameObject);

                    if (this._symmetricalEdition && this._twinBoneTarget != null)
                    {
                        this._dirtyBones[this._twinBoneTarget.gameObject].rotation.Reset();
                        this.SetBoneNotDirtyIf(this._twinBoneTarget.gameObject);
                    }
                }
                if (GUILayout.Button("Reset Scale") && this._boneTarget != null && this.IsBoneDirty(this._boneTarget.gameObject))
                {
                    this._dirtyBones[this._boneTarget.gameObject].scale.Reset();
                    this.SetBoneNotDirtyIf(this._boneTarget.gameObject);

                    if (this._symmetricalEdition && this._twinBoneTarget != null)
                    {
                        this._dirtyBones[this._twinBoneTarget.gameObject].scale.Reset();
                        this.SetBoneNotDirtyIf(this._twinBoneTarget.gameObject);
                    }
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
                    string sName = kvp.Value;
                    string newName;
                    if (MainWindow.self.boneAliases.TryGetValue(sName, out newName))
                        sName = newName;
                    if (GUILayout.Button(sName))
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
                    GUI.color = this._redColor;
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
            this.IncEditor(150, true);
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
                string dName = db.m_Root.name;
                string newName;
                if (MainWindow.self.boneAliases.TryGetValue(dName, out newName))
                    dName = newName;
                if (GUILayout.Button(dName + (this.IsDynamicBoneDirty(db) ? "*" : "")))
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
            g = this.Vector3Editor(g, this._redColor);
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
            f = this.Vector3Editor(f, this._blueColor);
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

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Reset", GUILayout.ExpandWidth(false)) && this._dynamicBoneTarget != null)
                this.SetDynamicBoneNotDirty(this._dynamicBoneTarget);
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();

            GUILayout.BeginVertical();
            GUILayout.FlexibleSpace();
            this.IncEditor(150, true);
            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
        }

        private void DisplaySingleBoob(DynamicBone_Ver02 boob)
        {
            GUILayout.BeginVertical();
            GUILayout.Label("Gravity");
            Vector3 gravity = boob.Gravity;
            gravity = this.Vector3Editor(gravity, this._redColor);
            if (gravity != boob.Gravity)
            {
                this.SetBoobDirty(boob);
                if (this._dirtyBoobs[boob].originalGravity.hasValue == false)
                    this._dirtyBoobs[boob].originalGravity = boob.Gravity;
                this._dirtyBoobs[boob].gravity.value = gravity;
            }
            GUILayout.EndVertical();

            GUILayout.BeginVertical();
            GUILayout.Label("Force");
            Vector3 force = boob.Force;
            force = this.Vector3Editor(force, this._blueColor);
            if (force != boob.Force)
            {
                this.SetBoobDirty(boob);
                if (this._dirtyBoobs[boob].originalForce.hasValue == false)
                    this._dirtyBoobs[boob].originalForce = boob.Force;
                this._dirtyBoobs[boob].force.value = force;
            }
            GUILayout.EndVertical();

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            Color c = GUI.color;
            GUI.color = this._redColor;
            if (GUILayout.Button("Reset", GUILayout.ExpandWidth(false)))
                this.SetBoobNotDirty(boob);
            GUI.color = c;
            GUILayout.EndHorizontal();
        }

        private void DisplayObjectTree(GameObject go, int indent)
        {
            if (this._ignoredObjects.Contains(go))
                return;
            Color c = GUI.color;
            if (this._dirtyBones.ContainsKey(go))
                GUI.color = Color.magenta;
            if (this._colliderObjects.Contains(go.transform))
                GUI.color = this._colliderMat.color;
            if (this._boneTarget == go.transform)
                GUI.color = Color.cyan;
            GUILayout.BeginHorizontal();
            GUILayout.Space(indent * 20f);
            int childCount = 0;
            for (int i = 0; i < go.transform.childCount; ++i)
                if (this._ignoredObjects.Contains(go.transform.GetChild(i).gameObject) == false)
                    ++childCount;
            if (childCount != 0)
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
            string bName = go.name;
            string newName;
            if (MainWindow.self.boneAliases.TryGetValue(bName, out newName))
                bName = newName;                
            if (GUILayout.Button(bName + (this.IsBoneDirty(go) ? "*" : ""), GUILayout.ExpandWidth(false)))
            {
                this._boneTarget = go.transform;
                this._currentAlias = MainWindow.self.boneAliases.ContainsKey(this._boneTarget.name) ? MainWindow.self.boneAliases[this._boneTarget.name] : "";
                this._twinBoneTarget = this.GetTwinBone(go.transform);
                if (this._boneTarget == this._twinBoneTarget)
                    this._twinBoneTarget = null;
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
            GUI.color = this._redColor;
            GUILayout.BeginHorizontal();
            GUILayout.Label("X:\t" + value.x.ToString("0.00000"));
            GUILayout.BeginHorizontal(GUILayout.MaxWidth(160f));
            if (GUILayout.RepeatButton((-this._inc).ToString("+0.#####;-0.#####")) && this.RepeatControl())
                value -= this._inc * Vector3.right;
            if (GUILayout.RepeatButton(this._inc.ToString("+0.#####;-0.#####")) && this.RepeatControl())
                value += this._inc * Vector3.right;
            GUILayout.EndHorizontal();
            GUILayout.EndHorizontal();
            GUI.color = c;

            GUI.color = this._greenColor;
            GUILayout.BeginHorizontal();
            GUILayout.Label("Y:\t" + value.y.ToString("0.00000"));
            GUILayout.BeginHorizontal(GUILayout.MaxWidth(160f));
            if (GUILayout.RepeatButton((-this._inc).ToString("+0.#####;-0.#####")) && this.RepeatControl())
                value -= this._inc * Vector3.up;
            if (GUILayout.RepeatButton(this._inc.ToString("+0.#####;-0.#####")) && this.RepeatControl())
                value += this._inc * Vector3.up;
            GUILayout.EndHorizontal();
            GUILayout.EndHorizontal();
            GUI.color = c;

            GUI.color = this._blueColor;
            GUILayout.BeginHorizontal();
            GUILayout.Label("Z:\t" + value.z.ToString("0.00000"));
            GUILayout.BeginHorizontal(GUILayout.MaxWidth(160f));
            if (GUILayout.RepeatButton((-this._inc).ToString("+0.#####;-0.#####")) && this.RepeatControl())
                value -= this._inc * Vector3.forward;
            if (GUILayout.RepeatButton(this._inc.ToString("+0.#####;-0.#####")) && this.RepeatControl())
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
            GUILayout.Label("X:\t" + value.x.ToString("0.00000"));
            GUILayout.BeginHorizontal(GUILayout.MaxWidth(160f));
            if (GUILayout.RepeatButton((-this._inc).ToString("+0.#####;-0.#####")) && this.RepeatControl())
                value -= this._inc * Vector3.right;
            if (GUILayout.RepeatButton(this._inc.ToString("+0.#####;-0.#####")) && this.RepeatControl())
                value += this._inc * Vector3.right;
            GUILayout.EndHorizontal();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Y:\t" + value.y.ToString("0.00000"));
            GUILayout.BeginHorizontal(GUILayout.MaxWidth(160f));
            if (GUILayout.RepeatButton((-this._inc).ToString("+0.#####;-0.#####")) && this.RepeatControl())
                value -= this._inc * Vector3.up;
            if (GUILayout.RepeatButton(this._inc.ToString("+0.#####;-0.#####")) && this.RepeatControl())
                value += this._inc * Vector3.up;
            GUILayout.EndHorizontal();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Z:\t" + value.z.ToString("0.00000"));
            GUILayout.BeginHorizontal(GUILayout.MaxWidth(160f));
            if (GUILayout.RepeatButton((-this._inc).ToString("+0.#####;-0.#####")) && this.RepeatControl())
                value -= this._inc * Vector3.forward;
            if (GUILayout.RepeatButton(this._inc.ToString("+0.#####;-0.#####")) && this.RepeatControl())
                value += this._inc * Vector3.forward;
            GUILayout.EndHorizontal();
            GUILayout.EndHorizontal();
            GUI.color = c;
            GUILayout.EndHorizontal();
            return value;
        }
        #endregion

        #region Private Methods
        private void OnPostUpdate()
        {
            bool shouldClean = false;
            foreach (KeyValuePair<GameObject, TransformData> kvp in this._dirtyBones)
            {
                if (kvp.Key == null)
                {
                    shouldClean = true;
                    continue;
                }
                if (kvp.Value.scale.hasValue)
                    kvp.Key.transform.localScale = kvp.Value.scale;
                if (kvp.Value.rotation.hasValue)
                    kvp.Key.transform.localRotation = kvp.Value.rotation;
                if (kvp.Value.position.hasValue)
                    kvp.Key.transform.localPosition = kvp.Value.position;
            }
            if (shouldClean)
            {
                Dictionary<GameObject, TransformData> newDirtyBones = new Dictionary<GameObject, TransformData>();
                foreach (KeyValuePair<GameObject, TransformData> kvp in this._dirtyBones)
                    if (kvp.Key != null)
                        newDirtyBones.Add(kvp.Key, kvp.Value);
                this._dirtyBones = newDirtyBones;
            }
            foreach (KeyValuePair<DynamicBone_Ver02, BoobData> kvp in this._dirtyBoobs)
            {
                if (kvp.Value.gravity.hasValue)
                    kvp.Key.Gravity = kvp.Value.gravity;
                if (kvp.Value.force.hasValue)
                    kvp.Key.Force = kvp.Value.force;
            }
        }

        private Transform GetCommonAncestor(Transform bone1, Transform bone2)
        {
            Transform i = bone1;
            Transform j = bone2;

            while (i != j && i != null && j != null)
            {
                i = i.parent;
                j = j.parent;
            }
            return i == j ? i : null;
        }

        private Transform GetTwinBone(Transform bone)
        {
            if (bone.name.EndsWith("_L"))
                return this.transform.FindDescendant(bone.name.Substring(0, bone.name.Length - 2) + "_R");
            if (bone.name.EndsWith("_R"))
                return this.transform.FindDescendant(bone.name.Substring(0, bone.name.Length - 2) + "_L");
            if (bone.parent.name.EndsWith("_L"))
                return this.transform.FindDescendant(bone.parent.name.Substring(0, bone.parent.name.Length - 2) + "_R").GetChild(bone.GetSiblingIndex());
            if (bone.parent.name.EndsWith("_R"))
                return this.transform.FindDescendant(bone.parent.name.Substring(0, bone.parent.name.Length - 2) + "_L").GetChild(bone.GetSiblingIndex());
            return null;
        }

        private void OnParentage(TreeNodeObject parent, TreeNodeObject child)
        {
            if (parent == null)
            {
                ObjectCtrlInfo info;
                if (Studio.Studio.Instance.dicInfo.TryGetValue(child, out info) && this._ignoredObjects.Contains(info.guideObject.transformTarget.gameObject))
                    this._ignoredObjects.Remove(info.guideObject.transformTarget.gameObject);
            }
            else
            {
                ObjectCtrlInfo info;
                if (Studio.Studio.Instance.dicInfo.TryGetValue(child, out info) && info.guideObject.transformTarget.IsChildOf(this.transform))
                    this._ignoredObjects.Add(info.guideObject.transformTarget.gameObject);
            }
        }

        private void GoToObject(string path)
        {
            if (this.transform.FindChild(path) != null)
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
            if (this._ignoredObjects.Contains(go))
                return false;
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
                if (data.gravity.hasValue)
                    data.gravity.Reset();
                if (data.force.hasValue)
                    data.force.Reset();
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

        private IEnumerator ExecuteDelayed(Action action)
        {
            yield return new WaitForEndOfFrame();
            action();
        }

        private void DrawGizmos()
        {
            if (!this.drawAdvancedMode/* || !this.isAdvancedModeEnabled*/)
                return;
            GL.PushMatrix();
            GL.LoadProjectionMatrix(this._cam.camera.projectionMatrix);
            switch (this._selectedTab)
            {
                case SelectedTab.BonesPosition:
                    if (this._boneTarget != null)
                    {
                        float size = 0.0125f;
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
            Quaternion rot = Quaternion.AngleAxis(15f, Vector3.ProjectOnPlane(Studio.Studio.Instance.cameraCtrl.mainCmaera.transform.position - end, -dir));

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
        public void ScheduleLoad(XmlNode node, HSPE.VersionNumber v)
        {
            this.StartCoroutine(this.LoadDefaultVersion_Routine(node.CloneNode(true)));
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

                    if (kvp.Value.rotation.hasValue && (!this.chara.oiCharInfo.enableFK || !this._fkObjects.ContainsKey(kvp.Key)))
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

        private IEnumerator LoadDefaultVersion_Routine(XmlNode xmlNode)
        {
            yield return null;
            yield return null;
            yield return null;
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
                                data.gravity = gravity;
                            }
                            if (node.Attributes["forceX"] != null && node.Attributes["forceY"] != null && node.Attributes["forceZ"] != null)
                            {
                                Vector3 force;
                                force.x = XmlConvert.ToSingle(node.Attributes["forceX"].Value);
                                force.y = XmlConvert.ToSingle(node.Attributes["forceY"].Value);
                                force.z = XmlConvert.ToSingle(node.Attributes["forceZ"].Value);
                                data.originalForce = boob.Force;
                                data.force = force;
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
            XmlNode dynamicBonesNode = xmlNode.FindChildNode("dynamicBones");
            if (dynamicBonesNode != null)
            {
                foreach (XmlNode node in dynamicBonesNode.ChildNodes)
                {
                    string root = node.Attributes["root"].Value;
                    DynamicBone db = null;
                    foreach (DynamicBone bone in this._dynamicBones)
                    {
                        if (bone.m_Root)
                        {
                            if ((bone.m_Root.GetPathFrom(this.transform).Equals(root) || bone.m_Root.name.Equals(root)) && this._dirtyDynamicBones.ContainsKey(bone) == false)
                            {
                                db = bone;
                                break;
                            }
                        }
                        else
                        {
                            if ((bone.transform.GetPathFrom(this.transform).Equals(root) || bone.name.Equals(root)) && this._dirtyDynamicBones.ContainsKey(bone) == false)
                            {
                                db = bone;
                                break;
                            }
                        }
                    }
                    if (db == null)
                        continue;
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