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
using RTL.ReportGenerator;
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
using Microsoft.WindowsAPICodePack.Sensors;
using FTServiceUtils;
using Newtonsoft.Json.Linq;
using RTL.Services;
using System.Management;
using System.Reflection;
namespace RTL.ViewModels
{
    public class RtlSwViewModel : Screen
    {
        public TscPrinterService _printerService;
        public bool isSwTestFull = false;
        public bool isSwTestSuccess = false;
        private bool _isCancellationRequested;

        public event PropertyChangedEventHandler PropertyChanged;

        public ICommand OpenFlashProgramCommand { get; }
        public ICommand OpenSwdProgramCommand { get; }


        #region логи
        private readonly Loggers _logger;
        public ObservableCollection<LogEntry> Logs => _logger.LogMessages;

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

                IsStandConnected = false;
                IsTestRunning = false;

                return;
            }

            _logger.LogToUser("Попытка подключения к стенду...", Loggers.LogLevel.Info);
            IsStandConnected = true;
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

                IsStandConnected = true;
                _logger.LogToUser($"Подключен СТЕНД RTL-SW, серийный номер стенда:", Loggers.LogLevel.Success);
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
            string portName = Properties.Settings.Default.ComSW;

            if (string.IsNullOrWhiteSpace(portName))
            {
                _logger.LogToUser("Ошибка: не указан COM-порт для стенда. Укажите порт в настройках.", Loggers.LogLevel.Error);
                return false;
            }

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

                    _logger.LogToUser("Проверка типа стенда...", Loggers.LogLevel.Info);

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

                            _logger.Log($"Тип стенда: {standType} (STAND RTL-SW), Серийный номер: {standSerial}", Loggers.LogLevel.Info);
                            return true;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogToUser($"Ошибка при проверке типа стенда: {ex.Message}", Loggers.LogLevel.Error);
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
            await CloseConnections();
            return false;
        }


        public async Task WriteToRegisterWithRetryAsync(ushort register, ushort value, int retries = 3)
        {
            for (int attempt = 1; attempt <= retries; attempt++)
            {
                if (!IsModbusConnected)
                {
                    if (!await TryReconnectModbusAsync())
                    {
                        _logger.LogToUser("Не удалось переподключиться к Modbus.", LogLevel.Error);
                        return;
                    }

                }

                try
                {
                    _logger.Log($"Попытка записи в {register}: {value}", LogLevel.Debug); // <---- Новый лог
                    _modbusMaster.WriteSingleRegister(1, register, value);
                    _logger.Log($"Запись успешна: {register} = {value}", LogLevel.Debug);
                    return; // Успешная запись
                }
                catch (Exception ex)
                {
                    _logger.Log($"Попытка {attempt} записи в регистр {register} не удалась: {ex.Message}", LogLevel.Warning);
                    await Task.Delay(1000);
                }
            }

            _logger.LogToUser($"Ошибка: не удалось записать {value} в {register} после {retries} попыток.", LogLevel.Error);
            await StopHard();
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

        public async Task<bool> WaitForDUTReadyAsync(CancellationToken cancellationToken, bool wasFlashed = false, int noLogTimeoutSeconds = 100, int maxWaitTimeSeconds = 1200)
        {
            //await WriteToRegisterWithRetryAsync(2301, 1);
            await Task.Delay(2000, cancellationToken);

            try
            {
                if (!await TryInitializeDutComPortAsync(cancellationToken))
                {
                    throw new InvalidOperationException("COM-порт для DUT не открыт.");
                }

                _logger.LogToUser("Ожидание ответа от платы...", LogLevel.Info);

                DateTime startTime = DateTime.Now;
                DateTime lastLogTime = DateTime.Now;
                bool successPromptShown = false;
                bool flashedOnce = false; // Флаг, который не даёт прошивать DUT повторно

                while ((DateTime.Now - startTime).TotalSeconds < maxWaitTimeSeconds)
                {
                    if (cancellationToken.IsCancellationRequested || StandRegisters.RunBtn == 0)
                    {
                        _logger.LogToUser("Ожидание ответа от платы прервано (кнопка RUN переведена в положение 0).", LogLevel.Warning);
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
                            _logger.LogToUser("Плата успешно запущена.", LogLevel.Success);
                            return true;
                        }
                    }
                    else if ((DateTime.Now - lastLogTime).TotalSeconds > noLogTimeoutSeconds && !successPromptShown)
                    {
                        _serialPortDut.Write("\n");
                        _logger.Log("Нет новых логов. Отправлена команда \\n для проверки готовности.", LogLevel.Debug);
                        successPromptShown = true;

                        await Task.Delay(1000, cancellationToken);
                        _serialPortDut.Write("ubus call tf_hwsys getParam '{\"name\":\"SW_VERS\"}'\n");
                        _logger.Log("Отправлена команда ubus call tf_hwsys getParam '{\"name\":\"SW_VERS\"}'", LogLevel.Debug);

                        await Task.Delay(2000, cancellationToken); // Ждём ответ

                        if (_serialPortDut.BytesToRead > 0)
                        {
                            string response = _serialPortDut.ReadExisting();
                            _logger.Log($"Ответ на команду ubus: {response.Trim()}", LogLevel.Debug);

                            if (!response.Contains("\"SW_VERS\": \"0\""))
                            {
                                _logger.Log("Обнаружен корректный ответ от ubus. DUT готов к работе.", LogLevel.Info);
                                _logger.LogToUser("DUT готов к работе.", LogLevel.Success);
                                return true;
                            }

                            if (response.Contains("\"SW_VERS\": \"0\""))
                            {

                                {
                                    _logger.LogToUser("Ошибка: SW_VERS = 0 после прошивки. Проверьте файл (.bin) прошивки и перезапустите тест.", LogLevel.Error);
                                    return false;

                                }

                            }
                        }
                    }
                }

                _logger.LogToUser($"Плата не завершила загрузку за {maxWaitTimeSeconds} секунд.", LogLevel.Error);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogToUser($"Ошибка при ожидании старта платы: {ex.Message}", LogLevel.Error);
                return false;
            }
        }


        public async Task<bool> TryInitializeDutComPortAsync(CancellationToken cancellationToken)
        {
            try
            {
                await WriteToRegisterWithRetryAsync(2301, 1, 3);
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

        public string SessionId;  //id сессии, без него не отправишь рез-ты

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
                            _logger.LogToUser($"Подключение к серверу успешно. Код: {response.StatusCode}", LogLevel.Success);
                            //SessionId = GetSessionId("alex", "alex", 5); //id сессии, без него не отправишь рез-ты
                            SessionId = App.StartupSessionId;

                            IsServerConnected = true;
                            return true;
                        }
                        else
                        {
                            _logger.LogToUser($"Ошибка подключения. Код: {response.StatusCode}, Ответ: {responseContent}", LogLevel.Warning);
                        }
                    }
                }
                catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
                {
                    _logger.LogToUser($"Тайм-аут при подключении. Сервер не отвечает. Попробуйте позже.", LogLevel.Error);
                }
                catch (HttpRequestException ex)
                {
                    _logger.LogToUser($"Ошибка сети: {ex.Message}. Проверьте интернет-соединение.", LogLevel.Error);
                }
                catch (Exception ex)
                {
                    _logger.LogToUser($"Ошибка подключения к серверу: {ex.Message}", LogLevel.Error);
                }

                if (attempt < maxRetries)
                {
                    _logger.LogToUser($"Повторная попытка через {delayMs / 1000} секунд...", LogLevel.Info);
                    await Task.Delay(delayMs);
                }
            }

            _logger.LogToUser($"Все попытки подключения исчерпаны. Проверьте настройки сети.", LogLevel.Error);
            IsServerConnected = false;
            return false;
        }
        private string GetSessionId(string login, string password, int timezone)
        {
            string url = $"http://iccid.fort-telecom.ru/api/Api.svc/connect?login={login}&password={password}&timezone={timezone}";

            try
            {
                using (HttpClient client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) })
                {
                    client.DefaultRequestHeaders.Add("User-Agent", "ftstand");

                    HttpResponseMessage response = client.GetAsync(url).Result;
                    string responseContent = response.Content.ReadAsStringAsync().Result;

                    _logger.Log($"Ответ сервера (sessionId): {responseContent}", LogLevel.Debug);

                    if (!response.IsSuccessStatusCode)
                    {
                        _logger.Log($"Ошибка получения sessionId. Код: {response.StatusCode}, Ответ: {responseContent}", LogLevel.Warning);
                        return null;
                    }

                    // Убираем кавычки, если сервер вернул строку
                    string sessionId = responseContent.Trim('"');

                    if (!string.IsNullOrEmpty(sessionId))
                    {
                        _logger.Log($"Успешно получен sessionId: {sessionId}", LogLevel.Success);
                        return sessionId;
                    }
                    else
                    {
                        _logger.Log($"Сервер не вернул корректный sessionId: {responseContent}", LogLevel.Warning);
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.Log($"Ошибка сети при получении sessionId: {ex.Message}", LogLevel.Error);
            }
            catch (Exception ex)
            {
                _logger.Log($"Ошибка при получении sessionId: {ex.Message}", LogLevel.Error);
            }

            return null;
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
                    OnPropertyChanged(nameof(TestConfig));
                    IsTestProfileLoaded = true;
                    _logger.LogToUser($"Файл  тестирования загружен: {testProfilePath}", LogLevel.Success);
                    return true;
                }
                else
                {
                    TestConfig = new ProfileTestModel();
                    OnPropertyChanged(nameof(TestConfig));
                    IsTestProfileLoaded = false;
                    _logger.LogToUser($"Файл  тестирования {testProfilePath} не найден.", LogLevel.Warning);
                    return false;
                }
            }
            catch (JsonException ex)
            {
                TestConfig = new ProfileTestModel();
                OnPropertyChanged(nameof(TestConfig));
                IsTestProfileLoaded = false;
                _logger.LogToUser($"Ошибка обработки JSON: {ex.Message}", LogLevel.Error);
                return false;
            }
            catch (Exception ex)
            {
                TestConfig = new ProfileTestModel();
                OnPropertyChanged(nameof(TestConfig));
                IsTestProfileLoaded = false;
                _logger.LogToUser($"Ошибка при загрузке профиля тестирования: {ex.Message}", LogLevel.Error);
                return false;
            }
        }


        #endregion Профиль тестирования
        #region мониторинг
        public StandRegistersModel StandRegisters { get; } = new StandRegistersModel();
        public ReportModel reportModel { get; } = new ReportModel(); // не помню нужно это или нет

        private CancellationTokenSource _testCancellationTokenSource;


        private async Task MonitorStandAsync()
        {
            while (IsStandConnected)
            {
                try
                {
                    if (!IsModbusConnected)
                    {
                        _logger.Log("Modbus отключен. Попытка переподключения...", LogLevel.Warning);

                        if (!await TryReconnectModbusAsync())
                        {
                            _logger.LogToUser("Не удалось переподключиться к стенду. Остановка.", LogLevel.Error);
                            IsStandConnected = false;
                            return;
                        }
                    }

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
                    _logger.Log($"Ошибка при чтении регистров Modbus: {ex.Message}", Loggers.LogLevel.Error);
                    IsModbusConnected = false;
                    await Task.Delay(2000); // Ждём перед новой попыткой
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
        private async Task<bool> PrepareStandForTestingAsync()
        {
            ServerTestResult = new TestResult
            {
                deviceType = DeviceType.RTL_SW,
                standName = Environment.MachineName,
                isSuccess = false,
                deviceIdent = "default", //серийник платы
                isFull = false
            };
            try
            {
                isSwTestFull = false;
                isSwTestSuccess = false;

                await WriteToRegisterWithRetryAsync(2301, 0);
                await WriteToRegisterWithRetryAsync(2302, 0);
                await WriteToRegisterWithRetryAsync(2303, 0);
                await WriteToRegisterWithRetryAsync(2304, 0);
                await WriteToRegisterWithRetryAsync(2305, 0);
                await WriteToRegisterWithRetryAsync(2307, 0);
                await WriteToRegisterWithRetryAsync(2308, 0);
                await WriteToRegisterWithRetryAsync(2329, TestConfig.K5TestDelay);
                await WriteToRegisterWithRetryAsync(2332, TestConfig.VccStartDelay);
                await WriteToRegisterWithRetryAsync(2333, TestConfig.K5_52V_Min);
                await WriteToRegisterWithRetryAsync(2334, TestConfig.K5_52V_Max);
                await WriteToRegisterWithRetryAsync(2336, TestConfig.K5_55V_Min);
                await WriteToRegisterWithRetryAsync(2337, TestConfig.K5_55V_Max);
                await WriteToRegisterWithRetryAsync(2339, TestConfig.VoutMin);
                await WriteToRegisterWithRetryAsync(2340, TestConfig.VoutMax);
                await WriteToRegisterWithRetryAsync(2341, TestConfig.VoutVresMin);
                await WriteToRegisterWithRetryAsync(2342, TestConfig.VoutVresMax);
                await WriteToRegisterWithRetryAsync(2344, TestConfig.VrefMin);
                await WriteToRegisterWithRetryAsync(2345, TestConfig.VrefMax);
                await WriteToRegisterWithRetryAsync(2347, TestConfig.V12Min);
                await WriteToRegisterWithRetryAsync(2348, TestConfig.V12Max);
                await WriteToRegisterWithRetryAsync(2350, TestConfig.Vcc3V3Min);
                await WriteToRegisterWithRetryAsync(2351, TestConfig.Vcc3V3Max);
                await WriteToRegisterWithRetryAsync(2353, TestConfig.Vcc1V5Min);
                await WriteToRegisterWithRetryAsync(2354, TestConfig.Vcc1V5Max);
                await WriteToRegisterWithRetryAsync(2356, TestConfig.Vcc1V1Min);
                await WriteToRegisterWithRetryAsync(2357, TestConfig.Vcc1V1Max);
                await WriteToRegisterWithRetryAsync(2359, TestConfig.CR2032Min);
                await WriteToRegisterWithRetryAsync(2360, TestConfig.CR2032Max);
                await WriteToRegisterWithRetryAsync(2362, TestConfig.CR2032CpuMin);
                await WriteToRegisterWithRetryAsync(2363, TestConfig.CR2032CpuMax);
                await WriteToRegisterWithRetryAsync(2365, TestConfig.DutTamperStatusMin); // TAMPER_STATUS_MIN
                await WriteToRegisterWithRetryAsync(2366, TestConfig.DutTamperStatusMax); // TAMPER_STATUS_MAX
                await WriteToRegisterWithRetryAsync(2368, TestConfig.DutTamperLedMin);
                await WriteToRegisterWithRetryAsync(2369, TestConfig.DutTamperLedMax);

                RtlStatus = 1;

                K5TestStatus = 1;

                V11Status = 1;
                V15Status = 1;
                V33Status = 1;
                CrStatus = 1;
                CrCpuStatus = 1;

                FlashStatus = 1;
                ConsoleStatus = 1;

                Sensor1Status = 1;
                Sensor2Status = 1;
                RelayStatus = 1;
                TamperStatus = 1;
                Rs485Status = 1;
                I2CStatus = 1;
                PoeStatus = 1;

                ProgressValue = 0;


                await Task.Delay(500);
                _logger.LogToUser("Настройка перед работой с СТЕНД RTL-SW...", LogLevel.Info);

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

                // K5
                K5TestStatus = 1;
                if (!await RunK5TestsAsync(cancellationToken))
                {
                    _logger.LogToUser("Тест К-5 завершен с ошибкой.", LogLevel.Error);

                    RtlStatus = 3;
                    isSwTestSuccess = false;
                    
                    await StopHard();
                    _logger.LogToUser($"Тест завершен: {(ServerTestResult.isSuccess ? "УСПЕШНО" : "НЕУСПЕШНО")}", ServerTestResult.isSuccess ? LogLevel.Success : LogLevel.Error);
                    await LoadSwReport();
                    return false;
                }
                //  VCC
                if (!await RunVCCTestAsync(cancellationToken))
                {
                    _logger.LogToUser("Тест VCC завершен с ошибкой.", LogLevel.Error);
                    isSwTestSuccess = false;
                    await StopHard();
                    _logger.LogToUser($"Тест завершен: {(ServerTestResult.isSuccess ? "УСПЕШНО" : "НЕУСПЕШНО")}", ServerTestResult.isSuccess ? LogLevel.Success : LogLevel.Error);
                    await LoadSwReport();

                    return false;
                }
                ProgressValue += 5;

                await WriteToRegisterWithRetryAsync(2301, 1); //-----------------------------------------подача питания перед прошивкой (52)
                // FLASH прошивка
                if (!await StartProgrammingAsync(cancellationToken))
                {
                    FlashStatus = 3;
                    RtlStatus = 3;
                    _logger.LogToUser("Прошивка FLASH завершена с ошибкой", LogLevel.Error);
                    isSwTestSuccess = false;
                    await StopHard();
                    _logger.LogToUser($"Тест завершен: {(ServerTestResult.isSuccess ? "УСПЕШНО" : "НЕУСПЕШНО")}", ServerTestResult.isSuccess ? LogLevel.Success : LogLevel.Error);
                    await LoadSwReport();
                    return false;
                }
                FlashStatus = 2;
                await WriteToRegisterWithRetryAsync(2307, 0); //------------------------------------------выключаем ресет
                await Task.Delay(2000);

                ProgressValue += 5;
                // MCU прошивка
                if (!await FlashMcuAsync(cancellationToken))
                {
                    RtlStatus = 3;
                    _logger.LogToUser("Прошивка SWD завершена с ошибкой", LogLevel.Error);
                    isSwTestSuccess = false;
                    await StopHard();
                    _logger.LogToUser($"Тест завершен: {(ServerTestResult.isSuccess ? "УСПЕШНО" : "НЕУСПЕШНО")}", ServerTestResult.isSuccess ? LogLevel.Success : LogLevel.Error);
                    await LoadSwReport();
                    return false;
                }

                if (!TestConfig.IsFlashProgrammingEnabled && !TestConfig.IsMcuProgrammingEnabled) // ------------------------ не перезагружаем плату если не прошивали 
                {
                    await WriteToRegisterWithRetryAsync(2301, 0);
                    await WriteToRegisterWithRetryAsync(2302, 0);
                    await Task.Delay(10000);
                    await WriteToRegisterWithRetryAsync(2301, 1);
                    await WriteToRegisterWithRetryAsync(2302, 1);
                    await Task.Delay(2000);
                }




                ProgressValue += 5;
                // DUT
                if (IsDutSelfTestEnabled)
                {
                    _logger.LogToUser("Запуск самотестирования DUT...", LogLevel.Info);
                    if (!await RunDutSelfTestAsync(cancellationToken))
                    {
                        _logger.LogToUser("Тестирование DUT завершилось с ошибкой.", LogLevel.Error);
                        await StopHard();
                        _logger.LogToUser($"Тест завершен: {(ServerTestResult.isSuccess ? "УСПЕШНО" : "НЕУСПЕШНО")}", ServerTestResult.isSuccess ? LogLevel.Success : LogLevel.Error);
                        return false;
                    }
                }
                else
                {
                    _logger.LogToUser("Тестирование DUT отключено, пропускаем.", LogLevel.Warning);
                }
                ProgressValue += 5;
                RtlStatus = 2;




                ServerTestResult.isFull = true; // доработать чтобы обрабатывал именно по профилю распознавал все ли в подтесте
                isSwTestSuccess = true;
                await LoadSwReport();

                // Печать этикетки
                if (!await PrintLabelAsync())
                {
                    _logger.LogToUser("Ошибка печати этикетки.", LogLevel.Error);
                    _logger.LogToUser($"Тест завершен: {(ServerTestResult.isSuccess ? "УСПЕШНО" : "НЕУСПЕШНО")}", ServerTestResult.isSuccess ? LogLevel.Success : LogLevel.Error);
                    await StopHard();
                    return false;
                }
                ServerTestResult.isSuccess = true;
                _logger.LogToUser($"Тест завершен: {(ServerTestResult.isSuccess ? "УСПЕШНО" : "НЕУСПЕШНО")}", ServerTestResult.isSuccess ? LogLevel.Success : LogLevel.Error);
                await StopHard();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogToUser($"Ошибка во время тестирования: {ex.Message}", LogLevel.Error);
                await StopHard();
                await LoadSwReport();

                return false;
            }
            finally
            {
                IsTestRunning = false;
            }
        }



        #region K5

        private async Task<bool> RunK5TestsAsync(CancellationToken cancellationToken)
        {
            if (!TestConfig.IsK5TestEnabled)
            {
                K5TestStatus = 1;
                _logger.LogToUser("Тестирование узла K-5 отключено в профиле.", LogLevel.Warning);
                return true;
            }

            _logger.LogToUser("Тестирование узла K-5", LogLevel.Info);

            var k5Tests = new List<(ushort register, Func<ushort> getStatus, string name, StageK5TestReport report)>
            {
                (2323, () => StandRegisters.K5Stage1Status, "VMAIN", ReportModel.Stage1K5),
                (2325, () => StandRegisters.K5Stage2Status, "VMAIN+VRES", ReportModel.Stage2K5),
                (2327, () => StandRegisters.K5Stage3Status, "VRES", ReportModel.Stage3K5),
                (2325, () => StandRegisters.K5Stage2Status, "VMAIN+VRES (2)", ReportModel.Stage4K5),
                (2323, () => StandRegisters.K5Stage1Status, "VMAIN (2)", ReportModel.Stage5K5),
            };

            bool allSuccess = true;

            foreach (var (reg, getStatus, name, report) in k5Tests)
            {
                _logger.LogToUser($"Тест {name}…", LogLevel.Info);

                bool result = await RunSubTestK5Async(reg, getStatus, name, report, cancellationToken);
                ProgressValue += 5;

                if (!result)
                {
                    allSuccess = false;
                }

                if (cancellationToken.IsCancellationRequested || StandRegisters.RunBtn == 0)
                    return false;
            }

            if (allSuccess)
            {
                K5TestStatus = 2;
                _logger.LogToUser("Тестирование узла K-5 пройдено успешно.", LogLevel.Success);
            }
            else
            {
                K5TestStatus = 3;
                _logger.LogToUser("Тестирование узла K-5 завершено с ошибкой.", LogLevel.Error);
            }

            return allSuccess;
        }




        private async Task<bool> RunSubTestK5Async(ushort startRegister, Func<ushort> getStatus, string testName, StageK5TestReport report, CancellationToken cancellationToken)
        {
            await Task.Delay(2000);
            try
            {


                await WriteToRegisterWithRetryAsync(startRegister, 1);

                while (getStatus() != 1)
                {
                    if (cancellationToken.IsCancellationRequested || StandRegisters.RunBtn == 0)
                    {
                        _logger.LogToUser($"Тест {testName} прерван (кнопка RUN переведена в положение 0).", LogLevel.Warning);
                        K5TestStatus = (ushort)3;
                        return false;
                    }
                    await Task.Delay(500);
                }

                while (true)
                {
                    if (cancellationToken.IsCancellationRequested || StandRegisters.RunBtn == 0)
                    {
                        _logger.LogToUser($"Тест {testName} прерван (кнопка RUN переведена в положение 0).", LogLevel.Warning);
                        K5TestStatus = (ushort)3;
                        return false;
                    }

                    var status = getStatus();

                    if (status == 2 || status == 3)
                    {
                        if (status == 3)
                        {
                            _logger.LogToUser($"Тест {testName} завершился с ошибкой, повторная проверка...", LogLevel.Warning);
                            await Task.Delay(2000);
                            status = getStatus();
                        }

                        bool success = status == 2;

                        report.ResultK5 = success;
                        report.V52Report = StandRegisters.V52Report;
                        report.V55Report = StandRegisters.V55Report;
                        report.VOUTReport = StandRegisters.VOutReport;
                        report.V2048Report = StandRegisters.Ref2048Report;
                        report.V12Report = StandRegisters.V12Report;

                        _logger.LogToUser(
                            $"Результаты измерений: 55V={report.V55Report}; 52V={report.V52Report}; Vout={report.VOUTReport}; 12V={report.V12Report}; Vref={report.V2048Report}",
                            success ? LogLevel.Success : LogLevel.Error
                        );

                        ServerTestResult.AddSubTest(
                            $"K5 Работа от {testName}",
                            success,
                            $"55V={report.V55Report}; 52V={report.V52Report}; Vout={report.VOUTReport}; 12V={report.V12Report}; Vref={report.V2048Report}"
                        );

                        K5TestStatus = (ushort)(success ? 2 : 3);
                        return success;
                    }

                    await Task.Delay(200);
                }
            }
            catch (Exception ex)
            {
                _logger.LogToUser($"Ошибка во время теста {testName}: {ex.Message}", LogLevel.Error);
                K5TestStatus = (ushort)3;
                return false;
            }
        }









        #endregion K5
        #region VCC
        private async Task<bool> RunVCCTestAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (!TestConfig.IsVccTestEnabled)
                {
                    _logger.LogToUser("Тестирование узла VCC отключено в профиле.", LogLevel.Warning);
                    return true;
                }

                _logger.LogToUser("Тестирование внутрисхемных питаний (VCC)", LogLevel.Info);

                if (StandRegisters.V52Out == 0 && StandRegisters.V55Out == 0)
                {
                    _logger.Log("Подача питания на V55 ...", LogLevel.Debug);
                    await WriteToRegisterWithRetryAsync(2302, 1);
                }

                for (int attempt = 1; attempt <= 2; attempt++)
                {
                    await WriteToRegisterWithRetryAsync(2330, 1);

                    while (StandRegisters.VCCTestStatus == 0)
                    {
                        if (cancellationToken.IsCancellationRequested || StandRegisters.RunBtn == 0)
                        {
                            SaveVCCReport(false);
                            _logger.LogToUser("Тест VCC прерван (кнопка RUN переведена в 0).", LogLevel.Warning);
                            return false;
                        }
                        await Task.Delay(500, cancellationToken);
                    }

                    _logger.LogToUser("Ожидание завершения теста VCC...", LogLevel.Info);

                    while (true)
                    {
                        if (cancellationToken.IsCancellationRequested || StandRegisters.RunBtn == 0)
                        {
                            SaveVCCReport(false);
                            _logger.LogToUser("Тест VCC прерван (кнопка RUN переведена в 0).", LogLevel.Warning);
                            return false;
                        }

                        await Task.Delay(5000, cancellationToken);

                        var status = StandRegisters.VCCTestStatus;
                        if (status == 2 || status == 3)
                        {
                            bool success = status == 2;
                            SaveVCCReport(success);
                            ValidateVCCResults();

                            List<string> problems = new();
                            if (V33Status != 2) problems.Add($"3.3V (вне диапазона {TestConfig.Vcc3V3Min}–{TestConfig.Vcc3V3Max})");
                            if (V15Status != 2) problems.Add($"1.5V (вне диапазона {TestConfig.Vcc1V5Min}–{TestConfig.Vcc1V5Max})");
                            if (V11Status != 2) problems.Add($"1.1V (вне диапазона {TestConfig.Vcc1V1Min}–{TestConfig.Vcc1V1Max})");
                            if (CrStatus != 2) problems.Add($"CR2032 (вне диапазона {TestConfig.CR2032Min}–{TestConfig.CR2032Max})");
                            if (CrCpuStatus != 2) problems.Add($"CR2032 CPU (вне диапазона {TestConfig.CR2032CpuMin}–{TestConfig.CR2032CpuMax})");

                            string measurementResults = $"3.3V={ReportModel.VCC.V33Report}; 1.5V={ReportModel.VCC.V15Report}; " +
                                                        $"1.1V={ReportModel.VCC.V11Report}; CR2032={ReportModel.VCC.CR2032Report}; " +
                                                        $"CR2032 CPU={ReportModel.VCC.CpuCR2032Report}";

                            if (!success && problems.Any())
                                measurementResults += $"; Нарушения: {string.Join(", ", problems)}";

                            _logger.LogToUser($"Результаты измерений: {measurementResults}", success ? LogLevel.Info : LogLevel.Warning);
                            ServerTestResult.AddSubTest("VCC", success, measurementResults);

                            if (success)
                            {
                                _logger.LogToUser("Тестирование узла VCC пройдено успешно.", LogLevel.Success);
                                return true;
                            }

                            if (attempt == 1)
                            {
                                _logger.LogToUser("Повторный запуск теста VCC...", LogLevel.Warning);
                                break; // ещё одна попытка
                            }

                            _logger.LogToUser("Тестирование узла VCC завершено с ошибкой.", LogLevel.Error);
                            return false;
                        }
                    }
                }

                _logger.LogToUser("Тест VCC завершён с ошибкой после повторной попытки.", LogLevel.Error);
                return false;
            }
            catch (TaskCanceledException)
            {
                SaveVCCReport(false);
                _logger.LogToUser("Тест VCC отменён.", LogLevel.Warning);
                return false;
            }
            catch (Exception ex)
            {
                SaveVCCReport(false);
                _logger.LogToUser($"Ошибка во время теста VCC: {ex.Message}", LogLevel.Error);
                return false;
            }
        }



        private void ValidateVCCResults()
        {
            V33Status = ValidateRange("3.3V", StandRegisters.V3_3Report, TestConfig.Vcc3V3Min, TestConfig.Vcc3V3Max);
            V15Status = ValidateRange("1.5V", StandRegisters.V1_5Report, TestConfig.Vcc1V5Min, TestConfig.Vcc1V5Max);
            V11Status = ValidateRange("1.1V", StandRegisters.V1_1Report, TestConfig.Vcc1V1Min, TestConfig.Vcc1V1Max);
            CrStatus = ValidateRange("CR2032", StandRegisters.CR2032Report, TestConfig.CR2032Min, TestConfig.CR2032Max);
            CrCpuStatus = ValidateRange("CR2032 CPU", StandRegisters.CR2032_CPUReport, TestConfig.CR2032CpuMin, TestConfig.CR2032CpuMax);
        }

        private ushort ValidateRange(string label, double value, double min, double max)
        {
            if (Math.Abs(value - min) < 0.0001 || Math.Abs(value - max) < 0.0001 || value < min || value > max)
            {
                _logger.LogToUser($"{label} вне диапазона: {value} (допустимо от {min} до {max}).", LogLevel.Warning);
                return 3; // Ошибка
            }

            return 2; // OK
        }


        private void SaveVCCReport(bool isSuccess)
        {
            ReportModel.VCC.ResultVcc = isSuccess;
            ReportModel.VCC.V33Report = StandRegisters.V3_3Report;
            ReportModel.VCC.V15Report = StandRegisters.V1_5Report;
            ReportModel.VCC.V11Report = StandRegisters.V1_1Report;
            ReportModel.VCC.CR2032Report = StandRegisters.CR2032Report;
            ReportModel.VCC.CpuCR2032Report = StandRegisters.CR2032_CPUReport;
        }






        #endregion VCC
        #region прошивка flash
        private bool _isFirstFlashProgramming;
        public async Task<bool> StartProgrammingAsync(CancellationToken cancellationToken)
        {
            if (!TestConfig.IsFlashProgrammingEnabled)
            {
                _logger.LogToUser("Прошивка FLASH отключена в настройках.", LogLevel.Warning);
                ReportModel.FlashReport.FlashResult = false; 
                return true;
            }

            if (_isFirstFlashProgramming && TestConfig.IsFlashProgrammingEnabled)
            {
                await OpenFlashProgramAsync();
                return true;
            }

            await WriteToRegisterWithRetryAsync(2307, 1);

            string programPath = Properties.Settings.Default.FlashProgramPath;
            string projectPath = Properties.Settings.Default.FlashFirmwarePath;
            int delay = TestConfig.FlashDelay * 1000;

            if (string.IsNullOrWhiteSpace(programPath) || !File.Exists(programPath))
            {
                _logger.LogToUser($"Программа для прошивки не найдена: {programPath}", LogLevel.Error);
                ReportModel.FlashReport.FlashResult = false;
                ReportModel.FlashReport.FlashErrorMessage = "Программа для прошивки не найдена";
                ServerTestResult.AddSubTest($"прошивка Flash", false, $"{ReportModel.FlashReport.FlashErrorMessage}; Path ={projectPath}");

                return false;
            }

            if (string.IsNullOrWhiteSpace(projectPath) || !File.Exists(projectPath))
            {
                _logger.LogToUser($"Файл проекта для прошивки не найден: {projectPath}", LogLevel.Error);
                ReportModel.FlashReport.FlashResult = false;
                ReportModel.FlashReport.FlashErrorMessage = "Файл проекта для прошивки не найден";
                ServerTestResult.AddSubTest($"прошивка Flash", false, $"{ReportModel.FlashReport.FlashErrorMessage}; Path ={projectPath}");
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
                    _logger.LogToUser("Программа прошивки уже запущена. Переключение фокуса...", LogLevel.Info);
                }

                if (cancellationToken.IsCancellationRequested || StandRegisters.RunBtn == 0)
                    throw new OperationCanceledException("Прошивка отменена пользователем.");

                IntPtr hWnd = programProcess.MainWindowHandle;
                if (hWnd == IntPtr.Zero)
                {
                    _logger.LogToUser("Ошибка: Не удалось найти главное окно программы.", LogLevel.Error);
                    ReportModel.FlashReport.FlashResult = false;
                    ReportModel.FlashReport.FlashErrorMessage = "Не удалось найти главное окно программы";
                    ServerTestResult.AddSubTest($"прошивка Flash", false, $"{ReportModel.FlashReport.FlashErrorMessage}; Path ={projectPath}");
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
                    ReportModel.FlashReport.FlashResult = false;
                    ReportModel.FlashReport.FlashErrorMessage = $"Ошибка вставки пути: {ex.Message}";
                    ServerTestResult.AddSubTest($"прошивка Flash", false, $"{ReportModel.FlashReport.FlashErrorMessage}; Path ={projectPath}");
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
                await Task.Delay(180000, cancellationToken); // Задержка 180 секунд
                _logger.LogToUser("Прошивка завершена. Закрытие программы прошивки...", LogLevel.Info);
                if (programProcess != null && !programProcess.HasExited)
                {
                    programProcess.Kill();
                    _logger.LogToUser("Программа прошивки успешно закрыта.", LogLevel.Info);
                }

                IntPtr mainWindowHandle = Process.GetCurrentProcess().MainWindowHandle;
                SetForegroundWindow(mainWindowHandle);
                _logger.LogToUser("Переключение обратно на стенд завершено.", LogLevel.Info);

                ReportModel.FlashReport.FlashResult = true;
                ServerTestResult.AddSubTest($"прошивка Flash", true, $"Path={projectPath}");
                return true;
            }
            catch (OperationCanceledException)
            {
                _logger.LogToUser("Прошивка была прервана пользователем.", LogLevel.Warning);
                ReportModel.FlashReport.FlashResult = false;
                ReportModel.FlashReport.FlashErrorMessage = "Прошивка отменена пользователем.";
                ServerTestResult.AddSubTest($"прошивка Flash", false, $"{ReportModel.FlashReport.FlashErrorMessage}; Path ={projectPath}");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogToUser($"Ошибка во время прошивки: {ex.Message}", LogLevel.Error);
                ReportModel.FlashReport.FlashResult = false;
                ReportModel.FlashReport.FlashErrorMessage = ex.Message;
                ServerTestResult.AddSubTest($"прошивка Flash", false, $"{ReportModel.FlashReport.FlashErrorMessage}; Path ={projectPath}");
                return false;
            }
        }



        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        #endregion прошивка flash
        #region прошивка 2

        private async Task<bool> FlashMcuAsync(CancellationToken cancellationToken)
        {
            if (!TestConfig.IsMcuProgrammingEnabled)
            {
                _logger.LogToUser("Прошивка MCU отключена в профиле тестирования.", LogLevel.Warning);
                return true; // Пропускаем прошивку
            }

            if (StandRegisters.V52Out == 0)
            {
                _logger.LogToUser("Подача питания на V52 ...", LogLevel.Debug);
                await WriteToRegisterWithRetryAsync(2301, 1);
            }
            string flashToolPath = Properties.Settings.Default.SwdProgramPath; // Путь к flash.bat  
            string firmwarePath = Properties.Settings.Default.SwdFirmwarePath; // Путь к .bin
            string workingDirectory = Path.GetDirectoryName(flashToolPath); // Рабочая директория

            _logger.Log("Подготовка к прошивке MCU...", LogLevel.Info);

            if (!File.Exists(flashToolPath))
            {
                _logger.LogToUser($"Ошибка: Не найден скрипт прошивки по пути {flashToolPath}.", LogLevel.Error);

                ServerTestResult.AddSubTest($"прошивка SWD", false, $"скрипт прошивки {flashToolPath} не найден");
                return false;
            }

            if (string.IsNullOrEmpty(firmwarePath) || !File.Exists(firmwarePath))
            {
                _logger.LogToUser($"Ошибка: Файл прошивки {firmwarePath} не найден.", LogLevel.Error);
                ServerTestResult.AddSubTest($"прошивка SWD", false, $"Файл прошивки {firmwarePath} не найден");
                return false;
            }

            try
            {
                _logger.LogToUser("Включаем питание на стенде перед прошивкой...", LogLevel.Info);
                await WriteToRegisterWithRetryAsync(2301, 1, 3);
                await Task.Delay(1000, cancellationToken); // Ждём 1 секунду перед прошивкой

                string formattedFirmwarePath = $"\"{firmwarePath.Replace("\\", "/")}\"";
                _logger.LogToUser($"Используемый файл прошивки: {firmwarePath}", LogLevel.Debug);
                _logger.LogToUser("Запуск прошивки MCU...", LogLevel.Info);

                var processStartInfo = new ProcessStartInfo
                {
                    FileName = flashToolPath,
                    Arguments = formattedFirmwarePath,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = workingDirectory
                };

                _logger.LogToUser($"Запуск процесса: {flashToolPath}", LogLevel.Debug);
                _logger.LogToUser($"Аргументы процесса: {processStartInfo.Arguments}", LogLevel.Debug);
                _logger.LogToUser($"Рабочая директория: {processStartInfo.WorkingDirectory}", LogLevel.Debug);

                DateTime startTime = DateTime.Now;

                using (var process = new Process { StartInfo = processStartInfo })
                {
                    process.OutputDataReceived += (sender, e) => { if (!string.IsNullOrEmpty(e.Data)) _logger.Log(e.Data, LogLevel.Debug); };
                    process.ErrorDataReceived += (sender, e) => { if (!string.IsNullOrEmpty(e.Data)) _logger.Log(e.Data, LogLevel.Error); };

                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    _logger.LogToUser("Ожидание завершения прошивки...", LogLevel.Info);
                    await process.WaitForExitAsync(cancellationToken);

                    DateTime endTime = DateTime.Now;
                    double duration = (endTime - startTime).TotalSeconds;

                    _logger.LogToUser($"Прошивка завершилась за {duration:F2} секунд.", LogLevel.Info);
                    _logger.LogToUser($"Код выхода процесса: {process.ExitCode}", LogLevel.Debug);

                    if (process.ExitCode != 0)
                    {
                        _logger.LogToUser($"Ошибка прошивки! Код выхода: {process.ExitCode}", LogLevel.Error);

                        ServerTestResult.AddSubTest($"прошивка SWD", false, $"{process.ExitCode}");
                        return false;
                    }

                    _logger.LogToUser("Прошивка завершена успешно!", LogLevel.Success);

                    ServerTestResult.AddSubTest($"прошивка SWD", true, $"{firmwarePath}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogToUser($"Ошибка во время прошивки MCU: {ex.Message}", LogLevel.Error);
                ServerTestResult.AddSubTest($"прошивка SWD", false, $"{ex.Message}");
                return false;
            }
        }


        #endregion прошивка 2
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
                _logger.LogToUser("Самотестирование DUT отключено, тест пропущен.", LogLevel.Warning);
                return true; 
            }
            ProgressValue += 5;
            // Ожидание загрузки DUT после прошивки
            if (!await WaitForDUTReadyAsync(cancellationToken, false, 30, 180))
            {
                RtlStatus = 3;
                ConsoleStatus = 3;
                _logger.LogToUser("DUT не готов после прошивки.", LogLevel.Error);
                await StopHard();
                await LoadSwReport();
                return false;
            }
            ConsoleStatus = 2;
            ProgressValue += 5;
            // Самотестирование
            if (TestConfig.DutSelfTest)
            {
                if (!await RunSelfTestAsync(cancellationToken))
                {
                    RtlStatus = 3;
                    _logger.LogToUser("Самотестирование завершилось с ошибкой.", LogLevel.Error);
                    await StopHard();
                    await LoadSwReport();
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
                    RtlStatus = 3;
                    Sensor1Status = 3;
                    _logger.LogToUser("Тест SENSOR1 завершился с ошибкой.", LogLevel.Error);
                    await StopHard();
                    await LoadSwReport();
                    return false;
                }
            }
            else
            {
                _logger.LogToUser("Тест SENSOR1 пропущен (отключен в конфигурации).", LogLevel.Info);
            }
            Sensor1Status = 2;
            if (cancellationToken.IsCancellationRequested) return false;
            ProgressValue += 5;
            // SENSOR2
            if (TestConfig.DutSensor2Test)
            {
                if (!await RunSensorTestAsync(2304, "sensor2", cancellationToken))
                {
                    RtlStatus = 3;
                    Sensor2Status = 3;
                    _logger.LogToUser("Тест SENSOR2 завершился с ошибкой.", LogLevel.Error);
                    await StopHard();
                    await LoadSwReport();
                    return false;
                }
            }
            else
            {
                _logger.LogToUser("Тест SENSOR2 пропущен (отключен в конфигурации).", LogLevel.Info);
            }
            Sensor2Status = 2;
            ProgressValue += 5;
            // RELAY

            if (TestConfig.DutRelayTest)
            {
                if (!await RunRelayTestAsync(2306, cancellationToken))
                {
                    RtlStatus = 3;
                    RelayStatus = 3;
                    _logger.LogToUser("Тест RELAY завершился с ошибкой.", LogLevel.Error);
                    await StopHard();
                    await LoadSwReport();
                    return false;
                }
            }
            else
            {
                _logger.LogToUser("Тест RELAY пропущен (отключен в конфигурации).", LogLevel.Info);
            }
            RelayStatus = 2;
            ProgressValue += 5;
            // TAMPER
            if (TestConfig.DutTamperTest)
            {
                if (!await RunTamperTestAsync(cancellationToken))
                {
                    RtlStatus = 3;
                    TamperStatus = 3;
                    _logger.LogToUser("Тестирование TAMPER завершилось с ошибкой.", LogLevel.Error);
                    await StopHard();
                    await LoadSwReport();
                    return false;
                }
            }
            else
            {
                _logger.LogToUser("Тест TAMPER пропущен (отключен в конфигурации).", LogLevel.Info);
                TamperStatus = 1;
            }
            
            ProgressValue += 5;
            // RS485

            if (TestConfig.DutRs485Test)
            {
                if (!await RunRS485TestAsync(cancellationToken))
                {
                    RtlStatus = 3;
                    Rs485Status = 3;
                    _logger.LogToUser("Тестирование RS485 завершилось с ошибкой.", LogLevel.Error);
                    await StopHard();
                    await LoadSwReport();
                    return false;
                }
            }
            else
            {

                _logger.LogToUser("Тест RS485 пропущен (отключен в конфигурации).", LogLevel.Info);
            }
            Rs485Status = 2;
            ProgressValue += 5;
            // I2C
            if (TestConfig.DutI2CTest)
            {
                if (!await RunI2CTestAsync(cancellationToken))
                {
                    RtlStatus = 3;
                    I2CStatus = 3;
                    _logger.LogToUser("Тестирование I2C завершилось с ошибкой.", LogLevel.Error);
                    await StopHard();
                    await LoadSwReport();
                    return false;
                }
            }
            else
            {
                _logger.LogToUser("Тест I2C пропущен (отключен в конфигурации).", LogLevel.Info);
            }
            I2CStatus = 2;
            ProgressValue += 5;
            // POE
            if (TestConfig.DutPoeTest)
            {
                if (!await RunPoETestAsync(cancellationToken))
                {
                    RtlStatus = 3;
                    PoeStatus = 3;
                    _logger.LogToUser("Тестирование POE завершилось с ошибкой.", LogLevel.Error);
                    await StopHard();
                    await LoadSwReport();
                    return false;
                }
            }
            else
            {
                _logger.LogToUser("Тест POE пропущен (отключен в конфигурации).", LogLevel.Info);
            }
            PoeStatus = 2;

            // 2.15 Получение серийного номера
            if (!await RunSerialNumberTestAsync(cancellationToken))
            {
                RtlStatus = 3;
                _logger.LogToUser("Ошибка получения серийного номера.", LogLevel.Error);
                await StopHard();
                await LoadSwReport();
                return false;
            }


            ProgressValue += 5;

            return true;
        }

        #region печать этикетки


        private async Task<bool> PrintLabelAsync()
        {
            try
            {
                if (!TestConfig.IsLabelPrintingEnabled)
                {
                    _logger.LogToUser("Печать этикетки отключена в профиле.", LogLevel.Warning);
                    return true;
                }

                if (!isSwTestSuccess)
                {
                    _logger.LogToUser("Тестирование завершено с ошибкой, печать этикетки не будет выполнена.", LogLevel.Warning);
                    return false;
                }


                string serialNumber = ServerTestResult.deviceSerial;
                //string serialNumber = "1488"; // Заменишь потом на ServerTestResult.deviceSerial;
                if (string.IsNullOrEmpty(serialNumber))
                {
                    _logger.LogToUser("Серийный номер не доступен для печати.", LogLevel.Error);
                    return false;
                }

                string barcode = serialNumber;
                _logger.LogToUser($"Подготовка к печати: Barcode={barcode}, Serial={serialNumber}", LogLevel.Debug);

                string printerName = "TSC TE310";

                // Инициализация принтера
                if (_printerService == null)
                {
                    try
                    {
                        _printerService = new TscPrinterService(printerName);
                        _logger.LogToUser("Принтер инициализирован.", LogLevel.Debug);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogToUser($"Ошибка при инициализации принтера: {ex.Message}", LogLevel.Error);
                        return false;
                    }
                }

                // Проверка установлен ли
                if (!_printerService.IsPrinterInstalled())
                {
                    _logger.LogToUser($"Принтер \"{printerName}\" не найден в системе.", LogLevel.Error);
                    return false;
                }

                // Проверка в онлайне ли он
                if (!_printerService.IsPrinterOnline())
                {
                    _logger.LogToUser($"Принтер \"{printerName}\" оффлайн или не готов.", LogLevel.Error);
                    return false;
                }

                _logger.LogToUser($"Отправка данных на принтер: Barcode={barcode}, Serial={serialNumber}", LogLevel.Debug);

                bool result = _printerService.PrintLabel(barcode, serialNumber);

                if (result)
                {
                    _logger.LogToUser("Печать завершена успешно!", LogLevel.Success);
                    return true;
                }
                else
                {
                    _logger.LogToUser("Печать завершена с ошибкой. Принтер вернул false.", LogLevel.Error);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogToUser($"Ошибка печати: {ex.Message}", LogLevel.Error);
                _logger.Log($"StackTrace: {ex.StackTrace}", LogLevel.Debug);
                return false;
            }
        }


        #endregion печать этикетки
        #region серийник
        private async Task<bool> RunSerialNumberTestAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogToUser("Получение уникального идентификатора платы (DEVICE_ID)...", LogLevel.Info);

                // Запрос 
                string deviceIdResponse = await SendConsoleCommandAsync("ubus call tf_hwsys getParam '{\"name\":\"DEVICE_ID\"}'");

                // Парсим 
                string deviceId = ParseDeviceId(deviceIdResponse);
                if (string.IsNullOrEmpty(deviceId))
                {
                    _logger.Log("Не удалось получить DEVICE_ID. Будем запрашивать серийный номер без него.", LogLevel.Warning);
                    //return false; // Если deviceId не удалось получить, не продолжаем тестирование
                }
                else
                {
                    _logger.LogToUser($"Уникальный идентификатор платы: {deviceId}", LogLevel.Success);
                    ServerTestResult.deviceIdent = deviceId;
                }
                return true;
                // Получаем серийник с сервера
                string serialNumber = await GetSerialNumberFromServer(deviceId);
                if (string.IsNullOrEmpty(serialNumber))
                {
                    _logger.LogToUser("❌ Серийный номер не получен!", LogLevel.Error);

                    return false;
                }

                _logger.LogToUser($"✅ Серийный номер платы: {serialNumber}", LogLevel.Success);


                // Переопределим ServerTestResult для использования полученного серийного номера
                ServerTestResult.deviceSerial = serialNumber;

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogToUser($"Ошибка во время запроса серийного номера: {ex.Message}", LogLevel.Error);
                ServerTestResult.AddSubTest($"SerialNumber", false, $"{ex.Message}");

                return false;
            }
        }


        private string ParseDeviceId(string response)
        {
            try
            {
                // Обрезаем первые 52 символа
                if (response.Length > 52)
                {
                    response = response.Substring(52);
                }
                else
                {
                    throw new Exception("Ответ слишком короткий для удаления фиксированного количества символов.");
                }

                // Обрезаем возможные лишние символы в конце
                if (response.Length > 20)
                {
                    response = response.Substring(0, response.Length - 19);
                }
                else
                {
                    throw new Exception("Ответ слишком короткий для удаления 20 символов в конце.");
                }

                // Добавляем закрывающую фигурную скобку в конце, если ее нет
                if (!response.EndsWith("}"))
                {
                    response += "}";
                }

                response = response.Trim();

                _logger.Log($"Ответ после очистки: {response}", LogLevel.Debug);

                var jsonObject = Newtonsoft.Json.Linq.JObject.Parse(response);
                string deviceId = jsonObject.SelectToken("DEVICE_ID")?.ToString();

                if (string.IsNullOrEmpty(deviceId))
                {
                    throw new Exception("Поле DEVICE_ID отсутствует в JSON-ответе.");
                }

                return deviceId;
            }
            catch (Exception ex)
            {
                _logger.Log($"Ошибка парсинга DEVICE_ID: {ex.Message}", LogLevel.Error);
                _logger.Log($"Сырой ответ: {response}", LogLevel.Error);
                return null;
            }
        }

        #endregion серийник


        private async Task<string> GetSerialNumberFromServer(string deviceId)
        {
            try
            {
                string sessionId = SessionId; // sessionId получен ранее при авторизации
                string devType = "RTL-SW"; // Тип устройства из профиля

                if (string.IsNullOrEmpty(sessionId))
                {
                    _logger.LogToUser("Ошибка: нет sessionId, запрос невозможен!", LogLevel.Error);
                    return null;
                }

                // Формируем URL запроса
                string url = $"http://iccid.fort-telecom.ru/api/Api.svc/getSerialNum?devType={devType}";
                if (!string.IsNullOrEmpty(deviceId))
                {
                    url += $"&cpuId={deviceId}";
                }

                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("User-Agent", "ftstand");
                    client.DefaultRequestHeaders.Add("Cookie", $"SGUID=session_id={sessionId}&login=");

                    HttpResponseMessage response = await client.GetAsync(url);
                    string responseContent = await response.Content.ReadAsStringAsync();

                    _logger.Log($"Ответ сервера (серийник): {responseContent}", LogLevel.Debug);

                    if (!response.IsSuccessStatusCode)
                    {
                        _logger.LogToUser($"Ошибка получения серийного номера. Код: {response.StatusCode}", LogLevel.Warning);
                        return null;
                    }

                    // Серийник должен быть просто строкой в ответе
                    return responseContent.Trim('"');
                }
            }
            catch (Exception ex)
            {
                _logger.Log($"Ошибка запроса серийного номера: {ex.Message}", LogLevel.Error);
                return null;
            }
        }


        #region Самотестирование
        private async Task<bool> RunSelfTestAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogToUser("Запуск самотестирования DUT...", LogLevel.Info);

                string[] errorParams = { "HW_ERR1", "HW_ERR2", "HW_ERR3" };
                var errorsDetected = new List<string>();

                foreach (var param in errorParams)
                {
                    if (cancellationToken.IsCancellationRequested || StandRegisters.RunBtn == 0)
                    {
                        _logger.LogToUser("Самотестирование прервано пользователем.", LogLevel.Warning);
                        ServerTestResult.AddSubTest("SelfTest", false, "тест прерван вручную");
                        return false;
                    }

                    string command = $"ubus call tf_hwsys getParam '{{\"name\":\"{param}\"}}'";
                    string result = await SendConsoleCommandAsync(command);

                    _logger.Log($"Результат {param}: {result}", LogLevel.Debug);

                    // Пытаемся извлечь значение параметра (ожидается формат "HW_ERRx": "0")
                    string expectedKey = $"\"{param}\": \"";
                    int index = result.IndexOf(expectedKey);
                    if (index == -1)
                    {
                        _logger.Log($"Ошибка парсинга ответа для {param}. Ответ: {result}", LogLevel.Error);
                        errorsDetected.Add($"{param}=недоступен");
                        continue;
                    }

                    string value = result.Substring(index + expectedKey.Length, 1); // только 1 символ: "0" или "1"
                    if (value != "0")
                    {
                        errorsDetected.Add($"{param}={value}");
                    }
                }

                if (errorsDetected.Count > 0)
                {
                    string errorMessage = "Обнаружены ошибки самотестирования: " + string.Join(", ", errorsDetected);
                    _logger.LogToUser(errorMessage, LogLevel.Error);
                    ServerTestResult.AddSubTest("SelfTest", false, errorMessage);
                    return false;
                }

                _logger.LogToUser("Самотестирование DUT завершено успешно.", LogLevel.Success);
                ServerTestResult.AddSubTest("SelfTest", true, "1");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogToUser($"Ошибка при выполнении самотестирования DUT: {ex.Message}", LogLevel.Error);
                ServerTestResult.AddSubTest("SelfTest", false, ex.Message);
                return false;
            }
        }


        #endregion Самотестирование 
        #region SENSOR 1 + 2 

        private async Task<bool> RunSensorTestAsync(ushort modbusRegister, string sensorName, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogToUser($"Тестирование {sensorName}...", LogLevel.Info);

                // Устанавливаем 0 в регистр
                await WriteToRegisterWithRetryAsync(modbusRegister, 0);
                await Task.Delay(2000, cancellationToken);
                if (cancellationToken.IsCancellationRequested || StandRegisters.RunBtn == 0) return false;
                if (!await VerifySensorStatus(sensorName, "0", cancellationToken)) return false;

                // Устанавливаем 1 в регистр
                await WriteToRegisterWithRetryAsync(modbusRegister, 1);
                await Task.Delay(2000, cancellationToken);
                if (cancellationToken.IsCancellationRequested || StandRegisters.RunBtn == 0) return false;
                if (!await VerifySensorStatus(sensorName, "1", cancellationToken)) return false;

                // Возвращаем 0
                await WriteToRegisterWithRetryAsync(modbusRegister, 0);
                await Task.Delay(2000, cancellationToken);
                if (cancellationToken.IsCancellationRequested || StandRegisters.RunBtn == 0) return false;
                if (!await VerifySensorStatus(sensorName, "0", cancellationToken)) return false;

                _logger.LogToUser($"Тестирование {sensorName} успешно завершено.", LogLevel.Success);
                ServerTestResult.AddSubTest($"{sensorName}", true, $"1");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogToUser($"Ошибка во время тестирования {sensorName}: {ex.Message}", LogLevel.Error);
                ServerTestResult.AddSubTest($"{sensorName}", false, $"{ex.Message}");
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
                    ServerTestResult.AddSubTest($"{sensorName}", false, $"Ошибка: ожидалось {expectedStatus}, получено {sensorStatus}");
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
                _logger.LogToUser("Тестирование RELAY......", LogLevel.Info);

                await SendConsoleCommandAsync("ubus call tf_hwsys setParam '{\"name\":\"relay\",\"value\":\"0\"}'");
                _logger.Log("Реле переведено в состояние 0 через консоль.", LogLevel.Info);

                await Task.Delay(5000, cancellationToken);
                if (cancellationToken.IsCancellationRequested || StandRegisters.RunBtn == 0)
                {
                    _logger.LogToUser("Тест RELAY прерван.", LogLevel.Warning);
                    ServerTestResult.AddSubTest($"Relay", false, $"тест прерван вручную");
                    return false;
                }

                if (StandRegisters.RelayIn != 0)
                {
                    _logger.Log($"Состояние реле в регистре {relayStatusRegister} не совпадает с ожидаемым (0).", LogLevel.Warning);
                    ServerTestResult.AddSubTest($"Relay", false, $"Ошибка: ожидалось ожидалось 0, получено {StandRegisters.RelayIn}");
                    return false;
                }

                await SendConsoleCommandAsync("ubus call tf_hwsys setParam '{\"name\":\"relay\",\"value\":\"1\"}'");
                _logger.Log("Реле переведено в состояние 1 через консоль.", LogLevel.Info);

                await Task.Delay(5000, cancellationToken);
                if (cancellationToken.IsCancellationRequested || StandRegisters.RunBtn == 0)
                {
                    _logger.LogToUser("Тест RELAY прерван.", LogLevel.Warning);
                    ServerTestResult.AddSubTest($"Relay", false, $"тест прерван вручную");
                    return false;
                }

                if (StandRegisters.RelayIn != 1)
                {
                    _logger.Log($"Состояние реле в регистре {relayStatusRegister} не совпадает с ожидаемым (1).", LogLevel.Warning);
                    ServerTestResult.AddSubTest($"Relay", false, $"Ошибка: ожидалось ожидалось 1, получено {StandRegisters.RelayIn}");
                    return false;
                }

                await SendConsoleCommandAsync("ubus call tf_hwsys setParam '{\"name\":\"relay\",\"value\":\"0\"}'");
                _logger.Log("Реле возвращено в состояние 0 через консоль.", LogLevel.Info);


                await Task.Delay(8000, cancellationToken);
                if (cancellationToken.IsCancellationRequested || StandRegisters.RunBtn == 0)
                {
                    _logger.LogToUser("Тест RELAY прерван.", LogLevel.Warning);
                    ServerTestResult.AddSubTest($"Relay", false, $"тест прерван вручную");
                    return false;
                }

                if (StandRegisters.RelayIn != 0)
                {
                    _logger.Log($"Состояние реле в регистре {relayStatusRegister} не совпадает с ожидаемым (0).", LogLevel.Warning);
                    ServerTestResult.AddSubTest($"Relay", false, $"Ошибка: ожидалось ожидалось 0, получено {StandRegisters.RelayIn}");
                    return false;
                }

                _logger.LogToUser("Тестирование релейного выхода успешно завершено.", LogLevel.Success);
                ServerTestResult.AddSubTest($"Relay", true, $"1");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogToUser($"Ошибка при тестировании реле: {ex.Message}", LogLevel.Error);
                ServerTestResult.AddSubTest($"Relay", false, $"{ex.Message}");
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
                    if (cancellationToken.IsCancellationRequested || StandRegisters.RunBtn == 0)
                    {
                        _logger.LogToUser("Тест Tamper прерван.", LogLevel.Warning);
                        ServerTestResult.AddSubTest($"Tamper", false, $"тест прерван вручную");
                        return false;
                    }

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
                        await Task.Delay(9000, cancellationToken);
                    }
                }

                _logger.Log($"Ошибка: Tamper после повторной проверки имеет неверное состояние. Ожидалось {expectedStatus}.", LogLevel.Error);
                ServerTestResult.AddSubTest($"Tamper", false, $"Tamper после повторной проверки имеет неверное состояние. Ожидалось {expectedStatus}");
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
                _logger.LogToUser("Тестирование TAMPER...", LogLevel.Info);

                // Получаем диапазоны из профиля тестирования
                ushort minStatusTamper = TestConfig.DutTamperStatusMin;
                ushort maxStatusTamper = TestConfig.DutTamperStatusMax;
                ushort minTamperLed = TestConfig.DutTamperLedMin;
                ushort maxTamperLed = TestConfig.DutTamperLedMax;

                // 1) Отключаем Tamper (2305 = 0)
                await WriteToRegisterWithRetryAsync(2305, 0);
                _logger.Log("2305 = 0 (Tamper отключён)", LogLevel.Debug);
                await Task.Delay(5000, cancellationToken);
                if (cancellationToken.IsCancellationRequested || StandRegisters.RunBtn == 0) return false;

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
                await WriteToRegisterWithRetryAsync(2305, 1);
                _logger.Log("2305 = 1 (Tamper включён)", LogLevel.Debug);
                await Task.Delay(5000, cancellationToken);
                if (cancellationToken.IsCancellationRequested || StandRegisters.RunBtn == 0) return false;

                // 6) Проверяем Tamper через консоль (должно быть 1)
                if (!await VerifyTamperStatus("1", "Включение Tamper"))
                    return false;

                // 7) Отключаем Tamper (2305 = 0)
                await WriteToRegisterWithRetryAsync(2305, 0);
                _logger.Log("2305 = 0 (Tamper отключён)", LogLevel.Debug);
                await Task.Delay(5000, cancellationToken);
                if (cancellationToken.IsCancellationRequested || StandRegisters.RunBtn == 0) return false;

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
                ServerTestResult.AddSubTest($"Tamper", true, $"1");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogToUser($"Ошибка во время тестирования Tamper: {ex.Message}", LogLevel.Error);
                ServerTestResult.AddSubTest($"Tamper", false, $"{ex.Message}");
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
                        ServerTestResult.AddSubTest($"RS485", false, $"S485 имеет состояние {rs485Status}, ожидалось {expectedStatus}.");
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
                ServerTestResult.AddSubTest($"RS485", true, $"1");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogToUser($"Ошибка во время тестирования RS485: {ex.Message}", LogLevel.Error);
                ServerTestResult.AddSubTest($"RS485", false, $"{ex.Message}");
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
                    ServerTestResult.AddSubTest($"I2C", false, $"I2C не подключен");

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
                    ServerTestResult.AddSubTest($"I2C", false, $"Не удалось извлечь температуру из ответа");
                    return false;
                }

                _logger.LogToUser($"Результаты измерений:  {temperature} °C.", LogLevel.Info);
                _logger.LogToUser("Тестирование I2C успешно завершено.", LogLevel.Success);
                ServerTestResult.AddSubTest($"I2C", true, $"1");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogToUser($"Ошибка во время тестирования I2C: {ex.Message}", LogLevel.Error);
                ServerTestResult.AddSubTest($"I2C", false, $"{ex.Message}");
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
                _logger.LogToUser("Тестирование PoE интерфейса…", LogLevel.Info);

                // Выполнение команды для получения информации о PoE
                string poeResponse = await SendConsoleCommandAsync("ubus call poe info");

                if (string.IsNullOrWhiteSpace(poeResponse))
                {
                    _logger.LogToUser("Ошибка: Команда 'ubus call poe info' не вернула ответ.", LogLevel.Error);
                    ServerTestResult.AddSubTest($"poe", false, $"Команда 'ubus call poe info' не вернула ответ");

                    return false;
                }

                _logger.Log($"Получен ответ от PoE: {poeResponse}", LogLevel.Info);

                if (poeResponse.Contains("\"budget\":"))
                {
                    _logger.LogToUser("Тестирование PoE интерфейса завершено успешно.", LogLevel.Success);
                    ServerTestResult.AddSubTest($"poe", true, $"1");
                    return true;
                }

                _logger.Log("Ошибка: В ответе отсутствует ключ 'budget'.", LogLevel.Error);
                ServerTestResult.AddSubTest($"poe", false, $"В ответе отсутствует ключ 'budget'");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogToUser($"Ошибка во время тестирования PoE: {ex.Message}", LogLevel.Error);
                ServerTestResult.AddSubTest($"poe", false, $"{ex.Message}");
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


        public static TestResult ServerTestResult;
        public RtlSwViewModel(Loggers logger, ReportService report)
        {

            SessionId = App.StartupSessionId;
            _isFirstFlashProgramming = true; // для первоначальной настройки xgpro.exe

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            _printerService = new TscPrinterService("TSC TE310");

            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger.Log("RtlSwViewModel инициализирован", Loggers.LogLevel.Success);

            ToggleModbusConnectionCommand = new RelayCommand(async () => await ToggleModbusConnection(), CanExecuteCommand);
            ConnectToServerCommand = new RelayCommand(async () => await TryConnectToServerAsync(), CanExecuteCommand);
            LoadTestProfileCommand = new RelayCommand(async () => await TryLoadTestProfileAsync(), CanExecuteCommand);
            OpenFlashProgramCommand = new AsyncRelayCommand(OpenFlashProgramAsync, () => true);
            OpenSwdProgramCommand = new AsyncRelayCommand(OpenSwdProgramAsync, () => true);
            ConnectCommand = new AsyncRelayCommand(ToggleConnectionAsync);  // Кнопка "Подключиться к стенду

            if (TestConfig != null)
            {
                TestConfig.PropertyChanged += ProfileTest_PropertyChanged;
            }
            else
            {
                _logger.Log("Ошибка: TestConfig не инициализирован!", Loggers.LogLevel.Error);
            }


            /*Task.Run(async () => // Автоматическое подключение к стенду
            {
                await Task.Delay(1000);
                await ToggleConnectionAsync();
            });*/
        }


        private async Task OpenFlashProgramAsync()
        {
            try
            {
                string exePath = Properties.Settings.Default.FlashProgramPath;
                string tempPdfPath = Path.Combine(Path.GetTempPath(), "Прошивка.pdf");

                if (!File.Exists(exePath))
                {
                    _logger.LogToUser($"Файл прошивальщика не найден: {exePath}", LogLevel.Error);
                    return;
                }

                // Подача питания
                await WriteToRegisterWithRetryAsync(2301, 1);
                await WriteToRegisterWithRetryAsync(2307, 1);
                _logger.LogToUser("Питание подано", LogLevel.Debug);

                // Открытие инструкции, если ещё не открыта
                if (!IsPdfInstructionAlreadyOpen(tempPdfPath))
                {
                    using (Stream resource = Assembly.GetExecutingAssembly()
                        .GetManifestResourceStream("RTL.Resources.Instructions.instructionForSw.pdf"))
                    {
                        if (resource != null)
                        {
                            using (FileStream file = new FileStream(tempPdfPath, FileMode.Create, FileAccess.Write))
                            {
                                await resource.CopyToAsync(file);
                            }

                            Process.Start(new ProcessStartInfo
                            {
                                FileName = tempPdfPath,
                                UseShellExecute = true
                            });

                            _logger.LogToUser("Инструкция открыта.", LogLevel.Info);
                        }
                        else
                        {
                            _logger.LogToUser("Встроенный PDF не найден.", LogLevel.Error);
                        }
                    }
                }
                else
                {
                    _logger.LogToUser("Инструкция уже открыта. Повторный запуск не требуется.", LogLevel.Info);
                }

                // Запуск программы прошивки
                var flashProcess = Process.Start(new ProcessStartInfo
                {
                    FileName = exePath,
                    UseShellExecute = true
                });

                if (flashProcess != null)
                {
                    _logger.LogToUser("Программа прошивки запущена. Ожидаю завершения...", LogLevel.Info);

                    // Асинхронно ждём завершения процесса
                    await Task.Run(() => flashProcess.WaitForExit());

                    _logger.LogToUser("Программа прошивки завершена.", LogLevel.Info);
                }
                else
                {
                    _logger.LogToUser("Не удалось запустить программу прошивки.", LogLevel.Warning);
                }

                // Снятие питания
                await WriteToRegisterWithRetryAsync(2301, 0);
                await WriteToRegisterWithRetryAsync(2307, 0);
                _logger.LogToUser("Питание снято", LogLevel.Debug);
                _isFirstFlashProgramming = false;
            }
            catch (Exception ex)
            {
                _logger.LogToUser($"Ошибка при запуске: {ex.Message}", LogLevel.Error);
            }
        }
        private async Task OpenSwdProgramAsync()
        {
            try
            {
                string flashToolPath = Properties.Settings.Default.SwdProgramPath;
                string firmwarePath = Properties.Settings.Default.SwdFirmwarePath;
                string workingDirectory = Path.GetDirectoryName(flashToolPath);

                if (!File.Exists(flashToolPath))
                {
                    _logger.LogToUser($"Скрипт прошивки не найден: {flashToolPath}", LogLevel.Error);
                    return;
                }

                if (string.IsNullOrWhiteSpace(firmwarePath) || !File.Exists(firmwarePath))
                {
                    _logger.LogToUser($"Файл прошивки не найден: {firmwarePath}", LogLevel.Error);
                    return;
                }

                _logger.LogToUser("Подача питания перед прошивкой...", LogLevel.Info);
                await WriteToRegisterWithRetryAsync(2301, 1);
                await Task.Delay(1000);

                string formattedFirmwarePath = $"\"{firmwarePath.Replace("\\", "/")}\"";

                var psi = new ProcessStartInfo
                {
                    FileName = flashToolPath,
                    Arguments = formattedFirmwarePath,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    WorkingDirectory = workingDirectory
                };

                _logger.LogToUser($"Запуск прошивки с аргументом: {formattedFirmwarePath}", LogLevel.Info);

                int exitCode = -1;

                using (var process = new Process { StartInfo = psi })
                {
                    process.OutputDataReceived += (s, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                            _logger.Log(e.Data, LogLevel.Debug);
                    };

                    process.ErrorDataReceived += (s, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                            _logger.Log(e.Data, LogLevel.Error);
                    };

                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    var timeoutTask = Task.Delay(TimeSpan.FromSeconds(20));
                    var waitForExitTask = process.WaitForExitAsync();

                    var completedTask = await Task.WhenAny(waitForExitTask, timeoutTask);

                    if (completedTask == timeoutTask)
                    {
                        try
                        {
                            _logger.LogToUser("Время ожидания прошивки истекло. Возможна проблема с программатором.", LogLevel.Warning);
                            if (!process.HasExited)
                                process.Kill();
                        }
                        catch (Exception ex)
                        {
                            _logger.LogToUser($"Ошибка при попытке завершить процесс: {ex.Message}", LogLevel.Error);
                        }

                        return;
                    }

                    exitCode = process.ExitCode;
                }

                if (exitCode != 0)
                {
                    _logger.LogToUser($"Ошибка прошивки! Код выхода: {exitCode}", LogLevel.Error);

                    if (exitCode == 1)
                        _logger.LogToUser("Возможно, устройство не подключено или не найдено.", LogLevel.Warning);
                    else if (exitCode == 2)
                        _logger.LogToUser("Ошибка доступа к HEX-файлу или неверный путь.", LogLevel.Warning);
                }
                else
                {
                    _logger.LogToUser("Прошивка успешно завершена.", LogLevel.Success);
                }

                await Task.Delay(500);
                await WriteToRegisterWithRetryAsync(2301, 0);
                _logger.LogToUser("Питание снято", LogLevel.Info);
            }
            catch (Exception ex)
            {
                _logger.LogToUser($"❌ Исключение при прошивке: {ex.Message}", LogLevel.Error);
            }
        }
        private bool IsPdfInstructionAlreadyOpen(string pdfPath)
        {
            var processes = Process.GetProcesses();

            return processes.Any(p =>
            {
                try
                {
                    return !string.IsNullOrEmpty(p.MainWindowTitle) &&
                           p.MainWindowTitle.Contains("Прошивка") &&
                           !p.HasExited;
                }
                catch
                {
                    return false;
                }
            });
        }
       
        private async Task<bool> TryReconnectModbusAsync()
        {
            for (int attempt = 1; attempt <= 3; attempt++)
            {
                try
                {
                    _logger.LogToUser($"Попытка {attempt} переподключения к Modbus...", LogLevel.Warning);

                    DisconnectModbus(); // Закрываем текущее соединение
                    await Task.Delay(2000); // Даем устройству время перед новым подключением

                    if (await TryInitializeModbusAsync())
                    {
                        _logger.LogToUser("Подключение к Modbus восстановлено.", LogLevel.Success);
                        return true;
                    }

                    _logger.LogToUser($"Попытка {attempt} не удалась.", LogLevel.Warning);
                }
                catch (Exception ex)
                {
                    _logger.LogToUser($"Ошибка при переподключении (попытка {attempt}): {ex.Message}", LogLevel.Error);
                }

                await Task.Delay(5000); // Интервал между попытками
            }

            _logger.LogToUser("Все попытки переподключения к Modbus не удались.", LogLevel.Error);
            return false;
        }
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


        #region  GUI логика

        #region тумблеры модбас


        public bool IsV52Enabled
        {
            get => StandRegisters.V52Out == 1;
            set
            {
                if (IsTestRunning) return; // Если тест выполняется, игнорируем изменение

                if (StandRegisters.V52Out != (ushort)(value ? 1 : 0))
                {
                    _logger.Log($"Изменение IsV52Enabled: {value}", LogLevel.Debug);

                    StandRegisters.V52Out = (ushort)(value ? 1 : 0);
                    OnPropertyChanged();

                    // Логируем попытку записи в Modbus
                    _logger.Log($"Запись в Modbus 2301: {StandRegisters.V52Out}", LogLevel.Debug);

                    _ = WriteToRegisterWithRetryAsync(2301, StandRegisters.V52Out);
                }
            }
        }



        public bool IsV55Enabled
        {
            get => StandRegisters.V55Out == 1;
            set
            {
                if (IsTestRunning) return; // Если тест выполняется, игнорируем изменение
                if (StandRegisters.V55Out != (ushort)(value ? 1 : 0))
                {
                    _logger.Log($"Изменение IsV55Enabled: {value}", LogLevel.Debug);

                    StandRegisters.V55Out = (ushort)(value ? 1 : 0);
                    OnPropertyChanged();

                    // Логируем попытку записи в Modbus
                    _logger.Log($"Запись в Modbus 2302: {StandRegisters.V55Out}", LogLevel.Debug);

                    _ = WriteToRegisterWithRetryAsync(2302, StandRegisters.V55Out);
                }
            }
        }




        public bool IsResetEnabled
        {
            get => StandRegisters.ResetOut == 1;
            set
            {
                if (IsTestRunning) return; // Если тест выполняется, игнорируем изменение
                if (StandRegisters.ResetOut != (ushort)(value ? 1 : 0))
                {
                    _logger.Log($"Изменение IsV55Enabled: {value}", LogLevel.Debug);

                    StandRegisters.ResetOut = (ushort)(value ? 1 : 0);
                    OnPropertyChanged();

                    // Логируем попытку записи в Modbus
                    _logger.Log($"Запись в Modbus 2307: {StandRegisters.ResetOut}", LogLevel.Debug);

                    _ = WriteToRegisterWithRetryAsync(2307, StandRegisters.ResetOut);
                }
            }
        }

        #endregion тумблеры модбас
        #region раскраски

        private ushort _k5testStatus = 1;
        public ushort K5TestStatus
        {
            get => _k5testStatus;
            set => SetAndNotify(ref _k5testStatus, value);
        }

        private ushort _rs485Status = 1;
        public ushort Rs485Status
        {
            get => _rs485Status;
            set => SetAndNotify(ref _rs485Status, value);
        }

        private ushort _sensor1Status = 1;
        public ushort Sensor1Status
        {
            get => _sensor1Status;
            set => SetAndNotify(ref _sensor1Status, value);
        }

        private ushort _sensor2Status = 1;
        public ushort Sensor2Status
        {
            get => _sensor2Status;
            set => SetAndNotify(ref _sensor2Status, value);
        }

        private ushort _relayStatus = 1;
        public ushort RelayStatus
        {
            get => _relayStatus;
            set => SetAndNotify(ref _relayStatus, value);
        }

        private ushort _poeStatus = 1;
        public ushort PoeStatus
        {
            get => _poeStatus;
            set => SetAndNotify(ref _poeStatus, value);
        }
        private ushort _tamperStatus = 1;
        public ushort TamperStatus
        {
            get => _tamperStatus;
            set => SetAndNotify(ref _tamperStatus, value);
        }

        private ushort _consoleStatus = 1;
        public ushort ConsoleStatus
        {
            get => _consoleStatus;
            set => SetAndNotify(ref _consoleStatus, value);
        }

        private ushort _i2cStatus = 1;
        public ushort I2CStatus
        {
            get => _i2cStatus;
            set => SetAndNotify(ref _i2cStatus, value);
        }


        private ushort _v11Status = 1;
        public ushort V11Status
        {
            get => _v11Status;
            set => SetAndNotify(ref _v11Status, value);
        }
        private ushort _v15Status = 1;
        public ushort V15Status
        {
            get => _v15Status;
            set => SetAndNotify(ref _v15Status, value);
        }

        private ushort _v33Status = 1;
        public ushort V33Status
        {
            get => _v33Status;
            set => SetAndNotify(ref _v33Status, value);
        }
        private ushort _crStatus = 1;
        public ushort CrStatus
        {
            get => _crStatus;
            set => SetAndNotify(ref _crStatus, value);
        }
        private ushort _crCpuStatus = 1;
        public ushort CrCpuStatus
        {
            get => _crCpuStatus;
            set => SetAndNotify(ref _crCpuStatus, value);
        }


        private ushort _flashStatus = 1;
        public ushort FlashStatus
        {
            get => _flashStatus;
            set => SetAndNotify(ref _flashStatus, value);
        }
        private ushort _rtlStatus = 1;
        public ushort RtlStatus
        {
            get => _rtlStatus;
            set => SetAndNotify(ref _rtlStatus, value);
        }



        #endregion раскраски

        private int _progressValue;
        public int ProgressValue
        {
            get => _progressValue;
            set => SetAndNotify(ref _progressValue, value);
        }

        #endregion GUI логика
        #region отключения
        private async Task StopHard()
        {
            isSwTestFull = false;
            isSwTestSuccess = false;

            _logger.LogToUser("Прерывание тестирования...", Loggers.LogLevel.Warning);
            await WriteToRegisterWithRetryAsync(2301, 0);
            await WriteToRegisterWithRetryAsync(2302, 0);
            await WriteToRegisterWithRetryAsync(2307, 0);

            await Task.Delay(500); // Даем время на обработку
            IsTestRunning = false;


            // Отключаем питание платы


            _logger.LogToUser("Питание снято. Плату можно безопасно извлечь из стенда.", Loggers.LogLevel.Info);
        }


        private async Task LoadSwReport()
        {
            if (TestConfig.IsReportGenerationEnabled)
            {
                try
                {
                    _logger.LogToUser("Подготовка к отправке результатов на сервер...", LogLevel.Info);

                    ServerTestResult.isSuccess = isSwTestSuccess;
                    ServerTestResult.isFull = isSwTestFull;

                    _logger.Log($"isSuccess={ServerTestResult.isSuccess}, isFull={ServerTestResult.isFull}", LogLevel.Debug);

                    _logger.LogToUser("Отправка отчёта на сервер (первая попытка)...", LogLevel.Info);

                    DeviceInfo di = Service.SendTestResult(ServerTestResult, SessionId, true);

                    if (di == null)
                    {
                        _logger.LogToUser("Первая попытка передачи результатов не удалась. Повторная попытка...", LogLevel.Warning);

                        di = Service.SendTestResult(ServerTestResult, SessionId, true);

                        if (di == null)
                        {
                            _logger.LogToUser("Ошибка: не удалось отправить результаты на сервер после двух попыток.", LogLevel.Error);
                            throw new Exception("Ошибка передачи результатов тестирования на сервер.");
                        }
                        else
                        {
                            _logger.LogToUser("Повторная попытка успешна. Данные успешно отправлены.", LogLevel.Success);
                        }
                    }
                    else
                    {
                        _logger.LogToUser("Данные успешно отправлены с первой попытки.", LogLevel.Success);
                    }

                    ServerTestResult.deviceSerial = di.serialNumber;
                    _logger.LogToUser($"Серийный номер устройства, полученный от сервера: {di.serialNumber}", LogLevel.Info);
                    _logger.Log( $"DeviceInfo: serialNumber={di.serialNumber}, hw_version={di.hw_version}, identifier={di.identifier}", LogLevel.Debug);
                }
                catch (Exception ex)
                {
                    _logger.LogToUser($"Ошибка при отправке отчёта: {ex.Message}", LogLevel.Error);
                    _logger.Log($"StackTrace: {ex.StackTrace}", LogLevel.Debug);
                }
            }
            else
            {
                _logger.LogToUser("Генерация и отправка отчета отключены в настройках профиля.", LogLevel.Warning);
            }
        }

        private async Task DisconnectStand()
        {
            await StopHard();
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