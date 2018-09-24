using IllusionPlugin;
using UnityEngine;
using System.Reflection;

namespace HSIBL
{
    public class HSIBLPlugin : IEnhancedPlugin
    {
        public string Name { get { return GetType().Name; } }
        public string Version { get { return Assembly.GetExecutingAssembly().GetName().Version.ToString(); } }
        public string[] Filter { get { return new[] {"StudioNEO_32", "StudioNEO_64", "HoneySelect_32", "HoneySelect_64"}; } }

        public void OnLevelWasLoaded(int level)
        {

        }

        public void OnUpdate() { }
        public void OnLateUpdate() { }
        public void OnApplicationStart() { }
        public void OnApplicationQuit() { }
        public void OnLevelWasInitialized(int level)
        {
            if ((Application.productName == "StudioNEO" && level == 3)  && !GameObject.Find("HSIBL"))
            {
                GameObject HSIBL = new GameObject("HSIBL");
                HSIBL.AddComponent<HSIBL>();
                HSIBL.AddComponent<CameraCtrlOffStudio>();
            }
            else if (Application.productName == "HoneySelect" && level == 15)
            {
                GameObject HSIBL = new GameObject("HSIBL");
                HSIBL.AddComponent<HSIBL>();
                HSIBL.AddComponent<CameraCtrlOffGame>();
            }
            else if (Application.productName == "HoneySelect" && (level == 21 || level == 22))
            {
                GameObject HSIBL = new GameObject("HSIBL");
                HSIBL.AddComponent<HSIBL>();
                HSIBL.AddComponent<CameraCtrlOffCM>();
            }
        }
        public void OnFixedUpdate() { }
    }
}
