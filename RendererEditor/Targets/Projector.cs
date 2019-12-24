using System;
using System.Collections.Generic;
using System.Xml;
using ToolBox;
using ToolBox.Extensions;
using UnityEngine;

namespace RendererEditor.Targets
{
    public struct ProjectorTarget : ITarget
    {
        private static readonly Dictionary<Projector, Material[]> _materialInstances = new Dictionary<Projector, Material[]>();

        public TargetType targetType { get { return TargetType.Projector; }}
        public bool enabled { get { return this._target.enabled; } set { this._target.enabled = value; } }
        public string name { get { return this._target.name; } }
        public Material[] sharedMaterials { get { return this.materials; } }
        public Material[] materials
        {
            get
            {
                Material[] res;
                if (_materialInstances.TryGetValue(this._target, out res) == false) //Emulating the behaviour of a Renderer of having an instanced material
                {
                    this._target.material = new Material(this._target.material);
                    res = new[] { this._target.material };
                    _materialInstances.Add(this._target, res);
                }
                return res;
            }
        }
        public Transform transform { get { return this._target.transform; } }
        public bool hasBounds { get { return false; } }
        public Bounds bounds { get { return default(Bounds); } }
        public Component target { get { return this._target; } }

        private readonly Projector _target;

        public ProjectorTarget(Projector target)
        {
            this._target = target;
        }

        public void CopyFrom(ITarget other)
        {
            ProjectorTarget rendererTarget = (ProjectorTarget)other;

            this._target.nearClipPlane = rendererTarget._target.nearClipPlane;
            this._target.farClipPlane = rendererTarget._target.farClipPlane;
            this._target.aspectRatio = rendererTarget._target.aspectRatio;
            this._target.orthographic = rendererTarget._target.orthographic;
            this._target.orthographicSize = rendererTarget._target.orthographicSize;
            this._target.fieldOfView = rendererTarget._target.fieldOfView;
        }

        public void DisplayParams(HashSet<ITarget> selectedTargets, SetDirtyDelegate setDirtyFunction)
        {
            IMGUIExtensions.HorizontalSliderWithValue("Near Clip Plane\t", this._target.nearClipPlane, 0.01f, 10f, "0.0000", newValue =>
            {
                SetAllTargetsValue(selectedTargets, setDirtyFunction, t => t.nearClipPlane = newValue);
            });

            IMGUIExtensions.HorizontalSliderWithValue("Far Clip Plane\t", this._target.farClipPlane, 0.02f, 100f, "0.0000", newValue =>
            {
                SetAllTargetsValue(selectedTargets, setDirtyFunction, t => t.farClipPlane = newValue);
            });

            IMGUIExtensions.HorizontalSliderWithValue("Aspect Ratio\t", this._target.aspectRatio, 0.1f, 10f, "0.00", newValue =>
            {
                SetAllTargetsValue(selectedTargets, setDirtyFunction, t => t.aspectRatio = newValue);
            });

            bool newOrthographic = GUILayout.Toggle(this._target.orthographic, "Orthographic");
            if (newOrthographic != this._target.orthographic)
                SetAllTargetsValue(selectedTargets, setDirtyFunction, t => t.orthographic = newOrthographic);

            if (newOrthographic)
                IMGUIExtensions.HorizontalSliderWithValue("Orthographic Size\t", this._target.orthographicSize, 0.01f, 10f, "0.00", newValue =>
                {
                    SetAllTargetsValue(selectedTargets, setDirtyFunction, t => t.orthographicSize = newValue);
                });
            else
                IMGUIExtensions.HorizontalSliderWithValue("FOV\t\t", this._target.fieldOfView, 1f, 179f, "0", newValue =>
                {
                    SetAllTargetsValue(selectedTargets, setDirtyFunction, t => t.fieldOfView = newValue);
                });
        }

        public ITargetData GetNewData()
        {
            return new ProjectorData()
            {
                target = this,
                currentEnabled = true,
                originalNearClipPlane = this._target.nearClipPlane,
                originalFarClipPlane = this._target.farClipPlane,
                originalAspectRatio = this._target.aspectRatio,
                originalOrthographic = this._target.orthographic,
                originalOrthographicSize = this._target.orthographicSize,
                originalFieldOfView = this._target.fieldOfView
            };
        }

        public void ResetData(ITargetData data)
        {
            ProjectorData projectorData = (ProjectorData)data;
            this._target.nearClipPlane = projectorData.originalNearClipPlane;
            this._target.farClipPlane = projectorData.originalFarClipPlane;
            this._target.aspectRatio = projectorData.originalAspectRatio;
            this._target.orthographic = projectorData.originalOrthographic;
            this._target.orthographicSize = projectorData.originalOrthographicSize;
            this._target.fieldOfView = projectorData.originalFieldOfView;
        }

        public void LoadXml(XmlNode node)
        {
            this._target.nearClipPlane = XmlConvert.ToSingle(node.Attributes["nearClipPlane"].Value);
            this._target.farClipPlane = XmlConvert.ToSingle(node.Attributes["farClipPlane"].Value);
            this._target.aspectRatio = XmlConvert.ToSingle(node.Attributes["aspectRatio"].Value);
            this._target.orthographic = XmlConvert.ToBoolean(node.Attributes["orthographic"].Value);
            this._target.orthographicSize = XmlConvert.ToSingle(node.Attributes["orthographicSize"].Value);
            this._target.fieldOfView = XmlConvert.ToSingle(node.Attributes["fieldOfView"].Value);
        }

        public void SaveXml(XmlTextWriter writer)
        {
            writer.WriteAttributeString("nearClipPlane", XmlConvert.ToString(this._target.nearClipPlane));
            writer.WriteAttributeString("farClipPlane", XmlConvert.ToString(this._target.farClipPlane));
            writer.WriteAttributeString("aspectRatio", XmlConvert.ToString(this._target.aspectRatio));
            writer.WriteAttributeString("orthographic", XmlConvert.ToString(this._target.orthographic));
            writer.WriteAttributeString("orthographicSize", XmlConvert.ToString(this._target.orthographicSize));
            writer.WriteAttributeString("fieldOfView", XmlConvert.ToString(this._target.fieldOfView));
        }

        private static void SetAllTargetsValue(HashSet<ITarget> targets, SetDirtyDelegate setDirtyFunction, Action<Projector> setValueFunction)
        {
            foreach (ITarget target in targets)
            {
                Projector projector = target.target as Projector;
                if (projector == null)
                    continue;
                ITargetData data;
                setDirtyFunction(target, out data);
                setValueFunction(projector);
            }
        }

        public static implicit operator ProjectorTarget(Projector r)
        {
            return new ProjectorTarget(r);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is ProjectorTarget))
            {
                return false;
            }

            ProjectorTarget target = (ProjectorTarget)obj;
            return EqualityComparer<Projector>.Default.Equals(this._target, target._target);
        }

        public override int GetHashCode()
        {
            return EqualityComparer<Projector>.Default.GetHashCode(this._target);
        }
    }

    public class ProjectorData : ITargetData
    {
        public ITarget target { get; set; }
        public bool currentEnabled { get; set; }
        public IDictionary<Material, MaterialData> dirtyMaterials { get { return this._dirtyMaterials; } }

        public float originalNearClipPlane;
        public float originalFarClipPlane;
        public float originalAspectRatio;
        public bool originalOrthographic;
        public float originalOrthographicSize;
        public float originalFieldOfView;

        private readonly Dictionary<Material, MaterialData> _dirtyMaterials = new Dictionary<Material, MaterialData>();
    }
}