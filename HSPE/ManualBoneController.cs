using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml;
using Manager;
using RootMotion.Demos;
using RootMotion.FinalIK;
using UnityEngine;
using UnityEngine.UI;

namespace HSPE
{
    public class ManualBoneController : MonoBehaviour
    {
        #region Private Variables
        private Animator _animator;
        private FullBodyBipedIK _body;
        private CameraGL _cam;
        private Material _mat;
        private Transform _advancedTarget;
        private readonly Dictionary<FullBodyBipedEffector, int> _effectorToIndex = new Dictionary<FullBodyBipedEffector, int>(); 
        private readonly HashSet<GameObject> _openedGameObjects = new HashSet<GameObject>();
        private bool _advancedCoordWorld = true;
        private bool _advancedCoordPosition = false;
        private Vector2 _advancedScroll;
        private float _inc = 1f;
        private readonly Dictionary<GameObject, KeyValuePair<Vector3, Quaternion>> _dirtyObjects = new Dictionary<GameObject, KeyValuePair<Vector3, Quaternion>>();
        private readonly Dictionary<GameObject, KeyValuePair<Vector3, Quaternion>> _advancedObjects = new Dictionary<GameObject, KeyValuePair<Vector3, Quaternion>>();
        private bool _isFemale = false;
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
        public bool isEnabled { get { return this.chara.ikCtrl.ikEnable; } }
        public bool draw { get; set; }
        #endregion

        #region Unity Methods
        void Awake()
        {
            this._effectorToIndex.Add(FullBodyBipedEffector.Body, 0);
            this._effectorToIndex.Add(FullBodyBipedEffector.LeftThigh, 1);
            this._effectorToIndex.Add(FullBodyBipedEffector.RightThigh, 2);
            this._effectorToIndex.Add(FullBodyBipedEffector.LeftHand, 3);
            this._effectorToIndex.Add(FullBodyBipedEffector.RightHand, 4);
            this._effectorToIndex.Add(FullBodyBipedEffector.LeftFoot, 5);
            this._effectorToIndex.Add(FullBodyBipedEffector.RightFoot, 6);
            this._effectorToIndex.Add(FullBodyBipedEffector.LeftShoulder, 7);
            this._effectorToIndex.Add(FullBodyBipedEffector.RightShoulder, 8);
            this._mat = new Material(Shader.Find("Unlit/Transparent"));
            this._cam = Camera.current.GetComponent<CameraGL>();
            if (this._cam == null)
                this._cam = Camera.current.gameObject.AddComponent<CameraGL>();
            this._cam.onPostRender += DrawGizmos;
            this._body = this.GetComponent<FullBodyBipedIK>();
            this._body.solver.OnPostSolve = OnPostSolve;
            this._animator = this.GetComponent<Animator>();
        }

        void Start()
        {
            this._isFemale = this.chara is StudioFemale;

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
            this.StartCoroutine(this.AfterStart());
        }

        private IEnumerator AfterStart()
        {
            yield return null;
            this.ApplyAdvancedObjects();
        }

        void Update()
        {
            this.CheckIkEnabled();
        }

        void LateUpdate()
        {
            if (!this.isEnabled)
                return;
            for (int i = 0; i < 4; ++i)
                this.chara.ikCtrl.drivingRig.bendGoals[i].weight = 1f;
        }

        void OnDestroy()
        {
            this._cam.onPostRender -= DrawGizmos;
            this._animator.enabled = true;
            this._body.solver.spineStiffness = this._cachedSpineStiffness;
            this._body.solver.pullBodyVertical = this._cachedPullBodyVertical;
            this._body.solver.iterations = this._cachedSolverIterations;
        }
        #endregion

        #region Public Methods
        public void SetBoneTargetRotation(FullBodyBipedEffector type, Quaternion targetRotation)
        { 
            if (this.isEnabled)
                this.chara.ikCtrl.drivingRig.effectorTargets[this._effectorToIndex[type]].target.rotation = targetRotation;
        }

        public Quaternion GetBoneTargetRotation(FullBodyBipedEffector type)
        {
            if (!this.isEnabled)
                return Quaternion.identity;
            return this.chara.ikCtrl.drivingRig.effectorTargets[this._effectorToIndex[type]].target.rotation;
        }

        public void SetBoneTargetPosition(FullBodyBipedEffector type, Vector3 targetPosition)
        {
            if (this.isEnabled)
                this.chara.ikCtrl.drivingRig.effectorTargets[this._effectorToIndex[type]].target.position = targetPosition;
        }

        public Vector3 GetBoneTargetPosition(FullBodyBipedEffector type)
        {
            if (!this.isEnabled)
                return Vector3.zero;
            return this.chara.ikCtrl.drivingRig.effectorTargets[this._effectorToIndex[type]].target.position;
        }

        public void SetBendGoalPosition(FullBodyBipedChain type, Vector3 position)
        {
            if (this.isEnabled)
                this.chara.ikCtrl.drivingRig.bendGoals[(int)type].transform.position = position;
        }

        public Vector3 GetBendGoalPosition(FullBodyBipedChain type)
        {
            if (!this.isEnabled)
                return Vector3.zero;
            return this.chara.ikCtrl.drivingRig.bendGoals[(int)type].transform.position;
        }

        public void CopyLimbToTwin(FullBodyBipedChain limb)
        {
            Transform effectorSrc;
            Transform bendGoalSrc;
            Transform effectorDest;
            Transform bendGoalDest;
            Transform root;
            switch (limb)
            {
                case FullBodyBipedChain.LeftArm:
                    effectorSrc = this.chara.ikCtrl.drivingRig.effectorTargets[this._effectorToIndex[FullBodyBipedEffector.LeftHand]].target;
                    bendGoalSrc = this.chara.ikCtrl.drivingRig.bendGoals[(int)limb].transform;
                    effectorDest = this.chara.ikCtrl.drivingRig.effectorTargets[this._effectorToIndex[FullBodyBipedEffector.RightHand]].target;
                    bendGoalDest = this.chara.ikCtrl.drivingRig.bendGoals[(int)FullBodyBipedChain.RightArm].transform;
                    root = this._body.solver.spineMapping.spineBones[this._body.solver.spineMapping.spineBones.Length - 2];
                    break;
                case FullBodyBipedChain.LeftLeg:
                    effectorSrc = this.chara.ikCtrl.drivingRig.effectorTargets[this._effectorToIndex[FullBodyBipedEffector.LeftFoot]].target;
                    bendGoalSrc = this.chara.ikCtrl.drivingRig.bendGoals[(int)limb].transform;
                    effectorDest = this.chara.ikCtrl.drivingRig.effectorTargets[this._effectorToIndex[FullBodyBipedEffector.RightFoot]].target;
                    bendGoalDest = this.chara.ikCtrl.drivingRig.bendGoals[(int)FullBodyBipedChain.RightLeg].transform;
                    root = this._body.solver.spineMapping.spineBones[0];
                    break;
                case FullBodyBipedChain.RightArm:
                    effectorSrc = this.chara.ikCtrl.drivingRig.effectorTargets[this._effectorToIndex[FullBodyBipedEffector.RightHand]].target;
                    bendGoalSrc = this.chara.ikCtrl.drivingRig.bendGoals[(int)limb].transform;
                    effectorDest = this.chara.ikCtrl.drivingRig.effectorTargets[this._effectorToIndex[FullBodyBipedEffector.LeftHand]].target;
                    bendGoalDest = this.chara.ikCtrl.drivingRig.bendGoals[(int)FullBodyBipedChain.LeftArm].transform;
                    root = this._body.solver.spineMapping.spineBones[this._body.solver.spineMapping.spineBones.Length - 2];
                    break;
                case FullBodyBipedChain.RightLeg:
                    effectorSrc = this.chara.ikCtrl.drivingRig.effectorTargets[this._effectorToIndex[FullBodyBipedEffector.RightFoot]].target;
                    bendGoalSrc = this.chara.ikCtrl.drivingRig.bendGoals[(int)limb].transform;
                    effectorDest = this.chara.ikCtrl.drivingRig.effectorTargets[this._effectorToIndex[FullBodyBipedEffector.LeftFoot]].target;
                    bendGoalDest = this.chara.ikCtrl.drivingRig.bendGoals[(int)FullBodyBipedChain.LeftLeg].transform;
                    root = this._body.solver.spineMapping.spineBones[0];
                    break;
                default:
                    effectorSrc = null;
                    bendGoalSrc = null;
                    effectorDest = null;
                    bendGoalDest = null;
                    root = null;
                    break;
            }

            Vector3 localPos = root.InverseTransformPoint(effectorSrc.position);
            localPos.x *= -1f;
            Vector3 effectorPosition = root.TransformPoint(localPos);

            localPos = root.InverseTransformPoint(bendGoalSrc.position);
            localPos.x *= -1f;
            Vector3 bendGoalPosition = root.TransformPoint(localPos);

            Quaternion rot = effectorSrc.localRotation;
            rot.w *= -1f;
            rot.x *= -1f;

            this.StartCoroutine(this.CopyLibmToTwin_Routine(effectorDest, effectorPosition, rot, bendGoalDest, bendGoalPosition));
        }

        public void AdvancedModeWindow(int id)
        {
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical();
            this._advancedScroll = GUILayout.BeginScrollView(_advancedScroll, GUI.skin.box, GUILayout.ExpandHeight(true));
            GUILayout.Label("Character Tree");
            foreach (Canvas C in FindObjectsOfType<Canvas>())
                this.DisplayObjectTree(C.gameObject, 0);
            this.DisplayObjectTree(this.transform.GetChild(0).gameObject, 0);
            GUILayout.EndScrollView();
            if (this._advancedTarget != null)
                foreach (Component c in this._advancedTarget.GetComponents<Component>())
                    GUILayout.Label(c.GetType().FullName);
            GUILayout.EndVertical();
            GUILayout.BeginVertical(GUI.skin.box, GUILayout.MinWidth(150f), GUILayout.MaxWidth(270f));
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
                    if (GUILayout.Button("Reset Local Pos."))
                        this._advancedTarget.localPosition = this._dirtyObjects[this._advancedTarget.gameObject].Key;
                    if (GUILayout.Button("Reset Local Rot."))
                        this._advancedTarget.localRotation = this._dirtyObjects[this._advancedTarget.gameObject].Value;
                    if (GUILayout.Button("Reset Both"))
                    {
                        this._advancedTarget.localPosition = this._dirtyObjects[this._advancedTarget.gameObject].Key;
                        this._advancedTarget.localRotation = this._dirtyObjects[this._advancedTarget.gameObject].Value;
                    }
                    GUILayout.EndHorizontal();
                }
            }
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
            GUI.DragWindow();
        }
        #endregion

        #region Private Methods

        private IEnumerator CopyLibmToTwin_Routine(Transform effector, Vector3 effectorTargetPos, Quaternion effectorTargetRot, Transform bendGoal, Vector3 bendGoalTargetPos)
        {
            float startTime = Time.unscaledTime - Time.unscaledDeltaTime;
            Vector3 effectorStartPos = effector.position;
            Vector3 bendGoalStartPos = bendGoal.position;
            Quaternion effectorStartRot = effector.localRotation;
            while (Time.unscaledTime - startTime < 0.1f)
            {
                float v = (Time.unscaledTime - startTime) * 10f;
                effector.position = Vector3.Lerp(effectorStartPos, effectorTargetPos, v);
                effector.localRotation = Quaternion.Lerp(effectorStartRot, effectorTargetRot, v);
                bendGoal.position = Vector3.Lerp(bendGoalStartPos, bendGoalTargetPos, v);
                yield return null;
            }
            effector.position = effectorTargetPos;
            effector.localRotation = effectorTargetRot;
            bendGoal.position = bendGoalTargetPos;
        }

        private void CheckIkEnabled()
        {
            if (this._animator.enabled == this.isEnabled)
               this._animator.enabled = !this.isEnabled;
        }

        private void DisplayObjectTree(GameObject go, int indent)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(indent * 20f);
            if (go.transform.childCount != 0)
            {
                if (GUILayout.Toggle(this._openedGameObjects.Contains(go), "", GUILayout.ExpandWidth(false)))
                {
                    if (this._openedGameObjects.Contains(go) == false)
                        this._openedGameObjects.Add(go);
                }
                else
                {
                    if (this._openedGameObjects.Contains(go))
                        this._openedGameObjects.Remove(go);
                }
            }
            else
                GUILayout.Space(21f);
            Color c = GUI.color;
            if (this._dirtyObjects.ContainsKey(go))
                GUI.color = Color.magenta;
            if (_advancedTarget == go.transform)
                GUI.color = Color.cyan;
            if (GUILayout.Button(go.name + (this.IsObjectDirty(go) ? "*" : ""), GUILayout.ExpandWidth(false)))
            {
                if (this._advancedTarget != null && this.IsObjectDirty(this._advancedTarget.gameObject) == false)
                    this._dirtyObjects.Remove(this._advancedTarget.gameObject);
                _advancedTarget = go.transform;
                if (this._dirtyObjects.ContainsKey(go) == false)
                    this._dirtyObjects.Add(go, new KeyValuePair<Vector3, Quaternion>(go.transform.localPosition, go.transform.localRotation));
            }
            GUI.color = c;
            GUILayout.EndHorizontal();
            if (this._openedGameObjects.Contains(go))
                for (int i = 0; i < go.transform.childCount; ++i)
                    this.DisplayObjectTree(go.transform.GetChild(i).gameObject, indent + 1);
        }

        private bool IsObjectDirty(GameObject go)
        {
            if (this._dirtyObjects.ContainsKey(go))
            {
                KeyValuePair<Vector3, Quaternion> tr = this._dirtyObjects[go];
                return (go.transform.localPosition != tr.Key || go.transform.localRotation != tr.Value);
            }
            return false;
        }


        private void OnPostSolve()
        {
            if (!this.isEnabled)
                return;
            this.StartCoroutine(this.AfterOnPostSolve());
        }

        private IEnumerator AfterOnPostSolve()
        {
            yield return new WaitForEndOfFrame();
            this._legKneeDamL.rotation = Quaternion.Lerp(this._body.solver.leftLegMapping.bone1.rotation, this._body.solver.leftLegMapping.bone2.rotation, 0.5f);
            this._legKneeDamR.rotation = Quaternion.Lerp(this._body.solver.rightLegMapping.bone1.rotation, this._body.solver.rightLegMapping.bone2.rotation, 0.5f);
            this._legKneeBackL.rotation = this._body.solver.leftLegMapping.bone2.rotation;
            this._legKneeBackR.rotation = this._body.solver.rightLegMapping.bone2.rotation;
            this._legUpL.rotation = Quaternion.Lerp(this._legUpL.parent.rotation, this._body.solver.leftThighEffector.bone.rotation, 0.85f);
            this._legUpR.rotation = Quaternion.Lerp(this._legUpR.parent.rotation, this._body.solver.rightThighEffector.bone.rotation, 0.85f);
            this._elbowDamL.rotation = Quaternion.Lerp(this._elbowDamL.parent.rotation, this._body.solver.leftArmMapping.bone2.rotation, 0.65f);
            this._elbowDamR.rotation = Quaternion.Lerp(this._elbowDamR.parent.rotation, this._body.solver.rightArmMapping.bone2.rotation, 0.65f);
        }

        private void ApplyAdvancedObjects()
        {
            foreach (KeyValuePair<GameObject, KeyValuePair<Vector3, Quaternion>> kvp in this._advancedObjects)
            {
                kvp.Key.transform.localPosition = kvp.Value.Key;
                kvp.Key.transform.localRotation = kvp.Value.Value;
            }
            this._advancedObjects.Clear();
        }

        private void DrawGizmos()
        {
            if (!this.draw || !this.isEnabled)
                return;
            if (!(this._advancedTarget is RectTransform))
            {
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
            else
            {
                RectTransform rt = this._advancedTarget as RectTransform;
                GL.PushMatrix();
                this._mat.SetPass(0);
                GL.LoadPixelMatrix();
                GL.Begin(GL.LINES);
                Vector2 size = Vector2.Scale(rt.rect.size, rt.lossyScale);
                float x = rt.position.x + rt.anchoredPosition.x;
                float y = Screen.height - rt.position.y - rt.anchoredPosition.y;
                Rect r = new Rect(x, y, size.x, size.y);
                //r.position += new Vector2(Screen.width / 2f, Screen.height / 2f);
                GL.Vertex(r.min);
                GL.Vertex(new Vector2(r.max.x, r.min.y));

                GL.Vertex(new Vector2(r.max.x, r.min.y));
                GL.Vertex(r.max);

                GL.Vertex(r.max);
                GL.Vertex(new Vector2(r.min.x, r.max.y));

                GL.Vertex(new Vector2(r.min.x, r.max.y));
                GL.Vertex(r.min);

                GL.End();
                GL.PopMatrix();
                
            }
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

        #region Saves
        public void LoadBinary(BinaryReader binaryReader)
        {
            this.LoadVersion_1_0_0(binaryReader);
        }

        public void LoadXml(XmlReader xmlReader, HSPE.VersionNumber v)
        {
            this.LoadDefaultVersion(xmlReader);
        }
        public int SaveXml(XmlTextWriter xmlWriter)
        {
            xmlWriter.WriteStartElement("advancedObjects");
            xmlWriter.WriteAttributeString("count", this._dirtyObjects.Count.ToString());
            foreach (KeyValuePair<GameObject, KeyValuePair<Vector3, Quaternion>> kvp in this._dirtyObjects)
            {
                Transform t = kvp.Key.transform.parent;
                string n = kvp.Key.transform.name;
                while (t != this.transform)
                {
                    n = t.name + "/" + n;
                    t = t.parent;
                }
                xmlWriter.WriteStartElement("object");
                xmlWriter.WriteAttributeString("name", n);

                xmlWriter.WriteAttributeString("originalPosX", XmlConvert.ToString(kvp.Value.Key.x));
                xmlWriter.WriteAttributeString("originalPosY", XmlConvert.ToString(kvp.Value.Key.y));
                xmlWriter.WriteAttributeString("originalPosZ", XmlConvert.ToString(kvp.Value.Key.z));

                xmlWriter.WriteAttributeString("originalRotW", XmlConvert.ToString(kvp.Value.Value.w));
                xmlWriter.WriteAttributeString("originalRotX", XmlConvert.ToString(kvp.Value.Value.x));
                xmlWriter.WriteAttributeString("originalRotY", XmlConvert.ToString(kvp.Value.Value.y));
                xmlWriter.WriteAttributeString("originalRotZ", XmlConvert.ToString(kvp.Value.Value.z));

                xmlWriter.WriteAttributeString("posX", XmlConvert.ToString(kvp.Key.transform.localPosition.x));
                xmlWriter.WriteAttributeString("posY", XmlConvert.ToString(kvp.Key.transform.localPosition.y));
                xmlWriter.WriteAttributeString("posZ", XmlConvert.ToString(kvp.Key.transform.localPosition.z));

                xmlWriter.WriteAttributeString("rotW", XmlConvert.ToString(kvp.Key.transform.localRotation.w));
                xmlWriter.WriteAttributeString("rotX", XmlConvert.ToString(kvp.Key.transform.localRotation.x));
                xmlWriter.WriteAttributeString("rotY", XmlConvert.ToString(kvp.Key.transform.localRotation.y));
                xmlWriter.WriteAttributeString("rotZ", XmlConvert.ToString(kvp.Key.transform.localRotation.z));

                xmlWriter.WriteEndElement();
            }
            xmlWriter.WriteEndElement();
            return this._dirtyObjects.Count;
        }

        private void LoadVersion_1_0_0(BinaryReader binaryReader)
        {
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

                this.SetBoneTargetPosition((FullBodyBipedEffector)i, pos);
                this.SetBoneTargetRotation((FullBodyBipedEffector)i, rot);
            }
            binaryReader.ReadInt32();
            for (int i = 0; i < 4; ++i)
            {
                Vector3 pos;

                pos.x = binaryReader.ReadSingle();
                pos.y = binaryReader.ReadSingle();
                pos.z = binaryReader.ReadSingle();

                this.SetBendGoalPosition((FullBodyBipedChain)i, pos);
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
                this._dirtyObjects.Add(obj, new KeyValuePair<Vector3, Quaternion>(pos, rot));

                pos.x = binaryReader.ReadSingle();
                pos.y = binaryReader.ReadSingle();
                pos.z = binaryReader.ReadSingle();

                rot.w = binaryReader.ReadSingle();
                rot.x = binaryReader.ReadSingle();
                rot.y = binaryReader.ReadSingle();
                rot.z = binaryReader.ReadSingle();
                this._advancedObjects.Add(obj, new KeyValuePair<Vector3, Quaternion>(pos, rot));
            }
        }

        private void LoadDefaultVersion(XmlReader xmlReader)
        {
            bool shouldContinue;
            while ((shouldContinue = xmlReader.Read()) == true && xmlReader.NodeType == XmlNodeType.Whitespace)
                ;
            if (xmlReader.NodeType != XmlNodeType.Element || xmlReader.Name != "advancedObjects")
                return;
            int count = XmlConvert.ToInt32(xmlReader.GetAttribute("count"));
            if (!shouldContinue)
                return;
            int i = 0;
            while (i < count && xmlReader.Read())
            {
                if (xmlReader.NodeType == XmlNodeType.Element)
                {
                    if (xmlReader.Name == "object")
                    {
                        string name = xmlReader.GetAttribute("name");
                        GameObject obj = this.transform.Find(name).gameObject;
                        if (xmlReader.GetAttribute("posX") != null && xmlReader.GetAttribute("posY") != null && xmlReader.GetAttribute("posZ") != null &&
                            xmlReader.GetAttribute("rotW") != null && xmlReader.GetAttribute("rotX") != null && xmlReader.GetAttribute("rotY") != null && xmlReader.GetAttribute("rotZ") != null)
                        {
                            Vector3 pos;
                            Quaternion rot;
                            pos.x = XmlConvert.ToSingle(xmlReader.GetAttribute("posX"));
                            pos.y = XmlConvert.ToSingle(xmlReader.GetAttribute("posY"));
                            pos.z = XmlConvert.ToSingle(xmlReader.GetAttribute("posZ"));

                            rot.w = XmlConvert.ToSingle(xmlReader.GetAttribute("rotW"));
                            rot.x = XmlConvert.ToSingle(xmlReader.GetAttribute("rotX"));
                            rot.y = XmlConvert.ToSingle(xmlReader.GetAttribute("rotY"));
                            rot.z = XmlConvert.ToSingle(xmlReader.GetAttribute("rotZ"));
                            if (xmlReader.GetAttribute("originalPosX") != null && xmlReader.GetAttribute("originalPosY") != null && xmlReader.GetAttribute("originalPosZ") != null &&
                                xmlReader.GetAttribute("originalRotW") != null && xmlReader.GetAttribute("originalRotX") != null && xmlReader.GetAttribute("originalRotY") != null && xmlReader.GetAttribute("originalRotZ") != null)
                            {
                                Vector3 originalPos;
                                Quaternion originalRot;
                                originalPos.x = XmlConvert.ToSingle(xmlReader.GetAttribute("originalPosX"));
                                originalPos.y = XmlConvert.ToSingle(xmlReader.GetAttribute("originalPosY"));
                                originalPos.z = XmlConvert.ToSingle(xmlReader.GetAttribute("originalPosZ"));

                                originalRot.w = XmlConvert.ToSingle(xmlReader.GetAttribute("originalRotW"));
                                originalRot.x = XmlConvert.ToSingle(xmlReader.GetAttribute("originalRotX"));
                                originalRot.y = XmlConvert.ToSingle(xmlReader.GetAttribute("originalRotY"));
                                originalRot.z = XmlConvert.ToSingle(xmlReader.GetAttribute("originalRotZ"));
                                this._dirtyObjects.Add(obj, new KeyValuePair<Vector3, Quaternion>(originalPos, originalRot));
                            }
                            else
                                this._dirtyObjects.Add(obj, new KeyValuePair<Vector3, Quaternion>(obj.transform.localPosition, obj.transform.localRotation));
                            this._advancedObjects.Add(obj, new KeyValuePair<Vector3, Quaternion>(pos, rot));
                        }
                        ++i;
                    }
                }
            }
        }
        #endregion
    }
}