using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
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
                        Console.WriteLine($"Castor 1.0");
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
            Console.WriteLine("-v -  display version");
            Environment.Exit(0);
        }

        private void Init(string[] args)
        {
            if (File.Exists("castor.json"))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("ERROR: castor.json already exists");
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
                IncludeFolders = new string[] {
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
                ExcludeFolders = new string[] {
                    "scripts/ArluqTools"
                }
            };

            string newJson = JToken.Parse(JsonSerializer.Serialize(newConfig)).ToString();
            File.WriteAllText("castor.json", newJson);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("initialized a new castor project");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine("check out castor.json and configure it as you need. Happy coding! :)");
            Environment.Exit(0);
        }
    }
}
