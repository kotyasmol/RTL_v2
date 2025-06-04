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

        private bool _isReportRequired;
        [JsonProperty("server_report")]
        public bool IsReportRequired
        {
            get => _isReportRequired;
            set => SetAndNotify(ref _isReportRequired, value);
        }


        private bool _flashFirmwareAuto;
        [JsonProperty("flash_firmware_auto")]
        public bool FlashFirmwareAuto
        {
            get => _flashFirmwareAuto;
            set => SetAndNotify(ref _flashFirmwareAuto, value);
        }

        private string _flashXgproPath;
        [JsonProperty("flash_mpj_path")]
        public string FlashXgproPath
        {
            get => _flashXgproPath;
            set => SetAndNotify(ref _flashXgproPath, value);
        }
        

        private string _flashInstructionPath;
        [JsonProperty("flash_instruction_path")]
        public string FlashInstructionPath
        {
            get => _flashInstructionPath;
            set => SetAndNotify(ref _flashInstructionPath, value);
        }

        private int _flashDelay;
        [JsonProperty("flash_delay_seconds")]
        public int FlashDelay
        {
            get => _flashDelay;
            set => SetAndNotify(ref _flashDelay, value);
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

        private string _mcuBinPath;
        [JsonProperty("mcu_bin_path")]
        public string McuBinPath
        {
            get => _mcuBinPath;
            set => SetAndNotify(ref _mcuBinPath, value);
        }

        private string _mcuReadIdBatPath;
        [JsonProperty("mcu_read_id_bat_path")]
        public string McuReadIdBatPath
        {
            get => _mcuReadIdBatPath;
            set => SetAndNotify(ref _mcuReadIdBatPath, value);
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

        private int _uartCh1VoltageMin;
        [JsonProperty("uart_ch1_voltage_min")]
        public int UartCh1VoltageMin
        {
            get => _uartCh1VoltageMin;
            set => SetAndNotify(ref _uartCh1VoltageMin, value);
        }

        private int _uartCh1VoltageMax;
        [JsonProperty("uart_ch1_voltage_max")]
        public int UartCh1VoltageMax
        {
            get => _uartCh1VoltageMax;
            set => SetAndNotify(ref _uartCh1VoltageMax, value);
        }

        private int _uartCh2VoltageMin;
        [JsonProperty("uart_ch2_voltage_min")]
        public int UartCh2VoltageMin
        {
            get => _uartCh2VoltageMin;
            set => SetAndNotify(ref _uartCh2VoltageMin, value);
        }

        private int _uartCh2VoltageMax;
        [JsonProperty("uart_ch2_voltage_max")]
        public int UartCh2VoltageMax
        {
            get => _uartCh2VoltageMax;
            set => SetAndNotify(ref _uartCh2VoltageMax, value);
        }

        private int _uartCh3VoltageMin;
        [JsonProperty("uart_ch3_voltage_min")]
        public int UartCh3VoltageMin
        {
            get => _uartCh3VoltageMin;
            set => SetAndNotify(ref _uartCh3VoltageMin, value);
        }

        private int _uartCh3VoltageMax;
        [JsonProperty("uart_ch3_voltage_max")]
        public int UartCh3VoltageMax
        {
            get => _uartCh3VoltageMax;
            set => SetAndNotify(ref _uartCh3VoltageMax, value);
        }

        private int _uartCh4VoltageMin;
        [JsonProperty("uart_ch4_voltage_min")]
        public int UartCh4VoltageMin
        {
            get => _uartCh4VoltageMin;
            set => SetAndNotify(ref _uartCh4VoltageMin, value);
        }

        private int _uartCh4VoltageMax;
        [JsonProperty("uart_ch4_voltage_max")]
        public int UartCh4VoltageMax
        {
            get => _uartCh4VoltageMax;
            set => SetAndNotify(ref _uartCh4VoltageMax, value);
        }

        private int _uartCh5VoltageMin;
        [JsonProperty("uart_ch5_voltage_min")]
        public int UartCh5VoltageMin
        {
            get => _uartCh5VoltageMin;
            set => SetAndNotify(ref _uartCh5VoltageMin, value);
        }

        private int _uartCh5VoltageMax;
        [JsonProperty("uart_ch5_voltage_max")]
        public int UartCh5VoltageMax
        {
            get => _uartCh5VoltageMax;
            set => SetAndNotify(ref _uartCh5VoltageMax, value);
        }

        private int _uartCh6VoltageMin;
        [JsonProperty("uart_ch6_voltage_min")]
        public int UartCh6VoltageMin
        {
            get => _uartCh6VoltageMin;
            set => SetAndNotify(ref _uartCh6VoltageMin, value);
        }

        private int _uartCh6VoltageMax;
        [JsonProperty("uart_ch6_voltage_max")]
        public int UartCh6VoltageMax
        {
            get => _uartCh6VoltageMax;
            set => SetAndNotify(ref _uartCh6VoltageMax, value);
        }

        private int _uartCh7VoltageMin;
        [JsonProperty("uart_ch7_voltage_min")]
        public int UartCh7VoltageMin
        {
            get => _uartCh7VoltageMin;
            set => SetAndNotify(ref _uartCh7VoltageMin, value);
        }

        private int _uartCh7VoltageMax;
        [JsonProperty("uart_ch7_voltage_max")]
        public int UartCh7VoltageMax
        {
            get => _uartCh7VoltageMax;
            set => SetAndNotify(ref _uartCh7VoltageMax, value);
        }

        private int _uartCh8VoltageMin;
        [JsonProperty("uart_ch8_voltage_min")]
        public int UartCh8VoltageMin
        {
            get => _uartCh8VoltageMin;
            set => SetAndNotify(ref _uartCh8VoltageMin, value);
        }

        private int _uartCh8VoltageMax;
        [JsonProperty("uart_ch8_voltage_max")]
        public int UartCh8VoltageMax
        {
            get => _uartCh8VoltageMax;
            set => SetAndNotify(ref _uartCh8VoltageMax, value);
        }

        private int _uartCh9VoltageMin;
        [JsonProperty("uart_ch9_voltage_min")]
        public int UartCh9VoltageMin
        {
            get => _uartCh9VoltageMin;
            set => SetAndNotify(ref _uartCh9VoltageMin, value);
        }

        private int _uartCh9VoltageMax;
        [JsonProperty("uart_ch9_voltage_max")]
        public int UartCh9VoltageMax
        {
            get => _uartCh9VoltageMax;
            set => SetAndNotify(ref _uartCh9VoltageMax, value);
        }

        private int _uartCh10VoltageMin;
        [JsonProperty("uart_ch10_voltage_min")]
        public int UartCh10VoltageMin
        {
            get => _uartCh10VoltageMin;
            set => SetAndNotify(ref _uartCh10VoltageMin, value);
        }

        private int _uartCh10VoltageMax;
        [JsonProperty("uart_ch10_voltage_max")]
        public int UartCh10VoltageMax
        {
            get => _uartCh10VoltageMax;
            set => SetAndNotify(ref _uartCh10VoltageMax, value);
        }

        private int _uartCh11VoltageMin;
        [JsonProperty("uart_ch11_voltage_min")]
        public int UartCh11VoltageMin
        {
            get => _uartCh11VoltageMin;
            set => SetAndNotify(ref _uartCh11VoltageMin, value);
        }

        private int _uartCh11VoltageMax;
        [JsonProperty("uart_ch11_voltage_max")]
        public int UartCh11VoltageMax
        {
            get => _uartCh11VoltageMax;
            set => SetAndNotify(ref _uartCh11VoltageMax, value);
        }

        private int _uartCh12VoltageMin;
        [JsonProperty("uart_ch12_voltage_min")]
        public int UartCh12VoltageMin
        {
            get => _uartCh12VoltageMin;
            set => SetAndNotify(ref _uartCh12VoltageMin, value);
        }

        private int _uartCh12VoltageMax;
        [JsonProperty("uart_ch12_voltage_max")]
        public int UartCh12VoltageMax
        {
            get => _uartCh12VoltageMax;
            set => SetAndNotify(ref _uartCh12VoltageMax, value);
        }

        private int _uartCh13VoltageMin;
        [JsonProperty("uart_ch13_voltage_min")]
        public int UartCh13VoltageMin
        {
            get => _uartCh13VoltageMin;
            set => SetAndNotify(ref _uartCh13VoltageMin, value);
        }

        private int _uartCh13VoltageMax;
        [JsonProperty("uart_ch13_voltage_max")]
        public int UartCh13VoltageMax
        {
            get => _uartCh13VoltageMax;
            set => SetAndNotify(ref _uartCh13VoltageMax, value);
        }

        private int _uartCh14VoltageMin;
        [JsonProperty("uart_ch14_voltage_min")]
        public int UartCh14VoltageMin
        {
            get => _uartCh14VoltageMin;
            set => SetAndNotify(ref _uartCh14VoltageMin, value);
        }

        private int _uartCh14VoltageMax;
        [JsonProperty("uart_ch14_voltage_max")]
        public int UartCh14VoltageMax
        {
            get => _uartCh14VoltageMax;
            set => SetAndNotify(ref _uartCh14VoltageMax, value);
        }

        private int _uartCh15VoltageMin;
        [JsonProperty("uart_ch15_voltage_min")]
        public int UartCh15VoltageMin
        {
            get => _uartCh15VoltageMin;
            set => SetAndNotify(ref _uartCh15VoltageMin, value);
        }

        private int _uartCh15VoltageMax;
        [JsonProperty("uart_ch15_voltage_max")]
        public int UartCh15VoltageMax
        {
            get => _uartCh15VoltageMax;
            set => SetAndNotify(ref _uartCh15VoltageMax, value);
        }

        private int _uartCh16VoltageMin;
        [JsonProperty("uart_ch16_voltage_min")]
        public int UartCh16VoltageMin
        {
            get => _uartCh16VoltageMin;
            set => SetAndNotify(ref _uartCh16VoltageMin, value);
        }

        private int _uartCh16VoltageMax;
        [JsonProperty("uart_ch16_voltage_max")]
        public int UartCh16VoltageMax
        {
            get => _uartCh16VoltageMax;
            set => SetAndNotify(ref _uartCh16VoltageMax, value);
        }



    }
}
