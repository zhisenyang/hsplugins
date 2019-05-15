using System;
using System.Text;
using IllusionPlugin;
using UnityEngine;

namespace VideoExport.Extensions
{
    public abstract class AFFMPEGBasedExtension : IExtension
    {
        protected enum Rotation
        {
            None,
            CW90,
            CW180,
            CW270
        }

        private const string _ffmpegFolder = VideoExport._pluginFolder + "ffmpeg/";
        private static readonly string _ffmpegExe;
        private static readonly string[] _rotationNames = new[] {"None", "90° CW", "180°", "90° CCW"};


        protected static Rotation _rotation = Rotation.None;
        protected int _progress;

        private StringBuilder _outputBuilder = new StringBuilder();
        private StringBuilder _errorBuilder = new StringBuilder();

        public virtual int progress { get { return this._progress; } }
        public bool canProcessStandardOutput { get { return true; } }
        public bool canProcessStandardError { get{ return true; } }

        static AFFMPEGBasedExtension()
        {
            if (IntPtr.Size == 8)
                _ffmpegExe = _ffmpegFolder + "ffmpeg-64.exe";
            else
                _ffmpegExe = _ffmpegFolder + "ffmpeg.exe";
        }

        protected AFFMPEGBasedExtension()
        {
            _rotation = (Rotation)ModPrefs.GetInt("VideoExport", "ffmpegRotation", (int)Rotation.None, true);
        }

        public virtual string GetExecutable()
        {
            return _ffmpegExe;
        }

        public abstract string GetArguments(string framesFolder, string inputExtension, int fps, bool transparency, bool resize, int resizeX, int resizeY, string fileName);

        public virtual void ProcessStandardOutput(char c)
        {
            if (c == '\n')
            {
                string line = this._outputBuilder.ToString();
                if (line.IndexOf("frame=", StringComparison.Ordinal) == 0)
                {
                    string frameString = line.Substring(6);
                    int frame;
                    if (int.TryParse(frameString, out frame))
                        this._progress = frame;
                }
                this._outputBuilder = new StringBuilder();
            }
            else
                this._outputBuilder.Append(c);
        }

        public virtual void ProcessStandardError(char c)
        {
            if (c == '\n')
            {
                string line = this._errorBuilder.ToString();
                UnityEngine.Debug.LogWarning(line);
                this._errorBuilder = new StringBuilder();
            }
            else
                this._errorBuilder.Append(c);
        }

        public virtual void DisplayParams()
        {
            GUILayout.Label("Rotation", GUILayout.ExpandWidth(false));
            _rotation = (Rotation)GUILayout.SelectionGrid((int)_rotation, _rotationNames, 4);
        }

        public virtual void SaveParams()
        {
            ModPrefs.SetInt("VideoExport", "ffmpegRotation", (int)_rotation);
        }

        protected string CompileFilters(bool resize, int resizeX, int resizeY)
        {
            bool hasFilters = false;
            string res = "";

            if (resize)
            {
                res += $"scale={resizeX}x{resizeY}:flags=lanczos";
                hasFilters = true;
            }

            if (_rotation != Rotation.None)
            {
                for (int i = 0; i < (int)_rotation; i++)
                {
                    if (hasFilters == false && i == 0)
                        res += "transpose=1";
                    else
                        res += ",transpose=1";
                }
                hasFilters = true;
            }

            if (hasFilters)
                res = "-vf \"" + res + "\"";

            return res;
        }
    }
}
