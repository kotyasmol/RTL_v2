using RTL.Logger;
using System;
using System.Windows;
using RTL.Properties; // Подключаем настройки

namespace RTL
{
    public partial class App : Application
    {
        private Bootstrapper _bootstrapper;

        // Добавляем публичные свойства для sessionId и username
        public static string StartupSessionId { get; private set; }
        public static string StartupUserName { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                // Чтение аргументов запуска
                if (e.Args.Length > 0)
                    StartupSessionId = e.Args[0];

                if (e.Args.Length > 1)
                    StartupUserName = e.Args[1];

                // Получаем путь к логам из настроек или устанавливаем путь по умолчанию
                string logDirectory = Settings.Default.LogFolderPath;
                if (string.IsNullOrWhiteSpace(logDirectory))
                {
                    logDirectory = "C:/TFortisBoardLogs"; // Значение по умолчанию
                }

                // Запускаем Bootstrapper
                _bootstrapper = new Bootstrapper();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при запуске: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }
        }
    }
}
