using Stylet;
using RTL.ViewModels;

namespace RTL.ViewModels
{
    public class MainViewModel : Conductor<IScreen>.Collection.OneActive, IDisposable
    {
        private readonly RtlSwViewModel _rtlSwViewModel;
        private readonly SettingsViewModel _settingsViewModel;
        private readonly RtlPoeViewModel _rtlPoeViewModel;

        public MainViewModel(RtlSwViewModel rtlSwViewModel, RtlPoeViewModel rtlPoeViewModel, SettingsViewModel settingsViewModel)
        {
            _rtlSwViewModel = rtlSwViewModel ?? throw new ArgumentNullException(nameof(rtlSwViewModel));
            _settingsViewModel = settingsViewModel ?? throw new ArgumentNullException(nameof(settingsViewModel));
            _rtlPoeViewModel = rtlPoeViewModel ?? throw new ArgumentNullException(nameof(rtlPoeViewModel));
            // Добавляем экраны в коллекцию
            Items.Add(_rtlSwViewModel);
            Items.Add(_settingsViewModel);
            Items.Add(_rtlPoeViewModel);

            // По умолчанию открываем SETTINGS
            ActivateItem(_settingsViewModel);
        }

        // Команды для навигации
        public void NavigateToRtlSw()
        {
            ActivateItem(_rtlSwViewModel);
        }
        public void NavigateToRtlPoe()
        {
            ActivateItem(_rtlPoeViewModel);
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