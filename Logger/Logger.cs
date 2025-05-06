using HandyControl.Themes;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using Serilog;

namespace RTL.Logger
{
    public class LogEntry : INotifyPropertyChanged
    {
        private string _message;
        private SolidColorBrush _color;

        public string Message
        {
            get => _message;
            set { _message = value; OnPropertyChanged(); }
        }

        public SolidColorBrush Color
        {
            get => _color;
            set { _color = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public class Loggers
    {
        private ObservableCollection<LogEntry> _userLogMessages = new ObservableCollection<LogEntry>();
        private Serilog.Core.Logger _serilogLogger;
        private string _currentLogDirectory;

        public ObservableCollection<LogEntry> LogMessages => _userLogMessages;

        public Loggers(string logDirectory)
        {
            InitializeLogger(logDirectory);
        }

        private void InitializeLogger(string logDirectory)
        {
            _currentLogDirectory = logDirectory;

            try
            {
                if (!Directory.Exists(logDirectory))
                    Directory.CreateDirectory(logDirectory);

                CleanOldLogs(logDirectory);

                string logFilePath = Path.Combine(logDirectory, $"logs_{DateTime.Now:yyyyMMdd_HHmmss}.txt");

                _serilogLogger = new LoggerConfiguration()
                    .MinimumLevel.Debug()
                    .WriteTo.File(logFilePath, retainedFileCountLimit: null, fileSizeLimitBytes: 10_000_000)
                    .CreateLogger();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка инициализации логгера: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CleanOldLogs(string logDirectory)
        {
            try
            {
                var logFiles = Directory.GetFiles(logDirectory, "logs_*.txt");
                foreach (var file in logFiles)
                {
                    if (File.GetCreationTime(file) < DateTime.Now.AddDays(-90))
                        File.Delete(file);
                }
            }
            catch (Exception ex)
            {
                Log($"Ошибка очистки старых логов: {ex.Message}", LogLevel.Warning);
            }
        }

        public void Log(string message, LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Error: _serilogLogger?.Error(message); break;
                case LogLevel.Info: _serilogLogger?.Information(message); break;
                case LogLevel.Warning: _serilogLogger?.Warning(message); break;
                case LogLevel.Debug: _serilogLogger?.Debug(message); break;
                case LogLevel.Critical: _serilogLogger?.Fatal(message); break;
                case LogLevel.Success: _serilogLogger?.Information(message); break;
                default: _serilogLogger?.Information(message); break;
            }
        }

        public void LogToUser(string message, LogLevel level)
        {
            Log(message, level);

            SolidColorBrush color = GetLogColor(level);

            Application.Current.Dispatcher.Invoke(() =>
            {
                _userLogMessages.Add(new LogEntry
                {
                    Message = message,
                    Color = color
                });
            });
        }

        public void UpdateLogColors()
        {
            foreach (var logEntry in _userLogMessages)
            {
                logEntry.Color = GetLogColor(LogLevel.Info); // Можно улучшить, если надо
            }
        }

        private bool IsDarkTheme() =>
            Application.Current.Dispatcher.Invoke(() =>
            {
                var theme = ThemeManager.Current.ActualApplicationTheme;
                return theme == ApplicationTheme.Dark;
            });

        private SolidColorBrush GetLogColor(LogLevel level) =>
            Application.Current.Dispatcher.Invoke(() =>
            {
                bool isDark = IsDarkTheme();
                return level switch
                {
                    LogLevel.Error => new SolidColorBrush(isDark ? Colors.Tomato : Colors.DarkRed),
                    LogLevel.Info => new SolidColorBrush(isDark ? Colors.White : Colors.Black),
                    LogLevel.Warning => new SolidColorBrush(isDark ? Colors.Yellow : Colors.DarkOrange),
                    LogLevel.Debug => new SolidColorBrush(isDark ? Colors.Gray : Colors.Blue),
                    LogLevel.Critical => new SolidColorBrush(isDark ? Colors.DarkRed : Colors.Red),
                    LogLevel.Success => new SolidColorBrush(isDark ? Colors.GreenYellow : Colors.DarkGreen),
                    _ => new SolidColorBrush(isDark ? Colors.White : Colors.Black),
                };
            });

        public enum LogLevel
        {
            Error,
            Info,
            Warning,
            Debug,
            Critical,
            Success
        }
    }
}
