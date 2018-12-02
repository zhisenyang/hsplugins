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
        #region Protected Variables
        protected BonesEditor _bonesEditor;
        protected DynamicBonesEditor _dynamicBonesEditor;
        protected BlendShapesEditor _blendShapesEditor;
        protected readonly List<AdvancedModeModule> _modules = new List<AdvancedModeModule>();
        protected AdvancedModeModule _currentModule;
        protected GenericOCITarget _target;
        #endregion

        #region Private Variables
        internal static bool _drawAdvancedMode = false;
        internal readonly HashSet<GameObject> _childObjects = new HashSet<GameObject>();
        #endregion

        #region Public Accessors
        public bool colliderEditEnabled { get { return this._bonesEditor.colliderEditEnabled; } }
        public virtual bool isDraggingDynamicBone { get { return this._dynamicBonesEditor.isDraggingDynamicBone; } }
        public GenericOCITarget target { get { return this._target; } }
        #endregion

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

            this._bonesEditor = this.gameObject.AddComponent<BonesEditor>();
            this._bonesEditor.parent = this;
            this._bonesEditor.target = this._target;
            this._modules.Add(this._bonesEditor);

            this._dynamicBonesEditor = this.gameObject.AddComponent<DynamicBonesEditor>();
            this._dynamicBonesEditor.parent = this;
            this._dynamicBonesEditor.target = this._target;
            this._modules.Add(this._dynamicBonesEditor);

            this._blendShapesEditor = this.gameObject.AddComponent<BlendShapesEditor>();
            this._blendShapesEditor.parent = this;
            this._blendShapesEditor.target = this._target;
            this._modules.Add(this._blendShapesEditor);

            this._currentModule = this._bonesEditor;
            this._currentModule.isEnabled = true;

            MainWindow.self.onParentage += this.OnParentage;
        }

        protected virtual void Start()
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

        protected virtual void OnDestroy()
        {
            MainWindow.self.onParentage -= this.OnParentage;
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
            GUI.color = AdvancedModeModule._redColor;
            if (GUILayout.Button("Close", GUILayout.ExpandWidth(false)))
                _drawAdvancedMode = false;
            GUI.color = c;
            GUILayout.EndHorizontal();
            this._currentModule.GUILogic();
            GUI.DragWindow();
        }

        public void SelectionChanged()
        {
            foreach (AdvancedModeModule module in this._modules)
                module.SelectionChanged();
        }

        public void ScheduleLoad(XmlNode node)
        {
            this.StartCoroutine(this.LoadDefaultVersion_Routine(node.CloneNode(true)));
        }

        public virtual void SaveXml(XmlTextWriter xmlWriter)
        {
            foreach (AdvancedModeModule module in this._modules)
                module.SaveXml(xmlWriter);
        }

        private IEnumerator LoadDefaultVersion_Routine(XmlNode xmlNode)
        {
            yield return null;
            yield return null;
            yield return null;
            this.LoadDefaultVersion(xmlNode);
        }

        protected virtual void LoadDefaultVersion(XmlNode xmlNode)
        {
            foreach (AdvancedModeModule module in this._modules)
                module.LoadXml(xmlNode);
        }
    }
}
