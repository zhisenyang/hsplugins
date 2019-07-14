using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Xml;
using Harmony;
using ToolBox;
#if HONEYSELECT
using IllusionPlugin;
#elif KOIKATSU
using BepInEx;
using KKAPI.Studio.SaveLoad;
using ExtensibleSaveFormat;
using UnityEngine.SceneManagement;
using MessagePack;
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
    [BepInDependency("marco.kkapi")]
    [BepInProcess("CharaStudio")]
#endif
    public class NodesConstraints :
#if HONEYSELECT
        IEnhancedPlugin
#elif KOIKATSU
        BaseUnityPlugin
#endif
    {
#if HONEYSELECT
        public const string versionNum = "1.0.1";
#elif KOIKATSU
        public const string versionNum = "1.0.1";
        private const string _extSaveKey = "nodesConstraints";
        private const int _saveVersion = 0;
#endif

        private static NodesConstraints _self;

#if HONEYSELECT
        public string Name { get { return "NodesConstraints"; } }
        public string Version { get { return versionNum; } }
        public string[] Filter { get { return new[] { "StudioNEO_32", "StudioNEO_64" }; } }
#endif

        #region Private Types
        private enum SimpleListShowNodeType
        {
            All,
            IK,
            FK
        }

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
            public string alias = "";
            private VectorLine _debugLine;

            public Constraint()
            {
                this._debugLine = VectorLine.SetLine(Color.white, Vector3.zero, Vector3.one);
                this._debugLine.lineWidth = 3f;
                this._debugLine.active = false;
            }

            public Constraint(Constraint other) : this()
            {
                this.enabled = other.enabled;
                this.parent = other.parent;
                this.parentTransform = other.parentTransform;
                this.child = other.child;
                this.childTransform = other.childTransform;
                this.position = other.position;
                this.rotation = other.rotation;
                this.positionOffset = other.positionOffset;
                this.rotationOffset = other.rotationOffset;
                this.originalChildPosition = other.originalChildPosition;
                this.originalChildRotation = other.originalChildRotation;
                this.alias = other.alias;
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

#if KOIKATSU
        [Serializable]
        [MessagePackObject]
        private class AnimationControllerInfo
        {
            [Key("CharDicKey")]
            public int CharDicKey { get; set; }
            [Key("ItemDicKey")]
            public int ItemDicKey { get; set; }
            [Key("IKPart")]
            public string IKPart { get; set; }
            [Key("Version")]
            public string Version { get; set; }
            public static AnimationControllerInfo Unserialize(byte[] data) => MessagePackSerializer.Deserialize<AnimationControllerInfo>(data);
            public byte[] Serialize() => MessagePackSerializer.Serialize(this);
        }
#endif
        #endregion

        #region Private Variables
        //private static readonly Dictionary<string, string> _boneAliases = new Dictionary<string, string>();
        private readonly HashSet<GameObject> _openedBones = new HashSet<GameObject>();
        private readonly GUIStyle _customBoxStyle = new GUIStyle { normal = new GUIStyleState { background = Texture2D.whiteTexture } };

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
        private GuideObject _selectedWorkspaceObject;
        private GuideObject _lastSelectedWorkspaceObject;
#if KOIKATSU
        private bool _kkAnimationControllerInstalled = true;
#endif
        private SimpleListShowNodeType _selectedShowNodeType = SimpleListShowNodeType.All;
        private string[] _simpleListShowNodeTypeNames;
#if HONEYSELECT
        private int _totalActiveExpressions = 0;
        private int _currentExpressionIndex = 0;
        private readonly HashSet<Expression> _allExpressions = new HashSet<Expression>();
#endif
        private string _positionXStr = "0.000";
        private string _positionYStr = "0.000";
        private string _positionZStr = "0.000";
        private string _rotationXStr = "0.00";
        private string _rotationYStr = "0.00";
        private string _rotationZStr = "0.00";
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

            this._simpleListShowNodeTypeNames = Enum.GetNames(typeof(SimpleListShowNodeType));
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
            this._dispatcher.gameObject.AddComponent<Expression>();
#elif KOIKATSU
            this._dispatcher.onPreCull += this.ApplyConstraints;
            this._dispatcher.onPreRender += this.DrawDebugLines;
            this._kkAnimationControllerInstalled = BepInEx.Bootstrap.Chainloader.Plugins
                                                          .Select(MetadataHelper.GetMetadata)
                                                          .FirstOrDefault(x => x.GUID == "com.deathweasel.bepinex.animationcontroller") != null;
#endif
#if HONEYSELECT
            this._dispatcher.ExecuteDelayed(() =>
            
#elif KOIKATSU
            this.ExecuteDelayed(() =>
#endif

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
            this._totalActiveExpressions = this._allExpressions.Count(e => e.enabled && e.gameObject.activeInHierarchy);
            this._currentExpressionIndex = 0;

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
            this._selectedWorkspaceObject = null;
            TreeNodeObject treeNode = this._selectedWorkspaceObjects?.FirstOrDefault();
            if (treeNode != null)
            {
                ObjectCtrlInfo info;
                if (Studio.Studio.Instance.dicInfo.TryGetValue(treeNode, out info))
                    this._selectedWorkspaceObject = info.guideObject;
            }
            if (this._selectedWorkspaceObject != this._lastSelectedWorkspaceObject && this._selectedWorkspaceObject != null)
                this._selectedBone = this._selectedWorkspaceObject.transformTarget;
            this._lastSelectedWorkspaceObject = this._selectedWorkspaceObject;
            this.ApplyNodesConstraints();
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
            this._windowRect = GUILayout.Window(this._randomId, this._windowRect, this.WindowFunction, "Nodes Constraints " + versionNum);
            Color c = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.6f, 0.6f, 0.6f, 0.5f);
            GUI.Box(this._windowRect, "", this._customBoxStyle);
            GUI.backgroundColor = c;

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
        [HarmonyPatch(typeof(Expression), "Start")]
        private static class Expression_Start_Patches
        {
            private static void Prefix(Expression __instance)
            {
                _self._allExpressions.Add(__instance);
            }
        }

        [HarmonyPatch(typeof(Expression), "OnDestroy")]
        private static class Expression_OnDestroy_Patches
        {
            private static void Prefix(Expression __instance)
            {
                _self._allExpressions.Remove(__instance);
            }
        }

        [HarmonyPatch(typeof(Expression), "LateUpdate")]
        private static class Expression_LateUpdate_Patches
        {
            private static void Postfix()
            {
                _self._currentExpressionIndex++;
                //UnityEngine.Debug.LogError("framecount " + Time.frameCount + " total " + _self._totalFKControllers + " current " + _self._currentFkCtrlIndex);
                if (_self._currentExpressionIndex == _self._totalActiveExpressions) //Dirty fucking hack that I hate to make sure this runs after *everything*
                {
                    _self.ApplyConstraints();
                    _self.DrawDebugLines();
                }
            }
        }
#endif

        [HarmonyPatch(typeof(Studio.Studio), "Duplicate")]
        private class Studio_Duplicate_Patches
        {
            private static void Prefix()
            {
                for (int i = 0; i < _self._constraints.Count; i++)
                {
                    Constraint constraint = _self._constraints[i];
                    if (constraint.parentTransform == null || constraint.childTransform == null)
                        continue;
                    if (constraint.enabled == false)
                        continue;
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

                Studio.Studio.Instance.ExecuteDelayed(() =>
                {
                    for (int i = 0; i < _self._constraints.Count; i++)
                    {
                        Constraint constraint = _self._constraints[i];
                        if (constraint.parentTransform == null || constraint.childTransform == null)
                            continue;

                        ObjectCtrlInfo parentObjectSource = null;
                        ObjectCtrlInfo parentObjectDestination = null;
                        ObjectCtrlInfo childObjectSource = null;
                        ObjectCtrlInfo childObjectDestination = null;

                        Transform parentT = constraint.parentTransform;
                        while ((parentObjectSource = Studio.Studio.Instance.dicObjectCtrl.FirstOrDefault(e => e.Value.guideObject.transformTarget == parentT).Value) == null)
                            parentT = parentT.parent;
                        foreach (KeyValuePair<int, int> pair in SceneInfo_Import_Patches._newToOldKeys)
                        {
                            if (pair.Value == parentObjectSource.objectInfo.dicKey)
                            {
                                parentObjectDestination = Studio.Studio.Instance.dicObjectCtrl[pair.Key];
                                break;
                            }
                        }
                        
                        Transform childT = constraint.childTransform;
                        while ((childObjectSource = Studio.Studio.Instance.dicObjectCtrl.FirstOrDefault(e => e.Value.guideObject.transformTarget == childT).Value) == null)
                            childT = childT.parent;
                        foreach (KeyValuePair<int, int> pair in SceneInfo_Import_Patches._newToOldKeys)
                        {
                            if (pair.Value == childObjectSource.objectInfo.dicKey)
                            {
                                childObjectDestination = Studio.Studio.Instance.dicObjectCtrl[pair.Key];
                                break;
                            }
                        }
                        if (parentObjectDestination != null && childObjectDestination != null)
                        {
                            _self.AddConstraint(
                                                constraint.enabled,
                                                parentObjectDestination.guideObject.transformTarget.Find(constraint.parentTransform.GetPathFrom(parentObjectSource.guideObject.transformTarget)),
                                                childObjectDestination.guideObject.transformTarget.Find(constraint.childTransform.GetPathFrom(childObjectSource.guideObject.transformTarget)),
                                                constraint.position,
                                                constraint.positionOffset,
                                                constraint.rotation,
                                                constraint.rotationOffset,
                                                constraint.alias
                                               );
                        }
                    }
                }, 3);
            }
        }

        [HarmonyPatch(typeof(ObjectInfo), "Load", new[] { typeof(BinaryReader), typeof(Version), typeof(bool), typeof(bool) })]
        internal static class ObjectInfo_Load_Patches
        {
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                bool set = false;
                List<CodeInstruction> instructionsList = instructions.ToList();
                for (int i = 0; i < instructionsList.Count; i++)
                {
                    CodeInstruction inst = instructionsList[i];
                    yield return inst;
                    if (set == false && instructionsList[i + 1].opcode == OpCodes.Pop)
                    {
                        yield return new CodeInstruction(OpCodes.Ldarg_0);
                        yield return new CodeInstruction(OpCodes.Call, typeof(ObjectInfo_Load_Patches).GetMethod(nameof(Injected), BindingFlags.NonPublic | BindingFlags.Static));
                        set = true;
                    }
                }
            }

            private static int Injected(int originalIndex, ObjectInfo __instance)
            {
                SceneInfo_Import_Patches._newToOldKeys.Add(__instance.dicKey, originalIndex);
                return originalIndex; //Doing this so other transpilers can use this value if they want
            }
        }

        [HarmonyPatch(typeof(SceneInfo), "Import", new[] { typeof(BinaryReader), typeof(Version) })]
        private static class SceneInfo_Import_Patches //This is here because I fucked up the save format making it impossible to import scenes correctly
        {
            internal static readonly Dictionary<int, int> _newToOldKeys = new Dictionary<int, int>();

            private static void Prefix()
            {
                _newToOldKeys.Clear();
            }
        }

        [HarmonyPatch(typeof(GuideSelect), "OnPointerClick", new[] { typeof(PointerEventData) })]
        private static class GuideSelect_OnPointerClick_Patches
        {
            private static void Postfix()
            {
                GuideObject selectedGuideObject = _self._selectedGuideObjects.FirstOrDefault();

                if (selectedGuideObject != null)
                {
                    _self._selectedBone = selectedGuideObject.transformTarget;
                }
            }
        }

        // Applies the constraints that have guideobjects linked (so that the underlying systems IK and FK can use those data after)
        private void ApplyNodesConstraints()
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
                if (constraint.enabled && constraint.child != null)
                {
                    if (constraint.position && constraint.child.enablePos)
                    {
                        constraint.childTransform.position = constraint.parentTransform.TransformPoint(constraint.positionOffset);
                        constraint.child.changeAmount.pos = constraint.child.transformTarget.localPosition;
                    }
                    if (constraint.rotation && constraint.child.enableRot)
                    {
                        constraint.childTransform.rotation = constraint.parentTransform.rotation * constraint.rotationOffset;
                        constraint.child.changeAmount.rot = constraint.child.transformTarget.localEulerAngles;
                    }
                }
            }
            if (toDelete != null)
                for (int i = toDelete.Count - 1; i >= 0; --i)
                    this.RemoveConstraintAt(toDelete[i]);
        }

        // Applies all the constraints indiscriminately after everything overwriting everything
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
            if (this._parentCircle != null)
            {
                this._parentCircle.active = this._displayedConstraint.parentTransform != null && this._showUI;
                if (this._parentCircle.active)
                {
                    this._parentCircle.MakeCircle(this._displayedConstraint.parentTransform.position, Studio.Studio.Instance.cameraCtrl.mainCmaera.transform.forward, 0.01f);
                    this._parentCircle.Draw();
                }
            }
            if (this._childCircle != null)
            {
                this._childCircle.active = this._displayedConstraint.childTransform != null && this._showUI;
                if (this._childCircle.active)
                {
                    this._childCircle.MakeCircle(this._displayedConstraint.childTransform.position, Studio.Studio.Instance.cameraCtrl.mainCmaera.transform.forward, 0.01f);
                    this._childCircle.Draw();
                }
            }

            if (this._selectedCircle != null)
            {
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
                            GUILayout.Label("->");
                            GUILayout.FlexibleSpace();
                            GUILayout.Label((this._displayedConstraint.childTransform != null ? this._displayedConstraint.childTransform.name : ""));
                        }
                        GUILayout.EndHorizontal();


                        GUILayout.BeginHorizontal();
                        {
                            GUI.enabled = this._displayedConstraint.parentTransform != null && this._displayedConstraint.childTransform != null;
                            this._displayedConstraint.position = GUILayout.Toggle(this._displayedConstraint.position && this._displayedConstraint.childTransform != null, "Link position");
                            GUILayout.FlexibleSpace();
                            GUILayout.Label("X", GUILayout.ExpandWidth(false));
                            this._positionXStr = GUILayout.TextField(this._positionXStr, GUILayout.Width(50));
                            GUILayout.Label("Y");
                            this._positionYStr = GUILayout.TextField(this._positionYStr, GUILayout.Width(50));
                            GUILayout.Label("Z");
                            this._positionZStr = GUILayout.TextField(this._positionZStr, GUILayout.Width(50));
                            if (GUILayout.Button("Set current", GUILayout.ExpandWidth(false)))
                                this._onPreCullAction = () =>
                                {
                                    this._displayedConstraint.positionOffset = this._displayedConstraint.parentTransform.InverseTransformPoint(this._displayedConstraint.childTransform.position);
                                    this.UpdateDisplayedPositionOffset();
                                };
                            if (GUILayout.Button("Reset", GUILayout.ExpandWidth(false)))
                            {
                                this._displayedConstraint.positionOffset = Vector3.zero;
                                this.UpdateDisplayedPositionOffset();
                            }
                            GUI.enabled = true;
                        }
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        {
                            GUI.enabled = this._displayedConstraint.parentTransform != null && this._displayedConstraint.childTransform != null;
                            this._displayedConstraint.rotation = GUILayout.Toggle(this._displayedConstraint.rotation && this._displayedConstraint.childTransform != null, "Link rotation");
                            GUILayout.FlexibleSpace();
                            GUILayout.Label("X", GUILayout.ExpandWidth(false));
                            this._rotationXStr = GUILayout.TextField(this._rotationXStr, GUILayout.Width(50));
                            GUILayout.Label("Y", GUILayout.ExpandWidth(false));
                            this._rotationYStr = GUILayout.TextField(this._rotationYStr, GUILayout.Width(50));
                            GUILayout.Label("Z", GUILayout.ExpandWidth(false));
                            this._rotationZStr = GUILayout.TextField(this._rotationZStr, GUILayout.Width(50));
                            if (GUILayout.Button("Set current", GUILayout.ExpandWidth(false)))
                            {
                                this._onPreCullAction = () =>
                                {
                                    this._displayedConstraint.rotationOffset = Quaternion.Inverse(this._displayedConstraint.parentTransform.rotation) * this._displayedConstraint.childTransform.rotation;
                                    this.UpdateDisplayedRotationOffset();
                                };
                            }
                            if (GUILayout.Button("Reset", GUILayout.ExpandWidth(false)))
                            {
                                this._displayedConstraint.rotationOffset = Quaternion.identity;
                                this.UpdateDisplayedRotationOffset();
                            }
                            GUI.enabled = true;
                        }
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        {
                            GUI.enabled = this._displayedConstraint.parentTransform != null && this._displayedConstraint.childTransform != null;
                            GUILayout.Label("Alias", GUILayout.ExpandWidth(false));
                            this._displayedConstraint.alias = GUILayout.TextField(this._displayedConstraint.alias);
                            GUI.enabled = true;
                        }
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        {
                            GUI.enabled = this._displayedConstraint.parentTransform != null && this._displayedConstraint.childTransform != null && (this._displayedConstraint.position || this._displayedConstraint.rotation) && this._displayedConstraint.parentTransform != this._displayedConstraint.childTransform;
                            if (GUILayout.Button("Add new"))
                            {
                                this.ValidateDisplayedPositionOffset();
                                this.ValidateDisplayedRotationOffset();

                                this.AddConstraint(true, this._displayedConstraint.parentTransform, this._displayedConstraint.childTransform, this._displayedConstraint.position, this._displayedConstraint.positionOffset, this._displayedConstraint.rotation, this._displayedConstraint.rotationOffset, this._displayedConstraint.alias);
                            }
                            GUI.enabled = this._selectedConstraint != null && this._displayedConstraint.parentTransform != null && this._displayedConstraint.childTransform != null && (this._displayedConstraint.position || this._displayedConstraint.rotation) && this._displayedConstraint.parentTransform != this._displayedConstraint.childTransform;
                            if (GUILayout.Button("Update selected"))
                            {
                                this.ValidateDisplayedPositionOffset();
                                this.ValidateDisplayedRotationOffset();
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
                                this._selectedConstraint.alias = this._displayedConstraint.alias;
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
                    Action afterLoopAction = null;
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

                            string constraintName;
                            if (string.IsNullOrEmpty(constraint.alias))
                                constraintName = constraint.parentTransform.name + " -> " + constraint.childTransform.name;
                            else
                                constraintName = constraint.alias;

                            if (GUILayout.Button(constraintName, this._wrapButton))
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
                                this._displayedConstraint.alias = this._selectedConstraint.alias;
                                this.UpdateDisplayedPositionOffset();
                                this.UpdateDisplayedRotationOffset();
                            }

                            if (GUILayout.Button("↑", GUILayout.ExpandWidth(false)) && i != 0)
                            {
                                int cachedI = i;
                                afterLoopAction = () =>
                                {
                                    this._constraints.RemoveAt(cachedI);
                                    this._constraints.Insert(cachedI - 1, constraint);
                                };
                            }
                            if (GUILayout.Button("↓", GUILayout.ExpandWidth(false)) && i != this._constraints.Count - 1)
                            {
                                int cachedI = i;
                                afterLoopAction = () =>
                                {
                                    this._constraints.RemoveAt(cachedI);
                                    this._constraints.Insert(cachedI + 1, constraint);
                                };
                            }

                            GUI.color = Color.red;
                            if (GUILayout.Button("X", GUILayout.ExpandWidth(false)))
                                toDelete = i;
                            GUI.color = c;
                        }
                        GUILayout.EndHorizontal();
                    }
                    if (afterLoopAction != null)
                        afterLoopAction();
                    if (toDelete != -1)
                        this.RemoveConstraintAt(toDelete);
                }
                GUILayout.EndScrollView();

                this._advancedList = GUILayout.Toggle(this._advancedList, "Advanced List");

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
                        this.OpenParents(this._selectedBone, this._selectedWorkspaceObject.transformTarget);
                }
                GUILayout.EndHorizontal();

                GuideObject selectedGuideObject = this._selectedGuideObjects.FirstOrDefault();

                if (this._advancedList == false)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Show nodes", GUILayout.ExpandWidth(false));
                    this._selectedShowNodeType = (SimpleListShowNodeType)GUILayout.SelectionGrid((int)this._selectedShowNodeType, this._simpleListShowNodeTypeNames, 3);
                    GUILayout.EndHorizontal();

                    this._simpleModeScroll = GUILayout.BeginScrollView(this._simpleModeScroll, false, false, GUI.skin.horizontalScrollbar, GUI.skin.verticalScrollbar, GUI.skin.box);
                    if (this._selectedWorkspaceObject != null)
                    {
                        foreach (KeyValuePair<Transform, GuideObject> pair in this._allGuideObjects)
                        {
                            if (pair.Key == null)
                                continue;
                            if (pair.Key.IsChildOf(this._selectedWorkspaceObject.transformTarget) == false && pair.Key != this._selectedWorkspaceObject.transformTarget)
                                continue;
                            switch (this._selectedShowNodeType)
                            {
                                case SimpleListShowNodeType.IK:
                                    if (pair.Value.enablePos == false)
                                        continue;
                                    break;
                                case SimpleListShowNodeType.FK:
                                    if (pair.Value.enablePos)
                                        continue;
                                    break;
                            }
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
                            {
                                GuideObjectManager.Instance.selectObject = pair.Value;
                                this._selectedBone = pair.Value.transformTarget;
                            }
                            GUI.color = c;
                        }
                    }
                    GUILayout.EndScrollView();
                }
                else
                {
                    this._advancedModeScroll = GUILayout.BeginScrollView(this._advancedModeScroll, false, false, GUI.skin.horizontalScrollbar, GUI.skin.verticalScrollbar, GUI.skin.box);
                    if (this._selectedWorkspaceObject != null)
                        foreach (Transform t in this._selectedWorkspaceObject.transformTarget)
                            this.DisplayObjectTree(t.gameObject, 0);
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

        private void UpdateDisplayedPositionOffset()
        {
            this._positionXStr = this._displayedConstraint.positionOffset.x.ToString("0.000");
            this._positionYStr = this._displayedConstraint.positionOffset.y.ToString("0.000");
            this._positionZStr = this._displayedConstraint.positionOffset.z.ToString("0.000");
        }

        private void ValidateDisplayedPositionOffset()
        {
            float res;
            if (float.TryParse(this._positionXStr, out res))
                this._displayedConstraint.positionOffset.x = res;
            if (float.TryParse(this._positionYStr, out res))
                this._displayedConstraint.positionOffset.y = res;
            if (float.TryParse(this._positionZStr, out res))
                this._displayedConstraint.positionOffset.z = res;
            this.UpdateDisplayedPositionOffset();
        }

        private void UpdateDisplayedRotationOffset()
        {
            Vector3 euler = this._displayedConstraint.rotationOffset.eulerAngles;
            this._rotationXStr = euler.x.ToString("0.00");
            this._rotationYStr = euler.y.ToString("0.00");
            this._rotationZStr = euler.z.ToString("0.00");
        }

        private void ValidateDisplayedRotationOffset()
        {
            float resX;
            float resY;
            float resZ;
            if (!float.TryParse(this._rotationXStr, out resX))
                resX = this._displayedConstraint.rotationOffset.eulerAngles.x;
            if (!float.TryParse(this._rotationYStr, out resY))
                resY = this._displayedConstraint.rotationOffset.eulerAngles.y;
            if (!float.TryParse(this._rotationZStr, out resZ))
                resZ = this._displayedConstraint.rotationOffset.eulerAngles.z;

            this._displayedConstraint.rotationOffset = Quaternion.Euler(resX, resY, resZ);
            this.UpdateDisplayedRotationOffset();
        }

        private Constraint AddConstraint(bool enabled, Transform parentTransform, Transform childTransform, bool linkPosition, Vector3 positionOffset, bool linkRotation, Quaternion rotationOffset, string alias)
        {
            bool shouldAdd = true;
            foreach (Constraint constraint in this._constraints)
            {
                if (constraint.parentTransform == parentTransform && constraint.childTransform == childTransform ||
                    constraint.childTransform == parentTransform && constraint.parentTransform == childTransform)
                {
                    shouldAdd = false;
                    break;
                }
            }
            if (shouldAdd)
            {
                Constraint newConstraint = new Constraint();
                newConstraint.enabled = enabled;
                newConstraint.parentTransform = parentTransform;
                newConstraint.childTransform = childTransform;
                newConstraint.position = linkPosition;
                newConstraint.rotation = linkRotation;
                newConstraint.positionOffset = positionOffset;
                newConstraint.rotationOffset = rotationOffset;
                newConstraint.alias = alias;

                if (this._allGuideObjects.TryGetValue(newConstraint.parentTransform, out newConstraint.parent) == false)
                    newConstraint.parent = null;
                if (this._allGuideObjects.TryGetValue(newConstraint.childTransform, out newConstraint.child) == false)
                    newConstraint.child = null;
                newConstraint.originalChildPosition = newConstraint.childTransform.localPosition;
                newConstraint.originalChildRotation = newConstraint.childTransform.localRotation;

                this._constraints.Add(newConstraint);
                return newConstraint;
            }
            return null;
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
#if KOIKATSU
        private void OnSceneLoad(string path)
        {
            if (this._kkAnimationControllerInstalled == false)
                this.ExecuteDelayed(() =>
                {
                    this.LoadDataFromKKAnimationController(Studio.Studio.Instance.dicObjectCtrl);
                }, 2);

            PluginData data = ExtendedSave.GetSceneExtendedDataById(_extSaveKey);
            if (data == null)
                return;
            XmlDocument doc = new XmlDocument();
            doc.LoadXml((string)data.data["constraints"]);
            this.OnSceneLoad(path, doc);
        }

        private void OnSceneImport(string path)
        {
            if (this._kkAnimationControllerInstalled == false)
                this.ExecuteDelayed(() =>
                {
                    this.LoadDataFromKKAnimationController((Dictionary<int, ObjectCtrlInfo>)typeof(StudioSaveLoadApi).GetMethod("GetLoadedObjects", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static).Invoke(null, new object[] {SceneOperationKind.Import}));
                }, 2);

            PluginData data = ExtendedSave.GetSceneExtendedDataById(_extSaveKey);
            if (data == null)
                return;
            XmlDocument doc = new XmlDocument();
            doc.LoadXml((string)data.data["constraints"]);
            this.OnSceneImport(path, doc);
        }

        private void OnSceneSave(string path)
        {
            using (StringWriter stringWriter = new StringWriter())
            using (XmlTextWriter xmlWriter = new XmlTextWriter(stringWriter))
            {
                this.OnSceneSave(path, xmlWriter);

                PluginData data = new PluginData();
                data.version = _saveVersion;
                data.data.Add("constraints", stringWriter.ToString());
                ExtendedSave.SetSceneExtendedDataById(_extSaveKey, data);
            }

        }
#endif

        private void OnSceneLoad(string path, XmlNode node)
        {
            if (node == null)
                return;
            Studio.Studio.Instance.ExecuteDelayed(() =>
            {
                this.LoadSceneGeneric(node.FirstChild, new SortedDictionary<int, ObjectCtrlInfo>(Studio.Studio.Instance.dicObjectCtrl).ToList());
            }, 8);
        }

        private void OnSceneImport(string path, XmlNode node)
        {
            if (node == null)
                return;
            Dictionary<int, ObjectCtrlInfo> toIgnore = new Dictionary<int, ObjectCtrlInfo>(Studio.Studio.Instance.dicObjectCtrl);
            Studio.Studio.Instance.ExecuteDelayed(() =>
            {
                this.LoadSceneGeneric(node.FirstChild, Studio.Studio.Instance.dicObjectCtrl.Where(e => toIgnore.ContainsKey(e.Key) == false).OrderBy(e => SceneInfo_Import_Patches._newToOldKeys[e.Key]).ToList());
            }, 8);
        }

        private void LoadSceneGeneric(XmlNode node, List<KeyValuePair<int, ObjectCtrlInfo>> dic)
        {
            string v = node.Attributes["version"].Value;

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

                this.AddConstraint(
                                   childNode.Attributes["enabled"] == null || XmlConvert.ToBoolean(childNode.Attributes["enabled"].Value),
                                   parentTransform,
                                   childTransform,
                                   XmlConvert.ToBoolean(childNode.Attributes["position"].Value),
                                   new Vector3(
                                               XmlConvert.ToSingle(childNode.Attributes["positionOffsetX"].Value),
                                               XmlConvert.ToSingle(childNode.Attributes["positionOffsetY"].Value),
                                               XmlConvert.ToSingle(childNode.Attributes["positionOffsetZ"].Value)
                                              ),
                                   XmlConvert.ToBoolean(childNode.Attributes["rotation"].Value),
                                   new Quaternion(
                                                  XmlConvert.ToSingle(childNode.Attributes["rotationOffsetX"].Value),
                                                  XmlConvert.ToSingle(childNode.Attributes["rotationOffsetY"].Value),
                                                  XmlConvert.ToSingle(childNode.Attributes["rotationOffsetZ"].Value),
                                                  XmlConvert.ToSingle(childNode.Attributes["rotationOffsetW"].Value)
                                                 ),
                                   childNode.Attributes["alias"] != null ? childNode.Attributes["alias"].Value : ""
                                  );
            }
        }

#if KOIKATSU
        private void LoadDataFromKKAnimationController(Dictionary<int, ObjectCtrlInfo> loadedObjects)
        {
            PluginData v1Data = ExtendedSave.GetSceneExtendedDataById("KK_AnimationController");
            object animationInfo;
            List<AnimationControllerInfo> animationControllerInfoList = null;
            if (v1Data != null && v1Data.data != null && v1Data.data.TryGetValue("AnimationInfo", out animationInfo))
                animationControllerInfoList = ((object[])animationInfo).Select(x => AnimationControllerInfo.Unserialize((byte[])x)).ToList();

            foreach (var kvp in loadedObjects)
            {
                OCIChar ociChar = kvp.Value as OCIChar;
                if (ociChar == null)
                    continue;
                try
                {
                    //Version 1 save data
                    if (animationControllerInfoList != null)
                    {
                        foreach (AnimationControllerInfo animInfo in animationControllerInfoList)
                        {
                            //See if this is the right character
                            if (animInfo.CharDicKey != kvp.Key)
                                continue;

                            ObjectCtrlInfo linkedItem = loadedObjects[animInfo.ItemDicKey];

                            if (!animInfo.Version.IsNullOrEmpty())
                                this.AddLink(ociChar, animInfo.IKPart, linkedItem);
                        }
                        UnityEngine.Debug.Log($"NodesConstraints: Loaded KK_AnimationController animations for character {ociChar.charInfo.chaFile.parameter.fullname.Trim()}");
                    }
                    //Version 2 save data
                    else
                    {
                        PluginData data = ExtendedSave.GetExtendedDataById(ociChar.charInfo.chaFile, "com.deathweasel.bepinex.animationcontroller");
                        if (data != null && data.data != null)
                        {
                            if (data.data.TryGetValue("Links", out object loadedLinks) && loadedLinks != null)
                                foreach (KeyValuePair<object, object> link in (Dictionary<object, object>)loadedLinks)
                                    this.AddLink(ociChar, (string)link.Key, loadedObjects[(int)link.Value]);

                            if (data.data.TryGetValue("Eyes", out var loadedEyeLink) && loadedEyeLink != null)
                                this.AddEyeLink(ociChar, loadedObjects[(int)loadedEyeLink]);

                            UnityEngine.Debug.Log($"NodesConstraints: Loaded KK_AnimationController animations for character {ociChar.charInfo.chaFile.parameter.fullname.Trim()}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError("NodesConstraints: Could not load KK_AnimationController animations.\n" + ex);
                }
            }
        }

        private void AddLink(OCIChar ociChar, string selectedGuideObject, ObjectCtrlInfo selectedObject)
        {
            OCIChar.IKInfo ikInfo = ociChar.listIKTarget.First(x => x.boneObject.name == selectedGuideObject);

            Transform parent = this.GetChildRootFromObjectCtrl(selectedObject);
            Transform child = ikInfo.guideObject.transformTarget;

            if (parent == null || child == null)
                return;
            foreach (Constraint c in this._constraints)
            {
                if (c.parentTransform == parent && c.childTransform == child ||
                    c.childTransform == parent && c.parentTransform == child)
                    return;
            }

            this.AddConstraint(true, parent, child, true, Vector3.zero, true, Quaternion.identity, "");
        }

        private void AddEyeLink(OCIChar ociChar, ObjectCtrlInfo selectedObject)
        {
            Transform parent = selectedObject.guideObject.transformTarget;
            Transform child = ociChar.lookAtInfo.target;

            if (parent == null || child == null)
                return;
            foreach (Constraint c in this._constraints)
            {
                if (c.parentTransform == parent && c.childTransform == child ||
                    c.childTransform == parent && c.parentTransform == child)
                    return;
            }

            Constraint constraint = new Constraint();
            constraint.enabled = true;
            constraint.parentTransform = parent;
            constraint.childTransform = child;

            if (this._allGuideObjects.TryGetValue(constraint.parentTransform, out constraint.parent) == false)
                constraint.parent = null;
            if (this._allGuideObjects.TryGetValue(constraint.childTransform, out constraint.child) == false)
                constraint.child = null;

            constraint.position = true;
            constraint.positionOffset = Vector3.zero;
            constraint.rotation = true;
            constraint.rotationOffset = Quaternion.identity;

            this.AddConstraint(true, parent, child, true, Vector3.zero, true, Quaternion.identity, "");
        }

        private Transform GetChildRootFromObjectCtrl(ObjectCtrlInfo objectCtrlInfo)
        {
            if (objectCtrlInfo == null)
                return null;
            OCIItem ociitem;
            if ((ociitem = (objectCtrlInfo as OCIItem)) != null)
                return ociitem.childRoot;
            OCIFolder ocifolder;
            if ((ocifolder = (objectCtrlInfo as OCIFolder)) != null)
                return ocifolder.childRoot;
            OCIRoute ociroute;
            if ((ociroute = (objectCtrlInfo as OCIRoute)) != null)
                return ociroute.childRoot;
            return null;
        }
#endif

        private void OnSceneSave(string path, XmlTextWriter xmlWriter)
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

                xmlWriter.WriteAttributeString("alias", constraint.alias);

                xmlWriter.WriteEndElement();
            }

            xmlWriter.WriteEndElement();
        }
        #endregion
    }
}
