using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PackPlugins
{
    public class ReleaseFile
    {
        public string Name;
        public string Path;
        public string DestinationPath;
        public string NewName;

        public void Release(string version)
        {
            Console.WriteLine($"Releasing {this.Name}");
            string destinationFileName = (this.NewName != null ? this.NewName : System.IO.Path.GetFileNameWithoutExtension(this.Path)) + System.IO.Path.GetExtension(this.Path);
            string destinationDirectory = System.IO.Path.Combine(this.DestinationPath, this.Name + " " + version);
            string destination = System.IO.Path.Combine(destinationDirectory, destinationFileName);
            if (Directory.Exists(destinationDirectory) == false)
                Directory.CreateDirectory(destinationDirectory);
            Console.WriteLine($"\tCopying {this.Path} to {destination}");
            File.Copy(this.Path, destination, true);
            Console.WriteLine("Done\n");
        }
    }
}
