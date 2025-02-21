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
        // Основные свойства модели
        [JsonProperty("model_name")]
        public string ModelName { get; set; }

        [JsonProperty("model_type")]
        public int ModelType { get; set; }

        // 1. Проверка узла K5
        [JsonProperty("1.Проверка узла K5")]
        public bool IsK5TestEnabled { get; set; }


        [JsonProperty("k5_start_delay")]
        public int K5StartDelay { get; set; }

        [JsonProperty("k5_test_delay")]
        public ushort K5TestDelay { get; set; }

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
        public ushort V12Min
        {
            get => _v12Min;
            set => SetAndNotify(ref _v12Min, value);
        }

        [JsonProperty("v12_max")]
        public ushort V12Max
        {
            get => _v12Max;
            set => SetAndNotify(ref _v12Max, value);
        }

        [JsonProperty("vout_vmain_min")]
        public ushort VoutMin
        {
            get => _voutMin;
            set => SetAndNotify(ref _voutMin, value);
        }
        [JsonProperty("vout_vmain_max")]
        public ushort VoutMax
        {
            get => _voutMax;
            set => SetAndNotify(ref _voutMax, value);
        }
        [JsonProperty("vout_vres_min")]
        public ushort VoutVresMin
        {
            get => _voutVresMin;
            set => SetAndNotify(ref _voutVresMin, value);
        }

        [JsonProperty("vout_vres_max")]
        public ushort VoutVresMax
        {
            get => _voutVresMax;
            set => SetAndNotify(ref _voutVresMax, value);
        }

        [JsonProperty("vref_min")]
        public ushort VrefMin
        {
            get => _vrefMin;
            set => SetAndNotify(ref _vrefMin, value);
        }

        [JsonProperty("vref_max")]
        public ushort VrefMax
        {
            get => _vrefMax;
            set => SetAndNotify(ref _vrefMax, value);
        }






        // 2. Проверка VCC
        [JsonProperty("2.Проверка VCC")]
        public bool IsVccTestEnabled { get; set; }


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
        #region
        // 3. Прошивка Flash
        [JsonProperty("3. Прошивка flash")]
        public bool IsFlashProgrammingEnabled { get; set; }

        // 4. Прошивка MCU
        [JsonProperty("4. Прошивка MCU")]
        public bool IsMcuProgrammingEnabled { get; set; }

        // 5. Самотестирование
        [JsonProperty("5. Самотестирование")]
        public bool IsDutSelfTestEnabled { get; set; }

        [JsonProperty("dut_selftest")]
        public bool DutSelfTest { get; set; }

        [JsonProperty("dut_start_time")]
        public ushort DutStartTime { get; set; }

        [JsonProperty("dut_end_of_new_logs_time")]
        public int DutEndOfNewLogsTime { get; set; }

        [JsonProperty("dut_sensor1_test")]
        public bool DutSensor1Test { get; set; }

        [JsonProperty("dut_sensor2_test")]
        public bool DutSensor2Test { get; set; }

        [JsonProperty("dut_relay_test")]
        public bool DutRelayTest { get; set; }

        [JsonProperty("dut_tamper_test")]
        public bool DutTamperTest { get; set; }




        [JsonProperty("dut_tamper_status_min")]
        public ushort DutTamperStatusMin {get; set;}


        [JsonProperty("dut_tamper_status_max")]
        public ushort DutTamperStatusMax { get; set; }




        [JsonProperty("dut_tamper_led_min")]
        public ushort DutTamperLedMin { get; set; }

        [JsonProperty("dut_tamper_led_max")]
        public ushort DutTamperLedMax { get; set; }

        [JsonProperty("dut_poe_test")]
        public bool DutPoeTest { get; set; }

        [JsonProperty("dut_rs485_test")]
        public bool DutRs485Test { get; set; }

        [JsonProperty("dut_i2c_test")]
        public bool DutI2CTest { get; set; }

        [JsonProperty("dut_i2c_temper_min")]
        public int DutI2CTemperMin { get; set; }

        [JsonProperty("dut_i2c_temper_max")]
        public int DutI2CTemperMax { get; set; }

        // 6. Отправка отчёта
        [JsonProperty("6. отправка отчёта")]
        public bool IsReportGenerationEnabled { get; set; }

        // 7. Печать этикетки
        [JsonProperty("7. печать этикетки")]
        public bool IsLabelPrintingEnabled { get; set; }

        [JsonProperty("print_label")]
        public int PrintLabel { get; set; }

        [JsonProperty("label_num")]
        public int LabelNum { get; set; }

        [JsonProperty("label_size")]
        public int LabelSize { get; set; }
#endregion

        // Приватные поля для свойств с уведомлениями
        private ushort _k5_52V_Min;
        private ushort _k5_52V_Max;
        private ushort _k5_55V_Min;
        private ushort _k5_55V_Max;
        private ushort _v12Min;
        private ushort _v12Max;
        private ushort _voutMin;
        private ushort _voutMax;
        private ushort _voutVresMin;
        private ushort _voutVresMax;
        private ushort _vrefMin;
        private ushort _vrefMax;
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

        // Реализация INotifyPropertyChanged
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
    }
}