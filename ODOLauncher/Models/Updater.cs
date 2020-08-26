using ODOLauncher.Models.Downloader;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using ODOLauncher.Models.Settings;
using ODOLauncher.ViewModels;

namespace ODOLauncher.Models
{
    internal class Patch
    {
        public int PatchId { get; set; }
        public string PatchName { get; set; }
        public string PatchFolder { get; set; }
    }
    public class Updater
    {
        private static MainViewModel _mainView;
        private static IDownloader _dlClient;
        public int LastPatchId { get; set; }
        public bool IsUpdating { get; private set; }

        public Updater(MainViewModel mainView,DownloadEventHandler onDownloadStart, DownloadEventHandler onDownloading,
            DownloadEventHandler onDownloadCompleted, DownloadErrorEventHandler onError)
        {
            _mainView = mainView;
            _dlClient = new DownloaderClient();
            _dlClient.OnDownloadStart += onDownloadStart;
            _dlClient.OnDownloading += onDownloading;
            _dlClient.OnDownloadCompleted += onDownloadCompleted;
            _dlClient.OnError += onError;
        }

        public async Task GetRemoteConfig(string configUrl, string configFile, RemoteSettings remoteConfig)
        {
            IsUpdating = true;
            await _dlClient.DownloadToFileAsync(new Uri(configUrl + configFile));
            RemoteConfig.LoadConfig(remoteConfig, configFile);
            IsUpdating = false;
        }

        public async Task UpdateLauncher(RemoteSettings settings)
        {
            IsUpdating = true;

            var launcherName = Assembly.GetExecutingAssembly().GetName().Name + ".exe";
            var launcherVersion = Assembly.GetExecutingAssembly().GetName().Version;
            File.Delete(launcherName + ".old");

            if (launcherVersion.CompareTo(settings.LauncherVersion) == -1)
            {
                File.Move(launcherName, launcherName + ".old");
                await _dlClient.DownloadToFileAsync(new Uri(settings.LauncherUrl));
                var psi = new ProcessStartInfo
                {
                    FileName = launcherName,
                    CreateNoWindow = true,
                };
                Process.Start(psi);
                Process.GetCurrentProcess().Kill();
            }

            IsUpdating = false;
        }

        public async Task UpdateClient(RemoteSettings settings)
        {
            IsUpdating = true;

            await _dlClient.DownloadToFileAsync(new Uri(settings.PatchListUrl));

            string line;
            var streamReader = new StreamReader(Path.GetFileName(settings.PatchListUrl));
            var patches = new List<Patch>();
            while (!string.IsNullOrWhiteSpace(line = await streamReader.ReadLineAsync()))
            {
                if ((line[0] == '/') && (line[1] == '/')) continue;
                var patch = line.Split(':');
                if (int.Parse(patch[0]) <= LastPatchId) continue;
                switch (patch.Length)
                {
                    case 2:
                    {
                        patches.Add(new Patch(){PatchId = int.Parse(patch[0]), PatchName = patch[1]});
                        break;
                    }
                    case 3:
                    {
                        patches.Add(new Patch(){PatchId = int.Parse(patch[0]), PatchName = patch[1], PatchFolder = patch[2]});
                        break;
                    }
                    default:
                    {
                        IsUpdating = false;
                        throw new Exception($"Invalid patch line: {line}");
                    }
                }
            }

            streamReader.Close();
            File.Delete(Path.GetFileName(settings.PatchListUrl!));

            if (patches.Count > 0)
            {
                for (var i = 0; i < patches.Count; i++)
                {
                    double progress = (double)(i+1) / patches.Count * 100;
                    _mainView.Progress = progress;
                    string folder = null;
                    if (!string.IsNullOrWhiteSpace(patches[i].PatchFolder))
                        folder = patches[i].PatchFolder;

                    await _dlClient.DownloadToFileAsync(new Uri(settings.PatchesUrl + patches[i].PatchName), folder);
                    LastPatchId = patches[i].PatchId;
                }
            }

            IsUpdating = false;
        }
    }
}