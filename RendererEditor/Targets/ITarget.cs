using System.Collections.Generic;
using System.Xml;
using UnityEngine;

namespace RendererEditor.Targets
{
    public delegate bool SetDirtyDelegate(ITarget target, out ITargetData data);

    public interface ITarget
    {
        TargetType targetType {get;}
        bool enabled { get; set; }
        string name { get; }
        Material[] sharedMaterials { get; }
        Material[] materials { get; }
        Transform transform { get; }
        bool hasBounds { get; }
        Bounds bounds { get; }
        Component target { get; }

        void CopyFrom(ITarget target);
        void DisplayParams(HashSet<ITarget> selectedTargets);
        ITargetData GetNewData();
        void ResetData(ITargetData data);
        void LoadXml(XmlNode node);
        void SaveXml(XmlTextWriter writer);
    }

    public enum TargetType
    {
        Renderer,
        Projector
    }
}
