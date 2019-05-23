using System.Collections.Generic;
using UnityEngine;

namespace RendererEditor.Targets
{
    public interface ITargetData
    {
        bool currentEnabled { get; set; }

        IDictionary<Material, MaterialData> dirtyMaterials { get; }
    }
}
