using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using RTL.Logger;

namespace RTL.Services
{
    public class McuProgrammerService : IMcuProgrammerService
    {
        private readonly Loggers _logger;

        public McuProgrammerService(Loggers logger)
        {
            _logger = logger;
        }

        public async Task<bool> FlashMcuAsync(string batPath, string binPath, CancellationToken cancellationToken)
        {
            _logger.LogToUser("Подготовка к прошивке MCU...", Loggers.LogLevel.Info);

            if (string.IsNullOrEmpty(batPath) || !File.Exists(batPath))
            {
                _logger.LogToUser($"Ошибка: Файл .bat не найден по пути {batPath}", Loggers.LogLevel.Error);
                return false;
            }

            if (string.IsNullOrEmpty(binPath) || !File.Exists(binPath))
            {
                _logger.LogToUser($"Ошибка: .bin прошивка не найдена по пути {binPath}", Loggers.LogLevel.Error);
                return false;
            }

            try
            {
                string workingDirectory = Path.GetDirectoryName(batPath);
                string argumentPath = $"\"{binPath}\"";

                var processStartInfo = new ProcessStartInfo
                {
                    FileName = batPath,
                    Arguments = argumentPath,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = workingDirectory
                };

                using var process = new Process { StartInfo = processStartInfo };
                process.OutputDataReceived += (s, e) => { if (!string.IsNullOrWhiteSpace(e.Data)) _logger.Log(e.Data, Loggers.LogLevel.Debug); };
                process.ErrorDataReceived += (s, e) => { if (!string.IsNullOrWhiteSpace(e.Data)) _logger.Log(e.Data, Loggers.LogLevel.Error); };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                await process.WaitForExitAsync(cancellationToken);

                if (process.ExitCode != 0)
                {
                    _logger.LogToUser($"Ошибка прошивки! Код выхода: {process.ExitCode}", Loggers.LogLevel.Error);
                    return false;
                }

                _logger.LogToUser("Прошивка MCU завершена успешно.", Loggers.LogLevel.Success);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogToUser($"Ошибка во время прошивки MCU: {ex.Message}", Loggers.LogLevel.Error);
                return false;
            }
        }
    }
}
