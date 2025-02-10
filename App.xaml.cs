using RTL.Logger;
using System;
using System.Windows;
using RTL.Properties; // Добавляем namespace

namespace RTL
{
    public partial class App : Application
    {
        private Bootstrapper _bootstrapper;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                // Получаем путь к логам из настроек
                string logDirectory = Settings.Default.LogFolderPath;

                if (string.IsNullOrWhiteSpace(logDirectory))
                {
                    logDirectory = "C:/Logs"; // Значение по умолчанию
                }

                // Создаём логгер
                var logger = new Loggers(logDirectory);
                logger.Log("Приложение запущено", Loggers.LogLevel.Success);

                // Запускаем Bootstrapper
                _bootstrapper = new Bootstrapper();


                logger.Log("Bootstrapper успешно запущен", Loggers.LogLevel.Success);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при запуске: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }
        }
    }
}
