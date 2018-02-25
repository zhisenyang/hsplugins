using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml;
using UnityEngine;
using UnityStandardAssets.ImageEffects;

namespace FogEditor
{
    public class MainWindow : MonoBehaviour
    {
        #region Private Variables
        private const float _width = 400;
        private const float _height = 500;
        private bool _enabled = false;
        private GlobalFog _fog;
        private Rect _windowRect = new Rect((Screen.width - _width) / 2f, (Screen.height - _height) / 2f, _width, _height);
        private bool _mouseInWindow;
        private FogMode[] _fogModeValues;
        #endregion

        #region Default Parameters
        private Color _defaultColor;
        private bool _defaultExcludeFarPixels;
        private bool _defaultDistanceFog;
        private FogMode _defaultDistanceFogMode;
        private bool _defaultUseRadialDistance;
        private float _defaultDistanceFogStartDistance;
        private float _defaultDistanceFogEndDistance;
        private float _defaultDistanceFogDensity;
        private bool _defaultHeightFog;
        private float _defaultHeightFogHeight;
        private float _defaultHeightFogHeightDensity;
        private float _defaultHeightFogStartDistance;
        private Vector2 _scroll;
        private Material _selectedMat;
        private string _search = "";
        private bool _advancedMode;
        private Dictionary<Material, int> _dirtyMaterials = new Dictionary<Material, int>();
        #endregion

        #region Unity Methods
        void Start()
        {
            HSExtSave.HSExtSave.RegisterHandler("fogEditor", null, null, this.OnSceneLoad, null, this.OnSceneSave, null, null);

            this._fog = Camera.main.GetComponent<GlobalFog>();
            this._fogModeValues = (FogMode[])Enum.GetValues(typeof(FogMode));
            this._defaultColor = RenderSettings.fogColor;
            this._defaultExcludeFarPixels = this._fog.excludeFarPixels;
            this._defaultDistanceFog = this._fog.distanceFog;
            this._defaultDistanceFogMode = RenderSettings.fogMode;
            this._defaultUseRadialDistance = this._fog.useRadialDistance;
            this._defaultDistanceFogStartDistance = RenderSettings.fogStartDistance;
            this._defaultDistanceFogEndDistance = RenderSettings.fogEndDistance;
            this._defaultDistanceFogDensity = RenderSettings.fogDensity;
            this._defaultHeightFog = this._fog.heightFog;
            this._defaultHeightFogHeight = this._fog.height;
            this._defaultHeightFogHeightDensity = this._fog.heightDensity;
            this._defaultHeightFogStartDistance = this._fog.startDistance;
        }

        void Update()
        {
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.F))
                this._enabled = !this._enabled;
        }

        void OnGUI()
        {
            if (!this._enabled)
                return;
            this._windowRect = GUILayout.Window(0, this._windowRect, this.WindowFunction, "Fog Editor");
            this._mouseInWindow = this._windowRect.Contains(Event.current.mousePosition);
            if (this._mouseInWindow)
                Studio.Studio.Instance.cameraCtrl.noCtrlCondition = () => this._mouseInWindow && this._enabled;
        }

        #endregion

        #region Private Methods
        private void WindowFunction(int id)
        {
            GUILayout.BeginVertical("Global Fog Parameters", GUI.skin.window);

            {
                Color c = RenderSettings.fogColor;

                GUILayout.Label("Color:");
                GUILayout.BeginHorizontal();
                GUILayout.Label("R", GUILayout.ExpandWidth(false));
                c.r = GUILayout.HorizontalSlider(c.r, 0f, 1f);
                GUILayout.Label(c.r.ToString("0.000"), GUILayout.ExpandWidth(false));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("G", GUILayout.ExpandWidth(false));
                c.g = GUILayout.HorizontalSlider(c.g, 0f, 1f);
                GUILayout.Label(c.g.ToString("0.000"), GUILayout.ExpandWidth(false));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("B", GUILayout.ExpandWidth(false));
                c.b = GUILayout.HorizontalSlider(c.b, 0f, 1f);
                GUILayout.Label(c.b.ToString("0.000"), GUILayout.ExpandWidth(false));
                GUILayout.EndHorizontal();

                RenderSettings.fogColor = c;
            }

            this._fog.excludeFarPixels = GUILayout.Toggle(this._fog.excludeFarPixels, "Exclude Far Pixels");


            GUILayout.EndVertical();

            GUILayout.BeginVertical("Distance Fog Parameters", GUI.skin.window);

            this._fog.distanceFog = GUILayout.Toggle(this._fog.distanceFog, "Enabled");

            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Mode:");
                foreach (FogMode mode in this._fogModeValues)
                {
                    if (GUILayout.Toggle(RenderSettings.fogMode == mode, mode.ToString()))
                        RenderSettings.fogMode = mode;
                }
                GUILayout.EndHorizontal();
            }

            this._fog.useRadialDistance = GUILayout.Toggle(this._fog.useRadialDistance, "Use Radial Distance");

            {
                GUI.enabled = RenderSettings.fogMode == FogMode.Linear;

                GUILayout.BeginHorizontal();
                GUILayout.Label("Start Distance", GUILayout.ExpandWidth(false));
                RenderSettings.fogStartDistance = GUILayout.HorizontalSlider(RenderSettings.fogStartDistance, 0.001f, 299f);
                GUILayout.Label(RenderSettings.fogStartDistance.ToString("000.000"), GUILayout.ExpandWidth(false));
                GUILayout.EndHorizontal();
                if (RenderSettings.fogStartDistance > RenderSettings.fogEndDistance)
                    RenderSettings.fogEndDistance = RenderSettings.fogStartDistance + 1;

                GUI.enabled = true;
            }

            {
                GUI.enabled = RenderSettings.fogMode == FogMode.Linear;

                GUILayout.BeginHorizontal();
                GUILayout.Label("End Distance", GUILayout.ExpandWidth(false));
                RenderSettings.fogEndDistance = GUILayout.HorizontalSlider(RenderSettings.fogEndDistance, 1.001f, 300f);
                GUILayout.Label(RenderSettings.fogEndDistance.ToString("000.000"), GUILayout.ExpandWidth(false));
                GUILayout.EndHorizontal();
                if (RenderSettings.fogEndDistance < RenderSettings.fogStartDistance)
                    RenderSettings.fogStartDistance = RenderSettings.fogEndDistance - 1;

                GUI.enabled = true;
            }

            {
                GUI.enabled = RenderSettings.fogMode != FogMode.Linear;

                GUILayout.BeginHorizontal();
                GUILayout.Label("Density", GUILayout.ExpandWidth(false));
                RenderSettings.fogDensity = GUILayout.HorizontalSlider(RenderSettings.fogDensity, 0f, 1f);
                GUILayout.Label(RenderSettings.fogDensity.ToString("0.000"), GUILayout.ExpandWidth(false));
                GUILayout.EndHorizontal();

                GUI.enabled = true;
            }

            GUILayout.EndVertical();

            GUILayout.BeginVertical("Height Fog Parameters", GUI.skin.window);

            this._fog.heightFog = GUILayout.Toggle(this._fog.heightFog, "Enabled");

            GUILayout.BeginHorizontal();
            GUILayout.Label("Height", GUILayout.ExpandWidth(false));
            this._fog.height = GUILayout.HorizontalSlider(this._fog.height, 0f, 30f);
            GUILayout.Label(this._fog.height.ToString("00.000"), GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Density", GUILayout.ExpandWidth(false));
            this._fog.heightDensity = GUILayout.HorizontalSlider(this._fog.heightDensity, 0.001f, 9.999f);
            GUILayout.Label(this._fog.heightDensity.ToString("0.000"), GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Start Distance", GUILayout.ExpandWidth(false));
            this._fog.startDistance = GUILayout.HorizontalSlider(this._fog.startDistance, 0.001f, 100f);
            GUILayout.Label(this._fog.startDistance.ToString("000.000"), GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Reset", GUILayout.ExpandWidth(false)))
            {
                RenderSettings.fogColor = this._defaultColor;
                this._fog.excludeFarPixels = this._defaultExcludeFarPixels;
                this._fog.distanceFog = this._defaultDistanceFog;
                RenderSettings.fogMode = this._defaultDistanceFogMode;
                this._fog.useRadialDistance = this._defaultUseRadialDistance;
                RenderSettings.fogStartDistance = this._defaultDistanceFogStartDistance;
                RenderSettings.fogEndDistance = this._defaultDistanceFogEndDistance;
                RenderSettings.fogDensity = this._defaultDistanceFogDensity;
                this._fog.heightFog = this._defaultHeightFog;
                this._fog.height = this._defaultHeightFogHeight;
                this._fog.heightDensity = this._defaultHeightFogHeightDensity;
                this._fog.startDistance = this._defaultHeightFogStartDistance;
            }
            GUILayout.EndHorizontal();

            bool advMode = GUILayout.Toggle(this._advancedMode, "Advanced Mode");

            if (this._advancedMode && advMode == false) // on disable
                this._windowRect.yMax = this._windowRect.yMin + _height;

            this._advancedMode = advMode;
            if (this._advancedMode)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Filter:", GUILayout.ExpandWidth(false));
                this._search = GUILayout.TextField(this._search);
                GUILayout.EndHorizontal();
                this._scroll = GUILayout.BeginScrollView(this._scroll, GUILayout.Height(200));
                if (Studio.Studio.Instance.treeNodeCtrl.selectNode != null)
                    foreach (Renderer renderer in Studio.Studio.Instance.dicInfo[Studio.Studio.Instance.treeNodeCtrl.selectNode].guideObject.transformTarget.GetComponentsInChildren<Renderer>(true))
                    {
                        foreach (Material material in renderer.materials)
                        {
                            if (material.name.IndexOf(this._search) == -1)
                                continue;
                            Color c = GUI.color;
                            bool isMaterialDirty = this._dirtyMaterials.ContainsKey(material);
                            if (material == this._selectedMat)
                                GUI.color = Color.cyan;
                            else if (isMaterialDirty)
                                GUI.color = Color.magenta;
                            if (GUILayout.Button(renderer.gameObject.name + "/" + material.name + (isMaterialDirty ? "*" : "")))
                            {
                                this._selectedMat = material;
                            }
                            GUI.color = c;
                        }
                    }

                GUILayout.EndScrollView();
                GUILayout.BeginHorizontal();
                if (this._selectedMat != null)
                {
                    GUILayout.Label("Render Queue:", GUILayout.ExpandWidth(false));
                    int renderQueue = (int)GUILayout.HorizontalSlider(this._selectedMat.renderQueue, -1, 5000); //TODO ADD RESET INDIVIDUAL
                    this.SetRenderQueue(this._selectedMat, renderQueue);
                    GUILayout.Label(this._selectedMat.renderQueue.ToString("0000"), GUILayout.ExpandWidth(false));
                    if (GUILayout.Button("-1000", GUILayout.ExpandWidth(false)))
                        this.SetRenderQueue(this._selectedMat, Mathf.Clamp(this._selectedMat.renderQueue - 1000, -1, 5000));
                    if (GUILayout.Button("Reset", GUILayout.ExpandWidth(false)) && this._dirtyMaterials.ContainsKey(this._selectedMat))
                    {
                        this._selectedMat.renderQueue = this._dirtyMaterials[this._selectedMat];
                        this._dirtyMaterials.Remove(this._selectedMat);
                    }
                }
                else
                {
                    GUI.enabled = false;
                    GUILayout.Label("Render Queue:", GUILayout.ExpandWidth(false));
                    GUILayout.HorizontalSlider(-1, -1, 5000);
                    GUILayout.Label("-1", GUILayout.ExpandWidth(false));
                    GUILayout.Button("Reset", GUILayout.ExpandWidth(false));
                    GUI.enabled = true;
                }
                GUILayout.EndHorizontal();
                
            }
            GUI.DragWindow();
        }

        private void SetRenderQueue(Material mat, int renderQueue)
        {
            if (renderQueue != mat.renderQueue && this._dirtyMaterials.ContainsKey(mat) == false)
                this._dirtyMaterials.Add(mat, mat.renderQueue);
            mat.renderQueue = renderQueue;
        }

        private void OnSceneLoad(string path, XmlNode node)
        {
            if (node == null)
                return;
            foreach (XmlNode childNode in node.ChildNodes)
            {
                switch (childNode.Name)
                {
                    case "color":
                        RenderSettings.fogColor = new Color(
                            XmlConvert.ToSingle(childNode.Attributes["r"].Value),
                            XmlConvert.ToSingle(childNode.Attributes["g"].Value),
                            XmlConvert.ToSingle(childNode.Attributes["b"].Value)
                            );
                        break;
                    case "excludeFarPixels":
                        this._fog.excludeFarPixels = XmlConvert.ToBoolean(childNode.Attributes["enabled"].Value);
                        break;
                    case "distanceFog":
                        this._fog.distanceFog = XmlConvert.ToBoolean(childNode.Attributes["enabled"].Value);
                        break;
                    case "distanceFogMode":
                        RenderSettings.fogMode = (FogMode)XmlConvert.ToInt32(childNode.Attributes["mode"].Value);
                        break;
                    case "useRadialDistance":
                        this._fog.useRadialDistance = XmlConvert.ToBoolean(childNode.Attributes["enabled"].Value);
                        break;
                    case "distanceFogStartDistance":
                        RenderSettings.fogStartDistance = XmlConvert.ToSingle(childNode.Attributes["value"].Value);
                        break;
                    case "distanceFogEndDistance":
                        RenderSettings.fogEndDistance = XmlConvert.ToSingle(childNode.Attributes["value"].Value);
                        break;
                    case "distanceFogDensity":
                        RenderSettings.fogDensity = XmlConvert.ToSingle(childNode.Attributes["value"].Value);
                        break;
                    case "heightFog":
                        this._fog.heightFog = XmlConvert.ToBoolean(childNode.Attributes["enabled"].Value);
                        break;
                    case "height":
                        this._fog.height = XmlConvert.ToSingle(childNode.Attributes["value"].Value);
                        break;
                    case "heightDensity":
                        this._fog.heightDensity = XmlConvert.ToSingle(childNode.Attributes["value"].Value);
                        break;
                    case "heightFogStartDistance":
                        this._fog.startDistance = XmlConvert.ToSingle(childNode.Attributes["value"].Value);
                        break;
                }
            }
        }

        private void OnSceneSave(string path, XmlTextWriter writer)
        {
            writer.WriteStartElement("color");
            writer.WriteAttributeString("r", XmlConvert.ToString(RenderSettings.fogColor.r));
            writer.WriteAttributeString("g", XmlConvert.ToString(RenderSettings.fogColor.g));
            writer.WriteAttributeString("b", XmlConvert.ToString(RenderSettings.fogColor.b));
            writer.WriteEndElement();

            writer.WriteStartElement("excludeFarPixels");
            writer.WriteAttributeString("enabled", XmlConvert.ToString(this._fog.excludeFarPixels));
            writer.WriteEndElement();

            writer.WriteStartElement("distanceFog");
            writer.WriteAttributeString("enabled", XmlConvert.ToString(this._fog.distanceFog));
            writer.WriteEndElement();

            writer.WriteStartElement("distanceFogMode");
            writer.WriteAttributeString("mode", XmlConvert.ToString((int)RenderSettings.fogMode));
            writer.WriteEndElement();

            writer.WriteStartElement("useRadialDistance");
            writer.WriteAttributeString("enabled", XmlConvert.ToString(this._fog.useRadialDistance));
            writer.WriteEndElement();

            writer.WriteStartElement("distanceFogStartDistance");
            writer.WriteAttributeString("value", XmlConvert.ToString(RenderSettings.fogStartDistance));
            writer.WriteEndElement();

            writer.WriteStartElement("distanceFogEndDistance");
            writer.WriteAttributeString("value", XmlConvert.ToString(RenderSettings.fogEndDistance));
            writer.WriteEndElement();

            writer.WriteStartElement("distanceFogDensity");
            writer.WriteAttributeString("value", XmlConvert.ToString(RenderSettings.fogDensity));
            writer.WriteEndElement();

            writer.WriteStartElement("heightFog");
            writer.WriteAttributeString("enabled", XmlConvert.ToString(this._fog.heightFog));
            writer.WriteEndElement();

            writer.WriteStartElement("height");
            writer.WriteAttributeString("value", XmlConvert.ToString(this._fog.height));
            writer.WriteEndElement();

            writer.WriteStartElement("heightDensity");
            writer.WriteAttributeString("value", XmlConvert.ToString(this._fog.heightDensity));
            writer.WriteEndElement();

            writer.WriteStartElement("heightFogStartDistance");
            writer.WriteAttributeString("value", XmlConvert.ToString(this._fog.startDistance));
            writer.WriteEndElement();
        }
        #endregion
    }
}
