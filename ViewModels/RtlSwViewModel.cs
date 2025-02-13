using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows.Threading;
using System.Windows;
using HandyControl.Controls;
using System.Windows.Controls;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;
using Stylet;
using Modbus.Device;
using static RTL.Logger.Loggers;
using RTL.Logger;
using RTL.Models;
using RTL.Commands;
using System.Net.Http;
using Newtonsoft.Json;
using System.IO;
using System.Windows.Input;
using System.ComponentModel;
using System.Runtime.CompilerServices;


namespace RTL.ViewModels
{
    public class RtlSwViewModel : Screen
    {



        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


        #region логи
        private readonly Loggers _logger;
        public ObservableCollection<LogEntry> Logs => Loggers.LogMessages;
        private ListBox _logListBox;
        public void SetLogListBox(ListBox listBox)
        {
            _logListBox = listBox;
            if (_logListBox != null)
            {
                Logs.CollectionChanged += ScrollToEnd;
            }
        }
        private void ScrollToEnd(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (_logListBox != null && _logListBox.Items.Count > 0)
            {
                _logListBox.Dispatcher.BeginInvoke(() =>
                {
                    _logListBox.ScrollIntoView(_logListBox.Items[_logListBox.Items.Count - 1]);
                }, DispatcherPriority.Background);
            }
        }
        #endregion логи


        #region подключение к стенду



        public ICommand ConnectCommand { get; }


        private bool _isStandConnected;
        public bool IsStandConnected
        {
            get => _isStandConnected;
            set => SetAndNotify(ref _isStandConnected, value);
        }




        private async Task ToggleConnectionAsync()
        {
            if (IsStandConnected) // Если мы уже подключены, то отключаемся
            {
                await StopHardAsync();
                _logger.LogToUser("Ручное отключение от стенда - ОК", Loggers.LogLevel.Info);
                IsStandConnected = false;  // Обновляем состояние
                return;
            }

            _logger.LogToUser("Попытка подключения к стенду...", Loggers.LogLevel.Info);
            IsStandConnected = true; // Когда подключение начато, обновляем состояние
            try
            {
                if (!await TryLoadTestProfileAsync())
                {
                    _logger.LogToUser("Не удалось загрузить профиль тестирования.", Loggers.LogLevel.Error);
                    return;
                }

                if (!await TryConnectToServerAsync() && TestConfig.IsReportGenerationEnabled)
                {
                    _logger.LogToUser("Не удалось подключиться к серверу, результаты тестирования не будут отправлены.", Loggers.LogLevel.Warning);
                }

                if (!await TryInitializeModbusAsync ())
                {
                    _logger.LogToUser("Не удалось подключиться к модбасу", Loggers.LogLevel.Warning);
                }
                _ = MonitorStandAsync();
                IsStandConnected = true;  // Если все прошло успешно, отмечаем подключение
                _logger.LogToUser("Стенд успешно подключен!", Loggers.LogLevel.Success);
            }
            catch (Exception ex)
            {
                _logger.LogToUser($"Ошибка подключения: {ex.Message}", Loggers.LogLevel.Error);
                await StopHardAsync();
            }
        }





        #endregion подключение к стенду
        #region подключения модбас
        private IModbusSerialMaster _modbusMaster;
        private SerialPort _serialPortCom;
        private bool _isModbusConnected;
        public bool IsModbusConnected
        {
            get => _isModbusConnected;
            set => SetAndNotify(ref _isModbusConnected, value);
        }
        private async Task<bool> TryInitializeModbusAsync()
        {
            var startTime = DateTime.Now;
            const int retryInterval = 2000;
            const int timeout = 10000;
            const int responseTimeout = 10000;

            while ((DateTime.Now - startTime).TotalMilliseconds < timeout)
            {
                try
                {
                    _logger.LogToUser($"Попытка открыть {Properties.Settings.Default.ComSW}...", Loggers.LogLevel.Info);

                    DisconnectModbus(); // Закрываем порт перед подключением
                    await Task.Delay(500);

                    _serialPortCom = new SerialPort(Properties.Settings.Default.ComSW, 9600, Parity.None, 8, StopBits.One)
                    {
                        ReadTimeout = 3000,
                        WriteTimeout = 3000
                    };

                    _serialPortCom.Open();
                    _logger.LogToUser($"Порт {Properties.Settings.Default.ComSW} успешно открыт.", Loggers.LogLevel.Info);

                    _modbusMaster = ModbusSerialMaster.CreateRtu(_serialPortCom);
                    _modbusMaster.Transport.Retries = 3;
                    IsModbusConnected = true;

                    _logger.LogToUser("Попытка чтения регистра 0 для проверки типа стенда...", Loggers.LogLevel.Info);

                    var readTask = Task.Run(() =>
                    {
                        try
                        {
                            ushort standType = _modbusMaster.ReadHoldingRegisters(1, 0, 1)[0];
                            if (standType != 104)
                            {
                                _logger.LogToUser($"Ошибка: неверный тип стенда ({standType}). Ожидалось: 104 (STAND RTL-SW).", Loggers.LogLevel.Error);
                                return false;
                            }

                            ushort standSerial = _modbusMaster.ReadHoldingRegisters(1, 2300, 1)[0];
                            ReportModel.StandType = standType;
                            ReportModel.StandSerialNumber = standSerial;

                            _logger.LogToUser($"Тип стенда: {standType} (STAND RTL-SW), Серийный номер: {standSerial}", Loggers.LogLevel.Info);
                            return true;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogToUser($"Ошибка при чтении регистра: {ex.Message}", Loggers.LogLevel.Error);
                            return false;
                        }
                    });

                    if (await Task.WhenAny(readTask, Task.Delay(responseTimeout)) == readTask)
                    {
                        if (await readTask)
                        {
                            return true;
                        }
                    }
                    else
                    {
                        _logger.LogToUser("Ошибка: не получен ответ от устройства в течение 10 секунд.", Loggers.LogLevel.Error);
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    _logger.LogToUser($"Ошибка: доступ к {Properties.Settings.Default.ComSW} запрещен. Возможно, порт уже используется другой программой.", Loggers.LogLevel.Error);
                    await Task.Delay(3000);
                }
                catch (IOException ex)
                {
                    _logger.LogToUser($"Ошибка ввода-вывода: {ex.Message}. Возможно, устройство отключено.", Loggers.LogLevel.Warning);
                }
                catch (Exception ex)
                {
                    _logger.LogToUser($"Ошибка подключения к {Properties.Settings.Default.ComSW}: {ex.Message}", Loggers.LogLevel.Warning);
                }

                _logger.LogToUser("Повторная попытка подключения через 2 секунды...", Loggers.LogLevel.Warning);
                await Task.Delay(retryInterval);
            }

            _logger.LogToUser(
                $"Не удалось подключиться к стенду в течение 10 секунд.\n" +
                $"Проверьте:\n" +
                $"- Корректность выбора порта ({Properties.Settings.Default.ComSW})\n" +
                $"- Доступность устройства\n",
                Loggers.LogLevel.Error
            );

            return false;
        }


        private void DisconnectModbus()
        {
            try
            {
                if (_serialPortCom != null)
                {
                    if (_serialPortCom.IsOpen)
                    {
                        _serialPortCom.Close();
                        _logger.LogToUser("COM-порт закрыт", Loggers.LogLevel.Warning);
                    }
                    _serialPortCom.Dispose();
                    _serialPortCom = null;
                }

                _modbusMaster = null;
                IsModbusConnected = false;
                _logger.LogToUser("Modbus отключен", Loggers.LogLevel.Warning);
            }
            catch (Exception ex)
            {
                _logger.LogToUser($"Ошибка при отключении: {ex.Message}", Loggers.LogLevel.Error);
            }
        }


        public RelayCommand ToggleModbusConnectionCommand { get; }

        private async Task ToggleModbusConnection()
        {
            if (IsModbusConnected)
            {
                DisconnectPorts();
            }
            else
            {
                await TryInitializeModbusAsync();
            }
        }


        #endregion подключения модбас
        #region подключение к серверу
        private bool _isServerConnected;

        public bool IsServerConnected
        {
            get => _isServerConnected;
            set => SetAndNotify(ref _isServerConnected, value);
        }
        public RelayCommand ConnectToServerCommand { get; }
        private async Task<bool> TryConnectToServerAsync()
        {
            const string url = "http://iccid.fort-telecom.ru/api/Api.svc/ping";
            const int maxRetries = 3; // Количество повторных попыток
            const int delayMs = 2000; // Задержка между попытками (2 сек)

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    using (HttpClient client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) })
                    {
                        client.DefaultRequestHeaders.Add("User-Agent", "ftstand");

                        _logger.LogToUser($"Попытка {attempt}/{maxRetries}: подключение к серверу...", LogLevel.Info);

                        HttpResponseMessage response = await client.GetAsync(url);
                        string responseContent = await response.Content.ReadAsStringAsync();

                        if (response.IsSuccessStatusCode && !responseContent.Contains("error"))
                        {
                            _logger.LogToUser($"✅ Подключение к серверу успешно. Код: {response.StatusCode}", LogLevel.Success);
                            IsServerConnected = true;
                            return true;
                        }
                        else
                        {
                            _logger.LogToUser($"⚠️ Ошибка подключения. Код: {response.StatusCode}, Ответ: {responseContent}", LogLevel.Warning);
                        }
                    }
                }
                catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
                {
                    _logger.LogToUser($"⏳ Тайм-аут при подключении. Сервер не отвечает. Попробуйте позже.", LogLevel.Error);
                }
                catch (HttpRequestException ex)
                {
                    _logger.LogToUser($"🌐 Ошибка сети: {ex.Message}. Проверьте интернет-соединение.", LogLevel.Error);
                }
                catch (Exception ex)
                {
                    _logger.LogToUser($"❌ Ошибка подключения к серверу: {ex.Message}", LogLevel.Error);
                }

                if (attempt < maxRetries)
                {
                    _logger.LogToUser($"🔄 Повторная попытка через {delayMs / 1000} секунд...", LogLevel.Info);
                    await Task.Delay(delayMs);
                }
            }

            _logger.LogToUser($"❌ Все попытки подключения исчерпаны. Проверьте настройки сети.", LogLevel.Error);
            IsServerConnected = false;
            return false;
        }


        #endregion подключение к серверу
        #region Профиль тестирования
        private bool _isTestProfileLoaded;

        public bool IsTestProfileLoaded

        {
            get => _isTestProfileLoaded;
            set => SetAndNotify(ref _isTestProfileLoaded, value);
        }

        public RelayCommand LoadTestProfileCommand { get; }


        private ProfileTestModel _testConfig;

        public ProfileTestModel TestConfig
        {
            get => _testConfig;
            set => SetAndNotify(ref _testConfig, value);
        }
        private void ProfileTest_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // Логирование изменений
            Console.WriteLine($"Свойство {e.PropertyName} изменилось в ProfileTestModel");
        }

        private async Task<bool> TryLoadTestProfileAsync()
        {
            try
            {
                string testProfilePath = Properties.Settings.Default.RtlSwProfilePath;
                if (File.Exists(testProfilePath))
                {
                    string json = await File.ReadAllTextAsync(testProfilePath);
                    TestConfig = JsonConvert.DeserializeObject<ProfileTestModel>(json) ?? new ProfileTestModel();
                    OnPropertyChanged(nameof(TestConfig)); // ✅ Уведомляем View
                    IsTestProfileLoaded = true;
                    _logger.LogToUser($"Файл тестирования загружен: {testProfilePath}", LogLevel.Success);
                    return true;
                }
                else
                {
                    TestConfig = new ProfileTestModel();
                    OnPropertyChanged(nameof(TestConfig)); // ✅ Уведомляем View
                    IsTestProfileLoaded = false;
                    _logger.LogToUser($"Файл тестирования {testProfilePath} не найден.", LogLevel.Warning);
                    return false;
                }
            }
            catch (JsonException ex)
            {
                TestConfig = new ProfileTestModel();
                OnPropertyChanged(nameof(TestConfig)); // ✅ Уведомляем View
                IsTestProfileLoaded = false;
                _logger.LogToUser($"Ошибка обработки JSON: {ex.Message}", LogLevel.Error);
                return false;
            }
            catch (Exception ex)
            {
                TestConfig = new ProfileTestModel();
                OnPropertyChanged(nameof(TestConfig)); // ✅ Уведомляем View
                IsTestProfileLoaded = false;
                _logger.LogToUser($"Ошибка при загрузке профиля тестирования: {ex.Message}", LogLevel.Error);
                return false;
            }
        }


        #endregion Профиль тестирования

        #region мониторинг
        public StandRegistersModel StandRegisters { get; } = new StandRegistersModel(); // Создаём экземпляр

        private async Task MonitorStandAsync()
        {
            while (IsStandConnected)
            {
                try
                {
                    // Читаем регистры Modbus (85 регистров, начиная с 2300)
                    var registers = await _modbusMaster.ReadHoldingRegistersAsync(1, 2300, 85);
                    #region обновление значений
                    // Обновляем значения в модели StandRegistersModel
                    StandRegisters.V52Out = registers[1];
                    StandRegisters.V55Out = registers[2];
                    StandRegisters.Sensor1Out = registers[3];
                    StandRegisters.Sensor2Out = registers[4];
                    StandRegisters.TamperOut = registers[5];
                    StandRegisters.RelayIn = registers[6];
                    StandRegisters.ResetOut = registers[7];
                    StandRegisters.BootOut = registers[8];
                    // --------------------------------------- друзья в гуи 
                    StandRegisters.V52 = registers[9];
                    StandRegisters.V55 = registers[10];
                    StandRegisters.VOut = registers[11];
                    StandRegisters.Ref2048 = registers[12];
                    StandRegisters.V12 = registers[13];
                    StandRegisters.V3_3 = registers[14];
                    StandRegisters.V1_5 = registers[15];
                    StandRegisters.V1_1 = registers[16];
                    StandRegisters.CR2032 = registers[17];
                    StandRegisters.CR2032_CPU = registers[18];
                    StandRegisters.Status_Tamper = registers[19];
                    StandRegisters.Status_Tamper_Led = registers[20];
                    // --------------------------------------- друзья в гуи 
                    // --------------------------------------- Кнопки и статусы тестов
                    StandRegisters.RunBtn = registers[21]; //------------------------------------------------------------------- КНОПКА RUN
                    StandRegisters.NextBtn = registers[22];
                    StandRegisters.K5Stage1Start = registers[23];
                    StandRegisters.K5Stage1Status = registers[24];
                    StandRegisters.K5Stage2Start = registers[25];
                    StandRegisters.K5Stage2Status = registers[26];
                    StandRegisters.K5Stage3Start = registers[27];
                    StandRegisters.K5Stage3Status = registers[28];
                    StandRegisters.K5TestDelay = registers[29];

                    StandRegisters.VCCStart = registers[30];
                    StandRegisters.VCCTestStatus = registers[31];
                    StandRegisters.VCCTestDelay = registers[32];

                    // --------------------------------------- Минимальные, максимальные и калибровочные значения
                    StandRegisters.V52Min = registers[33];
                    StandRegisters.V52Max = registers[34];
                    StandRegisters.V52Calibr = registers[35];

                    StandRegisters.V55Min = registers[36];
                    StandRegisters.V55Max = registers[37];
                    StandRegisters.V55Calibr = registers[38];

                    StandRegisters.VOutVmainMin = registers[39];
                    StandRegisters.VOutVmainMax = registers[40];
                    StandRegisters.VOutVresMin = registers[41];
                    StandRegisters.VOutVresMax = registers[42];
                    StandRegisters.VOutCalibr = registers[43];

                    StandRegisters.Ref2048Min = registers[44];
                    StandRegisters.Ref2048Max = registers[45];
                    StandRegisters.Ref2048Calibr = registers[46];

                    StandRegisters.V12Min = registers[47];
                    StandRegisters.V12Max = registers[48];
                    StandRegisters.V12Calibr = registers[49];

                    StandRegisters.V3_3Min = registers[50];
                    StandRegisters.V3_3Max = registers[51];
                    StandRegisters.V3_3Calibr = registers[52];

                    StandRegisters.V1_5Min = registers[53];
                    StandRegisters.V1_5Max = registers[54];
                    StandRegisters.V1_5Calibr = registers[55];

                    StandRegisters.V1_1Min = registers[56];
                    StandRegisters.V1_1Max = registers[57];
                    StandRegisters.V1_1Calibr = registers[58];

                    StandRegisters.CR2032Min = registers[59];
                    StandRegisters.CR2032Max = registers[60];
                    StandRegisters.CR2032Calibr = registers[61];

                    StandRegisters.CR2032_CPUMin = registers[62];
                    StandRegisters.CR2032_CPUMax = registers[63];
                    StandRegisters.CR2032_CPUCalibr = registers[64];

                    StandRegisters.TamperStatusMin = registers[65];
                    StandRegisters.TamperStatusMax = registers[66];
                    StandRegisters.TamperStatusCalibr = registers[67];

                    StandRegisters.TamperLedMin = registers[68];
                    StandRegisters.TamperLedMax = registers[69];
                    StandRegisters.TamperLedCalibr = registers[70];

                    // --------------------------------------- Отчёты
                    StandRegisters.V52Report = registers[71];
                    StandRegisters.V55Report = registers[72];
                    StandRegisters.VOutReport = registers[73];
                    StandRegisters.Ref2048Report = registers[74];
                    StandRegisters.V12Report = registers[75];
                    StandRegisters.V3_3Report = registers[76];
                    StandRegisters.V1_5Report = registers[77];
                    StandRegisters.V1_1Report = registers[78];
                    StandRegisters.CR2032Report = registers[79];
                    StandRegisters.CR2032_CPUReport = registers[80];
                    StandRegisters.TamperLedReport = registers[81];
                    StandRegisters.TamperReport = registers[82];

                    // --------------------------------------- Прочие регистры
                    StandRegisters.RS485Enable = registers[83];
                    StandRegisters.RS485RxOK = registers[84];
                    #endregion обновление значений

                }
                catch (Exception ex)
                {
                    _logger.LogToUser($"Ошибка при чтении регистров Modbus: {ex.Message}", Loggers.LogLevel.Error);
                }

                await Task.Delay(1000); // Ожидание 1 секунда перед следующим опросом
            }
        }




        #endregion мониторинг

        public RtlSwViewModel(Loggers logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger.Log("RtlSwViewModel инициализирован", Loggers.LogLevel.Success);

            ToggleModbusConnectionCommand = new RelayCommand(async () => await ToggleModbusConnection(), CanExecuteCommand);
            ConnectToServerCommand = new RelayCommand(async () => await TryConnectToServerAsync(), CanExecuteCommand);
            LoadTestProfileCommand = new RelayCommand(async () => await TryLoadTestProfileAsync(), CanExecuteCommand);

            if (TestConfig != null)
            {
                TestConfig.PropertyChanged += ProfileTest_PropertyChanged;
            }
            else
            {
                _logger.Log("Ошибка: TestConfig не инициализирован!", Loggers.LogLevel.Error);
            }

            // Кнопка "Подключиться к стенду"
            ConnectCommand = new AsyncRelayCommand(ToggleConnectionAsync);
        }

        private bool CanExecuteCommand() => !IsStandConnected;




        public void DisconnectPorts()
        {
            try
            {
                if (_serialPortCom != null)
                {
                    if (_serialPortCom.IsOpen)
                    {
                        _serialPortCom.Close();
                        _logger.LogToUser($"Порт {Properties.Settings.Default.ComSW} уже открыт, закрыт и пробуем подключиться снова.", Loggers.LogLevel.Info);
                    }
                    _serialPortCom.Dispose();
                }

                IsModbusConnected = false;

                _logger.LogToUser("COM-порты отключены", Loggers.LogLevel.Info);
            }
            catch (Exception ex)
            {
                _logger.LogToUser($"Ошибка при отключении портов: {ex.Message}", Loggers.LogLevel.Error);
            }
        }

        public void WriteToRegisterWithRetry(ushort register, ushort value, int retries = 3)
        {
            for (int i = 0; i < retries; i++)
            {
                try
                {
                    _modbusMaster.WriteSingleRegister(1, register, value);
                    _logger.Log($"{register} = {value}", LogLevel.Debug);
                    return;
                }
                catch (Exception ex)
                {
                    _logger.Log($"Попытка {i + 1} записи в регистр {register} не удалась: {ex.Message}", LogLevel.Warning);
                    Thread.Sleep(1000);
                }
            }
            _logger.Log($"Не удалось записать значение {value} в регистр {register} после {retries} попыток.", LogLevel.Error);
            throw new Exception($"Не удалось записать значение {value} в регистр {register} после {retries} попыток.");
        }


        private async Task StopHardAsync()
        {
            _logger.LogToUser("Отключение стенда...", Loggers.LogLevel.Warning);

            try
            {
                await Task.Delay(5000);

                _serialPortCom?.Close();
                _serialPortCom?.Dispose();
                _modbusMaster?.Dispose();
                IsModbusConnected = false;
                IsServerConnected = false;
                IsStandConnected = false;

                _logger.LogToUser("Стенд успешно отключен.", Loggers.LogLevel.Success);
            }
            catch (Exception ex)
            {
                _logger.LogToUser($"Ошибка при отключении стенда: {ex.Message}", Loggers.LogLevel.Error);
            }
        }


        protected override void OnClose()
        {
            _logger.LogToUser("Закрытие приложения, отключение портов...", Loggers.LogLevel.Info);

            try
            {
                if (_serialPortCom != null)
                {
                    if (_serialPortCom.IsOpen)
                    {
                        _serialPortCom.Close();
                        _serialPortCom.Dispose();
                    }
                }



                _modbusMaster?.Dispose();
                IsModbusConnected = false;


                _logger.LogToUser("COM-порты успешно закрыты.", Loggers.LogLevel.Success);
            }
            catch (Exception ex)
            {
                _logger.LogToUser($"Ошибка при закрытии портов: {ex.Message}", Loggers.LogLevel.Error);
            }

            base.OnClose();
        }

    }
}