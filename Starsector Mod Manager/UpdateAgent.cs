using System;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Text;
using System.Linq;
using System.Collections.Generic;

namespace Starsector_Mod_Manager
{
    class UpdateAgent
    {
        private static readonly HttpClient UpdateClient = new HttpClient();

        public static async Task<ModVersionInfo> GetRemoteVersionInfo(ModDataRow modData)
        {
            if (modData.VersionInfo.MasterVersionFile == null)
            {
                throw new MalformedVersionFileException(modData.Name, true, $"Remote master version file for {modData.Name} is null!");
            }
            WriteVerbose($"Getting remote version info for {modData.Name}");
            Regex remoteRawVersionURL = new Regex(".*\\.version$");
            Regex dropboxDLCheck = new Regex("^https:\\/\\/www\\.dropbox\\.com.*dl=0$");

            string requestURL = modData.VersionInfo.MasterVersionFile;

            if (dropboxDLCheck.IsMatch(modData.VersionInfo.MasterVersionFile))
            {
                requestURL = requestURL.Replace("dl=0", "dl=1");
            }

            HttpResponseMessage remoteVersionResponse = new HttpResponseMessage();
            string responseContent;

            try
            {
                remoteVersionResponse = await UpdateClient.GetAsync(requestURL);
            }
            catch
            {
                WriteError($"Failed to retrieve remote master version file for mod {modData.Name}, located at {requestURL} (response {remoteVersionResponse.StatusCode})");
                throw;
            }

            if (remoteRawVersionURL.IsMatch(requestURL))
            {
                responseContent = remoteVersionResponse.Content.ReadAsStringAsync().Result;
            }
            else
            {
                byte[] contentByteArray = remoteVersionResponse.Content.ReadAsByteArrayAsync().Result;
                responseContent = System.Text.Encoding.ASCII.GetString(contentByteArray);
            }

            string remoteMasterVersionJson = JsonParser.CleanJson(responseContent);

            ModVersionInfo remoteMasterVersionInfo = JsonParser.ParseRemoteVersionInfo(remoteMasterVersionJson);
            return remoteMasterVersionInfo;
        }

        // get remote version file, compare to local. Return true if remote has higher version
        public static bool CompareModVersions(ModVersionInfo local, ModVersionInfo remote)
        {
            static bool CompareSubVersions(string a, string b)
            {
                // Case 1: both strings only contain letters
                Regex lettersOnly = new Regex("^[a-zA-Z]+$");
                Regex numbersOnly = new Regex("^[\\d]+$");
                if (lettersOnly.IsMatch(a) && lettersOnly.IsMatch(b))
                {
                    return (String.Compare(a, b) == -1) ? true : false;
                }
                // Case 2: both strings only contain numbers
                if (numbersOnly.IsMatch(a) && numbersOnly.IsMatch(b))
                {
                    return Int32.Parse(b) > Int32.Parse(a) ? true : false;
                }
                // Case 3: one or both strings contain a mix of numbers and letters
                
                List<string> splitA = new List<string>();
                
                string remainder = a;
                do
                {
                    try
                    {
                        splitA.Add(ParseVersionSubstring(remainder, out remainder));
                    }
                    catch
                    {
                        throw;
                    }
                } while (remainder != null);

                List<string> splitB = new List<string>();

                remainder = b;
                do
                {
                    try
                    {
                        splitB.Add(ParseVersionSubstring(remainder, out remainder));
                    }
                    catch
                    {
                        throw;
                    }
                } while (remainder != null);

                if (splitA.Count != splitB.Count)
                {
                    int higherCount = splitA.Count >= splitB.Count ? splitA.Count : splitB.Count;
                    int lowerCount = splitA.Count >= splitB.Count ? splitB.Count : splitA.Count;
                    for (int i = lowerCount-1; i < higherCount; i++)
                    {
                        try
                        {
                            splitA[i] = splitA[i] != null ? splitA[i] : String.Empty;
                        }
                        catch (ArgumentOutOfRangeException)
                        {
                            splitA.Add(String.Empty);
                        }
                        try
                        {
                            splitB[i] = splitB[i] != null ? splitB[i] : String.Empty;
                        }
                        catch (ArgumentOutOfRangeException)
                        {
                            splitB.Add(String.Empty);
                        }

                    }  
                }

                for(int i=0;i<splitA.Count;i++)
                {
                    if (numbersOnly.IsMatch(splitA[i]) && (numbersOnly.IsMatch(splitB[i])))
                    {
                        if (Int32.Parse(splitB[i]) > Int32.Parse(splitA[i]))
                        {
                            return true;
                        }
                    }
                    else if (lettersOnly.IsMatch(splitA[i]) && (lettersOnly.IsMatch(splitB[i])))
                    {
                        if (String.Compare(splitA[i],splitB[i]) == -1)
                        {
                            return true;
                        }
                    }

                    else if (splitA[i] == String.Empty || splitB[i] == String.Empty)
                    {
                        return splitB[i] == String.Empty ? false : true;
                    }

                    else
                    {
                        throw new InvalidOperationException($"Attempted to compare a string and an integer! {splitA[i]}, {splitB[i]}");
                    }
                }
                return false;
            }

            try
            {
                if (CompareSubVersions(local.ModVersion.Major, remote.ModVersion.Major))
                {
                    return true;
                }
                else if (CompareSubVersions(local.ModVersion.Minor, remote.ModVersion.Minor))
                {
                    return true;
                }
                else if (CompareSubVersions(local.ModVersion.Patch, remote.ModVersion.Patch))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (InvalidOperationException)
            {
                throw;
            }
        }

        public static string ParseVersionSubstring(string input, out string remainder)
        {
            if (Char.IsDigit(input[0]))
            {
                string split = new String(input.TakeWhile(Char.IsDigit).ToArray());
                if (Regex.IsMatch(input, "[a-zA-Z]+"))
                {
                    remainder = input.Substring(Regex.Match(input, "[a-zA-Z]+").Index);
                }
                else
                {
                    remainder = null;
                }
                return split;
            }
            else if (Char.IsLetter(input[0]))
            {
                string split = new String(input.TakeWhile(Char.IsLetter).ToArray());
                if (Regex.IsMatch(input, "\\d+"))
                {
                    remainder = input.Substring(Regex.Match(input, "\\d+").Index);
                }
                else
                {
                    remainder = null;
                }
                return split;
            }
            else
            {
                throw new InvalidOperationException("Attempted to parse version substring containing non-alphanumeric characters");
            }  
        }

        // Case 2: one or both strings contain non-alphanumeric characters
        public static string RemoveNonAlphanumeric(string input)
        {
            StringBuilder builder = new StringBuilder(input);
            Regex nonAlphanumberic = new Regex("[^0-9a-zA-Z]+");
            if (nonAlphanumberic.IsMatch(builder.ToString()))
            {
                foreach (Match match in nonAlphanumberic.Matches(builder.ToString()))
                {
                    builder.Replace(match.Value, "");
                }
            }
            return builder.ToString();
        }

        public static string GetModThread(string threadID)
        {
            string baseURL = "https://fractalsoftworks.com/forum/index.php?topic=";
            StringBuilder urlBuilder = new StringBuilder("\"");
            urlBuilder.Append(String.Concat(baseURL, threadID));
            urlBuilder.Append('"');
            string modURL = urlBuilder.ToString();
            return modURL;
        }

        public static void WriteVerbose(string message)
        {
            if (Globals.VERBOSE_FLAG)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"Update Agent: {message}");
                Console.ResetColor();
            }
        }

        public static void WriteError(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine($"Update Agent: {message}");
            Console.ResetColor();
        }
    }
}
