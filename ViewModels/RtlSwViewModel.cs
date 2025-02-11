using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows.Threading;
using System.Windows;
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


namespace RTL.ViewModels
{
    public class RtlSwViewModel : Screen
    {


        private SerialPort _serialPortDut;
        private IModbusSerialMaster _modbusMaster;


        private bool _isDutConnected;


        public bool IsDutConnected
        {
            get => _isDutConnected;
            set => SetAndNotify(ref _isDutConnected, value);
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
        #region подключения модбас

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
            const int retryInterval = 2000; // Интервал между попытками (2 сек)
            const int timeout = 10000; // Общий таймаут 10 секунд

            while ((DateTime.Now - startTime).TotalMilliseconds < timeout)
            {
                try
                {
                    _serialPortCom = new SerialPort(Properties.Settings.Default.ComSW, 9600, Parity.None, 8, StopBits.One)
                    {
                        ReadTimeout = 3000
                    };
                    _serialPortCom.Open();
                    _modbusMaster = ModbusSerialMaster.CreateRtu(_serialPortCom);
                    _modbusMaster.Transport.Retries = 3;
                    IsModbusConnected = true;
                    _logger.LogToUser($"Подключено к {Properties.Settings.Default.ComSW}", Loggers.LogLevel.Success);

                    // Читаем тип стенда (регистр 0)
                    ushort standType = _modbusMaster.ReadHoldingRegisters(1, 0, 1)[0];
                    if (standType != 104)
                    {
                        _logger.LogToUser($"Ошибка: неверный тип стенда ({standType}). Ожидалось: 104 (STAND RTL-SW).", Loggers.LogLevel.Error);
                        DisconnectPorts();
                        return false;
                    }
                    ReportModel.StandType = standType;
                    _logger.LogToUser($"Тип стенда: {standType} (STAND RTL-SW)", Loggers.LogLevel.Info);

                    // Читаем серийный номер стенда (регистр 2300)
                    ushort standSerial = _modbusMaster.ReadHoldingRegisters(1, 2300, 1)[0];
                    ReportModel.StandSerialNumber = standSerial;
                    _logger.LogToUser($"Серийный номер стенда: {standSerial}", Loggers.LogLevel.Info);

                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogToUser($"Ошибка подключения к {Properties.Settings.Default.ComSW}: {ex.Message}", Loggers.LogLevel.Warning);
                    await Task.Delay(retryInterval);
                }
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
        public RelayCommand ToggleModbusConnectionCommand { get; }


        private async Task ToggleModbusConnection()
        {
            if (IsModbusConnected)
            {
                DisconnectModbus();
            }
            else
            {
                await TryInitializeModbusAsync();
            }
        }

        private void DisconnectModbus()
        {
            try
            {
                _serialPortCom?.Close();
                _serialPortCom?.Dispose();
                IsModbusConnected = false;
                _logger.LogToUser("Modbus отключен", Loggers.LogLevel.Warning);
            }
            catch (Exception ex)
            {
                _logger.LogToUser($"Ошибка при отключении: {ex.Message}", Loggers.LogLevel.Error);
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
        private async Task<bool> TryConnectToServerAsync() // --- доделать получение session_id
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("User-Agent", "ftstand");

                    HttpResponseMessage response = await client.GetAsync("http://iccid.fort-telecom.ru/api/Api.svc/ping");
                    string responseContent = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode && !responseContent.Contains("error"))
                    {
                        _logger.LogToUser($"Подключение к серверу успешно. Код: {response.StatusCode}", LogLevel.Success);
                        IsServerConnected = true;
                        return true;
                    }
                    else
                    {
                        _logger.LogToUser($"Ошибка подключения к серверу. Код: {response.StatusCode}, Ответ: {responseContent}", LogLevel.Warning);
                        IsServerConnected = false;
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogToUser($"Ошибка подключения к серверу: {ex.Message}", LogLevel.Error);
                IsServerConnected = false;
                return false;
            }
        }

        #endregion подключение к серверу
        #region Профиль тестирования
        public RelayCommand LoadTestProfileCommand { get; }
        public ProfileTestModel TestConfig { get; private set; }
        private async Task<bool> TryLoadTestProfileAsync()
        {
            try
            {
                string testProfilePath = Properties.Settings.Default.RtlSwProfilePath;
                if (File.Exists(testProfilePath))
                {
                    string json = await File.ReadAllTextAsync(testProfilePath);
                    TestConfig = JsonConvert.DeserializeObject<ProfileTestModel>(json) ?? new ProfileTestModel();
                    _logger.LogToUser($"Файл тестирования загружен: {testProfilePath}", LogLevel.Success);

                    _logger.LogToUser($"Модель платы: {TestConfig.ModelName}, Тип платы: {TestConfig.ModelType}", LogLevel.Info);
                    return true;
                }
                else
                {
                    TestConfig = new ProfileTestModel();
                    _logger.LogToUser($"Файл тестирования {testProfilePath} не найден.", LogLevel.Warning);
                    return false;
                }
            }
            catch (JsonException ex)
            {
                TestConfig = new ProfileTestModel();
                _logger.LogToUser($"Ошибка обработки JSON: {ex.Message}", LogLevel.Error);
                return false;
            }
            catch (Exception ex)
            {
                TestConfig = new ProfileTestModel();
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

            ToggleModbusConnectionCommand = new RelayCommand(async () => await ToggleModbusConnection());
            _ = TryInitializeModbusAsync();

            ConnectToServerCommand = new RelayCommand(async () => await TryConnectToServerAsync());
            _ = TryConnectToServerAsync();

            LoadTestProfileCommand = new RelayCommand(async () => await TryLoadTestProfileAsync());
            _ = TryLoadTestProfileAsync();




        }

      


        public void DisconnectPorts()
        {
            try
            {
                _serialPortCom?.Close();
                IsModbusConnected = false;
                _serialPortDut?.Close();
                IsDutConnected = false;
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

        protected override void OnClose()
        {
            _logger.LogToUser("Закрытие приложения, отключение портов...", Loggers.LogLevel.Info);
            DisconnectPorts();
            base.OnClose();
        }
    }
}