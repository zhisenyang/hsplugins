using IllusionPlugin;
using UnityEngine;
using System.Reflection;

namespace HSIBL
{
    public class HSIBLPlugin : IEnhancedPlugin
    {
        public string Name { get { return GetType().Name; } }
        public string Version { get { return Assembly.GetExecutingAssembly().GetName().Version.ToString(); } }
        public string[] Filter { get { return new[] { "HoneySelect_64", "HoneySelect_32", "StudioNEO_32", "StudioNEO_64", "Honey Select Unlimited_64", "Honey Select Unlimited_32" }; } }

        public void OnLevelWasLoaded(int level)
        {

        }

        public void OnUpdate() { }
        public void OnLateUpdate() { }
        public void OnApplicationStart() { }
        public void OnApplicationQuit() { }
        public void OnLevelWasInitialized(int level)
        {
            switch (Application.productName)
            {
                case "StudioNEO":
                    if (level == 3 && !GameObject.Find("HSIBL"))
                    {
                        GameObject hsibl = new GameObject("HSIBL");
                        hsibl.AddComponent<HSIBL>();
                        hsibl.AddComponent<CameraCtrlOffStudio>();
                    }
                    break;
                case "HoneySelect":
                case "Honey Select Unlimited":
                    switch (level)
                    {
                        case 15:
                            GameObject hsibl = new GameObject("HSIBL");
                            hsibl.AddComponent<HSIBL>();
                            hsibl.AddComponent<CameraCtrlOffGame>();
                            break;
                        case 21:
                        case 22:
                            hsibl = new GameObject("HSIBL");
                            hsibl.AddComponent<HSIBL>();
                            hsibl.AddComponent<CameraCtrlOffCM>();
                            break;
                    }
                    break;
            }
        }
        public void OnFixedUpdate() { }
    }
}
