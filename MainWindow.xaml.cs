using System.IO;
using System.Windows;
using MaterialDesignThemes.Wpf;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace NKhinsider
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly KhinsiderViewModel _viewModel;
        private readonly KhinsiderService _khinsiderService;
        private CancellationTokenSource _cancellationTokenSource;

        public MainWindow()
        {
            InitializeComponent();
            
            _viewModel = new KhinsiderViewModel();
            _khinsiderService = new KhinsiderService();
            
            DataContext = _viewModel;
            
            // Initialize Material Design
            var paletteHelper = new PaletteHelper();
            var theme = paletteHelper.GetTheme();
            theme.SetBaseTheme(Theme.Light);
            paletteHelper.SetTheme(theme);
            
            _viewModel.AddLog("Application started. Ready to download Khinsider albums!");
            _viewModel.StatusText = "Ready - Enter a Khinsider album URL to begin";
            
            // Subscribe to log updates for auto-scrolling
            _viewModel.LogUpdated += () =>
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    LogScrollViewer.ScrollToEnd();
                }));
            };
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CommonOpenFileDialog()
            {
                Title = "Select download folder",
                IsFolderPicker = true,
                InitialDirectory = _viewModel.DownloadPath,
                AddToMostRecentlyUsedList = false,
                AllowNonFileSystemItems = false,
                DefaultDirectory = _viewModel.DownloadPath,
                EnsureFileExists = true,
                EnsurePathExists = true,
                EnsureReadOnly = false,
                EnsureValidNames = true,
                Multiselect = false,
                ShowPlacesList = true
            };

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                _viewModel.DownloadPath = dialog.FileName;
                _viewModel.AddLog($"Download path changed to: {dialog.FileName}");
            }
        }

        private async void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.IsDownloading)
            {
                // Stop the download
                _cancellationTokenSource?.Cancel();
                _viewModel.AddLog("Stopping download...", LogLevel.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(_viewModel.AlbumUrl))
            {
                _viewModel.AddLog("Please enter a valid Khinsider album URL", LogLevel.Warning);
                _viewModel.StatusText = "Error - No URL provided";
                return;
            }

            _cancellationTokenSource = new CancellationTokenSource();
            _viewModel.IsDownloading = true;
            _viewModel.Progress = 0;
            _viewModel.StatusText = "Fetching album information...";
            StatusIcon.Kind = PackIconKind.Download;

            try
            {
                _viewModel.AddLog($"Fetching album info from: {_viewModel.AlbumUrl}");
                
                // Get album information
                var albumInfo = await _khinsiderService.GetAlbumInfoAsync(_viewModel.AlbumUrl);
                
                _viewModel.AlbumTitle = albumInfo.Title;
                _viewModel.AlbumCoverUrl = albumInfo.CoverUrl;
                _viewModel.TrackCountText = $"{albumInfo.Tracks.Count} tracks";
                _viewModel.AlbumInfoVisibility = Visibility.Visible;
                
                _viewModel.AddLog($"Album: {albumInfo.Title}");
                _viewModel.AddLog($"Cover: {albumInfo.CoverUrl}");
                _viewModel.AddLog($"Found {albumInfo.Tracks.Count} tracks");
                
                // Log track details for debugging
                var directCount = albumInfo.Tracks.Count(t => t.IsDirectDownload);
                var songPageCount = albumInfo.Tracks.Count(t => !t.IsDirectDownload);
                _viewModel.AddLog($"Direct MP3 links: {directCount}, Song pages: {songPageCount}", LogLevel.Debug);
                
                if (albumInfo.Tracks.Count > 0)
                {
                    var firstTrack = albumInfo.Tracks[0];
                    _viewModel.AddLog($"First track: {firstTrack.Name} -> {firstTrack.Url} (Direct: {firstTrack.IsDirectDownload})", LogLevel.Debug);
                }

                // Create album folder
                var albumFolderName = CleanFolderName(albumInfo.Title);
                var albumPath = Path.Combine(_viewModel.DownloadPath, albumFolderName);
                
                if (!Directory.Exists(albumPath))
                {
                    Directory.CreateDirectory(albumPath);
                    _viewModel.AddLog($"Created album folder: {albumPath}");
                }

                _viewModel.StatusText = "Downloading tracks...";
                StatusIcon.Kind = PackIconKind.CloudDownload;

                // Use parallel download with progress reporting
                var totalTracks = albumInfo.Tracks.Count;
                var downloadProgress = new Progress<(int completed, int total, string currentTrack)>(update =>
                {
                    var (completed, total, currentTrack) = update;
                    _viewModel.Progress = (double)completed / total * 100;
                    _viewModel.ProgressText = $"{completed}/{total}";
                    
                    if (currentTrack.StartsWith("Downloading:"))
                    {
                        _viewModel.AddLog(currentTrack.Replace("Downloading:", "⬇"), LogLevel.Info);
                    }
                    else if (currentTrack.StartsWith("Completed:"))
                    {
                        _viewModel.AddLog($"✓ {currentTrack.Replace("Completed:", "")}", LogLevel.Success);
                    }
                    else if (currentTrack.StartsWith("Failed:"))
                    {
                        _viewModel.AddLog($"✗ {currentTrack.Replace("Failed:", "")}", LogLevel.Error);
                    }
                    else if (currentTrack.StartsWith("Skipped:"))
                    {
                        _viewModel.AddLog($"⊘ {currentTrack.Replace("Skipped:", "")} (already exists)", LogLevel.Info);
                    }
                    else if (currentTrack.StartsWith("Cancelled:"))
                    {
                        _viewModel.AddLog($"⏹ {currentTrack.Replace("Cancelled:", "")} (cancelled)", LogLevel.Warning);
                    }
                });

                // Download all tracks in parallel (5 concurrent downloads by default)
                await _khinsiderService.DownloadAllTracksAsync(albumInfo.Tracks, albumPath, downloadProgress, maxConcurrentDownloads: 5, _cancellationTokenSource.Token);

                var finalCompleted = (int)Math.Round(_viewModel.Progress / 100 * totalTracks);
                _viewModel.ProgressText = $"Complete ({finalCompleted}/{totalTracks})";
                _viewModel.StatusText = $"Download completed! {finalCompleted}/{totalTracks} tracks downloaded";
                StatusIcon.Kind = PackIconKind.CheckCircle;
                
                _viewModel.AddLog($"Download completed! {finalCompleted} out of {totalTracks} tracks processed.", LogLevel.Success);
                _viewModel.AddLog($"Files saved to: {albumPath}", LogLevel.Info);

                // Open folder
                if (Directory.Exists(albumPath) && Directory.GetFiles(albumPath, "*.mp3").Length > 0)
                {
                    System.Diagnostics.Process.Start("explorer.exe", albumPath);
                }
            }
            catch (OperationCanceledException)
            {
                _viewModel.AddLog("Download cancelled by user.", LogLevel.Warning);
                _viewModel.StatusText = "Download cancelled";
                StatusIcon.Kind = PackIconKind.StopCircle;
            }
            catch (Exception ex)
            {
                _viewModel.AddLog($"Error: {ex.Message}", LogLevel.Error);
                _viewModel.StatusText = "Download failed - Check log for details";
                StatusIcon.Kind = PackIconKind.AlertCircle;
            }
            finally
            {
                _viewModel.IsDownloading = false;
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }

        private void ClearLogButton_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.ClearLog();
            _viewModel.StatusText = "Log cleared - Ready for new download";
        }

        private string CleanFolderName(string folderName)
        {
            var invalidChars = Path.GetInvalidPathChars().Concat(Path.GetInvalidFileNameChars()).ToArray();
            var cleanName = new string(folderName.Where(c => !invalidChars.Contains(c)).ToArray());
            return string.IsNullOrWhiteSpace(cleanName) ? "Unknown Album" : cleanName.Trim();
        }

        protected override void OnClosed(EventArgs e)
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _khinsiderService?.Dispose();
            base.OnClosed(e);
        }
    }
}