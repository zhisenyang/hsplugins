using System.Collections.Generic;
using System.Xml;
using Studio;
using UnityEngine;
using Vectrosity;

namespace HSPE.AMModules
{
    public class BonesEditor : AdvancedModeModule
    {
        #region Constants
        private static readonly Color _colliderColor = Color.Lerp(AdvancedModeModule._greenColor, Color.white, 0.5f);
        #endregion

        #region Private Types
        private struct ColliderDebugLines
        {
            public List<VectorLine> centerCircles;
            public List<VectorLine> capsCircles;
            public List<VectorLine> centerLines;
            public List<VectorLine> capsLines;

            public void Init()
            {
                {

                    const float radius = 1f;
                    const float num = 2f * 0.5f - 1f;
                    {
                        Vector3 position1 = Vector3.zero;
                        Vector3 position2 = Vector3.zero;
                        position1.x -= num;
                        position2.x += num;
                        Quaternion orientation = Quaternion.AngleAxis(90f, Vector3.up);
                        Vector3 dir = Vector3.right;
                        this.centerCircles = new List<VectorLine>();
                        for (int i = 1; i < 10; ++i)
                        {
                            VectorLine circle = VectorLine.SetLine(BonesEditor._colliderColor, new Vector3[37]);
                            circle.MakeCircle(Vector3.Lerp(position1, position2, i / 10f), dir, radius);
                            this.centerCircles.Add(circle);
                        }
                        this.centerLines = new List<VectorLine>();
                        for (int i = 0; i < 8; ++i)
                        {
                            float angle = 360 * (i / 8f) * Mathf.Deg2Rad;
                            Vector3 offset = orientation * (new Vector3(Mathf.Cos(angle), Mathf.Sin(angle))) * radius;
                            this.centerLines.Add(VectorLine.SetLine(BonesEditor._colliderColor, position1 + offset, position2 + offset));
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
                            VectorLine circle = VectorLine.SetLine(BonesEditor._colliderColor, new Vector3[37]);
                            circle.MakeCircle(center1, dir, radius2);
                            this.capsCircles.Add(circle);

                            circle = VectorLine.SetLine(BonesEditor._colliderColor, new Vector3[37]);
                            circle.MakeCircle(center2, dir, radius2);
                            this.capsCircles.Add(circle);

                            if (i != 0)
                                for (int j = 0; j < 8; ++j)
                                {
                                    float angle2 = 360 * (j / 8f) * Mathf.Deg2Rad;
                                    Vector3 offset = orientation * (new Vector3(Mathf.Cos(angle2), Mathf.Sin(angle2))) * radius2;
                                    this.capsLines.Add(VectorLine.SetLine(BonesEditor._colliderColor, prevCenter1 + prev[j], center1 + offset));
                                    this.capsLines.Add(VectorLine.SetLine(BonesEditor._colliderColor, prevCenter2 + prev[j], center2 + offset));
                                    prev[j] = offset;
                                }
                            prevCenter1 = center1;
                            prevCenter2 = center2;
                        }
                    }
                }
            }

            public void Draw(DynamicBoneCollider collider)
            {

                float radius = collider.m_Radius * Mathf.Abs(collider.transform.lossyScale.x);
                float num = collider.m_Height * 0.5f - collider.m_Radius;
                {
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
                        this.centerCircles[i].Draw();
                    }
                    for (int i = 0; i < 8; ++i)
                    {
                        float angle = 360 * (i / 8f) * Mathf.Deg2Rad;
                        Vector3 offset = orientation * (new Vector3(Mathf.Cos(angle), Mathf.Sin(angle))) * radius;
                        VectorLine line = this.centerLines[i];
                        line.points3[0] = position1 + offset;
                        line.points3[1] = position2 + offset;
                        line.Draw();
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
                        circle.Draw();

                        circle = this.capsCircles[k++];
                        circle.MakeCircle(center2, dir, radius2);
                        circle.Draw();

                        if (i != 0)
                            for (int j = 0; j < 8; ++j)
                            {
                                float angle2 = 360 * (j / 8f) * Mathf.Deg2Rad;
                                Vector3 offset = orientation * (new Vector3(Mathf.Cos(angle2), Mathf.Sin(angle2))) * radius2;
                                VectorLine line = this.capsLines[l++];
                                line.points3[0] = prevCenter1 + prev[j];
                                line.points3[1] = center1 + offset;
                                line.Draw();

                                line = this.capsLines[l++];
                                line.points3[0] = prevCenter2 + prev[j];
                                line.points3[1] = center2 + offset;
                                line.Draw();

                                prev[j] = offset;
                            }
                        prevCenter1 = center1;
                        prevCenter2 = center2;
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
        }

        private enum CoordType
        {
            Position,
            Rotation,
            Scale
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
        private Vector2 _boneEditionScroll;
        private string _currentAlias = "";
        private Transform _boneTarget;
        private Transform _twinBoneTarget;
        private readonly Dictionary<GameObject, OCIChar.BoneInfo> _fkObjects = new Dictionary<GameObject, OCIChar.BoneInfo>();
        private bool _symmetricalEdition = false;
        private CoordType _boneEditionCoordType = CoordType.Rotation;
        private Dictionary<GameObject, TransformData> _dirtyBones = new Dictionary<GameObject, TransformData>();
        private bool _lastShouldSaveValue = false;
        private Vector3 _oldFKRotationValue = Vector3.zero;
        private Vector3 _oldFKTwinRotationValue = Vector3.zero;
        private Vector2 _shortcutsScroll;
        private readonly Dictionary<Transform, string> _boneEditionShortcuts = new Dictionary<Transform, string>();
        private bool _removeShortcutMode;
        private bool _isFemale = false;
        private readonly HashSet<GameObject> _ignoredObjects = new HashSet<GameObject>();
        private readonly HashSet<Transform> _colliderObjects = new HashSet<Transform>();
        private readonly HashSet<GameObject> _openedBones = new HashSet<GameObject>();

        private DynamicBoneCollider _colliderTarget;
        private Rect _colliderEditRect = new Rect(Screen.width - 650, Screen.height - 690, 450, 300);
        private readonly Dictionary<DynamicBoneCollider, ColliderData> _dirtyColliders = new Dictionary<DynamicBoneCollider, ColliderData>();
        private readonly List<VectorLine> _cubeDebugLines = new List<VectorLine>();
        private ColliderDebugLines _colliderDebugLines;
        #endregion

        #region Public Accessors
        public override AdvancedModeModuleType type { get { return AdvancedModeModuleType.BonesEditor; } }
        public override string displayName { get { return "Bones"; } }
        public OCIChar chara { get; set; }
        public bool colliderEditEnabled { get { return this._colliderTarget != null; } }
        public Rect colliderEditRect { get { return this._colliderEditRect; } }
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
        public Dictionary<GameObject, OCIChar.BoneInfo> fkObjects { get { return this._fkObjects; } }
        #endregion

        #region Unity Methods
        void Awake()
        {
            foreach (DynamicBoneCollider c in this.GetComponentsInChildren<DynamicBoneCollider>(true))
                this._colliderObjects.Add(c.transform);
            MainWindow.self.onParentage += this.OnParentage;

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
                this._cubeDebugLines.Add(VectorLine.SetLine(Color.white, topLeftForward, topRightForward));
                this._cubeDebugLines.Add(VectorLine.SetLine(Color.white, topRightForward, bottomRightForward));
                this._cubeDebugLines.Add(VectorLine.SetLine(Color.white, bottomRightForward, bottomLeftForward));
                this._cubeDebugLines.Add(VectorLine.SetLine(Color.white, bottomLeftForward, topLeftForward));
                this._cubeDebugLines.Add(VectorLine.SetLine(Color.white, topLeftBack, topRightBack));
                this._cubeDebugLines.Add(VectorLine.SetLine(Color.white, topRightBack, bottomRightBack));
                this._cubeDebugLines.Add(VectorLine.SetLine(Color.white, bottomRightBack, bottomLeftBack));
                this._cubeDebugLines.Add(VectorLine.SetLine(Color.white, bottomLeftBack, topLeftBack));
                this._cubeDebugLines.Add(VectorLine.SetLine(Color.white, topLeftBack, topLeftForward));
                this._cubeDebugLines.Add(VectorLine.SetLine(Color.white, topRightBack, topRightForward));
                this._cubeDebugLines.Add(VectorLine.SetLine(Color.white, bottomRightBack, bottomRightForward));
                this._cubeDebugLines.Add(VectorLine.SetLine(Color.white, bottomLeftBack, bottomLeftForward));

                VectorLine l = VectorLine.SetLine(AdvancedModeModule._redColor, Vector3.zero, Vector3.right * size * 2);
                l.endCap = "vector";
                this._cubeDebugLines.Add(l);
                l = VectorLine.SetLine(AdvancedModeModule._greenColor, Vector3.zero, Vector3.up * size * 2);
                l.endCap = "vector";
                this._cubeDebugLines.Add(l);
                l = VectorLine.SetLine(AdvancedModeModule._blueColor, Vector3.zero, Vector3.forward * size * 2);
                l.endCap = "vector";
                this._cubeDebugLines.Add(l);

                foreach (VectorLine line in this._cubeDebugLines)
                {
                    line.lineWidth = 2f;
                    line.active = false;
                }
                this._colliderDebugLines.Init();
                this._colliderDebugLines.SetActive(false);
            }
        }

        void Start()
        {
            this._isFemale = this.chara.charInfo.Sex == 1;

            if (this._isFemale)
            {
                this._boneEditionShortcuts.Add(this.transform.FindDescendant("cf_J_Hand_s_L"), "L. Hand");
                this._boneEditionShortcuts.Add(this.transform.FindDescendant("cf_J_Hand_s_R"), "R. Hand");
                this._boneEditionShortcuts.Add(this.transform.FindDescendant("cf_J_Foot02_L"), "L. Foot");
                this._boneEditionShortcuts.Add(this.transform.FindDescendant("cf_J_Foot02_R"), "R. Foot");
                this._boneEditionShortcuts.Add(this.transform.FindDescendant("cf_J_FaceRoot"), "Face");
            }
            else
            {
                this._boneEditionShortcuts.Add(this.transform.FindDescendant("cm_J_Hand_s_L"), "L. Hand");
                this._boneEditionShortcuts.Add(this.transform.FindDescendant("cm_J_Hand_s_R"), "R. Hand");
                this._boneEditionShortcuts.Add(this.transform.FindDescendant("cm_J_Foot02_L"), "L. Foot");
                this._boneEditionShortcuts.Add(this.transform.FindDescendant("cm_J_Foot02_R"), "R. Foot");
                this._boneEditionShortcuts.Add(this.transform.FindDescendant("cm_J_FaceRoot"), "Face");
            }
            foreach (OCIChar.BoneInfo bone in this.chara.listBones)
            {
                this._fkObjects.Add(bone.guideObject.transformTarget.gameObject, bone);
            }
        }

        void OnGUI()
        {
            GUIUtility.ScaleAroundPivot(Vector2.one * (MainWindow.self.uiScale * MainWindow.self.resolutionRatio), new Vector2(Screen.width, Screen.height));
            if (this._colliderTarget)
            {
                for (int i = 0; i < 3; ++i)
                    GUI.Box(this._colliderEditRect, "");
                this._colliderEditRect = GUILayout.Window(2, this._colliderEditRect, this.ColliderEditor, "Collider Editor" + (this.IsColliderDirty(this._colliderTarget) ? "*" : ""));
            }
            GUIUtility.ScaleAroundPivot(Vector2.one, new Vector2(Screen.width, Screen.height));
        }

        void OnDestroy()
        {
            MainWindow.self.onParentage -= this.OnParentage;
        }
        #endregion

        #region Public Methods
        public override void IKSolverOnPostUpdate()
        {
            if (this.chara.oiCharInfo.enableFK == false)
            {
                this.ApplyBoneManualCorrection();
                this.DrawGizmos();                
            }
        }

        public override void FKCtrlOnPostLateUpdate()
        {
            this.ApplyBoneManualCorrection();
            this.DrawGizmos();
        }

        public override void CharBodyPreLateUpdate()
        {
            if (this.chara.oiCharInfo.enableIK == false && this.chara.oiCharInfo.enableFK == false)
            {
                this.ApplyBoneManualCorrection();
                this.DrawGizmos();                
            }
        }

        public Transform GetTwinBone(Transform bone)
        {
            if (bone.name.EndsWith("_L"))
                return this.transform.FindDescendant(bone.name.Substring(0, bone.name.Length - 2) + "_R");
            if (bone.name.EndsWith("_R"))
                return this.transform.FindDescendant(bone.name.Substring(0, bone.name.Length - 2) + "_L");
            if (bone.parent.name.EndsWith("_L"))
                return this.transform.FindDescendant(bone.parent.name.Substring(0, bone.parent.name.Length - 2) + "_R").GetChild(bone.GetSiblingIndex());
            if (bone.parent.name.EndsWith("_R"))
                return this.transform.FindDescendant(bone.parent.name.Substring(0, bone.parent.name.Length - 2) + "_L").GetChild(bone.GetSiblingIndex());
            return null;
        }


        public override void GUILogic()
        {
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical(GUILayout.ExpandWidth(true));
            GUILayout.Label("Character Tree", GUI.skin.box);
            this._boneEditionScroll = GUILayout.BeginScrollView(this._boneEditionScroll, GUI.skin.box, GUILayout.ExpandHeight(true));
            this.DisplayObjectTree(this.transform.GetChild(0).gameObject, 0);
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
                        if (MainWindow.self.boneAliases.ContainsKey(this._boneTarget.name))
                            MainWindow.self.boneAliases.Remove(this._boneTarget.name);
                    }
                    else
                    {
                        if (MainWindow.self.boneAliases.ContainsKey(this._boneTarget.name) == false)
                            MainWindow.self.boneAliases.Add(this._boneTarget.name, this._currentAlias);
                        else
                            MainWindow.self.boneAliases[this._boneTarget.name] = this._currentAlias;
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
            GUI.color = BonesEditor._colliderColor;
            GUILayout.Button("Collider");
            GUI.color = co;
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            GUILayout.BeginVertical(GUI.skin.box, GUILayout.MinWidth(350f));
            {
                OCIChar.BoneInfo fkBoneInfo = null;
                if (this._boneTarget != null && this.chara.oiCharInfo.enableFK)
                    this._fkObjects.TryGetValue(this._boneTarget.gameObject, out fkBoneInfo);
                OCIChar.BoneInfo fkTwinBoneInfo = null;
                if (this._symmetricalEdition && this._twinBoneTarget != null && this.chara.oiCharInfo.enableFK)
                    this._fkObjects.TryGetValue(this._twinBoneTarget.gameObject, out fkTwinBoneInfo);
                GUILayout.BeginHorizontal(GUI.skin.box);
                if (GUILayout.Toggle(this._boneEditionCoordType == CoordType.Position, "Position"))
                    this._boneEditionCoordType = CoordType.Position;
                if (GUILayout.Toggle(this._boneEditionCoordType == CoordType.Rotation, "Rotation"))
                    this._boneEditionCoordType = CoordType.Rotation;
                if (GUILayout.Toggle(this._boneEditionCoordType == CoordType.Scale, "Scale"))
                    this._boneEditionCoordType = CoordType.Scale;
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
                        if (GUILayout.RepeatButton((-this._inc).ToString("+0.#####;-0.#####")) && this.RepeatControl() && this._boneTarget != null)
                        {
                            shouldSaveValue = true;
                            position -= this._inc * Vector3.right;
                        }
                        if (GUILayout.RepeatButton(this._inc.ToString("+0.#####;-0.#####")) && this.RepeatControl() && this._boneTarget != null)
                        {
                            shouldSaveValue = true;
                            position += this._inc * Vector3.right;
                        }
                        GUILayout.EndHorizontal();
                        GUILayout.EndHorizontal();
                        GUI.color = c;

                        GUI.color = AdvancedModeModule._greenColor;
                        GUILayout.BeginHorizontal();
                        GUILayout.Label("Y:\t" + position.y.ToString("0.00000"));
                        GUILayout.BeginHorizontal(GUILayout.MaxWidth(160f));
                        if (GUILayout.RepeatButton((-this._inc).ToString("+0.#####;-0.#####")) && this.RepeatControl() && this._boneTarget != null)
                        {
                            shouldSaveValue = true;
                            position -= this._inc * Vector3.up;
                        }
                        if (GUILayout.RepeatButton(this._inc.ToString("+0.#####;-0.#####")) && this.RepeatControl() && this._boneTarget != null)
                        {
                            shouldSaveValue = true;
                            position += this._inc * Vector3.up;
                        }
                        GUILayout.EndHorizontal();
                        GUILayout.EndHorizontal();
                        GUI.color = c;

                        GUI.color = AdvancedModeModule._blueColor;
                        GUILayout.BeginHorizontal();
                        GUILayout.Label("Z:\t" + position.z.ToString("0.00000"));
                        GUILayout.BeginHorizontal(GUILayout.MaxWidth(160f));
                        if (GUILayout.RepeatButton((-this._inc).ToString("+0.#####;-0.#####")) && this.RepeatControl() && this._boneTarget != null)
                        {
                            shouldSaveValue = true;
                            position -= this._inc * Vector3.forward;
                        }
                        if (GUILayout.RepeatButton(this._inc.ToString("+0.#####;-0.#####")) && this.RepeatControl() && this._boneTarget != null)
                        {
                            shouldSaveValue = true;
                            position += this._inc * Vector3.forward;
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
                        if (GUILayout.RepeatButton((-this._inc).ToString("+0.#####;-0.#####")) && this._boneTarget != null)
                        {
                            shouldSaveValue = true;
                            if (this.RepeatControl())
                                rotation *= Quaternion.AngleAxis(-this._inc, Vector3.right);
                        }
                        if (GUILayout.RepeatButton(this._inc.ToString("+0.#####;-0.#####")) && this._boneTarget != null)
                        {
                            shouldSaveValue = true;
                            if (this.RepeatControl())
                                rotation *= Quaternion.AngleAxis(this._inc, Vector3.right);
                        }
                        GUILayout.EndHorizontal();
                        GUILayout.EndHorizontal();
                        GUI.color = c;

                        GUI.color = AdvancedModeModule._greenColor;
                        GUILayout.BeginHorizontal();
                        GUILayout.Label("Y (Yaw):\t" + rotation.eulerAngles.y.ToString("0.00"));
                        GUILayout.BeginHorizontal(GUILayout.MaxWidth(160f));
                        if (GUILayout.RepeatButton((-this._inc).ToString("+0.#####;-0.#####")) && this._boneTarget != null)
                        {
                            shouldSaveValue = true;
                            if (this.RepeatControl())
                                rotation *= Quaternion.AngleAxis(-this._inc, Vector3.up);
                        }
                        if (GUILayout.RepeatButton(this._inc.ToString("+0.#####;-0.#####")) && this._boneTarget != null)
                        {
                            shouldSaveValue = true;
                            if (this.RepeatControl())
                                rotation *= Quaternion.AngleAxis(this._inc, Vector3.up);
                        }
                        GUILayout.EndHorizontal();
                        GUILayout.EndHorizontal();
                        GUI.color = c;

                        GUI.color = AdvancedModeModule._blueColor;
                        GUILayout.BeginHorizontal();
                        GUILayout.Label("Z (Roll):\t" + rotation.eulerAngles.z.ToString("0.00"));
                        GUILayout.BeginHorizontal(GUILayout.MaxWidth(160f));
                        if (GUILayout.RepeatButton((-this._inc).ToString("+0.#####;-0.#####")) && this._boneTarget != null)
                        {
                            shouldSaveValue = true;
                            if (this.RepeatControl())
                                rotation *= Quaternion.AngleAxis(-this._inc, Vector3.forward);
                        }
                        if (GUILayout.RepeatButton(this._inc.ToString("+0.#####;-0.#####")) && this._boneTarget != null)
                        {
                            shouldSaveValue = true;
                            if (this.RepeatControl())
                                rotation *= Quaternion.AngleAxis(this._inc, Vector3.forward);
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
                                    if (fkBoneInfo != null && fkBoneInfo.active)
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
                                else if (fkBoneInfo != null && fkBoneInfo.active && this._lastShouldSaveValue)
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
                        if (GUILayout.RepeatButton((-this._inc).ToString("+0.#####;-0.#####")) && this.RepeatControl() && this._boneTarget != null)
                        {
                            shouldSaveValue = true;
                            scale -= this._inc * Vector3.right;
                        }
                        if (GUILayout.RepeatButton(this._inc.ToString("+0.#####;-0.#####")) && this.RepeatControl() && this._boneTarget != null)
                        {
                            shouldSaveValue = true;
                            scale += this._inc * Vector3.right;
                        }
                        GUILayout.EndHorizontal();
                        GUILayout.EndHorizontal();
                        GUI.color = c;

                        GUI.color = AdvancedModeModule._greenColor;
                        GUILayout.BeginHorizontal();
                        GUILayout.Label("Y:\t" + scale.y.ToString("0.00000"));
                        GUILayout.BeginHorizontal(GUILayout.MaxWidth(160f));
                        if (GUILayout.RepeatButton((-this._inc).ToString("+0.#####;-0.#####")) && this.RepeatControl() && this._boneTarget != null)
                        {
                            shouldSaveValue = true;
                            scale -= this._inc * Vector3.up;
                        }
                        if (GUILayout.RepeatButton(this._inc.ToString("+0.#####;-0.#####")) && this.RepeatControl() && this._boneTarget != null)
                        {
                            shouldSaveValue = true;
                            scale += this._inc * Vector3.up;
                        }
                        GUILayout.EndHorizontal();
                        GUILayout.EndHorizontal();
                        GUI.color = c;

                        GUI.color = AdvancedModeModule._blueColor;
                        GUILayout.BeginHorizontal();
                        GUILayout.Label("Z:\t" + scale.z.ToString("0.00000"));
                        GUILayout.BeginHorizontal(GUILayout.MaxWidth(160f));
                        if (GUILayout.RepeatButton((-this._inc).ToString("+0.#####;-0.#####")) && this.RepeatControl() && this._boneTarget != null)
                        {
                            shouldSaveValue = true;
                            scale -= this._inc * Vector3.forward;
                        }
                        if (GUILayout.RepeatButton(this._inc.ToString("+0.#####;-0.#####")) && this.RepeatControl() && this._boneTarget != null)
                        {
                            shouldSaveValue = true;
                            scale += this._inc * Vector3.forward;
                        }
                        GUILayout.EndHorizontal();
                        GUILayout.EndHorizontal();
                        GUI.color = c;

                        GUILayout.BeginHorizontal();
                        GUILayout.Label("X/Y/Z");
                        GUILayout.BeginHorizontal(GUILayout.MaxWidth(160f));
                        if (GUILayout.RepeatButton((-this._inc).ToString("+0.#####;-0.#####")) && this.RepeatControl() && this._boneTarget != null)
                        {
                            shouldSaveValue = true;
                            scale -= this._inc * Vector3.one;
                        }
                        if (GUILayout.RepeatButton(this._inc.ToString("+0.#####;-0.#####")) && this.RepeatControl() && this._boneTarget != null)
                        {
                            shouldSaveValue = true;
                            scale += this._inc * Vector3.one;
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
                }
                GUILayout.EndVertical();

                this.IncEditor();

                GUILayout.EndHorizontal();
                this._symmetricalEdition = GUILayout.Toggle(this._symmetricalEdition, "Symmetrical");

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
                    if (fkBoneInfo != null && fkBoneInfo.active)
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
                this._shortcutsScroll = GUILayout.BeginScrollView(this._shortcutsScroll, false, true, GUILayout.MinWidth(200));
                foreach (KeyValuePair<Transform, string> kvp in this._boneEditionShortcuts)
                    if (GUILayout.Button(kvp.Value))
                        this.GoToObject(kvp.Key.gameObject);

                Dictionary<string, string> customShortcuts = this._isFemale ? MainWindow.self.femaleShortcuts : MainWindow.self.maleShortcuts;
                string toRemove = null;
                foreach (KeyValuePair<string, string> kvp in customShortcuts)
                {
                    string sName = kvp.Value;
                    string newName;
                    if (MainWindow.self.boneAliases.TryGetValue(sName, out newName))
                        sName = newName;
                    if (GUILayout.Button(sName))
                    {
                        if (this._removeShortcutMode)
                        {
                            toRemove = kvp.Key;
                            this._removeShortcutMode = false;
                        }
                        else
                            this.GoToObject(kvp.Key);
                    }
                }
                if (toRemove != null)
                    customShortcuts.Remove(toRemove);
                GUILayout.EndScrollView();
                GUILayout.BeginVertical(GUILayout.ExpandWidth(false));

                GUIStyle style = GUI.skin.GetStyle("Label");
                TextAnchor bak = style.alignment;
                style.alignment = TextAnchor.MiddleCenter;
                GUILayout.Label("Shortcuts", style);
                style.alignment = bak;

                if (GUILayout.Button("+ Add Shortcut") && this._boneTarget != null)
                {
                    string path = this._boneTarget.GetPathFrom(this.transform);
                    if (customShortcuts.ContainsKey(path) == false)
                        customShortcuts.Add(path, this._boneTarget.name);
                    this._removeShortcutMode = false;
                }

                Color color = GUI.color;
                if (this._removeShortcutMode)
                    GUI.color = AdvancedModeModule._redColor;
                if (GUILayout.Button(this._removeShortcutMode ? "Click on a shortcut" : "- Remove Shortcut"))
                    this._removeShortcutMode = !this._removeShortcutMode;
                GUI.color = color;

                GUILayout.EndVertical();
                GUILayout.EndHorizontal();

                GUILayout.EndVertical();

            }
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }

        private void ChangeBoneTarget(Transform newTarget)
        {
            this._boneTarget = newTarget;
            this._currentAlias = MainWindow.self.boneAliases.ContainsKey(this._boneTarget.name) ? MainWindow.self.boneAliases[this._boneTarget.name] : "";
            this._twinBoneTarget = this.GetTwinBone(newTarget);
            if (this._boneTarget == this._twinBoneTarget)
                this._twinBoneTarget = null;
            this._colliderTarget = newTarget.GetComponent<DynamicBoneCollider>();
            this.UpdateGizmosParent();
            this.CheckGizmosEnabled();
        }

        public void LoadFrom(BonesEditor other)
        {
            this.ExecuteDelayed(() =>
            {
                foreach (GameObject openedBone in other._openedBones)
                {
                    Transform obj = this.transform.FindChild(openedBone.transform.GetPathFrom(other.transform));
                    if (obj != null)
                        this._openedBones.Add(obj.gameObject);
                }
                foreach (GameObject ignoredObject in other._ignoredObjects)
                {
                    Transform obj = this.transform.FindChild(ignoredObject.transform.GetPathFrom(other.transform));
                    if (obj != null)
                        this._ignoredObjects.Add(obj.gameObject);
                }
                foreach (KeyValuePair<GameObject, TransformData> kvp in other._dirtyBones)
                {
                    Transform obj = this.transform.FindChild(kvp.Key.transform.GetPathFrom(other.transform));
                    if (obj != null)
                        this._dirtyBones.Add(obj.gameObject, new TransformData(kvp.Value));
                }
                foreach (KeyValuePair<DynamicBoneCollider, ColliderData> kvp in other._dirtyColliders)
                {
                    Transform obj = this.transform.FindChild(kvp.Key.transform.GetPathFrom(other.transform));
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
                    while (t != this.transform)
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
                    if (kvp.Value.rotation.hasValue && (!this.chara.oiCharInfo.enableFK || !this._fkObjects.TryGetValue(kvp.Key, out info) || info.active == false))
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
            if (this._dirtyColliders.Count != 0)
            {
                xmlWriter.WriteStartElement("colliders");
                foreach (KeyValuePair<DynamicBoneCollider, ColliderData> kvp in this._dirtyColliders)
                {
                    Transform t = kvp.Key.transform.parent;
                    string n = kvp.Key.transform.name;
                    while (t != this.transform)
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

        public override void LoadXml(XmlNode xmlNode)
        {
            XmlNode objects = xmlNode.FindChildNode("advancedObjects");
            if (objects != null)
            {
                foreach (XmlNode node in objects.ChildNodes)
                {
                    if (node.Name == "object")
                    {
                        string name = node.Attributes["name"].Value;
                        GameObject obj = this.transform.FindChild(name).gameObject;
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
                            this._dirtyBones.Add(obj, data);
                    }
                }
            }

            XmlNode colliders = xmlNode.FindChildNode("colliders");
            if (colliders != null)
            {
                foreach (XmlNode node in colliders.ChildNodes)
                {
                    DynamicBoneCollider collider = this.transform.FindChild(node.Attributes["name"].Value).GetComponent<DynamicBoneCollider>();
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
                        this._dirtyColliders.Add(collider, data);
                }
            }
        }
        #endregion

        #region Private Methods
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
            if ((!this.chara.oiCharInfo.enableFK || !this._fkObjects.ContainsKey(bone.gameObject)) && this._dirtyBones.TryGetValue(bone.gameObject, out data))
            {
                data.rotation.Reset();
                this.SetBoneNotDirtyIf(bone.gameObject);
            }
            if (this._symmetricalEdition && twinBone != null && (!this.chara.oiCharInfo.enableFK || !this._fkObjects.ContainsKey(twinBone.gameObject)) && this._dirtyBones.TryGetValue(twinBone.gameObject, out data))
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
                    if (pair.Key.transform.IsChildOf(bone))
                    {
                        Transform childBone = pair.Key.transform;
                        Transform twinChildBone = this.GetTwinBone(childBone);
                        if (twinChildBone == childBone)
                            twinChildBone = null;
                        this.ResetBoneScale(childBone, twinChildBone, false);
                    }
                }
            }

        }

        private void DisplayObjectTree(GameObject go, int indent)
        {
            if (this._ignoredObjects.Contains(go))
                return;
            Color c = GUI.color;
            if (this._dirtyBones.ContainsKey(go))
                GUI.color = Color.magenta;
            if (this._colliderObjects.Contains(go.transform))
                GUI.color = BonesEditor._colliderColor;
            if (this._boneTarget == go.transform)
                GUI.color = Color.cyan;
            GUILayout.BeginHorizontal();
            GUILayout.Space(indent * 20f);
            int childCount = 0;
            for (int i = 0; i < go.transform.childCount; ++i)
                if (this._ignoredObjects.Contains(go.transform.GetChild(i).gameObject) == false)
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
            string bName = go.name;
            string newName;
            if (MainWindow.self.boneAliases.TryGetValue(bName, out newName))
                bName = newName;
            if (GUILayout.Button(bName + (this.IsBoneDirty(go) ? "*" : ""), GUILayout.ExpandWidth(false)))
            {
                this.ChangeBoneTarget(go.transform);
            }
            GUI.color = c;
            GUILayout.EndHorizontal();
            if (this._openedBones.Contains(go))
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

        private void GoToObject(string path)
        {
            if (this.transform.FindChild(path) != null)
                this.GoToObject(this.transform.FindChild(path).gameObject);
        }

        private void GoToObject(GameObject go)
        {
            if (ReferenceEquals(go, this.transform.GetChild(0).gameObject))
                return;
            GameObject goBak = go;
            this.ChangeBoneTarget(go.transform);
            go = go.transform.parent.gameObject;
            while (go.transform != this.transform.GetChild(0))
            {
                this._openedBones.Add(go);
                go = go.transform.parent.gameObject;
            }
            this._openedBones.Add(go);
            Vector2 scroll = new Vector2(0f, -GUI.skin.button.CalcHeight(new GUIContent("a"), 100f) - 4);
            this.GetScrollPosition(this.transform.GetChild(0).gameObject, goBak, 0, ref scroll);
            this._boneEditionScroll = scroll;
        }

        private bool GetScrollPosition(GameObject root, GameObject go, int indent, ref Vector2 scrollPosition)
        {
            if (this._ignoredObjects.Contains(go))
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
        private void ColliderEditor(int id)
        {
            GUILayout.BeginVertical();

            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("Center:");
            GUILayout.BeginHorizontal();
            Vector3 center = this.Vector3Editor(this._colliderTarget.m_Center);
            if (center != this._colliderTarget.m_Center)
            {
                this.SetColliderDirty(this._colliderTarget);
                if (this._dirtyColliders[this._colliderTarget].originalCenter.hasValue == false)
                    this._dirtyColliders[this._colliderTarget].originalCenter = this._colliderTarget.m_Center;
            }
            this._colliderTarget.m_Center = center;
            this.IncEditor();
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            GUILayout.BeginHorizontal(GUI.skin.box);
            GUILayout.Label("Radius\t", GUILayout.ExpandWidth(false));
            float radius = GUILayout.HorizontalSlider(this._colliderTarget.m_Radius, 0f, 1f);
            if (radius != this._colliderTarget.m_Radius)
            {
                this.SetColliderDirty(this._colliderTarget);
                if (this._dirtyColliders[this._colliderTarget].originalRadius.hasValue == false)
                    this._dirtyColliders[this._colliderTarget].originalRadius = this._colliderTarget.m_Radius;
            }
            this._colliderTarget.m_Radius = radius;
            GUILayout.Label(this._colliderTarget.m_Radius.ToString("0.000"), GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal(GUI.skin.box);
            GUILayout.Label("Height\t", GUILayout.ExpandWidth(false));
            float height = GUILayout.HorizontalSlider(this._colliderTarget.m_Height, 2 * this._colliderTarget.m_Radius, 3f);
            if (height < this._colliderTarget.m_Radius * 2)
                height = this._colliderTarget.m_Radius * 2;
            if (height != this._colliderTarget.m_Height)
            {
                this.SetColliderDirty(this._colliderTarget);
                if (this._dirtyColliders[this._colliderTarget].originalHeight.hasValue == false)
                    this._dirtyColliders[this._colliderTarget].originalHeight = this._colliderTarget.m_Height;
            }
            this._colliderTarget.m_Height = height;
            GUILayout.Label(this._colliderTarget.m_Height.ToString("0.000"), GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal(GUI.skin.box);
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

            GUILayout.BeginHorizontal(GUI.skin.box);
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
            Color c = GUI.color;
            GUI.color = AdvancedModeModule._redColor;
            if (GUILayout.Button("Reset", GUILayout.ExpandWidth(false)))
                this.SetColliderNotDirty(this._colliderTarget);
            GUI.color = c;
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();

            GUI.DragWindow();
        }

        private void OnParentage(TreeNodeObject parent, TreeNodeObject child)
        {
            if (parent == null)
            {
                ObjectCtrlInfo info;
                if (Studio.Studio.Instance.dicInfo.TryGetValue(child, out info) && this._ignoredObjects.Contains(info.guideObject.transformTarget.gameObject))
                    this._ignoredObjects.Remove(info.guideObject.transformTarget.gameObject);
            }
            else
            {
                ObjectCtrlInfo info;
                if (Studio.Studio.Instance.dicInfo.TryGetValue(child, out info) && info.guideObject.transformTarget.IsChildOf(this.transform))
                    this._ignoredObjects.Add(info.guideObject.transformTarget.gameObject);
            }
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

        private void ApplyBoneManualCorrection()
        {
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

        private void DrawGizmos()
        {
            if (!this.isEnabled || !this.drawAdvancedMode)
                return;
            foreach (VectorLine line in this._cubeDebugLines)
                line.Draw();
            if (this._colliderTarget != null)
                this._colliderDebugLines.Draw(this._colliderTarget);
        }

        private void UpdateGizmosParent()
        {
            foreach (VectorLine line in this._cubeDebugLines)
            {
                line.drawTransform = this._boneTarget;
            }
        }

        private void CheckGizmosEnabled()
        {
            foreach (VectorLine line in this._cubeDebugLines)
            {
                line.active = this.isEnabled && this.drawAdvancedMode;
            }
            this._colliderDebugLines.SetActive(this.isEnabled && this.drawAdvancedMode && this._colliderTarget != null);
        }
        #endregion
    }
}
