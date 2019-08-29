using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using ToolBox;
using UnityEngine;
using Vectrosity;

namespace HSPE.AMModules
{
    public class CollidersEditor : AdvancedModeModule
    {
        #region Constants
        internal static readonly Color _colliderColor = Color.Lerp(AdvancedModeModule._greenColor, Color.white, 0.5f);
        #endregion

        #region Private Types
        private class ColliderDebugLines
        {
            public readonly List<VectorLine> centerCircles;
            public readonly List<VectorLine> capsCircles;
            public readonly List<VectorLine> centerLines;
            public readonly List<VectorLine> capsLines;

            public ColliderDebugLines()
            {
                const float radius = 1f;
                const float num = 2f * 0.5f - 1f;
                Vector3 position1 = Vector3.zero;
                Vector3 position2 = Vector3.zero;
                position1.x -= num;
                position2.x += num;
                Quaternion orientation = Quaternion.AngleAxis(90f, Vector3.up);
                Vector3 dir = Vector3.right;
                this.centerCircles = new List<VectorLine>();
                for (int i = 1; i < 10; ++i)
                {
                    VectorLine circle = VectorLine.SetLine(CollidersEditor._colliderColor, new Vector3[37]);
                    circle.MakeCircle(Vector3.Lerp(position1, position2, i / 10f), dir, radius);
                    this.centerCircles.Add(circle);
                }
                this.centerLines = new List<VectorLine>();
                for (int i = 0; i < 8; ++i)
                {
                    float angle = 360 * (i / 8f) * Mathf.Deg2Rad;
                    Vector3 offset = orientation * (new Vector3(Mathf.Cos(angle), Mathf.Sin(angle))) * radius;
                    this.centerLines.Add(VectorLine.SetLine(CollidersEditor._colliderColor, position1 + offset, position2 + offset));
                }
                Vector3[] prev = new Vector3[8];
                Vector3 prevCenter1 = Vector3.zero;
                Vector3 prevCenter2 = Vector3.zero;
                for (int i = 0; i < 8; ++i)
                {
                    float angle = 360 * (i / 8f) * Mathf.Deg2Rad;
                    prev[i] = orientation * (new Vector3(Mathf.Cos(angle), Mathf.Sin(angle))) * radius;
                }
                this.capsCircles = new List<VectorLine>();
                this.capsLines = new List<VectorLine>();
                for (int i = 0; i < 6; ++i)
                {
                    float v = (i / 5f) * 0.95f;
                    float angle = Mathf.Asin(v);
                    float radius2 = radius * Mathf.Cos(angle);
                    Vector3 center1 = position1 - dir * v * radius;
                    Vector3 center2 = position2 + dir * v * radius;
                    VectorLine circle = VectorLine.SetLine(CollidersEditor._colliderColor, new Vector3[37]);
                    circle.MakeCircle(center1, dir, radius2);
                    this.capsCircles.Add(circle);

                    circle = VectorLine.SetLine(CollidersEditor._colliderColor, new Vector3[37]);
                    circle.MakeCircle(center2, dir, radius2);
                    this.capsCircles.Add(circle);

                    if (i != 0)
                        for (int j = 0; j < 8; ++j)
                        {
                            float angle2 = 360 * (j / 8f) * Mathf.Deg2Rad;
                            Vector3 offset = orientation * (new Vector3(Mathf.Cos(angle2), Mathf.Sin(angle2))) * radius2;
                            this.capsLines.Add(VectorLine.SetLine(CollidersEditor._colliderColor, prevCenter1 + prev[j], center1 + offset));
                            this.capsLines.Add(VectorLine.SetLine(CollidersEditor._colliderColor, prevCenter2 + prev[j], center2 + offset));
                            prev[j] = offset;
                        }
                    prevCenter1 = center1;
                    prevCenter2 = center2;
                }
            }

            public void Update(DynamicBoneCollider collider)
            {
                float radius = collider.m_Radius * Mathf.Abs(collider.transform.lossyScale.x);
                float num = collider.m_Height * 0.5f - collider.m_Radius;
                Vector3 position1 = collider.m_Center;
                Vector3 position2 = collider.m_Center;
                Quaternion orientation = Quaternion.identity;
                Vector3 dir = Vector3.zero;
                switch (collider.m_Direction)
                {
                    case DynamicBoneCollider.Direction.X:
                        position1.x -= num;
                        position2.x += num;
                        orientation = collider.transform.rotation * Quaternion.AngleAxis(90f, Vector3.up);
                        dir = Vector3.right;
                        break;
                    case DynamicBoneCollider.Direction.Y:
                        position1.y -= num;
                        position2.y += num;
                        orientation = collider.transform.rotation * Quaternion.AngleAxis(90f, Vector3.right);
                        dir = Vector3.up;
                        break;
                    case DynamicBoneCollider.Direction.Z:
                        position1.z -= num;
                        position2.z += num;
                        orientation = collider.transform.rotation;
                        dir = Vector3.forward;
                        break;
                }
                position1 = collider.transform.TransformPoint(position1);
                position2 = collider.transform.TransformPoint(position2);
                dir = collider.transform.TransformDirection(dir);
                for (int i = 0; i < 9; ++i)
                {
                    this.centerCircles[i].MakeCircle(Vector3.Lerp(position1, position2, (i + 1) / 10f), dir, radius);
                }
                for (int i = 0; i < 8; ++i)
                {
                    float angle = 360 * (i / 8f) * Mathf.Deg2Rad;
                    Vector3 offset = orientation * (new Vector3(Mathf.Cos(angle), Mathf.Sin(angle))) * radius;
                    VectorLine line = this.centerLines[i];
                    line.points3[0] = position1 + offset;
                    line.points3[1] = position2 + offset;
                }

                Vector3[] prev = new Vector3[8];
                Vector3 prevCenter1 = Vector3.zero;
                Vector3 prevCenter2 = Vector3.zero;
                for (int i = 0; i < 8; ++i)
                {
                    float angle = 360 * (i / 8f) * Mathf.Deg2Rad;
                    prev[i] = orientation * (new Vector3(Mathf.Cos(angle), Mathf.Sin(angle))) * radius;
                }
                int k = 0;
                int l = 0;
                for (int i = 0; i < 6; ++i)
                {
                    float v = (i / 5f) * 0.95f;
                    float angle = Mathf.Asin(v);
                    float radius2 = radius * Mathf.Cos(angle);
                    Vector3 center1 = position1 - dir * v * radius;
                    Vector3 center2 = position2 + dir * v * radius;
                    VectorLine circle = this.capsCircles[k++];
                    circle.MakeCircle(center1, dir, radius2);

                    circle = this.capsCircles[k++];
                    circle.MakeCircle(center2, dir, radius2);

                    if (i != 0)
                        for (int j = 0; j < 8; ++j)
                        {
                            float angle2 = 360 * (j / 8f) * Mathf.Deg2Rad;
                            Vector3 offset = orientation * (new Vector3(Mathf.Cos(angle2), Mathf.Sin(angle2))) * radius2;
                            VectorLine line = this.capsLines[l++];
                            line.points3[0] = prevCenter1 + prev[j];
                            line.points3[1] = center1 + offset;

                            line = this.capsLines[l++];
                            line.points3[0] = prevCenter2 + prev[j];
                            line.points3[1] = center2 + offset;

                            prev[j] = offset;
                        }
                    prevCenter1 = center1;
                    prevCenter2 = center2;
                }
            }

            public void Draw()
            {
                for (int i = 0; i < 9; ++i)
                {
                    this.centerCircles[i].Draw();
                }
                for (int i = 0; i < 8; ++i)
                {
                    VectorLine line = this.centerLines[i];
                    line.Draw();
                }

                int k = 0;
                int l = 0;
                for (int i = 0; i < 6; ++i)
                {
                    VectorLine circle = this.capsCircles[k++];
                    circle.Draw();

                    circle = this.capsCircles[k++];
                    circle.Draw();

                    if (i != 0)
                        for (int j = 0; j < 8; ++j)
                        {
                            VectorLine line = this.capsLines[l++];
                            line.Draw();

                            line = this.capsLines[l++];
                            line.Draw();
                        }
                }
            }

            public void SetActive(bool active)
            {
                foreach (VectorLine line in this.centerCircles)
                    line.active = active;
                foreach (VectorLine line in this.capsCircles)
                    line.active = active;
                foreach (VectorLine line in this.centerLines)
                    line.active = active;
                foreach (VectorLine line in this.capsLines)
                    line.active = active;
            }

            public void Destroy()
            {
                VectorLine.Destroy(this.capsLines);
                VectorLine.Destroy(this.centerLines);
                VectorLine.Destroy(this.capsCircles);
                VectorLine.Destroy(this.centerCircles);
            }
        }

        private class ColliderData
        {
            public EditableValue<Vector3> originalCenter;
            public EditableValue<float> originalRadius;
            public EditableValue<float> originalHeight;
            public EditableValue<DynamicBoneCollider.Direction> originalDirection;
            public EditableValue<DynamicBoneCollider.Bound> originalBound;
            public Dictionary<PoseController, HashSet<object>> ignoredDynamicBones = new Dictionary<PoseController, HashSet<object>>();

            public ColliderData()
            {
            }

            public ColliderData(ColliderData other)
            {
                this.originalCenter = other.originalCenter;
                this.originalRadius = other.originalRadius;
                this.originalHeight = other.originalHeight;
                this.originalDirection = other.originalDirection;
                this.originalBound = other.originalBound;
                foreach (KeyValuePair<PoseController, HashSet<object>> pair in other.ignoredDynamicBones)
                {
                    HashSet<object> dbs;
                    if (this.ignoredDynamicBones.TryGetValue(pair.Key, out dbs) == false)
                    {
                        dbs = new HashSet<object>();
                        this.ignoredDynamicBones.Add(pair.Key, dbs);
                    }
                    foreach (DynamicBone db in pair.Value)
                    {
                        if (dbs.Contains(db) == false)
                            dbs.Add(db);
                    }
                }
            }
        }
        #endregion

        #region Private Variables
        internal static readonly HashSet<DynamicBoneCollider> _loneColliders = new HashSet<DynamicBoneCollider>();

        private readonly GenericOCITarget _target;
        private Vector2 _collidersEditionScroll;
        internal readonly Dictionary<Transform, DynamicBoneCollider> _colliders = new Dictionary<Transform, DynamicBoneCollider>();
        internal readonly bool _isLoneCollider = false;
        private DynamicBoneCollider _colliderTarget;
        private readonly Dictionary<DynamicBoneCollider, ColliderData> _dirtyColliders = new Dictionary<DynamicBoneCollider, ColliderData>();
        private static ColliderDebugLines _colliderDebugLines;
        private static readonly string[] _directionNames;
        private static readonly string[] _boundNames;
        private Vector2 _ignoredDynamicBonesScroll;
        #endregion

        #region Public Accessors
        public override AdvancedModeModuleType type { get { return AdvancedModeModuleType.CollidersEditor; } }
        public override string displayName { get { return "Colliders"; } }
        public override bool isEnabled
        {
            set
            {
                base.isEnabled = value;
                UpdateDebugLinesState(this);
            }
        }
        public override bool shouldDisplay { get { return this._colliders.Count > 0; } }
        #endregion

        #region Unity Methods
        static CollidersEditor()
        {
            _directionNames = Enum.GetNames(typeof(DynamicBoneCollider.Direction));
            _boundNames = Enum.GetNames(typeof(DynamicBoneCollider.Bound));
        }

        public CollidersEditor(PoseController parent, GenericOCITarget target): base(parent)
        {
            this._target = target;

            DynamicBoneCollider collider;

            Transform colliderTransform;
            if (this._parent.name.Equals("Collider") && (colliderTransform = this._parent.transform.Find("Collider")) != null && (collider = colliderTransform.GetComponent<DynamicBoneCollider>()) != null)
            {
                this._parent.onUpdate += this.Update;
                this._isLoneCollider = true;
                _loneColliders.Add(collider);
                foreach (DynamicBone bone in Resources.FindObjectsOfTypeAll<DynamicBone>())
                {
                    if (bone.m_Colliders.Contains(collider) == false)
                        bone.m_Colliders.Add(collider);
                }
                foreach (DynamicBone_Ver02 bone in Resources.FindObjectsOfTypeAll<DynamicBone_Ver02>())
                {
                    if (bone.Colliders.Contains(collider) == false)
                        bone.Colliders.Add(collider);                        
                }
            }

            foreach (DynamicBoneCollider c in this._parent.GetComponentsInChildren<DynamicBoneCollider>(true))
                this._colliders.Add(c.transform, c);
            this._colliderTarget = this._colliders.FirstOrDefault().Value;
            if (_colliderDebugLines == null)
            {
                _colliderDebugLines = new ColliderDebugLines();
                _colliderDebugLines.SetActive(false);
                MainWindow._self._cameraEventsDispatcher.onPreRender += UpdateGizmosIf;
            }

            this._incIndex = -2;
        }

        private void Update()
        {
            foreach (KeyValuePair<DynamicBoneCollider, ColliderData> colliderPair in this._dirtyColliders)
            {
                Dictionary<PoseController, HashSet<object>> newIgnored = null;
                foreach (KeyValuePair<PoseController, HashSet<object>> pair in colliderPair.Value.ignoredDynamicBones)
                {
                    if (pair.Key == null || pair.Value.Any(e => e == null))
                    {
                        newIgnored = new Dictionary<PoseController, HashSet<object>>();
                        break;
                    }
                }
                if (newIgnored != null)
                {
                    foreach (KeyValuePair<PoseController, HashSet<object>> pair in colliderPair.Value.ignoredDynamicBones)
                    {
                        if (pair.Key != null)
                        {
                            HashSet<object> newValue;
                            if (pair.Value.Count != 0)
                            {
                                newValue = new HashSet<object>();
                                foreach (object o in pair.Value)
                                {
                                    if (o != null)
                                        newValue.Add(o);
                                }
                            }
                            else
                                newValue = pair.Value;
                            newIgnored.Add(pair.Key, newValue);
                        }
                    }
                    colliderPair.Value.ignoredDynamicBones = newIgnored;
                }
            }
        }


        public override void OnDestroy()
        {
            base.OnDestroy();
            if (this._isLoneCollider)
            {
                DynamicBoneCollider collider = this._parent.transform.GetChild(0).GetComponent<DynamicBoneCollider>();
                _loneColliders.Remove(collider);
                foreach (DynamicBone bone in Resources.FindObjectsOfTypeAll<DynamicBone>())
                {
                    if (bone.m_Colliders.Contains(collider))
                        bone.m_Colliders.Remove(collider);
                }
                foreach (DynamicBone_Ver02 bone in Resources.FindObjectsOfTypeAll<DynamicBone_Ver02>())
                {
                    if (bone.Colliders.Contains(collider))
                        bone.Colliders.Remove(collider);
                }
                this._parent.onUpdate -= this.Update;
            }
        }

        #endregion

        #region Public Methods
        public override void DrawAdvancedModeChanged()
        {
            UpdateDebugLinesState(this);
        }

        public static void SelectionChanged(CollidersEditor self)
        {
            UpdateDebugLinesState(self);
        }

        public override void GUILogic()
        {
            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical();
            this._collidersEditionScroll = GUILayout.BeginScrollView(this._collidersEditionScroll, false, true, GUI.skin.horizontalScrollbar, GUI.skin.verticalScrollbar, GUI.skin.box, GUILayout.ExpandWidth(false));
            Color c;
            foreach (KeyValuePair<Transform, DynamicBoneCollider> pair in this._colliders)
            {
                if (pair.Key == null)
                    continue;
                c = GUI.color;
                if (this.IsColliderDirty(pair.Value))
                    GUI.color = Color.magenta;
                if (pair.Value == this._colliderTarget)
                    GUI.color = Color.cyan;
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(pair.Value.name + (this.IsColliderDirty(pair.Value) ? "*" : "")))
                    this._colliderTarget = pair.Value;
                GUILayout.Space(GUI.skin.verticalScrollbar.fixedWidth);
                GUILayout.EndHorizontal();
                GUI.color = c;
            }
            GUILayout.EndScrollView();

            {
                c = GUI.color;
                GUI.color = Color.red;
                if (GUILayout.Button("Reset all"))
                {
                    foreach (KeyValuePair<DynamicBoneCollider, ColliderData> pair in new Dictionary<DynamicBoneCollider, ColliderData>(this._dirtyColliders))
                    {
                        if (pair.Key == null)
                            continue;
                        this.SetColliderNotDirty(pair.Key);
                    }
                    this._dirtyColliders.Clear();
                }
                GUI.color = c;
            }
            GUILayout.EndVertical();

            GUILayout.BeginVertical(GUI.skin.box);

            GUILayout.BeginVertical();
            GUILayout.Label("Center");
            GUILayout.BeginHorizontal();
            Vector3 center = this.Vector3Editor(this._colliderTarget.m_Center);
            if (center != this._colliderTarget.m_Center)
            {
                ColliderData data = this.SetColliderDirty(this._colliderTarget);
                if (data.originalCenter.hasValue == false)
                    data.originalCenter = this._colliderTarget.m_Center;
                this._colliderTarget.m_Center = center;
            }
            this.IncEditor();
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            float radius = this.FloatEditor(this._colliderTarget.m_Radius, 0f, 1f, "Radius\t");
            if (Mathf.Approximately(radius, this._colliderTarget.m_Radius) == false)
            {
                ColliderData data = this.SetColliderDirty(this._colliderTarget);
                if (data.originalRadius.hasValue == false)
                    data.originalRadius = this._colliderTarget.m_Radius;
                this._colliderTarget.m_Radius = radius;
            }

            float height = this.FloatEditor(this._colliderTarget.m_Height, 2 * this._colliderTarget.m_Radius, Mathf.Max(4f, 4f * this._colliderTarget.m_Radius), "Height\t");
            if (height < this._colliderTarget.m_Radius * 2)
                height = this._colliderTarget.m_Radius * 2;
            if (Mathf.Approximately(height, this._colliderTarget.m_Height) == false)
            {
                ColliderData data = this.SetColliderDirty(this._colliderTarget);
                if (data.originalHeight.hasValue == false)
                    data.originalHeight = this._colliderTarget.m_Height;
                this._colliderTarget.m_Height = height;
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label("Direction\t", GUILayout.ExpandWidth(false));
            DynamicBoneCollider.Direction direction = (DynamicBoneCollider.Direction)GUILayout.SelectionGrid((int)this._colliderTarget.m_Direction, _directionNames, 3);
            if (direction != this._colliderTarget.m_Direction)
            {
                ColliderData data = this.SetColliderDirty(this._colliderTarget);
                if (data.originalDirection.hasValue == false)
                    data.originalDirection = this._colliderTarget.m_Direction;
                this._colliderTarget.m_Direction = direction;
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Bound\t", GUILayout.ExpandWidth(false));
            DynamicBoneCollider.Bound bound = (DynamicBoneCollider.Bound)GUILayout.SelectionGrid((int)this._colliderTarget.m_Bound, _boundNames, 2);
            if (bound != this._colliderTarget.m_Bound)
            {
                ColliderData data = this.SetColliderDirty(this._colliderTarget);
                if (data.originalBound.hasValue == false)
                    data.originalBound = this._colliderTarget.m_Bound;
                this._colliderTarget.m_Bound = bound;
            }
            GUILayout.EndHorizontal();

            if (this._isLoneCollider)
            {
                GUILayout.BeginVertical(GUI.skin.box);
                GUILayout.Label("Affected Dynamic Bones");
                {
                    ColliderData cd;
                    if (this._dirtyColliders.TryGetValue(this._colliderTarget, out cd) == false)
                        cd = null;

                    this._ignoredDynamicBonesScroll = GUILayout.BeginScrollView(this._ignoredDynamicBonesScroll);
                    foreach (PoseController controller in PoseController._poseControllers)
                    {
                        int total = controller._dynamicBonesEditor._dynamicBones.Count;
                        CharaPoseController charaPoseController = null;
                        if (CharaPoseController._charaPoseControllers.Contains(controller))
                        {
                            charaPoseController = (CharaPoseController)controller;
                            if (charaPoseController._boobsEditor != null)
                                total += charaPoseController._boobsEditor._dynamicBones.Length;
                        }
                        if (total == 0)
                            continue;
                        GUILayout.BeginHorizontal();
                        GUILayout.Label(controller.target.oci.treeNodeObject.textName + " (" + controller.target.oci.guideObject.transformTarget.name + ") ", GUILayout.ExpandWidth(false));
                        if (GUILayout.Button("Center camera on", GUILayout.ExpandWidth(false)))
                            Studio.Studio.Instance.cameraCtrl.targetPos = controller.target.oci.guideObject.transformTarget.position;
                        GUILayout.EndHorizontal();

                        HashSet<object> ignored;
                        if (cd == null || cd.ignoredDynamicBones.TryGetValue(controller, out ignored) == false)
                            ignored = null;
                        int i = 1;
                        GUILayout.BeginHorizontal();
                        foreach (DynamicBone dynamicBone in controller._dynamicBonesEditor._dynamicBones)
                        {
                            if (dynamicBone.m_Root == null)
                                continue;
                            bool e = ignored == null || ignored.Contains(dynamicBone) == false;
                            bool newE = GUILayout.Toggle(e, dynamicBone.m_Root.name);
                            if (e != newE)
                                this.SetIgnoreDynamicBone(this._colliderTarget, controller, dynamicBone, !newE);
                            if (i % 3 == 0)
                            {
                                GUILayout.EndHorizontal();
                                GUILayout.BeginHorizontal();
                            }
                            ++i;
                        }

                        if (charaPoseController != null && charaPoseController._boobsEditor != null)
                        {
                            foreach (DynamicBone_Ver02 dynamicBone in charaPoseController._boobsEditor._dynamicBones)
                            {
                                bool e = ignored == null || ignored.Contains(dynamicBone) == false;
                                bool newE = GUILayout.Toggle(e, dynamicBone.Root.name);
                                if (e != newE)
                                    this.SetIgnoreDynamicBone(this._colliderTarget, controller, dynamicBone, !newE);
                                if (i % 3 == 0)
                                {
                                    GUILayout.EndHorizontal();
                                    GUILayout.BeginHorizontal();
                                }
                                ++i;
                            }
                        }
                        GUILayout.EndHorizontal();
                    }
                    GUILayout.EndScrollView();
                }
                GUILayout.EndVertical();
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Go to bone", GUILayout.ExpandWidth(false)))
            {
                this._parent.EnableModule(this._parent._bonesEditor);
                this._parent._bonesEditor.GoToObject(this._colliderTarget.gameObject);
            }
            GUILayout.FlexibleSpace();
            GUI.color = AdvancedModeModule._redColor;
            if (GUILayout.Button("Reset", GUILayout.ExpandWidth(false)))
                this.SetColliderNotDirty(this._colliderTarget);
            GUI.color = c;
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
        }

        public void LoadFrom(CollidersEditor other)
        {
            MainWindow._self.ExecuteDelayed(() =>
            {
                foreach (KeyValuePair<DynamicBoneCollider, ColliderData> kvp in other._dirtyColliders)
                {
                    Transform obj = this._parent.transform.Find(kvp.Key.transform.GetPathFrom(other._parent.transform));
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
                this._collidersEditionScroll = other._collidersEditionScroll;
            }, 2);
        }

        public override int SaveXml(XmlTextWriter xmlWriter)
        {
            int written = 0;
            if (this._dirtyColliders.Count != 0)
            {
                xmlWriter.WriteStartElement("colliders");
                foreach (KeyValuePair<DynamicBoneCollider, ColliderData> kvp in this._dirtyColliders)
                {
                    Transform t = kvp.Key.transform.parent;
                    string n = kvp.Key.transform.name;
                    while (t != this._parent.transform)
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

                    foreach (KeyValuePair<PoseController, HashSet<object>> ignoredPair in kvp.Value.ignoredDynamicBones)
                    {
                        CharaPoseController charaPoseController = ignoredPair.Key as CharaPoseController;
                        foreach (object o in ignoredPair.Value)
                        {
                            DynamicBone db = o as DynamicBone;
                            if (db != null)
                            {
                                xmlWriter.WriteStartElement("ignoredDynamicBone");

                                xmlWriter.WriteAttributeString("poseControllerId", XmlConvert.ToString(ignoredPair.Key.GetInstanceID()));
                                xmlWriter.WriteAttributeString("root", ignoredPair.Key._dynamicBonesEditor.GetRoot(db));

                                xmlWriter.WriteEndElement();
                            }
                            else if (charaPoseController != null && charaPoseController._boobsEditor != null)
                            {
                                DynamicBone_Ver02 db2 = (DynamicBone_Ver02)o;
                                xmlWriter.WriteStartElement("ignoredDynamicBone2");

                                xmlWriter.WriteAttributeString("poseControllerId", XmlConvert.ToString(ignoredPair.Key.GetInstanceID()));
                                xmlWriter.WriteAttributeString("id", charaPoseController._boobsEditor.GetID(db2));

                                xmlWriter.WriteEndElement();
                            }
                        }
                    }

                    xmlWriter.WriteEndElement();
                    ++written;
                }
                xmlWriter.WriteEndElement();
            }
            return written;
        }

        public override bool LoadXml(XmlNode xmlNode)
        {
            this.ResetAll();
            bool changed = false;
            XmlNode colliders = xmlNode.FindChildNode("colliders");
            if (colliders != null)
            {
                foreach (XmlNode node in colliders.ChildNodes)
                {
                    try
                    {
                        Transform t = this._parent.transform.Find(node.Attributes["name"].Value);
                        if (t == null)
                            continue;
                        DynamicBoneCollider collider = t.GetComponent<DynamicBoneCollider>();
                        if (collider == null)
                            continue;
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
                        if (node.HasChildNodes)
                            changed = true;

                        if (this._isLoneCollider)
                        {
                            this._parent.ExecuteDelayed(() =>
                            {
                                foreach (XmlNode childNode in node.ChildNodes)
                                {
                                    int id = XmlConvert.ToInt32(childNode.Attributes["poseControllerId"].Value);
                                    switch (childNode.Name)
                                    {
                                        case "ignoredDynamicBone":
                                            PoseController controller = PoseController._poseControllers.FirstOrDefault(e => e._oldInstanceId == id);
                                            if (controller == null)
                                                break;
                                            DynamicBone db = controller._dynamicBonesEditor.GetDynamicBone(childNode.Attributes["root"].Value);
                                            if (db == null)
                                                break;
                                            this.SetIgnoreDynamicBone(collider, controller, db, true);
                                            break;
                                        case "ignoredDynamicBone2":
                                            CharaPoseController controller2 = CharaPoseController._charaPoseControllers.FirstOrDefault(e => e._oldInstanceId == id) as CharaPoseController;
                                            if (controller2 == null || controller2._boobsEditor == null)
                                                break;
                                            DynamicBone_Ver02 db2 = controller2._boobsEditor.GetDynamicBone(childNode.Attributes["id"].Value);
                                            this.SetIgnoreDynamicBone(collider, controller2, db2, true);
                                            break;
                                    }
                                }
                            }, 5);
                        }
                        if (data.originalCenter.hasValue || data.originalRadius.hasValue || data.originalHeight.hasValue || data.originalDirection.hasValue || data.originalBound.hasValue)
                        {
                            changed = true;
                            this._dirtyColliders.Add(collider, data);
                        }
                    }
                    catch (Exception e)
                    {
                        UnityEngine.Debug.LogError("HSPE: Couldn't load collider for object " + this._parent.name + " " + node.OuterXml + "\n" + e);
                    }
                }
            }
            return changed;
        }
        #endregion

        #region Private Methods
        private static void UpdateGizmosIf()
        {
            if (PoseController._drawAdvancedMode && MainWindow._self._poseTarget != null && MainWindow._self._poseTarget._collidersEditor._isEnabled)
            {
                MainWindow._self._poseTarget._collidersEditor.UpdateGizmos();
                MainWindow._self._poseTarget._collidersEditor.DrawGizmos();
            }
        }

        private void ResetAll()
        {
            foreach (KeyValuePair<DynamicBoneCollider, ColliderData> pair in new Dictionary<DynamicBoneCollider, ColliderData>(this._dirtyColliders))
                this.SetColliderNotDirty(pair.Key);
        }

        private ColliderData SetColliderDirty(DynamicBoneCollider collider)
        {
            ColliderData data;
            if (this._dirtyColliders.TryGetValue(collider, out data) == false)
            {
                data = new ColliderData();
                this._dirtyColliders.Add(collider, data);
            }
            return data;
        }

        private bool IsColliderDirty(DynamicBoneCollider collider)
        {
            return this._dirtyColliders.ContainsKey(collider);
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
                foreach (KeyValuePair<PoseController, HashSet<object>> pair in data.ignoredDynamicBones)
                {
                    foreach (object dynamicBone in pair.Value)
                    {
                        List<DynamicBoneCollider> colliders;
                        if (dynamicBone is DynamicBone db)
                            colliders = db.m_Colliders;
                        else
                            colliders = ((DynamicBone_Ver02)dynamicBone).Colliders;
                        if (colliders.Contains(collider) == false)
                            colliders.Add(collider);
                    }
                }
                data.ignoredDynamicBones.Clear();
                this._dirtyColliders.Remove(collider);
            }
        }

        private void SetIgnoreDynamicBone(DynamicBoneCollider collider, PoseController dynamicBoneParent, object dynamicBone, bool ignore)
        {
            ColliderData colliderData = this.SetColliderDirty(collider);
            if (ignore)
            {
                HashSet<object> ignoredList;
                if (colliderData.ignoredDynamicBones.TryGetValue(dynamicBoneParent, out ignoredList) == false)
                {
                    ignoredList = new HashSet<object>();
                    colliderData.ignoredDynamicBones.Add(dynamicBoneParent, ignoredList);
                }
                if (ignoredList.Contains(dynamicBone) == false)
                    ignoredList.Add(dynamicBone);

                List<DynamicBoneCollider> colliders;
                if (dynamicBone is DynamicBone db)
                    colliders = db.m_Colliders;
                else
                    colliders = ((DynamicBone_Ver02)dynamicBone).Colliders;

                int index = colliders.IndexOf(collider);
                if (index != -1)
                    colliders.RemoveAt(index);
            }
            else
            {
                HashSet<object> ignoredList;
                if (colliderData.ignoredDynamicBones.TryGetValue(dynamicBoneParent, out ignoredList))
                {
                    if (ignoredList.Contains(dynamicBone))
                        ignoredList.Remove(dynamicBone);
                }

                List<DynamicBoneCollider> colliders;
                if (dynamicBone is DynamicBone db)
                    colliders = db.m_Colliders;
                else
                    colliders = ((DynamicBone_Ver02)dynamicBone).Colliders;

                if (colliders.Contains(collider) == false)
                    colliders.Add(collider);
            }
        }

        private void UpdateGizmos()
        {
            if (!this._isEnabled || !PoseController._drawAdvancedMode || this._colliderTarget == null || MainWindow._self._poseTarget != this._parent)
                return;
            if (this._colliderTarget != null)
                _colliderDebugLines.Update(this._colliderTarget);
        }

        private void DrawGizmos()
        {
            if (this._colliderTarget != null)
                _colliderDebugLines.Draw();
        }

        private static void UpdateDebugLinesState(CollidersEditor self)
        {
            if (_colliderDebugLines != null)
                _colliderDebugLines.SetActive(self != null && self._isEnabled && PoseController._drawAdvancedMode && self._colliderTarget != null);
        }
        #endregion
    }
}