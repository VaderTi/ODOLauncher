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

        public void DownloadToFile(Uri uri, string folderPath, string filename)
        {
            filename ??= Path.GetFileName(uri.ToString());
            folderPath ??= Directory.GetCurrentDirectory();
            ProcessDownload(uri, Path.Combine(folderPath, filename), CancellationToken.None);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "RCS1090:Call 'ConfigureAwait(false)'.",
            Justification = "<Ожидание>")]
        public async Task DownloadToFileAsync(Uri uri, string folderPath, string filename)
        {
            filename ??= Path.GetFileName(uri.ToString());
            folderPath ??= Directory.GetCurrentDirectory();
            await ProcessDownloadAsync(uri, Path.Combine(folderPath, filename), CancellationToken.None);
        }

        public string GetLogin(Uri uri)
        {
            return string.IsNullOrWhiteSpace(uri.UserInfo) ? string.Empty : uri.UserInfo.Split(':')[0];
        }

        public string GetPassword(Uri uri)
        {
            return string.IsNullOrWhiteSpace(uri.UserInfo) ? string.Empty : uri.UserInfo.Split(':')[1];
        }

        private void ProcessDownload(Uri uri, string filename, CancellationToken ct)
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

                var metric = new DownloaderMetric {FileName = filename};

                using (var response = request!.GetResponse())
                {
                    metric.TotalBytes = response.ContentLength;
                }

                #endregion

                #region Read Content Stream

                if (uri.Scheme == Uri.UriSchemeHttp)
                {
                    request = WebRequest.Create(uri) as HttpWebRequest;
                    request!.Method = WebRequestMethods.Http.Get;
                    request.AddRange(0, metric.TotalBytes);
                }
                else if (uri.Scheme == Uri.UriSchemeFtp)
                {
                    request = WebRequest.Create(uri) as FtpWebRequest;
                    request!.Method = WebRequestMethods.Ftp.DownloadFile;
                }

                var stopwatch = new Stopwatch();
                stopwatch.Start();
                OnDownloadStart?.Invoke(metric);
                if (!Directory.Exists(Path.GetDirectoryName(filename)))
                    Directory.CreateDirectory(Path.GetDirectoryName(filename)!);
                var fileStream = File.Open(filename, FileMode.Create, FileAccess.ReadWrite);
                var streamReader = new StreamReader(request.GetResponse().GetResponseStream()!);
                FromReaderToFile(streamReader, fileStream, ref metric, ref stopwatch, ct);
                OnDownloadCompleted?.Invoke(metric);
                stopwatch.Stop();

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

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "RCS1090:Call 'ConfigureAwait(false)'.",
            Justification = "<Ожидание>")]
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

                #region Read Content Stream Asynchronously

                if (uri.Scheme == Uri.UriSchemeHttp)
                {
                    request = WebRequest.Create(uri) as HttpWebRequest;
                    request!.Method = WebRequestMethods.Http.Get;
                    request.AddRange(0, metric.TotalBytes);
                }
                else if (uri.Scheme == Uri.UriSchemeFtp)
                {
                    request = WebRequest.Create(uri) as FtpWebRequest;
                    request!.Method = WebRequestMethods.Ftp.DownloadFile;
                }

                var stopwatch = new Stopwatch();
                stopwatch.Start();
                if (!Directory.Exists(Path.GetDirectoryName(filename)))
                    Directory.CreateDirectory(Path.GetDirectoryName(filename)!);
                var fileStream = new FileStream(filename, FileMode.Create, FileAccess.ReadWrite);
                await Task.Run(async () =>
                {
                    ct.ThrowIfCancellationRequested();

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
                    dest.SetLength(0);
                    dest.Close();

                    if (File.Exists(dest.Name))
                        File.Delete(dest.Name);

                    ct.ThrowIfCancellationRequested();
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