using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace RTL.Models
{
    public class StandRegistersModel : INotifyPropertyChanged
    {
        private static StandRegistersModel _instance;
        public static StandRegistersModel Instance => _instance ??= new StandRegistersModel();
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        // 1. Серийный номер стенда
        public static ushort StandSerialNumber { get; set; } // 0

        // 2-9. Напряжения на выходах
        public static ushort V52Out { get; set; } // 1. 52V OUT
        public static ushort V55Out { get; set; } // 2. 55V OUT
        public static ushort Sensor1Out { get; set; } // 3. SENSOR1 OUT
        public static ushort Sensor2Out { get; set; } // 4. SENSOR2 OUT
        public static ushort TamperOut { get; set; } // 5. TAMPER OUT
        public static ushort RelayIn { get; set; } // 6. RELAY IN
        public static ushort ResetOut { get; set; } // 7. RESET OUT
        public static ushort Boot0Out { get; set; } // 8. BOOT0 OUT

        // 10-18. Показатели напряжений
        public static ushort V52 { get; set; } // 9. 52V (62798)

        public static ushort V55 { get; set; } // 10. 55V (62800)
        public static ushort VOut { get; set; } // 11. VOUT (62798)
        public static ushort Ref2048 { get; set; } // 12. REF2048 (8058)
        public static ushort V12 { get; set; } // 13. 12V (58504)
        public static ushort V3_3 { get; set; } // 14. 3.3V (8055)
        public static ushort V1_5 { get; set; } // 15. 1.5V (8055)
        public static ushort V1_1 { get; set; } // 16. 1.1V (8055)
        public static ushort CR2032 { get; set; } // 17. CR2032 (8051)
        public static ushort CR2032_CPU { get; set; } // 18. CR2032_CPU (8052)

        // 20-24. Статус и управление TAMPER
        public static ushort StatusTamper { get; set; } // 19. STATUS_TAMPER (8052)
        public static ushort TamperLED { get; set; } // 20. TAMPER_LED (8052)
        public static ushort RunBtn { get; set; } // 21. RUN BTN (0)
        public static ushort NextBtn { get; set; } // 22. NEXT BTN (0)

        // 24-27. Узел K5
        public static ushort K5Stage1Start { get; set; } // 23. K5 STAGE1 START (VMAIN) (0-not run, 1 - run) (Регистр 0)
        public static ushort K5Stage1Status { get; set; } // 24. K5 STAGE1 STATUS (0-wait, 1-run, 2-ok, 3-fail) (Регистр 0)

        public static ushort K5Stage2Start { get; set; } // 25. K5 STAGE2 START (VMAIN+VRES) (Регистр 0)
        public static ushort K5Stage2Status { get; set; } // 26. K5 STAGE2 STATUS (Регистр 108)

        // 28-31. Узел K5: Этапы 3 и задержки
        public static ushort K5Stage3Start { get; set; } // 27. K5 STAGE3 START (VRES) (Регистр 0)
        public static ushort K5Stage3Status { get; set; } // 28. K5 STAGE3 STATUS (Регистр 0)

        public static ushort K5TestDelay { get; set; } // 29. K5 TEST DELAY (Регистр 10)

        // 32-34. VCC тест
        public static ushort VCCStart { get; set; } // 30. VCC START (0-not run,1-run) (Регистр 0)
        public static ushort VCCTestStatus { get; set; } // 31. VCC TEST STATUS (0-wait, 1-run, 2-ok, 3-fail) (Регистр 0)
        public static ushort VCCTestDelay { get; set; } // 32. VCC TEST DELAY (Регистр 10)

        // 35-50. Параметры калибровки
        public static ushort V52Min { get; set; } // 33. 52V_MIN (51000)
        public static ushort V52Max { get; set; } // 34. 52V_MAX (53000)
        public static ushort V52Calibr { get; set; } // 35. 52V_CALIBR (10000)

        public static ushort V55Min { get; set; } // 36. 55V_MIN (54000)
        public static ushort V55Max { get; set; } // 37. 55V_MAX (56000)
        public static ushort V55Calibr { get; set; } // 38. 55V_CALIBR (10000)

        public static ushort VOutVMainMin { get; set; } // 39. VOUT_VMAIN_MIN (54000)
        public static ushort VOutVMainMax { get; set; } // 40. VOUT_VMAIN_MAX (56000)
        public static ushort VOutVResMin { get; set; } // 41. VOUT_VRES_MIN (51000)
        public static ushort VOutVResMax { get; set; } // 42. VOUT_VRES_MAX (53000)
        public static ushort VOutCalibr { get; set; } // 43. VOUT_CALIBR (10000)

        public static ushort Ref2048Min { get; set; } // 44. REF2048_MIN (2000)
        public static ushort Ref2048Max { get; set; } // 45. REF2048_MAX (2100)
        public static ushort Ref2048Calibr { get; set; } // 46. REF2048_CALIBR (10000)

        public static ushort V12Min { get; set; } // 47. 12V_MIN (11000)
        public static ushort V12Max { get; set; } // 48. 12V_MAX (13000)
        public static ushort V12Calibr { get; set; } // 49. 12V_CALIBR (10000)

        // 51-62. Дополнительные калибровки
        public static ushort V3_3Min { get; set; } // 50. 3.3V_MIN (3200)
        public static ushort V3_3Max { get; set; } // 51. 3.3V_MAX (3400)
        public static ushort V3_3Calibr { get; set; } // 52. 3.3V_CALIBR (10000)

        public static ushort V1_5Min { get; set; } // 53. 1.5V_MIN (1100)
        public static ushort V1_5Max { get; set; } // 54. 1.5V_MAX (1600)
        public static ushort V1_5Calibr { get; set; } // 55. 1.5V_CALIBR (10000)

        public static ushort V1_1Min { get; set; } // 56. 1.1V_MIN (1000)
        public static ushort V1_1Max { get; set; } // 57. 1.1V_MAX (1200)
        public static ushort V1_1Calibr { get; set; } // 58. 1.1V_CALIBR (10000)

        public static ushort CR2032Min { get; set; } // 59. CR2032_MIN (2500)
        public static ushort CR2032Max { get; set; } // 60. CR2032_MAX (3100)
        public static ushort CR2032Calibr { get; set; } // 61. CR2032_CALIBR (10000)

        public static ushort CR2032_CPUMin { get; set; } // 62. CR2032_CPU_MIN (1000)
        public static ushort CR2032_CPUMax { get; set; } // 63. CR2032_CPU_MAX (10000)
        public static ushort CR2032_CPUCalibr { get; set; } // 64. CR2032_CPU_CALIBR (10000)

        public static ushort TamperStatusMin { get; set; } // 65. TAMPER_STATUS_MIN (0)
        public static ushort TamperStatusMax { get; set; } // 66. TAMPER_STATUS_MAX (3300)
        public static ushort TamperStatusCalibr { get; set; } // 67. TAMPER_STATUS_CALIBR (10000)

        public static ushort TamperLEDMin { get; set; } // 68. TAMPER_LED_MIN (0)
        public static ushort TamperLEDMax { get; set; } // 69. TAMPER_LED_MAX (3300)
        public static ushort TamperLEDCalibr { get; set; } // 70. TAMPER_LED_CALIBR (10000)

        // 71-75. Статус отчетности - отчеты для к5
        public static ushort V52Report { get; set; } // 71. 52V_REPORT (0)
        public static ushort V55Report { get; set; } // 72. 55V_REPORT (0)
        public static ushort VOutReport { get; set; } // 73. VOUT_REPORT (0)
        public static ushort Ref2048Report { get; set; } // 74. REF2048_REPORT (0)
        public static ushort V12Report { get; set; } // 75. 12V_REPORT (0)


        // vcc статистика
        public static ushort V3_3Report { get; set; } // 76. 3.3V_REPORT (0)
        public static ushort V1_5Report { get; set; } // 77. 1.5V_REPORT (0)
        public static ushort V1_1Report { get; set; } // 78. 1.1V_REPORT (0)
        public static ushort CR2032Report { get; set; } // 79. CR2032_REPORT (0)
        public static ushort CR2032_CPUReport { get; set; } // 80. CR2032_CPU_REPORT (0)

        public static ushort TamperLEDReport { get; set; } // 81. TAMPER_LED_REPORT (0)
        public static ushort TamperReport { get; set; } // 82. TAMPER_REPORT (0)

        // 84-85. RS485 параметры
        public static ushort RS485Enable { get; set; } // 83. RS485_ENABLE (1)
        public static ushort RS485RxOk { get; set; } // 84. RS485_RX_OK (190)


        public static void UpdateRegister(int index, ushort value)
        {
            switch (index)
            {
                case 0: StandSerialNumber = value; break;
                case 1: V52Out = value; break;
                case 2: V55Out = value; break;
                case 3: Sensor1Out = value; break;
                case 4: Sensor2Out = value; break;
                case 5: TamperOut = value; break;
                case 6: RelayIn = value; break;
                case 7: ResetOut = value; break;
                case 8: Boot0Out = value; break;
                case 9: V52 = value; break;
                case 10: V55 = value; break;
                case 11: VOut = value; break;
                case 12: Ref2048 = value; break;
                case 13: V12 = value; break;
                case 14: V3_3 = value; break;
                case 15: V1_5 = value; break;
                case 16: V1_1 = value; break;
                case 17: CR2032 = value; break;
                case 18: CR2032_CPU = value; break;
                case 19: StatusTamper = value; break;
                case 20: TamperLED = value; break;
                case 21: RunBtn = value; break; // кнопка старта тестирования (физическая)
                case 22: NextBtn = value; break;
                case 23: K5Stage1Start = value; break;
                case 24: K5Stage1Status = value; break;
                case 25: K5Stage2Start = value; break;
                case 26: K5Stage2Status = value; break;
                case 27: K5Stage3Start = value; break;
                case 28: K5Stage3Status = value; break;
                case 29: K5TestDelay = value; break;
                case 30: VCCStart = value; break;
                case 31: VCCTestStatus = value; break;
                case 32: VCCTestDelay = value; break;
                case 33: V52Min = value; break;
                case 34: V52Max = value; break;
                case 35: V52Calibr = value; break;
                case 36: V55Min = value; break;
                case 37: V55Max = value; break;
                case 38: V55Calibr = value; break;
                case 39: VOutVMainMin = value; break;
                case 40: VOutVMainMax = value; break;
                case 41: VOutVResMin = value; break;
                case 42: VOutVResMax = value; break;
                case 43: VOutCalibr = value; break;
                case 44: Ref2048Min = value; break;
                case 45: Ref2048Max = value; break;
                case 46: Ref2048Calibr = value; break;
                case 47: V12Min = value; break;
                case 48: V12Max = value; break;
                case 49: V12Calibr = value; break;
                case 50: V3_3Min = value; break;
                case 51: V3_3Max = value; break;
                case 52: V3_3Calibr = value; break;
                case 53: V1_5Min = value; break;
                case 54: V1_5Max = value; break;
                case 55: V1_5Calibr = value; break;
                case 56: V1_1Min = value; break;
                case 57: V1_1Max = value; break;
                case 58: V1_1Calibr = value; break;
                case 59: CR2032Min = value; break;
                case 60: CR2032Max = value; break;
                case 61: CR2032Calibr = value; break;
                case 62: CR2032_CPUMin = value; break;
                case 63: CR2032_CPUMax = value; break;
                case 64: CR2032_CPUCalibr = value; break;
                case 65: TamperStatusMin = value; break;
                case 66: TamperStatusMax = value; break;
                case 67: TamperStatusCalibr = value; break;
                case 68: TamperLEDMin = value; break;
                case 69: TamperLEDMax = value; break;
                case 70: TamperLEDCalibr = value; break;
                case 71: V52Report = value; break;
                case 72: V55Report = value; break;
                case 73: VOutReport = value; break;
                case 74: Ref2048Report = value; break;
                case 75: V12Report = value; break;
                case 76: V3_3Report = value; break;
                case 77: V1_5Report = value; break;
                case 78: V1_1Report = value; break;
                case 79: CR2032Report = value; break;
                case 80: CR2032_CPUReport = value; break;
                case 81: TamperLEDReport = value; break;
                case 82: TamperReport = value; break;
                case 83: RS485Enable = value; break;
                case 84: RS485RxOk = value; break;
                default: throw new ArgumentOutOfRangeException(nameof(index), "Индекс не существует");
            }
        }
    }
}
