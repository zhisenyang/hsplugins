using System;
using System.Collections.Generic;
using System.Reflection;
using Harmony;
using UILib;
using UnityEngine;
#if HONEYSELECT
using IllusionPlugin;
#elif KOIKATSU
using UnityEngine.SceneManagement;
using BepInEx;
#endif

namespace HSPE
{
#if HONEYSELECT
    public class HSPE : IEnhancedPlugin
#elif KOIKATSU
    [BepInPlugin(GUID: "com.joan6694.kkplugins.kkpe", Name: "KKPE", Version: KKPE.versionNum)]
    [BepInDependency("com.bepis.bepinex.extendedsave")]
    [BepInProcess("CharaStudio")]
    public class KKPE : BaseUnityPlugin
#endif
    {
#if HONEYSELECT
        public const string versionNum = "2.8.0";
        public string Name { get { return "HSPE"; } }
        public string Version { get { return versionNum; } }
        public string[] Filter { get { return new[] {"StudioNEO_32", "StudioNEO_64"}; } }
#elif KOIKATSU
        public const string versionNum = "1.1.0";
        public const int saveVersion = 0;
#endif

#if HONEYSELECT
        public void OnApplicationQuit(){}
        public void OnApplicationStart()
        {
            this.Init();
        }
        public void OnFixedUpdate(){}
        public void OnLateUpdate(){}
        public void OnLevelWasInitialized(int level)
        {
            if (level == 3)
                this.SceneLoaded(level);
        }
        public void OnLevelWasLoaded(int level){}
        public void OnUpdate(){}
#elif KOIKATSU
        public void Awake()
        {
            SceneManager.sceneLoaded += this.SceneManagerOnSceneLoaded;
            this.Init();
        }

        private void SceneManagerOnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
        {
            if (scene.buildIndex == 1)
                this.SceneLoaded(scene.buildIndex);            
        }
#endif
        private void Init()
        {
            HarmonyInstance harmony = HarmonyInstance.Create("com.joan6694.illusionplugins.poseeditor");
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
                    UnityEngine.Debug.Log("Pose Editor: Exception occured when patching: " + e.ToString());
                }
            }
            UIUtility.Init();
        }

        private void SceneLoaded(int level)
        {
            GameObject go = new GameObject("HSPEPlugin");
            go.AddComponent<MainWindow>();
        }
    }
}