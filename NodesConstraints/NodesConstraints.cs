using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Harmony;
using RootMotion.FinalIK;
using ToolBox;
#if HONEYSELECT
//using RootMotion.FinalIK;
using IllusionPlugin;
#elif KOIKATSU
using BepInEx;
using ExtensibleSaveFormat;
using UnityEngine.SceneManagement;
using System.IO;
#endif
using Studio;
using UnityEngine;
using UnityEngine.EventSystems;
using Vectrosity;

namespace NodesConstraints
{
#if KOIKATSU
    [BepInPlugin(GUID: "com.joan6694.illusionplugins.nodesconstraints", Name: "NodesConstraints", Version: NodesConstraints.versionNum)]
    [BepInDependency("com.bepis.bepinex.extendedsave")]
    [BepInProcess("CharaStudio")]
#endif
    public class NodesConstraints :
#if HONEYSELECT
        IEnhancedPlugin
#elif KOIKATSU
        BaseUnityPlugin
#endif
    {
        public const string versionNum = "1.0.0";

        private static NodesConstraints _self;
        private const string _extSaveKey = "nodesConstraints";
        private const int _saveVersion = 0;

#if HONEYSELECT
        public string Name { get { return "NodesConstraints"; } }
        public string Version { get { return versionNum; } }
        public string[] Filter { get { return new[] { "StudioNEO_32", "StudioNEO_64" }; } }
#endif

        #region Private Types
        private class Constraint
        {
            public bool enabled = true;
            public GuideObject parent;
            public Transform parentTransform;
            public GuideObject child;
            public Transform childTransform;
            public bool position = true;
            public bool rotation = true;
            public Vector3 positionOffset = Vector3.zero;
            public Quaternion rotationOffset = Quaternion.identity;
            public Vector3 originalChildPosition;
            public Quaternion originalChildRotation;
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
                this._debugLine.points3[0] = this.parentTransform.position;
                this._debugLine.points3[1] = this.childTransform.position;
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
        //private static readonly Dictionary<string, string> _boneAliases = new Dictionary<string, string>();
        private readonly HashSet<GameObject> _openedBones = new HashSet<GameObject>();

        private bool _studioLoaded;
        private bool _showUI = false;
        private int _randomId;
        private Rect _windowRect = new Rect(Screen.width / 2f - 200, Screen.height / 2f - 300, 400, 600);
        private readonly Constraint _displayedConstraint = new Constraint();
        private Constraint _selectedConstraint;
        private readonly List<Constraint> _constraints = new List<Constraint>();
        private bool _mouseInWindow;
        private Vector2 _scroll;
        private HashSet<GuideObject> _selectedGuideObjects;
        private bool _initUI;
        private GUIStyle _wrapButton;
        private Transform _selectedBone;
        private Vector2 _advancedModeScroll;
        private Vector2 _simpleModeScroll;
        private HashSet<TreeNodeObject> _selectedWorkspaceObjects;
        private Dictionary<Transform, GuideObject> _allGuideObjects;
        private VectorLine _parentCircle;
        private VectorLine _childCircle;
        private VectorLine _selectedCircle;
        private Action _onPreCullAction;
        private CameraEventsDispatcher _dispatcher;
        private bool _advancedList = false;
        private string _search = "";
        #endregion

        #region Unity Methods

#if HONEYSELECT
        public void OnApplicationStart()
#elif KOIKATSU
        void Awake()
#endif
        {
            _self = this;
#if HONEYSELECT
            HSExtSave.HSExtSave.RegisterHandler("nodesConstraints", null, null, this.OnSceneLoad, this.OnSceneImport, this.OnSceneSave, null, null);
            float width = ModPrefs.GetFloat("NodesConstraints", "windowWidth", 400, true);
            if (width < 400)
                width = 400;
            this._windowRect = new Rect((Screen.width - width) / 2f, Screen.height / 2f - 300, width, 600);
#elif KOIKATSU
            SceneManager.sceneLoaded += this.SceneLoaded;
            ExtensibleSaveFormat.ExtendedSave.SceneBeingLoaded += this.OnSceneLoad;
            ExtensibleSaveFormat.ExtendedSave.SceneBeingImported += this.OnSceneImport;
            ExtensibleSaveFormat.ExtendedSave.SceneBeingSaved += this.OnSceneSave;            
#endif
            this._randomId = (int)(UnityEngine.Random.value * UInt32.MaxValue);
            HarmonyInstance harmony = HarmonyInstance.Create("com.joan6694.illusionplugins.nodesconstraints");
            harmony.PatchAll();
        }


#if HONEYSELECT
        public void OnLevelWasLoaded(int level)
        {
            if (level == 3)
                this.Init();
        }

        public void OnLevelWasInitialized(int level)
        {
        }
#elif KOIKATSU
        private void SceneLoaded(Scene scene, LoadSceneMode loadMode)
        {
            if (scene.buildIndex == 1 && loadMode == LoadSceneMode.Single)
                this.Init();
        }
#endif

        void Init()
        {

            this._studioLoaded = true;
            this._selectedWorkspaceObjects = (HashSet<TreeNodeObject>)Studio.Studio.Instance.treeNodeCtrl.GetPrivate("hashSelectNode");
            this._selectedGuideObjects = (HashSet<GuideObject>)GuideObjectManager.Instance.GetPrivate("hashSelectObject");
            this._allGuideObjects = (Dictionary<Transform, GuideObject>)GuideObjectManager.Instance.GetPrivate("dicGuideObject");
            this._dispatcher = Camera.main.gameObject.AddComponent<CameraEventsDispatcher>();
#if HONEYSELECT
            this._dispatcher.onGUI += this.OnGUI;
            VectorLine.SetCamera3D(Camera.main);
            this._dispatcher.gameObject.AddComponent<FKCtrl>();
#elif KOIKATSU
            this._dispatcher.onPreCull += this.ApplyConstraints;
            this._dispatcher.onPreRender += this.DrawDebugLines;
#endif
            this._dispatcher.ExecuteDelayed(() =>
            {
                this._parentCircle = VectorLine.SetLine(Color.green, new Vector3[16]);
                this._childCircle = VectorLine.SetLine(Color.red, new Vector3[16]);
                this._selectedCircle = VectorLine.SetLine(Color.cyan, new Vector3[16]);
                this._parentCircle.lineWidth = 4f;
                this._childCircle.lineWidth = 4f;
                this._childCircle.lineWidth = 4f;
                this._parentCircle.MakeCircle(Vector3.zero, Vector3.up, 0.03f);
                this._childCircle.MakeCircle(Vector3.zero, Vector3.up, 0.03f);
                this._selectedCircle.MakeCircle(Vector3.zero, Vector3.up, 0.03f);
            }, 2);
        }

#if HONEYSELECT
        public void OnUpdate()
#elif KOIKATSU
        void Update()
#endif

        {
            if (this._studioLoaded == false)
                return;
#if HONEYSELECT
            if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyDown(KeyCode.N))
#elif KOIKATSU
            if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyDown(KeyCode.I))
#endif
            {
                this._showUI = !this._showUI;
                if (this._selectedConstraint != null)
                    this._selectedConstraint.SetActiveDebugLines(this._showUI);
            }
            if (this._onPreCullAction != null)
            {
                this._onPreCullAction();
                this._onPreCullAction = null;
            }

        }

#if HONEYSELECT
        public void OnLateUpdate()
        {
        }
#endif


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


        public void OnApplicationQuit()
        {
        }


        public void OnFixedUpdate()
        {
        }
        #endregion

        #region Private Methods
#if HONEYSELECT
        [HarmonyPatch(typeof(FKCtrl), "LateUpdate")]
        private static class IKExecutionOrder_LateUpdate_Patches
        {
            private static void Postfix(object ___listBones)
            {
                if (((IList)___listBones).Count == 0)
                {
                    _self.ApplyConstraints();
                    _self.DrawDebugLines();
                }
            }
        }
#endif

        [HarmonyPatch(typeof(GuideSelect), "OnPointerClick", new[] { typeof(PointerEventData) })]
        private static class GuideSelect_OnPointerClick_Patches
        {
            private static void Postfix()
            {
                if (GuideObjectManager.Instance.selectObject != null)
                {
                    _self._selectedBone = GuideObjectManager.Instance.selectObject.transformTarget;
                }
            }
        }

        private void ApplyConstraints()
        {
            List<int> toDelete = null;
            for (int i = 0; i < this._constraints.Count; i++)
            {
                Constraint constraint = this._constraints[i];
                if (constraint.parentTransform == null || constraint.childTransform == null)
                {
                    if (toDelete == null)
                        toDelete = new List<int>();
                    toDelete.Add(i);
                    if (this._selectedConstraint == constraint)
                        this._selectedConstraint = null;
                    continue;
                }
                if (constraint.enabled == false)
                    continue;
                if (constraint.position)
                {
                    constraint.childTransform.position = constraint.parentTransform.TransformPoint(constraint.positionOffset);
                    if (constraint.child != null)
                        constraint.child.changeAmount.pos = constraint.child.transformTarget.localPosition;
                }
                if (constraint.rotation)
                {
                    constraint.childTransform.rotation = constraint.parentTransform.rotation * constraint.rotationOffset;
                    if (constraint.child != null)
                        constraint.child.changeAmount.rot = constraint.child.transformTarget.localEulerAngles;
                }
            }
            if (toDelete != null)
                for (int i = toDelete.Count - 1; i >= 0; --i)
                    this.RemoveConstraintAt(toDelete[i]);
        }

        private void DrawDebugLines()
        {
            this._parentCircle.active = this._displayedConstraint.parentTransform != null && this._showUI;
            if (this._parentCircle.active)
            {
                this._parentCircle.MakeCircle(this._displayedConstraint.parentTransform.position, Studio.Studio.Instance.cameraCtrl.mainCmaera.transform.forward, 0.01f);
                this._parentCircle.Draw();
            }
            this._childCircle.active = this._displayedConstraint.childTransform != null && this._showUI;
            if (this._childCircle.active)
            {
                this._childCircle.MakeCircle(this._displayedConstraint.childTransform.position, Studio.Studio.Instance.cameraCtrl.mainCmaera.transform.forward, 0.01f);
                this._childCircle.Draw();
            }

            if (this._advancedList)
            {
                this._selectedCircle.active = this._selectedBone != null && this._showUI;
                if (this._selectedCircle.active)
                {
                    this._selectedCircle.MakeCircle(this._selectedBone.position, Studio.Studio.Instance.cameraCtrl.mainCmaera.transform.forward, 0.01f);
                    this._selectedCircle.Draw();
                }
            }
            else
            {
                GuideObject selectedGuideObject = this._selectedGuideObjects.FirstOrDefault();
                this._selectedCircle.active = selectedGuideObject != null && this._showUI;
                if (this._selectedCircle.active)
                {
                    this._selectedCircle.MakeCircle(selectedGuideObject.transformTarget.position, Studio.Studio.Instance.cameraCtrl.mainCmaera.transform.forward, 0.01f);
                    this._selectedCircle.Draw();
                }
            }

            if (this._selectedConstraint != null && this._showUI)
                this._selectedConstraint.UpdateDebugLines();
        }

        private void WindowFunction(int id)
        {
            GUILayout.BeginVertical();
            {
                GUILayout.BeginHorizontal();
                {
                    GUILayout.BeginVertical();
                    {
                        GUILayout.BeginHorizontal();
                        {
                            GUILayout.Label((this._displayedConstraint.parentTransform != null ? this._displayedConstraint.parentTransform.name : ""));
                            GUILayout.FlexibleSpace();
                            GUILayout.Label((this._displayedConstraint.childTransform != null ? this._displayedConstraint.childTransform.name : ""));
                        }
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        {
                            GUI.enabled = this._displayedConstraint.childTransform != null;
                            this._displayedConstraint.position = GUILayout.Toggle(this._displayedConstraint.position && this._displayedConstraint.childTransform != null, "Link position");
                            GUILayout.FlexibleSpace();
                            GUILayout.Label("X", GUILayout.ExpandWidth(false));
                            string before = this._displayedConstraint.positionOffset.x.ToString("0.000");
                            string after = GUILayout.TextField(before, GUILayout.Width(50));
                            if (before != after)
                            {
                                if (float.TryParse(after, out float res))
                                    this._displayedConstraint.positionOffset.x = res;
                            }
                            GUILayout.Label("Y");
                            before = this._displayedConstraint.positionOffset.y.ToString("0.000");
                            after = GUILayout.TextField(before, GUILayout.Width(50));
                            if (before != after)
                            {
                                if (float.TryParse(after, out float res))
                                    this._displayedConstraint.positionOffset.y = res;
                            }
                            GUILayout.Label("Z");
                            before = this._displayedConstraint.positionOffset.z.ToString("0.000");
                            after = GUILayout.TextField(before, GUILayout.Width(50));
                            if (before != after)
                            {
                                if (float.TryParse(after, out float res))
                                    this._displayedConstraint.positionOffset.z = res;
                            }
                            GUI.enabled = this._displayedConstraint.parentTransform != null && this._displayedConstraint.childTransform != null;
                            if (GUILayout.Button("Set current", GUILayout.ExpandWidth(false)))
                                this._onPreCullAction = () => { this._displayedConstraint.positionOffset = this._displayedConstraint.parentTransform.InverseTransformPoint(this._displayedConstraint.childTransform.position); };
                            if (GUILayout.Button("Reset", GUILayout.ExpandWidth(false)))
                                this._displayedConstraint.positionOffset = Vector3.zero;

                            GUI.enabled = true;
                        }
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        {
                            GUI.enabled = this._displayedConstraint.childTransform != null;
                            this._displayedConstraint.rotation = GUILayout.Toggle(this._displayedConstraint.rotation && this._displayedConstraint.childTransform != null, "Link rotation");
                            GUILayout.FlexibleSpace();
                            GUILayout.Label("X", GUILayout.ExpandWidth(false));
                            string before = this._displayedConstraint.rotationOffset.eulerAngles.x.ToString("0.00");
                            string after = GUILayout.TextField(before, GUILayout.Width(50));
                            if (before != after)
                            {
                                if (float.TryParse(after, out float res))
                                    this._displayedConstraint.rotationOffset = Quaternion.Euler(res, this._displayedConstraint.rotationOffset.eulerAngles.y, this._displayedConstraint.rotationOffset.eulerAngles.z);
                            }
                            GUILayout.Label("Y", GUILayout.ExpandWidth(false));
                            before = this._displayedConstraint.rotationOffset.eulerAngles.y.ToString("0.00");
                            after = GUILayout.TextField(before, GUILayout.Width(50));
                            if (before != after)
                            {
                                if (float.TryParse(after, out float res))
                                    this._displayedConstraint.rotationOffset = Quaternion.Euler(this._displayedConstraint.rotationOffset.eulerAngles.x, res, this._displayedConstraint.rotationOffset.eulerAngles.z);
                            }
                            GUILayout.Label("Z", GUILayout.ExpandWidth(false));
                            before = this._displayedConstraint.rotationOffset.eulerAngles.z.ToString("0.00");
                            after = GUILayout.TextField(before, GUILayout.Width(50));
                            if (before != after)
                            {
                                if (float.TryParse(after, out float res))
                                    this._displayedConstraint.rotationOffset = Quaternion.Euler(this._displayedConstraint.rotationOffset.eulerAngles.x, this._displayedConstraint.rotationOffset.eulerAngles.y, res);
                            }
                            GUI.enabled = this._displayedConstraint.parentTransform != null && this._displayedConstraint.childTransform != null;
                            if (GUILayout.Button("Set current", GUILayout.ExpandWidth(false)))
                            {
                                this._onPreCullAction = () => { this._displayedConstraint.rotationOffset = Quaternion.Inverse(this._displayedConstraint.parentTransform.rotation) * this._displayedConstraint.childTransform.rotation; };
                            }
                            if (GUILayout.Button("Reset", GUILayout.ExpandWidth(false)))
                                this._displayedConstraint.rotationOffset = Quaternion.identity;
                            GUI.enabled = true;
                        }
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        {
                            GUI.enabled = this._displayedConstraint.parentTransform != null && this._displayedConstraint.childTransform != null && (this._displayedConstraint.position || this._displayedConstraint.rotation) && this._displayedConstraint.parentTransform != this._displayedConstraint.childTransform;
                            if (GUILayout.Button("Add new"))
                            {
                                bool shouldAdd = true;
                                foreach (Constraint constraint in this._constraints)
                                {
                                    if (constraint.parentTransform == this._displayedConstraint.parentTransform && constraint.childTransform == this._displayedConstraint.childTransform ||
                                        constraint.childTransform == this._displayedConstraint.parentTransform && constraint.parentTransform == this._displayedConstraint.childTransform)
                                    {
                                        shouldAdd = false;
                                        break;
                                    }
                                }
                                if (shouldAdd)
                                {
                                    Constraint newConstraint = new Constraint();
                                    newConstraint.parentTransform = this._displayedConstraint.parentTransform;
                                    if (this._allGuideObjects.TryGetValue(newConstraint.parentTransform, out newConstraint.parent) == false)
                                        newConstraint.parent = null;
                                    newConstraint.childTransform = this._displayedConstraint.childTransform;
                                    if (this._allGuideObjects.TryGetValue(newConstraint.childTransform, out newConstraint.child) == false)
                                        newConstraint.child = null;
                                    newConstraint.position = this._displayedConstraint.position;
                                    newConstraint.rotation = this._displayedConstraint.rotation;
                                    newConstraint.positionOffset = this._displayedConstraint.positionOffset;
                                    newConstraint.rotationOffset = this._displayedConstraint.rotationOffset;
                                    newConstraint.originalChildPosition = newConstraint.childTransform.localPosition;
                                    newConstraint.originalChildRotation = newConstraint.childTransform.localRotation;

                                    this._constraints.Add(newConstraint);
                                }
                            }
                            GUI.enabled = this._selectedConstraint != null && this._displayedConstraint.parentTransform != null && this._displayedConstraint.childTransform != null && (this._displayedConstraint.position || this._displayedConstraint.rotation) && this._displayedConstraint.parentTransform != this._displayedConstraint.childTransform;
                            if (GUILayout.Button("Update selected"))
                            {
                                if (this._selectedConstraint.position && this._displayedConstraint.position == false)
                                {
                                    this._selectedConstraint.childTransform.localPosition = this._selectedConstraint.originalChildPosition;
                                    if (this._selectedConstraint.child != null)
                                        this._selectedConstraint.child.changeAmount.pos = this._selectedConstraint.originalChildPosition;
                                }
                                if (this._selectedConstraint.rotation && this._displayedConstraint.rotation == false)
                                {
                                    this._selectedConstraint.childTransform.localRotation = this._selectedConstraint.originalChildRotation;
                                    if (this._selectedConstraint.child != null)
                                        this._selectedConstraint.child.changeAmount.rot = this._selectedConstraint.originalChildRotation.eulerAngles;
                                }

                                this._selectedConstraint.parentTransform = this._displayedConstraint.parentTransform;
                                if (this._allGuideObjects.TryGetValue(this._selectedConstraint.parentTransform, out this._selectedConstraint.parent) == false)
                                    this._selectedConstraint.parent = null;
                                this._selectedConstraint.childTransform = this._displayedConstraint.childTransform;
                                if (this._allGuideObjects.TryGetValue(this._selectedConstraint.childTransform, out this._selectedConstraint.child) == false)
                                    this._selectedConstraint.child = null;
                                this._selectedConstraint.position = this._displayedConstraint.position;
                                this._selectedConstraint.rotation = this._displayedConstraint.rotation;
                                this._selectedConstraint.positionOffset = this._displayedConstraint.positionOffset;
                                this._selectedConstraint.rotationOffset = this._displayedConstraint.rotationOffset;
                                this._selectedConstraint.originalChildPosition = this._selectedConstraint.childTransform.localPosition;
                                this._selectedConstraint.originalChildRotation = this._selectedConstraint.childTransform.localRotation;
                            }
                            GUI.enabled = true;
                        }
                        GUILayout.EndHorizontal();
                    }
                    GUILayout.EndVertical();
                }
                GUILayout.EndHorizontal();

                this._scroll = GUILayout.BeginScrollView(this._scroll, false, false, GUI.skin.horizontalScrollbar, GUI.skin.verticalScrollbar, GUI.skin.box, GUILayout.Height(150));
                {
                    int toDelete = -1;
                    for (int i = 0; i < this._constraints.Count; i++)
                    {
                        Constraint constraint = this._constraints[i];
                        GUILayout.BeginHorizontal();
                        {
                            Color c = GUI.color;
                            if (this._selectedConstraint == constraint)
                                GUI.color = Color.cyan;
                            bool newEnabled = GUILayout.Toggle(constraint.enabled, "", GUILayout.ExpandWidth(false));
                            if (constraint.enabled && newEnabled == false)
                            {
                                if (constraint.position)
                                {
                                    constraint.childTransform.localPosition = constraint.originalChildPosition;
                                    if (constraint.child != null)
                                        constraint.child.changeAmount.pos = constraint.originalChildPosition;
                                }
                                if (constraint.rotation)
                                {
                                    constraint.childTransform.localRotation = constraint.originalChildRotation;
                                    if (constraint.child != null)
                                        constraint.child.changeAmount.rot = constraint.originalChildRotation.eulerAngles;
                                }
                            }
                            if (constraint.enabled == false && newEnabled)
                            {
                                constraint.originalChildPosition = constraint.childTransform.localPosition;
                                constraint.originalChildRotation = constraint.childTransform.localRotation;
                            }
                            constraint.enabled = newEnabled;

                            if (GUILayout.Button(constraint.parentTransform.name + " -> " + constraint.childTransform.name, this._wrapButton))
                            {
                                if (this._selectedConstraint != null)
                                    this._selectedConstraint.SetActiveDebugLines(false);
                                this._selectedConstraint = constraint;
                                this._selectedConstraint.SetActiveDebugLines(true);
                                this._displayedConstraint.parentTransform = this._selectedConstraint.parentTransform;
                                this._displayedConstraint.childTransform = this._selectedConstraint.childTransform;
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
                        this.RemoveConstraintAt(toDelete);
                }
                GUILayout.EndScrollView();

                this._advancedList = GUILayout.Toggle(this._advancedList, "Advanced List");

                GuideObject selectedWorkspaceObject = null;
                TreeNodeObject treeNode = this._selectedWorkspaceObjects?.FirstOrDefault();
                if (treeNode != null)
                {
                    ObjectCtrlInfo info;
                    if (Studio.Studio.Instance.dicInfo.TryGetValue(treeNode, out info))
                        selectedWorkspaceObject = info.guideObject;
                }

                GUILayout.BeginHorizontal();
                string oldSearch = this._search;
                GUILayout.Label("Search", GUILayout.ExpandWidth(false));
                this._search = GUILayout.TextField(this._search);
                if (GUILayout.Button("X", GUILayout.ExpandWidth(false)))
                    this._search = "";
                if (oldSearch.Length != 0 && this._selectedBone != null && (this._search.Length == 0 || (this._search.Length < oldSearch.Length && oldSearch.StartsWith(this._search))))
                {
                    //string displayedName;
                    //bool aliased = true;
                    //if (_boneAliases.TryGetValue(this._selectedBone.name, out displayedName) == false)
                    //{
                    //    displayedName = this._selectedBone.name;
                    //    aliased = false;
                    //}
                    if (this._selectedBone.name.IndexOf(oldSearch, StringComparison.OrdinalIgnoreCase) != -1/* || (aliased && displayedName.IndexOf(oldSearch, StringComparison.OrdinalIgnoreCase) != -1)*/)
                        this.OpenParents(this._selectedBone, selectedWorkspaceObject.transformTarget);
                }
                GUILayout.EndHorizontal();

                GuideObject selectedGuideObject = this._selectedGuideObjects.FirstOrDefault();

                if (this._advancedList == false)
                {
                    this._simpleModeScroll = GUILayout.BeginScrollView(this._simpleModeScroll, false, false, GUI.skin.horizontalScrollbar, GUI.skin.verticalScrollbar, GUI.skin.box);
                    if (selectedWorkspaceObject != null)
                    {
                        foreach (KeyValuePair<Transform, GuideObject> pair in this._allGuideObjects)
                        {
                            if (pair.Key == null)
                                continue;
                            if (pair.Key.IsChildOf(selectedWorkspaceObject.transformTarget) == false && pair.Key != selectedWorkspaceObject.transformTarget)
                                continue;
                            if (pair.Key.name.IndexOf(this._search, StringComparison.OrdinalIgnoreCase) == -1)
                                continue;
                            Color c = GUI.color;
                            if (pair.Value == selectedGuideObject)
                                GUI.color = Color.cyan;
                            else if (this._displayedConstraint.parentTransform == pair.Value.transformTarget)
                                GUI.color = Color.green;
                            else if (this._displayedConstraint.childTransform == pair.Value.transformTarget)
                                GUI.color = Color.red;

                            if (GUILayout.Button(pair.Key.name))
                                GuideObjectManager.Instance.selectObject = pair.Value;
                            GUI.color = c;
                        }
                    }
                    GUILayout.EndScrollView();
                }
                else
                {
                    this._advancedModeScroll = GUILayout.BeginScrollView(this._advancedModeScroll, false, false, GUI.skin.horizontalScrollbar, GUI.skin.verticalScrollbar, GUI.skin.box);
                    if (selectedWorkspaceObject != null)
                        this.DisplayObjectTree(selectedWorkspaceObject.transformTarget.GetChild(0).gameObject, 0);
                    GUILayout.EndScrollView();
                }

                GUILayout.BeginHorizontal();
                GUI.enabled = this._selectedBone != null;
                if (GUILayout.Button("Set as parent"))
                {
                    this._displayedConstraint.parentTransform = this._advancedList ? this._selectedBone : selectedGuideObject.transformTarget;
                }
                GUI.enabled = true;

                GUI.enabled = selectedGuideObject != null;
                if (GUILayout.Button("Set as child"))
                {
                    this._displayedConstraint.childTransform = this._advancedList ? this._selectedBone : selectedGuideObject.transformTarget;
                }
                GUI.enabled = true;
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
            GUI.DragWindow();
        }

        private void RemoveConstraintAt(int index)
        {
            Constraint c = this._constraints[index];
            if (c.childTransform != null)
            {
                if (c.position)
                {
                    c.childTransform.localPosition = c.originalChildPosition;
                    if (c.child != null)
                        c.child.changeAmount.pos = c.originalChildPosition;
                }
                if (c.rotation)
                {
                    c.childTransform.localRotation = c.originalChildRotation;
                    if (c.child != null)
                        c.child.changeAmount.rot = c.originalChildRotation.eulerAngles;
                }
            }

            c.Destroy();
            this._constraints.RemoveAt(index);
            if (c == this._selectedConstraint)
                this._selectedConstraint = null;
        }

        private void DisplayObjectTree(GameObject go, int indent)
        {
            string displayedName = go.name;
            //bool aliased = true;
            //if (_boneAliases.TryGetValue(go.name, out displayedName) == false)
            //{
            //    displayedName = go.name;
            //    aliased = false;
            //}

            if (this._search.Length == 0 || go.name.IndexOf(this._search, StringComparison.OrdinalIgnoreCase) != -1/* || (aliased && displayedName.IndexOf(_search, StringComparison.OrdinalIgnoreCase) != -1)*/)
            {
                Color c = GUI.color;
                if (this._selectedBone == go.transform)
                    GUI.color = Color.cyan;
                else if (this._displayedConstraint.parentTransform == go.transform)
                    GUI.color = Color.green;
                else if (this._displayedConstraint.childTransform == go.transform)
                    GUI.color = Color.red;
                GUILayout.BeginHorizontal();
                if (this._search.Length == 0)
                {
                    GUILayout.Space(indent * 20f);
                    if (go.transform.childCount != 0)
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
                }
                if (GUILayout.Button(displayedName, GUILayout.ExpandWidth(false)))
                    this._selectedBone = go.transform;
                GUI.color = c;
                GUILayout.EndHorizontal();
            }
            if (this._search.Length != 0 || this._openedBones.Contains(go))
                for (int i = 0; i < go.transform.childCount; ++i)
                    this.DisplayObjectTree(go.transform.GetChild(i).gameObject, indent + 1);
        }


        private void OpenParents(Transform child, Transform limit)
        {
            if (child == limit)
                return;
            child = child.parent;
            while (child.parent != null && child != limit)
            {
                this._openedBones.Add(child.gameObject);
                child = child.parent;
            }
            this._openedBones.Add(child.gameObject);
        }
        #endregion

        #region Saves
#if HONEYSELECT
        private void OnSceneLoad(string path, XmlNode node)
        {
            if (node == null)
                return;
            this.LoadSceneGeneric(node.FirstChild);
        }
#elif KOIKATSU
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
#endif
#if HONEYSELECT
        private void OnSceneImport(string path, XmlNode node)
        {
            if (node == null)
                return;
            int max = -1;
            foreach (KeyValuePair<int, ObjectCtrlInfo> pair in Studio.Studio.Instance.dicObjectCtrl)
            {
                if (pair.Key > max)
                    max = pair.Key;
            }
            this.LoadSceneGeneric(node.FirstChild, max);
        }
#elif KOIKATSU
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
#endif

        private void LoadSceneGeneric(XmlNode node, int lastIndex = -1)
        {
            this._dispatcher.ExecuteDelayed(() =>
            {
                string v = node.Attributes["version"].Value;
                List<KeyValuePair<int, ObjectCtrlInfo>> dic = new SortedDictionary<int, ObjectCtrlInfo>(Studio.Studio.Instance.dicObjectCtrl).Where(p => p.Key > lastIndex).ToList();

                foreach (XmlNode childNode in node.ChildNodes)
                {
                    int parentObjectIndex = XmlConvert.ToInt32(childNode.Attributes["parentObjectIndex"].Value);
                    if (parentObjectIndex >= dic.Count)
                        continue;
                    Transform parentTransform = dic[parentObjectIndex].Value.guideObject.transformTarget;
                    parentTransform = parentTransform.Find(childNode.Attributes["parentPath"].Value);
                    if (parentTransform == null)
                        continue;

                    int childObjectIndex = XmlConvert.ToInt32(childNode.Attributes["childObjectIndex"].Value);
                    if (childObjectIndex >= dic.Count)
                        continue;
                    Transform childTransform;
                    if (childNode.Attributes["childPath"] != null)
                    {
                        childTransform = dic[childObjectIndex].Value.guideObject.transformTarget;
                        childTransform = childTransform.Find(childNode.Attributes["childPath"].Value);
                        if (childTransform == null)
                            continue;
                    }
                    else
                    {
                        childTransform = dic[childObjectIndex].Value.guideObject.transformTarget;
                        childTransform = childTransform.FindDescendant(childNode.Attributes["childName"].Value);
                        if (childTransform == null)
                            continue;
                    }


                    Constraint constraint = new Constraint();
                    constraint.enabled = childNode.Attributes["enabled"] == null || XmlConvert.ToBoolean(childNode.Attributes["enabled"].Value);
                    constraint.parentTransform = parentTransform;
                    constraint.childTransform = childTransform;

                    if (this._allGuideObjects.TryGetValue(constraint.parentTransform, out constraint.parent) == false)
                        constraint.parent = null;
                    if (this._allGuideObjects.TryGetValue(constraint.childTransform, out constraint.child) == false)
                        constraint.child = null;

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
            }, 5);
        }

#if HONEYSELECT
        private void OnSceneSave(string path, XmlTextWriter writer)
        {
            this.SaveSceneGeneric(writer);
        }
#elif KOIKATSU
        private void OnSceneSave(string path)
        {
            using (StringWriter stringWriter = new StringWriter())
            using (XmlTextWriter xmlWriter = new XmlTextWriter(stringWriter))
            {
                this.SaveSceneGeneric(xmlWriter);

                PluginData data = new PluginData();
                data.version = _saveVersion;
                data.data.Add("constraints", stringWriter.ToString());
                ExtendedSave.SetSceneExtendedDataById(_extSaveKey, data);
            }

        }
#endif

        private void SaveSceneGeneric(XmlTextWriter xmlWriter)
        {
            List<KeyValuePair<int, ObjectCtrlInfo>> dic = new SortedDictionary<int, ObjectCtrlInfo>(Studio.Studio.Instance.dicObjectCtrl).ToList();

            xmlWriter.WriteStartElement("constraints");

            xmlWriter.WriteAttributeString("version", versionNum);

            foreach (Constraint constraint in this._constraints)
            {
                int parentObjectIndex = -1;
                Transform parentT = constraint.parentTransform;
                while ((parentObjectIndex = dic.FindIndex(e => e.Value.guideObject.transformTarget == parentT)) == -1)
                    parentT = parentT.parent;

                int childObjectIndex = -1;
                Transform childT = constraint.childTransform;
                while ((childObjectIndex = dic.FindIndex(e => e.Value.guideObject.transformTarget == childT)) == -1)
                    childT = childT.parent;

                xmlWriter.WriteStartElement("constraint");

                xmlWriter.WriteAttributeString("enabled", XmlConvert.ToString(constraint.enabled));
                xmlWriter.WriteAttributeString("parentObjectIndex", XmlConvert.ToString(parentObjectIndex));
                xmlWriter.WriteAttributeString("parentPath", constraint.parentTransform.GetPathFrom(parentT));
                xmlWriter.WriteAttributeString("childObjectIndex", XmlConvert.ToString(childObjectIndex));
                xmlWriter.WriteAttributeString("childPath", constraint.childTransform.GetPathFrom(childT));

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
        }
        #endregion
    }
}
