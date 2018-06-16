using System.Collections.Generic;
using System.Xml;
using Studio;
using UnityEngine;
using Vectrosity;

namespace HSPE.AMModules
{
    public class BoobsEditor : AdvancedModeModule
    {
        #region Constants
#if HONEYSELECT
        private const float _boobsDragRadius = 0.05f;
#elif KOIKATSU
        private const float _boobsDragRadius = 0.03f;
#endif
        #endregion

        #region Private Types
        private class BoobData
        {
            public EditableValue<Vector3> gravity;
            public EditableValue<Vector3> force;
            public EditableValue<Vector3> originalGravity;
            public EditableValue<Vector3> originalForce;

            public BoobData()
            {
            }

            public BoobData(BoobData other)
            {
                this.force = other.force;
                this.gravity = other.gravity;
                this.originalGravity = other.originalGravity;
                this.originalForce = other.originalForce;
            }
        }

        private enum DynamicBoneDragType
        {
            LeftBoob,
            RightBoob,
        }

        private struct DebugLines
        {
            public VectorLine leftGravity;
            public VectorLine leftForce;
            public VectorLine leftBoth;
            public VectorLine leftCircle;
            public VectorLine rightGravity;
            public VectorLine rightForce;
            public VectorLine rightBoth;
            public VectorLine rightCircle;

            public void Init()
            {
                Vector3 origin = Vector3.zero;
                Vector3 final = Vector3.one;

                this.leftGravity = VectorLine.SetLine(_redColor, origin, final);
                this.leftGravity.endCap = "vector";
                this.leftGravity.lineWidth = 4f;

                this.leftForce = VectorLine.SetLine(_blueColor, origin, final);
                this.leftForce.endCap = "vector";
                this.leftForce.lineWidth = 4f;

                this.leftBoth = VectorLine.SetLine(_greenColor, origin, final);
                this.leftBoth.endCap = "vector";
                this.leftBoth.lineWidth = 4f;

                this.rightGravity = VectorLine.SetLine(_redColor, origin, final);
                this.rightGravity.endCap = "vector";
                this.rightGravity.lineWidth = 4f;

                this.rightForce = VectorLine.SetLine(_blueColor, origin, final);
                this.rightForce.endCap = "vector";
                this.rightForce.lineWidth = 4f;

                this.rightBoth = VectorLine.SetLine(_greenColor, origin, final);
                this.rightBoth.endCap = "vector";
                this.rightBoth.lineWidth = 4f;

                this.leftCircle = VectorLine.SetLine(_greenColor, new Vector3[37]);
                this.rightCircle = VectorLine.SetLine(_greenColor, new Vector3[37]);
                this.leftCircle.lineWidth = 4f;
                this.rightCircle.lineWidth = 4f;
                this.leftCircle.MakeCircle(Vector3.zero, Studio.Studio.Instance.cameraCtrl.mainCmaera.transform.forward, _boobsDragRadius);
                this.rightCircle.MakeCircle(Vector3.zero, Studio.Studio.Instance.cameraCtrl.mainCmaera.transform.forward, _boobsDragRadius);
            }

            public void Draw(DynamicBone_Ver02 leftBoob, DynamicBone_Ver02 rightBoob)
            {
                float scale = 20f;
                Vector3 origin = leftBoob.Bones[leftBoob.Bones.Count - 1].position;
                Vector3 final = origin + (leftBoob.Gravity) * scale;

                this.leftGravity.points3[0] = origin;
                this.leftGravity.points3[1] = final;
                this.leftGravity.Draw();

                origin = final;
                final += leftBoob.Force * scale;

                this.leftForce.points3[0] = origin;
                this.leftForce.points3[1] = final;
                this.leftForce.Draw();

                origin = leftBoob.Bones[leftBoob.Bones.Count - 1].position;

                this.leftBoth.points3[0] = origin;
                this.leftBoth.points3[1] = final;
                this.leftBoth.Draw();

                origin = rightBoob.Bones[rightBoob.Bones.Count - 1].position;
                final = origin + (rightBoob.Gravity) * scale;

                this.rightGravity.points3[0] = origin;
                this.rightGravity.points3[1] = final;
                this.rightGravity.Draw();

                origin = final;
                final += rightBoob.Force * scale;

                this.rightForce.points3[0] = origin;
                this.rightForce.points3[1] = final;
                this.rightForce.Draw();

                origin = rightBoob.Bones[rightBoob.Bones.Count - 1].position;

                this.rightBoth.points3[0] = origin;
                this.rightBoth.points3[1] = final;
                this.rightBoth.Draw();

                this.leftCircle.MakeCircle(leftBoob.Bones[2].position, Studio.Studio.Instance.cameraCtrl.mainCmaera.transform.forward, _boobsDragRadius);
                this.leftCircle.Draw();
                this.rightCircle.MakeCircle(rightBoob.Bones[2].position, Studio.Studio.Instance.cameraCtrl.mainCmaera.transform.forward, _boobsDragRadius);
                this.rightCircle.Draw();
            }

            public void SetActive(bool active)
            {
                this.leftGravity.active = active;
                this.leftForce.active = active;
                this.leftBoth.active = active;
                this.leftCircle.active = active;
                this.rightGravity.active = active;
                this.rightForce.active = active;
                this.rightBoth.active = active;
                this.rightCircle.active = active;
            }
        }
        #endregion

        #region Private Variables
        private DynamicBone_Ver02 _rightBoob;
        private DynamicBone_Ver02 _leftBoob;
        private readonly Dictionary<DynamicBone_Ver02, BoobData> _dirtyBoobs = new Dictionary<DynamicBone_Ver02, BoobData>(2);
        private DynamicBoneDragType _dynamicBoneDragType;
        private Vector3 _dragDynamicBoneStartPosition;
        private Vector3 _dragDynamicBoneEndPosition;
        private Vector3 _lastDynamicBoneGravity;
        private DebugLines _debugLines;
        #endregion

        #region Public Fields        
        public override AdvancedModeModuleType type { get { return AdvancedModeModuleType.BoobsEditor; } }
        public override string displayName { get { return "Boobs"; } }
        public OCIChar chara { get; set; }
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
        void Start()
        {
#if HONEYSELECT
            this._leftBoob = ((CharFemaleBody)this.chara.charBody).getDynamicBone(CharFemaleBody.DynamicBoneKind.BreastL);
            this._rightBoob = ((CharFemaleBody)this.chara.charBody).getDynamicBone(CharFemaleBody.DynamicBoneKind.BreastR);
#elif KOIKATSU
            this._leftBoob = this.chara.charInfo.getDynamicBoneBust(ChaInfo.DynamicBoneKind.BreastL);
            this._rightBoob = this.chara.charInfo.getDynamicBoneBust(ChaInfo.DynamicBoneKind.BreastR);
#endif

            this._debugLines.Init();
            this._debugLines.SetActive(false);
        }

        protected override void Update()
        {
            base.Update();
            this.DynamicBoneDraggingLogic();
            foreach (KeyValuePair<DynamicBone_Ver02, BoobData> kvp in this._dirtyBoobs)
            {
                if (kvp.Value.gravity.hasValue)
                    kvp.Key.Gravity = kvp.Value.gravity;
                if (kvp.Value.force.hasValue)
                    kvp.Key.Force = kvp.Value.force;
            }
            if (!this.isEnabled || !this.drawAdvancedMode)
                return;
            this._debugLines.Draw(this._leftBoob, this._rightBoob);
        }
        #endregion

        #region Public Methods
        public override void GUILogic()
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

        public void LoadFrom(BoobsEditor other)
        {
            this.ExecuteDelayed(() =>
            {
#if HONEYSELECT
                CharFemale charFemale = this.chara.charInfo as CharFemale;
                CharFemale otherFemale = other.chara.charInfo as CharFemale;
#elif KOIKATSU
                ChaControl charFemale = this.chara.charInfo;
                ChaControl otherFemale = other.chara.charInfo;
#endif
                foreach (KeyValuePair<DynamicBone_Ver02, BoobData> kvp in other._dirtyBoobs)
                {
                    DynamicBone_Ver02 db = null;
#if HONEYSELECT
                    if (otherFemale.getDynamicBone(CharFemaleBody.DynamicBoneKind.BreastL) == kvp.Key)
                        db = charFemale.getDynamicBone(CharFemaleBody.DynamicBoneKind.BreastL);
                    else if (otherFemale.getDynamicBone(CharFemaleBody.DynamicBoneKind.BreastR) == kvp.Key)
                        db = charFemale.getDynamicBone(CharFemaleBody.DynamicBoneKind.BreastR);
#elif KOIKATSU
                    if (otherFemale.getDynamicBoneBust(ChaInfo.DynamicBoneKind.BreastL) == kvp.Key)
                        db = charFemale.getDynamicBoneBust(ChaInfo.DynamicBoneKind.BreastL);
                    else if (otherFemale.getDynamicBoneBust(ChaInfo.DynamicBoneKind.BreastR) == kvp.Key)
                        db = charFemale.getDynamicBoneBust(ChaInfo.DynamicBoneKind.BreastR);
#endif

                    if (db != null)
                    {
                        if (kvp.Value.originalForce.hasValue)
                            db.Force = kvp.Key.Force;
                        if (kvp.Value.originalGravity.hasValue)
                            db.Gravity = kvp.Key.Gravity;
                        this._dirtyBoobs.Add(db, new BoobData(kvp.Value));
                    }
                }
            }, 2);
        }

        public override int SaveXml(XmlTextWriter xmlWriter)
        {
            int written = 0;
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
            return written;
        }

        public override void LoadXml(XmlNode xmlNode)
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
        #endregion

        #region Private Methods
        private void DisplaySingleBoob(DynamicBone_Ver02 boob)
        {
            GUILayout.BeginVertical();
            GUILayout.Label("Gravity");
            Vector3 gravity = boob.Gravity;
            gravity = this.Vector3Editor(gravity, AdvancedModeModule._redColor);
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
            force = this.Vector3Editor(force, AdvancedModeModule._blueColor);
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
            GUI.color = AdvancedModeModule._redColor;
            if (GUILayout.Button("Reset", GUILayout.ExpandWidth(false)))
                this.SetBoobNotDirty(boob);
            GUI.color = c;
            GUILayout.EndHorizontal();
        }

        private bool IsBoobDirty(DynamicBone_Ver02 boob)
        {
            return (this._dirtyBoobs.ContainsKey(boob));
        }

        private void SetBoobDirty(DynamicBone_Ver02 boob)
        {
            if (!this.IsBoobDirty(boob))
                this._dirtyBoobs.Add(boob, new BoobData());
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

        private void DynamicBoneDraggingLogic()
        {
            if (!this.isEnabled || !this.drawAdvancedMode)
                return;
            if (Input.GetMouseButtonDown(0))
            {
                float distanceFromCamera = float.PositiveInfinity;
                Vector3 leftBoobRaycastPos = Studio.Studio.Instance.cameraCtrl.mainCmaera.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, Vector3.Project(this._leftBoob.Bones[2].position - Studio.Studio.Instance.cameraCtrl.mainCmaera.transform.position, Studio.Studio.Instance.cameraCtrl.mainCmaera.transform.forward).magnitude));
                if ((leftBoobRaycastPos - this._leftBoob.Bones[2].position).sqrMagnitude < (_boobsDragRadius * _boobsDragRadius))
                {
                    this.isDraggingDynamicBone = true;
                    distanceFromCamera = (leftBoobRaycastPos - Studio.Studio.Instance.cameraCtrl.mainCmaera.transform.position).sqrMagnitude;
                    this._dynamicBoneDragType = DynamicBoneDragType.LeftBoob;
                    this._dragDynamicBoneStartPosition = leftBoobRaycastPos;
                    this._lastDynamicBoneGravity = this._leftBoob.Gravity;
                }

                Vector3 rightBoobRaycastPos = Studio.Studio.Instance.cameraCtrl.mainCmaera.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, Vector3.Project(this._rightBoob.Bones[2].position - Studio.Studio.Instance.cameraCtrl.mainCmaera.transform.position, Studio.Studio.Instance.cameraCtrl.mainCmaera.transform.forward).magnitude));
                if ((rightBoobRaycastPos - this._rightBoob.Bones[2].position).sqrMagnitude < (_boobsDragRadius * _boobsDragRadius) &&
                    (rightBoobRaycastPos - Studio.Studio.Instance.cameraCtrl.mainCmaera.transform.position).sqrMagnitude < distanceFromCamera)
                {
                    this.isDraggingDynamicBone = true;
                    distanceFromCamera = (leftBoobRaycastPos - Studio.Studio.Instance.cameraCtrl.mainCmaera.transform.position).sqrMagnitude;
                    this._dynamicBoneDragType = DynamicBoneDragType.RightBoob;
                    this._dragDynamicBoneStartPosition = rightBoobRaycastPos;
                    this._lastDynamicBoneGravity = this._rightBoob.Gravity;
                }
                MainWindow.self.SetNoControlCondition();
            }
            else if (Input.GetMouseButton(0) && this.isDraggingDynamicBone)
            {
                switch (this._dynamicBoneDragType)
                {
                    case DynamicBoneDragType.LeftBoob:
                        this._dragDynamicBoneEndPosition = Studio.Studio.Instance.cameraCtrl.mainCmaera.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, Vector3.Project(this._dragDynamicBoneStartPosition - Studio.Studio.Instance.cameraCtrl.mainCmaera.transform.position, Studio.Studio.Instance.cameraCtrl.mainCmaera.transform.forward).magnitude));
                        this.SetBoobDirty(this._leftBoob);
                        if (this._dirtyBoobs[this._leftBoob].originalGravity.hasValue == false)
                            this._dirtyBoobs[this._leftBoob].originalGravity = this._leftBoob.Gravity;
                        this._leftBoob.Gravity = this._lastDynamicBoneGravity + (this._dragDynamicBoneEndPosition - this._dragDynamicBoneStartPosition) * _inc / 12f;
                        break;
                    case DynamicBoneDragType.RightBoob:
                        this._dragDynamicBoneEndPosition = Studio.Studio.Instance.cameraCtrl.mainCmaera.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, Vector3.Project(this._dragDynamicBoneStartPosition - Studio.Studio.Instance.cameraCtrl.mainCmaera.transform.position, Studio.Studio.Instance.cameraCtrl.mainCmaera.transform.forward).magnitude));
                        this.SetBoobDirty(this._rightBoob);
                        if (this._dirtyBoobs[this._rightBoob].originalGravity.hasValue == false)
                            this._dirtyBoobs[this._rightBoob].originalGravity = this._rightBoob.Gravity;
                        this._rightBoob.Gravity = this._lastDynamicBoneGravity + (this._dragDynamicBoneEndPosition - this._dragDynamicBoneStartPosition) * _inc / 12f;
                        break;
                }
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
