using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using WinterspringLauncher.Utils;

namespace WinterspringLauncher;

public enum OperatingSystem
{
    Windows,
    MacOs
}

public class VersionedBaseConfig
{
    public int ConfigVersion { get; set; } = 3;
}

public class LauncherConfig : VersionedBaseConfig
{
    private string _internalLastLoadedJsonString = string.Empty;

    public string LauncherLanguage { get; set; } = "en";
    public string? GitHubApiMirror { get; set; } = null;
    public string LastSelectedServerName { get; set; } = "";
    public bool CheckForLauncherUpdates { get; set; } = true;
    public bool CheckForHermesUpdates { get; set; } = true;
    public bool CheckForClientPatchUpdates { get; set; } = true;
    public bool CheckForClientBuildInfoUpdates { get; set; } = true;

    public ServerInfo[] KnownServers { get; set; } = new ServerInfo[]
    {
        new ServerInfo
        {
            Name = "SanctuaryWoW",
            RealmlistAddress = "logon.sanctuarywow.com",
            UsedInstallation = "Sanctuary 1.14.2 installation"
        },
    };

    public Dictionary<string, InstallationLocation> GameInstallations { get; set; } = new Dictionary<string, InstallationLocation>
    {
        ["Sanctuary 1.14.2 installation"] = new InstallationLocation
        {
            Directory = "./winterspring-data/WoW 1.14.2",
            Version = "1.14.2.42597",
            ClientPatchInfoURL = "https://wow-patches.blu.wtf/patches/1.14.2.42597_summary.json",
            BaseClientDownloadURL = new Dictionary<OperatingSystem, string>() {
                [OperatingSystem.Windows] = "https://ipfs.io/ipns/k51qzi5uqu5di6ej6e3vuit7yvtrf8buq8q7gsfhryvzolj9ckjpiuzw68bd5g",
                [OperatingSystem.MacOs] = "", // Temporary invalid link. Mac not supported.
            },
        }
    };

    public string HermesProxyLocation { get; set; } = "./winterspring-data/HermesProxy";

    public class ServerInfo
    {
        public string Name { get; set; }
        public string RealmlistAddress { get; set; }
        public string UsedInstallation { get; set; }
        //public bool? RequiresHermes { get; set; }
        public Dictionary<string, string>? HermesSettings { get; set; }
    }

    public class InstallationLocation
    {
        public string Version { get; set; }
        public string Directory { get; set; }
        public string ClientPatchInfoURL { get; set; }
        public string? CustomBuildInfoURL { get; set; } // Optional
        public Dictionary<OperatingSystem, string> BaseClientDownloadURL { get; set; }
    }

    public static LauncherConfig GetDefaultConfig() => new LauncherConfig();

    public void SaveConfig(string configPath)
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        string jsonString = JsonSerializer.Serialize(this, options);
        if (jsonString != _internalLastLoadedJsonString)
        {
            File.WriteAllText(configPath, jsonString, Encoding.UTF8);
        }
    }

    public static LauncherConfig LoadOrCreateDefault(string configPath)
    {
        LauncherConfig config;
        if (!File.Exists(configPath))
        {
            config = GetDefaultConfig();
        }
        else
        {
            string configTextContent = File.ReadAllText(configPath, Encoding.UTF8);
            string updatedConfig = PatchConfigIfNeeded(configTextContent);
            var loadedJson = JsonSerializer.Deserialize<LauncherConfig>(updatedConfig);
            if (loadedJson != null)
            {
                config = loadedJson;
                config._internalLastLoadedJsonString = configTextContent;
            }
            else
            {
                Console.WriteLine("Config is null after loading? Replacing it with default one");
                config = GetDefaultConfig();
            }
        }

        config.SaveConfig(configPath);

        return config;
    }

    private static string PatchConfigIfNeeded(string currentConfig)
    {
        var configVersion = JsonSerializer.Deserialize<VersionedBaseConfig>(currentConfig);
        if (configVersion == null)
        {
            Console.WriteLine("Unable to determine config version");
            return currentConfig;
        }

        if (configVersion.ConfigVersion >= 3)
            return currentConfig; // already on latest version

        if (configVersion.ConfigVersion == 1)
        {
            var v1Config = JsonSerializer.Deserialize<LegacyV1Config>(currentConfig);
            if (v1Config == null)
                return currentConfig; // Error ?

            var newConfig = new LauncherConfig();

            return JsonSerializer.Serialize(newConfig);
        }

        if (configVersion.ConfigVersion == 2)
        {
            var newConfig = JsonSerializer.Deserialize<LauncherConfig>(currentConfig);

            newConfig.ConfigVersion = 3;

            return JsonSerializer.Serialize(newConfig);
        }

        Console.WriteLine("Unknown version");
        return currentConfig;
    }

    private static void TryUpgradeOldGameFolder(string oldGameFolder, string newGameFolder)
    {
        try
        {
            bool weAreOnMacOs = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
            if (!weAreOnMacOs)
            {
                string known_1_14_2_client_hash = "43F407C7915602D195812620D68C3E5AE10F20740549D2D63A0B04658C02A123";

                var gameExecutablePath = Path.Combine(oldGameFolder, "_classic_era_", "WoWClassic.exe");

                if (File.Exists(gameExecutablePath) && HashHelper.CreateHexSha256HashFromFilename(gameExecutablePath) == known_1_14_2_client_hash)
                {
                    // We can just move the whole folder
                    Directory.Move(oldGameFolder, newGameFolder); // <-- might fail if target is not empty
                }
                else
                {
                    // Just copy the WTF and Interface folder

                    var oldInterfaceFolder = Path.Combine(oldGameFolder, "_classic_era_", "Interface");
                    var newInterfaceFolder = Path.Combine(newGameFolder, "_classic_era_", "Interface");
                    DirectoryCopy.Copy(oldInterfaceFolder, newInterfaceFolder);

                    var oldWtfFolder = Path.Combine(oldGameFolder, "_classic_era_", "WTF");
                    var newWtfFolder = Path.Combine(newGameFolder, "_classic_era_", "WTF");
                    DirectoryCopy.Copy(oldWtfFolder, newWtfFolder);
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("Error while TryUpgradeOldGameFolder");
            Console.WriteLine(e);
        }
    }

    private class LegacyV1Config : VersionedBaseConfig
    {
        public string GitRepoWinterspringLauncher { get; set; }
        public string GitRepoHermesProxy { get; set; }
        public string GitRepoArctiumLauncher { get; set; }

        public string WindowsGameDownloadUrl { get; set; }
        public string MacGameDownloadUrl { get; set; }
        public string GamePatcherUrl { get; set; }

        public string HermesProxyPath { get; set; }
        public string GamePath { get; set; }
        public string ArctiumLauncherPath { get; set; }
        public bool RecreateDesktopShortcut { get; set; }
        public bool AutoUpdateThisLauncher { get; set; }

        public string Realmlist { get; set; }
    }
}

