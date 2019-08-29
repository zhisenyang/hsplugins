using IllusionPlugin;
using UnityEngine;

namespace VideoExport.Extensions
{
    public class AVIExtension : AFFMPEGBasedExtension
    {
        private int _quality;

        public AVIExtension() : base()
        {
            this._quality = ModPrefs.GetInt("VideoExport", "aviQuality", 3, true);
        }

        public override string GetArguments(string framesFolder, string prefix, string postfix, string inputExtension, int fps, bool transparency, bool resize, int resizeX, int resizeY, string fileName)
        {
            this._progress = 1;
            return $"-loglevel error -r {fps} -f image2 -i \"{framesFolder}/{prefix}%d{postfix}.{inputExtension}\" {this.CompileFilters(resize, resizeX, resizeY)} -pix_fmt yuv420p -c:v libxvid -qscale:v {this._quality} -progress pipe:1 \"{fileName}.avi\"";
        }

        public override void DisplayParams()
        {
            Color c = GUI.color;
            GUI.color = Color.yellow;
            GUILayout.Label("Only use this format if you want your video to be compatible with older systems, otherwise prefer MP4/WEBM");
            GUI.color = c;


            GUILayout.Label("Quality (lower is better)");
            GUILayout.BeginHorizontal();
            {
                this._quality = Mathf.RoundToInt(GUILayout.HorizontalSlider(this._quality, 1, 31));
                GUILayout.Label(this._quality.ToString("00"), GUILayout.ExpandWidth(false));
            }
            GUILayout.EndHorizontal();
            base.DisplayParams();
        }

        public override void SaveParams()
        {
            ModPrefs.SetInt("VideoExport", "aviQuality", this._quality);
            base.SaveParams();
        }
    }
}
