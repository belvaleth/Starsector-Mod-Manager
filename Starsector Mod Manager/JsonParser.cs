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
        // This function accepts a folder and returns an object representation of mod_info.json
        public static ModInfo GetModInfo(DirectoryInfo path)
        {
            if (!path.Exists)
            {
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

            jsonString = CleanJson(jsonString);
            ModInfo modInfo = JsonSerializer.Deserialize<ModInfo>(jsonString, jsonSerializerOptions);
            return modInfo;
        }

        public static ModVersionInfo GetVersionInfo(DirectoryInfo path)
        {
            if (!path.Exists)
            {
                throw new DirectoryNotFoundException();
            }
            List<FileInfo> versionFileListing = (path.EnumerateFiles("*.version", SearchOption.AllDirectories)).ToList();
            // separated these cases for future differentiation
            if (versionFileListing.Count != 1)
            {
                throw new VersionNotSupportedException(path.FullName, versionFileListing.Count);
            }
            JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions
            {
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true,
                PropertyNameCaseInsensitive = true,
                NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };

            string jsonString = File.ReadAllText(versionFileListing[0].FullName);

            jsonString = CleanJson(jsonString);
            ModVersionInfo versionInfo = JsonSerializer.Deserialize<ModVersionInfo>(jsonString, jsonSerializerOptions);
            return versionInfo;
        }

        public static ModVersionInfo ParseRemoteVersionInfo(string remoteJson)
        {
            JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions
            {
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true,
                PropertyNameCaseInsensitive = true,
                NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString
            };

            string jsonString = remoteJson;

            jsonString = CleanJson(jsonString);
            ModVersionInfo versionInfo = JsonSerializer.Deserialize<ModVersionInfo>(jsonString, jsonSerializerOptions);
            return versionInfo;
        }

        // Standardizes input as something as close to standard JSON as possible
        public static string CleanJson(string jsonInput)
        {
            bool invalidJsonReported = false;
            string jsonOutput = jsonInput;
            // remove comments starting with #
            Regex commentRegexEoL = new Regex("#.*\n");
            MatchCollection commentRegexEoLMatches = commentRegexEoL.Matches(jsonOutput);
            if (commentRegexEoLMatches.Count != 0)
            {
                if (Globals.VERBOSE_FLAG)
                {
                    WriteVerbose("Found invalid JSON, cleaning up...");
                    invalidJsonReported = true;
                }
                jsonOutput = commentRegexEoL.Replace(jsonOutput, System.Environment.NewLine);
            }

            // add quotes around values that don't have them. Treat everything as a string on deserialization in order to standardize input
            Regex missingQuotesRegex = new Regex(@":[^""{},\/[\]\s]+|:\s\d+");
            MatchCollection missingQuotesMatches = missingQuotesRegex.Matches(jsonOutput);
            if (missingQuotesMatches.Count != 0)
            {
                if (Globals.VERBOSE_FLAG && !invalidJsonReported)
                {
                    WriteVerbose("Found invalid JSON, cleaning up...");
                }
                foreach (Match match in missingQuotesMatches)
                {
                    string trimmedValue = match.Value.TrimStart(':');
                    trimmedValue = trimmedValue.Replace(System.Environment.NewLine, null);
                    trimmedValue = trimmedValue.Trim();
                    string newValue = String.Concat(":\"", trimmedValue, "\"");
                    Regex test = new Regex($"{match.Value}");
                    jsonOutput = test.Replace(jsonOutput, newValue, 1);
                }
            }
            return jsonOutput;
        }
        public static void WriteVerbose(string message)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"JSON Parser: {message}");
            Console.ResetColor();
        }
        public static void WriteError(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine($"JSON Parser: {message}");
            Console.ResetColor();
        }
    }
}
