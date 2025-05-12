using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;

namespace RTL.Models
{
    public class PoeTestProfileModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        private bool SetAndNotify<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        private string _modelName;
        [JsonProperty("model_name")]
        public string ModelName
        {
            get => _modelName;
            set => SetAndNotify(ref _modelName, value);
        }

        private int _boardVersion;
        [JsonProperty("board_version")]
        public int BoardVersion
        {
            get => _boardVersion;
            set => SetAndNotify(ref _boardVersion, value);
        }

        private bool _flashFirmwareAuto;
        [JsonProperty("flash_firmware_auto")]
        public bool FlashFirmwareAuto
        {
            get => _flashFirmwareAuto;
            set => SetAndNotify(ref _flashFirmwareAuto, value);
        }

        private string _flashXgproPath;
        [JsonProperty("flash_xgpro_path")]
        public string FlashXgproPath
        {
            get => _flashXgproPath;
            set => SetAndNotify(ref _flashXgproPath, value);
        }

        private bool _mcuFirmwareAuto;
        [JsonProperty("mcu_firmware_auto")]
        public bool McuFirmwareAuto
        {
            get => _mcuFirmwareAuto;
            set => SetAndNotify(ref _mcuFirmwareAuto, value);
        }

        private string _mcuBatPath;
        [JsonProperty("mcu_bat_path")]
        public string McuBatPath
        {
            get => _mcuBatPath;
            set => SetAndNotify(ref _mcuBatPath, value);
        }

        private bool _is3v3TestRequired;
        [JsonProperty("3.3_test")]
        public bool Is3v3TestRequired
        {
            get => _is3v3TestRequired;
            set => SetAndNotify(ref _is3v3TestRequired, value);
        }

        private int _v3v3Min;
        [JsonProperty("3.3VMin")]
        public int V3v3Min
        {
            get => _v3v3Min;
            set => SetAndNotify(ref _v3v3Min, value);
        }

        private int _v3v3Max;
        [JsonProperty("3.3Vmax")]
        public int V3v3Max
        {
            get => _v3v3Max;
            set => SetAndNotify(ref _v3v3Max, value);
        }

        private bool _isPoeTestRequired;
        [JsonProperty("poe_test")]
        public bool IsPoeTestRequired
        {
            get => _isPoeTestRequired;
            set => SetAndNotify(ref _isPoeTestRequired, value);
        }

        private bool _isLedTestRequired;
        [JsonProperty("led_test")]
        public bool IsLedTestRequired
        {
            get => _isLedTestRequired;
            set => SetAndNotify(ref _isLedTestRequired, value);
        }

        private string _ledColour;
        [JsonProperty("led_colour")]
        public string LedColour
        {
            get => _ledColour;
            set => SetAndNotify(ref _ledColour, value);
        }

        private bool _isUartTestRequired;
        [JsonProperty("uart_test")]
        public bool IsUartTestRequired
        {
            get => _isUartTestRequired;
            set => SetAndNotify(ref _isUartTestRequired, value);
        }
    }
}
