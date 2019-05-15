using System;
using System.Reflection;
using IllusionPlugin;
using UnityEngine;

namespace VideoExport.ScreenshotPlugins
{
    class Screencap : IScreenshotPlugin
    {
        private delegate byte[] CaptureFunctionDelegate();

        private CaptureFunctionDelegate _captureFunction;
        private Vector2 _currentSize;

        public string name { get { return "Screencap"; } }
        public Vector2 currentSize { get { return this._currentSize; } }
        public bool transparency { get { return false; } }
        public string extension { get { return "png"; } }
        public bool Init()
        {
            Type screencapType = Type.GetType("ScreencapMB,Screencap");
            if (screencapType == null)
                return false;
            object plugin = GameObject.FindObjectOfType(screencapType);
            if (plugin == null)
                return false;
            MethodInfo captureOpaque = screencapType.GetMethod("CaptureOpaque", BindingFlags.NonPublic | BindingFlags.Instance);
            if (captureOpaque == null)
            {
                UnityEngine.Debug.LogError("VideoExport: Screencap was found but seems out of date, please update it.");
                return false;
            }
            this._captureFunction = (CaptureFunctionDelegate)Delegate.CreateDelegate(typeof(CaptureFunctionDelegate), plugin, captureOpaque);

            int downscalingRate = ModPrefs.GetInt("Screencap", "DownscalingRate", 1, true);
            this._currentSize = new Vector2(ModPrefs.GetInt("Screencap", "Width", 1280, true) * downscalingRate, ModPrefs.GetInt("Screencap", "Height", 720, true) * downscalingRate);
            return true;
        }

        public byte[] Capture(bool forcePng = false)
        {
            return this._captureFunction();
        }

        public void DisplayParams()
        {
        }

        public void SaveParams()
        {
        }
    }
}
