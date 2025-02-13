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

            if (propertyName == nameof(K5_52V_Min))
            {
                K5_52V_MinChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public event EventHandler K5_52V_MinChanged;

        private ushort _k5_52V_Min;

        [JsonProperty("k5_52V_min")]
        public ushort K5_52V_Min
        {
            get => _k5_52V_Min;
            set => SetAndNotify(ref _k5_52V_Min, value);
        }

        private bool SetAndNotify<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }


        [JsonProperty("k5_52V_max")]
        public ushort K5_52V_Max { get; set; }

        [JsonProperty("k5_55V_min")]
        public ushort K5_55V_Min { get; set; }

        [JsonProperty("k5_55V_max")]
        public ushort K5_55V_Max { get; set; }

        [JsonProperty("v12_min")]
        public ushort V12_min { get; set; }

        [JsonProperty("v12_max")]
        public ushort V12_max { get; set; }


        [JsonProperty("vout_vmain_min")]
        public ushort VoutVMainMin { get; set; }

        [JsonProperty("vout_vmain_max")]
        public int VoutVMainMax { get; set; }

        [JsonProperty("vout_vres_min")]
        public int VoutVResMin { get; set; }

        [JsonProperty("vout_vres_max")]
        public int VoutVResMax { get; set; }

        [JsonProperty("vref_min")]
        public ushort VRefMin { get; set; }

        [JsonProperty("vref_max")]
        public int VRefMax { get; set; }




        // 2. VCC Test
        [JsonProperty("2.Проверка VCC")]
        public bool IsVccTestEnabled { get; set; }

        [JsonProperty("vcc_test")]
        public ushort VccTest { get; set; }

        [JsonProperty("vcc_start_delay")]
        public ushort VccStartDelay { get; set; }

        [JsonProperty("vcc_3V3_min")]
        public ushort Vcc3V3Min { get; set; }

        [JsonProperty("vcc_3V3_max")]
        public ushort Vcc3V3Max { get; set; }

        [JsonProperty("vcc_1V5_min")]
        public ushort Vcc1V5Min { get; set; }

        [JsonProperty("vcc_1V5_max")]
        public ushort Vcc1V5Max { get; set; }

        [JsonProperty("vcc_1V1_min")]
        public ushort Vcc1V1Min { get; set; }

        [JsonProperty("vcc_1V1_max")]
        public ushort Vcc1V1Max { get; set; }

        [JsonProperty("cr2032_min")]
        public ushort CR2032Min { get; set; }

        [JsonProperty("cr2032_max")]
        public ushort CR2032Max { get; set; }

        [JsonProperty("cr2032_cpu_min")]
        public ushort CR2032CpuMin { get; set; }

        [JsonProperty("cr2032_cpu_max")]
        public ushort CR2032CpuMax { get; set; }

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