using Stylet;
using StyletIoC;
using RTL.ViewModels;
using RTL.Logger;

namespace RTL
{
    public class Bootstrapper : Stylet.Bootstrapper<MainViewModel>
    {
        protected override void ConfigureIoC(IStyletIoCBuilder builder)
        {
            // Создаём логгер и регистрируем его как singleton
            var logger = new Loggers(Properties.Settings.Default.LogFolderPath);
            builder.Bind<Loggers>().ToInstance(logger);

            // Регистрируем ViewModels
            builder.Bind<MainViewModel>().ToSelf();
            builder.Bind<RtlSwViewModel>().ToSelf();
            builder.Bind<RtlPoeViewModel>().ToSelf();
            builder.Bind<SettingsViewModel>().ToSelf();
        }
    }
}
