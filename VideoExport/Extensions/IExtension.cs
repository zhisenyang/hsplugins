using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VideoExport.Extensions
{
    public interface IExtension
    {
        int progress { get; }
        bool canProcessStandardOutput { get; }
        bool canProcessStandardError { get; }

        string GetExecutable();
        string GetArguments(string framesFolder, string inputExtension, int fps, bool transparency, bool resize, int resizeX, int resizeY, string fileName);
        void ProcessStandardOutput(char c);
        void ProcessStandardError(char c);
        void DisplayParams();
        void SaveParams();
    }

    public enum ExtensionsType
    {
        MP4,
        WEBM,
        GIF,
        AVI
    }

}
