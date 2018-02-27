using IllusionPlugin;
using UnityEngine;

namespace FogEditor
{
    public class FogEditor : IEnhancedPlugin
    {
        public string Name { get { return "FogEditor"; } }
        public string Version { get { return "1.1.0"; } }

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
                new GameObject("FogEditor", typeof(MainWindow));
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
