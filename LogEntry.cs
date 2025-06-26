using System.Windows.Media;

namespace NKhinsider
{
    public class LogEntry
    {
        public string Timestamp { get; set; } = "";
        public string Message { get; set; } = "";
        public LogLevel Level { get; set; } = LogLevel.Info;
        public Brush TextColor => Level switch
        {
            LogLevel.Info => new SolidColorBrush(Color.FromRgb(224, 224, 224)),      // Light gray
            LogLevel.Success => new SolidColorBrush(Color.FromRgb(76, 175, 80)),     // Green
            LogLevel.Warning => new SolidColorBrush(Color.FromRgb(255, 193, 7)),     // Amber
            LogLevel.Error => new SolidColorBrush(Color.FromRgb(244, 67, 54)),       // Red
            LogLevel.Debug => new SolidColorBrush(Color.FromRgb(156, 39, 176)),      // Purple
            _ => new SolidColorBrush(Color.FromRgb(224, 224, 224))
        };
    }

    public enum LogLevel
    {
        Info,
        Success,
        Warning,
        Error,
        Debug
    }
} 