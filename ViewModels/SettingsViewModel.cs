using Stylet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RTL.ViewModels
{
    public class SettingsViewModel : Screen
    {
        private string _selectedComPort;
        public string SelectedComPort
        {
            get => _selectedComPort;
            set => SetAndNotify(ref _selectedComPort, value);
        }

        private string _selectedDutPort;
        public string SelectedDutPort
        {
            get => _selectedDutPort;
            set => SetAndNotify(ref _selectedDutPort, value);
        }

        private string _logsPath;
        public string LogsPath
        {
            get => _logsPath;
            set => SetAndNotify(ref _logsPath, value);
        }

        private bool _isDarkTheme;
        public bool IsDarkTheme
        {
            get => _isDarkTheme;
            set => SetAndNotify(ref _isDarkTheme, value);
        }

        public BindableCollection<string> AvailableComPorts { get; set; }
        public BindableCollection<string> AvailableDutPorts { get; set; }

        public SettingsViewModel()
        {
            // Инициализация доступных портов и настроек
        }
    }
}