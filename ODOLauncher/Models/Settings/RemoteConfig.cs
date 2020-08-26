using System;
using System.IO;
using SharpConfig;

namespace ODOLauncher.Models.Settings
{
    public class RemoteSettings
    {
        public Version LauncherVersion;
        public string LauncherUrl;
        public string PatchListUrl;
        public string PatchesUrl;
    }

    internal static class RemoteConfig
    {
        public static void LoadConfig(RemoteSettings remoteSettings, string configFile)
        {
            var settings = Configuration.LoadFromFile(configFile);
            remoteSettings.LauncherVersion =
                Version.Parse(settings["Settings"][nameof(RemoteSettings.LauncherVersion)].StringValue);
            remoteSettings.LauncherUrl = settings["Settings"][nameof(RemoteSettings.LauncherUrl)].StringValue;
            remoteSettings.PatchListUrl = settings["Settings"][nameof(RemoteSettings.PatchListUrl)].StringValue;
            remoteSettings.PatchesUrl = settings["Settings"][nameof(RemoteSettings.PatchesUrl)].StringValue;
            File.Delete(configFile);
        }
    }
}