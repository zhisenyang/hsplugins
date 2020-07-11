using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PackPlugins
{
    public class PackArchive
    {
        public string Name;
        public string RootDirectory;
        public string DestinationDirectory;
        public string CopyTo;
        public string[] Files;

        public void Pack()
        {
            string destinationPath = Path.Combine(this.DestinationDirectory, this.Name + ".zip");
            Console.WriteLine($"Packing archive \"{destinationPath}\"");
            if (File.Exists(destinationPath))
            {
                Console.WriteLine("Archive already exists, removing...");
                File.Delete(destinationPath);
            }
            Console.WriteLine("Initializing archive...");
            string destinationDirectory = Path.GetDirectoryName(destinationPath);
            if (Directory.Exists(destinationDirectory) == false)
                Directory.CreateDirectory(destinationDirectory);
            using (FileStream fileStream = new FileStream(destinationPath, FileMode.CreateNew))
            {
                using (ZipArchive archive = new ZipArchive(fileStream, ZipArchiveMode.Create, true))
                {
                    Console.WriteLine("Adding files...");
                    foreach (string file in this.Files)
                    {
                        Console.WriteLine($"\tCreating file entry \"{file}\"");
                        ZipArchiveEntry entry = archive.CreateEntry(file, CompressionLevel.Optimal);
                        using (Stream zipStream = entry.Open())
                        {
                            string fullPath = Path.Combine(this.RootDirectory, file);
                            byte[] bytes = File.ReadAllBytes(fullPath);
                            zipStream.Write(bytes, 0, bytes.Length);
                        }
                        Console.WriteLine("\tFile added");
                    }
                }
            }
            if (string.IsNullOrEmpty(this.CopyTo) == false)
            {
                Console.WriteLine("Copying to " + this.CopyTo);
                if (Directory.Exists(this.CopyTo) == false)
                    Directory.CreateDirectory(this.CopyTo);
                
                File.Copy(destinationPath, Path.Combine(this.CopyTo, this.Name + ".zip"), true);
            }
            Console.WriteLine("Done\n");
        }
    }
}
