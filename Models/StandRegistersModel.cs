using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace RTL.Models
{
    public class StandRegistersModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private bool SetAndNotify<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        // 1. Серийный номер стенда
        private ushort _standSerialNumber;
        public ushort StandSerialNumber
        {
            get => _standSerialNumber;
            set => SetAndNotify(ref _standSerialNumber, value);
        }

        // 10-18. Показатели напряжений
        private ushort _v52;
        public ushort V52
        {
            get => _v52;
            set => SetAndNotify(ref _v52, value);
        }

        private ushort _v55;
        public ushort V55
        {
            get => _v55;
            set => SetAndNotify(ref _v55, value);
        }

        private ushort _vOut;
        public ushort VOut
        {
            get => _vOut;
            set => SetAndNotify(ref _vOut, value);
        }

        private ushort _ref2048;
        public ushort Ref2048
        {
            get => _ref2048;
            set => SetAndNotify(ref _ref2048, value);
        }

        private ushort _v12;
        public ushort V12
        {
            get => _v12;
            set => SetAndNotify(ref _v12, value);
        }
    }
}
