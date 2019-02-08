using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using HSPE.AMModules;
using Studio;
using ToolBox;
using UnityEngine;

namespace HSPE
{
    public class PoseController : MonoBehaviour
    {
        #region Events
        public static event Action<TreeNodeObject, TreeNodeObject> onParentage;
        public event Action onUpdate;
        public event Action onLateUpdate;
        public event Action onDestroy;
        public event Action onDisable; 
        #endregion

        #region Protected Variables
        protected BonesEditor _bonesEditor;
        internal DynamicBonesEditor _dynamicBonesEditor;
        internal BlendShapesEditor _blendShapesEditor;
        internal bool _isLoneCollider = false;
        protected readonly List<AdvancedModeModule> _modules = new List<AdvancedModeModule>();
        protected AdvancedModeModule _currentModule;
        protected GenericOCITarget _target;
        #endregion

        #region Private Variables
        internal static bool _drawAdvancedMode = false;
        internal static readonly HashSet<DynamicBoneCollider> _loneColliders = new HashSet<DynamicBoneCollider>();
        internal readonly HashSet<GameObject> _childObjects = new HashSet<GameObject>();
        #endregion

        #region Public Accessors
        public bool colliderEditEnabled { get { return this._bonesEditor.colliderEditEnabled; } }
        public virtual bool isDraggingDynamicBone { get { return this._dynamicBonesEditor.isDraggingDynamicBone; } }
        public GenericOCITarget target { get { return this._target; } }
        #endregion

        #region Unity Methods
        protected virtual void Awake()
        {
            foreach (KeyValuePair<int, ObjectCtrlInfo> pair in Studio.Studio.Instance.dicObjectCtrl)
            {
                if (pair.Value.guideObject.transformTarget.gameObject == this.gameObject)
                {
                    this._target = new GenericOCITarget(pair.Value);
                    break;
                }
            }

            this.FillChildObjects();
            DynamicBoneCollider collider;
            if (this.transform.childCount == 1 && (collider = this.transform.GetChild(0).GetComponent<DynamicBoneCollider>()) != null)
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

            this._bonesEditor = new BonesEditor(this, this._target);
            this._modules.Add(this._bonesEditor);

            this._dynamicBonesEditor = new DynamicBonesEditor(this, this._target);
            this._modules.Add(this._dynamicBonesEditor);

            this._blendShapesEditor = new BlendShapesEditor(this, this._target);
            this._modules.Add(this._blendShapesEditor);

            this._currentModule = this._bonesEditor;
            this._currentModule.isEnabled = true;

            onParentage += this.OnParentage;
        }

        protected virtual void Start()
        {
            this.FillChildObjects();
        }

        protected virtual void Update()
        {
            this.onUpdate();
        }

        protected virtual void LateUpdate()
        {
            this.onLateUpdate();
        }

        void OnGUI()
        {
            if (this._bonesEditor._colliderTarget && this._bonesEditor._isEnabled && PoseController._drawAdvancedMode && MainWindow._self._poseTarget == this)
                this._bonesEditor.OnGUI();
        }

        void OnDisable()
        {
            this.onDisable();
        }

        protected virtual void OnDestroy()
        {
            onParentage -= this.OnParentage;
            this.onDestroy();
            if (this._isLoneCollider)
            {
                DynamicBoneCollider collider = this.transform.GetChild(0).GetComponent<DynamicBoneCollider>();
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
        public virtual void LoadFrom(PoseController other)
        {
            if (other == null)
                return;
            this._bonesEditor.LoadFrom(other._bonesEditor);
            this._dynamicBonesEditor.LoadFrom(other._dynamicBonesEditor);
            this._blendShapesEditor.LoadFrom(other._blendShapesEditor);
            foreach (GameObject ignoredObject in other._childObjects)
            {
                if (ignoredObject == null)
                    continue;
                Transform obj = this.transform.Find(ignoredObject.transform.GetPathFrom(other.transform));
                if (obj != null && obj != this.transform)
                    this._childObjects.Add(obj.gameObject);
            }
        }

        public void AdvancedModeWindow(int id)
        {
            if (this.enabled == false)
            {
                GUILayout.BeginVertical();
                GUILayout.FlexibleSpace();
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.Label("In order to optimize things, the Advanced Mode is disabled on this object, you can enable it below.");
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                Color co = GUI.color;
                GUI.color = Color.magenta;
                if (GUILayout.Button("Enable", GUILayout.ExpandWidth(false)))
                    this.enabled = true;
                GUI.color = co;
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.EndVertical();
                GUI.DragWindow();
                return;
            }
            GUILayout.BeginHorizontal();
            foreach (AdvancedModeModule module in this._modules)
            {
                if (module.shouldDisplay && GUILayout.Button(module.displayName))
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
            GUI.color = Color.magenta;
            if (GUILayout.Button("Disable", GUILayout.ExpandWidth(false)))
                this.enabled = false;
            GUI.color = AdvancedModeModule._redColor;
            if (GUILayout.Button("Close", GUILayout.ExpandWidth(false)))
                this.ToggleAdvancedMode();
            GUI.color = c;
            GUILayout.EndHorizontal();
            this._currentModule.GUILogic();
            GUI.DragWindow();
        }

        public void ToggleAdvancedMode()
        {
            _drawAdvancedMode = !_drawAdvancedMode;
            foreach (AdvancedModeModule module in this._modules)
                module.DrawAdvancedModeChanged();
        }

        public static void SelectionChanged(PoseController self)
        {
            if (self != null)
            {
                BonesEditor.SelectionChanged(self._bonesEditor);
                DynamicBonesEditor.SelectionChanged(self._dynamicBonesEditor);
                CharaPoseController self2 = self as CharaPoseController;
                BoobsEditor.SelectionChanged(self2 != null ? self2._boobsEditor : null);
            }
            else
            {
                BonesEditor.SelectionChanged(null);
                DynamicBonesEditor.SelectionChanged(null);
                BoobsEditor.SelectionChanged(null);
            }
        }

        public static void InstallOnParentageEvent()
        {
            Action<TreeNodeObject, TreeNodeObject> oldDelegate = Studio.Studio.Instance.treeNodeCtrl.onParentage;
            Studio.Studio.Instance.treeNodeCtrl.onParentage = (parent, node) => PoseController.onParentage?.Invoke(parent, node);
            onParentage += oldDelegate;
        }

        public void ScheduleLoad(XmlNode node, Action<bool> onLoadEnd)
        {
            this.StartCoroutine(this.LoadDefaultVersion_Routine(node, onLoadEnd));
        }

        public virtual void SaveXml(XmlTextWriter xmlWriter)
        {
            foreach (AdvancedModeModule module in this._modules)
                module.SaveXml(xmlWriter);
        }
        #endregion

        #region Protected Methods
        protected virtual bool LoadDefaultVersion(XmlNode xmlNode)
        {
            bool changed = false;
            foreach (AdvancedModeModule module in this._modules)
                changed = module.LoadXml(xmlNode) || changed;
            return changed;
        }
        #endregion

        #region Private Methods
        private void FillChildObjects()
        {
            foreach (KeyValuePair<TreeNodeObject, ObjectCtrlInfo> pair in Studio.Studio.Instance.dicInfo)
            {
                if (pair.Value.guideObject.transformTarget != this.transform)
                    continue;
                foreach (TreeNodeObject child in pair.Key.child)
                {
                    this.RecurseChildObjects(child, childInfo =>
                    {
                        if (this._childObjects.Contains(childInfo.guideObject.transformTarget.gameObject) == false)
                            this._childObjects.Add(childInfo.guideObject.transformTarget.gameObject);
                    });
                }
                break;
            }
        }

        private void RecurseChildObjects(TreeNodeObject obj, Action<ObjectCtrlInfo> action)
        {
            ObjectCtrlInfo objInfo;
            if (Studio.Studio.Instance.dicInfo.TryGetValue(obj, out objInfo))
            {
                action(objInfo);
                return; //When the first "real" object is found, return to ignore its children;
            }
            foreach (TreeNodeObject child in obj.child)
                this.RecurseChildObjects(child, action);
        }

        private void OnParentage(TreeNodeObject parent, TreeNodeObject child)
        {
            if (parent == null)
            {
                ObjectCtrlInfo info;
                if (Studio.Studio.Instance.dicInfo.TryGetValue(child, out info) && this._childObjects.Contains(info.guideObject.transformTarget.gameObject))
                    this._childObjects.Remove(info.guideObject.transformTarget.gameObject);
            }
            else
            {
                ObjectCtrlInfo info;
                if (Studio.Studio.Instance.dicInfo.TryGetValue(child, out info) && info.guideObject.transformTarget != this.transform && info.guideObject.transformTarget.IsChildOf(this.transform))
                    this._childObjects.Add(info.guideObject.transformTarget.gameObject);
            }

            foreach (AdvancedModeModule module in this._modules)
                module.OnParentage(parent, child);
        }

        private IEnumerator LoadDefaultVersion_Routine(XmlNode xmlNode, Action<bool> onLoadEnd)
        {
            yield return null;
            yield return null;
            yield return null;
            bool changed = this.LoadDefaultVersion(xmlNode);
            onLoadEnd(changed);
        }
        #endregion

    }
}
