using Stylet;
using RTL.ViewModels;

namespace RTL.ViewModels
{
    public class MainViewModel : Screen
    {
        private readonly RtlSwViewModel _rtlSwViewModel;
        private readonly RtlPoeViewModel _rtlPoeViewModel;
        private readonly SettingsViewModel _settingsViewModel;
        private readonly ModbusViewModel _modbusViewModel;

        private object _currentView;
        public object CurrentView
        {
            get => _currentView;
            set => SetAndNotify(ref _currentView, value);
        }

        /* public MainViewModel(RtlSwViewModel rtlSwViewModel,
                             RtlPoeViewModel rtlPoeViewModel,
                             SettingsViewModel settingsViewModel,
                             ModbusViewModel modbusViewModel)
         {
             _rtlSwViewModel = rtlSwViewModel;
             _rtlPoeViewModel = rtlPoeViewModel;
             _settingsViewModel = settingsViewModel;
             _modbusViewModel = modbusViewModel;

             // По умолчанию открываем RTL-SW
             CurrentView = _rtlSwViewModel;
         }*/
        public MainViewModel(RtlSwViewModel rtlSwViewModel,
                     
                     SettingsViewModel settingsViewModel)

        {
            _rtlSwViewModel = rtlSwViewModel;

            _settingsViewModel = settingsViewModel;


            // По умолчанию открываем RTL-SW
            CurrentView = _rtlSwViewModel;
        }

        // Команды для навигации
        public void NavigateToRtlSw() => CurrentView = _rtlSwViewModel;
        public void NavigateToRtlPoe() => CurrentView = _rtlPoeViewModel;
        public void NavigateToSettings() => CurrentView = _settingsViewModel;
        public void NavigateToModbus() => CurrentView = _modbusViewModel;

        // Статусные свойства
        private bool _isModbusConnected;
        public bool IsModbusConnected
        {
            get => _isModbusConnected;
            set
            {
                SetAndNotify(ref _isModbusConnected, value);
                NotifyOfPropertyChange(nameof(ModbusStatusColor));
            }
        }

        public string ModbusStatusColor => IsModbusConnected ? "GreenYellow" : "Tomato";

        private bool _isServerConnected;
        public bool IsServerConnected
        {
            get => _isServerConnected;
            set
            {
                SetAndNotify(ref _isServerConnected, value);
                NotifyOfPropertyChange(nameof(ServerStatusColor));
            }
        }

        public string ServerStatusColor => IsServerConnected ? "GreenYellow" : "Tomato";

        private bool _isDutConnected;
        public bool IsDutConnected
        {
            get => _isDutConnected;
            set
            {
                SetAndNotify(ref _isDutConnected, value);
                NotifyOfPropertyChange(nameof(DutStatusColor));
            }
        }

        public string DutStatusColor => IsDutConnected ? "GreenYellow" : "Tomato";

        private bool _isRunActive;
        public bool IsRunActive
        {
            get => _isRunActive;
            set
            {
                SetAndNotify(ref _isRunActive, value);
                NotifyOfPropertyChange(nameof(RunStatusColor));
            }
        }

        public string RunStatusColor => IsRunActive ? "GreenYellow" : "Tomato";



    }
}