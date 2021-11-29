using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.CommandLine;
using System.CommandLine.Invocation;

namespace Starsector_Mod_Manager
{
    public static class Globals
    {
        public static bool VERBOSE_FLAG = false;
        public static bool UPDATE_FLAG = false;
    }
    class ModMan
    {
        public static string modManVersion = "v0.0.1";
        static int Main(string[] args)
        {
            RootCommand rootCommand = new RootCommand();

            rootCommand.Description = ("Starsector Mod Manager " + modManVersion);

            Option pathOption = new Option<string>("--path", description: "The Starsector install directory");
            pathOption.AddAlias("-p");
            Option updateOption = new Option<bool>("--update", description: "Update mods with available updates.");
            updateOption.AddAlias("-u");
            Option verboseOption = new Option<bool>("--verbose", "Enables verbose output");
            verboseOption.AddAlias("-v");

            rootCommand.AddOption(pathOption);
            rootCommand.AddOption(updateOption);
            rootCommand.AddOption(verboseOption);

            rootCommand.Handler = CommandHandler.Create<string, bool, bool>(CommandProcessor);

            return rootCommand.Invoke(args);
        }

        static void CommandProcessor(string path, bool update, bool verbose)
        {
            if (verbose)
            {
                Globals.VERBOSE_FLAG = true;
            }
            if (path == null)
            {
                throw new DirectoryNotFoundException($"No path provided.");
            }
            if (path.EndsWith(Path.DirectorySeparatorChar))
            {
                path = path.Remove(path.LastIndexOf(Path.DirectorySeparatorChar));
            }
            else if (path.EndsWith(Path.AltDirectorySeparatorChar))
            {
                path = path.Remove(path.LastIndexOf(Path.AltDirectorySeparatorChar));
            }
            if (!Directory.Exists(path))
            {
                throw new DirectoryNotFoundException($"User defined path {path} does not exist.");
            }
            WriteVerbose("Verbose output enabled.");

            WriteVerbose($"Path: {path}");
            WriteVerbose($"Update: {update}");

            List<ModDataRow> modDataTable = FillModDataTable(path);

            foreach (ModDataRow row in modDataTable)
            {
                ModVersionInfo remoteInfo;
                if (row.VersionInfo.ModVersion.Major.Equals("UNSUPPORTED"))
                {
                    Console.WriteLine($"{row.Name}: local: {row.ModInfo.Version} (No version file)");
                    continue;
                }
                try
                {
                    remoteInfo = (UpdateAgent.GetRemoteVersionInfo(row)).Result;
                }
                catch (MalformedVersionFileException e)
                {
                    if (e.IsNull)
                    {
                        WriteError($"{e.UnsupportedMod}: Remote version file is null");
                    }
                    else
                    {
                        WriteError($"{e.UnsupportedMod}: Remote version file is not .version file, skipping...");
                    }
                    continue;
                }
                catch (AggregateException e) when (e.InnerException is MalformedVersionFileException x)
                {
                    if (x.IsNull)
                    {
                        WriteError($"{row.Name}: remote master version file is null, please manually check for updates");
                    }
                    else
                    {
                        WriteError($"{row.Name}: malformed remote master version file, please manually check for updates");
                    }
                    continue;
                }
                catch (AggregateException e) when (e.InnerException is not MalformedVersionFileException x)
                {
                    WriteError($"{row.Name}: exception thrown, please manually check for updates");
                    if (Globals.VERBOSE_FLAG)
                    {
                        WriteError($"{e.GetType()}");
                    }
                    continue;

                }

                if (UpdateAgent.CompareModVersions(row.VersionInfo, remoteInfo))
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"UPDATE AVAILABLE: {row.Name} - local {row.VersionInfo.ModVersion.ToString()}, remote {remoteInfo.ModVersion.ToString()}");
                    Console.ResetColor();
                    if (update)
                    {
                        string modURL = UpdateAgent.GetModThread(row.VersionInfo.ModThreadID);
                        System.Diagnostics.Process.Start("explorer", modURL);
                    }
                }
                else
                {
                    Console.WriteLine($"{row.Name}: local {row.VersionInfo.ModVersion.ToString()}, remote {remoteInfo.ModVersion.ToString()}");
                }
            }            

        }

        public static List<ModDataRow> FillModDataTable(string path)
        {
            List<ModDataRow> modDataTable = new List<ModDataRow>();

            foreach (DirectoryInfo item in (GetModList(path)))
            {
                modDataTable.Add(new ModDataRow(item));
            }

            foreach (ModDataRow row in modDataTable)
            {
                row.ModInfo = JsonParser.GetModInfo(row.ModDirectory);
                row.Name = row.ModInfo.Name;
                try
                {
                    row.VersionInfo = JsonParser.GetVersionInfo(row.ModDirectory);
                }
                catch (VersionNotSupportedException e)
                {
                    if (e.VersionFilesFound == 0)
                    {
                        Console.WriteLine($"No version file found for mod: {row.Name}");
                        row.VersionInfo = new ModVersionInfo(row.Name);
                        row.VersionInfo.ModVersion = new ModVersion("UNSUPPORTED", "0", "0");
                    }
                    if (e.VersionFilesFound > 1)
                    {
                        Console.WriteLine($"Multiple version files found for mod: {row.Name}!");
                        row.VersionInfo = new ModVersionInfo(row.Name);
                        row.VersionInfo.ModVersion = new ModVersion("UNSUPPORTED", "0", e.VersionFilesFound.ToString());
                    }
                }
            }
            return modDataTable;
        }

        // This function accepts a path containing mod folders (typically Starsector installation directory\mods)
        // returns a list of subfolders (because we're not working with JSON yet)
        public static List<DirectoryInfo> GetModList(string path)
        {
            if (!Directory.Exists(path))
            {
                // Look at constructors for this exception
                throw new DirectoryNotFoundException($"Path {path} does not exist or is null.");
            }
            List<string> modDirectories = (Directory.EnumerateDirectories(path + Path.DirectorySeparatorChar + "mods")).ToList();

            List<DirectoryInfo> modDirectoryInfoList = new List<DirectoryInfo>();
            foreach (string item in modDirectories)
            {
                modDirectoryInfoList.Add(new DirectoryInfo(item));
            }
            return modDirectoryInfoList;
        }
        public static void WriteVerbose(string message)
        {
            if (Globals.VERBOSE_FLAG)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"Main: {message}");
                Console.ResetColor();
            }
        }
        public static void WriteError(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine($"Main: {message}");
            Console.ResetColor();
        }
    }
}