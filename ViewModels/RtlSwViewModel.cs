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
using System.Diagnostics;
using WindowsInput.Native;
using WindowsInput;
using System.Runtime.InteropServices;
using System.Text;
namespace RTL.ViewModels
{
    public class RtlSwViewModel : Screen
    {
        private int _progressValue;
        public int ProgressValue
        {
            get => _progressValue;
            set => SetAndNotify(ref _progressValue, value);
        }



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

        private bool CanExecuteCommand() => !IsStandConnected;

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
                await DisconnectStand();
                _logger.LogToUser("Ручное отключение от стенда - ОК", Loggers.LogLevel.Info);
                IsStandConnected = false;  // Обновляем состояние
                IsTestRunning = false;
                return;
            }

            _logger.LogToUser("Попытка подключения к стенду...", Loggers.LogLevel.Info);
            IsStandConnected = true; // Когда подключение начато, обновляем состояние
            try
            {
                _isCancellationRequested = false;


                if (!await TryLoadTestProfileAsync())
                {
                    _logger.LogToUser("Не удалось загрузить профиль тестирования.", Loggers.LogLevel.Error);
                    IsStandConnected = false;
                    return;
                }

                if (!await TryConnectToServerAsync() && TestConfig.IsReportGenerationEnabled)
                {
                    _logger.LogToUser("Не удалось подключиться к серверу, результаты тестирования не будут отправлены.", Loggers.LogLevel.Warning);

                }

                if (!await TryInitializeModbusAsync())
                {
                    _logger.LogToUser("Не удалось подключиться к стенду", Loggers.LogLevel.Warning);
                    IsStandConnected = false;
                    return;
                }

                Task.Run(() => MonitorStandAsync());

                IsStandConnected = true;  // Если все прошло успешно, отмечаем подключение
                _logger.LogToUser("Стенд успешно подключен!", Loggers.LogLevel.Success);
            }
            catch (Exception ex)
            {
                _logger.LogToUser($"Ошибка подключения: {ex.Message}", Loggers.LogLevel.Error);
                await DisconnectStand();
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

            string[] availablePorts = SerialPort.GetPortNames();
            if (availablePorts.Length == 0)
            {
                _logger.LogToUser("Ошибка: В системе нет доступных COM-портов.", Loggers.LogLevel.Error);
                return false;
            }

            while ((DateTime.Now - startTime).TotalMilliseconds < timeout)
            {
                if (_isCancellationRequested) // Проверяем, запросили ли мы отмену
                {
                    _logger.LogToUser("Остановлена попытка подключения к Modbus из-за отключения стенда.", Loggers.LogLevel.Warning);
                    return false;
                }

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
                await CloseConnections();
            }
            else
            {
                await TryInitializeModbusAsync();
            }
        }


        #endregion подключения модбас
        #region DUT

        private bool _isDutConnected;
        public bool IsDutConnected
        {
            get => _isDutConnected;
            set => SetAndNotify(ref _isDutConnected, value);
        }

        private SerialPort _serialPortDut;

        public async Task<bool> WaitForDUTReadyAsync(CancellationToken cancellationToken, int noLogTimeoutSeconds = 100, int maxWaitTimeSeconds = 1200)
        {
            WriteToRegisterWithRetry(2301, 0);
            WriteToRegisterWithRetry(2301, 1);
            await Task.Delay(2000, cancellationToken);

            try
            {
                if (!await TryInitializeDutComPortAsync(cancellationToken))
                {
                    throw new InvalidOperationException("COM-порт для DUT не открыт.");
                }

                _logger.LogToUser("Ожидание завершения загрузки DUT...", LogLevel.Info);

                DateTime startTime = DateTime.Now;
                DateTime lastLogTime = DateTime.Now;
                bool successPromptShown = false;

                while ((DateTime.Now - startTime).TotalSeconds < maxWaitTimeSeconds)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        _logger.LogToUser("Ожидание DUT отменено.", LogLevel.Warning);
                        _serialPortDut?.Close();
                        return false;
                    }

                    await Task.Delay(500, cancellationToken); // Проверка каждые 500 мс

                    if (_serialPortDut.BytesToRead > 0)
                    {
                        string data = _serialPortDut.ReadExisting();
                        _logger.Log($"Получены данные: {data.Trim()}", LogLevel.Debug);
                        lastLogTime = DateTime.Now;

                        if (data.Contains("root@TFortis:/#"))
                        {
                            _logger.Log("Обнаружено приглашение к вводу. DUT готов к работе.", LogLevel.Info);
                            _logger.LogToUser("DUT готов к работе.", LogLevel.Success);
                            return true;
                        }
                    }
                    else if ((DateTime.Now - lastLogTime).TotalSeconds > noLogTimeoutSeconds && !successPromptShown)
                    {
                        _serialPortDut.Write("\n");
                        _logger.Log("Нет новых логов. Отправлена команда \\n для проверки готовности.", LogLevel.Debug);
                        successPromptShown = true;
                    }
                }

                _logger.LogToUser($"DUT не завершил загрузку за {maxWaitTimeSeconds} секунд.", LogLevel.Error);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogToUser($"Ошибка при ожидании загрузки DUT: {ex.Message}", LogLevel.Error);
                return false;
            }
        }

        public async Task<bool> TryInitializeDutComPortAsync(CancellationToken cancellationToken)
        {
            try
            {
                string dutPort = Properties.Settings.Default.DutSW;
                if (string.IsNullOrEmpty(dutPort))
                {
                    _logger.LogToUser("DUT COM-порт не задан в настройках.", LogLevel.Error);
                    return false;
                }

                _serialPortDut = new SerialPort(dutPort, 115200, Parity.None, 8, StopBits.One)
                {
                    ReadTimeout = 3000
                };
                _serialPortDut.Open();
                _logger.LogToUser($"Подключение к DUT через {dutPort} успешно.", LogLevel.Info);
                IsDutConnected = true;
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogToUser($"Ошибка инициализации USB-COM порта для DUT: {ex.Message}", LogLevel.Error);

                try
                {
                    _serialPortDut?.Close();
                    _logger.Log($"COM-порт закрыт после ошибки инициализации.", LogLevel.Info);
                }
                catch (Exception closeEx)
                {
                    _logger.Log($"Ошибка закрытия COM-порта: {closeEx.Message}", LogLevel.Warning);
                }
                IsDutConnected = false;
                return false;
            }
        }



        #endregion DUT
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
        public ReportModel Report { get; } = new ReportModel(); // создаем репорт 

        private CancellationTokenSource _testCancellationTokenSource;


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
                    if (StandRegisters.RunBtn == 1 && !IsTestRunning && !_testCompleted)
                    {
                        _testCancellationTokenSource?.Cancel();
                        _testCancellationTokenSource = new CancellationTokenSource();

                        await PrepareStandForTestingAsync();
                        _ = Task.Run(() => StartTestingAsync(_testCancellationTokenSource.Token));

                        _testCompleted = true; // Устанавливаем флаг, чтобы тест не запускался повторно
                    }
                    else if (StandRegisters.RunBtn == 0)
                    {
                        _testCompleted = false; // Сбрасываем флаг, разрешая новый запуск тестов
                    }
                    else if (StandRegisters.RunBtn == 0 && IsTestRunning)
                    {
                        _logger.LogToUser("Тестирование прервано пользователем.", Loggers.LogLevel.Warning);
                        _testCancellationTokenSource?.Cancel();
                        IsTestRunning = false;
                        await StopHard();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogToUser($"Ошибка при чтении регистров Modbus: {ex.Message}", Loggers.LogLevel.Error);
                }

                await Task.Delay(1000);
            }
        }



        private bool _isTestRunning;
        public bool IsTestRunning
        {
            get => _isTestRunning;
            set => SetAndNotify(ref _isTestRunning, value);
        }
        private bool _testCompleted = false; // Флаг завершения теста



        #endregion мониторинг
        #region тестирование

        private bool _isCancellationRequested;

        private async Task<bool> PrepareStandForTestingAsync()
        {
            try
            {
                WriteToRegisterWithRetry(2301, 0);
                WriteToRegisterWithRetry(2302, 0);
                WriteToRegisterWithRetry(2303, 0);
                WriteToRegisterWithRetry(2304, 0);
                WriteToRegisterWithRetry(2305, 0);
                WriteToRegisterWithRetry(2307, 0);
                WriteToRegisterWithRetry(2308, 0);
                WriteToRegisterWithRetry(2329, TestConfig.K5TestDelay);
                WriteToRegisterWithRetry(2332, TestConfig.VccStartDelay);
                WriteToRegisterWithRetry(2333, TestConfig.K5_52V_Min);
                WriteToRegisterWithRetry(2334, TestConfig.K5_52V_Max);
                WriteToRegisterWithRetry(2336, TestConfig.K5_55V_Min);
                WriteToRegisterWithRetry(2337, TestConfig.K5_55V_Max);
                WriteToRegisterWithRetry(2339, TestConfig.VoutMin);
                WriteToRegisterWithRetry(2340, TestConfig.VoutMax);
                WriteToRegisterWithRetry(2341, TestConfig.VoutVresMin);
                WriteToRegisterWithRetry(2342, TestConfig.VoutVresMax);
                WriteToRegisterWithRetry(2344, TestConfig.VrefMin);
                WriteToRegisterWithRetry(2345, TestConfig.VrefMax);
                WriteToRegisterWithRetry(2347, TestConfig.V12Min);
                WriteToRegisterWithRetry(2348, TestConfig.V12Max);
                WriteToRegisterWithRetry(2350, TestConfig.Vcc3V3Min);
                WriteToRegisterWithRetry(2351, TestConfig.Vcc3V3Max);
                WriteToRegisterWithRetry(2353, TestConfig.Vcc1V5Min);
                WriteToRegisterWithRetry(2354, TestConfig.Vcc1V5Max);
                WriteToRegisterWithRetry(2356, TestConfig.Vcc1V1Min);
                WriteToRegisterWithRetry(2357, TestConfig.Vcc1V1Max);
                WriteToRegisterWithRetry(2359, TestConfig.CR2032Min);
                WriteToRegisterWithRetry(2360, TestConfig.CR2032Max);
                WriteToRegisterWithRetry(2362, TestConfig.CR2032CpuMin);
                WriteToRegisterWithRetry(2363, TestConfig.CR2032CpuMax);
                WriteToRegisterWithRetry(2365, TestConfig.DutTamperStatusMin); // TAMPER_STATUS_MIN
                WriteToRegisterWithRetry(2366, TestConfig.DutTamperStatusMax); // TAMPER_STATUS_MAX
                WriteToRegisterWithRetry(2368, TestConfig.DutTamperLedMin);
                WriteToRegisterWithRetry(2369, TestConfig.DutTamperLedMax);

                ProgressValue = 0;


                // Инициализируем новый отчёт
                //ReportGenerator.InitializeNewReportFile(Config.ReportPath);

                await Task.Delay(500);
                _logger.LogToUser("Стенд подготовлен для тестирования.", LogLevel.Info);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogToUser($"Ошибка подготовки стенда: {ex.Message}", LogLevel.Error);
                return false;
            }
        }

        private async Task<bool> StartTestingAsync(CancellationToken cancellationToken)
        {
            try
            {
                IsTestRunning = true;
                ProgressValue += 5;
                // Подтест 1: VMAIN
                if (!await RunSubTestK5Async(2323, () => StandRegisters.K5Stage1Status, "VMAIN", ReportModel.Stage1K5, cancellationToken))
                {
                    await StopHard();
                    return false;
                }
                if (cancellationToken.IsCancellationRequested) return false;
                ProgressValue += 5;
                // Подтест 2: VMAIN + VRES
                if (!await RunSubTestK5Async(2325, () => StandRegisters.K5Stage2Status, "VMAIN + VRES", ReportModel.Stage2K5, cancellationToken))
                {
                    await StopHard();
                    return false;
                }
                if (cancellationToken.IsCancellationRequested) return false;
                ProgressValue += 5;
                // Подтест 3: VRES
                if (!await RunSubTestK5Async(2327, () => StandRegisters.K5Stage3Status, "VRES", ReportModel.Stage3K5, cancellationToken))
                {
                    await StopHard();
                    return false;
                }
                if (cancellationToken.IsCancellationRequested) return false;
                ProgressValue += 5;
                // Подтест 2: VMAIN + VRES
                if (!await RunSubTestK5Async(2325, () => StandRegisters.K5Stage2Status, "VMAIN + VRES", ReportModel.Stage2K5, cancellationToken))
                {
                    await StopHard();
                    return false;
                }
                if (cancellationToken.IsCancellationRequested) return false;
                ProgressValue += 5;
                // VMAIN 2 
                if (!await RunSubTestK5Async(2323, () => StandRegisters.K5Stage1Status, "VMAIN", ReportModel.Stage4K5, cancellationToken))
                {
                    await StopHard();
                    return false;
                }
                if (cancellationToken.IsCancellationRequested) return false;


                IsK5TestPassed = true;
                ProgressValue += 5;
                //  VCC
                if (!await RunVCCTestAsync(cancellationToken))
                {
                    _logger.LogToUser("Тест VCC завершен с ошибкой.", LogLevel.Error);
                    await StopHard();
                    return false;
                }
                ProgressValue += 5;
                // FLASH прошивка
                if (!await StartProgrammingAsync(cancellationToken))
                {
                    _logger.LogToUser("Прошивка завершена с ошибкой", LogLevel.Error);
                    await StopHard();
                    return false;
                }
                ProgressValue += 5; ///--------------------------------------------------------------возможно убрать 
                // DUT
                if (IsDutSelfTestEnabled)
                {
                    _logger.LogToUser("Запуск самотестирования DUT...", LogLevel.Info);
                    if (!await RunDutSelfTestAsync(cancellationToken))
                    {
                        _logger.LogToUser("Тестирование DUT завершилось с ошибкой.", LogLevel.Error);
                        await StopHard();
                        return false;
                    }
                }
                else
                {
                    _logger.LogToUser("Тестирование DUT отключено, пропускаем.", LogLevel.Info);
                }

                await StopHard();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogToUser($"Ошибка во время тестирования: {ex.Message}", LogLevel.Error);
                await StopHard();
                return false;
            }
            finally
            {
                IsTestRunning = false;
            }
        }



        #region K5

        private async Task<bool> RunSubTestK5Async(ushort startRegister, Func<ushort> getStatus, string testName, StageK5TestReport report, CancellationToken cancellationToken)
        {
            await Task.Delay(2000);
            try
            {
                if (!TestConfig.IsK5TestEnabled)
                {
                    _logger.LogToUser($"Тест {testName} пропущен (отключен в профиле).", LogLevel.Warning);
                    return true;
                }

                _logger.LogToUser($"Тест {testName} запущен...", LogLevel.Info);
                WriteToRegisterWithRetry(startRegister, 1);

                while (getStatus() != 1)
                {
                    if (cancellationToken.IsCancellationRequested || StandRegisters.RunBtn == 0 )
                    {
                        _logger.LogToUser($"Тест {testName} прерван (кнопка RUN переведена в положение 0).", LogLevel.Warning);
                        return false;
                    }
                    await Task.Delay(500);
                }

                _logger.LogToUser($"Ожидание завершения теста {testName}...", LogLevel.Debug);
                while (true)
                {
                    if (cancellationToken.IsCancellationRequested || StandRegisters.RunBtn == 0 )
                    {
                        _logger.LogToUser($"Тест {testName} прерван (кнопка RUN переведена в положение 0).", LogLevel.Warning);
                        return false;
                    }

                    await Task.Delay(2000);
                    var status = getStatus();
                    if (status == 2 || status == 3)
                    {
                        bool success = status == 2;

                        report.ResultK5 = success;
                        report.V52Report = StandRegisters.V52Report;
                        report.V55Report = StandRegisters.V55Report;
                        report.VOUTReport = StandRegisters.VOutReport;
                        report.V2048Report = StandRegisters.Ref2048Report;
                        report.V12Report = StandRegisters.V12Report;

                        _logger.LogToUser(
                            $"K5 {testName}: {success} {Environment.NewLine}" +
                            $"55V={report.V55Report}{Environment.NewLine}" +
                            $"52V={report.V52Report}{Environment.NewLine}" +
                            $"Vout={report.VOUTReport}{Environment.NewLine}" +
                            $"12V={report.V12Report}{Environment.NewLine}" +
                            $"Vref={report.V2048Report}",
                            success ? LogLevel.Success : LogLevel.Error
                        );

                        return success;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogToUser($"Ошибка во время теста {testName}: {ex.Message}", LogLevel.Error);
                return false;
            }
        }

        private bool _isK5TestPassed;
        public bool IsK5TestPassed
        {
            get => _isK5TestPassed;
            set => SetAndNotify(ref _isK5TestPassed, value);
        }


        #endregion K5
        #region VCC
        private async Task<bool> RunVCCTestAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (!TestConfig.IsVccTestEnabled)
                {
                    _logger.LogToUser("Тест VCC пропущен (отключен в конфигурации).", LogLevel.Warning);
                    return true;
                }

                _logger.LogToUser("Тест VCC запущен...", LogLevel.Info);
            restartTest:
                WriteToRegisterWithRetry(2330, 1);

                // Ожидание запуска теста
                while (StandRegisters.VCCTestStatus == 0)
                {
                    if (cancellationToken.IsCancellationRequested || StandRegisters.RunBtn == 0)
                    {
                        _logger.LogToUser("Тест VCC прерван (кнопка RUN переведена в положение 0).", LogLevel.Warning);
                        return false;
                    }
                    await Task.Delay(500, cancellationToken);
                }

                _logger.LogToUser("Ожидание завершения теста VCC...", LogLevel.Debug);

                // Мониторинг завершения теста
                while (true)
                {
                    if (cancellationToken.IsCancellationRequested || StandRegisters.RunBtn == 0)
                    {
                        _logger.LogToUser("Тест VCC прерван (кнопка RUN переведена в положение 0).", LogLevel.Warning);
                        return false;
                    }

                    await Task.Delay(5000, cancellationToken);
                    var status = StandRegisters.VCCTestStatus;

                    if (status == 2 || status == 3) // 2 - Успешно, 3 - Ошибка
                    {
                        bool success = status == 2;

                        _logger.LogToUser(
                            $"VCC Тест: {(success ? "Успешно" : "Ошибка")}; " +
                            $"3.3V={StandRegisters.V3_3Report}; 1.5V={StandRegisters.V1_5Report}; " +
                            $"1.1V={StandRegisters.V1_1Report}; CR2032={StandRegisters.CR2032Report}; " +
                            $"CR2032 CPU={StandRegisters.CR2032_CPUReport}",
                            success ? LogLevel.Success : LogLevel.Error
                        );

                        if (!success)
                        {
                            bool shouldRestart = (StandRegisters.V3_3Report == 0 ||
                                                  (StandRegisters.V3_3Report >= TestConfig.Vcc3V3Min && StandRegisters.V3_3Report <= TestConfig.Vcc3V3Max)) &&
                                                 (StandRegisters.V1_5Report == 0 ||
                                                  (StandRegisters.V1_5Report >= TestConfig.Vcc1V5Min && StandRegisters.V1_5Report <= TestConfig.Vcc1V5Max)) &&
                                                 (StandRegisters.V1_1Report == 0 ||
                                                  (StandRegisters.V1_1Report >= TestConfig.Vcc1V1Min && StandRegisters.V1_1Report <= TestConfig.Vcc1V1Max)) &&
                                                 (StandRegisters.CR2032Report == 0 ||
                                                  (StandRegisters.CR2032Report >= TestConfig.CR2032Min && StandRegisters.CR2032Report <= TestConfig.CR2032Max)) &&
                                                 (StandRegisters.CR2032_CPUReport == 0 ||
                                                  (StandRegisters.CR2032_CPUReport >= TestConfig.CR2032CpuMin && StandRegisters.CR2032_CPUReport <= TestConfig.CR2032CpuMax));

                            if (shouldRestart)
                            {
                                _logger.LogToUser("Тест VCC не прошел, но значения в допустимых пределах. Повторный запуск...", LogLevel.Warning);
                                goto restartTest;
                            }
                        }

                        return success;
                    }
                }
            }
            catch (TaskCanceledException)
            {
                _logger.LogToUser("Тест VCC отменён.", LogLevel.Warning);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogToUser($"Ошибка во время теста VCC: {ex.Message}", LogLevel.Error);
                return false;
            }
        }





        #endregion VCC
        #region прошивка
        public async Task<bool> StartProgrammingAsync(CancellationToken cancellationToken)
        {
            if (!TestConfig.IsFlashProgrammingEnabled)
            {
                _logger.LogToUser("Прошивка FLASH отключена в настройках.", LogLevel.Warning);
                return true;
            }

            WriteToRegisterWithRetry(2301, 1);

            string programPath = Properties.Settings.Default.FlashProgramPath;
            string projectPath = Properties.Settings.Default.FlashFirmwarePath;
            int delay = 180000;

            if (string.IsNullOrWhiteSpace(programPath) || !File.Exists(programPath))
            {
                _logger.LogToUser($"Программа для прошивки не найдена: {programPath}", LogLevel.Error);
                return false;
            }

            if (string.IsNullOrWhiteSpace(projectPath) || !File.Exists(projectPath))
            {
                _logger.LogToUser($"Файл проекта для прошивки не найден: {projectPath}", LogLevel.Error);
                return false;
            }

            Process programProcess = null;

            try
            {
                if (cancellationToken.IsCancellationRequested || StandRegisters.RunBtn == 0)
                    throw new OperationCanceledException("Прошивка отменена пользователем.");

                programProcess = Process.GetProcessesByName("Xgpro").FirstOrDefault();
                if (programProcess == null)
                {
                    _logger.LogToUser("Запуск программы прошивки...", LogLevel.Info);
                    programProcess = Process.Start(programPath);
                    await Task.Delay(5000, cancellationToken);
                }
                else
                {
                    _logger.LogToUser("Программа уже запущена. Переключение фокуса...", LogLevel.Info);
                }

                if (cancellationToken.IsCancellationRequested || StandRegisters.RunBtn == 0)
                    throw new OperationCanceledException("Прошивка отменена пользователем.");

                IntPtr hWnd = programProcess.MainWindowHandle;
                if (hWnd == IntPtr.Zero)
                {
                    _logger.LogToUser("Ошибка: Не удалось найти главное окно программы.", LogLevel.Error);
                    return false;
                }

                SetForegroundWindow(hWnd);

                InputSimulator sim = new InputSimulator();

                sim.Keyboard.ModifiedKeyStroke(VirtualKeyCode.MENU, VirtualKeyCode.VK_P);
                await Task.Delay(500, cancellationToken);
                sim.Keyboard.KeyPress(VirtualKeyCode.VK_O);
                await Task.Delay(2000, cancellationToken);

                if (cancellationToken.IsCancellationRequested || StandRegisters.RunBtn == 0)
                    throw new OperationCanceledException("Прошивка отменена пользователем.");

                string fileName = Path.GetFileName(projectPath);
                _logger.LogToUser($"Вставка имени файла: {fileName}", LogLevel.Debug);

                try
                {
                    var staThread = new Thread(() =>
                    {
                        try
                        {
                            Clipboard.SetText(fileName);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogToUser($"Ошибка при установке текста в буфер обмена: {ex.Message}", LogLevel.Error);
                        }
                    });

                    staThread.SetApartmentState(ApartmentState.STA);
                    staThread.Start();
                    staThread.Join();

                    sim.Keyboard.ModifiedKeyStroke(VirtualKeyCode.CONTROL, VirtualKeyCode.VK_V);
                    await Task.Delay(500, cancellationToken);
                    sim.Keyboard.KeyPress(VirtualKeyCode.RETURN);
                }
                catch (Exception ex)
                {
                    _logger.LogToUser($"Ошибка при вставке пути файла: {ex.Message}", LogLevel.Error);
                    return false;
                }

                await Task.Delay(5000, cancellationToken);
                sim.Keyboard.KeyPress(VirtualKeyCode.RETURN);

                sim.Keyboard.ModifiedKeyStroke(VirtualKeyCode.MENU, VirtualKeyCode.VK_D);
                await Task.Delay(200, cancellationToken);
                sim.Keyboard.KeyPress(VirtualKeyCode.VK_P);
                await Task.Delay(5000, cancellationToken);

                if (cancellationToken.IsCancellationRequested || StandRegisters.RunBtn == 0)
                    throw new OperationCanceledException("Прошивка отменена пользователем.");

                sim.Keyboard.KeyPress(VirtualKeyCode.RETURN);
                await Task.Delay(500, cancellationToken);
                sim.Keyboard.KeyPress(VirtualKeyCode.RETURN);

                _logger.LogToUser("Прошивка запущена. Ожидание завершения...", LogLevel.Info);

                for (int i = 0; i < delay / 1000; i++)
                {
                    if (cancellationToken.IsCancellationRequested || StandRegisters.RunBtn == 0)
                        throw new OperationCanceledException("Прошивка отменена пользователем.");

                    await Task.Delay(1000, cancellationToken);
                }

                _logger.LogToUser("Прошивка завершена. Закрытие программы прошивки...", LogLevel.Info);
                if (programProcess != null && !programProcess.HasExited)
                {
                    programProcess.Kill();
                    _logger.LogToUser("Программа прошивки успешно закрыта.", LogLevel.Info);
                }

                IntPtr mainWindowHandle = Process.GetCurrentProcess().MainWindowHandle;
                SetForegroundWindow(mainWindowHandle);
                _logger.LogToUser("Переключение обратно на стенд завершено.", LogLevel.Info);

                return true;
            }
            catch (OperationCanceledException)
            {
                _logger.LogToUser("Прошивка была прервана пользователем.", LogLevel.Warning);
                if (programProcess != null && !programProcess.HasExited)
                {
                    programProcess.Kill();
                    _logger.LogToUser("Программа прошивки принудительно закрыта из-за отмены.", LogLevel.Warning);
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogToUser($"Ошибка во время прошивки: {ex.Message}", LogLevel.Error);
                if (programProcess != null && !programProcess.HasExited)
                {
                    programProcess.Kill();
                    _logger.LogToUser("Программа прошивки принудительно закрыта из-за ошибки.", LogLevel.Warning);
                }
                return false;
            }
        }

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        #endregion прошивка
        #region Dut тесты

        public bool IsDutSelfTestEnabled => TestConfig.IsDutSelfTestEnabled &&
                                            (TestConfig.DutSelfTest ||
                                            TestConfig.DutSensor1Test || 
                                            TestConfig.DutSensor2Test ||
                                            TestConfig.DutRelayTest || 
                                            TestConfig.DutTamperTest ||
                                            TestConfig.DutPoeTest ||
                                            TestConfig.DutRs485Test ||
                                            TestConfig.DutI2CTest);
        public async Task<bool> RunDutSelfTestAsync(CancellationToken cancellationToken)
        {
            if (!IsDutSelfTestEnabled)
            {
                _logger.LogToUser("Самотестирование DUT отключено, тест пропущен.", LogLevel.Info);
                return true; // Возвращаем true, так как тест просто не выполнялся
            }
            ProgressValue += 5;
            // Ожидание загрузки DUT после прошивки
            if (!await WaitForDUTReadyAsync(cancellationToken, 30, 180))
            {
                _logger.LogToUser("DUT не готов после прошивки.", LogLevel.Error);
                await StopHard();
                return false;
            }
            ProgressValue += 5;
            // Самотестирование
            if (TestConfig.DutSelfTest) 
            {
                if (!await RunSelfTestAsync(cancellationToken))
                {
                    _logger.LogToUser("Самотестирование завершилось с ошибкой.", LogLevel.Error);
                    await StopHard();
                    return false;
                }
            }
            else
            {
                _logger.LogToUser("Самотестирование пропущено (отключено в конфигурации).", LogLevel.Info);
            }

            ProgressValue += 5;
            // SENSOR1
            if (TestConfig.DutSensor1Test)
            {
                if (!await RunSensorTestAsync(2303, "sensor1", cancellationToken))
                {
                    _logger.LogToUser("Тест SENSOR1 завершился с ошибкой.", LogLevel.Error);
                    await StopHard();
                    return false;
                }
            }
            else
            {
                _logger.LogToUser("Тест SENSOR1 пропущен (отключен в конфигурации).", LogLevel.Info);
            }

            if (cancellationToken.IsCancellationRequested) return false;
            ProgressValue += 5;
            // SENSOR2
            if (TestConfig.DutSensor2Test)
            {
                if (!await RunSensorTestAsync(2304, "sensor2", cancellationToken))
                {
                    _logger.LogToUser("Тест SENSOR2 завершился с ошибкой.", LogLevel.Error);
                    await StopHard();
                    return false;
                }
            }
            else
            {
                _logger.LogToUser("Тест SENSOR2 пропущен (отключен в конфигурации).", LogLevel.Info);
            }
            ProgressValue += 5;
            // RELAY
            if (TestConfig.DutRelayTest)
            {
                if (!await RunRelayTestAsync(2306, cancellationToken))
                {
                    _logger.LogToUser("Тест RELAY завершился с ошибкой.", LogLevel.Error);
                    await StopHard();
                    return false;
                }
            }
            else
            {
                _logger.LogToUser("Тест RELAY пропущен (отключен в конфигурации).", LogLevel.Info);
            }
            ProgressValue += 5;
            // TAMPER
            if (TestConfig.DutTamperTest)
            {
                if (!await RunTamperTestAsync(cancellationToken))
                {
                    _logger.LogToUser("Тестирование TAMPER завершилось с ошибкой.", LogLevel.Error);
                    await StopHard();
                    return false;
                }
            }
            else
            {
                _logger.LogToUser("Тест TAMPER пропущен (отключен в конфигурации).", LogLevel.Info);
            }
            ProgressValue += 5;
            // RS485
            if (TestConfig.DutRs485Test)
            {
                if (!await RunRS485TestAsync(cancellationToken))
                {
                    _logger.LogToUser("Тестирование RS485 завершилось с ошибкой.", LogLevel.Error);
                    await StopHard();
                    return false;
                }
            }
            else
            {
                _logger.LogToUser("Тест RS485 пропущен (отключен в конфигурации).", LogLevel.Info);
            }
            ProgressValue += 5;
            // I2C
            if (TestConfig.DutI2CTest)
            {
                if (!await RunI2CTestAsync(cancellationToken))
                {
                    _logger.LogToUser("Тестирование I2C завершилось с ошибкой.", LogLevel.Error);
                    await StopHard();
                    return false;
                }
            }
            else
            {
                _logger.LogToUser("Тест I2C пропущен (отключен в конфигурации).", LogLevel.Info);
            }
            ProgressValue += 5;
            // POE
            if (TestConfig.DutPoeTest)
            {
                if (!await RunPoETestAsync(cancellationToken))
                {
                    _logger.LogToUser("Тестирование POE завершилось с ошибкой.", LogLevel.Error);
                    await StopHard();
                    return false;
                }
            }
            else
            {
                _logger.LogToUser("Тест POE пропущен (отключен в конфигурации).", LogLevel.Info);
            }

            return true;
        }


        #region Самотестирование
        private async Task<bool> RunSelfTestAsync(CancellationToken cancellationToken)
        {
            _logger.LogToUser("Заглушка самотестирования: выполнение не предусмотрено.", LogLevel.Info);
            await Task.Delay(500, cancellationToken); // Имитация выполнения
            return true; // Всегда успешно
        }


        #endregion Самотестирование 
        #region SENSOR 1 + 2 

        private async Task<bool> RunSensorTestAsync(ushort modbusRegister, string sensorName, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogToUser($"Тестирование {sensorName}...", LogLevel.Info);

                // Устанавливаем 0 в регистр
                WriteToRegisterWithRetry(modbusRegister, 0);
                await Task.Delay(2000, cancellationToken);
                if (cancellationToken.IsCancellationRequested || StandRegisters.RunBtn == 0) return false;
                if (!await VerifySensorStatus(sensorName, "0", cancellationToken)) return false;

                // Устанавливаем 1 в регистр
                WriteToRegisterWithRetry(modbusRegister, 1);
                await Task.Delay(2000, cancellationToken);
                if (cancellationToken.IsCancellationRequested || StandRegisters.RunBtn == 0) return false;
                if (!await VerifySensorStatus(sensorName, "1", cancellationToken)) return false;

                // Возвращаем 0
                WriteToRegisterWithRetry(modbusRegister, 0);
                await Task.Delay(2000, cancellationToken);
                if (cancellationToken.IsCancellationRequested || StandRegisters.RunBtn == 0) return false;
                if (!await VerifySensorStatus(sensorName, "0", cancellationToken)) return false;

                _logger.LogToUser($"Тестирование {sensorName} успешно завершено.", LogLevel.Success);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogToUser($"Ошибка во время тестирования {sensorName}: {ex.Message}", LogLevel.Error);
                return false;
            }
        }

        private async Task<bool> VerifySensorStatus(string sensorName, string expectedStatus, CancellationToken cancellationToken)
        {
            _logger.Log($"Проверка состояния сенсора {sensorName}: ожидаемое состояние {expectedStatus}.", LogLevel.Info);

            if (cancellationToken.IsCancellationRequested || StandRegisters.RunBtn == 0) return false;

            string sensorStatus = await SendConsoleCommandAsync($"ubus call tf_hwsys getParam '{{\"name\":\"{sensorName}\"}}'");

            if (!sensorStatus.Contains($"\"{sensorName}\": \"{expectedStatus}\""))
            {
                _logger.Log($"Несоответствие состояния {sensorName}: {sensorStatus}. Ожидалось {expectedStatus}. Повторная проверка через 5 секунд...", LogLevel.Warning);

                await Task.Delay(5000, cancellationToken);
                if (cancellationToken.IsCancellationRequested || StandRegisters.RunBtn == 0) return false;

                sensorStatus = await SendConsoleCommandAsync($"ubus call tf_hwsys getParam '{{\"name\":\"{sensorName}\"}}'");

                if (!sensorStatus.Contains($"\"{sensorName}\": \"{expectedStatus}\""))
                {
                    _logger.Log($"Ошибка: {sensorName} имеет состояние {sensorStatus}, ожидалось {expectedStatus}.", LogLevel.Error);
                    return false;
                }
            }

            _logger.Log($"{sensorName} успешно установлен в {expectedStatus}.", LogLevel.Info);
            return true;
        }


        #endregion SENSOR 1 + 2 
        #region RELAY
        private async Task<bool> RunRelayTestAsync(ushort relayStatusRegister, CancellationToken cancellationToken)
        {
            
            try
            {
                _logger.LogToUser("Тест RELAY запущен...", LogLevel.Info);
                await SendConsoleCommandAsync("ubus call tf_hwsys setParam '{\"name\":\"relay\",\"value\":\"0\"}'");
                _logger.Log("Реле переведено в состояние 0 через консоль.", LogLevel.Info);


                await Task.Delay(5000);
                if (cancellationToken.IsCancellationRequested) return false;

                if (StandRegisters.RelayIn != 0)
                {
                    _logger.Log($"Состояние реле в регистре {relayStatusRegister} не совпадает с ожидаемым (0).", LogLevel.Warning);
                    return false;
                }

                await SendConsoleCommandAsync("ubus call tf_hwsys setParam '{\"name\":\"relay\",\"value\":\"1\"}'");
                _logger.Log("Реле переведено в состояние 1 через консоль.", LogLevel.Info);

                await Task.Delay(5000);
                if (cancellationToken.IsCancellationRequested) return false;

                if (StandRegisters.RelayIn != 1)
                {
                    _logger.Log($"Состояние реле в регистре {relayStatusRegister} не совпадает с ожидаемым (1).", LogLevel.Warning);
                    return false;
                }

                await SendConsoleCommandAsync("ubus call tf_hwsys setParam '{\"name\":\"relay\",\"value\":\"0\"}'");
                _logger.Log("Реле возвращено в состояние 0 через консоль.", LogLevel.Info);

                await Task.Delay(8000);
                if (cancellationToken.IsCancellationRequested) return false;

                if (StandRegisters.RelayIn != 0)
                {
                    _logger.Log($"Состояние реле в регистре {relayStatusRegister} не совпадает с ожидаемым (0).", LogLevel.Warning);
                    return false;
                }

                _logger.LogToUser("Тестирование релейного выхода успешно завершено.", LogLevel.Success);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogToUser($"Ошибка при тестировании реле: {ex.Message}", LogLevel.Error);
                return false;
            }
        }
        #endregion RELAY
        #region TAMPER
        public async Task<bool> RunTamperTestAsync(CancellationToken cancellationToken)
        {
            async Task<bool> VerifyTamperStatus(string expectedStatus, string actionDescription)
            {
                _logger.Log($"{actionDescription}: ожидаемое состояние {expectedStatus}. Проверяем...", LogLevel.Info);

                for (int attempt = 1; attempt <= 2; attempt++) // Две попытки проверки
                {
                    string tamperStatus = await SendConsoleCommandAsync($"ubus call tf_hwsys getParam '{{\"name\":\"tamper\"}}'");

                    if (tamperStatus.Contains($"\"tamper\": \"{expectedStatus}\""))
                    {
                        _logger.Log($"Tamper успешно установлен в {expectedStatus}.", LogLevel.Info);
                        return true;
                    }

                    _logger.Log($"Попытка {attempt}: Tamper имеет состояние {tamperStatus}, ожидалось {expectedStatus}.", LogLevel.Warning);

                    if (attempt < 2)
                    {
                        _logger.Log("Ожидание 9 секунд перед повторной проверкой...", LogLevel.Debug);
                        await Task.Delay(9000); // Даем железу время обработать изменение
                    }
                }

                _logger.Log($"Ошибка: Tamper после повторной проверки имеет неверное состояние. Ожидалось {expectedStatus}.", LogLevel.Error);
                return false;
            }


            bool CheckRegisterValue(string name, ushort actualValue, ushort minValue, ushort maxValue)
            {
                if (actualValue < minValue || actualValue > maxValue)
                {
                    _logger.Log($"Ошибка: {name} = {actualValue}, ожидалось {minValue}-{maxValue}.", LogLevel.Error);
                    return false;
                }

                _logger.Log($"{name} = {actualValue} в допустимом диапазоне {minValue}-{maxValue}.", LogLevel.Info);
                return true;
            }

            try
            {
                _logger.LogToUser("Тестирование датчика вскрытия (Tamper)...", LogLevel.Info);

                // Получаем диапазоны из профиля тестирования
                ushort minStatusTamper = TestConfig.DutTamperStatusMin;
                ushort maxStatusTamper = TestConfig.DutTamperStatusMax;
                ushort minTamperLed = TestConfig.DutTamperLedMin;
                ushort maxTamperLed = TestConfig.DutTamperLedMax;

                // 1) Отключаем Tamper (2305 = 0)
                WriteToRegisterWithRetry(2305, 0);
                _logger.Log("2305 = 0 (Tamper отключён)", LogLevel.Debug);
                await Task.Delay(5000, cancellationToken);

                // 2) Проверяем STATUS_TAMPER (2319)
                if (!CheckRegisterValue("STATUS_TAMPER", StandRegisters.Status_Tamper, minStatusTamper, maxStatusTamper))
                    return false;

                // 3) Проверяем TAMPER_LED (2320)

                if (!CheckRegisterValue("TAMPER_LED", StandRegisters.Status_Tamper_Led, minTamperLed, maxTamperLed))
                    return false;

                // 4) Проверяем Tamper через консоль (должно быть 0)
                if (!await VerifyTamperStatus("0", "Отключение Tamper"))
                    return false;

                // 5) Включаем Tamper (2305 = 1)
                WriteToRegisterWithRetry(2305, 1);
                _logger.Log("2305 = 1 (Tamper включён)", LogLevel.Debug);
                await Task.Delay(5000, cancellationToken);

                // 6) Проверяем Tamper через консоль (должно быть 1)
                if (!await VerifyTamperStatus("1", "Включение Tamper"))
                    return false;

                // 7) Отключаем Tamper (2305 = 0)
                WriteToRegisterWithRetry(2305, 0);
                _logger.Log("2305 = 0 (Tamper отключён)", LogLevel.Debug);
                await Task.Delay(5000, cancellationToken);

                // 8) Проверяем STATUS_TAMPER (2319)

                if (!CheckRegisterValue("STATUS_TAMPER", StandRegisters.Status_Tamper, minStatusTamper, maxStatusTamper))
                    return false;

                // 9) Проверяем TAMPER_LED (2320)
                if (!CheckRegisterValue("TAMPER_LED", StandRegisters.Status_Tamper_Led, minTamperLed, maxTamperLed))
                    return false;

                // 10) Проверяем Tamper через консоль (должно быть 0)
                if (!await VerifyTamperStatus("0", "Отключение Tamper"))
                    return false;

                _logger.LogToUser("Тестирование датчика вскрытия (Tamper) успешно завершено.", LogLevel.Success);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogToUser($"Ошибка во время тестирования Tamper: {ex.Message}", LogLevel.Error);
                return false;
            }
        }

        #endregion TAMPER
        #region RS485

        private async Task<bool> RunRS485TestAsync(CancellationToken cancellationToken)
        {
            async Task<bool> VerifyRS485Status(string expectedStatus, string actionDescription)
            {
                _logger.Log($"{actionDescription}: ожидаемое состояние {expectedStatus}. Проверяем...", LogLevel.Info);

                // Отправка команды для проверки состояния RS485
                string rs485Status = await SendConsoleCommandAsync("ubus call tf_hwsys getParam '{\"name\":\"upsModeAvalible\"}'");

                if (!rs485Status.Contains($"\"upsModeAvalible\": \"{expectedStatus}\""))
                {
                    _logger.Log($"Несоответствие состояния RS485: {rs485Status}. Ожидалось {expectedStatus}. Повторная проверка через 5 секунд...", LogLevel.Warning);
                    await Task.Delay(5000, cancellationToken);

                    // Повторная проверка
                    rs485Status = await SendConsoleCommandAsync("ubus call tf_hwsys getParam '{\"name\":\"upsModeAvalible\"}'");
                    if (!rs485Status.Contains($"\"upsModeAvalible\": \"{expectedStatus}\""))
                    {
                        _logger.Log($"Ошибка: RS485 имеет состояние {rs485Status}, ожидалось {expectedStatus}.", LogLevel.Error);
                        return false;
                    }
                }

                _logger.Log($"RS485 успешно проверен: состояние {expectedStatus}.", LogLevel.Success);
                return true;
            }

            try
            {
                _logger.LogToUser("Тестирование RS485...", LogLevel.Info);

                // Проверка состояния RS485 через консоль
                if (!await VerifyRS485Status("1", "Проверка доступности RS485"))
                    return false;

                _logger.LogToUser("Тестирование RS485 успешно завершено.", LogLevel.Success);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogToUser($"Ошибка во время тестирования RS485: {ex.Message}", LogLevel.Error);
                return false;
            }
        }

        #endregion RS485
        #region I2C
        private async Task<bool> RunI2CTestAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogToUser("Тестирование I2C...", LogLevel.Info);

                // Проверка подключения I2C
                _logger.Log("Проверка подключения I2C...", LogLevel.Info);
                string sensorConnectedResponse = await SendConsoleCommandAsync("ubus call tf_hwsys getParam '{\"name\":\"sensorConnected\"}'");

                if (!sensorConnectedResponse.Contains("\"sensorConnected\": \"1\""))
                {
                    _logger.LogToUser("Ошибка: I2C не подключён.", LogLevel.Error);
                    return false;
                }

                _logger.LogToUser("I2C подключён.", LogLevel.Info);

                // Проверка температуры I2C
                _logger.Log("Проверка температуры I2C...", LogLevel.Info);
                string sensorTemperatureResponse = await SendConsoleCommandAsync("ubus call tf_hwsys getParam '{\"name\":\"sensorTemperature\"}'");

                // Извлечение температуры
                double? temperature = ParseTemperature(sensorTemperatureResponse);
                if (temperature == null)
                {
                    _logger.LogToUser("Ошибка: Не удалось извлечь температуру из ответа.", LogLevel.Error);
                    return false;
                }

                _logger.LogToUser($"Температура I2C: {temperature} °C.", LogLevel.Info);
                _logger.LogToUser("Тестирование I2C успешно завершено.", LogLevel.Success);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogToUser($"Ошибка во время тестирования I2C: {ex.Message}", LogLevel.Error);
                return false;
            }
        }
        private double? ParseTemperature(string response)
        {
            try
            {
                if (response.Length > 75)
                {
                    response = response.Substring(59, response.Length - 59 - 16).Trim();
                    _logger.Log($"Ответ после удаления символов: {response}", LogLevel.Debug);
                }
                else
                {
                    throw new Exception("Ответ слишком короткий для извлечения JSON.");
                }

                var jsonObject = Newtonsoft.Json.Linq.JObject.Parse(response);
                string temperatureString = jsonObject.SelectToken("sensorTemperature")?.ToString();

                if (string.IsNullOrEmpty(temperatureString))
                {
                    throw new Exception("Поле sensorTemperature отсутствует в JSON-ответе.");
                }

                temperatureString = temperatureString.Replace(".", ",");

                if (double.TryParse(temperatureString, out double temperature))
                {
                    return temperature;
                }

                throw new Exception("Не удалось преобразовать значение температуры.");
            }
            catch (Exception ex)
            {
                _logger.Log($"Ошибка парсинга температуры: {ex.Message}", LogLevel.Error);
                _logger.Log($"Сырой ответ: {response}", LogLevel.Error);
                return null;
            }
        }
        #endregion I2C
        #region POE
        private async Task<bool> RunPoETestAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogToUser("Тестирование PoE...", LogLevel.Info);

                // Выполнение команды для получения информации о PoE
                string poeResponse = await SendConsoleCommandAsync("ubus call poe info");

                if (string.IsNullOrWhiteSpace(poeResponse))
                {
                    _logger.LogToUser("Ошибка: Команда 'ubus call poe info' не вернула ответ.", LogLevel.Error);
                    return false;
                }

                _logger.Log($"Получен ответ от PoE: {poeResponse}", LogLevel.Info);

                if (poeResponse.Contains("\"budget\":"))
                {
                    _logger.LogToUser("Тестирование PoE успешно завершено.", LogLevel.Success);
                    return true;
                }

                _logger.Log("Ошибка: В ответе отсутствует ключ 'budget'.", LogLevel.Error);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogToUser($"Ошибка во время тестирования PoE: {ex.Message}", LogLevel.Error);
                return false;
            }
        }

        #endregion POE

        private async Task<string> SendConsoleCommandAsync(string command, int timeoutMilliseconds = 5000)
        {
            try
            {
                if (_serialPortDut == null || !_serialPortDut.IsOpen)
                {
                    throw new InvalidOperationException("COM-порт для DUT не открыт.");
                }

                _serialPortDut.DiscardInBuffer();
                _serialPortDut.DiscardOutBuffer();
                _serialPortDut.Write($"{command}\n");
                _logger.Log($"Отправлена команда: {command}", LogLevel.Debug);

                var responseBuilder = new StringBuilder();
                var timeout = TimeSpan.FromMilliseconds(timeoutMilliseconds);
                var startTime = DateTime.UtcNow;

                while (DateTime.UtcNow - startTime < timeout)
                {
                    await Task.Delay(100);
                    if (_serialPortDut.BytesToRead > 0)
                    {
                        string chunk = _serialPortDut.ReadExisting();
                        responseBuilder.Append(chunk);
                        if (chunk.Contains("root@TFortis:/#") || chunk.Trim().EndsWith("}"))
                        {
                            break;
                        }
                    }
                }

                string rawResponse = responseBuilder.ToString();
                _logger.Log($"Сырой ответ ({DateTime.UtcNow:HH:mm:ss.fff}):\n{rawResponse}", LogLevel.Debug);

                return rawResponse;
            }
            catch (Exception ex)
            {
                _logger.Log($"Критическая ошибка в SendConsoleCommand: {ex.Message}", LogLevel.Error);
                throw new InvalidOperationException($"Ошибка выполнения команды {command}", ex);
            }
        }
        #endregion Dut тесты
        #endregion тестирование





        public RtlSwViewModel(Loggers logger)
        {



            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger.Log("RtlSwViewModel инициализирован", Loggers.LogLevel.Success);

            ToggleModbusConnectionCommand = new RelayCommand(async () => await ToggleModbusConnection(), CanExecuteCommand);
            ConnectToServerCommand = new RelayCommand(async () => await TryConnectToServerAsync(), CanExecuteCommand);
            LoadTestProfileCommand = new RelayCommand(async () => await TryLoadTestProfileAsync(), CanExecuteCommand);


            ToggleV52Command = new RelayCommand(() => IsV52Enabled = !IsV52Enabled);


            if (TestConfig != null)
            {
                TestConfig.PropertyChanged += ProfileTest_PropertyChanged;
            }
            else
            {
                _logger.Log("Ошибка: TestConfig не инициализирован!", Loggers.LogLevel.Error);
            }


            ConnectCommand = new AsyncRelayCommand(ToggleConnectionAsync);  // Кнопка "Подключиться к стенду"
            Task.Run(async () => // Автоматическое подключение к стенду
            {
                await Task.Delay(1000);
                await ToggleConnectionAsync();
            });
        }








        public void WriteToRegisterWithRetry(ushort register, ushort value, int retries = 3)
        {

            if (IsModbusConnected) { 
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
        }

        #region  GUI логика

        #region тумблеры модбас

        public bool IsV52Enabled
        {
            get => StandRegisters.V52Out == 1;
            set
            {
                StandRegisters.V52Out = (ushort)(value ? 1 : 0);
                WriteToRegisterWithRetry(2301, StandRegisters.V52Out);
                OnPropertyChanged();
            }
        }
        public ICommand ToggleV52Command { get; }


        #endregion тумблеры модбас

        #endregion GUI логика


        #region отключения
        private async Task StopHard()
        {
            _logger.LogToUser("Прерывание тестирования...", Loggers.LogLevel.Warning);
            IsTestRunning = false;

            // Отключаем питание платы
            WriteToRegisterWithRetry(2301, 0);
            WriteToRegisterWithRetry(2302, 0);

            await Task.Delay(500); // Даем время на обработку

            _logger.LogToUser("Питание снято. Плату можно безопасно извлечь из стенда.", Loggers.LogLevel.Info);
        }

        private async Task DisconnectStand()
        {
            _isCancellationRequested = true; // Устанавливаем флаг для прерывания
            await Task.Delay(500); // Небольшая задержка, чтобы избежать гонки состояний

            DisconnectModbus();
            IsStandConnected = false;
            IsModbusConnected = false;

            _logger.LogToUser("Стенд успешно отключен.", Loggers.LogLevel.Info);
        }


        private async Task CloseConnections()
        {
            try
            {
                // Ожидаем перед отключением
                await Task.Delay(500);

                // Закрываем COM-порты и Modbus
                _serialPortCom?.Close();
                _serialPortCom?.Dispose();
                _serialPortDut?.Close();
                _serialPortDut?.Dispose();
                _modbusMaster?.Dispose();

                IsModbusConnected = false;
                IsDutConnected = false;
                IsStandConnected = false;

                _logger.LogToUser("Все соединения закрыты.", Loggers.LogLevel.Info);
            }
            catch (Exception ex)
            {
                _logger.LogToUser($"Ошибка при закрытии соединений: {ex.Message}", Loggers.LogLevel.Error);
            }
        }

        protected override async void OnClose()
        {
            _logger.LogToUser("Закрытие приложения. Завершаем работу и отключаем стенд...", Loggers.LogLevel.Info);
            if (IsStandConnected)
            {
                await StopHard(); // Сбрасываем питание
                await CloseConnections(); // Закрываем соединения
            }
            _logger.LogToUser("Приложение закрыто.", Loggers.LogLevel.Success);
            base.OnClose();
        }

        #endregion отключения
    }
}