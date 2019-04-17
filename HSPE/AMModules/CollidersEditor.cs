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
        public CollidersEditor(PoseController parent, GenericOCITarget target): base(parent)
        {
            this._target = target;

            DynamicBoneCollider collider;

            Transform colliderTransform;
            if (this._parent.name.Equals("Collider") && (colliderTransform = this._parent.transform.Find("Collider")) != null && (collider = colliderTransform.GetComponent<DynamicBoneCollider>()) != null)
            {
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
            }

            MainWindow._self._cameraEventsDispatcher.onPreRender += this.UpdateGizmosIf;
            this._incIndex = -2;
        }


        public override void OnDestroy()
        {
            base.OnDestroy();
            MainWindow._self._cameraEventsDispatcher.onPreRender -= this.UpdateGizmosIf;
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
            }
        }

        #endregion

        #region Public Methods
        #region Chara Only Methods

        #endregion

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
            GUILayout.Label("Center:");
            GUILayout.BeginHorizontal();
            Vector3 center = this.Vector3Editor(this._colliderTarget.m_Center);
            if (center != this._colliderTarget.m_Center)
            {
                this.SetColliderDirty(this._colliderTarget);
                if (this._dirtyColliders[this._colliderTarget].originalCenter.hasValue == false)
                    this._dirtyColliders[this._colliderTarget].originalCenter = this._colliderTarget.m_Center;
                this._colliderTarget.m_Center = center;
            }
            this.IncEditor();
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Radius\t", GUILayout.ExpandWidth(false));
            float radius = GUILayout.HorizontalSlider(this._colliderTarget.m_Radius, 0f, 1f);
            if (Mathf.Approximately(radius, this._colliderTarget.m_Radius) == false)
            {
                this.SetColliderDirty(this._colliderTarget);
                if (this._dirtyColliders[this._colliderTarget].originalRadius.hasValue == false)
                    this._dirtyColliders[this._colliderTarget].originalRadius = this._colliderTarget.m_Radius;
                this._colliderTarget.m_Radius = radius;
            }
            GUILayout.Label(this._colliderTarget.m_Radius.ToString("0.000"), GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Height\t", GUILayout.ExpandWidth(false));
            float height = GUILayout.HorizontalSlider(this._colliderTarget.m_Height, 2 * this._colliderTarget.m_Radius, 3f);
            if (height < this._colliderTarget.m_Radius * 2)
                height = this._colliderTarget.m_Radius * 2;
            if (Mathf.Approximately(height, this._colliderTarget.m_Height) == false)
            {
                this.SetColliderDirty(this._colliderTarget);
                if (this._dirtyColliders[this._colliderTarget].originalHeight.hasValue == false)
                    this._dirtyColliders[this._colliderTarget].originalHeight = this._colliderTarget.m_Height;
                this._colliderTarget.m_Height = height;
            }
            GUILayout.Label(this._colliderTarget.m_Height.ToString("0.000"), GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
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

            GUILayout.BeginHorizontal();
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
                    DynamicBoneCollider collider = this._parent.transform.Find(node.Attributes["name"].Value).GetComponent<DynamicBoneCollider>();
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
                    {
                        changed = true;
                        this._dirtyColliders.Add(collider, data);
                    }
                }
            }
            return changed;
        }
        #endregion

        #region Private Methods
        private void UpdateGizmosIf()
        {
            if (this._isEnabled && PoseController._drawAdvancedMode && this._colliderTarget != null && MainWindow._self._poseTarget == this._parent)
            {
                this.UpdateGizmos();
                this.DrawGizmos();
            }
        }

        private void ResetAll()
        {
            foreach (KeyValuePair<DynamicBoneCollider, ColliderData> pair in new Dictionary<DynamicBoneCollider, ColliderData>(this._dirtyColliders))
                this.SetColliderNotDirty(pair.Key);
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