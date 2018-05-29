using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml;
using HSPE.AMModules;
using RootMotion.FinalIK;
using Studio;
using UnityEngine;

namespace HSPE
{
    public class PoseController : MonoBehaviour
    {
        #region Constants
        private static readonly Dictionary<FullBodyBipedEffector, int> _effectorToIndex = new Dictionary<FullBodyBipedEffector, int>()
        {
            { FullBodyBipedEffector.Body, 0 },
            { FullBodyBipedEffector.LeftShoulder, 1 },
            { FullBodyBipedEffector.LeftHand, 3 },
            { FullBodyBipedEffector.RightShoulder, 4 },
            { FullBodyBipedEffector.RightHand, 6 },
            { FullBodyBipedEffector.LeftThigh, 7 },
            { FullBodyBipedEffector.LeftFoot, 9 },
            { FullBodyBipedEffector.RightThigh, 10 },
            { FullBodyBipedEffector.RightFoot, 12 }
        };
        private static readonly Dictionary<FullBodyBipedChain, int> _chainToIndex = new Dictionary<FullBodyBipedChain, int>()
        {
            { FullBodyBipedChain.LeftArm, 2 },
            { FullBodyBipedChain.RightArm, 5 },
            { FullBodyBipedChain.LeftLeg, 8 },
            { FullBodyBipedChain.RightLeg, 11 }
        };
        #endregion

        #region Private Types

        private delegate void TabDelegate();
        #endregion

        #region Public Types
        public enum DragType
        {
            None,
            Position,
            Rotation,
            Both
        }
        #endregion

        #region Private Variables
        private FullBodyBipedIK _body;
        private float _cachedSpineStiffness;
        private float _cachedPullBodyVertical;
        private readonly Dictionary<int, Vector3> _oldPosValues = new Dictionary<int, Vector3>();
        private readonly Dictionary<int, Vector3> _oldRotValues = new Dictionary<int, Vector3>();
        private bool _lockDrag = false;
        private bool _optimizeIK = true;
        private bool _isFemale;
        private OCIChar _chara;

        private BonesEditor _bonesEditor;
        private BoobsEditor _boobsEditor;
        private DynamicBonesEditor _dynamicBonesEditor;
        private BlendShapesEditor _blendShapesEditor;
        private readonly List<AdvancedModeModule> _modules = new List<AdvancedModeModule>();
        private AdvancedModeModule _currentModule;
        private bool _drawAdvancedMode = false;
        #endregion

        #region Public Accessors
        public bool isIKEnabled { get { return this._chara.oiCharInfo.enableIK; } }
        public bool drawAdvancedMode
        {
            get { return this._drawAdvancedMode; }
            set
            {
                this._drawAdvancedMode = value;
                foreach (AdvancedModeModule module in this._modules)
                    module.drawAdvancedMode = value;
            }
        }
        public DragType currentDragType { get; private set; }
        public bool optimizeIK
        {
            get { return this._optimizeIK; }
            set
            {
                this._optimizeIK = value;
                if (this._body != null)
                {
                    if (value)
                    {
                        this._body.solver.spineStiffness = 0f;
                        this._body.solver.pullBodyVertical = 0f;
                    }
                    else
                    {
                        this._body.solver.spineStiffness = this._cachedSpineStiffness;
                        this._body.solver.pullBodyVertical = this._cachedPullBodyVertical;
                    }
                }
            }
        }
        public bool colliderEditEnabled { get { return this._bonesEditor.colliderEditEnabled; } }
        public Rect colliderEditRect { get { return this._bonesEditor.colliderEditRect; } }
        public bool isDraggingDynamicBone { get { return this._dynamicBonesEditor.isDraggingDynamicBone || this._boobsEditor != null && this._boobsEditor.isDraggingDynamicBone; } }
        #endregion

        #region Unity Methods
        void Awake()
        {
            foreach (KeyValuePair<int, ObjectCtrlInfo> pair in Studio.Studio.Instance.dicObjectCtrl)
            {
                OCIChar ociChar = (OCIChar)pair.Value;
                if (ociChar.charInfo.gameObject == this.gameObject)
                {
                    this._chara = ociChar;
                    break;
                }
            }
            this._isFemale = this._chara.charInfo.Sex == 1;
            this._body = this._chara.animeIKCtrl.IK;

            this._bonesEditor = this.gameObject.AddComponent<BonesEditor>();
            this._bonesEditor.chara = this._chara;
            this._modules.Add(this._bonesEditor);
            if (this._isFemale)
            {
                this._boobsEditor = this.gameObject.AddComponent<BoobsEditor>();
                this._boobsEditor.chara = this._chara;
                this._modules.Add(this._boobsEditor);
            }
            this._dynamicBonesEditor = this.gameObject.AddComponent<DynamicBonesEditor>();
            this._modules.Add(this._dynamicBonesEditor);
            this._blendShapesEditor = this.gameObject.AddComponent<BlendShapesEditor>();
            this._blendShapesEditor.chara = this._chara;
            this._modules.Add(this._blendShapesEditor);

            this._currentModule = this._bonesEditor;
            this._currentModule.isEnabled = true;
            if (this._chara == null)
                UnityEngine.Debug.LogError("chara is null, this should not happen");
        }

        void Start()
        {
            this._cachedSpineStiffness = this._body.solver.spineStiffness;
            this._cachedPullBodyVertical = this._body.solver.pullBodyVertical;

            if (this._optimizeIK)
            {
                this._body.solver.spineStiffness = 0f;
                this._body.solver.pullBodyVertical = 0f;
            }
            else
            {
                this._body.solver.spineStiffness = this._cachedSpineStiffness;
                this._body.solver.pullBodyVertical = this._cachedPullBodyVertical;
            }
        }

        void OnDestroy()
        {
            this._body.solver.spineStiffness = this._cachedSpineStiffness;
            this._body.solver.pullBodyVertical = this._cachedPullBodyVertical;
        }
        #endregion

        #region Public Methods
        public void LoadFrom(PoseController other)
        {
            if (other == null)
                return;
            this._bonesEditor.LoadFrom(other._bonesEditor);
            if (this._isFemale)
                this._boobsEditor.LoadFrom(other._boobsEditor);
            this._dynamicBonesEditor.LoadFrom(other._dynamicBonesEditor);
            this._blendShapesEditor.LoadFrom(other._blendShapesEditor);
        }

        public void StartDrag(DragType dragType)
        {
            if (this._lockDrag)
                return;
            this.currentDragType = dragType;
        }

        public void StopDrag()
        {
            if (this._lockDrag)
                return;
            GuideCommand.EqualsInfo[] moveCommands = new GuideCommand.EqualsInfo[this._oldPosValues.Count];
            if (this.currentDragType == DragType.Position || this.currentDragType == DragType.Both)
            {
                int i = 0;
                foreach (KeyValuePair<int, Vector3> kvp in this._oldPosValues)
                {
                    moveCommands[i] = new GuideCommand.EqualsInfo()
                    {
                        dicKey = kvp.Key,
                        oldValue = kvp.Value,
                        newValue = Studio.Studio.Instance.dicChangeAmount[kvp.Key].pos
                    };
                    ++i;
                }
            }
            GuideCommand.EqualsInfo[] rotateCommands = new GuideCommand.EqualsInfo[this._oldRotValues.Count];
            if (this.currentDragType == DragType.Rotation || this.currentDragType == DragType.Both)
            {
                int i = 0;
                foreach (KeyValuePair<int, Vector3> kvp in this._oldRotValues)
                {
                    rotateCommands[i] = new GuideCommand.EqualsInfo()
                    {
                        dicKey = kvp.Key,
                        oldValue = kvp.Value,
                        newValue = Studio.Studio.Instance.dicChangeAmount[kvp.Key].rot
                    };
                    ++i;
                }
            }
            UndoRedoManager.Instance.Push(new Commands.MoveRotateEqualsCommand(moveCommands, rotateCommands));
            this.currentDragType = DragType.None;
            this._oldPosValues.Clear();
            this._oldRotValues.Clear();
        }

        public bool IsPartEnabled(FullBodyBipedEffector part)
        {
            return this.isIKEnabled && this._chara.listIKTarget[_effectorToIndex[part]].active;
        }

        public bool IsPartEnabled(FullBodyBipedChain part)
        {
            return this.isIKEnabled && this._chara.listIKTarget[_chainToIndex[part]].active;
        }

        public void SetBoneTargetRotation(FullBodyBipedEffector type, Quaternion targetRotation)
        {
            if (this.isIKEnabled && this._chara.listIKTarget[_effectorToIndex[type]].active)
            {
                GuideObject target = this._chara.listIKTarget[_effectorToIndex[type]].guideObject;
                if (this.currentDragType != DragType.None)
                {
                    if (this._oldRotValues.ContainsKey(target.dicKey) == false)
                        this._oldRotValues.Add(target.dicKey, target.changeAmount.rot);
                    target.changeAmount.rot = targetRotation.eulerAngles;
                }
            }
        }

        public Quaternion GetBoneTargetRotation(FullBodyBipedEffector type)
        {
            if (!this.isIKEnabled || this._chara.listIKTarget[_effectorToIndex[type]].active == false)
                return Quaternion.identity;
            return this._chara.listIKTarget[_effectorToIndex[type]].guideObject.transformTarget.localRotation;
        }

        public void SetBoneTargetPosition(FullBodyBipedEffector type, Vector3 targetPosition, bool world = true)
        {
            if (this.isIKEnabled && this._chara.listIKTarget[_effectorToIndex[type]].active)
            {
                GuideObject target = this._chara.listIKTarget[_effectorToIndex[type]].guideObject;
                if (this.currentDragType != DragType.None)
                {
                    if (this._oldPosValues.ContainsKey(target.dicKey) == false)
                        this._oldPosValues.Add(target.dicKey, target.changeAmount.pos);
                    if (world)
                        target.changeAmount.pos = target.transformTarget.parent.InverseTransformPoint(targetPosition);
                    else
                        target.changeAmount.pos = targetPosition;
                }
            }
        }

        public Vector3 GetBoneTargetPosition(FullBodyBipedEffector type, bool world = true)
        {
            if (!this.isIKEnabled || this._chara.listIKTarget[_effectorToIndex[type]].active == false)
                return Vector3.zero;
            if (world)
                return this._chara.listIKTarget[_effectorToIndex[type]].guideObject.transformTarget.position;
            return this._chara.listIKTarget[_effectorToIndex[type]].guideObject.transformTarget.localPosition;
        }

        public void SetBendGoalPosition(FullBodyBipedChain type, Vector3 targetPosition, bool world = true)
        {
            if (this.isIKEnabled && this._chara.listIKTarget[_chainToIndex[type]].active)
            {
                GuideObject target = this._chara.listIKTarget[_chainToIndex[type]].guideObject;
                if (this.currentDragType != DragType.None)
                {
                    if (this._oldPosValues.ContainsKey(target.dicKey) == false)
                        this._oldPosValues.Add(target.dicKey, target.changeAmount.pos);
                    if (world)
                        target.changeAmount.pos = target.transformTarget.parent.InverseTransformPoint(targetPosition);
                    else
                        target.changeAmount.pos = targetPosition;
                }
            }
        }

        public Vector3 GetBendGoalPosition(FullBodyBipedChain type, bool world = true)
        {
            if (!this.isIKEnabled || this._chara.listIKTarget[_chainToIndex[type]].active == false)
                return Vector3.zero;
            if (world)
                return this._chara.listIKTarget[_chainToIndex[type]].guideObject.transformTarget.position;
            return this._chara.listIKTarget[_chainToIndex[type]].guideObject.transformTarget.localPosition;
        }

        public void CopyLimbToTwin(FullBodyBipedChain limb)
        {
            FullBodyBipedEffector effectorSrc;
            FullBodyBipedChain bendGoalSrc;
            FullBodyBipedEffector effectorDest;
            FullBodyBipedChain bendGoalDest;
            Transform effectorSrcRealBone;
            Transform effectorDestRealBone;
            Transform root;
            switch (limb)
            {
                case FullBodyBipedChain.LeftArm:
                    effectorSrc = FullBodyBipedEffector.LeftHand;
                    bendGoalSrc = FullBodyBipedChain.LeftArm;
                    effectorDest = FullBodyBipedEffector.RightHand;
                    bendGoalDest = FullBodyBipedChain.RightArm;
                    effectorSrcRealBone = this._body.references.leftHand;
                    effectorDestRealBone = this._body.references.rightHand;
                    root = this._body.solver.spineMapping.spineBones[this._body.solver.spineMapping.spineBones.Length - 2];
                    break;
                case FullBodyBipedChain.LeftLeg:
                    effectorSrc = FullBodyBipedEffector.LeftFoot;
                    bendGoalSrc = FullBodyBipedChain.LeftLeg;
                    effectorDest = FullBodyBipedEffector.RightFoot;
                    bendGoalDest = FullBodyBipedChain.RightLeg;
                    effectorSrcRealBone = this._body.references.leftFoot;
                    effectorDestRealBone = this._body.references.rightFoot;
                    root = this._body.solver.spineMapping.spineBones[0];
                    break;
                case FullBodyBipedChain.RightArm:
                    effectorSrc = FullBodyBipedEffector.RightHand;
                    bendGoalSrc = FullBodyBipedChain.RightArm;
                    effectorDest = FullBodyBipedEffector.LeftHand;
                    bendGoalDest = FullBodyBipedChain.LeftArm;
                    effectorSrcRealBone = this._body.references.rightHand;
                    effectorDestRealBone = this._body.references.leftHand;
                    root = this._body.solver.spineMapping.spineBones[this._body.solver.spineMapping.spineBones.Length - 2];
                    break;
                case FullBodyBipedChain.RightLeg:
                    effectorSrc = FullBodyBipedEffector.RightFoot;
                    bendGoalSrc = FullBodyBipedChain.RightLeg;
                    effectorDest = FullBodyBipedEffector.LeftFoot;
                    bendGoalDest = FullBodyBipedChain.LeftLeg;
                    effectorSrcRealBone = this._body.references.rightFoot;
                    effectorDestRealBone = this._body.references.leftFoot;
                    root = this._body.solver.spineMapping.spineBones[0];
                    break;
                default:
                    effectorSrc = FullBodyBipedEffector.RightHand;
                    effectorDest = FullBodyBipedEffector.RightHand;
                    bendGoalSrc = FullBodyBipedChain.LeftArm;
                    bendGoalDest = FullBodyBipedChain.LeftArm;
                    effectorSrcRealBone = null;
                    effectorDestRealBone = null;
                    root = null;
                    break;
            }

            Vector3 localPos = root.InverseTransformPoint(this._chara.listIKTarget[_effectorToIndex[effectorSrc]].guideObject.transformTarget.position);
            localPos.x *= -1f;
            Vector3 effectorPosition = root.TransformPoint(localPos);
            localPos = root.InverseTransformPoint(this._chara.listIKTarget[_chainToIndex[bendGoalSrc]].guideObject.transformTarget.position);
            localPos.x *= -1f;
            Vector3 bendGoalPosition = root.TransformPoint(localPos);
            this.StartDrag(DragType.Both);
            this._lockDrag = true;

            this.SetBoneTargetPosition(effectorDest, effectorPosition);
            this.SetBendGoalPosition(bendGoalDest, bendGoalPosition);
            this.ExecuteDelayed(() =>
            {
                Quaternion rot = effectorSrcRealBone.localRotation;
                rot.Set(-rot.x, rot.y, rot.z, -rot.w);
                rot = effectorDestRealBone.parent.rotation * rot;
                rot *= Quaternion.Inverse(this._chara.listIKTarget[_effectorToIndex[effectorDest]].guideObject.transformTarget.parent.rotation);

                this.SetBoneTargetRotation(effectorDest, rot);

                this._lockDrag = false;
                this.StopDrag();
            });
        }

        public void AdvancedModeWindow(int id)
        {
            GUILayout.BeginHorizontal();
            foreach (AdvancedModeModule module in this._modules)
            {
                if (GUILayout.Button(module.displayName))
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
                this.drawAdvancedMode = false;
            GUI.color = c;
            GUILayout.EndHorizontal();
            this._currentModule.GUILogic();
            GUI.DragWindow();
        }
        #endregion

        #region Private Methods

        #endregion

        #region Saves
        public void ScheduleLoad(XmlNode node, string v)
        {
            this.StartCoroutine(this.LoadDefaultVersion_Routine(node.CloneNode(true)));
        }

        public int SaveXml(XmlTextWriter xmlWriter)
        {
            int written = 0;
            foreach (AdvancedModeModule module in this._modules)
                written += module.SaveXml(xmlWriter);
            return written;
        }

        private IEnumerator LoadDefaultVersion_Routine(XmlNode xmlNode)
        {
            yield return null;
            yield return null;
            yield return null;
            foreach (AdvancedModeModule module in this._modules)
            {
                module.LoadXml(xmlNode);
            }
        }
        #endregion
    }
}