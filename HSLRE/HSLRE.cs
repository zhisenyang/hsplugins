using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Harmony;
using HSLRE.CustomEffects;
using IllusionInjector;
using IllusionPlugin;
using ToolBox;
using UnityEngine;
using UnityStandardAssets.CinematicEffects;
using UnityStandardAssets.ImageEffects;
using DepthOfField = UnityStandardAssets.ImageEffects.DepthOfField;
using Object = UnityEngine.Object;

namespace HSLRE
{
    public class HSLRE : IEnhancedPlugin
    {
        public string Name { get { return "HSLRE"; } }
        public string Version { get { return Assembly.GetExecutingAssembly().GetName().Version.ToString(); } }
        public string[] Filter { get { return new[] { "HoneySelect_64", "HoneySelect_32", "StudioNEO_32", "StudioNEO_64", "Honey Select Unlimited_64", "Honey Select Unlimited_32" }; } }

        #region Types
        public enum Binary
        {
            Neo,
            Game,
        }

        [Flags]
        public enum EffectType
        {
            FourKDiffuse = 1 << 0,
            LRE = 1 << 1
        }

        public class EffectData
        {
            internal bool _enabled;

            public bool enabled
            {
                get { return this._enabled; }
                set
                {
                    if (this._enabled == value)
                        return;
                    this._enabled = value;
                    if (this._enabled)
                    {
                        if (this.onEnable != null)
                            this.onEnable();
                    }
                    else
                    {
                        if (this.onDisable != null)
                            this.onDisable();
                    }
                }
            }

            public object effect;
            public Action onEnable;
            public Action onDisable;
            public Action<RenderTexture, RenderTexture> onRenderImage;
            public EffectType effectType;
            public int originalPosition;
            public string simpleName;

            public EffectData(object effect, bool enabled, EffectType effectType, bool treatAsMonobehaviour, int originalPosition, string simpleName)
            {
                this.effect = effect;
                this._enabled = enabled;
                this.effectType = effectType;
                this.originalPosition = originalPosition;
                this.simpleName = simpleName;

                MethodInfo methodInfo;
                MonoBehaviour mono = effect as MonoBehaviour;
                if (treatAsMonobehaviour && mono != null)
                {
                    this.onEnable = () => mono.enabled = true;
                    this.onDisable = () => mono.enabled = false;
                }
                else
                {
                    methodInfo = this.effect.GetType().GetMethod("OnEnable", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
                    if (methodInfo != null)
                        this.onEnable = (Action)Delegate.CreateDelegate(typeof(Action), this.effect, methodInfo);

                    methodInfo = this.effect.GetType().GetMethod("OnDisable", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
                    if (methodInfo != null)
                        this.onDisable = (Action)Delegate.CreateDelegate(typeof(Action), this.effect, methodInfo);
                }

                methodInfo = this.effect.GetType().GetMethod("OnRenderImage", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
                if (methodInfo != null)
                    this.onRenderImage = (Action<RenderTexture, RenderTexture>)Delegate.CreateDelegate(typeof(Action<RenderTexture, RenderTexture>), this.effect, methodInfo);

                if (this.onEnable != null)
                    this.onEnable();
                if (!this._enabled && this.onDisable != null)
                    this.onDisable();
            }
        }
        #endregion

        public static HSLRE self;

        #region Private Variables
        private PluginComponent _pluginComponent;
        private readonly List<MonoBehaviour> _effectsToIgnore = new List<MonoBehaviour>();
        private int _currentEffectIndex;
        private RenderTexture _tempSource;
        private RenderTexture _tempDestination;
        private RenderTexture _originalDestination;
        internal Material _flipImageMat;
        internal AssetBundle _resources;
        #endregion

        #region Public Variables
        public readonly Dictionary<object, EffectData> effectsDictionary = new Dictionary<object, EffectData>();
        public bool init = false;
        public Binary binary;

        public readonly List<EffectData> opaqueEffects = new List<EffectData>();
        public SEGI segi;
        public SSAOPro ssao;
        public ScreenSpaceReflection ssr;
        //private GlobalFog _fog;

        public readonly List<EffectData> generalEffects = new List<EffectData>();
        public DepthOfField dof;
        public VignetteAndChromaticAberration vignette;
        public SunShafts sunShafts;
        public SMAA smaa;
        public CustomEffects.Bloom cinematicBloom;
        public CustomEffects.TonemappingColorGrading tonemapping;
        public LensAberrations lensAberrations;
        public ColorCorrectionCurves ccc;
        public BloomAndFlares bloomAndFlares;
        public Antialiasing antialiasing;
        public NoiseAndGrain noiseAndGrain;
        public CameraMotionBlur motionBlur;
        public AmplifyBloom amplifyBloom;
        public BlurOptimized blur;

        public bool fixDofForUpscaledScreenshots = true;
        public bool fixNoiseAndGrainForUpscaledScreenshots = true;
        public bool fixBlurForUpscaledScreenshots = true;
        public bool fixAmplifyBloomForUpscaledScreenshots = true;
        public bool fixSsrForUpscaledScreenshots = true;
        #endregion

        #region Unity Methods
        public void OnLevelWasInitialized(int level)
        {

        }

        public void OnFixedUpdate()
        {

        }

        public void OnApplicationStart()
        {
            self = this;

            switch (Process.GetCurrentProcess().ProcessName)
            {
                case "HoneySelect_32":
                case "HoneySelect_64":
                case "Honey Select Unlimited_32":
                case "Honey Select Unlimited_64":
                    this.binary = Binary.Game;
                    break;
                case "StudioNEO_32":
                case "StudioNEO_64":
                    this.binary = Binary.Neo;
                    break;
            }

            this._resources = AssetBundle.LoadFromMemory(Properties.Resources.LREResources);
            this._flipImageMat = new Material(this._resources.LoadAsset<Shader>("FlipImage"));


            HarmonyInstance harmony = HarmonyInstance.Create("com.joan6694.illusionplugins.hslre");
            foreach (Type type in Assembly.GetExecutingAssembly().GetTypes())
            {
                try
                {
                    List<HarmonyMethod> harmonyMethods = type.GetHarmonyMethods();
                    if (harmonyMethods != null && harmonyMethods.Count > 0)
                    {
                        HarmonyMethod attributes = HarmonyMethod.Merge(harmonyMethods);
                        new PatchProcessor(harmony, type, attributes).Patch();
                    }
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.Log("LRE: Exception occured when patching: " + e.ToString());
                }
            }
            this._pluginComponent = Object.FindObjectOfType<PluginComponent>();
        }

        public void OnApplicationQuit()
        {
            Settings.Save();
        }

        public void OnLevelWasLoaded(int level)
        {
            this._effectsToIgnore.Clear();
            this.opaqueEffects.Clear();
            this.generalEffects.Clear();
            this.init = false;
            Camera mainCamera = this.GetMainCamera();

            if (mainCamera == null)
                return;
            this._pluginComponent.ExecuteDelayed(() =>
            {
                CameraEventsDispatcher dispatcher = mainCamera.gameObject.AddComponent<CameraEventsDispatcher>();
                dispatcher.onPreCull += this.OnPreCull;
                dispatcher.onPreRender += this.OnPreRender;
                dispatcher.onPostRender += this.OnPostRender;

                this.ssao = mainCamera.GetComponent<SSAOPro>();
                if (this.ssao != null)
                {
                    try
                    {
                        this.segi = new SEGI(mainCamera);
                        this.segi.ApplyPreset(10);
                        this.AddPostProcessingToList(true, false, this.segi, false, EffectType.FourKDiffuse | EffectType.LRE, "segi");
                    }
                    catch
                    {
                        this.segi = null;
                    }

                    this.AddPostProcessingToList(true, false, this.ssao, true, EffectType.FourKDiffuse | EffectType.LRE, "ssao");

                    this.ssr = mainCamera.GetComponent<ScreenSpaceReflection>();
                    this.AddPostProcessingToList(true, true, this.ssr, this.ssr != null && this.ssr.enabled, EffectType.FourKDiffuse | EffectType.LRE, "ssr");
                    //this.IgnoreEffect(this.ssr);
                }

                this.vignette = mainCamera.GetComponent<VignetteAndChromaticAberration>();
                if (this.vignette != null)
                {
                    this.dof = mainCamera.GetComponent<DepthOfField>();
                    this.AddPostProcessingToList(false, false, this.dof, this.dof != null && this.dof.enabled, EffectType.FourKDiffuse | EffectType.LRE, "depthOfField");
                    this.IgnoreEffect(this.dof);

                    this.AddPostProcessingToList(false, false, this.vignette, this.vignette.enabled, EffectType.FourKDiffuse, "vignetteAndChromaticAberration");

                    this.sunShafts = mainCamera.GetComponent<SunShafts>();
                    this.AddPostProcessingToList(false, false, this.sunShafts, this.sunShafts != null && this.sunShafts.enabled, EffectType.FourKDiffuse | EffectType.LRE, "sunShafts");
                    this.IgnoreEffect(this.sunShafts);

                    this.smaa = new SMAA();
                    this.AddPostProcessingToList(false, false, this.smaa, true, EffectType.LRE, "smaa");

                    this.cinematicBloom = new CustomEffects.Bloom();
                    this.AddPostProcessingToList(false, false, this.cinematicBloom, true, EffectType.LRE, "bloom");

                    this.amplifyBloom = new AmplifyBloom(mainCamera);
                    this.AddPostProcessingToList(false, false, this.amplifyBloom, false, EffectType.FourKDiffuse | EffectType.LRE, "amplifyBloom");

                    this.tonemapping = new CustomEffects.TonemappingColorGrading();
                    this.AddPostProcessingToList(false, false, this.tonemapping, true, EffectType.LRE, "tonemapping");

                    this.ccc = mainCamera.GetComponent<ColorCorrectionCurves>();
                    this.AddPostProcessingToList(false, false, this.ccc, this.ccc != null && this.ccc.enabled, EffectType.FourKDiffuse, "colorCorrectionCurves");
                    this.IgnoreEffect(this.ccc);

                    this.bloomAndFlares = mainCamera.GetComponent<BloomAndFlares>();
                    this.AddPostProcessingToList(false, false, this.bloomAndFlares, this.bloomAndFlares != null && this.bloomAndFlares.enabled, EffectType.FourKDiffuse, "bloomAndFlares");
                    this.IgnoreEffect(this.bloomAndFlares);

                    this.motionBlur = mainCamera.gameObject.AddComponent<CameraMotionBlur>();
                    this.AddPostProcessingToList(false, false, this.motionBlur, false, EffectType.FourKDiffuse | EffectType.LRE, "motionBlur");
                    this.IgnoreEffect(this.motionBlur);

                    this.noiseAndGrain = mainCamera.gameObject.AddComponent<NoiseAndGrain>();
                    this.AddPostProcessingToList(false, false, this.noiseAndGrain, false, EffectType.FourKDiffuse | EffectType.LRE, "noiseAndGrain");
                    this.IgnoreEffect(this.noiseAndGrain);

                    this.lensAberrations = new LensAberrations();
                    this.AddPostProcessingToList(false, false, this.lensAberrations, true, EffectType.LRE, "lens");

                    this.blur = mainCamera.gameObject.AddComponent<BlurOptimized>();
                    this.AddPostProcessingToList(false, false, this.blur, false, EffectType.FourKDiffuse | EffectType.LRE, "blur");
                    this.IgnoreEffect(this.blur);
                    
                    this.antialiasing = mainCamera.GetComponent<Antialiasing>();
                    this.AddPostProcessingToList(false, false, this.antialiasing, this.antialiasing != null && this.antialiasing.enabled, EffectType.FourKDiffuse, "antialiasing");
                    this.IgnoreEffect(this.antialiasing);
                }

                Settings.Load();

                if (this.binary == Binary.Game && (level == 21 || level == 22))
                {
                    if (Settings.basicSettings.CharaMakerReform)
                    {
                        Camera.main.clearFlags = (CameraClearFlags)Settings.basicSettings.CharaMakerBackgroundType;
                        Camera.main.renderingPath = RenderingPath.DeferredShading;
                        Camera.main.hdr = true;
                        Camera.main.backgroundColor = Settings.basicSettings.CharaMakerBackground;
                        UnityEngine.Debug.Log("Chara maker reformed");
                    }
                }
                else
                {
                    mainCamera.hdr = true;
                    mainCamera.renderingPath = RenderingPath.DeferredShading;
                    if (Camera.main != null) //Because sometimes they're not the same, idk m8
                    {
                        Camera.main.hdr = true;
                        Camera.main.renderingPath = RenderingPath.DeferredShading;
                    }
                }

                this.init = true;
            }, 1);
        }

        public void OnUpdate()
        {
            if (this.segi != null && this.effectsDictionary[this.segi].enabled)
                this.segi.Update();
        }

        public void OnLateUpdate()
        {
            if (this.ssao != null)
                this.ssao.enabled = true;
            if (this.vignette != null)
                this.vignette.enabled = true;
            foreach (MonoBehaviour effect in this._effectsToIgnore)
                effect.enabled = false;
        }
        #endregion

        #region Public Methods
        public void MoveUp(object effect)
        {
            int index = this.generalEffects.FindIndex(e => e.effect == effect);
            if (index <= 0)
                return;
            EffectData temp = this.generalEffects[index - 1];
            this.generalEffects[index - 1] = this.generalEffects[index];
            this.generalEffects[index] = temp;
        }

        public void MoveDown(object effect)
        {
            int index = this.generalEffects.FindIndex(e => e.effect == effect);
            if (index == -1 || index == this.generalEffects.Count - 1)
                return;
            EffectData temp = this.generalEffects[index + 1];
            this.generalEffects[index + 1] = this.generalEffects[index];
            this.generalEffects[index] = temp;
        }

        public void ResetOrder()
        {
            this.generalEffects.Sort((x, y) => x.originalPosition.CompareTo(y.originalPosition));
        }

        public void MoveAtIndex(object effect, int index)
        {
            EffectData data = this.effectsDictionary[effect];
            this.generalEffects.Remove(data);
            this.generalEffects.Insert(index, data);
        }
        #endregion

        #region Private Methods
        private Camera GetMainCamera()
        {
            switch (this.binary)
            {
                case Binary.Neo:
                    GameObject go = GameObject.Find("StudioScene/Camera/Main Camera");
                    if (go != null)
                        return go.GetComponent<Camera>();
                    return null;
                case Binary.Game:
                    return Camera.main;
            }
            return null;
        }

        private void AddPostProcessingToList(bool opaqueEffect, bool treatAsMonobehaviour, object effect, bool enabledByDefault, EffectType effectType, string simpleName)
        {
            if (effect == null)
                return;
            EffectData data = new EffectData(effect, enabledByDefault, effectType, treatAsMonobehaviour, opaqueEffect ? this.opaqueEffects.Count : this.generalEffects.Count, simpleName);
            if (data.onRenderImage != null)
            {
                if (!opaqueEffect)
                    this.generalEffects.Add(data);
                else
                    this.opaqueEffects.Add(data);
                this.effectsDictionary.Add(effect, data);
            }
        }

        private void IgnoreEffect(MonoBehaviour effect)
        {
            if (effect != null)
                this._effectsToIgnore.Add(effect);
        }

        private void OnPreCull(Camera camera)
        {
            if (this.smaa != null && this.effectsDictionary[this.smaa].enabled)
                this.smaa.OnPreCull();
        }
        private void OnPreRender(Camera camera)
        {
            if (this.segi != null && this.effectsDictionary[this.segi].enabled)
                this.segi.OnPreRender();
        }

        private void OnPostRender(Camera camera)
        {
            if (this.smaa != null && this.effectsDictionary[this.smaa].enabled)
                this.smaa.OnPostRender();
        }

        private bool RenderEffectListPre(object hostInstance, ref RenderTexture source, ref RenderTexture destination, List<EffectData> effectList)
        {
            this._originalDestination = destination;
            this._tempSource = RenderTexture.GetTemporary(source.width, source.height, source.depth, source.format, RenderTextureReadWrite.Default, source.antiAliasing);
            this._tempDestination = RenderTexture.GetTemporary(source.width, source.height, source.depth, source.format, RenderTextureReadWrite.Default, source.antiAliasing);

            Graphics.Blit(source, this._tempSource);

            this._currentEffectIndex = 0;
            bool hostEffectEnabled = true;
            for (; this._currentEffectIndex < effectList.Count; ++this._currentEffectIndex)
            {
                EffectData effect = effectList[this._currentEffectIndex];
                if (ReferenceEquals(effect.effect, hostInstance))
                {
                    hostEffectEnabled = effect._enabled;
                    ++this._currentEffectIndex;
                    break;
                }
                if (effect._enabled == false)
                    continue;
                effect.onRenderImage(this._tempSource, this._tempDestination);

                RenderTexture temp = this._tempSource;
                this._tempSource = this._tempDestination;
                this._tempDestination = temp;
            }

            source = this._tempSource;
            destination = this._tempDestination;

            if (hostEffectEnabled)
            {
                RenderTexture temp = this._tempSource;
                this._tempSource = this._tempDestination;
                this._tempDestination = temp;
            }

            return hostEffectEnabled;
        }

        private void RenderEffectListPost(object hostInstance, RenderTexture source, RenderTexture destination, List<EffectData> effectList)
        {
            for (; this._currentEffectIndex < effectList.Count; ++this._currentEffectIndex)
            {
                EffectData effect = effectList[this._currentEffectIndex];
                if (effect._enabled == false)
                    continue;
                effect.onRenderImage(this._tempSource, this._tempDestination);

                RenderTexture temp = this._tempSource;
                this._tempSource = this._tempDestination;
                this._tempDestination = temp;
            }
            Graphics.Blit(this._tempSource, this._originalDestination);
            RenderTexture.ReleaseTemporary(this._tempSource);
            RenderTexture.ReleaseTemporary(this._tempDestination);

        }
        #endregion

        #region Patches
        [HarmonyPatch(typeof(SSAOPro), "OnRenderImage", typeof(RenderTexture), typeof(RenderTexture))]
        private static class SSAOPro_OnRenderImage_Patches
        {
            private static bool Prefix(SSAOPro __instance, RenderTexture source, RenderTexture destination)
            {
                if (self.ssao != __instance)
                    return true;

                EffectData data;
                if (self.segi != null && self.effectsDictionary.TryGetValue(self.segi, out data) && data.enabled)
                {
                    RenderTexture temporary = RenderTexture.GetTemporary(destination.width, destination.height, destination.depth, destination.format, RenderTextureReadWrite.Default, destination.antiAliasing);
                    data.onRenderImage(source, temporary);
                    Graphics.Blit(temporary, source);
                    RenderTexture.ReleaseTemporary(temporary);
                }
                if (self.effectsDictionary[__instance].enabled == false)
                {
                    Graphics.Blit(source, destination);
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(VignetteAndChromaticAberration), "OnRenderImage", typeof(RenderTexture), typeof(RenderTexture))]
        private static class VignetteAndChromaticAberration_OnRenderImage_Patches
        {
            [HarmonyAfter("com.joan6694.illusionplugins.fogeditor")]
            private static bool Prefix(VignetteAndChromaticAberration __instance, ref RenderTexture source, ref RenderTexture destination)
            {
                if (self.vignette != __instance)
                    return true;
                return self.RenderEffectListPre(__instance, ref source, ref destination, self.generalEffects);
            }

            [HarmonyBefore("com.joan6694.illusionplugins.fogeditor")]
            private static void Postfix(VignetteAndChromaticAberration __instance, RenderTexture source, RenderTexture destination)
            {
                if (self.vignette != __instance)
                    return;
                self.RenderEffectListPost(__instance, source, destination, self.generalEffects);
            }
        }
        #endregion
    }


}
