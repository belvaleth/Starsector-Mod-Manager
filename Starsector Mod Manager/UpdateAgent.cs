using System;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Text;

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
            int majorCheck = String.Compare(local.ModVersion.Major, remote.ModVersion.Major);

            if (majorCheck == -1)
            {
                return true;
            }
            else if (majorCheck == 0)
            {
                int minorCheck = String.Compare(local.ModVersion.Minor, remote.ModVersion.Minor);
                if (minorCheck == -1)
                {
                    return true;
                }
                else if (minorCheck == 0)
                {
                    int patchCheck = String.Compare(local.ModVersion.Patch, remote.ModVersion.Patch);
                    if (patchCheck == -1)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        public static string GetModThread(string threadID)
        {
            string baseURL = "https://fractalsoftworks.com/forum/index.php?topic=";
            StringBuilder urlBuilder = new StringBuilder("\"");
            urlBuilder.Append(String.Concat(baseURL, threadID));
            urlBuilder.Append("\"");
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
