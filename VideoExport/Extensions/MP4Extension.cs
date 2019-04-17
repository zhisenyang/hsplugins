using IllusionPlugin;
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

        private readonly string[] _codecNames = {"H.264", "H.265"};
        private readonly string[] _codecCLIOptions = {"libx264", "libx265" };

        private Codec _codec;
        private int _quality;

        public MP4Extension()
        {
            this._codec = (Codec)ModPrefs.GetInt("VideoExport", "mp4Codec", (int)Codec.H264, true);
            this._quality = ModPrefs.GetInt("VideoExport", "mp4Quality", 18, true);
        }

        public override string GetArguments(string framesFolder, string inputExtension, int fps, bool transparency, bool resize, int resizeX, int resizeY, string fileName)
        {
            this._progress = 1;
            return $"-loglevel error -r {fps} -f image2 -i \"{framesFolder}/%d.{inputExtension}\" -tune animation {this.CompileFilters(resize, resizeX, resizeY)} -pix_fmt yuv422p -vcodec {this._codecCLIOptions[(int)this._codec]} -crf {this._quality} -preset slower -progress pipe:1 \"{fileName}.mp4\"";
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
        }

        public override void SaveParams()
        {
            ModPrefs.SetInt("VideoExport", "mp4Codec", (int)this._codec);
            ModPrefs.SetInt("VideoExport", "mp4Quality", this._quality);
        }
    }
}
