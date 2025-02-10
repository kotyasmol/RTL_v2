using RTL.Logger;
using Stylet;
using System;

namespace RTL.ViewModels
{
    public class RtlSwViewModel : Screen
    {
        private int _progress;
        public int Progress
        {
            get => _progress;
            set => SetAndNotify(ref _progress, value);
        }

        private BindableCollection<string> _logs = new();
        public BindableCollection<string> Logs
        {
            get => _logs;
            set => SetAndNotify(ref _logs, value);
        }

        private string _register52V;
        public string Register52V
        {
            get => _register52V;
            set => SetAndNotify(ref _register52V, value);
        }

        private readonly Loggers _logger;

        // 💡 Внедряем логгер через конструктор
        public RtlSwViewModel(Loggers logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger.LogToUser("RtlSwViewModel инициализирован", Loggers.LogLevel.Success);
            SomeAction();
        }

        public void SomeAction()
        {
            try
            {
                _logger.LogToUser("Начало выполнения SomeAction", Loggers.LogLevel.Info);

                // Здесь код действия
                Progress += 10;

                _logger.LogToUser("SomeAction выполнен успешно", Loggers.LogLevel.Success);
            }
            catch (Exception ex)
            {
                _logger.Log($"Ошибка в SomeAction: {ex.Message}", Loggers.LogLevel.Error);
            }
        }
    }
}
 