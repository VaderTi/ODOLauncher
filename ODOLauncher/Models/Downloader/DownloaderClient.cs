using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;


namespace ODOLauncher.Models.Downloader
{
    /// <summary>
    /// Instantiates download client;
    /// </summary>
    public class DownloaderClient : IDownloader
    {
        public event DownloadEventHandler OnDownloadStart;
        public event DownloadEventHandler OnDownloading;
        public event DownloadEventHandler OnDownloadCompleted;
        public event DownloadErrorEventHandler OnError;

        public async Task DownloadToFileAsync(Uri uri, string folderPath, string filename,
            CancellationToken ct = default)
        {
            filename ??= Path.GetFileName(uri.ToString());
            folderPath ??= Directory.GetCurrentDirectory();
            await ProcessDownloadAsync(uri, Path.Combine(folderPath, filename), ct);
        }

        private async Task ProcessDownloadAsync(Uri uri, string filename, CancellationToken ct)
        {
            try
            {
                dynamic request = null;

                #region Get file size

                if (uri.Scheme == Uri.UriSchemeHttp)
                {
                    request = WebRequest.Create(uri) as HttpWebRequest;
                    request!.Method = WebRequestMethods.Http.Head;
                }
                else if (uri.Scheme == Uri.UriSchemeFtp)
                {
                    request = WebRequest.Create(uri) as FtpWebRequest;
                    request!.Method = WebRequestMethods.Ftp.GetFileSize;
                }

                var metric = new DownloaderMetric
                {
                    FileName = Path.GetFileName(filename)
                };

                using (var response = await request!.GetResponseAsync())
                {
                    metric.TotalBytes = response.ContentLength;
                }

                #endregion

                // Set start download position
                long startPos = 0;
                var fi = new FileInfo(filename);
                if (fi.Exists)
                {
                    if (metric.TotalBytes > fi.Length)
                    {
                        metric.DownloadedBytes = startPos = fi.Length - 1024;
                    }
                    else if (metric.TotalBytes == fi.Length)
                        return;
                }

                #region Read Content Stream Asynchronously

                if (uri.Scheme == Uri.UriSchemeHttp)
                {
                    request = WebRequest.Create(uri) as HttpWebRequest;
                    request = (HttpWebRequest) request;
                    request!.Method = WebRequestMethods.Http.Get;
                    request.AddRange(startPos, metric.TotalBytes);
                }
                else if (uri.Scheme == Uri.UriSchemeFtp)
                {
                    request = WebRequest.Create(uri) as FtpWebRequest;
                    request!.Method = WebRequestMethods.Ftp.DownloadFile;
                    request.ContentOffset = startPos;
                }

                var stopwatch = new Stopwatch();
                stopwatch.Start();
                if (!Directory.Exists(Path.GetDirectoryName(filename)))
                    Directory.CreateDirectory(Path.GetDirectoryName(filename)!);
                var fileStream = new FileStream(filename, FileMode.OpenOrCreate, FileAccess.ReadWrite)
                {
                    Position = metric.DownloadedBytes
                };
                await Task.Run(async () =>
                {
                    if (ct.IsCancellationRequested)
                        return;

                    using var streamReader = new StreamReader((await request.GetResponseAsync()).GetResponseStream());
                    OnDownloadStart?.Invoke(metric);
                    FromReaderToFile(streamReader, fileStream, ref metric, ref stopwatch, ct);
                    OnDownloadCompleted?.Invoke(metric);
                }, ct);

                #endregion
            }
            catch (OperationCanceledException)
            {
                const string msg = "Download cancelled by user";
                OnError?.Invoke(new DownloaderClientException(msg));
            }
            catch (Exception ex)
            {
                const string msg = "An unexpected error occurred.";
                OnError?.Invoke(
                    new DownloaderClientException($"{msg}\n\nDownload failed. See inner exception for details.", ex));
            }
        }

        private void FromReaderToFile(StreamReader src, FileStream dest,
            ref DownloaderMetric metric, ref Stopwatch stopwatch, CancellationToken ct)
        {
            var buffer = new byte[32 * 1024];
            int bytesRead;

            while ((bytesRead = src.BaseStream.Read(buffer, 0, buffer.Length)) > 0)
            {
                if (ct.IsCancellationRequested)
                {
                    dest.Flush(true);
                    dest.Close();
                    dest.Dispose();
                    return;
                }

                dest.Write(buffer, 0, bytesRead);
                metric.DownloadedBytes += bytesRead;
                metric.ElapsedTime = stopwatch.Elapsed;
                OnDownloading?.Invoke(metric);
            }

            OnDownloadCompleted?.Invoke(metric);

            dest.Flush(true);
            dest.Close();
            dest.Dispose();
        }
    }
}