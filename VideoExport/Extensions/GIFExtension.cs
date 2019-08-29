using System.Text;
using UnityEngine;

namespace VideoExport.Extensions
{
    public class GIFExtension : IExtension
    {
        private const string _gifskiFolder = VideoExport._pluginFolder + "gifski/";
        private const string _gifskiExe = _gifskiFolder + "gifski.exe";

        private StringBuilder _errorBuilder = new StringBuilder();

        public int progress { get { return 0; } }
        public bool canProcessStandardOutput { get { return false; } }
        public bool canProcessStandardError { get { return true; } }

        public string GetExecutable()
        {
            return _gifskiExe;
        }

        public string GetArguments(string framesFolder, string prefix, string postfix, string inputExtension, int fps, bool transparency, bool resize, int resizeX, int resizeY, string fileName)
        {
            return $"{(resize ? $"-W {resizeX} -H {resizeY}" : "")} --fps {fps} -o {fileName}.gif {framesFolder}/{prefix}*{postfix}.{inputExtension} --quiet";
        }

        public void ProcessStandardOutput(char c)
        {
        }

        public void ProcessStandardError(char c)
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

        public void DisplayParams()
        {
            Color c = GUI.color;
            GUI.color = Color.yellow;
            GUILayout.Label("It is recommended to downscale your images when using the GIF format (use Resize)");
            GUI.color = c;
        }

        public void SaveParams() {}
    }
}
