using System;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;
using UnityEngine.Rendering;

namespace RendererEditor.Targets
{
    public struct RendererTarget : ITarget
    {
        private static readonly string[] _shadowCastingModesNames;
        private static readonly string[] _reflectionProbeUsageNames;

        public TargetType targetType { get { return TargetType.Renderer; }}
        public bool enabled { get { return this._target.enabled; } set { this._target.enabled = value; } }
        public string name { get { return this._target.name; } }
        public Material[] sharedMaterials { get { return this._target.sharedMaterials; } }
        public Material[] materials { get { return this._target.materials; } }
        public Transform transform { get { return this._target.transform; } }
        public bool hasBounds { get{ return true; } }
        public Bounds bounds { get { return this._target.bounds; } }
        public Component target { get { return this._target; } }

        private readonly Renderer _target;

        static RendererTarget()
        {
            _shadowCastingModesNames = Enum.GetNames(typeof(ShadowCastingMode));
            _reflectionProbeUsageNames = Enum.GetNames(typeof(ReflectionProbeUsage));
        }

        public RendererTarget(Renderer target)
        {
            this._target = target;
        }

        public void CopyFrom(ITarget other)
        {
            RendererTarget rendererTarget = (RendererTarget)other;

            this._target.shadowCastingMode = rendererTarget._target.shadowCastingMode;
            this._target.receiveShadows = rendererTarget._target.receiveShadows;
            this._target.reflectionProbeUsage = rendererTarget._target.reflectionProbeUsage;
        }

        public void DisplayParams(HashSet<ITarget> selectedTargets, SetDirtyDelegate setDirtyFunction)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Cast Shadows");
            GUILayout.FlexibleSpace();

            bool newReceiveShadows = GUILayout.Toggle(this._target.receiveShadows, "Receive Shadows");
            if (newReceiveShadows != this._target.receiveShadows)
                SetAllTargetsValue(selectedTargets, setDirtyFunction, r => r.receiveShadows = newReceiveShadows);

            GUILayout.EndHorizontal();
            ShadowCastingMode newMode = (ShadowCastingMode)GUILayout.SelectionGrid((int)this._target.shadowCastingMode, _shadowCastingModesNames, 4);
            if (newMode != this._target.shadowCastingMode)
                SetAllTargetsValue(selectedTargets, setDirtyFunction, r => r.shadowCastingMode = newMode);

            GUILayout.Label("Reflection Probe Usage");
            ReflectionProbeUsage newUsage = (ReflectionProbeUsage)GUILayout.SelectionGrid((int)this._target.reflectionProbeUsage, _reflectionProbeUsageNames, 2);
            if (newUsage != this._target.reflectionProbeUsage)
                SetAllTargetsValue(selectedTargets, setDirtyFunction, r => r.reflectionProbeUsage = newUsage);
        }

        public ITargetData GetNewData()
        {
            return new RendererData
            {
                target = this,
                currentEnabled = true,
                originalShadowCastingMode = this._target.shadowCastingMode,
                originalReceiveShadow = this._target.receiveShadows,
                originalReflectionProbeUsage = this._target.reflectionProbeUsage
            };
        }

        public void ResetData(ITargetData data)
        {
            RendererData rendererData = (RendererData)data;
            this._target.shadowCastingMode = rendererData.originalShadowCastingMode;
            this._target.receiveShadows = rendererData.originalReceiveShadow;
            this._target.reflectionProbeUsage = rendererData.originalReflectionProbeUsage;
        }

        public void LoadXml(XmlNode node)
        {
            this._target.shadowCastingMode = (ShadowCastingMode)XmlConvert.ToInt32(node.Attributes["shadowCastingMode"].Value);
            this._target.receiveShadows = XmlConvert.ToBoolean(node.Attributes["receiveShadows"].Value);
            if (node.Attributes["reflectionProbeUsage"] != null)
                this._target.reflectionProbeUsage = (ReflectionProbeUsage)XmlConvert.ToInt32(node.Attributes["reflectionProbeUsage"].Value);
        }

        public void SaveXml(XmlTextWriter writer)
        {
            writer.WriteAttributeString("shadowCastingMode", XmlConvert.ToString((int)this._target.shadowCastingMode));
            writer.WriteAttributeString("receiveShadows", XmlConvert.ToString(this._target.receiveShadows));
            writer.WriteAttributeString("reflectionProbeUsage", XmlConvert.ToString((int)this._target.reflectionProbeUsage));
        }

        private static void SetAllTargetsValue(HashSet<ITarget> targets, SetDirtyDelegate setDirtyFunction, Action<Renderer> setValueFunction)
        {
            foreach (ITarget target in targets)
            {
                Renderer renderer = target.target as Renderer;
                if (renderer == null)
                    continue;
                ITargetData data;
                setDirtyFunction(target, out data);
                setValueFunction(renderer);
            }
        }

        public static implicit operator RendererTarget(Renderer r)
        {
            return new RendererTarget(r);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is RendererTarget))
            {
                return false;
            }

            RendererTarget target = (RendererTarget)obj;
            return EqualityComparer<Renderer>.Default.Equals(this._target, target._target);
        }

        public override int GetHashCode()
        {
            return EqualityComparer<Renderer>.Default.GetHashCode(this._target);
        }
    }

    public class RendererData : ITargetData
    {
        public ITarget target { get; set; }
        public bool currentEnabled { get; set; }
        public IDictionary<Material, MaterialData> dirtyMaterials { get { return this._dirtyMaterials; } }

        public ShadowCastingMode originalShadowCastingMode;
        public bool originalReceiveShadow;
        public ReflectionProbeUsage originalReflectionProbeUsage;

        private readonly Dictionary<Material, MaterialData> _dirtyMaterials = new Dictionary<Material, MaterialData>();
    }
}