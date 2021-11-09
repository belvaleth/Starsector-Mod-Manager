using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.CommandLine;
using System.CommandLine.Invocation;

namespace Starsector_Mod_Manager
{
    class ModMan
    {
        public static string modManVersion = "v0.0.1";
        static int Main(string[] args)
        {
            RootCommand rootCommand = new RootCommand();

            rootCommand.Description = ("Starsector Mod Manager " + modManVersion);

            Option pathOption = new Option<string>("--path", description: "The Starsector install directory");
            pathOption.AddAlias("-p");
            Option checkForUpdatesOption = new Option<bool>("--checkforupdates", description: "Check for mod updates, but do not download");
            checkForUpdatesOption.AddAlias("-c");
            Option updateOption = new Option<bool>("--update", description: "Update mods with available updates. Implies --checkforupdates");
            updateOption.AddAlias("-u");
            Option verboseOption = new Option<bool>("--verbose", "Enables verbose output");
            verboseOption.AddAlias("-v");

            rootCommand.AddOption(pathOption);
            rootCommand.AddOption(checkForUpdatesOption);
            rootCommand.AddOption(updateOption);
            rootCommand.AddOption(verboseOption);

            rootCommand.Handler = CommandHandler.Create<string, bool, bool, bool>(commandHandler);

            return rootCommand.Invoke(args);
        }

        static void commandHandler(string path, bool checkforupdates, bool update, bool verbose)
        {
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
            if (Directory.Exists(path) == false)
            {
                throw new DirectoryNotFoundException($"User defined path {path} does not exist.");
            }
            if (checkforupdates == true && update == true)
            {
                Console.WriteLine("\nNote: --update implies --checkforupdates.\n");
            }
            

            if (verbose)
            {
                Console.WriteLine($"Path: {path}");
                Console.WriteLine($"Check for updates: {checkforupdates}");
                Console.WriteLine($"Update: {update}");
            }

            List<ModDataRow> modDataTable = fillModDataTable(path, verbose);



           
        }

        public static List<ModDataRow> fillModDataTable(string path, bool verbose)
        {
            List<ModDataRow> modDataTable = new List<ModDataRow>();

            foreach (DirectoryInfo item in (GetModList(path, verbose)))
            {
                modDataTable.Add(new ModDataRow(item));
            }

            foreach (ModDataRow row in modDataTable)
            {
                row.ModInfo = JsonParser.GetModInfo(row.ModDirectory, verbose);
                row.Name = row.ModInfo.Name;
                try
                {
                    row.VersionInfo = JsonParser.GetVersionInfo(row.ModDirectory, verbose);
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
        public static List<DirectoryInfo> GetModList(string path, bool verbose)
        {
            if (Directory.Exists(path) == false)
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
    }
}