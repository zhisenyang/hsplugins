using System.Collections.Generic;
using System.Linq;
using System.Xml;
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

        private struct DebugLines
        {
            private List<DebugDynamicBone> _debugLines;

            public void Draw(List<DynamicBone> dynamicBones, DynamicBone target)
            {
                if (this._debugLines == null)
                    this._debugLines = new List<DebugDynamicBone>();
                int i = 0;
                for (; i < dynamicBones.Count; i++)
                {
                    DynamicBone db = dynamicBones[i];
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
                    debug.Draw(db, db == target);
                }
                for (; i < this._debugLines.Count; ++i)
                {
                    DebugDynamicBone debug = this._debugLines[i];
                    if (debug.IsActive())
                        debug.SetActive(false);
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

            public void Draw(DynamicBone db, bool isTarget)
            {
                Transform end = (db.m_Root ?? db.transform).GetFirstLeaf();

                float scale = 10f;
                Vector3 origin = end.position;
                Vector3 final = origin + (db.m_Gravity) * scale;

                this.gravity.points3[0] = origin;
                this.gravity.points3[1] = final;
                this.gravity.Draw();

                origin = final;
                final += db.m_Force * scale;

                this.force.points3[0] = origin;
                this.force.points3[1] = final;
                this.force.Draw();

                origin = end.position;

                if (isTarget)
                {
                    this.both.color = Color.magenta;
                    this.circle.color = Color.magenta;
                }
                else
                {
                    this.both.color = _greenColor;
                    this.circle.color = _greenColor;
                }
                this.both.points3[0] = origin;
                this.both.points3[1] = final;
                this.both.Draw();

                this.circle.MakeCircle(end.position, Studio.Studio.Instance.cameraCtrl.mainCmaera.transform.forward, _dynamicBonesDragRadius);
                this.circle.Draw();
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
        private List<DynamicBone> _dynamicBones = new List<DynamicBone>();
        private readonly Dictionary<DynamicBone, DynamicBoneData> _dirtyDynamicBones = new Dictionary<DynamicBone, DynamicBoneData>();
        private Vector3 _dragDynamicBoneStartPosition;
        private Vector3 _dragDynamicBoneEndPosition;
        private Vector3 _lastDynamicBoneGravity;
        private DynamicBone _draggedDynamicBone;
        private DebugLines _debugLines = new DebugLines();
        #endregion

        #region Public Fields
        public override AdvancedModeModuleType type { get { return AdvancedModeModuleType.DynamicBonesEditor; } }
        public override string displayName { get { return "Dynamic Bones"; } }
        public bool isDraggingDynamicBone { get; private set; }
        public override bool drawAdvancedMode
        {
            set
            {
                base.drawAdvancedMode = value;
                this.CheckGizmosEnabled();
            }
        }

        public override bool isEnabled
        {
            set
            {
                base.isEnabled = value;
                this.CheckGizmosEnabled();
            }
        }
        #endregion

        #region Unity Methods
        void Awake()
        {
            this._dynamicBones = this.GetComponentsInChildren<DynamicBone>(true).ToList();
            this._dynamicBoneTarget = this._dynamicBones.First(d => d.m_Root != null);
        }

        protected override void Update()
        {
            base.Update();
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
                this._dynamicBoneTarget = this._dynamicBones.FirstOrDefault(d => d.m_Root != null);
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
            this.DynamicBoneDraggingLogic();
            if (!this.isEnabled || !this.drawAdvancedMode)
                return;
            this._debugLines.Draw(this._dynamicBones, this._dynamicBoneTarget);

        }
        #endregion

        #region Public Methods
        public override void GUILogic()
        {
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical();
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

            if (GUILayout.Button("Reset All") && this._dynamicBoneTarget != null)
                while (this._dirtyDynamicBones.Count != 0)
                    this.SetDynamicBoneNotDirty(this._dirtyDynamicBones.First().Key);

            GUILayout.EndVertical();

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
            g = this.Vector3Editor(g, AdvancedModeModule._redColor);
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
            f = this.Vector3Editor(f, AdvancedModeModule._blueColor);
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

        public void LoadFrom(DynamicBonesEditor other)
        {
            this.ExecuteDelayed(() =>
            {
                foreach (KeyValuePair<DynamicBone, DynamicBoneData> kvp in other._dirtyDynamicBones)
                {
                    DynamicBone db = null;
                    foreach (DynamicBone bone in this._dynamicBones)
                    {
                        if (kvp.Key.m_Root != null && bone.m_Root != null)
                        {
                            if (kvp.Key.m_Root.name.Equals(bone.m_Root.name) && this._dirtyDynamicBones.ContainsKey(bone) == false)
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

        public override void LoadXml(XmlNode xmlNode)
        {
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

        #region Private Methods
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

        private void DynamicBoneDraggingLogic()
        {
            if (!this.isEnabled || !this.drawAdvancedMode)
                return;
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
                    MainWindow.self.SetNoControlCondition();
                }
            }
            else if (Input.GetMouseButton(0) && this.isDraggingDynamicBone)
            {
                this._dragDynamicBoneEndPosition = Studio.Studio.Instance.cameraCtrl.mainCmaera.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, Vector3.Project(this._dragDynamicBoneStartPosition - Studio.Studio.Instance.cameraCtrl.mainCmaera.transform.position, Studio.Studio.Instance.cameraCtrl.mainCmaera.transform.forward).magnitude));
                this.SetDynamicBoneDirty(this._draggedDynamicBone);
                if (this._dirtyDynamicBones[this._draggedDynamicBone].originalForce.hasValue == false)
                    this._dirtyDynamicBones[this._draggedDynamicBone].originalForce = this._draggedDynamicBone.m_Force;
                this._draggedDynamicBone.m_Force = this._lastDynamicBoneGravity + (this._dragDynamicBoneEndPosition - this._dragDynamicBoneStartPosition) * _inc / 12f;
            }
            else if (Input.GetMouseButtonUp(0))
            {
                this.isDraggingDynamicBone = false;
            }
        }

        private void CheckGizmosEnabled()
        {
            this._debugLines.SetActive(this.isEnabled && this.drawAdvancedMode);
        }
        #endregion
    }
}
