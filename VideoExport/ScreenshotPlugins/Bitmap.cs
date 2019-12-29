using System;
using UnityEngine;
#if HONEYSELECT
using Harmony;
#elif KOIKATSU || AISHOUJO
using HarmonyLib;
#endif

namespace VideoExport.ScreenshotPlugins
{
    public class Bitmap : IScreenshotPlugin
    {
        public string name { get { return "Built-in BMP"; } }
        public Vector2 currentSize { get { return new Vector2(Screen.width * this._scaleFactor, Screen.height * this._scaleFactor); } }
        public bool transparency { get { return false; } }
        public string extension { get { return "bmp"; } }

        private float _scaleFactor;
        private Texture2D _texture = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false, true);
        private readonly byte[] _bmpHeader = {
            0x42, 0x4D,
            0, 0, 0, 0,
            0, 0,
            0, 0,
            54, 0, 0, 0,
            40, 0, 0, 0,
            0, 0, 0, 0,
            0, 0, 0, 0,
            1, 0,
            24, 0,
            0, 0, 0, 0,
            0, 0, 0, 0,
            0, 0, 0, 0,
            0, 0, 0, 0,
            0, 0, 0, 0,
            0, 0, 0, 0
        };
        private byte[] _fileBytes = new byte[0];

#if HONEYSELECT
        public bool Init(HarmonyInstance harmony)
#elif KOIKATSU || AISHOUJO
        public bool Init(Harmony harmony)
#endif
        {
            this._scaleFactor = VideoExport._configFile.AddFloat("bmpSizeMultiplier", 1f, true);
            return true;
        }


        public byte[] Capture(bool forcePng = false)
        {
            Vector2 size = this.currentSize;
            int width = Mathf.RoundToInt(size.x);
            if (width % 2 != 0)
                width += 1;
            int height = Mathf.RoundToInt(size.y);
            if (height % 2 != 0)
                height += 1;
            if (this._texture.width != width || this._texture.height != height)
            {
                UnityEngine.Object.Destroy(this._texture);
                this._texture = new Texture2D(width, height, TextureFormat.RGB24, false, true);
            }

            RenderTexture cached = Camera.main.targetTexture;
            Camera.main.targetTexture = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);
            Camera.main.Render();
            RenderTexture cached2 = RenderTexture.active;
            RenderTexture.active = Camera.main.targetTexture;
            Camera.main.targetTexture = cached;
            this._texture.ReadPixels(new Rect(0, 0, width, height), 0, 0, false);
            RenderTexture.ReleaseTemporary(RenderTexture.active);
            RenderTexture.active = cached2;

            if (forcePng)
                return this._texture.EncodeToPNG();

            unsafe
            {
                uint byteSize = (uint)(width * height * 3);
                uint fileSize = (uint)(this._bmpHeader.Length + byteSize);
                if (this._fileBytes.Length != fileSize)
                {
                    this._fileBytes = new byte[fileSize];
                    Array.Copy(this._bmpHeader, this._fileBytes, this._bmpHeader.Length);

                    this._fileBytes[2] = ((byte*)&fileSize)[0];
                    this._fileBytes[3] = ((byte*)&fileSize)[1];
                    this._fileBytes[4] = ((byte*)&fileSize)[2];
                    this._fileBytes[5] = ((byte*)&fileSize)[3];

                    this._fileBytes[18] = ((byte*)&width)[0];
                    this._fileBytes[19] = ((byte*)&width)[1];
                    this._fileBytes[20] = ((byte*)&width)[2];
                    this._fileBytes[21] = ((byte*)&width)[3];

                    this._fileBytes[22] = ((byte*)&height)[0];
                    this._fileBytes[23] = ((byte*)&height)[1];
                    this._fileBytes[24] = ((byte*)&height)[2];
                    this._fileBytes[25] = ((byte*)&height)[3];

                    this._fileBytes[34] = ((byte*)&byteSize)[0];
                    this._fileBytes[35] = ((byte*)&byteSize)[1];
                    this._fileBytes[36] = ((byte*)&byteSize)[2];
                    this._fileBytes[37] = ((byte*)&byteSize)[3];
                }

                int i = this._bmpHeader.Length;
                Color32[] pixels = this._texture.GetPixels32();
                foreach (Color32 c in pixels)
                {
                    this._fileBytes[i++] = c.b;
                    this._fileBytes[i++] = c.g;
                    this._fileBytes[i++] = c.r;
                }
                return this._fileBytes;
            }
        }

        public void DisplayParams()
        {
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("Size Multiplier", GUILayout.ExpandWidth(false));
                this._scaleFactor = GUILayout.HorizontalSlider(this._scaleFactor, 1, 8);
                string s = this._scaleFactor.ToString("0.000");
                string newS = GUILayout.TextField(s, GUILayout.Width(40));
                if (newS != s)
                {
                    float res;
                    if (float.TryParse(newS, out res))
                        this._scaleFactor = res;
                }
            }
            GUILayout.EndHorizontal();
        }

        public void SaveParams()
        {
            VideoExport._configFile.SetFloat("bmpSizeMultiplier", this._scaleFactor);
        }
    }
}
