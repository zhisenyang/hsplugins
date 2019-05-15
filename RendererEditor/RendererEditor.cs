using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Harmony;
#if HONEYSELECT
using IllusionPlugin;
#elif KOIKATSU
using BepInEx;
#endif
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RendererEditor
{
#if KOIKATSU
    [BepInPlugin(GUID: "com.joan6694.kkplugins.renderereditor", Name: "RendererEditor", Version: RendererEditor.versionNum)]
    [BepInDependency("com.bepis.bepinex.extendedsave")]
    [BepInProcess("CharaStudio")]
#endif
    public class RendererEditor :
#if HONEYSELECT
        IEnhancedPlugin
#elif KOIKATSU
        BaseUnityPlugin
#endif
    {
#if HONEYSELECT
        public const string versionNum = "1.4.0b2";
#elif KOIKATSU
        public const string versionNum = "1.0.0";
        public const int saveVersion = 1;
#endif

#if HONEYSELECT
        public static bool previewTextures = true;
#elif KOIKATSU
        private static ConfigWrapper<bool> _previewTextures;
        public static bool previewTextures {get { return _previewTextures.Value; }}
#endif

#if HONEYSELECT
        public string Name { get { return "RendererEditor"; } }
        public string Version { get { return versionNum; } }
        public string[] Filter { get { return new[] {"StudioNEO_32", "StudioNEO_64"}; } }
#endif

#if HONEYSELECT
        public void OnApplicationStart()
#elif KOIKATSU
        void Awake()
#endif
        {
#if HONEYSELECT
            previewTextures = ModPrefs.GetBool("RendererEditor", "previewTextures", true, true);
#elif KOIKATSU
            SceneManager.sceneLoaded += this.SceneManagerOnSceneLoaded;
            _previewTextures = new ConfigWrapper<bool>("previewTextures", "RendererEditor", true);
#endif
            HarmonyInstance harmony = HarmonyInstance.Create("com.joan6694.hsplugins.renderereditor");
            harmony.PatchAll();
        }


        public void OnApplicationQuit()
        {
        }

#if HONEYSELECT
        public void OnLevelWasLoaded(int level)
#elif KOIKATSU
        private void SceneManagerOnSceneLoaded(Scene scene, LoadSceneMode loadMode)
#endif
        {
#if HONEYSELECT
            if (level == 3)
#elif KOIKATSU
            if (scene.buildIndex == 1 && loadMode == LoadSceneMode.Single)
#endif
            {
                new GameObject("RendererEditor", typeof(MainWindow));
            }
        }

#if HONEYSELECT
        public void OnLevelWasInitialized(int level)
        {
        }

        public void OnUpdate()
        {
        }

        public void OnFixedUpdate()
        {
        }

        public void OnLateUpdate()
        {
        }
#endif
    }
}
