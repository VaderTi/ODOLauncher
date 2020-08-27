using System;

namespace ODOLauncher.Models.Downloader
{
    /// <summary>
    /// A time-series representation metric object that defines the state
    /// of a download operation/activity either before, during download, or
    /// after the download completes.
    /// </summary>
    public struct DownloaderMetric
    {
        /// <summary>
        /// Initializes a download metric.
        /// </summary>
        /// <param name="totalBytes"></param>
        public DownloaderMetric(long? totalBytes = null)
        {
            FileName = string.Empty;
            DownloadedBytes = 0;
            TotalBytes = totalBytes ?? int.MaxValue;
            ElapsedTime = new TimeSpan();
        }

        public string FileName { get; set; }

        /// <summary>
        /// Received/Downloaded Bytes. (d)
        /// </summary>
        public long DownloadedBytes { get; set; }

        /// <summary>
        /// Total Bytes to download. (b)
        /// </summary>
        public long TotalBytes { get; set; }

        /// <summary>
        /// Download progress percentage. (p)
        /// 
        /// <code>
        ///     = (d / b) * 100;
        /// </code>
        /// </summary>
        public double Progress
        {
            get
            {
                try
                {
                    return (DownloadedBytes / (double) TotalBytes) * 100;
                }
                catch (DivideByZeroException)
                {
                    return 0;
                }
            }
        }

        /// <summary>
        /// Remaining Bytes to complete download. (r)
        /// 
        /// <code>
        ///     = (b - d);
        /// </code>
        /// </summary>
        public long RemainingBytes => TotalBytes - DownloadedBytes;

        /// <summary>
        /// Elapsed time at the current moment. (t)
        /// </summary>
        public TimeSpan ElapsedTime { get; set; }

        /// <summary>
        /// Download speed. (s)
        /// 
        /// <code>
        ///     = (d / t);
        /// </code>.
        /// 
        /// The result is in (bytes/sec)
        /// </summary>
        public double Speed
        {
            get
            {
                try
                {
                    return (DownloadedBytes / ElapsedTime.TotalSeconds);
                }
                catch (DivideByZeroException)
                {
                    return 0;
                }
            }
        }

        public double SpeedInKb => Speed / 1024;

        public double SpeedInMb => SpeedInKb / 1024;

        /// <summary>
        /// Expiration or time remaining for download to complete. (e)
        /// <code>
        ///     = (r / s);
        /// </code>
        /// </summary>
        public TimeSpan TimeRemaining
        {
            get
            {
                try
                {
                    return TimeSpan.FromSeconds(RemainingBytes / Speed);
                }
                catch (DivideByZeroException)
                {
                    return TimeSpan.FromSeconds(0);
                }
                catch (Exception)
                {
                    return new TimeSpan();
                }
            }
        }
    }
}