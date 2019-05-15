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
    public class BonesEditor : AdvancedModeModule
    {

        #region Private Types
        private enum CoordType
        {
            Position,
            Rotation,
            Scale,
            RotateAround
        }

        private class TransformData
        {
            public EditableValue<Vector3> position;
            public EditableValue<Quaternion> rotation;
            public EditableValue<Vector3> scale;
            public EditableValue<Vector3> originalPosition;
            public EditableValue<Quaternion> originalRotation;
            public EditableValue<Vector3> originalScale;

            public TransformData()
            {
            }

            public TransformData(TransformData other)
            {
                this.position = other.position;
                this.rotation = other.rotation;
                this.scale = other.scale;
                this.originalPosition = other.originalPosition;
                this.originalRotation = other.originalRotation;
                this.originalScale = other.originalScale;
            }
        }
        #endregion

        #region Private Variables
        private static int _positionIncIndex = -1;
        private static float _positionInc = 0.1f;
        private static int _rotationIncIndex = 0;
        private static float _rotationInc = 1f;
        private static int _scaleIncIndex = -2;
        private static float _scaleInc = 0.01f;
        private static int _rotateAroundIncIndex = 0;
        private static float _rotateAroundInc = 1f;
        private static string _search = "";
        internal static readonly Dictionary<string, string> _femaleShortcuts = new Dictionary<string, string>();
        internal static readonly Dictionary<string, string> _maleShortcuts = new Dictionary<string, string>();
        internal static readonly Dictionary<string, string> _itemShortcuts = new Dictionary<string, string>();
        internal static readonly Dictionary<string, string> _boneAliases = new Dictionary<string, string>();

        private readonly GenericOCITarget _target;
        private Vector2 _boneEditionScroll;
        private string _currentAlias = "";
        private Transform _boneTarget;
        private Transform _twinBoneTarget;
        private bool _symmetricalEdition = false;
        private CoordType _boneEditionCoordType = CoordType.Rotation;
        private Dictionary<GameObject, TransformData> _dirtyBones = new Dictionary<GameObject, TransformData>();
        private bool _lastShouldSaveValue = false;
        private Vector3 _oldFKRotationValue = Vector3.zero;
        private Vector3 _oldFKTwinRotationValue = Vector3.zero;
        private Vector2 _shortcutsScroll;
        private readonly Dictionary<Transform, string> _boneEditionShortcuts = new Dictionary<Transform, string>();
        private bool _removeShortcutMode;
        private readonly HashSet<GameObject> _openedBones = new HashSet<GameObject>();

        private static readonly List<VectorLine> _cubeDebugLines = new List<VectorLine>();
        #endregion

        #region Public Accessors
        public override AdvancedModeModuleType type { get { return AdvancedModeModuleType.BonesEditor; } }
        public override string displayName { get { return "Bones"; } }
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
        public BonesEditor(PoseController parent, GenericOCITarget target): base(parent)
        {
            this._target = target;
            this._parent.onLateUpdate += this.LateUpdate;
            this._parent.onDisable += this.OnDisable;
            if (_cubeDebugLines.Count == 0)
            {
                float size = 0.012f;
                Vector3 topLeftForward = (Vector3.up + Vector3.left + Vector3.forward) * size,
                    topRightForward = (Vector3.up + Vector3.right + Vector3.forward) * size,
                    bottomLeftForward = ((Vector3.down + Vector3.left + Vector3.forward) * size),
                    bottomRightForward = ((Vector3.down + Vector3.right + Vector3.forward) * size),
                    topLeftBack = (Vector3.up + Vector3.left + Vector3.back) * size,
                    topRightBack = (Vector3.up + Vector3.right + Vector3.back) * size,
                    bottomLeftBack = (Vector3.down + Vector3.left + Vector3.back) * size,
                    bottomRightBack = (Vector3.down + Vector3.right + Vector3.back) * size;
                _cubeDebugLines.Add(VectorLine.SetLine(Color.white, topLeftForward, topRightForward));
                _cubeDebugLines.Add(VectorLine.SetLine(Color.white, topRightForward, bottomRightForward));
                _cubeDebugLines.Add(VectorLine.SetLine(Color.white, bottomRightForward, bottomLeftForward));
                _cubeDebugLines.Add(VectorLine.SetLine(Color.white, bottomLeftForward, topLeftForward));
                _cubeDebugLines.Add(VectorLine.SetLine(Color.white, topLeftBack, topRightBack));
                _cubeDebugLines.Add(VectorLine.SetLine(Color.white, topRightBack, bottomRightBack));
                _cubeDebugLines.Add(VectorLine.SetLine(Color.white, bottomRightBack, bottomLeftBack));
                _cubeDebugLines.Add(VectorLine.SetLine(Color.white, bottomLeftBack, topLeftBack));
                _cubeDebugLines.Add(VectorLine.SetLine(Color.white, topLeftBack, topLeftForward));
                _cubeDebugLines.Add(VectorLine.SetLine(Color.white, topRightBack, topRightForward));
                _cubeDebugLines.Add(VectorLine.SetLine(Color.white, bottomRightBack, bottomRightForward));
                _cubeDebugLines.Add(VectorLine.SetLine(Color.white, bottomLeftBack, bottomLeftForward));

                VectorLine l = VectorLine.SetLine(_redColor, Vector3.zero, Vector3.right * size * 2);
                l.endCap = "vector";
                _cubeDebugLines.Add(l);
                l = VectorLine.SetLine(_greenColor, Vector3.zero, Vector3.up * size * 2);
                l.endCap = "vector";
                _cubeDebugLines.Add(l);
                l = VectorLine.SetLine(_blueColor, Vector3.zero, Vector3.forward * size * 2);
                l.endCap = "vector";
                _cubeDebugLines.Add(l);

                foreach (VectorLine line in _cubeDebugLines)
                {
                    line.lineWidth = 2f;
                    line.active = false;
                }

                MainWindow._self._cameraEventsDispatcher.onPreRender += UpdateGizmosIf;
            }

            if (this._target.type == GenericOCITarget.Type.Character)
            {
#if HONEYSELECT
                if (this._target.isFemale)
                {
                    this._boneEditionShortcuts.Add(this._parent.transform.FindDescendant("cf_J_Hand_s_L"), "L. Hand");
                    this._boneEditionShortcuts.Add(this._parent.transform.FindDescendant("cf_J_Hand_s_R"), "R. Hand");
                    this._boneEditionShortcuts.Add(this._parent.transform.FindDescendant("cf_J_Foot02_L"), "L. Foot");
                    this._boneEditionShortcuts.Add(this._parent.transform.FindDescendant("cf_J_Foot02_R"), "R. Foot");
                    this._boneEditionShortcuts.Add(this._parent.transform.FindDescendant("cf_J_FaceRoot"), "Face");
                }
                else
                {
                    this._boneEditionShortcuts.Add(this._parent.transform.FindDescendant("cm_J_Hand_s_L"), "L. Hand");
                    this._boneEditionShortcuts.Add(this._parent.transform.FindDescendant("cm_J_Hand_s_R"), "R. Hand");
                    this._boneEditionShortcuts.Add(this._parent.transform.FindDescendant("cm_J_Foot02_L"), "L. Foot");
                    this._boneEditionShortcuts.Add(this._parent.transform.FindDescendant("cm_J_Foot02_R"), "R. Foot");
                    this._boneEditionShortcuts.Add(this._parent.transform.FindDescendant("cm_J_FaceRoot"), "Face");
                }
#elif KOIKATSU
                this._boneEditionShortcuts.Add(this._parent.transform.FindDescendant("cf_s_hand_L"), "L. Hand");
                this._boneEditionShortcuts.Add(this._parent.transform.FindDescendant("cf_s_hand_R"), "R. Hand");
                this._boneEditionShortcuts.Add(this._parent.transform.FindDescendant("cf_j_foot_L"), "L. Foot");
                this._boneEditionShortcuts.Add(this._parent.transform.FindDescendant("cf_j_foot_R"), "R. Foot");
                this._boneEditionShortcuts.Add(this._parent.transform.FindDescendant("cf_J_FaceBase"), "Face");
#endif
            }

            //this._parent.StartCoroutine(this.EndOfFrame());
        }

        private void LateUpdate()
        {
            if (this._target.type == GenericOCITarget.Type.Item)
                this.ApplyBoneManualCorrection();
        }

        private void OnDisable()
        {
            if (this._dirtyBones.Count == 0)
                return;
            foreach (KeyValuePair<GameObject, TransformData> kvp in this._dirtyBones)
            {
                if (kvp.Key == null)
                    continue;
                if (kvp.Value.scale.hasValue)
                    kvp.Key.transform.localScale = kvp.Value.originalScale;
                if (kvp.Value.rotation.hasValue)
                    kvp.Key.transform.localRotation = kvp.Value.originalRotation;
                if (kvp.Value.position.hasValue)
                    kvp.Key.transform.localPosition = kvp.Value.originalPosition;
            }
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            this._parent.onLateUpdate -= this.LateUpdate;
            this._parent.onDisable -= this.OnDisable;
        }

        #endregion

        #region Public Methods
        #region Chara Only Methods
        public override void IKExecutionOrderOnPostLateUpdate()
        {
            this.ApplyBoneManualCorrection();
        }
        #endregion

        public override void DrawAdvancedModeChanged()
        {
            UpdateDebugLinesState(this);
        }

        public static void SelectionChanged(BonesEditor self)
        {
            UpdateDebugLinesState(self);
        }

        public Transform GetTwinBone(Transform bone)
        {
            if (bone.name.EndsWith("_L"))
                return this._parent.transform.FindDescendant(bone.name.Substring(0, bone.name.Length - 2) + "_R");
            if (bone.name.EndsWith("_R"))
                return this._parent.transform.FindDescendant(bone.name.Substring(0, bone.name.Length - 2) + "_L");
            if (bone.parent.name.EndsWith("_L"))
                return this._parent.transform.FindDescendant(bone.parent.name.Substring(0, bone.parent.name.Length - 2) + "_R").GetChild(bone.GetSiblingIndex());
            if (bone.parent.name.EndsWith("_R"))
                return this._parent.transform.FindDescendant(bone.parent.name.Substring(0, bone.parent.name.Length - 2) + "_L").GetChild(bone.GetSiblingIndex());
            return null;
        }

        public override void GUILogic()
        {
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical(GUILayout.ExpandWidth(true));
            GUILayout.BeginHorizontal();
            string oldSearch = _search;
            GUILayout.Label("Search", GUILayout.ExpandWidth(false));
            _search = GUILayout.TextField(_search);
            if (GUILayout.Button("X", GUILayout.ExpandWidth(false)))
                _search = "";
            if (oldSearch.Length != 0 && this._boneTarget != null && (_search.Length == 0 || (_search.Length < oldSearch.Length && oldSearch.StartsWith(_search))))
            {
                string displayedName;
                bool aliased = true;
                if (_boneAliases.TryGetValue(this._boneTarget.name, out displayedName) == false)
                {
                    displayedName = this._boneTarget.name;
                    aliased = false;
                }
                if (this._boneTarget.name.IndexOf(oldSearch, StringComparison.OrdinalIgnoreCase) != -1 || (aliased && displayedName.IndexOf(oldSearch, StringComparison.OrdinalIgnoreCase) != -1))
                    this.OpenParents(this._boneTarget.gameObject);
            }
            GUILayout.EndHorizontal();
            this._boneEditionScroll = GUILayout.BeginScrollView(this._boneEditionScroll, GUI.skin.box, GUILayout.ExpandHeight(true));
            foreach (Transform child in this._parent.transform)
            {
                this.DisplayObjectTree(child.gameObject, 0);
            }
            GUILayout.EndScrollView();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Alias", GUILayout.ExpandWidth(false));
            this._currentAlias = GUILayout.TextField(this._currentAlias, GUILayout.ExpandWidth(true));
            if (GUILayout.Button("Save", GUILayout.ExpandWidth(false)))
            {
                if (this._boneTarget != null)
                {
                    this._currentAlias = this._currentAlias.Trim();
                    if (this._currentAlias.Length == 0)
                    {
                        if (_boneAliases.ContainsKey(this._boneTarget.name))
                            _boneAliases.Remove(this._boneTarget.name);
                    }
                    else
                    {
                        if (_boneAliases.ContainsKey(this._boneTarget.name) == false)
                            _boneAliases.Add(this._boneTarget.name, this._currentAlias);
                        else
                            _boneAliases[this._boneTarget.name] = this._currentAlias;
                    }
                }
                else
                    this._currentAlias = "";
            }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Legend:");
            Color co = GUI.color;
            GUI.color = Color.cyan;
            GUILayout.Button("Selected");
            GUI.color = Color.magenta;
            GUILayout.Button("Changed*");
            GUI.color = CollidersEditor._colliderColor;
            GUILayout.Button("Collider");
            GUI.color = co;
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            GUILayout.BeginVertical(GUI.skin.box, GUILayout.MinWidth(350f));
            {
                OCIChar.BoneInfo fkBoneInfo = null;
                if (this._boneTarget != null && this._target.fkEnabled)
                    this._target.fkObjects.TryGetValue(this._boneTarget.gameObject, out fkBoneInfo);
                OCIChar.BoneInfo fkTwinBoneInfo = null;
                if (this._symmetricalEdition && this._twinBoneTarget != null && this._target.fkEnabled)
                    this._target.fkObjects.TryGetValue(this._twinBoneTarget.gameObject, out fkTwinBoneInfo);
                GUILayout.BeginHorizontal(GUI.skin.box);
                TransformData transformData = null;
                if (this._boneTarget != null)
                    this._dirtyBones.TryGetValue(this._boneTarget.gameObject, out transformData);

                if (transformData != null && transformData.position.hasValue)
                    GUI.color = Color.magenta;
                if (this._boneEditionCoordType == CoordType.Position)
                    GUI.color = Color.cyan;
                if (GUILayout.Button("Position" + (transformData != null && transformData.position.hasValue ? "*" : "")))
                    this._boneEditionCoordType = CoordType.Position;
                GUI.color = co;

                if (transformData != null && transformData.rotation.hasValue)
                    GUI.color = Color.magenta;
                if (this._boneEditionCoordType == CoordType.Rotation)
                    GUI.color = Color.cyan;
                if (GUILayout.Button("Rotation" + (transformData != null && transformData.rotation.hasValue ? "*" : "")))
                    this._boneEditionCoordType = CoordType.Rotation;
                GUI.color = co;

                if (transformData != null && transformData.scale.hasValue)
                    GUI.color = Color.magenta;
                if (this._boneEditionCoordType == CoordType.Scale)
                    GUI.color = Color.cyan;
                if (GUILayout.Button("Scale" + (transformData != null && transformData.scale.hasValue ? "*" : "")))
                    this._boneEditionCoordType = CoordType.Scale;
                GUI.color = co;

                if (this._boneEditionCoordType == CoordType.RotateAround)
                    GUI.color = Color.cyan;
                if (GUILayout.Button("Rotate Around"))
                    this._boneEditionCoordType = CoordType.RotateAround;
                GUI.color = co;

                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                GUILayout.BeginVertical();
                switch (this._boneEditionCoordType)
                {
                    case CoordType.Position:
                        Vector3 position = Vector3.zero;
                        if (this._boneTarget != null)
                        {
                            if (this.IsBoneDirty(this._boneTarget.gameObject) && this._dirtyBones[this._boneTarget.gameObject].position.hasValue)
                                position = this._dirtyBones[this._boneTarget.gameObject].position;
                            else
                                position = this._boneTarget.localPosition;
                        }
                        bool shouldSaveValue = false;
                        Color c = GUI.color;
                        GUI.color = AdvancedModeModule._redColor;
                        GUILayout.BeginHorizontal();
                        GUILayout.Label("X:\t" + position.x.ToString("0.00000"));
                        GUILayout.BeginHorizontal(GUILayout.MaxWidth(160f));
                        if (GUILayout.RepeatButton((-_positionInc).ToString("+0.#####;-0.#####")) && this.RepeatControl() && this._boneTarget != null)
                        {
                            shouldSaveValue = true;
                            position -= _positionInc * Vector3.right;
                        }
                        if (GUILayout.RepeatButton(_positionInc.ToString("+0.#####;-0.#####")) && this.RepeatControl() && this._boneTarget != null)
                        {
                            shouldSaveValue = true;
                            position += _positionInc * Vector3.right;
                        }
                        GUILayout.EndHorizontal();
                        GUILayout.EndHorizontal();
                        GUI.color = c;

                        GUI.color = AdvancedModeModule._greenColor;
                        GUILayout.BeginHorizontal();
                        GUILayout.Label("Y:\t" + position.y.ToString("0.00000"));
                        GUILayout.BeginHorizontal(GUILayout.MaxWidth(160f));
                        if (GUILayout.RepeatButton((-_positionInc).ToString("+0.#####;-0.#####")) && this.RepeatControl() && this._boneTarget != null)
                        {
                            shouldSaveValue = true;
                            position -= _positionInc * Vector3.up;
                        }
                        if (GUILayout.RepeatButton(_positionInc.ToString("+0.#####;-0.#####")) && this.RepeatControl() && this._boneTarget != null)
                        {
                            shouldSaveValue = true;
                            position += _positionInc * Vector3.up;
                        }
                        GUILayout.EndHorizontal();
                        GUILayout.EndHorizontal();
                        GUI.color = c;

                        GUI.color = AdvancedModeModule._blueColor;
                        GUILayout.BeginHorizontal();
                        GUILayout.Label("Z:\t" + position.z.ToString("0.00000"));
                        GUILayout.BeginHorizontal(GUILayout.MaxWidth(160f));
                        if (GUILayout.RepeatButton((-_positionInc).ToString("+0.#####;-0.#####")) && this.RepeatControl() && this._boneTarget != null)
                        {
                            shouldSaveValue = true;
                            position -= _positionInc * Vector3.forward;
                        }
                        if (GUILayout.RepeatButton(_positionInc.ToString("+0.#####;-0.#####")) && this.RepeatControl() && this._boneTarget != null)
                        {
                            shouldSaveValue = true;
                            position += _positionInc * Vector3.forward;
                        }
                        GUILayout.EndHorizontal();
                        GUILayout.EndHorizontal();
                        GUI.color = c;
                        if (Event.current.rawType == EventType.Repaint)
                        {
                            if (this._boneTarget != null && shouldSaveValue)
                            {
                                this.SetBoneDirty(this._boneTarget.gameObject);
                                TransformData td = this._dirtyBones[this._boneTarget.gameObject];
                                td.position = position;
                                if (!td.originalPosition.hasValue)
                                    td.originalPosition = this._boneTarget.localPosition;

                                if (this._symmetricalEdition && this._twinBoneTarget != null)
                                {
                                    position.x *= -1f;
                                    this.SetBoneDirty(this._twinBoneTarget.gameObject);
                                    td = this._dirtyBones[this._twinBoneTarget.gameObject];
                                    td.position = position;
                                    if (!td.originalPosition.hasValue)
                                        td.originalPosition = this._twinBoneTarget.localPosition;
                                }
                            }
                            this._lastShouldSaveValue = shouldSaveValue;
                        }
                        break;
                    case CoordType.Rotation:
                        Quaternion rotation = Quaternion.identity;
                        if (this._boneTarget != null)
                        {
                            if (fkBoneInfo != null && fkBoneInfo.active)
                                rotation = fkBoneInfo.guideObject.transformTarget.localRotation;
                            else if (this.IsBoneDirty(this._boneTarget.gameObject) && this._dirtyBones[this._boneTarget.gameObject].rotation.hasValue)
                                rotation = this._dirtyBones[this._boneTarget.gameObject].rotation;
                            else
                                rotation = this._boneTarget.localRotation;
                        }
                        shouldSaveValue = false;
                        c = GUI.color;
                        GUI.color = AdvancedModeModule._redColor;
                        GUILayout.BeginHorizontal();
                        GUILayout.Label("X (Pitch):\t" + rotation.eulerAngles.x.ToString("0.00"));
                        GUILayout.BeginHorizontal(GUILayout.MaxWidth(160f));
                        if (GUILayout.RepeatButton((-_rotationInc).ToString("+0.#####;-0.#####")) && this._boneTarget != null)
                        {
                            shouldSaveValue = true;
                            if (this.RepeatControl())
                                rotation *= Quaternion.AngleAxis(-_rotationInc, Vector3.right);
                        }
                        if (GUILayout.RepeatButton(_rotationInc.ToString("+0.#####;-0.#####")) && this._boneTarget != null)
                        {
                            shouldSaveValue = true;
                            if (this.RepeatControl())
                                rotation *= Quaternion.AngleAxis(_rotationInc, Vector3.right);
                        }
                        GUILayout.EndHorizontal();
                        GUILayout.EndHorizontal();
                        GUI.color = c;

                        GUI.color = AdvancedModeModule._greenColor;
                        GUILayout.BeginHorizontal();
                        GUILayout.Label("Y (Yaw):\t" + rotation.eulerAngles.y.ToString("0.00"));
                        GUILayout.BeginHorizontal(GUILayout.MaxWidth(160f));
                        if (GUILayout.RepeatButton((-_rotationInc).ToString("+0.#####;-0.#####")) && this._boneTarget != null)
                        {
                            shouldSaveValue = true;
                            if (this.RepeatControl())
                                rotation *= Quaternion.AngleAxis(-_rotationInc, Vector3.up);
                        }
                        if (GUILayout.RepeatButton(_rotationInc.ToString("+0.#####;-0.#####")) && this._boneTarget != null)
                        {
                            shouldSaveValue = true;
                            if (this.RepeatControl())
                                rotation *= Quaternion.AngleAxis(_rotationInc, Vector3.up);
                        }
                        GUILayout.EndHorizontal();
                        GUILayout.EndHorizontal();
                        GUI.color = c;

                        GUI.color = AdvancedModeModule._blueColor;
                        GUILayout.BeginHorizontal();
                        GUILayout.Label("Z (Roll):\t" + rotation.eulerAngles.z.ToString("0.00"));
                        GUILayout.BeginHorizontal(GUILayout.MaxWidth(160f));
                        if (GUILayout.RepeatButton((-_rotationInc).ToString("+0.#####;-0.#####")) && this._boneTarget != null)
                        {
                            shouldSaveValue = true;
                            if (this.RepeatControl())
                                rotation *= Quaternion.AngleAxis(-_rotationInc, Vector3.forward);
                        }
                        if (GUILayout.RepeatButton(_rotationInc.ToString("+0.#####;-0.#####")) && this._boneTarget != null)
                        {
                            shouldSaveValue = true;
                            if (this.RepeatControl())
                                rotation *= Quaternion.AngleAxis(_rotationInc, Vector3.forward);
                        }
                        GUILayout.EndHorizontal();
                        GUILayout.EndHorizontal();
                        GUI.color = c;

                        if (Event.current.rawType == EventType.Repaint)
                        {
                            if (this._boneTarget != null)
                            {
                                if (shouldSaveValue)
                                {
                                    Quaternion symmetricalRotation = new Quaternion(-rotation.x, rotation.y, rotation.z, -rotation.w);
                                    if (fkBoneInfo != null)
                                    {
                                        if (this._lastShouldSaveValue == false)
                                            this._oldFKRotationValue = fkBoneInfo.guideObject.changeAmount.rot;
                                        fkBoneInfo.guideObject.changeAmount.rot = rotation.eulerAngles;

                                        if (this._symmetricalEdition && fkTwinBoneInfo != null && fkTwinBoneInfo.active)
                                        {
                                            if (this._lastShouldSaveValue == false)
                                                this._oldFKTwinRotationValue = fkTwinBoneInfo.guideObject.changeAmount.rot;
                                            fkTwinBoneInfo.guideObject.changeAmount.rot = symmetricalRotation.eulerAngles;
                                        }
                                    }

                                    this.SetBoneDirty(this._boneTarget.gameObject);
                                    TransformData td = this._dirtyBones[this._boneTarget.gameObject];
                                    td.rotation = rotation;
                                    if (!td.originalRotation.hasValue)
                                        td.originalRotation = this._boneTarget.localRotation;

                                    if (this._symmetricalEdition && this._twinBoneTarget != null)
                                    {
                                        this.SetBoneDirty(this._twinBoneTarget.gameObject);
                                        td = this._dirtyBones[this._twinBoneTarget.gameObject];
                                        td.rotation = symmetricalRotation;
                                        if (!td.originalRotation.hasValue)
                                            td.originalRotation = this._twinBoneTarget.localRotation;
                                    }
                                }
                                else if (fkBoneInfo != null && this._lastShouldSaveValue)
                                {
                                    GuideCommand.EqualsInfo[] infos;
                                    if (this._symmetricalEdition && fkTwinBoneInfo != null && fkTwinBoneInfo.active)
                                        infos = new GuideCommand.EqualsInfo[2];
                                    else
                                        infos = new GuideCommand.EqualsInfo[1];
                                    infos[0] = new GuideCommand.EqualsInfo()
                                    {
                                        dicKey = fkBoneInfo.guideObject.dicKey,
                                        oldValue = this._oldFKRotationValue,
                                        newValue = fkBoneInfo.guideObject.changeAmount.rot
                                    };
                                    if (this._symmetricalEdition && fkTwinBoneInfo != null && fkTwinBoneInfo.active)
                                        infos[1] = new GuideCommand.EqualsInfo()
                                        {
                                            dicKey = fkTwinBoneInfo.guideObject.dicKey,
                                            oldValue = this._oldFKTwinRotationValue,
                                            newValue = fkTwinBoneInfo.guideObject.changeAmount.rot
                                        };
                                    UndoRedoManager.Instance.Push(new GuideCommand.RotationEqualsCommand(infos));
                                }
                            }
                            this._lastShouldSaveValue = shouldSaveValue;
                        }

                        break;
                    case CoordType.Scale:
                        Vector3 scale = Vector3.one;
                        if (this._boneTarget != null)
                        {
                            if (this.IsBoneDirty(this._boneTarget.gameObject) && this._dirtyBones[this._boneTarget.gameObject].scale.hasValue)
                                scale = this._dirtyBones[this._boneTarget.gameObject].scale;
                            else
                                scale = this._boneTarget.localScale;
                        }
                        shouldSaveValue = false;
                        c = GUI.color;
                        GUI.color = AdvancedModeModule._redColor;
                        GUILayout.BeginHorizontal();
                        GUILayout.Label("X:\t" + scale.x.ToString("0.00000"));
                        GUILayout.BeginHorizontal(GUILayout.MaxWidth(160f));
                        if (GUILayout.RepeatButton((-_scaleInc).ToString("+0.#####;-0.#####")) && this.RepeatControl() && this._boneTarget != null)
                        {
                            shouldSaveValue = true;
                            scale -= _scaleInc * Vector3.right;
                        }
                        if (GUILayout.RepeatButton(_scaleInc.ToString("+0.#####;-0.#####")) && this.RepeatControl() && this._boneTarget != null)
                        {
                            shouldSaveValue = true;
                            scale += _scaleInc * Vector3.right;
                        }
                        GUILayout.EndHorizontal();
                        GUILayout.EndHorizontal();
                        GUI.color = c;

                        GUI.color = AdvancedModeModule._greenColor;
                        GUILayout.BeginHorizontal();
                        GUILayout.Label("Y:\t" + scale.y.ToString("0.00000"));
                        GUILayout.BeginHorizontal(GUILayout.MaxWidth(160f));
                        if (GUILayout.RepeatButton((-_scaleInc).ToString("+0.#####;-0.#####")) && this.RepeatControl() && this._boneTarget != null)
                        {
                            shouldSaveValue = true;
                            scale -= _scaleInc * Vector3.up;
                        }
                        if (GUILayout.RepeatButton(_scaleInc.ToString("+0.#####;-0.#####")) && this.RepeatControl() && this._boneTarget != null)
                        {
                            shouldSaveValue = true;
                            scale += _scaleInc * Vector3.up;
                        }
                        GUILayout.EndHorizontal();
                        GUILayout.EndHorizontal();
                        GUI.color = c;

                        GUI.color = AdvancedModeModule._blueColor;
                        GUILayout.BeginHorizontal();
                        GUILayout.Label("Z:\t" + scale.z.ToString("0.00000"));
                        GUILayout.BeginHorizontal(GUILayout.MaxWidth(160f));
                        if (GUILayout.RepeatButton((-_scaleInc).ToString("+0.#####;-0.#####")) && this.RepeatControl() && this._boneTarget != null)
                        {
                            shouldSaveValue = true;
                            scale -= _scaleInc * Vector3.forward;
                        }
                        if (GUILayout.RepeatButton(_scaleInc.ToString("+0.#####;-0.#####")) && this.RepeatControl() && this._boneTarget != null)
                        {
                            shouldSaveValue = true;
                            scale += _scaleInc * Vector3.forward;
                        }
                        GUILayout.EndHorizontal();
                        GUILayout.EndHorizontal();
                        GUI.color = c;

                        GUILayout.BeginHorizontal();
                        GUILayout.Label("X/Y/Z");
                        GUILayout.BeginHorizontal(GUILayout.MaxWidth(160f));
                        if (GUILayout.RepeatButton((-_scaleInc).ToString("+0.#####;-0.#####")) && this.RepeatControl() && this._boneTarget != null)
                        {
                            shouldSaveValue = true;
                            scale -= _scaleInc * Vector3.one;
                        }
                        if (GUILayout.RepeatButton(_scaleInc.ToString("+0.#####;-0.#####")) && this.RepeatControl() && this._boneTarget != null)
                        {
                            shouldSaveValue = true;
                            scale += _scaleInc * Vector3.one;
                        }
                        GUILayout.EndHorizontal();
                        GUILayout.EndHorizontal();
                        GUI.enabled = true;
                        if (Event.current.rawType == EventType.Repaint)
                        {
                            if (this._boneTarget != null && shouldSaveValue)
                            {
                                this.SetBoneDirty(this._boneTarget.gameObject);
                                TransformData td = this._dirtyBones[this._boneTarget.gameObject];
                                td.scale = scale;
                                if (!td.originalScale.hasValue)
                                    td.originalScale = this._boneTarget.localScale;

                                if (this._symmetricalEdition && this._twinBoneTarget != null)
                                {
                                    this.SetBoneDirty(this._twinBoneTarget.gameObject);
                                    td = this._dirtyBones[this._twinBoneTarget.gameObject];
                                    td.scale = scale;
                                    if (!td.originalScale.hasValue)
                                        td.originalScale = this._twinBoneTarget.localScale;
                                }
                            }
                            this._lastShouldSaveValue = shouldSaveValue;
                        }
                        break;
                    case CoordType.RotateAround:
                        shouldSaveValue = false;
                        Vector3 axis = Vector3.zero;
                        float angle = 0f;
                        c = GUI.color;
                        GUI.color = AdvancedModeModule._redColor;
                        GUILayout.BeginHorizontal();
                        GUILayout.Label("X (Pitch)");
                        GUILayout.BeginHorizontal(GUILayout.MaxWidth(160f));
                        if (GUILayout.RepeatButton((-_rotateAroundInc).ToString("+0.#####;-0.#####")) && this._boneTarget != null)
                        {
                            shouldSaveValue = true;
                            axis = this._boneTarget.right;
                            if (this.RepeatControl())
                                angle = -_rotateAroundInc;
                        }
                        if (GUILayout.RepeatButton(_rotateAroundInc.ToString("+0.#####;-0.#####")) && this._boneTarget != null)
                        {
                            shouldSaveValue = true;
                            axis = this._boneTarget.right;
                            if (this.RepeatControl())
                                angle = _rotateAroundInc;
                        }
                        GUILayout.EndHorizontal();
                        GUILayout.EndHorizontal();
                        GUI.color = c;

                        GUI.color = AdvancedModeModule._greenColor;
                        GUILayout.BeginHorizontal();
                        GUILayout.Label("Y (Yaw)");
                        GUILayout.BeginHorizontal(GUILayout.MaxWidth(160f));
                        if (GUILayout.RepeatButton((-_rotateAroundInc).ToString("+0.#####;-0.#####")) && this._boneTarget != null)
                        {
                            shouldSaveValue = true;
                            axis = this._boneTarget.up;
                            if (this.RepeatControl())
                                angle = -_rotateAroundInc;
                        }
                        if (GUILayout.RepeatButton(_rotateAroundInc.ToString("+0.#####;-0.#####")) && this._boneTarget != null)
                        {
                            shouldSaveValue = true;
                            axis = this._boneTarget.up;
                            if (this.RepeatControl())
                                angle = _rotateAroundInc;
                        }
                        GUILayout.EndHorizontal();
                        GUILayout.EndHorizontal();
                        GUI.color = c;

                        GUI.color = AdvancedModeModule._blueColor;
                        GUILayout.BeginHorizontal();
                        GUILayout.Label("Z (Roll)");
                        GUILayout.BeginHorizontal(GUILayout.MaxWidth(160f));
                        if (GUILayout.RepeatButton((-_rotateAroundInc).ToString("+0.#####;-0.#####")) && this._boneTarget != null)
                        {
                            shouldSaveValue = true;
                            axis = this._boneTarget.forward;
                            if (this.RepeatControl())
                                angle = -_rotateAroundInc;
                        }
                        if (GUILayout.RepeatButton(_rotateAroundInc.ToString("+0.#####;-0.#####")) && this._boneTarget != null)
                        {
                            shouldSaveValue = true;
                            axis = this._boneTarget.forward;
                            if (this.RepeatControl())
                                angle = _rotateAroundInc;
                        }
                        GUILayout.EndHorizontal();
                        GUILayout.EndHorizontal();
                        GUI.color = c;

                        if (Event.current.rawType == EventType.Repaint)
                        {
                            if (this._boneTarget != null)
                            {
                                if (shouldSaveValue)
                                {
                                    Quaternion currentRotation;
                                    if (fkBoneInfo != null && fkBoneInfo.active)
                                        currentRotation = fkBoneInfo.guideObject.transformTarget.localRotation;
                                    else if (this.IsBoneDirty(this._boneTarget.gameObject) && this._dirtyBones[this._boneTarget.gameObject].rotation.hasValue)
                                        currentRotation = this._dirtyBones[this._boneTarget.gameObject].rotation;
                                    else
                                        currentRotation = this._boneTarget.localRotation;

                                    Vector3 currentPosition;
                                    if (this.IsBoneDirty(this._boneTarget.gameObject) && this._dirtyBones[this._boneTarget.gameObject].position.hasValue)
                                        currentPosition = this._dirtyBones[this._boneTarget.gameObject].position;
                                    else
                                        currentPosition = this._boneTarget.localPosition;

                                    this._boneTarget.RotateAround(Studio.Studio.Instance.cameraCtrl.targetPos, axis, angle);

                                    if (fkBoneInfo != null)
                                    {
                                        if (this._lastShouldSaveValue == false)
                                            this._oldFKRotationValue = fkBoneInfo.guideObject.changeAmount.rot;
                                        fkBoneInfo.guideObject.changeAmount.rot = this._boneTarget.localEulerAngles;
                                    }

                                    this.SetBoneDirty(this._boneTarget.gameObject);
                                    TransformData td = this._dirtyBones[this._boneTarget.gameObject];

                                    td.rotation = this._boneTarget.localRotation;
                                    if (!td.originalRotation.hasValue)
                                        td.originalRotation = currentRotation;

                                    td.position = this._boneTarget.localPosition;
                                    if (!td.originalPosition.hasValue)
                                        td.originalPosition = currentPosition;
                                }
                                else if (fkBoneInfo != null && this._lastShouldSaveValue)
                                {
                                    GuideCommand.EqualsInfo[] infos = new GuideCommand.EqualsInfo[1];
                                    infos[0] = new GuideCommand.EqualsInfo()
                                    {
                                        dicKey = fkBoneInfo.guideObject.dicKey,
                                        oldValue = this._oldFKRotationValue,
                                        newValue = fkBoneInfo.guideObject.changeAmount.rot
                                    };
                                    UndoRedoManager.Instance.Push(new GuideCommand.RotationEqualsCommand(infos));
                                }
                            }
                            this._lastShouldSaveValue = shouldSaveValue;
                        }

                        break;
                }
                GUILayout.EndVertical();
                switch (this._boneEditionCoordType)
                {
                    case CoordType.Position:
                        this.IncEditor(ref _positionIncIndex, out _positionInc);
                        break;
                    case CoordType.Rotation:
                        this.IncEditor(ref _rotationIncIndex, out _rotationInc);
                        break;
                    case CoordType.Scale:
                        this.IncEditor(ref _scaleIncIndex, out _scaleInc);
                        break;
                    case CoordType.RotateAround:
                        this.IncEditor(ref _rotateAroundIncIndex, out _rotateAroundInc);
                        break;
                }

                GUILayout.EndHorizontal();
                bool guiEnabled = GUI.enabled;
                GUI.enabled = this._boneEditionCoordType != CoordType.RotateAround;
                this._symmetricalEdition = GUILayout.Toggle(this._symmetricalEdition, "Symmetrical");
                GUI.enabled = guiEnabled;

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Reset Pos.") && this._boneTarget != null)
                    this.ResetBonePos(this._boneTarget, this._twinBoneTarget, Event.current.control);
                if ((fkBoneInfo == null || fkBoneInfo.active == false) && GUILayout.Button("Reset Rot.") && this._boneTarget != null)
                    this.ResetBoneRot(this._boneTarget, this._twinBoneTarget, Event.current.control);
                if (GUILayout.Button("Reset Scale") && this._boneTarget != null)
                    this.ResetBoneScale(this._boneTarget, this._twinBoneTarget, Event.current.control);

                if (GUILayout.Button("Default") && this._boneTarget != null)
                {
                    this.SetBoneDirty(this._boneTarget.gameObject);
                    TransformData td = this._dirtyBones[this._boneTarget.gameObject];
                    td.position = Vector3.zero;
                    if (!td.originalPosition.hasValue)
                        td.originalPosition = this._boneTarget.localPosition;

                    Quaternion symmetricalRotation = Quaternion.identity;
                    if (fkBoneInfo != null)
                    {
                        if (this._lastShouldSaveValue == false)
                            this._oldFKRotationValue = fkBoneInfo.guideObject.changeAmount.rot;
                        fkBoneInfo.guideObject.changeAmount.rot = Vector3.zero;

                        if (this._symmetricalEdition && fkTwinBoneInfo != null && fkTwinBoneInfo.active)
                        {
                            if (this._lastShouldSaveValue == false)
                                this._oldFKTwinRotationValue = fkTwinBoneInfo.guideObject.changeAmount.rot;
                            fkTwinBoneInfo.guideObject.changeAmount.rot = symmetricalRotation.eulerAngles;
                        }
                    }

                    td.rotation = Quaternion.identity;
                    if (!td.originalRotation.hasValue)
                        td.originalRotation = this._boneTarget.localRotation;

                    td.scale = Vector3.one;
                    if (!td.originalScale.hasValue)
                        td.originalScale = this._boneTarget.localScale;

                    if (this._symmetricalEdition && this._twinBoneTarget != null)
                    {
                        this.SetBoneDirty(this._twinBoneTarget.gameObject);
                        td = this._dirtyBones[this._twinBoneTarget.gameObject];
                        td.position = Vector3.zero;
                        if (!td.originalPosition.hasValue)
                            td.originalPosition = this._twinBoneTarget.localPosition;

                        td.rotation = symmetricalRotation;
                        if (!td.originalRotation.hasValue)
                            td.originalRotation = this._twinBoneTarget.localRotation;

                        td.scale = Vector3.one;
                        if (!td.originalScale.hasValue)
                            td.originalScale = this._twinBoneTarget.localScale;
                    }
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginVertical(GUI.skin.box);

                GUILayout.BeginHorizontal();

                Dictionary<string, string> customShortcuts = this._target.type == GenericOCITarget.Type.Character ? (this._target.isFemale ? _femaleShortcuts : _maleShortcuts) : _itemShortcuts;

                GUIStyle style = GUI.skin.GetStyle("Label");
                TextAnchor bak = style.alignment;
                style.alignment = TextAnchor.MiddleCenter;
                GUILayout.Label("Shortcuts", style);
                style.alignment = bak;

                if (GUILayout.Button("+ Add Shortcut") && this._boneTarget != null)
                {
                    string path = this._boneTarget.GetPathFrom(this._parent.transform);
                    if (path.Length != 0)
                    {
                        if (customShortcuts.ContainsKey(path) == false)
                            customShortcuts.Add(path, this._boneTarget.name);
                    }
                    this._removeShortcutMode = false;
                }

                Color color = GUI.color;
                if (this._removeShortcutMode)
                    GUI.color = AdvancedModeModule._redColor;
                if (GUILayout.Button(this._removeShortcutMode ? "Click on a shortcut" : "- Remove Shortcut"))
                    this._removeShortcutMode = !this._removeShortcutMode;
                GUI.color = color;

                GUILayout.EndHorizontal();

                this._shortcutsScroll = GUILayout.BeginScrollView(this._shortcutsScroll);

                GUILayout.BeginHorizontal();
                GUILayout.BeginVertical();

                int i = 0;
                int half = (this._boneEditionShortcuts.Count + customShortcuts.Count(e => this._parent.transform.Find(e.Key) != null)) / 2;
                foreach (KeyValuePair<Transform, string> kvp in this._boneEditionShortcuts)
                {
                    if (i == half)
                    {
                        GUILayout.EndVertical();
                        GUILayout.BeginVertical();
                    }

                    if (GUILayout.Button(kvp.Value))
                        this.GoToObject(kvp.Key.gameObject);
                    ++i;
                }
                string toRemove = null;
                foreach (KeyValuePair<string, string> kvp in customShortcuts)
                {
                    Transform shortcut = this._parent.transform.Find(kvp.Key);
                    if (shortcut == null)
                        continue;

                    if (i == half)
                    {
                        GUILayout.EndVertical();
                        GUILayout.BeginVertical();
                    }

                    string sName = kvp.Value;
                    string newName;
                    if (_boneAliases.TryGetValue(sName, out newName))
                        sName = newName;
                    if (GUILayout.Button(sName))
                    {
                        if (this._removeShortcutMode)
                        {
                            toRemove = kvp.Key;
                            this._removeShortcutMode = false;
                        }
                        else
                            this.GoToObject(shortcut.gameObject);
                    }
                    ++i;

                }
                if (toRemove != null)
                    customShortcuts.Remove(toRemove);

                GUILayout.EndVertical();
                GUILayout.EndHorizontal();

                GUILayout.EndScrollView();

                GUILayout.EndVertical();
            }
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }

        private void ChangeBoneTarget(Transform newTarget)
        {
            this._boneTarget = newTarget;
            this._currentAlias = _boneAliases.ContainsKey(this._boneTarget.name) ? _boneAliases[this._boneTarget.name] : "";
            this._twinBoneTarget = this.GetTwinBone(newTarget);
            if (this._boneTarget == this._twinBoneTarget)
                this._twinBoneTarget = null;
            UpdateDebugLinesState(this);
        }

        public void LoadFrom(BonesEditor other)
        {
            MainWindow._self.ExecuteDelayed(() =>
            {
                foreach (GameObject openedBone in other._openedBones)
                {
                    Transform obj = this._parent.transform.Find(openedBone.transform.GetPathFrom(other._parent.transform));
                    if (obj != null)
                        this._openedBones.Add(obj.gameObject);
                }
                foreach (KeyValuePair<GameObject, TransformData> kvp in other._dirtyBones)
                {
                    Transform obj = this._parent.transform.Find(kvp.Key.transform.GetPathFrom(other._parent.transform));
                    if (obj != null)
                        this._dirtyBones.Add(obj.gameObject, new TransformData(kvp.Value));
                }
                this._boneEditionScroll = other._boneEditionScroll;
                this._shortcutsScroll = other._shortcutsScroll;
            }, 2);
        }

        public override int SaveXml(XmlTextWriter xmlWriter)
        {
            int written = 0;
            if (this._dirtyBones.Count != 0)
            {
                xmlWriter.WriteStartElement("advancedObjects");
                foreach (KeyValuePair<GameObject, TransformData> kvp in this._dirtyBones)
                {
                    if (kvp.Key == null)
                        continue;
                    Transform t = kvp.Key.transform.parent;
                    string n = kvp.Key.transform.name;
                    while (t != this._parent.transform)
                    {
                        n = t.name + "/" + n;
                        t = t.parent;
                    }
                    xmlWriter.WriteStartElement("object");
                    xmlWriter.WriteAttributeString("name", n);

                    if (kvp.Value.position.hasValue)
                    {
                        xmlWriter.WriteAttributeString("posX", XmlConvert.ToString(kvp.Value.position.value.x));
                        xmlWriter.WriteAttributeString("posY", XmlConvert.ToString(kvp.Value.position.value.y));
                        xmlWriter.WriteAttributeString("posZ", XmlConvert.ToString(kvp.Value.position.value.z));
                    }
                    OCIChar.BoneInfo info;
                    if (kvp.Value.rotation.hasValue && (!this._target.fkEnabled || !this._target.fkObjects.TryGetValue(kvp.Key, out info) || info.active == false))
                    {
                        xmlWriter.WriteAttributeString("rotW", XmlConvert.ToString(kvp.Value.rotation.value.w));
                        xmlWriter.WriteAttributeString("rotX", XmlConvert.ToString(kvp.Value.rotation.value.x));
                        xmlWriter.WriteAttributeString("rotY", XmlConvert.ToString(kvp.Value.rotation.value.y));
                        xmlWriter.WriteAttributeString("rotZ", XmlConvert.ToString(kvp.Value.rotation.value.z));
                    }

                    if (kvp.Value.scale.hasValue)
                    {
                        xmlWriter.WriteAttributeString("scaleX", XmlConvert.ToString(kvp.Value.scale.value.x));
                        xmlWriter.WriteAttributeString("scaleY", XmlConvert.ToString(kvp.Value.scale.value.y));
                        xmlWriter.WriteAttributeString("scaleZ", XmlConvert.ToString(kvp.Value.scale.value.z));
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
            XmlNode objects = xmlNode.FindChildNode("advancedObjects");

            if (objects != null)
            {
                foreach (XmlNode node in objects.ChildNodes)
                {
                    if (node.Name != "object")
                        continue;
                    string name = node.Attributes["name"].Value;

                    GameObject obj = this._parent.transform.Find(name).gameObject;
                    TransformData data = new TransformData();
                    if (node.Attributes["posX"] != null && node.Attributes["posY"] != null && node.Attributes["posZ"] != null)
                    {
                        Vector3 pos;
                        pos.x = XmlConvert.ToSingle(node.Attributes["posX"].Value);
                        pos.y = XmlConvert.ToSingle(node.Attributes["posY"].Value);
                        pos.z = XmlConvert.ToSingle(node.Attributes["posZ"].Value);
                        data.position = pos;
                        data.originalPosition = obj.transform.localPosition;
                    }
                    if (node.Attributes["rotW"] != null && node.Attributes["rotX"] != null && node.Attributes["rotY"] != null && node.Attributes["rotZ"] != null)
                    {
                        Quaternion rot;
                        rot.w = XmlConvert.ToSingle(node.Attributes["rotW"].Value);
                        rot.x = XmlConvert.ToSingle(node.Attributes["rotX"].Value);
                        rot.y = XmlConvert.ToSingle(node.Attributes["rotY"].Value);
                        rot.z = XmlConvert.ToSingle(node.Attributes["rotZ"].Value);
                        data.rotation = rot;
                        data.originalRotation = obj.transform.localRotation;
                    }
                    if (node.Attributes["scaleX"] != null && node.Attributes["scaleY"] != null && node.Attributes["scaleZ"] != null)
                    {
                        Vector3 scale;
                        scale.x = XmlConvert.ToSingle(node.Attributes["scaleX"].Value);
                        scale.y = XmlConvert.ToSingle(node.Attributes["scaleY"].Value);
                        scale.z = XmlConvert.ToSingle(node.Attributes["scaleZ"].Value);
                        data.scale = scale;
                        data.originalScale = obj.transform.localScale;
                    }

                    if (data.position.hasValue || data.rotation.hasValue || data.scale.hasValue)
                    {
                        changed = true;
                        this._dirtyBones.Add(obj, data);
                    }
                }
            }
            return changed;
        }
        #endregion

        #region Private Methods
        private static void UpdateGizmosIf()
        {
            if (PoseController._drawAdvancedMode && MainWindow._self._poseTarget != null && MainWindow._self._poseTarget._bonesEditor._isEnabled && MainWindow._self._poseTarget._bonesEditor._boneTarget != null)
            {
                MainWindow._self._poseTarget._bonesEditor.UpdateGizmos();
                MainWindow._self._poseTarget._bonesEditor.DrawGizmos();                
            }
        }

        private void ResetAll()
        {
            this.ResetBonePos(this._parent.transform, null, true);
            this.ResetBoneRot(this._parent.transform, null, true);
            this.ResetBoneScale(this._parent.transform, null, true);
        }

        private void ResetBonePos(Transform bone, Transform twinBone = null, bool withChildren = false)
        {
            TransformData data;
            if (this._dirtyBones.TryGetValue(bone.gameObject, out data))
            {
                data.position.Reset();
                this.SetBoneNotDirtyIf(bone.gameObject);
            }
            if (this._symmetricalEdition && twinBone != null && this._dirtyBones.TryGetValue(twinBone.gameObject, out data))
            {
                data.position.Reset();
                this.SetBoneNotDirtyIf(twinBone.gameObject);
            }
            if (withChildren)
            {
                foreach (KeyValuePair<GameObject, TransformData> pair in new Dictionary<GameObject, TransformData>(this._dirtyBones))
                {
                    if (pair.Key.transform.IsChildOf(bone))
                    {
                        Transform childBone = pair.Key.transform;
                        Transform twinChildBone = this.GetTwinBone(childBone);
                        if (twinChildBone == childBone)
                            twinChildBone = null;
                        this.ResetBonePos(childBone, twinChildBone, false);
                    }
                }
            }
        }

        private void ResetBoneRot(Transform bone, Transform twinBone = null, bool withChildren = false)
        {
            TransformData data;
            OCIChar.BoneInfo info;
            if ((!this._target.fkEnabled || !this._target.fkObjects.TryGetValue(bone.gameObject, out info) || !info.active) && this._dirtyBones.TryGetValue(bone.gameObject, out data))
            {
                data.rotation.Reset();
                this.SetBoneNotDirtyIf(bone.gameObject);
            }
            if (this._symmetricalEdition && twinBone != null && (!this._target.fkEnabled || !this._target.fkObjects.TryGetValue(twinBone.gameObject, out info) || !info.active) && this._dirtyBones.TryGetValue(twinBone.gameObject, out data))
            {
                data.rotation.Reset();
                this.SetBoneNotDirtyIf(twinBone.gameObject);
            }
            if (withChildren)
            {
                foreach (KeyValuePair<GameObject, TransformData> pair in new Dictionary<GameObject, TransformData>(this._dirtyBones))
                {
                    if (pair.Key.transform.IsChildOf(bone))
                    {
                        Transform childBone = pair.Key.transform;
                        Transform twinChildBone = this.GetTwinBone(childBone);
                        if (twinChildBone == childBone)
                            twinChildBone = null;
                        this.ResetBoneRot(childBone, twinChildBone, false);
                    }
                }
            }
        }

        private void ResetBoneScale(Transform bone, Transform twinBone, bool withChildren = false)
        {
            TransformData data;
            if (this._dirtyBones.TryGetValue(bone.gameObject, out data))
            {
                data.scale.Reset();
                this.SetBoneNotDirtyIf(bone.gameObject);
            }
            if (this._symmetricalEdition && twinBone != null && this._dirtyBones.TryGetValue(twinBone.gameObject, out data))
            {
                data.scale.Reset();
                this.SetBoneNotDirtyIf(twinBone.gameObject);
            }
            if (withChildren)
            {
                foreach (KeyValuePair<GameObject, TransformData> pair in new Dictionary<GameObject, TransformData>(this._dirtyBones))
                {
                    if (!pair.Key.transform.IsChildOf(bone))
                        continue;
                    Transform childBone = pair.Key.transform;
                    Transform twinChildBone = this.GetTwinBone(childBone);
                    if (twinChildBone == childBone)
                        twinChildBone = null;
                    this.ResetBoneScale(childBone, twinChildBone, false);
                }
            }
        }

        private void DisplayObjectTree(GameObject go, int indent)
        {
            if (this._parent._childObjects.Contains(go))
                return;
            string displayedName;
            bool aliased = true;
            if (_boneAliases.TryGetValue(go.name, out displayedName) == false)
            {
                displayedName = go.name;
                aliased = false;
            }

            if (_search.Length == 0 || go.name.IndexOf(_search, StringComparison.OrdinalIgnoreCase) != -1 || (aliased && displayedName.IndexOf(_search, StringComparison.OrdinalIgnoreCase) != -1))
            {
                Color c = GUI.color;
                if (this._dirtyBones.ContainsKey(go))
                    GUI.color = Color.magenta;
                if (this._parent._collidersEditor._colliders.ContainsKey(go.transform))
                    GUI.color = CollidersEditor._colliderColor;
                if (this._boneTarget == go.transform)
                    GUI.color = Color.cyan;
                GUILayout.BeginHorizontal();
                if (_search.Length == 0)
                {
                    GUILayout.Space(indent * 20f);
                    int childCount = 0;
                    for (int i = 0; i < go.transform.childCount; ++i)
                        if (this._parent._childObjects.Contains(go.transform.GetChild(i).gameObject) == false)
                            ++childCount;
                    if (childCount != 0)
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
                if (GUILayout.Button(displayedName + (this.IsBoneDirty(go) ? "*" : ""), GUILayout.ExpandWidth(false)))
                {
                    this.ChangeBoneTarget(go.transform);
                }
                GUI.color = c;
                GUILayout.EndHorizontal();
            }
            if (_search.Length != 0 || this._openedBones.Contains(go))
                for (int i = 0; i < go.transform.childCount; ++i)
                    this.DisplayObjectTree(go.transform.GetChild(i).gameObject, indent + 1);
        }

        private void SetBoneDirty(GameObject go)
        {
            if (!this.IsBoneDirty(go))
                this._dirtyBones.Add(go, new TransformData());
        }

        private bool IsBoneDirty(GameObject go)
        {
            return (this._dirtyBones.ContainsKey(go));
        }

        private void SetBoneNotDirtyIf(GameObject go)
        {
            if (this.IsBoneDirty(go))
            {
                TransformData data = this._dirtyBones[go];
                if (data.position.hasValue == false && data.originalPosition.hasValue)
                {
                    go.transform.localPosition = data.originalPosition;
                    data.originalPosition.Reset();
                }
                if (data.rotation.hasValue == false && data.originalRotation.hasValue)
                {
                    go.transform.localRotation = data.originalRotation;
                    data.originalRotation.Reset();
                }
                if (data.scale.hasValue == false && data.originalScale.hasValue)
                {
                    go.transform.localScale = data.originalScale;
                    data.originalScale.Reset();
                }
                if (data.position.hasValue == false && data.rotation.hasValue == false && data.scale.hasValue == false)
                {
                    this._dirtyBones.Remove(go);
                }
            }
        }

        private void GoToObject(GameObject go)
        {
            if (ReferenceEquals(go, this._parent.transform.gameObject))
                return;
            GameObject goBak = go;
            this.ChangeBoneTarget(go.transform);
            this.OpenParents(go);
            Vector2 scroll = new Vector2(0f, -GUI.skin.button.CalcHeight(new GUIContent("a"), 100f) - 4);
            this.GetScrollPosition(this._parent.transform.gameObject, goBak, 0, ref scroll);
            scroll.y -= GUI.skin.button.CalcHeight(new GUIContent("a"), 100f) + 4;
            this._boneEditionScroll = scroll;
        }

        private void OpenParents(GameObject child)
        {
            if (ReferenceEquals(child, this._parent.transform.gameObject))
                return;
            child = child.transform.parent.gameObject;
            while (child.transform != this._parent.transform)
            {
                this._openedBones.Add(child);
                child = child.transform.parent.gameObject;
            }
            this._openedBones.Add(child);
        }

        private bool GetScrollPosition(GameObject root, GameObject go, int indent, ref Vector2 scrollPosition)
        {
            if (this._parent._childObjects.Contains(go))
                return false;
            scrollPosition = new Vector2(indent * 20f, scrollPosition.y + GUI.skin.button.CalcHeight(new GUIContent("a"), 100f) + 4);
            if (ReferenceEquals(root, go))
                return true;
            if (this._openedBones.Contains(root))
                for (int i = 0; i < root.transform.childCount; ++i)
                    if (this.GetScrollPosition(root.transform.GetChild(i).gameObject, go, indent + 1, ref scrollPosition))
                        return true;
            return false;
        }

        private void ApplyBoneManualCorrection()
        {
            if (this._dirtyBones.Count == 0)
                return;
            bool shouldClean = false;
            foreach (KeyValuePair<GameObject, TransformData> kvp in this._dirtyBones)
            {
                if (kvp.Key == null)
                {
                    shouldClean = true;
                    continue;
                }
                if (kvp.Value.scale.hasValue)
                    kvp.Key.transform.localScale = kvp.Value.scale;
                if (kvp.Value.rotation.hasValue)
                    kvp.Key.transform.localRotation = kvp.Value.rotation;
                if (kvp.Value.position.hasValue)
                    kvp.Key.transform.localPosition = kvp.Value.position;
            }
            if (shouldClean)
            {
                Dictionary<GameObject, TransformData> newDirtyBones = new Dictionary<GameObject, TransformData>();
                foreach (KeyValuePair<GameObject, TransformData> kvp in this._dirtyBones)
                    if (kvp.Key != null)
                        newDirtyBones.Add(kvp.Key, kvp.Value);
                this._dirtyBones = newDirtyBones;
            }
        }

        private void UpdateGizmos()
        {
            //if (!this._isEnabled || !PoseController._drawAdvancedMode || this._boneTarget == null || MainWindow._self._poseTarget != this._parent)
            //    return;
            float size = 0.012f;
            Vector3 topLeftForward = this._boneTarget.transform.position + (this._boneTarget.up + -this._boneTarget.right + this._boneTarget.forward) * size,
                topRightForward = this._boneTarget.transform.position + (this._boneTarget.up + this._boneTarget.right + this._boneTarget.forward) * size,
                bottomLeftForward = this._boneTarget.transform.position + ((-this._boneTarget.up + -this._boneTarget.right + this._boneTarget.forward) * size),
                bottomRightForward = this._boneTarget.transform.position + ((-this._boneTarget.up + this._boneTarget.right + this._boneTarget.forward) * size),
                topLeftBack = this._boneTarget.transform.position + (this._boneTarget.up + -this._boneTarget.right + -this._boneTarget.forward) * size,
                topRightBack = this._boneTarget.transform.position + (this._boneTarget.up + this._boneTarget.right + -this._boneTarget.forward) * size,
                bottomLeftBack = this._boneTarget.transform.position + (-this._boneTarget.up + -this._boneTarget.right + -this._boneTarget.forward) * size,
                bottomRightBack = this._boneTarget.transform.position + (-this._boneTarget.up + this._boneTarget.right + -this._boneTarget.forward) * size;
            int i = 0;
            _cubeDebugLines[i++].SetPoints(topLeftForward, topRightForward);
            _cubeDebugLines[i++].SetPoints(topRightForward, bottomRightForward);
            _cubeDebugLines[i++].SetPoints(bottomRightForward, bottomLeftForward);
            _cubeDebugLines[i++].SetPoints(bottomLeftForward, topLeftForward);
            _cubeDebugLines[i++].SetPoints(topLeftBack, topRightBack);
            _cubeDebugLines[i++].SetPoints(topRightBack, bottomRightBack);
            _cubeDebugLines[i++].SetPoints(bottomRightBack, bottomLeftBack);
            _cubeDebugLines[i++].SetPoints(bottomLeftBack, topLeftBack);
            _cubeDebugLines[i++].SetPoints(topLeftBack, topLeftForward);
            _cubeDebugLines[i++].SetPoints(topRightBack, topRightForward);
            _cubeDebugLines[i++].SetPoints(bottomRightBack, bottomRightForward);
            _cubeDebugLines[i++].SetPoints(bottomLeftBack, bottomLeftForward);

            _cubeDebugLines[i++].SetPoints(this._boneTarget.transform.position, this._boneTarget.transform.position + this._boneTarget.right * size * 2);
            _cubeDebugLines[i++].SetPoints(this._boneTarget.transform.position, this._boneTarget.transform.position + this._boneTarget.up * size * 2);
            _cubeDebugLines[i++].SetPoints(this._boneTarget.transform.position, this._boneTarget.transform.position + this._boneTarget.forward * size * 2);
        }

        private void DrawGizmos()
        {
            foreach (VectorLine line in _cubeDebugLines)
                line.Draw();
        }

        private static void UpdateDebugLinesState(BonesEditor self)
        {
            bool e = self != null && self._isEnabled && PoseController._drawAdvancedMode && self._boneTarget != null;
            foreach (VectorLine line in _cubeDebugLines)
                line.active = e;
        }
        #endregion
    }
}