using Stylet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        private BindableCollection<string> _logs;
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

        // Повторить для остальных регистров

        public RtlSwViewModel()
        {
            Logs = new BindableCollection<string>();
            // Инициализация регистров
        }
    }
}
