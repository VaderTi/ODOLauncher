using System;

namespace ODOLauncher.Models.Downloader
{
    public delegate void DownloadEventHandler(DownloaderMetric metric);

    public delegate void DownloadErrorEventHandler(Exception ex);
}