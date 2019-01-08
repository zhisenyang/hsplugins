using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Xml;
using BepInEx;
using ExtensibleSaveFormat;
using Harmony;
using Studio;
using ToolBox;
using UnityEngine;
using UnityEngine.SceneManagement;
using Vectrosity;

namespace NodesConstraints
{
    [BepInPlugin(GUID: "com.joan6694.illusionplugins.nodesconstraints", Name: "NodesConstraints", Version: NodesConstraints.versionNum)]
    [BepInDependency("com.bepis.bepinex.extendedsave")]
    [BepInProcess("CharaStudio")]
    public class NodesConstraints : BaseUnityPlugin
    {
        public const string versionNum = "1.0.0";

        private static NodesConstraints _self;
        private const string _extSaveKey = "nodesConstraints";
        private const int _saveVersion = 0;

        #region Private Types
        private class Constraint
        {
            public GuideObject parent;
            public GuideObject child;
            public bool position = true;
            public bool rotation = true;
            public Vector3 positionOffset = Vector3.zero;
            public Quaternion rotationOffset = Quaternion.identity;
            private VectorLine _debugLine;

            public Constraint()
            {
                this._debugLine = VectorLine.SetLine(Color.white, Vector3.zero, Vector3.one);
                this._debugLine.lineWidth = 3f;
                this._debugLine.active = false;
            }

            public void SetActiveDebugLines(bool active)
            {
                this._debugLine.active = active;
            }

            public void UpdateDebugLines()
            {
                this._debugLine.points3[0] = this.parent.transformTarget.position;
                this._debugLine.points3[1] = this.child.transformTarget.position;
                this._debugLine.SetColor((this.position && this.rotation ? Color.magenta : (this.position ? Color.cyan : Color.green)));
                this._debugLine.Draw();
            }

            public void Destroy()
            {
                VectorLine.Destroy(ref this._debugLine);
            }
        }
        #endregion

        #region Private Variables
        private bool _studioLoaded;
        private bool _showUI = false;
        private int _randomId;
        private Rect _windowRect = new Rect(Screen.width / 2f - 140, Screen.height / 2f - 200, 280, 400);
        private readonly Constraint _displayedConstraint = new Constraint();
        private Constraint _selectedConstraint;
        private readonly List<Constraint> _constraints = new List<Constraint>();
        private bool _mouseInWindow;
        private Vector2 _scroll;
        private HashSet<GuideObject> _selectedGuideObjects;
        private bool _initUI;
        private GUIStyle _wrapButton;
        private bool _applyCalled = false;
        #endregion

        #region Unity Methods
        void Awake()
        {
            _self = this;
            SceneManager.sceneLoaded += this.SceneLoaded;
            this._randomId = (int)(UnityEngine.Random.value * UInt32.MaxValue);
            ExtensibleSaveFormat.ExtendedSave.SceneBeingLoaded += this.OnSceneLoad;
            ExtensibleSaveFormat.ExtendedSave.SceneBeingImported += this.OnSceneImport;
            ExtensibleSaveFormat.ExtendedSave.SceneBeingSaved += this.OnSceneSave;
            HarmonyInstance harmony = HarmonyInstance.Create("com.joan6694.illusionplugins.nodesconstraints");
            harmony.PatchAll();
        }

        private void SceneLoaded(Scene scene, LoadSceneMode loadMode)
        {
            if (scene.buildIndex == 1 && loadMode == LoadSceneMode.Single)
            {
                this._studioLoaded = true;
                this._selectedGuideObjects = (HashSet<GuideObject>)GuideObjectManager.Instance.GetPrivate("hashSelectObject");
            }
        }

        void Update()
        {
            if (this._studioLoaded == false)
                return;
            if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyDown(KeyCode.I))
            {
                this._showUI = !this._showUI;
                if (this._selectedConstraint != null)
                    this._selectedConstraint.SetActiveDebugLines(this._showUI);
            }
        }

        void OnGUI()
        {
            if (this._showUI == false)
                return;
            if (this._initUI == false)
            {
                this._wrapButton = new GUIStyle(GUI.skin.button);
                this._wrapButton.wordWrap = true;
                this._wrapButton.alignment = TextAnchor.MiddleLeft;
                this._initUI = true;
            }
            this._mouseInWindow = this._windowRect.Contains(Event.current.mousePosition);
            this._windowRect = GUILayout.Window(this._randomId, this._windowRect, this.WindowFunction, "Nodes Constraints");
            if (this._mouseInWindow)
                Studio.Studio.Instance.cameraCtrl.noCtrlCondition = () => this._mouseInWindow && this._showUI;

        }

        void LateUpdate()
        {
            this.ApplyConstraints();
            this._applyCalled = false;
        }

        void OnApplicationQuit()
        {

        }
        #endregion

        #region Private Methods
        private void ApplyConstraints()
        {
            if (this._applyCalled)
                return;
            List<int> toDelete = null;
            for (int i = 0; i < this._constraints.Count; i++)
            {
                Constraint constraint = this._constraints[i];
                if (constraint.parent == null || constraint.child == null)
                {
                    if (toDelete == null)
                        toDelete = new List<int>();
                    toDelete.Add(i);
                    if (this._selectedConstraint == constraint)
                        this._selectedConstraint = null;
                    continue;
                }
                if (constraint.position)
                    constraint.child.transformTarget.position = constraint.parent.transformTarget.TransformPoint(constraint.positionOffset);
                if (constraint.rotation)
                    constraint.child.transformTarget.rotation = constraint.parent.transformTarget.rotation * constraint.rotationOffset;
                if (constraint == this._selectedConstraint)
                    constraint.UpdateDebugLines();
            }
            if (toDelete != null)
                for (int i = toDelete.Count - 1; i >= 0; --i)
                {
                    Constraint c = this._constraints[toDelete[i]];
                    c.Destroy();
                    this._constraints.RemoveAt(toDelete[i]);
                }
            this._applyCalled = true;
        }
        private void WindowFunction(int id)
        {
            GUILayout.BeginVertical();
            {
                GuideObject selected = this._selectedGuideObjects.FirstOrDefault();
                GUILayout.Label("Selected: " + (selected != null ? selected.transformTarget.name : ""));
                GUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button("Set as parent"))
                    {
                        this._displayedConstraint.parent = selected;
                    }
                    if (GUILayout.Button("Set as child"))
                    {
                        this._displayedConstraint.child = selected;
                    }
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label((this._displayedConstraint.parent != null ? this._displayedConstraint.parent.transformTarget.name : ""));
                    GUILayout.FlexibleSpace();
                    GUILayout.Label((this._displayedConstraint.child != null ? this._displayedConstraint.child.transformTarget.name : ""));
                }
                GUILayout.EndHorizontal();

                
                GUILayout.BeginHorizontal();
                {
                    GUI.enabled = this._displayedConstraint.child != null && this._displayedConstraint.child.enablePos;
                    this._displayedConstraint.position = GUILayout.Toggle(this._displayedConstraint.position && this._displayedConstraint.child != null && this._displayedConstraint.child.enablePos, "Link position");
                    GUILayout.FlexibleSpace();
                    GUILayout.Label("X", GUILayout.ExpandWidth(false));
                    string before = this._displayedConstraint.positionOffset.x.ToString("0.00");
                    string after = GUILayout.TextField(before, GUILayout.Width(40));
                    if (before != after)
                    {
                        if (float.TryParse(after, out float res))
                            this._displayedConstraint.positionOffset.x = res;
                    }
                    GUILayout.Label("Y");
                    before = this._displayedConstraint.positionOffset.y.ToString("0.00");
                    after = GUILayout.TextField(before, GUILayout.Width(40));
                    if (before != after)
                    {
                        if (float.TryParse(after, out float res))
                            this._displayedConstraint.positionOffset.y = res;
                    }
                    GUILayout.Label("Z");
                    before = this._displayedConstraint.positionOffset.z.ToString("0.00");
                    after = GUILayout.TextField(before, GUILayout.Width(40));
                    if (before != after)
                    {
                        if (float.TryParse(after, out float res))
                            this._displayedConstraint.positionOffset.z = res;
                    }
                    GUI.enabled = true;
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                {
                    GUI.enabled = this._displayedConstraint.child != null && this._displayedConstraint.child.enableRot;
                    this._displayedConstraint.rotation = GUILayout.Toggle(this._displayedConstraint.rotation && this._displayedConstraint.child != null && this._displayedConstraint.child.enableRot, "Link rotation");
                    GUILayout.FlexibleSpace();
                    GUILayout.Label("X", GUILayout.ExpandWidth(false));
                    string before = this._displayedConstraint.rotationOffset.eulerAngles.x.ToString("0.00");
                    string after = GUILayout.TextField(before, GUILayout.Width(40));
                    if (before != after)
                    {
                        if (float.TryParse(after, out float res))
                            this._displayedConstraint.rotationOffset = Quaternion.Euler(res, this._displayedConstraint.rotationOffset.eulerAngles.y, this._displayedConstraint.rotationOffset.eulerAngles.z);
                    }
                    GUILayout.Label("Y", GUILayout.ExpandWidth(false));
                    before = this._displayedConstraint.rotationOffset.eulerAngles.y.ToString("0.00");
                    after = GUILayout.TextField(before, GUILayout.Width(40));
                    if (before != after)
                    {
                        if (float.TryParse(after, out float res))
                            this._displayedConstraint.rotationOffset = Quaternion.Euler(this._displayedConstraint.rotationOffset.eulerAngles.x, res, this._displayedConstraint.rotationOffset.eulerAngles.z);
                    }
                    GUILayout.Label("Z", GUILayout.ExpandWidth(false));
                    before = this._displayedConstraint.rotationOffset.eulerAngles.z.ToString("0.00");
                    after = GUILayout.TextField(before, GUILayout.Width(40));
                    if (before != after)
                    {
                        if (float.TryParse(after, out float res))
                            this._displayedConstraint.rotationOffset = Quaternion.Euler(this._displayedConstraint.rotationOffset.eulerAngles.x, this._displayedConstraint.rotationOffset.eulerAngles.y, res);
                    }
                    GUI.enabled = true;
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                {
                    GUI.enabled = this._displayedConstraint.parent != null && this._displayedConstraint.child != null && (this._displayedConstraint.position || this._displayedConstraint.rotation) && this._displayedConstraint.parent != this._displayedConstraint.child;
                    if (GUILayout.Button("Add new"))
                    {
                        bool shouldAdd = true;
                        foreach (Constraint constraint in this._constraints)
                        {
                            if (constraint.parent == this._displayedConstraint.parent && constraint.child == this._displayedConstraint.child ||
                                constraint.child == this._displayedConstraint.parent && constraint.parent == this._displayedConstraint.child)
                            {
                                shouldAdd = false;
                                break;
                            }
                        }
                        if (shouldAdd)
                        {
                            Constraint newConstraint = new Constraint();
                            newConstraint.parent = this._displayedConstraint.parent;
                            newConstraint.child = this._displayedConstraint.child;
                            newConstraint.position = this._displayedConstraint.position;
                            newConstraint.rotation = this._displayedConstraint.rotation;
                            newConstraint.positionOffset = this._displayedConstraint.positionOffset;
                            newConstraint.rotationOffset = this._displayedConstraint.rotationOffset;
                            this._constraints.Add(newConstraint);
                        }
                    }
                    GUI.enabled = this._selectedConstraint != null && this._displayedConstraint.parent != null && this._displayedConstraint.child != null && (this._displayedConstraint.position || this._displayedConstraint.rotation) && this._displayedConstraint.parent != this._displayedConstraint.child;
                    if (GUILayout.Button("Update selected"))
                    {
                        this._selectedConstraint.parent = this._displayedConstraint.parent;
                        this._selectedConstraint.child = this._displayedConstraint.child;
                        this._selectedConstraint.position = this._displayedConstraint.position;
                        this._selectedConstraint.rotation = this._displayedConstraint.rotation;
                        this._selectedConstraint.positionOffset = this._displayedConstraint.positionOffset;
                        this._selectedConstraint.rotationOffset = this._displayedConstraint.rotationOffset;
                    }
                    GUI.enabled = true;
                }
                GUILayout.EndHorizontal();

                this._scroll = GUILayout.BeginScrollView(this._scroll, false, false, GUI.skin.horizontalScrollbar, GUI.skin.verticalScrollbar, GUI.skin.box);
                int toDelete = -1;
                for (int i = 0; i < this._constraints.Count; i++)
                {
                    Constraint constraint = this._constraints[i];
                    GUILayout.BeginHorizontal();
                    {
                        Color c = GUI.color;
                        if (this._selectedConstraint == constraint)
                            GUI.color = Color.cyan;
                        if (GUILayout.Button(constraint.parent.transformTarget.name + " -> " + constraint.child.transformTarget.name, this._wrapButton))
                        {
                            if (this._selectedConstraint != null)
                                this._selectedConstraint.SetActiveDebugLines(false);
                            this._selectedConstraint = constraint;
                            this._selectedConstraint.SetActiveDebugLines(true);
                            this._displayedConstraint.parent = this._selectedConstraint.parent;
                            this._displayedConstraint.child = this._selectedConstraint.child;
                            this._displayedConstraint.position = this._selectedConstraint.position;
                            this._displayedConstraint.rotation = this._selectedConstraint.rotation;
                            this._displayedConstraint.positionOffset = this._selectedConstraint.positionOffset;
                            this._displayedConstraint.rotationOffset = this._selectedConstraint.rotationOffset;
                        }
                        GUI.color = Color.red;
                        if (GUILayout.Button("X", GUILayout.ExpandWidth(false)))
                            toDelete = i;
                        GUI.color = c;
                    }
                    GUILayout.EndHorizontal();
                }
                if (toDelete != -1)
                {
                    Constraint c = this._constraints[toDelete];
                    c.Destroy();
                    this._constraints.RemoveAt(toDelete);
                }
                GUILayout.EndScrollView();
            }
            GUILayout.EndVertical();
            GUI.DragWindow();
        }
        #endregion

        #region Saves
        private void OnSceneLoad(string path)
        {
            PluginData data = ExtendedSave.GetSceneExtendedDataById(_extSaveKey);
            if (data == null)
                return;
            XmlDocument doc = new XmlDocument();
            doc.LoadXml((string)data.data["constraints"]);
            XmlNode node = doc.FirstChild;
            if (node == null)
                return;
            this.LoadSceneGeneric(node);
        }

        private void OnSceneImport(string path)
        {
            PluginData data = ExtendedSave.GetSceneExtendedDataById(_extSaveKey);
            if (data == null)
                return;
            XmlDocument doc = new XmlDocument();
            doc.LoadXml((string)data.data["constraints"]);
            XmlNode node = doc.FirstChild;
            if (node == null)
                return;
            int max = -1;
            foreach (KeyValuePair<int, ObjectCtrlInfo> pair in Studio.Studio.Instance.dicObjectCtrl)
            {
                if (pair.Key > max)
                    max = pair.Key;
            }
            this.LoadSceneGeneric(node, max);
        }

        private void LoadSceneGeneric(XmlNode node, int lastIndex = -1)
        {
            string v = node.Attributes["version"].Value;
            this.ExecuteDelayed(() =>
            {
                List<KeyValuePair<int, ObjectCtrlInfo>> dic = new SortedDictionary<int, ObjectCtrlInfo>(Studio.Studio.Instance.dicObjectCtrl).Where(p => p.Key > lastIndex).ToList();

                foreach (XmlNode childNode in node.ChildNodes)
                {
                    int parentObjectIndex = XmlConvert.ToInt32(childNode.Attributes["parentObjectIndex"].Value);
                    if (parentObjectIndex >= dic.Count)
                        continue;
                    Transform parentTransform = dic[parentObjectIndex].Value.guideObject.transformTarget;
                    parentTransform = parentTransform.FindDescendant(childNode.Attributes["parentName"].Value);
                    if (parentTransform == null)
                        continue;
                    GuideObject parentGuideObject = null;
                    foreach (KeyValuePair<int, ObjectCtrlInfo> pair in dic)
                    {
                        if (pair.Value.guideObject.transformTarget == parentTransform)
                        {
                            parentGuideObject = pair.Value.guideObject;
                            break;
                        }
                    }
                    if (parentGuideObject == null)
                        continue;

                    int childObjectIndex = XmlConvert.ToInt32(childNode.Attributes["childObjectIndex"].Value);
                    if (childObjectIndex >= dic.Count)
                        continue;
                    Transform childTransform = dic[childObjectIndex].Value.guideObject.transformTarget;
                    childTransform = childTransform.FindDescendant(childNode.Attributes["childName"].Value);
                    if (childTransform == null)
                        continue;
                    GuideObject childGuideObject = null;
                    foreach (KeyValuePair<int, ObjectCtrlInfo> pair in dic)
                    {
                        if (pair.Value.guideObject.transformTarget == childTransform)
                        {
                            childGuideObject = pair.Value.guideObject;
                            break;
                        }
                    }
                    if (childGuideObject == null)
                        continue;
                    Constraint constraint = new Constraint();
                    constraint.parent = parentGuideObject;
                    constraint.child = childGuideObject;
                    constraint.position = XmlConvert.ToBoolean(childNode.Attributes["position"].Value);
                    constraint.positionOffset = new Vector3(
                        XmlConvert.ToSingle(childNode.Attributes["positionOffsetX"].Value),
                        XmlConvert.ToSingle(childNode.Attributes["positionOffsetY"].Value),
                        XmlConvert.ToSingle(childNode.Attributes["positionOffsetZ"].Value)
                        );
                    constraint.rotation = XmlConvert.ToBoolean(childNode.Attributes["rotation"].Value);
                    constraint.rotationOffset = new Quaternion(
                        XmlConvert.ToSingle(childNode.Attributes["rotationOffsetX"].Value),
                        XmlConvert.ToSingle(childNode.Attributes["rotationOffsetY"].Value),
                        XmlConvert.ToSingle(childNode.Attributes["rotationOffsetZ"].Value),
                        XmlConvert.ToSingle(childNode.Attributes["rotationOffsetW"].Value)
                        );

                    this._constraints.Add(constraint);
                }
            });
        }

        private void OnSceneSave(string path)
        {
            List<KeyValuePair<int, ObjectCtrlInfo>> dic = new SortedDictionary<int, ObjectCtrlInfo>(Studio.Studio.Instance.dicObjectCtrl).ToList();

            using (StringWriter stringWriter = new StringWriter())
            using (XmlTextWriter xmlWriter = new XmlTextWriter(stringWriter))
            {
                xmlWriter.WriteStartElement("constraints");

                xmlWriter.WriteAttributeString("version", NodesConstraints.versionNum);

                foreach (Constraint constraint in this._constraints)
                {
                    GuideObject parent = constraint.parent;
                    while (parent.parentGuide != null)
                        parent = parent.parentGuide;
                    int parentObjectIndex = dic.FindIndex(o => o.Value.guideObject == parent);

                    GuideObject child = constraint.child;
                    while (child.parentGuide != null)
                        child = child.parentGuide;
                    int childObjectIndex = dic.FindIndex(o => o.Value.guideObject == child);

                    xmlWriter.WriteStartElement("constraint");

                    xmlWriter.WriteAttributeString("parentObjectIndex", XmlConvert.ToString(parentObjectIndex));
                    xmlWriter.WriteAttributeString("parentName", constraint.parent.transformTarget.name);
                    xmlWriter.WriteAttributeString("childObjectIndex", XmlConvert.ToString(childObjectIndex));
                    xmlWriter.WriteAttributeString("childName", constraint.child.transformTarget.name);

                    xmlWriter.WriteAttributeString("position", XmlConvert.ToString(constraint.position));
                    xmlWriter.WriteAttributeString("positionOffsetX", XmlConvert.ToString(constraint.positionOffset.x));
                    xmlWriter.WriteAttributeString("positionOffsetY", XmlConvert.ToString(constraint.positionOffset.y));
                    xmlWriter.WriteAttributeString("positionOffsetZ", XmlConvert.ToString(constraint.positionOffset.z));

                    xmlWriter.WriteAttributeString("rotation", XmlConvert.ToString(constraint.rotation));
                    xmlWriter.WriteAttributeString("rotationOffsetW", XmlConvert.ToString(constraint.rotationOffset.w));
                    xmlWriter.WriteAttributeString("rotationOffsetX", XmlConvert.ToString(constraint.rotationOffset.x));
                    xmlWriter.WriteAttributeString("rotationOffsetY", XmlConvert.ToString(constraint.rotationOffset.y));
                    xmlWriter.WriteAttributeString("rotationOffsetZ", XmlConvert.ToString(constraint.rotationOffset.z));

                    xmlWriter.WriteEndElement();
                }

                xmlWriter.WriteEndElement();

                PluginData data = new PluginData();
                data.version = NodesConstraints._saveVersion;
                data.data.Add("constraints", stringWriter.ToString());
                ExtendedSave.SetSceneExtendedDataById(_extSaveKey, data);
            }
        }
        #endregion

        #region Patches
        [HarmonyPatch(typeof(FKCtrl), "LateUpdate")]
        private static class FKCtrl_Patches
        {
            public static void Postfix(FKCtrl __instance)
            {
                _self.ApplyConstraints();
            }
        }
        #endregion
    }
}
