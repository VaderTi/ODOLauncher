using MahApps.Metro.Controls.Dialogs;
using ODOLauncher.Models;
using ODOLauncher.Models.Downloader;
using ODOLauncher.Models.Settings;
using ODOLauncher.ViewModels.Base;
using ODOLauncher.Views;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Input;

namespace ODOLauncher.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private static readonly LocalSettings LocalSettings = new LocalSettings();
        private static readonly RemoteSettings RemoteSettings = new RemoteSettings();
        private MainWindow _mainWindow;
        private Updater _updater;
        private const string RemoteUrl = "http://127.0.0.1/files/";
        private const string RemoteConfigName = "remote.config";

        private const string ExeFolder = "bin64";
        private const string GameExe = "BlackDesert64.exe";

        private readonly CancellationTokenSource _ctSource = new CancellationTokenSource();

        public void Initialize(MainWindow mv = null)
        {
            try
            {
                _mainWindow = mv;
                _updater = new Updater(this, OnDownloadStart, OnDownloadProgress, OnDownloadComplete, OnError)
                {
                    Token = _ctSource.Token
                };
                LoadLocalSettings();
                BeginPatch();
            }
            catch (Exception e)
            {
                _mainWindow.ShowMessageAsync("Error",
                    e.Message);
            }
        }

        private async void BeginPatch()
        {
            try
            {
                await _updater.GetRemoteConfig(RemoteUrl, RemoteConfigName, RemoteSettings);
                Thread.Sleep(500);
                await _updater.UpdateLauncher(RemoteSettings);
                await _updater.UpdateClient(RemoteSettings);
                Status = "Client up to date! Start Playing!";
            }
            catch (FileNotFoundException e)
            {
                await _mainWindow.ShowMessageAsync("Error",
                    $"{e.FileName} - {e.Message}");
            }
            catch (Exception e)
            {
                await _mainWindow.ShowMessageAsync("Error",
                    e.Message);
            }
        }

        public async void OnClosing(object sender, CancelEventArgs args)
        {
            SaveLocalSetting();
            if (!_updater.IsUpdating) return;
            args.Cancel = true;
            var dialogSettings = new MetroDialogSettings()
            {
                AffirmativeButtonText = "Close",
                NegativeButtonText = "Cancel",
                AnimateShow = true,
                AnimateHide = true
            };
            var result = await _mainWindow.ShowMessageAsync("Close Launcher?",
                "Launcher is working! Sure you want to close?",
                MessageDialogStyle.AffirmativeAndNegative, dialogSettings);

            if (result != MessageDialogResult.Affirmative) return;

            _ctSource.Cancel();
            Thread.Sleep(100);

            Application.Current.Shutdown();
        }

        private async void LaunchClient(object obj)
        {
            IsEnabledLaunch = false;
            IsEnabledLogin = false;
            IsEnabledPassword = false;
            IsEnabledSave = false;

            if (string.IsNullOrWhiteSpace(Login) || string.IsNullOrWhiteSpace(Password))
            {
                await _mainWindow.ShowMessageAsync("Authorization Credentials",
                    "Please enter Login and Password before launching client!");
                IsEnabledLaunch = true;
                return;
            }

            SaveLocalSetting();
            try
            {
                var psi = new ProcessStartInfo
                {
                    WorkingDirectory = ExeFolder,
                    FileName = GameExe,
                    Arguments = " " + Login + "," + Password,
                    CreateNoWindow = true
                };
                var p = Process.Start(psi);
                _mainWindow.Hide();
                p?.WaitForExit();
            }
            catch (Win32Exception e)
            {
                var message = e.NativeErrorCode switch
                {
                    2 => $"{ExeFolder}\\{GameExe}\n{e.Message}",
                    1223 => e.Message,
                    _ => $"({e.NativeErrorCode}):{e.Message}"
                };
                await _mainWindow.ShowMessageAsync("Error message!", message);
            }
            finally
            {
                IsEnabledLogin = true;
                IsEnabledPassword = true;
                IsEnabledSave = true;
                IsEnabledLaunch = true;

                _mainWindow.Show();
            }
        }

        private void LoadLocalSettings()
        {
            LocalConfig.Load(LocalSettings);
            _updater.LastPatchId = LocalSettings.LastPatchId;
            if (!LocalSettings.Save) return;
            Login = LocalSettings.Login.Trim();
            Password = Encoder.Decode(LocalSettings.Password);
            Save = LocalSettings.Save;
        }

        private void SaveLocalSetting()
        {
            LocalSettings.LastPatchId = _updater.LastPatchId;
            LocalSettings.Login = Login.Trim();
            LocalSettings.Password = Encoder.Encode(Password.Trim());
            LocalSettings.Save = Save;
            LocalConfig.Save(LocalSettings);
        }

        #region Properties

        private string _login;

        public string Login
        {
            get => _login;
            set
            {
                _login = value;
                RisePropertyChanged(nameof(Login));
            }
        }

        private bool _isEnabledLogin = true;

        public bool IsEnabledLogin
        {
            get => _isEnabledLogin;
            set
            {
                _isEnabledLogin = value;
                RisePropertyChanged(nameof(IsEnabledLogin));
            }
        }

        private string _password;

        public string Password
        {
            get => _password;
            set
            {
                _password = value;
                RisePropertyChanged(nameof(Password));
            }
        }

        private bool _isEnabledPassword = true;

        public bool IsEnabledPassword
        {
            get => _isEnabledPassword;
            set
            {
                _isEnabledPassword = value;
                RisePropertyChanged(nameof(IsEnabledPassword));
            }
        }

        private bool _save;

        public bool Save
        {
            get => _save;
            set
            {
                _save = value;
                RisePropertyChanged(nameof(Save));
            }
        }

        private bool _isEnabledSave = true;

        public bool IsEnabledSave
        {
            get => _isEnabledSave;
            set
            {
                _isEnabledSave = value;
                RisePropertyChanged(nameof(IsEnabledSave));
            }
        }

        private string _status;

        public string Status
        {
            get => _status;
            set
            {
                _status = value;
                RisePropertyChanged(nameof(Status));
            }
        }

        private double _cProgress = 100;

        public double CProgress
        {
            get => _cProgress;
            set
            {
                _cProgress = value;
                RisePropertyChanged(nameof(CProgress));
            }
        }

        private double _progress = 100;

        public double Progress
        {
            get => _progress;
            set
            {
                _progress = value;
                RisePropertyChanged(nameof(Progress));
            }
        }

        // ReSharper disable once UnusedMember.Global
        public string Version { get; } =
            "Version: " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;

        private bool _isEnabledLaunch;

        public bool IsEnabledLaunch
        {
            get => _isEnabledLaunch;
            set
            {
                _isEnabledLaunch = value;
                RisePropertyChanged(nameof(IsEnabledLaunch));
            }
        }

        #endregion

        #region Download Status

        private void OnDownloadStart(DownloaderMetric metric)
        {
            IsEnabledLaunch = false;
            Status = $"Downloading file {metric.FileName}";
        }

        private void OnDownloadProgress(DownloaderMetric metric)
        {
            Status = $"Downloading {metric.FileName} ({metric.SpeedInMb:0.00} MB/s)";
            CProgress = metric.Progress;
        }

        private void OnDownloadComplete(DownloaderMetric metric)
        {
            IsEnabledLaunch = true;
        }

        private void OnError(Exception ex)
        {
            _mainWindow.ShowMessageAsync("Error!", ex.Message + " " + ex.InnerException);
        }

        #endregion

        #region Commands

        private ICommand _cmdStartGame;

        // ReSharper disable once UnusedMember.Global
        public ICommand CmdStartGame => _cmdStartGame ??= new RelayCommand(LaunchClient);

        #endregion
    }
}