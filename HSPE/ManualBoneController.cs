using System.Collections.Generic;
using System.IO;
using Manager;
using RootMotion.Demos;
using RootMotion.FinalIK;
using UnityEngine;

namespace HSPE
{
    public class ManualBoneController : MonoBehaviour
    {
        #region Private Static Variables
        private static readonly Vector3 _targetsScale = Vector3.one / 10f;
        private static readonly Vector3 _goalsScale = Vector3.one / 12f;
        #endregion

        #region Private Variables
        private FullBodyBipedIK _body;
        private ObjController[] _targets = new ObjController[9];
        private FBIKBendGoal[] _bendGoals = new FBIKBendGoal[4];
        private ObjController[] _bendGoalsControllers = new ObjController[4];
        private CameraGL _cam;
        private Material _mat;
        private Transform _advancedTarget;
        private HashSet<GameObject> _openGameObjects = new HashSet<GameObject>();
        private bool _advancedCoordWorld = true;
        private bool _advancedCoordPosition = false;
        private Vector2 _advancedScroll;
        private float _inc = 1f;
        private Dictionary<GameObject, KeyValuePair<Vector3, Quaternion>> _originalTransforms = new Dictionary<GameObject, KeyValuePair<Vector3, Quaternion>>();
        private bool _ready = false;
        private bool _loaded = false;
        private bool _isFemale = false;
        private bool _showControllers = true;
        private float _cachedSpineStiffness;
        private float _cachedPullBodyVertical;
        private int _cachedSolverIterations;
        private Transform _legKneeDamL;
        private Transform _legKneeDamR;
        private Transform _legKneeBackL;
        private Transform _legKneeBackR;
        private Transform _legUpL;
        private Transform _legUpR;
        private Transform _elbowDamL;
        private Transform _elbowDamR;
        #endregion

        #region Public Accessors
        public StudioChara chara { get; set; }
        public bool draw { get; set; }
        public bool advancedMode { get; set; }

        public bool showControllers
        {
            get { return this._showControllers; }
            set
            {
                if (value != this._showControllers)
                {
                    foreach (ObjController controller in this._targets)
                    {
                        controller.ChangeEnable(value);
                    }
                    foreach (ObjController controller in this._bendGoalsControllers)
                    {
                        controller.ChangeEnable(value);
                    }
                }
                this._showControllers = value;
            }
        }
        #endregion

        #region Unity Methods
        void Awake()
        {
            this.GetComponent<Animator>().enabled = false;
            this._mat = new Material(Shader.Find("Unlit/Transparent"));
            this._cam = Camera.current.GetComponent<CameraGL>();
            if (this._cam == null)
                this._cam = Camera.current.gameObject.AddComponent<CameraGL>();
            this._cam.onPostRender += DrawGizmos;
            this._body = this.GetComponent<FullBodyBipedIK>();
            this._body.solver.OnPostSolve += OnPostSolve;
        }

        void Start()
        {
            this._isFemale = this.chara is StudioFemale;
            if (this._loaded == false)
            {
                for (int i = 0; i < 9; ++i)
                {
                    IKEffector effector = this._body.solver.GetEffector((FullBodyBipedEffector)i);
                    ObjController go = CommonLib.LoadAsset<GameObject>("studio/objcontroller.unity3d", "ObjController", true, string.Empty).GetComponent<ObjController>();
                    go.gameObject.name = "Effector_" + i + "Target";
                    go.transform.SetParent(this.transform);
                    go.transform.position = effector.bone.position;
                    go.transform.rotation = effector.bone.rotation;
                    go.transform.localScale = _targetsScale;
                    effector.target = go.transform;
                    go.AttuchObj = go.gameObject;
                    go.GetComponentInChildren<SelectedMouseScript>().ObjController = null;
                    this._targets[i] = go;
                }
                for (int i = 0; i < 4; ++i)
                {
                    FBIKBendGoal bendGoal = CommonLib.LoadAsset<GameObject>("studio/objcontroller.unity3d", "ObjController", true, string.Empty).AddComponent<FBIKBendGoal>();
                    bendGoal.gameObject.name = "BendGoal_" + i;
                    bendGoal.chain = (FullBodyBipedChain) i;
                    bendGoal.ik = this._body;
                    bendGoal.transform.SetParent(this.transform, true);
                    bendGoal.transform.localScale = _goalsScale;
                    bendGoal.transform.localRotation = Quaternion.identity;
                    Vector3 offset = Vector3.zero;
                    switch ((FullBodyBipedChain) i)
                    {
                        case FullBodyBipedChain.LeftArm:
                        case FullBodyBipedChain.RightArm:
                            offset = -this._body.solver.GetEndEffector((FullBodyBipedChain)i).bone.forward / 2f;
                            break;
                        case FullBodyBipedChain.LeftLeg:
                        case FullBodyBipedChain.RightLeg:
                            offset = this._body.solver.GetEndEffector((FullBodyBipedChain)i).bone.forward / 2f;
                            break;
                    }
                    bendGoal.transform.position = this._body.solver.GetEndEffector((FullBodyBipedChain)i).bone.position + offset;
                    this._bendGoals[i] = bendGoal;

                    foreach (RingObjRotationController r in bendGoal.transform.GetChild(0).GetComponentsInChildren<RingObjRotationController>())
                        if (r.gameObject != bendGoal.transform.GetChild(0).gameObject)
                            Destroy(r.gameObject);
                    ObjController controller = bendGoal.GetComponent<ObjController>();
                    controller.AttuchObj = bendGoal.gameObject;
                    SelectedMouseScript cube = bendGoal.GetComponentInChildren<SelectedMouseScript>();
                    cube.ObjController = null;
                    cube.transform.localPosition = Vector3.zero;
                    this._bendGoalsControllers[i] = controller;
                }
            }

            this._legKneeDamL = this._body.solver.leftLegMapping.bone1.GetChild(0);
            this._legKneeDamR = this._body.solver.rightLegMapping.bone1.GetChild(0);
            if (this._isFemale)
            {
                this._legKneeBackL = this._body.solver.leftLegMapping.bone1.FindDescendant("cf_J_LegKnee_back_L");
                this._legKneeBackR = this._body.solver.rightLegMapping.bone1.FindDescendant("cf_J_LegKnee_back_R");
                this._legUpL = this.transform.FindDescendant("cf_J_LegUpDam_L");
                this._legUpR = this.transform.FindDescendant("cf_J_LegUpDam_R");
                this._elbowDamL = this.transform.FindDescendant("cf_J_ArmElbo_dam_01_L");
                this._elbowDamR = this.transform.FindDescendant("cf_J_ArmElbo_dam_01_R");
            }
            else
            {
                this._legKneeBackL = this._body.solver.leftLegMapping.bone1.FindDescendant("cm_J_LegKnee_back_s_L");
                this._legKneeBackR = this._body.solver.rightLegMapping.bone1.FindDescendant("cm_J_LegKnee_back_s_R");
                this._legUpL = this.transform.FindDescendant("cm_J_LegUpDam_L");
                this._legUpR = this.transform.FindDescendant("cm_J_LegUpDam_R");
                this._elbowDamL = this.transform.FindDescendant("cm_J_ArmElbo_dam_02_L");
                this._elbowDamR = this.transform.FindDescendant("cm_J_ArmElbo_dam_02_R");
            }

            this._cachedSpineStiffness = this._body.solver.spineStiffness;
            this._cachedPullBodyVertical = this._body.solver.pullBodyVertical;
            this._cachedSolverIterations = this._body.solver.iterations;
            this._body.solver.spineStiffness = 0f;
            this._body.solver.pullBodyVertical = 0f;
            this._body.solver.iterations = 1;
            this._body.fixTransforms = true;
            this._ready = true;
        }

        void OnGUI()
        {
            if (this.draw && this.advancedMode)
                GUILayout.Window(50, new Rect(Screen.width * 0.66f, Screen.height * 0.66f, Screen.width * 0.33f, Screen.height * 0.33f), this.AdvancedModeWindow, "Advanced mode");
        }
        void LateUpdate()
        {
            for (int i = 0; i < this._targets.Length; ++i)
            {
                IKEffector effector = this._body.solver.GetEffector((FullBodyBipedEffector)i);
                effector.positionWeight = 1f;
                effector.rotationWeight = 1f;
            }
            foreach (FBIKBendGoal bendGoal in this._bendGoals)
            {
                bendGoal.weight = 1f;
            }
            this._body.solver.FixTransforms();
            this._body.solver.Update();
        }
        void OnDestroy()
        {
            foreach (DynamicBone_Ver02 b in this.GetComponents<DynamicBone_Ver02>())
                b.enabled = true;
            foreach (DynamicBone_MuneController b in this.GetComponents<DynamicBone_MuneController>())
                b.enabled = true;
            foreach (ObjController go in this._targets)
                Destroy(go.gameObject);
            foreach (FBIKBendGoal t in this._bendGoals)
                Destroy(t.gameObject);
            this._cam.onPostRender -= DrawGizmos;
            this._body.solver.OnPostSolve -= OnPostSolve;
            this.GetComponent<Animator>().enabled = true;
            this._body.solver.spineStiffness = this._cachedSpineStiffness;
            this._body.solver.pullBodyVertical = this._cachedPullBodyVertical;
            this._body.solver.iterations = this._cachedSolverIterations;
        }
        #endregion

        #region Public Methods
        public void SetBoneTargetRotation(FullBodyBipedEffector type, Quaternion targetRotation)
        {
            if (this._ready)
                this._targets[(int)type].transform.rotation = targetRotation;
        }

        public Quaternion GetBoneTargetRotation(FullBodyBipedEffector type)
        {
            if (!this._ready)
                return Quaternion.identity;
            return this._targets[(int)type].transform.rotation;
        }

        public void SetBoneTargetPosition(FullBodyBipedEffector type, Vector3 targetPosition)
        {
            if (this._ready)
                this._targets[(int)type].transform.position = targetPosition;
        }

        public Vector3 GetBoneTargetPosition(FullBodyBipedEffector type)
        {
            if (!this._ready)
                return Vector3.zero;
            return this._targets[(int)type].transform.position;
        }

        public void SetBendGoalPosition(FullBodyBipedChain type, Vector3 position)
        {
            if (this._ready)
                this._bendGoals[(int)type].transform.position = position;
        }

        public Vector3 GetBendGoalPosition(FullBodyBipedChain type)
        {
            if (!this._ready)
                return Vector3.zero;
            return this._bendGoals[(int)type].transform.position;
        }

        public void LoadBinary(BinaryReader binaryReader)
        {
            this._loaded = true;
            binaryReader.ReadInt32();
            for (int i = 0; i < 9; ++i)
            {
                Vector3 pos;
                Quaternion rot;

                pos.x = binaryReader.ReadSingle();
                pos.y = binaryReader.ReadSingle();
                pos.z = binaryReader.ReadSingle();

                rot.w = binaryReader.ReadSingle();
                rot.x = binaryReader.ReadSingle();
                rot.y = binaryReader.ReadSingle();
                rot.z = binaryReader.ReadSingle();

                IKEffector effector = this._body.solver.GetEffector((FullBodyBipedEffector)i);
                ObjController go = CommonLib.LoadAsset<GameObject>("studio/objcontroller.unity3d", "ObjController", true, string.Empty).GetComponent<ObjController>();
                go.gameObject.name = "Effector_" + i + "Target";
                go.transform.SetParent(this.transform);
                go.transform.position = pos;
                go.transform.rotation = rot;
                go.transform.localScale = _targetsScale;
                effector.target = go.transform;
                go.AttuchObj = go.gameObject;
                go.GetComponentInChildren<SelectedMouseScript>().ObjController = null;
                this._targets[i] = go;
            }
            binaryReader.ReadInt32();
            for (int i = 0; i < 4; ++i)
            {
                Vector3 pos;

                pos.x = binaryReader.ReadSingle();
                pos.y = binaryReader.ReadSingle();
                pos.z = binaryReader.ReadSingle();

                FBIKBendGoal bendGoal = CommonLib.LoadAsset<GameObject>("studio/objcontroller.unity3d", "ObjController", true, string.Empty).AddComponent<FBIKBendGoal>();
                bendGoal.gameObject.name = "BendGoal_" + i;
                bendGoal.chain = (FullBodyBipedChain)i;
                bendGoal.ik = this._body;
                bendGoal.transform.SetParent(this.transform, true);
                bendGoal.transform.localRotation = Quaternion.identity;
                bendGoal.transform.localScale = _goalsScale;
                bendGoal.transform.position = pos;
                this._bendGoals[i] = bendGoal;

                foreach (RingObjRotationController r in bendGoal.transform.GetChild(0).GetComponentsInChildren<RingObjRotationController>())
                    if (r.gameObject != bendGoal.transform.GetChild(0).gameObject)
                        Destroy(r.gameObject);
                ObjController controller = bendGoal.GetComponent<ObjController>();
                controller.AttuchObj = bendGoal.gameObject;
                SelectedMouseScript cube = bendGoal.GetComponentInChildren<SelectedMouseScript>();
                cube.ObjController = null;
                cube.transform.localPosition = Vector3.zero;
                this._bendGoalsControllers[i] = controller;
            }
            int count = binaryReader.ReadInt32();
            for (int i = 0; i < count; ++i)
            {
                string name = binaryReader.ReadString();
                GameObject obj = this.transform.FindDescendant(name).gameObject;
                Vector3 pos;
                Quaternion rot;
                pos.x = binaryReader.ReadSingle();
                pos.y = binaryReader.ReadSingle();
                pos.z = binaryReader.ReadSingle();

                rot.w = binaryReader.ReadSingle();
                rot.x = binaryReader.ReadSingle();
                rot.y = binaryReader.ReadSingle();
                rot.z = binaryReader.ReadSingle();
                this._originalTransforms.Add(obj, new KeyValuePair<Vector3, Quaternion>(pos, rot));

                pos.x = binaryReader.ReadSingle();
                pos.y = binaryReader.ReadSingle();
                pos.z = binaryReader.ReadSingle();

                rot.w = binaryReader.ReadSingle();
                rot.x = binaryReader.ReadSingle();
                rot.y = binaryReader.ReadSingle();
                rot.z = binaryReader.ReadSingle();
                obj.transform.localPosition = pos;
                obj.transform.localRotation = rot;
            }
        }

        public void SaveBinary(BinaryWriter binaryWriter)
        {
            binaryWriter.Write(this._targets.Length);
            foreach (ObjController con in this._targets)
            {
                binaryWriter.Write(con.transform.position.x);
                binaryWriter.Write(con.transform.position.y);
                binaryWriter.Write(con.transform.position.z);

                binaryWriter.Write(con.transform.rotation.w);
                binaryWriter.Write(con.transform.rotation.x);
                binaryWriter.Write(con.transform.rotation.y);
                binaryWriter.Write(con.transform.rotation.z);
            }
            binaryWriter.Write(this._bendGoals.Length);
            foreach (FBIKBendGoal goal in this._bendGoals)
            {
                binaryWriter.Write(goal.transform.position.x);
                binaryWriter.Write(goal.transform.position.y);
                binaryWriter.Write(goal.transform.position.z);
            }
            binaryWriter.Write(this._originalTransforms.Count);
            foreach (KeyValuePair<GameObject, KeyValuePair<Vector3, Quaternion>> kvp in this._originalTransforms)
            {
                binaryWriter.Write(kvp.Key.name);

                binaryWriter.Write(kvp.Value.Key.x);
                binaryWriter.Write(kvp.Value.Key.y);
                binaryWriter.Write(kvp.Value.Key.z);

                binaryWriter.Write(kvp.Value.Value.w);
                binaryWriter.Write(kvp.Value.Value.x);
                binaryWriter.Write(kvp.Value.Value.y);
                binaryWriter.Write(kvp.Value.Value.z);

                binaryWriter.Write(kvp.Key.transform.localPosition.x);
                binaryWriter.Write(kvp.Key.transform.localPosition.y);
                binaryWriter.Write(kvp.Key.transform.localPosition.z);

                binaryWriter.Write(kvp.Key.transform.localRotation.w);
                binaryWriter.Write(kvp.Key.transform.localRotation.x);
                binaryWriter.Write(kvp.Key.transform.localRotation.y);
                binaryWriter.Write(kvp.Key.transform.localRotation.z);
            }
        }
        #endregion

        #region Private Methods
        private void AdvancedModeWindow(int id)
        {
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical();
            this._advancedScroll = GUILayout.BeginScrollView(_advancedScroll, GUI.skin.box, GUILayout.ExpandHeight(true));
            GUILayout.Label("Character Tree");
            //foreach (Canvas c in FindObjectsOfType<Canvas>())
            //{
            //    this.DisplayObjectTree(c.gameObject, 0);
            //}
            this.DisplayObjectTree(this.transform.GetChild(0).gameObject, 0);
            GUILayout.EndScrollView();
            //if (this._advancedTarget != null)
            //    foreach (Component c in this._advancedTarget.GetComponents<Component>())
            //        GUILayout.Label(c.GetType().FullName);
            GUILayout.EndVertical();
            GUILayout.BeginVertical(GUI.skin.box, GUILayout.MinWidth(Screen.width * 0.08333f), GUILayout.MaxWidth(Screen.width * 0.165f));
            {
                GUILayout.BeginHorizontal(GUI.skin.box);
                GUILayout.Label("Frame of reference: ");
                this._advancedCoordWorld = GUILayout.Toggle(this._advancedCoordWorld, "World");
                this._advancedCoordWorld = !GUILayout.Toggle(!this._advancedCoordWorld, "Local");
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal(GUI.skin.box);
                this._advancedCoordPosition = GUILayout.Toggle(this._advancedCoordPosition, "Position");
                this._advancedCoordPosition = !GUILayout.Toggle(!this._advancedCoordPosition, "Rotation");
                GUILayout.EndHorizontal();
                if (!this._advancedCoordPosition)
                {
                    Quaternion rotation = Quaternion.identity;
                    if (this._advancedTarget != null)
                    {
                        if (this._advancedCoordWorld)
                            rotation = this._advancedTarget.rotation;
                        else
                            rotation = this._advancedTarget.localRotation;
                    }
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("X (Pitch): " + rotation.eulerAngles.x.ToString("0.00"));
                    GUILayout.BeginHorizontal(GUILayout.MaxWidth(200f));
                    if (GUILayout.RepeatButton((-this._inc).ToString("+0.###;-0.###")))
                        rotation *= Quaternion.AngleAxis(-this._inc, Vector3.right);
                    if (GUILayout.RepeatButton(this._inc.ToString("+0.###;-0.###")))
                        rotation *= Quaternion.AngleAxis(this._inc, Vector3.right);
                    GUILayout.EndHorizontal();
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Y (Yaw): " + rotation.eulerAngles.y.ToString("0.00"));
                    GUILayout.BeginHorizontal(GUILayout.MaxWidth(200f));
                    if (GUILayout.RepeatButton((-this._inc).ToString("+0.###;-0.###")))
                        rotation *= Quaternion.AngleAxis(-this._inc, Vector3.up);
                    if (GUILayout.RepeatButton(this._inc.ToString("+0.###;-0.###")))
                        rotation *= Quaternion.AngleAxis(this._inc, Vector3.up);
                    GUILayout.EndHorizontal();
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Z (Roll): " + rotation.eulerAngles.z.ToString("0.00"));
                    GUILayout.BeginHorizontal(GUILayout.MaxWidth(200f));
                    if (GUILayout.RepeatButton((-this._inc).ToString("+0.###;-0.###")))
                        rotation *= Quaternion.AngleAxis(-this._inc, Vector3.forward);
                    if (GUILayout.RepeatButton(this._inc.ToString("+0.###;-0.###")))
                        rotation *= Quaternion.AngleAxis(this._inc, Vector3.forward);
                    GUILayout.EndHorizontal();
                    GUILayout.EndHorizontal();
                    if (this._advancedTarget != null)
                    {
                        if (this._advancedCoordWorld)
                            this._advancedTarget.rotation = rotation;
                        else
                            this._advancedTarget.localRotation = rotation;
                    }
                }
                else
                {
                    Vector3 position = Vector3.zero;
                    if (this._advancedTarget != null)
                        position = this._advancedCoordWorld ? this._advancedTarget.position : this._advancedTarget.localPosition;
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("X: " + position.x.ToString("0.0000"));
                    GUILayout.BeginHorizontal(GUILayout.MaxWidth(200f));
                    if (GUILayout.RepeatButton((-this._inc).ToString("+0.###;-0.###")))
                        position -= this._inc * Vector3.right;
                    if (GUILayout.RepeatButton(this._inc.ToString("+0.###;-0.###")))
                        position += this._inc * Vector3.right;
                    GUILayout.EndHorizontal();
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Y: " + position.y.ToString("0.0000"));
                    GUILayout.BeginHorizontal(GUILayout.MaxWidth(200f));
                    if (GUILayout.RepeatButton((-this._inc).ToString("+0.###;-0.###")))
                        position -= this._inc * Vector3.up;
                    if (GUILayout.RepeatButton(this._inc.ToString("+0.###;-0.###")))
                        position += this._inc * Vector3.up;
                    GUILayout.EndHorizontal();
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Z: " + position.z.ToString("0.0000"));
                    GUILayout.BeginHorizontal(GUILayout.MaxWidth(200f));
                    if (GUILayout.RepeatButton((-this._inc).ToString("+0.###;-0.###")))
                        position -= this._inc * Vector3.forward;
                    if (GUILayout.RepeatButton(this._inc.ToString("+0.###;-0.###")))
                        position += this._inc * Vector3.forward;
                    GUILayout.EndHorizontal();
                    GUILayout.EndHorizontal();
                    if (this._advancedTarget != null)
                    {
                        if (this._advancedCoordWorld)
                            this._advancedTarget.position = position;
                        else
                            this._advancedTarget.localPosition = position;
                    }
                }
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("0.001"))
                    this._inc = 0.001f;
                if (GUILayout.Button("0.01"))
                    this._inc = 0.01f;
                if (GUILayout.Button("0.1"))
                    this._inc = 0.1f;
                if (GUILayout.Button("1"))
                    this._inc = 1f;
                if (GUILayout.Button("10"))
                    this._inc = 10f;
                GUILayout.EndHorizontal();
                if (this._advancedTarget != null)
                {
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Reset Local Position"))
                        this._advancedTarget.localPosition = this._originalTransforms[this._advancedTarget.gameObject].Key;
                    if (GUILayout.Button("Reset Local Rotation"))
                        this._advancedTarget.localRotation = this._originalTransforms[this._advancedTarget.gameObject].Value;
                    if (GUILayout.Button("Reset Both"))
                    {
                        this._advancedTarget.localPosition = this._originalTransforms[this._advancedTarget.gameObject].Key;
                        this._advancedTarget.localRotation = this._originalTransforms[this._advancedTarget.gameObject].Value;
                    }
                    GUILayout.EndHorizontal();
                }
            }
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }

        private void DisplayObjectTree(GameObject go, int indent)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(indent * 20f);
            if (go.transform.childCount != 0)
            {
                if (GUILayout.Toggle(this._openGameObjects.Contains(go), "", GUILayout.ExpandWidth(false)))
                {
                    if (this._openGameObjects.Contains(go) == false)
                        this._openGameObjects.Add(go);
                }
                else
                {
                    if (this._openGameObjects.Contains(go))
                        this._openGameObjects.Remove(go);
                }
            }
            else
                GUILayout.Space(21f);
            Color c = GUI.color;
            if (this._originalTransforms.ContainsKey(go))
                GUI.color = Color.magenta;
            if (_advancedTarget == go.transform)
                GUI.color = Color.cyan;
            if (GUILayout.Button(go.name + (this.IsObjectDirty(go) ? "*" : ""), GUILayout.ExpandWidth(false)))
            {
                if (this._advancedTarget != null && this.IsObjectDirty(this._advancedTarget.gameObject) == false)
                    this._originalTransforms.Remove(this._advancedTarget.gameObject);
                _advancedTarget = go.transform;
                if (this._originalTransforms.ContainsKey(go) == false)
                    this._originalTransforms.Add(go, new KeyValuePair<Vector3, Quaternion>(go.transform.localPosition, go.transform.localRotation));
            }
            GUI.color = c;
            GUILayout.EndHorizontal();
            if (this._openGameObjects.Contains(go))
                for (int i = 0; i < go.transform.childCount; ++i)
                    this.DisplayObjectTree(go.transform.GetChild(i).gameObject, indent + 1);
        }

        private bool IsObjectDirty(GameObject go)
        {
            if (this._originalTransforms.ContainsKey(go))
            {
                KeyValuePair<Vector3, Quaternion> tr = this._originalTransforms[go];
                return (go.transform.localPosition != tr.Key || go.transform.localRotation != tr.Value);
            }
            return false;
        }


        private void OnPostSolve()
        {
            this._legKneeDamL.rotation = Quaternion.Lerp(this._body.solver.leftLegMapping.bone1.rotation, this._body.solver.leftLegMapping.bone2.rotation, 0.5f);
            this._legKneeDamR.rotation = Quaternion.Lerp(this._body.solver.rightLegMapping.bone1.rotation, this._body.solver.rightLegMapping.bone2.rotation, 0.5f);
            this._legKneeBackL.rotation = this._body.solver.leftLegMapping.bone2.rotation;
            this._legKneeBackR.rotation = this._body.solver.rightLegMapping.bone2.rotation;
            this._legUpL.rotation = Quaternion.Lerp(this._legUpL.parent.rotation, this._body.solver.leftThighEffector.bone.rotation, 0.85f);
            this._legUpR.rotation = Quaternion.Lerp(this._legUpR.parent.rotation, this._body.solver.rightThighEffector.bone.rotation, 0.85f);
            this._elbowDamL.rotation = Quaternion.Lerp(this._elbowDamL.parent.rotation, this._body.solver.leftArmMapping.bone2.rotation, 0.65f);
            this._elbowDamR.rotation = Quaternion.Lerp(this._elbowDamR.parent.rotation, this._body.solver.rightArmMapping.bone2.rotation, 0.65f);
        }

        private void DrawGizmos()
        {
            if (!this.draw)
                return;
            GL.PushMatrix();
            this._mat.SetPass(0);
            GL.LoadProjectionMatrix(Studio.Instance.MainCamera.projectionMatrix);
            GL.MultMatrix(Studio.Instance.MainCamera.transform.worldToLocalMatrix);
            GL.Begin(GL.LINES);
            if (this._advancedTarget)    
                this.GLDrawCube(this._advancedTarget.position, this._advancedTarget.rotation, 0.025f, true);
            GL.End();
            GL.PopMatrix();
        }

        private void GLDrawCube(Vector3 position, Quaternion rotation, float size, bool up = false)
        {
            Vector3 topLeftForward = position + (rotation * ((Vector3.up + Vector3.left + Vector3.forward) * size)),
                    topRightForward = position + (rotation * ((Vector3.up + Vector3.right + Vector3.forward) * size)),
                    bottomLeftForward = position + (rotation * ((Vector3.down + Vector3.left + Vector3.forward) * size)),
                    bottomRightForward = position + (rotation * ((Vector3.down + Vector3.right + Vector3.forward) * size)),
                    topLeftBack = position + (rotation * ((Vector3.up + Vector3.left + Vector3.back) * size)),
                    topRightBack = position + (rotation * ((Vector3.up + Vector3.right + Vector3.back) * size)),
                    bottomLeftBack = position + (rotation * ((Vector3.down + Vector3.left + Vector3.back) * size)),
                    bottomRightBack = position + (rotation * ((Vector3.down + Vector3.right + Vector3.back) * size));
            GL.Vertex(topLeftForward);
            GL.Vertex(topRightForward);

            GL.Vertex(topRightForward);
            GL.Vertex(bottomRightForward);

            GL.Vertex(bottomRightForward);
            GL.Vertex(bottomLeftForward);

            GL.Vertex(bottomLeftForward);
            GL.Vertex(topLeftForward);

            GL.Vertex(topLeftBack);
            GL.Vertex(topRightBack);

            GL.Vertex(topRightBack);
            GL.Vertex(bottomRightBack);

            GL.Vertex(bottomRightBack);
            GL.Vertex(bottomLeftBack);

            GL.Vertex(bottomLeftBack);
            GL.Vertex(topLeftBack);

            GL.Vertex(topLeftBack);
            GL.Vertex(topLeftForward);

            GL.Vertex(topRightBack);
            GL.Vertex(topRightForward);

            GL.Vertex(bottomRightBack);
            GL.Vertex(bottomRightForward);

            GL.Vertex(bottomLeftBack);
            GL.Vertex(bottomLeftForward);
            if (up)
            {
                GL.Vertex(topRightForward);
                GL.Vertex(topLeftBack);

                GL.Vertex(topLeftForward);
                GL.Vertex(topRightBack);
            }
        }
        #endregion
    }
}