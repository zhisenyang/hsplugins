// Decompiled with JetBrains decompiler
// Type: ShortcutsHS.ShortcutsHS
// Assembly: ShortcutsHS, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 62AE9985-56CF-46BD-982F-B15D5A0C4B01
// Assembly location: C:\Program Files (x86)\HoneySelect\illusion\HoneySelect\Plugins\ShortcutsHS.dll

using System.Reflection;
using Harmony;
using IllusionPlugin;
using UILib;
using UnityEngine;

namespace HSPE
{
    public class HSPE : IEnhancedPlugin
    {
        public static string VersionNum { get { return "2.4.0"; } }

        public string Name { get { return "HSPE"; } }

        public string Version { get { return HSPE.VersionNum; } }

        public string[] Filter { get { return new[] { "StudioNEO_32", "StudioNEO_64" }; } }

        public void OnApplicationQuit()
        {

        }

        public void OnApplicationStart()
        {
            HarmonyInstance harmony = HarmonyInstance.Create("com.joan6694.hsplugins.hspe");
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            UIUtility.Init();
        }

        public void OnFixedUpdate()
        {
        }

        public void OnLateUpdate()
        {
        }

        public void OnLevelWasInitialized(int level)
        {
            if (level == 3)
            {
                GameObject go = new GameObject("HSPEPlugin");
                go.AddComponent<MainWindow>();
            }
        }

        public void OnLevelWasLoaded(int level)
        {
        }

        public void OnUpdate()
        {
        }
    }
}