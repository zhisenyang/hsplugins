using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using BegoneThot.Properties;
using Unity_Studio;

namespace BegoneThot
{
    /// <summary>
    /// Logique d'interaction pour MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const string _infoPath = "studioneo\\info";
        private const string _listFileName = "00.unity3d";
        private const string _extractedListFileName = "ItemList_00_00.MonoBehaviour";


        public MainWindow()
        {
            this.InitializeComponent();
            this.PathField.Text = Settings.Default.abdataPath;
        }

        private async void DoTheThing(object sender, RoutedEventArgs e)
        {
            //Console.WriteLine(@"C:\Program Files (x86)\HoneySelect\illusion\HoneySelect\abdata\hooh\fuckingpool.unity3d");
            //this.GetAnimatorsNoDependency(@"C:\Program Files (x86)\HoneySelect\illusion\HoneySelect\abdata\hooh\fuckingpool.unity3d", out HashSet<string> animators);
            //Console.WriteLine(@"C:\Program Files (x86)\HoneySelect\illusion\HoneySelect\abdata\studioneo\itemobject\DSWeapons.unity3d");
            //this.GetAnimatorsNoDependency(@"C:\Program Files (x86)\HoneySelect\illusion\HoneySelect\abdata\studioneo\itemobject\DSWeapons.unity3d", out animators);
            //Console.WriteLine(@"C:\Program Files (x86)\HoneySelect\illusion\HoneySelect\abdata\studio\itemobj\honey\Tacozera\AdultWaterGun_Zera.unity3d");
            //this.GetAnimatorsNoDependency(@"C:\Program Files (x86)\HoneySelect\illusion\HoneySelect\abdata\studio\itemobj\honey\Tacozera\AdultWaterGun_Zera.unity3d", out animators);

            //Console.WriteLine(@"C:\Program Files (x86)\HoneySelect\illusion\HoneySelect\abdata\studioneo\itemobject\bass_normal.unity3d");
            //this.GetAnimatorsNoDependency(@"C:\Program Files (x86)\HoneySelect\illusion\HoneySelect\abdata\studioneo\itemobject\bass_normal.unity3d", out animators);
            //Console.WriteLine(@"C:\Program Files (x86)\HoneySelect\illusion\HoneySelect\abdata\studioneo\itemobject\bass_uncompressed.unity3d");
            //this.GetAnimatorsNoDependency(@"C:\Program Files (x86)\HoneySelect\illusion\HoneySelect\abdata\studioneo\itemobject\bass_uncompressed.unity3d", out animators);
            //Console.WriteLine(@"C:\Program Files (x86)\HoneySelect\illusion\HoneySelect\abdata\studioneo\itemobject\bass_chunk.unity3d");
            //this.GetAnimatorsNoDependency(@"C:\Program Files (x86)\HoneySelect\illusion\HoneySelect\abdata\studioneo\itemobject\bass_chunk.unity3d", out animators);

            if (File.Exists(Path.Combine(Settings.Default.abdataPath, _infoPath, _listFileName)))
            {
                this.Button.IsEnabled = false;
                bool deep = this.DeepCheckbox.IsChecked ?? false;
                await Task.Factory.StartNew(() => this.DoTheActualThing(deep));
                this.Button.IsEnabled = true;
            }
            else
                this.LogText.Text = "Wrong abdata path, cannot find 00.unity3d...";
        }

        private void DoTheActualThing(bool deep)
        {
            List<ItemEntry> entries = new List<ItemEntry>();

            this.Dispatcher.Invoke(() => this.LogText.Text = "Extracting and parsing 00.unity3d...");

            this.ExtractListFile();

            StringBuilder builder = new StringBuilder();
            string extractedFilePath = Path.Combine(Settings.Default.abdataPath, _infoPath, _extractedListFileName);
            bool first = true;
            string firstLine ="";
            foreach (string line in File.ReadLines(extractedFilePath))
            {
                if (first)
                {
                    firstLine = line;
                    first = false;
                    continue;
                }
                if (ItemEntry.TryParse(line, out ItemEntry entry))
                    entries.Add(entry);
            }
            this.Dispatcher.Invoke(() => this.LogText.Text = "Checking individual entries...");

            Dictionary<string, HashSet<string>> animatorsByBundle = new Dictionary<string, HashSet<string>>();
            int i = 0;
            using (FileStream stream = File.Open(extractedFilePath, FileMode.Create, FileAccess.Write))
            {
                using (StreamWriter writer = new StreamWriter(stream))
                {
                    writer.WriteLine(firstLine);
                    foreach (ItemEntry entry in entries)
                    {
                        ++i;
                        int i1 = i;
                        this.ProgressBar.Dispatcher.Invoke(() =>
                        {
                            this.ProgressBar.Value = (i1 / (double)entries.Count) * 100;
                            this.LogText.Text = "Checking individual entries...\n" + entry.ItemName + " \"" + entry.FilePath + "\"";
                        });
                        string path = Path.Combine(Settings.Default.abdataPath, entry.FilePath).Replace("/", "\\");
                        if (entry.FilePath.Equals("studioneo/00.unity3d"))
                            writer.WriteLine(entry.ToString());
                        else if (File.Exists(path) == false)
                        {
                            builder.AppendLine(entry.ToString());
                            writer.WriteLine(ItemEntry.GenerateEmpty(entry.Id));
                        }
                        else if (deep)
                        {
                            HashSet<string> animators;
                            if (animatorsByBundle.TryGetValue(entry.FilePath.ToLower(), out animators) == false)
                            {
                                if (this.GetAnimatorsNoDependency(path, out animators) == false)
                                    animators = this.GetAnimatorsSB3U(path);
                                animatorsByBundle.Add(entry.FilePath.ToLower(), animators);
                                System.GC.Collect();
                            }
                            if (animators.Contains(entry.AnimatorName.ToLower()) == false)
                            {
                                builder.AppendLine(entry.ToString());
                                writer.WriteLine(ItemEntry.GenerateEmpty(entry.Id));
                            }
                            else
                                writer.WriteLine(entry.ToString());
                        }
                        else
                            writer.WriteLine(entry.ToString());
                    }
                }
            }
            this.Dispatcher.Invoke(() => this.LogText.Text = "Reimporting list file...");

            this.ReimportListFile();

            this.ProgressBar.Dispatcher.Invoke(() =>
            {
                this.ProgressBar.Value = 0;
                this.LogText.Text = "Done!\nHere's what was removed:\n" + builder;
            });
        }

        private void ExtractListFile()
        {
            File.WriteAllText("export.txt",
            $@"
            LoadPlugin(PluginDirectory+""UnityPlugin.dll"")
            unityParser0 = OpenUnity3d(path = ""{Path.Combine(Settings.Default.abdataPath, _infoPath, _listFileName)}"")
            unityEditor0 = Unity3dEditor(parser = unityParser0)
            unityEditor0.GetAssetNames(filter = True)
            unityEditor0.ExportMonoBehaviour(asset = unityParser0.Cabinet.Components[637], path = ""{Path.Combine(Settings.Default.abdataPath, _infoPath)}"")
            ");
            Process proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = ".\\SB3UGS_v1.0.54delta\\SB3UtilityScript.exe",
                    Arguments = $"\"{Directory.GetCurrentDirectory()}\\export.txt\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };
            proc.Start();
            proc.WaitForExit();
        }

        private void ReimportListFile()
        {
            File.WriteAllText("import.txt",
            $@"
            LoadPlugin(PluginDirectory+""UnityPlugin.dll"")
            unityParser0 = OpenUnity3d(path = ""{Path.Combine(Settings.Default.abdataPath, _infoPath, _listFileName)}"")
            unityEditor0 = Unity3dEditor(parser = unityParser0)
            unityEditor0.GetAssetNames(filter = True)
            unityEditor0.ReplaceMonoBehaviour(path = ""{Path.Combine(Settings.Default.abdataPath, _infoPath, _extractedListFileName)}"")
            unityEditor0.GetAssetNames(filter = True)
            unityEditor0.SaveUnity3d(keepBackup = False, backupExtension = "".unit - y3d"", background = False, pathIDsMode = -1)
            ");
            Process proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = ".\\SB3UGS_v1.0.54delta\\SB3UtilityScript.exe",
                    Arguments = $"\"{Directory.GetCurrentDirectory()}\\import.txt\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };
            proc.Start();
            proc.WaitForExit();
        }

        private HashSet<string> GetAnimatorsSB3U(string path)
        {
            File.WriteAllText("deep.txt",
            $@"
            LoadPlugin(PluginDirectory+""UnityPlugin.dll"")
            LoadPlugin(PluginDirectory+""SB3UDebug.dll"")
            unityParser0 = OpenUnity3d(path = ""{path}"")
            unityEditor0 = Unity3dEditor(parser = unityParser0)
            unityEditor0.GetAssetNames(filter = True)
            test = unityEditor0.GetAssetNames(filter=True)
            LogArray(test)
            ");
            Process proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = ".\\SB3UGS_v1.0.54delta\\SB3UtilityScript.exe",
                    Arguments = $"\"{Directory.GetCurrentDirectory()}\\deep.txt\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };
            proc.Start();
            HashSet<string> animators = new HashSet<string>();
            while (!proc.StandardOutput.EndOfStream)
            {
                string line = proc.StandardOutput.ReadLine().Replace("\n", "").ToLower();
                if (animators.Contains(line) == false)
                    animators.Add(line);
            }
            while (!proc.StandardError.EndOfStream)
            {
                Console.WriteLine(proc.StandardOutput.ReadLine().Replace("\n", ""));
            }
            proc.WaitForExit();
            return animators;
        }

        private void ChangePath(object sender, RoutedEventArgs e)
        {
            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                switch (dialog.ShowDialog())
                {
                    case System.Windows.Forms.DialogResult.OK:
                        if (File.Exists(Path.Combine(dialog.SelectedPath, _infoPath, _listFileName)))
                        {
                            Settings.Default.abdataPath = dialog.SelectedPath;
                            this.PathField.Text = dialog.SelectedPath;
                            Settings.Default.Save();
                        }
                        break;
                }
            }
        }

        private bool GetAnimatorsNoDependency(string bundleFileName, out HashSet<string> animators)
        {
            BundleFile b_File = new BundleFile(bundleFileName);
            List<AssetsFile> assetsfileList = new List<AssetsFile>();
            animators = new HashSet<string>();
            foreach (BundleFile.MemoryAssetsFile memFile in b_File.MemoryAssetsFileList)
            {
                memFile.fileName = Path.GetDirectoryName(bundleFileName) + "\\" + memFile.fileName;
                AssetsFile assetsFile = new AssetsFile(new EndianBinaryReader(memFile.memStream));
                if (assetsFile.valid)
                {
                    if (assetsFile.fileGen == 6 && Path.GetFileName(bundleFileName) != "mainData")
                    {
                        assetsFile.m_Version = b_File.versionEngine;
                        assetsFile.version = Array.ConvertAll((from Match m in Regex.Matches(assetsFile.m_Version, "[0-9]")
                                                                  select m.Value).ToArray(), int.Parse);
                        assetsFile.buildType = b_File.versionEngine.Split(AssetsFile.buildTypeSplit, StringSplitOptions.RemoveEmptyEntries);
                    }
                    assetsfileList.Add(assetsFile);
                }
                else
                    return false;
            }

            string fileIDfmt = "D" + assetsfileList.Count.ToString().Length;
            for (int i = 0; i < assetsfileList.Count; i++)
            {
                AssetsFile assetsFile = assetsfileList[i];
                string fileID = i.ToString(fileIDfmt);
                foreach (AssetPreloadData asset in assetsFile.preloadTable.Values)
                {
                    asset.uniqueID = fileID + asset.uniqueID;
                    switch (asset.Type2)
                    {
                        case 1:
                            GameObject m_GameObject = new GameObject(asset);
                            assetsFile.GameObjectList.Add(asset.m_PathID, m_GameObject);
                            break;
                        case 4:
                            Transform m_Transform = new Transform(asset);
                            assetsFile.TransformList.Add(asset.m_PathID, m_Transform);
                            break;
                    }
                }
            }
            foreach (AssetsFile assetsFile2 in assetsfileList)
            {
                GameObject fileNode = new GameObject(null);
                fileNode.m_Name = "RootNode";
                foreach (GameObject m_GameObject2 in assetsFile2.GameObjectList.Values)
                {
                    assetsFile2.ParseGameObject(m_GameObject2);
                    Transform m_Transform2;
                    Transform m_Father;
                    if (m_GameObject2.m_Transform == null || !assetsFile2.TransformList.TryGetValue(m_GameObject2.m_Transform.m_PathID, out m_Transform2) || !assetsFile2.TransformList.TryGetValue(m_Transform2.m_Father.m_PathID, out m_Father))
                    {
                        if (animators.Contains(m_GameObject2.m_Name.ToLower()) == false)
                            animators.Add(m_GameObject2.m_Name.ToLower());
                    }
                }
            }
            return true;
        }
    }
}
