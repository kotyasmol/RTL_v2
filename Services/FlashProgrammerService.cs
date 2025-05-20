using RTL.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using WindowsInput.Native;
using WindowsInput;
using RTL.Logger;
using System.IO;
using System.Windows;

namespace RTL.Services
{
    public class FlashProgrammerService : IFlashProgrammerService
    {
        private readonly Loggers _logger;

        public FlashProgrammerService(Loggers logger)
        {
            _logger = logger;
        }

        public async Task<bool> StartProgrammingAsync(FlashProgrammingContext ctx, CancellationToken cancellationToken)
        {
            if (!ctx.AutoMode)
            {
                return false;
            }

            if (ctx.IsFirstRun)
            {
                _logger.LogToUser($"Первый запуск прошивки. Проверка инструкции: {ctx.InstructionPath}", Loggers.LogLevel.Debug);

                // 1. Показать инструкцию
                if (!string.IsNullOrWhiteSpace(ctx.InstructionPath) && File.Exists(ctx.InstructionPath))
                {
                    try
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = ctx.InstructionPath,
                            UseShellExecute = true
                        });

                        _logger.LogToUser("Открыта инструкция по ручной прошивке.", Loggers.LogLevel.Info);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogToUser($"Ошибка при открытии инструкции: {ex.Message}", Loggers.LogLevel.Error);
                        return false;
                    }
                }
                else
                {
                    _logger.LogToUser("Инструкция по прошивке не найдена. Прошивка отменена.", Loggers.LogLevel.Error);
                    return false;
                }

                // 2. Запустить Xgpro
                if (string.IsNullOrWhiteSpace(ctx.FlashProgramPath) || !File.Exists(ctx.FlashProgramPath))
                {
                    _logger.LogToUser($"Программа для прошивки не найдена: {ctx.FlashProgramPath}", Loggers.LogLevel.Error);
                    return false;
                }

                try
                {
                    _logger.LogToUser("Запуск программы прошивки. Выполните ручную прошивку платы.", Loggers.LogLevel.Info);
                    Process.Start(ctx.FlashProgramPath);
                }
                catch (Exception ex)
                {
                    _logger.LogToUser($"Ошибка при запуске Xgpro: {ex.Message}", Loggers.LogLevel.Error);
                    return false;
                }

                // 3. Ждём ручной прошивки
                _logger.LogToUser("Ожидание завершения ручной прошивки... Закройте Xgpro", Loggers.LogLevel.Warning);

                // Ожидание закрытия Xgpro вручную
                try
                {
                    while (Process.GetProcessesByName("Xgpro").Any())
                    {
                        await Task.Delay(1000, cancellationToken);
                    }
                }
                catch (OperationCanceledException)
                {
                    _logger.LogToUser("Ожидание отменено.", Loggers.LogLevel.Warning);
                    return false;
                }

                _logger.LogToUser("Ручная прошивка завершена.", Loggers.LogLevel.Info);
                return true;
            }

            // Автоматическая прошивка
            return await PerformAutoFlashingAsync(ctx, cancellationToken);
        }



        private async Task<bool> PerformAutoFlashingAsync(FlashProgrammingContext ctx, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(ctx.FlashProgramPath) || !File.Exists(ctx.FlashProgramPath))
            {
                _logger.LogToUser($"Программа для прошивки не найдена: {ctx.FlashProgramPath}", Loggers.LogLevel.Error);
                return false;
            }

            if (string.IsNullOrWhiteSpace(ctx.ProjectFilePath) || !File.Exists(ctx.ProjectFilePath))
            {
                _logger.LogToUser($"Файл проекта для прошивки не найден: {ctx.ProjectFilePath}", Loggers.LogLevel.Error);
                return false;
            }

            Process programProcess = null;

            try
            {
                _logger.LogToUser("Проверка запущенных процессов Xgpro...", Loggers.LogLevel.Debug);

                programProcess = Process.GetProcessesByName("Xgpro").FirstOrDefault();

                if (programProcess == null)
                {
                    _logger.LogToUser("Запуск программы прошивки...", Loggers.LogLevel.Info);
                    programProcess = Process.Start(ctx.FlashProgramPath);
                    await Task.Delay(5000, cancellationToken);
                }
                else
                {
                    _logger.LogToUser("Программа прошивки уже запущена. Переключение фокуса...", Loggers.LogLevel.Info);
                }

                IntPtr hWnd = programProcess.MainWindowHandle;
                if (hWnd == IntPtr.Zero)
                    throw new InvalidOperationException("Не удалось найти главное окно программы");

                SetForegroundWindow(hWnd);

                var sim = new InputSimulator();

                _logger.LogToUser("Имитация нажатий: ALT+P, затем 'O'...", Loggers.LogLevel.Debug);
                sim.Keyboard.ModifiedKeyStroke(VirtualKeyCode.MENU, VirtualKeyCode.VK_P);
                await Task.Delay(500, cancellationToken);
                sim.Keyboard.KeyPress(VirtualKeyCode.VK_O);
                await Task.Delay(2000, cancellationToken);

                var fileName = Path.GetFileName(ctx.ProjectFilePath);
                SetClipboardText(fileName);

                _logger.LogToUser($"Вставка имени файла проекта: {fileName}", Loggers.LogLevel.Debug);
                sim.Keyboard.ModifiedKeyStroke(VirtualKeyCode.CONTROL, VirtualKeyCode.VK_V);
                await Task.Delay(500, cancellationToken);
                sim.Keyboard.KeyPress(VirtualKeyCode.RETURN);

                await Task.Delay(2000, cancellationToken);

                _logger.LogToUser("Подтверждение выбора и запуск прошивки...", Loggers.LogLevel.Debug);
                await Task.Delay(2000, cancellationToken);
                sim.Keyboard.KeyPress(VirtualKeyCode.RETURN);
                sim.Keyboard.ModifiedKeyStroke(VirtualKeyCode.MENU, VirtualKeyCode.VK_D);
                await Task.Delay(200, cancellationToken);
                sim.Keyboard.KeyPress(VirtualKeyCode.VK_P);


                sim.Keyboard.KeyPress(VirtualKeyCode.RETURN);

                _logger.LogToUser("Ожидание завершения прошивки...", Loggers.LogLevel.Info);

                var flashDelayTask = Task.Delay(ctx.FlashDelaySeconds * 1000, cancellationToken);
                var processExitedTask = Task.Run(async () =>
                {
                    while (!programProcess.HasExited)
                        await Task.Delay(500, cancellationToken);
                }, cancellationToken);

                var completedTask = await Task.WhenAny(flashDelayTask, processExitedTask);

                if (completedTask == processExitedTask)
                {
                    _logger.LogToUser("Ошибка: программа Xgpro была закрыта до завершения задержки прошивки.", Loggers.LogLevel.Error);
                    return false;
                }

                sim.Keyboard.KeyPress(VirtualKeyCode.RETURN); // возможно нужно это убрать
                await Task.Delay(500, cancellationToken);
                sim.Keyboard.KeyPress(VirtualKeyCode.RETURN);

                _logger.LogToUser("Прошивка завершена. Закрытие программы прошивки...", Loggers.LogLevel.Info);

                if (!programProcess.HasExited)
                    programProcess.Kill();

                SetForegroundWindow(Process.GetCurrentProcess().MainWindowHandle);
                _logger.LogToUser("Переключение обратно на стенд завершено.", Loggers.LogLevel.Info);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogToUser($"Ошибка прошивки: {ex.Message}", Loggers.LogLevel.Error);
                return false;
            }
        }

        private void SetClipboardText(string text)
        {
            var staThread = new Thread(() =>
            {
                try { Clipboard.SetText(text); }
                catch (Exception ex) { _logger.LogToUser($"Ошибка буфера обмена: {ex.Message}", Loggers.LogLevel.Error); }
            });
            staThread.SetApartmentState(ApartmentState.STA);
            staThread.Start();
            staThread.Join();
        }

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);
    }
}