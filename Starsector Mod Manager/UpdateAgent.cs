using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text.RegularExpressions;

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
            if (Globals.VERBOSE_FLAG)
            {
                WriteVerbose($"Getting remote version info for {modData.Name}");
            }
            Regex remoteURL = new Regex(".*\\.version$");


            //TODO: implement downloading of non-raw remote version files

            if (!remoteURL.IsMatch(modData.VersionInfo.MasterVersionFile))
            {
                var response = UpdateClient.GetAsync(modData.VersionInfo.MasterVersionFile).Result;
                while (response.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    response = UpdateClient.GetAsync(response.Headers.Location).Result;
                }

                throw new MalformedVersionFileException(modData.Name, false);
            }
            HttpResponseMessage remoteVersionResponse = await UpdateClient.GetAsync(modData.VersionInfo.MasterVersionFile);
            if (!remoteVersionResponse.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Failed to retrieve remote master version file for mod {modData.Name}, located at {modData.VersionInfo.MasterVersionFile} (response {remoteVersionResponse.StatusCode})");
            }
            string responseContent = remoteVersionResponse.Content.ReadAsStringAsync().Result;
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

        public static void WriteVerbose(string message)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"Update Agent: {message}");
            Console.ResetColor();
        }
        public static void WriteError(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine($"Update Agent: {message}");
            Console.ResetColor();
        }
        // function: download remote version
        // function: unpack archive

    }
}
