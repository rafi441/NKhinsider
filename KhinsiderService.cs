using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace NKhinsider
{
    public class KhinsiderService : IDisposable
    {
        private const string BASE_URL = "https://downloads.khinsider.com";
        private readonly HttpClient _httpClient;

        public KhinsiderService()
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:141.0) Gecko/20100101 Firefox/141.0");
            _httpClient.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8");
            _httpClient.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.5");
            _httpClient.Timeout = TimeSpan.FromMinutes(5); // 5 minute timeout
        }

        private bool ValidateUrl(string url)
        {
            return url.Contains("//downloads.khinsider.com/game-soundtracks/album/");
        }

        public async Task<AlbumInfo> GetAlbumInfoAsync(string albumUrl)
        {
            if (!ValidateUrl(albumUrl))
            {
                throw new Exception("Invalid URL. Must be a khinsider album URL.");
            }

            var html = await _httpClient.GetStringAsync(albumUrl);
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            // Get album title
            var albumTitle = GetAlbumTitle(albumUrl, doc);

            // Get album cover
            var albumCover = GetAlbumCover(doc);

            // Get tracks
            var tracks = GetTracks(doc);

            return new AlbumInfo
            {
                Title = albumTitle,
                CoverUrl = albumCover,
                Tracks = tracks
            };
        }

        private string GetAlbumTitle(string albumUrl, HtmlDocument doc)
        {
            // Try to get title from page first
            var titleNode = doc.DocumentNode.SelectSingleNode("//h2") ?? doc.DocumentNode.SelectSingleNode("//h1");
            if (titleNode != null && !string.IsNullOrWhiteSpace(titleNode.InnerText))
            {
                var pageTitle = titleNode.InnerText.Trim();
                pageTitle = System.Net.WebUtility.HtmlDecode(pageTitle);
                if (pageTitle.Length > 5 && !pageTitle.ToLower().Contains("khinsider"))
                {
                    return pageTitle;
                }
            }

            // Fallback to URL
            var urlParts = albumUrl.Split('/');
            if (urlParts.Length > 0)
            {
                return urlParts[urlParts.Length - 1].Replace("-", " ");
            }

            return "Unknown Album";
        }

        private string GetAlbumCover(HtmlDocument doc)
        {
            var selectors = new[]
            {
                "//img[@id='albumImage']",
                "//img[contains(@class, 'albumImage')]",
                "//img[contains(@alt, 'album')]",
                "//img[contains(@alt, 'Album')]",
                "//div[contains(@class, 'albumImage')]//img",
                "//td[contains(@class, 'albumImage')]//img",
                "//img[contains(@src, 'album')]",
                "//table//img[contains(@src, 'covers')]",
                "//img[contains(@src, 'cover')]"
            };

            foreach (var selector in selectors)
            {
                var coverNode = doc.DocumentNode.SelectSingleNode(selector);
                if (coverNode != null)
                {
                    var src = coverNode.GetAttributeValue("src", "");
                    if (!string.IsNullOrEmpty(src))
                    {
                        if (!src.StartsWith("http"))
                        {
                            if (src.StartsWith("//"))
                                src = "https:" + src;
                            else if (src.StartsWith("/"))
                                src = BASE_URL + src;
                            else
                                src = BASE_URL + "/" + src;
                        }

                        if (src.Contains(".jpg") || src.Contains(".jpeg") || src.Contains(".png") || src.Contains(".gif"))
                        {
                            return src;
                        }
                    }
                }
            }

            return "";
        }

        private List<TrackInfo> GetTracks(HtmlDocument doc)
        {
            var songList = doc.DocumentNode.SelectSingleNode("//table[@id='songlist']");
            if (songList == null)
            {
                throw new Exception("Could not find song list table.");
            }

            var anchors = songList.SelectNodes(".//a");
            if (anchors == null)
            {
                throw new Exception("No song links found.");
            }

            var songMap = new Dictionary<string, string>();

            foreach (var anchor in anchors)
            {
                var href = anchor.GetAttributeValue("href", "");
                if (!string.IsNullOrEmpty(href) && href.Contains("mp3"))
                {
                    href = BASE_URL + href;
                    if (!songMap.ContainsKey(href))
                    {
                        var songName = GetSongName(anchor);
                        if (!string.IsNullOrEmpty(songName))
                        {
                            songMap[href] = songName;
                        }
                    }
                }
            }

            if (songMap.Count == 0)
            {
                throw new Exception("No tracks found.");
            }

            var tracks = new List<TrackInfo>();
            foreach (var kvp in songMap)
            {
                tracks.Add(new TrackInfo
                {
                    Name = CleanFileName(kvp.Value),
                    Url = kvp.Key,
                    IsDirectDownload = false
                });
            }

            return tracks;
        }

        private string GetSongName(HtmlNode anchor)
        {
            var songName = anchor.InnerText?.Trim() ?? "";

            // If anchor text is empty or just "download", get from parent row
            if (string.IsNullOrEmpty(songName) || songName.ToLower().Contains("download"))
            {
                var parentRow = anchor.ParentNode;
                while (parentRow != null && parentRow.Name.ToLower() != "tr")
                {
                    parentRow = parentRow.ParentNode;
                }

                if (parentRow != null)
                {
                    var cells = parentRow.SelectNodes(".//td");
                    if (cells != null && cells.Count > 1)
                    {
                        songName = cells[1].InnerText?.Trim() ?? "";
                    }
                }
            }

            return songName.Length > 1 ? songName : "";
        }

        public async Task<string> GetDirectDownloadUrlAsync(string trackPageUrl)
        {
            var html = await _httpClient.GetStringAsync(trackPageUrl);
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            // Look for audio tag first
            var audioNode = doc.DocumentNode.SelectSingleNode("//audio");
            if (audioNode != null)
            {
                var src = audioNode.GetAttributeValue("src", "");
                if (!string.IsNullOrEmpty(src))
                {
                    return src;
                }
            }

            // Fallback to any MP3 link
            var mp3Links = doc.DocumentNode.SelectNodes("//a[contains(@href, '.mp3')]");
            if (mp3Links != null)
            {
                foreach (var link in mp3Links)
                {
                    var href = link.GetAttributeValue("href", "");
                    if (!string.IsNullOrEmpty(href) && href.Contains(".mp3"))
                    {
                        return href;
                    }
                }
            }

            throw new Exception("Could not find MP3 download link.");
        }

        public async Task<bool> ValidateUrlAsync(string url)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Head, url);
                using var response = await _httpClient.SendAsync(request);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task DownloadTrackAsync(string downloadUrl, string filePath, IProgress<double> progress = null)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, downloadUrl);
            request.Headers.Add("Referer", "https://downloads.khinsider.com/");

            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? 0;
            var downloadedBytes = 0L;

            using var contentStream = await response.Content.ReadAsStreamAsync();
            using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 8192, useAsync: true);

            var buffer = new byte[8192];
            int bytesRead;

            while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                await fileStream.WriteAsync(buffer, 0, bytesRead);
                downloadedBytes += bytesRead;

                if (totalBytes > 0 && progress != null)
                {
                    var progressPercentage = (double)downloadedBytes / totalBytes * 100;
                    progress.Report(progressPercentage);
                }
            }
        }

        public async Task DownloadAllTracksAsync(List<TrackInfo> tracks, string downloadFolder, 
            IProgress<(int completed, int total, string currentTrack)> progress = null, 
            int maxConcurrentDownloads = 5, CancellationToken cancellationToken = default)
        {
            var semaphore = new SemaphoreSlim(maxConcurrentDownloads, maxConcurrentDownloads);
            var completedCount = 0;
            var totalCount = tracks.Count;
            var downloadedMp3s = new ConcurrentDictionary<string, bool>();

            var downloadTasks = tracks.Select(async (track, index) =>
            {
                await semaphore.WaitAsync(cancellationToken);
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    // Get the direct download URL
                    var downloadUrl = await GetDirectDownloadUrlAsync(track.Url);
                    
                    // Skip if we've already downloaded this MP3 URL (avoid duplicates)
                    if (!downloadedMp3s.TryAdd(downloadUrl, true))
                    {
                        var skippedCount = Interlocked.Increment(ref completedCount);
                        progress?.Report((skippedCount, totalCount, $"Skipped: {track.Name} (duplicate)"));
                        return;
                    }

                    var fileName = track.Name;
                    if (!fileName.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase))
                    {
                        fileName += ".mp3";
                    }

                    var filePath = Path.Combine(downloadFolder, fileName);

                    // Check if file already exists and has content
                    if (File.Exists(filePath))
                    {
                        var fileInfo = new FileInfo(filePath);
                        if (fileInfo.Length > 0)
                        {
                            var skippedCount = Interlocked.Increment(ref completedCount);
                            progress?.Report((skippedCount, totalCount, $"Skipped: {track.Name}"));
                            return;
                        }
                    }

                    // Report current download
                    progress?.Report((completedCount, totalCount, $"Downloading: {track.Name}"));

                    // Create individual HttpClient for this download to avoid concurrency issues
                    using var httpClient = new HttpClient();
                    httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:141.0) Gecko/20100101 Firefox/141.0");
                    httpClient.Timeout = TimeSpan.FromMinutes(5);

                    using var request = new HttpRequestMessage(HttpMethod.Get, downloadUrl);
                    request.Headers.Add("Referer", "https://downloads.khinsider.com/");

                    using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                    response.EnsureSuccessStatusCode();

                    using var contentStream = await response.Content.ReadAsStreamAsync();
                    using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 8192, useAsync: true);

                    await contentStream.CopyToAsync(fileStream, cancellationToken);

                    // Update progress
                    var completedAfterDownload = Interlocked.Increment(ref completedCount);
                    progress?.Report((completedAfterDownload, totalCount, $"Completed: {track.Name}"));
                }
                catch (OperationCanceledException)
                {
                    // Download was cancelled - don't log as error
                    var completed = Interlocked.Increment(ref completedCount);
                    progress?.Report((completed, totalCount, $"Cancelled: {track.Name}"));
                    throw; // Re-throw to stop other downloads
                }
                catch (Exception ex)
                {
                    // Log error but continue with other downloads
                    var completed = Interlocked.Increment(ref completedCount);
                    progress?.Report((completed, totalCount, $"Failed: {track.Name} - {ex.Message}"));
                }
                finally
                {
                    semaphore.Release();
                }
            });

            await Task.WhenAll(downloadTasks);
        }

        private string CleanFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return "";

            fileName = System.Net.WebUtility.HtmlDecode(fileName);

            var invalidChars = Path.GetInvalidFileNameChars();
            foreach (var c in invalidChars)
            {
                fileName = fileName.Replace(c, '_');
            }

            fileName = Regex.Replace(fileName, @"\s+", " ").Trim();

            if (!fileName.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase))
            {
                fileName += ".mp3";
            }

            return fileName;
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }

    public class AlbumInfo
    {
        public string Title { get; set; } = "";
        public string CoverUrl { get; set; } = "";
        public List<TrackInfo> Tracks { get; set; } = new List<TrackInfo>();
    }

    public class TrackInfo
    {
        public string Name { get; set; } = "";
        public string Url { get; set; } = "";
        public bool IsDirectDownload { get; set; } = false;
    }
} 