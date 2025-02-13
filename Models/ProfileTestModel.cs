using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Stylet;


namespace RTL.Models
{
    public class ProfileTestModel : INotifyPropertyChanged
    {
        [JsonProperty("model_name")]
        public string ModelName { get; set; }

        [JsonProperty("model_type")]
        public int ModelType { get; set; }


        // 1. K5 Test
        [JsonProperty("1.Проверка узла K5")]
        public bool IsK5TestEnabled { get; set; }

        [JsonProperty("k5_test")]
        public int K5Test { get; set; }

        [JsonProperty("k5_start_delay")]
        public int K5StartDelay { get; set; }

        [JsonProperty("k5_test_delay")]
        public int K5TestDelay { get; set; }








        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

            // Вызов событий при изменении значений
            switch (propertyName)
            {
                case nameof(K5_52V_Min):
                    K5_52V_MinChanged?.Invoke(this, EventArgs.Empty);
                    break;
                case nameof(K5_52V_Max):
                    K5_52V_MaxChanged?.Invoke(this, EventArgs.Empty);
                    break;
                case nameof(K5_55V_Min):
                    K5_55V_MinChanged?.Invoke(this, EventArgs.Empty);
                    break;
                case nameof(K5_55V_Max):
                    K5_55V_MaxChanged?.Invoke(this, EventArgs.Empty);
                    break;
                case nameof(V12_Min):
                    V12_MinChanged?.Invoke(this, EventArgs.Empty);
                    break;
                case nameof(V12_Max):
                    V12_MaxChanged?.Invoke(this, EventArgs.Empty);
                    break;
                case nameof(VoutVMainMin): VoutVMainMinChanged?.Invoke(this, EventArgs.Empty); break;
                case nameof(VoutVMainMax): VoutVMainMaxChanged?.Invoke(this, EventArgs.Empty); break;
                case nameof(VoutVResMin): VoutVResMinChanged?.Invoke(this, EventArgs.Empty); break;
                case nameof(VoutVResMax): VoutVResMaxChanged?.Invoke(this, EventArgs.Empty); break;
                case nameof(VRefMin): VRefMinChanged?.Invoke(this, EventArgs.Empty); break;
                case nameof(VRefMax): VRefMaxChanged?.Invoke(this, EventArgs.Empty); break;
            }
        }

        private bool SetAndNotify<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        // События для каждого изменяемого свойства
        public event EventHandler K5_52V_MinChanged;
        public event EventHandler K5_52V_MaxChanged;
        public event EventHandler K5_55V_MinChanged;
        public event EventHandler K5_55V_MaxChanged;
        public event EventHandler V12_MinChanged;
        public event EventHandler V12_MaxChanged;
        public event EventHandler VoutVMainMinChanged;
        public event EventHandler VoutVMainMaxChanged;
        public event EventHandler VoutVResMinChanged;
        public event EventHandler VoutVResMaxChanged;
        public event EventHandler VRefMinChanged;
        public event EventHandler VRefMaxChanged;

        public event EventHandler Vcc3V3MinChanged;
        public event EventHandler Vcc3V3MaxChanged;
        public event EventHandler Vcc1V5MinChanged;
        public event EventHandler Vcc1V5MaxChanged;
        public event EventHandler Vcc1V1MinChanged;
        public event EventHandler Vcc1V1MaxChanged;
        public event EventHandler CR2032MinChanged;
        public event EventHandler CR2032MaxChanged;
        public event EventHandler CR2032CpuMinChanged;
        public event EventHandler CR2032CpuMaxChanged;

        // Поля и свойства с уведомлениями
        private ushort _k5_52V_Min;
        private ushort _k5_52V_Max;
        private ushort _k5_55V_Min;
        private ushort _k5_55V_Max;
        private ushort _v12_Min;
        private ushort _v12_Max;
        private ushort _voutVMainMin;
        private int _voutVMainMax;
        private int _voutVResMin;
        private int _voutVResMax;
        private ushort _vRefMin;
        private int _vRefMax;
        private ushort _vcc3V3Min;
        private ushort _vcc3V3Max;
        private ushort _vcc1V5Min;
        private ushort _vcc1V5Max;
        private ushort _vcc1V1Min;
        private ushort _vcc1V1Max;
        private ushort _cr2032Min;
        private ushort _cr2032Max;
        private ushort _cr2032CpuMin;
        private ushort _cr2032CpuMax;


        [JsonProperty("k5_52V_min")]
        public ushort K5_52V_Min
        {
            get => _k5_52V_Min;
            set => SetAndNotify(ref _k5_52V_Min, value);
        }

        [JsonProperty("k5_52V_max")]
        public ushort K5_52V_Max
        {
            get => _k5_52V_Max;
            set => SetAndNotify(ref _k5_52V_Max, value);
        }

        [JsonProperty("k5_55V_min")]
        public ushort K5_55V_Min
        {
            get => _k5_55V_Min;
            set => SetAndNotify(ref _k5_55V_Min, value);
        }

        [JsonProperty("k5_55V_max")]
        public ushort K5_55V_Max
        {
            get => _k5_55V_Max;
            set => SetAndNotify(ref _k5_55V_Max, value);
        }

        [JsonProperty("v12_min")]
        public ushort V12_Min
        {
            get => _v12_Min;
            set => SetAndNotify(ref _v12_Min, value);
        }

        [JsonProperty("v12_max")]
        public ushort V12_Max
        {
            get => _v12_Max;
            set => SetAndNotify(ref _v12_Max, value);
        }



        [JsonProperty("vout_vmain_min")]
        public ushort VoutVMainMin
        {
            get => _voutVMainMin;
            set => SetAndNotify(ref _voutVMainMin, value);
        }

        [JsonProperty("vout_vmain_max")]
        public int VoutVMainMax
        {
            get => _voutVMainMax;
            set => SetAndNotify(ref _voutVMainMax, value);
        }

        [JsonProperty("vout_vres_min")]
        public int VoutVResMin
        {
            get => _voutVResMin;
            set => SetAndNotify(ref _voutVResMin, value);
        }

        [JsonProperty("vout_vres_max")]
        public int VoutVResMax
        {
            get => _voutVResMax;
            set => SetAndNotify(ref _voutVResMax, value);
        }

        [JsonProperty("vref_min")]
        public ushort VRefMin
        {
            get => _vRefMin;
            set => SetAndNotify(ref _vRefMin, value);
        }

        [JsonProperty("vref_max")]
        public int VRefMax
        {
            get => _vRefMax;
            set => SetAndNotify(ref _vRefMax, value);
        }




        // 2. VCC Test
        [JsonProperty("2.Проверка VCC")]
        public bool IsVccTestEnabled { get; set; }

        [JsonProperty("vcc_test")]
        public ushort VccTest { get; set; }

        [JsonProperty("vcc_start_delay")]
        public ushort VccStartDelay { get; set; }

        [JsonProperty("vcc_3V3_min")]
        public ushort Vcc3V3Min
        {
            get => _vcc3V3Min;
            set => SetAndNotify(ref _vcc3V3Min, value);
        }

        [JsonProperty("vcc_3V3_max")]
        public ushort Vcc3V3Max
        {
            get => _vcc3V3Max;
            set => SetAndNotify(ref _vcc3V3Max, value);
        }

        [JsonProperty("vcc_1V5_min")]
        public ushort Vcc1V5Min
        {
            get => _vcc1V5Min;
            set => SetAndNotify(ref _vcc1V5Min, value);
        }

        [JsonProperty("vcc_1V5_max")]
        public ushort Vcc1V5Max
        {
            get => _vcc1V5Max;
            set => SetAndNotify(ref _vcc1V5Max, value);
        }

        [JsonProperty("vcc_1V1_min")]
        public ushort Vcc1V1Min
        {
            get => _vcc1V1Min;
            set => SetAndNotify(ref _vcc1V1Min, value);
        }

        [JsonProperty("vcc_1V1_max")]
        public ushort Vcc1V1Max
        {
            get => _vcc1V1Max;
            set => SetAndNotify(ref _vcc1V1Max, value);
        }

        [JsonProperty("cr2032_min")]
        public ushort CR2032Min
        {
            get => _cr2032Min;
            set => SetAndNotify(ref _cr2032Min, value);
        }

        [JsonProperty("cr2032_max")]
        public ushort CR2032Max
        {
            get => _cr2032Max;
            set => SetAndNotify(ref _cr2032Max, value);
        }

        [JsonProperty("cr2032_cpu_min")]
        public ushort CR2032CpuMin
        {
            get => _cr2032CpuMin;
            set => SetAndNotify(ref _cr2032CpuMin, value);
        }

        [JsonProperty("cr2032_cpu_max")]
        public ushort CR2032CpuMax
        {
            get => _cr2032CpuMax;
            set => SetAndNotify(ref _cr2032CpuMax, value);
        }



        // 3. Flash Programming
        [JsonProperty("3. Прошивка flash")]
        public bool IsFlashProgrammingEnabled { get; set; }

        [JsonProperty("flash_prog")]
        public ushort FlashProg { get; set; }

        [JsonProperty("flash_firmware_path")]
        public string FlashFirmwarePath { get; set; }

        // 4. MCU Programming
        [JsonProperty("4. Прошивка MCU")]
        public bool IsMcuProgrammingEnabled { get; set; }

        [JsonProperty("mcu_prog")]
        public ushort McuProg { get; set; }

        [JsonProperty("mcu_firmware_path")]
        public string McuFirmwarePath { get; set; }

        // 5. DUT Self-Test
        [JsonProperty("5. Самотестирование")]
        public bool IsDutSelfTestEnabled { get; set; }

        [JsonProperty("dut_selftest")]
        public int DutSelfTest { get; set; }

        [JsonProperty("dut_start_time")]
        public ushort DutStartTime { get; set; }

        [JsonProperty("dut_end_of_new_logs_time")]
        public int DutEndOfNewLogsTime { get; set; }

        [JsonProperty("dut_sensor1_test")]
        public int DutSensor1Test { get; set; }

        [JsonProperty("dut_sensor2_test")]
        public int DutSensor2Test { get; set; }

        [JsonProperty("dut_relay_test")]
        public int DutRelayTest { get; set; }

        [JsonProperty("dut_tamper_test")]
        public int DutTamperTest { get; set; }

        [JsonProperty("dut_tamper_led_min")]
        public int DutTamperLedMin { get; set; }

        [JsonProperty("dut_tamper_led_max")]
        public int DutTamperLedMax { get; set; }

        [JsonProperty("dut_poe_test")]
        public int DutPoeTest { get; set; }


        [JsonProperty("dut_rs485_test")]
        public int DutRs485Test { get; set; }

        [JsonProperty("dut_i2c_test")]
        public int DutI2CTest { get; set; }

        [JsonProperty("dut_i2c_temper_min")]
        public double DutI2CTemperMin { get; set; }

        [JsonProperty("dut_i2c_temper_max")]
        public double DutI2CTemperMax { get; set; }

        // 6. Report Generation
        [JsonProperty("6. отправка отчёта")]
        public bool IsReportGenerationEnabled { get; set; }

        [JsonProperty("make_report")]
        public int MakeReport { get; set; }

        // 7. Label Printing
        [JsonProperty("7. печать этикетки")]
        public bool IsLabelPrintingEnabled { get; set; }

        [JsonProperty("print_label")]
        public int PrintLabel { get; set; }

        [JsonProperty("label_num")]
        public int LabelNum { get; set; }

        [JsonProperty("label_size")]
        public int LabelSize { get; set; }




    }
}