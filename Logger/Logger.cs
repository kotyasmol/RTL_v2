using HandyControl.Themes;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows;
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
            set
            {
                _message = value;
                OnPropertyChanged();
            }
        }

        public SolidColorBrush Color
        {
            get => _color;
            set
            {
                _color = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class Loggers
    {
        private static ObservableCollection<LogEntry> _userLogMessages = new ObservableCollection<LogEntry>(); // Логи для пользовательского интерфейса
        private Serilog.Core.Logger _serilogLogger;
        private string _currentLogDirectory; // Текущий путь к директории логов

        // Статическое свойство для доступа к логам
        public static ObservableCollection<LogEntry> LogMessages => _userLogMessages;

        public Loggers(string logDirectory)
        {
            InitializeLogger(logDirectory);
        }

        /// <summary>
        /// Инициализирует логгер.
        /// </summary>
        private void InitializeLogger(string logDirectory)
        {
            _currentLogDirectory = logDirectory;

            try
            {
                // Создаём директорию, если она не существует
                if (!Directory.Exists(logDirectory))
                {
                    Directory.CreateDirectory(logDirectory);
                }

                // Очищаем старые логи
                CleanOldLogs(logDirectory);

                // Уникальное имя для нового файла лога
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

        /// <summary>
        /// Очищает старые логи старше двух дней.
        /// </summary>
        private void CleanOldLogs(string logDirectory)
        {
            try
            {
                var logFiles = Directory.GetFiles(logDirectory, "logs_*.txt");

                foreach (var file in logFiles)
                {
                    if (File.GetCreationTime(file) < DateTime.Now.AddDays(-2))
                    {
                        File.Delete(file);
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"Ошибка очистки старых логов: {ex.Message}", LogLevel.Warning);
            }
        }

        /// <summary>
        /// Обновляет директорию логов.
        /// </summary>
        public void UpdateLogDirectory(string newLogDirectory)
        {
            if (_currentLogDirectory != newLogDirectory)
            {
                InitializeLogger(newLogDirectory);
                Log($"Путь для логов обновлён на {newLogDirectory}", LogLevel.Info);
            }
        }

        /// <summary>
        /// Логирует сообщение в текстовый файл (по умолчанию).
        /// </summary>
        public void Log(string message, LogLevel level)
        {
            string logEntry = $"{DateTime.Now:dd.MM.yyyy HH:mm:ss}  {message}";

            switch (level)
            {
                case LogLevel.Error:
                    _serilogLogger?.Error(message);
                    break;
                case LogLevel.Info:
                    _serilogLogger?.Information(message);
                    break;
                case LogLevel.Warning:
                    _serilogLogger?.Warning(message);
                    break;
                case LogLevel.Debug:
                    _serilogLogger?.Debug(message);
                    break;
                case LogLevel.Critical:
                    _serilogLogger?.Fatal(message);
                    break;
                case LogLevel.Success:
                    _serilogLogger?.Information(message);
                    break;
                default:
                    _serilogLogger?.Information(message);
                    break;
            }
        }

        /// <summary>
        /// Логирует сообщение в интерфейс и текстовый файл.
        /// </summary>
        public void LogToUser(string message, LogLevel level)
        {
            Log(message, level); // Сохраняем лог в файл

            SolidColorBrush color = GetLogColor(level);

            // Добавляем сообщение в коллекцию для отображения в интерфейсе
            Application.Current.Dispatcher.Invoke(() =>
            {
                _userLogMessages.Add(new LogEntry
                {
                    Message = message,
                    Color = color
                });
            });
        }

        /// <summary>
        /// Обновляет цвета логов в зависимости от текущей темы.
        /// </summary>
        public void UpdateLogColors()
        {
            foreach (var logEntry in _userLogMessages)
            {
                logEntry.Color = GetLogColor(GetLogLevelFromMessage(logEntry.Message));
            }
        }

        /// <summary>
        /// Определяет уровень лога по сообщению.
        /// </summary>
        private LogLevel GetLogLevelFromMessage(string message)
        {
            // Здесь можно добавить логику для определения уровня лога по сообщению
            // Например, если сообщение содержит "[Error]", то это LogLevel.Error
            // В данном примере просто возвращаем LogLevel.Info
            return LogLevel.Info;
        }

        /// <summary>
        /// Проверяет, активна ли темная тема.
        /// </summary>
        private bool IsDarkTheme()
        {
            var theme = ThemeManager.Current.ActualApplicationTheme;
            return theme == ApplicationTheme.Dark;
        }

        /// <summary>
        /// Определяет цвет логов для пользовательского интерфейса.
        /// </summary>
        private SolidColorBrush GetLogColor(LogLevel level)
        {
            bool isDarkTheme = IsDarkTheme();

            return level switch
            {
                LogLevel.Error => isDarkTheme ? new SolidColorBrush(Colors.Tomato) : new SolidColorBrush(Colors.DarkRed),
                LogLevel.Info => isDarkTheme ? new SolidColorBrush(Colors.White) : new SolidColorBrush(Colors.Black),
                LogLevel.Warning => isDarkTheme ? new SolidColorBrush(Colors.Yellow) : new SolidColorBrush(Colors.Orange),
                LogLevel.Debug => isDarkTheme ? new SolidColorBrush(Colors.Gray) : new SolidColorBrush(Colors.DarkGray),
                LogLevel.Critical => isDarkTheme ? new SolidColorBrush(Colors.DarkRed) : new SolidColorBrush(Colors.Red),
                LogLevel.Success => isDarkTheme ? new SolidColorBrush(Colors.GreenYellow) : new SolidColorBrush(Colors.Green),
                _ => isDarkTheme ? new SolidColorBrush(Colors.White) : new SolidColorBrush(Colors.Black),
            };
        }

        public enum LogLevel
        {
            Error,    // Ошибка
            Info,     // Информация
            Warning,  // Предупреждение
            Debug,    // Отладка
            Critical, // Критическая ошибка
            Success   // Успех
        }
    }
}