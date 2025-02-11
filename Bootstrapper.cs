using Stylet;
using StyletIoC;
using RTL.ViewModels;
using RTL.Logger;
using RTL.Properties;

namespace RTL
{
    public class Bootstrapper : Stylet.Bootstrapper<MainViewModel>
    {
        protected override void ConfigureIoC(IStyletIoCBuilder builder)
        {
            // Получаем путь к логам из настроек
            string logDirectory = Settings.Default.LogFolderPath;
            if (string.IsNullOrWhiteSpace(logDirectory))
            {
                logDirectory = "C:/Logs"; // Значение по умолчанию
            }

            // Создаём логгер
            var logger = new Loggers(logDirectory);
            builder.Bind<Loggers>().ToInstance(logger);

            // Регистрируем ViewModels
            builder.Bind<MainViewModel>().ToSelf();
            builder.Bind<RtlSwViewModel>().ToSelf();
            builder.Bind<RtlPoeViewModel>().ToSelf();
            builder.Bind<SettingsViewModel>().ToSelf();

            // Логируем успешную инициализацию
            logger.Log("Bootstrapper успешно запущен", Loggers.LogLevel.Success);
        }
    }
}
