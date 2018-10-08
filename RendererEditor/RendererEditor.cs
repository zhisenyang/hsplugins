using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IllusionPlugin;
using UnityEngine;

namespace RendererEditor
{
    public class RendererEditor : IEnhancedPlugin
    {
        public string Name { get { return "RendererEditor"; } }
        public string Version { get { return "1.2.0"; } }
        public string[] Filter { get { return new[] {"StudioNEO_32", "StudioNEO_64"}; } }

        public void OnApplicationStart()
        {
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
