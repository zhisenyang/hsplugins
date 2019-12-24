using System.Collections.Generic;
using UnityEngine;

namespace RendererEditor.Targets
{
    public class MaterialData
    {
        public class TextureData
        {
            public Texture originalTexture;
            public string currentTexturePath;
        }

        public ITarget parent;
        public int index;

        public int originalRenderQueue;
        public bool hasRenderQueue = false;
        public string originalRenderType;
        public bool hasRenderType = false;
        public readonly Dictionary<string, Color> dirtyColorProperties = new Dictionary<string, Color>();
        public readonly Dictionary<string, float> dirtyFloatProperties = new Dictionary<string, float>();
        public readonly Dictionary<string, bool> dirtyBooleanProperties = new Dictionary<string, bool>();
        public readonly Dictionary<string, int> dirtyEnumProperties = new Dictionary<string, int>();
        public readonly Dictionary<string, Vector4> dirtyVector4Properties = new Dictionary<string, Vector4>();
        public readonly Dictionary<string, TextureData> dirtyTextureProperties = new Dictionary<string, TextureData>();
        public readonly Dictionary<string, Vector2> dirtyTextureOffsetProperties = new Dictionary<string, Vector2>();
        public readonly Dictionary<string, Vector2> dirtyTextureScaleProperties = new Dictionary<string, Vector2>();
        public readonly HashSet<string> disabledKeywords = new HashSet<string>();
        public readonly HashSet<string> enabledKeywords = new HashSet<string>();
    }
}