using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Net;

namespace BeanModManager.Services
{
    public static class HttpDownloadHelper
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        static HttpDownloadHelper()
        {
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "BeanModManager");
        }

        public static async Task DownloadFileAsync(string url, string destinationPath, IProgress<int> progress = null, CancellationToken cancellationToken = default)
        {
            using (var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false))
            {
                response.EnsureSuccessStatusCode();

                var totalBytes = response.Content.Headers.ContentLength ?? -1L;
                var canReportProgress = totalBytes > 0 && progress != null;

                using (var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                using (var contentStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                {
                    var totalBytesRead = 0L;
                    var buffer = new byte[8192];
                    var bytesRead = 0;

                    while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false)) > 0)
                    {
                        await fileStream.WriteAsync(buffer, 0, bytesRead, cancellationToken).ConfigureAwait(false);
                        totalBytesRead += bytesRead;

                        if (canReportProgress)
                        {
                            var percent = (int)((totalBytesRead * 100) / totalBytes);
                            progress.Report(percent);
                        }
                    }
                }
            }
        }

        public static async Task<string> DownloadStringAsync(string url, CancellationToken cancellationToken = default)
        {
            using (var response = await _httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false))
            {
                if (response.StatusCode == HttpStatusCode.Forbidden)
                {
                    throw new HttpRequestException("403 Forbidden - Rate limit exceeded");
                }
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            }
        }
        
        // Synchronous version for constructor use (blocks thread)
        public static string DownloadString(string url)
        {
            return DownloadStringAsync(url).GetAwaiter().GetResult();
        }
    }
}

