
using Stylet;
using RTL.ViewModels;
using StyletIoC;


namespace RTL
{
    public class Bootstrapper : Bootstrapper<MainViewModel>
    {
        protected override void ConfigureIoC(IStyletIoCBuilder builder)
        {
            // Регистрируем ViewModels
            builder.Bind<MainViewModel>().ToSelf();
            builder.Bind<RtlSwViewModel>().ToSelf();
            builder.Bind<SettingsViewModel>().ToSelf();

        }
    }
}