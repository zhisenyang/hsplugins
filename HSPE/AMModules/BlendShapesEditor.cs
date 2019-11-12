﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml;
#if HONEYSELECT || PLAYHOME
using Harmony;
#else
using HarmonyLib;
#endif
using Studio;
using ToolBox;
using ToolBox.Extensions;
using UnityEngine;

namespace HSPE.AMModules
{
    public class BlendShapesEditor : AdvancedModeModule
    {
        #region Constants
        private static readonly Dictionary<string, string> _skinnedMeshAliases = new Dictionary<string, string>
        {
#if HONEYSELECT || PLAYHOME
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
#elif KOIKATSU
            {"cf_O_face",  "Eyes / Mouth"},
            {"cf_O_tooth",  "Teeth"},
            {"cf_O_canine",  "Canines"},
            {"cf_O_eyeline",  "Upper Eyelashes"},
            {"cf_O_eyeline_low",  "Lower Eyelashes"},
            {"cf_O_mayuge",  "Eyebrows"},
            {"cf_Ohitomi_L",  "Left Eye White"},
            {"cf_Ohitomi_R",  "Right Eye White"},
            {"o_tang",  "Tongue"},
            {"cf_O_namida_L",  "Tears L"},
            {"cf_O_namida_M",  "Tears M"},
            {"cf_O_namida_S",  "Tears S"},
#elif AISHOUJO
                {"o_eyelashes",  "Eyelashes"},
                {"o_head",  "Head"},
                {"o_namida",  "Tears"},
                {"o_tang",  "Tongue"},
                {"o_tooth",  "Teeth"},
#endif
        };
        private static readonly string _presetsPath;
        #endregion

        #region Statics
        private static Dictionary<object, BlendShapesEditor> _instanceByFaceBlendShape = new Dictionary<object, BlendShapesEditor>();
        private static string[] _presets = new string[0];
        internal static readonly Dictionary<string, string> _blendShapeAliases = new Dictionary<string, string>();
        private static readonly Dictionary<int, string> _femaleSeparators = new Dictionary<int, string>();
        private static readonly Dictionary<int, string> _maleSeparators = new Dictionary<int, string>();
        private static readonly int _femaleEyesComponentsCount;
        private static readonly int _maleEyesComponentsCount;
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
                    this.dirtyBlendShapes.Add(kvp.Key, new BlendShapeData() { weight = kvp.Value.weight, originalWeight = kvp.Value.originalWeight });
                }
            }
        }

        private class BlendShapeData
        {
            public float weight;
            public float originalWeight;
        }

#if HONEYSELECT || KOIKATSU
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
#elif PLAYHOME
        [HarmonyPatch(typeof(Human), "LateUpdate")]
        private class Human_Patches
        {
            [HarmonyAfter("com.joan6694.hsplugins.instrumentation")]
            public static void Postfix(Human __instance)
            {
                BlendShapesEditor editor;
                if (_instanceByFaceBlendShape.TryGetValue(__instance, out editor))
                    editor.FaceBlendShapeOnPostLateUpdate();
            }
        }
#elif AISHOUJO
        [HarmonyPatch(typeof(FaceBlendShape), "OnLateUpdate")]
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
#endif

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
        private SkinnedMeshRenderer _headRenderer;
        private SkinnedMeshRenderer _skinnedMeshTarget;
        private bool _linkEyesComponents = true;
        private readonly Dictionary<SkinnedMeshRenderer, SkinnedMeshRendererWrapper> _links = new Dictionary<SkinnedMeshRenderer, SkinnedMeshRendererWrapper>();
        private string _search = "";
        private readonly GenericOCITarget _target;
        private readonly Dictionary<XmlNode, SkinnedMeshRenderer> _secondPassLoadingNodes = new Dictionary<XmlNode, SkinnedMeshRenderer>();
        private bool _showSaveLoadWindow = false;
        private Vector2 _presetsScroll;
        private string _presetName = "";
        private bool _removePresetMode;
        private int _renameIndex = -1;
        private string _renameString = "";
        private int _lastEditedBlendShape = -1;
        #endregion

        #region Public Fields
        public override AdvancedModeModuleType type { get { return AdvancedModeModuleType.BlendShapes; } }
        public override string displayName { get { return "Blend Shapes"; } }
        public override bool shouldDisplay { get { return this._skinnedMeshRenderers.Any(r => r != null && r.sharedMesh != null && r.sharedMesh.blendShapeCount > 0); } }
        #endregion

        #region Unity Methods
        static BlendShapesEditor()
        {
#if HONEYSELECT || PLAYHOME
            _femaleSeparators.Add(0, "Eyes");
            _femaleSeparators.Add(58, "Mouth");
            _maleSeparators.Add(0, "Eyes");
            _maleSeparators.Add(14, "Mouth");
            _femaleEyesComponentsCount = 58;
            _maleEyesComponentsCount = 14;
#elif KOIKATSU
            _femaleSeparators.Add(0, "Eyes");
            _femaleSeparators.Add(28, "Mouth");
            _maleSeparators = _femaleSeparators;
            _femaleEyesComponentsCount = 28;
            _maleEyesComponentsCount = _femaleEyesComponentsCount;
#elif AISHOUJO
            _femaleSeparators.Add(0, "Eyes");
            _femaleSeparators.Add(16, "Eyebrows");
            _femaleSeparators.Add(26, "Mouth");
            _maleSeparators = _femaleSeparators;
            _femaleEyesComponentsCount = 16;
            _maleEyesComponentsCount = _femaleEyesComponentsCount;
#endif
            _presetsPath = Path.Combine(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), HSPE._name), "BlendShapesPresets");
        }

        public BlendShapesEditor(PoseController parent, GenericOCITarget target) : base(parent)
        {
            this._parent.onLateUpdate += this.LateUpdate;
            this._parent.onDisable += this.OnDisable;
            this._target = target;
            MainWindow._self.ExecuteDelayed(() =>
            {
                this.RefreshSkinnedMeshRendererList();
                if (this._target.type == GenericOCITarget.Type.Character)
                    this.Init();
            });
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
            Color c = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.6f, 0.6f, 0.6f, 0.2f);
            Rect windowRect = Rect.MinMaxRect(MainWindow._self._advancedModeRect.xMin - 180, MainWindow._self._advancedModeRect.yMin, MainWindow._self._advancedModeRect.xMin, MainWindow._self._advancedModeRect.yMax);
            for (int i = 0; i < 3; ++i)
                GUI.Box(windowRect, "", MainWindow._customBoxStyle);
            GUI.backgroundColor = c;
            GUILayout.Window(3, windowRect, this.SaveLoadWindow, "Presets");
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
            _instanceByFaceBlendShape = new Dictionary<object, BlendShapesEditor>(_instanceByFaceBlendShape.Where(e => e.Key != null).ToDictionary(e => e.Key, e => e.Value));
        }
        #endregion

        #region Public Methods
        public override void OnCharacterReplaced()
        {
            Dictionary<object, BlendShapesEditor> newInstanceByFBS = null;
            foreach (KeyValuePair<object, BlendShapesEditor> pair in _instanceByFaceBlendShape)
            {
                if (pair.Key == null)
                {
                    newInstanceByFBS = new Dictionary<object, BlendShapesEditor>();
                    break;
                }
            }
            if (newInstanceByFBS != null)
            {
                foreach (KeyValuePair<object, BlendShapesEditor> pair in _instanceByFaceBlendShape)
                {
                    if (pair.Key != null)
                        newInstanceByFBS.Add(pair.Key, pair.Value);
                }
                _instanceByFaceBlendShape = newInstanceByFBS;
            }
            this._links.Clear();

            this.RefreshSkinnedMeshRendererList();
            MainWindow._self.ExecuteDelayed(() =>
            {
                this.RefreshSkinnedMeshRendererList();
                this.Init();
            });
        }

        public override void OnLoadClothesFile()
        {
            this.RefreshSkinnedMeshRendererList();
            MainWindow._self.ExecuteDelayed(this.RefreshSkinnedMeshRendererList);
        }
#if HONEYSELECT || KOIKATSU
#if HONEYSELECT
        public override void OnCoordinateReplaced(CharDefine.CoordinateType coordinateType, bool force)
#elif KOIKATSU
        public override void OnCoordinateReplaced(ChaFileDefine.CoordinateType coordinateType, bool force)
#endif
        {
            this.RefreshSkinnedMeshRendererList();
            MainWindow._self.ExecuteDelayed(this.RefreshSkinnedMeshRendererList);
        }
#endif

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
                {
                    this._skinnedMeshTarget = r;
                    this._lastEditedBlendShape = -1;
                }
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
                this._linkEyesComponents = GUILayout.Toggle(this._linkEyesComponents, "Link eyes components");
            if (GUILayout.Button("Force refresh list"))
                this.RefreshSkinnedMeshRendererList();
            GUI.color = Color.red;
            if (GUILayout.Button("Reset all"))
                this.ResetAll();
            GUI.color = c;
            GUILayout.EndVertical();


            if (this._skinnedMeshTarget != null)
            {
                GUILayout.BeginVertical(GUI.skin.box);
                GUILayout.BeginHorizontal();
                GUILayout.Label("Search", GUILayout.ExpandWidth(false));
                this._search = GUILayout.TextField(this._search, GUILayout.ExpandWidth(true));
                if (GUILayout.Button("X", GUILayout.ExpandWidth(false)))
                    this._search = "";
                GUILayout.EndHorizontal();

                this._blendShapesScroll = GUILayout.BeginScrollView(this._blendShapesScroll, false, true, GUILayout.ExpandWidth(false));

                SkinnedMeshRendererData data = null;
                this._dirtySkinnedMeshRenderers.TryGetValue(this._skinnedMeshTarget, out data);
                bool zeroResult = true;
                for (int i = 0; i < this._skinnedMeshTarget.sharedMesh.blendShapeCount; ++i)
                {
                    if (this._headRenderer == this._skinnedMeshTarget)
                    {
                        Dictionary<int, string> separatorDict = this._target.isFemale ? _femaleSeparators : _maleSeparators;
                        string s;
                        if (separatorDict.TryGetValue(i, out s))
                            GUILayout.Label(s, GUI.skin.box);
                    }
                    string blendShapeName = this._skinnedMeshTarget.sharedMesh.GetBlendShapeName(i);
                    string blendShapeAlias;
                    if (_blendShapeAliases.TryGetValue(blendShapeName, out blendShapeAlias) == false)
                        blendShapeAlias = null;
                    if ((blendShapeAlias != null && blendShapeAlias.IndexOf(this._search, StringComparison.CurrentCultureIgnoreCase) != -1) ||
                        blendShapeName.IndexOf(this._search, StringComparison.CurrentCultureIgnoreCase) != -1)
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
                        if (this._renameIndex != i)
                        {
                            GUILayout.Label($"{i} {(blendShapeAlias == null ? blendShapeName : blendShapeAlias)}");
                            GUILayout.FlexibleSpace();
                        }
                        else
                        {
                            GUILayout.Label(i.ToString(), GUILayout.ExpandWidth(false));
                            this._renameString = GUILayout.TextField(this._renameString, GUILayout.ExpandWidth(true));
                        }
                        if (GUILayout.Button(this._renameIndex != i ? "Rename" : "Save", GUILayout.ExpandWidth(false)))
                        {
                            if (this._renameIndex != i)
                            {
                                this._renameIndex = i;
                                this._renameString = blendShapeAlias == null ? blendShapeName : blendShapeAlias;
                            }
                            else
                            {
                                this._renameIndex = -1;
                                this._renameString = this._renameString.Trim();
                                if (this._renameString == string.Empty || this._renameString == blendShapeName)
                                {
                                    if (_blendShapeAliases.ContainsKey(blendShapeName))
                                        _blendShapeAliases.Remove(blendShapeName);
                                }
                                else
                                {
                                    if (_blendShapeAliases.ContainsKey(blendShapeName) == false)
                                        _blendShapeAliases.Add(blendShapeName, this._renameString);
                                    else
                                        _blendShapeAliases[blendShapeName] = this._renameString;
                                }
                            }
                        }
                        GUILayout.Label(blendShapeWeight.ToString("000"), GUILayout.ExpandWidth(false));
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        float newBlendShapeWeight = GUILayout.HorizontalSlider(blendShapeWeight, 0f, 100f);
                        if (GUILayout.Button("-1", GUILayout.ExpandWidth(false)))
                            newBlendShapeWeight -= 1;
                        if (GUILayout.Button("+1", GUILayout.ExpandWidth(false)))
                            newBlendShapeWeight += 1;
                        newBlendShapeWeight = Mathf.Clamp(newBlendShapeWeight, 0, 100);
                        GUILayout.EndHorizontal();
                        if (Mathf.Approximately(newBlendShapeWeight, blendShapeWeight) == false)
                        {
                            this._lastEditedBlendShape = i;
                            this.SetBlendShapeWeight(this._skinnedMeshTarget, i, newBlendShapeWeight);
                            if (this._linkEyesComponents && i < (this._target.isFemale ? _femaleEyesComponentsCount : _maleEyesComponentsCount))
                            {
                                SkinnedMeshRendererWrapper wrapper;
                                if (this._links.TryGetValue(this._skinnedMeshTarget, out wrapper))
                                {
                                    foreach (SkinnedMeshRendererWrapper link in wrapper.links)
                                    {
                                        if (i < link.renderer.sharedMesh.blendShapeCount)
                                            this.SetBlendShapeWeight(link.renderer, i, newBlendShapeWeight);
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

                            if (this._linkEyesComponents && i < (this._target.isFemale ? _femaleEyesComponentsCount : _maleEyesComponentsCount))
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
                    if (this._linkEyesComponents)
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

        private BlendShapeData SetBlendShapeWeight(SkinnedMeshRenderer renderer, int index, float weight)
        {
            BlendShapeData bsData = this.SetBlendShapeDirty(renderer, index);
            bsData.weight = weight;
            return bsData;
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
                if (this._target.type == GenericOCITarget.Type.Character)
                {
                    xmlWriter.WriteAttributeString("eyesPtn", XmlConvert.ToString(this._target.ociChar.charInfo.GetEyesPtn()));
#if HONEYSELECT || KOIKATSU || AISHOUJO
                    xmlWriter.WriteAttributeString("eyesOpen", XmlConvert.ToString(this._target.ociChar.charInfo.GetEyesOpenMax()));
#elif PLAYHOME
                    xmlWriter.WriteAttributeString("eyesOpen", XmlConvert.ToString(this._target.ociChar.charInfo.fileStatus.eyesOpenMax));
#endif
                    xmlWriter.WriteAttributeString("mouthPtn", XmlConvert.ToString(this._target.ociChar.charInfo.GetMouthPtn()));
                    xmlWriter.WriteAttributeString("mouthOpen", XmlConvert.ToString(this._target.ociChar.oiCharInfo.mouthOpen));
#if KOIKATSU || AISHOUJO
                    xmlWriter.WriteAttributeString("eyebrowsPtn", XmlConvert.ToString(this._target.ociChar.charInfo.GetEyebrowPtn()));
                    xmlWriter.WriteAttributeString("eyebrowsOpen", XmlConvert.ToString(this._target.ociChar.charInfo.GetEyebrowOpenMax()));
#endif
                    ++written;
                }
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
                if (this._target.type == GenericOCITarget.Type.Character)
                {
                    if (skinnedMeshesNode.Attributes["eyesPtn"] != null)
                    {
#if HONEYSELECT || KOIKATSU || AISHOUJO
                        this._target.ociChar.charInfo.ChangeEyesPtn(XmlConvert.ToInt32(skinnedMeshesNode.Attributes["eyesPtn"].Value), false);
#elif PLAYHOME
                        this._target.ociChar.charInfo.ChangeEyesPtn(XmlConvert.ToInt32(skinnedMeshesNode.Attributes["eyesPtn"].Value));
#endif
                    }
                    if (skinnedMeshesNode.Attributes["eyesOpen"] != null)
                        this._target.ociChar.ChangeEyesOpen(XmlConvert.ToSingle(skinnedMeshesNode.Attributes["eyesOpen"].Value));
                    if (skinnedMeshesNode.Attributes["mouthPtn"] != null)
                    {
#if HONEYSELECT || KOIKATSU || AISHOUJO
                        this._target.ociChar.charInfo.ChangeMouthPtn(XmlConvert.ToInt32(skinnedMeshesNode.Attributes["mouthPtn"].Value), false);
#elif PLAYHOME
                        this._target.ociChar.charInfo.ChangeMouthPtn(XmlConvert.ToInt32(skinnedMeshesNode.Attributes["mouthPtn"].Value));
#endif
                    }
                    if (skinnedMeshesNode.Attributes["mouthOpen"] != null)
                        this._target.ociChar.ChangeMouthOpen(XmlConvert.ToSingle(skinnedMeshesNode.Attributes["mouthOpen"].Value));
#if KOIKATSU || AISHOUJO
                    if (skinnedMeshesNode.Attributes["eyebrowsPtn"] != null)
                        this._target.ociChar.charInfo.ChangeEyebrowPtn(XmlConvert.ToInt32(skinnedMeshesNode.Attributes["eyebrowsPtn"].Value), false);
                    if (skinnedMeshesNode.Attributes["eyebrowsOpen"] != null)
                        this._target.ociChar.charInfo.ChangeEyebrowOpenMax(XmlConvert.ToSingle(skinnedMeshesNode.Attributes["eyebrowsOpen"].Value));
#endif

                }
                foreach (XmlNode node in skinnedMeshesNode.ChildNodes)
                {
                    try
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
                    catch (Exception e)
                    {
                        UnityEngine.Debug.LogError("HSPE: Couldn't load blendshape for object " + this._parent.name + " " + node.OuterXml + "\n" + e);
                    }
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
                    try
                    {
                        if (this._skinnedMeshRenderers.Contains(pair.Value) == false)
                            continue;
                        this.LoadSingleSkinnedMeshRenderer(pair.Key, pair.Value);

                    }
                    catch (Exception e)
                    {
                        UnityEngine.Debug.LogError("HSPE: Couldn't load blendshape for object " + this._parent.name + " " + pair.Key.OuterXml + "\n" + e);
                    }
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
                BlendShapeData bsData = this.SetBlendShapeDirty(data, index);
                bsData.originalWeight = renderer.GetBlendShapeWeight(index);
                bsData.weight = XmlConvert.ToSingle(childNode.Attributes["weight"].Value);
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
            this._headRenderer = null;
#if HONEYSELECT
            _instanceByFaceBlendShape.Add(this._target.ociChar.charBody.fbsCtrl, this);
#elif PLAYHOME
            _instanceByFaceBlendShape.Add(this._target.ociChar.charInfo.human, this);
#elif KOIKATSU
            _instanceByFaceBlendShape.Add(this._target.ociChar.charInfo.fbsCtrl, this);
#elif AISHOUJO
            _instanceByFaceBlendShape.Add(this._target.ociChar.charInfo.fbsCtrl, this);
#endif
            foreach (SkinnedMeshRenderer renderer in this._skinnedMeshRenderers)
            {
                switch (renderer.name)
                {
#if HONEYSELECT || KOIKATSU || PLAYHOME
                    case "cf_O_head":
                    case "cf_O_face":
#elif AISHOUJO
                    case "o_head":
#endif
                        this._headRenderer = renderer;
                        break;
                }

                switch (renderer.name)
                {
#if HONEYSELECT || PLAYHOME
                    case "cf_O_head":
                    case "cf_O_matuge":
                    case "cf_O_namida01":
                    case "cf_O_namida02":
#elif KOIKATSU
                    case "cf_O_face":
                    case "cf_O_eyeline":
                    case "cf_O_eyeline_low":
                    case "cf_Ohitomi_L":
                    case "cf_Ohitomi_R":
                    case "cf_O_namida_L":
                    case "cf_O_namida_M":
                    case "cf_O_namida_S":
#elif AISHOUJO
                    case "o_eyelashes":
                    case "o_namida":
                    case "o_head":
#endif
                        SkinnedMeshRendererWrapper wrapper = new SkinnedMeshRendererWrapper
                        {
                            renderer = renderer,
                            links = new List<SkinnedMeshRendererWrapper>()
                        };
                        this._links.Add(renderer, wrapper);

                        break;
                }
            }

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

        private SkinnedMeshRendererData SetMeshRendererDirty(SkinnedMeshRenderer renderer)
        {
            SkinnedMeshRendererData data;
            if (this._dirtySkinnedMeshRenderers.TryGetValue(renderer, out data) == false)
            {
                data = new SkinnedMeshRendererData();
                data.path = renderer.transform.GetPathFrom(this._parent.transform);
                this._dirtySkinnedMeshRenderers.Add(renderer, data);
            }
            return data;
        }

        private BlendShapeData SetBlendShapeDirty(SkinnedMeshRenderer renderer, int index)
        {
            SkinnedMeshRendererData data = this.SetMeshRendererDirty(renderer);
            BlendShapeData bsData;
            if (data.dirtyBlendShapes.TryGetValue(index, out bsData) == false)
            {
                bsData = new BlendShapeData();
                bsData.originalWeight = renderer.GetBlendShapeWeight(index);
                data.dirtyBlendShapes.Add(index, bsData);
            }
            return bsData;
        }

        private BlendShapeData SetBlendShapeDirty(SkinnedMeshRendererData data, int index)
        {
            BlendShapeData bsData;
            if (data.dirtyBlendShapes.TryGetValue(index, out bsData) == false)
            {
                bsData = new BlendShapeData();
                data.dirtyBlendShapes.Add(index, bsData);
            }
            return bsData;
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

        #region Timeline Compatibility
        internal static class TimelineCompatibility
        {
            public static void Populate()
            {
                ToolBox.TimelineCompatibility.AddInterpolableModelDynamic(
                        owner: HSPE._name,
                        id: "lastBlendShape",
                        name: "BlendShape (Last Modified)",
                        interpolateBefore: (oci, parameter, leftValue, rightValue, factor) =>
                        {
                            HashedPair<BlendShapesEditor, HashedPair<SkinnedMeshRenderer, int>> pair = ((HashedPair<BlendShapesEditor, HashedPair<SkinnedMeshRenderer, int>>)parameter);
                            pair.key.SetBlendShapeWeight(pair.value.key, pair.value.value, Mathf.LerpUnclamped((float)leftValue, (float)rightValue, factor));
                        },
                        interpolateAfter: null,
                        isCompatibleWithTarget: oci => oci != null && oci.guideObject.transformTarget.GetComponent<PoseController>() != null,
                        getValue: (oci, parameter) =>
                        {
                            HashedPair<BlendShapesEditor, HashedPair<SkinnedMeshRenderer, int>> pair = (HashedPair<BlendShapesEditor, HashedPair<SkinnedMeshRenderer, int>>)parameter;
                            return pair.value.key.GetBlendShapeWeight(pair.value.value);
                        },
                        readValueFromXml: node => node.ReadFloat("value"),
                        writeValueToXml: (writer, o) => writer.WriteValue("value", (float)o),
                        getParameter: oci =>
                        {
                            PoseController controller = oci.guideObject.transformTarget.GetComponent<PoseController>();
                            return new HashedPair<BlendShapesEditor, HashedPair<SkinnedMeshRenderer, int>>(controller._blendShapesEditor, new HashedPair<SkinnedMeshRenderer, int>(controller._blendShapesEditor._skinnedMeshTarget, controller._blendShapesEditor._lastEditedBlendShape));
                        },
                        readParameterFromXml: (oci, node) =>
                        {
                            SkinnedMeshRenderer renderer = oci.guideObject.transformTarget.Find(node.Attributes["parameter1"].Value).GetComponent<SkinnedMeshRenderer>();
                            int index = node.ReadInt("parameter2");
                            return new HashedPair<BlendShapesEditor, HashedPair<SkinnedMeshRenderer, int>>(oci.guideObject.transformTarget.GetComponent<PoseController>()._blendShapesEditor, new HashedPair<SkinnedMeshRenderer, int>(renderer, index));
                        },
                        writeParameterToXml: (oci, writer, o) =>
                        {
                            HashedPair<BlendShapesEditor, HashedPair<SkinnedMeshRenderer, int>> pair = (HashedPair<BlendShapesEditor, HashedPair<SkinnedMeshRenderer, int>>)o;
                            writer.WriteAttributeString("parameter1", pair.value.key.transform.GetPathFrom(oci.guideObject.transformTarget));
                            writer.WriteValue("parameter2", pair.value.value);
                        },
                        checkIntegrity: (oci, parameter) =>
                        {
                            if (parameter == null)
                                return false;
                            HashedPair<BlendShapesEditor, HashedPair<SkinnedMeshRenderer, int>> pair = (HashedPair<BlendShapesEditor, HashedPair<SkinnedMeshRenderer, int>>)parameter;
                            if (pair.key == null || pair.value.key == null || pair.value.value >= pair.value.key.sharedMesh.blendShapeCount)
                                return false;
                            return true;
                        },
                        getFinalName: (name, oci, parameter) =>
                        {
                            HashedPair<BlendShapesEditor, HashedPair<SkinnedMeshRenderer, int>> pair = (HashedPair<BlendShapesEditor, HashedPair<SkinnedMeshRenderer, int>>)parameter;
                            string skinnedMeshName;
                            if (_skinnedMeshAliases.TryGetValue(pair.value.key.name, out skinnedMeshName) == false)
                                skinnedMeshName = pair.value.key.name;
                            return $"BS ({skinnedMeshName} {pair.value.value})";
                        });

                ToolBox.TimelineCompatibility.AddInterpolableModelDynamic(
                        owner: HSPE._name,
                        id: "groupBlendShape",
                        name: "BlendShape (Group)",
                        interpolateBefore: (oci, parameter, leftValue, rightValue, factor) =>
                        {
                            HashedPair<BlendShapesEditor, SkinnedMeshRenderer> pair = (HashedPair<BlendShapesEditor, SkinnedMeshRenderer>)parameter;
                            float[] left = (float[])leftValue;
                            float[] right = (float[])rightValue;
                            for (int i = 0; i < left.Length; i++)
                                pair.key.SetBlendShapeWeight(pair.value, i, Mathf.LerpUnclamped(left[i], right[i], factor));
                        },
                        interpolateAfter: null,
                        isCompatibleWithTarget: oci => oci != null && oci.guideObject.transformTarget.GetComponent<PoseController>() != null,
                        getValue: (oci, parameter) =>
                        {
                            HashedPair<BlendShapesEditor, SkinnedMeshRenderer> pair = (HashedPair<BlendShapesEditor, SkinnedMeshRenderer>)parameter;
                            float[] value = new float[pair.value.sharedMesh.blendShapeCount];
                            for (int i = 0; i < value.Length; ++i)
                                value[i] = pair.value.GetBlendShapeWeight(i);
                            return value;
                        },
                        readValueFromXml: node =>
                        {
                            float[] value = new float[node.ReadInt("valueCount")];
                            for (int i = 0; i < value.Length; i++)
                                value[i] = node.ReadFloat($"value{i}");
                            return value;
                        },
                        writeValueToXml: (writer, o) =>
                        {
                            float[] value = (float[])o;
                            writer.WriteValue("valueCount", value.Length);
                            for (int i = 0; i < value.Length; i++)
                                writer.WriteValue($"value{i}", value[i]);
                        },
                        getParameter: oci =>
                        {
                            PoseController controller = oci.guideObject.transformTarget.GetComponent<PoseController>();
                            return new HashedPair<BlendShapesEditor, SkinnedMeshRenderer>(controller._blendShapesEditor, controller._blendShapesEditor._skinnedMeshTarget);
                        },
                        readParameterFromXml: (oci, node) => new HashedPair<BlendShapesEditor, SkinnedMeshRenderer>(oci.guideObject.transformTarget.GetComponent<PoseController>()._blendShapesEditor, oci.guideObject.transformTarget.Find(node.Attributes["parameter"].Value).GetComponent<SkinnedMeshRenderer>()),
                        writeParameterToXml: (oci, writer, o) => writer.WriteAttributeString("parameter", ((HashedPair<BlendShapesEditor, SkinnedMeshRenderer>)o).value.transform.GetPathFrom(oci.guideObject.transformTarget)),
                        checkIntegrity: (oci, parameter) =>
                        {
                            if (parameter == null)
                                return false;
                            HashedPair<BlendShapesEditor, SkinnedMeshRenderer> pair = (HashedPair<BlendShapesEditor, SkinnedMeshRenderer>)parameter;
                            if (pair.key == null || pair.value == null)
                                return false;
                            return true;
                        },
                        getFinalName: (name, oci, parameter) =>
                        {
                            HashedPair<BlendShapesEditor, SkinnedMeshRenderer> pair = (HashedPair<BlendShapesEditor, SkinnedMeshRenderer>)parameter;
                            string skinnedMeshName;
                            if (_skinnedMeshAliases.TryGetValue(pair.value.name, out skinnedMeshName) == false)
                                skinnedMeshName = pair.value.name;
                            return $"BS ({skinnedMeshName})";
                        });

                //TODO maybe do that, or maybe not, idk
                //ToolBox.TimelineCompatibility.AddInterpolableModelDynamic(
                //        owner: HSPE._name,
                //        id: "everythingBlendShape",
                //        name: "BlendShape (Everything)",
                //        interpolateBefore: (oci, parameter, leftValue, rightValue, factor) =>
                //        {
                //            HashedPair<BlendShapesEditor, SkinnedMeshRenderer> pair = (HashedPair<BlendShapesEditor, SkinnedMeshRenderer>)parameter;
                //            float[] left = (float[])leftValue;
                //            float[] right = (float[])rightValue;
                //            for (int i = 0; i < left.Length; i++)
                //                pair.key.SetBlendShapeWeight(pair.value, i, Mathf.LerpUnclamped(left[i], right[i], factor));
                //        },
                //        interpolateAfter: null,
                //        isCompatibleWithTarget: oci => oci != null && oci.guideObject.transformTarget.GetComponent<PoseController>() != null,
                //        getValue: (oci, parameter) =>
                //        {
                //            HashedPair<BlendShapesEditor, SkinnedMeshRenderer> pair = (HashedPair<BlendShapesEditor, SkinnedMeshRenderer>)parameter;
                //            float[] value = new float[pair.value.sharedMesh.blendShapeCount];
                //            for (int i = 0; i < value.Length; ++i)
                //                value[i] = pair.value.GetBlendShapeWeight(i);
                //            return value;
                //        },
                //        readValueFromXml: node =>
                //        {
                //            float[] value = new float[node.ReadInt("valueCount")];
                //            for (int i = 0; i < value.Length; i++)
                //                value[i] = node.ReadFloat($"value{i}");
                //            return value;
                //        },
                //        writeValueToXml: (writer, o) =>
                //        {
                //            float[] value = (float[])o;
                //            writer.WriteValue("valueCount", value.Length);
                //            for (int i = 0; i < value.Length; i++)
                //                writer.WriteValue($"value{i}", value[i]);
                //        },
                //        getParameter: oci => oci.guideObject.transformTarget.GetComponent<PoseController>()._blendShapesEditor,
                //        checkIntegrity: (oci, parameter) => parameter != null,
                //        getFinalName: (name, oci, parameter) => "BS (Everything)");
            }
        }
        #endregion
    }
}