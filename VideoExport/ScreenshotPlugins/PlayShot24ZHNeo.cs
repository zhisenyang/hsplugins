using System;
using System.Reflection;
using Harmony;
using IllusionPlugin;
using UnityEngine;

namespace VideoExport.ScreenshotPlugins
{
    public class PlayShot24ZHNeo : IScreenshotPlugin
    {
        private delegate void CaptureFunctionDelegate(int size);

        private static byte[] _currentBytes;
        private static bool _recordingVideo = false;

        private CaptureFunctionDelegate _captureFunction;
        private CaptureFunctionDelegate _captureFunctionTransparent;
        private bool _transparent = false;
        private int _currentSize = 1;

        public string name { get { return "PlayShot"; } }
        public Vector2 currentSize { get{return new Vector2(Screen.width * this._currentSize, Screen.height * this._currentSize);} }
        public bool transparency { get { return this._transparent; } }
        public string extension { get { return "png"; } }

        public bool Init()
        {
            Type playShotType = Type.GetType("ScreenShot,PlayShot24ZHNeo");
            if (playShotType == null)
                return false;
            Component c = (Component)GameObject.FindObjectOfType(playShotType);
            if (c == null)
                return false;
            MethodInfo myScreenShot = playShotType.GetMethod("myScreenShot", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo myScreenShotTransparent = playShotType.GetMethod("myScreenShotTransparent", BindingFlags.Public | BindingFlags.Instance);
            if (myScreenShot == null || myScreenShotTransparent == null)
            {
                UnityEngine.Debug.LogError("VideoExport: PlayShot24ZHNeo was found but seems out of date, please update it.");
                return false;
            }
            this._captureFunction = (CaptureFunctionDelegate)Delegate.CreateDelegate(typeof(CaptureFunctionDelegate), c, myScreenShot);
            this._captureFunctionTransparent = (CaptureFunctionDelegate)Delegate.CreateDelegate(typeof(CaptureFunctionDelegate), c, myScreenShotTransparent);
            this._transparent = ModPrefs.GetBool("VideoExport", "PlayShot24ZHNeo_transparent", false, true);
            this._currentSize = ModPrefs.GetInt("VideoExport", "PlayShot24ZHNeo_size", 1, true);
            return true;
        }

        public byte[] Capture(bool forcePng = false)
        {
            _recordingVideo = true;
            if (this._transparent)
                this._captureFunctionTransparent(this._currentSize);
            else
                this._captureFunction(this._currentSize);
            _recordingVideo = false;
            return _currentBytes;
        }

        public void DisplayParams()
        {
            GUILayout.BeginHorizontal();
            this._transparent = GUILayout.Toggle(this._transparent, "Transparent");
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("Size Multiplier", GUILayout.ExpandWidth(false));
                this._currentSize = Mathf.RoundToInt(GUILayout.HorizontalSlider(this._currentSize, 1, 4));
                GUILayout.Label(this._currentSize.ToString(), GUILayout.Width(15));
            }
            GUILayout.EndHorizontal();
        }

        public void SaveParams()
        {
            ModPrefs.SetBool("VideoExport", "PlayShot24ZHNeo_transparent", this._transparent);
            ModPrefs.SetInt("VideoExport", "PlayShot24ZHNeo_size", this._currentSize);
        }

        [HarmonyPatch]
        private static class ScreenShot_myScreenShot_Patches
        {
            private static bool Prepare()
            {
                return Type.GetType("ScreenShot,PlayShot24ZHNeo") != null;
            }

            private static MethodInfo TargetMethod()
            {
                return Type.GetType("ScreenShot,PlayShot24ZHNeo").GetMethod("myScreenShot", BindingFlags.Public | BindingFlags.Instance);
            }

            private static bool Prefix(int size)
            {
                if (_recordingVideo == false)
                    return true;
                Texture2D texture2D = new Texture2D(Screen.width * size, Screen.height * size, (TextureFormat)3, false);
                RenderTexture renderTexture = new RenderTexture(texture2D.width, texture2D.height, 24);
                Camera main = Camera.main;
                if (main.isActiveAndEnabled && !(main.targetTexture != null))
                {
                    RenderTexture targetTexture = main.targetTexture;
                    main.targetTexture = renderTexture;
                    main.Render();
                    main.targetTexture = targetTexture;
                    RenderTexture.active = renderTexture;
                    texture2D.ReadPixels(new Rect(0f, 0f, (float)texture2D.width, (float)texture2D.height), 0, 0);
                    texture2D.Apply();
                    RenderTexture.active = null;
                    _currentBytes = texture2D.EncodeToPNG();
                }
                return false;
            }

        }
        [HarmonyPatch]
        private static class ScreenShot_myScreenShotTransparent_Patches
        {
            private static bool Prepare()
            {
                return Type.GetType("ScreenShot,PlayShot24ZHNeo") != null;
            }

            private static MethodInfo TargetMethod()
            {
                return Type.GetType("ScreenShot,PlayShot24ZHNeo").GetMethod("myScreenShotTransparent", BindingFlags.Public | BindingFlags.Instance);
            }

            private static bool Prefix(int size)
            {
                if (_recordingVideo == false)
                    return true;
                Texture2D texture2D = new Texture2D(Screen.width * size, Screen.height * size, (TextureFormat)3, false);
                Texture2D texture2D2 = new Texture2D(Screen.width * size, Screen.height * size, (TextureFormat)3, false);
                Texture2D texture2D3 = new Texture2D(Screen.width * size, Screen.height * size, (TextureFormat)5, false);
                RenderTexture renderTexture = new RenderTexture(texture2D.width, texture2D.height, 24);
                RenderTexture renderTexture2 = new RenderTexture(texture2D.width, texture2D.height, 24);
                Camera main = Camera.main;
                if (main.isActiveAndEnabled && !(main.targetTexture != null))
                {
                    Color backgroundColor = main.backgroundColor;
                    RenderTexture targetTexture = main.targetTexture;
                    main.backgroundColor = Color.white;
                    main.targetTexture = renderTexture;
                    main.Render();
                    main.targetTexture = targetTexture;
                    RenderTexture.active = renderTexture;
                    texture2D.ReadPixels(new Rect(0f, 0f, (float)texture2D.width, (float)texture2D.height), 0, 0);
                    texture2D.Apply();
                    main.backgroundColor = Color.black;
                    main.targetTexture = renderTexture2;
                    main.Render();
                    main.targetTexture = targetTexture;
                    RenderTexture.active = renderTexture2;
                    texture2D2.ReadPixels(new Rect(0f, 0f, (float)texture2D.width, (float)texture2D.height), 0, 0);
                    texture2D2.Apply();
                    RenderTexture.active = null;
                    for (int i = 0; i < texture2D3.height; i++)
                    {
                        for (int j = 0; j < texture2D3.width; j++)
                        {
                            float num = texture2D.GetPixel(j, i).r - texture2D2.GetPixel(j, i).r;
                            num = 1f - num;
                            Color color;
                            if (num == 0f)
                            {
                                color = Color.clear;
                            }
                            else
                            {
                                color = texture2D2.GetPixel(j, i) / num;
                            }
                            color.a = num;
                            texture2D3.SetPixel(j, i, color);
                        }
                    }
                    _currentBytes = texture2D3.EncodeToPNG();
                    main.backgroundColor = backgroundColor;
                }
                return false;
            }
        }

    }
}
