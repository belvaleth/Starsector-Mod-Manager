using System;
using System.Collections.Generic;
using System.IO;

namespace Starsector_Mod_Manager
{
    public class StarsectorInfo
    {
        public string InstallFolder { get; set; }
        public string ModFolder { get; set; }
        public string Version { get; set; } // not sure this is possible - version number is not in FileInfo attributes

        public StarsectorInfo(string installFolder)
        {
            this.InstallFolder = installFolder;
            this.ModFolder = installFolder + Path.DirectorySeparatorChar + "mods";
        }
    }

    class ModDataRow
    {
        public string Name { get; set; }
        public DirectoryInfo ModDirectory { get; }
        public ModInfo ModInfo { get; set; }
        public ModVersionInfo VersionInfo { get; set; }
        public ModDataRow(DirectoryInfo modDirectory)
        {
            this.ModDirectory = modDirectory;
        }
    }

    public class ModInfo
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Author { get; set; }
        public string Version { get; set; }
        public string Description { get; set; }
        public string GameVersion { get; set; }
        public List<ModDependency> Dependencies { get; set; }
        public FileInfo VersionFile { get; set; }
    }

    public class ModDependency
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Version { get; set; }
    }

    public class ModVersionInfo
    {
        public string MasterVersionFile { get; set; }
        public string ModName { get; set; }
        public string ModThreadID { get; set; }
        public string ModNexusID { get; set; }
        public ModVersion ModVersion { get; set; }
        public string StarsectorVersion { get; set; }
        public ModVersionInfo(string modName)
        {
            this.ModName = modName;
        }
    }

    public class ModVersion
    {
        public string Major { get; set; }
        public string Minor { get; set; }
        public string Patch { get; set; }
        public ModVersion(string major, string minor, string patch)
        {
            this.Major = major;
            this.Minor = minor;
            this.Patch = patch;
        }
        public override string ToString()
        {
            string versionString = String.Concat(this.Major, ".", this.Minor, ".", this.Patch);
            return versionString;
        }
    }

    public class VersionNotSupportedException : System.Exception
    {
        public string UnsupportedMod { get; }
        public int VersionFilesFound { get; }
        public VersionNotSupportedException(string mod, int versionFilesFound)
        {
            this.UnsupportedMod = mod;
            this.VersionFilesFound = versionFilesFound;
        }
        public VersionNotSupportedException(string mod, int versionFilesFound, string message, Exception innerException) : base(message,innerException)
        {
            this.UnsupportedMod = mod;
            this.VersionFilesFound = versionFilesFound;
        }
    }
    public class MalformedVersionFileException : System.Exception
    {
        public string UnsupportedMod { get; }
        public bool IsNull { get; }
        
        public MalformedVersionFileException(string mod, bool isNull)
        {
            this.UnsupportedMod = mod;
            this.IsNull = isNull;
        }
        public MalformedVersionFileException(string mod, bool isNull, string message) : base(message)
        {
            this.UnsupportedMod = mod;
            this.IsNull = isNull;
        }
        public MalformedVersionFileException(string mod, bool isNull, string message, Exception innerException) : base(message,innerException)
        {
            this.UnsupportedMod = mod;
            this.IsNull = isNull;
        }
    }
}
