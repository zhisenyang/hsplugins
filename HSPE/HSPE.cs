// Decompiled with JetBrains decompiler
// Type: ShortcutsHS.ShortcutsHS
// Assembly: ShortcutsHS, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 62AE9985-56CF-46BD-982F-B15D5A0C4B01
// Assembly location: C:\Program Files (x86)\HoneySelect\illusion\HoneySelect\Plugins\ShortcutsHS.dll

using IllusionPlugin;
using System;
using UnityEngine;

namespace HSPE
{
    public class HSPE : IPlugin
    {
        public static int level;

        public string Name { get { return "HSPE"; } }

        public string Version { get { return "1.0.0"; } }

        public void OnApplicationQuit()
        {
        }

        public void OnApplicationStart()
        {
        }

        public void OnFixedUpdate()
        {
        }

        public void OnLateUpdate()
        {
        }

        public void OnLevelWasInitialized(int level)
        {
            if (level != 1 && level != 2 && (level != 20 && level != 11) && level != 14)
                return;
            if (!GameObject.Find("HSPE"))
            {
                GameObject go = new GameObject("HSPE");
                go.AddComponent<UIUtility>();
                go.AddComponent<MainWindow>();
            }
            HSPE.level = level;
            Console.WriteLine("HSPE");
        }

        public void OnLevelWasLoaded(int level)
        {
        }

        public void OnUpdate()
        {
        }
    }
}
