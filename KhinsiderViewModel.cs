using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace NKhinsider
{
    public class KhinsiderViewModel : INotifyPropertyChanged
    {
        private string _albumUrl = "";
        private string _downloadPath = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
        private string _logText = "";
        public ObservableCollection<LogEntry> LogEntries { get; } = new ObservableCollection<LogEntry>();
        private string _statusText = "Ready to download";
        private string _progressText = "";
        private string _albumTitle = "";
        private string _albumCoverUrl = "";
        private string _trackCountText = "";
        private double _progress = 0;
        private bool _canDownload = true;
        private Visibility _albumInfoVisibility = Visibility.Collapsed;
        private bool _isDownloading = false;
        private string _downloadButtonText = "Download";
        private string _downloadButtonIcon = "Download";

        public string AlbumUrl
        {
            get => _albumUrl;
            set
            {
                _albumUrl = value;
                OnPropertyChanged();
                if (!_isDownloading)
                {
                    CanDownload = !string.IsNullOrEmpty(value) && IsValidKhinsiderUrl(value);
                }
            }
        }

        public string DownloadPath
        {
            get => _downloadPath;
            set
            {
                _downloadPath = value;
                OnPropertyChanged();
            }
        }

        public string LogText
        {
            get => _logText;
            set
            {
                _logText = value;
                OnPropertyChanged();
            }
        }

        public string StatusText
        {
            get => _statusText;
            set
            {
                _statusText = value;
                OnPropertyChanged();
            }
        }

        public string ProgressText
        {
            get => _progressText;
            set
            {
                _progressText = value;
                OnPropertyChanged();
            }
        }

        public string AlbumTitle
        {
            get => _albumTitle;
            set
            {
                _albumTitle = value;
                OnPropertyChanged();
            }
        }

        public string AlbumCoverUrl
        {
            get => _albumCoverUrl;
            set
            {
                _albumCoverUrl = value;
                OnPropertyChanged();
            }
        }

        public string TrackCountText
        {
            get => _trackCountText;
            set
            {
                _trackCountText = value;
                OnPropertyChanged();
            }
        }

        public double Progress
        {
            get => _progress;
            set
            {
                _progress = value;
                OnPropertyChanged();
            }
        }

        public bool CanDownload
        {
            get => _canDownload;
            set
            {
                _canDownload = value;
                OnPropertyChanged();
            }
        }

        public Visibility AlbumInfoVisibility
        {
            get => _albumInfoVisibility;
            set
            {
                _albumInfoVisibility = value;
                OnPropertyChanged();
            }
        }

        public bool IsDownloading
        {
            get => _isDownloading;
            set
            {
                _isDownloading = value;
                OnPropertyChanged();
                UpdateDownloadButton();
            }
        }

        public string DownloadButtonText
        {
            get => _downloadButtonText;
            set
            {
                _downloadButtonText = value;
                OnPropertyChanged();
            }
        }

        public string DownloadButtonIcon
        {
            get => _downloadButtonIcon;
            set
            {
                _downloadButtonIcon = value;
                OnPropertyChanged();
            }
        }

        private void UpdateDownloadButton()
        {
            if (_isDownloading)
            {
                DownloadButtonText = "Stop";
                DownloadButtonIcon = "Stop";
                CanDownload = true; // Keep button enabled for stopping
            }
            else
            {
                DownloadButtonText = "Download";
                DownloadButtonIcon = "Download";
                CanDownload = !string.IsNullOrEmpty(_albumUrl) && IsValidKhinsiderUrl(_albumUrl);
            }
        }

        public void AddLog(string message, LogLevel level = LogLevel.Info)
        {
            LogText += $"[{DateTime.Now:HH:mm:ss}] {message}\n";
            
            LogEntries.Add(new LogEntry
            {
                Timestamp = DateTime.Now.ToString("HH:mm:ss"),
                Message = message,
                Level = level
            });
            
            LogUpdated?.Invoke();
        }

        public event Action? LogUpdated;

        public void ClearLog()
        {
            LogText = "";
            LogEntries.Clear();
        }

        private bool IsValidKhinsiderUrl(string url)
        {
            return !string.IsNullOrEmpty(url) && 
                   url.Contains("downloads.khinsider.com") && 
                   url.Contains("game-soundtracks");
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
} 