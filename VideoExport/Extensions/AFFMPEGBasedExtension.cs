﻿using System;
using System.IO;
using System.Text;
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

        private readonly string _ffmpegFolder;
        private readonly string _ffmpegExe;
        private readonly string[] _rotationNames = new[] {"None", "90° CW", "180°", "90° CCW"};

        protected static Rotation _rotation = Rotation.None;
        protected int _progress;

        private StringBuilder _outputBuilder = new StringBuilder();
        private StringBuilder _errorBuilder = new StringBuilder();

        public virtual int progress { get { return this._progress; } }
        public bool canProcessStandardOutput { get { return true; } }
        public bool canProcessStandardError { get{ return true; } }

        protected AFFMPEGBasedExtension()
        {
            this._ffmpegFolder = Path.Combine(VideoExport._pluginFolder, "ffmpeg");
            if (IntPtr.Size == 8)
                this._ffmpegExe = Path.Combine(this._ffmpegFolder, "ffmpeg-64.exe");
            else
                this._ffmpegExe = Path.Combine(this._ffmpegFolder, "ffmpeg.exe");
            _rotation = (Rotation)VideoExport._configFile.AddInt("ffmpegRotation", (int)Rotation.None, true);
        }

        public virtual string GetExecutable()
        {
            return this._ffmpegExe;
        }

        public abstract string GetArguments(string framesFolder, string prefix, string postfix, string inputExtension, int fps, bool transparency, bool resize, int resizeX, int resizeY, string fileName);

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
            _rotation = (Rotation)GUILayout.SelectionGrid((int)_rotation, this._rotationNames, 4);
        }

        public virtual void SaveParams()
        {
            VideoExport._configFile.SetInt("ffmpegRotation", (int)_rotation);
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
