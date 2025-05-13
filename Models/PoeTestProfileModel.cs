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

        private int _startupTime;
        [JsonProperty("startup_time")]
        public int StartUpTime
        {
            get => _startupTime;
            set => SetAndNotify(ref _startupTime, value);
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

        private bool _isBoardVersionCheckEnabled;
        [JsonProperty("board_version_test")]
        public bool IsBoardVersionCheckEnabled
        {
            get => _isBoardVersionCheckEnabled;
            set => SetAndNotify(ref _isBoardVersionCheckEnabled, value);
        }

        private int _boardVersion;
        [JsonProperty("board_version")]
        public int BoardVersion
        {
            get => _boardVersion;
            set => SetAndNotify(ref _boardVersion, value);
        }

        private bool _isPoeTestRequired;
        [JsonProperty("poe_test")]
        public bool IsPoeTestRequired
        {
            get => _isPoeTestRequired;
            set => SetAndNotify(ref _isPoeTestRequired, value);
        }

        private bool _isPort1TestEnabled;
        [JsonProperty("port1_test")]
        public bool IsPort1TestEnabled
        {
            get => _isPort1TestEnabled;
            set => SetAndNotify(ref _isPort1TestEnabled, value);
        }

        private bool _isPort2TestEnabled;
        [JsonProperty("port2_test")]
        public bool IsPort2TestEnabled
        {
            get => _isPort2TestEnabled;
            set => SetAndNotify(ref _isPort2TestEnabled, value);
        }

        private bool _isPort3TestEnabled;
        [JsonProperty("port3_test")]
        public bool IsPort3TestEnabled
        {
            get => _isPort3TestEnabled;
            set => SetAndNotify(ref _isPort3TestEnabled, value);
        }

        private bool _isPort4TestEnabled;
        [JsonProperty("port4_test")]
        public bool IsPort4TestEnabled
        {
            get => _isPort4TestEnabled;
            set => SetAndNotify(ref _isPort4TestEnabled, value);
        }

        private bool _isPort5TestEnabled;
        [JsonProperty("port5_test")]
        public bool IsPort5TestEnabled
        {
            get => _isPort5TestEnabled;
            set => SetAndNotify(ref _isPort5TestEnabled, value);
        }

        private bool _isPort6TestEnabled;
        [JsonProperty("port6_test")]
        public bool IsPort6TestEnabled
        {
            get => _isPort6TestEnabled;
            set => SetAndNotify(ref _isPort6TestEnabled, value);
        }

        private bool _isPort7TestEnabled;
        [JsonProperty("port7_test")]
        public bool IsPort7TestEnabled
        {
            get => _isPort7TestEnabled;
            set => SetAndNotify(ref _isPort7TestEnabled, value);
        }

        private bool _isPort8TestEnabled;
        [JsonProperty("port8_test")]
        public bool IsPort8TestEnabled
        {
            get => _isPort8TestEnabled;
            set => SetAndNotify(ref _isPort8TestEnabled, value);
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
