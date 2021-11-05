using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Linq;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Text.RegularExpressions;
using System.Text.Json.Serialization;
using System.Net.Http;

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
            if (path.EndsWith(Path.DirectorySeparatorChar))
            {
                path = path.Remove(path.LastIndexOf(Path.DirectorySeparatorChar));
            }
            else if (path.EndsWith(Path.AltDirectorySeparatorChar))
            {
                path = path.Remove(path.LastIndexOf(Path.AltDirectorySeparatorChar));
            }
            if (checkforupdates == true && update == true)
            {
                Console.WriteLine("\nNote: --update implies --checkforupdates.\n");
            }
            if (path == null || Directory.Exists(path) == false)
            {
                throw new DirectoryNotFoundException($"User defined path {path} does not exist or is null.");
            }

            if (verbose)
            {
                Console.WriteLine($"Path: {path}");
                Console.WriteLine($"Check for updates: {checkforupdates}");
                Console.WriteLine($"Update: {update}");
            }

            List<ModDataRow> modDataTable = fillModDataTable(path, verbose);



           
        }

        




        // get remote version file, compare to local. Return true if remote has higher version
        bool checkForUpdate(ModDataRow modData)
        {

        }

        static List<ModDataRow> fillModDataTable(string path, bool verbose)
        {
            List<ModDataRow> modDataTable = new List<ModDataRow>();

            foreach (DirectoryInfo item in (GetModList(path, verbose)))
            {
                modDataTable.Add(new ModDataRow(item));
            }

            foreach (ModDataRow row in modDataTable)
            {
                row.ModInfo = GetModInfo(row.ModDirectory, verbose);
                row.Name = row.ModInfo.Name;
                try
                {
                    row.VersionInfo = GetVersionInfo(row.ModDirectory, verbose);
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

        // This function accepts a folder name and returns an object representation of mod_info.json
        public static ModInfo GetModInfo(DirectoryInfo path, bool verbose)
        {
            if (path.Exists == false)
            {
                // Look at constructors for this exception
                throw new DirectoryNotFoundException();
            }

            JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions
            {
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true,
                PropertyNameCaseInsensitive = true,
                NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString
            };

            string jsonString = File.ReadAllText(Path.Combine(path.FullName, "mod_info.json"));
            if (verbose)
            {
                Console.WriteLine("Calling CleanJson on mod_info.json...");
            }
            jsonString = CleanJson(jsonString, verbose);
            ModInfo modInfo = JsonSerializer.Deserialize<ModInfo>(jsonString, jsonSerializerOptions);
            return modInfo;
        }
        public static ModVersionInfo GetVersionInfo(DirectoryInfo path, bool verbose)
        {
            if (path.Exists == false)
            {
                // Look at constructors for this exception
                throw new System.IO.DirectoryNotFoundException();
            }
            List<FileInfo> versionFileListing = (path.EnumerateFiles("*.version", SearchOption.AllDirectories)).ToList();
            // separated these cases for future differentiation
            if (versionFileListing.Count() != 1)
            {
                throw new VersionNotSupportedException(path.FullName, versionFileListing.Count());
            }
            JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions
            {
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true,
                PropertyNameCaseInsensitive = true,
                NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString
            };

            string jsonString = File.ReadAllText(versionFileListing[0].FullName);
            if (verbose)
            {
                Console.WriteLine("Calling CleanJson on .version file...");
            }
            jsonString = CleanJson(jsonString, verbose);
            ModVersionInfo versionInfo = JsonSerializer.Deserialize<ModVersionInfo>(jsonString, jsonSerializerOptions);
            return versionInfo;
        }

        // Because no one seems to be able to write standardized JSON.
        public static string CleanJson(string jsonInput, bool verbose)
        {
            string jsonOutput = jsonInput;
            // remove inline comments starting with #
            Regex commentRegexEoL = new Regex("#.*\n");
            MatchCollection commentRegexEoLMatches = commentRegexEoL.Matches(jsonOutput);
            if (commentRegexEoLMatches.Count != 0)
            {
                if (verbose)
                {
                    Console.WriteLine("Found end of line comment, removing...");
                }
                jsonOutput = commentRegexEoL.Replace(jsonOutput, System.Environment.NewLine);
            }

            // add quotes around values don't have them. Treat everything as a string on deserialization in order to standardize input
            Regex missingQuotesRegex = new Regex(@":[^""{},\/[\]\s]+|:\s\d+");
            MatchCollection missingQuotesMatches = missingQuotesRegex.Matches(jsonOutput);
            if (missingQuotesMatches.Count != 0)
            {
                if (verbose)
                {
                    Console.WriteLine($"{missingQuotesMatches.Count} Quotes missing!");
                }
                foreach (Match match in missingQuotesMatches)
                {
                    string trimmedValue = match.Value.TrimStart(':');
                    trimmedValue = trimmedValue.Replace(System.Environment.NewLine, null);
                    trimmedValue = trimmedValue.Trim();
                    string newValue = String.Concat(":\"", trimmedValue, "\"");
                    if (verbose)
                    {
                        Console.WriteLine($"Replacing {match.Value} with {newValue}");
                    }
                    Regex test = new Regex($"{match.Value}");
                    jsonOutput = test.Replace(jsonOutput, newValue, 1);
                }
            }
            return jsonOutput;
        }
    }

    




}
    



 // function: get remote mod version file
 // function: compare remote and local versions
 // function: download remote version
 // function: unpack archive
