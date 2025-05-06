using Modbus.Device;
using RTL.Logger;
using System;
using System.IO.Ports;
using System.Threading.Tasks;

namespace RTL.Services
{
    public class ModbusService
    {
        private SerialPort _serialPort;
        private IModbusSerialMaster _modbusMaster;
        private readonly Loggers _logger;
        private readonly Func<string> _getPortName;
        private bool _isConnected;

        public bool IsConnected => _isConnected;

        public ModbusService(Loggers logger, Func<string> getPortName)
        {
            _logger = logger;
            _getPortName = getPortName;
        }

        public async Task<bool> ConnectAsync()
        {
            Disconnect();

            string portName = _getPortName();

            if (string.IsNullOrWhiteSpace(portName))
            {
                _logger.LogToUser("Ошибка: COM-порт не указан.", Loggers.LogLevel.Error);
                return false;
            }

            try
            {
                _logger.LogToUser($"Попытка открыть {portName}...", Loggers.LogLevel.Info);

                _serialPort = new SerialPort(portName, 9600, Parity.None, 8, StopBits.One)
                {
                    ReadTimeout = 3000,
                    WriteTimeout = 3000
                };

                _serialPort.Open();
                _modbusMaster = ModbusSerialMaster.CreateRtu(_serialPort);
                _modbusMaster.Transport.Retries = 3;
                _isConnected = true;

                _logger.LogToUser($"Порт {portName} успешно открыт.", Loggers.LogLevel.Success);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogToUser($"Ошибка подключения к {portName}: {ex.Message}", Loggers.LogLevel.Error);
                return false;
            }
        }

        public async Task<bool> WriteSingleRegisterAsync(ushort register, ushort value)
        {
            if (!_isConnected)
            {
                _logger.LogToUser("Modbus не подключён. Попытка подключения...", Loggers.LogLevel.Warning);
                if (!await ConnectAsync())
                    return false;
            }

            try
            {
                _logger.Log($"Запись: {register} = {value}", Loggers.LogLevel.Debug);
                _modbusMaster.WriteSingleRegister(1, register, value);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogToUser($"Ошибка записи в регистр {register}: {ex.Message}", Loggers.LogLevel.Error);
                return false;
            }
        }

        public async Task<ushort[]> ReadRegistersAsync(ushort startAddress, ushort count)
        {
            if (!_isConnected)
            {
                _logger.LogToUser("Modbus не подключён. Попытка подключения...", Loggers.LogLevel.Warning);
                if (!await ConnectAsync())
                    return null;
            }

            try
            {
                return _modbusMaster.ReadHoldingRegisters(1, startAddress, count);
            }
            catch (Exception ex)
            {
                _logger.LogToUser($"Ошибка чтения регистров: {ex.Message}", Loggers.LogLevel.Error);
                return null;
            }
        }

        public void Disconnect()
        {
            try
            {
                _modbusMaster = null;

                if (_serialPort != null)
                {
                    if (_serialPort.IsOpen)
                        _serialPort.Close();
                    _serialPort.Dispose();
                    _serialPort = null;

                    _logger.LogToUser("COM-порт закрыт.", Loggers.LogLevel.Info);
                }

                _isConnected = false;
            }
            catch (Exception ex)
            {
                _logger.LogToUser($"Ошибка при отключении: {ex.Message}", Loggers.LogLevel.Error);
            }
        }
    }
}
