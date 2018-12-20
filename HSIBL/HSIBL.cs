using UnityEngine;
using System.Collections;
using UnityStandardAssets.ImageEffects;
using UnityStandardAssets.CinematicEffects;
using UnityEngine.SceneManagement;
using _4KManager;
using Manager;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using Studio;
using IllusionPlugin;
using IllusionUtility.GetUtility;
using UnityEngine.Rendering;
using _4KConfig;
using DepthOfField = UnityStandardAssets.ImageEffects.DepthOfField;

namespace HSIBL
{

    public class HSIBL : MonoBehaviour
    {

        #region Fields
        private readonly ProceduralSkyboxManager _proceduralSkybox = new ProceduralSkyboxManager();
        private readonly SkyboxManager _skybox = new SkyboxManager();
        public GameObject probeGameObject = new GameObject("RealtimeReflectionProbe");
        private ProceduralSkyboxParams _tempproceduralskyboxparams;
        private SkyboxParams _tempskyboxparams;
        private GameObject _lightsObj;
        private Quaternion _lightsObjDefaultRotation;
        private ReflectionProbe _probeComponent;
        private readonly int[] _possibleReflectionProbeResolutions = { 64, 128, 256, 512, 1024, 2048 };
        private string[] _possibleReflectionProbeResolutionsNames;
        private string[] _cubeMapFileNames;
        private int _reflectionProbeResolution;
        private CharFemale _cMfemale;
        public bool cameraCtrlOff;
        private Camera _subCamera;
        private FolderAssist _cubemapFolder;
        private const string _presetFolder = "Plugins\\HSIBL\\Presets\\";

        private static ushort _errorcode = 0;
        private Light _backDirectionalLight;
        private Light _frontDirectionalLight;
        private Light _frontDirectionalMapLight;
        private bool _cubemaploaded = false;
        private bool _hideSkybox = false;

        private bool _tonemappingEnabled = true;
        private TonemappingColorGrading.Tonemapper _toneMapper = TonemappingColorGrading.Tonemapper.ACES;
        private float _ev;
        private float _eyeSpeed;
        private bool _eyeEnabled = false;
        private float _eyeMiddleGrey;
        private float _eyeMin;
        private float _eyeMax;

        private TonemappingColorGrading _toneMappingManager;
        private Bloom _bloomManager;
        private LensAberrations _lensManager;

        private float _previousambientintensity = 1f;

        private bool _environmentUpdateFlag = false;

        public static bool MainWindow = false;

        private float _charaRotate = 0f;
        private Vector3 _rotateValue;
        private bool _autoRotate = false;
        private float _autoRotateSpeed = 0.2f;
        private int _selectedCubeMap;
        private int _previousSelectedCubeMap;
        private readonly int _cmWaringWindowId = 14321;
        private readonly int _errorwindowId = 14322;
        private readonly int _windowId = 14333;
        private int _reflectionProbeRefreshRate;

        private Studio.CameraControl _cameraCtrl;
        private bool _windowdragflag = false;
        private int _tabMenu;
        private bool _frontLightAnchor = true;
        private bool _backLightAnchor = true;
        private Vector3 _frontRotate;
        private Vector3 _backRotate;
        private bool _isLoading = false;

        private SSAOPro _ssao;
        private string[] _possibleSSAOSampleCountNames;
        private string[] _possibleSSAOBlurModeNames;

        private SunShafts _sunShafts;
        private string[] _possibleSunShaftsResolutionNames;
        private string[] _possibleShaftsScreenBlendModeNames;

        private DepthOfField _depthOfField;
        private string[] _possibleBlurSampleCountNames;
        private string[] _possibleBlurTypeNames;
        private Transform _depthOfFieldFocusPoint;

        private ScreenSpaceReflection _ssr;
        private string[] _possibleSSRResolutionNames;
        private string[] _possibleSSRDebugModeNames;

        private SMAA _smaa;
        private string[] _possibleSMAADebugPassNames;
        private string[] _possibleSMAAQualityPresetNames;
        private string[] _possibleSMAAEdgeDetectionMethodNames;

        private ColorCorrectionCurves _colorCorrectionCurves;

        private Quaternion _frontLightDefaultRotation;
        private Quaternion _backLightDefaultRotation;
        private Material _originalSkybox;
        private AmbientMode _originalAmbientMode;
        private DefaultReflectionMode _originalDefaultReflectionMode;
        private string _currentTooltip = "";
        private Vector2 _presetsScroll;
        private string _presetName = "";
        private bool _removePresetMode;
        private string[] _presets = new string[0];
        #endregion

        #region Accessors
        private readonly Func<float> _getWindowHeight = () => ModPrefs.GetFloat("HSIBL","Window.height", 1000);
        private readonly Func<float> _getWindowWidth = () => ModPrefs.GetFloat("HSIBL","Window.width", 1000);
        #endregion

        #region Unity Methods
        private void Awake()
        {
            if (!Camera.main.hdr)
            {
                _errorcode |= 1;
                Camera.main.hdr = true;
            }
            if(!GameObject.Find("4KManager"))
            {
                _errorcode |= 16;
            }
            if (_errorcode > 0)
            {
                Console.WriteLine("HSIBL is Loaded with error code:"+ _errorcode.ToString());
            }
            else
            {
                Console.WriteLine("HSIBL is Loaded");
            }
            Console.WriteLine("----------------");

            this._lightsObj = GameObject.Find("Lights");
            if (this._lightsObj != null)
                this._lightsObjDefaultRotation = this._lightsObj.transform.localRotation;
            this._probeComponent = this.probeGameObject.AddComponent<ReflectionProbe>();
            this._probeComponent.mode = ReflectionProbeMode.Realtime;
            this._probeComponent.resolution = 512;
            this._probeComponent.hdr = true;
            this._probeComponent.intensity = 1f;
            this._probeComponent.type = ReflectionProbeType.Cube;
            this._probeComponent.clearFlags = ReflectionProbeClearFlags.Skybox;
            this._probeComponent.size = new Vector3(1000, 1000, 1000);
            this.probeGameObject.transform.position = new Vector3(0, 2, 0);
            this._probeComponent.refreshMode = ReflectionProbeRefreshMode.ViaScripting;
            this._probeComponent.timeSlicingMode = ReflectionProbeTimeSlicingMode.AllFacesAtOnce;
            this._possibleReflectionProbeResolutionsNames = this._possibleReflectionProbeResolutions.Select(e => e.ToString()).ToArray();
            this._possibleSSAOSampleCountNames = Enum.GetNames(typeof(SSAOPro.SampleCount));
            this._possibleSSAOBlurModeNames = Enum.GetNames(typeof(SSAOPro.BlurMode));
            this._possibleSunShaftsResolutionNames = Enum.GetNames(typeof(SunShafts.SunShaftsResolution));
            this._possibleShaftsScreenBlendModeNames = Enum.GetNames(typeof(SunShafts.ShaftsScreenBlendMode));
            this._possibleBlurSampleCountNames = Enum.GetNames(typeof(DepthOfField.BlurSampleCount));
            this._possibleBlurTypeNames = Enum.GetNames(typeof(DepthOfField.BlurType));
            this._possibleSSRResolutionNames = Enum.GetNames(typeof(ScreenSpaceReflection.SSRResolution));
            this._possibleSSRDebugModeNames = Enum.GetNames(typeof(ScreenSpaceReflection.SSRDebugMode));
            this._possibleSMAADebugPassNames = Enum.GetNames(typeof(SMAA.DebugPass));
            this._possibleSMAAQualityPresetNames = Enum.GetNames(typeof(SMAA.QualityPreset));
            this._possibleSMAAEdgeDetectionMethodNames = Enum.GetNames(typeof(SMAA.EdgeDetectionMethod));
            if (Application.productName =="StudioNEO")
            {
                HSExtSave.HSExtSave.RegisterHandler("hsibl", null, null, this.OnSceneLoad, null, this.OnSceneSave, null, null);
            }
        }

        private IEnumerator Start()
        {
            yield return null;
            yield return null;
            yield return null;
            this._originalSkybox = RenderSettings.skybox;
            this._originalAmbientMode = RenderSettings.ambientMode;
            this._originalDefaultReflectionMode = RenderSettings.defaultReflectionMode;
        }

        private void OnEnable()
        {
            if (!this._proceduralSkybox.Proceduralsky)
            {
                this._proceduralSkybox.Init();
            }
            this._tempproceduralskyboxparams = this._proceduralSkybox.skyboxparams;
            this.StopAllCoroutines();
            this.StartCoroutine(this.UpdateEnvironment());
            if (Application.productName =="StudioNEO")
            {
                this._subCamera = GameObject.Find("Camera").GetComponent<Camera>();
                this._subCamera.fieldOfView = Camera.main.fieldOfView;
                UIUtils.windowRect = new Rect(Screen.width * 0.7f, Screen.height * 0.65f, Screen.width * 0.34f, Screen.height * 0.45f);
            }
            else if(SceneManager.GetActiveScene().buildIndex == 21 || SceneManager.GetActiveScene().buildIndex == 22)
            {

                UIUtils.windowRect = new Rect(Screen.width * 0.22f, Screen.height * 0.64f, Screen.width * 0.34f, Screen.height * 0.45f);
                this._cMfemale = Singleton<Character>.Instance.GetFemale(0);
                if (this._cMfemale)
                {
                    this._rotateValue = this._cMfemale.GetRotation();
                    this.StartCoroutine(this.RotateCharater());
                }
            }
            else
            {
                UIUtils.windowRect = new Rect(0f, Screen.height * 0.3f, Screen.width * 0.35f, Screen.height * 0.45f);
            }

            this._frontDirectionalLight = Camera.main.transform.FindLoop("DirectionalFront").GetComponent<Light>();
            this._backDirectionalLight = Camera.main.transform.FindLoop("DirectionalBack").GetComponent<Light>();
            this._frontLightDefaultRotation = this._frontDirectionalLight.transform.localRotation;
            this._backLightDefaultRotation = this._backDirectionalLight.transform.localRotation;
            GameObject mapLight;
            if ((mapLight = GameObject.Find("DirectionalFrontMap")) != null)
                this._frontDirectionalMapLight = mapLight.GetComponent<Light>();

            this._cubemapFolder = new FolderAssist();
            this._cubemapFolder.CreateFolderInfo(Application.dataPath +"/../abdata/plastic/cubemaps/","*.unity3d", true, true);
            this._selectedCubeMap = -1;
            this._previousSelectedCubeMap = -1;
            this._cubeMapFileNames = new string[this._cubemapFolder.lstFile.Count+1];
            this._cubeMapFileNames[0] ="Procedural";
            int count = 1;
            foreach (FolderAssist.FileInfo fileInfo in this._cubemapFolder.lstFile)
            {
                this._cubeMapFileNames[count] = fileInfo.FileName;
                count++;
            }
            this._colorCorrectionCurves = Camera.main.GetComponent<ColorCorrectionCurves>();
            if (this._colorCorrectionCurves != null)
            {
                this._smaa = (SMAA)this._colorCorrectionCurves.GetType().GetField("m_SMAA", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(this._colorCorrectionCurves);
                this._toneMappingManager = this._colorCorrectionCurves.Tonemapping;
                this._bloomManager = this._colorCorrectionCurves.CinematicBloom;
                this._lensManager = this._colorCorrectionCurves.LensAberrations;
                this._ev = Mathf.Log(this._toneMappingManager.tonemapping.exposure, 2f);
            }
            this._ssao = Camera.main.GetComponent<SSAOPro>();
            this._sunShafts = Camera.main.GetComponent<SunShafts>();
            this._depthOfField = Camera.main.GetComponent<DepthOfField>();
            if (this._depthOfField != null)
                this._depthOfFieldFocusPoint = this._depthOfField.focalTransform;
            this._ssr = Camera.main.GetComponent<ScreenSpaceReflection>();

            this.RefreshPresetList();

        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F5))
            {
                MainWindow = !MainWindow;
            }

            if (MainWindow)
            {
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    MainWindow = false;
                }
            }
            if (this._selectedCubeMap == 0)
            {
                if (Mathf.Approximately(this._previousambientintensity, RenderSettings.ambientIntensity) && (this._tempproceduralskyboxparams).Equals(this._proceduralSkybox.skyboxparams))
                {
                    return;
                }
                this._proceduralSkybox.ApplySkyboxParams();
                this._environmentUpdateFlag = true;
                this._tempproceduralskyboxparams = this._proceduralSkybox.skyboxparams;
                this._previousambientintensity = RenderSettings.ambientIntensity;
            }
            else if (this._selectedCubeMap > 0)
            {
                if (Mathf.Approximately(this._previousambientintensity, RenderSettings.ambientIntensity) && (this._tempskyboxparams).Equals(this._skybox.skyboxparams))
                {
                    return;
                }
                this._skybox.ApplySkyboxParams();
                this._environmentUpdateFlag = true;
                this._tempskyboxparams = this._skybox.skyboxparams;
                this._previousambientintensity = RenderSettings.ambientIntensity;
            }
        }

        private void OnGUI()
        {
            if (!MainWindow)
            {
                return;
            }
            if (!UIUtils.styleInitialized)
            {
                UIUtils.InitStyle();
            }
            if (!Camera.main.hdr)
            {

                if (Camera.main.actualRenderingPath != RenderingPath.DeferredShading)
                {
                    UIUtils.CMWarningRect = GUILayout.Window(this._cmWaringWindowId, UIUtils.CMWarningRect, this.CharaMakerWarningWindow,"Warning", UIUtils.windowstyle);
                    return;
                }
                Console.WriteLine("HSIBL Warning: HDR is somehow been disabled! Trying to re-enable it...");
                Camera.main.hdr = true;
                if (!Camera.main.hdr)
                {
                    Console.WriteLine("HSIBL Error: Failed to enable HDR");
                    MainWindow = false;
                    return;
                }
                Console.WriteLine("HSIBL Info: Done!");
            }

            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, UIUtils.scale);

            if (_errorcode > 1)
            {
                UIUtils.ErrorwindowRect = GUILayout.Window(this._errorwindowId, UIUtils.ErrorwindowRect, this.ErrorWindow,"", UIUtils.windowstyle);
                return;
            }
            if (Event.current.type == EventType.MouseDown || Event.current.type == EventType.MouseUp)
            {
                this.cameraCtrlOff = false;
                this._windowdragflag = false;
            }
            UIUtils.windowRect = UIUtils.LimitWindowRect(UIUtils.windowRect);
            UIUtils.windowRect = GUILayout.Window(this._windowId, UIUtils.windowRect, this.HSIBLWindow,"", UIUtils.windowstyle);
            if (this._currentTooltip.Length != 0)
            {
                Rect tooltipRect = new Rect(new Vector2(UIUtils.windowRect.xMin, UIUtils.windowRect.yMax), new Vector2(UIUtils.windowRect.width, UIUtils.labelstyle.CalcHeight(new GUIContent(this._currentTooltip), UIUtils.windowRect.width) + 10));
                GUI.Box(tooltipRect, "");
                GUI.Box(tooltipRect, "");
                GUI.Label(tooltipRect, this._currentTooltip, UIUtils.labelstyle);
            }
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one);
        }
        #endregion

        #region Public Methods
        public void CameraControlOffOnGUI()
        {
            switch (Event.current.type)
            {
                case EventType.MouseDown:
                case EventType.MouseDrag:
                    this.cameraCtrlOff = true;
                    break;
                default:
                    return;
            }

        }
        #endregion

        #region UI
        private void HSIBLWindow(int id)
        {
            this.CameraControlOffOnGUI();
            if (Event.current.type == EventType.MouseDown)
            {
                GUI.FocusWindow(this._windowId);
                this._windowdragflag = true;
                this.cameraCtrlOff = true;
            }
            else if (Event.current.type == EventType.MouseUp)
            {
                this._windowdragflag = false;
                this.cameraCtrlOff = false;
            }
            if (this._windowdragflag && Event.current.type == EventType.MouseDrag)
            {
                this.cameraCtrlOff = true;
            }

            GUILayout.BeginHorizontal();
            using (var verticalScope = new GUILayout.VerticalScope("box", GUILayout.MaxWidth(UIUtils.windowRect.width * 0.33f)))
            {
                //////////////////Load cubemaps/////////////// 
                GUI.enabled = !this._isLoading;
                this.CubeMapModule();
                GUI.enabled = true;

            }
            GUILayout.Space(1f);
            GUILayout.BeginVertical();
            this._tabMenu = GUILayout.Toolbar(this._tabMenu, GUIStrings.titlebar, UIUtils.titlestyle);
            UIUtils.scrollPosition[this._tabMenu + 1] = GUILayout.BeginScrollView(UIUtils.scrollPosition[this._tabMenu + 1]);
            using (var verticalScope = new GUILayout.VerticalScope("box", GUILayout.MaxHeight(Screen.height * 0.8f)))
            {

                GUILayout.Space(UIUtils.space);
                if (this._tabMenu == 0)
                {
                    ////////////////////Lighting tweak/////////////////////

                    if (this._selectedCubeMap == 0)
                        this.ProceduralSkyboxModule();
                    else
                        this.SkyboxModule();
                    UIUtils.HorizontalLine();
                    this.ReflectionModule();
                    UIUtils.HorizontalLine();
                    this.DefaultLightModule();

                }

                else if (this._tabMenu == 1)
                {
                    if (this._colorCorrectionCurves != null)
                    {
                        this.LensPresetsModule();
                        this.LensModule();
                    }
                }
                else if (this._tabMenu == 2)
                {
                    GUILayout.Label("Effects are displayed in the same order they are applied.", UIUtils.labelstyle);
                    UIUtils.HorizontalLine();
                    if (this._ssao != null)
                    {
                        this.SSAOModule();
                        UIUtils.HorizontalLine();
                    }
                    if (this._ssr != null)
                    {
                        this.SSRModule();
                        UIUtils.HorizontalLine();
                    }
                    if (this._depthOfField != null)
                    {
                        this.DepthOfFieldModule();
                        UIUtils.HorizontalLine();
                    }
                    if (this._sunShafts != null)
                    {
                        this.SunShaftsModule();
                        UIUtils.HorizontalLine();
                    }
                    if (this._colorCorrectionCurves != null)
                    {
                        this.SMAAModule();
                        UIUtils.HorizontalLine();
                        this.BloomModule();
                        UIUtils.HorizontalLine();
                        this.EyeAdaptationModule();
                        UIUtils.HorizontalLine();
                        this.ToneMappingModule();
                        UIUtils.HorizontalLine();
                        this.ColorGradingModule();
                    }
                }
                else if (this._tabMenu == 3)
                {
                    this.CharaRotateModule();
                    this.UserCustomModule();
                }
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Auto Mode", UIUtils.buttonstyleNoStretch))
            {
                this.OptimalSetting(true);
            }

            if (GUILayout.Button("Manual Mode", UIUtils.buttonstyleNoStretch))
            {
                this.OptimalSetting(false);
            }
            GUILayout.EndHorizontal();
            GUI.DragWindow();
            if (Event.current.type == EventType.repaint)
                this._currentTooltip = GUI.tooltip;
        }

        private void CubeMapModule()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Load Cubemaps:", UIUtils.labelstyle);
            GUILayout.FlexibleSpace();
            this._hideSkybox = UIUtils.ToggleButton(this._hideSkybox, new GUIContent("Hide","Hide skybox in the background"));
            GUILayout.EndHorizontal();
            if (this._hideSkybox)
            {
                Camera.main.clearFlags = CameraClearFlags.SolidColor;
            }
            else if (this._cubemaploaded)
            {
                Camera.main.clearFlags = CameraClearFlags.Skybox;
            }
            UIUtils.scrollPosition[0] = GUILayout.BeginScrollView(UIUtils.scrollPosition[0]);

            if (GUILayout.Button("None", UIUtils.buttonstyleStrechWidth))
            {
                this._skybox.Skybox = this._originalSkybox;
                RenderSettings.skybox = this._originalSkybox;
                RenderSettings.ambientMode = this._originalAmbientMode;
                RenderSettings.defaultReflectionMode = this._originalDefaultReflectionMode;
                Camera.main.clearFlags = CameraClearFlags.SolidColor;
                this._cubemaploaded = false;
                this._selectedCubeMap = -1;
                this._previousSelectedCubeMap = -1;
            }

            this._selectedCubeMap = GUILayout.SelectionGrid(this._selectedCubeMap, this._cubeMapFileNames, 1, UIUtils.buttonstyleStrechWidth);

            if (this._selectedCubeMap > 0 && this._previousSelectedCubeMap != this._selectedCubeMap)
            {
                this.StartCoroutine(this.LoadCubemapAsync(this._cubemapFolder.lstFile[this._selectedCubeMap - 1].FileName));
                this._previousSelectedCubeMap = this._selectedCubeMap;
            }
            if (this._selectedCubeMap == 0 && this._previousSelectedCubeMap != 0)
            {
                this._proceduralSkybox.ApplySkybox();
                this._proceduralSkybox.ApplySkyboxParams();
                this._environmentUpdateFlag = true;
                this._previousSelectedCubeMap = 0;
                this._cubemaploaded = true;
            }

            GUILayout.EndScrollView();
        }

        private void ProceduralSkyboxModule()
        {
            GUILayout.Label("Procedural Skybox", UIUtils.titlestyle2);
            GUILayout.Space(UIUtils.space);
            this._proceduralSkybox.skyboxparams.exposure = UIUtils.SliderGUI(this._proceduralSkybox.skyboxparams.exposure, 0f, 8f, 1f,"Skybox Exposure:","","N3");
            this._proceduralSkybox.skyboxparams.sunsize = UIUtils.SliderGUI(this._proceduralSkybox.skyboxparams.sunsize, 0f, 1f, 0.1f,"Sun Size :","","N3");
            this._proceduralSkybox.skyboxparams.atmospherethickness = UIUtils.SliderGUI(this._proceduralSkybox.skyboxparams.atmospherethickness, 0f, 5f, 1f,"Atmosphere Thickness:","","N3");
            UIUtils.ColorPickerGUI(this._proceduralSkybox.skyboxparams.skytint, Color.gray,"Sky Tint:","", (c) =>
            {
                this._proceduralSkybox.skyboxparams.skytint = c;
            });
            UIUtils.ColorPickerGUI(this._proceduralSkybox.skyboxparams.groundcolor, Color.gray,"Gound Color:","", (c) =>
            {
                this._proceduralSkybox.skyboxparams.groundcolor = c;
            });
            RenderSettings.ambientIntensity = UIUtils.SliderGUI(RenderSettings.ambientIntensity, 0f, 2f, 1f,"Ambient Intensity:","","N3");
        }

        private void SkyboxModule()
        {
            GUILayout.Label("Skybox", UIUtils.titlestyle2);
            GUILayout.Space(UIUtils.space);
            this._skybox.skyboxparams.rotation = UIUtils.SliderGUI(this._skybox.skyboxparams.rotation, 0f, 360f, 0f,"Skybox Rotation:","","N2");
            this._skybox.skyboxparams.exposure = UIUtils.SliderGUI(this._skybox.skyboxparams.exposure, 0f, 8f, 1f,"Skybox Exposure:","","N3");
            UIUtils.ColorPickerGUI(this._skybox.skyboxparams.tint, Color.gray,"Skybox Tint:","", c =>
            {
                this._skybox.skyboxparams.tint = c;
            });
            RenderSettings.ambientIntensity = UIUtils.SliderGUI(RenderSettings.ambientIntensity, 0f, 2f, 1f,"Ambient Intensity:","","N3");
        }

        private void ReflectionModule()
        {
            this._probeComponent.enabled = UIUtils.ToggleGUI(this._probeComponent.enabled, new GUIContent(GUIStrings.reflection), GUIStrings.disableVsEnable, UIUtils.titlestyle2);

            if (this._probeComponent.enabled)
            {
                GUILayout.Space(UIUtils.space);

                if (this._probeComponent.refreshMode == ReflectionProbeRefreshMode.ViaScripting)
                {
                    if (GUILayout.Button(GUIStrings.reflectionProbeRefresh, UIUtils.buttonstyleNoStretch))
                    {
                        this._probeComponent.RenderProbe();
                    }
                }

                if (this._probeComponent.refreshMode == ReflectionProbeRefreshMode.ViaScripting)
                {
                    this._reflectionProbeRefreshRate = 0;
                }
                else if (this._probeComponent.timeSlicingMode == ReflectionProbeTimeSlicingMode.AllFacesAtOnce)
                {
                    this._reflectionProbeRefreshRate = 2;
                }
                else if (this._probeComponent.timeSlicingMode == ReflectionProbeTimeSlicingMode.IndividualFaces)
                {
                    this._reflectionProbeRefreshRate = 1;
                }
                else if (this._probeComponent.timeSlicingMode == ReflectionProbeTimeSlicingMode.NoTimeSlicing)
                {
                    this._reflectionProbeRefreshRate = 3;
                }
                this._reflectionProbeRefreshRate = GUILayout.SelectionGrid(this._reflectionProbeRefreshRate, GUIStrings.reflectionProbeRefreshRateArray, 4, UIUtils.selectstyle);

                switch (this._reflectionProbeRefreshRate)
                {
                    default:
                    case 0:
                        this._probeComponent.refreshMode = ReflectionProbeRefreshMode.ViaScripting;
                        break;
                    case 1:
                        this._probeComponent.refreshMode = ReflectionProbeRefreshMode.EveryFrame;
                        this._probeComponent.timeSlicingMode = ReflectionProbeTimeSlicingMode.IndividualFaces;
                        break;
                    case 2:
                        this._probeComponent.refreshMode = ReflectionProbeRefreshMode.EveryFrame;
                        this._probeComponent.timeSlicingMode = ReflectionProbeTimeSlicingMode.AllFacesAtOnce;
                        break;
                    case 3:
                        this._probeComponent.refreshMode = ReflectionProbeRefreshMode.EveryFrame;
                        this._probeComponent.timeSlicingMode = ReflectionProbeTimeSlicingMode.NoTimeSlicing;
                        break;
                }

                GUILayout.Label(GUIStrings.reflectionProbeResolution, UIUtils.labelstyle);
                this._reflectionProbeResolution = 0;
                for (int i = 0; i < this._possibleReflectionProbeResolutions.Length; i++)
                {
                    if (this._possibleReflectionProbeResolutions[i] == this._probeComponent.resolution)
                    {
                        this._reflectionProbeResolution = i;
                        break;
                    }
                }

                this._reflectionProbeResolution = GUILayout.SelectionGrid(this._reflectionProbeResolution, this._possibleReflectionProbeResolutionsNames, this._possibleReflectionProbeResolutions.Length, UIUtils.selectstyle);

                this._probeComponent.resolution = this._possibleReflectionProbeResolutions[this._reflectionProbeResolution];

                if (Application.productName =="StudioNEO")
                {
                    if (GUILayout.Button("Move Reflection Probe to target", UIUtils.buttonstyleNoStretch))
                    {
                        this.probeGameObject.transform.position = this._cameraCtrl.targetTex.position;
                        if (this._probeComponent.refreshMode == ReflectionProbeRefreshMode.ViaScripting)
                        {
                            this._probeComponent.RenderProbe();
                        }
                    }
                }
                GUILayout.Space(UIUtils.space);
                this._probeComponent.intensity = UIUtils.SliderGUI(this._probeComponent.intensity, 0f, 2f, 1f, GUIStrings.reflectionIntensity,"N3");
                RenderSettings.reflectionBounces = Mathf.RoundToInt(UIUtils.SliderGUI(RenderSettings.reflectionBounces, 1, 5, 1, "Reflection Bounces", "The number of times a reflection includes other reflections. If set to 1, the scene will be rendered once, which means that a reflection will not be able to reflect another reflection and reflective objects will show up black, when seen in other reflective surfaces. If set to 2, the scene will be rendered twice and reflective objects will show reflections from the first pass, when seen in other reflective surfaces.", "0"));
            }
        }

        private void DefaultLightModule()
        {
            GUILayout.Label("Directional Light", UIUtils.titlestyle2);
            GUILayout.Space(UIUtils.space);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Front Light", UIUtils.labelstyle);
            GUILayout.FlexibleSpace();
            bool lastFrontLightAnchor = this._frontDirectionalLight.transform.parent != null;
            this._frontLightAnchor = UIUtils.ToggleButton(lastFrontLightAnchor, new GUIContent("Rotate with camera"));
            GUILayout.EndHorizontal();

            if (this._frontLightAnchor)
            {
                this._lightsObj.transform.parent = Camera.main.transform;
                this._frontDirectionalLight.transform.parent = this._lightsObj.transform;
                if (this._frontLightAnchor != lastFrontLightAnchor)
                {
                    this._lightsObj.transform.localRotation = Studio.Studio.Instance != null ? Quaternion.Euler(Studio.Studio.Instance.sceneInfo.cameraLightRot[0], Studio.Studio.Instance.sceneInfo.cameraLightRot[1], 0f) : this._lightsObjDefaultRotation;
                    this._frontDirectionalLight.transform.localRotation = this._frontLightDefaultRotation;
                }
            }
            else
            {
                this._lightsObj.transform.parent = null;
                this._frontDirectionalLight.transform.parent = null;
                this._frontRotate.x = UIUtils.SliderGUI(this._frontRotate.x, 0f, 360f, 0f, "Vertical rotation", "N1");
                this._frontRotate.y = UIUtils.SliderGUI(this._frontRotate.y, 0f, 360f, 0f, "Horizontal rotation", "N1");
                this._frontDirectionalLight.transform.eulerAngles = this._frontRotate;
            }
            this._frontDirectionalLight.intensity = UIUtils.SliderGUI(this._frontDirectionalLight.intensity, 0f, 8f, 1f,"Intensity:","N3");
            UIUtils.ColorPickerGUI(this._frontDirectionalLight.color, Color.white,"Color:", c =>
            {
                this._frontDirectionalLight.color = c;
            });

            GUILayout.BeginHorizontal();
            GUILayout.Label("Back Light", UIUtils.labelstyle);
            GUILayout.FlexibleSpace();
            bool lastBackLightAnchor = this._backDirectionalLight.transform.parent != null;
            this._backLightAnchor = UIUtils.ToggleButton(lastBackLightAnchor, new GUIContent("Rotate with camera"));
            GUILayout.EndHorizontal();
            if (this._backLightAnchor)
            {
                this._backDirectionalLight.transform.parent = Camera.main.transform;
                if (this._backLightAnchor != lastBackLightAnchor)
                    this._backDirectionalLight.transform.localRotation = this._backLightDefaultRotation;
            }
            else
            {
                this._backDirectionalLight.transform.parent = null;
                this._backRotate.x = UIUtils.SliderGUI(this._backRotate.x, 0f, 360f, 0f, "Vertical rotation", "", "N1");
                this._backRotate.y = UIUtils.SliderGUI(this._backRotate.y, 0f, 360f, 0f, "Horizontal rotation", "", "N1");
                this._backDirectionalLight.transform.eulerAngles = this._backRotate;
            }
            this._backDirectionalLight.intensity = UIUtils.SliderGUI(this._backDirectionalLight.intensity, 0f, 8f, 1f,"Intensity:","","N3");
            UIUtils.ColorPickerGUI(this._backDirectionalLight.color, Color.white, GUIStrings.color,"", c =>
            {
                this._backDirectionalLight.color = c;
            });
        }

        private void LensPresetsModule()
        {
            GUILayout.Label("Lens Presets", UIUtils.labelstyle);
            GUILayout.Space(UIUtils.space);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("85mm", UIUtils.buttonstyleStrechWidth))
            {
                Camera.main.fieldOfView = 23.9f;
                if (this._subCamera != null)
                {
                    this._subCamera.fieldOfView = Camera.main.fieldOfView;
                }
                this._lensManager.distortion.enabled = false;
                this._lensManager.vignette.enabled = true;
                this._lensManager.vignette.intensity = 0.7f;
                this._lensManager.vignette.color = Color.black;
                this._lensManager.vignette.blur = 0f;
                this._lensManager.vignette.desaturate = 0f;
                this._lensManager.vignette.roundness = 0.5625f;
                this._lensManager.vignette.smoothness = 2f;
            }
            if (GUILayout.Button("50mm", UIUtils.buttonstyleStrechWidth))
            {
                Camera.main.fieldOfView = 39.6f;
                if (this._subCamera != null)
                {
                    this._subCamera.fieldOfView = Camera.main.fieldOfView;
                }
                this._lensManager.distortion.enabled = false;
                this._lensManager.vignette.enabled = true;
                this._lensManager.vignette.intensity = 1f;
                this._lensManager.vignette.color = Color.black;
                this._lensManager.vignette.blur = 0f;
                this._lensManager.vignette.desaturate = 0f;
                this._lensManager.vignette.roundness = 0.5625f;
                this._lensManager.vignette.smoothness = 2f;
            }
            if (GUILayout.Button("35mm", UIUtils.buttonstyleStrechWidth))
            {
                Camera.main.fieldOfView = 57.9f;
                if (this._subCamera != null)
                {
                    this._subCamera.fieldOfView = Camera.main.fieldOfView;
                }
                this._lensManager.distortion.enabled = false;
                this._lensManager.vignette.enabled = true;
                this._lensManager.vignette.intensity = 1.6f;
                this._lensManager.vignette.color = Color.black;
                this._lensManager.vignette.blur = 0f;
                this._lensManager.vignette.desaturate = 0f;
                this._lensManager.vignette.roundness = 0.5625f;
                this._lensManager.vignette.smoothness = 1.6f;
            }
            if (GUILayout.Button("24mm", UIUtils.buttonstyleStrechWidth))
            {
                Camera.main.fieldOfView = 85.5f;
                if (this._subCamera != null)
                {
                    this._subCamera.fieldOfView = Camera.main.fieldOfView;
                }
                this._lensManager.distortion.enabled = true;
                this._lensManager.distortion.amount = 25f;
                this._lensManager.distortion.amountX = 1f;
                this._lensManager.distortion.amountY = 1f;
                this._lensManager.distortion.scale = 1.025f;
                this._lensManager.vignette.enabled = true;
                this._lensManager.vignette.intensity = 1.8f;
                this._lensManager.vignette.color = Color.black;
                this._lensManager.vignette.blur = 0.1f;
                this._lensManager.vignette.desaturate = 0f;
                this._lensManager.vignette.roundness = 0.187f;
                this._lensManager.vignette.smoothness = 1.4f;
            }
            if (GUILayout.Button("16mm", UIUtils.buttonstyleStrechWidth))
            {
                Camera.main.fieldOfView = 132.6f;
                if (this._subCamera != null)
                {
                    this._subCamera.fieldOfView = Camera.main.fieldOfView;
                }
                this._lensManager.distortion.enabled = true;
                this._lensManager.distortion.amount = 69f;
                this._lensManager.distortion.amountX = 1f;
                this._lensManager.distortion.amountY = 1f;
                this._lensManager.distortion.scale = 1.05f;
                this._lensManager.vignette.enabled = true;
                this._lensManager.vignette.intensity = 1.95f;
                this._lensManager.vignette.color = Color.black;
                this._lensManager.vignette.blur = 0.14f;
                this._lensManager.vignette.desaturate = 0.14f;
                this._lensManager.vignette.roundness = 0.814f;
                this._lensManager.vignette.smoothness = 1.143f;
            }
            GUILayout.EndHorizontal();
        }

        private void LensModule()
        {
            //////////////////////Field of View/////////////////////
            Camera.main.fieldOfView = UIUtils.SliderGUI(Camera.main.fieldOfView, 1f, 179f,"Field of View","N1");
            if (this._subCamera != null)
            {
                this._subCamera.fieldOfView = Camera.main.fieldOfView;
            }
            //GUILayout.Space(UIUtils.space);
            //////////////////////Lens Aberration/////////////////

            UIUtils.HorizontalLine();
            this._lensManager.distortion.enabled = UIUtils.ToggleGUI(this._lensManager.distortion.enabled, new GUIContent("Distortion"), GUIStrings.disableVsEnable, UIUtils.titlestyle2);
            if (this._lensManager.distortion.enabled)
            {
                this._lensManager.distortion.amount = UIUtils.SliderGUI(this._lensManager.distortion.amount, -100f, 100f, 0f,"Distortion Amount","N2");
                this._lensManager.distortion.amountX = UIUtils.SliderGUI(this._lensManager.distortion.amountX, 0f, 1f, 1f,"Amount multiplier on X axis","N3");
                this._lensManager.distortion.amountY = UIUtils.SliderGUI(this._lensManager.distortion.amountY, 0f, 1f, 1f,"Amount multiplier on Y axis","N3");
                this._lensManager.distortion.scale = UIUtils.SliderGUI(this._lensManager.distortion.scale, 0.5f, 1f, 2f,"Global screen scale","N3");
            }
            //GUILayout.Space(UIUtils.space);
            UIUtils.HorizontalLine();

            this._lensManager.chromaticAberration.enabled = UIUtils.ToggleGUI(this._lensManager.chromaticAberration.enabled, new GUIContent(GUIStrings.chromaticAberration), GUIStrings.disableVsEnable, UIUtils.titlestyle2);
            if (this._lensManager.chromaticAberration.enabled)
            {
                this._lensManager.chromaticAberration.amount = UIUtils.SliderGUI(this._lensManager.chromaticAberration.amount, -4f, 4f, 0f,"Tangential distortion Amount","N3");
                UIUtils.ColorPickerGUI(this._lensManager.chromaticAberration.color, Color.green, "Color", (c) =>
                {
                    this._lensManager.chromaticAberration.color = c;
                });
            }
            //GUILayout.Space(UIUtils.space);
            UIUtils.HorizontalLine();
            this._lensManager.vignette.enabled = UIUtils.ToggleGUI(this._lensManager.vignette.enabled, new GUIContent(GUIStrings.vignette), GUIStrings.disableVsEnable, UIUtils.titlestyle2);
            if (this._lensManager.vignette.enabled)
            {
                this._lensManager.vignette.intensity = UIUtils.SliderGUI(this._lensManager.vignette.intensity, 0f, 3f, 1.4f,"Intensity","N3");
                this._lensManager.vignette.smoothness = UIUtils.SliderGUI(this._lensManager.vignette.smoothness, 0.01f, 3f, 0.8f,"Smothness","N3");
                this._lensManager.vignette.roundness = UIUtils.SliderGUI(this._lensManager.vignette.roundness, 0f, 1f, 1f,"Roundness","N3");
                this._lensManager.vignette.desaturate = UIUtils.SliderGUI(this._lensManager.vignette.desaturate, 0f, 1f, 0f,"Desaturate","N3");
                this._lensManager.vignette.blur = UIUtils.SliderGUI(this._lensManager.vignette.blur, 0f, 1f, 0f,"Blur Corner","N3");

                UIUtils.ColorPickerGUI(this._lensManager.vignette.color, Color.black, GUIStrings.vignetteColor, (c) =>
                {
                    this._lensManager.vignette.color = c;
                });
            }
        }

        private void ToneMappingModule()
        {
            this._tonemappingEnabled = UIUtils.ToggleGUI(this._toneMappingManager.tonemapping.enabled, GUIStrings.tonemapping, GUIStrings.disableVsEnable, UIUtils.titlestyle2);

            if (this._tonemappingEnabled)
            {
                GUILayout.Space(UIUtils.space);
                int index = 0;
                switch (this._toneMappingManager.tonemapping.tonemapper)
                {
                    default:
                    case TonemappingColorGrading.Tonemapper.ACES:
                        index = 0;
                        break;
                    case TonemappingColorGrading.Tonemapper.Hable:
                        index = 1;
                        break;
                    case TonemappingColorGrading.Tonemapper.HejlDawson:
                        index = 2;
                        break;
                    case TonemappingColorGrading.Tonemapper.Photographic:
                        index = 3;
                        break;
                    case TonemappingColorGrading.Tonemapper.Reinhard:
                        index = 4;
                        break;
                }
                GUILayout.Space(5f);
                index = GUILayout.SelectionGrid(index, new string[]
                {
                    "ACES",
                    "Hable",
                    "HejlDawson",
                    "Photographic",
                    "Reinhard"
                }, 3, UIUtils.selectstyle);

                switch (index)
                {
                    default:
                    case 0:
                        this._toneMapper = TonemappingColorGrading.Tonemapper.ACES;
                        break;
                    case 1:
                        this._toneMapper = TonemappingColorGrading.Tonemapper.Hable;
                        break;
                    case 2:
                        this._toneMapper = TonemappingColorGrading.Tonemapper.HejlDawson;
                        break;
                    case 3:
                        this._toneMapper = TonemappingColorGrading.Tonemapper.Photographic;
                        break;
                    case 4:
                        this._toneMapper = TonemappingColorGrading.Tonemapper.Reinhard;
                        break;
                }
                this._ev = UIUtils.SliderGUI(this._ev, -5f, 5f, 0f, new GUIContent(GUIStrings.exposureValue, "Adjusts the overall exposure of the scene."), "N3");
            }

            this._toneMappingManager.tonemapping = new TonemappingColorGrading.TonemappingSettings
            {
                tonemapper = this._toneMapper,
                exposure = Mathf.Pow(2f, this._ev),
                enabled = this._tonemappingEnabled,
                neutralBlackIn = this._toneMappingManager.tonemapping.neutralBlackIn,
                neutralBlackOut = this._toneMappingManager.tonemapping.neutralBlackOut,
                neutralWhiteClip = this._toneMappingManager.tonemapping.neutralWhiteClip,
                neutralWhiteIn = this._toneMappingManager.tonemapping.neutralWhiteIn,
                neutralWhiteLevel = this._toneMappingManager.tonemapping.neutralWhiteLevel,
                neutralWhiteOut = this._toneMappingManager.tonemapping.neutralWhiteOut,
                curve = this._toneMappingManager.tonemapping.curve
            };
        }

        private void ColorGradingModule()
        {
            TonemappingColorGrading.ColorGradingSettings colorGrading = this._toneMappingManager.colorGrading;
            colorGrading.enabled = UIUtils.ToggleGUI(colorGrading.enabled, new GUIContent("Color Grading", "Color Grading is the process of altering or correcting the color and luminance of the final image."), GUIStrings.disableVsEnable, UIUtils.titlestyle2);

            if (colorGrading.enabled)
            {
                TonemappingColorGrading.BasicsSettings settings = colorGrading.basics;

                settings.temperatureShift = UIUtils.SliderGUI(settings.temperatureShift, -2f, 2f, () => GraphicSetting.ToneMapSettings.temperatureShift, GUIStrings.tonemappingTemperatureShift, "Sets the white balance to a custom color temperature.", "0.00");
                settings.tint = UIUtils.SliderGUI(settings.tint, -2f, 2f, () => GraphicSetting.ToneMapSettings.tint, GUIStrings.tonemappingTint, "Sets the white balance to compensate for a green or magenta tint.", "0.00");
                settings.contrast = UIUtils.SliderGUI(settings.contrast, 0f, 5f, () => GraphicSetting.ToneMapSettings.contrast, GUIStrings.tonemappingContrast, "Expands or shrinks the overall range of tonal values.", "0.00");
                settings.hue = UIUtils.SliderGUI(settings.hue, -0.5f, 0.5f, () => GraphicSetting.ToneMapSettings.hue, GUIStrings.tonemappingHue, "Shift the hue of all colors.", "0.00");
                settings.saturation = UIUtils.SliderGUI(settings.saturation, 0f, 3f, () => GraphicSetting.ToneMapSettings.saturation, GUIStrings.tonemappingSaturation, "Pushes the intensity of all colors.", "0.00");
                settings.value = UIUtils.SliderGUI(settings.value, 0f, 10f, () => GraphicSetting.ToneMapSettings.value, GUIStrings.tonemappingValue, "Brightens or darkens all colors.", "0.00");
                settings.vibrance = UIUtils.SliderGUI(settings.vibrance, -1f, 1f, () => GraphicSetting.ToneMapSettings.vibrance, GUIStrings.tonemappingVibrance, "Adjusts the saturation so that clipping is minimized as colors approach full saturation.", "0.00");
                settings.gain = UIUtils.SliderGUI(settings.gain, 0f, 5f, () => GraphicSetting.ToneMapSettings.gain, GUIStrings.tonemappingGain, "Contrast gain curve. Controls the steepness of the curve.", "0.00");
                settings.gamma = UIUtils.SliderGUI(settings.gamma, 0f, 5f, () => GraphicSetting.ToneMapSettings.gamma, GUIStrings.tonemappingGamma, "Applies a pow function to the source.", "0.00");
                colorGrading.basics = settings;
            }

            this._toneMappingManager.colorGrading = colorGrading;
        }

        private void EyeAdaptationModule()
        {
            this._eyeEnabled = UIUtils.ToggleGUI(this._toneMappingManager.eyeAdaptation.enabled, GUIStrings.eyeAdaptation, GUIStrings.disableVsEnable, UIUtils.titlestyle2);
            if (this._eyeEnabled)
            {
                GUILayout.Space(UIUtils.space);
                this._eyeMiddleGrey = UIUtils.SliderGUI(this._toneMappingManager.eyeAdaptation.middleGrey, 0f, 0.5f, 0.1f,"Middle Grey", "Midpoint Adjustment.", "N3");
                this._eyeMin = UIUtils.SliderGUI(this._toneMappingManager.eyeAdaptation.min, -8f, 0f, -4f,"Lowest Exposure Value", "The lowest possible exposure value; adjust this value to modify the brightest areas of your level.", "N3");
                this._eyeMax = UIUtils.SliderGUI(this._toneMappingManager.eyeAdaptation.max, 0f, 8f, 4f,"Highest Exposure Value", "The highest possible exposure value; adjust this value to modify the darkest areas of your level.", "N3");
                this._eyeSpeed = UIUtils.SliderGUI(this._toneMappingManager.eyeAdaptation.speed, 0f, 8f,"Adaptation Speed", "Speed of linear adaptation. Higher is faster.", "N3");
            }
            this._toneMappingManager.eyeAdaptation = new TonemappingColorGrading.EyeAdaptationSettings
            {
                enabled = this._eyeEnabled,
                showDebug = false,
                middleGrey = this._eyeMiddleGrey,
                max = this._eyeMax,
                min = this._eyeMin,
                speed = this._eyeSpeed
            };
        }

        private void SMAAModule()
        {
            GUILayout.Label("SMAA", UIUtils.titlestyle2);
            GUILayout.Space(UIUtils.space);
            SMAA.GlobalSettings settings = this._smaa.settings;
            SMAA.PredicationSettings predication = this._smaa.predication;
            SMAA.TemporalSettings temporal = this._smaa.temporal;

            GUILayout.Label(new GUIContent("Debug Pass", "Use this to fine tune your settings when working in Custom quality mode. \"Accumulation\" only works when \"Temporal Filtering\" is enabled."), UIUtils.labelstyle);
            settings.debugPass = (SMAA.DebugPass)GUILayout.SelectionGrid((int)settings.debugPass, this._possibleSMAADebugPassNames, 3, UIUtils.buttonstyleStrechWidth);
            GUILayout.Label(new GUIContent("Quality", "Low: 60% of the quality.\nMedium: 80% of the quality.\nHigh: 95% of the quality.\nUltra: 99% of the quality (overkill)."), UIUtils.labelstyle);
            settings.quality = (SMAA.QualityPreset)GUILayout.SelectionGrid((int)settings.quality, this._possibleSMAAQualityPresetNames, 5, UIUtils.buttonstyleStrechWidth);
            GUILayout.Label(new GUIContent("Edge Detection Method", "You have three edge detection methods to choose from: luma, color or depth.\nThey represent different quality/performance and anti-aliasing/sharpness tradeoffs, so our recommendation is for you to choose the one that best suits your particular scenario:\n\n- Depth edge detection is usually the fastest but it may miss some edges.\n- Luma edge detection is usually more expensive than depth edge detection, but catches visible edges that depth edge detection can miss.\n- Color edge detection is usually the most expensive one but catches chroma-only edges."), UIUtils.labelstyle);
            settings.edgeDetectionMethod = (SMAA.EdgeDetectionMethod)GUILayout.SelectionGrid((int)settings.edgeDetectionMethod, this._possibleSMAAEdgeDetectionMethodNames, 3, UIUtils.buttonstyleStrechWidth);
            if (settings.quality == SMAA.QualityPreset.Custom)
            {
                SMAA.QualitySettings quality = this._smaa.quality;
                quality.diagonalDetection = UIUtils.ToggleGUI(quality.diagonalDetection, new GUIContent("Diagonal Detection", "Enables/Disables diagonal processing."), GUIStrings.disableVsEnable);
                quality.cornerDetection = UIUtils.ToggleGUI(quality.cornerDetection, new GUIContent("Corner Detection", "Enables/Disables corner detection. Leave this on to avoid blurry corners."), GUIStrings.disableVsEnable);
                quality.threshold = UIUtils.SliderGUI(quality.threshold, 0f, 0.5f, 0.01f, "Threshold", "Filters out pixels under this level of brightness.", "N3");
                quality.depthThreshold = UIUtils.SliderGUI(quality.depthThreshold, 0.0001f, 10f, 0.01f, "Depth Threshold", "Specifies the threshold for depth edge detection. Lowering this value you will be able to detect more edges at the expense of performance.", "N4");
                quality.maxSearchSteps = (int)UIUtils.SliderGUI(quality.maxSearchSteps, 0f, 112, 16, "Max Search Steps", "Specifies the maximum steps performed in the horizontal/vertical pattern searches, at each side of the pixel.\nIn number of pixels, it's actually the double. So the maximum line length perfectly handled by, for example 16, is 64 (by perfectly, we meant that longer lines won't look as good, but still antialiased).", "N");
                quality.maxDiagonalSearchSteps = (int)UIUtils.SliderGUI(quality.maxDiagonalSearchSteps, 0f, 20f, 8, "Max Diagonal Search Steps", "Specifies the maximum steps performed in the diagonal pattern searches, at each side of the pixel. In this case we jump one pixel at time, instead of two.\nOn high-end machines it is cheap (between a 0.8x and 0.9x slower for 16 steps), but it can have a significant impact on older machines.", "N");
                quality.cornerRounding = (int)UIUtils.SliderGUI(quality.cornerRounding, 0f, 100f, 25f, "Corner Rounding", "Specifies how much sharp corners will be rounded.", "N");
                quality.localContrastAdaptationFactor = UIUtils.SliderGUI(quality.localContrastAdaptationFactor, 0f, 10f, 2f, "Local Contrast Adaptation Factor", "If there is a neighbor edge that has a local contrast factor times bigger contrast than current edge, current edge will be discarded.\nThis allows to eliminate spurious crossing edges, and is based on the fact that, if there is too much contrast in a direction, that will hide perceptually contrast in the other neighbors.", "N3");
                this._smaa.quality = quality;
            }

            predication.enabled = UIUtils.ToggleGUI(predication.enabled, new GUIContent("Predication", "Predicated thresholding allows to better preserve texture details and to improve performance, by decreasing the number of detected edges using an additional buffer (the detph buffer).\nIt locally decreases the luma or color threshold if an edge is found in an additional buffer (so the global threshold can be higher)."), GUIStrings.disableVsEnable);
            if (predication.enabled)
            {
                predication.threshold = UIUtils.SliderGUI(predication.threshold, 0.0001f, 10f, 0.01f, "Threshold", "Threshold to be used in the additional predication buffer.", "N4");
                predication.scale = UIUtils.SliderGUI(predication.scale, 1f, 5f, 2, "Scale", "How much to scale the global threshold used for luma or color edge detection when using predication.", "N3");
                predication.strength = UIUtils.SliderGUI(predication.strength, 0f, 1f, 0.4f, "Strength", "How much to locally decrease the threshold.", "N4");
            }

            temporal.enabled = UIUtils.ToggleGUI(temporal.enabled, new GUIContent("Temporal", "Temporal filtering makes it possible for the SMAA algorithm to benefit from minute subpixel information available that has been accumulated over many frames."), GUIStrings.disableVsEnable);
            if (temporal.enabled)
            {
                temporal.fuzzSize = UIUtils.SliderGUI(temporal.fuzzSize, 0.5f, 10f, 2f, "Fuzz Size", "The size of the fuzz-displacement (jitter) in pixels applied to the camera's perspective projection matrix.\nUsed for 2x temporal anti-aliasing.", "N3");
            }

            this._smaa.predication = predication;
            this._smaa.temporal = temporal;
            this._smaa.settings = settings;
        }

        private void BloomModule()
        {
            GUILayout.Label(GUIStrings.bloom, UIUtils.titlestyle2);
            GUILayout.Space(UIUtils.space);
            this._bloomManager.settings.intensity = UIUtils.SliderGUI(this._bloomManager.settings.intensity, 0f, 1f, GraphicSetting.BloomSettings.intensity, "Intensity", "Blend factor of the result image.", "N3");
            this._bloomManager.settings.threshold = UIUtils.SliderGUI(this._bloomManager.settings.threshold, 0f, 8f, GraphicSetting.BloomSettings.threshold, "Threshold", "Filters out pixels under this level of brightness.", "N3");
            this._bloomManager.settings.softKnee = UIUtils.SliderGUI(this._bloomManager.settings.softKnee, 0f, 1f, GraphicSetting.BloomSettings.softKnee, "Softknee", "Makes transition between under/over-threshold gradual.", "N3");
            this._bloomManager.settings.radius = UIUtils.SliderGUI(this._bloomManager.settings.radius, 0f, 16f, GraphicSetting.BloomSettings.raduis, "Radius", "Changes extent of veiling effects in a screen resolution-independent fashion.", "N3");
            this._bloomManager.settings.antiFlicker = UIUtils.ToggleGUI(this._bloomManager.settings.antiFlicker, GUIStrings.bloomAntiflicker, GUIStrings.disableVsEnable);
        }

        private void SSAOModule()
        {
            this._ssao.enabled = UIUtils.ToggleGUI(this._ssao.enabled, new GUIContent("SSAO"), GUIStrings.disableVsEnable, UIUtils.titlestyle2);
            if (Studio.Studio.Instance != null && Studio.Studio.Instance.sceneInfo != null)
                Studio.Studio.Instance.sceneInfo.enableSSAO = this._ssao.enabled;
            if (this._ssao.enabled)
            {
                //GUILayout.Label("SSAO", UIUtils.titlestyle2);
                GUILayout.Space(UIUtils.space);
                this._ssao.UseHighPrecisionDepthMap = UIUtils.ToggleGUI(this._ssao.UseHighPrecisionDepthMap, new GUIContent("Use high precision depth map"), GUIStrings.disableVsEnable);
                GUILayout.Label(new GUIContent("Sample Count", "The number of ambient occlusion samples for each pixel on screen. More samples means slower but smoother rendering. Five presets are available"), UIUtils.labelstyle);
                this._ssao.Samples = (SSAOPro.SampleCount)GUILayout.SelectionGrid((int)this._ssao.Samples, this._possibleSSAOSampleCountNames, 3, UIUtils.buttonstyleStrechWidth);
                this._ssao.Downsampling = Mathf.RoundToInt(UIUtils.SliderGUI(this._ssao.Downsampling, 1f, 4f, 1f,"Downsampling", "Lets you change resolution at which calculations should be performed (for example, a downsampling value of 2 will work at half the screen resolution). Using downsampling increases rendering speed at the cost of quality.", "0"));
                this._ssao.Intensity = UIUtils.SliderGUI(this._ssao.Intensity, 0.0f, 16f, 2f, "Intensity", "The occlusion multiplier (degree of darkness added by ambient occlusion). Push this up or down to get a more or less visible effect.", "N3");
                if (Studio.Studio.Instance != null && Studio.Studio.Instance.sceneInfo != null)
                    Studio.Studio.Instance.sceneInfo.ssaoIntensity = this._ssao.Intensity;
                this._ssao.Radius = UIUtils.SliderGUI(this._ssao.Radius, 0.01f, 1.25f, 0.125f,"Radius", "The maximum radius of a gap (in world units) that will introduce ambient occlusion.", "N3");
                this._ssao.Distance = UIUtils.SliderGUI(this._ssao.Distance, 0.0f, 10f, 1f,"Distance", "Represents the distance between an occluded sample and its occluder.", "N3");
                this._ssao.Bias = UIUtils.SliderGUI(this._ssao.Bias, 0.0f, 1f, 0.1f,"Bias", "The Bias value is added to the occlusion cone. If you’re getting artifacts you may want to push this up while playing with the Distance parameter.", "N3");
                this._ssao.LumContribution = UIUtils.SliderGUI(this._ssao.LumContribution, 0.0f, 1f, 0.5f, "Lighting Contribution", "Defines how much ambient occlusion should be added in bright areas. By pushing this up, bright areas will have less ambient occlusion which generally leads to more pleasing results.", "N3");
                UIUtils.ColorPickerGUI(this._ssao.OcclusionColor, Color.black,"Occlusion Color", c =>
                {
                    this._ssao.OcclusionColor = c;
                    if (Studio.Studio.Instance != null && Studio.Studio.Instance.sceneInfo != null)
                        Studio.Studio.Instance.sceneInfo.ssaoColor.SetDiffuseRGBA(c);
                });
                GUILayout.Label(new GUIContent("Blur Mode", "None: no blur will be applied to the ambient occlusion pass. Gaussian: an optimized 9 - tap filter. Bilateral: a bilateral box filter capable of detecting borders. High Quality Bilateral: a smooth bilateral gaussian filter capable of detecting borders."), UIUtils.labelstyle);
                this._ssao.Blur = (SSAOPro.BlurMode)GUILayout.SelectionGrid((int)this._ssao.Blur, this._possibleSSAOBlurModeNames, 2, UIUtils.buttonstyleStrechWidth);
                this._ssao.BlurDownsampling = UIUtils.ToggleGUI(this._ssao.BlurDownsampling, new GUIContent("Blur Downsampling", "If enabled, the blur pass will be applied to the downsampled render before it gets resized to fit the screen. Else, it will be applied after the resize, which increases quality but is a bit slower."), GUIStrings.disableVsEnable);
                if (this._ssao.Blur != SSAOPro.BlurMode.None)
                {
                    this._ssao.BlurPasses = Mathf.RoundToInt(UIUtils.SliderGUI(this._ssao.BlurPasses, 1f, 4f, 1, "Blur Passes", "Applies more blur to give a smoother effect at the cost of performance.", "0"));
                    if (this._ssao.Blur == SSAOPro.BlurMode.HighQualityBilateral)
                        this._ssao.BlurBilateralThreshold = UIUtils.SliderGUI(this._ssao.BlurBilateralThreshold, 0.05f, 1f, 0.1f, "Depth Threshold", "Tweak this to adjust the blur \"sharpness\".", "N3");
                }
                this._ssao.CutoffDistance = UIUtils.SliderGUI(this._ssao.CutoffDistance, 0.1f, 400f, 150f,"Max Distance", "Used to stop applying ambient occlusion for distant objects (very useful when using fog). ", "N1");
                this._ssao.CutoffFalloff = UIUtils.SliderGUI(this._ssao.CutoffFalloff, 0.1f, 100f, 50f,"Falloff", "Used to ease out the cutoff, i.e. set it to 0 and the SSAO will stop abruptly at Max Distance; set it to 50 and the SSAO will smoothly disappear starting at (Max Distance) - (Falloff)", "N1");
                this._ssao.DebugAO = UIUtils.ToggleGUI(this._ssao.DebugAO, new GUIContent("Debug AO"), GUIStrings.disableVsEnable);
                if (GUILayout.Button("Open full documentation in browser", UIUtils.buttonstyleStrechWidth))
                    System.Diagnostics.Process.Start("http://www.thomashourdel.com/ssaopro/doc/usage.html");
            }
        }

        private void SunShaftsModule()
        {
            this._sunShafts.enabled = UIUtils.ToggleGUI(this._sunShafts.enabled, new GUIContent("Sun Shafts"), GUIStrings.disableVsEnable, UIUtils.titlestyle2);
            if (Studio.Studio.Instance != null && Studio.Studio.Instance.sceneInfo != null)
                Studio.Studio.Instance.sceneInfo.enableSunShafts = this._sunShafts.enabled;
            if (this._sunShafts.enabled)
            {
                //GUILayout.Label("Sun Shafts", UIUtils.titlestyle2);
                GUILayout.Space(UIUtils.space);
                this._sunShafts.useDepthTexture = UIUtils.ToggleGUI(this._sunShafts.useDepthTexture, new GUIContent("Use Depth Buffer"), GUIStrings.disableVsEnable);
                GUILayout.Label(new GUIContent("Resolution", "The resolution at which the shafts are generated. Lower resolutions are faster to calculate and create softer results."), UIUtils.labelstyle);
                this._sunShafts.resolution = (SunShafts.SunShaftsResolution)GUILayout.SelectionGrid((int)this._sunShafts.resolution, this._possibleSunShaftsResolutionNames, 3, UIUtils.buttonstyleStrechWidth);
                GUILayout.Label("Screen Blend Mode", UIUtils.labelstyle);
                this._sunShafts.screenBlendMode = (SunShafts.ShaftsScreenBlendMode)GUILayout.SelectionGrid((int)this._sunShafts.screenBlendMode, this._possibleShaftsScreenBlendModeNames, 2, UIUtils.buttonstyleStrechWidth);
                UIUtils.ColorPickerGUI(this._sunShafts.sunThreshold, new Color(0.87f, 0.74f, 0.65f),"Threshold Color", c =>
                {
                    this._sunShafts.sunThreshold = c;
                });
                UIUtils.ColorPickerGUI(this._sunShafts.sunColor, Color.white,"Shafts Color", c =>
                {
                    this._sunShafts.sunColor = c;
                });
                this._sunShafts.maxRadius = UIUtils.SliderGUI(this._sunShafts.maxRadius, 0f, 1f, 0.75f,"Max Radius", "The degree to which the shafts' brightness diminishes with distance from the Sun object.", "N3");
                this._sunShafts.sunShaftBlurRadius = UIUtils.SliderGUI(this._sunShafts.sunShaftBlurRadius, 0.01f, 20f, 2.5f,"Blur Radius", "The radius over which pixel colours are combined during blurring.", "N3");
                this._sunShafts.radialBlurIterations = Mathf.RoundToInt(UIUtils.SliderGUI(this._sunShafts.radialBlurIterations, 0f, 8f, 2f,"Radial Blur Iterations", "The number of repetitions of the blur operation. More iterations will give smoother blurring but each has a cost in processing time.", "0"));
                this._sunShafts.sunShaftIntensity = UIUtils.SliderGUI(this._sunShafts.sunShaftIntensity, 0f, 20f, 1.15f,"Intensity", "The brightness of the generated shafts.", "N3");
                if (GUILayout.Button("Open full documentation in browser", UIUtils.buttonstyleStrechWidth))
                    System.Diagnostics.Process.Start("https://docs.unity3d.com/530/Documentation/Manual/script-SunShafts.html");
            }
        }

        private void DepthOfFieldModule()
        {
            this._depthOfField.enabled = UIUtils.ToggleGUI(this._depthOfField.enabled, new GUIContent("Depth Of Field"), GUIStrings.disableVsEnable, UIUtils.titlestyle2);
            if (Studio.Studio.Instance != null && Studio.Studio.Instance.sceneInfo != null)
                Studio.Studio.Instance.sceneInfo.enableDepth = this._depthOfField.enabled;
            //GUILayout.Label("Depth Of Field", UIUtils.titlestyle2);
            if (this._depthOfField.enabled)
            {
                GUILayout.Space(UIUtils.space);

                this._depthOfField.visualizeFocus = UIUtils.ToggleGUI(this._depthOfField.visualizeFocus, new GUIContent("Visualize Focus", "Overlay color indicating camera focus."), GUIStrings.disableVsEnable);
                bool useCameraOrigin = UIUtils.ToggleGUI(this._depthOfField.focalTransform != null, new GUIContent("Use Camera Origin as Focus", "If enabled, makes the camera origin the automatic focus point, otherwise the Focal Distance is used."), GUIStrings.disableVsEnable);
                if (useCameraOrigin)
                {
                    if (this._depthOfField.focalTransform == null)
                        this._depthOfField.focalTransform = this._depthOfFieldFocusPoint;
                }
                else
                {
                    if (this._depthOfField.focalTransform != null)
                        this._depthOfField.focalTransform = null;
                    this._depthOfField.focalLength = UIUtils.SliderGUI(this._depthOfField.focalLength, 0.01f, 50f, 10f, "Focal Distance", "The distance to the focal plane from the camera position in world space.", "N2");
                }
                this._depthOfField.focalSize = UIUtils.SliderGUI(this._depthOfField.focalSize, 0f, 2f, 0.05f,"Focal Size", "Increase the total focal area.", "N3");
                if (Studio.Studio.Instance != null && Studio.Studio.Instance.sceneInfo != null)
                    Studio.Studio.Instance.sceneInfo.depthFocalSize = this._depthOfField.focalSize;
                this._depthOfField.aperture = UIUtils.SliderGUI(this._depthOfField.aperture, 0f, 1f, 0.5f,"Aperture", "The camera’s aperture defining the transition between focused and defocused areas. It is good practice to keep this value as high as possible, as otherwise sampling artifacts might occur, especially when the Max Blur Distance is big. Bigger Aperture values will automatically downsample the image to produce a better defocus.", "N3");
                if (Studio.Studio.Instance != null && Studio.Studio.Instance.sceneInfo != null)
                    Studio.Studio.Instance.sceneInfo.depthAperture = this._depthOfField.aperture;
                this._depthOfField.maxBlurSize = UIUtils.SliderGUI(this._depthOfField.maxBlurSize, 0f, 128f, 2f,"Max Blur Distance", "Max distance for filter taps. Affects texture cache and can cause undersampling artifacts if value is too big. A value smaller than 4.0 should produce decent results.", "N1");
                this._depthOfField.highResolution = UIUtils.ToggleGUI(this._depthOfField.highResolution, new GUIContent("High Resolution", "Perform defocus operations in full resolution. Affects performance but might help reduce unwanted artifacts and produce more defined bokeh shapes."), GUIStrings.disableVsEnable);
                this._depthOfField.nearBlur = UIUtils.ToggleGUI(this._depthOfField.nearBlur, new GUIContent("Near Blur", "Foreground areas will overlap at a performance cost."), GUIStrings.disableVsEnable);
                if (this._depthOfField.nearBlur)
                    this._depthOfField.foregroundOverlap = UIUtils.SliderGUI(this._depthOfField.foregroundOverlap, 0.1f, 2f, 1,"Overlap Size", "Increase foreground overlap dilation if needed.", "N3");
                GUILayout.Label(new GUIContent("Blur Type", "Algorithm used to produce defocused areas. DX11 is effectively a bokeh splatting technique while DiscBlur indicates a more traditional (scatter as gather) based blur."), UIUtils.labelstyle);
                this._depthOfField.blurType = (DepthOfField.BlurType)GUILayout.SelectionGrid((int)this._depthOfField.blurType, this._possibleBlurTypeNames, 2, UIUtils.buttonstyleStrechWidth);
                switch (this._depthOfField.blurType)
                {
                    case DepthOfField.BlurType.DiscBlur:
                        GUILayout.Label(new GUIContent("Sample Count", "Amount of filter taps. Greatly affects performance."), UIUtils.labelstyle);
                        this._depthOfField.blurSampleCount = (DepthOfField.BlurSampleCount)GUILayout.SelectionGrid((int)this._depthOfField.blurSampleCount, this._possibleBlurSampleCountNames, 3, UIUtils.buttonstyleStrechWidth);
                        break;
                    case DepthOfField.BlurType.DX11:
                        GUILayout.Label("DX11 Bokeh Settings", UIUtils.labelstyle);
                        this._depthOfField.dx11BokehScale = UIUtils.SliderGUI(this._depthOfField.dx11BokehScale, 0f, 50f, 1.2f,"Bokeh Scale", "Size of bokeh texture.", "N3");
                        this._depthOfField.dx11BokehIntensity = UIUtils.SliderGUI(this._depthOfField.dx11BokehIntensity, 0f, 100f, 2.5f,"Bokeh Intensity", "Blend strength of bokeh shapes.", "N2");
                        this._depthOfField.dx11BokehThreshold = UIUtils.SliderGUI(this._depthOfField.dx11BokehThreshold, 0f, 2f, 0.5f,"Min Luminance", "Only pixels brighter than this value will cast bokeh shapes. Affects performance as it limits overdraw to a more reasonable amount.", "N3");
                        this._depthOfField.dx11SpawnHeuristic = UIUtils.SliderGUI(this._depthOfField.dx11SpawnHeuristic, 0.01f, 1f, 0.0875f,"Spawn Heuristic", "Bokeh shapes will only be cast if pixel in questions passes a frequency check. A threshold around 0.1 seems like a good tradeoff between performance and quality.", "N4");
                        break;
                }
                if (GUILayout.Button("Open full documentation in browser", UIUtils.buttonstyleStrechWidth))
                    System.Diagnostics.Process.Start("https://docs.unity3d.com/530/Documentation/Manual/script-DepthOfField.html");
            }
        }

        private void SSRModule()
        {
            this._ssr.enabled = UIUtils.ToggleGUI(this._ssr.enabled, new GUIContent("Screen Space Reflections"), GUIStrings.disableVsEnable, UIUtils.titlestyle2);
            //GUILayout.Label("Screen Space Reflections", UIUtils.titlestyle2);
            if (Studio.Studio.Instance != null && Studio.Studio.Instance.sceneInfo != null)
                Studio.Studio.Instance.sceneInfo.enableSSR = this._ssr.enabled;
            if (this._ssr.enabled)
            {
                GUILayout.Space(UIUtils.space);

                ScreenSpaceReflection.SSRSettings ssrSettings = this._ssr.settings;

                GUILayout.Label("Basic Settings", UIUtils.labelstyle);
                ScreenSpaceReflection.BasicSettings basicSettings = ssrSettings.basicSettings;
                {
                    basicSettings.reflectionMultiplier = UIUtils.SliderGUI(basicSettings.reflectionMultiplier, 0.0f, 2f, 1f,"Reflection Multiplier","Nonphysical multiplier for the SSR reflections. 1.0 is physically based.","N3");
                    basicSettings.maxDistance = UIUtils.SliderGUI(basicSettings.maxDistance, 0.5f, 1000f, 100f,"Max Distance","Maximum reflection distance in world units.","N1");
                    basicSettings.fadeDistance = UIUtils.SliderGUI(basicSettings.fadeDistance, 0.0f, 1000f, 100f,"Fade Distance","How far away from the maxDistance to begin fading SSR.","N1");
                    basicSettings.screenEdgeFading = UIUtils.SliderGUI(basicSettings.screenEdgeFading, 0.0f, 1f, 0.03f,"Screen Edge Fading","Higher = fade out SSRR near the edge of the screen so that reflections don't pop under camera motion.","N3");
                    basicSettings.enableHDR = UIUtils.ToggleGUI(basicSettings.enableHDR, new GUIContent("Enable HDR","Enable for better reflections of very bright objects at a performance cost"), GUIStrings.disableVsEnable);
                    basicSettings.additiveReflection = UIUtils.ToggleGUI(basicSettings.additiveReflection, new GUIContent("Additive Reflection","Add reflections on top of existing ones. Not physically correct."), GUIStrings.disableVsEnable);
                }

                GUILayout.Label("Reflection Settings", UIUtils.labelstyle);
                ScreenSpaceReflection.ReflectionSettings reflectionSettings = ssrSettings.reflectionSettings;
                {
                    reflectionSettings.maxSteps = Mathf.RoundToInt(UIUtils.SliderGUI(reflectionSettings.maxSteps, 16f, 2048f, 0f,"Max Steps","Max raytracing length.","0"));
                    reflectionSettings.rayStepSize = Mathf.RoundToInt(UIUtils.SliderGUI(reflectionSettings.rayStepSize, 0.0f, 4f, 3f,"Ray Step Size","Log base 2 of ray tracing coarse step size. Higher traces farther, lower gives better quality silhouettes.","0"));
                    reflectionSettings.widthModifier = UIUtils.SliderGUI(reflectionSettings.widthModifier, 0.01f, 10f, 0.5f,"Width Modifier","Typical thickness of columns, walls, furniture, and other objects that reflection rays might pass behind.","N3");
                    reflectionSettings.smoothFallbackThreshold = UIUtils.SliderGUI(reflectionSettings.smoothFallbackThreshold, 0.0f, 1f, 0f,"Smooth Fallback Threshold","Increase if reflections flicker on very rough surfaces.","N3");
                    reflectionSettings.smoothFallbackDistance = UIUtils.SliderGUI(reflectionSettings.smoothFallbackDistance, 0.0f, 0.2f, 0f,"Smooth Fallback Distance","Start falling back to non-SSR value solution at smoothFallbackThreshold - smoothFallbackDistance, with full fallback occuring at smoothFallbackThreshold.","N3");
                    reflectionSettings.fresnelFade = UIUtils.SliderGUI(reflectionSettings.fresnelFade, 0.0f, 1f, 1f,"Fresnel Fade","Amplify Fresnel fade out. Increase if floor reflections look good close to the surface and bad farther 'under' the floor.","N3");
                    reflectionSettings.fresnelFadePower = UIUtils.SliderGUI(reflectionSettings.fresnelFadePower, 0.1f, 10f, 1f,"Fresnel Fade Power","Higher values correspond to a faster Fresnel fade as the reflection changes from the grazing angle.","N3");
                    reflectionSettings.distanceBlur = UIUtils.SliderGUI(reflectionSettings.distanceBlur, 0.0f, 1f, 0f,"Distance Blur","Controls how blurry reflections get as objects are further from the camera. 0 is constant blur no matter trace distance or distance from camera. 1 fully takes into account both factors.","N3");
                }

                GUILayout.Label("Advanced Settings", UIUtils.labelstyle);
                ScreenSpaceReflection.AdvancedSettings advancedSettings = ssrSettings.advancedSettings;
                {
                    advancedSettings.temporalFilterStrength = UIUtils.SliderGUI(advancedSettings.temporalFilterStrength, 0.0f, 0.99f, 0f,"Temporal Filter Strength","Increase to decrease flicker in scenes; decrease to prevent ghosting (especially in dynamic scenes). 0 gives maximum performance.","N3");
                    advancedSettings.useTemporalConfidence = UIUtils.ToggleGUI(advancedSettings.useTemporalConfidence, new GUIContent("Use Temporal Confidence","Enable to limit ghosting from applying the temporal filter."), GUIStrings.disableVsEnable);
                    advancedSettings.traceBehindObjects = UIUtils.ToggleGUI(advancedSettings.traceBehindObjects, new GUIContent("Trace Behind Objects","Enable to allow rays to pass behind objects. This can lead to more screen-space reflections, but the reflections are more likely to be wrong."), GUIStrings.disableVsEnable);
                    advancedSettings.highQualitySharpReflections = UIUtils.ToggleGUI(advancedSettings.highQualitySharpReflections, new GUIContent("High Quality Sharp Reflections","Enable to increase quality of the sharpest reflections (through filtering), at a performance cost."), GUIStrings.disableVsEnable);
                    advancedSettings.traceEverywhere = UIUtils.ToggleGUI(advancedSettings.traceEverywhere, new GUIContent("Trace Everywhere","Improves quality in scenes with varying smoothness, at a potential performance cost."), GUIStrings.disableVsEnable);
                    advancedSettings.treatBackfaceHitAsMiss = UIUtils.ToggleGUI(advancedSettings.treatBackfaceHitAsMiss, new GUIContent("Treat Backface Hit As Miss","Enable to force more surfaces to use reflection probes if you see streaks on the sides of objects or bad reflections of their backs."), GUIStrings.disableVsEnable);
                    advancedSettings.allowBackwardsRays = UIUtils.ToggleGUI(advancedSettings.allowBackwardsRays, new GUIContent("Allow Backward Rays","Enable for a performance gain in scenes where most glossy objects are horizontal, like floors, water, and tables. Leave on for scenes with glossy vertical objects."), GUIStrings.disableVsEnable);
                    advancedSettings.improveCorners = UIUtils.ToggleGUI(advancedSettings.improveCorners, new GUIContent("Improve Corners","Improve visual fidelity of reflections on rough surfaces near corners in the scene, at the cost of a small amount of performance."), GUIStrings.disableVsEnable);
                    GUILayout.Label(new GUIContent("Resolution","Half resolution SSRR is much faster, but less accurate. Quality can be reclaimed for some performance by doing the resolve at full resolution."), UIUtils.labelstyle);
                    advancedSettings.resolution = (ScreenSpaceReflection.SSRResolution)GUILayout.SelectionGrid((int)advancedSettings.resolution, this._possibleSSRResolutionNames, 2, UIUtils.buttonstyleStrechWidth);
                    advancedSettings.bilateralUpsample = UIUtils.ToggleGUI(advancedSettings.bilateralUpsample, new GUIContent("Bilateral Upsample","Drastically improves reflection reconstruction quality at the expense of some performance."), GUIStrings.disableVsEnable);
                    advancedSettings.reduceBanding = UIUtils.ToggleGUI(advancedSettings.reduceBanding, new GUIContent("Reduce Banding","Improve visual fidelity of mirror reflections at the cost of a small amount of performance."), GUIStrings.disableVsEnable);
                    advancedSettings.highlightSuppression = UIUtils.ToggleGUI(advancedSettings.highlightSuppression, new GUIContent("Highlight Suppression","Enable to limit the effect a few bright pixels can have on rougher surfaces"), GUIStrings.disableVsEnable);
                }

                GUILayout.Label("Debug Settings", UIUtils.labelstyle);
                ScreenSpaceReflection.DebugSettings debugSettings = ssrSettings.debugSettings;
                GUILayout.Label(new GUIContent("Debug Mode","Various Debug Visualizations"), UIUtils.labelstyle);
                debugSettings.debugMode = (ScreenSpaceReflection.SSRDebugMode)GUILayout.SelectionGrid((int)debugSettings.debugMode, this._possibleSSRDebugModeNames, 2, UIUtils.buttonstyleStrechWidth);

                ssrSettings.basicSettings = basicSettings;
                ssrSettings.reflectionSettings = reflectionSettings;
                ssrSettings.advancedSettings = advancedSettings;
                ssrSettings.debugSettings = debugSettings;
                this._ssr.settings = ssrSettings;
            }
        }

        private void CharaMakerWarningWindow(int id)
        {
            GUILayout.BeginVertical();
            GUILayout.Label("Using image based lighting in the chara maker requires deferred renderingpath and HDR, meanwhile you can't use background image. DO you want to continue?", UIUtils.labelstyle3);
            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Just this time", UIUtils.buttonstyleNoStretch))
            {
                Console.WriteLine("HSIBL Info: Changing rendering path to deferred shading.");
                Camera.main.renderingPath = RenderingPath.DeferredShading;
                Camera.main.clearFlags = CameraClearFlags.SolidColor;
                //return;
            }
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Always", UIUtils.buttonstyleNoStretch))
            {
                GraphicSetting.BasicSettings.CharaMakerReform = true;
                Console.WriteLine("HSIBL Info: Changing rendering path to deferred shading.");
                Camera.main.renderingPath = RenderingPath.DeferredShading;
                Camera.main.clearFlags = CameraClearFlags.SolidColor;
                //return;
            }
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("No", UIUtils.buttonstyleNoStretch))
            {
                MainWindow = false;
                //return;
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();

        }

        private void ErrorWindow(int id)
        {

            GUILayout.BeginVertical();
            GUILayout.Label("Error"+ _errorcode +": Please make sure you have installed HS linear rendering experiment (Version ≥ 3).", UIUtils.labelstyle3);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("OK", UIUtils.buttonstyleNoStretch))
            {
                MainWindow = false;
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();
        }
        #endregion

        #region Private Methods
        private IEnumerator RotateCharater()
        {
            while(true)
            {
                if (this._autoRotate && this._cMfemale != null)
                {
                    this._charaRotate += this._autoRotateSpeed;
                    if (this._charaRotate > 180f)
                    {
                        this._charaRotate -= 360f;
                    }
                    else if (this._charaRotate < -180f)
                    {
                        this._charaRotate += 360f;
                    }
                    this._cMfemale.SetRotation(this._rotateValue.x, this._rotateValue.y + this._charaRotate, this._rotateValue.z);
                }
                yield return new WaitForEndOfFrame();
            }            
        }

        private void CharaRotateModule()
        {
            if (this._cMfemale)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Rotate Character", UIUtils.titlestyle2);
                GUILayout.FlexibleSpace();
                this._autoRotate = UIUtils.ToggleButton(this._autoRotate, new GUIContent("Auto Rotate"));
                GUILayout.EndHorizontal();
                if (this._autoRotate)
                {
                    GUILayout.Label("Auto Rotate Speed"+ this._autoRotateSpeed.ToString("N3"), UIUtils.labelstyle);
                    this._autoRotateSpeed = GUILayout.HorizontalSlider(this._autoRotateSpeed, -5f, 5f, UIUtils.sliderstyle, UIUtils.thumbstyle);
                }
                else
                {
                    this._charaRotate = GUILayout.HorizontalSlider(this._charaRotate, -180f, 180f, UIUtils.sliderstyle, UIUtils.thumbstyle);
                    this._cMfemale.SetRotation(this._rotateValue.x, this._rotateValue.y + this._charaRotate, this._rotateValue.z);
                }
                GUILayout.Space(UIUtils.space);
            }
        }

        private void UserCustomModule()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(GUIStrings.customWindow, UIUtils.titlestyle2);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(GUIStrings.customWindowRemember, UIUtils.buttonstyleNoStretch, GUILayout.ExpandWidth(false)))
            {
                ModPrefs.SetFloat("HSIBL","Window.width", UIUtils.windowRect.width);
                ModPrefs.SetFloat("HSIBL","Window.height", UIUtils.windowRect.height);
                ModPrefs.SetFloat("HSIBL","Window.x", UIUtils.windowRect.x);
                ModPrefs.SetFloat("HSIBL","Window.y", UIUtils.windowRect.y);
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(UIUtils.space);
            float widthtemp = UIUtils.SliderGUI(
                                                UIUtils.windowRect.width,
                                                UIUtils.minwidth,
                                                UIUtils.Screen.width * 0.5f,
                                                this._getWindowWidth,
                                                GUIStrings.windowWidth,
                                               "N0");
            if (Mathf.Approximately(widthtemp, UIUtils.windowRect.width) == false)
            {
                UIUtils.windowRect.x += (UIUtils.windowRect.width - widthtemp) * (UIUtils.windowRect.x) / (UIUtils.Screen.width - UIUtils.windowRect.width);
                UIUtils.windowRect.width = widthtemp;
            }
            UIUtils.windowRect.height = UIUtils.SliderGUI(
                                                          UIUtils.windowRect.height,
                                                          UIUtils.Screen.height * 0.2f,
                                                          UIUtils.Screen.height - 10f,
                                                          this._getWindowHeight,
                                                          GUIStrings.windowHeight,
                                                         "N0");
            GUILayout.Label("Presets", UIUtils.titlestyle2);
            GUILayout.BeginVertical(GUI.skin.box);
            this._presetsScroll = GUILayout.BeginScrollView(this._presetsScroll, false, true, GUILayout.MaxHeight(300));
            if (this._presets.Length != 0)
            foreach (string preset in this._presets)
            {
                if (GUILayout.Button(preset, UIUtils.buttonstyleStrechWidth))
                {
                    if (this._removePresetMode)
                        this.DeletePreset(preset + ".xml");
                    else
                        this.LoadPreset(preset + ".xml");
                }
            }
            else
                GUILayout.Label("No preset...", UIUtils.labelstyle);
            GUILayout.EndScrollView();
            GUILayout.EndVertical();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Name: ", UIUtils.labelstyle, GUILayout.ExpandWidth(false));
            Color c = GUI.color;
            if (this._presets.Any(p => p.Equals(this._presetName, StringComparison.OrdinalIgnoreCase)))
                GUI.color = Color.red;
            this._presetName = GUILayout.TextField(this._presetName, UIUtils.textFieldStyle2);
            GUI.color = c;
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUI.enabled = this._presetName.Length != 0;
            if (GUILayout.Button("Save current settings", UIUtils.buttonstyleStrechWidth))
            {
                this._presetName = this._presetName.Trim();
                this._presetName = string.Join("_", this._presetName.Split(Path.GetInvalidFileNameChars()));
                if (this._presetName.Length != 0)
                {
                    this.SavePreset(this._presetName + ".xml");
                    this.RefreshPresetList();
                    this._removePresetMode = false;
                }
            }
            GUI.enabled = true;
            if (this._removePresetMode)
                GUI.color = Color.red;
            GUI.enabled = this._presets.Length != 0;
            if (GUILayout.Button(this._removePresetMode ? "Click on preset" : "Removal mode", UIUtils.buttonstyleStrechWidth))
                this._removePresetMode = !this._removePresetMode;
            GUI.enabled = true;
            GUI.color = c;
            GUILayout.EndHorizontal();
        }

        private void RefreshPresetList()
        {
            if (Directory.Exists(_presetFolder))
            {
                this._presets = Directory.GetFiles(_presetFolder, "*.xml");
                for (int i = 0; i < this._presets.Length; i++)
                    this._presets[i] = Path.GetFileNameWithoutExtension(this._presets[i]);
            }
        }

        private void OptimalSetting(bool auto)
        {
            if (Application.productName =="StudioNEO")
            {
                this._cameraCtrl = Singleton<Studio.Studio>.Instance.cameraCtrl;
                SceneInfo sceneInfo = Singleton<Studio.Studio>.Instance.sceneInfo;
                sceneInfo.enableFog = false;
                sceneInfo.enableDepth = false;
                sceneInfo.enableSunShafts = false;
                SystemButtonCtrl sysbctrl = Singleton<Studio.Studio>.Instance.systemButtonCtrl;
                sysbctrl.UpdateInfo();
            }
            this._bloomManager.settings.intensity = 0.1f;
            this._bloomManager.settings.radius = 5f;
            this._bloomManager.settings.threshold = 5f;
            this._bloomManager.settings.softKnee = 0.5f;
            this._frontDirectionalLight.intensity = 1f;
            this._backDirectionalLight.intensity = 0f;
            RenderSettings.ambientIntensity = 1f;
            RenderSettings.reflectionIntensity = 1f;
            this._probeComponent.intensity = 1f;
            this._toneMappingManager.eyeAdaptation = new TonemappingColorGrading.EyeAdaptationSettings
            {
                enabled = auto,
                showDebug = false,
                middleGrey = 0.09f,
                max = 4f,
                min = -4f,
                speed = 2f
            };
            this._toneMappingManager.tonemapping = new TonemappingColorGrading.TonemappingSettings
            {
                tonemapper = TonemappingColorGrading.Tonemapper.ACES,
                exposure = 1f,
                enabled = true,
                neutralBlackIn = this._toneMappingManager.tonemapping.neutralBlackIn,
                neutralBlackOut = this._toneMappingManager.tonemapping.neutralBlackOut,
                neutralWhiteClip = this._toneMappingManager.tonemapping.neutralWhiteClip,
                neutralWhiteIn = this._toneMappingManager.tonemapping.neutralWhiteIn,
                neutralWhiteLevel = this._toneMappingManager.tonemapping.neutralWhiteLevel,
                neutralWhiteOut = this._toneMappingManager.tonemapping.neutralWhiteOut,
                curve = this._toneMappingManager.tonemapping.curve
            };
        }

        private IEnumerator UpdateEnvironment()
        {
            while (true)
            {
                if (this._environmentUpdateFlag)
                {
                    DynamicGI.UpdateEnvironment();
                    if (this._probeComponent.refreshMode == ReflectionProbeRefreshMode.ViaScripting)
                    {
                        this._probeComponent.RenderProbe();
                    }
                    this._environmentUpdateFlag = false;
                }
                yield return new WaitForEndOfFrame();
            }
        }

        private IEnumerator LoadCubemapAsync(string filename)
        {
            this._isLoading = true;
            AssetBundleCreateRequest assetBundleCreateRequest = AssetBundle.LoadFromFileAsync(Application.dataPath +"/../abdata/plastic/cubemaps/"+ filename +".unity3d");
            yield return assetBundleCreateRequest;
            AssetBundle cubemapbundle = assetBundleCreateRequest.assetBundle;
            AssetBundleRequest bundleRequest = assetBundleCreateRequest.assetBundle.LoadAssetAsync<Material>("skybox");
            yield return bundleRequest;
            this._skybox.Skybox = bundleRequest.asset as Material;
            cubemapbundle.Unload(false);
            this._skybox.ApplySkybox();
            this._skybox.ApplySkyboxParams();

            this._environmentUpdateFlag = true;

            if (false == this._hideSkybox)
            {
                Camera.main.clearFlags = CameraClearFlags.Skybox;
            }

            this._cubemaploaded = true;
            cubemapbundle = null;
            bundleRequest = null;
            assetBundleCreateRequest = null;
            Resources.UnloadUnusedAssets();
            this._isLoading = false;
            yield break;
        }

        private void LoadCubemap(string filename)
        {
            this._isLoading = true;
            AssetBundle cubemapbundle = AssetBundle.LoadFromFile(Application.dataPath + "/../abdata/plastic/cubemaps/" + filename + ".unity3d");
            this._skybox.Skybox = cubemapbundle.LoadAsset<Material>("skybox");
            cubemapbundle.Unload(false);
            this._skybox.ApplySkybox();
            this._skybox.ApplySkyboxParams();

            this._environmentUpdateFlag = true;

            if (false == this._hideSkybox)
            {
                Camera.main.clearFlags = CameraClearFlags.Skybox;
            }

            this._cubemaploaded = true;
            Resources.UnloadUnusedAssets();
            this._isLoading = false;
        }
        #endregion

        #region Saves
        private void OnSceneLoad(string path, XmlNode node)
        {
            this.StartCoroutine(this.OnSceneLoad_Routine(node));
        }

        private IEnumerator OnSceneLoad_Routine(XmlNode node)
        {
            yield return null;
            yield return null;
            if (node == null)
                yield break;
            this.LoadConfig(node);
        }

        private void OnSceneSave(string path, XmlTextWriter writer)
        {
            this.SaveConfig(writer);
        }

        private void SavePreset(string name)
        {
            if (Directory.Exists(_presetFolder) == false)
                Directory.CreateDirectory(_presetFolder);
            using (XmlTextWriter writer = new XmlTextWriter(Path.Combine(_presetFolder, name), Encoding.UTF8))
            {
                writer.WriteStartElement("root");
                this.SaveConfig(writer);
                writer.WriteEndElement();
            }
        }

        private void LoadPreset(string name)
        {
            string path = Path.Combine(_presetFolder, name);
            if (File.Exists(path) == false)
                return;
            XmlDocument doc = new XmlDocument();
            doc.Load(path);
            this.LoadConfig(doc.FirstChild);
        }

        private void DeletePreset(string name)
        {
            File.Delete(Path.GetFullPath(Path.Combine(_presetFolder, name)));
            this._removePresetMode = false;
            this.RefreshPresetList();
        }

        private void LoadConfig(XmlNode node)
        {
            foreach (XmlNode moduleNode in node.ChildNodes)
            {
                switch (moduleNode.Name)
                {
                    case "cubemap":
                        this._hideSkybox = XmlConvert.ToBoolean(moduleNode.Attributes["hide"].Value);
                        this._selectedCubeMap = XmlConvert.ToInt32(moduleNode.Attributes["index"].Value);
                        if (this._selectedCubeMap == -1)
                        {
                            this._skybox.Skybox = this._originalSkybox;
                            RenderSettings.skybox = this._originalSkybox;
                            RenderSettings.ambientMode = this._originalAmbientMode;
                            RenderSettings.defaultReflectionMode = this._originalDefaultReflectionMode;
                            this._cubemaploaded = false;
                            this._selectedCubeMap = -1;
                            this._previousSelectedCubeMap = -1;
                            Camera.main.clearFlags = CameraClearFlags.SolidColor;
                        }
                        else if (this._selectedCubeMap == 0)
                        {
                            this._proceduralSkybox.ApplySkybox();
                            this._proceduralSkybox.ApplySkyboxParams();
                            this._environmentUpdateFlag = true;
                            this._cubemaploaded = true;
                            this._previousSelectedCubeMap = -1;
                        }
                        else
                        {
                            string fileName = moduleNode.Attributes["fileName"].Value;
                            this._selectedCubeMap = -1;
                            for (int i = 0; i < this._cubemapFolder.lstFile.Count; i++)
                            {
                                FolderAssist.FileInfo fileInfo = this._cubemapFolder.lstFile[i];
                                if (string.Compare(fileName, fileInfo.FileName, StringComparison.OrdinalIgnoreCase) == 0)
                                {
                                    this._selectedCubeMap = i + 1;
                                    break;
                                }
                            }
                            if (this._selectedCubeMap == -1)
                            {
                                this._skybox.Skybox = this._originalSkybox;
                                RenderSettings.skybox = this._originalSkybox;
                                RenderSettings.ambientMode = this._originalAmbientMode;
                                RenderSettings.defaultReflectionMode = this._originalDefaultReflectionMode;
                                this._cubemaploaded = false;
                                this._selectedCubeMap = -1;
                                this._previousSelectedCubeMap = -1;
                                Camera.main.clearFlags = CameraClearFlags.SolidColor;
                            }
                            else
                            {
                                this.LoadCubemap(this._cubemapFolder.lstFile[this._selectedCubeMap - 1].FileName);
                                this._previousSelectedCubeMap = this._selectedCubeMap;
                            }
                        }

                        if (this._hideSkybox)
                            Camera.main.clearFlags = CameraClearFlags.SolidColor;
                        else if (this._cubemaploaded)
                            Camera.main.clearFlags = CameraClearFlags.Skybox;
                        break;
                    case "skybox":
                        this._proceduralSkybox.skyboxparams.exposure = XmlConvert.ToSingle(moduleNode.Attributes["proceduralExposure"].Value);
                        this._proceduralSkybox.skyboxparams.sunsize = XmlConvert.ToSingle(moduleNode.Attributes["proceduralSunsize"].Value);
                        this._proceduralSkybox.skyboxparams.atmospherethickness = XmlConvert.ToSingle(moduleNode.Attributes["proceduralAtmospherethickness"].Value);
                        Color c = Color.black;
                        c.r = XmlConvert.ToSingle(moduleNode.Attributes["proceduralSkytintR"].Value);
                        c.g = XmlConvert.ToSingle(moduleNode.Attributes["proceduralSkytintG"].Value);
                        c.b = XmlConvert.ToSingle(moduleNode.Attributes["proceduralSkytintB"].Value);
                        this._proceduralSkybox.skyboxparams.skytint = c;
                        c.r = XmlConvert.ToSingle(moduleNode.Attributes["proceduralGroundcolorR"].Value);
                        c.g = XmlConvert.ToSingle(moduleNode.Attributes["proceduralGroundcolorG"].Value);
                        c.b = XmlConvert.ToSingle(moduleNode.Attributes["proceduralGroundcolorB"].Value);
                        this._proceduralSkybox.skyboxparams.groundcolor = c;

                        this._skybox.skyboxparams.rotation = XmlConvert.ToSingle(moduleNode.Attributes["classicRotation"].Value);
                        this._skybox.skyboxparams.exposure = XmlConvert.ToSingle(moduleNode.Attributes["classicExposure"].Value);
                        c.r = XmlConvert.ToSingle(moduleNode.Attributes["classicTintR"].Value);
                        c.g = XmlConvert.ToSingle(moduleNode.Attributes["classicTintG"].Value);
                        c.b = XmlConvert.ToSingle(moduleNode.Attributes["classicTintB"].Value);
                        this._skybox.skyboxparams.tint = c;

                        RenderSettings.ambientIntensity = XmlConvert.ToSingle(moduleNode.Attributes["ambientIntensity"].Value);

                        break;
                    case "reflection":
                        this._probeComponent.enabled = moduleNode.Attributes["enabled"] == null || XmlConvert.ToBoolean(moduleNode.Attributes["enabled"].Value);
                        this._probeComponent.refreshMode = XmlConvert.ToBoolean(moduleNode.Attributes["refreshOnDemand"].Value) ? ReflectionProbeRefreshMode.ViaScripting : ReflectionProbeRefreshMode.EveryFrame;
                        this._probeComponent.timeSlicingMode = (ReflectionProbeTimeSlicingMode)XmlConvert.ToInt32(moduleNode.Attributes["timeSlicing"].Value);
                        this._reflectionProbeResolution = XmlConvert.ToInt32(moduleNode.Attributes["resolution"].Value);
                        this._probeComponent.resolution = this._possibleReflectionProbeResolutions[this._reflectionProbeResolution];
                        this._probeComponent.intensity = XmlConvert.ToSingle(moduleNode.Attributes["intensity"].Value);
                        if (moduleNode.Attributes["positionX"] != null)
                            this.probeGameObject.transform.position = new Vector3(
                                                                                  XmlConvert.ToSingle(moduleNode.Attributes["positionX"].Value),
                                                                                  XmlConvert.ToSingle(moduleNode.Attributes["positionY"].Value),
                                                                                  XmlConvert.ToSingle(moduleNode.Attributes["positionZ"].Value)
                                                                                 );
                        RenderSettings.reflectionBounces = moduleNode.Attributes["bounces"] != null ? XmlConvert.ToInt32(moduleNode["bounces"].Value) : 1;
                        break;
                    case "defaultLight":
                        bool frontAnchor = XmlConvert.ToBoolean(moduleNode.Attributes["frontAnchoredToCamera"].Value);
                        if (frontAnchor)
                        {
                            this._lightsObj.transform.parent = Camera.main.transform;
                            this._frontDirectionalLight.transform.parent = this._lightsObj.transform;
                            this._lightsObj.transform.localRotation = Studio.Studio.Instance != null ? Quaternion.Euler(Studio.Studio.Instance.sceneInfo.cameraLightRot[0], Studio.Studio.Instance.sceneInfo.cameraLightRot[1], 0f) : this._lightsObjDefaultRotation;
                            this._frontDirectionalLight.transform.localRotation = this._frontLightDefaultRotation;
                        }
                        else
                        {
                            this._lightsObj.transform.parent = null;
                            this._frontDirectionalLight.transform.parent = null;
                            this._frontDirectionalLight.transform.eulerAngles = this._frontRotate;
                        }

                        this._frontRotate.x = XmlConvert.ToSingle(moduleNode.Attributes["frontRotateX"].Value);
                        this._frontRotate.y = XmlConvert.ToSingle(moduleNode.Attributes["frontRotateY"].Value);

                        this._frontDirectionalLight.intensity = XmlConvert.ToSingle(moduleNode.Attributes["frontIntensity"].Value);
                        c = Color.black;
                        c.r = XmlConvert.ToSingle(moduleNode.Attributes["frontColorR"].Value);
                        c.g = XmlConvert.ToSingle(moduleNode.Attributes["frontColorG"].Value);
                        c.b = XmlConvert.ToSingle(moduleNode.Attributes["frontColorB"].Value);
                        this._frontDirectionalLight.color = c;

                        bool backAnchor = XmlConvert.ToBoolean(moduleNode.Attributes["backAnchoredToCamera"].Value);
                        this._backDirectionalLight.transform.parent = backAnchor == false ? null : Camera.main.transform;

                        if (backAnchor)
                        {
                            this._backDirectionalLight.transform.parent = Camera.main.transform;
                            this._backDirectionalLight.transform.localRotation = this._backLightDefaultRotation;
                        }
                        else
                        {
                            this._backDirectionalLight.transform.parent = null;
                            this._backDirectionalLight.transform.eulerAngles = this._backRotate;
                        }

                        this._backRotate.x = XmlConvert.ToSingle(moduleNode.Attributes["backRotateX"].Value);
                        this._backRotate.y = XmlConvert.ToSingle(moduleNode.Attributes["backRotateY"].Value);

                        this._backDirectionalLight.intensity = XmlConvert.ToSingle(moduleNode.Attributes["backIntensity"].Value);
                        c = Color.black;
                        c.r = XmlConvert.ToSingle(moduleNode.Attributes["backColorR"].Value);
                        c.g = XmlConvert.ToSingle(moduleNode.Attributes["backColorG"].Value);
                        c.b = XmlConvert.ToSingle(moduleNode.Attributes["backColorB"].Value);
                        this._backDirectionalLight.color = c;

                        break;
                    case "smaa":
                        if (this._colorCorrectionCurves != null)
                        {
                            SMAA.GlobalSettings settings = this._smaa.settings;
                            SMAA.PredicationSettings predication = this._smaa.predication;
                            SMAA.TemporalSettings temporal = this._smaa.temporal;
                            SMAA.QualitySettings quality = this._smaa.quality;

                            settings.debugPass = (SMAA.DebugPass)XmlConvert.ToInt32(moduleNode.Attributes["debugPass"].Value);
                            settings.quality = (SMAA.QualityPreset)XmlConvert.ToInt32(moduleNode.Attributes["quality"].Value);
                            settings.edgeDetectionMethod = (SMAA.EdgeDetectionMethod)XmlConvert.ToInt32(moduleNode.Attributes["edgeDetectionMethod"].Value);
                            quality.diagonalDetection = XmlConvert.ToBoolean(moduleNode.Attributes["qualityDiagonalDetection"].Value);
                            quality.cornerDetection = XmlConvert.ToBoolean(moduleNode.Attributes["qualityCornerDetection"].Value);
                            quality.threshold = XmlConvert.ToSingle(moduleNode.Attributes["qualityThreshold"].Value);
                            quality.depthThreshold = XmlConvert.ToSingle(moduleNode.Attributes["qualityDepthThreshold"].Value);
                            quality.maxSearchSteps = XmlConvert.ToInt32(moduleNode.Attributes["qualityMaxSearchSteps"].Value);
                            quality.maxDiagonalSearchSteps = XmlConvert.ToInt32(moduleNode.Attributes["qualityMaxDiagonalSearchSteps"].Value);
                            quality.cornerRounding = XmlConvert.ToInt32(moduleNode.Attributes["qualityCornerRounding"].Value);
                            quality.localContrastAdaptationFactor = XmlConvert.ToSingle(moduleNode.Attributes["qualityLocalContrastAdaptationFactor"].Value);
                            predication.enabled = XmlConvert.ToBoolean(moduleNode.Attributes["predicationEnabled"].Value);
                            predication.threshold = XmlConvert.ToSingle(moduleNode.Attributes["predicationThreshold"].Value);
                            predication.scale = XmlConvert.ToSingle(moduleNode.Attributes["predicationScale"].Value);
                            predication.strength = XmlConvert.ToSingle(moduleNode.Attributes["predicationStrength"].Value);
                            temporal.enabled = XmlConvert.ToBoolean(moduleNode.Attributes["temporalEnabled"].Value);
                            temporal.fuzzSize = XmlConvert.ToSingle(moduleNode.Attributes["temporalFuzzSize"].Value);

                            this._smaa.quality = quality;
                            this._smaa.temporal = temporal;
                            this._smaa.predication = predication;
                            this._smaa.settings = settings;
                        }
                        break;
                    case "lens":
                        if (this._colorCorrectionCurves != null)
                        {
                            Camera.main.fieldOfView = XmlConvert.ToSingle(moduleNode.Attributes["fov"].Value);
                            this._lensManager.distortion.enabled = XmlConvert.ToBoolean(moduleNode.Attributes["distortionEnabled"].Value);
                            this._lensManager.distortion.amount = XmlConvert.ToSingle(moduleNode.Attributes["distortionAmount"].Value);
                            this._lensManager.distortion.amountX = XmlConvert.ToSingle(moduleNode.Attributes["distortionAmountX"].Value);
                            this._lensManager.distortion.amountY = XmlConvert.ToSingle(moduleNode.Attributes["distortionAmountY"].Value);
                            this._lensManager.distortion.scale = XmlConvert.ToSingle(moduleNode.Attributes["distortionScale"].Value);

                            this._lensManager.chromaticAberration.enabled = XmlConvert.ToBoolean(moduleNode.Attributes["chromaticAberrationEnabled"].Value);
                            this._lensManager.chromaticAberration.amount = XmlConvert.ToSingle(moduleNode.Attributes["chromaticAberrationAmount"].Value);

                            if (moduleNode.Attributes["chromaticAberrationColorR"] != null)
                            {
                                c = Color.green;
                                c.r = XmlConvert.ToSingle(moduleNode.Attributes["chromaticAberrationColorR"].Value);
                                c.g = XmlConvert.ToSingle(moduleNode.Attributes["chromaticAberrationColorG"].Value);
                                c.b = XmlConvert.ToSingle(moduleNode.Attributes["chromaticAberrationColorB"].Value);
                                this._lensManager.chromaticAberration.color = c;
                            }

                            this._lensManager.vignette.enabled = XmlConvert.ToBoolean(moduleNode.Attributes["vignetteEnabled"].Value);
                            this._lensManager.vignette.intensity = XmlConvert.ToSingle(moduleNode.Attributes["vignetteIntensity"].Value);
                            this._lensManager.vignette.smoothness = XmlConvert.ToSingle(moduleNode.Attributes["vignetteSmoothness"].Value);
                            this._lensManager.vignette.roundness = XmlConvert.ToSingle(moduleNode.Attributes["vignetteRoundness"].Value);
                            this._lensManager.vignette.desaturate = XmlConvert.ToSingle(moduleNode.Attributes["vignetteDesaturate"].Value);
                            this._lensManager.vignette.blur = XmlConvert.ToSingle(moduleNode.Attributes["vignetteBlur"].Value);

                            c = Color.black;
                            c.r = XmlConvert.ToSingle(moduleNode.Attributes["vignetteColorR"].Value);
                            c.g = XmlConvert.ToSingle(moduleNode.Attributes["vignetteColorG"].Value);
                            c.b = XmlConvert.ToSingle(moduleNode.Attributes["vignetteColorB"].Value);
                            this._lensManager.vignette.color = c;
                        }
                        break;
                    case "colorGrading":
                        if (this._colorCorrectionCurves != null)
                        {
                            TonemappingColorGrading.ColorGradingSettings colorGrading = this._toneMappingManager.colorGrading;
                            TonemappingColorGrading.BasicsSettings settings = colorGrading.basics;
                            colorGrading.enabled = XmlConvert.ToBoolean(moduleNode.Attributes["enabled"].Value);
                            settings.temperatureShift = XmlConvert.ToSingle(moduleNode.Attributes["temperatureShift"].Value);
                            settings.tint = XmlConvert.ToSingle(moduleNode.Attributes["tint"].Value);
                            settings.contrast = XmlConvert.ToSingle(moduleNode.Attributes["contrast"].Value);
                            settings.hue = XmlConvert.ToSingle(moduleNode.Attributes["hue"].Value);
                            settings.saturation = XmlConvert.ToSingle(moduleNode.Attributes["saturation"].Value);
                            settings.value = XmlConvert.ToSingle(moduleNode.Attributes["value"].Value);
                            settings.vibrance = XmlConvert.ToSingle(moduleNode.Attributes["vibrance"].Value);
                            settings.gain = XmlConvert.ToSingle(moduleNode.Attributes["gain"].Value);
                            settings.gamma = XmlConvert.ToSingle(moduleNode.Attributes["gamma"].Value);
                            colorGrading.basics = settings;
                            this._toneMappingManager.colorGrading = colorGrading;
                        }
                        break;
                    case "tonemapping":
                        if (this._colorCorrectionCurves != null)
                        {
                            this._tonemappingEnabled = XmlConvert.ToBoolean(moduleNode.Attributes["enabled"].Value);
                            this._toneMapper = (TonemappingColorGrading.Tonemapper)XmlConvert.ToInt32(moduleNode.Attributes["tonemapper"].Value);
                            this._ev = XmlConvert.ToSingle(moduleNode.Attributes["exposure"].Value);

                            this._toneMappingManager.tonemapping = new TonemappingColorGrading.TonemappingSettings
                            {
                                tonemapper = this._toneMapper,
                                exposure = Mathf.Pow(2f, this._ev),
                                enabled = this._tonemappingEnabled,
                                neutralBlackIn = this._toneMappingManager.tonemapping.neutralBlackIn,
                                neutralBlackOut = this._toneMappingManager.tonemapping.neutralBlackOut,
                                neutralWhiteClip = this._toneMappingManager.tonemapping.neutralWhiteClip,
                                neutralWhiteIn = this._toneMappingManager.tonemapping.neutralWhiteIn,
                                neutralWhiteLevel = this._toneMappingManager.tonemapping.neutralWhiteLevel,
                                neutralWhiteOut = this._toneMappingManager.tonemapping.neutralWhiteOut,
                                curve = this._toneMappingManager.tonemapping.curve
                            };
                        }
                        break;
                    case "eyeAdaptation":
                        if (this._colorCorrectionCurves != null)
                        {
                            this._eyeEnabled = XmlConvert.ToBoolean(moduleNode.Attributes["enabled"].Value);
                            this._eyeMiddleGrey = XmlConvert.ToSingle(moduleNode.Attributes["middleGrey"].Value);
                            this._eyeMax = XmlConvert.ToSingle(moduleNode.Attributes["max"].Value);
                            this._eyeMin = XmlConvert.ToSingle(moduleNode.Attributes["min"].Value);
                            this._eyeSpeed = XmlConvert.ToSingle(moduleNode.Attributes["speed"].Value);

                            this._toneMappingManager.eyeAdaptation = new TonemappingColorGrading.EyeAdaptationSettings
                            {
                                enabled = this._eyeEnabled,
                                showDebug = false,
                                middleGrey = this._eyeMiddleGrey,
                                max = this._eyeMax,
                                min = this._eyeMin,
                                speed = this._eyeSpeed
                            };
                        }
                        break;
                    case "bloom":
                        if (this._colorCorrectionCurves != null)
                        {
                            this._bloomManager.settings.intensity = XmlConvert.ToSingle(moduleNode.Attributes["intensity"].Value);
                            this._bloomManager.settings.threshold = XmlConvert.ToSingle(moduleNode.Attributes["threshold"].Value);
                            this._bloomManager.settings.softKnee = XmlConvert.ToSingle(moduleNode.Attributes["softKnee"].Value);
                            this._bloomManager.settings.radius = XmlConvert.ToSingle(moduleNode.Attributes["radius"].Value);
                            this._bloomManager.settings.antiFlicker = XmlConvert.ToBoolean(moduleNode.Attributes["antiFlicker"].Value);
                        }
                        break;
                    case "ssao":
                        if (this._ssao != null)
                        {
                            if (moduleNode.Attributes["enabled"] != null)
                                this._ssao.enabled = XmlConvert.ToBoolean(moduleNode.Attributes["enabled"].Value);
                            this._ssao.Samples = (SSAOPro.SampleCount)XmlConvert.ToInt32(moduleNode.Attributes["samples"].Value);
                            this._ssao.Downsampling = XmlConvert.ToInt32(moduleNode.Attributes["downsampling"].Value);
                            this._ssao.Radius = XmlConvert.ToSingle(moduleNode.Attributes["radius"].Value);
                            this._ssao.Intensity = XmlConvert.ToSingle(moduleNode.Attributes["intensity"].Value);
                            this._ssao.Distance = XmlConvert.ToSingle(moduleNode.Attributes["distance"].Value);
                            this._ssao.Bias = XmlConvert.ToSingle(moduleNode.Attributes["bias"].Value);
                            this._ssao.LumContribution = XmlConvert.ToSingle(moduleNode.Attributes["lumContribution"].Value);
                            c = Color.black;
                            c.r = XmlConvert.ToSingle(moduleNode.Attributes["occlusionColorR"].Value);
                            c.g = XmlConvert.ToSingle(moduleNode.Attributes["occlusionColorG"].Value);
                            c.b = XmlConvert.ToSingle(moduleNode.Attributes["occlusionColorB"].Value);
                            this._ssao.OcclusionColor = c;
                            this._ssao.CutoffDistance = XmlConvert.ToSingle(moduleNode.Attributes["cutoffDistance"].Value);
                            this._ssao.CutoffFalloff = XmlConvert.ToSingle(moduleNode.Attributes["cutoffFalloff"].Value);
                            this._ssao.BlurPasses = XmlConvert.ToInt32(moduleNode.Attributes["blurPasses"].Value);
                            this._ssao.BlurBilateralThreshold = XmlConvert.ToSingle(moduleNode.Attributes["blurBilateralThreshold"].Value);
                            this._ssao.UseHighPrecisionDepthMap = XmlConvert.ToBoolean(moduleNode.Attributes["useHighPrecisionDepthMap"].Value);
                            this._ssao.Blur = (SSAOPro.BlurMode)XmlConvert.ToInt32(moduleNode.Attributes["blur"].Value);
                            this._ssao.BlurDownsampling = XmlConvert.ToBoolean(moduleNode.Attributes["blurDownsampling"].Value);
                            this._ssao.DebugAO = XmlConvert.ToBoolean(moduleNode.Attributes["debugAO"].Value);

                            if (Studio.Studio.Instance != null)
                            {
                                Studio.Studio.Instance.sceneInfo.enableSSAO = this._ssao.enabled;
                                Studio.Studio.Instance.sceneInfo.ssaoIntensity = this._ssao.Intensity;
                                Studio.Studio.Instance.sceneInfo.ssaoColor.SetDiffuseRGBA(c);
                            }
                        }
                        break;
                    case "sunShafts":
                        if (this._sunShafts != null)
                        {
                            if (moduleNode.Attributes["enabled"] != null)
                                this._sunShafts.enabled = XmlConvert.ToBoolean(moduleNode.Attributes["enabled"].Value);
                            this._sunShafts.useDepthTexture = XmlConvert.ToBoolean(moduleNode.Attributes["useDepthTexture"].Value);
                            this._sunShafts.resolution = (SunShafts.SunShaftsResolution)XmlConvert.ToInt32(moduleNode.Attributes["resolution"].Value);
                            this._sunShafts.screenBlendMode = (SunShafts.ShaftsScreenBlendMode)XmlConvert.ToInt32(moduleNode.Attributes["screenBlendMode"].Value);
                            c = Color.black;
                            c.r = XmlConvert.ToSingle(moduleNode.Attributes["sunThresholdR"].Value);
                            c.g = XmlConvert.ToSingle(moduleNode.Attributes["sunThresholdG"].Value);
                            c.b = XmlConvert.ToSingle(moduleNode.Attributes["sunThresholdB"].Value);
                            this._sunShafts.sunThreshold = c;
                            c.r = XmlConvert.ToSingle(moduleNode.Attributes["sunColorR"].Value);
                            c.g = XmlConvert.ToSingle(moduleNode.Attributes["sunColorG"].Value);
                            c.b = XmlConvert.ToSingle(moduleNode.Attributes["sunColorB"].Value);
                            this._sunShafts.sunColor = c;
                            this._sunShafts.maxRadius = XmlConvert.ToSingle(moduleNode.Attributes["maxRadius"].Value);
                            this._sunShafts.sunShaftBlurRadius = XmlConvert.ToSingle(moduleNode.Attributes["sunShaftBlurRadius"].Value);
                            this._sunShafts.radialBlurIterations = XmlConvert.ToInt32(moduleNode.Attributes["radialBlurIterations"].Value);
                            this._sunShafts.sunShaftIntensity = XmlConvert.ToSingle(moduleNode.Attributes["sunShaftIntensity"].Value);
                            if (Studio.Studio.Instance != null)
                            Studio.Studio.Instance.sceneInfo.enableSunShafts = this._sunShafts.enabled;
                        }

                        break;
                    case "depthOfField":
                        if (this._depthOfField != null)
                        {
                            if (moduleNode.Attributes["enabled"] != null)
                                this._depthOfField.enabled = XmlConvert.ToBoolean(moduleNode.Attributes["enabled"].Value);
                            this._depthOfField.visualizeFocus = XmlConvert.ToBoolean(moduleNode.Attributes["visualizeFocus"].Value);
                            this._depthOfField.focalTransform = moduleNode.Attributes["useCameraOriginAsFocus"] == null || XmlConvert.ToBoolean(moduleNode.Attributes["useCameraOriginAsFocus"].Value) ? this._depthOfFieldFocusPoint : null;
                            this._depthOfField.focalLength = XmlConvert.ToSingle(moduleNode.Attributes["focalLength"].Value);
                            this._depthOfField.focalSize = XmlConvert.ToSingle(moduleNode.Attributes["focalSize"].Value);
                            this._depthOfField.aperture = XmlConvert.ToSingle(moduleNode.Attributes["aperture"].Value);
                            this._depthOfField.maxBlurSize = XmlConvert.ToSingle(moduleNode.Attributes["maxBlurSize"].Value);
                            this._depthOfField.highResolution = XmlConvert.ToBoolean(moduleNode.Attributes["highResolution"].Value);
                            this._depthOfField.nearBlur = XmlConvert.ToBoolean(moduleNode.Attributes["nearBlur"].Value);
                            this._depthOfField.foregroundOverlap = XmlConvert.ToSingle(moduleNode.Attributes["foregroundOverlap"].Value);
                            this._depthOfField.blurType = (DepthOfField.BlurType)XmlConvert.ToInt32(moduleNode.Attributes["blurType"].Value);
                            this._depthOfField.blurSampleCount = (DepthOfField.BlurSampleCount)XmlConvert.ToInt32(moduleNode.Attributes["blurSampleCount"].Value);
                            this._depthOfField.dx11BokehScale = XmlConvert.ToSingle(moduleNode.Attributes["dx11BokehScale"].Value);
                            this._depthOfField.dx11BokehIntensity = XmlConvert.ToSingle(moduleNode.Attributes["dx11BokehIntensity"].Value);
                            this._depthOfField.dx11BokehThreshold = XmlConvert.ToSingle(moduleNode.Attributes["dx11BokehThreshold"].Value);
                            this._depthOfField.dx11SpawnHeuristic = XmlConvert.ToSingle(moduleNode.Attributes["dx11SpawnHeuristic"].Value);
                            Studio.Studio.Instance.sceneInfo.enableDepth = this._depthOfField.enabled;
                            Studio.Studio.Instance.sceneInfo.depthFocalSize = this._depthOfField.focalSize;
                            Studio.Studio.Instance.sceneInfo.depthAperture = this._depthOfField.aperture;
                        }
                        break;
                    case "ssr":
                        if (this._ssr != null)
                        {
                            if (moduleNode.Attributes["enabled"] != null)
                                this._ssr.enabled = XmlConvert.ToBoolean(moduleNode.Attributes["enabled"].Value);

                            ScreenSpaceReflection.SSRSettings ssrSettings = this._ssr.settings;

                            ScreenSpaceReflection.BasicSettings basicSettings = ssrSettings.basicSettings;
                            basicSettings.reflectionMultiplier = XmlConvert.ToSingle(moduleNode.Attributes["reflectionMultiplier"].Value);
                            basicSettings.maxDistance = XmlConvert.ToSingle(moduleNode.Attributes["maxDistance"].Value);
                            basicSettings.fadeDistance = XmlConvert.ToSingle(moduleNode.Attributes["fadeDistance"].Value);
                            basicSettings.screenEdgeFading = XmlConvert.ToSingle(moduleNode.Attributes["screenEdgeFading"].Value);
                            basicSettings.enableHDR = XmlConvert.ToBoolean(moduleNode.Attributes["enableHDR"].Value);
                            basicSettings.additiveReflection = XmlConvert.ToBoolean(moduleNode.Attributes["additiveReflection"].Value);

                            ScreenSpaceReflection.ReflectionSettings reflectionSettings = ssrSettings.reflectionSettings;
                            reflectionSettings.maxSteps = XmlConvert.ToInt32(moduleNode.Attributes["maxSteps"].Value);
                            reflectionSettings.rayStepSize = XmlConvert.ToInt32(moduleNode.Attributes["rayStepSize"].Value);
                            reflectionSettings.widthModifier = XmlConvert.ToSingle(moduleNode.Attributes["widthModifier"].Value);
                            reflectionSettings.smoothFallbackThreshold = XmlConvert.ToSingle(moduleNode.Attributes["smoothFallbackThreshold"].Value);
                            reflectionSettings.smoothFallbackDistance = XmlConvert.ToSingle(moduleNode.Attributes["smoothFallbackDistance"].Value);
                            reflectionSettings.fresnelFade = XmlConvert.ToSingle(moduleNode.Attributes["fresnelFade"].Value);
                            reflectionSettings.fresnelFadePower = XmlConvert.ToSingle(moduleNode.Attributes["fresnelFadePower"].Value);
                            reflectionSettings.distanceBlur = XmlConvert.ToSingle(moduleNode.Attributes["distanceBlur"].Value);

                            ScreenSpaceReflection.AdvancedSettings advancedSettings = ssrSettings.advancedSettings;
                            advancedSettings.temporalFilterStrength = XmlConvert.ToSingle(moduleNode.Attributes["temporalFilterStrength"].Value);
                            advancedSettings.useTemporalConfidence = XmlConvert.ToBoolean(moduleNode.Attributes["useTemporalConfidence"].Value);
                            advancedSettings.traceBehindObjects = XmlConvert.ToBoolean(moduleNode.Attributes["traceBehindObjects"].Value);
                            advancedSettings.highQualitySharpReflections = XmlConvert.ToBoolean(moduleNode.Attributes["highQualitySharpReflections"].Value);
                            advancedSettings.traceEverywhere = XmlConvert.ToBoolean(moduleNode.Attributes["traceEverywhere"].Value);
                            advancedSettings.treatBackfaceHitAsMiss = XmlConvert.ToBoolean(moduleNode.Attributes["treatBackfaceHitAsMiss"].Value);
                            advancedSettings.allowBackwardsRays = XmlConvert.ToBoolean(moduleNode.Attributes["allowBackwardsRays"].Value);
                            advancedSettings.improveCorners = XmlConvert.ToBoolean(moduleNode.Attributes["improveCorners"].Value);
                            advancedSettings.resolution = (ScreenSpaceReflection.SSRResolution)XmlConvert.ToInt32(moduleNode.Attributes["resolution"].Value);
                            advancedSettings.bilateralUpsample = XmlConvert.ToBoolean(moduleNode.Attributes["bilateralUpsample"].Value);
                            advancedSettings.reduceBanding = XmlConvert.ToBoolean(moduleNode.Attributes["reduceBanding"].Value);
                            advancedSettings.highlightSuppression = XmlConvert.ToBoolean(moduleNode.Attributes["highlightSuppression"].Value);

                            ssrSettings.basicSettings = basicSettings;
                            ssrSettings.reflectionSettings = reflectionSettings;
                            ssrSettings.advancedSettings = advancedSettings;
                            this._ssr.settings = ssrSettings;
                            Studio.Studio.Instance.sceneInfo.enableSSR = this._ssr.enabled;
                        }

                        break;
                }
            }
            if (this._selectedCubeMap == 0)
            {
                this._proceduralSkybox.ApplySkyboxParams();
                this._environmentUpdateFlag = true;
                this._tempproceduralskyboxparams = this._proceduralSkybox.skyboxparams;
                this._previousambientintensity = RenderSettings.ambientIntensity;
            }
            else if (this._selectedCubeMap > 0)
            {
                this._skybox.ApplySkyboxParams();
                this._environmentUpdateFlag = true;
                this._tempskyboxparams = this._skybox.skyboxparams;
                this._previousambientintensity = RenderSettings.ambientIntensity;
            }
        }

        private void SaveConfig(XmlWriter writer)
        {
            writer.WriteAttributeString("version", Assembly.GetExecutingAssembly().GetName().Version.ToString());

            {
                writer.WriteStartElement("cubemap");
                writer.WriteAttributeString("hide", XmlConvert.ToString(this._hideSkybox));
                writer.WriteAttributeString("index", XmlConvert.ToString(this._selectedCubeMap));
                string fileName = "";
                if (this._selectedCubeMap > 0)
                    fileName = this._cubemapFolder.lstFile[this._selectedCubeMap - 1].FileName;
                writer.WriteAttributeString("fileName", fileName);
                writer.WriteEndElement();
            }

            {
                writer.WriteStartElement("skybox");
                writer.WriteAttributeString("proceduralExposure", XmlConvert.ToString(this._proceduralSkybox.skyboxparams.exposure));
                writer.WriteAttributeString("proceduralSunsize", XmlConvert.ToString(this._proceduralSkybox.skyboxparams.sunsize));
                writer.WriteAttributeString("proceduralAtmospherethickness", XmlConvert.ToString(this._proceduralSkybox.skyboxparams.atmospherethickness));
                writer.WriteAttributeString("proceduralSkytintR", XmlConvert.ToString(this._proceduralSkybox.skyboxparams.skytint.r));
                writer.WriteAttributeString("proceduralSkytintG", XmlConvert.ToString(this._proceduralSkybox.skyboxparams.skytint.g));
                writer.WriteAttributeString("proceduralSkytintB", XmlConvert.ToString(this._proceduralSkybox.skyboxparams.skytint.b));
                writer.WriteAttributeString("proceduralGroundcolorR", XmlConvert.ToString(this._proceduralSkybox.skyboxparams.groundcolor.r));
                writer.WriteAttributeString("proceduralGroundcolorG", XmlConvert.ToString(this._proceduralSkybox.skyboxparams.groundcolor.g));
                writer.WriteAttributeString("proceduralGroundcolorB", XmlConvert.ToString(this._proceduralSkybox.skyboxparams.groundcolor.b));

                writer.WriteAttributeString("classicRotation", XmlConvert.ToString(this._skybox.skyboxparams.rotation));
                writer.WriteAttributeString("classicExposure", XmlConvert.ToString(this._skybox.skyboxparams.exposure));
                writer.WriteAttributeString("classicTintR", XmlConvert.ToString(this._skybox.skyboxparams.tint.r));
                writer.WriteAttributeString("classicTintG", XmlConvert.ToString(this._skybox.skyboxparams.tint.g));
                writer.WriteAttributeString("classicTintB", XmlConvert.ToString(this._skybox.skyboxparams.tint.b));

                writer.WriteAttributeString("ambientIntensity", XmlConvert.ToString(RenderSettings.ambientIntensity));
                writer.WriteEndElement();
            }

            {
                writer.WriteStartElement("reflection");
                writer.WriteAttributeString("enabled", XmlConvert.ToString(this._probeComponent.enabled));
                writer.WriteAttributeString("refreshOnDemand", XmlConvert.ToString(this._probeComponent.refreshMode == ReflectionProbeRefreshMode.ViaScripting));
                writer.WriteAttributeString("timeSlicing", XmlConvert.ToString((int)this._probeComponent.timeSlicingMode));
                writer.WriteAttributeString("resolution", XmlConvert.ToString(this._reflectionProbeResolution));
                writer.WriteAttributeString("intensity", XmlConvert.ToString(this._probeComponent.intensity));
                writer.WriteAttributeString("positionX", XmlConvert.ToString(this.probeGameObject.transform.position.x));
                writer.WriteAttributeString("positionY", XmlConvert.ToString(this.probeGameObject.transform.position.y));
                writer.WriteAttributeString("positionZ", XmlConvert.ToString(this.probeGameObject.transform.position.z));
                writer.WriteAttributeString("bounce", XmlConvert.ToString(RenderSettings.reflectionBounces));
                writer.WriteEndElement();
            }

            {
                writer.WriteStartElement("defaultLight");

                writer.WriteAttributeString("frontAnchoredToCamera", XmlConvert.ToString(this._frontDirectionalLight.transform.parent != null));
                writer.WriteAttributeString("frontRotateX", XmlConvert.ToString(this._frontRotate.x));
                writer.WriteAttributeString("frontRotateY", XmlConvert.ToString(this._frontRotate.y));
                writer.WriteAttributeString("frontIntensity", XmlConvert.ToString(this._frontDirectionalLight.intensity));
                writer.WriteAttributeString("frontColorR", XmlConvert.ToString(this._frontDirectionalLight.color.r));
                writer.WriteAttributeString("frontColorG", XmlConvert.ToString(this._frontDirectionalLight.color.g));
                writer.WriteAttributeString("frontColorB", XmlConvert.ToString(this._frontDirectionalLight.color.b));

                writer.WriteAttributeString("backAnchoredToCamera", XmlConvert.ToString(this._backDirectionalLight.transform.parent != null));
                writer.WriteAttributeString("backRotateX", XmlConvert.ToString(this._backRotate.x));
                writer.WriteAttributeString("backRotateY", XmlConvert.ToString(this._backRotate.y));
                writer.WriteAttributeString("backIntensity", XmlConvert.ToString(this._backDirectionalLight.intensity));
                writer.WriteAttributeString("backColorR", XmlConvert.ToString(this._backDirectionalLight.color.r));
                writer.WriteAttributeString("backColorG", XmlConvert.ToString(this._backDirectionalLight.color.g));
                writer.WriteAttributeString("backColorB", XmlConvert.ToString(this._backDirectionalLight.color.b));

                writer.WriteEndElement();
            }

            if (this._colorCorrectionCurves != null)
            {
                SMAA.GlobalSettings settings = this._smaa.settings;
                SMAA.PredicationSettings predication = this._smaa.predication;
                SMAA.TemporalSettings temporal = this._smaa.temporal;
                SMAA.QualitySettings quality = this._smaa.quality;

                writer.WriteStartElement("smaa");

                writer.WriteAttributeString("debugPass", XmlConvert.ToString((int)settings.debugPass));
                writer.WriteAttributeString("quality", XmlConvert.ToString((int)settings.quality));
                writer.WriteAttributeString("edgeDetectionMethod", XmlConvert.ToString((int)settings.edgeDetectionMethod));
                writer.WriteAttributeString("qualityDiagonalDetection", XmlConvert.ToString(quality.diagonalDetection));
                writer.WriteAttributeString("qualityCornerDetection", XmlConvert.ToString(quality.cornerDetection));
                writer.WriteAttributeString("qualityThreshold", XmlConvert.ToString(quality.threshold));
                writer.WriteAttributeString("qualityDepthThreshold", XmlConvert.ToString(quality.depthThreshold));
                writer.WriteAttributeString("qualityMaxSearchSteps", XmlConvert.ToString(quality.maxSearchSteps));
                writer.WriteAttributeString("qualityMaxDiagonalSearchSteps", XmlConvert.ToString(quality.maxDiagonalSearchSteps));
                writer.WriteAttributeString("qualityCornerRounding", XmlConvert.ToString(quality.cornerRounding));
                writer.WriteAttributeString("qualityLocalContrastAdaptationFactor", XmlConvert.ToString(quality.localContrastAdaptationFactor));
                writer.WriteAttributeString("predicationEnabled", XmlConvert.ToString(predication.enabled));
                writer.WriteAttributeString("predicationThreshold", XmlConvert.ToString(predication.threshold));
                writer.WriteAttributeString("predicationScale", XmlConvert.ToString(predication.scale));
                writer.WriteAttributeString("predicationStrength", XmlConvert.ToString(predication.strength));
                writer.WriteAttributeString("temporalEnabled", XmlConvert.ToString(temporal.enabled));
                writer.WriteAttributeString("temporalFuzzSize", XmlConvert.ToString(temporal.fuzzSize));

                writer.WriteEndElement();
            }

            if (this._colorCorrectionCurves != null)
            {
                writer.WriteStartElement("lens");

                writer.WriteAttributeString("fov", XmlConvert.ToString(Camera.main.fieldOfView));
                writer.WriteAttributeString("distortionEnabled", XmlConvert.ToString(this._lensManager.distortion.enabled));
                writer.WriteAttributeString("distortionAmount", XmlConvert.ToString(this._lensManager.distortion.amount));
                writer.WriteAttributeString("distortionAmountX", XmlConvert.ToString(this._lensManager.distortion.amountX));
                writer.WriteAttributeString("distortionAmountY", XmlConvert.ToString(this._lensManager.distortion.amountY));
                writer.WriteAttributeString("distortionScale", XmlConvert.ToString(this._lensManager.distortion.scale));

                writer.WriteAttributeString("chromaticAberrationEnabled", XmlConvert.ToString(this._lensManager.chromaticAberration.enabled));
                writer.WriteAttributeString("chromaticAberrationAmount", XmlConvert.ToString(this._lensManager.chromaticAberration.amount));
                writer.WriteAttributeString("chromaticAberrationColorR", XmlConvert.ToString(this._lensManager.chromaticAberration.color.r));
                writer.WriteAttributeString("chromaticAberrationColorG", XmlConvert.ToString(this._lensManager.chromaticAberration.color.g));
                writer.WriteAttributeString("chromaticAberrationColorB", XmlConvert.ToString(this._lensManager.chromaticAberration.color.b));

                writer.WriteAttributeString("vignetteEnabled", XmlConvert.ToString(this._lensManager.vignette.enabled));
                writer.WriteAttributeString("vignetteIntensity", XmlConvert.ToString(this._lensManager.vignette.intensity));
                writer.WriteAttributeString("vignetteSmoothness", XmlConvert.ToString(this._lensManager.vignette.smoothness));
                writer.WriteAttributeString("vignetteRoundness", XmlConvert.ToString(this._lensManager.vignette.roundness));
                writer.WriteAttributeString("vignetteDesaturate", XmlConvert.ToString(this._lensManager.vignette.desaturate));
                writer.WriteAttributeString("vignetteBlur", XmlConvert.ToString(this._lensManager.vignette.blur));
                writer.WriteAttributeString("vignetteColorR", XmlConvert.ToString(this._lensManager.vignette.color.r));
                writer.WriteAttributeString("vignetteColorG", XmlConvert.ToString(this._lensManager.vignette.color.g));
                writer.WriteAttributeString("vignetteColorB", XmlConvert.ToString(this._lensManager.vignette.color.b));

                writer.WriteEndElement();
            }

            if (this._colorCorrectionCurves != null)
            {
                writer.WriteStartElement("tonemapping");
                writer.WriteAttributeString("enabled", XmlConvert.ToString(this._toneMappingManager.tonemapping.enabled));
                writer.WriteAttributeString("tonemapper", XmlConvert.ToString((int)this._toneMappingManager.tonemapping.tonemapper));
                writer.WriteAttributeString("exposure", XmlConvert.ToString(this._ev));
                writer.WriteEndElement();
            }

            if (this._colorCorrectionCurves != null)
            {
                TonemappingColorGrading.ColorGradingSettings colorGrading = this._toneMappingManager.colorGrading;
                TonemappingColorGrading.BasicsSettings settings = colorGrading.basics;

                writer.WriteStartElement("colorGrading");
                writer.WriteAttributeString("enabled", XmlConvert.ToString(colorGrading.enabled));
                writer.WriteAttributeString("temperatureShift", XmlConvert.ToString(settings.temperatureShift));
                writer.WriteAttributeString("tint", XmlConvert.ToString(settings.tint));
                writer.WriteAttributeString("contrast", XmlConvert.ToString(settings.contrast));
                writer.WriteAttributeString("hue", XmlConvert.ToString(settings.hue));
                writer.WriteAttributeString("saturation", XmlConvert.ToString(settings.saturation));
                writer.WriteAttributeString("value", XmlConvert.ToString(settings.value));
                writer.WriteAttributeString("vibrance", XmlConvert.ToString(settings.vibrance));
                writer.WriteAttributeString("gain", XmlConvert.ToString(settings.gain));
                writer.WriteAttributeString("gamma", XmlConvert.ToString(settings.gamma));
                writer.WriteEndElement();
            }

            if (this._colorCorrectionCurves != null)
            {
                writer.WriteStartElement("eyeAdaptation");
                writer.WriteAttributeString("enabled", XmlConvert.ToString(this._toneMappingManager.eyeAdaptation.enabled));
                writer.WriteAttributeString("middleGrey", XmlConvert.ToString(this._toneMappingManager.eyeAdaptation.middleGrey));
                writer.WriteAttributeString("min", XmlConvert.ToString(this._toneMappingManager.eyeAdaptation.min));
                writer.WriteAttributeString("max", XmlConvert.ToString(this._toneMappingManager.eyeAdaptation.max));
                writer.WriteAttributeString("speed", XmlConvert.ToString(this._toneMappingManager.eyeAdaptation.speed));
                writer.WriteEndElement();
            }

            if (this._colorCorrectionCurves != null)
            {
                writer.WriteStartElement("bloom");
                writer.WriteAttributeString("intensity", XmlConvert.ToString(this._bloomManager.settings.intensity));
                writer.WriteAttributeString("threshold", XmlConvert.ToString(this._bloomManager.settings.threshold));
                writer.WriteAttributeString("softKnee", XmlConvert.ToString(this._bloomManager.settings.softKnee));
                writer.WriteAttributeString("radius", XmlConvert.ToString(this._bloomManager.settings.radius));
                writer.WriteAttributeString("antiFlicker", XmlConvert.ToString(this._bloomManager.settings.antiFlicker));
                writer.WriteEndElement();
            }

            if (this._ssao != null)
            {
                writer.WriteStartElement("ssao");
                writer.WriteAttributeString("enabled", XmlConvert.ToString(this._ssao.enabled));
                writer.WriteAttributeString("samples", XmlConvert.ToString((int)this._ssao.Samples));
                writer.WriteAttributeString("downsampling", XmlConvert.ToString(this._ssao.Downsampling));
                writer.WriteAttributeString("radius", XmlConvert.ToString(this._ssao.Radius));
                writer.WriteAttributeString("intensity", XmlConvert.ToString(this._ssao.Intensity));
                writer.WriteAttributeString("distance", XmlConvert.ToString(this._ssao.Distance));
                writer.WriteAttributeString("bias", XmlConvert.ToString(this._ssao.Bias));
                writer.WriteAttributeString("lumContribution", XmlConvert.ToString(this._ssao.LumContribution));
                writer.WriteAttributeString("occlusionColorR", XmlConvert.ToString(this._ssao.OcclusionColor.r));
                writer.WriteAttributeString("occlusionColorG", XmlConvert.ToString(this._ssao.OcclusionColor.g));
                writer.WriteAttributeString("occlusionColorB", XmlConvert.ToString(this._ssao.OcclusionColor.b));
                writer.WriteAttributeString("cutoffDistance", XmlConvert.ToString(this._ssao.CutoffDistance));
                writer.WriteAttributeString("cutoffFalloff", XmlConvert.ToString(this._ssao.CutoffFalloff));
                writer.WriteAttributeString("blurPasses", XmlConvert.ToString(this._ssao.BlurPasses));
                writer.WriteAttributeString("blurBilateralThreshold", XmlConvert.ToString(this._ssao.BlurBilateralThreshold));
                writer.WriteAttributeString("useHighPrecisionDepthMap", XmlConvert.ToString(this._ssao.UseHighPrecisionDepthMap));
                writer.WriteAttributeString("blur", XmlConvert.ToString((int)this._ssao.Blur));
                writer.WriteAttributeString("blurDownsampling", XmlConvert.ToString(this._ssao.BlurDownsampling));
                writer.WriteAttributeString("debugAO", XmlConvert.ToString(this._ssao.DebugAO));
                writer.WriteEndElement();
            }

            if (this._sunShafts != null)
            {
                writer.WriteStartElement("sunShafts");
                writer.WriteAttributeString("enabled", XmlConvert.ToString(this._sunShafts.enabled));
                writer.WriteAttributeString("useDepthTexture", XmlConvert.ToString(this._sunShafts.useDepthTexture));
                writer.WriteAttributeString("resolution", XmlConvert.ToString((int)this._sunShafts.resolution));
                writer.WriteAttributeString("screenBlendMode", XmlConvert.ToString((int)this._sunShafts.screenBlendMode));
                writer.WriteAttributeString("sunThresholdR", XmlConvert.ToString(this._sunShafts.sunThreshold.r));
                writer.WriteAttributeString("sunThresholdG", XmlConvert.ToString(this._sunShafts.sunThreshold.g));
                writer.WriteAttributeString("sunThresholdB", XmlConvert.ToString(this._sunShafts.sunThreshold.b));
                writer.WriteAttributeString("sunColorR", XmlConvert.ToString(this._sunShafts.sunColor.r));
                writer.WriteAttributeString("sunColorG", XmlConvert.ToString(this._sunShafts.sunColor.g));
                writer.WriteAttributeString("sunColorB", XmlConvert.ToString(this._sunShafts.sunColor.b));
                writer.WriteAttributeString("maxRadius", XmlConvert.ToString(this._sunShafts.maxRadius));
                writer.WriteAttributeString("sunShaftBlurRadius", XmlConvert.ToString(this._sunShafts.sunShaftBlurRadius));
                writer.WriteAttributeString("radialBlurIterations", XmlConvert.ToString(this._sunShafts.radialBlurIterations));
                writer.WriteAttributeString("sunShaftIntensity", XmlConvert.ToString(this._sunShafts.sunShaftIntensity));
                writer.WriteEndElement();
            }

            if (this._depthOfField != null)
            {
                writer.WriteStartElement("depthOfField");
                writer.WriteAttributeString("enabled", XmlConvert.ToString(this._depthOfField.enabled));
                writer.WriteAttributeString("visualizeFocus", XmlConvert.ToString(this._depthOfField.visualizeFocus));
                writer.WriteAttributeString("useCameraOriginAsFocus", XmlConvert.ToString(this._depthOfField.focalTransform != null));
                writer.WriteAttributeString("focalLength", XmlConvert.ToString(this._depthOfField.focalLength));
                writer.WriteAttributeString("focalSize", XmlConvert.ToString(this._depthOfField.focalSize));
                writer.WriteAttributeString("aperture", XmlConvert.ToString(this._depthOfField.aperture));
                writer.WriteAttributeString("maxBlurSize", XmlConvert.ToString(this._depthOfField.maxBlurSize));
                writer.WriteAttributeString("highResolution", XmlConvert.ToString(this._depthOfField.highResolution));
                writer.WriteAttributeString("nearBlur", XmlConvert.ToString(this._depthOfField.nearBlur));
                writer.WriteAttributeString("foregroundOverlap", XmlConvert.ToString(this._depthOfField.foregroundOverlap));
                writer.WriteAttributeString("blurType", XmlConvert.ToString((int)this._depthOfField.blurType));
                writer.WriteAttributeString("blurSampleCount", XmlConvert.ToString((int)this._depthOfField.blurSampleCount));
                writer.WriteAttributeString("dx11BokehScale", XmlConvert.ToString(this._depthOfField.dx11BokehScale));
                writer.WriteAttributeString("dx11BokehIntensity", XmlConvert.ToString(this._depthOfField.dx11BokehIntensity));
                writer.WriteAttributeString("dx11BokehThreshold", XmlConvert.ToString(this._depthOfField.dx11BokehThreshold));
                writer.WriteAttributeString("dx11SpawnHeuristic", XmlConvert.ToString(this._depthOfField.dx11SpawnHeuristic));
                writer.WriteEndElement();
            }

            if (this._ssr != null)
            {
                writer.WriteStartElement("ssr");

                writer.WriteAttributeString("enabled", XmlConvert.ToString(this._ssr.enabled));

                ScreenSpaceReflection.SSRSettings ssrSettings = this._ssr.settings;

                ScreenSpaceReflection.BasicSettings basicSettings = ssrSettings.basicSettings;
                {
                    writer.WriteAttributeString("reflectionMultiplier", XmlConvert.ToString(basicSettings.reflectionMultiplier));
                    writer.WriteAttributeString("maxDistance", XmlConvert.ToString(basicSettings.maxDistance));
                    writer.WriteAttributeString("fadeDistance", XmlConvert.ToString(basicSettings.fadeDistance));
                    writer.WriteAttributeString("screenEdgeFading", XmlConvert.ToString(basicSettings.screenEdgeFading));
                    writer.WriteAttributeString("enableHDR", XmlConvert.ToString(basicSettings.enableHDR));
                    writer.WriteAttributeString("additiveReflection", XmlConvert.ToString(basicSettings.additiveReflection));
                }

                ScreenSpaceReflection.ReflectionSettings reflectionSettings = ssrSettings.reflectionSettings;
                {
                    writer.WriteAttributeString("maxSteps", XmlConvert.ToString(reflectionSettings.maxSteps));
                    writer.WriteAttributeString("rayStepSize", XmlConvert.ToString(reflectionSettings.rayStepSize));
                    writer.WriteAttributeString("widthModifier", XmlConvert.ToString(reflectionSettings.widthModifier));
                    writer.WriteAttributeString("smoothFallbackThreshold", XmlConvert.ToString(reflectionSettings.smoothFallbackThreshold));
                    writer.WriteAttributeString("smoothFallbackDistance", XmlConvert.ToString(reflectionSettings.smoothFallbackDistance));
                    writer.WriteAttributeString("fresnelFade", XmlConvert.ToString(reflectionSettings.fresnelFade));
                    writer.WriteAttributeString("fresnelFadePower", XmlConvert.ToString(reflectionSettings.fresnelFadePower));
                    writer.WriteAttributeString("distanceBlur", XmlConvert.ToString(reflectionSettings.distanceBlur));
                }

                ScreenSpaceReflection.AdvancedSettings advancedSettings = ssrSettings.advancedSettings;
                {
                    writer.WriteAttributeString("temporalFilterStrength", XmlConvert.ToString(advancedSettings.temporalFilterStrength));
                    writer.WriteAttributeString("useTemporalConfidence", XmlConvert.ToString(advancedSettings.useTemporalConfidence));
                    writer.WriteAttributeString("traceBehindObjects", XmlConvert.ToString(advancedSettings.traceBehindObjects));
                    writer.WriteAttributeString("highQualitySharpReflections", XmlConvert.ToString(advancedSettings.highQualitySharpReflections));
                    writer.WriteAttributeString("traceEverywhere", XmlConvert.ToString(advancedSettings.traceEverywhere));
                    writer.WriteAttributeString("treatBackfaceHitAsMiss", XmlConvert.ToString(advancedSettings.treatBackfaceHitAsMiss));
                    writer.WriteAttributeString("allowBackwardsRays", XmlConvert.ToString(advancedSettings.allowBackwardsRays));
                    writer.WriteAttributeString("improveCorners", XmlConvert.ToString(advancedSettings.improveCorners));
                    writer.WriteAttributeString("resolution", XmlConvert.ToString((int)advancedSettings.resolution));
                    writer.WriteAttributeString("bilateralUpsample", XmlConvert.ToString(advancedSettings.bilateralUpsample));
                    writer.WriteAttributeString("reduceBanding", XmlConvert.ToString(advancedSettings.reduceBanding));
                    writer.WriteAttributeString("highlightSuppression", XmlConvert.ToString(advancedSettings.highlightSuppression));
                }
                writer.WriteEndElement();
            }

        }

        #endregion
    }
}

