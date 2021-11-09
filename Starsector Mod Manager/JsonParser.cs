using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text.Json;
using System.IO;

namespace Starsector_Mod_Manager
{
    class JsonParser
    {



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
            // remove comments starting with #
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
