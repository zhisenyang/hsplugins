using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Studio;
using ToolBox;
using UnityEngine;
using Vectrosity;

namespace HSPE.AMModules
{
    public class DynamicBonesEditor : AdvancedModeModule
    {
        #region Constants
        private const float _dynamicBonesDragRadius = 0.025f;
        #endregion

        #region Private Types
        private class DynamicBoneData
        {
            public EditableValue<Vector3> originalGravity;
            public Vector3 currentGravity;
            public EditableValue<Vector3> originalForce;
            public Vector3 currentForce;
            public EditableValue<DynamicBone.FreezeAxis> originalFreezeAxis;
            public DynamicBone.FreezeAxis currentFreezeAxis;
            public EditableValue<float> originalWeight;
            public float currentWeight;
            public EditableValue<float> originalDamping;
            public float currentDamping;
            public EditableValue<float> originalElasticity;
            public float currentElasticity;
            public EditableValue<float> originalStiffness;
            public float currentStiffness;
            public EditableValue<float> originalInert;
            public float currentInert;
            public EditableValue<float> originalRadius;
            public float currentRadius;

            public DynamicBoneData()
            {
            }

            public DynamicBoneData(DynamicBoneData other)
            {
                this.originalWeight = other.originalWeight;
                this.originalGravity = other.originalGravity;
                this.originalForce = other.originalForce;
                this.originalFreezeAxis = other.originalFreezeAxis;
                this.originalDamping = other.originalDamping;
                this.originalElasticity = other.originalElasticity;
                this.originalStiffness = other.originalStiffness;
                this.originalInert = other.originalInert;
                this.originalRadius = other.originalRadius;

                this.currentWeight = other.currentWeight;
                this.currentGravity = other.currentGravity;
                this.currentForce = other.currentForce;
                this.currentFreezeAxis = other.currentFreezeAxis;
                this.currentDamping = other.currentDamping;
                this.currentElasticity = other.currentElasticity;
                this.currentStiffness = other.currentStiffness;
                this.currentInert = other.currentInert;
                this.currentRadius = other.currentRadius;
            }
        }

        private class DebugLines
        {
            private List<DebugDynamicBone> _debugLines = new List<DebugDynamicBone>();

            public void Draw(List<DynamicBone> dynamicBones, DynamicBone target, Dictionary<DynamicBone, DynamicBoneData> dirtyDynamicBones)
            {
                int i = 0;
                for (; i < dynamicBones.Count; i++)
                {
                    DynamicBone db = dynamicBones[i];
                    if (db.m_Root == null)
                        continue;
                    DebugDynamicBone debug;
                    if (i < this._debugLines.Count)
                    {
                        debug = this._debugLines[i];
                        debug.SetActive(true);
                    }
                    else
                    {
                        debug = new DebugDynamicBone(db);
                        this._debugLines.Add(debug);
                    }
                    debug.Draw(db, db == target, dirtyDynamicBones.ContainsKey(db));
                }
                for (; i < this._debugLines.Count; ++i)
                {
                    DebugDynamicBone debug = this._debugLines[i];
                    if (debug.IsActive())
                        debug.SetActive(false);
                }
            }

            public void Destroy()
            {
                if (this._debugLines != null)
                    for (int i = 0; i < this._debugLines.Count; i++)
                    {
                        this._debugLines[i].Destroy();
                    }
            }

            public void SetActive(bool active, int limit = -1)
            {
                if (this._debugLines == null)
                    return;
                if (limit == -1)
                    limit = this._debugLines.Count;
                for (int i = 0; i < limit; i++)
                {
                    this._debugLines[i].SetActive(active);
                }
            }
        }

        private class DebugDynamicBone
        {
            public VectorLine gravity;
            public VectorLine force;
            public VectorLine both;
            public VectorLine circle;

            public DebugDynamicBone(DynamicBone db)
            {
                Transform end = (db.m_Root ?? db.transform).GetFirstLeaf();

                float scale = 10f;
                Vector3 origin = end.position;
                Vector3 final = origin + (db.m_Gravity) * scale;

                this.gravity = VectorLine.SetLine(_redColor, origin, final);
                this.gravity.endCap = "vector";
                this.gravity.lineWidth = 4f;

                origin = final;
                final += db.m_Force * scale;

                this.force = VectorLine.SetLine(_blueColor, origin, final);
                this.force.endCap = "vector";
                this.force.lineWidth = 4f;

                origin = end.position;

                this.both = VectorLine.SetLine(_greenColor, origin, final);
                this.both.endCap = "vector";
                this.both.lineWidth = 4f;

                this.circle = VectorLine.SetLine(_greenColor, new Vector3[37]);
                this.circle.lineWidth = 4f;
                this.circle.MakeCircle(end.position, Studio.Studio.Instance.cameraCtrl.mainCmaera.transform.forward, _dynamicBonesDragRadius);
            }

            public void Draw(DynamicBone db, bool isTarget, bool isDirty)
            {
                Transform end = (db.m_Root ?? db.transform).GetFirstLeaf();

                float scale = 10f;
                Vector3 origin = end.position;
                Vector3 final = origin + (db.m_Gravity) * scale;

                this.gravity.points3[0] = origin;
                this.gravity.points3[1] = final;

                origin = final;
                final += db.m_Force * scale;

                this.force.points3[0] = origin;
                this.force.points3[1] = final;

                origin = end.position;

                Color c;
                if (isTarget)
                    c = Color.cyan;
                else if (isDirty)
                    c = Color.magenta;
                else
                    c = _greenColor;

                this.both.points3[0] = origin;
                this.both.points3[1] = final;

                this.circle.MakeCircle(end.position, Studio.Studio.Instance.cameraCtrl.mainCmaera.transform.forward, _dynamicBonesDragRadius);

                float distance = 1 - (Mathf.Clamp((end.position - Studio.Studio.Instance.cameraCtrl.mainCmaera.transform.position).sqrMagnitude, 5, 20) - 5) / 15;
                if (distance < 0.3f)
                    distance = 0.3f;
                c.a = distance;

                this.both.color = c;
                this.circle.color = c;
                Color gravityColor = this.gravity.color;
                gravityColor.a = distance;
                this.gravity.color = gravityColor;
                Color forceColor = this.force.color;
                forceColor.a = distance;
                this.force.color = forceColor;

                this.gravity.Draw();
                this.force.Draw();
                this.both.Draw();
                this.circle.Draw();
            }

            public void Destroy()
            {
                VectorLine.Destroy(ref this.gravity);
                VectorLine.Destroy(ref this.force);
                VectorLine.Destroy(ref this.both);
                VectorLine.Destroy(ref this.circle);
            }

            public void SetActive(bool active)
            {
                this.gravity.active = active;
                this.force.active = active;
                this.both.active = active;
                this.circle.active = active;
            }

            public bool IsActive()
            {
                return this.gravity.active;
            }
        }
        #endregion

        #region Private Variables
        private Vector2 _dynamicBonesScroll;
        private DynamicBone _dynamicBoneTarget;
        private readonly List<DynamicBone> _dynamicBones = new List<DynamicBone>();
        private readonly Dictionary<DynamicBone, DynamicBoneData> _dirtyDynamicBones = new Dictionary<DynamicBone, DynamicBoneData>();
        private Vector3 _dragDynamicBoneStartPosition;
        private Vector3 _dragDynamicBoneEndPosition;
        private Vector3 _lastDynamicBoneGravity;
        private DynamicBone _draggedDynamicBone;
        private static DebugLines _debugLines;
        private bool _firstRefresh;
        private readonly GenericOCITarget _target;
        private Vector2 _dynamicBonesScroll2;
        private readonly List<XmlNode> _secondPassLoadingNodes = new List<XmlNode>();
        private readonly Dictionary<Transform, DynamicBoneData> _headlessDirtyDynamicBones = new Dictionary<Transform, DynamicBoneData>();
        private int _headlessReconstructionTimeout;
        #endregion

        #region Public Fields
        public override AdvancedModeModuleType type { get { return AdvancedModeModuleType.DynamicBonesEditor; } }
        public override string displayName { get { return "Dynamic Bones"; } }
        public bool isDraggingDynamicBone { get; private set; }
        public override bool isEnabled
        {
            set
            {
                base.isEnabled = value;
                UpdateDebugLinesState(this);
            }
        }
        public override bool shouldDisplay { get { return this._dynamicBones.Count(b => b.m_Root != null) > 0; } }
        #endregion

        #region Unity Methods
        public DynamicBonesEditor(PoseController parent, GenericOCITarget target) : base(parent)
        {
            this._target = target;
            this._parent.onUpdate += this.Update;
            this._parent.onLateUpdate += this.LateUpdate;
            if (_debugLines == null)
            {
                _debugLines = new DebugLines();
                MainWindow._self._cameraEventsDispatcher.onPreRender += UpdateGizmosIf;
            }
#if HONEYSELECT
            if (this._target.type == GenericOCITarget.Type.Character && this._target.ociChar.charInfo.Sex == 1)
            {
                this._parent.ExecuteDelayed(() =>
                {
                    DynamicBone leftButtCheek = this._parent.gameObject.AddComponent<DynamicBone>();
                    leftButtCheek.m_Root = this._parent.transform.FindDescendant("cf_J_SiriDam01_L");
                    leftButtCheek.m_Damping = 0.1f;
                    leftButtCheek.m_DampingDistrib = AnimationCurve.Linear(0, 1, 1, 1);
                    //this._leftButtCheek.m_Elasticity = 0.3f;
                    leftButtCheek.m_Elasticity = 0.06f;
                    leftButtCheek.m_ElasticityDistrib = AnimationCurve.Linear(0, 1, 1, 1);
                    //this._leftButtCheek.m_Stiffness = 0.65f;
                    leftButtCheek.m_Stiffness = 0.06f;
                    leftButtCheek.m_StiffnessDistrib = AnimationCurve.Linear(0, 1, 1, 1);
                    leftButtCheek.m_Radius = 0.0003f;
                    leftButtCheek.m_RadiusDistrib = AnimationCurve.Linear(0, 1, 1, 1);
                    leftButtCheek.m_Colliders = new List<DynamicBoneCollider>();
                    leftButtCheek.m_Exclusions = new List<Transform>();
                    leftButtCheek.m_notRolls = new List<Transform>();

                    DynamicBone rightButtCheek = this._parent.gameObject.AddComponent<DynamicBone>();
                    rightButtCheek.m_Root = this._parent.transform.FindDescendant("cf_J_SiriDam01_R");
                    rightButtCheek.m_Damping = 0.1f;
                    rightButtCheek.m_DampingDistrib = AnimationCurve.Linear(0, 1, 1, 1);
                    //this._rightButtCheek.m_Elasticity = 0.3f;
                    rightButtCheek.m_Elasticity = 0.06f;
                    rightButtCheek.m_ElasticityDistrib = AnimationCurve.Linear(0, 1, 1, 1);
                    //this._rightButtCheek.m_Stiffness = 0.65f;
                    rightButtCheek.m_Stiffness = 0.06f;
                    rightButtCheek.m_StiffnessDistrib = AnimationCurve.Linear(0, 1, 1, 1);
                    rightButtCheek.m_Radius = 0.0003f;
                    rightButtCheek.m_RadiusDistrib = AnimationCurve.Linear(0, 1, 1, 1);
                    rightButtCheek.m_Colliders = new List<DynamicBoneCollider>();
                    rightButtCheek.m_Exclusions = new List<Transform>();
                    rightButtCheek.m_notRolls = new List<Transform>();
                }, 2);
            }
#endif

            this.RefreshDynamicBoneList();
            MainWindow._self.ExecuteDelayed(this.RefreshDynamicBoneList);
            MainWindow._self.ExecuteDelayed(this.RefreshDynamicBoneList, 3);
            this._incIndex = -3;
        }

        private void Update()
        {
            if (!this._firstRefresh)
            {
                this.RefreshDynamicBoneList();
                this._firstRefresh = true;
            }

            if (!this._isEnabled || !PoseController._drawAdvancedMode || MainWindow._self._poseTarget != this._parent)
                return;
            this.DynamicBoneDraggingLogic();
        }

        private void LateUpdate()
        {
            if (this._headlessReconstructionTimeout >= 0)
            {
                this._headlessReconstructionTimeout--;
                foreach (KeyValuePair<Transform, DynamicBoneData> pair in this._headlessDirtyDynamicBones.ToList())
                {
                    if (pair.Key == null)
                        continue;
                    foreach (DynamicBone db in this._dynamicBones)
                    {
                        if (db != null && this._dirtyDynamicBones.ContainsKey(db) == false && (db.m_Root == pair.Key || db.transform == pair.Key))
                        {
                            this._dirtyDynamicBones.Add(db, pair.Value);

                            if (pair.Value.originalWeight.hasValue)
                            {
                                pair.Value.originalWeight = db.GetWeight();
                                db.SetWeight(pair.Value.currentWeight);
                            }
                            if (pair.Value.originalGravity.hasValue)
                            {
                                pair.Value.originalGravity = db.m_Gravity;
                                db.m_Gravity = pair.Value.currentGravity;
                            }
                            if (pair.Value.originalForce.hasValue)
                            {
                                pair.Value.originalForce = db.m_Force;
                                db.m_Force = pair.Value.currentForce;
                            }
                            if (pair.Value.originalFreezeAxis.hasValue)
                            {
                                pair.Value.originalFreezeAxis = db.m_FreezeAxis;
                                db.m_FreezeAxis = pair.Value.currentFreezeAxis;
                            }
                            if (pair.Value.originalDamping.hasValue)
                            {
                                pair.Value.originalDamping = db.m_Damping;
                                db.m_Damping = pair.Value.currentDamping;
                            }
                            if (pair.Value.originalElasticity.hasValue)
                            {
                                pair.Value.originalElasticity = db.m_Elasticity;
                                db.m_Elasticity = pair.Value.currentElasticity;
                            }
                            if (pair.Value.originalStiffness.hasValue)
                            {
                                pair.Value.originalStiffness = db.m_Stiffness;
                                db.m_Stiffness = pair.Value.currentStiffness;
                            }
                            if (pair.Value.originalInert.hasValue)
                            {
                                pair.Value.originalInert = db.m_Inert;
                                db.m_Inert = pair.Value.currentInert;
                            }
                            if (pair.Value.originalRadius.hasValue)
                            {
                                pair.Value.originalRadius = db.m_Radius;
                                db.m_Radius = pair.Value.currentRadius;
                            }
                            this._headlessDirtyDynamicBones.Remove(pair.Key);
                            this.NotifyDynamicBoneForUpdate(db);
                            break;
                        }
                    }
                }
            }
            else if (this._headlessDirtyDynamicBones.Count != 0)
                this._headlessDirtyDynamicBones.Clear();
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            this._parent.onUpdate -= this.Update;
            this._parent.onLateUpdate -= this.LateUpdate;
        }

        #endregion

        #region Public Methods
        public override void OnCharacterReplaced()
        {
            this.RefreshDynamicBoneList();
            MainWindow._self.ExecuteDelayed(this.RefreshDynamicBoneList);
            MainWindow._self.ExecuteDelayed(this.RefreshDynamicBoneList, 2);
        }

        public override void OnLoadClothesFile()
        {
            this.RefreshDynamicBoneList();
            MainWindow._self.ExecuteDelayed(this.RefreshDynamicBoneList);
            MainWindow._self.ExecuteDelayed(this.RefreshDynamicBoneList, 2);
        }

#if HONEYSELECT
        public override void OnCoordinateReplaced(CharDefine.CoordinateType coordinateType, bool force)
#elif KOIKATSU
        public override void OnCoordinateReplaced(ChaFileDefine.CoordinateType coordinateType, bool force)
#endif
        {
            this.RefreshDynamicBoneList();
            MainWindow._self.ExecuteDelayed(this.RefreshDynamicBoneList);
            MainWindow._self.ExecuteDelayed(this.RefreshDynamicBoneList, 2);
        }

        //public override void OnParentage(TreeNodeObject parent, TreeNodeObject child)
        //{
        //    MainWindow._self.ExecuteDelayed(this.RefreshDynamicBoneList, 10);
        //}

        public override void DrawAdvancedModeChanged()
        {
            UpdateDebugLinesState(this);
        }

        public static void SelectionChanged(DynamicBonesEditor self)
        {
            UpdateDebugLinesState(self);
        }

        public override void GUILogic()
        {
            Color c;
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical();
            this._dynamicBonesScroll = GUILayout.BeginScrollView(this._dynamicBonesScroll, false, true, GUI.skin.horizontalScrollbar, GUI.skin.verticalScrollbar, GUI.skin.box, GUILayout.ExpandWidth(false));
            foreach (DynamicBone db in this._dynamicBones)
            {
                if (db.m_Root == null)
                    continue;
                c = GUI.color;
                if (this.IsDynamicBoneDirty(db))
                    GUI.color = Color.magenta;
                if (ReferenceEquals(db, this._dynamicBoneTarget))
                    GUI.color = Color.cyan;
                string dName = db.m_Root.name;
                string newName;
                if (BonesEditor._boneAliases.TryGetValue(dName, out newName))
                    dName = newName;
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(dName + (this.IsDynamicBoneDirty(db) ? "*" : "")))
                    this._dynamicBoneTarget = db;
                GUILayout.Space(GUI.skin.verticalScrollbar.fixedWidth);
                GUILayout.EndHorizontal();
                GUI.color = c;
            }
            GUILayout.EndScrollView();

            if (GUILayout.Button("Copy to FK"))
                this.PhysicToFK();
            if (GUILayout.Button("Force refresh list"))
                this.RefreshDynamicBoneList();

            {
                c = GUI.color;
                GUI.color = Color.red;
                if (GUILayout.Button("Reset all") && this._dynamicBoneTarget != null)
                {
                    foreach (KeyValuePair<DynamicBone, DynamicBoneData> pair in new Dictionary<DynamicBone, DynamicBoneData>(this._dirtyDynamicBones))
                    {
                        if (pair.Key == null)
                            continue;
                        this.SetDynamicBoneNotDirty(pair.Key);
                    }
                    this.RefreshDynamicBoneList();
                    this._dirtyDynamicBones.Clear();
                }
                GUI.color = c;
            }
            GUILayout.EndVertical();

            GUILayout.BeginVertical(GUI.skin.box, GUILayout.ExpandWidth(true));

            this._dynamicBonesScroll2 = GUILayout.BeginScrollView(this._dynamicBonesScroll2, false, true);

            {
                GUILayout.BeginVertical();
                GUILayout.Label("Gravity");
                Vector3 g = Vector3.zero;
                if (this._dynamicBoneTarget != null)
                    g = this._dynamicBoneTarget.m_Gravity;
                g = this.Vector3Editor(g, _redColor);
                if (this._dynamicBoneTarget != null)
                {
                    if (this._dynamicBoneTarget.m_Gravity != g)
                    {
                        this.SetDynamicBoneDirty(this._dynamicBoneTarget);
                        DynamicBoneData data = this._dirtyDynamicBones[this._dynamicBoneTarget];
                        if (data.originalGravity.hasValue == false)
                            data.originalGravity = this._dynamicBoneTarget.m_Gravity;
                        data.currentGravity = g;
                        this._dynamicBoneTarget.m_Gravity = g;
                    }
                }
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                c = GUI.color;
                GUI.color = Color.red;
                if (GUILayout.Button("Reset", GUILayout.ExpandWidth(false)) && this._dynamicBoneTarget != null && this.IsDynamicBoneDirty(this._dynamicBoneTarget))
                {
                    DynamicBoneData data = this._dirtyDynamicBones[this._dynamicBoneTarget];
                    this._dynamicBoneTarget.m_Gravity = data.originalGravity;
                    data.currentGravity = data.originalGravity;
                    data.originalGravity.Reset();
                }
                GUI.color = c;
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
            }

            {
                GUILayout.BeginVertical();
                GUILayout.Label("Force");
                Vector3 f = Vector3.zero;
                if (this._dynamicBoneTarget != null)
                    f = this._dynamicBoneTarget.m_Force;
                f = this.Vector3Editor(f, _blueColor);
                if (this._dynamicBoneTarget != null)
                {
                    if (this._dynamicBoneTarget.m_Force != f)
                    {
                        this.SetDynamicBoneDirty(this._dynamicBoneTarget);
                        DynamicBoneData data = this._dirtyDynamicBones[this._dynamicBoneTarget];
                        if (data.originalForce.hasValue == false)
                            data.originalForce = this._dynamicBoneTarget.m_Force;
                        data.currentForce = f;
                        this._dynamicBoneTarget.m_Force = f;
                    }
                }

                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                c = GUI.color;
                GUI.color = Color.red;
                if (GUILayout.Button("Reset", GUILayout.ExpandWidth(false)) && this._dynamicBoneTarget != null && this.IsDynamicBoneDirty(this._dynamicBoneTarget))
                {
                    DynamicBoneData data = this._dirtyDynamicBones[this._dynamicBoneTarget];
                    this._dynamicBoneTarget.m_Force = data.originalForce;
                    data.currentForce = data.originalForce;
                    data.originalForce.Reset();
                }
                GUI.color = c;
                GUILayout.EndHorizontal();

                GUILayout.EndVertical();
            }

            {
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
                        DynamicBoneData data = this._dirtyDynamicBones[this._dynamicBoneTarget];
                        if (data.originalFreezeAxis.hasValue == false)
                            data.originalFreezeAxis = this._dynamicBoneTarget.m_FreezeAxis;
                        data.currentFreezeAxis = fa;
                        this._dynamicBoneTarget.m_FreezeAxis = fa;
                    }
                }

                c = GUI.color;
                GUI.color = Color.red;
                if (GUILayout.Button("Reset", GUILayout.ExpandWidth(false)) && this._dynamicBoneTarget != null && this.IsDynamicBoneDirty(this._dynamicBoneTarget))
                {
                    DynamicBoneData data = this._dirtyDynamicBones[this._dynamicBoneTarget];
                    this._dynamicBoneTarget.m_FreezeAxis = data.originalFreezeAxis;
                    data.currentFreezeAxis = data.originalFreezeAxis;
                    data.originalFreezeAxis.Reset();
                }
                GUI.color = c;

                GUILayout.EndHorizontal();
            }

            {
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
                        DynamicBoneData data = this._dirtyDynamicBones[this._dynamicBoneTarget];
                        if (data.originalWeight.hasValue == false)
                            data.originalWeight = this._dynamicBoneTarget.GetWeight();
                        data.currentWeight = w;
                        this._dynamicBoneTarget.SetWeight(w);
                    }
                }
                GUILayout.Label(w.ToString("0.000"), GUILayout.ExpandWidth(false));

                c = GUI.color;
                GUI.color = Color.red;
                if (GUILayout.Button("Reset", GUILayout.ExpandWidth(false)) && this._dynamicBoneTarget != null && this.IsDynamicBoneDirty(this._dynamicBoneTarget))
                {
                    DynamicBoneData data = this._dirtyDynamicBones[this._dynamicBoneTarget];
                    this._dynamicBoneTarget.SetWeight(data.originalWeight);
                    data.currentWeight = data.originalWeight;
                    data.originalWeight.Reset();
                }
                GUI.color = c;
                GUILayout.EndHorizontal();
            }

            {
                GUILayout.BeginHorizontal();
                float v = 0f;
                if (this._dynamicBoneTarget != null)
                    v = this._dynamicBoneTarget.m_Damping;
                GUILayout.Label("Damping\t", GUILayout.ExpandWidth(false));
                v = GUILayout.HorizontalSlider(v, 0f, 1f);
                if (this._dynamicBoneTarget != null)
                {
                    if (!Mathf.Approximately(this._dynamicBoneTarget.m_Damping, v))
                    {
                        this.SetDynamicBoneDirty(this._dynamicBoneTarget);
                        DynamicBoneData data = this._dirtyDynamicBones[this._dynamicBoneTarget];
                        if (data.originalDamping.hasValue == false)
                            data.originalDamping = this._dynamicBoneTarget.m_Damping;
                        data.currentDamping = v;
                        this._dynamicBoneTarget.m_Damping = v;
                        this.NotifyDynamicBoneForUpdate(this._dynamicBoneTarget);
                    }
                }
                GUILayout.Label(v.ToString("0.000"), GUILayout.ExpandWidth(false));

                c = GUI.color;
                GUI.color = Color.red;
                if (GUILayout.Button("Reset", GUILayout.ExpandWidth(false)) && this._dynamicBoneTarget != null && this.IsDynamicBoneDirty(this._dynamicBoneTarget))
                {
                    DynamicBoneData data = this._dirtyDynamicBones[this._dynamicBoneTarget];
                    this._dynamicBoneTarget.m_Damping = data.originalDamping;
                    data.currentDamping = data.originalDamping;
                    data.originalDamping.Reset();
                    this.NotifyDynamicBoneForUpdate(this._dynamicBoneTarget);
                }
                GUI.color = c;

                GUILayout.EndHorizontal();
            }

            {
                GUILayout.BeginHorizontal();
                float v = 0f;
                if (this._dynamicBoneTarget != null)
                    v = this._dynamicBoneTarget.m_Elasticity;
                GUILayout.Label("Elasticity\t", GUILayout.ExpandWidth(false));
                v = GUILayout.HorizontalSlider(v, 0f, 1f);
                if (this._dynamicBoneTarget != null)
                {
                    if (!Mathf.Approximately(this._dynamicBoneTarget.m_Elasticity, v))
                    {
                        this.SetDynamicBoneDirty(this._dynamicBoneTarget);
                        DynamicBoneData data = this._dirtyDynamicBones[this._dynamicBoneTarget];
                        if (data.originalElasticity.hasValue == false)
                            data.originalElasticity = this._dynamicBoneTarget.m_Elasticity;
                        data.currentElasticity = v;
                        this._dynamicBoneTarget.m_Elasticity = v;
                        this.NotifyDynamicBoneForUpdate(this._dynamicBoneTarget);
                    }
                }
                GUILayout.Label(v.ToString("0.000"), GUILayout.ExpandWidth(false));

                c = GUI.color;
                GUI.color = Color.red;
                if (GUILayout.Button("Reset", GUILayout.ExpandWidth(false)) && this._dynamicBoneTarget != null && this.IsDynamicBoneDirty(this._dynamicBoneTarget))
                {
                    DynamicBoneData data = this._dirtyDynamicBones[this._dynamicBoneTarget];
                    this._dynamicBoneTarget.m_Elasticity = data.originalElasticity;
                    data.currentElasticity = data.originalElasticity;
                    data.originalElasticity.Reset();
                    this.NotifyDynamicBoneForUpdate(this._dynamicBoneTarget);
                }
                GUI.color = c;

                GUILayout.EndHorizontal();
            }

            {
                GUILayout.BeginHorizontal();
                float v = 0f;
                if (this._dynamicBoneTarget != null)
                    v = this._dynamicBoneTarget.m_Stiffness;
                GUILayout.Label("Stiffness\t", GUILayout.ExpandWidth(false));
                v = GUILayout.HorizontalSlider(v, 0f, 1f);
                if (this._dynamicBoneTarget != null)
                {
                    if (!Mathf.Approximately(this._dynamicBoneTarget.m_Stiffness, v))
                    {
                        this.SetDynamicBoneDirty(this._dynamicBoneTarget);
                        DynamicBoneData data = this._dirtyDynamicBones[this._dynamicBoneTarget];
                        if (data.originalStiffness.hasValue == false)
                            data.originalStiffness = this._dynamicBoneTarget.m_Stiffness;
                        data.currentStiffness = v;
                        this._dynamicBoneTarget.m_Stiffness = v;
                        this.NotifyDynamicBoneForUpdate(this._dynamicBoneTarget);
                    }
                }
                GUILayout.Label(v.ToString("0.000"), GUILayout.ExpandWidth(false));

                c = GUI.color;
                GUI.color = Color.red;
                if (GUILayout.Button("Reset", GUILayout.ExpandWidth(false)) && this._dynamicBoneTarget != null && this.IsDynamicBoneDirty(this._dynamicBoneTarget))
                {
                    DynamicBoneData data = this._dirtyDynamicBones[this._dynamicBoneTarget];
                    this._dynamicBoneTarget.m_Stiffness = data.originalStiffness;
                    data.currentStiffness = data.originalStiffness;
                    data.originalStiffness.Reset();
                    this.NotifyDynamicBoneForUpdate(this._dynamicBoneTarget);
                }
                GUI.color = c;

                GUILayout.EndHorizontal();
            }

            {
                GUILayout.BeginHorizontal();
                float v = 0f;
                if (this._dynamicBoneTarget != null)
                    v = this._dynamicBoneTarget.m_Inert;
                GUILayout.Label("Inertia\t", GUILayout.ExpandWidth(false));
                v = GUILayout.HorizontalSlider(v, 0f, 1f);
                if (this._dynamicBoneTarget != null)
                {
                    if (!Mathf.Approximately(this._dynamicBoneTarget.m_Inert, v))
                    {
                        this.SetDynamicBoneDirty(this._dynamicBoneTarget);
                        DynamicBoneData data = this._dirtyDynamicBones[this._dynamicBoneTarget];
                        if (data.originalInert.hasValue == false)
                            data.originalInert = this._dynamicBoneTarget.m_Inert;
                        data.currentInert = v;
                        this._dynamicBoneTarget.m_Inert = v;
                        this.NotifyDynamicBoneForUpdate(this._dynamicBoneTarget);
                    }
                }
                GUILayout.Label(v.ToString("0.000"), GUILayout.ExpandWidth(false));

                c = GUI.color;
                GUI.color = Color.red;
                if (GUILayout.Button("Reset", GUILayout.ExpandWidth(false)) && this._dynamicBoneTarget != null && this.IsDynamicBoneDirty(this._dynamicBoneTarget))
                {
                    DynamicBoneData data = this._dirtyDynamicBones[this._dynamicBoneTarget];
                    this._dynamicBoneTarget.m_Inert = data.originalInert;
                    data.currentInert = data.originalInert;
                    data.originalInert.Reset();
                    this.NotifyDynamicBoneForUpdate(this._dynamicBoneTarget);
                }
                GUI.color = c;

                GUILayout.EndHorizontal();
            }

            {
                GUILayout.BeginHorizontal();
                float v = 0f;
                if (this._dynamicBoneTarget != null)
                    v = this._dynamicBoneTarget.m_Radius;
                GUILayout.Label("Radius\t", GUILayout.ExpandWidth(false));
                v = GUILayout.HorizontalSlider(v, 0f, 1f);
                if (this._dynamicBoneTarget != null)
                {
                    if (!Mathf.Approximately(this._dynamicBoneTarget.m_Radius, v))
                    {
                        this.SetDynamicBoneDirty(this._dynamicBoneTarget);
                        DynamicBoneData data = this._dirtyDynamicBones[this._dynamicBoneTarget];
                        if (data.originalRadius.hasValue == false)
                            data.originalRadius = this._dynamicBoneTarget.m_Radius;
                        data.currentRadius = v;
                        this._dynamicBoneTarget.m_Radius = v;
                        this.NotifyDynamicBoneForUpdate(this._dynamicBoneTarget);
                    }
                }
                GUILayout.Label(v.ToString("0.000"), GUILayout.ExpandWidth(false));

                c = GUI.color;
                GUI.color = Color.red;
                if (GUILayout.Button("Reset", GUILayout.ExpandWidth(false)) && this._dynamicBoneTarget != null && this.IsDynamicBoneDirty(this._dynamicBoneTarget))
                {
                    DynamicBoneData data = this._dirtyDynamicBones[this._dynamicBoneTarget];
                    this._dynamicBoneTarget.m_Radius = data.originalRadius;
                    data.currentRadius = data.originalRadius;
                    data.originalRadius.Reset();
                    this.NotifyDynamicBoneForUpdate(this._dynamicBoneTarget);
                }
                GUI.color = c;

                GUILayout.EndHorizontal();
            }

            GUILayout.EndScrollView();

            GUILayout.BeginHorizontal();
            c = GUI.color;
            GUI.color = Color.red;
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Reset", GUILayout.ExpandWidth(false)) && this._dynamicBoneTarget != null)
                this.SetDynamicBoneNotDirty(this._dynamicBoneTarget);
            GUI.color = c;
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();

            GUILayout.BeginVertical();
            GUILayout.FlexibleSpace();
            this.IncEditor(150, true);
            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
        }

        public void LoadFrom(DynamicBonesEditor other)
        {
            MainWindow._self.ExecuteDelayed(() =>
            {
                foreach (KeyValuePair<DynamicBone, DynamicBoneData> kvp in other._dirtyDynamicBones)
                {
                    DynamicBone db = null;
                    foreach (DynamicBone bone in this._dynamicBones)
                    {
                        if (kvp.Key.m_Root != null && bone.m_Root != null)
                        {
                            if (kvp.Key.m_Root.GetPathFrom(other._parent.transform).Equals(bone.m_Root.GetPathFrom(this._parent.transform)) && this._dirtyDynamicBones.ContainsKey(bone) == false)
                            {
                                db = bone;
                                break;
                            }
                        }
                        else
                        {
                            if (kvp.Key.name.Equals(bone.name) && this._dirtyDynamicBones.ContainsKey(bone) == false)
                            {
                                db = bone;
                                break;
                            }
                        }
                    }
                    if (db != null)
                    {
                        if (kvp.Value.originalForce.hasValue)
                            db.m_Force = kvp.Key.m_Force;
                        if (kvp.Value.originalFreezeAxis.hasValue)
                            db.m_FreezeAxis = kvp.Key.m_FreezeAxis;
                        if (kvp.Value.originalGravity.hasValue)
                            db.m_Gravity = kvp.Key.m_Gravity;
                        if (kvp.Value.originalWeight.hasValue)
                            db.SetWeight(kvp.Key.GetWeight());
                        if (kvp.Value.originalDamping.hasValue)
                            db.m_Damping = kvp.Key.m_Damping;
                        if (kvp.Value.originalElasticity.hasValue)
                            db.m_Elasticity = kvp.Key.m_Elasticity;
                        if (kvp.Value.originalStiffness.hasValue)
                            db.m_Stiffness = kvp.Key.m_Stiffness;
                        if (kvp.Value.originalInert.hasValue)
                            db.m_Inert = kvp.Key.m_Inert;
                        if (kvp.Value.originalRadius.hasValue)
                            db.m_Radius = kvp.Key.m_Radius;
                        this._dirtyDynamicBones.Add(db, new DynamicBoneData(kvp.Value));
                    }
                    this._dynamicBonesScroll = other._dynamicBonesScroll;
                }
            }, 2);
        }

        public override int SaveXml(XmlTextWriter xmlWriter)
        {
            int written = 0;
            if (this._dirtyDynamicBones.Count != 0)
            {
                xmlWriter.WriteStartElement("dynamicBones");
                foreach (KeyValuePair<DynamicBone, DynamicBoneData> kvp in this._dirtyDynamicBones)
                {
                    if (kvp.Key == null)
                        continue;
                    xmlWriter.WriteStartElement("dynamicBone");
                    xmlWriter.WriteAttributeString("root", kvp.Key.m_Root != null ? kvp.Key.m_Root.GetPathFrom(this._parent.transform) : kvp.Key.name);

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
                    if (kvp.Value.originalDamping.hasValue)
                        xmlWriter.WriteAttributeString("damping", XmlConvert.ToString(kvp.Key.m_Damping));
                    if (kvp.Value.originalElasticity.hasValue)
                        xmlWriter.WriteAttributeString("elasticity", XmlConvert.ToString(kvp.Key.m_Elasticity));
                    if (kvp.Value.originalStiffness.hasValue)
                        xmlWriter.WriteAttributeString("stiffness", XmlConvert.ToString(kvp.Key.m_Stiffness));
                    if (kvp.Value.originalInert.hasValue)
                        xmlWriter.WriteAttributeString("inert", XmlConvert.ToString(kvp.Key.m_Inert));
                    if (kvp.Value.originalRadius.hasValue)
                        xmlWriter.WriteAttributeString("radius", XmlConvert.ToString(kvp.Key.m_Radius));
                    xmlWriter.WriteEndElement();
                    ++written;
                }
                xmlWriter.WriteEndElement();
            }
            //this.RefreshDynamicBoneList();

            return written;
        }

        public override bool LoadXml(XmlNode xmlNode)
        {
            this.ResetAll();
            this.RefreshDynamicBoneList();
            bool changed = false;
            List<XmlNode> potentialChildrenNodes = new List<XmlNode>();

            XmlNode dynamicBonesNode = xmlNode.FindChildNode("dynamicBones");
            if (dynamicBonesNode != null)
            {
                foreach (XmlNode node in dynamicBonesNode.ChildNodes)
                {
                    try
                    {
                        string root = node.Attributes["root"].Value;
                        DynamicBone db = null;
                        foreach (DynamicBone bone in this._dynamicBones)
                        {
                            if (bone.m_Root)
                            {
                                if ((bone.m_Root.GetPathFrom(this._parent.transform).Equals(root) || bone.m_Root.name.Equals(root)) && this._dirtyDynamicBones.ContainsKey(bone) == false)
                                {
                                    db = bone;
                                    break;
                                }
                            }
                            else
                            {
                                if ((bone.transform.GetPathFrom(this._parent.transform).Equals(root) || bone.name.Equals(root)) && this._dirtyDynamicBones.ContainsKey(bone) == false)
                                {
                                    db = bone;
                                    break;
                                }
                            }
                        }
                        if (db == null)
                        {
                            potentialChildrenNodes.Add(node);
                            continue;
                        }
                        if (this.LoadSingleBone(db, node))
                            changed = true;
                    }
                    catch (Exception e)
                    {
                        UnityEngine.Debug.LogError("HSPE: Couldn't load dynamic bone for object " + this._parent.name + " " + node.OuterXml + "\n" + e);
                    }
                }
            }
            if (potentialChildrenNodes.Count > 0)
            {
                foreach (XmlNode node in potentialChildrenNodes)
                {
                    Transform t = this._parent.transform.Find(node.Attributes["root"].Value);
                    if (t == null)
                        continue;
                    PoseController childController = t.GetComponent<PoseController>();
                    if (childController == null)
                        childController = t.GetComponentInParent<PoseController>();
                    if (childController == null)
                        continue;
                    if (childController != this._parent)
                    {
                        childController.enabled = true;
                        childController._dynamicBonesEditor._secondPassLoadingNodes.Add(node);
                    }
                }
            }

            this._parent.ExecuteDelayed(() =>
            {
                foreach (XmlNode node in this._secondPassLoadingNodes)
                {
                    try
                    {
                        string root = node.Attributes["root"].Value;
                        DynamicBone db = null;
                        foreach (DynamicBone bone in this._dynamicBones)
                        {
                            if (bone.m_Root)
                            {
                                if ((root.EndsWith(bone.m_Root.GetPathFrom(this._parent.transform.parent)) || bone.m_Root.name.Equals(root)) && this._dirtyDynamicBones.ContainsKey(bone) == false)
                                {
                                    db = bone;
                                    break;
                                }
                            }
                            else
                            {
                                if ((root.EndsWith(bone.transform.GetPathFrom(this._parent.transform.parent)) || bone.name.Equals(root)) && this._dirtyDynamicBones.ContainsKey(bone) == false)
                                {
                                    db = bone;
                                    break;
                                }
                            }
                        }
                        if (db == null)
                            continue;
                        this.LoadSingleBone(db, node);
                    }
                    catch (Exception e)
                    {
                        UnityEngine.Debug.LogError("HSPE: Couldn't load dynamic bone for object " + this._parent.name + " " + node.OuterXml + "\n" + e);
                    }
                }
                this._secondPassLoadingNodes.Clear();
            }, 2);
            return changed || this._secondPassLoadingNodes.Count > 0;
        }
        #endregion

        #region Private Methods
        private static void UpdateGizmosIf()
        {
            if (PoseController._drawAdvancedMode && MainWindow._self._poseTarget != null && MainWindow._self._poseTarget._dynamicBonesEditor._isEnabled)
                MainWindow._self._poseTarget._dynamicBonesEditor.UpdateGizmos();
        }

        private void UpdateGizmos()
        {
            _debugLines.Draw(this._dynamicBones, this._dynamicBoneTarget, this._dirtyDynamicBones);
        }

        private bool LoadSingleBone(DynamicBone db, XmlNode node)
        {
            bool loaded = false;
            DynamicBoneData data = new DynamicBoneData();

            if (node.Attributes["weight"] != null)
            {
                float weight = XmlConvert.ToSingle(node.Attributes["weight"].Value);
                data.originalWeight = db.GetWeight();
                data.currentWeight = weight;
                db.SetWeight(weight);
            }
            if (node.Attributes["gravityX"] != null && node.Attributes["gravityY"] != null && node.Attributes["gravityZ"] != null)
            {
                Vector3 gravity;
                gravity.x = XmlConvert.ToSingle(node.Attributes["gravityX"].Value);
                gravity.y = XmlConvert.ToSingle(node.Attributes["gravityY"].Value);
                gravity.z = XmlConvert.ToSingle(node.Attributes["gravityZ"].Value);
                data.originalGravity = db.m_Gravity;
                data.currentGravity = gravity;
                db.m_Gravity = gravity;
            }
            if (node.Attributes["forceX"] != null && node.Attributes["forceY"] != null && node.Attributes["forceZ"] != null)
            {
                Vector3 force;
                force.x = XmlConvert.ToSingle(node.Attributes["forceX"].Value);
                force.y = XmlConvert.ToSingle(node.Attributes["forceY"].Value);
                force.z = XmlConvert.ToSingle(node.Attributes["forceZ"].Value);
                data.originalForce = db.m_Force;
                data.currentForce = force;
                db.m_Force = force;
            }
            if (node.Attributes["freezeAxis"] != null)
            {
                DynamicBone.FreezeAxis axis = (DynamicBone.FreezeAxis)XmlConvert.ToInt32(node.Attributes["freezeAxis"].Value);
                data.originalFreezeAxis = db.m_FreezeAxis;
                data.currentFreezeAxis = axis;
                db.m_FreezeAxis = axis;
            }
            if (node.Attributes["damping"] != null)
            {
                float damping = XmlConvert.ToSingle(node.Attributes["damping"].Value);
                data.originalDamping = db.m_Damping;
                data.currentDamping = damping;
                db.m_Damping = damping;
            }
            if (node.Attributes["elasticity"] != null)
            {
                float elasticity = XmlConvert.ToSingle(node.Attributes["elasticity"].Value);
                data.originalElasticity = db.m_Elasticity;
                data.currentElasticity = elasticity;
                db.m_Elasticity = elasticity;
            }
            if (node.Attributes["stiffness"] != null)
            {
                float stiffness = XmlConvert.ToSingle(node.Attributes["stiffness"].Value);
                data.originalStiffness = db.m_Stiffness;
                data.currentStiffness = stiffness;
                db.m_Stiffness = stiffness;
            }

            if (node.Attributes["inert"] != null)
            {
                float inert = XmlConvert.ToSingle(node.Attributes["inert"].Value);
                data.originalInert = db.m_Inert;
                data.currentInert = inert;
                db.m_Inert = inert;
            }
            if (node.Attributes["radius"] != null)
            {
                float radius = XmlConvert.ToSingle(node.Attributes["radius"].Value);
                data.originalRadius = db.m_Radius;
                data.currentRadius = radius;
                db.m_Radius = radius;
            }
            if (data.originalWeight.hasValue || data.originalGravity.hasValue || data.originalForce.hasValue || data.originalFreezeAxis.hasValue || data.originalDamping.hasValue || data.originalElasticity.hasValue || data.originalStiffness.hasValue || data.originalInert.hasValue || data.originalRadius.hasValue)
            {
                loaded = true;
                this.NotifyDynamicBoneForUpdate(db);
                this._dirtyDynamicBones.Add(db, data);                
            }
            return loaded;
        }

        private void ResetAll()
        {
            foreach (DynamicBone bone in this._dynamicBones)
                this.SetDynamicBoneNotDirty(bone);
        }

        private void PhysicToFK()
        {
            List<GuideCommand.EqualsInfo> infos = new List<GuideCommand.EqualsInfo>();
            foreach (DynamicBone bone in this._dynamicBones)
            {
                foreach (object o in (IList)bone.GetPrivate("m_Particles"))
                {
                    Transform t = (Transform)o.GetPrivate("m_Transform");
                    OCIChar.BoneInfo boneInfo;
                    if (t != null && this._target.fkObjects.TryGetValue(t.gameObject, out boneInfo))
                    {
                        Vector3 oldValue = boneInfo.guideObject.changeAmount.rot;
                        boneInfo.guideObject.changeAmount.rot = t.localEulerAngles;
                        infos.Add(new GuideCommand.EqualsInfo()
                        {
                            dicKey = boneInfo.guideObject.dicKey,
                            oldValue = oldValue,
                            newValue = boneInfo.guideObject.changeAmount.rot
                        });
                    }
                }
            }
            UndoRedoManager.Instance.Push(new GuideCommand.RotationEqualsCommand(infos.ToArray()));
        }

        private void SetDynamicBoneNotDirty(DynamicBone bone)
        {
            if (this._dynamicBones.Contains(bone) && this.IsDynamicBoneDirty(bone))
            {
                DynamicBoneData data = this._dirtyDynamicBones[bone];
                if (data == null)
                    return;
                if (data.originalWeight.hasValue)
                {
                    bone.SetWeight(data.originalWeight);
                    data.currentWeight = data.originalWeight;
                    data.originalWeight.Reset();
                }
                if (data.originalGravity.hasValue)
                {
                    bone.m_Gravity = data.originalGravity;
                    data.currentGravity = data.originalGravity;
                    data.originalGravity.Reset();
                }
                if (data.originalForce.hasValue)
                {
                    bone.m_Force = data.originalForce;
                    data.currentGravity = data.originalGravity;
                    data.originalForce.Reset();
                }
                if (data.originalFreezeAxis.hasValue)
                {
                    bone.m_FreezeAxis = data.originalFreezeAxis;
                    data.currentFreezeAxis = data.originalFreezeAxis;
                    data.originalFreezeAxis.Reset();
                }
                if (data.originalDamping.hasValue)
                {
                    bone.m_Damping = data.originalDamping;
                    data.currentDamping = data.originalDamping;
                    data.originalDamping.Reset();
                }
                if (data.originalElasticity.hasValue)
                {
                    bone.m_Elasticity = data.originalElasticity;
                    data.currentElasticity = data.originalElasticity;
                    data.originalElasticity.Reset();
                }
                if (data.originalStiffness.hasValue)
                {
                    bone.m_Stiffness = data.originalStiffness;
                    data.currentStiffness = data.originalStiffness;
                    data.originalStiffness.Reset();
                }
                if (data.originalInert.hasValue)
                {
                    bone.m_Inert = data.originalInert;
                    data.currentInert = data.originalInert;
                    data.originalInert.Reset();
                }
                if (data.originalRadius.hasValue)
                {
                    bone.m_Radius = data.originalRadius;
                    data.currentRadius = data.originalRadius;
                    data.originalRadius.Reset();
                }
                this._dirtyDynamicBones.Remove(bone);
                this.NotifyDynamicBoneForUpdate(bone);
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

        private void NotifyDynamicBoneForUpdate(DynamicBone bone)
        {
#if HONEYSELECT
            bone.InitTransforms();
            bone.SetupParticles();
#elif KOIKATSU
            bone.CallPrivate("InitTransforms");
            bone.CallPrivate("SetupParticles");
#endif
        }

        private void DynamicBoneDraggingLogic()
        {
            if (Input.GetMouseButtonDown(0))
            {
                float distanceFromCamera = float.PositiveInfinity;
                {
                    for (int i = 0; i < this._dynamicBones.Count; i++)
                    {
                        DynamicBone db = this._dynamicBones[i];
                        Transform leaf = (db.m_Root ?? db.transform).GetFirstLeaf();
                        Vector3 raycastPos = Studio.Studio.Instance.cameraCtrl.mainCmaera.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, Vector3.Project(leaf.position - Studio.Studio.Instance.cameraCtrl.mainCmaera.transform.position, Studio.Studio.Instance.cameraCtrl.mainCmaera.transform.forward).magnitude));
                        if ((raycastPos - leaf.position).sqrMagnitude < (_dynamicBonesDragRadius * _dynamicBonesDragRadius) &&
                            (raycastPos - Studio.Studio.Instance.cameraCtrl.mainCmaera.transform.position).sqrMagnitude < distanceFromCamera)
                        {
                            this.isDraggingDynamicBone = true;
                            distanceFromCamera = (raycastPos - Studio.Studio.Instance.cameraCtrl.mainCmaera.transform.position).sqrMagnitude;
                            this._dragDynamicBoneStartPosition = raycastPos;
                            this._lastDynamicBoneGravity = db.m_Force;
                            this._draggedDynamicBone = db;
                            this._dynamicBoneTarget = db;
                        }
                    }
                    MainWindow._self.SetNoControlCondition();
                }
            }
            else if (Input.GetMouseButton(0) && this.isDraggingDynamicBone)
            {
                this._dragDynamicBoneEndPosition = Studio.Studio.Instance.cameraCtrl.mainCmaera.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, Vector3.Project(this._dragDynamicBoneStartPosition - Studio.Studio.Instance.cameraCtrl.mainCmaera.transform.position, Studio.Studio.Instance.cameraCtrl.mainCmaera.transform.forward).magnitude));
                this.SetDynamicBoneDirty(this._draggedDynamicBone);
                if (this._dirtyDynamicBones[this._draggedDynamicBone].originalForce.hasValue == false)
                    this._dirtyDynamicBones[this._draggedDynamicBone].originalForce = this._draggedDynamicBone.m_Force;
                this._draggedDynamicBone.m_Force = this._lastDynamicBoneGravity + (this._dragDynamicBoneEndPosition - this._dragDynamicBoneStartPosition) * (this._inc * 1000f) / 12f;
            }
            else if (Input.GetMouseButtonUp(0))
            {
                this.isDraggingDynamicBone = false;
            }
        }

        private void RefreshDynamicBoneList()
        {
            DynamicBone[] dynamicBones = this._parent.GetComponentsInChildren<DynamicBone>(true);
            List<DynamicBone> toDelete = null;
            foreach (DynamicBone db in this._dynamicBones)
                if (dynamicBones.Contains(db) == false)
                {
                    if (toDelete == null)
                        toDelete = new List<DynamicBone>();
                    toDelete.Add(db);
                }
            foreach (KeyValuePair<DynamicBone, DynamicBoneData> pair in this._dirtyDynamicBones) //Putting every dirty one into headless, that should help
            {
                if (pair.Key != null)
                {
                    this._headlessDirtyDynamicBones.Add(pair.Key.m_Root != null ? pair.Key.m_Root : pair.Key.transform, pair.Value);
                    this._headlessReconstructionTimeout = 5;
                }
            }
            this._dirtyDynamicBones.Clear();
            if (toDelete != null)
            {
                foreach (DynamicBone db in toDelete)
                    this._dynamicBones.Remove(db);
            }
            List<DynamicBone> toAdd = null;
            foreach (DynamicBone db in dynamicBones)
                if (this._dynamicBones.Contains(db) == false && (this._parent == null || this._parent._childObjects.All(child => db.transform.IsChildOf(child.transform) == false)))
                {
                    if (toAdd == null)
                        toAdd = new List<DynamicBone>();
                    toAdd.Add(db);
                }
            if (toAdd != null)
            {
                foreach (DynamicBone db in toAdd)
                    this._dynamicBones.Add(db);
            }
            if (this._dynamicBones.Count != 0 && this._dynamicBoneTarget == null)
                this._dynamicBoneTarget = this._dynamicBones.FirstOrDefault(d => d.m_Root != null);
            foreach (DynamicBone bone in this._dynamicBones)
            {
                foreach (DynamicBoneCollider collider in CollidersEditor._loneColliders)
                {
                    if (bone.m_Colliders.Contains(collider) == false)
                        bone.m_Colliders.Add(collider);
                }
            }
        }


        private static void UpdateDebugLinesState(DynamicBonesEditor self)
        {
            if (_debugLines != null)
                _debugLines.SetActive(self != null && self._isEnabled && PoseController._drawAdvancedMode);
        }
        #endregion
    }
}
