using System;
using System.Linq;
using UnityEngine;

namespace VideoExport.Extensions
{
    public class MP4Extension : AFFMPEGBasedExtension
    {
        private enum Codec
        {
            H264,
            H265
        }

        private enum Preset
        {
            VerySlow,
            Slower,
            Slow,
            Medium,
            Fast,
            Faster,
            VeryFast,
            SuperFast,
            UltraFast
        }

        private readonly string[] _codecNames = {"H.264", "H.265"};
        private readonly string[] _codecCLIOptions = {"libx264", "libx265" };
        private readonly string[] _presetNames = {"Very Slow", "Slower", "Slow", "Medium", "Fast", "Faster", "Very Fast", "Super Fast", "Ultra Fast"};
        private readonly string[] _presetCLIOptions;

        private Codec _codec;
        private int _quality;
        private Preset _preset;

        public MP4Extension() : base()
        {
            this._codec = (Codec)VideoExport._configFile.AddInt("mp4Codec", (int)Codec.H264, true);
            this._quality = VideoExport._configFile.AddInt("mp4Quality", 18, true);
            this._preset = (Preset)VideoExport._configFile.AddInt("mp4Preset", (int)Preset.Slower, true);

            this._presetCLIOptions = Enum.GetNames(typeof(Preset)).Select(n => n.ToLowerInvariant()).ToArray();
        }

        public override string GetArguments(string framesFolder, string prefix, string postfix, string inputExtension, int fps, bool transparency, bool resize, int resizeX, int resizeY, string fileName)
        {
            this._progress = 1;
            return $"-loglevel error -r {fps} -f image2 -i \"{framesFolder}/{prefix}%d{postfix}.{inputExtension}\" -tune animation {this.CompileFilters(resize, resizeX, resizeY)} -vcodec {this._codecCLIOptions[(int)this._codec]} -pix_fmt yuv420p -crf {this._quality} -preset {this._presetCLIOptions[(int)this._preset]} -progress pipe:1 \"{fileName}.mp4\"";
        }

        public override void DisplayParams()
        {
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("Codec", GUILayout.ExpandWidth(false));
                this._codec = (Codec)GUILayout.SelectionGrid((int)this._codec, this._codecNames, 2);
            }
            GUILayout.EndHorizontal();

            if (this._codec == Codec.H265)
            {
                Color c = GUI.color;
                GUI.color = Color.yellow;
                GUILayout.Label("The H.265 codec will give you smaller file sizes for the same visual quality as H.264 but it will not embed correctly in most browsers/Discord.");
                GUI.color = c;
            }

            GUILayout.Label("Quality (lower is better but 18 is visually lossless)");
            GUILayout.BeginHorizontal();
            {
                this._quality = Mathf.RoundToInt(GUILayout.HorizontalSlider(this._quality, 0, 51));
                GUILayout.Label(this._quality.ToString("00"), GUILayout.ExpandWidth(false));
            }
            GUILayout.EndHorizontal();

            GUILayout.Label("Encoding Preset (slower = better quality/filesize, \"Medium\" is a good compromise)", GUILayout.ExpandWidth(false));
            this._preset = (Preset)GUILayout.SelectionGrid((int)this._preset, this._presetNames, 3);

            base.DisplayParams();
        }

        public override void SaveParams()
        {
            VideoExport._configFile.SetInt("mp4Codec", (int)this._codec);
            VideoExport._configFile.SetInt("mp4Quality", this._quality);
            VideoExport._configFile.SetInt("mp4Preset", (int)this._preset);
            base.SaveParams();
        }
    }
}
