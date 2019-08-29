#if HONEYSELECT
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Harmony;
using IllusionPlugin;
using Studio;
using UnityEngine;
using VideoExport.ScreenshotPlugins;
using VideoExport.Extensions;
#elif KOIKATSU
using BepInEx;
using UnityEngine.SceneManagement;
#endif

namespace VideoExport
{
#if KOIKATSU
    [BepInPlugin(GUID: "com.joan6694.illusionplugins.videoexport", Name: "VideoExport", Version: VideoExportPlugin.versionNum)]
    [BepInProcess("CharaStudio")]
#endif
#if HONEYSELECT
    public class VideoExportPlugin : IEnhancedPlugin
    {
        public string Name { get { return "VideoExport"; } }
        public string Version { get { return VideoExport.versionNum; } }
        public string[] Filter { get { return new[] { "StudioNEO_32", "StudioNEO_64" }; } }

        public void OnApplicationStart()
        {
            new GameObject("VideoExport", typeof(VideoExport));
        }
        public void OnApplicationQuit(){}
        public void OnLevelWasLoaded(int level){}
        public void OnLevelWasInitialized(int level){}
        public void OnUpdate(){}
        public void OnFixedUpdate(){}
        public void OnLateUpdate(){}
    }

    public class VideoExport : MonoBehaviour
#elif KOIKATSU
    public class VideoExportPlugin : BaseUnityPlugin
#endif
    {
        public const string versionNum = "1.0.0";

        #region Private Types
        private enum LimitDurationType
        {
            Frames,
            Seconds,
            Animation
        }

        private enum UpdateDynamicBonesType
        {
            Default,
            EveryFrame
        }
        #endregion

        #region Private Variables
        private static string _outputFolder = _pluginFolder + "Output/";
        private static string _globalFramesFolder = _pluginFolder + "Frames/";
        private static readonly GUIStyle _customBoxStyle = new GUIStyle { normal = new GUIStyleState { background = Texture2D.whiteTexture } };

        private string[] _limitDurationTypeNames;
        private string[] _extensionsNames;
        private readonly string[] _updateDynamicBonesTypeNames = {"Default", "Every Frame"};

        private bool _isRecording = false;
        private bool _breakRecording = false;
        private bool _generatingVideo = false;
        private readonly List<IScreenshotPlugin> _screenshotPlugins = new List<IScreenshotPlugin>();
        private int _randomId;
        private Rect _windowRect = new Rect(Screen.width / 2 - 160, Screen.height / 2 - 100, 320, 10);
        private bool _showUi = false;
        private bool _mouseInWindow;
        private string _currentMessage;
        private readonly List<IExtension> _extensions = new List<IExtension>();
        private Color _messageColor = Color.white;
        private float _progressBarPercentage;
        private bool _startOnNextClick = false;
        private Animator _currentAnimator;
        private float _lastAnimationNormalizedTime;
        private bool _animationIsPlaying;
        private int _currentRecordingFrame;
        private double _currentRecordingTime;
        private int _recordingFrameLimit;

        private int _selectedPlugin = 0;
        private int _fps = 60;
        private bool _autoGenerateVideo;
        private bool _autoDeleteImages;
        private bool _limitDuration;
        private LimitDurationType _selectedLimitDuration;
        private float _limitDurationNumber = 600;
        private ExtensionsType _selectedExtension;
        private bool _resize;
        private int _resizeX;
        private int _resizeY;
        private UpdateDynamicBonesType _selectedUpdateDynamicBones;
        private int _prewarmLoopCount = 3;
        private string _imagesPrefix = "";
        private string _imagesPostfix = "";
        #endregion

        #region Public Variables
        public const string _pluginFolder = "Plugins/VideoExport/";
        #endregion

        #region Public Accessors (for other plugins probably)
        public bool isRecording { get { return this._isRecording; } }
        public int currentRecordingFrame { get { return this._currentRecordingFrame; } }
        public double currentRecordingTime { get { return this._currentRecordingTime; } }
        public int recordingFrameLimit { get { return this._recordingFrameLimit; } }
        #endregion

        #region Unity Methods
        void Awake()
        {

            HarmonyInstance harmony = HarmonyInstance.Create("com.joan6694.illusionplugins.videoexport");
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            this._selectedPlugin = ModPrefs.GetInt("VideoExport", "selectedScreenshotPlugin", 0, true);
            this._fps = ModPrefs.GetInt("VideoExport", "framerate", 60, true);
            this._autoGenerateVideo = ModPrefs.GetBool("VideoExport", "autoGenerateVideo", true, true);
            this._autoDeleteImages = ModPrefs.GetBool("VideoExport", "autoDeleteImages", true, true);
            this._limitDuration = ModPrefs.GetBool("VideoExport", "limitDuration", false, true);
            this._selectedLimitDuration = (LimitDurationType)ModPrefs.GetInt("VideoExport", "selectedLimitDurationType", (int)LimitDurationType.Frames, true);
            this._limitDurationNumber = ModPrefs.GetFloat("VideoExport", "limitDurationNumber", 0, true);
            this._selectedExtension = (ExtensionsType)ModPrefs.GetInt("VideoExport", "selectedExtension", (int)ExtensionsType.MP4, true);
            this._resize = ModPrefs.GetBool("VideoExport", "resize", false, true);
            this._resizeX = ModPrefs.GetInt("VideoExport", "resizeX", Screen.width, true);
            this._resizeY = ModPrefs.GetInt("VideoExport", "resizeY", Screen.height, true);
            this._selectedUpdateDynamicBones = (UpdateDynamicBonesType)ModPrefs.GetInt("VideoExport", "selectedUpdateDynamicBonesMode", (int)UpdateDynamicBonesType.Default, true);
            this._prewarmLoopCount = ModPrefs.GetInt("VideoExport", "prewarmLoopCount", 3, true);
            this._imagesPrefix = ModPrefs.GetString("VideoExport", "imagesPrefix", "", true);
            this._imagesPostfix = ModPrefs.GetString("VideoExport", "imagesPostfix", "", true);
            _outputFolder = ModPrefs.GetString("VideoExport", "outputFolder", _outputFolder, true);
            _globalFramesFolder = ModPrefs.GetString("VideoExport", "framesFolder", _globalFramesFolder, true);
#if HONEYSELECT
            DontDestroyOnLoad(this.gameObject);
#elif KOIKATSU
#endif
            this._extensions.Add(new MP4Extension());
            this._extensions.Add(new WEBMExtension());
            this._extensions.Add(new GIFExtension());
            this._extensions.Add(new AVIExtension());

            this._randomId = (int)(UnityEngine.Random.value * int.MaxValue);

            this._limitDurationTypeNames = Enum.GetNames(typeof(LimitDurationType));
            this._extensionsNames = Enum.GetNames(typeof(ExtensionsType));
        }

        void Start()
        {
#if HONEYSELECT
            IScreenshotPlugin plugin = new HoneyShot();
            if (plugin.Init())
                this._screenshotPlugins.Add(plugin);
            plugin = new PlayShot24ZHNeo();
            if (plugin.Init())
                this._screenshotPlugins.Add(plugin);
            plugin = new Screencap();
            if (plugin.Init())
                this._screenshotPlugins.Add(plugin);
#endif
            if (this._screenshotPlugins.Count == 0)
                UnityEngine.Debug.LogError("VideoExport: No compatible screenshot plugin found, please install one.");
        }

        void Update()
        {
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.E))
            {
                if (this._isRecording == false)
                    this.RecordVideo();
                else
                    this.StopRecording();
            }
            else if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.E))
                this._showUi = !this._showUi;
            if (this._startOnNextClick && Input.GetMouseButtonDown(0))
            {
                this.RecordVideo();
                this._startOnNextClick = false;
            }
            TreeNodeObject treeNode = Studio.Studio.Instance?.treeNodeCtrl.selectNode;
            this._currentAnimator = null;
            if (treeNode != null)
            {
                ObjectCtrlInfo info;
                if (Studio.Studio.Instance.dicInfo.TryGetValue(treeNode, out info))
                    this._currentAnimator = info.guideObject.transformTarget.GetComponentInChildren<Animator>();
            }

            if (this._currentAnimator == null && this._selectedLimitDuration == LimitDurationType.Animation)
                this._selectedLimitDuration = LimitDurationType.Seconds;
        }

        void LateUpdate()
        {
            if (this._currentAnimator != null)
            {
                AnimatorStateInfo stateInfo = this._currentAnimator.GetCurrentAnimatorStateInfo(0);
                this._animationIsPlaying = stateInfo.normalizedTime > this._lastAnimationNormalizedTime;
                if (stateInfo.loop == false)
                    this._animationIsPlaying = this._animationIsPlaying && stateInfo.normalizedTime < 1f;
                this._lastAnimationNormalizedTime = stateInfo.normalizedTime;
            }
        }

        void OnGUI()
        {
            if (this._showUi == false)
                return;
            Color c = GUI.backgroundColor;
            this._windowRect = GUILayout.Window(this._randomId, this._windowRect, this.Window, "Video Export " + versionNum);
            GUI.backgroundColor = new Color(0.6f, 0.6f, 0.6f, 0.5f);
            GUI.Box(this._windowRect, "", _customBoxStyle);
            GUI.backgroundColor = c;
            this._mouseInWindow = this._windowRect.Contains(Event.current.mousePosition);
            if (this._mouseInWindow)
                Studio.Studio.Instance.cameraCtrl.noCtrlCondition = () => this._mouseInWindow && this._showUi;
            this._windowRect.height = 10f;
        }

        void OnDestroy()
        {
            ModPrefs.SetInt("VideoExport", "selectedScreenshotPlugin", this._selectedPlugin);
            ModPrefs.SetInt("VideoExport", "framerate", this._fps);
            ModPrefs.SetBool("VideoExport", "autoGenerateVideo", this._autoGenerateVideo);
            ModPrefs.SetBool("VideoExport", "autoDeleteImages", this._autoDeleteImages);
            ModPrefs.SetBool("VideoExport", "limitDuration", this._limitDuration);
            ModPrefs.SetInt("VideoExport", "selectedLimitDurationType", (int)this._selectedLimitDuration);
            ModPrefs.SetFloat("VideoExport", "limitDurationNumber", this._limitDurationNumber);
            ModPrefs.SetInt("VideoExport", "selectedExtension", (int)this._selectedExtension);
            ModPrefs.SetBool("VideoExport", "resize", this._resize);
            ModPrefs.SetInt("VideoExport", "resizeX", this._resizeX);
            ModPrefs.SetInt("VideoExport", "resizeY", this._resizeY);
            ModPrefs.SetInt("VideoExport", "selectedUpdateDynamicBonesMode", (int)this._selectedUpdateDynamicBones);
            ModPrefs.SetInt("VideoExport", "prewarmLoopCount", this._prewarmLoopCount);
            ModPrefs.SetString("VideoExport", "imagesPrefix", this._imagesPrefix);
            ModPrefs.SetString("VideoExport", "imagesPostfix", this._imagesPostfix);
            foreach (IScreenshotPlugin plugin in this._screenshotPlugins)
                plugin.SaveParams();
            foreach (IExtension extension in this._extensions)
                extension.SaveParams();
        }
        #endregion

        #region Public Methods
        public void RecordVideo()
        {
            if (this._isRecording == false)
                this.StartCoroutine(this.RecordVideo_Routine());
        }

        public void StopRecording()
        {
            this._breakRecording = true;
        }
        #endregion

        #region Private Methods
        private void Window(int id)
        {
            GUILayout.BeginVertical();
            {
                GUI.enabled = this._isRecording == false;

                if (this._screenshotPlugins.Count > 1)
                {
                    GUILayout.Label("Screenshot Plugin");
                    this._selectedPlugin = GUILayout.SelectionGrid(this._selectedPlugin, this._screenshotPlugins.Select(p => p.name).ToArray(), Mathf.Clamp(this._screenshotPlugins.Count, 1, 3));
                }

                IScreenshotPlugin plugin = this._screenshotPlugins[this._selectedPlugin];
                plugin.DisplayParams();

                Vector2 currentSize = plugin.currentSize;
                GUILayout.Label($"Current Size: {currentSize.x:#}x{currentSize.y:#}");

                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label("Framerate", GUILayout.ExpandWidth(false));
                    this._fps = Mathf.RoundToInt(GUILayout.HorizontalSlider(this._fps, 1, 120));
                    string s = GUILayout.TextField(this._fps.ToString(), GUILayout.Width(50));
                    int res;
                    if (int.TryParse(s, out res) == false || res < 1)
                        res = 1;
                    this._fps = res;
                }
                GUILayout.EndHorizontal();

                bool guiEnabled = GUI.enabled;
                GUI.enabled = true;

                GUILayout.BeginHorizontal();
                {
                    this._autoGenerateVideo = GUILayout.Toggle(this._autoGenerateVideo, "Auto Generate Video");
                    this._autoDeleteImages = GUILayout.Toggle(this._autoDeleteImages, "Auto Delete Images");
                }
                GUILayout.EndHorizontal();

                GUI.enabled = guiEnabled;

                GUILayout.BeginHorizontal();
                {
                    this._limitDuration = GUILayout.Toggle(this._limitDuration, "Limit By", GUILayout.ExpandWidth(false));
                    guiEnabled = GUI.enabled;
                    GUI.enabled = this._limitDuration && guiEnabled;
                    this._selectedLimitDuration = (LimitDurationType)GUILayout.SelectionGrid((int)this._selectedLimitDuration, this._limitDurationTypeNames, 3);

                    GUI.enabled = guiEnabled;
                }
                GUILayout.EndHorizontal();

                {
                    guiEnabled = GUI.enabled;
                    GUI.enabled = this._limitDuration && guiEnabled;
                    switch (this._selectedLimitDuration)
                    {
                        case LimitDurationType.Frames:
                        {
                            GUILayout.BeginHorizontal();

                            {
                                GUILayout.Label("Limit Count", GUILayout.ExpandWidth(false));
                                string s = GUILayout.TextField(this._limitDurationNumber.ToString("0"));
                                int res;
                                if (int.TryParse(s, out res) == false || res < 1)
                                    res = 1;
                                this._limitDurationNumber = res;

                            }
                            GUILayout.EndHorizontal();

                            float totalSeconds = this._limitDurationNumber / this._fps;
                            GUILayout.Label($"Estimated {totalSeconds:0.0000} seconds, {Mathf.RoundToInt(this._limitDurationNumber)} frames");

                            break;
                        }
                        case LimitDurationType.Seconds:
                        {
                            GUILayout.BeginHorizontal();

                            {
                                GUILayout.Label("Limit Count", GUILayout.ExpandWidth(false));
                                string s = GUILayout.TextField(this._limitDurationNumber.ToString("0.000"));
                                float res;
                                if (float.TryParse(s, out res) == false || res <= 0f)
                                    res = 0.001f;
                                this._limitDurationNumber = res;
                            }
                            GUILayout.EndHorizontal();

                            float totalFrames = this._limitDurationNumber * this._fps;
                            GUILayout.Label($"Estimated {this._limitDurationNumber:0.0000} seconds, {Mathf.RoundToInt(totalFrames)} frames ({totalFrames:0.000})");

                            break;
                        }
                        case LimitDurationType.Animation:
                        {
                            GUILayout.BeginHorizontal();
                            {
                                GUILayout.Label("Prewarm Loop Count", GUILayout.ExpandWidth(false));
                                string s = GUILayout.TextField(this._prewarmLoopCount.ToString());
                                int res;
                                if (int.TryParse(s, out res) == false || res < 0)
                                    res = 1;
                                this._prewarmLoopCount = res;
                            }
                            GUILayout.EndHorizontal();

                            GUILayout.BeginHorizontal();
                            {
                                GUILayout.Label("Loops To Record", GUILayout.ExpandWidth(false));
                                string s = GUILayout.TextField(this._limitDurationNumber.ToString("0.000"));
                                float res;
                                if (float.TryParse(s, out res) == false || res <= 0)
                                    res = 0.001f;
                                this._limitDurationNumber = res;
                            }
                            GUILayout.EndHorizontal();

                            if (this._currentAnimator != null)
                            {
                                AnimatorStateInfo info = this._currentAnimator.GetCurrentAnimatorStateInfo(0);
                                    float totalLength = info.length * this._limitDurationNumber;
                                    float totalFrames = totalLength * this._fps;
                                GUILayout.Label($"Estimated {totalLength:0.0000} seconds, {Mathf.RoundToInt(totalFrames)} frames ({totalFrames:0.000})");
                            }
                            break;
                        }
                    }

                    GUI.enabled = guiEnabled;
                }

                GUILayout.BeginHorizontal();
                {
                    this._resize = GUILayout.Toggle(this._resize, "Resize", GUILayout.ExpandWidth(false));
                    guiEnabled = GUI.enabled;
                    GUI.enabled = this._resize && guiEnabled;

                    GUILayout.FlexibleSpace();

                    string s = GUILayout.TextField(this._resizeX.ToString(), GUILayout.Width(50));
                    int res;
                    if (int.TryParse(s, out res) == false || res < 1)
                        res = 1;
                    this._resizeX = res;

                    s = GUILayout.TextField(this._resizeY.ToString(), GUILayout.Width(50));
                    if (int.TryParse(s, out res) == false || res < 1)
                        res = 1;
                    this._resizeY = res;

                    if (GUILayout.Button("Default", GUILayout.ExpandWidth(false)))
                    {
                        this._resizeX = Screen.width;
                        this._resizeY = Screen.height;
                    }

                    GUI.enabled = guiEnabled;
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label("Format", GUILayout.ExpandWidth(false));
                    this._selectedExtension = (ExtensionsType)GUILayout.SelectionGrid((int)this._selectedExtension, this._extensionsNames, 4);
                }
                GUILayout.EndHorizontal();

                IExtension extension = this._extensions[(int)this._selectedExtension];
                extension.DisplayParams();

                GUILayout.Label("Dynamic Bones Update Mode");
                    this._selectedUpdateDynamicBones = (UpdateDynamicBonesType)GUILayout.SelectionGrid((int)this._selectedUpdateDynamicBones, this._updateDynamicBonesTypeNames, 2);

                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label("Prefix", GUILayout.ExpandWidth(false));
                    string s = GUILayout.TextField(this._imagesPrefix);
                    if (s != this._imagesPrefix)
                    {
                        StringBuilder builder = new StringBuilder(s);
                        foreach (char chr in Path.GetInvalidFileNameChars())
                            builder.Replace(chr.ToString(), "");
                        foreach (char chr in Path.GetInvalidPathChars())
                            builder.Replace(chr.ToString(), "");
                        this._imagesPrefix = builder.ToString();
                    }
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label("Postfix", GUILayout.ExpandWidth(false));
                    string s = GUILayout.TextField(this._imagesPostfix);
                    if (s != this._imagesPostfix)
                    {
                        StringBuilder builder = new StringBuilder(s);
                        foreach (char chr in Path.GetInvalidFileNameChars())
                            builder.Replace(chr.ToString(), "");
                        foreach (char chr in Path.GetInvalidPathChars())
                            builder.Replace(chr.ToString(), "");
                        this._imagesPostfix = builder.ToString();
                    }
                }
                GUILayout.EndHorizontal();

                bool forcePng = this._autoGenerateVideo && extension is GIFExtension;
                string actualExtension = forcePng ? "png" : plugin.extension;

                GUILayout.Label($"Example Result: {this._imagesPrefix}123{this._imagesPostfix}.{actualExtension}");

                this._startOnNextClick = GUILayout.Toggle(this._startOnNextClick, "Start Recording On Next Click");

                GUI.enabled = this._generatingVideo == false && this._startOnNextClick == false &&
                              (this._limitDuration == false || this._selectedLimitDuration != LimitDurationType.Animation || (this._currentAnimator.speed > 0.001f && this._animationIsPlaying));

                GUILayout.BeginHorizontal();
                {
                    if (this._isRecording == false)
                    {
                        if (GUILayout.Button("Start Recording"))
                            this.RecordVideo();
                    }
                    else
                    {
                        if (GUILayout.Button("Stop Recording"))
                            this.StopRecording();
                    }
                }

                GUI.enabled = true;

                GUILayout.EndHorizontal();
                Color c = GUI.color;
                GUI.color = this._messageColor;

                GUIStyle customLabel = GUI.skin.label;
                TextAnchor cachedAlignment = customLabel.alignment;
                customLabel.alignment = TextAnchor.UpperCenter;
                GUILayout.Label(this._currentMessage);
                customLabel.alignment = cachedAlignment;

                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.Box("", _customBoxStyle, GUILayout.Width((this._windowRect.width - 20) * this._progressBarPercentage), GUILayout.Height(10));
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();

                GUI.color = c;
            }
            GUILayout.EndHorizontal();
            GUI.DragWindow();

        }

        private IEnumerator RecordVideo_Routine()
        {
            this._isRecording = true;
            this._messageColor = Color.white;

            Animator currentAnimator = this._currentAnimator;

            IScreenshotPlugin screenshotPlugin = this._screenshotPlugins[this._selectedPlugin];

            string tempName = DateTime.Now.ToString("yyyy-MM-ddTHH-mm-ss");
            string framesFolder = Path.Combine(_globalFramesFolder, tempName);
            if (Directory.Exists(framesFolder) == false)
                Directory.CreateDirectory(framesFolder);

            int cachedCaptureFramerate = Time.captureFramerate;
            Time.captureFramerate = this._fps;

            if (this._selectedUpdateDynamicBones == UpdateDynamicBonesType.EveryFrame)
            {
                foreach (DynamicBone dynamicBone in Resources.FindObjectsOfTypeAll<DynamicBone>())
                    dynamicBone.m_UpdateRate = -1;
                foreach (DynamicBone_Ver02 dynamicBone in Resources.FindObjectsOfTypeAll<DynamicBone_Ver02>())
                    dynamicBone.UpdateRate = -1;
            }

            this._currentRecordingFrame = 0;
            this._currentRecordingTime = 0;

            if (this._limitDuration && this._selectedLimitDuration == LimitDurationType.Animation)
            {
                if (this._prewarmLoopCount > 0)
                {
                    int j = 0;
                    float lastNormalizedTime = 0;
                    while (true)
                    {
                        AnimatorStateInfo info = currentAnimator.GetCurrentAnimatorStateInfo(0);
                        float currentNormalizedTime = info.normalizedTime % 1f;
                        if (lastNormalizedTime > 0.5f && currentNormalizedTime < lastNormalizedTime)
                            j++;
                        lastNormalizedTime = currentNormalizedTime;
                        if (j == this._prewarmLoopCount)
                        {
                            if (!info.loop)
                                yield return new WaitForEndOfFrame();
                            break;
                        }
                        yield return new WaitForEndOfFrame();
                        this._currentMessage = $"Prewarming animation {j + 1}/{this._prewarmLoopCount}";
                        this._progressBarPercentage = lastNormalizedTime;
                    }
                }
            }
            else
            {
                yield return new WaitForEndOfFrame();
            }

            DateTime startTime = DateTime.Now;
            this._progressBarPercentage = 0f;

            int limit = 1;
            if (this._limitDuration)
            {
                switch (this._selectedLimitDuration)
                {
                    case LimitDurationType.Frames:
                        limit = Mathf.RoundToInt(this._limitDurationNumber);
                        break;
                    case LimitDurationType.Seconds:
                        limit = Mathf.RoundToInt(this._limitDurationNumber * this._fps);
                        break;
                    case LimitDurationType.Animation:
                        limit = Mathf.RoundToInt(currentAnimator.GetCurrentAnimatorStateInfo(0).length * this._limitDurationNumber * this._fps);
                        break;
                }
                this._recordingFrameLimit = limit;
            }
            else
            {
                this._recordingFrameLimit = -1;
            }
            TimeSpan elapsed = TimeSpan.Zero;
            int i = 0;
            bool forcePng = this._autoGenerateVideo && this._extensions[(int)this._selectedExtension] is GIFExtension;
            string actualExtension = forcePng ? "png" : screenshotPlugin.extension;
            for (; ; i++)
            {
                if (this._limitDuration && i >= limit)
                    this.StopRecording();
                if (this._breakRecording)
                {
                    this._breakRecording = false;
                    break;
                }

                if (i % 5 == 0)
                {
                    Resources.UnloadUnusedAssets();
                    GC.Collect();
                }
                byte[] frame = screenshotPlugin.Capture(forcePng);
                File.WriteAllBytes($"{framesFolder}/{this._imagesPrefix}{i}{this._imagesPostfix}.{actualExtension}", frame);

                elapsed = DateTime.Now - startTime;

                TimeSpan remaining = TimeSpan.FromSeconds((limit - i - 1) * elapsed.TotalSeconds / (i + 1));

                if (this._limitDuration)
                    this._progressBarPercentage = (i + 1f) / limit;
                else
                    this._progressBarPercentage = (i % this._fps) / (float)this._fps;

                this._currentMessage = $"Taking screenshot {i + 1}{(this._limitDuration ? $"/{limit} {this._progressBarPercentage * 100:0.0}%\nETA: {remaining.Hours:0}:{remaining.Minutes:00}:{remaining.Seconds:00} Elapsed: {elapsed.Hours:0}:{elapsed.Minutes:00}:{elapsed.Seconds:00}" : "")}";

                this._currentRecordingFrame = i + 1;
                this._currentRecordingTime = (i + 1) / (double)this._fps;
                yield return new WaitForEndOfFrame();
            }
            Time.captureFramerate = cachedCaptureFramerate;

            UnityEngine.Debug.Log($"Time spent taking screenshots: {elapsed.Hours:0}:{elapsed.Minutes:00}:{elapsed.Seconds:00}");

            foreach (DynamicBone dynamicBone in Resources.FindObjectsOfTypeAll<DynamicBone>())
                dynamicBone.m_UpdateRate = 60;
            foreach (DynamicBone_Ver02 dynamicBone in Resources.FindObjectsOfTypeAll<DynamicBone_Ver02>())
                dynamicBone.UpdateRate = 60;

            bool error = false;
            if (this._autoGenerateVideo)
            {
                this._generatingVideo = true;
                this._messageColor = Color.yellow;
                if (Directory.Exists(_outputFolder) == false)
                    Directory.CreateDirectory(_outputFolder);
                this._currentMessage = "Generating video...";
                yield return null;
                IExtension extension = this._extensions[(int)this._selectedExtension];
                string executable = extension.GetExecutable();
                string arguments = extension.GetArguments(framesFolder, this._imagesPrefix, this._imagesPostfix, actualExtension, this._fps, screenshotPlugin.transparency, this._resize, this._resizeX, this._resizeY, Path.Combine(_outputFolder, tempName));
                startTime = DateTime.Now;
                Process proc = this.StartExternalProcess(executable, arguments, extension.canProcessStandardOutput, extension.canProcessStandardError);
                while (proc.HasExited == false)
                {
                    if (extension.canProcessStandardOutput)
                    {
                        int outputPeek = proc.StandardOutput.Peek();
                        for (int j = 0; j < outputPeek; j++)
                            extension.ProcessStandardOutput((char)proc.StandardOutput.Read());
                        yield return null;
                    }

                    elapsed = DateTime.Now - startTime;

                    if (extension.progress != 0)
                    {
                        TimeSpan eta = TimeSpan.FromSeconds((i - extension.progress) * elapsed.TotalSeconds / extension.progress);
                        this._progressBarPercentage = extension.progress / (float)i;
                        this._currentMessage = $"Generating video {extension.progress}/{i} {this._progressBarPercentage * 100:0.0}%\nETA: {eta.Hours:0}:{eta.Minutes:00}:{eta.Seconds:00} Elapsed: {elapsed.Hours:0}:{elapsed.Minutes:00}:{elapsed.Seconds:00}";
                    }
                    else
                        this._progressBarPercentage = (float)((elapsed.TotalSeconds % 6) / 6);


                    Resources.UnloadUnusedAssets();
                    GC.Collect();
                    yield return null;
                    proc.Refresh();
                }

                proc.WaitForExit();
                UnityEngine.Debug.LogError(proc.StandardError.ReadToEnd());

                yield return null;
                if (proc.ExitCode == 0)
                {
                    this._messageColor = Color.green;
                    this._currentMessage = "Done!";
                }
                else
                {
                    this._messageColor = Color.red;
                    this._currentMessage = "Error while generating the video, please check your output_log.txt file.";
                    error = true;
                }
                proc.Close();
                this._generatingVideo = false;
                UnityEngine.Debug.Log($"Time spent generating video: {elapsed.Hours:0}:{elapsed.Minutes:00}:{elapsed.Seconds:00}");
            }
            else
            {
                this._messageColor = Color.green;
                this._currentMessage = "Done!";
            }
            this._progressBarPercentage = 1;

            if (this._autoDeleteImages && error == false)
                Directory.Delete(framesFolder, true);
            this._isRecording = false;
            Resources.UnloadUnusedAssets();
            GC.Collect();
        }

        private Process StartExternalProcess(string exe, string arguments, bool redirectStandardOutput, bool redirectStandardError)
        {
            UnityEngine.Debug.Log($"{exe} {arguments}");
            Process proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = exe,
                    Arguments = arguments,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = Directory.GetCurrentDirectory(),
                    RedirectStandardOutput = redirectStandardOutput,
                    RedirectStandardError = redirectStandardError,
                }
            };
            proc.Start();

            return proc;
        }
        #endregion

    }
}
