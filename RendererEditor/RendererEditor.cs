using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Harmony;
using IllusionPlugin;
using UnityEngine;

namespace RendererEditor
{
    public class RendererEditor : IEnhancedPlugin
    {
        public static bool previewTextures = true;

        public string Name { get { return "RendererEditor"; } }
        public string Version { get { return "1.3.0"; } }
        public string[] Filter { get { return new[] {"StudioNEO_32", "StudioNEO_64"}; } }

        public void OnApplicationStart()
        {
            previewTextures = ModPrefs.GetBool("RendererEditor", "previewTextures", true, true);
            HarmonyInstance harmony = HarmonyInstance.Create("com.joan6694.hsplugins.renderereditor");
            harmony.PatchAll();
        }

        public void OnApplicationQuit()
        {
        }

        public void OnLevelWasLoaded(int level)
        {
            if (level == 3)
            {
                new GameObject("RendererEditor", typeof(MainWindow));
            }
        }

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
    }
}
