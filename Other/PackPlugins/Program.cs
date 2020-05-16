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
            Console.WriteLine("Select a pack profile:");
            for (int i = 0; i < _profiles.Length; i++)
            {
                PackProfile profile = _profiles[i];
                Console.WriteLine($"{i}: {profile.Name}");
            }
            string line = Console.ReadLine();
            if (int.TryParse(line, out int index))
            {
                _profiles[index].Pack();
                Console.WriteLine("Successfully packed");
            }
            else
                Console.WriteLine("Try again");
            Console.ReadKey();
        }

        private static readonly PackProfile[] _profiles = 
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
                    }
                }
            },
        };
    }
}
