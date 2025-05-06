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

        private ushort _v52Out;
        public ushort V52Out
        {
            get => _v52Out;
            set => SetAndNotify(ref _v52Out, value);
        }

        private ushort _v55Out;
        public ushort V55Out
        {
            get => _v55Out;
            set => SetAndNotify(ref _v55Out, value);
        }

        private ushort _sensor1Out;
        public ushort Sensor1Out
        {
            get => _sensor1Out;
            set => SetAndNotify(ref _sensor1Out, value);
        }

        private ushort _sensor2Out;
        public ushort Sensor2Out
        {
            get => _sensor2Out;
            set => SetAndNotify(ref _sensor2Out, value);
        }

        private ushort _tamperOut;
        public ushort TamperOut
        {
            get => _tamperOut;
            set => SetAndNotify(ref _tamperOut, value);
        }

        private ushort _relayIn;
        public ushort RelayIn
        {
            get => _relayIn;
            set => SetAndNotify(ref _relayIn, value);
        }

        private ushort _resetOut;
        public ushort ResetOut
        {
            get => _resetOut;
            set => SetAndNotify(ref _resetOut, value);
        }

        private ushort _bootOut;
        public ushort BootOut
        {
            get => _bootOut;
            set => SetAndNotify(ref _bootOut, value);
        }

        private ushort _v52;
        public ushort V52
        {
            get => _v52;
            set
            {
                if (SetAndNotify(ref _v52, value))
                {
                    OnPropertyChanged(nameof(V52Display));
                }
            }
        }
        public ushort V52Display => (_v52 > 56000 || _v52 < 1000) ? (ushort)0 : _v52;


        private ushort _v55;
        public ushort V55
        {
            get => _v55;
            set
            {
                if (SetAndNotify(ref _v55, value))
                    OnPropertyChanged(nameof(V55Display));
            }
        }
        public ushort V55Display => (_v55 > 56000 || _v55 < 1000) ? (ushort)0 : _v55;

        private ushort _vOut;
        public ushort VOut
        {
            get => _vOut;
            set
            {
                if (SetAndNotify(ref _vOut, value))
                    OnPropertyChanged(nameof(VOutDisplay));
            }
        }
        public ushort VOutDisplay => (_vOut > 56000 || _vOut < 1) ? (ushort)0 : _vOut;


        private ushort _ref2048;
        public ushort Ref2048
        {
            get => _ref2048;
            set
            {
                if (SetAndNotify(ref _ref2048, value))
                    OnPropertyChanged(nameof(Ref2048Display));
            }
        }
        public ushort Ref2048Display => (_ref2048 > 6000 || _ref2048 < 1000) ? (ushort)0 : _ref2048;

        private ushort _v12;
        public ushort V12
        {
            get => _v12;
            set
            {
                if (SetAndNotify(ref _v12, value))
                    OnPropertyChanged(nameof(V12Display));
            }
        }
        public ushort V12Display => (_v12 > 20000 || _v12 < 1000) ? (ushort)0 : _v12;

        private ushort _v3_3;
        public ushort V3_3
        {
            get => _v3_3;
            set
            {
                if (SetAndNotify(ref _v3_3, value))
                    OnPropertyChanged(nameof(V3_3Display));
            }
        }
        public ushort V3_3Display => (_v3_3 > 5000 || _v3_3 < 1000) ? (ushort)0 : _v3_3;

        private ushort _v1_5;
        public ushort V1_5
        {
            get => _v1_5;
            set
            {
                if (SetAndNotify(ref _v1_5, value))
                    OnPropertyChanged(nameof(V1_5Display));
            }
        }
        public ushort V1_5Display => (_v1_5 > 5000 || _v1_5 < 1000) ? (ushort)0 : _v1_5;

        private ushort _v1_1;
        public ushort V1_1
        {
            get => _v1_1;
            set
            {
                if (SetAndNotify(ref _v1_1, value))
                    OnPropertyChanged(nameof(V1_1Display));
            }
        }
        public ushort V1_1Display => (_v1_1 > 5000 || _v1_1 < 1000) ? (ushort)0 : _v1_1;

        private ushort _cr2032;
        public ushort CR2032
        {
            get => _cr2032;
            set
            {
                if (SetAndNotify(ref _cr2032, value))
                    OnPropertyChanged(nameof(CR2032Display));
            }
        }
        public ushort CR2032Display => (_cr2032 > 5000 || _cr2032 < 100) ? (ushort)0 : _cr2032;

        private ushort _cr2032Cpu;
        public ushort CR2032_CPU
        {
            get => _cr2032Cpu;
            set
            {
                if (SetAndNotify(ref _cr2032Cpu, value))
                    OnPropertyChanged(nameof(CR2032_CPUDisplay));
            }
        }
        public ushort CR2032_CPUDisplay => (_cr2032Cpu > 5000 || _cr2032Cpu < 100) ? (ushort)0 : _cr2032Cpu;








        private ushort _statusTamper;
        private ushort _statusTamperLed;
        private ushort _runBtn;
        private ushort _nextBtn;
        private ushort _k5Stage1Start;
        private ushort _k5Stage1Status;
        private ushort _k5Stage2Start;
        private ushort _k5Stage2Status;
        private ushort _k5Stage3Start;
        private ushort _k5Stage3Status;
        private ushort _k5TestDelay;
        private ushort _vccStart;
        private ushort _vccTestStatus;
        private ushort _vccTestDelay;
        private ushort _v52Min;
        private ushort _v52Max;
        private ushort _v52Calibr;
        private ushort _v55Min;
        private ushort _v55Max;
        private ushort _v55Calibr;
        private ushort _vOutVmainMin;
        private ushort _vOutVmainMax;
        private ushort _vOutVresMin;
        private ushort _vOutVresMax;
        private ushort _vOutCalibr;
        private ushort _ref2048Min;
        private ushort _ref2048Max;
        private ushort _ref2048Calibr;
        private ushort _v12Min;
        private ushort _v12Max;
        private ushort _v12Calibr;
        private ushort _v3_3Min;
        private ushort _v3_3Max;
        private ushort _v3_3Calibr;
        private ushort _v1_5Min;
        private ushort _v1_5Max;
        private ushort _v1_5Calibr;
        private ushort _v1_1Min;
        private ushort _v1_1Max;
        private ushort _v1_1Calibr;
        private ushort _cr2032Min;
        private ushort _cr2032Max;
        private ushort _cr2032Calibr;
        private ushort _cr2032CpuMin;
        private ushort _cr2032CpuMax;
        private ushort _cr2032CpuCalibr;
        private ushort _tamperStatusMin;
        private ushort _tamperStatusMax;
        private ushort _tamperStatusCalibr;
        private ushort _tamperLedMin;
        private ushort _tamperLedMax;
        private ushort _tamperLedCalibr;
        private ushort _v52Report;
        private ushort _v55Report;
        private ushort _vOutReport;
        private ushort _ref2048Report;
        private ushort _v12Report;
        private ushort _v3_3Report;
        private ushort _v1_5Report;
        private ushort _v1_1Report;
        private ushort _cr2032Report;
        private ushort _cr2032CpuReport;
        private ushort _tamperLedReport;
        private ushort _tamperReport;
        private ushort _rs485Enable;
        private ushort _rs485RxOk;



        public ushort Status_Tamper
        {
            get => _statusTamper;
            set => SetAndNotify(ref _statusTamper, value);
        }

        public ushort Status_Tamper_Led
        {
            get => _statusTamperLed;
            set => SetAndNotify(ref _statusTamperLed, value);
        }

        public ushort RunBtn
        {
            get => _runBtn;
            set => SetAndNotify(ref _runBtn, value);
        }

        public ushort NextBtn
        {
            get => _nextBtn;
            set => SetAndNotify(ref _nextBtn, value);
        }

        public ushort K5Stage1Start
        {
            get => _k5Stage1Start;
            set => SetAndNotify(ref _k5Stage1Start, value);
        }

        public ushort K5Stage1Status
        {
            get => _k5Stage1Status;
            set => SetAndNotify(ref _k5Stage1Status, value);
        }

        public ushort K5Stage2Start
        {
            get => _k5Stage2Start;
            set => SetAndNotify(ref _k5Stage2Start, value);
        }

        public ushort K5Stage2Status
        {
            get => _k5Stage2Status;
            set => SetAndNotify(ref _k5Stage2Status, value);
        }

        public ushort K5Stage3Start
        {
            get => _k5Stage3Start;
            set => SetAndNotify(ref _k5Stage3Start, value);
        }

        public ushort K5Stage3Status
        {
            get => _k5Stage3Status;
            set => SetAndNotify(ref _k5Stage3Status, value);
        }

        public ushort K5TestDelay
        {
            get => _k5TestDelay;
            set => SetAndNotify(ref _k5TestDelay, value);
        }

        public ushort VCCStart
        {
            get => _vccStart;
            set => SetAndNotify(ref _vccStart, value);
        }

        public ushort VCCTestStatus
        {
            get => _vccTestStatus;
            set => SetAndNotify(ref _vccTestStatus, value);
        }

        public ushort VCCTestDelay
        {
            get => _vccTestDelay;
            set => SetAndNotify(ref _vccTestDelay, value);
        }

        public ushort V52Min
        {
            get => _v52Min;
            set => SetAndNotify(ref _v52Min, value);
        }

        public ushort V52Max
        {
            get => _v52Max;
            set => SetAndNotify(ref _v52Max, value);
        }

        public ushort V52Calibr
        {
            get => _v52Calibr;
            set => SetAndNotify(ref _v52Calibr, value);
        }

        public ushort V55Min
        {
            get => _v55Min;
            set => SetAndNotify(ref _v55Min, value);
        }

        public ushort V55Max
        {
            get => _v55Max;
            set => SetAndNotify(ref _v55Max, value);
        }

        public ushort V55Calibr
        {
            get => _v55Calibr;
            set => SetAndNotify(ref _v55Calibr, value);
        }

        public ushort VOutVmainMin
        {
            get => _vOutVmainMin;
            set => SetAndNotify(ref _vOutVmainMin, value);
        }

        public ushort VOutVmainMax
        {
            get => _vOutVmainMax;
            set => SetAndNotify(ref _vOutVmainMax, value);
        }

        public ushort VOutVresMin
        {
            get => _vOutVresMin;
            set => SetAndNotify(ref _vOutVresMin, value);
        }

        public ushort VOutVresMax
        {
            get => _vOutVresMax;
            set => SetAndNotify(ref _vOutVresMax, value);
        }

        public ushort VOutCalibr
        {
            get => _vOutCalibr;
            set => SetAndNotify(ref _vOutCalibr, value);
        }

        public ushort Ref2048Min
        {
            get => _ref2048Min;
            set => SetAndNotify(ref _ref2048Min, value);
        }

        public ushort Ref2048Max
        {
            get => _ref2048Max;
            set => SetAndNotify(ref _ref2048Max, value);
        }

        public ushort Ref2048Calibr
        {
            get => _ref2048Calibr;
            set => SetAndNotify(ref _ref2048Calibr, value);
        }

        public ushort V12Min
        {
            get => _v12Min;
            set => SetAndNotify(ref _v12Min, value);
        }

        public ushort V12Max
        {
            get => _v12Max;
            set => SetAndNotify(ref _v12Max, value);
        }

        public ushort V12Calibr
        {
            get => _v12Calibr;
            set => SetAndNotify(ref _v12Calibr, value);
        }

        public ushort V3_3Min
        {
            get => _v3_3Min;
            set => SetAndNotify(ref _v3_3Min, value);
        }

        public ushort V3_3Max
        {
            get => _v3_3Max;
            set => SetAndNotify(ref _v3_3Max, value);
        }

        public ushort V3_3Calibr
        {
            get => _v3_3Calibr;
            set => SetAndNotify(ref _v3_3Calibr, value);
        }

        public ushort V1_5Min
        {
            get => _v1_5Min;
            set => SetAndNotify(ref _v1_5Min, value);
        }

        public ushort V1_5Max
        {
            get => _v1_5Max;
            set => SetAndNotify(ref _v1_5Max, value);
        }

        public ushort V1_5Calibr
        {
            get => _v1_5Calibr;
            set => SetAndNotify(ref _v1_5Calibr, value);
        }

        public ushort V1_1Min
        {
            get => _v1_1Min;
            set => SetAndNotify(ref _v1_1Min, value);
        }

        public ushort V1_1Max
        {
            get => _v1_1Max;
            set => SetAndNotify(ref _v1_1Max, value);
        }

        public ushort V1_1Calibr
        {
            get => _v1_1Calibr;
            set => SetAndNotify(ref _v1_1Calibr, value);
        }

        public ushort CR2032Min
        {
            get => _cr2032Min;
            set => SetAndNotify(ref _cr2032Min, value);
        }

        public ushort CR2032Max
        {
            get => _cr2032Max;
            set => SetAndNotify(ref _cr2032Max, value);
        }

        public ushort CR2032Calibr
        {
            get => _cr2032Calibr;
            set => SetAndNotify(ref _cr2032Calibr, value);
        }

        public ushort CR2032_CPUMin
        {
            get => _cr2032CpuMin;
            set => SetAndNotify(ref _cr2032CpuMin, value);
        }

        public ushort CR2032_CPUMax
        {
            get => _cr2032CpuMax;
            set => SetAndNotify(ref _cr2032CpuMax, value);
        }

        public ushort CR2032_CPUCalibr
        {
            get => _cr2032CpuCalibr;
            set => SetAndNotify(ref _cr2032CpuCalibr, value);
        }

        public ushort TamperStatusMin
        {
            get => _tamperStatusMin;
            set => SetAndNotify(ref _tamperStatusMin, value);
        }

        public ushort TamperStatusMax
        {
            get => _tamperStatusMax;
            set => SetAndNotify(ref _tamperStatusMax, value);
        }

        public ushort TamperStatusCalibr
        {
            get => _tamperStatusCalibr;
            set => SetAndNotify(ref _tamperStatusCalibr, value);
        }

        public ushort TamperLedMin
        {
            get => _tamperLedMin;
            set => SetAndNotify(ref _tamperLedMin, value);
        }

        public ushort TamperLedMax
        {
            get => _tamperLedMax;
            set => SetAndNotify(ref _tamperLedMax, value);
        }

        public ushort TamperLedCalibr
        {
            get => _tamperLedCalibr;
            set => SetAndNotify(ref _tamperLedCalibr, value);
        }

        public ushort V52Report
        {
            get => _v52Report;
            set => SetAndNotify(ref _v52Report, value);
        }

        public ushort V55Report
        {
            get => _v55Report;
            set => SetAndNotify(ref _v55Report, value);
        }

        public ushort VOutReport
        {
            get => _vOutReport;
            set => SetAndNotify(ref _vOutReport, value);
        }

        public ushort Ref2048Report
        {
            get => _ref2048Report;
            set => SetAndNotify(ref _ref2048Report, value);
        }

        public ushort V12Report
        {
            get => _v12Report;
            set => SetAndNotify(ref _v12Report, value);
        }

        public ushort V3_3Report
        {
            get => _v3_3Report;
            set => SetAndNotify(ref _v3_3Report, value);
        }

        public ushort V1_5Report
        {
            get => _v1_5Report;
            set => SetAndNotify(ref _v1_5Report, value);
        }

        public ushort V1_1Report
        {
            get => _v1_1Report;
            set => SetAndNotify(ref _v1_1Report, value);
        }

        public ushort CR2032Report
        {
            get => _cr2032Report;
            set => SetAndNotify(ref _cr2032Report, value);
        }

        public ushort CR2032_CPUReport
        {
            get => _cr2032CpuReport;
            set => SetAndNotify(ref _cr2032CpuReport, value);
        }

        public ushort TamperLedReport
        {
            get => _tamperLedReport;
            set => SetAndNotify(ref _tamperLedReport, value);
        }

        public ushort TamperReport
        {
            get => _tamperReport;
            set => SetAndNotify(ref _tamperReport, value);
        }

        public ushort RS485Enable
        {
            get => _rs485Enable;
            set => SetAndNotify(ref _rs485Enable, value);
        }

        public ushort RS485RxOK
        {
            get => _rs485RxOk;
            set => SetAndNotify(ref _rs485RxOk, value);
        }
    }
}
