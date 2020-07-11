using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PackPlugins
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("0: Pack");
            Console.WriteLine("1: Release");
            string line = Console.ReadLine();
            if (int.TryParse(line, out int index))
            {
                switch (index)
                {
                    case 0:
                        Pack();
                        break;
                    case 1:
                        Release();
                        break;
                }
            }
            else
                Console.WriteLine("Try again");
            Console.ReadKey();
        }

        private static void Pack()
        {
            Console.WriteLine("Select a pack profile:");
            for (int i = 0; i < _packProfiles.Length; i++)
            {
                PackProfile profile = _packProfiles[i];
                Console.WriteLine($"{i}: {profile.Name}");
            }
            string line = Console.ReadLine();
            if (int.TryParse(line, out int index))
            {
                _packProfiles[index].Pack();
                Console.WriteLine("Successfully packed");
            }
            else
                Console.WriteLine("Try again");
        }

        private static void Release()
        {
            Console.WriteLine("Select a release profile:");
            for (int i = 0; i < _releaseProfiles.Length; i++)
            {
                ReleaseProfile profile = _releaseProfiles[i];
                Console.WriteLine($"{i}: {profile.Name}");
            }
            string line = Console.ReadLine();
            if (int.TryParse(line, out int index))
            {
                Console.WriteLine("Type in version");
                line = Console.ReadLine();
                line = line.Replace("\n", "");
                try
                {
                    new Version(line);
                }
                catch
                {
                    Console.WriteLine("Try again");
                    return;
                }
                _releaseProfiles[index].Release(line);
                Console.WriteLine("Successfully released");
            }
            else
                Console.WriteLine("Try again");

        }

        private static readonly PackProfile[] _packProfiles = 
        {
            new PackProfile()
            {
                Name = "HSPE",
                Archives = new []
                {
                    new PackArchive()
                    {
                        Name = "HSPE",
                        RootDirectory = @"D:\Program Files (x86)\HoneySelect",
                        DestinationDirectory = @"D:\Program Files (x86)\HoneySelect\Modding",
                        Files = new []
                        {
                            @"Plugins\HSPENeo.dll",
                            @"abdata\studioneo\Joan6694\dynamic_bone_collider.unity3d",
                            @"abdata\studioneo\HoneyselectItemResolver\Joan6694 Dynamic Bone Collider.txt",
                        }
                    },
                    new PackArchive()
                    {
                        Name = "KKPE",
                        RootDirectory = @"D:\Program Files (x86)\Koikatu",
                        DestinationDirectory = @"D:\Program Files (x86)\Koikatu\Modding",
                        Files = new []
                        {
                            @"BepInEx\plugins\KKPE.dll",
                            @"mods\Joan6694DynamicBoneCollider.zipmod",
                        }
                    },
                    new PackArchive()
                    {
                        Name = "AIPE",
                        RootDirectory = @"D:\Program Files (x86)\AI-Syoujyo",
                        DestinationDirectory = @"D:\Program Files (x86)\AI-Syoujyo\Modding",
                        Files = new []
                        {
                            @"BepInEx\plugins\AIPE.dll",
                            @"mods\Joan6694DynamicBoneColliders.zipmod",
                        }
                    },
                    new PackArchive()
                    {
                        Name = "HS2PE",
                        RootDirectory = @"D:\Program Files (x86)\HoneySelect2",
                        DestinationDirectory = @"D:\Program Files (x86)\HoneySelect2\Modding",
                        Files = new []
                        {
                            @"BepInEx\plugins\HS2PE.dll",
                            @"mods\Joan6694DynamicBoneColliders.zipmod",
                        }
                    }
                }
            },
            new PackProfile()
            {
                Name = "VideoExport",
                Archives = new []
                {
                    new PackArchive()
                    {
                        Name = "HSVideoExport",
                        RootDirectory = @"D:\Program Files (x86)\HoneySelect",
                        DestinationDirectory = @"D:\Program Files (x86)\HoneySelect\Modding",
                        Files = new []
                        {
                            @"Plugins\VideoExport.dll",
                            @"Plugins\VideoExport\ffmpeg\ffmpeg.exe",
                            @"Plugins\VideoExport\ffmpeg\ffmpeg-64.exe",
                            @"Plugins\VideoExport\ffmpeg\LICENSE.txt",
                            @"Plugins\VideoExport\gifski\gifski.exe",
                            @"Plugins\VideoExport\gifski\LICENSE",
                        }
                    }, 
                    new PackArchive()
                    {
                        Name = "KKVideoExport",
                        RootDirectory = @"D:\Program Files (x86)\Koikatu",
                        DestinationDirectory = @"D:\Program Files (x86)\Koikatu\Modding",
                        Files = new []
                        {
                            @"BepInEx\plugins\VideoExport.dll",
                            @"BepInEx\plugins\VideoExport\ffmpeg\ffmpeg.exe",
                            @"BepInEx\plugins\VideoExport\ffmpeg\ffmpeg-64.exe",
                            @"BepInEx\plugins\VideoExport\ffmpeg\LICENSE.txt",
                            @"BepInEx\plugins\VideoExport\gifski\gifski.exe",
                            @"BepInEx\plugins\VideoExport\gifski\LICENSE",
                        }
                    }, 
                    new PackArchive()
                    {
                        Name = "AIVideoExport",
                        RootDirectory = @"D:\Program Files (x86)\AI-Syoujyo",
                        DestinationDirectory = @"D:\Program Files (x86)\AI-Syoujyo\Modding",
                        Files = new []
                        {
                            @"BepInEx\plugins\VideoExport.dll",
                            @"BepInEx\plugins\VideoExport\ffmpeg\ffmpeg.exe",
                            @"BepInEx\plugins\VideoExport\ffmpeg\ffmpeg-64.exe",
                            @"BepInEx\plugins\VideoExport\ffmpeg\LICENSE.txt",
                            @"BepInEx\plugins\VideoExport\gifski\gifski.exe",
                            @"BepInEx\plugins\VideoExport\gifski\LICENSE",
                        }
                    }, 
                    new PackArchive()
                    {
                        Name = "AIVideoExport",
                        RootDirectory = @"D:\Program Files (x86)\HoneySelect2",
                        DestinationDirectory = @"D:\Program Files (x86)\HoneySelect2\Modding",
                        Files = new []
                        {
                            @"BepInEx\plugins\VideoExport.dll",
                            @"BepInEx\plugins\VideoExport\ffmpeg\ffmpeg.exe",
                            @"BepInEx\plugins\VideoExport\ffmpeg\ffmpeg-64.exe",
                            @"BepInEx\plugins\VideoExport\ffmpeg\LICENSE.txt",
                            @"BepInEx\plugins\VideoExport\gifski\gifski.exe",
                            @"BepInEx\plugins\VideoExport\gifski\LICENSE",
                        }
                    }, 
                }
            },
        };

        private static readonly ReleaseProfile[] _releaseProfiles = new[]
        {
            new ReleaseProfile()
            {
                Name = "MoreAccessoriesKOI/EC",
                Files = new []
                {
                    new ReleaseFile()
                    {
                        Name = "MoreAccessories",
                        Path = @"D:\Program Files (x86)\Koikatu\BepInEx\plugins\MoreAccessories.dll",
                        DestinationPath = @"D:\Joan\Mega\Illusion Stuff\KK\KK Plugins\MoreAccessories"
                    }, 
                    new ReleaseFile()
                    {
                        Name = "MoreAccessories",
                        Path = @"D:\Program Files (x86)\EmotionCreators\BepInEx\plugins\MoreAccessories.dll",
                        DestinationPath = @"D:\Joan\Mega\Illusion Stuff\EC\EC Plugins\MoreAccessories"
                    }, 
                }
            }, 
            new ReleaseProfile()
            {
                Name = "MoreAccessoriesAI/HS2",
                Files = new []
                {
                    new ReleaseFile()
                    {
                        Name = "MoreAccessories",
                        Path = @"D:\Program Files (x86)\AI-Syoujyo\BepInEx\plugins\MoreAccessories.dll",
                        DestinationPath = @"D:\Joan\Mega\Illusion Stuff\AI\AI Plugins\MoreAccessories"
                    }, 
                    new ReleaseFile()
                    {
                        Name = "MoreAccessories",
                        Path = @"D:\Program Files (x86)\HoneySelect2\BepInEx\plugins\MoreAccessories.dll",
                        DestinationPath = @"D:\Joan\Mega\Illusion Stuff\HS2\HS2 Plugins\MoreAccessories"
                    }, 
                }
            }, 
        };
    }
}
