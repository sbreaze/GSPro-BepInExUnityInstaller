﻿using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;

namespace BepInExInstaller
{
    class Program
    {
        static void Main(string[] args)
        {
            string path = @"C:\Program Files (x86)\FSX Pro\";
            if (File.Exists(Path.Combine(Path.GetDirectoryName(path), "UnityPlayer.dll")))
            {
                Console.WriteLine("FSX Pro Directory Found.");
                InstallTo(Path.GetDirectoryName(path));
                Console.WriteLine("\nPress any key to exit...");
                Console.ReadKey();
                return;
            }
            Console.WriteLine("Game folder not found! Install here anyway? (Y to confirm)");
            var keyinfo = Console.ReadKey();
            if(keyinfo.Key == ConsoleKey.Y)
            {
                InstallTo(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
                Console.WriteLine("\nPress any key to exit...");
                Console.ReadKey();
            }
        }

        private static void InstallTo(string gamePath)
        {
            Console.WriteLine("Looking for BepInEx archive...");

            string path = @"C:\Program Files (x86)\FSX Pro";
            string path2 = (Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));

            bool x64 = true;
            foreach(string file in Directory.GetFiles(path, "*.exe"))
            {
                if (!file.StartsWith("BepInEx") && Directory.Exists(Path.Combine(path, Path.GetFileNameWithoutExtension(file)+"_Data")))
                {
                    Console.WriteLine($"Basing architecture on {file}: {(GetAppCompiledMachineType(file) == MachineType.x86 ? "32-bit" : "64-bit")}");
                    x64 = GetAppCompiledMachineType(file) != MachineType.x86;
                }
            }

            Console.WriteLine($"Game appears to be {(x64 ? "64-bit":"32-bit")}...");


            string zipPath = null;
            foreach (string file in Directory.GetFiles(path, "*.zip"))
            {
                if ((x64 && Path.GetFileName(file).StartsWith("BepInEx_x64")) || (!x64 && Path.GetFileName(file).StartsWith("BepInEx_x86")))
                {
                    zipPath = file;
                    break;
                }
            }

            if (zipPath == null)
            {
                Console.WriteLine("BepInEx zip file not found, downloading from web...");
                var client = new WebClient();
                string source = client.DownloadString("https://github.com/BepInEx/BepInEx/releases/");
                Match match = Regex.Match(source, $"a href=.(/BepInEx/BepInEx/releases/download/v[^/]+/BepInEx_{(x64 ? "x64" : "x86")}[^\"]+)\"");
                if (!match.Success)
                {
                    Console.WriteLine("Couldn't find latest BepInEx file, please visit https://github.com/BepInEx/BepInEx/releases/ to download the latest release.");
                }
                string latest = match.Groups[1].Value;
                Console.WriteLine($"Downloading https://github.com{latest}");
                string fileName = latest.Split('/')[latest.Split('/').Length - 1];
                client.DownloadFile("https://github.com" + latest, fileName);
                zipPath = Path.Combine(path2, fileName);
                Console.WriteLine($"Downloaded https://github.com{latest}");
            }

            if (!File.Exists(zipPath))
            {
                Console.WriteLine($"Zip file {zipPath} does not exist!");
                return;
            }

            Console.WriteLine("Installing BepInEx...");

            var archive = ZipFile.OpenRead(zipPath);
            foreach (var entry in archive.Entries)
            {
                string f = Path.Combine(gamePath, entry.FullName);
                if (!Directory.Exists(Path.GetDirectoryName(f)))
                    Directory.CreateDirectory(Path.GetDirectoryName(f));
                entry.ExtractToFile(Path.Combine(gamePath, entry.FullName), true);
                Console.WriteLine($"Copying {entry.FullName}");
            }
            archive.Dispose();

            if (!Directory.Exists(Path.Combine(gamePath, "BepInEx", "plugins")))
                Directory.CreateDirectory(Path.Combine(gamePath, "BepInEx","plugins"));

            Console.WriteLine($"BepInEx installed to {gamePath}!");
            File.Delete(zipPath);

            var archive2 = ZipFile.OpenRead("fsx_mod.zip");
            foreach (var entry in archive2.Entries)
            {
                string f = Path.Combine((Path.Combine(gamePath, "BepInEx", "plugins")), entry.FullName);
                if (!Directory.Exists(Path.GetDirectoryName(f)))
                    Directory.CreateDirectory(Path.GetDirectoryName(f));
                entry.ExtractToFile(Path.Combine(Path.Combine(gamePath, "BepInEx", "plugins"), entry.FullName), true);
                Console.WriteLine($"Copying {entry.FullName}");
                Console.WriteLine($"Plugins installed to {f}!");
            }
            archive2.Dispose();


        }

        public enum MachineType { Native = 0, x86 = 0x014c, Itanium = 0x0200, x64 = 0x8664 }

        public static MachineType GetAppCompiledMachineType(string fileName)
        {
            const int PE_POINTER_OFFSET = 60;
            const int MACHINE_OFFSET = 4;
            byte[] data = new byte[4096];
            using (Stream s = new FileStream(fileName, FileMode.Open, FileAccess.Read))
            {
                s.Read(data, 0, 4096);
            }
            // dos header is 64 bytes, last element, long (4 bytes) is the address of the PE header
            int PE_HEADER_ADDR = BitConverter.ToInt32(data, PE_POINTER_OFFSET);
            int machineUint = BitConverter.ToUInt16(data, PE_HEADER_ADDR + MACHINE_OFFSET);
            return (MachineType) machineUint;
        }
    }
}
