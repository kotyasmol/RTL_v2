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

                    // Принудительное закрытие перед подключением
                    DisconnectModbus();
                    await Task.Delay(500); // Даем порту время освободиться

                    // Создаём и открываем порт
                    using (var serialPort = new SerialPort(Properties.Settings.Default.ComSW, 9600, Parity.None, 8, StopBits.One)
                    {
                        ReadTimeout = 3000
                    })
                    {
                        serialPort.Open();
                        _logger.LogToUser($"Порт {Properties.Settings.Default.ComSW} успешно открыт.", Loggers.LogLevel.Info);

                        // Создаём master после успешного открытия порта
                        var modbusMaster = ModbusSerialMaster.CreateRtu(serialPort);
                        modbusMaster.Transport.Retries = 3;
                        IsModbusConnected = true;

                        _logger.LogToUser("Попытка чтения регистра 0 для проверки типа стенда...", Loggers.LogLevel.Info);

                        var readTask = Task.Run(() =>
                        {
                            try
                            {
                                ushort standType = modbusMaster.ReadHoldingRegisters(1, 0, 1)[0];
                                if (standType != 104)
                                {
                                    _logger.LogToUser($"Ошибка: неверный тип стенда ({standType}). Ожидалось: 104 (STAND RTL-SW).", Loggers.LogLevel.Error);
                                    return false;
                                }

                                ushort standSerial = modbusMaster.ReadHoldingRegisters(1, 2300, 1)[0];
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