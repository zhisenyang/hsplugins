using UnityEngine;

namespace HSIBL
{
    public struct SkyboxParams
    {
        internal float exposure;
        internal float rotation;
        internal Color tint;
        public SkyboxParams(float a, float b, Color A)
        {
            exposure = a;
            rotation = b;
            tint = A;
        }
    };
    class SkyboxManager
    {
        public void ApplySkybox()
        {
            RenderSettings.skybox = skybox;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Skybox;
            RenderSettings.defaultReflectionMode = UnityEngine.Rendering.DefaultReflectionMode.Skybox;

        }
        public void ApplySkyboxParams()
        {

            skybox.SetFloat("_Exposure", skyboxparams.exposure);
            skybox.SetColor("_Tint", skyboxparams.tint);
            skybox.SetFloat("_Rotation", skyboxparams.rotation);
        }
        Material skybox;
        public SkyboxParams skyboxparams = new SkyboxParams(1f,0f,Color.gray);
        public Material Skybox
        {
            get { return skybox; }
            set { skybox = value; }
        }
    }

}
