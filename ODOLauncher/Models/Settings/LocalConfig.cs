using System.IO;
using SharpConfig;

namespace ODOLauncher.Models.Settings
{
    public class LocalSettings
    {
        public int LastPatchId;
        public string Login;
        public string Password;

        private bool _save;

        public bool Save
        {
            get => _save;
            set
            {
                _save = value;
                if (_save) return;
                Login = string.Empty;
                Password = string.Empty;
            }
        }
    }

    internal static class LocalConfig
    {
        private const string DefaultConfigFile = "odol.config";

        public static void Load(LocalSettings localSettings, string configFile = null)
        {
            if (string.IsNullOrWhiteSpace(configFile))
            {
                configFile = DefaultConfigFile;
            }

            if (!File.Exists(configFile))
                using (File.Create(configFile))
                {
                }

            var config = Configuration.LoadFromFile(configFile);
            localSettings.LastPatchId = config["ODOL"][nameof(LocalSettings.LastPatchId)].IntValue;
            localSettings.Login = config["ODOL"][nameof(LocalSettings.Login)].StringValue;
            localSettings.Password = config["ODOL"][nameof(LocalSettings.Password)].StringValue;
            localSettings.Save = config["ODOL"][nameof(LocalSettings.Save)].BoolValue;
        }

        public static void Save(LocalSettings localSettings, string configFile = null)
        {
            if (string.IsNullOrWhiteSpace(configFile))
            {
                configFile = DefaultConfigFile;
            }

            var config = new Configuration();
            config["ODOL"][nameof(LocalSettings.LastPatchId)].IntValue = localSettings.LastPatchId;
            config["ODOL"][nameof(LocalSettings.Login)].StringValue = localSettings.Login;
            config["ODOL"][nameof(LocalSettings.Password)].StringValue = localSettings.Password;
            config["ODOL"][nameof(LocalSettings.Save)].BoolValue = localSettings.Save;
            config.SaveToFile(configFile);
        }
    }
}