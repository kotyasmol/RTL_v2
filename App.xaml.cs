using RTL.Logger;
using System;
using System.Windows;
using RTL.Properties; // Подключаем настройки

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
                // Получаем путь к логам из настроек или устанавливаем путь по умолчанию
                string logDirectory = Settings.Default.LogFolderPath;
                if (string.IsNullOrWhiteSpace(logDirectory))
                {
                    logDirectory = "C:/Logs"; // Значение по умолчанию
                }

                // Запускаем Bootstrapper и передаём путь к логам
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
