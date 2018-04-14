using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Studio;
using UnityEngine;
using UnityEngine.Rendering;

namespace RendererEditor
{
    public class MainWindow : MonoBehaviour
    {
        #region Private Types
        private class RendererData
        {
            public class MaterialData
            {
                public int renderQueue;
            }

            public ShadowCastingMode shadowCastingMode;
            public bool receiveShadow;
            public Dictionary<Material, MaterialData> dirtyMaterials = new Dictionary<Material, MaterialData>();
        }
        #endregion

        #region Private Variables
        private const float _width = 500;
        private const float _height = 300;
        private Rect _windowRect = new Rect((Screen.width - _width) / 2f, (Screen.height - _height) / 2f, _width, _height);
        private int _randomId;
        private bool _enabled;
        private bool _mouseInWindow;
        private Renderer _selectedRenderer;
        private Vector2 _rendererScroll;
        private ShadowCastingMode[] _shadowCastingModes;
        private Vector2 _materialsScroll;
        private Material _selectedMaterial;
        private TreeNodeObject _lastSelectedNode;
        private Renderer _lastSelectedRenderer;
        private Dictionary<Renderer, RendererData> _dirtyRenderers = new Dictionary<Renderer, RendererData>();
        #endregion

        #region Unity Methods
        void Start()
        {
            HSExtSave.HSExtSave.RegisterHandler("rendererEditor", null, null, this.OnSceneLoad, null, this.OnSceneSave, null, null);
            this._randomId = (int)(UnityEngine.Random.value * UInt32.MaxValue);
            this._shadowCastingModes = (ShadowCastingMode[])Enum.GetValues(typeof(ShadowCastingMode));
        }

        void Update()
        {
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.R))
                this._enabled = !this._enabled;
            if (Studio.Studio.Instance.treeNodeCtrl.selectNode != this._lastSelectedNode)
                this._selectedRenderer = null;
            if (this._lastSelectedRenderer != this._selectedRenderer)
            {
                this._selectedMaterial = null;
            }
            this._lastSelectedNode = Studio.Studio.Instance.treeNodeCtrl.selectNode;
            this._lastSelectedRenderer = this._selectedRenderer;

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
            }
        }

        void OnGUI()
        {
            if (!this._enabled || Studio.Studio.Instance.treeNodeCtrl.selectNode == null)
                return;
            this._windowRect = GUILayout.Window(this._randomId, this._windowRect, this.WindowFunction, "Renderer Editor");
            this._mouseInWindow = this._windowRect.Contains(Event.current.mousePosition);
            if (this._mouseInWindow)
                Studio.Studio.Instance.cameraCtrl.noCtrlCondition = () => this._mouseInWindow && this._enabled && Studio.Studio.Instance.treeNodeCtrl.selectNode != null;
        }
        #endregion

        #region Private Methods
        private void WindowFunction(int id)
        {
            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical();

            GUI.enabled = this._selectedRenderer != null;

            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Cast Shadows");
                foreach (ShadowCastingMode mode in this._shadowCastingModes)
                {
                    if (GUILayout.Toggle(this._selectedRenderer != null && this._selectedRenderer.shadowCastingMode == mode, mode.ToString()) && this._selectedRenderer != null)
                    {
                        if (mode != this._selectedRenderer.shadowCastingMode)
                        {
                            RendererData data;
                            if (this.SetRendererDirty(this._selectedRenderer, out data))
                                data.shadowCastingMode = this._selectedRenderer.shadowCastingMode;
                            this._selectedRenderer.shadowCastingMode = mode;
                        }
                    }
                }
                GUILayout.EndHorizontal();
            }

            {
                bool newReceiveShadows = GUILayout.Toggle(this._selectedRenderer != null && this._selectedRenderer.receiveShadows, "Receive Shadows");
                if (this._selectedRenderer != null && newReceiveShadows != this._selectedRenderer.receiveShadows)
                {
                    RendererData data;
                    if (this.SetRendererDirty(this._selectedRenderer, out data))
                        data.receiveShadow = this._selectedRenderer.receiveShadows;
                    this._selectedRenderer.receiveShadows = newReceiveShadows;
                }
            }

            {
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Reset") && this._selectedRenderer != null)
                    this.ResetRenderer(this._selectedRenderer);
                if (GUILayout.Button("Reset w/ materials") && this._selectedRenderer != null)
                    this.ResetRenderer(this._selectedRenderer, true);
                GUILayout.EndHorizontal();
            }

            GUILayout.BeginVertical("Materials", GUI.skin.window);
            this._materialsScroll = GUILayout.BeginScrollView(this._materialsScroll);
            if (this._selectedRenderer != null)
            {
                foreach (Material material in this._selectedRenderer.sharedMaterials)
                {
                    Color c = GUI.color;
                    bool isMaterialDirty = this._dirtyRenderers.TryGetValue(this._selectedRenderer, out RendererData rendererData) && rendererData.dirtyMaterials.ContainsKey(material);
                    if (material == this._selectedMaterial)
                        GUI.color = Color.cyan;
                    else if (isMaterialDirty)
                        GUI.color = Color.magenta;
                    if (GUILayout.Button(material.name + (isMaterialDirty? "*": "")))
                        this._selectedMaterial = material;
                    GUI.color = c;
                }
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();

            GUI.enabled = true;

            {
                GUI.enabled = this._selectedMaterial != null;

                GUILayout.Label(this._selectedMaterial != null ? this._selectedMaterial.shader.name : "Shader Name");

                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Render Queue:", GUILayout.ExpandWidth(false));
                    int newRenderQueue = (int)GUILayout.HorizontalSlider(this._selectedMaterial != null ? this._selectedMaterial.renderQueue : -1, -1, 5000);
                    if (this._selectedMaterial != null && newRenderQueue != this._selectedMaterial.renderQueue)
                    {
                        RendererData.MaterialData data;
                        if (this.SetMaterialDirty(this._selectedMaterial, out data))
                            data.renderQueue = this._selectedMaterial.renderQueue;
                        this._selectedMaterial.renderQueue = newRenderQueue;
                    }
                    GUILayout.Label(this._selectedMaterial != null ? this._selectedMaterial.renderQueue.ToString("0000") : "-1", GUILayout.ExpandWidth(false));
                    if (GUILayout.Button("-1000", GUILayout.ExpandWidth(false)) && this._selectedMaterial != null)
                    {
                        RendererData.MaterialData data;
                        if (this.SetMaterialDirty(this._selectedMaterial, out data))
                            data.renderQueue = this._selectedMaterial.renderQueue;
                        this._selectedMaterial.renderQueue = newRenderQueue;
                    }
                    GUILayout.EndHorizontal();
                    
                }

                {
                    GUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Reset") && this._selectedMaterial != null)
                        this.ResetMaterial(this._selectedMaterial);
                    GUILayout.EndHorizontal();
                }

                GUI.enabled = true;
            }
            GUILayout.EndVertical();

            GUILayout.BeginVertical("Renderers", GUI.skin.window);
            this._rendererScroll = GUILayout.BeginScrollView(this._rendererScroll);
            foreach (Renderer renderer in Studio.Studio.Instance.dicInfo[Studio.Studio.Instance.treeNodeCtrl.selectNode].guideObject.transformTarget.GetComponentsInChildren<Renderer>(true))
            {
                Color c = GUI.color;
                bool isMaterialDirty = this._dirtyRenderers.ContainsKey(renderer);
                if (renderer == this._selectedRenderer)
                    GUI.color = Color.cyan;
                else if (isMaterialDirty)
                    GUI.color = Color.magenta;

                if (GUILayout.Button(renderer.name + (isMaterialDirty ? "*" : "")))
                    this._selectedRenderer = renderer;
                GUI.color = c;
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();

            GUI.DragWindow();
        }

        private bool SetMaterialDirty(Material mat, out RendererData.MaterialData data)
        {
            RendererData rendererData;
            if (this._dirtyRenderers.TryGetValue(this._selectedRenderer, out rendererData) == false || rendererData.dirtyMaterials.TryGetValue(mat, out data) == false)
            {
                data = new RendererData.MaterialData();
                this.SetRendererDirty(this._selectedRenderer, out rendererData);
                rendererData.dirtyMaterials.Add(mat, data);
                return true;
            }
            return false;
        }

        private bool SetMaterialDirty(Material mat, out RendererData.MaterialData data, RendererData rendererData)
        {
            if (rendererData.dirtyMaterials.TryGetValue(mat, out data) == false)
            {
                data = new RendererData.MaterialData();
                rendererData.dirtyMaterials.Add(mat, data);
                return true;
            }
            return false;
        }

        private void ResetMaterial(Material mat)
        {
            RendererData rendererData;
            if (this._dirtyRenderers.TryGetValue(this._selectedRenderer, out rendererData) && rendererData.dirtyMaterials.TryGetValue(mat, out RendererData.MaterialData data))
                this.ResetMaterial(mat, data, rendererData);
        }

        private void ResetMaterial(Material mat, RendererData.MaterialData materialData, RendererData rendererData)
        {
            mat.renderQueue = materialData.renderQueue;
            rendererData.dirtyMaterials.Remove(mat);
        }

        private bool SetRendererDirty(Renderer renderer, out RendererData data)
        {
            if (this._dirtyRenderers.TryGetValue(renderer, out data) == false)
            {
                data = new RendererData();
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
                renderer.shadowCastingMode = data.shadowCastingMode;
                renderer.receiveShadows = data.receiveShadow;
                if (withMaterials)
                    foreach (KeyValuePair<Material, RendererData.MaterialData> pair in new Dictionary<Material, RendererData.MaterialData>(data.dirtyMaterials))
                        this.ResetMaterial(pair.Key, pair.Value, data);
                if (data.dirtyMaterials.Count == 0)
                    this._dirtyRenderers.Remove(renderer);
            }
        }

        private void OnSceneLoad(string path, XmlNode node)
        {
            if (node == null)
                return;
            node = node.CloneNode(true);
            this.ExecuteDelayed(() =>
            {
                List<KeyValuePair<int, ObjectCtrlInfo>> dic = new SortedDictionary<int, Studio.ObjectCtrlInfo>(Studio.Studio.Instance.dicObjectCtrl).ToList();
                foreach (XmlNode childNode in node.ChildNodes)
                {
                    switch (childNode.Name)
                    {
                        case "renderer":
                            int objectIndex = XmlConvert.ToInt32(childNode.Attributes["objectIndex"].Value);
                            Transform t = dic[objectIndex].Value.guideObject.transformTarget;
                            string rendererPath = childNode.Attributes["rendererPath"].Value;
                            Renderer renderer = t.Find(rendererPath).GetComponent<Renderer>();

                            if (this.SetRendererDirty(renderer, out RendererData rendererData))
                            {
                                rendererData.shadowCastingMode = renderer.shadowCastingMode;
                                renderer.shadowCastingMode = (ShadowCastingMode)XmlConvert.ToInt32(childNode.Attributes["shadowCastingMode"].Value);
                                rendererData.receiveShadow = renderer.receiveShadows;
                                renderer.receiveShadows = XmlConvert.ToBoolean(childNode.Attributes["receiveShadows"].Value);

                                foreach (XmlNode grandChildNode in childNode.ChildNodes)
                                {
                                    switch (grandChildNode.Name)
                                    {
                                        case "material":
                                            int index = XmlConvert.ToInt32(grandChildNode.Attributes["index"].Value);
                                            Material mat = renderer.sharedMaterials[index];

                                            if (this.SetMaterialDirty(mat, out RendererData.MaterialData materialData, rendererData))
                                            {
                                                materialData.renderQueue = mat.renderQueue;
                                                mat.renderQueue = XmlConvert.ToInt32(grandChildNode.Attributes["renderQueue"].Value);
                                            }
                                            break;
                                    }
                                }
                            }
                            break;
                    }
                }
            }, 3);
        }

        private void OnSceneSave(string path, XmlTextWriter writer)
        {
            List<KeyValuePair<int, ObjectCtrlInfo>> dic = new SortedDictionary<int, Studio.ObjectCtrlInfo>(Studio.Studio.Instance.dicObjectCtrl).ToList();
            foreach (KeyValuePair<Renderer, RendererData> rendererPair in this._dirtyRenderers)
            {
                Transform t = rendererPair.Key.transform;
                int objectIndex = -1;
                while ((objectIndex = dic.FindIndex(e => e.Value.guideObject.transformTarget == t)) == -1)
                    t = t.parent;

                writer.WriteStartElement("renderer");
                writer.WriteAttributeString("objectIndex", XmlConvert.ToString(objectIndex));
                writer.WriteAttributeString("rendererPath", rendererPair.Key.transform.GetPathFrom(t));

                writer.WriteAttributeString("shadowCastingMode", XmlConvert.ToString((int)rendererPair.Key.shadowCastingMode));
                writer.WriteAttributeString("receiveShadows", XmlConvert.ToString(rendererPair.Key.receiveShadows));

                List<Material> materials = rendererPair.Key.sharedMaterials.ToList();
                foreach (KeyValuePair<Material, RendererData.MaterialData> materialPair in rendererPair.Value.dirtyMaterials)
                {
                    writer.WriteStartElement("material");
                    writer.WriteAttributeString("index", XmlConvert.ToString(materials.IndexOf(materialPair.Key)));
                    writer.WriteAttributeString("renderQueue", XmlConvert.ToString(materialPair.Key.renderQueue));
                    writer.WriteEndElement();
                }

                writer.WriteEndElement();
            }
        }
        #endregion
    }
}