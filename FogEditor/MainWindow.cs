using System;
using System.Collections.Generic;
using System.Reflection;
using System.Xml;
using Harmony;
using UILib;
using UnityEngine;
using UnityEngine.UI;
using UnityStandardAssets.ImageEffects;

namespace FogEditor
{
    public class MainWindow : MonoBehaviour
    {
        #region Public Variables
        public static MainWindow self;
        #endregion

        #region Private Variables
        private GlobalFog _fog;

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
        private Canvas _ui;
        private Toggle _linearToggle;
        private Toggle _exponentialToggle;
        private Toggle _exponentialSquaredToggle;
        private Image _colorBackground;
        private GameObject _globalDensityContainer;
        private Slider _globalDensitySlider;
        private InputField _globalDensityInputField;
        private Slider _startRangeSlider;
        private InputField _startRangeInputField;
        private GameObject _startRangeContainer;
        private Slider _endRangeSlider;
        private InputField _endRangeInputField;
        private GameObject _endRangeContainer;
        private Slider _startDistanceSlider;
        private InputField _startDistanceInputField;
        private Toggle _excludeFarPixels;
        private Toggle _distanceFogEnabled;
        private Toggle _useRadialDistance;
        private Toggle _heightFogEnabled;
        private Slider _heightSlider;
        private InputField _heightInputField;
        private GameObject _heightContainer;
        private Slider _heightDensitySlider;
        private InputField _heightDensityInputField;
        private GameObject _heightDensityContainer;
        private Toggle _alternativeRendering;
        #endregion

        #region Public Accessors
        public bool alternativeRendering { get { return this._alternativeRendering != null && this._alternativeRendering.isOn; } }
        #endregion

        #region Unity Methods
        void Awake()
        {
            self = this;
        }
        void Start()
        {
            HSExtSave.HSExtSave.RegisterHandler("fogEditor", null, null, this.OnSceneLoad, null, this.OnSceneSave, null, null);

            this._fog = Camera.main.GetComponent<GlobalFog>();
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

            this.SpawnUI();
            this.UpdateUI();
            this._ui.enabled = false;
        }

        void Update()
        {
            Effects_OnRenderImage_Patches._alreadyRendered = false;
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.F))
            {
                this._ui.enabled = !this._ui.enabled;
                Studio.Studio.Instance.colorMenu.updateColorFunc = null;
                Studio.Studio.Instance.colorPaletteCtrl.visible = false;
                if (this._ui.enabled)
                    LayoutRebuilder.ForceRebuildLayoutImmediate(this._ui.transform.GetChild(0) as RectTransform);
            }
        }
        #endregion

        #region Private Methods
        private void SpawnUI()
        {
            AssetBundle bundle = AssetBundle.LoadFromMemory(Properties.Resources.FogEditorResources);
            this._ui = Instantiate(bundle.LoadAsset<GameObject>("FogEditorCanvas")).GetComponent<Canvas>();
            bundle.Unload(false);

            RectTransform bg = (RectTransform)this._ui.transform.Find("BG");
            Transform topContainer = bg.Find("Top Container");
            UIUtility.MakeObjectDraggable(topContainer as RectTransform, bg);
            this._linearToggle = this._ui.transform.LinkToggleTo("BG/Global Fog/Mode Container/Toggle", (b) => this.FogModeChanged(FogMode.Linear));
            this._exponentialToggle = this._ui.transform.LinkToggleTo("BG/Global Fog/Mode Container/Toggle (1)", (b) => this.FogModeChanged(FogMode.Exponential));
            this._exponentialSquaredToggle = this._ui.transform.LinkToggleTo("BG/Global Fog/Mode Container/Toggle (2)", (b) => this.FogModeChanged(FogMode.ExponentialSquared));
            this._colorBackground = this._ui.transform.LinkButtonTo("BG/Global Fog/Color Container/Image", this.OnClickColor).GetComponent<Image>();

            this._globalDensitySlider = this._ui.transform.LinkSliderTo("BG/Global Fog/Density/Slider", this.GlobalDensityChanged);
            this._globalDensityInputField = this._ui.transform.LinkInputFieldTo("BG/Global Fog/Density/InputField", null, this.GlobalDensityChanged);
            this._globalDensityContainer = this._globalDensitySlider.transform.parent.gameObject;

            this._startRangeSlider = this._ui.transform.LinkSliderTo("BG/Global Fog/Start Range/Slider", this.StartRangeChanged);
            this._startRangeInputField = this._ui.transform.LinkInputFieldTo("BG/Global Fog/Start Range/InputField", null, this.StartRangeChanged);
            this._startRangeContainer = this._startRangeSlider.transform.parent.gameObject;

            this._endRangeSlider = this._ui.transform.LinkSliderTo("BG/Global Fog/End Range/Slider", this.EndRangeChanged);
            this._endRangeInputField = this._ui.transform.LinkInputFieldTo("BG/Global Fog/End Range/InputField", null, this.EndRangeChanged);
            this._endRangeContainer = this._endRangeSlider.transform.parent.gameObject;

            this._startDistanceSlider = this._ui.transform.LinkSliderTo("BG/Global Fog/Start Distance/Slider", this.StartDistanceChanged);
            this._startDistanceInputField = this._ui.transform.LinkInputFieldTo("BG/Global Fog/Start Distance/InputField", null, this.StartDistanceChanged);

            this._excludeFarPixels = this._ui.transform.LinkToggleTo("BG/Global Fog/Exclude Far Pixels", this.ExcludeFarPixelsChanged);

            this._alternativeRendering = this._ui.transform.LinkToggleTo("BG/Global Fog/Alternative Rendering", null);

            this._distanceFogEnabled = this._ui.transform.LinkToggleTo("BG/Distance Fog/Enabled", this.DistanceFogEnabledChanged);

            this._useRadialDistance = this._ui.transform.LinkToggleTo("BG/Distance Fog/Radial Distance", this.UseRadialDistanceChanged);

            this._heightFogEnabled = this._ui.transform.LinkToggleTo("BG/Height Fog/Enabled", this.HeightFogEnabledChanged);

            this._heightSlider = this._ui.transform.LinkSliderTo("BG/Height Fog/Height/Slider", this.HeightChanged);
            this._heightInputField = this._ui.transform.LinkInputFieldTo("BG/Height Fog/Height/InputField", null, this.HeightChanged);
            this._heightContainer = this._heightSlider.transform.parent.gameObject;

            this._heightDensitySlider = this._ui.transform.LinkSliderTo("BG/Height Fog/Density/Slider", this.HeightDensityChanged);
            this._heightDensityInputField = this._ui.transform.LinkInputFieldTo("BG/Height Fog/Density/InputField", null, this.HeightDensityChanged);
            this._heightDensityContainer = this._heightDensitySlider.transform.parent.gameObject;

            this._ui.transform.LinkButtonTo("BG/Button Container/Button", this.Reset);
        }

        private void UpdateUI()
        {
            switch (RenderSettings.fogMode)
            {
                case FogMode.Linear:
                    this._linearToggle.isOn = true;
                    break;
                case FogMode.Exponential:
                    this._exponentialToggle.isOn = true;
                    break;
                case FogMode.ExponentialSquared:
                    this._exponentialSquaredToggle.isOn = true;
                    break;
            }
            this._colorBackground.color = RenderSettings.fogColor;
            this._globalDensitySlider.value = RenderSettings.fogDensity;
            this._globalDensityInputField.text = RenderSettings.fogDensity.ToString("0.000");
            this._startRangeSlider.value = RenderSettings.fogStartDistance;
            this._startRangeInputField.text = RenderSettings.fogStartDistance.ToString("0.000");
            this._endRangeSlider.value = RenderSettings.fogEndDistance;
            this._endRangeInputField.text = RenderSettings.fogEndDistance.ToString("0.000");
            this._startDistanceSlider.value = this._fog.startDistance;
            this._startDistanceInputField.text = this._fog.startDistance.ToString("0.000");
            this._excludeFarPixels.isOn = this._fog.excludeFarPixels;
            this._distanceFogEnabled.isOn = this._fog.distanceFog;
            this._useRadialDistance.isOn = this._fog.useRadialDistance;
            this._heightFogEnabled.isOn = this._fog.heightFog;
            this._heightSlider.value = this._fog.height;
            this._heightInputField.text = this._fog.height.ToString("0.000");
            this._heightDensitySlider.value = this._fog.heightDensity;
            this._heightDensityInputField.text = this._fog.heightDensity.ToString("0.000");

            this._globalDensityContainer.SetActive(RenderSettings.fogMode != FogMode.Linear);
            this._startRangeContainer.SetActive(RenderSettings.fogMode == FogMode.Linear);
            this._endRangeContainer.SetActive(RenderSettings.fogMode == FogMode.Linear);
            this._useRadialDistance.gameObject.SetActive(this._fog.distanceFog);
            this._heightContainer.SetActive(this._fog.heightFog);
            this._heightDensityContainer.SetActive(this._fog.heightFog);
        }

        private void FogModeChanged(FogMode fogMode)
        {
            RenderSettings.fogMode = fogMode;
            this._globalDensityContainer.SetActive(fogMode != FogMode.Linear);
            this._startRangeContainer.SetActive(fogMode == FogMode.Linear);
            this._endRangeContainer.SetActive(fogMode == FogMode.Linear);
        }

        private void OnClickColor()
        {
            Studio.Studio.Instance.colorPaletteCtrl.visible = !Studio.Studio.Instance.colorPaletteCtrl.visible;
            if (Studio.Studio.Instance.colorPaletteCtrl.visible)
            {
                Studio.Studio.Instance.colorMenu.updateColorFunc = this.ColorUpdated;
                Studio.Studio.Instance.colorMenu.SetColor(RenderSettings.fogColor, UI_ColorInfo.ControlType.PresetsSample);                
            }
        }

        private void ColorUpdated(Color c)
        {
            this._colorBackground.color = c;
            RenderSettings.fogColor = c;
        }

        private void GlobalDensityChanged(float f)
        {
            RenderSettings.fogDensity = this._globalDensitySlider.value;
            this._globalDensityInputField.text = f.ToString("0.000");
        }

        private void GlobalDensityChanged(string s)
        {
            if (float.TryParse(this._globalDensityInputField.text, out float value))
                RenderSettings.fogDensity = value;
            this._globalDensitySlider.value = RenderSettings.fogDensity;
            this._globalDensityInputField.text = RenderSettings.fogDensity.ToString("0.000");
        }

        private void StartRangeChanged(float f)
        {
            RenderSettings.fogStartDistance = this._startRangeSlider.value;
            this._startRangeInputField.text = f.ToString("0.000");
            if (RenderSettings.fogStartDistance > RenderSettings.fogEndDistance)
            {
                RenderSettings.fogEndDistance = RenderSettings.fogStartDistance + 1;
                this._endRangeSlider.value = RenderSettings.fogEndDistance;
                this._endRangeInputField.text = RenderSettings.fogEndDistance.ToString("0.000");
            }
        }

        private void StartRangeChanged(string s)
        {
            if (float.TryParse(this._startRangeInputField.text, out float value))
            {
                RenderSettings.fogStartDistance = value;
                if (RenderSettings.fogStartDistance > RenderSettings.fogEndDistance)
                {
                    RenderSettings.fogEndDistance = RenderSettings.fogStartDistance + 1;
                    this._endRangeSlider.value = RenderSettings.fogEndDistance;
                    this._endRangeInputField.text = RenderSettings.fogEndDistance.ToString("0.000");
                }
            }
            this._startRangeSlider.value = RenderSettings.fogStartDistance;
            this._startRangeInputField.text = RenderSettings.fogStartDistance.ToString("0.000");
        }
        
        private void EndRangeChanged(float f)
        {
            RenderSettings.fogEndDistance = this._endRangeSlider.value;
            this._endRangeInputField.text = f.ToString("0.000");
            if (RenderSettings.fogEndDistance < RenderSettings.fogStartDistance)
            {
                RenderSettings.fogStartDistance = RenderSettings.fogEndDistance - 1;
                this._startRangeSlider.value = RenderSettings.fogStartDistance;
                this._startRangeInputField.text = RenderSettings.fogStartDistance.ToString("0.000");
            }
        }

        private void EndRangeChanged(string s)
        {
            if (float.TryParse(this._endRangeInputField.text, out float value))
            {
                RenderSettings.fogEndDistance = value;
                if (RenderSettings.fogEndDistance < RenderSettings.fogStartDistance)
                {
                    RenderSettings.fogStartDistance = RenderSettings.fogEndDistance - 1;
                    this._startRangeSlider.value = RenderSettings.fogStartDistance;
                    this._startRangeInputField.text = RenderSettings.fogStartDistance.ToString("0.000");
                }
            }
            this._endRangeSlider.value = RenderSettings.fogEndDistance;
            this._endRangeInputField.text = RenderSettings.fogEndDistance.ToString("0.000");
        }

        private void StartDistanceChanged(float f)
        {
            this._fog.startDistance = this._startDistanceSlider.value;
            this._startDistanceInputField.text = f.ToString("0.000");
        }

        private void StartDistanceChanged(string s)
        {
            if (float.TryParse(this._startDistanceInputField.text, out float value))
                this._fog.startDistance = value;
            this._startDistanceSlider.value = this._fog.startDistance;
            this._startDistanceInputField.text = this._fog.startDistance.ToString("0.000");
        }

        private void ExcludeFarPixelsChanged(bool b)
        {
            this._fog.excludeFarPixels = this._excludeFarPixels.isOn;
        }

        private void DistanceFogEnabledChanged(bool b)
        {
            this._fog.distanceFog = this._distanceFogEnabled.isOn;
            this._useRadialDistance.gameObject.SetActive(this._distanceFogEnabled.isOn);
        }

        private void UseRadialDistanceChanged(bool b)
        {
            this._fog.useRadialDistance = this._useRadialDistance.isOn;
        }
        private void HeightFogEnabledChanged(bool b)
        {
            this._fog.heightFog = this._heightFogEnabled.isOn;
            this._heightContainer.SetActive(this._heightFogEnabled.isOn);
            this._heightDensityContainer.SetActive(this._heightFogEnabled.isOn);
        }

        private void HeightChanged(float f)
        {
            this._fog.height = this._heightSlider.value;
            this._heightInputField.text = this._heightSlider.value.ToString("0.000");
        }

        private void HeightChanged(string s)
        {
            if (float.TryParse(this._heightInputField.text, out float value))
                this._fog.height = value;
            this._heightSlider.value = this._fog.height;
            this._heightInputField.text = this._fog.height.ToString("0.000");
        }
        private void HeightDensityChanged(float f)
        {
            this._fog.heightDensity = this._heightDensitySlider.value;
            this._heightDensityInputField.text = f.ToString("0.000");
        }

        private void HeightDensityChanged(string s)
        {
            if (float.TryParse(this._heightDensityInputField.text, out float value))
                this._fog.heightDensity = value;
            this._heightSlider.value = this._fog.heightDensity;
            this._heightDensityInputField.text = this._fog.heightDensity.ToString("0.000");
        }

        private void Reset()
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
            this._alternativeRendering.isOn = false;
            this.UpdateUI();
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
                    case "fogMode":
                    case "distanceFogMode":
                        RenderSettings.fogMode = (FogMode)XmlConvert.ToInt32(childNode.Attributes["mode"].Value);
                        break;
                    case "useRadialDistance":
                        this._fog.useRadialDistance = XmlConvert.ToBoolean(childNode.Attributes["enabled"].Value);
                        break;
                    case "startRange":
                    case "distanceFogStartDistance":
                        RenderSettings.fogStartDistance = XmlConvert.ToSingle(childNode.Attributes["value"].Value);
                        break;
                    case "endRange":
                    case "distanceFogEndDistance":
                        RenderSettings.fogEndDistance = XmlConvert.ToSingle(childNode.Attributes["value"].Value);
                        break;
                    case "globalFogDensity":
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
                    case "startDistance":
                    case "heightFogStartDistance":
                        this._fog.startDistance = XmlConvert.ToSingle(childNode.Attributes["value"].Value);
                        break;
                    case "alternativeRendering":
                        this._alternativeRendering.isOn = XmlConvert.ToBoolean(childNode.Attributes["enabled"].Value);
                        break;
                }
            }
            this.UpdateUI();
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

            writer.WriteStartElement("fogMode");
            writer.WriteAttributeString("mode", XmlConvert.ToString((int)RenderSettings.fogMode));
            writer.WriteEndElement();

            writer.WriteStartElement("useRadialDistance");
            writer.WriteAttributeString("enabled", XmlConvert.ToString(this._fog.useRadialDistance));
            writer.WriteEndElement();

            writer.WriteStartElement("alternativeRendering");
            writer.WriteAttributeString("enabled", XmlConvert.ToString(this._alternativeRendering.isOn));
            writer.WriteEndElement();

            writer.WriteStartElement("startRange");
            writer.WriteAttributeString("value", XmlConvert.ToString(RenderSettings.fogStartDistance));
            writer.WriteEndElement();

            writer.WriteStartElement("endRange");
            writer.WriteAttributeString("value", XmlConvert.ToString(RenderSettings.fogEndDistance));
            writer.WriteEndElement();

            writer.WriteStartElement("globalFogDensity");
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

            writer.WriteStartElement("startDistance");
            writer.WriteAttributeString("value", XmlConvert.ToString(this._fog.startDistance));
            writer.WriteEndElement();
        }
        #endregion

        #region Patches
        [HarmonyPatch(typeof(GlobalFog), "OnRenderImage", new[] { typeof(RenderTexture), typeof(RenderTexture) })]
        private class GlobalFog_OnRenderImage_Patches
        {
            public static bool Prefix(GlobalFog __instance, RenderTexture source, RenderTexture destination)
            {
                if (MainWindow.self != null && MainWindow.self.alternativeRendering && __instance.CompareTag("MainCamera"))
                {
                    Effects_OnRenderImage_Patches._globalFog = __instance;
                    if (Effects_OnRenderImage_Patches._globalFog != null && Effects_OnRenderImage_Patches._fogMaterial == null)
                        Effects_OnRenderImage_Patches._fogMaterial = (Material)Effects_OnRenderImage_Patches._globalFog.GetType().GetField("fogMaterial", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(Effects_OnRenderImage_Patches._globalFog);
                    Graphics.Blit(source, destination);
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(DepthOfField), "OnRenderImage", new []{typeof(RenderTexture), typeof(RenderTexture)})]
        [HarmonyPatch(typeof(VignetteAndChromaticAberration), "OnRenderImage", new []{typeof(RenderTexture), typeof(RenderTexture)})]
        [HarmonyPatch(typeof(SunShafts), "OnRenderImage", new []{typeof(RenderTexture), typeof(RenderTexture)})]
        [HarmonyPatch(typeof(ColorCorrectionCurves), "OnRenderImage", new []{typeof(RenderTexture), typeof(RenderTexture)})]
        [HarmonyPatch(typeof(BloomAndFlares), "OnRenderImage", new []{typeof(RenderTexture), typeof(RenderTexture)})]
        [HarmonyPatch(typeof(Antialiasing), "OnRenderImage", new []{typeof(RenderTexture), typeof(RenderTexture)})]
        [HarmonyPatch(typeof(CrossFade), "OnRenderImage", new []{typeof(RenderTexture), typeof(RenderTexture)})]
        [HarmonyPatch(typeof(GameScreenShotAssist), "OnRenderImage", new []{typeof(RenderTexture), typeof(RenderTexture)})]
        private class Effects_OnRenderImage_Patches
        {
            internal static GlobalFog _globalFog;
            internal static Material _fogMaterial;
            internal static bool _alreadyRendered = false;

            private static RenderTexture _intermediateSourceDestination;

            public static bool Prefix(object __instance, ref RenderTexture source, RenderTexture destination)
            {
                if (MainWindow.self == null || !MainWindow.self.alternativeRendering || _globalFog == null || !_globalFog.enabled || _alreadyRendered || ((PostEffectsBase)__instance).CompareTag("MainCamera") == false)
                {
                    return true;
                }

                _intermediateSourceDestination = RenderTexture.GetTemporary(destination.width, destination.height, destination.depth, destination.format, RenderTextureReadWrite.Default, destination.antiAliasing);

                _alreadyRendered = true;

                if (_globalFog.CheckResources() == false || (!_globalFog.distanceFog && !_globalFog.heightFog))
                {
                    Graphics.Blit(source, _intermediateSourceDestination);
                    source = _intermediateSourceDestination;
                    return true;
                }
                Camera cam = _globalFog.GetComponent<Camera>();
                Transform camtr = cam.transform;
                float camNear = cam.nearClipPlane;
                float camFar = cam.farClipPlane;
                float camFov = cam.fieldOfView;
                float camAspect = cam.aspect;

                Matrix4x4 frustumCorners = Matrix4x4.identity;

                float fovWHalf = camFov * 0.5f;

                Vector3 toRight = camtr.right * camNear * Mathf.Tan(fovWHalf * Mathf.Deg2Rad) * camAspect;
                Vector3 toTop = camtr.up * camNear * Mathf.Tan(fovWHalf * Mathf.Deg2Rad);

                Vector3 topLeft = (camtr.forward * camNear - toRight + toTop);
                float camScale = topLeft.magnitude * camFar / camNear;

                topLeft.Normalize();
                topLeft *= camScale;

                Vector3 topRight = (camtr.forward * camNear + toRight + toTop);
                topRight.Normalize();
                topRight *= camScale;

                Vector3 bottomRight = (camtr.forward * camNear + toRight - toTop);
                bottomRight.Normalize();
                bottomRight *= camScale;

                Vector3 bottomLeft = (camtr.forward * camNear - toRight - toTop);
                bottomLeft.Normalize();
                bottomLeft *= camScale;

                frustumCorners.SetRow(0, topLeft);
                frustumCorners.SetRow(1, topRight);
                frustumCorners.SetRow(2, bottomRight);
                frustumCorners.SetRow(3, bottomLeft);

                var camPos = camtr.position;
                float FdotC = camPos.y - _globalFog.height;
                float paramK = (FdotC <= 0.0f ? 1.0f : 0.0f);
                float excludeDepth = (_globalFog.excludeFarPixels ? 1.0f : 2.0f);
                _fogMaterial.SetMatrix("_FrustumCornersWS", frustumCorners);
                _fogMaterial.SetVector("_CameraWS", camPos);
                _fogMaterial.SetVector("_HeightParams", new Vector4(_globalFog.height, FdotC, paramK, _globalFog.heightDensity * 0.5f));
                _fogMaterial.SetVector("_DistanceParams", new Vector4(-Mathf.Max(_globalFog.startDistance, 0.0f), excludeDepth, 0, 0));

                var sceneMode = RenderSettings.fogMode;
                var sceneDensity = RenderSettings.fogDensity;
                var sceneStart = RenderSettings.fogStartDistance;
                var sceneEnd = RenderSettings.fogEndDistance;
                Vector4 sceneParams;
                bool linear = (sceneMode == FogMode.Linear);
                float diff = linear ? sceneEnd - sceneStart : 0.0f;
                float invDiff = Mathf.Abs(diff) > 0.0001f ? 1.0f / diff : 0.0f;
                sceneParams.x = sceneDensity * 1.2011224087f; // density / sqrt(ln(2)), used by Exp2 fog mode
                sceneParams.y = sceneDensity * 1.4426950408f; // density / ln(2), used by Exp fog mode
                sceneParams.z = linear ? -invDiff : 0.0f;
                sceneParams.w = linear ? sceneEnd * invDiff : 0.0f;
                _fogMaterial.SetVector("_SceneFogParams", sceneParams);
                _fogMaterial.SetVector("_SceneFogMode", new Vector4((int)sceneMode, _globalFog.useRadialDistance ? 1 : 0, 0, 0));

                int pass = 0;
                if (_globalFog.distanceFog && _globalFog.heightFog)
                    pass = 0; // distance + height
                else if (_globalFog.distanceFog)
                    pass = 1; // distance only
                else
                    pass = 2; // height only
                CustomGraphicsBlit(source, _intermediateSourceDestination, _fogMaterial, pass);

                source = _intermediateSourceDestination;
                return true;
            }

            public static void Postfix(RenderTexture source, RenderTexture destination)
            {
                if (_intermediateSourceDestination != null)
                    RenderTexture.ReleaseTemporary(_intermediateSourceDestination);
                _intermediateSourceDestination = null;
            }

            static void CustomGraphicsBlit(RenderTexture source, RenderTexture dest, Material fxMaterial, int passNr)
            {
                RenderTexture.active = dest;

                fxMaterial.SetTexture("_MainTex", source);

                GL.PushMatrix();
                GL.LoadOrtho();

                fxMaterial.SetPass(passNr);

                GL.Begin(GL.QUADS);

                GL.MultiTexCoord2(0, 0.0f, 0.0f);
                GL.Vertex3(0.0f, 0.0f, 3.0f); // BL

                GL.MultiTexCoord2(0, 1.0f, 0.0f);
                GL.Vertex3(1.0f, 0.0f, 2.0f); // BR

                GL.MultiTexCoord2(0, 1.0f, 1.0f);
                GL.Vertex3(1.0f, 1.0f, 1.0f); // TR

                GL.MultiTexCoord2(0, 0.0f, 1.0f);
                GL.Vertex3(0.0f, 1.0f, 0.0f); // TL

                GL.End();
                GL.PopMatrix();
            }
        }
        #endregion
    }
}
