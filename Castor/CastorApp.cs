using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Castor
{
    public class CastorApp
    {
        private bool _useZip { get; set; }

        internal void Run(string[] args)
        {
            _useZip = false;
            for (int i = 0; i < args.Length; i++)
            {
                var arg = args[i].ToLower();
                if (arg == "--zip")
                {
                    _useZip = true;
                }
            }

            if (args.Length > 0)
            {
                switch (args[0])
                {
                    case "build":
                    case "-b":
                        Build();
                        break;
                    case "init":
                        Init(args);
                        break;
                    case "-v":
                        Console.WriteLine($"Castor 2.0");
                        break;
                    case "install":
                    case "i":
                        Install(args);
                        break;
                    default:
                    case "help":
                    case "-h":
                        Help();
                        break;
                }
            }
            else
            {
                Help();
            }
        }

        private void Build()
        {
            if (!File.Exists("castor.json"))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("ERROR: could not build, castor.json file not found");
                Console.ForegroundColor = ConsoleColor.Gray;
                Environment.Exit(1);
                return;
            }

            string castorConfigText = File.ReadAllText("castor.json");
            CastorConfig castorConfig = JsonSerializer.Deserialize<CastorConfig>(castorConfigText);

            string archiveName = $"{castorConfig.ArchiveName}.zip";

            Console.WriteLine("Building Zoo Tycoon 2 mod...");
            File.Create(archiveName).Close();
            using (FileStream zipToOpen = new FileStream(archiveName, FileMode.Open))
            {
                using (ZipArchive archive = new ZipArchive(zipToOpen, ZipArchiveMode.Update))
                {
                    foreach (var folder in castorConfig.IncludeFolders)
                    {
                        if (!Directory.Exists(folder))
                        {
                            continue;
                        }

                        DirectoryInfo directory = new DirectoryInfo(folder);
                        Zipping(archive, castorConfig, directory);
                    }
                }
            }
            if (castorConfig.Z2f || !_useZip)
            {
                File.Delete($"{castorConfig.ArchiveName}.z2f");
                File.Move(archiveName, $"{castorConfig.ArchiveName}.z2f");
            }
                

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Build succeeded.");
            Console.ForegroundColor = ConsoleColor.Gray;
            Environment.Exit(0);
        }

        private void Zipping (ZipArchive archive, CastorConfig config, DirectoryInfo directoryInfo)
        {
            string[] directoryPath = Directory.GetCurrentDirectory().Split('\\');
            string rootDirectory = directoryPath[directoryPath.Length - 1];
            string formattedPath = directoryInfo.FullName.Split(rootDirectory)[1].Remove(0, 1);

            var files = directoryInfo.GetFiles();
            foreach (var file in files)
            {
                Console.WriteLine($"compressing {formattedPath}\\{file.Name}");
                archive.CreateEntryFromFile(file.FullName, $"{formattedPath}\\{file.Name}");
            }
            var directories = directoryInfo.GetDirectories();
            bool exclude;
            string subDirectoryPath;
            foreach (var directory in directories)
            {
                exclude = false;
                subDirectoryPath = directory.FullName.Split(rootDirectory)[1].Remove(0, 1);
                foreach (var excludedPath in config.ExcludeFolders)
                {
                    if (subDirectoryPath == excludedPath || subDirectoryPath == excludedPath.Replace('/', '\\'))
                        exclude = true;
                }

                if (!exclude)
                    Zipping(archive, config, directory);
            }
        }

        private void Help()
        {
            Console.WriteLine("build - build from castor.json file");
            Console.WriteLine("init - create new castor.json file");
            Console.WriteLine("help -  display help message");
            Console.WriteLine("install -  install packages");
            Console.WriteLine("-v -  display version");
            Environment.Exit(0);
        }

        private void Init(string[] args)
        {
            if (File.Exists("castor.json"))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("ERROR: castor.json already exists");
                Console.ForegroundColor = ConsoleColor.Gray;
                Environment.Exit(1);
            }

            string archiveName;

            if (args.Length > 1)
            {
                archiveName = args[1];
            }
            else
            {
                string[] directoryPath = Directory.GetCurrentDirectory().Split('\\');
                archiveName = directoryPath[directoryPath.Length - 1];
            }
            
            CastorConfig newConfig = new()
            {
                ArchiveName = archiveName,
                Z2f = true,
                IncludeFolders = new List<string> {
                    "ai",
                    "awards",
                    "biomes",
                    "config",
                    "effects",
                    "entities",
                    "lang",
                    "locations",
                    "maps",
                    "materials",
                    "photochall",
                    "puzzles",
                    "scenario",
                    "scripts",
                    "shared",
                    "tourdata",
                    "ui",
                    "world",
                    "xpinfo"
                },
                ExcludeFolders = new List<string> {
                    "scripts/modules"
                },
                DevDependencies = new List<string>
                {

                }
            };

            string newJson = JToken.Parse(JsonSerializer.Serialize(newConfig)).ToString();
            File.WriteAllText("castor.json", newJson);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("initialized a new castor project");
            Console.ForegroundColor = ConsoleColor.Gray;

            if (CheckGit())
            {
                Console.WriteLine("Do you want to install ArluqTools? (Y/n)");
                string useArluq = Console.ReadLine();
                if (useArluq.ToLower() != "n")
                    InstallModule("ZtModArchive/ArluqTools", true);
            }

            Console.WriteLine("check out castor.json and configure it as you need. Happy coding! :)");
            Environment.Exit(0);
        }

        private bool CheckGit ()
        {
            using (Process p = ConsoleCommand("git --version"))
            {
                while(!p.WaitForExit(1000))
                {

                }
                if (p.ExitCode == 0)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        private void InstallModule (string arg, bool addToConfig = false)
        {
            if (!CheckGit())
                Environment.Exit(1);

            Guid g = Guid.NewGuid();
            using (Process p = ConsoleCommand($"git clone https://github.com/{arg}.git {g}"))
            {
                string castorConfigText = File.ReadAllText("castor.json");
                CastorConfig castorConfig = JsonSerializer.Deserialize<CastorConfig>(castorConfigText);

                while (!p.WaitForExit(1000)){}
                if (p.ExitCode == 0)
                {
                    if (!Directory.Exists("scripts"))
                        Directory.CreateDirectory("scripts");
                    if (!Directory.Exists("scripts/modules"))
                        Directory.CreateDirectory("scripts/modules");

                    if (Directory.Exists($"{g}/scripts/modules"))
                        Directory.Delete($"{g}/scripts/modules");
                    CopyFilesRecursively($"{g}/scripts","scripts/modules");

                    var directory = new DirectoryInfo($"{g}") { Attributes = FileAttributes.Normal };
                    foreach (var info in directory.GetFileSystemInfos("*", SearchOption.AllDirectories))
                    {
                        info.Attributes = FileAttributes.Normal;
                    }

                    Directory.Delete($"{g}", true);

                    if (addToConfig)
                    {
                        castorConfig.DevDependencies.Add(arg);
                        string newJson = JToken.Parse(JsonSerializer.Serialize(castorConfig)).ToString();
                        File.WriteAllText("castor.json", newJson);
                    }
                    
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"installed package {arg}");
                    Console.ForegroundColor = ConsoleColor.Gray;
                }
                else
                {
                    Environment.Exit(1);
                }
            }
        }

        private void Install(string[] args)
        {
            if (args.Length > 1)
            {
                InstallModule(args[1], true);
            }
            else
            {
                string castorConfigText = File.ReadAllText("castor.json");
                CastorConfig castorConfig = JsonSerializer.Deserialize<CastorConfig>(castorConfigText);

                if (castorConfig.DevDependencies.Count == 0)
                    Environment.Exit(1);

                foreach (var package in castorConfig.DevDependencies)
                {
                    InstallModule(package);
                }
            }
        }

        private Process ConsoleCommand (string arg)
        {
            Process process = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.FileName = "cmd.exe";
            startInfo.Arguments = $"/C {arg}";
            process.StartInfo = startInfo;
            process.Start();
            return process;
        }

        private static void CopyFilesRecursively(string sourcePath, string targetPath)
        {
            //Now Create all of the directories
            foreach (string dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
            {
                Directory.CreateDirectory(dirPath.Replace(sourcePath, targetPath));
            }

            //Copy all the files & Replaces any files with the same name
            foreach (string newPath in Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories))
            {
                File.Copy(newPath, newPath.Replace(sourcePath, targetPath), true);
            }
        }
    }
}
