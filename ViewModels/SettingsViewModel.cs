using Stylet;
using System.Collections.Generic;

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
                SetAndNotify(ref _selectedTheme, value);
                ApplyTheme(value);
            }
        }

        public List<string> Themes { get; set; } = new List<string> { "Светлая", "Темная" };

        public SettingsViewModel()
        {
            // Загрузите сохраненную тему из настроек
            SelectedTheme = Properties.Settings.Default.Theme;
        }

        private void ApplyTheme(string theme)
        {
            // Примените тему и сохраните настройки
            Properties.Settings.Default.Theme = theme;
            Properties.Settings.Default.Save();

            // Логика применения темы (например, через HandyControl)
        }
    }
}