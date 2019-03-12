using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using Harmony;
using Studio;
using ToolBox;
using UnityEngine;

namespace HSPE.AMModules
{
    public class BlendShapesEditor : AdvancedModeModule
    {
        #region Constants
        private static readonly Dictionary<string, string> _skinnedMeshAliases = new Dictionary<string, string>
#if HONEYSELECT
        {
            {"cf_O_head", "Eyes / Mouth"},
            {"cf_O_ha", "Teeth"},
            {"cf_O_matuge", "Eyelashes"},
            {"cf_O_mayuge", "Eyebrows"},
            {"cf_O_sita", "Tongue"},
            {"cf_O_namida01", "Tears 1"},
            {"cf_O_namida02", "Tears 2"},

            {"cm_O_head", "Face"},
            {"cm_O_ha", "Teeth"},
            {"cm_O_mayuge", "Eyebrows"},
            {"cm_O_sita", "Tongue"},
            {"O_hige00", "Jaw"},
        };
#elif KOIKATSU
        {
            {"cf_O_face",  "Eyes / Mouth"},
            {"cf_O_tooth",  "Teeth"},
            {"cf_O_eyeline",  "Upper Eyelashes"},
            {"cf_O_eyeline_low",  "Lower Eyelashes"},
            {"cf_O_mayuge",  "Eyebrows"},
            {"cf_Ohitomi_L",  "Left Eye White"},
            {"cf_Ohitomi_R",  "Right Eye White"},
            {"o_tang",  "Tongue"},
        };
#endif
        #endregion

        #region Statics
        private const string _presetsPath = MainWindow._pluginDir + "BlendShapesPresets\\";
        private static Dictionary<FaceBlendShape, BlendShapesEditor> _instanceByFaceBlendShape = new Dictionary<FaceBlendShape, BlendShapesEditor>();
        private static string[] _presets = new string[0];
        #endregion

        #region Private Types
        private class SkinnedMeshRendererData
        {
            public readonly Dictionary<int, BlendShapeData> dirtyBlendShapes = new Dictionary<int, BlendShapeData>();
            public string path;

            public SkinnedMeshRendererData()
            {
            }

            public SkinnedMeshRendererData(SkinnedMeshRendererData other)
            {
                foreach (KeyValuePair<int, BlendShapeData> kvp in other.dirtyBlendShapes)
                {
                    this.dirtyBlendShapes.Add(kvp.Key, new BlendShapeData() {weight = kvp.Value.weight, originalWeight = kvp.Value.originalWeight});
                }
            }
        }

        private class BlendShapeData
        {
            public float weight;
            public float originalWeight;
        }

        [HarmonyPatch(typeof(FaceBlendShape), "LateUpdate")]
        private class FaceBlendShape_Patches
        {
            [HarmonyAfter("com.joan6694.hsplugins.instrumentation")]
            public static void Postfix(FaceBlendShape __instance)
            {
                BlendShapesEditor editor;
                if (_instanceByFaceBlendShape.TryGetValue(__instance, out editor))
                    editor.FaceBlendShapeOnPostLateUpdate();
            }
        }

        private class SkinnedMeshRendererWrapper
        {
            public SkinnedMeshRenderer renderer;
            public List<SkinnedMeshRendererWrapper> links;
        }
        #endregion

        #region Private Variables
        private Vector2 _skinnedMeshRenderersScroll;
        private Vector2 _blendShapesScroll;
        private readonly List<SkinnedMeshRenderer> _skinnedMeshRenderers = new List<SkinnedMeshRenderer>();
        private readonly Dictionary<SkinnedMeshRenderer, SkinnedMeshRendererData> _dirtySkinnedMeshRenderers = new Dictionary<SkinnedMeshRenderer, SkinnedMeshRendererData>();
        private readonly Dictionary<string, SkinnedMeshRendererData> _headlessDirtySkinnedMeshRenderers = new Dictionary<string, SkinnedMeshRendererData>();
        private int _headlessReconstructionTimeout = 0;
#if HONEYSELECT
        private readonly HashSet<SkinnedMeshRenderer> _eyesSkinnedMeshRenderers = new HashSet<SkinnedMeshRenderer>();
        private readonly HashSet<SkinnedMeshRenderer> _mouthSkinnedMeshRenderers = new HashSet<SkinnedMeshRenderer>();
#endif
        private int _eyesShapesCount = Int32.MaxValue;
        private SkinnedMeshRenderer _skinnedMeshTarget;
        private bool _linkEyesAndEyelashes = true;
        private SkinnedMeshRenderer _eyesMouthRenderer;
        private readonly Dictionary<SkinnedMeshRenderer, SkinnedMeshRendererWrapper> _links = new Dictionary<SkinnedMeshRenderer, SkinnedMeshRendererWrapper>();
        private string _search = "";
        private readonly GenericOCITarget _target;
        private Dictionary<XmlNode, SkinnedMeshRenderer> _secondPassLoadingNodes = new Dictionary<XmlNode, SkinnedMeshRenderer>();
        private bool _showSaveLoadWindow = false;
        private Vector2 _presetsScroll;
        private string _presetName = "";
        private bool _removePresetMode;
        #endregion

        #region Public Fields
        public override AdvancedModeModuleType type { get { return AdvancedModeModuleType.BlendShapes; } }
        public override string displayName { get { return "Blend Shapes"; } }
        public override bool shouldDisplay { get { return this._skinnedMeshRenderers.Any(r => r != null && r.sharedMesh != null && r.sharedMesh.blendShapeCount > 0); } }
        #endregion

        #region Unity Methods
        public BlendShapesEditor(PoseController parent, GenericOCITarget target) : base(parent)
        {
            this._parent.onLateUpdate += this.LateUpdate;
            this._parent.onDisable += this.OnDisable;
            this._target = target;
            if (this._target.type == GenericOCITarget.Type.Character)
                this.Init();
            MainWindow._self.ExecuteDelayed(this.RefreshSkinnedMeshRendererList);
        }

        private void LateUpdate()
        {
            if (this._headlessReconstructionTimeout >= 0)
            {
                this._headlessReconstructionTimeout--;
                foreach (KeyValuePair<string, SkinnedMeshRendererData> pair in new Dictionary<string, SkinnedMeshRendererData>(this._headlessDirtySkinnedMeshRenderers))
                {
                    Transform t = this._parent.transform.Find(pair.Key);
                    if (t != null)
                    {
                        SkinnedMeshRenderer renderer = t.GetComponent<SkinnedMeshRenderer>();
                        if (renderer != null)
                        {
                            this._dirtySkinnedMeshRenderers.Add(renderer, pair.Value);
                            this._headlessDirtySkinnedMeshRenderers.Remove(pair.Key);
                        }
                    }
                }
            }
            else if (this._headlessDirtySkinnedMeshRenderers.Count != 0)
                this._headlessDirtySkinnedMeshRenderers.Clear();
            if (this._target.type == GenericOCITarget.Type.Item)
                this.ApplyBlendShapeWeights();
        }

        public void OnGUI()
        {
            if (this._showSaveLoadWindow == false)
                return;
            GUIUtility.ScaleAroundPivot(Vector2.one * (MainWindow._self.uiScale * MainWindow._self.resolutionRatio), new Vector2(Screen.width, Screen.height));
            Color c = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.6f, 0.6f, 0.6f, 0.2f);
            Rect windowRect = Rect.MinMaxRect(MainWindow._self._advancedModeRect.xMin - 180, MainWindow._self._advancedModeRect.yMin, MainWindow._self._advancedModeRect.xMin, MainWindow._self._advancedModeRect.yMax);
            for (int i = 0; i < 3; ++i)
                GUI.Box(windowRect, "", MainWindow._customBoxStyle);
            GUI.backgroundColor = c;
            GUILayout.Window(3, windowRect, this.SaveLoadWindow, "Presets");
            GUIUtility.ScaleAroundPivot(Vector2.one, new Vector2(Screen.width, Screen.height));
        }

        private void OnDisable()
        {
            if (this._dirtySkinnedMeshRenderers.Count != 0)
                foreach (KeyValuePair<SkinnedMeshRenderer, SkinnedMeshRendererData> kvp in this._dirtySkinnedMeshRenderers)
                    foreach (KeyValuePair<int, BlendShapeData> weight in kvp.Value.dirtyBlendShapes)
                        kvp.Key.SetBlendShapeWeight(weight.Key, weight.Value.originalWeight);
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            this._parent.onLateUpdate -= this.LateUpdate;
            this._parent.onDisable -= this.OnDisable;
            _instanceByFaceBlendShape = new Dictionary<FaceBlendShape, BlendShapesEditor>(_instanceByFaceBlendShape.Where(e => e.Key != null).ToDictionary(e => e.Key, e => e.Value));
        }
        #endregion

        #region Public Methods
        public override void OnCharacterReplaced()
        {
            Dictionary<FaceBlendShape, BlendShapesEditor> newInstanceByFBS = null;
            foreach (KeyValuePair<FaceBlendShape, BlendShapesEditor> pair in _instanceByFaceBlendShape)
            {
                if (pair.Key == null)
                {
                    newInstanceByFBS = new Dictionary<FaceBlendShape, BlendShapesEditor>();
                    break;
                }
            }
            if (newInstanceByFBS != null)
            {
                foreach (KeyValuePair<FaceBlendShape, BlendShapesEditor> pair in _instanceByFaceBlendShape)
                {
                    if (pair.Key != null)
                        newInstanceByFBS.Add(pair.Key, pair.Value);
                }
                _instanceByFaceBlendShape = newInstanceByFBS;
            }
            this._links.Clear();
#if HONEYSELECT
            this._eyesSkinnedMeshRenderers.Clear();
            this._mouthSkinnedMeshRenderers.Clear();
#endif

            this.Init();

            this.RefreshSkinnedMeshRendererList();
            MainWindow._self.ExecuteDelayed(this.RefreshSkinnedMeshRendererList);
        }

        public override void OnLoadClothesFile()
        {
            this.RefreshSkinnedMeshRendererList();
            MainWindow._self.ExecuteDelayed(this.RefreshSkinnedMeshRendererList);
        }
#if HONEYSELECT
        public override void OnCoordinateReplaced(CharDefine.CoordinateType coordinateType, bool force)
#elif KOIKATSU
        public override void OnCoordinateReplaced(ChaFileDefine.CoordinateType coordinateType, bool force)
#endif
        {
            this.RefreshSkinnedMeshRendererList();
            MainWindow._self.ExecuteDelayed(this.RefreshSkinnedMeshRendererList);
        }

        public override void OnParentage(TreeNodeObject parent, TreeNodeObject child)
        {
            this.RefreshSkinnedMeshRendererList();
            MainWindow._self.ExecuteDelayed(this.RefreshSkinnedMeshRendererList);
        }

        public override void GUILogic()
        {
            Color c = GUI.color;
            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical(GUILayout.ExpandWidth(false));

            this._skinnedMeshRenderersScroll = GUILayout.BeginScrollView(this._skinnedMeshRenderersScroll, false, true, GUI.skin.horizontalScrollbar, GUI.skin.verticalScrollbar, GUI.skin.box, GUILayout.ExpandWidth(false));
            foreach (SkinnedMeshRenderer r in this._skinnedMeshRenderers)
            {
                if (r == null || r.sharedMesh == null || r.sharedMesh.blendShapeCount == 0)
                    continue;
                if (this._dirtySkinnedMeshRenderers.ContainsKey(r))
                    GUI.color = Color.magenta;
                if (ReferenceEquals(r, this._skinnedMeshTarget))
                    GUI.color = Color.cyan;
                string dName;
                if (_skinnedMeshAliases.TryGetValue(r.name, out dName) == false)
                    dName = r.name;
                if (GUILayout.Button(dName + (this._dirtySkinnedMeshRenderers.ContainsKey(r) ? "*" : "")))
                    this._skinnedMeshTarget = r;
                GUI.color = c;
            }
            GUILayout.EndScrollView();

            GUI.color = Color.green;
            if (GUILayout.Button("Save/Load preset"))
            {
                this._showSaveLoadWindow = true;
                this.RefreshPresets();
            }
            GUI.color = c;

            if (this._target.type == GenericOCITarget.Type.Character)
                this._linkEyesAndEyelashes = GUILayout.Toggle(this._linkEyesAndEyelashes, "Link eyes components");
            if (GUILayout.Button("Force refresh list"))
                this.RefreshSkinnedMeshRendererList();
            GUI.color = Color.red;
            if (GUILayout.Button("Reset all"))
            {
                foreach (KeyValuePair<SkinnedMeshRenderer, SkinnedMeshRendererData> pair in new Dictionary<SkinnedMeshRenderer, SkinnedMeshRendererData>(this._dirtySkinnedMeshRenderers))
                    this.SetMeshRendererNotDirty(pair.Key);
            }
            GUI.color = c;
            GUILayout.EndVertical();


            if (this._skinnedMeshTarget != null)
            {
                GUILayout.BeginVertical(GUI.skin.box);
                GUILayout.BeginHorizontal();
                this._search = GUILayout.TextField(this._search, GUILayout.ExpandWidth(true));
                if (GUILayout.Button("X", GUILayout.ExpandWidth(false)))
                    this._search = "";
                GUILayout.EndHorizontal();

                this._blendShapesScroll = GUILayout.BeginScrollView(this._blendShapesScroll, false, true, GUILayout.ExpandWidth(false));

                SkinnedMeshRendererData data = null;
                this._dirtySkinnedMeshRenderers.TryGetValue(this._skinnedMeshTarget, out data);
#if HONEYSELECT
                bool eyesSkinnedMesh = this._eyesSkinnedMeshRenderers.Contains(this._skinnedMeshTarget);
                bool mouthSkinnedMesh = this._mouthSkinnedMeshRenderers.Contains(this._skinnedMeshTarget);
#endif
                bool zeroResult = true;
                for (int i = 0; i < this._skinnedMeshTarget.sharedMesh.blendShapeCount; ++i)
                {
                    int displayI = i;
#if HONEYSELECT
                    if (eyesSkinnedMesh && mouthSkinnedMesh)
                    {
                        if (i == 0)
                            GUILayout.Label("Eyes", GUI.skin.box);
                        else if (i == this._eyesShapesCount)
                            GUILayout.Label("Mouth", GUI.skin.box);
                        if (displayI >= this._eyesShapesCount)
                            displayI -= this._eyesShapesCount;
                    }
#elif KOIKATSU
                    if (this._eyesMouthRenderer == this._skinnedMeshTarget)
                    {
                        if (i == 0)
                            GUILayout.Label("Eyes", GUI.skin.box);
                        else if (i == this._eyesShapesCount + 1)
                            GUILayout.Label("Mouth", GUI.skin.box);
                        if (displayI > this._eyesShapesCount)
                            displayI -= this._eyesShapesCount;
                    }
#endif
                    string blendShapeName = this._skinnedMeshTarget.sharedMesh.GetBlendShapeName(i);
                    if (blendShapeName.IndexOf(this._search, StringComparison.CurrentCultureIgnoreCase) != -1)
                    {
                        zeroResult = false;
                        float blendShapeWeight;

                        BlendShapeData bsData;
                        if (data != null && data.dirtyBlendShapes.TryGetValue(i, out bsData))
                        {
                            blendShapeWeight = bsData.weight;
                            GUI.color = Color.magenta;
                        }
                        else
                            blendShapeWeight = this._skinnedMeshTarget.GetBlendShapeWeight(i);

                        GUILayout.BeginHorizontal();

                        GUILayout.BeginVertical(GUILayout.ExpandHeight(false));

                        GUILayout.BeginHorizontal();
                        GUILayout.Label($"{displayI} {blendShapeName}");
                        GUILayout.FlexibleSpace();
                        GUILayout.Label(blendShapeWeight.ToString("000"), GUILayout.ExpandWidth(false));
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        int newBlendShapeWeight = Mathf.RoundToInt(GUILayout.HorizontalSlider(blendShapeWeight, 0f, 100f));
                        if (GUILayout.Button("-1", GUILayout.ExpandWidth(false)) && newBlendShapeWeight != 0)
                            newBlendShapeWeight -= 1;
                        if (GUILayout.Button("+1", GUILayout.ExpandWidth(false)) && newBlendShapeWeight != 100)
                            newBlendShapeWeight += 1;
                        GUILayout.EndHorizontal();
                        if (Mathf.Approximately(newBlendShapeWeight, blendShapeWeight) == false)
                        {
                            data = this.SetMeshRendererDirty(this._skinnedMeshTarget);
                            if (data.dirtyBlendShapes.TryGetValue(i, out bsData) == false)
                            {
                                bsData = new BlendShapeData();
                                bsData.originalWeight = blendShapeWeight;
                                data.dirtyBlendShapes.Add(i, bsData);
                            }
                            bsData.weight = newBlendShapeWeight;
                            if (this._linkEyesAndEyelashes)
                            {
                                SkinnedMeshRendererWrapper wrapper;
                                if (this._links.TryGetValue(this._skinnedMeshTarget, out wrapper))
                                {
                                    foreach (SkinnedMeshRendererWrapper link in wrapper.links)
                                    {
                                        if (i >= link.renderer.sharedMesh.blendShapeCount)
                                            continue;
                                        data = this.SetMeshRendererDirty(link.renderer);
                                        if (data.dirtyBlendShapes.TryGetValue(i, out bsData) == false)
                                        {
                                            bsData = new BlendShapeData();
                                            bsData.originalWeight = blendShapeWeight;
                                            data.dirtyBlendShapes.Add(i, bsData);
                                        }
                                        bsData.weight = newBlendShapeWeight;
                                    }
                                }
                            }
                        }
                        GUILayout.EndVertical();

                        GUI.color = Color.red;

                        if (GUILayout.Button("Reset", GUILayout.ExpandWidth(false), GUILayout.Height(50)) && data != null && data.dirtyBlendShapes.TryGetValue(i, out bsData))
                        {
                            this._skinnedMeshTarget.SetBlendShapeWeight(i, bsData.originalWeight);
                            data.dirtyBlendShapes.Remove(i);
                            if (data.dirtyBlendShapes.Count == 0)
                                this.SetMeshRendererNotDirty(this._skinnedMeshTarget);

                            if (this._linkEyesAndEyelashes)
                            {
                                SkinnedMeshRendererWrapper wrapper;
                                if (this._links.TryGetValue(this._skinnedMeshTarget, out wrapper))
                                {
                                    foreach (SkinnedMeshRendererWrapper link in wrapper.links)
                                    {
                                        if (this._dirtySkinnedMeshRenderers.TryGetValue(link.renderer, out data) && data.dirtyBlendShapes.TryGetValue(i, out bsData))
                                        {
                                            link.renderer.SetBlendShapeWeight(i, bsData.originalWeight);
                                            data.dirtyBlendShapes.Remove(i);
                                            if (data.dirtyBlendShapes.Count == 0)
                                                this.SetMeshRendererNotDirty(link.renderer);
                                        }
                                    }
                                }
                            }
                        }
                        GUILayout.EndHorizontal();
                        GUI.color = c;
                    }
                }

                if (zeroResult)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();
                }

                GUILayout.EndScrollView();

                GUILayout.BeginHorizontal();
                GUI.color = Color.red;
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Reset", GUILayout.ExpandWidth(false)))
                {
                    this.SetMeshRendererNotDirty(this._skinnedMeshTarget);
                    if (this._linkEyesAndEyelashes)
                    {
                        SkinnedMeshRendererWrapper wrapper;
                        if (this._links.TryGetValue(this._skinnedMeshTarget, out wrapper))
                            foreach (SkinnedMeshRendererWrapper link in wrapper.links)
                                this.SetMeshRendererNotDirty(link.renderer);
                    }
                }
                GUI.color = c;
                GUILayout.EndHorizontal();

                GUILayout.EndVertical();
            }
            else
            {
                GUILayout.BeginVertical();
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
            }


            GUILayout.EndHorizontal();
        }

        public void LoadFrom(BlendShapesEditor other)
        {
            MainWindow._self.ExecuteDelayed(() =>
            {
                foreach (KeyValuePair<SkinnedMeshRenderer, SkinnedMeshRendererData> kvp in other._dirtySkinnedMeshRenderers)
                {
                    Transform obj = this._parent.transform.Find(kvp.Key.transform.GetPathFrom(other._parent.transform));
                    if (obj != null)
                    {
                        SkinnedMeshRenderer renderer = obj.GetComponent<SkinnedMeshRenderer>();
                        this._dirtySkinnedMeshRenderers.Add(renderer, new SkinnedMeshRendererData(kvp.Value));
                    }
                }
                this._blendShapesScroll = other._blendShapesScroll;
                this._skinnedMeshRenderersScroll = other._skinnedMeshRenderersScroll;
            }, 2);
        }

        public override int SaveXml(XmlTextWriter xmlWriter)
        {
            int written = 0;
            if (this._dirtySkinnedMeshRenderers.Count != 0)
            {
                xmlWriter.WriteStartElement("skinnedMeshes");
                foreach (KeyValuePair<SkinnedMeshRenderer, SkinnedMeshRendererData> kvp in this._dirtySkinnedMeshRenderers)
                {
                    xmlWriter.WriteStartElement("skinnedMesh");
                    xmlWriter.WriteAttributeString("name", kvp.Key.transform.GetPathFrom(this._parent.transform));

                    foreach (KeyValuePair<int, BlendShapeData> weight in kvp.Value.dirtyBlendShapes)
                    {
                        xmlWriter.WriteStartElement("blendShape");
                        xmlWriter.WriteAttributeString("index", XmlConvert.ToString(weight.Key));
                        xmlWriter.WriteAttributeString("weight", XmlConvert.ToString(weight.Value.weight));
                        xmlWriter.WriteEndElement();
                    }

                    xmlWriter.WriteEndElement();
                    ++written;
                }
                xmlWriter.WriteEndElement();
            }
            return written;
        }

        public override bool LoadXml(XmlNode xmlNode)
        {
            this.ResetAll();
            this.RefreshSkinnedMeshRendererList();

            bool changed = false;
            XmlNode skinnedMeshesNode = xmlNode.FindChildNode("skinnedMeshes");
            Dictionary<XmlNode, SkinnedMeshRenderer> potentialChildrenNodes = new Dictionary<XmlNode, SkinnedMeshRenderer>();
            if (skinnedMeshesNode != null)
            {
                foreach (XmlNode node in skinnedMeshesNode.ChildNodes)
                {
                    Transform t = this._parent.transform.Find(node.Attributes["name"].Value);
                    if (t == null)
                        continue;
                    SkinnedMeshRenderer renderer = t.GetComponent<SkinnedMeshRenderer>();
                    if (renderer == null)
                        continue;
                    if (this._skinnedMeshRenderers.Contains(renderer) == false)
                    {
                        potentialChildrenNodes.Add(node, renderer);
                        continue;
                    }
                    if (this.LoadSingleSkinnedMeshRenderer(node, renderer))
                        changed = true;
                }
            }
            if (potentialChildrenNodes.Count > 0)
            {
                foreach (KeyValuePair<XmlNode, SkinnedMeshRenderer> pair in potentialChildrenNodes)
                {
                    PoseController childController = pair.Value.GetComponentInParent<PoseController>();
                    if (childController != this._parent)
                    {
                        childController.enabled = true;
                        if (childController._blendShapesEditor._secondPassLoadingNodes.ContainsKey(pair.Key) == false)
                            childController._blendShapesEditor._secondPassLoadingNodes.Add(pair.Key, pair.Value);
                    }
                }
            }

            this._parent.ExecuteDelayed(() =>
            {
                foreach (KeyValuePair<XmlNode, SkinnedMeshRenderer> pair in this._secondPassLoadingNodes)
                {
                    if (this._skinnedMeshRenderers.Contains(pair.Value) == false)
                        continue;
                    this.LoadSingleSkinnedMeshRenderer(pair.Key, pair.Value);
                }
                this._secondPassLoadingNodes.Clear();
            }, 2);
            return changed || this._secondPassLoadingNodes.Count > 0;
        }
        #endregion

        #region Private Methods
        private bool LoadSingleSkinnedMeshRenderer(XmlNode node, SkinnedMeshRenderer renderer)
        {
            bool loaded = false;
            SkinnedMeshRendererData data = new SkinnedMeshRendererData();
            foreach (XmlNode childNode in node.ChildNodes)
            {
                int index = XmlConvert.ToInt32(childNode.Attributes["index"].Value);
                if (index >= renderer.sharedMesh.blendShapeCount)
                    continue;
                loaded = true;
                BlendShapeData bsData = new BlendShapeData();
                bsData.originalWeight = renderer.GetBlendShapeWeight(index);
                bsData.weight = XmlConvert.ToSingle(childNode.Attributes["weight"].Value);
                data.dirtyBlendShapes.Add(index, bsData);
            }
            data.path = renderer.transform.GetPathFrom(this._parent.transform);
            this._dirtySkinnedMeshRenderers.Add(renderer, data);
            return loaded;
        }

        private void RefreshPresets()
        {
            if (Directory.Exists(_presetsPath))
            {
                _presets = Directory.GetFiles(_presetsPath, "*.xml");
                for (int i = 0; i < _presets.Length; i++)
                    _presets[i] = Path.GetFileNameWithoutExtension(_presets[i]);
            }
        }

        private void SaveLoadWindow(int id)
        {
            GUILayout.BeginVertical();

            this._presetsScroll = GUILayout.BeginScrollView(this._presetsScroll, false, true, GUILayout.ExpandHeight(true));
            foreach (string preset in _presets)
            {
                if (GUILayout.Button(preset))
                {
                    if (this._removePresetMode)
                        this.DeletePreset(preset + ".xml");
                    else
                        this.LoadPreset(preset + ".xml");
                }
            }
            GUILayout.EndScrollView();

            Color c = GUI.color;
            GUILayout.BeginVertical();
            if (_presets.Any(p => p.Equals(this._presetName, StringComparison.OrdinalIgnoreCase)))
                GUI.color = Color.red;
            GUILayout.BeginHorizontal();
            GUILayout.Label("Name", GUILayout.ExpandWidth(false));
            this._presetName = GUILayout.TextField(this._presetName);
            GUILayout.EndHorizontal();
            GUI.color = c;

            GUI.enabled = this._presetName.Length != 0;
            if (GUILayout.Button("Save"))
            {
                this._presetName = this._presetName.Trim();
                this._presetName = string.Join("_", this._presetName.Split(Path.GetInvalidFileNameChars()));
                if (this._presetName.Length != 0)
                {
                    this.SavePreset(this._presetName + ".xml");
                    this.RefreshPresets();
                    this._removePresetMode = false;
                }

            }
            GUI.enabled = true;
            if (this._removePresetMode)
                GUI.color = Color.red;
            GUI.enabled = _presets.Length != 0;
            if (GUILayout.Button(this._removePresetMode ? "Click on preset" : "Delete"))
                this._removePresetMode = !this._removePresetMode;
            GUI.enabled = true;
            GUI.color = c;

            if (GUILayout.Button("Close"))
                this._showSaveLoadWindow = false;

            GUILayout.EndVertical();

            GUILayout.EndVertical();
        }


        private void SavePreset(string name)
        {
            if (Directory.Exists(_presetsPath) == false)
                Directory.CreateDirectory(_presetsPath);
            using (XmlTextWriter writer = new XmlTextWriter(Path.Combine(_presetsPath, name), Encoding.UTF8))
            {
                writer.WriteStartElement("root");
                this.SaveXml(writer);
                writer.WriteEndElement();
            }
        }

        private void LoadPreset(string name)
        {
            string path = Path.Combine(_presetsPath, name);
            if (File.Exists(path) == false)
                return;
            XmlDocument doc = new XmlDocument();
            doc.Load(path);
            this.LoadXml(doc.FirstChild);
        }

        private void DeletePreset(string name)
        {
            File.Delete(Path.GetFullPath(Path.Combine(_presetsPath, name)));
            this._removePresetMode = false;
            this.RefreshPresets();
        }

        private void Init()
        {
            this._eyesShapesCount = Int32.MaxValue;
#if HONEYSELECT
            _instanceByFaceBlendShape.Add(this._target.ociChar.charBody.fbsCtrl, this);
            foreach (FBSTargetInfo target in this._target.ociChar.charBody.eyesCtrl.FBSTarget)
#elif KOIKATSU
            _instanceByFaceBlendShape.Add(this._target.ociChar.charInfo.fbsCtrl, this);
            foreach (FBSTargetInfo target in this._target.ociChar.charInfo.eyesCtrl.FBSTarget)
#endif
            {
                SkinnedMeshRenderer renderer = target.GetSkinnedMeshRenderer();
#if HONEYSELECT
                if (this._eyesSkinnedMeshRenderers.Contains(renderer) == false)
                    this._eyesSkinnedMeshRenderers.Add(renderer);
#endif
                if (renderer.sharedMesh.blendShapeCount < this._eyesShapesCount)
                    this._eyesShapesCount = renderer.sharedMesh.blendShapeCount;
                switch (renderer.name)
                {
                    case "cf_O_head":
                    case "cf_O_face":
                        this._eyesMouthRenderer = renderer;
                        break;
                }
#if HONEYSELECT
                if (!renderer.name.Equals("cf_O_mayuge") && !renderer.name.Equals("cm_O_mayuge"))
#endif
                {
                    SkinnedMeshRendererWrapper wrapper = new SkinnedMeshRendererWrapper
                    {
                        renderer = renderer,
                        links = new List<SkinnedMeshRendererWrapper>()
                    };
                    this._links.Add(renderer, wrapper);
                }

            }
#if HONEYSELECT
            if (this._target.type == GenericOCITarget.Type.Character && this._target.isFemale == false)
                this._eyesShapesCount = 14;

            foreach (FBSTargetInfo target in this._target.ociChar.charBody.mouthCtrl.FBSTarget)
            {
                if (this._mouthSkinnedMeshRenderers.Contains(target.GetSkinnedMeshRenderer()) == false)
                    this._mouthSkinnedMeshRenderers.Add(target.GetSkinnedMeshRenderer());
            }
#endif
            foreach (KeyValuePair<SkinnedMeshRenderer, SkinnedMeshRendererWrapper> pair in this._links)
            {
                foreach (KeyValuePair<SkinnedMeshRenderer, SkinnedMeshRendererWrapper> pair2 in this._links)
                {
                    if (pair.Key != pair2.Key)
                        pair.Value.links.Add(pair2.Value);
                }
            }
        }

        private void ResetAll()
        {
            foreach (SkinnedMeshRenderer renderer in this._skinnedMeshRenderers)
                this.SetMeshRendererNotDirty(renderer);
        }

        private void FaceBlendShapeOnPostLateUpdate()
        {
            if (this._parent.enabled)
                this.ApplyBlendShapeWeights();
        }

        private void ApplyBlendShapeWeights()
        {
            if (this._dirtySkinnedMeshRenderers.Count != 0)
                foreach (KeyValuePair<SkinnedMeshRenderer, SkinnedMeshRendererData> kvp in this._dirtySkinnedMeshRenderers)
                    foreach (KeyValuePair<int, BlendShapeData> weight in kvp.Value.dirtyBlendShapes)
                        kvp.Key.SetBlendShapeWeight(weight.Key, weight.Value.weight);
        }

        private void SetMeshRendererNotDirty(SkinnedMeshRenderer renderer)
        {
            if (this._dirtySkinnedMeshRenderers.ContainsKey(renderer))
            {
                SkinnedMeshRendererData data = this._dirtySkinnedMeshRenderers[renderer];
                foreach (KeyValuePair<int, BlendShapeData> kvp in data.dirtyBlendShapes)
                    renderer.SetBlendShapeWeight(kvp.Key, kvp.Value.originalWeight);
                this._dirtySkinnedMeshRenderers.Remove(renderer);
            }
        }

        private SkinnedMeshRendererData SetMeshRendererDirty(SkinnedMeshRenderer bone)
        {
            SkinnedMeshRendererData data;
            if (this._dirtySkinnedMeshRenderers.TryGetValue(bone, out data) == false)
            {
                data = new SkinnedMeshRendererData();
                data.path = bone.transform.GetPathFrom(this._parent.transform);
                this._dirtySkinnedMeshRenderers.Add(bone, data);
            }
            return data;
        }

        private void RefreshSkinnedMeshRendererList()
        {
            SkinnedMeshRenderer[] skinnedMeshRenderers = this._parent.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            List<SkinnedMeshRenderer> toDelete = null;
            foreach (SkinnedMeshRenderer r in this._skinnedMeshRenderers)
                if (skinnedMeshRenderers.Contains(r) == false)
                {
                    if (toDelete == null)
                        toDelete = new List<SkinnedMeshRenderer>();
                    toDelete.Add(r);
                }
            if (toDelete != null)
            {
                foreach (SkinnedMeshRenderer r in toDelete)
                {
                    SkinnedMeshRendererData data;
                    if (this._dirtySkinnedMeshRenderers.TryGetValue(r, out data))
                    {
                        this._headlessDirtySkinnedMeshRenderers.Add(data.path, data);
                        this._headlessReconstructionTimeout = 5;
                        this._dirtySkinnedMeshRenderers.Remove(r);
                    }
                    this._skinnedMeshRenderers.Remove(r);
                }
            }
            List<SkinnedMeshRenderer> toAdd = null;
            foreach (SkinnedMeshRenderer r in skinnedMeshRenderers)
                if (this._skinnedMeshRenderers.Contains(r) == false && this._parent._childObjects.All(child => r.transform.IsChildOf(child.transform) == false))
                {
                    if (toAdd == null)
                        toAdd = new List<SkinnedMeshRenderer>();
                    toAdd.Add(r);
                }
            if (toAdd != null)
            {
                foreach (SkinnedMeshRenderer r in toAdd)
                    this._skinnedMeshRenderers.Add(r);
            }
            if (this._skinnedMeshRenderers.Count != 0 && this._skinnedMeshTarget != null)
                this._skinnedMeshTarget = this._skinnedMeshRenderers.FirstOrDefault(s => s.sharedMesh.blendShapeCount > 0);
        }
        #endregion
    }
}