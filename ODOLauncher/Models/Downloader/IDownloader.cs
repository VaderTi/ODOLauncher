using System;
using System.Threading;
using System.Threading.Tasks;

namespace ODOLauncher.Models.Downloader
{
    public interface IDownloader
    {
        /// <summary>
        /// Event handler or function/method to call passing along a <see cref="DownloaderMetric"/>
        /// for when a download starts.
        /// </summary>
        event DownloadEventHandler OnDownloadStart;

        /// <summary>
        /// Event handler or function/method to call passing along a <see cref="DownloaderMetric"/>
        /// for every time progress occurs during a download operation.
        /// </summary>
        event DownloadEventHandler OnDownloading;

        /// <summary>
        /// Event handler to call after a download operation successfully completes!
        /// </summary>
        event DownloadEventHandler OnDownloadCompleted;

        /// <summary>
        /// Event handler to call when an error occurs while processing a download
        /// operation. This event passes along an <see cref="DownloaderClientException"/>
        /// </summary>
        event DownloadErrorEventHandler OnError;

        /// <summary>
        /// Asynchronously downloads a file from the server and saves it to the application folder.
        /// </summary>
        /// <param name="uri">The path to the downloaded resource.</param>
        /// <param name="filename">The name of the saved file.</param>
        /// <param name="folderPath">Saved file folder.</param>
        /// <param name="token"><see cref="CancellationToken"/></param>
        Task DownloadToFileAsync(Uri uri, string folderPath = null, string filename = null,
            CancellationToken token = default);
    }
}