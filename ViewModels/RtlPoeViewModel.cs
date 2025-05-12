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

        private bool _isStandConnected;
        public bool IsStandConnected
        {
            get => _isStandConnected;
            set => SetAndNotify(ref _isStandConnected, value);
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


        public RtlPoeViewModel([Inject(Key = "POE")] Loggers logger)
        {
            _logger = logger;
            _modbusService = new ModbusService(_logger, () => RTL.Properties.Settings.Default.ComPoe);
            ConnectCommand = new RelayCommand(async () => await ToggleConnectionAsync());
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
                    // Загружаем профиль перед стартом мониторинга
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

                    IsStandConnected = true;
                    _logger.LogToUser("Успешное подключение к стенду POE.", Loggers.LogLevel.Success);

                    _ = MonitorPoeAsync(); // запускаем мониторинг без ожидания
                }
                else
                {
                    _logger.LogToUser("Ошибка подключения к стенду POE.", Loggers.LogLevel.Error);
                }
            }
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
                        if (_isPoeTestRunning)
                        {
                            _logger.LogToUser("Тумблер RUN выключен. Прерывание теста.", Loggers.LogLevel.Warning);
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

                }
                catch (Exception ex)
                {
                    _logger.Log($"Ошибка при чтении регистров POE: {ex.Message}", Loggers.LogLevel.Error);
                }

                await Task.Delay(1000); // частота опроса
            }
        }


        private async Task StartPoeTestAsync(CancellationToken token)
        {
            _isPoeTestRunning = true;

            try
            {
                _logger.LogToUser("Тест запущен.", Loggers.LogLevel.Info);

                // === Подтест 1 ===FlashFirmwareAuto
                if (TestConfig.FlashFirmwareAuto)
                {
                    _logger.LogToUser("Подтест 1: запуск...", Loggers.LogLevel.Info);
                    if (!await RunPoeSubTest1Async(token)) return;
                    _logger.LogToUser("Подтест 1: успешно завершён.", Loggers.LogLevel.Success);
                }
                else
                {
                    _logger.LogToUser("Подтест 1: пропущен (отключён в профиле).", Loggers.LogLevel.Warning);
                }

                // === Подтест 2 ===
                if (TestConfig.McuFirmwareAuto)
                {
                    _logger.LogToUser("Подтест 2: запуск...", Loggers.LogLevel.Info);
                    if (!await RunPoeSubTest2Async(token)) return;
                    _logger.LogToUser("Подтест 2: успешно завершён.", Loggers.LogLevel.Success);
                }
                else
                {
                    _logger.LogToUser("Подтест 2: пропущен (отключён в профиле).", Loggers.LogLevel.Warning);
                }

                // === Подтест 3 ===
                if (TestConfig.FlashFirmwareAuto)
                {
                    _logger.LogToUser("Подтест 3: запуск...", Loggers.LogLevel.Info);
                    if (!await RunPoeSubTest3Async(token)) return;
                    _logger.LogToUser("Подтест 3: успешно завершён.", Loggers.LogLevel.Success);
                }
                else
                {
                    _logger.LogToUser("Подтест 3: пропущен (отключён в профиле).", Loggers.LogLevel.Warning);
                }

                _logger.LogToUser("Все активные подтесты завершены успешно.", Loggers.LogLevel.Success);
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
        private async Task<bool> RunPoeSubTest1Async(CancellationToken token)
        {
            try
            {
                for (int i = 0; i < 3; i++)
                {
                    token.ThrowIfCancellationRequested();
                    _logger.LogToUser($"Подтест 1 — шаг {i + 1}/3...", Loggers.LogLevel.Info);
                    await Task.Delay(400, token);
                }

                return true;
            }
            catch (OperationCanceledException)
            {
                _logger.LogToUser("Подтест 1 был отменён.", Loggers.LogLevel.Warning);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogToUser($"Ошибка в подтесте 1: {ex.Message}", Loggers.LogLevel.Error);
                return false;
            }
        }

        private async Task<bool> RunPoeSubTest2Async(CancellationToken token)
        {
            try
            {
                await Task.Delay(1000, token); // имитация долгой операции
                return true;
            }
            catch (OperationCanceledException)
            {
                _logger.LogToUser("Подтест 2 был отменён.", Loggers.LogLevel.Warning);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogToUser($"Ошибка в подтесте 2: {ex.Message}", Loggers.LogLevel.Error);
                return false;
            }
        }

        private async Task<bool> RunPoeSubTest3Async(CancellationToken token)
        {
            try
            {
                await Task.Delay(700, token); // другой пример
                return true;
            }
            catch (OperationCanceledException)
            {
                _logger.LogToUser("Подтест 3 был отменён.", Loggers.LogLevel.Warning);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogToUser($"Ошибка в подтесте 3: {ex.Message}", Loggers.LogLevel.Error);
                return false;
            }
        }



    }
}