using UnityEngine;

namespace HSIBL
{
    public struct ProceduralSkyboxParams
    {
        internal float exposure;
        internal float sunsize;
        internal float atmospherethickness;
        internal Color skytint;
        internal Color groundcolor;
        public ProceduralSkyboxParams(float a, float b, float c,Color A,Color B)
        {
            exposure = a;
            sunsize = b;
            atmospherethickness = c;
            skytint = A;
            groundcolor = B;
        }
    };
    class ProceduralSkyboxManager
    {
        public void Init()
        {
            AssetBundle cubemapbundle = AssetBundle.LoadFromFile(Application.dataPath + "/../abdata/plastic/proceduralskybox.unity3d");
            proceduralsky = cubemapbundle.LoadAsset<Material>("Procedural Skybox");
            cubemapbundle.Unload(false);
            cubemapbundle = null;
            //proceduralsky = new Material();
        }
        public void ApplySkybox()
        {
            RenderSettings.skybox = proceduralsky;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Skybox;
            RenderSettings.defaultReflectionMode = UnityEngine.Rendering.DefaultReflectionMode.Skybox;
        }
        public void ApplySkyboxParams()
        {
            proceduralsky.SetFloat("_SunDisk", 2f);
            proceduralsky.SetFloat("_Exposure", skyboxparams.exposure);
            proceduralsky.SetFloat("_SunSize", skyboxparams.sunsize);
            proceduralsky.SetColor("_SkyTint", skyboxparams.skytint);
            proceduralsky.SetColor("_GroundColor", skyboxparams.groundcolor);
            proceduralsky.SetFloat("_AtmosphereThickness", skyboxparams.atmospherethickness);
        }
        Material proceduralsky;
        public ProceduralSkyboxParams skyboxparams = new ProceduralSkyboxParams(1f,0.1f,1f,Color.gray,Color.gray);
        public Material Proceduralsky
        {
            get { return proceduralsky; }
            set { proceduralsky = value; }
        }
    }
    
}
