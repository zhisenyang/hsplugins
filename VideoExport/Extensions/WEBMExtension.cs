using IllusionPlugin;
using UnityEngine;

namespace VideoExport.Extensions
{
    public class WEBMExtension : AFFMPEGBasedExtension
    {
        private enum Codec
        {
            VP8,
            VP9
        }

        private readonly string[] _codecNames = { "VP8", "VP9" };

        private Codec _codec;
        private int _quality;

        public WEBMExtension()
        {
            this._codec = (Codec)ModPrefs.GetInt("VideoExport", "webmCodec", (int)Codec.VP9, true);
            this._quality = ModPrefs.GetInt("VideoExport", "webmQuality", 15, true);
        }

        public override string GetArguments(string framesFolder, string inputExtension, int fps, bool transparency, bool resize, int resizeX, int resizeY, string fileName)
        {
            this._progress = 1;
            return $"-loglevel error -r {fps} -f image2 -i \"{framesFolder}/%d.{inputExtension}\" {this.CompileFilters(resize, resizeX, resizeY)} -c:v libvpx{(this._codec == Codec.VP9 ? "-vp9" : " -qmin 0")} -pix_fmt {(transparency ? "yuva422p -metadata:s:v:0 alpha_mode=\"1\"" : "yuv422p")} -auto-alt-ref 0 -crf {this._quality} {(this._codec == Codec.VP8 ? "-b:v 10M" : "-b:v 0")} -deadline best -progress pipe:1 \"{fileName}.webm\"";
        }

        public override void DisplayParams()
        {
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("Codec", GUILayout.ExpandWidth(false));
                this._codec = (Codec)GUILayout.SelectionGrid((int)this._codec, this._codecNames, 2);
            }
            GUILayout.EndHorizontal();

            if (this._codec == Codec.VP9)
            {
                Color c = GUI.color;
                GUI.color = Color.yellow;
                GUILayout.Label("The VP9 codec will give you smaller file sizes and better quality than VP8 but it might not be supported on phones");
                GUI.color = c;
            }

            switch (this._codec)
            {
                case Codec.VP8:
                    GUILayout.Label("Quality (lower is better, 10 is a good starting point)");
                    break;
                case Codec.VP9:
                    GUILayout.Label("Quality (lower is better, 15-35 is recommended)");
                    break;
            }
            GUILayout.BeginHorizontal();
            {
                this._quality = Mathf.RoundToInt(GUILayout.HorizontalSlider(this._quality, 0, 63));
                GUILayout.Label(this._quality.ToString("00"), GUILayout.ExpandWidth(false));
            }
            GUILayout.EndHorizontal();
        }

        public override void SaveParams()
        {
            ModPrefs.SetInt("VideoExport", "webmCodec", (int)this._codec);
            ModPrefs.SetInt("VideoExport", "webmQuality", this._quality);
        }
    }
}
