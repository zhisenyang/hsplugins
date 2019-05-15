using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml;
using Harmony;
using Studio;
using ToolBox;
using UnityEngine;
using Vectrosity;

namespace HSPE.AMModules
{
    public class BoobsEditor : AdvancedModeModule
    {
        #region Constants
#if HONEYSELECT
        private const float _dragRadius = 0.05f;
#elif KOIKATSU
        private const float _dragRadius = 0.03f;
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
#if KOIKATSU
            LeftButtCheek,
            RightButtCheek,
#endif
        }

        private class DebugLines
        {
            public VectorLine leftGravity;
            public VectorLine leftForce;
            public VectorLine leftBoth;
            public VectorLine leftCircle;
            public VectorLine rightGravity;
            public VectorLine rightForce;
            public VectorLine rightBoth;
            public VectorLine rightCircle;

            public DebugLines()
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
                this.leftCircle.MakeCircle(Vector3.zero, Studio.Studio.Instance.cameraCtrl.mainCmaera.transform.forward, _dragRadius);
                this.rightCircle.MakeCircle(Vector3.zero, Studio.Studio.Instance.cameraCtrl.mainCmaera.transform.forward, _dragRadius);
            }

            public void Draw(DynamicBone_Ver02 left, DynamicBone_Ver02 right, int index)
            {
                float scale = 20f;
                Vector3 origin = left.Bones[left.Bones.Count - 1].position;
                Vector3 final = origin + (left.Gravity) * scale;

                this.leftGravity.points3[0] = origin;
                this.leftGravity.points3[1] = final;
                this.leftGravity.Draw();

                origin = final;
                final += left.Force * scale;

                this.leftForce.points3[0] = origin;
                this.leftForce.points3[1] = final;
                this.leftForce.Draw();

                origin = left.Bones[left.Bones.Count - 1].position;

                this.leftBoth.points3[0] = origin;
                this.leftBoth.points3[1] = final;
                this.leftBoth.Draw();

                origin = right.Bones[right.Bones.Count - 1].position;
                final = origin + (right.Gravity) * scale;

                this.rightGravity.points3[0] = origin;
                this.rightGravity.points3[1] = final;
                this.rightGravity.Draw();

                origin = final;
                final += right.Force * scale;

                this.rightForce.points3[0] = origin;
                this.rightForce.points3[1] = final;
                this.rightForce.Draw();

                origin = right.Bones[right.Bones.Count - 1].position;

                this.rightBoth.points3[0] = origin;
                this.rightBoth.points3[1] = final;
                this.rightBoth.Draw();
                
                this.leftCircle.MakeCircle(left.Bones[index].position, Studio.Studio.Instance.cameraCtrl.mainCmaera.transform.forward, _dragRadius);
                this.leftCircle.Draw();
                this.rightCircle.MakeCircle(right.Bones[index].position, Studio.Studio.Instance.cameraCtrl.mainCmaera.transform.forward, _dragRadius);
                this.rightCircle.Draw();
            }

            public void Destroy()
            {
                VectorLine.Destroy(ref this.leftGravity);
                VectorLine.Destroy(ref this.leftForce);
                VectorLine.Destroy(ref this.leftBoth);
                VectorLine.Destroy(ref this.leftCircle);
                VectorLine.Destroy(ref this.rightGravity);
                VectorLine.Destroy(ref this.rightForce);
                VectorLine.Destroy(ref this.rightBoth);
                VectorLine.Destroy(ref this.rightCircle);
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
#if HONEYSELECT
        [HarmonyPatch(typeof(DynamicBone_Ver02), "LateUpdate")]
        private class DynamicBone_Ver02_LateUpdate_Patches
        {
            public delegate void VoidDelegate(DynamicBone_Ver02 db, ref bool b);

            public static event VoidDelegate shouldExecuteLateUpdate;
            public static bool Prefix(DynamicBone_Ver02 __instance)
            {
                bool result = true;
                if (shouldExecuteLateUpdate != null)
                    shouldExecuteLateUpdate(__instance, ref result);
                return result;
            }
        }
#endif
        #endregion

        #region Private Variables
#if HONEYSELECT
        private static MethodInfo _initTransforms;
        private static MethodInfo _updateDynamicBones;
        private static readonly object[] _paramArray = new object[1];
#endif

        private readonly OCIChar _chara;
        private readonly DynamicBone_Ver02 _rightBoob;
        private readonly DynamicBone_Ver02 _leftBoob;
#if KOIKATSU
        private readonly DynamicBone_Ver02 _rightButtCheek;
        private readonly DynamicBone_Ver02 _leftButtCheek;
#endif
        private readonly Dictionary<DynamicBone_Ver02, BoobData> _dirtyDynamicBones = new Dictionary<DynamicBone_Ver02, BoobData>(2);
        private DynamicBoneDragType _dynamicBoneDragType;
        private Vector3 _dragDynamicBoneStartPosition;
        private Vector3 _dragDynamicBoneEndPosition;
        private Vector3 _lastDynamicBoneGravity;
        private static DebugLines _debugLines;
#if KOIKATSU
        private static DebugLines _debugLinesButt;
        private Vector2 _scroll;
#endif
#if HONEYSELECT
        private bool _alternativeUpdateMode = false;
#endif
        #endregion

        #region Public Fields        
        public override AdvancedModeModuleType type { get { return AdvancedModeModuleType.BoobsEditor; } }
#if HONEYSELECT
        public override string displayName { get { return "Boobs"; } }
#elif KOIKATSU
        public override string displayName { get { return "Boobs & Butt"; } }
#endif
        public bool isDraggingDynamicBone { get; private set; }
        public override bool isEnabled
        {
            set
            {
                base.isEnabled = value;
                UpdateDebugLinesState(this);
            }
        }
        #endregion

        #region Unity Methods
        public BoobsEditor(CharaPoseController parent, OCIChar chara) : base(parent)
        {
            this._chara = chara;
            this._parent.onUpdate += this.Update;
            if (_debugLines == null)
            {
                _debugLines = new DebugLines();
                _debugLines.SetActive(false);
#if KOIKATSU
                _debugLinesButt = new DebugLines();
                _debugLinesButt.SetActive(false);
#endif
                MainWindow._self._cameraEventsDispatcher.onPreRender += UpdateGizmosIf;
            }
#if HONEYSELECT
            DynamicBone_Ver02_LateUpdate_Patches.shouldExecuteLateUpdate += this.ShouldExecuteDynamicBoneLateUpdate;
            this._leftBoob = ((CharFemaleBody)this._chara.charBody).getDynamicBone(CharFemaleBody.DynamicBoneKind.BreastL);
            this._rightBoob = ((CharFemaleBody)this._chara.charBody).getDynamicBone(CharFemaleBody.DynamicBoneKind.BreastR);

            if (_initTransforms == null)
                _initTransforms = this._leftBoob.GetType().GetMethod("InitTransforms", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
            if (_updateDynamicBones == null)
                _updateDynamicBones = this._leftBoob.GetType().GetMethod("UpdateDynamicBones", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
#elif KOIKATSU
            this._leftBoob = this._chara.charInfo.getDynamicBoneBust(ChaInfo.DynamicBoneKind.BreastL);
            this._rightBoob = this._chara.charInfo.getDynamicBoneBust(ChaInfo.DynamicBoneKind.BreastR);
            this._leftButtCheek = this._chara.charInfo.getDynamicBoneBust(ChaInfo.DynamicBoneKind.HipL); // "Hip" ( ͡° ͜ʖ ͡°)
            this._rightButtCheek = this._chara.charInfo.getDynamicBoneBust(ChaInfo.DynamicBoneKind.HipR);
#endif
            foreach (DynamicBone_Ver02 bone in this._parent.GetComponentsInChildren<DynamicBone_Ver02>(true))
                foreach (DynamicBoneCollider collider in CollidersEditor._loneColliders)
                {
                    if (bone.Colliders.Contains(collider) == false)
                        bone.Colliders.Add(collider);
                }
            this._incIndex = -3;
        }

        private void Update()
        {
            if (this._isEnabled && PoseController._drawAdvancedMode && MainWindow._self._poseTarget == this._parent)
                this.DynamicBoneDraggingLogic();
            if (this._dirtyDynamicBones.Count != 0)
                foreach (KeyValuePair<DynamicBone_Ver02, BoobData> kvp in this._dirtyDynamicBones)
                {
                    if (kvp.Value.gravity.hasValue)
                        kvp.Key.Gravity = kvp.Value.gravity;
                    if (kvp.Value.force.hasValue)
                        kvp.Key.Force = kvp.Value.force;
                }
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            this._parent.onUpdate -= this.Update;
#if HONEYSELECT
            DynamicBone_Ver02_LateUpdate_Patches.shouldExecuteLateUpdate -= this.ShouldExecuteDynamicBoneLateUpdate;
#endif
        }
        #endregion

        #region Public Methods
#if HONEYSELECT
        public override void IKSolverOnPostUpdate()
        {
            if (this._alternativeUpdateMode)
            {
                if (this._leftBoob.GetWeight() > 0f)
                {
                    _initTransforms.Invoke(this._leftBoob, new object[] { });
                    _paramArray[0] = Time.deltaTime;
                    _updateDynamicBones.Invoke(this._leftBoob, _paramArray);
                }
                if (this._rightBoob.GetWeight() > 0f)
                {
                    _initTransforms.Invoke(this._rightBoob, new object[] { });
                    _paramArray[0] = Time.deltaTime;
                    _updateDynamicBones.Invoke(this._rightBoob, _paramArray);
                }
            }
        }

        public override void FKCtrlOnPreLateUpdate()
        {
            if (this._alternativeUpdateMode && this._chara.oiCharInfo.enableIK == false)
            {
                if (this._leftBoob.GetWeight() > 0f)
                {
                    _initTransforms.Invoke(this._leftBoob, new object[] { });
                    _paramArray[0] = Time.deltaTime;
                    _updateDynamicBones.Invoke(this._leftBoob, _paramArray);
                }
                if (this._rightBoob.GetWeight() > 0f)
                {
                    _initTransforms.Invoke(this._rightBoob, new object[] { });
                    _paramArray[0] = Time.deltaTime;
                    _updateDynamicBones.Invoke(this._rightBoob, _paramArray);
                }
            }
        }
#endif

        public override void DrawAdvancedModeChanged()
        {
            UpdateDebugLinesState(this);
        }

        public static void SelectionChanged(BoobsEditor self)
        {
            UpdateDebugLinesState(self);
        }

        public override void GUILogic()
        {
            GUILayout.BeginVertical();

#if KOIKATSU
            GUILayout.BeginHorizontal();
            this._scroll = GUILayout.BeginScrollView(this._scroll);
#endif

            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("Right boob" + (this.IsDirty(this._rightBoob) ? "*" : ""));
            this.DisplaySingle(this._rightBoob);
            GUILayout.EndVertical();

#if HONEYSELECT
            GUILayout.BeginVertical();
            GUILayout.FlexibleSpace();
            this.IncEditor(150, true);
            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();
#endif

            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("Left boob" + (this.IsDirty(this._leftBoob) ? "*" : ""));
            this.DisplaySingle(this._leftBoob);
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();

#if KOIKATSU
            //// BUTT
            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("Right butt cheek" + (this.IsDirty(this._rightButtCheek) ? "*" : ""));
            this.DisplaySingle(this._rightButtCheek);
            GUILayout.EndVertical();

            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("Left butt cheek" + (this.IsDirty(this._leftButtCheek) ? "*" : ""));
            this.DisplaySingle(this._leftButtCheek);
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();

            GUILayout.EndScrollView();

            GUILayout.BeginVertical();
            GUILayout.FlexibleSpace();
            this.IncEditor(150, true);
            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
#endif

#if HONEYSELECT
            this._alternativeUpdateMode = GUILayout.Toggle(this._alternativeUpdateMode, "Alternative update mode");
#endif
            GUILayout.EndVertical();
        }

        public void LoadFrom(BoobsEditor other)
        {
            MainWindow._self.ExecuteDelayed(() =>
            {
#if HONEYSELECT
                this._alternativeUpdateMode = other._alternativeUpdateMode;
                CharFemale charFemale = this._chara.charInfo as CharFemale;
                CharFemale otherFemale = other._chara.charInfo as CharFemale;
#elif KOIKATSU
                ChaControl charFemale = this._chara.charInfo;
                ChaControl otherFemale = other._chara.charInfo;
#endif
                foreach (KeyValuePair<DynamicBone_Ver02, BoobData> kvp in other._dirtyDynamicBones)
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
                        this._dirtyDynamicBones.Add(db, new BoobData(kvp.Value));
                    }
                }
            }, 2);
        }

        public override int SaveXml(XmlTextWriter xmlWriter)
        {
            int written = 0;
            xmlWriter.WriteStartElement("boobs");
#if HONEYSELECT
            xmlWriter.WriteAttributeString("alternativeUpdateMode", XmlConvert.ToString(this._alternativeUpdateMode));
#endif
            written++;
            if (this._dirtyDynamicBones.Count != 0)
            {
                foreach (KeyValuePair<DynamicBone_Ver02, BoobData> kvp in this._dirtyDynamicBones)
                {
                    string name = "";
                    if (kvp.Key == this._leftBoob)
                        name = "left";
                    else if (kvp.Key == this._rightBoob)
                        name = "right";
#if KOIKATSU
                    else if (kvp.Key == this._leftButtCheek)
                        name = "leftButt";
                    else if (kvp.Key == this._rightButtCheek)
                        name = "rightButt";
#endif
                    xmlWriter.WriteStartElement(name);

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
            }
            xmlWriter.WriteEndElement();
            return written;
        }

        public override bool LoadXml(XmlNode xmlNode)
        {
            this.SetNotDirty(this._leftBoob);
            this.SetNotDirty(this._rightBoob);

            bool changed = false;
            XmlNode boobs = xmlNode.FindChildNode("boobs");
            if (boobs != null)
            {
#if HONEYSELECT
                if (boobs.Attributes != null && boobs.Attributes["alternativeUpdateMode"] != null)
                {
                    changed = true;
                    this._alternativeUpdateMode = XmlConvert.ToBoolean(boobs.Attributes["alternativeUpdateMode"].Value);
                }
#endif
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
#if KOIKATSU
                        case "leftButt":
                            boob = this._leftButtCheek;
                            break;
                        case "rightButt":
                            boob = this._rightButtCheek;
                            break;
#endif
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
                        {
                            changed = true;
                            this._dirtyDynamicBones.Add(boob, data);
                        }
                    }
                }
            }
            return changed;
        }
        #endregion

        #region Private Methods
#if HONEYSELECT
        private void ShouldExecuteDynamicBoneLateUpdate(DynamicBone_Ver02 bone, ref bool b)
        {
            if (this._parent.enabled && (bone == this._leftBoob || bone == this._rightBoob) && this._alternativeUpdateMode)
                b = this._chara.oiCharInfo.enableIK == false && this._chara.oiCharInfo.enableFK == false;
        }
#endif

        private static void UpdateGizmosIf()
        {
            if (PoseController._drawAdvancedMode && MainWindow._self._poseTarget != null && MainWindow._self._poseTarget._target.type == GenericOCITarget.Type.Character)

            {
                CharaPoseController charaPoseController = (CharaPoseController)MainWindow._self._poseTarget;
                if (charaPoseController._boobsEditor.isEnabled)
                    charaPoseController._boobsEditor.UpdateGizmos();
            }
        }

        private void UpdateGizmos()
        {
            _debugLines.Draw(this._leftBoob, this._rightBoob, 2);
#if KOIKATSU
            _debugLinesButt.Draw(this._leftButtCheek, this._rightButtCheek, 1);
#endif
        }

        private void DisplaySingle(DynamicBone_Ver02 elem)
        {
            GUILayout.BeginVertical();
            GUILayout.Label("Gravity");
            Vector3 gravity = elem.Gravity;
            gravity = this.Vector3Editor(gravity, AdvancedModeModule._redColor);
            if (gravity != elem.Gravity)
            {
                this.SetDirty(elem);
                if (this._dirtyDynamicBones[elem].originalGravity.hasValue == false)
                    this._dirtyDynamicBones[elem].originalGravity = elem.Gravity;
                this._dirtyDynamicBones[elem].gravity.value = gravity;
            }
            GUILayout.EndVertical();

            GUILayout.BeginVertical();
            GUILayout.Label("Force");
            Vector3 force = elem.Force;
            force = this.Vector3Editor(force, AdvancedModeModule._blueColor);
            if (force != elem.Force)
            {
                this.SetDirty(elem);
                if (this._dirtyDynamicBones[elem].originalForce.hasValue == false)
                    this._dirtyDynamicBones[elem].originalForce = elem.Force;
                this._dirtyDynamicBones[elem].force.value = force;
            }
            GUILayout.EndVertical();

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            Color c = GUI.color;
            GUI.color = AdvancedModeModule._redColor;
            if (GUILayout.Button("Reset", GUILayout.ExpandWidth(false)))
                this.SetNotDirty(elem);
            GUI.color = c;
            GUILayout.EndHorizontal();
        }

        private bool IsDirty(DynamicBone_Ver02 boob)
        {
            return (this._dirtyDynamicBones.ContainsKey(boob));
        }

        private void SetDirty(DynamicBone_Ver02 boob)
        {
            if (!this.IsDirty(boob))
                this._dirtyDynamicBones.Add(boob, new BoobData());
        }

        private void SetNotDirty(DynamicBone_Ver02 boob)
        {
            if (this.IsDirty(boob))
            {
                BoobData data = this._dirtyDynamicBones[boob];
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
                this._dirtyDynamicBones.Remove(boob);
            }
        }

        private void DynamicBoneDraggingLogic()
        {
            if (Input.GetMouseButtonDown(0))
            {
                float distanceFromCamera = float.PositiveInfinity;

                Vector3 leftBoobRaycastPos = Studio.Studio.Instance.cameraCtrl.mainCmaera.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, Vector3.Project(this._leftBoob.Bones[2].position - Studio.Studio.Instance.cameraCtrl.mainCmaera.transform.position, Studio.Studio.Instance.cameraCtrl.mainCmaera.transform.forward).magnitude));
                if ((leftBoobRaycastPos - this._leftBoob.Bones[2].position).sqrMagnitude < (_dragRadius * _dragRadius))
                {
                    this.isDraggingDynamicBone = true;
                    distanceFromCamera = (leftBoobRaycastPos - Studio.Studio.Instance.cameraCtrl.mainCmaera.transform.position).sqrMagnitude;
                    this._dynamicBoneDragType = DynamicBoneDragType.LeftBoob;
                    this._dragDynamicBoneStartPosition = leftBoobRaycastPos;
                    this._lastDynamicBoneGravity = this._leftBoob.Gravity;
                }

                Vector3 rightBoobRaycastPos = Studio.Studio.Instance.cameraCtrl.mainCmaera.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, Vector3.Project(this._rightBoob.Bones[2].position - Studio.Studio.Instance.cameraCtrl.mainCmaera.transform.position, Studio.Studio.Instance.cameraCtrl.mainCmaera.transform.forward).magnitude));
                if ((rightBoobRaycastPos - this._rightBoob.Bones[2].position).sqrMagnitude < (_dragRadius * _dragRadius) &&
                    (rightBoobRaycastPos - Studio.Studio.Instance.cameraCtrl.mainCmaera.transform.position).sqrMagnitude < distanceFromCamera)
                {
                    this.isDraggingDynamicBone = true;
                    distanceFromCamera = (leftBoobRaycastPos - Studio.Studio.Instance.cameraCtrl.mainCmaera.transform.position).sqrMagnitude;
                    this._dynamicBoneDragType = DynamicBoneDragType.RightBoob;
                    this._dragDynamicBoneStartPosition = rightBoobRaycastPos;
                    this._lastDynamicBoneGravity = this._rightBoob.Gravity;
                }

#if KOIKATSU
                Vector3 leftButtCheekRaycastPos = Studio.Studio.Instance.cameraCtrl.mainCmaera.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, Vector3.Project(this._leftButtCheek.Bones[1].position - Studio.Studio.Instance.cameraCtrl.mainCmaera.transform.position, Studio.Studio.Instance.cameraCtrl.mainCmaera.transform.forward).magnitude));
                if ((leftButtCheekRaycastPos - this._leftButtCheek.Bones[1].position).sqrMagnitude < (_dragRadius * _dragRadius))
                {
                    this.isDraggingDynamicBone = true;
                    distanceFromCamera = (leftButtCheekRaycastPos - Studio.Studio.Instance.cameraCtrl.mainCmaera.transform.position).sqrMagnitude;
                    this._dynamicBoneDragType = DynamicBoneDragType.LeftButtCheek;
                    this._dragDynamicBoneStartPosition = leftButtCheekRaycastPos;
                    this._lastDynamicBoneGravity = this._leftButtCheek.Gravity;
                }

                Vector3 rightButtCheekRaycastPos = Studio.Studio.Instance.cameraCtrl.mainCmaera.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, Vector3.Project(this._rightButtCheek.Bones[1].position - Studio.Studio.Instance.cameraCtrl.mainCmaera.transform.position, Studio.Studio.Instance.cameraCtrl.mainCmaera.transform.forward).magnitude));
                if ((rightButtCheekRaycastPos - this._rightButtCheek.Bones[1].position).sqrMagnitude < (_dragRadius * _dragRadius) &&
                    (rightButtCheekRaycastPos - Studio.Studio.Instance.cameraCtrl.mainCmaera.transform.position).sqrMagnitude < distanceFromCamera)
                {
                    this.isDraggingDynamicBone = true;
                    distanceFromCamera = (leftButtCheekRaycastPos - Studio.Studio.Instance.cameraCtrl.mainCmaera.transform.position).sqrMagnitude;
                    this._dynamicBoneDragType = DynamicBoneDragType.RightButtCheek;
                    this._dragDynamicBoneStartPosition = rightButtCheekRaycastPos;
                    this._lastDynamicBoneGravity = this._rightButtCheek.Gravity;
                }
#endif
                MainWindow._self.SetNoControlCondition();
            }
            else if (Input.GetMouseButton(0) && this.isDraggingDynamicBone)
            {
                this._dragDynamicBoneEndPosition = Studio.Studio.Instance.cameraCtrl.mainCmaera.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, Vector3.Project(this._dragDynamicBoneStartPosition - Studio.Studio.Instance.cameraCtrl.mainCmaera.transform.position, Studio.Studio.Instance.cameraCtrl.mainCmaera.transform.forward).magnitude));
                DynamicBone_Ver02 db = null;
                switch (this._dynamicBoneDragType)
                {
                    case DynamicBoneDragType.LeftBoob:
                        db = this._leftBoob;
                        break;
                    case DynamicBoneDragType.RightBoob:
                        db = this._rightBoob;
                        break;
#if KOIKATSU
                    case DynamicBoneDragType.LeftButtCheek:
                        db = this._leftButtCheek;
                        break;
                    case DynamicBoneDragType.RightButtCheek:
                        db = this._rightButtCheek;
                        break;
#endif
                }
                this.SetDirty(db);
                if (this._dirtyDynamicBones[db].originalGravity.hasValue == false)
                    this._dirtyDynamicBones[db].originalGravity = db.Gravity;
                db.Gravity = this._lastDynamicBoneGravity + (this._dragDynamicBoneEndPosition - this._dragDynamicBoneStartPosition) * (this._inc * 1000f) / 12f;

            }
            else if (Input.GetMouseButtonUp(0))
            {
                this.isDraggingDynamicBone = false;
            }
        }

        private static void UpdateDebugLinesState(BoobsEditor self)
        {
            if (_debugLines != null)
            {
                bool flag = self != null && self._isEnabled && PoseController._drawAdvancedMode && self._parent != null;
                _debugLines.SetActive(flag);
#if KOIKATSU
                _debugLinesButt.SetActive(flag);
#endif
            }
        }
        #endregion
    }
}
