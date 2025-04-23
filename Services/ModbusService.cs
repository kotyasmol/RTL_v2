using Modbus.Device;
using RTL.Logger;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RTL.Services
{
    public class ModbusService
    {
        private SerialPort _serialPort;
        private IModbusSerialMaster _master;
        
        private bool _isConnected;
        public bool IsConnected => _isConnected;

        private readonly Loggers _logger;

        public ModbusService(Loggers logger)
        {
            _logger = logger;
        }


        public async Task<bool> ConnectAsync(string portName, CancellationToken token)
        {
            Disconnect();

            try
            {
                _logger.LogToUser($"Попытка открыть {portName}...", Loggers.LogLevel.Info);

                _serialPort = new SerialPort(portName, 9600, Parity.None, 8, StopBits.One)
                {
                    ReadTimeout = 3000,
                    WriteTimeout = 3000
                };

                _serialPort.Open();
                _master = ModbusSerialMaster.CreateRtu(_serialPort);
                _master.Transport.Retries = 3;

                _isConnected = true;
                _logger.LogToUser($"Порт {portName} успешно открыт.", Loggers.LogLevel.Info);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogToUser($"Ошибка при подключении к {portName}: {ex.Message}", Loggers.LogLevel.Error);
                return false;
            }
        }

        public void Disconnect()
        {
            try
            {
                if (_serialPort != null)
                {
                    if (_serialPort.IsOpen)
                        _serialPort.Close();
                    _serialPort.Dispose();
                }

                _serialPort = null;
                _master = null;
                _isConnected = false;
                _logger.LogToUser("Modbus отключен", Loggers.LogLevel.Warning);
            }
            catch (Exception ex)
            {
                _logger.LogToUser($"Ошибка при отключении: {ex.Message}", Loggers.LogLevel.Error);
            }
        }

        public async Task WriteRegisterAsync(ushort register, ushort value, int retries = 3)
        {
            for (int i = 0; i < retries; i++)
            {
                if (!_isConnected)
                {
                    _logger.LogToUser("Modbus не подключён", Loggers.LogLevel.Error);
                    return;
                }

                try
                {
                    _master.WriteSingleRegister(1, register, value);
                    _logger.Log($"Успешно записано: {register} = {value}", Loggers.LogLevel.Debug);
                    return;
                }
                catch (Exception ex)
                {
                    _logger.Log($"Ошибка записи {i + 1}: {ex.Message}", Loggers.LogLevel.Warning);
                    await Task.Delay(1000);
                }
            }

            _logger.LogToUser($"Не удалось записать {value} в {register} после {retries} попыток.", Loggers.LogLevel.Error);
        }

        public async Task<ushort[]> ReadRegistersAsync(ushort startAddress, ushort numRegisters)
        {
            if (!_isConnected)
                throw new InvalidOperationException("Modbus не подключен");

            try
            {
                return await Task.Run(() => _master.ReadHoldingRegisters(1, startAddress, numRegisters));
            }
            catch (Exception ex)
            {
                _logger.LogToUser($"Ошибка чтения регистров: {ex.Message}", Loggers.LogLevel.Error);
                return Array.Empty<ushort>();
            }
        }
    }
}