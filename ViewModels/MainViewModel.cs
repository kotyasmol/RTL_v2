using Stylet;
using RTL.ViewModels;

namespace RTL.ViewModels
{
    public class MainViewModel : Conductor<IScreen>.Collection.OneActive, IDisposable
    {
        private readonly RtlSwViewModel _rtlSwViewModel;
        private readonly SettingsViewModel _settingsViewModel;

        public MainViewModel(RtlSwViewModel rtlSwViewModel, SettingsViewModel settingsViewModel)
        {
            _rtlSwViewModel = rtlSwViewModel ?? throw new ArgumentNullException(nameof(rtlSwViewModel));
            _settingsViewModel = settingsViewModel ?? throw new ArgumentNullException(nameof(settingsViewModel));

            // Добавляем экраны в коллекцию
            Items.Add(_rtlSwViewModel);
            Items.Add(_settingsViewModel);

            // По умолчанию открываем RTL-SW
            ActivateItem(_rtlSwViewModel);
        }

        // Команды для навигации
        public void NavigateToRtlSw()
        {
            ActivateItem(_rtlSwViewModel);
        }

        public void NavigateToSettings()
        {
            ActivateItem(_settingsViewModel);
        }

        public void Dispose()
        {
            // Очистка ресурсов, если необходимо
        }
    }
}