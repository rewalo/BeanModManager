using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

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
            var result = await DownloadStringWithETagAsync(url, null, cancellationToken).ConfigureAwait(false);
            return result?.Content;
        }

        public static async Task<string> DownloadStringAsync(string url, string etag, CancellationToken cancellationToken = default)
        {
            var result = await DownloadStringWithETagAsync(url, etag, cancellationToken).ConfigureAwait(false);
            return result?.Content;
        }

        public class DownloadResult
        {
            public string Content { get; set; }
            public string ETag { get; set; }
            public bool NotModified { get; set; }
        }

        public static async Task<DownloadResult> DownloadStringWithETagAsync(string url, string etag, CancellationToken cancellationToken = default)
        {
            using (var request = new HttpRequestMessage(HttpMethod.Get, url))
            {
                if (!string.IsNullOrEmpty(etag))
                {
                    request.Headers.TryAddWithoutValidation("If-None-Match", etag);
                }

                using (var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false))
                {
                    if (response.StatusCode == HttpStatusCode.NotModified)
                    {
                        return new DownloadResult { NotModified = true };
                    }

                    if (response.StatusCode == HttpStatusCode.Forbidden)
                    {
                        throw new HttpRequestException("403 Forbidden - Rate limit exceeded");
                    }

                    response.EnsureSuccessStatusCode();

                    var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    var responseETag = GetETagFromResponse(response);

                    return new DownloadResult
                    {
                        Content = content,
                        ETag = responseETag,
                        NotModified = false
                    };
                }
            }
        }

        private static string GetETagFromResponse(HttpResponseMessage response)
        {
            if (response?.Headers?.ETag != null)
            {
                var etagValue = response.Headers.ETag.ToString();
                if (etagValue.StartsWith("\"") && etagValue.EndsWith("\""))
                {
                    return etagValue.Substring(1, etagValue.Length - 2);
                }
                return etagValue;
            }
            return null;
        }

        public static string DownloadString(string url)
        {
            return DownloadStringAsync(url).GetAwaiter().GetResult();
        }
    }
}

