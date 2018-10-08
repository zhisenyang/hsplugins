using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using Studio;
using UnityEngine;
using ToolBox;
using UnityEngine.Rendering;
using Vectrosity;

namespace RendererEditor
{
    public class MainWindow : MonoBehaviour
    {
        #region Private Types
        private class RendererData
        {
            public class MaterialData
            {
                public class TextureData
                {
                    public Texture originalTexture;
                    public string currentTexturePath;
                }

                public int originalRenderQueue;
                public bool hasRenderQueue = false;
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

            public bool enabled;
            public ShadowCastingMode shadowCastingMode;
            public bool receiveShadow;
            public readonly Dictionary<Material, MaterialData> dirtyMaterials = new Dictionary<Material, MaterialData>();
        }

        private class ShaderProperty
        {
            public enum Type
            {
                Color,
                Texture,
                Float,
                Boolean,
                Enum,
                Vector4
            }

            public string name;
            public Type type;

            public bool hasFloatRange;
            public Vector2 floatRange = new Vector2(0f, 1f);
            public Dictionary<int, string> enumValues;
        }

        private class MaterialInfo
        {
            public Renderer renderer;
            public int index;
        }
        #endregion

        #region Private Variables
        private const string _texturesDir = "Plugins\\RendererEditor\\Textures";
        private const string _dumpDir = _texturesDir + "Dump\\";

        private readonly List<ShaderProperty> _shaderProperties = new List<ShaderProperty>()
        {
            //Color
            new ShaderProperty() {name = "_Color", type = ShaderProperty.Type.Color},
            new ShaderProperty() {name = "_Color_2", type = ShaderProperty.Type.Color},
            new ShaderProperty() {name = "_Color_3", type = ShaderProperty.Type.Color},
            new ShaderProperty() {name = "_Color_4", type = ShaderProperty.Type.Color},
            new ShaderProperty() {name = "_SpecColor", type = ShaderProperty.Type.Color},
            new ShaderProperty() {name = "_SpecColor_2", type = ShaderProperty.Type.Color},
            new ShaderProperty() {name = "_SpecColor_3", type = ShaderProperty.Type.Color},
            new ShaderProperty() {name = "_SpecColor_4", type = ShaderProperty.Type.Color},
            new ShaderProperty() {name = "_EmissionColor", type = ShaderProperty.Type.Color},
            new ShaderProperty() {name = "_BaseColor", type = ShaderProperty.Type.Color},
            new ShaderProperty() {name = "_ReflectionColor", type = ShaderProperty.Type.Color},
            new ShaderProperty() {name = "_SpecularColor", type = ShaderProperty.Type.Color},
            //Textures
            new ShaderProperty() {name = "_MainTex", type = ShaderProperty.Type.Texture},
            new ShaderProperty() {name = "_SpecGlossMap", type = ShaderProperty.Type.Texture},
            new ShaderProperty() {name = "_SpecGlossMap2", type = ShaderProperty.Type.Texture},
            new ShaderProperty() {name = "_BumpMap", type = ShaderProperty.Type.Texture},
            new ShaderProperty() {name = "_BumpMap2", type = ShaderProperty.Type.Texture},
            new ShaderProperty() {name = "_OcclusionMap", type = ShaderProperty.Type.Texture},
            new ShaderProperty() {name = "_BlendNormalMap", type = ShaderProperty.Type.Texture},
            new ShaderProperty() {name = "_DetailNormalMap", type = ShaderProperty.Type.Texture},
            new ShaderProperty() {name = "_DetailNormalMap_2", type = ShaderProperty.Type.Texture},
            new ShaderProperty() {name = "_DetailNormalMap_3", type = ShaderProperty.Type.Texture},
            new ShaderProperty() {name = "_DetailNormalMap_4", type = ShaderProperty.Type.Texture},
            new ShaderProperty() {name = "_DetailMask", type = ShaderProperty.Type.Texture},
            new ShaderProperty() {name = "_Colormask", type = ShaderProperty.Type.Texture},
            new ShaderProperty() {name = "_EffectMap", type = ShaderProperty.Type.Texture},
            new ShaderProperty() {name = "_OverTex", type = ShaderProperty.Type.Texture},
            new ShaderProperty() {name = "_MetallicGlossMap", type = ShaderProperty.Type.Texture},
            new ShaderProperty() {name = "_ParallaxMap", type = ShaderProperty.Type.Texture},
            new ShaderProperty() {name = "_EmissionMap", type = ShaderProperty.Type.Texture},
            new ShaderProperty() {name = "_DetailAlbedoMap", type = ShaderProperty.Type.Texture},
            new ShaderProperty() {name = "_ReflectionTex", type = ShaderProperty.Type.Texture},
            new ShaderProperty() {name = "_ShoreTex", type = ShaderProperty.Type.Texture},
            //Float
            new ShaderProperty() {name = "_Metallic", floatRange = new Vector2(0, 1), hasFloatRange = true, type = ShaderProperty.Type.Float},
            new ShaderProperty() {name = "_Smoothness", floatRange = new Vector2(0, 1), hasFloatRange = true, type = ShaderProperty.Type.Float},
            new ShaderProperty() {name = "_OcclusionStrength", floatRange = new Vector2(0, 1), hasFloatRange = true, type = ShaderProperty.Type.Float},
            new ShaderProperty() {name = "_BlendNormalMapScale", floatRange = new Vector2(0, 1), hasFloatRange = true, type = ShaderProperty.Type.Float},
            new ShaderProperty() {name = "_DetailNormalMapScale", floatRange = new Vector2(0, 1), hasFloatRange = true, type = ShaderProperty.Type.Float},
            new ShaderProperty() {name = "_DetailNormalMapScale_2", floatRange = new Vector2(0, 1), hasFloatRange = true, type = ShaderProperty.Type.Float},
            new ShaderProperty() {name = "_DetailNormalMapScale_3", floatRange = new Vector2(0, 1), hasFloatRange = true, type = ShaderProperty.Type.Float},
            new ShaderProperty() {name = "_DetailNormalMapScale_4", floatRange = new Vector2(0, 1), hasFloatRange = true, type = ShaderProperty.Type.Float},
            new ShaderProperty() {name = "_Cutoff", floatRange = new Vector2(0, 1), hasFloatRange = true, type = ShaderProperty.Type.Float},
            new ShaderProperty() {name = "_Occlusion", floatRange = new Vector2(0, 1), hasFloatRange = true, type = ShaderProperty.Type.Float},
            new ShaderProperty() {name = "_RimPower", floatRange = new Vector2(0, 1), hasFloatRange = true, type = ShaderProperty.Type.Float},
            new ShaderProperty() {name = "_Refraction", floatRange = new Vector2(0, 9), hasFloatRange = true, type = ShaderProperty.Type.Float},
            new ShaderProperty() {name = "_EffectContrast", floatRange = new Vector2(0, 9), hasFloatRange = true, type = ShaderProperty.Type.Float},
            new ShaderProperty() {name = "_Effect2Power", floatRange = new Vector2(0, 9), hasFloatRange = true, type = ShaderProperty.Type.Float},
            new ShaderProperty() {name = "_ColorReverse", floatRange = new Vector2(0, 1), hasFloatRange = true, type = ShaderProperty.Type.Float},
            new ShaderProperty() {name = "_SpecReverse", floatRange = new Vector2(0, 1), hasFloatRange = true, type = ShaderProperty.Type.Float},
            new ShaderProperty() {name = "_SmoothTuning", floatRange = new Vector2(0, 1), hasFloatRange = true, type = ShaderProperty.Type.Float},
            new ShaderProperty() {name = "_Glossiness", floatRange = new Vector2(0, 1), hasFloatRange = true, type = ShaderProperty.Type.Float},
            new ShaderProperty() {name = "_BumpScale", type = ShaderProperty.Type.Float},
            new ShaderProperty() {name = "_Parallax", floatRange = new Vector2(0.005f, 0.08f), hasFloatRange = true, type = ShaderProperty.Type.Float},
            new ShaderProperty() {name = "_Mode", type = ShaderProperty.Type.Float},
            new ShaderProperty() {name = "_SrcBlend", type = ShaderProperty.Type.Float},
            new ShaderProperty() {name = "_DstBlend", type = ShaderProperty.Type.Float},
            new ShaderProperty() {name = "_ZWrite", type = ShaderProperty.Type.Float},
            new ShaderProperty() {name = "_FresnelScale", floatRange = new Vector2(0.15f, 4), hasFloatRange = true, type = ShaderProperty.Type.Float},
            new ShaderProperty() {name = "_GerstnerIntensity", type = ShaderProperty.Type.Float},
            new ShaderProperty() {name = "_Shininess", floatRange = new Vector2(2, 500), hasFloatRange = true, type = ShaderProperty.Type.Float},
            //Bools
            new ShaderProperty() {name = "_HairEffect", type = ShaderProperty.Type.Boolean},
            new ShaderProperty() {name = "_GlossUseAlpha", type = ShaderProperty.Type.Boolean},
            new ShaderProperty() {name = "_TuningColor21or3", type = ShaderProperty.Type.Boolean},
            new ShaderProperty() {name = "_TuningSpec21or3", type = ShaderProperty.Type.Boolean},
            new ShaderProperty() {name = "_TuningSmooth21or3", type = ShaderProperty.Type.Boolean},
            new ShaderProperty() {name = "_NspecR", type = ShaderProperty.Type.Boolean},
            new ShaderProperty() {name = "_NColorR", type = ShaderProperty.Type.Boolean},
            new ShaderProperty() {name = "_DetailMask2", type = ShaderProperty.Type.Boolean},
            new ShaderProperty() {name = "_skin_effect", type = ShaderProperty.Type.Boolean},
            new ShaderProperty() {name = "_rimlight", type = ShaderProperty.Type.Boolean},
            //Enum
            new ShaderProperty() {name = "_UVSec", enumValues = new Dictionary<int, string>() {{0, "UV0"}, {1, "UV1"}}, type = ShaderProperty.Type.Enum},
            //Vector4
            new ShaderProperty() {name = "_UVScroll", type = ShaderProperty.Type.Vector4},
            new ShaderProperty() {name = "_DetailNormalConvert", type = ShaderProperty.Type.Vector4},
            new ShaderProperty() {name = "_DistortParams", type = ShaderProperty.Type.Vector4},
            new ShaderProperty() {name = "_InvFadeParemeter", type = ShaderProperty.Type.Vector4},
            new ShaderProperty() {name = "_AnimationTiling", type = ShaderProperty.Type.Vector4},
            new ShaderProperty() {name = "_AnimationDirection", type = ShaderProperty.Type.Vector4},
            new ShaderProperty() {name = "_BumpTiling", type = ShaderProperty.Type.Vector4},
            new ShaderProperty() {name = "_BumpDirection", type = ShaderProperty.Type.Vector4},
            new ShaderProperty() {name = "_WorldLightDir", type = ShaderProperty.Type.Vector4},
            new ShaderProperty() {name = "_Foam", type = ShaderProperty.Type.Vector4},
            new ShaderProperty() {name = "_GAmplitude", type = ShaderProperty.Type.Vector4},
            new ShaderProperty() {name = "_GFrequency", type = ShaderProperty.Type.Vector4},
            new ShaderProperty() {name = "_GSteepness", type = ShaderProperty.Type.Vector4},
            new ShaderProperty() {name = "_GSpeed", type = ShaderProperty.Type.Vector4},
            new ShaderProperty() {name = "_GDirectionAB", type = ShaderProperty.Type.Vector4},
            new ShaderProperty() {name = "_GDirectionCD", type = ShaderProperty.Type.Vector4}
        };
        private const float _width = 600;
        private const float _height = 600;
        private string _workingDirectory;
        private string _workingDirectoryParent;
        private Rect _windowRect = new Rect((Screen.width - _width) / 2f, (Screen.height - _height) / 2f, _width, _height);
        private int _randomId;
        private bool _enabled;
        private bool _mouseInWindow;
        private readonly HashSet<Renderer> _selectedRenderers = new HashSet<Renderer>();
        private Vector2 _rendererScroll;
        private ShadowCastingMode[] _shadowCastingModes;
        private Vector2 _materialsScroll;
        private TreeNodeObject _lastSelectedNode;
        private Dictionary<Renderer, RendererData> _dirtyRenderers = new Dictionary<Renderer, RendererData>();
        private string _rendererFilter = "";
        private readonly Dictionary<Shader, List<ShaderProperty>> _cachedProperties = new Dictionary<Shader, List<ShaderProperty>>();
        private Vector2 _propertiesScroll;
        private readonly Texture2D _simpleTexture = new Texture2D(1, 1, TextureFormat.ARGB32, false);
        private Action<string, Texture> _selectTextureCallback;
        private bool _texturesLoaded = false;
        private readonly Dictionary<string, Texture2D> _textures = new Dictionary<string, Texture2D>();
        private Vector2 _textureScroll;
        private string _textureFilter = "";
        private Dictionary<Material, MaterialInfo> _selectedMaterials = new Dictionary<Material, MaterialInfo>();
        private readonly List<VectorLine> _boundsDebugLines = new List<VectorLine>();
        private Vector2 _keywordsScroll;
        private string _keywordInput = "";
        private bool _loadingTextures = false;
        private string _pwd;
        private string[] _localFiles;
        private string[] _localFolders;
        private string _currentDirectory;
        #endregion

        #region Unity Methods
        void Start()
        {
            HSExtSave.HSExtSave.RegisterHandler("rendererEditor", null, null, this.OnSceneLoad, null, this.OnSceneSave, null, null);
            this._randomId = (int)(UnityEngine.Random.value * UInt32.MaxValue);
            this._shadowCastingModes = (ShadowCastingMode[])Enum.GetValues(typeof(ShadowCastingMode));

            float size = 0.012f;
            Vector3 topLeftForward = (Vector3.up + Vector3.left + Vector3.forward) * size,
                topRightForward = (Vector3.up + Vector3.right + Vector3.forward) * size,
                bottomLeftForward = ((Vector3.down + Vector3.left + Vector3.forward) * size),
                bottomRightForward = ((Vector3.down + Vector3.right + Vector3.forward) * size),
                topLeftBack = (Vector3.up + Vector3.left + Vector3.back) * size,
                topRightBack = (Vector3.up + Vector3.right + Vector3.back) * size,
                bottomLeftBack = (Vector3.down + Vector3.left + Vector3.back) * size,
                bottomRightBack = (Vector3.down + Vector3.right + Vector3.back) * size;
            this._boundsDebugLines.Add(VectorLine.SetLine(Color.green, topLeftForward, topRightForward));
            this._boundsDebugLines.Add(VectorLine.SetLine(Color.green, topRightForward, bottomRightForward));
            this._boundsDebugLines.Add(VectorLine.SetLine(Color.green, bottomRightForward, bottomLeftForward));
            this._boundsDebugLines.Add(VectorLine.SetLine(Color.green, bottomLeftForward, topLeftForward));
            this._boundsDebugLines.Add(VectorLine.SetLine(Color.green, topLeftBack, topRightBack));
            this._boundsDebugLines.Add(VectorLine.SetLine(Color.green, topRightBack, bottomRightBack));
            this._boundsDebugLines.Add(VectorLine.SetLine(Color.green, bottomRightBack, bottomLeftBack));
            this._boundsDebugLines.Add(VectorLine.SetLine(Color.green, bottomLeftBack, topLeftBack));
            this._boundsDebugLines.Add(VectorLine.SetLine(Color.green, topLeftBack, topLeftForward));
            this._boundsDebugLines.Add(VectorLine.SetLine(Color.green, topRightBack, topRightForward));
            this._boundsDebugLines.Add(VectorLine.SetLine(Color.green, bottomRightBack, bottomRightForward));
            this._boundsDebugLines.Add(VectorLine.SetLine(Color.green, bottomLeftBack, bottomLeftForward));

            foreach (VectorLine line in this._boundsDebugLines)
            {
                line.lineWidth = 2f;
                line.active = false;
            }
            this.StartCoroutine(this.EndOfFrame());

            this._currentDirectory = Directory.GetCurrentDirectory();
            this._workingDirectory = Path.Combine(this._currentDirectory, _texturesDir);
            this._workingDirectoryParent = Directory.GetParent(this._workingDirectory).FullName;
            this._pwd = this._workingDirectory;
            this.ExploreCurrentFolder();
        }

        void Update()
        {
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.R))
            {
                this._enabled = !this._enabled;
                Studio.Studio.Instance.colorPaletteCtrl.visible = false;
                this._selectTextureCallback = null;
                this.CheckGizmosEnabled();
            }
            if (Studio.Studio.Instance.treeNodeCtrl.selectNode != this._lastSelectedNode)
            {
                this.ClearSelectedRenderers();
                Studio.Studio.Instance.colorPaletteCtrl.visible = false;
                this._selectTextureCallback = null;
                this.CheckGizmosEnabled();
            }

            this._lastSelectedNode = Studio.Studio.Instance.treeNodeCtrl.selectNode;

            Dictionary<Renderer, RendererData> newDic = null;
            foreach (KeyValuePair<Renderer, RendererData> pair in this._dirtyRenderers)
            {
                if (pair.Key == null)
                {
                    newDic = new Dictionary<Renderer, RendererData>();
                    break;
                }
            }
            if (newDic != null)
            {
                foreach (KeyValuePair<Renderer, RendererData> pair in this._dirtyRenderers)
                {
                    if (pair.Key != null)
                        newDic.Add(pair.Key, pair.Value);
                }
                this._dirtyRenderers = newDic;
                this.CheckGizmosEnabled();
            }
            Dictionary<Material, MaterialInfo> newMaterialDic = null;
            foreach (KeyValuePair<Material, MaterialInfo> material in this._selectedMaterials)
            {
                if (material.Key == null)
                {
                    newMaterialDic = new Dictionary<Material, MaterialInfo>();
                    break;
                }
            }
            if (newMaterialDic != null)
            {
                foreach (KeyValuePair<Material, MaterialInfo> material in this._selectedMaterials)
                {
                    if (material.Key != null)
                        newMaterialDic.Add(material.Key, material.Value);
                    else if (material.Value.renderer != null)
                        newMaterialDic.Add(material.Value.renderer.materials[material.Value.index], material.Value);
                }
                this._selectedMaterials = newMaterialDic;
            }
        }

        private IEnumerator EndOfFrame()
        {
            while (true)
            {
                yield return new WaitForEndOfFrame();
                this.DrawGizmos();
            }
        }

        void OnGUI()
        {
            if (!this._enabled || Studio.Studio.Instance.treeNodeCtrl.selectNode == null)
                return;
            GUI.Box(this._windowRect, "", GUI.skin.window);
            GUI.Box(this._windowRect, "", GUI.skin.window);
            this._windowRect = GUILayout.Window(this._randomId, this._windowRect, this.WindowFunction, "Renderer Editor");            
            this._mouseInWindow = this._windowRect.Contains(Event.current.mousePosition);
            if (this._selectTextureCallback != null)
            {
                Rect selectTextureRect = new Rect(this._windowRect.max.x + 4, this._windowRect.max.y - 440, 230, 440);
                GUI.Box(selectTextureRect, "", GUI.skin.window);
                GUI.Box(selectTextureRect, "", GUI.skin.window);
                selectTextureRect = GUILayout.Window(this._randomId + 1, selectTextureRect, this.SelectTextureWindow, "Select Texture");
                this._mouseInWindow = this._mouseInWindow || selectTextureRect.Contains(Event.current.mousePosition);
            }
            if (this._mouseInWindow)
                Studio.Studio.Instance.cameraCtrl.noCtrlCondition = () => this._mouseInWindow && this._enabled && Studio.Studio.Instance.treeNodeCtrl.selectNode != null;
        }
        #endregion

        #region Private Methods
        private void WindowFunction(int id)
        {
            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical();

            GUI.enabled = this._selectedRenderers.Count != 0;

            {
                bool newEnabled = GUILayout.Toggle(this._selectedRenderers.Count != 0 && this._selectedRenderers.First().enabled, "Enabled");
                if (this._selectedRenderers.Count != 0 && newEnabled != this._selectedRenderers.First().enabled)
                {
                    foreach (Renderer renderer in this._selectedRenderers)
                    {
                        RendererData data;
                        this.SetRendererDirty(renderer, out data);
                        renderer.enabled = newEnabled;
                    }
                }
            }

            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Cast Shadows");
                foreach (ShadowCastingMode mode in this._shadowCastingModes)
                {
                    if (GUILayout.Toggle(this._selectedRenderers.Count != 0 && this._selectedRenderers.First().shadowCastingMode == mode, mode.ToString()))
                    {
                        if (mode != this._selectedRenderers.First().shadowCastingMode)
                        {
                            foreach (Renderer renderer in this._selectedRenderers)
                            {
                                RendererData data;
                                this.SetRendererDirty(renderer, out data);
                                renderer.shadowCastingMode = mode;
                            }
                        }
                    }
                }
                GUILayout.EndHorizontal();
            }

            {
                bool newReceiveShadows = GUILayout.Toggle(this._selectedRenderers.Count != 0 && this._selectedRenderers.First().receiveShadows, "Receive Shadows");
                if (this._selectedRenderers.Count != 0 && newReceiveShadows != this._selectedRenderers.First().receiveShadows)
                {
                    foreach (Renderer renderer in this._selectedRenderers)
                    {
                        RendererData data;
                        this.SetRendererDirty(renderer, out data);
                        renderer.receiveShadows = newReceiveShadows;
                    }
                }
            }

            {
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Reset renderer") && this._selectedRenderers.Count != 0)
                {
                    foreach (Renderer renderer in this._selectedRenderers)
                    {
                        this.ResetRenderer(renderer);
                    }
                }
                if (GUILayout.Button("Reset w/ materials") && this._selectedRenderers.Count != 0)
                {
                    foreach (Renderer renderer in this._selectedRenderers)
                    {
                        this.ResetRenderer(renderer, true);
                    }
                }
                GUILayout.EndHorizontal();
            }

            GUILayout.BeginVertical("Materials", GUI.skin.window);
            this._materialsScroll = GUILayout.BeginScrollView(this._materialsScroll, GUILayout.Height(120));
            if (this._selectedRenderers.Count != 0)
            {
                foreach (Renderer selectedRenderer in this._selectedRenderers)
                {
                    for (int i = 0; i < selectedRenderer.sharedMaterials.Length; i++)
                    {
                        Material material = selectedRenderer.sharedMaterials[i];
                        if (material == null)
                            continue;
                        Color c = GUI.color;
                        bool isMaterialDirty = this._dirtyRenderers.TryGetValue(selectedRenderer, out RendererData rendererData) && rendererData.dirtyMaterials.ContainsKey(material);
                        if (this._selectedMaterials.ContainsKey(material))
                            GUI.color = Color.cyan;
                        else if (isMaterialDirty)
                            GUI.color = Color.magenta;
                        if (GUILayout.Button(material.name + (isMaterialDirty ? "*" : "") + (this._selectedRenderers.Count > 1 ? "(" + selectedRenderer.name + ")" : "")))
                        {
                            material = selectedRenderer.materials[i];
                            if (Input.GetKey(KeyCode.LeftControl) == false)
                            {
                                this.ClearSelectedMaterials();
                                this.SelectMaterial(material, selectedRenderer, i);
                            }
                            else
                            {
                                if (this._selectedMaterials.ContainsKey(material))
                                    this.UnselectMaterial(material);
                                else
                                    this.SelectMaterial(material, selectedRenderer, i);
                            }
                            Studio.Studio.Instance.colorPaletteCtrl.visible = false;
                            this._selectTextureCallback = null;
                        }
                        GUI.color = c;
                    }
                }
            }
            GUILayout.EndScrollView();

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Select all"))
            {
                foreach (Renderer selectedRenderer in this._selectedRenderers)
                {
                    for (int i = 0; i < selectedRenderer.sharedMaterials.Length; i++)
                    {
                        Material material = selectedRenderer.materials[i];
                        if (material == null)
                            continue;
                        if (!this._selectedMaterials.ContainsKey(material))
                            this.SelectMaterial(material, selectedRenderer, i);
                    }
                }
                Studio.Studio.Instance.colorPaletteCtrl.visible = false;
                this._selectTextureCallback = null;
            }
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();

            GUI.enabled = true;

            if (this._selectedMaterials.Count != 0)
            {
                Material selectedMaterial = this._selectedMaterials.First().Key;
                this._propertiesScroll = GUILayout.BeginScrollView(this._propertiesScroll);

                GUILayout.Label(selectedMaterial.shader.name);

                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Render Queue", GUILayout.ExpandWidth(false));
                    int newRenderQueue = (int)GUILayout.HorizontalSlider(selectedMaterial.renderQueue, -1, 5000);
                    if (newRenderQueue != selectedMaterial.renderQueue)
                    {
                        foreach (KeyValuePair<Material, MaterialInfo> material in this._selectedMaterials)
                        {
                            RendererData.MaterialData data;
                            this.SetMaterialDirty(material.Key, out data);
                            if (data.hasRenderQueue == false)
                            {
                                data.originalRenderQueue = material.Key.renderQueue;
                                data.hasRenderQueue = true;
                            }
                            material.Key.renderQueue = newRenderQueue;
                        }
                    }
                    GUILayout.Label(selectedMaterial.renderQueue.ToString("0000"), GUILayout.ExpandWidth(false));
                    if (GUILayout.Button("-1000", GUILayout.ExpandWidth(false)))
                    {
                        foreach (KeyValuePair<Material, MaterialInfo> material in this._selectedMaterials)
                        {
                            RendererData.MaterialData data;
                            this.SetMaterialDirty(material.Key, out data);
                            if (data.hasRenderQueue == false)
                            {
                                data.originalRenderQueue = material.Key.renderQueue;
                                data.hasRenderQueue = true;
                            }
                            int value = material.Key.renderQueue - 1000;
                            if (value < -1)
                                value = -1;
                            material.Key.renderQueue = value;
                        }
                    }
                    if (GUILayout.Button("Reset", GUILayout.ExpandWidth(false)))
                    {
                        foreach (KeyValuePair<Material, MaterialInfo> material in this._selectedMaterials)
                        {
                            if (this._dirtyRenderers.TryGetValue(material.Value.renderer, out RendererData rendererData) &&
                                rendererData.dirtyMaterials.TryGetValue(material.Key, out RendererData.MaterialData data) &&
                                data.hasRenderQueue)
                            {
                                material.Key.renderQueue = data.originalRenderQueue;
                                data.hasRenderQueue = false;
                                this.TryResetMaterial(material.Key, data, rendererData);
                            }
                        }
                    }
                    GUILayout.EndHorizontal();
                }

                {
                    if (this._cachedProperties.TryGetValue(selectedMaterial.shader, out List<ShaderProperty> cachedProperties) == false)
                    {
                        cachedProperties = new List<ShaderProperty>();
                        foreach (ShaderProperty property in this._shaderProperties)
                            if (selectedMaterial.HasProperty(property.name))
                                cachedProperties.Add(property);
                        this._cachedProperties.Add(selectedMaterial.shader, cachedProperties);
                    }

                    foreach (ShaderProperty property in cachedProperties)
                        this.ShaderPropertyDrawer(property);
                }

                this.KeywordsDrawer();

                GUILayout.EndScrollView();

                {
                    GUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Reset"))
                        foreach (KeyValuePair<Material, MaterialInfo> material in this._selectedMaterials)
                        {
                            if (this._dirtyRenderers.TryGetValue(material.Value.renderer, out RendererData rendererData) &&
                                rendererData.dirtyMaterials.TryGetValue(material.Key, out RendererData.MaterialData data))
                                this.ResetMaterial(material.Key, data, rendererData);
                        }
                    GUILayout.EndHorizontal();
                }
            }

            GUILayout.EndVertical();

            GUILayout.BeginVertical("Renderers", GUI.skin.window, GUILayout.Width(180f));
            GUILayout.BeginHorizontal();
            this._rendererFilter = GUILayout.TextField(this._rendererFilter);
            if (GUILayout.Button("X", GUILayout.ExpandWidth(false)))
                this._rendererFilter = "";
            GUILayout.EndHorizontal();
            this._rendererScroll = GUILayout.BeginScrollView(this._rendererScroll);
            Renderer[] renderers = Studio.Studio.Instance.dicInfo[Studio.Studio.Instance.treeNodeCtrl.selectNode].guideObject.transformTarget.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                if (renderer.name.IndexOf(this._rendererFilter, StringComparison.OrdinalIgnoreCase) == -1 && renderer.sharedMaterials.All(m => m != null && m.name.IndexOf(this._rendererFilter, StringComparison.OrdinalIgnoreCase) == -1))
                    continue;
                Color c = GUI.color;
                bool isMaterialDirty = this._dirtyRenderers.ContainsKey(renderer);
                if (this._selectedRenderers.Contains(renderer))
                    GUI.color = Color.cyan;
                else if (isMaterialDirty)
                    GUI.color = Color.magenta;

                if (GUILayout.Button(renderer.name + (isMaterialDirty ? "*" : "")))
                {
                    if (Input.GetKey(KeyCode.LeftControl))
                    {
                        if (this._selectedRenderers.Contains(renderer))
                            this.UnselectRenderer(renderer);
                        else
                            this.SelectRenderer(renderer);
                    }
                    else if (Input.GetKey(KeyCode.LeftShift) && this._selectedRenderers.Count > 0)
                    {
                        int firstIndex = renderers.IndexOf(this._selectedRenderers.First());
                        int lastIndex = renderers.IndexOf(renderer);
                        if (firstIndex != lastIndex)
                        {
                            int inc;
                            if (firstIndex < lastIndex)
                                inc = 1;
                            else
                                inc = -1;
                            for (int i = firstIndex; i != lastIndex; i += inc)
                            {
                                Renderer r = renderers[i];
                                if (r.name.IndexOf(this._rendererFilter, StringComparison.OrdinalIgnoreCase) == -1 && r.sharedMaterials.All(m => m != null && m.name.IndexOf(this._rendererFilter, StringComparison.OrdinalIgnoreCase) == -1))
                                    continue;
                                if (this._selectedRenderers.Contains(r) == false)
                                    this.SelectRenderer(r);
                            }
                            if (this._selectedRenderers.Contains(renderer) == false)
                                this.SelectRenderer(renderer);
                        }
                    }
                    else
                    {
                        this.ClearSelectedRenderers();
                        this.SelectRenderer(renderer);
                    }
                    Studio.Studio.Instance.colorPaletteCtrl.visible = false;
                    this._selectTextureCallback = null;
                    this.CheckGizmosEnabled();
                }
                GUI.color = c;
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();

            GUI.DragWindow();
        }

        private void SelectTextureWindow(int id)
        {
            GUILayout.BeginHorizontal();
            this._textureFilter = GUILayout.TextField(this._textureFilter);
            if (GUILayout.Button("X", GUILayout.ExpandWidth(false)))
                this._textureFilter = "";
            GUILayout.EndHorizontal();

            GUILayout.Label(this._pwd.Substring(this._workingDirectoryParent.Length));
            if (this._loadingTextures)
            {
                GUILayout.FlexibleSpace();
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.Label("Loading" + new String('.', (int)(Time.time * 2 % 4)));
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                GUILayout.FlexibleSpace();
            }
            else
            {
                if (GUILayout.Button("No Texture"))
                {
                    this._selectTextureCallback("", null);
                    this._selectTextureCallback = null;
                }
                this._textureScroll = GUILayout.BeginScrollView(this._textureScroll);
                Color c = GUI.color;
                GUI.color = Color.green;
                if (this._pwd.Equals(this._workingDirectory, StringComparison.OrdinalIgnoreCase) == false && GUILayout.Button(".. (Parent folder)"))
                {
                    this._pwd = Directory.GetParent(this._pwd).FullName;
                    this.ExploreCurrentFolder();
                }
                foreach (string folder in this._localFolders)
                {
                    string directoryName = folder.Substring(this._pwd.Length);
                    if (directoryName.IndexOf(this._textureFilter, StringComparison.OrdinalIgnoreCase) != -1 && GUILayout.Button(directoryName))
                    {
                        this._pwd = folder;
                        this.ExploreCurrentFolder();
                    }
                }
                GUI.color = c;
                foreach (string file in this._localFiles)
                {
                    string localFileName = Path.GetFileName(file);
                    if (localFileName.IndexOf(this._textureFilter, StringComparison.OrdinalIgnoreCase) == -1)
                        continue;
                    Texture2D texture = this.GetTexture(file);
                    if (texture != null)
                    {
                        GUILayout.BeginVertical(GUI.skin.box);
                        GUILayout.Label(localFileName);
                        GUILayout.BeginHorizontal();
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button(GUIContent.none, GUILayout.Width(178), GUILayout.Height(178)))
                        {
                            this._selectTextureCallback(file, texture);
                            this._selectTextureCallback = null;
                        }
                        Rect layoutRectangle = GUILayoutUtility.GetLastRect();
                        GUILayout.FlexibleSpace();
                        GUILayout.EndHorizontal();

                        layoutRectangle.xMin += 4;
                        layoutRectangle.xMax -= 4;
                        layoutRectangle.yMin += 4;
                        layoutRectangle.yMax -= 4;
                        GUI.DrawTexture(layoutRectangle, texture, ScaleMode.StretchToFill, true);
                        GUILayout.EndVertical();
                    }
                }
                GUILayout.EndScrollView();
            }
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Refresh") && this._loadingTextures == false)
                this.RefreshTextures();
            if (GUILayout.Button("Cancel"))
                this._selectTextureCallback = null;
            GUILayout.EndHorizontal();
            if (GUILayout.Button("Open folder"))
                System.Diagnostics.Process.Start(this._pwd);
        }

        private void RefreshTextures()
        {
            this.StartCoroutine(this.RefreshTextures_Routine());
        }

        private IEnumerator RefreshTextures_Routine()
        {
            this._loadingTextures = true;
            this.ExploreCurrentFolder();
            foreach (string file in this._localFiles)
            {
                Texture2D texture;
                if (this._textures.TryGetValue(file, out texture))
                {
                    Destroy(texture);
                    this._textures.Remove(file);
                }
            }
            foreach (string file in this._localFiles)
            {
                yield return null;
                this.LoadSingleTexture(file);
            }
            this._loadingTextures = false;
        }

        private Texture2D LoadSingleTexture(string path)
        {
            Texture2D texture = new Texture2D(64, 64, TextureFormat.ARGB32, true);
            if (!texture.LoadImage(File.ReadAllBytes(path)))
                return null;
            texture.SetPixel(0, 0, texture.GetPixel(0, 0));
            texture.Apply(true);
            if (this._textures.ContainsKey(path) == false)
                this._textures.Add(path, texture);
            else
            {
                UnityEngine.Object.Destroy(this._textures[path]);
                this._textures[path] = texture;
            }
            foreach (KeyValuePair<Renderer, RendererData> pair in this._dirtyRenderers)
            {
                foreach (KeyValuePair<Material, RendererData.MaterialData> pair2 in pair.Value.dirtyMaterials)
                {
                    foreach (KeyValuePair<string, RendererData.MaterialData.TextureData> pair3 in pair2.Value.dirtyTextureProperties)
                    {
                        if (pair3.Value.currentTexturePath.Equals(path, StringComparison.OrdinalIgnoreCase))
                        {
                            pair2.Key.SetTexture(pair3.Key, texture);
                        }
                    }
                }
            }
            return texture;
        }

        private void ExploreCurrentFolder()
        {
            if (Directory.Exists(this._pwd) == false)
                this._pwd = _texturesDir;
            if (Directory.Exists(this._pwd) == false)
                Directory.CreateDirectory(_texturesDir);

            this._localFiles = Directory.GetFiles(this._pwd, "*.*").Where(s => s.EndsWith(".png", StringComparison.OrdinalIgnoreCase) || s.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)).ToArray();
            this._localFolders = Directory.GetDirectories(this._pwd);
        }

        private Texture2D GetTexture(string path)
        {
            Texture2D t;
            if (this._textures.TryGetValue(path, out t))
                return t;
            return this.LoadSingleTexture(path);
        }

        private void SelectMaterial(Material material, Renderer renderer, int index)
        {
            this._selectedMaterials.Add(material, new MaterialInfo{index = index, renderer = renderer});
        }

        private void UnselectMaterial(Material material)
        {
            this._selectedMaterials.Remove(material);
        }

        private void ClearSelectedMaterials()
        {
            this._selectedMaterials.Clear();
        }

        private void SelectRenderer(Renderer renderer)
        {
            this._selectedRenderers.Add(renderer);
        }

        private void UnselectRenderer(Renderer renderer)
        {
            this._selectedRenderers.Remove(renderer);
            foreach (KeyValuePair<Material, MaterialInfo> pair in new Dictionary<Material, MaterialInfo>(this._selectedMaterials))
            {
                if (pair.Value.renderer == renderer)
                    this.UnselectMaterial(pair.Key);
            }
        }

        private void ClearSelectedRenderers()
        {
            this._selectedRenderers.Clear();
            this.ClearSelectedMaterials();
        }

        private bool SetMaterialDirty(Material mat, out RendererData.MaterialData data, RendererData rendererData = null)
        {
            if (rendererData == null)
                this.SetRendererDirty(this._selectedMaterials[mat].renderer, out rendererData);
            if (rendererData.dirtyMaterials.TryGetValue(mat, out data) == false)
            {
                data = new RendererData.MaterialData();
                rendererData.dirtyMaterials.Add(mat, data);
                return true;
            }
            return false;
        }

        private void TryResetMaterial(Material mat, RendererData.MaterialData materialData, RendererData rendererData)
        {
            if (mat == null)
                return;
            if (materialData.hasRenderQueue == false && materialData.dirtyColorProperties.Count == 0 && materialData.dirtyBooleanProperties.Count == 0 && materialData.dirtyEnumProperties.Count == 0 && materialData.dirtyFloatProperties.Count == 0 && materialData.dirtyVector4Properties.Count == 0 && materialData.dirtyTextureOffsetProperties.Count == 0 && materialData.dirtyTextureScaleProperties.Count == 0 && materialData.dirtyTextureProperties.Count == 0 && materialData.enabledKeywords.Count == 0 && materialData.disabledKeywords.Count == 0)
            {
                this.ResetMaterial(mat, materialData, rendererData);
            }
        }

        private void ResetMaterial(Material mat, RendererData.MaterialData materialData, RendererData rendererData)
        {
            if (mat == null)
                return;
            if (materialData.hasRenderQueue)
            {
                mat.renderQueue = materialData.originalRenderQueue;
                materialData.hasRenderQueue = false;
            }
            foreach (KeyValuePair<string, Color> pair in materialData.dirtyColorProperties)
                mat.SetColor(pair.Key, pair.Value);
            foreach (KeyValuePair<string, bool> pair in materialData.dirtyBooleanProperties)
                mat.SetFloat(pair.Key, pair.Value ? 1f : 0f);
            foreach (KeyValuePair<string, int> pair in materialData.dirtyEnumProperties)
                mat.SetFloat(pair.Key, pair.Value);
            foreach (KeyValuePair<string, float> pair in materialData.dirtyFloatProperties)
                mat.SetFloat(pair.Key, pair.Value);
            foreach (KeyValuePair<string, Vector4> pair in materialData.dirtyVector4Properties)
                mat.SetVector(pair.Key, pair.Value);
            foreach (KeyValuePair<string, Vector2> pair in materialData.dirtyTextureOffsetProperties)
                mat.SetTextureOffset(pair.Key, pair.Value);
            foreach (KeyValuePair<string, Vector2> pair in materialData.dirtyTextureScaleProperties)
                mat.SetTextureScale(pair.Key, pair.Value);
            foreach (KeyValuePair<string, RendererData.MaterialData.TextureData> pair in materialData.dirtyTextureProperties)
                mat.SetTexture(pair.Key, pair.Value.originalTexture);

            rendererData.dirtyMaterials.Remove(mat);
        }

        private bool SetRendererDirty(Renderer renderer, out RendererData data)
        {
            if (this._dirtyRenderers.TryGetValue(renderer, out data) == false)
            {
                data = new RendererData
                {
                    enabled = renderer.enabled,
                    shadowCastingMode = renderer.shadowCastingMode,
                    receiveShadow = renderer.receiveShadows
                };
                this._dirtyRenderers.Add(renderer, data);
                return true;
            }
            return false;
        }

        private void ResetRenderer(Renderer renderer, bool withMaterials = false)
        {
            RendererData data;
            if (this._dirtyRenderers.TryGetValue(renderer, out data))
            {
                renderer.enabled = data.enabled;
                renderer.shadowCastingMode = data.shadowCastingMode;
                renderer.receiveShadows = data.receiveShadow;
                if (withMaterials)
                    foreach (KeyValuePair<Material, RendererData.MaterialData> pair in new Dictionary<Material, RendererData.MaterialData>(data.dirtyMaterials))
                        this.ResetMaterial(pair.Key, pair.Value, data);
                if (data.dirtyMaterials.Count == 0)
                    this._dirtyRenderers.Remove(renderer);
            }
        }

        private void DrawGizmos()
        {
            if (!this._enabled || Studio.Studio.Instance.treeNodeCtrl.selectNode == null || this._selectedRenderers.Count == 0)
                return;
            Bounds bounds = this._selectedRenderers.First().bounds;
            Vector3 topLeftForward = new Vector3(bounds.min.x, bounds.max.y, bounds.max.z),
                topRightForward = bounds.max,
                bottomLeftForward = new Vector3(bounds.min.x, bounds.min.y, bounds.max.z),
                bottomRightForward = new Vector3(bounds.max.x, bounds.min.y, bounds.max.z),
                topLeftBack = new Vector3(bounds.min.x, bounds.max.y, bounds.min.z) ,
                topRightBack = new Vector3(bounds.max.x, bounds.max.y, bounds.min.z) ,
                bottomLeftBack = bounds.min,
                bottomRightBack = new Vector3(bounds.max.x, bounds.min.y, bounds.min.z);
            int i = 0;
            this._boundsDebugLines[i++].SetPoints(topLeftForward, topRightForward);
            this._boundsDebugLines[i++].SetPoints(topRightForward, bottomRightForward);
            this._boundsDebugLines[i++].SetPoints(bottomRightForward, bottomLeftForward);
            this._boundsDebugLines[i++].SetPoints(bottomLeftForward, topLeftForward);
            this._boundsDebugLines[i++].SetPoints(topLeftBack, topRightBack);
            this._boundsDebugLines[i++].SetPoints(topRightBack, bottomRightBack);
            this._boundsDebugLines[i++].SetPoints(bottomRightBack, bottomLeftBack);
            this._boundsDebugLines[i++].SetPoints(bottomLeftBack, topLeftBack);
            this._boundsDebugLines[i++].SetPoints(topLeftBack, topLeftForward);
            this._boundsDebugLines[i++].SetPoints(topRightBack, topRightForward);
            this._boundsDebugLines[i++].SetPoints(bottomRightBack, bottomRightForward);
            this._boundsDebugLines[i++].SetPoints(bottomLeftBack, bottomLeftForward);

            foreach (VectorLine line in this._boundsDebugLines)
                line.Draw();

        }

        private void CheckGizmosEnabled()
        {
            bool a = this._enabled && Studio.Studio.Instance.treeNodeCtrl.selectNode != null && this._selectedRenderers.Count != 0;
            foreach (VectorLine line in this._boundsDebugLines)
                line.active = a;
        }
        #endregion

        #region Drawers
        private void ShaderPropertyDrawer(ShaderProperty property)
        {
            switch (property.type)
            {
                case ShaderProperty.Type.Color:
                    this.ColorDrawer(property);
                    break;
                case ShaderProperty.Type.Texture:
                    this.TextureDrawer(property);
                    break;
                case ShaderProperty.Type.Float:
                    this.FloatDrawer(property);
                    break;
                case ShaderProperty.Type.Boolean:
                    this.BooleanDrawer(property);
                    break;
                case ShaderProperty.Type.Enum:
                    this.EnumDrawer(property);
                    break;
                case ShaderProperty.Type.Vector4:
                    this.Vector4Drawer(property);
                    break;
            }
        }

        private void ColorDrawer(ShaderProperty property)
        {
            Color c = this._selectedMaterials.First().Key.GetColor(property.name);
            GUILayout.BeginHorizontal();
            GUILayout.Label(property.name, GUILayout.ExpandWidth(false));

            if (GUILayout.Button("Hit me senpai <3", GUILayout.ExpandWidth(true)))
            {
                    Studio.Studio.Instance.colorPaletteCtrl.visible = !Studio.Studio.Instance.colorPaletteCtrl.visible;
                if (Studio.Studio.Instance.colorPaletteCtrl.visible)
                {
                    Studio.Studio.Instance.colorMenu.updateColorFunc = col =>
                    {
                        foreach (KeyValuePair<Material, MaterialInfo> selectedMaterial in this._selectedMaterials)
                        {
                            this.SetMaterialDirty(selectedMaterial.Key, out RendererData.MaterialData materialData);
                            if (materialData.dirtyColorProperties.ContainsKey(property.name) == false)
                                materialData.dirtyColorProperties.Add(property.name, selectedMaterial.Key.GetColor(property.name));
                            selectedMaterial.Key.SetColor(property.name, col);
                        }
                    };
                    try
                    {
                        Studio.Studio.Instance.colorMenu.SetColor(c, UI_ColorInfo.ControlType.PresetsSample);
                    }
                    catch (Exception)
                    {
                        UnityEngine.Debug.LogError("RendererEditor: Color is HDR, couldn't assign it properly.");
                    }
                }
            }

            Rect layoutRectangle = GUILayoutUtility.GetLastRect();
            layoutRectangle.xMin += 3;
            layoutRectangle.xMax -= 3;
            layoutRectangle.yMin += 3;
            layoutRectangle.yMax -= 3;
            this._simpleTexture.SetPixel(0, 0, c);
            this._simpleTexture.Apply(false);
            GUI.DrawTexture(layoutRectangle, this._simpleTexture, ScaleMode.StretchToFill, true);

            if (GUILayout.Button("Reset", GUILayout.ExpandWidth(false)))
            {
                foreach (KeyValuePair<Material, MaterialInfo> selectedMaterial in this._selectedMaterials)
                {
                    if (this._dirtyRenderers.TryGetValue(selectedMaterial.Value.renderer, out RendererData rendererData) &&
                        rendererData.dirtyMaterials.TryGetValue(selectedMaterial.Key, out RendererData.MaterialData materialData) &&
                        materialData.dirtyColorProperties.ContainsKey(property.name))
                    {
                        selectedMaterial.Key.SetColor(property.name, materialData.dirtyColorProperties[property.name]);
                        materialData.dirtyColorProperties.Remove(property.name);
                        this.TryResetMaterial(selectedMaterial.Key, materialData, rendererData);
                    }
                }
            }
            GUILayout.EndHorizontal();

        }

        private void TextureDrawer(ShaderProperty property)
        {
            Texture texture = this._selectedMaterials.First().Key.GetTexture(property.name);
            Vector2 offset = this._selectedMaterials.First().Key.GetTextureOffset(property.name);
            Vector2 scale = this._selectedMaterials.First().Key.GetTextureScale(property.name);
            GUILayout.BeginHorizontal();
            GUILayout.Label(property.name, GUILayout.ExpandWidth(false));
            GUILayout.FlexibleSpace();

            if (GUILayout.Button(GUIContent.none, GUILayout.Width(90f), GUILayout.Height(90f)))
            {
                this._selectTextureCallback = (path, newTexture) =>
                {
                    foreach (KeyValuePair<Material, MaterialInfo> selectedMaterial in this._selectedMaterials)
                    {
                        this.SetMaterialDirty(selectedMaterial.Key, out RendererData.MaterialData materialData);
                        RendererData.MaterialData.TextureData textureData;
                        if (materialData.dirtyTextureProperties.TryGetValue(property.name, out textureData) == false)
                        {
                            textureData = new RendererData.MaterialData.TextureData();
                            textureData.originalTexture = selectedMaterial.Key.GetTexture(property.name);
                            materialData.dirtyTextureProperties.Add(property.name, textureData);
                        }
                        textureData.currentTexturePath = path;
                        selectedMaterial.Key.SetTexture(property.name, newTexture);
                    }
                };
            }
            Rect r = GUILayoutUtility.GetLastRect();
            r.xMin += 4;
            r.xMax -= 4;
            r.yMin += 4;
            r.yMax -= 4;
            if (texture != null)
                GUI.DrawTexture(r, texture, ScaleMode.StretchToFill, true);
            GUILayout.BeginVertical();
            if (GUILayout.Button("Reset", GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(false)))
            {
                foreach (KeyValuePair<Material, MaterialInfo> selectedMaterial in this._selectedMaterials)
                    if (this._dirtyRenderers.TryGetValue(selectedMaterial.Value.renderer, out RendererData rendererData) &&
                        rendererData.dirtyMaterials.TryGetValue(selectedMaterial.Key, out RendererData.MaterialData materialData) &&
                        materialData.dirtyTextureProperties.ContainsKey(property.name))
                    {
                        selectedMaterial.Key.SetTexture(property.name, materialData.dirtyTextureProperties[property.name].originalTexture);
                        materialData.dirtyTextureProperties.Remove(property.name);
                        this.TryResetMaterial(selectedMaterial.Key, materialData, rendererData);
                    }
            }
            if (this._selectedMaterials.Count == 1 && GUILayout.Button("Dump", GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(false)) && texture != null)
            {
                if (Directory.Exists(_dumpDir) == false)
                    Directory.CreateDirectory(_dumpDir);
                RenderTexture rt = RenderTexture.GetTemporary(texture.width, texture.height, 24, RenderTextureFormat.ARGB32);
                RenderTexture cachedActive = RenderTexture.active;
                RenderTexture.active = rt;
                Graphics.Blit(texture, rt);
                Texture2D tex = new Texture2D(rt.width, rt.height, TextureFormat.ARGB32, true);
                tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0, false);
                RenderTexture.active = cachedActive;

                byte[] bytes = tex.EncodeToPNG();
                KeyValuePair<Material, MaterialInfo> mat = this._selectedMaterials.First();
                string fileName = Path.Combine(_dumpDir, $"{mat.Value.renderer.name}_{mat.Key.name}_{mat.Value.index}_{texture.name}.png");
                File.WriteAllBytes(fileName, bytes);

                this.LoadSingleTexture(fileName);
            }
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            Vector2 newOffset = offset;
            GUILayout.FlexibleSpace();
            GUILayout.Label("Offset", GUILayout.ExpandWidth(false));
            GUILayout.Label("X", GUILayout.ExpandWidth(false));
            string stringValue = offset.x.ToString("0.000");
            string stringNewValue = GUILayout.TextField(stringValue);
            if (stringNewValue != stringValue)
            {
                float parsedValue;
                if (float.TryParse(stringNewValue, out parsedValue))
                    newOffset.x = parsedValue;
            }
            GUILayout.Label("Y", GUILayout.ExpandWidth(false));
            stringValue = offset.y.ToString("0.000");
            stringNewValue = GUILayout.TextField(stringValue);
            if (stringNewValue != stringValue)
            {
                float parsedValue;
                if (float.TryParse(stringNewValue, out parsedValue))
                    newOffset.y = parsedValue;
            }
            if (offset != newOffset)
            {
                foreach (KeyValuePair<Material, MaterialInfo> selectedMaterial in this._selectedMaterials)
                {
                    this.SetMaterialDirty(selectedMaterial.Key, out RendererData.MaterialData materialData);
                    if (materialData.dirtyTextureOffsetProperties.ContainsKey(property.name) == false)
                        materialData.dirtyTextureOffsetProperties.Add(property.name, selectedMaterial.Key.GetTextureOffset(property.name));
                    selectedMaterial.Key.SetTextureOffset(property.name, newOffset);
                }
            }
            if (GUILayout.Button("Reset", GUILayout.ExpandWidth(false)))
            {
                foreach (KeyValuePair<Material, MaterialInfo> selectedMaterial in this._selectedMaterials)
                    if (this._dirtyRenderers.TryGetValue(selectedMaterial.Value.renderer, out RendererData rendererData) &&
                        rendererData.dirtyMaterials.TryGetValue(selectedMaterial.Key, out RendererData.MaterialData materialData) &&
                        materialData.dirtyTextureOffsetProperties.ContainsKey(property.name))
                    {
                        selectedMaterial.Key.SetTextureOffset(property.name, materialData.dirtyTextureOffsetProperties[property.name]);
                        materialData.dirtyTextureOffsetProperties.Remove(property.name);
                        this.TryResetMaterial(selectedMaterial.Key, materialData, rendererData);
                    }
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            Vector2 newScale = scale;
            GUILayout.FlexibleSpace();
            GUILayout.Label("Scale", GUILayout.ExpandWidth(false));
            GUILayout.Label("X", GUILayout.ExpandWidth(false));
            stringValue = scale.x.ToString("0.000");
            stringNewValue = GUILayout.TextField(stringValue);
            if (stringNewValue != stringValue)
            {
                float parsedValue;
                if (float.TryParse(stringNewValue, out parsedValue))
                    newScale.x = parsedValue;
            }
            GUILayout.Label("Y", GUILayout.ExpandWidth(false));
            stringValue = scale.y.ToString("0.000");
            stringNewValue = GUILayout.TextField(stringValue);
            if (stringNewValue != stringValue)
            {
                float parsedValue;
                if (float.TryParse(stringNewValue, out parsedValue))
                    newScale.y = parsedValue;
            }
            if (scale != newScale)
            {
                foreach (KeyValuePair<Material, MaterialInfo> selectedMaterial in this._selectedMaterials)
                {
                    this.SetMaterialDirty(selectedMaterial.Key, out RendererData.MaterialData materialData);
                    if (materialData.dirtyTextureScaleProperties.ContainsKey(property.name) == false)
                        materialData.dirtyTextureScaleProperties.Add(property.name, selectedMaterial.Key.GetTextureScale(property.name));
                    selectedMaterial.Key.SetTextureScale(property.name, newScale);
                }
            }
            if (GUILayout.Button("Reset", GUILayout.ExpandWidth(false)))
            {
                foreach (KeyValuePair<Material, MaterialInfo> selectedMaterial in this._selectedMaterials)
                    if (this._dirtyRenderers.TryGetValue(selectedMaterial.Value.renderer, out RendererData rendererData) &&
                        rendererData.dirtyMaterials.TryGetValue(selectedMaterial.Key, out RendererData.MaterialData materialData) &&
                    materialData.dirtyTextureScaleProperties.ContainsKey(property.name))
                {
                    selectedMaterial.Key.SetTextureScale(property.name, materialData.dirtyTextureScaleProperties[property.name]);
                    materialData.dirtyTextureScaleProperties.Remove(property.name);
                    this.TryResetMaterial(selectedMaterial.Key, materialData, rendererData);
                }
            }
            GUILayout.EndHorizontal();
        }

        private void FloatDrawer(ShaderProperty property)
        {
            float value = this._selectedMaterials.First().Key.GetFloat(property.name);
            GUILayout.BeginHorizontal();
            GUILayout.Label(property.name, GUILayout.ExpandWidth(false));

            Vector2 range;
            if (property.hasFloatRange)
                range = property.floatRange;
            else
                range = new Vector2(0f, 4f);

            float newValue = GUILayout.HorizontalSlider(value, range.x, range.y);

            string valueString = value.ToString("0.000");
            string newValueString = GUILayout.TextField(valueString, 5, GUILayout.Width(50f));

            if (newValueString != valueString)
            {
                float parseResult;
                if (float.TryParse(newValueString, out parseResult))
                    newValue = parseResult;
            }
            if (Mathf.Approximately(value, newValue) == false)
            {
                foreach (KeyValuePair<Material, MaterialInfo> selectedMaterial in this._selectedMaterials)
                {
                    this.SetMaterialDirty(selectedMaterial.Key, out RendererData.MaterialData materialData);
                    if (materialData.dirtyFloatProperties.ContainsKey(property.name) == false)
                        materialData.dirtyFloatProperties.Add(property.name, selectedMaterial.Key.GetFloat(property.name));
                    selectedMaterial.Key.SetFloat(property.name, newValue);
                }
            }

            if (GUILayout.Button("Reset", GUILayout.ExpandWidth(false)))
            {
                foreach (KeyValuePair<Material, MaterialInfo> selectedMaterial in this._selectedMaterials)
                    if (this._dirtyRenderers.TryGetValue(selectedMaterial.Value.renderer, out RendererData rendererData) &&
                        rendererData.dirtyMaterials.TryGetValue(selectedMaterial.Key, out RendererData.MaterialData materialData) &&
                    materialData.dirtyFloatProperties.ContainsKey(property.name))
                {
                    selectedMaterial.Key.SetFloat(property.name, materialData.dirtyFloatProperties[property.name]);
                    materialData.dirtyFloatProperties.Remove(property.name);
                    this.TryResetMaterial(selectedMaterial.Key, materialData, rendererData);
                }
            }

            GUILayout.EndHorizontal();
        }

        private void BooleanDrawer(ShaderProperty property)
        {
            bool value = Mathf.Approximately(this._selectedMaterials.First().Key.GetFloat(property.name), 1f);
            
            GUILayout.BeginHorizontal();
            GUILayout.Label(property.name, GUILayout.ExpandWidth(false));
            bool newValue = GUILayout.Toggle(value, GUIContent.none, GUILayout.ExpandWidth(false));

            if (value != newValue)
            {
                foreach (KeyValuePair<Material, MaterialInfo> selectedMaterial in this._selectedMaterials)
                {
                    this.SetMaterialDirty(selectedMaterial.Key, out RendererData.MaterialData materialData);
                    if (materialData.dirtyBooleanProperties.ContainsKey(property.name) == false)
                        materialData.dirtyBooleanProperties.Add(property.name, Mathf.Approximately(selectedMaterial.Key.GetFloat(property.name), 1f));
                    selectedMaterial.Key.SetFloat(property.name, newValue ? 1f : 0f);
                }
            }

            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Reset", GUILayout.ExpandWidth(false)))
            {
                foreach (KeyValuePair<Material, MaterialInfo> selectedMaterial in this._selectedMaterials)
                    if (this._dirtyRenderers.TryGetValue(selectedMaterial.Value.renderer, out RendererData rendererData) &&
                        rendererData.dirtyMaterials.TryGetValue(selectedMaterial.Key, out RendererData.MaterialData materialData) &&
                    materialData.dirtyBooleanProperties.ContainsKey(property.name))
                {
                    selectedMaterial.Key.SetFloat(property.name, materialData.dirtyBooleanProperties[property.name] ? 1f : 0f);
                    materialData.dirtyBooleanProperties.Remove(property.name);
                    this.TryResetMaterial(selectedMaterial.Key, materialData, rendererData);
                }
            }

            GUILayout.EndHorizontal();
        }

        private void EnumDrawer(ShaderProperty property)
        {
            int key = Mathf.RoundToInt(this._selectedMaterials.First().Key.GetFloat(property.name));

            GUILayout.BeginHorizontal();
            GUILayout.Label(property.name, GUILayout.ExpandWidth(false));
            int newKey = key;
            foreach (KeyValuePair<int, string> pair in property.enumValues)
            {
                if (GUILayout.Toggle(pair.Key == newKey, pair.Value))
                    newKey = pair.Key;
            }
            if (newKey != key)
            {
                foreach (KeyValuePair<Material, MaterialInfo> selectedMaterial in this._selectedMaterials)
                {
                    this.SetMaterialDirty(selectedMaterial.Key, out RendererData.MaterialData materialData);
                    if (materialData.dirtyEnumProperties.ContainsKey(property.name) == false)
                        materialData.dirtyEnumProperties.Add(property.name, Mathf.RoundToInt(selectedMaterial.Key.GetFloat(property.name)));
                    selectedMaterial.Key.SetFloat(property.name, newKey);
                }
            }

            if (GUILayout.Button("Reset", GUILayout.ExpandWidth(false)))
            {
                foreach (KeyValuePair<Material, MaterialInfo> selectedMaterial in this._selectedMaterials)
                    if (this._dirtyRenderers.TryGetValue(selectedMaterial.Value.renderer, out RendererData rendererData) &&
                        rendererData.dirtyMaterials.TryGetValue(selectedMaterial.Key, out RendererData.MaterialData materialData) &&
                    materialData.dirtyEnumProperties.ContainsKey(property.name))
                {
                    selectedMaterial.Key.SetFloat(property.name, materialData.dirtyEnumProperties[property.name]);
                    materialData.dirtyEnumProperties.Remove(property.name);
                    this.TryResetMaterial(selectedMaterial.Key, materialData, rendererData);
                }
            }
            GUILayout.EndHorizontal();
        }

        private void Vector4Drawer(ShaderProperty property)
        {
            Vector4 value = this._selectedMaterials.First().Key.GetVector(property.name);

            GUILayout.BeginHorizontal();
            GUILayout.Label(property.name, GUILayout.ExpandWidth(false));
            Vector4 newValue = value;

            GUILayout.Label("W", GUILayout.ExpandWidth(false));
            string stringValue = value.w.ToString("0.00");
            string stringNewValue = GUILayout.TextField(stringValue);
            if (stringNewValue != stringValue)
            {
                float parsedValue;
                if (float.TryParse(stringNewValue, out parsedValue))
                    newValue.w = parsedValue;
            }

            GUILayout.Label("X", GUILayout.ExpandWidth(false));
            stringValue = value.x.ToString("0.00");
            stringNewValue = GUILayout.TextField(stringValue);
            if (stringNewValue != stringValue)
            {
                float parsedValue;
                if (float.TryParse(stringNewValue, out parsedValue))
                    newValue.x = parsedValue;
            }

            GUILayout.Label("Y", GUILayout.ExpandWidth(false));
            stringValue = value.y.ToString("0.00");
            stringNewValue = GUILayout.TextField(stringValue);
            if (stringNewValue != stringValue)
            {
                float parsedValue;
                if (float.TryParse(stringNewValue, out parsedValue))
                    newValue.y = parsedValue;
            }

            GUILayout.Label("Z", GUILayout.ExpandWidth(false));
            stringValue = value.z.ToString("0.00");
            stringNewValue = GUILayout.TextField(stringValue);
            if (stringNewValue != stringValue)
            {
                float parsedValue;
                if (float.TryParse(stringNewValue, out parsedValue))
                    newValue.z = parsedValue;
            }

            if (value != newValue)
            {
                foreach (KeyValuePair<Material, MaterialInfo> selectedMaterial in this._selectedMaterials)
                {
                    this.SetMaterialDirty(selectedMaterial.Key, out RendererData.MaterialData materialData);
                    if (materialData.dirtyVector4Properties.ContainsKey(property.name) == false)
                        materialData.dirtyVector4Properties.Add(property.name, selectedMaterial.Key.GetVector(property.name));
                    selectedMaterial.Key.SetVector(property.name, newValue);
                }
            }

            if (GUILayout.Button("Reset", GUILayout.ExpandWidth(false)))
            {
                foreach (KeyValuePair<Material, MaterialInfo> selectedMaterial in this._selectedMaterials)
                    if (this._dirtyRenderers.TryGetValue(selectedMaterial.Value.renderer, out RendererData rendererData) &&
                        rendererData.dirtyMaterials.TryGetValue(selectedMaterial.Key, out RendererData.MaterialData materialData) &&
                        materialData.dirtyVector4Properties.ContainsKey(property.name))
                    {
                        selectedMaterial.Key.SetVector(property.name, materialData.dirtyVector4Properties[property.name]);
                        materialData.dirtyVector4Properties.Remove(property.name);
                        this.TryResetMaterial(selectedMaterial.Key, materialData, rendererData);
                    }
            }
            GUILayout.EndHorizontal();
        }

        private void KeywordsDrawer()
        {
            GUILayout.BeginHorizontal();
            this._keywordsScroll = GUILayout.BeginScrollView(this._keywordsScroll, GUI.skin.box, GUILayout.Height(60f));
            Material material = this._selectedMaterials.First().Key;
            foreach (string keyword in material.shaderKeywords)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(keyword);
                if (GUILayout.Button("X", GUILayout.ExpandWidth(false)))
                {
                    foreach (KeyValuePair<Material, MaterialInfo> selectedMaterial in this._selectedMaterials)
                    {
                        if (selectedMaterial.Key.IsKeywordEnabled(keyword))
                        {
                            this.SetMaterialDirty(selectedMaterial.Key, out RendererData.MaterialData materialData);
                            bool inEnabled = materialData.enabledKeywords.Contains(keyword);
                            if (inEnabled) //Keyword was added artifically
                                materialData.enabledKeywords.Remove(keyword);
                            else //Keyword here in the first place
                                materialData.disabledKeywords.Add(keyword);
                            selectedMaterial.Key.DisableKeyword(keyword);
                        }
                    }
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();
            if (GUILayout.Button("Reset", GUILayout.ExpandWidth(false)))
            {
                foreach (KeyValuePair<Material, MaterialInfo> selectedMaterial in this._selectedMaterials)
                    if (this._dirtyRenderers.TryGetValue(selectedMaterial.Value.renderer, out RendererData rendererData) &&
                        rendererData.dirtyMaterials.TryGetValue(selectedMaterial.Key, out RendererData.MaterialData materialData))
                    {
                        foreach (string enabledKeyword in materialData.enabledKeywords)
                            selectedMaterial.Key.DisableKeyword(enabledKeyword);
                        materialData.enabledKeywords.Clear();
                        foreach (string disabledKeyword in materialData.disabledKeywords)
                            selectedMaterial.Key.EnableKeyword(disabledKeyword);
                        materialData.disabledKeywords.Clear();
                        this.TryResetMaterial(selectedMaterial.Key, materialData, rendererData);
                    }
                
            }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            this._keywordInput = GUILayout.TextField(this._keywordInput);
            if (GUILayout.Button("Add Keyword", GUILayout.ExpandWidth(false)))
            {
                foreach (KeyValuePair<Material, MaterialInfo> selectedMaterial in this._selectedMaterials)
                {
                    if (selectedMaterial.Key.IsKeywordEnabled(this._keywordInput) == false)
                    {
                        this.SetMaterialDirty(selectedMaterial.Key, out RendererData.MaterialData materialData);
                        if (materialData.disabledKeywords.Contains(this._keywordInput)) //Keyword was here in the first place
                            materialData.disabledKeywords.Remove(this._keywordInput);
                        else //Keyword is added artificially
                            materialData.enabledKeywords.Add(this._keywordInput);
                        selectedMaterial.Key.EnableKeyword(this._keywordInput);
                    }
                }
                this._keywordInput = "";
            }

            GUILayout.EndHorizontal();
        }
        #endregion

        #region Saves
        private void OnSceneLoad(string path, XmlNode node)
        {
            if (node == null)
                return;
            node = node.CloneNode(true);
            this.ExecuteDelayed(() =>
            {
                List<KeyValuePair<int, ObjectCtrlInfo>> dic = new SortedDictionary<int, ObjectCtrlInfo>(Studio.Studio.Instance.dicObjectCtrl).ToList();
                foreach (XmlNode childNode in node.ChildNodes)
                {
                    try
                    {
                        switch (childNode.Name)
                        {
                            case "renderer":
                                int objectIndex = XmlConvert.ToInt32(childNode.Attributes["objectIndex"].Value);
                                if (objectIndex >= dic.Count)
                                    continue;
                                Transform t = dic[objectIndex].Value.guideObject.transformTarget;
                                string rendererPath = childNode.Attributes["rendererPath"].Value;
                                Transform child;
                                child = string.IsNullOrEmpty(rendererPath) ? t : t.Find(rendererPath);
                                if (child == null)
                                    continue;
                                Renderer renderer = child.GetComponent<Renderer>();
                                if (renderer != null && this.SetRendererDirty(renderer, out RendererData rendererData))
                                {
                                    renderer.enabled = childNode.Attributes["enabled"] == null || XmlConvert.ToBoolean(childNode.Attributes["enabled"].Value);
                                    renderer.shadowCastingMode = (ShadowCastingMode)XmlConvert.ToInt32(childNode.Attributes["shadowCastingMode"].Value);
                                    renderer.receiveShadows = XmlConvert.ToBoolean(childNode.Attributes["receiveShadows"].Value);

                                    foreach (XmlNode grandChildNode in childNode.ChildNodes)
                                    {
                                        switch (grandChildNode.Name)
                                        {
                                            case "material":
                                                int index = XmlConvert.ToInt32(grandChildNode.Attributes["index"].Value);
                                                Material mat = renderer.materials[index];
                                                this.SetMaterialDirty(mat, out RendererData.MaterialData materialData, rendererData);
                                                if (grandChildNode.Attributes["renderQueue"] != null)
                                                {
                                                    materialData.originalRenderQueue = mat.renderQueue;
                                                    materialData.hasRenderQueue = true;
                                                    mat.renderQueue = XmlConvert.ToInt32(grandChildNode.Attributes["renderQueue"].Value);

                                                }

                                                foreach (XmlNode propertyGroupNode in grandChildNode.ChildNodes)
                                                {
                                                    switch (propertyGroupNode.Name)
                                                    {
                                                        case "colors":
                                                            foreach (XmlNode property in propertyGroupNode.ChildNodes)
                                                            {
                                                                string key = property.Attributes["key"].Value;
                                                                Color c = Color.black;
                                                                c.r = XmlConvert.ToSingle(property.Attributes["r"].Value);
                                                                c.g = XmlConvert.ToSingle(property.Attributes["g"].Value);
                                                                c.b = XmlConvert.ToSingle(property.Attributes["b"].Value);
                                                                c.a = XmlConvert.ToSingle(property.Attributes["a"].Value);
                                                                materialData.dirtyColorProperties.Add(key, mat.GetColor(key));
                                                                mat.SetColor(key, c);
                                                            }
                                                            break;
                                                        case "textures":
                                                            foreach (XmlNode property in propertyGroupNode.ChildNodes)
                                                            {
                                                                string key = property.Attributes["key"].Value;
                                                                string texturePath = property.Attributes["path"].Value;
                                                                if (string.IsNullOrEmpty(texturePath))
                                                                    continue;
                                                                int i = texturePath.IndexOf(_texturesDir, StringComparison.OrdinalIgnoreCase);
                                                                if (i > 0) //Doing this because I fucked up an older version
                                                                    texturePath = texturePath.Substring(i);
                                                                texturePath = Path.GetFullPath(texturePath);
                                                                Texture2D texture = this.GetTexture(texturePath);
                                                                if (texture != null)
                                                                {
                                                                    materialData.dirtyTextureProperties.Add(key, new RendererData.MaterialData.TextureData()
                                                                    {
                                                                        currentTexturePath = texturePath,
                                                                        originalTexture = mat.GetTexture(key)
                                                                    });
                                                                    mat.SetTexture(key, texture);
                                                                }
                                                            }
                                                            break;
                                                        case "textureOffsets":
                                                            foreach (XmlNode property in propertyGroupNode.ChildNodes)
                                                            {
                                                                string key = property.Attributes["key"].Value;
                                                                Vector2 offset;
                                                                offset.x = XmlConvert.ToSingle(property.Attributes["x"].Value);
                                                                offset.y = XmlConvert.ToSingle(property.Attributes["y"].Value);
                                                                materialData.dirtyTextureOffsetProperties.Add(key, mat.GetTextureOffset(key));
                                                                mat.SetTextureOffset(key, offset);
                                                            }
                                                            break;
                                                        case "textureScales":
                                                            foreach (XmlNode property in propertyGroupNode.ChildNodes)
                                                            {
                                                                string key = property.Attributes["key"].Value;
                                                                Vector2 scale;
                                                                scale.x = XmlConvert.ToSingle(property.Attributes["x"].Value);
                                                                scale.y = XmlConvert.ToSingle(property.Attributes["y"].Value);
                                                                materialData.dirtyTextureScaleProperties.Add(key, mat.GetTextureScale(key));
                                                                mat.SetTextureScale(key, scale);
                                                            }
                                                            break;
                                                        case "floats":
                                                            foreach (XmlNode property in propertyGroupNode.ChildNodes)
                                                            {
                                                                string key = property.Attributes["key"].Value;
                                                                float value = XmlConvert.ToSingle(property.Attributes["value"].Value);
                                                                materialData.dirtyFloatProperties.Add(key, mat.GetFloat(key));
                                                                mat.SetFloat(key, value);
                                                            }
                                                            break;
                                                        case "booleans":
                                                            foreach (XmlNode property in propertyGroupNode.ChildNodes)
                                                            {
                                                                string key = property.Attributes["key"].Value;
                                                                bool value = XmlConvert.ToBoolean(property.Attributes["value"].Value);
                                                                materialData.dirtyBooleanProperties.Add(key, Mathf.RoundToInt(mat.GetFloat(key)) == 1);
                                                                mat.SetFloat(key, value ? 1 : 0);
                                                            }
                                                            break;
                                                        case "enums":
                                                            foreach (XmlNode property in propertyGroupNode.ChildNodes)
                                                            {
                                                                string key = property.Attributes["key"].Value;
                                                                int value = XmlConvert.ToInt32(property.Attributes["value"].Value);
                                                                materialData.dirtyEnumProperties.Add(key, Mathf.RoundToInt(mat.GetFloat(key)));
                                                                mat.SetFloat(key, value);
                                                            }
                                                            break;
                                                        case "vector4s":
                                                            foreach (XmlNode property in propertyGroupNode.ChildNodes)
                                                            {
                                                                string key = property.Attributes["key"].Value;
                                                                Vector4 scale;
                                                                scale.w = XmlConvert.ToSingle(property.Attributes["w"].Value);
                                                                scale.x = XmlConvert.ToSingle(property.Attributes["x"].Value);
                                                                scale.y = XmlConvert.ToSingle(property.Attributes["y"].Value);
                                                                scale.z = XmlConvert.ToSingle(property.Attributes["z"].Value);
                                                                materialData.dirtyVector4Properties.Add(key, mat.GetVector(key));
                                                                mat.SetVector(key, scale);
                                                            }
                                                            break;
                                                        case "enabledKeywords":
                                                            foreach (XmlNode property in propertyGroupNode.ChildNodes)
                                                            {
                                                                string keyword = property.Attributes["value"].Value;
                                                                materialData.enabledKeywords.Add(keyword);
                                                                mat.EnableKeyword(keyword);
                                                            }
                                                            break;
                                                        case "disabledKeywords":
                                                            foreach (XmlNode property in propertyGroupNode.ChildNodes)
                                                            {
                                                                string keyword = property.Attributes["value"].Value;
                                                                materialData.disabledKeywords.Add(keyword);
                                                                mat.DisableKeyword(keyword);
                                                            }
                                                            break;
                                                    }
                                                }
                                                break;
                                        }
                                    }
                                }
                                break;
                        }

                    }
                    catch (Exception e)
                    {
                        UnityEngine.Debug.LogError("Exception happened while loading item " + childNode.OuterXml + "\n" + e);
                    }
                }
            }, 8);
        }

        private void OnSceneSave(string path, XmlTextWriter writer)
        {
            List<KeyValuePair<int, ObjectCtrlInfo>> dic = new SortedDictionary<int, ObjectCtrlInfo>(Studio.Studio.Instance.dicObjectCtrl).ToList();
            foreach (KeyValuePair<Renderer, RendererData> rendererPair in this._dirtyRenderers)
            {
                int objectIndex = -1;
                try
                {
                    Transform t = rendererPair.Key.transform;
                    while ((objectIndex = dic.FindIndex(e => e.Value.guideObject.transformTarget == t)) == -1)
                        t = t.parent;

                    writer.WriteStartElement("renderer");
                    writer.WriteAttributeString("objectIndex", XmlConvert.ToString(objectIndex));
                    writer.WriteAttributeString("rendererPath", rendererPair.Key.transform.GetPathFrom(t));

                    writer.WriteAttributeString("enabled", XmlConvert.ToString(rendererPair.Key.enabled));
                    writer.WriteAttributeString("shadowCastingMode", XmlConvert.ToString((int)rendererPair.Key.shadowCastingMode));
                    writer.WriteAttributeString("receiveShadows", XmlConvert.ToString(rendererPair.Key.receiveShadows));

                    List<Material> materials = rendererPair.Key.sharedMaterials.ToList();
                    foreach (KeyValuePair<Material, RendererData.MaterialData> materialPair in rendererPair.Value.dirtyMaterials)
                    {
                        if (materialPair.Key == null)
                            continue;
                        writer.WriteStartElement("material");
                        writer.WriteAttributeString("index", XmlConvert.ToString(materials.IndexOf(materialPair.Key)));
                        if (materialPair.Value.hasRenderQueue)
                            writer.WriteAttributeString("renderQueue", XmlConvert.ToString(materialPair.Key.renderQueue));

                        writer.WriteStartElement("colors");
                        foreach (KeyValuePair<string, Color> pair in materialPair.Value.dirtyColorProperties)
                        {
                            writer.WriteStartElement("color");
                            writer.WriteAttributeString("key", pair.Key);
                            Color c = materialPair.Key.GetColor(pair.Key);
                            writer.WriteAttributeString("r", XmlConvert.ToString(c.r));
                            writer.WriteAttributeString("g", XmlConvert.ToString(c.g));
                            writer.WriteAttributeString("b", XmlConvert.ToString(c.b));
                            writer.WriteAttributeString("a", XmlConvert.ToString(c.a));
                            writer.WriteEndElement();
                        }
                        writer.WriteEndElement();

                        writer.WriteStartElement("textures");
                        foreach (KeyValuePair<string, RendererData.MaterialData.TextureData> pair in materialPair.Value.dirtyTextureProperties)
                        {
                            writer.WriteStartElement("texture");
                            writer.WriteAttributeString("key", pair.Key);
                            writer.WriteAttributeString("path", pair.Value.currentTexturePath.Substring(this._currentDirectory.Length + 1));
                            writer.WriteEndElement();
                        }
                        writer.WriteEndElement();

                        writer.WriteStartElement("textureOffsets");
                        foreach (KeyValuePair<string, Vector2> pair in materialPair.Value.dirtyTextureOffsetProperties)
                        {
                            writer.WriteStartElement("textureOffset");
                            writer.WriteAttributeString("key", pair.Key);
                            Vector2 offset = materialPair.Key.GetTextureOffset(pair.Key);
                            writer.WriteAttributeString("x", XmlConvert.ToString(offset.x));
                            writer.WriteAttributeString("y", XmlConvert.ToString(offset.y));
                            writer.WriteEndElement();
                        }
                        writer.WriteEndElement();

                        writer.WriteStartElement("textureScales");
                        foreach (KeyValuePair<string, Vector2> pair in materialPair.Value.dirtyTextureScaleProperties)
                        {
                            writer.WriteStartElement("textureScale");
                            writer.WriteAttributeString("key", pair.Key);
                            Vector2 scale = materialPair.Key.GetTextureScale(pair.Key);
                            writer.WriteAttributeString("x", XmlConvert.ToString(scale.x));
                            writer.WriteAttributeString("y", XmlConvert.ToString(scale.y));
                            writer.WriteEndElement();
                        }
                        writer.WriteEndElement();

                        writer.WriteStartElement("floats");
                        foreach (KeyValuePair<string, float> pair in materialPair.Value.dirtyFloatProperties)
                        {
                            writer.WriteStartElement("float");
                            writer.WriteAttributeString("key", pair.Key);
                            writer.WriteAttributeString("value", XmlConvert.ToString(materialPair.Key.GetFloat(pair.Key)));
                            writer.WriteEndElement();
                        }
                        writer.WriteEndElement();

                        writer.WriteStartElement("booleans");
                        foreach (KeyValuePair<string, bool> pair in materialPair.Value.dirtyBooleanProperties)
                        {
                            writer.WriteStartElement("boolean");
                            writer.WriteAttributeString("key", pair.Key);
                            writer.WriteAttributeString("value", XmlConvert.ToString(Mathf.RoundToInt(materialPair.Key.GetFloat(pair.Key)) == 1));
                            writer.WriteEndElement();
                        }
                        writer.WriteEndElement();

                        writer.WriteStartElement("enums");
                        foreach (KeyValuePair<string, int> pair in materialPair.Value.dirtyEnumProperties)
                        {
                            writer.WriteStartElement("enum");
                            writer.WriteAttributeString("key", pair.Key);
                            writer.WriteAttributeString("value", XmlConvert.ToString(Mathf.RoundToInt(materialPair.Key.GetFloat(pair.Key))));
                            writer.WriteEndElement();
                        }
                        writer.WriteEndElement();

                        writer.WriteStartElement("vector4s");
                        foreach (KeyValuePair<string, Vector4> pair in materialPair.Value.dirtyVector4Properties)
                        {
                            writer.WriteStartElement("vector4");
                            writer.WriteAttributeString("key", pair.Key);
                            Vector4 value = materialPair.Key.GetVector(pair.Key);
                            writer.WriteAttributeString("w", XmlConvert.ToString(value.w));
                            writer.WriteAttributeString("x", XmlConvert.ToString(value.x));
                            writer.WriteAttributeString("y", XmlConvert.ToString(value.y));
                            writer.WriteAttributeString("z", XmlConvert.ToString(value.z));
                            writer.WriteEndElement();
                        }
                        writer.WriteEndElement();

                        writer.WriteStartElement("enabledKeywords");
                        foreach (string keyword in materialPair.Value.enabledKeywords)
                        {
                            writer.WriteStartElement("keyword");
                            writer.WriteAttributeString("value", keyword);
                            writer.WriteEndElement();
                        }
                        writer.WriteEndElement();

                        writer.WriteStartElement("disabledKeywords");
                        foreach (string keyword in materialPair.Value.disabledKeywords)
                        {
                            writer.WriteStartElement("keyword");
                            writer.WriteAttributeString("value", keyword);
                            writer.WriteEndElement();
                        }
                        writer.WriteEndElement();

                        writer.WriteEndElement();
                    }

                    writer.WriteEndElement();

                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogError("Exception happened during save with item " + rendererPair.Key.transform + " index " + objectIndex + "\n" + e);
                }
            }
        }
        #endregion
    }
}