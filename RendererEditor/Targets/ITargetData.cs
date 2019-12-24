using System.Collections.Generic;
using UnityEngine;

namespace RendererEditor.Targets
{
    public interface ITargetData
    {
        ITarget target { get; set; }
        bool currentEnabled { get; set; }
        IDictionary<Material, MaterialData> dirtyMaterials { get; }
    }
}
