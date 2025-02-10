using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RTL.ReportGenerator
{
    public static class ReportGenerator
    {
        private static string _currentReportFile;

        /// <summary>
        /// Устанавливает новый путь к файлу отчёта при старте тестирования.
        /// </summary>
        public static void InitializeNewReportFile(string reportDirectory)
        {
            if (!Directory.Exists(reportDirectory))
            {
                Directory.CreateDirectory(reportDirectory);
            }

            string fileName = $"TestReport_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
            _currentReportFile = Path.Combine(reportDirectory, fileName);
        }

        /// <summary>
        /// Добавляет строку в текущий файл отчёта.
        /// </summary>
        public static void AppendToReport(string content)
        {
            if (string.IsNullOrEmpty(_currentReportFile))
            {
                throw new InvalidOperationException("Файл отчёта не инициализирован. Вызовите InitializeNewReportFile.");
            }

            try
            {
                File.AppendAllText(_currentReportFile, content + Environment.NewLine, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка записи в файл отчёта: {ex.Message}");
            }
        }
    }
}
