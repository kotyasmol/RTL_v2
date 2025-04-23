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
        /* private IModbusMaster _modbusMaster;
         private SerialPort _serialPort;

         public bool IsConnected => _modbusMaster != null;

         public async Task<bool> TryInitializeAsync(string portName, int baudRate = 9600, Parity parity = Parity.None, int dataBits = 8, StopBits stopBits = StopBits.One)
         {
             try
             {
                 _serialPort = new SerialPort(portName, baudRate, parity, dataBits, stopBits);
                 _serialPort.Open();
                 var adapter = new SerialPortAdapter(_serialPort);
                 _modbusMaster = ModbusSerialMaster.CreateRtu(adapter);
                 return true;
             }
             catch (Exception ex)
             {
                 _modbusMaster = null;
                 _serialPort = null;
                 Loggers.LogToUser(LogLevel.Error, $"Не удалось подключиться к {portName}: {ex.Message}");
                 return false;
             }
         }

         public async Task<ushort[]> ReadRegistersWithRetryAsync(byte slaveId, ushort startAddress, ushort numRegisters, int retryCount = 3)
         {
             for (int i = 0; i < retryCount; i++)
             {
                 try
                 {
                     return _modbusMaster.ReadHoldingRegisters(slaveId, startAddress, numRegisters);
                 }
                 catch
                 {
                     await Task.Delay(200);
                 }
             }

             Loggers.LogToUser(LogLevel.Error, $"Ошибка чтения регистров с адреса {startAddress} (устройство {slaveId})");
             return null;
         }

         public async Task<bool> WriteSingleRegisterWithRetryAsync(byte slaveId, ushort registerAddress, ushort value, int retryCount = 3)
         {
             for (int i = 0; i < retryCount; i++)
             {
                 try
                 {
                     _modbusMaster.WriteSingleRegister(slaveId, registerAddress, value);
                     return true;
                 }
                 catch
                 {
                     await Task.Delay(200);
                 }
             }

             Loggers.LogToUser( $"Ошибка записи регистра {registerAddress} (устройство {slaveId})", LogLevel.Error);
             return false;
         }

         public void Disconnect()
         {
             _modbusMaster?.Dispose();
             _serialPort?.Close();
             _modbusMaster = null;
             _serialPort = null;
         }
     */
    }
}