using HandyControl.Themes;
using HandyControl.Tools;
using Stylet;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using Microsoft.WindowsAPICodePack.Dialogs;
using Microsoft.Win32; // Для выбора файлов
using RTL.Commands;
using RTL.Logger;
using Serilog.Core;
using System.Collections.ObjectModel;
using System.IO.Ports;
using System.Windows.Input;

namespace RTL.ViewModels
{
    public class SettingsViewModel : Screen
    {
        private string _selectedTheme;
        public string SelectedTheme
        {
            get => _selectedTheme;
            set
            {
                if (SetAndNotify(ref _selectedTheme, value))
                {
                    ApplyTheme(value);
                }
            }
        }

        private string _logFolderPath;
        public string LogFolderPath
        {
            get => _logFolderPath;
            set
            {
                if (SetAndNotify(ref _logFolderPath, value))
                {
                    Properties.Settings.Default.LogFolderPath = value;
                    Properties.Settings.Default.Save();
                }
            }
        }



        private string _rtlSwProfilePath;
        public string RtlSwProfilePath
        {
            get => _rtlSwProfilePath;
            set
            {
                if (SetAndNotify(ref _rtlSwProfilePath, value))
                {
                    Properties.Settings.Default.RtlSwProfilePath = value;
                    Properties.Settings.Default.Save();
                }
            }
        }


        private string _flashProgramPath;
        public string FlashProgramPath
        {
            get => _flashProgramPath;
            set
            {
                if (SetAndNotify(ref _flashProgramPath, value))
                {
                    Properties.Settings.Default.FlashProgramPath = value;
                    Properties.Settings.Default.Save();
                }
            }
        }

        private string _flashFirmwarePath;
        public string FlashFirmwarePath
        {
            get => _flashFirmwarePath;
            set
            {
                if (SetAndNotify(ref _flashFirmwarePath, value))
                {
                    Properties.Settings.Default.FlashFirmwarePath = value;
                    Properties.Settings.Default.Save();
                }
            }
        }

        private string _swdProgramPath;
        public string SwdProgramPath
        {
            get => _swdProgramPath;
            set
            {
                if (SetAndNotify(ref _swdProgramPath, value))
                {
                    Properties.Settings.Default.SwdProgramPath = value;
                    Properties.Settings.Default.Save();
                }
            }
        }

        private string _swdFirmwarePath;
        public string SwdFirmwarePath
        {
            get => _swdFirmwarePath;
            set
            {
                if (SetAndNotify(ref _swdFirmwarePath, value))
                {
                    Properties.Settings.Default.SwdFirmwarePath = value;
                    Properties.Settings.Default.Save();
                }
            }
        }




        private string _rtlPoeProfilePath;
        public string RtlPoeProfilePath
        {
            get => _rtlPoeProfilePath;
            set
            {
                if (SetAndNotify(ref _rtlPoeProfilePath, value))
                {
                    Properties.Settings.Default.RtlPoeProfilePath = value;
                    Properties.Settings.Default.Save();
                }
            }
        }





        private ObservableCollection<string> _availablePorts = new();
        public ObservableCollection<string> AvailablePorts
        {
            get => _availablePorts;
            set => SetAndNotify(ref _availablePorts, value);
        }

        private string _selectedComPort;
        public string SelectedComPort
        {
            get => _selectedComPort;
            set
            {
                SetAndNotify(ref _selectedComPort, value);
                Properties.Settings.Default.ComSW = value;
                Properties.Settings.Default.Save();

            }
        }

        private string _selectedDutPort;
        public string SelectedDutPort
        {
            get => _selectedDutPort;
            set
            {
                SetAndNotify(ref _selectedDutPort, value);
                Properties.Settings.Default.DutSW = value;
                Properties.Settings.Default.Save();

            }
        }


        public List<string> Themes { get; set; } = new List<string> { "Светлая", "Темная" };

        // Команды
        public RelayCommand SelectLogFolderCommand { get; }
        public RelayCommand SelectReportFolderCommand { get; }
        public RelayCommand SelectRtlSwProfileCommand { get; }
        public RelayCommand SelectRtlPoeProfileCommand { get; }
        public RelayCommand SelectFlashProgramCommand { get; }
        public RelayCommand SelectFlashFirmwareCommand { get; }
        public RelayCommand SelectSwdProgramCommand { get; }
        public RelayCommand SelectSwdFirmwareCommand { get; }
        

        public ICommand RefreshPortsCommand { get; }

        public SettingsViewModel()
        {
            // Загружаем сохранённые настройки
            SelectedTheme = Properties.Settings.Default.Theme;


            // Создаём команды
            ApplyTheme(SelectedTheme);

            // Устанавливаем пути по умолчанию
            LogFolderPath = string.IsNullOrEmpty(Properties.Settings.Default.LogFolderPath)
                ? Path.Combine(Directory.GetCurrentDirectory(), "TFortisBoardLogs")
                : Properties.Settings.Default.LogFolderPath;



            RtlSwProfilePath = Properties.Settings.Default.RtlSwProfilePath ?? string.Empty;
            RtlPoeProfilePath = Properties.Settings.Default.RtlPoeProfilePath ?? string.Empty;
            FlashProgramPath = Properties.Settings.Default.FlashProgramPath ?? string.Empty;
            FlashFirmwarePath = Properties.Settings.Default.FlashFirmwarePath ?? string.Empty;
            SwdProgramPath = Properties.Settings.Default.SwdProgramPath ?? string.Empty;
            SwdFirmwarePath = Properties.Settings.Default.SwdFirmwarePath ?? string.Empty;

            // Создаём команды
            SelectLogFolderCommand = new RelayCommand(SelectLogFolder);
            SelectRtlSwProfileCommand = new RelayCommand(SelectRtlSwProfile);
            SelectFlashProgramCommand = new RelayCommand(SelectFlashProgram);
            SelectFlashFirmwareCommand = new RelayCommand(SelectFlashFirmware);
            SelectSwdProgramCommand = new RelayCommand(SelectSwdProgram);
            SelectSwdFirmwareCommand = new RelayCommand(SelectSwdFirmwarePath);

            RefreshPortsCommand = new RelayCommand(LoadAvailablePorts);
            LoadAvailablePorts();

            SelectedComPort = Properties.Settings.Default.ComSW;
            SelectedDutPort = Properties.Settings.Default.DutSW;











            SelectRtlPoeProfileCommand = new RelayCommand(SelectRtlPoeProfile);

        }

        private void ApplyTheme(string theme)
        {
            Properties.Settings.Default.Theme = theme;
            Properties.Settings.Default.Save();

            var isDark = theme == "Темная";
            ThemeManager.Current.ApplicationTheme = isDark ? ApplicationTheme.Dark : ApplicationTheme.Light;
        }


        private void SelectLogFolder()
        {
            var dialog = new CommonOpenFileDialog { IsFolderPicker = true, InitialDirectory = LogFolderPath };
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                LogFolderPath = dialog.FileName;
            }
        }



        private void SelectRtlSwProfile()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "JSON файлы (*.json)|*.json",
                Title = "Выберите профиль RTL-SW"
            };
            if (dialog.ShowDialog() == true)
            {
                RtlSwProfilePath = dialog.FileName;
            }
        }

        private void SelectRtlPoeProfile()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "JSON файлы (*.json)|*.json",
                Title = "Выберите профиль RTL-POE"
            };
            if (dialog.ShowDialog() == true)
            {
                RtlPoeProfilePath = dialog.FileName;
            }
        }
        private void SelectFlashProgram()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Исполняемые файлы (*.exe)|*.exe",
                Title = "Выберите программу для прошивки (XGecu.exe)"
            };
            if (dialog.ShowDialog() == true)
            {
                FlashProgramPath = dialog.FileName;
            }
        }

        private void SelectFlashFirmware()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Файлы прошивки (*.mpj)|*.mpj",
                Title = "Выберите файл прошивки FLASH"
            };
            if (dialog.ShowDialog() == true)
            {
                FlashFirmwarePath = dialog.FileName;
            }
        }

        private void SelectSwdProgram()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Все файлы (*.*)|*.*",
                Title = "Выберите программу для прошивки SWD"
            };
            if (dialog.ShowDialog() == true)
            {
                SwdProgramPath = dialog.FileName;
            }
        }


        private void SelectSwdFirmwarePath()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Все файлы (*.*)|*.*",
                Title = "Выберите программу для прошивки SWD"
            };
            if (dialog.ShowDialog() == true)
            {
                SwdFirmwarePath = dialog.FileName;
            }
        }

        public void LoadAvailablePorts()
        {
            AvailablePorts = new ObservableCollection<string>(SerialPort.GetPortNames().ToList());

            // Восстанавливаем сохранённые порты
            if (!string.IsNullOrEmpty(Properties.Settings.Default.ComSW) && AvailablePorts.Contains(Properties.Settings.Default.ComSW))
                SelectedComPort = Properties.Settings.Default.ComSW;

            if (!string.IsNullOrEmpty(Properties.Settings.Default.DutSW) && AvailablePorts.Contains(Properties.Settings.Default.DutSW))
                SelectedDutPort = Properties.Settings.Default.DutSW;
        }

    }
}
