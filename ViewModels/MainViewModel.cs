using Stylet;
using RTL.Logger;
using RTL.ViewModels;

namespace RTL.ViewModels
{
    public class MainViewModel : Conductor<IScreen>.Collection.OneActive
    {
        private readonly Loggers _logger;
        private readonly RtlSwViewModel _rtlSwViewModel;
        private readonly RtlPoeViewModel _rtlPoeViewModel;
        private readonly SettingsViewModel _settingsViewModel;

        public MainViewModel(Loggers logger, RtlSwViewModel rtlSwViewModel, RtlPoeViewModel rtlPoeViewModel, SettingsViewModel settingsViewModel)
        {
            _logger = logger;
            _rtlSwViewModel = rtlSwViewModel;
            _settingsViewModel = settingsViewModel;
            _rtlPoeViewModel = rtlPoeViewModel;

            // Добавляем экраны в коллекцию
            Items.Add(_rtlSwViewModel);
            Items.Add(_settingsViewModel);
            Items.Add(_rtlPoeViewModel);

            // По умолчанию открываем настройки
            ActivateItem(_settingsViewModel);

            // Логируем создание MainViewModel
            _logger.Log("MainViewModel инициализирован", Loggers.LogLevel.Info);
        }

        public void NavigateToRtlSw() => ActivateItem(_rtlSwViewModel);
        public void NavigateToRtlPoe() => ActivateItem(_rtlPoeViewModel);
        public void NavigateToSettings() => ActivateItem(_settingsViewModel);
    }
}
