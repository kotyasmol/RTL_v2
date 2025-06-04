using Stylet;
using System.Windows.Input;
using RTL.Logger;
using RTL.Services;
using System.Threading.Tasks;
using System.Windows;
using System.Collections.ObjectModel;
using RTL.Commands;
using StyletIoC;
using System.Collections.Specialized;
using System.Windows.Threading;
using System.Windows.Controls;
using System.ComponentModel;
using RTL.Models;
using System.IO;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Net.Http;
using FTServiceUtils;
using FTServiceUtils.Enums;
using System.Configuration;
using System.Text;
using System.Threading.Channels;
namespace RTL.ViewModels
{
    public class RtlPoeViewModel : Screen
    {
        public event PropertyChangedEventHandler PropertyChanged;
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

        private readonly ModbusService _modbusService;
        public PoeRegistersModel PoeRegisters { get; } = new PoeRegistersModel();
        private CancellationTokenSource _monitoringCancellationTokenSource;

        public ICommand ConnectCommand { get; }
        public ICommand OpenFlashProgramCommand { get; }
        public ICommand OpenInstructionCommand { get; }

        private bool _isStandConnected;
        public bool IsStandConnected
        {
            get => _isStandConnected;
            set
            {
                if (SetAndNotify(ref _isStandConnected, value))
                {
                    (OpenFlashProgramCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
                    (OpenInstructionCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
                }
            }

        }
        private PoeTestProfileModel _testConfig;
        public PoeTestProfileModel TestConfig
        {
            get => _testConfig;
            set => SetAndNotify(ref _testConfig, value);
        }

        private bool _isTestProfileLoaded;
        public bool IsTestProfileLoaded
        {
            get => _isTestProfileLoaded;
            set => SetAndNotify(ref _isTestProfileLoaded, value);
        }

        private bool _isPoeTestRunning = false;
        private bool _canStartTest = true;
        private CancellationTokenSource _testCts;
        private bool CanExecuteFlashCommands() => IsStandConnected;
        private bool _isServerConnected;

        public bool IsServerConnected
        {
            get => _isServerConnected;
            set => SetAndNotify(ref _isServerConnected, value);
        }

        public string SessionId;  //id сессии, без него не отправишь рез-ты
        public static TestResult ServerPoeTestResult;
        private bool _isFirstFlashProgramming; // флаг для показа инструкции перед прошивкой 

        private readonly IFlashProgrammerService _flashProgrammerService;
        private readonly IMcuProgrammerService _mcuProgrammerService;
        private readonly ITestTimeoutService _timeoutService;


        public RtlPoeViewModel([Inject(Key = "POE")] Loggers logger,IFlashProgrammerService flashProgrammerService, IMcuProgrammerService mcuProgrammerService, ITestTimeoutService timeoutService)
        {
            _logger = logger;
            _flashProgrammerService = flashProgrammerService;
            _mcuProgrammerService = mcuProgrammerService;
            _timeoutService = timeoutService;
            _modbusService = new ModbusService(_logger, () => RTL.Properties.Settings.Default.ComPoe);
            ConnectCommand = new RelayCommand(async () => await ToggleConnectionAsync());
            OpenFlashProgramCommand = new AsyncRelayCommand(OpenFlashProgramAsync, () => IsStandConnected);
            OpenInstructionCommand = new AsyncRelayCommand(OpenInstructionAsync, () => IsStandConnected);

            _isFirstFlashProgramming = true;
            
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

        private async Task ToggleConnectionAsync()
        {
            if (IsStandConnected)
            {
                _modbusService.Disconnect();
                IsStandConnected = false;
                _logger.LogToUser("Отключено от стенда POE.", Loggers.LogLevel.Warning);
            }
            else
            {
                bool result = await _modbusService.ConnectAsync();
                if (result)
                {
                    // профиль перед стартом мониторинга
                    string profilePath = Properties.Settings.Default.RtlPoeProfilePath;
                    if (!File.Exists(profilePath))
                    {
                        _logger.LogToUser($"Файл профиля тестирования не найден: {profilePath}", Loggers.LogLevel.Error);
                        return;
                    }

                    bool profileLoaded = await TryLoadTestProfileAsync();
                    if (!profileLoaded)
                    {
                        _logger.LogToUser("Подключение отменено: не удалось загрузить профиль тестирования.", Loggers.LogLevel.Error);
                        return;
                    }

                    // Подключение к серверу — только если отчёты включены
                    if (TestConfig.IsReportRequired)
                    {
                        bool serverConnected = await TryConnectToServerAsync();
                        if (!serverConnected)
                        {
                            _logger.LogToUser("Подключение к серверу не удалось. Отправка отчётов обязательна. Работа прервана.", Loggers.LogLevel.Error);
                            return; // прерываем подключение — дальше не продолжаем
                        }
                    }
                    else
                    {
                        _logger.LogToUser("Отправка отчётов отключена в профиле. Подключение к серверу пропущено.", Loggers.LogLevel.Info);
                    }


                    IsStandConnected = true;

                    _ = MonitorPoeAsync(); // мониторинг без ожидания
                    _logger.LogToUser($"Успешное подключение к стенду POE. Серийный номер стенда: {PoeRegisters.StandSerialNumber}", Loggers.LogLevel.Success);
                }
                else
                {
                    _logger.LogToUser("Ошибка подключения к стенду POE.", Loggers.LogLevel.Error);
                }
            }
        }

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

                        _logger.LogToUser($"Попытка {attempt}/{maxRetries}: подключение к серверу...", Loggers.LogLevel.Info);

                        HttpResponseMessage response = await client.GetAsync(url);
                        string responseContent = await response.Content.ReadAsStringAsync();

                        if (response.IsSuccessStatusCode && !responseContent.Contains("error"))
                        {
                            _logger.LogToUser($"Подключение к серверу успешно. Код: {response.StatusCode}", Loggers.LogLevel.Success);
                            SessionId = GetSessionId("alex", "alex", 5); 
                            //SessionId = App.StartupSessionId; ---- только таким образом получаем в итоговом файле id сессии

                            _logger.LogToUser($"Получен SessionId: {SessionId}", Loggers.LogLevel.Debug);

                            IsServerConnected = true;
                            return true;
                        }
                        else
                        {
                            _logger.LogToUser($"Ошибка подключения. Код: {response.StatusCode}, Ответ: {responseContent}", Loggers.LogLevel.Warning);
                        }
                    }
                }
                catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
                {
                    _logger.LogToUser($"Тайм-аут при подключении. Сервер не отвечает. Попробуйте позже.", Loggers.LogLevel.Error);
                }
                catch (HttpRequestException ex)
                {
                    _logger.LogToUser($"Ошибка сети: {ex.Message}. Проверьте интернет-соединение.", Loggers.LogLevel.Error);
                }
                catch (Exception ex)
                {
                    _logger.LogToUser($"Ошибка подключения к серверу: {ex.Message}", Loggers.LogLevel.Error);
                }

                if (attempt < maxRetries)
                {
                    _logger.LogToUser($"Повторная попытка через {delayMs / 1000} секунд...", Loggers.LogLevel.Info);
                    await Task.Delay(delayMs);
                }
            }

            _logger.LogToUser($"Все попытки подключения исчерпаны. Проверьте настройки сети.", Loggers.LogLevel.Error);
            IsServerConnected = false;
            return false;
        }

        private string GetSessionId(string login, string password, int timezone) // заглушка временная
        {
            string url = $"http://iccid.fort-telecom.ru/api/Api.svc/connect?login={login}&password={password}&timezone={timezone}";

            try
            {
                using (HttpClient client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) })
                {
                    client.DefaultRequestHeaders.Add("User-Agent", "ftstand");

                    HttpResponseMessage response = client.GetAsync(url).Result;
                    string responseContent = response.Content.ReadAsStringAsync().Result;

                    _logger.Log($"Ответ сервера (sessionId): {responseContent}", Loggers.LogLevel.Debug);

                    if (!response.IsSuccessStatusCode)
                    {
                        _logger.Log($"Ошибка получения sessionId. Код: {response.StatusCode}, Ответ: {responseContent}", Loggers.LogLevel.Warning);
                        return null;
                    }

                    // Убираем кавычки, если сервер вернул строку
                    string sessionId = responseContent.Trim('"');

                    if (!string.IsNullOrEmpty(sessionId))
                    {
                        _logger.Log($"Успешно получен sessionId: {sessionId}", Loggers.LogLevel.Success);
                        return sessionId;
                    }
                    else
                    {
                        _logger.Log($"Сервер не вернул корректный sessionId: {responseContent}", Loggers.LogLevel.Warning);
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.Log($"Ошибка сети при получении sessionId: {ex.Message}", Loggers.LogLevel.Error);
            }
            catch (Exception ex)
            {
                _logger.Log($"Ошибка при получении sessionId: {ex.Message}", Loggers.LogLevel.Error);
            }

            return null;
        }

        private async Task<bool> TryLoadTestProfileAsync()
        {
            string testProfilePath = Properties.Settings.Default.RtlPoeProfilePath;

            try
            {
                if (!File.Exists(testProfilePath))
                {
                    TestConfig = new PoeTestProfileModel(); // создаём заглушку, чтобы не было null
                    IsTestProfileLoaded = false;
                    _logger.LogToUser($"Файл профиля тестирования не найден: {testProfilePath}", Loggers.LogLevel.Warning);
                    return false;
                }

                string json = await File.ReadAllTextAsync(testProfilePath);
                var parsedProfile = JsonConvert.DeserializeObject<PoeTestProfileModel>(json);

                if (parsedProfile == null)
                {
                    TestConfig = new PoeTestProfileModel();
                    IsTestProfileLoaded = false;
                    _logger.LogToUser($"Ошибка при чтении профиля: десериализация вернула null.", Loggers.LogLevel.Error);
                    return false;
                }

                // Проверка имени модели
                if (!string.Equals(parsedProfile.ModelName, "RTL_POE", StringComparison.Ordinal))
                {
                    TestConfig = new PoeTestProfileModel();
                    IsTestProfileLoaded = false;
                    _logger.LogToUser($"Загружен профиль не для RTL_POE: найдено '{parsedProfile.ModelName}'. Пожалуйста, выберите корректный профиль.", Loggers.LogLevel.Error);
                    return false;
                }

                TestConfig = parsedProfile;
                IsTestProfileLoaded = true;
                _logger.LogToUser($"Профиль тестирования успешно загружен: {testProfilePath}", Loggers.LogLevel.Success);
                return true;
            }
            catch (JsonException ex)
            {
                TestConfig = new PoeTestProfileModel();
                IsTestProfileLoaded = false;
                _logger.LogToUser($"Ошибка при разборе JSON профиля: {ex.Message}", Loggers.LogLevel.Error);
                return false;
            }
            catch (Exception ex)
            {
                TestConfig = new PoeTestProfileModel();
                IsTestProfileLoaded = false;
                _logger.LogToUser($"Общая ошибка при загрузке профиля: {ex.Message}", Loggers.LogLevel.Error);
                return false;
            }
        }

        private async Task MonitorPoeAsync()
        {
            while (IsStandConnected)
            {
                try
                {
                    var registers = await _modbusService.ReadRegistersAsync(2400, 111);

                    if (registers == null)
                    {
                        _logger.LogToUser("Ошибка при чтении регистров. Остановка мониторинга.", Loggers.LogLevel.Error);
                        IsStandConnected = false;
                        return;
                    }

                    PoeRegisters.StandSerialNumber = registers[0]; // серийный номер
                    PoeRegisters.RunButton = registers[1]; //  запуск теста

                    if (PoeRegisters.RunButton == 1)
                    {
                        if (!_isPoeTestRunning && _canStartTest)
                        {
                            _logger.LogToUser("Тумблер RUN включен. Запуск теста...", Loggers.LogLevel.Info);
                            _canStartTest = false; // блокируем повторный запуск до сброса тумблера
                            _testCts = new CancellationTokenSource();
                            _ = StartPoeTestAsync(_testCts.Token); // запускаем без ожидания
                        }
                    }
                    else // RunButton == 0
                    {
                        // Попытка закрыть Xgpro, если он запущен
                        var xgpro = Process.GetProcessesByName("Xgpro").FirstOrDefault();
                        if (xgpro != null && !xgpro.HasExited)
                        {
                            try
                            {
                                xgpro.Kill(); // Мгновенно завершает процесс
                                _logger.LogToUser("Программа Xgpro была закрыта после выключения тумблера RUN.", Loggers.LogLevel.Info);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogToUser($"Не удалось завершить Xgpro: {ex.Message}", Loggers.LogLevel.Error);
                            }
                        }

                        if (_isPoeTestRunning)
                        {
                            _logger.LogToUser("Тумблер RUN выключен. Прерывание теста.", Loggers.LogLevel.Warning);
                            //await StopHard(); ---- возможно не нужно убирать. дублировались логи и поэтому закомментировано
                            _testCts?.Cancel();
                        }
                        else
                        {
                            _canStartTest = true; // пользователь сбросил тумблер — разрешить новый запуск
                        }
                    }


                    PoeRegisters.NextButton = registers[2];
                    PoeRegisters.Enable52V = registers[3]; // подача питания
                    PoeRegisters.Voltage3V3Meas = registers[4]; // смотрим на плашку

                    PoeRegisters.PowerGood1AChannel1 = registers[5]; 
                    PoeRegisters.PowerGood1BChannel2 = registers[6];
                    PoeRegisters.PowerGood2AChannel3 = registers[7];
                    PoeRegisters.PowerGood2BChannel4 = registers[8];
                    PoeRegisters.PowerGood3AChannel5 = registers[9];
                    PoeRegisters.PowerGood3BChannel6 = registers[10];
                    PoeRegisters.PowerGood4AChannel7 = registers[11];
                    PoeRegisters.PowerGood4BChannel8 = registers[12];
                    PoeRegisters.PowerGood5AChannel9 = registers[13];
                    PoeRegisters.PowerGood5BChannel10 = registers[14];
                    PoeRegisters.PowerGood6AChannel11 = registers[15];
                    PoeRegisters.PowerGood6BChannel12 = registers[16];
                    PoeRegisters.PowerGood7AChannel13 = registers[17];
                    PoeRegisters.PowerGood7BChannel14 = registers[18];
                    PoeRegisters.PowerGood8AChannel15 = registers[19];
                    PoeRegisters.PowerGood8BChannel16 = registers[20];

                    PoeRegisters.White1AChannel1 = registers[21]; //белый 
                    PoeRegisters.White1BChannel2 = registers[22];
                    PoeRegisters.White2AChannel3 = registers[23];
                    PoeRegisters.White2BChannel4 = registers[24];
                    PoeRegisters.White3AChannel5 = registers[25];
                    PoeRegisters.White3BChannel6 = registers[26];
                    PoeRegisters.White4AChannel7 = registers[27];
                    PoeRegisters.White4BChannel8 = registers[28];
                    PoeRegisters.White5AChannel9 = registers[29];
                    PoeRegisters.White5BChannel10 = registers[30];
                    PoeRegisters.White6AChannel11 = registers[31];
                    PoeRegisters.White6BChannel12 = registers[32];
                    PoeRegisters.White7AChannel13 = registers[33];
                    PoeRegisters.White7BChannel14 = registers[34];
                    PoeRegisters.White8AChannel15 = registers[35];
                    PoeRegisters.White8BChannel16 = registers[36];

                    PoeRegisters.Red1AChannel1 = registers[37]; // красный
                    PoeRegisters.Red1BChannel2 = registers[38];
                    PoeRegisters.Red2AChannel3 = registers[39];
                    PoeRegisters.Red2BChannel4 = registers[40];
                    PoeRegisters.Red3AChannel5 = registers[41];
                    PoeRegisters.Red3BChannel6 = registers[42];
                    PoeRegisters.Red4AChannel7 = registers[43];
                    PoeRegisters.Red4BChannel8 = registers[44];
                    PoeRegisters.Red5AChannel9 = registers[45];
                    PoeRegisters.Red5BChannel10 = registers[46];
                    PoeRegisters.Red6AChannel11 = registers[47];
                    PoeRegisters.Red6BChannel12 = registers[48];
                    PoeRegisters.Red7AChannel13 = registers[49];
                    PoeRegisters.Red7BChannel14 = registers[50];
                    PoeRegisters.Red8AChannel15 = registers[51];
                    PoeRegisters.Red8BChannel16 = registers[52];

                    PoeRegisters.Green1AChannel1 = registers[53]; // зеленый
                    PoeRegisters.Green1BChannel2 = registers[54];
                    PoeRegisters.Green2AChannel3 = registers[55];
                    PoeRegisters.Green2BChannel4 = registers[56];
                    PoeRegisters.Green3AChannel5 = registers[57];
                    PoeRegisters.Green3BChannel6 = registers[58];
                    PoeRegisters.Green4AChannel7 = registers[59];
                    PoeRegisters.Green4BChannel8 = registers[60];
                    PoeRegisters.Green5AChannel9 = registers[61];
                    PoeRegisters.Green5BChannel10 = registers[62];
                    PoeRegisters.Green6AChannel11 = registers[63];
                    PoeRegisters.Green6BChannel12 = registers[64];
                    PoeRegisters.Green7AChannel13 = registers[65];
                    PoeRegisters.Green7BChannel14 = registers[66];
                    PoeRegisters.Green8AChannel15 = registers[67];
                    PoeRegisters.Green8BChannel16 = registers[68];

                    PoeRegisters.Blue1AChannel1 = registers[69]; // голубой 
                    PoeRegisters.Blue1BChannel2 = registers[70];
                    PoeRegisters.Blue2AChannel3 = registers[71];
                    PoeRegisters.Blue2BChannel4 = registers[72];
                    PoeRegisters.Blue3AChannel5 = registers[73];
                    PoeRegisters.Blue3BChannel6 = registers[74];
                    PoeRegisters.Blue4AChannel7 = registers[75];
                    PoeRegisters.Blue4BChannel8 = registers[76];
                    PoeRegisters.Blue5AChannel9 = registers[77];
                    PoeRegisters.Blue5BChannel10 = registers[78];
                    PoeRegisters.Blue6AChannel11 = registers[79];
                    PoeRegisters.Blue6BChannel12 = registers[80];
                    PoeRegisters.Blue7AChannel13 = registers[81];
                    PoeRegisters.Blue7BChannel14 = registers[82];
                    PoeRegisters.Blue8AChannel15 = registers[83];
                    PoeRegisters.Blue8BChannel16 = registers[84];

                    PoeRegisters.PoeBank = registers[85];
                    PoeRegisters.PoeId = registers[86];
                    PoeRegisters.PoePortEn = registers[87];
                    PoeRegisters.PoeInt = registers[88];
                    PoeRegisters.PoeReset = registers[89]; // reset
                    PoeRegisters.PoeMode = registers[90];
                    PoeRegisters.Voltage3V3MeasCalibr = registers[91];
                    PoeRegisters.Voltage3V3Enable = registers[92]; // подача 3.3
                    PoeRegisters.UartTestStart = registers[93]; // 1 = start
                    PoeRegisters.UartTestResult = registers[94]; // 0 - stop. 1 - running. 2 - ok. 3 - fail.

                    PoeRegisters.UartCh1Voltage = registers[95]; // uart voltage
                    PoeRegisters.UartCh2Voltage = registers[96];
                    PoeRegisters.UartCh3Voltage = registers[97];
                    PoeRegisters.UartCh4Voltage = registers[98];
                    PoeRegisters.UartCh5Voltage = registers[99];
                    PoeRegisters.UartCh6Voltage = registers[100];
                    PoeRegisters.UartCh7Voltage = registers[101];
                    PoeRegisters.UartCh8Voltage = registers[102];
                    PoeRegisters.UartCh9Voltage = registers[103];
                    PoeRegisters.UartCh10Voltage = registers[104];
                    PoeRegisters.UartCh11Voltage = registers[105];
                    PoeRegisters.UartCh12Voltage = registers[106];
                    PoeRegisters.UartCh13Voltage = registers[107];
                    PoeRegisters.UartCh14Voltage = registers[108];
                    PoeRegisters.UartCh15Voltage = registers[109];
                    PoeRegisters.UartCh16Voltage = registers[110];


                }
                catch (Exception ex)
                {
                    _logger.Log($"Ошибка при чтении регистров POE: {ex.Message}", Loggers.LogLevel.Error);
                }

                await Task.Delay(1000); 
            }
        }

        private async Task StartPoeTestAsync(CancellationToken token)
        {
            _isPoeTestRunning = true;

            try
            {
                ServerPoeTestResult = new TestResult
                {
                    deviceType = DeviceType.RTL_POE,
                    standName = Environment.MachineName, // заменить на серийный номер стенда
                    isSuccess = true,
                    deviceIdent = "413148580e5939514c53698b", // серийник платы
                    isFull = true,
                };
                //ServerPoeTestResult.deviceSerial = "31200001"; --- Эта ерунда всё ломает. не указывать вручную. 
                //ServerPoeTestResult.AddSubTest($"название теста ", true, $"результаты измерений и мин/макс допуски");


                _logger.LogToUser("Тестирование POE платы запущено.", Loggers.LogLevel.Info);


                await _modbusService.WriteSingleRegisterAsync(2492, 1);
                if (TestConfig?.FlashFirmwareAuto == true)
                {
                    _logger.LogToUser("Прошивка Flash: запуск...", Loggers.LogLevel.Info);
                    if (!await RunFlashProgrammingAsync(token)) return;
                    _logger.LogToUser("Прошивка Flash успешно завершена.", Loggers.LogLevel.Success);
                }
                else
                {
                    _logger.LogToUser("Прошивка Flash отключена в профиле тестирования.", Loggers.LogLevel.Warning);
                    ServerPoeTestResult.isFull = false;
                }



                // === Подтест 2 === MCU
                if (TestConfig?.McuFirmwareAuto == true)
                {
                    _logger.LogToUser("MCU прошивка: запуск...", Loggers.LogLevel.Info);
                    if (!await RunMcuProgrammingAsync(token)) return;
                    _logger.LogToUser("MCU прошивка: успешно завершена.", Loggers.LogLevel.Success);
                }
                else
                {
                    _logger.LogToUser("MCU Прошивка: пропущена (отключена в профиле).", Loggers.LogLevel.Warning);
                    ServerPoeTestResult.isFull = false;
                }

                // перезагрузка платы
                if (!await StartBoardPowerSequenceAsync(token))
                {
                    await StopHard();
                    return;
                }

                // Проверка напряжения 3.3В ===
                if (TestConfig?.Is3v3TestRequired == true)
                {
                    _logger.LogToUser("Проверка напряжения 3.3В...", Loggers.LogLevel.Info);
                    if (!await _timeoutService.RunWithTimeoutAsync(RunCheck3V3VoltageTestAsync, "Проверка 3.3В", TimeSpan.FromSeconds(5), token))
                        return;
                    _logger.LogToUser("Проверка напряжения 3.3В успешно завершена.", Loggers.LogLevel.Success);
                }

                else
                {
                    _logger.LogToUser("Проверка напряжения 3.3В отключена в профиле тестирования.", Loggers.LogLevel.Warning);
                    ServerPoeTestResult.isFull = false;
                }

                // проверка версии платы
                if (TestConfig.IsBoardVersionCheckEnabled)
                {
                    _logger.LogToUser("Проверка версии платы: запуск...", Loggers.LogLevel.Info);
                    if (!await RunCheckBoardVersionAsync(token)) return;
                    _logger.LogToUser("Проверка версии платы успешно завершена.", Loggers.LogLevel.Success);
                }
                else
                {
                    _logger.LogToUser("Проверка версии платы пропущена (отключена в профиле).", Loggers.LogLevel.Warning);
                    ServerPoeTestResult.isFull = false;
                }

                // проверка пое на портах
                if (TestConfig.IsPoeTestRequired)
                {
                    _logger.LogToUser("Проверка подачи PoE: запуск...", Loggers.LogLevel.Info);
                    if (!await RunPoePowerDeliveryTestAsync(token)) return;
                    _logger.LogToUser("Проверка подачи PoE успешно завершена.", Loggers.LogLevel.Success);
                }
                else
                {
                    _logger.LogToUser("Проверка подачи PoE пропущена (отключена в профиле).", Loggers.LogLevel.Warning);
                    ServerPoeTestResult.isFull = false;
                }

                // проверка светодиодов
                if (TestConfig.IsLedTestRequired)
                {
                    _logger.LogToUser("Проверка светодиодов: запуск...", Loggers.LogLevel.Info);

                    if (!await RunLedTestAsync(token))
                        return; 

                    _logger.LogToUser("Проверка светодиодов успешно завершена.", Loggers.LogLevel.Success);
                }
                else
                {
                    _logger.LogToUser("Проверка светодиодов пропущена (отключена в профиле).", Loggers.LogLevel.Warning);
                    ServerPoeTestResult.isFull = false;
                }



                // uart тест
                if (TestConfig.IsUartTestRequired)
                {
                    _logger.LogToUser("Тестирование интерфейса UART: запуск...", Loggers.LogLevel.Info);
                    if (!await RunUartInterfaceTestAsync(token))
                    {
                        _logger.LogToUser("Тестирование интерфейса UART завершено с ошибкой.", Loggers.LogLevel.Error);
                        return;
                    }
                    _logger.LogToUser("Тестирование интерфейса UART успешно завершено.", Loggers.LogLevel.Success);
                }
                else
                {
                    _logger.LogToUser("Тестирование интерфейса UART пропущено (отключено в профиле).", Loggers.LogLevel.Warning);
                    ServerPoeTestResult.isFull = false;
                }

                

                _logger.LogToUser("Все активные подтесты завершены успешно.", Loggers.LogLevel.Success);
                await StopHard();
            }
            catch (TaskCanceledException)
            {
                _logger.LogToUser("Тест был отменён пользователем.", Loggers.LogLevel.Warning);
            }
            catch (Exception ex)
            {
                _logger.LogToUser($"Ошибка во время выполнения теста: {ex.Message}", Loggers.LogLevel.Error);
            }
            finally
            {
                _isPoeTestRunning = false;
            }
        }



        private async Task<bool> RunFlashProgrammingAsync(CancellationToken token)
        {
            try
            {
                token.ThrowIfCancellationRequested();

                if (!TestConfig.FlashFirmwareAuto)
                {
                    _logger.LogToUser("Прошивка Flash отключена в профиле тестирования.", Loggers.LogLevel.Warning);
                    ServerPoeTestResult.isFull = false;
                    return true;
                }

                _logger.LogToUser("Прошивка flash: запуск...", Loggers.LogLevel.Info);

                var context = new FlashProgrammingContext
                {
                    FlashProgramPath = Properties.Settings.Default.FlashProgramPath,
                    ProjectFilePath = TestConfig.FlashXgproPath,
                    InstructionPath = TestConfig.FlashInstructionPath,
                    AutoMode = TestConfig.FlashFirmwareAuto,
                    IsFirstRun = _isFirstFlashProgramming,
                    FlashDelaySeconds = TestConfig.FlashDelay
                };

                var success = await _flashProgrammerService.StartProgrammingAsync(context, token);

                if (success)
                {
                    _logger.LogToUser("Прошивка flash успешно завершена.", Loggers.LogLevel.Success);
                    _isFirstFlashProgramming = false;
                    return true;
                }

                _logger.LogToUser("Прошивка flash завершена с ошибкой.", Loggers.LogLevel.Error);

                await StopHard();
                return false;
            }
            catch (OperationCanceledException)
            {
                _logger.LogToUser("Прошивка микросхемы была отменена пользователем.", Loggers.LogLevel.Warning);

                await StopHard();
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogToUser($"Ошибка при выполнении прошивки: {ex.Message}", Loggers.LogLevel.Error);
                await StopHard();
                return false;
            }
        }
        private async Task<bool> RunMcuProgrammingAsync(CancellationToken token)
        {
            try
            {
                var batPath = TestConfig.McuBatPath;
                var binPath = TestConfig.McuBinPath;
                var readIdPath = TestConfig.McuReadIdBatPath;

                if (string.IsNullOrEmpty(batPath) || !File.Exists(batPath))
                {
                    _logger.LogToUser("Файл .bat для MCU не указан или не найден.", Loggers.LogLevel.Error);
                    return false;
                }

                if (string.IsNullOrEmpty(binPath) || !File.Exists(binPath))
                {
                    _logger.LogToUser("Файл .bin для MCU не указан или не найден.", Loggers.LogLevel.Error);
                    return false;
                }

                if (!await _mcuProgrammerService.FlashMcuAsync(batPath, binPath, token))
                {
                    ServerPoeTestResult.isFull = false;
                    return false;
                }

                // Получение MCU ID
                if (!string.IsNullOrEmpty(readIdPath) && File.Exists(readIdPath))
                {
                    var serial = await GetMcuSerialAsync(readIdPath, token);
                    if (!string.IsNullOrWhiteSpace(serial))
                    {
                        _logger.LogToUser($"MCU Serial: {serial}", Loggers.LogLevel.Info);
                        ServerPoeTestResult.deviceIdent = serial; 
                    }
                    else
                    {
                        _logger.LogToUser("Не удалось получить серийный номер MCU.", Loggers.LogLevel.Warning);
                    }
                }
                else
                {
                    _logger.LogToUser("Скрипт получения MCU ID не найден.", Loggers.LogLevel.Warning);
                }

                return true;
            }
            catch (OperationCanceledException)
            {
                _logger.LogToUser("Прошивка MCU была отменена пользователем.", Loggers.LogLevel.Warning);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogToUser($"Ошибка при прошивке MCU: {ex.Message}", Loggers.LogLevel.Error);
                return false;
            }
        }

        private async Task<string?> GetMcuSerialAsync(string batPath, CancellationToken token)
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c \"{batPath}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.GetEncoding(866),
                    StandardErrorEncoding = Encoding.GetEncoding(866),
                    WorkingDirectory = Path.GetDirectoryName(batPath) // важный момент!
                };

                using var process = new Process { StartInfo = startInfo };

                process.Start();

                string stdOutput = await process.StandardOutput.ReadToEndAsync();
                string stdError = await process.StandardError.ReadToEndAsync();

                await process.WaitForExitAsync(token);

                _logger.Log($"StdOutput:\n{stdOutput}", Loggers.LogLevel.Debug);
                _logger.Log($"StdError:\n{stdError}", Loggers.LogLevel.Debug);

                var combinedOutput = stdOutput + "\n" + stdError;

                var lines = combinedOutput.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                var idLine = lines.FirstOrDefault(line => line.All(c => Uri.IsHexDigit(c)));

                if (idLine == null)
                {
                    _logger.LogToUser("В выводе не найден серийный номер MCU.", Loggers.LogLevel.Warning);
                    return null;
                }

                return idLine.Trim();
            }
            catch (Exception ex)
            {
                _logger.LogToUser($"Ошибка при получении MCU ID: {ex.Message}", Loggers.LogLevel.Error);
                return null;
            }
        }

        private async Task<bool> StartBoardPowerSequenceAsync(CancellationToken token)
        {
            try
            {
                token.ThrowIfCancellationRequested();

                // 1. Подать основное питание
                bool powerOnResult = await _modbusService.WriteSingleRegisterAsync(2403, 1);
                if (!powerOnResult)
                {
                    _logger.LogToUser("Не удалось подать основное питание на плату.", Loggers.LogLevel.Error);
                    await StopHard();
                    return false;
                }
                _logger.Log("Питание (52В) подано на плату.", Loggers.LogLevel.Info);
                //await _modbusService.WriteSingleRegisterAsync(2492, 1);
                token.ThrowIfCancellationRequested();

                // 2. Перезагрузка платы: 2489 = 1 → подождать 2 сек → 2489 = 0
                bool rebootStartResult = await _modbusService.WriteSingleRegisterAsync(2489, 1);
                if (!rebootStartResult)
                {
                    _logger.LogToUser("Не удалось инициировать перезагрузку платы.", Loggers.LogLevel.Error);
                    await StopHard();
                    return false;
                }
                _logger.Log("Перезагрузка платы запущена.", Loggers.LogLevel.Info);

                await Task.Delay(TimeSpan.FromSeconds(2), token);

                token.ThrowIfCancellationRequested();

                bool rebootStopResult = await _modbusService.WriteSingleRegisterAsync(2489, 0);
                if (!rebootStopResult)
                {
                    _logger.LogToUser("Не удалось завершить перезагрузку платы.", Loggers.LogLevel.Error);
                    await StopHard();
                    return false;
                }

                token.ThrowIfCancellationRequested();

                // 3. Подождать время запуска платы из профиля
                int delaySeconds = TestConfig?.StartUpTime ?? 0;
                if (delaySeconds > 0)
                {
                    _logger.LogToUser($"Ожидание запуска платы: {delaySeconds} секунд.", Loggers.LogLevel.Info);
                    await Task.Delay(TimeSpan.FromSeconds(delaySeconds), token);
                }

                _logger.LogToUser("Плата готова к тестированию.", Loggers.LogLevel.Success);
                return true;
            }
            catch (OperationCanceledException)
            {
                _logger.LogToUser("Последовательность включения платы была отменена.", Loggers.LogLevel.Warning);
                await StopHard();
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogToUser($"Ошибка при запуске платы: {ex.Message}", Loggers.LogLevel.Error);
                await StopHard();
                return false;
            }
        }



        private async Task<bool> RunCheck3V3VoltageTestAsync(CancellationToken token)
        {
            try
            {
                token.ThrowIfCancellationRequested();
                bool isSuccess = PoeRegisters.Voltage3V3Meas >= TestConfig.V3v3Min && PoeRegisters.Voltage3V3Meas <= TestConfig.V3v3Max;
                string measuredInfo = $"Измерено: {PoeRegisters.Voltage3V3Meas} (допуск: {TestConfig.V3v3Min} – {TestConfig.V3v3Max})";

                if (!isSuccess)
                {
                    _logger.LogToUser($"Измеренное значение напряжения 3.3В: {PoeRegisters.Voltage3V3Meas} — вне допустимого диапазона ({TestConfig.V3v3Min} – {TestConfig.V3v3Max}).", Loggers.LogLevel.Error);
                    ServerPoeTestResult.AddSubTest("Проверка напряжения 3.3В", false, measuredInfo);
                    await StopHard();
                    return false;
                }

                _logger.LogToUser($"Измеренное значение напряжения 3.3В: {PoeRegisters.Voltage3V3Meas} — в пределах нормы.", Loggers.LogLevel.Success);
                ServerPoeTestResult.AddSubTest("Проверка напряжения 3.3В", true, measuredInfo);
                return true;
            }
            catch (OperationCanceledException)
            {
                _logger.LogToUser("Подтест 3.3В был отменён пользователем.", Loggers.LogLevel.Warning);
                ServerPoeTestResult.AddSubTest("Проверка напряжения 3.3В", false, "Операция отменена");
                await StopHard();
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogToUser($"Ошибка при выполнении проверки 3.3В: {ex.Message}", Loggers.LogLevel.Error);
                ServerPoeTestResult.AddSubTest("Проверка напряжения 3.3В", false, $"Ошибка: {ex.Message}");
                await StopHard();
                return false;
            }
        }

        private async Task<bool> RunCheckBoardVersionAsync(CancellationToken token)
        {
            try
            {
                token.ThrowIfCancellationRequested();

                ushort actualBoardVersion = PoeRegisters.PoeId;
                int expectedBoardVersion = TestConfig.BoardVersion;

                if (actualBoardVersion != expectedBoardVersion)
                {
                    _logger.LogToUser($"Аппаратная версия платы: {actualBoardVersion} — не соответствует ожидаемой ({expectedBoardVersion}).", Loggers.LogLevel.Error);
                    ServerPoeTestResult.AddSubTest($"Аппаратная версия платы", false, $"{actualBoardVersion} — не соответствует ожидаемой ({expectedBoardVersion})");
                    await StopHard();
                    return false;
                }

                _logger.LogToUser($"Аппаратная версия платы: {actualBoardVersion} — соответствует ожидаемой.", Loggers.LogLevel.Success);
                ServerPoeTestResult.AddSubTest($"Аппаратная версия платы", true, $"{actualBoardVersion}");
                return true;
            }
            catch (OperationCanceledException)
            {
                _logger.LogToUser("Проверка версии платы была отменена пользователем.", Loggers.LogLevel.Warning);
                ServerPoeTestResult.AddSubTest($"Аппаратная версия платы", false, $"проверка отменена вручную");
                await StopHard();
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogToUser($"Ошибка при проверке версии платы: {ex.Message}", Loggers.LogLevel.Error);
                ServerPoeTestResult.AddSubTest($"Аппаратная версия платы", false, $"Ошибка при проверке версии платы: {ex.Message}");
                await StopHard();
                return false;
            }
        }

        private async Task<bool> RunPoePowerDeliveryTestAsync(CancellationToken token)
        {
            try
            {
                var portsToTest = new (bool isEnabled, ushort aChannel, ushort bChannel, int portNum)[]
                {
            (TestConfig.IsPort1TestEnabled, PoeRegisters.PowerGood1AChannel1, PoeRegisters.PowerGood1BChannel2, 1),
            (TestConfig.IsPort2TestEnabled, PoeRegisters.PowerGood2AChannel3, PoeRegisters.PowerGood2BChannel4, 2),
            (TestConfig.IsPort3TestEnabled, PoeRegisters.PowerGood3AChannel5, PoeRegisters.PowerGood3BChannel6, 3),
            (TestConfig.IsPort4TestEnabled, PoeRegisters.PowerGood4AChannel7, PoeRegisters.PowerGood4BChannel8, 4),
            (TestConfig.IsPort5TestEnabled, PoeRegisters.PowerGood5AChannel9, PoeRegisters.PowerGood5BChannel10, 5),
            (TestConfig.IsPort6TestEnabled, PoeRegisters.PowerGood6AChannel11, PoeRegisters.PowerGood6BChannel12, 6),
            (TestConfig.IsPort7TestEnabled, PoeRegisters.PowerGood7AChannel13, PoeRegisters.PowerGood7BChannel14, 7),
            (TestConfig.IsPort8TestEnabled, PoeRegisters.PowerGood8AChannel15, PoeRegisters.PowerGood8BChannel16, 8),
                };

                foreach (var (isEnabled, aReg, bReg, port) in portsToTest)
                {
                    token.ThrowIfCancellationRequested();
                    await Task.Delay(4000, token); 
                    if (!isEnabled)
                    {
                        _logger.LogToUser($"PoE-тест порта {port} пропущен (отключён в профиле).", Loggers.LogLevel.Warning);
                        continue;
                    }

                    if (aReg != 1 || bReg != 1)
                    {
                        string aStatus = aReg != 1 ? "A: нет PoE" : "A: ОК";
                        string bStatus = bReg != 1 ? "B: нет PoE" : "B: ОК";
                        _logger.LogToUser($"На порту {port} обнаружена проблема с подачей PoE. {aStatus}, {bStatus}.", Loggers.LogLevel.Error);
                        ServerPoeTestResult.AddSubTest($"PoE-тест порта {port}", false, $"проблема с подачей PoE: A = {aStatus}, B = {bStatus}");

                        await StopHard();
                        return false;
                    }

                    _logger.LogToUser($"PoE успешно подано на порт {port}.", Loggers.LogLevel.Success);
                    ServerPoeTestResult.AddSubTest($"PoE-тест порта {port}", true, $"A: OK, B: OK"); // ВОЗМОЖНО нужно выводить а и б 

                }
                return true;
            }
            catch (OperationCanceledException)
            {
                _logger.LogToUser("Тест подачи PoE был отменён пользователем.", Loggers.LogLevel.Warning);
                ServerPoeTestResult.AddSubTest($"PoE-тест портов", false, $"Тест подачи PoE был отменён пользователем");
                await StopHard();
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogToUser($"Ошибка при выполнении PoE-теста: {ex.Message}", Loggers.LogLevel.Error);
                ServerPoeTestResult.AddSubTest($"PoE-тест портов", false, $"Ошибка при выполнении PoE-теста: {ex.Message}");
                await StopHard();
                return false;
            }
        }

        public async Task<bool> RunUartInterfaceTestAsync(CancellationToken token)
        {
            try
            {

                _logger.Log("UART: Запускаем тест — записываем 1 в регистр 2493.", Loggers.LogLevel.Debug);
                await _modbusService.WriteSingleRegisterAsync(2493, 1);

                // Ожидаем, пока значение в 2494 станет 1 (тест запущен), максимум 5 секунд
                var sw = Stopwatch.StartNew();
                while (PoeRegisters.UartTestStart != 2 && sw.Elapsed < TimeSpan.FromSeconds(5))
                {
                    token.ThrowIfCancellationRequested();
                    _logger.Log($"UART: Ожидание запуска теста. Значение 2494: {PoeRegisters.UartTestResult}", Loggers.LogLevel.Debug);
                    await Task.Delay(200);
                }

                if (PoeRegisters.UartTestStart != 2)
                {
                    _logger.LogToUser("Тест UART не запустился в течение 5 секунд.", Loggers.LogLevel.Error);
                    ServerPoeTestResult.AddSubTest($"UART", false, $"Тест UART не запустился в течение 5 секунд.");
                    await StopHard();
                    return false;
                }

                _logger.Log("UART: Тест запущен. Ожидаем завершения...", Loggers.LogLevel.Debug);

                // Ожидаем завершения теста (2494 = 2 или 3), максимум 10 секунд
                sw.Restart();
                while (PoeRegisters.UartTestStart == 2 && sw.Elapsed < TimeSpan.FromSeconds(10))
                {
                    token.ThrowIfCancellationRequested();
                    _logger.Log("UART: Тест выполняется...", Loggers.LogLevel.Debug);
                    await Task.Delay(500);
                }

                _logger.Log($"UART: Тест завершён. Статус 2494: {PoeRegisters.UartTestResult}", Loggers.LogLevel.Debug);

                if (PoeRegisters.UartTestResult == 2)
                {
                    _logger.LogToUser("Тестирование UART прошло успешно.", Loggers.LogLevel.Success);
                    ServerPoeTestResult.AddSubTest($"UART", true, $"успешно");
                }
                else if (PoeRegisters.UartTestResult == 3)
                {
                    _logger.LogToUser("Тестирование UART неудачно.", Loggers.LogLevel.Error);
                    ServerPoeTestResult.AddSubTest($"UART", false, $"ошибка");
                    await StopHard();
                    return false;
                }
                else
                {
                    _logger.LogToUser("Тестирование UART завершено с неизвестным статусом.", Loggers.LogLevel.Warning);
                    _logger.Log($"UART: Неизвестный статус после теста: {PoeRegisters.UartTestResult}", Loggers.LogLevel.Debug);
                    ServerPoeTestResult.AddSubTest($"UART", false, $"UART: Неизвестный статус после теста: {PoeRegisters.UartTestResult}");
                    await StopHard();
                    return false;
                }


                // Проверка напряжений на каналах PoE
                bool isTestPassed = true;

                isTestPassed &= CheckUartVoltage(PoeRegisters.UartCh1Voltage, TestConfig.UartCh1VoltageMin, TestConfig.UartCh1VoltageMax, "1");
                isTestPassed &= CheckUartVoltage(PoeRegisters.UartCh2Voltage, TestConfig.UartCh2VoltageMin, TestConfig.UartCh2VoltageMax, "2");
                isTestPassed &= CheckUartVoltage(PoeRegisters.UartCh3Voltage, TestConfig.UartCh3VoltageMin, TestConfig.UartCh3VoltageMax, "3");
                isTestPassed &= CheckUartVoltage(PoeRegisters.UartCh4Voltage, TestConfig.UartCh4VoltageMin, TestConfig.UartCh4VoltageMax, "4");
                isTestPassed &= CheckUartVoltage(PoeRegisters.UartCh5Voltage, TestConfig.UartCh5VoltageMin, TestConfig.UartCh5VoltageMax, "5");
                isTestPassed &= CheckUartVoltage(PoeRegisters.UartCh6Voltage, TestConfig.UartCh6VoltageMin, TestConfig.UartCh6VoltageMax, "6");
                isTestPassed &= CheckUartVoltage(PoeRegisters.UartCh7Voltage, TestConfig.UartCh7VoltageMin, TestConfig.UartCh7VoltageMax, "7");
                isTestPassed &= CheckUartVoltage(PoeRegisters.UartCh8Voltage, TestConfig.UartCh8VoltageMin, TestConfig.UartCh8VoltageMax, "8");
                isTestPassed &= CheckUartVoltage(PoeRegisters.UartCh9Voltage, TestConfig.UartCh9VoltageMin, TestConfig.UartCh9VoltageMax, "9");
                isTestPassed &= CheckUartVoltage(PoeRegisters.UartCh10Voltage, TestConfig.UartCh10VoltageMin, TestConfig.UartCh10VoltageMax, "10");
                isTestPassed &= CheckUartVoltage(PoeRegisters.UartCh11Voltage, TestConfig.UartCh11VoltageMin, TestConfig.UartCh11VoltageMax, "11");
                isTestPassed &= CheckUartVoltage(PoeRegisters.UartCh12Voltage, TestConfig.UartCh12VoltageMin, TestConfig.UartCh12VoltageMax, "12");
                isTestPassed &= CheckUartVoltage(PoeRegisters.UartCh13Voltage, TestConfig.UartCh13VoltageMin, TestConfig.UartCh13VoltageMax, "13");
                isTestPassed &= CheckUartVoltage(PoeRegisters.UartCh14Voltage, TestConfig.UartCh14VoltageMin, TestConfig.UartCh14VoltageMax, "14");
                isTestPassed &= CheckUartVoltage(PoeRegisters.UartCh15Voltage, TestConfig.UartCh15VoltageMin, TestConfig.UartCh15VoltageMax, "15");
                isTestPassed &= CheckUartVoltage(PoeRegisters.UartCh16Voltage, TestConfig.UartCh16VoltageMin, TestConfig.UartCh16VoltageMax, "16");

                if (isTestPassed)
                {
                    _logger.LogToUser("Тестирование напряжений на каналах UART прошло успешно.", Loggers.LogLevel.Success);
                    ServerPoeTestResult.AddSubTest($"UART напряжения", true, $"успешно");
                    return true;
                }
                else
                {
                    _logger.LogToUser("Тестирование напряжений на каналах UART завершилось с ошибками.", Loggers.LogLevel.Error);
                    ServerPoeTestResult.AddSubTest($"UART напряжения", false, $"ошибка");
                    await StopHard();
                    return false;
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogToUser("Тестирование интерфейса UART было отменено.", Loggers.LogLevel.Warning);
                ServerPoeTestResult.AddSubTest($"UART", false, $"Прерван вручную");
                await StopHard();
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogToUser($"Ошибка при тестировании интерфейса UART: {ex.Message}", Loggers.LogLevel.Error);
                _logger.Log( $"UART: Исключение - {ex}", Loggers.LogLevel.Debug);
                ServerPoeTestResult.AddSubTest($"UART", false, $"UART: Исключение - {ex}");
                await StopHard();
                return false;
            }
        }
        private bool CheckUartVoltage(ushort registerValue, int minVoltage, int maxVoltage, string channel)
        {
            
            if (registerValue < minVoltage  || registerValue > maxVoltage )
            {
                _logger.LogToUser($"Канал {channel}: напряжение {registerValue} находится вне допустимого диапазона ({minVoltage} - {maxVoltage}).", Loggers.LogLevel.Error);
                return false;
            }
            else
            {
                _logger.LogToUser($"Канал {channel}: напряжение {registerValue} в пределах нормы.", Loggers.LogLevel.Info);
                return true;
            }
        }
        private async Task<bool> RunLedTestAsync(CancellationToken token)
        {
            try
            {
                if (!TestConfig.IsLedTestRequired)
                {
                    _logger.LogToUser("Тестирование светодиодов отключено в профиле.", Loggers.LogLevel.Info);
                    return true;
                }

                _logger.LogToUser("Подтест: Проверка светодиодов...", Loggers.LogLevel.Info);

                bool allOk = true;

                for (int channel = 1; channel <= 16; channel++)
                {
                    token.ThrowIfCancellationRequested();

                    var (r, g, b, w) = GetChannelColorValues(channel);
                    bool isMatch = IsExpectedColor(r, g, b, w, TestConfig.LedColour);

                    if (isMatch)
                    {
                        _logger.Log($"Порт {channel}: Цвет соответствует ожидаемому — R:{r}, G:{g}, B:{b}, W:{w}", Loggers.LogLevel.Debug);
                        ServerPoeTestResult.AddSubTest($"Проверка светодиодов канал {channel}", true, $"Цвет соответствует ожидаемому ({TestConfig.LedColour})");
                    }
                    else
                    {
                        _logger.LogToUser($"Порт {channel}: Цвет не соответствует ожидаемому ({TestConfig.LedColour}).", Loggers.LogLevel.Error);
                        _logger.Log($"Порт {channel}: Получено — R:{r}, G:{g}, B:{b}, W:{w}", Loggers.LogLevel.Debug);
                        ServerPoeTestResult.AddSubTest($"Проверка светодиодов канал {channel}", false, $"Цвет не соответствует ожидаемому ({TestConfig.LedColour})");
                        allOk = false;
                    }

                    await Task.Delay(100, token); // небольшая задержка между каналами
                }

                if (allOk)
                {
                    _logger.LogToUser("Проверка светодиодов прошла успешно.", Loggers.LogLevel.Success);
                    return true;
                }
                else
                {
                    _logger.LogToUser("Проверка светодиодов завершилась с ошибками.", Loggers.LogLevel.Warning);
                    await StopHard();
                    return false;
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogToUser("Проверка светодиодов была отменена.", Loggers.LogLevel.Warning);
                ServerPoeTestResult.AddSubTest($"Проверка светодиодов", false, $"Прерван вручную");
                await StopHard();
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogToUser($"Ошибка во время проверки светодиодов: {ex.Message}", Loggers.LogLevel.Error);
                ServerPoeTestResult.AddSubTest($"Проверка светодиодов", false, $"Ошибка во время проверки светодиодов: {ex.Message}");
                await StopHard();
                return false;
            }
        }

        private bool IsExpectedColor(int r, int g, int b, int w, string expectedColor)
        {
            expectedColor = expectedColor.ToLower();
            return expectedColor switch
            {
                "blue" => b > r + 20 && b > g + 20 && b > 50,
                "red" => r > g + 20 && r > b + 20 && r > 50,
                "green" => g > r + 20 && g > b + 20 && g > 50,
                "white" => Math.Abs(r - g) < 20 && Math.Abs(r - b) < 20 && w > 50,
                _ => false
            };
        }
        private (int r, int g, int b, int w) GetChannelColorValues(int channel)
        {
            return channel switch
            {
                1 => (PoeRegisters.Red1AChannel1, PoeRegisters.Green1AChannel1, PoeRegisters.Blue1AChannel1, PoeRegisters.White1AChannel1),
                2 => (PoeRegisters.Red1BChannel2, PoeRegisters.Green1BChannel2, PoeRegisters.Blue1BChannel2, PoeRegisters.White1BChannel2),
                3 => (PoeRegisters.Red2AChannel3, PoeRegisters.Green2AChannel3, PoeRegisters.Blue2AChannel3, PoeRegisters.White2AChannel3),
                4 => (PoeRegisters.Red2BChannel4, PoeRegisters.Green2BChannel4, PoeRegisters.Blue2BChannel4, PoeRegisters.White2BChannel4),
                5 => (PoeRegisters.Red3AChannel5, PoeRegisters.Green3AChannel5, PoeRegisters.Blue3AChannel5, PoeRegisters.White3AChannel5),
                6 => (PoeRegisters.Red3BChannel6, PoeRegisters.Green3BChannel6, PoeRegisters.Blue3BChannel6, PoeRegisters.White3BChannel6),
                7 => (PoeRegisters.Red4AChannel7, PoeRegisters.Green4AChannel7, PoeRegisters.Blue4AChannel7, PoeRegisters.White4AChannel7),
                8 => (PoeRegisters.Red4BChannel8, PoeRegisters.Green4BChannel8, PoeRegisters.Blue4BChannel8, PoeRegisters.White4BChannel8),
                9 => (PoeRegisters.Red5AChannel9, PoeRegisters.Green5AChannel9, PoeRegisters.Blue5AChannel9, PoeRegisters.White5AChannel9),
                10 => (PoeRegisters.Red5BChannel10, PoeRegisters.Green5BChannel10, PoeRegisters.Blue5BChannel10, PoeRegisters.White5BChannel10),
                11 => (PoeRegisters.Red6AChannel11, PoeRegisters.Green6AChannel11, PoeRegisters.Blue6AChannel11, PoeRegisters.White6AChannel11),
                12 => (PoeRegisters.Red6BChannel12, PoeRegisters.Green6BChannel12, PoeRegisters.Blue6BChannel12, PoeRegisters.White6BChannel12),
                13 => (PoeRegisters.Red7AChannel13, PoeRegisters.Green7AChannel13, PoeRegisters.Blue7AChannel13, PoeRegisters.White7AChannel13),
                14 => (PoeRegisters.Red7BChannel14, PoeRegisters.Green7BChannel14, PoeRegisters.Blue7BChannel14, PoeRegisters.White7BChannel14),
                15 => (PoeRegisters.Red8AChannel15, PoeRegisters.Green8AChannel15, PoeRegisters.Blue8AChannel15, PoeRegisters.White8AChannel15),
                16 => (PoeRegisters.Red8BChannel16, PoeRegisters.Green8BChannel16, PoeRegisters.Blue8BChannel16, PoeRegisters.White8BChannel16),
                _ => (0, 0, 0, 0)
            };
        }

        private async Task LoadPoeReportAsync()
        {
            if (TestConfig.IsReportRequired)
            {
                try
                {
                    _logger.LogToUser("Подготовка к отправке результатов на сервер...", Loggers.LogLevel.Info);

                    _logger.Log($"isSuccess={ServerPoeTestResult.isSuccess}, isFull={ServerPoeTestResult.isFull}", Loggers.LogLevel.Debug);

                    _logger.LogToUser("Отправка отчёта на сервер...", Loggers.LogLevel.Info);

                    DeviceInfo di = Service.SendTestResult(ServerPoeTestResult, SessionId, true);

                    if (di == null)
                    {
                        _logger.LogToUser("Ошибка: не удалось отправить результаты на сервер.", Loggers.LogLevel.Error);
                        throw new Exception("Ошибка передачи результатов тестирования на сервер.");
                    }

                    _logger.LogToUser("Данные успешно отправлены.", Loggers.LogLevel.Success);

                    ServerPoeTestResult.deviceSerial = di.serialNumber;
                    _logger.LogToUser($"Серийный номер устройства, полученный от сервера: {di.serialNumber}", Loggers.LogLevel.Info);
                    _logger.Log($"DeviceInfo: serialNumber={di.serialNumber}, hw_version={di.hw_version}, identifier={di.identifier}", Loggers.LogLevel.Debug);
                }
                catch (Exception ex)
                {
                    _logger.LogToUser($"Ошибка при отправке отчёта: {ex.Message}", Loggers.LogLevel.Error);
                    _logger.Log($"StackTrace: {ex.StackTrace}", Loggers.LogLevel.Debug);
                }
            }
            else
            {
                _logger.LogToUser("Генерация и отправка отчета отключены в настройках профиля.", Loggers.LogLevel.Warning);
            }
        }


        private async Task OpenFlashProgramAsync()
        {
            await Task.Yield(); // Чтобы не было warning о синхронном методе

            var flashPath = Properties.Settings.Default.FlashProgramPath;
            if (!File.Exists(flashPath))
            {
                _logger.LogToUser($"Программа прошивки не найдена: {flashPath}", Loggers.LogLevel.Error);
                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = flashPath,
                    UseShellExecute = true
                });
                _logger.LogToUser("Открыта программа Xgpro для ручной прошивки.", Loggers.LogLevel.Info);
            }
            catch (Exception ex)
            {
                _logger.LogToUser($"Ошибка при запуске программы: {ex.Message}", Loggers.LogLevel.Error);
            }
        }
        private async Task OpenInstructionAsync()
        {
            await Task.Yield(); // Нужен, чтобы метод считался "асинхронным"

            var pdfPath = TestConfig.FlashInstructionPath;
            if (!File.Exists(pdfPath))
            {
                _logger.LogToUser($"Файл инструкции не найден: {pdfPath}", Loggers.LogLevel.Error);
                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = pdfPath,
                    UseShellExecute = true
                });
                _logger.LogToUser("Открыта инструкция по прошивке.", Loggers.LogLevel.Info);
            }
            catch (Exception ex)
            {
                _logger.LogToUser($"Ошибка при открытии инструкции: {ex.Message}", Loggers.LogLevel.Error);
            }
        }

        private async Task StopHard()
        {


            _logger.LogToUser("Прерывание тестирования...", Loggers.LogLevel.Warning);
            
            await _modbusService.WriteSingleRegisterAsync(2403, 0);
            await _modbusService.WriteSingleRegisterAsync(2492, 0);
            if (TestConfig.IsReportRequired)
            {
                await LoadPoeReportAsync();
            }
            _logger.LogToUser("Питание снято. Плату можно безопасно извлечь из стенда.", Loggers.LogLevel.Info);
        }
    }
}