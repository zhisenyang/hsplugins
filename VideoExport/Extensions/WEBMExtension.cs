using IllusionPlugin;
using UnityEngine;

namespace VideoExport.Extensions
{
    public class WEBMExtension : AFFMPEGBasedExtension
    {
        private int _quality;

        public WEBMExtension()
        {
            this._quality = ModPrefs.GetInt("VideoExport", "webmQuality", 15, true);
        }

        public override string GetArguments(string framesFolder, string inputExtension, int fps, bool transparency, bool resize, int resizeX, int resizeY, string fileName)
        {
            this._progress = 1;
            return $"-loglevel error -r {fps} -f image2 -i \"{framesFolder}/%d.{inputExtension}\" {this.CompileFilters(resize, resizeX, resizeY)} -pix_fmt {(transparency ? "yuva422p" : "yuv422p")} -c:v libvpx-vp9 -crf {this._quality} -b:v 0 -deadline best -progress pipe:1 \"{fileName}.webm\"";
        }

        public override void DisplayParams()
        {
            GUILayout.Label("Quality (lower is better, 15-35 is recommended)");
            GUILayout.BeginHorizontal();
            {
                this._quality = Mathf.RoundToInt(GUILayout.HorizontalSlider(this._quality, 0, 63));
                GUILayout.Label(this._quality.ToString("00"), GUILayout.ExpandWidth(false));
            }
            GUILayout.EndHorizontal();
        }

        public override void SaveParams()
        {
            ModPrefs.SetInt("VideoExport", "webmQuality", this._quality);
        }
    }
}
