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
        public struct VersionNumber
        {
            public readonly int x;
            public readonly int y;
            public readonly int z;
            public readonly bool valid;

            public VersionNumber(int x, int y, int z)
            {
                this.x = x;
                this.y = y;
                this.z = z;
                this.valid = true;
            }

            public VersionNumber(string str)
            {
                string[] s = str.Split('.');
                this.valid = false;
                this.x = 0;
                this.y = 0;
                this.z = 0;
                if (!int.TryParse(s[0], out this.x))
                    return;
                if (!int.TryParse(s[1], out this.y))
                    return;
                if (!int.TryParse(s[2], out this.z))
                    return;
                this.valid = true;
            }

            public override string ToString()
            {
                return this.x + "." + this.y + "." + this.z;
            }

            public static bool operator <(VersionNumber first, VersionNumber second)
            {
                return (first.x < second.x || first.y < second.y || first.z < second.z);
            }
            public static bool operator >(VersionNumber first, VersionNumber second)
            {
                return (first.x > second.x || first.y > second.y || first.z > second.z);
            }
            public static bool operator ==(VersionNumber first, VersionNumber second)
            {
                return (first.x == second.x && first.y == second.y && first.z == second.z);
            }
            public static bool operator !=(VersionNumber first, VersionNumber second)
            {
                return !(first == second);
            }
            public static bool operator <(VersionNumber first, string s)
            {
                VersionNumber second = new VersionNumber(s);
                if (!second.valid)
                    return false;
                return (first.x < second.x || first.y < second.y || first.z < second.z);
            }
            public static bool operator >(VersionNumber first, string s)
            {
                VersionNumber second = new VersionNumber(s);
                if (!second.valid)
                    return false;
                return (first.x > second.x || first.y > second.y || first.z > second.z);
            }
            public static bool operator ==(VersionNumber first, string s)
            {
                VersionNumber second = new VersionNumber(s);
                if (!second.valid)
                    return false;
                return (first.x == second.x && first.y == second.y && first.z == second.z);
            }
            public static bool operator !=(VersionNumber first, string s)
            {
                VersionNumber second = new VersionNumber(s);
                if (!second.valid)
                    return false;
                return !(first == second);
            }
        }

        public static int level;

        private static VersionNumber _versionNumber = new VersionNumber("1.1.1");

        public static VersionNumber VersionNum { get { return _versionNumber; } }

        public string Name { get { return "HSPE"; } }

        public string Version { get { return _versionNumber.ToString(); } }

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
