using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System;



namespace RTL.Models
{
    public class PoeRegistersModel : INotifyPropertyChanged
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


        // плашка отображения 
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


        #region базовые регистры 
        private ushort _standSerialNumber;
        public ushort StandSerialNumber
        {
            get => _standSerialNumber;
            set => SetAndNotify(ref _standSerialNumber, value);
        }
        private ushort _runButton;
        public ushort RunButton
        {
            get => _runButton;
            set => SetAndNotify(ref _runButton, value);
        }
        private ushort _nextButton;
        public ushort NextButton
        {
            get => _nextButton;
            set => SetAndNotify(ref _nextButton, value);
        }
        private ushort _enable52V;
        public ushort Enable52V // --------------------------------------------- тумблер как v52 
        {
            get => _enable52V;
            set => SetAndNotify(ref _enable52V, value);
        }
        private ushort _voltage3V3Meas; //-------------------------------------- нужно для отображения  3.3V_MEAS
        public ushort Voltage3V3Meas
        {
            get => _voltage3V3Meas;
            set => SetAndNotify(ref _voltage3V3Meas, value);
        }

        #region PowerGood 
        private ushort _powerGood1AChannel1;  // -------------------- во всех регистрах 1405 - 1420 должны быть единички. если 0 то ошибка.
        public ushort PowerGood1AChannel1
        {
            get => _powerGood1AChannel1;
            set => SetAndNotify(ref _powerGood1AChannel1, value);
        }
        private ushort _powerGood1BChannel2;
        public ushort PowerGood1BChannel2
        {
            get => _powerGood1BChannel2;
            set => SetAndNotify(ref _powerGood1BChannel2, value);
        }
        private ushort _powerGood2AChannel3;
        public ushort PowerGood2AChannel3
        {
            get => _powerGood2AChannel3;
            set => SetAndNotify(ref _powerGood2AChannel3, value);
        }
        private ushort _powerGood2BChannel4;
        public ushort PowerGood2BChannel4
        {
            get => _powerGood2BChannel4;
            set => SetAndNotify(ref _powerGood2BChannel4, value);

        }
        private ushort _powerGood3AChannel5;
        public ushort PowerGood3AChannel5
        {
            get => _powerGood3AChannel5;
            set => SetAndNotify(ref _powerGood3AChannel5, value);
        }
        private ushort _powerGood3BChannel6;
        public ushort PowerGood3BChannel6
        {
            get => _powerGood3BChannel6;
            set => SetAndNotify(ref _powerGood3BChannel6, value);
        }

        private ushort _powerGood4AChannel7;
        public ushort PowerGood4AChannel7
        {
            get => _powerGood4AChannel7;
            set => SetAndNotify(ref _powerGood4AChannel7, value);
        }

        private ushort _powerGood4BChannel8;
        public ushort PowerGood4BChannel8
        {
            get => _powerGood4BChannel8;
            set => SetAndNotify(ref _powerGood4BChannel8, value);
        }

        private ushort _powerGood5AChannel9;
        public ushort PowerGood5AChannel9
        {
            get => _powerGood5AChannel9;
            set => SetAndNotify(ref _powerGood5AChannel9, value);
        }

        private ushort _powerGood5BChannel10;
        public ushort PowerGood5BChannel10
        {
            get => _powerGood5BChannel10;
            set => SetAndNotify(ref _powerGood5BChannel10, value);
        }

        private ushort _powerGood6AChannel11;
        public ushort PowerGood6AChannel11
        {
            get => _powerGood6AChannel11;
            set => SetAndNotify(ref _powerGood6AChannel11, value);
        }

        private ushort _powerGood6BChannel12;
        public ushort PowerGood6BChannel12
        {
            get => _powerGood6BChannel12;
            set => SetAndNotify(ref _powerGood6BChannel12, value);
        }

        private ushort _powerGood7AChannel13;
        public ushort PowerGood7AChannel13
        {
            get => _powerGood7AChannel13;
            set => SetAndNotify(ref _powerGood7AChannel13, value);
        }

        private ushort _powerGood7BChannel14;
        public ushort PowerGood7BChannel14
        {
            get => _powerGood7BChannel14;
            set => SetAndNotify(ref _powerGood7BChannel14, value);
        }

        private ushort _powerGood8AChannel15;
        public ushort PowerGood8AChannel15
        {
            get => _powerGood8AChannel15;
            set => SetAndNotify(ref _powerGood8AChannel15, value);
        }
        private ushort _powerGood8BChannel16;
        public ushort PowerGood8BChannel16
        {
            get => _powerGood8BChannel16;
            set => SetAndNotify(ref _powerGood8BChannel16, value);
        }
        #endregion PowerGood

        #region white
        private ushort _white1AChannel1;
        public ushort White1AChannel1 { get => _white1AChannel1; set => SetAndNotify(ref _white1AChannel1, value); }

        private ushort _white1BChannel2;
        public ushort White1BChannel2 { get => _white1BChannel2; set => SetAndNotify(ref _white1BChannel2, value); }

        private ushort _white2AChannel3;
        public ushort White2AChannel3 { get => _white2AChannel3; set => SetAndNotify(ref _white2AChannel3, value); }

        private ushort _white2BChannel4;
        public ushort White2BChannel4 { get => _white2BChannel4; set => SetAndNotify(ref _white2BChannel4, value); }

        private ushort _white3AChannel5;
        public ushort White3AChannel5 { get => _white3AChannel5; set => SetAndNotify(ref _white3AChannel5, value); }

        private ushort _white3BChannel6;
        public ushort White3BChannel6 { get => _white3BChannel6; set => SetAndNotify(ref _white3BChannel6, value); }

        private ushort _white4AChannel7;
        public ushort White4AChannel7 { get => _white4AChannel7; set => SetAndNotify(ref _white4AChannel7, value); }

        private ushort _white4BChannel8;
        public ushort White4BChannel8 { get => _white4BChannel8; set => SetAndNotify(ref _white4BChannel8, value); }

        private ushort _white5AChannel9;
        public ushort White5AChannel9 { get => _white5AChannel9; set => SetAndNotify(ref _white5AChannel9, value); }

        private ushort _white5BChannel10;
        public ushort White5BChannel10 { get => _white5BChannel10; set => SetAndNotify(ref _white5BChannel10, value); }

        private ushort _white6AChannel11;
        public ushort White6AChannel11 { get => _white6AChannel11; set => SetAndNotify(ref _white6AChannel11, value); }

        private ushort _white6BChannel12;
        public ushort White6BChannel12 { get => _white6BChannel12; set => SetAndNotify(ref _white6BChannel12, value); }

        private ushort _white7AChannel13;
        public ushort White7AChannel13 { get => _white7AChannel13; set => SetAndNotify(ref _white7AChannel13, value); }

        private ushort _white7BChannel14;
        public ushort White7BChannel14 { get => _white7BChannel14; set => SetAndNotify(ref _white7BChannel14, value); }

        private ushort _white8AChannel15;
        public ushort White8AChannel15 { get => _white8AChannel15; set => SetAndNotify(ref _white8AChannel15, value); }

        private ushort _white8BChannel16;
        public ushort White8BChannel16 { get => _white8BChannel16; set => SetAndNotify(ref _white8BChannel16, value); }
        #endregion white

        #region red

        private ushort _red1AChannel1;
        public ushort Red1AChannel1 { get => _red1AChannel1; set => SetAndNotify(ref _red1AChannel1, value); }

        private ushort _red1BChannel2;
        public ushort Red1BChannel2 { get => _red1BChannel2; set => SetAndNotify(ref _red1BChannel2, value); }

        private ushort _red2AChannel3;
        public ushort Red2AChannel3 { get => _red2AChannel3; set => SetAndNotify(ref _red2AChannel3, value); }

        private ushort _red2BChannel4;
        public ushort Red2BChannel4 { get => _red2BChannel4; set => SetAndNotify(ref _red2BChannel4, value); }

        private ushort _red3AChannel5;
        public ushort Red3AChannel5 { get => _red3AChannel5; set => SetAndNotify(ref _red3AChannel5, value); }

        private ushort _red3BChannel6;
        public ushort Red3BChannel6 { get => _red3BChannel6; set => SetAndNotify(ref _red3BChannel6, value); }

        private ushort _red4AChannel7;
        public ushort Red4AChannel7 { get => _red4AChannel7; set => SetAndNotify(ref _red4AChannel7, value); }

        private ushort _red4BChannel8;
        public ushort Red4BChannel8 { get => _red4BChannel8; set => SetAndNotify(ref _red4BChannel8, value); }

        private ushort _red5AChannel9;
        public ushort Red5AChannel9 { get => _red5AChannel9; set => SetAndNotify(ref _red5AChannel9, value); }

        private ushort _red5BChannel10;
        public ushort Red5BChannel10 { get => _red5BChannel10; set => SetAndNotify(ref _red5BChannel10, value); }

        private ushort _red6AChannel11;
        public ushort Red6AChannel11 { get => _red6AChannel11; set => SetAndNotify(ref _red6AChannel11, value); }

        private ushort _red6BChannel12;
        public ushort Red6BChannel12 { get => _red6BChannel12; set => SetAndNotify(ref _red6BChannel12, value); }

        private ushort _red7AChannel13;
        public ushort Red7AChannel13 { get => _red7AChannel13; set => SetAndNotify(ref _red7AChannel13, value); }

        private ushort _red7BChannel14;
        public ushort Red7BChannel14 { get => _red7BChannel14; set => SetAndNotify(ref _red7BChannel14, value); }

        private ushort _red8AChannel15;
        public ushort Red8AChannel15 { get => _red8AChannel15; set => SetAndNotify(ref _red8AChannel15, value); }

        private ushort _red8BChannel16;
        public ushort Red8BChannel16 { get => _red8BChannel16; set => SetAndNotify(ref _red8BChannel16, value); }

        #endregion red

        #region green

        private ushort _green1AChannel1;
        public ushort Green1AChannel1 { get => _green1AChannel1; set => SetAndNotify(ref _green1AChannel1, value); }

        private ushort _green1BChannel2;
        public ushort Green1BChannel2 { get => _green1BChannel2; set => SetAndNotify(ref _green1BChannel2, value); }

        private ushort _green2AChannel3;
        public ushort Green2AChannel3 { get => _green2AChannel3; set => SetAndNotify(ref _green2AChannel3, value); }

        private ushort _green2BChannel4;
        public ushort Green2BChannel4 { get => _green2BChannel4; set => SetAndNotify(ref _green2BChannel4, value); }

        private ushort _green3AChannel5;
        public ushort Green3AChannel5 { get => _green3AChannel5; set => SetAndNotify(ref _green3AChannel5, value); }

        private ushort _green3BChannel6;
        public ushort Green3BChannel6 { get => _green3BChannel6; set => SetAndNotify(ref _green3BChannel6, value); }

        private ushort _green4AChannel7;
        public ushort Green4AChannel7 { get => _green4AChannel7; set => SetAndNotify(ref _green4AChannel7, value); }

        private ushort _green4BChannel8;
        public ushort Green4BChannel8 { get => _green4BChannel8; set => SetAndNotify(ref _green4BChannel8, value); }

        private ushort _green5AChannel9;
        public ushort Green5AChannel9 { get => _green5AChannel9; set => SetAndNotify(ref _green5AChannel9, value); }

        private ushort _green5BChannel10;
        public ushort Green5BChannel10 { get => _green5BChannel10; set => SetAndNotify(ref _green5BChannel10, value); }

        private ushort _green6AChannel11;
        public ushort Green6AChannel11 { get => _green6AChannel11; set => SetAndNotify(ref _green6AChannel11, value); }

        private ushort _green6BChannel12;
        public ushort Green6BChannel12 { get => _green6BChannel12; set => SetAndNotify(ref _green6BChannel12, value); }

        private ushort _green7AChannel13;
        public ushort Green7AChannel13 { get => _green7AChannel13; set => SetAndNotify(ref _green7AChannel13, value); }

        private ushort _green7BChannel14;
        public ushort Green7BChannel14 { get => _green7BChannel14; set => SetAndNotify(ref _green7BChannel14, value); }

        private ushort _green8AChannel15;
        public ushort Green8AChannel15 { get => _green8AChannel15; set => SetAndNotify(ref _green8AChannel15, value); }

        private ushort _green8BChannel16;
        public ushort Green8BChannel16 { get => _green8BChannel16; set => SetAndNotify(ref _green8BChannel16, value); }

        #endregion green

        #region blue

        private ushort _blue1AChannel1;
        public ushort Blue1AChannel1 { get => _blue1AChannel1; set => SetAndNotify(ref _blue1AChannel1, value); }

        private ushort _blue1BChannel2;
        public ushort Blue1BChannel2 { get => _blue1BChannel2; set => SetAndNotify(ref _blue1BChannel2, value); }

        private ushort _blue2AChannel3;
        public ushort Blue2AChannel3 { get => _blue2AChannel3; set => SetAndNotify(ref _blue2AChannel3, value); }

        private ushort _blue2BChannel4;
        public ushort Blue2BChannel4 { get => _blue2BChannel4; set => SetAndNotify(ref _blue2BChannel4, value); }

        private ushort _blue3AChannel5;
        public ushort Blue3AChannel5 { get => _blue3AChannel5; set => SetAndNotify(ref _blue3AChannel5, value); }

        private ushort _blue3BChannel6;
        public ushort Blue3BChannel6 { get => _blue3BChannel6; set => SetAndNotify(ref _blue3BChannel6, value); }

        private ushort _blue4AChannel7;
        public ushort Blue4AChannel7 { get => _blue4AChannel7; set => SetAndNotify(ref _blue4AChannel7, value); }

        private ushort _blue4BChannel8;
        public ushort Blue4BChannel8 { get => _blue4BChannel8; set => SetAndNotify(ref _blue4BChannel8, value); }

        private ushort _blue5AChannel9;
        public ushort Blue5AChannel9 { get => _blue5AChannel9; set => SetAndNotify(ref _blue5AChannel9, value); }

        private ushort _blue5BChannel10;
        public ushort Blue5BChannel10 { get => _blue5BChannel10; set => SetAndNotify(ref _blue5BChannel10, value); }

        private ushort _blue6AChannel11;
        public ushort Blue6AChannel11 { get => _blue6AChannel11; set => SetAndNotify(ref _blue6AChannel11, value); }

        private ushort _blue6BChannel12;
        public ushort Blue6BChannel12 { get => _blue6BChannel12; set => SetAndNotify(ref _blue6BChannel12, value); }

        private ushort _blue7AChannel13;
        public ushort Blue7AChannel13 { get => _blue7AChannel13; set => SetAndNotify(ref _blue7AChannel13, value); }

        private ushort _blue7BChannel14;
        public ushort Blue7BChannel14 { get => _blue7BChannel14; set => SetAndNotify(ref _blue7BChannel14, value); }

        private ushort _blue8AChannel15;
        public ushort Blue8AChannel15 { get => _blue8AChannel15; set => SetAndNotify(ref _blue8AChannel15, value); }

        private ushort _blue8BChannel16;
        public ushort Blue8BChannel16 { get => _blue8BChannel16; set => SetAndNotify(ref _blue8BChannel16, value); }

        #endregion blue

        private ushort _poeBank;
        public ushort PoeBank { get => _poeBank; set => SetAndNotify(ref _poeBank, value); }

        private ushort _poeId;
        public ushort PoeId { get => _poeId; set => SetAndNotify(ref _poeId, value); }

        private ushort _poePortEn;
        public ushort PoePortEn { get => _poePortEn; set => SetAndNotify(ref _poePortEn, value); }

        private ushort _poeInt;
        public ushort PoeInt { get => _poeInt; set => SetAndNotify(ref _poeInt, value); }

        private ushort _poeReset;
        public ushort PoeReset { get => _poeReset; set => SetAndNotify(ref _poeReset, value); }

        private ushort _poeMode;
        public ushort PoeMode { get => _poeMode; set => SetAndNotify(ref _poeMode, value); }

        private ushort _voltage3V3MeasCalibr;
        public ushort Voltage3V3MeasCalibr { get => _voltage3V3MeasCalibr; set => SetAndNotify(ref _voltage3V3MeasCalibr, value); }

        private ushort _voltage3V3Enable;
        public ushort Voltage3V3Enable { get => _voltage3V3Enable; set => SetAndNotify(ref _voltage3V3Enable, value); }

        private ushort _uartTestStart;
        /// <summary>
        /// UART test control (0 - stop, 1 - start)
        /// </summary>
        public ushort UartTestStart { get => _uartTestStart; set => SetAndNotify(ref _uartTestStart, value); }

        private ushort _uartTestResult;
        /// <summary>
        /// UART test status (0 - stop, 1 - running, 2 - ok, 3 - fail)
        /// </summary>
        public ushort UartTestResult { get => _uartTestResult; set => SetAndNotify(ref _uartTestResult, value); }


        private ushort _uartCh1Voltage;
        public ushort UartCh1Voltage { get => _uartCh1Voltage; set => SetAndNotify(ref _uartCh1Voltage, value); }

        private ushort _uartCh2Voltage;
        public ushort UartCh2Voltage { get => _uartCh2Voltage; set => SetAndNotify(ref _uartCh2Voltage, value); }

        private ushort _uartCh3Voltage;
        public ushort UartCh3Voltage { get => _uartCh3Voltage; set => SetAndNotify(ref _uartCh3Voltage, value); }

        private ushort _uartCh4Voltage;
        public ushort UartCh4Voltage { get => _uartCh4Voltage; set => SetAndNotify(ref _uartCh4Voltage, value); }

        private ushort _uartCh5Voltage;
        public ushort UartCh5Voltage { get => _uartCh5Voltage; set => SetAndNotify(ref _uartCh5Voltage, value); }

        private ushort _uartCh6Voltage;
        public ushort UartCh6Voltage { get => _uartCh6Voltage; set => SetAndNotify(ref _uartCh6Voltage, value); }

        private ushort _uartCh7Voltage;
        public ushort UartCh7Voltage { get => _uartCh7Voltage; set => SetAndNotify(ref _uartCh7Voltage, value); }

        private ushort _uartCh8Voltage;
        public ushort UartCh8Voltage { get => _uartCh8Voltage; set => SetAndNotify(ref _uartCh8Voltage, value); }

        private ushort _uartCh9Voltage;
        public ushort UartCh9Voltage { get => _uartCh9Voltage; set => SetAndNotify(ref _uartCh9Voltage, value); }

        private ushort _uartCh10Voltage;
        public ushort UartCh10Voltage { get => _uartCh10Voltage; set => SetAndNotify(ref _uartCh10Voltage, value); }

        private ushort _uartCh11Voltage;
        public ushort UartCh11Voltage { get => _uartCh11Voltage; set => SetAndNotify(ref _uartCh11Voltage, value); }

        private ushort _uartCh12Voltage;
        public ushort UartCh12Voltage { get => _uartCh12Voltage; set => SetAndNotify(ref _uartCh12Voltage, value); }

        private ushort _uartCh13Voltage;
        public ushort UartCh13Voltage { get => _uartCh13Voltage; set => SetAndNotify(ref _uartCh13Voltage, value); }

        private ushort _uartCh14Voltage;
        public ushort UartCh14Voltage { get => _uartCh14Voltage; set => SetAndNotify(ref _uartCh14Voltage, value); }

        private ushort _uartCh15Voltage;
        public ushort UartCh15Voltage { get => _uartCh15Voltage; set => SetAndNotify(ref _uartCh15Voltage, value); }

        private ushort _uartCh16Voltage;
        public ushort UartCh16Voltage { get => _uartCh16Voltage; set => SetAndNotify(ref _uartCh16Voltage, value); }


        #endregion базовые регистры


    }
}
