using Stylet;
using StyletIoC;
using RTL.ViewModels;
using RTL.Logger;
using RTL.Properties;
using System.IO;
using RTL.Services;

namespace RTL
{
    public class Bootstrapper : Bootstrapper<MainViewModel>
    {
        protected override void ConfigureIoC(IStyletIoCBuilder builder)
        {
            string baseLogDir = Settings.Default.LogFolderPath;
            if (string.IsNullOrWhiteSpace(baseLogDir))
                baseLogDir = "C:/TFortisBoardLogs";

            // Создаём разные логгеры
            var systemLogger = new Loggers(Path.Combine(baseLogDir, "SystemLogs"));
            var swLogger = new Loggers(Path.Combine(baseLogDir, "SWLogs"));
            var poeLogger = new Loggers(Path.Combine(baseLogDir, "PoeLogs"));

            // Регистрируем их отдельно
            builder.Bind<Loggers>().ToInstance(systemLogger); // По умолчанию
            builder.Bind<Loggers>().WithKey("SW").ToInstance(swLogger);
            builder.Bind<Loggers>().WithKey("POE").ToInstance(poeLogger);

            // ViewModels — обычные бинды
            //builder.Bind<IFlashProgrammerService>().To<FlashProgrammerService>().InSingletonScope();
            builder.Bind<IFlashProgrammerService>().ToFactory(container =>
            {
                var poeLogger = container.Get<Loggers>("POE");
                return new FlashProgrammerService(poeLogger);
            }).InSingletonScope();

            builder.Bind<IMcuProgrammerService>().ToFactory(container =>
            {
                var poeLogger = container.Get<Loggers>("POE");
                return new McuProgrammerService(poeLogger);
            }).InSingletonScope();

            builder.Bind<ITestTimeoutService>().ToFactory(container =>
            {
                var poeLogger = container.Get<Loggers>("POE");
                return new TestTimeoutService(poeLogger);
            }).InSingletonScope();


            builder.Bind<RtlSwViewModel>().ToSelf();
            builder.Bind<RtlPoeViewModel>().ToSelf();
            builder.Bind<SettingsViewModel>().ToSelf();
            builder.Bind<MainViewModel>().ToSelf();

            systemLogger.Log("Bootstrapper успешно запущен", Loggers.LogLevel.Success);
        }
    }
}
