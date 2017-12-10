// Decompiled with JetBrains decompiler
// Type: ShortcutsHS.ShortcutsHS
// Assembly: ShortcutsHS, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 62AE9985-56CF-46BD-982F-B15D5A0C4B01
// Assembly location: C:\Program Files (x86)\HoneySelect\illusion\HoneySelect\Plugins\ShortcutsHS.dll

using System;
using System.Collections.Generic;
using System.Reflection;
using IllusionPlugin;
using UILib;
using UnityEngine;

namespace HSPE
{
    public class HSPE : IEnhancedPlugin
    {
        public static string VersionNum { get { return "2.3.0"; } }

        public string Name { get { return "HSPE"; } }

        public string Version { get { return HSPE.VersionNum; } }

        public string[] Filter { get { return new[] { "StudioNEO_32", "StudioNEO_64" }; } }

        public void OnApplicationQuit()
        {

        }

        public void OnApplicationStart()
        {
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
                if (!GameObject.Find("HSPE"))
                {
                    GameObject go = new GameObject("HSPE");
                    go.AddComponent<MainWindow>();
                }
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