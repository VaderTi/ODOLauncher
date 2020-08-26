using System;

namespace ODOLauncher.Models.Downloader
{
    public class DownloaderClientException : Exception
    {
        public DownloaderClientException(string message)
            : base(message)
        {
        }

        public DownloaderClientException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}