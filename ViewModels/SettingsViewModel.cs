using HandyControl.Themes;
using HandyControl.Tools;
using Stylet;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using Microsoft.WindowsAPICodePack.Dialogs;
using Microsoft.Win32; // Для выбора файлов
using RTL.Commands;

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

        private string _reportFolderPath;
        public string ReportFolderPath
        {
            get => _reportFolderPath;
            set
            {
                if (SetAndNotify(ref _reportFolderPath, value))
                {
                    Properties.Settings.Default.ReportFolderPath = value;
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

        public List<string> Themes { get; set; } = new List<string> { "Светлая", "Темная" };

        // Команды
        public RelayCommand SelectLogFolderCommand { get; }
        public RelayCommand SelectReportFolderCommand { get; }
        public RelayCommand SelectRtlSwProfileCommand { get; }
        public RelayCommand SelectRtlPoeProfileCommand { get; }

        public SettingsViewModel()
        {
            // Загружаем сохранённые настройки
            SelectedTheme = Properties.Settings.Default.Theme;
            ApplyTheme(SelectedTheme);

            // Устанавливаем пути по умолчанию
            LogFolderPath = string.IsNullOrEmpty(Properties.Settings.Default.LogFolderPath)
                ? Path.Combine(Directory.GetCurrentDirectory(), "Logs")
                : Properties.Settings.Default.LogFolderPath;

            ReportFolderPath = string.IsNullOrEmpty(Properties.Settings.Default.ReportFolderPath)
                ? Path.Combine(Directory.GetCurrentDirectory(), "Reports")
                : Properties.Settings.Default.ReportFolderPath;

            RtlSwProfilePath = Properties.Settings.Default.RtlSwProfilePath ?? string.Empty;
            RtlPoeProfilePath = Properties.Settings.Default.RtlPoeProfilePath ?? string.Empty;

            // Создаём команды
            SelectLogFolderCommand = new RelayCommand(SelectLogFolder);
            SelectReportFolderCommand = new RelayCommand(SelectReportFolder);
            SelectRtlSwProfileCommand = new RelayCommand(SelectRtlSwProfile);
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

        private void SelectReportFolder()
        {
            var dialog = new CommonOpenFileDialog { IsFolderPicker = true, InitialDirectory = ReportFolderPath };
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                ReportFolderPath = dialog.FileName;
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
    }
}
